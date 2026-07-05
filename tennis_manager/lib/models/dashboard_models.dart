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
}
