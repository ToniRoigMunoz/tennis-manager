import 'package:flutter/material.dart';
import '../../models/tournament_models.dart';

class TournamentTimelineItem extends StatelessWidget {
  final TournamentInfo tournament;
  final bool isFirst;
  final bool isLast;

  const TournamentTimelineItem({
    super.key,
    required this.tournament,
    required this.isFirst,
    required this.isLast,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isCurrent = tournament.status == TournamentStatus.current;

    final dotColor = isCurrent
        ? colorScheme.primary
        : tournament.status == TournamentStatus.past
        ? colorScheme.onSurfaceVariant.withValues(alpha: 0.4)
        : colorScheme.outlineVariant;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 36,
            child: Column(
              children: [
                Expanded(
                  child: isFirst
                      ? const SizedBox()
                      : Container(
                          width: 2,
                          color: colorScheme.outlineVariant.withValues(
                            alpha: 0.4,
                          ),
                        ),
                ),
                Container(
                  width: isCurrent ? 16 : 12,
                  height: isCurrent ? 16 : 12,
                  margin: const EdgeInsets.symmetric(vertical: 4),
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: dotColor,
                    border: isCurrent
                        ? Border.all(
                            color: colorScheme.primary.withValues(alpha: 0.3),
                            width: 4,
                          )
                        : null,
                  ),
                ),
                Expanded(
                  child: isLast
                      ? const SizedBox()
                      : Container(
                          width: 2,
                          color: colorScheme.outlineVariant.withValues(
                            alpha: 0.4,
                          ),
                        ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 14, top: 2),
              child: _TournamentCard(tournament: tournament),
            ),
          ),
        ],
      ),
    );
  }
}

class _TournamentCard extends StatelessWidget {
  final TournamentInfo tournament;
  const _TournamentCard({required this.tournament});

  Color _categoryColor(BuildContext context) {
    switch (tournament.category) {
      case TournamentCategory.grandSlam:
        return Colors.deepPurple;
      case TournamentCategory.finals:
        return Colors.amber.shade700;
      case TournamentCategory.regular:
        return Theme.of(context).colorScheme.primary;
    }
  }

  String _categoryLabel() {
    switch (tournament.category) {
      case TournamentCategory.grandSlam:
        return 'GRAND SLAM';
      case TournamentCategory.finals:
        return 'FINALES';
      case TournamentCategory.regular:
        return 'ATP';
    }
  }

  IconData _categoryIcon() {
    switch (tournament.category) {
      case TournamentCategory.grandSlam:
        return Icons.star_rounded;
      case TournamentCategory.finals:
        return Icons.emoji_events_rounded;
      case TournamentCategory.regular:
        return Icons.sports_tennis_rounded;
    }
  }

  String _dayLabel() {
    if (tournament.durationDays > 1) {
      return 'Días ${tournament.startDay}-${tournament.startDay + tournament.durationDays - 1}';
    }
    return 'Día ${tournament.startDay}';
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isPast = tournament.status == TournamentStatus.past;
    final isCurrent = tournament.status == TournamentStatus.current;
    final categoryColor = _categoryColor(context);

    return Opacity(
      opacity: isPast ? 0.6 : 1.0,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: isCurrent
              ? colorScheme.primary.withValues(alpha: 0.06)
              : colorScheme.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isCurrent
                ? colorScheme.primary.withValues(alpha: 0.4)
                : colorScheme.outlineVariant.withValues(alpha: 0.4),
            width: isCurrent ? 1.5 : 1,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(_categoryIcon(), size: 14, color: categoryColor),
                    const SizedBox(width: 6),
                    Text(
                      _categoryLabel(),
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        color: categoryColor,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
                if (isCurrent)
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 3,
                    ),
                    decoration: BoxDecoration(
                      color: colorScheme.primary,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Text(
                      'EN CURSO',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 9,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              tournament.name,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 15,
                color: colorScheme.onSurface,
              ),
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Text(
                  _dayLabel(),
                  style: TextStyle(
                    fontSize: 12,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  '·',
                  style: TextStyle(color: colorScheme.onSurfaceVariant),
                ),
                const SizedBox(width: 8),
                Text(
                  tournament.dateLabel,
                  style: TextStyle(
                    fontSize: 12,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  '·',
                  style: TextStyle(color: colorScheme.onSurfaceVariant),
                ),
                const SizedBox(width: 8),
                Text(
                  tournament.surface,
                  style: TextStyle(
                    fontSize: 12,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
            if (isPast && tournament.resultLabel != null) ...[
              const SizedBox(height: 8),
              Row(
                children: [
                  Icon(
                    Icons.check_circle_outline_rounded,
                    size: 14,
                    color: colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: 6),
                  Text(
                    tournament.resultLabel!,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}
