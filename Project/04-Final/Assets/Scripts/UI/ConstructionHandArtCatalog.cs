using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.UI
{
    public enum HandFooterIcon { Sprout, Person, Bin, Route }

    /// <summary>Printed artwork for the fill-mode hand and placement preview.</summary>
    public static class ConstructionHandArtCatalog
    {
        private const string Root = "UI/FillHandArt/";
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static readonly Dictionary<string, Rect> RoomSheetAreas = new()
        {
            // Pixel rectangles measured from the top-left of the 1774x887
            // approved concept sheet. The crop removes the preview borders.
            ["oak"] = new Rect(20f, 92f, 322f, 322f),
            ["meadow"] = new Rect(374f, 92f, 322f, 322f),
            ["shrub"] = new Rect(727f, 92f, 322f, 322f),
            ["residence"] = new Rect(1080f, 92f, 322f, 322f),
            ["workshop"] = new Rect(1433f, 92f, 322f, 322f),
            ["restaurant"] = new Rect(20f, 477f, 322f, 322f),
            ["supermarket"] = new Rect(374f, 477f, 322f, 322f),
            ["waste"] = new Rect(727f, 477f, 322f, 322f),
            ["street"] = new Rect(1080f, 477f, 322f, 322f),
            ["square"] = new Rect(1433f, 477f, 322f, 322f)
        };

        public static Sprite GetRoom(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            var name = category switch
            {
                ConstructionCategory.Green => planting switch
                {
                    GreenPlanting.Oak => "oak",
                    GreenPlanting.Shrub => "shrub",
                    _ => "meadow"
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
            return Load(name);
        }

        public static Sprite GetDeck() => Load("compact_deck_print_v2");
        public static Sprite GetTray() => Load("compact_tray_v2");
        public static Sprite GetCardPaper() => Load("compact_card_paper_v3");
        public static Sprite GetCardRestRail()
        {
            const string name = "card_rest_rail_v1";
            if (Cache.TryGetValue(name, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(Root + name);
            if (texture == null) return null;
            // The generated transparent sheet contains a shallow wooden lip
            // across its middle. Crop that strip so UI dimensions describe
            // the visible rail rather than the sheet's empty margins.
            var sprite = Sprite.Create(texture,
                new Rect(texture.width * 0.012f, texture.height * 0.463f,
                    texture.width * 0.978f, texture.height * 0.066f),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "Hand art card rest rail";
            Cache[name] = sprite;
            return sprite;
        }
        public static Sprite GetReroll() => Load("compact_swap_v2");
        public static Sprite GetFooter(HandFooterIcon icon) => Load(icon switch
        {
            HandFooterIcon.Sprout => "footer_sprout",
            HandFooterIcon.Person => "footer_person",
            HandFooterIcon.Bin => "footer_bin",
            HandFooterIcon.Route => "footer_route",
            _ => null
        });

        private static Sprite Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Cache.TryGetValue(name, out var cached) && cached != null)
                return cached;
            if (RoomSheetAreas.TryGetValue(name, out var topLeftArea))
            {
                var sheet = Resources.Load<Texture2D>(Root + "room_cards_v2");
                if (sheet != null)
                {
                    var scaleX = sheet.width / 1774f;
                    var scaleY = sheet.height / 887f;
                    var rect = new Rect(topLeftArea.x * scaleX,
                        sheet.height - (topLeftArea.y + topLeftArea.height) * scaleY,
                        topLeftArea.width * scaleX,
                        topLeftArea.height * scaleY);
                    var roomSprite = Sprite.Create(sheet, rect,
                        new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect);
                    roomSprite.name = "Hand art " + name;
                    Cache[name] = roomSprite;
                    return roomSprite;
                }
            }
            var texture = Resources.Load<Texture2D>(Root + name);
            if (texture == null) return null;
            var area = name switch
            {
                // Trim only the generated sheet's transparent padding. Keep
                // the original PNGs intact, including their alpha and light.
                "compact_tray_v2" => new Rect(0.00460405f, 0.16712707f, 0.99079190f, 0.66160221f),
                "compact_swap_v2" => new Rect(0.00782689f, 0.24309392f, 0.98434622f, 0.51795580f),
                "compact_card_paper_v3" => new Rect(0.04140414f, 0.03389831f, 0.91629163f, 0.92514124f),
                // The generated pictograms have transparent margins. Keep
                // their strokes legible in the 18-pixel card footer.
                "footer_sprout" => new Rect(0.193f, 0.142f, 0.614f, 0.614f),
                "footer_person" => new Rect(0.224f, 0.199f, 0.544f, 0.544f),
                "footer_bin" => new Rect(0.208f, 0.203f, 0.581f, 0.581f),
                "footer_route" => new Rect(0.108f, 0.079f, 0.783f, 0.783f),
                _ => new Rect(0f, 0f, 1f, 1f)
            };
            var pixels = new Rect(texture.width * area.x, texture.height * area.y,
                texture.width * area.width, texture.height * area.height);
            var border = name switch
            {
                "compact_tray_v2" => Vector4.one * pixels.height * 0.18f,
                "compact_swap_v2" => new Vector4(pixels.height * 0.8f,
                    pixels.height * 0.25f, pixels.height * 0.8f, pixels.height * 0.25f),
                "compact_card_paper_v3" => Vector4.one * pixels.height * 0.06f,
                _ => Vector4.zero
            };
            var sprite = Sprite.Create(texture, pixels,
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = "Hand art " + name;
            Cache[name] = sprite;
            return sprite;
        }
    }
}
