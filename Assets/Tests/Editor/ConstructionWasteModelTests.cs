using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionWasteModelTests
    {
        [Test]
        public void HomesAndServedMealsMakeWasteAndTwoDailyPortionsAreCleared()
        {
            var board = BasicBoard(out var home, out var restaurant);
            var waste = new ConstructionWasteModel();
            for (var day = 1; day <= 2; day++)
            {
                var availability = waste.PreviewAfterCleanup(board);
                var human = ConstructionHumanModel.SettleDay(day, board,
                    availability.BlockedHomes, availability.BlockedFoodServices);
                var result = waste.SettleDay(day, board, human, availability);
                Assert.That(result.Produced, Is.EqualTo(2));
                Assert.That(result.Cleared, Is.Zero);
            }
            Assert.That(waste.Backlog(home.id), Is.EqualTo(2));
            Assert.That(waste.Backlog(restaurant.id), Is.EqualTo(2));

            Build(board, 2, 1, ConstructionCategory.Waste);
            var cleaned = waste.PreviewAfterCleanup(board);
            Assert.That(cleaned.Cleared.Values.Sum(), Is.EqualTo(2));
            Assert.That(cleaned.BacklogAfterCleanup[home.id], Is.EqualTo(1));
            Assert.That(cleaned.BacklogAfterCleanup[restaurant.id], Is.EqualTo(1));
            var third = ConstructionHumanModel.SettleDay(3, board,
                cleaned.BlockedHomes, cleaned.BlockedFoodServices);
            Assert.That(third.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(waste.SettleDay(3, board, third, cleaned).Remaining,
                Is.EqualTo(4));
        }

        [Test]
        public void BacklogDisruptsJourneyAndTwoNewRoomsCanRecoverIt()
        {
            var board = BasicBoard(out var home, out var restaurant);
            var waste = new ConstructionWasteModel();
            for (var day = 1; day <= 3; day++)
            {
                var availability = waste.PreviewAfterCleanup(board);
                var human = ConstructionHumanModel.SettleDay(day, board,
                    availability.BlockedHomes, availability.BlockedFoodServices);
                waste.SettleDay(day, board, human, availability);
            }
            var blocked = waste.PreviewAfterCleanup(board);
            Assert.That(blocked.BlockedHomes, Does.Contain(home.id));
            Assert.That(blocked.BlockedFoodServices, Does.Contain(restaurant.id));
            Assert.That(ConstructionHumanModel.SettleDay(4, board,
                blocked.BlockedHomes, blocked.BlockedFoodServices)
                .CompletedWorkCycles, Is.Zero);

            Build(board, 2, 1, ConstructionCategory.Waste);
            Build(board, 4, 2, ConstructionCategory.Waste);
            var recovered = waste.PreviewAfterCleanup(board);
            Assert.That(recovered.BlockedHomes, Is.Empty);
            Assert.That(recovered.BlockedFoodServices, Is.Empty);
            Assert.That(ConstructionHumanModel.SettleDay(4, board,
                recovered.BlockedHomes, recovered.BlockedFoodServices)
                .CompletedWorkCycles, Is.EqualTo(1));
        }

        [Test]
        public void SaveRestoresBacklogAndRejectsUnknownSource()
        {
            var board = BasicBoard(out var home, out _);
            var waste = new ConstructionWasteModel();
            var availability = waste.PreviewAfterCleanup(board);
            var human = ConstructionHumanModel.SettleDay(1, board);
            waste.SettleDay(1, board, human, availability);
            var saved = waste.Export();
            Assert.That(ConstructionWasteModel.TryRestore(saved, board, 2,
                false, out var restored), Is.True);
            Assert.That(restored.Backlog(home.id), Is.EqualTo(1));
            saved.tiles[0].tileId = "not-built";
            Assert.That(ConstructionWasteModel.TryRestore(saved, board, 2,
                false, out _), Is.False);
        }

        [Test]
        public void RunPreviewAndDailyScoreUseSameCleanupResultAfterReload()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var home = Place(run, 3, 2, ConstructionCategory.Residence);
            var restaurant = Place(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 2, 2, ConstructionCategory.Workshop);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.WasteBacklog(home.id), Is.EqualTo(3));
            Assert.That(run.WasteBacklog(restaurant.id), Is.EqualTo(3));
            Assert.That(run.PreviewHumanDay().CompletedWorkCycles, Is.Zero);

            Place(run, 2, 1, ConstructionCategory.Waste);
            Place(run, 4, 2, ConstructionCategory.Waste);
            var expected = run.PreviewHumanDay();
            Assert.That(expected.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PreviewHumanDay().CompletedWorkCycles,
                Is.EqualTo(expected.CompletedWorkCycles));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.LastHumanDay.CompletedWorkCycles,
                Is.EqualTo(expected.CompletedWorkCycles));
            Assert.That(restored.LastWasteDay.Cleared, Is.EqualTo(4));
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var again), Is.True);
            Assert.That(again.LastWasteDay.Remaining, Is.EqualTo(4));
        }

        private static ConstructionBoardModel BasicBoard(
            out ConstructionTileData home, out ConstructionTileData restaurant)
        {
            var board = new ConstructionBoardModel();
            home = Build(board, 3, 2, ConstructionCategory.Residence);
            restaurant = Build(board, 3, 1, ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Workshop);
            return board;
        }

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(board.TryBuild(column, row, category, GreenPlanting.None,
                out var tile, out var failure), Is.True, failure.ToString());
            return tile;
        }

        private static ConstructionTileData Place(ConstructionRunModel run,
            int column, int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(run.TryBuild(column, row, category, planting,
                out var tile, out var failure, out var placement), Is.True,
                $"{failure}: {placement}");
            return tile;
        }
    }
}
