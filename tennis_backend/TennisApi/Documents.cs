using System.Text.Json.Serialization;

namespace TennisApi
{
    // ── USERS — solo datos de cuenta ─────────────────────────────────────────
    public class UserDocument
    {
        [JsonPropertyName("id")]       public string Id { get; set; } = "";
        [JsonPropertyName("money")]    public int Money { get; set; }
        [JsonPropertyName("rests")]    public int Rests { get; set; }
        [JsonPropertyName("leagueId")] public string LeagueId { get; set; } = "";
        [JsonPropertyName("seasonId")] public string SeasonId { get; set; } = "";
    }

    // ── PLAYERS — perfil completo del jugador, incluyendo partidos ────────────
    public class PlayerDocument
    {
        [JsonPropertyName("id")]              public string Id { get; set; } = "";
        [JsonPropertyName("userId")]          public string UserId { get; set; } = "";
        [JsonPropertyName("name")]            public string Name { get; set; } = "";
        [JsonPropertyName("nationality")]     public string Nationality { get; set; } = "";
        [JsonPropertyName("nationalityFlag")] public string NationalityFlag { get; set; } = "";
        [JsonPropertyName("age")]             public int Age { get; set; }
        [JsonPropertyName("heightCm")]        public int HeightCm { get; set; }
        [JsonPropertyName("weightKg")]        public int WeightKg { get; set; }
        [JsonPropertyName("dominantHand")]    public string DominantHand { get; set; } = "";
        [JsonPropertyName("playingStyle")]    public string PlayingStyle { get; set; } = "";
        [JsonPropertyName("currentEnergy")]   public int CurrentEnergy { get; set; }
        [JsonPropertyName("maxEnergy")]       public int MaxEnergy { get; set; }
        [JsonPropertyName("physical")]        public List<AttributeDoc> Physical { get; set; } = [];
        [JsonPropertyName("mental")]          public List<AttributeDoc> Mental { get; set; } = [];
        [JsonPropertyName("technical")]       public List<AttributeDoc> Technical { get; set; } = [];
        [JsonPropertyName("skills")]          public List<SkillDoc> Skills { get; set; } = [];
        // Los partidos viven aquí: pertenecen al jugador, no a la cuenta
        [JsonPropertyName("nextMatch")]       public MatchDoc? NextMatch { get; set; }
        [JsonPropertyName("lastMatch")]       public MatchResultDoc? LastMatch { get; set; }
        [JsonPropertyName("attributeProgress")] public double AttributeProgress { get; set; }
    }

    public class MatchDoc
    {
        [JsonPropertyName("opponentName")]   public string OpponentName { get; set; } = "";
        [JsonPropertyName("tournamentName")] public string TournamentName { get; set; } = "";
        [JsonPropertyName("round")]          public string Round { get; set; } = "";
        [JsonPropertyName("dateTime")]       public string DateTime { get; set; } = "";
        [JsonPropertyName("surface")]        public string Surface { get; set; } = "";
    }

    public class MatchResultDoc
    {
        [JsonPropertyName("opponentName")]   public string OpponentName { get; set; } = "";
        [JsonPropertyName("won")]            public bool Won { get; set; }
        [JsonPropertyName("setsScore")]      public string SetsScore { get; set; } = "";
        [JsonPropertyName("aces")]           public int Aces { get; set; }
        [JsonPropertyName("winners")]        public int Winners { get; set; }
        [JsonPropertyName("unforcedErrors")] public int UnforcedErrors { get; set; }
    }

    public class AttributeDoc
    {
        [JsonPropertyName("name")]  public string Name { get; set; } = "";
        [JsonPropertyName("value")] public int Value { get; set; }
    }

