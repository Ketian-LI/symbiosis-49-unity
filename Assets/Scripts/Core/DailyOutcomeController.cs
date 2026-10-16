using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct DailyOutcomeReport
    {
        public DailyOutcomeReport(DailyOutcomeSaveData state)
        {
            Day = state.lastDay;
            MovedRooms = state.lastMovedRooms;
            WorkingResidents = state.lastWorkingResidents;
            WorkingResidentsKnown = state.lastWorkingResidentsKnown;
            PlannedWorkersKnown = state.lastPlannedWorkersKnown;
            PlannedWorkers = state.lastPlannedWorkers;
            PigeonSeedForecastKnown = state.lastPigeonSeedForecastKnown;
            PigeonSeedCeiling = state.lastPigeonSeedCeiling;
            PigeonForecastBirds = state.lastPigeonForecastBirds;
            Production = state.lastProduction;
            Spending = state.lastSpending;
            ClosingBalance = state.lastClosingBalance;
            Deaths = state.lastDeaths;
            StarvationDeaths = state.lastStarvationDeaths;
            TrafficDeaths = state.lastTrafficDeaths;
            PredationDeaths = state.lastPredationDeaths;
            WasteIssues = state.lastWasteIssues;
            MealsKnown = state.lastMealsKnown;
            FedAnimals = state.lastFedAnimals;
            LivingAnimals = state.lastLivingAnimals;
            FedPigeons = state.lastFedPigeons;
            LivingPigeons = state.lastLivingPigeons;
            SeedPortionsLeft = state.lastSeedPortionsLeft;
        }

        public int Day { get; }
        public int MovedRooms { get; }
        public int WorkingResidents { get; }
        public bool WorkingResidentsKnown { get; }
        public bool PlannedWorkersKnown { get; }
        public int PlannedWorkers { get; }
        // Legacy day-end comparison: route matching against dawn seed target
        // capacities, not actual newly added portions or guaranteed meals.
        public bool PigeonSeedForecastKnown { get; }
        public int PigeonSeedCeiling { get; }
        public int PigeonForecastBirds { get; }
        public float Production { get; }
        public int Spending { get; }
        public float ClosingBalance { get; }
        public int Deaths { get; }
        public int StarvationDeaths { get; }
        public int TrafficDeaths { get; }
        public int PredationDeaths { get; }
        public int WasteIssues { get; }
        public bool MealsKnown { get; }
        public int FedAnimals { get; }
        public int LivingAnimals { get; }
        public int FedPigeons { get; }
        public int LivingPigeons { get; }
        public int SeedPortionsLeft { get; }
        public int OtherDeaths => Math.Max(0, Deaths - StarvationDeaths - TrafficDeaths - PredationDeaths);
    }

    // These observations share a day; movement alone is not claimed to cause them.
    public sealed class DailyOutcomeModel
    {
        private DailyOutcomeSaveData state = new();

        public DailyOutcomeReport LastReport => new(state);

        public void RecordMovement(int day, int count, int? plannedWorkers = null,
            int? pigeonSeedCeiling = null, int pigeonForecastBirds = 0)
        {
            if (day <= state.lastDay || count <= 0)
            {
                return;
            }
            if (state.pendingDay != day)
            {
                state.pendingDay = day;
                state.pendingMovedRooms = 0;
            }
            state.pendingMovedRooms += count;
            if (plannedWorkers.HasValue)
            {
                state.pendingCommuteForecastDay = day;
                state.pendingPlannedWorkers = Math.Max(0, plannedWorkers.Value);
            }
            // A second rearrangement before the forecast day supersedes the
            // previous route forecast. Holding the layout does not.
            state.pendingPigeonForecastDay = pigeonSeedCeiling.HasValue ? day + 1 : 0;
            state.pendingPigeonSeedCeiling = Math.Max(0, pigeonSeedCeiling ?? 0);
            state.pendingPigeonForecastBirds = Math.Max(0, pigeonForecastBirds);
        }

        public void CompleteDay(ResourceSettlement settlement, int workingResidents, int totalDeaths,
            int starvationDeaths, int trafficDeaths, int wasteIssues, int predationDeaths = 0,
            AnimalMealDaySummary meals = default)
        {
            if (settlement.DayNumber <= state.lastDay)
            {
                return;
            }
            state.lastDay = settlement.DayNumber;
            state.lastMovedRooms = state.pendingDay == settlement.DayNumber
                ? state.pendingMovedRooms : 0;
            state.lastWorkingResidents = Math.Max(0, workingResidents);
            state.lastWorkingResidentsKnown = true;
            state.lastPlannedWorkersKnown = state.pendingCommuteForecastDay == settlement.DayNumber;
            state.lastPlannedWorkers = state.lastPlannedWorkersKnown
                ? state.pendingPlannedWorkers : 0;
            state.lastPigeonSeedForecastKnown = state.pendingPigeonForecastDay == settlement.DayNumber;
            state.lastPigeonSeedCeiling = state.lastPigeonSeedForecastKnown
                ? state.pendingPigeonSeedCeiling : 0;
            state.lastPigeonForecastBirds = state.lastPigeonSeedForecastKnown
                ? state.pendingPigeonForecastBirds : 0;
            state.lastProduction = settlement.Production;
            state.lastSpending = settlement.TotalCost;
            state.lastClosingBalance = settlement.ClosingBalance;
            state.lastDeaths = Math.Max(0, totalDeaths - state.previousTotalDeaths);
            state.lastStarvationDeaths = Math.Max(0, starvationDeaths - state.previousStarvationDeaths);
            state.lastTrafficDeaths = Math.Max(0, trafficDeaths - state.previousTrafficDeaths);
            state.lastPredationDeaths = Math.Max(0, predationDeaths - state.previousPredationDeaths);
            state.lastWasteIssues = Math.Max(0, wasteIssues);
            state.lastMealsKnown = meals.Day == settlement.DayNumber;
            state.lastFedAnimals = state.lastMealsKnown ? Math.Max(0, meals.Fed) : 0;
            state.lastLivingAnimals = state.lastMealsKnown ? Math.Max(0, meals.Living) : 0;
            state.lastFedPigeons = state.lastMealsKnown ? Math.Max(0, meals.PigeonsFed) : 0;
            state.lastLivingPigeons = state.lastMealsKnown ? Math.Max(0, meals.PigeonsLiving) : 0;
            state.lastSeedPortionsLeft = state.lastMealsKnown ? Math.Max(0, meals.SeedPortionsLeft) : 0;
            state.previousTotalDeaths = Math.Max(0, totalDeaths);
            state.previousStarvationDeaths = Math.Max(0, starvationDeaths);
            state.previousTrafficDeaths = Math.Max(0, trafficDeaths);
            state.previousPredationDeaths = Math.Max(0, predationDeaths);
            state.predationHistoryKnown = true;
            state.pendingDay = 0;
            state.pendingMovedRooms = 0;
            state.pendingCommuteForecastDay = 0;
            state.pendingPlannedWorkers = 0;
            if (state.lastPigeonSeedForecastKnown)
            {
                state.pendingPigeonForecastDay = 0;
                state.pendingPigeonSeedCeiling = 0;
                state.pendingPigeonForecastBirds = 0;
            }
        }

        public void Reset(int totalDeaths = 0, int starvationDeaths = 0, int trafficDeaths = 0,
            int predationDeaths = 0)
        {
            state = new DailyOutcomeSaveData
            {
                previousTotalDeaths = Math.Max(0, totalDeaths),
                previousStarvationDeaths = Math.Max(0, starvationDeaths),
                previousTrafficDeaths = Math.Max(0, trafficDeaths),
                previousPredationDeaths = Math.Max(0, predationDeaths),
                predationHistoryKnown = true
            };
        }

        public DailyOutcomeSaveData Export() => new()
        {
            pendingDay = state.pendingDay,
            pendingMovedRooms = state.pendingMovedRooms,
            pendingCommuteForecastDay = state.pendingCommuteForecastDay,
            pendingPlannedWorkers = state.pendingPlannedWorkers,
            pendingPigeonForecastDay = state.pendingPigeonForecastDay,
            pendingPigeonSeedCeiling = state.pendingPigeonSeedCeiling,
            pendingPigeonForecastBirds = state.pendingPigeonForecastBirds,
            previousTotalDeaths = state.previousTotalDeaths,
            previousStarvationDeaths = state.previousStarvationDeaths,
            previousTrafficDeaths = state.previousTrafficDeaths,
            previousPredationDeaths = state.previousPredationDeaths,
            predationHistoryKnown = state.predationHistoryKnown,
            lastDay = state.lastDay,
            lastMovedRooms = state.lastMovedRooms,
            lastWorkingResidents = state.lastWorkingResidents,
            lastWorkingResidentsKnown = state.lastWorkingResidentsKnown,
            lastPlannedWorkersKnown = state.lastPlannedWorkersKnown,
            lastPlannedWorkers = state.lastPlannedWorkers,
            lastPigeonSeedForecastKnown = state.lastPigeonSeedForecastKnown,
            lastPigeonSeedCeiling = state.lastPigeonSeedCeiling,
            lastPigeonForecastBirds = state.lastPigeonForecastBirds,
            lastProduction = state.lastProduction,
            lastSpending = state.lastSpending,
            lastClosingBalance = state.lastClosingBalance,
            lastDeaths = state.lastDeaths,
            lastStarvationDeaths = state.lastStarvationDeaths,
            lastTrafficDeaths = state.lastTrafficDeaths,
            lastPredationDeaths = state.lastPredationDeaths,
            lastWasteIssues = state.lastWasteIssues,
            lastMealsKnown = state.lastMealsKnown,
            lastFedAnimals = state.lastFedAnimals,
            lastLivingAnimals = state.lastLivingAnimals,
            lastFedPigeons = state.lastFedPigeons,
            lastLivingPigeons = state.lastLivingPigeons,
            lastSeedPortionsLeft = state.lastSeedPortionsLeft
        };

        public void Restore(DailyOutcomeSaveData saved, int totalDeaths,
            int starvationDeaths, int trafficDeaths, int predationDeaths = 0)
        {
            if (saved == null)
            {
                Reset(totalDeaths, starvationDeaths, trafficDeaths, predationDeaths);
                return;
            }
            state = new DailyOutcomeSaveData
            {
                pendingDay = Math.Max(0, saved.pendingDay),
                pendingMovedRooms = Math.Max(0, saved.pendingMovedRooms),
                pendingCommuteForecastDay = Math.Max(0, saved.pendingCommuteForecastDay),
                pendingPlannedWorkers = Math.Max(0, saved.pendingPlannedWorkers),
                pendingPigeonForecastDay = Math.Max(0, saved.pendingPigeonForecastDay),
                pendingPigeonSeedCeiling = Math.Max(0, saved.pendingPigeonSeedCeiling),
                pendingPigeonForecastBirds = Math.Max(0, saved.pendingPigeonForecastBirds),
                previousTotalDeaths = Math.Min(Math.Max(0, saved.previousTotalDeaths), Math.Max(0, totalDeaths)),
                previousStarvationDeaths = Math.Min(Math.Max(0, saved.previousStarvationDeaths), Math.Max(0, starvationDeaths)),
                previousTrafficDeaths = Math.Min(Math.Max(0, saved.previousTrafficDeaths), Math.Max(0, trafficDeaths)),
                // Older saves have no predation baseline. Begin from the
                // currently recorded total rather than inventing past deaths.
                previousPredationDeaths = saved.predationHistoryKnown
                    ? Math.Min(Math.Max(0, saved.previousPredationDeaths), Math.Max(0, predationDeaths))
                    : Math.Max(0, predationDeaths),
                predationHistoryKnown = true,
                lastDay = Math.Max(0, saved.lastDay),
                lastMovedRooms = Math.Max(0, saved.lastMovedRooms),
                lastWorkingResidents = Math.Max(0, saved.lastWorkingResidents),
                lastWorkingResidentsKnown = saved.lastWorkingResidentsKnown,
                lastPlannedWorkersKnown = saved.lastPlannedWorkersKnown,
                lastPlannedWorkers = Math.Max(0, saved.lastPlannedWorkers),
                lastPigeonSeedForecastKnown = saved.lastPigeonSeedForecastKnown,
                lastPigeonSeedCeiling = Math.Max(0, saved.lastPigeonSeedCeiling),
                lastPigeonForecastBirds = Math.Max(0, saved.lastPigeonForecastBirds),
                lastProduction = Math.Max(0f, saved.lastProduction),
                lastSpending = Math.Max(0, saved.lastSpending),
                lastClosingBalance = saved.lastClosingBalance,
                lastDeaths = Math.Max(0, saved.lastDeaths),
                lastStarvationDeaths = Math.Max(0, saved.lastStarvationDeaths),
                lastTrafficDeaths = Math.Max(0, saved.lastTrafficDeaths),
                lastPredationDeaths = saved.predationHistoryKnown
                    ? Math.Max(0, saved.lastPredationDeaths) : 0,
                lastWasteIssues = Math.Max(0, saved.lastWasteIssues),
                lastMealsKnown = saved.lastMealsKnown,
                lastFedAnimals = saved.lastMealsKnown ? Math.Max(0, saved.lastFedAnimals) : 0,
                lastLivingAnimals = saved.lastMealsKnown ? Math.Max(0, saved.lastLivingAnimals) : 0,
                lastFedPigeons = saved.lastMealsKnown ? Math.Max(0, saved.lastFedPigeons) : 0,
                lastLivingPigeons = saved.lastMealsKnown ? Math.Max(0, saved.lastLivingPigeons) : 0,
                lastSeedPortionsLeft = saved.lastMealsKnown ? Math.Max(0, saved.lastSeedPortionsLeft) : 0
            };
        }
    }

    public sealed class DailyOutcomeController : MonoBehaviour
    {
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layout;
        private ResourceEconomyController economy;
        private AnimalMortalityController mortality;
        private ResidentPopulationController residents;
        private AnimalNeedsController needs;
        private WasteManagementController waste;

        public DailyOutcomeModel Model { get; } = new();
        public event Action StateChanged;

        public void Initialize(GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            ResourceEconomyController economyController,
            AnimalMortalityController mortalityController,
            WasteManagementController wasteController,
            ResidentPopulationController residentController,
            AnimalNeedsController needsController)
        {
            runtime = runtimeController;
            layout = layoutController;
            economy = economyController;
            mortality = mortalityController;
            waste = wasteController;
            residents = residentController;
            needs = needsController;
            layout.RoomsMoved += HandleRoomsMoved;
            economy.DaySettled += HandleDaySettled;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (layout != null) layout.RoomsMoved -= HandleRoomsMoved;
            if (economy != null) economy.DaySettled -= HandleDaySettled;
            if (runtime != null) runtime.RestartRequested -= HandleRestartRequested;
        }

        public void RestoreSession(DailyOutcomeSaveData saved)
        {
            Model.Restore(saved, mortality.Model.TotalDeaths,
                mortality.Model.StarvationDeaths, mortality.Model.TrafficDeaths,
                mortality.Model.PredationDeaths);
            StateChanged?.Invoke();
        }

        private void HandleRoomsMoved(IReadOnlyList<string> ids)
        {
            var forecast = layout.LastConfirmedImpact;
            Model.RecordMovement(runtime.Clock.DayNumber, ids?.Count ?? 0,
                forecast?.After.WorkingResidents,
                forecast is { } impact && impact.AfterPigeonSeedMealCeiling >= 0
                    ? impact.AfterPigeonSeedMealCeiling : null,
                forecast?.LivingPigeons ?? 0);
            StateChanged?.Invoke();
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            Model.CompleteDay(settlement, residents.LastReport.WorkingResidents,
                mortality.Model.TotalDeaths,
                mortality.Model.StarvationDeaths, mortality.Model.TrafficDeaths,
                waste.Model.AffectedWasteRoomCount, mortality.Model.PredationDeaths,
                needs.LastCompletedDayMeals);
            StateChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            Model.Reset();
            StateChanged?.Invoke();
        }
    }
}
