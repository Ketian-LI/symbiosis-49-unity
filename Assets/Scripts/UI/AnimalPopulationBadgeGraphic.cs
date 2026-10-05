using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // The portrait and live count stay as separate UI elements; only the frame is drawn here.
    public sealed class AnimalPopulationBadgeGraphic : MaskableGraphic
    {
        private static readonly Color Paper = new(0.94f, 0.88f, 0.77f, 1f);
        private static readonly Color Charcoal = new(0.16f, 0.17f, 0.18f, 0.98f);
        private static readonly Color Track = new(0.26f, 0.27f, 0.28f, 1f);

        [SerializeField] private Color accent = new(0.42f, 0.68f, 0.96f, 1f);
        [SerializeField, Range(0f, 1f)] private float livingFraction = 1f;
        [SerializeField, Range(0f, 1f)] private float arcFraction = 0.8f;

        public float LivingFraction => livingFraction;
        public float ArcFraction => arcFraction;

        public void SetPopulation(Color newAccent, int living, int total, float overrideFraction = -1f)
        {
            accent = newAccent;
            livingFraction = total > 0 ? Mathf.Clamp01(living / (float)total) : 0f;
            arcFraction = 0.80f * (overrideFraction >= 0f ? Mathf.Clamp01(overrideFraction) : livingFraction);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var center = new Vector2(0f, 11f);
            AddDisc(vh, center, 40f, Paper, 64);
            AddDisc(vh, center, 37f, Charcoal, 64);
            AddArc(vh, center, 34f, 28f, 1f, Track);
            AddArc(vh, center, 34f, 28f, arcFraction, accent);
            AddDisc(vh, center, 27f, Charcoal, 64);
            AddRoundedRect(vh, new Vector2(0f, -33f), 53f, 30f, 14f, Paper);
            AddRoundedRect(vh, new Vector2(0f, -33f), 48f, 25f, 12f, Charcoal);
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
        {
            var start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
            for (var i = 0; i < segments; i++)
            {
                vh.AddTriangle(start, start + i + 1, start + i + 2);
            }
        }

        private static void AddArc(VertexHelper vh, Vector2 center, float outer, float inner, float fraction, Color color)
        {
            if (fraction <= 0f)
            {
                return;
            }
            var segments = Mathf.Max(2, Mathf.CeilToInt(64f * fraction));
            for (var i = 0; i < segments; i++)
            {
                var a = (90f - 360f * fraction * i / segments) * Mathf.Deg2Rad;
                var b = (90f - 360f * fraction * (i + 1f) / segments) * Mathf.Deg2Rad;
                var outerA = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer;
                var outerB = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * outer;
                var innerA = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner;
                var innerB = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * inner;
                var start = vh.currentVertCount;
                vh.AddVert(outerA, color, Vector2.zero);
                vh.AddVert(outerB, color, Vector2.zero);
                vh.AddVert(innerB, color, Vector2.zero);
                vh.AddVert(innerA, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static void AddRoundedRect(VertexHelper vh, Vector2 center, float width, float height, float radius, Color color)
        {
            const int stepsPerCorner = 6;
            var start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            var cornerRadius = Mathf.Min(radius, width * 0.5f, height * 0.5f);
            var halfWidth = width * 0.5f - cornerRadius;
            var halfHeight = height * 0.5f - cornerRadius;
            for (var corner = 0; corner < 4; corner++)
            {
                var cornerCenter = center + new Vector2(
                    corner == 0 || corner == 3 ? halfWidth : -halfWidth,
                    corner < 2 ? halfHeight : -halfHeight);
                for (var step = 0; step <= stepsPerCorner; step++)
                {
                    var angle = (corner * 90f + step * 90f / stepsPerCorner) * Mathf.Deg2Rad;
                    vh.AddVert(cornerCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cornerRadius,
                        color, Vector2.zero);
                }
            }
            var perimeter = 4 * (stepsPerCorner + 1);
            for (var i = 0; i < perimeter; i++)
            {
                vh.AddTriangle(start, start + i + 1, start + (i + 1) % perimeter + 1);
            }
        }
    }
}
