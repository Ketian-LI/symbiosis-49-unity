using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    // Observed changes only. This summary never predicts a meal or awards points.
    public static class ConstructionRecentChanges
    {
        public readonly struct Entry
        {
            public readonly string English, Chinese;
            public readonly bool Concern, Upgrade;
            public Entry(string english, string chinese, bool concern = false, bool upgrade = false)
            { English = english; Chinese = chinese; Concern = concern; Upgrade = upgrade; }
        }

        public static IReadOnlyList<Entry> For(ConstructionRunModel run)
        {
            var rows = new List<Entry>();
            if (run?.LastScoreDay == null) return rows;
            var day = run.LastScoreDay.day;
            var placed = run.BuiltTiles.Where(tile => tile.builtDay <= day)
                .OrderByDescending(tile => tile.buildIndex).FirstOrDefault();
            var traffic = run.LastScoreDay.trafficPenaltyPoints;
            if (traffic > 0)
                rows.Add(new Entry($"Traffic collision  -{traffic}", $"车祸扣分  -{traffic}", true));
            if (run.UsesPlacementScore && placed != null)
            {
                foreach (var group in run.BuiltTiles.Where(tile => tile.buildIndex < placed.buildIndex &&
                    (run.PlacementRulesVersion >= ConstructionPlacementScore.BalancedRulesVersion
                        ? ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row,
                            placed.column, placed.row)
                        : System.Math.Abs(tile.column - placed.column) <= 1 &&
                          System.Math.Abs(tile.row - placed.row) <= 1))
                    .GroupBy(tile => tile.category))
                {
                    var points = ConstructionPlacementScore.PairPoints(placed.category, group.Key,
                        run.PlacementRulesVersion) * group.Count();
                    if (points >= 0) continue;
                    var road = placed.category == ConstructionCategory.Street || group.Key == ConstructionCategory.Street;
                    rows.Add(new Entry((road ? "Street beside green" : "Waste beside home") +
                        $" ×{group.Count()}  {points}", (road ? "街道贴绿地" : "垃圾间贴住宅") +
                        $" ×{group.Count()}  {points}", true));
                }
                if (run.UsesNeighborUpgrades)
                {
                    var board = run.BuiltTiles.Where(tile => tile.buildIndex <= placed.buildIndex).ToArray();
                    var upgrades = ConstructionPlacementScore.NewlyUpgradedTiles(placed, board);
                    foreach (var group in upgrades.GroupBy(tile => tile.category))
                    {
                        if (group.Key == ConstructionCategory.Green)
                        {
                            var level = group.Max(tile => ConstructionPlacementScore.UpgradeLevel(tile, board));
                            rows.Add(new Entry($"Habitat upgraded to level {level}", $"绿地升级至 {level} 级", upgrade:true));
                        }
                        else if (group.Key == ConstructionCategory.Residence)
                            rows.Add(new Entry($"Garden homes ×{group.Count()}", $"花园住宅 ×{group.Count()}", upgrade:true));
                        else if (group.Key == ConstructionCategory.Restaurant)
                            rows.Add(new Entry("Terrace: 3 meal seats", "露台餐厅：3 个餐位", upgrade:true));
                        else if (group.Key == ConstructionCategory.Street)
                            rows.Add(new Entry("Slow street: half the risk", "慢行街：过街风险减半", upgrade:true));
                    }
                }
            }
            var ecology = run.LastEcologyDay;
            var meals = (ecology?.squirrelFoodTileIds?.Count ?? 0) +
                (ecology?.pigeonMealTileIds?.Count ?? 0) + (ecology?.hedgehogMealTileIds?.Count ?? 0);
            if (meals > 0) rows.Add(new Entry($"Wildlife ate {meals} " + (meals == 1 ? "portion" : "portions"), $"动物吃了 {meals} 份食物"));
            return rows;
        }
    }
}