    public class SkillDoc
    {
        [JsonPropertyName("name")]        public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("iconName")]    public string IconName { get; set; } = "";
    }

    // ── LEAGUES ───────────────────────────────────────────────────────────────
    public class LeagueDocument
    {
        [JsonPropertyName("id")]                 public string Id { get; set; } = "";
        [JsonPropertyName("leagueId")]           public string LeagueId { get; set; } = "";
        [JsonPropertyName("name")]               public string Name { get; set; } = "";
        [JsonPropertyName("totalPlayers")]       public int TotalPlayers { get; set; }
        [JsonPropertyName("qualificationSlots")] public int QualificationSlots { get; set; }
        [JsonPropertyName("seasonEndsLabel")]    public string SeasonEndsLabel { get; set; } = "";
        [JsonPropertyName("standings")]          public List<StandingDoc> Standings { get; set; } = [];
        [JsonPropertyName("tier")] public int Tier { get; set; } = 1;
    }

    public class StandingDoc
    {
        [JsonPropertyName("position")]   public int Position { get; set; }
        [JsonPropertyName("name")]       public string Name { get; set; } = "";
        [JsonPropertyName("points")]     public int Points { get; set; }
        // userId es la referencia real; name es un caché que GetLeagueData sobreescribe en lectura
        [JsonPropertyName("userId")]     public string? UserId { get; set; }
        [JsonPropertyName("botId")]      public string? BotId { get; set; }
        [JsonPropertyName("recentForm")] public List<bool> RecentForm { get; set; } = [];
    }

    // ── TOURNAMENTS ───────────────────────────────────────────────────────────
    public class TournamentDocument
    {
        [JsonPropertyName("id")]          public string Id { get; set; } = "";
        [JsonPropertyName("seasonId")]    public string SeasonId { get; set; } = "";
        [JsonPropertyName("currentDay")]  public int CurrentDay { get; set; }
        [JsonPropertyName("totalDays")]   public int TotalDays { get; set; }
        [JsonPropertyName("tournaments")] public List<TournamentEntry> Tournaments { get; set; } = [];
        [JsonPropertyName("seasonNumber")] public int SeasonNumber { get; set; } = 1;
        [JsonPropertyName("currentDayStartUtc")] public string CurrentDayStartUtc { get; set; } = "";
        [JsonPropertyName("devTimeOffsetSeconds")] public long DevTimeOffsetSeconds { get; set; }
        [JsonPropertyName("roundIntervalMinutes")] public int RoundIntervalMinutes { get; set; } = 120;
    }

    public class TournamentEntry
    {
        [JsonPropertyName("name")]         public string Name { get; set; } = "";
        [JsonPropertyName("startDay")]     public int StartDay { get; set; }
        [JsonPropertyName("durationDays")] public int DurationDays { get; set; }
        [JsonPropertyName("surface")]      public string Surface { get; set; } = "";
        [JsonPropertyName("category")]     public string Category { get; set; } = "";
        [JsonPropertyName("status")]       public string Status { get; set; } = "";
        [JsonPropertyName("dateLabel")]    public string DateLabel { get; set; } = "";
        [JsonPropertyName("resultLabel")]  public string? ResultLabel { get; set; }
    }

    // ── BOTS — jugadores controlados por el sistema ───────────────────────────
    public class BotDocument
    {
        [JsonPropertyName("id")]              public string Id { get; set; } = "";
        [JsonPropertyName("leagueId")]        public string LeagueId { get; set; } = "";
        [JsonPropertyName("name")]            public string Name { get; set; } = "";
        [JsonPropertyName("nationality")]     public string Nationality { get; set; } = "";
        [JsonPropertyName("nationalityFlag")] public string NationalityFlag { get; set; } = "";
        [JsonPropertyName("playingStyle")]    public string PlayingStyle { get; set; } = "";
        [JsonPropertyName("overall")]         public int Overall { get; set; }
        [JsonPropertyName("physical")]        public List<AttributeDoc> Physical { get; set; } = [];
        [JsonPropertyName("mental")]          public List<AttributeDoc> Mental { get; set; } = [];
        [JsonPropertyName("technical")]       public List<AttributeDoc> Technical { get; set; } = [];
        [JsonPropertyName("attributeProgress")] public double AttributeProgress { get; set; }
    }
}