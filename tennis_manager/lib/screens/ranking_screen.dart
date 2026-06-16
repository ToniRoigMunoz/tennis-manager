import 'package:flutter/material.dart';
import 'widgets/placeholder_screen.dart';

class RankingScreen extends StatelessWidget {
  const RankingScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const PlaceholderScreen(
      icon: Icons.leaderboard_rounded,
      title: 'Ranking',
      subtitle: 'Aquí verás la clasificación\nde tu liga y tus rivales.',
    );
  }
}
