using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.CameraRecognition
{
    public sealed class ConstructionCameraTile
    {
        public int MarkerId { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public ConstructionCategory Category { get; set; }
        public GreenPlanting Planting { get; set; }
        public ResidenceKind ResidenceKind { get; set; }
    }

    public sealed class ConstructionCameraSnapshot
    {
        public long Sequence { get; set; }
        public IReadOnlyList<ConstructionCameraTile> Tiles { get; set; } =
            Array.Empty<ConstructionCameraTile>();
        public string Signature { get; set; } = string.Empty;

        public bool IsStarterOnly => Tiles.Count == 1 &&
            Tiles[0].MarkerId == ConstructionCameraSnapshotModel.StarterMarkerId;
    }

    public static class ConstructionCameraSnapshotModel
    {
        public const int SchemaVersion = 1;
        public const int FirstTileMarkerId = 10;
        public const int LastTileMarkerId = 89;
        public const int StarterMarkerId = 10;

        public static bool TryParseAndValidate(string json,
            out ConstructionCameraSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Camera packet is empty.";
                return false;
            }

            ConstructionCameraPacket packet;
            try
            {
                packet = JsonUtility.FromJson<ConstructionCameraPacket>(json);
            }
            catch (Exception exception)
            {
                error = $"Camera packet is not valid JSON: {exception.Message}";
                return false;
            }

            return TryValidate(packet, out snapshot, out error);
        }

        public static bool TryValidate(ConstructionCameraPacket packet,
            out ConstructionCameraSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (packet == null)
            {
                error = "Camera packet is missing.";
                return false;
            }
            if (packet.schemaVersion != SchemaVersion)
            {
                error = $"Unsupported camera schema {packet.schemaVersion}.";
                return false;
            }
            if (!packet.calibrated || packet.cornerCount != 4)
            {
                error = $"Board calibration needs four corner markers; found {packet.cornerCount}.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(packet.error))
            {
                error = $"Camera detector reported: {packet.error}";
                return false;
            }

            var input = packet.tiles ?? Array.Empty<ConstructionCameraTilePacket>();
            var markerIds = new HashSet<int>();
            var cells = new HashSet<(int column, int row)>();
            var tiles = new List<ConstructionCameraTile>(input.Length);
            foreach (var item in input)
            {
                if (item == null)
                {
                    error = "Camera packet contains a null tile.";
                    return false;
                }
                if (item.markerId < FirstTileMarkerId || item.markerId > LastTileMarkerId)
                {
                    error = $"Tile marker {item.markerId} is outside {FirstTileMarkerId}-{LastTileMarkerId}.";
                    return false;
                }
                if (!markerIds.Add(item.markerId))
                {
                    error = $"Tile marker {item.markerId} was detected more than once.";
                    return false;
                }
                if (item.column < 0 || item.column >= ConstructionBoardModel.Size ||
                    item.row < 0 || item.row >= ConstructionBoardModel.Size)
                {
                    error = $"Marker {item.markerId} is outside the 7x7 board.";
                    return false;
                }
                if (!cells.Add((item.column, item.row)))
                {
                    error = $"More than one marker occupies column {item.column}, row {item.row}.";
                    return false;
                }
                if (!Enum.TryParse(item.category, false, out ConstructionCategory category) ||
                    !Enum.IsDefined(typeof(ConstructionCategory), category))
                {
                    error = $"Marker {item.markerId} has unknown category '{item.category}'.";
                    return false;
                }
                var plantingText = string.IsNullOrWhiteSpace(item.planting)
                    ? nameof(GreenPlanting.None) : item.planting;
                if (!Enum.TryParse(plantingText, false, out GreenPlanting planting) ||
                    !Enum.IsDefined(typeof(GreenPlanting), planting))
                {
                    error = $"Marker {item.markerId} has unknown planting '{item.planting}'.";
                    return false;
                }
                if (category != ConstructionCategory.Green &&
                    planting != GreenPlanting.None)
                {
                    error = $"Marker {item.markerId} has an invalid category/planting combination.";
                    return false;
                }
                if (category == ConstructionCategory.Green &&
                    item.markerId != StarterMarkerId &&
                    planting != GreenPlanting.None)
                {
                    error = $"Marker {item.markerId} must be a generic green tile; " +
                            "vegetation is decided by its position.";
                    return false;
                }
                var residenceText = string.IsNullOrWhiteSpace(item.residenceKind)
                    ? nameof(ResidenceKind.Standard) : item.residenceKind;
                if (!Enum.TryParse(residenceText, false, out ResidenceKind residenceKind) ||
                    !Enum.IsDefined(typeof(ResidenceKind), residenceKind) ||
                    category != ConstructionCategory.Residence &&
                    residenceKind != ResidenceKind.Standard)
                {
                    error = $"Marker {item.markerId} has an invalid residence kind.";
                    return false;
                }
                // Legacy tabletop packets may still name Garden or Urban.
                // All residence markers now represent the same ordinary home;
                // earned conditions, not the piece, determine its bonuses.
                if (category == ConstructionCategory.Residence)
                    residenceKind = ResidenceKind.Standard;
                tiles.Add(new ConstructionCameraTile
                {
                    MarkerId = item.markerId,
                    Column = item.column,
                    Row = item.row,
                    Category = category,
                    Planting = planting,
                    ResidenceKind = residenceKind
                });
            }

            var starter = tiles.SingleOrDefault(item => item.MarkerId == StarterMarkerId);
            if (starter == null || starter.Column != ConstructionBoardModel.StarterColumn ||
                starter.Row != ConstructionBoardModel.StarterRow ||
                starter.Category != ConstructionCategory.Green ||
                starter.Planting != GreenPlanting.Oak)
            {
                error = "Marker 10 must be the starter oak at column 3, row 3 (zero based).";
                return false;
            }

            var ordered = tiles.OrderBy(item => item.MarkerId).ToArray();
            snapshot = new ConstructionCameraSnapshot
            {
                Sequence = packet.sequence,
                Tiles = ordered,
                Signature = Signature(ordered)
            };
            return true;
        }

        public static bool TryGetSingleAddition(ConstructionCameraSnapshot baseline,
            ConstructionCameraSnapshot next, out ConstructionCameraTile addition,
            out string error)
        {
            addition = null;
            error = string.Empty;
            if (baseline == null || next == null)
            {
                error = "Both camera snapshots are required.";
                return false;
            }
            if (next.Tiles.Count != baseline.Tiles.Count + 1)
            {
                error = "The physical board must add exactly one tile at a time.";
                return false;
            }

            var nextByMarker = next.Tiles.ToDictionary(item => item.MarkerId);
            foreach (var previous in baseline.Tiles)
            {
                if (!nextByMarker.TryGetValue(previous.MarkerId, out var current) ||
                    !SameTile(previous, current))
                {
                    error = $"Existing marker {previous.MarkerId} moved or changed type.";
                    return false;
                }
            }

            addition = next.Tiles.Single(item => baseline.Tiles.All(
                previous => previous.MarkerId != item.MarkerId));
            return true;
        }

        // Saves do not include physical marker IDs. Compare occupied cells to
        // synchronise a resumed tabletop run without guessing marker IDs.
        // Ignore saved residence subtypes: an old Garden or Urban home is the
        // same physical piece as an ordinary home in the current rules.
        public static bool MatchesBuiltTiles(ConstructionCameraSnapshot snapshot,
            IReadOnlyList<ConstructionTileData> builtTiles, out string error)
        {
            error = string.Empty;
            if (snapshot == null || builtTiles == null)
            {
                error = "A recognised board and saved run are required.";
                return false;
            }
            if (snapshot.Tiles.Count != builtTiles.Count)
            {
                error = $"Physical board has {snapshot.Tiles.Count} rooms; " +
                        $"saved run has {builtTiles.Count}.";
                return false;
            }
            var occupied = new Dictionary<(int column, int row), ConstructionTileData>();
            foreach (var tile in builtTiles)
            {
                if (tile == null || !occupied.TryAdd((tile.column, tile.row), tile))
                {
                    error = "Saved run has an invalid or duplicate room position.";
                    return false;
                }
            }
            foreach (var marker in snapshot.Tiles)
            {
                if (!occupied.TryGetValue((marker.Column, marker.Row), out var tile) ||
                    tile.category != marker.Category ||
                    (marker.Planting != GreenPlanting.None &&
                     tile.planting != marker.Planting))
                {
                    error = $"Physical marker {marker.MarkerId} does not match " +
                            $"the saved room at column {marker.Column}, row {marker.Row}.";
                    return false;
                }
            }
            return true;
        }

        private static string Signature(IEnumerable<ConstructionCameraTile> tiles) =>
            string.Join("|", tiles.OrderBy(item => item.MarkerId).Select(item =>
                $"{item.MarkerId}:{item.Column},{item.Row}:{item.Category}:" +
                $"{item.Planting}:{item.ResidenceKind}"));

        private static bool SameTile(ConstructionCameraTile left,
            ConstructionCameraTile right) => left.MarkerId == right.MarkerId &&
            left.Column == right.Column && left.Row == right.Row &&
            left.Category == right.Category && left.Planting == right.Planting &&
            left.ResidenceKind == right.ResidenceKind;
    }
}
