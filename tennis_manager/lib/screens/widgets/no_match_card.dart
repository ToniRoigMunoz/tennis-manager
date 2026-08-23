import 'package:flutter/material.dart';

class NoMatchCard extends StatelessWidget {
  final String? nextTournamentName;
  const NoMatchCard({super.key, this.nextTournamentName});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: cs.primary.withValues(alpha: 0.06),
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
              Icons.event_available_rounded,
              size: 40,
              color: cs.primary.withValues(alpha: 0.4),
            ),
            const SizedBox(height: 12),
            Text(
              'Ya has competido hoy',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 16,
                color: cs.onSurface,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              nextTournamentName != null
                  ? 'Mañana: ${nextTournamentName!}'
                  : 'Tu próximo torneo llegará pronto',
              style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
            ),
          ],
        ),
      ),
    );
  }
}
