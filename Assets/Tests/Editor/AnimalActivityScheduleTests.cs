using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalActivityScheduleTests
    {
        [TestCase(WildlifeSpecies.Pigeon, DayPhase.Day, true)]
        [TestCase(WildlifeSpecies.Pigeon, DayPhase.Night, false)]
        [TestCase(WildlifeSpecies.Squirrel, DayPhase.Day, true)]
        [TestCase(WildlifeSpecies.Squirrel, DayPhase.Night, false)]
        [TestCase(WildlifeSpecies.Hedgehog, DayPhase.Day, false)]
        [TestCase(WildlifeSpecies.Hedgehog, DayPhase.Dusk, true)]
        [TestCase(WildlifeSpecies.Hedgehog, DayPhase.Night, true)]
        [TestCase(WildlifeSpecies.Fox, DayPhase.Dawn, false)]
        [TestCase(WildlifeSpecies.Fox, DayPhase.Night, true)]
        public void SpeciesUsesConfirmedDailyActivityWindow(
            WildlifeSpecies species,
            DayPhase phase,
            bool expected)
        {
            Assert.That(AnimalActivitySchedule.IsNormallyActive(species, phase), Is.EqualTo(expected));
        }
    }
}
