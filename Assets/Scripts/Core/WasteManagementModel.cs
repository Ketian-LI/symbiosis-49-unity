using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class WasteManagementModel
    {
        public const int EmergencyCollectionCost = 2;
        public const int MaximumWastePenalty = 20;
        public const int PenaltyPerAffectedRoom = 5;

        private readonly IReadOnlyList<RoomSpec> roomSpecs;
        private readonly Dictionary<string, WasteRoomLoadModel> wasteRooms;
        private readonly Dictionary<string, int> blockedWaste = new();
        private RoomNavigationMap navigation;

        public WasteManagementModel(
            RoomNavigationMap navigationMap,
            IEnumerable<RoomSpec> specs)
        {
            navigation = navigationMap ?? throw new ArgumentNullException(nameof(navigationMap));
            roomSpecs = (specs ?? throw new ArgumentNullException(nameof(specs))).ToArray();
            wasteRooms = roomSpecs
                .Where(room => room.Type == RoomType.Trash)
                .ToDictionary(room => room.Id, _ => new WasteRoomLoadModel());
        }

        public event Action Changed;
        public event Action<string, int> WasteRouted;
        public event Action<string, int> WasteRouteBlocked;

        public IReadOnlyDictionary<string, WasteRoomLoadModel> WasteRooms => wasteRooms;
        public IReadOnlyDictionary<string, int> BlockedWaste => blockedWaste;
        public int TotalWasteUnits => wasteRooms.Values.Sum(room => room.Units) + blockedWaste.Values.Sum();
        public int HumanFunctionPenalty
        {
            get
            {
                var affectedRooms = wasteRooms.Values.Count(room => room.IsOverflowing) +
                                    blockedWaste.Count(pair => pair.Value > 0);
                return Math.Min(MaximumWastePenalty, affectedRooms * PenaltyPerAffectedRoom);
            }
        }

        public bool RouteWaste(string producerRoomId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Waste amount must be positive.");
            }

            if (!navigation.TryFindNearestReachable(
                    producerRoomId,
                    wasteRooms.Keys,
                    out var destination,
                    out _))
            {
                blockedWaste[producerRoomId] =
                    blockedWaste.TryGetValue(producerRoomId, out var current)
                        ? current + amount
                        : amount;
                WasteRouteBlocked?.Invoke(producerRoomId, amount);
                Changed?.Invoke();
                return false;
            }

            wasteRooms[destination].Add(amount);
            WasteRouted?.Invoke(destination, amount);
            Changed?.Invoke();
            return true;
        }

        public void ProduceDailyWaste(
            int residentCount,
            int operatingFoodShopCount,
            bool supermarketOperating)
        {
            var residences = roomSpecs
                .Where(room => room.Type == RoomType.Residence)
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .Take(Math.Max(0, residentCount));
            foreach (var residence in residences)
            {
                RouteWaste(residence.Id, 1);
            }

            var foodShops = roomSpecs
                .Where(room => room.Type == RoomType.Canteen)
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .Take(Math.Max(0, operatingFoodShopCount));
            foreach (var foodShop in foodShops)
            {
                RouteWaste(foodShop.Id, 2);
            }

            if (supermarketOperating)
            {
                var supermarket = roomSpecs.FirstOrDefault(room => room.Type == RoomType.Supermarket);
                if (supermarket != null)
                {
                    RouteWaste(supermarket.Id, 2);
                }
            }
        }

        public void UpdateNavigation(RoomNavigationMap navigationMap)
        {
            navigation = navigationMap ?? throw new ArgumentNullException(nameof(navigationMap));
            RerouteBlockedWaste();
        }

        public void MunicipalCollectAll()
        {
            foreach (var room in wasteRooms.Values)
            {
                room.CollectAll();
            }

            Changed?.Invoke();
        }

        public bool TryEmergencyCollect(
            string wasteRoomId,
            int availableResourcePoints,
            out int resourceCost)
        {
            resourceCost = 0;
            if (availableResourcePoints < EmergencyCollectionCost ||
                !wasteRooms.TryGetValue(wasteRoomId, out var room) ||
                !room.IsOverflowing)
            {
                return false;
            }

            room.CollectAll();
            resourceCost = EmergencyCollectionCost;
            Changed?.Invoke();
            return true;
        }

        public void Reset()
        {
            foreach (var room in wasteRooms.Values)
            {
                room.CollectAll();
            }

            blockedWaste.Clear();
            Changed?.Invoke();
        }

        public void Restore(
            IEnumerable<WasteRoomSaveData> savedWasteRooms,
            IEnumerable<BlockedWasteSaveData> savedBlockedWaste)
        {
            foreach (var pair in wasteRooms)
            {
                pair.Value.Restore(0);
            }

            foreach (var item in savedWasteRooms ?? Array.Empty<WasteRoomSaveData>())
            {
                if (item != null && wasteRooms.TryGetValue(item.id, out var room))
                {
                    room.Restore(Math.Max(0, item.units));
                }
            }

            blockedWaste.Clear();
            foreach (var item in savedBlockedWaste ?? Array.Empty<BlockedWasteSaveData>())
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.producerRoomId) && item.units > 0)
                {
                    blockedWaste[item.producerRoomId] = item.units;
                }
            }

            Changed?.Invoke();
        }

        public List<WasteRoomSaveData> ExportWasteRooms()
        {
            return wasteRooms
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new WasteRoomSaveData
                {
                    id = pair.Key,
                    units = pair.Value.Units
                })
                .ToList();
        }

        public List<BlockedWasteSaveData> ExportBlockedWaste()
        {
            return blockedWaste
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new BlockedWasteSaveData
                {
                    producerRoomId = pair.Key,
                    units = pair.Value
                })
                .ToList();
        }

        private void RerouteBlockedWaste()
        {
            var pending = blockedWaste.ToArray();
            foreach (var pair in pending)
            {
                if (!navigation.TryFindNearestReachable(
                        pair.Key,
                        wasteRooms.Keys,
                        out var destination,
                        out _))
                {
                    continue;
                }

                blockedWaste.Remove(pair.Key);
                wasteRooms[destination].Add(pair.Value);
                WasteRouted?.Invoke(destination, pair.Value);
            }

            Changed?.Invoke();
        }
    }
}
