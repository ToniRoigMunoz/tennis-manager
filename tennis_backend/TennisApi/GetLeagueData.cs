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

            // Para cada jugador humano (userId != null), resolver su nombre actual
            // desde la fuente de verdad (players), ignorando el caché del standing.
            var humanStandings = league.Standings
                .Where(s => !string.IsNullOrEmpty(s.UserId))
                .ToList();

            await Task.WhenAll(humanStandings.Select(async standing =>
            {
                var query = new QueryDefinition(
                    "SELECT c.name FROM c WHERE c.userId = @uid")
                    .WithParameter("@uid", standing.UserId);

                var opts = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(standing.UserId!)
                };

                using var iter = playersContainer.GetItemQueryIterator<PlayerNameDto>(
                    query, requestOptions: opts);

                if (iter.HasMoreResults)
                {
                    var page = await iter.ReadNextAsync();
                    var player = page.FirstOrDefault();
                    if (player is not null)
                        standing.Name = player.Name; // sobreescribe el caché con el nombre real
                }
            }));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(league, Opts));
            return res;
        }
    }
}