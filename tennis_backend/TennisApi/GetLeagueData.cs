using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TennisApi
{
    public class GetLeagueData(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // DTO mínimo solo para la resolución de nombres
        private record PlayerNameDto([property: JsonPropertyName("name")] string Name);

        private record BotNameDto([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);

        [Function("GetLeagueData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var leagueId = req.Query["leagueId"] ?? "league-elite-group-3";
            var leagueContainer   = cosmos.GetContainer("TennisManagerDB", "leagues");
            var playersContainer  = cosmos.GetContainer("TennisManagerDB", "players");

            var leagueResponse = await leagueContainer.ReadItemAsync<LeagueDocument>(
                leagueId, new PartitionKey(leagueId));
            var league = leagueResponse.Resource;

            // 1. Nombres de jugadores humanos (una consulta por jugador)
            var humanStandings = league.Standings
                .Where(s => !string.IsNullOrEmpty(s.UserId))
                .ToList();

            await Task.WhenAll(humanStandings.Select(async standing =>
            {
                var query = new QueryDefinition("SELECT c.name FROM c WHERE c.userId = @uid")
                    .WithParameter("@uid", standing.UserId);
                var opts = new QueryRequestOptions { PartitionKey = new PartitionKey(standing.UserId!) };

                using var iter = playersContainer.GetItemQueryIterator<PlayerNameDto>(query, requestOptions: opts);
                if (iter.HasMoreResults)
                {
                    var page = await iter.ReadNextAsync();
                    var player = page.FirstOrDefault();
                    if (player is not null) standing.Name = player.Name;
                }
            }));

            // 2. Nombres de bots: una sola consulta trae toda la partición de la liga
            var botsContainer = cosmos.GetContainer("TennisManagerDB", "bots");
            var botQuery = new QueryDefinition("SELECT c.id, c.name FROM c");
            var botOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(leagueId) };

            var botNames = new Dictionary<string, string>();
            using (var botIter = botsContainer.GetItemQueryIterator<BotNameDto>(botQuery, requestOptions: botOpts))
            {
                while (botIter.HasMoreResults)
                {
                    foreach (var b in await botIter.ReadNextAsync())
                        botNames[b.Id] = b.Name;
                }
            }

            foreach (var s in league.Standings.Where(s => !string.IsNullOrEmpty(s.BotId)))
            {
                if (botNames.TryGetValue(s.BotId!, out var botName))
                    s.Name = botName;
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(league, Opts));
            return res;
        }
    }
}