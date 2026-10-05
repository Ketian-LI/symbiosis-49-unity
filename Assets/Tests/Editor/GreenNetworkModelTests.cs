using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class GreenNetworkModelTests
    {
        [Test]
        public void OpeningParkHasFiveConnectedGreenCellsAndElevenSeedPortions()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var map = AnimalMap(layout, 1);
            Assert.That(GreenNetworkModel.ConnectedRooms(map), Is.EquivalentTo(new[]
                { "central-park", "shared-f", "shared-h", "shared-i", "shared-j" }));
            Assert.That(HabitatFoodNetworkModel.TotalSeedCapacity(map), Is.EqualTo(11));
            Assert.That(RoomLayoutData.All.Count(room => room.GreenRole != ParkGreenRole.None), Is.EqualTo(4));
        }

        [Test]
        public void VisitorClosurePreservesGreenCoreAndTargetedSwapImprovesSeeds()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("residence-d", "pigeon-d"), Is.True);
            var disturbed = AnimalMap(layout, 2);
            Assert.That(GreenNetworkModel.ConnectedCount(disturbed), Is.GreaterThanOrEqualTo(4));
            var beforeSeeds = HabitatFoodNetworkModel.TotalSeedCapacity(disturbed, 2);
            Assert.That(layout.TrySwap("pigeon-a", "shared-j"), Is.True);
            var repaired = AnimalMap(layout, 2);
            Assert.That(GreenNetworkModel.ConnectedCount(repaired), Is.EqualTo(4));
            Assert.That(HabitatFoodNetworkModel.TotalSeedCapacity(repaired, 2),
                Is.GreaterThan(beforeSeeds));
        }

        [Test]
        public void LaterGardenMaintenanceCanStillBreakTheGreenCorridor()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(GreenNetworkModel.ConnectedCount(AnimalMap(layout, 2)),
                Is.GreaterThanOrEqualTo(4));
            Assert.That(GreenNetworkModel.ConnectedCount(AnimalMap(layout, 5)),
                Is.LessThan(GreenNetworkModel.StableCellCount));
        }

        [Test]
        public void GreenNeighboursNeedFacingAnimalPortsNotOnlyPhysicalDoors()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TryRotate("shared-h"), Is.True);
            var people = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            var animals = AnimalMap(layout, 1);
            Assert.That(people.NeighboursOf("central-park"), Does.Contain("shared-h"));
            Assert.That(animals.NeighboursOf("central-park"), Does.Not.Contain("shared-h"));
            Assert.That(GreenNetworkModel.ConnectedCount(animals), Is.EqualTo(2));
        }

        private static RoomNavigationMap AnimalMap(RoomLayoutModel layout, int day) =>
            new(layout.ExportData(), RoomLayoutData.All, 3.1f, true, day);
    }
}
