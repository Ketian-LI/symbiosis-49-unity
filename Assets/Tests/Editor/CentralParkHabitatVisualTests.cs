using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class CentralParkHabitatVisualTests
    {
        [Test]
        public void FixedParkAnchorsDistributedGreenNichesAndTheCorrectRestingEdge()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var park = generated.GetComponentsInChildren<RoomView>(true)
                    .Single(view => view.Spec.Id == "central-park");
                var descendants = park.VisualRoot.GetComponentsInChildren<Transform>(true)
                    .GroupBy(item => item.name).ToDictionary(group => group.Key, group => group.First());
                foreach (var name in new[] { "Pigeon Resting Perch" })
                    Assert.That(descendants.ContainsKey(name), Is.True, name);
                var rooms = generated.GetComponentsInChildren<RoomView>(true);
                foreach (var id in new[] { "central-park", "shared-e", "shared-f", "shared-h",
                             "shared-i", "shared-j", "shrub-b", "oak-c" })
                {
                    var view = rooms.Single(item => item.Spec.Id == id);
                    var actual = view.VisualRoot.GetComponentsInChildren<Transform>(true)
                        .Where(item => item.name.StartsWith("Authored Human Route "))
                        .Select(item => item.name).ToArray();
                    var expected = HumanRoadLayout.Ports(view.Spec, 0)
                        .Select(port => $"Authored Human Route {port.Edge} {port.Segment}");
                    Assert.That(actual, Is.EquivalentTo(expected), $"{id} ground path must match pedestrian ports.");
                    var slots = RoomShellLayout.CreateDoorways(view.Spec.Width, view.Spec.Height,
                        WorldScaleStandards.CellSizeMeters, 0.16f);
                    foreach (var port in HumanRoadLayout.Ports(view.Spec, 0))
                    {
                        var route = view.VisualRoot.GetComponentsInChildren<Transform>(true)
                            .Single(item => item.name == $"Authored Human Route {port.Edge} {port.Segment}");
                        var surface = route.Find("Trail Surface")?.GetComponent<MeshFilter>()?.sharedMesh;
                        Assert.That(surface, Is.Not.Null, $"{id} {port.Edge} should have a trail mesh.");
                        Assert.That(route.GetComponentsInChildren<Collider>(true), Is.Empty,
                            "Decorative ground trails must not obstruct animal or resident movement.");
                        var vertices = surface.vertices;
                        var tip = (vertices[vertices.Length - 2] + vertices[vertices.Length - 1]) * 0.5f;
                        var doorway = slots.Single(slot => (int)slot.Edge == (int)port.Edge &&
                                                           slot.SegmentIndex == port.Segment);
                        Assert.That(Vector2.Distance(new Vector2(tip.x, tip.z),
                                new Vector2(doorway.LocalCenter.x, doorway.LocalCenter.z)),
                            Is.LessThan(0.02f), $"{id} trail must end at its actual pedestrian port.");
                    }
                }
                Assert.That(rooms.Single(view => view.Spec.Id == "shared-f").VisualRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Squirrel Grove Cache"), Is.True);
                Assert.That(rooms.Single(view => view.Spec.Id == "shared-h").VisualRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Hedgehog Garden Shelter"), Is.True);
                Assert.That(rooms.Single(view => view.Spec.Id == "shared-j").VisualRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Fox Edge Nook"), Is.True);

                var rest = park.VisualRoot.GetComponentInChildren<ParkEdgeRestVisual>(true);
                Assert.That(rest, Is.Not.Null);
                var west = descendants["Park Rest Warning West"].gameObject;
                var east = descendants["Park Rest Warning East"].gameObject;
                var south = descendants["Park Rest Warning South"].gameObject;
                var north = descendants["Park Rest Warning North"].gameObject;
                rest.RefreshForDay(1);
                Assert.That(new[] { west, east, south, north }.Any(item => item.activeSelf), Is.False);
                rest.RefreshForDay(4);
                Assert.That(west.activeSelf, Is.True);
                Assert.That(new[] { east, south, north }.Any(item => item.activeSelf), Is.False);
                rest.RefreshForDay(6);
                Assert.That(east.activeSelf, Is.True);
                rest.RefreshForDay(8);
                Assert.That(south.activeSelf, Is.True);
                rest.RefreshForDay(10);
                Assert.That(north.activeSelf, Is.True);

                bool InRoom(Component animal, string roomId)
                {
                    var center = rooms.Single(view => view.Spec.Id == roomId).VisualRoot.position;
                    return Mathf.Abs(animal.transform.position.x - center.x) < 1.5f &&
                           Mathf.Abs(animal.transform.position.z - center.z) < 1.5f;
                }
                Assert.That(generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                    .Count(animal => InRoom(animal, "central-park")), Is.EqualTo(2));
                Assert.That(generated.GetComponentsInChildren<SquirrelDemoAgent>(true)
                    .Count(animal => InRoom(animal, "shared-f")), Is.EqualTo(1));
                Assert.That(generated.GetComponentsInChildren<HedgehogDemoAgent>(true)
                    .Count(animal => InRoom(animal, "shared-h")), Is.EqualTo(1));
                Assert.That(generated.GetComponentsInChildren<FoxDemoAgent>(true)
                    .Count(animal => InRoom(animal, "shared-j")), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
