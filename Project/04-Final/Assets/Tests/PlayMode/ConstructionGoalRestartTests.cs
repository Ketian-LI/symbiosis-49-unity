using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionGoalRestartTests
    {
        private GameObject root;
        private ConstructionPrototypeController controller;
        private ConstructionRunModel initial;
        private ConstructionRunStore store;
        private string directory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            directory = Path.Combine(Application.temporaryCachePath,
                "goal-restart-" + Guid.NewGuid().ToString("N"));
            store = new ConstructionRunStore(Path.Combine(directory, "run.json"));
            initial = new ConstructionRunModel(ConstructionGameMode.Fill, useFillHand: true);
            root = new GameObject("Goal restart test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(initial, store,
                new ConstructionLocalLeaderboardStore(Path.Combine(directory, "ranks.json")));
            yield return null;
            yield return WaitForSelection();
            Click(Find("Special task selection/Confirm special tasks").GetComponent<Button>());
            yield return WaitForTray("initial confirmation");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(root);
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator RestartThenSkipKeepsThreeGoalsAndTheirBookmark()
        {
            var originalId = initial.RunId;
            yield return RestartFromPause();
            Assert.That(store.TryLoad(out var restarted), Is.True);
            Assert.That(restarted.RunId, Is.Not.EqualTo(originalId));
            Assert.That(restarted.SpecialTasks.Count, Is.EqualTo(3));
            yield return WaitForSelection();
            Click(Find("Special task selection/Confirm special tasks").GetComponent<Button>());
            yield return WaitForTray("restarted confirmation");
            controller.DismissOpeningTour();
            yield return null;
            AssertOverviewAndBookmark();
            Assert.That(store.TryLoad(out restarted), Is.True);
            Assert.That(restarted.FillTutorialSkipped, Is.True);
            Assert.That(restarted.IsSpecialTaskSelectionConfirmed, Is.True);
            Click(Find("Special task tray").GetComponent<Button>());
            yield return WaitForReview();
            Click(Find("Special task selection/Close special task review").GetComponent<Button>());
            yield return WaitForTray("closing review");
            AssertOverviewAndBookmark();
        }

        [UnityTest]
        public IEnumerator LeftTutorialSkipKeepsTheThreeChosenGoals()
        {
            var keys = initial.SpecialTasks.Select(task => task.Key).ToArray();
            for (var step = 0; step < 7; step++) controller.AdvanceOpeningTour();
            Assert.That(initial.OpeningTourCompleted, Is.True);
            controller.ToggleTutorialTips();
            yield return null;
            Assert.That(initial.FillTutorialSkipped, Is.True);
            Assert.That(initial.SpecialTasks.Select(task => task.Key), Is.EqualTo(keys));
            AssertOverviewAndBookmark();
        }

        [UnityTest]
        public IEnumerator SkipAndReloadPreserveTheChosenTaskIdentities()
        {
            var keys = initial.SpecialTasks.Select(task => task.Key).ToArray();
            controller.DismissOpeningTour();
            yield return null;
            AssertOverviewAndBookmark();
            Assert.That(store.TryLoad(out var restored), Is.True);
            Assert.That(restored.SpecialTasks.Select(task => task.Key), Is.EqualTo(keys));
            Assert.That(restored.SpecialTasks.All(task => task.Progress == 0), Is.True);
            controller.UseRunForTesting(restored, store);
            controller.ToggleLanguage();
            yield return null;
            AssertOverviewAndBookmark();
            Assert.That(Find("Special task selection").gameObject.activeSelf, Is.False,
                "Loading confirmed goals must not ask the player to select again.");
        }

        private IEnumerator RestartFromPause()
        {
            typeof(ConstructionPrototypeController).GetMethod("TogglePauseMenu",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            yield return null;
            var button = Find("Esc pause and settings/Pause actions/New run").GetComponent<Button>();
            Click(button);
            yield return null;
            Click(button);
            yield return null;
        }

        private void AssertOverviewAndBookmark()
        {
            var overview = Find("Building choices/Goal progress paper/Goal progress overview");
            Assert.That(overview.gameObject.activeInHierarchy, Is.True);
            Assert.That(Find("Special task tray").gameObject.activeInHierarchy, Is.True);
            for (var slot = 1; slot <= 3; slot++)
            {
                var row = overview.Find("Goal progress row " + slot);
                Assert.That(row.Find("Goal title").GetComponent<Text>().text, Is.Not.Empty);
                Assert.That(row.Find("Goal count").GetComponent<Text>().text, Is.Not.Empty);
            }
        }

        private IEnumerator WaitForSelection() => WaitFor(() =>
            Find("Special task selection")?.gameObject.activeInHierarchy == true &&
            Find("Special task selection/Confirm special tasks").GetComponent<Button>().interactable &&
            Find("Special task selection").GetComponent<CanvasGroup>().alpha >= 0.99f,
            "The fresh run did not display its three selectable goal cards.");

        private IEnumerator WaitForReview() => WaitFor(() =>
            Find("Special task selection")?.gameObject.activeInHierarchy == true &&
            Find("Special task selection/Close special task review").GetComponent<Button>().interactable &&
            Find("Special task selection").GetComponent<CanvasGroup>().alpha >= 0.99f,
            "The goal bookmark did not reopen its cards.");

        private IEnumerator WaitForTray(string phase) => WaitFor(() =>
            Find("Special task tray")?.gameObject.activeInHierarchy == true,
            "The three goals did not return to the right-hand bookmark: " + phase);

        private static IEnumerator WaitFor(Func<bool> ready, string message)
        {
            var deadline = Time.realtimeSinceStartup + 6f;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(ready(), Is.True, message);
            yield return null;
        }

        private Transform Find(string path) => root.transform.Find("Construction UI/" + path);

        private static void Click(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True, button.name);
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<Selectable>(), Is.EqualTo(button),
                button.name + " is covered by " + hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
