using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class MainMenuModeCardFeedbackTests
    {
        [Test]
        public void HoverAndSelectionSwapToBlueArtworkThenRestoreOriginalCard()
        {
            var root = new GameObject("Mode Card Feedback Test", typeof(RectTransform), typeof(Image));
            var normalTexture = new Texture2D(2, 2);
            var hoverTexture = new Texture2D(2, 2);
            var normal = Sprite.Create(normalTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            var hover = Sprite.Create(hoverTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);

            try
            {
                var artwork = root.GetComponent<Image>();
                var feedback = root.AddComponent<MainMenuModeCardFeedback>();
                feedback.Initialize(artwork, normal, hover);

                Assert.That(artwork.sprite, Is.SameAs(normal));
                feedback.OnPointerEnter(null);
                Assert.That(artwork.sprite, Is.SameAs(hover));
                feedback.OnPointerExit(null);
                Assert.That(artwork.sprite, Is.SameAs(normal));
                feedback.OnSelect((BaseEventData)null);
                Assert.That(artwork.sprite, Is.SameAs(hover));
                feedback.OnDeselect((BaseEventData)null);
                Assert.That(artwork.sprite, Is.SameAs(normal));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(normal);
                Object.DestroyImmediate(hover);
                Object.DestroyImmediate(normalTexture);
                Object.DestroyImmediate(hoverTexture);
            }
        }
    }
}
