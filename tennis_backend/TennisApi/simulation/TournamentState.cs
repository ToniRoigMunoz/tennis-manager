using System.Text.Json.Serialization;

namespace TennisApi
{
    public class ActiveTournamentDoc
    {
        [JsonPropertyName("id")]           public string Id { get; set; } = ""; // = leagueId (un torneo activo por liga)
        [JsonPropertyName("userId")]       public string UserId { get; set; } = "";
        [JsonPropertyName("leagueId")]     public string LeagueId { get; set; } = "";
        [JsonPropertyName("tournamentName")] public string TournamentName { get; set; } = "";
        [JsonPropertyName("surface")]      public string Surface { get; set; } = "";
        [JsonPropertyName("seed")]         public int Seed { get; set; }
        [JsonPropertyName("currentRound")] public int CurrentRound { get; set; } // 0 = primera ronda jugada
        [JsonPropertyName("humanAlive")]   public bool HumanAlive { get; set; } = true;
        [JsonPropertyName("humanEliminatedRound")] public int HumanEliminatedRound { get; set; } = -1;
        [JsonPropertyName("finished")]     public bool Finished { get; set; }
        [JsonPropertyName("championId")]   public string? ChampionId { get; set; }

        // Los que siguen vivos y esperan a la siguiente ronda (incluye byes)
        [JsonPropertyName("survivors")]    public List<Participant> Survivors { get; set; } = [];
        // Historial de resultados por ronda, para reconstruir el recorrido del humano
        [JsonPropertyName("history")]      public List<RoundRecord> History { get; set; } = [];
        // Cuántas rondas alcanzó cada participante (para repartir puntos al final)
        [JsonPropertyName("reachedRound")] public Dictionary<string, int> ReachedRound { get; set; } = [];
        [JsonPropertyName("category")] public string Category { get; set; } = "t250";
        [JsonPropertyName("humanRoundIndex")] public int HumanRoundIndex { get; set; } = 1;

        // Multi-humano.
        [JsonPropertyName("humanStates")] public Dictionary<string, HumanTournamentState> HumanStates { get; set; } = [];
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("lastSeenRound")] public int LastSeenRound { get; set; } = 0;

        // Ronda (ventana) de un humano concreto. Lee del diccionario multi-humano. Si no estuviera, cae al campo singular viejo (seguridad durante la transición).
        public int RoundIndexOf(string userId)
        {
            if (HumanStates.TryGetValue(userId, out var hs)) return hs.RoundIndex;
            return HumanRoundIndex;
        }

        // ¿Sigue vivo un humano concreto? Lee del diccionario; respaldo al campo viejo.
        public bool IsAlive(string userId)
        {
            if (HumanStates.TryGetValue(userId, out var hs)) return hs.Alive;
            return HumanAlive;
        }
    }

    public class RoundRecord
    {
        [JsonPropertyName("roundName")] public string RoundName { get; set; } = "";
        [JsonPropertyName("results")]   public List<MatchRecord> Results { get; set; } = [];
    }

    public class MatchRecord
    {
        [JsonPropertyName("p1Name")]   public string P1Name { get; set; } = "";
        [JsonPropertyName("p2Name")]   public string P2Name { get; set; } = "";
        [JsonPropertyName("winnerId")] public string WinnerId { get; set; } = "";
        [JsonPropertyName("winnerName")] public string WinnerName { get; set; } = "";
        [JsonPropertyName("setsScore")] public string SetsScore { get; set; } = "";
        [JsonPropertyName("involvesHuman")] public bool InvolvesHuman { get; set; }
    }

    // Estado de un humano dentro del torneo (para multi-humano). De momento se rellena en paralelo a los campos singulares, sin usarse aún.
    public class HumanTournamentState
    {
        [JsonPropertyName("userId")]           public string UserId { get; set; } = "";
        [JsonPropertyName("alive")]            public bool Alive { get; set; } = true;
        [JsonPropertyName("eliminatedRound")]  public int EliminatedRound { get; set; } = -1;
        [JsonPropertyName("roundIndex")]       public int RoundIndex { get; set; } = 1;
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }
}