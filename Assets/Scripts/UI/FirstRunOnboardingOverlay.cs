using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    public enum OnboardingPromptPhase
    {
        Default,
        WasteReviewed,
        RoomInTray,
        RestoreRoom,
        ConfirmLayout,
        LayoutRetry,
        FeedingActive,
        ReplayFinish
    }

    public sealed class FirstRunOnboardingOverlay : MonoBehaviour
    {
        private Camera worldCamera;
        private RectTransform root;
        private RectTransform spotlight;
        private Image spotlightImage;
        private Button spotlightButton;
        private RectTransform cardRect;
        private Image tutorialShade;
        private RectTransform accentRect;
        private RectTransform skipButtonRect;
        private RectTransform uiTarget;
        private Text header;
        private Text gesture;
        private Text instruction;
        private Text skipLabel;
        private GameObject continueButton;
        private Text continueLabel;
        private GameObject previousButton;
        private Text previousLabel;
        private readonly Graphic[] progressSegments = new Graphic[4];
        private Transform target;
        private OnboardingStep step;
        private string baseInstruction;
        private float feedbackUntil;
        private bool shouldBeVisible;
        private bool suppressed;
        private bool showingGuide;
        private Vector2 cardVelocity;

        public System.Action SkipRequested;
        public System.Action ContinueRequested;
        public System.Action PreviousRequested;
        public System.Action HighlightedResidentRequested;
        public bool IsVisible => root != null && root.gameObject.activeSelf;
        public string CurrentInstruction => instruction != null ? instruction.text : string.Empty;
        public string CurrentHeader => header != null ? header.text : string.Empty;

        public void Build(Font font, Camera camera, HideFlags hideFlags)
        {
            worldCamera = camera;
            root = NewRect("First Run Onboarding", transform, hideFlags);
            Stretch(root);

            tutorialShade = NewImage("Soft Tutorial Shade", root, hideFlags,
                new Color(0.02f, 0.03f, 0.04f, 0.07f));
            Stretch(tutorialShade.rectTransform);
            tutorialShade.raycastTarget = false;

            spotlightImage = NewImage("Tutorial Spotlight", root, hideFlags, new Color(0.42f, 0.96f, 0.90f, 0.96f));
            spotlightImage.sprite = AnimalSelectionVisualCatalog.GetSprite(AnimalSelectionVisual.SelectionRing);
            spotlightImage.preserveAspect = true;
            spotlightImage.raycastTarget = false;
            spotlight = spotlightImage.rectTransform;
            spotlight.sizeDelta = new Vector2(96f, 96f);
            spotlightButton = spotlightImage.gameObject.AddComponent<Button>();
            spotlightButton.targetGraphic = spotlightImage;
            spotlightButton.transition = Selectable.Transition.None;
            spotlightButton.onClick.AddListener(() => HighlightedResidentRequested?.Invoke());
            var outline = spotlight.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.28f, 0.30f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);

            var card = NewImage("One Sentence Tutorial Card", root, hideFlags, new Color(0.10f, 0.15f, 0.17f, 0.76f));
            cardRect = card.rectTransform;
            var panelSprite = TutorialPanelVisualCatalog.GetSprite();
            if (panelSprite != null)
            {
                card.sprite = panelSprite;
                card.type = Image.Type.Sliced;
                // The source artwork already has a translucent center. This
                // tint keeps the board visible without losing text contrast.
                card.color = new Color(1f, 1f, 1f, 0.90f);
            }
            else
            {
                var cardOutline = card.gameObject.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.85f, 0.77f, 0.61f, 0.7f);
                cardOutline.effectDistance = new Vector2(2f, -2f);
            }
            var accent = NewImage("Tutorial Accent", card.transform, hideFlags,
                new Color(0.30f, 0.82f, 0.79f, 1f));
            accentRect = accent.rectTransform;
            SetRect(accentRect, 0f, 0f, 5f, 170f);
            accent.raycastTarget = false;
            accent.gameObject.SetActive(panelSprite == null);

            gesture = NewText("Gesture", card.transform, hideFlags, font, "☝", 29,
                TextAnchor.MiddleCenter);
            SetRect(gesture.rectTransform, 19f, 119f, 36f, 34f);
            header = NewText("Step Progress", card.transform, hideFlags, font, string.Empty, 18,
                TextAnchor.MiddleLeft);
            SetRect(header.rectTransform, 61f, 120f, 280f, 30f);
            instruction = NewText("Instruction", card.transform, hideFlags, font, string.Empty, 18,
                TextAnchor.MiddleLeft);
            instruction.fontStyle = FontStyle.Normal;
            instruction.font = font;
            SetRect(instruction.rectTransform, 22f, 51f, 406f, 68f);
            instruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            instruction.verticalOverflow = VerticalWrapMode.Truncate;

            for (var index = 0; index < progressSegments.Length; index++)
            {
                var segment = NewRounded($"Progress {index + 1}", card.transform, hideFlags,
                    new Color(0.35f, 0.38f, 0.37f, 0.9f), 2.5f);
                SetRect(segment.rectTransform, 23f + index * 41f, 26f, 34f, 5f);
                segment.raycastTarget = false;
                progressSegments[index] = segment;
            }

            continueButton = NewRounded("Continue Tutorial", card.transform, hideFlags,
                new Color(0.22f, 0.48f, 0.52f, 0.78f), 17f).gameObject;
            SetBottomRight(continueButton.GetComponent<RectTransform>(), 119f, 13f, 92f, 36f);
            var continueAction = continueButton.AddComponent<Button>();
            continueAction.targetGraphic = continueButton.GetComponent<RoundedPanelGraphic>();
            StyleButton(continueAction);
            continueAction.onClick.AddListener(() => ContinueRequested?.Invoke());
            continueLabel = NewText("Continue Label", continueButton.transform, hideFlags, font,
                "继续", 16, TextAnchor.MiddleCenter);
            Stretch(continueLabel.rectTransform, 2f);
            continueButton.SetActive(false);

            previousButton = NewRounded("Previous Guide Page", card.transform, hideFlags,
                new Color(0.14f, 0.23f, 0.27f, 0.76f), 17f).gameObject;
            SetBottomRight(previousButton.GetComponent<RectTransform>(), 223f, 13f, 92f, 36f);
            var previousAction = previousButton.AddComponent<Button>();
            previousAction.targetGraphic = previousButton.GetComponent<RoundedPanelGraphic>();
            StyleButton(previousAction);
            previousAction.onClick.AddListener(() => PreviousRequested?.Invoke());
            previousLabel = NewText("Previous Label", previousButton.transform, hideFlags, font,
                "上页", 16, TextAnchor.MiddleCenter);
            Stretch(previousLabel.rectTransform, 2f);
            previousButton.SetActive(false);

            var skip = NewRounded("Skip", card.transform, hideFlags,
                new Color(0.20f, 0.21f, 0.25f, 0.76f), 17f);
            skipButtonRect = skip.rectTransform;
            SetBottomRight(skipButtonRect, 15f, 13f, 92f, 36f);
            var button = skip.gameObject.AddComponent<Button>();
            button.targetGraphic = skip;
            StyleButton(button);
            button.onClick.AddListener(() => SkipRequested?.Invoke());
            skipLabel = NewText("Skip Label", skip.transform, hideFlags, font, "跳过", 18, TextAnchor.MiddleCenter);
            Stretch(skipLabel.rectTransform, 2f);
            PositionCard();
            root.gameObject.SetActive(false);
        }

        public void Show(
            OnboardingStep nextStep,
            Transform worldTarget,
            bool chinese,
            OnboardingPromptPhase phase = OnboardingPromptPhase.Default,
            RectTransform highlightedUi = null,
            string overrideInstruction = null)
        {
            showingGuide = false;
            tutorialShade.raycastTarget = false;
            spotlightImage.raycastTarget = nextStep == OnboardingStep.SelectResident && worldTarget != null;
            spotlightButton.interactable = spotlightImage.raycastTarget;
            step = nextStep;
            target = worldTarget;
            uiTarget = highlightedUi;
            shouldBeVisible = nextStep != OnboardingStep.Hidden && nextStep != OnboardingStep.Complete;
            ApplyVisibility();
            if (!shouldBeVisible)
            {
                return;
            }
            previousButton.SetActive(false);
            foreach (var segment in progressSegments)
            {
                segment.gameObject.SetActive(true);
            }
            gesture.gameObject.SetActive(false);
            instruction.fontSize = 18;
            baseInstruction = overrideInstruction ?? InstructionFor(nextStep, phase, chinese);
            instruction.text = baseInstruction;
            feedbackUntil = 0f;
            var ordinal = nextStep switch
            {
                OnboardingStep.SelectResident => 1,
                OnboardingStep.InspectWaste => 2,
                OnboardingStep.PracticeLayout => 3,
                _ => 4
            };
            var title = nextStep switch
            {
                OnboardingStep.SelectResident => chinese ? "居民路线" : "Resident route",
                OnboardingStep.InspectWaste => chinese ? "垃圾清运" : "Waste collection",
                OnboardingStep.PracticeLayout => chinese ? "移动房间" : "Move a room",
                _ => chinese ? "安全投喂" : "Safe feeding"
            };
            header.text = chinese ? $"新手教程  {ordinal}/4  ·  {title}" : $"Tutorial  {ordinal}/4  ·  {title}";
            skipLabel.text = chinese ? "跳过" : "Skip";
            continueButton.SetActive(phase == OnboardingPromptPhase.WasteReviewed ||
                                     phase == OnboardingPromptPhase.LayoutRetry ||
                                     phase == OnboardingPromptPhase.ReplayFinish);
            continueLabel.text = phase == OnboardingPromptPhase.ReplayFinish
                ? chinese ? "完成" : "Finish"
                : phase == OnboardingPromptPhase.LayoutRetry
                    ? chinese ? "重试" : "Retry"
                : chinese ? "继续" : "Next";
            gesture.text = phase == OnboardingPromptPhase.WasteReviewed ||
                           phase == OnboardingPromptPhase.ReplayFinish
                ? "✓" : "☝";
            for (var index = 0; index < progressSegments.Length; index++)
            {
                progressSegments[index].color = index < ordinal
                    ? new Color(0.32f, 0.84f, 0.80f, 1f)
                    : new Color(0.35f, 0.38f, 0.37f, 0.9f);
            }
            spotlight.gameObject.SetActive(target != null || uiTarget != null);
            spotlight.sizeDelta = phase switch
            {
                OnboardingPromptPhase.RoomInTray => new Vector2(180f, 180f),
                OnboardingPromptPhase.ConfirmLayout => new Vector2(90f, 90f),
                OnboardingPromptPhase.FeedingActive => new Vector2(210f, 210f),
                _ => nextStep switch
                {
                    OnboardingStep.SelectResident => new Vector2(96f, 96f),
                    OnboardingStep.PlaceFood => new Vector2(120f, 120f),
                    _ => new Vector2(156f, 156f)
                }
            };
            UpdateSpotlight();
            PositionCard(true);
        }

        public void ShowGuide(int pageIndex, int pageCount, GameplayGuidePage page,
            Transform worldTarget, bool chinese, RectTransform highlightedUi = null)
        {
            showingGuide = true;
            tutorialShade.raycastTarget = true;
            spotlightImage.raycastTarget = false;
            spotlightButton.interactable = false;
            target = worldTarget;
            uiTarget = highlightedUi;
            shouldBeVisible = true;
            ApplyVisibility();
            baseInstruction = page.Body;
            instruction.text = baseInstruction;
            feedbackUntil = 0f;
            header.text = chinese
                ? $"玩法与区域  {pageIndex + 1}/{pageCount}  ·  {page.Title}"
                : $"Play & areas  {pageIndex + 1}/{pageCount}  ·  {page.Title}";
            gesture.gameObject.SetActive(false);
            skipLabel.text = chinese ? "跳过教程" : "Skip all";
            previousLabel.text = chinese ? "上页" : "Back";
            previousButton.SetActive(pageIndex > 0);
            continueButton.SetActive(true);
            continueLabel.text = pageIndex == pageCount - 1
                ? chinese ? "开始练习" : "Practice"
                : chinese ? "下页" : "Next";
            foreach (var segment in progressSegments)
            {
                segment.gameObject.SetActive(false);
            }
            instruction.fontSize = 17;
            spotlight.gameObject.SetActive(target != null || uiTarget != null);
            spotlight.sizeDelta = uiTarget != null ? new Vector2(112f, 112f) : new Vector2(172f, 172f);
            UpdateSpotlight();
            PositionCard(true);
        }

        public void ShowFeedback(string message)
        {
            if (!shouldBeVisible || instruction == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            instruction.text = message;
            feedbackUntil = Time.unscaledTime + 2.5f;
        }

        public void SetSuppressed(bool value)
        {
            if (suppressed == value)
            {
                return;
            }

            suppressed = value;
            ApplyVisibility();
            if (root != null && root.gameObject.activeSelf)
            {
                UpdateSpotlight();
            }
        }

        private void ApplyVisibility()
        {
            if (root != null)
            {
                root.gameObject.SetActive(shouldBeVisible && !suppressed);
            }
        }

        private void LateUpdate()
        {
            if (root != null && root.gameObject.activeSelf)
            {
                if (feedbackUntil > 0f && Time.unscaledTime >= feedbackUntil)
                {
                    instruction.text = baseInstruction;
                    feedbackUntil = 0f;
                }
                UpdateSpotlight();
                PositionCard();
                if (spotlight.gameObject.activeSelf)
                {
                    var pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.035f;
                    spotlight.localScale = Vector3.one * pulse;
                }
            }
        }

        private void UpdateSpotlight()
        {
            if (!spotlight.gameObject.activeSelf)
            {
                return;
            }
            if (worldCamera == null || (target == null && uiTarget == null))
            {
                spotlight.gameObject.SetActive(false);
                return;
            }
            var screen = uiTarget != null
                ? (Vector3)RectTransformUtility.WorldToScreenPoint(worldCamera, uiTarget.TransformPoint(uiTarget.rect.center))
                : worldCamera.WorldToScreenPoint(target.position + Vector3.up * 0.35f);
            if (uiTarget == null && screen.z <= 0f)
            {
                spotlight.gameObject.SetActive(false);
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, worldCamera, out var local))
            {
                spotlight.anchoredPosition = local;
            }
        }

        private static string InstructionFor(OnboardingStep value, OnboardingPromptPhase phase, bool chinese)
        {
            if (value == OnboardingStep.PracticeLayout)
            {
                return phase switch
                {
                    OnboardingPromptPhase.RoomInTray => chinese
                        ? "托盘已收下房间。把它拖回原来的格子，其他房间先不要移动。"
                        : "The room is in the tray. Return it to its original cell without moving other rooms.",
                    OnboardingPromptPhase.RestoreRoom => chinese
                        ? "还差一步：把练习房间准确放回原位。"
                        : "One more step: return the practice room to its original cell.",
                    OnboardingPromptPhase.ConfirmLayout => chinese
                        ? "房间已归位。点击下方的确认按钮；练习不占用今日免费调整次数。"
                        : "The room is back. Press Confirm below; practice does not use today's free rearrangement.",
                    OnboardingPromptPhase.LayoutRetry => chinese
                        ? "布局练习已取消。点“重试”重新练习，或点“跳过”继续游戏。"
                        : "Layout practice was cancelled. Press Retry, or Skip to continue playing.",
                    _ => chinese
                        ? "把高亮的 1×1 房间拖到左侧托盘；松开鼠标后再放回。"
                        : "Drag the highlighted 1×1 room to the tray on the left, then return it."
                };
            }
            if (value == OnboardingStep.PlaceFood)
            {
                return phase switch
                {
                    OnboardingPromptPhase.FeedingActive => chinese
                        ? "投喂模式已开启。点击房间内的空地放置食物；右键可取消。"
                        : "Feeding mode is on. Click open ground in a room; right-click to cancel.",
                    OnboardingPromptPhase.ReplayFinish => chinese
                        ? "投喂需先点“投喂”，再选空地；每 3 天可用一次。"
                        : "Select Feed, then open ground. Available once every three days.",
                    _ => chinese
                        ? "点击“投喂”按钮，随后选择房间空地；误触时可右键取消。"
                        : "Select Feed, then open ground in a room; right-click to cancel."
                };
            }
            return value switch
            {
                OnboardingStep.SelectResident => chinese ? "点击高亮居民或光圈，查看住处—办公室—餐饮路线。" : "Select the highlighted resident or ring to reveal their home–office–food route.",
                OnboardingStep.InspectWaste => chinese ? "点击高亮垃圾房，查看容量与下一次清运。" : "Select the highlighted waste room to inspect capacity and collection time.",
                _ => string.Empty
            };
        }

        private void PositionCard(bool immediate = false)
        {
            if (cardRect == null || root == null)
            {
                return;
            }
            var viewport = root.rect.width > 0f && root.rect.height > 0f
                ? root.rect
                : new Rect(-960f, -540f, 1920f, 1080f);
            var narrowGuide = showingGuide && viewport.width < 760f;
            var width = showingGuide ? 720f : 500f;
            var height = showingGuide ? narrowGuide ? 350f : 260f : 185f;
            var actualWidth = Mathf.Min(width, viewport.width - 40f);
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(actualWidth, height);
            var focus = spotlight != null && spotlight.gameObject.activeSelf
                ? (Vector2?)spotlight.anchoredPosition
                : null;
            var radius = focus.HasValue
                ? Mathf.Max(spotlight.sizeDelta.x, spotlight.sizeDelta.y) * 0.5f
                : 0f;
            var desired = TutorialCardPlacement.Choose(viewport, cardRect.sizeDelta, focus, radius);
            if (immediate || !Application.isPlaying)
            {
                cardVelocity = Vector2.zero;
                cardRect.anchoredPosition = desired;
            }
            else
            {
                cardRect.anchoredPosition = Vector2.SmoothDamp(
                    cardRect.anchoredPosition, desired, ref cardVelocity,
                    0.14f, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            accentRect.sizeDelta = new Vector2(5f, height);
            var buttonWidth = narrowGuide ? 80f : 92f;
            SetBottomRight(skipButtonRect, 15f, 13f, buttonWidth, 36f);
            SetBottomRight(continueButton.GetComponent<RectTransform>(),
                narrowGuide ? 108f : 119f, 13f, buttonWidth, 36f);
            SetBottomRight(previousButton.GetComponent<RectTransform>(),
                narrowGuide ? 201f : 223f, 13f, buttonWidth, 36f);
            if (showingGuide)
            {
                SetRect(header.rectTransform, 28f, height - 56f,
                    Mathf.Max(170f, actualWidth - 56f), 28f);
                SetRect(instruction.rectTransform, 28f, 62f,
                    Mathf.Max(210f, actualWidth - 56f), height - 128f);
                instruction.fontSize = narrowGuide ? 15 : 17;
            }
            else
            {
                SetRect(header.rectTransform, 28f, height - 55f,
                    Mathf.Max(170f, actualWidth - 56f), 28f);
                SetRect(instruction.rectTransform, 28f, 57f,
                    Mathf.Max(210f, actualWidth - 56f), height - 118f);
                instruction.fontSize = 18;
            }
        }

        private static RectTransform NewRect(string name, Transform parent, HideFlags hideFlags)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)) { hideFlags = hideFlags };
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Image NewImage(string name, Transform parent, HideFlags hideFlags, Color color)
        {
            var rect = NewRect(name, parent, hideFlags);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RoundedPanelGraphic NewRounded(string name, Transform parent,
            HideFlags hideFlags, Color color, float radius)
        {
            var rect = NewRect(name, parent, hideFlags);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            graphic.BorderWidth = 0f;
            return graphic;
        }

        private static Text NewText(string name, Transform parent, HideFlags hideFlags, Font font, string value, int size, TextAnchor alignment)
        {
            var rect = NewRect(name, parent, hideFlags);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UrbanFontResolver.GetFont(FontStyle.Bold);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = text.font != font ? FontStyle.Normal : FontStyle.Bold;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.90f, 0.80f, 1f);
            text.raycastTarget = false;
            return text;
        }

        private static void StyleButton(Button button)
        {
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.90f, 0.84f, 0.70f, 0.48f);
            outline.effectDistance = new Vector2(1f, -1f);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.97f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.72f, 0.91f, 0.96f, 1f);
            button.colors = colors;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static void SetBottomCenter(RectTransform rect, float x, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetBottomRight(RectTransform rect, float right, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-right, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
