using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class SimulateMatch(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("SimulateMatch")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var userId         = req.Query["userId"] ?? "demo-user-001";
            var opponentName   = req.Query["opponentName"] ?? "Carlos Ferrer";
            var opponentOverall = int.TryParse(req.Query["opponentOverall"], out var oo) ? oo : 72;
            var bestOf         = int.TryParse(req.Query["bestOf"], out var bo) ? bo : 3;
            var seed           = int.TryParse(req.Query["seed"], out var sd) ? sd : Random.Shared.Next();

            // 1. Cargar al jugador humano desde Cosmos
            var container = cosmos.GetContainer("TennisManagerDB", "players");
            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", userId);
            var qopts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };

            using var iter = container.GetItemQueryIterator<PlayerDocument>(query, requestOptions: qopts);
            var page = await iter.ReadNextAsync();
            var doc = page.FirstOrDefault();
            if (doc is null) return req.CreateResponse(HttpStatusCode.NotFound);

            var human = new SimPlayer(doc.Name, doc.PlayingStyle, AttrsToDict(doc));

            // 2. Generar rival sintético alrededor de un "overall"
            var opponent = BuildSyntheticOpponent(opponentName, opponentOverall, seed);

            // 3. Simular
            var engine = new MatchEngine(human, opponent, bestOf, seed);
            var result = engine.Simulate();
            result.Seed = seed;

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
            return res;
        }

        private static Dictionary<string, int> AttrsToDict(PlayerDocument doc)
        {
            var d = new Dictionary<string, int>();
            foreach (var a in doc.Physical)  d[a.Name] = a.Value;
            foreach (var a in doc.Mental)    d[a.Name] = a.Value;
            foreach (var a in doc.Technical) d[a.Name] = a.Value;
            return d;
        }

        private static readonly string[] AllAttrs =
        [
            "Resistencia","Velocidad","Fuerza","Reflejos","Flexibilidad",
            "Sangre Fría","Concentración","Visión de Juego","Anticipación","Creatividad",
            "Saque","Derecha","Revés","Juego en la Red","Efecto"
        ];

        private static readonly string[] AllStyles =
        [
            "El Muro","Cañonero","Agresivo de Fondo","Contraatacante","Mago de la Pista","Francotirador"
        ];

        private static SimPlayer BuildSyntheticOpponent(string name, int overall, int seed)
        {
            var rng = new Random(seed + 999); // desfase para no correlacionar con el partido
            var attrs = new Dictionary<string, int>();
            foreach (var a in AllAttrs)
                attrs[a] = Math.Clamp(overall + rng.Next(-8, 9), 1, 100);
            var style = AllStyles[rng.Next(AllStyles.Length)];
            return new SimPlayer(name, style, attrs);
        }
    }
}