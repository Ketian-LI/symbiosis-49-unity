using NUnit.Framework;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WorldScaleStandardsTests
    {
        [Test]
        public void Citizen_IsApproximatelyRealWorldHeight()
        {
            Assert.That(WorldScaleStandards.CitizenVisualHeight, Is.InRange(1.65f, 1.80f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.CitizenVisualHeight), Is.InRange(0.53f, 0.58f));
        }

        [Test]
        public void Pigeon_IsSmallRelativeToOneMapCell()
        {
            Assert.That(WorldScaleStandards.PigeonVisualLength, Is.InRange(0.38f, 0.46f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.PigeonVisualLength), Is.InRange(0.12f, 0.15f));
        }

        [Test]
        public void PlannedUrbanAnimals_StayWithinExpectedScaleBands()
        {
            Assert.That(WorldScaleStandards.SquirrelVisualLength, Is.InRange(0.36f, 0.40f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.SquirrelVisualLength), Is.InRange(0.11f, 0.14f));
            Assert.That(WorldScaleStandards.HedgehogVisualLength, Is.InRange(0.23f, 0.27f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.HedgehogVisualLength), Is.InRange(0.07f, 0.10f));
            Assert.That(WorldScaleStandards.FoxTargetLength, Is.LessThan(1.1f));
        }
    }
}
