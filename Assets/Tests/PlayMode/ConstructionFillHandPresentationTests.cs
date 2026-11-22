using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionFillHandPresentationTests
    {
        [Test]
        public void PrintedHandArtworkLoadsForEveryRoomAndControl()
        {
            foreach (ConstructionCategory category in
                     System.Enum.GetValues(typeof(ConstructionCategory)))
                Assert.That(ConstructionHandArtCatalog.GetRoom(category),
                    Is.Not.Null, $"Missing printed artwork for {category}.");
            Assert.That(ConstructionHandArtCatalog.GetRoom(
                ConstructionCategory.Green, GreenPlanting.Oak), Is.Not.Null);
            Assert.That(ConstructionHandArtCatalog.GetRoom(
                ConstructionCategory.Green, GreenPlanting.Shrub), Is.Not.Null);
            Assert.That(ConstructionHandArtCatalog.GetDeck(), Is.Not.Null);
            Assert.That(ConstructionHandArtCatalog.GetReroll(), Is.Not.Null);
            foreach (HandFooterIcon icon in
                     System.Enum.GetValues(typeof(HandFooterIcon)))
                Assert.That(ConstructionHandArtCatalog.GetFooter(icon),
                    Is.Not.Null, $"Missing card footer icon {icon}.");
        }

        [UnityTest]
        public IEnumerator SeparatePatchesRenderTenPigeonsAndTwoHedgehogs()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Pigeon models need a graphics device.");

            // Two separate meadow patches must each draw their own flock.
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var board = (ConstructionBoardModel)typeof(ConstructionRunModel)
                .GetField("board", flags).GetValue(run);
            Assert.That(board.TryPlaceStarterOak(3, 3, out _, out _), Is.True);
            var firstPatch = new (int column, int row)[]
            {
                (4, 3), (5, 3), (6, 3), (6, 2), (5, 2),
                (4, 2), (3, 2)
            };
            string firstMeadowId = null;
            foreach (var plot in firstPatch)
            {
                Assert.That(board.TryBuild(plot.column, plot.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out var tile, out _, builtDay: 1), Is.True);
                firstMeadowId ??= tile.id;
            }
            Assert.That(board.TryBuild(3, 4, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, builtDay: 1), Is.True);
            var secondPatch = new (int column, int row)[]
            {
                (3, 5), (4, 5), (5, 5), (6, 5), (6, 6),
                (5, 6), (4, 6)
            };
            foreach (var plot in secondPatch)
                Assert.That(board.TryBuild(plot.column, plot.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, builtDay: 1), Is.True);
            Assert.That(board.TryBuild(5, 1, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var firstShrub, out _, builtDay: 1), Is.True);
            Assert.That(board.TryBuild(3, 6, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, builtDay: 1), Is.True);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 3);
            typeof(ConstructionRunModel).GetProperty("PigeonTileId")
                .SetValue(run, firstMeadowId);
            Assert.That(run.PigeonPopulation, Is.EqualTo(10));
            var groups = run.PigeonGroups(run.PigeonPopulation);
            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups.Select(group => group.Count),
                Is.EquivalentTo(new[] { 5, 5 }));
            Assert.That(run.HedgehogHabitats.Count, Is.EqualTo(2));

            var cameraObject = new GameObject("Pigeon count camera", typeof(Camera));
            var canvasObject = new GameObject("Pigeon count canvas",
                typeof(RectTransform), typeof(Canvas));
            var plotObject = new GameObject("Pigeon count plot",
                typeof(RectTransform));
            var viewObject = new GameObject("Pigeon count world",
                typeof(ConstructionWorldView));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 42f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));
                canvasObject.GetComponent<Canvas>().renderMode =
                    RenderMode.ScreenSpaceOverlay;
                var plot = plotObject.GetComponent<RectTransform>();
                plot.SetParent(canvasObject.transform, false);
                plot.anchorMin = plot.anchorMax = new Vector2(0.5f, 0.5f);
                plot.sizeDelta = new Vector2(707f, 707f);
                var view = viewObject.GetComponent<ConstructionWorldView>();
                view.Initialize(camera, plot, 101f, ConstructionGameMode.Fill);
                Assert.That(view.IsReady, Is.True);
                view.Sync(run, animateGrowth: false);

                Assert.That(run.GreenUpgradeLevel(firstMeadowId), Is.EqualTo(3));
                var upgraded = view.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(part => part.name.StartsWith(
                        firstMeadowId + " "));
                Assert.That(upgraded, Is.Not.Null);
                Assert.That(upgraded.Find("Green habitat level 2")
                    .gameObject.activeSelf, Is.True);
                Assert.That(upgraded.Find("Green habitat level 3")
                    .gameObject.activeSelf, Is.True);
                Assert.That(upgraded.Find("Green habitat level 2")
                    .GetComponentsInChildren<MeshRenderer>()
                    .Any(part => part.name == "Meadow flowers"), Is.True);
                Assert.That(upgraded.Find("Green habitat level 3")
                    .GetComponentsInChildren<MeshRenderer>()
                    .Count(part => part.name == "Meadow flowers"),
                    Is.GreaterThanOrEqualTo(3));
                var oakRoom = view.GetComponentsInChildren<Transform>(true)
                    .First(part => part.name.StartsWith(
                        ConstructionBoardModel.StarterOakId + " "));
                Assert.That(oakRoom.Find("Green habitat level 3")
                    .GetComponentsInChildren<MeshRenderer>()
                    .Any(part => part.name == "Oak acorns"), Is.True);
                var shrubRoom = view.GetComponentsInChildren<Transform>(true)
                    .First(part => part.name.StartsWith(firstShrub.id + " "));
                Assert.That(shrubRoom.Find("Green habitat level 3")
                    .GetComponentsInChildren<MeshRenderer>()
                    .Any(part => part.name == "Shrub berries"), Is.True);

                var birds = view.GetComponentsInChildren<PigeonDemoVisual>(true)
                    .Where(visual => visual.gameObject.activeInHierarchy).ToArray();
                Assert.That(birds.Length, Is.EqualTo(run.PigeonPopulation));
                var homes = (Dictionary<Transform, string>)
                    typeof(ConstructionWorldView).GetField("animalHomeTiles", flags)
                        .GetValue(view);
                foreach (var group in groups)
                    Assert.That(birds.Count(bird => group.Patch.Meadows
                            .Any(tile => tile.id == homes[bird.transform])),
                        Is.EqualTo(group.Count),
                        "Each flock should stay in its own meadow patch.");
                var hedgehogs = view.GetComponentsInChildren<HedgehogDemoVisual>(true)
                    .Where(visual => visual.gameObject.activeInHierarchy).ToArray();
                Assert.That(hedgehogs.Length, Is.EqualTo(2));
                Assert.That(hedgehogs.Select(visual => homes[visual.transform]),
                    Is.EquivalentTo(run.HedgehogHabitats.Select(home => home.id)));

                var day = new ConstructionEcologyDayResult
                {
                    squirrelsPresent = 1,
                    squirrelsFed = 1,
                    pigeonsPresent = 10,
                    pigeonsFed = 8,
                    squirrelMealHomeTileIds = new System.Collections.Generic.List<string>
                    {
                        ConstructionBoardModel.StarterOakId
                    }
                };
                var idle = view.IdleCueActorsForDay(day, null);
                Assert.That(idle.Count, Is.EqualTo(2));
                Assert.That(idle.All(actor => actor.GetComponent<PigeonDemoVisual>()
                    != null), Is.True);
                day.pigeonsFed = 10;
                Assert.That(view.IdleCueActorsForDay(day, null), Is.Empty,
                    "Fed animals should not receive a sad cue.");
            }
            finally
            {
                Object.Destroy(viewObject);
                Object.Destroy(plotObject);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstMeadowVisitorsBecomeResidentsOnlyAfterSecondMatures()
        {
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Pigeon models need a graphics device.");

            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var board = (ConstructionBoardModel)typeof(ConstructionRunModel)
                .GetField("board", flags).GetValue(run);
            Assert.That(board.TryPlaceStarterOak(3, 3, out _, out _), Is.True);
            Assert.That(board.TryBuild(3, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var first, out _, builtDay: 1), Is.True);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 2);

            var cameraObject = new GameObject("Visitor camera", typeof(Camera));
            var canvasObject = new GameObject("Visitor canvas",
                typeof(RectTransform), typeof(Canvas));
            var plotObject = new GameObject("Visitor plot",
                typeof(RectTransform));
            var viewObject = new GameObject("Visitor world",
                typeof(ConstructionWorldView));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 42f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));
                canvasObject.GetComponent<Canvas>().renderMode =
                    RenderMode.ScreenSpaceOverlay;
                var plot = plotObject.GetComponent<RectTransform>();
                plot.SetParent(canvasObject.transform, false);
                plot.anchorMin = plot.anchorMax = new Vector2(0.5f, 0.5f);
                plot.sizeDelta = new Vector2(707f, 707f);
                var view = viewObject.GetComponent<ConstructionWorldView>();
                view.Initialize(camera, plot, 101f, ConstructionGameMode.Fill);
                Assert.That(view.IsReady, Is.True);
                view.Sync(run, animateGrowth: false);
                Assert.That(run.PigeonPopulation, Is.Zero);
                Assert.That(run.PreviewEcologyDay().pigeonsPresent, Is.Zero);
                Assert.That(view.GetComponentsInChildren<PigeonDemoVisual>(true)
                        .Count(visual => visual.gameObject.activeInHierarchy &&
                            visual.name.StartsWith("Visiting pigeon")),
                    Is.EqualTo(2));

                Assert.That(board.TryBuild(2, 4, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out _, out _, builtDay: 1), Is.True);
                typeof(ConstructionRunModel).GetProperty("CurrentDay")
                    .SetValue(run, 4);
                typeof(ConstructionRunModel).GetProperty("PigeonTileId")
                    .SetValue(run, first.id);
                view.Sync(run, animateGrowth: false);
                Assert.That(run.PigeonPopulation, Is.EqualTo(2));
                Assert.That(view.GetComponentsInChildren<PigeonDemoVisual>(true)
                        .Count(visual => visual.gameObject.activeInHierarchy &&
                            visual.name.StartsWith("Visiting pigeon")),
                    Is.Zero);
                Assert.That(view.GetComponentsInChildren<PigeonDemoVisual>(true)
                        .Count(visual => visual.gameObject.activeInHierarchy),
                    Is.EqualTo(2));
            }
            finally
            {
                Object.Destroy(viewObject);
                Object.Destroy(plotObject);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstOakCardIsVisibleAndSettlesAfterCenterPlacement()
        {
            var root = new GameObject("Fill hand presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            run.SetOpeningTourCompleted(true);
            run.SetTutorialTipsSkipped(true);
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);

            yield return WaitForBoardCase(root, controller);
            yield return WaitForSpecialTaskSelection(root);
            Canvas.ForceUpdateCanvases();
            var canvas = root.transform.Find("Construction UI");
            Assert.That(canvas, Is.Not.Null);
            var hand = canvas.Find("Fill hand on tabletop");
            Assert.That(hand, Is.Not.Null);
            Assert.That(hand.gameObject.activeInHierarchy, Is.True);
            var tasks = canvas.Find("Special task selection");
            Assert.That(tasks, Is.Not.Null);
            Assert.That(tasks.gameObject.activeInHierarchy, Is.True);
            Assert.That(tasks.GetComponent<CanvasGroup>().alpha,
                Is.EqualTo(1f).Within(0.02f));
            Assert.That(tasks.Find("Dark soft-focus veil")
                .GetComponent<Image>().color.a, Is.GreaterThan(0.7f));
            Assert.That(tasks.Find("Blurred gameplay"), Is.Not.Null);
            for (var slot = 0; slot < 3; slot++)
            {
                var card = tasks.Find($"Special task card {slot + 1}")
                    .GetComponent<RectTransform>();
                Assert.That(card.gameObject.activeInHierarchy, Is.True);
                Assert.That(card.localScale.x, Is.EqualTo(1f).Within(0.02f));
                Assert.That(card.anchoredPosition.x,
                    Is.EqualTo((slot - 1) * 370f).Within(2f));
                Assert.That(card.Find("Task illustration")
                    .GetComponent<Image>().sprite, Is.Not.Null,
                    "Every opening goal card needs its matching artwork.");
                Assert.That(card.Find("Task difficulty")
                    .GetComponent<Text>().text, Is.Not.Empty);
                Assert.That(card.Find("Unlocked task effect")
                    .GetComponent<Text>().text, Is.Not.Empty);
                Assert.That(card.Find("Replace special task")
                    .GetComponent<Button>().interactable, Is.True);
            }
            Assert.That(canvas.Find("Special task tray").gameObject.activeSelf,
                Is.False);
            Assert.That(run.IsSpecialTaskSelectionConfirmed, Is.False);
            var board = canvas.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.name ==
                    "7 by 7 construction board");
            Assert.That(board, Is.Not.Null);
            Assert.That(board.localScale.x, Is.EqualTo(0.84f).Within(0.02f));

            var first = hand.Find("Fill hand card 1").GetComponent<Button>();
            var second = hand.Find("Fill hand card 2").GetComponent<Button>();
            var third = hand.Find("Fill hand card 3").GetComponent<Button>();
            Assert.That(first.gameObject.activeInHierarchy, Is.True);
            Assert.That(second.gameObject.activeInHierarchy, Is.True);
            Assert.That(third.gameObject.activeInHierarchy, Is.True);
            Assert.That(first.interactable, Is.True);
            Assert.That(second.interactable, Is.False);
            Assert.That(third.interactable, Is.False);
            Assert.That(hand.Find("Fill hand card 1/Hand room name")
                .GetComponent<Text>().text, Does.Contain("oak").IgnoreCase);
            Assert.That(first.transform.Find("Hand room icon")
                .GetComponent<Image>().sprite.name, Is.EqualTo("Hand art oak"));
            Assert.That(first.transform.Find("Hand footer icon 1")
                .GetComponent<Image>().sprite.name,
                Is.EqualTo("Hand art footer_sprout"));
            Assert.That(hand.Find("Top card ink edge/Top card paper edge/Oak town card back")
                .GetComponent<Image>(), Is.Not.Null);

            var replacedKey = run.SpecialTasks[0].Key;
            tasks.Find("Special task card 1/Replace special task")
                .GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(run.SpecialTasks[0].Key, Is.Not.EqualTo(replacedKey));
            Assert.That(run.SpecialTasks[0].CanReplace, Is.False);

            tasks.Find("Confirm special tasks").GetComponent<Button>()
                .onClick.Invoke();
            yield return WaitForSpecialTaskTray(root, run);
            Assert.That(run.IsSpecialTaskSelectionConfirmed, Is.True);
            Assert.That(run.SpecialTasks.All(task => !task.CanReplace), Is.True);
            Assert.That(tasks.gameObject.activeSelf, Is.False);
            Assert.That(canvas.Find("Special task tray").gameObject.activeInHierarchy,
                Is.True);
            var bookmark = canvas.Find("Special task tray");
            Assert.That(bookmark.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(bookmark.Find("Bookmark title")
                .GetComponent<Text>().text, Does.Not.Contain("+"));
            Assert.That(bookmark.Find("Bookmark count")
                .GetComponent<TMPro.TextMeshProUGUI>().text,
                Is.EqualTo("3"));
            Assert.That(canvas.Find("Special task tray")
                .GetComponent<UnityEngine.EventSystems.EventTrigger>(), Is.Null,
                "The compact special-task tray should open on click without a hover tooltip.");
            bookmark.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(tasks.gameObject.activeInHierarchy, Is.True);
            for (var slot = 0; slot < 3; slot++)
            {
                var card = tasks.Find($"Special task card {slot + 1}");
                Assert.That(card.Find("Task progress").gameObject.activeInHierarchy,
                    Is.True);
                Assert.That(card.Find("Progress track").gameObject.activeInHierarchy,
                    Is.True);
                Assert.That(card.Find("Replace special task").gameObject.activeSelf,
                    Is.False);
            }
            tasks.Find("Close special task review")
                .GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(bookmark.gameObject.activeInHierarchy, Is.True);
            var lastReview = canvas.Find("Building choices/Last day review");
            var ledger = canvas.Find("Observation ledger");
            var routePanel = canvas.Find("Route inspection");
            Assert.That(lastReview.gameObject.activeInHierarchy, Is.False);
            Assert.That(ledger.gameObject.activeInHierarchy, Is.False);
            Assert.That(routePanel.gameObject.activeInHierarchy, Is.False);

            Assert.That(controller.RequestPlacement(3, 3), Is.True,
                "The first hand card should preview the center plot.");
            Assert.That(board.Find("Cell 4,4/Placement preview/Preview room icon")
                .GetComponent<Image>().sprite.name, Is.EqualTo("Hand art oak"));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(run.At(3, 3)?.id,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(run.BuiltCount, Is.EqualTo(1));
            Assert.That(run.CurrentDay, Is.EqualTo(2),
                "One internal simulation tick follows the first player placement.");
            var oakCell = board.Find("Cell 4,4");
            Assert.That(oakCell.Find("Metric side rails")
                .gameObject.activeInHierarchy, Is.False,
                "The hand game should not draw four corner badges over the oak.");
            var threeDReady = root.GetComponent<ConstructionWorldView>().IsReady;
            if (threeDReady)
                Assert.That(root.transform.Find(
                    "Construction low-poly world/Squirrel")
                    .gameObject.activeInHierarchy, Is.True);
            else
                Assert.That(oakCell.Find("Resident squirrel")
                    .gameObject.activeInHierarchy, Is.True,
                    "A non-graphical player still needs an animal fallback.");
            var total = canvas.GetComponentsInChildren<Text>(true)
                .Single(text => text.name == "Balance score");
            Assert.That(total.text, Does.Contain("BUILD 0"));
            Assert.That(total.text, Does.Contain("GOALS"));
            Assert.That(total.preferredHeight,
                Is.LessThanOrEqualTo(total.rectTransform.rect.height + 1f));
            var scorePage = canvas.Find("Building choices/Board progress backing");
            Assert.That(scorePage.Find("Board progress").GetComponent<Text>().text,
                Is.EqualTo("0"),
                "The run total waits for the blue shared-score batch.");
            Assert.That(canvas.Find("Building choices/Story cue backing/Animal score metric")
                .GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(first.interactable, Is.False);
            Assert.That(second.interactable, Is.True);
            Assert.That(third.interactable, Is.False);
            Assert.That(lastReview.gameObject.activeInHierarchy, Is.False);
            Assert.That(ledger.gameObject.activeInHierarchy, Is.False);
            Assert.That(routePanel.gameObject.activeInHierarchy, Is.False);

            var motion = canvas.Find("Score motion overlay");
            yield return WaitForScoreFly(motion, "Score fly Animal 0");
            var oakDay = motion.Find("Score fly Animal 0");
            Assert.That(oakDay, Is.Not.Null,
                "The oak's observed animal point should rise from its room.");
            Assert.That(oakDay.GetComponent<Text>().text, Is.EqualTo("+3"),
                "The first fly represents the squirrel's actual meal.");
            Assert.That(motion.Find("Score fly Animal 1")
                .GetComponent<Text>().text, Is.EqualTo("+2"),
                "The oak adds its source bonus once.");
            Assert.That(oakDay.GetComponent<Image>(), Is.Null,
                "The room score should be a bare number without a backing card.");
            var green = oakDay.GetComponent<Text>().color;
            Assert.That(green.g, Is.GreaterThan(green.r));
            Assert.That(green.g, Is.GreaterThan(green.b));
            Assert.That(motion.GetComponent<CanvasGroup>().blocksRaycasts,
                Is.False);
            yield return WaitForScoresToFinish(controller);
            Assert.That(scorePage.Find("Board progress").GetComponent<Text>().text,
                Is.EqualTo(run.OverallScore.ToString()));

            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            yield return null;
            Assert.That(run.LastPlacementPoints, Is.GreaterThan(0));
            var homeCell = board.Find("Cell 5,4");
            Assert.That(homeCell.Find("Metric side rails")
                .gameObject.activeInHierarchy, Is.False);
            var issueIcons = homeCell.Find("Room issue icons");
            Assert.That(issueIcons, Is.Not.Null);
            Assert.That(issueIcons.gameObject.activeInHierarchy, Is.True);
            foreach (var issue in new[] { "Waste", "MissingWork", "MissingMeal" })
            {
                var icon = issueIcons.Find($"Room issue {issue}");
                Assert.That(icon, Is.Not.Null);
                Assert.That(icon.gameObject.activeInHierarchy, Is.True);
                Assert.That(icon.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(icon.GetComponent<Image>().color,
                    Is.EqualTo(Color.white),
                    "Problem icons should keep their own colors without a red tint.");
            }
            if (threeDReady)
            {
                Assert.That(root.transform.Find(
                    "Construction low-poly world/First resident")
                    .gameObject.activeInHierarchy, Is.True);
                Assert.That(homeCell.Find("First resident")
                    .gameObject.activeInHierarchy, Is.False,
                    "A resident model should not have a duplicate UI marker.");
                var idleLayer = canvas.Find("Idle cue overlay");
                var residentCue = idleLayer.Find("Idle cue First resident");
                Assert.That(residentCue, Is.Not.Null,
                    "An incomplete resident day should show a cue at the model.");
                Assert.That(residentCue.GetComponent<Image>().sprite,
                    Is.Not.Null);
                Assert.That(residentCue.GetComponent<Image>().raycastTarget,
                    Is.False);
                Assert.That(idleLayer.Find("Idle cue Squirrel"), Is.Null,
                    "The fed squirrel should not be marked idle.");
                var resident = root.transform.Find(
                    "Construction low-poly world/First resident");
                var source = root.GetComponent<ConstructionWorldView>()
                    .BoardCamera.WorldToScreenPoint(resident.position);
                var cueScreen = RectTransformUtility.WorldToScreenPoint(null,
                    residentCue.position);
                Assert.That(cueScreen.x, Is.EqualTo(source.x).Within(15f));
                Assert.That(cueScreen.y, Is.GreaterThan(source.y));
            }
            else
                Assert.That(homeCell.Find("First resident")
                    .gameObject.activeInHierarchy, Is.True);
            var buildFly = motion.Find("Score fly Build 0");
            Assert.That(buildFly, Is.Not.Null,
                "The home should first show its one-time oak-neighbour point.");
            Assert.That(buildFly.GetComponent<Text>().text.Split('\n')[0],
                Is.EqualTo($"+{run.LastPlacementPoints}"));
            Assert.That(buildFly.GetComponent<Text>().text,
                Does.Contain("shared edge"),
                "The current score animation should retain its placement explanation.");
            var bronze = buildFly.GetComponent<Text>().color;
            Assert.That(bronze.r, Is.GreaterThan(bronze.g));
            Assert.That(bronze.g, Is.GreaterThan(bronze.b));
            var buildX = buildFly.GetComponent<RectTransform>()
                .anchoredPosition.x;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(buildFly.GetComponent<RectTransform>()
                    .anchoredPosition.x,
                Is.EqualTo(buildX).Within(1f),
                "Placement points should rise over their room, not fly right.");
            yield return new WaitForSecondsRealtime(0.75f);
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(total.text,
                Does.Contain($"BUILD {run.PlacementScore}"));
            Assert.That(total.preferredHeight,
                Is.LessThanOrEqualTo(total.rectTransform.rect.height + 1f));
            Assert.That(controller.TryPlace(4, 3), Is.False,
                "Selecting an occupied home inspects its automatic route.");
            Assert.That(canvas.Find("Building choices/Story cue backing/Open resident dispatch"),Is.Null);
            Assert.That(canvas.Find("Resident dispatch"),Is.Null);
            Assert.That(routePanel.gameObject.activeInHierarchy, Is.False);
            Assert.That(controller.TryPlace(4, 3), Is.False);

            Assert.That(controller.RequestPlacement(5, 3), Is.True);
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(issueIcons.Find("Room issue MissingMeal")
                    .gameObject.activeInHierarchy, Is.True,
                "The meal issue should stay visible while the resident and score move.");
            var overlay = motion.GetComponent<RectTransform>();
            yield return WaitForScoreFly(motion, "Score fly Resident 0");
            var residentFly = motion.Find("Score fly Resident 0");
            Assert.That(residentFly, Is.Not.Null);
            Assert.That(int.Parse(residentFly.GetComponent<Text>().text
                    .TrimStart('+')),
                Is.GreaterThanOrEqualTo(6));
            var gold = residentFly.GetComponent<Text>().color;
            Assert.That(gold.g, Is.EqualTo(0.82f).Within(0.01f),
                "A large resident gain should use the gold tier.");
            var residentTarget = scorePage.Find("Board progress")
                .GetComponent<RectTransform>();
            var residentDestination = (Vector2)overlay.InverseTransformPoint(
                residentTarget.position);
            var residentDistance = Vector2.Distance(residentFly
                .GetComponent<RectTransform>().anchoredPosition,
                residentDestination);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector2.Distance(residentFly.GetComponent<RectTransform>()
                    .anchoredPosition, residentDestination),
                Is.LessThan(residentDistance),
                "Resident points should travel toward the resident score.");

            yield return WaitForScoreFly(motion, "Score fly Animal 0");
            var animalFly = motion.Find("Score fly Animal 0");
            Assert.That(animalFly, Is.Not.Null);
            Assert.That(motion.Find("Score fly Resident 0"), Is.Null,
                "The earlier resident batch must be cleared before animal points appear.");
            var animalColor = animalFly.GetComponent<Text>().color;
            Assert.That(animalColor.g, Is.GreaterThan(animalColor.r));
            var animalTarget = scorePage.Find("Board progress")
                .GetComponent<RectTransform>();
            var animalDestination = (Vector2)overlay.InverseTransformPoint(
                animalTarget.position);
            var animalDistance = Vector2.Distance(animalFly
                .GetComponent<RectTransform>().anchoredPosition,
                animalDestination);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector2.Distance(animalFly.GetComponent<RectTransform>()
                    .anchoredPosition, animalDestination),
                Is.LessThan(animalDistance));

            yield return WaitForScoreFly(motion, "Score fly Shared 0");
            var sharedFly = motion.Find("Score fly Shared 0");
            var blue = sharedFly.GetComponent<Text>().color;
            Assert.That(blue.b, Is.GreaterThan(blue.g));
            Assert.That(blue.g, Is.GreaterThan(blue.r));
            var totalTarget = scorePage.Find("Board progress")
                .GetComponent<RectTransform>();
            var totalDestination = (Vector2)overlay.InverseTransformPoint(
                totalTarget.position);
            var sharedDistance = Vector2.Distance(sharedFly
                .GetComponent<RectTransform>().anchoredPosition,
                totalDestination);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector2.Distance(sharedFly.GetComponent<RectTransform>()
                    .anchoredPosition, totalDestination),
                Is.LessThan(sharedDistance),
                "Shared points should arrive at the run total last.");
            yield return WaitForScoresToFinish(controller);
            Assert.That(total.text, Does.Contain($"SHARED {run.BalanceScore}"));
            Assert.That(scorePage.Find("Board progress").GetComponent<Text>().text,
                Is.EqualTo(run.OverallScore.ToString()));
            Assert.That(lastReview.gameObject.activeInHierarchy, Is.False);
            Assert.That(ledger.gameObject.activeInHierarchy, Is.False);
            Assert.That(routePanel.gameObject.activeInHierarchy, Is.False);

            var issueWait = 0f;
            while (issueIcons.Find("Room issue MissingMeal")
                       .gameObject.activeInHierarchy && issueWait < 10f)
            {
                issueWait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(issueIcons.Find("Room issue MissingMeal")
                    .gameObject.activeInHierarchy, Is.False,
                "A confirmed meal should clear the issue after the activity ends.");

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreeFillSettlementWaitsForPlayerChoiceAndKeepsGoalsVisible()
        {
            var run = FreeFillWithRoomTourComplete();
            var saved = run.Export();
            saved.fillHandCards = new List<ConstructionCategory>
                { ConstructionCategory.Green, ConstructionCategory.Street,
                    ConstructionCategory.Waste };
            Assert.That(ConstructionRunModel.TryRestore(saved, out run), Is.True);
            var root = new GameObject("Free Fill post-placement choice test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                controller.UseLanguageForTesting(InterfaceLanguage.English);
                yield return WaitForBoardCase(root, controller);
                var canvas = root.transform.Find("Construction UI");
                var hand = canvas.Find("Fill hand on tabletop");
                AssertFreeFillWaitState(canvas, hand);

                var greenPlot = LegalFreeFillPlot(run, ConstructionCategory.Green);
                var before = run.BuiltCount;
                Assert.That(controller.RequestPlacement(greenPlot.column,
                    greenPlot.row), Is.False,
                    "A fresh free-play screen must wait for an explicit card choice.");
                Assert.That(run.BuiltCount, Is.EqualTo(before));
                var greenIndex = run.FillHandCards.ToList().IndexOf(
                    ConstructionCategory.Green);
                hand.Find($"Fill hand card {greenIndex + 1}").GetComponent<Button>()
                    .onClick.Invoke();
                Assert.That(controller.RequestPlacement(greenPlot.column,
                    greenPlot.row), Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                Assert.That(run.BuiltCount, Is.EqualTo(before + 1));
                Assert.That(run.BuiltToday, Is.False,
                    "The placement must have completed its automatic settlement.");
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertFreeFillWaitState(canvas, hand);

                var nextGreenPlot = LegalFreeFillPlot(run, ConstructionCategory.Green);
                Assert.That(controller.TryPlace(nextGreenPlot.column,
                    nextGreenPlot.row), Is.False,
                    "Every placement needs a fresh player card choice.");
                Assert.That(run.BuiltCount, Is.EqualTo(before + 1));

                var availableCard = run.FillHandCards.Select((category, index) =>
                    (category, index)).First(item => HasLegalFreeFillPlot(run,
                        item.category));
                var handPlot = LegalFreeFillPlot(run, availableCard.category);
                var card = hand.Find($"Fill hand card {availableCard.index + 1}")
                    .GetComponent<Button>();
                Assert.That(card.interactable, Is.True);
                card.onClick.Invoke();
                Assert.That(card.transform.Find("Selected card tab")
                    .gameObject.activeInHierarchy, Is.True,
                    "The actual click, rather than a background refresh, selects a card.");
                Assert.That(controller.RequestPlacement(handPlot.column,
                    handPlot.row), Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                Assert.That(run.BuiltCount, Is.EqualTo(before + 2));
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertFreeFillWaitState(canvas, hand);
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreeFillSwapCannotChooseACardWhileWaitingForPlayer()
        {
            var run = FreeFillWithRoomTourComplete();
            var root = new GameObject("Free Fill idle swap choice test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                controller.UseLanguageForTesting(InterfaceLanguage.English);
                yield return WaitForBoardCase(root, controller);
                var canvas = root.transform.Find("Construction UI");
                var hand = canvas.Find("Fill hand on tabletop");
                AssertFreeFillWaitState(canvas, hand);
                var cardsBefore = run.FillHandCards.ToArray();
                var swapsBefore = run.FillHandRerollsRemaining;
                Assert.That(swapsBefore, Is.GreaterThan(0));
                var swap = hand.Find("Reroll selected hand card")
                    .GetComponent<Button>();
                Assert.That(swap.interactable, Is.False,
                    "Swap needs a deliberately selected building card.");
                swap.onClick.Invoke();
                Assert.That(run.FillHandCards, Is.EqualTo(cardsBefore));
                Assert.That(run.FillHandRerollsRemaining, Is.EqualTo(swapsBefore));
                AssertFreeFillWaitState(canvas, hand);
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }

        private static ConstructionRunModel FreeFillWithRoomTourComplete()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            run.SetOpeningTourCompleted(true);
            run.SetTutorialTipsSkipped(true);
            Assert.That(run.TryConfirmSpecialTasks(), Is.True);
            var opening = new (ConstructionCategory category, int column, int row)[]
            {
                (ConstructionCategory.Green, 3, 3),
                (ConstructionCategory.Residence, 4, 3),
                (ConstructionCategory.Restaurant, 5, 3),
                (ConstructionCategory.Workshop, 4, 4),
                (ConstructionCategory.Street, 4, 2),
                (ConstructionCategory.Waste, 6, 3),
                (ConstructionCategory.Supermarket, 3, 2),
                (ConstructionCategory.Square, 4, 1)
            };
            foreach (var card in opening)
            {
                Assert.That(run.TryBuild(card.column, card.row, card.category,
                    card.category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            Assert.That(run.IsIntroPhase, Is.False);
            return run;
        }

        private static bool HasLegalFreeFillPlot(ConstructionRunModel run,
            ConstructionCategory category) =>
            Enumerable.Range(0, 7).Any(row => Enumerable.Range(0, 7)
                .Any(column => run.CheckPlacement(column, row, category) ==
                    ConstructionPlacementFailure.None));

        private static (int column, int row) LegalFreeFillPlot(
            ConstructionRunModel run, ConstructionCategory category) =>
            Enumerable.Range(0, 7).SelectMany(row => Enumerable.Range(0, 7)
                .Select(column => (column, row)))
                .First(plot => run.CheckPlacement(plot.column, plot.row,
                    category) == ConstructionPlacementFailure.None);

        private static void AssertFreeFillWaitState(Transform canvas,
            Transform hand)
        {
            Assert.That(canvas.Find("Building choices/Goal progress paper")
                .gameObject.activeInHierarchy, Is.True,
                "Goal progress stays on the left after every placement.");
            foreach (var name in new[]
                     { "Story preview", "Room function", "Story pictogram", "Story divider" })
                Assert.That(canvas.Find($"Building choices/Story cue backing/{name}")
                    .gameObject.activeSelf, Is.False,
                    name + " must not repeat the next card over the goal page.");
            Assert.That(canvas.Find("Building choices/Next step backing/Selected hand name")
                .GetComponent<Text>().text, Does.Contain("Choose a card"));
            foreach (var tab in hand.GetComponentsInChildren<Transform>(true)
                         .Where(child => child.name == "Selected card tab"))
                Assert.That(tab.gameObject.activeSelf, Is.False,
                    "Settlement must not select the newly dealt or remaining card.");
            var board = canvas.GetComponentsInChildren<RectTransform>(true)
                .First(rect => rect.name == "7 by 7 construction board");
            foreach (var hint in board.GetComponentsInChildren<Text>(true)
                         .Where(text => text.name == "Buildable hint"))
                Assert.That(hint.gameObject.activeSelf, Is.False,
                    "Available plots appear only after the player chooses a card.");
        }

        [UnityTest]
        public IEnumerator FourthCardAndSwapControlAppearAfterUnlock()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            run.SetOpeningTourCompleted(true);
            run.SetTutorialTipsSkipped(true);
            Assert.That(run.TryConfirmSpecialTasks(), Is.True);
            var opening = new (ConstructionCategory category, int column,
                int row)[]
            {
                (ConstructionCategory.Green, 3, 3),
                (ConstructionCategory.Residence, 4, 3),
                (ConstructionCategory.Restaurant, 5, 3),
                (ConstructionCategory.Workshop, 4, 4),
                (ConstructionCategory.Street, 4, 2),
                (ConstructionCategory.Waste, 6, 3),
                (ConstructionCategory.Supermarket, 3, 2),
                (ConstructionCategory.Square, 4, 1)
            };
            foreach (var card in opening)
            {
                Assert.That(run.TryBuild(card.column, card.row, card.category,
                    card.category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            while (run.CategoryBuiltCount(ConstructionCategory.Square) < 3 &&
                   !run.IsFinished)
            {
                (ConstructionCategory category, int column, int row)? next = null;
                foreach (var category in run.FillHandCards
                             .OrderBy(card => card == ConstructionCategory.Square
                                 ? 0 : 1))
                for (var row = 0; row < 7 && !next.HasValue; row++)
                for (var column = 0; column < 7 && !next.HasValue; column++)
                    if (run.CheckPlacement(column, row, category) ==
                        ConstructionPlacementFailure.None)
                        next = (category, column, row);
                Assert.That(next.HasValue, Is.True);
                Assert.That(run.TryBuild(next.Value.column, next.Value.row,
                    next.Value.category, GreenPlanting.None,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            Assert.That(run.FillHandSize, Is.EqualTo(4));
            var root = new GameObject("Expanded Fill hand presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return WaitForBoardCase(root, controller);
            Canvas.ForceUpdateCanvases();
            var hand = root.transform.Find(
                "Construction UI/Fill hand on tabletop");
            Assert.That(hand, Is.Not.Null);
            var goalOverview = root.transform.Find(
                "Construction UI/Building choices/Goal progress paper/Goal progress overview");
            Assert.That(goalOverview.gameObject.activeInHierarchy, Is.True);
            for (var goalIndex = 0; goalIndex < 3; goalIndex++)
            {
                var row = goalOverview.Find($"Goal progress row {goalIndex + 1}");
                Assert.That(row.Find("Goal title").GetComponent<Text>().text,
                    Is.Not.Empty);
                Assert.That(row.Find("Goal count").GetComponent<Text>().text,
                    Is.Not.Empty);
            }
            Assert.That(root.transform.Find(
                    "Construction UI/Building choices/Story cue backing/Story preview")
                .gameObject.activeSelf, Is.False,
                "Free-play card copy must leave the goal page clear.");
            Assert.That(root.transform.Find(
                    "Construction UI/Building choices/Story cue backing/Room function")
                .gameObject.activeSelf, Is.False);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var fillCanvas = root.transform.Find("Construction UI")
                .GetComponent<Canvas>();
            foreach (var category in new[] { "Green", "Street", "Waste" })
            {
                Assert.That(root.transform.Find(
                    $"Construction UI/Basic resource {category}"), Is.Null,
                    "All categories belong to the bottom hand.");
            }
            var boardRect = root.transform.GetComponentsInChildren<RectTransform>(true)
                .First(rect => rect.name == "7 by 7 construction board");
            var boardCorners = new Vector3[4];
            boardRect.GetWorldCorners(boardCorners);
            var cardCorners = new Vector3[4];
            hand.Find("Fill hand card 4").GetComponent<RectTransform>()
                .GetWorldCorners(cardCorners);
            Assert.That(cardCorners.Max(corner => corner.y),
                Is.LessThan(boardCorners.Min(corner => corner.y)));
            Assert.That(hand.Find("Fill hand card 4").gameObject.activeInHierarchy,
                Is.True);
            Assert.That(hand.Find("Fill hand card 5").gameObject.activeInHierarchy,
                Is.False);
            Assert.That(hand.GetComponent<RectTransform>().sizeDelta.x,
                Is.LessThan(760f));
            var swap = hand.Find("Reroll selected hand card")
                .GetComponent<Button>();
            Assert.That(swap.gameObject.activeInHierarchy, Is.True);
            Assert.That(swap.GetComponent<Image>().sprite.name,
                Is.EqualTo("Hand art compact_swap_v2"));
            Assert.That(swap.GetComponentInChildren<Text>().text,
                Does.Contain($"换牌 ×{run.FillHandRerollsRemaining}"));
            var slot = run.FillHandCards.ToList().FindIndex(
                run.CanRerollFillHandCard);
            // A random late deck can contain no legal replacement even when
            // reroll opportunities have accumulated.
            if (slot >= 0)
            {
                hand.Find($"Fill hand card {slot + 1}").GetComponent<Button>()
                    .onClick.Invoke();
                yield return null;
                var remaining = run.FillHandRerollsRemaining;
                Assert.That(swap.interactable, Is.True);
                swap.onClick.Invoke();
                yield return null;
                Assert.That(run.FillHandCards.Count, Is.EqualTo(4));
                Assert.That(run.FillHandRerollsRemaining,
                    Is.EqualTo(remaining - 1));
                Assert.That(swap.GetComponentInChildren<Text>().text,
                    Does.Contain($"换牌 ×{remaining - 1}"));
            }
            else
            {
                Assert.That(swap.interactable, Is.False);
                Assert.That(run.FillHandCards.Count, Is.EqualTo(4));
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessHorizontalPanButtonsAreClickable()
        {
            var root = new GameObject("Endless horizontal pan test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel(
                ConstructionGameMode.Endless));
            yield return WaitForBoardCase(root, controller);
            var rulesConfirm = root.GetComponentsInChildren<Button>(true)
                .First(button => button.name == "Confirm ending rules");
            rulesConfirm.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var canvas = root.transform.Find("Construction UI")
                .GetComponent<Canvas>();
            var coordinate = canvas.transform.Find(
                    "Viewport coordinates/Viewport position")
                .GetComponent<Text>();
            var startingPosition = coordinate.text;
            foreach (var direction in new[] { "east", "west" })
            {
                var button = canvas.transform.Find("Pan board " + direction)
                    .GetComponent<Button>();
                Assert.That(button.gameObject.activeInHierarchy, Is.True);
                var rect = button.GetComponent<RectTransform>();
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(
                        canvas.worldCamera,
                        rect.TransformPoint(rect.rect.center))
                };
                var hits = new List<RaycastResult>();
                canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject, Is.SameAs(button.gameObject),
                    direction + " button is covered by " +
                    string.Join(", ", hits.ConvertAll(hit => hit.gameObject.name)));
                button.onClick.Invoke();
                yield return null;
                if (direction == "east")
                    Assert.That(coordinate.text, Is.Not.EqualTo(startingPosition));
                else
                    Assert.That(coordinate.text, Is.EqualTo(startingPosition));
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OtherModesKeepTheirExistingControls()
        {
            foreach (var mode in new[]
                     { ConstructionGameMode.Endless,
                         ConstructionGameMode.BoardGame })
            {
                var root = new GameObject($"Non-hand {mode} test");
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(new ConstructionRunModel(mode));
                yield return WaitForBoardCase(root, controller);
                var canvas = root.transform.Find("Construction UI");
                Assert.That(canvas.Find("Fill hand on tabletop"), Is.Null);
                Assert.That(canvas.Find("Special task selection"), Is.Null);
                Assert.That(canvas.Find("Special task tray"), Is.Null);
                Assert.That(canvas.Find("Building choices"), Is.Not.Null);
                Assert.That(canvas.Find(
                    "7 by 7 construction board/Cell 4,4/Metric side rails")
                    .gameObject.activeSelf, Is.True,
                    "Other modes retain their established room metrics.");
                Object.Destroy(root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CompactTrayFitsThreeFourAndFiveCardsWithoutClipping()
        {
            foreach (var layout in new[] { (size: 3, intro: true),
                         (size: 3, intro: false), (size: 4, intro: false),
                         (size: 5, intro: false) })
            {
                var size = layout.size;
                var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                    useFillHand: true);
                if (layout.intro)
                    Assert.That(run.TryConfirmSpecialTasks(), Is.True);
                if (!layout.intro)
                {
                    Assert.That(run.SkipFillTutorial(), Is.True);
                    Assert.That(run.TryConfirmSpecialTasks(), Is.True);
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var cards = (List<ConstructionCategory>)typeof(ConstructionRunModel)
                        .GetField("fillHandCards", flags).GetValue(run);
                    cards.Clear();
                    cards.AddRange(new[] { ConstructionCategory.Residence,
                        ConstructionCategory.Restaurant, ConstructionCategory.Supermarket,
                        ConstructionCategory.Workshop, ConstructionCategory.Square }.Take(size));
                    typeof(ConstructionRunModel).GetField("fillHandExpanded", flags)
                        .SetValue(run, size >= 4);
                    typeof(ConstructionRunModel).GetField("fillHandExpandedToFive", flags)
                        .SetValue(run, size >= 5);
                }
                var root = new GameObject($"Compact tray {size} cards");
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
                yield return WaitForBoardCase(root, controller);
                yield return new WaitForSecondsRealtime(0.7f);
                Canvas.ForceUpdateCanvases();
                var canvas = root.transform.Find("Construction UI");
                var hand = canvas.Find("Fill hand on tabletop") as RectTransform;
                var tray = canvas.Find("Warm wood card rest") as RectTransform;
                var note = hand.Find("Hand note paper") as RectTransform;
                var deck = hand.Find("Top card ink edge") as RectTransform;
                var deckBounds = BoundsInHand(deck, hand);
                var trayBounds = BoundsInHand(tray, hand);
                Assert.That(note.gameObject.activeSelf, Is.EqualTo(layout.intro),
                    "The tutorial note should disappear in free play.");
                Rect previous = default;
                if (layout.intro)
                {
                    previous = BoundsInHand(note, hand);
                    Assert.That(previous.yMin, Is.GreaterThan(trayBounds.yMin));
                    Assert.That(previous.xMin, Is.GreaterThan(trayBounds.xMin));
                }
                for (var index = 0; index < size; index++)
                {
                    var card = hand.Find($"Fill hand card {index + 1}") as RectTransform;
                    var bounds = BoundsInHand(card, hand);
                    Assert.That(bounds.width, Is.EqualTo(deckBounds.width).Within(0.5f));
                    Assert.That(bounds.height, Is.EqualTo(deckBounds.height).Within(0.5f));
                    if (index > 0 || layout.intro)
                    {
                        Assert.That(bounds.xMin, Is.GreaterThan(previous.xMax + 8f));
                        Assert.That(bounds.xMin - previous.xMax, Is.LessThan(21f));
                    }
                    Assert.That(bounds.xMin, Is.GreaterThan(trayBounds.xMin));
                    Assert.That(bounds.yMin, Is.GreaterThan(trayBounds.yMin));
                    Assert.That(bounds.yMax, Is.LessThan(trayBounds.yMax));
                    previous = bounds;
                }
                Assert.That(deckBounds.xMin - previous.xMax,
                    Is.InRange(18f, 22f));
                Assert.That(deckBounds.xMax, Is.LessThan(trayBounds.xMax));
                var swap = hand.Find("Reroll selected hand card");
                Assert.That(swap.gameObject.activeSelf, Is.EqualTo(!layout.intro));
                if (!layout.intro)
                {
                    var swapBounds = BoundsInHand(swap as RectTransform, hand);
                    Assert.That(swapBounds.xMin, Is.GreaterThan(deckBounds.xMin));
                    Assert.That(swapBounds.xMax, Is.LessThan(deckBounds.xMax));
                    Assert.That(swapBounds.yMin, Is.GreaterThan(deckBounds.yMin));
                }
                var capture = System.Environment.GetEnvironmentVariable("SYMBIOSIS_UI_CAPTURE_DIR");
                if (!string.IsNullOrEmpty(capture) &&
                    SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    System.IO.Directory.CreateDirectory(capture);
                    yield return CapturePresentation(root, System.IO.Path.Combine(capture,
                        $"compact-tray-{size}{(layout.intro ? "-tutorial" : "")}.png"));
                }
                Object.Destroy(root);
                yield return null;
            }
        }

        private static IEnumerator CapturePresentation(GameObject root, string path)
        {
            var view = root.GetComponentInChildren<ConstructionWorldView>(true);
            var camera = view.BoardCamera;
            var canvas = root.GetComponentInChildren<Canvas>();
            var previousTarget = camera.targetTexture;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousDistance = canvas.planeDistance;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24);
            var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                shot.Apply();
                // Capture is opt-in; keep the game's minimal module set unchanged.
                var encodingModule = Assembly.LoadFrom(System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(typeof(Camera).Assembly.Location),
                    "UnityEngine.ImageConversionModule.dll"));
                var encode = encodingModule.GetType("UnityEngine.ImageConversion")
                    .GetMethod("EncodeToPNG", new[] { typeof(Texture2D) });
                var png = (byte[])encode.Invoke(null, new object[] { shot });
                System.IO.File.WriteAllBytes(path, png);
                var detail = new Texture2D(1280, 210, TextureFormat.RGB24, false);
                try
                {
                    detail.ReadPixels(new Rect(320, 0, 1280, 210), 0, 0);
                    detail.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(path),
                        System.IO.Path.GetFileNameWithoutExtension(path) + "-detail.png"),
                        (byte[])encode.Invoke(null, new object[] { detail }));
                }
                finally { Object.Destroy(detail); }
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(shot);
                target.Release();
                Object.Destroy(target);
            }
        }

        private static Rect BoundsInHand(RectTransform item, RectTransform hand)
        {
            var corners = new Vector3[4];
            item.GetWorldCorners(corners);
            var local = corners.Select(point => hand.InverseTransformPoint(point)).ToArray();
            return Rect.MinMaxRect(local.Min(point => point.x), local.Min(point => point.y),
                local.Max(point => point.x), local.Max(point => point.y));
        }

        private static IEnumerator WaitForBoardCase(GameObject root,
            ConstructionPrototypeController controller)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while ((root.transform.Find("Construction UI") == null ||
                    !controller.IsBoardCaseOpen) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True,
                "The construction UI did not finish opening.");
        }

        private static IEnumerator WaitForSpecialTaskSelection(GameObject root)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            Transform selection = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                selection = root.transform.Find(
                    "Construction UI/Special task selection");
                if (selection != null && selection.gameObject.activeInHierarchy &&
                    selection.GetComponent<CanvasGroup>().alpha >= 0.99f &&
                    selection.Find("Confirm special tasks")
                        .GetComponent<Button>().interactable)
                    yield break;
                yield return null;
            }
            Assert.Fail("The opening special-task cards did not finish sliding in.");
        }

        private static IEnumerator WaitForScoresToFinish(ConstructionPrototypeController controller)
        {
            var field=typeof(ConstructionPrototypeController).GetField("scoreMotionAnimation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var deadline=Time.realtimeSinceStartup+12f;
            while(field.GetValue(controller)!=null && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(field.GetValue(controller),Is.Null,"All three settlement batches must finish.");
        }

        private static IEnumerator WaitForScoreFly(Transform overlay, string name)
        {
            var deadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var cue = overlay.Find(name);
                if (cue != null && cue.gameObject.activeInHierarchy)
                    yield break;
                yield return null;
            }
            Assert.Fail($"The score batch {name} did not appear.");
        }

        private static IEnumerator WaitForSpecialTaskTray(GameObject root,
            ConstructionRunModel run)
        {
            var deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var tray = root.transform.Find(
                    "Construction UI/Special task tray");
                if (run.IsSpecialTaskSelectionConfirmed && tray != null &&
                    tray.gameObject.activeInHierarchy)
                    yield break;
                yield return null;
            }
            Assert.Fail("Confirmed special tasks did not collapse into the tray.");
        }
    }
}
