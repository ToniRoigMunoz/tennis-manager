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

            // ── USERS ────────────────────────────────────────────────────────
            await db.GetContainer("users").UpsertItemAsync(new UserDocument
            {
                Id = "demo-user-001",
                Money = 1250,
                Rests = 3,
                LeagueId = "league-elite-group-3",
                SeasonId = "season-2026-01",
                NextMatch = new MatchDoc
                {
                    OpponentName = "Carlos Ferrer",
                    TournamentName = "Masters de Valencia",
                    Round = "Octavos de final",
                    DateTime = "2026-06-22T18:30:00Z",
                    Surface = "Tierra batida"
                },
                LastMatch = new MatchResultDoc
                {
                    OpponentName = "Iker Bilbao",
                    Won = true,
                    SetsScore = "6-4, 7-5",
                    Aces = 8,
                    Winners = 24,
                    UnforcedErrors = 14
                }
            }, new PartitionKey("demo-user-001"));

            // ── PLAYERS ──────────────────────────────────────────────────────
            await db.GetContainer("players").UpsertItemAsync(new PlayerDocument
            {
                Id = "player-001",
                UserId = "demo-user-001",
                Name = "Toni Roig",
                Nationality = "España",
                NationalityFlag = "🇪🇸",
                Age = 22, HeightCm = 185, WeightKg = 78,
                DominantHand = "Diestro",
                PlayingStyle = "Agresivo de Fondo",
                CurrentEnergy = 64, MaxEnergy = 100,
                Physical =
                [
                    new() { Name = "Resistencia", Value = 68 },
                    new() { Name = "Velocidad",   Value = 74 },
                    new() { Name = "Fuerza",      Value = 81 },
                    new() { Name = "Reflejos",    Value = 65 },
                    new() { Name = "Flexibilidad",Value = 58 }
                ],
                Mental =
                [
                    new() { Name = "Sangre Fría",     Value = 62 },
                    new() { Name = "Concentración",   Value = 70 },
                    new() { Name = "Visión de Juego", Value = 77 },
                    new() { Name = "Anticipación",    Value = 66 },
                    new() { Name = "Creatividad",     Value = 59 }
                ],
                Technical =
                [
                    new() { Name = "Saque",          Value = 71 },
                    new() { Name = "Derecha",        Value = 83 },
                    new() { Name = "Revés",          Value = 60 },
                    new() { Name = "Juego en la Red",Value = 55 },
                    new() { Name = "Efecto",         Value = 75 }
                ],
                Skills =
                [
                    new() { Name = "Hielo en las Venas", IconName = "ac_unit_rounded",
                            Description = "Mejora su Sangre Fría y Saque en bolas de break en contra." },
                    new() { Name = "Matagigantes", IconName = "bolt_rounded",
                            Description = "Impulso temporal contra rivales muy superiores en el ranking." }
                ]
            }, new PartitionKey("demo-user-001"));

            // ── LEAGUES ──────────────────────────────────────────────────────
            await db.GetContainer("leagues").UpsertItemAsync(new LeagueDocument
            {
                Id = "league-elite-group-3",
                LeagueId = "league-elite-group-3",
                Name = "Liga Élite · Grupo 3",
                TotalPlayers = 24,
                QualificationSlots = 8,
                SeasonEndsLabel = "Termina en 12 días",
                Standings =
                [
                    new() { Position = 1,  Name = "Marc Aguilar",     Points = 4820, RecentForm = [true,  true,  true]  },
                    new() { Position = 2,  Name = "Núria Castell",    Points = 4690, RecentForm = [true,  false, true]  },
                    new() { Position = 3,  Name = "Iker Bilbao",      Points = 4490, RecentForm = [true,  true,  false] },
                    new() { Position = 4,  Name = "Pau Soler",        Points = 4205, RecentForm = [false, true,  true]  },
                    new() { Position = 5,  Name = "Diego Roma",       Points = 4102, RecentForm = [true,  false, false] },
                    new() { Position = 6,  Name = "Laura Vidal",      Points = 3980, RecentForm = [false, true,  true]  },
                    new() { Position = 7,  Name = "Hugo Prats",       Points = 3850, RecentForm = [true,  true,  true]  },
                    new() { Position = 8,  Name = "Mireia Costa",     Points = 3780, RecentForm = [true,  false, true]  },
                    new() { Position = 9,  Name = "Adrián Ruiz",      Points = 3720, RecentForm = [false, false, true]  },
                    new() { Position = 10, Name = "Sara Llopis",      Points = 3680, RecentForm = [true,  false, false] },
                    new() { Position = 11, Name = "Toni Roig", UserId = "demo-user-001",
                            Points = 3640, RecentForm = [false, true, false] },
                    new() { Position = 12, Name = "Bruno Ferrer",     Points = 3590, RecentForm = [false, false, true]  },
                    new() { Position = 13, Name = "Clara Munté",      Points = 3520, RecentForm = [false, true,  false] },
                    new() { Position = 14, Name = "Toni Beltrán",     Points = 3470, RecentForm = [true,  false, false] },
                    new() { Position = 15, Name = "Eva Domingo",      Points = 3410, RecentForm = [false, true,  false] },
                    new() { Position = 16, Name = "Raúl Esteve",      Points = 3350, RecentForm = [false, false, true]  },
                    new() { Position = 17, Name = "Marina Soto",      Points = 3290, RecentForm = [true,  true,  false] },
                    new() { Position = 18, Name = "Jordi Pla",        Points = 3230, RecentForm = [false, false, false] },
                    new() { Position = 19, Name = "Lucía Ferrando",   Points = 3170, RecentForm = [true,  false, true]  },
                    new() { Position = 20, Name = "Pablo Sanchís",    Points = 3100, RecentForm = [false, true,  false] },
                    new() { Position = 21, Name = "Andrea Gil",       Points = 3040, RecentForm = [false, false, true]  },
                    new() { Position = 22, Name = "Víctor Calatayud", Points = 2980, RecentForm = [true,  false, false] },
                    new() { Position = 23, Name = "Carla Mora",       Points = 2920, RecentForm = [false, false, false] },
                    new() { Position = 24, Name = "Òscar Beneyto",    Points = 2860, RecentForm = [false, true,  false] }
                ]
            }, new PartitionKey("league-elite-group-3"));

            // ── TOURNAMENTS ──────────────────────────────────────────────────
            await db.GetContainer("tournaments").UpsertItemAsync(new TournamentDocument
            {
                Id = "season-2026-01",
                SeasonId = "season-2026-01",
                CurrentDay = 9,
                TotalDays = 28,
                Tournaments =
                [
                    new() { Name = "Open de Castilla",       StartDay = 2,  DurationDays = 1, Surface = "Pista dura",    Category = "regular",   Status = "past",     DateLabel = "15 jun",    ResultLabel = "Eliminado en 2ª ronda" },
                    new() { Name = "Grand Slam Roland Sur",  StartDay = 5,  DurationDays = 2, Surface = "Tierra batida", Category = "grandSlam", Status = "past",     DateLabel = "18-19 jun", ResultLabel = "Cuartos de final" },
                    new() { Name = "Masters de Valencia",    StartDay = 9,  DurationDays = 1, Surface = "Tierra batida", Category = "regular",   Status = "current",  DateLabel = "22 jun" },
                    new() { Name = "Open de Madrid",         StartDay = 16, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   Status = "upcoming", DateLabel = "29 jun" },
                    new() { Name = "Copa Mediterráneo",      StartDay = 23, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   Status = "upcoming", DateLabel = "6 jul" },
                    new() { Name = "Grand Slam Costa Azul",  StartDay = 25, DurationDays = 2, Surface = "Hierba",        Category = "grandSlam", Status = "upcoming", DateLabel = "8-9 jul" },
                    new() { Name = "Finales ATP",            StartDay = 27, DurationDays = 2, Surface = "Pista dura",    Category = "finals",    Status = "upcoming", DateLabel = "10-11 jul" }
                ]
            }, new PartitionKey("season-2026-01"));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync("{\"status\":\"Datos de demo insertados correctamente\"}");
            return res;
        }
    }
}