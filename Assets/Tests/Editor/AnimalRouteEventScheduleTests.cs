using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalRouteEventScheduleTests
    {
        [Test]
        public void OpeningLayoutHasAllRouteShapesAndAConnectedGreenCore()
        {
            var oneCell = RoomLayoutData.All.Where(room => room.CellCount == 1).ToArray();
            var portCounts = oneCell.Select(room => AnimalPassageLayout.Ports(room, 0).Count).ToArray();
            Assert.That(portCounts.Count(count => count == 4), Is.GreaterThan(0));
            Assert.That(portCounts.Count(count => count == 3), Is.GreaterThan(0));
            Assert.That(portCounts.Count(count => count == 2), Is.GreaterThan(0));
            Assert.That(portCounts.Count(count => count == 1), Is.GreaterThan(0));
            Assert.That(oneCell.Count(room => IsStraight(AnimalPassageLayout.Ports(room, 0))), Is.GreaterThan(0));
            Assert.That(oneCell.Count(room => IsCorner(AnimalPassageLayout.Ports(room, 0))), Is.GreaterThan(0));

            var placements = RoomLayoutData.All.Select(Placement).ToArray();
            var people = new RoomNavigationMap(placements, RoomLayoutData.All, 2f,
                humanRoadsOnly: true);
            var animals = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true);
            Assert.That(people.ConnectionCount, Is.EqualTo(48));
            Assert.That(animals.ConnectionCount, Is.EqualTo(36));
            foreach (var roomId in new[] { "shared-f", "shared-h", "shared-i", "shared-j", "pigeon-b", "pigeon-c" })
                Assert.That(animals.TryFindRoute("central-park", roomId, out _), Is.True, roomId);
        }

        [Test]
        public void EveryDayFromTwoHasOneForecastableAnimalOnlyDisturbance()
        {
            var placements = RoomLayoutData.All.Select(Placement).ToArray();
            Assert.That(AnimalRouteEventSchedule.ForDay(1), Is.Null);
            for (var day = 2; day <= 16; day++)
            {
                var routeEvent = AnimalRouteEventSchedule.ForDay(day);
                Assert.That(routeEvent.HasValue, Is.True, $"day {day}");
                Assert.That(routeEvent.Value.DayNumber, Is.EqualTo(day));
                var candidate = new RoomLayoutModel(RoomLayoutData.All);
                var candidatePlacements = candidate.ExportData();
                var calmEdges = new RoomNavigationMap(candidatePlacements,
                    RoomLayoutData.All, 2f, true).ConnectionCount;
                var animals = new RoomNavigationMap(candidatePlacements,
                    RoomLayoutData.All, 2f, true, day);
                var people = new RoomNavigationMap(candidatePlacements,
                    RoomLayoutData.All, 2f, humanRoadsOnly: true);
                Assert.That(animals.ConnectionCount, Is.LessThan(calmEdges), $"day {day}");
                Assert.That(people.ConnectionCount, Is.EqualTo(48), $"day {day}");
            }
        }

        [Test]
        public void ParkDisturbanceHasARealDetourAndShrubDisturbanceCanBeRearranged()
        {
            var placements = RoomLayoutData.All.Select(Placement).ToArray();
            var parkDay = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true, 2);
            Assert.That(parkDay.TryGetConnectionPoint("pigeon-b", "central-park", out _), Is.False);
            Assert.That(GreenNetworkModel.ConnectedCount(parkDay), Is.GreaterThanOrEqualTo(4),
                "A single visitor closure must not collapse the whole green food network.");

            var shrubDay = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true, 3);
            Assert.That(shrubDay.TryGetConnectionPoint("shrub-a", "shared-k", out _), Is.False);
            var shrub = placements.Single(item => item.id == "shrub-a");
            var clearing = placements.Single(item => item.id == "shared-h");
            (shrub.column, clearing.column) = (clearing.column, shrub.column);
            (shrub.row, clearing.row) = (clearing.row, shrub.row);
            var rearranged = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true, 3);
            Assert.That(rearranged.TryFindRoute("shrub-a", "central-park", out var route), Is.True);
            Assert.That(route.Count - 1, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void MovingTheFoodShopCreatesAnAlternativeDuringCrowdEvent()
        {
            var placements = RoomLayoutData.All.Select(Placement).ToArray();
            var crowded = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true, 4);
            Assert.That(crowded.TryGetConnectionPoint("canteen-a", "shared-j", out _), Is.False);

            var shop = placements.Single(item => item.id == "canteen-a");
            var clearing = placements.Single(item => item.id == "shared-f");
            (shop.column, clearing.column) = (clearing.column, shop.column);
            (shop.row, clearing.row) = (clearing.row, shop.row);
            shop.quarterTurns = 3;
            var moved = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true, 4);
            Assert.That(moved.TryGetConnectionPoint("canteen-a", "central-park", out _), Is.True);
            Assert.That(moved.TryFindRoute("fox-den", "canteen-a", out _), Is.True);
        }

        private static bool IsStraight(System.Collections.Generic.IReadOnlyList<AnimalPassagePort> ports) =>
            ports.Count == 2 && ((int)ports[0].Edge + 2) % 4 == (int)ports[1].Edge;

        private static bool IsCorner(System.Collections.Generic.IReadOnlyList<AnimalPassagePort> ports) =>
            ports.Count == 2 && !IsStraight(ports);

        private static RoomPlacementData Placement(RoomSpec room) => new()
        {
            id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
        };
    }
}
