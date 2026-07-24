using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class SaveMatchPayload
    {
        public string UserId { get; set; } = "";
        public string OpponentName { get; set; } = "";
        public bool Won { get; set; }
        public string SetsScore { get; set; } = "";
        public int Aces { get; set; }
        public int Winners { get; set; }
        public int UnforcedErrors { get; set; }
    }

    public class SaveMatchResult(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts =
            new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("SaveMatchResult")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<SaveMatchPayload>(body, Opts);
            if (payload is null || string.IsNullOrEmpty(payload.UserId))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var container = cosmos.GetContainer("TennisManagerDB", "players");
            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid")
                .WithParameter("@uid", payload.UserId);
            var qopts = new QueryRequestOptions { PartitionKey = new PartitionKey(payload.UserId) };

            using var iter = container.GetItemQueryIterator<PlayerDocument>(query, requestOptions: qopts);
            var page = await iter.ReadNextAsync();
            var doc = page.FirstOrDefault();
            if (doc is null) return req.CreateResponse(HttpStatusCode.NotFound);

            doc.LastMatch = new MatchResultDoc
            {
                OpponentName   = payload.OpponentName,
                Won            = payload.Won,
                SetsScore      = payload.SetsScore,
                Aces           = payload.Aces,
                Winners        = payload.Winners,
                UnforcedErrors = payload.UnforcedErrors,
            };

            await container.UpsertItemAsync(doc, new PartitionKey(payload.UserId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync("{\"status\":\"ok\"}");
            return res;
        }
    }
}