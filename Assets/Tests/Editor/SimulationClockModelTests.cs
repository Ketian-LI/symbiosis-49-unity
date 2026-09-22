using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class SimulationClockModelTests
    {
        [Test]
        public void SixMinuteCycleUsesConfirmedPhaseDurations()
        {
            Assert.That(SimulationClockModel.CycleSeconds, Is.EqualTo(360d));
            Assert.That(SimulationClockModel.DawnSeconds, Is.EqualTo(30d));
            Assert.That(SimulationClockModel.DaySeconds, Is.EqualTo(150d));
            Assert.That(SimulationClockModel.DuskSeconds, Is.EqualTo(30d));
            Assert.That(SimulationClockModel.NightSeconds, Is.EqualTo(150d));
        }

        [TestCase(0d, DayPhase.Dawn)]
        [TestCase(29.99d, DayPhase.Dawn)]
        [TestCase(30d, DayPhase.Day)]
        [TestCase(179.99d, DayPhase.Day)]
        [TestCase(180d, DayPhase.Dusk)]
        [TestCase(209.99d, DayPhase.Dusk)]
        [TestCase(210d, DayPhase.Night)]
        [TestCase(359.99d, DayPhase.Night)]
        public void PhaseBoundariesMatchDesign(double seconds, DayPhase expected)
        {
            var clock = new SimulationClockModel();
            clock.Restore(seconds);
            Assert.That(clock.Phase, Is.EqualTo(expected));
        }

        [Test]
        public void SpeedMultiplierAdvancesClockAndDayNumber()
        {
            var clock = new SimulationClockModel();
            clock.Advance(90d, 4f);
            Assert.That(clock.DayNumber, Is.EqualTo(2));
            Assert.That(clock.SecondsIntoDay, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void PausedSpeedDoesNotAdvanceClock()
        {
            var clock = new SimulationClockModel();
            clock.Advance(10d, 0f);
            Assert.That(clock.TotalSeconds, Is.EqualTo(0d));
        }
    }
}
