using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionFoodTileSaveData
    {
        public string tileId;
        public int stock;
    }

    [Serializable]
    public sealed class ConstructionFoodSaveData
    {
        public int lastSettledDay;
        public List<ConstructionFoodTileSaveData> tiles = new();
    }

    [Serializable]
    public sealed class ConstructionFoodChange
    {
        public string tileId;
        public int produced;
        public int eaten;
        public int remaining;
    }

    [Serializable]
    public sealed class ConstructionEcologyDayResult
    {
        public int day;
        public bool squirrelAte;
        public string squirrelFoodTileId;
        public int pigeonsFed;
        public string pigeonFoodTileId;
        public List<ConstructionFoodChange> foodChanges = new();
        public int AnimalMeals => (squirrelAte ? 1 : 0) + pigeonsFed;
    }

    // Small deterministic ecology for the new board only. One portion feeds
    // one animal for one day; a flock occupies one tile and forages together.
    public sealed class ConstructionFoodModel
    {
        public const int PigeonFlockSize = 2;

        private readonly Dictionary<string, int> stock = new();
        public int LastSettledDay { get; private set; }

        public ConstructionFoodModel()
        {
            // The opening oak is mature and has one visible nut at the start.
            stock[ConstructionBoardModel.StarterOakId] = 1;
        }

        public static int Capacity(GreenPlanting planting) => planting switch
        {
            GreenPlanting.Oak => 3,
            GreenPlanting.Meadow => 2,
            GreenPlanting.Shrub => 2,
            _ => 0
        };

        public static int MaturityStage(ConstructionTileData tile, int day)
        {
            if (tile == null || tile.category != ConstructionCategory.Green) return 0;
            if (tile.id == ConstructionBoardModel.StarterOakId) return 2;
            return Math.Min(2, Math.Max(0, day - Math.Max(1, tile.builtDay)));
        }

        // Provisional tuning: a meadow seeds every other mature day. A second
        // meadow with a different planting day can cover its recovery day.
        public static int DailyYield(ConstructionTileData tile, int day)
        {
            if (tile == null) return 0;
            var stage = MaturityStage(tile, day);
            return tile.planting switch
            {
                GreenPlanting.Oak when stage == 2 => 1,
                GreenPlanting.Meadow when stage >= 1 &&
                    (day - tile.builtDay) % 2 == 1 => 2,
                GreenPlanting.Shrub when stage == 2 => 1,
                _ => 0
            };
        }

        public int Stock(string tileId) => tileId != null &&
            stock.TryGetValue(tileId, out var amount) ? amount : 0;

        // Used solely by the old externally-scored test/API path. It must not
        // silently produce food or award meals.
        public void MarkDayWithoutSimulation(int day)
        {
            if (day > LastSettledDay) LastSettledDay = day;
        }

        public ConstructionEcologyDayResult SettleDay(int day,
            ConstructionBoardModel board, string pigeonTileId)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (day <= LastSettledDay) throw new InvalidOperationException(
                "Food was already settled for this day.");

            var result = new ConstructionEcologyDayResult { day = day };
            var greens = board.BuiltTiles.Where(tile =>
                tile.category == ConstructionCategory.Green && tile.builtDay <= day)
                .OrderBy(tile => tile.buildIndex).ToArray();
            foreach (var green in greens)
            {
                var current = Stock(green.id);
                var produced = green.id == ConstructionBoardModel.StarterOakId && day == 1
                    ? 0 : Math.Min(DailyYield(green, day),
                        Capacity(green.planting) - current);
                stock[green.id] = current + produced;
                result.foodChanges.Add(new ConstructionFoodChange
                {
                    tileId = green.id,
                    produced = produced
                });
            }

            // The squirrel is faithful to the central oak and can reach an
            // adjacent connected oak; it cannot teleport across the park.
            var squirrelSource = greens.FirstOrDefault(tile =>
                tile.planting == GreenPlanting.Oak && Stock(tile.id) > 0 &&
                Math.Abs(tile.column - ConstructionBoardModel.StarterColumn) +
                Math.Abs(tile.row - ConstructionBoardModel.StarterRow) <= 1);
            if (squirrelSource != null)
            {
                Eat(result, squirrelSource.id, 1);
                result.squirrelAte = true;
                result.squirrelFoodTileId = squirrelSource.id;
            }

            if (!string.IsNullOrEmpty(pigeonTileId))
            {
                var origin = board.BuiltTiles.FirstOrDefault(tile =>
                    tile.id == pigeonTileId &&
                    tile.category == ConstructionCategory.Green);
                if (origin != null)
                {
                    var reachable = board.ConnectedGreenTiles(pigeonTileId)
                        .Where(tile => tile.planting == GreenPlanting.Meadow &&
                            tile.builtDay <= day && Stock(tile.id) > 0)
                        .OrderBy(tile => tile.id == pigeonTileId ? 0 : 1)
                        .ThenBy(tile => Math.Abs(tile.column - origin.column) +
                            Math.Abs(tile.row - origin.row))
                        .ThenBy(tile => tile.buildIndex)
                        .FirstOrDefault();
                    if (reachable != null)
                    {
                        result.pigeonFoodTileId = reachable.id;
                        result.pigeonsFed = Math.Min(PigeonFlockSize, Stock(reachable.id));
                        Eat(result, reachable.id, result.pigeonsFed);
                    }
                }
            }

            foreach (var change in result.foodChanges)
                change.remaining = Stock(change.tileId);
            LastSettledDay = day;
            return result;
        }

        public ConstructionFoodSaveData Export() => new()
        {
            lastSettledDay = LastSettledDay,
            tiles = stock.OrderBy(pair => pair.Key).Select(pair =>
                new ConstructionFoodTileSaveData
                {
                    tileId = pair.Key,
                    stock = pair.Value
                }).ToList()
        };

        public static bool TryRestore(ConstructionFoodSaveData saved,
            ConstructionBoardModel board, int currentDay, bool finished,
            out ConstructionFoodModel food)
        {
            food = null;
            if (saved?.tiles == null || board == null ||
                saved.lastSettledDay < 0 ||
                saved.lastSettledDay > currentDay ||
                saved.lastSettledDay != currentDay - (finished ? 0 : 1))
                return false;
            var greenById = board.BuiltTiles.Where(tile =>
                tile.category == ConstructionCategory.Green)
                .ToDictionary(tile => tile.id);
            if (!saved.tiles.Any(tile => tile?.tileId ==
                    ConstructionBoardModel.StarterOakId) ||
                saved.tiles.Count != saved.tiles.Where(tile => tile != null)
                    .Select(tile => tile.tileId).Distinct().Count()) return false;
            var candidate = new ConstructionFoodModel();
            candidate.stock.Clear();
            foreach (var item in saved.tiles)
            {
                if (item == null || string.IsNullOrEmpty(item.tileId) ||
                    !greenById.TryGetValue(item.tileId,
                        out var tile) || item.stock < 0 ||
                    item.stock > Capacity(tile.planting) ||
                    tile.builtDay > currentDay) return false;
                candidate.stock[item.tileId] = item.stock;
            }
            candidate.LastSettledDay = saved.lastSettledDay;
            food = candidate;
            return true;
        }

        // Existing v1 prototype saves did not contain food stocks. Keep the
        // board and score, but begin the ecological inventory fresh.
        public static ConstructionFoodModel FromLegacySave(int currentDay,
            bool finished)
        {
            var food = new ConstructionFoodModel();
            food.LastSettledDay = currentDay - (finished ? 0 : 1);
            return food;
        }

        private void Eat(ConstructionEcologyDayResult result, string tileId, int portions)
        {
            stock[tileId] -= portions;
            result.foodChanges.First(change => change.tileId == tileId).eaten += portions;
        }
    }
}
