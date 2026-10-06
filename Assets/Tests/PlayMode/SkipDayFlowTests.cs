using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class SkipDayFlowTests
    {
        [UnityTest]
        public IEnumerator FastForwardMovesActorsByTheSameSimulationTimeAsTheClock()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            ConfirmSwap(bootstrap, editor, generated.GetComponentsInChildren<RoomView>(true),
                "pigeon-c", "shared-f");
            Assert.That(runtime.TrySkipToNextDay(), Is.True);

            var before = runtime.Clock.TotalSeconds;
            runtime.AdvanceSimulation(0.1d);
            var clockAdvance = runtime.Clock.TotalSeconds - before;
            Assert.That(clockAdvance, Is.GreaterThan(Time.maximumDeltaTime));
            Assert.That(runtime.ActorPresentationDeltaTime,
                Is.EqualTo((float)clockAdvance).Within(0.001f),
                "At 72×, Unity's capped Time.deltaTime must not lag behind the game clock.");
        }

        [UnityTest]
        public IEnumerator SkipFromDawnCompletesInAboutFiveRealSeconds()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);

            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence);
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            ConfirmSwap(bootstrap, editor, generated.GetComponentsInChildren<RoomView>(true),
                "pigeon-c", "shared-f");
            Assert.That(runtime.TrySkipToNextDay(), Is.True);

            var startedAt = Time.realtimeSinceStartup;
            while (runtime.IsDaySkipping && Time.realtimeSinceStartup - startedAt < 7f)
            {
                yield return null;
            }

            var elapsed = Time.realtimeSinceStartup - startedAt;
            TestContext.WriteLine($"Skip-to-next-day elapsed real time: {elapsed:F2}s");
            Assert.That(runtime.IsDaySkipping, Is.False);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(elapsed, Is.InRange(4.5f, 6f));
        }

        [UnityTest]
        public IEnumerator AQualifiedLayoutDecisionIsRequiredBeforeSkippingOrNaturalDawn()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);

            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Assert.That(generated, Is.Not.Null);

            // A PlayMode test must not overwrite the player's save or profile.
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence);
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(editor, Is.Not.Null);
            var tutorial = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            Assert.That(tutorial, Is.Not.Null);
            var tutorialCard = tutorial.GetComponentsInChildren<Image>(true)
                .Single(image => image.name == "One Sentence Tutorial Card");
            Assert.That(tutorialCard.sprite, Is.EqualTo(TutorialPanelVisualCatalog.GetSprite()));

            runtime.StartNewRun(GameMode.Sandbox);
            // Keep the tutorial's PlayerPrefs untouched while allowing the
            // simulation to run during this automated check.
            runtime.SetOnboardingOpen(false);
            yield return null;
            var skipDayButton = generated.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Skip To Next Day");
            Assert.That(skipDayButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(skipDayButton.interactable, Is.False);
            Assert.That(runtime.TrySkipToNextDay(), Is.False);

            editor.SetGuidedTargetRoom(null);
            editor.EnterEditing();
            var canKeep = editor.CanHoldLayout;
            Assert.That(editor.CanConfirm, Is.EqualTo(canKeep));
            if (canKeep)
            {
                editor.ConfirmEditing();
                Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.Hold));
                runtime.StartNewRun(GameMode.Sandbox);
                runtime.SetOnboardingOpen(false);
            }
            else
            {
                editor.CancelEditing();
            }
            Assert.That(runtime.TrySkipToNextDay(), Is.False);

            ConfirmSwap(bootstrap, editor, generated.GetComponentsInChildren<RoomView>(true),
                "pigeon-c", "shared-f");
            Assert.That(runtime.HasDailySpatialDecision, Is.True);
            Assert.That(runtime.TrySkipToNextDay(), Is.True);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(runtime.HasDailySpatialDecision, Is.False);
            Assert.That(runtime.TrySkipToNextDay(), Is.False,
                "Yesterday's layout decision must not unlock another day.");

            editor.EnterEditing();
            var directionalRoom = generated.GetComponentsInChildren<RoomView>(true)
                .Single(item => item.Spec.Id == "trash-b");
            var screen = bootstrap.LayoutCamera.WorldToScreenPoint(
                directionalRoom.VisualRoot.position);
            var point = new Vector2(screen.x, screen.y);
            InvokeDrag(editor, "HandleDragStarted", directionalRoom, point);
            editor.RotateSelected();
            InvokeDrag(editor, "HandleDragEnded", directionalRoom, point);
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
            Assert.That(runtime.HasDailySpatialDecision, Is.True,
                "Rotating a directional animal passage must count as a real spatial decision.");

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds * 2d);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1),
                "Ordinary-speed play must also wait for the daily spatial decision.");
            Assert.That(runtime.Clock.TotalSeconds,
                Is.LessThan(SimulationClockModel.CycleSeconds));
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FeedAndRearrangeAreMutuallyExclusiveDailyActions()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var park = rooms.Single(room => room.Spec.Id == "central-park");
            var position = park.VisualRoot.TransformPoint(new Vector3(0.55f, 0.16f, 0.55f));
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.None));
            Assert.That(feeding.TryPlaceFood(string.Empty, position), Is.False);
            Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.None),
                "An invalid click must not spend the action.");
            Assert.That(feeding.TryPlaceFood("central-park", position), Is.True);
            Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.Feed));
            Assert.That(editor.FreeRearrangementAvailable, Is.False);

            editor.EnterEditing();
            var moving = rooms.Single(room => room.Spec.Id == "pigeon-c");
            var target = rooms.Single(room => room.Spec.Id == "shared-f");
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(moving.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(target.VisualRoot.position);
            InvokeDrag(editor, "HandleDragStarted", moving, new Vector2(from.x, from.y));
            InvokeDrag(editor, "HandleDragging", moving, new Vector2(to.x, to.y));
            InvokeDrag(editor, "HandleDragEnded", moving, new Vector2(to.x, to.y));
            Assert.That(editor.CanConfirm, Is.False,
                "A feed must prevent a second, layout action on the same day.");
            editor.CancelEditing();
            Assert.That(runtime.TrySkipToNextDay(), Is.True,
                "A successful feed must satisfy the daily decision gate.");
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.None));
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.GreaterThan(0));
            Assert.That(editor.FreeRearrangementAvailable, Is.True);

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            ConfirmSwap(bootstrap, editor, rooms, "pigeon-c", "shared-f");
            Assert.That(runtime.TodayAction, Is.EqualTo(DailyActionKind.Rearrange));
            Assert.That(feeding.CanActivateFeedingMode, Is.False);
            Assert.That(feeding.TryPlaceFood("central-park", position), Is.False,
                "A rearrangement must prevent feeding even if the cooldown is ready.");
            Assert.That(feeding.LastManualFeedingDay, Is.Zero);
            Assert.That(runtime.TrySkipToNextDay(), Is.True);
        }

        private static void ConfirmSwap(UrbanWildlifeBootstrap bootstrap,
            RoomLayoutEditorController editor, RoomView[] rooms, string movingId, string targetId)
        {
            editor.SetGuidedTargetRoom(null);
            editor.EnterEditing();
            var moving = rooms.Single(item => item.Spec.Id == movingId);
            var target = rooms.Single(item => item.Spec.Id == targetId);
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(moving.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(target.VisualRoot.position);
            foreach (var (methodName, position) in new[]
                     {
                         ("HandleDragStarted", new Vector2(from.x, from.y)),
                         ("HandleDragging", new Vector2(to.x, to.y)),
                         ("HandleDragEnded", new Vector2(to.x, to.y))
                     })
            {
                InvokeDrag(editor, methodName, moving, position);
            }
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
        }

        private static void InvokeDrag(RoomLayoutEditorController editor,
            string methodName, RoomView room, Vector2 position)
        {
            var method = typeof(RoomLayoutEditorController).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(editor, new object[] { room, position });
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
