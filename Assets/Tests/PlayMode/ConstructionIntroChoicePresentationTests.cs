using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionIntroChoicePresentationTests
    {
        [UnityTest]
        public IEnumerator OpeningTeachesAnimalScoringAndPaletteGuidesTheNextRoom()
        {
            var root = new GameObject("Linear introduction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);

            var canvas = root.transform.Find("Construction UI");
            Assert.That(canvas, Is.Not.Null);
            var scoreHeading = canvas.Find(
                "Building choices/Last day review/Review heading")
                .GetComponent<Text>();
            Assert.That(scoreHeading.text, Is.EqualTo("今日提示"));
            var rules = canvas.Find(
                "Building choices/Last day review/Primer rules");
            Assert.That(rules.gameObject.activeSelf, Is.True);
            var animalRule = rules.Find("Primer detail 0").GetComponent<Text>();
            var residentRule = rules.Find("Primer detail 1").GetComponent<Text>();
            var sharedRule = rules.Find("Primer detail 2").GetComponent<Text>();
            Assert.That(animalRule.text, Does.Contain("绿地").And.Contain("+1")
                .And.Contain("广场").And.Contain("+4"));
            Assert.That(animalRule.text, Does.Contain("自身及周围八格")
                .And.Contain("可用食源"),
                "Day 1 should explain where nearby food-area points come from.");
            Assert.That(residentRule.text, Is.Empty);
            Assert.That(sharedRule.text, Is.Empty);
            Assert.That(residentRule.gameObject.activeSelf, Is.False);
            Assert.That(sharedRule.gameObject.activeSelf, Is.False);
            Assert.That(rules.Find("Primer label chip 1").gameObject.activeSelf,
                Is.False);
            Assert.That(rules.Find("Primer label chip 2").gameObject.activeSelf,
                Is.False);
            var openingRules = animalRule.text + residentRule.text + sharedRule.text;
            Assert.That(openingRules, Does.Not.Contain("循环+1"));
            Assert.That(openingRules,
                Does.Not.Contain("动物得分 + 居民得分"),
                "The shared-score formula belongs to a later lesson.");
            foreach (var detail in new[] { animalRule, residentRule, sharedRule })
            {
                Assert.That(detail.preferredHeight,
                    Is.LessThanOrEqualTo(detail.rectTransform.rect.height + 1f),
                    "A rule must fit its printed row in Chinese.");
                Assert.That(detail.fontSize, Is.GreaterThanOrEqualTo(13));
            }
            var reviewPanel = canvas.Find("Building choices/Last day review")
                .GetComponent<RectTransform>();
            var routePanel = canvas.Find("Route inspection")
                .GetComponent<RectTransform>();
            var ledger = canvas.Find("Observation ledger")
                .GetComponent<RectTransform>();
            Assert.That(ledger.gameObject.activeSelf, Is.True);
            Assert.That(routePanel.gameObject.activeSelf, Is.True,
                "The opening route hint should be visible with the primer.");
            Assert.That(canvas.Find("Route inspection/Route success icon")
                .gameObject.activeSelf, Is.False,
                "A positive route badge requires an inspected viable route.");
            var routeStatus = canvas.Find("Route inspection/Route status")
                .GetComponent<Text>();
            var routeHeading = canvas.Find("Route inspection/Route heading")
                .GetComponent<RectTransform>();
            var reviewHeading = canvas.Find(
                "Building choices/Last day review/Review heading")
                .GetComponent<RectTransform>();
            Assert.That(routeHeading.anchoredPosition.x - routeHeading.rect.width / 2f,
                Is.EqualTo(reviewHeading.anchoredPosition.x -
                    reviewHeading.rect.width / 2f).Within(1f),
                "Without a route seal, both section titles should share a left edge.");
            Assert.That(routeStatus.preferredHeight,
                Is.LessThanOrEqualTo(routeStatus.rectTransform.rect.height + 1f),
                "The opening route hint must fit the printed status area.");
            Assert.That(reviewPanel.GetComponent<Image>().color.a,
                Is.GreaterThanOrEqualTo(0.9f),
                "The scoring explanation needs a paper surface over the city print.");
            Assert.That(routePanel.GetComponent<Image>().color.a,
                Is.GreaterThanOrEqualTo(0.9f),
                "Route text needs a paper surface over the city print.");
            Assert.That(reviewPanel.anchoredPosition.y -
                        reviewPanel.rect.height / 2f -
                        (routePanel.anchoredPosition.y +
                         routePanel.rect.height / 2f),
                Is.GreaterThanOrEqualTo(8f),
                "The two paper surfaces must not overlap.");
            Assert.That(ledger.anchoredPosition.y - ledger.rect.height / 2f,
                Is.InRange(-282f, -270f),
                "The moved ledger should still leave the town print visible.");
            Assert.That(ledger.anchoredPosition.y, Is.EqualTo(-130f).Within(1f),
                "Move the whole information card down one board cell.");
            Assert.That(ledger.rect.height, Is.EqualTo(292f).Within(1f),
                "Moving the card should preserve its original height.");
            Assert.That(reviewPanel.anchoredPosition.y - ledger.anchoredPosition.y,
                Is.EqualTo(49f).Within(1f));
            Assert.That(routePanel.anchoredPosition.y - ledger.anchoredPosition.y,
                Is.EqualTo(-97f).Within(1f));
            var primerTop = ledger.anchoredPosition.y + ledger.rect.height / 2f;
            var primerBottom = ledger.anchoredPosition.y - ledger.rect.height / 2f;

            controller.ToggleLanguage();
            Assert.That(scoreHeading.text, Is.EqualTo("TODAY'S TIP"));
            Assert.That(animalRule.text, Does.Contain("green").And.Contain("+1")
                .And.Contain("square").And.Contain("+4"));
            Assert.That(animalRule.text.ToLowerInvariant(),
                Does.Contain("food"),
                "Day 1 should introduce nearby food-area scoring.");
            openingRules = animalRule.text + residentRule.text + sharedRule.text;
            Assert.That(residentRule.text, Is.Empty);
            Assert.That(sharedRule.text, Is.Empty);
            Assert.That(openingRules, Does.Not.Contain("Cycle +1"));
            Assert.That(openingRules,
                Does.Not.Contain("Animal points + resident points"));
            foreach (var detail in new[] { animalRule, residentRule, sharedRule })
                Assert.That(detail.preferredHeight,
                    Is.LessThanOrEqualTo(detail.rectTransform.rect.height + 1f),
                    "A rule must fit its printed row in English.");
            Assert.That(routeStatus.preferredHeight,
                Is.LessThanOrEqualTo(routeStatus.rectTransform.rect.height + 1f),
                "The English route hint must fit the printed status area.");
            controller.ToggleLanguage();

            Assert.That(controller.TryPlace(3, 3), Is.False);
            var successSeal = canvas.Find("Route inspection/Route success icon")
                .GetComponent<RectTransform>();
            Assert.That(successSeal.gameObject.activeSelf, Is.True,
                "The green seal should appear after a viable squirrel route is inspected.");
            Assert.That(routeHeading.anchoredPosition.x -
                        routeHeading.rect.width / 2f -
                        (successSeal.anchoredPosition.x +
                         successSeal.rect.width / 2f),
                Is.GreaterThanOrEqualTo(4f),
                "The status seal must not collide with the route heading.");
            Assert.That(routeStatus.preferredHeight,
                Is.LessThanOrEqualTo(routeStatus.rectTransform.rect.height + 1f),
                "The selected route must fit the printed status area.");

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(rules.gameObject.activeSelf, Is.False,
                "The opening rule rows should give way to yesterday's results.");
            Assert.That(reviewPanel.anchoredPosition.y -
                        reviewPanel.rect.height / 2f -
                        (routePanel.anchoredPosition.y +
                         routePanel.rect.height / 2f),
                Is.GreaterThanOrEqualTo(8f),
                "Day settlement must keep the two paper surfaces separate.");
            Assert.That(ledger.anchoredPosition.y - ledger.rect.height / 2f,
                Is.EqualTo(primerBottom).Within(1f),
                "The settlement must retain its coverage of the city print.");
            Assert.That(ledger.anchoredPosition.y + ledger.rect.height / 2f,
                Is.EqualTo(primerTop).Within(1f),
                "The settlement card must stay in place when advancing the day.");
            var topLeftMetric = canvas.Find(
                "Building choices/Story cue backing/Animal score metric")
                .GetComponent<RectTransform>();
            var topRightMetric = canvas.Find(
                "Building choices/Story cue backing/Work cycles metric")
                .GetComponent<RectTransform>();
            var topRowLeft = topLeftMetric.anchoredPosition.x -
                topLeftMetric.rect.width / 2f;
            var topRowRight = topRightMetric.anchoredPosition.x +
                topRightMetric.rect.width / 2f;
            Assert.That(Mathf.Abs(topRowLeft + topRowRight), Is.LessThan(1f));
            Assert.That(reviewPanel.rect.width - (topRowRight - topRowLeft),
                Is.InRange(0f, 4f),
                "The two score cards should align with the settlement column.");
            var yesterdayMetric = canvas.Find(
                "Building choices/Last day review/Yesterday animal points")
                .GetComponent<RectTransform>();
            var metricBottom = reviewPanel.anchoredPosition.y +
                yesterdayMetric.anchoredPosition.y - yesterdayMetric.rect.height / 2f;
            var reviewDetail = canvas.Find(
                "Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(reviewDetail.GetComponent<CanvasGroup>().alpha,
                Is.GreaterThanOrEqualTo(0.9f),
                "Yesterday's actual/forecast report must fill the preserved ledger.");
            Assert.That(reviewDetail.text,
                Does.Contain("实际/预计").And.Contain("进食")
                    .And.Contain("通勤").And.Contain("清运"));
            Assert.That(reviewDetail.preferredHeight,
                Is.LessThanOrEqualTo(reviewDetail.rectTransform.rect.height + 1f),
                "The Chinese settlement report must fit its allotted space.");
            var detailTop = reviewPanel.anchoredPosition.y +
                reviewDetail.rectTransform.anchoredPosition.y +
                reviewDetail.rectTransform.rect.height / 2f;
            var detailBottom = reviewPanel.anchoredPosition.y +
                reviewDetail.rectTransform.anchoredPosition.y -
                reviewDetail.rectTransform.rect.height / 2f;
            var routeHeadingTop = routePanel.anchoredPosition.y +
                routeHeading.anchoredPosition.y + routeHeading.rect.height / 2f;
            Assert.That(metricBottom - detailTop, Is.InRange(4f, 16f));
            Assert.That(detailBottom - routeHeadingTop, Is.GreaterThanOrEqualTo(8f),
                "Settlement details must clear the route heading.");
            controller.ToggleLanguage();
            Assert.That(reviewDetail.text, Does.Contain("Actual/plan").And.Contain("Meals"));
            Assert.That(reviewDetail.preferredHeight,
                Is.LessThanOrEqualTo(reviewDetail.rectTransform.rect.height + 1f),
                "The English settlement report must fit its allotted space.");
            controller.ToggleLanguage();
            var story = canvas.Find(
                "Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            var storyPreview = canvas.Find(
                "Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            var roomFunction = canvas.Find(
                "Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            Assert.That(story.text + storyPreview.text, Does.Contain("绿地"),
                "The second day must say which room comes next.");
            Assert.That(story.text + storyPreview.text,
                Does.Contain("上下左右四格"),
                "The lesson must explain which plots touch the central oak.");
            Assert.That(roomFunction.text, Does.Contain("鸽子"),
                "Day 2 should explain which animal the first meadow attracts.");
            Assert.That(story.text + storyPreview.text + roomFunction.text,
                Does.Not.Contain("完整循环"),
                "The home-cycle lesson should wait until Day 3.");
            // Story instruction is a hidden source for hover details; only
            // preview and room function are visible in the side panel.
            AssertTutorialCopyFits("Day 2 Chinese", storyPreview, roomFunction);
            controller.ToggleLanguage();
            Assert.That(roomFunction.text.ToLowerInvariant(),
                Does.Contain("pigeon"));
            Assert.That((story.text + storyPreview.text + roomFunction.text)
                .ToLowerInvariant(), Does.Not.Contain("full cycle"));
            AssertTutorialCopyFits("Day 2 English", storyPreview, roomFunction);
            controller.ToggleLanguage();
            controller.ToggleBuildPalette();
            var square = canvas.Find("Building choices/Build Square");
            var green = canvas.Find("Building choices/Build Green");
            var residence = canvas.Find("Building choices/Build Residence");
            Assert.That(square.GetComponent<Button>().interactable, Is.False);
            Assert.That(green.GetComponent<Button>().interactable, Is.True,
                "The supplied oak must not count as the player's Green build.");
            Assert.That(residence.GetComponent<Button>().interactable, Is.False);
            Assert.That(green.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(0.001f));
            controller.ToggleBuildPalette();

            Assert.That(controller.TrySelect(ConstructionCategory.Square), Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            var board = canvas.Find("7 by 7 construction board");
            var visibleHints = 0;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var hint = board.Find($"Cell {column + 1},{row + 1}/Buildable hint");
                Assert.That(hint, Is.Not.Null);
                var expected = Mathf.Abs(column - ConstructionBoardModel.StarterColumn) +
                    Mathf.Abs(row - ConstructionBoardModel.StarterRow) == 1;
                Assert.That(hint.gameObject.activeSelf, Is.EqualTo(expected),
                    $"Day 2 plus marker at {column},{row}");
                if (hint.gameObject.activeSelf) visibleHints++;
            }
            Assert.That(visibleHints, Is.EqualTo(4));
            Assert.That(controller.RequestPlacement(0, 0), Is.False,
                "A distant plot cannot enter the confirmation flow.");
            Assert.That(canvas.Find("Building choices/Placement confirmation")
                .gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("上下左右"));
            Assert.That(controller.RequestPlacement(4, 4), Is.False,
                "A diagonal plot must not connect to the oak through a corner.");
            Assert.That(controller.RequestPlacement(4, 3), Is.True,
                "The plot directly beside the oak is buildable.");
            var confirmation = canvas.Find(
                "Building choices/Placement confirmation");
            Assert.That(confirmation.gameObject.activeSelf, Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(confirmation.gameObject.activeSelf, Is.False);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(story.text + storyPreview.text, Does.Contain("住宅"),
                "The next day should introduce the next room type.");
            Assert.That(roomFunction.text, Does.Contain("完整循环"),
                "Day 3 should explain what a home needs for a full cycle.");
            Assert.That(roomFunction.text, Does.Contain("居民得分＋1"),
                "Day 3 should reveal the first resident scoring rule.");
            Assert.That(storyPreview.text + roomFunction.text,
                Does.Not.Contain("先选住宅类型"),
                "A residence no longer needs a subtype choice before building.");
            Assert.That(roomFunction.text, Does.Contain("自动"),
                "Day 3 should explain that housing rewards follow achieved conditions.");
            AssertTutorialCopyFits("Day 3 Chinese", storyPreview, roomFunction);
            controller.ToggleLanguage();
            Assert.That(roomFunction.text.ToLowerInvariant(),
                Does.Contain("work, eat, return"));
            Assert.That(roomFunction.text.ToLowerInvariant(),
                Does.Contain("+1 resident point"));
            AssertTutorialCopyFits("Day 3 English", storyPreview, roomFunction);
            controller.ToggleLanguage();
            controller.ToggleBuildPalette();
            Assert.That(green.GetComponent<Button>().interactable, Is.False);
            Assert.That(green.GetComponent<CanvasGroup>().alpha,
                Is.LessThan(residence.GetComponent<CanvasGroup>().alpha),
                "An introduced room should look dimmer until free planning unlocks it.");
            Assert.That(residence.GetComponent<Button>().interactable, Is.True);
            Assert.That(square.GetComponent<Button>().interactable, Is.False);
            Assert.That(green.Find("Build limit badge/Built and limit")
                .GetComponent<Text>().text, Does.Contain("1/1"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AfterEightTypesThePreviouslyDimmedButtonIsEnabled()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            foreach (var (category, column, row) in ConnectedIntroduction)
            {
                Assert.That(run.TryBuild(column, row, category,
                    GreenPlanting.None, out _, out var failure,
                    out var placement), Is.True,
                    $"{category}: {failure}/{placement}");
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            var root = new GameObject("Completed introduction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);
            controller.ToggleBuildPalette();
            var square = root.transform.Find(
                "Construction UI/Building choices/Build Square");
            Assert.That(square.GetComponent<Button>().interactable, Is.True);
            Assert.That(square.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(0.001f));
            controller.ToggleBuildPalette();
            var roomFunction = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            Assert.That(run.CurrentDay, Is.EqualTo(10));
            Assert.That(roomFunction.text, Does.Contain("共栖分"),
                "Free planning should introduce the combined score after all eight rooms.");
            AssertTutorialCopyFits("Day 10 Chinese", roomFunction);
            var nextHint = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            Assert.That(nextHint.text, Does.Not.Contain("八类房间都已建过"),
                "The first free day should point to the current bottleneck.");
            Assert.That(new[] { "居民", "动物", "垃圾", "有人" }
                .Any(hint => nextHint.text.Contains(hint)), Is.True,
                "The recommendation should identify a present need.");
            AssertTutorialCopyFits("Day 10 action hint", nextHint);
            Object.Destroy(root);
            yield return null;

            foreach (var (day, keyword) in new[]
                     { (11, "角标"), (12, "食源") })
            {
                var nextGreen = Enumerable.Range(0, 49)
                    .Select(index => (column: index % 7, row: index / 7))
                    .First(plot => run.CheckPlacement(plot.column, plot.row,
                        ConstructionCategory.Green) ==
                        ConstructionPlacementFailure.None);
                Assert.That(run.TryBuild(nextGreen.column, nextGreen.row,
                    ConstructionCategory.Green, GreenPlanting.None,
                    out _, out var failure, out var placement), Is.True,
                    $"Day {day - 1}: {failure}/{placement}");
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
                Assert.That(run.CurrentDay, Is.EqualTo(day));
                var lessonRoot = new GameObject($"Day {day} lesson UI test");
                var lessonController = lessonRoot.AddComponent<ConstructionPrototypeController>();
                lessonController.UseRunForTesting(run);
                lessonController.UseLanguageForTesting(InterfaceLanguage.Chinese);
                yield return WaitForOpenCase(lessonController);
                var lesson = lessonRoot.transform.Find(
                    "Construction UI/Building choices/Story cue backing/Room function")
                    .GetComponent<Text>();
                Assert.That(lesson.text, Does.Contain(keyword));
                if (day == 12)
                    Assert.That(lesson.text,
                        Does.Contain("绿地+1").And.Contain("广场+4")
                            .And.Contain("超市+3").And.Contain("垃圾+1")
                            .And.Contain("进食关系到存活"));
                AssertTutorialCopyFits($"Day {day} Chinese", lesson);
                Object.Destroy(lessonRoot);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryIntroRoomHintsOnlyLegalPlotsAndRejectsInvalidClicks()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Original placement rules UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);
            Assert.That(controller.AdvanceDay(), Is.True);

            var canvas = root.transform.Find("Construction UI");
            var board = canvas.Find("7 by 7 construction board");
            var confirmation = canvas.Find(
                "Building choices/Placement confirmation");
            var roomFunction = canvas.Find(
                "Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            foreach (var (category, column, row) in ConnectedIntroduction)
            {
                var lessonDay = run.CurrentDay;
                var lessonKeyword = category switch
                {
                    ConstructionCategory.Green => "鸽子",
                    ConstructionCategory.Residence => "完整循环",
                    ConstructionCategory.Restaurant => "餐位",
                    ConstructionCategory.Workshop => "居民调度",
                    ConstructionCategory.Street => "连续街道",
                    ConstructionCategory.Waste => "清运",
                    ConstructionCategory.Supermarket => "2 餐位",
                    _ => "广场"
                };
                Assert.That(run.IntroRequiredCategory, Is.EqualTo(category));
                AssertRoomLesson(roomFunction, lessonKeyword, lessonDay,
                    "before building");
                if (lessonDay == 5)
                {
                    var settledResident = run.LastHumanDay?.residents?
                        .FirstOrDefault();
                    Assert.That(settledResident, Is.Not.Null);
                    Assert.That(roomFunction.text,
                        Does.Contain("2岗位").And.Contain("＋2分"),
                        "The visible workshop lesson should explain its benefit first.");
                    var expanded = canvas.Find(
                        "Building choices/Story cue backing/Story instruction")
                        .GetComponent<Text>();
                    Assert.That(expanded.text,
                        Does.Contain($"昨日居民+{settledResident.satisfactionPoints}"),
                        "The detailed workshop lesson should name the actual source.");
                    Assert.That(expanded.text,
                        Does.Contain("循环").And.Contain("岗位")
                            .And.Contain("餐食"));
                }
                Assert.That(controller.TrySelect(category), Is.True);
                var (invalidColumn, invalidRow) =
                    category is ConstructionCategory.Green or
                        ConstructionCategory.Residence or
                        ConstructionCategory.Square
                        ? (0, 0) : (3, 2);
                Assert.That(run.CheckPlacement(invalidColumn, invalidRow,
                    category), Is.Not.EqualTo(ConstructionPlacementFailure.None));
                Assert.That(Hint(board, invalidColumn, invalidRow).activeSelf,
                    Is.False, $"{category} invalid plus marker");
                Assert.That(Hint(board, column, row).activeSelf, Is.True,
                    $"{category} legal plus marker");
                Assert.That(controller.RequestPlacement(invalidColumn, invalidRow),
                    Is.False, $"{category} invalid click");
                Assert.That(confirmation.gameObject.activeSelf, Is.False);
                Assert.That(controller.RequestPlacement(column, row), Is.True,
                    $"{category} legal click");
                Assert.That(confirmation.gameObject.activeSelf, Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                AssertRoomLesson(roomFunction, lessonKeyword, lessonDay,
                    "after building");
                Assert.That(controller.AdvanceDay(), Is.True);
            }
            Assert.That(run.IsIntroPhase, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllEightRoomLessonsExplainPlacementAndBenefitInBothLanguages()
        {
            var issues = new List<string>();
            var run = new ConstructionRunModel();
            var root = new GameObject("Bilingual eight room lessons test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);
            Assert.That(controller.AdvanceDay(), Is.True);
            yield return WaitForOpenCase(controller);

            var story = root.transform.Find(
                "Construction UI/Building choices/Story cue backing");
            var placement = story.Find("Story preview").GetComponent<Text>();
            var benefit = story.Find("Room function").GetComponent<Text>();
            var storyPictogram = story.Find("Story pictogram")
                .GetComponent<EventTrigger>();
            var tooltip = root.transform.Find("Construction UI/Context tooltip");
            var tooltipDetails = tooltip.Find("Tooltip details").GetComponent<Text>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            var scoreCard = story.Find("Animal score metric") as RectTransform;
            Assert.That(scoreCard, Is.Not.Null);
            var gapAboveScores = benefit.rectTransform.anchoredPosition.y -
                benefit.rectTransform.rect.height * 0.5f -
                (scoreCard.anchoredPosition.y + scoreCard.rect.height * 0.5f);
            Assert.That(gapAboveScores, Is.GreaterThanOrEqualTo(8f),
                "Room lesson must not overlap the animal and resident score cards.");
            foreach (var (category, column, row) in ConnectedIntroduction)
            {
                var day = run.CurrentDay;
                Assert.That(run.IntroRequiredCategory, Is.EqualTo(category));
                yield return null;
                CollectLessonIssues(issues, category, true, day, placement, benefit);
                if (category == ConstructionCategory.Green)
                {
                    storyPictogram.OnPointerEnter(pointer);
                    yield return null;
                    CollectGreenTooltipIssues(issues, true, controller,
                        storyPictogram, tooltip, tooltipDetails);
                    storyPictogram.OnPointerExit(pointer);
                }
                if (category == ConstructionCategory.Residence)
                {
                    storyPictogram.OnPointerEnter(pointer);
                    yield return null;
                    CollectResidenceTooltipIssues(issues, true, tooltip,
                        tooltipDetails);
                    storyPictogram.OnPointerExit(pointer);
                }

                controller.ToggleLanguage();
                yield return null;
                CollectLessonIssues(issues, category, false, day, placement, benefit);
                if (category == ConstructionCategory.Green)
                {
                    storyPictogram.OnPointerEnter(pointer);
                    yield return null;
                    CollectGreenTooltipIssues(issues, false, controller,
                        storyPictogram, tooltip, tooltipDetails);
                    storyPictogram.OnPointerExit(pointer);
                }
                if (category == ConstructionCategory.Residence)
                {
                    storyPictogram.OnPointerEnter(pointer);
                    yield return null;
                    CollectResidenceTooltipIssues(issues, false, tooltip,
                        tooltipDetails);
                    storyPictogram.OnPointerExit(pointer);
                }
                controller.ToggleLanguage();
                yield return null;

                Assert.That(controller.TrySelect(category), Is.True);
                Assert.That(controller.RequestPlacement(column, row), Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                Assert.That(controller.AdvanceDay(), Is.True);
                yield return WaitForOpenCase(controller);
            }
            Assert.That(run.IntroTypesBuilt, Is.EqualTo(8));
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
            Object.Destroy(root);
            yield return null;
        }

        private static void CollectGreenTooltipIssues(List<string> issues,
            bool chinese, ConstructionPrototypeController controller,
            EventTrigger pictogram, Transform tooltip, Text details)
        {
            var context = chinese ? "Day 2 Chinese" : "Day 2 English";
            try
            {
                Assert.That(tooltip.gameObject.activeInHierarchy, Is.True,
                    $"{context} pictogram should open its detailed tooltip. " +
                    $"caseOpen={controller.IsBoardCaseOpen}, " +
                    $"pending={controller.HasPendingPlacement}, " +
                    $"final={tooltip.parent.Find("Construction final result").gameObject.activeSelf}, " +
                    $"triggerCount={pictogram.triggers.Count}, " +
                    $"detailsLength={details.text.Length}");
                AssertGreenGuideOrder(details.text, chinese, context);
                // The compact card gives the three decisions and a concrete
                // example. Exact neighbour precedence and species counts are
                // covered by the run-model and plot-preview tests.
                var tokens = chinese
                    ? new[] { "草地", "鸽子" }
                    : new[] { "meadow", "pigeon" };
                foreach (var token in tokens)
                    Assert.That(details.text.IndexOf(token,
                        StringComparison.OrdinalIgnoreCase),
                        Is.GreaterThanOrEqualTo(0),
                        $"{context} green tooltip should explain {token}.");
                AssertSdfLessonFits($"{context} green tooltip", details);
            }
            catch (AssertionException ex)
            {
                issues.Add($"{context} hover: {ex.Message}");
            }
        }

        private static void AssertGreenGuideOrder(string body, bool chinese,
            string context)
        {
            var labels = chinese
                ? new[] { "放置：", "地形：", "动物：" }
                : new[] { "PLACE:", "TERRAIN:", "WILDLIFE:" };
            var previous = -1;
            foreach (var label in labels)
            {
                var position = body.IndexOf(label,
                    StringComparison.OrdinalIgnoreCase);
                Assert.That(position, Is.GreaterThan(previous),
                    $"{context}: {label} should follow the earlier guidance.");
                previous = position;
            }
            foreach (var productionDetail in chinese
                         ? new[] { "80%", "满库存", "居民联动", "份/日" }
                         : new[] { "80%", "stock limit", "resident linkage",
                             "eligible days" })
                Assert.That(body, Does.Not.Contain(productionDetail),
                    $"{context}: detailed output belongs on a built plot.");
        }

        private static void CollectResidenceTooltipIssues(List<string> issues,
            bool chinese, Transform tooltip, Text details)
        {
            var context = chinese ? "Day 3 Chinese" : "Day 3 English";
            try
            {
                Assert.That(tooltip.gameObject.activeInHierarchy, Is.True);
                var tokens = chinese
                    ? new[] { "上下左右", "斜角", "广场", "连续街道", "完整循环" }
                    : new[] { "edge", "corners", "square",
                        "streets", "cycle" };
                foreach (var token in tokens)
                    Assert.That(details.text.IndexOf(token,
                        StringComparison.OrdinalIgnoreCase),
                        Is.GreaterThanOrEqualTo(0),
                        $"{context} residence tooltip should explain {token}.");
                AssertSdfLessonFits($"{context} residence tooltip", details);
            }
            catch (AssertionException ex)
            {
                issues.Add($"{context} hover: {ex.Message}");
            }
        }

        private static void CollectLessonIssues(List<string> issues,
            ConstructionCategory category, bool chinese, int day,
            Text placement, Text benefit)
        {
            var context = $"Day {day} {(chinese ? "Chinese" : "English")}";
            try
            {
                AssertLessonSemantics(category, chinese,
                    placement.text, benefit.text);
            }
            catch (AssertionException ex)
            {
                issues.Add($"{context} meaning: {ex.Message}");
            }
            foreach (var label in new[] { placement, benefit })
            {
                try
                {
                    AssertSdfLessonFits(context, label);
                }
                catch (AssertionException ex)
                {
                    issues.Add($"{context} layout: {ex.Message}");
                }
            }
        }

        private static void AssertLessonSemantics(ConstructionCategory category,
            bool chinese, string placement, string benefit)
        {
            Assert.That(placement, Is.Not.Empty, $"{category} needs a placement hint.");
            Assert.That(benefit, Is.Not.Empty, $"{category} needs a benefit hint.");
            var where = chinese ? category switch
            {
                ConstructionCategory.Green => new[] { "中央橡树", "上下左右" },
                ConstructionCategory.Residence => new[] { "已建格", "上下左右" },
                ConstructionCategory.Restaurant => new[] { "住宅", "街道" },
                ConstructionCategory.Workshop => new[] { "住宅", "街道" },
                ConstructionCategory.Street => new[] { "住宅", "延长一格" },
                ConstructionCategory.Waste => new[] { "餐厅", "街道" },
                ConstructionCategory.Supermarket => new[] { "住宅", "街道" },
                _ => new[] { "住宅", "街道" }
            } : category switch
            {
                ConstructionCategory.Green => new[] { "central oak", "above" },
                ConstructionCategory.Residence => new[] { "built plot", "edge" },
                ConstructionCategory.Restaurant => new[] { "home", "street" },
                ConstructionCategory.Workshop => new[] { "home", "street" },
                ConstructionCategory.Street => new[] { "home", "one step" },
                ConstructionCategory.Waste => new[] { "restaurant", "street" },
                ConstructionCategory.Supermarket => new[] { "home", "street" },
                _ => new[] { "home", "street" }
            };
            var effect = chinese ? category switch
            {
                ConstructionCategory.Green => new[] { "草地", "鸽子", "橡树", "松鼠", "灌木", "刺猬" },
                ConstructionCategory.Residence => new[] { "连续街道", "完整循环", "上班", "用餐" },
                ConstructionCategory.Restaurant => new[] { "岗位", "餐位", "上班", "吃饭" },
                ConstructionCategory.Workshop => new[] { "岗位", "居民调度" },
                ConstructionCategory.Street => new[] { "连续街道", "空格", "车祸" },
                ConstructionCategory.Waste => new[] { "清运", "积压" },
                ConstructionCategory.Supermarket => new[] { "餐位", "岗位", "动物" },
                _ => new[] { "居民", "食源", "动物" }
            } : category switch
            {
                ConstructionCategory.Green => new[] { "meadow", "pigeon", "squirrel", "shrub", "hedgehog" },
                ConstructionCategory.Residence => new[] { "Streets", "Work", "eat", "+1 resident point" },
                ConstructionCategory.Restaurant => new[] { "job", "meal seat", "work", "meal" },
                ConstructionCategory.Workshop => new[] { "job", "Assign", "home" },
                ConstructionCategory.Street => new[] { "continuous streets", "Gaps", "traffic" },
                ConstructionCategory.Waste => new[] { "Clears", "Piles" },
                ConstructionCategory.Supermarket => new[] { "meal seats", "job", "animal food" },
                _ => new[] { "resident", "food", "wildlife" }
            };
            foreach (var token in where)
                Assert.That(placement.IndexOf(token,
                    StringComparison.OrdinalIgnoreCase), Is.GreaterThanOrEqualTo(0),
                    $"{category} placement hint should explain {token}.");
            foreach (var token in effect)
                Assert.That(benefit.IndexOf(token,
                    StringComparison.OrdinalIgnoreCase), Is.GreaterThanOrEqualTo(0),
                    $"{category} benefit hint should explain {token}.");
        }

        private static void AssertSdfLessonFits(string lesson, params Text[] labels)
        {
            foreach (var label in labels)
            {
                Assert.That(label.gameObject.activeInHierarchy, Is.True,
                    $"{lesson} {label.name} should be visible.");
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{lesson} {label.name} UI.Text overflows: {label.text}");
                var sdf = label.GetComponentInChildren<TextMeshProUGUI>(true);
                Assert.That(sdf, Is.Not.Null,
                    $"{lesson} {label.name} should use TMP rendering.");
                Assert.That(sdf.text, Is.EqualTo(label.text));
                sdf.ForceMeshUpdate();
                Assert.That(sdf.isTextTruncated, Is.False,
                    $"{lesson} {label.name} TMP text is clipped: {label.text}");
            }
        }

        [UnityTest]
        public IEnumerator SkippingTipsKeepsTheRequiredRoomAndDailyBuildGates()
        {
            var run = new ConstructionRunModel();
            var root = new GameObject("Skipped construction tips UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForOpenCase(controller);

            var canvas = root.transform.Find("Construction UI");
            var skip = canvas.Find(
                "Building choices/Story cue backing/Skip tutorial tips")
                .GetComponent<Button>();
            var skipLabel = skip.GetComponentInChildren<Text>();
            var roomFunction = canvas.Find(
                "Building choices/Story cue backing/Room function")
                .GetComponent<Text>();
            var storyPreview = canvas.Find(
                "Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            var primer = canvas.Find("Building choices/Last day review/Primer rules");
            var nextDay = canvas.Find("Building choices/Advance to next day")
                .GetComponent<Button>();
            var placementGuide = canvas.Find(
                "Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(skip.gameObject.activeInHierarchy, Is.True);
            Assert.That(skipLabel.text, Is.EqualTo("跳过"));
            Assert.That(primer.gameObject.activeSelf, Is.True);

            skip.onClick.Invoke();
            Assert.That(run.TutorialTipsSkipped, Is.True);
            Assert.That(skipLabel.text, Is.EqualTo("提示"));
            Assert.That(primer.gameObject.activeSelf, Is.False);
            Assert.That(roomFunction.text, Is.Empty);
            Assert.That(storyPreview.text, Does.Contain("橡树"),
                "The required action must remain visible after Skip.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.TutorialTipsSkipped, Is.True,
                "Skip is a per-run presentation choice that survives Continue.");

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(run.CurrentDay, Is.EqualTo(2));
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Green));
            Assert.That(run.CanAdvanceConstructionDay, Is.False);
            Assert.That(nextDay.interactable, Is.False);
            Assert.That(controller.AdvanceDay(), Is.False);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out var blockedFailure,
                out var blockedPlacement), Is.False,
                "Skip must not bypass the model's required-room rule.");
            Assert.That(blockedFailure,
                Is.EqualTo(ConstructionBuildFailure.LockedByStory));
            Assert.That(blockedPlacement,
                Is.EqualTo(ConstructionPlacementFailure.IntroTypeNotNext));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.False);
            Assert.That(placementGuide.text, Does.Contain("绿地"));
            Assert.That(storyPreview.text, Does.Contain("绿地"));
            Assert.That(roomFunction.text, Is.Empty);
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(run.CanAdvanceConstructionDay, Is.True);
            Assert.That(nextDay.interactable, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);

            Assert.That(run.CurrentDay, Is.EqualTo(3));
            Assert.That(run.IntroRequiredCategory,
                Is.EqualTo(ConstructionCategory.Residence));
            Assert.That(run.TutorialTipsSkipped, Is.True);
            Assert.That(run.CanAdvanceConstructionDay, Is.False);
            Assert.That(controller.AdvanceDay(), Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.False);
            Assert.That(placementGuide.text, Does.Contain("住宅"));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.RequestPlacement(4, 4), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(run.CanAdvanceConstructionDay, Is.True);

            skip.onClick.Invoke();
            Assert.That(run.TutorialTipsSkipped, Is.False);
            Assert.That(skipLabel.text, Is.EqualTo("跳过"));
            Assert.That(roomFunction.text, Is.Not.Empty,
                "The player can restore the daily explanations.");
            Object.Destroy(root);
            yield return null;
        }

        private static GameObject Hint(Transform board, int column, int row) =>
            board.Find($"Cell {column + 1},{row + 1}/Buildable hint").gameObject;

        private static void AssertTutorialCopyFits(string lesson, params Text[] labels)
        {
            foreach (var label in labels)
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{lesson} {label.name} is clipped: {label.text}");
        }

        private static void AssertRoomLesson(Text roomFunction, string keyword,
            int day, string phase)
        {
            Assert.That(roomFunction.gameObject.activeInHierarchy, Is.True,
                $"Day {day} room lesson should be visible {phase}.");
            Assert.That(roomFunction.text, Does.Contain(keyword),
                $"Day {day} should still teach {keyword} {phase}.");
            AssertTutorialCopyFits($"Day {day} Chinese {phase}", roomFunction);
        }

        private static readonly (ConstructionCategory category, int column,
            int row)[] ConnectedIntroduction =
        {
            (ConstructionCategory.Green, 4, 3),
            (ConstructionCategory.Residence, 4, 4),
            (ConstructionCategory.Restaurant, 5, 4),
            (ConstructionCategory.Workshop, 4, 5),
            (ConstructionCategory.Street, 3, 4),
            (ConstructionCategory.Waste, 6, 4),
            (ConstructionCategory.Supermarket, 3, 5),
            (ConstructionCategory.Square, 2, 4)
        };

        private static IEnumerator WaitForOpenCase(
            ConstructionPrototypeController controller)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
        }
    }
}
