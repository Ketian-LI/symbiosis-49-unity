using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalMortalityModelTests
    {
        [Test]
        public void FifthAnimalDeathReachesLimitRegardlessOfSpecies()
        {
            var model = new AnimalMortalityModel();

            Assert.That(model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Traffic), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Squirrel, AnimalDeathCause.Other), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Hedgehog, AnimalDeathCause.Starvation), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Fox, AnimalDeathCause.Traffic), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Other), Is.True);

            Assert.That(model.TotalDeaths, Is.EqualTo(5));
            Assert.That(model.PigeonDeaths, Is.EqualTo(2));
            Assert.That(model.TrafficDeaths, Is.EqualTo(2));
            Assert.That(model.StarvationDeaths, Is.EqualTo(1));
        }

        [Test]
        public void RestoreClampsNegativeCountsAndResetReturnsToDefaultLimit()
        {
            var model = new AnimalMortalityModel();
            model.Restore(-1, 2, 1, 0, 1, 1, 9);

            Assert.That(model.TotalDeaths, Is.EqualTo(3));
            Assert.That(model.DeathLimit, Is.EqualTo(9));

            model.Reset();
            Assert.That(model.TotalDeaths, Is.Zero);
            Assert.That(model.DeathLimit, Is.EqualTo(5));
        }

        [Test]
        public void ResearchDeathLimitCanBeConfiguredBeforeDeaths()
        {
            var model = new AnimalMortalityModel();
            model.SetDeathLimit(2);

            Assert.That(model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Other), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Fox, AnimalDeathCause.Other), Is.True);
        }
    }
}
