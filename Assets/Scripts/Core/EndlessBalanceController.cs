using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class EndlessBalanceController : MonoBehaviour
    {
        private static readonly WildlifeSpecies[] Species =
        {
            WildlifeSpecies.Pigeon, WildlifeSpecies.Squirrel,
            WildlifeSpecies.Hedgehog, WildlifeSpecies.Fox
        };

        private GameRuntimeController runtime;
        private ResourceEconomyController economy;
        private ResidentPopulationController residents;
        private AnimalPopulationController population;
        private AnimalNeedsController needs;
        private AnimalMortalityController mortality;
        private AnimalNavigationCoordinator navigation;
        private OakTreeLifecycleController oakTrees;

        public event Action StateChanged;
        public EndlessBalanceModel Model { get; private set; }
        public WildlifeSpecies? LastArrival { get; private set; }

        public void Initialize(GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            ResidentPopulationController residentController,
            AnimalPopulationController populationController,
            AnimalNeedsController needsController,
            AnimalMortalityController mortalityController,
            AnimalNavigationCoordinator navigationController,
            OakTreeLifecycleController oakTreeController)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            economy = economyController ?? throw new ArgumentNullException(nameof(economyController));
            residents = residentController ?? throw new ArgumentNullException(nameof(residentController));
            population = populationController ?? throw new ArgumentNullException(nameof(populationController));
            needs = needsController ?? throw new ArgumentNullException(nameof(needsController));
            mortality = mortalityController ?? throw new ArgumentNullException(nameof(mortalityController));
            navigation = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
            oakTrees = oakTreeController ?? throw new ArgumentNullException(nameof(oakTreeController));
            Model = new EndlessBalanceModel(runtime.SandboxSettings);
            economy.DaySettled += HandleDaySettled;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (economy != null) economy.DaySettled -= HandleDaySettled;
            if (runtime != null) runtime.RestartRequested -= HandleRestartRequested;
        }

        public void RestoreSession(EndlessBalanceSaveData saved, IEnumerable<string> deadAnimalIds)
        {
            Model = new EndlessBalanceModel(runtime.SandboxSettings);
            Model.Restore(saved);
            if (runtime.Mode == GameMode.Sandbox)
                population.RestoreDeadIds(deadAnimalIds);
            LastArrival = null;
            StateChanged?.Invoke();
        }

        public List<string> ExportDeadIds() => runtime.Mode == GameMode.Sandbox
            ? population.ExportDeadIds()
            : new List<string>();

        public int HabitatCapacityOf(WildlifeSpecies species)
        {
            var map = navigation?.NavigationMap;
            if (map == null || population == null) return 0;
            var access = HabitatFoodNetworkModel.ForecastHabitatAccess(
                map, map, runtime.Clock.DayNumber, oakTrees?.Model);
            return EndlessBalanceModel.HabitatCapacityFor(species, access,
                population.TotalCount(species));
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            if (runtime.Mode != GameMode.Sandbox || !runtime.HasActiveRun ||
                Model == null || settlement.DayNumber <= Model.LastSettledDay)
                return;

            var report = residents.LastReport;
            if (report.DayNumber != settlement.DayNumber)
            {
                Debug.LogError($"[SYMBIOSIS: 49] Missing actual resident settlement for day {settlement.DayNumber}.", this);
                return;
            }
            var livingBeforeArrival = TotalLiving();
            Model.SettleDay(settlement.DayNumber, report.WorkingResidents,
                report.ResidentsEvaluated, livingBeforeArrival);

            var eligible = new Dictionary<WildlifeSpecies, bool>();
            var living = new Dictionary<WildlifeSpecies, int>();
            var availableSlots = new Dictionary<WildlifeSpecies, int>();
            var meals = needs.LastCompletedDayMeals;
            foreach (var species in Species)
            {
                var count = population.LivingCount(species);
                var dormant = population.FirstDormantAgent(species);
                living[species] = count;
                availableSlots[species] = HabitatCapacityOf(species) - count;
                eligible[species] = dormant != null &&
                    meals.Day == settlement.DayNumber &&
                    (meals.LivingOf(species) == 0 ||
                     meals.FedOf(species) * 4 >= meals.LivingOf(species) * 3) &&
                    needs.CanReintroduce(dormant);
            }

            LastArrival = Model.SelectArrival(eligible, living, availableSlots);
            if (LastArrival.HasValue)
            {
                var arrived = population.TryReintroduce(LastArrival.Value);
                if (arrived != null) needs.RegisterArrival(arrived);
                else LastArrival = null;
            }
            StateChanged?.Invoke();

            if ((Model.WildlifeCollapse || Model.CommunityCollapse) && runtime.HasActiveRun)
            {
                var results = new RunResultsData
                {
                    endReason = Model.WildlifeCollapse
                        ? RunEndReason.WildlifePopulationCollapse
                        : RunEndReason.CommunityCollapse,
                    daysSurvived = Mathf.Max(1, settlement.DayNumber),
                    finalWildlifeCount = livingBeforeArrival,
                    requiredWildlifeCount = Model.CurrentWildlifeFloor,
                    finalCommunity = Model.Community
                };
                mortality.PopulateResults(results);
                runtime.EndRun(results);
            }
        }

        private int TotalLiving()
        {
            var total = 0;
            foreach (var species in Species) total += population.LivingCount(species);
            return total;
        }

        private void HandleRestartRequested()
        {
            Model = new EndlessBalanceModel(runtime.SandboxSettings);
            LastArrival = null;
            StateChanged?.Invoke();
        }
    }
}
