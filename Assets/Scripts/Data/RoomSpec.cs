using System;

namespace UrbanWildlifeRooms.Data
{
    public enum RoomType
    {
        CentralPark,
        Residence,
        Office,
        Canteen,
        Supermarket,
        Garage,
        Trash,
        PigeonHabitat,
        OakHabitat,
        ShrubHabitat,
        FoxDen
    }

    [Serializable]
    public sealed class RoomSpec
    {
        public RoomSpec(
            string id,
            string displayName,
            RoomType type,
            int column,
            int row,
            int width,
            int height,
            bool movable,
            string description,
            bool startsRecovering = false)
        {
            Id = id;
            DisplayName = displayName;
            Type = type;
            Column = column;
            Row = row;
            Width = width;
            Height = height;
            Movable = movable;
            Description = description;
            StartsRecovering = startsRecovering;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public RoomType Type { get; }
        public int Column { get; }
        public int Row { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Movable { get; }
        public string Description { get; }
        public bool StartsRecovering { get; }
        public int CellCount => Width * Height;
        public string SizeLabel => $"{Width}×{Height}";
    }
}
