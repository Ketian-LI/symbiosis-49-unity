using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteRoomLoadVisualTests
    {
        [TestCase(0, 4, 0)]
        [TestCase(2, 9, 0)]
        [TestCase(5, 12, 3)]
        [TestCase(8, 16, 8)]
        [TestCase(9, 16, 12)]
        public void EveryStateUsesTheApprovedBinAndBagCounts(
            int units,
            int expectedBins,
            int expectedBags)
        {
            var root = new GameObject("Waste Visual Test");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var visual = root.AddComponent<WasteRoomLoadVisual>();
                visual.Initialize(material, HideFlags.None);
                visual.Model.Restore(units);

                var names = visual.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.gameObject.name)
                    .ToArray();
                Assert.That(names.Count(name => name.StartsWith("Closed Bin ")), Is.EqualTo(expectedBins));
                Assert.That(names.Count(name => name.StartsWith("Tied Bag ")), Is.EqualTo(expectedBags));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(9)]
        public void EveryStateLeavesAllFourDoorRoutesConnected(int units)
        {
            var root = new GameObject("Waste Clearance Test");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var visual = root.AddComponent<WasteRoomLoadVisual>();
                visual.Initialize(material, HideFlags.None);
                visual.Model.Restore(units);

                var clearances = RoomShellLayout.CreateDoorClearances(
                    1,
                    1,
                    WorldScaleStandards.CellSizeMeters,
                    0.16f,
                    0.72f,
                    0.85f,
                    0.06f);
                var obstacles = new List<RoomObstacle2D>();
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    GetRendererBounds(renderer, root.transform, out var center, out var size);
                    obstacles.Add(new RoomObstacle2D(center, size));
                    Assert.That(
                        clearances.All(clearance => !clearance.Overlaps(center, size)),
                        Is.True,
                        $"{renderer.gameObject.name} overlaps a doorway landing at load {units}.");
                }

                var roomSize = WorldScaleStandards.CellSizeMeters - 0.16f;
                Assert.That(
                    RoomShellLayout.AreDoorClearancesConnected(
                        roomSize,
                        roomSize,
                        clearances,
                        obstacles,
                        0.24f,
                        0.10f,
                        out _),
                    Is.True,
                    $"Waste state {units} disconnects the cross route.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        private static void GetRendererBounds(
            Renderer renderer,
            Transform root,
            out Vector2 center,
            out Vector2 size)
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
                        var rendererLocal = bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                        var local = root.InverseTransformPoint(renderer.transform.TransformPoint(rendererLocal));
                        minimum = Vector2.Min(minimum, new Vector2(local.x, local.z));
                        maximum = Vector2.Max(maximum, new Vector2(local.x, local.z));
                    }
                }
            }

            center = (minimum + maximum) * 0.5f;
            size = maximum - minimum;
        }
    }
}
