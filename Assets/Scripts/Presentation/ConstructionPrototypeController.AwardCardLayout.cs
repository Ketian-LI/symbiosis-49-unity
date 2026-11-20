using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private const string FinalCardArt = "UI/ConstructionFinal/";
        private RectTransform awardCardLayoutRoot;
        private GameObject awardStandardsOverlay;
        private Text cardResultTitle;
        private Text cardResultMode;
        private Image cardAwardIcon;
        private Text cardAwardName;
        private Text cardTotalScore;
        private Text cardTotalUnit;
        private Text cardTotalCaption;
        private Text cardCompositionTitle;
        private readonly Text[] cardScoreLabels = new Text[4];
        private readonly Text[] cardScoreValues = new Text[4];
        private Image cardCurrentTierIcon;
        private Text cardCurrentTierLabel;
        private Text cardStandardsLink;
        private Text cardStandardsTitle;
        private Text cardStandardsSubtitle;
        private Text cardStandardsCloseLabel;
        private readonly Image[] cardStandardIcons = new Image[5];
        private readonly Text[] cardStandardNames = new Text[5];
        private readonly Text[] cardStandardRanges = new Text[5];
        private readonly Image[] cardStandardHighlights = new Image[5];

        private void BuildAwardCardLayout(Transform resultCard)
        {
            awardCardLayoutRoot = NewRect("Illustrated result cards", resultCard,
                Vector2.zero, new Vector2(900, 800));
            // This background sits behind the live input and navigation
            // controls already built directly under the result card.
            awardCardLayoutRoot.SetAsFirstSibling();
            var slip = Panel("Result footer paper", awardCardLayoutRoot,
                new Vector2(0, -315), new Vector2(880, 130), Color.white);
            var slipTexture = Resources.Load<Texture2D>(FinalCardArt +
                "settlement-footer-strip-v01");
            if (slipTexture != null)
            {
                // The generated strip is panoramic inside a transparent PNG.
                // Crop its visible paper with a Sprite rect at render time.
                slip.sprite = Sprite.Create(slipTexture,
                    new Rect(0, slipTexture.height * 0.283f,
                        slipTexture.width, slipTexture.height * 0.437f),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            slip.raycastTarget = false;

            CardShadow("Main result card shadow", awardCardLayoutRoot,
                new Vector2(-213, 55), new Vector2(472, 648));
            var main = NewRect("Illustrated main card", awardCardLayoutRoot,
                new Vector2(-220, 65), new Vector2(460, 640));
            main.localEulerAngles = new Vector3(0, 0, 0.4f);
            var mainArt = Panel("Main card print", main, Vector2.zero,
                new Vector2(460, 640), Color.white);
            mainArt.sprite = LoadSprite(FinalCardArt +
                "settlement-main-card-v01");
            mainArt.raycastTarget = false;
            cardResultTitle = CardResultText("Card result title", main,
                new Vector2(0, -74), new Vector2(388, 44), 32, true,
                TextAnchor.MiddleCenter);
            cardResultMode = CardResultText("Card result mode", main,
                new Vector2(0, -115), new Vector2(380, 24), 16, false,
                TextAnchor.MiddleCenter);
            var awardStamp = Panel("Award stamp", main,
                new Vector2(0, -168), new Vector2(190, 38),
                new Color(0.99f, 0.90f, 0.70f, 0.75f));
            awardStamp.raycastTarget = false;
            CardRule("Award stamp top", main, 0, -149, 190);
            CardRule("Award stamp bottom", main, 0, -187, 190);
            cardAwardIcon = Panel("Card award trophy", main,
                new Vector2(-67, -168), new Vector2(27, 27), Color.white);
            cardAwardIcon.preserveAspect = true;
            cardAwardIcon.raycastTarget = false;
            cardAwardName = CardResultText("Card award name", main,
                new Vector2(15, -168), new Vector2(138, 34), 21, true,
                TextAnchor.MiddleCenter);
            cardTotalScore = CardResultText("Card total score", main,
                new Vector2(-12, -234), new Vector2(250, 72), 61, true,
                TextAnchor.MiddleRight);
            cardTotalScore.resizeTextForBestFit = true;
            cardTotalScore.resizeTextMinSize = 40;
            cardTotalScore.resizeTextMaxSize = 61;
            cardTotalUnit = CardResultText("Card score unit", main,
                new Vector2(130, -243), new Vector2(42, 36), 21, false,
                TextAnchor.MiddleLeft);
            cardTotalCaption = CardResultText("Card total caption", main,
                new Vector2(0, -282), new Vector2(260, 25), 17, false,
                TextAnchor.MiddleCenter);

            CardShadow("Breakdown card shadow", awardCardLayoutRoot,
                new Vector2(213, 55), new Vector2(410, 618));
            var breakdown = NewRect("Illustrated breakdown card",
                awardCardLayoutRoot, new Vector2(210, 65),
                new Vector2(400, 610));
            breakdown.localEulerAngles = new Vector3(0, 0, -1.2f);
            var breakdownArt = Panel("Breakdown card print", breakdown,
                Vector2.zero, new Vector2(400, 610), Color.white);
            breakdownArt.sprite = LoadSprite(FinalCardArt +
                "settlement-breakdown-card-v01");
            breakdownArt.raycastTarget = false;
            cardCompositionTitle = CardResultText("Card score heading",
                breakdown, new Vector2(0, 110), new Vector2(340, 36),
                25, true, TextAnchor.MiddleCenter);
            CardRule("Breakdown heading rule", breakdown, 0, 82, 285);
            var rowNames = new[] { "Animals", "Residents", "Build", "Goals" };
            for (var index = 0; index < rowNames.Length; index++)
            {
                var y = 44 - index * 51;
                cardScoreLabels[index] = CardResultText(
                    "Card " + rowNames[index] + " label",
                    breakdown, new Vector2(-68, y), new Vector2(185, 32),
                    18, false, TextAnchor.MiddleLeft);
                cardScoreValues[index] = CardResultText(
                    "Card " + rowNames[index] + " value", breakdown,
                    new Vector2(114, y), new Vector2(98, 32), 21, true,
                    TextAnchor.MiddleRight);
            }
            CardRule("Breakdown award rule", breakdown, 0, -134, 285);
            cardCurrentTierIcon = Panel("Current award icon", breakdown,
                new Vector2(-112, -177), new Vector2(25, 25), Color.white);
            cardCurrentTierIcon.preserveAspect = true;
            cardCurrentTierIcon.raycastTarget = false;
            cardCurrentTierLabel = CardResultText("Current award threshold",
                breakdown, new Vector2(33, -177), new Vector2(260, 32),
                17, false, TextAnchor.MiddleLeft);
            var standards = ButtonAt("Open award standards", breakdown,
                new Vector2(0, -235), new Vector2(255, 32), "", 16,
                Color.clear, Ink, () => OpenAwardStandards(), false);
            standards.GetComponent<Image>().color = Color.clear;
            cardStandardsLink = CardResultText("Award standards link",
                standards.transform, Vector2.zero, new Vector2(255, 29),
                16, false, TextAnchor.MiddleCenter);
            cardStandardsLink.text = L("View award bands  ›", "查看奖杯标准  ›");
            CardRule("Award link underline", breakdown, 0, -253, 142);

            BuildAwardStandardsOverlay();
            awardCardLayoutRoot.gameObject.SetActive(false);
        }

        private void BuildAwardStandardsOverlay()
        {
            awardStandardsOverlay = Panel("Award standards overlay",
                finalResultPanel.transform, Vector2.zero,
                new Vector2(1600, 900),
                new Color(0.05f, 0.09f, 0.11f, 0.82f)).gameObject;
            Stretch(awardStandardsOverlay.GetComponent<RectTransform>());
            var page = Panel("Award standards card",
                awardStandardsOverlay.transform, Vector2.zero,
                new Vector2(400, 610), Color.white);
            page.sprite = LoadSprite(FinalCardArt +
                "settlement-breakdown-card-v01");
            page.raycastTarget = false;
            cardStandardsTitle = CardResultText("Award standards title", page.transform,
                new Vector2(0, 110), new Vector2(345, 38), 25, true,
                TextAnchor.MiddleCenter);
            cardStandardsTitle.text = L("AWARD BANDS", "奖杯标准");
            cardStandardsSubtitle = CardResultText(
                "Award standards subtitle", page.transform,
                new Vector2(0, 72), new Vector2(345, 24), 15, false,
                TextAnchor.MiddleCenter);
            cardStandardsSubtitle.text =
                L("Based on total score", "按本局总分评定");
            for (var index = 0; index < AwardOrder.Length; index++)
            {
                var y = 24 - index * 47;
                cardStandardHighlights[index] = Panel(
                    "Award standard " + AwardOrder[index], page.transform,
                    new Vector2(0, y), new Vector2(322, 39),
                    Color.clear);
                cardStandardHighlights[index].raycastTarget = false;
                cardStandardIcons[index] = Panel("Standard icon",
                    cardStandardHighlights[index].transform,
                    new Vector2(-132, 0), new Vector2(27, 27), Color.white);
                cardStandardIcons[index].sprite = AwardSprite(AwardOrder[index]);
                cardStandardIcons[index].preserveAspect = true;
                cardStandardIcons[index].raycastTarget = false;
                cardStandardNames[index] = CardResultText("Standard name",
                    cardStandardHighlights[index].transform,
                    new Vector2(-42, 0), new Vector2(165, 30), 17,
                    false, TextAnchor.MiddleLeft);
                cardStandardRanges[index] = CardResultText("Standard range",
                    cardStandardHighlights[index].transform,
                    new Vector2(112, 0), new Vector2(86, 30), 16,
                    false, TextAnchor.MiddleRight);
            }
            var close = ButtonAt("Close award standards", page.transform,
                new Vector2(0, -250), new Vector2(230, 41),
                L("Close", "关闭"), 18, Ink, Paper,
                () => awardStandardsOverlay.SetActive(false));
            cardStandardsCloseLabel = close.GetComponentInChildren<Text>();
            cardStandardsCloseLabel.raycastTarget = false;
            awardStandardsOverlay.SetActive(false);
        }

        private void RefreshAwardCardLayout(ConstructionFinalResult result,
            bool eligible, ConstructionAwardTier tier)
        {
            if (awardCardLayoutRoot == null) return;
            var resultCard = awardCardLayoutRoot.parent;
            resultCard.GetComponent<Image>().enabled = !eligible;
            awardCardLayoutRoot.gameObject.SetActive(eligible);
            awardEvaluationRoot.gameObject.SetActive(false);
            resultTitleLabel.gameObject.SetActive(!eligible);
            resultModeLabel.gameObject.SetActive(!eligible);
            awardResultTopDivider.gameObject.SetActive(!eligible);
            resultCard.Find("Leaderboard divider").gameObject.SetActive(!eligible);
            if (!eligible)
            {
                awardStandardsOverlay.SetActive(false);
                RestoreLegacyResultFooter();
                return;
            }

            cardResultTitle.text = L("COEXISTENCE RESULTS", "共栖结算");
            cardResultMode.text = L("49 plots placed", "49 格已放置");
            cardAwardIcon.sprite = AwardSprite(tier);
            cardAwardName.text = AwardTierName(tier);
            cardTotalScore.text = (run.UsesOverallScore ? run.OverallScore :
                result.BalancePointsTotal).ToString(CultureInfo.InvariantCulture);
            cardTotalUnit.text = L("pts", "分");
            cardTotalCaption.text = L("TOTAL SCORE", "本局总分");
            cardCompositionTitle.text = L("SCORE BREAKDOWN", "分数构成");
            var labels = new[]
            {
                L("Animals", "动物"), L("Residents", "居民"),
                L("Build linkage", "建造联动"), L("Goal cards", "目标牌")
            };
            var values = new[]
            {
                result.AnimalPointsTotal, result.HumanPointsTotal,
                run.PlacementScore, run.SpecialTaskScore
            };
            for (var index = 0; index < labels.Length; index++)
            {
                cardScoreLabels[index].text = labels[index];
                cardScoreValues[index].text = index >= 2 && values[index] > 0
                    ? "+" + values[index].ToString(CultureInfo.InvariantCulture)
                    : values[index].ToString(CultureInfo.InvariantCulture);
            }
            cardCurrentTierIcon.sprite = AwardSprite(tier);
            cardCurrentTierLabel.text = AwardTierName(tier) + " · " +
                AwardRange(tier) + L(" pts", " 分");
            cardStandardsLink.text = L("View award bands  ›",
                "查看奖杯标准  ›");
            for (var index = 0; index < AwardOrder.Length; index++)
            {
                cardStandardNames[index].text = AwardTierName(AwardOrder[index]);
                cardStandardRanges[index].text = AwardRange(AwardOrder[index]);
                cardStandardHighlights[index].color = AwardOrder[index] == tier
                    ? new Color(Gold.r, Gold.g, Gold.b, 0.22f)
                    : Color.clear;
            }
            cardStandardsTitle.text = L("AWARD BANDS", "奖杯标准");
            cardStandardsSubtitle.text =
                L("Based on total score", "按本局总分评定");
            cardStandardsCloseLabel.text = L("Close", "关闭");
            LayoutCardResultFooter();
        }

        private void LayoutCardResultFooter()
        {
            resultNetworkStatus.rectTransform.anchoredPosition =
                new Vector2(0, -268);
            resultNetworkStatus.rectTransform.sizeDelta =
                new Vector2(745, 20);
            resultNicknameInput.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-235, -297);
            resultSubmitButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(55, -297);
            resultViewRankingLabel.GetComponentInParent<Button>(true)
                .GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(235, -297);
            resultLeaderboardStatus.rectTransform.anchoredPosition =
                new Vector2(0, -326);
            resultLeaderboardStatus.rectTransform.sizeDelta =
                new Vector2(745, 18);
            foreach (var (label, x) in new[]
            {
                (resultMenuLabel, -195f), (resultResetLabel, 195f)
            })
            {
                var action = label.GetComponentInParent<Button>(true)
                    .GetComponent<RectTransform>();
                action.anchoredPosition = new Vector2(x, -354);
                action.sizeDelta = new Vector2(270, 42);
                var icon = action.Find("Action icon");
                if (icon != null) icon.gameObject.SetActive(false);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = action.sizeDelta;
            }
        }

        private void RestoreLegacyResultFooter()
        {
            resultNetworkStatus.rectTransform.anchoredPosition =
                new Vector2(0, -346);
            resultNetworkStatus.rectTransform.sizeDelta =
                new Vector2(660, 20);
            resultNicknameInput.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-222, -293);
            resultSubmitButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(47, -293);
            resultViewRankingLabel.GetComponentInParent<Button>(true)
                .GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(241, -293);
            resultLeaderboardStatus.rectTransform.anchoredPosition =
                new Vector2(0, -327);
            resultLeaderboardStatus.rectTransform.sizeDelta =
                new Vector2(660, 20);
            foreach (var (label, x) in new[]
            {
                (resultMenuLabel, -151f), (resultResetLabel, 151f)
            })
            {
                var action = label.GetComponentInParent<Button>(true)
                    .GetComponent<RectTransform>();
                action.anchoredPosition = new Vector2(x, -234);
                action.sizeDelta = new Vector2(245, 48);
                var icon = action.Find("Action icon");
                if (icon != null) icon.gameObject.SetActive(true);
                label.rectTransform.anchoredPosition = new Vector2(13, 0);
                label.rectTransform.sizeDelta = new Vector2(209, 48);
            }
        }

        private void CardShadow(string name, Transform parent,
            Vector2 position, Vector2 size)
        {
            var shadow = Panel(name, parent, position, size,
                new Color(0, 0, 0, 0.28f));
            shadow.sprite = SoftHandShadowSprite();
            shadow.type = Image.Type.Sliced;
            shadow.raycastTarget = false;
        }

        private Text CardResultText(string name, Transform parent,
            Vector2 position, Vector2 size, int fontSize, bool bold,
            TextAnchor alignment)
        {
            var label = TextAt(name, parent, position, size, "", fontSize,
                bold, Ink, alignment);
            label.raycastTarget = false;
            return label;
        }

        private void CardRule(string name, Transform parent,
            float x, float y, float width)
        {
            Panel(name, parent, new Vector2(x, y), new Vector2(width, 1.5f),
                Gold).raycastTarget = false;
        }

        private void OpenAwardStandards()
        {
            if (awardStandardsOverlay == null) return;
            awardStandardsOverlay.SetActive(true);
            awardStandardsOverlay.transform.SetAsLastSibling();
        }
    }
}
