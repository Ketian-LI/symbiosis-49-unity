using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // A resolution-independent heartbeat mark for the fifth gameplay indicator.
    public sealed class PulseIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;
            var points = new[]
            {
                new Vector2(-0.43f, 0f),
                new Vector2(-0.26f, 0f),
                new Vector2(-0.15f, 0.14f),
                new Vector2(-0.04f, -0.24f),
                new Vector2(0.09f, 0.31f),
                new Vector2(0.20f, -0.06f),
                new Vector2(0.29f, 0f),
                new Vector2(0.43f, 0f)
            };
            for (var index = 0; index < points.Length - 1; index++)
            {
                var start = new Vector2(points[index].x * width, points[index].y * height);
                var end = new Vector2(points[index + 1].x * width, points[index + 1].y * height);
                AddSegment(vh, start, end, Mathf.Max(2f, width * 0.065f));
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 start, Vector2 end, float thickness)
        {
            var direction = (end - start).normalized;
            var normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
            var vertex = vh.currentVertCount;
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddTriangle(vertex, vertex + 1, vertex + 2);
            vh.AddTriangle(vertex, vertex + 2, vertex + 3);
        }
    }
}
