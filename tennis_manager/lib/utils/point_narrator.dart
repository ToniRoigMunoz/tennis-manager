import '../models/match_models.dart';

class PointNarrator {
  static String narrate(PointEvent p, String p1Name, String p2Name) {
    final serverName = p.server == 1 ? p1Name : p2Name;
    final winnerName = p.winner == 1 ? p1Name : p2Name;
    final loserName = p.winner == 1 ? p2Name : p1Name;

    switch (p.outcome) {
      case 'ace':
        return '¡Ace de $serverName!';
      case 'doubleFault':
        final faultName = p.winner == 1 ? p2Name : p1Name;
        return 'Doble falta de $faultName';
      case 'winner':
        return '¡Golpe ganador de $winnerName!';
      case 'forcedError':
        return '$winnerName fuerza el error de $loserName';
      case 'unforcedError':
        return 'Error no forzado de $loserName';
      case 'neutral':
      default:
        return 'Punto para $winnerName';
    }
  }

  static String outcomeLabel(String outcome) {
    switch (outcome) {
      case 'ace':
        return 'ACE';
      case 'doubleFault':
        return 'DOBLE FALTA';
      case 'winner':
        return 'WINNER';
      case 'forcedError':
        return 'ERROR FORZADO';
      case 'unforcedError':
        return 'ERROR NO FORZADO';
      default:
        return 'PUNTO';
    }
  }
}
