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

                    var obstacleRenderers = root.GetComponentsInChildren<Renderer>()
                        .Where(item => !RoomInteriorVisualBuilder.IsPassThroughFurnishing(item))
                        .ToArray();
                    for (var index = 0; index < obstacles.Count; index++)
                    {
                        var obstacle = obstacles[index];
                        Assert.That(
                            clearances.Any(clearance => clearance.Overlaps(obstacle.LocalCenter, obstacle.Size)),
                            Is.False,
                            $"{spec.Id} has {obstacleRenderers[index].name} inside a doorway landing.");
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
        public void ResidenceBedIsLargerAndRemainsPassableAtDoorway()
        {
            var spec = RoomLayoutData.All.First(item => item.Id == "residence-c");
            var room = new GameObject("Larger residence bed");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                    spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                    material, HideFlags.None);
                var frame = root.GetComponentsInChildren<Renderer>()
                    .Single(item => item.name == "Bed Frame");
                var wardrobe = root.GetComponentsInChildren<Renderer>()
                    .Single(item => item.name == "Wardrobe");
                var bounds = frame.bounds;
                var doorClearances = RoomShellLayout.CreateDoorClearances(
                    spec.Width, spec.Height, CellSize, RoomGap, 0.72f, 0.85f, 0.06f);

                Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(1.0f));
                Assert.That(bounds.size.z, Is.GreaterThanOrEqualTo(1.45f));
                Assert.That(RoomInteriorVisualBuilder.IsPassThroughFurnishing(frame), Is.True);
                Assert.That(RoomInteriorVisualBuilder.IsPassThroughFurnishing(wardrobe), Is.False);
                Assert.That(frame.GetComponentInParent<RoomPassThroughVisual>()
                    .GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(doorClearances.Any(clearance => clearance.Overlaps(
                    new Vector2(bounds.center.x, bounds.center.z),
                    new Vector2(bounds.size.x, bounds.size.z))), Is.True,
                    "The larger bed may overlap a doorway visually, but must not become a movement obstacle.");
            }
            finally
            {
                Object.DestroyImmediate(room);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void OfficesHaveTwoReadableWorkstationsAndASeparatePrinterCorner()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.Office))
                {
                    var room = new GameObject($"Office Details {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var renderers = root.GetComponentsInChildren<Renderer>();
                        foreach (var name in new[] { "Workstation A", "Workstation B" })
                        {
                            var desk = renderers.Single(item => item.name == $"{name} Desk Top");
                            var screen = renderers.Single(item => item.name == $"{name} Screen");
                            Assert.That(desk.transform.localScale.x, Is.GreaterThanOrEqualTo(0.90f), spec.Id);
                            Assert.That(screen.transform.localScale.z, Is.GreaterThanOrEqualTo(0.15f), spec.Id);
                            Assert.That(renderers.Count(item => item.name.StartsWith($"{name} Key ")),
                                Is.EqualTo(12), spec.Id);
                            Assert.That(renderers.Any(item => item.name == $"{name} Drawer Pedestal"),
                                Is.True, spec.Id);
                        }
                        Assert.That(renderers.Any(item => item.name == "Printer Output Tray"), Is.True, spec.Id);
                        Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty, spec.Id);
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
        public void EverydayRoomsContainTheApprovedFunctionalObjects()
        {
            var requiredNames = new Dictionary<RoomType, string[]>
            {
                [RoomType.Residence] = new[] { "Bed Frame", "Bed Footboard", "Bed Side Rail -1", "Duvet Foot Stitch", "Bedside Cabinet", "Wardrobe", "Wardrobe Wood Top Rim", "One Person Round Table", "Round Table Book", "Single Chair Seat", "Residence Plant Pot" },
                [RoomType.Office] = new[] { "Workstation A Desk Top", "Workstation B Desk Top", "Workstation A Chair Back", "Workstation B Chair Back", "Shared Filing Cabinet", "Compact Printer", "Waste Paper Basket", "Office Plant Pot" },
                [RoomType.Canteen] = new[] { "Compact Kitchen Counter", "Compact Cooker", "Compact Sink", "Compact Dining Table", "Compact Dining Chair", "Compact Plate" },
                [RoomType.Supermarket] = new[] { "Compact Gondola A", "Compact Gondola B", "Compact Produce Crate", "Compact Checkout Counter", "Compact Checkout Register" }
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
        public void SingleCellFoodShopsUseACompactCounterAndTable()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.Canteen))
                {
                    var room = new GameObject($"Dining Check {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var renderers = root.GetComponentsInChildren<Renderer>();
                        Assert.That(renderers.Count(item => item.name.EndsWith("Rug")), Is.Zero,
                            "The shared dining rug belongs to the walkable floor shell, not the obstacle kit.");
                        Assert.That(renderers.Count(item => item.name == "Compact Dining Table"), Is.EqualTo(1), spec.Id);
                        Assert.That(renderers.Count(item => item.name == "Compact Kitchen Counter"), Is.EqualTo(1), spec.Id);
                        Assert.That(renderers.Count(item => item.name.StartsWith("Dining Table ")), Is.Zero, spec.Id);
                        var table = root.InverseTransformPoint(
                            renderers.Single(item => item.name == "Compact Dining Table").transform.position);
                        Assert.That(Mathf.Abs(table.x), Is.LessThan(1f), spec.Id);
                        Assert.That(Mathf.Abs(table.z), Is.LessThan(1f), spec.Id);
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
        public void SupermarketHasExactlyTwoOpenStockedGondolas()
        {
            var spec = RoomLayoutData.All.Single(item => item.Type == RoomType.Supermarket);
            var room = new GameObject("Supermarket Kit Check");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                    spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                    material, HideFlags.None);
                var renderers = root.GetComponentsInChildren<Renderer>();
                foreach (var name in new[] { "Compact Gondola A", "Compact Gondola B" })
                {
                    var back = renderers.Single(item => item.name == name);
                    Assert.That(back.transform.localScale.z, Is.LessThan(0.10f), name);
                    Assert.That(renderers.Count(item => item.name.StartsWith($"{name} Product ")),
                        Is.EqualTo(15), name);
                }
                Assert.That(renderers.Count(item => item.name is "Compact Gondola A" or "Compact Gondola B"),
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(room);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SingleCellGarageCentresTheSharedPedestrianAndTrafficPortal()
        {
            foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.Garage))
            {
                var laneX = GarageVisualLayout.LaneCenterX(spec.Width);
                var doorways = RoomShellLayout.CreateDoorways(spec.Width, spec.Height, CellSize, RoomGap);
                foreach (var doorway in doorways.Where(item => item.Edge is RoomEdge.North or RoomEdge.South))
                {
                    Assert.That(laneX, Is.EqualTo(doorway.LocalCenter.x).Within(0.001f),
                        $"{spec.Id} has one shared single-cell entrance, not a separate bay.");
                }
            }
        }

        [Test]
        public void GaragePavingFramesTheRoadWithoutAddingClickableColliders()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.Garage))
                {
                    var room = new GameObject($"Paving Check {spec.Id}");
                    try
                    {
                        var width = spec.Width * CellSize - RoomGap;
                        var depth = spec.Height * CellSize - RoomGap;
                        GarageVisualLayout.BuildShellDetails(room.transform, spec,
                            width, depth, material, HideFlags.None);
                        var renderers = room.GetComponentsInChildren<Renderer>();
                        Assert.That(renderers.Any(item => item.name == "Single Vehicle Lane"), Is.True, spec.Id);
                        Assert.That(renderers.Count(item => item.name.Contains("Paving Brick")),
                            Is.GreaterThanOrEqualTo(24), spec.Id);
                        Assert.That(room.GetComponentsInChildren<Collider>(), Is.Empty, spec.Id);
                        foreach (var brick in renderers.Where(item => item.name.Contains("Paving Brick")))
                        {
                            var local = room.transform.InverseTransformPoint(brick.transform.position);
                            Assert.That(Mathf.Abs(local.x - GarageVisualLayout.LaneCenterX(spec.Width)),
                                Is.GreaterThan(0.47f), brick.name);
                            Assert.That(Mathf.Abs(local.z), Is.LessThan(depth * 0.5f), brick.name);
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
                            Assert.That(names.Count(name => name.StartsWith("Culvert Ring Stone")), Is.EqualTo(10), spec.Id);
                            Assert.That(names.Count(name => name.Contains("Wall Top Brick")), Is.EqualTo(8), spec.Id);
                            Assert.That(names, Does.Contain("Southwest Resting Hollow"), spec.Id);
                            var recess = root.GetComponentsInChildren<Renderer>()
                                .Single(item => item.name == "Sheltered Side Recess");
                            Assert.That(recess.transform.localPosition.x, Is.GreaterThan(0f),
                                "The smaller side recess belongs on the right-hand wall.");
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

        [Test]
        public void EachPigeonHabitatUsesOpenPlazaFurnitureWithoutRooftopOrNestBoxes()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.PigeonHabitat))
                {
                    var room = new GameObject($"Pigeon Kit {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var names = root.GetComponentsInChildren<Renderer>().Select(item => item.name).ToArray();
                        Assert.That(names.Count(name => name == "Plaza Planter Base"), Is.EqualTo(1), spec.Id);
                        Assert.That(names.Count(name => name == "Plaza Stone Bench Seat"), Is.EqualTo(1), spec.Id);
                        Assert.That(names.Count(name => name == "Plaza Bird Bath Water"), Is.EqualTo(1), spec.Id);
                        Assert.That(names.Count(name => name == "Plaza Feeding Stone"), Is.EqualTo(1), spec.Id);
                        Assert.That(names.Count(name => name.StartsWith("Plaza Scattered Seed")), Is.EqualTo(4), spec.Id);
                        Assert.That(names.Any(name => name.Contains("Ventilation") || name.Contains("Eave") ||
                            name.Contains("Nest Box") || name.Contains("Roof Nest") || name.Contains("Perch Rail")),
                            Is.False, spec.Id);
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
        public void ShrubVariantsKeepEqualCoverAndDistinctLayouts()
        {
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            var islandPositions = new List<Vector3>();
            try
            {
                foreach (var spec in RoomLayoutData.All.Where(item => item.Type == RoomType.ShrubHabitat))
                {
                    var room = new GameObject($"Shrub Kit {spec.Id}");
                    try
                    {
                        var root = RoomInteriorVisualBuilder.Build(room.transform, spec,
                            spec.Width * CellSize - RoomGap, spec.Height * CellSize - RoomGap,
                            material, HideFlags.None);
                        var renderers = root.GetComponentsInChildren<Renderer>();
                        Assert.That(renderers.Count(item => item.name.Contains("Crown")), Is.EqualTo(8), spec.Id);
                        Assert.That(renderers.Count(item => item.name.Contains("Ground Foliage")), Is.EqualTo(12), spec.Id);
                        Assert.That(renderers.Count(item => item.name.Contains("Low Tuft")), Is.EqualTo(6), spec.Id);
                        Assert.That(renderers.Count(item => item.name.Contains("Petal")), Is.EqualTo(10), spec.Id);
                        foreach (var crown in renderers.Where(item => item.name.Contains("Crown")))
                        {
                            Assert.That(crown.GetComponent<MeshFilter>().sharedMesh.name,
                                Does.Contain("Flat-shaded Crown"), spec.Id);
                        }
                        Assert.That(renderers.Any(item => item.name == "Dry Leaf Resting Patch"), Is.True, spec.Id);
                        Assert.That(renderers.Any(item => item.name == "Leaf Litter Insect Point"), Is.True, spec.Id);
                        var first = renderers.Single(item => item.name == "Cover Island A Crown 1");
                        var second = renderers.Single(item => item.name == "Cover Island B Crown 1");
                        islandPositions.Add(first.transform.localPosition + second.transform.localPosition * 0.1f);
                    }
                    finally
                    {
                        Object.DestroyImmediate(room);
                    }
                }
                Assert.That(islandPositions.Distinct().Count(), Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void WildlifeFloorColorsMatchTheExtractedReferencePalette()
        {
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.PigeonHabitat)), Is.EqualTo("9B989E"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.ShrubHabitat)), Is.EqualTo("8F934C"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.FoxDen)), Is.EqualTo("B39562"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.PigeonWater), Is.EqualTo("548FB9"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ShrubLeaf), Is.EqualTo("637239"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.FoxBrick), Is.EqualTo("835440"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.Doorframe), Is.EqualTo("C8AB82"));
        }

        [Test]
        public void AssembledBoardColorsUseTheMutedReferencePalette()
        {
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.Boundary), Is.EqualTo("263653"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.Wall), Is.EqualTo("24324C"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.CentralPark)), Is.EqualTo("718B49"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.Office)), Is.EqualTo("587D81"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.Canteen)), Is.EqualTo("A4764D"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.Supermarket)), Is.EqualTo("D6AC55"));
            Assert.That(ColorUtility.ToHtmlStringRGB(UrbanPalette.ForRoom(RoomType.OakHabitat)), Is.EqualTo("6A7C45"));
        }

        private static IReadOnlyList<RoomObstacle2D> BuildObstacles(Transform root)
        {
            var obstacles = new List<RoomObstacle2D>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (RoomInteriorVisualBuilder.IsPassThroughFurnishing(renderer))
                {
                    continue;
                }

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
