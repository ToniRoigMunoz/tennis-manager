namespace TennisApi
{
    public static class BotFactory
    {
        private static readonly string[] Styles =
        [
            "El Muro", "Cañonero", "Agresivo de Fondo",
            "Contraatacante", "Mago de la Pista", "Francotirador"
        ];

        private static readonly Dictionary<string, HashSet<string>> StyleHighlights = new()
        {
            ["El Muro"]           = ["Resistencia", "Velocidad", "Concentración", "Anticipación", "Revés"],
            ["Cañonero"]          = ["Fuerza", "Reflejos", "Sangre Fría", "Saque", "Juego en la Red"],
            ["Agresivo de Fondo"] = ["Fuerza", "Velocidad", "Visión de Juego", "Derecha", "Efecto"],
            ["Contraatacante"]    = ["Velocidad", "Reflejos", "Anticipación", "Derecha", "Revés"],
            ["Mago de la Pista"]  = ["Flexibilidad", "Creatividad", "Visión de Juego", "Juego en la Red", "Efecto"],
            ["Francotirador"]     = ["Resistencia", "Sangre Fría", "Concentración", "Visión de Juego", "Revés"],
        };

        private static readonly string[] PhysicalNames  = ["Resistencia", "Velocidad", "Fuerza", "Reflejos", "Flexibilidad"];
        private static readonly string[] MentalNames    = ["Sangre Fría", "Concentración", "Visión de Juego", "Anticipación", "Creatividad"];
        private static readonly string[] TechnicalNames = ["Saque", "Derecha", "Revés", "Juego en la Red", "Efecto"];

        public static BotDocument Create(
            string leagueId, string botId, string name,
            string nationality, string flag, int overall, int seed)
        {
            var rng = new Random(seed);
            var style = Styles[rng.Next(Styles.Length)];
            var highlighted = StyleHighlights[style];

            // Resaltados: overall+10 | resto: overall-5  →  media exacta = overall
            AttributeDoc Gen(string attrName)
            {
                var baseline = highlighted.Contains(attrName) ? overall + 10 : overall - 5;
                var value = Math.Clamp(baseline + rng.Next(-6, 7), 15, 99);
                return new AttributeDoc { Name = attrName, Value = value };
            }

            return new BotDocument
            {
                Id = botId,
                LeagueId = leagueId,
                Name = name,
                Nationality = nationality,
                NationalityFlag = flag,
                PlayingStyle = style,
                Overall = overall,
                Physical  = [.. PhysicalNames.Select(Gen)],
                Mental    = [.. MentalNames.Select(Gen)],
                Technical = [.. TechnicalNames.Select(Gen)],
            };
        }
    }
}