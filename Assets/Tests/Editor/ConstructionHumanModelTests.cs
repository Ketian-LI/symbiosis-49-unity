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
        public void OneResidentCompletesOnlyWhenWorkAndMealAreBothReachable()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var restaurant = Build(board, 3, 1, ConstructionCategory.Restaurant);
            var withoutWork = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(withoutWork.MealsEaten, Is.EqualTo(1));
            Assert.That(withoutWork.CompletedWorkCycles, Is.Zero);

            var workshop = Build(board, 2, 2, ConstructionCategory.Workshop);
            var day = ConstructionHumanModel.SettleDay(1, board);
            Assert.That(day.ResidentCount, Is.EqualTo(1));
            Assert.That(day.WorkedCount, Is.EqualTo(1));
            Assert.That(day.MealsEaten, Is.EqualTo(1));
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(day.residents.Single().routeTileIds,
                Is.EqualTo(new[] { home.id, workshop.id, home.id,
                    restaurant.id, home.id }));
        }

        [Test]
        public void CapacityRotatesFairlyAndExtraServicesResolveTheShortage()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 4, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Street);
            Build(board, 4, 1, ConstructionCategory.Street);
            Build(board, 5, 1, ConstructionCategory.Restaurant);
            Build(board, 2, 1, ConstructionCategory.Workshop);

            var first = ConstructionHumanModel.SettleDay(1, board);
            var second = ConstructionHumanModel.SettleDay(2, board);
            Assert.That(first.ResidentCount, Is.EqualTo(2));
            Assert.That(first.restaurantDemand, Is.EqualTo(2));
            Assert.That(first.restaurantServed, Is.EqualTo(1));
            Assert.That(first.restaurantRejectedForCapacity, Is.EqualTo(1));
            Assert.That(first.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(first.residents.Single(person => person.completedCycle).homeTileId,
                Is.Not.EqualTo(second.residents.Single(person =>
                    person.completedCycle).homeTileId));

            Build(board, 4, 0, ConstructionCategory.Supermarket);
            Build(board, 3, 0, ConstructionCategory.Workshop);
            var withCapacity = ConstructionHumanModel.SettleDay(3, board);
            Assert.That(withCapacity.WorkedCount, Is.EqualTo(2));
            Assert.That(withCapacity.MealsEaten, Is.EqualTo(2));
            Assert.That(withCapacity.CompletedWorkCycles, Is.EqualTo(2));
        }

        [Test]
        public void CrowdingUnlocksSupermarketOnlyAfterAnObservedRejectedMeal()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 3, 2, ConstructionCategory.Residence);
            Place(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.FirstResidentAte, Is.True);
            Place(run, 2, 2, ConstructionCategory.Workshop);
            Place(run, 2, 1, ConstructionCategory.Waste);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.AwaitAccessProblem));
            Place(run, 5, 3, ConstructionCategory.Residence);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildStreet),
                "A new household without work or food access reveals the street need.");
            Place(run, 4, 2, ConstructionCategory.Street);
            Place(run, 5, 2, ConstructionCategory.Street);
            Place(run, 4, 1, ConstructionCategory.Street);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(run.IsUnlocked(ConstructionCategory.Supermarket), Is.False);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastHumanDay.restaurantDemand, Is.EqualTo(2));
            Assert.That(run.LastHumanDay.restaurantRejectedForCapacity,
                Is.EqualTo(1));
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildSupermarket));
            Assert.That(run.HumanScore, Is.EqualTo(1));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.LastHumanDay.CompletedWorkCycles, Is.EqualTo(1));

            Place(run, 5, 1, ConstructionCategory.Supermarket);
            Assert.That(run.StoryStage, Is.EqualTo(ConstructionStoryStage.FreeBuild));
            var preview = run.PreviewHumanDay();
            Assert.That(preview.MealsEaten, Is.EqualTo(2));
            Assert.That(preview.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastHumanDay.MealsEaten, Is.EqualTo(2),
                "The market adds dining capacity but does not invent jobs.");
            Assert.That(run.LastHumanDay.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(run.HumanScore, Is.EqualTo(2));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var withMarketRestored), Is.True);
            Assert.That(withMarketRestored.LastHumanDay.MealsEaten, Is.EqualTo(2));
        }

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(board.TryBuild(column, row, category, GreenPlanting.None,
                out var built, out var failure), Is.True, failure.ToString());
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
