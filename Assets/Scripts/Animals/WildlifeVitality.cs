using System;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class WildlifeVitality : MonoBehaviour
    {
        public const float RespawnDelaySeconds = 20f;
        public const float RespawnGlowSeconds = 1f;

        private WildlifeSpecies species;
        private Behaviour movementBehaviour;
        private Renderer[] renderers;
        private Collider[] colliders;
        private Vector3 respawnPosition;
        private Material material;
        private HideFlags generatedHideFlags;
        private float deathTime;
        private float glowTime;
        private bool initialized;
        private bool respawnGlow;

        public event Action<WildlifeVitality, AnimalDeathCause> Died;
        public event Action<WildlifeVitality> Respawned;
        public event Action StateChanged;

        public WildlifeSpecies Species => species;
        public bool IsAlive { get; private set; } = true;

        public void Initialize(
            WildlifeSpecies animalSpecies,
            Behaviour behaviour,
            Vector3 initialRespawnPosition,
            Material sharedMaterial,
            HideFlags generatedHideFlags)
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            species = animalSpecies;
            movementBehaviour = behaviour;
            respawnPosition = initialRespawnPosition;
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
        }

        public bool Kill(AnimalDeathCause cause)
        {
            if (!initialized || !IsAlive)
            {
                return false;
            }
            IsAlive = false;
            deathTime = 0f;
            respawnGlow = false;
            if (movementBehaviour != null)
            {
                movementBehaviour.enabled = false;
            }
            foreach (var collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
            CreateDeathMarker();
            Died?.Invoke(this, cause);
            StateChanged?.Invoke();
            return true;
        }

        public void SetRespawnPosition(Vector3 worldPosition)
        {
            respawnPosition = worldPosition;
        }

        private void Update()
        {
            if (!initialized || !Application.isPlaying)
            {
                return;
            }
            if (!IsAlive)
            {
                deathTime += Time.deltaTime;
                if (deathTime >= 0.85f)
                {
                    SetRenderersVisible(false);
                }
                if (deathTime >= RespawnDelaySeconds)
                {
                    Respawn();
                }
                return;
            }
            if (respawnGlow)
            {
                glowTime += Time.deltaTime;
                if (glowTime >= RespawnGlowSeconds)
                {
                    respawnGlow = false;
                }
            }
        }

        private void Respawn()
        {
            transform.position = respawnPosition;
            SetRenderersVisible(true);
            foreach (var collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            if (movementBehaviour != null)
            {
                movementBehaviour.enabled = true;
            }
            IsAlive = true;
            respawnGlow = true;
            glowTime = 0f;
            CreateRespawnGlow();
            Respawned?.Invoke(this);
            StateChanged?.Invoke();
        }

        private void CreateDeathMarker()
        {
            var marker = UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                $"{species} Death Marker",
                transform.parent,
                transform.localPosition + Vector3.up * 0.02f,
                new Vector3(0.20f, 0.012f, 0.14f),
                new Color(0.42f, 0.08f, 0.07f, 0.92f),
                material,
                true,
                generatedHideFlags);
            marker.AddComponent<PigeonDemoTimedEffect>().Initialize(5f, true, false);
        }

        private void CreateRespawnGlow()
        {
            var glow = UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                $"{species} Respawn Glow",
                transform,
                new Vector3(0f, 0.22f, 0f),
                species == WildlifeSpecies.Fox
                    ? new Vector3(0.70f, 0.42f, 0.42f)
                    : new Vector3(0.34f, 0.28f, 0.34f),
                new Color(0.22f, 0.86f, 0.86f, 0.34f),
                material,
                true,
                generatedHideFlags);
            glow.AddComponent<PigeonDemoTimedEffect>().Initialize(RespawnGlowSeconds, true, false);
        }

        private void SetRenderersVisible(bool visible)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
}
