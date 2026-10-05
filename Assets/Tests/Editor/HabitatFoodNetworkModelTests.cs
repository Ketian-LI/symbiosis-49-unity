using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class HabitatFoodNetworkModelTests
    {
        [Test]
        public void LinkedGreenCellsMakeTwoOpeningPlazasProductive()
        {
            var map = Navigation(new RoomLayoutModel(RoomLayoutData.All), 1);
            Assert.That(GreenNetworkModel.ConnectedCount(map), Is.EqualTo(5));
            Assert.That(HabitatFoodNetworkModel.ParkLinkedPigeonHabitats(map), Is.EqualTo(2));
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-b"), Is.EqualTo(5));
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-c"), Is.EqualTo(5));
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-a"), Is.Zero);
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-d"), Is.Zero);
            Assert.That(HabitatFoodNetworkModel.TotalSeedCapacity(map), Is.EqualTo(11));
        }

        [Test]
        public void SeedMealCeilingCountsBirdsAndReachablePortionsOnlyOnce()
        {
            var map = Navigation(new RoomLayoutModel(RoomLayoutData.All), 1);
            var threeAtPlaza = HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                map, 1, new[] { new KeyValuePair<string, int>("pigeon-b", 3) });
            Assert.That(threeAtPlaza, Is.EqualTo(3));

            var manyAtPlaza = HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                map, 1, new[] { new KeyValuePair<string, int>("pigeon-b", 20) });
            Assert.That(manyAtPlaza, Is.LessThanOrEqualTo(
                HabitatFoodNetworkModel.TotalSeedCapacity(map, 1)));
            Assert.That(manyAtPlaza, Is.LessThan(20),
                "One reachable portion cannot be promised to several birds.");
            var none = HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                map, 1, new[] { new KeyValuePair<string, int>("pigeon-b", 0) });
            Assert.That(none, Is.Zero);
        }

        [Test]
        public void DailyRouteClosureLosesProductionAndATargetedSwapRepairsIt()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("residence-d", "pigeon-d"), Is.True);
            var calmSeeds = HabitatFoodNetworkModel.TotalSeedCapacity(Navigation(layout, 1), 1);
            var closed = Navigation(layout, 2);
            Assert.That(GreenNetworkModel.ConnectedCount(closed), Is.GreaterThanOrEqualTo(4));
            var closedSeeds = HabitatFoodNetworkModel.TotalSeedCapacity(closed, 2);
            Assert.That(closedSeeds, Is.LessThan(calmSeeds));

            Assert.That(layout.TrySwap("pigeon-a", "shared-j"), Is.True);
            var repaired = Navigation(layout, 2);
            Assert.That(GreenNetworkModel.ConnectedCount(repaired), Is.EqualTo(4));
            Assert.That(HabitatFoodNetworkModel.TotalSeedCapacity(repaired, 2),
                Is.GreaterThan(closedSeeds));
        }

        [Test]
        public void RestingPlotSuppressesOnlyItsOwnPigeonPlaza()
        {
            var map = Navigation(new RoomLayoutModel(RoomLayoutData.All), 4);
            Assert.That(ParkEdgeRestSchedule.TryGetRestingCell(4, out var column, out var row), Is.True);
            Assert.That((column, row), Is.EqualTo((1, 2)));
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-b", 4), Is.EqualTo(1));
            Assert.That(HabitatFoodNetworkModel.SeedCapacity(map, "pigeon-c", 4), Is.EqualTo(5));
            Assert.That(HabitatFoodNetworkModel.TotalSeedCapacity(map, 4), Is.EqualTo(7));
        }

        [Test]
        public void ChoiceUsesNearbySourceAndRejectsFoodOutsideSpeciesRange()
        {
            var map = Navigation(new RoomLayoutModel(RoomLayoutData.All), 1);
            var onlyPark = new List<NaturalFoodState>
            {
                new() { roomId = "central-park", kind = NaturalFoodKind.Seed, portions = 5 }
            };
            Assert.That(HabitatFoodNetworkModel.TryChooseReachableSource(
                map, "pigeon-d", WildlifeSpecies.Pigeon, NaturalFoodKind.Seed,
                onlyPark, out _), Is.False);

            onlyPark.Add(new NaturalFoodState
                { roomId = "pigeon-d", kind = NaturalFoodKind.Seed, portions = 1 });
            Assert.That(HabitatFoodNetworkModel.TryChooseReachableSource(
                map, "pigeon-d", WildlifeSpecies.Pigeon, NaturalFoodKind.Seed,
                onlyPark, out var chosen), Is.True);
            Assert.That(chosen, Is.EqualTo("pigeon-d"));
        }

        [Test]
        public void SquirrelForagingStaysInHomeOrOneDirectlyConnectedRoom()
        {
            var map = Navigation(new RoomLayoutModel(RoomLayoutData.All), 1);
            const string home = "oak-a";
            var adjacent = map.NeighboursOf(home)
                .First(roomId => HabitatFoodNetworkModel.CanReachSource(
                    map, home, WildlifeSpecies.Squirrel, roomId, out _) &&
                    map.NeighboursOf(roomId).Any(next => next != home &&
                        !HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(map, home, next)));
            var beyond = map.NeighboursOf(adjacent)
                .First(roomId => !HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                    map, home, roomId));

            Assert.That(HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(map, home, home), Is.True);
            Assert.That(HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(map, home, adjacent), Is.True);
            Assert.That(HabitatFoodNetworkModel.CanReachSource(
                map, home, WildlifeSpecies.Squirrel, adjacent, out var distance), Is.True);
            Assert.That(distance, Is.EqualTo(1));
            Assert.That(HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(map, home, beyond), Is.False);
            Assert.That(HabitatFoodNetworkModel.CanReachSource(
                map, home, WildlifeSpecies.Squirrel, beyond, out _), Is.False);
            Assert.That(HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(
                map, home, new[] { home, adjacent }), Is.True);
            Assert.That(HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(
                map, home, new[] { home, adjacent, beyond }), Is.False);
        }

        private static RoomNavigationMap Navigation(RoomLayoutModel layout, int day) =>
            new(layout.ExportData(), RoomLayoutData.All, 3.1f, true, day);
    }
}
