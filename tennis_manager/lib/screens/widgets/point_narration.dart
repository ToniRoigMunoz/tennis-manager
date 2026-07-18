import 'package:flutter/material.dart';
import '../../models/match_models.dart';
import '../../utils/point_narrator.dart';

class PointNarration extends StatelessWidget {
  final PointEvent? point;
  final String p1Name;
  final String p2Name;

  const PointNarration({
    super.key,
    required this.point,
    required this.p1Name,
    required this.p2Name,
  });

  Color _colorFor(String outcome) {
    switch (outcome) {
      case 'ace':
        return Colors.green;
      case 'winner':
        return Colors.teal;
      case 'doubleFault':
      case 'unforcedError':
        return Colors.redAccent;
      case 'forcedError':
        return Colors.orange;
      default:
        return Colors.blueGrey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    if (point == null) {
      return Container(
        height: 74,
        alignment: Alignment.center,
        child: Text(
          'El partido está a punto de comenzar…',
          style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13),
        ),
      );
    }

    final p = point!;
    final color = _colorFor(p.outcome);

    return AnimatedSwitcher(
      duration: const Duration(milliseconds: 250),
      child: Container(
        key: ValueKey(
          '${p.outcome}-${p.p1GameScore}-${p.p2GameScore}-${p.p1Games}',
        ),
        height: 74,
        width: double.infinity,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.10),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: color.withValues(alpha: 0.35)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              PointNarrator.outcomeLabel(p.outcome),
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.bold,
                letterSpacing: 0.6,
                color: color,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              PointNarrator.narrate(p, p1Name, p2Name),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: cs.onSurface,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
