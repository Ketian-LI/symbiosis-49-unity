using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    public sealed class ResultsButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private static readonly Color NormalTint = Color.white;
        private static readonly Color HighlightTint = new(1f, 0.96f, 0.82f, 1f);
        private static readonly Color PressedTint = new(0.92f, 0.83f, 0.65f, 1f);

        private RectTransform visualTarget;
        private Image artwork;
        private AudioSource audioSource;
        private AudioClip clickClip;
        private Coroutine transition;
        private bool highlighted;
        private bool pressed;

        public void Initialize(RectTransform target, Image backgroundArtwork, AudioSource source, AudioClip sound)
        {
            visualTarget = target;
            artwork = backgroundArtwork;
            audioSource = source;
            clickClip = sound;
            ApplyImmediate(1f, NormalTint);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;
            if (!pressed)
            {
                BeginTransition(1.015f, HighlightTint, 0.08f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            highlighted = false;
            if (!pressed)
            {
                BeginTransition(1f, NormalTint, 0.08f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            BeginTransition(0.96f, PressedTint, 0.05f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            BeginTransition(
                highlighted ? 1.015f : 1f,
                highlighted ? HighlightTint : NormalTint,
                0.07f);
        }

        public void OnSelect(BaseEventData eventData)
        {
            highlighted = true;
            if (!pressed)
            {
                BeginTransition(1.015f, HighlightTint, 0.08f);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            highlighted = false;
            pressed = false;
            BeginTransition(1f, NormalTint, 0.08f);
        }

        public void PlayClick()
        {
            if (audioSource != null && clickClip != null)
            {
                audioSource.PlayOneShot(clickClip, 0.46f);
            }
        }

        private void OnDisable()
        {
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }

            highlighted = false;
            pressed = false;
            ApplyImmediate(1f, NormalTint);
        }

        private void BeginTransition(float targetScale, Color targetTint, float duration)
        {
            if (visualTarget == null || artwork == null)
            {
                return;
            }

            if (transition != null)
            {
                StopCoroutine(transition);
            }

            transition = StartCoroutine(Animate(targetScale, targetTint, duration));
        }

        private IEnumerator Animate(float targetScale, Color targetTint, float duration)
        {
            var initialScale = visualTarget.localScale.x;
            var initialTint = artwork.color;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var scale = Mathf.LerpUnclamped(initialScale, targetScale, eased);
                visualTarget.localScale = new Vector3(scale, scale, 1f);
                artwork.color = Color.LerpUnclamped(initialTint, targetTint, eased);
                yield return null;
            }

            ApplyImmediate(targetScale, targetTint);
            transition = null;
        }

        private void ApplyImmediate(float scale, Color tint)
        {
            if (visualTarget != null)
            {
                visualTarget.localScale = new Vector3(scale, scale, 1f);
            }

            if (artwork != null)
            {
                artwork.color = tint;
            }
        }
    }
}
