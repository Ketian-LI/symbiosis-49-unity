using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionOpeningTourPresentationTests
    {
        [UnityTest]
        public IEnumerator FirstFillRunFramesSevenSectionsWithoutChangingTheRun()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Opening UI tour test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);

            var canvas = root.transform.Find("Construction UI");
            var canvasRect = canvas as RectTransform;
            var tour = canvas.Find("Opening UI tour");
            Assert.That(tour, Is.Not.Null);
            Assert.That(tour.gameObject.activeInHierarchy, Is.True,
                "A new Fill run should introduce the screen before Day 2. " +
                $"self={tour.gameObject.activeSelf} canvas={canvas.gameObject.activeInHierarchy} " +
                $"mode={run.Mode} day={run.CurrentDay} finished={run.IsFinished} " +
                $"tourDone={run.OpeningTourCompleted} tipsSkipped={run.TutorialTipsSkipped} " +
                $"caseOpen={controller.IsBoardCaseOpen}");
            var frame = tour.Find("Focus frame").GetComponent<RectTransform>();
            var focusNumber = frame.Find("Focus step badge/Focus step number")
                .GetComponent<Text>();
            var card = tour.Find("Tour card");
            var title = card.Find("Tour title").GetComponent<Text>();
            var body = card.Find("Tour body").GetComponent<Text>();
            var next = card.Find("Next tour step").GetComponent<Button>();
            var skip = card.Find("Skip tour").GetComponent<Button>();
            var story = canvas.Find("Building choices/Story cue backing");
            var skipDailyTips = story.Find("Skip tutorial tips") as RectTransform;
            var preview = story.Find("Story preview").GetComponent<Text>();
            var helper = story.Find("Room function") as RectTransform;
            var tip = canvas.Find("Building choices/Last day review") as RectTransform;
            var leftLeaf = canvas.Find("Observation frame") as RectTransform;
            var rightLeaf = canvas.Find("Action frame") as RectTransform;
            var progressIcon = canvas.Find(
                "Building choices/Board progress backing/Board progress icon")
                as RectTransform;
            var guideIcon = canvas.Find(
                "Building choices/Next step backing/Selected building icon")
                as RectTransform;
            var bindings = new List<RectTransform>();
            foreach (Transform child in canvas)
                if (child.name == "West case binding" ||
                    child.name == "East case binding")
                    bindings.Add(child as RectTransform);
            Assert.That(bindings.Count, Is.EqualTo(
                SystemInfo.graphicsDeviceType ==
                    UnityEngine.Rendering.GraphicsDeviceType.Null ? 0 : 2));
            var boardBounds = Bounds(canvas.Find("7 by 7 construction board")
                as RectTransform, canvasRect);
            Assert.That(Bounds(leftLeaf, canvasRect).xMax,
                Is.EqualTo(boardBounds.xMin).Within(2f),
                "The left paper leaf must meet the board frame without a gap.");
            Assert.That(Bounds(rightLeaf, canvasRect).xMin,
                Is.EqualTo(boardBounds.xMax).Within(2f),
                "The right paper leaf must meet the board frame without a gap.");
            foreach (var leaf in new[] { leftLeaf, rightLeaf })
            {
                var leafBounds = Bounds(leaf, canvasRect);
                Assert.That(leafBounds.yMin,
                    Is.EqualTo(boardBounds.yMin).Within(2f),
                    "The paper leaf must end level with the board.");
                Assert.That(leafBounds.yMax,
                    Is.EqualTo(boardBounds.yMax).Within(2f),
                    "The paper leaf must start level with the board.");
            }
            var tipDivider = tip.Find("Review divider") as RectTransform;
            var tipRule = tip.Find("Primer rules/Primer label chip 0") as
                RectTransform;
            var routeDivider = canvas.Find("Route inspection/Route divider")
                as RectTransform;
            Assert.That(next.interactable, Is.True);
            Assert.That(skip.interactable, Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(Bounds(preview.rectTransform, canvasRect).xMin,
                Is.EqualTo(Bounds(helper, canvasRect).xMin).Within(1f),
                "The main instruction and supporting copy need one left edge.");
            Assert.That(preview.cachedTextGenerator.lineCount,
                Is.LessThanOrEqualTo(2),
                "The first instruction should not leave punctuation on a third line.");
            Assert.That(preview.preferredHeight,
                Is.LessThanOrEqualTo(preview.rectTransform.rect.height + 1f));
            var upperTipGap = Bounds(tipDivider, canvasRect).yMin -
                Bounds(tipRule, canvasRect).yMax;
            var lowerTipGap = Bounds(tipRule, canvasRect).yMin -
                Bounds(routeDivider, canvasRect).yMax;
            Assert.That(Mathf.Abs(upperTipGap - lowerTipGap),
                Is.LessThanOrEqualTo(12f),
                "The single opening tip should have balanced vertical spacing.");

            var frameImages = frame.GetComponentsInChildren<Image>(true);
            Assert.That(Array.Exists(frameImages, image =>
                image.color.r >= 0.7f && image.color.g >= 0.5f &&
                image.color.b < 0.5f && image.color.a >= 0.8f), Is.True,
                "The focus frame should be visibly yellow.");
            foreach (var image in frameImages)
                Assert.That(image.raycastTarget, Is.False,
                    "The yellow frame must not intercept board input.");

            var targets = new[]
            {
                story.Find("Today tour target") as RectTransform,
                story.Find("Score tour target") as RectTransform,
                canvas.Find("Observation ledger/Review tour target") as RectTransform,
                canvas.Find("7 by 7 construction board/Cell 4,4") as RectTransform,
                canvas.Find("Building choices/Board progress backing/Progress tour target") as RectTransform,
                canvas.Find("Building choices/Next step backing/Guide tour target") as RectTransform,
                canvas.Find("Building choices/Advance to next day") as RectTransform
            };
            var centers = new List<Vector2>();
            var cardCenters = new List<Vector2>();
            for (var step = 0; step < 7; step++)
            {
                Canvas.ForceUpdateCanvases();
                Assert.That(tour.gameObject.activeInHierarchy, Is.True,
                    $"Step {step + 1} of the seven-part tour should appear.");
                Assert.That(frame.gameObject.activeInHierarchy, Is.True);
                Assert.That(title.text, Is.Not.Empty);
                if (step == 0)
                    Assert.That(title.text, Is.EqualTo("一座城，两种生活"),
                        "The first tour step should frame the coexistence goal.");
                else
                    Assert.That(title.text, Does.StartWith("新手引导 · "));
                Assert.That(body.text, Is.Not.Empty);
                Assert.That(focusNumber.text, Is.EqualTo((step + 1).ToString()),
                    "The lesson card must visibly pair with its current focus.");
                Assert.That(focusNumber.raycastTarget, Is.False);
                Assert.That(title.text.Length, Is.LessThanOrEqualTo(24),
                    "Each section needs a short heading.");
                Assert.That(title.preferredHeight,
                    Is.LessThanOrEqualTo(title.rectTransform.rect.height + 1f),
                    $"Tour step {step + 1} title is clipped.");
                Assert.That(body.text.Length, Is.LessThanOrEqualTo(90),
                    "The tour should use a short explanation per section.");
                Assert.That(body.preferredHeight,
                    Is.LessThanOrEqualTo(body.rectTransform.rect.height + 1f),
                    $"Tour step {step + 1} body text is clipped.");
                var cardBounds = Bounds(card as RectTransform, canvasRect);
                var frameBounds = Bounds(frame, canvasRect);
                var canvasBounds = Bounds(canvasRect, canvasRect);
                var focusBadgeBounds = Bounds(focusNumber.transform.parent
                    as RectTransform, canvasRect);
                var titleBounds = Bounds(title.rectTransform, canvasRect);
                var countBounds = Bounds(card.Find("Tour count") as RectTransform,
                    canvasRect);
                var bodyBounds = Bounds(body.rectTransform, canvasRect);
                var buttonsBounds = Bounds(next.transform as RectTransform,
                    canvasRect);
                Assert.That(Mathf.Abs(titleBounds.center.y - countBounds.center.y),
                    Is.LessThanOrEqualTo(1f),
                    "Tour title and progress count need the same baseline.");
                Assert.That(titleBounds.yMin - bodyBounds.yMax,
                    Is.GreaterThanOrEqualTo(5f),
                    "Tour title and explanation need visible separation.");
                Assert.That(bodyBounds.yMin - buttonsBounds.yMax,
                    Is.GreaterThanOrEqualTo(5f),
                    "Tour explanation must not run into the buttons.");
                Assert.That(canvasBounds.Contains(cardBounds.min) &&
                    canvasBounds.Contains(cardBounds.max), Is.True,
                    $"Tour step {step + 1} card escaped the screen.");
                Assert.That(canvasBounds.Contains(focusBadgeBounds.min) &&
                    canvasBounds.Contains(focusBadgeBounds.max), Is.True,
                    $"Tour step {step + 1} number escaped the screen.");
                Assert.That(cardBounds.Overlaps(frameBounds), Is.False,
                    $"Tour step {step + 1} card obscures its highlighted target.");
                Assert.That(targets[step], Is.Not.Null);
                var targetBounds = Bounds(targets[step], canvasRect);
                Assert.That(frameBounds.Contains(targetBounds.min) &&
                    frameBounds.Contains(targetBounds.max), Is.True,
                    $"Tour step {step + 1} focus missed its target.");
                foreach (var binding in bindings)
                    Assert.That(frameBounds.Overlaps(Bounds(binding, canvasRect)),
                        Is.False,
                        $"Tour step {step + 1} focus overlaps case hardware.");
                if (step <= 2)
                    Assert.That(frameBounds.xMax,
                        Is.LessThanOrEqualTo(Bounds(leftLeaf, canvasRect).xMax - 20f),
                        $"Tour step {step + 1} focus crosses the left hinge.");
                if (step == 4 || step == 5)
                    Assert.That(frameBounds.xMin,
                        Is.GreaterThanOrEqualTo(Bounds(rightLeaf, canvasRect).xMin + 20f),
                        $"Tour step {step + 1} focus crosses the right hinge.");
                if (step == 4 || step == 5)
                {
                    var iconBounds = Bounds(step == 4 ? progressIcon : guideIcon,
                        canvasRect);
                    Assert.That(frameBounds.Contains(iconBounds.min) &&
                        frameBounds.Contains(iconBounds.max), Is.True,
                        $"Tour step {step + 1} focus cuts off its icon.");
                }
                var gap = step <= 2
                    ? cardBounds.xMin - frameBounds.xMax
                    : step == 3
                        ? frameBounds.yMin - cardBounds.yMax
                        : frameBounds.xMin - cardBounds.xMax;
                Assert.That(gap, Is.InRange(30f, 42f),
                    $"Tour step {step + 1} explanation did not follow its focus.");
                Assert.That(cardCenters.Exists(previous =>
                    Vector2.Distance(previous, cardBounds.center) < 25f), Is.False,
                    $"Tour step {step + 1} reused an earlier card position.");
                cardCenters.Add(cardBounds.center);
                if (step == 0)
                {
                    Assert.That(frameBounds.yMin - Bounds(helper, canvasRect).yMax,
                        Is.GreaterThanOrEqualTo(3f),
                        "The yellow frame crosses the supporting instruction.");
                    Assert.That(frameBounds.Overlaps(Bounds(skipDailyTips,
                        canvasRect)), Is.False,
                        "The task highlight cuts through the Skip tips button.");
                }

                var corners = new Vector3[4];
                frame.GetWorldCorners(corners);
                var center = (Vector2)(corners[0] + corners[2]) / 2f;
                Assert.That(centers.Exists(previous =>
                    Vector2.Distance(previous, center) < 25f), Is.False,
                    $"Tour step {step + 1} should frame a different region.");
                centers.Add(center);
                Assert.That(run.CurrentDay, Is.EqualTo(1));
                Assert.That(run.BuiltCount, Is.EqualTo(1),
                    "The tour must not place a room or advance the simulation.");
                next.onClick.Invoke();
            }

            Assert.That(tour.gameObject.activeSelf, Is.False);
            Assert.That(run.OpeningTourCompleted, Is.True);
            Assert.That(run.TutorialTipsSkipped, Is.False,
                "Finishing the screen tour should preserve the daily lessons.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.OpeningTourCompleted, Is.True,
                "Continue should not replay a completed screen tour.");
            Assert.That(restored.TutorialTipsSkipped, Is.False);
            Object.Destroy(root);
            yield return null;

            var continuedRoot = new GameObject("Completed Day 1 opening tour test");
            var continuedController =
                continuedRoot.AddComponent<ConstructionPrototypeController>();
            continuedController.UseRunForTesting(restored);
            continuedController.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(continuedController);
            Assert.That(continuedRoot.transform.Find(
                    "Construction UI/Opening UI tour").gameObject.activeSelf,
                Is.False, "A completed Day 1 tour must not replay on Continue.");
            Object.Destroy(continuedRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkippingTourPreservesRequiredRoomGateAndDoesNotReplay()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Skipped opening UI tour test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);

            var tour = root.transform.Find("Construction UI/Opening UI tour");
            Assert.That(tour.gameObject.activeInHierarchy, Is.True,
                $"self={tour.gameObject.activeSelf} mode={run.Mode} day={run.CurrentDay} " +
                $"tourDone={run.OpeningTourCompleted} tipsSkipped={run.TutorialTipsSkipped} " +
                $"caseOpen={controller.IsBoardCaseOpen}");
            var skip = tour.Find("Tour card/Skip tour").GetComponent<Button>();
            skip.onClick.Invoke();
            Assert.That(tour.gameObject.activeSelf, Is.False);
            Assert.That(run.OpeningTourCompleted, Is.True);
            Assert.That(run.TutorialTipsSkipped, Is.False,
                "Skipping the screen tour must not skip later lessons.");

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(tour.gameObject.activeSelf, Is.False,
                "The first-day tour must not return on Day 2.");
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Green));
            Assert.That(run.CanAdvanceConstructionDay, Is.False);
            Assert.That(controller.AdvanceDay(), Is.False,
                "The tour Skip button must not bypass the required build.");
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.False);
            var dailyLesson = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            Assert.That(dailyLesson.text, Is.Not.Empty,
                "The Day 2 room lesson should remain after skipping the screen tour.");

            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var continued), Is.True);
            Assert.That(continued.OpeningTourCompleted, Is.True);
            Object.Destroy(root);
            yield return null;

            var continuedRoot = new GameObject("Continued opening UI tour test");
            var continuedController =
                continuedRoot.AddComponent<ConstructionPrototypeController>();
            continuedController.UseRunForTesting(continued);
            continuedController.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(continuedController);
            Assert.That(continuedRoot.transform.Find(
                    "Construction UI/Opening UI tour").gameObject.activeSelf,
                Is.False, "Continue should not replay the first-day tour.");
            Object.Destroy(continuedRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PriorSkipAndEndlessModeDoNotForceTheOpeningTour()
        {
            var skippedRun = new ConstructionRunModel();
            skippedRun.SetTutorialTipsSkipped(true);
            var skippedRoot = new GameObject("Previously skipped tips UI test");
            var skippedController =
                skippedRoot.AddComponent<ConstructionPrototypeController>();
            skippedController.UseRunForTesting(skippedRun);
            yield return WaitForOpenCase(skippedController);
            Assert.That(skippedRoot.transform.Find(
                    "Construction UI/Opening UI tour").gameObject.activeSelf,
                Is.False, "An existing Skip tips choice should be respected.");
            Object.Destroy(skippedRoot);
            yield return null;

            var endlessRoot = new GameObject("Endless mode opening tour test");
            var endlessController =
                endlessRoot.AddComponent<ConstructionPrototypeController>();
            endlessController.UseRunForTesting(
                new ConstructionRunModel(ConstructionGameMode.Endless));
            yield return WaitForOpenCase(endlessController);
            Assert.That(endlessRoot.transform.Find(
                    "Construction UI/Opening UI tour").gameObject.activeSelf,
                Is.False, "Endless mode has no scripted building tutorial.");
            Object.Destroy(endlessRoot);
            yield return null;
        }

        private static IEnumerator WaitForOpenCase(
            ConstructionPrototypeController controller)
        {
            // IsBoardCaseOpen starts as true before Start builds the UI and
            // begins the opening animation. Observe at least one frame first.
            yield return null;
            var deadline = Time.realtimeSinceStartup + 5f;
            while ((controller.transform.Find("Construction UI/Opening UI tour") == null ||
                    !controller.IsBoardCaseOpen) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            yield return null;
        }

        private static Rect Bounds(RectTransform rect, RectTransform root)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var low = root.InverseTransformPoint(corners[0]);
            var high = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(low.x, high.x),
                Mathf.Min(low.y, high.y), Mathf.Max(low.x, high.x),
                Mathf.Max(low.y, high.y));
        }
    }
}
