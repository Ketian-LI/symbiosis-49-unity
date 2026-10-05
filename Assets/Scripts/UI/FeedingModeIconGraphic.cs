using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // Food falling into a shallow dish. Shares the planning icon's ivory
    // strokes and teal accent, without a resolution-dependent sprite.
    public sealed class FeedingModeIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var side = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            if (side <= 0f) return;

            var origin = rectTransform.rect.center;
            var ivory = color;
            var teal = new Color(0.31f, 0.83f, 0.77f, color.a);
            Vector2 Point(float x, float y) => origin + new Vector2(x, y) * side * 1.12f;

            // Three falling pieces keep the action legible at HUD scale.
            Grain(vh, Point(-0.18f, 0.25f), side * 0.070f, teal);
            Grain(vh, Point(0.02f, 0.34f), side * 0.076f, teal);
            Grain(vh, Point(0.17f, 0.16f), side * 0.066f, teal);

            // A broad, outlined bowl has the same visual weight as the shovel.
            Quad(vh, Point(-0.34f, -0.10f), Point(0.34f, -0.10f),
                Point(0.23f, -0.36f), Point(-0.23f, -0.36f), ivory);
            Quad(vh, Point(-0.28f, -0.14f), Point(0.28f, -0.14f),
                Point(0.19f, -0.29f), Point(-0.19f, -0.29f), teal);
            Ellipse(vh, origin + new Vector2(0f, -0.10f) * side * 1.12f,
                side * 0.39f, side * 0.10f, side * 0.052f, ivory);
        }

        private static void Grain(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            Quad(vh,
                center + new Vector2(0f, radius * 1.35f),
                center + new Vector2(radius, 0f),
                center + new Vector2(0f, -radius),
                center + new Vector2(-radius, 0f), tint);
        }

        private static void Ellipse(VertexHelper vh, Vector2 center,
            float radiusX, float radiusY, float thickness, Color tint)
        {
            const int segments = 20;
            for (var index = 0; index < segments; index++)
            {
                var startAngle = index * Mathf.PI * 2f / segments;
                var endAngle = (index + 1) * Mathf.PI * 2f / segments;
                var start = center + new Vector2(
                    Mathf.Cos(startAngle) * radiusX, Mathf.Sin(startAngle) * radiusY);
                var end = center + new Vector2(
                    Mathf.Cos(endAngle) * radiusX, Mathf.Sin(endAngle) * radiusY);
                Stroke(vh, start, end, thickness, tint);
            }
        }

        private static void Stroke(VertexHelper vh, Vector2 start, Vector2 end,
            float thickness, Color tint)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.0001f) return;
            var normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
            Quad(vh, start + normal, end + normal, end - normal, start - normal, tint);
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b,
            Vector2 c, Vector2 d, Color tint)
        {
            var index = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero);
            vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero);
            vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(index, index + 2, index + 1);
            vh.AddTriangle(index, index + 3, index + 2);
        }
    }
}
