using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UnityEngine;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionBoardModelTests
    {
        [Test]
        public void BoardGameUsesFillRulesAndKeepsItsOwnSaveFile()
        {
            var boardGame = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(boardGame.Mode, Is.EqualTo(ConstructionGameMode.BoardGame));
            Assert.That(boardGame.BoardCapacity, Is.EqualTo(49));
            Assert.That(boardGame.CanAdvanceConstructionDay, Is.True);
            Assert.That(boardGame.TrySimulateDay(out _), Is.True);
            Assert.That(boardGame.IsRandomBoardGame, Is.True);
            Assert.That(boardGame.IsIntroPhase, Is.False,
                "The camera board game draws from its fixed card inventory.");
            Assert.That(boardGame.IntroRequiredCategory, Is.Null);
            Assert.That(boardGame.CanAdvanceConstructionDay, Is.False);
            Assert.That(ConstructionRunModel.TryRestore(boardGame.Export(),
                out var restored), Is.True);
            Assert.That(restored.Mode, Is.EqualTo(ConstructionGameMode.BoardGame));
            Assert.That(restored.IsRandomBoardGame, Is.True);
            Assert.That(ConstructionRunStore.PathFor(ConstructionGameMode.BoardGame),
                Does.EndWith("construction-boardgame-v1.json"));
            Assert.That(ConstructionRunStore.PathFor(ConstructionGameMode.BoardGame),
                Is.Not.EqualTo(ConstructionRunStore.PathFor(ConstructionGameMode.Fill)));
        }

        [Test]
        public void DiagonalOnlyPlotCannotBeBuiltAndAnimalRouteNeedsAnEdgeBridge()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TouchesBuiltTile(4, 3), Is.True);
            Assert.That(board.TouchesBuiltTile(4, 4), Is.False);
            Assert.That(board.CheckPlacement(4, 4,
                    ConstructionCategory.Green, GreenPlanting.Meadow),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out _, out var rejected),
                Is.False);
            Assert.That(rejected, Is.EqualTo(
                ConstructionPlacementFailure.Disconnected));
            // A diagonal still lies within the eight-cell influence area.
            Assert.That(ConstructionBoardModel.AreNeighbours(3, 3, 4, 4),
                Is.True);
            var bridge = Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out var diagonal, out var failure),
                Is.True, failure.ToString());
            Assert.That(board.AnimalRouteBetween(
                    ConstructionBoardModel.StarterOakId, diagonal.id)
                .Select(tile => tile.id), Is.EqualTo(new[]
                {
                    ConstructionBoardModel.StarterOakId, bridge.id, diagonal.id
                }));
            Assert.That(board.ConnectedGreenCount(diagonal.id), Is.EqualTo(3));
        }

        [Test]
        public void DiagonalHomeDoesNotConnectServiceOrStreet()
        {
            var board = new ConstructionBoardModel();
            Build(board, 4, 3, ConstructionCategory.Residence);
            Build(board, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.TouchesBuiltTile(5, 4), Is.True,
                "This plot has an edge connection to green space.");
            Assert.That(board.CheckPlacement(5, 4,
                    ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet),
                "A diagonal home cannot supply a workplace connection.");
            Assert.That(board.CheckPlacement(5, 4,
                    ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection),
                "A diagonal home cannot start a street route.");
            Assert.That(board.CheckPlacement(5, 4,
                    ConstructionCategory.Waste),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrRestaurant));
            Assert.That(board.WouldConnectSquareToHome(5, 4), Is.False);
        }

        [Test]
        public void OldDiagonalStreetChainLoadsWithoutAllowingNewDiagonalConnections()
        {
            var oldBoard = new ConstructionBoardModel();
            Assert.That(oldBoard.TryBuild(4, 4,
                ConstructionCategory.Residence, GreenPlanting.None,
                out _, out _, 2, allowIntroPlacement: true), Is.True);
            Assert.That(oldBoard.TryBuild(5, 5,
                ConstructionCategory.Street, GreenPlanting.None,
                out _, out _, 3, allowIntroPlacement: true), Is.True);
            Assert.That(oldBoard.TryBuild(6, 6,
                ConstructionCategory.Street, GreenPlanting.None,
                out _, out _, 4, allowIntroPlacement: true), Is.True);

            Assert.That(ConstructionBoardModel.TryRestore(oldBoard.Export(),
                out var restored), Is.True,
                "A change in movement rules must not erase an older save.");
            Assert.That(restored.BuiltCount, Is.EqualTo(4));
            Assert.That(restored.CheckPlacement(6, 5,
                    ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection),
                "New construction must use the current four-edge route rule.");
        }

        [Test]
        public void ResidenceKindsSurviveTileCopiesJsonAndBoardReplay()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out var garden, out _,
                residenceKind: ResidenceKind.Garden), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out var urban, out _,
                residenceKind: ResidenceKind.Urban), Is.True);
            Assert.That(garden.residenceKind, Is.EqualTo(ResidenceKind.Garden));
            Assert.That(urban.residenceKind, Is.EqualTo(ResidenceKind.Urban));
            Assert.That(board.At(4, 3).residenceKind,
                Is.EqualTo(ResidenceKind.Garden));
            Assert.That(board.BuiltTiles.Single(tile => tile.id == urban.id)
                .residenceKind, Is.EqualTo(ResidenceKind.Urban));

            var json = JsonUtility.ToJson(board.Export());
            var saved = JsonUtility.FromJson<ConstructionBoardSaveData>(json);
            Assert.That(ConstructionBoardModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.At(4, 3).residenceKind,
                Is.EqualTo(ResidenceKind.Garden));
            Assert.That(restored.At(5, 3).residenceKind,
                Is.EqualTo(ResidenceKind.Urban));
        }

        [Test]
        public void OlderBoardJsonWithoutResidenceKindRestoresStandardHome()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _), Is.True);
            var legacyJson = JsonUtility.ToJson(board.Export())
                .Replace(",\"residenceKind\":0", string.Empty);
            Assert.That(legacyJson, Does.Not.Contain("\"residenceKind\""));

            var saved = JsonUtility.FromJson<ConstructionBoardSaveData>(
                legacyJson);
            Assert.That(ConstructionBoardModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.At(4, 3).residenceKind,
                Is.EqualTo(ResidenceKind.Standard));
        }

        [Test]
        public void InvalidResidenceKindOrNonResidenceSubtypeCannotBeBuiltOrLoaded()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, residenceKind: ResidenceKind.Garden),
                Is.EqualTo(ConstructionPlacementFailure.InvalidResidenceKind));
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out var wrongCategory,
                residenceKind: ResidenceKind.Garden), Is.False);
            Assert.That(wrongCategory,
                Is.EqualTo(ConstructionPlacementFailure.InvalidResidenceKind));
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out var invalidValue,
                residenceKind: (ResidenceKind)99), Is.False);
            Assert.That(invalidValue,
                Is.EqualTo(ConstructionPlacementFailure.InvalidResidenceKind));
            Assert.That(board.BuiltCount, Is.EqualTo(1));

            var save = board.Export();
            save.tiles[0].residenceKind = ResidenceKind.Garden;
            Assert.That(ConstructionBoardModel.TryRestore(save, out _), Is.False);
            save.tiles[0].residenceKind = ResidenceKind.Standard;
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _), Is.True);
            save = board.Export();
            save.tiles[1].residenceKind = (ResidenceKind)99;
            Assert.That(ConstructionBoardModel.TryRestore(save, out _), Is.False);
            save.tiles[1].category = ConstructionCategory.Square;
            save.tiles[1].residenceKind = ResidenceKind.Garden;
            Assert.That(ConstructionBoardModel.TryRestore(save, out _), Is.False);
        }

        [Test]
        public void StreetExposureCountsAdjacentWildlifeWithoutInventingDeaths()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var street = Build(run, 3, 2, ConstructionCategory.Street);

            Assert.That(run.StreetAdjacentWildlifeCount(street.id),
                Is.EqualTo(run.SquirrelPopulation + run.PigeonPopulation),
                "The diagonally neighbouring meadow exposes its pigeon flock.");
            Assert.That(run.StreetAdjacentWildlifeCount("not-a-street"), Is.Zero);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.StreetAdjacentWildlifeCount(street.id),
                Is.EqualTo(run.StreetAdjacentWildlifeCount(street.id)));
        }

        [Test]
        public void StreetCarTracksTheCentreOfEitherStreetDirection()
        {
            var root = new GameObject("Street car centreline test");
            try
            {
                var car = root.AddComponent<ConstructionStreetVehicleVisual>();
                car.Configure(true, 0.25f);
                Assert.That(car.transform.localPosition.z,
                    Is.EqualTo(0f).Within(0.001f));
                Assert.That(car.transform.localScale.x, Is.GreaterThan(0.99f));
                car.Configure(false, 0.5f);
                Assert.That(car.transform.localPosition.x,
                    Is.EqualTo(0f).Within(0.001f));
                Assert.That(car.transform.localScale.x, Is.Zero,
                    "A direction change should restart at an invisible entrance, not jump across the road.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoadsideWildlifeCautionSlowsCarWithoutReversingOrChangingPassTime()
        {
            Assert.That(ConstructionStreetVehicleVisual.PassageProgress(0f, true),
                Is.Zero);
            Assert.That(ConstructionStreetVehicleVisual.PassageProgress(1f, true),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ConstructionStreetVehicleVisual.PassageProgress(0.5f, true),
                Is.EqualTo(0.5f).Within(0.0001f));
            var previous = 0f;
            for (var step = 1; step <= 100; step++)
            {
                var progress = ConstructionStreetVehicleVisual.PassageProgress(
                    step / 100f, true);
                Assert.That(progress, Is.GreaterThan(previous));
                previous = progress;
            }
            var centreTravel = ConstructionStreetVehicleVisual.PassageProgress(
                0.55f, true) - ConstructionStreetVehicleVisual.PassageProgress(
                0.45f, true);
            Assert.That(centreTravel, Is.LessThan(0.1f));
        }

        [Test]
        public void StreetCarRandomEntryUsesOnlyAvailableAxes()
        {
            var random = new System.Random(49);
            var allSides = Enumerable.Range(0, 100)
                .Select(_ => ConstructionStreetVehicleVisual.PickEntrySide(
                    true, true, random)).Distinct().ToArray();
            Assert.That(allSides, Is.EquivalentTo(new[]
            {
                StreetVehicleEntrySide.North, StreetVehicleEntrySide.East,
                StreetVehicleEntrySide.South, StreetVehicleEntrySide.West
            }));
            var northSouthOnly = Enumerable.Range(0, 50)
                .Select(_ => ConstructionStreetVehicleVisual.PickEntrySide(
                    false, true, random)).Distinct().ToArray();
            Assert.That(northSouthOnly, Is.EquivalentTo(new[]
            {
                StreetVehicleEntrySide.North, StreetVehicleEntrySide.South
            }));
        }

        [Test]
        public void FillAndEndlessUseDifferentSaveFiles()
        {
            Assert.That(ConstructionRunStore.PathFor(ConstructionGameMode.Fill),
                Is.Not.EqualTo(ConstructionRunStore.PathFor(
                    ConstructionGameMode.Endless)));
        }

        [Test]
        public void GreenBuildAutomaticallyChoosesVegetationAndPreviewMatchesBuild()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.ResolveGreenPlanting(4, 3),
                Is.EqualTo(GreenPlanting.Meadow));
            Assert.That(run.CheckPlacement(4, 3, ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.TryPreviewBuildImpact(4, 3,
                ConstructionCategory.Green, GreenPlanting.None, out var impact),
                Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.None, out var meadow, out _, out _), Is.True);
            Assert.That(meadow.planting, Is.EqualTo(GreenPlanting.Meadow));
            Assert.That(run.PreviewEcologyDay().AnimalMeals,
                Is.EqualTo(impact.animalMealsAfter));

            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.ResolveGreenPlanting(3, 2),
                Is.EqualTo(GreenPlanting.Oak));
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.None, out var oak, out _, out _), Is.True);
            Assert.That(oak.planting, Is.EqualTo(GreenPlanting.Oak));
            Assert.That(run.ResolveGreenPlanting(4, 2),
                Is.EqualTo(GreenPlanting.Shrub),
                "Two edge-connected green plots produce shrubs.");
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var shrub = Build(run, 4, 2, ConstructionCategory.Green,
                GreenPlanting.None);
            Assert.That(shrub.planting, Is.EqualTo(GreenPlanting.Shrub));
            Assert.That(run.ResolveGreenPlanting(5, 2),
                Is.EqualTo(GreenPlanting.Meadow),
                "A lone neighbouring shrub does not produce another shrub.");
        }

        [Test]
        public void BuildImpactUsesActualDayPreviewsWithoutChangingTheRun()
        {
            var run = LegacyFillRun();
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
        public void ResidenceRoutePreviewDoesNotBuildOrSpendTheDay()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.TryPreviewResidenceRoutes(3, 2,
                out var work, out var meal), Is.True);
            Assert.That(work, Is.False);
            Assert.That(meal, Is.False);
            Assert.That(run.BuiltCount, Is.EqualTo(1));
            Assert.That(run.At(3, 2), Is.Null);
            Assert.That(run.BuiltToday, Is.False);

            Build(run, 3, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.TryPreviewResidenceRoutes(2, 1,
                out work, out meal), Is.True);
            Assert.That(work, Is.True);
            Assert.That(meal, Is.True);
            Assert.That(run.At(2, 1), Is.Null);
            Assert.That(run.BuiltToday, Is.False);
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
        public void EndlessRunKeepsBuildingAfterDay49AndUsesSeparateModeSave()
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
            Assert.That(run.BoardCapacity, Is.EqualTo(int.MaxValue));
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
        public void EndlessUsesNoTypeCapsButEveryRoomMustTouchTheBuiltSettlement()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            foreach (ConstructionCategory category in Enum.GetValues(
                         typeof(ConstructionCategory)))
            {
                Assert.That(run.CategoryLimit(category), Is.EqualTo(int.MaxValue));
                Assert.That(run.IsCategoryAtLimit(category), Is.False);
            }
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var category in new[]
                     {
                         ConstructionCategory.Green,
                         ConstructionCategory.Residence,
                         ConstructionCategory.Square
                     })
            {
                Assert.That(run.CheckPlacement(0, 0, category,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Meadow : GreenPlanting.None),
                    Is.EqualTo(ConstructionPlacementFailure.Disconnected));
                Assert.That(run.CheckPlacement(4, 3, category,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Meadow : GreenPlanting.None),
                    Is.EqualTo(ConstructionPlacementFailure.None));
            }
            Assert.That(run.TryPreviewBuildImpact(0, 0,
                ConstructionCategory.Green, GreenPlanting.Meadow,
                out _), Is.False);
            Assert.That(run.TryBuild(0, 0, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out var failure,
                out var placement), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
            Assert.That(placement, Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(run.BuiltCount, Is.EqualTo(1));
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.CheckPlacement(5, 3, ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.CheckPlacement(2, 3, ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet),
                "Touching the oak does not replace a service room's own rule.");

            var fill = new ConstructionRunModel(ConstructionGameMode.Fill);
            Assert.That(fill.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(fill.CheckPlacement(0, 0, ConstructionCategory.Green,
                GreenPlanting.Meadow),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected),
                "Fill mode also requires contact with the settlement.");
            Assert.That(fill.CheckPlacement(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow),
                Is.EqualTo(ConstructionPlacementFailure.None));
        }

        [Test]
        public void EndlessHasNoRingGateAndStillRequiresAnAdjacentRoom()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var column in new[] { 4, 5, 6 })
            {
                Assert.That(run.TryBuild(column, 3, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out _, out var failure,
                    out var placement), Is.True, $"{failure}/{placement}");
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.BuiltCount, Is.EqualTo(4));
            Assert.That(run.ExpansionRings, Is.Zero);
            Assert.That(run.IsFull, Is.False);
            Assert.That(run.Contains(1000, -1000), Is.True);
            Assert.That(run.CheckPlacement(7, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.CheckPlacement(7, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.EqualTo(
                    ConstructionPlacementFailure.Disconnected),
                "An endless cell still needs a shared edge.");
            Assert.That(run.TryBuild(7, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.CheckPlacement(7, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.At(7, 3), Is.Not.Null);
            Assert.That(restored.CheckPlacement(7, 4, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
        }

        [Test]
        public void EndlessCanCrossSeveralFormerEdgesAndRestoreWithoutAnExpansionQuota()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            for (var column = 4; column <= 12; column++)
            {
                Assert.That(run.CheckPlacement(column, 3,
                    ConstructionCategory.Green), Is.EqualTo(
                        ConstructionPlacementFailure.None));
                Assert.That(run.TryBuild(column, 3, ConstructionCategory.Green,
                    GreenPlanting.None, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.Contains(-10000, 10000), Is.True);
            Assert.That(run.ExpansionRings, Is.Zero);
            Assert.That(run.IsFull, Is.False);
            Assert.That(run.CheckPlacement(13, 3,
                ConstructionCategory.Green), Is.EqualTo(
                    ConstructionPlacementFailure.None));
            Assert.That(run.CheckPlacement(13, 4,
                ConstructionCategory.Green), Is.EqualTo(
                    ConstructionPlacementFailure.Disconnected),
                "A diagonal across the frontier does not extend construction.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.At(12, 3), Is.Not.Null);
            Assert.That(restored.CheckPlacement(13, 3,
                ConstructionCategory.Green), Is.EqualTo(
                    ConstructionPlacementFailure.None));

            var fill = new ConstructionRunModel(ConstructionGameMode.Fill);
            Assert.That(fill.CheckPlacement(7, 3,
                ConstructionCategory.Green), Is.EqualTo(
                    ConstructionPlacementFailure.OutsideBoard));
        }

        [Test]
        public void FillModeHasNoTypeCapsButStillEndsAtFortyNineCells()
        {
            // Exercise free building through an older save. The current
            // introduction is covered separately in ConstructionIntroChoiceTests.
            var run = LegacyFillRun();
            var categories = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>().ToArray();
            Assert.That(run.BoardCapacity, Is.EqualTo(49));
            foreach (var category in categories)
            {
                Assert.That(run.CategoryLimit(category), Is.EqualTo(int.MaxValue),
                    category.ToString());
                Assert.That(run.IsCategoryAtLimit(category), Is.False,
                    category.ToString());
            }
            Assert.That(new ConstructionRunModel(ConstructionGameMode.BoardGame)
                .CategoryLimit(ConstructionCategory.Green), Is.EqualTo(21),
                "The physical deck's fixed inventory remains separate from Fill.");
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(1),
                "The starting oak counts as a green room.");
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.CheckPlacement(0, 0, ConstructionCategory.Green,
                    GreenPlanting.Meadow),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected),
                "Removing type caps must not remove adjacency rules.");
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(point => point != (3, 3))
                .OrderBy(point => Math.Abs(point.column - 3) +
                    Math.Abs(point.row - 3)).ToArray();
            foreach (var point in positions)
            {
                Build(run, point.column, point.row, ConstructionCategory.Green,
                    GreenPlanting.Meadow);
                Assert.That(run.TrySimulateDay(out _), Is.True);
                if (run.CategoryBuiltCount(ConstructionCategory.Green) == 21)
                {
                    Assert.That(run.IsCategoryAtLimit(ConstructionCategory.Green), Is.False);
                    Assert.That(run.BuiltCount, Is.LessThan(49),
                        "The previous 21-green cap must no longer end Fill.");
                }
            }
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(49));
            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.IsFull, Is.True);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.TryBuild(7, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out var failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.RunFinished));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored),
                Is.True);
            Assert.That(restored.BuiltCount, Is.EqualTo(49));
            Assert.That(restored.CategoryBuiltCount(ConstructionCategory.Green),
                Is.EqualTo(49));
        }

        [Test]
        public void EndlessCanBuildOnDayOneWithoutTheOakTutorial()
        {
            var endless = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(endless.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));
            foreach (ConstructionCategory category in Enum.GetValues(
                         typeof(ConstructionCategory)))
                Assert.That(endless.IsUnlocked(category), Is.True, category.ToString());
            Assert.That(endless.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out var failure, out var placement),
                Is.True, $"{failure}/{placement}");
            Assert.That(endless.CurrentDay, Is.EqualTo(1));
            Assert.That(endless.BuiltCount, Is.EqualTo(2));
            Assert.That(ConstructionRunModel.TryRestore(endless.Export(),
                out var restored), Is.True);
            Assert.That(restored.IsUnlocked(ConstructionCategory.Workshop), Is.True);
            Assert.That(restored.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));

            var fill = new ConstructionRunModel(ConstructionGameMode.Fill);
            Assert.That(fill.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.InspectOak));
            Assert.That(fill.IsUnlocked(ConstructionCategory.Residence), Is.False);
            Assert.That(fill.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.LockedByStory));
        }

        [Test]
        public void EndlessUpgradesAndCoexistenceStreakSurviveReload()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            var oakId = ConstructionBoardModel.StarterOakId;
            Assert.That(run.EndlessCoexistenceProgress, Is.EqualTo((0, 0, 1)));
            Assert.That(run.UsesNeighborUpgrades, Is.True);
            Assert.That(run.UsesPlacementScore, Is.False);

            Build(run, 3, 4, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.GreenUpgradeLevel(oakId), Is.EqualTo(2));
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 3, 5, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(run.GreenUpgradeLevel(oakId), Is.EqualTo(3));
            Assert.That(run.FoodCapacity(oakId), Is.EqualTo(5));
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);

            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var diner = Build(run, 5, 3, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.EndlessCoexistenceProgress, Is.EqualTo((1, 1, 3)));
            Build(run, 5, 4, ConstructionCategory.Square);
            Assert.That(ConstructionHumanModel.DailyMealCapacity(diner,
                run.BuiltTiles, run.UsesNeighborUpgrades), Is.EqualTo(3));
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.EndlessCoexistenceProgress, Is.EqualTo((2, 2, 3)));
            var stock = run.FoodStock(oakId);
            Assert.That(stock, Is.GreaterThan(run.FoodCapacity(GreenPlanting.Oak)),
                "The save must contain stock that only upgraded storage can hold.");

            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(
                JsonUtility.ToJson(run.Export()));
            Assert.That(saved.endlessUpgradeRulesVersion, Is.EqualTo(1));
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.UsesNeighborUpgrades, Is.True);
            Assert.That(restored.FoodStock(oakId), Is.EqualTo(stock));
            Assert.That(restored.FoodCapacity(oakId), Is.EqualTo(5));
            Assert.That(restored.EndlessCoexistenceProgress, Is.EqualTo((2, 2, 3)));
            Build(restored, 2, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(restored.LastHumanDay.CompletedWorkCycles, Is.Zero,
                "Without waste collection, the home's accumulated waste blocks its cycle.");
            Assert.That(restored.EndlessCoexistenceProgress, Is.EqualTo((0, 2, 3)));
        }

        [Test]
        public void OldEndlessSaveRemainsOnBaseGreenCapacity()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            var oakId = ConstructionBoardModel.StarterOakId;
            Build(run, 3, 4, ConstructionCategory.Green, GreenPlanting.Oak);
            var json = Regex.Replace(JsonUtility.ToJson(run.Export()),
                ",\"endlessUpgradeRulesVersion\":\\d+", string.Empty);
            Assert.That(json, Does.Not.Contain("\"endlessUpgradeRulesVersion\""));

            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(json);
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.UsesNeighborUpgrades, Is.False);
            Assert.That(restored.UsesGreenUpgrades, Is.False);
            Assert.That(restored.FoodCapacity(oakId), Is.EqualTo(3));
            Assert.That(restored.BuiltCount, Is.EqualTo(2));
            Assert.That(restored.BuiltToday, Is.True);
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(restored.Export().endlessUpgradeRulesVersion, Is.Zero);
        }

        [Test]
        public void EndlessRequiresOnePlacementPerDayAndKeepsTheGateOnReload()
        {
            var endless = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(endless.CanAdvanceConstructionDay, Is.False);
            Assert.That(endless.TrySimulateConstructionDay(out _), Is.False);
            Build(endless, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(endless.CanAdvanceConstructionDay, Is.True);
            Assert.That(endless.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(endless.CurrentDay, Is.EqualTo(2));
            Assert.That(endless.BuiltToday, Is.False);
            Assert.That(endless.CanAdvanceConstructionDay, Is.False);
            Assert.That(ConstructionRunModel.TryRestore(endless.Export(),
                out var restored), Is.True);
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.False);
            Build(restored, 5, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Assert.That(restored.CanAdvanceConstructionDay, Is.True);
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(restored.CurrentDay, Is.EqualTo(3));
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
        public void EndlessLossLimitsUseRecommendedDefaultsAndPersistOnlyInEndless()
        {
            var endless = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(endless.AnimalDeathLimit,
                Is.EqualTo(ConstructionRunModel.RecommendedAnimalDeathLimit));
            Assert.That(endless.UnemployedResidentLimit,
                Is.EqualTo(ConstructionRunModel.RecommendedUnemployedResidentLimit));
            Assert.That(endless.EndlessLossRulesConfirmed, Is.False);
            Assert.That(endless.ConfigureEndlessLossRules(0, 1), Is.False);
            Assert.That(endless.ConfigureEndlessLossRules(2, 3), Is.True);
            Assert.That(endless.EndlessLossRulesConfirmed, Is.True);
            Assert.That(endless.ConfigureEndlessLossRules(3, 1), Is.False,
                "Opening limits stay fixed so the unemployment streak cannot be reset.");
            Assert.That(endless.AnimalDeathLimit, Is.EqualTo(2));
            Assert.That(endless.UnemployedResidentLimit, Is.EqualTo(3));

            var json = JsonUtility.ToJson(endless.Export());
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(json),
                out var restored), Is.True);
            Assert.That(restored.AnimalDeathLimit, Is.EqualTo(2));
            Assert.That(restored.UnemployedResidentLimit, Is.EqualTo(3));
            Assert.That(restored.EndlessLossRulesConfirmed, Is.True);

            var fill = new ConstructionRunModel(ConstructionGameMode.Fill);
            Assert.That(fill.ConfigureEndlessLossRules(1, 1), Is.False);
            Assert.That(fill.TrySimulateFillDay(out _), Is.True);
            Assert.That(fill.EndReason, Is.EqualTo(ConstructionEndReason.None));
        }

        [Test]
        public void OldEndlessJsonWithoutLossFieldsRestoresUnconfirmedRecommendations()
        {
            var json = JsonUtility.ToJson(
                new ConstructionRunModel(ConstructionGameMode.Endless).Export());
            foreach (var field in new[]
                     {
                         "endlessLossRulesVersion", "animalDeathLimit",
                         "unemployedResidentLimit", "endlessLossRulesConfirmed",
                         "animalDeathDays", "unemployedResidentsLastDay",
                         "unemployedDaysInRow"
                     })
            {
                json = Regex.Replace(json, ",\"" + field +
                    "\":(?:-?\\d+|true|false|\\[\\])", string.Empty);
                Assert.That(json, Does.Not.Contain("\"" + field + "\""));
            }

            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(json),
                out var restored), Is.True, json);
            Assert.That(restored.EndlessLossRulesConfirmed, Is.False);
            Assert.That(restored.AnimalDeathLimit,
                Is.EqualTo(ConstructionRunModel.RecommendedAnimalDeathLimit));
            Assert.That(restored.UnemployedResidentLimit,
                Is.EqualTo(ConstructionRunModel.RecommendedUnemployedResidentLimit));
            Assert.That(restored.RecentAnimalDeaths, Is.Zero);
            Assert.That(restored.UnemployedResidentsLastDay, Is.Zero);
            Assert.That(restored.UnemployedDaysInRow, Is.Zero);
        }

        [Test]
        public void EndlessTrafficDeathCountsTowardPlayerLimitAfterDailyScore()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(1, 20), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var street = Build(run, 3, 2, ConstructionCategory.Street);

            ConstructionEcologyDayResult fatal = null;
            for (var day = 0; day < 64; day++)
            {
                Assert.That(run.TrySimulateConstructionDay(out var settled), Is.True);
                if (settled.trafficSpecies != ConstructionTrafficSpecies.Squirrel)
                    continue;
                fatal = settled;
                break;
            }
            Assert.That(fatal, Is.Not.Null,
                "The deterministic adjacent street must eventually strike the squirrel.");
            Assert.That(run.StreetTrafficDeathCount(street.id), Is.EqualTo(1));
            Assert.That(run.RecentAnimalDeaths, Is.EqualTo(1));
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.EndReason,
                Is.EqualTo(ConstructionEndReason.AnimalDeathsLimit));
            Assert.That(run.ConfigureEndlessLossRules(2, 20), Is.False,
                "A settled loss cannot be escaped by changing the opening limit.");
            var saved = run.Export();
            Assert.That(saved.score.days.Count, Is.EqualTo(saved.currentDay));
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.EndReason,
                Is.EqualTo(ConstructionEndReason.AnimalDeathsLimit));
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.False,
                "The ending day must not be scored a second time.");
        }

        [Test]
        public void StarvedPigeonCountsAsOneDeathButLastBirdLeavingDoesNot()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(1, 20), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));

            ConstructionEcologyDayResult finalDay = null;
            for (var day = 0; day < 10 && !run.IsFinished; day++)
                Assert.That(run.TrySimulateConstructionDay(out finalDay), Is.True);

            Assert.That(run.IsFinished, Is.True,
                "One meadow's alternate food yield cannot feed two pigeons every day.");
            Assert.That(finalDay.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.None));
            Assert.That(finalDay.trafficSpecies,
                Is.EqualTo(ConstructionTrafficSpecies.None));
            Assert.That(run.EndReason,
                Is.EqualTo(ConstructionEndReason.AnimalDeathsLimit));
            Assert.That(run.PigeonPopulation, Is.Zero);
            Assert.That(run.RecentAnimalDeaths, Is.EqualTo(1),
                "One pigeon starves; the lone survivor leaves and is not a death.");
        }

        [Test]
        public void EndlessDeathWindowDropsAnEventAfterSevenSettledDays()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var save = run.Export();
            save.survival.animalDeathDays.Add(1);
            Assert.That(ConstructionRunModel.TryRestore(save, out var restored),
                Is.True);
            Assert.That(restored.RecentAnimalDeaths, Is.EqualTo(1));
            for (var day = 2; day <= 7; day++)
            {
                Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
                Assert.That(restored.RecentAnimalDeaths, Is.EqualTo(1),
                    $"Day 1 remains in the seven-day window through day {day}.");
            }
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(restored.RecentAnimalDeaths, Is.Zero);
        }

        [Test]
        public void EndlessUnemploymentCountsActualWorkAndNeedsThreeConsecutiveDays()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 1), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.UnemployedResidentsLastDay, Is.EqualTo(1));
            Assert.That(run.UnemployedDaysInRow, Is.EqualTo(1));
            Assert.That(run.IsFinished, Is.False);

            Build(run, 5, 3, ConstructionCategory.Workshop);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.LastHumanDay.WorkedCount, Is.EqualTo(1));
            Assert.That(run.LastHumanDay.MealsEaten, Is.Zero,
                "A resident can work even while a restaurant is missing.");
            Assert.That(run.UnemployedResidentsLastDay, Is.Zero,
                "Missing a meal must not be mislabeled as unemployment.");
            Assert.That(run.UnemployedDaysInRow, Is.Zero);

            Build(run, 3, 4, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.UnemployedResidentsLastDay, Is.EqualTo(1));
            Assert.That(run.UnemployedDaysInRow, Is.EqualTo(1));
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.UnemployedDaysInRow, Is.EqualTo(2));
            Assert.That(run.IsFinished, Is.False);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.UnemployedDaysInRow, Is.EqualTo(3));
            Assert.That(run.EndReason,
                Is.EqualTo(ConstructionEndReason.ResidentsUnemployed));
            var saved = run.Export();
            Assert.That(saved.score.days.Count, Is.EqualTo(saved.currentDay));
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.EndReason,
                Is.EqualTo(ConstructionEndReason.ResidentsUnemployed));
            Assert.That(restored.TrySimulateConstructionDay(out _), Is.False);
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
        public void AdjacentPlotsArePlayerChosenButServiceAndStreetLinksStillMatter()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.CheckPlacement(6, 6, ConstructionCategory.Green,
                GreenPlanting.Oak),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(0, 0, ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(6, 0, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(3, 4, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(board.CheckPlacement(4, 4, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Workshop),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(board.CheckPlacement(4, 3, ConstructionCategory.Waste),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrRestaurant));

            Build(board, 4, 3, ConstructionCategory.Residence);
            var isolatedMeadow = Build(board, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                isolatedMeadow.id), Is.Empty,
                "Physical settlement adjacency through a home does not join green habitat.");
            Assert.That(board.CheckPlacement(4, 4, ConstructionCategory.Restaurant),
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
            Assert.That(restored.At(5, 3)?.id, Is.EqualTo(isolatedMeadow.id));
        }

        [Test]
        public void GreenComponentAndResidentialAdjacencyAreDifferentCounts()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var greenA = Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            Build(board, 5, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Build(board, 2, 3, ConstructionCategory.Green, GreenPlanting.Shrub);

            Assert.That(board.AdjacentGreenCount(home.id), Is.EqualTo(3));
            Assert.That(board.ConnectedGreenCount(greenA.id), Is.EqualTo(4));
            Assert.That(board.TouchesResidence(greenA.id), Is.True,
                "A home touching a green tile at the corner is adjacent.");
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
                { ConstructionBoardModel.StarterOakId, first.id, turn.id,
                    target.id }));

            Build(board, 5, 3, ConstructionCategory.Residence);
            var isolated = Build(board, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                isolated.id), Is.Empty,
                "A resident room must not bridge an animal route.");
        }

        [Test]
        public void AnimalRouteCrossesBuiltPublicTilesWithoutMergingGreenHabitat()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Street,
                GreenPlanting.None, out var street, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(6, 3, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(6, 4, ConstructionCategory.Waste,
                GreenPlanting.None, out var waste, out _, 2,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(6, 5, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, 1,
                allowIntroPlacement: true), Is.True);

            Assert.That(board.AnimalRouteBetween(
                ConstructionBoardModel.StarterOakId, meadow.id, 1), Is.Empty,
                "A room built tomorrow cannot carry today's animal route.");
            Assert.That(board.AnimalRouteBetween(
                    ConstructionBoardModel.StarterOakId, meadow.id, 2)
                .Select(tile => tile.id), Is.EqualTo(new[]
                {
                    ConstructionBoardModel.StarterOakId, square.id, street.id,
                    market.id, waste.id, meadow.id
                }));
            Assert.That(board.AnimalReachableGreenTiles(
                    ConstructionBoardModel.StarterOakId, 2)
                .Select(tile => tile.id), Does.Contain(meadow.id));
            Assert.That(board.ConnectedGreenTiles(
                ConstructionBoardModel.StarterOakId).Count, Is.EqualTo(1),
                "Foraging access must not create a new green habitat component.");
        }

        [TestCase(ConstructionCategory.Residence)]
        [TestCase(ConstructionCategory.Workshop)]
        [TestCase(ConstructionCategory.Restaurant)]
        public void AnimalRouteCannotCrossHumanOnlyRoom(
            ConstructionCategory blocker)
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, blocker, GreenPlanting.None,
                out _, out _, 1, allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.AnimalRouteBetween(
                ConstructionBoardModel.StarterOakId, meadow.id), Is.Empty);
            Assert.That(board.AnimalReachableGreenTiles(
                    ConstructionBoardModel.StarterOakId)
                .Select(tile => tile.id), Does.Not.Contain(meadow.id));
        }

        [Test]
        public void SquareIsHumanVisitDestinationButNeverHumanStreetTransit()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 3, 2, ConstructionCategory.Residence);
            var street = Build(board, 4, 2, ConstructionCategory.Street);
            var square = Build(board, 5, 2, ConstructionCategory.Square);
            Assert.That(board.ResidenceServiceRouteTo(home.id, square.id)
                .Select(tile => tile.id), Is.EqualTo(new[]
                { home.id, street.id, square.id }));
            Assert.That(board.TryBuild(6, 2, ConstructionCategory.Workshop,
                GreenPlanting.None, out var workshop, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.ResidenceServiceRouteTo(home.id, workshop.id),
                Is.Empty, "A plaza cannot extend a human street route.");
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

            Build(board, 6, 2, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Any(tile => tile.planting == GreenPlanting.Meadow), Is.False);
            Build(board, 4, 2, ConstructionCategory.Green, GreenPlanting.Oak);
            Build(board, 5, 2, ConstructionCategory.Green, GreenPlanting.Oak);
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
        public void EightRoomGuideCountsActualBuildsWithoutForcingAnimalEventsOrPlots()
        {
            var board = new ConstructionBoardModel();
            Assert.That(ConstructionTutorialGuide.FromBoard(board, false),
                Is.EqualTo(ConstructionTutorialChapter.InspectOak));
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Green),
                "The supplied oak is not the player's first green build.");
            Build(board, 2, 3, ConstructionCategory.Square);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Green),
                "Optional early builds remain allowed and count later.");
            Build(board, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Residence));
            Build(board, 4, 2, ConstructionCategory.Residence);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Restaurant));
            Build(board, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Workshop));
            Build(board, 3, 2, ConstructionCategory.Workshop);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Street));
            Build(board, 4, 1, ConstructionCategory.Street);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Waste));
            Build(board, 5, 1, ConstructionCategory.Waste);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Supermarket));
            Build(board, 3, 1, ConstructionCategory.Supermarket);
            Assert.That(ConstructionTutorialGuide.FromBoard(board, true),
                Is.EqualTo(ConstructionTutorialChapter.Complete),
                "The square built early must be counted when its chapter arrives.");
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
            Assert.That(story.HomesWithRestaurantRoute, Is.EqualTo(2),
                "Both homes already reach a restaurant here; the street is still unused.");
            Assert.That(story.LaterHomeUsesStreet, Is.False);
            foreach (var position in new[]
                     { (6, 1), (6, 0), (5, 0), (4, 0), (3, 0), (3, 1) })
                Build(board, position.Item1, position.Item2,
                    ConstructionCategory.Street);
            Assert.That(board.ResidenceServiceRoute(board.At(6, 2).id,
                ConstructionCategory.Workshop).Any(tile =>
                tile.category == ConstructionCategory.Street), Is.True);
            Assert.That(story.Observe(board), Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(story.HomesWithRestaurantRoute, Is.EqualTo(2));
            Assert.That(story.LaterHomeUsesStreet, Is.True);
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
            var run = LegacyFillRun();
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
            var run = LegacyFillRun();
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
            Assert.That(restored.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Restaurant),
                "The eight-room guide must reconstruct from confirmed board state.");

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
            var run = LegacyFillRun();
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
            var run = LegacyFillRun();
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
            var run = LegacyFillRun();
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
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out var opening), Is.True);
            var observedMeals = opening.AnimalMeals;
            var observedWorkdays = run.LastScoreDay.completedWorkCycles;
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
                observedWorkdays += run.LastScoreDay.completedWorkCycles;
            }
            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.FinalResult.DaysPlayed, Is.EqualTo(49));
            Assert.That(run.FinalResult.AnimalMealsTotal, Is.EqualTo(observedMeals));
            Assert.That(run.FinalResult.HumanWorkdaysTotal,
                Is.EqualTo(observedWorkdays));
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
            var run = LegacyFillRun();
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
            var run = LegacyFillRun();
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
                Assert.That(store.TryLoadDetailed(out _),
                    Is.EqualTo(ConstructionSaveLoadStatus.Missing));
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.None));
                var run = LegacyFillRun();
                run.RecordSquirrelMeal();
                Build(run, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
                Assert.That(run.TryEndDay(1, 0), Is.True);
                Assert.That(store.TrySave(run), Is.True);
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.InProgress));
                Assert.That(store.StatusFor(ConstructionGameMode.Endless),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt),
                    "A save for the wrong mode must not be silently overwritten.");
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
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.None));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void MenuStatusRejectsPartialDayResultsWithoutCrashing()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-partial-save-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            try
            {
                var ecology = new ConstructionRunModel().Export();
                ecology.lastEcologyDay = new ConstructionEcologyDayResult { day = 1 };
                File.WriteAllText(path, JsonUtility.ToJson(ecology));
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt));

                var human = new ConstructionRunModel().Export();
                human.lastHumanDay = new ConstructionHumanDayResult { day = 1 };
                File.WriteAllText(path, JsonUtility.ToJson(human));
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void InvalidConstructionSaveCannotBeOverwrittenOrDeletedAndCanBeArchived()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-invalid-save-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            var original = "{invalid json";
            string archivedPath = null;
            try
            {
                File.WriteAllText(path, original);
                Assert.That(store.TryLoadDetailed(out _),
                    Is.EqualTo(ConstructionSaveLoadStatus.Invalid));
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt));
                Assert.That(store.TrySave(new ConstructionRunModel()), Is.False);
                Assert.That(store.TryDelete(), Is.False);
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));

                Assert.That(store.TryArchiveInvalid(ConstructionGameMode.Fill,
                    out archivedPath), Is.True);
                Assert.That(File.Exists(path), Is.False);
                Assert.That(File.ReadAllText(archivedPath), Is.EqualTo(original));
                Assert.That(store.TrySave(new ConstructionRunModel()), Is.True);
                Assert.That(store.TryLoadDetailed(out _),
                    Is.EqualTo(ConstructionSaveLoadStatus.Loaded));
                Assert.That(File.ReadAllText(archivedPath), Is.EqualTo(original));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (archivedPath != null && File.Exists(archivedPath))
                    File.Delete(archivedPath);
            }
        }

        [Test]
        public void HugeTrafficDeathCountsAreInvalidWithoutOverflowOrReplacingSave()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 20), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var firstStreet = Build(run, 3, 2, ConstructionCategory.Street);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var secondStreet = Build(run, 2, 2, ConstructionCategory.Street);
            var saved = run.Export();
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.True);

            // Both tile IDs are valid and distinct, so validation reaches the
            // total-count check instead of rejecting the list earlier.
            saved.survival.trafficDeaths.Clear();
            saved.survival.trafficDeaths.Add(new ConstructionTrafficDeathSaveData
                { streetTileId = firstStreet.id, count = int.MaxValue });
            saved.survival.trafficDeaths.Add(new ConstructionTrafficDeathSaveData
                { streetTileId = secondStreet.id, count = int.MaxValue });
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.False);

            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-overflow-save-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            var original = JsonUtility.ToJson(saved);
            try
            {
                File.WriteAllText(path, original);
                Assert.That(store.TryLoadDetailed(out _),
                    Is.EqualTo(ConstructionSaveLoadStatus.Invalid));
                Assert.That(store.StatusFor(ConstructionGameMode.Endless),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt));
                Assert.That(store.TrySave(run), Is.False);
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void WrongModeConstructionSaveIsPreservedUntilExplicitArchive()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-wrong-mode-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            string archivedPath = null;
            try
            {
                var endless = new ConstructionRunModel(ConstructionGameMode.Endless);
                Assert.That(store.TrySave(endless), Is.True);
                var original = File.ReadAllText(path);
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.Corrupt));
                Assert.That(store.TrySave(new ConstructionRunModel()), Is.False);
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                Assert.That(store.TryArchiveInvalid(ConstructionGameMode.Fill,
                    out archivedPath), Is.True);
                Assert.That(File.ReadAllText(archivedPath), Is.EqualTo(original));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (archivedPath != null && File.Exists(archivedPath))
                    File.Delete(archivedPath);
            }
        }

        [Test]
        public void UnavailableConstructionSavePathCannotBeReplacedArchivedOrDeleted()
        {
            // A directory at the save path is a deterministic unreadable path
            // on desktop platforms. Keep a marker inside to prove that every
            // store operation leaves the existing path untouched.
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-unavailable-save-" + Guid.NewGuid().ToString("N") + ".json");
            var marker = Path.Combine(path, "keep.txt");
            var store = new ConstructionRunStore(path);
            try
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(marker, "preserve this path");
                Assert.That(store.TryLoadDetailed(out _),
                    Is.EqualTo(ConstructionSaveLoadStatus.Unavailable));
                Assert.That(store.StatusFor(ConstructionGameMode.Fill),
                    Is.EqualTo(ConstructionSavedRunStatus.Unavailable));
                Assert.That(store.TrySave(new ConstructionRunModel()), Is.False);
                Assert.That(store.TryDelete(), Is.False);
                Assert.That(store.TryArchiveInvalid(ConstructionGameMode.Fill,
                    out var archivedPath), Is.False);
                Assert.That(archivedPath, Is.Null);
                Assert.That(File.ReadAllText(marker), Is.EqualTo("preserve this path"));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally
            {
                if (File.Exists(marker)) File.Delete(marker);
                if (Directory.Exists(path)) Directory.Delete(path);
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

        [Test]
        public void PigeonMeadowRemainsInspectableAfterBuildingAResidence()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var meadow = Build(run, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.PigeonTileId, Is.EqualTo(meadow.id));
            Build(run, 3, 2, ConstructionCategory.Residence);
            Assert.That(run.StoryStage,
                Is.EqualTo(ConstructionStoryStage.BuildRestaurant));
            Assert.That(run.InspectPigeonMeadow(meadow.id), Is.True,
                "A later room build must not close an existing wildlife observation.");
            Assert.That(run.PigeonMeadowInspected, Is.True);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PigeonMeadowInspected, Is.True);
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
            // A deliberate mixed composition keeps this score fixture stable
            // after the Fill type caps were removed.
            if (run.CategoryBuiltCount(ConstructionCategory.Green) < 21)
                return (ConstructionCategory.Green, GreenPlanting.Meadow);
            if (run.CategoryBuiltCount(ConstructionCategory.Residence) < 12)
                return (ConstructionCategory.Residence, GreenPlanting.None);
            // Add five functional rooms before completing with squares.
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

        private static ConstructionRunModel LegacyFillRun()
        {
            var save = new ConstructionRunModel().Export();
            save.introRulesVersion = 0;
            Assert.That(ConstructionRunModel.TryRestore(save, out var run), Is.True);
            return run;
        }
    }
}
