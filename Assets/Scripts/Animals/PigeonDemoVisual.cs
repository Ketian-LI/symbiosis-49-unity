using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    /// <summary>
    /// A replaceable, procedural pigeon puppet used by the prototype.
    /// Behaviour lives in PigeonDemoAgent so this visual can later be replaced
    /// by a rigged production model without changing simulation code.
    /// </summary>
    public sealed class PigeonDemoVisual : MonoBehaviour
    {
        public const float AnimationFramesPerSecond = 12f;
        public const float RespawnFeedbackDurationSeconds = 1f;

        private static readonly Color BodyGrey = Hex("72777B");
        private static readonly Color ChestGrey = Hex("858B8D");
        private static readonly Color WingGrey = Hex("5E666B");
        private static readonly Color NeckGreen = Hex("3D7468");
        private static readonly Color NeckPurple = Hex("6A587A");
        private static readonly Color BeakOchre = Hex("D29A52");
        private static readonly Color FootRose = Hex("A86667");
        private static readonly Color EyeDark = Hex("17191A");
        private static readonly Color EyeAmber = Hex("D6A744");

        private Transform visualRoot;
        private Transform bodyPivot;
        private Transform headPivot;
        private Transform leftWingPivot;
        private Transform rightWingPivot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform tailPivot;
        private Renderer[] renderers;
        private Transform respawnAuraRoot;
        private Renderer[] respawnAuraRenderers;
        private Material respawnAuraMaterial;
        private Transform importedAxisRoot;
        private GameObject importedVisual;
        private AnimationClip importedAnimation;
        private bool usesImportedModel;
        private bool initialized;

        public bool IsRespawnFeedbackVisible =>
            respawnAuraRoot != null && respawnAuraRoot.gameObject.activeSelf;

        public void Initialize(Material material, HideFlags hideFlags)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (!TryBuildImportedModel(hideFlags))
            {
                BuildPuppet(material, hideFlags);
            }
            BuildRespawnAura(hideFlags);
            ApplyPose(PigeonDemoState.Idle, 0f);
        }

        public void ApplyPose(PigeonDemoState state, float continuousTime)
        {
            if (!initialized)
            {
                return;
            }

            if (usesImportedModel)
            {
                ApplyImportedPose(state, continuousTime);
                ApplyRespawnFeedback(state, continuousTime);
                return;
            }

            var steppedTime = QuantizeTime(continuousTime);
            ResetPose();

            switch (state)
            {
                case PigeonDemoState.Walk:
                    ApplyWalkPose(steppedTime);
                    break;
                case PigeonDemoState.Peck:
                    ApplyPeckPose(steppedTime);
                    break;
                case PigeonDemoState.Flutter:
                    ApplyFlutterPose(steppedTime);
                    break;
                case PigeonDemoState.Dead:
                    ApplyDeathPose(continuousTime);
                    break;
                case PigeonDemoState.Respawning:
                    ApplyRespawnPose(continuousTime);
                    break;
                default:
                    ApplyIdlePose(steppedTime);
                    break;
            }

            ApplyRespawnFeedback(state, continuousTime);
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(visible);
            }
        }

        public static float QuantizeTime(float time)
        {
            return Mathf.Floor(Mathf.Max(0f, time) * AnimationFramesPerSecond) / AnimationFramesPerSecond;
        }

        private bool TryBuildImportedModel(HideFlags hideFlags)
        {
            const string resourcePath = "Animals/Pigeon/Pigeon_Animated_v02";
            var prefab = Resources.Load<GameObject>(resourcePath);
            importedAnimation = Resources.LoadAll<AnimationClip>(resourcePath)
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
            if (prefab == null || importedAnimation == null)
            {
                return false;
            }

            visualRoot = NewPivot("Replaceable Blender Pigeon Visual", transform, Vector3.zero, hideFlags);
            importedAxisRoot = NewPivot("Blender To Unity Axis", visualRoot, Vector3.zero, hideFlags);
            importedAxisRoot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            importedAxisRoot.localScale = Vector3.one * WorldScaleStandards.PigeonModelScale;
            importedVisual = Instantiate(prefab, importedAxisRoot, false);
            importedVisual.name = "Pigeon Blender Model v02";
            importedVisual.hideFlags = hideFlags;
            importedVisual.transform.localPosition = Vector3.zero;
            importedVisual.transform.localRotation = Quaternion.identity;
            importedVisual.transform.localScale = Vector3.one;

            var animator = importedVisual.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            renderers = importedVisual.GetComponentsInChildren<Renderer>(true);
            usesImportedModel = true;
            return true;
        }

        private void ApplyImportedPose(PigeonDemoState state, float time)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            var sampleTime = state switch
            {
                PigeonDemoState.Walk => 1f + Mathf.Repeat(time, 2f),
                PigeonDemoState.Peck => 3f + Mathf.Repeat(time, 1.32f),
                PigeonDemoState.Flutter => 4.34f + Mathf.Repeat(time, 1.14f),
                PigeonDemoState.Dead => 5.55f,
                PigeonDemoState.Respawning => Mathf.Repeat(time, 0.95f),
                _ => Mathf.Repeat(time, 0.95f)
            };
            importedAnimation.SampleAnimation(importedVisual, Mathf.Min(sampleTime, importedAnimation.length));

            if (state == PigeonDemoState.Dead)
            {
                var fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.72f));
                visualRoot.localPosition = Vector3.down * (0.12f * fall);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, 70f * fall);
            }
            else if (state == PigeonDemoState.Respawning)
            {
                var appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time));
                visualRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, appear);
                visualRoot.localPosition = Vector3.up * ((1f - appear) * 0.04f);
            }
        }

        private void BuildPuppet(Material material, HideFlags hideFlags)
        {
            visualRoot = NewPivot("Replaceable Pigeon Visual", transform, Vector3.zero, hideFlags);
            bodyPivot = NewPivot("Body Pivot", visualRoot, new Vector3(0f, 0.67f, 0f), hideFlags);

            CreatePart(PrimitiveType.Sphere, "Body", bodyPivot, Vector3.zero, new Vector3(0.70f, 0.72f, 0.96f), BodyGrey, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Chest", bodyPivot, new Vector3(0f, 0.03f, 0.31f), new Vector3(0.59f, 0.66f, 0.60f), ChestGrey, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Neck Green Plane", bodyPivot, new Vector3(-0.05f, 0.24f, 0.32f), new Vector3(0.43f, 0.45f, 0.43f), NeckGreen, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Neck Purple Plane", bodyPivot, new Vector3(0.07f, 0.20f, 0.36f), new Vector3(0.34f, 0.34f, 0.34f), NeckPurple, material, hideFlags);

            headPivot = NewPivot("Head Pivot", bodyPivot, new Vector3(0f, 0.43f, 0.44f), hideFlags);
            CreatePart(PrimitiveType.Sphere, "Head", headPivot, Vector3.zero, new Vector3(0.43f, 0.43f, 0.43f), BodyGrey, material, hideFlags);
            CreatePart(PrimitiveType.Cube, "Beak", headPivot, new Vector3(0f, -0.03f, 0.30f), new Vector3(0.18f, 0.12f, 0.32f), BeakOchre, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Left Eye Amber", headPivot, new Vector3(-0.18f, 0.08f, 0.15f), Vector3.one * 0.11f, EyeAmber, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Right Eye Amber", headPivot, new Vector3(0.18f, 0.08f, 0.15f), Vector3.one * 0.11f, EyeAmber, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Left Pupil", headPivot, new Vector3(-0.205f, 0.085f, 0.22f), Vector3.one * 0.055f, EyeDark, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Right Pupil", headPivot, new Vector3(0.205f, 0.085f, 0.22f), Vector3.one * 0.055f, EyeDark, material, hideFlags);

            leftWingPivot = NewPivot("Left Wing Pivot", bodyPivot, new Vector3(-0.34f, 0.11f, 0f), hideFlags);
            rightWingPivot = NewPivot("Right Wing Pivot", bodyPivot, new Vector3(0.34f, 0.11f, 0f), hideFlags);
            CreatePart(PrimitiveType.Sphere, "Left Wing", leftWingPivot, new Vector3(-0.08f, 0f, -0.03f), new Vector3(0.25f, 0.46f, 0.78f), WingGrey, material, hideFlags);
            CreatePart(PrimitiveType.Sphere, "Right Wing", rightWingPivot, new Vector3(0.08f, 0f, -0.03f), new Vector3(0.25f, 0.46f, 0.78f), WingGrey, material, hideFlags);

            tailPivot = NewPivot("Tail Pivot", bodyPivot, new Vector3(0f, -0.02f, -0.43f), hideFlags);
            for (var index = -1; index <= 1; index++)
            {
                var tail = CreatePart(
                    PrimitiveType.Cube,
                    $"Tail Feather {index + 2}",
                    tailPivot,
                    new Vector3(index * 0.13f, 0f, -0.25f),
                    new Vector3(0.16f, 0.08f, 0.58f),
                    WingGrey,
                    material,
                    hideFlags);
                tail.transform.localRotation = Quaternion.Euler(index * 4f, index * -8f, 0f);
            }

            leftLegPivot = NewPivot("Left Leg Pivot", bodyPivot, new Vector3(-0.18f, -0.40f, 0.10f), hideFlags);
            rightLegPivot = NewPivot("Right Leg Pivot", bodyPivot, new Vector3(0.18f, -0.40f, 0.10f), hideFlags);
            BuildLeg(leftLegPivot, material, hideFlags);
            BuildLeg(rightLegPivot, material, hideFlags);

            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }

        private static void BuildLeg(Transform legPivot, Material material, HideFlags hideFlags)
        {
            CreatePart(PrimitiveType.Cylinder, "Lower Leg", legPivot, new Vector3(0f, -0.18f, 0f), new Vector3(0.055f, 0.18f, 0.055f), FootRose, material, hideFlags);
            CreatePart(PrimitiveType.Cube, "Middle Toe", legPivot, new Vector3(0f, -0.36f, 0.13f), new Vector3(0.045f, 0.035f, 0.28f), FootRose, material, hideFlags);

            var leftToe = CreatePart(PrimitiveType.Cube, "Left Toe", legPivot, new Vector3(-0.07f, -0.36f, 0.11f), new Vector3(0.04f, 0.03f, 0.23f), FootRose, material, hideFlags);
            leftToe.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);
            var rightToe = CreatePart(PrimitiveType.Cube, "Right Toe", legPivot, new Vector3(0.07f, -0.36f, 0.11f), new Vector3(0.04f, 0.03f, 0.23f), FootRose, material, hideFlags);
            rightToe.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
        }

        private void ResetPose()
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            bodyPivot.localPosition = new Vector3(0f, 0.67f, 0f);
            bodyPivot.localRotation = Quaternion.identity;
            headPivot.localPosition = new Vector3(0f, 0.43f, 0.44f);
            headPivot.localRotation = Quaternion.identity;
            leftWingPivot.localRotation = Quaternion.Euler(0f, 0f, 7f);
            rightWingPivot.localRotation = Quaternion.Euler(0f, 0f, -7f);
            leftLegPivot.localRotation = Quaternion.identity;
            rightLegPivot.localRotation = Quaternion.identity;
            tailPivot.localRotation = Quaternion.identity;
        }

        private void ApplyIdlePose(float time)
        {
            var breath = Mathf.Sin(time * 2.5f) * 0.018f;
            var look = Mathf.Sin(time * 1.15f) * 10f;
            bodyPivot.localPosition += Vector3.up * breath;
            headPivot.localRotation = Quaternion.Euler(0f, look, 0f);
            tailPivot.localRotation = Quaternion.Euler(-breath * 80f, 0f, 0f);
        }

        private void ApplyWalkPose(float time)
        {
            var cycle = time * Mathf.PI * 5.2f;
            var stride = Mathf.Sin(cycle);
            var bob = Mathf.Abs(Mathf.Sin(cycle)) * 0.075f;
            bodyPivot.localPosition += Vector3.up * bob;
            bodyPivot.localRotation = Quaternion.Euler(2f + stride * 2f, 0f, -stride * 2.5f);
            headPivot.localPosition += new Vector3(0f, -bob * 0.45f, stride * 0.055f);
            headPivot.localRotation = Quaternion.Euler(-stride * 7f, 0f, 0f);
            leftLegPivot.localRotation = Quaternion.Euler(stride * 38f, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(-stride * 38f, 0f, 0f);
            leftWingPivot.localRotation = Quaternion.Euler(0f, 0f, 9f + stride * 2f);
            rightWingPivot.localRotation = Quaternion.Euler(0f, 0f, -9f + stride * 2f);
        }

        private void ApplyPeckPose(float time)
        {
            var peckCycle = Mathf.Repeat(time * 1.8f, 1f);
            var peck = peckCycle < 0.36f
                ? Mathf.SmoothStep(0f, 1f, peckCycle / 0.36f)
                : Mathf.SmoothStep(1f, 0f, (peckCycle - 0.36f) / 0.64f);
            bodyPivot.localRotation = Quaternion.Euler(peck * 8f, 0f, 0f);
            headPivot.localPosition += new Vector3(0f, -0.33f * peck, 0.20f * peck);
            headPivot.localRotation = Quaternion.Euler(58f * peck, 0f, 0f);
        }

        private void ApplyFlutterPose(float time)
        {
            var flap = Mathf.Sin(time * Mathf.PI * 10f);
            var lift = Mathf.Abs(flap) * 0.11f;
            bodyPivot.localPosition += Vector3.up * lift;
            bodyPivot.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            leftWingPivot.localRotation = Quaternion.Euler(0f, 0f, 28f + flap * 66f);
            rightWingPivot.localRotation = Quaternion.Euler(0f, 0f, -28f - flap * 66f);
            tailPivot.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        }

        private void ApplyDeathPose(float time)
        {
            var fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.72f));
            bodyPivot.localPosition += new Vector3(0f, -0.20f * fall, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, 72f * fall);
            leftWingPivot.localRotation = Quaternion.Euler(0f, 0f, 18f + 28f * fall);
            rightWingPivot.localRotation = Quaternion.Euler(0f, 0f, -18f - 18f * fall);
        }

        private void ApplyRespawnPose(float time)
        {
            var appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time));
            visualRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, appear);
            bodyPivot.localPosition += Vector3.up * ((1f - appear) * 0.06f);
            leftWingPivot.localRotation = Quaternion.Euler(0f, 0f, 32f - appear * 25f);
            rightWingPivot.localRotation = Quaternion.Euler(0f, 0f, -32f + appear * 25f);
        }

        private void BuildRespawnAura(HideFlags hideFlags)
        {
            var auraObject = Instantiate(visualRoot.gameObject, transform, false);
            auraObject.name = "Pigeon Respawn Silhouette";
            ApplyHideFlagsRecursively(auraObject.transform, hideFlags);
            respawnAuraRoot = auraObject.transform;

            foreach (var animator in auraObject.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            respawnAuraMaterial = CreateRespawnAuraMaterial();
            respawnAuraRenderers = auraObject.GetComponentsInChildren<Renderer>(true);
            foreach (var auraRenderer in respawnAuraRenderers)
            {
                var materialCount = Mathf.Max(1, auraRenderer.sharedMaterials.Length);
                auraRenderer.sharedMaterials = Enumerable.Repeat(respawnAuraMaterial, materialCount).ToArray();
                auraRenderer.shadowCastingMode = ShadowCastingMode.Off;
                auraRenderer.receiveShadows = false;
            }

            auraObject.SetActive(false);
        }

        private void ApplyRespawnFeedback(PigeonDemoState state, float time)
        {
            if (respawnAuraRoot == null)
            {
                return;
            }

            if (state != PigeonDemoState.Respawning)
            {
                respawnAuraRoot.gameObject.SetActive(false);
                SetOriginalRenderersEnabled(true);
                return;
            }

            var progress = Mathf.Clamp01(time / RespawnFeedbackDurationSeconds);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            var bodyVisible = progress >= 0.18f;
            SetOriginalRenderersEnabled(bodyVisible);

            respawnAuraRoot.gameObject.SetActive(true);
            CopyPoseRecursively(visualRoot, respawnAuraRoot);
            var breathingScale = 1.035f + Mathf.Sin(progress * Mathf.PI * 2f) * 0.012f;
            respawnAuraRoot.localScale = Vector3.Scale(visualRoot.localScale, Vector3.one * breathingScale);

            var fade = 1f - eased;
            var alpha = Mathf.Lerp(0.58f, 0f, eased) + Mathf.Sin(progress * Mathf.PI) * 0.10f * fade;
            var auraColor = new Color(0.30f, 0.92f, 0.96f, Mathf.Clamp01(alpha));
            foreach (var auraRenderer in respawnAuraRenderers)
            {
                UrbanVisualFactory.ApplyColor(auraRenderer, auraColor);
            }
        }

        private void SetOriginalRenderersEnabled(bool value)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = value;
                }
            }
        }

        private static void CopyPoseRecursively(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;

            var childCount = Mathf.Min(source.childCount, target.childCount);
            for (var index = 0; index < childCount; index++)
            {
                CopyPoseRecursively(source.GetChild(index), target.GetChild(index));
            }
        }

        private static Material CreateRespawnAuraMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var auraMaterial = new Material(shader)
            {
                name = "Pigeon Respawn Cyan Silhouette",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Transparent
            };

            if (auraMaterial.HasProperty("_Surface"))
            {
                auraMaterial.SetFloat("_Surface", 1f);
            }
            if (auraMaterial.HasProperty("_Blend"))
            {
                auraMaterial.SetFloat("_Blend", 0f);
            }
            if (auraMaterial.HasProperty("_SrcBlend"))
            {
                auraMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }
            if (auraMaterial.HasProperty("_DstBlend"))
            {
                auraMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            if (auraMaterial.HasProperty("_ZWrite"))
            {
                auraMaterial.SetFloat("_ZWrite", 0f);
            }

            auraMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            auraMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return auraMaterial;
        }

        private static void ApplyHideFlagsRecursively(Transform root, HideFlags hideFlags)
        {
            root.gameObject.hideFlags = hideFlags;
            for (var index = 0; index < root.childCount; index++)
            {
                ApplyHideFlagsRecursively(root.GetChild(index), hideFlags);
            }
        }

        private void OnDestroy()
        {
            if (respawnAuraMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(respawnAuraMaterial);
            }
            else
            {
                DestroyImmediate(respawnAuraMaterial);
            }
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

        private static GameObject CreatePart(
            PrimitiveType primitiveType,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Material material,
            HideFlags hideFlags)
        {
            var part = UrbanVisualFactory.CreatePrimitive(
                primitiveType,
                objectName,
                parent,
                localPosition,
                localScale,
                color,
                material,
                true,
                hideFlags);
            part.transform.localRotation = Quaternion.identity;
            return part;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out var color) ? color : Color.magenta;
        }
    }
}
