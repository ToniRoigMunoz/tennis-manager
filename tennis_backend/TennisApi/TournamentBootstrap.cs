using Microsoft.Azure.Cosmos;
using System.Net;

namespace TennisApi
{
    public static class TournamentBootstrap
    {
        // IDs de los humanos que siguen vivos, según el diccionario de estados
        private static HashSet<string> AliveHumanIds(ActiveTournamentDoc state)
            => state.HumanStates.Where(kv => kv.Value.Alive).Select(kv => kv.Key).ToHashSet();

        // Crea el torneo del día para un usuario y devuelve el payload de su primer partido
        public static async Task<object> CreateDailyTournament(CosmosClient cosmos, string userId)
        {
            var seed = Random.Shared.Next();

            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var user = (await usersContainer.ReadItemAsync<UserDocument>(userId, new PartitionKey(userId))).Resource;

            var toursContainer = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await toursContainer.ReadItemAsync<TournamentDocument>(user.SeasonId, new PartitionKey(user.SeasonId))).Resource;
            var todayTournament = season.Tournaments.FirstOrDefault(t => t.Status == "current")
                                  ?? season.Tournaments.First(t => t.StartDay == season.CurrentDay);

            var participants = await ParticipantLoader.Load(cosmos, user.LeagueId);
            participants.Sort((a, b) => a.Seed.CompareTo(b.Seed));

            int byeCount = participants.Count - LargestPowerOfTwoBelow(participants.Count);
            var (playing, byes) = TournamentBracket.ApplyByes(participants, byeCount);

            var aliveHumans = participants.Where(p => p.IsHuman).Select(p => p.Id).ToHashSet();
            var (matches, humanMatches, advancingNonHuman) = TournamentOrchestrator.ResolveRoundMultiHuman(playing, seed, aliveHumans);

            var state = new ActiveTournamentDoc
            {
                Id = user.LeagueId,
                UserId = userId,
                LeagueId = user.LeagueId,
                TournamentName = todayTournament.Name,
                Surface = todayTournament.Surface,
                Seed = seed,
                CurrentRound = 0,
                HumanAlive = true,
                Finished = false,
                Survivors = [.. byes, .. advancingNonHuman],
                ReachedRound = [],
                History = [],
                Category = todayTournament.Category,
                HumanStates = participants
                    .Where(p => p.IsHuman)
                    .ToDictionary(
                        p => p.Id,
                        p => new HumanTournamentState
                        {
                            UserId = p.Id,
                            Alive = true,
                            RoundIndex = 1,
                            Name = p.Name,
                        }),
            };

            RecordRound(state, matches, TournamentBracket.RoundName(playing.Count));
            foreach (var m in matches.Where(m => m.WinnerId != null))
            {
                var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                state.ReachedRound[loser.Id] = playing.Count;
            }

            // El humano puede tener bye en primera ronda entonces avanza hasta su primer partido
            object payload;
            var myMatch = humanMatches.FirstOrDefault(m => m.Player1!.Id == userId || m.Player2!.Id == userId);
            if (myMatch != null)
            {
                var opponent = myMatch.Player1!.Id == userId ? myMatch.Player2! : myMatch.Player1!;
                state.HumanRoundIndex = state.History.Count;
                if (state.HumanStates.TryGetValue(userId, out var hs)) 
                {
                    hs.RoundIndex = state.History.Count;
                }
                payload = HumanPlaysPayload(state, opponent, TournamentBracket.RoundName(playing.Count));
            }
            else
            {
                payload = AdvanceThroughByes(state, seed);
            }

            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");
            await atContainer.UpsertItemAsync(state, new PartitionKey(user.LeagueId));

            return payload;
        }

        private static object AdvanceThroughByes(ActiveTournamentDoc state, int seed)
        {
            while (true)
            {
                state.CurrentRound++;
                var players = state.Survivors;

                if (players.Count == 1)
                {
                    state.Finished = true;
                    state.ChampionId = players[0].Id;
                    return new { status = "finished", tournamentName = state.TournamentName, championName = players[0].Name, history = state.History };
                }

                int playersInThisRound = players.Count;
                var (matches, humanMatches, advancing) = TournamentOrchestrator.ResolveRoundMultiHuman(players, seed + state.CurrentRound * 1000, AliveHumanIds(state));

                RecordRound(state, matches, TournamentBracket.RoundName(playersInThisRound));
                foreach (var m in matches.Where(m => m.WinnerId != null))
                {
                    var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                    state.ReachedRound[loser.Id] = playersInThisRound;
                }

                var myMatch = humanMatches.FirstOrDefault(m => m.Player1!.Id == state.UserId || m.Player2!.Id == state.UserId);
                if (myMatch != null)
                {
                    var opponent = myMatch.Player1!.Id == state.UserId ? myMatch.Player2! : myMatch.Player1!;
                    state.Survivors = advancing;
                    state.HumanRoundIndex = state.History.Count;
                    if (state.HumanStates.TryGetValue(state.UserId, out var hs)) 
                    {
                        hs.RoundIndex = state.History.Count;
                    }
                    return HumanPlaysPayload(state, opponent, TournamentBracket.RoundName(playersInThisRound));
                }
                state.Survivors = advancing;
            }
        }

        private static object HumanPlaysPayload(ActiveTournamentDoc state, Participant opponent, string roundName)
            => new
            {
                status = "humanPlays",
                tournamentName = state.TournamentName,
                surface = state.Surface,
                roundName = ServerClock.RoundNameByIndex(state.HumanRoundIndex),
                opponent = new { opponent.Id, opponent.Name, opponent.Overall },
                seed = state.Seed,
            };

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

        private static int LargestPowerOfTwoBelow(int n)
        {
            int p = 1;
            while (p * 2 <= n) p *= 2;
            return p;
        }
    }
}