using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.People
{
    // Keeps the number and homes of visible people in step with the resident model.
    public sealed class CitizenPopulationPresenter : MonoBehaviour
    {
        private static readonly Vector2 HomeRoamHalfExtents = new(0.52f, 0.52f);
        private readonly Dictionary<string, CitizenDemoAgent> agents = new();
        private Func<IReadOnlyList<ResidentState>> residentSource;
        private Func<string, Transform> roomTransformResolver;
        private Material material;
        private HideFlags generatedHideFlags;
        private ResidentPopulationModel populationModel;
        private Transform boardRoot;

        public event Action<string, CitizenDemoAgent> ResidentClicked;
        public event Action<string, Vector3, Vector3> WorkerCommuteMoved;

        public int VisibleResidentCount => agents.Count;

        public CitizenDemoAgent PrimaryAgent
        {
            get
            {
                var first = residentSource?.Invoke()?.FirstOrDefault();
                return first != null && agents.TryGetValue(first.id, out var agent) ? agent : null;
            }
        }

        public void Initialize(
            Func<IReadOnlyList<ResidentState>> getResidents,
            Func<string, Transform> resolveRoom,
            Material sharedMaterial,
            HideFlags hideFlags,
            ResidentPopulationModel residentsModel = null,
            Transform mapRoot = null)
        {
            residentSource = getResidents ?? throw new ArgumentNullException(nameof(getResidents));
            roomTransformResolver = resolveRoom ?? throw new ArgumentNullException(nameof(resolveRoom));
            material = sharedMaterial;
            generatedHideFlags = hideFlags;
            populationModel = residentsModel;
            boardRoot = mapRoot;
            Synchronize();
        }

        public void Synchronize()
        {
            if (residentSource == null)
            {
                return;
            }

            var residents = residentSource() ?? Array.Empty<ResidentState>();
            var activeIds = new HashSet<string>(residents.Where(item => item != null).Select(item => item.id));
            foreach (var oldId in agents.Keys.Where(id => !activeIds.Contains(id)).ToArray())
            {
                var oldAgent = agents[oldId];
                if (oldAgent != null)
                {
                    oldAgent.Clicked -= HandleAgentClicked;
                    oldAgent.WorkCommuteMoved -= HandleWorkCommuteMoved;
                    if (Application.isPlaying)
                    {
                        Destroy(oldAgent.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(oldAgent.gameObject);
                    }
                }
                agents.Remove(oldId);
            }

            foreach (var resident in residents)
            {
                if (resident == null || string.IsNullOrEmpty(resident.id))
                {
                    continue;
                }
                var room = roomTransformResolver(resident.residenceId);
                if (room == null)
                {
                    continue;
                }
                // A small lower-right offset keeps the initial model clear of
                // the bed in both 1×1 and 2×1 residence variants.
                var home = room.position + new Vector3(0.48f, 0.30f, -0.40f);
                if (!agents.TryGetValue(resident.id, out var agent) || agent == null)
                {
                    var person = new GameObject($"Resident {resident.id}")
                    {
                        hideFlags = generatedHideFlags
                    };
                    person.transform.SetParent(transform, false);
                    agent = person.AddComponent<CitizenDemoAgent>();
                    var number = int.TryParse(resident.id.TrimStart('R'), out var parsed) ? parsed : 0;
                    agent.Initialize(material, home, HomeRoamHalfExtents,
                        generatedHideFlags, 149 + number * 17);
                    agent.Clicked += HandleAgentClicked;
                    agent.WorkCommuteMoved += HandleWorkCommuteMoved;
                    agents[resident.id] = agent;
                }
                else
                {
                    agent.MoveHome(home, HomeRoamHalfExtents);
                }

                if (populationModel != null && boardRoot != null &&
                    populationModel.TryGetRoute(resident.id, out _,
                        out var officeId, out var foodShopId))
                {
                    var navigation = populationModel.NavigationMap;
                    agent.SetDailyRoute(resident.residenceId, officeId, foodShopId,
                        (origin, destination) => BuildRoute(navigation, origin, destination));
                }
                else
                {
                    agent.SetDailyRoute(resident.residenceId, string.Empty, string.Empty, null);
                }
                if (populationModel != null)
                    agent.SetCycleLegs(populationModel.PreviewRouteLegs(resident.id));
            }
        }

        public bool TryBuildActivityRoute(string residentId, out IReadOnlyList<Vector3> route)
        {
            route = Array.Empty<Vector3>();
            if (populationModel == null || boardRoot == null ||
                !populationModel.TryGetRoute(residentId, out var residenceId,
                    out var officeId, out var foodShopId))
            {
                return false;
            }

            var residence = roomTransformResolver(residenceId);
            if (residence == null)
            {
                return false;
            }

            var home = residence.position + new Vector3(0.48f, 0.30f, -0.40f);
            var navigation = populationModel.NavigationMap;
            var toOffice = BuildRoute(navigation, home, officeId);
            if (toOffice.Count == 0)
            {
                return false;
            }
            var toFoodShop = BuildRoute(navigation,
                toOffice[toOffice.Count - 1], foodShopId);
            if (toFoodShop.Count == 0)
            {
                return false;
            }
            var toHome = BuildRoute(navigation,
                toFoodShop[toFoodShop.Count - 1], residenceId);
            if (toHome.Count == 0)
            {
                return false;
            }

            var points = new List<Vector3> { home };
            points.AddRange(toOffice);
            points.AddRange(toFoodShop);
            points.AddRange(toHome);
            route = points;
            return true;
        }

        private IReadOnlyList<Vector3> BuildRoute(
            RoomNavigationMap navigation,
            Vector3 origin,
            string destinationRoomId)
        {
            var destination = roomTransformResolver(destinationRoomId);
            if (navigation == null || destination == null)
            {
                return Array.Empty<Vector3>();
            }

            var localOrigin = boardRoot.InverseTransformPoint(origin);
            if (!navigation.TryFindRoomContaining(
                    new Vector2(localOrigin.x, localOrigin.z), out var originRoomId) ||
                !navigation.TryFindRoute(originRoomId, destinationRoomId, out var rooms))
            {
                return Array.Empty<Vector3>();
            }

            var points = new List<Vector3>();
            if (rooms.Count > 1 && IsRoadRoom(rooms[0]))
            {
                var start = roomTransformResolver(rooms[0]);
                if (start != null) points.Add(RoadCenter(start, origin.y));
            }
            for (var index = 0; index < rooms.Count - 1; index++)
            {
                if (!navigation.TryGetConnectionPoint(rooms[index], rooms[index + 1],
                        out var connection))
                {
                    return Array.Empty<Vector3>();
                }

                var doorway = boardRoot.TransformPoint(
                    new Vector3(connection.x, localOrigin.y, connection.y));
                var from = roomTransformResolver(rooms[index]);
                var to = roomTransformResolver(rooms[index + 1]);
                if (from == null || to == null)
                {
                    return Array.Empty<Vector3>();
                }

                // Pass through the shared opening, with a small floor-side
                // waypoint on either side instead of cutting across a wall.
                points.Add(InsideDoorway(doorway, from.position));
                points.Add(doorway);
                points.Add(InsideDoorway(doorway, to.position));
                if (IsRoadRoom(rooms[index + 1]))
                    points.Add(RoadCenter(to, doorway.y));
            }

            var offset = destinationRoomId.StartsWith("residence-", StringComparison.Ordinal)
                ? new Vector3(0.48f, 0.30f, -0.40f)
                : new Vector3(0.34f, 0.30f, -0.28f);
            points.Add(destination.position + offset);
            return points;
        }

        private static bool IsRoadRoom(string roomId) => HumanRoadLayout.HasVisibleRoad(
            RoomLayoutData.All.FirstOrDefault(spec => spec.Id == roomId));

        private static Vector3 RoadCenter(Transform room, float height) =>
            new Vector3(room.position.x, height, room.position.z);

        private static Vector3 InsideDoorway(Vector3 doorway, Vector3 roomCenter)
        {
            var inward = roomCenter - doorway;
            inward.y = 0f;
            return doorway + inward.normalized * 0.26f;
        }

        private void HandleAgentClicked(CitizenDemoAgent selected)
        {
            TrySelectAgent(selected);
        }

        private void HandleWorkCommuteMoved(CitizenDemoAgent selected, Vector3 from, Vector3 to)
        {
            foreach (var pair in agents)
            {
                if (pair.Value == selected)
                {
                    WorkerCommuteMoved?.Invoke(pair.Key, from, to);
                    return;
                }
            }
        }

        public bool TrySelectAgent(CitizenDemoAgent selected)
        {
            if (selected == null)
            {
                return false;
            }
            foreach (var pair in agents)
            {
                if (pair.Value == selected)
                {
                    ResidentClicked?.Invoke(pair.Key, selected);
                    return true;
                }
            }
            return false;
        }

        private void OnDestroy()
        {
            foreach (var agent in agents.Values)
            {
                if (agent != null)
                {
                    agent.Clicked -= HandleAgentClicked;
                    agent.WorkCommuteMoved -= HandleWorkCommuteMoved;
                }
            }
        }
    }
}
