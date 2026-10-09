using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Data
{
    // These are construction choices, not animal-specific rooms. Vegetation is
    // a property of Green; wildlife may later arrive in any suitable green area.
    public enum ConstructionCategory
    {
        Green,
        Residence,
        Workshop,
        Restaurant,
        Supermarket,
        Waste,
        Street,
        Square
    }

    public enum GreenPlanting { None, Oak, Meadow, Shrub }

    public enum ConstructionPlacementFailure
    {
        None,
        BoardFull,
        OutsideBoard,
        Occupied,
        Disconnected,
        NeedsHomeOrConnectedStreet,
        NeedsHomeOrRestaurant,
        StreetNeedsHomeConnection,
        InvalidPlanting,
        InvalidBuildDay
    }

    [Serializable]
    public sealed class ConstructionTileData
    {
        public string id;
        public int buildIndex;
        public int builtDay;
        public int column;
        public int row;
        public ConstructionCategory category;
        public GreenPlanting planting;
    }

    [Serializable]
    public sealed class ConstructionBoardSaveData
    {
        public List<ConstructionTileData> tiles = new();
    }

    // A separate board model keeps the legacy 49-filled layout readable while
    // the simulation is migrated to built-only inputs. An invisible legacy room
    // must never count as a constructed room, path node, food source or score.
    public sealed class ConstructionBoardModel
    {
        public const int Size = 7;
        public const int Capacity = Size * Size;
        public const int StarterColumn = Size / 2;
        public const int StarterRow = Size / 2;
        public const string StarterOakId = "starter-oak";

        private readonly ConstructionTileData[,] cells = new ConstructionTileData[Size, Size];
        private readonly List<ConstructionTileData> tiles = new();

        public ConstructionBoardModel()
        {
            Add(new ConstructionTileData
            {
                id = StarterOakId,
                buildIndex = 1,
                builtDay = 1,
                column = StarterColumn,
                row = StarterRow,
                category = ConstructionCategory.Green,
                planting = GreenPlanting.Oak
            });
        }

        public int BuiltCount => tiles.Count;
        public bool IsFull => BuiltCount == Capacity;
        public IReadOnlyList<ConstructionTileData> BuiltTiles => tiles.Select(Clone).ToArray();
        public ConstructionTileData FirstResidence => CloneOrNull(tiles.FirstOrDefault(
            tile => tile.category == ConstructionCategory.Residence));

        public ConstructionTileData At(int column, int row) => InBounds(column, row)
            ? cells[column, row] == null ? null : Clone(cells[column, row]) : null;

        public ConstructionPlacementFailure CheckPlacement(int column, int row,
            ConstructionCategory category, GreenPlanting planting = GreenPlanting.None)
        {
            if (IsFull) return ConstructionPlacementFailure.BoardFull;
            if (!InBounds(column, row)) return ConstructionPlacementFailure.OutsideBoard;
            if (cells[column, row] != null) return ConstructionPlacementFailure.Occupied;
            if (!Enum.IsDefined(typeof(ConstructionCategory), category) ||
                (category == ConstructionCategory.Green && planting == GreenPlanting.None) ||
                (category != ConstructionCategory.Green && planting != GreenPlanting.None) ||
                !Enum.IsDefined(typeof(GreenPlanting), planting))
                return ConstructionPlacementFailure.InvalidPlanting;
            if (!Neighbours(column, row).Any()) return ConstructionPlacementFailure.Disconnected;

            return category switch
            {
                ConstructionCategory.Street => Neighbours(column, row).Any(tile =>
                    tile.category == ConstructionCategory.Residence ||
                    tile.category == ConstructionCategory.Street && StreetReachesHome(tile))
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.StreetNeedsHomeConnection,
                ConstructionCategory.Workshop or ConstructionCategory.Restaurant or
                    ConstructionCategory.Supermarket => Neighbours(column, row).Any(tile =>
                    tile.category == ConstructionCategory.Residence ||
                    tile.category == ConstructionCategory.Street && StreetReachesHome(tile))
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.NeedsHomeOrConnectedStreet,
                ConstructionCategory.Waste => Neighbours(column, row).Any(tile =>
                    tile.category is ConstructionCategory.Residence or ConstructionCategory.Restaurant)
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.NeedsHomeOrRestaurant,
                _ => ConstructionPlacementFailure.None
            };
        }

        public bool TryBuild(int column, int row, ConstructionCategory category,
            GreenPlanting planting, out ConstructionTileData built,
            out ConstructionPlacementFailure failure, int builtDay = 1)
        {
            built = null;
            failure = CheckPlacement(column, row, category, planting);
            if (failure != ConstructionPlacementFailure.None) return false;
            if (builtDay < 1)
            {
                failure = ConstructionPlacementFailure.InvalidBuildDay;
                return false;
            }
            built = new ConstructionTileData
            {
                id = $"built-{BuiltCount + 1:000}",
                buildIndex = BuiltCount + 1,
                builtDay = builtDay,
                column = column,
                row = row,
                category = category,
                planting = planting
            };
            Add(built);
            built = Clone(built);
            return true;
        }

        public int AdjacentGreenCount(string residenceId)
        {
            var home = tiles.FirstOrDefault(tile => tile.id == residenceId &&
                tile.category == ConstructionCategory.Residence);
            return home == null ? 0 : Neighbours(home.column, home.row)
                .Count(tile => tile.category == ConstructionCategory.Green);
        }

        public bool TouchesResidence(string greenId)
        {
            var green = tiles.FirstOrDefault(tile => tile.id == greenId &&
                tile.category == ConstructionCategory.Green);
            return green != null && Neighbours(green.column, green.row)
                .Any(tile => tile.category == ConstructionCategory.Residence);
        }

        // A resident can leave their own home via a directly adjacent service
        // or streets attached to that home. Another home's service does not
        // silently satisfy this resident's need.
        public bool FirstResidenceCanReachService(ConstructionCategory service) =>
            FirstResidenceServiceRoute(service).Count > 0;

        public IReadOnlyList<ConstructionTileData> FirstResidenceServiceRoute(
            ConstructionCategory service, int throughDay = int.MaxValue)
        {
            var first = tiles.FirstOrDefault(tile =>
                tile.category == ConstructionCategory.Residence);
            return ResidenceServiceRoute(first?.id, service, throughDay);
        }

        public IReadOnlyList<ConstructionTileData> ResidenceServiceRoute(
            string residenceId, ConstructionCategory service,
            int throughDay = int.MaxValue) =>
            FindServiceRoute(residenceId, service, null, throughDay);

        public IReadOnlyList<ConstructionTileData> ResidenceServiceRouteTo(
            string residenceId, string serviceTileId,
            int throughDay = int.MaxValue)
        {
            var target = tiles.FirstOrDefault(tile => tile.id == serviceTileId);
            return target == null ? Array.Empty<ConstructionTileData>() :
                FindServiceRoute(residenceId, target.category, serviceTileId,
                    throughDay);
        }

        private IReadOnlyList<ConstructionTileData> FindServiceRoute(
            string residenceId, ConstructionCategory service, string targetId,
            int throughDay)
        {
            if (service is not (ConstructionCategory.Restaurant or
                ConstructionCategory.Workshop or ConstructionCategory.Supermarket) ||
                throughDay < 1) return Array.Empty<ConstructionTileData>();
            var home = tiles.FirstOrDefault(tile => tile.id == residenceId &&
                tile.category == ConstructionCategory.Residence);
            if (home == null || Math.Max(1, home.builtDay) > throughDay)
                return Array.Empty<ConstructionTileData>();
            var seen = new HashSet<string> { home.id };
            var pending = new Queue<ConstructionTileData>();
            var paths = new Dictionary<string, List<ConstructionTileData>>
            {
                [home.id] = new() { home }
            };
            pending.Enqueue(home);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in Neighbours(current.column, current.row))
                {
                    if (Math.Max(1, neighbour.builtDay) > throughDay) continue;
                    if (neighbour.category == service &&
                        (targetId == null || neighbour.id == targetId))
                        return paths[current.id].Append(neighbour).Select(Clone).ToArray();
                    if (neighbour.category == ConstructionCategory.Street &&
                        seen.Add(neighbour.id))
                    {
                        paths[neighbour.id] = paths[current.id].Append(neighbour).ToList();
                        pending.Enqueue(neighbour);
                    }
                }
            }
            return Array.Empty<ConstructionTileData>();
        }

        public int ConnectedGreenCount(string greenId)
            => ConnectedGreenTiles(greenId).Count;

        public IReadOnlyList<ConstructionTileData> ConnectedGreenTiles(string greenId)
        {
            var start = tiles.FirstOrDefault(tile => tile.id == greenId &&
                tile.category == ConstructionCategory.Green);
            if (start == null) return Array.Empty<ConstructionTileData>();
            var seen = new HashSet<string> { start.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(start);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in Neighbours(current.column, current.row))
                {
                    if (neighbour.category != ConstructionCategory.Green ||
                        !seen.Add(neighbour.id)) continue;
                    pending.Enqueue(neighbour);
                }
            }
            return tiles.Where(tile => seen.Contains(tile.id)).Select(Clone).ToArray();
        }

        public ConstructionBoardSaveData Export() => new()
        {
            tiles = tiles.Select(Clone).ToList()
        };

        public static bool TryRestore(ConstructionBoardSaveData saved,
            out ConstructionBoardModel board)
        {
            board = null;
            var ordered = saved?.tiles?.OrderBy(tile => tile?.buildIndex).ToArray();
            if (ordered == null || ordered.Length is < 1 or > Capacity ||
                ordered[0]?.id != StarterOakId || ordered[0].buildIndex != 1 ||
                ordered[0].column != StarterColumn || ordered[0].row != StarterRow ||
                ordered[0].builtDay < 0 || ordered[0].builtDay > 1 ||
                ordered[0].category != ConstructionCategory.Green ||
                ordered[0].planting != GreenPlanting.Oak)
                return false;

            var candidate = new ConstructionBoardModel();
            var previousDay = 1;
            for (var index = 1; index < ordered.Length; index++)
            {
                var item = ordered[index];
                var day = item?.builtDay == 0 ? 1 : item?.builtDay ?? 0;
                if (item == null || item.buildIndex != index + 1 ||
                    day < previousDay ||
                    !candidate.TryBuild(item.column, item.row, item.category,
                        item.planting, out var built, out _, day) || built.id != item.id)
                    return false;
                previousDay = day;
            }
            board = candidate;
            return true;
        }

        private void Add(ConstructionTileData tile)
        {
            cells[tile.column, tile.row] = tile;
            tiles.Add(tile);
        }

        private bool StreetReachesHome(ConstructionTileData street)
        {
            var seen = new HashSet<string> { street.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(street);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in Neighbours(current.column, current.row))
                {
                    if (neighbour.category == ConstructionCategory.Residence) return true;
                    if (neighbour.category != ConstructionCategory.Street ||
                        !seen.Add(neighbour.id)) continue;
                    pending.Enqueue(neighbour);
                }
            }
            return false;
        }

        private IEnumerable<ConstructionTileData> Neighbours(int column, int row)
        {
            foreach (var (x, y) in new[]
                     {
                         (column - 1, row), (column + 1, row),
                         (column, row - 1), (column, row + 1)
                     })
            {
                if (InBounds(x, y) && cells[x, y] != null) yield return cells[x, y];
            }
        }

        private static bool InBounds(int column, int row) =>
            column >= 0 && row >= 0 && column < Size && row < Size;

        private static ConstructionTileData Clone(ConstructionTileData tile) => new()
        {
            id = tile.id,
            buildIndex = tile.buildIndex,
            builtDay = tile.builtDay,
            column = tile.column,
            row = tile.row,
            category = tile.category,
            planting = tile.planting
        };

        private static ConstructionTileData CloneOrNull(ConstructionTileData tile) =>
            tile == null ? null : Clone(tile);
    }
}
