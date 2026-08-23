import 'dart:async';
import 'package:flutter/material.dart';
import '../../models/tournament_bracket_models.dart';

class WaitingRoundCard extends StatefulWidget {
  final TournamentStep step;
  const WaitingRoundCard({super.key, required this.step});

  @override
  State<WaitingRoundCard> createState() => _WaitingRoundCardState();
}

class _WaitingRoundCardState extends State<WaitingRoundCard> {
  Timer? _ticker;

  @override
  void initState() {
    super.initState();
    // Refresca cada segundo para la cuenta atrás
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  String _formatCountdown(Duration d) {
    if (d.isNegative) return '00:00:00';
    final h = d.inHours.toString().padLeft(2, '0');
    final m = (d.inMinutes % 60).toString().padLeft(2, '0');
    final s = (d.inSeconds % 60).toString().padLeft(2, '0');
    return '$h:$m:$s';
  }

  String _formatLocalTime(DateTime dt) {
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final unlock = widget.step.unlockLocalTime;
    final remaining = unlock != null
        ? unlock.difference(DateTime.now())
        : Duration.zero;

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            cs.primary.withValues(alpha: 0.12),
            cs.primary.withValues(alpha: 0.04),
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
              Icons.schedule_rounded,
              size: 40,
              color: cs.primary.withValues(alpha: 0.6),
            ),
            const SizedBox(height: 12),
            Text(
              'Has ganado tu partido',
              style: TextStyle(fontSize: 13, color: cs.onSurfaceVariant),
            ),
            const SizedBox(height: 8),
            if (unlock != null) ...[
              Text(
                'Tu próximo partido se desbloquea a las ${_formatLocalTime(unlock)}',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: cs.onSurface,
                ),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: cs.primary.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  _formatCountdown(remaining),
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    fontFeatures: const [FontFeature.tabularFigures()],
                    color: cs.primary,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
