using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // A resolution-independent rounded panel for the UI. The outline
    // is a ring, so translucent fills do not wash out its inner edge.
    public sealed class RoundedPanelGraphic : MaskableGraphic
    {
        private const int StepsPerCorner = 8;
        [SerializeField] private float cornerRadius = 36f;
        [SerializeField] private float borderWidth = 2f;
        [SerializeField] private Color borderColor = new(0.96f, 0.92f, 0.82f, 0.8f);

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float BorderWidth
        {
            get => borderWidth;
            set
            {
                borderWidth = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var outer = GetPixelAdjustedRect();
            if (outer.width <= 0f || outer.height <= 0f)
            {
                return;
            }

            var radius = Mathf.Min(cornerRadius, outer.width * 0.5f, outer.height * 0.5f);
            var border = Mathf.Min(borderWidth, outer.width * 0.5f, outer.height * 0.5f);
            var inner = Rect.MinMaxRect(
                outer.xMin + border, outer.yMin + border,
                outer.xMax - border, outer.yMax - border);
            var innerRadius = Mathf.Max(0f, radius - border);
            var perimeterCount = 4 * (StepsPerCorner + 1);

            if (border > 0f && inner.width > 0f && inner.height > 0f)
            {
                for (var index = 0; index < perimeterCount; index++)
                {
                    vh.AddVert(PerimeterPoint(outer, radius, index), borderColor, Vector2.zero);
                    vh.AddVert(PerimeterPoint(inner, innerRadius, index), borderColor, Vector2.zero);
                }
                for (var index = 0; index < perimeterCount; index++)
                {
                    var next = (index + 1) % perimeterCount;
                    var outerIndex = index * 2;
                    var innerIndex = outerIndex + 1;
                    var nextOuter = next * 2;
                    var nextInner = nextOuter + 1;
                    vh.AddTriangle(outerIndex, nextOuter, nextInner);
                    vh.AddTriangle(outerIndex, nextInner, innerIndex);
                }
            }
            else
            {
                inner = outer;
                innerRadius = radius;
            }

            var centerIndex = vh.currentVertCount;
            vh.AddVert(inner.center, color, Vector2.zero);
            for (var index = 0; index < perimeterCount; index++)
            {
                vh.AddVert(PerimeterPoint(inner, innerRadius, index), color, Vector2.zero);
            }
            for (var index = 0; index < perimeterCount; index++)
            {
                vh.AddTriangle(centerIndex, centerIndex + index + 1,
                    centerIndex + (index + 1) % perimeterCount + 1);
            }
        }

        private static Vector2 PerimeterPoint(Rect rect, float radius, int index)
        {
            var corner = index / (StepsPerCorner + 1);
            var step = index % (StepsPerCorner + 1);
            var center = new Vector2(
                corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                corner < 2 ? rect.yMax - radius : rect.yMin + radius);
            var angle = (corner * 90f + step * 90f / StepsPerCorner) * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}
