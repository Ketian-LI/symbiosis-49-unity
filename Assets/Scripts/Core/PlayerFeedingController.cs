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
        private WasteManagementController waste;
        private RoomLayoutEditorController layoutEditor;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private Camera worldCamera;
        private Transform mapRoot;
        private Transform foodRoot;
        private Material material;
        private HideFlags generatedHideFlags;
        private double lastProcessedSimulationSeconds;
        private bool retryAnimalDispatch;
        private int lastManualFeedingDay;

        // A feed on day 1 becomes available again on day 4. This is a
        // renewable emergency action, not a lifetime cap in Endless Mode.
        public const int ManualFeedingIntervalDays = 3;

        public event Action StateChanged;
        public event Action<PlayerFoodSourceState> FoodPlaced;
        public event Action<string> FoodExpired;
        public event Action<string, int, bool> LeftoverFoodDiscarded;
        public event Action<string> InvalidPlacementAttempted;
        public event Action<IWildlifeLayoutAgent> AnimalAte;
        public event Action FeedingModeChanged;

        public PlayerFoodSourceModel Model { get; private set; }
        public bool FeedingModeActive => interaction.IsActive;
        public int LastManualFeedingDay => lastManualFeedingDay;
        public int NextManualFeedingDay => lastManualFeedingDay <= 0
            ? 1 : lastManualFeedingDay + ManualFeedingIntervalDays;
        public int ManualFeedingDaysRemaining => runtime == null
            ? 0 : Mathf.Max(0, NextManualFeedingDay - runtime.Clock.DayNumber);
        public bool CanActivateFeedingMode =>
            InteractionAllowed &&
            runtime.CanTakeDailyAction &&
            ManualFeedingDaysRemaining == 0 &&
            Model != null &&
            Model.PlayerPlacedCount < PlayerFoodSourceModel.MaximumSources;
        public int PrimarySquirrelCachePortions => squirrels.Count > 0
            ? squirrels[0].CachePortions
            : 0;
        public List<int> SquirrelCachePortions => squirrels
            .Select(squirrel => squirrel.CachePortions)
            .ToList();

        public void Initialize(
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            WasteManagementController wasteController,
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
            waste = wasteController;
            layoutEditor = editorController;
            navigation = navigationCoordinator;
            navigation.NavigationChanged += HandleNavigationChanged;
            traffic = trafficController;
            worldCamera = camera;
            mapRoot = boardRoot;
            foodRoot = sourceContainer;
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            pigeons.AddRange((pigeonAgents ?? Array.Empty<PigeonDemoAgent>()).Where(agent => agent != null));
            squirrels.AddRange((squirrelAgents ?? Array.Empty<SquirrelDemoAgent>()).Where(agent => agent != null));
            Model = new PlayerFoodSourceModel();
            lastProcessedSimulationSeconds = runtime.Clock.TotalSeconds;
            runtime.RestartRequested += HandleRestartRequested;
            runtime.StateChanged += HandleRuntimeStateChanged;
            runtime.BindPlayerFeeding(this);
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
                runtime.StateChanged -= HandleRuntimeStateChanged;
            }
            if (navigation != null)
            {
                navigation.NavigationChanged -= HandleNavigationChanged;
            }
        }

        private void Update()
        {
            if (runtime == null || Model == null || !Application.isPlaying)
            {
                return;
            }

            var elapsed = runtime.Clock.TotalSeconds - lastProcessedSimulationSeconds;
            lastProcessedSimulationSeconds = runtime.Clock.TotalSeconds;
            var expiredSources = Model.AdvanceExpired((float)Math.Max(0d, elapsed));
            foreach (var expired in expiredSources)
            {
                RemoveVisual(expired.id);
                FoodExpired?.Invoke(expired.id);
                if (expired.playerPlaced && expired.portions > 0 && waste?.Model != null)
                {
                    var roomId = FindRoomId(expired.worldPosition);
                    if (string.IsNullOrEmpty(roomId))
                    {
                        roomId = expired.roomId;
                    }
                    var routed = waste.Model.RouteWaste(roomId, 1);
                    LeftoverFoodDiscarded?.Invoke(roomId, expired.portions, routed);
                }
            }
            if (expiredSources.Count > 0)
            {
                StateChanged?.Invoke();
            }

            if (retryAnimalDispatch && runtime.HasActiveRun && !runtime.IsPaused)
            {
                retryAnimalDispatch = false;
                foreach (var source in Model.Sources.Values.Where(item => item.portions > 0).ToArray())
                {
                    DispatchAnimals(source);
                }
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
            if (runtime == null || !runtime.CanTakeDailyAction || ManualFeedingDaysRemaining > 0 ||
                Model.PlayerPlacedCount >= PlayerFoodSourceModel.MaximumSources ||
                string.IsNullOrEmpty(roomId) ||
                !Model.TryCreate(roomId, worldPosition, out var source))
            {
                ShowInvalidMarker(worldPosition);
                InvalidPlacementAttempted?.Invoke(roomId);
                return false;
            }

            lastManualFeedingDay = runtime.Clock.DayNumber;
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
            IEnumerable<int> allSquirrelCachePortions = null,
            int lastFeedingDay = 0)
        {
            ClearVisuals();
            Model.Restore(savedSources);
            // Older saves had no cooldown field. A still-active manual food
            // source is evidence that a feed happened on the saved day.
            lastManualFeedingDay = Mathf.Clamp(lastFeedingDay > 0
                ? lastFeedingDay
                : Model.PlayerPlacedCount > 0 ? runtime.Clock.DayNumber : 0,
                0, runtime.Clock.DayNumber);
            lastProcessedSimulationSeconds = runtime.Clock.TotalSeconds;
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

        private void HandleNavigationChanged()
        {
            // A route change can cancel a trip to player-placed food before
            // collection. Its unclaimed portion remains available to retry.
            retryAnimalDispatch = true;
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
                var route = BuildWaypoints(pigeon, pigeon.transform.position,
                    source.worldPosition, source.roomId);
                if (route.Count == 0)
                {
                    continue;
                }
                var delay = Mathf.Lerp(0.2f, 0.8f, availablePigeons.Count <= 1
                    ? 0f
                    : index / (float)(availablePigeons.Count - 1));
                pigeon.BeginFoodMission(
                    route,
                    delay,
                    () => TryClaimAndReportMeal(source.id, pigeon));
            }

            DispatchSquirrels(source);
        }

        private void DispatchSquirrels(PlayerFoodSourceState source)
        {
            var foodRoom = FindRoomId(source.worldPosition);
            if (string.IsNullOrEmpty(foodRoom))
            {
                return;
            }
            foreach (var squirrel in squirrels
                         .Where(agent => !agent.IsRespondingToFood && agent.CanStoreFood)
                         .OrderBy(agent => (agent.transform.position - source.worldPosition).sqrMagnitude))
            {
                var cacheRoom = FindRoomId(squirrel.CachePosition);
                var routeMap = navigation.NavigationMapFor(squirrel);
                if (!HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                        routeMap, cacheRoom, foodRoom))
                {
                    continue;
                }
                if (!TryBuildGroundWaypoints(
                    squirrel,
                    squirrel.transform.position,
                    source.worldPosition,
                    foodRoom,
                    out var routeToFood,
                    out var foodHazard,
                    out var foodFatal,
                    cacheRoom) ||
                    !TryBuildGroundWaypoints(
                    squirrel,
                    source.worldPosition,
                    squirrel.CachePosition,
                    cacheRoom,
                    out var routeToCache,
                    out var cacheHazard,
                    out var cacheFatal,
                    cacheRoom))
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
            IWildlifeLayoutAgent animal,
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoomId)
        {
            var routeMap = navigation.NavigationMapFor(animal);
            var startRoomId = FindRoomId(worldStart);
            if (string.IsNullOrEmpty(startRoomId) ||
                string.IsNullOrEmpty(destinationRoomId) ||
                !routeMap.TryFindRoute(startRoomId, destinationRoomId, out var route))
            {
                return Array.Empty<Vector3>();
            }

            var result = new List<Vector3>();
            for (var index = 0; index < route.Count - 1; index++)
            {
                if (!routeMap.TryGetConnectionPoint(
                        route[index], route[index + 1], out var localDoor))
                {
                    return Array.Empty<Vector3>();
                }
                var waypoint = mapRoot.TransformPoint(new Vector3(localDoor.x, 0f, localDoor.y));
                waypoint.y = worldStart.y;
                result.Add(waypoint);
            }

            var final = worldDestination;
            final.y = worldStart.y;
            result.Add(final);
            return result;
        }

        private bool TryBuildGroundWaypoints(
            SquirrelDemoAgent squirrel,
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoomId,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal,
            string squirrelHomeRoomId = null)
        {
            hazardWaypoint = -1;
            fatal = false;
            var routeMap = navigation.NavigationMapFor(squirrel);
            var startRoomId = FindRoomId(worldStart);
            if (squirrelHomeRoomId != null &&
                (!HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                     routeMap, squirrelHomeRoomId, startRoomId) ||
                 !HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                     routeMap, squirrelHomeRoomId, destinationRoomId)))
            {
                waypoints = Array.Empty<Vector3>();
                return false;
            }
            if (string.IsNullOrEmpty(startRoomId) || string.IsNullOrEmpty(destinationRoomId) || traffic == null)
            {
                if (squirrelHomeRoomId != null &&
                    (!routeMap.TryFindRoute(startRoomId, destinationRoomId,
                         out var localRoute) ||
                     !HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(
                         routeMap, squirrelHomeRoomId, localRoute)))
                {
                    waypoints = Array.Empty<Vector3>();
                    return false;
                }
                waypoints = BuildWaypoints(squirrel, worldStart, worldDestination,
                    destinationRoomId);
                return waypoints.Count > 0;
            }

            var plan = traffic.PlanRoute(startRoomId, destinationRoomId, routeMap);
            if (plan.Abandoned ||
                squirrelHomeRoomId != null && !HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(
                    routeMap, squirrelHomeRoomId, plan.Rooms))
            {
                waypoints = Array.Empty<Vector3>();
                return false;
            }
            var result = new List<Vector3>();
            for (var index = 0; index < plan.Rooms.Count - 1; index++)
            {
                if (!routeMap.TryGetConnectionPoint(
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

        // Placed dishes stay at a world position when rooms are swapped. The
        // original roomId can therefore be stale in the food-location UI.
        public string CurrentRoomIdAt(Vector3 worldPosition) => FindRoomId(worldPosition);

        private void CreateVisual(PlayerFoodSourceState source)
        {
            var root = new GameObject($"Player Food {source.id}")
            {
                hideFlags = generatedHideFlags
            };
            root.transform.SetParent(foodRoot, true);
            root.transform.position = source.worldPosition;
            var visual = root.AddComponent<PlayerFoodSourceVisual>();
            visual.Initialize(material, generatedHideFlags, source.playerPlaced);
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
            visual.gameObject.SetActive(false);
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
            retryAnimalDispatch = false;
            lastManualFeedingDay = 0;
            Model?.Reset();
            lastProcessedSimulationSeconds = runtime.Clock.TotalSeconds;
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
