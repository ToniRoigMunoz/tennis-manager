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

            // ── BOTS + LEAGUE ─────────────────────────────────────────────────
            const string leagueId = "league-elite-group-3";

            // (nombre, nacionalidad, bandera, puntos, forma reciente)
            var roster = new (string Name, string Nat, string Flag, int Points, bool[] Form)[]
            {
                ("Marc Aguilar",     "España",   "🇪🇸", 4820, [true,  true,  true ]),
                ("Núria Castell",    "España",   "🇪🇸", 4690, [true,  false, true ]),
                ("Iker Bilbao",      "España",   "🇪🇸", 4490, [true,  true,  false]),
                ("Pau Soler",        "España",   "🇪🇸", 4205, [false, true,  true ]),
                ("Diego Roma",       "Italia",   "🇮🇹", 4102, [true,  false, false]),
                ("Laura Vidal",      "España",   "🇪🇸", 3980, [false, true,  true ]),
                ("Hugo Prats",       "Francia",  "🇫🇷", 3850, [true,  true,  true ]),
                ("Mireia Costa",     "España",   "🇪🇸", 3780, [true,  false, true ]),
                ("Adrián Ruiz",      "España",   "🇪🇸", 3720, [false, false, true ]),
                ("Sara Llopis",      "España",   "🇪🇸", 3680, [true,  false, false]),
                // ── posición 11: jugador humano ──
                ("Bruno Ferrer",     "España",   "🇪🇸", 3590, [false, false, true ]),
                ("Clara Munté",      "España",   "🇪🇸", 3520, [false, true,  false]),
                ("Toni Beltrán",     "España",   "🇪🇸", 3470, [true,  false, false]),
                ("Eva Domingo",      "Portugal", "🇵🇹", 3410, [false, true,  false]),
                ("Raúl Esteve",      "España",   "🇪🇸", 3350, [false, false, true ]),
                ("Marina Soto",      "Argentina","🇦🇷", 3290, [true,  true,  false]),
                ("Jordi Pla",        "España",   "🇪🇸", 3230, [false, false, false]),
                ("Lucía Ferrando",   "España",   "🇪🇸", 3170, [true,  false, true ]),
                ("Pablo Sanchís",    "España",   "🇪🇸", 3100, [false, true,  false]),
                ("Andrea Gil",       "España",   "🇪🇸", 3040, [false, false, true ]),
                ("Víctor Calatayud", "España",   "🇪🇸", 2980, [true,  false, false]),
                ("Carla Mora",       "Brasil",   "🇧🇷", 2920, [false, false, false]),
                ("Òscar Beneyto",    "España",   "🇪🇸", 2860, [false, true,  false]),
            };

            var botsContainer = db.GetContainer("bots");
            var standings = new List<StandingDoc>();
            int rosterIdx = 0;

            for (int position = 1; position <= 24; position++)
            {
                // El puesto 11 lo ocupa el jugador humano
                if (position == 11)
                {
                    standings.Add(new StandingDoc
                    {
                        Position = 11,
                        Name = "Toni Roig",
                        Points = 3640,
                        UserId = "demo-user-001",
                        RecentForm = [false, true, false],
                    });
                    continue;
                }

                var r = roster[rosterIdx++];
                var botId = $"bot-{leagueId}-{position:D2}";

                // Nivel según posición: 82 en el nº1, ~54 en el nº24
                var overall = (int)Math.Round(82 - (position - 1) * 1.22);

                var bot = BotFactory.Create(
                    leagueId, botId, r.Name, r.Nat, r.Flag, overall,
                    seed: position * 7919); // determinista: mismo bot en cada reseed

                await botsContainer.UpsertItemAsync(bot, new PartitionKey(leagueId));

                standings.Add(new StandingDoc
                {
                    Position = position,
                    Name = r.Name,
                    Points = r.Points,
                    BotId = botId,
                    RecentForm = [.. r.Form],
                });
            }

            await db.GetContainer("leagues").UpsertItemAsync(new LeagueDocument
            {
                Id = leagueId,
                LeagueId = leagueId,
                Name = "Liga Élite · Grupo 3",
                TotalPlayers = 24,
                QualificationSlots = 8,
                SeasonEndsLabel = "Termina en 12 días",
                Standings = standings,
            }, new PartitionKey(leagueId));

            // ── TOURNAMENTS — calendario completo de 28 días (circuito común) ──
            await db.GetContainer("tournaments").UpsertItemAsync(new TournamentDocument
            {
                Id = "season-2026-01",
                SeasonId = "season-2026-01",
                CurrentDay = 1,
                TotalDays = 28,
                Tournaments =
                [
                    new() { Name = "Brisbane (250)", StartDay = 1,  DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 1" },
                    new() { Name = "Adelaida (250)", StartDay = 2,  DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 2" },
                    new() { Name = "Australia (2000)", StartDay = 3,  DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 3" },
                    new() { Name = "Róterdam (500)", StartDay = 4,  DurationDays = 1, Surface = "Tierra batida", Category = "grandSlam", DateLabel = "Día 4" },
                    new() { Name = "Río de Janeiro (500)", StartDay = 5,  DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 5" },
                    new() { Name = "Acapulco (500)", StartDay = 6,  DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 6" },
                    new() { Name = "Indian Wells (1000)", StartDay = 7,  DurationDays = 1, Surface = "Hierba",        Category = "regular",   DateLabel = "Día 7" },
                    new() { Name = "Miami (1000)", StartDay = 8,  DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 8" },
                    new() { Name = "Marrakech (250)", StartDay = 9,  DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 9" },
                    new() { Name = "Montecarlo (1000)", StartDay = 10, DurationDays = 1, Surface = "Hierba",        Category = "grandSlam", DateLabel = "Día 10" },
                    new() { Name = "Barcelona (500)", StartDay = 11, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 11" },
                    new() { Name = "Madrid (1000)", StartDay = 12, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 12" },
                    new() { Name = "Roma (1000)", StartDay = 13, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 13" },
                    new() { Name = "Hamburgo (500)", StartDay = 14, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 14" },
                    new() { Name = "Roland Garros (2000)", StartDay = 15, DurationDays = 1, Surface = "Tierra batida", Category = "grandSlam", DateLabel = "Día 15" },
                    new() { Name = "Londres (500)", StartDay = 16, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 16" },
                    new() { Name = "Halle (500)", StartDay = 17, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 17" },
                    new() { Name = "Wimbledon (2000)", StartDay = 18, DurationDays = 1, Surface = "Hierba",        Category = "regular",   DateLabel = "Día 18" },
                    new() { Name = "Washington (500)", StartDay = 19, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 19" },
                    new() { Name = "Canada (1000)", StartDay = 20, DurationDays = 1, Surface = "Hierba",        Category = "grandSlam", DateLabel = "Día 20" },
                    new() { Name = "Cincinnati (1000)", StartDay = 21, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 21" },
                    new() { Name = "US (2000)", StartDay = 22, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 22" },
                    new() { Name = "Tokio (500)", StartDay = 23, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 23" },
                    new() { Name = "Shanghái (1000)", StartDay = 24, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 24" },
                    new() { Name = "Marsella (250)", StartDay = 25, DurationDays = 1, Surface = "Tierra batida", Category = "regular",   DateLabel = "Día 25" },
                    new() { Name = "Basilea (500)", StartDay = 26, DurationDays = 1, Surface = "Pista dura",    Category = "grandSlam", DateLabel = "Día 26" },
                    new() { Name = "París (1000)", StartDay = 27, DurationDays = 1, Surface = "Pista dura",    Category = "regular",   DateLabel = "Día 27" },
                    new() { Name = "Finales (1500)", StartDay = 28, DurationDays = 1, Surface = "Pista dura",    Category = "finals",    DateLabel = "Día 28" },
                ],
            }, new PartitionKey("season-2026-01"));

            var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync("{\"status\":\"Datos actualizados correctamente\"}");
            return res;
        }
    }
}