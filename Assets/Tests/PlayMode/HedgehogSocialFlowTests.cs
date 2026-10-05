using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class HedgehogSocialFlowTests
    {
        [UnityTest]
        public IEnumerator FedHedgehogsMeetThenReturnToTheirOwnHomes()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            var social = generated.GetComponentInChildren<HedgehogSocialController>(true);
            var hedgehogs = generated.GetComponentsInChildren<HedgehogDemoAgent>(true)
                .OrderBy(item => item.name).ToArray();
            generated.GetComponentInChildren<FoxPredationController>(true).enabled = false;
            Assert.That(social, Is.Not.Null);
            Assert.That(hedgehogs.Length, Is.EqualTo(2));
            Assert.That(navigation.NavigationMap.TryFindRoute(
                "shared-h", "shrub-a", out _), Is.True,
                "The starting board should offer a legal animal path for a meeting.");

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            foreach (var hedgehog in hedgehogs)
                needs.RegisterPredationMeal(hedgehog);
            runtime.Clock.Restore(210d);
            runtime.SetSpeed(4);

            var deadline = Time.realtimeSinceStartup + 14f;
            while (social.LastMeetingDay != runtime.Clock.DayNumber &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(social.LastMeetingDay, Is.EqualTo(runtime.Clock.DayNumber),
                "Both hedgehogs must physically reach the shared meeting point.");

            while (social.MeetingInProgress && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(social.MeetingInProgress, Is.False);
            foreach (var hedgehog in hedgehogs)
                Assert.That(Vector3.Distance(hedgehog.transform.position,
                    hedgehog.SpawnPosition), Is.LessThan(0.6f));
        }

        [UnityTest]
        public IEnumerator HungryHedgehogsDoNotLeaveForACompanion()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var social = generated.GetComponentInChildren<HedgehogSocialController>(true);
            var hedgehogs = generated.GetComponentsInChildren<HedgehogDemoAgent>(true);
            generated.GetComponentInChildren<HedgehogForagingController>(true).enabled = false;
            generated.GetComponentInChildren<FoxPredationController>(true).enabled = false;

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.Clock.Restore(210d);
            runtime.SetSpeed(4);
            Assert.That(hedgehogs.All(needs.NeedsMealOf), Is.True);

            var deadline = Time.realtimeSinceStartup + 0.75f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(social.MeetingInProgress, Is.False);
            Assert.That(social.LastMeetingDay, Is.EqualTo(0));
            Assert.That(hedgehogs.Any(item => item.IsWaitingForCompanion), Is.False);
        }

        [UnityTest]
        public IEnumerator PredatorDefenceInterruptsACompanionTrip()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var social = generated.GetComponentInChildren<HedgehogSocialController>(true);
            var hedgehogs = generated.GetComponentsInChildren<HedgehogDemoAgent>(true);
            generated.GetComponentInChildren<FoxPredationController>(true).enabled = false;

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            foreach (var hedgehog in hedgehogs) needs.RegisterPredationMeal(hedgehog);
            runtime.Clock.Restore(210d);
            runtime.SetSpeed(4);

            var deadline = Time.realtimeSinceStartup + 3f;
            while (!social.MeetingInProgress && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(social.MeetingInProgress, Is.True);
            Assert.That(hedgehogs[0].BeginPredatorDefence(), Is.True);
            deadline = Time.realtimeSinceStartup + 0.4f;
            while (social.MeetingInProgress && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(social.MeetingInProgress, Is.False);
            Assert.That(hedgehogs.Any(item => item.IsWaitingForCompanion), Is.False);
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
