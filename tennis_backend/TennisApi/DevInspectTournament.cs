using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class DevInspectTournament(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("DevInspectTournament")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var userId = req.Query["userId"] ?? "demo-user-001";
            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");
            var leagueId = await LeagueLookup.GetLeagueId(cosmos, userId);

            ActiveTournamentDoc? state;
            try
            {
                state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                    leagueId, new PartitionKey(leagueId))).Resource;
            }
            catch (CosmosException)
            {
                var notFound = req.CreateResponse(HttpStatusCode.OK);
                await notFound.WriteStringAsync("{\"status\":\"no hay torneo activo en esta liga\"}");
                return notFound;
            }

            // Devolvemos un resumen centrado en lo que nos interesa depurar los campos singulares viejos y el diccionario nuevo en paralelo.
            var summary = new
            {
                leagueId = state.LeagueId,
                finished = state.Finished,
                championId = state.ChampionId,
                singular = new
                {
                    userId = state.UserId,
                    humanAlive = state.HumanAlive,
                    humanEliminatedRound = state.HumanEliminatedRound,
                    humanRoundIndex = state.HumanRoundIndex,
                },
                humanStates = state.HumanStates,
                history = state.History.Select(r => new
                {
                    roundName = r.RoundName,
                    matches = r.Results.Select(m => new
                    {
                        p1 = m.P1Name,
                        p2 = m.P2Name,
                        winner = m.WinnerName,
                    }).ToList(),
                }).ToList(),
                survivors = state.Survivors.Select(p => p.Name).ToList(),
            };

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(summary, Opts));
            return res;
        }
    }
}