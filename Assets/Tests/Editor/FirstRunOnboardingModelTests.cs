using NUnit.Framework;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class FirstRunOnboardingModelTests
    {
        [Test]
        public void ResidentSpotlightIsClickableOnlyDuringResidentPractice()
        {
            var hud = new GameObject("Tutorial HUD");
            var cameraObject = new GameObject("Tutorial Camera");
            var resident = new GameObject("Highlighted Resident");
            try
            {
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 32f, 0f), Quaternion.Euler(90f, 0f, 0f));
                var camera = cameraObject.AddComponent<Camera>();
                resident.transform.position = Vector3.zero;
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), camera, HideFlags.None);
                var spotlight = hud.transform.Find("First Run Onboarding/Tutorial Spotlight");
                var image = spotlight.GetComponent<Image>();
                var button = spotlight.GetComponent<Button>();
                var selected = 0;
                overlay.HighlightedResidentRequested += () => selected++;

                overlay.Show(OnboardingStep.SelectResident, resident.transform, true);
                Assert.That(spotlight.gameObject.activeSelf, Is.True);
                Assert.That(image.raycastTarget, Is.True);
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                Assert.That(selected, Is.EqualTo(1));

                overlay.Show(OnboardingStep.InspectWaste, resident.transform, true);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(button.interactable, Is.False);
                overlay.ShowGuide(0, GameplayGuideCatalog.PageCount,
                    GameplayGuideCatalog.GetPage(0, true), resident.transform, true);
                Assert.That(image.raycastTarget, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(resident);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void TutorialControlsAreRoundedWithoutChangingTheirProportions()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                var card = hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card");
                foreach (var name in new[] { "Continue Tutorial", "Previous Guide Page", "Skip" })
                {
                    var control = card.Find(name);
                    var rounded = control.GetComponent<RoundedPanelGraphic>();
                    Assert.That(rounded, Is.Not.Null, name);
                    Assert.That(control.GetComponent<Button>().targetGraphic, Is.SameAs(rounded), name);
                    Assert.That(control.GetComponent<RectTransform>().sizeDelta,
                        Is.EqualTo(new Vector2(92f, 36f)), name);
                }

                for (var index = 1; index <= 4; index++)
                {
                    var segment = card.Find($"Progress {index}");
                    Assert.That(segment.GetComponent<RoundedPanelGraphic>(), Is.Not.Null);
                    Assert.That(segment.GetComponent<RectTransform>().sizeDelta,
                        Is.EqualTo(new Vector2(34f, 5f)));
                }
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void GameplaySolidPanelsHaveRoundedFillAndPreservePanelSize()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var rects = generated.GetComponentsInChildren<RectTransform>(true);
                foreach (var name in new[] { "Resident Count Badge", "Temporary Tray Icon Slot" })
                {
                    var panel = rects.Single(rect => rect.name == name);
                    var backing = panel.Find("Rounded Backing");
                    Assert.That(backing.GetComponent<RoundedPanelGraphic>(), Is.Not.Null, name);
                    Assert.That(backing.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero), name);
                    Assert.That(backing.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one), name);
                }

                var badge = rects.Single(rect => rect.name == "Resident Count Badge");
                Assert.That(badge.sizeDelta, Is.EqualTo(new Vector2(42f, 21f)));
                var tray = rects.Single(rect => rect.name == "Temporary Tray Icon Slot");
                Assert.That(tray.sizeDelta, Is.EqualTo(new Vector2(54f, 54f)));
                var tooltips = rects.Where(rect => rect.name == "Hover Detail").ToArray();
                Assert.That(tooltips.Length, Is.GreaterThan(0));
                foreach (var tooltip in tooltips)
                {
                    var expectedSize = tooltip.transform.parent.name switch
                    {
                        "Animal Death Limit" => new Vector2(410f, 184f),
                        "Animals Fed Today" => new Vector2(420f, 120f),
                        _ => new Vector2(310f, 76f)
                    };
                    Assert.That(tooltip.sizeDelta, Is.EqualTo(expectedSize));
                    Assert.That(tooltip.Find("Rounded Backing")?.GetComponent<RoundedPanelGraphic>(),
                        Is.Not.Null);
                }
                var speciesTooltips = rects.Where(rect => rect.name == "Death Causes").ToArray();
                Assert.That(speciesTooltips.Length, Is.EqualTo(4));
                Assert.That(speciesTooltips.All(rect => rect.sizeDelta == new Vector2(310f, 92f)), Is.True);
                var population = rects.Single(rect => rect.name == "Animal Population");
                var nightReport = rects.Single(rect => rect.name == "Hedgehog Night Report");
                var nightReportTop = nightReport.anchoredPosition.y + nightReport.rect.height;
                foreach (var tooltip in speciesTooltips)
                {
                    var tooltipBottom = population.anchoredPosition.y + population.rect.height +
                                        tooltip.anchoredPosition.y;
                    Assert.That(tooltipBottom, Is.GreaterThanOrEqualTo(nightReportTop + 8f),
                        $"{tooltip.transform.parent.name} details must not overlap the night report.");
                    var tooltipLeft = population.anchoredPosition.x +
                                      ((RectTransform)tooltip.transform.parent).anchoredPosition.x +
                                      tooltip.anchoredPosition.x;
                    Assert.That(tooltipLeft, Is.EqualTo(nightReport.anchoredPosition.x),
                        $"{tooltip.transform.parent.name} details should stay in the left HUD column.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void EveryRightSideIndicatorHasAHoverTargetAndNamedTooltip()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);

                foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
                {
                    var holder = hud.GetEcologicalIndicatorRect(kind);
                    var chineseName = kind switch
                    {
                        EcologicalMetricKind.HumanFunction => "居民",
                        EcologicalMetricKind.FoodAccessibility => "今日已进食",
                        EcologicalMetricKind.HabitatProvision => "庇护指数",
                        _ => "存活动物"
                    };
                    var englishName = kind switch
                    {
                        EcologicalMetricKind.HumanFunction => "Residents",
                        EcologicalMetricKind.FoodAccessibility => "Fed today",
                        EcologicalMetricKind.HabitatProvision => "Shelter index",
                        _ => "Living animals"
                    };
                    AssertHoverTooltip(holder, kind.ToString(), chineseName, englishName);
                }
                AssertHoverTooltip(hud.AnimalDeathIndicatorRect, "Animal death limit",
                    "累计死亡", "Cumulative deaths");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertHoverTooltip(
            RectTransform holder, string indicatorName, string chineseName, string englishName)
        {
            Assert.That(holder, Is.Not.Null, indicatorName);
            var hitArea = holder.Find("Hover Hit Area")?.GetComponent<Image>();
            Assert.That(hitArea, Is.Not.Null, indicatorName);
            Assert.That(hitArea.raycastTarget, Is.True, indicatorName);
            var detail = holder.Find("Hover Detail");
            Assert.That(detail, Is.Not.Null, indicatorName);
            Assert.That(detail.GetComponent<Image>().raycastTarget, Is.False, indicatorName);
            var label = detail.GetComponentInChildren<Text>(true);
            Assert.That(label, Is.Not.Null, indicatorName);
            Assert.That(label.text.Contains(chineseName) ||
                        label.text.Contains(englishName), Is.True, indicatorName);
            var hover = holder.GetComponent<EcologicalIndicatorHover>();
            Assert.That(hover, Is.Not.Null, indicatorName);
            Assert.That(detail.gameObject.activeSelf, Is.False, indicatorName);
            hover.OnPointerEnter(null);
            Assert.That(detail.gameObject.activeSelf, Is.True, indicatorName);
            hover.OnPointerExit(null);
            Assert.That(detail.gameObject.activeSelf, Is.False, indicatorName);
        }

        [Test]
        public void RightSideBadgesShowCountsInsteadOfRiskScores()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);

                var residents = hud.GetEcologicalIndicatorRect(EcologicalMetricKind.HumanFunction);
                var food = hud.GetEcologicalIndicatorRect(EcologicalMetricKind.FoodAccessibility);
                var animals = hud.GetEcologicalIndicatorRect(EcologicalMetricKind.AnimalSafety);
                Assert.That(residents.Find("Resident Count Badge").GetComponentInChildren<Text>().text,
                    Is.EqualTo("4/8"));
                Assert.That(food.Find("Value Badge").GetComponentInChildren<Text>().text,
                    Is.EqualTo($"0/{AnimalPopulationDefaults.Total}"));
                Assert.That(animals.Find("Value Badge").GetComponentInChildren<Text>().text,
                    Is.EqualTo($"{AnimalPopulationDefaults.Total}/{AnimalPopulationDefaults.Total}"));
                Assert.That(hud.AnimalDeathIndicatorRect.Find("Death Count Badge")
                    .GetComponentInChildren<Text>().text, Is.EqualTo("0/3"));

                Canvas.ForceUpdateCanvases();
                foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
                {
                    var tooltip = hud.GetEcologicalIndicatorRect(kind)
                        .Find("Hover Detail").GetComponentInChildren<Text>(true);
                    Assert.That(tooltip.preferredHeight,
                        Is.LessThanOrEqualTo(tooltip.rectTransform.rect.height + 2f),
                        $"{kind} tooltip is clipped");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void WorkforceCounterAndSpeedControlsUseEqualRoundedSegments()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
                var counter = hud.WorkforceCounterRect;
                Assert.That(counter, Is.Not.Null);
                Assert.That(counter.GetComponentInChildren<Text>().text,
                    Does.Contain("4/4"));
                Assert.That(counter.GetComponent<Image>().raycastTarget, Is.False);

                var speedRoot = generated.GetComponentsInChildren<RectTransform>(true)
                    .Single(rect => rect.name == "Simulation Speed");
                Assert.That(speedRoot.Find("Speed Selection Rail/Rounded Backing")
                    ?.GetComponent<RoundedPanelGraphic>(), Is.Not.Null);
                foreach (var (speed, x, label) in new[]
                         {
                             (0, -81f, "Ⅱ"), (1, -27f, "1×"),
                             (2, 27f, "2×"), (4, 81f, "4×")
                         })
                {
                    var button = speedRoot.Find($"Speed {speed}") as RectTransform;
                    Assert.That(button, Is.Not.Null, speed.ToString());
                    Assert.That(button.sizeDelta, Is.EqualTo(new Vector2(50f, 34f)));
                    Assert.That(button.anchoredPosition.x, Is.EqualTo(x));
                    Assert.That(button.Find("Rounded Speed Segment")
                        ?.GetComponent<RoundedPanelGraphic>(), Is.Not.Null);
                    Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo(label));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void SelectedRoomChipsExplainTypeFunctionAndUseOnHover()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
                var context = generated.GetComponentsInChildren<RectTransform>(true)
                    .Single(rect => rect.name == "Selected Room Context");
                hud.ShowRoom(RoomLayoutData.All.Single(room => room.Id == "canteen-a"),
                    bootstrap.transform);
                foreach (var (chipName, detailName, keyword) in new[]
                         {
                             ("Room Type Chip", "Room Type Detail", "餐饮商铺"),
                             ("Current Function Chip", "Current Function Detail", "食物"),
                             ("Current Use Chip", "Current Use Detail", "居民")
                         })
                {
                    var chip = context.Find(chipName);
                    var detail = context.Find(detailName);
                    Assert.That(chip, Is.Not.Null, chipName);
                    Assert.That(chip.GetComponent<Image>().raycastTarget, Is.True, chipName);
                    Assert.That(detail, Is.Not.Null, detailName);
                    Assert.That(detail.GetComponent<Image>().raycastTarget, Is.False, detailName);
                    var text = detail.GetComponentInChildren<Text>(true).text;
                    Assert.That(text, Does.Contain(keyword).Or.Contain(
                        chipName == "Room Type Chip" ? "Food Shop" :
                        chipName == "Current Function Chip" ? "Food source" : "Resident"));
                    var hover = chip.GetComponent<EcologicalIndicatorHover>();
                    Assert.That(detail.gameObject.activeSelf, Is.False);
                    hover.OnPointerEnter(null);
                    Assert.That(detail.gameObject.activeSelf, Is.True);
                    hover.OnPointerExit(null);
                    Assert.That(detail.gameObject.activeSelf, Is.False);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GuideCoversEveryRoomAndEachGameplayArea()
        {
            var pages = Enumerable.Range(0, GameplayGuideCatalog.PageCount)
                .Select(index => GameplayGuideCatalog.GetPage(index, true))
                .ToArray();
            Assert.That(pages.Length, Is.EqualTo(23));
            Assert.That(pages[0].Body, Does.Contain("7×7"));
            Assert.That(pages[1].Body, Does.Contain("上班"));
            Assert.That(pages[2].Body, Does.Contain("右侧五个圆环"));
            Assert.That(pages[8].Body, Does.Contain("投喂"));
            foreach (var kind in System.Enum.GetValues(typeof(EcologicalMetricKind)).Cast<EcologicalMetricKind>())
            {
                Assert.That(pages.Count(page => page.MetricKind == kind), Is.EqualTo(1), kind.ToString());
            }
            Assert.That(pages.Count(page => page.DeathLimit), Is.EqualTo(1));
            Assert.That(pages[3].Body, Does.Contain("4/8"));
            Assert.That(pages[4].Body, Does.Contain("食物份数"));
            Assert.That(pages[5].Body, Does.Contain("成熟橡树"));
            Assert.That(pages[6].Body, Does.Contain("存活动物"));
            Assert.That(pages[7].Body, Does.Contain("累计 3 次死亡"));
            foreach (var type in System.Enum.GetValues(typeof(RoomType)).Cast<RoomType>())
            {
                var page = pages.Single(candidate => candidate.RoomType == type);
                if (type == RoomType.SharedSpace)
                {
                    Assert.That(page.Body, Does.Contain("12"));
                    Assert.That(page.Body, Does.Contain("动物专用通道"));
                    continue;
                }
                foreach (var room in RoomLayoutData.All.Where(room => room.Type == type))
                {
                    Assert.That(page.Body, Does.Contain(room.DisplayName), room.Id);
                }
            }
        }

        [Test]
        public void GuideCanNavigateBackAndReturnToPracticeLayout()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                overlay.ShowGuide(0, GameplayGuideCatalog.PageCount,
                    GameplayGuideCatalog.GetPage(0, true), null, true);
                Assert.That(overlay.CurrentHeader, Does.Contain("1/23"));
                Assert.That(overlay.CurrentInstruction, Does.Contain("49 间单格房"));
                Assert.That(hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Previous Guide Page")
                    .gameObject.activeSelf, Is.False);

                overlay.ShowGuide(22, GameplayGuideCatalog.PageCount,
                    GameplayGuideCatalog.GetPage(22, true), null, true);
                Assert.That(overlay.CurrentHeader, Does.Contain("23/23"));
                Assert.That(hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Previous Guide Page")
                    .gameObject.activeSelf, Is.True);

                overlay.Show(OnboardingStep.SelectResident, null, true);
                Assert.That(overlay.CurrentHeader, Does.Contain("1/4"));
                Assert.That(hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Previous Guide Page")
                    .gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void MainSceneHasAHighlightTargetForEveryIndicatorPage()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var hud = generated.GetComponentsInChildren<UrbanWildlifeHud>(true).Single();
                foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
                {
                    Assert.That(hud.GetEcologicalIndicatorRect(kind), Is.Not.Null, kind.ToString());
                }
                Assert.That(hud.AnimalDeathIndicatorRect, Is.Not.Null);
                Assert.That(hud.ClockDialRect, Is.Not.Null);
                Assert.That(hud.WorkforceCounterRect, Is.Not.Null);
                Assert.That(hud.LayoutEditButtonRect, Is.Not.Null);
                var moveButton = hud.LayoutEditButtonRect;
                Assert.That(moveButton.rect.width, Is.GreaterThanOrEqualTo(140f));
                var shovel = moveButton.GetComponentInChildren<RoomMoveIconGraphic>(true);
                Assert.That(shovel, Is.Not.Null,
                    "The layout action should show a dedicated shovel-and-room mark.");
                Assert.That(shovel.rectTransform.rect.width, Is.GreaterThanOrEqualTo(56f));
                var feeding = hud.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .Single(button => button.name == "Activate Feeding Mode")
                    .GetComponent<RectTransform>();
                Assert.That(moveButton.anchoredPosition.x + moveButton.rect.width,
                    Is.LessThan(feeding.anchoredPosition.x),
                    "The enlarged move action must not cover the feeding action.");
                Assert.That(feeding.rect.size, Is.EqualTo(moveButton.rect.size),
                    "Planning and feeding should use the same action-button footprint.");
                Assert.That(feeding.anchoredPosition.y, Is.EqualTo(moveButton.anchoredPosition.y));
                var feedingButton = feeding.GetComponent<UnityEngine.UI.Button>();
                var feedingBacking = feeding.GetComponentInChildren<RoundedPanelGraphic>(true);
                Assert.That(feedingBacking, Is.Not.Null);
                Assert.That(feedingButton.targetGraphic, Is.SameAs(feedingBacking));
                Assert.That(feedingBacking.CornerRadius, Is.EqualTo(
                    moveButton.GetComponentInChildren<RoundedPanelGraphic>(true).CornerRadius));
                var foodDish = feeding.GetComponentInChildren<FeedingModeIconGraphic>(true);
                Assert.That(foodDish, Is.Not.Null,
                    "Feeding should use a matching ivory-and-teal UI pictogram.");
                Assert.That(foodDish.rectTransform.rect.size, Is.EqualTo(shovel.rectTransform.rect.size));
                Assert.That(foodDish.raycastTarget, Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GuideTextFitsItsCardInBothLanguages()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                var body = hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Instruction")
                    .GetComponent<Text>();
                foreach (var chinese in new[] { true, false })
                {
                    for (var index = 0; index < GameplayGuideCatalog.PageCount; index++)
                    {
                        overlay.ShowGuide(index, GameplayGuideCatalog.PageCount,
                            GameplayGuideCatalog.GetPage(index, chinese), null, chinese);
                        Assert.That(body.preferredHeight, Is.LessThanOrEqualTo(body.rectTransform.rect.height + 2f),
                            $"Page {index + 1}, Chinese={chinese}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void GeneratedTutorialPanelIsUsedBehindLiveText()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                var card = hud.transform.Find("First Run Onboarding/One Sentence Tutorial Card");
                Assert.That(card.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(card.GetComponent<Image>().sprite.name, Is.EqualTo("Tutorial Panel V2"));
                Assert.That(card.GetComponent<Image>().type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(card.GetComponent<Image>().color.a, Is.LessThan(1f));

                overlay.ShowGuide(3, GameplayGuideCatalog.PageCount,
                    GameplayGuideCatalog.GetPage(3, true), null, true);
                Assert.That(overlay.CurrentInstruction, Does.Contain("居民人数"));
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void FourConfirmedInteractionsCompleteTutorialInOrder()
        {
            var model = new FirstRunOnboardingModel();
            model.Begin();
            Assert.That(model.CompleteStep(OnboardingStep.InspectWaste), Is.False);
            Assert.That(model.CompleteStep(OnboardingStep.SelectResident), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.InspectWaste), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.PracticeLayout), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.PlaceFood), Is.True);
            Assert.That(model.Step, Is.EqualTo(OnboardingStep.Complete));
        }

        [Test]
        public void SkipCompletesFromAnyActiveStep()
        {
            var model = new FirstRunOnboardingModel();
            model.Begin();
            model.Skip();
            Assert.That(model.IsActive, Is.False);
        }

        [Test]
        public void PauseSuppressionHidesAndThenRestoresTheCurrentTutorialStep()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                overlay.Show(OnboardingStep.SelectResident, null, true);
                Assert.That(overlay.IsVisible, Is.True);

                overlay.SetSuppressed(true);
                Assert.That(overlay.IsVisible, Is.False);

                overlay.Show(OnboardingStep.InspectWaste, null, true);
                Assert.That(overlay.IsVisible, Is.False);

                overlay.SetSuppressed(false);
                Assert.That(overlay.IsVisible, Is.True);

                overlay.Show(OnboardingStep.Hidden, null, true);
                Assert.That(overlay.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void WasteReviewRequiresContinueAndUpdatesProgressText()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                overlay.Show(OnboardingStep.InspectWaste, null, true,
                    OnboardingPromptPhase.WasteReviewed, null,
                    "垃圾房：3/8；下次清运在第 2 天 04:00。看完点“继续”。");

                Assert.That(overlay.CurrentHeader, Does.Contain("2/4"));
                Assert.That(overlay.CurrentInstruction, Does.Contain("3/8"));
                var next = overlay.transform.Find("First Run Onboarding/One Sentence Tutorial Card/Continue Tutorial");
                Assert.That(next, Is.Not.Null);
                Assert.That(next.gameObject.activeSelf, Is.True);

                overlay.Show(OnboardingStep.PracticeLayout, null, true,
                    OnboardingPromptPhase.RoomInTray);
                Assert.That(overlay.CurrentHeader, Does.Contain("3/4"));
                Assert.That(overlay.CurrentInstruction, Does.Contain("拖回"));
                Assert.That(next.gameObject.activeSelf, Is.False);

                overlay.Show(OnboardingStep.PracticeLayout, null, true,
                    OnboardingPromptPhase.LayoutRetry);
                Assert.That(overlay.CurrentInstruction, Does.Contain("重试"));
                Assert.That(next.gameObject.activeSelf, Is.True);

                overlay.Show(OnboardingStep.PlaceFood, null, true,
                    OnboardingPromptPhase.ReplayFinish);
                Assert.That(overlay.CurrentHeader, Does.Contain("4/4"));
                Assert.That(overlay.CurrentInstruction, Does.Contain("每 3 天"));
                Assert.That(next.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }
    }
}
