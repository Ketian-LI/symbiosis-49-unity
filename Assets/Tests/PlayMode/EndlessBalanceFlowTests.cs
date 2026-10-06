using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class EndlessBalanceFlowTests
    {
        [UnityTest]
        public IEnumerator CustomStartRestartAndRestoreUseSameRules()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = UnityEngine.Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            UnityEngine.Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var balance = generated.GetComponentInChildren<EndlessBalanceController>(true);
            var custom = EndlessDifficultySettings.Preset(EndlessDifficulty.Gentle);
            custom.startingCommunity = 85;
            custom.wildlifeFloor = 6;
            custom.wildlifeGraceDays = 4;
            runtime.StartNewRun(GameMode.Sandbox, custom);
            Assert.That(balance.Model.Community, Is.EqualTo(85));
            Assert.That(balance.Model.CurrentWildlifeFloor, Is.EqualTo(6));
            Assert.That(balance.Model.WildlifeGraceDays, Is.EqualTo(4));
            balance.Model.SettleDay(1, 0, 4, 5);
            runtime.RestartRun();
            Assert.That(balance.Model.Community, Is.EqualTo(85));
            Assert.That(balance.Model.CriticalWildlifeDays, Is.Zero);

            var saved = balance.Model.Export();
            runtime.RestoreSession(0d, 1, GameMode.Sandbox,
                EndlessDifficulty.Gentle, custom);
            balance.RestoreSession(saved, Array.Empty<string>());
            Assert.That(balance.Model.CurrentWildlifeFloor, Is.EqualTo(6));
            Assert.That(balance.Model.Community, Is.EqualTo(85));

            runtime.RestoreSession(0d, 1, GameMode.Sandbox,
                EndlessDifficulty.Gentle, null);
            balance.RestoreSession(null, Array.Empty<string>());
            Assert.That(balance.Model.Community, Is.EqualTo(75),
                "An enum-only older save should use its preset when editable settings are absent.");

            runtime.StartNewRun(GameMode.Research, custom);
            Assert.That(runtime.SandboxSettings.startingCommunity, Is.EqualTo(60));
            Assert.That(balance.Model.CurrentWildlifeFloor, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator ZeroCommunityEndsSandboxButNotResearch()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = UnityEngine.Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            UnityEngine.Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var balance = generated.GetComponentInChildren<EndlessBalanceController>(true);
            var reportProperty = typeof(ResidentPopulationController).GetProperty(
                nameof(ResidentPopulationController.LastReport),
                BindingFlags.Instance | BindingFlags.Public);
            var settleMethod = typeof(EndlessBalanceController).GetMethod(
                "HandleDaySettled", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(reportProperty, Is.Not.Null);
            Assert.That(settleMethod, Is.Not.Null);
            var failedCommute = new ResidentDayReport(1, 0, 4, 0f, 0,
                string.Empty, string.Empty, string.Empty);
            var settlement = new ResourceSettlement(1, 0f, 0f, 0, 0, 0f, 0f);

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            balance.RestoreSession(new EndlessBalanceSaveData
            {
                community = 4,
                lastWildlifeCount = AnimalPopulationDefaults.Total
            }, Array.Empty<string>());
            reportProperty.SetValue(residents, failedCommute);
            settleMethod.Invoke(balance, new object[] { settlement });
            Assert.That(runtime.HasActiveRun, Is.False);
            Assert.That(runtime.CurrentResults.endReason, Is.EqualTo(RunEndReason.CommunityCollapse));
            Assert.That(runtime.CurrentResults.finalCommunity, Is.Zero);

            runtime.StartNewRun(GameMode.Research);
            runtime.SetOnboardingOpen(false);
            reportProperty.SetValue(residents, failedCommute);
            settleMethod.Invoke(balance, new object[] { settlement });
            Assert.That(runtime.HasActiveRun, Is.True,
                "Sandbox community pressure must not change Research end conditions.");
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
