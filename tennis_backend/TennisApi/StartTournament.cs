using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class StartTournament(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("StartTournament")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                var userId = req.Query["userId"] ?? "demo-user-001";
            var seed = int.TryParse(req.Query["seed"], out var sd) ? sd : Random.Shared.Next();

            // 1. Datos de usuario → liga y torneo actual
            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var user = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;

            var toursContainer = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await toursContainer.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;
            var todayTournament = season.Tournaments.FirstOrDefault(t => t.Status == "current")
                                  ?? season.Tournaments.First(t => t.StartDay == season.CurrentDay);

            // 2. Cargar los 24 participantes de la liga
            var participants = await ParticipantLoader.Load(cosmos, user.LeagueId);
            participants.Sort((a, b) => a.Seed.CompareTo(b.Seed));

            // 3. Byes + primera ronda saltando al humano
            int byeCount = participants.Count - LargestPowerOfTwoBelow(participants.Count);
            var (playing, byes) = TournamentBracket.ApplyByes(participants, byeCount);

            var (matches, humanMatch, advancingNonHuman) =
                TournamentOrchestrator.ResolveRoundSkippingHuman(playing, seed, humanAlive: true);

            // 4. Construir el estado persistente
            var state = new ActiveTournamentDoc
            {
                Id = userId,
                UserId = userId,
                LeagueId = user.LeagueId,
                TournamentName = todayTournament.Name,
                Surface = todayTournament.Surface,
                Seed = seed,
                CurrentRound = 0,
                HumanAlive = true,
                Survivors = [.. byes, .. advancingNonHuman], // aún sin el resultado del humano
                ReachedRound = [],
            };

            // Registrar la ronda (los partidos ya resueltos)
            RecordRound(state, matches, TournamentBracket.RoundName(playing.Count));

            // Marcar la ronda alcanzada por los perdedores de esta ronda
            foreach (var m in matches.Where(m => m.WinnerId != null))
            {
                var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                state.ReachedRound[loser.Id] = playing.Count;
            }

            // 5. ¿El humano juega esta ronda, o tiene bye?
            object payload;
            if (humanMatch != null)
            {
                // Juega: devolvemos su rival para que Flutter lo anime
                var opponent = humanMatch.Player1!.IsHuman ? humanMatch.Player2! : humanMatch.Player1!;
                payload = HumanPlaysPayload(state, opponent, TournamentBracket.RoundName(playing.Count));
            }
            else
            {
                // Bye en primera ronda: el humano está entre los byes.
                // Avanzamos hasta su primer partido real.
                payload = await AdvanceUntilHumanPlaysOrEnds(state, seed);
            }

            // 6. Persistir estado
            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");
            await atContainer.UpsertItemAsync(state, new PartitionKey(userId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(payload, Opts));
            return res;
            } catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                err.Headers.Add("Content-Type", "application/json");
                await err.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    type = ex.GetType().Name,
                    stack = ex.StackTrace,
                    inner = ex.InnerException?.Message,
                }));
                return err;
            }
        }

        // Cuando el humano tiene bye o ya avanzó sin jugar, resolvemos rondas
        // hasta que le toque jugar o el torneo acabe.
        private Task<object> AdvanceUntilHumanPlaysOrEnds(ActiveTournamentDoc state, int seed)
        {
            while (true)
            {
                state.CurrentRound++;
                var players = state.Survivors;

                if (players.Count == 1)
                {
                    state.Finished = true;
                    state.ChampionId = players[0].Id;
                    state.ReachedRound[players[0].Id] = 1;
                    return Task.FromResult<object>(FinishedPayload(state));
                }

                var (matches, humanMatch, advancing) =
                    TournamentOrchestrator.ResolveRoundSkippingHuman(players, seed + state.CurrentRound * 1000, state.HumanAlive);

                RecordRound(state, matches, TournamentBracket.RoundName(players.Count));
                foreach (var m in matches.Where(m => m.WinnerId != null))
                {
                    var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                    state.ReachedRound[loser.Id] = players.Count;
                }

                if (humanMatch != null)
                {
                    var opponent = humanMatch.Player1!.IsHuman ? humanMatch.Player2! : humanMatch.Player1!;
                    // Los que avanzan quedan pendientes de sumar al humano tras su partido
                    state.Survivors = advancing;
                    return Task.FromResult<object>(HumanPlaysPayload(state, opponent, TournamentBracket.RoundName(players.Count)));
                }

                state.Survivors = advancing;
            }
        }

        private static void RecordRound(ActiveTournamentDoc state, List<BracketMatch> matches, string roundName)
        {
            var rec = new RoundRecord { RoundName = roundName };
            foreach (var m in matches)
            {
                var winnerName = m.WinnerId == null ? "(pendiente)"
                    : (m.WinnerId == m.Player1!.Id ? m.Player1!.Name : m.Player2!.Name);
                rec.Results.Add(new MatchRecord
                {
                    P1Name = m.Player1!.Name,
                    P2Name = m.Player2!.Name,
                    WinnerId = m.WinnerId ?? "",
                    WinnerName = winnerName,
                    SetsScore = m.SetsScore,
                    InvolvesHuman = m.InvolvesHuman,
                });
            }
            state.History.Add(rec);
        }

        private static object HumanPlaysPayload(ActiveTournamentDoc state, Participant opponent, string roundName)
            => new
            {
                status = "humanPlays",
                tournamentName = state.TournamentName,
                surface = state.Surface,
                roundName,
                opponent = new { opponent.Id, opponent.Name, opponent.Overall },
            };

        private static object FinishedPayload(ActiveTournamentDoc state)
            => new
            {
                status = "finished",
                tournamentName = state.TournamentName,
                championId = state.ChampionId,
                humanEliminatedRound = state.HumanEliminatedRound,
                history = state.History,
            };

        private static int LargestPowerOfTwoBelow(int n)
        {
            int p = 1;
            while (p * 2 <= n) p *= 2;
            return p;
        }
    }
}