using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Animals
{
    public enum PigeonDemoState
    {
        Idle,
        Walk,
        Peck,
        Flutter,
        Dead,
        Respawning
    }

    public sealed class PigeonDemoAgent : MonoBehaviour, IWildlifeLayoutAgent
    {
        public const float RespawnDelaySeconds = 20f;
        public const float RespawnFeedbackDurationSeconds = PigeonDemoVisual.RespawnFeedbackDurationSeconds;
        public const float DeathMarkerLifetimeSeconds = 5f;
        public const float FootprintLifetimeSeconds = 6f;
        public const float FootprintExitFadeSeconds = 0.35f;

        private System.Random random = new(49);
        private readonly List<PigeonDemoTimedEffect> footprintEffects = new();

        private Material material;
        private HideFlags generatedHideFlags;
        private Transform effectsRoot;
        private PigeonDemoVisual visual;
        private AnimalNeedIndicator needIndicator;
        private Collider clickCollider;
        private GameObject selectionRing;
        private Vector3 spawnPosition;
        private Vector3 habitatCenter;
        private Vector2 habitatHalfExtents;
        private Vector3 targetPosition;
        private PigeonDemoState state;
        private float stateTime;
        private float stateDuration;
        private float footprintTimer;
        private float respawnRemaining;
        private bool leftFoot;
        private bool initialized;
        private bool selected;
        private bool relocating;
        private Vector3 relocationStart;
        private Vector3 relocationTarget;
        private float relocationDuration;
        private float relocationTime;
        private readonly List<Vector3> foodWaypoints = new();
        private int foodWaypointIndex;
        private float foodResponseDelay;
        private Action foodArrival;
        private bool foodMission;
        private bool foodMissionFlying;
        private float foodGroundHeight;
        private bool activityEnabled = true;
        private bool escapingPredator;
        private float predatorEscapeTime;
        private Vector3 predatorEscapeDirection;

        public event Action<PigeonDemoAgent> Clicked;
        public event Action<PigeonDemoAgent, PigeonDemoState> StateChanged;

        public PigeonDemoState State => state;
        public bool IsAlive => state != PigeonDemoState.Dead;
        public bool IsSelected => selected;
        public float RespawnRemaining => respawnRemaining;
        public Vector3 SpawnPosition => spawnPosition;
        public WildlifeSpecies Species => WildlifeSpecies.Pigeon;
        public Transform AgentTransform => transform;
        public bool IsRespondingToFood => foodMission;
        public bool IsFlying => foodMissionFlying && foodMission ||
                                escapingPredator && predatorEscapeTime >= 0.18f;

        public void Initialize(
            Material sharedMaterial,
            Transform effectContainer,
            Vector3 initialPosition,
            Vector2 movementHalfExtents,
            HideFlags hideFlags,
            int randomSeed = 49)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            random = new System.Random(randomSeed);
            material = sharedMaterial;
            effectsRoot = effectContainer;
            generatedHideFlags = hideFlags;
            spawnPosition = initialPosition;
            habitatCenter = initialPosition;
            habitatHalfExtents = movementHalfExtents;
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);

            visual = gameObject.AddComponent<PigeonDemoVisual>();
            visual.Initialize(material, hideFlags);

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.18f, 0f);
            collider.size = new Vector3(0.52f, 0.42f, 0.55f);
            clickCollider = collider;

            selectionRing = CreateGroundSprite(
                "Follow Selection Ring",
                transform,
                AnimalSelectionVisualCatalog.GetSprite(AnimalSelectionVisual.SelectionRing),
                new Vector3(0f, 0.025f, 0f),
                0.54f,
                30,
                hideFlags);
            selectionRing.SetActive(false);

            needIndicator = gameObject.AddComponent<AnimalNeedIndicator>();
            needIndicator.Initialize(hideFlags);

            BeginState(PigeonDemoState.Idle, 1.2f);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selectionRing != null)
            {
                selectionRing.SetActive(value && state != PigeonDemoState.Dead);
            }

            needIndicator?.SetSelected(value);

            if (!value)
            {
                FadeExistingFootprints();
            }
        }

        public void SetNeedWarnings(bool hungry, bool habitatWarning, bool danger)
        {
            needIndicator?.SetNeeds(hungry, habitatWarning, danger);
        }

        public void SetActivityEnabled(bool value)
        {
            activityEnabled = value;
            if (!value && !foodMission && state != PigeonDemoState.Dead && state != PigeonDemoState.Respawning)
            {
                BeginState(PigeonDemoState.Idle, 999f);
            }
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

        public bool BeginFoodMission(
            IReadOnlyList<Vector3> waypoints,
            float responseDelay,
            Action onArrival)
        {
            if (!initialized || !IsAlive || foodMission || waypoints == null || waypoints.Count == 0)
            {
                return false;
            }

            foodWaypoints.Clear();
            foodWaypoints.AddRange(waypoints);
            foodWaypointIndex = 0;
            foodResponseDelay = Mathf.Max(0f, responseDelay);
            foodArrival = onArrival;
            foodMission = true;
            foodGroundHeight = transform.position.y;
            var distance = Vector3.Distance(transform.position, waypoints[waypoints.Count - 1]);
            foodMissionFlying = waypoints.Count > 1 || distance > WorldScaleStandards.CellSizeMeters;
            BeginState(foodMissionFlying ? PigeonDemoState.Flutter : PigeonDemoState.Walk, 999f);
            return true;
        }

        public bool BeginPredatorEscape(Vector3 predatorPosition)
        {
            if (!initialized || !IsAlive || escapingPredator)
            {
                return false;
            }

            foodMission = false;
            foodMissionFlying = false;
            foodArrival = null;
            foodWaypoints.Clear();
            predatorEscapeDirection = transform.position - predatorPosition;
            predatorEscapeDirection.y = 0f;
            if (predatorEscapeDirection.sqrMagnitude < 0.01f)
            {
                predatorEscapeDirection = transform.forward;
                predatorEscapeDirection.y = 0f;
            }
            predatorEscapeDirection.Normalize();
            predatorEscapeTime = 0f;
            escapingPredator = true;
            BeginState(PigeonDemoState.Flutter, 1.2f);
            return true;
        }

        public void Kill()
        {
            if (!initialized || state == PigeonDemoState.Dead || state == PigeonDemoState.Respawning)
            {
                return;
            }

            CreateDeathMarker();
            foodMission = false;
            foodMissionFlying = false;
            escapingPredator = false;
            foodArrival = null;
            foodWaypoints.Clear();
            SetSelected(false);
            needIndicator?.SetAlive(false);
            BeginState(PigeonDemoState.Dead, RespawnDelaySeconds);
            respawnRemaining = RespawnDelaySeconds;
            clickCollider.enabled = false;
            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }
        }

        public void ApplyPreviewPose(PigeonDemoState previewState, float poseTime, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!initialized)
            {
                return;
            }

            transform.position = worldPosition;
            transform.rotation = worldRotation;
            visual.SetVisible(true);
            visual.ApplyPose(previewState, poseTime);
            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (relocating)
            {
                UpdateRelocation(deltaTime);
                return;
            }

            stateTime += deltaTime;

            if (state == PigeonDemoState.Dead)
            {
                UpdateDeadState();
                return;
            }

            if (state == PigeonDemoState.Respawning)
            {
                visual.ApplyPose(state, stateTime);
                if (stateTime >= stateDuration)
                {
                    clickCollider.enabled = true;
                    BeginState(PigeonDemoState.Idle, RandomRange(1.1f, 2.1f));
                }

                return;
            }

            if (escapingPredator)
            {
                UpdatePredatorEscape(deltaTime);
                visual.ApplyPose(PigeonDemoState.Flutter, stateTime);
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
                visual.ApplyPose(PigeonDemoState.Idle, stateTime);
                return;
            }

            if (state == PigeonDemoState.Walk)
            {
                UpdateWalk(deltaTime);
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

        private void UpdateFoodMission(float deltaTime)
        {
            if (foodResponseDelay > 0f)
            {
                foodResponseDelay -= deltaTime;
                return;
            }

            var target = foodWaypoints[foodWaypointIndex];
            var flatTarget = new Vector3(target.x, foodGroundHeight, target.z);
            var currentFlat = new Vector3(transform.position.x, foodGroundHeight, transform.position.z);
            var toTarget = flatTarget - currentFlat;
            if (toTarget.sqrMagnitude <= 0.025f)
            {
                foodWaypointIndex++;
                if (foodWaypointIndex < foodWaypoints.Count)
                {
                    return;
                }

                transform.position = flatTarget;
                foodMission = false;
                foodWaypoints.Clear();
                BeginState(PigeonDemoState.Peck, 1.1f);
                var callback = foodArrival;
                foodArrival = null;
                callback?.Invoke();
                return;
            }

            var direction = toTarget.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                deltaTime * 8f);
            var moved = Vector3.MoveTowards(
                currentFlat,
                flatTarget,
                deltaTime * (foodMissionFlying ? 2.15f : 0.82f));
            moved.y = foodGroundHeight + (foodMissionFlying ? 0.33f : 0f);
            transform.position = moved;
        }

        private void UpdatePredatorEscape(float deltaTime)
        {
            predatorEscapeTime += deltaTime;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(predatorEscapeDirection, Vector3.up),
                deltaTime * 10f);
            var next = transform.position + predatorEscapeDirection * (2.45f * deltaTime);
            next.y = spawnPosition.y + Mathf.Lerp(0f, 0.72f, Mathf.Clamp01(predatorEscapeTime / 0.42f));
            transform.position = next;
            if (predatorEscapeTime < 1.2f)
            {
                return;
            }

            transform.position = new Vector3(transform.position.x, spawnPosition.y, transform.position.z);
            habitatCenter = transform.position;
            escapingPredator = false;
            BeginState(PigeonDemoState.Idle, RandomRange(1.0f, 2.0f));
        }

        private void OnMouseDown()
        {
            if (Application.isPlaying && state != PigeonDemoState.Dead)
            {
                Clicked?.Invoke(this);
            }
        }

        private void UpdateWalk(float deltaTime)
        {
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.025f)
            {
                BeginState(PigeonDemoState.Peck, RandomRange(1.0f, 1.7f));
                return;
            }

            var direction = toTarget.normalized;
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, deltaTime * 7f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, deltaTime * 0.82f);

            if (!selected)
            {
                return;
            }

            footprintTimer -= deltaTime;
            if (footprintTimer <= 0f)
            {
                CreateFootprint(leftFoot);
                leftFoot = !leftFoot;
                footprintTimer = 0.28f;
            }
        }

        private void UpdateDeadState()
        {
            respawnRemaining = Mathf.Max(0f, RespawnDelaySeconds - stateTime);
            if (stateTime <= 0.85f)
            {
                visual.SetVisible(true);
                visual.ApplyPose(PigeonDemoState.Dead, stateTime);
            }
            else
            {
                visual.SetVisible(false);
            }

            if (stateTime < RespawnDelaySeconds)
            {
                return;
            }

            transform.position = spawnPosition;
            transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            visual.SetVisible(true);
            needIndicator?.SetNeeds(false, false, false);
            needIndicator?.SetAlive(true);
            BeginState(PigeonDemoState.Respawning, RespawnFeedbackDurationSeconds);
        }

        private void ChooseNextState()
        {
            switch (state)
            {
                case PigeonDemoState.Idle:
                    if (RandomValue() < 0.68f)
                    {
                        ChooseWalkTarget();
                        BeginState(PigeonDemoState.Walk, RandomRange(1.8f, 3.5f));
                    }
                    else
                    {
                        BeginState(PigeonDemoState.Peck, RandomRange(0.9f, 1.6f));
                    }
                    break;
                case PigeonDemoState.Walk:
                    BeginState(PigeonDemoState.Peck, RandomRange(0.9f, 1.6f));
                    break;
                case PigeonDemoState.Peck:
                    if (RandomValue() < 0.18f)
                    {
                        BeginState(PigeonDemoState.Flutter, 0.78f);
                    }
                    else
                    {
                        BeginState(PigeonDemoState.Idle, RandomRange(0.8f, 1.8f));
                    }
                    break;
                case PigeonDemoState.Flutter:
                    BeginState(PigeonDemoState.Idle, RandomRange(1.0f, 2.0f));
                    break;
            }
        }

        private void ChooseWalkTarget()
        {
            var x = RandomRange(-habitatHalfExtents.x, habitatHalfExtents.x);
            var z = RandomRange(-habitatHalfExtents.y, habitatHalfExtents.y);
            targetPosition = habitatCenter + new Vector3(x, 0f, z);
            targetPosition.y = spawnPosition.y;
        }

        private void BeginState(PigeonDemoState nextState, float duration)
        {
            state = nextState;
            stateTime = 0f;
            stateDuration = Mathf.Max(0.01f, duration);
            footprintTimer = 0f;
            StateChanged?.Invoke(this, state);
        }

        private void CreateFootprint(bool isLeft)
        {
            if (effectsRoot == null)
            {
                return;
            }

            var side = isLeft ? -0.036f : 0.036f;
            var root = NewEffectRoot(isLeft ? "Left Pigeon Footprint" : "Right Pigeon Footprint");
            root.transform.position = transform.position + transform.right * side + Vector3.up * 0.008f;
            root.transform.rotation = transform.rotation * Quaternion.Euler(0f, RandomRange(-12f, 12f), 0f);

            var footprintSprite = AnimalSelectionVisualCatalog.GetSprite(AnimalSelectionVisual.PigeonFootprint);
            if (footprintSprite == null)
            {
                Destroy(root);
                return;
            }

            var footprint = CreateGroundSprite(
                "Pigeon Footprint Sprite",
                root.transform,
                footprintSprite,
                new Vector3(0f, 0.010f, 0f),
                0.14f,
                24,
                generatedHideFlags);
            footprint.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.82f);

            var timedEffect = root.AddComponent<PigeonDemoTimedEffect>();
            timedEffect.Initialize(FootprintLifetimeSeconds, false, true);
            footprintEffects.Add(timedEffect);
        }

        private void FadeExistingFootprints()
        {
            for (var index = footprintEffects.Count - 1; index >= 0; index--)
            {
                var effect = footprintEffects[index];
                if (effect == null)
                {
                    footprintEffects.RemoveAt(index);
                    continue;
                }

                effect.StartEarlyFade(FootprintExitFadeSeconds);
            }
        }

        private void CreateDeathMarker()
        {
            if (effectsRoot == null)
            {
                return;
            }

            var root = NewEffectRoot("Pigeon Death Imprint");
            root.transform.position = new Vector3(transform.position.x, transform.position.y + 0.01f, transform.position.z);
            root.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            var footprintSprite = DeathFeedbackVisualCatalog.GetSprite(DeathFeedbackVisual.PigeonFootprint);
            var rippleSprite = DeathFeedbackVisualCatalog.GetSprite(DeathFeedbackVisual.Ripple);

            var leftFoot = CreateGroundSprite(
                "Left Death Footprint",
                root.transform,
                footprintSprite,
                new Vector3(-0.055f, 0.015f, -0.055f),
                0.16f,
                25,
                generatedHideFlags);
            leftFoot.transform.localRotation = Quaternion.Euler(90f, 0f, -8f);

            var rightFoot = CreateGroundSprite(
                "Right Death Footprint",
                root.transform,
                footprintSprite,
                new Vector3(0.055f, 0.016f, 0.065f),
                0.16f,
                25,
                generatedHideFlags);
            rightFoot.transform.localRotation = Quaternion.Euler(90f, 0f, 7f);

            var ripple = CreateGroundSprite(
                "Death Ripple",
                root.transform,
                rippleSprite,
                new Vector3(0f, 0.010f, 0f),
                0.56f,
                23,
                generatedHideFlags);

            var leftRenderer = leftFoot.GetComponent<SpriteRenderer>();
            var rightRenderer = rightFoot.GetComponent<SpriteRenderer>();
            var rippleRenderer = ripple.GetComponent<SpriteRenderer>();
            leftRenderer.color = new Color(1f, 1f, 1f, 0.94f);
            rightRenderer.color = new Color(1f, 1f, 1f, 0.94f);
            rippleRenderer.color = new Color(1f, 1f, 1f, 0.58f);

            root.AddComponent<PigeonDeathMarkerEffect>().Initialize(
                DeathMarkerLifetimeSeconds,
                ripple.transform,
                rippleRenderer,
                leftRenderer,
                rightRenderer);
        }

        private GameObject NewEffectRoot(string objectName)
        {
            var root = new GameObject(objectName)
            {
                hideFlags = generatedHideFlags
            };
            root.transform.SetParent(effectsRoot, true);
            return root;
        }

        private static GameObject CreateGroundSprite(
            string objectName,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            float worldSize,
            int sortingOrder,
            HideFlags hideFlags)
        {
            var instance = new GameObject(objectName)
            {
                hideFlags = hideFlags
            };
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var spriteRenderer = instance.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            if (sprite != null)
            {
                var longestSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                instance.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.001f, longestSide));
            }

            return instance;
        }

        private float RandomRange(float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, RandomValue());
        }

        private float RandomValue()
        {
            return (float)random.NextDouble();
        }
    }

    public sealed class PigeonDemoTimedEffect : MonoBehaviour
    {
        private float lifetime;
        private float elapsed;
        private bool pulse;
        private bool fadeVisuals;
        private Vector3 originalScale;
        private SpriteRenderer[] spriteRenderers;
        private Color[] originalColors;

        public void Initialize(float duration, bool shouldPulse, bool shouldFadeVisuals = false)
        {
            lifetime = Mathf.Max(0.05f, duration);
            pulse = shouldPulse;
            fadeVisuals = shouldFadeVisuals;
            originalScale = transform.localScale;
            CaptureSpriteColors();
        }

        public void StartEarlyFade(float duration)
        {
            lifetime = Mathf.Max(0.05f, duration);
            elapsed = 0f;
            pulse = false;
            fadeVisuals = true;
            originalScale = transform.localScale;
            CaptureSpriteColors();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / lifetime);
            var pulseScale = pulse ? 0.90f + Mathf.Sin(progress * Mathf.PI) * 0.16f : 1f;
            var exitScale = pulse && progress > 0.82f
                ? Mathf.Lerp(1f, 0.72f, (progress - 0.82f) / 0.18f)
                : 1f;
            transform.localScale = originalScale * pulseScale * exitScale;

            if (fadeVisuals && spriteRenderers != null)
            {
                for (var index = 0; index < spriteRenderers.Length; index++)
                {
                    if (spriteRenderers[index] == null)
                    {
                        continue;
                    }

                    var color = originalColors[index];
                    color.a *= 1f - progress;
                    spriteRenderers[index].color = color;
                }
            }

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void CaptureSpriteColors()
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[spriteRenderers.Length];
            for (var index = 0; index < spriteRenderers.Length; index++)
            {
                originalColors[index] = spriteRenderers[index].color;
            }
        }
    }
}
