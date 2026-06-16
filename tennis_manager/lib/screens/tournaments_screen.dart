import 'package:flutter/material.dart';
import 'widgets/placeholder_screen.dart';

class TournamentsScreen extends StatelessWidget {
  const TournamentsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const PlaceholderScreen(
      icon: Icons.emoji_events_rounded,
      title: 'Torneos',
      subtitle:
          'Aquí podrás inscribirte en torneos\ny seguir el calendario de partidos.',
    );
  }
}
