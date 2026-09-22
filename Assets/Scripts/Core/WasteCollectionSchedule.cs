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
}
