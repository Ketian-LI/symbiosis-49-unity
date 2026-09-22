using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResearchSessionModelTests
    {
        [Test]
        public void OnlyActiveSimulationConsumesResearchDuration()
        {
            var model = new ResearchSessionModel();
            model.Configure("P007", 6, 8);
            model.Begin();

            model.Advance(60f, true);
            model.Advance(30f, false);

            Assert.That(model.ActiveSeconds, Is.EqualTo(60f));
            Assert.That(model.OperationSeconds, Is.EqualTo(90f));
            Assert.That(model.RemainingFraction, Is.EqualTo(5f / 6f).Within(0.001f));
        }

        [Test]
        public void ConfigurationIsSanitizedClampedAndLockedWhileRunning()
        {
            var model = new ResearchSessionModel();
            model.Configure(" p 01!* ", 2, 99);

            Assert.That(model.ParticipantCode, Is.EqualTo("P01"));
            Assert.That(model.DurationMinutes, Is.EqualTo(6));
            Assert.That(model.DeathLimit, Is.EqualTo(20));

            model.Begin();
            model.Configure("P999", 30, 2);
            Assert.That(model.ParticipantCode, Is.EqualTo("P01"));
        }

        [Test]
        public void TimeExpiresAtConfiguredActiveDuration()
        {
            var model = new ResearchSessionModel();
            model.Configure("P001", 6, 5);
            model.Begin();
            model.Advance(360f, true);

            Assert.That(model.TimeExpired, Is.True);
            Assert.That(model.ActiveSeconds, Is.EqualTo(360f));
        }
    }
}
