using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum CameraRecognitionVisual
    {
        CyanPerimeter,
        GreenPerimeter,
        AmberInvalidPlacement
    }

    public static class CameraRecognitionVisualCatalog
    {
        private const string ResourceRoot = "UI/CameraRecognition/";

        private static readonly IReadOnlyDictionary<CameraRecognitionVisual, string> ResourceNames =
            new Dictionary<CameraRecognitionVisual, string>
            {
                { CameraRecognitionVisual.CyanPerimeter, "camera-recognition-perimeter-cyan-v01" },
                { CameraRecognitionVisual.GreenPerimeter, "camera-recognition-perimeter-green-v01" },
                { CameraRecognitionVisual.AmberInvalidPlacement, "camera-recognition-invalid-placement-amber-v01" }
            };

        private static readonly Dictionary<CameraRecognitionVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(CameraRecognitionVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(CameraRecognitionVisual visual)
        {
            if (SpriteCache.TryGetValue(visual, out var cached) && cached != null)
            {
                return cached;
            }

            var path = ResourcePath(visual);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = $"Camera Recognition {visual}";
                }
            }

            if (sprite != null)
            {
                SpriteCache[visual] = sprite;
            }

            return sprite;
        }
    }
}
