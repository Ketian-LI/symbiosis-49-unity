using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class HedgehogDemoVisual : MonoBehaviour
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

            ApplyPose(HedgehogDemoState.Idle, 0f);
        }

        public void ApplyPose(HedgehogDemoState state, float continuousTime)
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
                HedgehogDemoState.Waddle => 1f + Mathf.Repeat(continuousTime, 2f),
                HedgehogDemoState.Sniff => 3f + Mathf.Repeat(continuousTime, 1.32f),
                HedgehogDemoState.Curl => 4.34f + Mathf.Repeat(continuousTime, 1.14f),
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
            const string resourcePath = "Animals/Hedgehog/Hedgehog_Animated_v04";
            var prefab = Resources.Load<GameObject>(resourcePath);
            importedAnimation = Resources.LoadAll<AnimationClip>(resourcePath)
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
            if (prefab == null || importedAnimation == null)
            {
                return false;
            }

            visualRoot = NewPivot("Replaceable Hedgehog Visual", transform, Vector3.zero, hideFlags);
            var importedAxisRoot = NewPivot("Blender To Unity Axis", visualRoot, Vector3.zero, hideFlags);
            importedAxisRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            importedAxisRoot.localScale = Vector3.one * WorldScaleStandards.HedgehogModelScale;
            importedVisual = Instantiate(prefab, importedAxisRoot, false);
            importedVisual.name = "Hedgehog Blender Model v04";
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
            visualRoot = NewPivot("Hedgehog Fallback Visual", transform, Vector3.zero, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                "Hedgehog Placeholder",
                visualRoot,
                new Vector3(0f, 0.10f, 0f),
                new Vector3(0.18f, 0.18f, 0.25f),
                new Color(0.55f, 0.42f, 0.27f),
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
