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
    public sealed class AnimalHomeMovementFlowTests
    {
        [UnityTest]
        public IEnumerator RestartDuringRoomRelocationRestoresOriginalHomesAndLivingAnimals()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var pigeon = generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                .Single(agent => agent.name == "Pigeon 01");
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true)
                .Single(agent => agent.name == "Squirrel 01");
            var hedgehog = generated.GetComponentsInChildren<HedgehogDemoAgent>(true)
                .Single(agent => agent.name == "Hedgehog 01");
            var fox = generated.GetComponentsInChildren<FoxDemoAgent>(true)
                .Single(agent => agent.name == "Fox 01");
            var pigeonStart = pigeon.SpawnPosition;
            var squirrelStart = squirrel.SpawnPosition;
            var cacheStart = squirrel.CachePosition;
            var hedgehogStart = hedgehog.SpawnPosition;
            var foxStart = fox.SpawnPosition;

            var placements = RoomLayoutData.All.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
            }).ToArray();
            Swap(placements, "pigeon-a", "residence-e");
            Swap(placements, "oak-a", "residence-f");
            Swap(placements, "shrub-a", "residence-g");
            Swap(placements, "shared-j", "residence-h");
            Assert.That(editor.RestoreLayout(placements), Is.True);
            Assert.That(Vector3.Distance(fox.SpawnPosition, foxStart), Is.GreaterThan(1f));
            pigeon.Kill();
            Assert.That(squirrel.Vitality.Kill(AnimalDeathCause.Starvation), Is.True);

            // A new run can be requested before the previous room animation ends.
            runtime.StartNewRun(GameMode.Sandbox);
            Assert.That(Vector3.Distance(pigeon.SpawnPosition, pigeonStart), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(squirrel.SpawnPosition, squirrelStart), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(squirrel.CachePosition, cacheStart), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(hedgehog.SpawnPosition, hedgehogStart), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(fox.SpawnPosition, foxStart), Is.LessThan(0.001f));
            Assert.That(pigeon.IsAlive, Is.True);
            Assert.That(squirrel.IsAlive, Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Vector3.Distance(fox.transform.position, foxStart), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator FourSpeciesAndSquirrelCacheFollowMovedHomeRooms()
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
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var pigeons = generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                .Where(bird => bird.name is "Pigeon 01" or "Pigeon 02" or "Pigeon 10")
                .OrderBy(bird => bird.name).ToArray();
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true)
                .Single(agent => agent.name == "Squirrel 01");
            var hedgehog = generated.GetComponentsInChildren<HedgehogDemoAgent>(true)
                .Single(agent => agent.name == "Hedgehog 01");
            var fox = generated.GetComponentsInChildren<FoxDemoAgent>(true)
                .Single(agent => agent.name == "Fox 01");
            Assert.That(pigeons.Length, Is.EqualTo(3));

            var pigeonSpawns = pigeons.Select(bird => bird.SpawnPosition).ToArray();
            var squirrelSpawn = squirrel.SpawnPosition;
            var hedgehogSpawn = hedgehog.SpawnPosition;
            var foxSpawn = fox.SpawnPosition;
            var oldCache = squirrel.CachePosition;
            var trackedIds = new[] { "pigeon-a", "oak-a", "shrub-a", "shared-j" };
            var oldCenters = trackedIds.ToDictionary(id => id,
                id => rooms.Single(room => room.Spec.Id == id).VisualRoot.position);

            var placements = RoomLayoutData.All.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
            }).ToArray();
            Swap(placements, "pigeon-a", "residence-e");
            Swap(placements, "oak-a", "residence-f");
            Swap(placements, "shrub-a", "residence-g");
            Swap(placements, "shared-j", "residence-h");
            Assert.That(editor.RestoreLayout(placements), Is.True);

            Vector3 Delta(string id) => rooms.Single(room => room.Spec.Id == id)
                .VisualRoot.position - oldCenters[id];
            var pigeonDelta = Delta("pigeon-a");
            var squirrelDelta = Delta("oak-a");
            var hedgehogDelta = Delta("shrub-a");
            var foxDelta = Delta("shared-j");
            foreach (var delta in new[] { pigeonDelta, squirrelDelta, hedgehogDelta, foxDelta })
                Assert.That(delta.sqrMagnitude, Is.GreaterThan(1f));

            for (var index = 0; index < pigeons.Length; index++)
                AssertShift(pigeons[index].SpawnPosition, pigeonSpawns[index], pigeonDelta,
                    pigeons[index].name);
            AssertShift(squirrel.SpawnPosition, squirrelSpawn, squirrelDelta, "squirrel");
            AssertShift(squirrel.CachePosition, oldCache, squirrelDelta, "squirrel cache");
            AssertShift(hedgehog.SpawnPosition, hedgehogSpawn, hedgehogDelta, "hedgehog");
            AssertShift(fox.SpawnPosition, foxSpawn, foxDelta, "fox");

            // Relocation is animated, rather than changing only the hidden home anchor.
            var foxBeforeAnimation = fox.transform.position;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Vector3.Distance(fox.transform.position, foxBeforeAnimation),
                Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator SquirrelAwayFromHomeStaysInItsCurrentRoomWhenOnlyHomeMoves()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true)
                .Single(agent => agent.name == "Squirrel 01");
            squirrel.SetActivityEnabled(false);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var park = rooms.Single(room => room.Spec.Id == "central-park");
            var home = rooms.Single(room => room.Spec.Id == "oak-a");
            squirrel.transform.position = park.VisualRoot.position + Vector3.up * 0.30f;
            var beforePosition = squirrel.transform.position;
            var beforeHome = squirrel.SpawnPosition;
            var beforeCache = squirrel.CachePosition;
            var beforeHomeCenter = home.VisualRoot.position;

            var placements = RoomLayoutData.All.Select(room => new RoomPlacementData
            {
                id = room.Id, column = room.Column, row = room.Row, quarterTurns = 0
            }).ToArray();
            Swap(placements, "oak-a", "residence-e");
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            Assert.That(editor.RestoreLayout(placements), Is.True);
            var homeDelta = home.VisualRoot.position - beforeHomeCenter;
            Assert.That(homeDelta.sqrMagnitude, Is.GreaterThan(1f));
            AssertShift(squirrel.SpawnPosition, beforeHome, homeDelta, "squirrel home");
            AssertShift(squirrel.CachePosition, beforeCache, homeDelta, "squirrel cache");

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Vector3.Distance(squirrel.transform.position, beforePosition),
                Is.LessThan(0.05f),
                "Moving the home alone must not teleport an animal standing in the fixed park.");
        }

        [UnityTest]
        public IEnumerator SquirrelAwayFromHomeFollowsTheRoomItOccupies()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true)
                .Single(agent => agent.name == "Squirrel 01");
            squirrel.SetActivityEnabled(false);
            var room = generated.GetComponentsInChildren<RoomView>(true)
                .Single(view => view.Spec.Id == "residence-e");
            squirrel.transform.position = room.VisualRoot.position + Vector3.up * 0.30f;
            var beforePosition = squirrel.transform.position;
            var beforeHome = squirrel.SpawnPosition;
            var beforeCache = squirrel.CachePosition;
            var beforeRoomCenter = room.VisualRoot.position;

            var placements = RoomLayoutData.All.Select(spec => new RoomPlacementData
            {
                id = spec.Id, column = spec.Column, row = spec.Row, quarterTurns = 0
            }).ToArray();
            Swap(placements, "residence-e", "residence-f");
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            Assert.That(editor.RestoreLayout(placements), Is.True);
            var roomDelta = room.VisualRoot.position - beforeRoomCenter;
            Assert.That(roomDelta.sqrMagnitude, Is.GreaterThan(1f));
            Assert.That(Vector3.Distance(squirrel.SpawnPosition, beforeHome), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(squirrel.CachePosition, beforeCache), Is.LessThan(0.001f));

            yield return new WaitForSecondsRealtime(0.6f);
            AssertShift(squirrel.transform.position, beforePosition, roomDelta,
                "squirrel standing in moved room");
        }

        private static void Swap(RoomPlacementData[] placements, string firstId, string secondId)
        {
            var first = placements.Single(room => room.id == firstId);
            var second = placements.Single(room => room.id == secondId);
            (first.column, second.column) = (second.column, first.column);
            (first.row, second.row) = (second.row, first.row);
        }

        private static void AssertShift(Vector3 actual, Vector3 before, Vector3 delta, string name)
        {
            Assert.That(Vector3.Distance(actual - before, delta), Is.LessThan(0.001f), name);
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
