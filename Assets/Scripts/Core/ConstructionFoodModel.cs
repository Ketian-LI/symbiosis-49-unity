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
        public int squirrelsPresent;
        public int squirrelsFed;
        public List<string> squirrelFoodTileIds = new();
        public int pigeonsFed;
        public string pigeonFoodTileId;
        public int pigeonsPresent;
        public List<string> pigeonFoodTileIds = new();
        public ConstructionPreySpecies predatedSpecies;
        public string predationTileId;
        public List<string> foxHuntRouteTileIds = new();
        public List<ConstructionFoodChange> foodChanges = new();
        // Older saves have only the single-squirrel boolean.
        public int AnimalMeals => Math.Max(squirrelsFed, squirrelAte ? 1 : 0) +
            pigeonsFed + (predatedSpecies == ConstructionPreySpecies.None ? 0 : 1);
    }

    public enum ConstructionPreySpecies { None, Pigeon, Squirrel, Hedgehog }

    public sealed class ConstructionPredationPlan
    {
        public ConstructionPreySpecies species;
        public string preyTileId;
        public List<string> routeTileIds = new();
        public bool WillHunt => species != ConstructionPreySpecies.None;
    }

    // Small deterministic ecology for the new board only. One portion feeds
    // one animal for one day; a flock occupies one tile and forages together.
    public sealed class ConstructionFoodModel
    {
        public const int PigeonFlockSize = 2;

        // Temporary balance rule: two additional mature habitat tiles attract
        // one more animal. Squirrels remain within one cell of the starter oak.
        public static int MatureNearbyOakCount(ConstructionBoardModel board, int day)
        {
            return board.ConnectedGreenTiles(
                    ConstructionBoardModel.StarterOakId)
                .Count(tile => tile.planting == GreenPlanting.Oak &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2 &&
                    Math.Abs(tile.column - ConstructionBoardModel.StarterColumn) +
                    Math.Abs(tile.row - ConstructionBoardModel.StarterRow) <= 1);
        }

        public static int MatureConnectedMeadowCount(ConstructionBoardModel board,
            int day, string pigeonTileId)
        {
            if (string.IsNullOrEmpty(pigeonTileId)) return 0;
            return board.ConnectedGreenTiles(pigeonTileId)
                .Count(tile => tile.planting == GreenPlanting.Meadow &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2);
        }

        public static int SquirrelPopulation(ConstructionBoardModel board, int day)
            => 1 + Math.Max(0, MatureNearbyOakCount(board, day) - 1) / 2;

        public static int OaksForNextSquirrel(int currentPopulation)
            => 1 + currentPopulation * 2;

        public static int PigeonPopulation(ConstructionBoardModel board,
            int day, string pigeonTileId)
            => string.IsNullOrEmpty(pigeonTileId) ? 0 : PigeonFlockSize +
                Math.Max(0, MatureConnectedMeadowCount(board, day,
                    pigeonTileId) - 1) / 2;

        public static int MeadowsForNextPigeon(int currentPopulation)
            => 3 + (currentPopulation - PigeonFlockSize) * 2;

        // Hedgehogs currently visit suitable habitat. Foxes may take one
        // reachable, unprotected prey when their hunger state permits it.
        public static int MatureConnectedShrubCount(
            ConstructionBoardModel board, int day) =>
            MatureConnectedShrubs(board, day).Length;

        public static ConstructionTileData HedgehogHabitat(
            ConstructionBoardModel board, int day) =>
            MatureConnectedShrubs(board, day).FirstOrDefault();

        public static ConstructionTileData FoxHabitat(
            ConstructionBoardModel board, int day, int pigeonPopulation)
        {
            var shrubs = MatureConnectedShrubs(board, day);
            return shrubs.Length >= 2 && pigeonPopulation >= 3
                ? shrubs[shrubs.Length - 1] : null;
        }

        private static ConstructionTileData[] MatureConnectedShrubs(
            ConstructionBoardModel board, int day) =>
            board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Where(tile => tile.planting == GreenPlanting.Shrub &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2)
                .OrderBy(tile => tile.buildIndex).ToArray();

        // Daily abstraction of the old fox's prey order. A route may cross at
        // most three green thresholds; denser same-species planting provides
        // cover in place of the old per-frame flight/curl state.
        public static ConstructionPredationPlan PlanFoxHunt(
            ConstructionBoardModel board, int day, string foxTileId,
            string pigeonTileId, int pigeons, int squirrels,
            string hedgehogTileId, bool foxHungry)
        {
            var noHunt = new ConstructionPredationPlan();
            if (!foxHungry || string.IsNullOrEmpty(foxTileId)) return noHunt;
            foreach (var candidate in new[]
            {
                (species: ConstructionPreySpecies.Pigeon,
                    tileId: pigeons > 0 ? pigeonTileId : null,
                    planting: GreenPlanting.Meadow, coverNeeded: 2),
                (species: ConstructionPreySpecies.Squirrel,
                    tileId: squirrels > 0 ? ConstructionBoardModel.StarterOakId : null,
                    planting: GreenPlanting.Oak, coverNeeded: 1),
                (species: ConstructionPreySpecies.Hedgehog,
                    tileId: hedgehogTileId,
                    planting: GreenPlanting.Shrub, coverNeeded: 2)
            })
            {
                if (string.IsNullOrEmpty(candidate.tileId)) continue;
                var route = board.GreenRouteBetween(foxTileId, candidate.tileId);
                if (route.Count == 0 || route.Count > 4) continue;
                var prey = board.BuiltTiles.FirstOrDefault(tile =>
                    tile.id == candidate.tileId);
                if (prey == null || AdjacentMaturePlantings(board, prey,
                        candidate.planting, day) >= candidate.coverNeeded)
                    continue;
                return new ConstructionPredationPlan
                {
                    species = candidate.species,
                    preyTileId = candidate.tileId,
                    routeTileIds = route.Select(tile => tile.id).ToList()
                };
            }
            return noHunt;
        }

        private static int AdjacentMaturePlantings(
            ConstructionBoardModel board, ConstructionTileData centre,
            GreenPlanting planting, int day)
        {
            var count = 0;
            foreach (var offset in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var tile = board.At(centre.column + offset.Item1,
                    centre.row + offset.Item2);
                if (tile?.planting == planting && MaturityStage(tile, day) == 2)
                    count++;
            }
            return count;
        }

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

        // Preview the same yield, target selection and portion allocation as
        // settlement on a copy. Inspecting a route must never eat food.
        public ConstructionEcologyDayResult PreviewDay(int day,
            ConstructionBoardModel board, string pigeonTileId,
            int? squirrelsPresent = null, int? pigeonsPresent = null)
        {
            var copy = new ConstructionFoodModel();
            copy.stock.Clear();
            foreach (var pair in stock) copy.stock[pair.Key] = pair.Value;
            copy.LastSettledDay = LastSettledDay;
            return copy.SettleDay(day, board, pigeonTileId,
                squirrelsPresent, pigeonsPresent);
        }

        // Used solely by the old externally-scored test/API path. It must not
        // silently produce food or award meals.
        public void MarkDayWithoutSimulation(int day)
        {
            if (day > LastSettledDay) LastSettledDay = day;
        }

        public ConstructionEcologyDayResult SettleDay(int day,
            ConstructionBoardModel board, string pigeonTileId,
            int? squirrelsPresent = null, int? pigeonsPresent = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (day <= LastSettledDay) throw new InvalidOperationException(
                "Food was already settled for this day.");

            var result = new ConstructionEcologyDayResult { day = day };
            result.squirrelsPresent = squirrelsPresent ?? SquirrelPopulation(board, day);
            result.pigeonsPresent = pigeonsPresent ??
                PigeonPopulation(board, day, pigeonTileId);
            if (result.squirrelsPresent < 0 || result.pigeonsPresent < 0)
                throw new ArgumentOutOfRangeException(nameof(squirrelsPresent));
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
            var squirrelSources = greens.Where(tile =>
                tile.planting == GreenPlanting.Oak && Stock(tile.id) > 0 &&
                Math.Abs(tile.column - ConstructionBoardModel.StarterColumn) +
                Math.Abs(tile.row - ConstructionBoardModel.StarterRow) <= 1 &&
                board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                    tile.id).Count > 0);
            foreach (var squirrelSource in squirrelSources)
            {
                if (result.squirrelsFed >= result.squirrelsPresent) break;
                Eat(result, squirrelSource.id, 1);
                result.squirrelAte = true;
                result.squirrelsFed++;
                result.squirrelFoodTileIds.Add(squirrelSource.id);
                result.squirrelFoodTileId ??= squirrelSource.id;
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
                        .ThenBy(tile => tile.buildIndex);
                    foreach (var source in reachable)
                    {
                        var needed = result.pigeonsPresent - result.pigeonsFed;
                        if (needed <= 0) break;
                        var portions = Math.Min(needed, Stock(source.id));
                        Eat(result, source.id, portions);
                        result.pigeonsFed += portions;
                        result.pigeonFoodTileIds.Add(source.id);
                        result.pigeonFoodTileId ??= source.id;
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
