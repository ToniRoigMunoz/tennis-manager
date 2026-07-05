import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/dashboard_provider.dart';
import '../providers/league_provider.dart';
import '../providers/tournament_provider.dart';
import 'widgets/next_match_card.dart';
import 'widgets/ranking_summary_card.dart';
import 'widgets/last_match_card.dart';
import 'widgets/upcoming_tournaments_strip.dart';

class GeneralScreen extends ConsumerWidget {
  final VoidCallback onRankingTap;
  final VoidCallback onTournamentsTap;

  const GeneralScreen({
    super.key,
    required this.onRankingTap,
    required this.onTournamentsTap,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(dashboardProvider);
    final league = ref.watch(leagueProvider);
    final tournaments = ref.watch(tournamentProvider);

    if (dashboard.nextMatch == null || dashboard.lastMatch == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final topHeight = constraints.maxHeight * 0.33;

        return Column(
          children: [
            SizedBox(
              height: topHeight,
              width: double.infinity,
              child: NextMatchCard(match: dashboard.nextMatch!),
            ),
            const SizedBox(height: 16),
            UpcomingTournamentsStrip(
              tournaments: tournaments.upcoming.take(3).toList(),
              onSeeAllTap: onTournamentsTap,
            ),
            const SizedBox(height: 8),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(
                      child: RankingSummaryCard(
                        entries: league.dashboardSummary(),
                        onTap: onRankingTap,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: LastMatchCard(match: dashboard.lastMatch!)),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
