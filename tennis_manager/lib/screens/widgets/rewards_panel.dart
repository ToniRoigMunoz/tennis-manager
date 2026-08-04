import 'package:flutter/material.dart';
import '../../models/tournament_bracket_models.dart';

class RewardsPanel extends StatelessWidget {
  final TournamentRewards rewards;
  const RewardsPanel({super.key, required this.rewards});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Container(
      width: double.infinity,
      margin: const EdgeInsets.fromLTRB(16, 12, 16, 0),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant.withValues(alpha: 0.4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.card_giftcard_rounded, size: 16, color: cs.primary),
              const SizedBox(width: 8),
              Text(
                'Recompensas obtenidas',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                  color: cs.onSurface,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _RewardRow(
            icon: Icons.stars_rounded,
            color: cs.primary,
            label: 'Puntos de ranking',
            value: '+${rewards.pointsEarned}',
          ),
          _RewardRow(
            icon: Icons.monetization_on_rounded,
            color: Colors.amber.shade700,
            label: 'Dinero',
            value: '+${rewards.moneyEarned}',
          ),
          _RewardRow(
            icon: Icons.spa_rounded,
            color: Colors.green.shade600,
            label: 'Descansos',
            value: '+${rewards.restsEarned}',
          ),
          if (rewards.isChampion) _AttributeReward(rewards: rewards),
        ],
      ),
    );
  }
}

class _RewardRow extends StatelessWidget {
  final IconData icon;
  final Color color;
  final String label;
  final String value;

  const _RewardRow({
    required this.icon,
    required this.color,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Container(
            width: 30,
            height: 30,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, size: 16, color: color),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              label,
              style: TextStyle(fontSize: 13, color: cs.onSurface),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}

class _AttributeReward extends StatelessWidget {
  final TournamentRewards rewards;
  const _AttributeReward({required this.rewards});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final applied = rewards.attributePointsApplied;
    final progress = rewards.attributeProgress;

    return Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _RewardRow(
            icon: Icons.trending_up_rounded,
            color: Colors.deepPurple,
            label: applied > 0 ? 'Mejora de atributos' : 'Progreso de mejora',
            value: applied > 0
                ? '+$applied a todos'
                : '+${(progress * 100).toInt()}%',
          ),
          if (applied == 0)
            Padding(
              padding: const EdgeInsets.only(left: 42, top: 2),
              child: Text(
                'Sigue ganando torneos para subir de nivel',
                style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
              ),
            ),
        ],
      ),
    );
  }
}
