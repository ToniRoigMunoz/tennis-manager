import 'package:flutter/material.dart';

class PlayerSwitcher extends StatelessWidget {
  final String currentPlayerName;
  final int totalPlayers;
  final VoidCallback onTap;

  const PlayerSwitcher({
    super.key,
    required this.currentPlayerName,
    required this.totalPlayers,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 8),
          child: Row(
            children: [
              Text(
                currentPlayerName,
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(width: 4),
              Icon(
                Icons.unfold_more_rounded,
                size: 16,
                color: colorScheme.onSurfaceVariant,
              ),
              const Spacer(),
              if (totalPlayers > 1)
                Text(
                  '$totalPlayers jugadores en tu academia',
                  style: TextStyle(
                    fontSize: 11,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
