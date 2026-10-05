using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Data
{
    [Serializable]
    public sealed class RoomPlacementData
    {
        public string id;
        public int column;
        public int row;
        public int quarterTurns;
    }

    [Serializable]
    public sealed class RoomPlacement
    {
        public RoomPlacement(RoomSpec spec)
        {
            Id = spec.Id;
            BaseWidth = spec.Width;
            BaseHeight = spec.Height;
            Column = spec.Column;
            Row = spec.Row;
            canRotate = AnimalPassageLayout.CanRotate(spec);
        }

        private RoomPlacement(RoomPlacement source)
        {
            Id = source.Id;
            BaseWidth = source.BaseWidth;
            BaseHeight = source.BaseHeight;
            Column = source.Column;
            Row = source.Row;
            QuarterTurns = source.QuarterTurns;
            InTray = source.InTray;
            canRotate = source.canRotate;
        }

        private readonly bool canRotate;

        public string Id { get; }
        public int BaseWidth { get; }
        public int BaseHeight { get; }
        public int Column { get; internal set; }
        public int Row { get; internal set; }
        public int QuarterTurns { get; internal set; }
        public bool InTray { get; internal set; }
        public int Width => QuarterTurns % 2 == 0 ? BaseWidth : BaseHeight;
        public int Height => QuarterTurns % 2 == 0 ? BaseHeight : BaseWidth;
        public bool CanRotate => canRotate;

        public RoomPlacement Clone()
        {
            return new RoomPlacement(this);
        }
    }

    /// <summary>
    /// Pure layout rules used by both mouse editing and future camera recognition.
    /// </summary>
    public sealed class RoomLayoutModel
    {
        private readonly Dictionary<string, RoomSpec> specs;
        private readonly Dictionary<string, RoomPlacement> placements;

        public RoomLayoutModel(IEnumerable<RoomSpec> roomSpecs)
        {
            specs = roomSpecs.ToDictionary(room => room.Id);
            placements = specs.Values.ToDictionary(room => room.Id, room => new RoomPlacement(room));
        }

        public IReadOnlyCollection<RoomPlacement> All => placements.Values;
        public string TrayRoomId => placements.Values.FirstOrDefault(item => item.InTray)?.Id;
        public bool TrayOccupied => TrayRoomId != null;

        public RoomPlacement Get(string id)
        {
            return placements[id];
        }

        public bool CanMove(string id)
        {
            return specs.TryGetValue(id, out var spec) && spec.Movable;
        }

        public bool CanPlace(string id, int column, int row, int quarterTurns)
        {
            if (!placements.TryGetValue(id, out var current))
            {
                return false;
            }

            var turns = NormalizeTurns(quarterTurns);
            var width = turns % 2 == 0 ? current.BaseWidth : current.BaseHeight;
            var height = turns % 2 == 0 ? current.BaseHeight : current.BaseWidth;
            if (column < 0 || row < 0 || column + width > RoomLayoutData.GridSize || row + height > RoomLayoutData.GridSize)
            {
                return false;
            }

            foreach (var other in placements.Values)
            {
                if (other.Id == id || other.InTray)
                {
                    continue;
                }

                if (RectanglesOverlap(column, row, width, height, other.Column, other.Row, other.Width, other.Height))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlace(string id, int column, int row, int quarterTurns)
        {
            if (!CanMove(id) || !CanPlace(id, column, row, quarterTurns))
            {
                return false;
            }

            var placement = placements[id];
            placement.Column = column;
            placement.Row = row;
            placement.QuarterTurns = NormalizeTurns(quarterTurns);
            placement.InTray = false;
            return true;
        }

        public bool CanSwap(string firstId, string secondId)
        {
            return placements.TryGetValue(firstId, out var first) &&
                   CanSwap(firstId, secondId, first.QuarterTurns);
        }

        public bool CanSwap(string firstId, string secondId, int firstQuarterTurns)
        {
            if (firstId == secondId || !CanMove(firstId) || !CanMove(secondId) ||
                !placements.TryGetValue(firstId, out var first) ||
                !placements.TryGetValue(secondId, out var second) ||
                first.InTray || second.InTray ||
                first.Width != second.Width || first.Height != second.Height ||
                (NormalizeTurns(firstQuarterTurns) % 2 == 0 ? first.BaseWidth : first.BaseHeight) != second.Width ||
                (NormalizeTurns(firstQuarterTurns) % 2 == 0 ? first.BaseHeight : first.BaseWidth) != second.Height)
            {
                return false;
            }

            var firstTurns = first.QuarterTurns;
            var firstColumn = first.Column;
            var firstRow = first.Row;
            var secondColumn = second.Column;
            var secondRow = second.Row;
            first.Column = secondColumn;
            first.Row = secondRow;
            first.QuarterTurns = NormalizeTurns(firstQuarterTurns);
            second.Column = firstColumn;
            second.Row = firstRow;

            var legal = CanPlace(firstId, first.Column, first.Row, first.QuarterTurns) &&
                        CanPlace(secondId, second.Column, second.Row, second.QuarterTurns);

            first.Column = firstColumn;
            first.Row = firstRow;
            first.QuarterTurns = firstTurns;
            second.Column = secondColumn;
            second.Row = secondRow;
            return legal;
        }

        public bool TrySwap(string firstId, string secondId)
        {
            return placements.TryGetValue(firstId, out var first) &&
                   TrySwap(firstId, secondId, first.QuarterTurns);
        }

        public bool TrySwap(string firstId, string secondId, int firstQuarterTurns)
        {
            if (!CanSwap(firstId, secondId, firstQuarterTurns))
            {
                return false;
            }

            var first = placements[firstId];
            var second = placements[secondId];
            (first.Column, second.Column) = (second.Column, first.Column);
            (first.Row, second.Row) = (second.Row, first.Row);
            first.QuarterTurns = NormalizeTurns(firstQuarterTurns);
            return true;
        }

        // A two-cell room can exchange with the movable one-cell rooms covering
        // its destination. Moving by one cell displaces one room; moving to a
        // separate two-cell area displaces two. The displaced rooms fill only
        // the cells vacated by the larger room.
        public bool TryPlanSwapWithSingles(
            string largeRoomId,
            int column,
            int row,
            out IReadOnlyList<RoomPlacementData> plannedPlacements)
        {
            plannedPlacements = Array.Empty<RoomPlacementData>();
            return placements.TryGetValue(largeRoomId, out var large) &&
                   TryPlanSwapWithSingles(largeRoomId, column, row, large.QuarterTurns, out plannedPlacements);
        }

        public bool TryPlanSwapWithSingles(
            string largeRoomId,
            int column,
            int row,
            int quarterTurns,
            out IReadOnlyList<RoomPlacementData> plannedPlacements)
        {
            plannedPlacements = Array.Empty<RoomPlacementData>();
            var turns = NormalizeTurns(quarterTurns);
            if (!CanMove(largeRoomId) || !placements.TryGetValue(largeRoomId, out var large) ||
                large.InTray || large.BaseWidth * large.BaseHeight != 2)
            {
                return false;
            }

            var targetWidth = turns % 2 == 0 ? large.BaseWidth : large.BaseHeight;
            var targetHeight = turns % 2 == 0 ? large.BaseHeight : large.BaseWidth;
            if (
                column < 0 || row < 0 ||
                column + targetWidth > RoomLayoutData.GridSize ||
                row + targetHeight > RoomLayoutData.GridSize)
            {
                return false;
            }

            var displaced = new List<RoomPlacement>();
            for (var targetRow = row; targetRow < row + targetHeight; targetRow++)
            {
                for (var targetColumn = column; targetColumn < column + targetWidth; targetColumn++)
                {
                    if (ContainsCell(large, targetColumn, targetRow))
                    {
                        continue;
                    }

                    var occupant = placements.Values.FirstOrDefault(item =>
                        item.Id != largeRoomId && !item.InTray &&
                        ContainsCell(item, targetColumn, targetRow));
                    if (occupant == null || occupant.Width != 1 || occupant.Height != 1 ||
                        !CanMove(occupant.Id) || displaced.Contains(occupant))
                    {
                        return false;
                    }
                    displaced.Add(occupant);
                }
            }

            var vacated = new List<(int column, int row)>();
            for (var sourceRow = large.Row; sourceRow < large.Row + large.Height; sourceRow++)
            {
                for (var sourceColumn = large.Column; sourceColumn < large.Column + large.Width; sourceColumn++)
                {
                    if (sourceColumn < column || sourceColumn >= column + targetWidth ||
                        sourceRow < row || sourceRow >= row + targetHeight)
                    {
                        vacated.Add((sourceColumn, sourceRow));
                    }
                }
            }

            if (displaced.Count == 0 || displaced.Count != vacated.Count)
            {
                return false;
            }

            var plan = new List<RoomPlacementData>
            {
                new()
                {
                    id = largeRoomId,
                    column = column,
                    row = row,
                    quarterTurns = turns
                }
            };
            for (var index = 0; index < displaced.Count; index++)
            {
                plan.Add(new RoomPlacementData
                {
                    id = displaced[index].Id,
                    column = vacated[index].column,
                    row = vacated[index].row,
                    quarterTurns = displaced[index].QuarterTurns
                });
            }

            var originalPositions = plan.Select(item =>
                (placement: placements[item.id], column: placements[item.id].Column,
                    row: placements[item.id].Row, turns: placements[item.id].QuarterTurns)).ToArray();
            try
            {
                foreach (var item in plan)
                {
                    placements[item.id].Column = item.column;
                    placements[item.id].Row = item.row;
                    placements[item.id].QuarterTurns = item.quarterTurns;
                }
                if (plan.Any(item => !CanPlace(item.id, item.column, item.row, item.quarterTurns)))
                {
                    return false;
                }
            }
            finally
            {
                foreach (var original in originalPositions)
                {
                    original.placement.Column = original.column;
                    original.placement.Row = original.row;
                    original.placement.QuarterTurns = original.turns;
                }
            }

            plannedPlacements = plan;
            return true;
        }

        public bool TrySwapWithSingles(string largeRoomId, int column, int row)
        {
            return placements.TryGetValue(largeRoomId, out var large) &&
                   TrySwapWithSingles(largeRoomId, column, row, large.QuarterTurns);
        }

        public bool TrySwapWithSingles(string largeRoomId, int column, int row, int quarterTurns)
        {
            if (!TryPlanSwapWithSingles(largeRoomId, column, row, quarterTurns, out var plan))
            {
                return false;
            }
            foreach (var item in plan)
            {
                placements[item.id].Column = item.column;
                placements[item.id].Row = item.row;
                placements[item.id].QuarterTurns = item.quarterTurns;
            }
            return true;
        }

        public bool TryMoveToTray(string id)
        {
            return placements.TryGetValue(id, out var placement) &&
                   TryMoveToTray(id, placement.QuarterTurns);
        }

        public bool TryMoveToTray(string id, int quarterTurns)
        {
            if (!CanMove(id) || (TrayOccupied && TrayRoomId != id))
            {
                return false;
            }

            placements[id].InTray = true;
            placements[id].QuarterTurns = NormalizeTurns(quarterTurns);
            return true;
        }

        public bool TryRotate(string id)
        {
            if (!CanMove(id) || !placements.TryGetValue(id, out var placement) || !placement.CanRotate)
            {
                return false;
            }

            var nextTurns = NormalizeTurns(placement.QuarterTurns + 1);
            if (!placement.InTray && !CanPlace(id, placement.Column, placement.Row, nextTurns))
            {
                return false;
            }

            placement.QuarterTurns = nextTurns;
            return true;
        }

        public Dictionary<string, RoomPlacement> CaptureSnapshot()
        {
            return placements.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
        }

        public List<RoomPlacementData> ExportData()
        {
            return placements.Values
                .OrderBy(item => item.Id)
                .Select(item => new RoomPlacementData
                {
                    id = item.Id,
                    column = item.Column,
                    row = item.Row,
                    quarterTurns = item.QuarterTurns
                })
                .ToList();
        }

        public bool TryRestore(IEnumerable<RoomPlacementData> data)
        {
            var backup = CaptureSnapshot();
            var items = (data ?? Array.Empty<RoomPlacementData>()).ToList();
            if (items.Count != placements.Count || items.Select(item => item?.id).Distinct().Count() != placements.Count)
            {
                return false;
            }

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || !placements.TryGetValue(item.id, out var target))
                {
                    Restore(backup);
                    return false;
                }

                target.Column = item.column;
                target.Row = item.row;
                target.QuarterTurns = NormalizeTurns(item.quarterTurns);
                target.InTray = false;
            }

            if (!IsCompleteAndLegal())
            {
                Restore(backup);
                return false;
            }

            return true;
        }

        public void Restore(IReadOnlyDictionary<string, RoomPlacement> snapshot)
        {
            foreach (var pair in snapshot)
            {
                if (!placements.TryGetValue(pair.Key, out var target))
                {
                    continue;
                }

                target.Column = pair.Value.Column;
                target.Row = pair.Value.Row;
                target.QuarterTurns = pair.Value.QuarterTurns;
                target.InTray = pair.Value.InTray;
            }
        }

        public bool IsCompleteAndLegal()
        {
            if (TrayOccupied)
            {
                return false;
            }

            var occupied = new bool[RoomLayoutData.GridSize, RoomLayoutData.GridSize];
            foreach (var placement in placements.Values)
            {
                if (!CanPlace(placement.Id, placement.Column, placement.Row, placement.QuarterTurns))
                {
                    return false;
                }

                for (var row = placement.Row; row < placement.Row + placement.Height; row++)
                {
                    for (var column = placement.Column; column < placement.Column + placement.Width; column++)
                    {
                        if (occupied[column, row])
                        {
                            return false;
                        }

                        occupied[column, row] = true;
                    }
                }
            }

            for (var row = 0; row < RoomLayoutData.GridSize; row++)
            {
                for (var column = 0; column < RoomLayoutData.GridSize; column++)
                {
                    if (!occupied[column, row])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int NormalizeTurns(int value)
        {
            return ((value % 4) + 4) % 4;
        }

        private static bool ContainsCell(RoomPlacement placement, int column, int row)
        {
            return column >= placement.Column && column < placement.Column + placement.Width &&
                   row >= placement.Row && row < placement.Row + placement.Height;
        }

        private static bool RectanglesOverlap(
            int firstColumn,
            int firstRow,
            int firstWidth,
            int firstHeight,
            int secondColumn,
            int secondRow,
            int secondWidth,
            int secondHeight)
        {
            return firstColumn < secondColumn + secondWidth &&
                   firstColumn + firstWidth > secondColumn &&
                   firstRow < secondRow + secondHeight &&
                   firstRow + firstHeight > secondRow;
        }
    }
}
