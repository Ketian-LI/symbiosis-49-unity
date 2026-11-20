using System;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionRandomBoardGameTests
    {
        [Test]
        public void NewBoardGameUnlocksAllEightDrawTypesFromDayTwo()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.IsRandomBoardGame, Is.True);
            Assert.That(run.BoardGameRulesVersion, Is.EqualTo(2));
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Complete));
            Assert.That(run.IsUnlocked(ConstructionCategory.Green), Is.False);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.IsRandomBoardGame, Is.True);
            foreach (ConstructionCategory category in
                     Enum.GetValues(typeof(ConstructionCategory)))
                Assert.That(run.IsUnlocked(category), Is.True, category.ToString());

            // Random draw changes the category order, not spatial rules.
            Assert.That(run.CheckPlacement(4, 3,
                    ConstructionCategory.Residence),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(run.CheckPlacement(4, 3,
                    ConstructionCategory.Street),
                Is.EqualTo(ConstructionPlacementFailure.StreetNeedsHomeConnection));
        }

        [Test]
        public void NewBoardGameCanPlaceGreenByAHomeWithoutTouchingStarter()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.CheckPlacement(5, 3, ConstructionCategory.Green),
                Is.EqualTo(ConstructionPlacementFailure.None));
        }

        [Test]
        public void OldBoardGameSaveKeepsLinearIntroduction()
        {
            var fresh = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(fresh.TrySimulateDay(out _), Is.True);
            var saved = fresh.Export();
            saved.introRulesVersion = 1;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var oldRun),
                Is.True);
            Assert.That(oldRun.IsRandomBoardGame, Is.False);
            Assert.That(oldRun.BoardGameRulesVersion, Is.EqualTo(1));
            Assert.That(oldRun.IsIntroPhase, Is.True);
            Assert.That(oldRun.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Green));
            Assert.That(oldRun.IsUnlocked(ConstructionCategory.Residence), Is.False);
        }

        [Test]
        public void RandomRuleVersionCannotBeAssignedToFillSave()
        {
            var saved = new ConstructionRunModel(ConstructionGameMode.Fill).Export();
            saved.introRulesVersion = 2;
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.False);
        }
    }
}
