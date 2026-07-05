import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/dashboard_models.dart';

class DashboardState {
  final NextMatchInfo? nextMatch;
  final LastMatchInfo? lastMatch;

  const DashboardState({this.nextMatch, this.lastMatch});
}

class DashboardNotifier extends StateNotifier<DashboardState> {
  DashboardNotifier() : super(_initial());

  static DashboardState _initial() => DashboardState(
    nextMatch: NextMatchInfo(
      opponentName: 'Carlos Ferrer',
      tournamentName: 'Masters de Valencia',
      round: 'Octavos de final',
      dateTime: DateTime(2026, 6, 22, 18, 30),
      surface: 'Tierra batida',
    ),
    lastMatch: const LastMatchInfo(
      opponentName: 'Iker Bilbao',
      won: true,
      setsScore: '6-4, 7-5',
      aces: 8,
      winners: 24,
      unforcedErrors: 14,
    ),
  );

  void setNextMatch(NextMatchInfo match) =>
      state = DashboardState(nextMatch: match, lastMatch: state.lastMatch);

  void setLastMatch(LastMatchInfo match) =>
      state = DashboardState(nextMatch: state.nextMatch, lastMatch: match);
}

final dashboardProvider =
    StateNotifierProvider<DashboardNotifier, DashboardState>(
      (ref) => DashboardNotifier(),
    );
