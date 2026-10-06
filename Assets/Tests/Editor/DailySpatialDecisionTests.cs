using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class DailySpatialDecisionTests
    {
        private static RoomLayoutImpactPreview Preview(int beforeWorkers = 4, int afterWorkers = 4,
            int beforeSeeds = 12, int afterSeeds = 12, int beforeWaste = 0,
            int beforeGreen = 4, int pigeonMeals = 12, int livingPigeons = 12) =>
            new(0, new ResidentCommutePreview(beforeWorkers, 0),
                new ResidentCommutePreview(afterWorkers, 0),
                4, 4, beforeSeeds, afterSeeds, beforeWaste, beforeWaste,
                0, null, 0, 0, null, beforeGreenCells: beforeGreen,
                afterGreenCells: beforeGreen, livingPigeons: livingPigeons,
                beforePigeonSeedMealCeiling: pigeonMeals,
                afterPigeonSeedMealCeiling: pigeonMeals);

        [Test]
        public void CoordinateOnlySwapDoesNotQualify()
        {
            Assert.That(DailySpatialDecision.HasMaterialImpact(Preview()), Is.False);
        }

        [Test]
        public void ChangedForecastQualifiesEvenWhenTradeoffIsNegative()
        {
            Assert.That(DailySpatialDecision.HasMaterialImpact(
                Preview(beforeWorkers: 4, afterWorkers: 3)), Is.True);
            Assert.That(DailySpatialDecision.HasMaterialImpact(
                Preview(beforeSeeds: 12, afterSeeds: 13)), Is.True);
        }

        [Test]
        public void MovingShrubQualifiesBecauseShelterMustRecover()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 0),
                new ResidentCommutePreview(4, 0),
                4, 4, 12, 12, 0, 0, 0, null, 0, 0, null,
                beforeShelterPairs: 0, afterShelterPairs: 0,
                recoveredShelterPairs: 2, movedShrubs: 1,
                beforeAnimalConnections: 4, afterAnimalConnections: 4,
                beforeGreenCells: 4, afterGreenCells: 4,
                beforePigeonSeedMealCeiling: 12, afterPigeonSeedMealCeiling: 12);
            Assert.That(DailySpatialDecision.HasMaterialImpact(impact), Is.True);
        }

        [Test]
        public void KeepingLayoutRequiresCoreForecastToBeStable()
        {
            Assert.That(DailySpatialDecision.CanKeepLayout(Preview(), 4), Is.True);
            Assert.That(DailySpatialDecision.BlockingRisk(
                Preview(beforeWorkers: 3, afterWorkers: 3), 4),
                Is.EqualTo(DailySpatialDecision.Risk.Commute));
            Assert.That(DailySpatialDecision.CanKeepLayout(
                Preview(beforeWaste: 1), 4), Is.False);
            Assert.That(DailySpatialDecision.CanKeepLayout(
                Preview(beforeGreen: 3), 4), Is.False);
            Assert.That(DailySpatialDecision.CanKeepLayout(
                Preview(pigeonMeals: 11), 4), Is.False);
        }
    }
}
