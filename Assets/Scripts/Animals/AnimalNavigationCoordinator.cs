using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class AnimalNavigationCoordinator : MonoBehaviour
    {
        private readonly List<IWildlifeLayoutAgent> agents = new();
        private readonly Dictionary<IWildlifeLayoutAgent, string> wildlifeHomes = new();
        private readonly Dictionary<string, Vector3> previousRoomCenters = new();
        private readonly Dictionary<IWildlifeLayoutAgent, Vector3> initialSpawns = new();
        private readonly Dictionary<SquirrelDemoAgent, Vector3> initialCaches = new();

        private RoomLayoutEditorController layoutEditor;
        private Transform mapRoot;
        private float cellSize;
        private Func<string, Transform> roomTransformResolver;
        private GameRuntimeController runtime;
        private RoomNavigationMap navigationMap;
        private RoomNavigationMap hungryNavigationMap;
        private AnimalNeedsController needs;
        private int navigationDay;
        public event Action NavigationChanged;

        public RoomNavigationMap NavigationMap
        {
            get { RefreshDay(); return navigationMap; }
        }
        public void BindNeeds(AnimalNeedsController controller) => needs = controller;

        public RoomNavigationMap NavigationMapFor(IWildlifeLayoutAgent agent)
        {
            RefreshDay();
            return RoomNavigationMap.ForWildlifeHunger(
                navigationMap, hungryNavigationMap,
                agent != null && needs != null ? needs.HungerDaysOf(agent) : 0);
        }
        public int NavigationRevision
        {
            get { RefreshDay(); return navigationRevision; }
        }

        public bool TryGetCurrentRoomCenter(Vector3 worldPosition, out Vector3 center)
        {
            center = default;
            if (mapRoot == null || roomTransformResolver == null || NavigationMap == null)
                return false;
            var local = mapRoot.InverseTransformPoint(worldPosition);
            if (!navigationMap.TryFindRoomContaining(new Vector2(local.x, local.z), out var roomId))
                return false;
            var room = roomTransformResolver(roomId);
            if (room == null) return false;
            center = room.position;
            return true;
        }
        private int navigationRevision;

        public void Initialize(
            IEnumerable<IWildlifeLayoutAgent> wildlifeAgents,
            RoomLayoutEditorController editor,
            Transform boardRoot,
            float gridCellSize,
            IReadOnlyDictionary<PigeonDemoAgent, string> pigeonHomeRooms = null,
            Func<string, Transform> resolveRoom = null,
            GameRuntimeController runtimeController = null,
            IReadOnlyDictionary<IWildlifeLayoutAgent, string> allWildlifeHomeRooms = null)
        {
            agents.AddRange(wildlifeAgents);
            foreach (var agent in agents)
            {
                switch (agent)
                {
                    case PigeonDemoAgent pigeon:
                        initialSpawns[pigeon] = pigeon.SpawnPosition;
                        break;
                    case SquirrelDemoAgent squirrel:
                        initialSpawns[squirrel] = squirrel.SpawnPosition;
                        initialCaches[squirrel] = squirrel.CachePosition;
                        squirrel.BindNavigation(this);
                        break;
                    case HedgehogDemoAgent hedgehog:
                        initialSpawns[hedgehog] = hedgehog.SpawnPosition;
                        hedgehog.BindNavigation(this);
                        break;
                    case FoxDemoAgent fox:
                        initialSpawns[fox] = fox.SpawnPosition;
                        fox.BindNavigation(this);
                        break;
                }
            }
            layoutEditor = editor;
            mapRoot = boardRoot;
            cellSize = gridCellSize;
            roomTransformResolver = resolveRoom;
            runtime = runtimeController;
            if (pigeonHomeRooms != null)
            {
                foreach (var pair in pigeonHomeRooms)
                {
                    wildlifeHomes[pair.Key] = pair.Value;
                }
            }
            if (allWildlifeHomeRooms != null)
            {
                foreach (var pair in allWildlifeHomeRooms)
                    wildlifeHomes[pair.Key] = pair.Value;
            }
            layoutEditor.LayoutConfirmed += HandleLayoutConfirmed;
            layoutEditor.LayoutRestored += HandleLayoutConfirmed;
            RebuildNavigation(false);
            foreach (var roomSpec in RoomLayoutData.All)
            {
                var roomId = roomSpec.Id;
                var room = roomTransformResolver?.Invoke(roomId);
                if (room != null)
                {
                    previousRoomCenters[roomId] = room.position;
                }
            }
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= HandleLayoutConfirmed;
                layoutEditor.LayoutRestored -= HandleLayoutConfirmed;
            }
        }

        public void ResetWildlifeForNewRun()
        {
            foreach (var pair in initialSpawns)
            {
                switch (pair.Key)
                {
                    case PigeonDemoAgent pigeon:
                        pigeon.ResetForNewRun(pair.Value);
                        break;
                    case SquirrelDemoAgent squirrel:
                        squirrel.ResetForNewRun(pair.Value,
                            initialCaches.TryGetValue(squirrel, out var cache) ? cache : pair.Value);
                        break;
                    case HedgehogDemoAgent hedgehog:
                        hedgehog.ResetForNewRun(pair.Value);
                        break;
                    case FoxDemoAgent fox:
                        fox.ResetForNewRun(pair.Value);
                        break;
                }
            }
        }

        private void HandleLayoutConfirmed()
        {
            var movedAnimals = new HashSet<IWildlifeLayoutAgent>();
            var roomMovements = new Dictionary<string, Vector3>();
            foreach (var roomSpec in RoomLayoutData.All)
            {
                var roomId = roomSpec.Id;
                var room = roomTransformResolver?.Invoke(roomId);
                if (room == null) continue;
                roomMovements[roomId] = previousRoomCenters.TryGetValue(roomId, out var oldCenter)
                    ? room.position - oldCenter : Vector3.zero;
                previousRoomCenters[roomId] = room.position;
            }
            foreach (var pair in wildlifeHomes)
            {
                var agent = pair.Key;
                if (agent?.AgentTransform == null)
                    continue;
                var homeDelta = roomMovements.TryGetValue(pair.Value, out var shift)
                    ? shift : Vector3.zero;
                var currentLocal = mapRoot.InverseTransformPoint(agent.AgentTransform.position);
                var currentRoomId = navigationMap != null && navigationMap.TryFindRoomContaining(
                    new Vector2(currentLocal.x, currentLocal.z), out var occupiedRoom)
                    ? occupiedRoom : pair.Value;
                var positionDelta = roomMovements.TryGetValue(currentRoomId, out shift)
                    ? shift : Vector3.zero;

                // An animal follows the physical room it is standing in. Its
                // home/roaming anchor and squirrel cache follow the home room,
                // which can be somewhere else during a food trip.
                if (positionDelta.sqrMagnitude > 0.0001f)
                {
                    agent.RelocateTo(agent.AgentTransform.position + positionDelta, 0.45f);
                    movedAnimals.Add(agent);
                }
                var remainingHomeDelta = homeDelta - positionDelta;
                if (remainingHomeDelta.sqrMagnitude > 0.0001f)
                    agent.ShiftHomeAnchor(remainingHomeDelta);
                if (agent is SquirrelDemoAgent squirrel && homeDelta.sqrMagnitude > 0.0001f)
                    squirrel.ShiftCacheWithHome(homeDelta);
            }
            RebuildNavigation(true, movedAnimals);
        }

        private void RebuildNavigation(bool correctAnimalPositions,
            HashSet<IWildlifeLayoutAgent> alreadyRelocating = null)
        {
            var previousNavigation = navigationMap;
            navigationDay = runtime?.Clock.DayNumber ?? 1;
            navigationMap = new RoomNavigationMap(
                layoutEditor.ExportLayout(),
                RoomLayoutData.All,
                cellSize, true, navigationDay);
            hungryNavigationMap = new RoomNavigationMap(
                layoutEditor.ExportLayout(), RoomLayoutData.All, cellSize,
                animalPassagesOnly: true, animalDayNumber: navigationDay,
                hungryWildlifeMayUsePedestrianDoors: true);
            navigationRevision++;

            if (correctAnimalPositions)
            {
                foreach (var agent in agents)
                {
                    if (agent?.AgentTransform == null ||
                        alreadyRelocating != null && alreadyRelocating.Contains(agent))
                    {
                        continue;
                    }

                    var transform = agent.AgentTransform;
                    var local = mapRoot.InverseTransformPoint(transform.position);
                    var legal = navigationMap.FindNearestLegalFloorPoint(
                        new Vector2(local.x, local.z),
                        ClearanceFor(agent.Species));
                    var correctedLocal = new Vector3(legal.x, local.y, legal.y);
                    if ((correctedLocal - local).sqrMagnitude < 0.0025f)
                    {
                        continue;
                    }

                    agent.RelocateTo(mapRoot.TransformPoint(correctedLocal), 0.45f);
                }
            }

            // An opening cannot invalidate existing waypoints. A closed edge
            // can, and a committed layout may also move their world positions.
            if (!correctAnimalPositions && !HasRemovedConnection(previousNavigation, navigationMap))
            {
                return;
            }

            foreach (var agent in agents)
            {
                switch (agent)
                {
                    case PigeonDemoAgent pigeon:
                        pigeon.CancelFoodMissionForRouteChange();
                        break;
                    case SquirrelDemoAgent squirrel:
                        squirrel.CancelFoodMissionForRouteChange();
                        break;
                    case HedgehogDemoAgent hedgehog:
                        hedgehog.CancelForagingForRouteChange();
                        break;
                    case FoxDemoAgent fox:
                        fox.CancelHuntForRouteChange();
                        break;
                }
            }
            NavigationChanged?.Invoke();
        }

        private static bool HasRemovedConnection(RoomNavigationMap previous,
            RoomNavigationMap current)
        {
            if (previous == null)
            {
                return true;
            }
            foreach (var room in RoomLayoutData.All)
            {
                var available = new HashSet<string>(current.NeighboursOf(room.Id));
                foreach (var neighbour in previous.NeighboursOf(room.Id))
                {
                    if (!available.Contains(neighbour))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RefreshDay()
        {
            if (layoutEditor != null && runtime != null &&
                navigationDay != runtime.Clock.DayNumber)
            {
                RebuildNavigation(false);
            }
        }

        private static float ClearanceFor(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => 0.20f,
                WildlifeSpecies.Squirrel => 0.24f,
                WildlifeSpecies.Hedgehog => 0.18f,
                WildlifeSpecies.Fox => 0.38f,
                _ => 0.24f
            };
        }
    }
}
