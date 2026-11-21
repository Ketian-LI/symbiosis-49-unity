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
        private Text readingAwardProgress, readingChangeHeading;
        private readonly Text[] readingChanges = new Text[3];
        private readonly Text[] readingChangeSymbols = new Text[3];
        private RectTransform readingChangeRoot;

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
            SetRect(oldRule.GetComponent<RectTransform>(), new Vector2(-12f, -149f), new Vector2(260f, 1f));

            var paper = Panel("Recent observed changes", guidePanel.transform.parent,
                new Vector2(CaseLeafCenter - 10f, -220f), new Vector2(288f, 112f), ReadingPaper);
            paper.raycastTarget = false;
            readingChangeRoot = paper.rectTransform;
            Panel("Recent changes rule", paper.transform, new Vector2(0f, 55f),
                new Vector2(272f, 1f), HandQuietBrass).raycastTarget = false;
            readingChangeHeading = TextAt("Recent changes heading", paper.transform,
                new Vector2(0f, 40f), new Vector2(264f, 24f), "", 16, true, Ink, TextAnchor.MiddleLeft);
            for (var index = 0; index < 3; index++)
            {
                readingChangeSymbols[index] = TextAt("Change symbol " + index, paper.transform,
                    new Vector2(-124f, 12f - 26f * index), new Vector2(20f, 26f), "", 16,
                    true, Ink, TextAnchor.MiddleCenter);
                readingChanges[index] = TextAt("Observed change " + index, paper.transform,
                    new Vector2(12f, 12f - 26f * index), new Vector2(240f, 24f), "", 16,
                    false, Ink, TextAnchor.MiddleLeft);
            }
        }

        private void RefreshReadingSummary()
        {
            if (readingAwardProgress == null) return;
            balanceScoreText.gameObject.SetActive(false); // Kept as an animation target only.
            RefreshReadingTotals(run.BalanceScore, run.PlacementScore, run.SpecialTaskScore);
            var changes = ConstructionRecentChanges.For(run).Take(3).ToArray();
            readingChangeRoot.gameObject.SetActive(changes.Length > 0);
            readingChangeHeading.text = L($"Latest changes · day {run.LastScoreDay?.day}", $"最近变化 · 第 {run.LastScoreDay?.day} 天");
            for (var index = 0; index < 3; index++)
            {
                readingChanges[index].gameObject.SetActive(index < changes.Length);
                readingChangeSymbols[index].gameObject.SetActive(index < changes.Length);
                if (index >= changes.Length) continue;
                var change = changes[index];
                // Keep the fallback arrow in its own label: its font metrics must
                // not make TMP truncate the accompanying sentence after the icon.
                readingChangeSymbols[index].text = change.Concern ? "!" : change.Upgrade ? "↑" : "·";
                readingChanges[index].text = L(change.English, change.Chinese);
                readingChanges[index].color = change.Concern ? ReadingWarning : change.Upgrade ? ReadingGreen : Ink;
                readingChangeSymbols[index].color = readingChanges[index].color;
            }
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
