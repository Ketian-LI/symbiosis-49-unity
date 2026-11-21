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
            var initial = run.Export();
            initial.runId = "00000000-0000-0000-0000-000000000001";
            initial.fillHandRandomState = 73856093;
            initial.specialTasks = null;
            Assert.That(ConstructionRunModel.TryRestore(initial, out run), Is.True);
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
                // This is a fixed rendering fixture, not a random-hand test.
                if (run.BuiltCount == 8 && !run.FillHandCards.Contains(ConstructionCategory.Green))
                {
                    var fixture = run.Export();
                    fixture.fillHandCards[0] = ConstructionCategory.Green;
                    Assert.That(ConstructionRunModel.TryRestore(fixture, out run), Is.True);
                }
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
                root.GetComponentsInChildren<Button>().First(button => button.name == "Fill hand card 3")
                    .onClick.Invoke();
                yield return null;
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
                    var earlyCapture = Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
                    if (!string.IsNullOrEmpty(earlyCapture) && SystemInfo.graphicsDeviceType !=
                        UnityEngine.Rendering.GraphicsDeviceType.Null)
                        yield return Capture(root, Path.Combine(Path.GetDirectoryName(earlyCapture),
                            language == InterfaceLanguage.Chinese ? "game-readable-zh.png" : "game-readable-en.png"));
                    foreach (var name in new[] { "Story date", "Story title", "Story paragraph", "Story trigger", "Previous story" })
                    {
                        var label = paper.Find(name).GetComponent<Text>();
                        Assert.That(label.text, Is.Not.Empty);
                        Assert.That(label.raycastTarget, Is.False);
                        Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f), name);
                        AssertSdfFits(label);
                    }
                    foreach (var label in goals.GetComponentsInChildren<Text>())
                        if (label.gameObject.activeInHierarchy && label.text.Length > 0)
                            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f), label.name);
                    foreach (var label in root.GetComponentsInChildren<Text>())
                        if (label.name is "Hand room name" or "Draw pile title" or "Remaining cards on draw pile")
                            AssertSdfFits(label);
                    AssertSdfFits(root.GetComponentsInChildren<Button>().First(button =>
                        button.name == "Reroll selected hand card").GetComponentInChildren<Text>());
                    var tagline = side.Find("Next step backing/Selected hand tagline").GetComponent<Text>();
                    Assert.That(tagline.preferredHeight, Is.LessThanOrEqualTo(tagline.rectTransform.rect.height + 1f));
                }
                Assert.That(side.Find("Story cue backing/Today heading").GetComponent<Text>().text,
                    Is.EqualTo("街坊手记"));
                Assert.That(goals.Find("Goal progress heading").GetComponent<Text>().text,
                    Is.EqualTo("目标牌进度"));
                var capture = Environment.GetEnvironmentVariable("SYMBIOSIS49_STORY_CAPTURE_PATH");
                if (!string.IsNullOrEmpty(capture) && SystemInfo.graphicsDeviceType !=
                    UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    yield return Capture(root, capture);
                    typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(controller, null);
                    yield return null;
                    yield return Capture(root, Path.Combine(Path.GetDirectoryName(capture), "manual-readable-zh.png"));
                    controller.UseLanguageForTesting(InterfaceLanguage.English);
                    controller.ToggleLanguage();
                    controller.ToggleLanguage();
                    yield return null;
                    Assert.That(root.GetComponentsInChildren<Text>().First(label => label.name == "Manual heading").text,
                        Is.EqualTo("GAME MANUAL"));
                    yield return Capture(root, Path.Combine(Path.GetDirectoryName(capture), "manual-readable-en.png"));
                }
            }
            finally { UnityEngine.Object.Destroy(root); }
            yield return null;
        }

        private static void AssertSdfFits(Text source)
        {
            var sdf = source.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (sdf == null || !sdf.gameObject.activeInHierarchy) return;
            sdf.ForceMeshUpdate();
            Assert.That(sdf.preferredHeight, Is.LessThanOrEqualTo(source.rectTransform.rect.height + 1f),
                source.name + " SDF: " + source.text);
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
                    File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-left.png"),
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
