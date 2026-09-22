using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Animals
{
    public enum HedgehogDemoState
    {
        Idle,
        Waddle,
        Sniff,
        Curl
    }

    public sealed class HedgehogDemoAgent : MonoBehaviour, IWildlifeLayoutAgent
    {
        private System.Random random = new(349);

        private HedgehogDemoVisual visual;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private HedgehogDemoState state;
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
        private bool predatorDefence;
        private float predatorDefenceTime;
        private readonly List<Vector3> forageWaypoints = new();
        private int forageWaypointIndex;
        private Action forageArrival;
        private bool foraging;
        private int forageHazardWaypoint = -1;
        private bool forageHazardFatal;
        private bool forageHazardResolved;
        private float forageTrafficWaitRemaining = -1f;

        public Vector3 SpawnPosition => spawnPosition;
        public HedgehogDemoState State => state;
        public WildlifeSpecies Species => WildlifeSpecies.Hedgehog;
        public Transform AgentTransform => transform;
        public bool IsAlive => vitality == null || vitality.IsAlive;
        public WildlifeVitality Vitality => vitality;
        public bool IsPredatorSafe => predatorDefence && predatorDefenceTime >= 0.32f;
        public bool IsForaging => foraging;

        public void SetActivityEnabled(bool value)
        {
            activityEnabled = value;
            if (!value)
            {
                CancelForaging();
                BeginState(HedgehogDemoState.Idle, 999f);
            }
        }

        public void Initialize(Material sharedMaterial, Vector3 initialPosition, Vector2 movementHalfExtents, HideFlags hideFlags, int randomSeed = 349)
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
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);

            visual = gameObject.AddComponent<HedgehogDemoVisual>();
            visual.Initialize(sharedMaterial, hideFlags);

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.11f, 0f);
            collider.size = new Vector3(0.30f, 0.25f, 0.32f);
            vitality = gameObject.AddComponent<WildlifeVitality>();
            vitality.Initialize(Species, this, initialPosition, sharedMaterial, hideFlags);
            BeginState(HedgehogDemoState.Idle, 1.4f);
        }

        public void ApplyPreviewPose(
            HedgehogDemoState previewState,
            float poseTime,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            if (!initialized)
            {
                return;
            }

            transform.position = worldPosition;
            transform.rotation = worldRotation;
            visual.SetVisible(true);
            visual.ApplyPose(previewState, poseTime);
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

        public bool BeginPredatorDefence()
        {
            if (!IsAlive || predatorDefence)
            {
                return false;
            }
            predatorDefence = true;
            predatorDefenceTime = 0f;
            CancelForaging();
            BeginState(HedgehogDemoState.Curl, 2.5f);
            return true;
        }

        public bool BeginForaging(
            IReadOnlyList<Vector3> waypoints,
            Action onArrival,
            int hazardWaypoint = -1,
            bool fatalTrafficCrossing = false)
        {
            if (!IsAlive || !activityEnabled || predatorDefence || foraging ||
                waypoints == null || waypoints.Count == 0)
            {
                return false;
            }
            forageWaypoints.Clear();
            forageWaypoints.AddRange(waypoints);
            forageWaypointIndex = 0;
            forageArrival = onArrival;
            forageHazardWaypoint = hazardWaypoint;
            forageHazardFatal = fatalTrafficCrossing;
            forageHazardResolved = false;
            forageTrafficWaitRemaining = -1f;
            foraging = true;
            BeginState(HedgehogDemoState.Waddle, 999f);
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
                UpdateRelocation(Time.deltaTime);
                return;
            }

            stateTime += Time.deltaTime;
            if (predatorDefence)
            {
                predatorDefenceTime += Time.deltaTime;
                visual.ApplyPose(HedgehogDemoState.Curl, stateTime);
                if (predatorDefenceTime >= 2.5f)
                {
                    predatorDefence = false;
                    BeginState(HedgehogDemoState.Idle, 1.2f);
                }
                return;
            }
            if (foraging)
            {
                UpdateForaging(Time.deltaTime);
                visual.ApplyPose(state, stateTime);
                return;
            }
            if (!activityEnabled)
            {
                visual.ApplyPose(HedgehogDemoState.Idle, stateTime);
                return;
            }
            if (state == HedgehogDemoState.Waddle)
            {
                UpdateWaddle(Time.deltaTime);
            }

            visual.ApplyPose(state, stateTime);
            if (stateTime >= stateDuration)
            {
                ChooseNextState();
            }
        }

        private void UpdateForaging(float deltaTime)
        {
            var target = forageWaypoints[forageWaypointIndex];
            target.y = spawnPosition.y;
            var current = transform.position;
            current.y = spawnPosition.y;
            var toTarget = target - current;
            if (toTarget.sqrMagnitude <= 0.0225f)
            {
                if (!forageHazardResolved && forageWaypointIndex == forageHazardWaypoint)
                {
                    if (forageTrafficWaitRemaining < 0f)
                    {
                        forageTrafficWaitRemaining = 1f;
                        BeginState(HedgehogDemoState.Sniff, 999f);
                        return;
                    }
                    forageTrafficWaitRemaining -= deltaTime;
                    if (forageTrafficWaitRemaining > 0f)
                    {
                        return;
                    }
                    forageHazardResolved = true;
                    if (forageHazardFatal)
                    {
                        vitality?.Kill(AnimalDeathCause.Traffic);
                        CancelForaging();
                        return;
                    }
                    BeginState(HedgehogDemoState.Waddle, 999f);
                }

                forageWaypointIndex++;
                if (forageWaypointIndex < forageWaypoints.Count)
                {
                    return;
                }
                var callback = forageArrival;
                CancelForaging();
                BeginState(HedgehogDemoState.Sniff, 1.4f);
                callback?.Invoke();
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 7f);
            transform.position = Vector3.MoveTowards(current, target, deltaTime * 0.44f);
        }

        private void CancelForaging()
        {
            foraging = false;
            forageWaypoints.Clear();
            forageArrival = null;
            forageHazardWaypoint = -1;
            forageHazardResolved = false;
            forageTrafficWaitRemaining = -1f;
        }

        private void UpdateRelocation(float deltaTime)
        {
            relocationTime += deltaTime;
            var progress = Mathf.Clamp01(relocationTime / relocationDuration);
            transform.position = Vector3.Lerp(relocationStart, relocationTarget, Mathf.SmoothStep(0f, 1f, progress));
            visual.ApplyPose(state, stateTime);
            if (progress >= 1f)
            {
                relocating = false;
            }
        }

        private void UpdateWaddle(float deltaTime)
        {
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                BeginState(HedgehogDemoState.Sniff, RandomRange(1.1f, 1.8f));
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 6f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, deltaTime * 0.34f);
        }

        private void ChooseNextState()
        {
            switch (state)
            {
                case HedgehogDemoState.Waddle:
                    BeginState(HedgehogDemoState.Sniff, RandomRange(1.0f, 1.8f));
                    return;
                case HedgehogDemoState.Sniff:
                    BeginState(random.NextDouble() < 0.22 ? HedgehogDemoState.Curl : HedgehogDemoState.Idle, RandomRange(0.9f, 1.5f));
                    return;
                case HedgehogDemoState.Curl:
                    BeginState(HedgehogDemoState.Idle, RandomRange(1.1f, 2.0f));
                    return;
            }

            if (random.NextDouble() < 0.72)
            {
                targetPosition = new Vector3(
                    habitatCenter.x + RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x),
                    habitatCenter.y,
                    habitatCenter.z + RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y));
                BeginState(HedgehogDemoState.Waddle, RandomRange(1.8f, 3.2f));
            }
            else
            {
                BeginState(HedgehogDemoState.Sniff, RandomRange(1.0f, 1.7f));
            }
        }

        private void BeginState(HedgehogDemoState nextState, float duration)
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
