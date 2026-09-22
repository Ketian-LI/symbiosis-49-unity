using System;
using System.IO;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class SessionPersistenceController : MonoBehaviour
    {
        private const float AutosaveIntervalSeconds = 15f;

        private GameRuntimeController runtime;
        private RoomLayoutEditorController layoutEditor;
        private WasteManagementController wasteManagement;
        private ResourceEconomyController resourceEconomy;
        private ResidentPopulationController residentPopulation;
        private OakTreeLifecycleController oakTrees;
        private PlayerFeedingController playerFeeding;
        private AnimalMortalityController animalMortality;
        private NaturalFoodController naturalFood;
        private AnimalNeedsController animalNeeds;
        private float autosaveTimer;
        private bool initialized;

        public string SavePath => Path.Combine(Application.persistentDataPath, "SYMBIOSIS_49", "session-v1.json");
        public string ProfilePath => Path.Combine(Application.persistentDataPath, "SYMBIOSIS_49", "profile-v1.json");
        public int BestSurvivalDays { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController editorController,
            WasteManagementController wasteController,
            ResourceEconomyController resourceController,
            ResidentPopulationController residentController,
            OakTreeLifecycleController oakTreeController,
            PlayerFeedingController feedingController,
            AnimalMortalityController mortalityController,
            NaturalFoodController naturalFoodController,
            AnimalNeedsController needsController)
        {
            runtime = runtimeController;
            layoutEditor = editorController;
            wasteManagement = wasteController;
            resourceEconomy = resourceController;
            residentPopulation = residentController;
            oakTrees = oakTreeController;
            playerFeeding = feedingController;
            animalMortality = mortalityController;
            naturalFood = naturalFoodController;
            animalNeeds = needsController;
            layoutEditor.LayoutConfirmed += SaveNow;
            runtime.SaveRequested += SaveNow;
            runtime.RunEnded += HandleRunEnded;
            initialized = true;

            if (Application.isPlaying)
            {
                LoadProfile();
                runtime.SetBestSurvivalDays(BestSurvivalDays);
                LoadExistingSession();
            }
        }

        public bool RecordBestSurvivalDays(int days)
        {
            var profile = new ProfileSaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                bestSurvivalDays = BestSurvivalDays
            };

            if (!profile.TryRecordSurvivalDays(days))
            {
                return false;
            }

            try
            {
                WriteJsonAtomically(ProfilePath, JsonUtility.ToJson(profile, true));
                BestSurvivalDays = profile.bestSurvivalDays;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SYMBIOSIS: 49] Profile save failed: {exception.Message}", this);
                return false;
            }
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying || runtime.LayoutEditing)
            {
                return;
            }

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveIntervalSeconds)
            {
                SaveNow();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= SaveNow;
            }
            if (runtime != null)
            {
                runtime.SaveRequested -= SaveNow;
                runtime.RunEnded -= HandleRunEnded;
            }
        }

        private void HandleRunEnded(RunResultsData results)
        {
            if (results == null)
            {
                return;
            }

            results.isNewRecord = RecordBestSurvivalDays(results.daysSurvived);
            results.bestSurvivalDays = BestSurvivalDays;
            runtime.SetBestSurvivalDays(BestSurvivalDays);
        }

        public void SaveNow()
        {
            if (!initialized || !Application.isPlaying || runtime.LayoutEditing)
            {
                return;
            }

            if (runtime.HasEndedRun)
            {
                DeleteCompletedSession();
                autosaveTimer = 0f;
                return;
            }

            if (!runtime.HasActiveRun)
            {
                return;
            }

            try
            {
                var save = new SessionSaveData
                {
                    savedAtUtc = DateTime.UtcNow.ToString("O"),
                    mode = runtime.Mode.ToString(),
                    elapsedSimulationSeconds = runtime.Clock.TotalSeconds,
                    speedMultiplier = runtime.SpeedMultiplier,
                    rooms = new System.Collections.Generic.List<RoomPlacementData>(layoutEditor.ExportLayout()),
                    residentCount = wasteManagement?.ResidentCount ?? WasteManagementController.StartingResidentCount,
                    operatingFoodShopCount = wasteManagement?.OperatingFoodShopCount ?? WasteManagementController.FoodShopCount,
                    supermarketOperating = wasteManagement?.SupermarketOperating ?? true,
                    resourceBalance = resourceEconomy?.Model?.Balance ?? ResourceEconomyModel.StartingBalance,
                    cumulativeResourceIncome = resourceEconomy?.Model?.CumulativeIncome ?? 0f,
                    cumulativeResourceSpending = resourceEconomy?.Model?.CumulativeSpending ?? 0f,
                    unstoredResourceSurplus = resourceEconomy?.Model?.UnstoredSurplus ?? 0f,
                    peakResourceBalance = resourceEconomy?.Model?.PeakBalance ?? ResourceEconomyModel.StartingBalance,
                    residents = residentPopulation?.Model?.ExportResidents() ?? new System.Collections.Generic.List<ResidentSaveData>(),
                    prospectiveResidenceId = residentPopulation?.Model?.ProspectiveResidenceId,
                    nextResidentNumber = residentPopulation?.Model?.NextResidentNumber ?? 5,
                    cumulativeResidentArrivals = residentPopulation?.Model?.CumulativeArrivals ?? 0,
                    cumulativeResidentRelocations = residentPopulation?.Model?.CumulativeRelocations ?? 0,
                    cumulativeResidentDepartures = residentPopulation?.Model?.CumulativeDepartures ?? 0,
                    peakResidentCount = residentPopulation?.Model?.PeakResidents ?? WasteManagementController.StartingResidentCount,
                    oakTrees = oakTrees?.Model?.ExportTrees() ?? new System.Collections.Generic.List<OakTreeSaveData>(),
                    treesFelled = oakTrees?.Model?.TreesFelled ?? 0,
                    treesPlanted = oakTrees?.Model?.TreesPlanted ?? 0,
                    treesMatured = oakTrees?.Model?.TreesMatured ?? 0,
                    roomMovements = layoutEditor.TotalRoomsMoved,
                    playerFoodSources = playerFeeding?.Model?.Export() ?? new System.Collections.Generic.List<PlayerFoodSourceSaveData>(),
                    squirrelDemoCachePortions = playerFeeding?.PrimarySquirrelCachePortions ?? 0,
                    squirrelCachePortions = playerFeeding?.SquirrelCachePortions ?? new System.Collections.Generic.List<int>(),
                    pigeonDeaths = animalMortality?.Model?.PigeonDeaths ?? 0,
                    squirrelDeaths = animalMortality?.Model?.SquirrelDeaths ?? 0,
                    hedgehogDeaths = animalMortality?.Model?.HedgehogDeaths ?? 0,
                    foxDeaths = animalMortality?.Model?.FoxDeaths ?? 0,
                    starvationDeaths = animalMortality?.Model?.StarvationDeaths ?? 0,
                    trafficDeaths = animalMortality?.Model?.TrafficDeaths ?? 0,
                    naturalFoodSources = naturalFood?.Model?.Export() ?? new System.Collections.Generic.List<NaturalFoodSaveData>(),
                    animalNeeds = animalNeeds?.Model?.Export() ?? new System.Collections.Generic.List<AnimalNeedSaveData>(),
                    wasteRooms = wasteManagement?.Model?.ExportWasteRooms() ?? new System.Collections.Generic.List<WasteRoomSaveData>(),
                    blockedWaste = wasteManagement?.Model?.ExportBlockedWaste() ?? new System.Collections.Generic.List<BlockedWasteSaveData>()
                };
                WriteJsonAtomically(SavePath, JsonUtility.ToJson(save, true));
                autosaveTimer = 0f;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SYMBIOSIS: 49] Session save failed: {exception.Message}", this);
            }
        }

        private void LoadExistingSession()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var save = JsonUtility.FromJson<SessionSaveData>(File.ReadAllText(SavePath));
                if (save == null || save.schemaVersion != 1 || save.rooms == null || !layoutEditor.RestoreLayout(save.rooms))
                {
                    Debug.LogWarning("[SYMBIOSIS: 49] Existing session was ignored because its layout is incomplete or incompatible.", this);
                    return;
                }

                var mode = Enum.TryParse<GameMode>(save.mode, out var parsedMode)
                    ? parsedMode
                    : GameMode.Sandbox;
                runtime.RestoreSession(save.elapsedSimulationSeconds, save.speedMultiplier, mode);
                wasteManagement?.RestoreSession(
                    save.wasteRooms,
                    save.blockedWaste,
                    save.residentCount,
                    save.operatingFoodShopCount,
                    save.supermarketOperating);
                resourceEconomy?.RestoreSession(
                    save.resourceBalance,
                    save.cumulativeResourceIncome,
                    save.cumulativeResourceSpending,
                    save.unstoredResourceSurplus,
                    save.peakResourceBalance);
                residentPopulation?.RestoreSession(
                    save.residents,
                    save.prospectiveResidenceId,
                    save.nextResidentNumber,
                    save.cumulativeResidentArrivals,
                    save.cumulativeResidentRelocations,
                    save.cumulativeResidentDepartures,
                    save.peakResidentCount);
                oakTrees?.RestoreSession(
                    save.oakTrees,
                    save.treesFelled,
                    save.treesPlanted,
                    save.treesMatured);
                playerFeeding?.RestoreSession(
                    save.playerFoodSources,
                    save.squirrelDemoCachePortions,
                    save.squirrelCachePortions);
                layoutEditor.RestoreMovementCount(save.roomMovements);
                animalMortality?.RestoreSession(
                    save.pigeonDeaths,
                    save.squirrelDeaths,
                    save.hedgehogDeaths,
                    save.foxDeaths,
                    save.starvationDeaths,
                    save.trafficDeaths);
                naturalFood?.RestoreSession(save.naturalFoodSources);
                animalNeeds?.RestoreSession(save.animalNeeds);
                Debug.Log($"[SYMBIOSIS: 49] Continued saved session from {save.savedAtUtc}.", this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SYMBIOSIS: 49] Existing session could not be loaded: {exception.Message}", this);
            }
        }

        private void LoadProfile()
        {
            if (!File.Exists(ProfilePath))
            {
                BestSurvivalDays = 0;
                return;
            }

            try
            {
                var profile = JsonUtility.FromJson<ProfileSaveData>(File.ReadAllText(ProfilePath));
                BestSurvivalDays = profile != null && profile.schemaVersion == 1
                    ? Math.Max(0, profile.bestSurvivalDays)
                    : 0;
            }
            catch (Exception exception)
            {
                BestSurvivalDays = 0;
                Debug.LogWarning($"[SYMBIOSIS: 49] Profile save could not be loaded: {exception.Message}", this);
            }
        }

        private void DeleteCompletedSession()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                var temporaryPath = SavePath + ".tmp";
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SYMBIOSIS: 49] Completed session could not be cleared: {exception.Message}", this);
            }
        }

        private static void WriteJsonAtomically(string path, string json)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }
    }
}
