import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/dashboard_provider.dart';
import '../providers/league_provider.dart';
import '../providers/tournament_provider.dart';
import 'widgets/next_match_card.dart';
import 'widgets/ranking_summary_card.dart';
import 'widgets/last_match_card.dart';
import 'widgets/upcoming_tournaments_strip.dart';
import 'widgets/error_view.dart';

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
    final dashAsync = ref.watch(dashboardProvider);
    final leagueAsync = ref.watch(leagueProvider);
    final tourAsync = ref.watch(tournamentProvider);

    final isLoading = [
      dashAsync,
      leagueAsync,
      tourAsync,
    ].any((a) => a.isLoading);
    final hasError = [dashAsync, leagueAsync, tourAsync].any((a) => a.hasError);

    if (isLoading) return const Center(child: CircularProgressIndicator());
    if (hasError) {
      return ErrorView(
        onRetry: () {
          ref.invalidate(dashboardProvider);
          ref.invalidate(leagueProvider);
          ref.invalidate(tournamentProvider);
        },
      );
    }

    final dash = dashAsync.value!;
    final league = leagueAsync.value!;
    final tournaments = tourAsync.value!;

    return LayoutBuilder(
      builder: (context, constraints) {
        final topHeight = constraints.maxHeight * 0.33;
        return Column(
          children: [
            if (dash.nextMatch != null)
              SizedBox(
                height: topHeight,
                width: double.infinity,
                child: NextMatchCard(match: dash.nextMatch!),
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
                    if (dash.lastMatch != null)
                      Expanded(child: LastMatchCard(match: dash.lastMatch!)),
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
