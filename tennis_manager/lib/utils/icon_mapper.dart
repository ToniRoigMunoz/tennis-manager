import 'package:flutter/material.dart';

class IconMapper {
  static IconData fromName(String name) {
    switch (name) {
      case 'ac_unit_rounded':
        return Icons.ac_unit_rounded;
      case 'bolt_rounded':
        return Icons.bolt_rounded;
      case 'star_rounded':
        return Icons.star_rounded;
      case 'flash_on_rounded':
        return Icons.flash_on_rounded;
      case 'shield_rounded':
        return Icons.shield_rounded;
      case 'speed_rounded':
        return Icons.speed_rounded;
      default:
        return Icons.auto_awesome_rounded;
    }
  }
}
