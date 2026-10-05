using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class OnboardingResidentClickTests
    {
        [UnityTest]
        public IEnumerator ClickingResidentSpotlightOpensTheRouteEvenWhenHudOverlapsIt()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence); // Leave the player's save untouched.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var onboarding = generated.GetComponentInChildren<FirstRunOnboardingController>(true);
            var overlay = generated.GetComponentInChildren<FirstRunOnboardingOverlay>(true);
            var residents = generated.GetComponentInChildren<CitizenPopulationPresenter>(true);
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(residents.PrimaryAgent, Is.Not.Null);

            runtime.StartNewRun(GameMode.Sandbox);
            onboarding.Replay();
            for (var page = 0; page < GameplayGuideCatalog.PageCount; page++)
            {
                overlay.ContinueRequested?.Invoke();
            }
            yield return null;

            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.SelectResident));
            var spotlight = overlay.transform.Find("First Run Onboarding/Tutorial Spotlight");
            var button = spotlight.GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            Assert.That(spotlight.GetComponent<Image>().raycastTarget, Is.True);

            var hud = overlay.GetComponent<UrbanWildlifeHud>();
            var raycaster = hud.GetComponent<GraphicRaycaster>();
            var screenPosition = RectTransformUtility.WorldToScreenPoint(
                bootstrap.LayoutCamera,
                spotlight.GetComponent<RectTransform>().TransformPoint(
                    spotlight.GetComponent<RectTransform>().rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.SameAs(spotlight.gameObject),
                "The spotlight must receive a click before the speed controls below it.");

            button.onClick.Invoke();
            Assert.That(onboarding.Model.Step, Is.EqualTo(OnboardingStep.InspectWaste));
            Assert.That(overlay.CurrentHeader, Does.Contain("2/4"));
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
