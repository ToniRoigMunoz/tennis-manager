import 'package:flutter/material.dart';
import 'widgets/placeholder_screen.dart';

class GeneralScreen extends StatelessWidget {
  const GeneralScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const PlaceholderScreen(
      icon: Icons.home_rounded,
      title: 'General',
      subtitle:
          'Aquí irá el resumen de tu equipo,\npróximos partidos y notificaciones.',
    );
  }
}
