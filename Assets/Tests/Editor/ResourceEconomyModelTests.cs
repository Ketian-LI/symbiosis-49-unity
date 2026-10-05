using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    // Kept only to detect regressions when reading historical session data.
    // The playable run no longer calls these income or spending rules.
    [Category("LegacyCompatibility")]
    public sealed class LegacyResourceEconomyModelTests
    {
        [Test]
        public void NewRunStartsAtTenAndCapsAtTwentyAfterSettlement()
        {
            var model = new ResourceEconomyModel();
            Assert.That(model.Balance, Is.EqualTo(10f));

            var settlement = model.SettleDay(1, 20f, 2);

            Assert.That(settlement.ClosingBalance, Is.EqualTo(20f));
            Assert.That(settlement.UnstoredSurplus, Is.EqualTo(8f));
            Assert.That(model.UnstoredSurplus, Is.EqualTo(8f));
        }

        [Test]
        public void ProductionIsAddedBeforeFoodServiceCost()
        {
            var model = new ResourceEconomyModel();
            var settlement = model.SettleDay(1, 4f, 2);

            Assert.That(settlement.OpeningBalance, Is.EqualTo(10f));
            Assert.That(settlement.Production, Is.EqualTo(4f));
            Assert.That(settlement.FoodServiceCost, Is.EqualTo(2));
            Assert.That(settlement.ClosingBalance, Is.EqualTo(12f));
        }

        [Test]
        public void InfrastructureAndWastePressureAreIncludedInDailySpending()
        {
            var model = new ResourceEconomyModel();
            var upkeep = ResourceEconomyModel.InfrastructureCost(4, 1);
            var settlement = model.SettleDay(1, 4f, 2, upkeep);

            Assert.That(upkeep, Is.EqualTo(4));
            Assert.That(settlement.TotalCost, Is.EqualTo(6));
            Assert.That(settlement.ClosingBalance, Is.EqualTo(8f));
            Assert.That(model.CumulativeSpending, Is.EqualTo(6f));
        }

        [TestCase(0, 0, 0)]
        [TestCase(4, 0, 2)]
        [TestCase(5, 0, 2)]
        [TestCase(7, 0, 2)]
        [TestCase(8, 0, 3)]
        [TestCase(4, 2, 6)]
        public void InfrastructureCostTracksPopulationAndWaste(
            int residents, int affectedWasteRooms, int expected)
        {
            Assert.That(ResourceEconomyModel.InfrastructureCost(residents, affectedWasteRooms),
                Is.EqualTo(expected));
        }

        [Test]
        public void ZeroIsAllowedButNegativeSettlementFails()
        {
            var model = new ResourceEconomyModel();
            model.Restore(0f, 0f, 0f, 0f, 8f);

            Assert.That(model.SettleDay(1, 2f, 2).Failed, Is.False);
            Assert.That(model.Balance, Is.Zero);
            Assert.That(model.SettleDay(2, 0f, 2).Failed, Is.True);
            Assert.That(model.Balance, Is.EqualTo(-2f));
        }

        [Test]
        public void VoluntarySpendingCannotCreateDebt()
        {
            var model = new ResourceEconomyModel();

            Assert.That(model.TrySpend(6), Is.True);
            Assert.That(model.Balance, Is.EqualTo(4f));
            Assert.That(model.TrySpend(5), Is.False);
            Assert.That(model.Balance, Is.EqualTo(4f));
        }

        [TestCase(4, 2, 2)]
        [TestCase(8, 2, 4)]
        [TestCase(2, 1, 1)]
        [TestCase(4, 1, 2)]
        [TestCase(0, 2, 0)]
        public void FoodShopCostScalesWithAssignedResidents(
            int residents,
            int shops,
            int expectedCost)
        {
            Assert.That(
                ResourceEconomyModel.FoodServiceCost(residents, shops),
                Is.EqualTo(expectedCost));
        }

        [Test]
        public void LegacyActionCostConstantsRemainStableForOldData()
        {
            Assert.That(ResourceEconomyModel.PlantTreeCost, Is.EqualTo(6));
            Assert.That(ResourceEconomyModel.FeedActionCost, Is.EqualTo(1));
            Assert.That(ResourceEconomyModel.EmergencyCollectionCost, Is.EqualTo(2));
        }
    }
}
