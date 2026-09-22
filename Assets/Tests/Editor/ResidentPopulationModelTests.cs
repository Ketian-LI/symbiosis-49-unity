using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResidentPopulationModelTests
    {
        [Test]
        public void NewRunStartsWithFourResidentsInDifferentHomes()
        {
            var model = BuildInitialModel();

            Assert.That(model.ResidentCount, Is.EqualTo(4));
            Assert.That(model.Residents.Select(item => item.residenceId).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void TwoQualifiedDaysAnnounceThenAddOneResident()
        {
            var model = BuildInitialModel();

            var first = model.CompleteDay(1, 100, true);
            Assert.That(first.ResidentArrived, Is.False);
            Assert.That(model.ProspectiveResidenceId, Is.Not.Empty);

            var announcedHome = model.ProspectiveResidenceId;
            var second = model.CompleteDay(2, 100, true);
            Assert.That(second.ResidentArrived, Is.True);
            Assert.That(second.ArrivalResidenceId, Is.EqualTo(announcedHome));
            Assert.That(model.ResidentCount, Is.EqualTo(5));
        }

        [Test]
        public void PopulationNeverExceedsEightAndArrivalsAreAtMostEveryTwoDays()
        {
            var model = BuildInitialModel();

            for (var day = 1; day <= 12; day++)
            {
                model.CompleteDay(day, 100, true);
            }

            Assert.That(model.ResidentCount, Is.EqualTo(8));
            Assert.That(model.CumulativeArrivals, Is.EqualTo(4));
        }

        [Test]
        public void BrokenHumanFunctionCancelsProspectiveArrival()
        {
            var model = BuildInitialModel();
            model.CompleteDay(1, 100, true);
            Assert.That(model.ProspectiveResidenceId, Is.Not.Empty);

            model.CompleteDay(2, 74, true);

            Assert.That(model.ProspectiveResidenceId, Is.Empty);
            Assert.That(model.ResidentCount, Is.EqualTo(4));
        }

        [Test]
        public void FullAndHalfEfficiencyProduceOneOrHalfPoint()
        {
            Assert.That(ResourceEconomyModel.FoodServiceCost(4, 2), Is.EqualTo(2));
            var model = BuildInitialModel();
            var report = model.CompleteDay(1, 100, true);

            Assert.That(report.Production, Is.GreaterThanOrEqualTo(0f));
            Assert.That(model.Residents.All(item =>
                item.lastEfficiency == 0f || item.lastEfficiency == 0.5f || item.lastEfficiency == 1f), Is.True);
        }

        private static ResidentPopulationModel BuildInitialModel()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(
                layout.ExportData(),
                RoomLayoutData.All,
                3.1f);
            return new ResidentPopulationModel(navigation, RoomLayoutData.All);
        }
    }
}
