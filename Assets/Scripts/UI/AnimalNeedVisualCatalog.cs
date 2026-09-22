using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum AnimalNeedVisual
    {
        HungerWarning,
        HabitatWarning,
        SafetyDanger
    }

    public static class AnimalNeedVisualCatalog
    {
        private const string ResourceRoot = "UI/AnimalNeeds/";

        private static readonly IReadOnlyDictionary<AnimalNeedVisual, string> ResourceNames =
            new Dictionary<AnimalNeedVisual, string>
            {
                { AnimalNeedVisual.HungerWarning, "animal-need-hunger-warning-v01" },
                { AnimalNeedVisual.HabitatWarning, "animal-need-habitat-warning-v01" },
                { AnimalNeedVisual.SafetyDanger, "animal-need-safety-danger-v01" }
            };

        private static readonly Dictionary<AnimalNeedVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(AnimalNeedVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(AnimalNeedVisual visual)
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
                    sprite.name = $"Animal Need {visual}";
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
