using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionPaletteGeometryTests
    {
        private static readonly ConstructionCategory[] Categories =
        {
            ConstructionCategory.Green, ConstructionCategory.Residence,
            ConstructionCategory.Restaurant, ConstructionCategory.Workshop,
            ConstructionCategory.Waste, ConstructionCategory.Street,
            ConstructionCategory.Supermarket, ConstructionCategory.Square
        };

        [UnityTest]
        public IEnumerator BuildPaletteKeepsEightReadableCardsOnOneGrid()
        {
            var root = new GameObject("Palette geometry test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            controller.UseLanguageForTesting(InterfaceLanguage.Chinese);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 8f;
            while ((root.transform.Find("Construction UI") == null ||
                    !controller.IsBoardCaseOpen) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            controller.ToggleBuildPalette();
            var canvas = root.transform.Find("Construction UI") as RectTransform;
            var choices = canvas.Find("Building choices");
            var backing = choices.Find("Build palette backing") as RectTransform;
            Assert.That(backing.gameObject.activeInHierarchy, Is.True);
            var notice = choices.Find("Build palette backing/Palette notice") as
                RectTransform;
            Assert.That(Bounds(notice, canvas).yMax,
                Is.LessThan(Bounds(backing, canvas).yMax - 100f),
                "The palette heading should sit below the painted top trim.");
            var hinges = canvas.GetComponentsInChildren<RectTransform>(true);

            for (var language = 0; language < 2; language++)
            {
                Canvas.ForceUpdateCanvases();
                var firstLeftX = float.NaN;
                var firstRightX = float.NaN;
                var previousY = float.NaN;
                for (var index = 0; index < Categories.Length; index++)
                {
                    var category = Categories[index];
                    var button = choices.Find($"Build {category}");
                    Assert.That(button, Is.Not.Null, category.ToString());
                    var buttonRect = button as RectTransform;
                    var trim = button.GetComponent<Image>();
                    var face = button.Find("Card face") as RectTransform;
                    Assert.That(trim.sprite, Is.Null,
                        $"{category} border should be solid, not padded artwork.");
                    Assert.That(trim.color.a, Is.GreaterThanOrEqualTo(0.95f));
                    Assert.That(face, Is.Not.Null,
                        $"{category} needs an explicit visible card face.");
                    var faceImage = face.GetComponent<Image>();
                    Assert.That(faceImage, Is.Not.Null);
                    Assert.That(faceImage.sprite, Is.Null,
                        $"{category} face should not have transparent art margins.");
                    Assert.That(faceImage.color.a, Is.GreaterThanOrEqualTo(0.95f));

                    var outer = Bounds(buttonRect, canvas);
                    var inner = Bounds(face, canvas);
                    foreach (var hinge in hinges)
                        if (hinge.name == "East case binding")
                            AssertNoOverlap(outer, Bounds(hinge, canvas),
                                $"{category} card / case hinge");
                    AssertInside(Bounds(backing, canvas), outer, 8f,
                        $"{category} palette backing");
                    AssertInside(outer, inner, 1f,
                        $"{category} face inside gold trim");
                    var icon = Bounds(button.Find("Building icon") as RectTransform,
                        canvas);
                    var titleRect = button.Find("Label") as RectTransform;
                    var title = titleRect.GetComponent<Text>();
                    var titleBounds = Bounds(titleRect, canvas);
                    var badge = Bounds(button.Find("Build limit badge") as
                        RectTransform, canvas);
                    var count = button.Find("Build limit badge/Built and limit")
                        .GetComponent<Text>();
                    var provisionRect = button.Find("Daily provision") as
                        RectTransform;
                    var provision = provisionRect.GetComponent<Text>();
                    var provisionBounds = Bounds(provisionRect, canvas);
                    AssertInside(inner, icon, 0f, $"{category} icon");
                    AssertInside(inner, titleBounds, 0f, $"{category} title");
                    AssertInside(inner, badge, 0f, $"{category} count badge");
                    AssertInside(inner, provisionBounds, 0f,
                        $"{category} daily provision");
                    var elements = new[] { icon, titleBounds, badge,
                        provisionBounds };
                    var names = new[] { "icon", "title", "count", "provision" };
                    for (var a = 0; a < elements.Length; a++)
                    for (var b = a + 1; b < elements.Length; b++)
                        AssertNoOverlap(elements[a], elements[b],
                            $"{category} {names[a]}/{names[b]}");
                    foreach (var label in new[] { title, count, provision })
                    {
                        if (!label.resizeTextForBestFit)
                            Assert.That(label.preferredWidth,
                                Is.LessThanOrEqualTo(
                                    label.rectTransform.rect.width + 1f),
                                $"{category} text is horizontally clipped: {label.text}");
                        Assert.That(label.cachedTextGenerator.lineCount,
                            Is.LessThanOrEqualTo(1),
                            $"{category} compact label wrapped: {label.text}");
                        Assert.That(label.preferredHeight,
                            Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                            $"{category} text is vertically clipped: {label.text}");
                    }

                    if (index % 2 == 0)
                    {
                        if (!float.IsNaN(firstLeftX))
                            Assert.That(buttonRect.anchoredPosition.x,
                                Is.EqualTo(firstLeftX).Within(0.5f),
                                $"{category} left-column alignment");
                        firstLeftX = buttonRect.anchoredPosition.x;
                        if (!float.IsNaN(previousY))
                            Assert.That(previousY - buttonRect.anchoredPosition.y,
                                Is.GreaterThanOrEqualTo(buttonRect.rect.height + 10f),
                                $"{category} row spacing");
                        previousY = buttonRect.anchoredPosition.y;
                    }
                    else
                    {
                        if (!float.IsNaN(firstRightX))
                            Assert.That(buttonRect.anchoredPosition.x,
                                Is.EqualTo(firstRightX).Within(0.5f),
                                $"{category} right-column alignment");
                        firstRightX = buttonRect.anchoredPosition.x;
                        Assert.That(buttonRect.anchoredPosition.y,
                            Is.EqualTo(previousY).Within(0.5f),
                            $"{category} row baseline");
                    }

                    var select = button.GetComponent<Button>();
                    var alpha = button.GetComponent<CanvasGroup>().alpha;
                    if (!select.interactable &&
                        select.transition == Selectable.Transition.ColorTint)
                        alpha *= select.colors.disabledColor.a;
                    Assert.That(alpha, Is.GreaterThanOrEqualTo(0.95f),
                        $"{category} is too faint to read in the introduction.");
                }
                Assert.That(firstRightX - firstLeftX,
                    Is.GreaterThan(150f), "Columns must have a clear gutter.");
                Assert.That(firstRightX - firstLeftX,
                    Is.LessThan(190f), "Columns must stay inside the leaf.");
                var build = choices.Find("Open build palette") as RectTransform;
                var nextDay = choices.Find("Advance to next day") as RectTransform;
                Assert.That(build.anchoredPosition.x,
                    Is.EqualTo(firstLeftX).Within(0.5f),
                    "The Build footer should be centered under the left column.");
                Assert.That(nextDay.anchoredPosition.x,
                    Is.EqualTo(firstRightX).Within(0.5f),
                    "Next day should be centered under the right column.");
                var bottomLeft = Bounds(choices.Find("Build Supermarket") as
                    RectTransform, canvas);
                var bottomRight = Bounds(choices.Find("Build Square") as
                    RectTransform, canvas);
                var buildFooter = Bounds(build, canvas);
                var nextFooter = Bounds(nextDay, canvas);
                Assert.That(bottomLeft.yMin - buildFooter.yMax,
                    Is.GreaterThanOrEqualTo(10f),
                    "Last row and Build footer need breathing room.");
                Assert.That(bottomRight.yMin - nextFooter.yMax,
                    Is.GreaterThanOrEqualTo(10f),
                    "Last row and Next day footer need breathing room.");
                if (language == 0) controller.ToggleLanguage();
            }

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResidenceConditionsHaveOneHeadingAndClearVerticalRows()
        {
            var root = new GameObject("Residence condition geometry test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(
                ConstructionRunModel.RecommendedAnimalDeathLimit,
                ConstructionRunModel.RecommendedUnemployedResidentLimit), Is.True);
            controller.UseRunForTesting(run);
            yield return null;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (!controller.IsBoardCaseOpen &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.IsBoardCaseOpen, Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            controller.ToggleBuildPalette();
            Canvas.ForceUpdateCanvases();

            var canvas = root.transform.Find("Construction UI") as RectTransform;
            var choices = canvas.Find("Building choices");
            var backing = choices.Find("Build palette backing") as RectTransform;
            var notice = backing.Find("Palette notice");
            var heading = choices.Find("Residence condition heading") as RectTransform;
            Assert.That(heading.gameObject.activeInHierarchy, Is.True);
            Assert.That(notice.gameObject.activeInHierarchy, Is.False,
                "The normal palette heading must not compete with the condition guide.");
            var headingBounds = Bounds(heading, canvas);
            Assert.That(headingBounds.yMax,
                Is.LessThan(Bounds(backing, canvas).yMax - 100f),
                "The home heading is on the painted top trim.");
            var firstCard = Bounds(choices.Find("Build Green") as RectTransform,
                canvas);
            foreach (var kind in new[] { ResidenceKind.Standard,
                         ResidenceKind.Garden, ResidenceKind.Urban })
            {
                var option = choices.Find($"Residence condition {kind}") as
                    RectTransform;
                Assert.That(option, Is.Not.Null, $"Missing {kind} condition card.");
                Assert.That(option.gameObject.activeInHierarchy, Is.True);
                Assert.That(option.GetComponent<Button>(), Is.Null,
                    "A scoring condition must not be a prebuild choice.");
                var bounds = Bounds(option, canvas);
                Assert.That(headingBounds.yMin - bounds.yMax,
                    Is.GreaterThanOrEqualTo(8f),
                    $"{kind} overlaps the condition heading.");
                Assert.That(bounds.yMin - firstCard.yMax,
                    Is.GreaterThanOrEqualTo(6f),
                    $"{kind} overlaps the category cards.");
            }

            Object.Destroy(root);
            yield return null;
        }

        private static Rect Bounds(RectTransform rect, RectTransform root)
        {
            Assert.That(rect, Is.Not.Null);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var low = root.InverseTransformPoint(corners[0]);
            var high = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(low.x, high.x),
                Mathf.Min(low.y, high.y), Mathf.Max(low.x, high.x),
                Mathf.Max(low.y, high.y));
        }

        private static void AssertInside(Rect outer, Rect inner, float margin,
            string label)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin + margin - 1f),
                $"{label} escapes left edge");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax - margin + 1f),
                $"{label} escapes right edge");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin + margin - 1f),
                $"{label} escapes bottom edge");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax - margin + 1f),
                $"{label} escapes top edge");
        }

        private static void AssertNoOverlap(Rect a, Rect b, string label)
        {
            var width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            var height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            Assert.That(width <= 1f || height <= 1f, Is.True,
                $"{label} overlaps by {width:0.#}×{height:0.#}.");
        }
    }
}
