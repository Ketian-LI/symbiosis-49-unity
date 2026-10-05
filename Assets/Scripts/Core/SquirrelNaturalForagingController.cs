using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class SquirrelNaturalForagingController : MonoBehaviour
    {
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly Dictionary<SquirrelDemoAgent, float> retryTimers = new();
        private GameRuntimeController runtime;
        private NaturalFoodController naturalFood;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private Transform mapRoot;
        private float evaluationTimer;

        public void Initialize(
            GameRuntimeController runtimeController,
            NaturalFoodController foodController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Transform boardRoot,
            IEnumerable<SquirrelDemoAgent> agents)
        {
            runtime = runtimeController;
            naturalFood = foodController;
            navigation = navigationCoordinator;
            navigation.NavigationChanged += HandleNavigationChanged;
            traffic = trafficController;
            mapRoot = boardRoot;
            squirrels.AddRange(agents ?? Array.Empty<SquirrelDemoAgent>());
            foreach (var squirrel in squirrels)
            {
                retryTimers[squirrel] = 1.1f;
            }
        }

        private void OnDestroy()
        {
            if (navigation != null)
            {
                navigation.NavigationChanged -= HandleNavigationChanged;
            }
        }

        private void HandleNavigationChanged()
        {
            foreach (var squirrel in squirrels)
            {
                if (squirrel == null || !squirrel.IsAlive || squirrel.IsRespondingToFood)
                {
                    continue;
                }
                // Player food gets the first chance to redispatch; a carried
                // portion instead needs an immediate new route to the cache.
                retryTimers[squirrel] = squirrel.HasFoodInTransit ? 0f : 0.6f;
            }
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused)
            {
                return;
            }
            foreach (var squirrel in squirrels)
            {
                retryTimers[squirrel] = Mathf.Max(0f,
                    retryTimers[squirrel] - runtime.ActorPresentationDeltaTime);
            }
            evaluationTimer -= runtime.ActorPresentationDeltaTime;
            if (evaluationTimer > 0f)
            {
                return;
            }
            evaluationTimer = 0.5f;
            var active = runtime.Clock.Phase == DayPhase.Dawn ||
                         runtime.Clock.Phase == DayPhase.Day ||
                         runtime.Clock.Phase == DayPhase.Dusk;
            foreach (var squirrel in squirrels)
            {
                if (squirrel == null || !squirrel.IsAlive || squirrel.IsRespondingToFood ||
                    retryTimers[squirrel] > 0f)
                {
                    continue;
                }
                if (squirrel.HasFoodInTransit)
                {
                    TryBeginCacheReturn(squirrel);
                }
                else if (active && squirrel.CanStoreFood)
                {
                    TryBeginForaging(squirrel);
                }
            }
        }

        private void TryBeginCacheReturn(SquirrelDemoAgent squirrel)
        {
            if (!squirrel.CanResumeCacheReturn)
            {
                retryTimers[squirrel] = 0.5f;
                return;
            }
            var cacheRoom = FindRoomId(squirrel.CachePosition);
            if (TryBuildRoute(squirrel, squirrel.transform.position, squirrel.CachePosition, cacheRoom,
                    out var route, out var hazard, out var fatal) &&
                squirrel.BeginCacheReturn(route, hazard, fatal))
            {
                retryTimers[squirrel] = 1f;
                return;
            }
            retryTimers[squirrel] = 1f;
        }

        private void TryBeginForaging(SquirrelDemoAgent squirrel)
        {
            var routeMap = navigation.NavigationMapFor(squirrel);
            var cacheRoom = FindRoomId(squirrel.CachePosition);
            var currentRoom = FindRoomId(squirrel.transform.position);
            if (!HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                    routeMap, cacheRoom, currentRoom))
            {
                retryTimers[squirrel] = 1.8f;
                return;
            }
            var sources = naturalFood.Model.Sources.Values
                .Where(item => item.kind == NaturalFoodKind.Nut && item.portions > 0)
                .Select(item => item.roomId)
                .Distinct()
                .Select(roomId => new
                {
                    RoomId = roomId,
                    HasPosition = naturalFood.TryGetWorldPosition(roomId, NaturalFoodKind.Nut, out var position),
                    Position = position
                })
                .Where(item => item.HasPosition)
                .OrderBy(item => (item.Position - squirrel.transform.position).sqrMagnitude)
                .ToArray();

            foreach (var source in sources)
            {
                if (!HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                        routeMap, cacheRoom, source.RoomId) ||
                    !HabitatFoodNetworkModel.CanReachSource(
                        routeMap, currentRoom,
                        squirrel.Species, source.RoomId, out _))
                {
                    continue;
                }
                if (!TryBuildRoute(squirrel, squirrel.transform.position, source.Position, source.RoomId,
                        out var toFood, out var foodHazard, out var foodFatal, cacheRoom) ||
                    !TryBuildRoute(squirrel, source.Position, squirrel.CachePosition, cacheRoom,
                        out var toCache, out var cacheHazard, out var cacheFatal, cacheRoom))
                {
                    continue;
                }
                if (squirrel.BeginFoodMission(
                        toFood,
                        toCache,
                        0.15f,
                        () => naturalFood.TryConsume(source.RoomId, NaturalFoodKind.Nut),
                        foodHazard,
                        foodFatal,
                        cacheHazard,
                        cacheFatal))
                {
                    retryTimers[squirrel] = 4f;
                    return;
                }
            }
            retryTimers[squirrel] = 1.8f;
        }

        private bool TryBuildRoute(
            SquirrelDemoAgent squirrel,
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoom,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal,
            string homeRoomId = null)
        {
            waypoints = Array.Empty<Vector3>();
            hazardWaypoint = -1;
            fatal = false;
            var startRoom = FindRoomId(worldStart);
            if (string.IsNullOrEmpty(startRoom) || string.IsNullOrEmpty(destinationRoom))
            {
                return false;
            }
            var routeMap = navigation.NavigationMapFor(squirrel);
            var plan = traffic.PlanRoute(startRoom, destinationRoom, routeMap);
            if (plan.Abandoned ||
                homeRoomId != null && !HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(
                    routeMap, homeRoomId, plan.Rooms))
            {
                return false;
            }
            var result = new List<Vector3>();
            for (var index = 0; index < plan.Rooms.Count - 1; index++)
            {
                if (!routeMap.TryGetConnectionPoint(
                        plan.Rooms[index], plan.Rooms[index + 1], out var localDoor))
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
            waypoints = result;
            hazardWaypoint = plan.HazardConnectionIndex;
            fatal = plan.Fatal;
            return true;
        }

        private string FindRoomId(Vector3 worldPosition)
        {
            var local = mapRoot.InverseTransformPoint(worldPosition);
            return navigation.NavigationMap.TryFindRoomContaining(new Vector2(local.x, local.z), out var roomId)
                ? roomId
                : string.Empty;
        }
    }
}
