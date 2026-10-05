using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RunResultsDataTests
    {
        [Test]
        public void InitializationOpensMainMenuWithoutInventingAResumableRun()
        {
            var runtimeObject = new GameObject("Fresh Main Menu Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);

                Assert.That(runtime.AtDesktop, Is.True);
                Assert.That(runtime.HasActiveRun, Is.False);
                Assert.That(runtime.HasResumableRun, Is.False);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void StartingResearchFromNoCameraMenuCreatesAResumableDayOneRun()
        {
            Assert.That(BuildVariantSettings.SupportsResearch, Is.True,
                "The editor test assembly represents the default no-camera build.");

            var runtimeObject = new GameObject("Research Menu Entry Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Research);

                Assert.That(runtime.AtDesktop, Is.False);
                Assert.That(runtime.Mode, Is.EqualTo(GameMode.Research));
                Assert.That(runtime.HasActiveRun, Is.True);
                Assert.That(runtime.HasResumableRun, Is.True);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void DesktopSettingsCloseBackToTheDesktopInsteadOfOpeningPauseMenu()
        {
            var runtimeObject = new GameObject("Desktop Settings Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);

                runtime.OpenSettings();
                Assert.That(runtime.AtDesktop, Is.True);
                Assert.That(runtime.SettingsOpen, Is.True);
                Assert.That(runtime.PauseMenuOpen, Is.True);

                runtime.CloseSettings();
                Assert.That(runtime.AtDesktop, Is.True);
                Assert.That(runtime.SettingsOpen, Is.False);
                Assert.That(runtime.PauseMenuOpen, Is.False);
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void TotalAnimalDeathsSumsSpeciesWithoutNegativeCounts()
        {
            var data = new RunResultsData
            {
                pigeonDeaths = 2,
                squirrelDeaths = 1,
                hedgehogDeaths = -3,
                foxDeaths = 2
            };

            Assert.That(data.TotalAnimalDeaths, Is.EqualTo(5));
        }

        [Test]
        public void EndReportNamesTheRecordedCausesAndDoesNotInventLegacyCauses()
        {
            var data = new RunResultsData
            {
                pigeonDeaths = 2,
                hedgehogDeaths = 1,
                animalDeathBreakdown = new System.Collections.Generic.List<AnimalDeathBreakdownData>
                {
                    new() { species = WildlifeSpecies.Pigeon, predation = 1, starvation = 1 }
                }
            };

            Assert.That(data.BreakdownOf(WildlifeSpecies.Pigeon).LocalizedLine(true),
                Is.EqualTo("鸽子 2：饥饿 1、狐狸捕食 1"));
            Assert.That(data.BreakdownOf(WildlifeSpecies.Hedgehog).LocalizedLine(true),
                Is.EqualTo("刺猬 1：原因未记录 1"));
            Assert.That(data.BreakdownOf(WildlifeSpecies.Fox).LocalizedLine(false),
                Is.EqualTo("Fox: no deaths"));
        }

        [Test]
        public void EndReasonsAreLocalizedWithoutVictoryOrFailureLabels()
        {
            var deaths = new RunResultsData { endReason = RunEndReason.AnimalDeathLimit };
            var resources = new RunResultsData { endReason = RunEndReason.NegativeResourceBalance };
            var workers = new RunResultsData
            {
                endReason = RunEndReason.InsufficientWorkers,
                lastWorkingResidents = 2,
                requiredWorkingResidents = 3
            };

            Assert.That(deaths.LocalizedEndReason(true), Is.EqualTo("动物死亡达到 3"));
            Assert.That(deaths.LocalizedEndReason(false), Is.EqualTo("3 animal deaths"));
            Assert.That(resources.LocalizedEndReason(true), Is.EqualTo("资源点结算为负数"));
            Assert.That(resources.LocalizedEndReason(false), Is.EqualTo("Negative resource balance"));
            Assert.That(workers.LocalizedEndReason(true), Is.EqualTo("上班人数不足：2/3"));
            Assert.That(workers.LocalizedEndReason(false), Is.EqualTo("Too few workers: 2/3"));
        }

        [Test]
        public void RuntimePausesForResultsAndRestartReturnsToDayOne()
        {
            var runtimeObject = new GameObject("Results Runtime Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.Clock.Advance(SimulationClockModel.CycleSeconds * 4.2d, 1f);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(5));

                runtime.EndRun(new RunResultsData
                {
                    endReason = RunEndReason.AnimalDeathLimit,
                    daysSurvived = 5
                });
                Assert.That(runtime.ResultsOpen, Is.True);
                Assert.That(runtime.IsPaused, Is.True);

                runtime.RestartRun();
                Assert.That(runtime.ResultsOpen, Is.False);
                Assert.That(runtime.CurrentResults, Is.Null);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void ActiveSandboxRunCanRestartFromPauseWithoutChangingMode()
        {
            var runtimeObject = new GameObject("Sandbox Pause Restart Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                var restartRequested = false;
                runtime.RestartRequested += () => restartRequested = true;
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);
                runtime.Clock.Advance(SimulationClockModel.CycleSeconds * 2.4d, 1f);
                runtime.TogglePauseMenu();

                Assert.That(runtime.PauseMenuOpen, Is.True);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(3));

                runtime.RestartRun();

                Assert.That(restartRequested, Is.True);
                Assert.That(runtime.Mode, Is.EqualTo(GameMode.Sandbox));
                Assert.That(runtime.HasActiveRun, Is.True);
                Assert.That(runtime.PauseMenuOpen, Is.False);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void FailedRunCannotResumeFromMainMenu()
        {
            var runtimeObject = new GameObject("Completed Run Menu Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.Clock.Advance(SimulationClockModel.CycleSeconds * 3.1d, 1f);
                runtime.EndRun(new RunResultsData
                {
                    endReason = RunEndReason.NegativeResourceBalance,
                    daysSurvived = 4
                });

                runtime.ReturnToMainMenuFromResults();
                Assert.That(runtime.AtDesktop, Is.True);
                Assert.That(runtime.ResultsOpen, Is.False);
                Assert.That(runtime.HasEndedRun, Is.True);
                Assert.That(runtime.HasResumableRun, Is.False);

                runtime.ResumeFromDesktop();
                Assert.That(runtime.AtDesktop, Is.False);
                Assert.That(runtime.CurrentResults, Is.Null);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
                Assert.That(runtime.HasResumableRun, Is.True);
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void RunEndedCanCommitRecordBeforeResultsArePresented()
        {
            var runtimeObject = new GameObject("Run Record Event Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.RunEnded += results =>
                {
                    results.isNewRecord = true;
                    results.bestSurvivalDays = results.daysSurvived;
                    runtime.SetBestSurvivalDays(results.daysSurvived);
                };

                var completed = new RunResultsData
                {
                    endReason = RunEndReason.AnimalDeathLimit,
                    daysSurvived = 9
                };
                runtime.EndRun(completed);

                Assert.That(runtime.CurrentResults, Is.SameAs(completed));
                Assert.That(runtime.CurrentResults.isNewRecord, Is.True);
                Assert.That(runtime.CurrentResults.bestSurvivalDays, Is.EqualTo(9));
                Assert.That(runtime.BestSurvivalDays, Is.EqualTo(9));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }
    }
}
