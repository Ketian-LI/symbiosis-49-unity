using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalPopulationDefaultsTests
    {
        [Test]
        public void ApprovedStartingPopulationUsesOneFox()
        {
            Assert.That(AnimalPopulationDefaults.Pigeons, Is.EqualTo(12));
            Assert.That(AnimalPopulationDefaults.Squirrels, Is.EqualTo(4));
            Assert.That(AnimalPopulationDefaults.Hedgehogs, Is.EqualTo(2));
            Assert.That(AnimalPopulationDefaults.Foxes, Is.EqualTo(1));
            Assert.That(AnimalPopulationDefaults.Total, Is.EqualTo(19));
        }
    }
}
