using UnityEngine;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class PigeonDeathMarkerEffect : MonoBehaviour
    {
        private float lifetime;
        private float elapsed;
        private Transform rippleTransform;
        private SpriteRenderer rippleRenderer;
        private SpriteRenderer[] footprintRenderers;
        private Vector3 rippleStartScale;
        private Color rippleStartColor;
        private Color[] footprintStartColors;

        public void Initialize(
            float duration,
            Transform ripple,
            SpriteRenderer rippleVisual,
            params SpriteRenderer[] footprints)
        {
            lifetime = Mathf.Max(0.05f, duration);
            rippleTransform = ripple;
            rippleRenderer = rippleVisual;
            footprintRenderers = footprints;
            rippleStartScale = rippleTransform != null ? rippleTransform.localScale : Vector3.one;
            rippleStartColor = rippleRenderer != null ? rippleRenderer.color : Color.white;
            footprintStartColors = new Color[footprintRenderers.Length];
            for (var index = 0; index < footprintRenderers.Length; index++)
            {
                footprintStartColors[index] = footprintRenderers[index].color;
            }
        }

        public static float FootprintAlphaAt(float normalizedTime)
        {
            var progress = Mathf.Clamp01(normalizedTime);
            return 1f - Mathf.SmoothStep(0f, 1f, progress);
        }

        public static float RippleAlphaAt(float normalizedTime)
        {
            var progress = Mathf.Clamp01(normalizedTime);
            return (1f - progress) * (1f - progress);
        }

        public static float RippleScaleAt(float normalizedTime)
        {
            return Mathf.Lerp(0.78f, 1.18f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime)));
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / lifetime);

            var footprintAlpha = FootprintAlphaAt(progress);
            for (var index = 0; index < footprintRenderers.Length; index++)
            {
                if (footprintRenderers[index] == null)
                {
                    continue;
                }

                var color = footprintStartColors[index];
                color.a *= footprintAlpha;
                footprintRenderers[index].color = color;
            }

            if (rippleTransform != null)
            {
                rippleTransform.localScale = rippleStartScale * RippleScaleAt(progress);
            }

            if (rippleRenderer != null)
            {
                var color = rippleStartColor;
                color.a *= RippleAlphaAt(progress);
                rippleRenderer.color = color;
            }

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
