using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetTournamentStatus(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetTournamentStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                var userId = req.Query["userId"] ?? "demo-user-001";
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");

                // 1. ¿Existe ya un torneo activo para este usuario?
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                        userId, new PartitionKey(userId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null; // no hay torneo, lo crearemos abajo
                }

                // 2. Si no existe, o el que existe ya terminó, creamos el torneo del día
                if (state == null || state.Finished)
                {
                    var payload = await TournamentBootstrap.CreateDailyTournament(cosmos, userId);
                    var resNew = req.CreateResponse(HttpStatusCode.OK);
                    resNew.Headers.Add("Content-Type", "application/json");
                    await resNew.WriteStringAsync(JsonSerializer.Serialize(payload, Opts));
                    return resNew;
                }

                // 3. Hay un torneo en curso: buscar el partido pendiente del humano
                var pendingMatch = FindPendingHumanMatch(state);
                object result;

                if (pendingMatch != null)
                {
                    var opponentName = pendingMatch.P1Name == GetHumanName(state, userId)
                        ? pendingMatch.P2Name
                        : pendingMatch.P1Name;

                    var opponent = state.Survivors.FirstOrDefault(p => p.Name == opponentName && !p.IsHuman);

                    result = new
                    {
                        status = "humanPlays",
                        tournamentName = state.TournamentName,
                        surface = state.Surface,
                        roundName = state.History.Count > 0 ? state.History[^1].RoundName : "Ronda",
                        opponent = new
                        {
                            id = opponent?.Id ?? "unknown",
                            name = opponentName,
                            overall = opponent?.Overall ?? 70,
                        },
                        seed = state.Seed,
                    };
                }
                else
                {
                    // No hay partido pendiente pero el torneo no está terminado:
                    // situación transitoria, lo tratamos como "sin partido jugable ahora"
                    result = new { status = "noPendingMatch", tournamentName = state.TournamentName };
                }

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

        // Busca en la última ronda registrada un partido del humano sin resolver
        private static MatchRecord? FindPendingHumanMatch(ActiveTournamentDoc state)
        {
            if (state.History.Count == 0) return null;
            var lastRound = state.History[^1];
            return lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId));
        }

        private static string GetHumanName(ActiveTournamentDoc state, string userId)
        {
            var human = state.Survivors.FirstOrDefault(p => p.IsHuman);
            if (human != null) return human.Name;
            // Buscar en el historial
            foreach (var round in state.History)
                foreach (var m in round.Results.Where(r => r.InvolvesHuman))
                    return m.WinnerId == userId ? m.WinnerName : (m.P1Name); // aproximación
            return "";
        }
    }
}