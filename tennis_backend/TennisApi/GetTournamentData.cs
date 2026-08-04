using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetTournamentData(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetTournamentData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var seasonId = req.Query["seasonId"] ?? "season-2026-01";
            var container = cosmos.GetContainer("TennisManagerDB", "tournaments");

            var response = await container.ReadItemAsync<TournamentDocument>(
                seasonId, new PartitionKey(seasonId));
            var season = response.Resource;

            // Derivar el estado de cada torneo según el día actual
            foreach (var t in season.Tournaments)
            {
                if (t.StartDay < season.CurrentDay)
                    t.Status = "past";
                else if (t.StartDay == season.CurrentDay)
                    t.Status = "current";
                else
                    t.Status = "upcoming";
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(season, Opts));
            return res;
        }
    }
}