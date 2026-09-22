using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    public sealed class CircularMeterGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float value = 0.75f;
        [SerializeField] private Color trackColor = new(0.12f, 0.14f, 0.16f, 0.32f);
        [SerializeField] private Color fillColor = new(0.30f, 0.76f, 0.72f, 1f);
        [SerializeField, Range(2f, 18f)] private float thickness = 6f;

        public float Value => value;

        public void SetValue(float newValue, Color newFillColor)
        {
            value = Mathf.Clamp01(newValue);
            fillColor = newFillColor;
            SetVerticesDirty();
        }

        public void AnimateTo(float newValue, Color newFillColor)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateValue(Mathf.Clamp01(newValue), newFillColor));
        }

        private System.Collections.IEnumerator AnimateValue(float target, Color targetColor)
        {
            var startValue = value;
            var startColor = fillColor;
            var elapsed = 0f;
            const float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                value = Mathf.Lerp(startValue, target, progress);
                fillColor = Color.Lerp(startColor, targetColor, progress);
                SetVerticesDirty();
                yield return null;
            }
            value = target;
            fillColor = targetColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            var inner = Mathf.Max(0f, radius - thickness);
            AddArc(vh, 0f, 1f, trackColor, radius, inner, 64);
            AddArc(vh, 0f, value, fillColor, radius, inner, Mathf.Max(1, Mathf.CeilToInt(64 * value)));
        }

        private static void AddArc(
            VertexHelper vh,
            float from,
            float to,
            Color color,
            float outerRadius,
            float innerRadius,
            int segments)
        {
            if (to <= from || segments <= 0)
            {
                return;
            }

            for (var segment = 0; segment < segments; segment++)
            {
                var a = Mathf.Lerp(from, to, segment / (float)segments);
                var b = Mathf.Lerp(from, to, (segment + 1f) / segments);
                var angleA = (90f - a * 360f) * Mathf.Deg2Rad;
                var angleB = (90f - b * 360f) * Mathf.Deg2Rad;
                var outerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outerRadius;
                var outerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outerRadius;
                var innerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;
                var innerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;
                var start = vh.currentVertCount;
                vh.AddVert(outerA, color, Vector2.zero);
                vh.AddVert(outerB, color, Vector2.zero);
                vh.AddVert(innerB, color, Vector2.zero);
                vh.AddVert(innerA, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
