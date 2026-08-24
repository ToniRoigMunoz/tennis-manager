using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    public static class LeagueLookup
    {
        // Dado un userId, devuelve su leagueId (leyendo su documento de usuario)
        public static async Task<string> GetLeagueId(CosmosClient cosmos, string userId)
        {
            var users = cosmos.GetContainer("TennisManagerDB", "users");
            var user = (await users.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;
            return user.LeagueId;
        }
    }
}