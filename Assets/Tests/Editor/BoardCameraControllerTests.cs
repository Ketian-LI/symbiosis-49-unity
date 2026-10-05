using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class BoardCameraControllerTests
    {
        [Test]
        public void MenuUsesTrapezoidPerspectiveAndGameplayRestoresSquareTopDownBoard()
        {
            var cameraObject = new GameObject("Menu Camera Test");
            try
            {
                cameraObject.transform.position = new Vector3(0f, 32f, 0f);
                cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 14.2f;
                var gameplaySize = camera.orthographicSize;

                var initialForward = cameraObject.transform.forward;
                Assert.That(Vector3.Angle(initialForward, Vector3.down), Is.LessThan(0.01f));

                var controller = cameraObject.AddComponent<BoardCameraController>();
                controller.Initialize(camera);
                controller.SetMenuViewImmediate();

                Assert.That(controller.IsMenuView, Is.True);
                Assert.That(camera.orthographicSize, Is.EqualTo(gameplaySize * 1.18f).Within(0.001f));
                Assert.That(Vector3.Angle(cameraObject.transform.forward, Vector3.down), Is.EqualTo(16f).Within(0.01f));
                var menuFocus = cameraObject.transform.position + cameraObject.transform.forward *
                    (cameraObject.transform.position.y / -cameraObject.transform.forward.y);
                Assert.That(menuFocus.magnitude, Is.LessThan(0.01f));
                Assert.That(camera.orthographic, Is.False);
                var nearWidth = camera.WorldToViewportPoint(new Vector3(10f, 0f, -10f)).x -
                    camera.WorldToViewportPoint(new Vector3(-10f, 0f, -10f)).x;
                var farWidth = camera.WorldToViewportPoint(new Vector3(10f, 0f, 10f)).x -
                    camera.WorldToViewportPoint(new Vector3(-10f, 0f, 10f)).x;
                Assert.That(farWidth, Is.LessThan(nearWidth * 0.94f),
                    "The distant menu rooms must appear narrower than the near rooms.");

                controller.SetGameplayViewImmediate();

                Assert.That(controller.IsMenuView, Is.False);
                Assert.That(camera.orthographicSize, Is.EqualTo(gameplaySize).Within(0.001f));
                Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 32f, 0f)));
                Assert.That(Vector3.Angle(cameraObject.transform.forward, Vector3.down), Is.LessThan(0.01f));
                Assert.That(camera.orthographic, Is.True);
                nearWidth = camera.WorldToViewportPoint(new Vector3(10f, 0f, -10f)).x -
                    camera.WorldToViewportPoint(new Vector3(-10f, 0f, -10f)).x;
                farWidth = camera.WorldToViewportPoint(new Vector3(10f, 0f, 10f)).x -
                    camera.WorldToViewportPoint(new Vector3(-10f, 0f, 10f)).x;
                Assert.That(farWidth, Is.EqualTo(nearWidth).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void TabletopPhotographFacesMenuCameraAndReturnsToGameplayPlane()
        {
            var cameraObject = new GameObject("Backdrop Camera Test");
            var backdrop = new GameObject("Backdrop Test");
            try
            {
                cameraObject.transform.position = new Vector3(0f, 32f, 0f);
                cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 14.2f;
                var controller = cameraObject.AddComponent<BoardCameraController>();
                controller.Initialize(camera);

                var originalPosition = new Vector3(0f, -0.58f, 0f);
                backdrop.transform.SetPositionAndRotation(originalPosition, Quaternion.Euler(0f, 180f, 0f));
                var alignment = backdrop.AddComponent<TabletopBackdropAlignment>();
                alignment.Initialize(camera);

                controller.SetMenuViewImmediate();
                alignment.AlignNow();
                Assert.That(Vector3.Angle(backdrop.transform.up, -cameraObject.transform.forward), Is.LessThan(0.01f));
                Assert.That(Vector3.Dot(backdrop.transform.position - cameraObject.transform.position,
                    cameraObject.transform.forward), Is.EqualTo(90f).Within(0.01f));
                Assert.That(backdrop.transform.localScale.x, Is.GreaterThan(1f));

                controller.SetGameplayViewImmediate();
                alignment.AlignNow();
                Assert.That(Vector3.Distance(backdrop.transform.position, originalPosition), Is.LessThan(0.001f));
                Assert.That(Vector3.Angle(backdrop.transform.up, Vector3.up), Is.LessThan(0.01f));
                Assert.That(backdrop.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(backdrop);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void MiddleDragPansFocusedCameraAndClampsItToTheBoard()
        {
            var cameraObject = new GameObject("Focused Camera Pan Test");
            var room = new GameObject("Outer Room");
            try
            {
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 32f, 0f), Quaternion.Euler(90f, 0f, 0f));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12.9f;
                var controller = cameraObject.AddComponent<BoardCameraController>();
                controller.Initialize(camera);
                room.transform.position = new Vector3(9.3f, 0f, 0f);
                controller.FocusRoom(room.transform, 3.1f);
                var start = controller.TargetPosition;

                Assert.That(controller.PanByPixels(new Vector2(120f, -60f), 1000f), Is.True);
                Assert.That(controller.IsFocused, Is.True);
                Assert.That(controller.TargetPosition.x, Is.LessThan(start.x));
                Assert.That(controller.TargetPosition.z, Is.GreaterThan(start.z));
                Assert.That(controller.TargetPosition.y, Is.EqualTo(start.y).Within(0.001f));

                Assert.That(controller.PanByPixels(new Vector2(-10000f, 0f), 1000f), Is.True);
                Assert.That(controller.TargetPosition.x, Is.EqualTo(10.85f).Within(0.001f));
                controller.SetMenuViewImmediate();
                Assert.That(controller.PanByPixels(Vector2.one, 1000f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(room);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void MiddleDragDetachesAnimalFollowWithoutReturningToOverview()
        {
            var cameraObject = new GameObject("Follow Camera Pan Test");
            var animal = new GameObject("Followed Animal");
            try
            {
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 32f, 0f), Quaternion.Euler(90f, 0f, 0f));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12.9f;
                var controller = cameraObject.AddComponent<BoardCameraController>();
                controller.Initialize(camera);
                controller.Follow(animal.transform, 1.45f);
                camera.orthographicSize = 1.45f;
                cameraObject.transform.position = new Vector3(0f, 10f, 0f);
                var stopped = 0;
                controller.FollowingStopped += () => stopped++;

                Assert.That(controller.IsFollowing, Is.True);
                Assert.That(controller.PanByPixels(new Vector2(100f, 0f), 1000f), Is.True);
                Assert.That(controller.IsFollowing, Is.False);
                Assert.That(controller.IsFocused, Is.True);
                Assert.That(controller.TargetSize, Is.EqualTo(1.45f).Within(0.001f));
                Assert.That(controller.TargetPosition.x, Is.LessThan(0f));
                Assert.That(stopped, Is.EqualTo(1));
                Assert.That(controller.PanByPixels(new Vector2(20f, 0f), 1000f), Is.True);
                Assert.That(stopped, Is.EqualTo(1));
                Assert.That(controller.ReturnToOverviewIfNeeded(), Is.True);
                Assert.That(controller.IsFocused, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(animal);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
