using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class AdvanceDay(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("AdvanceDay")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                var userId = req.Query["userId"] ?? "demo-user-001";
                var sw = Stopwatch.StartNew();

                // 1. Cargar usuario → liga y temporada
                var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
                var user = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;

                // 2. Cargar el torneo activo del día (el que jugó el humano)
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(userId, new PartitionKey(userId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null;
                }

                int botsRewarded = 0;
                string? distributionNote = null;

                // 3. Repartir puntos a los bots SOLO si el torneo del día terminó
                if (state != null && state.Finished)
                {
                    botsRewarded = await DistributeBotRewards(state);
                }
                else
                {
                    // Caso torneo a medias / sin jugar: auto-resolución pendiente (siguiente capa)
                    distributionNote = state == null
                        ? "No había torneo activo hoy; no se repartieron puntos a bots."
                        : "El torneo del día no estaba terminado; auto-resolución pendiente (no se repartieron puntos).";
                }

                // 4. Incrementar el día de la temporada
                var toursContainer = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursContainer.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;
                int previousDay = season.CurrentDay;
                if (season.CurrentDay < season.TotalDays)
                    season.CurrentDay++;
                await toursContainer.UpsertItemAsync(season, new PartitionKey(user.SeasonId));

                // 5. Limpiar el torneo del día (para que mañana se cree uno nuevo)
                if (state != null)
                {
                    try
                    {
                        await atContainer.DeleteItemAsync<ActiveTournamentDoc>(userId, new PartitionKey(userId));
                    }
                    catch (CosmosException) { /* ya no existe, sin problema */ }
                }

                var result = new
                {
                    status = "dayAdvanced",
                    previousDay,
                    newDay = season.CurrentDay,
                    totalDays = season.TotalDays,
                    seasonFinished = season.CurrentDay >= season.TotalDays,
                    botsRewarded,
                    distributionNote,
                    elapsedMs = sw.ElapsedMilliseconds,
                };

                var res = req.CreateResponse(HttpStatusCode.OK);
                res.Headers.Add("Content-Type", "application/json");
                await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
                return res;
            }
            catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                err.Headers.Add("Content-Type", "application/json");
                await err.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message, stack = ex.StackTrace }));
                return err;
            }
        }

        // Reparte puntos (y mejora de atributos al campeón) a los bots de la liga,
        // según la ronda que alcanzó cada uno en el torneo del día. Recalcula la clasificación.
        private async Task<int> DistributeBotRewards(ActiveTournamentDoc state)
        {
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;

            int championBase = TournamentRewards.ChampionPoints(state.Category);
            double champAttrGain = TournamentRewards.ChampionAttributeGain(state.Category);
            int rewarded = 0;

            // ID del bot campeón (si el campeón es un bot)
            string? championBotId = state.ChampionId;

            foreach (var standing in league.Standings.Where(s => !string.IsNullOrEmpty(s.BotId)))
            {
                if (!state.ReachedRound.TryGetValue(standing.BotId!, out var reached)) continue;

                bool isChampion = championBotId == standing.BotId;
                double fraction = TournamentRewards.PointsFractionByRound(reached, isChampion);
                int points = (int)Math.Round(championBase * fraction);
                standing.Points += points;
                rewarded++;

                // Mejora de atributos SOLO al bot campeón (acumulación fraccionada, igual que el humano)
                if (isChampion && champAttrGain > 0)
                {
                    await ImproveBotAttributes(state.LeagueId, standing.BotId!, champAttrGain);
                }
            }

            // Recalcular la clasificación: ordenar por puntos y reasignar posiciones
            league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
            for (int i = 0; i < league.Standings.Count; i++)
                league.Standings[i].Position = i + 1;

            await leaguesContainer.UpsertItemAsync(league, new PartitionKey(state.LeagueId));
            return rewarded;
        }

        // Sube atributos de un bot con acumulación fraccionada (igual que el humano)
        private async Task ImproveBotAttributes(string leagueId, string botId, double gain)
        {
            var botsContainer = cosmos.GetContainer("TennisManagerDB", "bots");
            BotDocument bot;
            try
            {
                bot = (await botsContainer.ReadItemAsync<BotDocument>(botId, new PartitionKey(leagueId))).Resource;
            }
            catch (CosmosException) { return; }

            bot.AttributeProgress += gain;
            int applied = 0;
            while (bot.AttributeProgress >= 1.0)
            {
                bot.AttributeProgress -= 1.0;
                applied++;
            }
            if (applied > 0)
            {
                BumpAll(bot.Physical, applied);
                BumpAll(bot.Mental, applied);
                BumpAll(bot.Technical, applied);
                // Recalcular overall como media de los 15 atributos
                var all = bot.Physical.Concat(bot.Mental).Concat(bot.Technical).ToList();
                bot.Overall = (int)Math.Round(all.Average(a => a.Value));
            }
            await botsContainer.UpsertItemAsync(bot, new PartitionKey(leagueId));
        }

        private static void BumpAll(List<AttributeDoc> attrs, int amount)
        {
            foreach (var a in attrs)
                a.Value = Math.Min(a.Value + amount, 99);
        }
    }
}