using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    // Resuelve el torneo diario de una liga SIN humano: simula el cuadro completo
    // de 24 bots y reparte puntos según la ronda alcanzada. Actualiza la clasificación.
    public static class HeadlessLeagueResolver
    {
        public static async Task<(int botsRewarded, int? humanReachedRound)> ResolveDailyTournament(
            CosmosClient cosmos, string leagueId, string category, int seed,
            string? humanUserId = null)
        {
            // 1. Cargar los 24 participantes (bots + humano si lo hay en esta liga)
            var participants = await ParticipantLoader.Load(cosmos, leagueId);
            if (participants.Count < 2) return (0, null);
            participants.Sort((a, b) => a.Seed.CompareTo(b.Seed));

            // 2. Resolver el torneo completo (byes + rondas) hasta el campeón
            var reached = new Dictionary<string, int>();
            var players = new List<Participant>(participants);

            int byeCount = players.Count - LargestPowerOfTwoBelow(players.Count);
            var (playing, byes) = TournamentBracket.ApplyByes(players, byeCount);

            var firstMatches = TournamentBracket.PairRound(playing);
            var advancing = new List<Participant>(byes);
            foreach (var m in firstMatches)
            {
                var (winnerId, _) = TournamentBracket.SimulateFast(m.Player1!, m.Player2!, seed + m.MatchIndex);
                var winner = winnerId == m.Player1!.Id ? m.Player1! : m.Player2!;
                var loser  = winnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                reached[loser.Id] = playing.Count;
                advancing.Add(winner);
            }

            var (champion, finalReached, _) = TournamentOrchestrator.ResolveRemainingFully(
                advancing, seed + 50000, reached);

            // 3. Repartir puntos a los BOTS según la ronda alcanzada
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(leagueId, new PartitionKey(leagueId))).Resource;

            int championBase = TournamentRewards.ChampionPoints(category);
            double champAttrGain = TournamentRewards.ChampionAttributeGain(category);
            int rewarded = 0;

            foreach (var standing in league.Standings.Where(s => !string.IsNullOrEmpty(s.BotId)))
            {
                bool isChampion = champion.Id == standing.BotId;
                int roundReached = isChampion ? 1 : (finalReached.TryGetValue(standing.BotId!, out var r) ? r : 24);
                double fraction = TournamentRewards.PointsFractionByRound(roundReached, isChampion);
                standing.Points += (int)Math.Round(championBase * fraction);
                rewarded++;

                if (isChampion && champAttrGain > 0)
                    await ImproveBotAttributes(cosmos, leagueId, standing.BotId!, champAttrGain);
            }

            // 4. Si hay humano en esta liga, aplicarle SUS recompensas según su ronda
            int? humanReached = null;
            if (humanUserId != null)
            {
                bool humanIsChampion = champion.Id == humanUserId;
                humanReached = humanIsChampion ? 1 : (finalReached.TryGetValue(humanUserId, out var hr) ? hr : 24);

                await ApplyHumanRewards(cosmos, leagueId, humanUserId, category, humanReached.Value, humanIsChampion, league);
            }

            // 5. Reordenar la clasificación (con puntos de bots y humano ya sumados)
            league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
            for (int i = 0; i < league.Standings.Count; i++)
                league.Standings[i].Position = i + 1;

            await leaguesContainer.UpsertItemAsync(league, new PartitionKey(leagueId));
            return (rewarded, humanReached);
        }

        // Aplica al humano puntos (en el league doc en memoria), dinero, descansos y atributos
        private static async Task ApplyHumanRewards(
            CosmosClient cosmos, string leagueId, string userId, string category,
            int reachedRound, bool isChampion, LeagueDocument league)
        {
            int championBase = TournamentRewards.ChampionPoints(category);
            double fraction = TournamentRewards.PointsFractionByRound(reachedRound, isChampion);
            int pointsEarned = (int)Math.Round(championBase * fraction);
            int moneyEarned = TournamentRewards.MoneyFromPoints(pointsEarned);
            int restsEarned = TournamentRewards.RestsByRound(reachedRound, isChampion);
            double attrGain = isChampion ? TournamentRewards.ChampionAttributeGain(category) : 0.0;

            // Puntos: sobre el league doc en memoria (se guardará en el paso 5)
            var myStanding = league.Standings.FirstOrDefault(s => s.UserId == userId);
            if (myStanding != null) myStanding.Points += pointsEarned;

            // Dinero y descansos: en el user doc
            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var userDoc = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;
            userDoc.Money += moneyEarned;
            userDoc.Rests += restsEarned;
            await usersContainer.UpsertItemAsync(userDoc, new PartitionKey(userId));

            // Atributos: en el player doc (solo campeón)
            if (attrGain > 0)
            {
                var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
                var q = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", userId);
                var opts = new QueryRequestOptions { PartitionKey = new PartitionKey(userId) };
                using var iter = playersContainer.GetItemQueryIterator<PlayerDocument>(q, requestOptions: opts);
                var page = await iter.ReadNextAsync();
                var playerDoc = page.FirstOrDefault();
                if (playerDoc != null)
                {
                    playerDoc.AttributeProgress += attrGain;
                    int applied = 0;
                    while (playerDoc.AttributeProgress >= 1.0) { playerDoc.AttributeProgress -= 1.0; applied++; }
                    if (applied > 0)
                    {
                        Bump(playerDoc.Physical, applied);
                        Bump(playerDoc.Mental, applied);
                        Bump(playerDoc.Technical, applied);
                    }
                    await playersContainer.UpsertItemAsync(playerDoc, new PartitionKey(userId));
                }
            }
        }

        private static async Task ImproveBotAttributes(CosmosClient cosmos, string leagueId, string botId, double gain)
        {
            var botsContainer = cosmos.GetContainer("TennisManagerDB", "bots");
            BotDocument bot;
            try { bot = (await botsContainer.ReadItemAsync<BotDocument>(botId, new PartitionKey(leagueId))).Resource; }
            catch (CosmosException) { return; }

            bot.AttributeProgress += gain;
            int applied = 0;
            while (bot.AttributeProgress >= 1.0) { bot.AttributeProgress -= 1.0; applied++; }
            if (applied > 0)
            {
                Bump(bot.Physical, applied); Bump(bot.Mental, applied); Bump(bot.Technical, applied);
                var all = bot.Physical.Concat(bot.Mental).Concat(bot.Technical).ToList();
                bot.Overall = (int)Math.Round(all.Average(a => a.Value));
            }
            await botsContainer.UpsertItemAsync(bot, new PartitionKey(leagueId));
        }

        private static void Bump(List<AttributeDoc> attrs, int amount)
        {
            foreach (var a in attrs) a.Value = Math.Min(a.Value + amount, 99);
        }

        private static int LargestPowerOfTwoBelow(int n)
        {
            int p = 1;
            while (p * 2 <= n) p *= 2;
            return p;
        }
    }
}