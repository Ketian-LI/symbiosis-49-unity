using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionBoardModelTests
    {
        [Test]
        public void NewRunContainsOnlyOneCenteredOak()
        {
            var board = new ConstructionBoardModel();

            Assert.That(board.BuiltCount, Is.EqualTo(1));
            Assert.That(board.IsFull, Is.False);
            Assert.That(board.At(3, 3).id, Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(board.At(3, 3).planting, Is.EqualTo(GreenPlanting.Oak));
            Assert.That(board.At(3, 3).builtDay, Is.EqualTo(1));
            Assert.That(board.At(0, 0), Is.Null);
        }

        [Test]
        public void PlacementUsesFourNeighboursAndRequiresAnchoredStreet()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.CheckPlacement(6, 6, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(4, 4, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Waste),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrRestaurant));

            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Street);
            Build(board, 3, 0, ConstructionCategory.Workshop);
            Build(board, 2, 1, ConstructionCategory.Restaurant);
            Build(board, 4, 1, ConstructionCategory.Supermarket);
            Build(board, 4, 2, ConstructionCategory.Waste);

            Assert.That(board.BuiltCount, Is.EqualTo(7));
            Assert.That(board.CheckPlacement(5, 1, ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
        }

        [Test]
        public void GreenComponentAndResidentialAdjacencyAreDifferentCounts()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var greenA = Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Build(board, 5, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Build(board, 2, 3, ConstructionCategory.Green, GreenPlanting.Shrub);

            Assert.That(board.AdjacentGreenCount(home.id), Is.EqualTo(1));
            Assert.That(board.ConnectedGreenCount(greenA.id), Is.EqualTo(4));
            Assert.That(board.TouchesResidence(greenA.id), Is.False);
            Assert.That(board.TouchesResidence(ConstructionBoardModel.StarterOakId), Is.True);
        }

        [Test]
        public void FirstResidentNeedsOwnRestaurantAccessNotAnotherHomesRestaurant()
        {
            var board = new ConstructionBoardModel();
            var firstHome = Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 4, 3, ConstructionCategory.Residence);
            Build(board, 5, 3, ConstructionCategory.Restaurant);
            Assert.That(board.FirstResidence.id, Is.EqualTo(firstHome.id));
            Assert.That(board.FirstResidenceCanReachService(
                ConstructionCategory.Restaurant), Is.False);

            Build(board, 3, 1, ConstructionCategory.Street);
            Build(board, 4, 1, ConstructionCategory.Street);
            Build(board, 5, 1, ConstructionCategory.Street);
            Build(board, 5, 2, ConstructionCategory.Street);
            Assert.That(board.FirstResidenceCanReachService(
                ConstructionCategory.Restaurant), Is.True);
            Assert.That(board.FirstResidenceCanReachService(
                ConstructionCategory.Workshop), Is.False);
        }

        [Test]
        public void PigeonHabitatMustBeMeadowInTheOaksGreenComponent()
        {
            var board = new ConstructionBoardModel();
            Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Any(tile => tile.planting == GreenPlanting.Meadow), Is.False);

            Build(board, 5, 3, ConstructionCategory.Residence);
            var separateMeadow = Build(board, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Any(tile => tile.id == separateMeadow.id), Is.False,
                "A house does not connect two green areas for wildlife arrival.");

            Build(board, 5, 2, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Any(tile => tile.planting == GreenPlanting.Meadow), Is.False);
            Build(board, 4, 2, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Any(tile => tile.planting == GreenPlanting.Meadow), Is.True);
        }

        [Test]
        public void OnlyGreenCanHavePlantingAndSavedDataCannotMutateLiveBoard()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.InvalidPlanting));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Residence,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.InvalidPlanting));

            board.At(3, 3).planting = GreenPlanting.Shrub;
            board.BuiltTiles[0].column = 0;
            board.Export().tiles[0].row = 0;
            Assert.That(board.At(3, 3).planting, Is.EqualTo(GreenPlanting.Oak));
            Assert.That(board.At(3, 3).column, Is.EqualTo(3));
            Assert.That(board.At(3, 3).row, Is.EqualTo(3));
        }

        [Test]
        public void PartialBoardRoundTripsAndCorruptSaveIsRejected()
        {
            var board = new ConstructionBoardModel();
            Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Build(board, 3, 2, ConstructionCategory.Residence);
            var saved = board.Export();

            Assert.That(ConstructionBoardModel.TryRestore(saved, out var restored), Is.True);
            Assert.That(restored.BuiltCount, Is.EqualTo(3));
            Assert.That(restored.At(3, 2).builtDay, Is.EqualTo(1));
            Assert.That(restored.At(4, 3).planting, Is.EqualTo(GreenPlanting.Oak));
            Assert.That(restored.At(0, 0), Is.Null);

            saved.tiles[1].column = 6;
            Assert.That(ConstructionBoardModel.TryRestore(saved, out _), Is.False);
            saved.tiles[1].column = 4;
            saved.tiles[0].planting = GreenPlanting.Meadow;
            Assert.That(ConstructionBoardModel.TryRestore(saved, out _), Is.False);
        }

        [Test]
        public void ConstructionEndsAtExactlyFortyNineCells()
        {
            var board = new ConstructionBoardModel();
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Math.Abs(position.column - 3) +
                    Math.Abs(position.row - 3));
            foreach (var position in positions)
                Build(board, position.column, position.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow);

            Assert.That(board.BuiltCount, Is.EqualTo(49));
            Assert.That(board.IsFull, Is.True);
            Assert.That(ConstructionBoardModel.TryRestore(board.Export(), out var restored), Is.True);
            Assert.That(restored.IsFull, Is.True);
            Assert.That(board.CheckPlacement(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.BoardFull));
        }

        [Test]
        public void ScoreCountsActualDailyMealsAndWorkOnce()
        {
            var ledger = new ConstructionScoreLedger();
            Assert.That(ledger.TryRecordDay(1, 1, 0), Is.True);
            Assert.That(ledger.TryRecordDay(2, 3, 2), Is.True);
            Assert.That(ledger.TryRecordDay(2, 999, 999), Is.False);
            Assert.That(ledger.TryRecordDay(4, 1, 1), Is.False);
            Assert.That(ledger.AnimalScore, Is.EqualTo(4));
            Assert.That(ledger.HumanScore, Is.EqualTo(2));

            var saved = ledger.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out var restored), Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(4));
            saved.days[0].animalMeals = -1;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
        }

        [Test]
        public void StoryUnlocksFollowObservedNeedsAndConstructedRooms()
        {
            var board = new ConstructionBoardModel();
            var story = new ConstructionStoryModel();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Assert.That(story.IsUnlocked(ConstructionCategory.Green), Is.False);
            story.RecordSquirrelMeal();
            Assert.That(story.Observe(board), Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(story.IsUnlocked(ConstructionCategory.Green), Is.True);

            Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(story.Observe(board), Is.EqualTo(ConstructionStoryStage.GrowGreen),
                "A new tile alone must not invent an animal arrival.");
            story.RecordNewWildlifeArrival();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(story.IsUnlocked(ConstructionCategory.Restaurant), Is.False);
            Build(board, 4, 2, ConstructionCategory.Residence);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Build(board, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(story.IsUnlocked(ConstructionCategory.Workshop), Is.False);
            story.RecordResidentMeal();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Build(board, 3, 2, ConstructionCategory.Workshop);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildWasteRoom));
            Build(board, 5, 1, ConstructionCategory.Waste);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.AwaitAccessProblem));
            Assert.That(story.IsUnlocked(ConstructionCategory.Street), Is.False);

            story.RecordDirectAccessBlocked();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildStreet));
            Build(board, 4, 1, ConstructionCategory.Street);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(story.IsUnlocked(ConstructionCategory.Supermarket), Is.False);

            story.RecordRestaurantCrowded();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildSupermarket));
            Build(board, 3, 1, ConstructionCategory.Supermarket);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.FreeBuild));
            Assert.That(story.IsUnlocked(ConstructionCategory.Square), Is.True);

            Assert.That(ConstructionStoryModel.TryRestore(story.Export(), board,
                out var restored), Is.True);
            Assert.That(restored.Stage, Is.EqualTo(ConstructionStoryStage.FreeBuild));
        }

        [Test]
        public void RunSaveKeepsPartialConstructionDayAndActualScoresTogether()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out var locked, out _), Is.False);
            Assert.That(locked, Is.EqualTo(ConstructionBuildFailure.LockedByStory));

            run.RecordSquirrelMeal();
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            Assert.That(run.PigeonTileId, Is.EqualTo("built-002"));
            Assert.That(run.PigeonArrivalDay, Is.EqualTo(2));
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TryEndDay(0, 0), Is.True);

            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored), Is.True);
            Assert.That(restored.CurrentDay, Is.EqualTo(3));
            Assert.That(restored.BuiltCount, Is.EqualTo(3));
            Assert.That(restored.At(3, 2).builtDay, Is.EqualTo(2));
            Assert.That(restored.AnimalScore, Is.EqualTo(1));
            Assert.That(restored.HumanScore, Is.Zero,
                "Moving into a home is not a completed work cycle.");
            Assert.That(restored.FirstResidentHomeTileId,
                Is.EqualTo(run.FirstResidentHomeTileId));
            Assert.That(restored.FirstResidentHasRestaurantAccess, Is.False);
            Assert.That(restored.PigeonTileId, Is.EqualTo("built-002"));
            Assert.That(restored.PigeonArrivalDay, Is.EqualTo(2));
            Assert.That(restored.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));

            var corrupt = run.Export();
            corrupt.currentDay = 1;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False);
            corrupt = run.Export();
            corrupt.board.tiles[2].builtDay = 99;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False);
        }

        [Test]
        public void FailedRemoteWorkshopPlacementExplainsAndUnlocksStreet()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Build(run, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.FirstResidentMealDay, Is.EqualTo(2));
            Build(run, 3, 2, ConstructionCategory.Workshop);
            Build(run, 5, 1, ConstructionCategory.Waste);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.AwaitAccessProblem));

            Assert.That(run.TryBuild(5, 0, ConstructionCategory.Workshop,
                GreenPlanting.None, out _, out var failure, out var placement), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
            Assert.That(placement, Is.EqualTo(
                ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(run.StoryStage, Is.EqualTo(ConstructionStoryStage.BuildStreet));
            Assert.That(run.IsUnlocked(ConstructionCategory.Street), Is.True);
            Build(run, 4, 1, ConstructionCategory.Street);
        }

        [Test]
        public void RestaurantByAnotherHomeDoesNotResolveFirstResidentsNeed()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var firstHome = Build(run, 3, 2, ConstructionCategory.Residence);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Build(run, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(run.FirstResidentHomeTileId, Is.EqualTo(firstHome.id));
            Assert.That(run.FirstResidentHasRestaurantAccess, Is.False);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Assert.That(run.IsUnlocked(ConstructionCategory.Workshop), Is.False);

            Build(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.FirstResidentHasRestaurantAccess, Is.True);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(run.FirstResidentAte, Is.False);
            Assert.That(run.IsUnlocked(ConstructionCategory.Workshop), Is.False);
            var route = run.FirstResidentRestaurantRoute;
            Assert.That(route.Select(tile => tile.id), Is.EqualTo(new[]
                { firstHome.id, run.At(3, 1).id }));
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.FirstResidentAte, Is.True);
            Assert.That(run.FirstResidentMealDay, Is.EqualTo(2));
            Assert.That(run.HumanScore, Is.Zero,
                "Eating is not a completed work cycle.");
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.FirstResidentMealDay, Is.EqualTo(2));
            var corrupt = run.Export();
            corrupt.firstResidentMealDay = 1;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False,
                "The restaurant did not exist on day 1.");
        }

        [Test]
        public void FullBoardScoresTheFinalDayThenStops()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Math.Abs(position.column - 3) +
                    Math.Abs(position.row - 3));
            foreach (var position in positions)
                Assert.That(run.TryBuild(position.column, position.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out var failure, out _), Is.True, failure.ToString());

            Assert.That(run.IsFull, Is.True);
            Assert.That(run.IsFinished, Is.False);
            Assert.That(run.TryEndDay(3, 0), Is.True);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.AnimalScore, Is.EqualTo(3));
            Assert.That(run.TryEndDay(3, 0), Is.False);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored), Is.True);
            Assert.That(restored.IsFinished, Is.True);
        }

        [Test]
        public void MeadowAttractsOnePigeonFlockOnlyAfterTheDayChanges()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            Assert.That(run.TryEndDay(1, 0), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.PigeonTileId, Is.Null);
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.PigeonTileId, Is.Null,
                "More oak alone must not summon pigeons.");
            var meadow = Build(run, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(run.PigeonTileId, Is.Null,
                "Placing a meadow must not cause an immediate arrival.");
            Assert.That(run.StoryStage, Is.EqualTo(ConstructionStoryStage.GrowGreen));

            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(4));
            Assert.That(run.PigeonTileId, Is.EqualTo(meadow.id));
            Assert.That(run.PigeonArrivalDay, Is.EqualTo(4));
            Assert.That(run.StoryStage, Is.EqualTo(ConstructionStoryStage.WelcomeResident));
            Assert.That(run.AnimalScore, Is.EqualTo(1),
                "Arriving pigeons have not yet been observed eating.");
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.PigeonTileId, Is.EqualTo(meadow.id));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.PigeonTileId, Is.EqualTo(meadow.id));

            var corrupt = run.Export();
            corrupt.pigeonTileId = ConstructionBoardModel.StarterOakId;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False);
            corrupt = run.Export();
            corrupt.pigeonArrivalDay = meadow.builtDay;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False);
        }

        [Test]
        public void SeparateConstructionSaveLoadsAndCanBeClearedForNewGame()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-construction-test-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            try
            {
                Assert.That(store.TryLoad(out _), Is.False);
                var run = new ConstructionRunModel();
                run.RecordSquirrelMeal();
                Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
                Assert.That(run.TryEndDay(1, 0), Is.True);
                Assert.That(store.TrySave(run), Is.True);
                Assert.That(store.TryLoad(out var loaded), Is.True);
                Assert.That(loaded.CurrentDay, Is.EqualTo(2));
                Assert.That(loaded.BuiltCount, Is.EqualTo(2));
                Assert.That(loaded.AnimalScore, Is.EqualTo(1));
                Assert.That(loaded.PigeonTileId, Is.EqualTo("built-002"));
                Assert.That(loaded.PigeonArrivalDay, Is.EqualTo(2));

                Build(loaded, 3, 2, ConstructionCategory.Residence);
                Build(loaded, 3, 1, ConstructionCategory.Restaurant);
                Assert.That(loaded.StoryStage, Is.EqualTo(
                    ConstructionStoryStage.WatchResidentEat));
                Assert.That(loaded.TryEndDay(0, 0), Is.True);
                Assert.That(store.TrySave(loaded), Is.True);
                Assert.That(store.TryLoad(out var reloaded), Is.True);
                Assert.That(reloaded.CurrentDay, Is.EqualTo(3));
                Assert.That(reloaded.FirstResidentMealDay, Is.EqualTo(2));
                Assert.That(reloaded.FirstResidentAte, Is.True);
                Assert.That(reloaded.HumanScore, Is.Zero);
                Assert.That(store.TryDelete(), Is.True);
                Assert.That(store.TryLoad(out _), Is.False);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static ConstructionTileData Build(ConstructionBoardModel board, int column,
            int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(board.TryBuild(column, row, category, planting,
                out var built, out var failure), Is.True, failure.ToString());
            return built;
        }

        private static ConstructionTileData Build(ConstructionRunModel run, int column,
            int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(run.TryBuild(column, row, category, planting,
                out var built, out var failure, out var placement), Is.True,
                $"{failure}: {placement}");
            return built;
        }
    }
}
