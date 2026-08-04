namespace TennisApi
{
    public static class TournamentRewards
    {
        // Puntos que recibe el CAMPEÓN según categoría (escala ATP real)
        public static int ChampionPoints(string category) => category switch
        {
            "t250"      => 250,
            "t500"      => 500,
            "t1000"     => 1000,
            "grandSlam" => 2000,
            "finals"    => 1500,
            _           => 250,
        };

        // Subida de atributos al campeón según categoría (acumulación fraccionada)
        public static double ChampionAttributeGain(string category) => category switch
        {
            "t250"      => 0.1,
            "t500"      => 0.2,
            "t1000"     => 0.4,
            "grandSlam" => 0.7,
            "finals"    => 1.0,
            _           => 0.1,
        };

        // Fracción de puntos según la ronda alcanzada (1.0 = campeón).
        // playersInReachedRound: nº de jugadores que había en la ronda donde cayó.
        // Ej: cae en ronda de 16 → llegó a octavos → fracción menor que quien llega a la final.
        public static double PointsFractionByRound(int playersInReachedRound, bool isChampion)
        {
            if (isChampion) return 1.0;
            return playersInReachedRound switch
            {
                2  => 0.60, // finalista (perdió la final)
                4  => 0.36, // semifinalista
                8  => 0.18, // cuartos
                16 => 0.09, // octavos
                _  => 0.045,
            };
        }

        // Dinero según puntos ganados (simple: proporcional)
        public static int MoneyFromPoints(int points) => points * 2;

        // Descansos (maletines verdes): todos ganan alguno, más cuanto antes caes.
        // isChampion = 1 descanso (recompensa simbólica; su premio fuerte es la mejora)
        public static int RestsByRound(int playersInReachedRound, bool isChampion)
        {
            if (isChampion) return 1;
            return playersInReachedRound switch
            {
                2  => 1, // finalista
                4  => 2, // semis
                8  => 2, // cuartos
                16 => 3, // octavos (cae pronto → más descansos)
                _  => 4, // primera ronda o antes
            };
        }
    }
}