import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/league_models.dart';
import '../services/api_service.dart';
import '../config.dart';

class LeagueState {
  final LeagueInfo info;
  final List<LeagueStanding> standings;

  const LeagueState({required this.info, required this.standings});

  int get userPosition => standings.firstWhere((s) => s.isCurrentUser).position;

  List<LeagueStanding> dashboardSummary({int count = 5}) {
    if (standings.length <= count) return standings;
    final userIdx = standings.indexWhere((s) => s.isCurrentUser);
    if (userIdx < count) return standings.take(count).toList();
    return [...standings.take(count - 1), standings[userIdx]];
  }

  factory LeagueState.fromJson(Map<String, dynamic> json) {
    final standingsList = (json['standings'] as List).map((e) {
      final map = e as Map<String, dynamic>;
      final userId = map['userId'] as String?;
      return LeagueStanding(
        position: map['position'] as int,
        name: map['name'] as String,
        points: map['points'] as int,
        isCurrentUser: userId == Config.demoUserId,
        recentForm: (map['recentForm'] as List).cast<bool>(),
      );
    }).toList();

    return LeagueState(
      info: LeagueInfo(
        name: json['name'] as String,
        totalPlayers: json['totalPlayers'] as int,
        qualificationSlots: json['qualificationSlots'] as int,
        seasonEndsLabel: json['seasonEndsLabel'] as String,
      ),
      standings: standingsList,
    );
  }
}

class LeagueNotifier extends AsyncNotifier<LeagueState> {
  @override
  Future<LeagueState> build() async {
    final json = await ApiService.fetchLeague(Config.demoLeagueId);
    return LeagueState.fromJson(json);
  }
}

final leagueProvider = AsyncNotifierProvider<LeagueNotifier, LeagueState>(
  LeagueNotifier.new,
);
