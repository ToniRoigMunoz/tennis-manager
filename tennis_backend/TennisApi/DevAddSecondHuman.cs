using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace TennisApi
{
    public class DevAddSecondHuman(CosmosClient cosmos)
    {
        [Function("DevAddSecondHuman")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var newUserId = req.Query["userId"] ?? "demo-user-002";
            var leagueId = req.Query["leagueId"] ?? "league-elite-group-3";
            var db = cosmos.GetDatabase("TennisManagerDB");

            // Crear el documento de usuario del segundo humano (misma liga y temporada)
            var users = db.GetContainer("users");
            await users.UpsertItemAsync(new UserDocument
            {
                Id = newUserId,
                Money = 1000,
                Rests = 5,
                LeagueId = leagueId,
                SeasonId = "season-2026-01",
            }, new PartitionKey(newUserId));

            // Crear su jugador humano
            var players = db.GetContainer("players");
            await players.UpsertItemAsync(new PlayerDocument
            {
                Id = $"player-{newUserId}",
                UserId = newUserId,
                Name = "Rival Humano",
                Nationality = "España",
                NationalityFlag = "🇪🇸",
                Age = 24,
                HeightCm = 185,
                WeightKg = 78,
                DominantHand = "Derecha",
                PlayingStyle = "Agresivo de fondo",
                CurrentEnergy = 100,
                MaxEnergy = 100,
                Physical = Attrs(("Velocidad", 70), ("Resistencia", 70), ("Fuerza", 70), ("Agilidad", 70), ("Salto", 70)),
                Mental = Attrs(("Concentración", 70), ("Sangre fría", 70), ("Táctica", 70), ("Competitividad", 70), ("Determinación", 70)),
                Technical = Attrs(("Derecha", 70), ("Revés", 70), ("Saque", 70), ("Volea", 70), ("Resto", 70)),
                AttributeProgress = 0,
            }, new PartitionKey(newUserId));

            // Reemplazar un bot de la liga por el nuevo humano (mantener los 24)
            var leagues = db.GetContainer("leagues");
            var league = (await leagues.ReadItemAsync<LeagueDocument>(leagueId, new PartitionKey(leagueId))).Resource;

            // Elegir un bot a reemplazar (el primero que no sea el humano existente)
            var botStanding = league.Standings.FirstOrDefault(s => !string.IsNullOrEmpty(s.BotId) && s.Position == 12) 
                ?? league.Standings.FirstOrDefault(s => !string.IsNullOrEmpty(s.BotId));
            string replacedInfo;
            if (botStanding != null)
            {
                // Borrar el documento del bot reemplazado
                var bots = db.GetContainer("bots");
                try { await bots.DeleteItemAsync<BotDocument>(botStanding.BotId!, new PartitionKey(leagueId)); }
                catch (CosmosException) { }

                replacedInfo = $"reemplazado bot {botStanding.Name} (pos {botStanding.Position})";

                // Convertir ese hueco en el nuevo humano
                botStanding.BotId = null;
                botStanding.UserId = newUserId;
                botStanding.Name = "Rival Humano";
            }
            else
            {
                replacedInfo = "no se encontró bot que reemplazar";
            }

            await leagues.UpsertItemAsync(league, new PartitionKey(leagueId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(
                $"{{\"status\":\"segundo humano creado ({newUserId})\",\"detalle\":\"{replacedInfo}\"}}");
            return res;
        }

        private static List<AttributeDoc> Attrs(params (string, int)[] items)
            => items.Select(i => new AttributeDoc { Name = i.Item1, Value = i.Item2 }).ToList();
    }
}