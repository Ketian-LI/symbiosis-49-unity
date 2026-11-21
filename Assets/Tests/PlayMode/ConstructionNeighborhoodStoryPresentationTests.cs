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
                        {
                            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f), label.name);
                            AssertSdfFits(label);
                        }
                    foreach (var label in root.GetComponentsInChildren<Text>())
                        if (label.name is "Hand room name" or "Draw pile title" or "Remaining cards on draw pile")
                            AssertSdfFits(label);
                    AssertSdfFits(root.GetComponentsInChildren<Button>().First(button =>
                        button.name == "Reroll selected hand card").GetComponentInChildren<Text>());
                    var tagline = side.Find("Next step backing/Selected hand tagline").GetComponent<Text>();
                    Assert.That(tagline.preferredHeight, Is.LessThanOrEqualTo(tagline.rectTransform.rect.height + 1f));
                    foreach (var label in side.GetComponentsInChildren<Text>())
                        if (label.name.StartsWith("Score component") || label.name == "Live award distance" ||
                            label.name.StartsWith("Observed change") || label.name == "Recent changes heading")
                        {
                            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f), label.name);
                            AssertSdfFits(label);
                        }
                    Assert.That(side.Find("Board progress backing/Balance score").gameObject.activeSelf, Is.False);
                    var award = side.Find("Board progress backing/Live award distance").GetComponent<RectTransform>();
                    var selectedHeading = side.Find("Next step backing/Next step heading").GetComponent<RectTransform>();
                    var awardCorners = new Vector3[4]; var headingCorners = new Vector3[4];
                    award.GetWorldCorners(awardCorners); selectedHeading.GetWorldCorners(headingCorners);
                    Assert.That(awardCorners.Min(point => point.y), Is.GreaterThan(headingCorners.Max(point => point.y)));
                    var details = side.Find("Recent observed changes").GetComponent<RectTransform>();
                    var body = tagline.rectTransform;
                    var bodyCorners = new Vector3[4];
                    var detailsCorners = new Vector3[4];
                    body.GetWorldCorners(bodyCorners); details.GetWorldCorners(detailsCorners);
                    Assert.That(detailsCorners.Max(point => point.y), Is.LessThan(bodyCorners.Min(point => point.y)));
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
                    typeof(ConstructionPrototypeController).GetMethod("ToggleQuickGuide", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(controller, null);
                    Assert.That(run.TryBuild(2, 2, ConstructionCategory.Residence, GreenPlanting.None,
                        out _, out var houseFailure, out var housePlacement), Is.True, houseFailure + " / " + housePlacement);
                    Assert.That(run.TrySimulateFillDay(out _), Is.True);
                    Assert.That(run.TryBuild(2, 1, ConstructionCategory.Waste, GreenPlanting.None,
                        out _, out var wasteFailure, out var wastePlacement), Is.True, wasteFailure + " / " + wastePlacement);
                    Assert.That(run.TrySimulateFillDay(out _), Is.True);
                    foreach (var language in new[] { InterfaceLanguage.English, InterfaceLanguage.Chinese })
                    {
                        controller.UseLanguageForTesting(language);
                        // Exercise the actual language-change path so legacy
                        // labels and their SDF bridge receive the right font.
                        controller.ToggleLanguage();
                        controller.ToggleLanguage();
                        root.GetComponentsInChildren<Button>().First(button => button.name == "Fill hand card 1")
                            .onClick.Invoke();
                        yield return null;
                        yield return null;
                        var reason = side.GetComponentsInChildren<Text>().First(label =>
                            label.name.StartsWith("Observed change") && label.text.Contains("-80"));
                        Assert.That(reason.gameObject.activeInHierarchy, Is.True);
                        AssertSdfFits(reason);
                        Assert.That(reason.color.r, Is.GreaterThan(reason.color.g));
                        yield return Capture(root, Path.Combine(Path.GetDirectoryName(capture),
                            language == InterfaceLanguage.Chinese ? "penalty-readable-zh.png" : "penalty-readable-en.png"));
                    }
                }
                // Inspect every room's real SDF text at the existing card-detail
                // width; this is a typography fixture, not a claim that all room
                // categories are simultaneously in the player's current hand.
                var categoryField = typeof(ConstructionPrototypeController).GetField("selectedCategory", BindingFlags.Instance | BindingFlags.NonPublic);
                var refreshCard = typeof(ConstructionPrototypeController).GetMethod("RefreshFillRightHandPreview", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var language in new[] { InterfaceLanguage.English, InterfaceLanguage.Chinese })
                {
                    controller.UseLanguageForTesting(language);
                    controller.ToggleLanguage(); controller.ToggleLanguage();
                    foreach (ConstructionCategory category in Enum.GetValues(typeof(ConstructionCategory)))
                    {
                        categoryField.SetValue(controller, category);
                        refreshCard.Invoke(controller, null);
                        yield return null;
                        yield return null;
                        foreach (var name in new[] { "Selected hand name", "Selected hand valid plots", "Selected hand tagline" })
                            AssertSdfFits(side.Find("Next step backing/" + name).GetComponent<Text>());
                    }
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
            Assert.That(sdf.isTextTruncated, Is.False, source.name + " truncates: " + source.text);
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
            var oldSize = camera.orthographicSize;
            var oldPosition = camera.transform.position;
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
                var board = root.transform.Find("Construction UI/7 by 7 construction board")
                    .GetComponent<RectTransform>();
                var centre = (Vector2)RectTransformUtility.WorldToScreenPoint(camera, board.position);
                var across = (Vector2)RectTransformUtility.WorldToScreenPoint(camera,
                    board.TransformPoint(new Vector3(101f, 0f, 0f)));
                var pixelPitch = Vector2.Distance(centre, across);
                camera.orthographicSize = WorldScaleStandards.CellSizeMeters * target.height / (2f * pixelPitch);
                var size = camera.orthographicSize;
                camera.transform.position = new Vector3((0.5f - centre.x / target.width) * 2f * size * camera.aspect,
                    42f, (0.5f - centre.y / target.height) * 2f * size);
                Canvas.ForceUpdateCanvases();
                var projected = camera.WorldToScreenPoint(Vector3.zero);
                Assert.That(projected.x, Is.EqualTo(centre.x).Within(2f));
                Assert.That(projected.y, Is.EqualTo(centre.y).Within(2f));
                var worldPitch = camera.WorldToScreenPoint(new Vector3(WorldScaleStandards.CellSizeMeters, 0f, 0f));
                Assert.That(worldPitch.x - projected.x, Is.EqualTo(pixelPitch).Within(2f));
                typeof(ConstructionPrototypeController).GetMethod("DrawScoreTrace", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(root.GetComponent<ConstructionPrototypeController>(), null);
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
                camera.orthographicSize = oldSize;
                camera.transform.position = oldPosition;
                RenderTexture.active = oldActive;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(shot);
            }
        }
    }
}
