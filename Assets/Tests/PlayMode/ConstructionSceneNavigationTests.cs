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
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionSceneNavigationTests
    {
        [UnityTest]
        public IEnumerator EndlessMenuCardOpensTheExpandingConstructionMode()
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
            endlessCard.GetComponent<Button>().onClick.Invoke();
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
                Assert.That(controller.GetComponent<ConstructionWorldView>()
                    .BoardFoundation, Is.SameAs(originalFoundation));
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
            var titleBounds = ScreenBounds(menu.Find("Desktop Title")
                .GetComponent<RectTransform>(), canvas);
            var backdrop = menu.Find("Main Menu Choice Backdrop");
            Assert.That(backdrop, Is.Not.Null);
            var backdropRect = backdrop.GetComponent<RectTransform>();
            var backdropBounds = ScreenBounds(backdropRect, canvas);
            Assert.That(rect.anchorMin, Is.EqualTo(backdropRect.anchorMin));
            Assert.That(endlessRect.anchorMin, Is.EqualTo(backdropRect.anchorMin));
            Assert.That(menu.Find("Desktop Title").GetComponent<RectTransform>().anchorMin,
                Is.EqualTo(backdropRect.anchorMin),
                "The whole choice group must stay together in tall windows.");
            Assert.That(backdrop.GetComponent<RoundedPanelGraphic>().raycastTarget,
                Is.False, "The menu backing must not block mode selection.");
            Assert.That(backdrop.GetComponent<RoundedPanelGraphic>().color.a,
                Is.Zero, "The tabletop and live 3D board must remain visible.");
            Assert.That(entranceBounds.Overlaps(endlessBounds),
                Is.False, "The construction button must not cover the legacy mode card.");
            Assert.That(titleBounds.Overlaps(entranceBounds), Is.False,
                "The title must not overlap the first mode card.");
            Assert.That(backdropBounds.Contains(entranceBounds.min) &&
                        backdropBounds.Contains(entranceBounds.max) &&
                        backdropBounds.Contains(endlessBounds.min) &&
                        backdropBounds.Contains(endlessBounds.max), Is.True,
                "Both mode cards should sit on the readable menu backing.");
            Assert.That(entrance.GetComponent<RoundedPanelGraphic>().color.a,
                Is.GreaterThan(0.85f), "The board should not show through mode labels.");
            Assert.That(entrance.Find("Construction Entry Label")
                .GetComponent<Text>().alignment, Is.EqualTo(TextAnchor.MiddleLeft));
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
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            Assert.That(bridge.transform.Find("Matching tabletop")
                    .GetComponent<CanvasGroup>().alpha, Is.LessThan(0.01f),
                "The shared 3D board must not disappear behind a full-screen image at handoff.");
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
            Assert.That(opening.IsCaseOpen, Is.True,
                "The same lid should be fully open when gameplay begins.");
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
                Assert.That(live.GetComponent<ConstructionWorldView>().BoardFoundation,
                    Is.SameAs(originalFoundation),
                    "Fill should display the same physical board as its opening.");
                Assert.That(live.GetComponent<ConstructionWorldView>().UsesOpeningBoard,
                    Is.True);
                Assert.That(live.transform.Find("Construction UI/Left board-case cover"),
                    Is.Null,
                    "The already-visible board must not acquire a closing lid on entry.");
            }
            Assert.That(live.transform.Find("Construction UI/Return to main menu"),
                Is.Not.Null);
            live.UseRunForTesting(new ConstructionRunModel());
            var reset = live.transform.Find("Construction UI/Start a new construction run")
                .GetComponent<Button>();
            reset.onClick.Invoke();
            reset.onClick.Invoke();
            Assert.That(originalFoundation, Is.Not.Null);
            Assert.That(generated.GetComponent<ConstructionOpeningBoardPreview>()
                .OpeningOak.VisualRoot.gameObject.activeInHierarchy, Is.True,
                "Reset must preserve the original central oak model.");
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
            for (var frame = 0; frame < 300 &&
                 SceneManager.GetActiveScene().name != "Main"; frame++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            Assert.That(originalFoundation, Is.Not.Null,
                "Returning to the menu must not rebuild the board.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(opening.IsCaseClosed, Is.True,
                "Returning to the menu must close the same two lid halves.");
            Assert.That(System.Array.FindAll(
                generated.GetComponentsInChildren<RoomView>(true),
                room => room.VisualRoot.gameObject.activeInHierarchy).Length,
                Is.EqualTo(1),
                "Returning to the menu should still show only the central oak.");
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
