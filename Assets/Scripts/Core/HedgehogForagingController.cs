using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class HedgehogForagingController : MonoBehaviour
    {
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly Dictionary<HedgehogDemoAgent, float> retryTimers = new();
        private GameRuntimeController runtime;
        private NaturalFoodController naturalFood;
        private AnimalNeedsController needs;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private Transform mapRoot;
        private float evaluationTimer;

        public void Initialize(
            GameRuntimeController runtimeController,
            NaturalFoodController foodController,
            AnimalNeedsController needsController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Transform boardRoot,
            IEnumerable<HedgehogDemoAgent> agents)
        {
            runtime = runtimeController;
            naturalFood = foodController;
            needs = needsController;
            navigation = navigationCoordinator;
            traffic = trafficController;
            mapRoot = boardRoot;
            hedgehogs.AddRange(agents ?? Array.Empty<HedgehogDemoAgent>());
            foreach (var hedgehog in hedgehogs)
            {
                retryTimers[hedgehog] = 0.8f;
            }
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused)
            {
                return;
            }
            foreach (var hedgehog in hedgehogs)
            {
                retryTimers[hedgehog] = Mathf.Max(0f, retryTimers[hedgehog] - Time.deltaTime);
            }
            evaluationTimer -= Time.deltaTime;
            if (evaluationTimer > 0f)
            {
                return;
            }
            evaluationTimer = 0.5f;
            var active = runtime.Clock.Phase == DayPhase.Dusk || runtime.Clock.Phase == DayPhase.Night;
            if (!active)
            {
                return;
            }
            foreach (var hedgehog in hedgehogs)
            {
                if (hedgehog == null || !hedgehog.IsAlive || hedgehog.IsForaging ||
                    retryTimers[hedgehog] > 0f || !needs.NeedsMealOf(hedgehog))
                {
                    continue;
                }
                TryBeginForaging(hedgehog);
            }
        }

        private void TryBeginForaging(HedgehogDemoAgent hedgehog)
        {
            var sources = naturalFood.Model.Sources.Values
                .Where(item => item.kind == NaturalFoodKind.Insect && item.portions > 0)
                .Select(item => item.roomId)
                .Distinct()
                .Select(roomId => new
                {
                    RoomId = roomId,
                    HasPosition = naturalFood.TryGetWorldPosition(roomId, NaturalFoodKind.Insect, out var position),
                    Position = position
                })
                .Where(item => item.HasPosition)
                .OrderBy(item => (item.Position - hedgehog.transform.position).sqrMagnitude)
                .ToArray();

            foreach (var source in sources)
            {
                if (!TryBuildRoute(hedgehog.transform.position, source.Position, source.RoomId,
                        out var route, out var hazard, out var fatal))
                {
                    continue;
                }
                if (hedgehog.BeginForaging(
                        route,
                        () =>
                        {
                            if (naturalFood.TryConsume(source.RoomId, NaturalFoodKind.Insect))
                            {
                                needs.RegisterPredationMeal(hedgehog);
                            }
                            retryTimers[hedgehog] = 2f;
                        },
                        hazard,
                        fatal))
                {
                    retryTimers[hedgehog] = 3f;
                    return;
                }
            }
            retryTimers[hedgehog] = 1.5f;
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
