using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    public static class LeagueGenerator
    {
        // Rango de overall por tier: tier 1 (élite) más fuerte, tier 3 más débil
        private static (int min, int max) OverallRange(int tier) => tier switch
        {
            1 => (72, 86),
            2 => (58, 74),
            3 => (44, 60),
            _ => (58, 74),
        };

        // Puntos iniciales por posición (para dar un ranking de partida coherente)
        private static int InitialPoints(int position) => 5000 - (position - 1) * 90;

        // Crea una liga de bots. Si humanUserId != null, reserva humanPosition para el humano.
        public static async Task Create(
            CosmosClient cosmos, string leagueId, string leagueName, int tier,
            int seed, string? humanUserId = null, int humanPosition = 0)
        {
            var db = cosmos.GetDatabase("TennisManagerDB");
            var botsContainer = db.GetContainer("bots");
            var rng = new Random(seed);
            var (minOv, maxOv) = OverallRange(tier);

            // Necesitamos 24 identidades (menos 1 si hay humano)
            int botCount = humanUserId != null ? 23 : 24;
            var identities = BotNameGenerator.Generate(botCount, rng);

            var standings = new List<StandingDoc>();
            int identityIdx = 0;

            for (int position = 1; position <= 24; position++)
            {
                if (humanUserId != null && position == humanPosition)
                {
                    // Hueco del humano (sus datos reales vienen de players)
                    standings.Add(new StandingDoc
                    {
                        Position = position,
                        Name = "(humano)", // se resuelve en lectura vía GetLeagueData
                        Points = InitialPoints(position),
                        UserId = humanUserId,
                        RecentForm = [rng.Next(2) == 1, rng.Next(2) == 1, rng.Next(2) == 1],
                    });
                    continue;
                }

                var (name, nat, flag) = identities[identityIdx++];
                var botId = $"bot-{leagueId}-{position:D2}";

                // Overall según posición dentro del tier: mejor posición → mejor overall
                double t = (position - 1) / 23.0; // 0 en pos 1, 1 en pos 24
                int overall = (int)Math.Round(maxOv - t * (maxOv - minOv));

                var bot = BotFactory.Create(leagueId, botId, name, nat, flag, overall, seed: seed + position * 7919);
                await botsContainer.UpsertItemAsync(bot, new PartitionKey(leagueId));

                standings.Add(new StandingDoc
                {
                    Position = position,
                    Name = name,
                    Points = InitialPoints(position),
                    BotId = botId,
                    RecentForm = [rng.Next(2) == 1, rng.Next(2) == 1, rng.Next(2) == 1],
                });
            }

            await db.GetContainer("leagues").UpsertItemAsync(new LeagueDocument
            {
                Id = leagueId,
                LeagueId = leagueId,
                Name = leagueName,
                Tier = tier,
                TotalPlayers = 24,
                QualificationSlots = 8,
                SeasonEndsLabel = "Termina en 28 días",
                Standings = standings,
            }, new PartitionKey(leagueId));
        }
    }
}