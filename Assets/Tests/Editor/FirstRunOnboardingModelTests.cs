using NUnit.Framework;
using UrbanWildlifeRooms.Core;

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
    }
}
