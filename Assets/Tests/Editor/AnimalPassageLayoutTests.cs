using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalPassageLayoutTests
    {
        [Test]
        public void FacingAnimalPortsConnectWithoutCreatingAPedestrianRoute()
        {
            var rooms = new[]
            {
                Spec("shared-h", 0, 0, 1, 1),
                Spec("shared-i", 1, 0, 1, 1)
            };
            var placements = rooms.Select(room => Placement(room)).ToArray();
            var people = new RoomNavigationMap(placements, rooms, 2f, humanRoadsOnly: true);
            var animals = new RoomNavigationMap(placements, rooms, 2f, true);

            Assert.That(people.ConnectionCount, Is.Zero);
            Assert.That(animals.ConnectionCount, Is.EqualTo(1));
            Assert.That(animals.TryGetAnimalConnectionPort("shared-h", "shared-i",
                out var firstPort), Is.True);
            Assert.That(firstPort.Edge, Is.EqualTo(AnimalPassageEdge.East));
            Assert.That(animals.TryGetAnimalConnectionPort("shared-i", "shared-h",
                out var secondPort), Is.True);
            Assert.That(secondPort.Edge, Is.EqualTo(AnimalPassageEdge.West));

            placements[1].quarterTurns = 1; // the second room no longer offers a west animal port
            people = new RoomNavigationMap(placements, rooms, 2f, humanRoadsOnly: true);
            animals = new RoomNavigationMap(placements, rooms, 2f, true);
            Assert.That(people.ConnectionCount, Is.Zero);
            Assert.That(animals.ConnectionCount, Is.Zero);
            Assert.That(animals.TryFindRoute("shared-h", "shared-i", out _), Is.False);
            Assert.That(animals.TryGetConnectionPoint("shared-h", "shared-i", out _), Is.False);
            Assert.That(animals.TryGetAnimalConnectionPort("shared-h", "shared-i", out _), Is.False);
        }

        [Test]
        public void HungryWildlifeMayBorrowPedestrianDoorsButNotEventClosedDoors()
        {
            var pedestrianRooms = new[]
            {
                Spec("shared-d", 0, 0, 1, 1),
                Spec("office-a", 1, 0, 1, 1)
            };
            var pedestrianPlacements = pedestrianRooms.Select(Placement).ToArray();
            var normal = new RoomNavigationMap(pedestrianPlacements,
                pedestrianRooms, 2f, animalPassagesOnly: true);
            var hungry = new RoomNavigationMap(pedestrianPlacements,
                pedestrianRooms, 2f, animalPassagesOnly: true,
                hungryWildlifeMayUsePedestrianDoors: true);
            Assert.That(normal.ConnectionCount, Is.Zero);
            Assert.That(hungry.ConnectionCount, Is.EqualTo(1));

            var closedRooms = new[]
            {
                Spec("shared-h", 0, 0, 1, 1),
                Spec("office-a", 1, 0, 1, 1)
            };
            var closedPlacements = closedRooms.Select(Placement).ToArray();
            var closed = new RoomNavigationMap(closedPlacements,
                closedRooms, 2f, animalPassagesOnly: true,
                animalDayNumber: 5, hungryWildlifeMayUsePedestrianDoors: true);
            Assert.That(closed.ConnectionCount, Is.Zero,
                "Day 5 maintenance closes every exit of shared-h, even when hungry.");
        }

        [Test]
        public void MultiCellPortMatchesExactDoorSegment()
        {
            var rooms = new[]
            {
                Spec("central-park", 0, 0, 2, 2),
                Spec("pigeon-b", 2, 1, 1, 1)
            };
            var map = new RoomNavigationMap(rooms.Select(Placement), rooms, 2f, true);

            Assert.That(map.TryGetConnectionPoint("central-park", "pigeon-b", out var point), Is.True);
            Assert.That(point.x, Is.EqualTo(-3f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void RotatedPortSetMatchesRotatedShellDoorways()
        {
            var room = Spec("shared-i", 0, 0, 1, 1);
            var ports = AnimalPassageLayout.Ports(room, 1);
            var shell = RoomShellLayout.CreateDoorways(1, 1, 2f, 0.16f);

            Assert.That(ports.Count, Is.EqualTo(2));
            Assert.That(ports.All(port => shell.Any(slot =>
                (int)slot.Edge == (int)port.Edge && slot.SegmentIndex == port.Segment)), Is.True);
            Assert.That(AnimalPassageLayout.CanRotate(room), Is.True);
            Assert.That(ports.Any(port => port.Edge == AnimalPassageEdge.North), Is.True);
            Assert.That(ports.Any(port => port.Edge == AnimalPassageEdge.East), Is.False);
        }

        [Test]
        public void FoxKeepsAnAlternativeFoodRouteWhenPlazaMoves()
        {
            var placements = RoomLayoutData.All.Select(Placement).ToArray();
            var plaza = placements.Single(item => item.id == "pigeon-d");
            var house = placements.Single(item => item.id == "residence-d");
            (plaza.column, house.column) = (house.column, plaza.column);
            (plaza.row, house.row) = (house.row, plaza.row);
            var map = new RoomNavigationMap(placements, RoomLayoutData.All, 2f, true);

            Assert.That(map.TryFindRoute("shared-j", "canteen-a", out var route), Is.True);
            Assert.That(route.Count - 1, Is.EqualTo(1),
                "The fox's edge refuge retains its dedicated food-shop exit.");
        }

        [Test]
        public void DirectionalOneCellRoomCanRotateWithoutChangingItsFootprint()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var room = layout.Get("trash-b");

            Assert.That(room.CanRotate, Is.True);
            Assert.That(layout.TryRotate("trash-b"), Is.True);
            Assert.That(room.Width, Is.EqualTo(1));
            Assert.That(room.Height, Is.EqualTo(1));
            Assert.That(AnimalPassageLayout.Ports(RoomLayoutData.All.Single(item => item.Id == "trash-b"),
                room.QuarterTurns).Single().Edge, Is.EqualTo(AnimalPassageEdge.East));
        }

        [Test]
        public void FloorVisualMarksExactlyTheAuthoredPorts()
        {
            var room = Spec("shared-i", 0, 0, 1, 1);
            var root = new GameObject("Passage Test Root");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                AnimalPassageVisual.Build(root.transform, room, 2f, 0.16f,
                    material, HideFlags.None);
                var labels = root.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                Assert.That(root.transform.Find("Animal Passage Overlay").gameObject.activeSelf,
                    Is.False, "Animal routes must stay hidden outside layout planning.");
                Assert.That(labels.Count(name => name.StartsWith("Animal Passage Port ")), Is.EqualTo(2));
                Assert.That(labels.Any(name => name == "Animal Passage Port East 0"), Is.True);
                Assert.That(labels.Any(name => name == "Animal Passage Port West 0"), Is.True);
                Assert.That(labels.Any(name => name == "Animal Passage Port North 0"), Is.False);
                foreach (var stroke in root.GetComponentsInChildren<Transform>(true)
                             .Where(item => item.name.StartsWith("Animal Passage ") &&
                                            !item.name.Contains("Port") &&
                                            item.name != "Animal Passage Overlay"))
                    Assert.That(stroke.localScale.z, Is.LessThan(0.3f),
                        "Animal markings stay at the doorway rather than crossing a human trail.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void DisturbedPortIsAmberOnlyInThePlanningOverlay()
        {
            var room = RoomLayoutData.All.Single(item => item.Id == "central-park");
            var root = new GameObject("Park Passage Test");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                AnimalPassageVisual.Build(root.transform, room, 2f, 0.16f,
                    material, HideFlags.None);
                var overlay = root.GetComponentInChildren<AnimalPassageOverlay>(true);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);

                overlay.SetConnectedPorts(AnimalPassageLayout.Ports(room, 0));
                overlay.SetPreview(true, 0, 2);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                var blocked = overlay.GetComponentsInChildren<Renderer>()
                    .Single(item => item.name == "Animal Passage Port West 0");
                var open = overlay.GetComponentsInChildren<Renderer>()
                    .Single(item => item.name == "Animal Passage Port East 0");
                var block = new MaterialPropertyBlock();
                blocked.GetPropertyBlock(block);
                Assert.That(block.GetColor("_Color").r, Is.GreaterThan(0.9f));
                open.GetPropertyBlock(block);
                Assert.That(block.GetColor("_Color").g, Is.GreaterThan(0.8f));
                Assert.That(blocked.enabled, Is.True);
                Assert.That(open.enabled, Is.False, "Open crossings are drawn as lines, not dots.");
                overlay.SetPreview(false, 0, 2);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PlanningOverlayDrawsOnlyConnectedAnimalLines()
        {
            var room = Spec("shared-i", 0, 0, 1, 1);
            var root = new GameObject("Connected Passage Test");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                AnimalPassageVisual.Build(root.transform, room, 2f, 0.16f,
                    material, HideFlags.None);
                var overlay = root.GetComponentInChildren<AnimalPassageOverlay>(true);
                overlay.SetConnectedPorts(new[] { new AnimalPassagePort(AnimalPassageEdge.East, 0) });
                overlay.SetPreview(true, 0, 1);

                var renderers = overlay.GetComponentsInChildren<Renderer>();
                Assert.That(renderers.Single(item => item.name == "Animal Passage East 0").enabled,
                    Is.True);
                Assert.That(renderers.Single(item => item.name == "Animal Passage West 0").enabled,
                    Is.False);
                Assert.That(renderers.Where(item => item.name.StartsWith("Animal Passage Port "))
                    .All(item => !item.enabled), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SelectedRoomShowsOnlyItsUnconnectedPortMarkers()
        {
            var room = Spec("shared-i", 0, 0, 1, 1);
            var root = new GameObject("Selected Passage Test");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                AnimalPassageVisual.Build(root.transform, room, 2f, 0.16f,
                    material, HideFlags.None);
                var overlay = root.GetComponentInChildren<AnimalPassageOverlay>(true);
                overlay.SetConnectedPorts(new[] { new AnimalPassagePort(AnimalPassageEdge.East, 0) });
                overlay.SetPreview(true, 0, 1);
                var renderers = overlay.GetComponentsInChildren<Renderer>();
                var west = renderers.Single(item => item.name == "Animal Passage Port West 0");
                var east = renderers.Single(item => item.name == "Animal Passage Port East 0");
                Assert.That(west.enabled, Is.False);

                overlay.SetShowUnconnectedPorts(true);
                Assert.That(west.enabled, Is.True);
                Assert.That(east.enabled, Is.False, "Connected exits remain continuous lines.");
                Assert.That(renderers.Single(item => item.name == "Animal Passage West 0").enabled,
                    Is.False, "An unmatched exit is a short marker, not a misleading full route.");

                overlay.SetShowUnconnectedPorts(false);
                Assert.That(west.enabled, Is.False);
                overlay.SetPreview(false, 0, 1);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }
        }

        private static RoomSpec Spec(string id, int column, int row, int width, int height) =>
            new(id, id, RoomType.Residence, column, row, width, height, true, string.Empty);

        private static RoomPlacementData Placement(RoomSpec room) => new()
        {
            id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
        };
    }
}
