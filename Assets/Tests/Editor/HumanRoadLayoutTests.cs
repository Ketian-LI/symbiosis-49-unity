using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class HumanRoadLayoutTests
    {
        [Test]
        public void PedestrianAndWildlifeEntrancesUseDifferentDoors()
        {
            var rooms = new[]
            {
                new RoomSpec("shared-d", "Square", RoomType.SharedSpace,
                    1, 0, 1, 1, true, ""),
                new RoomSpec("central-park", "Park", RoomType.CentralPark,
                    1, 1, 1, 1, false, ""),
                new RoomSpec("pigeon-b", "Birds", RoomType.PigeonHabitat,
                    0, 1, 1, 1, true, "")
            };
            var placements = rooms.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row
            }).ToArray();
            var pedestrians = new RoomNavigationMap(placements, rooms, 2f,
                humanRoadsOnly: true);
            var animals = new RoomNavigationMap(placements, rooms, 2f, true);

            Assert.That(pedestrians.TryFindRoute("shared-d", "central-park", out _), Is.True);
            Assert.That(animals.TryFindRoute("shared-d", "central-park", out _), Is.False);
            Assert.That(animals.TryFindRoute("pigeon-b", "central-park", out _), Is.True);
            Assert.That(pedestrians.TryFindRoute("pigeon-b", "central-park", out _), Is.False);
        }

        [Test]
        public void EveryDoorBelongsToAtMostOneNetworkEvenAfterRotation()
        {
            foreach (var room in RoomLayoutData.All)
            for (var turn = 0; turn < 4; turn++)
            {
                var human = HumanRoadLayout.Ports(room, turn)
                    .Select(port => (port.Edge, port.Segment)).ToHashSet();
                var animal = AnimalPassageLayout.Ports(room, turn)
                    .Select(port => (port.Edge, port.Segment)).ToHashSet();
                Assert.That(human.Overlaps(animal), Is.False,
                    $"{room.Id} shares a doorway after {turn} quarter-turns.");
            }
        }

        [Test]
        public void StartingLayoutKeepsMinimumWorkingResidentsOnPedestrianRoads()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var pedestrians = new RoomNavigationMap(layout.ExportData(),
                RoomLayoutData.All, 3.1f, humanRoadsOnly: true);
            var population = new ResidentPopulationModel(pedestrians, RoomLayoutData.All);

            Assert.That(pedestrians.ConnectionCount, Is.LessThan(84),
                "Pedestrian roads must actually constrain the full-door graph.");
            Assert.That(population.PreviewCommute(pedestrians).WorkingResidents,
                Is.EqualTo(ResidentPopulationModel.StartingResidents),
                "All starting residents need a viable office-and-food route.");
        }

        [Test]
        public void AnimalOnlyDoorDoesNotPretendToBeAPedestrianRoad()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var pedestrians = new RoomNavigationMap(layout.ExportData(),
                RoomLayoutData.All, 3.1f, humanRoadsOnly: true);
            var animals = new RoomNavigationMap(layout.ExportData(),
                RoomLayoutData.All, 3.1f, true);

            Assert.That(pedestrians.NeighboursOf("shared-f"), Does.Contain("shared-i"));
            Assert.That(animals.NeighboursOf("shared-f"), Does.Not.Contain("shared-i"));
            Assert.That(pedestrians.NeighboursOf("central-park"), Does.Not.Contain("shared-f"));
            Assert.That(animals.NeighboursOf("central-park"), Does.Contain("shared-f"));
        }

        [Test]
        public void RotatingAVisibleRoadChangesTheCommuteForecast()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var before = new RoomNavigationMap(layout.ExportData(),
                RoomLayoutData.All, 3.1f, humanRoadsOnly: true);
            var population = new ResidentPopulationModel(before, RoomLayoutData.All);
            var workingBefore = population.PreviewCommute(before).WorkingResidents;

            Assert.That(layout.TryRotate("shared-i"), Is.True);
            var after = new RoomNavigationMap(layout.ExportData(),
                RoomLayoutData.All, 3.1f, humanRoadsOnly: true);
            var workingAfter = population.PreviewCommute(after).WorkingResidents;
            TestContext.WriteLine($"Connection-court rotation: workers {workingBefore} -> {workingAfter}.");
            Assert.That(workingAfter,
                Is.LessThan(workingBefore),
                "A road rotation should be a meaningful, forecastable spatial decision.");
        }

        [Test]
        public void StartingBoardContainsBothKindsOfExclusiveConnection()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var placements = layout.ExportData();
            var physical = new RoomNavigationMap(placements, RoomLayoutData.All, 3.1f);
            var pedestrians = new RoomNavigationMap(placements,
                RoomLayoutData.All, 3.1f, humanRoadsOnly: true);
            var animals = new RoomNavigationMap(placements,
                RoomLayoutData.All, 3.1f, true);
            var pedestrianOnly = 0;
            var animalOnly = 0;
            var shared = 0;
            foreach (var room in RoomLayoutData.All)
            foreach (var neighbour in physical.NeighboursOf(room.Id))
            {
                if (string.CompareOrdinal(room.Id, neighbour) >= 0) continue;
                var pedestrian = pedestrians.NeighboursOf(room.Id).Contains(neighbour);
                var animal = animals.NeighboursOf(room.Id).Contains(neighbour);
                if (pedestrian && animal) shared++;
                else if (pedestrian) pedestrianOnly++;
                else if (animal) animalOnly++;
            }

            TestContext.WriteLine($"Starting links: pedestrian-only {pedestrianOnly}, " +
                                  $"animal-only {animalOnly}, shared {shared}.");
            Assert.That(animalOnly, Is.GreaterThan(0));
            Assert.That(pedestrianOnly, Is.GreaterThan(0));
            Assert.That(shared, Is.Zero, "A doorway may not serve both networks.");
        }

        [Test]
        public void RotatingTheCornerTurnsBothExclusivePortPatterns()
        {
            var corner = RoomLayoutData.All.Single(room => room.Id == "shared-g");
            var before = HumanRoadLayout.Ports(corner, 0);
            var after = HumanRoadLayout.Ports(corner, 1);
            Assert.That(before.Any(port => port.Edge == AnimalPassageEdge.North), Is.True);
            Assert.That(after.Any(port => port.Edge == AnimalPassageEdge.West), Is.False);
            Assert.That(AnimalPassageLayout.Ports(corner, 0).Count, Is.EqualTo(2));
            Assert.That(AnimalPassageLayout.Ports(corner, 1).Count, Is.EqualTo(2));
        }
    }
}
