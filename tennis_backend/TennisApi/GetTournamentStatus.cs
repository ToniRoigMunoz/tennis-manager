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
                var seasonId = req.Query["seasonId"] ?? "season-2026-01";
                var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");

                // La liga del usuario que consulta (el torneo vive por liga)
                var leagueId = await LeagueLookup.GetLeagueId(cosmos, userId);

                // ¿Existe ya un torneo activo para esta liga?
                ActiveTournamentDoc? state = null;
                try
                {
                    state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                        leagueId, new PartitionKey(leagueId))).Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    state = null; // No hay torneo, lo crearemos abajo
                }

                                // Si no existe torneo, creamos el del día
                if (state == null)
                {
                    var payload = await TournamentBootstrap.CreateDailyTournament(cosmos, userId);
                    var resNew = req.CreateResponse(HttpStatusCode.OK);
                    resNew.Headers.Add("Content-Type", "application/json");
                    await resNew.WriteStringAsync(JsonSerializer.Serialize(payload, Opts));
                    return resNew;
                }

                // Si el torneo del día ya terminó, no creamos otro
                if (state.Finished || !state.IsAlive(userId))
                {
                    var doneResult = await SeasonDayDonePayload(state);
                    var resDone = req.CreateResponse(HttpStatusCode.OK);
                    resDone.Headers.Add("Content-Type", "application/json");
                    await resDone.WriteStringAsync(JsonSerializer.Serialize(doneResult, Opts));
                    return resDone;
                }

                // Cargar la temporada para consultar el reloj
                var toursC = cosmos.GetContainer("TennisManagerDB", "tournaments");
                var season = (await toursC.ReadItemAsync<TournamentDocument>(
                    seasonId, new PartitionKey(seasonId))).Resource;

                // Poner al día el torneo (simular rondas cuya ventana ya se cerró)
                await TournamentClockSync.SyncRoundsToClock(cosmos, state, season);
                await atContainer.UpsertItemAsync(state, new PartitionKey(leagueId));

                // Si la puesta al día terminó el torneo, lo indicamos
                if (state.Finished)
                {
                    var resFin = req.CreateResponse(HttpStatusCode.OK);
                    resFin.Headers.Add("Content-Type", "application/json");
                    await resFin.WriteStringAsync(JsonSerializer.Serialize(
                        new { status = "noPendingMatch", tournamentName = state.TournamentName }, Opts));
                    return resFin;
                }

                // Hay un torneo en curso: buscar el partido pendiente del humano
                var humanName = HumanNameOf(state, userId);
                var pendingMatch = FindPendingHumanMatch(state, humanName);
                object result;

                if (pendingMatch != null)
                {
                    // Gating por reloj
                    int clockRound = ServerClock.CurrentUnlockedRound(season);
                    int humanRound = state.RoundIndexOf(userId);

                    if (clockRound < humanRound)
                    {
                        // El humano ya está listo, pero su ronda aún no se ha desbloqueado
                        result = new
                        {
                            status = "waitingForRound",
                            tournamentName = state.TournamentName,
                            roundName = ServerClock.RoundNameByIndex(state.RoundIndexOf(userId)),
                            unlockUtc = ServerClock.RoundUnlockTime(season, humanRound).ToString("o"),
                            currentRound = humanRound,
                        };
                    }
                    else
                    {
                        // Ventana abierta: el humano puede jugar
                        var opponentName = pendingMatch.P1Name == humanName
                            ? pendingMatch.P2Name
                            : pendingMatch.P1Name;

                        var opponent = state.Survivors.FirstOrDefault(p => p.Name == opponentName && !p.IsHuman);

                        result = new
                        {
                            status = "humanPlays",
                            tournamentName = state.TournamentName,
                            surface = state.Surface,
                            roundName = ServerClock.RoundNameByIndex(state.RoundIndexOf(userId)),
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

                // Sincroniza el torneo con el reloj: por cada ronda cuya ventana se cerró,
        // simula los partidos humanos no jugados y monta la siguiente ronda (para todos).
        private async Task SyncRoundsToClock(ActiveTournamentDoc state, TournamentDocument season)
        {
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);
            var byName = all.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());

            int safety = 0;
            while (!state.Finished && safety++ < 12)
            {
                int assembledRound = state.History.Count;                 // última ronda montada
                int clockWindow = ServerClock.CurrentUnlockedRound(season);

                // Si el reloj no ha pasado de la ronda montada, no hay nada que cerrar aún
                if (clockWindow <= assembledRound) break;

                var lastRound = state.History[^1];
                int roundSize = RoundSizeFromName(lastRound.RoundName);

                // 1. Simular los partidos humanos NO jugados de esta ronda (ventana cerrada)
                foreach (var m in lastRound.Results.Where(r => r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId)).ToList())
                {
                    if (!byName.TryGetValue(m.P1Name, out var p1) || !byName.TryGetValue(m.P2Name, out var p2)) continue;
                    if (p1.Sim == null && simById.TryGetValue(p1.Id, out var s1)) p1.Sim = s1;
                    if (p2.Sim == null && simById.TryGetValue(p2.Id, out var s2)) p2.Sim = s2;

                    int matchSeed = state.Seed + assembledRound * 7919 + m.P1Name.GetHashCode();
                    var (winnerId, score) = TournamentBracket.SimulateFast(p1, p2, matchSeed);
                    var winner = winnerId == p1.Id ? p1 : p2;
                    var loser = winnerId == p1.Id ? p2 : p1;

                    m.WinnerId = winner.Id;
                    m.WinnerName = winner.Name;
                    m.SetsScore = score;

                    // El perdedor (humano o bot) cae
                    state.ReachedRound[loser.Id] = roundSize;
                    if (loser.IsHuman && state.HumanStates.TryGetValue(loser.Id, out var hsL))
                    {
                        hsL.Alive = false;
                        hsL.EliminatedRound = roundSize;
                    }
                    // El ganador entra en supervivientes (sin duplicar)
                    if (!state.Survivors.Any(p => p.Id == winner.Id)) state.Survivors.Add(winner);
                    state.Survivors.RemoveAll(p => p.Id == loser.Id);
                }

                // 2. ¿Queda un solo superviviente? Campeón.
                if (state.Survivors.Count <= 1)
                {
                    state.Finished = true;
                    if (state.Survivors.Count == 1)
                    {
                        state.ChampionId = state.Survivors[0].Id;
                        state.ReachedRound[state.Survivors[0].Id] = 1;
                    }
                    break;
                }

                // 3. Montar la siguiente ronda (una sola vez, con todos los supervivientes)
                foreach (var p in state.Survivors)
                    if (p.Sim == null && simById.TryGetValue(p.Id, out var sim)) p.Sim = sim;

                state.CurrentRound++;
                var aliveHumans = state.HumanStates.Where(kv => kv.Value.Alive).Select(kv => kv.Key).ToHashSet();
                var (matches, humanMatches, advancing) = TournamentOrchestrator.ResolveRoundMultiHuman(
                    state.Survivors, state.Seed + state.CurrentRound * 1000, aliveHumans);

                RecordRound(state, matches, TournamentBracket.RoundName(state.Survivors.Count));
                foreach (var mm in matches.Where(x => x.WinnerId != null))
                {
                    var loser = mm.WinnerId == mm.Player1!.Id ? mm.Player2! : mm.Player1!;
                    state.ReachedRound[loser.Id] = state.Survivors.Count;
                }
                state.Survivors = advancing;

                // Actualizar la ronda de cada humano vivo que sigue en el cuadro
                int newRoundIndex = state.History.Count;
                foreach (var hm in humanMatches)
                {
                    foreach (var pid in new[] { hm.Player1!.Id, hm.Player2!.Id })
                        if (state.HumanStates.TryGetValue(pid, out var hs)) hs.RoundIndex = newRoundIndex;
                }
            }
        }

        // Busca el partido pendiente de UN humano concreto (por su nombre) en la última ronda
        private static MatchRecord? FindPendingHumanMatch(ActiveTournamentDoc state, string humanName)
        {
            if (state.History.Count == 0) return null;
            var lastRound = state.History[^1];
            return lastRound.Results.FirstOrDefault(r =>
                r.InvolvesHuman && string.IsNullOrEmpty(r.WinnerId) &&
                (r.P1Name == humanName || r.P2Name == humanName));
        }

        private static string HumanNameOf(ActiveTournamentDoc state, string userId)
        {
            if (state.HumanStates.TryGetValue(userId, out var hs) && !string.IsNullOrEmpty(hs.Name))
                return hs.Name;
            return ""; // respaldo
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