using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class WasteManagementController : MonoBehaviour
    {
        public const int StartingResidentCount = 4;
        public const int MaximumResidentCount = 8;
        public const int FoodShopCount = 2;

        private readonly Dictionary<string, WasteRoomLoadVisual> visuals = new();
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layoutEditor;
        private float cellSize;
        private double lastProcessedTime;
        private bool initialized;

        public event Action<int> DailyWasteProduced;
        public event Action<int> CollectionWarningRaised;
        public event Action<int> MunicipalCollectionCompleted;
        public event Action<string, int> EmergencyCollectionCompleted;
        public event Action StateChanged;

        public WasteManagementModel Model { get; private set; }
        public int ResidentCount { get; private set; } = StartingResidentCount;
        public int OperatingFoodShopCount { get; private set; } = FoodShopCount;
        public bool SupermarketOperating { get; private set; } = true;
        public int HumanFunctionPenalty => Model?.HumanFunctionPenalty ?? 0;

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController editorController,
            IReadOnlyDictionary<string, WasteRoomLoadVisual> roomVisuals,
            float gridCellSize)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            cellSize = gridCellSize;
            visuals.Clear();
            if (roomVisuals != null)
            {
                foreach (var pair in roomVisuals)
                {
                    if (pair.Value != null)
                    {
                        visuals[pair.Key] = pair.Value;
                    }
                }
            }

            Model = new WasteManagementModel(BuildNavigation(), RoomLayoutData.All);
            Model.Changed += HandleModelChanged;
            layoutEditor.LayoutConfirmed += HandleLayoutConfirmed;
            runtime.RestartRequested += HandleRestartRequested;
            lastProcessedTime = runtime.Clock.TotalSeconds;
            initialized = true;
            SyncVisuals();
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
            if (Model != null)
            {
                Model.Changed -= HandleModelChanged;
            }

            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= HandleLayoutConfirmed;
            }

            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
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

            foreach (var scheduledEvent in WasteCollectionSchedule.EventsBetween(
                         lastProcessedTime,
                         totalSeconds))
            {
                switch (scheduledEvent.Kind)
                {
                    case WasteScheduleEventKind.DailyProduction:
                        Model.ProduceDailyWaste(
                            ResidentCount,
                            OperatingFoodShopCount,
                            SupermarketOperating);
                        DailyWasteProduced?.Invoke(scheduledEvent.DayNumber);
                        break;
                    case WasteScheduleEventKind.CollectionWarning:
                        CollectionWarningRaised?.Invoke(scheduledEvent.DayNumber);
                        break;
                    case WasteScheduleEventKind.MunicipalCollection:
                        Model.MunicipalCollectAll();
                        MunicipalCollectionCompleted?.Invoke(scheduledEvent.DayNumber);
                        break;
                }
            }

            lastProcessedTime = Math.Max(0d, totalSeconds);
        }

        public void SetResidentCount(int count)
        {
            ResidentCount = Mathf.Clamp(count, 0, MaximumResidentCount);
            StateChanged?.Invoke();
        }

        public void SetOperatingFoodShopCount(int count)
        {
            OperatingFoodShopCount = Mathf.Clamp(count, 0, FoodShopCount);
            StateChanged?.Invoke();
        }

        public void SetSupermarketOperating(bool operating)
        {
            SupermarketOperating = operating;
            StateChanged?.Invoke();
        }

        public bool TryEmergencyCollect(
            string wasteRoomId,
            int availableResourcePoints,
            out int resourceCost)
        {
            resourceCost = 0;
            var collected = Model != null &&
                            Model.TryEmergencyCollect(
                                wasteRoomId,
                                availableResourcePoints,
                                out resourceCost);
            if (collected)
            {
                EmergencyCollectionCompleted?.Invoke(wasteRoomId, resourceCost);
            }

            return collected;
        }

        public void RestoreSession(
            IEnumerable<WasteRoomSaveData> savedWasteRooms,
            IEnumerable<BlockedWasteSaveData> savedBlockedWaste,
            int residentCount,
            int operatingFoodShopCount,
            bool supermarketIsOperating)
        {
            if (Model == null)
            {
                return;
            }

            ResidentCount = residentCount <= 0
                ? StartingResidentCount
                : Mathf.Clamp(residentCount, 0, MaximumResidentCount);
            OperatingFoodShopCount = operatingFoodShopCount < 0
                ? FoodShopCount
                : Mathf.Clamp(operatingFoodShopCount, 0, FoodShopCount);
            SupermarketOperating = supermarketIsOperating;
            Model.Restore(savedWasteRooms, savedBlockedWaste);
            lastProcessedTime = runtime.Clock.TotalSeconds;
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleLayoutConfirmed()
        {
            Model?.UpdateNavigation(BuildNavigation());
        }

        private void HandleRestartRequested()
        {
            ResidentCount = StartingResidentCount;
            OperatingFoodShopCount = FoodShopCount;
            SupermarketOperating = true;
            lastProcessedTime = 0d;
            Model?.Reset();
        }

        private void HandleModelChanged()
        {
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void SyncVisuals()
        {
            if (Model == null)
            {
                return;
            }

            foreach (var pair in Model.WasteRooms)
            {
                if (visuals.TryGetValue(pair.Key, out var visual) && visual?.Model != null)
                {
                    visual.Model.Restore(pair.Value.Units);
                }
            }
        }

        private RoomNavigationMap BuildNavigation()
        {
            return new RoomNavigationMap(
                layoutEditor.ExportLayout(),
                RoomLayoutData.All,
                cellSize);
        }
    }
}
