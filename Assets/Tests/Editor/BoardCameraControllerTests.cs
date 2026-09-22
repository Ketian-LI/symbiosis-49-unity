using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class BoardCameraControllerTests
    {
        [Test]
        public void MenuUsesMildAngleAndGameplayReturnsToExactTopDownView()
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

                var position = cameraObject.transform.position;
                var horizontalDistance = Vector3.ProjectOnPlane(position, Vector3.up).magnitude;
                var elevation = Mathf.Atan2(position.y, horizontalDistance) * Mathf.Rad2Deg;
                Assert.That(controller.IsMenuView, Is.True);
                Assert.That(camera.orthographicSize, Is.GreaterThan(gameplaySize));
                Assert.That(elevation, Is.EqualTo(74f).Within(0.01f));
                Assert.That(Vector3.Angle(cameraObject.transform.forward, Vector3.down), Is.EqualTo(16f).Within(0.01f));
                Assert.That(camera.orthographic, Is.True);

                controller.SetGameplayViewImmediate();

                Assert.That(controller.IsMenuView, Is.False);
                Assert.That(camera.orthographicSize, Is.EqualTo(gameplaySize).Within(0.001f));
                Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 32f, 0f)));
                Assert.That(Vector3.Angle(cameraObject.transform.forward, Vector3.down), Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
