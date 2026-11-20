using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionSceneNavigationTests
    {
        [UnityTest]
        public IEnumerator ConstructionOpeningBoardPlotsDoNotReceiveCaseShadows()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var foundation = bootstrap.transform.Find(
                UrbanWildlifeBootstrap.GeneratedRootName +
                "/7x7 Modular Floor/Fixed Board Foundation");
            Assert.That(foundation, Is.Not.Null);
            var preview = bootstrap.transform.Find(
                    UrbanWildlifeBootstrap.GeneratedRootName)
                .GetComponent<ConstructionOpeningBoardPreview>();
            Assert.That(preview, Is.Not.Null);
            for (var frame = 0; frame < 120 && !preview.IsPreviewing; frame++)
                yield return null;
            Assert.That(preview.IsPreviewing, Is.True);
            var emptyFace = foundation.Find(
                "49 Fixed Cell Slots/Foundation Slot 1-1/Pastel plot face");
            Assert.That(emptyFace, Is.Not.Null);
            var renderer = emptyFace.GetComponent<Renderer>();
            Assert.That(renderer.sharedMaterial.HasProperty("_ShadowStrength"),
                Is.True);
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_ShadowStrength"), Is.Zero);
            preview.RestoreFullBoard();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_ShadowStrength"), Is.EqualTo(1f),
                "Returning to the legacy board must restore its material settings.");
        }

        [UnityTest]
        public IEnumerator ConstructionLanguageChoiceUpdatesTheSurvivingMainMenu()
        {
            const string languageKey = "symbiosis49.language";
            var hadPreference = PlayerPrefs.HasKey(languageKey);
            var previousPreference = PlayerPrefs.GetInt(languageKey, -1);
            var savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"symbiosis-language-return-{System.Guid.NewGuid():N}.json");
            try
            {
                PlayerPrefs.SetInt(languageKey, (int)InterfaceLanguage.Chinese);
                PlayerPrefs.Save();
                yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);

                var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
                Assert.That(bootstrap, Is.Not.Null);
                var generated = bootstrap.transform.Find(
                    UrbanWildlifeBootstrap.GeneratedRootName);
                Assert.That(generated, Is.Not.Null);
                var persistence = generated.GetComponentInChildren<
                    SessionPersistenceController>(true);
                if (persistence != null) Object.Destroy(persistence);
                var mainRuntime = generated.GetComponentInChildren<
                    GameRuntimeController>(true);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
                Assert.That(mainRuntime.Language, Is.EqualTo(InterfaceLanguage.Chinese));
                var menu = hud.transform.Find("Main Menu");
                Transform entrance = null;
                for (var frame = 0; frame < 120 && entrance == null; frame++)
                {
                    entrance = menu.Find("Construction Mode Entrance");
                    if (entrance == null) yield return null;
                }
                Assert.That(entrance, Is.Not.Null);
                entrance.GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSecondsRealtime(2.2f);
                for (var frame = 0; frame < 300 &&
                     SceneManager.GetActiveScene().name != "ConstructionPrototype";
                     frame++)
                    yield return null;
                Assert.That(SceneManager.GetActiveScene().name,
                    Is.EqualTo("ConstructionPrototype"));

                var construction = Object.FindFirstObjectByType<
                    ConstructionPrototypeController>();
                Assert.That(construction, Is.Not.Null);
                Assert.That(construction.Language,
                    Is.EqualTo(InterfaceLanguage.Chinese));
                var english = construction.transform.Find(
                    "Construction UI/Construction language/English")
                    .GetComponent<Button>();
                english.onClick.Invoke();
                Assert.That(construction.Language,
                    Is.EqualTo(InterfaceLanguage.English));
                Assert.That(PlayerPrefs.GetInt(languageKey, -1),
                    Is.EqualTo((int)InterfaceLanguage.English));
                Assert.That(mainRuntime.Language,
                    Is.EqualTo(InterfaceLanguage.Chinese),
                    "The Main runtime survives additively while its HUD is hidden.");

                var testRun = new ConstructionRunModel();
                var testStore = new ConstructionRunStore(savePath);
                construction.UseRunForTesting(testRun, testStore);
                Assert.That(construction.ReturnToMainMenu(), Is.True);
                yield return new WaitForSecondsRealtime(2.2f);
                for (var frame = 0; frame < 300 &&
                     SceneManager.GetSceneByName("ConstructionPrototype").isLoaded;
                     frame++)
                    yield return null;
                yield return null; // Let the menu caption refresh in LateUpdate.

                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
                Assert.That(SceneManager.GetSceneByName("ConstructionPrototype").isLoaded,
                    Is.False);
                Assert.That(mainRuntime.Language,
                    Is.EqualTo(InterfaceLanguage.English),
                    "Returning to the persistent Main scene must use the selected language.");
                var modeLabel = entrance.Find("Construction Mode Label")
                    .GetComponent<Text>();
                Assert.That(modeLabel.text, Is.EqualTo("Fill Mode"));
                Assert.That(testStore.TryLoad(out var restored), Is.True,
                    "The language sync must not discard the construction run.");
                Assert.That(restored.CurrentDay, Is.EqualTo(testRun.CurrentDay));
                Assert.That(restored.BuiltCount, Is.EqualTo(testRun.BuiltCount));
            }
            finally
            {
                if (hadPreference)
                    PlayerPrefs.SetInt(languageKey, previousPreference);
                else
                    PlayerPrefs.DeleteKey(languageKey);
                PlayerPrefs.Save();
                if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
            }
        }

        [UnityTest]
        public IEnumerator EndlessMenuCardOpensAnUnboundedCameraIsolatedMode()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var hud = bootstrap.GetComponentInChildren<
                UrbanWildlifeRooms.UI.UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            var originalFoundation = bootstrap.transform.Find(
                UrbanWildlifeBootstrap.GeneratedRootName +
                "/7x7 Modular Floor/Fixed Board Foundation");
            Assert.That(originalFoundation, Is.Not.Null);
            var openingSlots = originalFoundation.Find("49 Fixed Cell Slots");
            Assert.That(openingSlots, Is.Not.Null);
            Assert.That(openingSlots.childCount, Is.EqualTo(49));
            var openingFrame = originalFoundation.Find("Automatic Boundary Cover Layer");
            Assert.That(openingFrame, Is.Not.Null);
            Assert.That(openingFrame.Find("Wooden case edge"), Is.Not.Null);
            Assert.That(openingFrame.Find("Brass inner rule"), Is.Not.Null,
                "Both modes should inherit the same layered physical board frame.");
            foreach (Transform slot in openingSlots)
                Assert.That(slot.Find("Pastel plot face"), Is.Not.Null,
                    "The menu board and construction board must share the same plot finish.");
            for (var frame = 0; frame < 120 &&
                 menu.Find("Construction Mode Entrance") == null; frame++)
                yield return null;
            var endlessCard = menu.Find("Sandbox Mode Entrance");
            Assert.That(endlessCard, Is.Not.Null);
            Assert.That(endlessCard.gameObject.activeInHierarchy, Is.True);
            var physicalCamera = Object.FindFirstObjectByType<
                PhysicalBoardCameraController>();
            endlessCard.GetComponent<Button>().onClick.Invoke();
            if (physicalCamera != null)
                Assert.That(physicalCamera.RecognitionSuspended, Is.True,
                    "Endless must stop recognition before entering gameplay.");
            for (var frame = 0; frame < 300 &&
                 SceneManager.GetActiveScene().name != "ConstructionPrototype"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("ConstructionPrototype"));
            var controller = Object.FindFirstObjectByType<ConstructionPrototypeController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Mode, Is.EqualTo(ConstructionGameMode.Endless));
            Assert.That(originalFoundation, Is.Not.Null,
                "Endless must retain the opening board object through the transition.");
            Assert.That(Object.FindFirstObjectByType<ConstructionOpeningBoardPreview>()
                .Foundation, Is.SameAs(originalFoundation));
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.That(controller.GetComponent<ConstructionWorldView>()
                    .BoardFoundation, Is.SameAs(originalFoundation));
                Assert.That(controller.transform.Find(
                    "Construction low-poly world/Unbounded moving plot surface"),
                    Is.Not.Null);
                Assert.That(openingFrame.GetComponentInChildren<Renderer>().enabled,
                    Is.False, "The finite frame must be hidden in endless gameplay.");
            }
            for (var frame = 0; frame < 200 && !controller.IsBoardCaseOpen; frame++)
                yield return null;
            var entryDeadline = Time.realtimeSinceStartup + 8f;
            while (GameObject.Find("Tabletop scene transition") != null &&
                   Time.realtimeSinceStartup < entryDeadline)
                yield return null;
            Assert.That(GameObject.Find("Tabletop scene transition"), Is.Null);
            var finished = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(finished.TrySimulateConstructionDay(out _), Is.False,
                "An empty endless turn must not advance the clock.");
            Assert.That(finished.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(finished.TrySimulateConstructionDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .OrderBy(point => System.Math.Abs(point.column - 3) +
                    System.Math.Abs(point.row - 3))
                .Where(point => point != (3, 3) && point != (4, 3));
            foreach (var point in positions)
            {
                if (finished.IsFinished) break;
                Assert.That(finished.TryBuild(point.column, point.row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, out _), Is.True);
                Assert.That(finished.TrySimulateConstructionDay(out _), Is.True);
            }
            Assert.That(finished.IsFinished, Is.True);
            var savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "symbiosis-finished-menu-" + System.Guid.NewGuid().ToString("N") + ".json");
            var testStore = new ConstructionRunStore(savePath);
            controller.UseRunForTesting(finished, testStore);
            controller.ToggleLanguage(); // Refresh without changing PlayerPrefs.
            var resultPanel = controller.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(resultPanel.gameObject.activeSelf, Is.True);
            yield return null;
            var resultMenu = resultPanel.Find(
                "Result card/Return to menu after result").GetComponent<Button>();
            Assert.That(resultMenu.interactable, Is.True);
            Assert.That(resultMenu.GetComponentInChildren<Text>().text,
                Is.EqualTo("主菜单"));
            var resultCanvas = controller.transform.Find("Construction UI")
                .GetComponent<Canvas>();
            Canvas.ForceUpdateCanvases();
            var resultPointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(
                    resultCanvas.worldCamera,
                    resultMenu.GetComponent<RectTransform>().TransformPoint(
                        resultMenu.GetComponent<RectTransform>().rect.center))
            };
            var resultHits = new List<RaycastResult>();
            resultCanvas.GetComponent<GraphicRaycaster>().Raycast(
                resultPointer, resultHits);
            Assert.That(resultHits.Count, Is.GreaterThan(0),
                "Result pointer " + resultPointer.position +
                "; screen " + Screen.width + "x" + Screen.height +
                "; button active " + resultMenu.gameObject.activeInHierarchy +
                "; canvas active " + resultCanvas.isActiveAndEnabled +
                "; image raycast " +
                resultMenu.GetComponent<Image>().raycastTarget +
                "; image hit " + resultMenu.GetComponent<Image>().Raycast(
                    resultPointer.position, resultCanvas.worldCamera) +
                "; graphic depth " + resultMenu.GetComponent<Image>().depth +
                "; ignore reversed " + resultCanvas.GetComponent<GraphicRaycaster>().ignoreReversedGraphics +
                "; ancestor groups " + string.Join(", ",
                    resultMenu.GetComponentsInParent<CanvasGroup>(true)
                        .Select(group => group.name + ":" + group.alpha +
                            "/" + group.blocksRaycasts)) +
                "; registered graphics " +
                GraphicRegistry.GetGraphicsForCanvas(resultCanvas).Count +
                "; raycaster enabled " + resultCanvas.GetComponent<GraphicRaycaster>().isActiveAndEnabled +
                "; blocking " + resultCanvas.GetComponent<GraphicRaycaster>().blockingObjects +
                "; button bounds " + ScreenBounds(
                    resultMenu.GetComponent<RectTransform>(), resultCanvas));
            Assert.That(resultHits[0].gameObject, Is.SameAs(resultMenu.gameObject),
                "The result menu button must receive the pointer. Hits: " +
                string.Join(", ", resultHits.ConvertAll(hit => hit.gameObject.name)));
            resultMenu.onClick.Invoke();
            for (var frame = 0; frame < 400 &&
                 (SceneManager.GetSceneByName("ConstructionPrototype").isLoaded ||
                  GameObject.Find("Tabletop scene transition") != null);
                 frame++)
                yield return null;
            Assert.That(SceneManager.GetSceneByName("ConstructionPrototype").isLoaded,
                Is.False);
            Assert.That(testStore.StatusFor(ConstructionGameMode.Endless),
                Is.EqualTo(ConstructionSavedRunStatus.Finished),
                "Returning from the result must preserve the finished save.");
            Assert.That(testStore.TryDelete(), Is.True);
            if (physicalCamera != null)
                Assert.That(physicalCamera.RecognitionSuspended, Is.False,
                    "Returning to Main must restore its camera capability.");
        }

        [UnityTest]
        public IEnumerator EndlessContinueCardRoutesToSavedModeWithoutChangingFillSave()
        {
            var prefix = System.IO.Path.Combine(Application.temporaryCachePath,
                "construction-continue-" + System.Guid.NewGuid().ToString("N"));
            var fillStore = new ConstructionRunStore(prefix + "-fill.json");
            var endlessStore = new ConstructionRunStore(prefix + "-endless.json");
            var fill = new ConstructionRunModel(ConstructionGameMode.Fill);
            Assert.That(fill.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(fill.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(fillStore.TrySave(fill), Is.True);
            var endless = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(endless.TrySimulateConstructionDay(out _), Is.False);
            Assert.That(endless.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(endless.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(endlessStore.TrySave(endless), Is.True);
            Assert.That(endlessStore.TryLoad(out var continued), Is.True);

            ConstructionGameMode? requestedMode = null;
            // Inject at the scene boundary so the menu click never reads the
            // player's default save. This tests the card route, not PathFor.
            void UseIsolatedRun(UnityEngine.SceneManagement.Scene scene,
                LoadSceneMode loadMode)
            {
                if (scene.name != "ConstructionPrototype") return;
                requestedMode = ConstructionLaunchContext.Take();
                Object.FindFirstObjectByType<ConstructionPrototypeController>()
                    .UseRunForTesting(continued, endlessStore);
            }

            try
            {
                yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
                var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
                Assert.That(bootstrap, Is.Not.Null);
                var generated = bootstrap.transform.Find(
                    UrbanWildlifeBootstrap.GeneratedRootName);
                var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
                var menu = hud.transform.Find("Main Menu");
                var entry = bootstrap.GetComponent<ConstructionMenuEntry>();
                for (var frame = 0; frame < 120 &&
                     menu.Find("Construction Mode Entrance") == null; frame++)
                    yield return null;
                Assert.That(menu.Find("Construction Mode Entrance"), Is.Not.Null);
                // The preceding return-to-menu test can leave its visible
                // camera transition running briefly. A new menu click is
                // intentionally unavailable until that transition finishes.
                var transitionDeadline = Time.realtimeSinceStartup + 4f;
                while (GameObject.Find("Tabletop scene transition") != null &&
                       Time.realtimeSinceStartup < transitionDeadline)
                    yield return null;
                Assert.That(GameObject.Find("Tabletop scene transition"),
                    Is.Null, "The Main menu must be ready before Continue is clicked.");

                entry.UseSaveStatesForTesting(
                    fillStore.StatusFor(ConstructionGameMode.Fill),
                    endlessStore.StatusFor(ConstructionGameMode.Endless));
                runtime.UseLanguageForTesting(InterfaceLanguage.English);
                yield return null;
                var fillCard = menu.Find("Construction Mode Entrance");
                var endlessCard = menu.Find("Sandbox Mode Entrance");
                Assert.That(fillCard.Find("Construction Entry Label")
                    .GetComponent<Text>().text, Is.EqualTo("Continue Game"));
                Assert.That(endlessCard.Find("Live Mode Label")
                    .GetComponent<Text>().text, Is.EqualTo("Continue Game"));

                SceneManager.sceneLoaded += UseIsolatedRun;
                endlessCard.GetComponent<Button>().onClick.Invoke();
                Assert.That(GameObject.Find("Tabletop scene transition"),
                    Is.Not.Null, "Continue must start the scene transition.");
                var sceneDeadline = Time.realtimeSinceStartup + 8f;
                while (SceneManager.GetActiveScene().name !=
                       "ConstructionPrototype" &&
                       Time.realtimeSinceStartup < sceneDeadline)
                    yield return null;
                Assert.That(SceneManager.GetActiveScene().name,
                    Is.EqualTo("ConstructionPrototype"));
                Assert.That(requestedMode, Is.EqualTo(ConstructionGameMode.Endless));
                var controller = Object.FindFirstObjectByType<
                    ConstructionPrototypeController>();
                Assert.That(controller.Mode, Is.EqualTo(ConstructionGameMode.Endless));
                Assert.That(controller.CurrentDay, Is.EqualTo(2));
                Assert.That(controller.BuiltCount, Is.EqualTo(2));
                Assert.That(fillStore.TryLoad(out var fillAfter), Is.True);
                Assert.That(fillAfter.Mode, Is.EqualTo(ConstructionGameMode.Fill));
                Assert.That(fillAfter.CurrentDay, Is.EqualTo(fill.CurrentDay));
                Assert.That(fillAfter.BuiltCount, Is.EqualTo(fill.BuiltCount));
            }
            finally
            {
                SceneManager.sceneLoaded -= UseIsolatedRun;
                fillStore.TryDelete();
                endlessStore.TryDelete();
            }
        }

        [UnityTest]
        public IEnumerator LegacyMenuCanEnterConstructionAndConstructionCanReturn()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.GetComponent<ConstructionMenuEntry>(), Is.Not.Null);
            var generated = bootstrap.transform.Find(
                UrbanWildlifeBootstrap.GeneratedRootName);
            Assert.That(generated, Is.Not.Null);
            var originalFoundation = generated.Find(
                "7x7 Modular Floor/Fixed Board Foundation");
            var openingPreview = generated.GetComponent<ConstructionOpeningBoardPreview>();
            Assert.That(openingPreview.IsCaseClosed, Is.True,
                "The menu should conceal the playable board until a mode is chosen.");
            Assert.That(openingPreview.OpeningOak.VisualRoot.gameObject.activeSelf,
                Is.False, "The tall oak and its badge must not protrude through a shut lid.");
            Assert.That(openingPreview.OpeningSquirrel.gameObject.activeSelf,
                Is.False, "The squirrel must be concealed with the shut board.");
            var gameplayRim = originalFoundation.Find(
                "Automatic Boundary Cover Layer");
            Assert.That(gameplayRim, Is.Not.Null);
            Assert.That(gameplayRim.gameObject.activeSelf, Is.False,
                "The original board rim must not become a second frame under the closed box.");
            var frontWall = openingPreview.CaseShell.Find("Slim front case edge");
            Assert.That(frontWall, Is.Not.Null,
                "The case should retain one slim physical front edge.");
            Assert.That(frontWall.localScale.y, Is.LessThan(0.30f),
                "A tall opaque wall looks like an extra frame under the lid.");
            Assert.That(frontWall.gameObject.activeSelf, Is.False,
                "The closed reference has a soft shadow, not a front wall below its lid.");
            var lidHalf = openingPreview.CaseShell.Find("West lid hinge/Navy lid half");
            Assert.That(lidHalf, Is.Not.Null);
            Assert.That(lidHalf.localScale.z,
                Is.LessThan(7f * WorldScaleStandards.CellSizeMeters + 0.81f),
                "The closed lid needs a slim edge, not another thick outer band.");
            var contactShadow = openingPreview.CaseShell.Find(
                "Soft tabletop contact shadow");
            Assert.That(contactShadow, Is.Not.Null,
                "The depth below the reference box is a cast shadow on the tabletop.");
            var shadowTexture = contactShadow.GetComponent<MeshRenderer>()
                .sharedMaterial.mainTexture as Texture2D;
            Assert.That(shadowTexture, Is.Not.Null);
            Assert.That(shadowTexture.GetPixel(0, 0).a, Is.LessThan(0.01f),
                "The shadow must feather to transparency, not form a hard outer frame.");
            Assert.That(shadowTexture.GetPixel(shadowTexture.width / 2, 0).a,
                Is.LessThan(0.01f),
                "The lower edge of the shadow quad must not be a visible line.");
            Assert.That(shadowTexture.GetPixel(shadowTexture.width / 2,
                shadowTexture.height - 1).a, Is.LessThan(0.01f));
            Assert.That(shadowTexture.GetPixel(shadowTexture.width / 2,
                shadowTexture.height / 2).a, Is.GreaterThan(0.2f));
            Assert.That(openingPreview.CaseShell.Find("Dark lower case bevel"),
                Is.Null, "The front should not be built from stacked flat bands.");
            Assert.That(openingPreview.CaseShell.parent, Is.SameAs(originalFoundation),
                "The lid must belong to the same board used in gameplay.");
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            if (persistence != null) Object.Destroy(persistence);
            var hud = generated.GetComponentInChildren<UrbanWildlifeRooms.UI.UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            Transform entrance = null;
            for (var frame = 0; frame < 120 && entrance == null; frame++)
            {
                entrance = menu.Find("Construction Mode Entrance");
                if (entrance == null) yield return null;
            }
            Assert.That(entrance, Is.Not.Null);
            Assert.That(entrance.gameObject.activeInHierarchy, Is.True);
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(entrance.gameObject.activeInHierarchy, Is.True);
            var rect = entrance.GetComponent<RectTransform>();
            var endlessRect = menu.Find("Sandbox Mode Entrance")
                .GetComponent<RectTransform>();
            Assert.That(rect.sizeDelta, Is.EqualTo(endlessRect.sizeDelta),
                "Fill and Endless use the same sized mode card.");
            var canvas = menu.GetComponentInParent<Canvas>();
            var entranceBounds = ScreenBounds(rect, canvas);
            var endlessBounds = ScreenBounds(endlessRect, canvas);
            var titleRect = menu.Find("Desktop Title").GetComponent<RectTransform>();
            var titleBounds = ScreenBounds(titleRect, canvas);
            Assert.That(titleRect.anchoredPosition.y, Is.GreaterThan(0f),
                "The wordmark belongs above the closed board, not on its artwork.");
            Assert.That(rect.anchoredPosition.y,
                Is.EqualTo(endlessRect.anchoredPosition.y).Within(0.01f),
                "Both game modes share one layout baseline.");
            var backdrop = menu.Find("Main Menu Choice Backdrop");
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.GetComponent<RoundedPanelGraphic>().raycastTarget,
                Is.False, "The menu backing must not block mode selection.");
            Assert.That(backdrop.GetComponent<RoundedPanelGraphic>().color.a,
                Is.Zero, "The tabletop and live 3D board must remain visible.");
            Assert.That(entranceBounds.xMax, Is.LessThan(endlessBounds.xMin),
                "Fill and Endless should be separate buttons in one row below the box.");
            Assert.That(Mathf.Abs(entranceBounds.center.y - endlessBounds.center.y),
                Is.LessThan(2f), "The two mode buttons should share a baseline.");
            Assert.That(titleBounds.yMin, Is.GreaterThan(entranceBounds.yMax),
                "The wordmark should sit above the box, away from the mode buttons.");
            Assert.That(entranceBounds.Overlaps(ScreenBounds(
                menu.Find("Desktop Settings").GetComponent<RectTransform>(), canvas)),
                Is.False, "The settings control must not cover mode selection.");
            Assert.That(endlessBounds.Overlaps(ScreenBounds(
                menu.Find("Desktop Exit").GetComponent<RectTransform>(), canvas)),
                Is.False, "The exit control must not cover mode selection.");
            Assert.That(entrance.GetComponent<RoundedPanelGraphic>().BorderWidth,
                Is.Zero, "The artwork already has its own rim; no second button frame.");
            Assert.That(entrance.GetComponent<RoundedPanelGraphic>().color.a,
                Is.LessThan(0.01f));
            Assert.That(entrance.Find("Mode plaque artwork").GetComponent<Image>().sprite,
                Is.Not.Null, "The illustrated plaque must remain visible.");
            var modeShadow = entrance.Find("Soft plaque shadow")
                .GetComponent<Image>();
            Assert.That(modeShadow.raycastTarget, Is.False);
            var modeShadowTexture = modeShadow.sprite.texture;
            Assert.That(modeShadowTexture.GetPixel(0, 0).a, Is.LessThan(0.01f),
                "The dark underside of the button must feather away like a shadow.");
            Assert.That(endlessRect.Find("Soft plaque shadow"), Is.Not.Null,
                "Both modes use the same single-plaque-and-shadow treatment.");
            Assert.That(entrance.Find("Construction Entry Label")
                .GetComponent<Text>().alignment, Is.EqualTo(TextAnchor.MiddleLeft));
            Assert.That(menu.Find("Best Survival Record").gameObject.activeSelf,
                Is.False, "The retired survival record must not compete with the two construction modes.");
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(
                    canvas.worldCamera, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits, Has.Some.Matches<RaycastResult>(hit =>
                hit.gameObject == entrance.gameObject),
                "The construction button must be visible to a real UI click. Hits: " +
                string.Join(", ", hits.ConvertAll(hit => hit.gameObject.name)));
            Assert.That(menu.Find("Sandbox Mode Entrance"), Is.Not.Null,
                "The old mode must remain available.");
            Assert.That(entrance.GetComponent<UrbanWildlifeRooms.UI.MainMenuModeCardFeedback>(),
                Is.Not.Null, "The new entry should use the existing mode-card hover style.");
            Assert.That(entrance.Find("Construction pictogram"), Is.Not.Null);
            entrance.GetComponent<Button>().onClick.Invoke();
            var bridge = GameObject.Find("Tabletop scene transition");
            Assert.That(bridge, Is.Not.Null,
                "Choosing a mode must start the case transition immediately.");
            Assert.That(bridge.transform.Find("Left closing board-case cover"), Is.Null);
            Assert.That(bridge.transform.Find("Right closing board-case cover"), Is.Null,
                "Mode selection must not play a case-closing animation before opening.");
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            Assert.That(openingPreview.IsCaseClosed, Is.False,
                "Mode selection should lift the lid during the camera move.");
            Assert.That(openingPreview.IsCaseOpen, Is.False,
                "The two halves should visibly pass through an opening phase.");
            Assert.That(bridge.transform.Find("Matching tabletop")
                    .GetComponent<CanvasGroup>().alpha, Is.LessThan(0.01f),
                "The board remains visible during the initial camera move.");
            var boardCamera = generated.GetComponentInChildren<BoardCameraController>(true);
            Assert.That(boardCamera, Is.Not.Null);
            Assert.That(boardCamera.GetComponent<Camera>().orthographic, Is.False,
                "Entering construction should begin with the old perspective-camera move.");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(bridge.transform.Find("Matching tabletop")
                    .GetComponent<CanvasGroup>().alpha, Is.LessThan(0.01f),
                "The shared 3D board must not disappear behind a full-screen image while the lid and camera move together.");
            for (var frame = 0; frame < 300 &&
                 SceneManager.GetActiveScene().name != "ConstructionPrototype"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("ConstructionPrototype"));
            Assert.That(originalFoundation, Is.Not.Null,
                "Fill must retain the opening board object through the transition.");
            var opening = generated.GetComponent<ConstructionOpeningBoardPreview>();
            Assert.That(opening.IsPreviewing, Is.True,
                "Construction must not restore the full-board preview during the fade.");
            var lidDeadline = Time.realtimeSinceStartup + 3f;
            while (!opening.IsCaseOpen &&
                   Time.realtimeSinceStartup < lidDeadline)
                yield return null;
            Assert.That(opening.IsCaseOpen, Is.True,
                "The same lid should finish opening while the camera turns.");
            var menuOakMount = opening.OpeningOak.VisualRoot.Find(
                "Ivory room mounting card");
            Assert.That(menuOakMount, Is.Not.Null);
            Assert.That(menuOakMount.gameObject.activeSelf, Is.False,
                "The menu oak's oversized ivory frame must not remain on the gameplay board.");
            Assert.That(gameplayRim.gameObject.activeSelf, Is.True,
                "The original board rim must return when the lid opens for gameplay.");
            Assert.That(frontWall.gameObject.activeSelf, Is.True,
                "Opening the box restores its physical front edge.");
            var centreOak = opening.OpeningOak.VisualRoot.position;
            Assert.That(Mathf.Abs(centreOak.x), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(centreOak.z), Is.LessThan(0.05f),
                "The only oak must share the centre cell with the squirrel.");
            // A legacy room highlight calls RoomView.ApplyState, which used to
            // put the starter oak back in its old full-board slot after entry.
            opening.OpeningOak.SetSelected(true);
            opening.OpeningOak.SetSelected(false);
            yield return null;
            centreOak = opening.OpeningOak.VisualRoot.position;
            Assert.That(Mathf.Abs(centreOak.x), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(centreOak.z), Is.LessThan(0.05f),
                "Legacy presentation refresh must not displace the starter oak.");

            var live = Object.FindFirstObjectByType<ConstructionPrototypeController>();
            Assert.That(live, Is.Not.Null);
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var constructionWorld = live.GetComponent<ConstructionWorldView>();
                Assert.That(constructionWorld.BoardFoundation,
                    Is.SameAs(originalFoundation),
                    "Fill should display the same physical board as its opening.");
                Assert.That(constructionWorld.UsesOpeningBoard, Is.True);
                var emptyFace = originalFoundation.Find(
                    "49 Fixed Cell Slots/Foundation Slot 1-1/Pastel plot face");
                Assert.That(emptyFace, Is.Not.Null);
                var emptyRenderer = emptyFace.GetComponent<Renderer>();
                Assert.That(emptyRenderer.sharedMaterial.HasProperty("_ShadowStrength"),
                    Is.True);
                var emptyProperties = new MaterialPropertyBlock();
                emptyRenderer.GetPropertyBlock(emptyProperties);
                Assert.That(emptyProperties.GetFloat("_ShadowStrength"), Is.Zero,
                    "The playable opening board must not receive case-lid shadows.");
                if (constructionWorld.IsEntranceCameraMoving)
                {
                    Assert.That(opening.Tabletop.GetComponent<
                        TabletopBackdropAlignment>().enabled, Is.True,
                        "The desk image must remain camera-facing instead of moving through the board.");
                    var position = boardCamera.GetComponent<Camera>().transform.position;
                    constructionWorld.SetViewport(0, 0);
                    Assert.That(Vector3.Distance(position,
                        boardCamera.GetComponent<Camera>().transform.position),
                        Is.LessThan(0.0001f),
                        "Refreshing the viewport must not snap the entrance camera.");
                }
                Assert.That(live.transform.Find("Construction UI/Left board-case cover"),
                    Is.Null,
                    "The already-visible board must not acquire a closing lid on entry.");
                var constructionUi = live.transform.Find("Construction UI");
                Assert.That(constructionUi.Find("Observation frame").localScale.x,
                    Is.EqualTo(1f).Within(0.01f),
                    "The physical lid already unfolded; the UI leaf must not fold twice.");
                var openDeadline = Time.realtimeSinceStartup + 6f;
                while (!live.IsBoardCaseOpen &&
                       Time.realtimeSinceStartup < openDeadline)
                    yield return null;
                Assert.That(live.IsBoardCaseOpen, Is.True);
                Assert.That(constructionUi.Find("Observation frame")
                    .GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
                Assert.That(constructionUi.Find("7 by 7 construction board")
                    .GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
                Assert.That(opening.CaseShell.Find("West lid hinge/Printed city inside lid")
                    .GetComponent<Renderer>().enabled, Is.False,
                    "The physical print should hand off to the matching UI page.");
            }
            live.UseRunForTesting(new ConstructionRunModel());
            live.ToggleBuildPalette();
            var pauseToggle = typeof(ConstructionPrototypeController).GetMethod(
                "TogglePauseMenu", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(pauseToggle, Is.Not.Null);
            pauseToggle.Invoke(live, null);
            yield return null;
            var liveCanvas = live.transform.Find("Construction UI").GetComponent<Canvas>();
            var pauseActions = live.transform.Find(
                "Construction UI/Esc pause and settings/Pause actions");
            Assert.That(pauseActions, Is.Not.Null);
            Assert.That(pauseActions.gameObject.activeInHierarchy, Is.True);
            var back = pauseActions.Find("Main menu").GetComponent<Button>();
            var reset = pauseActions.Find("New run").GetComponent<Button>();
            foreach (var button in new[] { back, reset })
            {
                var navigationRect = button.GetComponent<RectTransform>();
                var navigationPointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(
                        liveCanvas.worldCamera,
                        navigationRect.TransformPoint(navigationRect.rect.center))
                };
                var navigationHits = new List<RaycastResult>();
                liveCanvas.GetComponent<GraphicRaycaster>().Raycast(
                    navigationPointer, navigationHits);
                Assert.That(button.interactable, Is.True);
                Assert.That(navigationHits.Count, Is.GreaterThan(0),
                    button.name + " has no raycast hit at " +
                    navigationPointer.position + "; screen=" +
                    Screen.width + "x" + Screen.height);
                Assert.That(navigationHits[0].gameObject, Is.SameAs(button.gameObject),
                    button.name + " must be clickable. Hits: " +
                    string.Join(", ", navigationHits.ConvertAll(
                        hit => hit.gameObject.name)));
            }
            reset.onClick.Invoke();
            Assert.That(reset.GetComponentInChildren<Text>().text,
                Is.EqualTo("Confirm restart"),
                "Restart should still require a second deliberate click.");
            reset.onClick.Invoke();
            Assert.That(originalFoundation, Is.Not.Null);
            Assert.That(generated.GetComponent<ConstructionOpeningBoardPreview>()
                .OpeningOak.VisualRoot.gameObject.activeInHierarchy, Is.True,
                "Reset must preserve the original central oak model.");
            // Exercise the live controller and save-before-return path without
            // touching the player's persistent construction run.
            var testSavePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"symbiosis-return-{System.Guid.NewGuid():N}.json");
            live.UseRunForTesting(new ConstructionRunModel(),
                new ConstructionRunStore(testSavePath));
            pauseToggle.Invoke(live, null);
            yield return null;
            back.onClick.Invoke();
            for (var frame = 0; frame < 300 &&
                 SceneManager.GetActiveScene().name != "Main"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            Assert.That(System.IO.File.Exists(testSavePath), Is.True,
                "Returning to the menu should save the current run first.");
            System.IO.File.Delete(testSavePath);
            Assert.That(originalFoundation, Is.Not.Null,
                "Returning to the menu must not rebuild the board.");
            var closeDeadline = Time.realtimeSinceStartup + 6f;
            while (!opening.IsCaseClosed &&
                   Time.realtimeSinceStartup < closeDeadline)
                yield return null;
            var menuCamera = openingPreview.BoardCamera;
            var menuTabletop = openingPreview.Tabletop;
            Assert.That(menuTabletop, Is.Not.Null);
            var tabletopRenderer = menuTabletop.GetComponent<Renderer>();
            Assert.That(menuTabletop.gameObject.activeInHierarchy, Is.True);
            Assert.That(tabletopRenderer.enabled, Is.True);
            Assert.That(tabletopRenderer.sharedMaterial.mainTexture, Is.Not.Null);
            var tabletopDepth = Vector3.Dot(
                menuTabletop.position - menuCamera.transform.position,
                menuCamera.transform.forward);
            Assert.That(tabletopDepth, Is.GreaterThan(menuCamera.nearClipPlane));
            Assert.That(tabletopDepth, Is.LessThan(menuCamera.farClipPlane),
                "Restart then return must keep the photographed tabletop inside the menu camera frustum.");
            Assert.That(opening.IsCaseClosed, Is.True,
                "Returning to the menu must close the same two lid halves. " +
                "Previewing=" + opening.IsPreviewing +
                "; caseOpen=" + opening.IsCaseOpen +
                "; constructionLoaded=" +
                    SceneManager.GetSceneByName("ConstructionPrototype").isLoaded +
                "; bridge=" + (GameObject.Find("Tabletop scene transition") != null));
            Assert.That(menuOakMount.gameObject.activeSelf, Is.True,
                "The oak mounting card should return with the menu preview.");
            Assert.That(gameplayRim.gameObject.activeSelf, Is.False,
                "Returning to the shut box must conceal the gameplay rim again.");
            Assert.That(frontWall.gameObject.activeSelf, Is.False,
                "Closing the lid must not recreate a hard lower frame.");
            if (SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.That(opening.CaseShell.Find("West lid hinge/Printed city inside lid")
                    .GetComponent<Renderer>().enabled, Is.True,
                    "The printed lid must return before the UI pages disappear.");
            Assert.That(System.Array.FindAll(
                generated.GetComponentsInChildren<RoomView>(true),
                room => room.VisualRoot.gameObject.activeInHierarchy).Length,
                Is.Zero,
                "A shut box must conceal even the central oak until it opens again.");
            Assert.That(entrance.GetComponent<Button>().interactable, Is.True,
                "Returning must let the player choose Fill again.");
            entrance.GetComponent<Button>().onClick.Invoke();
            // The opening animation uses unscaled seconds. In batch mode,
            // 300 frames can finish before its 1.8-second camera move.
            yield return new WaitForSecondsRealtime(2.1f);
            for (var frame = 0; frame < 300 &&
                 SceneManager.GetActiveScene().name != "ConstructionPrototype"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("ConstructionPrototype"),
                "The returned menu card must actually start another construction run.");
        }

        private static Rect ScreenBounds(RectTransform rect, Canvas canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var a = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
    }
}
