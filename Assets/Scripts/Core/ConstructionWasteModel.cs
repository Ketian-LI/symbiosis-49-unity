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

        public ConstructionWasteAvailability(Dictionary<string, int> backlog,
            HashSet<string> blockedHomes, HashSet<string> blockedFoodServices,
            Dictionary<string, int> cleared)
        {
            BacklogAfterCleanup = backlog;
            BlockedHomes = blockedHomes;
            BlockedFoodServices = blockedFoodServices;
            Cleared = cleared;
        }
    }

    // Provisional values: each room clears two portions/day within Manhattan
    // distance two; three uncollected portions disrupt a home or food service.
    public sealed class ConstructionWasteModel
    {
        public const int CleanupCapacity = 2;
        public const int CleanupRadius = 2;
        public const int DisruptionThreshold = 3;

        private readonly Dictionary<string, int> backlog = new();
        public int LastSettledDay { get; private set; }
        public int Backlog(string tileId) => tileId != null &&
            backlog.TryGetValue(tileId, out var amount) ? amount : 0;

        public ConstructionWasteAvailability PreviewAfterCleanup(
            ConstructionBoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var built = board.BuiltTiles;
            var sources = built.Where(tile => tile.category is
                ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket).ToArray();
            var remaining = sources.ToDictionary(tile => tile.id,
                tile => Backlog(tile.id));
            var cleared = sources.ToDictionary(tile => tile.id, _ => 0);
            foreach (var room in built.Where(tile =>
                         tile.category == ConstructionCategory.Waste)
                         .OrderBy(tile => tile.buildIndex))
            {
                for (var portion = 0; portion < CleanupCapacity; portion++)
                {
                    var source = sources.Where(tile => remaining[tile.id] > 0 &&
                            Distance(tile, room) <= CleanupRadius)
                        .OrderByDescending(tile => remaining[tile.id])
                        .ThenBy(tile => Distance(tile, room))
                        .ThenBy(tile => tile.buildIndex)
                        .FirstOrDefault();
                    if (source == null) break;
                    remaining[source.id]--;
                    cleared[source.id]++;
                }
            }
            return new ConstructionWasteAvailability(remaining,
                sources.Where(tile => tile.category == ConstructionCategory.Residence &&
                    remaining[tile.id] >= DisruptionThreshold)
                    .Select(tile => tile.id).ToHashSet(),
                sources.Where(tile => tile.category is
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket &&
                    remaining[tile.id] >= DisruptionThreshold)
                    .Select(tile => tile.id).ToHashSet(), cleared);
        }

        public ConstructionWasteDayResult SettleDay(int day,
            ConstructionBoardModel board, ConstructionHumanDayResult human,
            ConstructionWasteAvailability preview)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (human == null) throw new ArgumentNullException(nameof(human));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (day <= LastSettledDay || human.day != day)
                throw new InvalidOperationException("Waste was already settled for this day.");
            var result = new ConstructionWasteDayResult { day = day };
            foreach (var tile in board.BuiltTiles.Where(tile => tile.category is
                         ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                         ConstructionCategory.Supermarket))
            {
                var produced = tile.category == ConstructionCategory.Residence
                    ? human.residents.Count(person => person.homeTileId == tile.id)
                    : human.residents.Count(person => person.mealTileId == tile.id);
                var cleaned = preview.BacklogAfterCleanup.TryGetValue(tile.id,
                    out var afterCleanup) ? afterCleanup : 0;
                var remaining = cleaned + produced;
                backlog[tile.id] = remaining;
                result.changes.Add(new ConstructionWasteChange
                {
                    tileId = tile.id,
                    cleared = preview.Cleared.TryGetValue(tile.id,
                        out var cleared) ? cleared : 0,
                    produced = produced,
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

        private static int Distance(ConstructionTileData first,
            ConstructionTileData second) =>
            Math.Abs(first.column - second.column) +
            Math.Abs(first.row - second.row);
    }
}
