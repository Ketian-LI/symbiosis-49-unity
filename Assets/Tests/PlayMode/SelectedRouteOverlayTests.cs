using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class SelectedRouteOverlayTests
    {
        [UnityTest]
        public IEnumerator SelectedResidentAndForagingAnimalExposeTheirRoutes()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            // A test must not read or overwrite the player's saved run.
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            var overlay = generated.GetComponentInChildren<SelectedRouteOverlay>(true);
            var presenter = generated.GetComponentInChildren<CitizenPopulationPresenter>(true);
            var camera = generated.GetComponentInChildren<BoardCameraController>(true);
            var resident = presenter.PrimaryAgent;
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var card = hud.transform.Find("Selected Route Status");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(resident, Is.Not.Null);
            Assert.That(card, Is.Not.Null);

            Assert.That(presenter.TrySelectAgent(resident), Is.True);
            Assert.That(card.gameObject.activeSelf, Is.True);
            var lines = overlay.GetComponentsInChildren<LineRenderer>(true);
            Assert.That(lines.Count(line => line.enabled), Is.EqualTo(3),
                "A full resident cycle should be shown as separate work, meal and home legs.");

            var pigeon = generated.GetComponentInChildren<PigeonDemoAgent>(true);
            Assert.That(pigeon, Is.Not.Null);
            pigeon.CancelFoodMissionForRouteChange();
            var foodPoint = pigeon.transform.position + new Vector3(0.45f, 0f, 0f);
            Assert.That(pigeon.BeginFoodMission(new[] { foodPoint }, 0f, null), Is.True);
            camera.SetGameplayViewImmediate();
            if (EventSystem.current != null)
                EventSystem.current.gameObject.SetActive(false);
            pigeon.gameObject.SendMessage("OnMouseDown");
            Assert.That(lines.Count(line => line.enabled), Is.EqualTo(1));
            Assert.That(lines.Single(line => line.enabled).positionCount, Is.EqualTo(2));
            Assert.That(card.GetComponentInChildren<Text>().text,
                Does.Contain(runtime.Language == InterfaceLanguage.Chinese ? "正在取食" : "Foraging"));

            Assert.That(camera.PanByPixels(new Vector2(80f, 0f), 1000f), Is.True);
            Assert.That(lines.Count(line => line.enabled), Is.EqualTo(1),
                "Panning away from a close-up should not erase the selected route.");

            camera.ReturnToOverviewIfNeeded();
            Assert.That(card.gameObject.activeSelf, Is.False);
            Assert.That(lines.All(line => !line.enabled), Is.True);
        }
    }
}
