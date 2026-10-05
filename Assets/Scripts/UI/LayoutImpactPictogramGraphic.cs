using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // Small, resolution-independent pictograms for layout consequences.
    // Geometry stays inside a square safe area, so no imported sprite can be stretched.
    public sealed class LayoutImpactPictogramGraphic : MaskableGraphic
    {
        [SerializeField] private LayoutImpactMetricKind kind;

        public LayoutImpactMetricKind Kind
        {
            get => kind;
            set
            {
                if (kind == value) return;
                kind = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var side = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            if (side <= 0f) return;

            var center = rectTransform.rect.center;
            var ivory = color;
            var accent = new Color(0.35f, 0.85f, 0.79f, color.a);
            Vector2 Point(float x, float y) => center + new Vector2(x, y) * side;
            void Line(float x0, float y0, float x1, float y1, Color tint = default)
            {
                AddLine(vh, Point(x0, y0), Point(x1, y1), side * 0.052f,
                    tint == default ? ivory : tint);
            }
            void Ring(float x, float y, float radius, Color tint = default)
            {
                AddRing(vh, Point(x, y), radius * side, side * 0.052f,
                    tint == default ? ivory : tint);
            }
            void Oval(float x, float y, float rx, float ry, Color tint)
            {
                AddOval(vh, Point(x, y), rx * side, ry * side, tint);
            }
            void Box(float left, float bottom, float right, float top, Color tint = default)
            {
                Line(left, bottom, right, bottom, tint);
                Line(right, bottom, right, top, tint);
                Line(right, top, left, top, tint);
                Line(left, top, left, bottom, tint);
            }

            switch (kind)
            {
                case LayoutImpactMetricKind.Seeds:
                    Line(-0.36f, -0.33f, 0.36f, -0.33f);
                    Line(0f, -0.31f, 0f, 0.17f, accent);
                    Oval(-0.15f, 0.13f, 0.17f, 0.082f, accent);
                    Oval(0.16f, 0.25f, 0.17f, 0.082f, accent);
                    Oval(-0.20f, -0.28f, 0.035f, 0.035f, ivory);
                    Oval(0.22f, -0.28f, 0.035f, 0.035f, ivory);
                    break;

                case LayoutImpactMetricKind.ParkLinks:
                    Line(-0.25f, -0.22f, 0f, 0.23f, accent);
                    Line(0f, 0.23f, 0.25f, -0.22f, accent);
                    Line(-0.25f, -0.22f, 0.25f, -0.22f, accent);
                    Ring(-0.25f, -0.22f, 0.085f);
                    Ring(0f, 0.23f, 0.085f);
                    Ring(0.25f, -0.22f, 0.085f);
                    break;

                case LayoutImpactMetricKind.GreenCells:
                    Line(-0.25f, -0.22f, -0.25f, 0.20f, accent);
                    Line(-0.25f, 0.20f, 0.22f, 0.20f, accent);
                    Line(0.22f, 0.20f, 0.22f, -0.22f, accent);
                    Line(0.22f, -0.22f, -0.25f, -0.22f, accent);
                    Ring(-0.25f, -0.22f, 0.09f);
                    Ring(-0.25f, 0.20f, 0.09f);
                    Ring(0.22f, 0.20f, 0.09f);
                    Ring(0.22f, -0.22f, 0.09f);
                    Oval(0f, 0f, 0.12f, 0.16f, ivory);
                    break;

                case LayoutImpactMetricKind.AnimalPassages:
                    Line(-0.37f, 0.0f, -0.09f, 0.0f, accent);
                    Line(0.09f, 0.0f, 0.37f, 0.0f, accent);
                    Ring(-0.30f, 0.0f, 0.11f);
                    Ring(0.30f, 0.0f, 0.11f);
                    Oval(0f, -0.08f, 0.11f, 0.09f, ivory);
                    Oval(-0.13f, 0.11f, 0.04f, 0.06f, ivory);
                    Oval(-0.045f, 0.18f, 0.04f, 0.06f, ivory);
                    Oval(0.045f, 0.18f, 0.04f, 0.06f, ivory);
                    Oval(0.13f, 0.11f, 0.04f, 0.06f, ivory);
                    break;

                case LayoutImpactMetricKind.PigeonSeedMealCeiling:
                case LayoutImpactMetricKind.PigeonFoodAccess:
                    Oval(-0.05f, -0.04f, 0.27f, 0.23f, ivory);
                    Ring(0.05f, 0.05f, 0.045f, accent);
                    Line(0.20f, -0.06f, 0.39f, -0.13f, accent);
                    Line(0.39f, -0.13f, 0.20f, -0.18f, accent);
                    Line(-0.28f, -0.29f, -0.02f, -0.29f);
                    break;

                case LayoutImpactMetricKind.SquirrelFoodAccess:
                    Ring(0.02f, -0.08f, 0.20f);
                    Ring(-0.20f, 0.14f, 0.15f, accent);
                    Oval(0.13f, 0.20f, 0.09f, 0.13f, ivory);
                    Ring(0.11f, -0.01f, 0.03f, accent);
                    Line(0.16f, -0.28f, 0.36f, -0.28f);
                    break;

                case LayoutImpactMetricKind.HedgehogFoodAccess:
                    Oval(0f, -0.15f, 0.32f, 0.17f, ivory);
                    for (var spine = -2; spine <= 2; spine++)
                        Line(spine * 0.13f, 0.01f,
                            spine * 0.13f - 0.06f, 0.24f, accent);
                    Ring(0.19f, -0.12f, 0.03f, accent);
                    break;

                case LayoutImpactMetricKind.FoxPreyAccess:
                    Line(-0.30f, 0.32f, -0.14f, -0.06f, accent);
                    Line(-0.30f, 0.32f, -0.02f, 0.11f, accent);
                    Line(0.30f, 0.32f, 0.14f, -0.06f, accent);
                    Line(0.30f, 0.32f, 0.02f, 0.11f, accent);
                    Line(-0.25f, -0.08f, 0f, -0.33f);
                    Line(0.25f, -0.08f, 0f, -0.33f);
                    Line(-0.25f, -0.08f, -0.02f, 0.11f);
                    Line(0.25f, -0.08f, 0.02f, 0.11f);
                    Oval(0f, -0.31f, 0.06f, 0.04f, accent);
                    break;

                case LayoutImpactMetricKind.Workers:
                    Line(-0.40f, 0.04f, -0.19f, 0.25f);
                    Line(-0.19f, 0.25f, 0.02f, 0.04f);
                    Box(-0.34f, -0.27f, -0.04f, 0.04f);
                    Line(-0.19f, -0.27f, -0.19f, -0.08f, accent);
                    Line(0.04f, -0.13f, 0.12f, -0.13f, accent);
                    Line(0.19f, -0.13f, 0.27f, -0.13f, accent);
                    Box(0.29f, -0.27f, 0.44f, 0.18f);
                    Line(0.30f, 0.03f, 0.43f, 0.03f);
                    break;

                case LayoutImpactMetricKind.Production:
                    Box(-0.36f, -0.32f, -0.20f, -0.13f);
                    Box(-0.09f, -0.32f, 0.07f, 0.04f);
                    Box(0.18f, -0.32f, 0.34f, 0.19f);
                    Line(-0.29f, 0.22f, 0.04f, 0.31f, accent);
                    Line(0.04f, 0.31f, 0.30f, 0.31f, accent);
                    Line(0.30f, 0.31f, 0.20f, 0.39f, accent);
                    break;

                case LayoutImpactMetricKind.Waste:
                    DrawWasteBin(Line, ivory);
                    break;

                case LayoutImpactMetricKind.Shelter:
                    DrawShrub(Line, Ring, Oval, ivory, accent);
                    break;

                case LayoutImpactMetricKind.MarketWaste:
                    DrawWasteBin(Line, ivory);
                    Ring(0.29f, 0.31f, 0.10f, accent);
                    Line(0.21f, 0.31f, 0.36f, 0.31f, accent);
                    break;

                case LayoutImpactMetricKind.ShrubRecovery:
                    DrawShrub(Line, Ring, Oval, ivory, accent);
                    Ring(0.29f, 0.27f, 0.14f, ivory);
                    Line(0.29f, 0.27f, 0.29f, 0.34f, accent);
                    Line(0.29f, 0.27f, 0.35f, 0.22f, accent);
                    break;

                case LayoutImpactMetricKind.BufferedGarages:
                    Box(-0.32f, -0.25f, 0.32f, 0.16f);
                    Line(-0.37f, 0.16f, 0f, 0.37f, accent);
                    Line(0f, 0.37f, 0.37f, 0.16f, accent);
                    Oval(0f, -0.03f, 0.10f, 0.15f, accent);
                    break;
            }
        }

        private static void DrawWasteBin(System.Action<float, float, float, float, Color> line,
            Color ivory)
        {
            line(-0.27f, 0.22f, 0.27f, 0.22f, ivory);
            line(-0.12f, 0.31f, 0.12f, 0.31f, ivory);
            line(-0.12f, 0.31f, -0.12f, 0.22f, ivory);
            line(0.12f, 0.31f, 0.12f, 0.22f, ivory);
            line(-0.23f, 0.16f, -0.19f, -0.32f, ivory);
            line(0.23f, 0.16f, 0.19f, -0.32f, ivory);
            line(-0.19f, -0.32f, 0.19f, -0.32f, ivory);
            line(-0.08f, 0.09f, -0.08f, -0.23f, ivory);
            line(0.08f, 0.09f, 0.08f, -0.23f, ivory);
        }

        private static void DrawShrub(System.Action<float, float, float, float, Color> line,
            System.Action<float, float, float, Color> ring,
            System.Action<float, float, float, float, Color> oval, Color ivory, Color accent)
        {
            line(-0.38f, -0.30f, 0.38f, -0.30f, ivory);
            oval(-0.24f, -0.05f, 0.15f, 0.16f, accent);
            oval(-0.05f, 0.09f, 0.18f, 0.20f, accent);
            oval(0.15f, -0.04f, 0.15f, 0.17f, accent);
            ring(-0.05f, -0.17f, 0.08f, ivory);
        }

        private static void AddLine(VertexHelper vh, Vector2 start, Vector2 end,
            float thickness, Color tint)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.0001f) return;
            var normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
            var first = vh.currentVertCount;
            vh.AddVert(start + normal, tint, Vector2.zero);
            vh.AddVert(start - normal, tint, Vector2.zero);
            vh.AddVert(end - normal, tint, Vector2.zero);
            vh.AddVert(end + normal, tint, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first, first + 2, first + 3);
        }

        private static void AddRing(VertexHelper vh, Vector2 center, float radius,
            float thickness, Color tint)
        {
            const int steps = 18;
            for (var index = 0; index < steps; index++)
            {
                var first = index * Mathf.PI * 2f / steps;
                var second = (index + 1) * Mathf.PI * 2f / steps;
                AddLine(vh,
                    center + new Vector2(Mathf.Cos(first), Mathf.Sin(first)) * radius,
                    center + new Vector2(Mathf.Cos(second), Mathf.Sin(second)) * radius,
                    thickness, tint);
            }
        }

        private static void AddOval(VertexHelper vh, Vector2 center, float rx, float ry, Color tint)
        {
            const int steps = 20;
            var first = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            for (var index = 0; index <= steps; index++)
            {
                var angle = index * Mathf.PI * 2f / steps;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry),
                    tint, Vector2.zero);
                if (index > 0) vh.AddTriangle(first, first + index, first + index + 1);
            }
        }
    }
}
