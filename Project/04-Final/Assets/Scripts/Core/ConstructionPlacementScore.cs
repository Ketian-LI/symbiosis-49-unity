using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionPlacementScoreEntry
    {
        public string tileId;
        public int points;
    }

    [Serializable]
    public sealed class ConstructionPlacementScoreSaveData
    {
        public int version;
        public List<ConstructionPlacementScoreEntry> entries = new();
        public List<ConstructionCommunityAwardSaveData> communityAwards = new();
    }

    [Serializable]
    public sealed class ConstructionCommunityAwardSaveData
    {
        public string homeTileId;
        public string placementTileId;
        public int points;
    }

    public enum ConstructionSlowStreetAxis { None, NorthSouth, EastWest }

    // A one-time score when a tile lands. Daily resident and wildlife scores
    // are settled separately; adjacency never invents a completed activity.
    public static class ConstructionPlacementScore
    {
        public const int OriginalRulesVersion = 1;
        public const int GreenUpgradeRulesVersion = 2;
        public const int RulesVersion = 3;
        public const int BalancedRulesVersion = 4;
        public const int NeighborUpgradeRulesVersion = 5;
        public const int LayoutQualityRulesVersion = 6;
        public const int CompleteNeighborhoodPoints = 24;
        public const int BalancedCommunityPoints = 120;
        public const int LayoutCommunityPoints = 240;
        public static int CommunityPoints(int rulesVersion) =>
            rulesVersion >= LayoutQualityRulesVersion ? LayoutCommunityPoints : BalancedCommunityPoints;
        public const int TwoGreenMilestonePoints = 20;
        public const int DiverseGreenMilestonePoints = 40;
        public const int GardenResidencePoints = 4;

        public static int PairPoints(ConstructionCategory first,
            ConstructionCategory second,
            int rulesVersion = OriginalRulesVersion)
        {
            if (rulesVersion >= LayoutQualityRulesVersion)
            {
                if (Pair(first, second, ConstructionCategory.Street,
                        ConstructionCategory.Green)) return -40;
                if (Pair(first, second, ConstructionCategory.Waste,
                        ConstructionCategory.Residence)) return -80;
                return 2 * PairPoints(first, second, NeighborUpgradeRulesVersion);
            }
            if (rulesVersion >= BalancedRulesVersion)
            {
                if (first == ConstructionCategory.Green &&
                    second == ConstructionCategory.Green) return 6;
                if (Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Workshop) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Restaurant)) return 12;
                if (Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Green) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Supermarket) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Square)) return 8;
                if (Pair(first, second, ConstructionCategory.Square,
                        ConstructionCategory.Green) ||
                    Pair(first, second, ConstructionCategory.Supermarket,
                        ConstructionCategory.Green)) return 6;
                if (Pair(first, second, ConstructionCategory.Street,
                        ConstructionCategory.Green)) return -8;
                if (Pair(first, second, ConstructionCategory.Waste,
                        ConstructionCategory.Residence)) return -18;
                return 0;
            }
            if (rulesVersion >= RulesVersion)
            {
                if (first == ConstructionCategory.Green &&
                    second == ConstructionCategory.Green) return 2;
                if (Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Workshop) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Restaurant)) return 3;
                if (Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Green) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Supermarket) ||
                    Pair(first, second, ConstructionCategory.Residence,
                        ConstructionCategory.Square) ||
                    Pair(first, second, ConstructionCategory.Square,
                        ConstructionCategory.Green) ||
                    Pair(first, second, ConstructionCategory.Supermarket,
                        ConstructionCategory.Green)) return 2;
                if (Pair(first, second, ConstructionCategory.Street,
                        ConstructionCategory.Green) ||
                    Pair(first, second, ConstructionCategory.Waste,
                        ConstructionCategory.Residence)) return -3;
                return 0;
            }
            if (first == ConstructionCategory.Green &&
                second == ConstructionCategory.Green) return 1;
            if (Pair(first, second, ConstructionCategory.Residence,
                    ConstructionCategory.Green) ||
                Pair(first, second, ConstructionCategory.Residence,
                    ConstructionCategory.Workshop) ||
                Pair(first, second, ConstructionCategory.Residence,
                    ConstructionCategory.Restaurant) ||
                Pair(first, second, ConstructionCategory.Residence,
                    ConstructionCategory.Supermarket) ||
                Pair(first, second, ConstructionCategory.Square,
                    ConstructionCategory.Green) ||
                Pair(first, second, ConstructionCategory.Supermarket,
                    ConstructionCategory.Green)) return 1;
            if (Pair(first, second, ConstructionCategory.Street,
                    ConstructionCategory.Green) ||
                Pair(first, second, ConstructionCategory.Waste,
                    ConstructionCategory.Residence)) return -1;
            return 0;
        }

        public static int PointsFor(ConstructionTileData placed,
            IEnumerable<ConstructionTileData> earlierTiles,
            int rulesVersion = OriginalRulesVersion)
        {
            if (placed == null || earlierTiles == null)
                throw new ArgumentNullException(placed == null
                    ? nameof(placed) : nameof(earlierTiles));
            var previous = earlierTiles.Where(tile => tile != null &&
                tile.buildIndex < placed.buildIndex).ToArray();
            var adjacentPoints = previous.Where(tile =>
                    tile.buildIndex < placed.buildIndex &&
                    (rulesVersion >= BalancedRulesVersion
                        ? ConstructionBoardModel.AreCardinalNeighbours(
                            tile.column, tile.row, placed.column, placed.row)
                        : Math.Abs(tile.column - placed.column) <= 1 &&
                          Math.Abs(tile.row - placed.row) <= 1 &&
                          (tile.column != placed.column ||
                           tile.row != placed.row)))
                .Sum(tile => PairPoints(placed.category, tile.category,
                    rulesVersion));
            var greenUpgradePoints =
                rulesVersion >= BalancedRulesVersion
                    ? GreenMilestonePoints(placed, previous)
                    : rulesVersion >= GreenUpgradeRulesVersion &&
                      placed.category == ConstructionCategory.Green
                        ? GreenUpgradeLevel(placed, previous) - 1 : 0;
            if (rulesVersion >= NeighborUpgradeRulesVersion)
                return adjacentPoints + greenUpgradePoints +
                    GardenResidencePoints *
                    AwardedGardenResidences(placed, previous).Count;
            if (rulesVersion < RulesVersion)
                return adjacentPoints + greenUpgradePoints;
            return rulesVersion >= BalancedRulesVersion
                ? adjacentPoints + greenUpgradePoints
                : adjacentPoints + greenUpgradePoints +
                CompleteNeighborhoodPoints *
                NewlyCompletedNeighborhoods(placed, previous);
        }

        public static int GreenMilestonePoints(ConstructionTileData placed,
            IReadOnlyCollection<ConstructionTileData> earlier)
        {
            if (placed.category != ConstructionCategory.Green) return 0;
            var previousGreens = earlier.Where(tile =>
                tile.category == ConstructionCategory.Green).ToArray();
            var after = GreenComponent(placed,
                previousGreens.Append(placed).ToArray());
            var previousComponents = new List<ConstructionTileData[]>();
            var visited = new HashSet<string>();
            foreach (var neighbour in previousGreens.Where(tile =>
                         ConstructionBoardModel.AreCardinalNeighbours(
                             tile.column, tile.row, placed.column, placed.row)))
            {
                if (visited.Contains(neighbour.id)) continue;
                var component = GreenComponent(neighbour, previousGreens);
                previousComponents.Add(component);
                foreach (var tile in component) visited.Add(tile.id);
            }
            var points = 0;
            if (after.Length >= 2 &&
                previousComponents.All(component => component.Length < 2))
                points += TwoGreenMilestonePoints;
            if (DiverseThree(after) &&
                previousComponents.All(component => !DiverseThree(component)))
                points += DiverseGreenMilestonePoints;
            return points;
        }

        private static bool DiverseThree(
            IReadOnlyCollection<ConstructionTileData> component) =>
            component.Count >= 3 &&
            component.Select(tile => tile.planting).Distinct().Count() >= 2;

        private static ConstructionTileData[] GreenComponent(
            ConstructionTileData origin,
            IReadOnlyCollection<ConstructionTileData> greens)
        {
            var visited = new HashSet<string> { origin.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in greens)
                    if (ConstructionBoardModel.AreCardinalNeighbours(
                            current.column, current.row, neighbour.column,
                            neighbour.row) && visited.Add(neighbour.id))
                        pending.Enqueue(neighbour);
            }
            return greens.Where(tile => visited.Contains(tile.id)).ToArray();
        }

        public static int NewlyCompletedNeighborhoods(
            ConstructionTileData placed,
            IEnumerable<ConstructionTileData> earlierTiles)
        {
            if (placed == null || earlierTiles == null) return 0;
            var previous = earlierTiles.Where(tile => tile != null &&
                tile.buildIndex < placed.buildIndex).ToArray();
            var after = previous.Append(placed).ToArray();
            return after.Count(home =>
                home.category == ConstructionCategory.Residence &&
                HasCompleteNeighborhood(home, after) &&
                !HasCompleteNeighborhood(home, previous));
        }

        // This one-time build award belongs to a home with a genuinely mixed
        // neighbourhood. Roads and waste are not required to earn it.
        public static bool HasCompleteNeighborhood(ConstructionTileData home,
            IEnumerable<ConstructionTileData> builtTiles)
        {
            if (home == null || home.category !=
                ConstructionCategory.Residence) return false;
            var tiles = builtTiles.Where(tile => tile != null).ToArray();
            if (!tiles.Any(tile => tile.id == home.id)) return false;
            var near = tiles.Where(tile =>
                tile.id != home.id &&
                Math.Abs(tile.column - home.column) <= 1 &&
                Math.Abs(tile.row - home.row) <= 1).ToArray();
            return near.Any(tile => tile.category ==
                       ConstructionCategory.Workshop) &&
                   near.Any(tile => tile.category ==
                       ConstructionCategory.Restaurant) &&
                   near.Any(tile => tile.category ==
                       ConstructionCategory.Supermarket) &&
                   near.Any(tile => tile.category ==
                       ConstructionCategory.Square) &&
                   near.Where(tile => tile.category ==
                           ConstructionCategory.Green)
                       .Select(tile => tile.planting).Distinct().Count() >= 2;
        }

        // Green upgrades use shared edges in every rules version that supports
        // them, independently of the ordinary placement scoring radius.
        public static int GreenUpgradeLevel(ConstructionTileData green,
            IEnumerable<ConstructionTileData> builtTiles)
        {
            if (green == null || green.category != ConstructionCategory.Green)
                return 0;
            var greens = builtTiles.Where(tile => tile != null &&
                    tile.category == ConstructionCategory.Green &&
                    tile.id != green.id)
                .Append(green).ToArray();
            var seen = new HashSet<string> { green.id };
            var pending = new Queue<ConstructionTileData>();
            pending.Enqueue(green);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in greens)
                {
                    if (ConstructionBoardModel.AreCardinalNeighbours(
                            current.column, current.row, neighbour.column,
                            neighbour.row) && seen.Add(neighbour.id))
                    {
                        if (seen.Count == 3) return 3;
                        pending.Enqueue(neighbour);
                    }
                }
            }
            return seen.Count;
        }

        public static bool IsGardenResidence(ConstructionTileData tile,
            IEnumerable<ConstructionTileData> builtTiles)
        {
            if (tile?.category != ConstructionCategory.Residence) return false;
            var greens = builtTiles.Where(item =>
                item?.category == ConstructionCategory.Green).ToArray();
            // Level 2 or higher means the bordering green touches at least
            // one other green. No full component search is needed here.
            return greens.Any(green => SharesEdge(tile, green) &&
                greens.Any(other => SharesEdge(green, other)));
        }

        public static bool IsTerraceRestaurant(ConstructionTileData tile,
            IEnumerable<ConstructionTileData> builtTiles) =>
            tile?.category == ConstructionCategory.Restaurant &&
            builtTiles.Any(item => item?.category == ConstructionCategory.Square &&
                SharesEdge(tile, item));

        public static bool IsSlowStreet(ConstructionTileData tile,
            IEnumerable<ConstructionTileData> builtTiles) =>
            SlowStreetAxis(tile, builtTiles) != ConstructionSlowStreetAxis.None;

        public static ConstructionSlowStreetAxis SlowStreetAxis(
            ConstructionTileData tile, IEnumerable<ConstructionTileData> builtTiles)
        {
            if (tile?.category != ConstructionCategory.Street)
                return ConstructionSlowStreetAxis.None;

            var north = false;
            var south = false;
            var east = false;
            var west = false;
            foreach (var green in builtTiles)
            {
                if (green?.category != ConstructionCategory.Green) continue;
                if (green.column == tile.column)
                {
                    north |= green.row == tile.row - 1;
                    south |= green.row == tile.row + 1;
                }
                else if (green.row == tile.row)
                {
                    east |= green.column == tile.column + 1;
                    west |= green.column == tile.column - 1;
                }
            }
            if (north && south) return ConstructionSlowStreetAxis.NorthSouth;
            if (east && west) return ConstructionSlowStreetAxis.EastWest;
            return ConstructionSlowStreetAxis.None;
        }

        public static int UpgradeLevel(ConstructionTileData tile,
            IEnumerable<ConstructionTileData> builtTiles) => tile?.category switch
        {
            null => 0,
            ConstructionCategory.Green => GreenUpgradeLevel(tile, builtTiles),
            ConstructionCategory.Residence => IsGardenResidence(tile, builtTiles) ? 2 : 1,
            ConstructionCategory.Restaurant => IsTerraceRestaurant(tile, builtTiles) ? 2 : 1,
            ConstructionCategory.Street => IsSlowStreet(tile, builtTiles) ? 2 : 1,
            _ => 1
        };

        public static IReadOnlyList<ConstructionTileData> NewlyUpgradedTiles(
            ConstructionTileData placed, IEnumerable<ConstructionTileData> existingTiles)
        {
            if (placed == null) return Array.Empty<ConstructionTileData>();

            var before = ExcludePlacement(placed, existingTiles);
            var after = before.Append(placed).ToArray();
            // An upgrade can only change near the new room. For green plots
            // and garden homes a second shared edge can complete the pattern.
            return after.Where(tile =>
                Math.Abs((long)tile.column - placed.column) +
                Math.Abs((long)tile.row - placed.row) <= 2 &&
                UpgradeLevel(tile, after) >
                (tile == placed ? 1 : UpgradeLevel(tile, before))).ToArray();
        }

        public static IReadOnlyList<ConstructionTileData> AwardedGardenResidences(
            ConstructionTileData placed, IEnumerable<ConstructionTileData> existingTiles)
        {
            if (placed?.category != ConstructionCategory.Green &&
                placed?.category != ConstructionCategory.Residence)
                return Array.Empty<ConstructionTileData>();

            var before = ExcludePlacement(placed, existingTiles);
            var after = before.Append(placed).ToArray();
            // The board only grows. A home can qualify at most once, even
            // when either the home or its supporting greens arrive last.
            return after.Where(home => IsGardenResidence(home, after) &&
                (home == placed || !IsGardenResidence(home, before))).ToArray();
        }

        private static ConstructionTileData[] ExcludePlacement(
            ConstructionTileData placed, IEnumerable<ConstructionTileData> builtTiles) =>
            builtTiles.Where(tile => tile != null &&
                (tile.column != placed.column || tile.row != placed.row)).ToArray();

        private static bool SharesEdge(ConstructionTileData first,
            ConstructionTileData second) => ConstructionBoardModel.AreCardinalNeighbours(
                first.column, first.row, second.column, second.row);

        private static bool Pair(ConstructionCategory first,
            ConstructionCategory second, ConstructionCategory left,
            ConstructionCategory right) =>
            first == left && second == right ||
            first == right && second == left;
    }

    public sealed partial class ConstructionRunModel
    {
        private ConstructionPlacementScoreSaveData placementScore;

        public bool UsesOverallScore => UsesSpecialTasks || UsesPlacementScore;
        public int PlacementRulesVersion => placementScore?.version ?? 0;
        public bool UsesPlacementScore => (UsesFillHand ||
            Mode == ConstructionGameMode.Endless && ScoringVersion >= 13) &&
            placementScore?.version is
                ConstructionPlacementScore.OriginalRulesVersion or
                ConstructionPlacementScore.GreenUpgradeRulesVersion or
                ConstructionPlacementScore.RulesVersion or
                ConstructionPlacementScore.BalancedRulesVersion or
                ConstructionPlacementScore.NeighborUpgradeRulesVersion or
                ConstructionPlacementScore.LayoutQualityRulesVersion;
        public bool UsesGreenUpgrades => endlessUpgradeRulesVersion == 1 ||
            UsesPlacementScore &&
            placementScore.version >=
                ConstructionPlacementScore.GreenUpgradeRulesVersion;
        public bool UsesCompleteNeighborhoods => UsesPlacementScore &&
            placementScore.version >= ConstructionPlacementScore.RulesVersion;
        public bool UsesNeighborUpgrades => endlessUpgradeRulesVersion == 1 ||
            UsesFiniteFillDeck &&
            placementScore?.version >= ConstructionPlacementScore.NeighborUpgradeRulesVersion;
        public int GreenUpgradeLevel(string tileId)
        {
            if (!UsesGreenUpgrades) return 0;
            var tiles = board.BuiltTiles;
            var green = tiles.FirstOrDefault(tile => tile.id == tileId &&
                tile.category == ConstructionCategory.Green);
            return green == null ? 0 : board.GreenUpgradeLevel(green);
        }
        public int PlacementScore => UsesPlacementScore
            ? placementScore.entries.Sum(entry => entry.points) +
              (placementScore.communityAwards?.Sum(award => award.points) ?? 0)
            : 0;
        public int LastPlacementPoints => UsesPlacementScore &&
            placementScore.entries.Count > 0
                ? placementScore.entries[placementScore.entries.Count - 1].points +
                  (placementScore.communityAwards?.Where(award =>
                      award.placementTileId ==
                      placementScore.entries[placementScore.entries.Count - 1]
                          .tileId).Sum(award => award.points) ?? 0)
                : 0;
        public int LastCommunityAwardCount => UsesPlacementScore &&
            placementScore.entries.Count > 0
                ? placementScore.communityAwards?.Count(award =>
                    award.placementTileId ==
                    placementScore.entries[placementScore.entries.Count - 1]
                        .tileId) ?? 0
                : 0;

        private void InitializePlacementScore()
        {
            placementScore = UsesFillHand || Mode == ConstructionGameMode.Endless && ScoringVersion >= 13
                ? new ConstructionPlacementScoreSaveData
                {
                    version = ScoringVersion >= 13
                        ? ConstructionPlacementScore.LayoutQualityRulesVersion : UsesFiniteFillDeck
                        ? ConstructionPlacementScore.NeighborUpgradeRulesVersion
                        : ConstructionPlacementScore.OriginalRulesVersion
                }
                : null;
            if (placementScore != null)
                foreach (var tile in board.BuiltTiles.OrderBy(tile => tile.buildIndex))
                    RecordPlacementScore(tile);
        }

        private void RecordPlacementScore(ConstructionTileData placed)
        {
            if (!UsesPlacementScore) return;
            placementScore.entries.Add(new ConstructionPlacementScoreEntry
            {
                tileId = placed.id,
                points = ConstructionPlacementScore.PointsFor(placed,
                    board.BuiltTiles, placementScore.version)
            });
        }

        private void AwardCompletedCommunities(
            ConstructionHumanDayResult observedHuman)
        {
            if (!UsesPlacementScore || placementScore.version <
                    ConstructionPlacementScore.BalancedRulesVersion ||
                observedHuman?.residents == null) return;
            var tiles = board.BuiltTiles.OrderBy(tile => tile.buildIndex)
                .ToArray();
            var placement = tiles.LastOrDefault();
            if (placement == null) return;
            foreach (var person in observedHuman.residents.Where(person =>
                         person != null && person.completedCycle))
            {
                if (placementScore.communityAwards.Any(award =>
                        award.homeTileId == person.homeTileId)) continue;
                var home = tiles.FirstOrDefault(tile =>
                    tile.id == person.homeTileId);
                if (!ConstructionPlacementScore.HasCompleteNeighborhood(
                        home, tiles)) continue;
                placementScore.communityAwards.Add(
                    new ConstructionCommunityAwardSaveData
                    {
                        homeTileId = home.id,
                        placementTileId = placement.id,
                        points = ConstructionPlacementScore.CommunityPoints(placementScore.version)
                    });
            }
        }

        private ConstructionPlacementScoreSaveData ExportPlacementScore() =>
            !UsesPlacementScore ? null : new ConstructionPlacementScoreSaveData
            {
                version = placementScore.version,
                entries = placementScore.entries.Select(entry =>
                    new ConstructionPlacementScoreEntry
                    {
                        tileId = entry.tileId,
                        points = entry.points
                    }).ToList(),
                communityAwards = placementScore.communityAwards.Select(award =>
                    new ConstructionCommunityAwardSaveData
                    {
                        homeTileId = award.homeTileId,
                        placementTileId = award.placementTileId,
                        points = award.points
                    }).ToList()
            };

        private bool TryRestorePlacementScore(
            ConstructionPlacementScoreSaveData saved)
        {
            if (!UsesFillHand && !(Mode == ConstructionGameMode.Endless && ScoringVersion >= 13))
            {
                placementScore = null;
                return saved == null || saved.version == 0;
            }
            // Runs saved before this one-time rule keep their original score.
            if (saved == null || saved.version == 0)
            {
                placementScore = null;
                return score.ScoringVersion < 12;
            }
            if ((score.ScoringVersion >= 12) !=
                (saved.version >= ConstructionPlacementScore
                    .BalancedRulesVersion)) return false;
            if ((score.ScoringVersion >= 13) !=
                (saved.version >= ConstructionPlacementScore.LayoutQualityRulesVersion)) return false;
            if (saved.version == ConstructionPlacementScore.NeighborUpgradeRulesVersion &&
                !UsesFiniteFillDeck) return false;
            var tiles = board.BuiltTiles.OrderBy(tile => tile.buildIndex)
                .ToArray();
            if (saved.version is not (ConstructionPlacementScore.OriginalRulesVersion
                    or ConstructionPlacementScore.GreenUpgradeRulesVersion
                    or ConstructionPlacementScore.RulesVersion
                    or ConstructionPlacementScore.BalancedRulesVersion
                    or ConstructionPlacementScore.NeighborUpgradeRulesVersion
                    or ConstructionPlacementScore.LayoutQualityRulesVersion) ||
                saved.entries == null || saved.entries.Count != tiles.Length)
                return false;
            for (var index = 0; index < tiles.Length; index++)
            {
                var entry = saved.entries[index];
                if (entry == null || entry.tileId != tiles[index].id ||
                    entry.points != ConstructionPlacementScore.PointsFor(
                        tiles[index], tiles, saved.version)) return false;
            }
            if (saved.version >= ConstructionPlacementScore.BalancedRulesVersion)
            {
                if (saved.communityAwards == null) return false;
                var awardedHomes = new HashSet<string>();
                foreach (var award in saved.communityAwards)
                {
                    if (award == null ||
                        !awardedHomes.Add(award.homeTileId) ||
                        award.points != ConstructionPlacementScore.CommunityPoints(saved.version)) return false;
                    var placement = tiles.FirstOrDefault(tile =>
                        tile.id == award.placementTileId);
                    var home = tiles.FirstOrDefault(tile =>
                        tile.id == award.homeTileId);
                    if (placement == null || home == null ||
                        home.buildIndex > placement.buildIndex ||
                        !ConstructionPlacementScore.HasCompleteNeighborhood(
                            home, tiles.Where(tile => tile.buildIndex <=
                                placement.buildIndex)) ||
                        !score.Days.Any(day => day.day ==
                            placement.builtDay &&
                            day.completedWorkCycles > 0))
                        return false;
                    if (LastHumanDay?.day == placement.builtDay &&
                        !LastHumanDay.residents.Any(person =>
                            person.homeTileId == home.id &&
                            person.completedCycle)) return false;
                }
            }
            placementScore = new ConstructionPlacementScoreSaveData
            {
                version = saved.version,
                entries = saved.entries.Select(entry =>
                    new ConstructionPlacementScoreEntry
                    {
                        tileId = entry.tileId,
                        points = entry.points
                    }).ToList(),
                communityAwards = saved.communityAwards?.Select(award =>
                    new ConstructionCommunityAwardSaveData
                    {
                        homeTileId = award.homeTileId,
                        placementTileId = award.placementTileId,
                        points = award.points
                    }).ToList() ?? new List<ConstructionCommunityAwardSaveData>()
            };
            return true;
        }
    }
}
