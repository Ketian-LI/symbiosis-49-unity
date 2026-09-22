using System;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteRoomLoadModelTests
    {
        [TestCase(0, WasteRoomLoadStage.Empty)]
        [TestCase(1, WasteRoomLoadStage.Low)]
        [TestCase(2, WasteRoomLoadStage.Low)]
        [TestCase(3, WasteRoomLoadStage.Medium)]
        [TestCase(5, WasteRoomLoadStage.Medium)]
        [TestCase(6, WasteRoomLoadStage.Full)]
        [TestCase(8, WasteRoomLoadStage.Full)]
        [TestCase(9, WasteRoomLoadStage.Overflow)]
        public void UnitsMapToApprovedVisualStages(int units, WasteRoomLoadStage expected)
        {
            Assert.That(WasteRoomLoadModel.StageForUnits(units), Is.EqualTo(expected));
        }

        [Test]
        public void OverflowRetainsEightStoredUnitsAndReportsExcess()
        {
            var model = new WasteRoomLoadModel();
            model.Add(11);

            Assert.That(model.StoredUnits, Is.EqualTo(8));
            Assert.That(model.OverflowUnits, Is.EqualTo(3));
            Assert.That(model.IsOverflowing, Is.True);
        }

        [Test]
        public void CollectionReturnsRoomToEmpty()
        {
            var model = new WasteRoomLoadModel();
            model.Add(9);
            model.CollectAll();

            Assert.That(model.Units, Is.Zero);
            Assert.That(model.Stage, Is.EqualTo(WasteRoomLoadStage.Empty));
        }

        [Test]
        public void AddRejectsNonPositiveAmounts()
        {
            var model = new WasteRoomLoadModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Add(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Add(-1));
        }
    }
}
