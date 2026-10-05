using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Core
{
    public enum WasteScheduleEventKind
    {
        DailyProduction,
        CollectionWarning,
        MunicipalCollection
    }

    public readonly struct WasteScheduleEvent
    {
        public WasteScheduleEvent(WasteScheduleEventKind kind, int dayNumber)
        {
            Kind = kind;
            DayNumber = dayNumber;
        }

        public WasteScheduleEventKind Kind { get; }
        public int DayNumber { get; }
    }

    public static class WasteCollectionSchedule
    {
        public const double DailyProductionSeconds =
            SimulationClockModel.DawnSeconds +
            SimulationClockModel.DaySeconds +
            SimulationClockModel.DuskSeconds;

        // The cycle begins at 06:00. 03:00 and 04:00 are therefore 21 and 22
        // game hours after the start of the current day.
        public const double CollectionWarningSeconds =
            SimulationClockModel.CycleSeconds * 21d / 24d;
        public const double MunicipalCollectionSeconds =
            SimulationClockModel.CycleSeconds * 22d / 24d;

        public static int NextMunicipalCollectionDayNumber(double totalSeconds)
        {
            var now = Math.Max(0d, totalSeconds);
            var dayIndex = (int)(now / SimulationClockModel.CycleSeconds);
            var dayNumber = dayIndex + 1;
            var collectionTime = dayIndex * SimulationClockModel.CycleSeconds +
                                 MunicipalCollectionSeconds;
            if (dayNumber % 2 == 0 && now < collectionTime)
            {
                return dayNumber;
            }
            return dayNumber + (dayNumber % 2 == 0 ? 2 : 1);
        }

        public static IReadOnlyList<WasteScheduleEvent> EventsBetween(
            double previousTotalSeconds,
            double currentTotalSeconds)
        {
            var events = new List<WasteScheduleEvent>();
            var previous = Math.Max(0d, previousTotalSeconds);
            var current = Math.Max(0d, currentTotalSeconds);
            if (current <= previous)
            {
                return events;
            }

            var firstDayIndex = (int)(previous / SimulationClockModel.CycleSeconds);
            var lastDayIndex = (int)(current / SimulationClockModel.CycleSeconds);
            for (var dayIndex = firstDayIndex; dayIndex <= lastDayIndex; dayIndex++)
            {
                var dayNumber = dayIndex + 1;
                AddIfCrossed(
                    events,
                    WasteScheduleEventKind.DailyProduction,
                    dayNumber,
                    dayIndex,
                    DailyProductionSeconds,
                    previous,
                    current);

                if (dayNumber % 2 != 0)
                {
                    continue;
                }

                AddIfCrossed(
                    events,
                    WasteScheduleEventKind.CollectionWarning,
                    dayNumber,
                    dayIndex,
                    CollectionWarningSeconds,
                    previous,
                    current);
                AddIfCrossed(
                    events,
                    WasteScheduleEventKind.MunicipalCollection,
                    dayNumber,
                    dayIndex,
                    MunicipalCollectionSeconds,
                    previous,
                    current);
            }

            return events;
        }

        private static void AddIfCrossed(
            ICollection<WasteScheduleEvent> events,
            WasteScheduleEventKind kind,
            int dayNumber,
            int dayIndex,
            double secondsIntoDay,
            double previous,
            double current)
        {
            var absolute = dayIndex * SimulationClockModel.CycleSeconds + secondsIntoDay;
            if (absolute > previous && absolute <= current)
            {
                events.Add(new WasteScheduleEvent(kind, dayNumber));
            }
        }
    }

    public readonly struct NeighborhoodMarketEvent
    {
        public NeighborhoodMarketEvent(int dayNumber, string roomId, int extraWaste,
            int extraIncome, int cleanBonus, bool isMajorMarket)
        {
            DayNumber = dayNumber;
            RoomId = roomId;
            ExtraWaste = extraWaste;
            ExtraIncome = extraIncome;
            CleanBonus = cleanBonus;
            IsMajorMarket = isMajorMarket;
        }

        public int DayNumber { get; }
        public string RoomId { get; }
        public int ExtraWaste { get; }
        public int ExtraIncome { get; }
        public int CleanBonus { get; }
        public bool IsMajorMarket { get; }
    }

    // A predictable spatial pressure, not a penalty for leaving the controls idle.
    // The player can prepare for a known producer, accept the extra cleanup, or
    // build a layout resilient to several producers at once.
    public static class NeighborhoodMarketSchedule
    {
        public const int FirstDailyDay = 4;
        public const int FirstDay = 7;
        public const int IntervalDays = 4;
        public const int DailyExtraWaste = 3;
        public const int DailyExtraIncome = 1;
        public const int ExtraWaste = 5;
        public const int ExtraIncome = 3;
        public const int CleanBonus = 2;

        private static readonly string[] Producers =
        {
            "canteen-a", "canteen-b", "supermarket"
        };

        public static NeighborhoodMarketEvent? ForDay(int dayNumber)
        {
            if (dayNumber < FirstDailyDay)
            {
                return null;
            }

            // The active shop changes every day. The stronger market still
            // returns every four days, but it does not leave the intervening
            // days strategically identical.
            var index = (dayNumber - FirstDailyDay) % Producers.Length;
            var isMajorMarket = dayNumber >= FirstDay &&
                                (dayNumber - FirstDay) % IntervalDays == 0;
            return new NeighborhoodMarketEvent(dayNumber,
                Producers[index],
                isMajorMarket ? ExtraWaste : DailyExtraWaste,
                isMajorMarket ? ExtraIncome : DailyExtraIncome,
                isMajorMarket ? CleanBonus : 0,
                isMajorMarket);
        }

        public static NeighborhoodMarketEvent NextOnOrAfter(int dayNumber)
        {
            return ForDay(Math.Max(FirstDailyDay, dayNumber)).Value;
        }

        public static NeighborhoodMarketEvent NextUnprocessed(double totalSeconds)
        {
            var now = Math.Max(0d, totalSeconds);
            var dayIndex = (int)(now / SimulationClockModel.CycleSeconds);
            var dayNumber = dayIndex + 1;
            var wasteProductionTime = dayIndex * SimulationClockModel.CycleSeconds +
                                      WasteCollectionSchedule.DailyProductionSeconds;
            return NextOnOrAfter(now >= wasteProductionTime ? dayNumber + 1 : dayNumber);
        }
    }
}
