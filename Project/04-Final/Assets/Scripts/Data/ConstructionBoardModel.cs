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

    // A preference within the Residence category, not a ninth room type.
    // Standard is zero so homes in older JSON saves keep their original rules.
    public enum ResidenceKind { Standard = 0, Garden = 1, Urban = 2 }

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
        InvalidBuildDay,
        TypeLimitReached,
        IntroTypeAlreadyBuilt,
        IntroTypeNotNext,
        InvalidResidenceKind,
        StarterOakMustBeCenter
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
        public ResidenceKind residenceKind;
    }

    [Serializable]
    public sealed class ConstructionBoardSaveData
    {
        public int expansionRings;
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
        // Keep the squared board capacity inside Int32; this is a storage
        // safety bound, not a game-mode room quota.
        private const int MaxSafeExpansionRings = (46340 - Size) / 2;

        private readonly Dictionary<(int column, int row), ConstructionTileData> cells = new();
        private readonly List<ConstructionTileData> tiles = new();
        private readonly bool unbounded;
        public int ExpansionRings { get; private set; }
        public int MinCoordinate => -ExpansionRings;
        public int MaxCoordinate => Size - 1 + ExpansionRings;
        public int CurrentCapacity => unbounded ? int.MaxValue :
            (Size + 2 * ExpansionRings) * (Size + 2 * ExpansionRings);

        public ConstructionBoardModel(bool unbounded = false,
            bool includeStarterOak = true)
        {
            this.unbounded = unbounded;
            if (!includeStarterOak) return;
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
        public bool IsFull => !unbounded && BuiltCount == CurrentCapacity;
        public bool Contains(int column, int row) => InBounds(column, row);
        public bool ExpandWhenFull()
        {
            if (unbounded || !IsFull) return false;
            return ExpandOneRing();
        }
        public bool HasBuiltBoundary => tiles.Any(tile =>
            tile.column == MinCoordinate || tile.column == MaxCoordinate ||
            tile.row == MinCoordinate || tile.row == MaxCoordinate);
        public bool ExpandWhenFrontierReached()
        {
            if (unbounded || !HasBuiltBoundary) return false;
            return ExpandOneRing();
        }
        private bool ExpandOneRing()
        {
            if (ExpansionRings >= MaxSafeExpansionRings) return false;
            ExpansionRings++;
            return true;
        }
        public IReadOnlyList<ConstructionTileData> BuiltTiles => tiles.Select(Clone).ToArray();
        public ConstructionTileData FirstResidence => CloneOrNull(tiles.FirstOrDefault(
            tile => tile.category == ConstructionCategory.Residence));

        public ConstructionTileData At(int column, int row) =>
            InBounds(column, row) && cells.TryGetValue((column, row), out var tile)
                ? Clone(tile) : null;

        // Neighbourhood effects use eight cells; routes and construction
        // require a shared edge. Long arithmetic supports endless coordinates.
        public static long GridDistance(int firstColumn, int firstRow,
            int secondColumn, int secondRow) =>
            Math.Max(Math.Abs((long)firstColumn - secondColumn),
                Math.Abs((long)firstRow - secondRow));

        public static bool AreNeighbours(int firstColumn, int firstRow,
            int secondColumn, int secondRow) =>
            GridDistance(firstColumn, firstRow, secondColumn, secondRow) == 1;

        public static bool AreCardinalNeighbours(int firstColumn, int firstRow,
            int secondColumn, int secondRow) =>
            Math.Abs((long)firstColumn - secondColumn) +
            Math.Abs((long)firstRow - secondRow) == 1;

        public bool TouchesBuiltTile(int column, int row) =>
            Neighbours(column, row).Any();

        // The introductory square should be usable on the day it is built.
        // Free construction may still reserve one for a future street route.
        public bool WouldConnectSquareToHome(int column, int row) =>
            Neighbours(column, row).Any(tile =>
                tile.category == ConstructionCategory.Residence ||
                tile.category == ConstructionCategory.Street &&
                StreetReachesHome(tile));

        public ConstructionPlacementFailure CheckPlacement(int column, int row,
            ConstructionCategory category, GreenPlanting planting = GreenPlanting.None,
            bool allowIntroPlacement = false,
            bool allowLegacyDisconnectedReplay = false,
            ResidenceKind residenceKind = ResidenceKind.Standard,
            bool allowLegacyEightNeighbourReplay = false)
        {
            if (IsFull) return ConstructionPlacementFailure.BoardFull;
            if (!InBounds(column, row)) return ConstructionPlacementFailure.OutsideBoard;
            if (cells.ContainsKey((column, row))) return ConstructionPlacementFailure.Occupied;
            if (!Enum.IsDefined(typeof(ConstructionCategory), category) ||
                (category == ConstructionCategory.Green && planting == GreenPlanting.None) ||
                (category != ConstructionCategory.Green && planting != GreenPlanting.None) ||
                !Enum.IsDefined(typeof(GreenPlanting), planting))
                return ConstructionPlacementFailure.InvalidPlanting;
            if (!Enum.IsDefined(typeof(ResidenceKind), residenceKind) ||
                category != ConstructionCategory.Residence &&
                residenceKind != ResidenceKind.Standard)
                return ConstructionPlacementFailure.InvalidResidenceKind;
            // Earlier first-of-type saves can be replayed through this flag.
            // New builds never use it; the run model checks all live rules.
            if (allowIntroPlacement) return ConstructionPlacementFailure.None;
            var neighbours = (allowLegacyEightNeighbourReplay
                    ? SurroundingTiles(column, row) : Neighbours(column, row))
                .ToArray();
            // Older fill-mode saves may contain isolated rooms. Only the
            // restore path can bypass this geometric rule for those rooms.
            if (!allowLegacyDisconnectedReplay && neighbours.Length == 0)
                return ConstructionPlacementFailure.Disconnected;
            // Service buildings, waste rooms and streets also need their
            // functional neighbours below.
            return category switch
            {
                ConstructionCategory.Street => neighbours.Any(tile =>
                    tile.category == ConstructionCategory.Residence ||
                    tile.category == ConstructionCategory.Street &&
                    StreetReachesHome(tile, allowLegacyEightNeighbourReplay))
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.StreetNeedsHomeConnection,
                ConstructionCategory.Workshop or ConstructionCategory.Restaurant or
                    ConstructionCategory.Supermarket => neighbours.Any(tile =>
                    tile.category == ConstructionCategory.Residence ||
                    tile.category == ConstructionCategory.Street &&
                    StreetReachesHome(tile, allowLegacyEightNeighbourReplay))
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.NeedsHomeOrConnectedStreet,
                ConstructionCategory.Waste => neighbours.Any(tile =>
                    (tile.category is ConstructionCategory.Residence or
                        ConstructionCategory.Restaurant) ||
                    (tile.category == ConstructionCategory.Street &&
                        StreetReachesHome(tile, allowLegacyEightNeighbourReplay)))
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.NeedsHomeOrRestaurant,
                _ => ConstructionPlacementFailure.None
            };
        }

        public bool TryBuild(int column, int row, ConstructionCategory category,
            GreenPlanting planting, out ConstructionTileData built,
            out ConstructionPlacementFailure failure, int builtDay = 1,
            bool allowIntroPlacement = false,
            bool allowLegacyDisconnectedReplay = false,
            ResidenceKind residenceKind = ResidenceKind.Standard,
            bool allowLegacyEightNeighbourReplay = false)
        {
            built = null;
            failure = CheckPlacement(column, row, category, planting,
                allowIntroPlacement, allowLegacyDisconnectedReplay,
                residenceKind, allowLegacyEightNeighbourReplay);
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
                planting = planting,
                residenceKind = residenceKind
            };
            Add(built);
            built = Clone(built);
            return true;
        }

        // The digital Fill opening deals the oak as its first card. Other
        // modes and historical saves continue to receive the preplaced oak.
        public bool TryPlaceStarterOak(int column, int row,
            out ConstructionTileData built,
            out ConstructionPlacementFailure failure, int builtDay = 1)
        {
            built = null;
            failure = ConstructionPlacementFailure.None;
            if (tiles.Count != 0)
            {
                failure = ConstructionPlacementFailure.Occupied;
                return false;
            }
            if (!InBounds(column, row))
            {
                failure = ConstructionPlacementFailure.OutsideBoard;
                return false;
            }
            if (column != StarterColumn || row != StarterRow)
            {
                failure = ConstructionPlacementFailure.StarterOakMustBeCenter;
                return false;
            }
            if (builtDay < 1)
            {
                failure = ConstructionPlacementFailure.InvalidBuildDay;
                return false;
            }
            built = new ConstructionTileData
            {
                id = StarterOakId,
                buildIndex = 1,
                builtDay = builtDay,
                column = StarterColumn,
                row = StarterRow,
                category = ConstructionCategory.Green,
                planting = GreenPlanting.Oak
            };
            Add(built);
            built = Clone(built);
            return true;
        }

        public int AdjacentGreenCount(string residenceId)
        {
            var home = tiles.FirstOrDefault(tile => tile.id == residenceId &&
                tile.category == ConstructionCategory.Residence);
            return home == null ? 0 : SurroundingTiles(home.column, home.row)
                .Count(tile => tile.category == ConstructionCategory.Green);
        }

        public bool TouchesResidence(string greenId)
        {
            var green = tiles.FirstOrDefault(tile => tile.id == greenId &&
                tile.category == ConstructionCategory.Green);
            return green != null && SurroundingTiles(green.column, green.row)
                .Any(tile => tile.category == ConstructionCategory.Residence);
        }

        // A resident can leave their own home via a directly adjacent service
        // or streets attached to that home. Another home's service does not
        // silently satisfy this resident's need.
        public bool FirstResidenceCanReachService(ConstructionCategory service,
            bool useLegacyEightNeighbours = false) =>
            FirstResidenceServiceRoute(service, int.MaxValue,
                useLegacyEightNeighbours).Count > 0;

        public IReadOnlyList<ConstructionTileData> FirstResidenceServiceRoute(
            ConstructionCategory service, int throughDay = int.MaxValue,
            bool useLegacyEightNeighbours = false)
        {
            var first = tiles.FirstOrDefault(tile =>
                tile.category == ConstructionCategory.Residence);
            return ResidenceServiceRoute(first?.id, service, throughDay,
                useLegacyEightNeighbours);
        }

        public IReadOnlyList<ConstructionTileData> ResidenceServiceRoute(
            string residenceId, ConstructionCategory service,
            int throughDay = int.MaxValue,
            bool useLegacyEightNeighbours = false) =>
            FindServiceRoute(residenceId, service, null, throughDay,
                useLegacyEightNeighbours);

        public IReadOnlyList<ConstructionTileData> ResidenceServiceRouteTo(
            string residenceId, string serviceTileId,
            int throughDay = int.MaxValue,
            bool useLegacyEightNeighbours = false)
        {
            var target = tiles.FirstOrDefault(tile => tile.id == serviceTileId);
            return target == null ? Array.Empty<ConstructionTileData>() :
                FindServiceRoute(residenceId, target.category, serviceTileId,
                    throughDay, useLegacyEightNeighbours);
        }

        private IReadOnlyList<ConstructionTileData> FindServiceRoute(
            string residenceId, ConstructionCategory service, string targetId,
            int throughDay, bool useLegacyEightNeighbours)
        {
            if (service is not (ConstructionCategory.Restaurant or
                ConstructionCategory.Workshop or ConstructionCategory.Supermarket or
                ConstructionCategory.Square) ||
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
                foreach (var neighbour in useLegacyEightNeighbours
                             ? SurroundingTiles(current.column, current.row)
                             : Neighbours(current.column, current.row))
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

        internal int GreenUpgradeLevel(ConstructionTileData green)
        {
            var adjacent = Neighbours(green.column, green.row)
                .Where(tile => tile.category == ConstructionCategory.Green)
                .Take(2).ToArray();
            if (adjacent.Length != 1) return adjacent.Length + 1;
            var secondEdge = Neighbours(adjacent[0].column, adjacent[0].row)
                .Any(tile => tile.category == ConstructionCategory.Green &&
                    tile.id != green.id);
            return secondEdge ? 3 : 2;
        }

        public IReadOnlyList<ConstructionTileData> ConnectedGreenTiles(
            string greenId, bool useLegacyEightNeighbours = false)
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
                foreach (var neighbour in useLegacyEightNeighbours
                             ? SurroundingTiles(current.column, current.row)
                             : Neighbours(current.column, current.row))
                {
                    if (neighbour.category != ConstructionCategory.Green ||
                        !seen.Add(neighbour.id)) continue;
                    pending.Enqueue(neighbour);
                }
            }
            return tiles.Where(tile => seen.Contains(tile.id)).Select(Clone).ToArray();
        }

        public static bool AnimalCanTraverse(ConstructionCategory category) =>
            category is ConstructionCategory.Green or ConstructionCategory.Square or
                ConstructionCategory.Street or ConstructionCategory.Supermarket or
                ConstructionCategory.Waste;

        // A green habitat cluster is still green-only. Foraging can use any
        // built animal-passable tile, without treating streets or plazas as
        // habitat or allowing a path through a human-only room.
        public IReadOnlyList<ConstructionTileData> AnimalReachableGreenTiles(
            string originId, int throughDay = int.MaxValue,
            bool useLegacyEightNeighbours = false)
        {
            var origin = tiles.FirstOrDefault(tile => tile.id == originId &&
                AnimalCanTraverse(tile.category) &&
                Math.Max(1, tile.builtDay) <= throughDay);
            if (origin == null) return Array.Empty<ConstructionTileData>();
            var seen = new HashSet<string> { origin.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in useLegacyEightNeighbours
                             ? SurroundingTiles(current.column, current.row)
                             : Neighbours(current.column, current.row))
                    if (AnimalCanTraverse(neighbour.category) &&
                        Math.Max(1, neighbour.builtDay) <= throughDay &&
                        seen.Add(neighbour.id))
                        pending.Enqueue(neighbour);
            }
            return tiles.Where(tile => seen.Contains(tile.id) &&
                    tile.category == ConstructionCategory.Green)
                .Select(Clone).ToArray();
        }

        // A shortest built-only cardinal animal route. Neighbours has a stable
        // order, so equal-length routes stay deterministic across saves.
        public IReadOnlyList<ConstructionTileData> AnimalRouteBetween(
            string originId, string destinationId,
            int throughDay = int.MaxValue)
        {
            var origin = tiles.FirstOrDefault(tile => tile.id == originId &&
                AnimalCanTraverse(tile.category) &&
                Math.Max(1, tile.builtDay) <= throughDay);
            var destination = tiles.FirstOrDefault(tile =>
                tile.id == destinationId && AnimalCanTraverse(tile.category) &&
                Math.Max(1, tile.builtDay) <= throughDay);
            if (origin == null || destination == null)
                return Array.Empty<ConstructionTileData>();
            var previous = new Dictionary<string, string> { [origin.id] = null };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(origin);
            while (pending.Count > 0 && !previous.ContainsKey(destination.id))
            {
                var current = pending.Dequeue();
                foreach (var neighbour in Neighbours(current.column, current.row))
                {
                    if (!AnimalCanTraverse(neighbour.category) ||
                        Math.Max(1, neighbour.builtDay) > throughDay ||
                        previous.ContainsKey(neighbour.id)) continue;
                    previous[neighbour.id] = current.id;
                    pending.Enqueue(neighbour);
                }
            }
            if (!previous.ContainsKey(destination.id))
                return Array.Empty<ConstructionTileData>();
            var byId = tiles.ToDictionary(tile => tile.id);
            var route = new List<ConstructionTileData>();
            for (var id = destination.id; id != null; id = previous[id])
                route.Add(Clone(byId[id]));
            route.Reverse();
            return route;
        }

        // Historical callers ask for a green-to-green route. The endpoints
        // remain green while the route may now cross passable urban tiles.
        public IReadOnlyList<ConstructionTileData> GreenRouteBetween(
            string originId, string destinationId)
        {
            var origin = tiles.FirstOrDefault(tile => tile.id == originId &&
                tile.category == ConstructionCategory.Green);
            var destination = tiles.FirstOrDefault(tile =>
                tile.id == destinationId &&
                tile.category == ConstructionCategory.Green);
            if (origin == null || destination == null)
                return Array.Empty<ConstructionTileData>();
            return AnimalRouteBetween(originId, destinationId);
        }

        public ConstructionBoardSaveData Export() => new()
        {
            expansionRings = ExpansionRings,
            tiles = tiles.Select(Clone).ToList()
        };

        public static bool TryRestore(ConstructionBoardSaveData saved,
            out ConstructionBoardModel board, bool unbounded = false,
            bool allowEmptyStarter = false)
        {
            board = null;
            var ordered = saved?.tiles?.OrderBy(tile => tile?.buildIndex).ToArray();
            if (allowEmptyStarter && !unbounded && saved?.expansionRings == 0 &&
                ordered?.Length == 0)
            {
                board = new ConstructionBoardModel(includeStarterOak: false);
                return true;
            }
            if (saved == null || saved.expansionRings < 0 ||
                saved.expansionRings > MaxSafeExpansionRings || ordered == null ||
                ordered.Length < 1 ||
                (!unbounded && ordered.Length >
                    (Size + 2 * saved.expansionRings) *
                    (Size + 2 * saved.expansionRings)) ||
                ordered[0]?.id != StarterOakId || ordered[0].buildIndex != 1 ||
                ordered[0].column != StarterColumn || ordered[0].row != StarterRow ||
                ordered[0].builtDay < 0 || ordered[0].builtDay > 1 ||
                ordered[0].category != ConstructionCategory.Green ||
                ordered[0].planting != GreenPlanting.Oak ||
                ordered[0].residenceKind != ResidenceKind.Standard)
                return false;

            var candidate = new ConstructionBoardModel(unbounded);
            candidate.ExpansionRings = saved.expansionRings;
            var previousDay = 1;
            for (var index = 1; index < ordered.Length; index++)
            {
                var item = ordered[index];
                var day = item?.builtDay == 0 ? 1 : item?.builtDay ?? 0;
                // Replay earlier first-of-type placements, including Green
                // plots built before the oak-adjacency tutorial rule. Earlier
                // fill-mode free builds could also be isolated, so preserve
                // them without allowing new disconnected construction.
                var firstOfType = item != null && !unbounded && day > 1 &&
                    !candidate.tiles.Any(tile => tile.category == item.category &&
                        tile.id != StarterOakId);
                var legacyDisconnected = item != null && !unbounded &&
                    !candidate.SurroundingTiles(item.column, item.row).Any();
                if (item == null || item.buildIndex != index + 1 ||
                    day < previousDay ||
                    !candidate.TryBuild(item.column, item.row, item.category,
                        item.planting, out var built, out _, day,
                        firstOfType, legacyDisconnected, item.residenceKind,
                        allowLegacyEightNeighbourReplay: true) ||
                    built.id != item.id)
                    return false;
                previousDay = day;
            }
            board = candidate;
            return true;
        }

        private void Add(ConstructionTileData tile)
        {
            cells[(tile.column, tile.row)] = tile;
            tiles.Add(tile);
        }

        private bool StreetReachesHome(ConstructionTileData street,
            bool useLegacyEightNeighbours = false)
        {
            var seen = new HashSet<string> { street.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(street);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in useLegacyEightNeighbours
                             ? SurroundingTiles(current.column, current.row)
                             : Neighbours(current.column, current.row))
                {
                    if (neighbour.category == ConstructionCategory.Residence) return true;
                    if (neighbour.category != ConstructionCategory.Street ||
                        !seen.Add(neighbour.id)) continue;
                    pending.Enqueue(neighbour);
                }
            }
            return false;
        }

        private IEnumerable<ConstructionTileData> Neighbours(int column, int row) =>
            SurroundingTiles(column, row).Where(tile => AreCardinalNeighbours(
                column, row, tile.column, tile.row));

        private IEnumerable<ConstructionTileData> SurroundingTiles(int column,
            int row)
        {
            foreach (var (x, y) in new[]
                     {
                         ((long)column - 1, (long)row),
                         ((long)column + 1, (long)row),
                         ((long)column, (long)row - 1),
                         ((long)column, (long)row + 1),
                         ((long)column - 1, (long)row - 1),
                         ((long)column - 1, (long)row + 1),
                         ((long)column + 1, (long)row - 1),
                         ((long)column + 1, (long)row + 1)
                     })
            {
                if (x >= int.MinValue && x <= int.MaxValue &&
                    y >= int.MinValue && y <= int.MaxValue &&
                    InBounds((int)x, (int)y) &&
                    cells.TryGetValue(((int)x, (int)y), out var tile))
                    yield return tile;
            }
        }

        private bool InBounds(int column, int row) =>
            unbounded || column >= MinCoordinate && row >= MinCoordinate &&
            column <= MaxCoordinate && row <= MaxCoordinate;

        private static ConstructionTileData Clone(ConstructionTileData tile) => new()
        {
            id = tile.id,
            buildIndex = tile.buildIndex,
            builtDay = tile.builtDay,
            column = tile.column,
            row = tile.row,
            category = tile.category,
            planting = tile.planting,
            residenceKind = tile.residenceKind
        };

        private static ConstructionTileData CloneOrNull(ConstructionTileData tile) =>
            tile == null ? null : Clone(tile);
    }
}
