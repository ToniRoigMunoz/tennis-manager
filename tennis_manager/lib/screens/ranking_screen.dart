import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/league_provider.dart';
import 'widgets/league_header_card.dart';
import 'widgets/league_standing_row.dart';
import 'widgets/error_view.dart';

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
    return ref
        .watch(leagueProvider)
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) =>
              ErrorView(onRetry: () => ref.invalidate(leagueProvider)),
          data: (league) => Column(
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
                    final s = league.standings[index];
                    return SizedBox(
                      height: _rowHeight,
                      child: LeagueStandingRow(
                        standing: s,
                        qualifiesForFinals:
                            s.position <= league.info.qualificationSlots,
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        );
  }
}
