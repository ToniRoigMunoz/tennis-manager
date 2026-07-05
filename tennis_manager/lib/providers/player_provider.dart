import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/player_models.dart';

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

  PlayerState withEnergy(int energy) => PlayerState(
    profile: PlayerProfile(
      name: profile.name,
      nationality: profile.nationality,
      nationalityFlag: profile.nationalityFlag,
      age: profile.age,
      heightCm: profile.heightCm,
      weightKg: profile.weightKg,
      dominantHand: profile.dominantHand,
      playingStyle: profile.playingStyle,
      currentEnergy: energy.clamp(0, profile.maxEnergy),
      maxEnergy: profile.maxEnergy,
    ),
    physical: physical,
    mental: mental,
    technical: technical,
    skills: skills,
  );
}

class PlayerNotifier extends StateNotifier<PlayerState> {
  PlayerNotifier() : super(_initial());

  static PlayerState _initial() => const PlayerState(
    profile: PlayerProfile(
      name: 'Toni Roig',
      nationality: 'España',
      nationalityFlag: '🇪🇸',
      age: 22,
      heightCm: 185,
      weightKg: 78,
      dominantHand: 'Diestro',
      playingStyle: 'Agresivo de Fondo',
      currentEnergy: 64,
      maxEnergy: 100,
    ),
    physical: [
      PlayerAttribute(name: 'Resistencia', value: 68),
      PlayerAttribute(name: 'Velocidad', value: 74),
      PlayerAttribute(name: 'Fuerza', value: 81),
      PlayerAttribute(name: 'Reflejos', value: 65),
      PlayerAttribute(name: 'Flexibilidad', value: 58),
    ],
    mental: [
      PlayerAttribute(name: 'Sangre Fría', value: 62),
      PlayerAttribute(name: 'Concentración', value: 70),
      PlayerAttribute(name: 'Visión de Juego', value: 77),
      PlayerAttribute(name: 'Anticipación', value: 66),
      PlayerAttribute(name: 'Creatividad', value: 59),
    ],
    technical: [
      PlayerAttribute(name: 'Saque', value: 71),
      PlayerAttribute(name: 'Derecha', value: 83),
      PlayerAttribute(name: 'Revés', value: 60),
      PlayerAttribute(name: 'Juego en la Red', value: 55),
      PlayerAttribute(name: 'Efecto', value: 75),
    ],
    skills: [
      PlayerSkill(
        name: 'Hielo en las Venas',
        description:
            'Mejora su Sangre Fría y Saque en bolas de break en contra.',
        icon: Icons.ac_unit_rounded,
      ),
      PlayerSkill(
        name: 'Matagigantes',
        description:
            'Impulso temporal contra rivales muy superiores en el ranking.',
        icon: Icons.bolt_rounded,
      ),
    ],
  );

  void updateEnergy(int energy) => state = state.withEnergy(energy);
}

final playerProvider = StateNotifierProvider<PlayerNotifier, PlayerState>(
  (ref) => PlayerNotifier(),
);
