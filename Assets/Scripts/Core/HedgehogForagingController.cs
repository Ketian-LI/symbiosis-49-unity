using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct HedgehogNightReport
    {
        public HedgehogNightReport(int day, int coveredTrips, int otherTrips, int meals)
        {
            Day = day;
            CoveredTrips = coveredTrips;
            OtherTrips = otherTrips;
            Meals = meals;
        }

        public int Day { get; }
        public int CoveredTrips { get; }
        public int OtherTrips { get; }
        public int Meals { get; }
    }

    public sealed class HedgehogForagingController : MonoBehaviour
    {
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly Dictionary<HedgehogDemoAgent, float> retryTimers = new();
        private GameRuntimeController runtime;
        private NaturalFoodController naturalFood;
        private AnimalNeedsController needs;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private RoomLayoutEditorController layoutEditor;
        private Transform mapRoot;
        private float evaluationTimer;
        private int trackedDay;
        private int coveredTrips;
        private int otherTrips;
        private int meals;

        public ShrubShelterModel Shelter { get; } = new();
        public int CurrentShelterPairs => navigation != null && runtime != null
            ? Shelter.ConnectedPairCount(navigation.NavigationMap, runtime.Clock.DayNumber)
            : 0;
        public int RecoveringShrubs => runtime != null
            ? Shelter.RecoveringShrubCount(runtime.Clock.DayNumber)
            : 0;
        public HedgehogNightReport LastNightReport { get; private set; }
        public event Action NightReportChanged;
        public event Action<bool, string> ForagingRouteStarted;

        public void Initialize(
            GameRuntimeController runtimeController,
            NaturalFoodController foodController,
            AnimalNeedsController needsController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Transform boardRoot,
            IEnumerable<HedgehogDemoAgent> agents,
            RoomLayoutEditorController editorController)
        {
            runtime = runtimeController;
            naturalFood = foodController;
            needs = needsController;
            navigation = navigationCoordinator;
            navigation.NavigationChanged += HandleNavigationChanged;
            traffic = trafficController;
            mapRoot = boardRoot;
            layoutEditor = editorController;
            trackedDay = runtime.Clock.DayNumber;
            layoutEditor.RoomsMoved += HandleRoomsMoved;
            runtime.RestartRequested += HandleRestartRequested;
            hedgehogs.AddRange(agents ?? Array.Empty<HedgehogDemoAgent>());
            foreach (var hedgehog in hedgehogs)
            {
                retryTimers[hedgehog] = 0.8f;
            }
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.RoomsMoved -= HandleRoomsMoved;
            }
            if (navigation != null)
            {
                navigation.NavigationChanged -= HandleNavigationChanged;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
        }

        public void RestoreSession(IEnumerable<UrbanWildlifeRooms.Data.ShrubShelterSaveData> saved)
        {
            Shelter.Restore(saved);
            trackedDay = runtime.Clock.DayNumber;
            coveredTrips = otherTrips = meals = 0;
            LastNightReport = default;
            NightReportChanged?.Invoke();
        }

        public bool IsCovered(Vector3 worldPosition)
        {
            return Shelter.IsCovered(navigation.NavigationMap, FindRoomId(worldPosition),
                runtime.Clock.DayNumber);
        }

        private void HandleRoomsMoved(IReadOnlyList<string> roomIds)
        {
            Shelter.RecordMovement(roomIds, runtime.Clock.DayNumber);
        }

        private void HandleNavigationChanged()
        {
            foreach (var hedgehog in hedgehogs)
            {
                if (hedgehog != null && hedgehog.IsAlive && !hedgehog.IsForaging)
                {
                    retryTimers[hedgehog] = 0.5f;
                }
            }
        }

        private void HandleRestartRequested()
        {
            RestoreSession(null);
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun)
            {
                return;
            }
            if (trackedDay != runtime.Clock.DayNumber)
            {
                LastNightReport = new HedgehogNightReport(trackedDay, coveredTrips, otherTrips, meals);
                trackedDay = runtime.Clock.DayNumber;
                coveredTrips = otherTrips = meals = 0;
                NightReportChanged?.Invoke();
            }
            if (runtime.IsPaused)
            {
                return;
            }
            foreach (var hedgehog in hedgehogs)
            {
                retryTimers[hedgehog] = Mathf.Max(0f,
                    retryTimers[hedgehog] - runtime.ActorPresentationDeltaTime);
            }
            evaluationTimer -= runtime.ActorPresentationDeltaTime;
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
                .OrderByDescending(item => Shelter.IsCovered(navigation.NavigationMap,
                    item.RoomId, runtime.Clock.DayNumber))
                .ThenBy(item => (item.Position - hedgehog.transform.position).sqrMagnitude)
                .ToArray();

            foreach (var source in sources)
            {
                if (!HabitatFoodNetworkModel.CanReachSource(
                        navigation.NavigationMapFor(hedgehog), FindRoomId(hedgehog.transform.position),
                        hedgehog.Species, source.RoomId, out _))
                {
                    continue;
                }
                if (!TryBuildRoute(hedgehog, hedgehog.transform.position, source.Position, source.RoomId,
                        out var route, out var hazard, out var fatal, out var coveredRoute))
                {
                    continue;
                }
                var missionDay = runtime.Clock.DayNumber;
                if (hedgehog.BeginForaging(
                        route,
                        () =>
                        {
                            if (naturalFood.TryConsume(source.RoomId, NaturalFoodKind.Insect))
                            {
                                needs.RegisterPredationMeal(hedgehog);
                                if (trackedDay == missionDay)
                                {
                                    meals++;
                                }
                                else if (LastNightReport.Day == missionDay)
                                {
                                    LastNightReport = new HedgehogNightReport(missionDay,
                                        LastNightReport.CoveredTrips, LastNightReport.OtherTrips,
                                        LastNightReport.Meals + 1);
                                    NightReportChanged?.Invoke();
                                }
                            }
                            retryTimers[hedgehog] = 2f;
                        },
                        hazard,
                        fatal))
                {
                    if (coveredRoute) coveredTrips++;
                    else otherTrips++;
                    ForagingRouteStarted?.Invoke(coveredRoute, source.RoomId);
                    retryTimers[hedgehog] = 3f;
                    return;
                }
            }
            retryTimers[hedgehog] = 1.5f;
        }

        private bool TryBuildRoute(
            HedgehogDemoAgent hedgehog,
            Vector3 worldStart,
            Vector3 worldDestination,
            string destinationRoom,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal,
            out bool coveredRoute)
        {
            waypoints = Array.Empty<Vector3>();
            hazardWaypoint = -1;
            fatal = false;
            coveredRoute = false;
            var startRoom = FindRoomId(worldStart);
            if (string.IsNullOrEmpty(startRoom) || string.IsNullOrEmpty(destinationRoom))
            {
                return false;
            }
            var routeMap = navigation.NavigationMapFor(hedgehog);
            coveredRoute = Shelter.TryFindCoveredRoute(routeMap,
                startRoom, destinationRoom, runtime.Clock.DayNumber, out var shelteredRooms);
            var plan = coveredRoute
                ? new GroundRoutePlan { Rooms = shelteredRooms, Decision = GarageCrossingDecision.NoGarage }
                : traffic.PlanRoute(startRoom, destinationRoom, routeMap);
            if (plan.Abandoned)
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
