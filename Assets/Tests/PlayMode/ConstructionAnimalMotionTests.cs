using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionAnimalMotionTests
    {
        private static readonly Vector3 StarterSquirrelHome =
            new Vector3(0.60f, 0.23f, -0.70f);

        [UnityTest]
        public IEnumerator SquirrelRoamsRestsAndSyncDoesNotSnapItHome()
        {
            RequireGraphics();
            var run = new ConstructionRunModel();
            var root = new GameObject("Squirrel ambient continuity test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                yield return WaitForBoard(controller);

                var view = root.GetComponent<ConstructionWorldView>();
                var squirrel = root.transform.Find("Construction low-poly world/Squirrel");
                Assert.That(view, Is.Not.Null);
                Assert.That(squirrel, Is.Not.Null);
                yield return new WaitForSecondsRealtime(0.20f);

                var previous = squirrel.position;
                var previousTime = Time.realtimeSinceStartup;
                var deadline = previousTime + 6f;
                var distanceTravelled = 0f;
                var stillSpan = 0f;
                var longestStillSpan = 0f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var now = Time.realtimeSinceStartup;
                    var delta = now - previousTime;
                    var position = squirrel.position;
                    var step = HorizontalDistance(previous, position);
                    if (delta > 0f && delta < 0.10f)
                    {
                        Assert.That(step, Is.LessThanOrEqualTo(2.2f * delta + 0.055f),
                            "Ambient movement jumped instead of travelling through the room.");
                        distanceTravelled += step;
                        stillSpan = step < 0.0015f ? stillSpan + delta : 0f;
                        longestStillSpan = Mathf.Max(longestStillSpan, stillSpan);
                    }
                    else stillSpan = 0f;
                    previous = position;
                    previousTime = now;
                }

                Assert.That(distanceTravelled, Is.GreaterThan(0.15f),
                    "The squirrel should visibly explore the starter oak room.");
                Assert.That(longestStillSpan, Is.GreaterThanOrEqualTo(0.15f),
                    "Ambient roaming needs a readable pause between short walks.");

                deadline = Time.realtimeSinceStartup + 3f;
                while (HorizontalDistance(squirrel.position, StarterSquirrelHome) < 0.20f &&
                       Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(HorizontalDistance(squirrel.position, StarterSquirrelHome),
                    Is.GreaterThanOrEqualTo(0.20f));
                var beforeSync = squirrel.position;
                view.Sync(run, false);
                Assert.That(HorizontalDistance(beforeSync, squirrel.position),
                    Is.LessThan(0.055f),
                    "Refreshing board data must not reset a roaming animal to its home anchor.");
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator ForagingDetoursAroundResidenceThroughEdgeConnectedTiles()
        {
            RequireGraphics();
            var run = BuildBlockedDetour();
            var food = run.BuiltTiles.Single(tile =>
                tile.column == 5 && tile.row == 3);
            var route = run.GreenFoodRouteTo(
                ConstructionBoardModel.StarterOakId, food.id);
            Assert.That(route.Count, Is.EqualTo(5),
                "The animal must travel through four edge-connected steps.");
            Assert.That(route[1].column, Is.EqualTo(3));
            Assert.That(route[1].row, Is.EqualTo(2));
            Assert.That(route[2].column, Is.EqualTo(4));
            Assert.That(route[2].row, Is.EqualTo(2));
            Assert.That(route.Zip(route.Skip(1), (first, second) =>
                    System.Math.Abs(first.column - second.column) +
                    System.Math.Abs(first.row - second.row))
                .All(distance => distance == 1), Is.True,
                "No foraging step may cut diagonally across a corner.");
            Assert.That(route.Any(tile => tile.column == 4 && tile.row == 3),
                Is.False, "The residence is closed to wildlife.");

            var root = new GameObject("Squirrel foraging continuity test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                yield return WaitForBoard(controller);

                var view = root.GetComponent<ConstructionWorldView>();
                var squirrel = root.transform.Find("Construction low-poly world/Squirrel");
                Assert.That(view, Is.Not.Null);
                Assert.That(squirrel, Is.Not.Null);
                var waypoints = route.Select((tile, index) =>
                    Cell(tile.column, tile.row) +
                    (index == 0 ? StarterSquirrelHome : TravelSquirrelOffset)).ToArray();
                var blockedCenter = Cell(4, 3);
                var foodPoint = waypoints[waypoints.Length - 1];
                var previous = squirrel.position;
                var previousTime = Time.realtimeSinceStartup;
                var cornerSpeeds = new List<float>();
                var cruiseSpeeds = new List<float>();
                var reachedFood = false;
                var returnedHome = false;
                var returnedAt = 0f;

                view.PlayForaging(route, null);
                var deadline = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < deadline &&
                       (!returnedHome || Time.realtimeSinceStartup - returnedAt < 0.70f))
                {
                    yield return null;
                    var now = Time.realtimeSinceStartup;
                    var delta = now - previousTime;
                    var position = squirrel.position;
                    var step = HorizontalDistance(previous, position);
                    if (delta > 0f && delta < 0.10f)
                    {
                        Assert.That(step, Is.LessThanOrEqualTo(8f * delta + 0.075f),
                            "The animal snapped at a route join or on returning home.");
                        Assert.That(DistanceToPolyline(position, waypoints),
                            Is.LessThan(0.60f),
                            "The animal crossed a room instead of following its route.");
                        Assert.That(HorizontalDistance(position, blockedCenter),
                            Is.GreaterThan(1.45f),
                            "The foraging trip cut across a residence.");

                        if (!returnedHome)
                        {
                            var internalCornerDistance = NearestInternalCorner(
                                position, waypoints);
                            var nearestWaypoint = waypoints.Min(point =>
                                HorizontalDistance(position, point));
                            if (internalCornerDistance < 0.32f)
                                cornerSpeeds.Add(step / delta);
                            else if (nearestWaypoint > 0.75f)
                                cruiseSpeeds.Add(step / delta);
                        }
                    }
                    if (HorizontalDistance(position, foodPoint) < 0.28f)
                        reachedFood = true;
                    if (reachedFood && !returnedHome &&
                        HorizontalDistance(position, StarterSquirrelHome) < 0.18f)
                    {
                        returnedHome = true;
                        returnedAt = now;
                    }
                    previous = position;
                    previousTime = now;
                }

                Assert.That(reachedFood, Is.True, "The squirrel never reached the food tile.");
                Assert.That(returnedHome, Is.True, "The squirrel never followed the detour home.");
                Assert.That(cornerSpeeds.Count, Is.GreaterThanOrEqualTo(4));
                Assert.That(cruiseSpeeds.Count, Is.GreaterThanOrEqualTo(4));
                cornerSpeeds.Sort();
                cruiseSpeeds.Sort();
                var cornerLow = cornerSpeeds[Mathf.FloorToInt(
                    0.20f * (cornerSpeeds.Count - 1))];
                var cruiseMedian = cruiseSpeeds[cruiseSpeeds.Count / 2];
                Assert.That(cornerLow, Is.GreaterThan(cruiseMedian * 0.27f),
                    "The animal should turn continuously instead of stopping at every tile.");
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedDayPreviewKeepsTheCurrentTripOnItsPassableRoute()
        {
            RequireGraphics();
            var run = BuildBlockedDetour();
            var food = run.BuiltTiles.Single(tile =>
                tile.column == 5 && tile.row == 3);
            var route = run.GreenFoodRouteTo(
                ConstructionBoardModel.StarterOakId, food.id);
            var waypoints = route.Select((tile, index) =>
                Cell(tile.column, tile.row) +
                (index == 0 ? StarterSquirrelHome : TravelSquirrelOffset)).ToArray();
            var root = new GameObject("Repeated animal trip continuity test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                yield return WaitForBoard(controller);
                var view = root.GetComponent<ConstructionWorldView>();
                var squirrel = root.transform.Find("Construction low-poly world/Squirrel");
                Assert.That(squirrel, Is.Not.Null);

                view.PlayForaging(route, null);
                yield return new WaitForSecondsRealtime(0.45f);
                Assert.That(HorizontalDistance(squirrel.position,
                    StarterSquirrelHome), Is.GreaterThan(0.10f));
                var beforeRefresh = squirrel.position;
                view.PlayForaging(route, null);
                Assert.That(HorizontalDistance(beforeRefresh, squirrel.position),
                    Is.LessThan(0.02f),
                    "A new day must not pull a travelling animal home instantly.");

                var previous = squirrel.position;
                var previousTime = Time.realtimeSinceStartup;
                var deadline = previousTime + 2.5f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var now = Time.realtimeSinceStartup;
                    var delta = now - previousTime;
                    var position = squirrel.position;
                    if (delta > 0f && delta < 0.10f)
                    {
                        Assert.That(HorizontalDistance(previous, position),
                            Is.LessThanOrEqualTo(8f * delta + 0.075f));
                        Assert.That(DistanceToPolyline(position, waypoints),
                            Is.LessThan(0.60f));
                        Assert.That(HorizontalDistance(position, Cell(4, 3)),
                            Is.GreaterThan(1.45f));
                    }
                    previous = position;
                    previousTime = now;
                }
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Construction animal visuals require a graphics device.");
        }

        private static IEnumerator WaitForBoard(ConstructionPrototypeController controller)
        {
            var deadline = Time.realtimeSinceStartup + 4f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
        }

        private static ConstructionRunModel BuildBlockedDetour()
        {
            var save = new ConstructionRunModel().Export();
            save.introRulesVersion = 0;
            Assert.That(ConstructionRunModel.TryRestore(save, out var run), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var construction = new[]
            {
                (column: 4, row: 3, category: ConstructionCategory.Residence,
                    planting: GreenPlanting.None),
                (column: 3, row: 2, category: ConstructionCategory.Green,
                    planting: GreenPlanting.Meadow),
                (column: 4, row: 2, category: ConstructionCategory.Green,
                    planting: GreenPlanting.Meadow),
                (column: 5, row: 2, category: ConstructionCategory.Green,
                    planting: GreenPlanting.Meadow),
                (column: 5, row: 3, category: ConstructionCategory.Green,
                    planting: GreenPlanting.Meadow)
            };
            for (var index = 0; index < construction.Length; index++)
            {
                var item = construction[index];
                Assert.That(run.TryBuild(item.column, item.row, item.category,
                    item.planting, out _, out var failure, out var placement),
                    Is.True, $"Cannot construct detour tile {index}: {failure}/{placement}");
                if (index + 1 < construction.Length)
                    Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            return run;
        }

        private static Vector3 Cell(int column, int row) => new Vector3(
            (column - 3) * WorldScaleStandards.CellSizeMeters, 0f,
            (3 - row) * WorldScaleStandards.CellSizeMeters);

        private static Vector3 TravelSquirrelOffset => new Vector3(
            StarterSquirrelHome.x * 0.2f, StarterSquirrelHome.y,
            StarterSquirrelHome.z * 0.2f);

        private static float HorizontalDistance(Vector3 first, Vector3 second) =>
            Vector2.Distance(new Vector2(first.x, first.z),
                new Vector2(second.x, second.z));

        private static float NearestInternalCorner(Vector3 point,
            IReadOnlyList<Vector3> waypoints)
        {
            var nearest = float.PositiveInfinity;
            for (var index = 1; index < waypoints.Count - 1; index++)
                nearest = Mathf.Min(nearest,
                    HorizontalDistance(point, waypoints[index]));
            return nearest;
        }

        private static float DistanceToPolyline(Vector3 point,
            IReadOnlyList<Vector3> waypoints)
        {
            var nearest = float.PositiveInfinity;
            var p = new Vector2(point.x, point.z);
            for (var index = 1; index < waypoints.Count; index++)
            {
                var a = new Vector2(waypoints[index - 1].x,
                    waypoints[index - 1].z);
                var b = new Vector2(waypoints[index].x,
                    waypoints[index].z);
                var segment = b - a;
                var fraction = Mathf.Clamp01(Vector2.Dot(p - a, segment) /
                    Mathf.Max(segment.sqrMagnitude, 0.0001f));
                nearest = Mathf.Min(nearest, Vector2.Distance(p, a + segment * fraction));
            }
            return nearest;
        }
    }
}
