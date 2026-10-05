using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

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
        public void FormerlyMultiCellRoomsNowSwapAsIndependentSingleCells()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.Get("garage-a").Width * model.Get("garage-a").Height, Is.EqualTo(1));
            Assert.That(model.TrySwap("garage-a", "shared-a"), Is.True);
            Assert.That(model.Get("garage-a").Column, Is.EqualTo(1));
            Assert.That(model.Get("shared-a").Column, Is.EqualTo(0));
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void OverlappingTwoCellMoveCanDisplaceOneAdjacentSingle()
        {
            var model = new RoomLayoutModel(new[]
            {
                new RoomSpec("double", "两格", RoomType.Residence, 0, 0, 2, 1, true, ""),
                new RoomSpec("single", "一格", RoomType.Office, 2, 0, 1, 1, true, "")
            });

            Assert.That(model.TryPlanSwapWithSingles("double", 1, 0, out var plan), Is.True);
            Assert.That(plan.Count, Is.EqualTo(2));
            Assert.That(model.TrySwapWithSingles("double", 1, 0), Is.True);
            Assert.That(model.Get("single").Column, Is.EqualTo(0));
            Assert.That(model.CanPlace("double", 1, 0, 0), Is.True);
        }

        [Test]
        public void SingleCellBoardRejectsLegacyMultiCellSwapButGenericRuleRejectsIncompleteTargets()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            var initial = model.ExportData();

            Assert.That(model.TrySwapWithSingles("garage-a", 3, 2), Is.False,
                "No production room is multi-cell anymore.");
            Assert.That(model.TrySwapWithSingles("garage-a", 5, 1), Is.False,
                "The multi-cell swap API is not used by single-cell rooms.");
            Assert.That(model.TrySwapWithSingles("garage-a", 6, 0), Is.False);
            Assert.That(model.IsCompleteAndLegal(), Is.True);
            Assert.That(model.ExportData().Select(item => item.id + ":" + item.column + "," + item.row),
                Is.EqualTo(initial.Select(item => item.id + ":" + item.column + "," + item.row)));

            var incomplete = new RoomLayoutModel(new[]
            {
                new RoomSpec("double", "两格", RoomType.Residence, 0, 0, 2, 1, true, ""),
                new RoomSpec("single", "一格", RoomType.Office, 3, 0, 1, 1, true, "")
            });
            Assert.That(incomplete.TrySwapWithSingles("double", 3, 0), Is.False,
                "A destination with only one of two cells occupied cannot complete a swap.");
        }

        [Test]
        public void SwapRejectsFixedButAllowsDifferentTypesOfSingleCellRooms()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);

            Assert.That(model.TrySwap("residence-e", "central-park"), Is.False);
            Assert.That(model.TrySwap("residence-e", "garage-a"), Is.True);
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
            Assert.That(model.TryMoveToTray("fox-den"), Is.False);
        }

        [Test]
        public void PigeonPlazasCanSwapWithOtherSingleCellRooms()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);

            Assert.That(model.TrySwap("pigeon-b", "residence-g"), Is.True);
            Assert.That(model.CanMove("pigeon-c"), Is.True);
            Assert.That(model.CanMove("pigeon-d"), Is.True);
            Assert.That(model.TrySwap("pigeon-a", "shared-k"), Is.True);
            Assert.That(model.Get("pigeon-a").Column, Is.EqualTo(4));
            Assert.That(model.Get("pigeon-a").Row, Is.EqualTo(4));
            Assert.That(model.Get("shared-k").Column, Is.EqualTo(0));
            Assert.That(model.Get("shared-k").Row, Is.EqualTo(1));
            Assert.That(model.IsCompleteAndLegal(), Is.True);
        }

        [Test]
        public void DirectionalGarageEntranceCanRotateWithinOneCell()
        {
            var model = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(model.TryRotate("garage-a"), Is.True);
            Assert.That(model.Get("garage-a").QuarterTurns, Is.EqualTo(1));
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

        [Test]
        public void DragPreviewMovesItsPhysicsHitAreaWhileSimulationIsPaused()
        {
            var roomRoot = new GameObject("Movable Room Root");
            var previousTimeScale = Time.timeScale;
            try
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(roomRoot.transform, false);
                floor.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                floor.transform.localScale = new Vector3(2f, 0.24f, 2f);
                var collider = floor.GetComponent<Collider>();
                var view = floor.AddComponent<RoomView>();
                view.Initialize(
                    RoomLayoutData.All[23], roomRoot.transform, floor.GetComponent<Renderer>(),
                    Color.white, 2f, 2f, HideFlags.None);
                Time.timeScale = 0f;
                Physics.SyncTransforms();

                view.SetDraggedLocalPosition(new Vector3(8f, 0f, 0f));

                Assert.That(Physics.Raycast(new Vector3(0f, 5f, 0f), Vector3.down, 10f), Is.False);
                Assert.That(Physics.Raycast(new Vector3(8f, 5f, 0f), Vector3.down, out var movedHit, 10f), Is.True);
                Assert.That(movedHit.collider, Is.EqualTo(collider));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(roomRoot);
            }
        }

        [Test]
        public void CommittedVisualPlacementMovesItsPhysicsHitArea()
        {
            var roomRoot = new GameObject("Movable Room Root");
            try
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(roomRoot.transform, false);
                floor.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                floor.transform.localScale = new Vector3(2f, 0.24f, 2f);
                var collider = floor.GetComponent<Collider>();
                var view = floor.AddComponent<RoomView>();
                view.Initialize(
                    RoomLayoutData.All[23],
                    roomRoot.transform,
                    floor.GetComponent<Renderer>(),
                    Color.white,
                    2f,
                    2f,
                    HideFlags.None);
                Physics.SyncTransforms();

                Assert.That(
                    Physics.Raycast(new Vector3(0f, 5f, 0f), Vector3.down, out var originalHit, 10f),
                    Is.True);
                Assert.That(originalHit.collider, Is.EqualTo(collider));

                view.ApplyPlacement(new Vector3(8f, 0f, 0f), 0);

                Assert.That(
                    Physics.Raycast(new Vector3(0f, 5f, 0f), Vector3.down, 10f),
                    Is.False);
                Assert.That(
                    Physics.Raycast(new Vector3(8f, 5f, 0f), Vector3.down, out var movedHit, 10f),
                    Is.True);
                Assert.That(movedHit.collider, Is.EqualTo(collider));
            }
            finally
            {
                Object.DestroyImmediate(roomRoot);
            }
        }
    }
}
