using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionBalanceScoreTests
    {
        [Test]
        public void NewLedgerRecordsResidentSatisfactionAndAdditiveCoexistence()
        {
            var ledger = new ConstructionScoreLedger();
            Assert.That(ledger.HasBalanceScore, Is.True);
            Assert.That(ledger.TryRecordDay(1, 2, 0, 6, 0), Is.True);
            Assert.That(ledger.TryRecordDay(2, 3, 2, 9, 7), Is.True);
            Assert.That(ledger.TryRecordDay(3, 2, 3, 4, 6), Is.True);

            Assert.That(ledger.AnimalScore, Is.EqualTo(19));
            Assert.That(ledger.HumanScore, Is.EqualTo(13));
            Assert.That(ledger.HasAdditiveCoexistenceScore, Is.True);
            Assert.That(ledger.BalanceScore, Is.EqualTo(32));
            Assert.That(ledger.BalanceScore,
                Is.EqualTo(ledger.AnimalScore + ledger.HumanScore));
            Assert.That(ledger.Days[0].balancePoints, Is.EqualTo(6));
            Assert.That(ledger.Days[1].balancePoints, Is.EqualTo(16));
            Assert.That(ledger.Days[2].balancePoints, Is.EqualTo(10));

            var final = ledger.FinalResult();
            Assert.That(final.HumanWorkdaysTotal, Is.EqualTo(5),
                "Workdays remain a count, separate from satisfaction points.");
            Assert.That(final.HumanPointsTotal, Is.EqualTo(13));
            Assert.That(final.BalancePointsTotal, Is.EqualTo(32));
            Assert.That(final.BothSidesScoredDays, Is.EqualTo(2));
        }

        [Test]
        public void AdditiveCoexistenceScoreIsTheSameRegardlessOfDailyTiming()
        {
            var alternating = new ConstructionScoreLedger();
            Assert.That(alternating.TryRecordDay(1, 3, 0, 9, 0), Is.True);
            Assert.That(alternating.TryRecordDay(2, 0, 3, 0, 9), Is.True);

            var simultaneous = new ConstructionScoreLedger();
            Assert.That(simultaneous.TryRecordDay(1, 1, 1, 3, 3), Is.True);
            Assert.That(simultaneous.TryRecordDay(2, 2, 2, 6, 6), Is.True);

            Assert.That(alternating.AnimalScore, Is.EqualTo(simultaneous.AnimalScore));
            Assert.That(alternating.HumanScore, Is.EqualTo(simultaneous.HumanScore));
            Assert.That(alternating.BalanceScore, Is.EqualTo(18));
            Assert.That(simultaneous.BalanceScore, Is.EqualTo(18));
            Assert.That(alternating.FinalResult().BothSidesScoredDays,
                Is.Zero, "A shared total alone does not show simultaneous benefit.");
            Assert.That(simultaneous.FinalResult().BothSidesScoredDays,
                Is.EqualTo(2));
        }

        [Test]
        public void OlderScoreVersionsPreserveTheirRecordedTotals()
        {
            var versionTwo = new ConstructionScoreLedger(2);
            Assert.That(versionTwo.TryRecordDay(1, 2, 1, 6, 3), Is.True);
            Assert.That(ConstructionScoreLedger.TryRestore(versionTwo.Export(),
                out var restoredVersionTwo), Is.True);
            Assert.That(restoredVersionTwo.HasAdditiveCoexistenceScore,
                Is.False);
            Assert.That(restoredVersionTwo.BalanceScore, Is.EqualTo(3),
                "The previous version retains its recorded daily minimum.");
            Assert.That(restoredVersionTwo.TryRecordDay(2, 2, 1, 6, 8),
                Is.True, "A continued old save can earn the new square visit.");
            Assert.That(restoredVersionTwo.BalanceScore, Is.EqualTo(9),
                "Continued version-two saves retain the old minimum formula.");

            var previous = new ConstructionScoreLedger(1);
            Assert.That(previous.TryRecordDay(1, 2, 1, 6), Is.True);
            Assert.That(ConstructionScoreLedger.TryRestore(previous.Export(),
                out var restored), Is.True);
            Assert.That(restored.HasBalanceScore, Is.False);
            Assert.That(restored.AnimalScore, Is.EqualTo(6));
            Assert.That(restored.HumanScore, Is.EqualTo(1));
            Assert.That(restored.BalanceScore, Is.Zero);
            Assert.That(restored.FinalResult().HumanWorkdaysTotal, Is.EqualTo(1));

            var oldest = new ConstructionScoreSaveData
            {
                scoringVersion = 0,
                days = new List<ConstructionDayResult>
                {
                    new() { day = 1, animalMeals = 2, completedWorkCycles = 1 }
                }
            };
            Assert.That(ConstructionScoreLedger.TryRestore(oldest,
                out var legacy), Is.True);
            Assert.That(legacy.AnimalScore, Is.EqualTo(2));
            Assert.That(legacy.HumanScore, Is.EqualTo(1));
            Assert.That(legacy.BalanceScore, Is.Zero);
        }

        [Test]
        public void UntouchedEndlessSaveKeepsOriginalScoringWithoutChangingSource()
        {
            var saved = new ConstructionRunModel(ConstructionGameMode.Endless).Export();
            saved.score.scoringVersion = 1;
            saved.placementScore = null;
            Assert.That(saved.score.days, Is.Empty);

            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(saved.score.scoringVersion, Is.EqualTo(1),
                "Restore must not mutate the supplied save data.");
            Assert.That(restored.HasAdditiveCoexistenceScore, Is.False);
            Assert.That(restored.Export().score.scoringVersion,
                Is.EqualTo(1));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.BalanceScore, Is.Zero);
        }

        [Test]
        public void VersionThreeRestoreRejectsTamperedPointsAndBalance()
        {
            var ledger = new ConstructionScoreLedger(3);
            Assert.That(ledger.TryRecordDay(1, 2, 1, 6, 3), Is.True);
            var saved = ledger.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.BalanceScore, Is.EqualTo(9));

            saved.days[0].balancePoints = 4;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
            saved.days[0].balancePoints = 9;
            saved.days[0].humanPoints = 0;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
            saved.days[0].humanPoints = 8;
            saved.days[0].balancePoints = 14;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.True,
                "A square visit can raise one completed cycle to eight points.");
            saved.days[0].humanPoints = 9;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
            saved.days[0].humanPoints = 3;
            saved.days[0].balancePoints = 9;
            saved.days[0].animalPoints = 7;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);

            var direct = new ConstructionScoreLedger(3);
            Assert.That(direct.TryRecordDay(1, 2, 1, 6, 0), Is.False);
            Assert.That(direct.TryRecordDay(1, 2, 1, 6, 9), Is.False);
            Assert.That(direct.TryRecordDay(1, 2, 1, 6, 8), Is.True);
            Assert.That(direct.HumanScore, Is.EqualTo(8));
            Assert.That(direct.BalanceScore, Is.EqualTo(14));
        }

        [Test]
        public void VersionSixKeepsMealCapWhileVersionSevenAllowsFoodAreaWithoutMeal()
        {
            var old = new ConstructionScoreLedger(6);
            Assert.That(old.TryRecordDay(1, 0, 0, 1, 0), Is.False,
                "A V6 animal point requires an actual meal.");
            Assert.That(old.TryRecordDay(1, 1, 0, 4, 0), Is.False,
                "V6 still caps each meal at three animal points.");
            Assert.That(old.TryRecordDay(1, 1, 0, 3, 0), Is.True);

            var current = new ConstructionScoreLedger(7);
            Assert.That(current.TryRecordDay(1, 0, 1, 4, 4,
                sharedSquareMeals: 1), Is.True,
                "A nearby available Square may score for wildlife and a resident " +
                "even when no animal actually eats there today.");
            Assert.That(current.AnimalScore, Is.EqualTo(4));
            Assert.That(current.HumanScore, Is.EqualTo(4));
            Assert.That(current.BalanceScore, Is.EqualTo(8));
            Assert.That(ConstructionScoreLedger.TryRestore(current.Export(),
                out var restored), Is.True);
            Assert.That(restored.BalanceScore, Is.EqualTo(8));
        }

        [Test]
        public void VersionFourAcceptsOnlyRecordedLinkageWithinDailyBounds()
        {
            var ledger = new ConstructionScoreLedger(4);
            Assert.That(ledger.HasLinkageScore, Is.True);
            Assert.That(ledger.TryRecordDay(1, 2, 1, 6, 9), Is.False,
                "A ninth resident point needs a recorded linkage event.");
            Assert.That(ledger.TryRecordDay(1, 2, 1, 6, 11,
                sharedSquareMeals: 1), Is.True);
            Assert.That(ledger.HumanScore, Is.EqualTo(11));
            Assert.That(ledger.BalanceScore, Is.EqualTo(17));
            Assert.That(ledger.FinalResult().LinkagePointsTotal, Is.EqualTo(3));
            Assert.That(ledger.FinalResult().SharedSquareMealsTotal,
                Is.EqualTo(1));

            var saved = ledger.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _),
                Is.True);
            saved.days[0].linkagePoints = 2;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _),
                Is.False);
            saved.days[0].linkagePoints = 3;
            saved.days[0].linkageRulesApplied = false;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _),
                Is.False);
        }

        [Test]
        public void VersionThreeUpgradePreservesHistoryAndScoresFutureLinkage()
        {
            var original = new ConstructionScoreLedger(3);
            Assert.That(original.TryRecordDay(1, 2, 1, 6, 8), Is.True);
            var upgraded = original.UpgradeVersionThreeToCurrent();
            Assert.That(upgraded.HasLinkageScore, Is.True);
            Assert.That(upgraded.Days[0].linkageRulesApplied, Is.False);
            Assert.That(upgraded.Days[0].balancePoints, Is.EqualTo(14));
            Assert.That(upgraded.TryRecordDay(2, 2, 1, 6, 11,
                sharedSquareMeals: 1), Is.True);
            Assert.That(upgraded.Days[1].linkageRulesApplied, Is.True);
            Assert.That(upgraded.BalanceScore, Is.EqualTo(31));
            Assert.That(ConstructionScoreLedger.TryRestore(upgraded.Export(),
                out var restored), Is.True);
            Assert.That(restored.BalanceScore, Is.EqualTo(31));
            Assert.That(restored.FinalResult().LinkagePointsTotal,
                Is.EqualTo(3));
            Assert.That(restored.FinalResult().LinkageTrackedDays,
                Is.EqualTo(1), "The migrated first day was never tracked.");
        }

        [Test]
        public void TrafficCollisionSubtractsThreeFromBothAnimalAndCoexistenceScores()
        {
            var ledger = new ConstructionScoreLedger(11);
            Assert.That(ledger.TryRecordDay(1, 1, 1, 3, 4,
                trafficPenaltyPoints: ConstructionScoreLedger.TrafficCollisionPenalty),
                Is.True);
            Assert.That(ledger.TryRecordDay(2, 0, 0, 0, 0,
                trafficPenaltyPoints: ConstructionScoreLedger.TrafficCollisionPenalty),
                Is.True);

            Assert.That(ledger.Days[0].animalPoints, Is.EqualTo(3),
                "Gross food points remain available to explain the daily result.");
            Assert.That(ledger.Days[0].trafficPenaltyPoints, Is.EqualTo(3));
            Assert.That(ledger.LastDay.trafficPenaltyRulesApplied, Is.True);
            Assert.That(ledger.AnimalScore, Is.EqualTo(-3));
            Assert.That(ledger.HumanScore, Is.EqualTo(4));
            Assert.That(ledger.BalanceScore, Is.EqualTo(1));
            Assert.That(ledger.BalanceScore,
                Is.EqualTo(ledger.AnimalScore + ledger.HumanScore));
            Assert.That(ledger.FinalResult().BothSidesScoredDays, Is.Zero,
                "A day with zero net animal points does not count for both sides.");
        }

        [Test]
        public void BalancedTrafficCollisionCostsTwentyFivePoints()
        {
            var ledger = new ConstructionScoreLedger(12);
            Assert.That(ledger.TryRecordDay(1, 1, 1, 5, 4,
                trafficPenaltyPoints: ledger.CollisionPenalty), Is.True);
            Assert.That(ledger.CollisionPenalty, Is.EqualTo(25));
            Assert.That(ledger.AnimalScore, Is.EqualTo(-20));
            Assert.That(ledger.BalanceScore, Is.EqualTo(-16));
            Assert.That(ConstructionScoreLedger.TryRestore(ledger.Export(),
                out var restored), Is.True);
            Assert.That(restored.BalanceScore, Is.EqualTo(-16));
        }

        [Test]
        public void TrafficPenaltySaveRestoresAndRejectsTampering()
        {
            var ledger = new ConstructionScoreLedger();
            Assert.That(ledger.TryRecordDay(1, 0, 0, 0, 0,
                trafficPenaltyPoints: ledger.CollisionPenalty), Is.True);
            var saved = ledger.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(-ledger.CollisionPenalty));
            Assert.That(restored.LastDay.trafficPenaltyPoints, Is.EqualTo(ledger.CollisionPenalty));

            saved.days[0].trafficPenaltyPoints = ledger.CollisionPenalty + 1;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
            saved.days[0].trafficPenaltyPoints = ledger.CollisionPenalty;
            saved.days[0].trafficPenaltyRulesApplied = false;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);
            saved.days[0].trafficPenaltyRulesApplied = true;
            saved.days[0].balancePoints = 0;
            Assert.That(ConstructionScoreLedger.TryRestore(saved, out _), Is.False);

            var oldLedger = new ConstructionScoreLedger(4);
            Assert.That(oldLedger.TryRecordDay(1, 0, 0, 0, 0,
                trafficPenaltyPoints: 3), Is.False);
            Assert.That(oldLedger.TryRecordDay(1, 1, 0, 3, 0), Is.True);
            Assert.That(ConstructionScoreLedger.TryRestore(oldLedger.Export(),
                out var historical), Is.True);
            Assert.That(historical.ScoringVersion, Is.EqualTo(4));
            Assert.That(historical.AnimalScore, Is.EqualTo(3));
        }

        [Test]
        public void ActiveVersionFourRunKeepsOldRulesAndRankingVersion()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var oldSave = run.Export();
            oldSave.score.scoringVersion = 4;
            oldSave.placementScore = null;
            Assert.That(oldSave.score.days[0].trafficPenaltyRulesApplied,
                Is.False);

            Assert.That(ConstructionRunModel.TryRestore(oldSave,
                out var restored), Is.True);
            Assert.That(restored.ScoringVersion,
                Is.EqualTo(4));
            Assert.That(restored.HasTrafficPenaltyScore, Is.False);
            Assert.That(restored.LastScoreDay.trafficPenaltyRulesApplied,
                Is.False, "The old day must not acquire a retroactive penalty.");
            Assert.That(restored.AnimalScore, Is.EqualTo(1));
            Assert.That(restored.TryEndDay(0, 0), Is.True);
            Assert.That(restored.LastScoreDay.trafficPenaltyRulesApplied,
                Is.False, "Compatibility settlement does not simulate traffic.");
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var roundTrip), Is.True);
            Assert.That(roundTrip.AnimalScore, Is.EqualTo(1));
        }

        [Test]
        public void FreshModesUseTheirRankingVersionAndAnActiveVersionFiveSaveKeepsItsYield()
        {
            foreach (var mode in new[]
                     {
                         ConstructionGameMode.Fill,
                         ConstructionGameMode.Endless,
                         ConstructionGameMode.BoardGame
                     })
            {
                var fresh = new ConstructionRunModel(mode);
                Assert.That(fresh.ScoringVersion,
                    Is.EqualTo(mode == ConstructionGameMode.Endless
                        ? ConstructionScoreLedger.CurrentScoringVersion : 9),
                    mode.ToString());
                Assert.That(fresh.HasVariableGreenYield, Is.True, mode.ToString());
            }

            // Day 1 has the same food outcome in V5 and V7. Convert only the
            // recorded score to its historical meal-based value.
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var saved = run.Export();
            saved.score.scoringVersion = 5;
            saved.placementScore = null;
            saved.score.days[0].animalPoints = 3;
            saved.score.days[0].balancePoints = 3;
            saved.lastEcologyDay.animalPoints = 3;
            saved.runId = "00000000-0000-0000-0000-000000000008";
            Assert.That(ConstructionRunModel.TryRestore(saved, out var oldRun),
                Is.True);
            Assert.That(oldRun.ScoringVersion, Is.EqualTo(5));
            Assert.That(oldRun.HasVariableGreenYield, Is.False);

            while (oldRun.CurrentDay < 10)
                Assert.That(oldRun.TrySimulateDay(out _), Is.True);
            Assert.That(oldRun.GreenDailyYield(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1), "The old oak must still produce every day.");
            Assert.That(ConstructionRunModel.TryRestore(oldRun.Export(),
                out var reloaded), Is.True);
            Assert.That(reloaded.ScoringVersion, Is.EqualTo(5));
            Assert.That(reloaded.HasVariableGreenYield, Is.False);
            Assert.That(reloaded.GreenDailyYield(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1));
        }

        [Test]
        public void CurrentVersionDayTenFoodPreviewMatchesSettlementAndSaveReplay()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            while (run.CurrentDay < 10)
            {
                Assert.That(run.GreenDailyYield(ConstructionBoardModel.StarterOakId),
                    Is.EqualTo(1), "The first nine days keep the tutorial yield.");
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }

            // This valid run ID gives the starter oak a no-harvest day at day 10.
            // A fixed case makes the regression independent of chance.
            var saved = run.Export();
            saved.runId = "00000000-0000-0000-0000-000000000008";
            Assert.That(ConstructionRunModel.TryRestore(saved, out var resumed),
                Is.True);
            Assert.That(resumed.HasVariableGreenYield, Is.True);
            Assert.That(resumed.GreenDailyYield(ConstructionBoardModel.StarterOakId),
                Is.Zero);

            var preview = resumed.PreviewEcologyDay();
            var repeatedPreview = resumed.PreviewEcologyDay();
            var previewOak = preview.foodChanges.Find(change =>
                change.tileId == ConstructionBoardModel.StarterOakId);
            Assert.That(previewOak, Is.Not.Null);
            Assert.That(previewOak.produced, Is.Zero);
            Assert.That(repeatedPreview.foodChanges.Find(change =>
                change.tileId == ConstructionBoardModel.StarterOakId).produced,
                Is.EqualTo(previewOak.produced));

            Assert.That(ConstructionRunModel.TryRestore(resumed.Export(),
                out var replay), Is.True);
            Assert.That(replay.GreenDailyYield(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(resumed.GreenDailyYield(ConstructionBoardModel.StarterOakId)));
            Assert.That(resumed.TrySimulateDay(out var settled), Is.True);
            Assert.That(replay.TrySimulateDay(out var replayed), Is.True);
            var settledOak = settled.foodChanges.Find(change =>
                change.tileId == ConstructionBoardModel.StarterOakId);
            var replayedOak = replayed.foodChanges.Find(change =>
                change.tileId == ConstructionBoardModel.StarterOakId);
            Assert.That(settledOak.produced, Is.EqualTo(previewOak.produced));
            Assert.That(settledOak.eaten, Is.EqualTo(previewOak.eaten));
            Assert.That(settledOak.remaining, Is.EqualTo(previewOak.remaining));
            Assert.That(settled.animalPoints, Is.EqualTo(preview.animalPoints));
            Assert.That(replayedOak.produced, Is.EqualTo(settledOak.produced));
            Assert.That(replayed.animalPoints, Is.EqualTo(settled.animalPoints));
        }
    }
}
