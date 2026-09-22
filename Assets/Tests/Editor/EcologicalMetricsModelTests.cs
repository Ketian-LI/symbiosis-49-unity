using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class EcologicalMetricsModelTests
    {
        [Test]
        public void HealthyOpeningStateUsesActualFoodDemandAndMatureTrees()
        {
            var snapshot = EcologicalMetricsModel.Calculate(
                0,
                1f,
                10,
                20,
                3,
                0,
                0,
                0);

            Assert.That(snapshot.HumanFunction, Is.EqualTo(1f));
            Assert.That(snapshot.FoodAccessibility, Is.EqualTo(0.5f));
            Assert.That(snapshot.HabitatProvision, Is.EqualTo(0.9375f));
            Assert.That(snapshot.AnimalSafety, Is.EqualTo(1f));
        }

        [Test]
        public void HumanFunctionUsesWeakerOfCommuteAndWasteConditions()
        {
            var snapshot = EcologicalMetricsModel.Calculate(
                20,
                0.5f,
                0,
                20,
                0,
                0,
                0,
                0);

            Assert.That(snapshot.HumanFunction, Is.EqualTo(0.5f));
        }

        [Test]
        public void HungerAndRecordedDeathsReduceSafetyWithoutGoingBelowZero()
        {
            var snapshot = EcologicalMetricsModel.Calculate(
                0,
                1f,
                0,
                20,
                4,
                10,
                5,
                5);

            Assert.That(snapshot.AnimalSafety, Is.GreaterThanOrEqualTo(0f));
            Assert.That(snapshot.AnimalSafety, Is.LessThan(0.3f));
        }
    }
}
