using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace TennisApi
{
    public class AdvanceTournamentPayload
    {
        public string UserId { get; set; } = "";
        public bool HumanWon { get; set; }
        public string SetsScore { get; set; } = "";
    }

    public class AdvanceTournament(CosmosClient cosmos)
    {
        private static readonly JsonSerializerOptions Opts =
            new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Function("AdvanceTournament")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<AdvanceTournamentPayload>(body, Opts);
            if (payload is null || string.IsNullOrEmpty(payload.UserId))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeTournaments");
            var state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                payload.UserId, new PartitionKey(payload.UserId))).Resource;

            if (state.Finished)
                return req.CreateResponse(HttpStatusCode.Conflict);

            await RehydrateSims(state);

            // 1. Localizar al humano y su partido pendiente en la ronda actual
            var lastRound = state.History[^1];
            var humanMatch = lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && r.WinnerId == "");
            if (humanMatch is null)
                return req.CreateResponse(HttpStatusCode.Conflict);

            var human = FindHuman(state);
            if (human is null) return req.CreateResponse(HttpStatusCode.NotFound);

            // 2. Integrar el resultado del partido animado
            var opponentName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;

            if (payload.HumanWon)
            {
                humanMatch.WinnerId = state.UserId;
                humanMatch.WinnerName = human.Name;
                humanMatch.SetsScore = payload.SetsScore;

                // El rival batido no puede seguir vivo
                var beatenName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;
                state.Survivors.RemoveAll(p => p.Name == beatenName && !p.IsHuman);

                // El humano se une a los supervivientes de esta ronda (evitando duplicados)
                if (!state.Survivors.Any(p => p.IsHuman))
                    state.Survivors.Add(human);
            }
            else
            {
                // El humano pierde: gana el rival, que ya está resuelto de fuerza
                var opponent = FindOpponentInSurvivorsPool(state, opponentName, human);
                humanMatch.WinnerId = opponent?.Id ?? "opponent";
                humanMatch.WinnerName = opponentName;
                humanMatch.SetsScore = payload.SetsScore;

                // Registrar hasta dónde llegó el humano
                var playersThisRound = state.Survivors.Count + 1; // +1 por el propio humano que sale
                state.ReachedRound[state.UserId] = RoundSizeFromName(lastRound.RoundName);
                state.HumanAlive = false;
                state.HumanEliminatedRound = RoundSizeFromName(lastRound.RoundName);

                // Añadir al rival que venció al humano a los supervivientes
                if (opponent != null) state.Survivors.Add(opponent);
            }

            // 3. Decidir el siguiente paso
            object result;
            if (!state.HumanAlive)
            {
                // El humano cayó: resolvemos el resto del torneo de golpe
                var (champion, reached, history) = TournamentOrchestrator.ResolveRemainingFully(
                    state.Survivors, state.Seed + 50000, state.ReachedRound);

                state.ReachedRound = reached;
                state.History.AddRange(history);
                state.Finished = true;
                state.ChampionId = champion.Id;

                result = new
                {
                    status = "finished",
                    humanWonTournament = false,
                    humanEliminatedRound = state.HumanEliminatedRound,
                    championName = champion.Name,
                    history = state.History,
                };
            }
            else
            {
                // El humano sigue vivo: ¿es ya el único que queda?
                if (state.Survivors.Count == 1 && state.Survivors[0].IsHuman)
                {
                    state.Finished = true;
                    state.ChampionId = state.UserId;
                    state.ReachedRound[state.UserId] = 1;
                    result = new
                    {
                        status = "finished",
                        humanWonTournament = true,
                        championName = human.Name,
                        history = state.History,
                    };
                }
                else
                {
                    state.CurrentRound++;

                    // Nº real de jugadores en esta ronda = supervivientes vivos ahora mismo
                    // (incluye al humano recién añadido). Se captura antes de emparejar.
                    int playersInThisRound = state.Survivors.Count;

                    // Resolver la siguiente ronda saltando otra vez al humano
                    var (matches, nextHumanMatch, advancing) =
                        TournamentOrchestrator.ResolveRoundSkippingHuman(
                            state.Survivors, state.Seed + state.CurrentRound * 1000, humanAlive: true);

                    RecordRound(state, matches, TournamentBracket.RoundName(state.Survivors.Count));
                    foreach (var m in matches.Where(m => m.WinnerId != null))
                    {
                        var loser = m.WinnerId == m.Player1!.Id ? m.Player2! : m.Player1!;
                        state.ReachedRound[loser.Id] = state.Survivors.Count;
                    }

                    if (nextHumanMatch != null)
                    {
                        var opp = nextHumanMatch.Player1!.IsHuman ? nextHumanMatch.Player2! : nextHumanMatch.Player1!;
                        state.Survivors = advancing; // pendiente de sumar al humano tras su próximo partido
                        result = new
                        {
                            status = "humanPlays",
                            tournamentName = state.TournamentName,
                            surface = state.Surface,
                            roundName = TournamentBracket.RoundName(playersInThisRound),
                            opponent = new { opp.Id, opp.Name, opp.Overall },
                            history = state.History,
                        };
                    }
                    else
                    {
                        // No debería pasar si el humano sigue vivo, pero por seguridad
                        state.Survivors = advancing;
                        result = new { status = "error", message = "Estado inconsistente" };
                    }
                }
            }

            // 4. Persistir el estado actualizado
            await atContainer.UpsertItemAsync(state, new PartitionKey(payload.UserId));

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json");
            await res.WriteStringAsync(JsonSerializer.Serialize(result, Opts));
            return res;
        }

        // Reconstruye el Participant del humano desde Cosmos (con su Sim)
        private Participant? FindHuman(ActiveTournamentDoc state)
        {
            // El humano puede estar ya en Survivors si avanzó por bye; si no, lo recreamos
            var existing = state.Survivors.FirstOrDefault(p => p.IsHuman);
            if (existing != null) return existing;

            // Reconstruir desde el loader completo
            var all = ParticipantLoader.Load(cosmos, state.LeagueId).GetAwaiter().GetResult();
            return all.FirstOrDefault(p => p.Id == state.UserId);
        }

        private Participant? FindOpponentInSurvivorsPool(ActiveTournamentDoc state, string opponentName, Participant human)
        {
            var all = ParticipantLoader.Load(cosmos, state.LeagueId).GetAwaiter().GetResult();
            return all.FirstOrDefault(p => p.Name == opponentName && p.Id != human.Id);
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

        // Rehidrata el Sim de cada participante del estado (no se persiste en Cosmos)
        private async Task RehydrateSims(ActiveTournamentDoc state)
        {
            var all = await ParticipantLoader.Load(cosmos, state.LeagueId);
            var simById = all.ToDictionary(p => p.Id, p => p.Sim);

            foreach (var p in state.Survivors)
                if (p.Sim == null && simById.TryGetValue(p.Id, out var sim))
                    p.Sim = sim;
        }
    }
}