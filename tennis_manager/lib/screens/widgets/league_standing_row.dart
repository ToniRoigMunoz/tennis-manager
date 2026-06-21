import 'package:flutter/material.dart';
import '../../models/league_models.dart';

class LeagueStandingRow extends StatelessWidget {
  final LeagueStanding standing;
  final bool qualifiesForFinals;

  const LeagueStandingRow({
    super.key,
    required this.standing,
    required this.qualifiesForFinals,
  });

  String _initialsOf(String name) {
    final parts = name.trim().split(' ');
    if (parts.length >= 2) return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    return name.substring(0, name.length >= 2 ? 2 : 1).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final goldColor = Colors.amber.shade600;

    return Container(
      decoration: BoxDecoration(
        color: standing.isCurrentUser
            ? colorScheme.primary.withValues(alpha: 0.08)
            : null,
        border: Border(
          left: BorderSide(
            color: qualifiesForFinals ? goldColor : Colors.transparent,
            width: 4,
          ),
          bottom: BorderSide(
            color: colorScheme.outlineVariant.withValues(alpha: 0.3),
          ),
        ),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          SizedBox(
            width: 26,
            child: Text(
              '${standing.position}',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 13,
                color: standing.isCurrentUser
                    ? colorScheme.primary
                    : colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          CircleAvatar(
            radius: 15,
            backgroundColor: standing.isCurrentUser
                ? colorScheme.primary
                : colorScheme.primary.withValues(alpha: 0.15),
            child: Text(
              _initialsOf(standing.name),
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.bold,
                color: standing.isCurrentUser
                    ? Colors.white
                    : colorScheme.primary,
              ),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              standing.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontWeight: standing.isCurrentUser
                    ? FontWeight.bold
                    : FontWeight.normal,
                fontSize: 14,
                color: standing.isCurrentUser
                    ? colorScheme.primary
                    : colorScheme.onSurface,
              ),
            ),
          ),
          if (qualifiesForFinals) ...[
            const SizedBox(width: 4),
            Icon(Icons.emoji_events_rounded, size: 14, color: goldColor),
          ],
          const SizedBox(width: 8),
          Row(
            children: standing.recentForm
                .map(
                  (won) => Container(
                    width: 7,
                    height: 7,
                    margin: const EdgeInsets.symmetric(horizontal: 1.5),
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: won ? Colors.green : Colors.red,
                    ),
                  ),
                )
                .toList(),
          ),
          const SizedBox(width: 14),
          SizedBox(
            width: 50,
            child: Text(
              '${standing.points}',
              textAlign: TextAlign.right,
              style: TextStyle(
                fontWeight: FontWeight.w700,
                fontSize: 13,
                color: standing.isCurrentUser
                    ? colorScheme.primary
                    : colorScheme.onSurface,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
