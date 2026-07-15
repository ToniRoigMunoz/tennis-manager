import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/tournament_models.dart';
import '../services/api_service.dart';
import 'user_resources_provider.dart';

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

  factory TournamentState.fromJson(Map<String, dynamic> json) {
    final list = (json['tournaments'] as List).map((e) {
      final map = e as Map<String, dynamic>;
      return TournamentInfo(
        name: map['name'] as String,
        startDay: map['startDay'] as int,
        durationDays: map['durationDays'] as int,
        surface: map['surface'] as String,
        dateLabel: map['dateLabel'] as String,
        resultLabel: map['resultLabel'] as String?,
        category: switch (map['category'] as String) {
          'grandSlam' => TournamentCategory.grandSlam,
          'finals' => TournamentCategory.finals,
          _ => TournamentCategory.regular,
        },
        status: switch (map['status'] as String) {
          'past' => TournamentStatus.past,
          'current' => TournamentStatus.current,
          _ => TournamentStatus.upcoming,
        },
      );
    }).toList();

    return TournamentState(
      tournaments: list,
      currentDay: json['currentDay'] as int,
      totalDays: json['totalDays'] as int,
    );
  }
}

class TournamentNotifier extends AsyncNotifier<TournamentState> {
  @override
  Future<TournamentState> build() async {
    final userData = await ref.watch(userDataProvider.future);
    final json = await ApiService.fetchTournaments(userData.seasonId);
    return TournamentState.fromJson(json);
  }
}

final tournamentProvider =
    AsyncNotifierProvider<TournamentNotifier, TournamentState>(
      TournamentNotifier.new,
    );
