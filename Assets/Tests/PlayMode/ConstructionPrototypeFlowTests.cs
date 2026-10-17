using System;
using System.Collections;
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
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionPrototypeFlowTests
    {
        [UnityTest]
        public IEnumerator ConstructionWildlifeIsVisibleAndMovesWithoutCrossRoomFood()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));

            var root = new GameObject("Construction wildlife presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }

            var world = root.transform.Find("Construction low-poly world");
            Assert.That(world, Is.Not.Null);
            Assert.That(world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>(), Is.Null);
            Assert.That(world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.FoxDemoVisual>(), Is.Null);
            var view = root.GetComponent<ConstructionWorldView>();
            var squirrel = world.Find("Squirrel");
            Assert.That(squirrel, Is.Not.Null);
            Assert.That(squirrel.gameObject.activeInHierarchy, Is.True);
            Assert.That(System.Array.Exists(
                squirrel.GetComponentsInChildren<Renderer>(), renderer =>
                    renderer.enabled && renderer.gameObject.activeInHierarchy),
                Is.True, "The construction squirrel must actually render.");
            var birds = world.GetComponentsInChildren<
                UrbanWildlifeRooms.Animals.PigeonDemoVisual>();
            Assert.That(birds.Length, Is.EqualTo(2),
                "The two arriving pigeons need two visible 3D animals, not one static icon.");
            foreach (var bird in birds)
                Assert.That(System.Array.Exists(
                    bird.GetComponentsInChildren<Renderer>(), renderer =>
                        renderer.enabled && renderer.gameObject.activeInHierarchy),
                    Is.True, "Every displayed pigeon must actually render.");
            var idleSquirrelStart = squirrel.position;
            var idlePigeonStart = birds[0].transform.position;
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(Vector3.Distance(idleSquirrelStart, squirrel.position),
                Is.GreaterThan(0.03f),
                "The squirrel should move even while no day-end route is playing.");
            Assert.That(Vector3.Distance(idlePigeonStart, birds[0].transform.position),
                Is.GreaterThan(0.03f),
                "Pigeons should move even while no day-end route is playing.");
            var squirrelRoute = run.PreviewSquirrelFoodRoute();
            Assert.That(squirrelRoute.Count, Is.EqualTo(1),
                "This checks visible foraging even when food is in the home room.");
            var pigeonRoute = run.PreviewPigeonFoodRoute();
            Assert.That(pigeonRoute.Count, Is.GreaterThan(0));
            view.PlayForaging(
                new System.Collections.Generic.IReadOnlyList<ConstructionTileData>[]
                    { squirrelRoute },
                new System.Collections.Generic.IReadOnlyList<ConstructionTileData>[]
                    { pigeonRoute, pigeonRoute });
            var start = squirrel.position;
            var firstBirdStart = birds[0].transform.position;
            var secondBirdStart = birds[1].transform.position;
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(Vector3.Distance(start, squirrel.position),
                Is.GreaterThan(0.08f),
                "A same-room food route should not leave the squirrel motionless.");
            Assert.That(Vector3.Distance(firstBirdStart,
                birds[0].transform.position), Is.GreaterThan(0.08f));
            Assert.That(Vector3.Distance(secondBirdStart,
                birds[1].transform.position), Is.GreaterThan(0.08f),
                "The second fed pigeon must forage too, not just the first bird.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoFedSquirrelsBothShowTheirFoodTrips()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var position in new[] { (4, 3), (3, 2) })
            {
                Assert.That(run.TryBuild(position.Item1, position.Item2,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.SquirrelPopulation, Is.EqualTo(2));
            var routes = run.PreviewSquirrelFoodRoutes();
            Assert.That(routes.Count, Is.EqualTo(2));

            var root = new GameObject("Two squirrel forage presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var squirrels = root.transform.Find("Construction low-poly world")
                .GetComponentsInChildren<UrbanWildlifeRooms.Animals.SquirrelDemoVisual>();
            Assert.That(squirrels.Length, Is.EqualTo(2));
            var view = root.GetComponent<ConstructionWorldView>();
            var firstStart = squirrels[0].transform.position;
            var secondStart = squirrels[1].transform.position;
            view.PlayForaging(routes, null);
            var firstImmediate = squirrels[0].transform.position;
            var secondImmediate = squirrels[1].transform.position;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Mathf.Max(Vector3.Distance(firstStart, firstImmediate),
                Vector3.Distance(firstStart, squirrels[0].transform.position)),
                Is.GreaterThan(0.08f));
            Assert.That(Mathf.Max(Vector3.Distance(secondStart, secondImmediate),
                Vector3.Distance(secondStart, squirrels[1].transform.position)),
                Is.GreaterThan(0.08f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatureShrubsAndPigeonsBringVisibleMovingVisitors()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            // The final meadow needs one more day to reach mature habitat.
            Assert.That(run.TryBuild(5, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.HedgehogHabitat, Is.Not.Null);
            Assert.That(run.FoxHabitat, Is.Not.Null);
            var root = new GameObject("Construction habitat visitors test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var world = root.transform.Find("Construction low-poly world");
            Assert.That(world, Is.Not.Null);
            var hedgehog = world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>();
            var fox = world.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.FoxDemoVisual>();
            Assert.That(hedgehog, Is.Not.Null);
            Assert.That(fox, Is.Not.Null);
            foreach (var actor in new[] { hedgehog.transform, fox.transform })
                Assert.That(System.Array.Exists(
                    actor.GetComponentsInChildren<Renderer>(), renderer =>
                        renderer.enabled && renderer.gameObject.activeInHierarchy),
                    Is.True, "Each visitor needs an enabled 3D renderer.");
            var hedgehogStart = hedgehog.transform.position;
            var foxStart = fox.transform.position;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Vector3.Distance(hedgehogStart,
                hedgehog.transform.position), Is.GreaterThan(0.03f));
            Assert.That(Vector3.Distance(foxStart, fox.transform.position),
                Is.GreaterThan(0.03f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FoxHuntIsReportedAndFollowsTheSettledGreenRoute()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 6, row: 3, planting: GreenPlanting.Shrub)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var riskRoot = new GameObject("Construction fox risk preview test");
            var riskController = riskRoot.AddComponent<ConstructionPrototypeController>();
            riskController.UseRunForTesting(run);
            riskController.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var riskPreview = riskRoot.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback preview")
                .GetComponent<Text>();
            Assert.That(riskPreview.text, Does.Contain("Fox risk"));
            var riskIcon = riskRoot.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback icon")
                .GetComponent<Image>();
            Assert.That(riskIcon.sprite, Is.Not.Null);
            Assert.That(riskIcon.sprite.texture.name, Does.Contain("population-fox"));
            var forecast = run.PreviewEcologyDay();
            var riskTile = run.BuiltTiles.First(tile =>
                tile.id == forecast.predationTileId);
            var riskCell = riskRoot.GetComponentsInChildren<Transform>(true)
                .First(item => item.name ==
                    $"Cell {riskTile.column + 1},{riskTile.row + 1}");
            Assert.That(riskCell.Find("Fox risk").gameObject.activeSelf, Is.True,
                "The fox warning must identify the actual threatened room.");
            Assert.That(riskRoot.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "Fox risk" && item.gameObject.activeSelf),
                Is.EqualTo(1));
            var feedbackPanel = riskRoot.transform.Find(
                "Construction UI/Building choices/Feedback backing");
            feedbackPanel.GetComponent<EventTrigger>().OnPointerClick(
                new PointerEventData(EventSystem.current));
            var focusedText = riskRoot.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>();
            Assert.That(riskCell.Find("Fox risk").localScale.x,
                Is.GreaterThan(1f));
            Assert.That(riskPreview.text, Does.Contain(
                $"{riskTile.column + 1},{riskTile.row + 1}"));
            Assert.That(focusedText.raycastTarget, Is.False,
                "The feedback label must not block clicks on its backing.");
            var riskView = riskRoot.GetComponent<ConstructionWorldView>();
            var liveSquirrel = SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null ? null :
                riskRoot.transform.Find("Construction low-poly world/Squirrel");
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.That(liveSquirrel, Is.Not.Null);
            if (liveSquirrel != null)
                Assert.That(liveSquirrel.gameObject.activeInHierarchy, Is.True);
            Assert.That(run.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(ecology.foxHuntRouteTileIds.Count, Is.GreaterThan(1));
            if (liveSquirrel != null)
            {
                riskView.Sync(run, false);
                Assert.That(liveSquirrel.gameObject.activeSelf, Is.False,
                    "The settled population should already reflect the hunt.");
                riskView.PlayFoxHunt(ecology.foxHuntRouteTileIds, run.BuiltTiles,
                    ecology.predatedSpecies);
                Assert.That(liveSquirrel.gameObject.activeInHierarchy, Is.True,
                    "Keep the victim visible until the fox reaches it.");
                yield return new WaitForSecondsRealtime(1.6f);
                Assert.That(liveSquirrel.gameObject.activeSelf, Is.False,
                    "The victim should disappear when the fox reaches its tile.");
            }
            Object.Destroy(riskRoot);
            yield return null;
            var root = new GameObject("Construction fox hunt presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var review = root.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(review.text, Does.Contain("Fox hunted one squirrel"));
            var nextForecast = run.PreviewEcologyDay();
            var activeWarnings = root.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "Fox risk" && item.gameObject.activeSelf);
            Assert.That(activeWarnings, Is.EqualTo(
                nextForecast.predatedSpecies == ConstructionPreySpecies.None ? 0 : 1),
                "An obsolete fox warning must clear after the next day settles.");
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var fox = root.GetComponentInChildren<
                    UrbanWildlifeRooms.Animals.FoxDemoVisual>();
                Assert.That(fox, Is.Not.Null);
                var start = fox.transform.position;
                root.GetComponent<ConstructionWorldView>().PlayFoxHunt(
                    ecology.foxHuntRouteTileIds, run.BuiltTiles,
                    ecology.predatedSpecies);
                var replayPrey = root.transform.Find(
                    "Construction low-poly world/Fox hunt prey") ??
                    root.transform.Find("Construction low-poly world/Squirrel");
                Assert.That(replayPrey, Is.Not.Null,
                    "A replay needs either the hidden original or a temporary prey visual.");
                Assert.That(replayPrey.gameObject.activeInHierarchy, Is.True);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(Vector3.Distance(start, fox.transform.position),
                    Is.GreaterThan(0.3f));
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(replayPrey.gameObject.activeSelf, Is.False);
            }
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HedgehogStaysVisibleUntilTheFoxReachesItsShrub()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 3, row: 4, planting: GreenPlanting.Oak),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var save = run.Export();
            save.pigeonTileId = run.At(4, 3).id;
            Assert.That(ConstructionRunModel.TryRestore(save, out var sheltered),
                Is.True);
            Assert.That(sheltered.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));

            var root = new GameObject("Construction hedgehog hunt visual test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(sheltered);
            yield return null;
            if (SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Object.Destroy(root);
                yield break;
            }
            var view = root.GetComponent<ConstructionWorldView>();
            var hedgehog = root.GetComponentInChildren<
                UrbanWildlifeRooms.Animals.HedgehogDemoVisual>();
            Assert.That(hedgehog, Is.Not.Null);
            Assert.That(hedgehog.gameObject.activeInHierarchy, Is.True);
            Assert.That(sheltered.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));
            view.Sync(sheltered, false);
            Assert.That(hedgehog.gameObject.activeSelf, Is.False);
            view.PlayFoxHunt(ecology.foxHuntRouteTileIds, sheltered.BuiltTiles,
                ecology.predatedSpecies);
            Assert.That(hedgehog.gameObject.activeInHierarchy, Is.True);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(hedgehog.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoryTutorialPointsFromOakToMeadowAndBuildConfirmation()
        {
            var root = new GameObject("Construction tutorial focus test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var focus = root.GetComponentsInChildren<RectTransform>(true)
                .Single(rect => rect.name == "Tutorial focus");
            Assert.That(focus.parent.name, Is.EqualTo("Cell 4,4"));
            Assert.That(focus.GetComponentsInChildren<Image>(true)
                .All(edge => !edge.raycastTarget), Is.True,
                "Tutorial focus must not intercept board or button clicks.");

            controller.TryPlace(3, 3);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(focus.parent.name, Is.EqualTo("Open build palette"));

            controller.ToggleBuildPalette();
            Assert.That(focus.parent.name, Is.EqualTo("Plant Meadow"));
            var canvas = root.transform.Find("Construction UI");
            canvas.Find("Building choices/Green planting/Plant Meadow")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(focus.parent.name, Does.StartWith("Cell "));
            Assert.That(focus.parent.Find("Buildable hint").gameObject.activeSelf,
                Is.True);

            Assert.That(controller.RequestPlacement(4, 3), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Confirm placement"));
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            controller.ToggleLanguage();
            Assert.That(canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>().text, Does.Contain("草地"));
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(focus.parent.name, Is.EqualTo("Cell 5,4"),
                "The next lesson should point to the newly arrived pigeons.");
            var storyIcon = canvas.Find(
                "Building choices/Story cue backing/Story pictogram")
                .GetComponent<Image>();
            Assert.That(storyIcon.sprite, Is.EqualTo(canvas.Find(
                "Building choices/Green planting/Plant Meadow/Plant icon")
                .GetComponent<Image>().sprite),
                "Pigeon inspection must show the meadow, not a future home.");
            controller.TryPlace(4, 3);
            Assert.That(focus.parent.name, Is.EqualTo("Open build palette"));
            Assert.That(storyIcon.sprite, Is.EqualTo(canvas.Find(
                "Building choices/Build Residence/Building icon")
                .GetComponent<Image>().sprite));
            controller.ToggleBuildPalette();
            Assert.That(focus.parent.name, Is.EqualTo("Build Residence"));
            canvas.Find("Building choices/Build Residence")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(focus.parent.name, Does.StartWith("Cell "));
            Assert.That(controller.RequestPlacement(3, 2), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Confirm placement"));
            Assert.That(controller.ConfirmPendingPlacement(), Is.True);
            Assert.That(focus.parent.name, Is.EqualTo("Advance to next day"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TutorialTooltipsMatchAvailableActionsAndLeaveDayButtonClear()
        {
            var root = new GameObject("Construction tutorial action hints test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var tooltip = canvas.Find("Context tooltip");
            var tooltipDetails = tooltip.Find("Tooltip details").GetComponent<Text>();
            var emptyPlot = canvas.Find("7 by 7 construction board/Cell 4,3")
                .GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(850f, 460f) };
            emptyPlot.OnPointerEnter(pointer);
            Assert.That(tooltipDetails.text, Does.Contain("Building starts tomorrow"),
                "The first-day empty plot must not promise an unavailable build.");
            emptyPlot.OnPointerExit(pointer);

            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(controller.AdvanceDay(), Is.True);
            var heading = canvas.Find("Building choices/Next step backing/Next step heading")
                .GetComponent<Text>();
            var guide = canvas.Find("Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(heading.text, Is.EqualTo("CURRENT BUILD"));
            Assert.That(guide.text, Does.Contain("Story tip: Meadow green"),
                "The story suggestion must be distinguished from the current Oak selection.");
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(guide.text, Does.Contain("故事建议：草地绿地"));
            Assert.That(guide.preferredHeight,
                Is.LessThanOrEqualTo(guide.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();

            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(guide.text, Does.Not.Contain("Story tip"));
            emptyPlot.OnPointerEnter(pointer);
            Assert.That(tooltipDetails.text, Does.Contain("Click to preview"));
            emptyPlot.OnPointerExit(pointer);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(heading.text, Is.EqualTo("NEXT STEP"));
            var unbuiltPlot = canvas.Find("7 by 7 construction board/Cell 3,4")
                .GetComponent<EventTrigger>();
            unbuiltPlot.OnPointerEnter(pointer);
            Assert.That(tooltipDetails.text, Does.Contain("Today's room is built"));
            unbuiltPlot.OnPointerExit(pointer);

            var feedback = canvas.Find("Building choices/Feedback backing")
                .GetComponent<EventTrigger>();
            feedback.OnPointerEnter(pointer);
            Assert.That(tooltip.GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(-125f).Within(0.01f),
                "Status tooltip must stay above the Next Day button.");
            feedback.OnPointerExit(pointer);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChineseStoryCuesStayReadableThroughTheFullTutorial()
        {
            var root = new GameObject("Chinese construction tutorial test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var openingDeadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsBoardCaseOpen &&
                Time.realtimeSinceStartup < openingDeadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True,
                "The tutorial focus is only visible after the board case opens.");
            AssertStoryPresentationFits(controller);
            controller.TryPlace(3, 3);
            AssertStoryPresentationFits(controller);
            Assert.That(controller.AdvanceDay(), Is.True);
            AssertStoryPresentationFits(controller);

            PlaceAndAdvance(controller, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(controller.TryPlace(4, 3), Is.False);
            AssertStoryPresentationFits(controller);
            PlaceAndAdvance(controller, 3, 2, ConstructionCategory.Residence);
            PlaceAndAdvance(controller, 3, 1, ConstructionCategory.Restaurant);
            PlaceAndAdvance(controller, 2, 2, ConstructionCategory.Workshop);
            PlaceAndAdvance(controller, 2, 1, ConstructionCategory.Waste);
            PlaceAndAdvance(controller, 5, 3, ConstructionCategory.Residence);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildStreet));
            Assert.That(controller.TrySelect(ConstructionCategory.Street), Is.True);
            var focus = root.GetComponentsInChildren<RectTransform>(true)
                .Single(rect => rect.name == "Tutorial focus");
            Assert.That(focus.parent.name, Is.EqualTo("Cell 5,3"),
                "The street hint should start at a buildable link toward the disconnected home.");
            Assert.That(focus.parent.Find("Buildable hint").gameObject.activeSelf,
                Is.True, "Tutorial focus must not ask the player to build on an occupied tile.");
            PlaceAndAdvance(controller, 4, 2, ConstructionCategory.Street);
            Assert.That(focus.parent.name, Does.StartWith("Cell "));
            Assert.That(focus.parent.Find("Buildable hint").gameObject.activeSelf,
                Is.True, "Either side of the street link may be built next.");
            PlaceAndAdvance(controller, 5, 2, ConstructionCategory.Street);
            Assert.That(focus.parent.name, Is.EqualTo("Cell 5,2"));
            PlaceAndAdvance(controller, 4, 1, ConstructionCategory.Street);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            var canvas = root.transform.Find("Construction UI");
            Assert.That(canvas.Find("Building choices/Story cue backing/Story pictogram")
                .GetComponent<Image>().sprite, Is.EqualTo(canvas.Find(
                    "Building choices/Build Waste/Building icon")
                    .GetComponent<Image>().sprite),
                "When waste blocks food access, the cue should show the remedy.");
            PlaceAndAdvance(controller, 6, 3, ConstructionCategory.Waste);
            PlaceAndAdvance(controller, 5, 1, ConstructionCategory.Supermarket);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.FreeBuild));
            AssertStoryPresentationFits(controller);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IconSummaryAndHoverRevealDetailsWithoutCoveringBoard()
        {
            var root = new GameObject("Construction hover UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);

            var canvas = root.transform.Find("Construction UI");
            var story = canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            var preview = canvas.Find("Building choices/Story cue backing/Story preview")
                .GetComponent<Text>();
            Assert.That(preview.text.Length, Is.LessThan(story.text.Length));
            var metric = canvas.Find("Building choices/Story cue backing/Animal meals metric");
            Assert.That(metric.Find("Value").GetComponent<Text>().text, Is.EqualTo("0"));
            var tooltip = canvas.Find("Context tooltip");
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            metric.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.True);
            Assert.That(tooltip.Find("Tooltip title").GetComponent<Text>().text,
                Is.EqualTo("Animal meals"));
            Assert.That(tooltip.GetComponent<RectTransform>().anchoredPosition.x,
                Is.EqualTo(-568f).Within(0.01f));
            metric.GetComponent<EventTrigger>().OnPointerExit(pointer);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);

            var starter = canvas.Find("7 by 7 construction board/Cell 4,4");
            starter.GetComponent<EventTrigger>().OnPointerEnter(pointer);
            Assert.That(tooltip.Find("Tooltip details").GetComponent<Text>().text,
                Does.Contain("food"));
            Assert.That(tooltip.GetComponent<RectTransform>().anchoredPosition.x,
                Is.EqualTo(568f).Within(0.01f));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCanChooseBuildingWhilePlacementAndDailyLimitsStillApply()
        {
            var root = new GameObject("Construction free choice test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            controller.ToggleBuildPalette();
            var canvas = root.transform.Find("Construction UI");
            var residence = canvas.Find("Building choices/Build Residence")
                .GetComponent<Button>();
            var status = residence.transform.Find("Unlock status")
                .GetComponent<Text>();
            Assert.That(residence.interactable, Is.True,
                "The first free construction day must offer a residence.");
            Assert.That(status.text, Does.Contain("plots"));
            var restaurant = canvas.Find("Building choices/Build Restaurant")
                .GetComponent<Button>();
            Assert.That(restaurant.transform.Find("Unlock status")
                .GetComponent<Text>().text, Does.Contain("Needs home/road"));
            restaurant.onClick.Invoke();
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("0 valid plot"));
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("needs a home"));
            controller.ToggleBuildPalette();
            residence.onClick.Invoke();
            Assert.That(canvas.Find("Building choices/Build palette backing")
                .gameObject.activeSelf, Is.False,
                "Choosing a building should close the list and show valid plots.");
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("Selected: Residence"));
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True,
                "A residence is allowed before the pigeon lesson.");
            Assert.That(canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>().text, Does.Contain("resident arrived early"),
                "The story should acknowledge a player-led build order.");
            Assert.That(controller.TrySelect(ConstructionCategory.Square), Is.True,
                "Players may already choose tomorrow's building.");
            Assert.That(controller.TryPlace(3, 4), Is.False,
                "Choosing tomorrow's type must not bypass one build per day.");
            Assert.That(canvas.Find("Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("One room per day"));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TryPlace(3, 4), Is.True,
                "The selected type persists into the next build day.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardPlacementNeedsConfirmationAndCancelKeepsDailyBuild()
        {
            var root = new GameObject("Construction confirmation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);

            var canvas = root.transform.Find("Construction UI");
            var firstPlot = canvas.Find("7 by 7 construction board/Cell 5,4")
                .GetComponent<Button>();
            var secondPlot = canvas.Find("7 by 7 construction board/Cell 3,4")
                .GetComponent<Button>();
            var confirmation = canvas.Find("Building choices/Placement confirmation");
            var cancel = confirmation.Find("Cancel placement").GetComponent<Button>();
            var confirm = confirmation.Find("Confirm placement").GetComponent<Button>();

            firstPlot.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(1),
                "Selecting a plot must not mutate the board.");
            Assert.That(confirmation.gameObject.activeSelf, Is.True);
            Assert.That(confirmation.Find("Confirmation notice")
                .GetComponent<Text>().text, Does.Contain("Day-end meals"));
            Assert.That(confirmation.Find("Confirmation notice")
                .GetComponent<Text>().text, Does.Contain("Uses today's build"));
            Assert.That(firstPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.True);
            secondPlot.onClick.Invoke();
            Assert.That(firstPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.False, "A new plot replaces the staged placement.");
            Assert.That(secondPlot.transform.Find("Placement preview").gameObject.activeSelf,
                Is.True);
            cancel.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(confirmation.gameObject.activeSelf, Is.False);

            firstPlot.onClick.Invoke();
            confirm.onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.ConfirmPendingPlacement(), Is.False,
                "A second confirmation must never build twice.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingBuildingCancelsStagedPlacement()
        {
            var root = new GameObject("Construction cancelled selection test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);
            var canvas = root.transform.Find("Construction UI");
            canvas.Find("7 by 7 construction board/Cell 5,4")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(controller.HasPendingPlacement, Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.HasPendingPlacement, Is.False);
            Assert.That(controller.ConfirmPendingPlacement(), Is.False);
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardCaseOpensIntoTwoInformationLeaves()
        {
            var root = new GameObject("Board-case opening test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            var canvas = root.transform.Find("Construction UI");
            var left = canvas.Find("Observation frame").GetComponent<RectTransform>();
            var right = canvas.Find("Action frame").GetComponent<RectTransform>();
            var sharedInformation = canvas.Find("Building choices")
                .GetComponent<CanvasGroup>();
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(controller.IsBoardCaseOpen, Is.False);
                Assert.That(left.localScale.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(right.localScale.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(canvas.Find("Left board-case cover").gameObject.activeSelf,
                    Is.True);
                Assert.That(root.GetComponent<ConstructionWorldView>()
                    .BoardFoundation.Find("Folding board-case hardware"),
                    Is.Not.Null, "The original 3D board gains a physical tray and hinges.");
                yield return new WaitForSecondsRealtime(1.4f);
                Assert.That(controller.IsBoardCaseOpen, Is.True);
                Assert.That(left.localScale.x, Is.EqualTo(1f).Within(0.01f));
                Assert.That(right.localScale.x, Is.EqualTo(1f).Within(0.01f));
                Assert.That(sharedInformation.alpha, Is.EqualTo(1f).Within(0.01f));
                Assert.That(canvas.Find("Board-case opening input guard")
                    .gameObject.activeSelf, Is.False);
                Assert.That(canvas.Find("Left board-case cover")
                    .gameObject.activeSelf, Is.False);
            }
            else
                Assert.That(controller.IsBoardCaseOpen, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessRestartPersistsWithoutReplacingFillProgress()
        {
            var prefix = Path.Combine(Application.temporaryCachePath,
                "construction-modes-" + Guid.NewGuid().ToString("N"));
            var fillStore = new ConstructionRunStore(prefix + "-fill.json");
            var endlessStore = new ConstructionRunStore(prefix + "-endless.json");
            var fillRun = new ConstructionRunModel();
            Assert.That(fillRun.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(fillRun.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(fillStore.TrySave(fillRun), Is.True);
            var root = new GameObject("Endless restart save separation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(
                new ConstructionRunModel(ConstructionGameMode.Endless), endlessStore);
            yield return null;
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(endlessStore.TryLoad(out var existing), Is.True);
            Assert.That(existing.BuiltCount, Is.EqualTo(2));
            var reset = root.transform.Find(
                "Construction UI/Start a new construction run").GetComponent<Button>();
            reset.onClick.Invoke();
            reset.onClick.Invoke();
            Assert.That(endlessStore.TryLoad(out var fresh), Is.True);
            Assert.That(fresh.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(fresh.BuiltCount, Is.EqualTo(1));
            Assert.That(fillStore.TryLoad(out var fillAfterReset), Is.True);
            Assert.That(fillAfterReset.BuiltCount, Is.EqualTo(2));
            Object.Destroy(root);
            Assert.That(fillStore.TryDelete(), Is.True);
            Assert.That(endlessStore.TryDelete(), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessViewCanPanToTheNewRingAndBuildAfterDay49()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            foreach (var point in Enumerable.Range(0, 7)
                         .SelectMany(column => Enumerable.Range(0, 7)
                             .Select(row => (column, row)))
                         .OrderBy(point => Mathf.Abs(point.column - 3) +
                             Mathf.Abs(point.row - 3)))
            {
                if (point == (3, 3)) continue;
                Assert.That(run.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            }
            var root = new GameObject("Expanded endless construction test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;
            Assert.That(controller.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(controller.CurrentDay, Is.EqualTo(50));
            Assert.That(root.transform.Find("Construction UI/Title")
                .GetComponent<Text>().text, Does.Contain("ENDLESS BUILD"));
            Assert.That(root.transform.Find("Construction UI/Pan board west")
                .gameObject.activeSelf, Is.True);
            Assert.That(controller.Pan(-1, 0), Is.True);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var boardBase = root.transform.Find(
                    "Construction low-poly world/Board base");
                Assert.That(boardBase, Is.Not.Null);
                Assert.That(boardBase.localPosition.x,
                    Is.EqualTo(-WorldScaleStandards.CellSizeMeters).Within(0.01f),
                    "The navy board backing must follow the visible endless ring.");
            }
            Assert.That(controller.TryPlace(-1, 3), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(50));
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.CurrentDay, Is.EqualTo(51));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChineseConstructionUsesSharedPreferenceAndLegacyVisuals()
        {
            const string languageKey = "symbiosis49.language";
            var previousPreference = PlayerPrefs.GetInt(languageKey, -1);
            var root = new GameObject("Chinese construction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;

            var canvas = root.transform.Find("Construction UI");
            var story = canvas.Find("Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            var category = canvas.Find("Building choices/Build Green/Label")
                .GetComponent<Text>();
            var languageSelector = canvas.Find("Construction language");
            var englishButton = languageSelector.Find("English").GetComponent<Button>();
            var chineseButton = languageSelector.Find("Chinese").GetComponent<Button>();
            var englishLabel = englishButton.GetComponentInChildren<Text>();
            var chineseLabel = chineseButton.GetComponentInChildren<Text>();
            Assert.That(englishLabel.text, Is.EqualTo("EN"));
            Assert.That(chineseLabel.text, Is.EqualTo("中文"));
            Assert.That(englishLabel.font.name, Does.Contain("Nunito"));
            Assert.That(chineseLabel.font.name, Does.Contain("NotoSansSC"));
            Assert.That(chineseButton.targetGraphic.color.a,
                Is.GreaterThan(englishButton.targetGraphic.color.a),
                "The selected language must be visibly indicated.");
            var boardRect = canvas.Find("7 by 7 construction board")
                .GetComponent<RectTransform>();
            Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(boardRect.rect.width, Is.GreaterThanOrEqualTo(740f));
            var choices = canvas.Find("Building choices").GetComponent<Image>();
            Assert.That(choices.color.a, Is.EqualTo(0f).Within(0.001f),
                "Construction choices must not form a permanent right sidebar.");
            var palette = canvas.Find("Building choices/Build palette backing");
            Assert.That(palette.gameObject.activeSelf, Is.False);
            var buildToggle = canvas.Find("Building choices/Open build palette")
                .GetComponent<Button>();
            Assert.That(buildToggle.gameObject.activeSelf, Is.False,
                "The first day asks the player to observe the squirrel, not build.");
            var openingGuide = canvas.Find(
                "Building choices/Next step backing/Placement guide")
                .GetComponent<Text>();
            Assert.That(openingGuide.text, Is.EqualTo("点击中央橡树。"));
            Assert.That(openingGuide.preferredHeight,
                Is.LessThanOrEqualTo(openingGuide.rectTransform.rect.height + 1f));
            Assert.That(story.text, Does.Contain("松鼠"));
            Assert.That(story.fontSize, Is.GreaterThanOrEqualTo(17));
            Assert.That(story.rectTransform.rect.width, Is.LessThanOrEqualTo(250f),
                "Text must remain inside the opaque part of the left paper leaf.");
            Assert.That(category.text, Is.EqualTo("绿地"));
            Assert.That(category.font.name, Does.Contain("NotoSansSC"));
            Assert.That(canvas.Find("Paper background").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(canvas.Find("Building choices").GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(canvas.Find("Building choices/Build Green")
                .GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(canvas.Find(
                "7 by 7 construction board/Live low-poly board"), Is.Null,
                "The board must not be a RenderTexture inside a UI image.");
            Assert.That(canvas.Find(
                "7 by 7 construction board/3D board alignment"), Is.Not.Null);
            var worldView = root.GetComponent<ConstructionWorldView>();
            Assert.That(worldView, Is.Not.Null);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(Camera.main.targetTexture, Is.Null,
                    "The scene camera renders the shared-style board directly.");
                var world = root.transform.Find("Construction low-poly world");
                Assert.That(world, Is.Not.Null);
                Assert.That(world.Find("Squirrel"), Is.Not.Null);
                Assert.That(world.Find("Board base").gameObject.layer, Is.EqualTo(29),
                    "The board base must be visible to the construction camera.");
                Assert.That(world.Find("Board base/Empty plot 1,1").gameObject.layer,
                    Is.EqualTo(29),
                    "Unbuilt plots must be visible in the 3D board render.");
                Assert.That(world.Find("Board base/Empty plot 1,1")
                    .localScale.x, Is.LessThan(WorldScaleStandards.CellSizeMeters),
                    "Vacant cells should expose the original navy board backing.");
                Assert.That(world.Find(
                    "Board base/Empty plot 1,1/Pastel plot face"), Is.Not.Null,
                    "Vacant plots should use the lighter face while keeping the navy frame.");
                Assert.That(world.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == "Low-poly Mature Tree"), Is.True,
                    "The construction oak must reuse the original faceted tree kit.");
                Assert.That(root.GetComponentInChildren<UrbanWildlifeRooms.Animals.SquirrelDemoVisual>(),
                    Is.Not.Null);
            }
            Assert.That(story.preferredHeight,
                Is.LessThanOrEqualTo(story.rectTransform.rect.height + 1f));

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(buildToggle.gameObject.activeSelf, Is.True);
            buildToggle.onClick.Invoke();
            Assert.That(palette.gameObject.activeSelf, Is.True);
            buildToggle.onClick.Invoke();
            Assert.That(palette.gameObject.activeSelf, Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(canvas.Find("Building choices/Green planting/Plant Meadow/Label")
                .GetComponent<Text>().text, Is.EqualTo("草地"));
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(story.text, Does.Contain("鸽群"));
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(story.text, Does.Contain("住宅"));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(canvas.Find("Route inspection/Route status")
                .GetComponent<Text>().text, Does.Contain("明日"));

            englishButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.English));
            Assert.That(category.text, Is.EqualTo("Green"));
            Assert.That(category.font.name, Does.Contain("Nunito"));
            Assert.That(story.text, Does.Contain("residence"));
            Assert.That(englishButton.targetGraphic.color.a,
                Is.GreaterThan(chineseButton.targetGraphic.color.a));
            englishButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.English),
                "Choosing the active language must not toggle to the other one.");
            chineseButton.onClick.Invoke();
            Assert.That(controller.Language, Is.EqualTo(InterfaceLanguage.Chinese));
            Assert.That(category.font.name, Does.Contain("NotoSansSC"));
            Assert.That(PlayerPrefs.GetInt(languageKey, -1), Is.EqualTo(previousPreference),
                "A PlayMode test must not change the player's language preference.");
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatureNeighbourOaksShowGrowingSquirrelCountAndMeals()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var root = new GameObject("Squirrel growth UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            var starter = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            Assert.That(starter.transform.Find(
                "Resident squirrel/Animal group count/Count")
                .GetComponent<Text>().text, Is.EqualTo("2"));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            var routeStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status").GetComponent<Text>();
            Assert.That(routeStatus.text, Does.Contain("2/2"));
            Assert.That(routeStatus.text, Does.Contain("3/5 mature nearby oaks"));
            var outcomes = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Observed outcomes")
                .GetComponent<Text>();
            Assert.That(outcomes.text, Does.Contain("Next: Squirrel 2/2"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullBoardShowsResultAndCanStartNewRun()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Mathf.Abs(position.column - 3) +
                    Mathf.Abs(position.row - 3));
            foreach (var position in positions)
            {
                Assert.That(run.TryBuild(position.column, position.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out var failure, out _), Is.True, failure.ToString());
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var root = new GameObject("Construction result test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            var result = root.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(result.gameObject.activeSelf, Is.True);
            var numbers = result.Find("Result card/Result numbers")
                .GetComponent<Text>().text;
            Assert.That(numbers, Does.Contain("Animal score"));
            Assert.That(numbers, Does.Contain("Human score"));
            Assert.That(numbers, Does.Contain("Completed in 49 days"));
            var reset = result.GetComponentInChildren<Button>();
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(result.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MixedStoryReachesFinalResultAndNewRunSurvivesReload()
        {
            var savePath = Path.Combine(Application.temporaryCachePath,
                "construction-flow-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(savePath);
            var root = new GameObject("Mixed construction flow test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel(), store);
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            PlaceAndAdvance(controller, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(controller.TryPlace(4, 3), Is.False,
                "Inspecting the arrived pigeons unlocks the resident beat.");
            PlaceAndAdvance(controller, 3, 2, ConstructionCategory.Residence);
            PlaceAndAdvance(controller, 3, 1, ConstructionCategory.Restaurant);
            Assert.That(store.TryLoad(out var saved), Is.True);
            Assert.That(saved.BuiltCount, Is.EqualTo(4));
            Assert.That(saved.CurrentDay, Is.EqualTo(5));

            // A newly opened screen must continue the saved run, not its starter state.
            Object.Destroy(root);
            yield return null;
            root = new GameObject("Reopened mixed construction flow test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(saved, store);
            yield return null;
            Assert.That(controller.BuiltCount, Is.EqualTo(4));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));

            PlaceAndAdvance(controller, 2, 2, ConstructionCategory.Workshop);
            PlaceAndAdvance(controller, 2, 1, ConstructionCategory.Waste);
            PlaceAndAdvance(controller, 5, 3, ConstructionCategory.Residence);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var world = root.transform.Find("Construction low-poly world");
                Assert.That(store.TryLoad(out var afterSecondHome), Is.True);
                var secondHome = afterSecondHome.BuiltTiles.Single(tile =>
                    tile.category == ConstructionCategory.Residence &&
                    tile.id != afterSecondHome.FirstResidentHomeTileId);
                var firstActor = world.Find("First resident");
                var secondActor = world.Find($"Resident {secondHome.id}");
                Assert.That(firstActor, Is.Not.Null);
                Assert.That(secondActor, Is.Not.Null,
                    "Every occupied residence needs its own 3D resident.");
                Assert.That(secondActor.gameObject.activeSelf, Is.True);
                Assert.That(world.GetComponentsInChildren<UrbanWildlifeRooms.People.CitizenDemoVisual>()
                    .Length, Is.EqualTo(2));
                Assert.That(secondActor.localPosition.x,
                    Is.GreaterThan(WorldScaleStandards.CellSizeMeters),
                    "The second resident should appear at the second home, not the first.");
                var firstHome = afterSecondHome.BuiltTiles.Single(tile =>
                    tile.id == afterSecondHome.FirstResidentHomeTileId);
                var service = afterSecondHome.BuiltTiles.First(tile =>
                    tile.category == ConstructionCategory.Workshop);
                var view = root.GetComponent<ConstructionWorldView>();
                view.PlayResidentCommutes(
                    Array.Empty<ConstructionResidentDayResult>(),
                    afterSecondHome.BuiltTiles);
                var firstStart = firstActor.localPosition;
                var secondStart = secondActor.localPosition;
                view.PlayResidentCommutes(new[]
                {
                    new ConstructionResidentDayResult
                    {
                        homeTileId = firstHome.id,
                        worked = true,
                        routeTileIds = new System.Collections.Generic.List<string>
                            { firstHome.id, service.id, firstHome.id }
                    },
                    new ConstructionResidentDayResult
                    {
                        homeTileId = secondHome.id,
                        worked = true,
                        routeTileIds = new System.Collections.Generic.List<string>
                            { secondHome.id, service.id, secondHome.id }
                    }
                }, afterSecondHome.BuiltTiles);
                var firstImmediate = firstActor.localPosition;
                var secondImmediate = secondActor.localPosition;
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(Mathf.Max(
                    Vector3.Distance(firstStart, firstImmediate),
                    Vector3.Distance(firstStart, firstActor.localPosition)),
                    Is.GreaterThan(0.1f));
                Assert.That(Mathf.Max(
                    Vector3.Distance(secondStart, secondImmediate),
                    Vector3.Distance(secondStart, secondActor.localPosition)),
                    Is.GreaterThan(0.1f),
                    "Both resident visuals should move on supplied routes.");
                view.PlayResidentCommutes(
                    Array.Empty<ConstructionResidentDayResult>(),
                    afterSecondHome.BuiltTiles);
                Assert.That(Vector3.Distance(firstStart, firstActor.localPosition),
                    Is.LessThan(0.1f));
                Assert.That(Vector3.Distance(secondStart, secondActor.localPosition),
                    Is.LessThan(0.1f),
                    "Stopping simultaneous commutes must restore both homes.");
            }
            PlaceAndAdvance(controller, 4, 2, ConstructionCategory.Street);
            PlaceAndAdvance(controller, 5, 2, ConstructionCategory.Street);
            PlaceAndAdvance(controller, 4, 1, ConstructionCategory.Street);
            var story = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.AwaitRestaurantCrowding));
            Assert.That(story.text, Does.Contain("Waste blocks a home"),
                "A connected route alone cannot reveal crowding while waste blocks a home.");
            PlaceAndAdvance(controller, 6, 3, ConstructionCategory.Waste);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildSupermarket));
            Assert.That(story.text, Does.Contain("two daily meals"));
            PlaceAndAdvance(controller, 5, 1, ConstructionCategory.Supermarket);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.FreeBuild));

            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(position => Mathf.Abs(position.column - 3) +
                    Mathf.Abs(position.row - 3));
            var occupied = new[]
            {
                (3, 3), (4, 3), (3, 2), (3, 1), (2, 2), (2, 1),
                (5, 3), (6, 3), (4, 2), (5, 2), (4, 1), (5, 1)
            };
            foreach (var position in positions)
            {
                if (controller.BuiltCount == 49) break;
                if (occupied.Contains((position.column, position.row))) continue;
                PlaceAndAdvance(controller, position.column, position.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow);
            }
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            Assert.That(controller.CurrentDay, Is.EqualTo(49));
            Assert.That(controller.ObservedAnimalMeals, Is.GreaterThan(0));
            Assert.That(controller.ObservedHumanWorkCycles, Is.GreaterThan(0));
            var result = root.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(result.gameObject.activeSelf, Is.True);
            var numbers = result.Find("Result card/Result numbers")
                .GetComponent<Text>().text;
            Assert.That(numbers, Does.Contain("Animal score"));
            Assert.That(numbers, Does.Contain("Human score"));
            Assert.That(numbers, Does.Contain("Completed in 49 days"));
            Assert.That(store.TryLoad(out var finished), Is.True);
            Assert.That(finished.IsFinished, Is.True);
            Assert.That(finished.BuiltCount, Is.EqualTo(49));

            var reset = result.GetComponentInChildren<Button>();
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(result.gameObject.activeSelf, Is.False);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.That(root.transform.Find("Construction low-poly world")
                    .GetComponentsInChildren<UrbanWildlifeRooms.People.CitizenDemoVisual>()
                    .Length, Is.Zero,
                    "Restart must remove both residents from the old run.");
            Assert.That(store.TryLoad(out var fresh), Is.True);
            Assert.That(fresh.BuiltCount, Is.EqualTo(1));
            Assert.That(fresh.CurrentDay, Is.EqualTo(1));
            Assert.That(fresh.IsFinished, Is.False);

            Object.Destroy(root);
            yield return null;
            root = new GameObject("Reopened after reset test");
            controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(fresh, store);
            yield return null;
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Object.Destroy(root);
            Assert.That(store.TryDelete(), Is.True);
            yield return null;
        }

        private static void PlaceAndAdvance(ConstructionPrototypeController controller,
            int column, int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(controller.TrySelect(category, planting), Is.True,
                $"{category} is locked before placing {column + 1},{row + 1}.");
            Assert.That(controller.TryPlace(column, row), Is.True,
                $"Could not place {category} at {column + 1},{row + 1}.");
            if (controller.BuiltCount < 49)
                Assert.That(controller.AdvanceDay(), Is.True,
                    $"Could not settle day after {column + 1},{row + 1}.");
            // Placing the last tile settles day 49 automatically.
            AssertStoryPresentationFits(controller);
        }

        private static void AssertStoryPresentationFits(
            ConstructionPrototypeController controller)
        {
            var backing = controller.transform.Find(
                "Construction UI/Building choices/Story cue backing");
            foreach (var name in new[] { "Story instruction", "Story preview" })
            {
                var label = backing.Find(name).GetComponent<Text>();
                Assert.That(label.preferredHeight,
                    Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    $"{controller.Language} {name} is clipped: {label.text}");
            }
            if (!controller.IsBoardCaseOpen || controller.HasPendingPlacement) return;
            var icon = backing.Find("Story pictogram").GetComponent<EventTrigger>();
            var pointer = new PointerEventData(EventSystem.current)
                { position = new Vector2(280f, 450f) };
            icon.OnPointerEnter(pointer);
            var tooltip = controller.transform.Find("Construction UI/Context tooltip");
            var details = tooltip.Find("Tooltip details").GetComponent<Text>();
            Assert.That(details.preferredHeight,
                Is.LessThanOrEqualTo(details.rectTransform.rect.height + 1f),
                $"{controller.Language} tutorial tooltip is clipped: {details.text}");
            icon.OnPointerExit(pointer);
        }

        [UnityTest]
        public IEnumerator FillDayCannotBeSkippedAfterTheStarterOak()
        {
            var root = new GameObject("Daily fill rule test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True,
                "The day-one oak and squirrel observation is scripted.");
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(controller.AdvanceDay(), Is.False);
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback").GetComponent<Text>().text,
                Does.Contain("Build one room"));
            var advance = root.transform.Find(
                "Construction UI/Building choices/Advance to next day")
                .GetComponent<Button>();
            Assert.That(advance.interactable, Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(advance.interactable, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildableMarkersFollowSelectedRulesAndDailyLimit()
        {
            var root = new GameObject("Buildable markers test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(BuildableAt(root, 4, 3), Is.True,
                "An orthogonal green cell should be suggested.");
            Assert.That(BuildableAt(root, 4, 2), Is.False,
                "A diagonal cell should not be suggested.");
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(BuildableAt(root, 3, 3), Is.False,
                "No second build may be suggested on the same day.");

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(BuildableAt(root, 3, 2), Is.True);
            Assert.That(BuildableAt(root, 6, 6), Is.False);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(BuildableAt(root, 3, 1), Is.True,
                "A restaurant may touch the residence.");
            Assert.That(BuildableAt(root, 5, 3), Is.False,
                "A restaurant may not attach only to meadow.");
            Assert.That(root.transform.Find("Construction UI/Building choices/Next step backing/Placement guide")
                .GetComponent<Text>().text, Does.Contain("Selected: Restaurant"));
            Object.Destroy(root);
            yield return null;
        }

        private static bool BuildableAt(GameObject root, int column, int row) =>
            root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == $"Cell {column + 1},{row + 1}")
                .transform.Find("Buildable hint").gameObject.activeSelf;

        [UnityTest]
        public IEnumerator PigeonInspectionPersistsAndResidenceRemainsAvailableOnReload()
        {
            var savePath = Path.Combine(Application.temporaryCachePath,
                "construction-pigeon-inspection-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new ConstructionRunStore(savePath);
            var root = new GameObject("Pigeon inspection persistence test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(new ConstructionRunModel(), store);
                yield return null;
                Assert.That(controller.AdvanceDay(), Is.True);
                Assert.That(controller.TrySelect(ConstructionCategory.Green,
                    GreenPlanting.Meadow), Is.True);
                Assert.That(controller.TryPlace(4, 3), Is.True);
                Assert.That(controller.AdvanceDay(), Is.True);
                Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True,
                    "Pigeon inspection is narrative context, not a building lock.");
                Assert.That(controller.TryPlace(4, 3), Is.False);
                Assert.That(store.TryLoad(out var saved), Is.True);
                Assert.That(saved.PigeonMeadowInspected, Is.True);

                Object.Destroy(root);
                yield return null;
                root = new GameObject("Reloaded pigeon inspection test");
                controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(saved, store);
                yield return null;
                Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
                Assert.That(controller.TryPlace(3, 2), Is.True);
                Assert.That(controller.Stage, Is.EqualTo(
                    ConstructionStoryStage.BuildRestaurant));
            }
            finally
            {
                Object.Destroy(root);
                if (File.Exists(savePath)) File.Delete(savePath);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonArrivalIntroducesOneResidentWithVisibleFoodNeed()
        {
            var root = new GameObject("Construction resident test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            var visitor = root.transform.Find(
                "Construction UI/Building choices/Waiting visitor");
            Assert.That(visitor, Is.Not.Null);
            Assert.That(visitor.gameObject.activeSelf, Is.False,
                "The resident beat waits until the flock has been inspected.");
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True,
                "The player can choose a home before inspecting the flock.");
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(visitor.gameObject.activeSelf, Is.True);
            var pigeonFeedback = root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>();
            Assert.That(pigeonFeedback.text, Does.Contain("grass seeds"));
            Assert.That(pigeonFeedback.preferredHeight,
                Is.LessThanOrEqualTo(pigeonFeedback.rectTransform.rect.height + 1f));

            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Assert.That(visitor.gameObject.activeSelf, Is.False);
            var home = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,3");
            Assert.That(home.transform.Find("First resident").gameObject.activeSelf,
                Is.True);
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.True);
            var arrivalFeedback = root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>();
            Assert.That(arrivalFeedback.text, Does.Contain("1 meal/day"));
            Assert.That(arrivalFeedback.preferredHeight,
                Is.LessThanOrEqualTo(arrivalFeedback.rectTransform.rect.height + 1f));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(residentActor, Is.Not.Null);
                Assert.That(residentActor.localPosition.x, Is.LessThan(0.1f),
                    "The resident should walk into the new home, not appear at its centre.");
                yield return new WaitForSecondsRealtime(1f);
                Assert.That(residentActor.localPosition.x, Is.EqualTo(0.8f)
                    .Within(0.05f));
            }
            Assert.That(controller.TryPlace(3, 2), Is.False);
            var firstRouteStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status")
                .GetComponent<Text>();
            Assert.That(firstRouteStatus.text, Does.Contain("No meal route"));
            Assert.That(firstRouteStatus.preferredHeight,
                Is.LessThanOrEqualTo(firstRouteStatus.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(firstRouteStatus.text, Does.Contain("无用餐路线"));
            Assert.That(firstRouteStatus.preferredHeight,
                Is.LessThanOrEqualTo(firstRouteStatus.rectTransform.rect.height + 1f));
            controller.ToggleLanguage();
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.True,
                "Workshop choice is available before the story recommends it.");

            Assert.That(controller.AdvanceDay(), Is.True,
                "A residence uses today's one construction slot.");
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(controller.TryPlace(3, 1), Is.True);
            var restaurant = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,2");
            var seats = restaurant.transform.Find("Daily capacity/Capacity count")
                .GetComponent<Text>();
            Assert.That(seats.text, Is.EqualTo("~1/1"));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Assert.That(home.transform.Find("Waste backlog").gameObject.activeSelf,
                Is.True);
            Assert.That(home.transform.Find("Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("2"),
                "The residence produced waste on its own construction day too.");
            Assert.That(restaurant.transform.Find("Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("1"));
            var walking = root.transform.Find(
                "Construction UI/7 by 7 construction board/Resident walking");
            Assert.That(walking, Is.Not.Null);
            Assert.That(walking.gameObject.activeSelf, Is.True);
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.False);
            Assert.That(home.transform.Find("Work access needed").gameObject.activeSelf,
                Is.True);
            Assert.That(seats.text, Is.EqualTo("~1/1"));
            Assert.That(controller.ObservedHumanWorkCycles, Is.Zero);
            var residentHomePosition = Vector3.zero;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(residentActor, Is.Not.Null);
                residentHomePosition = residentActor.localPosition;
                yield return new WaitForSecondsRealtime(0.16f);
                Assert.That(residentActor.localPosition.z,
                    Is.GreaterThan(residentHomePosition.z + 0.2f),
                    "The first meal must move the 3D resident toward the restaurant.");
            }
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(walking.gameObject.activeSelf, Is.False);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var residentActor = root.transform.Find(
                    "Construction low-poly world/First resident");
                Assert.That(Vector3.Distance(residentActor.localPosition,
                    residentHomePosition), Is.LessThan(0.05f),
                    "The resident should return home after the meal.");
            }
            var resident = home.transform.Find("First resident")
                .GetComponent<RectTransform>();
            Assert.That(resident.gameObject.activeSelf, Is.True);
            Assert.That(resident.anchoredPosition.x, Is.EqualTo(23f).Within(0.1f));
            Assert.That(resident.anchoredPosition.y, Is.EqualTo(-22f).Within(0.1f));
            Assert.That(controller.TryPlace(3, 2), Is.False,
                "Selecting an occupied residence inspects rather than rebuilds it.");
            var routeStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status").GetComponent<Text>();
            Assert.That(routeStatus.text, Does.Contain("no work route"));
            var overlay = root.transform.Find(
                "Construction UI/7 by 7 construction board/Selected route overlay");
            Assert.That(overlay.GetComponentsInChildren<Image>()
                .Count(image => image.name == "Route segment"), Is.GreaterThan(0));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(routeStatus.text, Does.Contain("SQUIRREL"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CentralOakDayAndGreenPlacementWorkInActualUi()
        {
            var root = new GameObject("Construction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Assert.That(root.GetComponentsInChildren<Button>(true)
                .Count(button => button.name.StartsWith("Cell ")), Is.EqualTo(49));
            Assert.That(root.GetComponentsInChildren<Text>(true)
                .All(label => label.font != null), Is.True);
            var starter = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            Assert.That(starter.transform.Find(
                "Resident squirrel/Animal group count/Count")
                .GetComponent<Text>().text, Is.EqualTo("1"));
            var oakIcon = starter.transform.Find("Room icon").GetComponent<Image>();
            Assert.That(oakIcon.sprite, Is.Not.Null);
            var initialOakSize = oakIcon.rectTransform.sizeDelta.x;

            var openingStory = root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>();
            Assert.That(openingStory.text, Does.Contain("Click that room"));
            Assert.That(controller.TryPlace(3, 3), Is.False,
                "Inspecting the starter oak must not place a second room.");
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(openingStory.text, Does.Contain("End Day 1"));
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("1 nut in stock"));

            Assert.That(controller.AdvanceDay(), Is.True);
            var review = root.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(review.text, Does.Contain("Meals 1/1"));
            Assert.That(oakIcon.rectTransform.sizeDelta.x, Is.EqualTo(initialOakSize),
                "The established starter oak should already be mature.");
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TryPlace(2, 3), Is.False,
                "A second valid cell cannot be built on the same day.");
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback").GetComponent<Text>().text,
                Does.Contain("One room per day"));
            Assert.That(controller.TryPlace(6, 6), Is.False);

            var newGreen = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 5,4");
            var newGreenIcon = newGreen.transform.Find("Room icon")
                .GetComponent<Image>();
            var initialGreenSize = newGreenIcon.rectTransform.sizeDelta.x;
            Assert.That(newGreen.transform.Find("Food stock/Food count")
                .GetComponent<Text>().text, Is.EqualTo("0"));
            Assert.That(controller.AdvanceDay(), Is.True);
            var pigeonFlock = newGreen.transform.Find("Pigeon flock");
            Assert.That(pigeonFlock.gameObject.activeSelf, Is.True);
            Assert.That(pigeonFlock.Find("Animal group count/Count")
                .GetComponent<Text>().text, Is.EqualTo("2"));
            Assert.That(pigeonFlock.localScale.x, Is.LessThan(1f),
                "The arriving flock should pop into view instead of appearing abruptly.");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var threeDFlock = root.transform.Find(
                    "Construction low-poly world/Pigeon flock");
                Assert.That(threeDFlock, Is.Not.Null);
                Assert.That(threeDFlock.localPosition.y, Is.GreaterThan(1f),
                    "The 3D flock should descend onto the meadow.");
            }
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(newGreenIcon.rectTransform.sizeDelta.x,
                Is.GreaterThan(initialGreenSize),
                "A new green tile should visibly grow after the day changes.");
            Assert.That(controller.CurrentDay, Is.EqualTo(3));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(pigeonFlock.localScale.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(pigeonFlock.GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(controller.TryPlace(4, 3), Is.False);
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Story cue backing/Story instruction")
                .GetComponent<Text>().text, Does.Contain("visitor"));
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback backing/Feedback")
                .GetComponent<Text>().text, Does.Contain("grass seeds"));
            Assert.That(root.transform.Find(
                "Construction UI/Route inspection/Route status")
                .GetComponent<Text>().text,
                Does.Contain("0/3 connected mature meadows"));
            Assert.That(controller.ObservedAnimalMeals, Is.EqualTo(2),
                "Each of the first two days includes one actual oak meal; the " +
                "new flock has not eaten until its first full day.");

            var reset = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Start a new construction run");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2),
                "Any intervening action should cancel reset confirmation.");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(review.text, Is.Empty,
                "A fresh run has no previous day to review.");
            Assert.That(review.transform.parent.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }
    }
}
