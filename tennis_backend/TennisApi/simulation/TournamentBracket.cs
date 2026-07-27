namespace TennisApi
{
    public static class TournamentBracket
    {
        // Nombre de ronda según cuántos jugadores quedan
        public static string RoundName(int playersInRound) => playersInRound switch
        {
            2  => "Final",
            4  => "Semifinales",
            8  => "Cuartos de final",
            16 => "Octavos de final",
            _  => $"Ronda de {playersInRound}",
        };

        // Empareja una ronda al estilo tenis: 1º-último, 2º-penúltimo, etc.
        // La lista de entrada debe venir ordenada por seed (mejor primero).
        public static List<BracketMatch> PairRound(List<Participant> players)
        {
            var matches = new List<BracketMatch>();
            int n = players.Count;
            for (int i = 0; i < n / 2; i++)
            {
                var p1 = players[i];
                var p2 = players[n - 1 - i];
                matches.Add(new BracketMatch
                {
                    MatchIndex = i,
                    Player1 = p1,
                    Player2 = p2,
                    InvolvesHuman = p1.IsHuman || p2.IsHuman,
                });
            }
            return matches;
        }

        // Aplica byes: los 'byeCount' mejores sembrados pasan directos.
        // Devuelve (jugadores que juegan la ronda, jugadores con bye).
        public static (List<Participant> playing, List<Participant> byes) ApplyByes(
            List<Participant> seeded, int byeCount)
        {
            var byes = seeded.Take(byeCount).ToList();
            var playing = seeded.Skip(byeCount).ToList();
            return (playing, byes);
        }

        // Resuelve en PARALELO todos los partidos de una ronda que no involucran al humano.
        // Devuelve la lista de ganadores (participantes que avanzan).
        public static async Task<List<Participant>> ResolveRoundParallel(
            List<BracketMatch> matches, int seedBase, bool skipHuman)
        {
            var tasks = matches.Select(m => Task.Run(() =>
            {
                if (skipHuman && m.InvolvesHuman)
                    return; // el partido del humano se resuelve aparte (animado)

                var (winnerId, score) = SimulateFast(m.Player1!, m.Player2!, seedBase + m.MatchIndex);
                m.WinnerId = winnerId;
                m.SetsScore = score;
            }));

            await Task.WhenAll(tasks);

            var advancing = new List<Participant>();
            foreach (var m in matches)
            {
                if (m.WinnerId == null) continue; // partido del humano pendiente
                advancing.Add(m.WinnerId == m.Player1!.Id ? m.Player1! : m.Player2!);
            }
            return advancing;
        }

        // Simulación RÁPIDA: solo ganador y marcador, sin registro punto a punto.
        public static (string winnerId, string setsScore) SimulateFast(
            Participant p1, Participant p2, int seed)
        {
            var engine = new MatchEngine(p1.Sim!, p2.Sim!, bestOf: 3, seed: seed);
            var result = engine.Simulate();
            var winnerId = result.Winner == 1 ? p1.Id : p2.Id;
            return (winnerId, result.SetsScore);
        }
    }
}