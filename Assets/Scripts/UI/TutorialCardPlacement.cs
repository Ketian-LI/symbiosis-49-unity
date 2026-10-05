using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    // Keeps a guide card next to its current highlight, with a fallback for
    // narrow screens where neither side has enough room.
    public static class TutorialCardPlacement
    {
        public static Vector2 Choose(Rect viewport, Vector2 cardSize, Vector2? focus,
            float focusRadius, float margin = 20f)
        {
            var half = cardSize * 0.5f;
            var minX = viewport.xMin + margin + half.x;
            var maxX = viewport.xMax - margin - half.x;
            var minY = viewport.yMin + margin + half.y;
            var maxY = viewport.yMax - margin - half.y;
            if (minX > maxX || minY > maxY)
            {
                return viewport.center;
            }

            if (!focus.HasValue)
            {
                return new Vector2(viewport.center.x, minY);
            }

            var point = focus.Value;
            var offsetX = half.x + focusRadius + 26f;
            var offsetY = half.y + focusRadius + 26f;
            var nearHorizontalCenter = Mathf.Abs(point.x - viewport.center.x) < viewport.width * 0.18f;
            var nearVerticalEdge = Mathf.Abs(point.y - viewport.center.y) > viewport.height * 0.22f;
            var preferVertical = nearHorizontalCenter && nearVerticalEdge;

            if (preferVertical)
            {
                if (TryVertical(point.y > viewport.center.y ? -1 : 1, out var vertical) ||
                    TryVertical(point.y > viewport.center.y ? 1 : -1, out vertical))
                {
                    return vertical;
                }
            }

            if (TryHorizontal(point.x <= viewport.center.x ? 1 : -1, out var horizontal) ||
                TryHorizontal(point.x <= viewport.center.x ? -1 : 1, out horizontal))
            {
                return horizontal;
            }

            if (!preferVertical &&
                (TryVertical(point.y > viewport.center.y ? -1 : 1, out var laterVertical) ||
                 TryVertical(point.y > viewport.center.y ? 1 : -1, out laterVertical)))
            {
                return laterVertical;
            }

            // A tiny window can make all adjacent positions impossible. Use
            // the visible corner with the most space around the highlight.
            var corners = new[]
            {
                new Vector2(minX, minY), new Vector2(maxX, minY),
                new Vector2(minX, maxY), new Vector2(maxX, maxY)
            };
            var best = corners[0];
            var bestClearance = -1f;
            foreach (var corner in corners)
            {
                var clearance = Clearance(point, corner, half);
                if (clearance <= bestClearance)
                {
                    continue;
                }
                best = corner;
                bestClearance = clearance;
            }
            return best;

            bool TryHorizontal(int direction, out Vector2 position)
            {
                position = new Vector2(point.x + direction * offsetX,
                    Mathf.Clamp(point.y, minY, maxY));
                return position.x >= minX && position.x <= maxX;
            }

            bool TryVertical(int direction, out Vector2 position)
            {
                position = new Vector2(Mathf.Clamp(point.x, minX, maxX),
                    point.y + direction * offsetY);
                return position.y >= minY && position.y <= maxY;
            }
        }

        private static float Clearance(Vector2 point, Vector2 cardCenter, Vector2 halfSize)
        {
            var nearest = new Vector2(
                Mathf.Clamp(point.x, cardCenter.x - halfSize.x, cardCenter.x + halfSize.x),
                Mathf.Clamp(point.y, cardCenter.y - halfSize.y, cardCenter.y + halfSize.y));
            return Vector2.Distance(point, nearest);
        }
    }
}
