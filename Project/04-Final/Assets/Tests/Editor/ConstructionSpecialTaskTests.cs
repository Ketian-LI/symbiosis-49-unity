using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionSpecialTaskTests
    {
        [Test]
        public void EveryTaskKindHasAnImportedCardIllustration()
        {
            var names = new[]
            {
                "commute-loop-v1", "raise-animals-v1", "build-squares-v1",
                "no-road-v1", "connected-green-v1",
                "complete-communities-v1"
            };
            foreach (var name in names)
                Assert.That(Resources.Load<Sprite>(
                    $"UI/ConstructionSpecialTasks/{name}"), Is.Not.Null,
                    $"The {name} task card needs a Sprite import.");
        }

        [Test]
        public void DigitalFillStartsWithThreeDistinctUnconfirmedTasks()
        {
            var run = NewHandRun();
            Assert.That(run.Export().specialTasks.version, Is.EqualTo(6));
            Assert.That(run.UsesSpecialTasks, Is.True);
            Assert.That(run.SpecialTasks.Count, Is.EqualTo(3));
            Assert.That(run.SpecialTasks.Select(task => task.Kind).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(run.SpecialTasks.All(task =>
                task.Progress < task.Target && task.Points > 0), Is.True);
            Assert.That(run.SpecialTasks.Where(task => task.Kind ==
                    ConstructionSpecialTaskKind.RaiseAnimals)
                .All(task => task.Target is 3 or 4), Is.True);
            Assert.That(run.SpecialTaskScore, Is.Zero);
            Assert.That(run.OverallScore, Is.EqualTo(run.BalanceScore));
            Assert.That(run.IsSpecialTaskSelectionConfirmed, Is.False);
            Assert.That(run.SpecialTasks.All(task => task.CanReplace), Is.True);

            Assert.That(new ConstructionRunModel(ConstructionGameMode.Fill)
                .UsesSpecialTasks, Is.False);
            Assert.That(new ConstructionRunModel(ConstructionGameMode.Endless)
                .SpecialTasks, Is.Empty);
            Assert.That(new ConstructionRunModel(ConstructionGameMode.BoardGame)
                .SpecialTasks, Is.Empty);
        }

        [Test]
        public void DifficultyDrawWeightsAreFiftyThirtyFiveFifteen()
        {
            var easy = ConstructionRunModel.SpecialTaskDifficultyDrawWeight(
                ConstructionSpecialTaskDifficulty.Easy);
            var medium = ConstructionRunModel.SpecialTaskDifficultyDrawWeight(
                ConstructionSpecialTaskDifficulty.Medium);
            var hard = ConstructionRunModel.SpecialTaskDifficultyDrawWeight(
                ConstructionSpecialTaskDifficulty.Hard);
            var total = easy + medium + hard;

            Assert.That(total, Is.EqualTo(20));
            Assert.That((float)easy / total, Is.EqualTo(0.5f));
            Assert.That((float)medium / total, Is.EqualTo(0.35f));
            Assert.That((float)hard / total, Is.EqualTo(0.15f));
        }

        [Test]
        public void EachTaskCanBeReplacedOnceAtOpeningThenAllAreLocked()
        {
            var run = NewHandRun();
            var before = run.SpecialTasks[0].Key;
            var beforeKind = run.SpecialTasks[0].Kind;
            Assert.That(run.SpecialTasks[0].CanReplace, Is.True);
            Assert.That(run.TryReplaceSpecialTask(0), Is.True);
            Assert.That(run.SpecialTasks[0].Key, Is.Not.EqualTo(before));
            Assert.That(run.SpecialTasks[0].Kind, Is.Not.EqualTo(beforeKind),
                "Changing only the difficulty still shows the same task card.");
            Assert.That(run.SpecialTasks[0].ReplacementUsed, Is.True);
            Assert.That(run.SpecialTasks[0].CanReplace, Is.False);
            Assert.That(run.TryReplaceSpecialTask(0), Is.False);
            Assert.That(run.SpecialTasks[1].CanReplace, Is.True);

            Assert.That(run.TryConfirmSpecialTasks(), Is.True);
            Assert.That(run.IsSpecialTaskSelectionConfirmed, Is.True);
            Assert.That(run.SpecialTasks.All(task => !task.CanReplace), Is.True);
            Assert.That(run.TryReplaceSpecialTask(1), Is.False,
                "Confirmation must permanently close every manual swap.");
            Assert.That(run.TryConfirmSpecialTasks(), Is.False);

            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.SpecialTasks[0].Key,
                Is.EqualTo(run.SpecialTasks[0].Key));
            Assert.That(restored.SpecialTasks[0].ReplacementUsed, Is.True);
            Assert.That(restored.TryReplaceSpecialTask(0), Is.False);
            Assert.That(restored.IsSpecialTaskSelectionConfirmed, Is.True);
            Assert.That(restored.TryReplaceSpecialTask(1), Is.False);
        }

        [Test]
        public void ReplacementsNeverRedisplayAnEarlierTaskKind()
        {
            var opening = NewHandRun().Export();
            for (var seed = 1; seed <= 64; seed++)
            {
                opening.specialTasks.randomState = seed;
                Assert.That(ConstructionRunModel.TryRestore(opening,
                    out var run), Is.True, $"Seed {seed}");
                var shownKinds = run.SpecialTasks.Select(task => task.Kind)
                    .ToHashSet();
                Assert.That(shownKinds.Count, Is.EqualTo(3));
                for (var slot = 0; slot < 3; slot++)
                {
                    Assert.That(run.SpecialTasks[slot].CanReplace, Is.True,
                        $"Seed {seed}, slot {slot}");
                    Assert.That(run.TryReplaceSpecialTask(slot), Is.True);
                    Assert.That(shownKinds.Add(run.SpecialTasks[slot].Kind),
                        Is.True, $"Seed {seed}, slot {slot} repeated a title.");
                }
                Assert.That(shownKinds.Count, Is.EqualTo(6));
            }
        }

        [Test]
        public void FixedTutorialDoesNotAdvanceAnyOfTheSixGoalKinds()
        {
            var taskSets = new[]
            {
                new[]
                {
                    Task(ConstructionSpecialTaskKind.AvoidStreets,
                        ConstructionSpecialTaskDifficulty.Easy, 3, 15),
                    Task(ConstructionSpecialTaskKind.RaiseAnimals,
                        ConstructionSpecialTaskDifficulty.Medium, 3, 50),
                    Task(ConstructionSpecialTaskKind.CommuteCycles,
                        ConstructionSpecialTaskDifficulty.Easy, 6, 25)
                },
                new[]
                {
                    Task(ConstructionSpecialTaskKind.BuildSquares,
                        ConstructionSpecialTaskDifficulty.Easy, 1, 20),
                    Task(ConstructionSpecialTaskKind.ConnectGreens,
                        ConstructionSpecialTaskDifficulty.Easy, 3, 25),
                    Task(ConstructionSpecialTaskKind.CompleteCommunities,
                        ConstructionSpecialTaskDifficulty.Easy, 1, 50)
                }
            };

            foreach (var cards in taskSets)
            {
                var run = RunWithTasks(cards);
                foreach (var (category, column, row) in TutorialOpening)
                {
                    Assert.That(run.TryBuild(column, row, category,
                        category == ConstructionCategory.Green
                            ? GreenPlanting.Oak : GreenPlanting.None,
                        out _, out var failure, out var placement), Is.True,
                        $"{category}: {failure}/{placement}");
                    Assert.That(run.SpecialTasks.All(task =>
                        task.Progress == 0 && !task.Completed), Is.True,
                        $"Tutorial build {run.BuiltCount} advanced a goal.");
                    Assert.That(run.TrySimulateFillDay(out _), Is.True);
                    Assert.That(run.SpecialTasks.All(task =>
                        task.Progress == 0 && !task.Completed), Is.True,
                        $"Tutorial settlement {run.BuiltCount} advanced a goal.");
                    Assert.That(run.SpecialTaskScore, Is.Zero);
                }
                Assert.That(run.IsIntroPhase, Is.False);
                Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square),
                    Is.EqualTo(1), "The tutorial square exists but is excluded.");
                Assert.That(run.CompletedSpecialTaskCount, Is.Zero);
                Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                    out var restored), Is.True);
                Assert.That(restored.SpecialTasks.All(task =>
                    task.Progress == 0 && !task.Completed), Is.True);
            }
        }

        [Test]
        public void FirstFreePlacementStartsGoalProgressWithoutTutorialCycles()
        {
            var run = RunWithTasks(
                Task(ConstructionSpecialTaskKind.AvoidStreets,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 15),
                Task(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Medium, 3, 50),
                Task(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 6, 25));
            foreach (var (category, column, row) in TutorialOpening)
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);
            var tutorialCycles = run.CompletedWorkCyclesTotal;
            Assert.That(tutorialCycles, Is.GreaterThan(0),
                "The tutorial must actually include commutes for this check.");

            var choices = run.FillHandCards.SelectMany(category =>
                    Enumerable.Range(0, 7).SelectMany(row =>
                        Enumerable.Range(0, 7).Select(column =>
                            (category, column, row))))
                .Where(choice => run.CheckPlacement(choice.column,
                    choice.row, choice.category) ==
                    ConstructionPlacementFailure.None).ToArray();
            Assert.That(choices, Is.Not.Empty);
            var next = choices[0];
            BuildAndSettle(run, next.category, next.column, next.row);

            Assert.That(run.SpecialTasks.Single(task => task.Kind ==
                    ConstructionSpecialTaskKind.AvoidStreets).Progress,
                Is.EqualTo(next.category == ConstructionCategory.Street ? 0 : 1));
            Assert.That(run.SpecialTasks.Single(task => task.Kind ==
                    ConstructionSpecialTaskKind.CommuteCycles).Progress,
                Is.EqualTo(Math.Min(6,
                    run.CompletedWorkCyclesTotal - tutorialCycles)));
        }

        [Test]
        public void ExistingTutorialSaveLosesGoalsEarnedFromLessons()
        {
            var run = RunWithTasks(
                Task(ConstructionSpecialTaskKind.AvoidStreets,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 15),
                Task(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Medium, 3, 50),
                Task(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 6, 25));
            foreach (var (category, column, row) in TutorialOpening)
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);

            var old = run.Export();
            old.specialTasks.version = 5;
            old.specialTasks.activeTasks[0].progress = 3;
            old.specialTasks.activeTasks[0].completed = true;
            old.specialTasks.activeTasks[1].progress = 1;
            old.specialTasks.activeTasks[2].progress = 6;
            old.specialTasks.activeTasks[2].completed = true;
            old.specialTasks.completedTaskKeys = new() { "3:0", "0:0" };
            old.specialTasks.bonusScore = 40;

            Assert.That(ConstructionRunModel.TryRestore(old, out var restored),
                Is.True);
            Assert.That(restored.Export().specialTasks.version, Is.EqualTo(6));
            Assert.That(restored.SpecialTasks.All(task =>
                task.Progress == 0 && !task.Completed), Is.True);
            Assert.That(restored.SpecialTaskScore, Is.Zero);
            Assert.That(restored.CompletedSpecialTaskCount, Is.Zero);
        }

        [Test]
        public void RoadlessTaskStaysInPlaceAndUnlocksLaterBuildEffects()
        {
            var run = RunWithTasks(
                Task(ConstructionSpecialTaskKind.AvoidStreets,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 15),
                Task(ConstructionSpecialTaskKind.CompleteCommunities,
                    ConstructionSpecialTaskDifficulty.Easy, 1, 50),
                Task(ConstructionSpecialTaskKind.ConnectGreens,
                    ConstructionSpecialTaskDifficulty.Hard, 8, 70));
            foreach (var (category, column, row) in TutorialOpening)
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:0")
                .Progress, Is.Zero);

            for (var step = 0; step < 20 &&
                 !run.SpecialTasks.Single(task => task.Key == "3:0")
                     .Completed; step++)
                BuildNextLegalCard(run, preferNonStreet: true);
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:0")
                .Completed, Is.True, "Three free-play builds complete the card.");
            Assert.That(run.SpecialTasks.All(task => !task.CanReplace), Is.True);
            var scoreBefore = run.SpecialTaskEffectScore;
            var next = ConstructionCategory.Street;
            for (var step = 0; step < 8 &&
                 next == ConstructionCategory.Street; step++)
                next = BuildNextLegalCard(run, preferNonStreet: true);
            Assert.That(next, Is.Not.EqualTo(ConstructionCategory.Street));
            Assert.That(run.SpecialTaskEffectScore,
                Is.EqualTo(scoreBefore + 2));

            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.SpecialTaskScore,
                Is.EqualTo(run.SpecialTaskScore));
            Assert.That(restored.SpecialTasks.Select(task => task.Key),
                Is.EqualTo(run.SpecialTasks.Select(task => task.Key)));
        }

        [Test]
        public void StreetResetsTheNoStreetStreak()
        {
            var run = RunWithTasks(Task(
                ConstructionSpecialTaskKind.AvoidStreets,
                ConstructionSpecialTaskDifficulty.Hard, 8, 40),
                Task(ConstructionSpecialTaskKind.ConnectGreens,
                    ConstructionSpecialTaskDifficulty.Hard, 8, 70),
                Task(ConstructionSpecialTaskKind.BuildSquares,
                    ConstructionSpecialTaskDifficulty.Hard, 4, 55));
            foreach (var (category, column, row) in TutorialOpening)
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:2").Progress,
                Is.Zero);

            Assert.That(BuildNextLegalCard(run, preferNonStreet: true),
                Is.Not.EqualTo(ConstructionCategory.Street));
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:2").Progress,
                Is.GreaterThan(0));
            var street = Enumerable.Range(0, 7)
                .SelectMany(row => Enumerable.Range(0, 7)
                    .Select(column => (column, row)))
                .First(choice => run.CheckPlacement(choice.column, choice.row,
                    ConstructionCategory.Street) == ConstructionPlacementFailure.None);
            BuildAndSettle(run, ConstructionCategory.Street,
                street.column, street.row);
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:2").Progress,
                Is.Zero);
            Assert.That(run.SpecialTasks.Single(task => task.Key == "3:2").Completed,
                Is.False);
        }

        [Test]
        public void BalancedGoalEffectStopsAtItsCardLimitAndRejectsTampering()
        {
            var saved = NewHandRun().Export();
            saved.specialTasks.activeTasks = new()
            {
                Task(ConstructionSpecialTaskKind.AvoidStreets,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 15),
                Task(ConstructionSpecialTaskKind.ConnectGreens,
                    ConstructionSpecialTaskDifficulty.Hard, 8, 70),
                Task(ConstructionSpecialTaskKind.BuildSquares,
                    ConstructionSpecialTaskDifficulty.Hard, 4, 55)
            };
            saved.specialTasks.seenTaskKeys = new()
            {
                "3:0", "4:2", "2:2"
            };
            saved.specialTasks.selectionConfirmed = true;
            saved.fillHandRandomState = 1;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);
            foreach (var (category, column, row) in new[]
                     {
                         (ConstructionCategory.Green, 3, 3),
                         (ConstructionCategory.Residence, 4, 3),
                         (ConstructionCategory.Restaurant, 5, 3),
                         (ConstructionCategory.Workshop, 4, 4),
                         (ConstructionCategory.Street, 4, 2),
                         (ConstructionCategory.Waste, 6, 3),
                         (ConstructionCategory.Supermarket, 3, 2),
                         (ConstructionCategory.Square, 4, 1)
                     })
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);

            while (!run.IsFinished && run.SpecialTasks.Single(task =>
                       task.Kind == ConstructionSpecialTaskKind.AvoidStreets)
                   .EffectPoints < 20)
            {
                var placement = run.FillHandCards
                    .OrderBy(category => category ==
                        ConstructionCategory.Street ? 1 : 0)
                    .SelectMany(category =>
                        Enumerable.Range(0, 7).SelectMany(row =>
                            Enumerable.Range(0, 7).Select(column =>
                                (category, column, row))))
                    .First(candidate => run.CheckPlacement(candidate.column,
                        candidate.row, candidate.category) ==
                        ConstructionPlacementFailure.None);
                BuildAndSettle(run, placement.category, placement.column,
                    placement.row);
            }
            var roadless = run.SpecialTasks.Single(task => task.Kind ==
                ConstructionSpecialTaskKind.AvoidStreets);
            Assert.That(roadless.Completed, Is.True);
            Assert.That(roadless.EffectPoints, Is.EqualTo(20));
            Assert.That(roadless.EffectLimit, Is.EqualTo(20));
            Assert.That(run.IsFinished, Is.False,
                "This test checks the card cap before the unrelated endgame save path.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.SpecialTasks.Single(task => task.Kind ==
                ConstructionSpecialTaskKind.AvoidStreets).EffectPoints,
                Is.EqualTo(20));
            var tampered = run.Export();
            tampered.specialTasks.activeTasks[0].effectPoints = 21;
            Assert.That(ConstructionRunModel.TryRestore(tampered, out _),
                Is.False);
        }

        [Test]
        public void CompletedCommuteCardStaysAndAwardsItsEndgameEffectOnce()
        {
            var saved = LegacyHandRun().Export();
            // This scenario deliberately exercises a V3 card and its saved
            // score. Fresh V4 cards use a longer six-cycle opening target.
            saved.specialTasks.version = 3;
            saved.fillHandRandomState = 1;
            saved.specialTasks.activeTasks = new()
            {
                Task(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 5),
                Task(ConstructionSpecialTaskKind.AvoidStreets,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 6),
                Task(ConstructionSpecialTaskKind.ConnectGreens,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 6)
            };
            foreach (var key in new[] { "0:0", "3:0", "4:0" })
                if (!saved.specialTasks.seenTaskKeys.Contains(key))
                    saved.specialTasks.seenTaskKeys.Add(key);
            saved.specialTasks.selectionConfirmed = true;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);

            var opening = new[]
            {
                (ConstructionCategory.Green, 3, 3),
                (ConstructionCategory.Residence, 4, 3),
                (ConstructionCategory.Restaurant, 5, 3),
                (ConstructionCategory.Workshop, 4, 4),
                (ConstructionCategory.Street, 4, 2),
                (ConstructionCategory.Waste, 6, 3),
                (ConstructionCategory.Supermarket, 3, 2),
                (ConstructionCategory.Square, 4, 1)
            };
            foreach (var (category, column, row) in opening)
                BuildAndSettle(run, category, column, row,
                    category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None);

            while (!run.IsFinished)
            {
                var placements = run.FillHandCards
                    .SelectMany(category =>
                        Enumerable.Range(0, 7).SelectMany(row =>
                            Enumerable.Range(0, 7).Select(column =>
                                (category, column, row))))
                    .Where(candidate =>
                        run.CheckPlacement(candidate.column, candidate.row,
                            candidate.category) ==
                        ConstructionPlacementFailure.None)
                    .Take(1).ToArray();
                Assert.That(placements, Has.Length.EqualTo(1),
                    $"No playable card at {run.BuiltCount}/49.");
                var placement = placements[0];
                BuildAndSettle(run, placement.category, placement.column,
                    placement.row);
            }

            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.CompletedWorkCyclesTotal, Is.GreaterThanOrEqualTo(3));
            Assert.That(run.SpecialTasks[0].Key, Is.EqualTo("0:0"));
            Assert.That(run.SpecialTasks[0].Completed, Is.True);
            Assert.That(run.SpecialTaskEndgameScore,
                Is.EqualTo(Math.Min(30, run.CompletedWorkCyclesTotal) * 2));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.SpecialTaskEndgameScore,
                Is.EqualTo(run.SpecialTaskEndgameScore));
            Assert.That(restored.TrySimulateFillDay(out _), Is.False);
            Assert.That(restored.SpecialTaskEndgameScore,
                Is.EqualTo(run.SpecialTaskEndgameScore));
        }

        [Test]
        public void VersionOneTaskSaveMigratesAsAlreadyConfirmed()
        {
            var source = LegacyHandRun();
            var saved = source.Export();
            saved.specialTasks.version = 1;
            saved.specialTasks.selectionConfirmed = false;
            saved.specialTasks.activeTasks = new()
            {
                Task(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 3, 5),
                Task(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Easy, 2, 6),
                Task(ConstructionSpecialTaskKind.BuildSquares,
                    ConstructionSpecialTaskDifficulty.Easy, 1, 5)
            };
            saved.specialTasks.seenTaskKeys = new() { "0:0", "1:0", "2:0" };

            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.IsSpecialTaskSelectionConfirmed, Is.True);
            Assert.That(restored.SpecialTasks.All(task => !task.CanReplace), Is.True);
            Assert.That(restored.Export().specialTasks.version, Is.EqualTo(3));
        }

        [Test]
        public void FreshSpeciesGoalUsesThreeOrFourKindsAndOlderCardsKeepCounts()
        {
            var fresh = NewHandRun().Export();
            fresh.specialTasks.activeTasks = new()
            {
                Task(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Medium, 3, 50),
                Task(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 6, 25),
                Task(ConstructionSpecialTaskKind.BuildSquares,
                    ConstructionSpecialTaskDifficulty.Easy, 1, 20)
            };
            fresh.specialTasks.seenTaskKeys = new() { "1:1", "0:0", "2:0" };
            Assert.That(ConstructionRunModel.TryRestore(fresh, out var current),
                Is.True);
            Assert.That(current.UsesSpeciesTaskRules, Is.True);
            Assert.That(current.SpecialTasks.Single(task => task.Kind ==
                ConstructionSpecialTaskKind.RaiseAnimals).Target,
                Is.EqualTo(3));

            var legacy = LegacyHandRun().Export();
            legacy.specialTasks.version = 3;
            legacy.specialTasks.activeTasks = new()
            {
                Task(
                ConstructionSpecialTaskKind.RaiseAnimals,
                ConstructionSpecialTaskDifficulty.Easy, 2, 6),
                Task(
                ConstructionSpecialTaskKind.CommuteCycles,
                ConstructionSpecialTaskDifficulty.Easy, 3, 5),
                Task(ConstructionSpecialTaskKind.BuildSquares,
                    ConstructionSpecialTaskDifficulty.Easy, 1, 5)
            };
            legacy.specialTasks.seenTaskKeys = new() { "1:0", "0:0", "2:0" };
            Assert.That(ConstructionRunModel.TryRestore(legacy, out var older),
                Is.True);
            Assert.That(older.UsesSpeciesTaskRules, Is.False);
            Assert.That(older.SpecialTasks.Single(task => task.Kind ==
                ConstructionSpecialTaskKind.RaiseAnimals).Target,
                Is.EqualTo(2));
            Assert.That(older.Export().specialTasks.version, Is.EqualTo(3));
        }

        private static ConstructionRunModel NewHandRun() =>
            new(ConstructionGameMode.Fill, useFillHand: true);

        private static readonly (ConstructionCategory category, int column,
            int row)[] TutorialOpening =
        {
            (ConstructionCategory.Green, 3, 3),
            (ConstructionCategory.Residence, 4, 3),
            (ConstructionCategory.Restaurant, 5, 3),
            (ConstructionCategory.Workshop, 4, 4),
            (ConstructionCategory.Street, 4, 2),
            (ConstructionCategory.Waste, 6, 3),
            (ConstructionCategory.Supermarket, 3, 2),
            (ConstructionCategory.Square, 4, 1)
        };

        private static ConstructionRunModel RunWithTasks(
            params ConstructionSpecialTaskSaveData[] cards)
        {
            var saved = NewHandRun().Export();
            saved.specialTasks.activeTasks = cards.ToList();
            saved.specialTasks.seenTaskKeys = cards.Select(card =>
                $"{(int)card.kind}:{(int)card.difficulty}").ToList();
            saved.specialTasks.completedTaskKeys.Clear();
            saved.specialTasks.selectionConfirmed = true;
            saved.fillHandRandomState = 1;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);
            return run;
        }

        private static ConstructionCategory BuildNextLegalCard(
            ConstructionRunModel run, bool preferNonStreet)
        {
            var choice = run.FillHandCards
                .OrderBy(category => preferNonStreet &&
                    category == ConstructionCategory.Street ? 1 : 0)
                .SelectMany(category => Enumerable.Range(0, 7)
                    .SelectMany(row => Enumerable.Range(0, 7)
                        .Select(column => (category, column, row))))
                .First(candidate => run.CheckPlacement(candidate.column,
                    candidate.row, candidate.category) ==
                    ConstructionPlacementFailure.None);
            BuildAndSettle(run, choice.category, choice.column, choice.row);
            return choice.category;
        }

        private static ConstructionRunModel LegacyHandRun()
        {
            var saved = NewHandRun().Export();
            saved.score.scoringVersion = 11;
            saved.placementScore.version =
                ConstructionPlacementScore.RulesVersion;
            saved.specialTasks = null;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);
            return run;
        }

        private static ConstructionSpecialTaskSaveData Task(
            ConstructionSpecialTaskKind kind,
            ConstructionSpecialTaskDifficulty difficulty, int target,
            int points) => new()
        {
            kind = kind,
            difficulty = difficulty,
            target = target,
            points = points
        };

        private static void BuildAndSettle(ConstructionRunModel run,
            ConstructionCategory category, int column, int row,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(run.TryBuild(column, row, category, planting,
                out _, out var failure, out var placement), Is.True,
                $"{category}: {failure}/{placement}");
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
        }
    }
}
