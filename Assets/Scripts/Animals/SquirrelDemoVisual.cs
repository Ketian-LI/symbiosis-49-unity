using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class SquirrelDemoVisual : MonoBehaviour
    {
        public const float AnimationFramesPerSecond = 12f;

        private Transform visualRoot;
        private GameObject importedVisual;
        private AnimationClip importedAnimation;
        private bool initialized;

        public void Initialize(Material fallbackMaterial, HideFlags hideFlags)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (!TryBuildImportedModel(hideFlags))
            {
                BuildFallback(fallbackMaterial, hideFlags);
            }

            ApplyPose(SquirrelDemoState.Idle, 0f);
        }

        public void ApplyPose(SquirrelDemoState state, float continuousTime)
        {
            if (!initialized || importedVisual == null || importedAnimation == null)
            {
                return;
            }

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            var sampleTime = state switch
            {
                SquirrelDemoState.Hop => 1f + Mathf.Repeat(continuousTime, 2f),
                SquirrelDemoState.Forage => 3f + Mathf.Repeat(continuousTime, 1.32f),
                SquirrelDemoState.Alert => 4.34f + Mathf.Repeat(continuousTime, 1.14f),
                _ => Mathf.Repeat(continuousTime, 0.95f)
            };
            importedAnimation.SampleAnimation(importedVisual, Mathf.Min(sampleTime, importedAnimation.length));
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(visible);
            }
        }

        private bool TryBuildImportedModel(HideFlags hideFlags)
        {
            const string resourcePath = "Animals/Squirrel/Squirrel_Animated_v02";
            var prefab = Resources.Load<GameObject>(resourcePath);
            importedAnimation = Resources.LoadAll<AnimationClip>(resourcePath)
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
            if (prefab == null || importedAnimation == null)
            {
                return false;
            }

            visualRoot = NewPivot("Replaceable Squirrel Visual", transform, Vector3.zero, hideFlags);
            var importedAxisRoot = NewPivot("Blender To Unity Axis", visualRoot, Vector3.zero, hideFlags);
            importedAxisRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            importedAxisRoot.localScale = Vector3.one * WorldScaleStandards.SquirrelModelScale;
            importedVisual = Instantiate(prefab, importedAxisRoot, false);
            importedVisual.name = "Squirrel Blender Model v02";
            importedVisual.hideFlags = hideFlags;
            importedVisual.transform.localPosition = Vector3.zero;
            importedVisual.transform.localRotation = Quaternion.identity;
            importedVisual.transform.localScale = Vector3.one;

            var animator = importedVisual.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            return true;
        }

        private void BuildFallback(Material material, HideFlags hideFlags)
        {
            visualRoot = NewPivot("Squirrel Fallback Visual", transform, Vector3.zero, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                "Squirrel Placeholder",
                visualRoot,
                new Vector3(0f, 0.17f, 0f),
                new Vector3(0.26f, 0.30f, 0.38f),
                new Color(0.78f, 0.31f, 0.14f),
                material,
                true,
                hideFlags);
        }

        private static Transform NewPivot(string objectName, Transform parent, Vector3 localPosition, HideFlags hideFlags)
        {
            var instance = new GameObject(objectName)
            {
                hideFlags = hideFlags
            };
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            return instance.transform;
        }
    }
}
