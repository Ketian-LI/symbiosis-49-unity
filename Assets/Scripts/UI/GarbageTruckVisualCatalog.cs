using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum GarbageTruckVisual
    {
        Truck,
        CountdownRing,
        BinPip,
        CollectionComplete,
        EmergencyCost
    }

    public static class GarbageTruckVisualCatalog
    {
        private const string ResourceRoot = "UI/GarbageTruck/";

        private static readonly IReadOnlyDictionary<GarbageTruckVisual, string> ResourceNames =
            new Dictionary<GarbageTruckVisual, string>
            {
                { GarbageTruckVisual.Truck, "garbage-truck-base-v01" },
                { GarbageTruckVisual.CountdownRing, "garbage-truck-countdown-ring-v01" },
                { GarbageTruckVisual.BinPip, "garbage-truck-bin-pip-v01" },
                { GarbageTruckVisual.CollectionComplete, "garbage-truck-complete-v01" },
                { GarbageTruckVisual.EmergencyCost, "garbage-truck-emergency-cost-v01" }
            };

        private static readonly Dictionary<GarbageTruckVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(GarbageTruckVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(GarbageTruckVisual visual)
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
                    sprite.name = $"Garbage Truck {visual}";
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
