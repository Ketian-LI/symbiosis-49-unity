using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class NaturalFoodModelTests
    {
        [Test]
        public void DawnCreatesSevenGuaranteedPigeonSeedPortionsAndOnlyMatureTreeNuts()
        {
            var model = new NaturalFoodModel(RoomLayoutData.All);
            model.ProduceDawn(1, roomId => roomId == "oak-c" ? OakTreeStage.Young : OakTreeStage.Mature);

            Assert.That(model.PortionsIn("central-park", NaturalFoodKind.Seed), Is.EqualTo(3));
            Assert.That(model.PortionsIn("pigeon-a", NaturalFoodKind.Seed), Is.EqualTo(1));
            Assert.That(model.PortionsIn("pigeon-b", NaturalFoodKind.Seed), Is.EqualTo(1));
            Assert.That(model.PortionsIn("pigeon-c", NaturalFoodKind.Seed), Is.EqualTo(1));
            Assert.That(model.PortionsIn("pigeon-d", NaturalFoodKind.Seed), Is.EqualTo(1));
            Assert.That(model.PortionsIn("oak-c", NaturalFoodKind.Nut), Is.Zero);
            Assert.That(model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
        }

        [Test]
        public void MatureTreeRetainsAtMostOneUncollectedNut()
        {
            var model = new NaturalFoodModel(RoomLayoutData.All);
            model.ProduceDawn(1, _ => OakTreeStage.Mature);
            model.ProduceDawn(2, _ => OakTreeStage.Mature);

            Assert.That(model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
        }

        [TestCase(0, 0f)]
        [TestCase(1, 0.25f)]
        [TestCase(5, 0.25f)]
        [TestCase(6, 0.5f)]
        [TestCase(7, 0.5f)]
        [TestCase(8, 1f)]
        [TestCase(12, 1f)]
        public void EdibleWasteProbabilityFollowsConfirmedFillBands(int units, float expected)
        {
            Assert.That(NaturalFoodModel.EdibleWasteProbability(units), Is.EqualTo(expected));
        }

        [Test]
        public void RestoreRoundTripPreservesKindsAndPortions()
        {
            var original = new NaturalFoodModel(RoomLayoutData.All);
            original.ProduceDawn(1, _ => OakTreeStage.Mature);
            original.TryConsume("central-park", NaturalFoodKind.Seed);

            var restored = new NaturalFoodModel(RoomLayoutData.All);
            restored.Restore(original.Export());

            Assert.That(restored.PortionsIn("central-park", NaturalFoodKind.Seed), Is.EqualTo(2));
            Assert.That(restored.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
        }
    }
}
