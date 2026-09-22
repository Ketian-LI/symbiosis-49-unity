using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Animals
{
    public enum FoxDemoState
    {
        Rest,
        Trot,
        Sniff,
        Alert
    }

    public sealed class FoxDemoAgent : MonoBehaviour, IWildlifeLayoutAgent
    {
        private System.Random random;
        private FoxDemoVisual visual;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private FoxDemoState state;
        private float stateTime;
        private float stateDuration;
        private bool initialized;
        private bool relocating;
        private Vector3 relocationStart;
        private Vector3 relocationTarget;
        private float relocationDuration;
        private float relocationTime;
        private WildlifeVitality vitality;
        private bool activityEnabled;
        private readonly List<Vector3> huntWaypoints = new();
        private int huntWaypointIndex;
        private Transform huntTarget;
        private Func<bool> huntTargetIsSafe;
        private Action huntCaught;
        private bool hunting;
        private int huntHazardWaypoint = -1;
        private bool huntHazardFatal;
        private bool huntHazardResolved;
        private float huntTrafficWaitRemaining = -1f;

        public WildlifeSpecies Species => WildlifeSpecies.Fox;
        public Transform AgentTransform => transform;
        public FoxDemoState State => state;
        public bool IsAlive => vitality == null || vitality.IsAlive;
        public WildlifeVitality Vitality => vitality;
        public bool IsHunting => hunting;

        public void SetActivityEnabled(bool value)
        {
            activityEnabled = value;
            if (!value)
            {
                FinishHunt(false);
                BeginState(FoxDemoState.Rest, 999f);
            }
        }

        public void Initialize(
            Material material,
            Vector3 initialPosition,
            Vector2 movementHalfExtents,
            HideFlags hideFlags,
            int randomSeed = 449)
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            random = new System.Random(randomSeed);
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            habitatHalfExtents = movementHalfExtents;
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, randomSeed % 360, 0f);
            visual = gameObject.AddComponent<FoxDemoVisual>();
            visual.Initialize(material, hideFlags);
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.28f, 0f);
            collider.size = new Vector3(0.92f, 0.62f, 0.42f);
            vitality = gameObject.AddComponent<WildlifeVitality>();
            vitality.Initialize(Species, this, initialPosition, material, hideFlags);
            BeginState(FoxDemoState.Rest, 1.6f);
        }

        public void RelocateTo(Vector3 worldPosition, float durationSeconds)
        {
            var delta = worldPosition - transform.position;
            spawnPosition += delta;
            habitatCenter += delta;
            targetPosition += delta;
            relocationStart = transform.position;
            relocationTarget = worldPosition;
            relocationDuration = Mathf.Max(0.05f, durationSeconds);
            relocationTime = 0f;
            relocating = true;
        }

        public bool BeginHunt(
            IReadOnlyList<Vector3> waypoints,
            Transform target,
            Func<bool> targetIsSafe,
            Action onCaught,
            int hazardWaypoint = -1,
            bool fatalTrafficCrossing = false)
        {
            if (!initialized || !IsAlive || !activityEnabled || hunting ||
                target == null || waypoints == null || waypoints.Count == 0)
            {
                return false;
            }

            huntWaypoints.Clear();
            huntWaypoints.AddRange(waypoints);
            huntWaypointIndex = 0;
            huntTarget = target;
            huntTargetIsSafe = targetIsSafe;
            huntCaught = onCaught;
            huntHazardWaypoint = hazardWaypoint;
            huntHazardFatal = fatalTrafficCrossing;
            huntHazardResolved = false;
            huntTrafficWaitRemaining = -1f;
            hunting = true;
            BeginState(FoxDemoState.Trot, 999f);
            return true;
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }
            if (relocating)
            {
                relocationTime += Time.deltaTime;
                var progress = Mathf.Clamp01(relocationTime / relocationDuration);
                transform.position = Vector3.Lerp(relocationStart, relocationTarget, Mathf.SmoothStep(0f, 1f, progress));
                visual.ApplyPose(state, stateTime);
                relocating = progress < 1f;
                return;
            }

            stateTime += Time.deltaTime;
            if (!IsAlive)
            {
                FinishHunt(false);
                return;
            }
            if (hunting)
            {
                UpdateHunt(Time.deltaTime);
                visual.ApplyPose(state, stateTime);
                return;
            }
            if (!activityEnabled)
            {
                visual.ApplyPose(FoxDemoState.Rest, stateTime);
                return;
            }
            if (state == FoxDemoState.Trot)
            {
                var toTarget = targetPosition - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.025f)
                {
                    BeginState(FoxDemoState.Sniff, RandomRange(1.1f, 1.8f));
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                        Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 0.72f);
                }
            }
            visual.ApplyPose(state, stateTime);
            if (stateTime >= stateDuration)
            {
                ChooseNextState();
            }
        }

        private void UpdateHunt(float deltaTime)
        {
            if (huntTarget == null || huntTargetIsSafe != null && huntTargetIsSafe())
            {
                FinishHunt(false);
                return;
            }

            var destination = huntWaypointIndex < huntWaypoints.Count - 1
                ? huntWaypoints[huntWaypointIndex]
                : huntTarget.position;
            destination.y = spawnPosition.y;
            var current = transform.position;
            current.y = spawnPosition.y;
            var toTarget = destination - current;
            if (toTarget.sqrMagnitude <= 0.09f)
            {
                if (!huntHazardResolved && huntWaypointIndex == huntHazardWaypoint)
                {
                    if (huntTrafficWaitRemaining < 0f)
                    {
                        huntTrafficWaitRemaining = 1f;
                        BeginState(FoxDemoState.Alert, 999f);
                        return;
                    }
                    huntTrafficWaitRemaining -= deltaTime;
                    if (huntTrafficWaitRemaining > 0f)
                    {
                        return;
                    }
                    huntHazardResolved = true;
                    if (huntHazardFatal)
                    {
                        vitality?.Kill(AnimalDeathCause.Traffic);
                        FinishHunt(false);
                        return;
                    }
                    BeginState(FoxDemoState.Trot, 999f);
                }
                if (huntWaypointIndex < huntWaypoints.Count - 1)
                {
                    huntWaypointIndex++;
                    return;
                }

                FinishHunt(true);
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 9f);
            transform.position = Vector3.MoveTowards(current, destination, deltaTime * 1.05f);
        }

        private void FinishHunt(bool caught)
        {
            if (!hunting)
            {
                return;
            }

            var callback = huntCaught;
            hunting = false;
            huntWaypoints.Clear();
            huntTarget = null;
            huntTargetIsSafe = null;
            huntCaught = null;
            huntHazardWaypoint = -1;
            huntHazardResolved = false;
            huntTrafficWaitRemaining = -1f;
            BeginState(caught ? FoxDemoState.Sniff : FoxDemoState.Alert, caught ? 1.4f : 0.9f);
            if (caught)
            {
                callback?.Invoke();
            }
        }

        private void ChooseNextState()
        {
            if (state == FoxDemoState.Trot)
            {
                BeginState(FoxDemoState.Sniff, RandomRange(1f, 1.7f));
                return;
            }
            if (state == FoxDemoState.Sniff || state == FoxDemoState.Alert)
            {
                BeginState(FoxDemoState.Rest, RandomRange(1.2f, 2.2f));
                return;
            }
            if (random.NextDouble() < 0.55)
            {
                targetPosition = habitatCenter + new Vector3(
                    RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x),
                    0f,
                    RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y));
                targetPosition.y = spawnPosition.y;
                BeginState(FoxDemoState.Trot, RandomRange(1.5f, 2.8f));
            }
            else
            {
                BeginState(FoxDemoState.Alert, RandomRange(0.9f, 1.5f));
            }
        }

        private void BeginState(FoxDemoState nextState, float duration)
        {
            state = nextState;
            stateTime = 0f;
            stateDuration = Mathf.Max(0.01f, duration);
            visual?.ApplyPose(state, 0f);
        }

        private float RandomRange(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }
    }
}
