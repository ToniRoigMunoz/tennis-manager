using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace TennisApi
{
    public class DevResetTournament(CosmosClient cosmos)
    {
        [Function("DevResetTournament")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var userId = req.Query["userId"] ?? "demo-user-001";
            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");

            try
            {
                await atContainer.DeleteItemAsync<ActiveTournamentDoc>(userId, new PartitionKey(userId));
            }
            catch (CosmosException) { /* no existía, sin problema */ }

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync("{\"status\":\"torneo activo borrado\"}");
            return res;
        }
    }
}