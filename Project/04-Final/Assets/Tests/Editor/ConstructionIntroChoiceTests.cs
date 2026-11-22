using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionIntroChoiceTests
    {
        [Test]
        public void DayOneReservesTheOakAndDayTwoRequiresGreen()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill);
            var oak = run.At(ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            Assert.That(oak.id, Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(run.CurrentDay, Is.EqualTo(1));
            Assert.That(run.IntroTypesBuilt, Is.Zero,
                "The supplied oak must not use the player's Green introduction.");
            Assert.That(run.IsIntroTypeUsed(ConstructionCategory.Green), Is.False);
            Assert.That(run.IsIntroCategoryAvailable(ConstructionCategory.Green),
                Is.False, "The first day is reserved for observing the oak.");
            Assert.That(run.TryBuild(0, 0, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out var failure, out _), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.LockedByStory));

            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.IsIntroPhase, Is.True);
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Green));
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Green));
            foreach (ConstructionCategory category in Enum.GetValues(
                         typeof(ConstructionCategory)))
            {
                Assert.That(run.IsIntroCategoryAvailable(category),
                    Is.EqualTo(category == ConstructionCategory.Green),
                    category.ToString());
                Assert.That(run.CheckPlacement(4, 3, category),
                    Is.EqualTo(category == ConstructionCategory.Green
                        ? ConstructionPlacementFailure.None
                        : ConstructionPlacementFailure.IntroTypeNotNext));
            }
        }

        [TestCase(2, 3)]
        [TestCase(4, 3)]
        [TestCase(3, 2)]
        [TestCase(3, 4)]
        public void FirstGreenMustShareAnEdgeWithTheStarterOak(
            int column, int row)
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.CheckPlacement(column, row,
                ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.TryBuild(column, row, ConstructionCategory.Green,
                GreenPlanting.None, out var tile, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
            Assert.That(tile.column, Is.EqualTo(column));
            Assert.That(tile.row, Is.EqualTo(row));
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Residence));
        }

        [TestCase(4, 4)] // A diagonal is in range for effects, not construction.
        [TestCase(0, 0)] // A distant tile must also be unavailable.
        public void FirstGreenCannotBePlacedWithoutAnEdgeConnection(
            int column, int row)
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.CheckPlacement(column, row,
                ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(run.TryBuild(column, row, ConstructionCategory.Green,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.False);
            Assert.That(failure,
                Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
            Assert.That(placement,
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(run.At(column, row), Is.Null);
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Green));
            Assert.That(run.BuiltToday, Is.False);
        }

        [Test]
        public void HistoricalDiagonalFirstGreenLoadsButCurrentRoutesUseSharedEdges()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                    GreenPlanting.None, out var firstGreen, out var failure,
                    out var placement), Is.True, $"{failure}/{placement}");
            var oldSave = run.Export();
            oldSave.board.tiles.Single(tile => tile.id == firstGreen.id).row = 4;

            Assert.That(ConstructionRunModel.TryRestore(oldSave,
                out var restored), Is.True,
                "The former diagonal introduction must remain loadable.");
            Assert.That(restored.At(4, 4)?.id, Is.EqualTo(firstGreen.id));
            Assert.That(restored.GreenFoodRouteTo(
                ConstructionBoardModel.StarterOakId, firstGreen.id), Is.Empty,
                "A restored diagonal is no longer a traversable route.");
            Assert.That(restored.ConnectedGreenCount(firstGreen.id), Is.EqualTo(1));
            Assert.That(restored.TrySimulateFillDay(out _), Is.True);
            Assert.That(restored.CheckPlacement(5, 5,
                    ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(restored.CheckPlacement(5, 4,
                    ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.None));
        }

        [TestCase(ConstructionCategory.Residence)]
        [TestCase(ConstructionCategory.Restaurant)]
        [TestCase(ConstructionCategory.Workshop)]
        [TestCase(ConstructionCategory.Waste)]
        [TestCase(ConstructionCategory.Street)]
        [TestCase(ConstructionCategory.Supermarket)]
        [TestCase(ConstructionCategory.Square)]
        public void AFutureRoomCannotBeTheFirstDistantBuild(
            ConstructionCategory category)
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.TryBuild(0, 0, category, GreenPlanting.None,
                out _, out var failure, out var placement), Is.False,
                $"{category}: {failure}/{placement}");
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.LockedByStory));
            Assert.That(placement,
                Is.EqualTo(ConstructionPlacementFailure.IntroTypeNotNext));
            Assert.That(run.IntroTypesBuilt, Is.Zero);
        }

        [TestCase(ConstructionCategory.Green, 0, 0,
            ConstructionPlacementFailure.Disconnected)]
        [TestCase(ConstructionCategory.Residence, 0, 0,
            ConstructionPlacementFailure.Disconnected)]
        [TestCase(ConstructionCategory.Restaurant, 3, 2,
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet)]
        [TestCase(ConstructionCategory.Workshop, 3, 2,
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet)]
        [TestCase(ConstructionCategory.Waste, 3, 2,
            ConstructionPlacementFailure.NeedsHomeOrRestaurant)]
        [TestCase(ConstructionCategory.Street, 3, 2,
            ConstructionPlacementFailure.StreetNeedsHomeConnection)]
        [TestCase(ConstructionCategory.Supermarket, 3, 2,
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet)]
        [TestCase(ConstructionCategory.Square, 0, 0,
            ConstructionPlacementFailure.Disconnected)]
        public void EachIntroRoomKeepsItsOriginalPlacementRequirements(
            ConstructionCategory category, int invalidColumn, int invalidRow,
            ConstructionPlacementFailure expectedFailure)
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            foreach (var (earlierCategory, earlierColumn, earlierRow) in
                     ConnectedIntroduction)
            {
                if (earlierCategory == category)
                {
                    Assert.That(run.IntroRequiredCategory, Is.EqualTo(category));
                    Assert.That(run.CheckPlacement(invalidColumn, invalidRow,
                        category), Is.EqualTo(expectedFailure));
                    Assert.That(run.TryBuild(invalidColumn, invalidRow, category,
                        GreenPlanting.None, out _, out var rejected,
                        out var reason), Is.False);
                    Assert.That(rejected,
                        Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
                    Assert.That(reason, Is.EqualTo(expectedFailure));
                    Assert.That(run.BuiltToday, Is.False);
                    Assert.That(run.CheckPlacement(earlierColumn, earlierRow,
                        category), Is.EqualTo(ConstructionPlacementFailure.None));
                    return;
                }
                BuildAndAdvance(run, earlierColumn, earlierRow, earlierCategory);
            }
            Assert.Fail($"No tutorial coordinate supplied for {category}");
        }

        [Test]
        public void EightTypesUnlockInOrderThenDuplicatesUnlock()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            var choices = ConnectedIntroduction;
            for (var index = 0; index < choices.Length; index++)
            {
                var (category, column, row) = choices[index];
                Assert.That(run.CurrentDay, Is.EqualTo(index + 2));
                Assert.That(run.IntroRequiredCategory, Is.EqualTo(category));
                Assert.That(run.IsIntroCategoryAvailable(category), Is.True);
                Assert.That(run.CheckPlacement(column, row, category),
                    Is.EqualTo(ConstructionPlacementFailure.None),
                    "The requested room has a valid plot under the current lesson.");
                Assert.That(run.TryBuild(column, row, category,
                    GreenPlanting.None, out _, out var failure,
                    out var placement), Is.True,
                    $"Day {run.CurrentDay}: {category}: {failure}/{placement}");
                Assert.That(run.IntroTypesBuilt, Is.EqualTo(index + 1));
                Assert.That(run.IsIntroTypeUsed(category), Is.True);
                if (index < choices.Length - 1)
                {
                    Assert.That(run.IsIntroCategoryAvailable(category), Is.False);
                    Assert.That(run.CheckPlacement(2, 2, category),
                        Is.EqualTo(
                            ConstructionPlacementFailure.IntroTypeAlreadyBuilt));
                }
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.IntroTypesBuilt, Is.EqualTo(8));
            Assert.That(run.IntroRequiredCategory, Is.Null);
            Assert.That(run.CurrentDay, Is.EqualTo(10));
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));
            Assert.That(run.IsIntroCategoryAvailable(ConstructionCategory.Square),
                Is.True);
            Assert.That(run.WouldConnectSquareToHome(1, 4), Is.False,
                "Free play can reserve a square for a later street link.");
            Assert.That(run.CheckPlacement(1, 4, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.TryBuild(1, 4, ConstructionCategory.Square,
                GreenPlanting.None, out _, out var repeatFailure,
                out var repeatPlacement), Is.True,
                $"{repeatFailure}/{repeatPlacement}");
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square),
                Is.EqualTo(2));
        }

        [Test]
        public void RequiredSquareNeedsAnImmediateResidentRoute()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            foreach (var (category, column, row) in ConnectedIntroduction.Take(7))
                BuildAndAdvance(run, column, row, category);

            Assert.That(run.CurrentDay, Is.EqualTo(9));
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Square));
            Assert.That(run.CheckPlacement(2, 5, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet),
                "Touching a supermarket alone would teach a square with no visits.");
            Assert.That(run.TryBuild(2, 5, ConstructionCategory.Square,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.False);
            Assert.That(failure, Is.EqualTo(ConstructionBuildFailure.InvalidPlacement));
            Assert.That(placement,
                Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));
            Assert.That(run.BuiltToday, Is.False);

            Assert.That(run.CheckPlacement(2, 4, ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.WouldConnectSquareToHome(2, 4), Is.True);
            Assert.That(run.TryBuild(2, 4, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out failure,
                out placement), Is.True, $"{failure}/{placement}");
            Assert.That(run.ResidenceRouteTo(run.FirstResidentHomeTileId,
                    square.id).Select(tile => tile.id), Does.Contain(square.id),
                "The required square must be reachable; visiting still needs a completed resident cycle.");
        }

        [Test]
        public void SavedIntroductionRestoresTheNextRequiredRoom()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            BuildAndAdvance(run, 4, 3, ConstructionCategory.Green);
            BuildAndAdvance(run, 4, 4, ConstructionCategory.Residence);

            var serialized = JsonUtility.ToJson(run.Export());
            var save = JsonUtility.FromJson<ConstructionRunSaveData>(serialized);
            Assert.That(ConstructionRunModel.TryRestore(save, out var restored),
                Is.True);
            Assert.That(restored.IntroTypesBuilt, Is.EqualTo(2));
            Assert.That(restored.IsIntroPhase, Is.True);
            Assert.That(restored.CurrentDay, Is.EqualTo(4));
            Assert.That(restored.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Restaurant));
            Assert.That(restored.IsIntroCategoryAvailable(
                ConstructionCategory.Green), Is.False);
            Assert.That(restored.IsIntroCategoryAvailable(
                ConstructionCategory.Residence), Is.False);
            Assert.That(restored.IsIntroCategoryAvailable(
                ConstructionCategory.Restaurant), Is.True);
            Assert.That(restored.IsIntroCategoryAvailable(
                ConstructionCategory.Workshop), Is.False);
            Assert.That(restored.CheckPlacement(1, 1,
                ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.IntroTypeAlreadyBuilt));
            Assert.That(restored.TryBuild(1, 1, ConstructionCategory.Green,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.False);
            Assert.That(failure,
                Is.EqualTo(ConstructionBuildFailure.IntroTypeAlreadyBuilt));
            Assert.That(placement,
                Is.EqualTo(ConstructionPlacementFailure.IntroTypeAlreadyBuilt));
            Assert.That(restored.CheckPlacement(1, 1,
                ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.IntroTypeNotNext));
            Assert.That(restored.TryBuild(5, 4, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out failure, out placement), Is.True,
                $"{failure}/{placement}");
        }

        [Test]
        public void PreviouslySavedBoardWithRepeatedTypesStillRestores()
        {
            // Earlier builds allowed a second green before the other seven
            // types. Such save data must remain readable after the rule change.
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 2), Is.True);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 3), Is.True);
            Assert.That(ConstructionBoardModel.TryRestore(board.Export(),
                out var restored), Is.True);
            Assert.That(restored.BuiltTiles.Count(tile =>
                tile.category == ConstructionCategory.Green), Is.EqualTo(3));
        }

        [Test]
        public void OldSaveWithoutIntroVersionKeepsRoomChoiceButUsesCurrentPlacement()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            BuildAndAdvance(run, 4, 3, ConstructionCategory.Green);
            var json = JsonUtility.ToJson(run.Export());
            Assert.That(json, Does.Contain("\"introRulesVersion\":1,"));
            var oldJson = json.Replace("\"introRulesVersion\":1,", "");
            var oldSave = JsonUtility.FromJson<ConstructionRunSaveData>(oldJson);
            Assert.That(ConstructionRunModel.TryRestore(oldSave,
                out var restored), Is.True);
            Assert.That(restored.IsIntroPhase, Is.False);
            Assert.That(restored.CheckPlacement(0, 0,
                ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.Disconnected));
            Assert.That(restored.CheckPlacement(2, 3,
                ConstructionCategory.Square),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(restored.TryBuild(2, 3, ConstructionCategory.Square,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
        }

        [Test]
        public void EndlessNeverAppliesTheLinearRoomIntroduction()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Square,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.IsIntroCategoryAvailable(ConstructionCategory.Square),
                Is.True);
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Square,
                GreenPlanting.None, out _, out failure, out placement), Is.True,
                $"{failure}/{placement}");
        }

        [Test]
        public void ExistingSaveWithDistantGreenStillRestoresTheResidenceLesson()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
            var save = run.Export();
            var green = save.board.tiles.Single(tile => tile.buildIndex == 2);
            green.column = 0;
            green.row = 0;
            Assert.That(ConstructionRunModel.TryRestore(save, out var restored),
                Is.True, "Previously valid distant-green saves must remain readable.");
            Assert.That(restored.At(0, 0)?.category,
                Is.EqualTo(ConstructionCategory.Green));
            Assert.That(restored.TrySimulateFillDay(out _), Is.True);
            Assert.That(restored.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Residence),
                "Animal arrival must not gate the next room lesson.");
            BuildAndAdvance(restored, 1, 0, ConstructionCategory.Residence);
            Assert.That(restored.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Restaurant));
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var loadedAgain), Is.True);
            Assert.That(loadedAgain.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Restaurant));
        }

        [Test]
        public void ExistingSaveWithDistantResidenceStillRestoresAndCanBuildFood()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            BuildAndAdvance(run, 4, 3, ConstructionCategory.Green);
            Assert.That(run.TryBuild(4, 4, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
            var save = run.Export();
            var residence = save.board.tiles.Single(tile =>
                tile.category == ConstructionCategory.Residence);
            residence.column = 6;
            residence.row = 6;

            Assert.That(ConstructionRunModel.TryRestore(save, out var restored),
                Is.True, "An earlier tutorial save may contain an isolated home.");
            Assert.That(restored.At(6, 6)?.category,
                Is.EqualTo(ConstructionCategory.Residence));
            Assert.That(restored.TrySimulateFillDay(out _), Is.True);
            Assert.That(restored.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Restaurant));
            Assert.That(restored.TryBuild(5, 6, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out failure,
                out placement), Is.True, $"{failure}/{placement}");
        }

        [Test]
        public void ConnectedStreetChainLetsResidentsWorkAndEatAtRemoteRooms()
        {
            var board = new ConstructionBoardModel();
            ConstructionTileData Build(int column, int row,
                ConstructionCategory category, int day)
            {
                Assert.That(board.TryBuild(column, row, category,
                    GreenPlanting.None, out var tile, out var failure, day),
                    Is.True, $"{category} at {column},{row}: {failure}");
                return tile;
            }

            var home = Build(4, 3, ConstructionCategory.Residence, 2);
            var nearStreet = Build(5, 3, ConstructionCategory.Street, 3);
            var farStreet = Build(6, 3, ConstructionCategory.Street, 4);
            var workshop = Build(6, 2, ConstructionCategory.Workshop, 5);
            var market = Build(6, 4, ConstructionCategory.Supermarket, 6);
            Assert.That(board.ResidenceServiceRouteTo(home.id, workshop.id)
                .Select(tile => tile.id),
                Is.EqualTo(new[]
                    { home.id, nearStreet.id, farStreet.id, workshop.id }),
                "A diagonal cannot skip the far street on a resident route.");
            Assert.That(board.ResidenceServiceRouteTo(home.id, market.id)
                .Select(tile => tile.id),
                Is.EqualTo(new[]
                    { home.id, nearStreet.id, farStreet.id, market.id }));

            var day = ConstructionHumanModel.SettleDay(6, board);
            Assert.That(day.WorkedCount, Is.EqualTo(1));
            Assert.That(day.MealsEaten, Is.EqualTo(1));
            Assert.That(day.CompletedWorkCycles, Is.EqualTo(1));
            Assert.That(day.residents.Single().routeTileIds,
                Does.Contain(nearStreet.id));
            Assert.That(day.residents.Single().routeTileIds,
                Does.Contain(farStreet.id),
                "Both services need the complete edge-connected street chain.");

            Assert.That(board.CheckPlacement(5, 4, ConstructionCategory.Waste),
                Is.EqualTo(ConstructionPlacementFailure.None),
                "Waste may share an edge with a street connected to a home.");
            Build(5, 4, ConstructionCategory.Waste, 7);
        }

        private static void BuildAndAdvance(ConstructionRunModel run,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True, $"{failure}/{placement}");
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
        }

        private static readonly (ConstructionCategory category, int column,
            int row)[] ConnectedIntroduction =
        {
            (ConstructionCategory.Green, 4, 3),
            (ConstructionCategory.Residence, 4, 4),
            (ConstructionCategory.Restaurant, 5, 4),
            (ConstructionCategory.Workshop, 4, 5),
            (ConstructionCategory.Street, 3, 4),
            (ConstructionCategory.Waste, 6, 4),
            (ConstructionCategory.Supermarket, 3, 5),
            (ConstructionCategory.Square, 2, 4)
        };
    }
}
