import 'package:flutter/material.dart';
import '../models/player_models.dart';
import 'widgets/player_switcher.dart';
import 'widgets/player_info_card.dart';
import 'widgets/skills_card.dart';
import 'widgets/attributes_panel.dart';

class PlayerScreen extends StatelessWidget {
  const PlayerScreen({super.key});

  // Datos de ejemplo. Más adelante vendrán de Cosmos DB vía Azure Functions.
  static const _player = PlayerProfile(
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
  );

  static const _physicalAttributes = [
    PlayerAttribute(name: 'Resistencia', value: 68),
    PlayerAttribute(name: 'Velocidad', value: 74),
    PlayerAttribute(name: 'Fuerza', value: 81),
    PlayerAttribute(name: 'Reflejos', value: 65),
    PlayerAttribute(name: 'Flexibilidad', value: 58),
  ];

  static const _mentalAttributes = [
    PlayerAttribute(name: 'Sangre Fría', value: 62),
    PlayerAttribute(name: 'Concentración', value: 70),
    PlayerAttribute(name: 'Visión de Juego', value: 77),
    PlayerAttribute(name: 'Anticipación', value: 66),
    PlayerAttribute(name: 'Creatividad', value: 59),
  ];

  static const _technicalAttributes = [
    PlayerAttribute(name: 'Saque', value: 71),
    PlayerAttribute(name: 'Derecha', value: 83),
    PlayerAttribute(name: 'Revés', value: 60),
    PlayerAttribute(name: 'Juego en la Red', value: 55),
    PlayerAttribute(name: 'Efecto', value: 75),
  ];

  static const _skills = [
    PlayerSkill(
      name: 'Hielo en las Venas',
      description: 'Mejora su Sangre Fría y Saque en bolas de break en contra.',
      icon: Icons.ac_unit_rounded,
    ),
    PlayerSkill(
      name: 'Matagigantes',
      description:
          'Impulso temporal contra rivales muy superiores en el ranking.',
      icon: Icons.bolt_rounded,
    ),
  ];

  int get _overallRating {
    final all = [
      ..._physicalAttributes,
      ..._mentalAttributes,
      ..._technicalAttributes,
    ];
    final sum = all.fold<int>(0, (acc, a) => acc + a.value);
    return (sum / all.length).round();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      children: [
        PlayerSwitcher(
          currentPlayerName: _player.name,
          totalPlayers: 1,
          onTap: () {}, // pendiente: selector de jugadores de la academia
        ),
        const SizedBox(height: 8),
        PlayerInfoCard(player: _player, overallRating: _overallRating),
        const SizedBox(height: 14),
        SkillsCard(skills: _skills),
        const SizedBox(height: 14),
        AttributesPanel(
          physical: _physicalAttributes,
          mental: _mentalAttributes,
          technical: _technicalAttributes,
          playingStyle: _player.playingStyle,
        ),
      ],
    );
  }
}
