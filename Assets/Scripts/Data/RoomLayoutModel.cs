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
        }

        public string Id { get; }
        public int BaseWidth { get; }
        public int BaseHeight { get; }
        public int Column { get; internal set; }
        public int Row { get; internal set; }
        public int QuarterTurns { get; internal set; }
        public bool InTray { get; internal set; }
        public int Width => QuarterTurns % 2 == 0 ? BaseWidth : BaseHeight;
        public int Height => QuarterTurns % 2 == 0 ? BaseHeight : BaseWidth;
        public bool CanRotate => BaseWidth != BaseHeight;

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
            if (firstId == secondId || !CanMove(firstId) || !CanMove(secondId) ||
                !placements.TryGetValue(firstId, out var first) ||
                !placements.TryGetValue(secondId, out var second) ||
                first.InTray || second.InTray ||
                first.Width != second.Width || first.Height != second.Height)
            {
                return false;
            }

            var firstColumn = first.Column;
            var firstRow = first.Row;
            var secondColumn = second.Column;
            var secondRow = second.Row;
            first.Column = secondColumn;
            first.Row = secondRow;
            second.Column = firstColumn;
            second.Row = firstRow;

            var legal = CanPlace(firstId, first.Column, first.Row, first.QuarterTurns) &&
                        CanPlace(secondId, second.Column, second.Row, second.QuarterTurns);

            first.Column = firstColumn;
            first.Row = firstRow;
            second.Column = secondColumn;
            second.Row = secondRow;
            return legal;
        }

        public bool TrySwap(string firstId, string secondId)
        {
            if (!CanSwap(firstId, secondId))
            {
                return false;
            }

            var first = placements[firstId];
            var second = placements[secondId];
            (first.Column, second.Column) = (second.Column, first.Column);
            (first.Row, second.Row) = (second.Row, first.Row);
            return true;
        }

        public bool TryMoveToTray(string id)
        {
            if (!CanMove(id) || (TrayOccupied && TrayRoomId != id))
            {
                return false;
            }

            placements[id].InTray = true;
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
