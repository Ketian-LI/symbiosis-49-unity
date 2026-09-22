using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PhysicalBoardRecognitionModelTests
    {
        [Test]
        public void CompleteInitialThirtyFiveModuleLayoutIsLegal()
        {
            var placements = new RoomLayoutModel(RoomLayoutData.All).ExportData();
            var result = PhysicalBoardRecognitionModel.Validate(placements);

            Assert.That(result.IsCompleteAndLegal, Is.True);
            Assert.That(result.RecognisedModuleCount, Is.EqualTo(35));
            Assert.That(result.AffectedCells, Is.Empty);
        }

        [Test]
        public void MissingModuleReportsIncompleteCells()
        {
            var placements = new RoomLayoutModel(RoomLayoutData.All).ExportData();
            placements.RemoveAt(0);
            var result = PhysicalBoardRecognitionModel.Validate(placements);

            Assert.That(result.IsCompleteAndLegal, Is.False);
            Assert.That(result.RecognisedModuleCount, Is.EqualTo(34));
            Assert.That(result.AffectedCells, Is.Not.Empty);
        }

        [Test]
        public void StableLayoutRequiresUnchangedOnePointFiveSeconds()
        {
            var tracker = new PhysicalBoardStabilityTracker();
            tracker.Observe("a", 0.8f);
            tracker.Observe("a", 0.8f);
            Assert.That(tracker.Progress, Is.EqualTo(0.8f / 1.5f).Within(0.001f));
            tracker.Observe("a", 0.7f);
            Assert.That(tracker.Progress, Is.EqualTo(1f));
            tracker.Observe("b", 0.5f);
            Assert.That(tracker.Progress, Is.Zero);
        }
    }
}
