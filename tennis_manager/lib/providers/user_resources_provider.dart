import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../services/api_service.dart';
import '../config.dart';

class UserDataState {
  final int money;
  final int rests;
  final String leagueId;
  final String seasonId;

  const UserDataState({
    required this.money,
    required this.rests,
    required this.leagueId,
    required this.seasonId,
  });

  UserDataState copyWith({int? money, int? rests}) => UserDataState(
    money: money ?? this.money,
    rests: rests ?? this.rests,
    leagueId: leagueId,
    seasonId: seasonId,
  );

  factory UserDataState.fromJson(Map<String, dynamic> json) => UserDataState(
    money: json['money'] as int,
    rests: json['rests'] as int,
    leagueId: json['leagueId'] as String,
    seasonId: json['seasonId'] as String,
  );
}

class UserDataNotifier extends AsyncNotifier<UserDataState> {
  @override
  Future<UserDataState> build() async {
    final json = await ApiService.fetchUserData(Config.demoUserId);
    return UserDataState.fromJson(json);
  }

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

final userDataProvider = AsyncNotifierProvider<UserDataNotifier, UserDataState>(
  UserDataNotifier.new,
);

// Provider de conveniencia para el AppBar — no cambia nada en main.dart
final userResourcesProvider = Provider((ref) {
  final ud = ref.watch(userDataProvider);
  return (money: ud.valueOrNull?.money ?? 0, rests: ud.valueOrNull?.rests ?? 0);
});
