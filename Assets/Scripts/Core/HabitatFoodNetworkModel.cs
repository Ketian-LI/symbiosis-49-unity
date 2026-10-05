using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Counts habitat rooms with a route to a possible next-day food source,
    // not individual animals, portions, or guaranteed meals.
    public readonly struct AnimalFoodAccessSnapshot
    {
        public AnimalFoodAccessSnapshot(int pigeon, int squirrel, int hedgehog, int foxPrey)
        {
            Pigeon = pigeon;
            Squirrel = squirrel;
            Hedgehog = hedgehog;
            FoxPrey = foxPrey;
        }

        public int Pigeon { get; }
        public int Squirrel { get; }
        public int Hedgehog { get; }
        public int FoxPrey { get; }
    }

    // A small, inspectable rule set for the spatial food puzzle. The room graph,
    // rather than the order of entries in a global food dictionary, decides
    // which source can serve an animal.
    public static class HabitatFoodNetworkModel
    {
        public const int ParkEdgeSeedBonus = 4;

        private static readonly HashSet<string> GarageRooms = RoomLayoutData.All
            .Where(room => room.Type == RoomType.Garage)
            .Select(room => room.Id)
            .ToHashSet(StringComparer.Ordinal);

        public static AnimalFoodAccessSnapshot ForecastHabitatAccess(
            RoomNavigationMap animalNavigation, RoomNavigationMap humanNavigation,
            int dayNumber, OakTreeGrowthModel oakTrees = null,
            ISet<string> movedRooms = null)
        {
            if (animalNavigation == null || humanNavigation == null)
                return default;

            var pigeonHomes = RoomLayoutData.All.Where(room => room.Type == RoomType.PigeonHabitat)
                .Select(room => room.Id).ToArray();
            var oakHomes = RoomLayoutData.All.Where(room => room.Type == RoomType.OakHabitat)
                .Select(room => room.Id).ToArray();
            var shrubHomes = RoomLayoutData.All.Where(room => room.Type == RoomType.ShrubHabitat)
                .Select(room => room.Id).ToArray();
            var seedSources = pigeonHomes
                .Where(id => SeedCapacity(animalNavigation, id, dayNumber) > 0)
                .Concat(new[] { "central-park" }).ToArray();
            var nutSources = oakTrees == null ? Array.Empty<string>() : oakHomes
                .Where(id => movedRooms == null || !movedRooms.Contains(id))
                .Where(id => oakTrees.StageOf(id) == OakTreeStage.Mature ||
                             oakTrees.Trees.TryGetValue(id, out var tree) &&
                             (tree.stage == OakTreeStage.Sapling || tree.stage == OakTreeStage.Young) &&
                             tree.completeDaysSincePlanting >= 1)
                .ToArray();
            var connectedGreen = GreenNetworkModel.ConnectedRooms(animalNavigation);
            var grove = RoomLayoutData.All.First(item => item.GreenRole == ParkGreenRole.SquirrelGrove).Id;
            var garden = RoomLayoutData.All.First(item => item.GreenRole == ParkGreenRole.HedgehogGarden).Id;
            var squirrelHomes = oakHomes.Concat(new[] { grove }).ToArray();
            var hedgehogHomes = shrubHomes.Concat(new[] { garden }).ToArray();
            var preyHomes = pigeonHomes.Concat(squirrelHomes).Concat(hedgehogHomes).ToArray();
            // FoxPredationController uses the full animal route (with garage
            // hazards), not the short foraging limits used by small animals.
            var foxReach = preyHomes.Any(id =>
                animalNavigation.TryFindRoute("shared-j", id, out _)) ? 1 : 0;

            int ReachableHomes(IEnumerable<string> homes, WildlifeSpecies species,
                IEnumerable<string> sources) => homes.Count(home => sources.Any(source =>
                    CanReachSource(animalNavigation, home, species, source, out _)));

            return new AnimalFoodAccessSnapshot(
                ReachableHomes(pigeonHomes, WildlifeSpecies.Pigeon, seedSources),
                ReachableHomes(squirrelHomes, WildlifeSpecies.Squirrel,
                    nutSources.Concat(connectedGreen.Count >= GreenNetworkModel.StableCellCount &&
                                      connectedGreen.Contains(grove) ? new[] { grove } : Array.Empty<string>())),
                ReachableHomes(hedgehogHomes, WildlifeSpecies.Hedgehog,
                    new[] { "central-park" }.Concat(connectedGreen.Count >= GreenNetworkModel.StableCellCount &&
                        connectedGreen.Contains(garden)
                        ? new[] { garden } : Array.Empty<string>())),
                foxReach);
        }

        public static bool BordersPark(RoomNavigationMap navigation, string roomId)
        {
            return navigation != null && navigation.NeighboursOf(roomId).Contains("central-park");
        }

        public static int SeedCapacity(RoomNavigationMap navigation, string roomId)
        {
            return SeedCapacity(navigation, roomId, 1);
        }

        public static int SeedCapacity(RoomNavigationMap navigation, string roomId, int dayNumber)
        {
            if (roomId == "central-park")
            {
                return 1;
            }
            var room = RoomLayoutData.All.FirstOrDefault(item => item.Id == roomId);
            if (room?.Type != RoomType.PigeonHabitat) return 0;
            // A plaza needs a facing animal exit to the connected green
            // network to replenish seed. With fewer than four green cells,
            // or during a park-edge rest, only its one base portion remains.
            var green = GreenNetworkModel.ConnectedRooms(navigation);
            if (!green.Any(id => navigation.NeighboursOf(roomId).Contains(id))) return 0;
            return 1 + (green.Count >= GreenNetworkModel.StableCellCount &&
                        !ParkEdgeRestSchedule.IsResting(navigation, roomId, dayNumber)
                ? ParkEdgeSeedBonus : 0);
        }

        public static int TotalSeedCapacity(RoomNavigationMap navigation)
        {
            return TotalSeedCapacity(navigation, 1);
        }

        public static int TotalSeedCapacity(RoomNavigationMap navigation, int dayNumber)
        {
            return RoomLayoutData.All.Sum(room => SeedCapacity(navigation, room.Id, dayNumber));
        }

        // Capacity-aware upper bound for newly replenished seeds only. A route
        // to one portion cannot promise a meal to every bird in a flock, and
        // several flocks may compete for the same reachable source. Existing
        // stock, other food and visible travel are deliberately excluded.
        public static int ForecastPigeonSeedMealCeiling(RoomNavigationMap navigation,
            int dayNumber, IEnumerable<KeyValuePair<string, int>> livingPigeonsByHome)
        {
            if (navigation == null || livingPigeonsByHome == null)
                return 0;

            var birds = livingPigeonsByHome
                .Where(pair => !string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .SelectMany(pair => Enumerable.Repeat(pair.Key, pair.Value))
                .ToArray();
            var portions = RoomLayoutData.All
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .SelectMany(room => Enumerable.Repeat(room.Id,
                    SeedCapacity(navigation, room.Id, dayNumber)))
                .ToArray();
            var ownerByPortion = Enumerable.Repeat(-1, portions.Length).ToArray();

            bool Assign(int bird, bool[] visited)
            {
                for (var slot = 0; slot < portions.Length; slot++)
                {
                    if (visited[slot] || !CanReachSource(navigation, birds[bird],
                            WildlifeSpecies.Pigeon, portions[slot], out _))
                        continue;
                    visited[slot] = true;
                    if (ownerByPortion[slot] >= 0 &&
                        !Assign(ownerByPortion[slot], visited))
                        continue;
                    ownerByPortion[slot] = bird;
                    return true;
                }
                return false;
            }

            var matched = 0;
            for (var bird = 0; bird < birds.Length; bird++)
                if (Assign(bird, new bool[portions.Length])) matched++;
            return matched;
        }

        public static int ParkLinkedPigeonHabitats(RoomNavigationMap navigation)
        {
            return ParkLinkedPigeonHabitats(navigation, 1);
        }

        public static int ParkLinkedPigeonHabitats(RoomNavigationMap navigation, int dayNumber)
        {
            return RoomLayoutData.All.Count(room =>
                room.Type == RoomType.PigeonHabitat &&
                SeedCapacity(navigation, room.Id, dayNumber) > 1);
        }

        public static bool TryChooseReachableSource(
            RoomNavigationMap navigation,
            string startRoomId,
            WildlifeSpecies species,
            NaturalFoodKind kind,
            IEnumerable<NaturalFoodState> sources,
            out string sourceRoomId)
        {
            sourceRoomId = null;
            if (navigation == null || string.IsNullOrEmpty(startRoomId))
            {
                return false;
            }

            var bestDistance = int.MaxValue;
            foreach (var source in sources ?? Array.Empty<NaturalFoodState>())
            {
                if (source == null || source.kind != kind || source.portions <= 0 ||
                    !CanReachSource(navigation, startRoomId, species, source.roomId, out var distance))
                {
                    continue;
                }
                if (distance > bestDistance ||
                    distance == bestDistance &&
                    string.CompareOrdinal(source.roomId, sourceRoomId) >= 0)
                {
                    continue;
                }
                bestDistance = distance;
                sourceRoomId = source.roomId;
            }
            return sourceRoomId != null;
        }

        public static bool CanReachSource(
            RoomNavigationMap navigation,
            string startRoomId,
            WildlifeSpecies species,
            string sourceRoomId,
            out int distance)
        {
            distance = -1;
            if (navigation == null || string.IsNullOrEmpty(startRoomId) ||
                string.IsNullOrEmpty(sourceRoomId))
            {
                return false;
            }
            var maxSteps = species switch
            {
                WildlifeSpecies.Pigeon => 2,
                WildlifeSpecies.Squirrel => 1,
                WildlifeSpecies.Hedgehog => 2,
                WildlifeSpecies.Fox => 5,
                _ => 0
            };
            // Foxes may cross a garage at a traffic risk. The smaller ground
            // foragers avoid garages when choosing a natural-food route.
            var excluded = species is WildlifeSpecies.Pigeon or WildlifeSpecies.Fox
                ? null : GarageRooms;
            if (!navigation.TryFindRoute(startRoomId, sourceRoomId, excluded, out var route))
            {
                return false;
            }
            distance = route.Count - 1;
            return distance <= maxSteps;
        }

        // A squirrel's one-room range is anchored to its cache/home, not to
        // whichever room it happens to occupy after a cancelled trip.
        public static bool IsWithinSquirrelHomeRange(
            RoomNavigationMap navigation, string homeRoomId, string roomId)
        {
            return navigation != null && !string.IsNullOrEmpty(homeRoomId) &&
                   !string.IsNullOrEmpty(roomId) &&
                   (homeRoomId == roomId ||
                    navigation.NeighboursOf(homeRoomId).Contains(roomId));
        }

        public static bool SquirrelRouteStaysNearHome(
            RoomNavigationMap navigation, string homeRoomId,
            IReadOnlyList<string> route)
        {
            return route != null && route.Count > 0 && route.All(roomId =>
                IsWithinSquirrelHomeRange(navigation, homeRoomId, roomId));
        }
    }

    // A different park-edge plot rests every two days from day 4. Only a
    // pigeon plaza on that plot loses its bonus; other plazas can replenish
    // through any connected green cell. Coordinates are board cells, not IDs.
    public static class ParkEdgeRestSchedule
    {
        public const int FirstDay = 4;
        public const int RotationDays = 2;
        private static readonly (int column, int row)[] Beds =
        {
            (1, 2), (3, 2), (2, 3), (2, 1)
        };

        public static bool TryGetRestingCell(int dayNumber, out int column, out int row)
        {
            if (dayNumber < FirstDay)
            {
                column = row = -1;
                return false;
            }

            var bed = Beds[((dayNumber - FirstDay) / RotationDays) % Beds.Length];
            column = bed.column;
            row = bed.row;
            return true;
        }

        public static bool IsResting(RoomNavigationMap navigation, string roomId, int dayNumber)
        {
            return navigation != null &&
                   TryGetRestingCell(dayNumber, out var restingColumn, out var restingRow) &&
                   navigation.TryGetOrigin(roomId, out var column, out var row) &&
                   column == restingColumn && row == restingRow;
        }
    }
}
