namespace UrbanWildlifeRooms.Data
{
    public enum AnimalRouteEventKind
    {
        ParkVisitors,
        ShelterMaintenance,
        ShopCrowd
    }

    public readonly struct AnimalRouteEvent
    {
        public AnimalRouteEvent(int dayNumber, AnimalRouteEventKind kind,
            string roomId, AnimalPassageEdge edge, int segment,
            bool blocksAllPorts = false)
        {
            DayNumber = dayNumber;
            Kind = kind;
            RoomId = roomId;
            Edge = edge;
            Segment = segment;
            BlocksAllPorts = blocksAllPorts;
        }

        public int DayNumber { get; }
        public AnimalRouteEventKind Kind { get; }
        public string RoomId { get; }
        public AnimalPassageEdge Edge { get; }
        public int Segment { get; }
        public bool BlocksAllPorts { get; }

        public bool Blocks(string roomId, AnimalPassagePort port) =>
            roomId == RoomId && (BlocksAllPorts || port.Edge == Edge && port.Segment == Segment);
    }

    /// <summary>
    /// One predictable animal-only disturbance per day. Most affect one
    /// world-facing port; a garden-maintenance event closes that room's animal
    /// exits for the day. Human commuting is unaffected, but an extremely
    /// hungry animal still cannot borrow an event-closed doorway.
    /// The player must still confirm a real daily spatial change before skipping.
    /// </summary>
    public static class AnimalRouteEventSchedule
    {
        public const int FirstDay = 2;

        public static AnimalRouteEvent? ForDay(int dayNumber)
        {
            if (dayNumber < FirstDay) return null;

            var cycle = (dayNumber - FirstDay) / 3;
            return ((dayNumber - FirstDay) % 3) switch
            {
                0 => cycle % 4 == 1
                    ? new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ShelterMaintenance,
                        "shared-h", AnimalPassageEdge.East, 0, true)
                    : new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ParkVisitors,
                        "central-park", cycle % 4 == 3
                            ? AnimalPassageEdge.East : AnimalPassageEdge.West, 0),
                1 => new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ShelterMaintenance,
                    "shrub-a", (cycle % 3) switch
                    {
                        0 => AnimalPassageEdge.West,
                        1 => AnimalPassageEdge.North,
                        _ => AnimalPassageEdge.South
                    }, 0),
                _ => (cycle % 3) switch
                {
                    0 => new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ShopCrowd,
                        "canteen-a", AnimalPassageEdge.North, 0),
                    1 => new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ShopCrowd,
                        "shared-k", AnimalPassageEdge.East, 0),
                    _ => new AnimalRouteEvent(dayNumber, AnimalRouteEventKind.ShopCrowd,
                        "supermarket", AnimalPassageEdge.South, 0)
                }
            };
        }

        public static string ShortLabel(AnimalRouteEvent routeEvent, bool chinese)
        {
            var place = routeEvent.RoomId switch
            {
                "central-park" => chinese ? "公园" : "Park",
                "shared-h" => chinese ? "刺猬花园" : "Hedgehog garden",
                "shrub-a" => chinese ? "灌木 A" : "Shrub A",
                "canteen-a" => chinese ? "餐饮 A" : "Food shop A",
                "canteen-b" => chinese ? "餐饮 B" : "Food shop B",
                "shared-k" => chinese ? "社区活动场" : "Community square",
                "supermarket" => chinese ? "超市" : "Market",
                _ => routeEvent.RoomId
            };
            var cause = routeEvent.Kind switch
            {
                AnimalRouteEventKind.ParkVisitors => chinese ? "游客" : "visitors",
                AnimalRouteEventKind.ShelterMaintenance => chinese ? "养护" : "maintenance",
                _ => chinese ? "人流" : "crowd"
            };
            if (routeEvent.BlocksAllPorts)
                return chinese ? $"{place}全部动物出口 · {cause}" : $"{place} all animal exits · {cause}";
            var side = routeEvent.Edge switch
            {
                AnimalPassageEdge.North => chinese ? "北侧" : "north",
                AnimalPassageEdge.East => chinese ? "东侧" : "east",
                AnimalPassageEdge.South => chinese ? "南侧" : "south",
                _ => chinese ? "西侧" : "west"
            };
            return chinese ? $"{place}{side} · {cause}" : $"{place} {side} · {cause}";
        }
    }
}
