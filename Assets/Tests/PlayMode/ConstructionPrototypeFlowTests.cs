using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.CameraRecognition;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionPrototypeFlowTests
    {
        [UnityTest]
        public IEnumerator BoardGameMouseCannotReplaceCameraStagedPlot()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Board game camera placement gate test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            Assert.That(controller.RequestPlacement(4, 3), Is.False,
                "A mouse click cannot stage a new physical room.");
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.RequestCameraPlacement(4, 3), Is.True);
            Assert.That(controller.CameraPendingMatches(4, 3,
                ConstructionCategory.Green, GreenPlanting.None,
                ResidenceKind.Standard), Is.True);
            Assert.That(controller.RequestPlacement(3, 2), Is.False,
                "A mouse click cannot replace a camera-staged coordinate.");
            Assert.That(controller.CameraPendingMatches(4, 3,
                ConstructionCategory.Green, GreenPlanting.None,
                ResidenceKind.Standard), Is.True);
            Assert.That(controller.CameraPendingMatches(3, 2,
                ConstructionCategory.Green, GreenPlanting.None,
                ResidenceKind.Standard), Is.False);
            controller.CancelPendingPlacement();
            Assert.That(controller.CameraPendingMatches(4, 3,
                ConstructionCategory.Green, GreenPlanting.None,
                ResidenceKind.Standard), Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardGameUdpRecognitionStagesAndConfirmsOneRoom()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Board game UDP recognition test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            var bridge = root.AddComponent<ConstructionCameraUdpBridge>();
            yield return null;

            const string starter = "{\"schemaVersion\":1,\"sequence\":1," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";
            const string added = "{\"schemaVersion\":1,\"sequence\":2," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}," +
                "{\"markerId\":12,\"column\":4,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"None\"}]}";
            using (var sender = new UdpClient())
            {
                for (var frame = 0; frame < 55; frame++)
                {
                    var bytes = Encoding.UTF8.GetBytes(starter);
                    sender.Send(bytes, bytes.Length,
                        new IPEndPoint(IPAddress.Loopback,
                            ConstructionCameraUdpBridge.ListenPort));
                    yield return new WaitForSecondsRealtime(0.04f);
                }
                Assert.That(bridge.CurrentStatus,
                    Does.Contain("Board synced").And.Contain("Draw one face-down tile"));

                for (var frame = 0; frame < 55; frame++)
                {
                    var bytes = Encoding.UTF8.GetBytes(added);
                    sender.Send(bytes, bytes.Length,
                        new IPEndPoint(IPAddress.Loopback,
                            ConstructionCameraUdpBridge.ListenPort));
                    yield return new WaitForSecondsRealtime(0.04f);
                }
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);
                Assert.That(controller.RequestPlacement(3, 2), Is.False);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                Assert.That(run.BuiltCount, Is.EqualTo(2));
                Assert.That(run.At(4, 3).planting,
                    Is.EqualTo(GreenPlanting.Meadow));
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingOccupiedNeighbourOakShowsSquirrelForagingRoutes()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var point in new[] { (column: 4, row: 3), (column: 3, row: 2) })
            {
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var occupiedOak = run.At(4, 3);
            Assert.That(run.SquirrelPopulation, Is.EqualTo(2));
            Assert.That(run.VisibleSquirrelHomeTiles(8)[1].id,
                Is.EqualTo(occupiedOak.id));
            Assert.That(run.PreviewEcologyDay().squirrelFoodTileIds[1],
                Is.EqualTo(occupiedOak.id),
                "The second squirrel eats in its own oak in this fixture.");

            var root = new GameObject("Occupied neighbour oak route inspection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            AssertAnimalCornerBadge(root, occupiedOak, "Resident squirrel", "1");

            Assert.That(controller.TryPlace(4, 3), Is.False);
            var canvas = root.transform.Find("Construction UI");
            var status = canvas.Find("Route inspection/Route status")
                .GetComponent<Text>();
            var overlay = canvas.Find(
                "7 by 7 construction board/Selected route overlay");
            Assert.That(status.text, Does.Contain("SQUIRRELS"));
            AssertRouteTextFits(status);
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"),
                Is.Zero, "An on-tile meal needs no centre marker.");
            Assert.That(ActiveRouteElementCount(overlay, "Route segment"),
                Is.Zero,
                "A squirrel eating in this oak should not get a false line from the central oak.");
            Assert.That(ActiveRouteElementCount(overlay, "Food destination"),
                Is.Zero, "An on-tile meal should not leave a gold square.");
            controller.ToggleLanguage();
            Assert.That(status.text, Does.Contain("此处"));
            AssertRouteTextFits(status);
            AssertGreenHoverAnimal(root, occupiedOak, "松鼠", 1);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingHedgehogShrubShowsItsFoodRoute()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var habitat = run.HedgehogHabitat;
            Assert.That(habitat, Is.Not.Null);
            Assert.That(run.PreviewEcologyDay().hedgehogFoodTileId, Is.Not.Null,
                "This fixture needs an actual next-day hedgehog meal.");

            var root = new GameObject("Hedgehog shrub route inspection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            AssertAnimalCornerBadge(root, habitat, "Resident hedgehog", "1");

            Assert.That(controller.TryPlace(habitat.column, habitat.row), Is.False);
            var canvas = root.transform.Find("Construction UI");
            var status = canvas.Find("Route inspection/Route status")
                .GetComponent<Text>();
            var overlay = canvas.Find(
                "7 by 7 construction board/Selected route overlay");
            Assert.That(status.text, Does.Contain("HEDGEHOG"));
            AssertRouteTextFits(status);
            Assert.That(run.PreviewEcologyDay().hedgehogFoodTileId,
                Is.EqualTo(habitat.id), "This hedgehog eats in its own shrub.");
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"),
                Is.Zero);
            Assert.That(ActiveRouteElementCount(overlay, "Food destination"),
                Is.Zero);
            controller.ToggleLanguage();
            Assert.That(status.text, Does.Contain("刺猬"));
            AssertRouteTextFits(status);
            AssertGreenHoverAnimal(root, habitat, "刺猬", 1);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingFoxShrubShowsItsHuntRoute()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 6, row: 3, planting: GreenPlanting.Shrub)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var habitat = run.FoxHabitat;
            Assert.That(habitat, Is.Not.Null);
            Assert.That(run.PreviewEcologyDay().foxHuntRouteTileIds.Count,
                Is.GreaterThan(1), "This fixture needs a reachable prey route.");

            var root = new GameObject("Fox shrub route inspection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            AssertAnimalCornerBadge(root, habitat, "Resident fox", "1");

            Assert.That(controller.TryPlace(habitat.column, habitat.row), Is.False);
            var canvas = root.transform.Find("Construction UI");
            var status = canvas.Find("Route inspection/Route status")
                .GetComponent<Text>();
            var overlay = canvas.Find(
                "7 by 7 construction board/Selected route overlay");
            Assert.That(status.text, Does.Contain("FOX"));
            AssertRouteTextFits(status);
            AssertActiveRouteOriginAt(overlay, habitat);
            Assert.That(ActiveRouteElementCount(overlay, "Route segment"),
                Is.GreaterThan(0));
            controller.ToggleLanguage();
            Assert.That(status.text, Does.Contain("狐狸"));
            AssertRouteTextFits(status);
            AssertGreenHoverAnimal(root, habitat, "狐狸", 1);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameTileMealLeavesNoMarkerAndDayAdvanceClearsInspection()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Same-tile meal marker regression test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var canvas = root.transform.Find("Construction UI");
            var status = canvas.Find("Route inspection/Route status")
                .GetComponent<Text>();
            var overlay = canvas.Find(
                "7 by 7 construction board/Selected route overlay");
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(status.text, Does.Contain("SQUIRRELS"));
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"), Is.Zero);
            Assert.That(ActiveRouteElementCount(overlay, "Food destination"), Is.Zero);
            Assert.That(ActiveRouteElementCount(overlay, "Route segment"), Is.Zero);

            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(status.text, Does.Not.Contain("SQUIRRELS"),
                "Clicking the selected room again clears its route inspection.");
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(status.text, Does.Contain("SQUIRRELS"));

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(status.text, Does.Not.Contain("SQUIRRELS"),
                "A new day must not preserve yesterday's selected route.");
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"), Is.Zero);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingAnimalFreeGreenClearsPreviousAnimalRoute()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.Zero);

            var root = new GameObject("Empty green route inspection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var emptyMeadow = run.At(4, 3);
            var emptyCell = root.transform.Find(
                "Construction UI/7 by 7 construction board/Cell 5,4");
            Assert.That(FindCellMarker(emptyCell, "Pigeon flock").gameObject.activeSelf,
                Is.False, "A green tile without a flock must not show a pigeon count.");
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            AssertGreenHoverWithoutAnimals(root, emptyMeadow);

            var canvas = root.transform.Find("Construction UI");
            var overlay = canvas.Find(
                "7 by 7 construction board/Selected route overlay");
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"),
                Is.Zero, "A squirrel eating in its own oak has no travel marker.");
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(ActiveRouteElementCount(overlay, "Route origin"), Is.Zero);
            Assert.That(ActiveRouteElementCount(overlay, "Food destination"), Is.Zero);
            Assert.That(ActiveRouteElementCount(overlay, "Route segment"), Is.Zero,
                "The previous animal's route must not remain on an unoccupied green.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExtinctFlockShowsWaitingBadgeWithoutFalseOccupancy()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));
            var save = run.Export();
            save.survival.predatedPigeons = 2;
            Assert.That(ConstructionRunModel.TryRestore(save, out var extinct),
                Is.True, "A former flock site with no surviving birds is valid.");
            Assert.That(extinct.PigeonPopulation, Is.Zero);

            var root = new GameObject("Extinct pigeon corner status test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(extinct);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var meadow = extinct.At(4, 3);
            var cell = root.transform.Find(
                "Construction UI/7 by 7 construction board/Cell 5,4");
            var badge = FindCellMarker(cell, "Pigeon flock");
            Assert.That(badge.gameObject.activeSelf,
                Is.EqualTo(!root.GetComponent<ConstructionWorldView>().IsReady),
                "The pending flock uses a centre portrait only without the 3D board.");
            Assert.That(ActiveAnimalCount(cell).text, Is.EqualTo("待"),
                "An absent flock must not be counted as a living 0 or 2.");
            var returnStatus = extinct.ReturnStatus(
                ConstructionWildlifeSpecies.Pigeon);
            Assert.That(returnStatus.Pending, Is.True);
            Assert.That(returnStatus.EarliestDay,
                Is.EqualTo(extinct.CurrentDay + 3));
            AssertGreenHoverWithoutAnimals(root, meadow);
            var hoverText = RoomBadgeDetails(root, meadow, "Animal group count");
            Assert.That(hoverText, Does.Contain("待迁入"));
            Assert.That(hoverText, Does.Contain(
                $"最早第{returnStatus.EarliestDay}天"));
            Assert.That(hoverText, Does.Contain("成熟连通草地"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonMeadowShowsReadableCountAndRecoveryHint()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));

            var root = new GameObject("Pigeon meadow corner status test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var meadow = run.BuiltTiles.Single(tile => tile.id == run.PigeonTileId);
            AssertAnimalCornerBadge(root, meadow, "Pigeon flock", "2");
            AssertGreenHoverAnimal(root, meadow, "鸽子", 2);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreenAnimalPortraitsFollow3DAvailabilityWithoutLosingCounts()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));

            var root = new GameObject("Animal centre portrait fallback test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var threeDReady = root.GetComponent<ConstructionWorldView>().IsReady;
            Debug.Log($"Animal portrait test: 3D ready = {threeDReady}");
            if (threeDReady)
            {
                Assert.That(root.transform.Find("Construction low-poly world/Squirrel"),
                    Is.Not.Null);
                Assert.That(root.transform.Find("Construction low-poly world/Pigeon flock"),
                    Is.Not.Null);
            }
            var board = root.transform.Find("Construction UI/7 by 7 construction board");
            foreach (var animal in new[]
                     {
                         (cell: "Cell 4,4", portrait: "Resident squirrel", count: "1"),
                         (cell: "Cell 5,4", portrait: "Pigeon flock", count: "2")
                     })
            {
                var cell = board.Find(animal.cell);
                var portrait = FindCellMarker(cell, animal.portrait);
                Assert.That(portrait.gameObject.activeInHierarchy,
                    Is.EqualTo(!threeDReady),
                    $"{animal.portrait} centre art must yield to the 3D animal, " +
                    "but remain available without 3D rendering.");
                var count = ActiveAnimalCount(cell);
                Assert.That(count.text, Is.EqualTo(animal.count));
                Assert.That(count.gameObject.activeInHierarchy, Is.True,
                    "Hiding duplicate centre art must not hide the side count badge.");
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConstructionWildlifeIsVisibleAndMovesWithoutCrossRoomFood()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));

            var root = new GameObject("Construction wildlife presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }

            var world = root.transform.Find("Construction low-poly world");
            Assert.That(world, Is.Not.Null);
            Assert.That(world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>(), Is.Null);
            Assert.That(world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.FoxDemoVisual>(), Is.Null);
            var view = root.GetComponent<ConstructionWorldView>();
            var squirrel = world.Find("Squirrel");
            Assert.That(squirrel, Is.Not.Null);
            Assert.That(squirrel.gameObject.activeInHierarchy, Is.True);
            Assert.That(System.Array.Exists(
                squirrel.GetComponentsInChildren<Renderer>(), renderer =>
                    renderer.enabled && renderer.gameObject.activeInHierarchy),
                Is.True, "The construction squirrel must actually render.");
            var birds = world.GetComponentsInChildren<
                UrbanWildlifeRooms.Animals.PigeonDemoVisual>();
            Assert.That(birds.Length, Is.EqualTo(2),
                "The two arriving pigeons need two visible 3D animals, not one static icon.");
            foreach (var bird in birds)
                Assert.That(System.Array.Exists(
                    bird.GetComponentsInChildren<Renderer>(), renderer =>
                        renderer.enabled && renderer.gameObject.activeInHierarchy),
                    Is.True, "Every displayed pigeon must actually render.");
            var idleSquirrelStart = squirrel.position;
            var idlePigeonStart = birds[0].transform.position;
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(Vector3.Distance(idleSquirrelStart, squirrel.position),
                Is.GreaterThan(0.03f),
                "The squirrel should move even while no day-end route is playing.");
            Assert.That(Vector3.Distance(idlePigeonStart, birds[0].transform.position),
                Is.GreaterThan(0.03f),
                "Pigeons should move even while no day-end route is playing.");
            var squirrelRoute = run.PreviewSquirrelFoodRoute();
            Assert.That(squirrelRoute.Count, Is.EqualTo(1),
                "This checks visible foraging even when food is in the home room.");
            var pigeonRoute = run.PreviewPigeonFoodRoute();
            Assert.That(pigeonRoute.Count, Is.GreaterThan(0));
            view.PlayForaging(
                new System.Collections.Generic.IReadOnlyList<ConstructionTileData>[]
                    { squirrelRoute },
                new System.Collections.Generic.IReadOnlyList<ConstructionTileData>[]
                    { pigeonRoute, pigeonRoute });
            var start = squirrel.position;
            var firstBirdStart = birds[0].transform.position;
            var secondBirdStart = birds[1].transform.position;
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(Vector3.Distance(start, squirrel.position),
                Is.GreaterThan(0.08f),
                "A same-room food route should not leave the squirrel motionless.");
            Assert.That(Vector3.Distance(firstBirdStart,
                birds[0].transform.position), Is.GreaterThan(0.08f));
            Assert.That(Vector3.Distance(secondBirdStart,
                birds[1].transform.position), Is.GreaterThan(0.08f),
                "The second fed pigeon must forage too, not just the first bird.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuiltStreetShowsMovingCarAndResetRemovesIt()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Street,
                GreenPlanting.None, out _, out _, out _), Is.True);

            var root = new GameObject("Construction street traffic test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }

            var world = root.transform.Find("Construction low-poly world");
            var paving = world.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.name.Contains("Street north-south paving") ||
                    renderer.name.Contains("Street east-west paving")).ToArray();
            Assert.That(paving.Length, Is.EqualTo(3),
                "The cross should be three flush pieces with no overlapping top faces.");
            foreach (var renderer in paving)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_InkStrength"), Is.Zero,
                    "Paving must not draw box-edge ink as black stripes across the street.");
            }
            var cars = world.GetComponentsInChildren<ConstructionStreetVehicleVisual>();
            Assert.That(cars.Length, Is.EqualTo(1),
                "Only the built street should display traffic.");
            var car = cars[0];
            var wheels = car.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.name.StartsWith("Wheel ")).ToArray();
            Assert.That(wheels.Length, Is.EqualTo(4));
            foreach (var renderer in wheels)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_InkStrength"), Is.Zero,
                    "Wheel outlines should not look like black marks on the road.");
            }
            Assert.That(car.EastWest, Is.True,
                "The car should align with the home-to-street connection.");
            Assert.That(car.transform.localPosition.z, Is.EqualTo(0f).Within(0.001f),
                "East-west traffic must stay on the street's painted centreline.");
            Assert.That(car.GetComponentsInChildren<Renderer>().Length,
                Is.GreaterThan(0), "The car must have visible 3D geometry.");
            var start = car.transform.position;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Vector3.Distance(start, car.transform.position),
                Is.GreaterThan(0.03f), "Street traffic should visibly move.");

            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(5, 4, ConstructionCategory.Street,
                GreenPlanting.None, out _, out _, out _), Is.True);
            var view = root.GetComponent<ConstructionWorldView>();
            view.Sync(run, false);
            Assert.That(world.GetComponentsInChildren<ConstructionStreetVehicleVisual>()
                .Length, Is.EqualTo(2));
            Assert.That(car.transform.localScale.x, Is.Zero,
                "A newly available approach must begin offscreen, not turn in place.");
            Assert.That(car.EastWest ? car.transform.localPosition.z :
                car.transform.localPosition.x, Is.EqualTo(0f).Within(0.001f),
                "Traffic must stay on the selected street's centreline.");

            view.ClearBuiltWorld();
            Assert.That(car.gameObject.activeInHierarchy, Is.False,
                "A new run must not retain cars from the previous street layout.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StreetCarLeavesTheTileBeforeStartingAnotherPass()
        {
            var root = new GameObject("Single street-car passage test");
            var car = root.AddComponent<ConstructionStreetVehicleVisual>();
            car.Configure(false, 0f);
            Assert.That(car.transform.localScale.x, Is.Zero);
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(car.transform.localScale.x, Is.GreaterThan(0.9f));
            var firstPosition = car.transform.localPosition.z;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Mathf.Abs(car.transform.localPosition.z - firstPosition),
                Is.GreaterThan(0.03f),
                "Traffic should move from either randomly chosen end.");
            yield return new WaitForSecondsRealtime(3.3f);
            Assert.That(car.transform.localScale.x, Is.Zero,
                "The car should leave through the far threshold before the loop resets.");
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(car.transform.localScale.x, Is.GreaterThan(0.9f),
                "A later car should enter after the traffic gap.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoFedSquirrelsBothShowTheirFoodTrips()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var position in new[] { (4, 3), (3, 2) })
            {
                Assert.That(run.TryBuild(position.Item1, position.Item2,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.SquirrelPopulation, Is.EqualTo(2));
            var routes = run.PreviewSquirrelFoodRoutes();
            Assert.That(routes.Count, Is.EqualTo(2));

            var root = new GameObject("Two squirrel forage presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var squirrels = root.transform.Find("Construction low-poly world")
                .GetComponentsInChildren<UrbanWildlifeRooms.Animals.SquirrelDemoVisual>();
            Assert.That(squirrels.Length, Is.EqualTo(2));
            var view = root.GetComponent<ConstructionWorldView>();
            view.PlayForaging(routes, null, run);
            var firstImmediate = squirrels[0].transform.position;
            var secondImmediate = squirrels[1].transform.position;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Vector3.Distance(firstImmediate,
                squirrels[0].transform.position),
                Is.GreaterThan(0.08f));
            Assert.That(Vector3.Distance(secondImmediate,
                squirrels[1].transform.position), Is.GreaterThan(0.08f),
                "A squirrel eating in its own oak must visibly forage in-room.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonRetracesConnectedMeadowsAfterForaging()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var position in new[] { (4, 3), (4, 2), (5, 2), (5, 1) })
            {
                Assert.That(run.TryBuild(position.Item1, position.Item2,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            // The flock can shift its home meadow after eating. Choose the
            // farthest connected source so the return has a real corner.
            var route = run.BuiltTiles.Where(tile =>
                    tile.planting == GreenPlanting.Meadow)
                .Select(tile => run.GreenFoodRouteTo(run.PigeonTileId, tile.id))
                .OrderByDescending(candidate => candidate.Count).First();
            Assert.That(route.Count, Is.GreaterThanOrEqualTo(3));

            var root = new GameObject("Pigeon return route presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var bird = root.transform.Find("Construction low-poly world")
                .GetComponentInChildren<UrbanWildlifeRooms.Animals.PigeonDemoVisual>();
            Assert.That(bird, Is.Not.Null);
            // Multi-tile journeys travel through edge-connected rooms and
            // must not visibly clip a closed neighbouring room.
            var foodTile = route[route.Count - 1];
            var intermediateTile = route[route.Count - 2];
            var foodPosition = new Vector3(
                (foodTile.column - 3) * WorldScaleStandards.CellSizeMeters,
                0.23f, (3 - foodTile.row) * WorldScaleStandards.CellSizeMeters);
            var intermediate = new Vector3(
                (intermediateTile.column - 3) * WorldScaleStandards.CellSizeMeters,
                0.23f, (3 - intermediateTile.row) * WorldScaleStandards.CellSizeMeters);
            root.GetComponent<ConstructionWorldView>().PlayForaging(null,
                new System.Collections.Generic.IReadOnlyList<ConstructionTileData>[]
                    { route }, run);
            var deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline &&
                   Vector3.Distance(bird.transform.position, foodPosition) > 0.3f)
                yield return null;
            Assert.That(Vector3.Distance(bird.transform.position, foodPosition),
                Is.LessThan(0.3f), "The pigeon must reach its food tile.");
            deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline &&
                   Vector3.Distance(bird.transform.position, intermediate) > 0.4f)
                yield return null;
            Assert.That(Vector3.Distance(bird.transform.position, intermediate),
                Is.LessThan(0.4f),
                "The return trip must pass through the connected meadow, not a wall.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatureShrubsAndPigeonsBringVisibleMovingVisitors()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            // The final meadow needs one more day to reach mature habitat.
            Assert.That(run.TryBuild(5, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.HedgehogHabitat, Is.Not.Null);
            Assert.That(run.FoxHabitat, Is.Not.Null);
            Assert.That(run.TryBuild(2, 1, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out _, out _), Is.True);
            var hedgehogRoute = run.GreenFoodRouteTo(run.HedgehogHabitat.id,
                square.id);
            Assert.That(hedgehogRoute.Count, Is.GreaterThanOrEqualTo(2),
                "The animal reaches the square through a built passable route.");
            Assert.That(hedgehogRoute.Last().category,
                Is.EqualTo(ConstructionCategory.Square));
            var root = new GameObject("Construction habitat visitors test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            var habitatDeadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < habitatDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            var habitatCanvas = root.transform.Find("Construction UI");
            var habitatTooltip = habitatCanvas.Find("Room badge tooltip");
            var roomHeading = habitatTooltip.Find("Tooltip heading").GetComponent<Text>();
            var roomDetails = habitatTooltip.Find("Tooltip details").GetComponent<Text>();
            var shrubHover = habitatCanvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            var shrubPointer = new PointerEventData(EventSystem.current);
            shrubHover.OnPointerEnter(shrubPointer);
            Assert.That(habitatTooltip.gameObject.activeSelf, Is.True);
            Assert.That(roomHeading.text, Does.Contain("Shrub"));
            Assert.That(roomDetails.text, Is.Not.Empty);
            Assert.That(habitatCanvas.Find("Context tooltip").gameObject.activeSelf,
                Is.False);
            shrubHover.OnPointerExit(shrubPointer);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            shrubHover.OnPointerEnter(shrubPointer);
            Assert.That(roomHeading.text, Does.Contain("灌木"));
            Assert.That(roomDetails.text, Is.Not.Empty);
            Assert.That(roomDetails.preferredHeight,
                Is.LessThanOrEqualTo(roomDetails.rectTransform.rect.height + 1f));
            shrubHover.OnPointerExit(shrubPointer);
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var world = root.transform.Find("Construction low-poly world");
            Assert.That(world, Is.Not.Null);
            var hedgehog = world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>();
            var fox = world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.FoxDemoVisual>();
            Assert.That(hedgehog, Is.Not.Null);
            Assert.That(fox, Is.Not.Null);
            foreach (var actor in new[] { hedgehog.transform, fox.transform })
                Assert.That(System.Array.Exists(
                    actor.GetComponentsInChildren<Renderer>(), renderer =>
                        renderer.enabled && renderer.gameObject.activeInHierarchy),
                    Is.True, "Each visitor needs an enabled 3D renderer.");
            var hedgehogStart = hedgehog.transform.position;
            var foxStart = fox.transform.position;
            // Ambient wildlife now pauses between short walks. Observe a full
            // activity cycle instead of requiring motion in one fixed window.
            var ambientDeadline = Time.realtimeSinceStartup + 2.2f;
            var hedgehogTravel = 0f;
            var foxTravel = 0f;
            while (Time.realtimeSinceStartup < ambientDeadline &&
                   (hedgehogTravel <= 0.03f || foxTravel <= 0.03f))
            {
                yield return null;
                hedgehogTravel = Mathf.Max(hedgehogTravel,
                    Vector3.Distance(hedgehogStart, hedgehog.transform.position));
                foxTravel = Mathf.Max(foxTravel,
                    Vector3.Distance(foxStart, fox.transform.position));
            }
            Assert.That(hedgehogTravel, Is.GreaterThan(0.03f));
            Assert.That(foxTravel, Is.GreaterThan(0.03f));
            var forageStart = hedgehog.transform.position;
            root.GetComponent<ConstructionWorldView>().PlayForaging(null, null,
                run, hedgehogRoute);
            var squareRoom = world.Find($"{square.id} CommunitySquare");
            Assert.That(squareRoom, Is.Not.Null);
            var routeDeadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < routeDeadline &&
                   Vector3.Distance(hedgehog.transform.position,
                       squareRoom.position) > 0.8f)
                yield return null;
            Assert.That(Vector3.Distance(hedgehog.transform.position,
                squareRoom.position), Is.LessThan(0.8f),
                "The hedgehog must visibly reach the passable square.");
            routeDeadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < routeDeadline &&
                   Vector3.Distance(hedgehog.transform.position,
                       forageStart) > 0.75f)
                yield return null;
            Assert.That(Vector3.Distance(hedgehog.transform.position,
                forageStart), Is.LessThan(0.75f),
                "The hedgehog must return along the connected route.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FoxHuntIsReportedAndFollowsTheSettledGreenRoute()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 6, row: 3, planting: GreenPlanting.Shrub)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var riskRoot = new GameObject("Construction fox risk preview test");
            var riskController = riskRoot.AddComponent<ConstructionPrototypeController>();
            riskController.UseRunForTesting(run);
            riskController.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var forecast = run.PreviewEcologyDay();
            var riskTile = run.BuiltTiles.First(tile =>
                tile.id == forecast.predationTileId);
            var riskCell = riskRoot.GetComponentsInChildren<Transform>(true)
                .First(item => item.name ==
                    $"Cell {riskTile.column + 1},{riskTile.row + 1}");
            Assert.That(riskCell.Find("Fox risk").gameObject.activeSelf, Is.True,
                "The fox warning must identify the actual threatened room.");
            Assert.That(riskRoot.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "Fox risk" && item.gameObject.activeSelf),
                Is.EqualTo(1));
            var riskBadge = riskCell.Find("Fox risk/Fox risk badge");
            Assert.That(riskBadge, Is.Not.Null);
            riskBadge.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(
                new PointerEventData(EventSystem.current));
            var localRisk = riskRoot.transform.Find(
                "Construction UI/Room badge tooltip");
            Assert.That(localRisk.gameObject.activeSelf, Is.True);
            var riskPreview = localRisk.Find("Tooltip details").GetComponent<Text>();
            Assert.That(localRisk.Find("Tooltip heading").GetComponent<Text>().text,
                Does.Contain("FOX RISK"));
            Assert.That(riskPreview.text, Does.Contain("Fox risk"));
            riskBadge.GetComponent<Button>().onClick.Invoke();
            Assert.That(riskCell.Find("Fox risk").localScale.x,
                Is.GreaterThan(1f));
            Assert.That(riskPreview.text, Does.Contain(
                $"{riskTile.column + 1},{riskTile.row + 1}"));
            Assert.That(riskPreview.raycastTarget, Is.False,
                "The local explanation must not block clicks on the risk badge.");
            var riskView = riskRoot.GetComponent<ConstructionWorldView>();
            var liveSquirrel = SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null ? null :
                riskRoot.transform.Find("Construction low-poly world/Squirrel");
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.That(liveSquirrel, Is.Not.Null);
            if (liveSquirrel != null)
                Assert.That(liveSquirrel.gameObject.activeInHierarchy, Is.True);
            Assert.That(run.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(ecology.foxHuntRouteTileIds.Count, Is.GreaterThan(1));
            if (liveSquirrel != null)
            {
                riskView.Sync(run, false);
                Assert.That(liveSquirrel.gameObject.activeSelf, Is.False,
                    "The settled population should already reflect the hunt.");
                riskView.PlayFoxHunt(ecology.foxHuntRouteTileIds, run.BuiltTiles,
                    ecology.predatedSpecies);
                Assert.That(liveSquirrel.gameObject.activeInHierarchy, Is.True,
                    "Keep the victim visible until the fox reaches it.");
                yield return new WaitForSecondsRealtime(1.6f);
                Assert.That(liveSquirrel.gameObject.activeSelf, Is.False,
                    "The victim should disappear when the fox reaches its tile.");
            }
            Object.Destroy(riskRoot);
            yield return null;
            var root = new GameObject("Construction fox hunt presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var review = root.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(review.text, Does.Contain("Fox: 1 squirrel hunted"));
            Assert.That(review.GetComponent<CanvasGroup>().alpha,
                Is.GreaterThanOrEqualTo(0.9f));
            Assert.That(review.preferredHeight,
                Is.LessThanOrEqualTo(review.rectTransform.rect.height + 1f),
                "A reported animal incident must remain legible inside the ledger.");
            var nextForecast = run.PreviewEcologyDay();
            var activeWarnings = root.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "Fox risk" && item.gameObject.activeSelf);
            Assert.That(activeWarnings, Is.EqualTo(
                nextForecast.predatedSpecies == ConstructionPreySpecies.None ? 0 : 1),
                "An obsolete fox warning must clear after the next day settles.");
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var fox = root.GetComponentInChildren<
                    UrbanWildlifeRooms.Animals.FoxDemoVisual>();
                Assert.That(fox, Is.Not.Null);
                var start = fox.transform.position;
                root.GetComponent<ConstructionWorldView>().PlayFoxHunt(
                    ecology.foxHuntRouteTileIds, run.BuiltTiles,
                    ecology.predatedSpecies);
                var replayPrey = root.transform.Find(
                    "Construction low-poly world/Fox hunt prey") ??
                    root.transform.Find("Construction low-poly world/Squirrel");
                Assert.That(replayPrey, Is.Not.Null,
                    "A replay needs either the hidden original or a temporary prey visual.");
                Assert.That(replayPrey.gameObject.activeInHierarchy, Is.True);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(Vector3.Distance(start, fox.transform.position),
                    Is.GreaterThan(0.3f));
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(replayPrey.gameObject.activeSelf, Is.False);
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HedgehogStaysVisibleUntilTheFoxReachesItsShrub()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 3, row: 4, planting: GreenPlanting.Oak),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var save = run.Export();
            save.pigeonTileId = run.At(4, 3).id;
            Assert.That(ConstructionRunModel.TryRestore(save, out var sheltered),
                Is.True);
            Assert.That(sheltered.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));

            var root = new GameObject("Construction hedgehog hunt visual test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(sheltered);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var view = root.GetComponent<ConstructionWorldView>();
            var hedgehog = root.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>();
            Assert.That(hedgehog, Is.Not.Null);
            Assert.That(hedgehog.gameObject.activeInHierarchy, Is.True);
            Assert.That(sheltered.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));
            view.Sync(sheltered, false);
            Assert.That(hedgehog.gameObject.activeSelf, Is.False);
            view.PlayFoxHunt(ecology.foxHuntRouteTileIds, sheltered.BuiltTiles,
                ecology.predatedSpecies);
            Assert.That(hedgehog.gameObject.activeInHierarchy, Is.True);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(hedgehog.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoryTutorialSuggestsBuildTypeWithoutChoosingAPlot()
        {
            var root = new GameObject("Construction tutorial focus test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            var legacyRun = LegacyFillRun();
            controller.UseRunForTesting(legacyRun);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            controller.DismissOpeningTour();

            var focus = root.GetComponentsInChildren<RectTransform>(true)
                .Single(rect => rect.name == "Tutorial focus");
            Assert.That(focus.parent.name, Is.EqualTo("Cell 4,4"));
            Assert.That(focus.GetComponentsInChildren<Image>(true)
                .All(edge => !edge.raycastTarget), Is.True,
                "Tutorial focus must not intercept board or button clicks.");

            controller.TryPlace(3, 3);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(focus.parent.name, Is.EqualTo("Open build palette"));

            controller.ToggleBuildPalette();
            Assert.That(focus.parent.name, Is.EqualTo("Card face"));
            Assert.That(focus.parent.parent.name, Is.EqualTo("Build Green"));
            var canvas = root.transform.Find("Construction UI");
            Assert.That(canvas.Find("Building choices/Green planting"), Is.Null,
                "Vegetation is determined by the green plot, not a second menu.");
            canvas.Find("Building choices/Build Green")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(focus.gameObject.activeSelf, Is.False,
                "The tutorial must not single out a mandatory building plot.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 1,1/Buildable hint")
                .gameObject.activeSelf, Is.False,
                "The distant plot cannot start a disconnected settlement.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 5,4/Buildable hint")
                .gameObject.activeSelf, Is.True,
                "The adjacent meadow remains a valid player choice.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 5,5/Buildable hint")
                .gameObject.activeSelf, Is.False,
                "A corner-only plot does not connect to the central oak.");

            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Confirm placement"));
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            controller.ToggleLanguage();
            Assert.That(canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("今日已建 1/1"));
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("住宅贴已建格的上下左右"));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(focus.parent.name, Is.EqualTo("Open build palette"),
                "Pigeon arrival is observed, not a gate for the next room.");
            var storyIcon = canvas.Find(
                "Building choices/Story cue backing/Story pictogram")
                .GetComponent<Image>();
            Assert.That(storyIcon.sprite, Is.EqualTo(canvas.Find(
                "Building choices/Build Residence/Building icon")
                .GetComponent<Image>().sprite));
            controller.ToggleBuildPalette();
            Assert.That(focus.parent.name, Is.EqualTo("Card face"));
            Assert.That(focus.parent.parent.name, Is.EqualTo("Build Residence"));
            canvas.Find("Building choices/Build Residence")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(focus.gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("7 by 7 construction board/Cell 7,7/Buildable hint")
                .gameObject.activeSelf, Is.False,
                "A new home still needs a side-adjacent built room.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 4,3/Buildable hint")
                .gameObject.activeSelf, Is.True,
                "The player may choose any legal neighbouring plot.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 6,5/Buildable hint")
                .gameObject.activeSelf, Is.False,
                "A home may not connect to green through a corner alone.");
            Assert.That(canvas.Find("7 by 7 construction board/Cell 6,4/Buildable hint")
                .gameObject.activeSelf, Is.True,
                "A side-adjacent plot beside the new green remains available.");
            Assert.That(controller.RequestPlacement(3, 2), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Confirm placement"));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeadowBesideAHomeCanStayIsolatedUntilGreenConnects()
        {
            var root = new GameObject("Free tutorial placement test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence),
                Is.True);
            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.RequestPlacement(5, 3), Is.True,
                "The meadow touches the home but remains separate from oak habitat.");
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(3));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen),
                "An isolated meadow should not prematurely attract pigeons.");
            var habitatDeadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < habitatDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            var canvas = root.transform.Find("Construction UI");
            var habitatTooltip = canvas.Find("Room badge tooltip");
            var habitatHeading = habitatTooltip.Find("Tooltip heading").GetComponent<Text>();
            var habitatDetails = habitatTooltip.Find("Tooltip details").GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current);
            var isolatedMeadow = canvas.Find("7 by 7 construction board/Cell 6,4")
                .GetComponent<EventTrigger>();
            isolatedMeadow.OnPointerEnter(pointer);
            Assert.That(habitatTooltip.gameObject.activeSelf, Is.True);
            Assert.That(habitatHeading.text, Does.Contain("Meadow"));
            Assert.That(habitatDetails.text, Does.Contain("seeds"));
            isolatedMeadow.OnPointerExit(pointer);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.RequestPlacement(3, 4), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant),
                "Connected meadow can attract pigeons; the existing home then needs food.");
            var connectedMeadow = canvas.Find("7 by 7 construction board/Cell 4,5")
                .GetComponent<EventTrigger>();
            connectedMeadow.OnPointerEnter(pointer);
            Assert.That(habitatHeading.text, Does.Contain("Meadow"));
            Assert.That(habitatDetails.text, Does.Contain("seeds"),
                "Pigeons can arrive when a young meadow first produces seeds.");
            Assert.That(habitatDetails.preferredHeight,
                Is.LessThanOrEqualTo(habitatDetails.rectTransform.rect.height + 1f));
            connectedMeadow.OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyPlotsNeverDisplayAbsentWildlifeHabitats()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Empty wildlife badge regression test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            Assert.That(run.HedgehogHabitat, Is.Null);
            Assert.That(run.FoxHabitat, Is.Null);
            AssertNoWildlifeBadgesOnEmptyPlots(root, run, 48);

            controller.TryPlace(3, 3); // Inspect the existing oak.
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(run.HedgehogHabitat, Is.Null);
            Assert.That(run.FoxHabitat, Is.Null);
            AssertNoWildlifeBadgesOnEmptyPlots(root, run, 47);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarterOakTeachesHoverAndBadgePositionsWithoutBlockingProgress()
        {
            var root = new GameObject("Starter oak badge lesson test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var story = canvas.Find("Building choices/Story cue backing");
            var preview = story.Find("Story preview").GetComponent<Text>();
            var function = story.Find("Room function").GetComponent<Text>();
            var guide = canvas.Find("Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(preview.text, Does.Contain("下一天"));
            Assert.That(function.text, Does.Contain("角标"));
            Assert.That(guide.text, Is.Not.Empty);

            var oak = canvas.Find("7 by 7 construction board/Cell 4,4")
                .GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            oak.OnPointerEnter(pointer);
            var localTooltip = canvas.Find("Room badge tooltip");
            var heading = localTooltip.Find("Tooltip heading").GetComponent<Text>();
            var details = localTooltip.Find("Tooltip details").GetComponent<Text>();
            Assert.That(localTooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Does.Contain("橡树"));
            Assert.That(details.text, Is.Not.Empty);
            Assert.That(details.text, Does.Not.Contain("左上："),
                "The room summary must not repeat all four badge definitions.");
            Assert.That(canvas.Find("Context tooltip").gameObject.activeSelf,
                Is.False, "Room metrics should use the dedicated nearby tooltip.");
            var fixedCard = canvas.Find("Building choices/Feedback backing");
            Assert.That(fixedCard == null || !fixedCard.gameObject.activeSelf,
                Is.True);
            oak.OnPointerExit(pointer);
            Assert.That(localTooltip.gameObject.activeSelf, Is.False);

            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(heading.text, Does.Contain("橡树"),
                "Clicking an existing room should show its nearby summary.");
            Assert.That(guide.text, Does.Contain("已查看橡树"));
            Assert.That(guide.text, Does.Contain("下一天"));
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            Assert.That(function.preferredHeight,
                Is.LessThanOrEqualTo(function.rectTransform.rect.height + 1f));
            Assert.That(controller.AdvanceDay(), Is.True,
                "Hover is taught without becoming a compulsory extra gate.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SideRailBadgesRevealOnCellHoverAndExplainBesideTheBadge()
        {
            var root = new GameObject("Built room side rails and badge test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var canvas = root.transform.Find("Construction UI");
            var cell = canvas.Find("7 by 7 construction board/Cell 4,4");
            var rails = cell.Find("Metric side rails");
            Assert.That(rails, Is.Not.Null);
            var reveal = rails.GetComponent<CanvasGroup>();
            Assert.That(reveal, Is.Not.Null);
            Assert.That(reveal.alpha, Is.InRange(0.1f, 0.45f),
                "Idle metrics remain faintly discoverable without covering the art.");
            var leftRail = rails.Find("Left metric rail").GetComponent<RectTransform>();
            var rightRail = rails.Find("Right metric rail").GetComponent<RectTransform>();
            Assert.That(leftRail.anchoredPosition.x,
                Is.LessThan(rightRail.anchoredPosition.x));
            Assert.That(leftRail.rect.height,
                Is.EqualTo(rightRail.rect.height).Within(1f),
                "The two information columns should share a top and bottom line.");
            var roomArt = cell.Find("Room icon");
            var squirrelArt = FindCellMarker(cell, "Resident squirrel");
            Assert.That(roomArt.IsChildOf(rails), Is.False,
                "The room illustration must not fade with the metric rails.");
            Assert.That(squirrelArt.IsChildOf(rails), Is.False,
                "The 2D animal fallback must not inherit the metric fade.");
            var cellRect = cell.GetComponent<RectTransform>();
            var corners = new Vector3[4];
            foreach (var rail in new[] { leftRail, rightRail })
            {
                rail.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = cellRect.InverseTransformPoint(corner);
                    Assert.That(Mathf.Abs(local.x),
                        Is.LessThanOrEqualTo(cellRect.rect.width * 0.5f + 1f),
                        "Metric rails should stay within their own room.");
                }
            }
            var tooltip = canvas.Find("Room badge tooltip");
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);
            var heading = tooltip.Find("Tooltip heading").GetComponent<Text>();
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };

            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            var roomHeading = heading.text;
            var roomDetails = details.text;
            Assert.That(roomHeading, Does.Contain("橡树"));
            Assert.That(roomDetails, Is.Not.Empty);
            var fixedCard = canvas.Find("Building choices/Feedback backing");
            Assert.That(fixedCard == null || !fixedCard.gameObject.activeSelf,
                Is.True, "Badge explanations must not use a right-side fixed card.");
            cell.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(reveal.alpha, Is.GreaterThan(0.94f),
                "Hovering the room reveals both information columns.");

            var growth = FindCellMarker(cell, "Plant growth");
            var forecast = FindCellMarker(cell, "Utilization status");
            var stock = FindCellMarker(cell, "Food stock");
            foreach (var badge in new[] { growth, forecast, stock })
            {
                Assert.That(badge.GetComponent<Image>().raycastTarget, Is.True,
                    $"{badge.name} must receive pointer hover.");
                Assert.That(badge.GetComponent<ConstructionRoomBadgeHover>(),
                    Is.Not.Null, $"{badge.name} is missing badge-specific hover.");
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                        badge.gameObject), Is.EqualTo(cell.gameObject),
                    $"{badge.name} must leave the room click reachable.");
            }

            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Does.Contain("生长"));
            Assert.That(details.text, Is.Not.Empty);
            Assert.That(details.text, Does.Not.Contain("食物存量"),
                "One badge should explain one metric.");
            var badgeWorldX = growth.GetComponent<RectTransform>().position.x;
            var tooltipWorldX = tooltip.GetComponent<RectTransform>().position.x;
            Assert.That(Mathf.Abs(tooltipWorldX - badgeWorldX),
                Is.LessThan(330f), "The explanation should appear near its badge.");
            Assert.That(tooltip.GetComponent<Image>().raycastTarget, Is.False,
                "A non-interactive popover must not steal hover from its badge.");
            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerExit(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Is.EqualTo(roomHeading));
            Assert.That(details.text, Is.EqualTo(roomDetails),
                "Leaving the badge restores the selected room summary.");

            stock.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Does.Contain("食物存量"));
            stock.GetComponent<ConstructionRoomBadgeHover>().OnPointerExit(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(details.text, Is.EqualTo(roomDetails));
            cell.GetComponent<EventTrigger>().OnPointerExit(pointer);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(reveal.alpha, Is.InRange(0.1f, 0.45f));
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Does.Contain("GROWTH").IgnoreCase);
            Assert.That(details.text, Is.Not.Empty);
            Assert.That(details.text, Does.Not.Contain("成熟"));
            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerExit(pointer);

            controller.ToggleBuildPalette();
            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True,
                "The build palette occupies the right leaf and must not hide board metric hints.");
            growth.GetComponent<ConstructionRoomBadgeHover>().OnPointerExit(pointer);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisitedSquareShowsSharedFoodAndNearbyStreetSafetyAtTheirRooms()
        {
            var run = ConfirmedEndlessRun();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            void BuildAndAdvance(int column, int row,
                ConstructionCategory category)
            {
                Assert.That(run.TryBuild(column, row, category,
                    GreenPlanting.None, out _, out var failure,
                    out var placement), Is.True,
                    $"{category}: {failure} / {placement}");
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            // The square stays beside the oak, while the home and street
            // connect through shared edges before the first restaurant opens.
            BuildAndAdvance(4, 3, ConstructionCategory.Square);
            BuildAndAdvance(3, 2, ConstructionCategory.Residence);
            BuildAndAdvance(4, 2, ConstructionCategory.Street);
            Assert.That(run.TryBuild(5, 2, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out var mealFailure,
                out var mealPlacement), Is.True,
                $"{mealFailure} / {mealPlacement}");
            var square = run.At(4, 3);
            var street = run.At(4, 2);
            Assert.That(run.ScoringVersion, Is.GreaterThanOrEqualTo(8));
            var preview = run.PreviewHumanDay();
            Assert.That(preview.residents.Any(person =>
                person.linkageSourceTileId == square.id), Is.True,
                $"squirrels={run.SquirrelPopulation} sources={string.Join(",", run.PreviewEcologyDay().scoredFoodTileIds)}; " +
                string.Join(" | ", preview.residents.Select(person =>
                    $"cycle={person.completedCycle} work={person.worked} ate={person.ate} square={person.visitedSquare} squareId={person.squareTileId} link={person.linkageSourceTileId}")));
            Assert.That(run.IsStreetProtectedTomorrow(street.id), Is.True);

            var root = new GameObject("Coexistence badge source test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);
            var canvas = root.transform.Find("Construction UI");
            var board = canvas.Find("7 by 7 construction board");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var minColumn = (int)typeof(ConstructionPrototypeController)
                .GetField("viewMinColumn", flags).GetValue(controller);
            var minRow = (int)typeof(ConstructionPrototypeController)
                .GetField("viewMinRow", flags).GetValue(controller);
            Transform CellFor(ConstructionTileData tile) => board.Find(
                $"Cell {tile.column - minColumn + 1},{tile.row - minRow + 1}");
            var squareCell = CellFor(square);
            var streetCell = CellFor(street);
            var homeCell = CellFor(run.At(3, 2));
            var squareMark = squareCell.Find("Metric side rails/Coexistence link");
            var streetMark = streetCell.Find("Metric side rails/Coexistence link");
            var homeMark = homeCell.Find("Metric side rails/Coexistence link");
            Assert.That(squareMark.gameObject.activeSelf, Is.True);
            Assert.That(streetMark.gameObject.activeSelf, Is.True);
            Assert.That(homeMark.gameObject.activeSelf, Is.True);
            Assert.That(squareMark.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                squareMark.gameObject), Is.EqualTo(squareCell.gameObject));

            var pointer = new PointerEventData(EventSystem.current);
            var tooltip = canvas.Find("Room badge tooltip");
            var heading = tooltip.Find("Tooltip heading").GetComponent<Text>();
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            squareMark.GetComponent<ConstructionRoomBadgeHover>()
                .OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(heading.text, Does.Contain("共享食源"));
            Assert.That(details.text.Replace(" ", ""),
                Does.Contain("居民与附近动物").And.Contain("结算后显示"));
            Assert.That(details.text, Does.Not.Contain("+4").And.Not.Contain("+3"),
                "A pre-settlement tooltip must not reveal the exact score.");
            Assert.That(squareCell.Find("Coexistence neighbours").gameObject.activeSelf,
                Is.True);
            Assert.That(homeCell.Find("Coexistence neighbours").gameObject.activeSelf,
                Is.True);
            squareMark.GetComponent<ConstructionRoomBadgeHover>()
                .OnPointerExit(pointer);

            streetMark.GetComponent<ConstructionRoomBadgeHover>()
                .OnPointerEnter(pointer);
            Assert.That(heading.text, Does.Contain("共栖安全"));
            Assert.That(details.text, Does.Contain("1/16").And.Contain("1/32"));
            Assert.That(streetCell.Find("Coexistence neighbours").gameObject.activeSelf,
                Is.True);
            Assert.That(squareCell.Find("Coexistence neighbours").gameObject.activeSelf,
                Is.True);
            streetMark.GetComponent<ConstructionRoomBadgeHover>()
                .OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoveringOneBuiltRoomRevealsOtherRoomBadgesForComparison()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            var root = new GameObject("Compare room badges on hover");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var board = root.transform.Find(
                "Construction UI/7 by 7 construction board");
            var oak = board.Find("Cell 4,4");
            var meadow = board.Find("Cell 5,4");
            var oakMetrics = oak.Find("Metric side rails")
                .GetComponent<CanvasGroup>();
            var meadowMetrics = meadow.Find("Metric side rails")
                .GetComponent<CanvasGroup>();
            Assert.That(oakMetrics.alpha, Is.InRange(0.1f, 0.45f));
            Assert.That(meadowMetrics.alpha, Is.InRange(0.1f, 0.45f));

            var pointer = new PointerEventData(EventSystem.current);
            meadow.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(meadowMetrics.alpha, Is.GreaterThan(0.94f));
            Assert.That(oakMetrics.alpha, Is.GreaterThan(0.94f),
                "Other built rooms must reveal their badges for comparison.");

            meadow.GetComponent<EventTrigger>().OnPointerExit(pointer);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(meadowMetrics.alpha, Is.InRange(0.1f, 0.45f));
            Assert.That(oakMetrics.alpha, Is.InRange(0.1f, 0.45f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EdgeRoomBadgeTooltipStaysInsideTheBoard()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var column in new[] { 2, 1, 0 })
            {
                Assert.That(run.TryBuild(column, 3, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var root = new GameObject("Construction edge badge placement test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);
            var canvas = root.transform.Find("Construction UI");
            var board = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            var edgeCell = canvas.Find("7 by 7 construction board/Cell 1,4");
            var badge = FindCellMarker(edgeCell, "Plant growth");
            Assert.That(badge, Is.Not.Null);
            badge.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(
                new PointerEventData(EventSystem.current));
            var tooltip = canvas.Find("Room badge tooltip")
                .GetComponent<RectTransform>();
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            var boardCorners = new Vector3[4];
            var tooltipCorners = new Vector3[4];
            board.GetWorldCorners(boardCorners);
            tooltip.GetWorldCorners(tooltipCorners);
            Assert.That(tooltipCorners[0].x,
                Is.GreaterThanOrEqualTo(boardCorners[0].x - 1f));
            Assert.That(tooltipCorners[2].x,
                Is.LessThanOrEqualTo(boardCorners[2].x + 1f));
            Assert.That(tooltipCorners[0].y,
                Is.GreaterThanOrEqualTo(boardCorners[0].y - 1f));
            Assert.That(tooltipCorners[2].y,
                Is.LessThanOrEqualTo(boardCorners[2].y + 1f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TutorialTooltipsMatchAvailableActionsAndLeaveDayButtonClear()
        {
            var root = new GameObject("Construction tutorial action hints test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun());
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var tooltip = canvas.Find("Context tooltip");
            var fixedCard = canvas.Find("Building choices/Feedback backing");
            var roomTooltip = canvas.Find("Room badge tooltip");
            var roomDetails = roomTooltip.Find("Tooltip details").GetComponent<Text>();
            var emptyPlot = canvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            emptyPlot.OnPointerEnter(pointer);
            Assert.That(roomTooltip.gameObject.activeSelf, Is.True);
            Assert.That(roomDetails.text, Does.Contain("Building starts on Day 2"),
                "The first-day empty plot must not promise an unavailable build.");
            Assert.That(fixedCard.gameObject.activeSelf, Is.False);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);
            emptyPlot.OnPointerExit(pointer);

            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(controller.AdvanceDay(), Is.True);
            var heading = canvas.Find("Building choices/Next step backing/Next step heading")
                .GetComponent<Text>();
            var guide = canvas.Find("Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            var story = canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            Assert.That(heading.text, Is.EqualTo("CURRENT BUILD"));
            Assert.That(guide.text, Does.Contain("Build one room of your choice today"),
                "No room type should be selected before the player chooses one.");
            Assert.That(story.text, Does.Contain("starter oak is not a new room"));
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(story.text, Does.Contain("初始橡树不算新建房间"));
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();

            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            Assert.That(guide.text, Does.Contain("Selected: Green"),
                "The selected type is Green; its vegetation is resolved by the plot.");
            Assert.That(guide.text, Does.Not.Contain("Pigeons need grass seeds"));
            emptyPlot.OnPointerEnter(pointer);
            Assert.That(roomDetails.text, Does.Contain("Click to preview"));
            emptyPlot.OnPointerExit(pointer);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(heading.text, Is.EqualTo("NEXT STEP"));
            var unbuiltPlot = canvas.Find("7 by 7 construction board/Cell 3,4")
                .GetComponent<EventTrigger>();
            unbuiltPlot.OnPointerEnter(pointer);
            Assert.That(roomDetails.text, Does.Contain("One room has been built today"));
            unbuiltPlot.OnPointerExit(pointer);

            var cardRect = roomTooltip.GetComponent<RectTransform>();
            var boardRect = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            var nextDayRect = canvas.Find("Building choices/Advance to next day")
                .GetComponent<RectTransform>();
            Assert.That(cardRect.anchoredPosition.x + cardRect.rect.width * 0.5f,
                Is.LessThanOrEqualTo(boardRect.anchoredPosition.x +
                    boardRect.rect.width * 0.5f + 1f),
                "The nearby tooltip stays on the board, away from Next Day.");
            Assert.That(nextDayRect, Is.Not.Null);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearbyRoomTooltipKeepsBodyBelowDividerAndInsideCard()
        {
            var root = new GameObject("Nearby empty plot tooltip geometry test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var canvas = root.transform.Find("Construction UI");
            var tooltip = canvas.Find("Room badge tooltip");
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            var empty = canvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            empty.OnPointerEnter(pointer);
            Assert.That(details.text, Does.Contain("第2天开始建造"));
            AssertNearbyTooltipTextFits(tooltip, "Chinese empty plot");
            empty.OnPointerExit(pointer);
            controller.ToggleLanguage();
            empty.OnPointerEnter(pointer);
            Assert.That(details.text, Does.Contain("Building starts on Day 2"));
            AssertNearbyTooltipTextFits(tooltip, "English first-day empty plot");
            empty.OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;

            var homeRun = LegacyFillRun();
            Assert.That(homeRun.TrySimulateDay(out _), Is.True);
            Assert.That(homeRun.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            root = new GameObject("Nearby long room tooltip geometry test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(homeRun);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            canvas = root.transform.Find("Construction UI");
            tooltip = canvas.Find("Room badge tooltip");
            details = tooltip.Find("Tooltip details").GetComponent<Text>();
            var home = canvas.Find("7 by 7 construction board/Cell 4,3");
            home.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            AssertNearbyTooltipTextFits(tooltip, "English residence summary");
            foreach (var badgeName in new[] { "Home bonus", "Utilization status" })
            {
                var badge = FindCellMarker(home, badgeName);
                Assert.That(badge.gameObject.activeInHierarchy, Is.True);
                badge.GetComponent<ConstructionRoomBadgeHover>().OnPointerEnter(pointer);
                Assert.That(details.text.Length, Is.GreaterThan(75),
                    $"{badgeName} should exercise a genuinely long explanation.");
                AssertNearbyTooltipTextFits(tooltip,
                    $"English residence {badgeName}");
                badge.GetComponent<ConstructionRoomBadgeHover>().OnPointerExit(pointer);
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChineseStoryCuesStayReadableThroughTheFullTutorial()
        {
            var root = new GameObject("Chinese construction tutorial test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            // This visual regression intentionally exercises a version-zero
            // saved tutorial; current linear onboarding is covered by
            // ConstructionIntroChoicePresentationTests.
            var run = LegacyFillRun();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var openingDeadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < openingDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True,
                "The tutorial focus is only visible after the board case opens.");
            AssertStoryPresentationFits(controller);
            controller.ToggleLanguage();
            AssertStoryPresentationFits(controller);
            controller.ToggleLanguage();
            controller.TryPlace(3, 3);
            AssertStoryPresentationFits(controller);
            Assert.That(controller.AdvanceDay(), Is.True);
            AssertStoryPresentationFits(controller);
            var canvas = root.transform.Find("Construction UI");
            var storyPreview = canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            var roomFunction = canvas.Find("Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            Assert.That(storyPreview.text, Does.Contain("还没有新建绿地"));
            Assert.That(roomFunction.text, Does.Contain("次日引来鸽子")
                .And.Contain("隔日有产食机会"));
            Assert.That(roomFunction.GetComponent<CanvasGroup>(), Is.Null,
                "The room function must remain visible without hovering.");

            PlaceAndAdvance(controller, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(storyPreview.text, Does.Contain("住宅贴已建格的上下左右"));
            Assert.That(roomFunction.text, Does.Contain("完整循环"));
            AssertStoryPresentationFits(controller);
            PlaceAndAdvance(controller, 3, 2, ConstructionCategory.Residence);
            Assert.That(storyPreview.text, Does.Contain("餐厅"));
            Assert.That(roomFunction.text,
                Does.Contain("2餐位").And.Contain("1岗位"));
            PlaceAndAdvance(controller, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(storyPreview.text, Does.Contain("工作坊"));
            Assert.That(roomFunction.text, Does.Contain("居民调度"));
            PlaceAndAdvance(controller, 2, 2, ConstructionCategory.Workshop);
            var workshopCell = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 3,3");
            Assert.That(FindCellMarker(workshopCell.transform, "Work cycle/Work cycle value")
                .GetComponent<Text>().text, Is.Not.EqualTo("✓"),
                "A built workshop is not automatically a complete resident cycle.");
            Assert.That(RoomBadgeDetails(root, run.At(2, 2), "Work cycle"),
                Does.Contain("员工上班并用餐"),
                "The work-cycle corner should explain its own symbols.");
            Assert.That(storyPreview.text, Does.Contain("街道"));
            Assert.That(roomFunction.text, Does.Contain("连续街道"));
            controller.ToggleLanguage();
            AssertStoryPresentationFits(controller);
            controller.ToggleLanguage();
            Assert.That(controller.TrySelect(ConstructionCategory.Street), Is.True);
            var focus = root.GetComponentsInChildren<RectTransform>(true)
                .Single(rect => rect.name == "Tutorial focus");
            Assert.That(focus.gameObject.activeSelf, Is.False,
                "Street routing should be chosen by the player, not a single frame.");
            PlaceAndAdvance(controller, 4, 2, ConstructionCategory.Street);
            var streetCell = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 5,3");
            var deaths = FindCellMarker(streetCell.transform,
                "Traffic deaths/Traffic deaths value").GetComponent<Text>();
            Assert.That(int.Parse(deaths.text),
                Is.EqualTo(run.StreetTrafficDeathCount(run.At(4, 2).id)));
            var exposed = FindCellMarker(streetCell.transform,
                "Roadside wildlife/Roadside wildlife value").GetComponent<Text>();
            Assert.That(int.Parse(exposed.text), Is.GreaterThan(0));
            var street = run.At(4, 2);
            var deathInfo = RoomBadgeDetails(root, street, "Traffic deaths");
            var exposureInfo = RoomBadgeDetails(root, street, "Roadside wildlife");
            Assert.That(deathInfo, Does.Contain("累计造成"));
            Assert.That(exposureInfo, Does.Contain("车祸风险"));
            controller.ToggleLanguage();
            Assert.That(RoomBadgeDetails(root, street, "Traffic deaths"),
                Does.Contain("wildlife deaths"));
            Assert.That(RoomBadgeDetails(root, street, "Roadside wildlife"),
                Does.Contain("traffic risk"));
            controller.ToggleLanguage();
            Assert.That(storyPreview.text, Does.Contain("垃圾"));
            Assert.That(roomFunction.text, Does.Contain("清运"));
            PlaceAndAdvance(controller, 2, 1, ConstructionCategory.Waste);
            Assert.That(FindCellMarker(workshopCell.transform, "Work cycle/Work cycle value")
                .GetComponent<Text>().text, Is.EqualTo("✓"),
                "Cleanup should let the assigned resident complete work and a meal.");
            Assert.That(storyPreview.text, Does.Contain("超市"));
            Assert.That(roomFunction.text, Does.Contain("2 餐位").And
                .Contain("1 岗位").And.Contain("动物食源"));
            PlaceAndAdvance(controller, 4, 1, ConstructionCategory.Supermarket);
            Assert.That(storyPreview.text, Does.Contain("广场"));
            Assert.That(roomFunction.text, Does.Contain("广场"),
                "The final room category must be built before open-ended play.");
            PlaceAndAdvance(controller, 5, 1, ConstructionCategory.Square);
            Assert.That(canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>().text, Is.Not.Empty,
                "Free planning should show the current need after all eight rooms.");
            Assert.That(canvas.Find("Building choices/Next step backing/Placement guide")
                .GetComponent<Text>().text, Does.Contain("今天还能自行选建一格"));
            Assert.That(focus.gameObject.activeSelf, Is.False);
            AssertStoryPresentationFits(controller);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IconSummaryAndHoverRevealDetailsWithoutCoveringBoard()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Construction hover UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var story = canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            var preview = canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            Assert.That(preview.text.Length, Is.LessThan(story.text.Length));
            var metric = canvas.Find("Building choices/Story cue backing/Animal score metric");
            Assert.That(metric.Find("Value").GetComponent<Text>().text,
                Is.EqualTo(run.AnimalScore.ToString()));
            var tooltip = canvas.Find("Context tooltip");
            var plaque = canvas.Find("Score plaque");
            Assert.That(plaque, Is.Not.Null);
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            metric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.True);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);
            Assert.That(plaque.Find("Plaque face/Score title")
                .GetComponent<Text>().text, Does.Contain("ANIMAL").IgnoreCase);
            Assert.That(plaque.Find("Plaque face/Score points")
                .GetComponent<Text>().text, Is.EqualTo(run.AnimalScore.ToString()));
            Assert.That(plaque.Find("Plaque face/Comparison summary")
                .GetComponent<Text>().text, Does.Contain(run.HumanScore.ToString()));
            Assert.That(plaque.Find("Plaque face/Scoring rule")
                .GetComponent<Text>().text, Does.Contain("+3"));
            foreach (var name in new[] { "Squirrel", "Pigeon", "Hedgehog", "Meal" })
                Assert.That(ScoreForecastText(plaque),
                    Does.Contain(name).IgnoreCase);
            AssertScorePlaqueTextFits(plaque);
            var boardRect = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            var leafRect = canvas.Find("Observation frame")
                .GetComponent<RectTransform>();
            var expectedLeafCenter = -(boardRect.rect.width *
                boardRect.localScale.x + leafRect.rect.width) * 0.5f;
            Assert.That(plaque.GetComponent<RectTransform>().anchoredPosition.x,
                Is.EqualTo(expectedLeafCenter).Within(0.01f));
            metric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.False);

            controller.ToggleLanguage();
            metric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(plaque.Find("Plaque face/Score title")
                .GetComponent<Text>().text, Does.Contain("动物"));
            Assert.That(plaque.Find("Plaque face/Scoring rule")
                .GetComponent<Text>().text, Does.Contain("绿地"));
            Assert.That(plaque.Find("Plaque face/Scoring rule")
                .GetComponent<Text>().text, Does.Contain("附近食源").And
                    .Contain("广场+4"));
            foreach (var name in new[] { "松鼠", "鸽", "刺猬", "进食" })
                Assert.That(ScoreForecastText(plaque), Does.Contain(name));
            AssertScorePlaqueTextFits(plaque);
            metric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.False);

            var starter = canvas.Find("7 by 7 construction board/Cell 4,4");
            starter.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            var roomCard = canvas.Find("Room badge tooltip");
            Assert.That(roomCard.gameObject.activeSelf, Is.True);
            Assert.That(roomCard.Find("Tooltip heading").GetComponent<Text>().text,
                Does.Contain("橡树"));
            Assert.That(roomCard.Find("Tooltip details").GetComponent<Text>().text,
                Is.Not.Empty);
            Assert.That(tooltip.gameObject.activeSelf, Is.False,
                "Built-room details belong beside the room.");
            Assert.That(plaque.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidentScoreHoverUsesTheSharedBilingualPlaqueAndLocalRoomTooltip()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Resident score plaque test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var metric = canvas.Find("Building choices/Story cue backing/Work cycles metric");
            var plaque = canvas.Find("Score plaque");
            var standard = canvas.Find("Context tooltip");
            Assert.That(plaque, Is.Not.Null);
            Assert.That(plaque.GetComponent<RectTransform>().rect.width,
                Is.LessThan(382f), "The score card must fit the left case leaf.");
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            metric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.True);
            Assert.That(standard.gameObject.activeSelf, Is.False);
            Assert.That(plaque.Find("Plaque face/Score title")
                .GetComponent<Text>().text, Does.Contain("RESIDENT").IgnoreCase);
            Assert.That(plaque.Find("Plaque face/Score points")
                .GetComponent<Text>().text, Is.EqualTo(run.HumanScore.ToString()));
            Assert.That(plaque.Find("Plaque face/Comparison summary")
                .GetComponent<Text>().text, Does.Contain(run.AnimalScore.ToString()));
            Assert.That(plaque.Find("Plaque face/Scoring rule")
                .GetComponent<Text>().text, Does.Contain("+"));
            foreach (var name in new[] { "Work", "Meal", "Cycle", "Plaza" })
                Assert.That(ScoreForecastText(plaque),
                    Does.Contain(name).IgnoreCase);
            AssertScorePlaqueTextFits(plaque);
            metric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.False);

            controller.ToggleLanguage();
            metric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(plaque.Find("Plaque face/Score eyebrow")
                .GetComponent<Text>().text, Is.EqualTo("累计得分"));
            Assert.That(plaque.Find("Plaque face/Score title")
                .GetComponent<Text>().text, Does.Contain("居民"));
            Assert.That(plaque.Find("Plaque face/Forecast heading")
                .GetComponent<Text>().text, Is.EqualTo("当前状态"));
            Assert.That(plaque.Find("Plaque face/Scoring rule")
                .GetComponent<Text>().text, Does.Contain("循环"));
            foreach (var name in new[] { "上班", "用餐", "循环", "广场" })
                Assert.That(ScoreForecastText(plaque), Does.Contain(name));
            AssertScorePlaqueTextFits(plaque);
            Assert.That(standard.gameObject.activeSelf, Is.False);
            metric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.False);

            var starter = canvas.Find("7 by 7 construction board/Cell 4,4");
            starter.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(standard.gameObject.activeSelf, Is.False,
                "Room hover should use the local room tooltip.");
            var roomTooltip = canvas.Find("Room badge tooltip");
            Assert.That(roomTooltip.gameObject.activeSelf, Is.True);
            Assert.That(roomTooltip.Find("Tooltip heading")
                .GetComponent<Text>().text, Does.Contain("橡树"));
            Assert.That(plaque.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SharedScorePlaqueHasAngledGradientAndFitsFourLateGameForecasts()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 1, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PreviewEcologyDay().hedgehogPresent, Is.True);

            var root = new GameObject("Late habitat score plaque test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var animalMetric = canvas.Find("Building choices/Story cue backing/Animal score metric");
            var residentMetric = canvas.Find("Building choices/Story cue backing/Work cycles metric");
            var plaque = canvas.Find("Score plaque");
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            animalMetric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            var picture = plaque.GetComponent<Image>().sprite.texture;
            Assert.That(picture.width, Is.EqualTo(692));
            Assert.That(picture.height, Is.InRange(356, 480),
                "The score plaque must fit its forecast without becoming oversized.");
            Assert.That(picture.GetPixel(0, 0).a, Is.EqualTo(0f));
            Assert.That(picture.GetPixel(picture.width - 1,
                picture.height - 1).a, Is.EqualTo(0f));
            Assert.That(picture.GetPixel(0, picture.height - 1).a,
                Is.EqualTo(1f));
            Assert.That(picture.GetPixel(691, 0).a, Is.EqualTo(1f));
            Assert.That(picture.GetPixel(40, picture.height - 46).r,
                Is.GreaterThan(picture.GetPixel(650, 40).r),
                "The plaque surface needs a visible navy gradient.");

            var face = plaque.Find("Plaque face");
            var labels = new[] { "Forecast slot 1", "Forecast slot 2",
                "Forecast slot 3", "Forecast slot 4" }
                .Select(name => face.Find(name).GetComponent<Text>()).ToArray();
            Assert.That(labels[2].text, Does.Contain("Hedgehog"));
            AssertScorePlaqueTextFits(plaque);
            for (var i = 0; i < labels.Length; i++)
            {
                var rect = labels[i].rectTransform;
                if (i == 0) continue;
                var previous = labels[i - 1].rectTransform;
                Assert.That(rect.anchoredPosition.x - rect.rect.width * 0.5f,
                    Is.GreaterThanOrEqualTo(
                        previous.anchoredPosition.x +
                        previous.rect.width * 0.5f),
                    $"{labels[i].name} must not overlap its neighbour.");
            }
            var sharedSprite = plaque.GetComponent<Image>().sprite;
            var sharedPosition = plaque.GetComponent<RectTransform>().anchoredPosition;
            animalMetric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            residentMetric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(plaque.gameObject.activeSelf, Is.True);
            Assert.That(plaque.GetComponent<Image>().sprite, Is.SameAs(sharedSprite));
            Assert.That(plaque.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(sharedPosition));
            Assert.That(face.Find("Score points").GetComponent<Text>().text,
                Is.EqualTo(run.HumanScore.ToString()));
            AssertScorePlaqueTextFits(plaque);
            Object.Destroy(root);
            yield return null;
        }

        private static string ScoreForecastText(Transform plaque) =>
            string.Join(" · ", Enumerable.Range(1, 4).Select(index =>
                plaque.Find($"Plaque face/Forecast slot {index}")
                    .GetComponent<Text>().text));

        private static void AssertScorePlaqueTextFits(Transform plaque)
        {
            var face = plaque.Find("Plaque face");
            foreach (var name in new[] { "Score eyebrow", "Score title",
                "Score points", "Points unit", "Comparison summary",
                "Scoring rule", "Forecast heading", "Forecast slot 1",
                "Forecast slot 2", "Forecast slot 3", "Forecast slot 4" })
            {
                var label = face.Find(name)?.GetComponent<Text>();
                Assert.That(label, Is.Not.Null, $"Missing {name} on score plaque.");
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{name} text is clipped: {label.text}");
            }
        }

        [UnityTest]
        public IEnumerator PlayerCanChooseBuildingWhilePlacementAndDailyLimitsStillApply()
        {
            var root = new GameObject("Construction free choice test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            controller.ToggleBuildPalette();
            var canvas = root.transform.Find("Construction UI");
            var residence = canvas.Find("Building choices/Build Residence")
                .GetComponent<Button>();
            var status = residence.transform.Find("Unlock status")
                .GetComponent<Text>();
            Assert.That(residence.interactable, Is.True,
                "The first free construction day must offer a residence.");
            Assert.That(status.text, Does.Contain("plots"));
            var restaurant = canvas.Find("Building choices/Build Restaurant")
                .GetComponent<Button>();
            Assert.That(restaurant.transform.Find("Unlock status")
                .GetComponent<Text>().text, Does.Contain("Needs home/linked road"));
            restaurant.onClick.Invoke();
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("0 valid plot"));
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("share an edge"));
            controller.ToggleBuildPalette();
            residence.onClick.Invoke();
            Assert.That(canvas.Find("Building choices/Build palette backing")
                .gameObject.activeSelf, Is.True,
                "Residence selection shows its automatic scoring conditions.");
            Assert.That(canvas.Find("Building choices/Residence condition Garden")
                .GetComponent<Button>(), Is.Null,
                "A housing reward is not a prebuild selection.");
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("Selected: Residence"));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True,
                "A residence is allowed before the pigeon lesson.");
            Assert.That(canvas.Find("Building choices/Build palette backing")
                .gameObject.activeSelf, Is.False,
                "After placing the selected home, the palette closes.");
            Assert.That(controller.BuiltCount, Is.EqualTo(2),
                "A player-led residence build must count immediately.");
            Assert.That(controller.TrySelect(ConstructionCategory.Square), Is.True,
                "Players may already choose tomorrow's building.");
            Assert.That(controller.TryPlace(3, 4), Is.False,
                "Choosing tomorrow's type must not bypass one build per day.");
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("Built 1/1 today"));
            var squareFace = canvas.Find("Building choices/Build Square/Card face")
                .GetComponent<Image>();
            var selectedColor = squareFace.color;
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(BuildableAt(root, 3, 4), Is.False,
                "Yesterday's placement hints must clear when a new day begins.");
            Assert.That(squareFace.color, Is.Not.EqualTo(selectedColor),
                "Yesterday's selected building must lose its palette highlight.");
            Assert.That(controller.TryPlace(3, 4), Is.False,
                "A new day needs an explicit building choice.");
            Assert.That(controller.TrySelect(ConstructionCategory.Square), Is.True);
            Assert.That(squareFace.color, Is.EqualTo(selectedColor));
            Assert.That(BuildableAt(root, 3, 4), Is.True);
            Assert.That(controller.TryPlace(3, 4), Is.True,
                "Selecting again restores today's placement hints.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildChoicesShowDailyProvisionAndExplainConditions()
        {
            var root = new GameObject("Construction daily provision choices test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            controller.ToggleBuildPalette();
            var canvas = root.transform.Find("Construction UI");
            var choices = canvas.Find("Building choices");
            var tooltip = canvas.Find("Context tooltip/Tooltip details")
                .GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(1200f, 500f) };
            foreach (var (category, brief, detail) in new[]
            {
                ("Green", "食源随地块变化", "放置："),
                ("Residence", "居民 1 人", "容纳1人"),
                ("Restaurant", "餐食2·岗位1", "每日 1 岗位、2 餐位"),
                ("Workshop", "岗位 2 个/日", "每天最多提供2个岗位"),
                ("Waste", "清运至多2/日", "每天最多清运"),
                ("Street", "连通出行路线", "自身不直接产食"),
                ("Supermarket", "餐食2·岗位1", "每日 1 岗位、2 餐位"),
                ("Square", "到访激活食源", "动物+4")
            })
            {
                var button = choices.Find($"Build {category}");
                var provision = button.Find("Daily provision").GetComponent<Text>();
                Assert.That(provision.text, Is.EqualTo(brief), category);
                Assert.That(provision.preferredWidth,
                    Is.LessThanOrEqualTo(provision.rectTransform.rect.width + 1f),
                    $"{category} card provision is clipped: {provision.text}");
                button.GetComponent<EventTrigger>().OnPointerEnter(pointer);
                Assert.That(tooltip.text.Replace(" ", ""),
                    Does.Contain(detail.Replace(" ", "")), category);
                Assert.That(tooltip.preferredHeight,
                    Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f),
                    $"{category} tooltip is clipped: {tooltip.text}");
                AssertNoPunctuationOnlyTooltipLine(tooltip, category);
                button.GetComponent<EventTrigger>().OnPointerExit(pointer);
            }
            controller.ToggleLanguage();
            Assert.That(choices.Find("Build Restaurant/Daily provision")
                .GetComponent<Text>().text, Is.EqualTo("Meals 2 · job 1"));
            foreach (var category in new[] { "Green", "Residence", "Restaurant",
                         "Workshop", "Waste", "Street", "Supermarket", "Square" })
            {
                var button = choices.Find($"Build {category}");
                var provision = button.Find("Daily provision").GetComponent<Text>();
                Assert.That(provision.preferredWidth,
                    Is.LessThanOrEqualTo(provision.rectTransform.rect.width + 1f),
                    $"{category} English card provision is clipped: {provision.text}");
                button.GetComponent<EventTrigger>().OnPointerEnter(pointer);
                Assert.That(tooltip.preferredHeight,
                    Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f),
                    $"{category} English tooltip is clipped: {tooltip.text}");
                button.GetComponent<EventTrigger>().OnPointerExit(pointer);
            }
            var meadow = choices.Find("Build Green").GetComponent<EventTrigger>();
            meadow.OnPointerEnter(pointer);
            Assert.That(tooltip.text, Does.Contain("PLACE:").And
                .Contain("TERRAIN:").And.Contain("WILDLIFE:"));
            Assert.That(tooltip.text, Does.Contain("meadow").And.Contain("pigeon"));
            Assert.That(tooltip.preferredHeight,
                Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f));
            meadow.OnPointerExit(pointer);
            controller.ToggleLanguage();
            controller.ToggleBuildPalette();
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            var empty = canvas.Find("7 by 7 construction board/Cell 5,4")
                .GetComponent<EventTrigger>();
            empty.OnPointerEnter(pointer);
            var plotHint = canvas.Find("Room badge tooltip/Tooltip details")
                .GetComponent<Text>();
            Assert.That(plotHint.text, Does.Contain("这里会形成草地"),
                "The selected green plot should preview its exact terrain.");
            Assert.That(plotHint.text, Does.Contain("鸽子"));
            Assert.That(plotHint.preferredHeight,
                Is.LessThanOrEqualTo(plotHint.rectTransform.rect.height + 1f));
            empty.OnPointerExit(pointer);
            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            var confirmation = choices.Find("Placement confirmation/Confirmation location")
                .GetComponent<Text>();
            Assert.That(confirmation.text,
                Does.Contain("隔日限2份").And.Contain("第10天起80%"));
            Assert.That(confirmation.preferredHeight,
                Is.LessThanOrEqualTo(confirmation.rectTransform.rect.height + 1f),
                $"Build confirmation is clipped: {confirmation.text}");
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            controller.ToggleBuildPalette();
            meadow.OnPointerEnter(pointer);
            var completedGreen = tooltip.text;
            Assert.That(completedGreen, Does.Contain("放置：").And
                .Contain("地形：").And.Contain("动物："));
            Assert.That(completedGreen.IndexOf("放置：", StringComparison.Ordinal),
                Is.LessThan(completedGreen.IndexOf("地形：", StringComparison.Ordinal)));
            Assert.That(completedGreen.IndexOf("地形：", StringComparison.Ordinal),
                Is.LessThan(completedGreen.IndexOf("动物：", StringComparison.Ordinal)));
            Assert.That(completedGreen, Does.Contain("草地").And.Contain("鸽子"));
            Assert.That(completedGreen, Does.Not.Contain("80%").And
                .Not.Contain("满库存").And.Not.Contain("居民联动"));
            Assert.That(tooltip.preferredHeight,
                Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f));
            meadow.OnPointerExit(pointer);
            controller.ToggleLanguage();
            meadow.OnPointerEnter(pointer);
            var englishGreen = tooltip.text;
            Assert.That(englishGreen, Does.Contain("PLACE:").And
                .Contain("TERRAIN:").And.Contain("WILDLIFE:"));
            Assert.That(englishGreen.IndexOf("PLACE:", StringComparison.Ordinal),
                Is.LessThan(englishGreen.IndexOf("TERRAIN:", StringComparison.Ordinal)));
            Assert.That(englishGreen.IndexOf("TERRAIN:", StringComparison.Ordinal),
                Is.LessThan(englishGreen.IndexOf("WILDLIFE:", StringComparison.Ordinal)));
            Assert.That(englishGreen, Does.Contain("meadow").And.Contain("pigeon"));
            Assert.That(englishGreen, Does.Not.Contain("80%").And
                .Not.Contain("stock limit").And.Not.Contain("resident linkage"));
            Assert.That(tooltip.preferredHeight,
                Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f));
            meadow.OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreenPlotHoverPreviewsTerrainFromItsActualNeighbours()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.ResolveGreenPlanting(4, 2), Is.EqualTo(GreenPlanting.Meadow),
                "A neighbouring residence takes precedence over a green plot.");
            Assert.That(run.ResolveGreenPlanting(5, 3), Is.EqualTo(GreenPlanting.Oak),
                "A plot touching only meadow should become oak.");

            var root = new GameObject("Green plot terrain hover test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForBoardCaseOpen(controller);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            var canvas = root.transform.Find("Construction UI");
            var plotHint = canvas.Find("Room badge tooltip/Tooltip details")
                .GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            var besideResidence = canvas.Find("7 by 7 construction board/Cell 5,3")
                .GetComponent<EventTrigger>();
            besideResidence.OnPointerEnter(pointer);
            Assert.That(plotHint.text, Does.Contain("这里会形成草地").And
                .Contain("鸽子"));
            besideResidence.OnPointerExit(pointer);
            var besideMeadow = canvas.Find("7 by 7 construction board/Cell 6,4")
                .GetComponent<EventTrigger>();
            besideMeadow.OnPointerEnter(pointer);
            Assert.That(plotHint.text, Does.Contain("这里会形成橡树").And
                .Contain("松鼠"));
            besideMeadow.OnPointerExit(pointer);

            controller.ToggleLanguage();
            besideResidence.OnPointerEnter(pointer);
            Assert.That(plotHint.text, Does.Contain("becomes meadow").And
                .Contain("pigeons"));
            besideResidence.OnPointerExit(pointer);
            besideMeadow.OnPointerEnter(pointer);
            Assert.That(plotHint.text, Does.Contain("becomes oak").And
                .Contain("squirrel"));
            besideMeadow.OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SquareCardAndBadgesExplainVisitsAndTemporaryFood()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Square,
                GreenPlanting.None, out _, out _, out _), Is.True);

            var root = new GameObject("Square status presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            var canvas = root.transform.Find("Construction UI");
            var square = canvas.Find("7 by 7 construction board/Cell 5,4");
            Assert.That(FindCellMarker(square, "Food stock/Food count").GetComponent<Text>().text,
                Is.EqualTo("+0"));
            Assert.That(FindCellMarker(square, "Utilization status/Use count")
                .GetComponent<Text>().text, Is.EqualTo("0/1"));
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(800f, 500f) };
            square.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            var card = canvas.Find("Room badge tooltip");
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(card.Find("Tooltip heading").GetComponent<Text>().text,
                Does.Contain("广场"));
            Assert.That(card.Find("Tooltip details").GetComponent<Text>().text,
                Does.Contain("食源"));
            Assert.That(canvas.Find("Context tooltip").gameObject.activeSelf,
                Is.False);
            square.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(RoomBadgeDetails(root, run.At(4, 3), "Utilization status"),
                Does.Contain("居民到访"));
            Assert.That(RoomBadgeDetails(root, run.At(4, 3), "Food stock"),
                Does.Contain("到访后"));
            var shared = canvas.GetComponentsInChildren<Text>(true)
                .First(text => text.gameObject.name == "Balance score");
            Assert.That(shared.text, Is.EqualTo(
                $"共栖 {run.BalanceScore} = 动物 {run.AnimalScore} + 居民 {run.HumanScore}"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuiltTilesKeepProvisionBriefSeparateFromBadgeExplanations()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 2, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Supermarket,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 4, ConstructionCategory.Waste,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);

            var root = new GameObject("Construction per-tile daily provision test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            var canvas = root.transform.Find("Construction UI");
            var board = canvas.Find("7 by 7 construction board");
            var card = canvas.Find("Room badge tooltip");
            var heading = card.Find("Tooltip heading").GetComponent<Text>();
            var details = card.Find("Tooltip details")
                .GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(800f, 500f) };
            foreach (var (cell, column, row, provision, badge, meaning) in new[]
            {
                ("Cell 4,4", 3, 3, "橡果", "Plant growth", "成熟后可产食"),
                ("Cell 5,4", 4, 3, "居民 1 人", "Utilization status", "完整循环"),
                ("Cell 5,3", 4, 2, "餐食2·岗位1", "Utilization status", "餐位使用"),
                ("Cell 6,4", 5, 3, "餐食2·岗位1", "Utilization status", "餐位使用"),
                ("Cell 5,5", 4, 4, "清运至多2/日", "Food stock", "清运后"),
                ("Cell 3,4", 2, 3, "草籽", "Plant growth", "成熟后可产食")
            })
            {
                var hover = board.Find(cell).GetComponent<EventTrigger>();
                hover.OnPointerEnter(pointer);
                Assert.That(card.gameObject.activeSelf, Is.True, cell);
                Assert.That(heading.text, Is.Not.Empty, cell);
                Assert.That(details.text, Does.Contain(provision), cell);
                Assert.That(details.text, Does.Not.Match("左上|左下|右上|右下"),
                    $"{cell} room summary should describe metrics without corner names.");
                Assert.That(details.preferredHeight,
                    Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                    $"{cell} nearby tooltip is clipped: {details.text}");
                AssertTmpBackedTextReadable(details, cell);
                hover.OnPointerExit(pointer);
                var badgeDetails = RoomBadgeDetails(root,
                    run.At(column, row), badge);
                Assert.That(badgeDetails, Does.Contain(meaning), cell);
                Assert.That(badgeDetails, Does.Not.Match("左上|左下|右上|右下"),
                    $"{cell} badge should explain its meaning without a corner name.");
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildPaletteCloseButtonReceivesRealClicks()
        {
            var root = new GameObject("Construction palette close test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);

            var side = root.transform.Find("Construction UI/Building choices");
            var backdrop = side.Find("Build palette backing");
            var close = side.Find("Open build palette").GetComponent<Button>();
            Assert.That(close.gameObject.activeInHierarchy, Is.True);
            close.onClick.Invoke();
            Assert.That(backdrop.gameObject.activeSelf, Is.True);
            Assert.That(close.GetComponentInChildren<Text>().text, Is.EqualTo("Close"));
            Assert.That(backdrop.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(close.transform.GetSiblingIndex(),
                Is.GreaterThan(backdrop.GetSiblingIndex()),
                "The raycast-blocking palette must stay behind its visible Close button.");
            close.onClick.Invoke();
            Assert.That(backdrop.gameObject.activeSelf, Is.False);
            Assert.That(side.Find("Next step backing").gameObject.activeSelf, Is.True);
            Assert.That(side.Find("Board progress backing").gameObject.activeSelf, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardPlacementNeedsConfirmationAndCancelKeepsDailyBuild()
        {
            var root = new GameObject("Construction confirmation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True,
                "The new room tour asks the player to choose a type first.");

            var canvas = root.transform.Find("Construction UI");
            var firstPlot = canvas.Find("7 by 7 construction board/Cell 5,4")
                .GetComponent<Button>();
            var secondPlot = canvas.Find("7 by 7 construction board/Cell 3,4")
                .GetComponent<Button>();
            var confirmation = canvas.Find("Building choices/Placement confirmation");
            var cancel = confirmation.Find("Cancel placement").GetComponent<Button>();
            var confirm = confirmation.Find("Confirm placement").GetComponent<Button>();

            firstPlot.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(1),
                "Selecting a plot must not mutate the board.");
            Assert.That(confirmation.gameObject.activeSelf, Is.True);
            var location = confirmation.Find("Confirmation location")
                .GetComponent<Text>();
            var notice = confirmation.Find("Confirmation notice")
                .GetComponent<Text>();
            Assert.That(location.text, Does.Contain("Green"));
            Assert.That(location.text, Does.Not.Contain("Plot"));
            Assert.That(location.text, Does.Not.Contain("Day"));
            Assert.That(notice.text, Does.Contain("Animal habitat"));
            Assert.That(notice.text, Does.Contain("survival"));
            Assert.That(notice.text, Does.Not.Contain("Day-end"));
            Assert.That(notice.preferredHeight,
                Is.LessThanOrEqualTo(notice.rectTransform.rect.height + 1f),
                $"Build confirmation notice is clipped: {notice.text}");
            var cardRect = confirmation.GetComponent<RectTransform>();
            foreach (var button in new[] { cancel, confirm })
            {
                var buttonRect = button.GetComponent<RectTransform>();
                Assert.That(buttonRect.anchoredPosition.y -
                        buttonRect.rect.height / 2f,
                    Is.GreaterThanOrEqualTo(-cardRect.rect.height / 2f),
                    "Confirmation actions must stay within the card.");
                Assert.That(Mathf.Abs(buttonRect.anchoredPosition.x) +
                        buttonRect.rect.width / 2f,
                    Is.LessThanOrEqualTo(cardRect.rect.width / 2f),
                    "Confirmation actions must stay within the card width.");
            }
            Assert.That(notice.rectTransform.anchoredPosition.y -
                    notice.rectTransform.rect.height / 2f,
                Is.GreaterThan(confirm.GetComponent<RectTransform>()
                    .anchoredPosition.y +
                    confirm.GetComponent<RectTransform>().rect.height / 2f),
                "The decision hint must stay above the action buttons.");
            controller.ToggleLanguage();
            Assert.That(notice.text, Does.Contain("周围八格"));
            Assert.That(notice.preferredHeight,
                Is.LessThanOrEqualTo(notice.rectTransform.rect.height + 1f),
                $"Chinese build confirmation is clipped: {notice.text}");
            Assert.That(firstPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.True);
            secondPlot.onClick.Invoke();
            Assert.That(firstPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.False, "A new plot replaces the staged placement.");
            Assert.That(secondPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.True);
            cancel.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(confirmation.gameObject.activeSelf, Is.False);

            firstPlot.onClick.Invoke();
            confirm.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.ConfirmPendingPlacement(), Is.False,
                "A second confirmation must never build twice.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReselectingGuidedRoomCancelsStagedPlacement()
        {
            var root = new GameObject("Construction cancelled selection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            var canvas = root.transform.Find("Construction UI");
            canvas.Find("7 by 7 construction board/Cell 5,4")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.False,
                "The next room stays locked until the green lesson is complete.");
            Assert.That(controller.HasPendingPlacement, Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.ConfirmPendingPlacement(), Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardCaseOpensIntoTwoInformationLeaves()
        {
            var root = new GameObject("Board-case opening test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            var canvas = root.transform.Find("Construction UI");
            var left = canvas.Find("Observation frame").GetComponent<RectTransform>();
            var right = canvas.Find("Action frame").GetComponent<RectTransform>();
            var board = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            Assert.That(left.rect.height, Is.EqualTo(right.rect.height));
            Assert.That(left.rect.height,
                Is.EqualTo(board.rect.height * board.localScale.y).Within(0.01f));
            Assert.That(left.Find("Printed leaf paper").GetComponent<Image>().sprite,
                Is.Not.Null, "The city print should be a real leaf texture.");
            Assert.That(right.Find("Printed leaf paper").GetComponent<Image>().sprite,
                Is.Not.Null, "The oak print should be a real leaf texture.");
            var sharedInformation = canvas.Find("Building choices")
                .GetComponent<CanvasGroup>();
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(controller.IsBoardCaseOpen, Is.False);
                Assert.That(left.localScale.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(right.localScale.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(canvas.Find("Left board-case cover").gameObject.activeSelf,
                    Is.True);
                Assert.That(canvas.Find("Left board-case cover/City cover print")
                    .GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(root.GetComponent<ConstructionWorldView>()
                    .BoardFoundation.Find("Folding board-case hardware"),
                    Is.Not.Null, "The original 3D board gains a physical tray and hinges.");
                yield return new WaitForSecondsRealtime(1.4f);
                Assert.That(controller.IsBoardCaseOpen, Is.True);
                Assert.That(left.localScale.x, Is.EqualTo(1f).Within(0.01f));
                Assert.That(right.localScale.x, Is.EqualTo(1f).Within(0.01f));
                Assert.That(sharedInformation.alpha, Is.EqualTo(1f).Within(0.01f));
                Assert.That(canvas.Find("Board-case opening input guard")
                    .gameObject.activeSelf, Is.False);
                Assert.That(canvas.Find("Left board-case cover")
                    .gameObject.activeSelf, Is.False);
            }
            else
                Assert.That(controller.IsBoardCaseOpen, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessStartsWithFreeBuildingAndAnUnobstructedDayPlaque()
        {
            var root = new GameObject("Endless no tutorial UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var canvas = root.transform.Find("Construction UI");
            var choices = canvas.Find("Building choices");
            var rules = canvas.Find("Construction Endless rules overlay");
            Assert.That(rules.gameObject.activeSelf, Is.True,
                "A fresh endless run must ask for ending limits before play.");
            Assert.That(choices.Find("Advance to next day").GetComponent<Button>()
                .interactable, Is.False);
            rules.Find("Rules card border/Rules card/Confirm ending rules")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(run.EndlessLossRulesConfirmed, Is.True);
            Assert.That(rules.gameObject.activeSelf, Is.False);
            Assert.That(choices.Find("Story cue backing/Today heading")
                .GetComponent<Text>().text, Is.EqualTo("ECOLOGY"));
            Assert.That(choices.Find("Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("0/1"));
            Assert.That(choices.Find("Next step backing/Next step heading")
                .GetComponent<Text>().text, Is.EqualTo("BUILD"));
            Assert.That(choices.Find("Open build palette").gameObject.activeSelf,
                Is.True);
            Assert.That(choices.Find("Advance to next day").GetComponent<Button>()
                .interactable, Is.False,
                "The next day becomes available after placing a room.");
            Assert.That(choices.Find("Next step backing/Placement guide")
                .GetComponent<Text>().text,
                Does.Contain("welcome a resident").And.Contain("Place one room"));
            Assert.That(choices.Find("Build Residence/Building icon")
                .GetComponent<Image>().sprite.name, Is.EqualTo("Hand art residence"));
            var scoreStrip = choices.Find("Board progress backing/Endless score strip");
            Assert.That(scoreStrip.gameObject.activeSelf, Is.True);
            Assert.That(choices.Find("Board progress backing/Balance score")
                .gameObject.activeSelf, Is.False);
            Assert.That(scoreStrip.Find("Score caption 0").GetComponent<Text>().text,
                Is.EqualTo("SHARED"));
            Assert.That(scoreStrip.Find("Score caption 1").GetComponent<Text>().text,
                Is.EqualTo("ANIMAL"));
            Assert.That(scoreStrip.Find("Score caption 2").GetComponent<Text>().text,
                Is.EqualTo("RESIDENT"));
            Assert.That(scoreStrip.Find("Score value 0").GetComponent<Text>()
                .resizeTextForBestFit, Is.False,
                "Long endless totals use compact notation instead of shrinking the font.");
            Assert.That(canvas.Find("Tutorial focus").gameObject.activeSelf,
                Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);

            var plaque = canvas.Find("Construction day plaque") as RectTransform;
            var north = canvas.Find("Pan board north") as RectTransform;
            Assert.That(plaque, Is.Not.Null);
            Assert.That(plaque.Find("Day").GetComponent<Text>().text,
                Is.EqualTo("DAY 1"));
            Assert.That(plaque.GetComponent<Image>().sprite, Is.Null,
                "The construction date should not reuse the day/night artwork.");
            Assert.That(plaque.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(Mathf.Abs(plaque.anchoredPosition.x - north.anchoredPosition.x),
                Is.GreaterThan((plaque.rect.width + north.rect.width) * 0.5f),
                "The north pan button must not cover the day marker.");
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(choices.Find("Advance to next day").GetComponent<Button>()
                .interactable, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessPlacementColorsResetAfterEachDay()
        {
            var root = new GameObject("Endless daily selection reset test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(ConfirmedEndlessRun());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.False);
            Assert.That(BuildableAt(root, 4, 3), Is.False);
            var greenFace = root.transform.Find(
                "Construction UI/Building choices/Build Green/Card face")
                .GetComponent<Image>();
            var idleColor = greenFace.color;
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(greenFace.color, Is.Not.EqualTo(idleColor));
            Assert.That(BuildableAt(root, 4, 3), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(greenFace.color, Is.EqualTo(idleColor));
            Assert.That(BuildableAt(root, 4, 2), Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(BuildableAt(root, 4, 2), Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConstructionDayPlaqueUpdatesWithDayAndLanguage()
        {
            var root = new GameObject("Construction date label test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;

            var plaque = root.transform.Find("Construction UI/Construction day plaque");
            var day = plaque.Find("Day").GetComponent<Text>();
            var progress = root.transform.Find(
                "Construction UI/Building choices/Board progress backing");
            Assert.That(progress.Find("Balance score").gameObject.activeSelf, Is.True);
            Assert.That(progress.Find("Endless score strip").gameObject.activeSelf,
                Is.False, "Fill keeps its established progress layout.");
            Assert.That(progress.Find("Board progress").GetComponent<RectTransform>()
                .anchoredPosition.y, Is.EqualTo(-10f));
            Assert.That(day.text, Is.EqualTo("DAY 1"));
            Assert.That(day.GetComponent<Outline>(), Is.Null,
                "The flat label needs no outline used by the old illustrated dial.");
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(day.text, Is.EqualTo("DAY 2"));
            controller.ToggleLanguage();
            Assert.That(day.text, Is.EqualTo("第 2 天"));
            Assert.That(day.resizeTextForBestFit, Is.True,
                "The label should also fit larger endless-mode day numbers.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FinishedEndlessRunShowsResultsInsteadOfMoreBuilding()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var plots = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(point => Math.Abs(point.column - 3) +
                    Math.Abs(point.row - 3))
                .Where(point => point != (3, 3) && point != (4, 3) && point != (2, 3));
            foreach (var plot in plots)
            {
                if (run.IsFinished) break;
                Assert.That(run.TryBuild(plot.column, plot.row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.IsFinished, Is.True);

            var root = new GameObject("Finished endless result UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            var choices = root.transform.Find("Construction UI/Building choices");
            Assert.That(choices.Find("Story cue backing/Today heading")
                .GetComponent<Text>().text, Is.EqualTo("RUN ENDED"));
            Assert.That(choices.Find("Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("Run ended"));
            Assert.That(choices.Find("Next step backing/Next step heading")
                .GetComponent<Text>().text, Is.EqualTo("RESULT"));
            Assert.That(choices.Find("Open build palette").gameObject.activeSelf,
                Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.False,
                "A finished run must reject even a direct category selection call.");
            Assert.That(root.transform.Find("Construction UI/Tutorial focus")
                .gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessRestartPersistsWithoutReplacingFillProgress()
        {
            var prefix = Path.Combine(Application.temporaryCachePath,
                "construction-modes-" + Guid.NewGuid().ToString("N"));
            var fillStore = new ConstructionRunStore(prefix + "-fill.json");
            var endlessStore = new ConstructionRunStore(prefix + "-endless.json");
            var fillRun = new ConstructionRunModel();
            Assert.That(fillRun.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(fillRun.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(fillStore.TrySave(fillRun), Is.True);
            var root = new GameObject("Endless restart save separation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(ConfirmedEndlessRun(), endlessStore);
            yield return null;
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(endlessStore.TryLoad(out var existing), Is.True);
            Assert.That(existing.BuiltCount, Is.EqualTo(2));
            var reset = root.transform.Find(
                "Construction UI/Start a new construction run").GetComponent<Button>();
            reset.onClick.Invoke();
            var rules = root.transform.Find(
                "Construction UI/Construction Endless rules overlay");
            Assert.That(rules.gameObject.activeSelf, Is.True);
            Assert.That(endlessStore.TryLoad(out var beforeConfirmation), Is.True);
            Assert.That(beforeConfirmation.BuiltCount, Is.EqualTo(2),
                "Opening new-run options must not erase the saved run.");
            rules.Find("Rules card border/Rules card/Confirm ending rules")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(endlessStore.TryLoad(out var fresh), Is.True);
            Assert.That(fresh.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(fresh.BuiltCount, Is.EqualTo(1));
            Assert.That(fresh.EndlessLossRulesConfirmed, Is.True);
            Assert.That(fillStore.TryLoad(out var fillAfterReset), Is.True);
            Assert.That(fillAfterReset.BuiltCount, Is.EqualTo(2));
            Object.Destroy(root);
            Assert.That(fillStore.TryDelete(), Is.True);
            Assert.That(endlessStore.TryDelete(), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessViewCanPanToTheNewRingAndBuildAfterDay49()
        {
            var run = ConfirmedEndlessRun();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var point in Enumerable.Range(0, 7)
                         .SelectMany(column => Enumerable.Range(0, 7)
                             .Select(row => (column, row)))
                         .OrderBy(point => Mathf.Abs(point.column - 3) +
                             Mathf.Abs(point.row - 3)))
            {
                if (point == (3, 3)) continue;
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            var root = new GameObject("Expanded endless construction test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            Assert.That(controller.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(controller.CurrentDay, Is.EqualTo(50));
            Assert.That(root.transform.Find("Construction UI/Title")
                .GetComponent<Text>().text, Does.Contain("ENDLESS BUILD"));
            var choices = root.transform.Find("Construction UI/Building choices");
            var progress = choices.Find("Board progress backing");
            Assert.That(progress.Find("Progress heading").GetComponent<Text>().text,
                Is.EqualTo("ROOMS BUILT"));
            Assert.That(progress.Find("Board progress").GetComponent<Text>().text,
                Is.EqualTo("49"), "Endless must not imply a 49-room limit.");
            Assert.That(choices.Find("Build Green/Build limit badge/Built and limit")
                .GetComponent<Text>().text, Is.EqualTo("49"),
                "Endless badges show rooms built, not a type cap.");
            Assert.That(root.transform.Find("Construction UI/Pan board west")
                .gameObject.activeSelf, Is.True);
            var continuousGround = root.transform.Find(
                "Construction low-poly world/Unbounded moving plot surface");
            var previousGroundX = continuousGround == null ? 0f :
                continuousGround.localPosition.x;
            Assert.That(controller.Pan(-1, 0), Is.True);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(continuousGround, Is.Not.Null);
                Assert.That(continuousGround.localPosition.x,
                    Is.EqualTo(previousGroundX - WorldScaleStandards.CellSizeMeters)
                        .Within(0.01f),
                    "The borderless plots must follow the endless viewport.");
            }
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(-1, 3), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(50));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.CurrentDay, Is.EqualTo(51));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonArrivalOnAnEndlessOuterPlotUsesThePannedViewport()
        {
            var run = ConfirmedEndlessRun();
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            for (var column = 4; column <= 6; column++)
            {
                Assert.That(run.TryBuild(column, 3, ConstructionCategory.Green,
                    GreenPlanting.Oak, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(run.PigeonTileId, Is.Null);

            var root = new GameObject("Endless outer pigeon arrival test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);
            Assert.That(controller.Pan(1, 0), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(7, 3), Is.True);

            var visibleCell = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            Assert.That(controller.AdvanceDay(), Is.True,
                "A flock arriving outside the original 7x7 area must not index UI by world coordinate.");
            Assert.That(run.PigeonTileId, Is.EqualTo(run.At(7, 3).id));
            var flock = FindCellMarker(visibleCell.transform, "Pigeon flock");
            var threeDReady = root.GetComponent<ConstructionWorldView>().IsReady;
            Assert.That(flock.gameObject.activeSelf, Is.EqualTo(!threeDReady));
            Assert.That(ActiveAnimalCount(visibleCell.transform).text, Is.EqualTo("2"));
            if (!threeDReady)
                Assert.That(flock.localScale.x, Is.LessThan(1f),
                    "The 2D fallback arrival must animate on the visible panned cell.");
            yield return new WaitForSecondsRealtime(0.55f);
            if (!threeDReady)
                Assert.That(flock.localScale.x, Is.EqualTo(1f).Within(0.01f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidentMealMarkerStaysHiddenWhenItsRouteLeavesTheEndlessViewport()
        {
            var root = new GameObject("Endless offscreen resident route test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(
                new ConstructionRunModel(ConstructionGameMode.Endless));
            yield return null;

            var walker = root.transform.Find(
                "Construction UI/7 by 7 construction board/Resident walking");
            Assert.That(walker, Is.Not.Null);
            Assert.That(walker.gameObject.activeSelf, Is.False);
            var route = new List<ConstructionTileData>
            {
                new() { column = 3, row = 3 },
                new() { column = 8, row = 3 }
            };
            var animation = typeof(ConstructionPrototypeController).GetMethod(
                "AnimateResidentMeal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(animation, Is.Not.Null);
            var steps = (IEnumerator)animation.Invoke(controller, new object[] { route });
            Assert.That(steps.MoveNext(), Is.False,
                "The UI marker must not cross the side panels for an offscreen route.");
            Assert.That(walker.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessLatestRoomControlRecentresTheBoardAndShowsBothCoordinates()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            var root = new GameObject("Endless latest-room navigation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            yield return WaitForBoardCaseOpen(controller);

            var canvas = root.transform.Find("Construction UI");
            var coordinates = canvas.Find("Viewport coordinates/Viewport position")
                .GetComponent<Text>();
            var recenter = canvas.Find("Center on latest room").GetComponent<Button>();
            Assert.That(recenter.gameObject.activeSelf, Is.True);
            Assert.That(coordinates.text, Does.Contain("VIEW 5,4"));
            Assert.That(coordinates.text, Does.Contain("LATEST 5,4"));
            Assert.That(controller.Pan(2, 1), Is.True);
            Assert.That(coordinates.text, Does.Contain("VIEW 7,5"));
            Assert.That(coordinates.text, Does.Contain("LATEST 5,4"));
            recenter.onClick.Invoke();
            Assert.That(coordinates.text, Does.Contain("VIEW 5,4"));
            controller.ToggleLanguage();
            Assert.That(recenter.GetComponentInChildren<Text>().text,
                Is.EqualTo("回到最新房间"));
            Assert.That(coordinates.text, Does.Contain("视野 5,4"));
            Object.Destroy(root);
            yield return null;
        }

        [Test]
        public void EndlessScoreAbbreviationsKeepLargeValuesShort()
        {
            var formatter = typeof(ConstructionPrototypeController).GetMethod(
                "CompactScore", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(formatter, Is.Not.Null);
            Assert.That(formatter.Invoke(null, new object[] { 99999, false }),
                Is.EqualTo("99999"));
            Assert.That(formatter.Invoke(null, new object[] { 1234567, false }),
                Is.EqualTo("1.2M"));
            Assert.That(formatter.Invoke(null, new object[] { 1234567, true }),
                Is.EqualTo("123.4万"));
        }

        [UnityTest]
        public IEnumerator FreshEndlessRunCanPanAndBuildPastOriginalEdgeBeforeDay49()
        {
            var path = Path.Combine(Application.temporaryCachePath,
                "construction-early-edge-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(path);
            var root = new GameObject("Early endless edge placement test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(ConfirmedEndlessRun(), store);
                yield return null;
                yield return WaitForBoardCaseOpen(controller);

                foreach (var column in new[] { 2, 1, 0 })
                {
                    Assert.That(controller.TrySelect(ConstructionCategory.Green,
                        GreenPlanting.Meadow), Is.True);
                    Assert.That(controller.TryPlace(column, 3), Is.True);
                    Assert.That(controller.AdvanceDay(), Is.True);
                }

                Assert.That(controller.CurrentDay, Is.EqualTo(4));
                Assert.That(controller.BuiltCount, Is.EqualTo(4));
                // Building at column 1 auto-centers the view at -2. Move it
                // one cell east so the first viewport column is (-1,3).
                Assert.That(controller.Pan(1, 0), Is.True,
                    "The view should move before the original board is full.");
                Assert.That(controller.TrySelect(ConstructionCategory.Green,
                    GreenPlanting.Meadow), Is.True);
                Assert.That(BuildableAt(root, 0, 3), Is.True,
                    "The viewport's first column now maps to (-1,3) and needs a build hint.");
                Assert.That(controller.TryPlace(-1, 3), Is.True);
                Assert.That(controller.BuiltCount, Is.EqualTo(5));
                Assert.That(controller.CurrentDay, Is.LessThan(49));

                Assert.That(store.TryLoad(out var restored), Is.True);
                Assert.That(restored.Mode, Is.EqualTo(ConstructionGameMode.Endless));
                Assert.That(restored.CurrentDay, Is.EqualTo(4));
                Assert.That(restored.BuiltCount, Is.EqualTo(5));
                Assert.That(restored.At(-1, 3), Is.Not.Null,
                    "The early edge placement must survive a real isolated save/load.");
            }
            finally
            {
                Object.Destroy(root);
                store.TryDelete();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChineseConstructionUsesSharedPreferenceAndLegacyVisuals()
        {
            const string languageKey = "symbiosis49.language";
            var previousPreference = PlayerPrefs.GetInt(languageKey, -1);
            var root = new GameObject("Chinese construction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            var legacyRun = LegacyFillRun();
            controller.UseRunForTesting(legacyRun);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;

            var canvas = root.transform.Find("Construction UI");
            var story = canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            var category = canvas.Find("Building choices/Build Green/Label")
                .GetComponent<Text>();
            var languageSelector = canvas.Find("Construction language");
            var englishButton = languageSelector.Find("English").GetComponent<Button>();
            var chineseButton = languageSelector.Find("Chinese").GetComponent<Button>();
            var englishLabel = englishButton.GetComponentInChildren<Text>();
            var chineseLabel = chineseButton.GetComponentInChildren<Text>();
            Assert.That(englishLabel.text, Is.EqualTo("EN"));
            Assert.That(chineseLabel.text, Is.EqualTo("中文"));
            Assert.That(englishLabel.font.name, Does.Contain("Nunito"));
            Assert.That(chineseLabel.font.name, Does.Contain("NotoSansSC"));
            Assert.That(chineseButton.targetGraphic.color.a,
                Is.GreaterThan(englishButton.targetGraphic.color.a),
                "The selected language must be visibly indicated.");
            var boardRect = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(boardRect.rect.width, Is.GreaterThanOrEqualTo(740f));
            var choices = canvas.Find("Building choices").GetComponent<Image>();
            Assert.That(choices.color.a, Is.EqualTo(0f).Within(0.001f),
                "Construction choices must not form a permanent right sidebar.");
            var palette = canvas.Find("Building choices/Build palette backing");
            Assert.That(palette.gameObject.activeSelf, Is.False);
            var buildToggle = canvas.Find("Building choices/Open build palette")
                .GetComponent<Button>();
            Assert.That(buildToggle.gameObject.activeSelf, Is.False,
                "The first day asks the player to observe the squirrel, not build.");
            var openingGuide = canvas.Find(
                "Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(openingGuide.text, Does.Contain("点击中央橡树"));
            Assert.That(openingGuide.text, Does.Contain("右侧固定"));
            Assert.That(openingGuide.preferredHeight,
                Is.LessThanOrEqualTo(openingGuide.rectTransform.rect.height + 1f));
            Assert.That(story.text, Does.Contain("松鼠"));
            Assert.That(story.fontSize, Is.GreaterThanOrEqualTo(17));
            Assert.That(story.rectTransform.rect.width, Is.LessThanOrEqualTo(250f),
                "Text must remain inside the opaque part of the left paper leaf.");
            Assert.That(category.text, Is.EqualTo("绿地"));
            Assert.That(canvas.Find("Building choices/Build Green/Build limit badge/Built and limit")
                .GetComponent<Text>().text, Does.Not.Contain("/21"),
                "The Fill interface must not show a category quota from the physical deck.");
            Assert.That(category.font.name, Does.Contain("NotoSansSC"));
            Assert.That(canvas.Find("Paper background").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(canvas.Find("Building choices").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(canvas.Find("Building choices/Build Green/Building icon")
                .GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(canvas.Find(
                "7 by 7 construction board/Live low-poly board"), Is.Null,
                "The board must not be a RenderTexture inside a UI image.");
            Assert.That(canvas.Find(
                "7 by 7 construction board/3D board alignment"), Is.Not.Null);
            var worldView = root.GetComponent<ConstructionWorldView>();
            Assert.That(worldView, Is.Not.Null);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(Camera.main.targetTexture, Is.Null,
                    "The scene camera renders the shared-style board directly.");
                var world = root.transform.Find("Construction low-poly world");
                Assert.That(world, Is.Not.Null);
                Assert.That(world.Find("Squirrel"), Is.Not.Null);
                Assert.That(world.Find("Board base").gameObject.layer, Is.EqualTo(29),
                    "The board base must be visible to the construction camera.");
                Assert.That(world.Find("Board base/Empty plot 1,1").gameObject.layer,
                    Is.EqualTo(29),
                    "Unbuilt plots must be visible in the 3D board render.");
                Assert.That(world.Find("Board base/Empty plot 1,1")
                    .localScale.x, Is.LessThan(WorldScaleStandards.CellSizeMeters),
                    "Vacant cells should leave a fine ivory grid joint.");
                Assert.That(world.Find("Board base/Empty plot 1,1")
                    .localScale.x, Is.GreaterThan(WorldScaleStandards.CellSizeMeters - 0.1f),
                    "The green field must not look like widely spaced raised tiles.");
                var emptyFace = world.Find(
                    "Board base/Empty plot 1,1/Pastel plot face");
                Assert.That(emptyFace, Is.Not.Null,
                    "Vacant plots keep the 3D face while matching the sage field.");
                var emptyRenderer = emptyFace.GetComponent<Renderer>();
                Assert.That(emptyRenderer.sharedMaterial.HasProperty("_ShadowStrength"),
                    Is.True);
                var emptyProperties = new MaterialPropertyBlock();
                emptyRenderer.GetPropertyBlock(emptyProperties);
                Assert.That(emptyProperties.GetFloat("_ShadowStrength"), Is.Zero,
                    "An empty plot must not inherit broad shadows from the case lid.");
                Assert.That(world.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Low-poly Mature Tree"), Is.True,
                    "The construction oak must reuse the original faceted tree kit.");
                Assert.That(root.GetComponentInChildren<UrbanWildlifeRooms.Animals.SquirrelDemoVisual>(),
                    Is.Not.Null);
            }
            Assert.That(story.preferredHeight,
                Is.LessThanOrEqualTo(story.rectTransform.rect.height + 1f));

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(buildToggle.gameObject.activeSelf, Is.True);
            buildToggle.onClick.Invoke();
            Assert.That(palette.gameObject.activeSelf, Is.True);
            buildToggle.onClick.Invoke();
            Assert.That(palette.gameObject.activeSelf, Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            Assert.That(canvas.Find("Building choices/Green planting"), Is.Null);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(legacyRun.PigeonPopulation, Is.GreaterThan(0),
                "The connected meadow should still attract a visible flock.");
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(story.text, Does.Contain("住宅"));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(canvas.Find("Route inspection/Route status")
                .GetComponent<Text>().text, Does.Contain("明日"));

            englishButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.English));
            Assert.That(category.text, Is.EqualTo("Green"));
            Assert.That(category.font.name, Does.Contain("Nunito"));
            Assert.That(story.text, Does.Contain("residence").And
                .Contain("sharing an edge"));
            Assert.That(englishButton.targetGraphic.color.a,
                Is.GreaterThan(chineseButton.targetGraphic.color.a));
            englishButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.English),
                "Choosing the active language must not toggle to the other one.");
            chineseButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.Chinese));
            Assert.That(category.font.name, Does.Contain("NotoSansSC"));
            Assert.That(PlayerPrefs.GetInt(languageKey, -1), Is.EqualTo(previousPreference),
                "A PlayMode test must not change the player's language preference.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatureNeighbourOaksShowGrowingSquirrelCountAndMeals()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Squirrel growth UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            var starter = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            Assert.That(FindCellMarker(starter.transform, "Plant growth/Plant growth value")
                .GetComponent<Text>().text, Is.EqualTo("2/2"));
            Assert.That(ActiveAnimalCount(starter.transform).text, Is.EqualTo("1"),
                "The badge counts squirrels living on this oak, not across the map.");
            var neighbour = run.VisibleSquirrelHomeTiles(8)[1];
            AssertAnimalCornerBadge(root, neighbour, "Resident squirrel", "1");
            Assert.That(controller.TryPlace(3, 3), Is.False);
            var routeStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status").GetComponent<Text>();
            Assert.That(routeStatus.text, Does.Contain("2/2"));
            Assert.That(routeStatus.text, Does.Contain("Here 1/1"));
            Assert.That(routeStatus.text, Does.Contain("oaks 3/5"));
            var outcomes = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Observed outcomes")
                .GetComponent<Text>();
            Assert.That(outcomes.text, Does.Contain("Next: Squirrel 2/2"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FillPaletteShowsOnlyBuiltCountAndAllowsMoreThanTwentyOneGreenRooms()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(point => point != (3, 3))
                .OrderBy(point => Mathf.Abs(point.column - 3) +
                    Mathf.Abs(point.row - 3)).ToArray();
            foreach (var point in positions.Take(20))
            {
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(21));
            Assert.That(run.IsCategoryAtLimit(ConstructionCategory.Green), Is.False);
            var root = new GameObject("Construction unlimited Fill category test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            var canvas = root.transform.Find("Construction UI");
            var badge = canvas.Find(
                "Building choices/Build Green/Build limit badge/Built and limit")
                .GetComponent<Text>();
            Assert.That(badge.text, Is.EqualTo("21"),
                "Fill shows the number built, without a per-type denominator.");
            var guide = canvas.Find("Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(guide.text, Does.Contain("Build one room of your choice today"),
                "The general build guide remains usable after the former cap.");
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            var next = positions[20];
            Assert.That(controller.TryPlace(next.column, next.row), Is.True);
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(22));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullBoardShowsResultAndCanStartNewRun()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Mathf.Abs(position.column - 3) +
                    Mathf.Abs(position.row - 3)).ToArray();
            var requiredServices = new[]
            {
                ConstructionCategory.Workshop, ConstructionCategory.Restaurant,
                ConstructionCategory.Supermarket, ConstructionCategory.Waste,
                ConstructionCategory.Street
            };
            for (var index = 0; index < positions.Length; index++)
            {
                var position = positions[index];
                var category = index < 20 ? ConstructionCategory.Green
                    : index < 32 ? ConstructionCategory.Residence
                    : index < 37 ? requiredServices[index - 32]
                    : ConstructionCategory.Square;
                Assert.That(run.TryBuild(position.column, position.row,
                    category, category == ConstructionCategory.Green
                        ? GreenPlanting.Meadow : GreenPlanting.None,
                    out _, out var failure, out _), Is.True, failure.ToString());
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.BuiltCount, Is.EqualTo(49));
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Green), Is.EqualTo(21));
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Residence), Is.EqualTo(12));
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square), Is.EqualTo(11));
            var resultSavePath = Path.Combine(Application.temporaryCachePath,
                "construction-result-" + Guid.NewGuid().ToString("N") + ".json");
            var rankingPath = Path.Combine(Application.temporaryCachePath,
                "construction-rank-" + Guid.NewGuid().ToString("N") + ".json");
            var resultStore = new ConstructionRunStore(resultSavePath);
            var root = new GameObject("Construction result test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run, resultStore,
                new ConstructionLocalLeaderboardStore(rankingPath));
            yield return null;

            var result = root.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(result.gameObject.activeSelf, Is.True);
            Assert.That(result.GetSiblingIndex(),
                Is.EqualTo(result.parent.childCount - 1),
                "The final cards must render above board decorations.");
            var resultMenu = result.Find("Result card/Return to menu after result")
                .GetComponent<Button>();
            Assert.That(resultMenu.interactable, Is.True,
                "The result overlay must let players leave without erasing the finished run.");
            Assert.That(resultMenu.GetComponentInChildren<Text>().text,
                Is.EqualTo("Menu"));
            var resultResetButton = result.Find("Result card/New run after result")
                .GetComponent<Button>();
            Assert.That(resultMenu.GetComponent<RectTransform>().anchoredPosition.x,
                Is.LessThan(resultResetButton.GetComponent<RectTransform>()
                    .anchoredPosition.x));
            var resultCard = result.Find("Result card").GetComponent<RectTransform>();
            Assert.That(resultCard.GetComponent<Image>().enabled, Is.False,
                "The Fill result should use printed cards, not the pause panel.");
            var printed = result.Find("Result card/Illustrated result cards");
            Assert.That(printed.gameObject.activeSelf, Is.True);
            var mainCard = printed.Find("Illustrated main card");
            var breakdownCard = printed.Find("Illustrated breakdown card");
            Assert.That(mainCard.Find("Main card print").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(breakdownCard.Find("Breakdown card print")
                .GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(printed.Find("Result footer paper")
                .GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(mainCard.Find("Card result title").GetComponent<Text>().text,
                Is.EqualTo("COEXISTENCE RESULTS"));
            Assert.That(mainCard.Find("Card result mode").GetComponent<Text>().text,
                Is.EqualTo("49 plots placed"));
            var footerControls = new[]
            {
                result.Find("Result card/Leaderboard name")
                    .GetComponent<RectTransform>(),
                result.Find("Result card/Network leaderboard status")
                    .GetComponent<RectTransform>(),
                resultMenu.GetComponent<RectTransform>(),
                resultResetButton.GetComponent<RectTransform>()
            };
            foreach (var rect in footerControls)
                Assert.That(Mathf.Abs(rect.anchoredPosition.y) +
                    rect.sizeDelta.y * 0.5f, Is.LessThan(390f),
                    rect.name + " extends beyond the result slip.");
            foreach (var action in new[] { resultMenu, resultResetButton })
            {
                var rect = action.GetComponent<RectTransform>();
                Assert.That(Mathf.Abs(rect.anchoredPosition.y) + rect.sizeDelta.y * 0.5f,
                    Is.LessThan(resultCard.sizeDelta.y * 0.5f),
                    "Result actions must remain inside the illustrated card.");
            }
            var award = result.Find("Result card/Award evaluation");
            Assert.That(award.gameObject.activeSelf, Is.False);
            Assert.That(ConstructionAwardModel.TryEvaluate(run.Mode,
                run.ScoringVersion, run.FinalResult.DaysPlayed, run.EndReason,
                run.FinalResult.BalancePointsTotal, out var awardedTier), Is.True);
            Assert.That(mainCard.Find("Card award trophy").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(mainCard.Find("Card total score").GetComponent<Text>().text,
                Is.EqualTo(run.FinalResult.BalancePointsTotal.ToString()));
            Assert.That(breakdownCard.Find("Card Animals value")
                .GetComponent<Text>().text,
                Is.EqualTo(run.FinalResult.AnimalPointsTotal.ToString()));
            Assert.That(breakdownCard.Find("Card Residents value")
                .GetComponent<Text>().text,
                Is.EqualTo(run.FinalResult.HumanPointsTotal.ToString()));
            breakdownCard.Find("Open award standards").GetComponent<Button>()
                .onClick.Invoke();
            var standards = result.Find("Award standards overlay");
            Assert.That(standards.gameObject.activeSelf, Is.True);
            Assert.That(standards.Find("Award standards card/Award standard " +
                awardedTier).GetComponent<Image>().color.a,
                Is.GreaterThan(0f));
            standards.Find("Award standards card/Close award standards")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(standards.gameObject.activeSelf, Is.False);
            Assert.That(result.Find("Result card/Result numbers").gameObject.activeSelf,
                Is.False);
            var clippedResultLabels = new List<string>();
            for (var languageIndex = 0; languageIndex < 2; languageIndex++)
            {
                if (languageIndex == 1)
                {
                    controller.ToggleLanguage();
                    yield return null;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var label in result.GetComponentsInChildren<
                    TMPro.TextMeshProUGUI>(true))
                {
                    if (!label.gameObject.activeInHierarchy || label.color.a < 0.01f)
                        continue;
                    label.ForceMeshUpdate();
                    if (label.isTextTruncated)
                        clippedResultLabels.Add(
                            $"{(languageIndex == 0 ? "English" : "Chinese")}: " +
                            $"{label.transform.parent.name} ({label.rectTransform.rect.size}): {label.text}");
                }
            }
            Assert.That(clippedResultLabels, Is.Empty,
                "Result text truncated: " + string.Join(" | ", clippedResultLabels));
            var openRanks = result.Find("Result card/Open local rankings")
                .GetComponent<Button>();
            openRanks.onClick.Invoke();
            var ranks = root.transform.Find("Construction UI/Local rankings overlay");
            Assert.That(ranks.gameObject.activeSelf, Is.True);
            var ranksCard = ranks.Find("Local rankings card");
            Assert.That(ranksCard.GetComponent<RectTransform>().sizeDelta.y,
                Is.LessThan(720), "One result should use a compact ranking card.");
            var nameInput = ranksCard.Find(
                "Edit my ranking name/My ranking name input")
                .GetComponent<InputField>();
            Assert.That(nameInput.interactable, Is.True);
            yield return null;
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(nameInput.gameObject),
                "Opening rankings should put keyboard focus in the name field.");
            Assert.That(nameInput.isFocused, Is.True);
            nameInput.ProcessEvent(new Event
            {
                type = EventType.KeyDown, keyCode = KeyCode.B,
                character = 'B'
            });
            Assert.That(nameInput.text, Is.EqualTo("B"),
                "Typing should replace the selected previous name.");
            nameInput.text = "Oak Tester";
            ranksCard.Find("Edit my ranking name/Save my ranking name")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(ranksCard.Find("Rank 1 column 1")
                .GetComponent<Text>().text, Is.EqualTo("Oak Tester"),
                "Saving through the ranking card should rename this run.");
            ranksCard.Find("Close local rankings").GetComponent<Button>()
                .onClick.Invoke();
            Assert.That(ranks.gameObject.activeSelf, Is.False);
            var reset = result.Find("Result card/New run after result")
                .GetComponent<Button>();
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            // A previously written save can become invalid after a rules
            // update. Confirmation must keep its bytes and still start anew.
            const string invalidSave = "{ damaged previous run";
            File.WriteAllText(resultSavePath, invalidSave);
            reset.onClick.Invoke();
            Assert.That(resultStore.TryLoad(out var newRun), Is.True);
            Assert.That(newRun.BuiltCount, Is.Zero,
                "A confirmed Fill reset must save an empty board before reloading the scene.");
            var archives = Directory.GetFiles(
                Path.GetDirectoryName(resultSavePath),
                Path.GetFileName(resultSavePath) + ".invalid-*.json");
            Assert.That(archives, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(archives[0]), Is.EqualTo(invalidSave),
                "The unreadable old run must remain recoverable.");
            Object.Destroy(root);
            yield return null;
            if (File.Exists(resultSavePath)) File.Delete(resultSavePath);
            foreach (var archive in archives) File.Delete(archive);
            if (File.Exists(rankingPath)) File.Delete(rankingPath);
        }

        [UnityTest]
        public IEnumerator MixedStoryReachesFinalResultAndNewRunSurvivesReload()
        {
            var savePath = Path.Combine(Application.temporaryCachePath,
                "construction-flow-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(savePath);
            var root = new GameObject("Mixed construction flow test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun(), store);
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            PlaceAndAdvance(controller, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(controller.TryPlace(4, 3), Is.False,
                "Inspecting the arrived pigeons unlocks the resident beat.");
            PlaceAndAdvance(controller, 3, 2, ConstructionCategory.Residence);
            PlaceAndAdvance(controller, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(store.TryLoad(out var saved), Is.True);
            Assert.That(saved.BuiltCount, Is.EqualTo(4));
            Assert.That(saved.CurrentDay, Is.EqualTo(5));

            // A newly opened screen must continue the saved run, not its starter state.
            Object.Destroy(root);
            yield return null;
            root = new GameObject("Reopened mixed construction flow test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(saved, store);
            yield return null;
            Assert.That(controller.BuiltCount, Is.EqualTo(4));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));

            PlaceAndAdvance(controller, 2, 2, ConstructionCategory.Workshop);
            PlaceAndAdvance(controller, 2, 1, ConstructionCategory.Waste);
            PlaceAndAdvance(controller, 5, 3, ConstructionCategory.Residence);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var world = root.transform.Find("Construction low-poly world");
                Assert.That(store.TryLoad(out var afterSecondHome), Is.True);
                var secondHome = afterSecondHome.BuiltTiles.Single(tile =>
                    tile.category == ConstructionCategory.Residence &&
                    tile.id != afterSecondHome.FirstResidentHomeTileId);
                var firstActor = world.Find("First resident");
                var secondActor = world.Find($"Resident {secondHome.id}");
                Assert.That(firstActor, Is.Not.Null);
                Assert.That(secondActor, Is.Not.Null,
                    "Every occupied residence needs its own 3D resident.");
                Assert.That(secondActor.gameObject.activeSelf, Is.True);
                Assert.That(world.GetComponentsInChildren<UrbanWildlifeRooms.People.CitizenDemoVisual>()
                    .Length, Is.EqualTo(2));
                Assert.That(secondActor.localPosition.x,
                    Is.GreaterThan(WorldScaleStandards.CellSizeMeters),
                    "The second resident should appear at the second home, not the first.");
                var firstHome = afterSecondHome.BuiltTiles.Single(tile =>
                    tile.id == afterSecondHome.FirstResidentHomeTileId);
                var service = afterSecondHome.BuiltTiles.First(tile =>
                    tile.category == ConstructionCategory.Workshop);
                var view = root.GetComponent<ConstructionWorldView>();
                view.PlayResidentCommutes(
                    Array.Empty<ConstructionResidentDayResult>(),
                    afterSecondHome.BuiltTiles);
                var firstStart = firstActor.localPosition;
                var secondStart = secondActor.localPosition;
                view.PlayResidentCommutes(new[]
                {
                    new ConstructionResidentDayResult
                    {
                        homeTileId = firstHome.id,
                        worked = true,
                        routeTileIds = new System.Collections.Generic.List<string>
                            { firstHome.id, service.id, firstHome.id }
                    },
                    new ConstructionResidentDayResult
                    {
                        homeTileId = secondHome.id,
                        worked = true,
                        routeTileIds = new System.Collections.Generic.List<string>
                            { secondHome.id, service.id, secondHome.id }
                    }
                }, afterSecondHome.BuiltTiles);
                var firstImmediate = firstActor.localPosition;
                var secondImmediate = secondActor.localPosition;
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(Mathf.Max(
                    Vector3.Distance(firstStart, firstImmediate),
                    Vector3.Distance(firstStart, firstActor.localPosition)),
                    Is.GreaterThan(0.1f));
                Assert.That(Mathf.Max(
                    Vector3.Distance(secondStart, secondImmediate),
                    Vector3.Distance(secondStart, secondActor.localPosition)),
                    Is.GreaterThan(0.1f),
                    "Both resident visuals should move on supplied routes.");
                view.PlayResidentCommutes(
                    Array.Empty<ConstructionResidentDayResult>(),
                    afterSecondHome.BuiltTiles);
                Assert.That(Vector3.Distance(firstStart, firstActor.localPosition),
                    Is.LessThan(0.1f));
                Assert.That(Vector3.Distance(secondStart, secondActor.localPosition),
                    Is.LessThan(0.1f),
                    "Stopping simultaneous commutes must restore both homes.");
            }
            PlaceAndAdvance(controller, 4, 2, ConstructionCategory.Street);
            PlaceAndAdvance(controller, 5, 2, ConstructionCategory.Street);
            PlaceAndAdvance(controller, 4, 1, ConstructionCategory.Street);
            var story = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            Assert.That(controller.Stage ==
                        ConstructionStoryStage.AwaitRestaurantCrowding ||
                        controller.Stage == ConstructionStoryStage.BuildSupermarket,
                Is.True,
                "Cardinal routes may reveal restaurant crowding before the next lesson.");
            Assert.That(story.text, Does.Contain("2 meal seats").And
                .Contain("1 job").And.Contain("animal"),
                "The next-room guidance must explain the supermarket's role.");
            PlaceAndAdvance(controller, 6, 3, ConstructionCategory.Waste);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildSupermarket));
            Assert.That(story.text, Does.Contain("2 meal seats").And
                .Contain("1 job").And.Contain("animal"));
            PlaceAndAdvance(controller, 5, 1, ConstructionCategory.Supermarket);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.FreeBuild));
            Assert.That(story.text, Does.Contain("square").And
                .Contain("sharing an edge"));
            PlaceAndAdvance(controller, 6, 2, ConstructionCategory.Square);
            var freeStory = root.transform.Find(
                "Construction UI/Building choices/Story cue backing");
            var freeBuildHint = freeStory.Find("Story preview")
                .GetComponent<Text>().text;
            Assert.That(freeBuildHint.Contains("reachable green food") ||
                        freeBuildHint.Contains("Wildlife lacks food") &&
                        freeBuildHint.Contains("add green"), Is.True,
                "Free-build guidance should explain how to restore animal food.");
            Assert.That(freeStory.Find("Story pictogram").GetComponent<Image>().sprite,
                Is.SameAs(UrbanWildlifeRooms.UI.ConstructionIconCatalog.GetControl(
                    UrbanWildlifeRooms.UI.ConstructionControlIcon.FillMode)),
                "Free-build guidance must not picture the last tutorial room.");
            var freeGuide = root.transform.Find(
                "Construction UI/Building choices/Next step backing");
            Assert.That(freeGuide.Find("Next step heading").GetComponent<Text>().text,
                Is.EqualTo("TODAY"));
            Assert.That(freeGuide.Find("Placement guide").GetComponent<Text>().text,
                Does.Contain("one room of your choice"));
            Assert.That(root.GetComponentsInChildren<RectTransform>(true)
                .Single(rect => rect.name == "Tutorial focus").gameObject.activeSelf,
                Is.False);
            Assert.That(BuildableAt(root, 5, 4), Is.False,
                "The last tutorial selection must not auto-mark free-build plots.");
            Assert.That(controller.RequestPlacement(5, 4), Is.False);
            Assert.That(controller.TryPlace(5, 4), Is.False);
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(13));

            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(position => Mathf.Abs(position.column - 3) +
                    Mathf.Abs(position.row - 3));
            var occupied = new[]
            {
                (3, 3), (4, 3), (3, 2), (3, 1), (2, 2), (2, 1),
                (5, 3), (6, 3), (4, 2), (5, 2), (4, 1), (5, 1), (6, 2)
            };
            foreach (var position in positions)
            {
                if (controller.BuiltCount == 49) break;
                if (occupied.Contains((position.column, position.row))) continue;
                var category = saved.CategoryBuiltCount(ConstructionCategory.Green) < 21
                    ? ConstructionCategory.Green
                    : saved.CategoryBuiltCount(ConstructionCategory.Residence) < 12
                        ? ConstructionCategory.Residence : ConstructionCategory.Square;
                PlaceAndAdvance(controller, position.column, position.row,
                    category, category == ConstructionCategory.Green
                        ? GreenPlanting.Meadow : GreenPlanting.None);
            }
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            Assert.That(controller.CurrentDay, Is.EqualTo(49));
            Assert.That(controller.ObservedAnimalMeals, Is.GreaterThan(0));
            Assert.That(controller.ObservedHumanWorkCycles, Is.GreaterThan(0));
            var finishedStory = root.transform.Find(
                "Construction UI/Building choices/Story cue backing");
            Assert.That(finishedStory.Find("Today heading").GetComponent<Text>().text,
                Is.EqualTo("COMPLETE"));
            Assert.That(finishedStory.Find("Story preview").GetComponent<Text>().text,
                Does.Contain("Run ended"),
                "A finished run must not ask for another room or free-build choice.");
            var result = root.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(result.gameObject.activeSelf, Is.True);
            Assert.That(store.TryLoad(out var finished), Is.True);
            var numbers = result.Find("Result card/Result numbers")
                .GetComponent<Text>().text;
            Assert.That(numbers, Is.EqualTo(
                finished.FinalResult.BalancePointsTotal.ToString()));
            Assert.That(result.Find("Result card/Result title")
                .GetComponent<Text>().text, Is.EqualTo("COEXISTENCE RATING"));
            Assert.That(result.Find("Result card/Result equation")
                .GetComponent<Text>().text,
                Does.Contain("animal score + resident score"));
            Assert.That(result.Find("Result card/Result mode")
                .GetComponent<Text>().text, Does.Contain("FINAL RESULT"));
            Assert.That(finished.IsFinished, Is.True);
            Assert.That(finished.BuiltCount, Is.EqualTo(49));

            var reset = result.GetComponentInChildren<Button>();
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(result.gameObject.activeSelf, Is.False);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.That(root.transform.Find("Construction low-poly world")
                    .GetComponentsInChildren<UrbanWildlifeRooms.People.CitizenDemoVisual>()
                    .Length, Is.Zero,
                    "Restart must remove both residents from the old run.");
            Assert.That(store.TryLoad(out var fresh), Is.True);
            Assert.That(fresh.BuiltCount, Is.EqualTo(1));
            Assert.That(fresh.CurrentDay, Is.EqualTo(1));
            Assert.That(fresh.IsFinished, Is.False);

            Object.Destroy(root);
            yield return null;
            root = new GameObject("Reopened after reset test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(fresh, store);
            yield return null;
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Object.Destroy(root);
            Assert.That(store.TryDelete(), Is.True);
            yield return null;
        }

        private static void PlaceAndAdvance(ConstructionPrototypeController controller,
            int column, int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(controller.TrySelect(category, planting), Is.True,
                $"{category} is locked before placing {column + 1},{row + 1}.");
            Assert.That(controller.TryPlace(column, row), Is.True,
                $"Could not place {category} at {column + 1},{row + 1}.");
            if (controller.BuiltCount < 49)
                Assert.That(controller.AdvanceDay(), Is.True,
                    $"Could not settle day after {column + 1},{row + 1}.");
            // Placing the last tile settles day 49 automatically.
            AssertStoryPresentationFits(controller);
        }

        private static void AssertStoryPresentationFits(
            ConstructionPrototypeController controller)
        {
            var backing = controller.transform.Find(
                "Construction UI/Building choices/Story cue backing");
            // Story instruction is a hidden data source for the resizable
            // hover tooltip; its fixed source rect is never drawn on screen.
            foreach (var name in new[] { "Story preview", "Room function" })
            {
                var label = backing.Find(name).GetComponent<Text>();
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{controller.Language} {name} is clipped: {label.text}");
            }
            var report = controller.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            if (report.gameObject.activeInHierarchy &&
                report.GetComponent<CanvasGroup>().alpha > 0f)
                Assert.That(report.preferredHeight,
                    Is.LessThanOrEqualTo(report.rectTransform.rect.height + 1f),
                    $"{controller.Language} day review is clipped: {report.text}");
            if (!controller.IsBoardCaseOpen || controller.HasPendingPlacement) return;
            var icon = backing.Find("Story pictogram").GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            icon.OnPointerEnter(pointer);
            var tooltip = controller.transform.Find("Construction UI/Context tooltip");
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            Assert.That(details.preferredHeight,
                Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                $"{controller.Language} tutorial tooltip is clipped: {details.text}");
            icon.OnPointerExit(pointer);
        }

        [UnityTest]
        public IEnumerator StarterOakFoodScoreFloatsThenSettlesOnActualTotals()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill);
            var root = new GameObject("Starter oak score motion test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(run.LastScoreDay, Is.Not.Null);
            Assert.That(run.LastScoreDay.animalPoints, Is.EqualTo(1));
            Assert.That(run.AnimalScore, Is.EqualTo(1));
            var canvas = root.transform.Find("Construction UI");
            var motion = canvas.Find("Score motion overlay");
            Assert.That(motion, Is.Not.Null);
            var tag = motion.Find("Score motion border/Score motion face");
            Assert.That(tag.Find("Score change").GetComponent<Text>().text,
                Is.EqualTo("+1"));
            Assert.That(tag.Find("Score reason").GetComponent<Text>().text,
                Is.EqualTo("Nearby food areas"));
            Assert.That(motion.GetComponent<CanvasGroup>().blocksRaycasts,
                Is.False, "Score feedback must not block board input.");

            yield return new WaitForSecondsRealtime(1.1f);
            var story = canvas.Find("Building choices/Story cue backing");
            Assert.That(story.Find("Animal score metric/Value")
                .GetComponent<Text>().text, Is.EqualTo(run.AnimalScore.ToString()));
            Assert.That(story.Find("Work cycles metric/Value")
                .GetComponent<Text>().text, Is.EqualTo(run.HumanScore.ToString()));
            Assert.That(canvas.Find("Building choices/Board progress backing/Balance score")
                .GetComponent<Text>().text,
                Is.EqualTo($"SHARED {run.BalanceScore}\n= ANIMAL {run.AnimalScore} + RESIDENT {run.HumanScore}"));
            Assert.That(motion.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FillDayCannotBeSkippedAfterTheStarterOak()
        {
            var root = new GameObject("Daily fill rule test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True,
                "The day-one oak and squirrel observation is scripted.");
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(controller.AdvanceDay(), Is.False);
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback").GetComponent<Text>().text,
                Does.Contain("Build one room"));
            var advance = root.transform.Find(
                "Construction UI/Building choices/Advance to next day")
                .GetComponent<Button>();
            Assert.That(advance.interactable, Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(advance.interactable, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildableMarkersFollowSelectedRulesAndDailyLimit()
        {
            var root = new GameObject("Buildable markers test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(BuildableAt(root, 4, 3), Is.False,
                "The guided day still waits for the player to select its room.");
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(BuildableAt(root, 4, 3), Is.True,
                "An edge-adjacent green cell should be suggested.");
            Assert.That(BuildableAt(root, 4, 2), Is.False,
                "A diagonal plot must not be suggested for the first green.");
            Assert.That(BuildableAt(root, 3, 2), Is.True,
                "The first green may touch the central oak along an edge.");
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(BuildableAt(root, 3, 3), Is.False,
                "No second build may be suggested on the same day.");

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(BuildableAt(root, 3, 2), Is.True);
            Assert.That(BuildableAt(root, 6, 6), Is.False,
                "A residence must touch an existing cell within one step.");
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(BuildableAt(root, 3, 1), Is.True,
                "A restaurant may touch the residence.");
            Assert.That(BuildableAt(root, 4, 2), Is.True,
                "Another plot beside the home remains a valid player choice.");
            Assert.That(BuildableAt(root, 5, 3), Is.False,
                "Touching only a meadow does not provide a resident route.");
            Assert.That(BuildableAt(root, 3, 4), Is.False,
                "Touching an oak or green tile does not provide a resident route.");
            Assert.That(root.transform.Find("Construction UI/Building choices/Next step backing/Placement guide")
                .GetComponent<Text>().text, Does.Contain("Restaurant selected"));
            Object.Destroy(root);
            yield return null;
        }

        private static bool BuildableAt(GameObject root, int column, int row) =>
            root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == $"Cell {column + 1},{row + 1}")
                .transform.Find("Buildable hint").gameObject.activeSelf;

        [UnityTest]
        public IEnumerator WorkshopCardRejectsCornerOnlyConnections()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 20), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var plot in new[] { (column: 5, row: 3),
                         (column: 4, row: 2), (column: 4, row: 4) })
            {
                Assert.That(run.TryBuild(plot.column, plot.row,
                    ConstructionCategory.Green, GreenPlanting.None,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }

            // The central oak and three new green rooms occupy every cardinal
            // neighbour of the home. Corner contact cannot place a service.
            foreach (var plot in new[] { (column: 3, row: 3),
                         (column: 5, row: 3), (column: 4, row: 2),
                         (column: 4, row: 4) })
                Assert.That(run.At(plot.column, plot.row), Is.Not.Null);
            foreach (var plot in new[] { (column: 3, row: 2),
                         (column: 3, row: 4), (column: 5, row: 2),
                         (column: 5, row: 4) })
                Assert.That(run.CheckPlacement(plot.column, plot.row,
                    ConstructionCategory.Workshop),
                    Is.EqualTo(ConstructionPlacementFailure.NeedsHomeOrConnectedStreet));

            var root = new GameObject("Corner-only workshop palette test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var openingDeadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < openingDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            controller.ToggleBuildPalette();
            var workshop = canvas.Find("Building choices/Build Workshop");
            Assert.That(workshop.GetComponent<Button>().interactable, Is.True,
                "A room may still be selected so the player sees why no plot is legal.");
            var pointer = new PointerEventData(EventSystem.current);
            workshop.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            var tooltip = canvas.Find("Context tooltip/Tooltip details")
                .GetComponent<Text>();
            Assert.That(tooltip.text, Does.Contain("Needs home/linked road"),
                "The category explanation should identify its missing connection.");
            workshop.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.True);
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("0 valid plot(s)"));
            Assert.That(root.GetComponentsInChildren<Button>(true)
                    .Where(button => button.name.StartsWith("Cell "))
                    .All(button => !button.transform.Find("Buildable hint")
                        .gameObject.activeSelf), Is.True,
                "No corner-only workshop plot may show a build hint.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuiltDayShowsNextDayGateAndRestoresPlotsAfterAdvancing()
        {
            var root = new GameObject("Daily build guidance test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var openingDeadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < openingDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            var canvas = root.transform.Find("Construction UI");
            var side = canvas.Find("Building choices");
            Assert.That(side.Find("Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("今日已建 1/1"));
            var nextDay = side.Find("Advance to next day").GetComponent<Button>();
            Assert.That(nextDay.interactable, Is.True);
            Assert.That(BuildableAt(root, 4, 2), Is.False);

            controller.ToggleBuildPalette();
            var palette = side.Find("Build palette backing");
            Assert.That(nextDay.transform.GetSiblingIndex(),
                Is.GreaterThan(palette.GetSiblingIndex()),
                "Next day must remain clickable above the open palette.");
            Assert.That(palette.Find("Palette notice")
                .GetComponent<Text>().text, Does.Contain("点击“下一天”"));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(side.Find("Feedback backing/Feedback").GetComponent<Text>()
                .text, Does.Contain("明日可放"));
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(400f, 400f) };
            var category = side.Find("Build Residence").GetComponent<EventTrigger>();
            category.OnPointerEnter(pointer);
            var tooltip = canvas.Find("Context tooltip");
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(tooltip.Find("Tooltip details").GetComponent<Text>().text,
                Does.Contain("明日可放"));
            category.OnPointerExit(pointer);
            var plot = canvas.Find("7 by 7 construction board/Cell 5,3")
                .GetComponent<EventTrigger>();
            plot.OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf,
                Is.False, "Board hover must not cover the open building palette.");

            nextDay.onClick.Invoke();
            Assert.That(controller.CurrentDay, Is.EqualTo(3));
            Assert.That(BuildableAt(root, 4, 2), Is.False,
                "A new intro day waits for the player's room choice.");
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(BuildableAt(root, 4, 2), Is.True,
                "The daily allowance must reopen after Next day.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonInspectionPersistsAndResidenceRemainsAvailableOnReload()
        {
            var savePath = Path.Combine(Application.temporaryCachePath,
                "construction-pigeon-inspection-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(savePath);
            var root = new GameObject("Pigeon inspection persistence test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(new ConstructionRunModel(), store);
                yield return null;
                Assert.That(controller.AdvanceDay(), Is.True);
                Assert.That(controller.TrySelect(ConstructionCategory.Green,
                    GreenPlanting.Meadow), Is.True);
                Assert.That(controller.TryPlace(4, 3), Is.True);
                Assert.That(controller.AdvanceDay(), Is.True);
                Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True,
                    "Pigeon inspection is narrative context, not a building lock.");
                Assert.That(controller.TryPlace(4, 3), Is.False);
                Assert.That(store.TryLoad(out var saved), Is.True);
                Assert.That(saved.PigeonMeadowInspected, Is.True);

                Object.Destroy(root);
                yield return null;
                root = new GameObject("Reloaded pigeon inspection test");
                controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(saved, store);
                yield return null;
                Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
                Assert.That(controller.TryPlace(3, 2), Is.True);
                Assert.That(controller.Stage, Is.EqualTo(
                    ConstructionStoryStage.BuildRestaurant));
            }
            finally
            {
                Object.Destroy(root);
                if (File.Exists(savePath)) File.Delete(savePath);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonArrivalIntroducesOneResidentWithVisibleUtilizationStatus()
        {
            var root = new GameObject("Construction resident test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(LegacyFillRun());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            var visitor = root.transform.Find(
                "Construction UI/Building choices/Waiting visitor");
            Assert.That(visitor, Is.Not.Null);
            Assert.That(visitor.gameObject.activeSelf, Is.False,
                "The resident beat waits until the flock has been inspected.");
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True,
                "The player can choose a home before inspecting the flock.");
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(visitor.gameObject.activeSelf, Is.True);
            var pigeonFeedback = root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>();
            Assert.That(pigeonFeedback.text, Does.Contain("grass seeds"));
            Assert.That(pigeonFeedback.preferredHeight,
                Is.LessThanOrEqualTo(pigeonFeedback.rectTransform.rect.height + 1f));

            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Assert.That(visitor.gameObject.activeSelf, Is.False);
            var home = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,3");
            Assert.That(home.transform.Find("First resident").gameObject.activeSelf,
                Is.True);
            var homeStatus = FindCellMarker(home.transform, "Utilization status");
            Assert.That(homeStatus.gameObject.activeSelf, Is.True);
            Assert.That(homeStatus.Find("Status symbol").GetComponent<Text>().text,
                Is.EqualTo("!"), "A resident without a meal needs a visible warning.");
            Assert.That(homeStatus.Find("Use count").GetComponent<Text>().text,
                Is.EqualTo("0/1"));
            var arrivalFeedback = root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>();
            Assert.That(arrivalFeedback.text, Does.Contain("1 meal/day"));
            Assert.That(arrivalFeedback.preferredHeight,
                Is.LessThanOrEqualTo(arrivalFeedback.rectTransform.rect.height + 1f));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(residentActor, Is.Not.Null);
                Assert.That(residentActor.localPosition.x, Is.LessThan(0.1f),
                    "The resident should walk into the new home, not appear at its centre.");
                yield return new WaitForSecondsRealtime(1f);
                Assert.That(residentActor.localPosition.x, Is.EqualTo(0.8f)
                    .Within(0.05f));
            }
            Assert.That(controller.TryPlace(3, 2), Is.False);
            var firstRouteStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status")
                .GetComponent<Text>();
            Assert.That(firstRouteStatus.text, Does.Contain("No meal route"));
            Assert.That(firstRouteStatus.preferredHeight,
                Is.LessThanOrEqualTo(firstRouteStatus.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(firstRouteStatus.text, Does.Contain("无用餐路线"));
            Assert.That(firstRouteStatus.preferredHeight,
                Is.LessThanOrEqualTo(firstRouteStatus.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.True,
                "Workshop choice is available before the story recommends it.");

            Assert.That(controller.AdvanceDay(), Is.True,
                "A residence uses today's one construction slot.");
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(controller.TryPlace(3, 1), Is.True);
            var restaurant = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,2");
            var seats = FindCellMarker(restaurant.transform, "Utilization status/Use count")
                .GetComponent<Text>();
            Assert.That(seats.text, Is.EqualTo("1/2"));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(homeStatus.Find("Status symbol").GetComponent<Text>().text,
                Is.EqualTo("✓"),
                "The restaurant provides both a meal seat and a separate job.");
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Assert.That(FindCellMarker(home.transform, "Waste backlog").gameObject.activeSelf,
                Is.True);
            Assert.That(FindCellMarker(home.transform, "Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("2"),
                "The residence produced waste on its own construction day too.");
            Assert.That(FindCellMarker(restaurant.transform, "Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("1"));
            var walking = root.transform.Find(
                "Construction UI/7 by 7 construction board/Resident walking");
            Assert.That(walking, Is.Not.Null);
            Assert.That(walking.gameObject.activeSelf, Is.True);
            Assert.That(homeStatus.gameObject.activeSelf, Is.True);
            Assert.That(seats.text, Is.EqualTo("1/2"));
            Assert.That(controller.ObservedHumanWorkCycles, Is.EqualTo(1));
            var residentHomePosition = Vector3.zero;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(residentActor, Is.Not.Null);
                residentHomePosition = residentActor.localPosition;
                yield return new WaitForSecondsRealtime(0.16f);
                Assert.That(residentActor.localPosition.z,
                    Is.GreaterThan(residentHomePosition.z + 0.2f),
                    "The first meal must move the 3D resident toward the restaurant.");
            }
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(walking.gameObject.activeSelf, Is.False);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(Vector3.Distance(residentActor.localPosition,
                    residentHomePosition), Is.LessThan(0.05f),
                    "The resident should return home after the meal.");
            }
            var resident = home.transform.Find("First resident")
                .GetComponent<RectTransform>();
            Assert.That(resident.gameObject.activeSelf, Is.True);
            Assert.That(Mathf.Abs(resident.anchoredPosition.x), Is.LessThan(15f),
                "The resident remains in the central art area between the side rails.");
            Assert.That(resident.anchoredPosition.y, Is.EqualTo(-20f).Within(0.1f));
            Assert.That(controller.TryPlace(3, 2), Is.False,
                "Selecting an occupied residence inspects rather than rebuilds it.");
            var routeStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status").GetComponent<Text>();
            Assert.That(routeStatus.text, Does.Contain("work"));
            Assert.That(routeStatus.text, Does.Contain("meal"));
            var overlay = root.transform.Find(
                "Construction UI/7 by 7 construction board/Selected route overlay");
            Assert.That(overlay.GetComponentsInChildren<Image>()
                .Count(image => image.name == "Route segment"), Is.GreaterThan(0));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(routeStatus.text, Does.Contain("SQUIRREL"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UtilizationCornerShowsUnmetMealIdleWorkAndSpareServiceCapacity()
        {
            var root = new GameObject("Construction utilization UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(3, 2), Is.True);

            var home = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,3");
            var homeStatus = FindCellMarker(home.transform, "Utilization status");
            var homeSymbol = homeStatus.Find("Status symbol").GetComponent<Text>();
            var homeUse = homeStatus.Find("Use count").GetComponent<Text>();
            Assert.That(homeStatus.gameObject.activeSelf, Is.True);
            Assert.That(homeSymbol.text, Is.EqualTo("!"));
            Assert.That(homeUse.text, Is.EqualTo("0/1"));
            var blockedColor = homeStatus.GetComponent<Image>().color;
            Assert.That(blockedColor.r, Is.GreaterThan(blockedColor.g),
                "Missing food must appear as the red severity, not idle amber.");

            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(950f, 450f) };
            home.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            var card = root.transform.Find(
                "Construction UI/Room badge tooltip");
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(card.Find("Tooltip heading").GetComponent<Text>().text,
                Does.Contain("Residence"));
            var blockedInfo = FindCellMarker(home.transform, "Utilization status")
                .GetComponent<ConstructionRoomBadgeHover>();
            blockedInfo.OnPointerEnter(pointer);
            var blockedText = card.Find("Tooltip details").GetComponent<Text>().text;
            Assert.That(blockedText.Contains("meal") || blockedText.Contains("food"),
                Is.True, "The red badge must name the missing meal or food.");
            blockedInfo.OnPointerExit(pointer);
            home.GetComponent<EventTrigger>().OnPointerExit(pointer);

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(controller.TryPlace(3, 1), Is.True);
            Assert.That(homeSymbol.text, Is.EqualTo("✓"),
                "A restaurant can supply the resident's job and meal independently.");
            var completeColor = homeStatus.GetComponent<Image>().color;
            Assert.That(completeColor.g, Is.GreaterThan(completeColor.r));
            var restaurant = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,2");
            var mealStatus = FindCellMarker(restaurant.transform, "Utilization status");
            Assert.That(mealStatus.Find("Status symbol").GetComponent<Text>().text,
                Is.EqualTo("·"));
            Assert.That(mealStatus.Find("Use count").GetComponent<Text>().text,
                Is.EqualTo("1/2"));
            var spareColor = mealStatus.GetComponent<Image>().color;
            Assert.That(spareColor.r, Is.GreaterThan(spareColor.g),
                "One used seat and one spare seat should remain amber, not full green.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CentralOakDayAndGreenPlacementWorkInActualUi()
        {
            var root = new GameObject("Construction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Assert.That(root.GetComponentsInChildren<Button>(true)
                .Count(button => button.name.StartsWith("Cell ")), Is.EqualTo(49));
            Assert.That(root.GetComponentsInChildren<Text>(true)
                .All(label => label.font != null), Is.True);
            var starter = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            Assert.That(FindCellMarker(starter.transform, "Food stock/Food count")
                .GetComponent<Text>().text, Is.EqualTo("1/3"));
            Assert.That(ActiveAnimalCount(starter.transform).text,
                Is.EqualTo("1"));
            var oakIcon = starter.transform.Find("Room icon").GetComponent<Image>();
            Assert.That(oakIcon.sprite, Is.Not.Null);
            var initialOakSize = oakIcon.rectTransform.sizeDelta.x;

            var openingStory = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            Assert.That(openingStory.text, Does.Contain("Click the oak to inspect food"));
            Assert.That(controller.TryPlace(3, 3), Is.False,
                "Inspecting the starter oak must not place a second room.");
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(openingStory.text, Does.Contain("End Day 1"));
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("1 nut in stock"));

            Assert.That(controller.AdvanceDay(), Is.True);
            var review = root.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(review.text, Does.Contain("Meals 1/1"));
            Assert.That(oakIcon.rectTransform.sizeDelta.x, Is.EqualTo(initialOakSize),
                "The established starter oak should already be mature.");
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TryPlace(2, 3), Is.False,
                "A second valid cell cannot be built on the same day.");
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback").GetComponent<Text>().text,
                Does.Contain("Built 1/1 today"));
            Assert.That(controller.TryPlace(6, 6), Is.False);

            var newGreen = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 5,4");
            var newGreenIcon = newGreen.transform.Find("Room icon")
                .GetComponent<Image>();
            var initialGreenSize = newGreenIcon.rectTransform.sizeDelta.x;
            Assert.That(FindCellMarker(newGreen.transform, "Food stock/Food count")
                .GetComponent<Text>().text, Is.EqualTo("0/2"));
            Assert.That(FindCellMarker(newGreen.transform, "Plant growth/Plant growth value")
                .GetComponent<Text>().text, Is.EqualTo("0/2"));
            Assert.That(controller.AdvanceDay(), Is.True);
            var pigeonFlock = FindCellMarker(newGreen.transform, "Pigeon flock");
            var threeDReady = root.GetComponent<ConstructionWorldView>().IsReady;
            Assert.That(pigeonFlock.gameObject.activeSelf, Is.EqualTo(!threeDReady));
            Assert.That(ActiveAnimalCount(newGreen.transform).text,
                Is.EqualTo("2"));
            if (!threeDReady)
                Assert.That(pigeonFlock.localScale.x, Is.LessThan(1f),
                    "The 2D fallback flock should pop into view.");
            if (threeDReady)
            {
                var threeDFlock = root.transform.Find(
                    "Construction low-poly world/Pigeon flock");
                Assert.That(threeDFlock, Is.Not.Null);
                Assert.That(threeDFlock.localPosition.y, Is.GreaterThan(1f),
                    "The 3D flock should descend onto the meadow.");
            }
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(newGreenIcon.rectTransform.sizeDelta.x,
                Is.GreaterThan(initialGreenSize),
                "A new green tile should visibly grow after the day changes.");
            Assert.That(controller.CurrentDay, Is.EqualTo(3));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            if (!threeDReady)
                Assert.That(pigeonFlock.localScale.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(pigeonFlock.GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>().text,
                Does.Contain("residence").IgnoreCase,
                "The next linear lesson should name the room to build.");
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text,
                Does.Contain("First meal tomorrow"));
            Assert.That(root.transform.Find(
                "Construction UI/Route inspection/Route status")
                .GetComponent<Text>().text,
                Does.Contain("0/3 connected mature meadows"));
            Assert.That(controller.ObservedAnimalScore, Is.EqualTo(2),
                "The available starter oak scores once on each day; the " +
                "new flock has not gained a scored food area yet.");

            var reset = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Start a new construction run");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2),
                "Any intervening action should cancel reset confirmation.");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            var freshAnimalRule = root.transform.Find(
                "Construction UI/Building choices/Last day review/Primer rules/Primer detail 0")
                .GetComponent<Text>();
            Assert.That(freshAnimalRule.text,
                Does.Contain("own tile").And.Contain("eight neighbours").And
                    .Contain("green +1").And.Contain("square +4"),
                "A fresh run starts with animal scoring before its first settlement.");
            Assert.That(freshAnimalRule.text.ToLowerInvariant(),
                Does.Not.Contain("only when eaten"),
                "The primer must not teach the retired meal-scoring rule.");
            var freshResidentRule = root.transform.Find(
                "Construction UI/Building choices/Last day review/Primer rules/Primer detail 1")
                .GetComponent<Text>();
            var freshSharedRule = root.transform.Find(
                "Construction UI/Building choices/Last day review/Primer rules/Primer detail 2")
                .GetComponent<Text>();
            var firstDayRules = freshAnimalRule.text + freshResidentRule.text +
                freshSharedRule.text;
            Assert.That(freshResidentRule.text, Is.Empty);
            Assert.That(freshSharedRule.text, Is.Empty);
            Assert.That(freshResidentRule.gameObject.activeSelf, Is.False);
            Assert.That(freshSharedRule.gameObject.activeSelf, Is.False);
            Assert.That(firstDayRules, Does.Not.Contain("Cycle +1"));
            Assert.That(firstDayRules,
                Does.Not.Contain("Animal points + resident points"),
                "Resident and shared scoring are taught on later days.");
            Assert.That(review.transform.parent.gameObject.activeSelf, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidenceConditionsAreReadOnlyAndGenericHomeIsBuilt()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TutorialChapter,
                Is.EqualTo(ConstructionTutorialChapter.Residence));

            var root = new GameObject("Automatic residence rewards test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            controller.ToggleBuildPalette();
            Assert.That(controller.TrySelect(ConstructionCategory.Residence),
                Is.True);
            var conditionCopy = canvas.Find("Building choices/Residence condition heading")
                .GetComponent<Text>().text;
            foreach (var (kind, bonus) in new[]
                     { ("Standard", "+1"), ("Garden", "+1"), ("Urban", "+2") })
            {
                var condition = canvas.Find($"Building choices/Residence condition {kind}");
                Assert.That(condition, Is.Not.Null,
                    $"Missing {kind} automatic scoring condition.");
                Assert.That(condition.gameObject.activeInHierarchy, Is.True);
                Assert.That(condition.GetComponent<Button>(), Is.Null,
                    $"{kind} is not a prebuild housing choice.");
                var labels = string.Join(" ", condition.GetComponentsInChildren<Text>(true)
                    .Select(label => label.text));
                conditionCopy += " " + labels;
                Assert.That(labels, Does.Contain(bonus),
                    $"{kind} condition must explain its daily reward.");
            }
            Assert.That(conditionCopy.ToLowerInvariant(), Does.Contain("automatic"),
                "The palette should state that rewards are earned automatically.");
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            Assert.That(controller.RequestPlacement(3, 2), Is.True);
            Assert.That(canvas.Find("Building choices/Placement confirmation/Confirmation location")
                .GetComponent<Text>().text, Does.Contain("Residence"));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(run.At(3, 2).residenceKind,
                Is.EqualTo(ResidenceKind.Standard),
                "Building a residence must not require or save a subtype choice.");

            var home = canvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            home.OnPointerEnter(pointer);
            var title = canvas.Find("Room badge tooltip/Tooltip heading")
                .GetComponent<Text>().text;
            Assert.That(title, Does.Contain("Residence"));
            home.OnPointerExit(pointer);
            Assert.That(RoomBadgeDetails(root, run.At(3, 2), "Home bonus"),
                Does.Contain("cycle").IgnoreCase,
                "The home bonus badge should explain when its forecast settles.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidenceConditionCardsAndHoverTextFitBothLanguages()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);

            var root = new GameObject("Residence hover text layout test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            controller.ToggleBuildPalette();
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            var canvas = root.transform.Find("Construction UI");
            var tooltip = canvas.Find("Context tooltip");
            var title = tooltip.Find("Tooltip title").GetComponent<Text>();
            var body = tooltip.Find("Tooltip details").GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };

            foreach (var language in new[]
                     { InterfaceLanguage.Chinese, InterfaceLanguage.English })
            {
                foreach (var kind in new[] { "Standard", "Garden", "Urban" })
                {
                    var condition = canvas.Find(
                        $"Building choices/Residence condition {kind}");
                    Assert.That(condition, Is.Not.Null,
                        $"{language} {kind} scoring guide is missing.");
                    Assert.That(condition.GetComponent<Button>(), Is.Null,
                        "A scoring guide must not act as a build button.");
                    var labels = condition.GetComponentsInChildren<Text>(true)
                        .Where(label => !string.IsNullOrWhiteSpace(label.text))
                        .ToArray();
                    Assert.That(labels.Length, Is.GreaterThan(0),
                        $"{language} {kind} guide has no explanation.");
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in labels)
                        Assert.That(label.preferredHeight,
                            Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                            $"{language} {kind} guide text is clipped: {label.text}");
                }

                foreach (var category in new[]
                         {
                             ConstructionCategory.Green,
                             ConstructionCategory.Residence,
                             ConstructionCategory.Restaurant,
                             ConstructionCategory.Workshop,
                             ConstructionCategory.Waste,
                             ConstructionCategory.Street,
                             ConstructionCategory.Supermarket,
                             ConstructionCategory.Square
                         })
                {
                    var choice = canvas.Find($"Building choices/Build {category}")
                        .GetComponent<EventTrigger>();
                    choice.OnPointerEnter(pointer);
                    Assert.That(tooltip.gameObject.activeSelf, Is.True,
                        $"{language} {category} hover did not open.");
                    Canvas.ForceUpdateCanvases();
                    Assert.That(body.preferredHeight,
                        Is.LessThanOrEqualTo(body.rectTransform.rect.height + 1f),
                        $"{language} {category} hover body is clipped.");
                    var titleLeft = title.rectTransform.anchoredPosition.x -
                        title.rectTransform.rect.width * 0.5f;
                    var bodyLeft = body.rectTransform.anchoredPosition.x -
                        body.rectTransform.rect.width * 0.5f;
                    Assert.That(titleLeft, Is.EqualTo(bodyLeft).Within(0.1f),
                        $"{language} {category} hover title and body use different left edges.");
                    choice.OnPointerExit(pointer);
                }
                controller.ToggleLanguage();
            }

            Assert.That(controller.RequestPlacement(3, 2), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            var builtHome = canvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            builtHome.OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);
            var localTooltip = canvas.Find("Room badge tooltip");
            Assert.That(localTooltip.gameObject.activeSelf, Is.True);
            var localBody = localTooltip.Find("Tooltip details").GetComponent<Text>();
            Canvas.ForceUpdateCanvases();
            Assert.That(localBody.preferredHeight,
                Is.LessThanOrEqualTo(localBody.rectTransform.rect.height + 1f),
                "Built residence local tooltip body is clipped.");
            AssertTmpBackedTextReadable(localBody, "built residence");
            builtHome.OnPointerExit(pointer);

            Object.Destroy(root);
            yield return null;
        }

        private static void AssertNoPunctuationOnlyTooltipLine(Text label,
            string context)
        {
            if (!label.enabled &&
                label.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) != null)
            {
                AssertTmpBackedTextReadable(label, context);
                return;
            }
            Canvas.ForceUpdateCanvases();
            var lines = label.cachedTextGenerator.lines;
            Assert.That(lines.Count, Is.GreaterThan(0),
                $"{context} did not generate any visible text lines.");
            for (var index = 0; index < lines.Count; index++)
            {
                var start = Mathf.Clamp(lines[index].startCharIdx, 0,
                    label.text.Length);
                var end = index + 1 < lines.Count
                    ? Mathf.Clamp(lines[index + 1].startCharIdx, start,
                        label.text.Length)
                    : label.text.Length;
                var visibleLine = label.text.Substring(start, end - start).Trim();
                if (visibleLine.Length == 0) continue;
                var previousLine = index == 0 ? string.Empty :
                    label.text.Substring(lines[index - 1].startCharIdx,
                        start - lines[index - 1].startCharIdx).Trim();
                Assert.That(visibleLine.Any(char.IsLetterOrDigit), Is.True,
                    $"{context} has a punctuation-only line after [{previousLine}]: [{visibleLine}]\nFull text: {label.text}");
                Assert.That("，。；：！？）】》".IndexOf(visibleLine[0]),
                    Is.LessThan(0),
                    $"{context} starts a line with closing punctuation: {visibleLine}");
            }
        }

        private static void AssertTmpBackedTextReadable(Text label,
            string context)
        {
            Assert.That(label.gameObject.activeInHierarchy, Is.True,
                $"{context} text is hidden.");
            Assert.That(label.text, Is.Not.Empty,
                $"{context} text is empty.");
            var rendered = label.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.That(rendered, Is.Not.Null,
                $"{context} has no TMP renderer.");
            Assert.That(rendered.gameObject.activeInHierarchy, Is.True,
                $"{context} TMP renderer is hidden.");
            Assert.That(rendered.font, Is.Not.Null,
                $"{context} TMP font is missing.");
            foreach (var line in label.text.Split('\n'))
            {
                var visibleLine = line.Trim();
                if (visibleLine.Length == 0) continue;
                Assert.That(visibleLine.Any(char.IsLetterOrDigit), Is.True,
                    $"{context} has a punctuation-only source line: {visibleLine}");
                Assert.That("，。；：！？）】》".IndexOf(visibleLine[0]),
                    Is.EqualTo(-1),
                    $"{context} starts a source line with punctuation: {visibleLine}");
            }
        }

        [UnityTest]
        public IEnumerator WorkplaceHoverExplainsDistinctJobAndMealScores()
        {
            var run = LegacyFillRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Service job palette hints test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            controller.ToggleBuildPalette();
            var canvas = root.transform.Find("Construction UI");
            var tooltip = canvas.Find("Context tooltip/Tooltip details")
                .GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            foreach (var category in new[]
                     { ConstructionCategory.Workshop, ConstructionCategory.Restaurant,
                         ConstructionCategory.Supermarket })
            {
                var hover = canvas.Find($"Building choices/Build {category}")
                    .GetComponent<EventTrigger>();
                hover.OnPointerEnter(pointer);
                Assert.That(tooltip.text, Does.Contain("岗位"),
                    $"{category} must explain its job slot.");
                Assert.That(tooltip.text, Does.Contain(category switch
                {
                    ConstructionCategory.Workshop => "工作坊上班额外+2",
                    ConstructionCategory.Restaurant => "餐厅上班额外+1",
                    _ => "超市上班额外+0"
                }), $"{category} must show its distinct job satisfaction.");
                if (category != ConstructionCategory.Workshop)
                    Assert.That(tooltip.text, Does.Contain("餐位"),
                        $"{category} must explain its meal capacity.");
                if (category != ConstructionCategory.Workshop)
                    Assert.That(tooltip.text, Does.Contain(
                        category == ConstructionCategory.Restaurant
                            ? "餐厅用餐额外+1" : "超市用餐额外+0"),
                        $"{category} must show its distinct meal satisfaction.");
                Assert.That(tooltip.preferredHeight,
                    Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 1f));
                hover.OnPointerExit(pointer);
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardProgressShowsObservedBalancePoints()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 1, ConstructionCategory.Restaurant,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.AnimalScore, Is.GreaterThan(0));
            Assert.That(run.HumanScore, Is.GreaterThan(0));
            Assert.That(run.BalanceScore, Is.GreaterThan(0),
                "Both observed sides must score before a balance bonus appears.");

            var root = new GameObject("Observed balance score UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var balance = root.transform.Find(
                "Construction UI/Building choices/Board progress backing/Balance score");
            Assert.That(balance, Is.Not.Null,
                "The balance total belongs beside board progress on the right leaf.");
            Assert.That(balance.gameObject.activeInHierarchy, Is.True);
            Assert.That(balance.GetComponent<Text>().text,
                Does.Contain(run.BalanceScore.ToString()),
                "The label must use settled animal and resident points, not a forecast.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidentRoutesAreAutomaticAndInspectionCannotChangeAssignments()
        {
            var run=ConfirmedEndlessRun();Assert.That(run.TrySimulateDay(out _),Is.True);
            foreach(var item in new[]{(ConstructionCategory.Residence,4,3),(ConstructionCategory.Restaurant,4,2),(ConstructionCategory.Workshop,5,3),(ConstructionCategory.Waste,5,2),(ConstructionCategory.Supermarket,4,4)})
            {Assert.That(run.TryBuild(item.Item2,item.Item3,item.Item1,GreenPlanting.None,out _,out _,out _),Is.True);Assert.That(run.TrySimulateDay(out _),Is.True);}
            var home=run.At(4,3);var before=JsonUtility.ToJson(run.Export());
            var expected=run.PreviewHumanDay().residents.Single(person=>person.homeTileId==home.id);
            Assert.That(expected.completedCycle,Is.True);
            var root=new GameObject("Automatic resident route UI test");
            try
            {
                var controller=root.AddComponent<ConstructionPrototypeController>();controller.UseRunForTesting(run);
                yield return null;var end=Time.realtimeSinceStartup+12f;while(!controller.IsBoardCaseOpen && Time.realtimeSinceStartup<end)yield return null;
                Assert.That(controller.IsBoardCaseOpen,Is.True);Assert.That(controller.TryPlace(4,3),Is.False);yield return null;
                Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name is "Open resident dispatch" or "Resident dispatch"),Is.False);
                Assert.That(typeof(ConstructionPrototypeController).GetField("selectedHomeId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller),Is.EqualTo(home.id));
                controller.OpenResidentDispatch();Assert.That(controller.CycleResidentDispatch(false,1),Is.False);
                Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
                var after=run.PreviewHumanDay().residents.Single(person=>person.homeTileId==home.id);
                Assert.That(after.workTileId,Is.EqualTo(expected.workTileId));Assert.That(after.mealTileId,Is.EqualTo(expected.mealTileId));
                Assert.That(after.routeTileIds.First(),Is.EqualTo(home.id));Assert.That(after.routeTileIds.Last(),Is.EqualTo(home.id));
            }
            finally{Object.Destroy(root);}yield return null;
        }

        private static int ActiveRouteElementCount(Transform overlay, string name) =>
            overlay.Cast<Transform>().Count(child =>
                child.gameObject.activeSelf && child.name == name);

        private static void AssertRouteTextFits(Text status) =>
            Assert.That(status.preferredHeight,
                Is.LessThanOrEqualTo(status.rectTransform.rect.height + 1f),
                "The selected wildlife route must fit the left route panel.");

        private static void AssertActiveRouteOriginAt(Transform overlay,
            ConstructionTileData habitat)
        {
            var origin = overlay.Cast<Transform>().FirstOrDefault(child =>
                child.gameObject.activeSelf && child.name == "Route origin");
            Assert.That(origin, Is.Not.Null,
                "A same-tile meal still needs a visible animal origin marker.");
            var point = origin.GetComponent<RectTransform>().anchoredPosition;
            var cell = overlay.parent.Find(
                $"Cell {habitat.column + 1},{habitat.row + 1}")
                .GetComponent<RectTransform>();
            Assert.That(point.x, Is.EqualTo(cell.anchoredPosition.x).Within(1f));
            Assert.That(point.y, Is.EqualTo(cell.anchoredPosition.y).Within(1f));
        }

        private static void AssertAnimalCornerBadge(GameObject root,
            ConstructionTileData tile, string iconName, string expectedCount)
        {
            var cell = root.transform.Find(
                $"Construction UI/7 by 7 construction board/Cell {tile.column + 1},{tile.row + 1}");
            Assert.That(cell, Is.Not.Null);
            var animal = cell.Find(iconName);
            Assert.That(animal, Is.Not.Null, $"Missing {iconName} on {tile.id}.");
            var threeDReady = root.GetComponent<ConstructionWorldView>().IsReady;
            Assert.That(animal.gameObject.activeInHierarchy,
                Is.EqualTo(!threeDReady),
                $"{iconName} centre art should be visible only without the 3D world.");
            var image = animal.GetComponent<Image>();
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.color.a, Is.GreaterThan(0.9f),
                $"The {iconName} fallback icon must remain legible when used.");
            var count = ActiveAnimalCount(cell);
            Assert.That(count.text, Is.EqualTo(expectedCount));
            Assert.That(count.gameObject.activeInHierarchy, Is.True);
        }

        private static void AssertGreenHoverAnimal(GameObject root,
            ConstructionTileData tile, string species, int localCount)
        {
            var text = RoomBadgeDetails(root, tile, "Animal group count");
            Assert.That(text, Does.Contain($"本格栖居 {localCount} 只"));
        }

        private static Text ActiveAnimalCount(Transform cell)
        {
            var activeCounts = cell.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name == "Animal group count" &&
                    child.gameObject.activeInHierarchy).ToArray();
            Assert.That(activeCounts.Length, Is.EqualTo(1),
                "One visible species count belongs on the side rail.");
            return activeCounts[0].Find("Count").GetComponent<Text>();
        }

        private static Transform FindCellMarker(Transform cell, string path)
        {
            var slash = path.IndexOf('/');
            var name = slash < 0 ? path : path.Substring(0, slash);
            var matches = cell.GetComponentsInChildren<Transform>(true)
                .Where(child => child != cell && child.name == name).ToArray();
            var marker = matches.FirstOrDefault(child =>
                    child.gameObject.activeInHierarchy) ?? matches.FirstOrDefault();
            return slash < 0 ? marker : marker?.Find(path.Substring(slash + 1));
        }

        private static void AssertGreenHoverWithoutAnimals(GameObject root,
            ConstructionTileData tile)
        {
            Assert.That(GreenHoverDetails(root, tile), Is.Not.Empty,
                "The green room should have a concise nearby summary.");
        }

        private static string GreenHoverDetails(GameObject root,
            ConstructionTileData tile)
        {
            var canvas = root.transform.Find("Construction UI");
            var cell = canvas.Find(
                $"7 by 7 construction board/Cell {tile.column + 1},{tile.row + 1}");
            var trigger = cell.GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(900f, 500f) };
            trigger.OnPointerEnter(pointer);
            var panel = canvas.Find("Room badge tooltip");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            var details = panel.Find("Tooltip details").GetComponent<Text>();
            var text = details.text;
            Assert.That(text, Is.Not.Empty);
            Assert.That(details.preferredHeight,
                Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                $"Nearby green tooltip text is clipped: {text}");
            AssertTmpBackedTextReadable(details,
                $"green tile {tile.column + 1},{tile.row + 1}");
            trigger.OnPointerExit(pointer);
            return text;
        }

        private static string RoomBadgeDetails(GameObject root,
            ConstructionTileData tile, string badgeName)
        {
            var canvas = root.transform.Find("Construction UI");
            var cell = canvas.Find(
                $"7 by 7 construction board/Cell {tile.column + 1},{tile.row + 1}");
            var badge = FindCellMarker(cell, badgeName);
            Assert.That(badge, Is.Not.Null);
            Assert.That(badge.gameObject.activeSelf, Is.True,
                $"{badgeName} should be visible on the room.");
            var hover = badge.GetComponent<ConstructionRoomBadgeHover>();
            Assert.That(hover, Is.Not.Null);
            var panel = canvas.Find("Room badge tooltip");
            var details = panel.Find("Tooltip details").GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(900f, 500f) };
            hover.OnPointerEnter(pointer);
            Assert.That(panel.gameObject.activeSelf, Is.True);
            var text = details.text;
            Assert.That(text, Is.Not.Empty);
            Assert.That(details.preferredHeight,
                Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                $"{badgeName} nearby tooltip text is clipped: {text}");
            hover.OnPointerExit(pointer);
            return text;
        }

        private static void AssertNearbyTooltipTextFits(Transform tooltip,
            string scenario)
        {
            Assert.That(tooltip.gameObject.activeInHierarchy, Is.True, scenario);
            Canvas.ForceUpdateCanvases();
            var rim = tooltip.GetComponent<RectTransform>();
            var divider = tooltip.Find("Tooltip divider")
                .GetComponent<RectTransform>();
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            Assert.That(details.text, Is.Not.Empty, scenario);
            Assert.That(details.preferredHeight,
                Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                $"{scenario}: text exceeds the tooltip body height.");
            var dividerCorners = new Vector3[4];
            var bodyCorners = new Vector3[4];
            divider.GetWorldCorners(dividerCorners);
            details.rectTransform.GetWorldCorners(bodyCorners);
            var dividerBottom = rim.InverseTransformPoint(dividerCorners[0]).y;
            var bodyTop = rim.InverseTransformPoint(bodyCorners[1]).y;
            var bodyBottom = rim.InverseTransformPoint(bodyCorners[0]).y;
            Assert.That(bodyTop, Is.LessThanOrEqualTo(dividerBottom - 4f),
                $"{scenario}: text box overlaps the heading divider.");
            Assert.That(bodyBottom, Is.GreaterThanOrEqualTo(rim.rect.yMin + 4f),
                $"{scenario}: text box extends below the card.");
            Assert.That(bodyTop - details.preferredHeight,
                Is.GreaterThanOrEqualTo(rim.rect.yMin + 4f),
                $"{scenario}: rendered text extends below the card.");
        }

        private static IEnumerator WaitForBoardCaseOpen(
            ConstructionPrototypeController controller)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True,
                "The board must finish opening before its hover can appear.");
        }

        private static void AssertNoWildlifeBadgesOnEmptyPlots(
            GameObject root, ConstructionRunModel run, int expectedEmptyCount)
        {
            var board = root.transform.Find(
                "Construction UI/7 by 7 construction board");
            Assert.That(board, Is.Not.Null);
            var emptyCount = 0;
            foreach (Transform cell in board)
            {
                if (!cell.name.StartsWith("Cell ", StringComparison.Ordinal))
                    continue;
                var parts = cell.name.Substring(5).Split(',');
                var column = int.Parse(parts[0]) - 1;
                var row = int.Parse(parts[1]) - 1;
                if (run.At(column, row) != null) continue;
                emptyCount++;
                foreach (var name in new[] { "Resident squirrel", "Pigeon flock",
                             "Resident hedgehog", "Resident fox" })
                {
                    var badge = cell.Find(name);
                    Assert.That(badge, Is.Not.Null);
                    Assert.That(badge.gameObject.activeSelf, Is.False,
                        $"Empty plot {column + 1},{row + 1} shows {name}.");
                }
            }
            Assert.That(emptyCount, Is.EqualTo(expectedEmptyCount));
        }

        private static ConstructionRunModel ConfirmedEndlessRun()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(
                ConstructionRunModel.MaximumEndlessLossLimit,
                ConstructionRunModel.MaximumEndlessLossLimit), Is.True);
            return run;
        }

        private static ConstructionRunModel LegacyFillRun()
        {
            var save = new ConstructionRunModel().Export();
            save.introRulesVersion = 0;
            Assert.That(ConstructionRunModel.TryRestore(save, out var run), Is.True);
            return run;
        }
    }
}
