using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        // The digital Fill hand sits on the photographed tabletop, just below
        // the board. It deliberately remains separate from the physical-board
        // inventory and from the Endless build palette.
        private RectTransform fillHandRoot;
        private Text fillHandNote;
        private Text fillHandDeck;
        private Text fillHandDeckTitle;
        private Button fillHandReroll;
        private RectTransform fillHandNoteShadowRect;
        private RectTransform fillHandNoteRect;
        private RectTransform fillHandDeckShadowRect;
        private RectTransform fillHandDeckBackRect;
        private RectTransform fillHandDeckRect;
        private RectTransform fillHandDeckSecondBackRect;
        private RectTransform fillHandDeckCountTab;
        private Image fillHandWood;
        private Image fillHandNoteShadow;
        private Image fillHandDeckShadow;
        private Image[] fillHandShadowImages = new Image[5];
        private readonly Vector2[] fillHandTargetPositions = new Vector2[5];
        private readonly float[] fillHandTargetAngles = new float[5];
        private readonly float[] fillHandTargetScales = new float[5];
        private readonly float[] fillHandDealTimes = new float[5];
        private readonly bool[] fillHandDealing = new bool[5];
        private readonly bool[] fillHandCategoryKnown = new bool[5];
        private readonly ConstructionCategory[] fillHandPreviousCategories =
            new ConstructionCategory[5];
        private float fillHandRerollPulseTime = -10f;
        private static Sprite fillHandSoftShadowSprite;
        private const float FillHandDeckX = 250f;
        private float fillHandDeckBaseScale = 1f;
        // Sampled from the approved tabletop reference, then rounded to stable
        // game colors. Wood and paper retain their own photographed texture.
        private static readonly Color HandPaper = HexHandColor("#F5DDB0");
        private static readonly Color HandInk = HexHandColor("#19323D");
        private static readonly Color HandBrass = HexHandColor("#D9A447");
        private static readonly Color HandQuietBrass = HexHandColor("#C9AD7D");
        private const float HandCardScale = 0.70f;
        private const float HandCardWidth = 144f * HandCardScale;
        private const float HandCardGap = 12f;
        private float fillHandDeckPositionX;
        private readonly RectTransform[] fillHandCardShadows = new RectTransform[5];
        private readonly Button[] fillHandCards = new Button[5];
        private readonly Image[] fillHandFaces = new Image[5];
        private readonly Image[] fillHandArtMats = new Image[5];
        private readonly Image[] fillHandSelectionTabs = new Image[5];
        private readonly Image[] fillHandIcons = new Image[5];
        private readonly Image[,] fillHandFooterIcons = new Image[5, 2];
        private readonly Text[] fillHandNames = new Text[5];
        private readonly Text[] fillHandStates = new Text[5];
        private readonly CanvasGroup[] fillHandGroups = new CanvasGroup[5];
        private Text fillAnimalScoreValue;
        private Text fillResidentScoreValue;
        private Text fillAnimalScoreCaption;
        private Text fillResidentScoreCaption;
        private Image fillRightHandArt;
        private Text fillRightHandName;
        private Text fillRightHandCount;
        private Text fillRightHandTagline;
        private RectTransform fillIntroGuideRoot;
        private Image fillIntroGuideArt;
        private Text fillIntroGuideTitle;
        private Text fillIntroGuideCaption;
        private Text fillIntroGuideSteps;
        private TextMeshProUGUI fillRightScoreTitle;
        private TextMeshProUGUI fillRightScoreNumber;
        private TextMeshProUGUI fillRightScoreUnit;
        private static readonly Color ScoreBronze =
            new(0.78f, 0.52f, 0.32f);
        private static readonly Color ScoreSilver =
            new(0.83f, 0.88f, 0.91f);
        private static readonly Color ScoreGold =
            new(1f, 0.82f, 0.36f);
        private static readonly Color ScoreAnimalGreen =
            new(0.30f, 0.70f, 0.42f);
        private static readonly Color ScoreSharedBlue =
            new(0.26f, 0.56f, 0.82f);

        private ConstructionTileData PendingUpgradePlot()
        {
            if (!hasPendingPlacement || run?.UsesNeighborUpgrades != true)
                return null;
            var planting = pendingCategory == ConstructionCategory.Green &&
                pendingPlanting == GreenPlanting.None
                ? run.ResolveGreenPlanting(pendingColumn, pendingRow)
                : pendingPlanting;
            return new ConstructionTileData
            {
                id = "placement-preview",
                column = pendingColumn,
                row = pendingRow,
                builtDay = run.CurrentDay,
                buildIndex = run.BuiltCount + 1,
                category = pendingCategory,
                planting = planting
            };
        }

        private string UpgradePreviewSummary(
            IReadOnlyList<ConstructionTileData> changed,
            IReadOnlyList<ConstructionTileData> projectedBoard)
        {
            var lines = new List<string>(4);
            foreach (var greens in changed.Where(tile =>
                tile.category == ConstructionCategory.Green).GroupBy(tile =>
                    ConstructionPlacementScore.UpgradeLevel(tile, projectedBoard)))
            {
                var count = greens.Count();
                lines.Add(L($"Habitat level {greens.Key} · {count} plot(s)",
                    $"{count} 格绿地升到 {greens.Key} 级"));
            }
            var homes = changed.Count(tile =>
                tile.category == ConstructionCategory.Residence);
            if (homes > 0)
            {
                var reward = homes * ConstructionPlacementScore.GardenResidencePoints;
                lines.Add(run.UsesPlacementScore
                    ? L($"Garden homes ×{homes} · +{reward} build points",
                        $"花园住宅 ×{homes} · 建造 +{reward}")
                    : L($"Garden homes ×{homes}", $"花园住宅 ×{homes}"));
            }
            if (changed.Any(tile => tile.category == ConstructionCategory.Restaurant))
                lines.Add(L("Terrace restaurant · 3 meal seats",
                    "露台餐厅 · 餐位增至 3 个"));
            if (changed.Any(tile => tile.category == ConstructionCategory.Street))
                lines.Add(L("Slow street · half the collision risk",
                    "慢行街 · 动物过街风险减半"));
            return string.Join("\n", lines);
        }

        private void StartFillHandScoreMotion(ConstructionEcologyDayResult ecology,
            int oldAnimal, int oldResident, int oldShared)
        {
            var placedTileId = pendingPlacementScoreTileId;
            var placementDelta = run?.ScoringVersion >= 12 &&
                placedTileId != null
                    ? run.LastPlacementPoints : pendingPlacementScoreDelta;
            pendingPlacementScoreTileId = null;
            pendingPlacementScoreDelta = 0;
            if (scoreMotionLayer == null || run == null || ecology == null) return;

            var build = new List<ScoreMotionStep>();
            var placed = run.BuiltTiles.FirstOrDefault(tile =>
                tile.id == placedTileId);
            var buildRemainder = placementDelta;
            if (run.UsesNeighborUpgrades && placed != null)
            {
                var changed = ConstructionPlacementScore.NewlyUpgradedTiles(
                    placed, run.BuiltTiles);
                foreach (var tile in changed)
                {
                    if (tile.category == ConstructionCategory.Residence)
                    {
                        const int award = ConstructionPlacementScore.GardenResidencePoints;
                        build.Add(new ScoreMotionStep(ScoreSide.Build,
                            award, tile.id, "Beside connected green", "邻接连片绿地 · 花园住宅", true));
                        buildRemainder -= award;
                    }
                    else if (tile.category == ConstructionCategory.Green)
                    {
                        var level = run.GreenUpgradeLevel(tile.id);
                        build.Add(new ScoreMotionStep(ScoreSide.Build,
                            0, tile.id, $"Connected green → L{level}", $"绿地共边 → {level} 级"));
                    }
                    else if (tile.category == ConstructionCategory.Restaurant)
                        build.Add(new ScoreMotionStep(ScoreSide.Build,
                            0, tile.id, "Beside plaza · seats 2→3", "邻接广场 · 餐位 2→3"));
                    else if (tile.category == ConstructionCategory.Street)
                        build.Add(new ScoreMotionStep(ScoreSide.Build,
                            0, tile.id, "Green on both sides · risk ½", "两侧绿地 · 风险减半"));
                }
            }
            if (run.UsesPlacementScore && run.ScoringVersion >= 12 && placed != null)
            {
                foreach (var group in run.BuiltTiles.Where(tile =>
                    tile.buildIndex < placed.buildIndex &&
                    ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row,
                        placed.column, placed.row)).GroupBy(tile => tile.category))
                {
                    var points = group.Count() * ConstructionPlacementScore.PairPoints(
                        placed.category, group.Key, run.PlacementRulesVersion);
                    if (points == 0) continue;
                    var reason = CategoryLabel(placed.category) + " + " + CategoryLabel(group.Key);
                    var englishReason = reason + " · shared edge";
                    var chineseReason = reason + "共边";
                    if (points < 0)
                    {
                        var traffic = placed.category == ConstructionCategory.Street ||
                            group.Key == ConstructionCategory.Street;
                        englishReason = traffic ? "Street beside green · crossing risk" :
                            "Waste beside home · disturbance";
                        chineseReason = traffic ? "道路贴绿地 · 过街风险" :
                            "垃圾间邻接住宅 · 干扰生活";
                    }
                    build.Add(new ScoreMotionStep(ScoreSide.Build, points, group.First().id,
                        englishReason, chineseReason));
                    buildRemainder -= points;
                }
            }
            if (run.UsesPlacementScore && buildRemainder != 0)
            {
                var completedNeighborhood = run.ScoringVersion >= 12
                    ? run.LastCommunityAwardCount > 0
                    : run.UsesCompleteNeighborhoods &&
                      ConstructionPlacementScore.NewlyCompletedNeighborhoods(
                          placed, run.BuiltTiles) > 0;
                var upgradedGreen = run.ScoringVersion >= 12
                    ? placed?.category == ConstructionCategory.Green &&
                      ConstructionPlacementScore.GreenMilestonePoints(
                          placed, run.BuiltTiles.Where(tile =>
                              tile.buildIndex < placed.buildIndex).ToArray()) > 0
                    : run.UsesGreenUpgrades &&
                      placed?.category == ConstructionCategory.Green &&
                      run.GreenUpgradeLevel(placed.id) > 1;
                build.Add(new ScoreMotionStep(ScoreSide.Build,
                    buildRemainder, placedTileId,
                    completedNeighborhood ? "Complete neighbourhood" :
                        upgradedGreen ? "Green upgrade" :
                        run.ScoringVersion >= 12
                            ? "Shared-edge pairing" : "Nearby rooms",
                    completedNeighborhood ? "街坊成形" :
                        upgradedGreen ? "绿地连片" :
                        run.ScoringVersion >= 12
                            ? "共边搭配" : "八邻格联动"));
            }

            var animal = new List<ScoreMotionStep>();
            var resident = new List<ScoreMotionStep>();
            var accountedAnimal = 0;
            if (run.HasFedAnimalScore)
            {
                var mealsBySource = (ecology.squirrelFoodTileIds ??
                        new List<string>())
                    .Concat(ecology.pigeonMealTileIds ?? new List<string>())
                    .Concat(ecology.hedgehogMealTileIds ?? new List<string>())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .GroupBy(id => id);
                foreach (var meals in mealsBySource)
                {
                    var points = meals.Count() *
                        (run.ScoringVersion >= 12
                            ? ConstructionFoodAreaScorer.BalancedFedAnimalPoints
                            : ConstructionFoodAreaScorer.FedAnimalPoints);
                    animal.Add(new ScoreMotionStep(ScoreSide.Animal, points,
                        meals.Key, "Wildlife fed", "动物进食"));
                    accountedAnimal += points;
                }
                foreach (var id in ecology.fedScoringSourceTileIds ??
                             new List<string>())
                {
                    var source = run.BuiltTiles.FirstOrDefault(tile =>
                        tile.id == id);
                    if (source == null) continue;
                    var points = ConstructionFoodAreaScorer
                        .PointsForFedSource(source.category,
                            run.ScoringVersion >= 11,
                            run.ScoringVersion >= 12);
                    if (points == 0) continue;
                    animal.Add(new ScoreMotionStep(ScoreSide.Animal, points,
                        id, "Feeding source", "实际供食"));
                    accountedAnimal += points;
                }
            }
            else if (run.ScoringVersion >= 7 &&
                ecology.scoredFoodTileIds != null)
                foreach (var id in ecology.scoredFoodTileIds.Distinct())
                {
                    var source = run.BuiltTiles.FirstOrDefault(tile =>
                        tile.id == id);
                    if (source == null) continue;
                    var points = ConstructionFoodAreaScorer.PointsFor(
                        source.category);
                    if (points == 0) continue;
                    animal.Add(new ScoreMotionStep(ScoreSide.Animal, points,
                        id, "Wildlife food", "动物食源"));
                    accountedAnimal += points;
                }
            var penalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
            if (penalty > 0)
            {
                animal.Add(new ScoreMotionStep(ScoreSide.Animal, -penalty,
                    ecology.trafficStreetTileId, "Traffic", "街道车祸"));
                accountedAnimal -= penalty;
            }
            var animalRemainder = run.AnimalScore - oldAnimal -
                accountedAnimal;
            if (animalRemainder != 0)
                animal.Add(new ScoreMotionStep(ScoreSide.Animal,
                    animalRemainder, null, "Wildlife day", "动物活动"));

            var accountedResident = 0;
            if (run.LastHumanDay?.residents != null)
                foreach (var person in run.LastHumanDay.residents)
                {
                    if (person == null || person.satisfactionPoints == 0)
                        continue;
                    resident.Add(new ScoreMotionStep(ScoreSide.Resident,
                        person.satisfactionPoints, person.homeTileId,
                        "Resident cycle", "居民循环"));
                    accountedResident += person.satisfactionPoints;
                }
            var residentRemainder = run.HumanScore - oldResident -
                accountedResident;
            if (residentRemainder != 0)
                resident.Add(new ScoreMotionStep(ScoreSide.Resident,
                    residentRemainder, null, "Resident day", "居民活动"));

            // A zero phase stays beside its own counter, so all three stages
            // remain legible without implying points came from an empty room.
            if (animal.Count == 0)
                animal.Add(new ScoreMotionStep(ScoreSide.Animal, 0,
                    null, "No wildlife points", "动物未得分"));
            if (resident.Count == 0)
                resident.Add(new ScoreMotionStep(ScoreSide.Resident, 0,
                    null, "No resident points", "居民未得分"));
            var shared = new[]
            {
                new ScoreMotionStep(ScoreSide.Shared,
                    run.BalanceScore - oldShared, null,
                    "Shared score", "共栖得分")
            };
            scoreMotionAnimation = StartCoroutine(AnimateFillHandScoreMotion(
                build, animal, resident, shared,
                oldAnimal, oldResident, oldShared,
                run.PlacementScore - placementDelta));
        }

        private IEnumerator AnimateFillHandScoreMotion(
            IReadOnlyList<ScoreMotionStep> build,
            IReadOnlyList<ScoreMotionStep> animal,
            IReadOnlyList<ScoreMotionStep> resident,
            IReadOnlyList<ScoreMotionStep> shared,
            int oldAnimal, int oldResident, int oldShared, int oldBuild)
        {
            WriteFillScoreTotals(oldAnimal, oldResident, oldShared, oldBuild);
            if (build.Count > 0)
                yield return AnimateFillScoreGroup(build, oldAnimal,
                    oldResident, oldShared, oldBuild,
                    oldAnimal, oldResident, oldShared, run.PlacementScore,
                    "Score fly Build");
            yield return AnimateFillScoreGroup(animal, oldAnimal,
                oldResident, oldShared, run.PlacementScore,
                run.AnimalScore, oldResident, oldShared,
                run.PlacementScore, "Score fly Animal");
            yield return AnimateFillScoreGroup(resident, run.AnimalScore,
                oldResident, oldShared, run.PlacementScore,
                run.AnimalScore, run.HumanScore, oldShared,
                run.PlacementScore, "Score fly Resident");
            yield return AnimateFillScoreGroup(shared, run.AnimalScore,
                run.HumanScore, oldShared, run.PlacementScore,
                run.AnimalScore, run.HumanScore, run.BalanceScore,
                run.PlacementScore, "Score fly Shared");
            WriteFillScoreTotals(run.AnimalScore, run.HumanScore,
                run.BalanceScore, run.PlacementScore);
            ClearFillScoreTags();
            scoreMotionGroup.alpha = 0f;
            scoreMotionAnimation = null;
        }

        private IEnumerator AnimateFillScoreGroup(
            IReadOnlyList<ScoreMotionStep> steps,
            int fromAnimal, int fromResident, int fromShared, int fromBuild,
            int toAnimal, int toResident, int toShared, int toBuild,
            string objectPrefix)
        {
            var tags = new List<(RectTransform rect, Vector2 source,
                Vector2 target, bool travel)>();
            scoreMotionTag.gameObject.SetActive(false);
            foreach (var step in steps)
            {
                // A score is a small cue over its room, not another card
                // covering the board. Keep the older modes' plaque intact.
                var clone = Instantiate(scoreMotionValue.gameObject,
                    scoreMotionLayer);
                clone.name = $"{objectPrefix} {fillScoreTags.Count}";
                clone.SetActive(true);
                fillScoreTags.Add(clone);
                var rect = clone.GetComponent<RectTransform>();
                var isUpgradeLabel = step.Side == ScoreSide.Build &&
                    step.Delta == 0;
                Vector2 source = step.Side == ScoreSide.Shared
                    ? (Vector2)scoreMotionLayer.InverseTransformPoint(
                        balanceScoreText.rectTransform.position)
                    : ScoreMotionSource(step.SourceTileId);
                if (isUpgradeLabel || step.MoveToTotal) source.y -= 24f;
                // Placement points belong to the built room. Its numeric
                // total updates during the cue; the cue itself rises there.
                var travel = step.MoveToTotal ||
                    step.Side != ScoreSide.Build && step.Delta != 0;
                var target = source;
                if (step.MoveToTotal)
                    target = scoreMotionLayer.InverseTransformPoint(
                        balanceScoreText.rectTransform.position);
                else if (step.Side != ScoreSide.Build)
                {
                    var targetText = run.IsFinished && finalResultText != null
                        ? finalResultText
                        : progressText;
                    target = scoreMotionLayer.InverseTransformPoint(
                        targetText.rectTransform.position);
                    if (step.Delta == 0) source = target;
                }
                rect.anchoredPosition = source;
                rect.sizeDelta = new Vector2(84f, 48f);
                var value = clone.GetComponent<Text>();
                var showReason = step.Delta < 0 || step.Side == ScoreSide.Build;
                value.text = isUpgradeLabel
                    ? L(step.EnglishReason, step.ChineseReason) :
                    (step.Delta > 0 ? $"+{step.Delta}" : step.Delta.ToString()) +
                    (showReason ? "\n<size=13>" + L(step.EnglishReason, step.ChineseReason) + "</size>" : "");
                value.fontSize = isUpgradeLabel ? 14 : 30;
                if (showReason || isUpgradeLabel)
                    rect.sizeDelta = new Vector2(180f, 66f);
                value.alignment = TextAnchor.MiddleCenter;
                value.color = FillScoreColor(step.Side, step.Delta);
                value.raycastTarget = false;
                var outline = clone.AddComponent<Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                tags.Add((rect, source, target, travel));
            }
            scoreMotionGroup.alpha = 1f;
            var elapsed = 0f;
            var hoverDuration = steps.Any(step => step.Delta < 0 ||
                step.Side == ScoreSide.Build) ? 1.5f : 0.16f;
            const float flyDuration = 0.50f;
            while (elapsed < hoverDuration + flyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var flying = elapsed > hoverDuration;
                var fraction = flying ? Mathf.Clamp01(
                    (elapsed - hoverDuration) / flyDuration) : 0f;
                var eased = Mathf.SmoothStep(0f, 1f, fraction);
                foreach (var tag in tags)
                {
                    var position = Vector2.Lerp(tag.source, tag.target, eased);
                    position.y += flying
                        ? tag.travel
                            ? Mathf.Sin(fraction * Mathf.PI) * 24f
                            : 18f + eased * 24f
                        : Mathf.Clamp01(elapsed / hoverDuration) * 18f;
                    tag.rect.anchoredPosition = position;
                    tag.rect.localScale = Vector3.one *
                        Mathf.Lerp(0.82f, 1f, Mathf.Min(1f,
                            elapsed / hoverDuration));
                }
                scoreMotionGroup.alpha = flying
                    ? Mathf.Clamp01((1f - fraction) * 6f) : 1f;
                WriteFillScoreTotals(
                    InterpolateScore(fromAnimal, toAnimal, eased),
                    InterpolateScore(fromResident, toResident, eased),
                    InterpolateScore(fromShared, toShared, eased),
                    InterpolateScore(fromBuild, toBuild, eased));
                yield return null;
            }
            WriteFillScoreTotals(toAnimal, toResident, toShared, toBuild);
            ClearFillScoreTags();
            scoreMotionGroup.alpha = 0f;
        }

        private void WriteFillScoreTotals(int animal, int resident,
            int shared, int build)
        {
            WriteScoreMotionTotals(animal, resident, shared, build);
            if (run.IsFinished && finalResultText != null)
                finalResultText.text = ((int)Math.Max(int.MinValue,
                    Math.Min(int.MaxValue, (long)shared + build +
                        run.SpecialTaskScore))).ToString();
        }

        private void BuildFillScoreSummary()
        {
            if (run?.UsesFillHand != true) return;

            // The old plaques remain as data targets for older presentation
            // paths, but only the score area on the right is visible.
            foreach (var value in new[] { animalMetricText, humanMetricText })
            {
                var group = value.transform.parent.gameObject
                    .AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            progressIcon.gameObject.SetActive(false);
            progressPanel.rectTransform.anchoredPosition =
                new Vector2(CaseLeafCenter, 197f);
            progressPanel.rectTransform.sizeDelta =
                new Vector2(342f, 242f);
            progressHeading.rectTransform.anchoredPosition =
                new Vector2(18f, 50f);
            progressHeading.rectTransform.sizeDelta =
                new Vector2(294f, 32f);
            progressHeading.fontSize = 23;
            progressText.rectTransform.anchoredPosition =
                new Vector2(-8f, -5f);
            progressText.rectTransform.sizeDelta = new Vector2(248f, 91f);
            progressText.fontSize = 73;
            progressText.alignment = TextAnchor.MiddleLeft;
            // The legacy labels still hold the score animation target, while
            // these direct TMP labels paint the larger right-leaf typography.
            progressHeading.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            fillRightScoreTitle = CreateFillScoreLabel("Score heading print",
                new Vector2(18f, 50f), new Vector2(294f, 38f),
                "Fonts/TMP/Nunito-Bold SDF", 23f);
            fillRightScoreNumber = CreateFillScoreLabel("Score number print",
                new Vector2(-8f, -5f), new Vector2(248f, 94f),
                "Fonts/TMP/Nunito-Bold SDF", 73f);
            fillRightScoreUnit = CreateFillScoreLabel("Score unit print",
                new Vector2(-56f, -21f), new Vector2(50f, 37f),
                "Fonts/TMP/NotoSansSC-Regular SDF", 23f);
            balanceScoreText.rectTransform.anchoredPosition =
                new Vector2(22f, -50f);
            balanceScoreText.rectTransform.sizeDelta = new Vector2(308f, 34f);
            balanceScoreText.fontSize = 17;
            balanceScoreText.alignment = TextAnchor.MiddleLeft;
            openingTourScoreTarget.SetParent(progressPanel.transform, false);
            SetRect(openingTourScoreTarget, new Vector2(0f, 19f),
                new Vector2(318f, 196f));
            Panel("Score summary rule", progressPanel.transform,
                new Vector2(-45f, -84f), new Vector2(170f, 1.5f), Gold)
                .raycastTarget = false;
        }

        private TextMeshProUGUI CreateFillScoreLabel(string name,
            Vector2 position, Vector2 size, string fontPath, float fontSize)
        {
            var rect = NewRect(name, progressPanel.transform, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Resources.Load<TMP_FontAsset>(fontPath);
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Left;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = Ink;
            label.raycastTarget = false;
            return label;
        }

        private void BuildFillRightHandPreview()
        {
            if (run?.UsesFillHand != true) return;
            guidePanel.rectTransform.anchoredPosition =
                new Vector2(CaseLeafCenter, 0f);
            guidePanel.rectTransform.sizeDelta =
                new Vector2(342f, 340f);
            nextStepHeading.rectTransform.anchoredPosition =
                new Vector2(18f, 84f);
            nextStepHeading.rectTransform.sizeDelta =
                new Vector2(294f, 35f);
            nextStepHeading.fontSize = 23;
            placementGuideText.gameObject.SetActive(false);
            selectedBuildIcon.gameObject.SetActive(false);
            var card = Panel("Selected hand card", guidePanel.transform,
                new Vector2(-82f, -28f), new Vector2(128f, 128f),
                HandPaper);
            card.raycastTarget = false;
            var cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = HandInk;
            cardOutline.effectDistance = new Vector2(1f, -1f);
            fillRightHandArt = Panel("Selected hand illustration",
                card.transform, Vector2.zero,
                new Vector2(122f, 122f), Color.white);
            fillRightHandArt.preserveAspect = true;
            fillRightHandArt.raycastTarget = false;
            fillRightHandName = TextAt("Selected hand name",
                guidePanel.transform, new Vector2(68f, 6f),
                new Vector2(142f, 52f), "", 22, true,
                Ink, TextAnchor.MiddleLeft);
            fillRightHandName.raycastTarget = false;
            fillRightHandName.resizeTextForBestFit = true;
            fillRightHandName.resizeTextMinSize = 17;
            fillRightHandName.resizeTextMaxSize = 22;
            fillRightHandCount = TextAt("Selected hand valid plots",
                guidePanel.transform, new Vector2(68f, -42f),
                new Vector2(142f, 34f), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            fillRightHandCount.raycastTarget = false;
            Panel("Selected hand divider", guidePanel.transform,
                new Vector2(18f, -111f), new Vector2(294f, 1.5f),
                HandQuietBrass).raycastTarget = false;
            fillRightHandTagline = TextAt("Selected hand tagline",
                guidePanel.transform, new Vector2(-24f, -138f),
                new Vector2(238f, 60f), "", 18, false,
                Ink, TextAnchor.MiddleLeft);
            fillRightHandTagline.raycastTarget = false;
            SetRect(openingTourGuideTarget, new Vector2(18f, -32f),
                new Vector2(304f, 274f));
        }

        private void RefreshFillRightHandPreview()
        {
            if (fillRightHandArt == null || run?.UsesFillHand != true) return;
            if (AwaitingFreeBuildChoice)
            {
                fillRightHandArt.transform.parent.gameObject.SetActive(false);
                fillRightHandCount.gameObject.SetActive(false);
                fillRightHandTagline.gameObject.SetActive(false);
                SetRect(fillRightHandName.rectTransform, new Vector2(18f, 0f), new Vector2(294f, 52f));
                fillRightHandName.text = L("Choose a card below", "从下方选一张牌");
                return;
            }
            fillRightHandArt.transform.parent.gameObject.SetActive(true);
            fillRightHandArt.gameObject.SetActive(true);
            fillRightHandCount.gameObject.SetActive(true);
            fillRightHandTagline.gameObject.SetActive(true);
            SetRect(fillRightHandName.rectTransform, new Vector2(68f, 6f), new Vector2(142f, 52f));
            var planting = run.BuiltCount == 0
                ? GreenPlanting.Oak
                : selectedCategory == ConstructionCategory.Green
                    ? selectedPlanting : GreenPlanting.None;
            fillRightHandArt.sprite = ConstructionHandArtCatalog.GetRoom(
                selectedCategory, planting) ??
                ConstructionIconCatalog.GetBuild(selectedCategory, planting);
            fillRightHandName.text = run.BuiltCount == 0
                ? L("Central oak", "中央橡树") : SelectedBuildLabel();
            var count = AvailablePlotCount(selectedCategory, planting);
            fillRightHandCount.text = run.IsFinished
                ? L("Run complete", "本局结束")
                : count > 0
                    ? L($"{count} plot{(count == 1 ? "" : "s")}",
                        $"可放 {count} 处")
                    : L("No valid plot", "暂无落点");
            fillRightHandTagline.text = run.BuiltCount == 0
                ? L("Leave the first oak to its squirrel.",
                    "先给松鼠留下一棵树。")
                : selectedCategory switch
                {
                    ConstructionCategory.Green => L(
                        "Hover a plot for its plant and maturity date.",
                        "落点决定类型；悬停空格查看植物和成熟日期。"),
                    ConstructionCategory.Residence => L(
                        "A new neighbour is looking for a home.",
                        "有位新邻居想搬来。"),
                    ConstructionCategory.Restaurant => L(
                        "A warm meal along the way.",
                        "让居民路上吃顿热饭。"),
                    ConstructionCategory.Workshop => L(
                        "More work close to home.",
                        "在家附近找份工作。"),
                    ConstructionCategory.Waste => L(
                        "Clear the waste nearby.",
                        "把附近的垃圾收走。"),
                    ConstructionCategory.Street => L(
                        "Make the walk a little easier.",
                        "让这段路更好走。"),
                    ConstructionCategory.Supermarket => L(
                        "Dining leaves 1 wildlife portion today; eaten food means less waste.",
                        "当天有人用餐留 1 份动物食物；吃掉后少留垃圾。"),
                    ConstructionCategory.Square => L(
                        "A place to stop and meet.",
                        "留个地方歇脚、相遇。"),
                    _ => string.Empty
                };
        }

        private void BuildFillIntroGuide(Transform parent)
        {
            if (run?.UsesFillHand != true) return;
            fillIntroGuideRoot = NewRect("Illustrated room lesson", parent,
                Vector2.zero, new Vector2(310f, 390f));
            Panel("Lesson header rule", fillIntroGuideRoot,
                new Vector2(-12f, 72f), new Vector2(274f, 1.5f), HandInk)
                .raycastTarget = false;
            Panel("Lesson title accent", fillIntroGuideRoot,
                new Vector2(-147f, 32f), new Vector2(4f, 32f),
                new Color(0.68f, 0.31f, 0.18f)).raycastTarget = false;
            fillIntroGuideTitle = TextAt("Lesson room name",
                fillIntroGuideRoot, new Vector2(-7f, 32f),
                new Vector2(264f, 42f), "", 28, true,
                Ink, TextAnchor.MiddleLeft);
            fillIntroGuideTitle.raycastTarget = false;
            var artMat = Panel("Lesson artwork mount", fillIntroGuideRoot,
                new Vector2(-12f, -92f), new Vector2(162f, 162f),
                HandPaper);
            artMat.raycastTarget = false;
            var outline = artMat.gameObject.AddComponent<Outline>();
            outline.effectColor = HandQuietBrass;
            outline.effectDistance = new Vector2(1f, -1f);
            fillIntroGuideArt = Panel("Lesson room illustration",
                artMat.transform, Vector2.zero,
                new Vector2(154f, 154f), Color.white);
            fillIntroGuideArt.preserveAspect = true;
            fillIntroGuideArt.raycastTarget = false;
            fillIntroGuideCaption = TextAt("Lesson room summary",
                fillIntroGuideRoot, new Vector2(-7f, -204f),
                new Vector2(268f, 64f), "", 15, false,
                Ink, TextAnchor.MiddleLeft);
            fillIntroGuideCaption.raycastTarget = false;
            Panel("Lesson progress rule", fillIntroGuideRoot,
                new Vector2(-12f, -244f), new Vector2(236f, 1f),
                HandQuietBrass).raycastTarget = false;
            fillIntroGuideSteps = TextAt("Lesson progress dots",
                fillIntroGuideRoot, new Vector2(-12f, -268f),
                new Vector2(240f, 27f), "", 19, false,
                UsedGreen, TextAnchor.MiddleCenter);
            fillIntroGuideSteps.raycastTarget = false;
            fillIntroGuideRoot.gameObject.SetActive(false);
        }

        private void RefreshFillIntroGuide()
        {
            if (fillIntroGuideRoot == null || run == null) return;
            var visible = run.IsIntroPhase && !run.IsFinished &&
                !run.TutorialTipsSkipped;
            fillIntroGuideRoot.gameObject.SetActive(visible);
            todayHeading.rectTransform.anchoredPosition = visible
                ? new Vector2(-29f, 99f) : new Vector2(-18f, 99f);
            todayHeading.rectTransform.sizeDelta = visible
                ? new Vector2(220f, 28f) : new Vector2(150f, 28f);
            todayHeading.fontSize = visible ? 17 : 19;
            skipTutorialButton.GetComponent<RectTransform>().anchoredPosition =
                visible ? new Vector2(116f, 99f) : new Vector2(88f, 99f);
            if (openDispatchButton != null)
                openDispatchButton.GetComponent<RectTransform>()
                    .anchoredPosition = visible
                    ? new Vector2(0f, -320f)
                    : new Vector2(0f, -161f);
            if (!run.IsIntroPhase) return;
            storyPreviewText.gameObject.SetActive(!visible);
            roomFunctionText.gameObject.SetActive(!visible);
            storyCueIcon.gameObject.SetActive(!visible);
            storyDivider.gameObject.SetActive(!visible);
            if (!visible) return;
            var lesson = Mathf.Clamp(run.IntroTypesBuilt + 1, 1,
                ConstructionTutorialGuide.RoomCount);
            todayHeading.text = L($"MEET THE ROOMS {lesson}/8",
                $"认识新房间 {lesson}/8");
            var planting = run.BuiltCount == 0 ? GreenPlanting.Oak :
                selectedCategory == ConstructionCategory.Green
                    ? selectedPlanting : GreenPlanting.None;
            fillIntroGuideArt.sprite = ConstructionHandArtCatalog.GetRoom(
                selectedCategory, planting);
            fillIntroGuideTitle.text = run.BuiltCount == 0
                ? L("Central oak", "中央橡树") : CategoryLabel(selectedCategory);
            fillIntroGuideCaption.text = FillIntroGuideSummary();
            fillIntroGuideSteps.text = string.Join(" ",
                Enumerable.Range(0, ConstructionTutorialGuide.RoomCount)
                    .Select(index => index < run.IntroTypesBuilt ? "●" : "○"));
            // Frame the complete lesson: heading, room name, illustration and
            // its short explanation. The old target only enclosed the title.
            SetRect(openingTourTodayTarget, new Vector2(-7f, -60f),
                new Vector2(292f, 374f));
        }

        private string FillIntroGuideSummary()
        {
            if (run.BuiltCount == 0)
                return L("The squirrel was here first. Place the oak on the highlighted centre plot, then confirm.",
                    "松鼠比城市来得早。把橡树放到高亮的正中格，再确认建造，给它留个家。");
            return selectedCategory switch
            {
                ConstructionCategory.Green => L(
                    "Join green plots edge to edge so wildlife can find shelter and food.",
                    "把绿地沿上下左右接起来，给动物多留一点栖身和觅食的地方。"),
                ConstructionCategory.Residence => L(
                    "Place the home beside a built plot. After building, click the home and choose Set work & meals on the left.",
                    "把住宅贴着已建格放下。建好后点击住宅，再点左侧“安排工作与用餐”。"),
                ConstructionCategory.Restaurant => run.UsesNeighborUpgrades
                    ? L("Build by a home or its connected street. One job and two seats, or three seats beside a plaza.",
                        "贴住宅或与住宅接通的街道。餐厅有 1 个岗位和 2 个餐位，贴广场可增至 3 个餐位。")
                    : L("Build by a home or its connected street to provide one job and two meal seats.",
                        "贴住宅或与住宅接通的街道，让邻居有 1 个岗位和 2 个餐位。"),
                ConstructionCategory.Workshop => L(
                    "Build by a home or its connected street to open two jobs.",
                    "把工作坊贴着住宅或与住宅接通的街道放下，给邻居多开 2 个岗位。"),
                ConstructionCategory.Waste => run.UsesUncappedWaste
                    ? L("Build by a home, restaurant, or a home-linked street. It clears nearby waste and waste reached by roads.",
                        "贴住宅、餐厅或与住宅接通的街道。结算时清运附近和连续街道接通的垃圾。")
                    : L("Build by a home, restaurant, or a home-linked street. It clears up to two portions of old waste.",
                        "贴住宅、餐厅或与住宅接通的街道。每回合最多清运 2 份旧垃圾。"),
                ConstructionCategory.Street => L(
                    "Extend a street from a home or its connected street. Shared edges join the route.",
                    "从住宅或与住宅接通的街道往外铺。上下左右贴边才接得通。"),
                ConstructionCategory.Supermarket => L(
                    "Build by a home or its street. One job, two seats. Dining leaves 1 wildlife portion today; eating it removes 1 waste portion.",
                    "贴住宅或连通街道。提供 1 岗位、2 餐位；当天有人用餐留 1 份动物食物，动物吃掉后少留 1 份垃圾。"),
                ConstructionCategory.Square => L(
                    "For this lesson, connect the plaza to a home or its street. A resident who worked and ate can visit.",
                    "这一步把广场接到住宅或连通住宅的街道。居民上班、用餐后可到访，给动物留食物。"),
                _ => string.Empty
            };
        }

        private Text BuildFillScorePart(string name, float x, Sprite sprite,
            Func<string> details, out Text caption)
        {
            var row = Panel(name, progressPanel.transform,
                new Vector2(x, -108f), new Vector2(148f, 49f), Color.clear);
            row.raycastTarget = true;
            var icon = Panel("Score source icon", row.transform,
                new Vector2(-51f, 1f), new Vector2(29f, 29f), Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var value = TextAt("Score source value", row.transform,
                new Vector2(12f, 8f), new Vector2(92f, 28f), "0", 22,
                true, Ink, TextAnchor.MiddleLeft);
            value.raycastTarget = false;
            caption = TextAt("Score source caption", row.transform,
                new Vector2(12f, -14f), new Vector2(92f, 19f), "", 12,
                false, HeadingInk, TextAnchor.MiddleLeft);
            caption.raycastTarget = false;
            AttachHover(row.gameObject, () => sprite,
                () => name == "Animal score summary"
                    ? L("Animal score", "动物得分")
                    : L("Resident score", "居民得分"),
                details, TooltipLocation.BoardInfo);
            return value;
        }

        private void UpdateFillScoreSummary(int animal, int resident,
            int shared, int build)
        {
            if (run?.UsesFillHand != true) return;
            if (fillAnimalScoreValue != null)
                fillAnimalScoreValue.text = animal.ToString();
            if (fillResidentScoreValue != null)
                fillResidentScoreValue.text = resident.ToString();
            if (fillAnimalScoreCaption != null)
                fillAnimalScoreCaption.text = L("ANIMALS", "动物");
            if (fillResidentScoreCaption != null)
                fillResidentScoreCaption.text = L("RESIDENTS", "居民");
            var total = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
                (long)shared + build + run.SpecialTaskScore));
            progressText.text = total.ToString();
            if (fillRightScoreTitle != null)
            {
                // Match the selected-card heading while retaining Chinese
                // glyph coverage when the player changes language.
                var font = Resources.Load<TMP_FontAsset>(Chinese
                    ? "Fonts/TMP/NotoSansSC-Regular SDF"
                    : "Fonts/TMP/Nunito-Bold SDF");
                if (font != null) fillRightScoreTitle.font = font;
                fillRightScoreTitle.fontStyle = Chinese
                    ? FontStyles.Bold : FontStyles.Normal;
                fillRightScoreTitle.text = L("RUN SCORE", "本局得分");
            }
            if (fillRightScoreNumber != null)
                fillRightScoreNumber.text = total.ToString();
            if (fillRightScoreUnit != null)
            {
                fillRightScoreUnit.text = L("pts", "分");
                // Keep the unit beside the final digit instead of under the
                // decorative branches at the outer edge of the page.
                fillRightScoreUnit.rectTransform.anchoredPosition =
                    new Vector2(-56f + Math.Min(4, total.ToString().Length - 1) * 45f, -21f);
            }
            balanceScoreText.text = L(
                $"SHARED {shared} · BUILD {build} · GOALS {run.SpecialTaskScore}",
                $"共栖 {shared} · 建造 {build} · 目标 {run.SpecialTaskScore}");
        }

        private static Color FillScoreColor(ScoreSide side, int delta)
        {
            if (delta < 0) return new Color(1f, 0.53f, 0.43f);
            if (side == ScoreSide.Animal) return ScoreAnimalGreen;
            if (side == ScoreSide.Shared) return ScoreSharedBlue;
            return delta <= 2 ? ScoreBronze :
                delta <= 5 ? ScoreSilver : ScoreGold;
        }

        private void ClearFillScoreTags()
        {
            foreach (var tag in fillScoreTags)
                if (tag != null)
                {
                    tag.SetActive(false);
                    Destroy(tag);
                }
            fillScoreTags.Clear();
            if (scoreMotionTag != null)
                scoreMotionTag.gameObject.SetActive(true);
        }

        private void BuildFillHand(Transform canvasParent)
        {
            if (run?.UsesFillHand != true) return;

            fillHandWood = Panel("Warm wood card rest", canvasParent,
                new Vector2(0f, -368f), new Vector2(610f, 156f),
                Color.white);
            fillHandWood.sprite = ConstructionHandArtCatalog.GetTray();
            fillHandWood.type = Image.Type.Sliced;
            fillHandWood.pixelsPerUnitMultiplier =
                fillHandWood.sprite.rect.height / 156f;
            fillHandWood.raycastTarget = false;

            fillHandRoot = NewRect("Fill hand on tabletop", canvasParent,
                new Vector2(0, -368f), new Vector2(610f, 156f));

            var noteShadow = Panel("Hand note shadow", fillHandRoot,
                new Vector2(-230f, -19f), new Vector2(128f, 104f),
                new Color(0.10f, 0.14f, 0.14f, 0.40f));
            noteShadow.sprite = SoftHandShadowSprite();
            noteShadow.type = Image.Type.Sliced;
            noteShadow.raycastTarget = false;
            fillHandNoteShadow = noteShadow;
            fillHandNoteShadowRect = noteShadow.rectTransform;
            var note = Panel("Hand note paper", fillHandRoot,
                new Vector2(-233f, -15f), new Vector2(118f, 98f), Color.white);
            note.sprite = ConstructionHandArtCatalog.GetCardPaper();
            note.type = Image.Type.Sliced;
            note.pixelsPerUnitMultiplier = note.sprite.rect.height / 184f;
            note.raycastTarget = false;
            fillHandNoteRect = note.rectTransform;
            Panel("Note brass rule", note.transform,
                new Vector2(-55f, 0f), new Vector2(3f, 82f), HandBrass)
                .raycastTarget = false;
            fillHandNote = TextAt("Hand instruction", note.transform,
                new Vector2(4f, 0f), new Vector2(104f, 86f), "", 12, true,
                HandInk, TextAnchor.MiddleLeft);
            fillHandNote.raycastTarget = false;

            // Image first, one short footer second. Fine trim and a soft cast
            // shadow make these read as physical cards instead of UI tiles.
            for (var index = 0; index < fillHandCards.Length; index++)
            {
                var slot = index;
                var x = (index - 1) * 154f - 74f;
                var shadow = Panel($"Hand card {index + 1} shadow", fillHandRoot,
                    new Vector2(x + 7f, -10f), new Vector2(166, 205),
                    new Color(0.09f, 0.13f, 0.13f, 0.47f));
                shadow.sprite = SoftHandShadowSprite();
                shadow.type = Image.Type.Sliced;
                shadow.raycastTarget = false;
                fillHandShadowImages[index] = shadow;
                fillHandCardShadows[index] = shadow.rectTransform;
                shadow.rectTransform.localRotation = Quaternion.Euler(0, 0,
                    index == 0 ? 4.5f : index == 2 ? -4.5f : 0f);

                var card = ButtonAt($"Fill hand card {index + 1}", fillHandRoot,
                    new Vector2(x, 0), new Vector2(144, 184), "", 12,
                    HandQuietBrass, HandPaper,
                    () => SelectFillHandCard(slot), themed: false);
                var cardImage = card.GetComponent<Image>();
                cardImage.sprite = ConstructionHandArtCatalog.GetCardPaper();
                cardImage.type = Image.Type.Sliced;
                cardImage.pixelsPerUnitMultiplier = cardImage.sprite.rect.height / 184f;
                card.transform.localRotation = Quaternion.Euler(0, 0,
                    index == 0 ? 4.5f : index == 2 ? -4.5f : 0f);
                var cardColors = card.colors;
                cardColors.normalColor = Color.white;
                cardColors.highlightedColor = Color.white;
                cardColors.selectedColor = Color.white;
                cardColors.disabledColor = Color.white;
                cardColors.pressedColor = new Color(0.94f, 0.91f, 0.84f);
                card.colors = cardColors;
                fillHandCards[index] = card;
                fillHandGroups[index] = card.gameObject.AddComponent<CanvasGroup>();
                var face = Panel("Cardstock face", card.transform,
                    Vector2.zero, new Vector2(140, 180), Color.white);
                face.sprite = ConstructionHandArtCatalog.GetCardPaper();
                face.type = Image.Type.Sliced;
                face.pixelsPerUnitMultiplier = face.sprite.rect.height / 180f;
                face.raycastTarget = false;
                fillHandFaces[index] = face;
                fillHandArtMats[index] = Panel("Card illustration field",
                    card.transform, new Vector2(0f, 20f),
                    new Vector2(134f, 128f), HandPaper);
                fillHandArtMats[index].raycastTarget = false;
                var icon = Panel("Hand room icon", card.transform,
                    new Vector2(0f, 20f), new Vector2(132f, 126f), Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                fillHandIcons[index] = icon;
                Panel("Card footer rule", card.transform,
                    new Vector2(0f, -47f), new Vector2(126f, 1f),
                    new Color(0.76f, 0.60f, 0.37f, 0.65f))
                    .raycastTarget = false;
                fillHandNames[index] = TextAt("Hand room name", card.transform,
                    new Vector2(-19f, -68f), new Vector2(91f, 36f), "", 18,
                    true, HandInk, TextAnchor.MiddleLeft);
                fillHandNames[index].raycastTarget = false;
                for (var iconIndex = 0; iconIndex < 2; iconIndex++)
                {
                    var footer = Panel($"Hand footer icon {iconIndex + 1}",
                        card.transform, new Vector2(43f + iconIndex * 17f, -68f),
                        new Vector2(20f, 20f), Color.white);
                    footer.preserveAspect = true;
                    footer.raycastTarget = false;
                    fillHandFooterIcons[index, iconIndex] = footer;
                }
                fillHandStates[index] = TextAt("Hand room status", card.transform,
                    new Vector2(0f, -68f), new Vector2(112f, 16f), "", 10, false,
                    HandInk, TextAnchor.MiddleCenter);
                fillHandStates[index].raycastTarget = false;
                fillHandStates[index].gameObject.SetActive(false);
                fillHandSelectionTabs[index] = Panel("Selected card tab",
                    card.transform, new Vector2(0f, 89f),
                    new Vector2(88f, 2f), new Color(1f, 0.89f, 0.61f, 0.9f));
                fillHandSelectionTabs[index].raycastTarget = false;
                fillHandSelectionTabs[index].gameObject.SetActive(false);
            }

            var deckShadow = Panel("Draw pile shadow", fillHandRoot,
                new Vector2(FillHandDeckX + 7f, -10f),
                new Vector2(166f, 205f),
                new Color(0.10f, 0.13f, 0.13f, 0.42f));
            deckShadow.sprite = SoftHandShadowSprite();
            deckShadow.type = Image.Type.Sliced;
            deckShadow.raycastTarget = false;
            fillHandDeckShadow = deckShadow;
            fillHandDeckShadowRect = deckShadow.rectTransform;
            // The pile is a stack of the same portrait cardstock as the hand.
            // The oak print belongs on the back, without a separate sleeve.
            var secondBack = Panel("Lower card in draw pile", fillHandRoot,
                new Vector2(FillHandDeckX + 10f, -8f),
                new Vector2(144f, 184f), Color.white);
            secondBack.sprite = ConstructionHandArtCatalog.GetCardPaper();
            secondBack.type = Image.Type.Sliced;
            secondBack.pixelsPerUnitMultiplier = secondBack.sprite.rect.height / 184f;
            secondBack.raycastTarget = false;
            secondBack.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -2.5f);
            fillHandDeckSecondBackRect = secondBack.rectTransform;
            var deckBack = Panel("Middle card in draw pile", fillHandRoot,
                new Vector2(FillHandDeckX + 5f, -4f),
                new Vector2(142f, 182f), Color.white);
            deckBack.sprite = ConstructionHandArtCatalog.GetCardPaper();
            deckBack.type = Image.Type.Sliced;
            deckBack.pixelsPerUnitMultiplier = deckBack.sprite.rect.height / 182f;
            deckBack.raycastTarget = false;
            deckBack.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, 1.5f);
            fillHandDeckBackRect = deckBack.rectTransform;
            var brassRim = Panel("Top card ink edge", fillHandRoot,
                new Vector2(FillHandDeckX, 0f),
                new Vector2(144f, 184f), HandInk);
            brassRim.sprite = ConstructionHandArtCatalog.GetCardPaper();
            brassRim.type = Image.Type.Sliced;
            brassRim.pixelsPerUnitMultiplier = brassRim.sprite.rect.height / 184f;
            brassRim.raycastTarget = false;
            var deck = Panel("Top card paper edge", brassRim.transform,
                Vector2.zero, new Vector2(140f, 180f), Color.white);
            deck.sprite = ConstructionHandArtCatalog.GetCardPaper();
            deck.type = Image.Type.Sliced;
            deck.pixelsPerUnitMultiplier = deck.sprite.rect.height / 180f;
            deck.raycastTarget = false;
            fillHandDeckRect = brassRim.rectTransform;
            var deckPrint = Panel("Oak town card back", deck.transform,
                new Vector2(0f, 34f), new Vector2(132f, 100f), Color.white);
            deckPrint.sprite = ConstructionHandArtCatalog.GetDeck();
            deckPrint.preserveAspect = true;
            deckPrint.raycastTarget = false;
            Panel("Deck footer rule", deck.transform,
                new Vector2(0f, -31f), new Vector2(126f, 1f),
                new Color(0.76f, 0.60f, 0.37f, 0.65f))
                .raycastTarget = false;
            fillHandDeckTitle = TextAt("Draw pile title", deck.transform,
                new Vector2(-27f, -44f), new Vector2(72f, 36f), "", 17,
                true, HandInk, TextAnchor.MiddleLeft);
            fillHandDeckTitle.raycastTarget = false;
            var countTab = Panel("Deck footer divider", deck.transform,
                new Vector2(16f, -44f), new Vector2(1f, 20f),
                HandQuietBrass);
            countTab.raycastTarget = false;
            fillHandDeckCountTab = countTab.rectTransform;
            fillHandDeck = TextAt("Remaining cards on draw pile",
                deck.transform, new Vector2(43f, -44f),
                new Vector2(54f, 34f), "", 20, true, HandInk,
                TextAnchor.MiddleRight);
            fillHandDeck.raycastTarget = false;
            fillHandReroll = ButtonAt("Reroll selected hand card", fillHandRoot,
                new Vector2(FillHandDeckX, -51f),
                new Vector2(92f, 26f), "换牌 ×0", 16,
                HandInk, Color.white, RerollSelectedFillHandCard,
                themed: false);
            var rerollImage = fillHandReroll.GetComponent<Image>();
            rerollImage.sprite = ConstructionHandArtCatalog.GetReroll();
            rerollImage.color = Color.white;
            rerollImage.type = Image.Type.Sliced;
            rerollImage.pixelsPerUnitMultiplier = rerollImage.sprite.rect.height / 22f;
            var rerollColors = fillHandReroll.colors;
            rerollColors.normalColor = Color.white;
            rerollColors.highlightedColor = Color.white;
            rerollColors.pressedColor = new Color(0.88f, 0.84f, 0.74f, 1f);
            rerollColors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            fillHandReroll.colors = rerollColors;
            fillHandReroll.GetComponentInChildren<Text>().color = HandPaper;
            AttachHover(fillHandReroll.gameObject, () => null,
                () => L("Swap the selected card", "更换当前手牌"),
                () => L(
                    (run.FillHandStartingRerolls > 0
                        ? $"Start with {run.FillHandStartingRerolls} swaps. " : "") +
                    "The first time a squirrel, resident pigeon or hedgehog eats, that species earns one swap. In free play, draw a different card with a valid plot.",
                    (run.FillHandStartingRerolls > 0
                        ? $"开局有 {run.FillHandStartingRerolls} 次换牌；" : "") +
                    "松鼠、常驻鸽子和刺猬首次进食各增加 1 次。可将选中的手牌换成有落点的另一类。"));
        }

        private void RerollSelectedFillHandCard()
        {
            if (run == null || hasPendingPlacement || AwaitingFreeBuildChoice ||
                !run.TryRerollFillHandCard(selectedCategory,
                    out var replacement)) return;
            selectedCategory = replacement;
            selectedPlanting = GreenPlanting.None;
            freeBuildChoiceMade = true;
            fillHandRerollPulseTime = Time.unscaledTime;
            Save();
            Refresh();
        }

        private void SelectFillHandCard(int index)
        {
            if (run?.UsesFillHand != true || run.IsFinished) return;
            var cards = run.FillHandCards;
            if (index < 0 || index >= cards.Count) return;
            var category = cards[index];
            if (!run.IsFillHandCardPlayable(category)) return;
            TrySelect(category, category == ConstructionCategory.Green &&
                run.BuiltCount == 0 ? GreenPlanting.Oak : GreenPlanting.None);
        }

        private void SyncFillHandSelection()
        {
            if (run?.UsesFillHand != true || run.IsFinished) return;
            var cards = run.FillHandCards;
            if (cards.Count == 0) return;
            if (run.IsIntroPhase)
            {
                var active = Mathf.Clamp(run.FillHandActiveIndex, 0,
                    cards.Count - 1);
                selectedCategory = cards[active];
                selectedPlanting = run.BuiltCount == 0 &&
                    selectedCategory == ConstructionCategory.Green
                    ? GreenPlanting.Oak : GreenPlanting.None;
                introChoiceMade = true;
            }
            // Free play waits for a deliberate hand-card click.
            // Replenishing the hand must not choose the next building.
        }

        private void RefreshFillHand()
        {
            if (fillHandRoot == null || run == null) return;
            RefreshFillIntroGuide();
            fillHandRoot.gameObject.SetActive(run.UsesFillHand && !run.IsFinished);
            if (!run.UsesFillHand || run.IsFinished) return;
            var cards = run.FillHandCards;
            var intro = run.IsIntroPhase;
            fillHandNoteRect.gameObject.SetActive(intro);
            fillHandNoteShadowRect.gameObject.SetActive(intro);
            fillHandNote.fontSize = 16;
            if (intro)
                fillHandNote.text = run.BuiltCount == 0
                    ? L("1/8\nOak in centre\nThen build",
                        "第 1/8 张\n橡树放正中\n点建造确认")
                    : L($"CARD {run.IntroTypesBuilt + 1}/8\nPick a lit plot\nThen confirm",
                        $"第 {run.IntroTypesBuilt + 1}/8 张\n选高亮格\n再确认建造");
            fillHandDeckTitle.text = L("DECK", "牌堆");
            fillHandDeckTitle.gameObject.SetActive(false);
            SetRect(fillHandDeck.rectTransform, new Vector2(0f, -38f), new Vector2(126f, 34f));
            fillHandDeck.text = intro
                ? L("Lesson deck", "教程牌堆")
                : run.UsesFiniteFillDeck
                    ? L($"Deck {run.FillDeckCardsRemaining}",
                        $"牌堆 {run.FillDeckCardsRemaining}")
                    : L("RANDOM", "随机补牌");
            fillHandDeck.fontSize = 20;
            var handSize = intro ? 3 : Mathf.Max(1, cards.Count);
            var handWidth = handSize * HandCardWidth + (handSize - 1) * HandCardGap;
            var noteWidth = intro ? 118f + 16f : 0f;
            const float trayPadding = 22f;
            var trayWidth = trayPadding * 2f + noteWidth + handWidth + 20f + HandCardWidth;
            fillHandRoot.sizeDelta = new Vector2(trayWidth, 156f);
            fillHandWood.rectTransform.sizeDelta = fillHandRoot.sizeDelta;
            var noteX = -trayWidth * 0.5f + trayPadding + 59f;
            var firstCardX = -trayWidth * 0.5f + trayPadding + noteWidth + HandCardWidth * 0.5f;
            var deckX = firstCardX + (handSize - 1) *
                (HandCardWidth + HandCardGap) + HandCardWidth + 20f;
            fillHandDeckPositionX = deckX;
            fillHandDeckBaseScale = HandCardScale;
            fillHandNoteShadowRect.anchoredPosition =
                new Vector2(noteX + 3f, -19f);
            fillHandNoteRect.anchoredPosition = new Vector2(noteX, -15f);
            fillHandDeckShadowRect.anchoredPosition =
                new Vector2(deckX + 3f, -4f);
            fillHandDeckShadowRect.localScale =
                Vector3.one * fillHandDeckBaseScale;
            fillHandDeckSecondBackRect.anchoredPosition =
                new Vector2(deckX + 6f, -4f);
            fillHandDeckSecondBackRect.localScale =
                Vector3.one * fillHandDeckBaseScale;
            fillHandDeckBackRect.anchoredPosition =
                new Vector2(deckX + 3f, -2f);
            fillHandDeckBackRect.localScale =
                Vector3.one * fillHandDeckBaseScale;
            fillHandDeckRect.anchoredPosition = new Vector2(deckX, 0f);
            fillHandDeckRect.localScale =
                Vector3.one * fillHandDeckBaseScale;
            fillHandDeckCountTab.gameObject.SetActive(false);
            fillHandDeck.gameObject.SetActive(true);
            fillHandReroll.gameObject.SetActive(!intro && run.UsesFiniteFillDeck);
            if (!intro && run.UsesFiniteFillDeck)
            {
                fillHandReroll.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(deckX, -51f);
                fillHandReroll.GetComponent<RectTransform>().sizeDelta =
                    new Vector2(92f, 26f);
                fillHandReroll.GetComponentInChildren<Text>().text =
                    L($"SWAP ×{run.FillHandRerollsRemaining}",
                        $"换牌 ×{run.FillHandRerollsRemaining}");
                fillHandReroll.interactable = !hasPendingPlacement &&
                    !AwaitingFreeBuildChoice &&
                    run.CanRerollFillHandCard(selectedCategory);
            }
            for (var index = 0; index < fillHandCards.Length; index++)
            {
                var visible = index < cards.Count;
                var button = fillHandCards[index];
                button.gameObject.SetActive(visible);
                fillHandCardShadows[index].gameObject.SetActive(visible);
                if (!visible) continue;
                var category = cards[index];
                var active = run.IsFillHandCardPlayable(category);
                var selected = active && !AwaitingFreeBuildChoice &&
                    category == selectedCategory;
                var completed = intro && index < run.FillHandActiveIndex;
                var starterOak = intro && run.IntroTypesBuilt < 3 && index == 0;
                var planting = starterOak ? GreenPlanting.Oak :
                    GreenPlanting.None;
                var plots = active ? AvailablePlotCount(category, planting) : 0;
                fillHandIcons[index].sprite =
                    ConstructionHandArtCatalog.GetRoom(category, planting) ??
                    ConstructionIconCatalog.GetBuild(category, planting);
                fillHandNames[index].fontSize = Chinese ? 22 : 18;
                fillHandNames[index].text = starterOak
                    ? L("Oak", "中央橡树")
                    : !Chinese && category == ConstructionCategory.Supermarket
                        ? "Market" : !Chinese && category == ConstructionCategory.Waste
                            ? "Waste" : CategoryLabel(category);
                var footerIcon = category switch
                {
                    ConstructionCategory.Green => HandFooterIcon.Sprout,
                    ConstructionCategory.Waste => HandFooterIcon.Bin,
                    ConstructionCategory.Street => HandFooterIcon.Route,
                    _ => HandFooterIcon.Person
                };
                var footerCount = category is ConstructionCategory.Workshop or
                    ConstructionCategory.Square ? 2 : 1;
                for (var iconIndex = 0; iconIndex < 2; iconIndex++)
                {
                    var footer = fillHandFooterIcons[index, iconIndex];
                    footer.gameObject.SetActive(iconIndex < footerCount);
                    if (iconIndex >= footerCount) continue;
                    footer.sprite = ConstructionHandArtCatalog.GetFooter(footerIcon);
                    footer.rectTransform.anchoredPosition =
                        new Vector2(footerCount == 1 ? 52f :
                            41f + iconIndex * 19f, -68f);
                }
                fillHandStates[index].text = completed
                    ? L("BUILT", "已建")
                    : intro && !active
                        ? L("COMING NEXT", "稍后登场")
                        : active && plots > 0
                            ? L($"{plots} plots", $"可放 {plots} 处")
                            : L("NO VALID PLOT", "暂无落点");
                button.interactable = active && plots > 0 &&
                    !hasPendingPlacement;
                if (!intro && active && !hasPendingPlacement)
                    button.interactable = true;
                fillHandGroups[index].alpha = completed ? 0.86f : 1f;
                button.GetComponent<Image>().color =
                    selected ? HandBrass : HandQuietBrass;
                fillHandFaces[index].color = Color.white;
                fillHandArtMats[index].color = HandPaper;
                fillHandIcons[index].color = intro && !active
                    ? new Color(0.94f, 0.93f, 0.89f) : Color.white;
                fillHandSelectionTabs[index].gameObject.SetActive(selected);
                const float angle = 0f;
                fillHandTargetAngles[index] = angle;
                fillHandTargetScales[index] = fillHandDeckBaseScale;
                fillHandTargetPositions[index] = new Vector2(
                    firstCardX + index * (HandCardWidth + HandCardGap),
                    selected ? 6f : 0f);
                if (!fillHandCategoryKnown[index] ||
                    fillHandPreviousCategories[index] != category)
                {
                    fillHandCategoryKnown[index] = true;
                    fillHandPreviousCategories[index] = category;
                    fillHandDealing[index] = true;
                    fillHandDealTimes[index] = Time.unscaledTime +
                        (intro ? index * 0.055f : 0f);
                    button.GetComponent<RectTransform>().anchoredPosition =
                        new Vector2(deckX, 0f);
                    button.transform.localScale = Vector3.one *
                        (fillHandDeckBaseScale * 0.82f);
                    button.transform.localRotation =
                        Quaternion.Euler(0f, 0f, -7f);
                }
                if (selected) button.transform.SetAsLastSibling();
            }
            RefreshFillRightHandPreview();
        }

        private void UpdateFillHandMotion()
        {
            if (fillHandRoot == null || !fillHandRoot.gameObject.activeInHierarchy)
                return;
            var now = Time.unscaledTime;
            var follow = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 13f);
            for (var index = 0; index < fillHandCards.Length; index++)
            {
                var card = fillHandCards[index];
                if (card == null || !card.gameObject.activeSelf) continue;
                var rect = (RectTransform)card.transform;
                Vector2 position;
                float angle;
                float scale;
                if (fillHandDealing[index])
                {
                    if (now < fillHandDealTimes[index]) continue;
                    var t = Mathf.Clamp01((now - fillHandDealTimes[index]) /
                        0.38f);
                    var ease = 1f - Mathf.Pow(1f - t, 3f);
                    position = Vector2.Lerp(new Vector2(fillHandDeckPositionX, 0f),
                        fillHandTargetPositions[index], ease);
                    position.y += Mathf.Sin(t * Mathf.PI) * 7f;
                    angle = Mathf.Lerp(-7f, fillHandTargetAngles[index], ease);
                    scale = Mathf.Lerp(fillHandDeckBaseScale * 0.82f,
                        fillHandTargetScales[index], ease);
                    if (t >= 1f) fillHandDealing[index] = false;
                }
                else
                {
                    position = Vector2.Lerp(rect.anchoredPosition,
                        fillHandTargetPositions[index], follow);
                    angle = Mathf.LerpAngle(rect.localEulerAngles.z,
                        fillHandTargetAngles[index], follow);
                    scale = Mathf.Lerp(rect.localScale.x,
                        fillHandTargetScales[index], follow);
                }
                rect.anchoredPosition = position;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                rect.localScale = Vector3.one * scale;
                var shadow = fillHandCardShadows[index];
                shadow.anchoredPosition = position + new Vector2(3f, -4f);
                shadow.localRotation = rect.localRotation;
                shadow.localScale = Vector3.one * scale;
                fillHandShadowImages[index].color = new Color(0.09f,
                    0.13f, 0.13f, fillHandTargetScales[index] > 1f
                        ? 0.57f : 0.43f);
            }

            var pulse = Mathf.Clamp01((now - fillHandRerollPulseTime) / 0.24f);
            var deckScale = 1f + (1f - pulse) * 0.055f;
            if (fillHandDeckRect != null)
                fillHandDeckRect.localScale = Vector3.one *
                    fillHandDeckBaseScale * deckScale;
            if (fillHandReroll != null)
            {
                var buttonScale = 1f - Mathf.Sin(pulse * Mathf.PI) * 0.065f;
                fillHandReroll.transform.localScale = Vector3.one * buttonScale;
            }
        }

        private static Color HexHandColor(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out var color)
                ? color : Color.white;
        }

        // A nine-sliced alpha falloff keeps every card shadow soft at any UI
        // scale without stretching the corner blur into square blocks.
        private static Sprite SoftHandShadowSprite()
        {
            if (fillHandSoftShadowSprite != null)
                return fillHandSoftShadowSprite;
            const int side = 96;
            var texture = new Texture2D(side, side,
                TextureFormat.RGBA32, false)
            {
                name = "Soft card contact shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[side * side];
            for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
            {
                var dx = Mathf.Max(24 - x, x - 71, 0);
                var dy = Mathf.Max(24 - y, y - 71, 0);
                var distanceSquared = dx * dx + dy * dy;
                var alpha = Mathf.Exp(-distanceSquared / 162f);
                pixels[y * side + x] = new Color32(255, 255, 255,
                    (byte)Mathf.RoundToInt(190f * alpha));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            fillHandSoftShadowSprite = Sprite.Create(texture,
                new Rect(0f, 0f, side, side), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(24f, 24f, 24f, 24f));
            fillHandSoftShadowSprite.name = "Soft card shadow 9 slice";
            return fillHandSoftShadowSprite;
        }

        private string FillHandRoomFunction()
        {
            if (run.BuiltCount == 0)
                return L("Place the oak in the centre. It preserves a home and food for the squirrel already living here.",
                    "把橡树放在正中间，为原本生活在这里的松鼠留下家和食物。");
            return selectedCategory switch
            {
                ConstructionCategory.Green => L(
                    "Plant type follows the plot, not the day. Hover to see its type and maturity date; habitat level does not speed up growth.",
                    "类型由落点决定，不按天数解锁。悬停看类型和成熟日期；连片升级不会提前成熟。"),
                ConstructionCategory.Residence => L(
                    "Place beside a built plot. Click the home, then Set work & meals on the left to choose reachable destinations.",
                    "贴已建格放置，住进 1 人。点击住宅，再点左侧“安排工作与用餐”，选择可达的岗位和餐位。"),
                ConstructionCategory.Restaurant => L(
                    "Place beside a home or its street. Offers one job and two meal seats.",
                    "贴住宅或住宅连通街道；提供 1 个岗位和 2 个餐位。"),
                ConstructionCategory.Workshop => L(
                    "Place beside a home or its street. Adds two reachable jobs.",
                    "贴住宅或住宅连通街道；增加 2 个可达岗位。"),
                ConstructionCategory.Street => L(
                    "Extend from a home or connected street. Routes need shared sides; traffic risks wildlife.",
                    "从住宅或已通街道延伸；路线只沿共边接通，临路动物有车祸风险。"),
                ConstructionCategory.Waste => L(
                    "Place beside a home, restaurant or linked street. Clears nearby waste.",
                    "贴住宅、餐厅或连通街道；清运附近积压垃圾。"),
                ConstructionCategory.Supermarket => L(
                    "Two meal seats, one job. Dining leaves 1 wildlife portion today; hungry wildlife with access can eat it and reduce waste.",
                    "提供 2 餐位、1 岗位。当天有人用餐留 1 份食物；未吃饱且能到达的动物可取食，减少垃圾。"),
                ConstructionCategory.Square => L(
                    "In this lesson, connect the plaza to a home or its street. A resident who worked and ate can visit and leave wildlife food.",
                    "广场接通住宅后，完成上班和用餐的居民可到访，并留下动物食物。每 3 座广场增加 1 张手牌上限。"),
                _ => string.Empty
            };
        }
    }
}
