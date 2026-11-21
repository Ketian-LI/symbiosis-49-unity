using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.Presentation
{
    // A feathered paper field keeps progress legible over the printed foliage
    // without introducing a hard rectangular card around the whole section.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConstructionGoalReadingWash : MaskableGraphic
    {
        private Texture2D paperTexture;

        public override Texture mainTexture
        {
            get
            {
                if (paperTexture == null)
                    paperTexture = Resources.Load<Texture2D>(
                        "UI/FillHandArt/compact_card_paper_v3");
                return paperTexture != null ? paperTexture : Texture2D.whiteTexture;
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var feather = Mathf.Min(16f, Mathf.Min(rect.width, rect.height) * 0.25f);
            var xs = new[] { rect.xMin, rect.xMin + feather, rect.xMax - feather, rect.xMax };
            var ys = new[] { rect.yMin, rect.yMin + feather, rect.yMax - feather, rect.yMax };
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var tint = color;
                if (x == 0 || x == 3 || y == 0 || y == 3) tint.a = 0f;
                // Sample only the existing card paper's unprinted centre.
                // Its frame and shadow must not become a second UI border.
                var uv = new Vector2(0.2f + 0.6f * (xs[x] - rect.xMin) / rect.width,
                    0.2f + 0.6f * (ys[y] - rect.yMin) / rect.height);
                mesh.AddVert(new Vector3(xs[x], ys[y], 0f), tint, uv);
            }
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
            {
                var index = y * 4 + x;
                mesh.AddTriangle(index, index + 4, index + 5);
                mesh.AddTriangle(index, index + 5, index + 1);
            }
        }
    }
}
