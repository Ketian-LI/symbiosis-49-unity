using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class PlayerFeedingController : MonoBehaviour
    {
        private readonly Dictionary<string, PlayerFoodSourceVisual> visuals = new();
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly PlayerFeedingInteractionModel interaction = new();
        private GameRuntimeController runtime;
        private ResourceEconomyController economy;
        private RoomLayoutEditorController layoutEditor;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private Camera worldCamera;
        private Transform mapRoot;
        private Transform foodRoot;
        private Material material;
        private HideFlags generatedHideFlags;

        public event Action StateChanged;
        public event Action<PlayerFoodSourceState> FoodPlaced;
        public event Action<string> FoodExpired;
        public event Action<string> InvalidPlacementAttempted;
        public event Action<IWildlifeLayoutAgent> AnimalAte;
        public event Action FeedingModeChanged;

        public PlayerFoodSourceModel Model { get; private set; }
        public bool FeedingModeActive => interaction.IsActive;
        public bool CanActivateFeedingMode =>
            InteractionAllowed &&
            Model != null &&
            Model.PlayerPlacedCount < PlayerFoodSourceModel.MaximumSources &&
            economy != null &&
            economy.Balance >= ResourceEconomyModel.FeedActionCost;
        public int PrimarySquirrelCachePortions => squirrels.Count > 0
            ? squirrels[0].CachePortions
            : 0;
        public List<int> SquirrelCachePortions => squirrels
            .Select(squirrel => squirrel.CachePortions)
            .ToList();

        public void Initialize(
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            RoomLayoutEditorController editorController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Camera camera,
            Transform boardRoot,
            Transform sourceContainer,
            Material sharedMaterial,
            HideFlags generatedHideFlags,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents)
        {
            runtime = runtimeController;
            economy = economyController;
            layoutEditor = editorController;
            navigation = navigationCoordinator;
            traffic = trafficController;
            worldCamera = camera;
            mapRoot = boardRoot;
            foodRoot = sourceContainer;
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            pigeons.AddRange((pigeonAgents ?? Array.Empty<PigeonDemoAgent>()).Where(agent => agent != null));
            squirrels.AddRange((squirrelAgents ?? Array.Empty<SquirrelDemoAgent>()).Where(agent => agent != null));
            Model = new PlayerFoodSourceModel();
            runtime.RestartRequested += HandleRestartRequested;
            runtime.StateChanged += HandleRuntimeStateChanged;
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
                runtime.StateChanged -= HandleRuntimeStateChanged;
            }
        }

        private void Update()
        {
            if (runtime == null || Model == null || !Application.isPlaying)
            {
                return;
            }

            foreach (var expiredId in Model.Advance(Time.deltaTime))
            {
                RemoveVisual(expiredId);
                FoodExpired?.Invoke(expiredId);
            }

            if (FeedingModeActive && Input.GetMouseButtonDown(1))
            {
                CancelFeedingMode();
                return;
            }

            if (!FeedingModeActive || !InteractionAllowed ||
                !Input.GetMouseButtonDown(0) ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryHandleFeedingClick(Input.mousePosition);
        }

        public bool ToggleFeedingMode()
        {
            var wasActive = FeedingModeActive;
            var active = interaction.Toggle(CanActivateFeedingMode);
            if (wasActive != active)
            {
                FeedingModeChanged?.Invoke();
            }
            return active;
        }

        public bool ActivateFeedingMode()
        {
            if (!interaction.TryActivate(CanActivateFeedingMode))
            {
                return false;
            }

            FeedingModeChanged?.Invoke();
            return true;
        }

        public void CancelFeedingMode()
        {
            if (interaction.Cancel())
            {
                FeedingModeChanged?.Invoke();
            }
        }

        public bool TryPlaceFood(string roomId, Vector3 worldPosition)
        {
            if (Model.PlayerPlacedCount >= PlayerFoodSourceModel.MaximumSources ||
                !economy.TrySpend(ResourceEconomyModel.FeedActionCost))
            {
                ShowInvalidMarker(worldPosition);
                InvalidPlacementAttempted?.Invoke(roomId);
                return false;
            }

            if (!Model.TryCreate(roomId, worldPosition, out var source))
            {
                return false;
            }

            CreateVisual(source);
            DispatchAnimals(source);
            FoodPlaced?.Invoke(source);
            StateChanged?.Invoke();
            return true;
        }

        public bool AddExposedCacheFood(Vector3 worldPosition, int portions)
        {
            var roomId = FindRoomId(worldPosition);
            if (!Model.TryCreateExposed(roomId, worldPosition, portions, out var source))
            {
                return false;
            }
            CreateVisual(source);
            DispatchAnimals(source);
            StateChanged?.Invoke();
            return true;
        }

        public void RestoreSession(
            IEnumerable<PlayerFoodSourceSaveData> savedSources,
            int squirrelCachePortions = 0,
            IEnumerable<int> allSquirrelCachePortions = null)
        {
            ClearVisuals();
            Model.Restore(savedSources);
            foreach (var source in Model.Sources.Values)
            {
                CreateVisual(source);
            }
            var restoredCaches = (allSquirrelCachePortions ?? Array.Empty<int>()).ToArray();
            for (var index = 0; index < squirrels.Count; index++)
            {
                squirrels[index].RestoreCache(index < restoredCaches.Length
                    ? restoredCaches[index]
                    : index == 0 ? squirrelCachePortions : 0);
            }
            StateChanged?.Invoke();
        }

        private void TryHandleFeedingClick(Vector3 screenPosition)
        {
            var ray = worldCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 200f))
            {
                return;
            }

            if (hit.collider.GetComponentInParent<PigeonDemoAgent>() != null ||
                hit.collider.GetComponentInParent<SquirrelDemoAgent>() != null ||
                hit.collider.GetComponentInParent<HedgehogDemoAgent>() != null)
            {
                return;
            }
            if (hit.collider.GetComponentInParent<PlayerFoodSourceVisual>() != null)
            {
                return;
            }

            var roomView = hit.collider.GetComponentInParent<RoomView>();
            if (roomView == null || hit.collider.gameObject.name != "Clickable Floor")
            {
                ShowInvalidMarker(hit.point);
                InvalidPlacementAttempted?.Invoke(string.Empty);
                return;
            }

            if (TryPlaceFood(
                roomView.Spec.Id,
                hit.point + Vector3.up * 0.16f))
            {
                interaction.CompletePlacement();
                FeedingModeChanged?.Invoke();
            }
        }

        private bool InteractionAllowed =>
            runtime != null &&
            runtime.HasActiveRun &&
            (!runtime.IsPaused || runtime.OnboardingInteractionAllowed) &&
            layoutEditor != null &&
            !layoutEditor.IsEditing;

        private void HandleRuntimeStateChanged()
        {
            if (FeedingModeActive && !InteractionAllowed)
            {
                CancelFeedingMode();
            }
        }

        private void DispatchAnimals(PlayerFoodSourceState source)
        {
            var availablePigeons = pigeons
                .Where(agent => agent.IsAlive && !agent.IsRespondingToFood)
                .OrderBy(agent => (agent.transform.position - source.worldPosition).sqrMagnitude)
                .Take(5)
                .ToList();
            for (var index = 0; index < availablePigeons.Count; index++)
            {
                var pigeon = availablePigeons[index];
                var route = BuildWaypoints(pigeon.transform.position, source.worldPosition, source.roomId);
                var delay = Mathf.Lerp(0.2f, 0.8f, availablePigeons.Count <= 1
                    ? 0f
                    : index / (float)(availablePigeons.Count - 1));
                pigeon.BeginFoodMission(
                    route,
                    delay,
                    () => TryClaimAndReportMeal(source.id, pigeon));
            }

            foreach (var squirrel in squirrels
                         .Where(agent => !agent.IsRespondingToFood && agent.CanStoreFood)
                         .OrderBy(agent => (agent.transform.position - source.worldPosition).sqrMagnitude))
            {
                if (!TryBuildGroundWaypoints(
                    squirrel.transform.position,
                    source.worldPosition,
                    source.roomId,
                    out var routeToFood,
                    out var foodHazard,
                    out var foodFatal) ||
                    !TryBuildGroundWaypoints(
                    source.worldPosition,
                    squirrel.CachePosition,
                    FindRoomId(squirrel.CachePosition),
                    out var routeToCache,
                    out var cacheHazard,
                    out var cacheFatal))
                {
                    continue;
                }
                if (squirrel.BeginFoodMission(
                        routeToFood,
                        routeToCache,
                        1.5f,
                        () => TryClaim(source.id),
                        foodHazard,
                        foodFatal,
                        cacheHazard,
                        cacheFatal))
                {
                    break;
                }
            }
        }

        private bool TryClaim(string sourceId)
        {
            var claimed = Model.TryClaimPortion(sourceId);
            if (!claimed)
            {
                return false;
            }

            if (Model.Sources.TryGetValue(sourceId, out var remaining))
            {
                if (visuals.TryGetValue(sourceId, out var visual))
                {
                    visual.SetPortions(remaining.portions);
                }
            }
            else
            {
                RemoveVisual(sourceId);
            }
            StateChanged?.Invoke();
            return true;
        }

        private void TryClaimAndReportMeal(
            string sourceId,
            IWildlifeLayoutAgent animal)
        {
            if (TryClaim(sourceId))
            {
                AnimalAte?.Invoke(animal);
            }
        }

        private IReadOnlyList<Vector3> BuildWaypoints(
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoomId)
        {
            var result = new List<Vector3>();
            var startRoomId = FindRoomId(worldStart);
            if (!string.IsNullOrEmpty(startRoomId) &&
                !string.IsNullOrEmpty(destinationRoomId) &&
                navigation.NavigationMap.TryFindRoute(startRoomId, destinationRoomId, out var route))
            {
                for (var index = 0; index < route.Count - 1; index++)
                {
                    if (!navigation.NavigationMap.TryGetConnectionPoint(
                            route[index],
                            route[index + 1],
                            out var localDoor))
                    {
                        continue;
                    }
                    var waypoint = mapRoot.TransformPoint(new Vector3(localDoor.x, 0f, localDoor.y));
                    waypoint.y = worldStart.y;
                    result.Add(waypoint);
                }
            }

            var final = worldDestination;
            final.y = worldStart.y;
            result.Add(final);
            return result;
        }

        private bool TryBuildGroundWaypoints(
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoomId,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal)
        {
            hazardWaypoint = -1;
            fatal = false;
            var startRoomId = FindRoomId(worldStart);
            if (string.IsNullOrEmpty(startRoomId) || string.IsNullOrEmpty(destinationRoomId) || traffic == null)
            {
                waypoints = BuildWaypoints(worldStart, worldDestination, destinationRoomId);
                return true;
            }

            var plan = traffic.PlanRoute(startRoomId, destinationRoomId);
            if (plan.Abandoned)
            {
                waypoints = Array.Empty<Vector3>();
                return false;
            }
            var result = new List<Vector3>();
            for (var index = 0; index < plan.Rooms.Count - 1; index++)
            {
                if (!navigation.NavigationMap.TryGetConnectionPoint(
                        plan.Rooms[index],
                        plan.Rooms[index + 1],
                        out var localDoor))
                {
                    continue;
                }
                var waypoint = mapRoot.TransformPoint(new Vector3(localDoor.x, 0f, localDoor.y));
                waypoint.y = worldStart.y;
                result.Add(waypoint);
            }
            var final = worldDestination;
            final.y = worldStart.y;
            result.Add(final);
            hazardWaypoint = plan.HazardConnectionIndex;
            fatal = plan.Fatal;
            waypoints = result;
            return true;
        }

        private string FindRoomId(Vector3 worldPosition)
        {
            var local = mapRoot.InverseTransformPoint(worldPosition);
            return navigation.NavigationMap.TryFindRoomContaining(
                new Vector2(local.x, local.z),
                out var roomId)
                ? roomId
                : string.Empty;
        }

        private void CreateVisual(PlayerFoodSourceState source)
        {
            var root = new GameObject($"Player Food {source.id}")
            {
                hideFlags = generatedHideFlags
            };
            root.transform.SetParent(foodRoot, true);
            root.transform.position = source.worldPosition;
            var visual = root.AddComponent<PlayerFoodSourceVisual>();
            visual.Initialize(material, generatedHideFlags);
            visual.SetPortions(source.portions);
            visuals[source.id] = visual;
        }

        private void RemoveVisual(string sourceId)
        {
            if (!visuals.TryGetValue(sourceId, out var visual))
            {
                return;
            }

            visuals.Remove(sourceId);
            Destroy(visual.gameObject);
        }

        private void ShowInvalidMarker(Vector3 worldPosition)
        {
            var marker = UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                "Invalid Feeding Position",
                foodRoot,
                worldPosition + Vector3.up * 0.03f,
                new Vector3(0.42f, 0.018f, 0.42f),
                new Color(0.95f, 0.38f, 0.12f, 0.88f),
                material,
                false,
                generatedHideFlags);
            marker.AddComponent<TemporaryFeedingMarker>();
        }

        private void HandleRestartRequested()
        {
            CancelFeedingMode();
            Model?.Reset();
            foreach (var squirrel in squirrels)
            {
                squirrel.RestoreCache(0);
            }
            ClearVisuals();
            StateChanged?.Invoke();
        }

        private void ClearVisuals()
        {
            foreach (var visual in visuals.Values)
            {
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
            }
            visuals.Clear();
        }
    }

    public sealed class TemporaryFeedingMarker : MonoBehaviour
    {
        private float remaining = 0.55f;

        private void Update()
        {
            remaining -= Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * (1f + (0.55f - remaining) * 0.25f);
            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
