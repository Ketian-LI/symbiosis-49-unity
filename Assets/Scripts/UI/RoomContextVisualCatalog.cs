using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum RoomContextVisual
    {
        Chip,
        FoodAvailable,
        HumanUse,
        ResidenceOccupied,
        ResidenceVacant,
        OfficeProduction0,
        OfficeProduction1,
        OfficeProduction2,
        WasteRouteBlocked,
        GarageConnectionHub,
        GarageConnectionNodeInactive,
        GarageConnectionNodeActive,
        OakFelled,
        OakSapling,
        OakYoung,
        OakMature,
        Connector
    }

    /// <summary>
    /// Provides the separate layers used by the selected-room contextual group.
    /// The room-type pictogram continues to come from RoomIconCatalog.
    /// </summary>
    public static class RoomContextVisualCatalog
    {
        private const string ResourceRoot = "UI/RoomContext/";

        private static readonly IReadOnlyDictionary<RoomContextVisual, string> ResourceNames =
            new Dictionary<RoomContextVisual, string>
            {
                { RoomContextVisual.Chip, "room-context-chip-v01" },
                { RoomContextVisual.FoodAvailable, "status-food-available-v01" },
                { RoomContextVisual.HumanUse, "status-human-use-v01" },
                { RoomContextVisual.ResidenceOccupied, "status-residence-occupied-v01" },
                { RoomContextVisual.ResidenceVacant, "status-residence-vacant-v01" },
                { RoomContextVisual.OfficeProduction0, "status-office-production-0-v01" },
                { RoomContextVisual.OfficeProduction1, "status-office-production-1-v01" },
                { RoomContextVisual.OfficeProduction2, "status-office-production-2-v01" },
                { RoomContextVisual.WasteRouteBlocked, "status-waste-route-blocked-v01" },
                { RoomContextVisual.GarageConnectionHub, "status-garage-connection-hub-v01" },
                { RoomContextVisual.GarageConnectionNodeInactive, "status-garage-connection-node-inactive-v01" },
                { RoomContextVisual.GarageConnectionNodeActive, "status-garage-connection-node-active-v01" },
                { RoomContextVisual.OakFelled, "status-oak-felled-v01" },
                { RoomContextVisual.OakSapling, "status-oak-sapling-v01" },
                { RoomContextVisual.OakYoung, "status-oak-young-v01" },
                { RoomContextVisual.OakMature, "status-oak-mature-v01" },
                { RoomContextVisual.Connector, "room-context-connector-v01" }
            };

        private static readonly Dictionary<RoomContextVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(RoomContextVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(RoomContextVisual visual)
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
                    sprite.name = $"Room Context {visual}";
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
