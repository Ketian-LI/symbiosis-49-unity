using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class FoxPredationModelTests
    {
        [Test]
        public void HuntRequiresLivingActiveHungryFox()
        {
            Assert.That(FoxPredationModel.CanStartHunt(true, true, 1), Is.True);
            Assert.That(FoxPredationModel.CanStartHunt(true, true, 0), Is.False);
            Assert.That(FoxPredationModel.CanStartHunt(false, true, 2), Is.False);
            Assert.That(FoxPredationModel.CanStartHunt(true, false, 2), Is.False);
        }

        [Test]
        public void GroundedPigeonOutranksCloserSquirrelAndHedgehog()
        {
            var result = FoxPredationModel.SelectPrey(new[]
            {
                new FoxPreyCandidate("hedgehog", WildlifeSpecies.Hedgehog, 1f, true, true),
                new FoxPreyCandidate("squirrel", WildlifeSpecies.Squirrel, 0.5f, true, true),
                new FoxPreyCandidate("pigeon", WildlifeSpecies.Pigeon, 8f, true, true)
            });

            Assert.That(result, Is.EqualTo("pigeon"));
        }

        [Test]
        public void FlyingOrDeadPreyIsExcluded()
        {
            var result = FoxPredationModel.SelectPrey(new[]
            {
                new FoxPreyCandidate("flying-pigeon", WildlifeSpecies.Pigeon, 0.2f, true, false),
                new FoxPreyCandidate("dead-squirrel", WildlifeSpecies.Squirrel, 0.1f, false, true),
                new FoxPreyCandidate("hedgehog", WildlifeSpecies.Hedgehog, 2f, true, true)
            });

            Assert.That(result, Is.EqualTo("hedgehog"));
        }
    }
}
