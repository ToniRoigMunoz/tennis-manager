namespace TennisApi
{
    public static class ServerClock
    {
        // Un torneo de 24 con byes tiene 5 rondas (octavos con byes → final)
        public const int MaxRounds = 5;

        // "Ahora" efectivo del servidor: hora real UTC + desfase de desarrollo
        public static DateTime EffectiveNow(TournamentDocument season)
            => DateTime.UtcNow.AddSeconds(season.DevTimeOffsetSeconds);

        // Instante en que empezó el día de juego actual
        public static DateTime DayStart(TournamentDocument season)
        {
            if (DateTime.TryParse(season.CurrentDayStartUtc, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var parsed))
                return parsed;
            return DateTime.UtcNow; // fallback si aún no se ha fijado
        }

        // Hora UTC a la que se desbloquea una ronda (1..MaxRounds)
        public static DateTime RoundUnlockTime(TournamentDocument season, int round)
            => DayStart(season).AddMinutes((round - 1) * (double)season.RoundIntervalMinutes);

        // Ronda actualmente desbloqueada según el reloj (1..MaxRounds)
        public static int CurrentUnlockedRound(TournamentDocument season)
        {
            var elapsed = EffectiveNow(season) - DayStart(season);
            if (elapsed.TotalMinutes < 0) return 1;
            int round = (int)(elapsed.TotalMinutes / season.RoundIntervalMinutes) + 1;
            return Math.Clamp(round, 1, MaxRounds);
        }

        // ¿Se ha cerrado ya la ventana de una ronda? (para simular lo no jugado)
        public static bool IsRoundClosed(TournamentDocument season, int round)
            => EffectiveNow(season) >= RoundUnlockTime(season, round + 1);
    }
}