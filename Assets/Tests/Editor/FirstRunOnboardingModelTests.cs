using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class FirstRunOnboardingModelTests
    {
        [Test]
        public void FourConfirmedInteractionsCompleteTutorialInOrder()
        {
            var model = new FirstRunOnboardingModel();
            model.Begin();
            Assert.That(model.CompleteStep(OnboardingStep.InspectWaste), Is.False);
            Assert.That(model.CompleteStep(OnboardingStep.SelectResident), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.InspectWaste), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.PracticeLayout), Is.True);
            Assert.That(model.CompleteStep(OnboardingStep.PlaceFood), Is.True);
            Assert.That(model.Step, Is.EqualTo(OnboardingStep.Complete));
        }

        [Test]
        public void SkipCompletesFromAnyActiveStep()
        {
            var model = new FirstRunOnboardingModel();
            model.Begin();
            model.Skip();
            Assert.That(model.IsActive, Is.False);
        }

        [Test]
        public void PauseSuppressionHidesAndThenRestoresTheCurrentTutorialStep()
        {
            var hud = new GameObject("Tutorial HUD");
            try
            {
                var overlay = hud.AddComponent<FirstRunOnboardingOverlay>();
                overlay.Build(UrbanFontResolver.GetFont(), null, HideFlags.None);
                overlay.Show(OnboardingStep.SelectResident, null, true);
                Assert.That(overlay.IsVisible, Is.True);

                overlay.SetSuppressed(true);
                Assert.That(overlay.IsVisible, Is.False);

                overlay.Show(OnboardingStep.InspectWaste, null, true);
                Assert.That(overlay.IsVisible, Is.False);

                overlay.SetSuppressed(false);
                Assert.That(overlay.IsVisible, Is.True);

                overlay.Show(OnboardingStep.Hidden, null, true);
                Assert.That(overlay.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }
    }
}
