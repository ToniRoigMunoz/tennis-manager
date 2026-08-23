using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace TennisApi
{
    public class DevBoostPlayer(CosmosClient cosmos)
    {
        [Function("DevBoostPlayer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var userId = req.Query["userId"] ?? "demo-user-001";
            int value = int.TryParse(req.Query["value"], out var v) ? v : 90;

            var playersC = cosmos.GetContainer("TennisManagerDB", "players");
            var q = new QueryDefinition("SELECT * FROM c WHERE c.userId = @u").WithParameter("@u", userId);
            var opts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };
            using var iter = playersC.GetItemQueryIterator<PlayerDocument>(q, requestOptions: opts);
            var page = await iter.ReadNextAsync();
            var doc = page.FirstOrDefault();
            if (doc == null) return req.CreateResponse(HttpStatusCode.NotFound);

            foreach (var a in doc.Physical) a.Value = value;
            foreach (var a in doc.Mental) a.Value = value;
            foreach (var a in doc.Technical) a.Value = value;
            await playersC.UpsertItemAsync(doc, new PartitionKey(userId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync($"{{\"status\":\"jugador potenciado a {value}\"}}");
            return res;
        }
    }
}