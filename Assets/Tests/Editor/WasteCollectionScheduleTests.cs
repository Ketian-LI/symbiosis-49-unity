using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteCollectionScheduleTests
    {
        [Test]
        public void DailyProductionOccursAtNightStart()
        {
            var before = WasteCollectionSchedule.DailyProductionSeconds - 0.01d;
            Assert.That(WasteCollectionSchedule.EventsBetween(0d, before), Is.Empty);

            var events = WasteCollectionSchedule.EventsBetween(before, WasteCollectionSchedule.DailyProductionSeconds);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(WasteScheduleEventKind.DailyProduction));
            Assert.That(events[0].DayNumber, Is.EqualTo(1));
        }

        [Test]
        public void CollectionWarningAndCollectionOnlyOccurOnEvenDays()
        {
            var dayTwoStart = SimulationClockModel.CycleSeconds;
            var events = WasteCollectionSchedule.EventsBetween(
                dayTwoStart + WasteCollectionSchedule.CollectionWarningSeconds - 0.01d,
                dayTwoStart + WasteCollectionSchedule.MunicipalCollectionSeconds);

            Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
            {
                WasteScheduleEventKind.CollectionWarning,
                WasteScheduleEventKind.MunicipalCollection
            }));
            Assert.That(events.All(item => item.DayNumber == 2), Is.True);
        }

        [Test]
        public void ATimeJumpProcessesEveryMissedDailyEvent()
        {
            var events = WasteCollectionSchedule.EventsBetween(
                0d,
                SimulationClockModel.CycleSeconds * 3d);

            Assert.That(
                events.Count(item => item.Kind == WasteScheduleEventKind.DailyProduction),
                Is.EqualTo(3));
            Assert.That(
                events.Count(item => item.Kind == WasteScheduleEventKind.MunicipalCollection),
                Is.EqualTo(1));
        }
    }
}
