using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionWasteModelTests
    {
        [Test]
        public void CleanupReachesOneDiagonalStepButNotTwoRowsAway()
        {
            var board = BasicBoard(out var diagonalHome, out var restaurant);
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out _, out var bridgeFailure),
                Is.True, bridgeFailure.ToString());
            Build(board, 4, 3, ConstructionCategory.Residence);
            var distantHome = Build(board, 5, 3,
                ConstructionCategory.Residence);
            var waste = new ConstructionWasteModel();
            var initial = waste.PreviewAfterCleanup(board);
            var human = ConstructionHumanModel.SettleDay(1, board,
                initial.BlockedHomes, initial.BlockedFoodServices);
            waste.SettleDay(1, board, human, initial);
            Assert.That(waste.Backlog(diagonalHome.id), Is.EqualTo(1));
            Assert.That(waste.Backlog(restaurant.id), Is.EqualTo(1));
            Assert.That(waste.Backlog(distantHome.id), Is.EqualTo(1));

            // (4,1) touches (3,2) at a corner. (5,3) is two rows away.
            Build(board, 4, 1, ConstructionCategory.Waste);
            var cleaned = waste.PreviewAfterCleanup(board);
            Assert.That(cleaned.Cleared[restaurant.id], Is.EqualTo(1));
            Assert.That(cleaned.Cleared[diagonalHome.id], Is.EqualTo(1));
            Assert.That(cleaned.Cleared[distantHome.id], Is.Zero);
        }

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
            Assert.That(cleaned.CollectedByRoom.Values.Sum(), Is.EqualTo(2));
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
        public void DigitalHandWasteRoomClearsAllNearbyWithoutShutdownThreshold()
        {
            var board = BasicBoard(out var home, out var restaurant);
            var waste = new ConstructionWasteModel();
            for (var day = 1; day <= 4; day++)
            {
                var availability = waste.PreviewAfterCleanup(board,
                    clearAllNearby: true);
                Assert.That(availability.BlockedHomes, Is.Empty);
                Assert.That(availability.BlockedFoodServices, Is.Empty);
                var human = ConstructionHumanModel.SettleDay(day, board,
                    availability.BlockedHomes, availability.BlockedFoodServices);
                Assert.That(human.CompletedWorkCycles, Is.EqualTo(1));
                waste.SettleDay(day, board, human, availability,
                    clearAllNearby: true);
            }
            Assert.That(waste.Backlog(home.id), Is.EqualTo(4));
            Assert.That(waste.Backlog(restaurant.id), Is.EqualTo(4));

            var wasteRoom = Build(board, 2, 1, ConstructionCategory.Waste);
            var cleaned = waste.PreviewAfterCleanup(board,
                clearAllNearby: true);
            Assert.That(cleaned.Cleared.Values.Sum(), Is.EqualTo(8));
            Assert.That(cleaned.CollectedByRoom[wasteRoom.id], Is.EqualTo(8));
            Assert.That(cleaned.BacklogAfterCleanup[home.id], Is.Zero);
            Assert.That(cleaned.BacklogAfterCleanup[restaurant.id], Is.Zero);
            var fifth = ConstructionHumanModel.SettleDay(5, board,
                cleaned.BlockedHomes, cleaned.BlockedFoodServices);
            var result = waste.SettleDay(5, board, fifth, cleaned,
                clearAllNearby: true);
            Assert.That(result.Cleared, Is.EqualTo(10),
                "The same room also handles today's new waste.");
            Assert.That(result.Remaining, Is.Zero);
            Assert.That(waste.Backlog(home.id), Is.Zero);
            Assert.That(waste.Backlog(restaurant.id), Is.Zero);
        }

        [Test]
        public void ConnectedStreetsExtendWasteCollectionBeyondSurroundingPlots()
        {
            var board = new ConstructionBoardModel();
            var connectedHome = Build(board, 4, 3,
                ConstructionCategory.Residence);
            var unconnectedHome = Build(board, 5, 3,
                ConstructionCategory.Residence);
            var waste = new ConstructionWasteModel();
            var first = waste.PreviewAfterCleanup(board, clearAllNearby: true);
            var firstHuman = ConstructionHumanModel.SettleDay(1, board,
                first.BlockedHomes, first.BlockedFoodServices);
            waste.SettleDay(1, board, firstHuman, first,
                clearAllNearby: true);
            Assert.That(waste.Backlog(connectedHome.id), Is.EqualTo(1));
            Assert.That(waste.Backlog(unconnectedHome.id), Is.EqualTo(1));

            Build(board, 4, 2, ConstructionCategory.Street);
            Build(board, 4, 1, ConstructionCategory.Street);
            Build(board, 4, 0, ConstructionCategory.Waste);
            var cappedPreview = waste.PreviewAfterCleanup(board);
            Assert.That(cappedPreview.Cleared[connectedHome.id], Is.EqualTo(1));
            Assert.That(cappedPreview.Cleared[unconnectedHome.id], Is.Zero);
            var preview = waste.PreviewAfterCleanup(board,
                clearAllNearby: true);
            Assert.That(preview.Cleared[connectedHome.id], Is.EqualTo(1));
            Assert.That(preview.Cleared[unconnectedHome.id], Is.Zero,
                "A home beside another home does not join the street network.");
            var secondHuman = ConstructionHumanModel.SettleDay(2, board,
                preview.BlockedHomes, preview.BlockedFoodServices);
            waste.SettleDay(2, board, secondHuman, preview,
                clearAllNearby: true);
            Assert.That(waste.Backlog(connectedHome.id), Is.Zero,
                "The road-linked room also clears waste produced now.");
            Assert.That(waste.Backlog(unconnectedHome.id), Is.EqualTo(2));
        }

        [Test]
        public void DigitalHandHomeShowsWasteWorkAndMealIssuesTogether()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Assert.That(run.UsesUncappedWaste, Is.True);
            Place(run, 3, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            var home = Place(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.WasteBacklog(home.id), Is.EqualTo(1));

            var issues = ConstructionRoomIssueModel.ForTile(run, home,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(issues, Is.EquivalentTo(new[]
            {
                ConstructionRoomIssue.Waste,
                ConstructionRoomIssue.MissingWork,
                ConstructionRoomIssue.MissingMeal
            }));
            Assert.That(run.PreviewWasteAvailability().BlockedHomes, Is.Empty);
        }

        [Test]
        public void NewRestaurantDoesNotClearMissedWorkOrMealBeforeSettlement()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Place(run, 3, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            var home = Place(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.LastHumanDay.residents.Single().ate, Is.False);

            Place(run, 5, 3, ConstructionCategory.Restaurant);
            var forecast = run.PreviewHumanDay();
            Assert.That(forecast.residents.Single().worked, Is.True);
            Assert.That(forecast.residents.Single().ate, Is.True);
            var before = ConstructionRoomIssueModel.ForTile(run, home,
                forecast, run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(before, Does.Contain(ConstructionRoomIssue.MissingWork));
            Assert.That(before, Does.Contain(ConstructionRoomIssue.MissingMeal));

            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.LastHumanDay.residents.Single().worked, Is.True);
            Assert.That(run.LastHumanDay.residents.Single().ate, Is.True);
            var after = ConstructionRoomIssueModel.ForTile(run, home,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(after.Contains(ConstructionRoomIssue.MissingWork), Is.False);
            Assert.That(after.Contains(ConstructionRoomIssue.MissingMeal), Is.False);
        }

        [Test]
        public void WasteIconWaitsForActualCleanupEvenWhenForecastIsClear()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var opening = new[]
            {
                (ConstructionCategory.Green, 3, 3),
                (ConstructionCategory.Residence, 4, 3),
                (ConstructionCategory.Restaurant, 5, 3),
                (ConstructionCategory.Workshop, 4, 4),
                (ConstructionCategory.Street, 4, 2)
            };
            ConstructionTileData restaurant = null;
            foreach (var (category, column, row) in opening)
            {
                var built = Place(run, column, row, category,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);
                if (category == ConstructionCategory.Restaurant)
                    restaurant = built;
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            Assert.That(run.WasteBacklog(restaurant.id), Is.GreaterThan(0));

            Place(run, 6, 3, ConstructionCategory.Waste);
            Assert.That(run.PreviewWasteAvailability()
                .BacklogAfterCleanup[restaurant.id], Is.Zero);
            var before = ConstructionRoomIssueModel.ForTile(run, restaurant,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(before, Does.Contain(ConstructionRoomIssue.Waste));

            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.WasteBacklog(restaurant.id), Is.Zero);
            var after = ConstructionRoomIssueModel.ForTile(run, restaurant,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(after.Contains(ConstructionRoomIssue.Waste), Is.False);
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
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var restaurant = Place(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 2, 2, ConstructionCategory.Workshop);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.WasteBacklog(home.id), Is.EqualTo(3));
            Assert.That(run.WasteBacklog(restaurant.id), Is.EqualTo(2));
            Assert.That(run.PreviewHumanDay().CompletedWorkCycles, Is.Zero);

            Place(run, 4, 2, ConstructionCategory.Street);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 2, 1, ConstructionCategory.Waste);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 5, 2, ConstructionCategory.Supermarket);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            // The introductory Square needs an edge-connected resident route.
            // (4,1) touches the street at (4,2); the supermarket alone would
            // not provide a route for resident visits.
            Place(run, 4, 1, ConstructionCategory.Square);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Place(run, 3, 0, ConstructionCategory.Waste);
            var expectedCleared = run.PreviewWasteAvailability().Cleared.Values.Sum();
            var expected = run.PreviewHumanDay();
            Assert.That(expected.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(expectedCleared, Is.EqualTo(4));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PreviewHumanDay().CompletedWorkCycles,
                Is.EqualTo(expected.CompletedWorkCycles));
            Assert.That(restored.PreviewWasteAvailability().Cleared.Values.Sum(),
                Is.EqualTo(expectedCleared));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.LastHumanDay.CompletedWorkCycles,
                Is.EqualTo(expected.CompletedWorkCycles));
            Assert.That(restored.LastWasteDay.Cleared, Is.EqualTo(expectedCleared));
            Assert.That(restored.LastForecast.completedWorkCycles,
                Is.EqualTo(expected.CompletedWorkCycles));
            Assert.That(restored.LastForecast.wasteCleared,
                Is.EqualTo(expectedCleared));
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var again), Is.True);
            Assert.That(again.LastWasteDay.Remaining,
                Is.EqualTo(restored.LastWasteDay.Remaining));
            Assert.That(again.WasteBacklog(home.id),
                Is.EqualTo(restored.WasteBacklog(home.id)));
            Assert.That(again.WasteBacklog(restaurant.id),
                Is.EqualTo(restored.WasteBacklog(restaurant.id)));
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
