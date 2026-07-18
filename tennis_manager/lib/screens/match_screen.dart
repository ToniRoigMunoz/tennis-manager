import 'package:flutter/material.dart';
import '../config.dart';
import '../models/match_models.dart';
import '../providers/match_playback.dart';
import '../services/api_service.dart';
import 'widgets/live_scoreboard.dart';
import 'widgets/point_narration.dart';
import 'widgets/live_stats_panel.dart';
import 'widgets/playback_controls.dart';
import 'widgets/error_view.dart';

class MatchScreen extends StatefulWidget {
  final String opponentName;
  final String tournamentName;
  final String round;

  const MatchScreen({
    super.key,
    required this.opponentName,
    required this.tournamentName,
    required this.round,
  });

  @override
  State<MatchScreen> createState() => _MatchScreenState();
}

class _MatchScreenState extends State<MatchScreen> {
  MatchPlaybackController? _controller;
  bool _loading = true;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final json = await ApiService.simulateMatch(
        userId: Config.demoUserId,
        opponentName: widget.opponentName,
      );
      final sim = MatchSimulation.fromJson(json);
      _controller?.dispose();
      _controller = MatchPlaybackController(sim)..play();
      setState(() => _loading = false);
    } catch (e) {
      setState(() {
        _error = e;
        _loading = false;
      });
    }
  }

  @override
  void dispose() {
    _controller?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          widget.tournamentName,
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(18),
          child: Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Text(widget.round, style: const TextStyle(fontSize: 12)),
          ),
        ),
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            CircularProgressIndicator(),
            SizedBox(height: 16),
            Text('Simulando el partido en la nube…'),
          ],
        ),
      );
    }

    if (_error != null) return ErrorView(onRetry: _load);

    final c = _controller!;

    return AnimatedBuilder(
      animation: c,
      builder: (context, _) {
        final finished = c.status == PlaybackStatus.finished;
        final winnerName = c.match.winner == 1
            ? c.match.player1Name
            : c.match.player2Name;

        return SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
          child: Column(
            children: [
              LiveScoreboard(match: c.match, currentIndex: c.currentIndex),
              const SizedBox(height: 14),
              if (finished)
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: Colors.amber.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: Colors.amber.withValues(alpha: 0.5),
                    ),
                  ),
                  child: Column(
                    children: [
                      Icon(
                        Icons.emoji_events_rounded,
                        color: Colors.amber.shade700,
                        size: 28,
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Victoria de $winnerName',
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 15,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        c.match.setsScore,
                        style: const TextStyle(fontSize: 13),
                      ),
                    ],
                  ),
                )
              else
                PointNarration(
                  point: c.currentPoint,
                  p1Name: c.match.player1Name,
                  p2Name: c.match.player2Name,
                ),
              const SizedBox(height: 14),
              PlaybackControls(controller: c),
              const SizedBox(height: 16),
              LiveStatsPanel(
                stats: c.liveStats,
                p1Name: c.match.player1Name,
                p2Name: c.match.player2Name,
              ),
            ],
          ),
        );
      },
    );
  }
}
