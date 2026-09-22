using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public sealed class FirstRunOnboardingOverlay : MonoBehaviour
    {
        private Camera worldCamera;
        private RectTransform root;
        private RectTransform spotlight;
        private Text instruction;
        private Text skipLabel;
        private Transform target;
        private OnboardingStep step;

        public System.Action SkipRequested;

        public void Build(Font font, Camera camera, HideFlags hideFlags)
        {
            worldCamera = camera;
            root = NewRect("First Run Onboarding", transform, hideFlags);
            Stretch(root);

            var shade = NewImage("Soft Tutorial Shade", root, hideFlags, new Color(0.02f, 0.03f, 0.04f, 0.20f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            spotlight = NewImage("Tutorial Spotlight", root, hideFlags, new Color(0.27f, 0.78f, 0.74f, 0.08f)).rectTransform;
            spotlight.sizeDelta = new Vector2(180f, 180f);
            var outline = spotlight.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.94f, 0.88f, 0.96f);
            outline.effectDistance = new Vector2(4f, -4f);
            spotlight.GetComponent<Image>().raycastTarget = false;

            var card = NewImage("One Sentence Tutorial Card", root, hideFlags, new Color(0.10f, 0.12f, 0.15f, 0.96f));
            SetBottomCenter(card.rectTransform, 0f, 38f, 850f, 112f);
            var gesture = NewText("Gesture", card.transform, hideFlags, font, "☝", 38, TextAnchor.MiddleCenter);
            SetRect(gesture.rectTransform, 24f, 22f, 70f, 66f);
            instruction = NewText("Instruction", card.transform, hideFlags, font, string.Empty, 22, TextAnchor.MiddleLeft);
            SetRect(instruction.rectTransform, 104f, 20f, 590f, 72f);
            var skip = NewImage("Skip", card.transform, hideFlags, new Color(0.31f, 0.27f, 0.39f, 1f));
            SetRect(skip.rectTransform, 710f, 28f, 116f, 56f);
            var button = skip.gameObject.AddComponent<Button>();
            button.targetGraphic = skip;
            button.onClick.AddListener(() => SkipRequested?.Invoke());
            skipLabel = NewText("Skip Label", skip.transform, hideFlags, font, "跳过", 18, TextAnchor.MiddleCenter);
            Stretch(skipLabel.rectTransform, 4f);
            root.gameObject.SetActive(false);
        }

        public void Show(OnboardingStep nextStep, Transform worldTarget, bool chinese)
        {
            step = nextStep;
            target = worldTarget;
            root.gameObject.SetActive(nextStep != OnboardingStep.Hidden && nextStep != OnboardingStep.Complete);
            if (!root.gameObject.activeSelf)
            {
                return;
            }
            instruction.text = InstructionFor(nextStep, chinese);
            skipLabel.text = chinese ? "跳过" : "Skip";
            spotlight.sizeDelta = nextStep == OnboardingStep.PracticeLayout || nextStep == OnboardingStep.PlaceFood
                ? new Vector2(900f, 900f)
                : new Vector2(180f, 180f);
            UpdateSpotlight();
        }

        private void LateUpdate()
        {
            if (root != null && root.gameObject.activeSelf)
            {
                UpdateSpotlight();
            }
        }

        private void UpdateSpotlight()
        {
            if (step == OnboardingStep.PracticeLayout || step == OnboardingStep.PlaceFood || target == null)
            {
                spotlight.anchoredPosition = Vector2.zero;
                return;
            }
            var screen = worldCamera.WorldToScreenPoint(target.position + Vector3.up * 0.35f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, worldCamera, out var local);
            spotlight.anchoredPosition = local;
        }

        private static string InstructionFor(OnboardingStep value, bool chinese)
        {
            return value switch
            {
                OnboardingStep.SelectResident => chinese ? "点击高亮居民，查看住处—办公室—餐饮路线。" : "Select the highlighted resident to reveal their home–office–food route.",
                OnboardingStep.InspectWaste => chinese ? "点击高亮垃圾房，查看容量与下一次清运。" : "Select the highlighted waste room to inspect capacity and collection time.",
                OnboardingStep.PracticeLayout => chinese ? "把一个高亮 1×1 房间放入托盘，再放回原位并确认。" : "Move one highlighted 1×1 room to the tray, return it, then confirm.",
                _ => chinese ? "点击空地投喂一次；本次会正常消耗 1 资源点。" : "Place one food source on open ground; it costs the normal 1 resource point."
            };
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

        private static Text NewText(string name, Transform parent, HideFlags hideFlags, Font font, string value, int size, TextAnchor alignment)
        {
            var rect = NewRect(name, parent, hideFlags);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.90f, 0.80f, 1f);
            text.raycastTarget = false;
            return text;
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

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
