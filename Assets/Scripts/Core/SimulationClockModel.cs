using System;

namespace UrbanWildlifeRooms.Core
{
    public enum DayPhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    /// <summary>
    /// Deterministic, Unity-independent simulation clock.
    /// One in-game day lasts six real-time minutes at 1x speed.
    /// </summary>
    [Serializable]
    public sealed class SimulationClockModel
    {
        public const double DawnSeconds = 30d;
        public const double DaySeconds = 150d;
        public const double DuskSeconds = 30d;
        public const double NightSeconds = 150d;
        public const double CycleSeconds = DawnSeconds + DaySeconds + DuskSeconds + NightSeconds;

        private double totalSeconds;

        public int DayNumber => (int)(totalSeconds / CycleSeconds) + 1;
        public double TotalSeconds => totalSeconds;
        public double SecondsIntoDay => totalSeconds % CycleSeconds;
        public float CycleProgress => (float)(SecondsIntoDay / CycleSeconds);

        public DayPhase Phase
        {
            get
            {
                var seconds = SecondsIntoDay;
                if (seconds < DawnSeconds)
                {
                    return DayPhase.Dawn;
                }

                if (seconds < DawnSeconds + DaySeconds)
                {
                    return DayPhase.Day;
                }

                if (seconds < DawnSeconds + DaySeconds + DuskSeconds)
                {
                    return DayPhase.Dusk;
                }

                return DayPhase.Night;
            }
        }

        public float PhaseProgress
        {
            get
            {
                var seconds = SecondsIntoDay;
                return Phase switch
                {
                    DayPhase.Dawn => (float)(seconds / DawnSeconds),
                    DayPhase.Day => (float)((seconds - DawnSeconds) / DaySeconds),
                    DayPhase.Dusk => (float)((seconds - DawnSeconds - DaySeconds) / DuskSeconds),
                    DayPhase.Night => (float)((seconds - DawnSeconds - DaySeconds - DuskSeconds) / NightSeconds),
                    _ => 0f
                };
            }
        }

        public void Advance(double unscaledDeltaSeconds, float speedMultiplier)
        {
            if (unscaledDeltaSeconds <= 0d || speedMultiplier <= 0f)
            {
                return;
            }

            totalSeconds += unscaledDeltaSeconds * speedMultiplier;
        }

        public void Restore(double elapsedSeconds)
        {
            totalSeconds = Math.Max(0d, elapsedSeconds);
        }

        public void Reset()
        {
            totalSeconds = 0d;
        }
    }
}
