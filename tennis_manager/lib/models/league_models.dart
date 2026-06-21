class LeagueInfo {
  final String name;
  final int totalPlayers;
  final int qualificationSlots;
  final String seasonEndsLabel;

  const LeagueInfo({
    required this.name,
    required this.totalPlayers,
    required this.qualificationSlots,
    required this.seasonEndsLabel,
  });
}

class LeagueStanding {
  final int position;
  final String name;
  final int points;
  final bool isCurrentUser;
  final List<bool> recentForm; // true = victoria, false = derrota

  const LeagueStanding({
    required this.position,
    required this.name,
    required this.points,
    this.isCurrentUser = false,
    this.recentForm = const [],
  });
}
