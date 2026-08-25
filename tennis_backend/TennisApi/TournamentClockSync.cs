using Microsoft.Azure.Cosmos;

namespace TennisApi
{
    // Lógica compartida de avance de rondas gobernado por el reloj.
    // La usan GetTournamentStatus (al consultar) y AdvanceDay (al cerrar el día).
    public static class TournamentClockSync
    {
        // Por cada ronda cuya ventana se cerró: simula los partidos humanos no jugados
        // y monta la siguiente ronda (para todos a la vez).
        public static async Task SyncRoundsToClock(CosmosClient cosmos, ActiveTournamentDoc state, TournamentDocument season)
        {
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);
            var byName = all.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());

            int safety = 0;
            while (!state.Finished && safety++ < 12)
            {
                int assembledRound = state.History.Count;
                int clockWindow = ServerClock.CurrentUnlockedRound(season);
                if (clockWindow <= assembledRound) break;

                var lastRound = state.History[^1];
                int roundSize = RoundSizeFromName(lastRound.RoundName);

                // 1. Simular los partidos humanos NO jugados de esta ronda
                foreach (var m in lastRound.Results.Where(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId)).ToList())
                {
                    if (!byName.TryGetValue(m.P1Name, out var p1) || !byName.TryGetValue(m.P2Name, out var p2)) continue;
                    if (p1.Sim == null && simById.TryGetValue(p1.Id, out var s1)) p1.Sim = s1;
                    if (p2.Sim == null && simById.TryGetValue(p2.Id, out var s2)) p2.Sim = s2;

                    int matchSeed = state.Seed + assembledRound * 7919 + m.P1Name.GetHashCode();
                    var (winnerId, score) = TournamentBracket.SimulateFast(p1, p2, matchSeed);
                    var winner = winnerId == p1.Id ? p1 : p2;
                    var loser = winnerId == p1.Id ? p2 : p1;

                    m.WinnerId = winner.Id;
                    m.WinnerName = winner.Name;
                    m.SetsScore = score;

                    state.ReachedRound[loser.Id] = roundSize;
                    if (loser.IsHuman && state.HumanStates.TryGetValue(loser.Id, out var hsL))
                    {
                        hsL.Alive = false;
                        hsL.EliminatedRound = roundSize;
                    }
                    if (!state.Survivors.Any(p => p.Id == winner.Id)) state.Survivors.Add(winner);
                    state.Survivors.RemoveAll(p => p.Id == loser.Id);
                }

                // 2. ¿Queda un solo superviviente? Campeón.
                if (state.Survivors.Count <= 1)
                {
                    state.Finished = true;
                    if (state.Survivors.Count == 1)
                    {
                        state.ChampionId = state.Survivors[0].Id;
                        state.ReachedRound[state.Survivors[0].Id] = 1;
                    }
                    break;
                }

                // 3. Montar la siguiente ronda (una sola vez, con todos los supervivientes)
                foreach (var p in state.Survivors)
                    if (p.Sim == null && simById.TryGetValue(p.Id, out var sim)) p.Sim = sim;

                state.CurrentRound++;
                var aliveHumans = state.HumanStates.Where(kv => kv.Value.Alive).Select(kv => kv.Key).ToHashSet();
                var (matches, humanMatches, advancing) = TournamentOrchestrator.ResolveRoundMultiHuman(
                    state.Survivors, state.Seed + state.CurrentRound * 1000, aliveHumans);

                RecordRound(state, matches, TournamentBracket.RoundName(state.Survivors.Count));
                foreach (var mm in matches.Where(x => x.WinnerId != null))
                {
                    var loser = mm.WinnerId == mm.Player1!.Id ? mm.Player2! : mm.Player1!;
                    state.ReachedRound[loser.Id] = state.Survivors.Count;
                }
                state.Survivors = advancing;

                int newRoundIndex = state.History.Count;
                foreach (var hm in humanMatches)
                    foreach (var pid in new[] { hm.Player1!.Id, hm.Player2!.Id })
                        if (state.HumanStates.TryGetValue(pid, out var hs)) hs.RoundIndex = newRoundIndex;
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

        private static int RoundSizeFromName(string roundName) => roundName switch
        {
            "Final" => 2,
            "Semifinales" => 4,
            "Cuartos de final" => 8,
            "Octavos de final" => 16,
            _ => 16,
        };
    }
}