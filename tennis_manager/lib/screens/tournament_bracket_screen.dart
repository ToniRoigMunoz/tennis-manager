import 'package:flutter/material.dart';
import '../models/tournament_bracket_models.dart';
import 'widgets/rewards_panel.dart';

class TournamentBracketScreen extends StatelessWidget {
  final TournamentStep step;
  final void Function(BuildContext) onContinue;

  const TournamentBracketScreen({
    super.key,
    required this.step,
    required this.onContinue,
  });

  @override
  Widget build(BuildContext context) {
    final finished = step.isFinished;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          step.tournamentName ?? 'Torneo',
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        automaticallyImplyLeading: false,
      ),
      body: Column(
        children: [
          if (finished) _FinishedBanner(step: step),
          if (finished && step.rewards != null)
            RewardsPanel(rewards: step.rewards!),
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
              itemCount: step.history.length,
              itemBuilder: (context, index) {
                final round = step.history[index];
                return _RoundBlock(round: round);
              },
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () => onContinue(context),
                  icon: const Icon(Icons.check_rounded),
                  label: const Text('Continuar'),
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FinishedBanner extends StatelessWidget {
  final TournamentStep step;
  const _FinishedBanner({required this.step});

  @override
  Widget build(BuildContext context) {
    final won = step.humanWonTournament ?? false;
    final color = won ? Colors.amber : Colors.blueGrey;
    final icon = won ? Icons.emoji_events_rounded : Icons.flag_rounded;
    final title = won ? '¡Campeón del torneo!' : 'Eliminado del torneo';
    final subtitle = won
        ? 'Has ganado ${step.tournamentName}'
        : 'Campeón: ${step.championName ?? "—"}';

    return Container(
      width: double.infinity,
      margin: const EdgeInsets.fromLTRB(16, 16, 16, 0),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withValues(alpha: 0.5)),
      ),
      child: Column(
        children: [
          Icon(
            icon,
            color: won ? Colors.amber.shade700 : Colors.blueGrey,
            size: 34,
          ),
          const SizedBox(height: 8),
          Text(
            title,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
          ),
          const SizedBox(height: 2),
          Text(
            subtitle,
            style: TextStyle(
              fontSize: 13,
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ),
    );
  }
}

class _RoundBlock extends StatelessWidget {
  final BracketRound round;
  const _RoundBlock({required this.round});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, top: 8, bottom: 8),
          child: Text(
            round.roundName.toUpperCase(),
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              letterSpacing: 0.6,
              color: cs.primary,
            ),
          ),
        ),
        ...round.results.map((m) => _MatchRow(match: m)),
        const SizedBox(height: 8),
      ],
    );
  }
}

class _MatchRow extends StatelessWidget {
  final BracketMatchResult match;
  const _MatchRow({required this.match});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final pending =
        match.winnerName == '(pendiente)' || match.winnerName.isEmpty;

    return Container(
      margin: const EdgeInsets.only(bottom: 6),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: match.involvesHuman
            ? cs.primary.withValues(alpha: 0.08)
            : cs.surface,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: match.involvesHuman
              ? cs.primary.withValues(alpha: 0.3)
              : cs.outlineVariant.withValues(alpha: 0.4),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _PlayerLine(
                  name: match.p1Name,
                  isWinner: !pending && match.winnerName == match.p1Name,
                  human: match.involvesHuman,
                ),
                const SizedBox(height: 2),
                _PlayerLine(
                  name: match.p2Name,
                  isWinner: !pending && match.winnerName == match.p2Name,
                  human: match.involvesHuman,
                ),
              ],
            ),
          ),
          if (pending)
            Text(
              '—',
              style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant),
            )
          else
            Text(
              match.setsScore,
              style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant),
            ),
        ],
      ),
    );
  }
}

class _PlayerLine extends StatelessWidget {
  final String name;
  final bool isWinner;
  final bool human;
  const _PlayerLine({
    required this.name,
    required this.isWinner,
    required this.human,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      children: [
        if (isWinner)
          Padding(
            padding: const EdgeInsets.only(right: 4),
            child: Icon(
              Icons.chevron_right_rounded,
              size: 14,
              color: cs.primary,
            ),
          ),
        Flexible(
          child: Text(
            name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              fontSize: 13,
              fontWeight: isWinner ? FontWeight.bold : FontWeight.normal,
              color: isWinner ? cs.onSurface : cs.onSurfaceVariant,
            ),
          ),
        ),
      ],
    );
  }
}
