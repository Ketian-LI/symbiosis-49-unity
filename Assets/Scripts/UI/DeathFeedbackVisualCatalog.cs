using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum DeathFeedbackVisual
    {
        PigeonFootprint,
        Ripple
    }

    public static class DeathFeedbackVisualCatalog
    {
        private const string ResourceRoot = "UI/DeathFeedback/";

        private static readonly IReadOnlyDictionary<DeathFeedbackVisual, string> ResourceNames =
            new Dictionary<DeathFeedbackVisual, string>
            {
                { DeathFeedbackVisual.PigeonFootprint, "death-pigeon-footprint-v01" },
                { DeathFeedbackVisual.Ripple, "death-ripple-v01" }
            };

        private static readonly Dictionary<DeathFeedbackVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(DeathFeedbackVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(DeathFeedbackVisual visual)
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
                    sprite.name = $"Death Feedback {visual}";
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
