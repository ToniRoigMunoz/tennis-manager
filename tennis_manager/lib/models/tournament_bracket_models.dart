// Respuesta de StartTournament / AdvanceTournament
class TournamentStep {
  final String status; // "humanPlays" | "finished"
  final String? tournamentName;
  final String? surface;
  final String? roundName;
  final TournamentOpponent? opponent;

  // Solo cuando status == "finished"
  final bool? humanWonTournament;
  final int? humanEliminatedRound;
  final String? championName;
  final List<BracketRound> history;

  const TournamentStep({
    required this.status,
    this.tournamentName,
    this.surface,
    this.roundName,
    this.opponent,
    this.humanWonTournament,
    this.humanEliminatedRound,
    this.championName,
    this.history = const [],
  });

  bool get isFinished => status == 'finished';
  bool get humanPlays => status == 'humanPlays';

  factory TournamentStep.fromJson(Map<String, dynamic> json) => TournamentStep(
    status: json['status'] as String,
    tournamentName: json['tournamentName'] as String?,
    surface: json['surface'] as String?,
    roundName: json['roundName'] as String?,
    opponent: json['opponent'] != null
        ? TournamentOpponent.fromJson(json['opponent'] as Map<String, dynamic>)
        : null,
    humanWonTournament: json['humanWonTournament'] as bool?,
    humanEliminatedRound: json['humanEliminatedRound'] as int?,
    championName: json['championName'] as String?,
    history:
        (json['history'] as List?)
            ?.map((e) => BracketRound.fromJson(e as Map<String, dynamic>))
            .toList() ??
        const [],
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
