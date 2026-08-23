using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class GetTournamentStatus(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("GetTournamentStatus")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                var userId = req.Query["userId"] ?? "demo-user-001";
                var seasonId = req.Query["seasonId"] ?? "season-2026-01"; // ★ NUEVO
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");

                // 1. ¿Existe ya un torneo activo para este usuario?
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                        userId, new PartitionKey(userId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null; // no hay torneo, lo crearemos abajo
                }

                                // 2a. Si no existe torneo, creamos el del día
                if (state == null)
                {
                    var payload = await TournamentBootstrap.CreateDailyTournament(cosmos, userId);
                    var resNew = req.CreateResponse(HttpStatusCode.OK);
                    resNew.Headers.Add("Content-Type", "application/json");
                    await resNew.WriteStringAsync(JsonSerializer.Serialize(payload, Opts));
                    return resNew;
                }

                // 2b. Si el torneo del día ya terminó, NO creamos otro: "ya jugaste hoy"
                if (state.Finished || !state.HumanAlive)
                {
                    var doneResult = await SeasonDayDonePayload(state);
                    var resDone = req.CreateResponse(HttpStatusCode.OK);
                    resDone.Headers.Add("Content-Type", "application/json");
                    await resDone.WriteStringAsync(JsonSerializer.Serialize(doneResult, Opts));
                    return resDone;
                }

                // ★ NUEVO: cargar la temporada para consultar el reloj
                var toursC = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursC.ReadItemAsync<TournamentDocument>(
                    seasonId, new PartitionKey(seasonId))).Resource;

                // ★ NUEVO: poner al día el torneo (simular rondas cuya ventana ya se cerró)
                await CatchUpClosedRounds(state, season);
                await atContainer.UpsertItemAsync(state, new PartitionKey(userId));

                // ★ NUEVO: si la puesta al día terminó el torneo, lo indicamos
                if (state.Finished)
                {
                    var resFin = req.CreateResponse(HttpStatusCode.OK);
                    resFin.Headers.Add("Content-Type", "application/json");
                    await resFin.WriteStringAsync(JsonSerializer.Serialize(
                        new { status = "noPendingMatch", tournamentName = state.TournamentName }, Opts));
                    return resFin;
                }

                // 3. Hay un torneo en curso: buscar el partido pendiente del humano
                var pendingMatch = FindPendingHumanMatch(state);
                object result;

                if (pendingMatch != null)
                {
                    // ★ NUEVO: gating por reloj
                    int clockRound = ServerClock.CurrentUnlockedRound(season);
                    int humanRound = state.HumanRoundIndex;

                    if (clockRound < humanRound)
                    {
                        // El humano ya está listo, pero su ronda aún no se ha desbloqueado
                        result = new
                        {
                            status = "waitingForRound",
                            tournamentName = state.TournamentName,
                            roundName = ServerClock.RoundNameByIndex(state.HumanRoundIndex),
                            unlockUtc = ServerClock.RoundUnlockTime(season, humanRound).ToString("o"),
                            currentRound = humanRound,
                        };
                    }
                    else
                    {
                        // Ventana abierta: el humano puede jugar
                        var opponentName = pendingMatch.P1Name == GetHumanName(state, userId)
                            ? pendingMatch.P2Name
                            : pendingMatch.P1Name;

                        var opponent = state.Survivors.FirstOrDefault(p => p.Name == opponentName && !p.IsHuman);

                        result = new
                        {
                            status = "humanPlays",
                            tournamentName = state.TournamentName,
                            surface = state.Surface,
                            roundName = ServerClock.RoundNameByIndex(state.HumanRoundIndex),
                            opponent = new
                            {
                                id = opponent?.Id ?? "unknown",
                                name = opponentName,
                                overall = opponent?.Overall ?? 70,
                            },
                            seed = state.Seed,
                        };
                    }
                }
                else
                {
                    result = new { status = "noPendingMatch", tournamentName = state.TournamentName };
                }

                var res = req.CreateResponse(HttpStatusCode.OK);
                res.Headers.Add("Content-Type", "application/json");
                await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
                return res;
            }
            catch (Exception ex)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                err.Headers.Add("Content-Type", "application/json");
                await err.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message, stack = ex.StackTrace }));
                return err;
            }
        }

        // ★ NUEVO: simula las rondas del humano cuya ventana horaria ya se cerró
        private async Task CatchUpClosedRounds(ActiveTournamentDoc state, TournamentDocument season)
        {
            int safety = 0;
            while (!state.Finished && safety++ < 10)
            {
                var pending = FindPendingHumanMatch(state);
                if (pending == null) break;

                int humanRound = state.HumanRoundIndex;
                // Si la ventana de esta ronda NO se ha cerrado aún, no hay nada que simular
                if (ServerClock.CurrentUnlockedRound(season) <= humanRound) break;

                // La ventana se cerró sin que el humano jugara: simular su partido
                await SimulateHumanPendingMatch(state);
            }
        }

        // Busca en la última ronda registrada un partido del humano sin resolver
        private static MatchRecord? FindPendingHumanMatch(ActiveTournamentDoc state)
        {
            if (state.History.Count == 0) return null;
            var lastRound = state.History[^1];
            return lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId));
        }

        private static string GetHumanName(ActiveTournamentDoc state, string userId)
        {
            var human = state.Survivors.FirstOrDefault(p => p.IsHuman);
            if (human != null) return human.Name;
            foreach (var round in state.History)
                foreach (var m in round.Results.Where(r => r.InvolvesHuman))
                    return m.WinnerId == userId ? m.WinnerName : (m.P1Name);
            return "";
        }

        // Simula UN partido pendiente del humano e integra el resultado en el cuadro.
        // Reutiliza las piezas del orquestador. Al terminar, el humano tiene un nuevo
        // pendiente (si sigue vivo) o el torneo queda resuelto.
        private async Task SimulateHumanPendingMatch(ActiveTournamentDoc state)
        {
            // Rehidratar los Sim de los supervivientes (no se persisten)
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);
            foreach (var p in state.Survivors)
                if (p.Sim == null && simById.TryGetValue(p.Id, out var sim))
                    p.Sim = sim;

            var human = all.FirstOrDefault(p => p.Id == state.UserId);
            if (human == null) return;

            var lastRound = state.History[^1];
            var humanMatch = lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId));
            if (humanMatch == null) return;

            var opponentName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;
            var opponent = all.FirstOrDefault(p => p.Name == opponentName && p.Id != human.Id);
            if (opponent == null) return;

            // Simular el partido del humano
            int matchSeed = state.Seed + state.HumanRoundIndex * 7919 + 31;
            var (winnerId, score) = TournamentBracket.SimulateFast(human, opponent, matchSeed);
            bool humanWon = winnerId == human.Id;

            humanMatch.WinnerId = winnerId;
            humanMatch.WinnerName = humanWon ? human.Name : opponent.Name;
            humanMatch.SetsScore = score;

            if (humanWon)
            {
                state.ReachedRound[opponent.Id] = RoundSizeFromName(lastRound.RoundName);
                state.Survivors.RemoveAll(p => p.Name == opponentName && !p.IsHuman);
                if (!state.Survivors.Any(p => p.IsHuman)) state.Survivors.Add(human);
            }
            else
            {
                state.ReachedRound[state.UserId] = RoundSizeFromName(lastRound.RoundName);
                state.HumanAlive = false;
                state.HumanEliminatedRound = RoundSizeFromName(lastRound.RoundName);
                if (!state.Survivors.Any(p => p.Id == opponent.Id)) state.Survivors.Add(opponent);
            }

            // Avanzar el estado del torneo tras integrar el resultado
            if (!state.HumanAlive)
            {
                var (champion, reached, history) = TournamentOrchestrator.ResolveRemainingFully(
                    state.Survivors, state.Seed + 50000, state.ReachedRound);
                state.ReachedRound = reached;
                state.History.AddRange(history);
                state.Finished = true;
                state.ChampionId = champion.Id;
            }
            else if (state.Survivors.Count == 1 && state.Survivors[0].IsHuman)
            {
                state.Finished = true;
                state.ChampionId = state.UserId;
                state.ReachedRound[state.UserId] = 1;
            }
            else
            {
                state.CurrentRound++;
                var (matches, nextHumanMatch, advancing) =
                    TournamentOrchestrator.ResolveRoundSkippingHuman(
                        state.Survivors, state.Seed + state.CurrentRound * 1000, humanAlive: true);

                RecordRound(state, matches, TournamentBracket.RoundName(state.Survivors.Count));
                foreach (var m in matches.Where(m => m.WinnerId != null))
                {
                    var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                    state.ReachedRound[loser.Id] = state.Survivors.Count;
                }
                state.Survivors = advancing;
                state.HumanRoundIndex = state.History.Count; // la nueva ronda pendiente
            }
        }

        private static void RecordRound(ActiveTournamentDoc state, List<BracketMatch> matches, string roundName)
        {
            var rec = new RoundRecord { RoundName = roundName };
            foreach (var m in matches)
            {
                var winnerName = m.WinnerId == null ? "(pendiente)"
                    : (m.WinnerId == m.Player1!.Id ? m.Player1!.Name : m.Player2!.Name);
                rec.Results.Add(new MatchRecord
                {
                    P1Name = m.Player1!.Name,
                    P2Name = m.Player2!.Name,
                    WinnerId = m.WinnerId ?? "",
                    WinnerName = winnerName,
                    SetsScore = m.SetsScore,
                    InvolvesHuman = m.InvolvesHuman,
                });
            }
            state.History.Add(rec);
        }

        private static int RoundSizeFromName(string roundName) => roundName switch
        {
            "Final" => 2,
            "Semifinales" => 4,
            "Cuartos de final" => 8,
            "Octavos de final" => 16,
            _ => 16,
        };

                // Payload para "ya jugaste hoy": indica el torneo de mañana
        private async Task<object> SeasonDayDonePayload(ActiveTournamentDoc state)
        {
            string nextTournamentName = "Próximo torneo";
            try
            {
                var toursC = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursC.ReadItemAsync<TournamentDocument>(
                    "season-2026-01", new PartitionKey("season-2026-01"))).Resource;
                var next = season.Tournaments.FirstOrDefault(t => t.StartDay == season.CurrentDay + 1)
                           ?? season.Tournaments.FirstOrDefault(t => t.StartDay == season.CurrentDay);
                if (next != null) nextTournamentName = next.Name;
            }
            catch { /* si falla, usamos el texto por defecto */ }

            return new
            {
                status = "seasonDayDone",
                todayTournamentName = state.TournamentName,
                nextTournamentName,
            };
        }
    }
}