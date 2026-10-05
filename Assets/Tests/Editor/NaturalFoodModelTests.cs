using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class NaturalFoodModelTests
    {
        [Test]
        public void DawnRewardsLinkedPigeonPlazasAndTheSquirrelGrove()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3f, true);
            var model = new NaturalFoodModel(RoomLayoutData.All);
            model.ProduceDawn(1, roomId => roomId == "oak-c" ? OakTreeStage.Young : OakTreeStage.Mature,
                navigation);

            Assert.That(model.PortionsIn("central-park", NaturalFoodKind.Seed), Is.EqualTo(1));
            Assert.That(model.PortionsIn("pigeon-a", NaturalFoodKind.Seed), Is.Zero);
            Assert.That(model.PortionsIn("pigeon-b", NaturalFoodKind.Seed), Is.EqualTo(5));
            Assert.That(model.PortionsIn("pigeon-c", NaturalFoodKind.Seed), Is.EqualTo(5));
            Assert.That(model.PortionsIn("pigeon-d", NaturalFoodKind.Seed), Is.Zero);
            Assert.That(model.PortionsIn("oak-c", NaturalFoodKind.Nut), Is.Zero);
            Assert.That(model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
            Assert.That(model.PortionsIn("shared-f", NaturalFoodKind.Nut), Is.EqualTo(1));
        }

        [Test]
        public void MatureTreeRetainsAtMostOneUncollectedNut()
        {
            var model = new NaturalFoodModel(RoomLayoutData.All);
            model.ProduceDawn(1, _ => OakTreeStage.Mature);
            model.ProduceDawn(2, _ => OakTreeStage.Mature);

            Assert.That(model.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
        }

        [Test]
        public void StartingDayFoodBudgetIncludesDistinctGreenNichesButNotRemotePlazas()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3f, true);
            var waste = new WasteManagementModel(navigation, RoomLayoutData.All);
            waste.ProduceDailyWaste(ResidentPopulationModel.StartingResidents, 2, true);

            var food = new NaturalFoodModel(RoomLayoutData.All);
            food.ProduceDawn(1, roomId => roomId == "oak-c" ? OakTreeStage.Young : OakTreeStage.Mature,
                navigation);
            food.ProduceDusk(1, 2);
            food.ProduceNight(1, waste.WasteRooms, navigation);

            Assert.That(AnimalPopulationDefaults.Total, Is.EqualTo(19));
            Assert.That(food.Sources.Values.Where(item => item.kind == NaturalFoodKind.Seed)
                .Sum(item => item.portions), Is.EqualTo(11));
            Assert.That(food.Sources.Values.Where(item => item.kind == NaturalFoodKind.Nut)
                .Sum(item => item.portions), Is.EqualTo(4));
            Assert.That(food.Sources.Values.Where(item => item.kind == NaturalFoodKind.Insect)
                .Sum(item => item.portions), Is.GreaterThanOrEqualTo(2));
            Assert.That(food.PortionsIn("shared-h", NaturalFoodKind.Insect), Is.EqualTo(1));
            Assert.That(food.Sources.Values.Where(item => item.kind == NaturalFoodKind.DiscardedFood)
                .Sum(item => item.portions), Is.EqualTo(2));
            Assert.That(food.TotalPortions, Is.GreaterThanOrEqualTo(19));
            Assert.That(HabitatFoodNetworkModel.ParkLinkedPigeonHabitats(navigation), Is.EqualTo(2));
            Assert.That(HabitatFoodNetworkModel.CanReachSource(navigation, "pigeon-d",
                UrbanWildlifeRooms.Animals.WildlifeSpecies.Pigeon, "central-park", out _), Is.False,
                "The initial total must not imply that every pigeon can use the park's food.");
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

            Assert.That(restored.PortionsIn("central-park", NaturalFoodKind.Seed), Is.Zero);
            Assert.That(restored.PortionsIn("oak-a", NaturalFoodKind.Nut), Is.EqualTo(1));
        }
    }
}
