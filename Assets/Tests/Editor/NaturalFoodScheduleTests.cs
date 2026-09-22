using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class NaturalFoodScheduleTests
    {
        [Test]
        public void FirstDayProducesAtDuskAndNightButNotAnExtraDawn()
        {
            var events = NaturalFoodSchedule.EventsBetween(
                0d,
                NaturalFoodSchedule.NightStartSeconds + 0.1d);

            Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
            {
                NaturalFoodScheduleEventKind.DuskProduction,
                NaturalFoodScheduleEventKind.NightProduction
            }));
        }

        [Test]
        public void CrossingCycleBoundaryDoesNotDuplicateDawnProduction()
        {
            var boundary = SimulationClockModel.CycleSeconds;
            var events = NaturalFoodSchedule.EventsBetween(boundary - 0.1d, boundary + 0.1d);

            Assert.That(events, Is.Empty);
        }
    }
}
