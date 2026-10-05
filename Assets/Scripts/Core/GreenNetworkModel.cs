using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    /// <summary>
    /// The park is a one-cell anchor; movable planted rooms make its usable
    /// footprint. Touching corners and human-only doorways do not join it.
    /// </summary>
    public static class GreenNetworkModel
    {
        public const int StableCellCount = 4;
        private static readonly Dictionary<string, RoomSpec> GreenRooms = RoomLayoutData.All
            .Where(room => room.IsGreen)
            .ToDictionary(room => room.Id, StringComparer.Ordinal);

        public static IReadOnlyCollection<string> ConnectedRooms(RoomNavigationMap animalNavigation)
        {
            if (animalNavigation == null || !GreenRooms.ContainsKey("central-park"))
                return Array.Empty<string>();

            var connected = new HashSet<string>(StringComparer.Ordinal) { "central-park" };
            var queue = new Queue<string>();
            queue.Enqueue("central-park");
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in animalNavigation.NeighboursOf(current))
                {
                    if (GreenRooms.ContainsKey(next) && connected.Add(next))
                        queue.Enqueue(next);
                }
            }
            return connected;
        }

        public static int ConnectedCount(RoomNavigationMap animalNavigation) =>
            ConnectedRooms(animalNavigation).Count;

        public static bool IsStable(RoomNavigationMap animalNavigation) =>
            ConnectedCount(animalNavigation) >= StableCellCount;
    }
}
