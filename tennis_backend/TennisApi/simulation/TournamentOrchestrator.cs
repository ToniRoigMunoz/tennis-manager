namespace TennisApi
{
    public static class TournamentOrchestrator
    {
        /*
        * Resuelve una ronda dejando pendiente el partido del humano (si sigue vivo).
        * Devuelve los partidos de la ronda, el rival del humano (si juega), y los que avanzan (sin contar al humano aún).
        */ 
        public static (List<BracketMatch> matches, BracketMatch? humanMatch, List<Participant> advancingNonHuman)
        ResolveRoundSkippingHuman(List<Participant> players, int seedBase, bool humanAlive)
        {
            players.Sort((a, b) => a.Seed.CompareTo(b.Seed));
            var matches = TournamentBracket.PairRound(players);

            BracketMatch? humanMatch = null;
            var advancing = new List<Participant>();

            foreach (var m in matches)
            {
                if (humanAlive && m.InvolvesHuman)
                {
                    humanMatch = m; // pendiente: lo juega el usuario animado
                    continue;
                }
                var (winnerId, score) = TournamentBracket.SimulateFast(m.Player1!, m.Player2!, seedBase + m.MatchIndex);
                m.WinnerId = winnerId;
                m.SetsScore = score;
                advancing.Add(winnerId == m.Player1!.Id ? m.Player1! : m.Player2!);
            }

            return (matches, humanMatch, advancing);
        }

        /*
        * Resuelve una ronda dejando pendientes los partidos de los humanos vivos.
        * Devuelve todos los partidos, la lista de partidos humanos pendientes, y los que avanzan (sin contar aún a los humanos, que avanzarán cuando jueguen).
        */ 
        public static (List<BracketMatch> matches, List<BracketMatch> humanMatches, List<Participant> advancingNonHuman)
        ResolveRoundMultiHuman(List<Participant> players, int seedBase, HashSet<string> aliveHumanIds)
        {
            players.Sort((a, b) => a.Seed.CompareTo(b.Seed));
            var matches = TournamentBracket.PairRound(players);

            var humanMatches = new List<BracketMatch>();
            var advancing = new List<Participant>();

            foreach (var m in matches)
            {
                // ¿Este partido involucra a un humano que sigue vivo?
                bool p1AliveHuman = m.Player1!.IsHuman && aliveHumanIds.Contains(m.Player1!.Id);
                bool p2AliveHuman = m.Player2!.IsHuman && aliveHumanIds.Contains(m.Player2!.Id);

                if (p1AliveHuman || p2AliveHuman)
                {
                    // Pendiente: lo jugará el humano (o los humanos, en la 3b) cuando entre.
                    humanMatches.Add(m);
                    continue;
                }

                // Partido solo de bots: se resuelve al momento.
                var (winnerId, score) = TournamentBracket.SimulateFast(m.Player1!, m.Player2!, seedBase + m.MatchIndex);
                m.WinnerId = winnerId;
                m.SetsScore = score;
                advancing.Add(winnerId == m.Player1!.Id ? m.Player1! : m.Player2!);
            }

            return (matches, humanMatches, advancing);
        }

        /*
        * Resuelve el torneo completo de golpe (usado cuando el humano ya no está).
        * Devuelve el campeón y cuántas rondas alcanzó cada uno.
        */
        public static (Participant champion, Dictionary<string, int> reached, List<RoundRecord> history)
            ResolveRemainingFully(List<Participant> players, int seedBase, Dictionary<string, int> reached)
        {
            var history = new List<RoundRecord>();
            int roundOffset = 100;

            while (players.Count > 1)
            {
                players.Sort((a, b) => a.Seed.CompareTo(b.Seed));
                var matches = TournamentBracket.PairRound(players);
                var advancing = new List<Participant>();
                var record = new RoundRecord { RoundName = TournamentBracket.RoundName(players.Count) };

                foreach (var m in matches)
                {
                    var (winnerId, score) = TournamentBracket.SimulateFast(m.Player1!, m.Player2!, seedBase + roundOffset + m.MatchIndex);
                    var winner = winnerId == m.Player1!.Id ? m.Player1! : m.Player2!;
                    var loser = winnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                    advancing.Add(winner);
                    reached[loser.Id] = players.Count; // alcanzó esta ronda (perdió aquí)

                    record.Results.Add(new MatchRecord
                    {
                        P1Name = m.Player1!.Name, P2Name = m.Player2!.Name,
                        WinnerId = winnerId, WinnerName = winner.Name, SetsScore = score,
                    });
                }

                history.Add(record);
                players = advancing;
                roundOffset += 100;
            }

            var champion = players[0];
            reached[champion.Id] = 1; // el campeón "alcanzó" la ronda final (1 = ganó todo)
            return (champion, reached, history);
        }
    }
}