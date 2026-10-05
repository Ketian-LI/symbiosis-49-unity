using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Image coordinates increase right/down. Grid coordinates do the same:
    // (0,0) is the top-left cell and (7,7) is the far board boundary.
    public readonly struct PhysicalBoardImageCalibration
    {
        private readonly double[] imageToGrid;
        private readonly double[] gridToImage;

        private PhysicalBoardImageCalibration(double[] imageToGrid,
            double[] gridToImage)
        {
            this.imageToGrid = imageToGrid;
            this.gridToImage = gridToImage;
        }

        public bool IsValid => imageToGrid != null && gridToImage != null;

        public static bool TryCreate(Vector2 topLeft, Vector2 topRight,
            Vector2 bottomRight, Vector2 bottomLeft,
            out PhysicalBoardImageCalibration calibration)
        {
            calibration = default;
            var imageCorners = new[] { topLeft, topRight, bottomRight, bottomLeft };
            if (!IsConvex(imageCorners)) return false;
            var side = RoomLayoutData.GridSize;
            var gridCorners = new[]
            {
                Vector2.zero, new Vector2(side, 0f),
                new Vector2(side, side), new Vector2(0f, side)
            };
            if (!TrySolve(imageCorners, gridCorners, out var forward) ||
                !TrySolve(gridCorners, imageCorners, out var reverse))
                return false;
            calibration = new PhysicalBoardImageCalibration(forward, reverse);
            return true;
        }

        public bool TryImageToGrid(Vector2 imagePoint, out Vector2 gridPoint) =>
            TryProject(imageToGrid, imagePoint, out gridPoint);

        public bool TryGridToImage(Vector2 gridPoint, out Vector2 imagePoint) =>
            TryProject(gridToImage, gridPoint, out imagePoint);

        private static bool IsConvex(IReadOnlyList<Vector2> points)
        {
            var sign = 0f;
            for (var index = 0; index < 4; index++)
            {
                var a = points[(index + 1) % 4] - points[index];
                var b = points[(index + 2) % 4] - points[(index + 1) % 4];
                var cross = a.x * b.y - a.y * b.x;
                if (float.IsNaN(cross) || float.IsInfinity(cross) ||
                    Mathf.Abs(cross) < 1f || sign != 0f && Mathf.Sign(cross) != sign)
                    return false;
                sign = Mathf.Sign(cross);
            }
            return true;
        }

        private static bool TryProject(double[] coefficients, Vector2 input,
            out Vector2 output)
        {
            output = default;
            if (coefficients == null || float.IsNaN(input.x) ||
                float.IsNaN(input.y) || float.IsInfinity(input.x) ||
                float.IsInfinity(input.y)) return false;
            var denominator = coefficients[6] * input.x +
                              coefficients[7] * input.y + 1d;
            if (Math.Abs(denominator) < 1e-10d) return false;
            var x = (coefficients[0] * input.x + coefficients[1] * input.y +
                     coefficients[2]) / denominator;
            var y = (coefficients[3] * input.x + coefficients[4] * input.y +
                     coefficients[5]) / denominator;
            if (double.IsNaN(x) || double.IsInfinity(x) ||
                double.IsNaN(y) || double.IsInfinity(y)) return false;
            output = new Vector2((float)x, (float)y);
            return true;
        }

        private static bool TrySolve(IReadOnlyList<Vector2> source,
            IReadOnlyList<Vector2> destination, out double[] coefficients)
        {
            coefficients = null;
            var equations = new double[8, 9];
            for (var index = 0; index < 4; index++)
            {
                var x = source[index].x;
                var y = source[index].y;
                var u = destination[index].x;
                var v = destination[index].y;
                var first = index * 2;
                equations[first, 0] = x;
                equations[first, 1] = y;
                equations[first, 2] = 1d;
                equations[first, 6] = -u * x;
                equations[first, 7] = -u * y;
                equations[first, 8] = u;
                equations[first + 1, 3] = x;
                equations[first + 1, 4] = y;
                equations[first + 1, 5] = 1d;
                equations[first + 1, 6] = -v * x;
                equations[first + 1, 7] = -v * y;
                equations[first + 1, 8] = v;
            }

            for (var column = 0; column < 8; column++)
            {
                var pivot = column;
                for (var row = column + 1; row < 8; row++)
                    if (Math.Abs(equations[row, column]) > Math.Abs(equations[pivot, column]))
                        pivot = row;
                if (Math.Abs(equations[pivot, column]) < 1e-10d) return false;
                for (var cell = column; cell < 9; cell++)
                    (equations[column, cell], equations[pivot, cell]) =
                        (equations[pivot, cell], equations[column, cell]);
                var scale = equations[column, column];
                for (var cell = column; cell < 9; cell++)
                    equations[column, cell] /= scale;
                for (var row = 0; row < 8; row++)
                {
                    if (row == column) continue;
                    var factor = equations[row, column];
                    for (var cell = column; cell < 9; cell++)
                        equations[row, cell] -= factor * equations[column, cell];
                }
            }
            coefficients = Enumerable.Range(0, 8)
                .Select(index => equations[index, 8]).ToArray();
            return true;
        }
    }

    public readonly struct PhysicalRoomImageObservation
    {
        public PhysicalRoomImageObservation(string roomId, Vector2 center,
            Vector2 markerTop)
        {
            RoomId = roomId;
            Center = center;
            MarkerTop = markerTop;
        }

        public string RoomId { get; }
        public Vector2 Center { get; }
        // A point toward the printed marker's top, in the same image frame.
        public Vector2 MarkerTop { get; }
    }

    public enum PhysicalProjectionError
    {
        None,
        InvalidCalibration,
        UnknownRoom,
        DuplicateRoom,
        OutsideBoard,
        CellBoundary,
        RotationUnclear
    }

    public static class PhysicalBoardProjectionModel
    {
        private const float CellBoundaryMargin = 0.12f;
        private const float RotationDominance = 1.5f;

        public static bool TryMap(PhysicalBoardImageCalibration calibration,
            IEnumerable<PhysicalRoomImageObservation> observations,
            out IReadOnlyList<RoomPlacementData> placements,
            out PhysicalProjectionError error)
        {
            placements = Array.Empty<RoomPlacementData>();
            error = PhysicalProjectionError.InvalidCalibration;
            if (!calibration.IsValid) return false;

            var knownRooms = RoomLayoutData.All.Select(room => room.Id)
                .ToHashSet(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var mapped = new List<RoomPlacementData>();
            foreach (var observation in observations ?? Array.Empty<PhysicalRoomImageObservation>())
            {
                if (string.IsNullOrEmpty(observation.RoomId) ||
                    !knownRooms.Contains(observation.RoomId))
                {
                    error = PhysicalProjectionError.UnknownRoom;
                    return false;
                }
                if (!seen.Add(observation.RoomId))
                {
                    error = PhysicalProjectionError.DuplicateRoom;
                    return false;
                }
                if (!calibration.TryImageToGrid(observation.Center, out var center) ||
                    !calibration.TryImageToGrid(observation.MarkerTop, out var top))
                {
                    error = PhysicalProjectionError.InvalidCalibration;
                    return false;
                }
                if (center.x < 0f || center.y < 0f ||
                    center.x >= RoomLayoutData.GridSize ||
                    center.y >= RoomLayoutData.GridSize)
                {
                    error = PhysicalProjectionError.OutsideBoard;
                    return false;
                }
                var column = Mathf.FloorToInt(center.x);
                var row = Mathf.FloorToInt(center.y);
                var withinColumn = center.x - column;
                var withinRow = center.y - row;
                if (withinColumn < CellBoundaryMargin ||
                    withinColumn > 1f - CellBoundaryMargin ||
                    withinRow < CellBoundaryMargin ||
                    withinRow > 1f - CellBoundaryMargin)
                {
                    error = PhysicalProjectionError.CellBoundary;
                    return false;
                }
                var direction = top - center;
                var horizontal = Mathf.Abs(direction.x);
                var vertical = Mathf.Abs(direction.y);
                if (Mathf.Max(horizontal, vertical) < 0.05f ||
                    Mathf.Max(horizontal, vertical) <
                    RotationDominance * Mathf.Min(horizontal, vertical))
                {
                    error = PhysicalProjectionError.RotationUnclear;
                    return false;
                }
                var turns = horizontal > vertical
                    ? direction.x > 0f ? 1 : 3
                    : direction.y > 0f ? 2 : 0;
                mapped.Add(new RoomPlacementData
                {
                    id = observation.RoomId,
                    column = column,
                    row = row,
                    quarterTurns = turns
                });
            }
            placements = mapped;
            error = PhysicalProjectionError.None;
            return true;
        }
    }
}
