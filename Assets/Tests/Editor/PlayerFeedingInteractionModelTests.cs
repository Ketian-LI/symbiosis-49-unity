using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PlayerFeedingInteractionModelTests
    {
        [Test]
        public void FeedingRequiresExplicitActivationAndCompletesAsOneShot()
        {
            var model = new PlayerFeedingInteractionModel();

            Assert.That(model.IsActive, Is.False);
            Assert.That(model.TryActivate(true), Is.True);
            Assert.That(model.IsActive, Is.True);
            Assert.That(model.CompletePlacement(), Is.True);
            Assert.That(model.IsActive, Is.False);
        }

        [Test]
        public void FeedingCannotActivateWhenInteractionIsUnavailable()
        {
            var model = new PlayerFeedingInteractionModel();

            Assert.That(model.TryActivate(false), Is.False);
            Assert.That(model.IsActive, Is.False);
        }

        [Test]
        public void ToggleAndCancelProvideAnExplicitEscapePath()
        {
            var model = new PlayerFeedingInteractionModel();

            Assert.That(model.Toggle(true), Is.True);
            Assert.That(model.Toggle(true), Is.False);
            Assert.That(model.TryActivate(true), Is.True);
            Assert.That(model.Cancel(), Is.True);
            Assert.That(model.IsActive, Is.False);
        }
    }
}
