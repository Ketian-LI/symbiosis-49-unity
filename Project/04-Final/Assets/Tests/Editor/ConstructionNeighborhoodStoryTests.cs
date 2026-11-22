using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionNeighborhoodStoryTests
    {
        [Test]
        public void EmptyCityDoesNotInventMealsOrResidents()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill, useFillHand: true);
            var stories = ConstructionNeighborhoodStories.For(run);
            Assert.That(stories.Count, Is.EqualTo(1));
            Assert.That(stories[0].Kind, Is.EqualTo(NeighborhoodStoryKind.Beginning));
        }

        [Test]
        public void RestaurantStoryWaitsForAnActualMealAndRoutine()
        {
            var run = Endless();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 5, 3, ConstructionCategory.Restaurant);
            Assert.That(ConstructionNeighborhoodStories.For(run).Any(story =>
                story.Kind is NeighborhoodStoryKind.FirstMeal or NeighborhoodStoryKind.FirstCycle), Is.False);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.FirstResidentAte, Is.True);
            var stories = ConstructionNeighborhoodStories.For(run);
            Assert.That(stories.Any(story => story.Kind == NeighborhoodStoryKind.FirstMeal), Is.True);
            Assert.That(stories.Any(story => story.Kind == NeighborhoodStoryKind.FirstCycle), Is.True);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out var restored), Is.True);
            Assert.That(ConstructionNeighborhoodStories.For(restored).Select(story => (story.Kind, story.Day)),
                Is.EqualTo(stories.Select(story => (story.Kind, story.Day))),
                "Loading must reconstruct the same observed story, without inventing a new event date.");
        }

        [Test]
        public void UpgradeDoesNotPretendNewPlantingHasMatured()
        {
            var run = Endless();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Green);
            var stories = ConstructionNeighborhoodStories.For(run);
            Assert.That(stories.Any(story => story.Kind == NeighborhoodStoryKind.GreenConnected), Is.True);
            Assert.That(stories.Any(story => story.Kind == NeighborhoodStoryKind.MatureGreen), Is.False);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(ConstructionNeighborhoodStories.For(run).Any(story =>
                story.Kind == NeighborhoodStoryKind.MatureGreen), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NegativePairingIsExplainedForEitherPlacementOrder(bool homeFirst)
        {
            var run = Endless();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 5, 3, ConstructionCategory.Waste);
            if (!homeFirst)
            {
                Assert.That(run.TrySimulateDay(out _), Is.True);
                Build(run, 6, 3, ConstructionCategory.Residence);
            }
            var story = ConstructionNeighborhoodStories.For(run)
                .First(item => item.Kind == NeighborhoodStoryKind.WasteBesideHome);
            Assert.That(story.ChineseCause, Does.Contain("-80"));
            Assert.That(story.Day, Is.EqualTo(run.CurrentDay));
            Assert.That(story.Concern, Is.True);
            Assert.That(ConstructionNeighborhoodStories.For(run).Any(item =>
                item.Kind == NeighborhoodStoryKind.MarketFood), Is.False,
                "An urban building alone must not claim that animals ate its food.");
        }

        private static ConstructionRunModel Endless()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 20), Is.True);
            return run;
        }

        private static void Build(ConstructionRunModel run, int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category,
                category == ConstructionCategory.Green ? GreenPlanting.Meadow : GreenPlanting.None,
                out _, out var failure, out var reason), Is.True, failure + " / " + reason);
        }
    }
}
