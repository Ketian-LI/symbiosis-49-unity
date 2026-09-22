using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public sealed class ResourcePointCounter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color Graphite = new(0.10f, 0.12f, 0.14f, 0.96f);
        private static readonly Color WarmPaper = new(0.96f, 0.92f, 0.82f, 1f);
        private static readonly Color Amber = new(0.92f, 0.62f, 0.23f, 1f);

        private ResourceEconomyController economy;
        private GameRuntimeController runtime;
        private RectTransform root;
        private Image stateImage;
        private Text balanceText;
        private GameObject detailTag;
        private Text detailText;
        private float transientRemaining;
        private float automaticDetailRemaining;
        private bool pointerInside;

        public void Build(
            RectTransform parent,
            Font font,
            HideFlags hideFlags,
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController)
        {
            runtime = runtimeController;
            economy = economyController;

            root = NewRect("Resource Point Counter", parent, hideFlags);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(22f, -20f);
            root.sizeDelta = new Vector2(132f, 72f);

            stateImage = NewImage("Work Voucher", root, hideFlags);
            stateImage.rectTransform.anchorMin = stateImage.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            stateImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            stateImage.rectTransform.anchoredPosition = Vector2.zero;
            stateImage.rectTransform.sizeDelta = new Vector2(68f, 68f);
            stateImage.preserveAspect = true;
            stateImage.raycastTarget = true;

            balanceText = NewText("Current Balance", root, font, 29, TextAnchor.MiddleLeft, WarmPaper);
            balanceText.fontStyle = FontStyle.Bold;
            balanceText.rectTransform.anchorMin = new Vector2(0f, 0f);
            balanceText.rectTransform.anchorMax = new Vector2(1f, 1f);
            balanceText.rectTransform.offsetMin = new Vector2(68f, 0f);
            balanceText.rectTransform.offsetMax = Vector2.zero;

            var tagImage = NewImage("Resource Detail Tag", root, hideFlags);
            detailTag = tagImage.gameObject;
            tagImage.color = Graphite;
            tagImage.rectTransform.anchorMin = tagImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            tagImage.rectTransform.pivot = new Vector2(0f, 1f);
            tagImage.rectTransform.anchoredPosition = new Vector2(0f, -76f);
            tagImage.rectTransform.sizeDelta = new Vector2(286f, 70f);
            tagImage.raycastTarget = false;

            detailText = NewText("Resource Detail", tagImage.transform, font, 18, TextAnchor.MiddleLeft, WarmPaper);
            detailText.rectTransform.anchorMin = Vector2.zero;
            detailText.rectTransform.anchorMax = Vector2.one;
            detailText.rectTransform.offsetMin = new Vector2(18f, 8f);
            detailText.rectTransform.offsetMax = new Vector2(-14f, -8f);
            detailTag.SetActive(false);

            economy.StateChanged += Refresh;
            economy.ResourceGained += HandleGain;
            economy.ResourceSpent += HandleSpend;
            economy.InsufficientResources += HandleInsufficient;
            economy.FullCapacityReached += HandleFull;
            economy.DaySettled += HandleDaySettled;
            runtime.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (economy != null)
            {
                economy.StateChanged -= Refresh;
                economy.ResourceGained -= HandleGain;
                economy.ResourceSpent -= HandleSpend;
                economy.InsufficientResources -= HandleInsufficient;
                economy.FullCapacityReached -= HandleFull;
                economy.DaySettled -= HandleDaySettled;
            }
            if (runtime != null)
            {
                runtime.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (transientRemaining <= 0f)
            {
                if (automaticDetailRemaining > 0f)
                {
                    automaticDetailRemaining -= Time.unscaledDeltaTime;
                    RefreshDetailVisibility();
                }
                return;
            }

            transientRemaining -= Time.unscaledDeltaTime;
            if (transientRemaining <= 0f)
            {
                ApplyRestingVisual();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            RefreshDetailVisibility();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            RefreshDetailVisibility();
        }

        private void Refresh()
        {
            if (economy == null || balanceText == null)
            {
                return;
            }

            balanceText.text = FormatPoints(economy.Balance);
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            if (automaticDetailRemaining <= 0f)
            {
                detailText.text = chinese
                    ? $"资源点 {FormatPoints(economy.Balance)} / 20\n今日预计  +{FormatPoints(economy.ExpectedDailyIncome)}  −{economy.ExpectedDailySpending}"
                    : $"Resources {FormatPoints(economy.Balance)} / 20\nExpected  +{FormatPoints(economy.ExpectedDailyIncome)}  −{economy.ExpectedDailySpending}";
            }
            if (transientRemaining <= 0f)
            {
                ApplyRestingVisual();
            }
            RefreshDetailVisibility();
        }

        private void HandleGain(float amount)
        {
            PlayTransient(ResourcePointVisual.Gain);
        }

        private void HandleSpend(int amount)
        {
            PlayTransient(ResourcePointVisual.Spend);
        }

        private void HandleInsufficient(int missing)
        {
            PlayTransient(ResourcePointVisual.Insufficient, 1.05f);
        }

        private void HandleFull()
        {
            PlayTransient(ResourcePointVisual.Full, 1.05f);
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            detailText.text = chinese
                ? $"日终结算  +{FormatPoints(settlement.Production)}  −{settlement.FoodServiceCost}  = {FormatPoints(settlement.ClosingBalance)}"
                : $"Daily settlement  +{FormatPoints(settlement.Production)}  −{settlement.FoodServiceCost}  = {FormatPoints(settlement.ClosingBalance)}";
            automaticDetailRemaining = 1.8f;
            RefreshDetailVisibility();
        }

        private void PlayTransient(ResourcePointVisual visual, float duration = 0.72f)
        {
            stateImage.sprite = ResourcePointVisualCatalog.GetSprite(visual);
            transientRemaining = duration;
        }

        private void ApplyRestingVisual()
        {
            var visual = economy.Balance >= ResourceEconomyModel.MaximumBalance - 0.001f
                ? ResourcePointVisual.Full
                : ResourcePointVisual.Base;
            stateImage.sprite = ResourcePointVisualCatalog.GetSprite(visual);
        }

        private void RefreshDetailVisibility()
        {
            if (detailTag != null)
            {
                detailTag.SetActive(
                    (pointerInside || automaticDetailRemaining > 0f) &&
                    runtime != null && !runtime.AtDesktop && !runtime.ResultsOpen);
            }
        }

        private static string FormatPoints(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.0");
        }

        private static RectTransform NewRect(string name, Transform parent, HideFlags hideFlags)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            instance.hideFlags = hideFlags;
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }

        private static Image NewImage(string name, Transform parent, HideFlags hideFlags)
        {
            var rect = NewRect(name, parent, hideFlags);
            rect.gameObject.AddComponent<CanvasRenderer>();
            return rect.gameObject.AddComponent<Image>();
        }

        private static Text NewText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var rect = NewRect(name, parent, parent.gameObject.hideFlags);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
