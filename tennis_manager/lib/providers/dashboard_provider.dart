import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/dashboard_models.dart';
import '../services/api_service.dart';
import '../config.dart';

class DashboardState {
  final int money;
  final int rests;
  final NextMatchInfo? nextMatch;
  final LastMatchInfo? lastMatch;

  const DashboardState({
    required this.money,
    required this.rests,
    this.nextMatch,
    this.lastMatch,
  });

  DashboardState copyWith({
    int? money,
    int? rests,
    NextMatchInfo? nextMatch,
    LastMatchInfo? lastMatch,
  }) => DashboardState(
    money: money ?? this.money,
    rests: rests ?? this.rests,
    nextMatch: nextMatch ?? this.nextMatch,
    lastMatch: lastMatch ?? this.lastMatch,
  );

  factory DashboardState.fromJson(Map<String, dynamic> json) => DashboardState(
    money: json['money'] as int,
    rests: json['rests'] as int,
    nextMatch: json['nextMatch'] != null
        ? NextMatchInfo.fromJson(json['nextMatch'] as Map<String, dynamic>)
        : null,
    lastMatch: json['lastMatch'] != null
        ? LastMatchInfo.fromJson(json['lastMatch'] as Map<String, dynamic>)
        : null,
  );
}

class DashboardNotifier extends AsyncNotifier<DashboardState> {
  @override
  Future<DashboardState> build() async {
    final json = await ApiService.fetchDashboard(Config.demoUserId);
    return DashboardState.fromJson(json);
  }

  // Mutaciones locales — se sincronizan con el servidor cuando hay acción de juego
  void spendMoney(int amount) => state.whenData((d) {
    if (d.money >= amount) {
      state = AsyncData(d.copyWith(money: d.money - amount));
    }
  });

  void earnMoney(int amount) => state.whenData(
    (d) => state = AsyncData(d.copyWith(money: d.money + amount)),
  );

  void spendRest() => state.whenData((d) {
    if (d.rests > 0) state = AsyncData(d.copyWith(rests: d.rests - 1));
  });

  void gainRest() =>
      state.whenData((d) => state = AsyncData(d.copyWith(rests: d.rests + 1)));
}

final dashboardProvider =
    AsyncNotifierProvider<DashboardNotifier, DashboardState>(
      DashboardNotifier.new,
    );
