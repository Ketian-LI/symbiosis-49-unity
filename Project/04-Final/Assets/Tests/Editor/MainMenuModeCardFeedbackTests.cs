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
        public void HoverAndSelectionTintTheSinglePlaqueWithoutAnotherBorder()
        {
            var root = new GameObject("Mode Card Feedback Test", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic));

            try
            {
                var artwork = root.GetComponent<RoundedPanelGraphic>();
                var plaqueObject = new GameObject("Plaque artwork", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                plaqueObject.transform.SetParent(root.transform, false);
                var plaque = plaqueObject.GetComponent<Image>();
                var feedback = root.AddComponent<MainMenuModeCardFeedback>();
                feedback.Initialize(artwork, plaque);

                var restingColor = plaque.color;
                Assert.That(artwork.BorderWidth, Is.Zero);
                Assert.That(artwork.color.a, Is.LessThan(0.01f));
                feedback.OnPointerEnter(null);
                Assert.That(plaque.color, Is.Not.EqualTo(restingColor));
                Assert.That(artwork.BorderWidth, Is.Zero);
                feedback.OnPointerExit(null);
                Assert.That(plaque.color, Is.EqualTo(restingColor));
                feedback.OnSelect((BaseEventData)null);
                Assert.That(plaque.color, Is.Not.EqualTo(restingColor));
                feedback.OnPointerEnter(null);
                feedback.OnPointerExit(null);
                Assert.That(plaque.color, Is.Not.EqualTo(restingColor),
                    "Keyboard selection survives mouse exit.");
                feedback.OnDeselect((BaseEventData)null);
                Assert.That(plaque.color, Is.EqualTo(restingColor));
                feedback.OnPointerEnter(null);
                feedback.OnSelect((BaseEventData)null);
                feedback.OnDeselect((BaseEventData)null);
                Assert.That(plaque.color, Is.Not.EqualTo(restingColor),
                    "Mouse hover survives keyboard deselection.");
                feedback.OnPointerExit(null);
                Assert.That(plaque.color, Is.EqualTo(restingColor));

                feedback.OnPointerEnter(null);
                feedback.OnPointerDown(null);
                Assert.That(plaque.color.b, Is.LessThan(restingColor.b));
                feedback.OnPointerUp(null);
                Assert.That(plaque.color.b, Is.GreaterThan(restingColor.b * 0.85f));
                Assert.That(artwork.BorderWidth, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
