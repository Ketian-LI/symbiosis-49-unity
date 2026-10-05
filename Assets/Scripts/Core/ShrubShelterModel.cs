using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Moving a shrub changes its position immediately, but its cover does not
    // recover until two day boundaries have passed. This is a gameplay rule,
    // not a biological growth estimate.
    public sealed class ShrubShelterModel
    {
        public const int RecoveryDays = 2;

        private static readonly HashSet<string> ShrubIds = RoomLayoutData.All
            .Where(room => room.Type == RoomType.ShrubHabitat)
            .Select(room => room.Id)
            .ToHashSet(StringComparer.Ordinal);

        private static readonly HashSet<string> GarageIds = RoomLayoutData.All
            .Where(room => room.Type == RoomType.Garage)
            .Select(room => room.Id)
            .ToHashSet(StringComparer.Ordinal);

        private readonly Dictionary<string, int> readyDays = new(StringComparer.Ordinal);

        public void RecordMovement(IEnumerable<string> roomIds, int dayNumber)
        {
            foreach (var id in roomIds ?? Array.Empty<string>())
            {
                if (ShrubIds.Contains(id))
                {
                    readyDays[id] = Math.Max(1, dayNumber) + RecoveryDays;
                }
            }
        }

        public int DaysUntilReady(string roomId, int dayNumber, ISet<string> pendingMoves = null)
        {
            if (!ShrubIds.Contains(roomId))
            {
                return 0;
            }
            if (pendingMoves != null && pendingMoves.Contains(roomId))
            {
                return RecoveryDays;
            }
            return readyDays.TryGetValue(roomId, out var readyDay)
                ? Math.Max(0, readyDay - dayNumber)
                : 0;
        }

        public bool IsCovered(RoomNavigationMap navigation, string roomId, int dayNumber,
            ISet<string> pendingMoves = null)
        {
            return navigation != null && ShrubIds.Contains(roomId) &&
                   DaysUntilReady(roomId, dayNumber, pendingMoves) == 0 &&
                   navigation.NeighboursOf(roomId).Any(neighbour =>
                       ShrubIds.Contains(neighbour) &&
                       DaysUntilReady(neighbour, dayNumber, pendingMoves) == 0);
        }

        public int ConnectedPairCount(RoomNavigationMap navigation, int dayNumber,
            ISet<string> pendingMoves = null)
        {
            if (navigation == null)
            {
                return 0;
            }
            return ShrubIds.Sum(id =>
                DaysUntilReady(id, dayNumber, pendingMoves) == 0
                    ? navigation.NeighboursOf(id).Count(neighbour =>
                        StringComparer.Ordinal.Compare(id, neighbour) < 0 &&
                        ShrubIds.Contains(neighbour) &&
                        DaysUntilReady(neighbour, dayNumber, pendingMoves) == 0)
                    : 0);
        }

        public int RecoveringShrubCount(int dayNumber)
        {
            return ShrubIds.Count(id => DaysUntilReady(id, dayNumber) > 0);
        }

        // A hedgehog may choose a connected shrub passage when it avoids
        // garages and adds at most two doors to the direct path. No route is
        // guaranteed: the animal still makes its own food choice.
        public bool TryFindCoveredRoute(RoomNavigationMap navigation, string startRoomId,
            string destinationRoomId, int dayNumber, out IReadOnlyList<string> route)
        {
            route = Array.Empty<string>();
            if (navigation == null ||
                !navigation.TryFindRoute(startRoomId, destinationRoomId, out var direct))
            {
                return false;
            }

            List<string> best = null;
            foreach (var first in ShrubIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                if (!IsCovered(navigation, first, dayNumber) ||
                    !navigation.TryFindRoute(startRoomId, first, GarageIds, out var approach))
                {
                    continue;
                }
                foreach (var second in navigation.NeighboursOf(first)
                             .Where(id => IsCovered(navigation, id, dayNumber))
                             .OrderBy(id => id, StringComparer.Ordinal))
                {
                    if (!navigation.TryFindRoute(second, destinationRoomId, GarageIds, out var exit))
                    {
                        continue;
                    }
                    var candidate = approach.Concat(new[] { second }).Concat(exit.Skip(1)).ToList();
                    if (candidate.Count > direct.Count + 2 ||
                        candidate.Distinct(StringComparer.Ordinal).Count() != candidate.Count ||
                        best != null && candidate.Count >= best.Count)
                    {
                        continue;
                    }
                    best = candidate;
                }
            }
            route = best != null ? best : Array.Empty<string>();
            return best != null;
        }

        public List<ShrubShelterSaveData> Export()
        {
            return readyDays.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ShrubShelterSaveData
                {
                    roomId = pair.Key,
                    readyDay = pair.Value
                }).ToList();
        }

        public void Restore(IEnumerable<ShrubShelterSaveData> saved)
        {
            readyDays.Clear();
            foreach (var item in saved ?? Array.Empty<ShrubShelterSaveData>())
            {
                if (item != null && ShrubIds.Contains(item.roomId) && item.readyDay > 0)
                {
                    readyDays[item.roomId] = item.readyDay;
                }
            }
        }
    }
}
