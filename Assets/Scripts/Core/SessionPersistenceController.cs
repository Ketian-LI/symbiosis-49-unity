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
        private WorkerPasserbyFeedingController workerFeeding;
        private HedgehogForagingController hedgehogForaging;
        private DailyOutcomeController dailyOutcome;
        private EndlessBalanceController endlessBalance;
        private float autosaveTimer;
        private bool initialized;
        private string activeRunId;

        // An isolated directory lets PlayMode persistence tests exercise real
        // scene reloads without touching the player's active session/profile.
        public static string SaveDirectoryOverrideForTests { get; set; }
        private static string SaveDirectory => string.IsNullOrEmpty(SaveDirectoryOverrideForTests)
            ? Path.Combine(Application.persistentDataPath, "SYMBIOSIS_49")
            : SaveDirectoryOverrideForTests;

        // Keep the incompatible 35-room session-v1 file untouched. A 49-room
        // session starts in a separate slot rather than silently overwriting it.
        public string SavePath => Path.Combine(SaveDirectory, "session-v2.json");
        public string RunIdPath => Path.Combine(SaveDirectory, "session-v2.run-id");
        public string ProfilePath => Path.Combine(SaveDirectory, "profile-v1.json");
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
            AnimalNeedsController needsController,
            WorkerPasserbyFeedingController workerFeedingController,
            HedgehogForagingController hedgehogForagingController,
            DailyOutcomeController dailyOutcomeController,
            EndlessBalanceController endlessBalanceController)
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
            workerFeeding = workerFeedingController;
            hedgehogForaging = hedgehogForagingController;
            dailyOutcome = dailyOutcomeController;
            endlessBalance = endlessBalanceController;
            layoutEditor.LayoutConfirmed += SaveNow;
            if (playerFeeding != null) playerFeeding.FoodPlaced += HandlePlayerFoodPlaced;
            runtime.SaveRequested += SaveNow;
            runtime.RestartStarting += HandleRestartStarting;
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
            if (playerFeeding != null)
            {
                playerFeeding.FoodPlaced -= HandlePlayerFoodPlaced;
            }
            if (runtime != null)
            {
                runtime.SaveRequested -= SaveNow;
                runtime.RestartStarting -= HandleRestartStarting;
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

        private void HandlePlayerFoodPlaced(PlayerFoodSourceState _)
        {
            SaveNow();
        }

        private void HandleRestartStarting()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }

            try
            {
                // Commit the new run identity before resetting any in-memory
                // state. An older writer may later recreate session-v2.json;
                // the marker makes that file ineligible for restore.
                var nextRunId = Guid.NewGuid().ToString("N");
                WriteJsonAtomically(RunIdPath, nextRunId);
                activeRunId = nextRunId;
                autosaveTimer = 0f;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SYMBIOSIS: 49] Restart was cancelled because its new save identity could not be written: {exception.Message}", this);
                throw;
            }

            try
            {
                DeleteSessionFiles();
            }
            catch (Exception exception)
            {
                // The committed marker still prevents the old file from
                // loading. Continue the reset and let SaveNow replace it.
                Debug.LogWarning($"[SYMBIOSIS: 49] Old session file could not be removed, but it has been invalidated: {exception.Message}", this);
            }
        }

        public void SaveNow()
        {
            if (!initialized || !Application.isPlaying || runtime.LayoutEditing || runtime.RestartInProgress)
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
                    runId = activeRunId,
                    savedAtUtc = DateTime.UtcNow.ToString("O"),
                    mode = runtime.Mode.ToString(),
                    sandboxDifficulty = runtime.SandboxDifficulty.ToString(),
                    sandboxSettings = runtime.Mode == GameMode.Sandbox
                        ? runtime.SandboxSettings.Normalized() : null,
                    elapsedSimulationSeconds = runtime.Clock.TotalSeconds,
                    speedMultiplier = runtime.SelectedSpeedMultiplier,
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
                    lastRoomMovementDay = layoutEditor.LastConfirmedMovementDay,
                    lastLayoutPlanningDay = layoutEditor.LastConfirmedPlanningDay,
                    lastLayoutHoldDay = layoutEditor.LastConfirmedHoldDay,
                    shrubShelters = hedgehogForaging?.Shelter.Export() ?? new System.Collections.Generic.List<ShrubShelterSaveData>(),
                    dailyOutcome = dailyOutcome?.Model.Export(),
                    playerFoodSources = playerFeeding?.Model?.Export() ?? new System.Collections.Generic.List<PlayerFoodSourceSaveData>(),
                    lastManualFeedingDay = playerFeeding?.LastManualFeedingDay ?? 0,
                    squirrelDemoCachePortions = playerFeeding?.PrimarySquirrelCachePortions ?? 0,
                    squirrelCachePortions = playerFeeding?.SquirrelCachePortions ?? new System.Collections.Generic.List<int>(),
                    pigeonDeaths = animalMortality?.Model?.PigeonDeaths ?? 0,
                    squirrelDeaths = animalMortality?.Model?.SquirrelDeaths ?? 0,
                    hedgehogDeaths = animalMortality?.Model?.HedgehogDeaths ?? 0,
                    foxDeaths = animalMortality?.Model?.FoxDeaths ?? 0,
                    starvationDeaths = animalMortality?.Model?.StarvationDeaths ?? 0,
                    trafficDeaths = animalMortality?.Model?.TrafficDeaths ?? 0,
                    animalDeathBreakdown = animalMortality?.Model?.ExportBreakdown() ?? new System.Collections.Generic.List<AnimalDeathBreakdownData>(),
                    naturalFoodSources = naturalFood?.Model?.Export() ?? new System.Collections.Generic.List<NaturalFoodSaveData>(),
                    animalNeeds = animalNeeds?.Model?.Export() ?? new System.Collections.Generic.List<AnimalNeedSaveData>(),
                    workerFeedDayNumber = workerFeeding?.Quota.DayNumber ?? 0,
                    workerFedResidentIds = workerFeeding != null
                        ? new System.Collections.Generic.List<string>(workerFeeding.Quota.FedResidents)
                        : new System.Collections.Generic.List<string>(),
                    wasteRooms = wasteManagement?.Model?.ExportWasteRooms() ?? new System.Collections.Generic.List<WasteRoomSaveData>(),
                    blockedWaste = wasteManagement?.Model?.ExportBlockedWaste() ?? new System.Collections.Generic.List<BlockedWasteSaveData>(),
                    endlessBalance = endlessBalance?.Model?.Export(),
                    endlessDeadAnimalIds = endlessBalance?.ExportDeadIds() ?? new System.Collections.Generic.List<string>()
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
            if (File.Exists(RunIdPath))
            {
                try
                {
                    activeRunId = File.ReadAllText(RunIdPath).Trim();
                    if (string.IsNullOrEmpty(activeRunId))
                    {
                        Debug.LogWarning("[SYMBIOSIS: 49] Empty run identity marker; saved session was not resumed.", this);
                        return;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[SYMBIOSIS: 49] Run identity could not be read, so the saved session was not resumed: {exception.Message}", this);
                    return;
                }
            }

            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var save = JsonUtility.FromJson<SessionSaveData>(json);
                if (activeRunId != null && !string.Equals(save?.runId, activeRunId, StringComparison.Ordinal))
                {
                    Debug.LogWarning("[SYMBIOSIS: 49] A stale session from before the latest restart was ignored.", this);
                    return;
                }
                if (save == null || save.schemaVersion != 2 || save.rooms == null || !layoutEditor.RestoreLayout(save.rooms))
                {
                    Debug.LogWarning("[SYMBIOSIS: 49] Existing session was ignored because its layout is incomplete or incompatible.", this);
                    return;
                }

                var mode = Enum.TryParse<GameMode>(save.mode, out var parsedMode)
                    ? parsedMode
                    : GameMode.Sandbox;
                var difficulty = Enum.TryParse<EndlessDifficulty>(save.sandboxDifficulty,
                    out var parsedDifficulty)
                    ? EndlessDifficultyRules.Normalize(parsedDifficulty)
                    : EndlessDifficulty.Standard;
                runtime.RestoreSession(save.elapsedSimulationSeconds, save.speedMultiplier,
                    mode, difficulty,
                    json.Contains("\"sandboxSettings\"") ? save.sandboxSettings : null);
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
                    save.squirrelCachePortions,
                    save.lastManualFeedingDay);
                layoutEditor.RestoreMovementCount(save.roomMovements);
                layoutEditor.RestoreLastMovementDay(save.lastRoomMovementDay);
                layoutEditor.RestoreLastPlanningDay(save.lastLayoutPlanningDay);
                layoutEditor.RestoreLastHoldDay(save.lastLayoutHoldDay);
                hedgehogForaging?.RestoreSession(save.shrubShelters);
                animalMortality?.RestoreSession(
                    save.pigeonDeaths,
                    save.squirrelDeaths,
                    save.hedgehogDeaths,
                    save.foxDeaths,
                    save.starvationDeaths,
                    save.trafficDeaths,
                    save.animalDeathBreakdown);
                naturalFood?.RestoreSession(save.naturalFoodSources);
                animalNeeds?.RestoreSession(save.animalNeeds);
                endlessBalance?.RestoreSession(save.endlessBalance, save.endlessDeadAnimalIds);
                workerFeeding?.Quota.Restore(save.workerFeedDayNumber, save.workerFedResidentIds);
                dailyOutcome?.RestoreSession(save.dailyOutcome);
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
                DeleteSessionFiles();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SYMBIOSIS: 49] Completed session could not be cleared: {exception.Message}", this);
            }
        }

        private void DeleteSessionFiles()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            var temporaryPath = SavePath + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
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
