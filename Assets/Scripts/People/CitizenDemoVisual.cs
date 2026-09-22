using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.People
{
    public sealed class CitizenDemoVisual : MonoBehaviour
    {
        public const float AnimationFramesPerSecond = 12f;

        private Transform visualRoot;
        private Transform importedAxisRoot;
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

            ApplyPose(CitizenDemoState.Idle, 0f);
        }

        public void ApplyPose(CitizenDemoState state, float continuousTime)
        {
            if (!initialized || importedVisual == null || importedAnimation == null)
            {
                return;
            }

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            var sampleTime = state switch
            {
                CitizenDemoState.Walk => 1f + Mathf.Repeat(continuousTime, 2f),
                CitizenDemoState.Observe => 3f + Mathf.Repeat(continuousTime, 1.48f),
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
            const string resourcePath = "People/Citizen/Citizen_Animated_v01";
            var prefab = Resources.Load<GameObject>(resourcePath);
            importedAnimation = Resources.LoadAll<AnimationClip>(resourcePath)
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
            if (prefab == null || importedAnimation == null)
            {
                return false;
            }

            visualRoot = NewPivot("Replaceable Citizen Visual", transform, Vector3.zero, hideFlags);
            importedAxisRoot = NewPivot("Blender To Unity Axis", visualRoot, Vector3.zero, hideFlags);
            importedAxisRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            importedAxisRoot.localScale = Vector3.one * WorldScaleStandards.CitizenModelScale;
            importedVisual = Instantiate(prefab, importedAxisRoot, false);
            importedVisual.name = "Citizen Blender Model v01";
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
            visualRoot = NewPivot("Citizen Fallback Visual", transform, Vector3.zero, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Capsule,
                "Citizen Placeholder",
                visualRoot,
                new Vector3(0f, 0.88f, 0f),
                new Vector3(0.48f, 0.88f, 0.48f),
                Hex("D49A36"),
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

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out var color) ? color : Color.magenta;
        }
    }
}
