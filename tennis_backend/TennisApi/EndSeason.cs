using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    public static class EndSeason
    {
        private const double BaseGain = 7.0;

        // Factor por posición: 1.0 en el 1º, ~0.14 en el 24º
        private static double PositionFactor(int position) => 1.0 - (position - 1) / 24.0 * 0.86;

        // Mejora un atributo con rendimiento decreciente (los altos casi no suben)
        private static int ImproveAttr(int value, double pf)
        {
            double inc = BaseGain * pf * (100 - value) / 100.0;
            return Math.Min(value + (int)Math.Round(inc), 99);
        }

        public static async Task<object> Run(CosmosClient cosmos, string humanUserId)
        {
            var db = cosmos.GetDatabase("TennisManagerDB");
            var leaguesC = db.GetContainer("leagues");
            var usersC = db.GetContainer("users");

            var primera = (await leaguesC.ReadItemAsync<LeagueDocument>("league-primera", new PartitionKey("league-primera"))).Resource;
            var segunda = (await leaguesC.ReadItemAsync<LeagueDocument>("league-elite-group-3", new PartitionKey("league-elite-group-3"))).Resource;
            var tercera = (await leaguesC.ReadItemAsync<LeagueDocument>("league-tercera", new PartitionKey("league-tercera"))).Resource;

            foreach (var lg in new[] { primera, segunda, tercera })
            {
                lg.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
                for (int i = 0; i < lg.Standings.Count; i++) lg.Standings[i].Position = i + 1;
            }

            var champions = new
            {
                primera = primera.Standings[0].Name,
                segunda = segunda.Standings[0].Name,
                tercera = tercera.Standings[0].Name,
            };

            // PASO 1: mejora de atributos (todos, por posición) + cache de overalls
            var overallByRef = new Dictionary<string, int>();
            foreach (var lg in new[] { primera, segunda, tercera })
                foreach (var s in lg.Standings)
                    await ImprovePlayer(cosmos, lg.LeagueId, s, overallByRef);

            // PASO 2: nuevos rosters (10 en la frontera de arriba, 8 en la de abajo)
            var segundaPromote  = segunda.Standings.Take(10).ToList();          // → Primera
            var segundaStay     = segunda.Standings.Skip(10).Take(6).ToList();  // 11-16 se quedan
            var segundaRelegate = segunda.Standings.Skip(16).Take(8).ToList();  // → Tercera

            var primeraStay     = primera.Standings.Take(14).ToList();
            var primeraRelegate = primera.Standings.Skip(14).Take(10).ToList(); // → Segunda

            var terceraPromote  = tercera.Standings.Take(8).ToList();           // → Segunda
            var terceraStay     = tercera.Standings.Skip(8).Take(16).ToList();

            var newPrimera = new List<StandingDoc>(); newPrimera.AddRange(primeraStay); newPrimera.AddRange(segundaPromote);
            var newSegunda = new List<StandingDoc>(); newSegunda.AddRange(segundaStay); newSegunda.AddRange(primeraRelegate); newSegunda.AddRange(terceraPromote);
            var newTercera = new List<StandingDoc>(); newTercera.AddRange(terceraStay); newTercera.AddRange(segundaRelegate);

            // PASO 3: mover bots de partición y actualizar liga del humano
            await MoveGroup(cosmos, segundaPromote,  "league-elite-group-3", "league-primera",       humanUserId, usersC);
            await MoveGroup(cosmos, segundaRelegate, "league-elite-group-3", "league-tercera",       humanUserId, usersC);
            await MoveGroup(cosmos, primeraRelegate, "league-primera",       "league-elite-group-3", humanUserId, usersC);
            await MoveGroup(cosmos, terceraPromote,  "league-tercera",       "league-elite-group-3", humanUserId, usersC);

            // PASO 4: reconstruir standings (0 puntos, orden por overall)
            RebuildLeague(primera, newPrimera, overallByRef);
            RebuildLeague(segunda, newSegunda, overallByRef);
            RebuildLeague(tercera, newTercera, overallByRef);

            await leaguesC.UpsertItemAsync(primera, new PartitionKey("league-primera"));
            await leaguesC.UpsertItemAsync(segunda, new PartitionKey("league-elite-group-3"));
            await leaguesC.UpsertItemAsync(tercera, new PartitionKey("league-tercera"));

            string humanMovement = "se mantiene en su división";
            if (segundaPromote.Any(s => s.UserId == humanUserId)) humanMovement = "asciende a Primera División";
            else if (segundaRelegate.Any(s => s.UserId == humanUserId)) humanMovement = "desciende a Tercera División";
            // (si el humano no estaba en Segunda, este resumen no aplica; se mantiene por defecto)

            return new { champions, humanMovement };
        }

        private static async Task ImprovePlayer(CosmosClient cosmos, string leagueId, StandingDoc s, Dictionary<string, int> cache)
        {
            double pf = PositionFactor(s.Position);
            if (!string.IsNullOrEmpty(s.BotId))
            {
                var botsC = cosmos.GetContainer("TennisManagerDB", "bots");
                BotDocument bot;
                try { bot = (await botsC.ReadItemAsync<BotDocument>(s.BotId, new PartitionKey(leagueId))).Resource; }
                catch (CosmosException) { return; }
                ImproveList(bot.Physical, pf); ImproveList(bot.Mental, pf); ImproveList(bot.Technical, pf);
                bot.Overall = OverallOf(bot.Physical, bot.Mental, bot.Technical);
                await botsC.UpsertItemAsync(bot, new PartitionKey(leagueId));
                cache[s.BotId!] = bot.Overall;
            }
            else if (!string.IsNullOrEmpty(s.UserId))
            {
                var playersC = cosmos.GetContainer("TennisManagerDB", "players");
                var q = new QueryDefinition("SELECT * FROM c WHERE c.userId=@u").WithParameter("@u", s.UserId);
                var opts = new QueryRequestOptions { PartitionKey = new PartitionKey(s.UserId!) };
                using var it = playersC.GetItemQueryIterator<PlayerDocument>(q, requestOptions: opts);
                var page = await it.ReadNextAsync();
                var pd = page.FirstOrDefault();
                if (pd == null) return;
                ImproveList(pd.Physical, pf); ImproveList(pd.Mental, pf); ImproveList(pd.Technical, pf);
                await playersC.UpsertItemAsync(pd, new PartitionKey(s.UserId!));
                cache[s.UserId!] = OverallOf(pd.Physical, pd.Mental, pd.Technical);
            }
        }

        private static void ImproveList(List<AttributeDoc> attrs, double pf)
        {
            foreach (var a in attrs) a.Value = ImproveAttr(a.Value, pf);
        }

        private static int OverallOf(List<AttributeDoc> p, List<AttributeDoc> m, List<AttributeDoc> t)
            => (int)Math.Round(p.Concat(m).Concat(t).Average(a => a.Value));

        private static async Task MoveGroup(
            CosmosClient cosmos, List<StandingDoc> group, string fromLeague, string toLeague,
            string humanUserId, Container usersC)
        {
            var botsC = cosmos.GetContainer("TennisManagerDB", "bots");
            foreach (var s in group)
            {
                if (!string.IsNullOrEmpty(s.BotId))
                {
                    BotDocument bot;
                    try { bot = (await botsC.ReadItemAsync<BotDocument>(s.BotId, new PartitionKey(fromLeague))).Resource; }
                    catch (CosmosException) { continue; }
                    bot.LeagueId = toLeague;
                    await botsC.UpsertItemAsync(bot, new PartitionKey(toLeague));
                    await botsC.DeleteItemAsync<BotDocument>(s.BotId, new PartitionKey(fromLeague));
                }
                else if (s.UserId == humanUserId)
                {
                    var u = (await usersC.ReadItemAsync<UserDocument>(humanUserId, new PartitionKey(humanUserId))).Resource;
                    u.LeagueId = toLeague;
                    await usersC.UpsertItemAsync(u, new PartitionKey(humanUserId));
                }
            }
        }

        private static void RebuildLeague(LeagueDocument league, List<StandingDoc> roster, Dictionary<string, int> overallByRef)
        {
            foreach (var s in roster) { s.Points = 0; s.RecentForm = [false, false, false]; }
            roster.Sort((a, b) =>
            {
                int oa = overallByRef.GetValueOrDefault(a.UserId ?? a.BotId ?? "", 50);
                int ob = overallByRef.GetValueOrDefault(b.UserId ?? b.BotId ?? "", 50);
                return ob.CompareTo(oa);
            });
            for (int i = 0; i < roster.Count; i++) roster[i].Position = i + 1;
            league.Standings = roster;
        }
    }
}