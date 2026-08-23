// Respuesta de StartTournament / AdvanceTournament
class TournamentStep {
  final String status; // "humanPlays" | "finished"
  final String? tournamentName;
  final String? surface;
  final String? roundName;
  final String? unlockUtc; // hora UTC de desbloqueo (solo en waitingForRound)
  final TournamentOpponent? opponent;

  // Solo cuando status == "finished"
  final bool? humanWonTournament;
  final int? humanEliminatedRound;
  final String? championName;
  final TournamentRewards? rewards;
  final List<BracketRound> history;
  final String? nextTournamentName; // en seasonDayDone

  const TournamentStep({
    required this.status,
    this.tournamentName,
    this.surface,
    this.roundName,
    this.unlockUtc,
    this.opponent,
    this.humanWonTournament,
    this.humanEliminatedRound,
    this.championName,
    this.rewards,
    this.history = const [],
    this.nextTournamentName,
  });

  bool get isFinished => status == 'finished';
  bool get humanPlays => status == 'humanPlays';
  bool get isWaitingForRound => status == 'waitingForRound';
  bool get isNoPendingMatch => status == 'noPendingMatch';
  bool get isSeasonDayDone => status == 'seasonDayDone';

  // Hora de desbloqueo convertida a la zona horaria local del dispositivo
  DateTime? get unlockLocalTime {
    if (unlockUtc == null) return null;
    return DateTime.parse(unlockUtc!).toLocal();
  }

  factory TournamentStep.fromJson(Map<String, dynamic> json) => TournamentStep(
    status: json['status'] as String,
    tournamentName: json['tournamentName'] as String?,
    surface: json['surface'] as String?,
    roundName: json['roundName'] as String?,
    unlockUtc: json['unlockUtc'] as String?,
    opponent: json['opponent'] != null
        ? TournamentOpponent.fromJson(json['opponent'] as Map<String, dynamic>)
        : null,
    humanWonTournament: json['humanWonTournament'] as bool?,
    humanEliminatedRound: json['humanEliminatedRound'] as int?,
    championName: json['championName'] as String?,
    rewards: json['rewards'] != null
        ? TournamentRewards.fromJson(json['rewards'] as Map<String, dynamic>)
        : null,
    history:
        (json['history'] as List?)
            ?.map((e) => BracketRound.fromJson(e as Map<String, dynamic>))
            .toList() ??
        const [],
    nextTournamentName: json['nextTournamentName'] as String?,
  );
}

class TournamentOpponent {
  final String id;
  final String name;
  final int overall;

  const TournamentOpponent({
    required this.id,
    required this.name,
    required this.overall,
  });

  factory TournamentOpponent.fromJson(Map<String, dynamic> json) =>
      TournamentOpponent(
        id: json['id'] as String,
        name: json['name'] as String,
        overall: (json['overall'] as int?) ?? 70,
      );
}

class BracketRound {
  final String roundName;
  final List<BracketMatchResult> results;

  const BracketRound({required this.roundName, required this.results});

  factory BracketRound.fromJson(Map<String, dynamic> json) => BracketRound(
    roundName: json['roundName'] as String,
    results: (json['results'] as List)
        .map((e) => BracketMatchResult.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

class BracketMatchResult {
  final String p1Name;
  final String p2Name;
  final String winnerName;
  final String setsScore;
  final bool involvesHuman;

  const BracketMatchResult({
    required this.p1Name,
    required this.p2Name,
    required this.winnerName,
    required this.setsScore,
    required this.involvesHuman,
  });

  factory BracketMatchResult.fromJson(Map<String, dynamic> json) =>
      BracketMatchResult(
        p1Name: json['p1Name'] as String,
        p2Name: json['p2Name'] as String,
        winnerName: json['winnerName'] as String,
        setsScore: json['setsScore'] as String,
        involvesHuman: json['involvesHuman'] as bool,
      );
}

class TournamentRewards {
  final int pointsEarned;
  final int moneyEarned;
  final int restsEarned;
  final int attributePointsApplied;
  final double attributeProgress;
  final bool isChampion;

  const TournamentRewards({
    required this.pointsEarned,
    required this.moneyEarned,
    required this.restsEarned,
    required this.attributePointsApplied,
    required this.attributeProgress,
    required this.isChampion,
  });

  factory TournamentRewards.fromJson(Map<String, dynamic> json) =>
      TournamentRewards(
        pointsEarned: json['pointsEarned'] as int,
        moneyEarned: json['moneyEarned'] as int,
        restsEarned: json['restsEarned'] as int,
        attributePointsApplied: json['attributePointsApplied'] as int,
        attributeProgress: (json['attributeProgress'] as num).toDouble(),
        isChampion: json['isChampion'] as bool,
      );
}

class SeasonEndResult {
  final String championPrimera;
  final String championSegunda;
  final String championTercera;
  final String humanMovement;

  const SeasonEndResult({
    required this.championPrimera,
    required this.championSegunda,
    required this.championTercera,
    required this.humanMovement,
  });

  factory SeasonEndResult.fromJson(Map<String, dynamic> json) {
    final champions = json['champions'] as Map<String, dynamic>? ?? {};
    return SeasonEndResult(
      championPrimera: champions['primera'] as String? ?? '—',
      championSegunda: champions['segunda'] as String? ?? '—',
      championTercera: champions['tercera'] as String? ?? '—',
      humanMovement:
          json['humanMovement'] as String? ?? 'se mantiene en su división',
    );
  }

  // Clasifica el movimiento para pintar el color/icono adecuado
  bool get isPromotion => humanMovement.contains('asciende');
  bool get isRelegation => humanMovement.contains('desciende');
  bool get isStay => !isPromotion && !isRelegation;
}
