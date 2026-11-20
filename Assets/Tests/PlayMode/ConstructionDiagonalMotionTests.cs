using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionDiagonalMotionTests
    {
        private const float Pitch = WorldScaleStandards.CellSizeMeters;
        private static readonly BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator ResidentDiagonalTripPassesTheSharedCornerWithoutEnteringSideRooms()
        {
            var root = new GameObject("Diagonal resident route test");
            var view = root.AddComponent<ConstructionWorldView>();
            var resident = new GameObject("Resident").transform;
            resident.SetParent(root.transform, false);
            var start = Cell(3, 3) + new Vector3(0.8f, 0.23f, -0.5f);
            resident.localPosition = start;
            var route = DiagonalRoute();
            var travel = typeof(ConstructionWorldView).GetMethod("Travel",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(travel, Is.Not.Null);
            var completed = false;
            try
            {
                view.StartCoroutine(Track((IEnumerator)travel.Invoke(null,
                    new object[] { resident, route, 0.25f }),
                    () => completed = true));
                var sampledCorner = false;
                var deadline = Time.realtimeSinceStartup + 5f;
                while (!completed && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var position = resident.localPosition;
                    AssertOutsideClosedSideRooms(position);
                    sampledCorner |= NearSharedCorner(position);
                }
                Assert.That(completed, Is.True);
                Assert.That(sampledCorner, Is.True,
                    "The resident should actually cross the shared corner.");
                Assert.That(Vector3.Distance(resident.localPosition, start),
                    Is.LessThan(0.01f));
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator FoxDiagonalHuntAndReturnAvoidClosedSideRooms()
        {
            var root = new GameObject("Diagonal fox route test");
            var view = root.AddComponent<ConstructionWorldView>();
            var fox = new GameObject("Fox").AddComponent<FoxDemoVisual>();
            fox.transform.SetParent(root.transform, false);
            var start = Cell(3, 3) + new Vector3(0.46f, 0.23f, -0.36f);
            fox.transform.position = start;
            typeof(ConstructionWorldView).GetField("fox", PrivateInstance)
                .SetValue(view, fox);
            var homes = (Dictionary<Transform, Vector3>)
                typeof(ConstructionWorldView).GetField("animalHomes",
                    PrivateInstance).GetValue(view);
            homes.Add(fox.transform, start);
            var hunt = typeof(ConstructionWorldView).GetMethod(
                "AnimateFoxHunt", PrivateInstance);
            Assert.That(hunt, Is.Not.Null);
            var completed = false;
            try
            {
                view.StartCoroutine(Track((IEnumerator)hunt.Invoke(view,
                    new object[] { DiagonalRoute() }),
                    () => completed = true));
                var sampledCorner = false;
                var deadline = Time.realtimeSinceStartup + 7f;
                while (!completed && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var position = fox.transform.position;
                    AssertOutsideClosedSideRooms(position);
                    sampledCorner |= NearSharedCorner(position);
                }
                Assert.That(completed, Is.True);
                Assert.That(sampledCorner, Is.True,
                    "The fox should actually cross the shared corner.");
            }
            finally { Object.Destroy(root); }
        }

        private static List<ConstructionTileData> DiagonalRoute() => new()
        {
            new ConstructionTileData
            {
                id = "start", column = 3, row = 3,
                category = ConstructionCategory.Green
            },
            new ConstructionTileData
            {
                id = "target", column = 4, row = 2,
                category = ConstructionCategory.Green
            }
        };

        private static Vector3 Cell(int column, int row) => new(
            (column - 3) * Pitch, 0f, (3 - row) * Pitch);

        private static bool NearSharedCorner(Vector3 position) =>
            Mathf.Abs(position.x - Pitch * 0.5f) < 0.20f &&
            Mathf.Abs(position.z - Pitch * 0.5f) < 0.20f;

        private static void AssertOutsideClosedSideRooms(Vector3 position)
        {
            const float margin = 0.15f;
            var half = Pitch * 0.5f;
            var inEastSideRoom = position.x > half + margin &&
                position.z < half - margin;
            var inNorthSideRoom = position.z > half + margin &&
                position.x < half - margin;
            Assert.That(inEastSideRoom || inNorthSideRoom, Is.False,
                $"A diagonal crossing entered a closed side room at {position}.");
        }

        private static IEnumerator Track(IEnumerator trip, Action completed)
        {
            yield return trip;
            completed();
        }
    }
}
