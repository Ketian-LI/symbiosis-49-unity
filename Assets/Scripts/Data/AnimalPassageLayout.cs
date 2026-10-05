using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Data
{
    public enum AnimalPassageEdge { North, East, South, West }

    public readonly struct AnimalPassagePort
    {
        public AnimalPassagePort(AnimalPassageEdge edge, int segment)
        {
            Edge = edge;
            Segment = segment;
        }

        public AnimalPassageEdge Edge { get; }
        public int Segment { get; }
    }

    /// <summary>
    /// Wildlife traversal ports on each room. An animal may use only a doorway
    /// without an authored pedestrian entrance; no doorway is shared.
    /// The animal planning overlay reveals the usable links when needed.
    /// Profiles are authored in the unrotated room frame; the room visual and these
    /// ports rotate together. A port connects only to a facing port on the same cell edge.
    /// </summary>
    public static class AnimalPassageLayout
    {
        private const int N = 1, E = 2, S = 4, W = 8;

        public static bool CanRotate(RoomSpec spec) => spec != null && spec.Movable &&
            (spec.Width != spec.Height ||
             MaskFor(spec) != 0 && MaskFor(spec) != (N | E | S | W));

        public static IReadOnlyList<AnimalPassagePort> Ports(RoomSpec spec, int quarterTurns)
        {
            if (spec == null) return Array.Empty<AnimalPassagePort>();
            var ports = new List<AnimalPassagePort>();
            var mask = MaskFor(spec);
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

        private static int MaskFor(RoomSpec spec)
        {
            return (N | E | S | W) & ~HumanRoadLayout.MaskFor(spec);
        }
    }
}
