using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum AnimalSelectionVisual
    {
        SelectionRing,
        PigeonFootprint
    }

    public static class AnimalSelectionVisualCatalog
    {
        private const string ResourceRoot = "UI/AnimalSelection/";

        private static readonly IReadOnlyDictionary<AnimalSelectionVisual, string> ResourceNames =
            new Dictionary<AnimalSelectionVisual, string>
            {
                { AnimalSelectionVisual.SelectionRing, "animal-selection-ring-v01" },
                { AnimalSelectionVisual.PigeonFootprint, "pigeon-footprint-v01" }
            };

        private static readonly Dictionary<AnimalSelectionVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(AnimalSelectionVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(AnimalSelectionVisual visual)
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
                    sprite.name = $"Animal Selection {visual}";
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
