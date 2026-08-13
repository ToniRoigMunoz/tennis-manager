using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class DevAdvanceClock(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("DevAdvanceClock")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var seasonId = req.Query["seasonId"] ?? "season-2026-01";
            int minutes = int.TryParse(req.Query["minutes"], out var m) ? m : 0;
            int hours = int.TryParse(req.Query["hours"], out var h) ? h : 0;
            bool reset = req.Query["reset"] == "true";

            var container = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await container.ReadItemAsync<TournamentDocument>(seasonId, new PartitionKey(seasonId))).Resource;

            if (reset)
                season.DevTimeOffsetSeconds = 0;
            else
                season.DevTimeOffsetSeconds += hours * 3600L + minutes * 60L;

            await container.UpsertItemAsync(season, new PartitionKey(seasonId));

            var result = new
            {
                devOffsetSeconds = season.DevTimeOffsetSeconds,
                effectiveNowUtc = ServerClock.EffectiveNow(season).ToString("o"),
                currentUnlockedRound = ServerClock.CurrentUnlockedRound(season),
            };

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
            return res;
        }
    }
}