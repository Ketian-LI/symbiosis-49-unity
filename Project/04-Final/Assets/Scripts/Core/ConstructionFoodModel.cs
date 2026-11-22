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

    // Transient daily scraps, never a permanent food generator just because a
    // supermarket or waste room exists on the board.
    public sealed class ConstructionUrbanFoodSource
    {
        public string tileId;
        public ConstructionCategory category;
        public int portions;
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
        // V7: one home per squirrelFoodTileIds entry. An unfed animal leaves
        // no entry, so view routes must not infer homes from meal-list indices.
        public List<string> squirrelMealHomeTileIds = new();
        public List<string> pigeonMealHomeTileIds = new();
        public int pigeonsFed;
        public string pigeonFoodTileId;
        // V8: where the flock lived before this day settled. A food visit may
        // move its home later, so a saved day's neighbourhood must use this ID.
        public string pigeonHabitatTileId;
        public int pigeonsPresent;
        public List<string> pigeonFoodTileIds = new();
        // One source per fed pigeon; pigeonFoodTileIds remains the distinct
        // source list used by the food-location UI and older saves.
        public List<string> pigeonMealTileIds = new();
        public bool hedgehogPresent;
        public bool hedgehogFed;
        public List<string> hedgehogHomeTileIds = new();
        public List<string> hedgehogMealHomeTileIds = new();
        public List<string> hedgehogMealTileIds = new();
        public string hedgehogFoodTileId;
        public string hedgehogHabitatTileId;
        public List<string> urbanMealTileIds = new();
        public int animalPoints;
        // V7 available food areas. V10 retains these for resident linkage,
        // while its animal score uses the observed meals below.
        public List<string> scoredFoodTileIds = new();
        // V10 sources that actually fed wildlife and earned a source bonus.
        public List<string> fedScoringSourceTileIds = new();
        public ConstructionPreySpecies predatedSpecies;
        public string predationTileId;
        public List<string> foxHuntRouteTileIds = new();
        public ConstructionTrafficSpecies trafficSpecies;
        public string trafficStreetTileId;
        public string trafficWildlifeTileId;
        public List<ConstructionFoodChange> foodChanges = new();
        // Older saves have only the single-squirrel boolean.
        public int AnimalMeals => Math.Max(squirrelsFed, squirrelAte ? 1 : 0) +
            pigeonsFed + Math.Max(hedgehogMealHomeTileIds?.Count ?? 0,
                hedgehogFed ? 1 : 0);
        public int LegacyAnimalMeals => AnimalMeals +
            (predatedSpecies == ConstructionPreySpecies.None ? 0 : 1);
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
        // one more animal. The starter oak's full eight-cell ring is nearby.
        public static int MatureNearbyOakCount(ConstructionBoardModel board,
            int day, bool useLegacyEightNeighbours = false)
        {
            return board.ConnectedGreenTiles(
                    ConstructionBoardModel.StarterOakId,
                    useLegacyEightNeighbours)
                .Count(tile => tile.planting == GreenPlanting.Oak &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2 &&
                    ConstructionBoardModel.GridDistance(tile.column, tile.row,
                        ConstructionBoardModel.StarterColumn,
                        ConstructionBoardModel.StarterRow) <= 1);
        }

        public static int MatureConnectedMeadowCount(ConstructionBoardModel board,
            int day, string pigeonTileId,
            bool useLegacyEightNeighbours = false)
        {
            if (string.IsNullOrEmpty(pigeonTileId)) return 0;
            return board.ConnectedGreenTiles(pigeonTileId,
                    useLegacyEightNeighbours)
                .Count(tile => tile.planting == GreenPlanting.Meadow &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2);
        }

        public static int SquirrelPopulation(ConstructionBoardModel board,
            int day, bool useLegacyEightNeighbours = false)
        {
            // Digital Fill starts with an empty board until the player places
            // the opening central oak. No squirrel exists before that card.
            if (board.BuiltCount == 0) return 0;
            return 1 + Math.Max(0, MatureNearbyOakCount(board, day,
                useLegacyEightNeighbours) - 1) / 2;
        }

        // The occupied oak rooms used by the world view and V7 local food
        // scoring. More than one squirrel may share a room; each room remains
        // a single origin for nearby food.
        public static IReadOnlyList<ConstructionTileData> SquirrelHomeTiles(
            ConstructionBoardModel board, int day, int population,
            bool usePatchWildlife = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (population <= 0) return Array.Empty<ConstructionTileData>();
            if (usePatchWildlife)
                return ConstructionGreenPatchModel.SquirrelHomes(board, day,
                    population);
            var oaks = board.BuiltTiles.Where(tile =>
                    tile.category == ConstructionCategory.Green &&
                    tile.planting == GreenPlanting.Oak &&
                    tile.builtDay <= day && MaturityStage(tile, day) == 2 &&
                    ConstructionBoardModel.GridDistance(tile.column, tile.row,
                        ConstructionBoardModel.StarterColumn,
                        ConstructionBoardModel.StarterRow) <= 1 &&
                    board.GreenRouteBetween(
                        ConstructionBoardModel.StarterOakId,
                        tile.id).Count > 0)
                .OrderBy(tile => tile.id ==
                    ConstructionBoardModel.StarterOakId ? 0 : 1)
                .ThenBy(tile => tile.buildIndex).ToArray();
            if (oaks.Length == 0) return Array.Empty<ConstructionTileData>();
            return Enumerable.Range(0, Math.Min(population, oaks.Length))
                .Select(index => oaks[index]).ToArray();
        }

        public static int OaksForNextSquirrel(int currentPopulation)
            => 1 + currentPopulation * 2;

        // The central oak is the squirrels' home. Its eight neighbours can
        // become additional nearby oaks; an occupied non-oak
        // plot cannot be replaced later in the same run.
        public static int MaximumNearbyOakCount(ConstructionBoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var count = 1;
            foreach (var (column, row) in new[]
                     {
                         (ConstructionBoardModel.StarterColumn - 1,
                             ConstructionBoardModel.StarterRow),
                         (ConstructionBoardModel.StarterColumn + 1,
                             ConstructionBoardModel.StarterRow),
                         (ConstructionBoardModel.StarterColumn,
                             ConstructionBoardModel.StarterRow - 1),
                         (ConstructionBoardModel.StarterColumn,
                             ConstructionBoardModel.StarterRow + 1),
                         (ConstructionBoardModel.StarterColumn - 1,
                             ConstructionBoardModel.StarterRow - 1),
                         (ConstructionBoardModel.StarterColumn - 1,
                             ConstructionBoardModel.StarterRow + 1),
                         (ConstructionBoardModel.StarterColumn + 1,
                             ConstructionBoardModel.StarterRow - 1),
                         (ConstructionBoardModel.StarterColumn + 1,
                             ConstructionBoardModel.StarterRow + 1)
                     })
            {
                var tile = board.At(column, row);
                if (tile == null || tile.category == ConstructionCategory.Green &&
                    tile.planting == GreenPlanting.Oak) count++;
            }
            return count;
        }

        public static int PigeonPopulation(ConstructionBoardModel board,
            int day, string pigeonTileId,
            bool useLegacyEightNeighbours = false)
            => string.IsNullOrEmpty(pigeonTileId) ? 0 : PigeonFlockSize +
                Math.Max(0, MatureConnectedMeadowCount(board, day,
                    pigeonTileId, useLegacyEightNeighbours) - 1) / 2;

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
            string hedgehogTileId, bool foxHungry,
            bool usePatchWildlife = false,
            IReadOnlyList<string> patchHedgehogTileIds = null)
        {
            var noHunt = new ConstructionPredationPlan();
            if (!foxHungry || string.IsNullOrEmpty(foxTileId)) return noHunt;
            var candidates = new List<(ConstructionPreySpecies species,
                string tileId, GreenPlanting planting, int coverNeeded)>();
            if (usePatchWildlife)
            {
                candidates.AddRange(ConstructionGreenPatchModel.PigeonGroups(
                    board, day, pigeons).Select(group =>
                    (ConstructionPreySpecies.Pigeon, group.Origin.id,
                        GreenPlanting.Meadow, 2)));
                candidates.AddRange(ConstructionGreenPatchModel.SquirrelHomes(
                    board, day, squirrels).Select(home =>
                    (ConstructionPreySpecies.Squirrel, home.id,
                        GreenPlanting.Oak, 1)));
            }
            else
            {
                candidates.Add((ConstructionPreySpecies.Pigeon,
                    pigeons > 0 ? pigeonTileId : null,
                    GreenPlanting.Meadow, 2));
                candidates.Add((ConstructionPreySpecies.Squirrel,
                    squirrels > 0 ? ConstructionBoardModel.StarterOakId : null,
                    GreenPlanting.Oak, 1));
            }
            if (usePatchWildlife && patchHedgehogTileIds != null)
                candidates.AddRange(patchHedgehogTileIds.Select(id =>
                    (ConstructionPreySpecies.Hedgehog, id,
                        GreenPlanting.Shrub, 2)));
            else
                candidates.Add((ConstructionPreySpecies.Hedgehog,
                    hedgehogTileId, GreenPlanting.Shrub, 2));
            foreach (var candidate in candidates)
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
            foreach (var offset in new[]
                     {
                         (1, 0), (-1, 0), (0, 1), (0, -1),
                         (1, 1), (1, -1), (-1, 1), (-1, -1)
                     })
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

        public static int Capacity(ConstructionBoardModel board,
            ConstructionTileData tile, bool useGreenUpgrade)
        {
            if (tile == null || tile.category != ConstructionCategory.Green)
                return 0;
            return Capacity(tile.planting) + (useGreenUpgrade
                ? board.GreenUpgradeLevel(tile) - 1
                : 0);
        }

        public static int MaturityStage(ConstructionTileData tile, int day)
        {
            if (tile == null || tile.category != ConstructionCategory.Green) return 0;
            if (tile.id == ConstructionBoardModel.StarterOakId) return 2;
            return Math.Min(2, Math.Max(0, day - Math.Max(1, tile.builtDay)));
        }

        // Provisional tuning: a meadow seeds every other mature day. A second
        // meadow with a different planting day can cover its recovery day.
        public static int DailyYield(ConstructionTileData tile, int day,
            bool useV6YieldRule = false, string runSeed = null,
            int greenUpgradeLevel = 1)
        {
            if (tile == null) return 0;
            var stage = MaturityStage(tile, day);
            var normalYield = tile.planting switch
            {
                GreenPlanting.Oak when stage == 2 => 1,
                GreenPlanting.Meadow when stage >= 1 &&
                    (day - tile.builtDay) % 2 == 1 => 2,
                GreenPlanting.Shrub when stage == 2 => 1,
                _ => 0
            };
            // An upgraded mature habitat produces more on its existing
            // harvest days. Meadows still rest on alternate days.
            if (stage == 2 && normalYield > 0)
                normalYield += Math.Max(0, Math.Min(3, greenUpgradeLevel) - 1);
            // V6: young plots and days 1-9 keep the established tutorial yield.
            // From day 10, each mature plot has an 80% chance to produce its
            // entire normal yield. The persisted run id makes this vary between
            // games while remaining identical in previews and after reloads.
            if (!useV6YieldRule || day < 10 || stage != 2 || normalYield == 0)
                return normalYield;
            return V6HasHarvest(runSeed, tile.id, day) ? normalYield : 0;
        }

        private static bool V6HasHarvest(string runSeed, string tileId, int day)
        {
            // Do not use string.GetHashCode or UnityEngine.Random: their output
            // is unsuitable for replaying a saved day's preview and settlement.
            unchecked
            {
                const uint prime = 16777619;
                uint hash = 2166136261;
                foreach (var value in runSeed ?? string.Empty)
                {
                    hash = (hash ^ (byte)value) * prime;
                    hash = (hash ^ (uint)(value >> 8)) * prime;
                }
                hash = (hash ^ 255u) * prime;
                foreach (var value in tileId ?? string.Empty)
                {
                    hash = (hash ^ (byte)value) * prime;
                    hash = (hash ^ (uint)(value >> 8)) * prime;
                }
                hash = (hash ^ 255u) * prime;
                for (var shift = 0; shift < 32; shift += 8)
                    hash = (hash ^ (uint)((day >> shift) & 255)) * prime;
                // Mix the high and low bits before reducing to five buckets.
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return hash % 5 != 0;
            }
        }

        public int Stock(string tileId) => tileId != null &&
            stock.TryGetValue(tileId, out var amount) ? amount : 0;

        // Preview the same yield, target selection and portion allocation as
        // settlement on a copy. Inspecting a route must never eat food.
        public ConstructionEcologyDayResult PreviewDay(int day,
            ConstructionBoardModel board, string pigeonTileId,
            int? squirrelsPresent = null, int? pigeonsPresent = null,
            string hedgehogTileId = null,
            IReadOnlyList<ConstructionUrbanFoodSource> urbanSources = null,
            bool useV6YieldRule = false, string runSeed = null,
            bool useLocalFoodAreaScore = false,
            bool useGreenUpgrade = false,
            bool usePatchWildlife = false,
            IReadOnlyList<string> patchHedgehogTileIds = null,
            bool useFedAnimalScore = false)
        {
            var copy = new ConstructionFoodModel();
            copy.stock.Clear();
            foreach (var pair in stock) copy.stock[pair.Key] = pair.Value;
            copy.LastSettledDay = LastSettledDay;
            return copy.SettleDay(day, board, pigeonTileId,
                squirrelsPresent, pigeonsPresent, hedgehogTileId, urbanSources,
                useV6YieldRule, runSeed, useLocalFoodAreaScore,
                useGreenUpgrade, usePatchWildlife,
                patchHedgehogTileIds, useFedAnimalScore);
        }

        // Used solely by the old externally-scored test/API path. It must not
        // silently produce food or award meals.
        public void MarkDayWithoutSimulation(int day)
        {
            if (day > LastSettledDay) LastSettledDay = day;
        }

        public ConstructionEcologyDayResult SettleDay(int day,
            ConstructionBoardModel board, string pigeonTileId,
            int? squirrelsPresent = null, int? pigeonsPresent = null,
            string hedgehogTileId = null,
            IReadOnlyList<ConstructionUrbanFoodSource> urbanSources = null,
            bool useV6YieldRule = false, string runSeed = null,
            bool useLocalFoodAreaScore = false,
            bool useGreenUpgrade = false,
            bool usePatchWildlife = false,
            IReadOnlyList<string> patchHedgehogTileIds = null,
            bool useFedAnimalScore = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (day <= LastSettledDay) throw new InvalidOperationException(
                "Food was already settled for this day.");

            var result = new ConstructionEcologyDayResult { day = day };
            result.squirrelsPresent = squirrelsPresent ?? SquirrelPopulation(board, day);
            result.pigeonsPresent = pigeonsPresent ??
                PigeonPopulation(board, day, pigeonTileId);
            if (usePatchWildlife && patchHedgehogTileIds != null)
                result.hedgehogHomeTileIds.AddRange(patchHedgehogTileIds);
            else if (!string.IsNullOrEmpty(hedgehogTileId))
                result.hedgehogHomeTileIds.Add(hedgehogTileId);
            result.hedgehogPresent = result.hedgehogHomeTileIds.Count > 0;
            if (result.squirrelsPresent < 0 || result.pigeonsPresent < 0)
                throw new ArgumentOutOfRangeException(nameof(squirrelsPresent));
            var greens = board.BuiltTiles.Where(tile =>
                tile.category == ConstructionCategory.Green && tile.builtDay <= day)
                .OrderBy(tile => tile.buildIndex).ToArray();
            foreach (var green in greens)
            {
                var current = Stock(green.id);
                var produced = green.id == ConstructionBoardModel.StarterOakId && day == 1
                    ? 0 : Math.Min(DailyYield(green, day,
                            useV6YieldRule, runSeed,
                            useGreenUpgrade ? board.GreenUpgradeLevel(green) : 1),
                        Capacity(board, green, useGreenUpgrade) - current);
                stock[green.id] = current + produced;
                result.foodChanges.Add(new ConstructionFoodChange
                {
                    tileId = green.id,
                    produced = produced
                });
            }

            if (useLocalFoodAreaScore)
            {
                var area = ConstructionFoodAreaScorer.Score(board, day,
                    Stock, ConstructionBoardModel.StarterOakId,
                    result.squirrelsPresent, pigeonTileId,
                    result.pigeonsPresent, hedgehogTileId, urbanSources,
                    usePatchWildlife, result.hedgehogHomeTileIds);
                result.animalPoints = area.points;
                result.scoredFoodTileIds = area.sourceTileIds;
            }

            // V7 places each living squirrel in an occupied oak. A nearby
            // source can feed that squirrel once; it cannot feed another
            // squirrel whose own home is outside the source's eight cells.
            var squirrelHomes = SquirrelHomeTiles(board, day,
                result.squirrelsPresent, usePatchWildlife);
            var hungrySquirrelHomes = new List<ConstructionTileData>();
            if (useLocalFoodAreaScore)
            {
                foreach (var home in squirrelHomes)
                {
                    var source = greens.Where(tile =>
                            tile.planting == GreenPlanting.Oak &&
                            Stock(tile.id) > 0 &&
                            ConstructionBoardModel.GridDistance(tile.column,
                                tile.row, home.column, home.row) <= 1 &&
                            board.GreenRouteBetween(home.id, tile.id).Count > 0)
                        .OrderBy(tile => tile.id == home.id ? 0 : 1)
                        .ThenBy(tile => tile.buildIndex).FirstOrDefault();
                    if (source == null)
                    {
                        hungrySquirrelHomes.Add(home);
                        continue;
                    }
                    Eat(result, source.id, 1);
                    result.squirrelAte = true;
                    result.squirrelsFed++;
                    result.squirrelFoodTileIds.Add(source.id);
                    result.squirrelMealHomeTileIds.Add(home.id);
                    result.squirrelFoodTileId ??= source.id;
                }
            }
            else
            {
                // Historical saves retain their established collective
                // feeding order and per-meal points.
                var squirrelSources = greens.Where(tile =>
                    tile.planting == GreenPlanting.Oak && Stock(tile.id) > 0 &&
                    squirrelHomes.Any(home =>
                        ConstructionBoardModel.GridDistance(tile.column,
                            tile.row, home.column, home.row) <= 1 &&
                        board.GreenRouteBetween(home.id, tile.id).Count > 0));
                foreach (var squirrelSource in squirrelSources)
                {
                    if (result.squirrelsFed >= result.squirrelsPresent) break;
                    Eat(result, squirrelSource.id, 1);
                    result.squirrelAte = true;
                    result.squirrelsFed++;
                    result.squirrelFoodTileIds.Add(squirrelSource.id);
                    result.squirrelFoodTileId ??= squirrelSource.id;
                    result.animalPoints += 3;
                }
            }

            IReadOnlyList<ConstructionPigeonGroup> patchPigeonGroups =
                usePatchWildlife
                ? ConstructionGreenPatchModel.PigeonGroups(board, day,
                    result.pigeonsPresent)
                : Array.Empty<ConstructionPigeonGroup>();
            if (usePatchWildlife)
            {
                foreach (var group in patchPigeonGroups)
                {
                    var fedInGroup = 0;
                    foreach (var source in group.Patch.Meadows.Where(tile =>
                                 Stock(tile.id) > 0))
                    {
                        var portions = Math.Min(group.Count - fedInGroup,
                            Stock(source.id));
                        if (portions == 0) break;
                        Eat(result, source.id, portions);
                        fedInGroup += portions;
                        result.pigeonsFed += portions;
                        result.pigeonFoodTileIds.Add(source.id);
                        for (var portion = 0; portion < portions; portion++)
                        {
                            result.pigeonMealTileIds.Add(source.id);
                            result.pigeonMealHomeTileIds.Add(group.Origin.id);
                        }
                        result.pigeonFoodTileId ??= source.id;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(pigeonTileId))
            {
                var origin = board.BuiltTiles.FirstOrDefault(tile =>
                    tile.id == pigeonTileId &&
                    tile.category == ConstructionCategory.Green);
                if (origin != null)
                {
                    var reachable = board.AnimalReachableGreenTiles(pigeonTileId, day)
                        .Where(tile => tile.planting == GreenPlanting.Meadow &&
                            tile.builtDay <= day && Stock(tile.id) > 0)
                        .OrderBy(tile => tile.id == pigeonTileId ? 0 : 1)
                        .ThenBy(tile => ConstructionBoardModel.GridDistance(
                            tile.column, tile.row, origin.column, origin.row))
                        .ThenBy(tile => tile.buildIndex);
                    foreach (var source in reachable)
                    {
                        var needed = result.pigeonsPresent - result.pigeonsFed;
                        if (needed <= 0) break;
                        var portions = Math.Min(needed, Stock(source.id));
                        Eat(result, source.id, portions);
                        result.pigeonsFed += portions;
                        result.pigeonFoodTileIds.Add(source.id);
                        for (var portion = 0; portion < portions; portion++)
                        {
                            result.pigeonMealTileIds.Add(source.id);
                            result.pigeonMealHomeTileIds.Add(pigeonTileId);
                        }
                        result.pigeonFoodTileId ??= source.id;
                        if (!useLocalFoodAreaScore)
                            result.animalPoints += 3 * portions;
                    }
                }
            }

            if (usePatchWildlife)
            {
                foreach (var homeId in result.hedgehogHomeTileIds)
                {
                    var patch = ConstructionGreenPatchModel.Find(board, day)
                        .FirstOrDefault(group => group.Shrubs.Any(tile =>
                            tile.id == homeId));
                    var shrub = patch?.Shrubs.FirstOrDefault(tile =>
                        Stock(tile.id) > 0);
                    if (shrub == null) continue;
                    Eat(result, shrub.id, 1);
                    result.hedgehogFed = true;
                    result.hedgehogFoodTileId ??= shrub.id;
                    result.hedgehogMealHomeTileIds.Add(homeId);
                    result.hedgehogMealTileIds.Add(shrub.id);
                }
            }
            else if (result.hedgehogPresent)
            {
                var shrub = board.AnimalReachableGreenTiles(hedgehogTileId, day)
                    .Where(tile => tile.planting == GreenPlanting.Shrub &&
                        tile.builtDay <= day && Stock(tile.id) > 0)
                    .OrderBy(tile => tile.id == hedgehogTileId ? 0 : 1)
                    .ThenBy(tile => tile.buildIndex).FirstOrDefault();
                if (shrub != null)
                {
                    Eat(result, shrub.id, 1);
                    result.hedgehogFed = true;
                    result.hedgehogFoodTileId = shrub.id;
                    result.hedgehogMealHomeTileIds.Add(hedgehogTileId);
                    result.hedgehogMealTileIds.Add(shrub.id);
                    if (!useLocalFoodAreaScore) result.animalPoints += 3;
                }
            }

            // Every urban portion must be produced by today's actual activity
            // or collected waste and be reachable through built passable tiles.
            var scraps = (urbanSources ?? Array.Empty<ConstructionUrbanFoodSource>())
                .Where(source => source != null && source.portions > 0 &&
                    (source.category is ConstructionCategory.Supermarket or
                        ConstructionCategory.Waste or ConstructionCategory.Square) &&
                    board.BuiltTiles.Any(tile => tile.id == source.tileId &&
                        tile.category == source.category && tile.builtDay <= day))
                .Select(source => new ConstructionUrbanFoodSource
                {
                    tileId = source.tileId,
                    category = source.category,
                    portions = source.portions
                }).OrderBy(source => source.category switch
                {
                    ConstructionCategory.Supermarket => 0,
                    ConstructionCategory.Square => 1,
                    _ => 2
                })
                .ThenBy(source => source.tileId).ToList();
            var builtById = useLocalFoodAreaScore
                ? board.BuiltTiles.ToDictionary(tile => tile.id) : null;
            void EatUrban(string originId, Action<string> record,
                Func<bool> hungry, bool localOnly = false)
            {
                if (string.IsNullOrEmpty(originId) || !hungry()) return;
                if (localOnly && !builtById.TryGetValue(originId,
                        out _)) return;
                var origin = localOnly ? builtById[originId] : null;
                var reachableIds = new HashSet<string>();
                foreach (var source in scraps)
                    if (!reachableIds.Contains(source.tileId) &&
                        (!localOnly || builtById.TryGetValue(source.tileId,
                            out var sourceTile) &&
                         ConstructionBoardModel.GridDistance(origin.column,
                             origin.row, sourceTile.column,
                             sourceTile.row) <= 1) &&
                        board.AnimalRouteBetween(originId, source.tileId,
                            day).Count > 0)
                        reachableIds.Add(source.tileId);
                while (hungry())
                {
                    var source = scraps.FirstOrDefault(candidate =>
                        candidate.portions > 0 &&
                        reachableIds.Contains(candidate.tileId));
                    if (source == null) break;
                    source.portions--;
                    result.urbanMealTileIds.Add(source.tileId);
                    if (!useLocalFoodAreaScore)
                        result.animalPoints += source.category ==
                            ConstructionCategory.Waste ? 1 : 2;
                    record(source.tileId);
                }
            }
            void RecordSquirrelUrbanMeal(string id)
            {
                result.squirrelsFed++;
                result.squirrelAte = true;
                result.squirrelFoodTileIds.Add(id);
                result.squirrelFoodTileId ??= id;
            }
            if (useLocalFoodAreaScore)
            {
                foreach (var home in hungrySquirrelHomes)
                {
                    var hungry = true;
                    EatUrban(home.id, id =>
                    {
                        RecordSquirrelUrbanMeal(id);
                        result.squirrelMealHomeTileIds.Add(home.id);
                        hungry = false;
                    }, () => hungry, localOnly: true);
                }
            }
            else
                EatUrban(ConstructionBoardModel.StarterOakId,
                    RecordSquirrelUrbanMeal,
                    () => result.squirrelsFed < result.squirrelsPresent);
            if (usePatchWildlife)
            {
                foreach (var group in patchPigeonGroups)
                {
                    var fedAtHome = result.pigeonMealHomeTileIds.Count(id =>
                        id == group.Origin.id);
                    EatUrban(group.Origin.id, id =>
                    {
                        result.pigeonsFed++;
                        result.pigeonMealTileIds.Add(id);
                        result.pigeonMealHomeTileIds.Add(group.Origin.id);
                        if (!result.pigeonFoodTileIds.Contains(id))
                            result.pigeonFoodTileIds.Add(id);
                        result.pigeonFoodTileId ??= id;
                        fedAtHome++;
                    }, () => fedAtHome < group.Count);
                }
            }
            else if (!string.IsNullOrEmpty(pigeonTileId))
            {
                EatUrban(pigeonTileId, id =>
                {
                    result.pigeonsFed++;
                    result.pigeonMealTileIds.Add(id);
                    result.pigeonMealHomeTileIds.Add(pigeonTileId);
                    if (!result.pigeonFoodTileIds.Contains(id))
                        result.pigeonFoodTileIds.Add(id);
                    result.pigeonFoodTileId ??= id;
                }, () => result.pigeonsFed < result.pigeonsPresent);
            }
            if (usePatchWildlife)
            {
                foreach (var homeId in result.hedgehogHomeTileIds.Where(id =>
                             !result.hedgehogMealHomeTileIds.Contains(id)))
                {
                    var hungry = true;
                    EatUrban(homeId, id =>
                    {
                        result.hedgehogFed = true;
                        result.hedgehogFoodTileId ??= id;
                        result.hedgehogMealHomeTileIds.Add(homeId);
                        result.hedgehogMealTileIds.Add(id);
                        hungry = false;
                    }, () => hungry, localOnly: true);
                }
            }
            else if (result.hedgehogPresent && !result.hedgehogFed)
                EatUrban(hedgehogTileId, id =>
                {
                    result.hedgehogFed = true;
                    result.hedgehogFoodTileId = id;
                    result.hedgehogMealHomeTileIds.Add(hedgehogTileId);
                    result.hedgehogMealTileIds.Add(id);
                }, () => !result.hedgehogFed);

            foreach (var change in result.foodChanges)
                change.remaining = Stock(change.tileId);
            if (useFedAnimalScore)
            {
                var fedScore = ConstructionFoodAreaScorer.ScoreFed(board, result);
                result.animalPoints = fedScore.points;
                result.fedScoringSourceTileIds = fedScore.sourceTileIds;
            }
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
            out ConstructionFoodModel food,
            bool useGreenUpgrade = false)
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
                    item.stock > Capacity(board, tile, useGreenUpgrade) ||
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
