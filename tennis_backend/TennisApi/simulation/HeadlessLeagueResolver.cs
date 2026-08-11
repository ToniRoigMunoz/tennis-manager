using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    // Resuelve el torneo diario de una liga SIN humano: simula el cuadro completo
    // de 24 bots y reparte puntos según la ronda alcanzada. Actualiza la clasificación.
    public static class HeadlessLeagueResolver
    {
        public static async Task<int> ResolveDailyTournament(
            CosmosClient cosmos, string leagueId, string category, int seed)
        {
            // 1. Cargar los 24 participantes (todos bots en estas ligas)
            var participants = await ParticipantLoader.Load(cosmos, leagueId);
            if (participants.Count < 2) return 0;
            participants.Sort((a, b) => a.Seed.CompareTo(b.Seed));

            // 2. Resolver el torneo completo (byes + rondas) hasta el campeón
            var reached = new Dictionary<string, int>();
            var players = new List<Participant>(participants);

            // Aplicar byes si no es potencia de dos
            int byeCount = players.Count - LargestPowerOfTwoBelow(players.Count);
            var (playing, byes) = TournamentBracket.ApplyByes(players, byeCount);

            // Primera ronda (con byes al margen)
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

            // Rondas siguientes: resolver de golpe con el orquestador
            var (champion, finalReached, _) = TournamentOrchestrator.ResolveRemainingFully(
                advancing, seed + 50000, reached);

            // 3. Repartir puntos según la ronda alcanzada
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

            // 4. Reordenar la clasificación
            league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
            for (int i = 0; i < league.Standings.Count; i++)
                league.Standings[i].Position = i + 1;

            await leaguesContainer.UpsertItemAsync(league, new PartitionKey(leagueId));
            return rewarded;
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