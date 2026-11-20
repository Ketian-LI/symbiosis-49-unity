using NUnit.Framework;
using System.Collections.Generic;
using UrbanWildlifeRooms.CameraRecognition;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionCameraSnapshotModelTests
    {
        [Test]
        public void StarterOnlyPacket_IsValidBaseline()
        {
            var packet = Packet(Tile(10, 3, 3, "Green", "Oak"));

            Assert.That(ConstructionCameraSnapshotModel.TryValidate(packet,
                out var snapshot, out var error), Is.True, error);
            Assert.That(snapshot.IsStarterOnly, Is.True);
        }

        [Test]
        public void PythonStyleJson_ParsesAsValidBaseline()
        {
            const string json = "{\"schemaVersion\":1,\"sequence\":3," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";

            Assert.That(ConstructionCameraSnapshotModel.TryParseAndValidate(json,
                out var snapshot, out var error), Is.True, error);
            Assert.That(snapshot.Sequence, Is.EqualTo(3));
            Assert.That(snapshot.IsStarterOnly, Is.True);
        }

        [Test]
        public void MissingStarter_IsRejected()
        {
            var packet = Packet(Tile(11, 3, 3, "Green", "None"));

            Assert.That(ConstructionCameraSnapshotModel.TryValidate(packet,
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("Marker 10"));
        }

        [Test]
        public void DuplicateCell_IsRejected()
        {
            var packet = Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 3, 3, "Residence", "None"));

            Assert.That(ConstructionCameraSnapshotModel.TryValidate(packet,
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("More than one marker"));
        }

        [Test]
        public void NonGreenPlanting_IsRejected()
        {
            var packet = Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 3, 2, "Residence", "Meadow"));

            Assert.That(ConstructionCameraSnapshotModel.TryValidate(packet,
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("invalid category/planting"));
        }

        [Test]
        public void NonstarterGreenMustUsePositionResolvedPlanting()
        {
            Assert.That(ConstructionCameraSnapshotModel.TryValidate(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(12, 3, 2, "Green", "Meadow")),
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("generic green tile"));
        }

        [Test]
        public void SparePieceIdentifiersExtendToEightyPieces()
        {
            AssertValid(Packet(Tile(10, 3, 3, "Green", "Oak"),
                Tile(89, 4, 3, "Square", "None")), out var snapshot);
            Assert.That(snapshot.Tiles.Count, Is.EqualTo(2));
            Assert.That(ConstructionCameraSnapshotModel.TryValidate(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(90, 4, 3, "Square", "None")),
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("outside"));
        }

        [Test]
        public void SingleUnchangedAddition_IsReturned()
        {
            AssertValid(Packet(Tile(10, 3, 3, "Green", "Oak")), out var baseline);
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(12, 3, 2, "Green", "None")), out var next);

            Assert.That(ConstructionCameraSnapshotModel.TryGetSingleAddition(
                baseline, next, out var addition, out var error), Is.True, error);
            Assert.That(addition.MarkerId, Is.EqualTo(12));
            Assert.That(addition.Category, Is.EqualTo(ConstructionCategory.Green));
            Assert.That(addition.Planting, Is.EqualTo(GreenPlanting.None));
        }

        [Test]
        public void MovingExistingMarker_IsRejectedAsAddition()
        {
            AssertValid(Packet(Tile(10, 3, 3, "Green", "Oak")), out var baseline);
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 3, 2, "Residence", "None")), out var first);
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 2, 3, "Residence", "None"),
                Tile(12, 3, 2, "Green", "None")), out var moved);

            Assert.That(ConstructionCameraSnapshotModel.TryGetSingleAddition(
                first, moved, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("moved or changed"));
        }

        [TestCase(ResidenceKind.Standard)]
        [TestCase(ResidenceKind.Garden)]
        [TestCase(ResidenceKind.Urban)]
        public void ResumedBoardMatchesSavedRoomsWithoutStoredMarkerIds(
            ResidenceKind savedKind)
        {
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(23, 3, 2, "Green", "None"),
                Tile(32, 4, 2, "Residence", "None", "Standard")),
                out var snapshot);
            var saved = new List<ConstructionTileData>
            {
                Built(3, 3, ConstructionCategory.Green, GreenPlanting.Oak),
                Built(3, 2, ConstructionCategory.Green, GreenPlanting.Meadow),
                Built(4, 2, ConstructionCategory.Residence,
                    GreenPlanting.None, savedKind)
            };

            Assert.That(ConstructionCameraSnapshotModel.MatchesBuiltTiles(snapshot,
                saved, out var error), Is.True, error);
        }

        [Test]
        public void ResumedBoardRejectsWrongCategoryOrPosition()
        {
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(32, 4, 2, "Residence", "None")),
                out var snapshot);
            var saved = new List<ConstructionTileData>
            {
                Built(3, 3, ConstructionCategory.Green, GreenPlanting.Oak),
                Built(4, 2, ConstructionCategory.Residence)
            };

            saved[1].category = ConstructionCategory.Workshop;
            Assert.That(ConstructionCameraSnapshotModel.MatchesBuiltTiles(snapshot,
                saved, out var error), Is.False);
            Assert.That(error, Does.Contain("does not match"));
            saved[1].category = ConstructionCategory.Residence;
            saved[1].column = 5;
            Assert.That(ConstructionCameraSnapshotModel.MatchesBuiltTiles(snapshot,
                saved, out error), Is.False);
        }

        [Test]
        public void OldResidencePacketDefaultsToStandardAndNonResidenceCannotClaimKind()
        {
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 4, 3, "Residence", "None")), out var snapshot);
            Assert.That(snapshot.Tiles[1].ResidenceKind,
                Is.EqualTo(ResidenceKind.Standard));

            Assert.That(ConstructionCameraSnapshotModel.TryValidate(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(14, 3, 2, "Street", "None", "Urban")),
                out _, out var error), Is.False);
            Assert.That(error, Does.Contain("invalid residence kind"));
        }

        [TestCase("Garden")]
        [TestCase("Urban")]
        public void LegacyResidenceMarkerIsTreatedAsOrdinaryHome(
            string legacyKind)
        {
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 4, 3, "Residence", "None", legacyKind)),
                out var snapshot);
            Assert.That(snapshot.Tiles[1].ResidenceKind,
                Is.EqualTo(ResidenceKind.Standard));
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 4, 3, "Residence", "None", "Standard")),
                out var ordinarySnapshot);
            Assert.That(snapshot.Signature,
                Is.EqualTo(ordinarySnapshot.Signature));

            var saved = new List<ConstructionTileData>
            {
                Built(3, 3, ConstructionCategory.Green, GreenPlanting.Oak),
                Built(4, 3, ConstructionCategory.Residence,
                    GreenPlanting.None, ResidenceKind.Urban)
            };
            Assert.That(ConstructionCameraSnapshotModel.MatchesBuiltTiles(
                snapshot, saved, out var error), Is.True, error);
        }

        [Test]
        public void ChangingALegacyResidenceLabelDoesNotChangeThePhysicalPiece()
        {
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 4, 3, "Residence", "None", "Standard")),
                out var baseline);
            AssertValid(Packet(
                Tile(10, 3, 3, "Green", "Oak"),
                Tile(13, 4, 3, "Residence", "None", "Garden"),
                Tile(14, 3, 2, "Street", "None")), out var next);
            Assert.That(ConstructionCameraSnapshotModel.TryGetSingleAddition(
                baseline, next, out var addition, out var error), Is.True, error);
            Assert.That(addition.MarkerId, Is.EqualTo(14));
        }

        private static void AssertValid(ConstructionCameraPacket packet,
            out ConstructionCameraSnapshot snapshot)
        {
            Assert.That(ConstructionCameraSnapshotModel.TryValidate(packet,
                out snapshot, out var error), Is.True, error);
        }

        private static ConstructionCameraPacket Packet(
            params ConstructionCameraTilePacket[] tiles) => new()
        {
            schemaVersion = 1,
            sequence = 7,
            calibrated = true,
            cornerCount = 4,
            error = string.Empty,
            tiles = tiles
        };

        private static ConstructionCameraTilePacket Tile(int markerId,
            int column, int row, string category, string planting,
            string residenceKind = null) => new()
        {
            markerId = markerId,
            column = column,
            row = row,
            category = category,
            planting = planting,
            residenceKind = residenceKind
        };

        private static ConstructionTileData Built(int column, int row,
            ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None,
            ResidenceKind residenceKind = ResidenceKind.Standard) => new()
        {
            column = column,
            row = row,
            category = category,
            planting = planting,
            residenceKind = residenceKind
        };
    }
}
