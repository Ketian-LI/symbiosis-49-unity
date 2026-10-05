using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Data
{
    /// <summary>
    /// Pedestrian entrances for every room. AnimalPassageLayout uses the
    /// complementary entrances, so one doorway never serves both networks.
    /// Painted trails are shown only in outdoor rooms; indoor rooms still use
    /// these authored pedestrian entrances for commute and waste routing.
    /// </summary>
    public static class HumanRoadLayout
    {
        private const int N = 1, E = 2, S = 4, W = 8;

        public static bool HasVisibleRoad(RoomSpec spec) => spec != null &&
            spec.Type is RoomType.CentralPark or RoomType.SharedSpace or
                RoomType.CommunitySquare or RoomType.OakHabitat or RoomType.ShrubHabitat;

        public static IReadOnlyList<AnimalPassagePort> Ports(RoomSpec spec, int quarterTurns)
        {
            if (spec == null) return Array.Empty<AnimalPassagePort>();
            var mask = MaskFor(spec);
            var ports = new List<AnimalPassagePort>();
            for (var x = 0; x < spec.Width; x++)
            {
                if ((mask & N) != 0) ports.Add(new AnimalPassagePort(AnimalPassageEdge.North, x));
                if ((mask & S) != 0) ports.Add(new AnimalPassagePort(AnimalPassageEdge.South, x));
            }
            for (var y = 0; y < spec.Height; y++)
            {
                if ((mask & E) != 0) ports.Add(new AnimalPassagePort(AnimalPassageEdge.East, y));
                if ((mask & W) != 0) ports.Add(new AnimalPassagePort(AnimalPassageEdge.West, y));
            }

            var turns = ((quarterTurns % 4) + 4) % 4;
            for (var turn = 0; turn < turns; turn++)
            {
                var height = turn % 2 == 0 ? spec.Height : spec.Width;
                for (var i = 0; i < ports.Count; i++)
                {
                    var port = ports[i];
                    ports[i] = port.Edge switch
                    {
                        AnimalPassageEdge.North => new AnimalPassagePort(AnimalPassageEdge.East, port.Segment),
                        AnimalPassageEdge.East => new AnimalPassagePort(AnimalPassageEdge.South, height - 1 - port.Segment),
                        AnimalPassageEdge.South => new AnimalPassagePort(AnimalPassageEdge.West, port.Segment),
                        _ => new AnimalPassagePort(AnimalPassageEdge.North, height - 1 - port.Segment)
                    };
                }
            }
            return ports;
        }

        internal static int MaskFor(RoomSpec spec)
        {
            // Keep the two networks disjoint even for a future multi-cell room.
            // Segment matching is handled by RoomNavigationMap.
            if (spec.Width * spec.Height > 1) return N | S;
            // The initial 7x7 arrangement has a connected commute network and
            // a separate green/wildlife network. These profiles move and rotate
            // with their rooms; mismatched neighbours close that doorway.
            return spec.Id switch
            {
                "garage-a" => E,
                "shared-a" => E | S | W,
                "residence-a" => E | S | W,
                "shared-b" => S | W,
                "oak-a" => 0,
                "shared-c" => S,
                "trash-a" => S,
                "pigeon-a" => 0,
                "supermarket" => N | E,
                "shared-d" => N | E | S | W,
                "office-a" => N | E | S | W,
                "trash-b" => E | S | W,
                "residence-c" => N | E | S | W,
                "garage-b" => N | S | W,
                "shared-e" => 0,
                "pigeon-b" => 0,
                "central-park" => N,
                "shared-f" => N | S,
                "pigeon-c" => N | E | S,
                "office-b" => N | E | S | W,
                "shared-g" => N | W,
                "residence-g" => E | S,
                "oak-c" => E | S | W,
                "shared-h" => W,
                "shared-i" => N | S,
                "residence-d" => N | S,
                "trash-c" => N | E,
                "oak-b" => S | W,
                "residence-h" => N | E | S,
                "residence-b" => N | E | S | W,
                "shared-j" => W,
                "canteen-b" => N | E | S,
                "shared-k" => N | S | W,
                "shrub-a" => 0,
                "shared-l" => N,
                "residence-e" => N | E | S,
                "residence-f" => N | E | S | W,
                "canteen-a" => E | S | W,
                "office-c" => N | E | S | W,
                "trash-d" => N | W,
                "pigeon-d" => 0,
                "shrub-b" => 0,
                "garage-c" => N | E,
                "shared-m" => N | W,
                "shared-n" => N | E,
                "office-d" => N | W,
                "oak-d" => 0,
                "shrub-c" => 0,
                "fox-den" => 0,
                _ => N | S
            };
        }
    }
}
