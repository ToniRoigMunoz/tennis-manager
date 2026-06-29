import 'package:flutter/material.dart';
import 'resource_chip.dart';

class ResourceBar extends StatelessWidget {
  final int money;
  final int rests;

  const ResourceBar({super.key, required this.money, required this.rests});

  String _formatMoney(int amount) {
    final str = amount.toString();
    final buffer = StringBuffer();
    for (int i = 0; i < str.length; i++) {
      if (i > 0 && (str.length - i) % 3 == 0) buffer.write('.');
      buffer.write(str[i]);
    }
    return buffer.toString();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          ResourceChip(
            icon: Icons.monetization_on_rounded,
            color: Colors.amber.shade700,
            value: _formatMoney(money),
          ),
          ResourceChip(
            icon: Icons.spa_rounded,
            color: Colors.green.shade600,
            value: '$rests',
          ),
        ],
      ),
    );
  }
}
