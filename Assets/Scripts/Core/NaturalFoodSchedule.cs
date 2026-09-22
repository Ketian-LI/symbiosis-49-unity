using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Core
{
    public enum NaturalFoodScheduleEventKind
    {
        DuskProduction,
        NightProduction
    }

    public readonly struct NaturalFoodScheduleEvent
    {
        public NaturalFoodScheduleEvent(NaturalFoodScheduleEventKind kind, int dayNumber)
        {
            Kind = kind;
            DayNumber = dayNumber;
        }

        public NaturalFoodScheduleEventKind Kind { get; }
        public int DayNumber { get; }
    }

    public static class NaturalFoodSchedule
    {
        public const double DuskStartSeconds =
            SimulationClockModel.DawnSeconds + SimulationClockModel.DaySeconds;
        public const double NightStartSeconds =
            DuskStartSeconds + SimulationClockModel.DuskSeconds;

        public static IReadOnlyList<NaturalFoodScheduleEvent> EventsBetween(
            double previousTotalSeconds,
            double currentTotalSeconds)
        {
            var events = new List<NaturalFoodScheduleEvent>();
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
                AddIfCrossed(events, NaturalFoodScheduleEventKind.DuskProduction,
                    dayNumber, dayIndex, DuskStartSeconds, previous, current);
                AddIfCrossed(events, NaturalFoodScheduleEventKind.NightProduction,
                    dayNumber, dayIndex, NightStartSeconds, previous, current);

            }
            return events;
        }

        private static void AddIfCrossed(
            ICollection<NaturalFoodScheduleEvent> events,
            NaturalFoodScheduleEventKind kind,
            int dayNumber,
            int dayIndex,
            double secondsIntoDay,
            double previous,
            double current)
        {
            var absolute = dayIndex * SimulationClockModel.CycleSeconds + secondsIntoDay;
            if (absolute > previous && absolute <= current)
            {
                events.Add(new NaturalFoodScheduleEvent(kind, dayNumber));
            }
        }
    }
}
