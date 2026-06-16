using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks; // Añadido para poder usar Task

namespace TennisApi
{
    public class GetServerTime
    {
        [Function("GetServerTime")]
        // 1. Añadimos 'async Task<>' a la respuesta
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var data = new
            {
                serverTime = DateTime.UtcNow.ToString("o"),
                message = "Tennis Manager API funcionando correctamente"
            };
            
            // 2. Usamos WriteStringAsync con 'await'
            await response.WriteStringAsync(JsonSerializer.Serialize(data));
            
            return response;
        }
    }
}