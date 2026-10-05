using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    // A circular backdrop keeps indicator badges circular at every canvas scale.
    public sealed class DiscGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int segments = 48;
            var radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            vh.AddVert(Vector2.zero, color, Vector2.zero);
            for (var index = 0; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                vh.AddVert(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    color, Vector2.zero);
                if (index > 0)
                {
                    vh.AddTriangle(0, index, index + 1);
                }
            }
        }
    }
}
