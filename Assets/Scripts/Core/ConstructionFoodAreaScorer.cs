using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class ConstructionFoodAreaScore
    {
        public int points;
        public List<string> sourceTileIds = new();
    }

    // V7: an available food area contributes once, even if several animals
    // can use it. Each living species can claim its own vegetation and any
    // active urban food source in its home tile or one of the eight neighbours.
    public static class ConstructionFoodAreaScorer
    {
        public const int GreenPoints = 1;
        public const int SquarePoints = 4;
        public const int SupermarketPoints = 3;
        public const int WastePoints = 1;
        public const int FedAnimalPoints = 4;
        public const int FedGreenSourcePoints = 2;
        public const int FedUrbanSourcePoints = 1;
        public const int BalancedFedAnimalPoints = 3;
        public const int BalancedFedGreenSourcePoints = 2;

        public static ConstructionFoodAreaScore Score(
            ConstructionBoardModel board, int day,
            Func<string, int> greenStock,
            string squirrelTileId, int squirrelsPresent,
            string pigeonTileId, int pigeonsPresent,
            string hedgehogTileId,
            IReadOnlyList<ConstructionUrbanFoodSource> urbanSources,
            bool usePatchWildlife = false,
            IReadOnlyList<string> patchHedgehogTileIds = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (greenStock == null)
                throw new ArgumentNullException(nameof(greenStock));

            var result = new ConstructionFoodAreaScore();
            var built = board.BuiltTiles.Where(tile => tile.builtDay <= day)
                .OrderBy(tile => tile.buildIndex).ToArray();
            var origins = new List<(ConstructionTileData tile,
                GreenPlanting foodPlanting)>();

            void AddOrigin(string id, bool present, GreenPlanting planting)
            {
                if (!present || string.IsNullOrEmpty(id)) return;
                var tile = built.FirstOrDefault(candidate =>
                    candidate.id == id &&
                    candidate.category == ConstructionCategory.Green);
                if (tile != null) origins.Add((tile, planting));
            }

            if (!string.IsNullOrEmpty(squirrelTileId))
                foreach (var squirrelHome in
                         ConstructionFoodModel.SquirrelHomeTiles(board, day,
                             squirrelsPresent, usePatchWildlife))
                    origins.Add((squirrelHome, GreenPlanting.Oak));
            if (usePatchWildlife)
                foreach (var group in ConstructionGreenPatchModel.PigeonGroups(
                             board, day, pigeonsPresent))
                    AddOrigin(group.Origin.id, true, GreenPlanting.Meadow);
            else
                AddOrigin(pigeonTileId, pigeonsPresent > 0,
                    GreenPlanting.Meadow);
            if (usePatchWildlife && patchHedgehogTileIds != null)
                foreach (var homeId in patchHedgehogTileIds)
                    AddOrigin(homeId, true, GreenPlanting.Shrub);
            else
                AddOrigin(hedgehogTileId,
                    !string.IsNullOrEmpty(hedgehogTileId),
                    GreenPlanting.Shrub);
            if (origins.Count == 0) return result;

            bool IsNearAndReachable(ConstructionTileData origin,
                ConstructionTileData source) =>
                ConstructionBoardModel.GridDistance(origin.column,
                    origin.row, source.column, source.row) <= 1 &&
                board.AnimalRouteBetween(origin.id, source.id, day).Count > 0;

            foreach (var source in built)
            {
                var value = source.category switch
                {
                    ConstructionCategory.Green when greenStock(source.id) > 0 &&
                        origins.Any(origin =>
                            origin.foodPlanting == source.planting &&
                            IsNearAndReachable(origin.tile, source)) =>
                        GreenPoints,
                    ConstructionCategory.Square when HasActiveUrbanSource(
                        urbanSources, source) && origins.Any(origin =>
                            IsNearAndReachable(origin.tile, source)) =>
                        SquarePoints,
                    ConstructionCategory.Supermarket when HasActiveUrbanSource(
                        urbanSources, source) && origins.Any(origin =>
                            IsNearAndReachable(origin.tile, source)) =>
                        SupermarketPoints,
                    ConstructionCategory.Waste when HasActiveUrbanSource(
                        urbanSources, source) && origins.Any(origin =>
                            IsNearAndReachable(origin.tile, source)) =>
                        WastePoints,
                    _ => 0
                };
                if (value == 0) continue;
                result.points += value;
                result.sourceTileIds.Add(source.id);
            }
            return result;
        }

        public static int PointsFor(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Green => GreenPoints,
                ConstructionCategory.Square => SquarePoints,
                ConstructionCategory.Supermarket => SupermarketPoints,
                ConstructionCategory.Waste => WastePoints,
                _ => 0
            };

        // V10: count observed meals, then award each source used by those
        // meals once. The V7 available-source score above remains intact for
        // historical runs and for the existing resident linkage calculation.
        public static ConstructionFoodAreaScore ScoreFed(
            ConstructionBoardModel board, ConstructionEcologyDayResult ecology,
            bool enhancedGreenSourceScore = false,
            bool balancedScore = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (ecology == null) throw new ArgumentNullException(nameof(ecology));

            var usedIds = new HashSet<string>(
                ecology.squirrelFoodTileIds ?? new List<string>());
            usedIds.UnionWith(ecology.pigeonMealTileIds ?? new List<string>());
            usedIds.UnionWith(ecology.hedgehogMealTileIds ?? new List<string>());
            var result = new ConstructionFoodAreaScore
            {
                points = (balancedScore ? BalancedFedAnimalPoints :
                    FedAnimalPoints) * ecology.AnimalMeals
            };
            foreach (var source in board.BuiltTiles
                         .Where(tile => tile.builtDay <= ecology.day &&
                                        usedIds.Contains(tile.id))
                         .OrderBy(tile => tile.buildIndex))
            {
                var bonus = PointsForFedSource(source.category,
                    enhancedGreenSourceScore, balancedScore);
                if (bonus == 0) continue;
                result.points += bonus;
                result.sourceTileIds.Add(source.id);
            }
            return result;
        }

        public static int PointsForFedSource(ConstructionCategory category,
            bool enhancedGreenSourceScore = false,
            bool balancedScore = false) =>
            category switch
            {
                ConstructionCategory.Green => balancedScore
                    ? BalancedFedGreenSourcePoints
                    : FedGreenSourcePoints +
                      (enhancedGreenSourceScore ? 1 : 0),
                ConstructionCategory.Square or ConstructionCategory.Supermarket =>
                    FedUrbanSourcePoints,
                _ => 0
            };

        private static bool HasActiveUrbanSource(
            IReadOnlyList<ConstructionUrbanFoodSource> sources,
            ConstructionTileData tile) =>
            sources != null && sources.Any(source => source != null &&
                source.tileId == tile.id && source.category == tile.category &&
                source.portions > 0);
    }
}
