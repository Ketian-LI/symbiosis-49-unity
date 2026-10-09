using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionResidentDayResult
    {
        public string homeTileId;
        public string workTileId;
        public string mealTileId;
        public bool worked;
        public bool ate;
        public bool completedCycle;
        public List<string> routeTileIds = new();
    }

    [Serializable]
    public sealed class ConstructionHumanDayResult
    {
        public int day;
        public int restaurantDemand;
        public int restaurantServed;
        public int restaurantRejectedForCapacity;
        public int restaurantRejectedForWaste;
        public List<ConstructionResidentDayResult> residents = new();
        public int ResidentCount => residents?.Count ?? 0;
        public int WorkedCount => residents?.Count(person => person.worked) ?? 0;
        public int MealsEaten => residents?.Count(person => person.ate) ?? 0;
        public int CompletedWorkCycles => residents?.Count(person =>
            person.completedCycle) ?? 0;
    }

    // One resident per home. Capacities are provisional balancing values, but
    // routes, assignment, first meal and score all use this same daily result.
    public static class ConstructionHumanModel
    {
        public static int DailyCapacity(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Residence => 1,
                ConstructionCategory.Workshop => 1,
                ConstructionCategory.Restaurant => 1,
                ConstructionCategory.Supermarket => 2,
                _ => 0
            };

        public static ConstructionHumanDayResult SettleDay(int day,
            ConstructionBoardModel board,
            IReadOnlyCollection<string> blockedHomes = null,
            IReadOnlyCollection<string> blockedFoodServices = null)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            if (board == null) throw new ArgumentNullException(nameof(board));
            var built = board.BuiltTiles.Where(tile => tile.builtDay <= day).ToArray();
            var homes = built.Where(tile =>
                tile.category == ConstructionCategory.Residence).ToArray();
            var services = built.Where(tile => DailyCapacity(tile.category) > 0 &&
                tile.category != ConstructionCategory.Residence).ToArray();
            var remaining = services.ToDictionary(tile => tile.id,
                tile => DailyCapacity(tile.category));
            var result = new ConstructionHumanDayResult { day = day };
            if (homes.Length == 0) return result;

            // Rotate priority so a full room does not always exclude the same
            // household. The same board and day always produce the same result.
            foreach (var home in homes.Select((tile, index) => (tile, index))
                         .OrderBy(item => (item.index + day) % homes.Length)
                         .Select(item => item.tile))
            {
                var resident = new ConstructionResidentDayResult
                {
                    homeTileId = home.id
                };
                if (blockedHomes?.Contains(home.id) == true)
                {
                    resident.routeTileIds.Add(home.id);
                    result.residents.Add(resident);
                    continue;
                }
                var work = ChooseAvailable(home.id, services.Where(tile =>
                    tile.category == ConstructionCategory.Workshop), remaining,
                    board, day, blockedFoodServices);
                if (work.tile != null)
                {
                    remaining[work.tile.id]--;
                    resident.worked = true;
                    resident.workTileId = work.tile.id;
                }

                var restaurants = services.Where(tile =>
                    tile.category == ConstructionCategory.Restaurant).ToArray();
                var reachableRestaurants = restaurants.Where(tile =>
                    board.ResidenceServiceRouteTo(home.id, tile.id, day).Count > 0)
                    .ToArray();
                if (reachableRestaurants.Length > 0)
                    result.restaurantDemand++;
                var meal = ChooseAvailable(home.id, restaurants, remaining,
                    board, day, blockedFoodServices);
                if (meal.tile == null && reachableRestaurants.Length > 0)
                {
                    if (reachableRestaurants.Any(tile =>
                            blockedFoodServices?.Contains(tile.id) != true))
                        result.restaurantRejectedForCapacity++;
                    else result.restaurantRejectedForWaste++;
                }
                if (meal.tile == null)
                    meal = ChooseAvailable(home.id, services.Where(tile =>
                        tile.category == ConstructionCategory.Supermarket),
                        remaining, board, day, blockedFoodServices);
                if (meal.tile != null)
                {
                    remaining[meal.tile.id]--;
                    resident.ate = true;
                    resident.mealTileId = meal.tile.id;
                    if (meal.tile.category == ConstructionCategory.Restaurant)
                        result.restaurantServed++;
                }
                resident.completedCycle = resident.worked && resident.ate;
                resident.routeTileIds = ComposeRoundTrip(home.id,
                    work.route, meal.route);
                result.residents.Add(resident);
            }
            return result;
        }

        private static (ConstructionTileData tile,
            IReadOnlyList<ConstructionTileData> route) ChooseAvailable(
            string homeId, IEnumerable<ConstructionTileData> services,
            IReadOnlyDictionary<string, int> remaining,
            ConstructionBoardModel board, int day,
            IReadOnlyCollection<string> blockedServices)
        {
            var options = services.Where(tile => remaining[tile.id] > 0 &&
                    blockedServices?.Contains(tile.id) != true)
                .Select(tile => (tile, route: board.ResidenceServiceRouteTo(
                    homeId, tile.id, day)))
                .Where(option => option.route.Count > 0)
                .OrderBy(option => option.route.Count)
                .ThenBy(option => option.tile.buildIndex)
                .ToArray();
            return options.Length > 0 ? options[0] :
                (null, Array.Empty<ConstructionTileData>());
        }

        // Work -> meal may pass through the home. Every adjacent step remains
        // on a checked home/street/service route, and the resident returns.
        private static List<string> ComposeRoundTrip(string homeId,
            IReadOnlyList<ConstructionTileData> work,
            IReadOnlyList<ConstructionTileData> meal)
        {
            var route = new List<string> { homeId };
            AppendOutAndBack(route, work);
            AppendOutAndBack(route, meal);
            return route;
        }

        private static void AppendOutAndBack(List<string> journey,
            IReadOnlyList<ConstructionTileData> leg)
        {
            if (leg == null || leg.Count < 2) return;
            for (var index = 1; index < leg.Count; index++)
                journey.Add(leg[index].id);
            for (var index = leg.Count - 2; index >= 0; index--)
                journey.Add(leg[index].id);
        }
    }
}
