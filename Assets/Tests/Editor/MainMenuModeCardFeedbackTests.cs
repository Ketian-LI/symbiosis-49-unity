using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class MainMenuModeCardFeedbackTests
    {
        [Test]
        public void HoverAndSelectionTintRoundedCardBlueThenRestoreTransparency()
        {
            var root = new GameObject("Mode Card Feedback Test", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic));

            try
            {
                var artwork = root.GetComponent<RoundedPanelGraphic>();
                var feedback = root.AddComponent<MainMenuModeCardFeedback>();
                feedback.Initialize(artwork);

                var restingColor = artwork.color;
                var restingBorder = artwork.BorderColor;
                Assert.That(artwork.color.a, Is.LessThan(0.5f));
                feedback.OnPointerEnter(null);
                Assert.That(artwork.color.b, Is.GreaterThan(restingColor.b));
                Assert.That(artwork.color.a, Is.GreaterThan(0.5f));
                Assert.That(artwork.color.a, Is.LessThan(0.8f));
                Assert.That(artwork.BorderColor.b, Is.GreaterThan(restingBorder.b));
                feedback.OnPointerExit(null);
                Assert.That(artwork.color, Is.EqualTo(restingColor));
                feedback.OnSelect((BaseEventData)null);
                Assert.That(artwork.color.b, Is.GreaterThan(restingColor.b));
                feedback.OnPointerEnter(null);
                feedback.OnPointerExit(null);
                Assert.That(artwork.color.b, Is.GreaterThan(restingColor.b),
                    "Keyboard selection survives mouse exit.");
                feedback.OnDeselect((BaseEventData)null);
                Assert.That(artwork.color, Is.EqualTo(restingColor));
                feedback.OnPointerEnter(null);
                feedback.OnSelect((BaseEventData)null);
                feedback.OnDeselect((BaseEventData)null);
                Assert.That(artwork.color.b, Is.GreaterThan(restingColor.b),
                    "Mouse hover survives keyboard deselection.");
                feedback.OnPointerExit(null);
                Assert.That(artwork.color, Is.EqualTo(restingColor));

                feedback.OnPointerEnter(null);
                feedback.OnPointerDown(null);
                Assert.That(artwork.color.a, Is.GreaterThan(0.6f));
                feedback.OnPointerUp(null);
                Assert.That(artwork.color.a, Is.LessThan(0.6f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
