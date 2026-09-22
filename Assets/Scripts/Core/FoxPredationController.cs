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
        private GameRuntimeController runtime;
        private AnimalNeedsController needs;
        private AnimalMortalityController mortality;
        private AnimalNavigationCoordinator navigation;
        private GarageTrafficController traffic;
        private Transform mapRoot;
        private float evaluationTimer;

        public void Initialize(
            GameRuntimeController runtimeController,
            AnimalNeedsController needsController,
            AnimalMortalityController mortalityController,
            AnimalNavigationCoordinator navigationCoordinator,
            GarageTrafficController trafficController,
            Transform boardRoot,
            IEnumerable<FoxDemoAgent> foxAgents,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents)
        {
            runtime = runtimeController;
            needs = needsController;
            mortality = mortalityController;
            navigation = navigationCoordinator;
            traffic = trafficController;
            mapRoot = boardRoot;
            foxes.AddRange(foxAgents ?? Array.Empty<FoxDemoAgent>());
            foreach (var fox in foxes)
            {
                huntCooldowns[fox] = 1.5f;
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
                    isSafe = () => !capturedPigeon.IsAlive || capturedPigeon.IsFlying,
                    beginDefence = () => capturedPigeon.BeginPredatorEscape(ClosestFoxPosition(capturedPigeon.transform.position)),
                    kill = () =>
                    {
                        if (!capturedPigeon.IsAlive || capturedPigeon.IsFlying)
                        {
                            return;
                        }
                        mortality.PreparePigeonDeath(capturedPigeon, AnimalDeathCause.Other);
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
                    isSafe = () => !capturedSquirrel.IsAlive || capturedSquirrel.IsPredatorSafe,
                    beginDefence = () => capturedSquirrel.BeginPredatorEscape(),
                    kill = () => capturedSquirrel.Vitality?.Kill(AnimalDeathCause.Other)
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
                    isSafe = () => !capturedHedgehog.IsAlive || capturedHedgehog.IsPredatorSafe,
                    beginDefence = () => capturedHedgehog.BeginPredatorDefence(),
                    kill = () => capturedHedgehog.Vitality?.Kill(AnimalDeathCause.Other)
                });
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
                huntCooldowns[fox] = Mathf.Max(0f, huntCooldowns[fox] - Time.deltaTime);
            }
            evaluationTimer -= Time.deltaTime;
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
                TryBeginHunt(fox);
            }
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
            if (target == null || !TryBuildRoute(fox.transform.position, target.agent.AgentTransform.position, out var route, out var hazard, out var fatal))
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

        private Vector3 ClosestFoxPosition(Vector3 preyPosition)
        {
            return foxes.Where(item => item != null)
                .OrderBy(item => (item.transform.position - preyPosition).sqrMagnitude)
                .Select(item => item.transform.position)
                .FirstOrDefault();
        }
    }
}
