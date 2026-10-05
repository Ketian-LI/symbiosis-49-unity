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

        [Test]
        public void NextCollectionRemainsDayTwoUntilItsActualCollectionTime()
        {
            var cycle = SimulationClockModel.CycleSeconds;
            var dayTwoCollection = cycle + WasteCollectionSchedule.MunicipalCollectionSeconds;
            Assert.That(WasteCollectionSchedule.NextMunicipalCollectionDayNumber(0d), Is.EqualTo(2));
            Assert.That(WasteCollectionSchedule.NextMunicipalCollectionDayNumber(dayTwoCollection - 0.01d),
                Is.EqualTo(2));
            Assert.That(WasteCollectionSchedule.NextMunicipalCollectionDayNumber(dayTwoCollection),
                Is.EqualTo(4));
        }

        [Test]
        public void DailyShopForecastRotatesAndKeepsLargerFourDayMarkets()
        {
            Assert.That(NeighborhoodMarketSchedule.ForDay(3), Is.Null);
            Assert.That(NeighborhoodMarketSchedule.NextOnOrAfter(1).DayNumber, Is.EqualTo(4));
            Assert.That(NeighborhoodMarketSchedule.ForDay(4)?.RoomId, Is.EqualTo("canteen-a"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(5)?.RoomId, Is.EqualTo("canteen-b"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(6)?.RoomId, Is.EqualTo("supermarket"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(4)?.IsMajorMarket, Is.False);
            Assert.That(NeighborhoodMarketSchedule.ForDay(4)?.ExtraWaste, Is.EqualTo(3));
            Assert.That(NeighborhoodMarketSchedule.ForDay(4)?.ExtraIncome, Is.EqualTo(1));
            Assert.That(NeighborhoodMarketSchedule.ForDay(4)?.CleanBonus, Is.Zero);
            Assert.That(NeighborhoodMarketSchedule.ForDay(7)?.RoomId, Is.EqualTo("canteen-a"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(8)?.RoomId, Is.EqualTo("canteen-b"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(8)?.IsMajorMarket, Is.False);
            Assert.That(NeighborhoodMarketSchedule.ForDay(11)?.RoomId, Is.EqualTo("canteen-b"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(15)?.RoomId, Is.EqualTo("supermarket"));
            Assert.That(NeighborhoodMarketSchedule.ForDay(19)?.RoomId, Is.EqualTo("canteen-a"));
            Assert.That(NeighborhoodMarketSchedule.NextOnOrAfter(12).DayNumber, Is.EqualTo(12));
            Assert.That(NeighborhoodMarketSchedule.ForDay(7)?.IsMajorMarket, Is.True);
            Assert.That(NeighborhoodMarketSchedule.ForDay(7)?.ExtraWaste, Is.EqualTo(5));
            Assert.That(NeighborhoodMarketSchedule.ForDay(7)?.ExtraIncome, Is.EqualTo(3));
            Assert.That(NeighborhoodMarketSchedule.ForDay(7)?.CleanBonus, Is.EqualTo(2));
            var dayFourStart = 3d * SimulationClockModel.CycleSeconds;
            Assert.That(NeighborhoodMarketSchedule.NextUnprocessed(dayFourStart).DayNumber,
                Is.EqualTo(4));
            Assert.That(NeighborhoodMarketSchedule.NextUnprocessed(
                dayFourStart + WasteCollectionSchedule.DailyProductionSeconds).DayNumber,
                Is.EqualTo(5), "A processed daily hotspot must not remain the forecast.");
            var daySevenStart = 6d * SimulationClockModel.CycleSeconds;
            Assert.That(NeighborhoodMarketSchedule.NextUnprocessed(daySevenStart).DayNumber,
                Is.EqualTo(7));
            Assert.That(NeighborhoodMarketSchedule.NextUnprocessed(
                daySevenStart + WasteCollectionSchedule.DailyProductionSeconds).DayNumber,
                Is.EqualTo(8), "Forecast must move to tomorrow after the market is processed.");
        }
    }
}
