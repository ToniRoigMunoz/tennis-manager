import 'package:flutter/material.dart';
import '../../models/player_models.dart';

class AttributesPanel extends StatelessWidget {
  final List<PlayerAttribute> physical;
  final List<PlayerAttribute> mental;
  final List<PlayerAttribute> technical;
  final String playingStyle;

  const AttributesPanel({
    super.key,
    required this.physical,
    required this.mental,
    required this.technical,
    required this.playingStyle,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.4),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: _AttributeColumn(
              title: 'Físico',
              icon: Icons.directions_run_rounded,
              color: Colors.blue,
              attributes: physical,
              playingStyle: playingStyle,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _AttributeColumn(
              title: 'Mental',
              icon: Icons.psychology_outlined,
              color: Colors.deepPurple,
              attributes: mental,
              playingStyle: playingStyle,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _AttributeColumn(
              title: 'Técnico',
              icon: Icons.sports_tennis_rounded,
              color: Colors.teal,
              attributes: technical,
              playingStyle: playingStyle,
            ),
          ),
        ],
      ),
    );
  }
}

class _AttributeColumn extends StatelessWidget {
  final String title;
  final IconData icon;
  final Color color;
  final List<PlayerAttribute> attributes;
  final String playingStyle;

  const _AttributeColumn({
    required this.title,
    required this.icon,
    required this.color,
    required this.attributes,
    required this.playingStyle,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Icon(icon, size: 13, color: color),
            const SizedBox(width: 4),
            Text(
              title,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 11,
                color: colorScheme.onSurface,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        ...attributes.map(
          (attr) => _CompactAttributeItem(
            attribute: attr,
            isHighlighted: PlayerStyles.isHighlighted(playingStyle, attr.name),
          ),
        ),
      ],
    );
  }
}

class _CompactAttributeItem extends StatelessWidget {
  final PlayerAttribute attribute;
  final bool isHighlighted;

  const _CompactAttributeItem({
    required this.attribute,
    required this.isHighlighted,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final barColor = isHighlighted
        ? colorScheme.primary
        : colorScheme.onSurfaceVariant.withValues(alpha: 0.45);

    return Padding(
      padding: const EdgeInsets.only(bottom: 7),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (isHighlighted)
                Padding(
                  padding: const EdgeInsets.only(right: 3),
                  child: Icon(
                    Icons.star_rounded,
                    size: 10,
                    color: Colors.amber.shade600,
                  ),
                ),
              Expanded(
                child: Text(
                  attribute.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 9.5,
                    fontWeight: isHighlighted
                        ? FontWeight.bold
                        : FontWeight.normal,
                    color: colorScheme.onSurface,
                  ),
                ),
              ),
              Text(
                '${attribute.value}',
                style: TextStyle(
                  fontSize: 9.5,
                  fontWeight: FontWeight.bold,
                  color: colorScheme.onSurface,
                ),
              ),
            ],
          ),
          const SizedBox(height: 2),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: attribute.value / 100,
              minHeight: 4,
              backgroundColor: colorScheme.outlineVariant.withValues(
                alpha: 0.3,
              ),
              valueColor: AlwaysStoppedAnimation(barColor),
            ),
          ),
        ],
      ),
    );
  }
}
