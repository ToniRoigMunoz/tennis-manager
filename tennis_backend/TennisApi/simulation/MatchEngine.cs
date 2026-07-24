namespace TennisApi
{
    public class MatchEngine
    {
        // ── Constantes de ajuste (validadas con 2000 partidos/escenario) ───────
        private const double AceBase = 0.03, AceScale = 0.14;
        private const double DfBase  = 0.06, DfScale  = 0.05;
        private const double ServeRallyBonus = 58.0;   // se escala con el saque del jugador
        private const double FatigueMax = 0.15;
        private const double WinnerBase = 0.30;        // reparto del desenlace del peloteo
        private const double ForcedBase = 0.26;

        private readonly Random _rng;
        private readonly SimPlayer _p1, _p2;
        private readonly int _setsToWin;
        private int _pointsPlayed1, _pointsPlayed2;

        public MatchEngine(SimPlayer p1, SimPlayer p2, int bestOf, int seed)
        {
            _p1 = p1;
            _p2 = p2;
            _setsToWin = bestOf / 2 + 1;
            _rng = new Random(seed);
        }

        public MatchResult Simulate()
        {
            var result = new MatchResult
            {
                Player1Name = _p1.Name,
                Player2Name = _p2.Name,
                BestOf = _setsToWin * 2 - 1,
            };

            int sets1 = 0, sets2 = 0, setIndex = 0;
            var setScores = new List<string>();

            while (sets1 < _setsToWin && sets2 < _setsToWin)
            {
                var (g1, g2) = PlaySet(setIndex, sets1, sets2, result);
                if (g1 > g2) sets1++; else sets2++;
                setScores.Add($"{g1}-{g2}");
                setIndex++;
            }

            result.Winner = sets1 > sets2 ? 1 : 2;
            result.SetsScore = string.Join(", ", setScores);

            foreach (var pe in result.Points)
            {
                var winnerStats = pe.Winner == 1 ? result.Stats1 : result.Stats2;
                var loserStats  = pe.Winner == 1 ? result.Stats2 : result.Stats1;
                winnerStats.TotalPointsWon++;
                Tally(pe, winnerStats, loserStats);
            }

            return result;
        }

        // Contabilidad correcta: cada desenlace lo comete/logra quien corresponde
        private static void Tally(PointEvent pe, MatchStats winnerStats, MatchStats loserStats)
        {
            switch (pe.Outcome)
            {
                case "ace":           winnerStats.Aces++; break;
                case "doubleFault":   loserStats.DoubleFaults++; break;   // la doble falta la comete quien saca y pierde el punto
                case "winner":        winnerStats.Winners++; break;
                case "forcedError":   loserStats.ForcedErrors++; break;   // el perdedor falla presionado
                case "unforcedError": loserStats.UnforcedErrors++; break; // el perdedor falla solo
                case "neutral":       winnerStats.NeutralPoints++; break;
            }
        }

        private (int, int) PlaySet(int setIndex, int sets1, int sets2, MatchResult result)
        {
            int g1 = 0, g2 = 0;
            int server = setIndex % 2 == 0 ? 1 : 2;

            while (true)
            {
                if (g1 == 6 && g2 == 6)
                {
                    var tbWinner = PlayTiebreak(setIndex, sets1, sets2, g1, g2, server, result);
                    if (tbWinner == 1) g1++; else g2++;
                    return (g1, g2);
                }

                PlayGame(server, setIndex, sets1, sets2, ref g1, ref g2, result);
                server = server == 1 ? 2 : 1;

                bool p1Wins = g1 >= 6 && g1 - g2 >= 2;
                bool p2Wins = g2 >= 6 && g2 - g1 >= 2;
                if (p1Wins || p2Wins) return (g1, g2);
            }
        }

        private void PlayGame(int server, int setIndex, int sets1, int sets2,
                              ref int g1, ref int g2, MatchResult result)
        {
            int pts1 = 0, pts2 = 0;

            while (true)
            {
                var (winner, outcome) = ResolvePoint(server);
                if (winner == 1) pts1++; else pts2++;

                bool gameOver = (pts1 >= 4 || pts2 >= 4) && Math.Abs(pts1 - pts2) >= 2;
                int futureG1 = g1 + (gameOver && pts1 > pts2 ? 1 : 0);
                int futureG2 = g2 + (gameOver && pts2 > pts1 ? 1 : 0);

                result.Points.Add(new PointEvent
                {
                    Server = server,
                    Winner = winner,
                    Outcome = outcome,
                    P1GameScore = GameScore(pts1, pts2),
                    P2GameScore = GameScore(pts2, pts1),
                    P1Games = futureG1,
                    P2Games = futureG2,
                    P1Sets = sets1,
                    P2Sets = sets2,
                    SetIndex = setIndex,
                    IsTiebreak = false,
                    IsGameOver = gameOver,
                    IsSetPoint = IsSetPointSituation(futureG1, futureG2, pts1, pts2, gameOver),
                    IsMatchPoint = false,
                    IsSetOver = gameOver && (futureG1 >= 6 && futureG1 - futureG2 >= 2 ||
                                             futureG2 >= 6 && futureG2 - futureG1 >= 2),
                });

                if (gameOver)
                {
                    if (pts1 > pts2) g1++; else g2++;
                    return;
                }
            }
        }

        private int PlayTiebreak(int setIndex, int sets1, int sets2, int g1, int g2,
                                 int server, MatchResult result)
        {
            int pts1 = 0, pts2 = 0, played = 0;

            while (true)
            {
                var (winner, outcome) = ResolvePoint(server);
                if (winner == 1) pts1++; else pts2++;
                played++;

                bool tbOver = (pts1 >= 7 || pts2 >= 7) && Math.Abs(pts1 - pts2) >= 2;

                // Si el tie-break se cierra, el juego ya cuenta para el marcador (7-6)
                int shownG1 = g1 + (tbOver && pts1 > pts2 ? 1 : 0);
                int shownG2 = g2 + (tbOver && pts2 > pts1 ? 1 : 0);

                result.Points.Add(new PointEvent
                {
                    Server = server,
                    Winner = winner,
                    Outcome = outcome,
                    P1GameScore = pts1.ToString(),
                    P2GameScore = pts2.ToString(),
                    P1Games = shownG1,
                    P2Games = shownG2,
                    P1Sets = sets1,
                    P2Sets = sets2,
                    SetIndex = setIndex,
                    IsTiebreak = true,
                    IsGameOver = tbOver,
                    IsSetPoint = (pts1 >= 6 || pts2 >= 6) && Math.Abs(pts1 - pts2) >= 1,
                    IsMatchPoint = false,
                    IsSetOver = tbOver,
                });

                if (played == 1 || (played - 1) % 2 == 0)
                    server = server == 1 ? 2 : 1;

                if (tbOver) return pts1 > pts2 ? 1 : 2;
            }
        }

        // ── Resolución de un punto: 5 desenlaces posibles ─────────────────────
        private (int winner, string outcome) ResolvePoint(int server)
        {
            var s = server == 1 ? _p1 : _p2;
            var r = server == 1 ? _p2 : _p1;

            // Fase de saque
            double aceProb = AceBase + s.Serve / 100.0 * AceScale;
            double dfProb  = DfBase  - s.Consistency / 100.0 * DfScale;

            double roll = _rng.NextDouble();
            if (roll < aceProb)
                return (server, "ace");
            if (roll < aceProb + dfProb)
                return (server == 1 ? 2 : 1, "doubleFault");

            // Fase de peloteo — el bonus de saque escala con la capacidad de saque
            double serveBonus = ServeRallyBonus * (s.Serve / 100.0);
            double sPower = (s.Rally + serveBonus) * Fatigue(server);
            double rPower = r.Rally * Fatigue(server == 1 ? 2 : 1);
            double pServerWins = sPower / (sPower + rPower);

            int winner = _rng.NextDouble() < pServerWins ? server : (server == 1 ? 2 : 1);
            int loser  = winner == 1 ? 2 : 1;
            var w = winner == 1 ? _p1 : _p2;
            var l = loser  == 1 ? _p1 : _p2;

            if (winner == 1) _pointsPlayed1++; else _pointsPlayed2++;

            // Reparto del desenlace en 4 categorías: winner / forced / unforced / neutral
            double winnerW   = WinnerBase * (w.Winner / 55.0);
            double forcedW   = ForcedBase * (w.Winner / 55.0);
            double unforcedW = (1 - WinnerBase - ForcedBase) * ((100 - l.Consistency) / 42.0);
            const double neutralW = 1.0;
            double total = winnerW + forcedW + unforcedW + neutralW;

            double r2 = _rng.NextDouble() * total;
            string outcome;
            if (r2 < winnerW)                         outcome = "winner";
            else if (r2 < winnerW + forcedW)          outcome = "forcedError";
            else if (r2 < winnerW + forcedW + unforcedW) outcome = "unforcedError";
            else                                      outcome = "neutral";

            return (winner, outcome);
        }

        private double Fatigue(int player)
        {
            var played  = player == 1 ? _pointsPlayed1 : _pointsPlayed2;
            var stamina = player == 1 ? _p1.Stamina : _p2.Stamina;
            var fatigue = Math.Min(played / 500.0, 1.0) * FatigueMax * (1 - stamina / 100.0);
            return 1 - fatigue;
        }

        private static string GameScore(int me, int opp)
        {
            if (me >= 3 && opp >= 3)
            {
                if (me == opp) return "40";
                return me > opp ? "Ad" : "40";
            }
            return me switch { 0 => "0", 1 => "15", 2 => "30", _ => "40" };
        }

        private static bool IsSetPointSituation(int g1, int g2, int pts1, int pts2, bool gameOver)
        {
            bool nearSet1 = g1 >= 5 && g1 - g2 >= 1;
            bool nearSet2 = g2 >= 5 && g2 - g1 >= 1;
            bool gamePoint = (pts1 >= 3 || pts2 >= 3) && Math.Abs(pts1 - pts2) >= 1;
            return !gameOver && gamePoint && (nearSet1 || nearSet2);
        }
    }
}