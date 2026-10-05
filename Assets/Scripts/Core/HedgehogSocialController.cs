using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // An optional night-time outing. Food always has priority; a meeting can
    // happen only after both hedgehogs have eaten and a safe animal route exists.
    public sealed class HedgehogSocialController : MonoBehaviour
    {
        private static readonly HashSet<string> UnsafeRooms = new(
            RoomLayoutData.All.Where(room => room.Type == RoomType.Garage ||
                                             room.Type == RoomType.FoxDen)
                .Select(room => room.Id).Append("shared-j"));

        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly Dictionary<HedgehogDemoAgent, string> homes = new();
        private GameRuntimeController runtime;
        private AnimalNeedsController needs;
        private AnimalNavigationCoordinator navigation;
        private Transform mapRoot;
        private Func<string, Transform> resolveRoom;
        private System.Random random;
        private int trackedDay;
        private float evaluationTimer;
        private bool meetingInProgress;
        private bool returningHome;
        private bool firstArrived;
        private bool secondArrived;

        public int LastMeetingDay { get; private set; }
        public bool MeetingInProgress => meetingInProgress;

        public void Initialize(
            GameRuntimeController runtimeController,
            AnimalNeedsController needsController,
            AnimalNavigationCoordinator navigationCoordinator,
            Transform boardRoot,
            Func<string, Transform> roomResolver,
            IEnumerable<HedgehogDemoAgent> agents,
            IReadOnlyDictionary<IWildlifeLayoutAgent, string> homeRooms)
        {
            runtime = runtimeController;
            needs = needsController;
            navigation = navigationCoordinator;
            mapRoot = boardRoot;
            resolveRoom = roomResolver;
            hedgehogs.AddRange((agents ?? Array.Empty<HedgehogDemoAgent>())
                .Where(item => item != null).Take(2));
            foreach (var hedgehog in hedgehogs)
            {
                if (homeRooms != null && homeRooms.TryGetValue(hedgehog, out var roomId))
                {
                    homes[hedgehog] = roomId;
                }
            }
            trackedDay = runtime.Clock.DayNumber;
            random = new System.Random(349 + trackedDay * 37);
            navigation.NavigationChanged += HandleNavigationChanged;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (navigation != null) navigation.NavigationChanged -= HandleNavigationChanged;
            if (runtime != null) runtime.RestartRequested -= HandleRestartRequested;
        }

        private void HandleRestartRequested()
        {
            AbortMeeting();
            LastMeetingDay = 0;
            trackedDay = runtime.Clock.DayNumber;
            random = new System.Random(349 + trackedDay * 37);
        }

        private void HandleNavigationChanged()
        {
            // The navigation coordinator cancels invalid animal waypoints
            // before raising this event. Release a waiting partner as well.
            AbortMeeting();
            evaluationTimer = 0.5f;
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused ||
                hedgehogs.Count != 2 || homes.Count != 2)
            {
                return;
            }
            if (trackedDay != runtime.Clock.DayNumber)
            {
                AbortMeeting();
                trackedDay = runtime.Clock.DayNumber;
                random = new System.Random(349 + trackedDay * 37);
            }
            var phase = runtime.Clock.Phase;
            if (phase != DayPhase.Dusk && phase != DayPhase.Night)
            {
                AbortMeeting();
                return;
            }

            evaluationTimer -= runtime.ActorPresentationDeltaTime;
            if (evaluationTimer > 0f) return;
            evaluationTimer = 0.5f;

            var first = hedgehogs[0];
            var second = hedgehogs[1];
            if (!first.IsAlive || !second.IsAlive)
            {
                AbortMeeting();
                return;
            }
            if (meetingInProgress)
            {
                if (!returningHome &&
                    (!firstArrived && !first.IsForaging ||
                     !secondArrived && !second.IsForaging))
                {
                    AbortMeeting();
                }
                else if (returningHome)
                {
                    TryReturnHome(first);
                    TryReturnHome(second);
                    if (IsAtHome(first) && IsAtHome(second) &&
                        !first.IsForaging && !second.IsForaging)
                    {
                        meetingInProgress = false;
                        returningHome = false;
                    }
                }
                return;
            }

            // A disrupted return is still owed, but must not interrupt food.
            if (!needs.NeedsMealOf(first)) TryReturnHome(first);
            if (!needs.NeedsMealOf(second)) TryReturnHome(second);
            if (LastMeetingDay == trackedDay || first.IsForaging || second.IsForaging ||
                first.IsWaitingForCompanion || second.IsWaitingForCompanion ||
                needs.NeedsMealOf(first) || needs.NeedsMealOf(second))
            {
                return;
            }
            TryBeginMeeting(first, second);
        }

        private void TryBeginMeeting(HedgehogDemoAgent first, HedgehogDemoAgent second)
        {
            var map = navigation.NavigationMap;
            var firstRoom = FindRoomId(first.transform.position);
            var secondRoom = FindRoomId(second.transform.position);
            if (!TryRandomSafeRoute(map, firstRoom, secondRoom, out var route) ||
                route.Count > 9)
            {
                return;
            }
            var middleIndex = route.Count / 2;
            var meetingRoot = resolveRoom?.Invoke(route[middleIndex]);
            if (meetingRoot == null) return;
            var meetingPoint = meetingRoot.position;
            if (!TryBuildWaypoints(first.transform.position,
                    route.Take(middleIndex + 1).ToArray(), meetingPoint,
                    out var firstRoute) ||
                !TryBuildWaypoints(second.transform.position,
                    route.Skip(middleIndex).Reverse().ToArray(), meetingPoint,
                    out var secondRoute) ||
                !first.BeginForaging(firstRoute, () => ReachedMeeting(first)))
            {
                return;
            }
            if (!second.BeginForaging(secondRoute, () => ReachedMeeting(second)))
            {
                first.CancelForagingForRouteChange();
                return;
            }
            meetingInProgress = true;
            returningHome = false;
            firstArrived = secondArrived = false;
        }

        private void ReachedMeeting(HedgehogDemoAgent hedgehog)
        {
            if (!meetingInProgress || returningHome ||
                runtime.Clock.DayNumber != trackedDay)
            {
                return;
            }
            if (!hedgehog.BeginCompanionWait())
            {
                AbortMeeting();
                return;
            }
            if (hedgehog == hedgehogs[0]) firstArrived = true;
            if (hedgehog == hedgehogs[1]) secondArrived = true;
            if (!firstArrived || !secondArrived) return;

            LastMeetingDay = trackedDay;
            returningHome = true;
            foreach (var partner in hedgehogs) partner.EndCompanionWait();
            foreach (var partner in hedgehogs) TryReturnHome(partner);
        }

        private void TryReturnHome(HedgehogDemoAgent hedgehog)
        {
            if (!hedgehog.IsAlive || hedgehog.IsForaging ||
                hedgehog.IsWaitingForCompanion || IsAtHome(hedgehog) ||
                !homes.TryGetValue(hedgehog, out var homeRoom))
            {
                return;
            }
            var currentRoom = FindRoomId(hedgehog.transform.position);
            if (!TryRandomSafeRoute(navigation.NavigationMap, currentRoom,
                    homeRoom, out var rooms) ||
                !TryBuildWaypoints(hedgehog.transform.position, rooms,
                    hedgehog.SpawnPosition, out var waypoints))
            {
                return;
            }
            hedgehog.BeginForaging(waypoints, null);
        }

        private bool IsAtHome(HedgehogDemoAgent hedgehog) =>
            homes.TryGetValue(hedgehog, out var homeRoom) &&
            FindRoomId(hedgehog.transform.position) == homeRoom;

        private void AbortMeeting()
        {
            if (!meetingInProgress) return;
            foreach (var hedgehog in hedgehogs)
            {
                if (hedgehog == null) continue;
                hedgehog.EndCompanionWait();
                hedgehog.CancelForagingForRouteChange();
            }
            meetingInProgress = returningHome = firstArrived = secondArrived = false;
        }

        private bool TryBuildWaypoints(Vector3 start, IReadOnlyList<string> rooms,
            Vector3 destination, out IReadOnlyList<Vector3> waypoints)
        {
            waypoints = Array.Empty<Vector3>();
            if (rooms == null || rooms.Count == 0) return false;
            var points = new List<Vector3>();
            for (var index = 0; index < rooms.Count - 1; index++)
            {
                if (!navigation.NavigationMap.TryGetConnectionPoint(
                        rooms[index], rooms[index + 1], out var door))
                {
                    return false;
                }
                var point = mapRoot.TransformPoint(new Vector3(door.x, 0f, door.y));
                point.y = start.y;
                points.Add(point);
            }
            points.Add(new Vector3(destination.x, start.y, destination.z));
            waypoints = points;
            return true;
        }

        private bool TryRandomSafeRoute(RoomNavigationMap map, string start,
            string destination, out IReadOnlyList<string> route)
        {
            route = Array.Empty<string>();
            if (map == null || string.IsNullOrEmpty(start) ||
                string.IsNullOrEmpty(destination) ||
                UnsafeRooms.Contains(start) || UnsafeRooms.Contains(destination))
            {
                return false;
            }
            var previous = new Dictionary<string, string> { [start] = null };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0 && !previous.ContainsKey(destination))
            {
                var room = queue.Dequeue();
                var neighbours = map.NeighboursOf(room)
                    .Where(id => !UnsafeRooms.Contains(id))
                    .OrderBy(id => id, StringComparer.Ordinal).ToList();
                for (var index = neighbours.Count - 1; index > 0; index--)
                {
                    var swap = random.Next(index + 1);
                    (neighbours[index], neighbours[swap]) =
                        (neighbours[swap], neighbours[index]);
                }
                foreach (var neighbour in neighbours)
                {
                    if (previous.ContainsKey(neighbour)) continue;
                    previous[neighbour] = room;
                    queue.Enqueue(neighbour);
                }
            }
            if (!previous.ContainsKey(destination)) return false;
            var result = new List<string>();
            for (var room = destination; room != null; room = previous[room])
                result.Add(room);
            result.Reverse();
            route = result;
            return true;
        }

        private string FindRoomId(Vector3 position)
        {
            var local = mapRoot.InverseTransformPoint(position);
            return navigation.NavigationMap.TryFindRoomContaining(
                new Vector2(local.x, local.z), out var roomId)
                ? roomId : string.Empty;
        }
    }
}
