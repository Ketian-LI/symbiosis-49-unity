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
        private RoomLayoutEditorController layoutEditor;
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
        public float Balance => Model?.Balance ?? 0f;
        public float ExpectedDailyIncome => dailyProductionOverride ?? CalculateDefaultProduction();
        public int ExpectedDailySpending => dailyFoodCostOverride ??
                                            ResourceEconomyModel.FoodServiceCost(
                                                wasteManagement?.ResidentCount ?? 0,
                                                wasteManagement?.OperatingFoodShopCount ?? 0);

        public void Initialize(
            GameRuntimeController runtimeController,
            WasteManagementController wasteController,
            RoomLayoutEditorController editorController)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            Model = new ResourceEconomyModel();
            lastProcessedTime = runtime.Clock.TotalSeconds;
            runtime.RestartRequested += HandleRestartRequested;
            wasteManagement.StateChanged += HandleDependencyStateChanged;
            layoutEditor.BindResourceEconomy(TrySpend, () => Balance);
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
            if (Model == null || !Model.TrySpend(cost))
            {
                var missing = Mathf.Max(0, Mathf.CeilToInt(cost - Balance));
                InsufficientResources?.Invoke(missing);
                return false;
            }

            ResourceSpent?.Invoke(cost);
            StateChanged?.Invoke();
            return true;
        }

        public void BindRunSummaryProvider(Action<RunResultsData> populateResults)
        {
            populateRunSummary = populateResults;
        }

        public bool TryEmergencyCollect(string wasteRoomId)
        {
            if (!Model.CanAfford(ResourceEconomyModel.EmergencyCollectionCost))
            {
                InsufficientResources?.Invoke(
                    Mathf.CeilToInt(ResourceEconomyModel.EmergencyCollectionCost - Balance));
                return false;
            }

            if (!wasteManagement.TryEmergencyCollect(
                    wasteRoomId,
                    Mathf.FloorToInt(Balance),
                    out var cost))
            {
                return false;
            }

            return TrySpend(cost);
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
            SettlementPreparing?.Invoke(completedDay);
            var before = Balance;
            var settlement = Model.SettleDay(
                completedDay,
                ExpectedDailyIncome,
                ExpectedDailySpending);

            if (settlement.Production > 0f)
            {
                ResourceGained?.Invoke(settlement.Production);
            }
            if (settlement.FoodServiceCost > 0)
            {
                ResourceSpent?.Invoke(settlement.FoodServiceCost);
            }
            if (before < ResourceEconomyModel.MaximumBalance &&
                Mathf.Approximately(Balance, ResourceEconomyModel.MaximumBalance))
            {
                FullCapacityReached?.Invoke();
            }

            DaySettled?.Invoke(settlement);
            StateChanged?.Invoke();
            if (settlement.Failed)
            {
                runtime.EndRun(BuildNegativeResourceResults(settlement));
            }
        }

        private RunResultsData BuildNegativeResourceResults(ResourceSettlement settlement)
        {
            var results = new RunResultsData
            {
                endReason = RunEndReason.NegativeResourceBalance,
                daysSurvived = Mathf.Max(1, settlement.DayNumber),
                cumulativeResourceIncome = Mathf.RoundToInt(Model.CumulativeIncome),
                cumulativeResourceSpending = Mathf.RoundToInt(Model.CumulativeSpending),
                finalResourceBalance = Mathf.FloorToInt(Model.Balance),
                peakResourceBalance = Mathf.RoundToInt(Model.PeakBalance),
                finalResidents = wasteManagement.ResidentCount,
                peakResidents = wasteManagement.ResidentCount
            };
            populateRunSummary?.Invoke(results);
            return results;
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
