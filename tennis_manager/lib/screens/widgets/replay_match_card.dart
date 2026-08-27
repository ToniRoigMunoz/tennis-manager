import 'package:flutter/material.dart';
import '../../models/tournament_bracket_models.dart';

class ReplayMatchCard extends StatelessWidget {
  final TournamentStep step;
  final VoidCallback onSeen;
  const ReplayMatchCard({super.key, required this.step, required this.onSeen});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final won = step.humanWon ?? false;
    final color = won ? cs.primary : cs.error;

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            color.withValues(alpha: 0.12),
            color.withValues(alpha: 0.04),
          ],
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
        ),
        borderRadius: const BorderRadius.only(
          bottomLeft: Radius.circular(28),
          bottomRight: Radius.circular(28),
        ),
      ),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              won ? Icons.emoji_events_rounded : Icons.sports_tennis_rounded,
              size: 40,
              color: color.withValues(alpha: 0.8),
            ),
            const SizedBox(height: 12),
            Text(
              won ? '¡Has ganado tu partido!' : 'Has perdido tu partido',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: cs.onSurface,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '${step.roundName ?? ''} · vs ${step.opponent?.name ?? ''}',
              style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant),
            ),
            if (step.replayScore != null && step.replayScore!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  step.replayScore!,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: color,
                  ),
                ),
              ),
            ],
            const SizedBox(height: 14),
            TextButton(onPressed: onSeen, child: const Text('Continuar')),
          ],
        ),
      ),
    );
  }
}
