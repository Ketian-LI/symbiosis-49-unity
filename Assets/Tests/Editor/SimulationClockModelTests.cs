using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class SimulationClockModelTests
    {
        [Test]
        public void SixMinuteCycleUsesConfirmedPhaseDurations()
        {
            Assert.That(SimulationClockModel.CycleSeconds, Is.EqualTo(360d));
            Assert.That(SimulationClockModel.DawnSeconds, Is.EqualTo(30d));
            Assert.That(SimulationClockModel.DaySeconds, Is.EqualTo(150d));
            Assert.That(SimulationClockModel.DuskSeconds, Is.EqualTo(30d));
            Assert.That(SimulationClockModel.NightSeconds, Is.EqualTo(150d));
        }

        [TestCase(0d, DayPhase.Dawn)]
        [TestCase(29.99d, DayPhase.Dawn)]
        [TestCase(30d, DayPhase.Day)]
        [TestCase(179.99d, DayPhase.Day)]
        [TestCase(180d, DayPhase.Dusk)]
        [TestCase(209.99d, DayPhase.Dusk)]
        [TestCase(210d, DayPhase.Night)]
        [TestCase(359.99d, DayPhase.Night)]
        public void PhaseBoundariesMatchDesign(double seconds, DayPhase expected)
        {
            var clock = new SimulationClockModel();
            clock.Restore(seconds);
            Assert.That(clock.Phase, Is.EqualTo(expected));
        }

        [Test]
        public void SpeedMultiplierAdvancesClockAndDayNumber()
        {
            var clock = new SimulationClockModel();
            clock.Advance(90d, 4f);
            Assert.That(clock.DayNumber, Is.EqualTo(2));
            Assert.That(clock.SecondsIntoDay, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void PausedSpeedDoesNotAdvanceClock()
        {
            var clock = new SimulationClockModel();
            clock.Advance(10d, 0f);
            Assert.That(clock.TotalSeconds, Is.EqualTo(0d));
        }

        [Test]
        public void DaySkipStopsAtNextDawnAndRestoresPreviousSpeed()
        {
            var previousTimeScale = Time.timeScale;
            var gameObject = new GameObject("Day Skip Runtime");
            try
            {
                var runtime = gameObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);
                runtime.SetSpeed(2);
                runtime.Clock.Advance(90d, 1f);

                Assert.That(runtime.TrySkipToNextDay(), Is.True);
                Assert.That(runtime.DaySkipTargetSeconds, Is.EqualTo(360d));
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(54));
                Assert.That(runtime.SelectedSpeedMultiplier, Is.EqualTo(2));
                runtime.AdvanceSimulation(4.9d);
                Assert.That(runtime.IsDaySkipping, Is.True);
                runtime.AdvanceSimulation(0.1d);

                Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(360d).Within(0.001d));
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
                Assert.That(runtime.IsDaySkipping, Is.False);
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void FullDaySkipFinishesInFiveUnpausedSeconds()
        {
            var previousTimeScale = Time.timeScale;
            var gameObject = new GameObject("Full Day Skip Runtime");
            try
            {
                var runtime = gameObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);

                Assert.That(runtime.TrySkipToNextDay(), Is.True);
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(GameRuntimeController.DaySkipSpeed));
                runtime.AdvanceSimulation(4.9d);
                Assert.That(runtime.IsDaySkipping, Is.True);
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
                runtime.AdvanceSimulation(0.1d);
                Assert.That(runtime.IsDaySkipping, Is.False);
                Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(360d).Within(0.001d));
                Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void DaySkipIsUnavailableInResearchOrPausedTutorial()
        {
            var previousTimeScale = Time.timeScale;
            var gameObject = new GameObject("Day Skip Restrictions");
            try
            {
                var runtime = gameObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);
                runtime.SetOnboardingOpen(true);
                Assert.That(runtime.TrySkipToNextDay(), Is.False);
                runtime.SetOnboardingOpen(false);
                runtime.SetMode(GameMode.Research);
                Assert.That(runtime.TrySkipToNextDay(), Is.False);
                Assert.That(runtime.SpeedMultiplier, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void PausingDaySkipFreezesTimeAndSpeedSelectionCancelsIt()
        {
            var previousTimeScale = Time.timeScale;
            var gameObject = new GameObject("Day Skip Pause");
            try
            {
                var runtime = gameObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);
                Assert.That(runtime.TrySkipToNextDay(), Is.True);

                runtime.TogglePauseMenu();
                runtime.AdvanceSimulation(10d);
                Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(0d));
                Assert.That(runtime.IsDaySkipping, Is.True);

                runtime.ContinueGame();
                runtime.SetSpeed(4);
                Assert.That(runtime.IsDaySkipping, Is.False);
                runtime.AdvanceSimulation(1d);
                Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(4d));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
