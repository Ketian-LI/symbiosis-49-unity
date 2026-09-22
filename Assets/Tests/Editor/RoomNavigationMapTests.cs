using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomNavigationMapTests
    {
        [Test]
        public void InitialLayoutBuildsOneNavigationNodePerRoom()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            Assert.That(navigation.RoomCount, Is.EqualTo(35));
        }

        [Test]
        public void AdjacencyIsRecomputedFromModuleFootprints()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            Assert.That(navigation.NeighboursOf("garage-a"), Does.Contain("pigeon-a"));
            Assert.That(navigation.NeighboursOf("garage-a"), Does.Contain("residence-a"));
        }

        [Test]
        public void PointOnOuterWallMovesToNearestSafeFloorInterior()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            var wallPoint = new Vector2(-10.85f, 9.1f);
            var corrected = navigation.FindNearestLegalFloorPoint(wallPoint, 0.25f);
            Assert.That(corrected.x, Is.GreaterThan(wallPoint.x));
        }

        [Test]
        public void InteriorPointRemainsUnchanged()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            var interior = new Vector2(-8f, 9.1f);
            var corrected = navigation.FindNearestLegalFloorPoint(interior, 0.25f);
            Assert.That((corrected - interior).sqrMagnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FindsNearestReachableCandidateByRoomHops()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);

            Assert.That(
                navigation.TryFindNearestReachable(
                    "residence-a",
                    new[] { "trash-a", "trash-d" },
                    out var roomId,
                    out var distance),
                Is.True);
            Assert.That(roomId, Is.EqualTo("trash-a"));
            Assert.That(distance, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void MissingStartRoomHasNoReachableCandidate()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);

            Assert.That(
                navigation.TryFindNearestReachable(
                    "missing-room",
                    new[] { "trash-a" },
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void FindsRouteAndDoorPointBetweenAdjacentRooms()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);

            Assert.That(
                navigation.TryFindRoute("garage-a", "residence-a", out var route),
                Is.True);
            Assert.That(route.First(), Is.EqualTo("garage-a"));
            Assert.That(route.Last(), Is.EqualTo("residence-a"));
            Assert.That(
                navigation.TryGetConnectionPoint(route[0], route[1], out var connection),
                Is.True);
            Assert.That(float.IsNaN(connection.x) || float.IsNaN(connection.y), Is.False);
        }

        [Test]
        public void FindsRoomContainingInteriorBoardPoint()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);

            Assert.That(
                navigation.TryFindRoomContaining(new Vector2(-8f, 9.1f), out var roomId),
                Is.True);
            Assert.That(roomId, Is.Not.Empty);
        }

        [Test]
        public void ExcludedGarageIsNotUsedAsIntermediateRoom()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            var excluded = new System.Collections.Generic.HashSet<string>
            {
                "garage-a", "garage-b", "garage-c"
            };

            Assert.That(
                navigation.TryFindRoute("residence-a", "pigeon-a", excluded, out var route),
                Is.True);
            Assert.That(route.Any(excluded.Contains), Is.False);
        }
    }
}
