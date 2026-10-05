using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // Three tiny world-space cells, shared by animal hunger and resident
    // commute feedback. No text or camera-following UI covers the room floor.
    public sealed class WorldStatusPips : MonoBehaviour
    {
        private static readonly Color Safe = new(0.46f, 0.84f, 0.70f);
        private static readonly Color Danger = new(0.95f, 0.39f, 0.32f);
        private static readonly Color Empty = new(0.22f, 0.27f, 0.30f);
        private static readonly Color Pending = new(0.31f, 0.66f, 0.53f);
        private static readonly Color Backing = new(0.10f, 0.15f, 0.18f);
        private static readonly Color Rim = new(0.94f, 0.86f, 0.66f);

        private readonly Renderer[] segments = new Renderer[3];
        private GameObject root;
        private Material ownedMaterial;
        private bool emphasized;

        public bool IsVisible => root != null && root.activeInHierarchy;

        public void Initialize(float height, float width, HideFlags hideFlags,
            bool emphasize = false)
        {
            if (root != null) return;
            emphasized = emphasize;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            ownedMaterial = new Material(shader)
            {
                name = "World Status Unlit",
                hideFlags = HideFlags.HideAndDontSave
            };

            root = new GameObject("Three-cell Status") { hideFlags = hideFlags };
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            if (emphasized)
            {
                CreatePlate("Status Rim", new Vector3(0f, -0.042f, 0f),
                    new Vector3(width * 4.66f, 0.018f, width * 1.84f), Rim, hideFlags);
                CreatePlate("Status Backing", new Vector3(0f, -0.028f, 0f),
                    new Vector3(width * 4.48f, 0.020f, width * 1.66f), Backing, hideFlags);
            }
            for (var index = 0; index < segments.Length; index++)
            {
                var cell = UrbanVisualFactory.CreatePrimitive(
                    PrimitiveType.Cube, $"Status Cell {index + 1}", root.transform,
                    new Vector3((index - 1) * width * 1.28f, 0f, 0f),
                    new Vector3(width, 0.028f, width * 0.82f), Safe,
                    ownedMaterial, true, hideFlags);
                segments[index] = cell.GetComponent<Renderer>();
                segments[index].shadowCastingMode = ShadowCastingMode.Off;
                segments[index].receiveShadows = false;
            }
            FaceCamera();
        }

        private void CreatePlate(string name, Vector3 position, Vector3 scale,
            Color color, HideFlags hideFlags)
        {
            var plate = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                name, root.transform, position, scale, color, ownedMaterial, true, hideFlags);
            var renderer = plate.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public void SetHungerDays(int hungryDays)
        {
            SetHungerAccess(hungryDays, true);
        }

        // The number of lit cells still shows days left before starvation;
        // their colour now answers the different question: is a meal currently
        // reachable within this species' range and the available portions?
        public void SetHungerAccess(int hungryDays, bool mealAccessible)
        {
            var days = Mathf.Clamp(hungryDays, 0, AnimalNeedsModel.StarvationDays);
            var remaining = AnimalNeedsModel.StarvationDays - days;
            var activeColor = mealAccessible ? Safe : Danger;
            for (var index = 0; index < segments.Length; index++)
                SetColor(index, index < remaining ? activeColor : Empty);
        }

        public void SetCommuteProgress(ResidentRouteLegs legs,
            bool reachedWork, bool reachedMeal, bool reachedHome)
        {
            if (legs.AssignmentBlocked)
            {
                // At least one work/meal capacity is unavailable. Highlight
                // the failed full-cycle assignment without accusing the
                // reachable home leg of being blocked.
                SetColor(0, Danger);
                SetColor(1, Pending);
                SetColor(2, Pending);
                return;
            }
            SetColor(0, !legs.Work ? Danger : reachedWork ? Safe : Pending);
            SetColor(1, !legs.Meal ? Danger : reachedMeal ? Safe : Pending);
            SetColor(2, !legs.Home ? Danger : reachedHome ? Safe : Pending);
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        private void FaceCamera()
        {
            if (!emphasized || root == null || !root.activeInHierarchy) return;
            var camera = Camera.main;
            if (camera == null) return;
            // The shallow cube tops form a screen-facing card. Keeping the
            // three cells in the camera plane avoids floor-pattern occlusion
            // and keeps their order readable while the resident turns.
            root.transform.rotation = Quaternion.LookRotation(
                camera.transform.up, -camera.transform.forward);
        }

        private void SetColor(int index, Color color)
        {
            if (segments[index] != null)
                UrbanVisualFactory.ApplyColor(segments[index], color);
        }

        private void OnDestroy()
        {
            if (ownedMaterial == null) return;
            if (Application.isPlaying) Destroy(ownedMaterial);
            else DestroyImmediate(ownedMaterial);
        }
    }
}
