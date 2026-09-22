using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.UI
{
    /// <summary>
    /// Provides the approved frameless room pictograms from Resources.
    /// State such as fixed, recovering, selected or dangerous stays in separate UI layers.
    /// </summary>
    public static class RoomIconCatalog
    {
        private const string ResourceRoot = "UI/RoomIcons/";

        private static readonly IReadOnlyDictionary<RoomType, string> ResourceNames =
            new Dictionary<RoomType, string>
            {
                { RoomType.CentralPark, "room-icon-central-park-v02" },
                { RoomType.Residence, "room-icon-residence-v01" },
                { RoomType.Office, "room-icon-office-v01" },
                { RoomType.Canteen, "room-icon-food-shop-v01" },
                { RoomType.Supermarket, "room-icon-supermarket-v01" },
                { RoomType.Garage, "room-icon-garage-v01" },
                { RoomType.Trash, "room-icon-waste-v01" },
                { RoomType.PigeonHabitat, "room-icon-pigeon-habitat-v01" },
                { RoomType.OakHabitat, "room-icon-oak-habitat-v01" },
                { RoomType.ShrubHabitat, "room-icon-shrub-habitat-v01" },
                { RoomType.FoxDen, "room-icon-fox-den-v01" }
            };

        private static readonly Dictionary<RoomType, Sprite> SpriteCache = new();

        public static string ResourcePath(RoomType type)
        {
            return ResourceNames.TryGetValue(type, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(RoomType type)
        {
            if (SpriteCache.TryGetValue(type, out var cached) && cached != null)
            {
                return cached;
            }

            var path = ResourcePath(type);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

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
                    sprite.name = $"{type} Room Icon";
                }
            }

            if (sprite != null)
            {
                SpriteCache[type] = sprite;
            }

            return sprite;
        }
    }
}
