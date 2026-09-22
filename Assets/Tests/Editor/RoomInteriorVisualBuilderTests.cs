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
