using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionNeighborhoodStoryPresentationTests
    {
        [UnityTest]
        public IEnumerator StoryAndGoalsFitTheSameLeafInBothLanguages()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill, useFillHand: true);
            Assert.That(run.TryConfirmSpecialTasks(), Is.True);
            run.SetOpeningTourCompleted(true);
            var opening = new (ConstructionCategory category, int column, int row)[]
            {
                (ConstructionCategory.Green, 3, 3), (ConstructionCategory.Residence, 4, 3),
                (ConstructionCategory.Restaurant, 5, 3), (ConstructionCategory.Workshop, 4, 4),
                (ConstructionCategory.Street, 4, 2), (ConstructionCategory.Waste, 6, 3),
                (ConstructionCategory.Supermarket, 3, 2), (ConstructionCategory.Square, 4, 1),
                (ConstructionCategory.Green, 2, 3)
            };
            foreach (var tile in opening)
            {
                Assert.That(run.TryBuild(tile.column, tile.row, tile.category,
                    tile.category == ConstructionCategory.Green
                        ? tile.column == 3 ? GreenPlanting.Oak : GreenPlanting.Meadow
                        : GreenPlanting.None, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            var root = new GameObject("Neighbourhood story test");
            try
            {
                var controller = root.AddComponent<ConstructionPrototypeController>();
                controller.UseRunForTesting(run);
                yield return null;
                var deadline = Time.realtimeSinceStartup + 12f;
                while (!controller.IsBoardCaseOpen && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(controller.IsBoardCaseOpen, Is.True);
                var side = root.transform.Find("Construction UI/Building choices");
                var paper = side.Find("Neighbourhood story paper").GetComponent<RectTransform>();
                var goals = side.Find("Goal progress paper").GetComponent<RectTransform>();
                Assert.That(paper.gameObject.activeInHierarchy && goals.gameObject.activeInHierarchy, Is.True);
                var storyCorners = new Vector3[4];
                var goalCorners = new Vector3[4];
                paper.GetWorldCorners(storyCorners);
                goals.GetWorldCorners(goalCorners);
                Assert.That(storyCorners.Min(point => side.InverseTransformPoint(point).y),
                    Is.GreaterThan(goalCorners.Max(point => side.InverseTransformPoint(point).y) + 12f));
                foreach (var language in new[] { InterfaceLanguage.English, InterfaceLanguage.Chinese })
                {
                    controller.UseLanguageForTesting(language);
                    controller.ToggleLanguage();
                    controller.ToggleLanguage();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (var name in new[] { "Story title", "Story paragraph", "Story trigger" })
                    {
                        var label = paper.Find(name).GetComponent<Text>();
                        Assert.That(label.text, Is.Not.Empty);
                        Assert.That(label.raycastTarget, Is.False);
                        Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f), name);
                    }
                }
                Assert.That(side.Find("Story cue backing/Today heading").GetComponent<Text>().text,
                    Is.EqualTo("街坊手记"));
                Assert.That(goals.Find("Goal progress heading").GetComponent<Text>().text,
                    Is.EqualTo("目标牌进度"));
                var capture = Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
                if (!string.IsNullOrEmpty(capture) && SystemInfo.graphicsDeviceType !=
                    UnityEngine.Rendering.GraphicsDeviceType.Null)
                    yield return Capture(root, capture);
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        private static IEnumerator Capture(GameObject root, string path)
        {
            var camera = root.GetComponentInChildren<ConstructionWorldView>(true).BoardCamera;
            var canvas = root.GetComponentInChildren<Canvas>();
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldDistance = canvas.planeDistance;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24);
            var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                shot.Apply();
                var module = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(Camera).Assembly.Location),
                    "UnityEngine.ImageConversionModule.dll"));
                var encode = module.GetType("UnityEngine.ImageConversion").GetMethod("EncodeToPNG", new[] { typeof(Texture2D) });
                File.WriteAllBytes(path, (byte[])encode.Invoke(null, new object[] { shot }));
                var detail = new Texture2D(480, 790, TextureFormat.RGB24, false);
                try
                {
                    detail.ReadPixels(new Rect(112, 195, 480, 790), 0, 0);
                    detail.Apply();
                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path), "street-journal-left.png"),
                        (byte[])encode.Invoke(null, new object[] { detail }));
                }
                finally { UnityEngine.Object.Destroy(detail); }
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(shot);
            }
        }
    }
}
