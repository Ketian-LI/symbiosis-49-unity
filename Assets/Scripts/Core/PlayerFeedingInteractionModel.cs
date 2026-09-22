namespace UrbanWildlifeRooms.Core
{
    /// <summary>
    /// Requires an explicit feeding-mode activation before a world click can
    /// place food. A successful placement consumes the one-shot activation.
    /// </summary>
    public sealed class PlayerFeedingInteractionModel
    {
        public bool IsActive { get; private set; }

        public bool TryActivate(bool interactionAllowed)
        {
            if (!interactionAllowed || IsActive)
            {
                return false;
            }

            IsActive = true;
            return true;
        }

        public bool Toggle(bool interactionAllowed)
        {
            if (IsActive)
            {
                IsActive = false;
                return false;
            }

            return TryActivate(interactionAllowed);
        }

        public bool CompletePlacement()
        {
            if (!IsActive)
            {
                return false;
            }

            IsActive = false;
            return true;
        }

        public bool Cancel()
        {
            if (!IsActive)
            {
                return false;
            }

            IsActive = false;
            return true;
        }
    }
}
