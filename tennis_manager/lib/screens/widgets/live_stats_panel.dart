import 'package:flutter/material.dart';
import '../../providers/match_playback.dart';

class LiveStatsPanel extends StatelessWidget {
  final LiveStats stats;
  final String p1Name;
  final String p2Name;

  const LiveStatsPanel({
    super.key,
    required this.stats,
    required this.p1Name,
    required this.p2Name,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant.withValues(alpha: 0.4)),
      ),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  p1Name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: cs.primary,
                  ),
                ),
              ),
              Text(
                'ESTADÍSTICAS',
                style: TextStyle(
                  fontSize: 9,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 0.5,
                  color: cs.onSurfaceVariant,
                ),
              ),
              Expanded(
                child: Text(
                  p2Name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.right,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: cs.onSurface,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _statRow(
            context,
            'Puntos ganados',
            stats.p1.pointsWon,
            stats.p2.pointsWon,
          ),
          _statRow(context, 'Aces', stats.p1.aces, stats.p2.aces),
          _statRow(
            context,
            'Dobles faltas',
            stats.p1.doubleFaults,
            stats.p2.doubleFaults,
          ),
          _statRow(context, 'Winners', stats.p1.winners, stats.p2.winners),
          _statRow(
            context,
            'Errores forzados',
            stats.p1.forcedErrors,
            stats.p2.forcedErrors,
          ),
          _statRow(
            context,
            'Errores no forzados',
            stats.p1.unforcedErrors,
            stats.p2.unforcedErrors,
          ),
        ],
      ),
    );
  }

  Widget _statRow(BuildContext context, String label, int v1, int v2) {
    final cs = Theme.of(context).colorScheme;
    final total = v1 + v2;
    final ratio = total == 0 ? 0.5 : v1 / total;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Column(
        children: [
          Row(
            children: [
              SizedBox(
                width: 28,
                child: Text(
                  '$v1',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: cs.primary,
                  ),
                ),
              ),
              Expanded(
                child: Text(
                  label,
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
                ),
              ),
              SizedBox(
                width: 28,
                child: Text(
                  '$v2',
                  textAlign: TextAlign.right,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: cs.onSurface,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 3),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: Row(
              children: [
                Expanded(
                  flex: (ratio * 1000).round().clamp(1, 999),
                  child: Container(height: 4, color: cs.primary),
                ),
                Expanded(
                  flex: ((1 - ratio) * 1000).round().clamp(1, 999),
                  child: Container(
                    height: 4,
                    color: cs.onSurfaceVariant.withValues(alpha: 0.35),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
