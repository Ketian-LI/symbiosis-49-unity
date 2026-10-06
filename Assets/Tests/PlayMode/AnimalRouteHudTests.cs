using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class AnimalRouteHudTests
    {
        [UnityTest]
        public IEnumerator ThirdDayHungerOpensBorrowedDoorsAndBothActorTypesShowThreeCells()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var pigeon = generated.GetComponentInChildren<PigeonDemoAgent>(true);
            var resident = generated.GetComponentInChildren<CitizenDemoAgent>(true);
            Assert.That(pigeon.GetComponent<WorldStatusPips>()?.IsVisible, Is.True);
            Assert.That(resident.GetComponent<WorldStatusPips>()?.IsVisible, Is.True);
            var residentStatus = resident.transform.Find("Three-cell Status");
            Assert.That(residentStatus, Is.Not.Null);
            Assert.That(residentStatus.Find("Status Rim"), Is.Not.Null);
            Assert.That(residentStatus.Find("Status Backing"), Is.Not.Null);
            var visibleCells = residentStatus.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.name.StartsWith("Status Cell ")).ToArray();
            Assert.That(visibleCells.Length, Is.EqualTo(3));
            Assert.That(visibleCells.All(renderer => renderer.enabled &&
                renderer.gameObject.activeInHierarchy &&
                renderer.transform.localScale.x >= 0.15f &&
                renderer.sharedMaterial.name == "World Status Unlit"), Is.True,
                "The three resident cells must be renderable and readable, not merely active as a parent object.");
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Vector3.Dot(residentStatus.up, -Camera.main.transform.forward),
                Is.GreaterThan(0.98f),
                "The resident cells must face the game camera rather than resemble floor furniture.");
            var bodyTop = resident.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !renderer.transform.IsChildOf(residentStatus))
                .Max(renderer => renderer.bounds.max.y);
            TestContext.WriteLine($"Resident body top {bodyTop:F3}; status bottom " +
                                  $"{visibleCells.Min(renderer => renderer.bounds.min.y):F3}; " +
                                  $"origin {resident.transform.position.y:F3}.");
            Assert.That(residentStatus.position.y - bodyTop,
                Is.InRange(0.04f, 0.16f),
                "The status must sit immediately above the head, not float over furniture behind it.");
            resident.GetComponent<WorldStatusPips>().SetCommuteProgress(
                new ResidentRouteLegs(true, true, true), false, false, false);
            var block = new MaterialPropertyBlock();
            visibleCells[0].GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").g,
                Is.GreaterThan(block.GetColor("_BaseColor").r),
                "Reachable but unfinished commute legs should remain visibly green.");
            resident.GetComponent<WorldStatusPips>().SetCommuteProgress(
                new ResidentRouteLegs(false, false, false), false, false, false);
            visibleCells[0].GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").r,
                Is.GreaterThan(block.GetColor("_BaseColor").g),
                "An unavailable commute leg should turn red immediately.");

            var ordinaryConnections = navigation.NavigationMapFor(pigeon).ConnectionCount;
            needs.Model.Animals["pigeon-01"].hungerDays = 1;
            needs.RestoreSession(needs.Model.Export());
            Assert.That(navigation.NavigationMapFor(pigeon).ConnectionCount,
                Is.EqualTo(ordinaryConnections));
            needs.Model.Animals["pigeon-01"].hungerDays = 2;
            needs.RestoreSession(needs.Model.Export());
            Assert.That(navigation.NavigationMapFor(pigeon).ConnectionCount,
                Is.GreaterThan(ordinaryConnections));
        }

        [UnityTest]
        public IEnumerator FoodRiskUsesCurrentReachablePortionsAndShowsAttentionPanel()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            // The menu-to-gameplay camera/canvas transition must finish before
            // a pointer hit test represents an actionable player click.
            yield return new WaitForSecondsRealtime(2.1f);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            food.RestoreSession(new[]
            {
                new UrbanWildlifeRooms.Data.NaturalFoodSaveData
                {
                    roomId = "pigeon-a", kind = NaturalFoodKind.Seed.ToString(), portions = 1
                }
            });
            yield return null;

            var birds = generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                .Where(bird => bird.IsAlive).ToArray();
            var greenBirds = birds.Count(bird =>
            {
                var cell = bird.transform.Find("Three-cell Status/Status Cell 1")
                    ?.GetComponent<Renderer>();
                if (cell == null) return false;
                var colorBlock = new MaterialPropertyBlock();
                cell.GetPropertyBlock(colorBlock);
                var color = colorBlock.GetColor("_BaseColor");
                return color.g > color.r;
            });
            Assert.That(greenBirds, Is.EqualTo(1),
                "One reachable seed portion must not paint an entire pigeon flock green.");
            Assert.That(needs.AnimalsWithoutFoodAccess, Is.GreaterThan(0));
            var riskPanel = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Need Risk Attention");
            Assert.That(riskPanel.gameObject.activeInHierarchy, Is.True);
            var animalLabel = riskPanel.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Animal Risk");
            Assert.That(animalLabel.text, Does.Contain(needs.AnimalsWithoutFoodAccess.ToString()));
            var animalRiskButton = riskPanel.GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "Animal Risk Target");
            var residentRiskButton = riskPanel.GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "Resident Risk Target");
            Assert.That(animalRiskButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(animalRiskButton.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(residentRiskButton.gameObject.activeSelf, Is.False,
                "A resolved category must not keep a green status row inside the alert.");
            Assert.That(riskPanel.GetComponent<RectTransform>().rect.height,
                Is.EqualTo(44f).Within(0.1f),
                "A single warning should not leave an empty second row.");

            food.RestoreSession(System.Array.Empty<UrbanWildlifeRooms.Data.NaturalFoodSaveData>());
            yield return null;
            Assert.That(needs.AnimalsWithoutFoodAccess, Is.GreaterThan(birds.Length - 1));
            Assert.That(birds.All(bird =>
            {
                var cell = bird.transform.Find("Three-cell Status/Status Cell 1")
                    .GetComponent<Renderer>();
                var colorBlock = new MaterialPropertyBlock();
                cell.GetPropertyBlock(colorBlock);
                var color = colorBlock.GetColor("_BaseColor");
                return color.r > color.g;
            }), Is.True, "The warning must update when the last reachable food is removed.");

            var riskRoom = generated.GetComponentsInChildren<RoomView>(true)
                .Single(room => room.Spec.Id == needs.FirstFoodRiskRoomId);
            var camera = generated.GetComponentInChildren<BoardCameraController>(true);
            Canvas.ForceUpdateCanvases();
            var buttonRect = animalRiskButton.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main,
                buttonRect.TransformPoint(buttonRect.rect.center));
            var click = new PointerEventData(EventSystem.current)
            {
                position = screenPoint,
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(click, hits);
            TestContext.WriteLine($"Risk raycast: screen={Screen.width}x{Screen.height}, " +
                                  $"point={screenPoint}, camera={Camera.main?.pixelRect}, " +
                                  $"raycasters={Object.FindObjectsByType<BaseRaycaster>(FindObjectsSortMode.None).Length}, " +
                                  $"targetActive={animalRiskButton.gameObject.activeInHierarchy}, " +
                                  $"contains={RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPoint, Camera.main)}");
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits.First().gameObject, Is.EqualTo(animalRiskButton.gameObject));
            }
            ExecuteEvents.Execute(animalRiskButton.gameObject, click,
                ExecuteEvents.pointerClickHandler);
            Assert.That(camera.IsFocused, Is.True);
            Assert.That(camera.TargetPosition.x,
                Is.EqualTo(riskRoom.transform.parent.position.x).Within(0.01f));
            Assert.That(camera.TargetPosition.z,
                Is.EqualTo(riskRoom.transform.parent.position.z).Within(0.01f));
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(camera.IsFocused, Is.True,
                "The alert focus must survive any in-progress menu camera transition.");
            Assert.That(camera.TargetPosition.x,
                Is.EqualTo(riskRoom.transform.parent.position.x).Within(0.01f));

            foreach (var animalId in needs.Model.Animals.Keys.ToArray())
                needs.Model.MarkMeal(animalId);
            needs.RestoreSession(needs.Model.Export());
            Assert.That(needs.AnimalsWithoutFoodAccess, Is.Zero);
            Assert.That(riskPanel.gameObject.activeSelf, Is.False,
                "A resolved warning must disappear immediately, not at the next day boundary.");
            camera.ReturnToOverviewIfNeeded();
            animalRiskButton.onClick.Invoke();
            Assert.That(camera.IsFocused, Is.False,
                "A stale alert callback must not navigate after the risk is resolved.");

            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var connected = residents.Model.NavigationMap;
            residents.Model.UpdateNavigation(new RoomNavigationMap(
                System.Array.Empty<RoomPlacementData>(), RoomLayoutData.All, 3.1f,
                humanRoadsOnly: true));
            runtime.SetSpeed(2); // Recompute the HUD after changing the isolated test model.
            Assert.That(residentRiskButton.gameObject.activeInHierarchy, Is.True);
            var residentRoomId = residents.Model.Residents
                .First(person => !residents.Model.PreviewRouteLegs(person.id).Complete).residenceId;
            var residentRoom = generated.GetComponentsInChildren<RoomView>(true)
                .Single(room => room.Spec.Id == residentRoomId);
            residentRiskButton.onClick.Invoke();
            Assert.That(camera.TargetPosition.x,
                Is.EqualTo(residentRoom.transform.parent.position.x).Within(0.01f));
            Assert.That(camera.TargetPosition.z,
                Is.EqualTo(residentRoom.transform.parent.position.z).Within(0.01f));
            residents.Model.UpdateNavigation(connected);
            runtime.SetSpeed(1);
            Assert.That(residentRiskButton.gameObject.activeSelf, Is.False);
            Assert.That(riskPanel.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator NextDayRouteIsAnnouncedAndFloorMarksAppearOnlyWhileEditing()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            yield return null;

            var forecast = generated.GetComponentsInChildren<Text>(true)
                .Single(item => item.gameObject.name == "Market Forecast Text");
            Assert.That(forecast.transform.parent.gameObject.activeInHierarchy, Is.True);
            Assert.That(forecast.text, runtime.Language == InterfaceLanguage.Chinese
                ? Does.Contain("明日 D2 · 公园西侧")
                : Does.Contain("D2 · Park west"));

            var park = generated.GetComponentsInChildren<RoomView>(true)
                .Single(item => item.Spec.Id == "central-park");
            var overlay = park.VisualRoot.GetComponentInChildren<AnimalPassageOverlay>(true);
            Assert.That(overlay.gameObject.activeSelf, Is.False);
            var moveButton = generated.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Enter Layout Editing");
            Assert.That(moveButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(moveButton.GetComponent<Image>().raycastTarget, Is.True);
            moveButton.onClick.Invoke();
            Assert.That(editor.IsEditing, Is.True);
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            editor.CancelEditing();
            Assert.That(overlay.gameObject.activeSelf, Is.False);
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
