using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class NaturalFoodController : MonoBehaviour
    {
        private readonly Dictionary<string, Transform> roomRoots = new();
        private readonly Dictionary<string, GameObject> visuals = new();
        private GameRuntimeController runtime;
        private WasteManagementController wasteManagement;
        private OakTreeLifecycleController oakTrees;
        private ResourceEconomyController resourceEconomy;
        private Material material;
        private HideFlags generatedHideFlags;
        private double lastProcessedTime;
        private bool initialized;

        public event Action StateChanged;
        public NaturalFoodModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            WasteManagementController wasteController,
            OakTreeLifecycleController oakTreeController,
            ResourceEconomyController economyController,
            IEnumerable<RoomView> rooms,
            Material sharedMaterial,
            HideFlags generatedHideFlags)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            oakTrees = oakTreeController ?? throw new ArgumentNullException(nameof(oakTreeController));
            resourceEconomy = economyController ?? throw new ArgumentNullException(nameof(economyController));
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            foreach (var room in rooms ?? Array.Empty<RoomView>())
            {
                roomRoots[room.Spec.Id] = room.VisualRoot;
            }

            Model = new NaturalFoodModel(RoomLayoutData.All);
            Model.ProduceDawn(1, oakTrees.Model.StageOf);
            lastProcessedTime = runtime.Clock.TotalSeconds;
            runtime.RestartRequested += HandleRestartRequested;
            resourceEconomy.DaySettled += HandleDaySettled;
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
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            if (resourceEconomy != null)
            {
                resourceEconomy.DaySettled -= HandleDaySettled;
            }
        }

        public void ProcessUntil(double totalSeconds)
        {
            if (totalSeconds < lastProcessedTime)
            {
                lastProcessedTime = Math.Max(0d, totalSeconds);
                return;
            }

            foreach (var scheduledEvent in NaturalFoodSchedule.EventsBetween(lastProcessedTime, totalSeconds))
            {
                switch (scheduledEvent.Kind)
                {
                    case NaturalFoodScheduleEventKind.DuskProduction:
                        Model.ProduceDusk(scheduledEvent.DayNumber, wasteManagement.OperatingFoodShopCount);
                        break;
                    case NaturalFoodScheduleEventKind.NightProduction:
                        Model.ProduceNight(scheduledEvent.DayNumber, wasteManagement.Model.WasteRooms);
                        break;
                }
                SyncVisuals();
                StateChanged?.Invoke();
            }
            lastProcessedTime = Math.Max(0d, totalSeconds);
        }

        public bool TryConsume(string roomId, NaturalFoodKind kind)
        {
            if (!Model.TryConsume(roomId, kind))
            {
                return false;
            }
            SyncVisuals();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryConsumeAny(NaturalFoodKind kind)
        {
            string roomId = null;
            foreach (var source in Model.Sources.Values)
            {
                if (source.kind == kind)
                {
                    roomId = source.roomId;
                    break;
                }
            }
            return !string.IsNullOrEmpty(roomId) && TryConsume(roomId, kind);
        }

        public bool TryGetWorldPosition(string roomId, NaturalFoodKind kind, out Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(roomId) || Model.PortionsIn(roomId, kind) <= 0 ||
                !roomRoots.TryGetValue(roomId, out var roomRoot))
            {
                worldPosition = default;
                return false;
            }
            worldPosition = roomRoot.TransformPoint(LocalPositionFor(kind));
            return true;
        }

        public void RestoreSession(IEnumerable<NaturalFoodSaveData> savedSources)
        {
            Model.Restore(savedSources);
            lastProcessedTime = runtime.Clock.TotalSeconds;
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            Model.Reset();
            Model.ProduceDawn(1, oakTrees.Model.StageOf);
            lastProcessedTime = 0d;
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            Model.ProduceDawn(settlement.DayNumber + 1, oakTrees.Model.StageOf);
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void SyncVisuals()
        {
            foreach (var visual in visuals.Values)
            {
                if (visual != null)
                {
                    Destroy(visual);
                }
            }
            visuals.Clear();

            foreach (var source in Model.Sources.Values)
            {
                if (!roomRoots.TryGetValue(source.roomId, out var roomRoot))
                {
                    continue;
                }
                var root = new GameObject($"Natural Food {source.kind}")
                {
                    hideFlags = generatedHideFlags
                };
                root.transform.SetParent(roomRoot, false);
                root.transform.localPosition = LocalPositionFor(source.kind);
                root.AddComponent<NaturalFoodVisual>().Initialize(
                    source.kind,
                    source.portions,
                    material,
                    generatedHideFlags);
                visuals[$"{source.roomId}:{source.kind}"] = root;
            }
        }

        private static Vector3 LocalPositionFor(NaturalFoodKind kind)
        {
            return kind switch
            {
                NaturalFoodKind.Seed => new Vector3(0.42f, 0f, -0.34f),
                NaturalFoodKind.Nut => new Vector3(-0.46f, 0f, 0.34f),
                NaturalFoodKind.Insect => new Vector3(0.38f, 0f, 0.38f),
                _ => new Vector3(-0.38f, 0f, -0.38f)
            };
        }
    }
}
