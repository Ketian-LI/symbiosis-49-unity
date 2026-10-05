using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalFoodAccessForecastTests
    {
        [Test]
        public void DayTwoParkDisturbanceRemovesAnimalPassageWithoutOpeningAHumanDoor()
        {
            var placements = new RoomLayoutModel(RoomLayoutData.All).ExportData();
            var today = new RoomNavigationMap(placements, RoomLayoutData.All, 1f, true, 1);
            var tomorrow = new RoomNavigationMap(placements, RoomLayoutData.All, 1f, true, 2);
            var people = new RoomNavigationMap(placements, RoomLayoutData.All, 1f,
                humanRoadsOnly: true);

            Assert.That(today.NeighboursOf("central-park"), Does.Contain("pigeon-b"));
            Assert.That(tomorrow.NeighboursOf("central-park"), Does.Not.Contain("pigeon-b"));
            Assert.That(people.NeighboursOf("central-park"), Does.Not.Contain("pigeon-b"));
        }

        [Test]
        public void ForecastCountsHabitatRoutesNotFoodPortionsAndExcludesFelledTrees()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var placements = layout.ExportData();
            var human = new RoomNavigationMap(placements, RoomLayoutData.All, 1f);
            var animal = new RoomNavigationMap(placements, RoomLayoutData.All, 1f, true, 2);
            var trees = new OakTreeGrowthModel(RoomLayoutData.All);

            var initial = HabitatFoodNetworkModel.ForecastHabitatAccess(
                animal, human, 2, trees);
            var movedOaks = RoomLayoutData.All.Where(room => room.Type == RoomType.OakHabitat)
                .Select(room => room.Id).ToHashSet();
            var afterFelling = HabitatFoodNetworkModel.ForecastHabitatAccess(
                animal, human, 2, trees, movedOaks);

            Assert.That(initial.Squirrel, Is.GreaterThan(0));
            Assert.That(afterFelling.Squirrel, Is.EqualTo(1),
                "The connected squirrel grove remains a food source after the oak plots are felled.");
            Assert.That(initial.Pigeon, Is.InRange(0, 4));
            Assert.That(initial.Hedgehog, Is.InRange(0, 3));
            Assert.That(initial.FoxPrey, Is.InRange(0, 1));
            var anyFoxRoute = RoomLayoutData.All
                .Where(room => room.Type == RoomType.PigeonHabitat ||
                               room.Type == RoomType.OakHabitat ||
                               room.Type == RoomType.ShrubHabitat)
                .Any(room => animal.TryFindRoute("shared-j", room.Id, out _));
            Assert.That(initial.FoxPrey, Is.EqualTo(anyFoxRoute ? 1 : 0));
            Assert.That(afterFelling.Pigeon, Is.EqualTo(initial.Pigeon));
            Assert.That(afterFelling.Hedgehog, Is.EqualTo(initial.Hedgehog));
        }

        [Test]
        public void EndOfDayFoodReachUsesTheSameDesperateDoorwaysAsVisibleMovement()
        {
            var rooms = new[]
            {
                new RoomSpec("shared-d", "Square", RoomType.SharedSpace, 0, 0, 1, 1, true, ""),
                new RoomSpec("office-a", "Office", RoomType.Office, 1, 0, 1, 1, true, "")
            };
            var placements = rooms.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row
            }).ToArray();
            var ordinary = new RoomNavigationMap(placements, rooms, 2f, animalPassagesOnly: true);
            var desperate = new RoomNavigationMap(placements, rooms, 2f,
                animalPassagesOnly: true, hungryWildlifeMayUsePedestrianDoors: true);
            var food = new[]
            {
                new NaturalFoodState { roomId = "office-a", kind = NaturalFoodKind.Nut, portions = 1 }
            };

            foreach (var hungerDays in new[] { 0, 1 })
                Assert.That(HabitatFoodNetworkModel.TryChooseReachableSource(
                    RoomNavigationMap.ForWildlifeHunger(ordinary, desperate, hungerDays),
                    "shared-d", WildlifeSpecies.Squirrel, NaturalFoodKind.Nut, food, out _),
                    Is.False);

            Assert.That(HabitatFoodNetworkModel.TryChooseReachableSource(
                RoomNavigationMap.ForWildlifeHunger(ordinary, desperate, 2),
                "shared-d", WildlifeSpecies.Squirrel, NaturalFoodKind.Nut, food, out var destination),
                Is.True);
            Assert.That(destination, Is.EqualTo("office-a"));
        }

        [Test]
        public void DesperateFoodReachStillRespectsAnEventClosedDoor()
        {
            var rooms = new[]
            {
                new RoomSpec("shared-h", "Garden", RoomType.SharedSpace, 0, 0, 1, 1, true, ""),
                new RoomSpec("office-a", "Office", RoomType.Office, 1, 0, 1, 1, true, "")
            };
            var placements = rooms.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row
            }).ToArray();
            var closed = new RoomNavigationMap(placements, rooms, 2f,
                animalPassagesOnly: true, animalDayNumber: 5,
                hungryWildlifeMayUsePedestrianDoors: true);
            var food = new[]
            {
                new NaturalFoodState { roomId = "office-a", kind = NaturalFoodKind.Nut, portions = 1 }
            };

            Assert.That(HabitatFoodNetworkModel.TryChooseReachableSource(
                closed, "shared-h", WildlifeSpecies.Squirrel, NaturalFoodKind.Nut, food, out _),
                Is.False);
        }

        [Test]
        public void FoxFoodRouteCanCrossGarageWithTrafficRisk()
        {
            var rooms = new[]
            {
                new RoomSpec("shared-j", "Fox edge", RoomType.SharedSpace, 0, 0, 1, 1, true, ""),
                new RoomSpec("garage-c", "Garage", RoomType.Garage, 1, 0, 1, 1, true, ""),
                new RoomSpec("canteen-a", "Shop", RoomType.Canteen, 1, 1, 1, 1, true, "")
            };
            var placements = rooms.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row
            }).ToArray();
            var navigation = new RoomNavigationMap(placements, rooms, 2f, animalPassagesOnly: true);

            Assert.That(navigation.TryFindRoute("shared-j", "canteen-a", out var route), Is.True);
            Assert.That(route, Does.Contain("garage-c"));
            Assert.That(HabitatFoodNetworkModel.CanReachSource(navigation, "shared-j",
                WildlifeSpecies.Fox, "canteen-a", out var distance), Is.True);
            Assert.That(distance, Is.EqualTo(2));
        }
    }
}
