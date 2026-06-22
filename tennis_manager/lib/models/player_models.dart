import 'package:flutter/material.dart';

class PlayerProfile {
  final String name;
  final String nationality;
  final String nationalityFlag;
  final int age;
  final int heightCm;
  final int weightKg;
  final String dominantHand;
  final String playingStyle;
  final int currentEnergy;
  final int maxEnergy;

  const PlayerProfile({
    required this.name,
    required this.nationality,
    required this.nationalityFlag,
    required this.age,
    required this.heightCm,
    required this.weightKg,
    required this.dominantHand,
    required this.playingStyle,
    required this.currentEnergy,
    required this.maxEnergy,
  });
}

class PlayerAttribute {
  final String name;
  final int value; // 1-100

  const PlayerAttribute({required this.name, required this.value});
}

class PlayerSkill {
  final String name;
  final String description;
  final IconData icon;

  const PlayerSkill({
    required this.name,
    required this.description,
    required this.icon,
  });
}

class PlayerStyles {
  // Atributos resaltados ("habilidad blanca", ×1.5) por cada estilo de juego.
  // Nota: "Táctica" del documento se ha mapeado a "Visión de Juego" (pendiente de confirmación).
  static const Map<String, Set<String>> highlightedAttributes = {
    'El Muro': {
      'Resistencia',
      'Velocidad',
      'Concentración',
      'Anticipación',
      'Revés',
    },
    'Cañonero': {
      'Fuerza',
      'Reflejos',
      'Sangre Fría',
      'Saque',
      'Juego en la Red',
    },
    'Agresivo de Fondo': {
      'Fuerza',
      'Velocidad',
      'Visión de Juego',
      'Derecha',
      'Efecto',
    },
    'Contraatacante': {
      'Velocidad',
      'Reflejos',
      'Anticipación',
      'Derecha',
      'Revés',
    },
    'Mago de la Pista': {
      'Flexibilidad',
      'Creatividad',
      'Visión de Juego',
      'Juego en la Red',
      'Efecto',
    },
    'Francotirador': {
      'Resistencia',
      'Sangre Fría',
      'Concentración',
      'Visión de Juego',
      'Revés',
    },
  };

  static bool isHighlighted(String style, String attribute) {
    return highlightedAttributes[style]?.contains(attribute) ?? false;
  }
}
