import 'dart:async';
import 'package:flutter/foundation.dart';
import '../models/match_models.dart';

enum PlaybackStatus { loading, playing, paused, finished, error }

class MatchPlaybackController extends ChangeNotifier {
  final MatchSimulation match;

  MatchPlaybackController(this.match) {
    _status = PlaybackStatus.paused;
    _startTimer();
  }

  Timer? _timer;
  int _currentIndex = -1; // -1 = nada mostrado aún
  double _speed = 1.0;
  PlaybackStatus _status = PlaybackStatus.paused;

  static const int _baseMs = 900; // ms por punto a velocidad x1

  // ── Getters de estado ──────────────────────────────────────────────────────
  PlaybackStatus get status => _status;
  double get speed => _speed;
  int get currentIndex => _currentIndex;
  int get totalPoints => match.points.length;
  bool get hasStarted => _currentIndex >= 0;

  PointEvent? get currentPoint =>
      _currentIndex >= 0 && _currentIndex < match.points.length
      ? match.points[_currentIndex]
      : null;

  double get progress =>
      totalPoints == 0 ? 0 : (_currentIndex + 1) / totalPoints;

  // ── Estadísticas acumuladas hasta el punto actual ──────────────────────────
  LiveStats get liveStats {
    int a1 = 0, df1 = 0, w1 = 0, fe1 = 0, ue1 = 0, pts1 = 0;
    int a2 = 0, df2 = 0, w2 = 0, fe2 = 0, ue2 = 0, pts2 = 0;

    for (int i = 0; i <= _currentIndex && i < match.points.length; i++) {
      final p = match.points[i];
      final winner = p.winner;
      final loser = winner == 1 ? 2 : 1;
      if (winner == 1) {
        pts1++;
      } else {
        pts2++;
      }

      switch (p.outcome) {
        case 'ace':
          if (p.server == 1) {
            a1++;
          } else {
            a2++;
          }
          break;
        case 'doubleFault':
          if (loser == 1) {
            df1++;
          } else {
            df2++;
          }
          break;
        case 'winner':
          if (winner == 1) {
            w1++;
          } else {
            w2++;
          }
          break;
        case 'forcedError':
          if (loser == 1) {
            fe1++;
          } else {
            fe2++;
          }
          break;
        case 'unforcedError':
          if (loser == 1) {
            ue1++;
          } else {
            ue2++;
          }
          break;
      }
    }

    return LiveStats(
      p1: PlayerLiveStats(
        aces: a1,
        doubleFaults: df1,
        winners: w1,
        forcedErrors: fe1,
        unforcedErrors: ue1,
        pointsWon: pts1,
      ),
      p2: PlayerLiveStats(
        aces: a2,
        doubleFaults: df2,
        winners: w2,
        forcedErrors: fe2,
        unforcedErrors: ue2,
        pointsWon: pts2,
      ),
    );
  }

  // ── Controles ──────────────────────────────────────────────────────────────
  void play() {
    if (_status == PlaybackStatus.finished) return;
    _status = PlaybackStatus.playing;
    _startTimer();
    notifyListeners();
  }

  void pause() {
    _status = PlaybackStatus.paused;
    _timer?.cancel();
    notifyListeners();
  }

  void togglePlayPause() {
    if (_status == PlaybackStatus.playing) {
      pause();
    } else {
      play();
    }
  }

  void setSpeed(double s) {
    _speed = s;
    if (_status == PlaybackStatus.playing) {
      _startTimer(); // reinicia con nuevo intervalo
    }
    notifyListeners();
  }

  void skipToEnd() {
    _timer?.cancel();
    _currentIndex = match.points.length - 1;
    _status = PlaybackStatus.finished;
    notifyListeners();
  }

  void _startTimer() {
    _timer?.cancel();
    if (_status != PlaybackStatus.playing) return;
    final interval = Duration(milliseconds: (_baseMs / _speed).round());
    _timer = Timer.periodic(interval, (_) => _advance());
  }

  void _advance() {
    if (_currentIndex >= match.points.length - 1) {
      _currentIndex = match.points.length - 1;
      _status = PlaybackStatus.finished;
      _timer?.cancel();
      notifyListeners();
      return;
    }
    _currentIndex++;
    notifyListeners();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}

// ── Estructuras de estadísticas en vivo ──────────────────────────────────────
class LiveStats {
  final PlayerLiveStats p1;
  final PlayerLiveStats p2;
  const LiveStats({required this.p1, required this.p2});
}

class PlayerLiveStats {
  final int aces;
  final int doubleFaults;
  final int winners;
  final int forcedErrors;
  final int unforcedErrors;
  final int pointsWon;

  const PlayerLiveStats({
    required this.aces,
    required this.doubleFaults,
    required this.winners,
    required this.forcedErrors,
    required this.unforcedErrors,
    required this.pointsWon,
  });
}
