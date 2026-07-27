using System.Text.Json.Serialization;

namespace TennisApi
{
    // Un participante del cuadro (bot o humano), ya con su fuerza resuelta
    public class Participant
    {
        [JsonPropertyName("id")]        public string Id { get; set; } = "";      // botId o userId
        [JsonPropertyName("name")]      public string Name { get; set; } = "";
        [JsonPropertyName("seed")]      public int Seed { get; set; }             // posición en el ranking (1 = mejor)
        [JsonPropertyName("isHuman")]   public bool IsHuman { get; set; }
        [JsonPropertyName("overall")]   public int Overall { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public SimPlayer? Sim { get; set; }
    }

    // Un enfrentamiento dentro de una ronda
    public class BracketMatch
    {
        [JsonPropertyName("matchIndex")] public int MatchIndex { get; set; }
        [JsonPropertyName("player1")]    public Participant? Player1 { get; set; }
        [JsonPropertyName("player2")]    public Participant? Player2 { get; set; }
        [JsonPropertyName("winnerId")]   public string? WinnerId { get; set; }
        [JsonPropertyName("setsScore")]  public string SetsScore { get; set; } = "";
        [JsonPropertyName("involvesHuman")] public bool InvolvesHuman { get; set; }
    }

    // El resultado de resolver una ronda
    public class RoundResult
    {
        [JsonPropertyName("roundName")]   public string RoundName { get; set; } = "";
        [JsonPropertyName("matches")]     public List<BracketMatch> Matches { get; set; } = [];
        [JsonPropertyName("advancing")]   public List<Participant> Advancing { get; set; } = [];
    }
}