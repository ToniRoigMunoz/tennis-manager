import 'package:flutter/material.dart';
import '../../providers/match_playback.dart';

class PlaybackControls extends StatelessWidget {
  final MatchPlaybackController controller;
  const PlaybackControls({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final finished = controller.status == PlaybackStatus.finished;
    final playing = controller.status == PlaybackStatus.playing;

    return Column(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(6),
          child: LinearProgressIndicator(
            value: controller.progress,
            minHeight: 5,
            backgroundColor: cs.outlineVariant.withValues(alpha: 0.3),
            valueColor: AlwaysStoppedAnimation(cs.primary),
          ),
        ),
        const SizedBox(height: 6),
        Text(
          finished
              ? 'Partido finalizado'
              : 'Punto ${controller.currentIndex + 1} de ${controller.totalPoints}',
          style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
        ),
        const SizedBox(height: 10),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            IconButton.filled(
              onPressed: finished ? null : controller.togglePlayPause,
              icon: Icon(
                playing ? Icons.pause_rounded : Icons.play_arrow_rounded,
              ),
            ),
            const SizedBox(width: 12),
            ...[1.0, 2.0, 4.0].map((s) {
              final active = controller.speed == s;
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: 3),
                child: ChoiceChip(
                  label: Text(
                    'x${s.toInt()}',
                    style: const TextStyle(fontSize: 12),
                  ),
                  selected: active,
                  onSelected: finished ? null : (_) => controller.setSpeed(s),
                  visualDensity: VisualDensity.compact,
                ),
              );
            }),
            const SizedBox(width: 12),
            IconButton(
              onPressed: finished ? null : controller.skipToEnd,
              icon: const Icon(Icons.skip_next_rounded),
              tooltip: 'Saltar al final',
            ),
          ],
        ),
      ],
    );
  }
}
