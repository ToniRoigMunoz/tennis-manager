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

            var atContainer = cosmos.GetContainer("TennisManagerDB", "activeLeagueTournaments");
            var leagueId = await LeagueLookup.GetLeagueId(cosmos, payload.UserId);
            var state = (await atContainer.ReadItemAsync<ActiveTournamentDoc>(
                leagueId, new PartitionKey(leagueId))).Resource;

            if (state.Finished)
                return req.CreateResponse(HttpStatusCode.Conflict);

            // Cargar la temporada para el reloj
            var toursC = cosmos.GetContainer("TennisManagerDB", "tournaments");
            var season = (await toursC.ReadItemAsync<TournamentDocument>(
                "season-2026-01", new PartitionKey("season-2026-01"))).Resource;

            await RehydrateSims(state);

            // Localizar al humano y su partido pendiente en la ronda actual
            var lastRound = state.History[^1];
            var humanMatch = lastRound.Results.FirstOrDefault(r => r.InvolvesHuman && r.WinnerId == "");
            if (humanMatch is null)
                return req.CreateResponse(HttpStatusCode.Conflict);

            var human = FindHuman(state);
            if (human is null) return req.CreateResponse(HttpStatusCode.NotFound);

            // Integrar el resultado del partido animado
            var opponentName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;

            if (payload.HumanWon)
            {
                humanMatch.WinnerId = state.UserId;
                humanMatch.WinnerName = human.Name;
                humanMatch.SetsScore = payload.SetsScore;

                // El rival batido no puede seguir vivo. Lo registramos en ReachedRound antes de quitarlo, para que reciba sus puntos.
                var beatenName = humanMatch.P1Name == human.Name ? humanMatch.P2Name : humanMatch.P1Name;
                var beaten = state.Survivors.FirstOrDefault(p => p.Name == beatenName && !p.IsHuman);
                if (beaten != null)
                {
                    int roundSize = RoundSizeFromName(state.History[^1].RoundName);
                    state.ReachedRound[beaten.Id] = roundSize;
                }
                state.Survivors.RemoveAll(p => p.Name == beatenName && !p.IsHuman);

                // El humano se une a los supervivientes de esta ronda (evitando duplicados)
                if (!state.Survivors.Any(p => p.IsHuman))
                    state.Survivors.Add(human);
            }
            else
            {
                var opponent = FindOpponentInSurvivorsPool(state, opponentName, human);
                humanMatch.WinnerId = opponent?.Id ?? "opponent";
                humanMatch.WinnerName = opponentName;
                humanMatch.SetsScore = payload.SetsScore;

                // Registrar hasta dónde llegó el humano
                var playersThisRound = state.Survivors.Count + 1; // +1 por el propio humano que sale
                state.ReachedRound[state.UserId] = RoundSizeFromName(lastRound.RoundName);
                state.HumanAlive = false;
                if (state.HumanStates.TryGetValue(state.UserId, out var hsLost))
                {
                    hsLost.Alive = false;
                    hsLost.EliminatedRound = RoundSizeFromName(lastRound.RoundName);
                }
                state.HumanEliminatedRound = RoundSizeFromName(lastRound.RoundName);

                // Añadir al rival que venció al humano a los supervivientes
                if (opponent != null) state.Survivors.Add(opponent);
            }

            // Decidir el siguiente paso
            object result = new { status = "error", message = "Estado no resuelto" };
            if (!state.HumanAlive)
            {
                // El humano cayó por lo que no resolvemos el resto del cuadro (se hará al avanzar el día).
                // Solo aplicamos sus recompensas y marcamos que su torneo acabó por hoy.
                var rewards = await ApplyHumanRewards(state, isChampion: false);

                result = new
                {
                    status = "finished",
                    humanWonTournament = false,
                    humanEliminatedRound = state.HumanEliminatedRound,
                    championName = (string?)null, // Aún no se conoce, el cuadro se resuelve luego
                    history = state.History,
                    rewards,
                };
            }
            else
            {
                // El humano sigue vivo, ¿es ya el único que queda?
                if (state.Survivors.Count == 1 && state.Survivors[0].IsHuman)
                {
                    state.Finished = true;
                    state.ChampionId = state.UserId;
                    state.ReachedRound[state.UserId] = 1;

                    var rewards = await ApplyHumanRewards(state, isChampion: true);

                    result = new
                    {
                        status = "finished",
                        humanWonTournament = true,
                        championName = human.Name,
                        history = state.History,
                        rewards,
                    };
                }
                else
                {
                    state.CurrentRound++;

                    // Número real de jugadores en esta ronda igual a supervivientes vivos ahora mismo (incluye al humano recién añadido)
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
                        state.Survivors = advancing;
                        state.HumanRoundIndex = state.History.Count;
                        if (state.HumanStates.TryGetValue(state.UserId, out var hsNext)) 
                        {
                            hsNext.RoundIndex = state.History.Count;
                        }

                        // Gating por reloj: ¿está abierta la ventana de la siguiente ronda?
                        int clockRound = ServerClock.CurrentUnlockedRound(season);
                        if (clockRound < state.HumanRoundIndex)
                        {
                            // Aún no toca, el humano debe esperar a la siguiente ventana
                            result = new
                            {
                                status = "waitingForRound",
                                tournamentName = state.TournamentName,
                                roundName = ServerClock.RoundNameByIndex(state.HumanRoundIndex),
                                unlockUtc = ServerClock.RoundUnlockTime(season, state.HumanRoundIndex).ToString("o"),
                                history = state.History,
                            };
                        }
                        else
                        {
                            // La ventana ya está abierta: puede jugar ya
                            result = new
                            {
                                status = "humanPlays",
                                tournamentName = state.TournamentName,
                                surface = state.Surface,
                                roundName = ServerClock.RoundNameByIndex(state.HumanRoundIndex),
                                opponent = new { opp.Id, opp.Name, opp.Overall },
                                history = state.History,
                            };
                        }
                    }
                }
            }

            // Persistir el estado actualizado
            await atContainer.UpsertItemAsync(state, new PartitionKey(leagueId));

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

        // Aplica las recompensas al humano cuando termina su recorrido en el torneo. Devuelve un resumen para el frontend.
        private async Task<object> ApplyHumanRewards(ActiveTournamentDoc state, bool isChampion)
        {
            // Ronda donde cayó (o 1 si es campeón). ReachedRound guarda el tamaño de ronda.
            int reachedRoundSize = state.ReachedRound.TryGetValue(state.UserId, out var rr) ? rr : 16;

            // Calcular recompensas según categoría y ronda
            int championPoints = TournamentRewards.ChampionPoints(state.Category);
            double fraction = TournamentRewards.PointsFractionByRound(reachedRoundSize, isChampion);
            int pointsEarned = (int)Math.Round(championPoints * fraction);
            int moneyEarned = TournamentRewards.MoneyFromPoints(pointsEarned);
            int restsEarned = TournamentRewards.RestsByRound(reachedRoundSize, isChampion);
            double attrGain = isChampion ? TournamentRewards.ChampionAttributeGain(state.Category) : 0.0;

            // Actualizar el documento del jugador (dinero, descansos, atributos)
            var playersContainer = cosmos.GetContainer("TennisManagerDB", "players");
            var pQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid").WithParameter("@uid", state.UserId);
            var pOpts = new QueryRequestOptions { PartitionKey = new PartitionKey(state.UserId) };
            using var pIter = playersContainer.GetItemQueryIterator<PlayerDocument>(pQuery, requestOptions: pOpts);
            var pPage = await pIter.ReadNextAsync();
            var playerDoc = pPage.FirstOrDefault();

            int attrPointsApplied = 0;
            if (playerDoc != null)
            {
                // Subida fraccionada de atributos
                if (attrGain > 0)
                {
                    playerDoc.AttributeProgress += attrGain;
                    while (playerDoc.AttributeProgress >= 1.0)
                    {
                        playerDoc.AttributeProgress -= 1.0;
                        attrPointsApplied++;
                    }
                    if (attrPointsApplied > 0)
                    {
                        BumpAllAttributes(playerDoc.Physical, attrPointsApplied);
                        BumpAllAttributes(playerDoc.Mental, attrPointsApplied);
                        BumpAllAttributes(playerDoc.Technical, attrPointsApplied);
                    }
                }
                await playersContainer.UpsertItemAsync(playerDoc, new PartitionKey(state.UserId));
            }

            // Actualizar dinero y descansos en el documento de usuario
            var usersContainer = cosmos.GetContainer("TennisManagerDB", "users");
            var userDoc = (await usersContainer.ReadItemAsync<UserDocument>(state.UserId, new PartitionKey(state.UserId))).Resource;
            userDoc.Money += moneyEarned;
            userDoc.Rests += restsEarned;
            await usersContainer.UpsertItemAsync(userDoc, new PartitionKey(state.UserId));

            // Sumar puntos al ranking de la liga
            var leaguesContainer = cosmos.GetContainer("TennisManagerDB", "leagues");
            var league = (await leaguesContainer.ReadItemAsync<LeagueDocument>(state.LeagueId, new PartitionKey(state.LeagueId))).Resource;
            var myStanding = league.Standings.FirstOrDefault(s => s.UserId == state.UserId);
            if (myStanding != null)
            {
                myStanding.Points += pointsEarned;
                // Reordenar la clasificación por puntos y reasignar posiciones
                league.Standings.Sort((a, b) => b.Points.CompareTo(a.Points));
                for (int i = 0; i < league.Standings.Count; i++)
                    league.Standings[i].Position = i + 1;
                await leaguesContainer.UpsertItemAsync(league, new PartitionKey(state.LeagueId));
            }

            // Resumen para el frontend
            return new
            {
                pointsEarned,
                moneyEarned,
                restsEarned,
                attributePointsApplied = attrPointsApplied,
                attributeProgress = playerDoc?.AttributeProgress ?? 0.0,
                isChampion,
            };
        }

        private static void BumpAllAttributes(List<AttributeDoc> attrs, int amount)
        {
            foreach (var a in attrs)
                a.Value = Math.Min(a.Value + amount, 99);
        }
    }
}