using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.People
{
    public enum CitizenDemoState
    {
        Idle,
        Walk,
        Observe
    }

    public sealed class CitizenDemoAgent : MonoBehaviour
    {
        private System.Random random;

        private CitizenDemoVisual visual;
        private WorldStatusPips commutePips;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private CitizenDemoState state;
        private float stateTime;
        private float stateDuration;
        private bool initialized;
        private GameRuntimeController runtime;
        private Func<Vector3, string, IReadOnlyList<Vector3>> routeBuilder;
        private string homeRoomId;
        private string officeRoomId;
        private string foodShopRoomId;
        private string currentDestinationRoomId;
        private IReadOnlyList<Vector3> tripWaypoints;
        private int tripWaypointIndex;
        private ResidentRouteLegs cycleLegs;
        private int presentationDay;
        private bool reachedWork;
        private bool reachedMeal;
        private bool reachedHome;

        private const float CommuteSpeed = 1.85f;

        public Vector3 SpawnPosition => spawnPosition;
        public CitizenDemoState State => state;
        public string CurrentDestinationRoomId => currentDestinationRoomId;
        public bool IsCommuting => tripWaypoints != null;
        public event Action<CitizenDemoAgent> Clicked;
        public event Action<CitizenDemoAgent, Vector3, Vector3> WorkCommuteMoved;

        public void Initialize(
            Material sharedMaterial,
            Vector3 initialPosition,
            Vector2 movementHalfExtents,
            HideFlags hideFlags,
            int randomSeed = 149)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            runtime = FindFirstObjectByType<GameRuntimeController>();
            presentationDay = runtime?.Clock.DayNumber ?? 1;
            random = new System.Random(randomSeed);
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            habitatHalfExtents = movementHalfExtents;
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            visual = gameObject.AddComponent<CitizenDemoVisual>();
            visual.Initialize(sharedMaterial, hideFlags);
            commutePips = gameObject.AddComponent<WorldStatusPips>();
            commutePips.Initialize(WorldScaleStandards.CitizenVisualHeight + 0.09f,
                0.15f, hideFlags, emphasize: true);
            commutePips.SetCommuteProgress(default, false, false, false);
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.48f, 0f);
            collider.radius = 0.20f;
            collider.height = 0.95f;
            BeginState(CitizenDemoState.Idle, 1.4f);
        }

        public void MoveHome(Vector3 worldPosition, Vector2 movementHalfExtents)
        {
            if (!initialized ||
                (spawnPosition - worldPosition).sqrMagnitude < 0.0001f)
            {
                return;
            }

            spawnPosition = worldPosition;
            habitatCenter = worldPosition;
            habitatHalfExtents = movementHalfExtents;
            targetPosition = worldPosition;
            transform.position = worldPosition;
            tripWaypoints = null;
            currentDestinationRoomId = string.Empty;
            BeginState(CitizenDemoState.Idle, 1.0f);
        }

        public void SetDailyRoute(
            string residenceId,
            string workRoomId,
            string mealRoomId,
            Func<Vector3, string, IReadOnlyList<Vector3>> buildRoute)
        {
            homeRoomId = residenceId;
            officeRoomId = workRoomId;
            foodShopRoomId = mealRoomId;
            routeBuilder = buildRoute;
            tripWaypoints = null;
            currentDestinationRoomId = string.Empty;
        }

        public void SetCycleLegs(ResidentRouteLegs legs)
        {
            cycleLegs = legs;
            UpdateCyclePips();
        }

        private void OnMouseDown()
        {
            if (Application.isPlaying)
            {
                Clicked?.Invoke(this);
            }
        }

        public void ApplyPreviewPose(
            CitizenDemoState previewState,
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

        private void Update()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }

            // System.Random is not restored by Unity's Play Mode script reload.
            random ??= new System.Random(149);

            var deltaTime = runtime != null ? runtime.ActorPresentationDeltaTime : Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (routeBuilder != null && runtime != null && runtime.HasActiveRun &&
                !runtime.OnboardingOpen)
            {
                if (presentationDay != runtime.Clock.DayNumber)
                {
                    presentationDay = runtime.Clock.DayNumber;
                    reachedWork = reachedMeal = reachedHome = false;
                    UpdateCyclePips();
                }
                var destination = ScheduledDestination(runtime.Clock.SecondsIntoDay);
                if (destination != currentDestinationRoomId)
                {
                    BeginTrip(destination);
                }
            }

            stateTime += deltaTime;
            if (tripWaypoints != null)
            {
                UpdateTrip(deltaTime);
            }
            else if (state == CitizenDemoState.Walk)
            {
                UpdateWalk(deltaTime);
            }

            visual.ApplyPose(state, stateTime);
            if (tripWaypoints == null && stateTime >= stateDuration)
            {
                ChooseNextState();
            }
        }

        private string ScheduledDestination(double secondsIntoDay)
        {
            if (secondsIntoDay < 12d || secondsIntoDay >= 140d)
            {
                return homeRoomId;
            }
            if (secondsIntoDay < 110d)
            {
                return officeRoomId;
            }
            return foodShopRoomId;
        }

        private void BeginTrip(string destinationRoomId)
        {
            currentDestinationRoomId = destinationRoomId;
            tripWaypoints = routeBuilder?.Invoke(transform.position, destinationRoomId);
            tripWaypointIndex = 0;
            if (tripWaypoints == null || tripWaypoints.Count == 0)
            {
                tripWaypoints = null;
                return;
            }
            BeginState(CitizenDemoState.Walk, float.PositiveInfinity);
        }

        private void UpdateTrip(float deltaTime)
        {
            var distanceBudget = deltaTime * CommuteSpeed;
            while (distanceBudget > 0f && tripWaypointIndex < tripWaypoints.Count)
            {
                var next = tripWaypoints[tripWaypointIndex];
                var difference = next - transform.position;
                difference.y = 0f;
                var distance = difference.magnitude;
                if (distance <= 0.025f)
                {
                    transform.position = next;
                    tripWaypointIndex++;
                    continue;
                }

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(difference / distance, Vector3.up),
                    Mathf.Min(1f, deltaTime * 8f));
                var step = Mathf.Min(distanceBudget, distance);
                var previousPosition = transform.position;
                transform.position += difference / distance * step;
                distanceBudget -= step;
                if (!string.IsNullOrEmpty(officeRoomId) &&
                    currentDestinationRoomId == officeRoomId)
                {
                    WorkCommuteMoved?.Invoke(this, previousPosition, transform.position);
                }
            }

            if (tripWaypointIndex < tripWaypoints.Count)
            {
                return;
            }

            tripWaypoints = null;
            if (currentDestinationRoomId == officeRoomId && cycleLegs.Work)
                reachedWork = true;
            else if (currentDestinationRoomId == foodShopRoomId &&
                     reachedWork && cycleLegs.Meal)
                reachedMeal = true;
            else if (currentDestinationRoomId == homeRoomId &&
                     reachedWork && reachedMeal && cycleLegs.Home)
                reachedHome = true;
            UpdateCyclePips();
            habitatCenter = transform.position;
            habitatHalfExtents = currentDestinationRoomId == homeRoomId
                ? new Vector2(0.52f, 0.52f)
                : new Vector2(0.22f, 0.22f);
            BeginState(CitizenDemoState.Observe, RandomRange(1.4f, 2.4f));
        }

        private void UpdateCyclePips() => commutePips?.SetCommuteProgress(
            cycleLegs, reachedWork, reachedMeal, reachedHome);

        private void UpdateWalk(float deltaTime)
        {
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.03f)
            {
                BeginState(CitizenDemoState.Observe, RandomRange(1.1f, 1.8f));
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 6f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, deltaTime * 0.72f);
        }

        private void ChooseNextState()
        {
            if (state == CitizenDemoState.Walk)
            {
                BeginState(CitizenDemoState.Observe, RandomRange(1.0f, 1.8f));
                return;
            }

            if (random.NextDouble() < 0.62)
            {
                targetPosition = new Vector3(
                    habitatCenter.x + RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x),
                    habitatCenter.y,
                    habitatCenter.z + RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y));
                BeginState(CitizenDemoState.Walk, RandomRange(1.8f, 3.4f));
            }
            else
            {
                BeginState(CitizenDemoState.Idle, RandomRange(1.0f, 2.2f));
            }
        }

        private void BeginState(CitizenDemoState nextState, float duration)
        {
            state = nextState;
            stateTime = 0f;
            stateDuration = duration;
            visual?.ApplyPose(state, 0f);
        }

        private float RandomRange(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }
    }
}
