using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class MainMenuLiveSceneTests
    {
        [UnityTest]
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

            var sandbox = menu.Find("Sandbox Mode Entrance").GetComponent<Button>();
            sandbox.onClick.Invoke();
            Assert.That(runtime.AtDesktop, Is.False);

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
