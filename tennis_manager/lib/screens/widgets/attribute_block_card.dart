import 'package:flutter/material.dart';
import '../../models/player_models.dart';

class AttributeBlockCard extends StatelessWidget {
  final String title;
  final IconData icon;
  final Color accentColor;
  final List<PlayerAttribute> attributes;
  final String playingStyle;

  const AttributeBlockCard({
    super.key,
    required this.title,
    required this.icon,
    required this.accentColor,
    required this.attributes,
    required this.playingStyle,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.4),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 16, color: accentColor),
              const SizedBox(width: 8),
              Text(
                title,
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                  color: colorScheme.onSurface,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          ...attributes.map(
            (attr) => _AttributeRow(
              attribute: attr,
              isHighlighted: PlayerStyles.isHighlighted(
                playingStyle,
                attr.name,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AttributeRow extends StatelessWidget {
  final PlayerAttribute attribute;
  final bool isHighlighted;

  const _AttributeRow({required this.attribute, required this.isHighlighted});

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final barColor = isHighlighted
        ? colorScheme.primary
        : colorScheme.onSurfaceVariant.withValues(alpha: 0.45);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          SizedBox(
            width: 116,
            child: Row(
              children: [
                if (isHighlighted)
                  Padding(
                    padding: const EdgeInsets.only(right: 4),
                    child: Icon(
                      Icons.star_rounded,
                      size: 13,
                      color: Colors.amber.shade600,
                    ),
                  ),
                Expanded(
                  child: Text(
                    attribute.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: isHighlighted
                          ? FontWeight.bold
                          : FontWeight.normal,
                      color: colorScheme.onSurface,
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: attribute.value / 100,
                minHeight: 7,
                backgroundColor: colorScheme.outlineVariant.withValues(
                  alpha: 0.3,
                ),
                valueColor: AlwaysStoppedAnimation(barColor),
              ),
            ),
          ),
          SizedBox(
            width: 26,
            child: Text(
              '${attribute.value}',
              textAlign: TextAlign.right,
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: colorScheme.onSurface,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
