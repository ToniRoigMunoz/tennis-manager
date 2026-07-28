import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../config.dart';
import '../models/match_models.dart';
import '../providers/match_playback.dart';
import '../providers/player_provider.dart';
import '../services/api_service.dart';
import 'widgets/live_scoreboard.dart';
import 'widgets/point_narration.dart';
import 'widgets/live_stats_panel.dart';
import 'widgets/playback_controls.dart';
import 'widgets/error_view.dart';
import '../providers/tournament_flow_provider.dart';
import 'tournament_bracket_screen.dart';

class MatchScreen extends ConsumerStatefulWidget {
  final String opponentName;
  final String tournamentName;
  final String round;

  // Modo torneo (opcional)
  final bool isTournament;
  final int opponentOverall;
  final int tournamentSeed;

  const MatchScreen({
    super.key,
    required this.opponentName,
    required this.tournamentName,
    required this.round,
    this.isTournament = false,
    this.opponentOverall = 72,
    this.tournamentSeed = 0,
  });

  @override
  ConsumerState<MatchScreen> createState() => _MatchScreenState();
}

class _MatchScreenState extends ConsumerState<MatchScreen> {
  MatchPlaybackController? _controller;
  bool _loading = true;
  bool _saving = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  // Seed determinista derivado del torneo + rival, para que el resultado
  // sea reproducible y "propiedad" del servidor.
  int _matchSeed() {
    int sum = 0;
    for (final c in widget.opponentName.codeUnits) {
      sum = (sum + c) % 100000;
    }
    return widget.tournamentSeed + sum;
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final json = widget.isTournament
          ? await ApiService.simulateTournamentMatch(
              userId: Config.demoUserId,
              opponentName: widget.opponentName,
              opponentOverall: widget.opponentOverall,
              seed: _matchSeed(),
            )
          : await ApiService.simulateMatch(
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

  Future<void> _continue() async {
    final c = _controller!;
    setState(() => _saving = true);

    if (widget.isTournament) {
      try {
        final step = await ref
            .read(tournamentFlowProvider.notifier)
            .reportResultAndAdvance(
              humanWon: c.match.winner == 1,
              setsScore: c.match.setsScore,
            );
        if (mounted) {
          Navigator.of(context).pushReplacement(
            MaterialPageRoute(
              builder: (_) => TournamentBracketScreen(
                step: step,
                onContinue: () {
                  // Volver a General y refrescar
                  Navigator.of(context).popUntil((r) => r.isFirst);
                  ref.invalidate(tournamentFlowProvider);
                  ref.invalidate(playerProvider);
                },
              ),
            ),
          );
        }
      } catch (e) {
        if (mounted) {
          setState(() => _saving = false);
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('No se pudo avanzar el torneo')),
          );
        }
      }
      return;
    }

    // Modo amistoso (comportamiento original)
    try {
      await ApiService.saveMatchResult(
        userId: Config.demoUserId,
        opponentName: c.match.player2Name,
        won: c.match.winner == 1,
        setsScore: c.match.setsScore,
        aces: c.match.stats1.aces,
        winners: c.match.stats1.winners,
        unforcedErrors: c.match.stats1.unforcedErrors,
      );
      ref.invalidate(playerProvider);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No se pudo guardar el resultado')),
        );
      }
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
              if (!finished) PlaybackControls(controller: c),
              const SizedBox(height: 16),
              LiveStatsPanel(
                stats: c.liveStats,
                p1Name: c.match.player1Name,
                p2Name: c.match.player2Name,
              ),
              if (finished) ...[
                const SizedBox(height: 20),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: _saving ? null : _continue,
                    icon: _saving
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.check_rounded),
                    label: Text(_saving ? 'Guardando…' : 'Continuar'),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                    ),
                  ),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
