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
                room => room.VisualRoot.gameObject.activeInHierarchy).Length, Is.EqualTo(1));
            Assert.That(System.Array.FindAll(squirrels,
                animal => System.Array.Exists(animal.GetComponentsInChildren<Renderer>(true),
                    renderer => renderer.enabled)).Length, Is.EqualTo(1));
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
