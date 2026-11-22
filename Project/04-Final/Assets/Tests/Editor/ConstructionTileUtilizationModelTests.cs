using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionTileUtilizationModelTests
    {
        [Test]
        public void StoredFoodIsAmberUntilEatenButLostYieldIsRed()
        {
            var savedForLater = ConstructionTileUtilizationModel.Food(
                eaten: 0, remaining: 2, potentialYield: 0,
                actualYield: 0, capacity: 3, openingStock: 2);
            Assert.That(savedForLater.Level,
                Is.EqualTo(ConstructionUtilizationLevel.Idle));
            Assert.That(savedForLater.Idle, Is.EqualTo(2));

            var lostProduction = ConstructionTileUtilizationModel.Food(
                eaten: 1, remaining: 2, potentialYield: 1,
                actualYield: 0, capacity: 3, openingStock: 3);
            Assert.That(lostProduction.Level,
                Is.EqualTo(ConstructionUtilizationLevel.Blocked));
            Assert.That(lostProduction.Reason,
                Is.EqualTo(ConstructionUtilizationReason.YieldLostAtCapacity));

            var allEaten = ConstructionTileUtilizationModel.Food(
                eaten: 2, remaining: 0, potentialYield: 1,
                actualYield: 0, capacity: 2, openingStock: 2);
            Assert.That(allEaten.Level,
                Is.EqualTo(ConstructionUtilizationLevel.Full),
                "Taking all available food is full utilization even when dawn production was capped.");
        }

        [Test]
        public void UnusedServiceOnlyBecomesRedForConfirmedBlockage()
        {
            var spare = ConstructionTileUtilizationModel.Service(
                0, 2, blockedByWaste: false,
                unmetNeed: false, reachable: false);
            Assert.That(spare.Level,
                Is.EqualTo(ConstructionUtilizationLevel.Idle));

            var unreachable = ConstructionTileUtilizationModel.Service(
                0, 2, blockedByWaste: false,
                unmetNeed: true, reachable: false);
            Assert.That(unreachable.Reason,
                Is.EqualTo(ConstructionUtilizationReason.UnreachableServiceDemand));

            var waste = ConstructionTileUtilizationModel.Service(
                0, 2, blockedByWaste: true,
                unmetNeed: true, reachable: true);
            Assert.That(waste.Reason,
                Is.EqualTo(ConstructionUtilizationReason.ServiceBlockedByWaste));
        }

        [Test]
        public void HomeDistinguishesIdleWorkFromMissedMeal()
        {
            Assert.That(ConstructionTileUtilizationModel.Residence(
                    true, worked: false, ate: true, blockedByWaste: false).Level,
                Is.EqualTo(ConstructionUtilizationLevel.Idle));
            Assert.That(ConstructionTileUtilizationModel.Residence(
                    true, worked: true, ate: false, blockedByWaste: false).Level,
                Is.EqualTo(ConstructionUtilizationLevel.Blocked));
            Assert.That(ConstructionTileUtilizationModel.Residence(
                    true, worked: true, ate: true, blockedByWaste: false).Level,
                Is.EqualTo(ConstructionUtilizationLevel.Full));
        }

        [Test]
        public void StarterOakStatusUsesTheSameDayForecastAsTheRun()
        {
            var run = new ConstructionRunModel();
            var oak = run.At(ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            var status = ConstructionTileUtilizationModel.Evaluate(oak, run,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
            Assert.That(status.Kind,
                Is.EqualTo(ConstructionUtilizationKind.GreenFood));
            Assert.That(status.Level,
                Is.EqualTo(ConstructionUtilizationLevel.Full));
            Assert.That(status.Used, Is.EqualTo(1));
            Assert.That(status.Available, Is.EqualTo(1));
        }
    }
}
