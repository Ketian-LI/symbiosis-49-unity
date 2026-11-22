using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionTrafficVisualTests
    {
        [UnityTest]
        public IEnumerator SettledCollisionStopsCarAtRoadCentreThenResumes()
        {
            var root = new GameObject("Collision car test");
            var lamps = new GameObject("Brake lamps");
            lamps.transform.SetParent(root.transform, false);
            var car = root.AddComponent<ConstructionStreetVehicleVisual>();
            car.SetBrakeLamps(lamps);
            car.Configure(true, false, 0f);
            Assert.That(car.QueueCollision(), Is.EqualTo(2f).Within(0.05f));

            var deadline = Time.realtimeSinceStartup + 4f;
            while (!car.CollisionContact && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(car.CollisionContact, Is.True);
            Assert.That(car.transform.localPosition.x, Is.EqualTo(0f).Within(0.3f));
            var stoppedAt = car.transform.localPosition;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(car.transform.localPosition,
                Is.EqualTo(stoppedAt), "The impact beat should hold the car still.");

            deadline = Time.realtimeSinceStartup + 2f;
            while (car.CollisionContact && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(car.CollisionContact, Is.False);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Vector3.Distance(stoppedAt, car.transform.localPosition),
                Is.GreaterThan(0.1f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DailyCollisionUpdatesStreetBadgeAndShowsOneCrossing()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(
                ConstructionRunModel.MaximumEndlessLossLimit,
                ConstructionRunModel.MaximumEndlessLossLimit), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 3, 2, ConstructionCategory.Street);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var (column, row) in new[]
                     { (5, 2), (5, 3), (5, 4), (4, 4) })
            {
                Build(run, column, row, ConstructionCategory.Square);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.CurrentDay, Is.EqualTo(9));
            Build(run, 3, 4, ConstructionCategory.Square);
            Assert.That(run.PreviewEcologyDay().trafficSpecies,
                Is.EqualTo(ConstructionTrafficSpecies.Squirrel));

            var root = new GameObject("Traffic settlement screen test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                Assert.Ignore("A rendered Unity board is required for this visual test.");
            }
            var deadline = Time.realtimeSinceStartup + 6f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(run.LastEcologyDay.trafficSpecies,
                Is.EqualTo(ConstructionTrafficSpecies.Squirrel));
            Assert.That(run.StreetTrafficDeathCount(
                run.LastEcologyDay.trafficStreetTileId), Is.EqualTo(1));
            var badge = root.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Traffic deaths value" &&
                    item.gameObject.activeInHierarchy);
            Assert.That(badge.text, Is.EqualTo("1"));
            var world = root.GetComponent<ConstructionWorldView>();
            Assert.That(world.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == "Traffic crossing silhouette"), Is.True);
            var car = world.GetComponentInChildren<ConstructionStreetVehicleVisual>();
            deadline = Time.realtimeSinceStartup + 9f;
            while (!car.CollisionContact && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(car.CollisionContact, Is.True);
            Assert.That(world.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == "Traffic impact glint"), Is.True);
            world.ClearBuiltWorld();
            Assert.That(world.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == "Traffic crossing silhouette" &&
                    item.gameObject.activeSelf), Is.False);
            Object.Destroy(root);
            yield return null;
        }

        private static void Build(ConstructionRunModel run, int column, int row,
            ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category, GreenPlanting.None,
                out _, out var failure, out var placement), Is.True,
                $"{category} ({column},{row}): {failure} / {placement}");
        }
    }
}
