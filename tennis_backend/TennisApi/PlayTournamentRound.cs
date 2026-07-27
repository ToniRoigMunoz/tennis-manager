using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class PlayTournamentRound(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("PlayTournamentRound")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var leagueId = req.Query["leagueId"] ?? "league-elite-group-3";
            var seed = int.TryParse(req.Query["seed"], out var sd) ? sd : Random.Shared.Next();

            var sw = Stopwatch.StartNew();

            // 1. Cargar participantes (bots de la liga + humano)
            var participants = await ParticipantLoader.Load(cosmos, leagueId);
            var loadMs = sw.ElapsedMilliseconds;

            // 2. Ordenar por seed (ranking) y aplicar byes (24 → 8 byes, 16 juegan)
            participants.Sort((a, b) => a.Seed.CompareTo(b.Seed));
            int byeCount = participants.Count - LargestPowerOfTwoBelow(participants.Count);
            var (playing, byes) = TournamentBracket.ApplyByes(participants, byeCount);

            // 3. Emparejar y resolver la primera ronda en paralelo
            var matches = TournamentBracket.PairRound(playing);
            var advancing = await TournamentBracket.ResolveRoundParallel(matches, seed, skipHuman: false);
            var simMs = sw.ElapsedMilliseconds - loadMs;

            // 4. Los que pasan de ronda = ganadores + los que tenían bye
            var nextRound = new List<Participant>();
            nextRound.AddRange(byes);
            nextRound.AddRange(advancing);
            nextRound.Sort((a, b) => a.Seed.CompareTo(b.Seed));

            var response = new
            {
                leagueId,
                totalParticipants = participants.Count,
                byeCount,
                firstRoundMatches = matches.Count,
                roundName = TournamentBracket.RoundName(playing.Count),
                advancingCount = nextRound.Count,
                timings = new { loadMs, simMs, totalMs = sw.ElapsedMilliseconds },
                matches = matches.Select(m => new
                {
                    p1 = m.Player1!.Name,
                    p2 = m.Player2!.Name,
                    winner = m.WinnerId == m.Player1!.Id ? m.Player1!.Name : m.Player2!.Name,
                    score = m.SetsScore,
                    involvesHuman = m.InvolvesHuman,
                }),
                nextRound = nextRound.Select(p => new { p.Name, p.Seed, p.IsHuman }),
            };

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(response, Opts));
            return res;
        }

        private static int LargestPowerOfTwoBelow(int n)
        {
            int p = 1;
            while (p * 2 <= n) p *= 2;
            return p;
        }
    }
}