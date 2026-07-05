import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/tournament_models.dart';

class TournamentState {
  final List<TournamentInfo> tournaments;
  final int currentDay;
  final int totalDays;

  const TournamentState({
    required this.tournaments,
    required this.currentDay,
    required this.totalDays,
  });

  List<TournamentInfo> get upcoming =>
      tournaments.where((t) => t.status == TournamentStatus.upcoming).toList();

  TournamentInfo? get current => tournaments
      .where((t) => t.status == TournamentStatus.current)
      .firstOrNull;
}

class TournamentNotifier extends StateNotifier<TournamentState> {
  TournamentNotifier() : super(_initial());

  static TournamentState _initial() => const TournamentState(
    currentDay: 9,
    totalDays: 28,
    tournaments: [
      TournamentInfo(
        name: 'Open de Castilla',
        startDay: 2,
        durationDays: 1,
        surface: 'Pista dura',
        category: TournamentCategory.regular,
        status: TournamentStatus.past,
        dateLabel: '15 jun',
        resultLabel: 'Eliminado en 2ª ronda',
      ),
      TournamentInfo(
        name: 'Grand Slam de Roland Sur',
        startDay: 5,
        durationDays: 2,
        surface: 'Tierra batida',
        category: TournamentCategory.grandSlam,
        status: TournamentStatus.past,
        dateLabel: '18-19 jun',
        resultLabel: 'Cuartos de final',
      ),
      TournamentInfo(
        name: 'Masters de Valencia',
        startDay: 9,
        durationDays: 1,
        surface: 'Tierra batida',
        category: TournamentCategory.regular,
        status: TournamentStatus.current,
        dateLabel: '22 jun',
      ),
      TournamentInfo(
        name: 'Open de Madrid',
        startDay: 16,
        durationDays: 1,
        surface: 'Pista dura',
        category: TournamentCategory.regular,
        status: TournamentStatus.upcoming,
        dateLabel: '29 jun',
      ),
      TournamentInfo(
        name: 'Copa Mediterráneo',
        startDay: 23,
        durationDays: 1,
        surface: 'Tierra batida',
        category: TournamentCategory.regular,
        status: TournamentStatus.upcoming,
        dateLabel: '6 jul',
      ),
      TournamentInfo(
        name: 'Grand Slam Costa Azul',
        startDay: 25,
        durationDays: 2,
        surface: 'Hierba',
        category: TournamentCategory.grandSlam,
        status: TournamentStatus.upcoming,
        dateLabel: '8-9 jul',
      ),
      TournamentInfo(
        name: 'Finales ATP',
        startDay: 27,
        durationDays: 2,
        surface: 'Pista dura',
        category: TournamentCategory.finals,
        status: TournamentStatus.upcoming,
        dateLabel: '10-11 jul',
      ),
    ],
  );
}

final tournamentProvider =
    StateNotifierProvider<TournamentNotifier, TournamentState>(
      (ref) => TournamentNotifier(),
    );
