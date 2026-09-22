using System;
using UnityEngine;

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
        private readonly System.Random random = new(149);

        private CitizenDemoVisual visual;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private CitizenDemoState state;
        private float stateTime;
        private float stateDuration;
        private bool initialized;

        public Vector3 SpawnPosition => spawnPosition;
        public CitizenDemoState State => state;
        public event Action<CitizenDemoAgent> Clicked;

        public void Initialize(
            Material sharedMaterial,
            Vector3 initialPosition,
            Vector2 movementHalfExtents,
            HideFlags hideFlags)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            habitatHalfExtents = movementHalfExtents;
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            visual = gameObject.AddComponent<CitizenDemoVisual>();
            visual.Initialize(sharedMaterial, hideFlags);
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.48f, 0f);
            collider.radius = 0.20f;
            collider.height = 0.95f;
            BeginState(CitizenDemoState.Idle, 1.4f);
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

            stateTime += Time.deltaTime;
            if (state == CitizenDemoState.Walk)
            {
                UpdateWalk(Time.deltaTime);
            }

            visual.ApplyPose(state, stateTime);
            if (stateTime >= stateDuration)
            {
                ChooseNextState();
            }
        }

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
