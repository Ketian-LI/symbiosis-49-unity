using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionSceneNavigationTests
    {
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
            var canvas = menu.GetComponentInParent<Canvas>();
            var entranceBounds = ScreenBounds(rect, canvas);
            Assert.That(entranceBounds.Overlaps(ScreenBounds(
                menu.Find("Sandbox Mode Entrance").GetComponent<RectTransform>(), canvas)),
                Is.False, "The construction button must not cover the legacy mode card.");
            Assert.That(entranceBounds.Overlaps(ScreenBounds(
                menu.Find("Best Survival Record").GetComponent<RectTransform>(), canvas)),
                Is.False, "The construction button must not cover the record when it appears.");
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
            entrance.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("ConstructionPrototype"));

            var live = Object.FindFirstObjectByType<ConstructionPrototypeController>();
            Assert.That(live, Is.Not.Null);
            Assert.That(live.transform.Find("Construction UI/Return to main menu"),
                Is.Not.Null);
            // Replace the loaded controller with an in-memory test run before
            // returning. The test must not overwrite the player's saved run.
            Object.Destroy(live.gameObject);
            yield return null;
            var testRoot = new GameObject("Unsaved construction navigation test");
            var controller = testRoot.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;
            var back = testRoot.transform.Find(
                "Construction UI/Return to main menu").GetComponent<Button>();
            back.onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
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
