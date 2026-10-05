using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Birds sharing a home room leave as a small flock for the same reachable
    // source. Each bird still has to arrive and claim its own portion.
    public sealed class PigeonNaturalForagingController : MonoBehaviour
    {
        private readonly Dictionary<string, List<PigeonDemoAgent>> flocks = new();
        private readonly Dictionary<PigeonDemoAgent, string> homeRooms = new();
        private readonly Dictionary<PigeonDemoAgent, (string roomId, NaturalFoodKind kind)> reservations = new();
        private GameRuntimeController runtime;
        private NaturalFoodController naturalFood;
        private AnimalNeedsController needs;
        private AnimalNavigationCoordinator navigation;
        private Transform mapRoot;
        private float evaluationTimer;

        public void Initialize(
            GameRuntimeController runtimeController,
            NaturalFoodController foodController,
            AnimalNeedsController needsController,
            AnimalNavigationCoordinator navigationCoordinator,
            Transform boardRoot,
            IReadOnlyDictionary<PigeonDemoAgent, string> homeRooms)
        {
            runtime = runtimeController;
            naturalFood = foodController;
            needs = needsController;
            navigation = navigationCoordinator;
            mapRoot = boardRoot;
            foreach (var pair in homeRooms ?? new Dictionary<PigeonDemoAgent, string>())
            {
                if (pair.Key == null || string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }
                this.homeRooms[pair.Key] = pair.Value;
                if (!flocks.TryGetValue(pair.Value, out var flock))
                {
                    flock = new List<PigeonDemoAgent>();
                    flocks.Add(pair.Value, flock);
                }
                flock.Add(pair.Key);
            }
            navigation.NavigationChanged += HandleNavigationChanged;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (navigation != null)
            {
                navigation.NavigationChanged -= HandleNavigationChanged;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
        }

        private void HandleNavigationChanged()
        {
            ReleaseStaleReservations();
            evaluationTimer = 0f;
        }

        private void HandleRestartRequested()
        {
            foreach (var bird in reservations.Keys.ToArray())
            {
                bird?.CancelFoodMissionForRouteChange();
            }
            reservations.Clear();
            evaluationTimer = 0f;
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused)
            {
                return;
            }

            ReleaseStaleReservations();
            evaluationTimer -= runtime.ActorPresentationDeltaTime;
            if (evaluationTimer > 0f)
            {
                return;
            }
            evaluationTimer = 0.5f;
            if (runtime.Clock.Phase != DayPhase.Dawn &&
                runtime.Clock.Phase != DayPhase.Day)
            {
                return;
            }

            foreach (var flock in flocks.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                foreach (var bird in flock.Value.Where(bird => bird != null &&
                             bird.IsAlive && !bird.IsRespondingToFood))
                {
                    TryReturnHome(bird);
                }
                var availableBirds = flock.Value
                    .Where(bird => bird != null && bird.IsAlive &&
                                   !bird.IsRespondingToFood && needs.NeedsMealOf(bird))
                    .ToArray();
                if (availableBirds.Length == 0)
                {
                    continue;
                }

                // The flock chooses one place. Limited portions may send only
                // part of it now; the rest can leave for a later source.
                // Prefer a reachable source near this flock. Sorting only by
                // room ID could send birds two rooms away while food remained
                // next door, wasting the short dawn/day foraging window.
                var orderedSources = naturalFood.Model.Sources.Values
                    .Where(source => source.kind == NaturalFoodKind.Seed ||
                                     source.kind == NaturalFoodKind.DiscardedFood)
                    .Select(source => new
                    {
                        Source = source,
                        Distance = availableBirds.Select(bird =>
                            HabitatFoodNetworkModel.CanReachSource(
                                navigation.NavigationMapFor(bird), flock.Key,
                                WildlifeSpecies.Pigeon, source.roomId, out var distance)
                                ? distance : int.MaxValue).Min()
                    })
                    .Where(item => item.Distance < int.MaxValue)
                    .OrderBy(item => item.Source.kind == NaturalFoodKind.Seed ? 0 : 1)
                    .ThenBy(item => item.Distance)
                    .ThenBy(item => item.Source.roomId, StringComparer.Ordinal);
                foreach (var candidate in orderedSources)
                {
                    var source = candidate.Source;
                    if (!naturalFood.TryGetWorldPosition(source.roomId, source.kind,
                            out var foodPosition))
                    {
                        continue;
                    }

                    var remaining = source.portions - reservations.Values.Count(item =>
                        item.roomId == source.roomId && item.kind == source.kind);
                    if (remaining <= 0)
                    {
                        continue;
                    }

                    var dispatched = 0;
                    foreach (var bird in availableBirds)
                    {
                        if (dispatched >= remaining ||
                            !HabitatFoodNetworkModel.CanReachSource(
                                navigation.NavigationMapFor(bird), flock.Key,
                                WildlifeSpecies.Pigeon, source.roomId, out _) ||
                            !TryBuildRoute(bird, bird.transform.position, foodPosition,
                                source.roomId, out var waypoints))
                        {
                            continue;
                        }

                        var roomId = source.roomId;
                        var kind = source.kind;
                        if (!bird.BeginFoodMission(waypoints, dispatched * 0.12f,
                                () => Arrive(bird, roomId, kind)))
                        {
                            continue;
                        }
                        reservations[bird] = (roomId, kind);
                        dispatched++;
                    }
                    if (dispatched > 0)
                    {
                        break;
                    }
                }
            }
        }

        private void Arrive(PigeonDemoAgent bird, string roomId, NaturalFoodKind kind)
        {
            reservations.Remove(bird);
            needs.TryFeedFromNaturalArrival(bird, roomId, kind);
            TryReturnHome(bird);
        }

        private void TryReturnHome(PigeonDemoAgent bird)
        {
            if (bird == null || !bird.IsAlive || bird.IsRespondingToFood ||
                !homeRooms.TryGetValue(bird, out var homeRoomId))
            {
                return;
            }
            var local = mapRoot.InverseTransformPoint(bird.transform.position);
            if (!navigation.NavigationMap.TryFindRoomContaining(
                    new Vector2(local.x, local.z), out var currentRoomId) ||
                currentRoomId == homeRoomId ||
                !TryBuildRoute(bird, bird.transform.position, bird.SpawnPosition,
                    homeRoomId, out var waypoints))
            {
                return;
            }
            bird.BeginFoodMission(waypoints, 0f, null, false);
        }

        private void ReleaseStaleReservations()
        {
            foreach (var bird in reservations.Keys.ToArray())
            {
                if (bird == null || !bird.IsAlive || !bird.IsRespondingToFood)
                {
                    reservations.Remove(bird);
                }
            }
        }

        private bool TryBuildRoute(PigeonDemoAgent bird, Vector3 start, Vector3 destination,
            string destinationRoomId, out IReadOnlyList<Vector3> waypoints)
        {
            waypoints = Array.Empty<Vector3>();
            var routeMap = navigation.NavigationMapFor(bird);
            var local = mapRoot.InverseTransformPoint(start);
            if (!routeMap.TryFindRoomContaining(new Vector2(local.x, local.z), out var startRoomId) ||
                !HabitatFoodNetworkModel.CanReachSource(routeMap, startRoomId,
                    WildlifeSpecies.Pigeon, destinationRoomId, out _) ||
                !routeMap.TryFindRoute(startRoomId, destinationRoomId, out var rooms))
            {
                return false;
            }

            var points = new List<Vector3>();
            for (var index = 0; index < rooms.Count - 1; index++)
            {
                if (!routeMap.TryGetConnectionPoint(rooms[index], rooms[index + 1],
                        out var door))
                {
                    return false;
                }
                var waypoint = mapRoot.TransformPoint(new Vector3(door.x, local.y, door.y));
                waypoint.y = start.y;
                points.Add(waypoint);
            }
            points.Add(new Vector3(destination.x, start.y, destination.z));
            waypoints = points;
            return true;
        }
    }
}
