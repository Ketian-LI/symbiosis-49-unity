using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public static class UrbanPalette
    {
        public static readonly Color Background = Hex("F3F0EA");
        public static readonly Color Board = Hex("DCD5C8");
        public static readonly Color BoardSlot = Hex("CEC5B6");
        public static readonly Color BoardSlotAlternate = Hex("D6CEBF");
        public static readonly Color Boundary = Hex("243154");
        public static readonly Color Doorframe = Hex("F4EEE3");
        public static readonly Color Wall = Hex("17213F");
        public static readonly Color Text = Hex("10182E");
        public static readonly Color LightText = Hex("F8F4FF");
        public static readonly Color Selection = Hex("F24BC7");
        public static readonly Color Legal = Hex("26D7E8");
        public static readonly Color Risk = Hex("FF704D");
        public static readonly Color Recovery = Hex("7C62E8");
        public static readonly Color Safe = Hex("3CCB8C");

        public static Color ForRoom(RoomType type)
        {
            return type switch
            {
                RoomType.CentralPark => Hex("82B968"),
                RoomType.Residence => Hex("8E83AE"),
                RoomType.Office => Hex("4F8C92"),
                RoomType.Canteen => Hex("D59A3D"),
                RoomType.Supermarket => Hex("E4B85A"),
                RoomType.Garage => Hex("5C6874"),
                RoomType.Trash => Hex("A95441"),
                RoomType.PigeonHabitat => Hex("A2764E"),
                RoomType.OakHabitat => Hex("46764F"),
                RoomType.ShrubHabitat => Hex("78A65A"),
                RoomType.FoxDen => Hex("765066"),
                _ => Color.gray
            };
        }

        public static string TypeName(RoomType type)
        {
            return type switch
            {
                RoomType.CentralPark => "公共生态",
                RoomType.Residence => "住宅",
                RoomType.Office => "办公",
                RoomType.Canteen => "餐饮商铺",
                RoomType.Supermarket => "超市",
                RoomType.Garage => "车库",
                RoomType.Trash => "垃圾",
                RoomType.PigeonHabitat => "鸽群栖息",
                RoomType.OakHabitat => "橡树栖息",
                RoomType.ShrubHabitat => "灌木栖息",
                RoomType.FoxDen => "狐狸栖息",
                _ => "未知"
            };
        }

        public static string LocalizedRoomName(RoomSpec room, bool chinese)
        {
            if (room == null)
            {
                return string.Empty;
            }

            if (chinese)
            {
                return room.DisplayName;
            }

            var baseName = room.Type switch
            {
                RoomType.CentralPark => "Central Park",
                RoomType.Residence => "Residence",
                RoomType.Office => "Office",
                RoomType.Canteen => "Food Shop",
                RoomType.Supermarket => "Small Supermarket",
                RoomType.Garage => "Garage",
                RoomType.Trash => "Waste Room",
                RoomType.PigeonHabitat => "Pigeon Habitat",
                RoomType.OakHabitat => "Oak Habitat",
                RoomType.ShrubHabitat => "Shrub Habitat",
                RoomType.FoxDen => "Fox Den",
                _ => "Room"
            };

            var lastSpace = room.DisplayName.LastIndexOf(' ');
            if (lastSpace < 0 || lastSpace != room.DisplayName.Length - 2)
            {
                return baseName;
            }

            var suffix = room.DisplayName[room.DisplayName.Length - 1];
            return suffix is >= 'A' and <= 'Z' ? $"{baseName} {suffix}" : baseName;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out var color) ? color : Color.magenta;
        }
    }
}
