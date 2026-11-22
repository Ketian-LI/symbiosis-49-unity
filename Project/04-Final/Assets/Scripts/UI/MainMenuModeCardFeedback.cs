using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private Image plaque;
        private RectTransform visualTarget;
        private bool pointerInside;
        private bool selected;
        private bool pressed;

        private static readonly Color HitAreaTint = new(0f, 0f, 0f, 0.001f);
        private static readonly Color HighlightTint = new(1f, 0.98f, 0.90f, 1f);
        private static readonly Color PressedTint = new(0.83f, 0.86f, 0.88f, 1f);

        public void Initialize(RoundedPanelGraphic targetArtwork, Image plaqueArtwork)
        {
            artwork = targetArtwork;
            plaque = plaqueArtwork;
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
                // The illustrated plaque already owns its brass rim. Keep the
                // Button graphic as a transparent hit area, never a second rim.
                artwork.BorderWidth = 0f;
                artwork.color = HitAreaTint;
            }
            if (plaque != null)
                plaque.color = pressed ? PressedTint :
                    showHighlight ? HighlightTint : Color.white;

            if (visualTarget != null)
            {
                var scale = pressed ? 0.98f : showHighlight ? 1.025f : 1f;
                visualTarget.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
