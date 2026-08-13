using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class AdvanceDay(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("AdvanceDay")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                var userId = req.Query["userId"] ?? "demo-user-001";
                var sw = Stopwatch.StartNew();

                // 1. Cargar usuario → liga y temporada
                var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
                var user = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;

                // 2. Cargar el torneo activo del día (el que jugó el humano)
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(userId, new PartitionKey(userId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null;
                }

                int botsRewarded = 0;
                string? distributionNote = null;

                // 3. Si el torneo del día no está terminado, auto-resolverlo antes de repartir
                if (state != null && !state.Finished)
                {
                    await AutoResolveHumanTournament(state);
                    // Persistir el estado ya terminado
                    await atContainer.UpsertItemAsync(state, new PartitionKey(userId));
                    distributionNote = "El jugador no terminó su torneo; el servidor lo resolvió automáticamente.";
                }

                // 4. Repartir puntos a la liga del humano
                if (state != null && state.Finished)
                {
                    botsRewarded = await DistributeBotRewards(state);
                }
                else if (state == null)
                {
                    // No había torneo activo: resolvemos la liga del humano en headless,
                    // aplicándole SUS recompensas según hasta dónde llegue.
                    var catForHuman = await GetTodayCategory(user.SeasonId);
                    var (botsR, humanRound) = await HeadlessLeagueResolver.ResolveDailyTournament(
                        cosmos, user.LeagueId, catForHuman, Random.Shared.Next(),
                        humanUserId: userId);
                    botsRewarded = botsR;
                    distributionNote = $"El jugador no abrió la app; su torneo se resolvió solo (llegó a ronda de {humanRound}).";
                }

                // 5. Resolver el torneo diario de las otras ligas (sin humano)
                var category = state?.Category ?? "t250";
                // Categoría del torneo de hoy (para que todas las ligas usen la misma)
                var toursForCat = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var seasonForCat = (await toursForCat.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;
                var todayTour = seasonForCat.Tournaments.FirstOrDefault(t => t.StartDay == seasonForCat.CurrentDay);
                if (todayTour != null) category = todayTour.Category;

                var otherLeagues = new[] { "league-primera", "league-tercera" };
                int otherLeaguesResolved = 0;
                foreach (var otherLeagueId in otherLeagues)
                {
                    var resolveSeed = Random.Shared.Next();
                    await HeadlessLeagueResolver.ResolveDailyTournament(cosmos, otherLeagueId, category, resolveSeed);
                    otherLeaguesResolved++;
                }

                // 6. Incrementar el día de la temporada
                var toursContainer = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursContainer.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;
                int previousDay = season.CurrentDay;
                object? endSeasonSummary = null;

                if (season.CurrentDay >= season.TotalDays)
                {
                    // Fin de temporada: campeones, ascensos/descensos, mejora de atributos, reset
                    endSeasonSummary = await EndSeason.Run(cosmos, userId);
                    season.CurrentDay = 1;
                    season.SeasonNumber = (season.SeasonNumber <= 0 ? 1 : season.SeasonNumber) + 1;
                    season.CurrentDayStartUtc = ServerClock.EffectiveNow(season).ToString("o");
                }
                else
                {
                    season.CurrentDay++;
                    season.CurrentDayStartUtc = ServerClock.EffectiveNow(season).ToString("o");
                }
                await toursContainer.UpsertItemAsync(season, new PartitionKey(user.SeasonId));

                // 7. Limpiar el torneo del día (para que mañana se cree uno nuevo)
                if (state != null)
                {
                    try
                    {
                        await atContainer.DeleteItemAsync<ActiveTournamentDoc>(userId, new PartitionKey(userId));
                    }
                    catch (CosmosException) { /* ya no existe, sin problema */ }
                }

                var result = new
                {
                    status = "dayAdvanced",
                    previousDay,
                    newDay = season.CurrentDay,
                    totalDays = season.TotalDays,
                    seasonFinished = season.CurrentDay >= season.TotalDays,
                    botsRewarded,
                    otherLeaguesResolved,
                    distributionNote,
                    elapsedMs = sw.ElapsedMilliseconds,
                    endSeason = endSeasonSummary,
                };

                var res = req.CreateResponse(HttpStatusCode.OK);
                res.Headers.Add("Content-Type", "application/json");
                await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
                return res;
            }
            catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                err.Headers.Add("Content-Type", "application/json");
                await err.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message, stack = ex.StackTrace }));
                return err;
            }
        }

        // Reparte puntos (y mejora de atributos al campeón) a los bots de la liga,
        // según la ronda que alcanzó cada uno en el torneo del día. Recalcula la clasificación.
        private async Task<int> DistributeBotRewards(ActiveTournamentDoc state)
        {
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;

            int championBase = TournamentRewards.ChampionPoints(state.Category);
            double champAttrGain = TournamentRewards.ChampionAttributeGain(state.Category);
            int rewarded = 0;

            // ID del bot campeón (si el campeón es un bot)
            string? championBotId = state.ChampionId;

            foreach (var standing in league.Standings.Where(s => !string.IsNullOrEmpty(s.BotId)))
            {
                if (!state.ReachedRound.TryGetValue(standing.BotId!, out var reached)) continue;

                bool isChampion = championBotId == standing.BotId;
                double fraction = TournamentRewards.PointsFractionByRound(reached, isChampion);
                int points = (int)Math.Round(championBase * fraction);
                standing.Points += points;
                rewarded++;

                // Mejora de atributos SOLO al bot campeón (acumulación fraccionada, igual que el humano)
                if (isChampion && champAttrGain > 0)
                {
                    await ImproveBotAttributes(state.LeagueId, standing.BotId!, champAttrGain);
                }
            }

            // Recalcular la clasificación: ordenar por puntos y reasignar posiciones
            league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
            for (int i = 0; i < league.Standings.Count; i++)
                league.Standings[i].Position = i + 1;

            await leaguesContainer.UpsertItemAsync(league, new PartitionKey(state.LeagueId));
            return rewarded;
        }

        // Sube atributos de un bot con acumulación fraccionada (igual que el humano)
        private async Task ImproveBotAttributes(string leagueId, string botId, double gain)
        {
            var botsContainer = cosmos.GetContainer("TennisManagerDB", "bots");
            BotDocument bot;
            try
            {
                bot = (await botsContainer.ReadItemAsync<BotDocument>(botId, new PartitionKey(leagueId))).Resource;
            }
            catch (CosmosException) { return; }

            bot.AttributeProgress += gain;
            int applied = 0;
            while (bot.AttributeProgress >= 1.0)
            {
                bot.AttributeProgress -= 1.0;
                applied++;
            }
            if (applied > 0)
            {
                BumpAll(bot.Physical, applied);
                BumpAll(bot.Mental, applied);
                BumpAll(bot.Technical, applied);
                // Recalcular overall como media de los 15 atributos
                var all = bot.Physical.Concat(bot.Mental).Concat(bot.Technical).ToList();
                bot.Overall = (int)Math.Round(all.Average(a => a.Value));
            }
            await botsContainer.UpsertItemAsync(bot, new PartitionKey(leagueId));
        }

        private static void BumpAll(List<AttributeDoc> attrs, int amount)
        {
            foreach (var a in attrs)
                a.Value = Math.Min(a.Value + amount, 99);
        }

        // Resuelve automáticamente el recorrido restante del humano cuando no terminó
        // su torneo. Simula sus partidos pendientes hasta que el torneo acaba.
        private async Task AutoResolveHumanTournament(ActiveTournamentDoc state)
        {
            await RehydrateSims(state);

            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var byId = all.ToDictionary(p => p.Id, p => p);
            var human = all.FirstOrDefault(p => p.Id == state.UserId);
            if (human is null) return;

            // Bucle: mientras el torneo no esté terminado, resolver el partido pendiente del humano
            int safety = 0;
            while (!state.Finished && safety++ < 20)
            {
                var lastRound = state.History[^1];
                var humanMatch = lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId));

                if (humanMatch is null)
                {
                    // No hay partido pendiente del humano en la última ronda: algo raro, salimos
                    break;
                }

                // Simular el partido pendiente del humano
                var opponentName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;
                var opponent = all.FirstOrDefault(p => p.Name == opponentName && p.Id != human.Id);
                if (opponent is null) break;

                int matchSeed = state.Seed + state.CurrentRound * 7919 + 31;
                var (winnerId, score) = TournamentBracket.SimulateFast(human, opponent, matchSeed);
                bool humanWon = winnerId == human.Id;

                humanMatch.WinnerId = winnerId;
                humanMatch.WinnerName = humanWon ? human.Name : opponent.Name;
                humanMatch.SetsScore = score;

                if (humanWon)
                {
                    // Registrar al rival batido en ReachedRound (cayó en la ronda actual) antes de quitarlo, para que reciba sus puntos.
                    state.ReachedRound[opponent.Id] = RoundSizeFromName(lastRound.RoundName);
                    state.Survivors.RemoveAll(p => p.Name == opponentName && !p.IsHuman);
                    if (!state.Survivors.Any(p => p.IsHuman)) state.Survivors.Add(human);
                }
                else
                {
                    // El humano cae aquí
                    state.ReachedRound[state.UserId] = RoundSizeFromName(lastRound.RoundName);
                    state.HumanAlive = false;
                    state.HumanEliminatedRound = RoundSizeFromName(lastRound.RoundName);
                    if (!state.Survivors.Any(p => p.Id == opponent.Id)) state.Survivors.Add(opponent);
                }

                // Avanzar el estado del torneo tras integrar el resultado
                if (!state.HumanAlive)
                {
                    // El humano cayó: resolver el resto de golpe
                    var (champion, reached, history) = TournamentOrchestrator.ResolveRemainingFully(
                        state.Survivors, state.Seed + 50000, state.ReachedRound);
                    state.ReachedRound = reached;
                    state.History.AddRange(history);
                    state.Finished = true;
                    state.ChampionId = champion.Id;

                    // El humano también recibe sus recompensas (no las recibió porque no jugó)
                    await ApplyHumanRewardsInternal(state, isChampion: false);
                }
                else if (state.Survivors.Count == 1 && state.Survivors[0].IsHuman)
                {
                    // El humano es el único que queda: campeón
                    state.Finished = true;
                    state.ChampionId = state.UserId;
                    state.ReachedRound[state.UserId] = 1;
                    await ApplyHumanRewardsInternal(state, isChampion: true);
                }
                else
                {
                    // Sigue vivo y queda torneo: montar la siguiente ronda saltando al humano
                    state.CurrentRound++;
                    var (matches, nextHumanMatch, advancing) =
                        TournamentOrchestrator.ResolveRoundSkippingHuman(
                            state.Survivors, state.Seed + state.CurrentRound * 1000, humanAlive: true);

                    RecordRound(state, matches, TournamentBracket.RoundName(state.Survivors.Count));
                    foreach (var m in matches.Where(m => m.WinnerId != null))
                    {
                        var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                        state.ReachedRound[loser.Id] = state.Survivors.Count;
                    }
                    state.Survivors = advancing;
                }
            }
        }

        private async Task RehydrateSims(ActiveTournamentDoc state)
        {
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);
            foreach (var p in state.Survivors)
                if (p.Sim == null && simById.TryGetValue(p.Id, out var sim))
                    p.Sim = sim;
        }

        private static void RecordRound(ActiveTournamentDoc state, List<BracketMatch> matches, string roundName)
        {
            var rec = new RoundRecord { RoundName = roundName };
            foreach (var m in matches)
            {
                var winnerName = m.WinnerId == null ? "(pendiente)"
                    : (m.WinnerId == m.Player1!.Id ? m.Player1!.Name : m.Player2!.Name);
                rec.Results.Add(new MatchRecord
                {
                    P1Name = m.Player1!.Name,
                    P2Name = m.Player2!.Name,
                    WinnerId = m.WinnerId ?? "",
                    WinnerName = winnerName,
                    SetsScore = m.SetsScore,
                    InvolvesHuman = m.InvolvesHuman,
                });
            }
            state.History.Add(rec);
        }

        private static int RoundSizeFromName(string roundName) => roundName switch
        {
            "Final" => 2,
            "Semifinales" => 4,
            "Cuartos de final" => 8,
            "Octavos de final" => 16,
            _ => 16,
        };

        // Aplica recompensas al humano (versión interna, sin devolver payload).
        // Reutiliza la lógica de reparto: puntos al ranking, dinero, descansos, atributos.
        private async Task ApplyHumanRewardsInternal(ActiveTournamentDoc state, bool isChampion)
        {
            int reachedRoundSize = state.ReachedRound.TryGetValue(state.UserId, out var rr) ? rr : 16;
            int championPoints = TournamentRewards.ChampionPoints(state.Category);
            double fraction = TournamentRewards.PointsFractionByRound(reachedRoundSize, isChampion);
            int pointsEarned = (int)Math.Round(championPoints * fraction);
            int moneyEarned = TournamentRewards.MoneyFromPoints(pointsEarned);
            int restsEarned = TournamentRewards.RestsByRound(reachedRoundSize, isChampion);
            double attrGain = isChampion ? TournamentRewards.ChampionAttributeGain(state.Category) : 0.0;

            var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
            var pQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", state.UserId);
            var pOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(state.UserId) };
            using var pIter = playersContainer.GetItemQueryIterator<PlayerDocument>(pQuery, requestOptions: pOpts);
            var pPage = await pIter.ReadNextAsync();
            var playerDoc = pPage.FirstOrDefault();

            if (playerDoc != null)
            {
                if (attrGain > 0)
                {
                    playerDoc.AttributeProgress += attrGain;
                    int applied = 0;
                    while (playerDoc.AttributeProgress >= 1.0) { playerDoc.AttributeProgress -= 1.0; applied++; }
                    if (applied > 0)
                    {
                        BumpAll(playerDoc.Physical, applied);
                        BumpAll(playerDoc.Mental, applied);
                        BumpAll(playerDoc.Technical, applied);
                    }
                }
                await playersContainer.UpsertItemAsync(playerDoc, new PartitionKey(state.UserId));
            }

            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var userDoc = (await usersContainer.ReadItemAsync<UserDocument>(state.UserId, new PartitionKey(state.UserId))).Resource;
            userDoc.Money += moneyEarned;
            userDoc.Rests += restsEarned;
            await usersContainer.UpsertItemAsync(userDoc, new PartitionKey(state.UserId));

            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;
            var myStanding = league.Standings.FirstOrDefault(s => s.UserId == state.UserId);
            if (myStanding != null)
            {
                myStanding.Points += pointsEarned;
                await leaguesContainer.UpsertItemAsync(league, new PartitionKey(state.LeagueId));
            }
        }

        private async Task<string> GetTodayCategory(string seasonId)
        {
            var tours = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await tours.ReadItemAsync<TournamentDocument>(seasonId, new PartitionKey(seasonId))).Resource;
            var today = season.Tournaments.FirstOrDefault(t => t.StartDay == season.CurrentDay);
            return today?.Category ?? "t250";
        }
    }
}