using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
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
        private RoomLayoutEditorController layoutEditor;
        private Transform mapRoot;
        private float cellSize;
        private RoomNavigationMap animalNavigation;
        private RoomNavigationMap hungryAnimalNavigation;
        private int animalNavigationDay;
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
            RoomLayoutEditorController editorController,
            Transform boardRoot,
            float gridCellSize,
            IEnumerable<RoomView> rooms,
            Material sharedMaterial,
            HideFlags generatedHideFlags)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            oakTrees = oakTreeController ?? throw new ArgumentNullException(nameof(oakTreeController));
            resourceEconomy = economyController ?? throw new ArgumentNullException(nameof(economyController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            mapRoot = boardRoot ?? throw new ArgumentNullException(nameof(boardRoot));
            cellSize = gridCellSize;
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            foreach (var room in rooms ?? Array.Empty<RoomView>())
            {
                roomRoots[room.Spec.Id] = room.VisualRoot;
            }

            Model = new NaturalFoodModel(RoomLayoutData.All);
            RebuildNavigation();
            Model.ProduceDawn(1, oakTrees.Model.StageOf, animalNavigation);
            lastProcessedTime = runtime.Clock.TotalSeconds;
            runtime.RestartRequested += HandleRestartRequested;
            resourceEconomy.DaySettled += HandleDaySettled;
            layoutEditor.LayoutConfirmed += RebuildNavigation;
            layoutEditor.LayoutRestored += RebuildNavigation;
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
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= RebuildNavigation;
                layoutEditor.LayoutRestored -= RebuildNavigation;
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
                        RefreshAnimalNavigationDay();
                        Model.ProduceNight(scheduledEvent.DayNumber, wasteManagement.Model.WasteRooms,
                            animalNavigation);
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

        public bool TryConsumeReachable(
            Vector3 animalPosition,
            WildlifeSpecies species,
            NaturalFoodKind kind,
            int hungerDays = 0)
        {
            RefreshAnimalNavigationDay();
            var local = mapRoot.InverseTransformPoint(animalPosition);
            return animalNavigation.TryFindRoomContaining(new Vector2(local.x, local.z), out var startRoom) &&
                   TryConsumeReachable(startRoom, species, kind, hungerDays);
        }

        public bool TryConsumeReachable(
            string homeRoomId,
            WildlifeSpecies species,
            NaturalFoodKind kind,
            int hungerDays = 0)
        {
            RefreshAnimalNavigationDay();
            var routeMap = RoomNavigationMap.ForWildlifeHunger(
                animalNavigation, hungryAnimalNavigation, hungerDays);
            return HabitatFoodNetworkModel.TryChooseReachableSource(
                       routeMap, homeRoomId, species, kind, Model.Sources.Values, out var sourceRoom) &&
                   TryConsume(sourceRoom, kind);
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
            RebuildNavigation();
            Model.ProduceDawn(1, oakTrees.Model.StageOf, animalNavigation);
            lastProcessedTime = 0d;
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            var nextDay = runtime.Mode == GameMode.Sandbox ? settlement.DayNumber + 1 : 1;
            var nextAnimalNavigation = new RoomNavigationMap(
                layoutEditor.ExportLayout(), RoomLayoutData.All, cellSize, true, nextDay);
            Model.ProduceDawn(nextDay, oakTrees.Model.StageOf, nextAnimalNavigation);
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void RebuildNavigation()
        {
            animalNavigationDay = runtime?.Clock.DayNumber ?? 1;
            animalNavigation = new RoomNavigationMap(
                layoutEditor.ExportLayout(), RoomLayoutData.All, cellSize,
                true, animalNavigationDay);
            hungryAnimalNavigation = new RoomNavigationMap(
                layoutEditor.ExportLayout(), RoomLayoutData.All, cellSize,
                animalPassagesOnly: true, animalDayNumber: animalNavigationDay,
                hungryWildlifeMayUsePedestrianDoors: true);
            StateChanged?.Invoke();
        }

        private void RefreshAnimalNavigationDay()
        {
            if (runtime != null && animalNavigationDay != runtime.Clock.DayNumber)
            {
                RebuildNavigation();
            }
        }

        private void SyncVisuals()
        {
            foreach (var visual in visuals.Values)
            {
                if (visual != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(visual);
                    }
                    else
                    {
                        DestroyImmediate(visual);
                    }
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
                var sourcePosition = LocalPositionFor(source.kind);
                root.transform.localPosition = sourcePosition;
                root.AddComponent<NaturalFoodVisual>().Initialize(
                    source.kind,
                    source.portions,
                    BadgePositionFor(source.kind) - sourcePosition,
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

        private static Vector3 BadgePositionFor(NaturalFoodKind kind)
        {
            // Different food kinds in one room occupy distinct corners. The
            // badges stay legible above furniture without covering walkways.
            return kind switch
            {
                NaturalFoodKind.Seed => new Vector3(0.98f, 1.67f, 1.02f),
                NaturalFoodKind.Nut => new Vector3(-0.98f, 1.67f, 1.02f),
                NaturalFoodKind.Insect => new Vector3(0.98f, 1.67f, -1.02f),
                _ => new Vector3(-0.98f, 1.67f, -1.02f)
            };
        }
    }
}
