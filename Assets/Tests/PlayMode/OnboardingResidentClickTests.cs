using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class OnboardingResidentClickTests
    {
        [UnityTest]
        public IEnumerator ClickingSquirrelSpotlightAdvancesTheIsolatedOpening()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence); // Leave the player's save untouched.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var onboarding = generated.GetComponentInChildren<FirstRunOnboardingController>(true);
            var overlay = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            var squirrel = generated.GetComponentInChildren<SquirrelDemoAgent>(true);
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(squirrel, Is.Not.Null);

            runtime.StartNewRun(GameMode.Sandbox);
            yield return new WaitForSecondsRealtime(1f);

            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening));
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(1));
            Assert.That(generated.GetComponentsInChildren<SquirrelDemoAgent>().Length, Is.EqualTo(1));
            var oak = generated.GetComponentInChildren<RoomView>();
            Assert.That(oak.Spec.Id, Is.EqualTo("oak-a"));
            var oakScreen = bootstrap.LayoutCamera.WorldToViewportPoint(oak.VisualRoot.position);
            Assert.That(oakScreen.x, Is.EqualTo(0.5f).Within(0.03f));
            Assert.That(oakScreen.y, Is.EqualTo(0.5f).Within(0.03f));
            var spotlight = overlay.transform.Find("First Run Onboarding/Tutorial Spotlight");
            var button = spotlight.GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            Assert.That(spotlight.GetComponent<Image>().raycastTarget, Is.True);

            var hud = overlay.GetComponent<UrbanWildlifeHud>();
            var raycaster = hud.GetComponent<GraphicRaycaster>();
            var screenPosition = RectTransformUtility.WorldToScreenPoint(
                bootstrap.LayoutCamera,
                spotlight.GetComponent<RectTransform>().TransformPoint(
                    spotlight.GetComponent<RectTransform>().rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.SameAs(spotlight.gameObject),
                "The spotlight must receive a click before the speed controls below it.");

            button.onClick.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening));
            Assert.That(button.interactable, Is.True,
                "The second spotlight must let the player inspect the actual oak room.");
            Assert.That(overlay.CurrentInstruction, Does.Contain("room").Or.Contain("房间"));
            overlay.ContinueRequested?.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening),
                "The opening cannot be skipped by pressing Continue before room inspection.");

            button.onClick.Invoke();
            Assert.That(button.interactable, Is.False);
            Assert.That(overlay.CurrentHeader, Does.Contain("Oak").Or.Contain("橡树"));
            Assert.That(overlay.CurrentInstruction, Does.Contain("type").Or.Contain("类型"));
            Assert.That(overlay.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Oak Room Fact Icon")
                .gameObject.activeSelf, Is.True);
            overlay.ContinueRequested?.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening));
            Assert.That(overlay.CurrentInstruction, Does.Contain("nut").Or.Contain("坚果"));
            overlay.ContinueRequested?.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening));
            var clock = hud.transform.Find("Day Night Clock");
            Assert.That(clock, Is.Not.Null,
                "The date lesson must reveal the real gameplay clock, not a duplicate illustration.");
            Assert.That(clock.Find("Simulation Speed").gameObject.activeSelf, Is.False);
            Assert.That(clock.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(overlay.CurrentInstruction, Does.Contain("day").Or.Contain("天"));
            Assert.That(overlay.transform.Find("First Run Onboarding/Tutorial Spotlight")
                .gameObject.activeSelf, Is.True);
            var clockTime = runtime.Clock.TotalSeconds;
            overlay.ContinueRequested?.Invoke();
            Assert.That(overlay.CurrentInstruction, Does.Contain("next").Or.Contain("次日"));
            Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(clockTime),
                "Reading the date lesson must not secretly advance simulation time.");
            overlay.ContinueRequested?.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.OakOpening));
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(2));
            Assert.That(overlay.CurrentHeader, Does.Contain("home").Or.Contain("住宅"));
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke(); // Inspect the home.
            Assert.That(overlay.CurrentInstruction, Does.Contain("home").Or.Contain("住宅"));
            overlay.ContinueRequested?.Invoke(); // Reveal one resident.
            Assert.That(generated.GetComponentsInChildren<UrbanWildlifeRooms.People.CitizenDemoAgent>().Length,
                Is.EqualTo(1));
            button.onClick.Invoke(); // Meet the resident.
            Assert.That(overlay.CurrentInstruction, Does.Contain("work").Or.Contain("工作"));
            overlay.ContinueRequested?.Invoke(); // Reveal a workplace.
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(3));
            button.onClick.Invoke();
            overlay.ContinueRequested?.Invoke(); // Reveal a food shop.
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(4));
            button.onClick.Invoke();
            Assert.That(overlay.CurrentInstruction, Does.Contain("home").Or.Contain("回家"));
            overlay.ContinueRequested?.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.Complete));
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(49));
            Assert.That(runtime.OnboardingOpen, Is.False);
            Assert.That(hud.transform.Find("Minimal Gameplay HUD/Day Night Clock"), Is.Not.Null);
            Assert.That(clock.Find("Simulation Speed").gameObject.activeSelf, Is.True);
            Assert.That(clock.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        }

        [UnityTest]
        public IEnumerator SkippingRestartingAndReturningToMenuRestoreTheWholeWorld()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var onboarding = generated.GetComponentInChildren<FirstRunOnboardingController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var overlay = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            var originalClockIndex = hud.transform.Find("Minimal Gameplay HUD/Day Night Clock")
                .GetSiblingIndex();
            runtime.StartNewRun(GameMode.Sandbox);
            Assert.That(onboarding.IsOakOpeningActive, Is.True);
            Assert.That(hud.transform.Find("Minimal Gameplay HUD").gameObject.activeSelf, Is.False);

            overlay.HighlightedSquirrelRequested?.Invoke();
            overlay.HighlightedRoomRequested?.Invoke();
            overlay.ContinueRequested?.Invoke();
            overlay.ContinueRequested?.Invoke();
            Assert.That(hud.transform.Find("Day Night Clock"), Is.Not.Null);

            onboarding.Skip();
            Assert.That(onboarding.IsOakOpeningActive, Is.False);
            var restoredClock = hud.transform.Find("Minimal Gameplay HUD/Day Night Clock");
            Assert.That(restoredClock, Is.Not.Null);
            Assert.That(restoredClock.GetSiblingIndex(), Is.EqualTo(originalClockIndex));
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(49));
            Assert.That(hud.transform.Find("Minimal Gameplay HUD").gameObject.activeSelf, Is.True);

            runtime.StartNewRun(GameMode.Sandbox);
            Assert.That(onboarding.IsOakOpeningActive, Is.True);
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(1));
            runtime.ReturnToDesktop();
            Assert.That(onboarding.IsOakOpeningActive, Is.False);
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(49));
            Assert.That(onboarding.OakOpeningPending, Is.True);
            runtime.ResumeFromDesktop();
            Assert.That(onboarding.IsOakOpeningActive, Is.True);
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HumanStoryUsesTemporaryTilesAndRestoresEveryRoomAndResident()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var onboarding = generated.GetComponentInChildren<FirstRunOnboardingController>(true);
            var overlay = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            var originalPositions = new Dictionary<string, Vector3>();
            foreach (var room in generated.GetComponentsInChildren<RoomView>(true))
                originalPositions[room.Spec.Id] = room.VisualRoot.position;
            var citizen = generated.GetComponentInChildren<CitizenPopulationPresenter>(true).PrimaryAgent;
            var originalHome = citizen.SpawnPosition;
            var originalTime = runtime.Clock.TotalSeconds;

            overlay.HighlightedSquirrelRequested?.Invoke();
            overlay.HighlightedRoomRequested?.Invoke();
            for (var index = 0; index < 4; index++) overlay.ContinueRequested?.Invoke();
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(2));
            Assert.That(generated.GetComponentsInChildren<CitizenDemoAgent>().Length, Is.EqualTo(0));
            overlay.ContinueRequested?.Invoke();
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(2),
                "Continue alone cannot skip room inspection.");
            overlay.HighlightedRoomRequested?.Invoke();
            overlay.ContinueRequested?.Invoke();
            Assert.That(generated.GetComponentsInChildren<CitizenDemoAgent>().Length, Is.EqualTo(1));
            overlay.HighlightedResidentRequested?.Invoke();
            overlay.ContinueRequested?.Invoke();
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(3));
            overlay.HighlightedRoomRequested?.Invoke();
            overlay.ContinueRequested?.Invoke();
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(4));
            Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(originalTime));

            onboarding.Skip();
            Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(49));
            foreach (var room in generated.GetComponentsInChildren<RoomView>())
                Assert.That(Vector3.Distance(room.VisualRoot.position, originalPositions[room.Spec.Id]),
                    Is.LessThan(0.001f), room.Spec.Id);
            Assert.That(Vector3.Distance(citizen.SpawnPosition, originalHome), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator ResearchDateLessonDoesNotClaimARequiredDailyAction()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var overlay = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            runtime.StartNewRun(GameMode.Research);
            Assert.That(runtime.Mode, Is.EqualTo(GameMode.Research));
            overlay.HighlightedSquirrelRequested?.Invoke();
            overlay.HighlightedRoomRequested?.Invoke();
            overlay.ContinueRequested?.Invoke(); // Room function
            overlay.ContinueRequested?.Invoke(); // Dial
            overlay.ContinueRequested?.Invoke(); // Next-day rule
            Assert.That(overlay.CurrentInstruction,
                Does.Contain("fixed timer").Or.Contain("固定时长"));
            Assert.That(overlay.CurrentInstruction, Does.Not.Contain("Make one spatial choice"));
            Assert.That(overlay.CurrentInstruction, Does.Not.Contain("每天完成一次"));
        }

        [UnityTest]
        public IEnumerator SavedOpeningReturnsAfterSceneReload()
        {
            var oldDirectory = SessionPersistenceController.SaveDirectoryOverrideForTests;
            var directory = Path.Combine(Application.temporaryCachePath,
                "oak-opening-" + System.Guid.NewGuid().ToString("N"));
            SessionPersistenceController.SaveDirectoryOverrideForTests = directory;
            try
            {
                Directory.CreateDirectory(directory);
                yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
                var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                    .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
                var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
                runtime.StartNewRun(GameMode.Sandbox);
                persistence.SaveNow();
                var saved = JsonUtility.FromJson<SessionSaveData>(File.ReadAllText(persistence.SavePath));
                Assert.That(saved.storyOakOpeningPending, Is.True);

                yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
                generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                    .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
                var onboarding = generated.GetComponentInChildren<FirstRunOnboardingController>(true);
                Assert.That(runtime.HasResumableRun, Is.True);
                Assert.That(runtime.AtDesktop, Is.True);
                runtime.ResumeFromDesktop();
                Assert.That(onboarding.IsOakOpeningActive, Is.True);
                Assert.That(generated.GetComponentsInChildren<RoomView>().Length, Is.EqualTo(1));
            }
            finally
            {
                var livePersistence = Object.FindFirstObjectByType<SessionPersistenceController>(
                    FindObjectsInactive.Include);
                if (livePersistence != null) livePersistence.gameObject.SetActive(false);
                SessionPersistenceController.SaveDirectoryOverrideForTests = oldDirectory;
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
