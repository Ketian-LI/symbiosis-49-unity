using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        // Semantic ink on warm paper, separate from the bright flying score cues.
        private static readonly Color ReadingBlue = new(0.141f, 0.314f, 0.420f); // #24506B
        private static readonly Color ReadingGreen = new(0.176f, 0.376f, 0.290f); // #2D604A
        private static readonly Color ReadingWarning = new(0.596f, 0.290f, 0.208f); // #984A35
        private readonly Text[] readingScoreLabels = new Text[3];
        private readonly Text[] readingScoreValues = new Text[3];
        private Text readingAwardProgress;

        private void BuildReadingSummary()
        {
            balanceScoreText.gameObject.SetActive(false);
            progressPanel.rectTransform.anchoredPosition = new Vector2(CaseLeafCenter, 255f);
            SetRect(fillRightScoreTitle.rectTransform, new Vector2(-12f, 38f), new Vector2(260f, 38f));
            SetRect(fillRightScoreNumber.rectTransform, new Vector2(-12f, -5f), new Vector2(260f, 94f));
            for (var index = 0; index < 3; index++)
            {
                var x = -100f + index * 88f;
                readingScoreLabels[index] = TextAt("Score component label " + index, progressPanel.transform,
                    new Vector2(x, -61f), new Vector2(84f, 28f), "", 16, false, Ink, TextAnchor.MiddleLeft);
                readingScoreValues[index] = TextAt("Score component value " + index, progressPanel.transform,
                    new Vector2(x, -88f), new Vector2(84f, 32f), "", 21, true, Ink, TextAnchor.MiddleLeft);
            }
            readingScoreValues[0].color = ReadingBlue;
            readingAwardProgress = TextAt("Live award distance", progressPanel.transform,
                new Vector2(-12f, -124f), new Vector2(260f, 34f), "", 16, false, Ink, TextAnchor.MiddleLeft);
            var oldRule = progressPanel.transform.Find("Score summary rule");
            SetRect(oldRule.GetComponent<RectTransform>(), new Vector2(-12f, -190f), new Vector2(260f, 1f));

        }

        private void RefreshReadingSummary()
        {
            if (readingAwardProgress == null) return;
            balanceScoreText.gameObject.SetActive(false); // Kept as an animation target only.
            RefreshReadingTotals(run.BalanceScore, run.PlacementScore, run.SpecialTaskScore);
        }

        private void RefreshReadingTotals(int shared, int build, int goals)
        {
            if (readingAwardProgress == null) return;
            var values = new[] { shared, build, goals };
            var labels = new[] { L("Shared", "共栖"), L("Build", "建造"), L("Goals", "目标") };
            for (var index = 0; index < 3; index++)
            {
                readingScoreLabels[index].text = labels[index];
                readingScoreValues[index].text = values[index].ToString();
                readingScoreValues[index].color = values[index] < 0 ? ReadingWarning : index == 0 ? ReadingBlue : Ink;
            }
            var total = (long)shared + build + goals;
            var next = new[] { ConstructionAwardTier.Iron, ConstructionAwardTier.Bronze,
                ConstructionAwardTier.Silver, ConstructionAwardTier.Gold }.FirstOrDefault(tier =>
                    total < ConstructionAwardModel.MinimumFor(tier, run.ScoringVersion, run.AwardRulesVersion));
            var gap = (long)ConstructionAwardModel.MinimumFor(next, run.ScoringVersion, run.AwardRulesVersion) - total;
            readingAwardProgress.text = next == ConstructionAwardTier.Plastic
                ? L("Gold threshold reached", "已达到金杯分数线")
                : L($"{next}: {gap:N0} pts to go", $"距{AwardTierName(next)}还差 {gap:N0} 分");
        }
    }
}
