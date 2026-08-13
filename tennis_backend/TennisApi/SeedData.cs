using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace TennisApi
{
    public class SeedData(CosmosClient cosmos)
    {
        [Function("SeedData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var db = cosmos.GetDatabase("TennisManagerDB");

            // ── USERS — solo datos de cuenta, sin partidos ────────────────────
            await db.GetContainer("users").UpsertItemAsync(new UserDocument
            {
                Id       = "demo-user-001",
                Money    = 1250,
                Rests    = 3,
                LeagueId = "league-elite-group-3",
                SeasonId = "season-2026-01",
            }, new PartitionKey("demo-user-001"));

            // ── PLAYERS — datos del jugador + sus partidos ────────────────────
            await db.GetContainer("players").UpsertItemAsync(new PlayerDocument
            {
                Id = "player-001", UserId = "demo-user-001",
                Name = "Toni Roig", Nationality = "España", NationalityFlag = "🇪🇸",
                Age = 22, HeightCm = 185, WeightKg = 78,
                DominantHand = "Diestro", PlayingStyle = "Agresivo de Fondo",
                CurrentEnergy = 64, MaxEnergy = 100,
                Physical =
                [
                    new() { Name = "Resistencia",  Value = 68 },
                    new() { Name = "Velocidad",    Value = 74 },
                    new() { Name = "Fuerza",       Value = 81 },
                    new() { Name = "Reflejos",     Value = 65 },
                    new() { Name = "Flexibilidad", Value = 58 },
                ],
                Mental =
                [
                    new() { Name = "Sangre Fría",     Value = 62 },
                    new() { Name = "Concentración",   Value = 70 },
                    new() { Name = "Visión de Juego", Value = 77 },
                    new() { Name = "Anticipación",    Value = 66 },
                    new() { Name = "Creatividad",     Value = 59 },
                ],
                Technical =
                [
                    new() { Name = "Saque",           Value = 71 },
                    new() { Name = "Derecha",         Value = 83 },
                    new() { Name = "Revés",           Value = 60 },
                    new() { Name = "Juego en la Red", Value = 55 },
                    new() { Name = "Efecto",          Value = 75 },
                ],
                Skills =
                [
                    new() { Name = "Hielo en las Venas", IconName = "ac_unit_rounded",
                            Description = "Mejora su Sangre Fría y Saque en bolas de break en contra." },
                    new() { Name = "Matagigantes", IconName = "bolt_rounded",
                            Description = "Impulso temporal contra rivales muy superiores en el ranking." },
                ],
                // Los partidos pertenecen al jugador
                NextMatch = new MatchDoc
                {
                    OpponentName   = "Carlos Ferrer",
                    TournamentName = "Masters de Valencia",
                    Round          = "Octavos de final",
                    DateTime       = "2026-06-22T18:30:00Z",
                    Surface        = "Tierra batida",
                },
                LastMatch = new MatchResultDoc
                {
                    OpponentName   = "Iker Bilbao",
                    Won            = true,
                    SetsScore      = "6-4, 7-5",
                    Aces           = 8,
                    Winners        = 24,
                    UnforcedErrors = 14,
                },
            }, new PartitionKey("demo-user-001"));

            // ── LIGAS EN PIRÁMIDE (3 divisiones) ──────────────────────────────
            // Tier 1: Primera (élite) · Tier 2: Segunda (humano) · Tier 3: Tercera
            await LeagueGenerator.Create(cosmos, "league-primera",
                "Primera División", tier: 1, seed: 1001);

            await LeagueGenerator.Create(cosmos, "league-elite-group-3",
                "Segunda División", tier: 2, seed: 2002,
                humanUserId: "demo-user-001", humanPosition: 11);

            await LeagueGenerator.Create(cosmos, "league-tercera",
                "Tercera División", tier: 3, seed: 3003);

            // ── TOURNAMENTS — calendario completo de 28 días (circuito común) ──
            await db.GetContainer("tournaments").UpsertItemAsync(new TournamentDocument
            {
                Id = "season-2026-01",
                SeasonId = "season-2026-01",
                CurrentDay = 1,
                CurrentDayStartUtc = DateTime.UtcNow.ToString("o"),
                DevTimeOffsetSeconds = 0,
                RoundIntervalMinutes = 120,
                TotalDays = 28,
                Tournaments =
                [
                    new() { Name = "Brisbane", StartDay = 1,  DurationDays = 1, Surface = "Pista dura", Category = "t250", DateLabel = "Día 1" },
                    new() { Name = "Adelaida", StartDay = 2,  DurationDays = 1, Surface = "Tierra batida", Category = "t250", DateLabel = "Día 2" },
                    new() { Name = "Australia", StartDay = 3,  DurationDays = 1, Surface = "Pista dura", Category = "grandSlam", DateLabel = "Día 3" },
                    new() { Name = "Róterdam", StartDay = 4,  DurationDays = 1, Surface = "Tierra batida", Category = "t500", DateLabel = "Día 4" },
                    new() { Name = "Río de Janeiro", StartDay = 5,  DurationDays = 1, Surface = "Pista dura", Category = "t500", DateLabel = "Día 5" },
                    new() { Name = "Acapulco", StartDay = 6,  DurationDays = 1, Surface = "Tierra batida", Category = "t500", DateLabel = "Día 6" },
                    new() { Name = "Indian Wells", StartDay = 7,  DurationDays = 1, Surface = "Hierba", Category = "t1000", DateLabel = "Día 7" },
                    new() { Name = "Miami", StartDay = 8,  DurationDays = 1, Surface = "Tierra batida", Category = "t1000", DateLabel = "Día 8" },
                    new() { Name = "Marrakech", StartDay = 9,  DurationDays = 1, Surface = "Pista dura", Category = "t250", DateLabel = "Día 9" },
                    new() { Name = "Montecarlo", StartDay = 10, DurationDays = 1, Surface = "Hierba", Category = "t1000", DateLabel = "Día 10" },
                    new() { Name = "Barcelona", StartDay = 11, DurationDays = 1, Surface = "Tierra batida", Category = "t500", DateLabel = "Día 11" },
                    new() { Name = "Madrid", StartDay = 12, DurationDays = 1, Surface = "Pista dura", Category = "t1000", DateLabel = "Día 12" },
                    new() { Name = "Roma", StartDay = 13, DurationDays = 1, Surface = "Tierra batida", Category = "t1000", DateLabel = "Día 13" },
                    new() { Name = "Hamburgo", StartDay = 14, DurationDays = 1, Surface = "Pista dura", Category = "t500", DateLabel = "Día 14" },
                    new() { Name = "Roland Garros", StartDay = 15, DurationDays = 1, Surface = "Tierra batida", Category = "grandSlam", DateLabel = "Día 15" },
                    new() { Name = "Londres", StartDay = 16, DurationDays = 1, Surface = "Pista dura", Category = "t500", DateLabel = "Día 16" },
                    new() { Name = "Halle", StartDay = 17, DurationDays = 1, Surface = "Tierra batida", Category = "t500", DateLabel = "Día 17" },
                    new() { Name = "Wimbledon", StartDay = 18, DurationDays = 1, Surface = "Hierba", Category = "grandSlam", DateLabel = "Día 18" },
                    new() { Name = "Washington", StartDay = 19, DurationDays = 1, Surface = "Pista dura", Category = "t500", DateLabel = "Día 19" },
                    new() { Name = "Canada", StartDay = 20, DurationDays = 1, Surface = "Hierba", Category = "t1000", DateLabel = "Día 20" },
                    new() { Name = "Cincinnati", StartDay = 21, DurationDays = 1, Surface = "Tierra batida", Category = "t1000",   DateLabel = "Día 21" },
                    new() { Name = "US", StartDay = 22, DurationDays = 1, Surface = "Pista dura", Category = "grandSlam", DateLabel = "Día 22" },
                    new() { Name = "Tokio", StartDay = 23, DurationDays = 1, Surface = "Tierra batida", Category = "t500", DateLabel = "Día 23" },
                    new() { Name = "Shanghái", StartDay = 24, DurationDays = 1, Surface = "Pista dura", Category = "t1000", DateLabel = "Día 24" },
                    new() { Name = "Marsella", StartDay = 25, DurationDays = 1, Surface = "Tierra batida", Category = "t250", DateLabel = "Día 25" },
                    new() { Name = "Basilea", StartDay = 26, DurationDays = 1, Surface = "Pista dura", Category = "t500", DateLabel = "Día 26" },
                    new() { Name = "París", StartDay = 27, DurationDays = 1, Surface = "Pista dura", Category = "t1000", DateLabel = "Día 27" },
                    new() { Name = "Finales", StartDay = 28, DurationDays = 1, Surface = "Pista dura", Category = "finals", DateLabel = "Día 28" },
                ],
            }, new PartitionKey("season-2026-01"));

            var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync("{\"status\":\"Datos actualizados correctamente\"}");
            return res;
        }
    }
}