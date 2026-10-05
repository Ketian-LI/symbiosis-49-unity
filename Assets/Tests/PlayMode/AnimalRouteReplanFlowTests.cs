using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class AnimalRouteReplanFlowTests
    {
        [UnityTest]
        public IEnumerator InFlightGroundAnimalStopsAtPortClosedAtDawn()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            // Keep the authored layout static while isolating the dawn closure.
            generated.GetComponentInChildren<RoomLayoutEditorController>(true)
                .RestoreLastMovementDay(runtime.Clock.DayNumber);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds - 0.1d);
            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            var board = generated.Find("7x7 Modular Floor");
            Assert.That(navigation.NavigationMap.TryGetConnectionPoint(
                "pigeon-b", "central-park", out var crossing), Is.True);

            var start = board.TransformPoint(new Vector3(crossing.x - 0.30f, 0.32f, crossing.y));
            var gate = board.TransformPoint(new Vector3(crossing.x, 0.32f, crossing.y));
            var destination = board.TransformPoint(new Vector3(crossing.x + 0.45f, 0.32f, crossing.y));
            squirrel.transform.position = start;
            Assert.That(squirrel.BeginFoodMission(
                new[] { gate, destination }, new[] { start }, 0f, () => false), Is.True);

            runtime.AdvanceSimulation(0.2d);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(navigation.NavigationMap.TryGetConnectionPoint(
                "pigeon-b", "central-park", out _), Is.False);

            var deadline = Time.realtimeSinceStartup + 0.9f;
            while (Time.realtimeSinceStartup < deadline)
                yield return null;
            var finalLocal = board.InverseTransformPoint(squirrel.transform.position);
            Assert.That(squirrel.IsRespondingToFood, Is.False);
            Assert.That(finalLocal.x, Is.LessThanOrEqualTo(crossing.x + 0.06f),
                "A mission started yesterday must not cross today's closed animal port.");
        }

        [UnityTest]
        public IEnumerator RerouteKeepsFoodAlreadyCarriedBySquirrel()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            var start = squirrel.transform.position;
            Assert.That(squirrel.BeginFoodMission(
                new[] { start }, new[] { squirrel.CachePosition }, 0f, () => true), Is.True);
            yield return null;
            Assert.That(squirrel.HasFoodInTransit, Is.True);
            Assert.That(squirrel.CancelFoodMissionForRouteChange(), Is.True);
            Assert.That(squirrel.IsRespondingToFood, Is.False);
            Assert.That(squirrel.HasFoodInTransit, Is.True);
            Assert.That(squirrel.CanStoreFood, Is.False);
            Assert.That(squirrel.BeginCacheReturn(new[] { squirrel.CachePosition }), Is.True);
        }

        [UnityTest]
        public IEnumerator EventWithNoClosedConnectionDoesNotInterruptCurrentTrip()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var placements = RoomLayoutData.All.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
            }).ToArray();
            var shrub = placements.Single(room => room.id == "shrub-a");
            var house = placements.Single(room => room.id == "residence-e");
            (shrub.column, house.column) = (house.column, shrub.column);
            (shrub.row, house.row) = (house.row, shrub.row);
            Assert.That(editor.RestoreLayout(placements), Is.True);

            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            editor.RestoreLastMovementDay(runtime.Clock.DayNumber);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            _ = navigation.NavigationMap; // Day 2 park-edge closure.
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            var start = squirrel.transform.position;
            Assert.That(squirrel.BeginFoodMission(
                new[] { start + Vector3.right }, new[] { start }, 3f, () => false), Is.True);

            editor.RestoreLastMovementDay(runtime.Clock.DayNumber);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            _ = navigation.NavigationMap; // Day 3 shrub event faces the outer board edge.
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(3));
            Assert.That(squirrel.IsRespondingToFood, Is.True,
                "A newly opened edge and an inert disturbance must not cancel a valid trip.");
        }

        [UnityTest]
        public IEnumerator LayoutChangeInvalidatesAllActiveGroundRoutes()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            var hedgehog = generated.GetComponentsInChildren<HedgehogDemoAgent>(true).First();
            var fox = generated.GetComponentsInChildren<FoxDemoAgent>(true).First();
            hedgehog.SetActivityEnabled(true);
            fox.SetActivityEnabled(true);
            var squirrelPosition = squirrel.transform.position;
            Assert.That(squirrel.BeginFoodMission(new[] { squirrelPosition + Vector3.right },
                new[] { squirrelPosition }, 3f, () => false), Is.True);
            Assert.That(hedgehog.BeginForaging(new[] { hedgehog.transform.position + Vector3.right },
                () => { }), Is.True);
            Assert.That(fox.BeginHunt(new[] { fox.transform.position + Vector3.right },
                squirrel.transform, () => false, () => { }), Is.True);

            var placements = RoomLayoutData.All.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
            }).ToArray();
            var shrub = placements.Single(room => room.id == "shrub-a");
            var house = placements.Single(room => room.id == "residence-e");
            (shrub.column, house.column) = (house.column, shrub.column);
            (shrub.row, house.row) = (house.row, shrub.row);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            Assert.That(editor.RestoreLayout(placements), Is.True);
            Assert.That(squirrel.IsRespondingToFood, Is.False);
            Assert.That(hedgehog.IsForaging, Is.False);
            Assert.That(fox.IsHunting, Is.False);
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
