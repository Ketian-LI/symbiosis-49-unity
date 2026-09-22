using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteCollectionNotificationTests
    {
        [Test]
        public void WarningWindowOnlyAppearsOnEvenDaysBetweenThreeAndFour()
        {
            var secondDay = SimulationClockModel.CycleSeconds;
            Assert.That(WasteCollectionNotification.IsCollectionWarningWindow(
                WasteCollectionSchedule.CollectionWarningSeconds), Is.False);
            Assert.That(WasteCollectionNotification.IsCollectionWarningWindow(
                secondDay + WasteCollectionSchedule.CollectionWarningSeconds), Is.True);
            Assert.That(WasteCollectionNotification.IsCollectionWarningWindow(
                secondDay + WasteCollectionSchedule.MunicipalCollectionSeconds), Is.False);
        }

        [Test]
        public void CountdownClosesFromOneToZero()
        {
            var secondDay = SimulationClockModel.CycleSeconds;
            Assert.That(WasteCollectionNotification.WarningCountdown01(
                secondDay + WasteCollectionSchedule.CollectionWarningSeconds), Is.EqualTo(1f));
            Assert.That(WasteCollectionNotification.WarningCountdown01(
                secondDay + WasteCollectionSchedule.CollectionWarningSeconds + 7.5d),
                Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void FourBinsClearOneAtATimeDuringTruckPass()
        {
            Assert.That(WasteCollectionNotification.VisibleBinCount(0f), Is.EqualTo(4));
            Assert.That(WasteCollectionNotification.VisibleBinCount(
                WasteCollectionNotification.FirstBinClearTime), Is.EqualTo(3));
            Assert.That(WasteCollectionNotification.VisibleBinCount(
                WasteCollectionNotification.FirstBinClearTime +
                WasteCollectionNotification.BinClearInterval * 3f), Is.Zero);
            Assert.That(WasteCollectionNotification.TruckTravel01(
                WasteCollectionNotification.TruckTravelDuration), Is.EqualTo(1f));
        }
    }
}
