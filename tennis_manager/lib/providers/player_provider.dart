//import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/player_models.dart';
import '../services/api_service.dart';
import '../config.dart';

class PlayerState {
  final PlayerProfile profile;
  final List<PlayerAttribute> physical;
  final List<PlayerAttribute> mental;
  final List<PlayerAttribute> technical;
  final List<PlayerSkill> skills;

  const PlayerState({
    required this.profile,
    required this.physical,
    required this.mental,
    required this.technical,
    required this.skills,
  });

  int get overallRating {
    final all = [...physical, ...mental, ...technical];
    final sum = all.fold<int>(0, (acc, a) => acc + a.value);
    return (sum / all.length).round();
  }

  static List<PlayerAttribute> _parseAttrs(List<dynamic> list) => list
      .map(
        (e) => PlayerAttribute(
          name: e['name'] as String,
          value: e['value'] as int,
        ),
      )
      .toList();

  static List<PlayerSkill> _parseSkills(List<dynamic> list) => list
      .map(
        (e) => PlayerSkill(
          name: e['name'] as String,
          description: e['description'] as String,
          iconName: e['iconName'] as String,
        ),
      )
      .toList();

  factory PlayerState.fromJson(Map<String, dynamic> json) => PlayerState(
    profile: PlayerProfile(
      name: json['name'] as String,
      nationality: json['nationality'] as String,
      nationalityFlag: json['nationalityFlag'] as String,
      age: json['age'] as int,
      heightCm: json['heightCm'] as int,
      weightKg: json['weightKg'] as int,
      dominantHand: json['dominantHand'] as String,
      playingStyle: json['playingStyle'] as String,
      currentEnergy: json['currentEnergy'] as int,
      maxEnergy: json['maxEnergy'] as int,
    ),
    physical: _parseAttrs(json['physical'] as List),
    mental: _parseAttrs(json['mental'] as List),
    technical: _parseAttrs(json['technical'] as List),
    skills: _parseSkills(json['skills'] as List),
  );
}

class PlayerNotifier extends AsyncNotifier<PlayerState> {
  @override
  Future<PlayerState> build() async {
    final json = await ApiService.fetchPlayer(Config.demoUserId);
    return PlayerState.fromJson(json);
  }
}

final playerProvider = AsyncNotifierProvider<PlayerNotifier, PlayerState>(
  PlayerNotifier.new,
);
