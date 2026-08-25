using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class AdvanceTournamentPayload
    {
        public string UserId { get; set; } = "";
        public bool HumanWon { get; set; }
        public string SetsScore { get; set; } = "";
    }

    public class AdvanceTournament(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts =
            new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("AdvanceTournament")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<AdvanceTournamentPayload>(body, Opts);
            if (payload is null || string.IsNullOrEmpty(payload.UserId))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");
            var leagueId = await LeagueLookup.GetLeagueId(cosmos, payload.UserId);
            var state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                leagueId, new PartitionKey(leagueId))).Resource;

            if (state.Finished)
                return req.CreateResponse(HttpStatusCode.Conflict);

            // Cargar la temporada para el reloj
            var toursC = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await toursC.ReadItemAsync<TournamentDocument>(
                "season-2026-01", new PartitionKey("season-2026-01"))).Resource;

            await RehydrateSims(state);

            // Cargar participantes para resolver rivales (humanos o bots)
            var allParticipants = await ParticipantLoader.Load(cosmos, state.LeagueId);

            // El humano que REPORTÓ (no "el humano" genérico)
            var reporter = allParticipants.FirstOrDefault(p => p.Id == payload.UserId);
            if (reporter is null) return req.CreateResponse(HttpStatusCode.NotFound);

            // Localizar el partido pendiente DE ESE humano en la ronda actual
            var lastRound = state.History[^1];
            var humanMatch = lastRound.Results.FirstOrDefault(r =>
                r.WinnerId == "" && (r.P1Name == reporter.Name || r.P2Name == reporter.Name));
            if (humanMatch is null)
                return req.CreateResponse(HttpStatusCode.Conflict);

            // Identificar al rival y si es humano o bot
            var opponentName = humanMatch.P1Name == reporter.Name ? humanMatch.P2Name : humanMatch.P1Name;
            var opponent = allParticipants.FirstOrDefault(p => p.Name == opponentName && p.Id != reporter.Id);
            bool opponentIsHuman = opponent?.IsHuman ?? false;
            int roundSize = RoundSizeFromName(lastRound.RoundName);

            if (payload.HumanWon)
            {
                // Gana el que reportó
                humanMatch.WinnerId = reporter.Id;
                humanMatch.WinnerName = reporter.Name;
                humanMatch.SetsScore = payload.SetsScore;

                // El rival (bot O humano) cae y se registra su ronda
                if (opponent != null)
                {
                    state.ReachedRound[opponent.Id] = roundSize;
                    if (opponentIsHuman && state.HumanStates.TryGetValue(opponent.Id, out var hsRivalOut))
                    {
                        hsRivalOut.Alive = false;
                        hsRivalOut.EliminatedRound = roundSize;
                    }
                }
                state.Survivors.RemoveAll(p => p.Name == opponentName && p.Id != reporter.Id);

                // El que reportó se une a los supervivientes (sin duplicar)
                if (!state.Survivors.Any(p => p.Id == reporter.Id))
                    state.Survivors.Add(reporter);
            }
            else
            {
                // Pierde el que reportó; gana el rival (bot o humano)
                humanMatch.WinnerId = opponent?.Id ?? "opponent";
                humanMatch.WinnerName = opponentName;
                humanMatch.SetsScore = payload.SetsScore;

                // El que reportó cae
                state.ReachedRound[reporter.Id] = roundSize;
                if (state.HumanStates.TryGetValue(reporter.Id, out var hsLost))
                {
                    hsLost.Alive = false;
                    hsLost.EliminatedRound = roundSize;
                }

                // El rival avanza (sin duplicar). Si es humano, sigue vivo con su partido ya resuelto.
                if (opponent != null && !state.Survivors.Any(p => p.Id == opponent.Id))
                    state.Survivors.Add(opponent);
            }

            // Compatibilidad con los campos singulares viejos (los lee alguna rama)
            state.HumanAlive = state.IsAlive(payload.UserId);
            if (!state.HumanAlive) state.HumanEliminatedRound = roundSize;

            // Decidir el siguiente paso
            object result = new { status = "error", message = "Estado no resuelto" };
            if (!state.HumanAlive)
            {
                // El humano cayó por lo que no resolvemos el resto del cuadro (se hará al avanzar el día). Solo aplicamos sus recompensas y marcamos que su torneo acabó por hoy.
                var rewards = await ApplyHumanRewards(state, payload.UserId, isChampion: false);

                result = new
                {
                    status = "finished",
                    humanWonTournament = false,
                    humanEliminatedRound = state.HumanEliminatedRound,
                    championName = (string?)null, // Aún no se conoce, el cuadro se resuelve luego
                    history = state.History,
                    rewards,
                };
            }
            else
            {
                // El humano ganó su partido. El que reportó queda a la espera de su próxima ronda.

                int wonRoundIndex = state.History.Count;      // la ronda que acaba de ganar
                bool wasFinal = lastRound.Results.Count == 1; // la final tiene un único partido

                if (wasFinal)
                {
                    // Ganó la final: es campeón
                    state.Finished = true;
                    state.ChampionId = payload.UserId;
                    state.ReachedRound[payload.UserId] = 1;

                    var rewards = await ApplyHumanRewards(state, payload.UserId, isChampion: true);

                    result = new
                    {
                        status = "finished",
                        humanWonTournament = true,
                        championName = reporter.Name,
                        history = state.History,
                        rewards,
                    };
                }
                else
                {
                    // Queda a la espera de la siguiente ronda (la montará el reloj)
                    int nextRound = wonRoundIndex + 1;
                    if (state.HumanStates.TryGetValue(payload.UserId, out var hsNext))
                        hsNext.RoundIndex = nextRound;
                    state.HumanRoundIndex = nextRound;

                    result = new
                    {
                        status = "waitingForRound",
                        tournamentName = state.TournamentName,
                        roundName = ServerClock.RoundNameByIndex(nextRound),
                        unlockUtc = ServerClock.RoundUnlockTime(season, nextRound).ToString("o"),
                        history = state.History,
                    };
                }
            }

            // Persistir el estado actualizado
            await atContainer.UpsertItemAsync(state, new PartitionKey(leagueId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
            return res;
        }

        // Reconstruye el Participant del humano desde Cosmos (con su Sim)
        private Participant? FindHuman(ActiveTournamentDoc state)
        {
            // El humano puede estar ya en Survivors si avanzó por bye; si no, lo recreamos
            var existing = state.Survivors.FirstOrDefault(p => p.IsHuman);
            if (existing != null) return existing;

            // Reconstruir desde el loader completo
            var all = ParticipantLoader.Load(cosmos, state.LeagueId).GetAwaiter().GetResult();
            return all.FirstOrDefault(p => p.Id == state.UserId);
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

        // Rehidrata el Sim de cada participante del estado (no se persiste en Cosmos)
        private async Task RehydrateSims(ActiveTournamentDoc state)
        {
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);

            foreach (var p in state.Survivors)
                if (p.Sim == null && simById.TryGetValue(p.Id, out var sim))
                    p.Sim = sim;
        }

        // Aplica las recompensas al humano cuando termina su recorrido en el torneo. Devuelve un resumen para el frontend.
        private async Task<object> ApplyHumanRewards(ActiveTournamentDoc state, string userId, bool isChampion)
        {
            // Ronda donde cayó (o 1 si es campeón). ReachedRound guarda el tamaño de ronda.
            int reachedRoundSize = state.ReachedRound.TryGetValue(userId, out var rr) ? rr : 16;

            // Calcular recompensas según categoría y ronda
            int championPoints = TournamentRewards.ChampionPoints(state.Category);
            double fraction = TournamentRewards.PointsFractionByRound(reachedRoundSize, isChampion);
            int pointsEarned = (int)Math.Round(championPoints * fraction);
            int moneyEarned = TournamentRewards.MoneyFromPoints(pointsEarned);
            int restsEarned = TournamentRewards.RestsByRound(reachedRoundSize, isChampion);
            double attrGain = isChampion ? TournamentRewards.ChampionAttributeGain(state.Category) : 0.0;

            // Actualizar el documento del jugador (dinero, descansos, atributos)
            var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
            var pQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", userId);
            var pOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };
            using var pIter = playersContainer.GetItemQueryIterator<PlayerDocument>(pQuery, requestOptions: pOpts);
            var pPage = await pIter.ReadNextAsync();
            var playerDoc = pPage.FirstOrDefault();

            int attrPointsApplied = 0;
            if (playerDoc != null)
            {
                // Subida fraccionada de atributos
                if (attrGain > 0)
                {
                    playerDoc.AttributeProgress += attrGain;
                    while (playerDoc.AttributeProgress >= 1.0)
                    {
                        playerDoc.AttributeProgress -= 1.0;
                        attrPointsApplied++;
                    }
                    if (attrPointsApplied > 0)
                    {
                        BumpAllAttributes(playerDoc.Physical, attrPointsApplied);
                        BumpAllAttributes(playerDoc.Mental, attrPointsApplied);
                        BumpAllAttributes(playerDoc.Technical, attrPointsApplied);
                    }
                }
                await playersContainer.UpsertItemAsync(playerDoc, new PartitionKey(userId));
            }

            // Actualizar dinero y descansos en el documento de usuario
            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var userDoc = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;
            userDoc.Money += moneyEarned;
            userDoc.Rests += restsEarned;
            await usersContainer.UpsertItemAsync(userDoc, new PartitionKey(userId));

            // Sumar puntos al ranking de la liga
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;
            var myStanding = league.Standings.FirstOrDefault(s => s.UserId == userId);
            if (myStanding != null)
            {
                myStanding.Points += pointsEarned;
                // Reordenar la clasificación por puntos y reasignar posiciones
                league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
                for (int i = 0; i < league.Standings.Count; i++)
                    league.Standings[i].Position = i + 1;
                await leaguesContainer.UpsertItemAsync(league, new PartitionKey(state.LeagueId));
            }

            // Resumen para el frontend
            return new
            {
                pointsEarned,
                moneyEarned,
                restsEarned,
                attributePointsApplied = attrPointsApplied,
                attributeProgress = playerDoc?.AttributeProgress ?? 0.0,
                isChampion,
            };
        }

        private static void BumpAllAttributes(List<AttributeDoc> attrs, int amount)
        {
            foreach (var a in attrs)
                a.Value = Math.Min(a.Value + amount, 99);
        }
    }
}