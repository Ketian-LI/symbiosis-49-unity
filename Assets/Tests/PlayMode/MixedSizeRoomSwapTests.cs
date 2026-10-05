using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class MixedSizeRoomSwapTests
    {
        [UnityTest]
        public IEnumerator DraggingOneCellRoomPreviewsAndCommitsOnlyItsTargetExchange()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Object.Destroy(persistence); // Do not read or write the player's save.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var garage = rooms.Single(item => item.Spec.Id == "garage-a");
            var office = rooms.Single(item => item.Spec.Id == "office-a");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(editor, Is.Not.Null);

            runtime.StartNewRun(GameMode.Sandbox);
            editor.SetGuidedTargetRoom(null);
            editor.EnterEditing();
            var officeStart = office.VisualRoot.localPosition;
            var start = bootstrap.LayoutCamera.WorldToScreenPoint(garage.VisualRoot.position);
            var destination = bootstrap.LayoutCamera.WorldToScreenPoint(office.VisualRoot.position);
            var startScreen = new Vector2(start.x, start.y);
            var destinationScreen = new Vector2(destination.x, destination.y);

            InvokeDrag(editor, "HandleDragStarted", garage, startScreen);
            InvokeDrag(editor, "HandleDragging", garage, destinationScreen);
            Assert.That(editor.StatusText, Does.Contain(runtime.Language == InterfaceLanguage.Chinese
                ? "松手交换房间" : "Release to swap rooms"));
            Assert.That(office.VisualRoot.localPosition, Is.Not.EqualTo(officeStart));

            InvokeDrag(editor, "HandleDragEnded", garage, destinationScreen);
            Assert.That(editor.ExportLayout().Single(item => item.id == "garage-a").column, Is.EqualTo(3));
            Assert.That(editor.ExportLayout().Single(item => item.id == "office-a").column, Is.EqualTo(0));
            Assert.That(editor.ExportLayout().Single(item => item.id == "garage-a").row, Is.EqualTo(1));
            Assert.That(editor.ExportLayout().Single(item => item.id == "office-a").row, Is.EqualTo(0));
            Assert.That(editor.PendingMovementCost, Is.EqualTo(0));
            Assert.That(editor.CanConfirm, Is.True);
            editor.ConfirmEditing();
            Assert.That(editor.TotalRoomsMoved, Is.EqualTo(2));
            Assert.That(runtime.LayoutEditing, Is.False);
        }

        [UnityTest]
        public IEnumerator PressingRWhileDraggingRotatesOneCellAnimalPassageAndCommitsExchange()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Object.Destroy(persistence); // Keep the player's save untouched.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var garage = rooms.Single(item => item.Spec.Id == "garage-a");
            var office = rooms.Single(item => item.Spec.Id == "office-a");

            runtime.StartNewRun(GameMode.Sandbox);
            editor.SetGuidedTargetRoom(null);
            editor.EnterEditing();
            // Office A is all-pedestrian and rotationally symmetric; rotate
            // the directional garage passage instead.
            var start = bootstrap.LayoutCamera.WorldToScreenPoint(garage.VisualRoot.position);
            var destination = bootstrap.LayoutCamera.WorldToScreenPoint(office.VisualRoot.position);
            var startScreen = new Vector2(start.x, start.y);
            var destinationScreen = new Vector2(destination.x, destination.y);

            InvokeDrag(editor, "HandleDragStarted", garage, startScreen);
            InvokeDrag(editor, "HandleDragging", garage, destinationScreen);
            editor.RotateSelected();
            Assert.That(editor.StatusText, Does.Contain(runtime.Language == InterfaceLanguage.Chinese
                ? "松手交换房间" : "Release to swap rooms"));
            Assert.That(garage.VisualRoot.localRotation.eulerAngles.y, Is.EqualTo(90f).Within(0.1f));

            InvokeDrag(editor, "HandleDragEnded", garage, destinationScreen);
            var layout = editor.ExportLayout();
            Assert.That(layout.Single(item => item.id == "garage-a").quarterTurns, Is.EqualTo(1));
            Assert.That(layout.Single(item => item.id == "office-a").column, Is.EqualTo(0));
            Assert.That(layout.Single(item => item.id == "office-a").row, Is.EqualTo(0));
            Assert.That(layout.Single(item => item.id == "garage-a").column, Is.EqualTo(3));
            Assert.That(layout.Single(item => item.id == "garage-a").row, Is.EqualTo(1));
            Assert.That(editor.CanConfirm, Is.True);
            editor.ConfirmEditing();
            Assert.That(runtime.LayoutEditing, Is.False);
        }

        [UnityTest]
        public IEnumerator FreeRearrangementDoesNotSpendResourcesAndRenewsNextDay()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);
            var openingBalance = economy.Balance;
            Swap(editor, bootstrap, rooms, "pigeon-d", "residence-d");
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
            Assert.That(editor.LastConfirmedMovementDay, Is.EqualTo(1));
            Assert.That(economy.Balance, Is.EqualTo(openingBalance));

            Swap(editor, bootstrap, rooms, "pigeon-b", "office-a");
            Assert.That(editor.CanConfirm, Is.False, "A second confirmed layout on the same day is blocked.");
            editor.CancelEditing();
            Assert.That(runtime.TrySkipToNextDay(), Is.True);
            var deadline = Time.realtimeSinceStartup + 8f;
            while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(runtime.IsDaySkipping, Is.False);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));

            Swap(editor, bootstrap, rooms, "pigeon-b", "office-a");
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
            Assert.That(editor.LastConfirmedMovementDay, Is.EqualTo(2));
        }

        private static void Swap(RoomLayoutEditorController editor, UrbanWildlifeBootstrap bootstrap,
            RoomView[] rooms, string movingId, string targetId)
        {
            editor.EnterEditing();
            var moving = rooms.Single(item => item.Spec.Id == movingId);
            var target = rooms.Single(item => item.Spec.Id == targetId);
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(moving.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(target.VisualRoot.position);
            InvokeDrag(editor, "HandleDragStarted", moving, new Vector2(from.x, from.y));
            InvokeDrag(editor, "HandleDragging", moving, new Vector2(to.x, to.y));
            InvokeDrag(editor, "HandleDragEnded", moving, new Vector2(to.x, to.y));
        }

        private static void InvokeDrag(
            RoomLayoutEditorController editor, string methodName, RoomView room, Vector2 position)
        {
            var method = typeof(RoomLayoutEditorController).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(editor, new object[] { room, position });
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
