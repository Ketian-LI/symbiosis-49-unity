using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionResultReviewTests
    {
        private GameObject root;
        private string directory;
        private ConstructionPrototypeController controller;
        private ConstructionRunStore save;
        private ConstructionLocalLeaderboardStore ranks;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            directory = Path.Combine(Application.temporaryCachePath,
                "result-review-" + Guid.NewGuid().ToString("N"));
            save = new ConstructionRunStore(Path.Combine(directory, "run.json"));
            ranks = new ConstructionLocalLeaderboardStore(Path.Combine(directory, "ranks.json"));
            var run = FinishedRun();
            root = new GameObject("Result review test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run, save, ranks);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(root);
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator CollapsePreservesFinishedBoardAndCanReturnAfterRefresh()
        {
            var result = Find("Construction final result");
            var before = File.ReadAllText(Path.Combine(directory, "run.json"));
            Click(result.Find("Review finished layout").GetComponent<Button>());
            yield return null;
            Assert.That(result.gameObject.activeSelf, Is.False);
            var review = Find("Finished layout review");
            Assert.That(review.gameObject.activeSelf, Is.True);
            controller.ToggleLanguage();
            yield return null;
            Assert.That(result.gameObject.activeSelf, Is.False,
                "Refreshing the finished board must not reopen the result overlay.");
            Assert.That(controller.TryPlace(4, 4), Is.False,
                "Reviewing must not allow building after the run has finished.");
            Click(review.Find("Return to results").GetComponent<Button>());
            Assert.That(result.gameObject.activeSelf, Is.True);
            Assert.That(review.gameObject.activeSelf, Is.False);
            yield return null;
            Click(result.Find("Result card/Open local rankings").GetComponent<Button>());
            yield return null;
            Click(Find("Local rankings overlay/Local rankings card/Review layout from local rankings")
                .GetComponent<Button>());
            Assert.That(Find("Local rankings overlay").gameObject.activeSelf, Is.False);
            Assert.That(result.gameObject.activeSelf, Is.False);
            Assert.That(controller.TryPlace(4, 3), Is.False,
                "Inspecting an existing home must preserve the finished board.");
            Assert.That(File.ReadAllText(Path.Combine(directory, "run.json")), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator RestartRulesRemainClickableAfterRefreshingFinishedResult()
        {
            Click(Find("Construction final result/Result card/New run after result").GetComponent<Button>());
            controller.ToggleLanguage();
            yield return null;
            var confirm = Find("Construction Endless rules overlay/Rules card border/Rules card/Confirm ending rules")
                .GetComponent<Button>();
            Click(confirm);
            yield return null;
            Assert.That(save.TryLoad(out var restarted), Is.True);
            Assert.That(restarted.IsFinished, Is.False);
            Assert.That(restarted.BuiltCount, Is.EqualTo(1));
            Assert.That(restarted.CurrentDay, Is.EqualTo(1));
            Assert.That(Find("Construction final result").gameObject.activeSelf, Is.False);
            Assert.That(ranks.RankingsFor(ConstructionGameMode.Endless, out var entries),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(entries.Count, Is.EqualTo(1), "Restart must keep the completed score.");
        }

        [UnityTest]
        public IEnumerator BadAdjacencyShowsItsAmountAndCauseDuringSettlement()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 20), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            controller.UseRunForTesting(run, save, ranks);
            controller.ToggleLanguage();
            yield return null;
            Assert.That(controller.TrySelect(ConstructionCategory.Waste), Is.True);
            Assert.That(controller.TryPlace(4, 4), Is.True);
            Assert.That(run.LastPlacementPoints, Is.EqualTo(-80));
            Assert.That(controller.AdvanceDay(), Is.True);
            yield return null;
            var deduction = root.GetComponentsInChildren<Text>()
                .FirstOrDefault(text => text.name.StartsWith("Score fly Build") &&
                    text.text.Contains("-80"));
            Assert.That(deduction, Is.Not.Null, "The deduction must appear even during other scoring batches.");
            Assert.That(deduction.text, Does.Contain("垃圾间").And.Contain("住宅"));
            Assert.That(deduction.raycastTarget, Is.False, "Feedback must not block board input.");
        }

        [UnityTest]
        public IEnumerator OnlineNicknameReceivesKeyboardAndRenamesTheActualLocalRecord()
        {
            Click(Find("Construction final result/Result card/Network leaderboard status").GetComponent<Button>());
            yield return null;
            var card = Find("Online rankings overlay/Online rankings card");
            var input = card.Find("Online nickname").GetComponent<InputField>();
            Assert.That(input.interactable, Is.True);
            AssertTopmost(input.gameObject);
            input.Select();
            input.ActivateInputField();
            yield return null;
            Assert.That(input.isFocused, Is.True);
            input.text = "";
            input.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.B, character = 'B' });
            Assert.That(input.text, Is.EqualTo("B"), "The online name field must accept physical keyboard events.");
            input.text = "橡树玩家";
            Click(card.Find("Save online nickname").GetComponent<Button>());
            Assert.That(ranks.RankingsFor(ConstructionGameMode.Endless, out var entries),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(entries[0].score.nickname, Is.EqualTo("橡树玩家"));
            Click(card.Find("Review layout from online rankings").GetComponent<Button>());
            Assert.That(Find("Online rankings overlay").gameObject.activeSelf, Is.False);
            Assert.That(Find("Construction final result").gameObject.activeSelf, Is.False);
        }

        private Transform Find(string path) => root.transform.Find("Construction UI/" + path);

        private static void Click(Button button)
        {
            AssertTopmost(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }

        private static void AssertTopmost(GameObject target)
        {
            Assert.That(target.activeInHierarchy, Is.True, target.name);
            Canvas.ForceUpdateCanvases();
            var rect = target.GetComponent<RectTransform>();
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits, Is.Not.Empty, target.name + " must receive a pointer raycast.");
            var selectable = hits[0].gameObject.GetComponentInParent<Selectable>();
            Assert.That(selectable, Is.EqualTo(target.GetComponent<Selectable>()),
                target.name + " is covered by " + hits[0].gameObject.name);
        }

        private static ConstructionRunModel FinishedRun()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 1), Is.True);
            // Set up an ecological ending without coupling this UI test to the
            // separate once-per-day construction gate.
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 5, 3, ConstructionCategory.Workshop);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 3, 4, ConstructionCategory.Residence);
            for (var day = 0; day < 3; day++) Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.IsFinished, Is.True);
            return run;
        }

        private static void Build(ConstructionRunModel run, int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category, GreenPlanting.None,
                out _, out var failure, out var placement), Is.True, failure + " / " + placement);
        }
    }
}
