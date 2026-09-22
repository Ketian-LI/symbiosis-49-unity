using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class AnimalMortalityController : MonoBehaviour
    {
        private GameRuntimeController runtime;
        private ResourceEconomyController resourceEconomy;
        private ResidentPopulationController residents;
        private OakTreeLifecycleController oakTrees;
        private RoomLayoutEditorController layoutEditor;
        private WasteManagementController wasteManagement;
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly Dictionary<PigeonDemoAgent, AnimalDeathCause> pendingPigeonCauses = new();
        private readonly List<WildlifeVitality> vitalities = new();

        public event Action StateChanged;
        public event Action<WildlifeSpecies, AnimalDeathCause, int> AnimalDied;

        public AnimalMortalityModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            ResidentPopulationController residentController,
            OakTreeLifecycleController oakTreeController,
            RoomLayoutEditorController editorController,
            WasteManagementController wasteController,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<WildlifeVitality> otherAnimalVitalities = null)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            resourceEconomy = economyController ?? throw new ArgumentNullException(nameof(economyController));
            residents = residentController ?? throw new ArgumentNullException(nameof(residentController));
            oakTrees = oakTreeController ?? throw new ArgumentNullException(nameof(oakTreeController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            pigeons.AddRange((pigeonAgents ?? Array.Empty<PigeonDemoAgent>()).Where(item => item != null));
            vitalities.AddRange((otherAnimalVitalities ?? Array.Empty<WildlifeVitality>()).Where(item => item != null));
            Model = new AnimalMortalityModel();

            runtime.RestartRequested += HandleRestartRequested;
            foreach (var pigeon in pigeons)
            {
                pigeon.StateChanged += HandlePigeonStateChanged;
            }
            foreach (var vitality in vitalities)
            {
                vitality.Died += HandleVitalityDied;
            }
            resourceEconomy.BindRunSummaryProvider(PopulateResults);
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            foreach (var pigeon in pigeons)
            {
                if (pigeon != null)
                {
                    pigeon.StateChanged -= HandlePigeonStateChanged;
                }
            }
            foreach (var vitality in vitalities)
            {
                if (vitality != null)
                {
                    vitality.Died -= HandleVitalityDied;
                }
            }
        }

        public void RecordDeath(
            WildlifeSpecies species,
            AnimalDeathCause cause = AnimalDeathCause.Other)
        {
            if (Model == null || runtime == null || !runtime.HasActiveRun)
            {
                return;
            }

            var ended = Model.RecordDeath(species, cause);
            AnimalDied?.Invoke(species, cause, Model.TotalDeaths);
            StateChanged?.Invoke();
            if (ended && runtime.HasActiveRun)
            {
                var results = new RunResultsData
                {
                    endReason = RunEndReason.AnimalDeathLimit,
                    daysSurvived = Mathf.Max(1, runtime.Clock.DayNumber)
                };
                PopulateResults(results);
                runtime.EndRun(results);
            }
        }

        public void PreparePigeonDeath(
            PigeonDemoAgent pigeon,
            AnimalDeathCause cause)
        {
            if (pigeon != null)
            {
                pendingPigeonCauses[pigeon] = cause;
            }
        }

        public void ConfigureDeathLimit(int deathLimit)
        {
            Model?.SetDeathLimit(deathLimit);
            StateChanged?.Invoke();
        }

        public void RestoreSession(
            int pigeonDeaths,
            int squirrelDeaths,
            int hedgehogDeaths,
            int foxDeaths,
            int starvationDeaths,
            int trafficDeaths)
        {
            Model?.Restore(
                pigeonDeaths,
                squirrelDeaths,
                hedgehogDeaths,
                foxDeaths,
                starvationDeaths,
                trafficDeaths);
            StateChanged?.Invoke();
        }

        public void PopulateResults(RunResultsData results)
        {
            if (results == null)
            {
                return;
            }

            var economy = resourceEconomy.Model;
            var population = residents.Model;
            var trees = oakTrees.Model;
            results.cumulativeResourceIncome = Mathf.RoundToInt(economy?.CumulativeIncome ?? 0f);
            results.cumulativeResourceSpending = Mathf.RoundToInt(economy?.CumulativeSpending ?? 0f);
            results.finalResourceBalance = Mathf.FloorToInt(economy?.Balance ?? 0f);
            results.peakResourceBalance = Mathf.RoundToInt(economy?.PeakBalance ?? 0f);
            results.finalResidents = population?.ResidentCount ?? wasteManagement.ResidentCount;
            results.peakResidents = population?.PeakResidents ?? results.finalResidents;
            results.averageCommuteEfficiency = population != null && population.Residents.Count > 0
                ? population.Residents.Average(item => item.lastEfficiency)
                : 0f;
            results.arrivals = population?.CumulativeArrivals ?? 0;
            results.relocations = population?.CumulativeRelocations ?? 0;
            results.departures = population?.CumulativeDepartures ?? 0;
            results.roomMovements = layoutEditor.TotalRoomsMoved;
            results.treesFelled = trees?.TreesFelled ?? 0;
            results.treesPlanted = trees?.TreesPlanted ?? 0;
            results.treesMatured = trees?.TreesMatured ?? 0;
            results.pigeonDeaths = Model?.PigeonDeaths ?? 0;
            results.squirrelDeaths = Model?.SquirrelDeaths ?? 0;
            results.hedgehogDeaths = Model?.HedgehogDeaths ?? 0;
            results.foxDeaths = Model?.FoxDeaths ?? 0;
            results.starvationDeaths = Model?.StarvationDeaths ?? 0;
            results.trafficDeaths = Model?.TrafficDeaths ?? 0;
            results.configuredDeathLimit = Model?.DeathLimit ?? AnimalMortalityModel.DefaultDeathLimit;
        }

        private void HandlePigeonStateChanged(PigeonDemoAgent agent, PigeonDemoState state)
        {
            if (state == PigeonDemoState.Dead)
            {
                var cause = pendingPigeonCauses.TryGetValue(agent, out var prepared)
                    ? prepared
                    : AnimalDeathCause.Other;
                pendingPigeonCauses.Remove(agent);
                RecordDeath(WildlifeSpecies.Pigeon, cause);
            }
        }

        private void HandleVitalityDied(WildlifeVitality vitality, AnimalDeathCause cause)
        {
            RecordDeath(vitality.Species, cause);
        }

        private void HandleRestartRequested()
        {
            Model?.Reset();
            pendingPigeonCauses.Clear();
            StateChanged?.Invoke();
        }
    }
}
