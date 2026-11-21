using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionRecentChangesTests
    {
        [Test]
        public void AdjacencyPenaltySurvivesTheAnimationAndDoesNotChangeTheRun()
        {
            var run = Start();
            Build(run, 4, 3, ConstructionCategory.Residence);
            Build(run, 4, 4, ConstructionCategory.Waste);
            var score = run.OverallScore;
            var day = run.CurrentDay;
            var rows = ConstructionRecentChanges.For(run);
            Assert.That(rows.Any(row => row.Concern && row.Chinese.Contains("垃圾间贴住宅") && row.Chinese.Contains("-80")), Is.True);
            Assert.That(run.OverallScore, Is.EqualTo(score));
            Assert.That(run.CurrentDay, Is.EqualTo(day));
        }

        [Test]
        public void UnsettledNextPlacementDoesNotReplaceTheLastObservedCause()
        {
            var run = Start();
            Build(run, 4, 3, ConstructionCategory.Residence);
            Build(run, 4, 4, ConstructionCategory.Waste);
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Restaurant, GreenPlanting.None,
                out _, out _, out _), Is.True);
            Assert.That(ConstructionRecentChanges.For(run).Any(row => row.Concern && row.English.Contains("-80")), Is.True);
        }

        [Test]
        public void UpgradeTextFollowsTheActualNeighbourUpgrade()
        {
            var run = Start();
            Build(run, 4, 3, ConstructionCategory.Residence);
            Build(run, 3, 4, ConstructionCategory.Green);
            Assert.That(ConstructionRecentChanges.For(run).Any(row => row.Upgrade), Is.True);
        }

        [Test]
        public void NewRunNeverInventsAnObservedChange()
        {
            Assert.That(ConstructionRecentChanges.For(new ConstructionRunModel(ConstructionGameMode.Fill, true)), Is.Empty);
            Assert.That(ConstructionRecentChanges.For(null), Is.Empty);
        }

        private static ConstructionRunModel Start()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20,20), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            return run;
        }
        private static void Build(ConstructionRunModel run, int x, int y, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(x,y,category,category==ConstructionCategory.Green ? GreenPlanting.Meadow : GreenPlanting.None,
                out _,out var failure,out var placement), Is.True, failure+" / "+placement);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
        }
    }
}
