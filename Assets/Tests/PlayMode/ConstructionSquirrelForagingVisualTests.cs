using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionSquirrelForagingVisualTests
    {
        private static readonly BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator SecondSquirrelEatsWithoutAnimatingHungryFirstSquirrel()
        {
            var run = RunWithSecondOak(7);
            var secondOak = run.At(4, 3);
            var (root, view, first, second) = CreateView(secondOak.id);
            try
            {
                // The meal list is compact: the first squirrel found no food.
                view.PlayForaging(SecondOakMeal(secondOak), null, run);
                var traveling = Traveling(view);
                Assert.That(traveling.Contains(second.transform), Is.True,
                    "The meal should animate the squirrel living at its route origin.");
                Assert.That(traveling.Contains(first.transform), Is.False,
                    "An unfed squirrel should not inherit another home’s meal.");
                yield return null;
                Assert.That(traveling.Contains(second.transform), Is.True);
                Assert.That(traveling.Contains(first.transform), Is.False);
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator OlderScoringVersionRetainsIndexedSquirrelTrips()
        {
            var run = RunWithSecondOak(6);
            var secondOak = run.At(4, 3);
            var (root, view, first, second) = CreateView(secondOak.id);
            try
            {
                Assert.That(run.ScoringVersion, Is.EqualTo(6));
                view.PlayForaging(SecondOakMeal(secondOak), null, run);
                var traveling = Traveling(view);
                Assert.That(traveling.Contains(first.transform), Is.True,
                    "Historical routes retain their existing index assignment.");
                Assert.That(traveling.Contains(second.transform), Is.False);
                yield return null;
            }
            finally { Object.Destroy(root); }
        }

        private static ConstructionRunModel RunWithSecondOak(int scoringVersion)
        {
            var original = new ConstructionRunModel();
            Assert.That(original.TrySimulateDay(out _), Is.True);
            var save = original.Export();
            save.introRulesVersion = 0;
            save.score.scoringVersion = scoringVersion;
            Assert.That(ConstructionRunModel.TryRestore(save, out var run), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            return run;
        }

        private static IReadOnlyList<IReadOnlyList<ConstructionTileData>>
            SecondOakMeal(ConstructionTileData secondOak) =>
            new IReadOnlyList<ConstructionTileData>[]
            {
                new[] { secondOak }
            };

        private static (GameObject root, ConstructionWorldView view,
            SquirrelDemoVisual first, SquirrelDemoVisual second) CreateView(
            string secondOakId)
        {
            var root = new GameObject("Squirrel meal home assignment test");
            var view = root.AddComponent<ConstructionWorldView>();
            var camera = new GameObject("Test camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.enabled = false;
            Field("world").SetValue(view, root.transform);
            Field("boardCamera").SetValue(view, camera);

            var first = NewSquirrel(root.transform, "Hungry squirrel");
            var second = NewSquirrel(root.transform, "Fed squirrel");
            var actors = (List<SquirrelDemoVisual>)Field("squirrels")
                .GetValue(view);
            actors.Add(first);
            actors.Add(second);
            var homeTiles = (Dictionary<Transform, string>)Field(
                "animalHomeTiles").GetValue(view);
            homeTiles.Add(first.transform, ConstructionBoardModel.StarterOakId);
            homeTiles.Add(second.transform, secondOakId);
            var homes = (Dictionary<Transform, Vector3>)Field(
                "animalHomes").GetValue(view);
            homes.Add(first.transform, first.transform.position);
            homes.Add(second.transform, second.transform.position);
            return (root, view, first, second);
        }

        private static HashSet<Transform> Traveling(ConstructionWorldView view) =>
            (HashSet<Transform>)Field("travelingAnimals").GetValue(view);

        private static SquirrelDemoVisual NewSquirrel(Transform parent,
            string name)
        {
            var actor = new GameObject(name);
            actor.transform.SetParent(parent, false);
            return actor.AddComponent<SquirrelDemoVisual>();
        }

        private static FieldInfo Field(string name)
        {
            var field = typeof(ConstructionWorldView).GetField(name,
                PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            return field;
        }
    }
}
