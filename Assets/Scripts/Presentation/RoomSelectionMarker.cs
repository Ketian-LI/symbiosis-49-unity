using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Four detached, screen-readable corners used only for room selection.
    /// Legal and illegal placement previews remain on the room floor layer.
    /// </summary>
    public sealed class RoomSelectionMarker : MonoBehaviour
    {
        private const float MarkerHeight = 0.72f;
        private const float OuterOffset = 0.08f;

        private readonly SpriteRenderer[] cornerRenderers = new SpriteRenderer[4];
        private GameObject markerRoot;
        private bool visible;

        public bool IsVisible => markerRoot != null && markerRoot.activeSelf;
        public int CornerCount => cornerRenderers.Length;

        public void Initialize(float roomWidth, float roomDepth, HideFlags generatedHideFlags)
        {
            if (markerRoot != null)
            {
                return;
            }

            var sprite = RoomSelectionVisualCatalog.GetCornerSprite();
            if (sprite == null)
            {
                Debug.LogWarning("Room selection corner sprite could not be loaded.", this);
                return;
            }

            markerRoot = new GameObject("Room Selection Corners")
            {
                hideFlags = generatedHideFlags
            };
            markerRoot.transform.SetParent(transform, false);

            var halfWidth = roomWidth * 0.5f + OuterOffset;
            var halfDepth = roomDepth * 0.5f + OuterOffset;
            var cornerSize = Mathf.Clamp(Mathf.Min(roomWidth, roomDepth) * 0.30f, 0.64f, 0.92f);
            var spriteScale = cornerSize / Mathf.Max(0.001f, sprite.bounds.size.x);

            CreateCorner(0, "North West", sprite, new Vector3(-halfWidth, MarkerHeight, halfDepth), 0f, spriteScale, generatedHideFlags);
            CreateCorner(1, "North East", sprite, new Vector3(halfWidth, MarkerHeight, halfDepth), -90f, spriteScale, generatedHideFlags);
            CreateCorner(2, "South East", sprite, new Vector3(halfWidth, MarkerHeight, -halfDepth), 180f, spriteScale, generatedHideFlags);
            CreateCorner(3, "South West", sprite, new Vector3(-halfWidth, MarkerHeight, -halfDepth), 90f, spriteScale, generatedHideFlags);

            markerRoot.SetActive(false);
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (markerRoot != null)
            {
                markerRoot.SetActive(value);
            }
        }

        private void Update()
        {
            if (!visible || markerRoot == null)
            {
                return;
            }

            var alpha = Application.isPlaying
                ? 0.90f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.07f
                : 0.94f;

            foreach (var cornerRenderer in cornerRenderers)
            {
                if (cornerRenderer != null)
                {
                    cornerRenderer.color = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        private void CreateCorner(
            int index,
            string cornerName,
            Sprite sprite,
            Vector3 localPosition,
            float rotationDegrees,
            float uniformScale,
            HideFlags generatedHideFlags)
        {
            var cornerObject = new GameObject($"{cornerName} Selection Corner")
            {
                hideFlags = generatedHideFlags
            };
            cornerObject.transform.SetParent(markerRoot.transform, false);
            cornerObject.transform.localPosition = localPosition;
            cornerObject.transform.localRotation =
                Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, rotationDegrees);
            cornerObject.transform.localScale = Vector3.one * uniformScale;

            var spriteRenderer = cornerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.94f);
            spriteRenderer.sortingOrder = 40;
            cornerRenderers[index] = spriteRenderer;
        }
    }
}
