using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.UI
{
    public enum ConstructionCharacterIcon { Squirrel, Pigeon, Hedgehog, Fox, Resident }
    public enum ConstructionStatusIcon { AnimalFood, CompletedCommute, RiskAlert }
    public enum ConstructionControlIcon
    {
        FillMode, EndlessMode, Build, Confirm, NextDay,
        Back, Restart, Settings, Language
    }

    /// <summary>
    /// UI-only construction pictograms. The board's 3D rooms and agents remain unchanged.
    /// The generated PNGs include generous transparent margins, so the sprite rect
    /// trims only that empty canvas before a small UI Image displays it.
    /// </summary>
    public static class ConstructionIconCatalog
    {
        private const string Root = "UI/ConstructionIcons/";
        private const float Trim = 0.13f;
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static string BuildResourcePath(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.Oak)
        {
            var name = category switch
            {
                ConstructionCategory.Green => planting switch
                {
                    GreenPlanting.Meadow => "meadow",
                    GreenPlanting.Shrub => "shrub",
                    _ => "oak"
                },
                ConstructionCategory.Residence => "residence",
                ConstructionCategory.Workshop => "workshop",
                ConstructionCategory.Restaurant => "restaurant",
                ConstructionCategory.Supermarket => "supermarket",
                ConstructionCategory.Waste => "waste",
                ConstructionCategory.Street => "street",
                ConstructionCategory.Square => "square",
                _ => null
            };
            return name == null ? string.Empty : Root + "build/" + name;
        }

        public static string CharacterResourcePath(ConstructionCharacterIcon icon) =>
            icon switch
            {
                ConstructionCharacterIcon.Squirrel => Root + "characters/squirrel",
                ConstructionCharacterIcon.Pigeon => Root + "characters/pigeon",
                ConstructionCharacterIcon.Hedgehog => Root + "characters/hedgehog",
                ConstructionCharacterIcon.Fox => Root + "characters/fox",
                ConstructionCharacterIcon.Resident => Root + "characters/resident",
                _ => string.Empty
            };

        public static string StatusResourcePath(ConstructionStatusIcon icon) =>
            icon switch
            {
                ConstructionStatusIcon.AnimalFood => Root + "status/animal_food",
                ConstructionStatusIcon.CompletedCommute => Root + "status/completed_commute",
                ConstructionStatusIcon.RiskAlert => Root + "status/risk_alert",
                _ => string.Empty
            };

        public static string ControlResourcePath(ConstructionControlIcon icon) =>
            icon switch
            {
                ConstructionControlIcon.FillMode => Root + "controls/fill_mode",
                ConstructionControlIcon.EndlessMode => Root + "controls/endless_mode",
                ConstructionControlIcon.Build => Root + "controls/build",
                ConstructionControlIcon.Confirm => Root + "controls/confirm",
                ConstructionControlIcon.NextDay => Root + "controls/next_day",
                ConstructionControlIcon.Back => Root + "controls/back",
                ConstructionControlIcon.Restart => Root + "controls/restart",
                ConstructionControlIcon.Settings => Root + "controls/settings",
                ConstructionControlIcon.Language => Root + "controls/language",
                _ => string.Empty
            };

        public static Sprite GetBuild(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.Oak) =>
            Load(BuildResourcePath(category, planting));

        public static Sprite GetCharacter(ConstructionCharacterIcon icon) =>
            Load(CharacterResourcePath(icon));

        public static Sprite GetStatus(ConstructionStatusIcon icon) =>
            Load(StatusResourcePath(icon));

        public static Sprite GetControl(ConstructionControlIcon icon) =>
            Load(ControlResourcePath(icon));

        private static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Cache.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var rect = new Rect(texture.width * Trim, texture.height * Trim,
                texture.width * (1f - Trim * 2f),
                texture.height * (1f - Trim * 2f));
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Construction " + path.Substring(Root.Length);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
