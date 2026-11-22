using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public enum ScoreTraceKind { Build, Animal, Resident, Shared }
    public enum ScoreTraceActorKind { Resident, Squirrel, Pigeon, Hedgehog, TrafficVictim }

    public readonly struct ScoreTraceActor
    {
        public readonly ScoreTraceActorKind Kind;
        public readonly string HomeId;
        public readonly int Ordinal;
        public ScoreTraceActor(ScoreTraceActorKind kind, string homeId, int ordinal = 0)
        { Kind = kind; HomeId = homeId; Ordinal = ordinal; }
    }

    public sealed class ScoreTraceEntry
    {
        public ScoreTraceKind Kind { get; }
        public string English { get; }
        public string Chinese { get; }
        public int? Points { get; }
        public IReadOnlyList<string> Tiles { get; }
        public IReadOnlyList<ScoreTraceActor> Actors { get; }
        public ScoreTraceEntry(ScoreTraceKind kind, string english, string chinese, int? points,
            IEnumerable<string> tiles, IEnumerable<ScoreTraceActor> actors = null)
        {
            Kind = kind; English = english; Chinese = chinese; Points = points;
            Tiles = tiles.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToArray();
            Actors = (actors ?? Array.Empty<ScoreTraceActor>()).ToArray();
        }
    }

    // This projection reads settled records; previews and the current idle pose
    // cannot turn an unfed animal or an incomplete routine into a contributor.
    public static class ConstructionScoreTrace
    {
        public static IReadOnlyList<ScoreTraceEntry> For(ConstructionRunModel run)
        {
            var result = new List<ScoreTraceEntry>();
            if (run?.LastScoreDay == null) return result;
            var day = run.LastScoreDay.day;
            var board = run.BuiltTiles.Where(tile => tile.builtDay <= day).ToArray();
            var ids = board.Select(tile => tile.id).ToHashSet();
            foreach(var penalty in run.RecurringPenalties.Where(p=>p.day==day))
                result.Add(new ScoreTraceEntry(ScoreTraceKind.Build,"Recurring bad pairing","持续扣分 · 不良共边",penalty.points,
                    new[]{penalty.firstTileId,penalty.secondTileId}));

            var placed = board.Where(tile => tile.builtDay == day).OrderByDescending(tile => tile.buildIndex).FirstOrDefault();
            if (run.UsesPlacementScore && placed != null)
            {
                var before = board.Where(tile => tile.buildIndex < placed.buildIndex).ToArray();
                var accounted = 0;
                foreach (var neighbours in before.Where(tile => run.PlacementRulesVersion >= 4
                    ? ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row, placed.column, placed.row)
                    : Math.Abs(tile.column - placed.column) <= 1 && Math.Abs(tile.row - placed.row) <= 1)
                    .GroupBy(tile => tile.category))
                {
                    var points = ConstructionPlacementScore.PairPoints(placed.category, neighbours.Key,
                        run.PlacementRulesVersion) * neighbours.Count();
                    if (points == 0) continue;
                    accounted += points;
                    result.Add(new ScoreTraceEntry(ScoreTraceKind.Build,
                        points < 0 ? "Poor pairing" : "Room pairing", points < 0 ? "不良搭配" : "房间搭配", points,
                        neighbours.Select(tile => tile.id).Prepend(placed.id)));
                }
                var recorded = run.Export().placementScore;
                var pointsForPlot = recorded?.entries?.FirstOrDefault(entry => entry.tileId == placed.id)?.points ?? 0;
                if (pointsForPlot != accounted)
                    result.Add(new ScoreTraceEntry(ScoreTraceKind.Build, "Neighbour upgrade", "邻里升级",
                        pointsForPlot - accounted, board.Where(tile => tile.id == placed.id ||
                            run.UsesNeighborUpgrades && ConstructionPlacementScore.NewlyUpgradedTiles(placed, board)
                                .Any(upgraded => upgraded.id == tile.id)).Select(tile => tile.id)));
                foreach (var award in recorded?.communityAwards?.Where(award => award.placementTileId == placed.id)
                    ?? Enumerable.Empty<ConstructionCommunityAwardSaveData>())
                {
                    var home = board.FirstOrDefault(tile => tile.id == award.homeTileId);
                    if (home == null) continue;
                    result.Add(new ScoreTraceEntry(ScoreTraceKind.Build, "Complete neighbourhood", "街坊成形", award.points,
                        board.Where(tile => Math.Abs(tile.column - home.column) <= 1 && Math.Abs(tile.row - home.row) <= 1)
                            .Select(tile => tile.id)));
                }
            }

            var ecology = run.LastEcologyDay;
            if (ecology?.day == day)
            {
                var fedActors = new List<ScoreTraceActor>();
                var sourceActors = new Dictionary<string, List<ScoreTraceActor>>();
                void Meals(List<string> sources, List<string> homes, ScoreTraceActorKind kind, string english, string chinese)
                {
                    var ordinals = new Dictionary<string, int>();
                    for (var index = 0; index < (sources?.Count ?? 0); index++)
                    {
                        var source = sources[index];
                        if (!ids.Contains(source)) continue;
                        var home = index < (homes?.Count ?? 0) ? homes[index] : null;
                        var actors = new List<ScoreTraceActor>();
                        if (!string.IsNullOrEmpty(home) && ids.Contains(home))
                        {
                            ordinals.TryGetValue(home, out var ordinal);
                            var actor = new ScoreTraceActor(kind, home, ordinal);
                            ordinals[home] = ordinal + 1; actors.Add(actor); fedActors.Add(actor);
                        }
                        if (!sourceActors.ContainsKey(source)) sourceActors[source] = new List<ScoreTraceActor>();
                        sourceActors[source].AddRange(actors);
                        result.Add(new ScoreTraceEntry(ScoreTraceKind.Animal, english, chinese,
                            run.HasFedAnimalScore ? run.ScoringVersion >= 12
                                ? ConstructionFoodAreaScorer.BalancedFedAnimalPoints : ConstructionFoodAreaScorer.FedAnimalPoints : (int?)null,
                            new[] { source, home }, actors));
                    }
                }
                Meals(ecology.squirrelFoodTileIds, ecology.squirrelMealHomeTileIds, ScoreTraceActorKind.Squirrel, "Squirrel meal", "松鼠进食");
                Meals(ecology.pigeonMealTileIds, ecology.pigeonMealHomeTileIds, ScoreTraceActorKind.Pigeon, "Pigeon meal", "鸽子进食");
                Meals(ecology.hedgehogMealTileIds, ecology.hedgehogMealHomeTileIds, ScoreTraceActorKind.Hedgehog, "Hedgehog meal", "刺猬进食");
                foreach (var id in ecology.fedScoringSourceTileIds?.Distinct() ?? Enumerable.Empty<string>())
                {
                    var tile = board.FirstOrDefault(tile => tile.id == id);
                    if (tile == null || !sourceActors.ContainsKey(id) || !run.HasFedAnimalScore) continue;
                    var points = ConstructionFoodAreaScorer.PointsForFedSource(tile.category, run.ScoringVersion >= 11, run.ScoringVersion >= 12);
                    if (points != 0) result.Add(new ScoreTraceEntry(ScoreTraceKind.Animal,
                        "Feeding source", "实际供食", points, new[] { id }, sourceActors[id]));
                }
                if (run.LastScoreDay.trafficPenaltyPoints > 0)
                    result.Add(new ScoreTraceEntry(ScoreTraceKind.Animal, "Traffic collision", "街道车祸",
                        -run.LastScoreDay.trafficPenaltyPoints, new[] { ecology.trafficStreetTileId, ecology.trafficWildlifeTileId },
                        new[] { new ScoreTraceActor(ScoreTraceActorKind.TrafficVictim, ecology.trafficWildlifeTileId) }));
            }
            if (run.LastHumanDay?.day == day)
                foreach (var person in run.LastHumanDay.residents.Where(person => person != null && person.satisfactionPoints != 0))
                    result.Add(new ScoreTraceEntry(ScoreTraceKind.Resident, "Resident routine", "居民循环", person.satisfactionPoints,
                        (person.routeTileIds ?? new List<string>()).Concat(new[] { person.homeTileId, person.workTileId,
                            person.mealTileId, person.visitedSquare ? person.squareTileId : null, person.linkageSourceTileId })
                            .Concat(person.biodiversityHabitatTileIds ?? new List<string>()).Where(ids.Contains),
                        new[] { new ScoreTraceActor(ScoreTraceActorKind.Resident, person.homeTileId) }));
            // Shared is a view over the same contributors, not a second award.
            var shared = result.Where(entry => entry.Kind is ScoreTraceKind.Animal or ScoreTraceKind.Resident).ToArray();
            if (shared.Length > 0)
                result.Add(new ScoreTraceEntry(ScoreTraceKind.Shared, "Shared score", "共栖得分", run.LastScoreDay.balancePoints,
                    shared.SelectMany(entry => entry.Tiles), shared.SelectMany(entry => entry.Actors)
                        .GroupBy(actor => (actor.Kind, actor.HomeId, actor.Ordinal)).Select(group => group.First())));
            return result;
        }
    }
}
