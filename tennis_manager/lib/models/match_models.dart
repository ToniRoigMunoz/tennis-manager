class MatchSimulation {
  final String player1Name;
  final String player2Name;
  final int bestOf;
  final int winner;
  final String setsScore;
  final List<PointEvent> points;
  final MatchStats stats1;
  final MatchStats stats2;

  const MatchSimulation({
    required this.player1Name,
    required this.player2Name,
    required this.bestOf,
    required this.winner,
    required this.setsScore,
    required this.points,
    required this.stats1,
    required this.stats2,
  });

  factory MatchSimulation.fromJson(Map<String, dynamic> json) =>
      MatchSimulation(
        player1Name: json['player1Name'] as String,
        player2Name: json['player2Name'] as String,
        bestOf: json['bestOf'] as int,
        winner: json['winner'] as int,
        setsScore: json['setsScore'] as String,
        points: (json['points'] as List)
            .map((e) => PointEvent.fromJson(e as Map<String, dynamic>))
            .toList(),
        stats1: MatchStats.fromJson(json['stats1'] as Map<String, dynamic>),
        stats2: MatchStats.fromJson(json['stats2'] as Map<String, dynamic>),
      );
}

class PointEvent {
  final int server;
  final int winner;
  final String
  outcome; // ace | doubleFault | winner | forcedError | unforcedError | neutral
  final String p1GameScore;
  final String p2GameScore;
  final int p1Games;
  final int p2Games;
  final int p1Sets;
  final int p2Sets;
  final int setIndex;
  final bool isTiebreak;
  final bool isSetPoint;
  final bool isMatchPoint;
  final bool isGameOver;
  final bool isSetOver;

  const PointEvent({
    required this.server,
    required this.winner,
    required this.outcome,
    required this.p1GameScore,
    required this.p2GameScore,
    required this.p1Games,
    required this.p2Games,
    required this.p1Sets,
    required this.p2Sets,
    required this.setIndex,
    required this.isTiebreak,
    required this.isSetPoint,
    required this.isMatchPoint,
    required this.isGameOver,
    required this.isSetOver,
  });

  factory PointEvent.fromJson(Map<String, dynamic> json) => PointEvent(
    server: json['server'] as int,
    winner: json['winner'] as int,
    outcome: json['outcome'] as String,
    p1GameScore: json['p1GameScore'] as String,
    p2GameScore: json['p2GameScore'] as String,
    p1Games: json['p1Games'] as int,
    p2Games: json['p2Games'] as int,
    p1Sets: json['p1Sets'] as int,
    p2Sets: json['p2Sets'] as int,
    setIndex: json['setIndex'] as int,
    isTiebreak: json['isTiebreak'] as bool,
    isSetPoint: json['isSetPoint'] as bool,
    isMatchPoint: json['isMatchPoint'] as bool,
    isGameOver: json['isGameOver'] as bool,
    isSetOver: json['isSetOver'] as bool,
  );
}

class MatchStats {
  final int aces;
  final int doubleFaults;
  final int winners;
  final int forcedErrors;
  final int unforcedErrors;
  final int neutralPoints;
  final int totalPointsWon;

  const MatchStats({
    required this.aces,
    required this.doubleFaults,
    required this.winners,
    required this.forcedErrors,
    required this.unforcedErrors,
    required this.neutralPoints,
    required this.totalPointsWon,
  });

  factory MatchStats.fromJson(Map<String, dynamic> json) => MatchStats(
    aces: json['aces'] as int,
    doubleFaults: json['doubleFaults'] as int,
    winners: json['winners'] as int,
    forcedErrors: json['forcedErrors'] as int,
    unforcedErrors: json['unforcedErrors'] as int,
    neutralPoints: json['neutralPoints'] as int,
    totalPointsWon: json['totalPointsWon'] as int,
  );
}
