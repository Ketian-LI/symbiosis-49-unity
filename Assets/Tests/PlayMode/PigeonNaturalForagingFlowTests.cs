using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class PigeonNaturalForagingFlowTests
    {
        [UnityTest]
        public IEnumerator PigeonNaturalMealRequiresVisibleArrival()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var pigeon = generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                .Single(item => item.name == "Pigeon 01");
            Assert.That(generated.GetComponentInChildren<PigeonNaturalForagingController>(true), Is.Not.Null);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            Assert.That(needs.Model.Animals["pigeon-01"].ateToday, Is.False);
            var start = pigeon.transform.position;
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!pigeon.IsRespondingToFood && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(pigeon.IsRespondingToFood, Is.True,
                "The pigeon should leave its home room for a natural source.");
            Assert.That(needs.Model.Animals["pigeon-01"].ateToday, Is.False,
                "Dispatch alone must not award a meal.");

            while (!needs.Model.Animals["pigeon-01"].ateToday &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(needs.Model.Animals["pigeon-01"].ateToday, Is.True);
            Assert.That(Vector3.Distance(start, pigeon.transform.position), Is.GreaterThan(0.1f));
            // After eating, the same travel state carries the bird back home.
            deadline = Time.realtimeSinceStartup + 5f;
            while (pigeon.IsRespondingToFood && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(pigeon.IsRespondingToFood, Is.False);
            Assert.That(Vector3.Distance(start, pigeon.transform.position), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator GroundAnimalsDoNotReceiveInvisibleNaturalFoodAtSettlement()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            needs.Model.Reset();
            foreach (var squirrel in generated.GetComponentsInChildren<SquirrelDemoAgent>(true))
                squirrel.RestoreCache(0);
            food.Model.Restore(new[]
            {
                new NaturalFoodSaveData { roomId = "shrub-a", kind = "Insect", portions = 1 },
                new NaturalFoodSaveData { roomId = "oak-a", kind = "Nut", portions = 1 },
                new NaturalFoodSaveData { roomId = "canteen-b", kind = "DiscardedFood", portions = 1 }
            });

            typeof(AnimalNeedsController).GetMethod("AllocateAvailableFood",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(needs, null);

            Assert.That(needs.Model.Animals["hedgehog-01"].ateToday, Is.False);
            Assert.That(needs.Model.Animals["squirrel-01"].ateToday, Is.False);
            Assert.That(needs.Model.Animals["fox-01"].ateToday, Is.False);
            Assert.That(food.Model.PortionsIn("shrub-a", NaturalFoodKind.Insect), Is.EqualTo(1));
            Assert.That(food.Model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
            Assert.That(food.Model.PortionsIn("canteen-b", NaturalFoodKind.DiscardedFood), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SquirrelStocksCacheOnlyAfterAVisibleNutTrip()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            squirrel.RestoreCache(0);
            food.RestoreSession(new[]
            {
                new NaturalFoodSaveData { roomId = "oak-a", kind = "Nut", portions = 1 }
            });
            var start = squirrel.transform.position;
            runtime.SetSpeed(1);

            var deadline = Time.realtimeSinceStartup + 10f;
            while (!squirrel.IsRespondingToFood && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(squirrel.IsRespondingToFood, Is.True);
            Assert.That(food.Model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1),
                "Starting the journey must not remove a nut from the room.");
            Assert.That(squirrel.CachePortions, Is.Zero);

            while (squirrel.CachePortions == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(squirrel.CachePortions, Is.EqualTo(1));
            Assert.That(food.Model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.Zero);
            Assert.That(Vector3.Distance(start, squirrel.transform.position), Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator HedgehogReceivesMealOnlyAfterReachingInsects()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var hedgehog = generated.GetComponentsInChildren<HedgehogDemoAgent>(true).First();
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.RestoreSession(SimulationClockModel.DawnSeconds +
                SimulationClockModel.DaySeconds + 1d, 1, GameMode.Sandbox);
            runtime.SetSpeed(0);
            yield return null;
            food.RestoreSession(new[]
            {
                new NaturalFoodSaveData { roomId = "shrub-a", kind = "Insect", portions = 1 }
            });
            var start = hedgehog.transform.position;
            runtime.SetSpeed(1);

            var deadline = Time.realtimeSinceStartup + 10f;
            while (!hedgehog.IsForaging && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(hedgehog.IsForaging, Is.True);
            Assert.That(needs.Model.Animals["hedgehog-01"].ateToday, Is.False);
            Assert.That(food.Model.PortionsIn("shrub-a", NaturalFoodKind.Insect), Is.EqualTo(1));

            while (!needs.Model.Animals["hedgehog-01"].ateToday &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(needs.Model.Animals["hedgehog-01"].ateToday, Is.True);
            Assert.That(food.Model.PortionsIn("shrub-a", NaturalFoodKind.Insect), Is.Zero);
            Assert.That(Vector3.Distance(start, hedgehog.transform.position), Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator HungryFoxScavengesOnlyAfterWalkingToDiscardedFood()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var fox = generated.GetComponentsInChildren<FoxDemoAgent>(true).First();
            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            var predation = generated.GetComponentInChildren<FoxPredationController>(true);
            generated.GetComponentInChildren<WorkerPasserbyFeedingController>(true).enabled = false;
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.RestoreSession(SimulationClockModel.DawnSeconds +
                SimulationClockModel.DaySeconds + 1d, 1, GameMode.Sandbox);
            // The phase transition first moves this animal to its active room.
            // Choose a source after that relocation so the journey has a
            // deliberately reachable destination.
            yield return new WaitForSeconds(3f);
            var startRoom = (string)typeof(FoxPredationController).GetMethod("FindRoomId",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(predation, new object[] { fox.transform.position });
            var sourceRoom = navigation.NavigationMapFor(fox).NeighboursOf(startRoom).FirstOrDefault();
            Assert.That(sourceRoom, Is.Not.Null, "The fox needs a reachable neighbouring room for this journey test.");
            food.RestoreSession(new[]
            {
                new NaturalFoodSaveData { roomId = sourceRoom, kind = "DiscardedFood", portions = 1 }
            });
            needs.RestoreSession(new[]
            {
                new AnimalNeedSaveData { id = "fox-01", species = "Fox", hungerDays = 1 }
            });
            var start = fox.transform.position;

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!fox.IsHunting && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(fox.IsHunting, Is.True, "The fox should depart for accessible discarded food.");
            Assert.That(food.Model.PortionsIn(sourceRoom, NaturalFoodKind.DiscardedFood), Is.EqualTo(1),
                "Dispatch must not grant the meal before arrival.");

            while (food.Model.PortionsIn(sourceRoom, NaturalFoodKind.DiscardedFood) > 0 &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(food.Model.PortionsIn(sourceRoom, NaturalFoodKind.DiscardedFood), Is.Zero);
            Assert.That(needs.Model.Animals["fox-01"].ateToday, Is.True);
            Assert.That(Vector3.Distance(start, fox.transform.position), Is.GreaterThan(0.2f));
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
