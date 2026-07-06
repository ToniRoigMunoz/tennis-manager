import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/tournament_provider.dart';
import 'widgets/season_progress_header.dart';
import 'widgets/tournament_timeline_item.dart';
import 'widgets/error_view.dart';

class TournamentsScreen extends ConsumerWidget {
  const TournamentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref
        .watch(tournamentProvider)
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) =>
              ErrorView(onRetry: () => ref.invalidate(tournamentProvider)),
          data: (t) => Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                child: SeasonProgressHeader(
                  currentDay: t.currentDay,
                  totalDays: t.totalDays,
                ),
              ),
              Expanded(
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  itemCount: t.tournaments.length,
                  itemBuilder: (context, index) => TournamentTimelineItem(
                    tournament: t.tournaments[index],
                    isFirst: index == 0,
                    isLast: index == t.tournaments.length - 1,
                  ),
                ),
              ),
            ],
          ),
        );
  }
}
