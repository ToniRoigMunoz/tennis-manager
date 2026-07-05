import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/tournament_provider.dart';
import 'widgets/season_progress_header.dart';
import 'widgets/tournament_timeline_item.dart';

class TournamentsScreen extends ConsumerWidget {
  const TournamentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tournaments = ref.watch(tournamentProvider);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: SeasonProgressHeader(
            currentDay: tournaments.currentDay,
            totalDays: tournaments.totalDays,
          ),
        ),
        Expanded(
          child: ListView.builder(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            itemCount: tournaments.tournaments.length,
            itemBuilder: (context, index) => TournamentTimelineItem(
              tournament: tournaments.tournaments[index],
              isFirst: index == 0,
              isLast: index == tournaments.tournaments.length - 1,
            ),
          ),
        ),
      ],
    );
  }
}
