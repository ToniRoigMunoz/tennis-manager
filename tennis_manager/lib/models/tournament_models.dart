enum TournamentCategory { regular, grandSlam, finals }

enum TournamentStatus { past, current, upcoming }

class TournamentInfo {
  final String name;
  final int startDay;
  final int durationDays;
  final String surface;
  final TournamentCategory category;
  final TournamentStatus status;
  final String dateLabel;
  final String? resultLabel; // solo si status == past

  const TournamentInfo({
    required this.name,
    required this.startDay,
    required this.durationDays,
    required this.surface,
    required this.category,
    required this.status,
    required this.dateLabel,
    this.resultLabel,
  });
}
