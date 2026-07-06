using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetLeagueData(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetLeagueData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var leagueId = req.Query["leagueId"] ?? "league-elite-group-3";
            var container = cosmos.GetContainer("TennisManagerDB", "leagues");

            var response = await container.ReadItemAsync<LeagueDocument>(
                leagueId, new PartitionKey(leagueId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(response.Resource, Opts));
            return res;
        }
    }
}