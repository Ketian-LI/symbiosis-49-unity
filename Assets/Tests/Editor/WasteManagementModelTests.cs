using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteManagementModelTests
    {
        [Test]
        public void DailyBaselineProducesTenUnits()
        {
            var model = BuildInitialModel();

            model.ProduceDailyWaste(4, 2, true);

            Assert.That(model.TotalWasteUnits, Is.EqualTo(10));
        }

        [Test]
        public void WasteUsesNearestReachableRoom()
        {
            var specs = LinearSpecs();
            var model = new WasteManagementModel(BuildNavigation(specs, specs), specs);

            Assert.That(model.RouteWaste("source", 2), Is.True);
            Assert.That(model.WasteRooms["trash-near"].Units, Is.EqualTo(2));
            Assert.That(model.WasteRooms["trash-far"].Units, Is.Zero);
        }

        [Test]
        public void BlockedWasteReroutesAfterConnectivityReturns()
        {
            var specs = LinearSpecs();
            var isolatedSource = new[] { specs.Single(room => room.Id == "source") };
            var model = new WasteManagementModel(BuildNavigation(isolatedSource, specs), specs);

            Assert.That(model.RouteWaste("source", 3), Is.False);
            Assert.That(model.BlockedWaste["source"], Is.EqualTo(3));

            model.UpdateNavigation(BuildNavigation(specs, specs));

            Assert.That(model.BlockedWaste, Is.Empty);
            Assert.That(model.WasteRooms["trash-near"].Units, Is.EqualTo(3));
        }

        [Test]
        public void PenaltyStacksPerAffectedRoomAndCapsAtTwenty()
        {
            var model = BuildInitialModel();
            var states = model.WasteRooms.Keys
                .Select(id => new WasteRoomSaveData { id = id, units = 9 })
                .ToList();
            model.Restore(
                states,
                new[] { new BlockedWasteSaveData { producerRoomId = "residence-a", units = 1 } });

            Assert.That(model.HumanFunctionPenalty, Is.EqualTo(20));
        }

        [Test]
        public void EmergencyCollectionRequiresOverflowAndTwoResources()
        {
            var model = BuildInitialModel();
            model.WasteRooms["trash-a"].Restore(9);

            Assert.That(model.TryEmergencyCollect("trash-a", 1, out var rejectedCost), Is.False);
            Assert.That(rejectedCost, Is.Zero);
            Assert.That(model.TryEmergencyCollect("trash-a", 2, out var acceptedCost), Is.True);
            Assert.That(acceptedCost, Is.EqualTo(2));
            Assert.That(model.WasteRooms["trash-a"].Units, Is.Zero);
        }

        [Test]
        public void MunicipalCollectionEmptiesAllWasteRooms()
        {
            var model = BuildInitialModel();
            model.ProduceDailyWaste(8, 2, true);

            model.MunicipalCollectAll();

            Assert.That(model.WasteRooms.Values.All(room => room.Units == 0), Is.True);
        }

        private static WasteManagementModel BuildInitialModel()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            return new WasteManagementModel(navigation, RoomLayoutData.All);
        }

        private static RoomSpec[] LinearSpecs()
        {
            return new[]
            {
                new RoomSpec("source", "Source", RoomType.Residence, 0, 0, 1, 1, true, ""),
                new RoomSpec("trash-near", "Near", RoomType.Trash, 1, 0, 1, 1, true, ""),
                new RoomSpec("middle", "Middle", RoomType.Office, 2, 0, 1, 1, true, ""),
                new RoomSpec("trash-far", "Far", RoomType.Trash, 3, 0, 1, 1, true, "")
            };
        }

        private static RoomNavigationMap BuildNavigation(
            IEnumerable<RoomSpec> placedRooms,
            IEnumerable<RoomSpec> allSpecs)
        {
            var placements = placedRooms.Select(room => new RoomPlacementData
            {
                id = room.Id,
                column = room.Column,
                row = room.Row,
                quarterTurns = 0
            });
            return new RoomNavigationMap(placements, allSpecs, 3.1f);
        }
    }
}
