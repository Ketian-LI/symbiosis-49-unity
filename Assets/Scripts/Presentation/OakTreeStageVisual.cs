using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class OakTreeStageVisual : MonoBehaviour
    {
        private GameObject felled;
        private GameObject sapling;
        private GameObject young;
        private GameObject mature;
        private readonly Dictionary<GameObject, Vector3> originalScales = new();
        private readonly Dictionary<GameObject, Vector3> originalPositions = new();
        private Vector3 growthAnchor;
        private Coroutine growthRoutine;

        private const float GrowthSeconds = 0.85f;

        public OakTreeStage Stage { get; private set; }
        public bool IsGrowthAnimating => growthRoutine != null;

        public void Initialize(
            GameObject felledRoot,
            GameObject saplingRoot,
            GameObject youngRoot,
            GameObject matureRoot,
            OakTreeStage initialStage,
            Vector3 localGrowthAnchor = default)
        {
            felled = felledRoot;
            sapling = saplingRoot;
            young = youngRoot;
            mature = matureRoot;
            growthAnchor = localGrowthAnchor;
            originalScales.Clear();
            originalPositions.Clear();
            foreach (var root in Roots())
            {
                if (root == null) continue;
                originalScales[root] = root.transform.localScale;
                originalPositions[root] = root.transform.localPosition;
            }
            SetStage(initialStage);
        }

        public void SetStage(OakTreeStage stage)
        {
            if (growthRoutine != null)
            {
                StopCoroutine(growthRoutine);
                growthRoutine = null;
            }
            Stage = stage;
            foreach (var root in Roots())
            {
                if (root == null) continue;
                RestoreTransform(root);
                root.SetActive(root == RootFor(stage));
            }
        }

        // Called only for an actual day-to-day growth change. Loading a save,
        // restarting, planting and felling use SetStage and never replay growth.
        public void AnimateGrowthTo(OakTreeStage stage)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || stage <= Stage)
            {
                SetStage(stage);
                return;
            }
            if (growthRoutine != null)
            {
                SetStage(Stage);
            }
            var outgoing = RootFor(Stage);
            var incoming = RootFor(stage);
            if (incoming == null)
            {
                SetStage(stage);
                return;
            }
            Stage = stage; // The visual's logical state is current immediately.
            incoming.SetActive(true);
            ApplyGrowthRatio(incoming, 0.35f);
            growthRoutine = StartCoroutine(Grow(outgoing, incoming));
        }

        private IEnumerator Grow(GameObject outgoing, GameObject incoming)
        {
            var elapsed = 0f;
            while (elapsed < GrowthSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / GrowthSeconds);
                var firstPart = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.78f));
                var settle = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((progress - 0.78f) / 0.22f));
                var ratio = progress < 0.78f
                    ? Mathf.Lerp(0.35f, 1.06f, firstPart)
                    : Mathf.Lerp(1.06f, 1f, settle);
                ApplyGrowthRatio(incoming, ratio);

                if (outgoing != null && outgoing != incoming)
                {
                    if (progress < 0.28f)
                        ApplyGrowthRatio(outgoing, Mathf.Lerp(1f, 0.80f,
                            Mathf.SmoothStep(0f, 1f, progress / 0.28f)));
                    else if (outgoing.activeSelf)
                        outgoing.SetActive(false);
                }
                yield return null;
            }
            growthRoutine = null;
            SetStage(Stage);
        }

        private void OnDisable()
        {
            if (growthRoutine != null) SetStage(Stage);
        }

        private IEnumerable<GameObject> Roots()
        {
            yield return felled;
            yield return sapling;
            yield return young;
            yield return mature;
        }

        private GameObject RootFor(OakTreeStage stage)
        {
            return stage switch
            {
                OakTreeStage.Felled => felled,
                OakTreeStage.Sapling => sapling,
                OakTreeStage.Young => young,
                _ => mature
            };
        }

        private void ApplyGrowthRatio(GameObject root, float ratio)
        {
            if (root == null || !originalScales.TryGetValue(root, out var baseScale)) return;
            root.transform.localScale = baseScale * ratio;
            root.transform.localPosition = originalPositions[root] +
                Vector3.Scale(growthAnchor, baseScale) * (1f - ratio);
        }

        private void RestoreTransform(GameObject root)
        {
            if (root == null || !originalScales.TryGetValue(root, out var baseScale)) return;
            root.transform.localScale = baseScale;
            root.transform.localPosition = originalPositions[root];
        }
    }
}
