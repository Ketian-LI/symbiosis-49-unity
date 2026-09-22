using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomInteriorVisualBuilderTests
    {
        private const float CellSize = 3.1f;
        private const float RoomGap = 0.16f;

        [Test]
        public void EverySupportedRoomBuildsRecognisableMultiPartFurniture()
        {
            foreach (var spec in RoomLayoutData.All.Where(item => RoomInteriorVisualBuilder.Supports(item.Type)))
            {
                var room = new GameObject($"Test {spec.Id}");
                var material = UrbanVisualFactory.CreateSurfaceMaterial();
                try
                {
                    var root = RoomInteriorVisualBuilder.Build(
                        room.transform,
                        spec,
                        spec.Width * CellSize - RoomGap,
                        spec.Height * CellSize - RoomGap,
                        material,
                        HideFlags.None);

                    Assert.That(root, Is.Not.Null, spec.Id);
                    Assert.That(root.GetComponentsInChildren<Renderer>(), Has.Length.GreaterThanOrEqualTo(4), spec.Id);
                    Assert.That(
                        root.GetComponentsInChildren<Renderer>().Any(item => item.name.Contains("Marker")),
                        Is.False,
                        spec.Id);
                }
                finally
                {
                    Object.DestroyImmediate(room);
                    Object.DestroyImmediate(material);
                }
            }
        }

        [Test]
        public void FurnishingsKeepEveryDoorLandingAndInternalRouteClear()
        {
            foreach (var spec in RoomLayoutData.All.Where(item => RoomInteriorVisualBuilder.Supports(item.Type)))
            {
                var room = new GameObject($"Clearance {spec.Id}");
                var material = UrbanVisualFactory.CreateSurfaceMaterial();
                try
                {
                    var width = spec.Width * CellSize - RoomGap;
                    var depth = spec.Height * CellSize - RoomGap;
                    var root = RoomInteriorVisualBuilder.Build(
                        room.transform,
                        spec,
                        width,
                        depth,
                        material,
                        HideFlags.None);
                    var clearances = RoomShellLayout.CreateDoorClearances(
                        spec.Width,
                        spec.Height,
                        CellSize,
                        RoomGap,
                        0.72f,
                        0.85f,
                        0.06f);
                    var obstacles = BuildObstacles(root);

                    foreach (var obstacle in obstacles)
                    {
                        Assert.That(
                            clearances.Any(clearance => clearance.Overlaps(obstacle.LocalCenter, obstacle.Size)),
                            Is.False,
                            $"{spec.Id} has furniture inside a doorway landing.");
                    }

                    Assert.That(
                        RoomShellLayout.AreDoorClearancesConnected(
                            width,
                            depth,
                            clearances,
                            obstacles,
                            0.24f,
                            0.10f,
                            out var unreachable),
                        Is.True,
                        $"{spec.Id} disconnects {unreachable.Edge} doorway {unreachable.SegmentIndex + 1}.");
                }
                finally
                {
                    Object.DestroyImmediate(room);
                    Object.DestroyImmediate(material);
                }
            }
        }

        [Test]
        public void EverydayRoomsContainTheApprovedFunctionalObjects()
        {
            var requiredNames = new Dictionary<RoomType, string[]>
            {
                [RoomType.Residence] = new[] { "Bed Frame", "Bedside Cabinet", "Wardrobe", "One Person Round Table", "Single Chair Seat", "Residence Plant Pot" },
                [RoomType.Office] = new[] { "Workstation A Desk Top", "Workstation B Desk Top", "Shared Filing Cabinet", "Compact Printer", "Waste Paper Basket", "Office Plant Pot" },
                [RoomType.Canteen] = new[] { "Kitchen Counter", "Low Kitchen Divider", "Under-counter Refrigerator", "Host Order Station", "Dining Table A", "Dining Table B", "Wall Booth Seat", "Closed Food Waste Bin" },
                [RoomType.Supermarket] = new[] { "Central Gondola A", "Central Gondola B", "Wall Chilled Case", "Produce Crate", "Checkout Counter", "Card Terminal", "Shopping Basket Stack", "Closed Waste Container" }
            };
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => requiredNames.ContainsKey(item.Type)))
                {
                    var room = new GameObject($"Objects {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var names = new HashSet<string>(root.GetComponentsInChildren<Renderer>().Select(item => item.name));
                        foreach (var required in requiredNames[spec.Type])
                        {
                            Assert.That(names.Contains(required), Is.True, $"{spec.Id} is missing {required}.");
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(room);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GarageLaneLeavesPedestrianDoorsSeparateFromTrafficPortals()
        {
            foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.Garage))
            {
                var laneX = GarageVisualLayout.LaneCenterX(spec.Width);
                var doorways = RoomShellLayout.CreateDoorways(spec.Width, spec.Height, CellSize, RoomGap);
                foreach (var doorway in doorways.Where(item => item.Edge is RoomEdge.North or RoomEdge.South))
                {
                    var separation = Mathf.Abs(laneX - doorway.LocalCenter.x);
                    Assert.That(separation, Is.GreaterThan(0.40f + 0.36f),
                        $"{spec.Id} vehicle portal overlaps a pedestrian door.");
                }
            }
        }

        [Test]
        public void GarageAndFoxDenUseApprovedRoomObjects()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type is RoomType.Garage or RoomType.FoxDen))
                {
                    var room = new GameObject($"Object Check {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var names = root.GetComponentsInChildren<Renderer>().Select(item => item.name).ToArray();
                        if (spec.Type == RoomType.Garage)
                        {
                            Assert.That(names.Any(name => name.Contains("Tool") || name.Contains("Tyre")), Is.False, spec.Id);
                            Assert.That(names.Any(name => name.Contains("Lane Edge")), Is.True, spec.Id);
                        }
                        else
                        {
                            Assert.That(names.Any(name => name.Contains("Drainage Culvert")), Is.True, spec.Id);
                            Assert.That(names.Any(name => name.Contains("Retaining Wall")), Is.True, spec.Id);
                            Assert.That(names.Any(name => name.Contains("Earth Mound")), Is.False, spec.Id);
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(room);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static IReadOnlyList<RoomObstacle2D> BuildObstacles(Transform root)
        {
            var obstacles = new List<RoomObstacle2D>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.localBounds;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var local = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                            var roomLocal = root.InverseTransformPoint(renderer.transform.TransformPoint(local));
                            minimum = Vector2.Min(minimum, new Vector2(roomLocal.x, roomLocal.z));
                            maximum = Vector2.Max(maximum, new Vector2(roomLocal.x, roomLocal.z));
                        }
                    }
                }

                obstacles.Add(new RoomObstacle2D((minimum + maximum) * 0.5f, maximum - minimum));
            }

            return obstacles;
        }
    }
}
