using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class GarageTrafficModelTests
    {
        [Test]
        public void RouteWithoutGarageHasNoTrafficDecision()
        {
            Assert.That(
                GarageTrafficModel.Decide(false, false, 0f, 0f),
                Is.EqualTo(GarageCrossingDecision.NoGarage));
        }

        [Test]
        public void FirstHalfDetoursWhenAlternativeExistsAndAbandonsOtherwise()
        {
            Assert.That(
                GarageTrafficModel.Decide(true, true, 0.49f, 0f),
                Is.EqualTo(GarageCrossingDecision.Detour));
            Assert.That(
                GarageTrafficModel.Decide(true, false, 0.49f, 0f),
                Is.EqualTo(GarageCrossingDecision.Abandon));
        }

        [Test]
        public void ChosenGarageCrossingHasEvenSafeAndFatalJudgementBands()
        {
            Assert.That(
                GarageTrafficModel.Decide(true, true, 0.5f, 0.49f),
                Is.EqualTo(GarageCrossingDecision.CrossFatally));
            Assert.That(
                GarageTrafficModel.Decide(true, true, 0.5f, 0.5f),
                Is.EqualTo(GarageCrossingDecision.CrossSafely));
        }
    }
}
