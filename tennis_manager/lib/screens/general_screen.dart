import 'package:flutter/material.dart';
import '../models/dashboard_models.dart';
import 'widgets/next_match_card.dart';
import 'widgets/ranking_summary_card.dart';
import 'widgets/last_match_card.dart';
import 'widgets/upcoming_tournaments_strip.dart';

class GeneralScreen extends StatelessWidget {
  final VoidCallback onRankingTap;
  final VoidCallback onTournamentsTap;

  const GeneralScreen({
    super.key,
    required this.onRankingTap,
    required this.onTournamentsTap,
  });

  // Datos de ejemplo. Más adelante vendrán de Cosmos DB vía Azure Functions.
  static final _nextMatch = NextMatchInfo(
    opponentName: 'Carlos Ferrer',
    tournamentName: 'Masters de Valencia',
    round: 'Octavos de final',
    dateTime: DateTime(2026, 6, 22, 18, 30),
    surface: 'Tierra batida',
  );

  static const _topRanking = [
    RankingEntry(position: 1, name: 'Marc Aguilar', points: 4820),
    RankingEntry(position: 2, name: 'Tú', points: 4510, isCurrentUser: true),
    RankingEntry(position: 3, name: 'Iker Bilbao', points: 4490),
    RankingEntry(position: 4, name: 'Pau Soler', points: 4205),
    RankingEntry(position: 5, name: 'Diego Roma', points: 4102),
  ];

  static const _lastMatch = LastMatchInfo(
    opponentName: 'Iker Bilbao',
    won: true,
    setsScore: '6-4, 7-5',
    aces: 8,
    winners: 24,
    unforcedErrors: 14,
  );

  static const _upcomingTournaments = [
    UpcomingTournamentInfo(name: 'Masters de Valencia', dateLabel: '22 jun'),
    UpcomingTournamentInfo(name: 'Open de Madrid', dateLabel: '29 jun'),
    UpcomingTournamentInfo(name: 'Copa Mediterráneo', dateLabel: '6 jul'),
  ];

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final topHeight = constraints.maxHeight * 0.33;

        return Column(
          children: [
            SizedBox(
              height: topHeight,
              width: double.infinity,
              child: NextMatchCard(match: _nextMatch),
            ),
            const SizedBox(height: 16),
            UpcomingTournamentsStrip(
              tournaments: _upcomingTournaments,
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
                        entries: _topRanking,
                        onTap: onRankingTap,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: LastMatchCard(match: _lastMatch)),
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
