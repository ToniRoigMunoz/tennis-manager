import 'package:flutter/material.dart';
import '../../providers/match_playback.dart';

class PlaybackControls extends StatelessWidget {
  final MatchPlaybackController controller;
  const PlaybackControls({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    final finished = controller.status == PlaybackStatus.finished;
    final playing = controller.status == PlaybackStatus.playing;

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        IconButton.filled(
          onPressed: finished ? null : controller.togglePlayPause,
          icon: Icon(playing ? Icons.pause_rounded : Icons.play_arrow_rounded),
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
    );
  }
}
