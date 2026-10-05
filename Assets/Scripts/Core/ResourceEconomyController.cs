using System;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class ResourceEconomyController : MonoBehaviour
    {
        private GameRuntimeController runtime;
        private WasteManagementController wasteManagement;
        private double lastProcessedTime;
        private float? dailyProductionOverride;
        private int? dailyFoodCostOverride;
        private bool initialized;
        private Action<RunResultsData> populateRunSummary;

        public event Action StateChanged;
        public event Action<float> ResourceGained;
        public event Action<int> ResourceSpent;
        public event Action<int> InsufficientResources;
        public event Action FullCapacityReached;
        public event Action<int> SettlementPreparing;
        public event Action<ResourceSettlement> DaySettled;

        public ResourceEconomyModel Model { get; private set; }
        public int CurrentSettlementDay { get; private set; }
        public float Balance => Model?.Balance ?? 0f;
        public float ExpectedDailyIncome =>
            (dailyProductionOverride ?? CalculateDefaultProduction()) + MarketIncomeForDay(
                CurrentSettlementDay > 0 ? CurrentSettlementDay : runtime?.Clock.DayNumber ?? 0);
        public bool IncomeEstimateUsesPreviousSettlement => dailyProductionOverride.HasValue;
        public bool FoodEstimateUsesPreviousSettlement => dailyFoodCostOverride.HasValue;
        public int ExpectedFoodServiceCost => dailyFoodCostOverride ??
                                              ResourceEconomyModel.FoodServiceCost(
                                                  wasteManagement?.ResidentCount ?? 0,
                                                  wasteManagement?.OperatingFoodShopCount ?? 0);
        public int ExpectedInfrastructureCost => ResourceEconomyModel.InfrastructureCost(
            wasteManagement?.ResidentCount ?? 0,
            wasteManagement?.Model?.AffectedWasteRoomCount ?? 0);
        public int ExpectedDailySpending => ExpectedFoodServiceCost + ExpectedInfrastructureCost;

        public void Initialize(
            GameRuntimeController runtimeController,
            WasteManagementController wasteController,
            RoomLayoutEditorController editorController)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            _ = editorController ?? throw new ArgumentNullException(nameof(editorController));
            Model = new ResourceEconomyModel();
            lastProcessedTime = runtime.Clock.TotalSeconds;
            runtime.RestartRequested += HandleRestartRequested;
            wasteManagement.StateChanged += HandleDependencyStateChanged;
            initialized = true;
            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying || !runtime.HasActiveRun)
            {
                return;
            }

            ProcessUntil(runtime.Clock.TotalSeconds);
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            if (wasteManagement != null)
            {
                wasteManagement.StateChanged -= HandleDependencyStateChanged;
            }
        }

        public void SetDailyProductionEstimate(float completedWorkProduction)
        {
            dailyProductionOverride = Mathf.Max(0f, completedWorkProduction);
            StateChanged?.Invoke();
        }

        public void SetDailyEconomyEstimate(
            float completedWorkProduction,
            int foodServiceCost)
        {
            dailyProductionOverride = Mathf.Max(0f, completedWorkProduction);
            dailyFoodCostOverride = Mathf.Max(0, foodServiceCost);
            StateChanged?.Invoke();
        }

        public void ClearDailyProductionEstimate()
        {
            dailyProductionOverride = null;
            dailyFoodCostOverride = null;
            StateChanged?.Invoke();
        }

        public bool TrySpend(int cost)
        {
            // Retained only for callers compiled against older revisions.
            // Currency has no active gameplay meaning and cannot be spent.
            return false;
        }

        public void BindRunSummaryProvider(Action<RunResultsData> populateResults)
        {
            populateRunSummary = populateResults;
        }

        public void EndRunForInsufficientWorkers(int completedDay, int workingResidents, int requiredWorkers)
        {
            if (runtime == null || !runtime.HasActiveRun)
            {
                return;
            }

            var results = new RunResultsData
            {
                endReason = RunEndReason.InsufficientWorkers,
                daysSurvived = Mathf.Max(1, completedDay),
                lastWorkingResidents = Mathf.Max(0, workingResidents),
                requiredWorkingResidents = Mathf.Max(1, requiredWorkers)
            };
            populateRunSummary?.Invoke(results);
            runtime.EndRun(results);
        }

        public void EndRunForInsufficientResidents(int completedDay, int residentCount, int minimumResidents)
        {
            if (runtime == null || !runtime.HasActiveRun)
            {
                return;
            }

            var results = new RunResultsData
            {
                endReason = RunEndReason.InsufficientResidents,
                daysSurvived = Mathf.Max(1, completedDay),
                finalResidents = Mathf.Max(0, residentCount),
                requiredResidents = Mathf.Max(1, minimumResidents)
            };
            populateRunSummary?.Invoke(results);
            runtime.EndRun(results);
        }

        public bool TryEmergencyCollect(string wasteRoomId)
        {
            // Manual collection formerly consumed resource points. Keep the
            // scheduled waste service; do not turn this into a free unlimited
            // action merely because the currency has been removed.
            return false;
        }

        public void ProcessUntil(double totalSeconds)
        {
            if (Model == null)
            {
                return;
            }

            if (totalSeconds < lastProcessedTime)
            {
                lastProcessedTime = Math.Max(0d, totalSeconds);
                return;
            }

            var firstBoundary = ((int)(lastProcessedTime / SimulationClockModel.CycleSeconds) + 1) *
                                SimulationClockModel.CycleSeconds;
            for (var boundary = firstBoundary;
                 boundary <= totalSeconds;
                 boundary += SimulationClockModel.CycleSeconds)
            {
                var completedDay = (int)(boundary / SimulationClockModel.CycleSeconds);
                SettleDay(completedDay);
                if (!runtime.HasActiveRun)
                {
                    break;
                }
            }

            lastProcessedTime = Math.Max(0d, totalSeconds);
        }

        public void RestoreSession(
            float balance,
            float cumulativeIncome,
            float cumulativeSpending,
            float unstoredSurplus,
            float peakBalance)
        {
            Model?.Restore(
                balance,
                cumulativeIncome,
                cumulativeSpending,
                unstoredSurplus,
                peakBalance);
            lastProcessedTime = runtime.Clock.TotalSeconds;
            StateChanged?.Invoke();
        }

        private void SettleDay(int completedDay)
        {
            CurrentSettlementDay = completedDay;
            try
            {
                SettlementPreparing?.Invoke(completedDay);
                // Preparing the settlement can kill an animal and end the run.
                // Never apply another resource settlement or replace its result.
                if (!runtime.HasActiveRun)
                {
                    return;
                }

                // DaySettled is also the day-boundary signal for ecology,
                // waste and reporting. Emit it without a hidden currency
                // calculation now that resource points are not a rule.
                var settlement = new ResourceSettlement(
                    completedDay, 0f, 0f, 0, 0, 0f, 0f);
                DaySettled?.Invoke(settlement);
                StateChanged?.Invoke();
            }
            finally
            {
                CurrentSettlementDay = 0;
            }
        }

        private float CalculateDefaultProduction()
        {
            if (wasteManagement == null || wasteManagement.OperatingFoodShopCount <= 0)
            {
                return 0f;
            }

            return Mathf.Min(
                wasteManagement.ResidentCount,
                wasteManagement.OperatingFoodShopCount * 4);
        }

        private int MarketIncomeForDay(int dayNumber)
        {
            if (runtime == null || runtime.Mode != GameMode.Sandbox ||
                NeighborhoodMarketSchedule.ForDay(dayNumber) is not { } market ||
                wasteManagement == null || !wasteManagement.MarketProducerOperating(market.RoomId))
            {
                return 0;
            }
            return market.ExtraIncome +
                   (CurrentSettlementDay == dayNumber &&
                    wasteManagement.Model?.AffectedWasteRoomCount == 0
                       ? market.CleanBonus
                       : 0);
        }

        private void HandleRestartRequested()
        {
            dailyProductionOverride = null;
            dailyFoodCostOverride = null;
            lastProcessedTime = 0d;
            Model?.Reset();
            StateChanged?.Invoke();
        }

        private void HandleDependencyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
