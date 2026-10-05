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
        private GameRuntimeController runtime;
        private AnimalNavigationCoordinator navigationCoordinator;
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
        private bool carryingFood;
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

        public event Action<SquirrelDemoAgent> Clicked;

        public Vector3 SpawnPosition => spawnPosition;
        public SquirrelDemoState State => state;
        public WildlifeSpecies Species => WildlifeSpecies.Squirrel;
        public Transform AgentTransform => transform;
        public bool IsRespondingToFood => foodMission;
        public bool HasFoodInTransit => carryingFood;
        public bool CanResumeCacheReturn => carryingFood && !foodMission && !panicking && !relocating && IsAlive;
        public int CachePortions { get; private set; }
        public Vector3 CachePosition => cacheRoot != null ? cacheRoot.position : spawnPosition;
        public bool CanStoreFood => CachePortions < 3 && !carryingFood;
        public bool IsAlive => vitality == null || vitality.IsAlive;
        public WildlifeVitality Vitality => vitality;
        public bool IsPredatorSafe => predatorSafeRemaining > 0f;

        public void BindNavigation(AnimalNavigationCoordinator coordinator) =>
            navigationCoordinator = coordinator;

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
            runtime = FindFirstObjectByType<GameRuntimeController>();
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
            collider.size = new Vector3(0.56f, 0.48f, 0.58f);
            vitality = gameObject.AddComponent<WildlifeVitality>();
            vitality.Initialize(Species, this, initialPosition, sharedMaterial, hideFlags);
            BeginState(SquirrelDemoState.Idle, 1.2f);
        }

        private void OnMouseDown()
        {
            if (Application.isPlaying && IsAlive)
            {
                Clicked?.Invoke(this);
            }
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
            if (!initialized || !IsAlive || foodMission || !CanStoreFood ||
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

        public bool BeginCacheReturn(IReadOnlyList<Vector3> routeToCache,
            int hazardWaypoint = -1, bool fatalTrafficCrossing = false)
        {
            if (!initialized || !CanResumeCacheReturn ||
                routeToCache == null || routeToCache.Count == 0)
            {
                return false;
            }

            foodWaypoints.Clear();
            cacheReturnWaypoints.Clear();
            cacheReturnWaypoints.AddRange(routeToCache);
            foodWaypointIndex = 0;
            foodResponseDelay = 0f;
            claimFood = null;
            foodMission = true;
            returningToCache = true;
            foodHazardWaypoint = -1;
            cacheHazardWaypoint = hazardWaypoint;
            cacheHazardFatal = fatalTrafficCrossing;
            hazardResolved = false;
            trafficWaitRemaining = -1f;
            BeginState(SquirrelDemoState.Hop, 999f);
            return true;
        }

        public bool CancelFoodMissionForRouteChange()
        {
            if (!foodMission)
            {
                return false;
            }

            // Claiming already removed a portion from its source. Keep it with
            // the squirrel until a new path to the fixed cache is available.
            carryingFood |= returningToCache;
            FinishFoodMission(SquirrelDemoState.Alert);
            return true;
        }

        public void RestoreCache(int portions)
        {
            if (foodMission)
            {
                FinishFoodMission(SquirrelDemoState.Idle);
            }
            carryingFood = false;
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
            if (activityEnabled == value)
            {
                return;
            }
            activityEnabled = value;
            if (foodMission)
            {
                return;
            }
            if (value)
            {
                BeginState(SquirrelDemoState.Idle, RandomRange(0.35f, 0.75f));
            }
            else
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
            carryingFood |= foodMission && returningToCache;
            foodMission = false;
            returningToCache = false;
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
            ShiftHomeAnchor(delta);
            targetPosition += delta;
            relocationStart = transform.position;
            relocationTarget = worldPosition;
            relocationDuration = Mathf.Max(0.05f, durationSeconds);
            relocationTime = 0f;
            relocating = true;
        }

        public void ShiftHomeAnchor(Vector3 delta)
        {
            spawnPosition += delta;
            habitatCenter += delta;
        }

        public void ShiftCacheWithHome(Vector3 delta)
        {
            if (cacheRoot != null)
                cacheRoot.position += delta;
        }

        public void ResetForNewRun(Vector3 initialPosition, Vector3 initialCachePosition)
        {
            if (!initialized) return;
            relocating = false;
            panicking = false;
            panicRemaining = 0f;
            predatorSafeRemaining = 0f;
            foodMission = false;
            returningToCache = false;
            carryingFood = false;
            claimFood = null;
            foodWaypoints.Clear();
            cacheReturnWaypoints.Clear();
            foodHazardWaypoint = -1;
            cacheHazardWaypoint = -1;
            hazardResolved = false;
            trafficWaitRemaining = -1f;
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            targetPosition = initialPosition;
            if (cacheRoot != null) cacheRoot.position = initialCachePosition;
            CachePortions = 0;
            RefreshCacheVisuals();
            vitality?.ResetForNewRun(initialPosition);
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            BeginState(SquirrelDemoState.Idle, 1.2f);
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }

            var deltaTime = runtime != null ? runtime.ActorPresentationDeltaTime : Time.deltaTime;

            if (foodMission && navigationCoordinator != null)
            {
                // The getter detects dawn even when this agent updates before
                // a foraging controller. It invalidates old waypoints first.
                _ = navigationCoordinator.NavigationRevision;
            }

            if (relocating)
            {
                UpdateRelocation(deltaTime);
                return;
            }

            stateTime += deltaTime;
            predatorSafeRemaining = Mathf.Max(0f, predatorSafeRemaining - deltaTime);
            if (panicking)
            {
                UpdatePanic(deltaTime);
                visual.ApplyPose(SquirrelDemoState.Hop, stateTime);
                return;
            }
            if (foodMission)
            {
                UpdateFoodMission(deltaTime);
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
                UpdateHop(deltaTime);
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
                        returningToCache = false;
                        carryingFood = false;
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
                    carryingFood = true;
                    foodWaypointIndex = 0;
                    hazardResolved = false;
                    trafficWaitRemaining = -1f;
                    return;
                }

                CachePortions = Mathf.Min(3, CachePortions + 1);
                carryingFood = false;
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
            claimFood = null;
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

            if (random.NextDouble() < 0.78)
            {
                targetPosition = new Vector3(
                    habitatCenter.x + RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x),
                    habitatCenter.y,
                    habitatCenter.z + RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y));
                BeginState(SquirrelDemoState.Hop, RandomRange(1.3f, 2.6f));
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
