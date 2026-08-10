namespace TennisApi
{
    public static class BotNameGenerator
    {
        private static readonly string[] FirstNames =
        [
            "Álex", "Marc", "Pau", "Iker", "Hugo", "Diego", "Adrián", "Bruno",
            "Jordi", "Raúl", "Víctor", "Óscar", "Pablo", "Sergio", "Nico", "Gael",
            "Mateo", "Leo", "Iván", "Rubén", "Dani", "Guille", "Álvaro", "Carlos",
            "Andrés", "Fran", "Javi", "Manu", "Toni", "Rafa", "Lucas", "Martín",
            "Enzo", "Thiago", "Marco", "Luca", "Dennis", "Felix", "Jonas", "Ivan",
        ];

        private static readonly string[] Surnames =
        [
            "Aguilar", "Castell", "Bilbao", "Soler", "Roma", "Vidal", "Prats", "Costa",
            "Ruiz", "Llopis", "Ferrer", "Munté", "Beltrán", "Domingo", "Esteve", "Soto",
            "Pla", "Ferrando", "Sanchís", "Gil", "Calatayud", "Mora", "Beneyto", "Navarro",
            "Ibáñez", "Ortega", "Serrano", "Molina", "Cano", "Ramos", "Vega", "Bauer",
            "Novak", "Rossi", "Moretti", "Petrov", "Kovač", "Hansen", "Berg", "Fischer",
        ];

        private static readonly (string Nat, string Flag)[] Nationalities =
        [
            ("España", "🇪🇸"), ("España", "🇪🇸"), ("España", "🇪🇸"),
            ("Italia", "🇮🇹"), ("Francia", "🇫🇷"), ("Portugal", "🇵🇹"),
            ("Argentina", "🇦🇷"), ("Brasil", "🇧🇷"), ("Alemania", "🇩🇪"),
            ("Serbia", "🇷🇸"), ("Suiza", "🇨🇭"), ("Noruega", "🇳🇴"),
        ];

        // Genera 'count' identidades únicas (nombre + nacionalidad)
        public static List<(string Name, string Nat, string Flag)> Generate(int count, Random rng)
        {
            var used = new HashSet<string>();
            var result = new List<(string, string, string)>();

            while (result.Count < count)
            {
                var name = $"{FirstNames[rng.Next(FirstNames.Length)]} {Surnames[rng.Next(Surnames.Length)]}";
                if (!used.Add(name)) continue; // evitar duplicados
                var (nat, flag) = Nationalities[rng.Next(Nationalities.Length)];
                result.Add((name, nat, flag));
            }
            return result;
        }
    }
}