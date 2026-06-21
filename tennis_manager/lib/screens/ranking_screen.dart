import 'package:flutter/material.dart';
import '../models/league_models.dart';
import 'widgets/league_header_card.dart';
import 'widgets/league_standing_row.dart';

class RankingScreen extends StatefulWidget {
  const RankingScreen({super.key});

  @override
  State<RankingScreen> createState() => _RankingScreenState();
}

class _RankingScreenState extends State<RankingScreen> {
  final ScrollController _scrollController = ScrollController();
  static const double _rowHeight = 60;

  // Datos de ejemplo. Más adelante vendrán de Cosmos DB vía Azure Functions.
  static const _league = LeagueInfo(
    name: 'Liga Élite · Grupo 3',
    totalPlayers: 24,
    qualificationSlots: 8,
    seasonEndsLabel: 'Termina en 12 días',
  );

  static const _standings = <LeagueStanding>[
    LeagueStanding(
      position: 1,
      name: 'Marc Aguilar',
      points: 4820,
      recentForm: [true, true, true],
    ),
    LeagueStanding(
      position: 2,
      name: 'Núria Castell',
      points: 4690,
      recentForm: [true, false, true],
    ),
    LeagueStanding(
      position: 3,
      name: 'Iker Bilbao',
      points: 4490,
      recentForm: [true, true, false],
    ),
    LeagueStanding(
      position: 4,
      name: 'Pau Soler',
      points: 4205,
      recentForm: [false, true, true],
    ),
    LeagueStanding(
      position: 5,
      name: 'Diego Roma',
      points: 4102,
      recentForm: [true, false, false],
    ),
    LeagueStanding(
      position: 6,
      name: 'Laura Vidal',
      points: 3980,
      recentForm: [false, true, true],
    ),
    LeagueStanding(
      position: 7,
      name: 'Hugo Prats',
      points: 3850,
      recentForm: [true, true, true],
    ),
    LeagueStanding(
      position: 8,
      name: 'Mireia Costa',
      points: 3780,
      recentForm: [true, false, true],
    ),
    LeagueStanding(
      position: 9,
      name: 'Adrián Ruiz',
      points: 3720,
      recentForm: [false, false, true],
    ),
    LeagueStanding(
      position: 10,
      name: 'Sara Llopis',
      points: 3680,
      recentForm: [true, false, false],
    ),
    LeagueStanding(
      position: 11,
      name: 'Tú',
      points: 3640,
      isCurrentUser: true,
      recentForm: [false, true, false],
    ),
    LeagueStanding(
      position: 12,
      name: 'Bruno Ferrer',
      points: 3590,
      recentForm: [false, false, true],
    ),
    LeagueStanding(
      position: 13,
      name: 'Clara Munté',
      points: 3520,
      recentForm: [false, true, false],
    ),
    LeagueStanding(
      position: 14,
      name: 'Toni Beltrán',
      points: 3470,
      recentForm: [true, false, false],
    ),
    LeagueStanding(
      position: 15,
      name: 'Eva Domingo',
      points: 3410,
      recentForm: [false, true, false],
    ),
    LeagueStanding(
      position: 16,
      name: 'Raúl Esteve',
      points: 3350,
      recentForm: [false, false, true],
    ),
    LeagueStanding(
      position: 17,
      name: 'Marina Soto',
      points: 3290,
      recentForm: [true, true, false],
    ),
    LeagueStanding(
      position: 18,
      name: 'Jordi Pla',
      points: 3230,
      recentForm: [false, false, false],
    ),
    LeagueStanding(
      position: 19,
      name: 'Lucía Ferrando',
      points: 3170,
      recentForm: [true, false, true],
    ),
    LeagueStanding(
      position: 20,
      name: 'Pablo Sanchís',
      points: 3100,
      recentForm: [false, true, false],
    ),
    LeagueStanding(
      position: 21,
      name: 'Andrea Gil',
      points: 3040,
      recentForm: [false, false, true],
    ),
    LeagueStanding(
      position: 22,
      name: 'Víctor Calatayud',
      points: 2980,
      recentForm: [true, false, false],
    ),
    LeagueStanding(
      position: 23,
      name: 'Carla Mora',
      points: 2920,
      recentForm: [false, false, false],
    ),
    LeagueStanding(
      position: 24,
      name: 'Òscar Beneyto',
      points: 2860,
      recentForm: [false, true, false],
    ),
  ];

  void _scrollToMyPosition() {
    final myIndex = _standings.indexWhere((s) => s.isCurrentUser);
    if (myIndex == -1) return;
    _scrollController.animateTo(
      myIndex * _rowHeight,
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
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: LeagueHeaderCard(
            league: _league,
            userPosition: _standings
                .firstWhere((s) => s.isCurrentUser)
                .position,
            onLocateMeTap: _scrollToMyPosition,
          ),
        ),
        Expanded(
          child: ListView.builder(
            controller: _scrollController,
            padding: const EdgeInsets.only(bottom: 16),
            itemCount: _standings.length,
            itemBuilder: (context, index) {
              final standing = _standings[index];
              final qualifies = standing.position <= _league.qualificationSlots;
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
