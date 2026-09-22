using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ResearchIndicatorData
    {
        public float humanFunction;
        public float foodAccessibility;
        public float habitatProvision;
        public float animalSafety;
    }

    [Serializable]
    public sealed class ResearchRecordData
    {
        public string participantCode;
        public string startedAtUtc;
        public int configuredDurationMinutes;
        public int configuredDeathLimit;
        public float elapsedResearchSeconds;
        public float operationSeconds;
        public string endingReason;
        public List<RoomPlacementData> initialLayout = new();
        public List<RoomPlacementData> finalLayout = new();
        public ResearchIndicatorData initialIndicators = new();
        public ResearchIndicatorData finalIndicators = new();
        public List<ResearchEventData> events = new();
    }

    public sealed class ResearchSessionController : MonoBehaviour
    {
        private const string ParticipantSequenceKey = "symbiosis49.researchParticipantSequence";

        private GameRuntimeController runtime;
        private RoomLayoutEditorController layout;
        private AnimalMortalityController mortality;
        private EcologicalMetricsController metrics;
        private ResearchRecordData record;
        private string lastMetricSignature;
        private bool endingForTime;

        public event Action StateChanged;
        public ResearchSessionModel Model { get; private set; }
        public string LastExportDirectory { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            AnimalMortalityController mortalityController,
            EcologicalMetricsController metricsController)
        {
            runtime = runtimeController;
            layout = layoutController;
            mortality = mortalityController;
            metrics = metricsController;
            Model = new ResearchSessionModel();
            Model.Configure(SuggestParticipantCode(), ResearchSessionModel.DefaultDurationMinutes, ResearchSessionModel.DefaultDeathLimit);
            runtime.RestartRequested += HandleRestartRequested;
            runtime.RunEnded += HandleRunEnded;
            layout.LayoutConfirmed += HandleLayoutConfirmed;
            mortality.AnimalDied += HandleAnimalDied;
            metrics.StateChanged += HandleMetricsChanged;
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
                runtime.RunEnded -= HandleRunEnded;
            }
            if (layout != null)
            {
                layout.LayoutConfirmed -= HandleLayoutConfirmed;
            }
            if (mortality != null)
            {
                mortality.AnimalDied -= HandleAnimalDied;
            }
            if (metrics != null)
            {
                metrics.StateChanged -= HandleMetricsChanged;
            }
        }

        public void Configure(string participantCode, int durationMinutes, int deathLimit)
        {
            Model.Configure(participantCode, durationMinutes, deathLimit);
            StateChanged?.Invoke();
        }

        public void AdjustDuration(int deltaMinutes)
        {
            Configure(Model.ParticipantCode, Model.DurationMinutes + deltaMinutes, Model.DeathLimit);
        }

        public void AdjustDeathLimit(int delta)
        {
            Configure(Model.ParticipantCode, Model.DurationMinutes, Model.DeathLimit + delta);
        }

        private void Update()
        {
            if (runtime == null || runtime.Mode != GameMode.Research || !Model.IsRunning || runtime.ResultsOpen)
            {
                return;
            }
            Model.Advance(Time.unscaledDeltaTime, runtime.HasActiveRun && !runtime.IsPaused);
            StateChanged?.Invoke();
            if (!Model.TimeExpired || endingForTime)
            {
                return;
            }

            endingForTime = true;
            var results = new RunResultsData
            {
                endReason = RunEndReason.ResearchTimeExpired,
                daysSurvived = Mathf.Max(1, runtime.Clock.DayNumber)
            };
            mortality.PopulateResults(results);
            PopulateResearchResults(results);
            runtime.EndRun(results);
            endingForTime = false;
        }

        public string ExportResearchRecord()
        {
            if (record == null)
            {
                return string.Empty;
            }
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var folder = Path.Combine(
                Application.persistentDataPath,
                "ResearchRecords",
                $"{ResearchSessionModel.SanitizeParticipantCode(record.participantCode)}_{timestamp}");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "research-record.json"), JsonUtility.ToJson(record, true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(folder, "events.csv"), BuildCsv(record.events), Encoding.UTF8);
            if (Application.isPlaying)
            {
                CaptureResultsScreenshot(Path.Combine(folder, "results.png"));
            }
            LastExportDirectory = folder;
            return folder;
        }

        private static void CaptureResultsScreenshot(string path)
        {
            var screenCaptureType = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule");
            var captureMethod = screenCaptureType?.GetMethod(
                "CaptureScreenshot",
                new[] { typeof(string) });
            captureMethod?.Invoke(null, new object[] { path });
        }

        private void HandleRestartRequested()
        {
            if (runtime.Mode != GameMode.Research)
            {
                Model.End("mode_changed");
                return;
            }
            Model.Begin();
            mortality.ConfigureDeathLimit(Model.DeathLimit);
            record = new ResearchRecordData
            {
                participantCode = Model.ParticipantCode,
                startedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                configuredDurationMinutes = Model.DurationMinutes,
                configuredDeathLimit = Model.DeathLimit,
                initialLayout = layout.ExportLayout().Select(ClonePlacement).ToList(),
                initialIndicators = CaptureIndicators()
            };
            lastMetricSignature = MetricSignature(metrics.Snapshot);
            var next = PlayerPrefs.GetInt(ParticipantSequenceKey, 1) + 1;
            PlayerPrefs.SetInt(ParticipantSequenceKey, next);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        private void HandleRunEnded(RunResultsData results)
        {
            if (runtime.Mode != GameMode.Research || record == null)
            {
                return;
            }
            PopulateResearchResults(results);
            record.finalLayout = layout.ExportLayout().Select(ClonePlacement).ToList();
            record.finalIndicators = CaptureIndicators();
            record.endingReason = results.endReason.ToString();
            Model.End(record.endingReason);
            record.events = Model.Events.Select(CloneEvent).ToList();
            StateChanged?.Invoke();
        }

        private void HandleLayoutConfirmed()
        {
            if (!Model.IsRunning)
            {
                return;
            }
            var summary = string.Join("|", layout.ExportLayout()
                .OrderBy(item => item.id, StringComparer.Ordinal)
                .Select(item => $"{item.id}:{item.column},{item.row},r{item.quarterTurns}"));
            Model.AddEvent("layout_change", summary);
        }

        private void HandleAnimalDied(WildlifeSpecies species, AnimalDeathCause cause, int totalDeaths)
        {
            if (Model.IsRunning)
            {
                Model.AddEvent("animal_death", $"species={species};cause={cause};total={totalDeaths}");
            }
        }

        private void HandleMetricsChanged()
        {
            if (!Model.IsRunning)
            {
                return;
            }
            var signature = MetricSignature(metrics.Snapshot);
            if (signature == lastMetricSignature)
            {
                return;
            }
            lastMetricSignature = signature;
            Model.AddEvent("indicator_change", signature);
        }

        private void PopulateResearchResults(RunResultsData results)
        {
            results.configuredDeathLimit = Model.DeathLimit;
            results.researchParticipantCode = Model.ParticipantCode;
            results.researchDurationSeconds = Model.DurationMinutes * 60f;
            results.researchElapsedSeconds = Model.ActiveSeconds;
            results.researchOperationSeconds = Model.OperationSeconds;
        }

        private ResearchIndicatorData CaptureIndicators()
        {
            var snapshot = metrics.Snapshot;
            return new ResearchIndicatorData
            {
                humanFunction = snapshot.HumanFunction,
                foodAccessibility = snapshot.FoodAccessibility,
                habitatProvision = snapshot.HabitatProvision,
                animalSafety = snapshot.AnimalSafety
            };
        }

        private static string MetricSignature(EcologicalMetricsSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "human={0:F3};food={1:F3};habitat={2:F3};safety={3:F3}",
                snapshot.HumanFunction,
                snapshot.FoodAccessibility,
                snapshot.HabitatProvision,
                snapshot.AnimalSafety);
        }

        private static string BuildCsv(IEnumerable<ResearchEventData> events)
        {
            var builder = new StringBuilder("simulation_seconds,operation_seconds,type,detail\n");
            foreach (var item in events ?? Array.Empty<ResearchEventData>())
            {
                builder.Append(item.simulationSeconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.operationSeconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Csv(item.type)).Append(',')
                    .Append(Csv(item.detail)).Append('\n');
            }
            return builder.ToString();
        }

        private static string Csv(string value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        }

        private static RoomPlacementData ClonePlacement(RoomPlacementData item)
        {
            return new RoomPlacementData
            {
                id = item.id,
                column = item.column,
                row = item.row,
                quarterTurns = item.quarterTurns
            };
        }

        private static ResearchEventData CloneEvent(ResearchEventData item)
        {
            return new ResearchEventData
            {
                simulationSeconds = item.simulationSeconds,
                operationSeconds = item.operationSeconds,
                type = item.type,
                detail = item.detail
            };
        }

        private static string SuggestParticipantCode()
        {
            return $"P{Mathf.Max(1, PlayerPrefs.GetInt(ParticipantSequenceKey, 1)):000}";
        }
    }
}
