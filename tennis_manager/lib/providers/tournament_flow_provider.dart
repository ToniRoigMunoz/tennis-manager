import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../config.dart';
import '../models/tournament_bracket_models.dart';
import '../services/api_service.dart';

class TournamentFlowState {
  final TournamentStep step;

  const TournamentFlowState({required this.step});

  bool get canPlay => step.humanPlays && step.opponent != null;
  bool get isFinished => step.isFinished;
  bool get isWaiting => step.isWaitingForRound;
  bool get isNoMatch => step.isNoPendingMatch;
}

class TournamentFlowNotifier extends AsyncNotifier<TournamentFlowState> {
  @override
  Future<TournamentFlowState> build() async {
    final json = await ApiService.getTournamentStatus(Config.demoUserId);
    return TournamentFlowState(step: TournamentStep.fromJson(json));
  }

  // Reporta el resultado del partido animado y avanza el torneo.
  // Devuelve el nuevo step (para que la pantalla de bracket sepa qué mostrar).
  Future<TournamentStep> reportResultAndAdvance({
    required bool humanWon,
    required String setsScore,
  }) async {
    final json = await ApiService.advanceTournament(
      userId: Config.demoUserId,
      humanWon: humanWon,
      setsScore: setsScore,
    );
    final newStep = TournamentStep.fromJson(json);
    // Actualizar el estado del provider con el nuevo step
    state = AsyncData(TournamentFlowState(step: newStep));
    return newStep;
  }

  // Refresca el estado desde el servidor (p.ej. al volver a General)
  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final json = await ApiService.getTournamentStatus(Config.demoUserId);
      return TournamentFlowState(step: TournamentStep.fromJson(json));
    });
  }
}

final tournamentFlowProvider =
    AsyncNotifierProvider<TournamentFlowNotifier, TournamentFlowState>(
      TournamentFlowNotifier.new,
    );
