using NUnit.Framework;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomLayoutModelTests
    {
        [Test]
        public void InitialLayoutIsCompleteAndLegal()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void HoldingTrayEnablesAThreeStepRoomSwap()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);

            Assert.That(model.TryMoveToTray("residence-e"), Is.True);
            Assert.That(model.IsCompleteAndLegal(), Is.False);
            Assert.That(model.TryPlace("residence-f", 0, 5, 0), Is.True);
            Assert.That(model.TryPlace("residence-e", 1, 5, 0), Is.True);
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void SameSizeMovableRoomsCanSwapInOneStep()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);

            Assert.That(model.CanSwap("residence-e", "residence-f"), Is.True);
            Assert.That(model.TrySwap("residence-e", "residence-f"), Is.True);
            Assert.That(model.Get("residence-e").Column, Is.EqualTo(1));
            Assert.That(model.Get("residence-f").Column, Is.EqualTo(0));
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void SwapRejectsFixedOrDifferentSizeRooms()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);

            Assert.That(model.TrySwap("residence-e", "central-park"), Is.False);
            Assert.That(model.TrySwap("residence-e", "garage-a"), Is.False);
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void TrayAcceptsOnlyOneRoom()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.TryMoveToTray("residence-e"), Is.True);
            Assert.That(model.TryMoveToTray("residence-f"), Is.False);
        }

        [Test]
        public void FixedRoomsCannotMoveToTray()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.TryMoveToTray("central-park"), Is.False);
            Assert.That(model.TryMoveToTray("pigeon-a"), Is.False);
            Assert.That(model.TryMoveToTray("fox-den"), Is.False);
        }

        [Test]
        public void RotationIsRejectedWhenRotatedFootprintWouldOverlap()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.TryRotate("garage-a"), Is.False);
            Assert.That(model.Get("garage-a").QuarterTurns, Is.EqualTo(0));
        }

        [Test]
        public void SnapshotRestoresLayoutAndTrayState()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            var snapshot = model.CaptureSnapshot();
            Assert.That(model.TryMoveToTray("residence-e"), Is.True);
            model.Restore(snapshot);
            Assert.That(model.TrayOccupied, Is.False);
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void ExportedLayoutCanBeRestoredWithoutStartingANewRun()
        {
            var source = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(source.TryMoveToTray("residence-e"), Is.True);
            Assert.That(source.TryPlace("residence-f", 0, 5, 0), Is.True);
            Assert.That(source.TryPlace("residence-e", 1, 5, 0), Is.True);

            var restored = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(restored.TryRestore(source.ExportData()), Is.True);
            Assert.That(restored.Get("residence-e").Column, Is.EqualTo(1));
            Assert.That(restored.Get("residence-f").Column, Is.EqualTo(0));
            Assert.That(restored.IsCompleteAndLegal(), Is.True);
        }
    }
}
