import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/player_provider.dart';
import 'widgets/player_switcher.dart';
import 'widgets/player_info_card.dart';
import 'widgets/skills_card.dart';
import 'widgets/attributes_panel.dart';
import 'widgets/error_view.dart';

class PlayerScreen extends ConsumerWidget {
  const PlayerScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref
        .watch(playerProvider)
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) =>
              ErrorView(onRetry: () => ref.invalidate(playerProvider)),
          data: (player) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            children: [
              PlayerSwitcher(
                currentPlayerName: player.profile.name,
                totalPlayers: 1,
                onTap: () {},
              ),
              const SizedBox(height: 8),
              PlayerInfoCard(
                player: player.profile,
                overallRating: player.overallRating,
              ),
              const SizedBox(height: 14),
              SkillsCard(skills: player.skills),
              const SizedBox(height: 14),
              AttributesPanel(
                physical: player.physical,
                mental: player.mental,
                technical: player.technical,
                playingStyle: player.profile.playingStyle,
              ),
            ],
          ),
        );
  }
}
