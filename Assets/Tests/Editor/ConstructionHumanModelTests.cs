using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionHumanModelTests
    {
        [Test]
        public void SquareNeedsAResidentWithACompletedCycleAndAHomeRoute()
        {
            var board = new ConstructionBoardModel();
            // Keep the square connected to the board without putting it in
            // any of the future home's four directly reachable cells.
            Build(board, 4, 3, ConstructionCategory.Green);
            var square = Build(board, 5, 3, ConstructionCategory.Square);
            Assert.That(ConstructionHumanModel.SettleDay(1, board).SquareVisits,
                Is.Zero, "An empty public square leaves no food.");

            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 3, 1,
                ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Workshop);
            var inaccessible = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(inaccessible.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(inaccessible.SquareVisits, Is.Zero);
            Assert.That(inaccessible.residents.Single().squareTileId, Is.Null);

            var street = Build(board, 4, 2, ConstructionCategory.Street);
            var secondStreet = Build(board, 5, 2, ConstructionCategory.Street);
            var reachable = ConstructionHumanModel.SettleDay(1, board);
            var person = reachable.residents.Single();
            Assert.That(reachable.SquareVisits, Is.EqualTo(1));
            Assert.That(person.squareTileId, Is.EqualTo(square.id));
            Assert.That(person.squareBonus, Is.EqualTo(1));
            Assert.That(reachable.SatisfactionPoints,
                Is.EqualTo(inaccessible.SatisfactionPoints + 1));
            Assert.That(person.routeTileIds.Skip(person.routeTileIds.Count - 6),
                Is.EqualTo(new[] { street.id, secondStreet.id, square.id,
                    secondStreet.id, street.id, home.id }));
            Assert.That(person.routeTileIds,
                Does.Contain(restaurant.id));
        }

        [Test]
        public void SquareHostsOneVisitorAndRotatesHouseholdPriority()
        {
            var board = new ConstructionBoardModel();
            var first = Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 2, 2, ConstructionCategory.Street);
            var second = Build(board, 2, 1, ConstructionCategory.Residence);
            Build(board, 2, 0, ConstructionCategory.Restaurant);
            Build(board, 4, 2, ConstructionCategory.Supermarket);
            var square = Build(board, 3, 1, ConstructionCategory.Square);

            var dayOne = ConstructionHumanModel.SettleDay(1, board);
            var dayTwo = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(dayOne.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(dayTwo.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(dayOne.SquareVisits, Is.EqualTo(1));
            Assert.That(dayTwo.SquareVisits, Is.EqualTo(1));
            Assert.That(dayOne.residents.Single(person => person.visitedSquare)
                .homeTileId, Is.EqualTo(second.id));
            Assert.That(dayTwo.residents.Single(person => person.visitedSquare)
                .homeTileId, Is.EqualTo(first.id));
            Assert.That(dayOne.residents.Single(person => person.visitedSquare)
                .squareTileId, Is.EqualTo(square.id));
        }

        [Test]
        public void IncompleteCycleCannotCreateSquareFoodOrSatisfaction()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 2, 2, ConstructionCategory.Workshop);
            Build(board, 3, 1, ConstructionCategory.Square);
            var day = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(day.WorkedCount, Is.EqualTo(1));
            Assert.That(day.MealsEaten, Is.Zero);
            Assert.That(day.SquareVisits, Is.Zero);
            Assert.That(day.SatisfactionPoints, Is.Zero);
            var person = day.residents.Single();
            Assert.That(person.greenNeighbourBonus, Is.Zero,
                "A mature neighbour is scored only after a complete resident day.");
            Assert.That(person.shortStreetBonus, Is.Zero);
        }

        [Test]
        public void SquareAllocatorDoesNotTakeTheOnlySquareAnotherHomeCanReach()
        {
            var board = new ConstructionBoardModel();
            var flexible = Build(board, 3, 2,
                ConstructionCategory.Residence);
            var constrained = Build(board, 4, 2,
                ConstructionCategory.Residence);
            Build(board, 4, 3, ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Street);
            Build(board, 2, 3, ConstructionCategory.Supermarket);
            var shared = Build(board, 3, 1, ConstructionCategory.Square);
            Build(board, 4, 1, ConstructionCategory.Street);
            var flexibleOnly = Build(board, 2, 1,
                ConstructionCategory.Square);

            Assert.That(board.ResidenceServiceRouteTo(flexible.id,
                shared.id).Count, Is.EqualTo(2));
            Assert.That(board.ResidenceServiceRouteTo(flexible.id,
                flexibleOnly.id).Count, Is.EqualTo(3),
                "The square requires the shared edge-adjacent street.");
            Assert.That(board.ResidenceServiceRouteTo(constrained.id,
                flexibleOnly.id).Count, Is.Zero);
            // On day 2 the flexible resident is first in the rotating
            // priority order. A greedy nearest-square choice would strand
            // the other resident at one visit despite two usable squares.
            var day = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(day.SquareVisits, Is.EqualTo(2));
            Assert.That(day.residents.Single(person =>
                person.homeTileId == flexible.id).squareTileId,
                Is.EqualTo(flexibleOnly.id));
            Assert.That(day.residents.Single(person =>
                person.homeTileId == constrained.id).squareTileId,
                Is.EqualTo(shared.id));
        }

        [Test]
        public void SanitationClosureIsNotMistakenForRestaurantCrowding()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 3, 1, ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Workshop);
            var closedService = ConstructionHumanModel.SettleDay(1, board,
                null, new HashSet<string> { restaurant.id });
            Assert.That(closedService.restaurantDemand, Is.EqualTo(1));
            Assert.That(closedService.restaurantRejectedForWaste, Is.EqualTo(1));
            Assert.That(closedService.restaurantRejectedForCapacity, Is.Zero);
            Assert.That(closedService.residents.Single().worked, Is.True);
            Assert.That(closedService.residents.Single().ate, Is.False);

            var blockedHome = ConstructionHumanModel.SettleDay(1, board,
                new HashSet<string> { home.id });
            Assert.That(blockedHome.residents.Single().routeTileIds,
                Is.EqualTo(new[] { home.id }));
            Assert.That(blockedHome.CompletedWorkCycles, Is.Zero);
        }

        [Test]
        public void RestaurantProvidesAJobAndMealWithoutConsumingItsOwnSeat()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 3, 1, ConstructionCategory.Restaurant);
            var restaurantOnly = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(restaurantOnly.WorkedCount, Is.EqualTo(1));
            Assert.That(restaurantOnly.MealsEaten, Is.EqualTo(1));
            Assert.That(restaurantOnly.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(restaurantOnly.SatisfactionPoints, Is.EqualTo(5),
                "A generic home also earns the mature-green neighbour point.");
            Assert.That(restaurantOnly.residents.Single().routeTileIds,
                Is.EqualTo(new[] { home.id, restaurant.id, home.id }),
                "A combined job and meal is one trip in the animation.");

            var workshop = Build(board, 2, 2, ConstructionCategory.Workshop);
            var day = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(day.ResidentCount, Is.EqualTo(1));
            Assert.That(day.WorkedCount, Is.EqualTo(1));
            Assert.That(day.MealsEaten, Is.EqualTo(1));
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(day.SatisfactionPoints, Is.EqualTo(6),
                "At equal distance the dedicated workshop gets the work assignment.");
            var person = day.residents.Single();
            Assert.That(person.workTileId, Is.EqualTo(workshop.id));
            Assert.That(person.workStreetTiles, Is.Zero);
            Assert.That(person.mealStreetTiles, Is.Zero);
            Assert.That(person.residencePreferenceMet, Is.True);
            Assert.That((person.basePoints, person.workplaceBonus,
                person.mealBonus, person.homeBonus, person.routeBonus),
                Is.EqualTo((1, 2, 1, 1, 1)),
                "The local route and mature-green neighbour are independent rewards.");
            Assert.That((person.greenNeighbourBonus, person.shortStreetBonus),
                Is.EqualTo((1, 0)));
            Assert.That(day.residents.Single().routeTileIds,
                Is.EqualTo(new[] { home.id, workshop.id, home.id,
                    restaurant.id, home.id }));
        }

        [Test]
        public void RestaurantMealAddsOneMorePointThanSupermarketMealOnSameRoutes()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var workshop = Build(board, 2, 2, ConstructionCategory.Workshop);
            var restaurant = Build(board, 3, 1,
                ConstructionCategory.Restaurant);
            var supermarket = Build(board, 4, 2,
                ConstructionCategory.Supermarket);

            ConstructionResidentDayResult EatAt(ConstructionTileData meal) =>
                ConstructionHumanModel.SettleDay(1, board, dispatches: new[]
                {
                    new ConstructionResidentDispatch
                    {
                        homeTileId = home.id,
                        workTileId = workshop.id,
                        mealTileId = meal.id
                    }
                }).residents.Single();

            var restaurantDay = EatAt(restaurant);
            var supermarketDay = EatAt(supermarket);
            Assert.That(restaurantDay.completedCycle, Is.True);
            Assert.That(supermarketDay.completedCycle, Is.True);
            Assert.That(restaurantDay.basePoints, Is.EqualTo(1));
            Assert.That(supermarketDay.basePoints, Is.EqualTo(1),
                "A supermarket meal still completes the basic work-and-meal cycle.");
            Assert.That(restaurantDay.mealBonus, Is.EqualTo(1));
            Assert.That(supermarketDay.mealBonus, Is.Zero);
            Assert.That((restaurantDay.workStreetTiles,
                restaurantDay.mealStreetTiles), Is.EqualTo((0, 0)));
            Assert.That((supermarketDay.workStreetTiles,
                supermarketDay.mealStreetTiles), Is.EqualTo((0, 0)));
            Assert.That(restaurantDay.satisfactionPoints -
                supermarketDay.satisfactionPoints, Is.EqualTo(1));
        }

        [Test]
        public void WorkplaceBonusesRankWorkshopAboveRestaurantAboveSupermarket()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var workshop = Build(board, 2, 2,
                ConstructionCategory.Workshop);
            var restaurant = Build(board, 3, 1,
                ConstructionCategory.Restaurant);
            var supermarket = Build(board, 4, 2,
                ConstructionCategory.Supermarket);

            ConstructionResidentDayResult WorkAt(ConstructionTileData work) =>
                ConstructionHumanModel.SettleDay(1, board, dispatches: new[]
                {
                    new ConstructionResidentDispatch
                    {
                        homeTileId = home.id,
                        workTileId = work.id,
                        mealTileId = supermarket.id
                    }
                }).residents.Single();

            var workshopDay = WorkAt(workshop);
            var restaurantDay = WorkAt(restaurant);
            var supermarketDay = WorkAt(supermarket);
            Assert.That((workshopDay.workplaceBonus,
                    restaurantDay.workplaceBonus,
                    supermarketDay.workplaceBonus),
                Is.EqualTo((2, 1, 0)));
            Assert.That(workshopDay.satisfactionPoints,
                Is.GreaterThan(restaurantDay.satisfactionPoints));
            Assert.That(restaurantDay.satisfactionPoints,
                Is.GreaterThan(supermarketDay.satisfactionPoints));
        }

        [Test]
        public void SupermarketJobAndTwoMealSeatsHaveSeparateCapacities()
        {
            var board = new ConstructionBoardModel();
            var firstHome = Build(board, 3, 2, ConstructionCategory.Residence);
            var secondHome = Build(board, 4, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Street);
            var market = Build(board, 4, 1, ConstructionCategory.Supermarket);

            var firstDay = ConstructionHumanModel.SettleDay(1, board);
            var secondDay = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(ConstructionHumanModel.DailyJobCapacity(
                ConstructionCategory.Supermarket), Is.EqualTo(1));
            Assert.That(ConstructionHumanModel.DailyMealCapacity(
                ConstructionCategory.Supermarket), Is.EqualTo(2));
            Assert.That(firstDay.WorkedCount, Is.EqualTo(1));
            Assert.That(firstDay.MealsEaten, Is.EqualTo(2));
            Assert.That(firstDay.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(secondDay.WorkedCount, Is.EqualTo(1));
            Assert.That(secondDay.MealsEaten, Is.EqualTo(2));
            Assert.That(secondDay.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(firstDay.residents.Single(person =>
                person.homeTileId == secondHome.id).workTileId,
                Is.EqualTo(market.id));
            Assert.That(secondDay.residents.Single(person =>
                person.homeTileId == firstHome.id).workTileId,
                Is.EqualTo(market.id),
                "A green-neighbour score bonus must not monopolize the job.");
            Assert.That(firstDay.residents.All(person => person.ate), Is.True);
            Assert.That(secondDay.residents.All(person => person.ate), Is.True);
            Assert.That(firstDay.residents.Single(person =>
                person.homeTileId == secondHome.id).routeBonus,
                Is.EqualTo(1), "The direct local route still earns its point.");
        }

        [Test]
        public void TerraceAddsOneMealSeatAfterTheSquareIsBuilt()
        {
            var board = new ConstructionBoardModel();
            var firstHome = Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 4, 2, ConstructionCategory.Street);
            var secondHome = Build(board, 4, 1, ConstructionCategory.Residence);
            var thirdHome = Build(board, 4, 3, ConstructionCategory.Residence);
            var restaurant = Build(board, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Square,
                GreenPlanting.None, out _, out var failure, builtDay: 2),
                Is.True, failure.ToString());
            var dispatches = new[] { firstHome, secondHome, thirdHome }
                .Select(home => new ConstructionResidentDispatch
                {
                    homeTileId = home.id,
                    mealTileId = restaurant.id
                }).ToArray();

            var before = ConstructionHumanModel.SettleDay(1, board,
                useNeighborUpgrades: true);
            var legacy = ConstructionHumanModel.SettleDay(2, board);
            var terrace = ConstructionHumanModel.SettleDay(2, board,
                useNeighborUpgrades: true);
            var manuallyDispatched = ConstructionHumanModel.SettleDay(2, board,
                dispatches: dispatches, useNeighborUpgrades: true);

            Assert.That(before.MealsEaten, Is.EqualTo(2));
            Assert.That(before.restaurantRejectedForCapacity, Is.EqualTo(1));
            Assert.That(legacy.MealsEaten, Is.EqualTo(2));
            Assert.That(terrace.MealsEaten, Is.EqualTo(3));
            Assert.That(terrace.restaurantRejectedForCapacity, Is.Zero);
            Assert.That(terrace.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(manuallyDispatched.residents.Count(person =>
                person.mealTileId == restaurant.id), Is.EqualTo(3));
        }

        [Test]
        public void RestaurantTwoMealSeatsServeBothDispatchesRegardlessOfOrder()
        {
            var board = new ConstructionBoardModel();
            var firstHome = Build(board, 3, 2,
                ConstructionCategory.Residence);
            var restaurant = Build(board, 4, 2,
                ConstructionCategory.Restaurant);
            var secondHome = Build(board, 4, 1,
                ConstructionCategory.Residence);
            var market = Build(board, 3, 1,
                ConstructionCategory.Supermarket);
            var firstPlan = new ConstructionResidentDispatch
            {
                homeTileId = firstHome.id,
                mealTileId = restaurant.id
            };
            var secondPlan = new ConstructionResidentDispatch
            {
                homeTileId = secondHome.id,
                mealTileId = restaurant.id
            };

            var secondWins = ConstructionHumanModel.SettleDay(1, board,
                dispatches: new[] { firstPlan, secondPlan });
            Assert.That(secondWins.residents.Single(person =>
                person.homeTileId == secondHome.id).mealTileId,
                Is.EqualTo(restaurant.id));
            Assert.That(secondWins.residents.Single(person =>
                person.homeTileId == firstHome.id).mealTileId,
                Is.EqualTo(restaurant.id));
            Assert.That(secondWins.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(secondWins.restaurantDemand, Is.EqualTo(2));
            Assert.That(secondWins.restaurantServed, Is.EqualTo(2));
            Assert.That(secondWins.restaurantRejectedForCapacity,
                Is.Zero);

            firstPlan.mealTileId = market.id;
            var optedIntoMarket = ConstructionHumanModel.SettleDay(1, board,
                dispatches: new[] { firstPlan, secondPlan });
            Assert.That(optedIntoMarket.restaurantDemand, Is.EqualTo(1));
            Assert.That(optedIntoMarket.restaurantServed, Is.EqualTo(1));
            Assert.That(optedIntoMarket.restaurantRejectedForCapacity,
                Is.Zero,
                "A deliberate supermarket meal is not restaurant overflow.");
            firstPlan.mealTileId = restaurant.id;

            var firstWins = ConstructionHumanModel.SettleDay(1, board,
                dispatches: new[] { secondPlan, firstPlan });
            Assert.That(firstWins.residents.Single(person =>
                person.homeTileId == firstHome.id).mealTileId,
                Is.EqualTo(restaurant.id));
            Assert.That(firstWins.residents.Single(person =>
                person.homeTileId == secondHome.id).mealTileId,
                Is.EqualTo(restaurant.id));
            Assert.That(firstWins.restaurantServed, Is.EqualTo(2));
            Assert.That(firstWins.restaurantRejectedForCapacity, Is.Zero);

            var closed = ConstructionHumanModel.SettleDay(1, board,
                blockedFoodServices: new[] { restaurant.id },
                dispatches: new[] { secondPlan });
            Assert.That(closed.residents.Single(person =>
                person.homeTileId == secondHome.id).mealTileId,
                Is.EqualTo(market.id),
                "A temporarily closed preferred site falls back to an open one.");
        }

        [Test]
        public void ShortStreetTripCanOutscoreACompletelyLocalTrip()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 3, 1,
                ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Street);
            var workshop = Build(board, 1, 2,
                ConstructionCategory.Workshop);

            var person = ConstructionHumanModel.SettleDay(1, board)
                .residents.Single();
            Assert.That(person.workTileId, Is.EqualTo(workshop.id));
            Assert.That(person.mealTileId, Is.EqualTo(restaurant.id));
            Assert.That(person.workStreetTiles, Is.EqualTo(1));
            Assert.That(person.mealStreetTiles, Is.Zero);
            Assert.That(person.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(person.shortStreetBonus, Is.EqualTo(2));
            Assert.That(person.routeBonus, Is.Zero);
            Assert.That(person.satisfactionPoints, Is.EqualTo(7));
        }

        [Test]
        public void AnyHomeRewardsShortStreetRouteButNotLongOne()
        {
            var shortRoute = new ConstructionBoardModel();
            var home = Build(shortRoute, 3, 2,
                ConstructionCategory.Residence);
            Build(shortRoute, 4, 2, ConstructionCategory.Street);
            Build(shortRoute, 4, 1, ConstructionCategory.Restaurant);
            Build(shortRoute, 5, 2, ConstructionCategory.Workshop);
            var shortDay = ConstructionHumanModel.SettleDay(1, shortRoute);
            var shortPerson = shortDay.residents.Single();
            Assert.That(shortPerson.workStreetTiles, Is.EqualTo(1));
            Assert.That(shortPerson.mealStreetTiles, Is.EqualTo(1));
            Assert.That(shortPerson.residencePreferenceMet, Is.True);
            Assert.That(shortPerson.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(shortPerson.shortStreetBonus, Is.EqualTo(2));
            Assert.That(shortPerson.homeBonus, Is.EqualTo(3));
            Assert.That(shortPerson.routeBonus, Is.Zero);
            Assert.That(shortPerson.satisfactionPoints, Is.EqualTo(7));
            Assert.That(shortPerson.routeTileIds.First(), Is.EqualTo(home.id));
            Assert.That(shortPerson.routeTileIds.Last(), Is.EqualTo(home.id));

            var longRoute = new ConstructionBoardModel();
            Build(longRoute, 3, 2,
                ConstructionCategory.Residence, ResidenceKind.Urban);
            Build(longRoute, 4, 2, ConstructionCategory.Street);
            Build(longRoute, 5, 2, ConstructionCategory.Street);
            Build(longRoute, 6, 2, ConstructionCategory.Street);
            Build(longRoute, 6, 3, ConstructionCategory.Street);
            Build(longRoute, 6, 4, ConstructionCategory.Street);
            Build(longRoute, 6, 5, ConstructionCategory.Street);
            Build(longRoute, 6, 6, ConstructionCategory.Restaurant);
            Build(longRoute, 5, 5, ConstructionCategory.Workshop);
            var longPerson = ConstructionHumanModel.SettleDay(1, longRoute)
                .residents.Single();
            Assert.That(longPerson.workStreetTiles, Is.EqualTo(6));
            Assert.That(longPerson.mealStreetTiles, Is.EqualTo(6));
            Assert.That(longPerson.shortStreetBonus, Is.Zero);
            Assert.That(longPerson.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(longPerson.homeBonus, Is.EqualTo(1));
            Assert.That(longPerson.satisfactionPoints, Is.EqualTo(5));
        }

        [Test]
        public void GenericHomeUsesVegetationMaturityOnTheSettledDay()
        {
            var board = new ConstructionBoardModel();
            Build(board, 4, 3, ConstructionCategory.Green);
            Build(board, 5, 3, ConstructionCategory.Residence);
            Build(board, 5, 2, ConstructionCategory.Restaurant);
            Build(board, 6, 3, ConstructionCategory.Workshop);

            var young = ConstructionHumanModel.SettleDay(1, board)
                .residents.Single();
            var mature = ConstructionHumanModel.SettleDay(3, board)
                .residents.Single();
            Assert.That(young.completedCycle, Is.True);
            Assert.That(young.residencePreferenceMet, Is.True,
                "The local route is already an earned housing condition.");
            Assert.That(young.greenNeighbourBonus, Is.Zero);
            Assert.That(young.homeBonus, Is.Zero);
            Assert.That(young.routeBonus, Is.EqualTo(1));
            Assert.That(young.satisfactionPoints, Is.EqualTo(5));
            Assert.That(mature.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(mature.homeBonus, Is.EqualTo(1));
            Assert.That(mature.satisfactionPoints, Is.EqualTo(6));
        }

        [Test]
        public void DiagonalServicesNeedAnEdgeConnectedStreetRoute()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 4, 3, ConstructionCategory.Residence);
            Assert.That(board.CheckPlacement(5, 4,
                ConstructionCategory.Restaurant),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(5, 2,
                ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            var mealStreet = Build(board, 4, 4, ConstructionCategory.Street);
            var workStreet = Build(board, 4, 2, ConstructionCategory.Street);
            var restaurant = Build(board, 5, 4, ConstructionCategory.Restaurant);
            var workshop = Build(board, 5, 2, ConstructionCategory.Workshop);

            var person = ConstructionHumanModel.SettleDay(3, board)
                .residents.Single();
            Assert.That(person.completedCycle, Is.True);
            Assert.That(person.homeTileId, Is.EqualTo(home.id));
            Assert.That(person.mealTileId, Is.EqualTo(restaurant.id));
            Assert.That(person.workTileId, Is.EqualTo(workshop.id));
            Assert.That(person.mealStreetTiles, Is.EqualTo(1));
            Assert.That(person.workStreetTiles, Is.EqualTo(1));
            Assert.That(person.greenNeighbourBonus, Is.EqualTo(1),
                "The mature starter oak still benefits this home.");
            Assert.That(board.ResidenceServiceRouteTo(home.id, restaurant.id)
                .Select(tile => tile.id), Is.EqualTo(new[]
                { home.id, mealStreet.id, restaurant.id }));
            Assert.That(board.ResidenceServiceRouteTo(home.id, workshop.id)
                .Select(tile => tile.id), Is.EqualTo(new[]
                { home.id, workStreet.id, workshop.id }));
            Assert.That(person.routeTileIds, Does.Contain(restaurant.id));
            Assert.That(person.routeTileIds, Does.Contain(workshop.id));
        }

        [TestCase(ResidenceKind.Standard)]
        [TestCase(ResidenceKind.Garden)]
        [TestCase(ResidenceKind.Urban)]
        public void SavedResidenceKindDoesNotGateEarnedHousingBonuses(
            ResidenceKind savedKind)
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence, savedKind);
            Build(board, 4, 2, ConstructionCategory.Street);
            Build(board, 4, 1, ConstructionCategory.Restaurant);
            Build(board, 5, 2, ConstructionCategory.Workshop);
            Build(board, 4, 3, ConstructionCategory.Square);

            var person = ConstructionHumanModel.SettleDay(1, board)
                .residents.Single();
            Assert.That(person.completedCycle, Is.True);
            Assert.That(person.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(person.shortStreetBonus, Is.EqualTo(2));
            Assert.That(person.routeBonus, Is.Zero);
            Assert.That(person.homeBonus, Is.EqualTo(3));
            Assert.That(person.squareBonus, Is.EqualTo(1));
            Assert.That(person.satisfactionPoints, Is.EqualTo(8),
                "The three condition rewards keep a completed cycle within the score cap.");
        }

        [Test]
        public void WorkshopAndRestaurantCapacitiesSupportTwoResidents()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 4, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Street);
            Build(board, 4, 1, ConstructionCategory.Street);
            Build(board, 5, 1, ConstructionCategory.Restaurant);
            var workshop = Build(board, 2, 1,
                ConstructionCategory.Workshop);

            var first = ConstructionHumanModel.SettleDay(1, board);
            var second = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(ConstructionHumanModel.DailyJobCapacity(
                ConstructionCategory.Workshop), Is.EqualTo(2));
            Assert.That(ConstructionHumanModel.DailyJobCapacity(
                ConstructionCategory.Restaurant), Is.EqualTo(1));
            Assert.That(ConstructionHumanModel.DailyMealCapacity(
                ConstructionCategory.Restaurant), Is.EqualTo(2));
            Assert.That(first.ResidentCount, Is.EqualTo(2));
            Assert.That(first.WorkedCount, Is.EqualTo(2));
            Assert.That(first.MealsEaten, Is.EqualTo(2));
            Assert.That(first.restaurantDemand, Is.EqualTo(2));
            Assert.That(first.restaurantServed, Is.EqualTo(2));
            Assert.That(first.restaurantRejectedForCapacity, Is.Zero);
            Assert.That(first.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(first.residents.All(person =>
                person.workTileId == workshop.id), Is.True);
            Assert.That(second.WorkedCount, Is.EqualTo(2));
            Assert.That(second.MealsEaten, Is.EqualTo(2));
            Assert.That(second.CompletedWorkCycles, Is.EqualTo(2));
        }

        [Test]
        public void ScarceRestaurantJobGoesToTheHomeWithNoMarketRoute()
        {
            var board = new ConstructionBoardModel();
            var flexibleHome = Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 2, 2,
                ConstructionCategory.Restaurant);
            var constrainedHome = Build(board, 2, 1,
                ConstructionCategory.Residence);
            var market = Build(board, 4, 2,
                ConstructionCategory.Supermarket);

            var day = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(2),
                "A flexible household must leave the only restaurant job to its neighbour.");
            var flexible = day.residents.Single(person =>
                person.homeTileId == flexibleHome.id);
            var constrained = day.residents.Single(person =>
                person.homeTileId == constrainedHome.id);
            Assert.That(flexible.workTileId, Is.EqualTo(market.id));
            Assert.That(flexible.mealTileId, Is.EqualTo(restaurant.id));
            Assert.That((constrained.workTileId, constrained.mealTileId),
                Is.EqualTo((restaurant.id, restaurant.id)));
            Assert.That(day.restaurantServed, Is.EqualTo(2));
            Assert.That(day.SatisfactionPoints, Is.EqualTo(8),
                "The home beside the starter oak earns a mature-green point.");
        }

        [Test]
        public void SharedJobCannotCombineDifferentHomesMealRoutes()
        {
            var board = new ConstructionBoardModel();
            var marketHome = Build(board, 3, 2,
                ConstructionCategory.Residence);
            var restaurantHome = Build(board, 2, 2,
                ConstructionCategory.Residence);
            var workshop = Build(board, 3, 1,
                ConstructionCategory.Workshop);
            Build(board, 2, 1, ConstructionCategory.Street);
            var market = Build(board, 4, 2,
                ConstructionCategory.Supermarket);
            var restaurant = Build(board, 1, 2,
                ConstructionCategory.Restaurant);

            Assert.That(board.ResidenceServiceRouteTo(marketHome.id,
                restaurant.id).Count, Is.Zero);
            Assert.That(board.ResidenceServiceRouteTo(restaurantHome.id,
                market.id).Count, Is.Zero);
            var day = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(2));
            Assert.That(day.residents.Single(person =>
                person.homeTileId == marketHome.id).mealTileId,
                Is.EqualTo(market.id));
            Assert.That(day.residents.Single(person =>
                person.homeTileId == restaurantHome.id).mealTileId,
                Is.EqualTo(restaurant.id));
            Assert.That(day.residents.Count(person =>
                person.workTileId == workshop.id), Is.EqualTo(2));
            foreach (var person in day.residents)
            {
                Assert.That(board.ResidenceServiceRouteTo(person.homeTileId,
                    person.workTileId).Count, Is.GreaterThan(0));
                Assert.That(board.ResidenceServiceRouteTo(person.homeTileId,
                    person.mealTileId).Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void GenericResidentChoosesNearbyMarketOverDistantRestaurant()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2,
                ConstructionCategory.Residence);
            Build(board, 4, 2, ConstructionCategory.Street);
            var workshop = Build(board, 2, 2,
                ConstructionCategory.Workshop);
            Build(board, 5, 2, ConstructionCategory.Street);
            var market = Build(board, 5, 3,
                ConstructionCategory.Supermarket);
            Build(board, 6, 2, ConstructionCategory.Street);
            var restaurant = Build(board, 6, 3,
                ConstructionCategory.Restaurant);

            Assert.That(board.ResidenceServiceRouteTo(home.id,
                restaurant.id).Count(tile =>
                    tile.category == ConstructionCategory.Street), Is.EqualTo(3));
            var person = ConstructionHumanModel.SettleDay(1, board)
                .residents.Single();
            Assert.That(person.workTileId, Is.EqualTo(workshop.id));
            Assert.That(person.mealTileId, Is.EqualTo(market.id));
            Assert.That(person.residencePreferenceMet, Is.True);
            Assert.That(person.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(person.shortStreetBonus, Is.EqualTo(2));
            Assert.That(person.satisfactionPoints, Is.EqualTo(6));
        }

        [Test]
        public void OneBuildPerSimulatedDayCompletesTheEightRoomIntroduction()
        {
            var run = CompleteLinearIntroduction();
            Assert.That(run.IntroTypesBuilt, Is.EqualTo(8));
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.IsUnlocked(ConstructionCategory.Green), Is.True,
                "All room types reopen after their first introduction.");
            Assert.That(run.BuiltCount, Is.EqualTo(9));
            Assert.That(run.BuiltTiles.Skip(1).Select(tile => tile.builtDay)
                .Distinct().Count(), Is.EqualTo(run.BuiltCount - 1));
        }

        [Test]
        public void DispatchCanBeSwappedWithoutUsingTheDailyBuildAndSurvivesSave()
        {
            var run = CompleteLinearIntroduction();
            var home = run.At(3, 2);
            var workshop = run.At(2, 2);
            var restaurant = run.At(3, 1);
            var market = run.At(5, 2);
            Assert.That(run.DispatchWorkOptions(home.id).Select(tile => tile.id),
                Does.Contain(workshop.id));
            Assert.That(run.DispatchMealOptions(home.id).Select(tile => tile.id),
                Does.Contain(market.id));

            var day = run.CurrentDay;
            for (var index = 0; index < 12; index++)
                Assert.That(run.TrySetResidentDispatch(home.id, workshop.id,
                    index % 2 == 0 ? restaurant.id : market.id), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(day));
            Assert.That(run.BuiltToday, Is.False);
            Assert.That(run.CanAdvanceFillDay, Is.False,
                "Dispatch does not replace the daily room placement.");
            Assert.That(run.GetResidentDispatch(home.id).mealTileId,
                Is.EqualTo(market.id));
            var preview = run.PreviewHumanDay().residents.Single(person =>
                person.homeTileId == home.id);
            Assert.That(preview.workTileId, Is.EqualTo(workshop.id));
            Assert.That(preview.mealTileId, Is.EqualTo(market.id));

            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.GetResidentDispatch(home.id).mealTileId,
                Is.EqualTo(market.id));
            Assert.That(restored.PreviewHumanDay().residents.Single(person =>
                person.homeTileId == home.id).mealTileId,
                Is.EqualTo(market.id));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.LastHumanDay.residents.Single(person =>
                person.homeTileId == home.id).mealTileId,
                Is.EqualTo(preview.mealTileId));
            Assert.That(restored.GetResidentDispatch(home.id).mealTileId,
                Is.EqualTo(market.id),
                "The selected destination remains set for the next day.");
            Assert.That(restored.TrySetResidentDispatch(home.id, null, null),
                Is.True);
            Assert.That(restored.GetResidentDispatch(home.id), Is.Null);

            var legacy = run.Export();
            legacy.residentDispatches = null;
            Assert.That(ConstructionRunModel.TryRestore(legacy,
                out var noDispatchSave), Is.True);
            Assert.That(noDispatchSave.GetResidentDispatch(home.id), Is.Null);
        }

        [Test]
        public void DispatchRejectsWrongOrUnreachableTargetsWithoutChangingPlan()
        {
            var run = CompleteLinearIntroduction();
            var home = run.At(3, 2);
            var workshop = run.At(2, 2);
            var market = run.At(5, 2);
            Assert.That(run.TrySetResidentDispatch(home.id, workshop.id,
                market.id), Is.True);
            Assert.That(run.TrySetResidentDispatch(home.id, home.id,
                market.id), Is.False);
            Assert.That(run.TrySetResidentDispatch(home.id, workshop.id,
                "missing-tile"), Is.False);
            Assert.That(run.TrySetResidentDispatch("missing-home", null,
                market.id), Is.False);
            Assert.That(run.GetResidentDispatch(home.id).workTileId,
                Is.EqualTo(workshop.id));
            Assert.That(run.GetResidentDispatch(home.id).mealTileId,
                Is.EqualTo(market.id));

            var corrupted = run.Export();
            corrupted.residentDispatches[0].mealTileId = home.id;
            Assert.That(ConstructionRunModel.TryRestore(corrupted, out _),
                Is.False);
        }

        [Test]
        public void MarketMealDispatchProducesReachableWildlifeScrapsInForecastAndDay()
        {
            var run = CompleteLinearIntroduction();
            var home = run.At(3, 2);
            var market = run.At(5, 2);
            Place(run, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            var saved = run.Export();
            saved.food.tiles.Single(tile =>
                tile.tileId == run.At(2, 3).id).stock = 0;
            Assert.That(ConstructionRunModel.TryRestore(saved, out run),
                Is.True);
            Assert.That(run.TrySetResidentDispatch(home.id, null,
                market.id), Is.True);
            Assert.That(run.PreviewHumanDay().residents.Single(person =>
                person.homeTileId == home.id).mealTileId,
                Is.EqualTo(market.id));
            Assert.That(run.PreviewHumanDay().restaurantDemand, Is.Zero);
            var forecast = run.PreviewEcologyDay();
            Assert.That(forecast.urbanMealTileIds,
                Does.Contain(market.id),
                "A hungry pigeon can use market scraps through the new green link.");
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.urbanMealTileIds,
                Is.EquivalentTo(forecast.urbanMealTileIds));
            Assert.That(actual.animalPoints,
                Is.EqualTo(forecast.animalPoints));
            Assert.That(run.LastHumanDay.residents.Single(person =>
                person.homeTileId == home.id).mealTileId,
                Is.EqualTo(market.id));
            Assert.That(run.LastHumanDay.restaurantRejectedForCapacity,
                Is.Zero);
        }

        private static ConstructionRunModel CompleteLinearIntroduction()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            PlaceAndSimulate(run, 2, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            PlaceAndSimulate(run, 3, 2, ConstructionCategory.Residence);
            PlaceAndSimulate(run, 3, 1, ConstructionCategory.Restaurant);
            PlaceAndSimulate(run, 2, 2, ConstructionCategory.Workshop);
            PlaceAndSimulate(run, 4, 2, ConstructionCategory.Street);
            PlaceAndSimulate(run, 2, 1, ConstructionCategory.Waste);
            PlaceAndSimulate(run, 5, 2, ConstructionCategory.Supermarket);
            PlaceAndSimulate(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));
            return run;
        }

        [Test]
        public void SquareVisitForecastSettlementAndSaveAgreeOnTemporaryFood()
        {
            var run = CompleteLinearIntroduction();
            var square = run.At(4, 3);
            var saved = run.Export();
            saved.food.tiles.Single(tile => tile.tileId == run.At(2, 3).id)
                .stock = 0;
            Assert.That(ConstructionRunModel.TryRestore(saved, out run), Is.True);

            var humanPreview = run.PreviewHumanDay();
            var animalPreview = run.PreviewEcologyDay();
            Assert.That(humanPreview.SquareVisits, Is.EqualTo(1));
            Assert.That(animalPreview.urbanMealTileIds,
                Does.Contain(square.id));
            Assert.That(run.GreenFoodRouteTo(run.PigeonTileId, square.id)
                .Select(tile => tile.id).Last(), Is.EqualTo(square.id));
            Assert.That(run.TrySimulateDay(out var settled), Is.True);
            Assert.That(run.LastHumanDay.SquareVisits,
                Is.EqualTo(humanPreview.SquareVisits));
            Assert.That(settled.urbanMealTileIds,
                Is.EqualTo(animalPreview.urbanMealTileIds));
            Assert.That(run.BalanceScore,
                Is.EqualTo(run.AnimalScore + run.HumanScore));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.LastHumanDay.SquareVisits, Is.EqualTo(1));
            Assert.That(restored.LastEcologyDay.urbanMealTileIds,
                Does.Contain(square.id));
        }

        [Test]
        public void ContinuedVersionTwoSaveCanScoreSquareVisitWithoutStalling()
        {
            var previous = CompleteLinearIntroduction().Export();
            previous.score.scoringVersion = 2;
            foreach (var day in previous.score.days)
            {
                // Fabricate a valid historical V2 ledger rather than only
                // changing its version number while leaving V7 area awards.
                day.animalPoints = System.Math.Min(day.animalPoints,
                    3 * day.animalMeals);
                day.humanPoints = day.completedWorkCycles;
                day.balancePoints = System.Math.Min(day.animalPoints,
                    day.humanPoints);
                day.trafficPenaltyPoints = 0;
                day.trafficPenaltyRulesApplied = false;
                day.linkageRulesApplied = false;
                day.sharedSquareMeals = 0;
                day.sharedSupermarketMeals = 0;
                day.sharedGreenMeals = 0;
                day.biodiversityHomes = 0;
                day.linkagePoints = 0;
            }
            previous.lastEcologyDay = null;
            previous.lastHumanDay = null;
            previous.lastWasteDay = null;
            previous.lastForecast = null;
            Assert.That(ConstructionRunModel.TryRestore(previous,
                out var continued), Is.True);
            Assert.That(continued.At(4, 3).category,
                Is.EqualTo(ConstructionCategory.Square));
            Assert.That(continued.PreviewHumanDay().SquareVisits,
                Is.EqualTo(1));
            Assert.That(continued.TrySimulateDay(out _), Is.True);
            Assert.That(continued.HasAdditiveCoexistenceScore, Is.False);
            var latest = continued.Export().score.days.Last();
            Assert.That(latest.humanPoints,
                Is.EqualTo(continued.LastHumanDay.SatisfactionPoints));
            Assert.That(latest.balancePoints,
                Is.EqualTo(System.Math.Min(latest.animalPoints,
                    latest.humanPoints)));
        }

        private static void PlaceAndSimulate(ConstructionRunModel run, int column,
            int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Place(run, column, row, category, planting);
            Assert.That(run.TrySimulateDay(out _), Is.True);
        }

        [Test]
        public void MixedFortyNineCellStrategiesExposeDailyRateTradeoffs()
        {
            var greenFirst = FinishMixedRun(false, 0);
            var servicesFirst = FinishMixedRun(true, 0);
            var greenAfterWait = FinishMixedRun(false, 20);
            foreach (var run in new[] { greenFirst, servicesFirst, greenAfterWait })
            {
                Assert.That(run.IsFinished, Is.True);
                Assert.That(run.BuiltCount, Is.EqualTo(49));
                Assert.That(run.FinalResult.DaysPlayed,
                    Is.EqualTo(run.CurrentDay));
                Assert.That(run.BuiltTiles.Skip(1).Select(tile => tile.builtDay)
                    .Distinct().Count(), Is.EqualTo(48));
            }
            Assert.That(greenFirst.FinalResult.DaysPlayed, Is.EqualTo(49));
            Assert.That(servicesFirst.FinalResult.DaysPlayed, Is.EqualTo(49));
            Assert.That(greenAfterWait.FinalResult.DaysPlayed, Is.EqualTo(69));
            Assert.That(greenFirst.FinalResult.AnimalMealsPerDay,
                Is.GreaterThan(servicesFirst.FinalResult.AnimalMealsPerDay));
            Assert.That(servicesFirst.FinalResult.HumanWorkdaysPerDay,
                Is.GreaterThan(greenFirst.FinalResult.HumanWorkdaysPerDay));
            Assert.That(greenAfterWait.FinalResult.AnimalMealsTotal,
                Is.GreaterThan(greenFirst.FinalResult.AnimalMealsTotal));
            Assert.That(greenAfterWait.FinalResult.AnimalMealsPerDay,
                Is.LessThan(greenFirst.FinalResult.AnimalMealsPerDay));
            TestContext.WriteLine($"green first: animal {greenFirst.FinalResult.AnimalMealsPerDay:F3} " +
                $"({greenFirst.FinalResult.AnimalMealsTotal}), human " +
                $"{greenFirst.FinalResult.HumanWorkdaysPerDay:F3} " +
                $"({greenFirst.FinalResult.HumanWorkdaysTotal})");
            TestContext.WriteLine($"services first: animal {servicesFirst.FinalResult.AnimalMealsPerDay:F3} " +
                $"({servicesFirst.FinalResult.AnimalMealsTotal}), human " +
                $"{servicesFirst.FinalResult.HumanWorkdaysPerDay:F3} " +
                $"({servicesFirst.FinalResult.HumanWorkdaysTotal})");
            TestContext.WriteLine($"green after 20 idle days: animal " +
                $"{greenAfterWait.FinalResult.AnimalMealsPerDay:F3} " +
                $"({greenAfterWait.FinalResult.AnimalMealsTotal}), human " +
                $"{greenAfterWait.FinalResult.HumanWorkdaysPerDay:F3} " +
                $"({greenAfterWait.FinalResult.HumanWorkdaysTotal})");
        }

        private static ConstructionRunModel FinishMixedRun(bool addHumanServices,
            int idleDays)
        {
            var run = CompleteLinearIntroduction();
            PlaceAndSimulate(run, 5, 1, ConstructionCategory.Residence);
            PlaceAndSimulate(run, 4, 1, ConstructionCategory.Street);
            for (var day = 0; day < idleDays; day++)
                Assert.That(run.TrySimulateDay(out _), Is.True);
            if (addHumanServices)
            {
                // The eastern household gains a nearby dining room and waste
                // collection. Without them its distant market accumulates
                // waste, making the workday rate diverge over time.
                PlaceAndSimulate(run, 6, 1, ConstructionCategory.Restaurant);
                PlaceAndSimulate(run, 5, 0, ConstructionCategory.Waste);
            }
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(position => System.Math.Abs(position.column - 3) +
                    System.Math.Abs(position.row - 3));
            foreach (var position in positions)
                if (run.At(position.column, position.row) == null)
                {
                    var category = run.CategoryBuiltCount(ConstructionCategory.Green) < 21
                        ? ConstructionCategory.Green
                        : run.CategoryBuiltCount(ConstructionCategory.Residence) < 12
                            ? ConstructionCategory.Residence : ConstructionCategory.Square;
                    PlaceAndSimulate(run, position.column, position.row,
                        category, category == ConstructionCategory.Green
                            ? GreenPlanting.Meadow : GreenPlanting.None);
                }
            return run;
        }

        [Test]
        public void FullRestaurantIntroducesTheAlreadyBuiltMarketWithoutRejectingAMeal()
        {
            var run = CompleteLinearIntroduction();
            Assert.That(run.FirstResidentAte, Is.True);
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Supermarket),
                Is.EqualTo(1), "The linear tutorial has already introduced the market.");
            Assert.That(run.LastHumanDay.restaurantRejectedForCapacity, Is.Zero);
            Place(run, 5, 1, ConstructionCategory.Residence);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildStreet),
                "The new household has no street route to the restaurant yet.");
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Place(run, 4, 1, ConstructionCategory.Street);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(run.LastHumanDay, Is.Null,
                "A route preview must not count as an observed meal rejection.");
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastHumanDay.restaurantDemand, Is.EqualTo(2));
            Assert.That(run.LastHumanDay.restaurantServed, Is.EqualTo(2));
            Assert.That(run.LastHumanDay.restaurantRejectedForCapacity, Is.Zero);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.FreeBuild));
            Assert.That(run.LastHumanDay.SatisfactionPoints, Is.GreaterThan(0));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.LastHumanDay.restaurantServed, Is.EqualTo(2));
            Assert.That(restored.LastHumanDay.restaurantRejectedForCapacity,
                Is.Zero);
            Assert.That(run.LastHumanDay.MealsEaten, Is.EqualTo(2),
                "The restaurant's two seats can serve both residents.");
            Assert.That(run.LastHumanDay.CompletedWorkCycles, Is.EqualTo(2));
        }

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category,
            ResidenceKind residenceKind = ResidenceKind.Standard)
        {
            var planting = category == ConstructionCategory.Green
                ? GreenPlanting.Meadow : GreenPlanting.None;
            Assert.That(board.TryBuild(column, row, category, planting,
                out var built, out var failure,
                residenceKind: residenceKind), Is.True, failure.ToString());
            return built;
        }

        private static void Place(ConstructionRunModel run, int column, int row,
            ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(run.TryBuild(column, row, category, planting,
                out _, out var failure, out var placement), Is.True,
                $"{failure}: {placement}");
        }
    }
}
