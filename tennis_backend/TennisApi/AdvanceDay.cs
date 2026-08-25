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

                // Cargar usuario, liga y temporada
                var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
                var user = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;

                // Cargar el torneo activo del día (el que jugó el humano)
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(user.LeagueId, new PartitionKey(user.LeagueId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null;
                }

                int botsRewarded = 0;
                string? distributionNote = null;

                var toursContainer = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursContainer.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;

                // Si el torneo del día no está terminado, resolverlo automáticamente (el humano no terminó de jugarlo)
                if (state != null && !state.Finished)
                {
                    // Resolver el torneo del día ENTERO con la lógica compartida gobernada por el reloj.
                    // Empujamos el reloj más allá de la última ronda para que SyncRoundsToClock
                    // simule todas las rondas pendientes (de todos los humanos y bots).
                    season.DevTimeOffsetSeconds += (long)(ServerClock.MaxRounds + 1) * season.RoundIntervalMinutes * 60;
                    await TournamentClockSync.SyncRoundsToClock(cosmos, state, season);

                    // Aplicar recompensas a los humanos que no las recibieron (no jugaron su torneo)
                    foreach (var hs in state.HumanStates.Values)
                        await ApplyHumanRewardsInternal(state, hs.UserId, isChampion: state.ChampionId == hs.UserId);

                    await atContainer.UpsertItemAsync(state, new PartitionKey(user.LeagueId));
                    distributionNote = "Torneo del día resuelto al avanzar (multi-humano).";
                }

                // Repartir puntos a la liga del humano
                if (state != null && state.Finished)
                {
                    botsRewarded = await DistributeBotRewards(state);
                }
                else if (state == null)
                {
                    // No había torneo activo, entonces resolvemos la liga del humano aplicándole sus recompensas según hasta dónde llegue.
                    var catForHuman = await GetTodayCategory(user.SeasonId);
                    var (botsR, humanRound) = await HeadlessLeagueResolver.ResolveDailyTournament(
                        cosmos, user.LeagueId, catForHuman, Random.Shared.Next(),
                        humanUserId: userId);
                    botsRewarded = botsR;
                    distributionNote = $"El jugador no abrió la app; su torneo se resolvió solo (llegó a ronda de {humanRound}).";
                }

                // Resolver el torneo diario de las otras ligas (sin humano)
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

                // Incrementar el día de la temporada
                int previousDay = season.CurrentDay;
                object? endSeasonSummary = null;

                if (season.CurrentDay >= season.TotalDays)
                {
                    // Fin de temporada, campeones, ascensos/descensos, mejora de atributos, reset
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

                // Limpiar el torneo del día (para que mañana se cree uno nuevo)
                if (state != null)
                {
                    try
                    {
                        await atContainer.DeleteItemAsync<ActiveTournamentDoc>(user.LeagueId, new PartitionKey(user.LeagueId));
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

        // Reparte puntos (y mejora de atributos al campeón) a los bots de la liga, según la ronda que alcanzó cada uno en el torneo del día. Recalcula la clasificación.
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

                // Mejora de atributos solo al bot campeón (acumulación fraccionada, igual que el humano)
                if (isChampion && champAttrGain > 0)
                {
                    await ImproveBotAttributes(state.LeagueId, standing.BotId!, champAttrGain);
                }
            }

            // Recalcular la clasificación, ordenar por puntos y reasignar posiciones
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

        // Aplica recompensas al humano. Reutiliza la lógica de reparto, puntos al ranking, dinero, descansos, atributos.
        private async Task ApplyHumanRewardsInternal(ActiveTournamentDoc state, string userId, bool isChampion)
        {
            int reachedRoundSize = state.ReachedRound.TryGetValue(userId, out var rr) ? rr : 16;
            int championPoints = TournamentRewards.ChampionPoints(state.Category);
            double fraction = TournamentRewards.PointsFractionByRound(reachedRoundSize, isChampion);
            int pointsEarned = (int)Math.Round(championPoints * fraction);
            int moneyEarned = TournamentRewards.MoneyFromPoints(pointsEarned);
            int restsEarned = TournamentRewards.RestsByRound(reachedRoundSize, isChampion);
            double attrGain = isChampion ? TournamentRewards.ChampionAttributeGain(state.Category) : 0.0;

            var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
            var pQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", userId);
            var pOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };
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
                await playersContainer.UpsertItemAsync(playerDoc, new PartitionKey(userId));
            }

            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var userDoc = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;
            userDoc.Money += moneyEarned;
            userDoc.Rests += restsEarned;
            await usersContainer.UpsertItemAsync(userDoc, new PartitionKey(userId));

            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;
            var myStanding = league.Standings.FirstOrDefault(s => s.UserId == userId);
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