using System;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionLayoutQualityScoreTests
    {
        [Test]
        public void EqualInventoriesHaveAWiderGapForConnectedGreenAndSeparatedWaste()
        {
            var good = Layout(true, 13);
            var poor = Layout(false, 13);
            var oldGood = Layout(true, 9);
            var oldPoor = Layout(false, 9);
            Assert.That(good.BuiltCount, Is.EqualTo(poor.BuiltCount));
            foreach (ConstructionCategory category in Enum.GetValues(typeof(ConstructionCategory)))
                Assert.That(good.CategoryBuiltCount(category), Is.EqualTo(poor.CategoryBuiltCount(category)));
            var difference = good.OverallScore - poor.OverallScore;
            var oldDifference = oldGood.OverallScore - oldPoor.OverallScore;
            Assert.That(difference, Is.GreaterThan(oldDifference + 100),
                "Layout quality must affect the total beyond the previous daily activity difference.");
            TestContext.WriteLine($"Same 6 plots: v9 good {oldGood.OverallScore}, poor {oldPoor.OverallScore}, gap {oldDifference}; v13 good {good.OverallScore}, poor {poor.OverallScore}, gap {difference}.");
            Assert.That(ConstructionRunModel.TryRestore(good.Export(), out var restored), Is.True);
            Assert.That(restored.OverallScore, Is.EqualTo(good.OverallScore));
            Assert.That(ConstructionRunModel.TryRestore(poor.Export(), out restored), Is.True);
            Assert.That(restored.OverallScore, Is.EqualTo(poor.OverallScore));
        }

        [Test]
        public void PenaltyBandsStayVersionedAndDoNotClampNegativeTotals()
        {
            Assert.That(ConstructionPlacementScore.PairPoints(ConstructionCategory.Waste,
                ConstructionCategory.Residence, 6), Is.EqualTo(-80));
            Assert.That(ConstructionPlacementScore.PairPoints(ConstructionCategory.Street,
                ConstructionCategory.Green, 6), Is.EqualTo(-40));
            Assert.That(ConstructionPlacementScore.PairPoints(ConstructionCategory.Waste,
                ConstructionCategory.Residence, 5), Is.EqualTo(-18));
            var ledger = new ConstructionScoreLedger(13);
            Assert.That(ledger.TryRecordDay(1, 0, 0, 0, 0,
                trafficPenaltyPoints: 60), Is.True);
            Assert.That(ledger.BalanceScore, Is.EqualTo(-60));
            Assert.That(ConstructionScoreLedger.TryRestore(ledger.Export(), out var restored), Is.True);
            Assert.That(restored.BalanceScore, Is.EqualTo(-60));
            Assert.That(new ConstructionScoreLedger(12).CollisionPenalty, Is.EqualTo(25));
            Assert.That(new ConstructionScoreLedger(9).CollisionPenalty, Is.EqualTo(3));
        }

        private static ConstructionRunModel Layout(bool good, int version)
        {
            var initial = new ConstructionRunModel(ConstructionGameMode.Endless).Export();
            initial.runId = "50f9dd3b-8ef0-43ed-8fa5-05284a32def0";
            if (version == 9)
            {
                initial.score.scoringVersion = 9;
                initial.placementScore = null;
            }
            Assert.That(ConstructionRunModel.TryRestore(initial, out var run), Is.True);
            Assert.That(run.ConfigureEndlessLossRules(20, 20), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Build(run, 5, 3, ConstructionCategory.Restaurant);
            Build(run, 4, 2, ConstructionCategory.Street);
            Build(run, good ? 3 : 5, good ? 4 : 2, ConstructionCategory.Green);
            Build(run, good ? 5 : 4, good ? 2 : 4, ConstructionCategory.Waste);
            return run;
        }

        private static void Build(ConstructionRunModel run, int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category,
                category == ConstructionCategory.Green ? GreenPlanting.Meadow : GreenPlanting.None,
                out _, out var failure, out var placement), Is.True, failure + " / " + placement + " / " + run.EndReason);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
        }
    }
}
