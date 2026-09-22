using System;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Presentation;
using UnityEngine;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomShellLayoutTests
    {
        private const float CellSize = 3.1f;
        private const float RoomGap = 0.16f;

        [TestCase(1, 1, 4)]
        [TestCase(1, 2, 6)]
        [TestCase(2, 1, 6)]
        [TestCase(2, 2, 8)]
        public void DoorwayCountFollowsFootprintPerimeter(int width, int height, int expected)
        {
            var doorways = RoomShellLayout.CreateDoorways(width, height, CellSize, RoomGap);
            Assert.That(doorways, Has.Count.EqualTo(expected));
        }

        [Test]
        public void VerticalOneByTwoHasOneDoorOnShortEdgesAndTwoOnLongEdges()
        {
            var doorways = RoomShellLayout.CreateDoorways(1, 2, CellSize, RoomGap);

            Assert.That(doorways.Count(item => item.Edge == RoomEdge.North), Is.EqualTo(1));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.South), Is.EqualTo(1));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.West), Is.EqualTo(2));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.East), Is.EqualTo(2));
        }

        [Test]
        public void HorizontalTwoByOneHasTwoDoorsOnLongEdgesAndOneOnShortEdges()
        {
            var doorways = RoomShellLayout.CreateDoorways(2, 1, CellSize, RoomGap);

            Assert.That(doorways.Count(item => item.Edge == RoomEdge.North), Is.EqualTo(2));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.South), Is.EqualTo(2));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.West), Is.EqualTo(1));
            Assert.That(doorways.Count(item => item.Edge == RoomEdge.East), Is.EqualTo(1));
        }

        [Test]
        public void RotatedFootprintsUseTheSameSixDoorCentersRotatedByNinetyDegrees()
        {
            var vertical = RoomShellLayout.CreateDoorways(1, 2, CellSize, RoomGap);
            var horizontal = RoomShellLayout.CreateDoorways(2, 1, CellSize, RoomGap);

            foreach (var doorway in vertical)
            {
                var rotatedX = doorway.LocalCenter.z;
                var rotatedZ = -doorway.LocalCenter.x;
                Assert.That(
                    horizontal.Any(candidate =>
                        candidate.LocalCenter.x == rotatedX && candidate.LocalCenter.z == rotatedZ),
                    Is.True);
            }
        }

        [TestCase(1, 1, 4)]
        [TestCase(1, 2, 6)]
        [TestCase(2, 1, 6)]
        public void EveryDoorwayCreatesOneReservedInteriorClearance(int width, int height, int expected)
        {
            var zones = RoomShellLayout.CreateDoorClearances(
                width,
                height,
                CellSize,
                RoomGap,
                0.72f,
                0.85f,
                0.06f);

            Assert.That(zones, Has.Count.EqualTo(expected));
            Assert.That(zones.All(zone => zone.Size.x > 0f && zone.Size.y > 0f), Is.True);
        }

        [Test]
        public void ClearanceZonesExtendInwardFromEachWall()
        {
            var zones = RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0.85f, 0.06f);

            Assert.That(zones.Single(zone => zone.Edge == RoomEdge.North).LocalCenter.y, Is.LessThan(0.5f * (CellSize - RoomGap)));
            Assert.That(zones.Single(zone => zone.Edge == RoomEdge.South).LocalCenter.y, Is.GreaterThan(-0.5f * (CellSize - RoomGap)));
            Assert.That(zones.Single(zone => zone.Edge == RoomEdge.West).LocalCenter.x, Is.GreaterThan(-0.5f * (CellSize - RoomGap)));
            Assert.That(zones.Single(zone => zone.Edge == RoomEdge.East).LocalCenter.x, Is.LessThan(0.5f * (CellSize - RoomGap)));
        }

        [Test]
        public void OverlapTestRejectsFurnitureInsideDoorLanding()
        {
            var north = RoomShellLayout
                .CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0.85f, 0.06f)
                .Single(zone => zone.Edge == RoomEdge.North);

            Assert.That(north.Overlaps(north.LocalCenter, new Vector2(0.3f, 0.3f)), Is.True);
            Assert.That(north.Overlaps(new Vector2(1.1f, 0f), new Vector2(0.3f, 0.3f)), Is.False);
        }

        [Test]
        public void ClearanceParametersRejectInvalidSizes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0f, 0.85f, 0.06f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0f, 0.06f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0.85f, -0.01f));
        }

        [Test]
        public void EmptyRoomConnectsEveryDoorLanding()
        {
            var zones = RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0.85f, 0.06f);

            var connected = RoomShellLayout.AreDoorClearancesConnected(
                CellSize - RoomGap,
                CellSize - RoomGap,
                zones,
                Array.Empty<RoomObstacle2D>(),
                0.24f,
                0.10f,
                out _);

            Assert.That(connected, Is.True);
        }

        [Test]
        public void FullWidthFurnitureBarrierDisconnectsOppositeDoors()
        {
            var roomSize = CellSize - RoomGap;
            var zones = RoomShellLayout.CreateDoorClearances(1, 1, CellSize, RoomGap, 0.72f, 0.85f, 0.06f);
            var barrier = new[]
            {
                new RoomObstacle2D(Vector2.zero, new Vector2(roomSize, 0.20f))
            };

            var connected = RoomShellLayout.AreDoorClearancesConnected(
                roomSize,
                roomSize,
                zones,
                barrier,
                0.24f,
                0.10f,
                out _);

            Assert.That(connected, Is.False);
        }
    }
}
