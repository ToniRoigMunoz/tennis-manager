import 'package:flutter/material.dart';
import '../models/tournament_bracket_models.dart';

class SeasonEndScreen extends StatelessWidget {
  final SeasonEndResult result;
  final VoidCallback onContinue;

  const SeasonEndScreen({
    super.key,
    required this.result,
    required this.onContinue,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(20, 32, 20, 20),
                child: Column(
                  children: [
                    // Cabecera
                    Icon(
                      Icons.emoji_events_rounded,
                      size: 56,
                      color: Colors.amber.shade600,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      'Fin de temporada',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                        color: cs.onSurface,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Se cierra el circuito. Estos son los campeones.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 13,
                        color: cs.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Tu movimiento (lo más importante para el jugador, va primero y destacado)
                    _MovementCard(result: result),
                    const SizedBox(height: 28),

                    // Campeones de las tres divisiones
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Text(
                        'CAMPEONES POR DIVISIÓN',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 0.6,
                          color: cs.onSurfaceVariant,
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    _ChampionCard(
                      division: 'Primera División',
                      name: result.championPrimera,
                      medalColor: Colors.amber.shade600,
                      rank: 1,
                    ),
                    const SizedBox(height: 10),
                    _ChampionCard(
                      division: 'Segunda División',
                      name: result.championSegunda,
                      medalColor: Colors.blueGrey.shade400,
                      rank: 2,
                    ),
                    const SizedBox(height: 10),
                    _ChampionCard(
                      division: 'Tercera División',
                      name: result.championTercera,
                      medalColor: Colors.brown.shade400,
                      rank: 3,
                    ),
                  ],
                ),
              ),
            ),

            // Botón de continuar
            Padding(
              padding: const EdgeInsets.all(20),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: onContinue,
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 15),
                  ),
                  child: const Text('Comenzar nueva temporada'),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MovementCard extends StatelessWidget {
  final SeasonEndResult result;
  const _MovementCard({required this.result});

  @override
  Widget build(BuildContext context) {
    final (color, icon, label) = switch (result) {
      _ when result.isPromotion => (
        Colors.green,
        Icons.arrow_upward_rounded,
        '¡Ascenso!',
      ),
      _ when result.isRelegation => (
        Colors.redAccent,
        Icons.arrow_downward_rounded,
        'Descenso',
      ),
      _ => (Colors.blueGrey, Icons.remove_rounded, 'Te mantienes'),
    };

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            color.withValues(alpha: 0.18),
            color.withValues(alpha: 0.06),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: color.withValues(alpha: 0.45)),
      ),
      child: Column(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.2),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: color, size: 28),
          ),
          const SizedBox(height: 10),
          Text(
            label,
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Tu jugador ${result.humanMovement}',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 13,
              color: Theme.of(context).colorScheme.onSurface,
            ),
          ),
        ],
      ),
    );
  }
}

class _ChampionCard extends StatelessWidget {
  final String division;
  final String name;
  final Color medalColor;
  final int rank;

  const _ChampionCard({
    required this.division,
    required this.name,
    required this.medalColor,
    required this.rank,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: cs.outlineVariant.withValues(alpha: 0.4)),
      ),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: medalColor.withValues(alpha: 0.18),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Icons.emoji_events_rounded,
              color: medalColor,
              size: 22,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  division,
                  style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
                ),
                const SizedBox(height: 2),
                Text(
                  name,
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: cs.onSurface,
                  ),
                ),
              ],
            ),
          ),
          Icon(Icons.workspace_premium_rounded, color: medalColor, size: 20),
        ],
      ),
    );
  }
}
