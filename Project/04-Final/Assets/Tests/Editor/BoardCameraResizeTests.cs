using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class BoardCameraResizeTests
    {
        [Test]
        public void MenuFramingRespondsToAspectAndRestoresSixteenByNine()
        {
            var cameraObject = new GameObject("Menu resize camera", typeof(Camera),
                typeof(BoardCameraController));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.transform.SetPositionAndRotation(new Vector3(0f, 32f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));
                camera.orthographic = true;
                camera.orthographicSize = 14.2f;
                camera.aspect = 16f / 9f;
                var controller = cameraObject.GetComponent<BoardCameraController>();
                controller.Initialize(camera);
                controller.SetMenuViewImmediate();
                var wideSize = camera.orthographicSize;

                camera.aspect = 4f / 3f;
                controller.SetMenuViewImmediate();
                Assert.That(camera.orthographicSize,
                    Is.EqualTo(wideSize * 4f / 3f).Within(0.001f));

                camera.aspect = 16f / 9f;
                controller.SetMenuViewImmediate();
                Assert.That(camera.orthographicSize,
                    Is.EqualTo(wideSize).Within(0.001f));

                // The return animation must target the new framing too.
                controller.SetGameplayViewImmediate();
                camera.aspect = 4f / 3f;
                controller.BeginMenuViewTransition(0.5f);
                var endSizeField = typeof(BoardCameraController).GetField(
                    "transitionEndSize", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(endSizeField, Is.Not.Null);
                Assert.That((float)endSizeField.GetValue(controller),
                    Is.EqualTo(wideSize * 4f / 3f).Within(0.001f));

                var transitionPosition = camera.transform.position;
                camera.aspect = 1f;
                var refresh = typeof(BoardCameraController).GetMethod(
                    "RefreshMenuFramingIfNeeded",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(refresh, Is.Not.Null);
                refresh.Invoke(controller, null);
                Assert.That((float)endSizeField.GetValue(controller),
                    Is.EqualTo(wideSize * 16f / 9f).Within(0.001f));
                Assert.That(camera.transform.position,
                    Is.EqualTo(transitionPosition),
                    "Resizing during the return animation must not teleport the camera.");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
