import 'package:flutter_riverpod/flutter_riverpod.dart';

class UserResources {
  final int money;
  final int rests;

  const UserResources({required this.money, required this.rests});

  UserResources copyWith({int? money, int? rests}) =>
      UserResources(money: money ?? this.money, rests: rests ?? this.rests);
}

class UserResourcesNotifier extends StateNotifier<UserResources> {
  UserResourcesNotifier() : super(const UserResources(money: 1250, rests: 3));

  void spendMoney(int amount) {
    if (state.money >= amount) {
      state = state.copyWith(money: state.money - amount);
    }
  }

  void earnMoney(int amount) =>
      state = state.copyWith(money: state.money + amount);

  void spendRest() {
    if (state.rests > 0) state = state.copyWith(rests: state.rests - 1);
  }

  void gainRest() => state = state.copyWith(rests: state.rests + 1);
}

final userResourcesProvider =
    StateNotifierProvider<UserResourcesNotifier, UserResources>(
      (ref) => UserResourcesNotifier(),
    );
