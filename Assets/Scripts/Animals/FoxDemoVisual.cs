using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class FoxDemoVisual : MonoBehaviour
    {
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
            ApplyPose(FoxDemoState.Rest, 0f);
        }

        public void ApplyPose(FoxDemoState state, float continuousTime)
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
                FoxDemoState.Trot => 1f + Mathf.Repeat(continuousTime, 2f),
                FoxDemoState.Sniff => 3f + Mathf.Repeat(continuousTime, 1.32f),
                FoxDemoState.Alert => 4.34f + Mathf.Repeat(continuousTime, 1.14f),
                _ => Mathf.Repeat(continuousTime, 0.95f)
            };
            importedAnimation.SampleAnimation(importedVisual, Mathf.Min(sampleTime, importedAnimation.length));
        }

        private bool TryBuildImportedModel(HideFlags hideFlags)
        {
            const string resourcePath = "Animals/Fox/Fox_Animated_v01";
            var prefab = Resources.Load<GameObject>(resourcePath);
            importedAnimation = Resources.LoadAll<AnimationClip>(resourcePath)
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
            if (prefab == null || importedAnimation == null)
            {
                return false;
            }
            visualRoot = NewPivot("Replaceable Fox Visual", transform, hideFlags);
            var axis = NewPivot("Blender To Unity Axis", visualRoot, hideFlags);
            axis.localRotation = Quaternion.Euler(0f, 90f, 0f);
            axis.localScale = Vector3.one * WorldScaleStandards.FoxModelScale;
            importedVisual = Instantiate(prefab, axis, false);
            importedVisual.name = "Fox Blender Model v01";
            importedVisual.hideFlags = hideFlags;
            var animator = importedVisual.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
            return true;
        }

        private void BuildFallback(Material material, HideFlags hideFlags)
        {
            visualRoot = NewPivot("Fox Fallback Visual", transform, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Capsule,
                "Fox Placeholder",
                visualRoot,
                new Vector3(0f, 0.28f, 0f),
                new Vector3(0.32f, 0.42f, 0.64f),
                new Color(0.72f, 0.27f, 0.12f),
                material,
                true,
                hideFlags);
        }

        private static Transform NewPivot(string name, Transform parent, HideFlags hideFlags)
        {
            var instance = new GameObject(name) { hideFlags = hideFlags };
            instance.transform.SetParent(parent, false);
            return instance.transform;
        }
    }
}
