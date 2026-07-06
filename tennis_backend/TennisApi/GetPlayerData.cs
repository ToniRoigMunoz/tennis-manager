using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetPlayerData(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetPlayerData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var userId = req.Query["userId"] ?? "demo-user-001";
            var container = cosmos.GetContainer("TennisManagerDB", "players");

            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid")
                .WithParameter("@uid", userId);
            var opts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };

            using var iter = container.GetItemQueryIterator<PlayerDocument>(query, requestOptions: opts);
            var page = await iter.ReadNextAsync();
            var player = page.FirstOrDefault();

            if (player is null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(player, Opts));
            return res;
        }
    }
}