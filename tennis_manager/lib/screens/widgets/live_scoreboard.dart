import 'package:flutter/material.dart';
import '../../models/match_models.dart';

class LiveScoreboard extends StatelessWidget {
  final MatchSimulation match;
  final int currentIndex;

  const LiveScoreboard({
    super.key,
    required this.match,
    required this.currentIndex,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    // Sets ya cerrados hasta el punto actual
    final completed = <List<int>>[];
    for (int i = 0; i <= currentIndex && i < match.points.length; i++) {
      final p = match.points[i];
      if (p.isSetOver) completed.add([p.p1Games, p.p2Games]);
    }

    final p = currentIndex >= 0 && currentIndex < match.points.length
        ? match.points[currentIndex]
        : null;

    // Si el último punto cerró set, el set en curso aún no ha empezado
    final showCurrentSet = p != null && !p.isSetOver;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.primary,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          _row(
            context,
            name: match.player1Name,
            isServing: p?.server == 1,
            sets: completed.map((s) => s[0]).toList(),
            currentGames: showCurrentSet ? p.p1Games : null,
            gameScore: showCurrentSet ? p.p1GameScore : null,
          ),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Divider(
              height: 1,
              color: Colors.white.withValues(alpha: 0.25),
            ),
          ),
          _row(
            context,
            name: match.player2Name,
            isServing: p?.server == 2,
            sets: completed.map((s) => s[1]).toList(),
            currentGames: showCurrentSet ? p.p2Games : null,
            gameScore: showCurrentSet ? p.p2GameScore : null,
          ),
          if (p != null && p.isTiebreak) ...[
            const SizedBox(height: 10),
            _badge('TIE-BREAK', Colors.orangeAccent),
          ],
          if (p != null && p.isSetPoint && !p.isSetOver) ...[
            const SizedBox(height: 10),
            _badge('PUNTO DE SET', Colors.amberAccent),
          ],
        ],
      ),
    );
  }

  static Widget _badge(String text, Color color) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
    decoration: BoxDecoration(
      color: color,
      borderRadius: BorderRadius.circular(20),
    ),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 10,
        fontWeight: FontWeight.bold,
        color: Colors.black87,
      ),
    ),
  );

  Widget _row(
    BuildContext context, {
    required String name,
    required bool isServing,
    required List<int> sets,
    required int? currentGames,
    required String? gameScore,
  }) {
    return Row(
      children: [
        SizedBox(
          width: 16,
          child: isServing
              ? const Icon(
                  Icons.sports_baseball_rounded,
                  size: 12,
                  color: Colors.amberAccent,
                )
              : null,
        ),
        Expanded(
          child: Text(
            name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w600,
              fontSize: 15,
            ),
          ),
        ),
        // Sets cerrados
        ...sets.map(
          (g) => Padding(
            padding: const EdgeInsets.symmetric(horizontal: 5),
            child: Text(
              '$g',
              style: TextStyle(
                color: Colors.white.withValues(alpha: 0.65),
                fontWeight: FontWeight.w600,
                fontSize: 16,
              ),
            ),
          ),
        ),
        // Set en curso
        if (currentGames != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 5),
            child: Text(
              '$currentGames',
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 18,
              ),
            ),
          ),
        // Puntos del juego actual
        Container(
          width: 46,
          margin: const EdgeInsets.only(left: 8),
          padding: const EdgeInsets.symmetric(vertical: 4),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.18),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(
            gameScore ?? '-',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.bold,
              fontSize: 16,
            ),
          ),
        ),
      ],
    );
  }
}
