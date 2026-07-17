using System.Text.Json.Serialization;

namespace TennisApi
{
    // ── Salida que consumirá el cliente para reproducir el partido ────────────
    public class MatchResult
    {
        [JsonPropertyName("player1Name")] public string Player1Name { get; set; } = "";
        [JsonPropertyName("player2Name")] public string Player2Name { get; set; } = "";
        [JsonPropertyName("bestOf")]      public int BestOf { get; set; }
        [JsonPropertyName("winner")]      public int Winner { get; set; } // 1 o 2
        [JsonPropertyName("setsScore")]   public string SetsScore { get; set; } = "";
        [JsonPropertyName("points")]      public List<PointEvent> Points { get; set; } = [];
        [JsonPropertyName("stats1")]      public MatchStats Stats1 { get; set; } = new();
        [JsonPropertyName("stats2")]      public MatchStats Stats2 { get; set; } = new();
        [JsonPropertyName("seed")]        public int Seed { get; set; }
    }

    // Estado del marcador DESPUÉS de jugarse cada punto. El cliente solo pinta.
    public class PointEvent
    {
        [JsonPropertyName("server")]      public int Server { get; set; }   // 1 o 2
        [JsonPropertyName("winner")]      public int Winner { get; set; }   // 1 o 2
        [JsonPropertyName("outcome")]     public string Outcome { get; set; } = ""; // ace | doubleFault | winner | unforcedError
        [JsonPropertyName("p1GameScore")] public string P1GameScore { get; set; } = ""; // "0","15","30","40","Ad"
        [JsonPropertyName("p2GameScore")] public string P2GameScore { get; set; } = "";
        [JsonPropertyName("p1Games")]     public int P1Games { get; set; }
        [JsonPropertyName("p2Games")]     public int P2Games { get; set; }
        [JsonPropertyName("p1Sets")]      public int P1Sets { get; set; }
        [JsonPropertyName("p2Sets")]      public int P2Sets { get; set; }
        [JsonPropertyName("setIndex")]    public int SetIndex { get; set; }
        [JsonPropertyName("isTiebreak")]  public bool IsTiebreak { get; set; }
        [JsonPropertyName("isSetPoint")]  public bool IsSetPoint { get; set; }
        [JsonPropertyName("isMatchPoint")]public bool IsMatchPoint { get; set; }
        [JsonPropertyName("isGameOver")]  public bool IsGameOver { get; set; }
        [JsonPropertyName("isSetOver")]   public bool IsSetOver { get; set; }
    }

    public class MatchStats
    {
        [JsonPropertyName("aces")]           public int Aces { get; set; }
        [JsonPropertyName("doubleFaults")]   public int DoubleFaults { get; set; }
        [JsonPropertyName("winners")]        public int Winners { get; set; }
        [JsonPropertyName("forcedErrors")]   public int ForcedErrors { get; set; }
        [JsonPropertyName("unforcedErrors")] public int UnforcedErrors { get; set; }
        [JsonPropertyName("neutralPoints")]  public int NeutralPoints { get; set; }
        [JsonPropertyName("totalPointsWon")] public int TotalPointsWon { get; set; }
    }

    // ── Representación interna de un jugador para la simulación ────────────────
    public class SimPlayer
    {
        public string Name { get; }
        public double Serve { get; }        // capacidad de saque
        public double Rally { get; }        // fondo de pista
        public double Winner { get; }       // capacidad de golpe ganador
        public double Consistency { get; }  // pocos errores no forzados
        public double Stamina { get; }      // aguante físico

        // Atributos resaltados (×1.5) por cada estilo de juego (espejo del cliente)
        private static readonly Dictionary<string, HashSet<string>> StyleHighlights = new()
        {
            ["El Muro"]           = ["Resistencia", "Velocidad", "Concentración", "Anticipación", "Revés"],
            ["Cañonero"]          = ["Fuerza", "Reflejos", "Sangre Fría", "Saque", "Juego en la Red"],
            ["Agresivo de Fondo"] = ["Fuerza", "Velocidad", "Visión de Juego", "Derecha", "Efecto"],
            ["Contraatacante"]    = ["Velocidad", "Reflejos", "Anticipación", "Derecha", "Revés"],
            ["Mago de la Pista"]  = ["Flexibilidad", "Creatividad", "Visión de Juego", "Juego en la Red", "Efecto"],
            ["Francotirador"]     = ["Resistencia", "Sangre Fría", "Concentración", "Visión de Juego", "Revés"],
        };

        public SimPlayer(string name, string style, Dictionary<string, int> attrs)
        {
            Name = name;
            var hl = StyleHighlights.GetValueOrDefault(style, []);

            // Aplica el multiplicador de estilo (×1.5, tope 100) a cada atributo
            double A(string key)
            {
                var raw = attrs.GetValueOrDefault(key, 50);
                var boosted = hl.Contains(key) ? raw * 1.5 : raw;
                return Math.Min(boosted, 100);
            }

            // Combinaciones de atributos → capacidades de simulación
            Serve       = A("Saque") * 0.70 + A("Fuerza") * 0.30;
            Rally       = (A("Derecha") + A("Revés")) / 2 * 0.50 + A("Velocidad") * 0.25 + A("Anticipación") * 0.25;
            Winner      = A("Efecto") * 0.35 + A("Fuerza") * 0.25 + A("Creatividad") * 0.20 + A("Juego en la Red") * 0.20;
            Consistency = A("Concentración") * 0.50 + A("Sangre Fría") * 0.50;
            Stamina     = A("Resistencia");
        }
    }
}