using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class FoxPredationController : MonoBehaviour
    {
        private sealed class PreyTarget
        {
            public string id;
            public IWildlifeLayoutAgent agent;
            public Func<bool> isAlive;
            public Func<bool> isSafe;
            public Action beginDefence;
            public Action kill;
        }

        private readonly List<FoxDemoAgent> foxes = new();
        private readonly List<PreyTarget> prey = new();
        private readonly Dictionary<FoxDemoAgent, float> huntCooldowns = new();
        private readonly Dictionary<FoxDemoAgent, Transform> scavengingTargets = new();
        private GameRuntimeController runtime;
        private AnimalNeedsController needs;
        private NaturalFoodController naturalFood;
        private AnimalMortalityController mortality;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private HedgehogForagingController hedgehogForaging;
        private Transform mapRoot;
        private float evaluationTimer;

        public void Initialize(
            GameRuntimeController runtimeController,
            AnimalNeedsController needsController,
            NaturalFoodController foodController,
            AnimalMortalityController mortalityController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Transform boardRoot,
            IEnumerable<FoxDemoAgent> foxAgents,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            HedgehogForagingController hedgehogForagingController)
        {
            runtime = runtimeController;
            needs = needsController;
            naturalFood = foodController;
            mortality = mortalityController;
            navigation = navigationCoordinator;
            navigation.NavigationChanged += HandleNavigationChanged;
            traffic = trafficController;
            hedgehogForaging = hedgehogForagingController;
            mapRoot = boardRoot;
            foxes.AddRange(foxAgents ?? Array.Empty<FoxDemoAgent>());
            foreach (var fox in foxes)
            {
                huntCooldowns[fox] = 1.5f;
                var target = new GameObject($"{fox.name} Scavenging Target");
                target.transform.SetParent(transform, false);
                scavengingTargets[fox] = target.transform;
            }

            var pigeonIndex = 0;
            foreach (var pigeon in pigeonAgents ?? Array.Empty<PigeonDemoAgent>())
            {
                var capturedPigeon = pigeon;
                prey.Add(new PreyTarget
                {
                    id = $"pigeon-{++pigeonIndex:00}",
                    agent = capturedPigeon,
                    isAlive = () => capturedPigeon.IsAlive,
                    isSafe = () => !capturedPigeon.IsAlive || capturedPigeon.IsFlying ||
                                   IsInSharedParkRefuge(capturedPigeon.AgentTransform.position),
                    beginDefence = () => capturedPigeon.BeginPredatorEscape(ClosestFoxPosition(capturedPigeon.transform.position)),
                    kill = () =>
                    {
                        if (!capturedPigeon.IsAlive || capturedPigeon.IsFlying)
                        {
                            return;
                        }
                        mortality.PreparePigeonDeath(capturedPigeon, AnimalDeathCause.Predation);
                        capturedPigeon.Kill();
                    }
                });
            }

            var squirrelIndex = 0;
            foreach (var squirrel in squirrelAgents ?? Array.Empty<SquirrelDemoAgent>())
            {
                var capturedSquirrel = squirrel;
                prey.Add(new PreyTarget
                {
                    id = $"squirrel-{++squirrelIndex:00}",
                    agent = capturedSquirrel,
                    isAlive = () => capturedSquirrel.IsAlive,
                    isSafe = () => !capturedSquirrel.IsAlive || capturedSquirrel.IsPredatorSafe ||
                                   IsInSharedParkRefuge(capturedSquirrel.AgentTransform.position),
                    beginDefence = () => capturedSquirrel.BeginPredatorEscape(),
                    kill = () => capturedSquirrel.Vitality?.Kill(AnimalDeathCause.Predation)
                });
            }

            var hedgehogIndex = 0;
            foreach (var hedgehog in hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>())
            {
                var capturedHedgehog = hedgehog;
                prey.Add(new PreyTarget
                {
                    id = $"hedgehog-{++hedgehogIndex:00}",
                    agent = capturedHedgehog,
                    isAlive = () => capturedHedgehog.IsAlive,
                    isSafe = () => !capturedHedgehog.IsAlive || capturedHedgehog.IsPredatorSafe ||
                                   IsInSharedParkRefuge(capturedHedgehog.AgentTransform.position) ||
                                   hedgehogForaging != null && hedgehogForaging.IsCovered(capturedHedgehog.transform.position),
                    beginDefence = () => capturedHedgehog.BeginPredatorDefence(),
                    kill = () => capturedHedgehog.Vitality?.Kill(AnimalDeathCause.Predation)
                });
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
            foreach (var fox in foxes)
            {
                if (fox != null && fox.IsAlive && !fox.IsHunting)
                {
                    huntCooldowns[fox] = 0.35f;
                }
            }
        }

        private void Update()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused)
            {
                return;
            }

            foreach (var fox in foxes)
            {
                huntCooldowns[fox] = Mathf.Max(0f,
                    huntCooldowns[fox] - runtime.ActorPresentationDeltaTime);
            }
            evaluationTimer -= runtime.ActorPresentationDeltaTime;
            if (evaluationTimer > 0f)
            {
                return;
            }
            evaluationTimer = 0.35f;

            var activePhase = runtime.Clock.Phase == DayPhase.Dusk || runtime.Clock.Phase == DayPhase.Night;
            foreach (var fox in foxes)
            {
                if (fox == null || fox.IsHunting || huntCooldowns[fox] > 0f ||
                    !FoxPredationModel.CanStartHunt(fox.IsAlive, activePhase, needs.HungerDaysOf(fox)))
                {
                    continue;
                }
                if (!TryBeginScavenge(fox))
                    TryBeginHunt(fox);
            }
        }

        private bool TryBeginScavenge(FoxDemoAgent fox)
        {
            if (naturalFood?.Model == null || !scavengingTargets.TryGetValue(fox, out var target))
                return false;
            var startRoom = FindRoomId(fox.transform.position);
            var routeMap = navigation.NavigationMapFor(fox);
            foreach (var source in naturalFood.Model.Sources.Values
                         .Where(item => item.kind == NaturalFoodKind.DiscardedFood && item.portions > 0)
                         .OrderBy(item => item.roomId, StringComparer.Ordinal))
            {
                if (!naturalFood.TryGetWorldPosition(source.roomId, source.kind, out var position) ||
                    !HabitatFoodNetworkModel.CanReachSource(routeMap, startRoom,
                        WildlifeSpecies.Fox, source.roomId, out _) ||
                    !TryBuildRoute(fox, fox.transform.position, position,
                        out var route, out var hazard, out var fatal))
                    continue;

                var roomId = source.roomId;
                target.position = position;
                if (!fox.BeginHunt(route, target,
                        () => naturalFood.Model.PortionsIn(roomId, NaturalFoodKind.DiscardedFood) <= 0,
                        () =>
                        {
                            if (naturalFood.TryConsume(roomId, NaturalFoodKind.DiscardedFood))
                            {
                                needs.RegisterPredationMeal(fox);
                                huntCooldowns[fox] = 6f;
                            }
                            else
                                huntCooldowns[fox] = 1f;
                        },
                        hazard, fatal))
                    continue;

                huntCooldowns[fox] = 3f;
                return true;
            }
            return false;
        }

        private void TryBeginHunt(FoxDemoAgent fox)
        {
            var candidates = prey.Select(item => new FoxPreyCandidate(
                item.id,
                item.agent.Species,
                Vector3.Distance(fox.transform.position, item.agent.AgentTransform.position),
                item.isAlive(),
                !item.isSafe()));
            var selectedId = FoxPredationModel.SelectPrey(candidates);
            var target = prey.FirstOrDefault(item => item.id == selectedId);
            if (target == null || !TryBuildRoute(fox, fox.transform.position, target.agent.AgentTransform.position, out var route, out var hazard, out var fatal))
            {
                huntCooldowns[fox] = 1f;
                return;
            }

            var caught = false;
            if (!fox.BeginHunt(
                    route,
                    target.agent.AgentTransform,
                    target.isSafe,
                    () =>
                    {
                        if (target.isSafe())
                        {
                            return;
                        }
                        target.kill();
                        caught = !target.isAlive();
                        if (caught)
                        {
                            needs.RegisterPredationMeal(fox);
                        }
                        huntCooldowns[fox] = caught ? 6f : 2f;
                    },
                    hazard,
                    fatal))
            {
                huntCooldowns[fox] = 1f;
                return;
            }
            target.beginDefence();
            huntCooldowns[fox] = 3f;
        }

        private bool TryBuildRoute(
            FoxDemoAgent fox,
            Vector3 worldStart,
            Vector3 worldDestination,
            out IReadOnlyList<Vector3> waypoints,
            out int hazardWaypoint,
            out bool fatal)
        {
            waypoints = Array.Empty<Vector3>();
            hazardWaypoint = -1;
            fatal = false;
            var startRoom = FindRoomId(worldStart);
            var destinationRoom = FindRoomId(worldDestination);
            if (string.IsNullOrEmpty(startRoom) || string.IsNullOrEmpty(destinationRoom))
            {
                return false;
            }
            var routeMap = navigation.NavigationMapFor(fox);
            var plan = traffic.PlanRoute(startRoom, destinationRoom, routeMap);
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

        private bool IsInSharedParkRefuge(Vector3 worldPosition) =>
            FindRoomId(worldPosition) == "central-park";

        private Vector3 ClosestFoxPosition(Vector3 preyPosition)
        {
            return foxes.Where(item => item != null)
                .OrderBy(item => (item.transform.position - preyPosition).sqrMagnitude)
                .Select(item => item.transform.position)
                .FirstOrDefault();
        }
    }
}
