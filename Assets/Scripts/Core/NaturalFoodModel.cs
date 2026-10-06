using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum NaturalFoodKind
    {
        Seed,
        Nut,
        Insect,
        DiscardedFood
    }

    [Serializable]
    public sealed class NaturalFoodState
    {
        public string roomId;
        public NaturalFoodKind kind;
        public int portions;
        public int addedToday;
        public bool dailyAdditionKnown;
    }

    public sealed class NaturalFoodModel
    {
        private readonly IReadOnlyList<RoomSpec> rooms;
        private readonly Dictionary<string, NaturalFoodState> sources = new();
        private readonly int runSeed;
        private int productionDayNumber = -1;
        private bool recordingCurrentDay = true;

        public NaturalFoodModel(IEnumerable<RoomSpec> roomSpecs, int seed = 49)
        {
            rooms = (roomSpecs ?? throw new ArgumentNullException(nameof(roomSpecs))).ToArray();
            runSeed = seed;
        }

        public IReadOnlyDictionary<string, NaturalFoodState> Sources => sources;
        public int TotalPortions => sources.Values.Sum(source => source.portions);

        public void ProduceDawn(
            int dayNumber,
            Func<string, OakTreeStage> oakStage,
            RoomNavigationMap navigation = null)
        {
            BeginProductionDay(dayNumber);
            var connectedGreen = GreenNetworkModel.ConnectedRooms(navigation);
            foreach (var room in rooms)
            {
                if (room.Type == RoomType.PigeonHabitat)
                {
                    Ensure(room.Id, NaturalFoodKind.Seed,
                        HabitatFoodNetworkModel.SeedCapacity(navigation, room.Id, dayNumber));
                }
                else if (room.Type == RoomType.CentralPark)
                {
                    Ensure(room.Id, NaturalFoodKind.Seed, 1);
                }
                else if (room.GreenRole == ParkGreenRole.SquirrelGrove &&
                         connectedGreen.Count >= GreenNetworkModel.StableCellCount &&
                         connectedGreen.Contains(room.Id))
                {
                    Ensure(room.Id, NaturalFoodKind.Nut, 1);
                }
                else if (room.Type == RoomType.OakHabitat &&
                         oakStage != null && oakStage(room.Id) == OakTreeStage.Mature)
                {
                    Ensure(room.Id, NaturalFoodKind.Nut, 1);
                }
            }
        }

        public void ProduceDusk(int dayNumber, int operatingFoodShopCount)
        {
            BeginProductionDay(dayNumber);
            foreach (var room in rooms
                         .Where(room => room.Type == RoomType.Canteen)
                         .OrderBy(room => room.Id, StringComparer.Ordinal)
                         .Take(Math.Max(0, operatingFoodShopCount)))
            {
                Ensure(room.Id, NaturalFoodKind.DiscardedFood, 1);
            }
        }

        public void ProduceNight(
            int dayNumber,
            IReadOnlyDictionary<string, WasteRoomLoadModel> wasteRooms,
            RoomNavigationMap animalNavigation = null)
        {
            BeginProductionDay(dayNumber);
            var connectedGreen = GreenNetworkModel.ConnectedRooms(animalNavigation);
            foreach (var room in rooms)
            {
                if (room.Type == RoomType.CentralPark)
                {
                    Ensure(room.Id, NaturalFoodKind.Insect, 1);
                    continue;
                }

                if (room.GreenRole == ParkGreenRole.HedgehogGarden &&
                    connectedGreen.Count >= GreenNetworkModel.StableCellCount &&
                    connectedGreen.Contains(room.Id))
                {
                    Ensure(room.Id, NaturalFoodKind.Insect, 1);
                }

                if ((room.Type == RoomType.ShrubHabitat || room.Type == RoomType.Trash) &&
                    Roll(dayNumber, room.Id, NaturalFoodKind.Insect) < 0.5f)
                {
                    Ensure(room.Id, NaturalFoodKind.Insect, 1);
                }

                if (room.Type != RoomType.Trash || wasteRooms == null ||
                    !wasteRooms.TryGetValue(room.Id, out var waste))
                {
                    continue;
                }

                var probability = EdibleWasteProbability(waste.Units);
                if (probability >= 1f ||
                    probability > 0f && Roll(dayNumber, room.Id, NaturalFoodKind.DiscardedFood) < probability)
                {
                    Ensure(room.Id, NaturalFoodKind.DiscardedFood, 1);
                }
            }
        }

        public bool TryConsume(string roomId, NaturalFoodKind kind)
        {
            var key = Key(roomId, kind);
            if (!sources.TryGetValue(key, out var source) || source.portions <= 0)
            {
                return false;
            }

            source.portions--;
            if (source.portions == 0)
            {
                sources.Remove(key);
            }
            return true;
        }

        public int PortionsIn(string roomId, NaturalFoodKind kind)
        {
            return sources.TryGetValue(Key(roomId, kind), out var source)
                ? source.portions
                : 0;
        }

        public List<NaturalFoodSaveData> Export()
        {
            return sources.Values
                .OrderBy(source => source.roomId, StringComparer.Ordinal)
                .ThenBy(source => source.kind)
                .Select(source => new NaturalFoodSaveData
                {
                    roomId = source.roomId,
                    kind = source.kind.ToString(),
                    portions = source.portions,
                    addedToday = source.addedToday,
                    dailyAdditionKnown = source.dailyAdditionKnown,
                    productionDayNumber = productionDayNumber
                })
                .ToList();
        }

        public void Restore(IEnumerable<NaturalFoodSaveData> savedSources, int currentDayNumber = 0)
        {
            sources.Clear();
            productionDayNumber = currentDayNumber > 0 ? currentDayNumber : -1;
            recordingCurrentDay = true;
            foreach (var item in savedSources ?? Array.Empty<NaturalFoodSaveData>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.roomId) ||
                    item.portions <= 0 || !Enum.TryParse<NaturalFoodKind>(item.kind, out var kind))
                {
                    continue;
                }
                var key = Key(item.roomId, kind);
                if (sources.TryGetValue(key, out var prior) && prior.portions >= item.portions)
                    continue;
                sources[key] = new NaturalFoodState
                {
                    roomId = item.roomId,
                    kind = kind,
                    portions = item.portions,
                    addedToday = Math.Max(0, item.addedToday),
                    dailyAdditionKnown = item.dailyAdditionKnown &&
                        (currentDayNumber <= 0 || item.productionDayNumber == currentDayNumber)
                };
                if (currentDayNumber <= 0 && item.dailyAdditionKnown)
                    productionDayNumber = item.productionDayNumber;
            }
        }

        public void Reset()
        {
            sources.Clear();
            productionDayNumber = -1;
            recordingCurrentDay = true;
        }

        public static float EdibleWasteProbability(int units)
        {
            return units switch
            {
                <= 0 => 0f,
                <= 5 => 0.25f,
                <= 7 => 0.5f,
                _ => 1f
            };
        }

        private void Ensure(string roomId, NaturalFoodKind kind, int maximumPortions)
        {
            if (maximumPortions <= 0) return;
            var key = Key(roomId, kind);
            if (sources.TryGetValue(key, out var source))
            {
                var added = Math.Max(0, maximumPortions - source.portions);
                source.portions += added;
                if (recordingCurrentDay)
                    source.addedToday += added;
                else
                    source.dailyAdditionKnown = false;
                return;
            }

            sources[key] = new NaturalFoodState
            {
                roomId = roomId,
                kind = kind,
                portions = maximumPortions,
                addedToday = recordingCurrentDay ? maximumPortions : 0,
                dailyAdditionKnown = recordingCurrentDay
            };
        }

        private void BeginProductionDay(int dayNumber)
        {
            // A large skip can process an older dusk/night event after the next
            // dawn callback. Keep stock correct, but do not attribute that late
            // event to today's production total.
            if (dayNumber < productionDayNumber)
            {
                recordingCurrentDay = false;
                return;
            }
            recordingCurrentDay = true;
            if (productionDayNumber == dayNumber) return;
            productionDayNumber = dayNumber;
            foreach (var source in sources.Values)
            {
                source.addedToday = 0;
                source.dailyAdditionKnown = true;
            }
        }

        private float Roll(int dayNumber, string roomId, NaturalFoodKind kind)
        {
            unchecked
            {
                var hash = runSeed;
                hash = hash * 397 ^ Math.Max(1, dayNumber);
                foreach (var character in roomId ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }
                hash = hash * 397 ^ (int)kind;
                var value = (uint)hash;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return (value & 0x00ffffff) / 16777216f;
            }
        }

        private static string Key(string roomId, NaturalFoodKind kind)
        {
            return $"{roomId}:{kind}";
        }
    }
}
