using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform idleCueLayer;
        private Sprite idleCueSprite;
        private Texture2D idleCueTexture;
        private Coroutine idleCueAnimation;
        private readonly List<GameObject> idleCueObjects = new();

        private void BuildIdleCueLayer(Transform canvas)
        {
            idleCueLayer = NewRect("Idle cue overlay", canvas,
                Vector2.zero, Vector2.zero);
            Stretch(idleCueLayer);
        }

        private void StartIdleCues(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human)
        {
            ClearIdleCues();
            if (run?.UsesFillHand != true || idleCueLayer == null ||
                worldView?.IsReady != true) return;
            var actors = worldView.IdleCueActorsForDay(ecology, human);
            if (actors.Count > 0)
                idleCueAnimation = StartCoroutine(AnimateIdleCues(actors));
        }

        private IEnumerator AnimateIdleCues(IReadOnlyList<Transform> actors)
        {
            if (idleCueSprite == null) CreateIdleCueSprite();
            var cues = new List<(Transform actor, RectTransform rect,
                Image image)>();
            foreach (var actor in actors)
            {
                if (actor == null) continue;
                var icon = new GameObject($"Idle cue {actor.name}",
                    typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(idleCueLayer, false);
                idleCueObjects.Add(icon);
                var rect = icon.GetComponent<RectTransform>();
                SetRect(rect, Vector2.zero, new Vector2(40f, 40f));
                var image = icon.GetComponent<Image>();
                image.sprite = idleCueSprite;
                image.raycastTarget = false;
                cues.Add((actor, rect, image));
            }

            const float duration = 1.3f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var fraction = Mathf.Clamp01(elapsed / duration);
                var rise = Mathf.SmoothStep(0f, 1f, fraction);
                var alpha = Mathf.Clamp01(fraction * 10f) *
                    Mathf.Clamp01((1f - fraction) * 4f);
                var camera = worldView?.BoardCamera;
                foreach (var cue in cues)
                {
                    if (cue.actor == null ||
                        !cue.actor.gameObject.activeInHierarchy || camera == null)
                    {
                        cue.image.gameObject.SetActive(false);
                        continue;
                    }
                    var screen = camera.WorldToScreenPoint(cue.actor.position);
                    if (screen.z <= 0f ||
                        !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            idleCueLayer, screen, null, out var position))
                    {
                        cue.image.gameObject.SetActive(false);
                        continue;
                    }
                    position.y += 24f + 42f * rise;
                    cue.rect.anchoredPosition = position;
                    cue.rect.localScale = Vector3.one *
                        Mathf.Lerp(0.82f, 1.08f, rise);
                    cue.image.color = new Color(1f, 1f, 1f, alpha);
                }
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                yield return null;
            }
            RemoveIdleCueObjects();
            idleCueAnimation = null;
        }

        private void ClearIdleCues()
        {
            if (idleCueAnimation != null)
            {
                StopCoroutine(idleCueAnimation);
                idleCueAnimation = null;
            }
            RemoveIdleCueObjects();
        }

        private void RemoveIdleCueObjects()
        {
            foreach (var icon in idleCueObjects)
                if (icon != null)
                {
                    icon.SetActive(false);
                    Destroy(icon);
                }
            idleCueObjects.Clear();
        }

        private void CreateIdleCueSprite()
        {
            const int size = 48;
            idleCueTexture = new Texture2D(size, size, TextureFormat.RGBA32,
                false)
            {
                name = "Idle sad face",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var ink = new Color32(31, 50, 65, 255);
            var tear = new Color32(80, 141, 168, 255);
            for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
            {
                var x = (column - 23.5f) / 21f;
                var y = (row - 23.5f) / 21f;
                var ring = Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.82f) <
                    0.075f;
                var leftEye = (x + 0.29f) * (x + 0.29f) +
                    (y - 0.16f) * (y - 0.16f) < 0.011f;
                var rightEye = (x - 0.29f) * (x - 0.29f) +
                    (y - 0.16f) * (y - 0.16f) < 0.011f;
                var frown = Mathf.Abs(x) < 0.47f &&
                    Mathf.Abs(y - (-0.43f + 0.47f * x * x)) < 0.065f;
                var fallingTear = x > 0.36f && x < 0.51f &&
                    y < 0.12f && y > -0.18f;
                idleCueTexture.SetPixel(column, row, fallingTear ? tear :
                    ring || leftEye || rightEye || frown ? ink : Color.clear);
            }
            idleCueTexture.Apply();
            idleCueSprite = Sprite.Create(idleCueTexture,
                new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
