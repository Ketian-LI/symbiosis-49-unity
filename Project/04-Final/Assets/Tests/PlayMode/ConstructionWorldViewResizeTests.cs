using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionWorldViewResizeTests
    {
        [UnityTest]
        public IEnumerator ReframingKeepsWorldBoardCentredOnItsUiPlot()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("World camera framing needs a graphics device.");

            var cameraObject = new GameObject("Resize test camera", typeof(Camera));
            var canvasObject = new GameObject("Resize test canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var plotObject = new GameObject("Resize test plot", typeof(RectTransform));
            var viewObject = new GameObject("Resize test world",
                typeof(ConstructionWorldView));
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 42f, 0f),
                    Quaternion.Euler(90f, 0f, 0f));
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600f, 900f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                var plot = plotObject.GetComponent<RectTransform>();
                plot.SetParent(canvasObject.transform, false);
                plot.anchorMin = plot.anchorMax = new Vector2(0.5f, 0.5f);
                plot.sizeDelta = new Vector2(707f, 707f);
                plot.anchoredPosition = new Vector2(100f, -15f);

                var view = viewObject.GetComponent<ConstructionWorldView>();
                view.Initialize(camera, plot, 101f, ConstructionGameMode.Fill);
                view.SetViewport(0, 0);
                Assert.That(camera.WorldToScreenPoint(Vector3.zero).x,
                    Is.EqualTo(PlotScreenCentre(plot).x).Within(2f));
                Assert.That(camera.WorldToScreenPoint(Vector3.zero).y,
                    Is.EqualTo(PlotScreenCentre(plot).y).Within(2f));

                // Simulate the stale framing left by a resized window. Moving
                // the plot also verifies the actual screen-space alignment.
                plot.anchoredPosition += new Vector2(115f, -40f);
                camera.aspect *= 0.85f;
                camera.orthographicSize = 1f;
                var cachedWidth = typeof(ConstructionWorldView).GetField(
                    "framedScreenWidth", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(cachedWidth, Is.Not.Null);
                cachedWidth.SetValue(view, -1);
                yield return null;

                var centre = PlotScreenCentre(plot);
                var projected = camera.WorldToScreenPoint(Vector3.zero);
                Assert.That(projected.x, Is.EqualTo(centre.x).Within(2f));
                Assert.That(projected.y, Is.EqualTo(centre.y).Within(2f));
                Assert.That(camera.orthographicSize, Is.GreaterThan(1f));
                var tabletop = viewObject.transform.Find("Gameplay Tabletop Backdrop");
                Assert.That(tabletop, Is.Not.Null);
                Assert.That(tabletop.localScale.x,
                    Is.EqualTo(camera.orthographicSize * camera.aspect *
                        2f * 1.22f / 10f).Within(0.01f));
            }
            finally
            {
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                Object.Destroy(viewObject);
                Object.Destroy(plotObject);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
            }
        }

        private static Vector2 PlotScreenCentre(RectTransform plot)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            plot.GetWorldCorners(corners);
            return (RectTransformUtility.WorldToScreenPoint(null, corners[0]) +
                RectTransformUtility.WorldToScreenPoint(null, corners[2])) * 0.5f;
        }
    }
}
