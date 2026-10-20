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
        public void FillAndEndlessUseDifferentSaveFiles()
        {
            Assert.That(ConstructionRunStore.PathFor(ConstructionGameMode.Fill),
                Is.Not.EqualTo(ConstructionRunStore.PathFor(
                    ConstructionGameMode.Endless)));
        }

        [Test]
        public void BuildImpactUsesActualDayPreviewsWithoutChangingTheRun()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var mealsBefore = run.PreviewEcologyDay().AnimalMeals;
            var workBefore = run.PreviewHumanDay().CompletedWorkCycles;
            Assert.That(run.TryPreviewBuildImpact(3, 2,
                ConstructionCategory.Residence, GreenPlanting.None,
                out var impact), Is.True);
            Assert.That(impact.animalMealsBefore, Is.EqualTo(mealsBefore));
            Assert.That(impact.workCyclesBefore, Is.EqualTo(workBefore));
            Assert.That(impact.residentsBefore, Is.Zero);
            Assert.That(impact.residentsAfter, Is.EqualTo(1));
            Assert.That(run.BuiltCount, Is.EqualTo(1));
            Assert.That(run.At(3, 2), Is.Null);
            Assert.That(run.BuiltToday, Is.False);

            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(impact.animalMealsAfter,
                Is.EqualTo(run.PreviewEcologyDay().AnimalMeals));
            Assert.That(impact.workCyclesAfter,
                Is.EqualTo(run.PreviewHumanDay().CompletedWorkCycles));
            Assert.That(run.TryPreviewBuildImpact(2, 2,
                ConstructionCategory.Residence, GreenPlanting.None,
                out _), Is.False, "The already-used daily build cannot be previewed.");
        }

        [Test]
        public void EndlessBoardExpandsOneRingOnlyAfterItIsFullAndRestoresOuterTile()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.ExpandWhenFull(), Is.False);
            Assert.That(board.CheckPlacement(-1, 3, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.OutsideBoard));
            foreach (var point in Enumerable.Range(0, 7)
                         .SelectMany(column => Enumerable.Range(0, 7)
                             .Select(row => (column, row)))
                         .OrderBy(point => Math.Abs(point.column - 3) +
                             Math.Abs(point.row - 3)))
            {
                if (point == (3, 3)) continue;
                Build(board, point.column, point.row, ConstructionCategory.Green,
                    GreenPlanting.Oak);
            }
            Assert.That(board.IsFull, Is.True);
            Assert.That(board.ExpandWhenFull(), Is.True);
            Assert.That(board.CurrentCapacity, Is.EqualTo(81));
            Assert.That(board.IsFull, Is.False);
            var outer = Build(board, -1, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(ConstructionBoardModel.TryRestore(board.Export(),
                out var restored), Is.True);
            Assert.That(restored.At(-1, 3).id, Is.EqualTo(outer.id));
            Assert.That(restored.ExpansionRings, Is.EqualTo(1));
        }

        [Test]
        public void EndlessRunExpandsAfterDay49AndUsesSeparateModeSave()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var point in Enumerable.Range(0, 7)
                         .SelectMany(column => Enumerable.Range(0, 7)
                             .Select(row => (column, row)))
                         .OrderBy(point => Math.Abs(point.column - 3) +
                             Math.Abs(point.row - 3)))
            {
                if (point == (3, 3)) continue;
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out var failure, out var placement), Is.True,
                    $"Day {run.CurrentDay}: {failure}/{placement} at {point}.");
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.BoardCapacity, Is.EqualTo(81));
            Assert.That(run.CurrentDay, Is.EqualTo(50));
            Assert.That(run.IsFinished, Is.False);
            Assert.That(run.TryBuild(-1, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(restored.At(-1, 3), Is.Not.Null);
        }

        [Test]
        public void FillModeCapsEachCategoryButStillAllowsFortyNineChoices()
        {
            var run = new ConstructionRunModel();
            var categories = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>().ToArray();
            Assert.That(run.CategoryLimit(ConstructionCategory.Green), Is.EqualTo(21));
            Assert.That(run.CategoryLimit(ConstructionCategory.Residence), Is.EqualTo(12));
            Assert.That(run.CategoryLimit(ConstructionCategory.Workshop), Is.EqualTo(7));
            Assert.That(run.CategoryLimit(ConstructionCategory.Restaurant), Is.EqualTo(5));
            Assert.That(run.CategoryLimit(ConstructionCategory.Supermarket), Is.EqualTo(5));
            Assert.That(run.CategoryLimit(ConstructionCategory.Waste), Is.EqualTo(7));
            Assert.That(run.CategoryLimit(ConstructionCategory.Street), Is.EqualTo(12));
            Assert.That(run.CategoryLimit(ConstructionCategory.Square), Is.EqualTo(11));
            Assert.That(categories.All(category => run.CategoryLimit(category) > 0 &&
                run.CategoryLimit(category) < ConstructionBoardModel.Capacity), Is.True);
            Assert.That(categories.Sum(run.CategoryLimit),
                Is.EqualTo(80).And.GreaterThan(ConstructionBoardModel.Capacity),
                "Caps must leave room to choose a composition, not prescribe one.");
            Assert.That(run.CategoryLimit(ConstructionCategory.Green) +
                run.CategoryLimit(ConstructionCategory.Residence) +
                run.CategoryLimit(ConstructionCategory.Square),
                Is.EqualTo(44).And.LessThan(ConstructionBoardModel.Capacity),
                "A full board now requires at least five functional rooms.");
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(1),
                "The starting oak counts as a green room.");
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(point => point != (3, 3))
                .OrderBy(point => Math.Abs(point.column - 3) +
                    Math.Abs(point.row - 3)).ToArray();
            var greenLimit = run.CategoryLimit(ConstructionCategory.Green);
            foreach (var point in positions.Take(greenLimit - 1))
            {
                Build(run, point.column, point.row, ConstructionCategory.Green,
                    GreenPlanting.Meadow);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var next = positions[greenLimit - 1];
            Assert.That(run.IsCategoryAtLimit(ConstructionCategory.Green), Is.True);
            Assert.That(run.CheckPlacement(next.column, next.row,
                ConstructionCategory.Green, GreenPlanting.Meadow),
                Is.EqualTo(ConstructionPlacementFailure.TypeLimitReached));
            Assert.That(run.TryPreviewBuildImpact(next.column, next.row,
                ConstructionCategory.Green, GreenPlanting.Meadow, out _), Is.False);
            Assert.That(run.TryBuild(next.column, next.row,
                ConstructionCategory.Green, GreenPlanting.Meadow,
                out _, out var failure, out var placement), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.TypeLimitReached));
            Assert.That(placement,
                Is.EqualTo(ConstructionPlacementFailure.TypeLimitReached));
            Assert.That(run.BuiltCount, Is.EqualTo(greenLimit));
            Assert.That(run.CheckPlacement(next.column, next.row,
                ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Build(run, next.column, next.row, ConstructionCategory.Residence);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.IsCategoryAtLimit(ConstructionCategory.Green), Is.True);
            Assert.That(new ConstructionRunModel(ConstructionGameMode.Endless)
                .CategoryLimit(ConstructionCategory.Green), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void EndlessUnsustainedPigeonFlockCanEndTheRun()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));
            var next = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(point => Math.Abs(point.column - 3) +
                    Math.Abs(point.row - 3))
                .Where(point => point != (3, 3) && point != (4, 3));
            foreach (var point in next)
            {
                if (run.IsFinished) break;
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.EndReason, Is.EqualTo(
                ConstructionEndReason.WildlifeGone));
            Assert.That(run.PigeonPopulation, Is.Zero);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.IsFinished, Is.True);
            Assert.That(restored.EndReason, Is.EqualTo(
                ConstructionEndReason.WildlifeGone));
        }

        [Test]
        public void EndlessResidentsCanLeaveAfterThreeMissedCyclesAndEndRun()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            void PlaceAndSettle(int column, int row,
                ConstructionCategory category, GreenPlanting planting)
            {
                Assert.That(run.TryBuild(column, row, category, planting,
                    out _, out var failure, out var placement), Is.True,
                    $"Day {run.CurrentDay}: {failure}/{placement}.");
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            PlaceAndSettle(4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            PlaceAndSettle(5, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            PlaceAndSettle(3, 2, ConstructionCategory.Residence, GreenPlanting.None);
            PlaceAndSettle(3, 1, ConstructionCategory.Restaurant, GreenPlanting.None);
            PlaceAndSettle(2, 2, ConstructionCategory.Workshop, GreenPlanting.None);
            Assert.That(run.LastHumanDay.CompletedWorkCycles, Is.EqualTo(1));
            PlaceAndSettle(2, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            PlaceAndSettle(1, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            PlaceAndSettle(1, 2, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.EndReason, Is.EqualTo(
                ConstructionEndReason.ResidentsGone));
            Assert.That(run.ResidentPopulation, Is.Zero);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.EndReason, Is.EqualTo(
                ConstructionEndReason.ResidentsGone));
        }

        [Test]
        public void FillModeRequiresOnePlacementAfterScriptedDayOne()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.CanAdvanceFillDay, Is.True);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.CanAdvanceFillDay, Is.False);
            Assert.That(run.TrySimulateFillDay(out _), Is.False);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.CanAdvanceFillDay, Is.True);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(3));
        }

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
        public void OpenPlotsArePlayerChosenButServiceAndStreetLinksStillMatter()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.CheckPlacement(6, 6, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(0, 0, ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(6, 0, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Waste),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrRestaurant));

            var isolatedMeadow = Build(board, 6, 6, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                isolatedMeadow.id), Is.Empty,
                "A freely placed meadow does not magically join the oak habitat.");
            Build(board, 0, 0, ConstructionCategory.Residence);
            Assert.That(board.CheckPlacement(1, 0, ConstructionCategory.Restaurant),
                Is.EqualTo(ConstructionPlacementFailure.None));

            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Street);
            Build(board, 3, 0, ConstructionCategory.Workshop);
            Build(board, 2, 1, ConstructionCategory.Restaurant);
            Build(board, 4, 1, ConstructionCategory.Supermarket);
            Build(board, 4, 2, ConstructionCategory.Waste);

            Assert.That(board.BuiltCount, Is.EqualTo(9));
            Assert.That(board.CheckPlacement(5, 1, ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
            Assert.That(ConstructionBoardModel.TryRestore(board.Export(),
                out var restored), Is.True);
            Assert.That(restored.At(6, 6)?.id, Is.EqualTo(isolatedMeadow.id));
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
        public void AnimalRouteTurnsOnlyThroughConnectedGreenTiles()
        {
            var board = new ConstructionBoardModel();
            var first = Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var turn = Build(board, 4, 2, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var target = Build(board, 5, 2, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                target.id).Select(tile => tile.id), Is.EqualTo(new[]
                { ConstructionBoardModel.StarterOakId, first.id, turn.id, target.id }));

            Build(board, 6, 2, ConstructionCategory.Residence);
            var isolated = Build(board, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                isolated.id), Is.Empty,
                "A resident room must not bridge an animal route.");
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

            saved.tiles[1].column = 7;
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
            Assert.That(ledger.FinalResult().AnimalMealsPerDay, Is.EqualTo(2f));
            Assert.That(ledger.FinalResult().HumanWorkdaysPerDay, Is.EqualTo(1f));
            Assert.That(ledger.TryRecordDay(3, 2, 1), Is.True);
            Assert.That(ledger.FinalResult().AnimalMealsPerDay, Is.EqualTo(2f),
                "Repeating a stable day raises totals, not the daily-rate score.");
            Assert.That(ledger.FinalResult().HumanWorkdaysPerDay, Is.EqualTo(1f));

            var saved = ledger.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out var restored), Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(6));
            saved.days[0].animalMeals = -1;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
        }

        [Test]
        public void StorySuggestsObservedNeedsWithoutLockingConstructionChoices()
        {
            var board = new ConstructionBoardModel();
            var story = new ConstructionStoryModel();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Assert.That(story.IsUnlocked(ConstructionCategory.Green), Is.False);
            story.RecordSquirrelMeal();
            Assert.That(story.Observe(board), Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(story.IsUnlocked(ConstructionCategory.Green), Is.True);
            Assert.That(story.IsUnlocked(ConstructionCategory.Residence), Is.True);
            Assert.That(story.IsUnlocked(ConstructionCategory.Square), Is.True);

            Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(story.Observe(board), Is.EqualTo(ConstructionStoryStage.GrowGreen),
                "A new tile alone must not invent an animal arrival.");
            story.RecordNewWildlifeArrival();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(story.IsUnlocked(ConstructionCategory.Restaurant), Is.True);
            Build(board, 4, 2, ConstructionCategory.Residence);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Build(board, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(story.IsUnlocked(ConstructionCategory.Workshop), Is.True);
            story.RecordResidentMeal();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Build(board, 3, 2, ConstructionCategory.Workshop);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildWasteRoom));
            Build(board, 5, 1, ConstructionCategory.Waste);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.AwaitAccessProblem));
            Assert.That(story.IsUnlocked(ConstructionCategory.Street), Is.True);

            Build(board, 6, 2, ConstructionCategory.Residence);
            story.RecordDirectAccessBlocked();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildStreet));
            Build(board, 4, 1, ConstructionCategory.Street);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildStreet),
                "A street unused by the second home has not solved its access problem.");
            foreach (var position in new[]
                     { (6, 1), (6, 0), (5, 0), (4, 0), (3, 0), (3, 1) })
                Build(board, position.Item1, position.Item2,
                    ConstructionCategory.Street);
            Assert.That(board.ResidenceServiceRoute(board.At(6, 2).id,
                ConstructionCategory.Workshop).Any(tile =>
                tile.category == ConstructionCategory.Street), Is.True);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(story.IsUnlocked(ConstructionCategory.Supermarket), Is.True);

            story.RecordRestaurantCrowded();
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.BuildSupermarket));
            Build(board, 2, 1, ConstructionCategory.Supermarket);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.FreeBuild));
            Assert.That(story.IsUnlocked(ConstructionCategory.Square), Is.True);

            Assert.That(ConstructionStoryModel.TryRestore(story.Export(), board,
                out var restored), Is.True);
            Assert.That(restored.Stage, Is.EqualTo(ConstructionStoryStage.FreeBuild));
        }

        [Test]
        public void FreeChoiceStillRequiresValidPlacementAndOneRoomPerDay()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.IsUnlocked(ConstructionCategory.Residence), Is.True);
            Assert.That(run.IsUnlocked(ConstructionCategory.Restaurant), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out var failure, out var placement), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
            Assert.That(placement, Is.EqualTo(
                ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out failure, out _), Is.True);
            Assert.That(run.TryBuild(3, 4, ConstructionCategory.Square,
                GreenPlanting.None, out _, out failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.DailyLimitReached));
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 4, ConstructionCategory.Square,
                GreenPlanting.None, out _, out failure, out _), Is.True);
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
        public void OneNewCellPerDaySurvivesSaveButWaitingIsAllowed()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.BuiltToday, Is.False,
                "The starter oak is not the player's daily construction.");
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.BuiltCount, Is.EqualTo(1));
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.BuiltToday, Is.True);
            Assert.That(run.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out var failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.DailyLimitReached));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.BuiltToday, Is.True);
            Assert.That(restored.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.DailyLimitReached));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.BuiltToday, Is.False);
            Build(restored, 2, 3, ConstructionCategory.Green, GreenPlanting.Oak);
        }

        [Test]
        public void FailedRemoteWorkshopPlacementExplainsAndUnlocksStreet()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Build(run, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.FirstResidentMealDay, Is.EqualTo(3));
            Build(run, 3, 2, ConstructionCategory.Workshop);
            Assert.That(run.TryEndDay(0, 0), Is.True);
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
            Assert.That(run.TryEndDay(0, 0), Is.True);
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
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Build(run, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(run.FirstResidentHomeTileId, Is.EqualTo(firstHome.id));
            Assert.That(run.FirstResidentHasRestaurantAccess, Is.False);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Assert.That(run.IsUnlocked(ConstructionCategory.Workshop), Is.True);

            Assert.That(run.TryEndDay(0, 0), Is.True);
            Build(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.FirstResidentHasRestaurantAccess, Is.True);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(run.FirstResidentAte, Is.False);
            Assert.That(run.IsUnlocked(ConstructionCategory.Workshop), Is.True);
            var route = run.FirstResidentRestaurantRoute;
            Assert.That(route.Select(tile => tile.id), Is.EqualTo(new[]
                { firstHome.id, run.At(3, 1).id }));
            Assert.That(run.TryEndDay(0, 0), Is.True);
            Assert.That(run.FirstResidentAte, Is.True);
            Assert.That(run.FirstResidentMealDay, Is.EqualTo(5));
            Assert.That(run.HumanScore, Is.Zero,
                "Eating is not a completed work cycle.");
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.FirstResidentMealDay, Is.EqualTo(5));
            var corrupt = run.Export();
            corrupt.firstResidentMealDay = 1;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False,
                "The restaurant did not exist on day 1.");
        }

        [Test]
        public void SimulatedFortyNineCellRunEndsWithObservedRateScores()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out var opening), Is.True);
            var observedMeals = opening.AnimalMeals;
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Math.Abs(position.column - 3) +
                    Math.Abs(position.row - 3));
            foreach (var position in positions)
            {
                var choice = FillChoice(run);
                Build(run, position.column, position.row,
                    choice.category, choice.planting);
                Assert.That(run.TrySimulateDay(out var ecology), Is.True);
                observedMeals += ecology.AnimalMeals;
            }
            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.FinalResult.DaysPlayed, Is.EqualTo(49));
            Assert.That(run.FinalResult.AnimalMealsTotal, Is.EqualTo(observedMeals));
            Assert.That(run.FinalResult.HumanWorkdaysTotal, Is.Zero);
            Assert.That(run.FinalResult.AnimalMealsPerDay,
                Is.EqualTo((float)observedMeals / 49).Within(0.0001f));
            Assert.That(run.FinalResult.AnimalMealsPerDay,
                Is.LessThanOrEqualTo(run.SquirrelPopulation + run.PigeonPopulation));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FinalResult.AnimalMealsTotal,
                Is.EqualTo(observedMeals));
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
            {
                var choice = FillChoice(run);
                Assert.That(run.TryBuild(position.column, position.row,
                    choice.category, choice.planting,
                    out _, out var failure, out _), Is.True, failure.ToString());
                if (!run.IsFull) Assert.That(run.TryEndDay(0, 0), Is.True);
            }

            Assert.That(run.IsFull, Is.True);
            Assert.That(run.IsFinished, Is.False);
            Assert.That(run.TryEndDay(3, 0), Is.True);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.AnimalScore, Is.EqualTo(3));
            Assert.That(run.FinalResult.DaysPlayed, Is.EqualTo(48));
            Assert.That(run.FinalResult.AnimalMealsPerDay,
                Is.EqualTo(3f / 48f).Within(0.0001f));
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
        public void RestoredEmptyPigeonIdStillAllowsTheFlockToArrive()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var saved = run.Export();
            // Unity JSON writes an unset string as "". This must not block
            // the first flock on a resumed run.
            saved.pigeonTileId = string.Empty;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.PigeonTileId, Is.Null);
            var meadow = Build(restored, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(restored.PigeonTileId, Is.EqualTo(meadow.id));
            Assert.That(restored.PigeonArrivalDay, Is.EqualTo(3));
            Assert.That(restored.IsUnlocked(ConstructionCategory.Residence), Is.True);
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
                Assert.That(loaded.TryEndDay(0, 0), Is.True);
                Build(loaded, 3, 1, ConstructionCategory.Restaurant);
                Assert.That(loaded.StoryStage, Is.EqualTo(
                    ConstructionStoryStage.WatchResidentEat));
                Assert.That(loaded.TryEndDay(0, 0), Is.True);
                Assert.That(store.TrySave(loaded), Is.True);
                Assert.That(store.TryLoad(out var reloaded), Is.True);
                Assert.That(reloaded.CurrentDay, Is.EqualTo(4));
                Assert.That(reloaded.FirstResidentMealDay, Is.EqualTo(3));
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

        [Test]
        public void PigeonMeadowInspectionSurvivesSaveWithoutUnlockingWrongTile()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.InspectPigeonMeadow(ConstructionBoardModel.StarterOakId),
                Is.False);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var meadow = Build(run, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.StoryStage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(run.InspectPigeonMeadow(ConstructionBoardModel.StarterOakId),
                Is.False);
            Assert.That(run.PigeonMeadowInspected, Is.False);
            Assert.That(run.InspectPigeonMeadow(meadow.id), Is.True);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PigeonMeadowInspected, Is.True);
            var corrupt = run.Export();
            corrupt.pigeonTileId = string.Empty;
            Assert.That(ConstructionRunModel.TryRestore(corrupt, out _), Is.False);
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

        private static (ConstructionCategory category, GreenPlanting planting)
            FillChoice(ConstructionRunModel run)
        {
            if (!run.IsCategoryAtLimit(ConstructionCategory.Green))
                return (ConstructionCategory.Green, GreenPlanting.Meadow);
            if (!run.IsCategoryAtLimit(ConstructionCategory.Residence))
                return (ConstructionCategory.Residence, GreenPlanting.None);
            // Green, residences and squares can occupy only 44 cells under the
            // 80-room allocation. Add five functional rooms near the homes
            // before completing the board with squares.
            foreach (var category in new[]
                     {
                         ConstructionCategory.Workshop,
                         ConstructionCategory.Restaurant,
                         ConstructionCategory.Supermarket,
                         ConstructionCategory.Waste,
                         ConstructionCategory.Street
                     })
            {
                if (run.CategoryBuiltCount(category) == 0)
                    return (category, GreenPlanting.None);
            }
            return (ConstructionCategory.Square, GreenPlanting.None);
        }
    }
}
