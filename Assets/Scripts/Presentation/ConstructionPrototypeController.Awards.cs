using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private static readonly ConstructionAwardTier[] AwardOrder =
        {
            ConstructionAwardTier.Gold, ConstructionAwardTier.Silver,
            ConstructionAwardTier.Bronze, ConstructionAwardTier.Iron,
            ConstructionAwardTier.Plastic
        };

        private RectTransform awardEvaluationRoot;
        private RectTransform awardResultTopDivider;
        private GameObject[] awardLegacySections;
        private Image awardHeroIcon;
        private Text awardName;
        private Text awardTotal;
        private Text awardUnit;
        private Text awardCaption;
        private Text awardFormula;
        private Text awardCompositionHeading;
        private Text awardAnimalHeading;
        private Text awardAnimalScore;
        private Text awardResidentHeading;
        private Text awardResidentScore;
        private Text awardBuildHeading;
        private Text awardBuildScore;
        private Text awardTaskHeading;
        private Text awardTaskScore;
        private Text awardNextTier;
        private readonly Image[] awardRowBackgrounds = new Image[5];
        private readonly GameObject[] awardRowBorders = new GameObject[5];
        private readonly Image[] awardRowIcons = new Image[5];
        private readonly Text[] awardRowNames = new Text[5];
        private readonly Text[] awardRowRanges = new Text[5];
        private readonly Image[] localRankingsAwardIcons = new Image[10];

        private static readonly string[] LegacyAwardSectionNames =
        {
            "Shared score caption border", "Shared score caption paper",
            "Shared score caption", "Result numbers", "Result score divider",
            "Result Animal score", "Result Resident score", "Result score split",
            "Linkage ribbon border", "Linkage ribbon", "Linkage value",
            "Linkage divider", "Linkage detail", "Result events divider",
            "Result event 0", "Result event 1", "Result event 2",
            "Result equation", "Result observed", "Result notice"
        };

        private void BuildAwardEvaluation(Transform resultCard)
        {
            awardResultTopDivider = resultCard.Find("Result top divider")
                .GetComponent<RectTransform>();
            awardLegacySections = new GameObject[LegacyAwardSectionNames.Length];
            for (var i = 0; i < LegacyAwardSectionNames.Length; i++)
            {
                var part = resultCard.Find(LegacyAwardSectionNames[i]);
                awardLegacySections[i] = part != null ? part.gameObject : null;
            }

            awardEvaluationRoot = NewRect("Award evaluation", resultCard,
                Vector2.zero, new Vector2(646, 400));
            // Keep the heading below the panel's painted top clasp. The score
            // breakdown and award bands share the right-hand column.
            Panel("Award column divider", awardEvaluationRoot,
                new Vector2(-37, -22), new Vector2(2, 275), Gold)
                .raycastTarget = false;

            awardHeroIcon = AwardIcon("Award trophy", awardEvaluationRoot,
                new Vector2(-187, 60), new Vector2(110, 110));
            awardName = AwardText("Award name", awardEvaluationRoot,
                new Vector2(-187, -26), new Vector2(260, 36), 26, true,
                TextAnchor.MiddleCenter);
            awardTotal = AwardText("Award total points", awardEvaluationRoot,
                new Vector2(-209, -83), new Vector2(199, 65), 56, true,
                TextAnchor.MiddleRight);
            awardTotal.resizeTextForBestFit = true;
            awardTotal.resizeTextMinSize = 37;
            awardTotal.resizeTextMaxSize = 56;
            awardUnit = AwardText("Award point unit", awardEvaluationRoot,
                new Vector2(-89, -89), new Vector2(38, 38), 20, false,
                TextAnchor.MiddleLeft);
            awardCaption = AwardText("Award total caption", awardEvaluationRoot,
                new Vector2(-187, -130), new Vector2(265, 25), 17, false,
                TextAnchor.MiddleCenter);
            awardFormula = AwardText("Award total formula", awardEvaluationRoot,
                new Vector2(-187, -157), new Vector2(272, 25), 14, false,
                TextAnchor.MiddleCenter);

            awardCompositionHeading = AwardText("Award composition heading",
                awardEvaluationRoot, new Vector2(157, 124),
                new Vector2(310, 28), 20, true, TextAnchor.MiddleLeft);
            AwardRule("Award composition rule", awardEvaluationRoot, 105);
            awardAnimalHeading = AwardText("Award animal heading",
                awardEvaluationRoot, new Vector2(90, 82),
                new Vector2(170, 24), 17, false, TextAnchor.MiddleLeft);
            awardAnimalScore = AwardText("Award animal value",
                awardEvaluationRoot, new Vector2(280, 82),
                new Vector2(73, 24), 20, true, TextAnchor.MiddleRight);
            awardResidentHeading = AwardText("Award resident heading",
                awardEvaluationRoot, new Vector2(90, 55),
                new Vector2(170, 24), 17, false, TextAnchor.MiddleLeft);
            awardResidentScore = AwardText("Award resident value",
                awardEvaluationRoot, new Vector2(280, 55),
                new Vector2(73, 24), 20, true, TextAnchor.MiddleRight);
            awardBuildHeading = AwardText("Award build heading",
                awardEvaluationRoot, new Vector2(90, 28),
                new Vector2(170, 24), 17, false, TextAnchor.MiddleLeft);
            awardBuildScore = AwardText("Award build value",
                awardEvaluationRoot, new Vector2(280, 28),
                new Vector2(73, 24), 20, true, TextAnchor.MiddleRight);
            awardTaskHeading = AwardText("Award task heading",
                awardEvaluationRoot, new Vector2(90, 1),
                new Vector2(170, 24), 17, false, TextAnchor.MiddleLeft);
            awardTaskScore = AwardText("Award task value",
                awardEvaluationRoot, new Vector2(280, 1),
                new Vector2(73, 24), 20, true, TextAnchor.MiddleRight);
            AwardRule("Award thresholds rule", awardEvaluationRoot, -17);

            for (var i = 0; i < AwardOrder.Length; i++)
            {
                var row = Panel("Award threshold " + AwardOrder[i],
                    awardEvaluationRoot, new Vector2(160, -37 - i * 26),
                    new Vector2(320, 25), Color.clear);
                row.raycastTarget = false;
                awardRowBackgrounds[i] = row;
                var border = NewRect("Selected award border", row.transform,
                    Vector2.zero, new Vector2(320, 25));
                AwardBorder(border, 320, 25);
                awardRowBorders[i] = border.gameObject;
                awardRowIcons[i] = AwardIcon("Tier icon", row.transform,
                    new Vector2(-141, 0), new Vector2(21, 21));
                awardRowIcons[i].sprite = AwardSprite(AwardOrder[i]);
                awardRowNames[i] = AwardText("Tier name", row.transform,
                    new Vector2(-35, 0), new Vector2(180, 24), 16,
                    false, TextAnchor.MiddleLeft);
                awardRowRanges[i] = AwardText("Tier range", row.transform,
                    new Vector2(112, 0), new Vector2(85, 24), 15,
                    false, TextAnchor.MiddleRight);
            }
            awardNextTier = AwardText("Next award progress",
                awardEvaluationRoot, new Vector2(160, -166),
                new Vector2(320, 20), 13, false, TextAnchor.MiddleCenter);
            awardNextTier.color = UsedGreen;
            awardEvaluationRoot.gameObject.SetActive(false);
            BuildAwardCardLayout(resultCard);
        }

        private void RefreshAwardEvaluation(ConstructionFinalResult result)
        {
            if (awardEvaluationRoot == null || run == null) return;
            var eligible = ConstructionAwardModel.TryEvaluate(run.Mode,
                run.ScoringVersion, result.DaysPlayed, run.EndReason,
                run.UsesOverallScore ? run.OverallScore :
                    result.BalancePointsTotal, out var tier);
            foreach (var section in awardLegacySections)
            {
                if (section == null) continue;
                var active = !eligible;
                if (section == resultLinkageDivider.gameObject ||
                    section == resultLinkageDetail.gameObject)
                    active &= run.HasLinkageScore;
                section.SetActive(active);
            }
            awardEvaluationRoot.gameObject.SetActive(eligible);
            if (!eligible)
            {
                // Keep the title inside the painted frame even when a mode
                // has no fixed Fill award.
                resultTitleLabel.rectTransform.anchoredPosition =
                    new Vector2(0, 250);
                resultModeLabel.rectTransform.anchoredPosition =
                    new Vector2(0, 213);
                awardResultTopDivider.anchoredPosition = new Vector2(0, 190);
                awardResultTopDivider.sizeDelta = new Vector2(646, 2);
                RefreshAwardCardLayout(result, false, tier);
                return;
            }

            resultTitleLabel.text = L("COEXISTENCE RESULTS", "共栖结算");
            resultModeLabel.text = L("49 PLOTS PLACED · FINAL SCORE",
                "49 格已放置 · 最终得分");
            resultTitleLabel.rectTransform.anchoredPosition =
                new Vector2(0, 215);
            resultModeLabel.rectTransform.anchoredPosition =
                new Vector2(0, 176);
            awardResultTopDivider.anchoredPosition = new Vector2(0, 151);
            awardResultTopDivider.sizeDelta = new Vector2(646, 2);

            // The painted frame has generous transparent margins. Keep the
            // name field, status lines and actions inside its opaque centre.
            var resultCard = awardEvaluationRoot.parent;
            resultCard.Find("Leaderboard divider")
                .GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0, -181);
            resultNetworkStatus.rectTransform.anchoredPosition =
                new Vector2(0, -194);
            resultNicknameInput.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-180, -229);
            resultSubmitButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(70, -229);
            resultViewRankingLabel.GetComponentInParent<Button>(true)
                .GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(238, -229);
            resultLeaderboardStatus.rectTransform.anchoredPosition =
                new Vector2(0, -260);
            foreach (var label in new[] { resultMenuLabel, resultResetLabel })
            {
                var action = label.GetComponentInParent<Button>(true)
                    .GetComponent<RectTransform>();
                action.anchoredPosition = new Vector2(
                    action.anchoredPosition.x, -291);
                action.sizeDelta = new Vector2(action.sizeDelta.x, 40);
            }

            awardHeroIcon.sprite = AwardSprite(tier);
            awardName.text = AwardTierName(tier);
            awardTotal.text = (run.UsesOverallScore ? run.OverallScore :
                result.BalancePointsTotal).ToString(
                CultureInfo.InvariantCulture);
            awardUnit.text = L("pts", "分");
            awardCaption.text = L("TOTAL SCORE", "本局总分");
            awardFormula.text = L("Award based on the total score",
                "奖杯按本局总分评定");
            awardCompositionHeading.text = L("SCORE BREAKDOWN", "分数构成");
            awardAnimalHeading.text = L("Animals", "动物");
            awardAnimalScore.text = result.AnimalPointsTotal.ToString(
                CultureInfo.InvariantCulture);
            awardResidentHeading.text = L("Residents", "居民");
            awardResidentScore.text = result.HumanPointsTotal.ToString(
                CultureInfo.InvariantCulture);
            awardBuildHeading.text = L("Build", "建造联动");
            awardBuildScore.text = run.PlacementScore.ToString(
                CultureInfo.InvariantCulture);
            awardTaskHeading.text = L("Goals", "目标牌");
            awardTaskScore.text = run.SpecialTaskScore.ToString(
                CultureInfo.InvariantCulture);

            for (var i = 0; i < AwardOrder.Length; i++)
            {
                var selected = tier == AwardOrder[i];
                awardRowBackgrounds[i].color = selected
                    ? new Color(Gold.r, Gold.g, Gold.b, 0.16f)
                    : Color.clear;
                awardRowBorders[i].SetActive(selected);
                awardRowNames[i].text = AwardTierName(AwardOrder[i]);
                awardRowNames[i].fontStyle = Chinese && selected
                    ? FontStyle.Bold : FontStyle.Normal;
                awardRowRanges[i].text = AwardRange(AwardOrder[i]);
                awardRowRanges[i].color = selected ? Ink : UsedGreen;
            }
            var next = NextAwardMinimum(tier);
            awardNextTier.text = next == 0
                ? L("Highest award reached", "已达到最高奖项")
                : L($"{next - (run.UsesOverallScore ? run.OverallScore : result.BalancePointsTotal)} points to next award",
                    $"距离下一档还差 {next - (run.UsesOverallScore ? run.OverallScore : result.BalancePointsTotal)} 分");
            RefreshAwardCardLayout(result, true, tier);
        }

        private Text AwardText(string name, Transform parent, Vector2 position,
            Vector2 size, int fontSize, bool bold, TextAnchor alignment)
        {
            var label = TextAt(name, parent, position, size, "", fontSize,
                bold, Ink, alignment);
            label.raycastTarget = false;
            return label;
        }

        private Image AwardIcon(string name, Transform parent, Vector2 position,
            Vector2 size)
        {
            var icon = Panel(name, parent, position, size, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private void AwardRule(string name, Transform parent, float y)
        {
            Panel(name, parent, new Vector2(160, y), new Vector2(320, 2), Gold)
                .raycastTarget = false;
        }

        private void AwardBorder(Transform parent, float width, float height)
        {
            foreach (var y in new[] { -height / 2, height / 2 })
                Panel("Award border horizontal", parent, new Vector2(0, y),
                    new Vector2(width, 2), Gold).raycastTarget = false;
            foreach (var x in new[] { -width / 2, width / 2 })
                Panel("Award border vertical", parent, new Vector2(x, 0),
                    new Vector2(2, height), Gold).raycastTarget = false;
        }

        private static Sprite AwardSprite(ConstructionAwardTier tier) =>
            LoadSprite("UI/Awards/award-" + tier.ToString().ToLowerInvariant() +
                "-v01");

        private string AwardTierName(ConstructionAwardTier tier) => tier switch
        {
            ConstructionAwardTier.Plastic => L("Plastic medal", "塑料奖牌"),
            ConstructionAwardTier.Iron => L("Iron medal", "铁奖牌"),
            ConstructionAwardTier.Bronze => L("Bronze trophy", "铜奖杯"),
            ConstructionAwardTier.Silver => L("Silver trophy", "银奖杯"),
            _ => L("Gold trophy", "金奖杯")
        };

        private string AwardRange(ConstructionAwardTier tier)
        {
            var balanced = run?.ScoringVersion >= 12;
            var iron = balanced ? ConstructionAwardModel.BalancedIronMinimum :
                ConstructionAwardModel.IronMinimum;
            var bronze = balanced ? ConstructionAwardModel.BalancedBronzeMinimum :
                ConstructionAwardModel.BronzeMinimum;
            var silver = balanced ? ConstructionAwardModel.BalancedSilverMinimum :
                ConstructionAwardModel.SilverMinimum;
            return tier switch
            {
                ConstructionAwardTier.Plastic => $"<{iron}",
                ConstructionAwardTier.Iron => $"{iron}–{bronze - 1}",
                ConstructionAwardTier.Bronze => $"{bronze}–{silver - 1}",
                ConstructionAwardTier.Silver =>
                    $"{silver}–{ConstructionAwardModel.GoldMinimum - 1}",
                _ => $"≥{ConstructionAwardModel.GoldMinimum}"
            };
        }

        private int NextAwardMinimum(ConstructionAwardTier tier)
        {
            var balanced = run?.ScoringVersion >= 12;
            return tier switch
            {
                ConstructionAwardTier.Plastic => balanced
                    ? ConstructionAwardModel.BalancedIronMinimum
                    : ConstructionAwardModel.IronMinimum,
                ConstructionAwardTier.Iron => balanced
                    ? ConstructionAwardModel.BalancedBronzeMinimum
                    : ConstructionAwardModel.BronzeMinimum,
                ConstructionAwardTier.Bronze => balanced
                    ? ConstructionAwardModel.BalancedSilverMinimum
                    : ConstructionAwardModel.SilverMinimum,
                ConstructionAwardTier.Silver =>
                    ConstructionAwardModel.GoldMinimum,
                _ => 0
            };
        }
    }
}
