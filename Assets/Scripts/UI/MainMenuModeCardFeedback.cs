using UnityEngine;
using UnityEngine.EventSystems;

namespace UrbanWildlifeRooms.UI
{
    public sealed class MainMenuModeCardFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private RoundedPanelGraphic artwork;
        private RectTransform visualTarget;
        private bool pointerInside;
        private bool selected;
        private bool pressed;

        private static readonly Color RestingTint = new(0.10f, 0.17f, 0.19f, 0.42f);
        private static readonly Color HighlightTint = new(0.44f, 0.80f, 0.91f, 0.55f);
        private static readonly Color PressedTint = new(0.42f, 0.78f, 0.89f, 0.68f);
        private static readonly Color RestingBorder = new(0.97f, 0.92f, 0.80f, 0.75f);
        private static readonly Color HighlightBorder = new(0.66f, 0.95f, 1f, 0.95f);

        public void Initialize(RoundedPanelGraphic targetArtwork)
        {
            artwork = targetArtwork;
            visualTarget = targetArtwork != null ? targetArtwork.rectTransform : null;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Apply();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Apply();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
            Apply();
        }

        private void OnDisable()
        {
            pointerInside = false;
            selected = false;
            pressed = false;
            Apply();
        }

        private void Apply()
        {
            var showHighlight = pointerInside || selected;
            if (artwork != null)
            {
                artwork.color = pressed ? PressedTint : showHighlight ? HighlightTint : RestingTint;
                artwork.BorderColor = showHighlight || pressed ? HighlightBorder : RestingBorder;
            }

            if (visualTarget != null)
            {
                var scale = pressed ? 0.985f : showHighlight ? 1.012f : 1f;
                visualTarget.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
