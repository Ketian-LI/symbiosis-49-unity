using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // A shovel lifting a floor tile. Built as UI geometry so the mark stays
    // crisp and keeps its proportions at every game-window aspect ratio.
    public sealed class RoomMoveIconGraphic : MaskableGraphic
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

            // The square is drawn in perspective only within the pictogram;
            // the actual room action remains a one-cell layout swap.
            Quad(vh, Point(0.02f, -0.13f), Point(0.30f, -0.05f),
                Point(0.43f, -0.31f), Point(0.14f, -0.39f), teal);
            Stroke(vh, Point(0.02f, -0.13f), Point(0.30f, -0.05f), side * 0.045f, ivory);
            Stroke(vh, Point(0.30f, -0.05f), Point(0.43f, -0.31f), side * 0.045f, ivory);
            Stroke(vh, Point(0.43f, -0.31f), Point(0.14f, -0.39f), side * 0.045f, ivory);

            // Rounded D grip, long shaft, and a broad pointed spade.
            Stroke(vh, Point(-0.34f, 0.28f), Point(-0.27f, 0.40f), side * 0.07f, ivory);
            Stroke(vh, Point(-0.27f, 0.40f), Point(-0.15f, 0.36f), side * 0.07f, ivory);
            Stroke(vh, Point(-0.15f, 0.36f), Point(-0.09f, 0.25f), side * 0.07f, ivory);
            Stroke(vh, Point(-0.09f, 0.25f), Point(-0.34f, 0.28f), side * 0.055f, ivory);
            Stroke(vh, Point(-0.22f, 0.26f), Point(-0.03f, -0.15f), side * 0.075f, ivory);
            Quad(vh, Point(-0.13f, -0.15f), Point(0.05f, -0.20f),
                Point(-0.01f, -0.37f), Point(-0.20f, -0.29f), ivory);
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
