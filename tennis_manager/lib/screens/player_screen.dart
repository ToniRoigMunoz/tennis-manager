import 'package:flutter/material.dart';
import 'widgets/placeholder_screen.dart';

class PlayerScreen extends StatelessWidget {
  const PlayerScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const PlaceholderScreen(
      icon: Icons.person_rounded,
      title: 'Jugador',
      subtitle: 'Aquí verás las estadísticas y\nel progreso de tu jugador.',
    );
  }
}
