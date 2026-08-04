using Microsoft.Azure.Cosmos;
using System.Net;

namespace TennisApi
{
    public static class TournamentBootstrap
    {
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

            var (matches, humanMatch, advancingNonHuman) =
                TournamentOrchestrator.ResolveRoundSkippingHuman(playing, seed, humanAlive: true);

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
                Finished = false,
                Survivors = [.. byes, .. advancingNonHuman],
                ReachedRound = [],
                History = [],
                Category = todayTournament.Category,
            };

            RecordRound(state, matches, TournamentBracket.RoundName(playing.Count));
            foreach (var m in matches.Where(m => m.WinnerId != null))
            {
                var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                state.ReachedRound[loser.Id] = playing.Count;
            }

            // El humano puede tener bye en primera ronda: avanzar hasta su primer partido
            object payload;
            if (humanMatch != null)
            {
                var opponent = humanMatch.Player1!.IsHuman ? humanMatch.Player2! : humanMatch.Player1!;
                payload = HumanPlaysPayload(state, opponent, TournamentBracket.RoundName(playing.Count));
            }
            else
            {
                payload = AdvanceThroughByes(state, seed);
            }

            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");
            await atContainer.UpsertItemAsync(state, new PartitionKey(userId));

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
                var (matches, humanMatch, advancing) =
                    TournamentOrchestrator.ResolveRoundSkippingHuman(players, seed + state.CurrentRound * 1000, state.HumanAlive);

                RecordRound(state, matches, TournamentBracket.RoundName(playersInThisRound));
                foreach (var m in matches.Where(m => m.WinnerId != null))
                {
                    var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                    state.ReachedRound[loser.Id] = playersInThisRound;
                }

                if (humanMatch != null)
                {
                    var opponent = humanMatch.Player1!.IsHuman ? humanMatch.Player2! : humanMatch.Player1!;
                    state.Survivors = advancing;
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
                roundName,
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