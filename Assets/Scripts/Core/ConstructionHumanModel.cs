using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionResidentDispatch
    {
        public string homeTileId;
        // A null destination leaves that leg to the automatic allocator.
        public string workTileId;
        public string mealTileId;
    }

    [Serializable]
    public sealed class ConstructionResidentDayResult
    {
        public string homeTileId;
        public string workTileId;
        public string mealTileId;
        public string squareTileId;
        // Mature green next to the home that the resident can visibly visit.
        public string greenVisitTileId;
        public bool worked;
        public bool ate;
        public bool completedCycle;
        public bool visitedSquare;
        // Count street cells on each outbound leg, not again on the return.
        public int workStreetTiles;
        public int mealStreetTiles;
        public bool residencePreferenceMet;
        public int basePoints;
        public int workplaceBonus;
        public int mealBonus;
        // Housing rewards are earned from today's surroundings and routes,
        // not from the subtype saved when the room was first built.
        public int greenNeighbourBonus;
        public int shortStreetBonus;
        public int homeBonus;
        public int routeBonus;
        public int squareBonus;
        // V4 scoring: a witnessed, same-day wildlife interaction. Included in
        // satisfactionPoints, never added a second time to shared score.
        public int linkageBonus;
        public ConstructionLinkageKind linkageKind;
        // The actual shared food source credited today. A null value means
        // there was no shared-food award, including on historical saves.
        public string linkageSourceTileId;
        // V8: two different fed species near this home add two resident
        // points. These habitat IDs identify the visible cause of the award.
        public int biodiversityBonus;
        public List<string> biodiversityHabitatTileIds = new();
        public int satisfactionPoints;
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
        public int SquareVisits => residents?.Count(person =>
            person.visitedSquare) ?? 0;
        public int SatisfactionPoints => residents?.Sum(person =>
            person.satisfactionPoints) ?? 0;
        public int LinkagePoints => residents?.Sum(person =>
            person.linkageBonus + person.biodiversityBonus) ?? 0;
    }

    // One resident per home. Capacities are provisional balancing values, but
    // routes, assignment, first meal and score all use this same daily result.
    public static class ConstructionHumanModel
    {
        public static int DailyCapacity(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Residence => 1,
                ConstructionCategory.Workshop => 2,
                ConstructionCategory.Restaurant => 2,
                ConstructionCategory.Supermarket => 2,
                _ => 0
            };

        public static int DailyJobCapacity(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Workshop => 2,
                ConstructionCategory.Restaurant or
                    ConstructionCategory.Supermarket => 1,
                _ => 0
            };

        public static int DailyMealCapacity(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Restaurant => 2,
                ConstructionCategory.Supermarket => 2,
                _ => 0
            };

        public static int DailyMealCapacity(ConstructionTileData tile,
            IEnumerable<ConstructionTileData> builtTiles,
            bool useNeighborUpgrades)
        {
            if (tile == null) return 0;
            return DailyMealCapacity(tile.category) +
                (useNeighborUpgrades &&
                 ConstructionPlacementScore.IsTerraceRestaurant(tile,
                     builtTiles) ? 1 : 0);
        }

        public static ConstructionHumanDayResult SettleDay(int day,
            ConstructionBoardModel board,
            IReadOnlyCollection<string> blockedHomes = null,
            IReadOnlyCollection<string> blockedFoodServices = null,
            IReadOnlyCollection<string> departedHomes = null,
            IReadOnlyList<ConstructionResidentDispatch> dispatches = null,
            bool enhancedGreenVisitScore = false,
            bool useNeighborUpgrades = false)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            if (board == null) throw new ArgumentNullException(nameof(board));
            var built = board.BuiltTiles.Where(tile => tile.builtDay <= day).ToArray();
            var homes = built.Where(tile =>
                tile.category == ConstructionCategory.Residence &&
                departedHomes?.Contains(tile.id) != true).ToArray();
            var workplaces = built.Where(tile =>
                DailyJobCapacity(tile.category) > 0).ToArray();
            var foodServices = built.Where(tile =>
                DailyMealCapacity(tile.category) > 0).ToArray();
            var squares = built.Where(tile =>
                tile.category == ConstructionCategory.Square).ToArray();
            var result = new ConstructionHumanDayResult { day = day };
            if (homes.Length == 0) return result;

            // Rotate priority so a full room does not always exclude the same
            // household. The same board and day always produce the same result.
            var active = new List<Household>();
            foreach (var home in homes.Select((tile, index) => (tile, index))
                         .OrderBy(item => (item.index + day) % homes.Length)
                         .Select(item => item.tile))
            {
                var resident = new ConstructionResidentDayResult
                {
                    homeTileId = home.id
                };
                result.residents.Add(resident);
                if (blockedHomes?.Contains(home.id) == true)
                {
                    resident.routeTileIds.Add(home.id);
                    continue;
                }
                resident.greenVisitTileId = FindMatureGreenNeighbour(home,
                    board, day)?.id;
                active.Add(BuildHousehold(home, resident, workplaces,
                    board, day, blockedFoodServices));
            }

            result.restaurantDemand = active.Count(home => home.reachesRestaurant);
            var remainingJobs = workplaces.ToDictionary(tile => tile.id,
                tile => DailyJobCapacity(tile.category));
            var remainingMeals = foodServices.ToDictionary(tile => tile.id,
                tile => DailyMealCapacity(tile, built, useNeighborUpgrades));
            // Explicit routes are reserved first. Most recently changed
            // households win a scarce seat; all other homes retain the
            // maximum-cycle allocator and its normal rotating priority.
            var reserved = ReserveDispatches(dispatches, active, board, day,
                remainingJobs, remainingMeals);
            var automatic = AssignCompletedCycles(active.Where(home =>
                    !reserved.ContainsKey(home.home.id)).ToArray(), workplaces,
                foodServices, board, day, remainingJobs, remainingMeals);
            foreach (var assignment in automatic)
            {
                if (assignment.job != null)
                    remainingJobs[assignment.job.tile.id]--;
                if (assignment.meal != null)
                    remainingMeals[assignment.meal.tile.id]--;
            }
            var automaticByHome = automatic.ToDictionary(item =>
                item.household.home.id);
            var assignments = active.Select(home =>
                reserved.TryGetValue(home.home.id, out var explicitAssignment)
                    ? explicitAssignment : automaticByHome[home.home.id]).ToList();
            // Only after reserving every possible complete cycle may an
            // unserved resident take a spare job or meal by itself.
            AssignSpareServices(assignments, true, remainingJobs);
            AssignSpareServices(assignments, false, remainingMeals);
            var squareVisits = AssignSquareVisits(assignments, squares,
                board, day);
            foreach (var assignment in assignments)
            {
                var household = assignment.household;
                var resident = household.resident;
                var work = assignment.job;
                var meal = assignment.meal;
                resident.worked = work != null;
                resident.ate = meal != null;
                resident.workTileId = work?.tile.id;
                resident.mealTileId = meal?.tile.id;
                resident.workStreetTiles = work?.streets ?? 0;
                resident.mealStreetTiles = meal?.streets ?? 0;
                resident.completedCycle = resident.worked && resident.ate;
                if (resident.completedCycle)
                {
                    resident.basePoints = 1;
                    resident.workplaceBonus = WorkplaceBonus(work.tile.category);
                    resident.mealBonus = MealBonus(meal.tile.category);
                    resident.greenNeighbourBonus = household.adjacentMatureGreen
                        ? (enhancedGreenVisitScore ? 2 : 1) : 0;
                    resident.shortStreetBonus = HasShortStreetCommute(
                        work.streets, meal.streets) ? 2 : 0;
                    resident.homeBonus = resident.greenNeighbourBonus +
                        resident.shortStreetBonus;
                    resident.routeBonus = work.streets == 0 &&
                        meal.streets == 0 ? 1 : 0;
                    // Retain the saved flag for compatibility; it now means
                    // at least one of the three housing conditions was met.
                    resident.residencePreferenceMet = resident.homeBonus > 0 ||
                        resident.routeBonus > 0;
                    resident.satisfactionPoints = resident.basePoints +
                        resident.workplaceBonus + resident.mealBonus +
                        resident.homeBonus + resident.routeBonus;
                }
                // A completed work and meal day can end with one public-space
                // visit. Each square hosts one resident and leaves one edible
                // portion for wildlife on that day only.
                squareVisits.TryGetValue(household.home.id, out var square);
                if (square != null)
                {
                    resident.squareTileId = square.tile.id;
                    resident.visitedSquare = true;
                    resident.squareBonus = 1;
                    resident.satisfactionPoints += resident.squareBonus;
                }
                resident.routeTileIds = ComposeRoundTrip(household.home.id,
                    work?.route, meal?.route, square?.route, board, day);
                if (!household.reachesRestaurant) continue;
                // A resident who deliberately selected and reached the market
                // did not request a restaurant seat. Automatic overflow to a
                // market still counts as unmet restaurant demand.
                if (meal?.tile.category == ConstructionCategory.Supermarket &&
                    dispatches?.LastOrDefault(plan => plan != null &&
                        plan.homeTileId == household.home.id)?.mealTileId ==
                        meal.tile.id)
                {
                    result.restaurantDemand--;
                    continue;
                }
                if (meal?.tile.category == ConstructionCategory.Restaurant)
                    result.restaurantServed++;
                else if (!household.openRestaurants.Any())
                    result.restaurantRejectedForWaste++;
                else if (household.openRestaurants.All(option =>
                             remainingMeals[option.tile.id] == 0))
                    result.restaurantRejectedForCapacity++;
            }
            return result;
        }

        private sealed class ServiceChoice
        {
            public ConstructionTileData tile;
            public IReadOnlyList<ConstructionTileData> route;
            public int streets;
            public int rank;
        }

        private sealed class Household
        {
            public ConstructionTileData home;
            public ConstructionResidentDayResult resident;
            public bool reachesRestaurant;
            public bool adjacentMatureGreen;
            public List<ServiceChoice> openRestaurants = new();
            public List<ServiceChoice> jobs = new();
            public List<ServiceChoice> meals = new();
        }

        private sealed class Assignment
        {
            public Household household;
            public ServiceChoice job;
            public ServiceChoice meal;
        }

        private static Dictionary<string, ServiceChoice> AssignSquareVisits(
            IReadOnlyList<Assignment> assignments,
            IReadOnlyList<ConstructionTileData> squares,
            ConstructionBoardModel board, int day)
        {
            var eligible = assignments.Where(item => item.job != null &&
                item.meal != null).ToArray();
            var visits = new Dictionary<string, ServiceChoice>();
            if (eligible.Length == 0 || squares.Count == 0) return visits;
            var firstSquare = 1 + eligible.Length;
            var sink = firstSquare + squares.Count;
            var flow = new FlowNetwork(sink + 1);
            var links = new List<(string homeId, ServiceChoice choice,
                FlowEdge edge)>();
            var priorityWeight = squares.Count + 1L;
            var distanceWeight = (eligible.Length + 1L) *
                (eligible.Length + 1L) * priorityWeight;
            for (var index = 0; index < eligible.Length; index++)
            {
                var home = eligible[index].household.home;
                flow.AddEdge(0, 1 + index, 1, 0);
                for (var squareIndex = 0; squareIndex < squares.Count;
                     squareIndex++)
                {
                    var square = squares[squareIndex];
                    var route = board.ResidenceServiceRouteTo(home.id,
                        square.id, day);
                    if (route.Count == 0) continue;
                    var choice = new ServiceChoice
                    {
                        tile = square,
                        route = route,
                        streets = CountStreets(route)
                    };
                    // Maximum flow serves as many households as possible;
                    // cost then prefers shorter journeys. Rotating household
                    // order and square index settle equally short ties.
                    var cost = route.Count * distanceWeight +
                        index * priorityWeight +
                        (squareIndex + day) % squares.Count;
                    var edge = flow.AddEdge(1 + index,
                        firstSquare + squareIndex, 1, cost);
                    links.Add((home.id, choice, edge));
                }
            }
            for (var index = 0; index < squares.Count; index++)
                flow.AddEdge(firstSquare + index, sink, 1, 0);
            flow.MaxFlow(0, sink);
            foreach (var link in links.Where(item => item.edge.capacity == 0))
                visits.Add(link.homeId, link.choice);
            return visits;
        }

        private static Dictionary<string, Assignment> ReserveDispatches(
            IReadOnlyList<ConstructionResidentDispatch> dispatches,
            IReadOnlyList<Household> households,
            ConstructionBoardModel board, int day,
            Dictionary<string, int> remainingJobs,
            Dictionary<string, int> remainingMeals)
        {
            var reserved = new Dictionary<string, Assignment>();
            if (dispatches == null || dispatches.Count == 0) return reserved;
            var byId = households.ToDictionary(home => home.home.id);
            for (var index = dispatches.Count - 1; index >= 0; index--)
            {
                var plan = dispatches[index];
                if (plan == null || string.IsNullOrEmpty(plan.homeTileId) ||
                    !byId.TryGetValue(plan.homeTileId,
                        out var home) || reserved.ContainsKey(plan.homeTileId))
                    continue;
                var jobs = home.jobs.Where(choice =>
                    remainingJobs[choice.tile.id] > 0).ToArray();
                var meals = home.meals.Where(choice =>
                    remainingMeals[choice.tile.id] > 0).ToArray();
                var wantedJob = jobs.FirstOrDefault(choice =>
                    choice.tile.id == plan.workTileId);
                var wantedMeal = meals.FirstOrDefault(choice =>
                    choice.tile.id == plan.mealTileId);
                var assignment = new Assignment { household = home };
                if (jobs.Length > 0 && meals.Length > 0 &&
                    (wantedJob != null || wantedMeal != null))
                {
                    var pair = jobs.SelectMany(job => meals.Select(meal =>
                        (job, meal)))
                        .OrderByDescending(item =>
                            (item.job == wantedJob ? 1 : 0) +
                            (item.meal == wantedMeal ? 1 : 0))
                        .ThenBy(item => PairCost(home, item.job, item.meal,
                            board, day, 100000L, 100L))
                        .First();
                    assignment.job = pair.job;
                    assignment.meal = pair.meal;
                }
                else
                {
                    // A chosen destination may still be used when no
                    // complementary service is currently available.
                    assignment.job = wantedJob;
                    assignment.meal = wantedMeal;
                }
                if (assignment.job == null && assignment.meal == null)
                    continue;
                if (assignment.job != null)
                    remainingJobs[assignment.job.tile.id]--;
                if (assignment.meal != null)
                    remainingMeals[assignment.meal.tile.id]--;
                reserved.Add(home.home.id, assignment);
            }
            return reserved;
        }

        private static Household BuildHousehold(ConstructionTileData home,
            ConstructionResidentDayResult resident,
            IEnumerable<ConstructionTileData> workplaces,
            ConstructionBoardModel board, int day,
            IReadOnlyCollection<string> blockedServices)
        {
            var household = new Household
            {
                home = home,
                resident = resident,
                adjacentMatureGreen = HasMatureGreenNeighbour(home, board, day)
            };
            foreach (var tile in workplaces)
            {
                var route = board.ResidenceServiceRouteTo(home.id, tile.id, day);
                if (route.Count == 0) continue;
                if (tile.category == ConstructionCategory.Restaurant)
                    household.reachesRestaurant = true;
                if (blockedServices?.Contains(tile.id) == true) continue;
                var choice = new ServiceChoice
                {
                    tile = tile,
                    route = route,
                    streets = CountStreets(route)
                };
                household.jobs.Add(choice);
                if (DailyMealCapacity(tile.category) > 0)
                    household.meals.Add(choice);
                if (tile.category == ConstructionCategory.Restaurant)
                    household.openRestaurants.Add(choice);
            }
            household.jobs = household.jobs.OrderBy(option => option.route.Count)
                .ThenBy(option => JobPriority(option.tile.category))
                .ThenBy(option => option.tile.buildIndex).ToList();
            household.meals = household.meals.OrderBy(option =>
                    option.tile.category == ConstructionCategory.Restaurant ? 0 : 1)
                .ThenBy(option => option.route.Count)
                .ThenBy(option => option.tile.buildIndex).ToList();
            for (var index = 0; index < household.jobs.Count; index++)
                household.jobs[index].rank = index;
            // The same service choice may occur in both lists. Meal rank is
            // kept separately so the work tie-break remains unchanged.
            return household;
        }

        private static List<Assignment> AssignCompletedCycles(
            IReadOnlyList<Household> households,
            IReadOnlyList<ConstructionTileData> workplaces,
            IReadOnlyList<ConstructionTileData> foodServices,
            ConstructionBoardModel board, int day,
            IReadOnlyDictionary<string, int> remainingJobs,
            IReadOnlyDictionary<string, int> remainingMeals)
        {
            var assignments = households.Select(home =>
                new Assignment { household = home }).ToList();
            if (households.Count == 0 || workplaces.Count == 0 ||
                foodServices.Count == 0) return assignments;
            var rankBudget = 1L + households.Sum(home =>
                home.jobs.Count + home.meals.Count);
            var restaurantWeight = rankBudget + 1;
            var scoreWeight = (households.Count + 1L) *
                restaurantWeight + rankBudget + 1;
            // MaxFlow first maximizes complete work-and-meal days. For an
            // equally large group, the day-rotated home order must outweigh
            // every possible score, meal preference, and route-rank change.
            // Otherwise a permanent green-neighbour bonus can give the same
            // home a scarce seat every day. RefineSatisfaction later chooses
            // the best services for the homes selected by this priority.
            var maximumQualityCost = 6L * scoreWeight + restaurantWeight +
                rankBudget;
            var homePriorityWeight = households.Count * maximumQualityCost + 1;
            var jobIndexes = workplaces.Select((tile, index) => (tile, index))
                .ToDictionary(item => item.tile.id, item => item.index);
            var mealIndexes = foodServices.Select((tile, index) => (tile, index))
                .ToDictionary(item => item.tile.id, item => item.index);
            var workStart = 1;
            var homeInStart = workStart + workplaces.Count;
            var homeOutStart = homeInStart + households.Count;
            var mealStart = homeOutStart + households.Count;
            var sink = mealStart + foodServices.Count;
            var flow = new FlowNetwork(sink + 1);
            var jobEdges = new List<(ServiceChoice choice, FlowEdge edge)>[households.Count];
            var mealEdges = new List<(ServiceChoice choice, FlowEdge edge)>[households.Count];
            for (var index = 0; index < workplaces.Count; index++)
                flow.AddEdge(0, workStart + index,
                    remainingJobs[workplaces[index].id], 0);
            for (var index = 0; index < foodServices.Count; index++)
                flow.AddEdge(mealStart + index, sink,
                    remainingMeals[foodServices[index].id], 0);
            for (var index = 0; index < households.Count; index++)
            {
                var home = households[index];
                jobEdges[index] = new();
                mealEdges[index] = new();
                // A job and a meal must pass through the same resident's
                // single-capacity node. Shared services cannot mix routes
                // from different homes into a fictitious completed cycle.
                var possibleHomePoints = PossibleHomePoints(home);
                flow.AddEdge(homeInStart + index, homeOutStart + index, 1,
                    (4 - possibleHomePoints) * scoreWeight +
                    index * homePriorityWeight);
                foreach (var choice in home.jobs)
                {
                    var cost = (2 - WorkplaceBonus(choice.tile.category)) *
                        scoreWeight + choice.rank;
                    var edge = flow.AddEdge(workStart +
                        jobIndexes[choice.tile.id], homeInStart + index, 1, cost);
                    jobEdges[index].Add((choice, edge));
                }
                foreach (var choice in home.meals)
                {
                    var cost = (1 - MealBonus(choice.tile.category)) *
                        scoreWeight +
                        (choice.tile.category == ConstructionCategory.Restaurant
                            ? 0 : restaurantWeight) + MealRank(home, choice);
                    var edge = flow.AddEdge(homeOutStart + index,
                        mealStart + mealIndexes[choice.tile.id], 1, cost);
                    mealEdges[index].Add((choice, edge));
                }
            }
            flow.MaxFlow(0, sink);
            for (var index = 0; index < households.Count; index++)
            {
                assignments[index].job = jobEdges[index]
                    .FirstOrDefault(item => item.edge.capacity == 0).choice;
                assignments[index].meal = mealEdges[index]
                    .FirstOrDefault(item => item.edge.capacity == 0).choice;
            }
            RefineSatisfaction(assignments, workplaces, foodServices,
                board, day, scoreWeight, restaurantWeight,
                remainingJobs, remainingMeals);
            return assignments;
        }

        // Match leftover capacity globally too: a flexible household must not
        // take the only reachable job/seat of a constrained household.
        private static void AssignSpareServices(IReadOnlyList<Assignment> assignments,
            bool work, Dictionary<string, int> remaining)
        {
            var missing = assignments.Where(item => work ? item.job == null : item.meal == null).ToArray();
            var services = remaining.Where(item => item.Value > 0).Select(item => item.Key).ToArray();
            if (missing.Length == 0 || services.Length == 0) return;
            var indexes = services.Select((id,index)=>(id,index)).ToDictionary(item=>item.id,item=>item.index);
            var sink = 1 + missing.Length + services.Length;
            var flow = new FlowNetwork(sink + 1);
            var links = new List<(int home, ServiceChoice choice, FlowEdge edge)>();
            var choices = missing.Select(item => (work ? item.household.jobs : item.household.meals)
                .Where(choice => indexes.ContainsKey(choice.tile.id)).ToArray()).ToArray();
            var rankBudget = 1L + choices.Sum(list=>list.Length==0?0:list.Max(choice=>choice.rank));
            var priorityWeight = 1L + choices.Sum(list=>list.Length == 0 ? 0L :
                list.Max(choice=>choice.route.Count * rankBudget + choice.rank));
            for (var index=0;index<missing.Length;index++)
            {
                flow.AddEdge(0,1+index,1,index * priorityWeight);
                foreach (var choice in choices[index])
                {
                    var edge=flow.AddEdge(1+index,1+missing.Length+indexes[choice.tile.id],1,
                        choice.route.Count * rankBudget + choice.rank);
                    links.Add((index,choice,edge));
                }
            }
            for(var index=0;index<services.Length;index++)
                flow.AddEdge(1+missing.Length+index,sink,remaining[services[index]],0);
            flow.MaxFlow(0,sink);
            foreach(var link in links.Where(item=>item.edge.capacity==0))
            {
                if(work)missing[link.home].job=link.choice;else missing[link.home].meal=link.choice;
                remaining[link.choice.tile.id]--;
            }
        }

        private static int MealRank(Household home, ServiceChoice choice) =>
            home.meals.IndexOf(choice);

        private static int PossibleHomePoints(Household home)
        {
            var local = home.jobs.Any(job => job.streets == 0) &&
                home.meals.Any(meal => meal.streets == 0);
            var shortJob = home.jobs.Any(job => job.streets <= 2);
            var shortMeal = home.meals.Any(meal => meal.streets <= 2);
            var streetOnOneLeg = home.jobs.Any(job =>
                job.streets is > 0 and <= 2) || home.meals.Any(meal =>
                meal.streets is > 0 and <= 2);
            var shortStreet = shortJob && shortMeal && streetOnOneLeg;
            // A local route and a street route cannot both be taken on one
            // day. Mature adjacent green can reward either route, so the
            // maximum home-side contribution remains four points.
            return 1 + (home.adjacentMatureGreen ? 1 : 0) +
                (shortStreet ? 2 : local ? 1 : 0);
        }

        private static int WorkplaceBonus(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Workshop => 2,
                ConstructionCategory.Restaurant => 1,
                _ => 0
            };

        private static int MealBonus(ConstructionCategory category) =>
            category == ConstructionCategory.Restaurant ? 1 : 0;

        private static void RefineSatisfaction(List<Assignment> assignments,
            IReadOnlyList<ConstructionTileData> workplaces,
            IReadOnlyList<ConstructionTileData> foodServices,
            ConstructionBoardModel board, int day,
            long scoreWeight, long restaurantWeight,
            IReadOnlyDictionary<string, int> remainingJobs,
            IReadOnlyDictionary<string, int> remainingMeals)
        {
            var served = assignments.Where(item => item.job != null &&
                item.meal != null).ToArray();
            if (served.Length == 0) return;
            // Route comfort depends on both legs. Alternating exact weighted
            // matchings for each leg resolves common restaurant/market choices
            // while retaining the maximum number of completed residents.
            for (var pass = 0; pass < 6; pass++)
            {
                var improved = false;
                foreach (var workSide in new[] { false, true })
                {
                    var choices = OptimizeSide(served, workSide,
                        workSide ? workplaces : foodServices,
                        board, day, scoreWeight, restaurantWeight,
                        workSide ? remainingJobs : remainingMeals);
                    if (choices == null) continue;
                    var previous = PlanCost(served, board, day,
                        scoreWeight, restaurantWeight);
                    var candidate = 0L;
                    for (var index = 0; index < served.Length; index++)
                        candidate += PairCost(served[index].household,
                            workSide ? choices[index] : served[index].job,
                            workSide ? served[index].meal : choices[index],
                            board, day, scoreWeight, restaurantWeight);
                    if (candidate >= previous) continue;
                    for (var index = 0; index < served.Length; index++)
                    {
                        if (workSide) served[index].job = choices[index];
                        else served[index].meal = choices[index];
                    }
                    improved = true;
                }
                if (!improved) break;
            }
        }

        private static ServiceChoice[] OptimizeSide(Assignment[] served,
            bool workSide, IReadOnlyList<ConstructionTileData> services,
            ConstructionBoardModel board, int day,
            long scoreWeight, long restaurantWeight,
            IReadOnlyDictionary<string, int> remainingCapacity)
        {
            var serviceIndexes = services.Select((tile, index) => (tile, index))
                .ToDictionary(item => item.tile.id, item => item.index);
            var serviceStart = 1;
            var homeStart = serviceStart + services.Count;
            var sink = homeStart + served.Length;
            var flow = new FlowNetwork(sink + 1);
            var candidateEdges = new List<(ServiceChoice choice, FlowEdge edge)>[served.Length];
            for (var index = 0; index < services.Count; index++)
            {
                if (workSide)
                    flow.AddEdge(0, serviceStart + index,
                        remainingCapacity[services[index].id], 0);
                else
                    flow.AddEdge(serviceStart + index, sink,
                        remainingCapacity[services[index].id], 0);
            }
            for (var index = 0; index < served.Length; index++)
            {
                candidateEdges[index] = new();
                if (workSide) flow.AddEdge(homeStart + index, sink, 1, 0);
                else flow.AddEdge(0, homeStart + index, 1, 0);
                var home = served[index].household;
                foreach (var choice in workSide ? home.jobs : home.meals)
                {
                    var cost = PairCost(home,
                        workSide ? choice : served[index].job,
                        workSide ? served[index].meal : choice,
                        board, day, scoreWeight, restaurantWeight);
                    var serviceNode = serviceStart + serviceIndexes[choice.tile.id];
                    var edge = workSide
                        ? flow.AddEdge(serviceNode, homeStart + index, 1, cost)
                        : flow.AddEdge(homeStart + index, serviceNode, 1, cost);
                    candidateEdges[index].Add((choice, edge));
                }
            }
            if (flow.MaxFlow(0, sink) != served.Length) return null;
            return candidateEdges.Select(edges => edges.First(item =>
                item.edge.capacity == 0).choice).ToArray();
        }

        private static long PlanCost(IEnumerable<Assignment> served,
            ConstructionBoardModel board, int day,
            long scoreWeight, long restaurantWeight) => served.Sum(item =>
            PairCost(item.household, item.job, item.meal,
                board, day, scoreWeight, restaurantWeight));

        private static long PairCost(Household home, ServiceChoice job,
            ServiceChoice meal, ConstructionBoardModel board, int day,
            long scoreWeight, long restaurantWeight)
        {
            var score = 1 + WorkplaceBonus(job.tile.category) +
                MealBonus(meal.tile.category) +
                (job.streets == 0 && meal.streets == 0 ? 1 : 0) +
                (HasMatureGreenNeighbour(home.home, board, day) ? 1 : 0) +
                (HasShortStreetCommute(job.streets, meal.streets) ? 2 : 0);
            return (7 - score) * scoreWeight +
                (meal.tile.category == ConstructionCategory.Restaurant
                    ? 0 : restaurantWeight) + job.rank + MealRank(home, meal);
        }

        private sealed class FlowEdge
        {
            public int to;
            public int reverse;
            public int capacity;
            public long cost;
        }

        private sealed class FlowNetwork
        {
            private readonly List<FlowEdge>[] edges;

            public FlowNetwork(int nodes)
            {
                edges = Enumerable.Range(0, nodes)
                    .Select(_ => new List<FlowEdge>()).ToArray();
            }

            public FlowEdge AddEdge(int from, int to, int capacity, long cost)
            {
                var forward = new FlowEdge
                {
                    to = to, reverse = edges[to].Count,
                    capacity = capacity, cost = cost
                };
                var backward = new FlowEdge
                {
                    to = from, reverse = edges[from].Count,
                    capacity = 0, cost = -cost
                };
                edges[from].Add(forward);
                edges[to].Add(backward);
                return forward;
            }

            public int MaxFlow(int source, int sink)
            {
                const long infinity = long.MaxValue / 4;
                var potential = new long[edges.Length];
                var total = 0;
                while (true)
                {
                    var distance = Enumerable.Repeat(infinity, edges.Length)
                        .ToArray();
                    var previousNode = Enumerable.Repeat(-1, edges.Length)
                        .ToArray();
                    var previousEdge = new int[edges.Length];
                    var pending = new MinHeap();
                    distance[source] = 0;
                    pending.Push(0, source);
                    while (pending.TryPop(out var currentDistance, out var node))
                    {
                        if (currentDistance != distance[node]) continue;
                        for (var index = 0; index < edges[node].Count; index++)
                        {
                            var edge = edges[node][index];
                            if (edge.capacity == 0) continue;
                            var nextDistance = currentDistance + edge.cost +
                                potential[node] - potential[edge.to];
                            if (nextDistance >= distance[edge.to]) continue;
                            distance[edge.to] = nextDistance;
                            previousNode[edge.to] = node;
                            previousEdge[edge.to] = index;
                            pending.Push(nextDistance, edge.to);
                        }
                    }
                    if (previousNode[sink] < 0) break;
                    for (var node = 0; node < edges.Length; node++)
                        if (distance[node] < infinity)
                            potential[node] += distance[node];
                    var increase = int.MaxValue;
                    for (var node = sink; node != source;
                         node = previousNode[node])
                        increase = Math.Min(increase,
                            edges[previousNode[node]][previousEdge[node]].capacity);
                    for (var node = sink; node != source;
                         node = previousNode[node])
                    {
                        var edge = edges[previousNode[node]][previousEdge[node]];
                        edge.capacity -= increase;
                        edges[edge.to][edge.reverse].capacity += increase;
                    }
                    total += increase;
                }
                return total;
            }
        }

        private sealed class MinHeap
        {
            private readonly List<(long distance, int node)> items = new();

            public void Push(long distance, int node)
            {
                items.Add((distance, node));
                for (var child = items.Count - 1; child > 0;)
                {
                    var parent = (child - 1) / 2;
                    if (Compare(items[parent], items[child]) <= 0) break;
                    (items[parent], items[child]) = (items[child], items[parent]);
                    child = parent;
                }
            }

            public bool TryPop(out long distance, out int node)
            {
                if (items.Count == 0)
                {
                    distance = 0;
                    node = -1;
                    return false;
                }
                (distance, node) = items[0];
                var last = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return true;
                items[0] = last;
                for (var parent = 0; ;)
                {
                    var child = parent * 2 + 1;
                    if (child >= items.Count) break;
                    if (child + 1 < items.Count &&
                        Compare(items[child + 1], items[child]) < 0)
                        child++;
                    if (Compare(items[parent], items[child]) <= 0) break;
                    (items[parent], items[child]) = (items[child], items[parent]);
                    parent = child;
                }
                return true;
            }

            private static int Compare((long distance, int node) left,
                (long distance, int node) right)
            {
                var distance = left.distance.CompareTo(right.distance);
                return distance != 0 ? distance : left.node.CompareTo(right.node);
            }
        }

        private static int JobPriority(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Workshop => 0,
                ConstructionCategory.Restaurant => 1,
                ConstructionCategory.Supermarket => 2,
                _ => 3
            };

        // Retained for source compatibility. Daily scoring never reads the
        // saved residence kind or this helper; old day results keep their
        // historical point totals in the save data.
        public static int ResidencePreferenceBonus(ResidenceKind kind) =>
            kind switch
            {
                ResidenceKind.Garden => 1,
                ResidenceKind.Urban => 2,
                _ => 0
            };

        private static int CountStreets(IReadOnlyList<ConstructionTileData> route) =>
            route?.Count(tile => tile.category == ConstructionCategory.Street) ?? 0;

        private static bool HasShortStreetCommute(int workStreetTiles,
            int mealStreetTiles) =>
            (workStreetTiles > 0 || mealStreetTiles > 0) &&
            workStreetTiles <= 2 && mealStreetTiles <= 2;

        private static bool HasMatureGreenNeighbour(ConstructionTileData home,
            ConstructionBoardModel board, int day) =>
            FindMatureGreenNeighbour(home, board, day) != null;

        private static ConstructionTileData FindMatureGreenNeighbour(
            ConstructionTileData home, ConstructionBoardModel board, int day) =>
            board.BuiltTiles.Where(green =>
                green.category == ConstructionCategory.Green &&
                ConstructionBoardModel.AreNeighbours(home.column, home.row,
                    green.column, green.row) &&
                green.builtDay <= day &&
                ConstructionFoodModel.MaturityStage(green, day) == 2)
                .OrderBy(green => green.buildIndex).FirstOrDefault();

        // Shortest legal legs in the fixed work -> meal -> optional square ->
        // home order. Only streets and this resident's own home are transit.
        private static List<string> ComposeRoundTrip(string homeId,
            IReadOnlyList<ConstructionTileData> work,
            IReadOnlyList<ConstructionTileData> meal,
            IReadOnlyList<ConstructionTileData> square,
            ConstructionBoardModel board, int day)
        {
            var tiles=board.BuiltTiles.Where(t=>Math.Max(1,t.builtDay)<=day).ToArray();
            var route=new List<string>{homeId};
            var stops=new[]{work?.LastOrDefault()?.id,meal?.LastOrDefault()?.id,square?.LastOrDefault()?.id,homeId};
            foreach(var stop in stops.Where(id=>id!=null))
            {
                var start=route.Last();if(start==stop)continue;
                var queue=new Queue<string>();queue.Enqueue(start);
                var previous=new Dictionary<string,string>{{start,null}};
                while(queue.Count>0 && !previous.ContainsKey(stop))
                {
                    var id=queue.Dequeue();var tile=tiles.First(t=>t.id==id);
                    foreach(var next in tiles.Where(t=>ConstructionBoardModel.AreCardinalNeighbours(tile.column,tile.row,t.column,t.row)))
                    {
                        if(previous.ContainsKey(next.id) || !(next.id==stop || next.id==homeId || next.category==ConstructionCategory.Street))continue;
                        previous[next.id]=id;queue.Enqueue(next.id);
                    }
                }
                if(!previous.ContainsKey(stop))throw new InvalidOperationException("Assigned resident leg is disconnected.");
                var leg=new List<string>();for(var id=stop;id!=start;id=previous[id])leg.Add(id);leg.Reverse();route.AddRange(leg);
            }
            return route;
        }
    }
}
