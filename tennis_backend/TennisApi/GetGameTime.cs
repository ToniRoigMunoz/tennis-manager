using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetGameTime(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetGameTime")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var seasonId = req.Query["seasonId"] ?? "season-2026-01";
            var container = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await container.ReadItemAsync<TournamentDocument>(seasonId, new PartitionKey(seasonId))).Resource;

            var now = ServerClock.EffectiveNow(season);
            var currentRound = ServerClock.CurrentUnlockedRound(season);

            var roundTimes = new List<object>();
            for (int r = 1; r <= ServerClock.MaxRounds; r++)
            {
                var unlock = ServerClock.RoundUnlockTime(season, r);
                roundTimes.Add(new
                {
                    round = r,
                    unlockUtc = unlock.ToString("o"),
                    isUnlocked = now >= unlock,
                    isClosed = ServerClock.IsRoundClosed(season, r),
                });
            }

            var result = new
            {
                serverNowUtc = now.ToString("o"),
                currentDay = season.CurrentDay,
                dayStartUtc = ServerClock.DayStart(season).ToString("o"),
                roundIntervalMinutes = season.RoundIntervalMinutes,
                currentUnlockedRound = currentRound,
                rounds = roundTimes,
                devOffsetSeconds = season.DevTimeOffsetSeconds,
            };

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
            return res;
        }
    }
}