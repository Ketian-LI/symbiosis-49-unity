using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum ResourcePointVisual
    {
        Base,
        Gain,
        Spend,
        Insufficient,
        Full
    }

    public static class ResourcePointVisualCatalog
    {
        private const string ResourceRoot = "UI/ResourcePoints/";
        private static readonly IReadOnlyDictionary<ResourcePointVisual, string> ResourceNames =
            new Dictionary<ResourcePointVisual, string>
            {
                { ResourcePointVisual.Base, "resource-point-base-v01" },
                { ResourcePointVisual.Gain, "resource-point-gain-v01" },
                { ResourcePointVisual.Spend, "resource-point-spend-v01" },
                { ResourcePointVisual.Insufficient, "resource-point-insufficient-v01" },
                { ResourcePointVisual.Full, "resource-point-full-v01" }
            };

        private static readonly Dictionary<ResourcePointVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(ResourcePointVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(ResourcePointVisual visual)
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
                    sprite.name = $"Resource Point {visual}";
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
