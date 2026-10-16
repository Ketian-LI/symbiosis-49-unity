namespace UrbanWildlifeRooms.Core
{
    public enum OnboardingStep
    {
        Hidden,
        OakOpening,
        SelectResident,
        InspectWaste,
        PracticeLayout,
        PlaceFood,
        Complete
    }

    public sealed class FirstRunOnboardingModel
    {
        public OnboardingStep Step { get; private set; } = OnboardingStep.Hidden;
        public bool IsActive => Step != OnboardingStep.Hidden && Step != OnboardingStep.Complete;

        public void Begin()
        {
            Step = OnboardingStep.SelectResident;
        }

        public void BeginOakOpening()
        {
            Step = OnboardingStep.OakOpening;
        }

        public bool CompleteStep(OnboardingStep completed)
        {
            if (!IsActive || completed != Step)
            {
                return false;
            }
            Step = Step switch
            {
                OnboardingStep.OakOpening => OnboardingStep.Complete,
                OnboardingStep.SelectResident => OnboardingStep.InspectWaste,
                OnboardingStep.InspectWaste => OnboardingStep.PracticeLayout,
                OnboardingStep.PracticeLayout => OnboardingStep.PlaceFood,
                _ => OnboardingStep.Complete
            };
            return true;
        }

        public void Skip()
        {
            Step = OnboardingStep.Complete;
        }

        public void Hide()
        {
            Step = OnboardingStep.Hidden;
        }
    }
}
