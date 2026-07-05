import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/league_provider.dart';
import 'widgets/league_header_card.dart';
import 'widgets/league_standing_row.dart';

class RankingScreen extends ConsumerStatefulWidget {
  const RankingScreen({super.key});

  @override
  ConsumerState<RankingScreen> createState() => _RankingScreenState();
}

class _RankingScreenState extends ConsumerState<RankingScreen> {
  final ScrollController _scrollController = ScrollController();
  static const double _rowHeight = 60;

  void _scrollToMyPosition(int userPosition) {
    _scrollController.animateTo(
      (userPosition - 1) * _rowHeight,
      duration: const Duration(milliseconds: 400),
      curve: Curves.easeInOut,
    );
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final league = ref.watch(leagueProvider);

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: LeagueHeaderCard(
            league: league.info,
            userPosition: league.userPosition,
            onLocateMeTap: () => _scrollToMyPosition(league.userPosition),
          ),
        ),
        Expanded(
          child: ListView.builder(
            controller: _scrollController,
            padding: const EdgeInsets.only(bottom: 16),
            itemCount: league.standings.length,
            itemBuilder: (context, index) {
              final standing = league.standings[index];
              final qualifies =
                  standing.position <= league.info.qualificationSlots;
              return SizedBox(
                height: _rowHeight,
                child: LeagueStandingRow(
                  standing: standing,
                  qualifiesForFinals: qualifies,
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
