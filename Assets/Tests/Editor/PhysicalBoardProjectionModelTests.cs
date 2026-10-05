using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PhysicalBoardProjectionModelTests
    {
        [Test]
        public void PerspectiveCalibrationRoundTripsBoardCoordinates()
        {
            Assert.That(PhysicalBoardImageCalibration.TryCreate(
                new Vector2(120f, 80f), new Vector2(900f, 120f),
                new Vector2(840f, 800f), new Vector2(70f, 710f),
                out var calibration), Is.True);
            foreach (var grid in new[]
                     {
                         new Vector2(0f, 0f), new Vector2(7f, 7f),
                         new Vector2(2.5f, 3.5f), new Vector2(6.5f, 0.5f)
                     })
            {
                Assert.That(calibration.TryGridToImage(grid, out var image), Is.True);
                Assert.That(calibration.TryImageToGrid(image, out var recovered), Is.True);
                Assert.That(recovered.x, Is.EqualTo(grid.x).Within(0.002f));
                Assert.That(recovered.y, Is.EqualTo(grid.y).Within(0.002f));
            }
        }

        [Test]
        public void FortyNineSyntheticRoomMarkersBecomeOneLegalBoard()
        {
            var calibration = Calibration();
            var observations = new List<PhysicalRoomImageObservation>();
            foreach (var room in RoomLayoutData.All)
            {
                var center = new Vector2(room.Column + 0.5f, room.Row + 0.5f);
                Assert.That(calibration.TryGridToImage(center, out var imageCenter), Is.True);
                Assert.That(calibration.TryGridToImage(center + Vector2.down * 0.25f,
                    out var imageTop), Is.True);
                observations.Add(new PhysicalRoomImageObservation(
                    room.Id, imageCenter, imageTop));
            }
            Assert.That(PhysicalBoardProjectionModel.TryMap(calibration,
                observations, out var placements, out var error), Is.True, error.ToString());
            Assert.That(error, Is.EqualTo(PhysicalProjectionError.None));
            Assert.That(placements, Has.Count.EqualTo(49));
            Assert.That(PhysicalBoardRecognitionModel.Validate(placements).IsCompleteAndLegal,
                Is.True);
        }

        [TestCase(0, 0f, -0.25f)]
        [TestCase(1, 0.25f, 0f)]
        [TestCase(2, 0f, 0.25f)]
        [TestCase(3, -0.25f, 0f)]
        public void MarkerTopDeterminesFourRoomRotations(int expectedTurns,
            float xOffset, float yOffset)
        {
            var calibration = Calibration();
            var center = new Vector2(3.5f, 4.5f);
            calibration.TryGridToImage(center, out var imageCenter);
            calibration.TryGridToImage(center + new Vector2(xOffset, yOffset),
                out var imageTop);
            Assert.That(PhysicalBoardProjectionModel.TryMap(calibration,
                new[] { new PhysicalRoomImageObservation("office-a", imageCenter, imageTop) },
                out var placements, out var error), Is.True, error.ToString());
            Assert.That(placements[0].column, Is.EqualTo(3));
            Assert.That(placements[0].row, Is.EqualTo(4));
            Assert.That(placements[0].quarterTurns, Is.EqualTo(expectedTurns));
        }

        [Test]
        public void RejectsDegenerateCalibrationAndUncertainMarkers()
        {
            Assert.That(PhysicalBoardImageCalibration.TryCreate(
                Vector2.zero, Vector2.right, Vector2.right * 2f,
                Vector2.right * 3f, out _), Is.False);

            var calibration = Calibration();
            calibration.TryGridToImage(new Vector2(1.05f, 2.5f), out var edge);
            calibration.TryGridToImage(new Vector2(1.05f, 2.25f), out var edgeTop);
            Assert.That(PhysicalBoardProjectionModel.TryMap(calibration,
                new[] { new PhysicalRoomImageObservation("office-a", edge, edgeTop) },
                out _, out var edgeError), Is.False);
            Assert.That(edgeError, Is.EqualTo(PhysicalProjectionError.CellBoundary));

            calibration.TryGridToImage(new Vector2(2.5f, 2.5f), out var center);
            calibration.TryGridToImage(new Vector2(2.7f, 2.3f), out var diagonalTop);
            Assert.That(PhysicalBoardProjectionModel.TryMap(calibration,
                new[] { new PhysicalRoomImageObservation("office-a", center, diagonalTop) },
                out _, out var rotationError), Is.False);
            Assert.That(rotationError, Is.EqualTo(PhysicalProjectionError.RotationUnclear));

            calibration.TryGridToImage(new Vector2(2.5f, 2.25f), out var north);
            var sameRoom = new PhysicalRoomImageObservation("office-a", center, north);
            Assert.That(PhysicalBoardProjectionModel.TryMap(calibration,
                new[] { sameRoom, sameRoom }, out _, out var duplicateError), Is.False);
            Assert.That(duplicateError, Is.EqualTo(PhysicalProjectionError.DuplicateRoom));
        }

        private static PhysicalBoardImageCalibration Calibration()
        {
            Assert.That(PhysicalBoardImageCalibration.TryCreate(
                new Vector2(120f, 80f), new Vector2(900f, 120f),
                new Vector2(840f, 800f), new Vector2(70f, 710f),
                out var calibration), Is.True);
            return calibration;
        }
    }
}
