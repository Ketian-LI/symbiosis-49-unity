using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public enum SquirrelDemoState
    {
        Idle,
        Hop,
        Forage,
        Alert
    }

    public sealed class SquirrelDemoAgent : MonoBehaviour, IWildlifeLayoutAgent
    {
        private System.Random random = new(249);

        private SquirrelDemoVisual visual;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private SquirrelDemoState state;
        private float stateTime;
        private float stateDuration;
        private bool initialized;
        private bool relocating;
        private Vector3 relocationStart;
        private Vector3 relocationTarget;
        private float relocationDuration;
        private float relocationTime;
        private Material material;
        private HideFlags generatedHideFlags;
        private Transform cacheRoot;
        private readonly List<GameObject> cachePortionVisuals = new();
        private readonly List<Vector3> foodWaypoints = new();
        private readonly List<Vector3> cacheReturnWaypoints = new();
        private int foodWaypointIndex;
        private float foodResponseDelay;
        private Func<bool> claimFood;
        private bool foodMission;
        private bool returningToCache;
        private WildlifeVitality vitality;
        private int foodHazardWaypoint = -1;
        private int cacheHazardWaypoint = -1;
        private bool foodHazardFatal;
        private bool cacheHazardFatal;
        private bool hazardResolved;
        private float trafficWaitRemaining = -1f;
        private bool activityEnabled = true;
        private bool panicking;
        private float panicRemaining;
        private Vector3 panicTarget;
        private Vector3 postPanicDestination;
        private float predatorSafeRemaining;

        public Vector3 SpawnPosition => spawnPosition;
        public SquirrelDemoState State => state;
        public WildlifeSpecies Species => WildlifeSpecies.Squirrel;
        public Transform AgentTransform => transform;
        public bool IsRespondingToFood => foodMission;
        public int CachePortions { get; private set; }
        public Vector3 CachePosition => cacheRoot != null ? cacheRoot.position : spawnPosition;
        public bool CanStoreFood => CachePortions < 3;
        public bool IsAlive => vitality == null || vitality.IsAlive;
        public WildlifeVitality Vitality => vitality;
        public bool IsPredatorSafe => predatorSafeRemaining > 0f;

        public void Initialize(
            Material sharedMaterial,
            Vector3 initialPosition,
            Vector2 movementHalfExtents,
            HideFlags hideFlags,
            int randomSeed = 249)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            random = new System.Random(randomSeed);
            material = sharedMaterial;
            generatedHideFlags = hideFlags;
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            habitatHalfExtents = movementHalfExtents;
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);

            visual = gameObject.AddComponent<SquirrelDemoVisual>();
            visual.Initialize(sharedMaterial, hideFlags);

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.18f, 0f);
            collider.size = new Vector3(0.44f, 0.44f, 0.48f);
            vitality = gameObject.AddComponent<WildlifeVitality>();
            vitality.Initialize(Species, this, initialPosition, sharedMaterial, hideFlags);
            BeginState(SquirrelDemoState.Idle, 1.2f);
        }

        public void InitializeCache(Transform container, Vector3 worldPosition)
        {
            if (cacheRoot != null || container == null)
            {
                return;
            }

            var root = new GameObject("Fixed Squirrel Food Cache")
            {
                hideFlags = generatedHideFlags
            };
            root.transform.SetParent(container, true);
            root.transform.position = worldPosition;
            cacheRoot = root.transform;
            for (var index = 0; index < 3; index++)
            {
                var portion = UrbanVisualFactory.CreatePrimitive(
                    PrimitiveType.Sphere,
                    $"Cached Portion {index + 1}",
                    cacheRoot,
                    new Vector3((index - 1) * 0.12f, 0.08f, index % 2 == 0 ? 0.03f : -0.04f),
                    new Vector3(0.10f, 0.06f, 0.08f),
                    new Color(0.73f, 0.45f, 0.16f),
                    material,
                    false,
                    generatedHideFlags);
                cachePortionVisuals.Add(portion);
            }
            RefreshCacheVisuals();
        }

        public bool BeginFoodMission(
            IReadOnlyList<Vector3> routeToFood,
            IReadOnlyList<Vector3> routeToCache,
            float responseDelay,
            Func<bool> tryClaimFood,
            int routeToFoodHazardWaypoint = -1,
            bool routeToFoodFatal = false,
            int routeToCacheHazardWaypoint = -1,
            bool routeToCacheFatal = false)
        {
            if (!initialized || foodMission || !CanStoreFood ||
                routeToFood == null || routeToFood.Count == 0 ||
                routeToCache == null || routeToCache.Count == 0)
            {
                return false;
            }

            foodWaypoints.Clear();
            foodWaypoints.AddRange(routeToFood);
            cacheReturnWaypoints.Clear();
            cacheReturnWaypoints.AddRange(routeToCache);
            foodWaypointIndex = 0;
            foodResponseDelay = Mathf.Max(0f, responseDelay);
            claimFood = tryClaimFood;
            foodMission = true;
            returningToCache = false;
            foodHazardWaypoint = routeToFoodHazardWaypoint;
            cacheHazardWaypoint = routeToCacheHazardWaypoint;
            foodHazardFatal = routeToFoodFatal;
            cacheHazardFatal = routeToCacheFatal;
            hazardResolved = false;
            trafficWaitRemaining = -1f;
            BeginState(SquirrelDemoState.Hop, 999f);
            return true;
        }

        public void RestoreCache(int portions)
        {
            CachePortions = Mathf.Clamp(portions, 0, 3);
            RefreshCacheVisuals();
        }

        public int ExposeCache()
        {
            var exposed = CachePortions;
            CachePortions = 0;
            RefreshCacheVisuals();
            return exposed;
        }

        public bool TryConsumeCachedPortion()
        {
            if (CachePortions <= 0)
            {
                return false;
            }
            CachePortions--;
            RefreshCacheVisuals();
            return true;
        }

        public void SetActivityEnabled(bool value)
        {
            activityEnabled = value;
            if (!value && !foodMission)
            {
                BeginState(SquirrelDemoState.Idle, 999f);
            }
        }

        public void BeginPanic(float durationSeconds, Vector3 safeDestination)
        {
            if (!IsAlive)
            {
                return;
            }
            foodMission = false;
            claimFood = null;
            foodWaypoints.Clear();
            cacheReturnWaypoints.Clear();
            panicking = true;
            panicRemaining = Mathf.Max(0.1f, durationSeconds);
            postPanicDestination = safeDestination;
            ChoosePanicTarget();
            BeginState(SquirrelDemoState.Hop, 999f);
        }

        public bool BeginPredatorEscape()
        {
            if (!IsAlive || predatorSafeRemaining > 0f)
            {
                return false;
            }
            BeginPanic(0.85f, spawnPosition);
            return true;
        }

        public void ApplyPreviewPose(
            SquirrelDemoState previewState,
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
            predatorSafeRemaining = Mathf.Max(0f, predatorSafeRemaining - Time.deltaTime);
            if (panicking)
            {
                UpdatePanic(Time.deltaTime);
                visual.ApplyPose(SquirrelDemoState.Hop, stateTime);
                return;
            }
            if (foodMission)
            {
                UpdateFoodMission(Time.deltaTime);
                visual.ApplyPose(state, stateTime);
                return;
            }
            if (!activityEnabled)
            {
                visual.ApplyPose(SquirrelDemoState.Idle, stateTime);
                return;
            }
            if (state == SquirrelDemoState.Hop)
            {
                UpdateHop(Time.deltaTime);
            }

            visual.ApplyPose(state, stateTime);
            if (stateTime >= stateDuration)
            {
                ChooseNextState();
            }
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

        private void UpdateHop(float deltaTime)
        {
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.02f)
            {
                BeginState(SquirrelDemoState.Forage, RandomRange(1.0f, 1.7f));
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 8f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, deltaTime * 0.95f);
        }

        private void UpdateFoodMission(float deltaTime)
        {
            if (foodResponseDelay > 0f)
            {
                foodResponseDelay -= deltaTime;
                return;
            }

            var route = returningToCache ? cacheReturnWaypoints : foodWaypoints;
            var target = route[foodWaypointIndex];
            target.y = transform.position.y;
            var toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.025f)
            {
                var hazardWaypoint = returningToCache ? cacheHazardWaypoint : foodHazardWaypoint;
                var fatal = returningToCache ? cacheHazardFatal : foodHazardFatal;
                if (!hazardResolved && foodWaypointIndex == hazardWaypoint)
                {
                    if (trafficWaitRemaining < 0f)
                    {
                        trafficWaitRemaining = 1f;
                        BeginState(SquirrelDemoState.Alert, 999f);
                        return;
                    }
                    trafficWaitRemaining -= deltaTime;
                    if (trafficWaitRemaining > 0f)
                    {
                        return;
                    }
                    hazardResolved = true;
                    if (fatal)
                    {
                        foodMission = false;
                        claimFood = null;
                        foodWaypoints.Clear();
                        cacheReturnWaypoints.Clear();
                        vitality?.Kill(AnimalDeathCause.Traffic);
                        return;
                    }
                    BeginState(SquirrelDemoState.Hop, 999f);
                }

                foodWaypointIndex++;
                if (foodWaypointIndex < route.Count)
                {
                    return;
                }

                if (!returningToCache)
                {
                    var claimed = claimFood?.Invoke() ?? false;
                    claimFood = null;
                    if (!claimed)
                    {
                        FinishFoodMission(SquirrelDemoState.Forage);
                        return;
                    }

                    returningToCache = true;
                    foodWaypointIndex = 0;
                    hazardResolved = false;
                    trafficWaitRemaining = -1f;
                    return;
                }

                CachePortions = Mathf.Min(3, CachePortions + 1);
                RefreshCacheVisuals();
                FinishFoodMission(SquirrelDemoState.Idle);
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 8f);
            transform.position = Vector3.MoveTowards(transform.position, target, deltaTime * 0.95f);
        }

        private void UpdatePanic(float deltaTime)
        {
            panicRemaining -= deltaTime;
            var toTarget = panicTarget - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.02f)
            {
                ChoosePanicTarget();
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                    deltaTime * 12f);
                transform.position = Vector3.MoveTowards(transform.position, panicTarget, deltaTime * 1.25f);
            }
            if (panicRemaining <= 0f)
            {
                panicking = false;
                predatorSafeRemaining = 2.5f;
                RelocateTo(postPanicDestination, 0.8f);
                BeginState(SquirrelDemoState.Alert, 1.4f);
            }
        }

        private void ChoosePanicTarget()
        {
            panicTarget = transform.position + new Vector3(
                RandomRange(-0.72f, 0.72f),
                0f,
                RandomRange(-0.72f, 0.72f));
            panicTarget.y = transform.position.y;
        }

        private void FinishFoodMission(SquirrelDemoState nextState)
        {
            foodMission = false;
            returningToCache = false;
            foodWaypoints.Clear();
            cacheReturnWaypoints.Clear();
            foodHazardWaypoint = -1;
            cacheHazardWaypoint = -1;
            hazardResolved = false;
            trafficWaitRemaining = -1f;
            BeginState(nextState, nextState == SquirrelDemoState.Forage ? 1.1f : 1.4f);
        }

        private void RefreshCacheVisuals()
        {
            for (var index = 0; index < cachePortionVisuals.Count; index++)
            {
                cachePortionVisuals[index].SetActive(index < CachePortions);
            }
        }

        private void ChooseNextState()
        {
            switch (state)
            {
                case SquirrelDemoState.Hop:
                    BeginState(SquirrelDemoState.Forage, RandomRange(1.0f, 1.7f));
                    return;
                case SquirrelDemoState.Forage:
                    BeginState(random.NextDouble() < 0.28 ? SquirrelDemoState.Alert : SquirrelDemoState.Idle, RandomRange(0.8f, 1.5f));
                    return;
                case SquirrelDemoState.Alert:
                    BeginState(SquirrelDemoState.Idle, RandomRange(0.8f, 1.5f));
                    return;
            }

            if (random.NextDouble() < 0.7)
            {
                targetPosition = new Vector3(
                    habitatCenter.x + RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x),
                    habitatCenter.y,
                    habitatCenter.z + RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y));
                BeginState(SquirrelDemoState.Hop, RandomRange(1.2f, 2.5f));
            }
            else
            {
                BeginState(SquirrelDemoState.Alert, RandomRange(0.8f, 1.3f));
            }
        }

        private void BeginState(SquirrelDemoState nextState, float duration)
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
