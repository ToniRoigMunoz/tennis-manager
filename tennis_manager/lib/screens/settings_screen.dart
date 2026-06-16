import 'package:flutter/material.dart';
import 'widgets/placeholder_screen.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const PlaceholderScreen(
      icon: Icons.settings_rounded,
      title: 'Ajustes',
      subtitle:
          'Aquí irán las opciones de perfil,\ncuenta y preferencias del juego.',
    );
  }
}
