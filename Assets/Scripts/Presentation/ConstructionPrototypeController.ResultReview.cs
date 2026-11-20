using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private bool resultReviewOpen;
        private GameObject resultReviewBar;
        private Text resultReviewHint;
        private Text resultReviewCloseLabel;
        private Text resultReviewReturnLabel;

        private void BuildResultReview(Transform canvas)
        {
            var collapse = ButtonAt("Review finished layout", finalResultPanel.transform,
                Vector2.zero, new Vector2(180, 40), L("Review layout", "收起复盘"), 17, Ink, Paper,
                () => SetResultReview(true));
            var rect = collapse.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.anchoredPosition = new Vector2(-115, -35);
            resultReviewCloseLabel = collapse.GetComponentInChildren<Text>();
            var bar = Panel("Finished layout review", canvas,
                Vector2.zero, new Vector2(620, 48), Ink);
            rect = bar.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -30);
            resultReviewBar = bar.gameObject;
            resultReviewHint = TextAt("Review hint", bar.transform,
                new Vector2(-95, 0), new Vector2(405, 40), "", 16,
                false, Paper, TextAnchor.MiddleCenter);
            resultReviewHint.raycastTarget = false;
            var reopen = ButtonAt("Return to results", bar.transform,
                new Vector2(215, 0), new Vector2(165, 36), L("Return to results", "返回结算"), 16,
                Paper, Ink, () => SetResultReview(false), themed: false);
            resultReviewReturnLabel = reopen.GetComponentInChildren<Text>();
            resultReviewBar.SetActive(false);
        }

        private void SetResultReview(bool reviewing)
        {
            if (run?.FinalResult == null) return;
            resultReviewOpen = reviewing;
            if (localRankingsOverlay != null) localRankingsOverlay.SetActive(false);
            if (networkRankingsOverlay != null) networkRankingsOverlay.SetActive(false);
            if (awardStandardsOverlay != null) awardStandardsOverlay.SetActive(false);
            if (reviewing)
            {
                ClearResetConfirmation();
                ClearFixedInfoSelection();
                HideTooltip();
            }
            Refresh();
        }

        private void RefreshResultReview()
        {
            if (resultReviewBar == null) return;
            resultReviewBar.SetActive(run?.FinalResult != null && resultReviewOpen);
            resultReviewCloseLabel.text = L("Review layout", "收起复盘");
            resultReviewHint.text = L("Reviewing · click plots to inspect",
                "复盘中 · 点击地块查看布局与供给");
            resultReviewReturnLabel.text = L("Return to results", "返回结算");
            if (resultReviewBar.activeSelf) resultReviewBar.transform.SetAsLastSibling();
        }
    }
}
