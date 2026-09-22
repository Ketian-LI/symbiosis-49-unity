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
        private Image artwork;
        private Sprite normalSprite;
        private Sprite highlightedSprite;
        private RectTransform visualTarget;
        private bool highlighted;
        private bool pressed;

        public void Initialize(Image targetArtwork, Sprite defaultSprite, Sprite hoverSprite)
        {
            artwork = targetArtwork;
            normalSprite = defaultSprite;
            highlightedSprite = hoverSprite;
            visualTarget = targetArtwork != null ? targetArtwork.rectTransform : null;
            Apply(false, false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;
            Apply(true, pressed);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            highlighted = false;
            Apply(false, pressed);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            Apply(highlighted, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Apply(highlighted, false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            highlighted = true;
            Apply(true, pressed);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            highlighted = false;
            pressed = false;
            Apply(false, false);
        }

        private void OnDisable()
        {
            highlighted = false;
            pressed = false;
            Apply(false, false);
        }

        private void Apply(bool showHighlight, bool showPressed)
        {
            if (artwork != null)
            {
                artwork.sprite = showHighlight && highlightedSprite != null
                    ? highlightedSprite
                    : normalSprite;
                artwork.color = showPressed
                    ? new Color(0.88f, 0.92f, 0.94f, 1f)
                    : Color.white;
            }

            if (visualTarget != null)
            {
                var scale = showPressed ? 0.985f : showHighlight ? 1.012f : 1f;
                visualTarget.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
