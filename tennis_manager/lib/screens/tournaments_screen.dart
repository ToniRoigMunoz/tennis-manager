import 'package:flutter/material.dart';
import '../models/tournament_models.dart';
import 'widgets/season_progress_header.dart';
import 'widgets/tournament_timeline_item.dart';

class TournamentsScreen extends StatelessWidget {
  const TournamentsScreen({super.key});

  // Datos de ejemplo. Más adelante vendrán de Cosmos DB vía Azure Functions.
  static const _currentDay = 9;
  static const _totalDays = 28;

  static const _tournaments = <TournamentInfo>[
    TournamentInfo(
      name: 'Open de Castilla',
      startDay: 2,
      durationDays: 1,
      surface: 'Pista dura',
      category: TournamentCategory.regular,
      status: TournamentStatus.past,
      dateLabel: '15 jun',
      resultLabel: 'Eliminado en 2ª ronda',
    ),
    TournamentInfo(
      name: 'Grand Slam de Roland Sur',
      startDay: 5,
      durationDays: 2,
      surface: 'Tierra batida',
      category: TournamentCategory.grandSlam,
      status: TournamentStatus.past,
      dateLabel: '18-19 jun',
      resultLabel: 'Cuartos de final',
    ),
    TournamentInfo(
      name: 'Masters de Valencia',
      startDay: 9,
      durationDays: 1,
      surface: 'Tierra batida',
      category: TournamentCategory.regular,
      status: TournamentStatus.current,
      dateLabel: '22 jun',
    ),
    TournamentInfo(
      name: 'Open de Madrid',
      startDay: 16,
      durationDays: 1,
      surface: 'Pista dura',
      category: TournamentCategory.regular,
      status: TournamentStatus.upcoming,
      dateLabel: '29 jun',
    ),
    TournamentInfo(
      name: 'Copa Mediterráneo',
      startDay: 23,
      durationDays: 1,
      surface: 'Tierra batida',
      category: TournamentCategory.regular,
      status: TournamentStatus.upcoming,
      dateLabel: '6 jul',
    ),
    TournamentInfo(
      name: 'Grand Slam Costa Azul',
      startDay: 25,
      durationDays: 2,
      surface: 'Hierba',
      category: TournamentCategory.grandSlam,
      status: TournamentStatus.upcoming,
      dateLabel: '8-9 jul',
    ),
    TournamentInfo(
      name: 'Finales ATP',
      startDay: 27,
      durationDays: 2,
      surface: 'Pista dura',
      category: TournamentCategory.finals,
      status: TournamentStatus.upcoming,
      dateLabel: '10-11 jul',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const Padding(
          padding: EdgeInsets.fromLTRB(16, 16, 16, 8),
          child: SeasonProgressHeader(
            currentDay: _currentDay,
            totalDays: _totalDays,
          ),
        ),
        Expanded(
          child: ListView.builder(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            itemCount: _tournaments.length,
            itemBuilder: (context, index) => TournamentTimelineItem(
              tournament: _tournaments[index],
              isFirst: index == 0,
              isLast: index == _tournaments.length - 1,
            ),
          ),
        ),
      ],
    );
  }
}
