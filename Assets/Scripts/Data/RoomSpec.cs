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
        FoxDen,
        SharedSpace,
        EcologicalBuffer,
        CommunitySquare
    }

    public enum ParkGreenRole
    {
        None,
        Connector,
        SquirrelGrove,
        HedgehogGarden,
        FoxEdge
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
            bool startsRecovering = false,
            ParkGreenRole greenRole = ParkGreenRole.None)
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
            GreenRole = greenRole;
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
        public ParkGreenRole GreenRole { get; }
        public bool IsGreen => Type is RoomType.CentralPark or RoomType.OakHabitat or
            RoomType.ShrubHabitat || GreenRole != ParkGreenRole.None;
        public int CellCount => Width * Height;
        public string SizeLabel => $"{Width}×{Height}";
    }
}
