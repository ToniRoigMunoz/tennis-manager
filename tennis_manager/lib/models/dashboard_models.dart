class NextMatchInfo {
  final String opponentName;
  final String tournamentName;
  final String round;
  final DateTime dateTime;
  final String surface;

  const NextMatchInfo({
    required this.opponentName,
    required this.tournamentName,
    required this.round,
    required this.dateTime,
    required this.surface,
  });

  factory NextMatchInfo.fromJson(Map<String, dynamic> json) => NextMatchInfo(
    opponentName: json['opponentName'] as String,
    tournamentName: json['tournamentName'] as String,
    round: json['round'] as String,
    dateTime: DateTime.parse(json['dateTime'] as String),
    surface: json['surface'] as String,
  );
}

class LastMatchInfo {
  final String opponentName;
  final bool won;
  final String setsScore;
  final int aces;
  final int winners;
  final int unforcedErrors;

  const LastMatchInfo({
    required this.opponentName,
    required this.won,
    required this.setsScore,
    required this.aces,
    required this.winners,
    required this.unforcedErrors,
  });

  factory LastMatchInfo.fromJson(Map<String, dynamic> json) => LastMatchInfo(
    opponentName: json['opponentName'] as String,
    won: json['won'] as bool,
    setsScore: json['setsScore'] as String,
    aces: json['aces'] as int,
    winners: json['winners'] as int,
    unforcedErrors: json['unforcedErrors'] as int,
  );
}
