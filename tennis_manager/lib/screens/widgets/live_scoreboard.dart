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
            sets: completed,
            isPlayer1: true,
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
            sets: completed,
            isPlayer1: false,
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
    required List<List<int>> sets,
    required bool isPlayer1,
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
        // Sets cerrados: en blanco y negrita el del ganador de cada set
        ...sets.map((s) {
          final mine = isPlayer1 ? s[0] : s[1];
          final theirs = isPlayer1 ? s[1] : s[0];
          final wonSet = mine > theirs;
          return Padding(
            padding: const EdgeInsets.symmetric(horizontal: 5),
            child: Text(
              '$mine',
              style: TextStyle(
                color: wonSet
                    ? Colors.white
                    : Colors.white.withValues(alpha: 0.45),
                fontWeight: wonSet ? FontWeight.bold : FontWeight.w500,
                fontSize: wonSet ? 17 : 16,
              ),
            ),
          );
        }),
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
