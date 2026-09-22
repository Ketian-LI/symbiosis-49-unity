using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public sealed class DayNightDialGraphic : MaskableGraphic
    {
        private static readonly Color Dawn = new(0.93f, 0.68f, 0.55f, 1f);
        private static readonly Color Day = new(0.88f, 0.70f, 0.28f, 1f);
        private static readonly Color Dusk = new(0.73f, 0.36f, 0.25f, 1f);
        private static readonly Color Night = new(0.18f, 0.24f, 0.34f, 1f);
        private static readonly Color Centre = new(0.94f, 0.90f, 0.80f, 1f);

        [SerializeField, Range(0f, 1f)] private float progress;
        [SerializeField] private DayPhase phase;

        public void SetTime(float cycleProgress, DayPhase currentPhase)
        {
            progress = Mathf.Repeat(cycleProgress, 1f);
            phase = currentPhase;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            var inner = radius - 12f;
            var dawnEnd = (float)(SimulationClockModel.DawnSeconds / SimulationClockModel.CycleSeconds);
            var dayEnd = (float)((SimulationClockModel.DawnSeconds + SimulationClockModel.DaySeconds) / SimulationClockModel.CycleSeconds);
            var duskEnd = (float)((SimulationClockModel.DawnSeconds + SimulationClockModel.DaySeconds + SimulationClockModel.DuskSeconds) / SimulationClockModel.CycleSeconds);

            AddArc(vh, 0f, dawnEnd, Highlight(Dawn, DayPhase.Dawn), radius, inner);
            AddArc(vh, dawnEnd, dayEnd, Highlight(Day, DayPhase.Day), radius, inner);
            AddArc(vh, dayEnd, duskEnd, Highlight(Dusk, DayPhase.Dusk), radius, inner);
            AddArc(vh, duskEnd, 1f, Highlight(Night, DayPhase.Night), radius, inner);
            AddDisc(vh, inner - 4f, Centre, 40);
            AddPointer(vh, radius + 3f);
        }

        private Color Highlight(Color source, DayPhase target)
        {
            if (phase != target)
            {
                source.a = 0.48f;
                return source;
            }

            return Color.Lerp(source, Color.white, 0.18f);
        }

        private static void AddArc(VertexHelper vh, float from, float to, Color color, float outer, float inner)
        {
            var segments = Mathf.Max(2, Mathf.CeilToInt((to - from) * 72f));
            for (var segment = 0; segment < segments; segment++)
            {
                var a = Mathf.Lerp(from, to, segment / (float)segments);
                var b = Mathf.Lerp(from, to, (segment + 1f) / segments);
                var angleA = (90f - a * 360f) * Mathf.Deg2Rad;
                var angleB = (90f - b * 360f) * Mathf.Deg2Rad;
                var outerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outer;
                var outerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outer;
                var innerA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * inner;
                var innerB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * inner;
                var start = vh.currentVertCount;
                vh.AddVert(outerA, color, Vector2.zero);
                vh.AddVert(outerB, color, Vector2.zero);
                vh.AddVert(innerB, color, Vector2.zero);
                vh.AddVert(innerA, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static void AddDisc(VertexHelper vh, float radius, Color color, int segments)
        {
            var center = vh.currentVertCount;
            vh.AddVert(Vector2.zero, color, Vector2.zero);
            for (var index = 0; index <= segments; index++)
            {
                var angle = (90f - index / (float)segments * 360f) * Mathf.Deg2Rad;
                vh.AddVert(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }

            for (var index = 0; index < segments; index++)
            {
                vh.AddTriangle(center, center + index + 1, center + index + 2);
            }
        }

        private void AddPointer(VertexHelper vh, float radius)
        {
            var angle = (90f - progress * 360f) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var tangent = new Vector2(-direction.y, direction.x);
            var tip = direction * radius;
            var baseCenter = direction * (radius - 13f);
            var pointerColor = phase == DayPhase.Night
                ? new Color(0.88f, 0.91f, 0.96f)
                : new Color(1f, 0.88f, 0.48f);
            var start = vh.currentVertCount;
            vh.AddVert(tip, pointerColor, Vector2.zero);
            vh.AddVert(baseCenter + tangent * 5f, pointerColor, Vector2.zero);
            vh.AddVert(baseCenter - tangent * 5f, pointerColor, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
