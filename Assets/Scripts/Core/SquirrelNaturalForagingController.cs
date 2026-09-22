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
            traffic = trafficController;
            mapRoot = boardRoot;
            squirrels.AddRange(agents ?? Array.Empty<SquirrelDemoAgent>());
            foreach (var squirrel in squirrels)
            {
                retryTimers[squirrel] = 1.1f;
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
                retryTimers[squirrel] = Mathf.Max(0f, retryTimers[squirrel] - Time.deltaTime);
            }
            evaluationTimer -= Time.deltaTime;
            if (evaluationTimer > 0f)
            {
                return;
            }
            evaluationTimer = 0.5f;
            var active = runtime.Clock.Phase == DayPhase.Dawn ||
                         runtime.Clock.Phase == DayPhase.Day ||
                         runtime.Clock.Phase == DayPhase.Dusk;
            if (!active)
            {
                return;
            }
            foreach (var squirrel in squirrels)
            {
                if (squirrel == null || !squirrel.IsAlive || squirrel.IsRespondingToFood ||
                    !squirrel.CanStoreFood || retryTimers[squirrel] > 0f)
                {
                    continue;
                }
                TryBeginForaging(squirrel);
            }
        }

        private void TryBeginForaging(SquirrelDemoAgent squirrel)
        {
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
                var cacheRoom = FindRoomId(squirrel.CachePosition);
                if (!TryBuildRoute(squirrel.transform.position, source.Position, source.RoomId,
                        out var toFood, out var foodHazard, out var foodFatal) ||
                    !TryBuildRoute(source.Position, squirrel.CachePosition, cacheRoom,
                        out var toCache, out var cacheHazard, out var cacheFatal))
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
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoom,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal)
        {
            waypoints = Array.Empty<Vector3>();
            hazardWaypoint = -1;
            fatal = false;
            var startRoom = FindRoomId(worldStart);
            if (string.IsNullOrEmpty(startRoom) || string.IsNullOrEmpty(destinationRoom))
            {
                return false;
            }
            var plan = traffic.PlanRoute(startRoom, destinationRoom);
            if (plan.Abandoned)
            {
                return false;
            }
            var result = new List<Vector3>();
            for (var index = 0; index < plan.Rooms.Count - 1; index++)
            {
                if (!navigation.NavigationMap.TryGetConnectionPoint(
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
