using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ResearchEventData
    {
        public float simulationSeconds;
        public float operationSeconds;
        public string type;
        public string detail;
    }

    [Serializable]
    public sealed class ResearchSessionModel
    {
        public const int DefaultDurationMinutes = 18;
        public const int DefaultDeathLimit = 5;

        private readonly List<ResearchEventData> events = new();

        public string ParticipantCode { get; private set; } = "P001";
        public int DurationMinutes { get; private set; } = DefaultDurationMinutes;
        public int DeathLimit { get; private set; } = DefaultDeathLimit;
        public float ActiveSeconds { get; private set; }
        public float OperationSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public bool TimeExpired => IsRunning && ActiveSeconds >= DurationMinutes * 60f;
        public float RemainingFraction => DurationMinutes <= 0
            ? 0f
            : Math.Max(0f, 1f - ActiveSeconds / (DurationMinutes * 60f));
        public IReadOnlyList<ResearchEventData> Events => events;

        public void Configure(string participantCode, int durationMinutes, int deathLimit)
        {
            if (IsRunning)
            {
                return;
            }
            ParticipantCode = SanitizeParticipantCode(participantCode);
            DurationMinutes = Math.Clamp(durationMinutes, 6, 60);
            DeathLimit = Math.Clamp(deathLimit, 1, 20);
        }

        public void Begin()
        {
            ActiveSeconds = 0f;
            OperationSeconds = 0f;
            events.Clear();
            IsRunning = true;
            AddEvent("session_start", $"duration={DurationMinutes};death_limit={DeathLimit}");
        }

        public void Advance(float unscaledDeltaTime, bool simulationActive)
        {
            if (!IsRunning || unscaledDeltaTime <= 0f)
            {
                return;
            }
            OperationSeconds += unscaledDeltaTime;
            if (simulationActive)
            {
                ActiveSeconds = Math.Min(DurationMinutes * 60f, ActiveSeconds + unscaledDeltaTime);
            }
        }

        public void AddEvent(string type, string detail)
        {
            if (!IsRunning)
            {
                return;
            }
            events.Add(new ResearchEventData
            {
                simulationSeconds = ActiveSeconds,
                operationSeconds = OperationSeconds,
                type = type ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void End(string reason)
        {
            if (!IsRunning)
            {
                return;
            }
            AddEvent("session_end", reason);
            IsRunning = false;
        }

        public static string SanitizeParticipantCode(string value)
        {
            var compact = Regex.Replace((value ?? string.Empty).ToUpperInvariant(), "[^A-Z0-9_-]", string.Empty);
            return string.IsNullOrEmpty(compact) ? "P001" : compact.Substring(0, Math.Min(16, compact.Length));
        }
    }
}
