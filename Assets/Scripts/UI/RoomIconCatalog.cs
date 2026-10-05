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
                { RoomType.FoxDen, "room-icon-fox-den-v01" },
                { RoomType.SharedSpace, "room-icon-shared-space-v01" }
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

            if (type is RoomType.EcologicalBuffer or RoomType.CommunitySquare)
            {
                var generated = CreatePublicSpacePictogram(type);
                SpriteCache[type] = generated;
                return generated;
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

        // Keep the two new symbols crisp and distinct without introducing
        // placeholder copies of existing room art. Both use the HUD's ivory ink.
        private static Sprite CreatePublicSpacePictogram(RoomType type)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{type} Pictogram",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var ink = new Color(0.96f, 0.93f, 0.80f, 1f);
            var accent = new Color(0.48f, 0.83f, 0.71f, 1f);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    var x = (column + 0.5f) * 2f / size - 1f;
                    var y = (row + 0.5f) * 2f / size - 1f;
                    Color pixel;
                    if (type == RoomType.EcologicalBuffer)
                    {
                        var u = (x + y) * 0.7071f;
                        var v = (y - x) * 0.7071f;
                        var leafWidth = 0.37f * Mathf.Max(0f, 1f - u * u / 0.57f);
                        var leaf = u > -0.72f && u < 0.72f && Mathf.Abs(v) < leafWidth;
                        var vein = Mathf.Abs(v) < 0.035f && u > -0.62f && u < 0.58f;
                        var stem = Mathf.Abs(x + y + 1.12f) < 0.055f &&
                                   x > -0.73f && x < -0.40f;
                        pixel = leaf ? (vein ? accent : ink) : stem ? accent : clear;
                    }
                    else
                    {
                        var left = Mathf.Abs(x + 0.31f) < 0.22f;
                        var right = Mathf.Abs(x - 0.31f) < 0.22f;
                        var top = Mathf.Abs(y - 0.31f) < 0.22f;
                        var bottom = Mathf.Abs(y + 0.31f) < 0.22f;
                        var paving = (left || right) && (top || bottom);
                        var gatheringPoint = x * x + y * y < 0.13f * 0.13f;
                        pixel = gatheringPoint ? accent : paving ? ink : clear;
                    }
                    texture.SetPixel(column, row, pixel);
                }
            }
            texture.Apply(false, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = $"{type} Room Icon";
            return sprite;
        }
    }
}
