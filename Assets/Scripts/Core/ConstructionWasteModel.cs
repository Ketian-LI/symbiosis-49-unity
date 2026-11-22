using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionWasteTileSaveData
    {
        public string tileId;
        public int backlog;
    }

    [Serializable]
    public sealed class ConstructionWasteSaveData
    {
        public int lastSettledDay;
        public List<ConstructionWasteTileSaveData> tiles = new();
    }

    [Serializable]
    public sealed class ConstructionWasteChange
    {
        public string tileId;
        public int cleared;
        public int produced;
        public int foraged;
        public int remaining;
    }

    [Serializable]
    public sealed class ConstructionWasteDayResult
    {
        public int day;
        public List<ConstructionWasteChange> changes = new();
        public int Produced => changes?.Sum(change => change.produced) ?? 0;
        public int Cleared => changes?.Sum(change => change.cleared) ?? 0;
        public int Remaining => changes?.Sum(change => change.remaining) ?? 0;
    }

    public sealed class ConstructionWasteAvailability
    {
        public IReadOnlyDictionary<string, int> BacklogAfterCleanup { get; }
        public IReadOnlyCollection<string> BlockedHomes { get; }
        public IReadOnlyCollection<string> BlockedFoodServices { get; }
        public IReadOnlyDictionary<string, int> Cleared { get; }
        public IReadOnlyDictionary<string, int> CollectedByRoom { get; }

        public ConstructionWasteAvailability(Dictionary<string, int> backlog,
            HashSet<string> blockedHomes, HashSet<string> blockedFoodServices,
            Dictionary<string, int> cleared, Dictionary<string, int> collectedByRoom)
        {
            BacklogAfterCleanup = backlog;
            BlockedHomes = blockedHomes;
            BlockedFoodServices = blockedFoodServices;
            Cleared = cleared;
            CollectedByRoom = collectedByRoom;
        }
    }

    // Legacy runs clear two portions/day and close rooms at three portions.
    // Finite hand runs clear every nearby portion without closing rooms.
    public sealed class ConstructionWasteModel
    {
        public const int CleanupCapacity = 2;
        public const int CleanupRadius = 1;
        public const int DisruptionThreshold = 3;

        private readonly Dictionary<string, int> backlog = new();
        public int LastSettledDay { get; private set; }
        public int Backlog(string tileId) => tileId != null &&
            backlog.TryGetValue(tileId, out var amount) ? amount : 0;

        public ConstructionWasteAvailability PreviewAfterCleanup(
            ConstructionBoardModel board, bool clearAllNearby = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var built = board.BuiltTiles;
            var sources = built.Where(tile => tile.category is
                ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket).ToArray();
            var remaining = sources.ToDictionary(tile => tile.id,
                tile => Backlog(tile.id));
            var cleared = sources.ToDictionary(tile => tile.id, _ => 0);
            var collectedByRoom = built.Where(tile =>
                tile.category == ConstructionCategory.Waste)
                .ToDictionary(tile => tile.id, _ => 0);
            foreach (var room in built.Where(tile =>
                         tile.category == ConstructionCategory.Waste)
                         .OrderBy(tile => tile.buildIndex))
            {
                var covered = CoveredSources(room, built, sources);
                if (clearAllNearby)
                {
                    foreach (var source in sources.Where(tile =>
                                 remaining[tile.id] > 0 &&
                                 covered.Contains(tile.id)))
                    {
                        var amount = remaining[source.id];
                        remaining[source.id] = 0;
                        cleared[source.id] += amount;
                        collectedByRoom[room.id] += amount;
                    }
                    continue;
                }
                for (var portion = 0; portion < CleanupCapacity; portion++)
                {
                    var source = sources.Where(tile => remaining[tile.id] > 0 &&
                            covered.Contains(tile.id))
                        .OrderByDescending(tile => remaining[tile.id])
                        .ThenBy(tile => Distance(tile, room))
                        .ThenBy(tile => tile.buildIndex)
                        .FirstOrDefault();
                    if (source == null) break;
                    remaining[source.id]--;
                    cleared[source.id]++;
                    collectedByRoom[room.id]++;
                }
            }
            return new ConstructionWasteAvailability(remaining,
                sources.Where(tile => !clearAllNearby &&
                    tile.category == ConstructionCategory.Residence &&
                    remaining[tile.id] >= DisruptionThreshold)
                    .Select(tile => tile.id).ToHashSet(),
                sources.Where(tile => !clearAllNearby &&
                    (tile.category is ConstructionCategory.Restaurant or
                        ConstructionCategory.Supermarket) &&
                    remaining[tile.id] >= DisruptionThreshold)
                    .Select(tile => tile.id).ToHashSet(), cleared,
                collectedByRoom);
        }

        public ConstructionWasteDayResult SettleDay(int day,
            ConstructionBoardModel board, ConstructionHumanDayResult human,
            ConstructionWasteAvailability preview,
            IReadOnlyDictionary<string, int> supermarketForaged = null,
            bool clearAllNearby = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (human == null) throw new ArgumentNullException(nameof(human));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (day <= LastSettledDay || human.day != day)
                throw new InvalidOperationException("Waste was already settled for this day.");
            var result = new ConstructionWasteDayResult { day = day };
            var built = board.BuiltTiles;
            var wasteRooms = clearAllNearby ? built.Where(tile =>
                tile.category == ConstructionCategory.Waste).ToArray() :
                Array.Empty<ConstructionTileData>();
            var sources = built.Where(tile => tile.category is
                ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket).ToArray();
            var covered = new HashSet<string>();
            foreach (var room in wasteRooms)
                covered.UnionWith(CoveredSources(room, built, sources));
            foreach (var tile in sources)
            {
                var produced = tile.category == ConstructionCategory.Residence
                    ? human.residents.Count(person => person.homeTileId == tile.id)
                    : human.residents.Count(person => person.mealTileId == tile.id);
                var eaten = supermarketForaged != null &&
                    supermarketForaged.TryGetValue(tile.id, out var portions)
                    ? portions : 0;
                if (eaten < 0 || eaten > produced ||
                    eaten > 0 && tile.category != ConstructionCategory.Supermarket)
                    throw new InvalidOperationException("Invalid urban forage amount.");
                var cleaned = preview.BacklogAfterCleanup.TryGetValue(tile.id,
                    out var afterCleanup) ? afterCleanup : 0;
                var remaining = cleaned + produced - eaten;
                var newlyCleared = clearAllNearby && covered.Contains(tile.id)
                    ? remaining : 0;
                remaining -= newlyCleared;
                backlog[tile.id] = remaining;
                result.changes.Add(new ConstructionWasteChange
                {
                    tileId = tile.id,
                    cleared = preview.Cleared.TryGetValue(tile.id,
                        out var cleared) ? cleared + newlyCleared : newlyCleared,
                    produced = produced,
                    foraged = eaten,
                    remaining = remaining
                });
            }
            LastSettledDay = day;
            return result;
        }

        public void MarkDayWithoutSimulation(int day)
        {
            if (day > LastSettledDay) LastSettledDay = day;
        }

        public ConstructionWasteSaveData Export() => new()
        {
            lastSettledDay = LastSettledDay,
            tiles = backlog.OrderBy(pair => pair.Key).Select(pair =>
                new ConstructionWasteTileSaveData
                {
                    tileId = pair.Key,
                    backlog = pair.Value
                }).ToList()
        };

        public static bool TryRestore(ConstructionWasteSaveData saved,
            ConstructionBoardModel board, int currentDay, bool finished,
            out ConstructionWasteModel waste)
        {
            waste = null;
            if (saved?.tiles == null || board == null ||
                saved.lastSettledDay != currentDay - (finished ? 0 : 1))
                return false;
            var sources = board.BuiltTiles.Where(tile => tile.category is
                ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket).ToDictionary(tile => tile.id);
            var candidate = new ConstructionWasteModel();
            foreach (var item in saved.tiles)
            {
                if (item == null || string.IsNullOrEmpty(item.tileId) ||
                    !sources.TryGetValue(item.tileId, out var source) ||
                    source.builtDay > currentDay || item.backlog < 0 ||
                    !candidate.backlog.TryAdd(item.tileId, item.backlog))
                    return false;
            }
            candidate.LastSettledDay = saved.lastSettledDay;
            waste = candidate;
            return true;
        }

        public static ConstructionWasteModel FromLegacySave(int currentDay,
            bool finished)
        {
            var waste = new ConstructionWasteModel();
            waste.LastSettledDay = currentDay - (finished ? 0 : 1);
            return waste;
        }

        private static long Distance(ConstructionTileData first,
            ConstructionTileData second) =>
            ConstructionBoardModel.GridDistance(first.column, first.row,
                second.column, second.row);

        // A waste room serves its surrounding eight plots and any source
        // reached through a continuous shared-edge chain of street plots.
        private static HashSet<string> CoveredSources(ConstructionTileData room,
            IReadOnlyList<ConstructionTileData> built,
            IReadOnlyList<ConstructionTileData> sources)
        {
            var plots = CoveragePlots(room, built);
            return sources.Where(source => plots.Contains((source.column, source.row)))
                .Select(source => source.id).ToHashSet();
        }

        // Shared by cleanup and the selection overlay so the displayed range
        // cannot drift from the simulation. Empty plots are included as range,
        // not as claims that waste was produced or removed there.
        public static HashSet<(int column, int row)> CoveragePlots(ConstructionTileData room,
            IReadOnlyList<ConstructionTileData> built)
        {
            var covered = new HashSet<(int column, int row)>();
            if (room == null || room.category != ConstructionCategory.Waste || built == null) return covered;
            for (var y = -CleanupRadius; y <= CleanupRadius; y++)
            for (var x = -CleanupRadius; x <= CleanupRadius; x++)
                covered.Add((room.column + x, room.row + y));
            var streets = built.Where(tile =>
                tile.category == ConstructionCategory.Street).ToArray();
            var visited = new HashSet<string>();
            var pending = new Queue<ConstructionTileData>();
            foreach (var street in streets.Where(tile =>
                         SharesEdge(room, tile)))
                if (visited.Add(street.id)) pending.Enqueue(street);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                covered.Add((current.column, current.row));
                covered.Add((current.column - 1, current.row));
                covered.Add((current.column + 1, current.row));
                covered.Add((current.column, current.row - 1));
                covered.Add((current.column, current.row + 1));
                foreach (var street in streets.Where(tile =>
                             SharesEdge(current, tile)))
                    if (visited.Add(street.id)) pending.Enqueue(street);
            }
            return covered;
        }

        private static bool SharesEdge(ConstructionTileData first,
            ConstructionTileData second) =>
            ConstructionBoardModel.AreCardinalNeighbours(first.column,
                first.row, second.column, second.row);
    }
}
