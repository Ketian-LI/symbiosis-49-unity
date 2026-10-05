using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ShrubShelterModelTests
    {
        [Test]
        public void MovedShrubCreatesCoverOnlyAfterTwoDayBoundaries()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var shelter = new ShrubShelterModel();
            Assert.That(shelter.ConnectedPairCount(Navigation(layout), 1), Is.Zero);

            Assert.That(layout.TrySwap("shrub-a", "pigeon-d"), Is.True);
            var navigation = Navigation(layout);
            Assert.That(shelter.ConnectedPairCount(navigation, 1,
                new HashSet<string> { "shrub-a" }), Is.Zero);
            shelter.RecordMovement(new[] { "shrub-a", "pigeon-d" }, 1);
            Assert.That(shelter.DaysUntilReady("shrub-a", 2), Is.EqualTo(1));
            Assert.That(shelter.RecoveringShrubCount(2), Is.EqualTo(1));
            Assert.That(shelter.IsCovered(navigation, "shrub-a", 2), Is.False);
            Assert.That(shelter.ConnectedPairCount(navigation, 3), Is.EqualTo(2));
            Assert.That(shelter.RecoveringShrubCount(3), Is.Zero);
            Assert.That(shelter.IsCovered(navigation, "shrub-c", 3), Is.True);
        }

        [Test]
        public void ConnectedCoverOffersARealGarageFreeForagingRoute()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("shrub-a", "pigeon-d"), Is.True);
            var navigation = Navigation(layout);
            var shelter = new ShrubShelterModel();
            shelter.RecordMovement(new[] { "shrub-a" }, 1);

            Assert.That(shelter.TryFindCoveredRoute(navigation, "trash-d", "shrub-b", 2,
                out _), Is.False);
            Assert.That(shelter.TryFindCoveredRoute(navigation, "trash-d", "shrub-b", 3,
                out var route), Is.True);
            Assert.That(route, Is.EqualTo(new[] { "trash-d", "shrub-a", "shrub-b" }));
        }

        [Test]
        public void SavedRecoveryResumesAndOldSavesDefaultToReady()
        {
            var shelter = new ShrubShelterModel();
            shelter.RecordMovement(new[] { "shrub-b" }, 5);
            var saved = shelter.Export();
            var restored = new ShrubShelterModel();
            restored.Restore(saved);
            Assert.That(restored.DaysUntilReady("shrub-b", 6), Is.EqualTo(1));
            Assert.That(restored.DaysUntilReady("shrub-b", 7), Is.Zero);
            restored.Restore(null);
            Assert.That(restored.DaysUntilReady("shrub-b", 6), Is.Zero);
        }

        private static RoomNavigationMap Navigation(RoomLayoutModel layout)
        {
            return new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
        }
    }
}
