using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    public static class ParticipantLoader
    {
        public static async Task<List<Participant>> Load(CosmosClient cosmos, string leagueId)
        {
            var result = new List<Participant>();

            // Bots (una consulta, misma partición)
            var botsContainer = cosmos.GetContainer("TennisManagerDB", "bots");
            var botQuery = new QueryDefinition("SELECT * FROM c");
            var botOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(leagueId) };
            using (var iter = botsContainer.GetItemQueryIterator<BotDocument>(botQuery, requestOptions: botOpts))
            {
                while (iter.HasMoreResults)
                    foreach (var b in await iter.ReadNextAsync())
                        result.Add(new Participant
                        {
                            Id = b.Id, Name = b.Name, Overall = b.Overall, IsHuman = false,
                            Sim = new SimPlayer(b.Name, b.PlayingStyle, AttrsToDict(b.Physical, b.Mental, b.Technical)),
                        });
            }

            // Liga → seeds y humanos
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(leagueId, new PartitionKey(leagueId))).Resource;

            var seedByRef = new Dictionary<string, int>();
            foreach (var s in league.Standings)
            {
                var key = s.UserId ?? s.BotId;
                if (key != null) seedByRef[key] = s.Position;
            }
            foreach (var p in result)
                if (seedByRef.TryGetValue(p.Id, out var seed)) p.Seed = seed;

            // Humanos
            var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
            foreach (var s in league.Standings.Where(x => x.UserId != null))
            {
                var pQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", s.UserId);
                var pOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(s.UserId!) };
                using var iter = playersContainer.GetItemQueryIterator<PlayerDocument>(pQuery, requestOptions: pOpts);
                var page = await iter.ReadNextAsync();
                var doc = page.FirstOrDefault();
                if (doc is null) continue;

                result.Add(new Participant
                {
                    Id = s.UserId!, Name = doc.Name, Seed = s.Position,
                    Overall = AttrsAverage(doc.Physical, doc.Mental, doc.Technical),
                    IsHuman = true,
                    Sim = new SimPlayer(doc.Name, doc.PlayingStyle, AttrsToDict(doc.Physical, doc.Mental, doc.Technical)),
                });
            }

            return result;
        }

        private static Dictionary<string, int> AttrsToDict(List<AttributeDoc> phys, List<AttributeDoc> ment, List<AttributeDoc> tech)
        {
            var d = new Dictionary<string, int>();
            foreach (var a in phys) d[a.Name] = a.Value;
            foreach (var a in ment) d[a.Name] = a.Value;
            foreach (var a in tech) d[a.Name] = a.Value;
            return d;
        }

        private static int AttrsAverage(List<AttributeDoc> phys, List<AttributeDoc> ment, List<AttributeDoc> tech)
        {
            var all = phys.Concat(ment).Concat(tech).ToList();
            return all.Count == 0 ? 50 : (int)Math.Round(all.Average(a => a.Value));
        }
    }
}