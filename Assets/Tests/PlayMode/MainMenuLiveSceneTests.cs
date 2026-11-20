using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class MainMenuLiveSceneTests
    {
        [UnityTest]
        public IEnumerator EndlessEntryWaitsForDesktopMenuBinding()
        {
            var probe = new GameObject("Unbound construction menu entry test");
            try
            {
                var entry = probe.AddComponent<ConstructionMenuEntry>();
                Assert.DoesNotThrow(() => entry.EnterEndlessConstruction(),
                    "A click before the menu has been bound must not access a missing Fill card.");
            }
            finally
            {
                Object.Destroy(probe);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DesktopLeaderboardOpensModeSeparatedLocalScores()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(
                UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            for (var frame = 0; frame < 120 &&
                 menu.Find("Desktop Leaderboard") == null; frame++)
                yield return null;

            var leaderboard = menu.Find("Desktop Leaderboard");
            Assert.That(leaderboard, Is.Not.Null);
            var language = menu.Find("Desktop Language")
                .GetComponent<RectTransform>();
            var rankings = leaderboard.GetComponent<RectTransform>();
            var exit = menu.Find("Desktop Exit").GetComponent<RectTransform>();
            Assert.That(rankings.anchoredPosition.x,
                Is.GreaterThan(language.anchoredPosition.x));
            Assert.That(rankings.anchoredPosition.x,
                Is.LessThan(exit.anchoredPosition.x));
            Assert.That(rankings.anchoredPosition.y,
                Is.EqualTo(language.anchoredPosition.y).Within(0.01f));
            Assert.That(leaderboard.Find("Paper Button Backing/Control Pictogram")
                .GetComponent<Image>().sprite, Is.Not.Null);

            runtime.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            Assert.That(leaderboard.Find("Live Control Label")
                .GetComponent<Text>().text, Is.EqualTo("排行榜"));
            leaderboard.Find("Paper Button Backing")
                .GetComponent<Button>().onClick.Invoke();
            var overlay = menu.Find("Main Menu Local Rankings");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            var paper = overlay.Find("Gold edge/Ranking paper");
            Assert.That(paper.Find("Ranking title").GetComponent<Text>().text,
                Is.EqualTo("本地排行榜"));
            Assert.That(paper.Find("Rankings header Days")
                .GetComponent<Text>().text, Is.EqualTo("奖项"));
            paper.Find("Rankings BoardGame").GetComponent<Button>()
                .onClick.Invoke();
            Assert.That(paper.Find("Rankings BoardGame/Mode label")
                .GetComponent<Text>().text, Is.EqualTo("桌游"));
            Assert.That(paper.Find("Rankings header Days")
                .GetComponent<Text>().text, Is.EqualTo("均衡"));
            paper.Find("Close local rankings").GetComponent<Button>()
                .onClick.Invoke();
            Assert.That(overlay.gameObject.activeSelf, Is.False);

            runtime.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            Assert.That(leaderboard.Find("Live Control Label")
                .GetComponent<Text>().text, Is.EqualTo("Rankings"));
        }

        [UnityTest]
        public IEnumerator ModeCardsShowIndependentSaveActionsWithMatchingFonts()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var entry = bootstrap.GetComponent<ConstructionMenuEntry>();
            var menu = hud.transform.Find("Main Menu");
            for (var frame = 0; frame < 120 &&
                 menu.Find("Construction Mode Entrance") == null; frame++)
                yield return null;

            Assert.That(menu.Find("Construction Mode Entrance"), Is.Not.Null);
            var fill = menu.Find("Construction Mode Entrance");
            var endless = menu.Find("Sandbox Mode Entrance");
            var boardGame = menu.Find("Board Game Mode Entrance");
            Assert.That(boardGame, Is.Not.Null);
            var fillAction = fill.Find("Construction Entry Label").GetComponent<Text>();
            var fillMode = fill.Find("Construction Mode Label").GetComponent<Text>();
            var endlessAction = endless.Find("Live Mode Label").GetComponent<Text>();
            var endlessMode = endless.Find("Endless Mode Subtitle").GetComponent<Text>();
            var boardAction = boardGame.Find("Board Game Entry Label").GetComponent<Text>();
            var boardMode = boardGame.Find("Board Game Mode Label").GetComponent<Text>();

            entry.UseSaveStatesForTesting(ConstructionSavedRunStatus.InProgress,
                ConstructionSavedRunStatus.None, ConstructionSavedRunStatus.Finished);
            runtime.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            Assert.That(fillAction.text, Is.EqualTo("继续游戏"));
            Assert.That(fillMode.text, Is.EqualTo("填空模式"));
            Assert.That(endlessAction.text, Is.EqualTo("开始游戏"));
            Assert.That(endlessMode.text, Is.EqualTo("无尽模式"));
            Assert.That(boardAction.text, Is.EqualTo("查看结果"));
            Assert.That(boardMode.text, Is.EqualTo("桌游模式"));
            var chineseFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
            Assert.That(fillAction.font, Is.SameAs(chineseFont));
            Assert.That(endlessAction.font, Is.SameAs(chineseFont));
            Assert.That(fillMode.font, Is.SameAs(chineseFont));
            Assert.That(endlessMode.font, Is.SameAs(chineseFont));
            Assert.That(boardAction.font, Is.SameAs(chineseFont));
            Assert.That(boardMode.font, Is.SameAs(chineseFont));
            Assert.That(fillAction.preferredWidth,
                Is.LessThanOrEqualTo(fillAction.rectTransform.rect.width));
            Assert.That(endlessAction.preferredWidth,
                Is.LessThanOrEqualTo(endlessAction.rectTransform.rect.width));
            Assert.That(fillMode.preferredWidth,
                Is.LessThanOrEqualTo(fillMode.rectTransform.rect.width));
            Assert.That(endlessMode.preferredWidth,
                Is.LessThanOrEqualTo(endlessMode.rectTransform.rect.width));
            Assert.That(boardAction.preferredWidth,
                Is.LessThanOrEqualTo(boardAction.rectTransform.rect.width));
            Assert.That(boardMode.preferredWidth,
                Is.LessThanOrEqualTo(boardMode.rectTransform.rect.width));

            entry.UseSaveStatesForTesting(ConstructionSavedRunStatus.None,
                ConstructionSavedRunStatus.Finished, ConstructionSavedRunStatus.InProgress);
            runtime.UseLanguageForTesting(InterfaceLanguage.English);
            yield return null;
            Assert.That(fillAction.text, Is.EqualTo("Start Game"));
            Assert.That(fillMode.text, Is.EqualTo("Fill Mode"));
            Assert.That(endlessAction.text, Is.EqualTo("View Results"));
            Assert.That(endlessMode.text, Is.EqualTo("Endless Mode"));
            Assert.That(boardAction.text, Is.EqualTo("Continue Game"));
            Assert.That(boardMode.text, Is.EqualTo("Board Game"));
            var englishFont = Resources.Load<Font>("Fonts/Nunito-Bold");
            Assert.That(fillAction.font, Is.SameAs(englishFont));
            Assert.That(endlessAction.font, Is.SameAs(englishFont));
            Assert.That(fillMode.font, Is.SameAs(englishFont));
            Assert.That(endlessMode.font, Is.SameAs(englishFont));
            Assert.That(boardAction.font, Is.SameAs(englishFont));
            Assert.That(boardMode.font, Is.SameAs(englishFont));
            Assert.That(fillAction.fontSize, Is.EqualTo(endlessAction.fontSize));
            Assert.That(fillAction.fontSize, Is.EqualTo(boardAction.fontSize));
            Assert.That(fillMode.fontSize, Is.EqualTo(endlessMode.fontSize));
            Assert.That(fillMode.fontSize, Is.EqualTo(boardMode.fontSize));
            Assert.That(fillAction.preferredWidth,
                Is.LessThanOrEqualTo(fillAction.rectTransform.rect.width));
            Assert.That(endlessAction.preferredWidth,
                Is.LessThanOrEqualTo(endlessAction.rectTransform.rect.width));
            Assert.That(fillMode.preferredWidth,
                Is.LessThanOrEqualTo(fillMode.rectTransform.rect.width));
            Assert.That(endlessMode.preferredWidth,
                Is.LessThanOrEqualTo(endlessMode.rectTransform.rect.width));
            Assert.That(boardAction.preferredWidth,
                Is.LessThanOrEqualTo(boardAction.rectTransform.rect.width));
            Assert.That(boardMode.preferredWidth,
                Is.LessThanOrEqualTo(boardMode.rectTransform.rect.width));
            Assert.That(fill.GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(endless.GetComponent<RectTransform>().sizeDelta));
            Assert.That(boardGame.GetComponent<RectTransform>().sizeDelta,
                Is.EqualTo(fill.GetComponent<RectTransform>().sizeDelta));
            Assert.That(boardGame.GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(fill.GetComponent<RectTransform>().anchoredPosition.y)
                    .Within(0.01f));
            Assert.That(fillAction.horizontalOverflow,
                Is.EqualTo(endlessAction.horizontalOverflow));
            Assert.That(fillAction.verticalOverflow,
                Is.EqualTo(endlessAction.verticalOverflow));
        }

        [UnityTest]
        public IEnumerator AllVisibleModeCardsKeepHoverScaleThroughMenuRefresh()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(
                UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            for (var frame = 0; frame < 120 &&
                 menu.Find("Construction Mode Entrance") == null; frame++)
                yield return null;

            foreach (var name in new[]
                     {
                         "Construction Mode Entrance",
                         "Sandbox Mode Entrance",
                         "Board Game Mode Entrance"
                     })
            {
                var card = menu.Find(name);
                Assert.That(card, Is.Not.Null, name);
                var feedback = card.GetComponent<MainMenuModeCardFeedback>();
                Assert.That(feedback, Is.Not.Null, name);
                feedback.OnPointerEnter(null);
                yield return null; // Runtime refreshes the desktop menu each frame.
                runtime.UseLanguageForTesting(InterfaceLanguage.Chinese);
                Assert.That(card.localScale.x, Is.EqualTo(1.025f).Within(0.001f),
                    name + " must stay enlarged while hovered.");
                feedback.OnPointerExit(null);
                yield return null;
                Assert.That(card.localScale.x, Is.EqualTo(1f).Within(0.001f),
                    name + " must return to its resting size.");
            }
        }

        [UnityTest]
        public IEnumerator DesktopLanguageOpensExplicitChoicesWithoutChangingPreference()
        {
            const string preferenceKey = "symbiosis49.language";
            var originalPreference = PlayerPrefs.GetInt(preferenceKey, -1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Object.Destroy(persistence);
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            runtime.UseLanguageForTesting(InterfaceLanguage.Chinese);
            var desktopLanguage = hud.transform.Find(
                "Main Menu/Desktop Language/Paper Button Backing").GetComponent<Button>();
            desktopLanguage.onClick.Invoke();
            Assert.That(runtime.SettingsOpen, Is.True);
            Assert.That(runtime.Language, Is.EqualTo(InterfaceLanguage.Chinese),
                "Opening language settings must not silently toggle the language.");

            var options = hud.transform.Find(
                "Esc Overlay/Settings Paper Panel/Settings Language Options");
            var english = options.Find("English Option").GetComponent<Button>();
            var chinese = options.Find("Chinese Option").GetComponent<Button>();
            Assert.That(english.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("EN"));
            Assert.That(chinese.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("中文"));
            Assert.That(chinese.GetComponent<Image>().color.a,
                Is.GreaterThan(english.GetComponent<Image>().color.a));

            english.onClick.Invoke();
            Assert.That(runtime.Language, Is.EqualTo(InterfaceLanguage.English));
            Assert.That(english.GetComponent<Image>().color.a,
                Is.GreaterThan(chinese.GetComponent<Image>().color.a));
            english.onClick.Invoke();
            Assert.That(runtime.Language, Is.EqualTo(InterfaceLanguage.English),
                "Selecting the active language must not reverse it.");
            chinese.onClick.Invoke();
            Assert.That(runtime.Language, Is.EqualTo(InterfaceLanguage.Chinese));
            Assert.That(PlayerPrefs.GetInt(preferenceKey, -1),
                Is.EqualTo(originalPreference),
                "A UI test must not replace the player's language preference.");
        }

        [UnityTest]
        public IEnumerator DesktopShowsOnlyCentralOakAndSquirrelOnOriginalBoard()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var preview = generated.GetComponent<ConstructionOpeningBoardPreview>();
            var foundation = generated.Find("7x7 Modular Floor/Fixed Board Foundation");
            var oak = System.Array.Find(generated.GetComponentsInChildren<RoomView>(true),
                room => room.Spec.Id == "oak-a");
            var squirrels = generated.GetComponentsInChildren<SquirrelDemoAgent>(true);

            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.IsPreviewing, Is.True);
            Assert.That(preview.IsCaseClosed, Is.True,
                "The desktop must begin with both lid halves shut.");
            Assert.That(preview.CaseShell.Find("West lid hinge/Navy lid half"),
                Is.Not.Null);
            Assert.That(preview.CaseShell.Find("East lid hinge/Navy lid half"),
                Is.Not.Null);
            Assert.That(preview.CaseShell.Find("West lid hinge/Printed city lid"),
                Is.Not.Null, "The shut left lid needs the approved city artwork.");
            Assert.That(preview.CaseShell.Find("East lid hinge/Printed oak lid"),
                Is.Not.Null, "The shut right lid needs the approved oak artwork.");
            var cityInside = preview.CaseShell.Find(
                "West lid hinge/Printed city inside lid");
            var oakInside = preview.CaseShell.Find(
                "East lid hinge/Printed oak inside lid");
            Assert.That(cityInside, Is.Not.Null,
                "The opening animation must reveal printed city paper, not bare navy.");
            Assert.That(oakInside, Is.Not.Null,
                "The opening animation must reveal printed oak paper, not bare navy.");
            Assert.That(cityInside.GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null);
            Assert.That(oakInside.GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null);
            Assert.That(foundation, Is.Not.Null);
            Assert.That(foundation.gameObject.activeInHierarchy, Is.True);
            Assert.That(oak.VisualRoot.position.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(oak.VisualRoot.position.z, Is.EqualTo(0f).Within(0.01f));
            var matureTree = System.Array.Find(
                oak.VisualRoot.GetComponentsInChildren<Transform>(true),
                child => child.name == "Low-poly Mature Tree");
            Assert.That(matureTree, Is.Not.Null,
                "The opening should reuse the original three-dimensional oak.");
            var openingTreeScale = matureTree.localScale.x;
            var openingTreePosition = matureTree.localPosition;
            Assert.That(openingTreePosition.x, Is.EqualTo(-0.24f).Within(0.001f));
            Assert.That(System.Array.FindAll(generated.GetComponentsInChildren<RoomView>(true),
                room => room.VisualRoot.gameObject.activeInHierarchy).Length, Is.Zero,
                "The closed lid must conceal the starter room until the case opens.");
            Assert.That(System.Array.FindAll(squirrels,
                animal => animal.gameObject.activeInHierarchy).Length, Is.Zero,
                "The squirrel must not show through the closed lid.");
            Assert.That(System.Array.FindAll(
                generated.GetComponentsInChildren<PigeonDemoAgent>(true),
                animal => animal.gameObject.activeInHierarchy).Length, Is.Zero,
                "Legacy pigeons must not appear on vacant construction plots.");

            preview.RestoreFullBoard();
            Assert.That(preview.IsPreviewing, Is.False);
            Assert.That(preview.CaseShell.gameObject.activeSelf, Is.False,
                "The older full-board mode must not retain the closed case lid.");
            Assert.That(matureTree.localScale.x * 1.55f,
                Is.EqualTo(openingTreeScale).Within(0.001f),
                "Returning to the older mode must restore its original tree scale.");
            Assert.That(Vector3.Distance(matureTree.localPosition,
                openingTreePosition), Is.GreaterThan(0.1f),
                "Returning to the older mode must restore its corner planting position.");
            Assert.That(System.Array.FindAll(generated.GetComponentsInChildren<RoomView>(true),
                room => room.VisualRoot.gameObject.activeInHierarchy).Length, Is.EqualTo(49));
            Assert.That(System.Array.FindAll(
                generated.GetComponentsInChildren<PigeonDemoAgent>(true),
                animal => animal.gameObject.activeInHierarchy).Length,
                Is.GreaterThan(0), "The legacy board must be restorable.");
        }

        [UnityTest]
        [Ignore("Legacy Sandbox card now enters Endless Build; construction navigation tests cover the public menu.")]
        public IEnumerator SandboxEntryAndReturnMoveCameraAcrossTheSameVisibleBoard()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Assert.That(generated, Is.Not.Null);

            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence); // Do not modify the player's save.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var boardCamera = generated.GetComponentInChildren<BoardCameraController>(true);
            var camera = boardCamera.GetComponent<Camera>();
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            var backdrop = generated.Find("Gameplay Tabletop Backdrop");
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            Assert.That(runtime.AtDesktop, Is.True);
            Assert.That(boardCamera.IsMenuView, Is.True);
            Assert.That(camera.orthographic, Is.False);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.Find("Warm Tabletop Background"), Is.Null,
                "The main menu must not cover the live board with a baked board image.");
            Assert.That(menu.GetComponent<Image>().color.a, Is.Zero);
            Assert.That(menu.Find("Sandbox Mode Entrance").gameObject.activeSelf, Is.True);
            Assert.That(menu.Find("Research Mode Entrance").gameObject.activeSelf, Is.False,
                "Players should see one Endless entry; the study workflow remains internal.");
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(Vector3.Angle(backdrop.up, -camera.transform.forward), Is.LessThan(0.1f),
                "The tabletop photograph must not receive the board's menu tilt twice.");
            var menuNearWidth = BoardWidthAt(camera, -10f);
            var menuFarWidth = BoardWidthAt(camera, 10f);
            Assert.That(menuFarWidth, Is.LessThan(menuNearWidth * 0.94f),
                "The menu must show the live rooms as a trapezoid.");
            Assert.That(rooms.Length, Is.EqualTo(49));
            var sameBoardRoom = rooms[0];
            var menuCameraSize = camera.orthographicSize;

            // A real save may already be resumable when the scene boots. The
            // Endless card correctly resumes it; this test needs a fresh-run
            // menu state without touching that save on disk.
            if (runtime.HasResumableRun)
            {
                runtime.EndRun(new RunResultsData());
                runtime.ReturnToMainMenuFromResults();
            }

            var sandbox = menu.Find("Sandbox Mode Entrance").GetComponent<Button>();
            sandbox.onClick.Invoke();
            var difficultyCard = menu.Find("Endless Difficulty Overlay/Endless Difficulty Card");
            Assert.That(difficultyCard, Is.Not.Null);
            Assert.That(difficultyCard.Find("Difficulty Setting 2 Label"), Is.Null,
                "Retired community-health controls must not leave empty rows.");
            var rulesText = difficultyCard.Find("Difficulty Rule Summary").GetComponent<Text>().text;
            Assert.That(rulesText.ToLowerInvariant(), Does.Not.Contain("community"));
            Assert.That(rulesText, Does.Not.Contain("社区活力"));
            Assert.That(runtime.AtDesktop, Is.True,
                "Selecting Endless should expose rules before starting the run.");
            difficultyCard.Find("Gentle Difficulty").GetComponent<Button>().onClick.Invoke();
            difficultyCard.Find("Difficulty Setting 0 Increase")
                .GetComponent<Button>().onClick.Invoke();
            difficultyCard.Find("Start Configured Endless")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(runtime.AtDesktop, Is.False);
            Assert.That(runtime.SandboxSettings.wildlifeFloor, Is.EqualTo(9));
            Assert.That(generated.GetComponentInChildren<EndlessBalanceController>(true)
                .Model.CurrentWildlifeFloor, Is.EqualTo(9));

            var deadline = Time.realtimeSinceStartup + 3f;
            while (boardCamera.IsMenuView && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;

            Assert.That(boardCamera.IsMenuView, Is.False);
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.LessThan(menuCameraSize));
            Assert.That(BoardWidthAt(camera, 10f), Is.EqualTo(BoardWidthAt(camera, -10f)).Within(0.001f),
                "Gameplay rooms must return to equal-width square geometry.");
            Assert.That(Vector3.Distance(backdrop.position, new Vector3(0f, -0.58f, 0f)), Is.LessThan(0.01f),
                "Gameplay restores the fixed world-space tabletop.");
            Assert.That(sameBoardRoom, Is.Not.Null,
                "Entering a mode must keep the existing room objects instead of loading a replacement scene.");
            Assert.That(generated.GetComponentsInChildren<RoomView>(true).Length, Is.EqualTo(49));

            runtime.ReturnToDesktop();
            deadline = Time.realtimeSinceStartup + 3f;
            while (!boardCamera.IsMenuView && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(boardCamera.IsMenuView, Is.True);
            Assert.That(camera.orthographic, Is.False);
            Assert.That(camera.orthographicSize, Is.EqualTo(menuCameraSize).Within(0.01f));
            Assert.That(menu.gameObject.activeInHierarchy, Is.True);
            Assert.That(sameBoardRoom, Is.Not.Null);

            if (BuildVariantSettings.SupportsResearch)
            {
                var research = menu.Find("Research Mode Entrance").GetComponent<Button>();
                research.onClick.Invoke();
                var researchSetup = menu.Find("Research Setup Overlay/Handcrafted Research Setup Card");
                Assert.That(researchSetup, Is.Not.Null);
                researchSetup.Find("Start Research Artwork").GetComponent<Button>().onClick.Invoke();
                Assert.That(runtime.AtDesktop, Is.False);
                deadline = Time.realtimeSinceStartup + 3f;
                while (boardCamera.IsMenuView && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(boardCamera.IsMenuView, Is.False);
                Assert.That(sameBoardRoom, Is.Not.Null);
            }
        }

        [UnityTest]
        [Ignore("Legacy difficulty overlay is retired from the public Endless Build flow.")]
        public IEnumerator ExistingEndlessRunCanContinueOrConfirmNewSettings()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var menu = hud.transform.Find("Main Menu");
            var entrance = menu.Find("Sandbox Mode Entrance").GetComponent<Button>();
            var card = menu.Find("Endless Difficulty Overlay/Endless Difficulty Card");
            var oldSettings = EndlessDifficultySettings.Preset(EndlessDifficulty.Gentle);
            runtime.StartNewRun(GameMode.Sandbox, oldSettings);
            runtime.ReturnToDesktop();
            yield return null;

            entrance.onClick.Invoke();
            Assert.That(card.parent.gameObject.activeSelf, Is.True);
            Assert.That(card.Find("Continue Existing Endless").gameObject.activeSelf, Is.True);
            Assert.That(runtime.AtDesktop, Is.True);
            card.Find("Continue Existing Endless").GetComponent<Button>().onClick.Invoke();
            Assert.That(runtime.AtDesktop, Is.False);
            Assert.That(runtime.SandboxSettings.wildlifeFloor, Is.EqualTo(8));

            runtime.ReturnToDesktop();
            yield return null;
            entrance.onClick.Invoke();
            card.Find("Demanding Difficulty").GetComponent<Button>().onClick.Invoke();
            card.Find("Difficulty Setting 0 Increase").GetComponent<Button>().onClick.Invoke();
            var start = card.Find("Start Configured Endless").GetComponent<Button>();
            start.onClick.Invoke();
            Assert.That(runtime.AtDesktop, Is.True,
                "The first New Run click must not replace a resumable run.");
            Assert.That(runtime.SandboxSettings.wildlifeFloor, Is.EqualTo(8));
            Assert.That(start.GetComponentInChildren<Text>().text, Does.Contain("Confirm").Or.Contain("确认"));
            start.onClick.Invoke();
            Assert.That(runtime.AtDesktop, Is.False);
            Assert.That(runtime.SandboxSettings.wildlifeFloor, Is.EqualTo(13));
            Assert.That(runtime.SandboxSettings.wildlifeGraceDays, Is.EqualTo(2));

            if (BuildVariantSettings.SupportsResearch)
            {
                runtime.StartNewRun(GameMode.Research);
                runtime.ReturnToDesktop();
                yield return null;
                entrance.onClick.Invoke();
                Assert.That(card.Find("Continue Existing Endless").gameObject.activeSelf, Is.False);
                start.onClick.Invoke();
                Assert.That(runtime.AtDesktop, Is.True,
                    "Entering Endless must not silently replace a Research save.");
                Assert.That(runtime.Mode, Is.EqualTo(GameMode.Research));
                Assert.That(start.GetComponentInChildren<Text>().text,
                    Does.Contain("Confirm").Or.Contain("确认"));
            }
        }

        private static float BoardWidthAt(Camera camera, float z)
        {
            return camera.WorldToViewportPoint(new Vector3(10f, 0f, z)).x -
                   camera.WorldToViewportPoint(new Vector3(-10f, 0f, z)).x;
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
