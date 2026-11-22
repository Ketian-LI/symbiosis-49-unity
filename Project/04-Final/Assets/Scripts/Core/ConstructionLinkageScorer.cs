using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionLinkageKind
    {
        None,
        SharedSquare,
        SharedSupermarket,
        SharedGreen
    }

    public sealed class ConstructionLinkageDayResult
    {
        public int sharedSquareMeals;
        public int sharedSupermarketMeals;
        public int sharedGreenMeals;
        public int biodiversityHomes;
        public int SharedPoints =>
            3 * sharedSquareMeals + 2 * sharedSupermarketMeals +
            sharedGreenMeals;
        public int LinkagePoints =>
            SharedPoints + 2 * biodiversityHomes;
    }

    // Linkage is included in resident satisfaction. A source and resident
    // each receive at most one shared-food award per day. V8 biodiversity is
    // a separate local award for at most two completed-cycle homes per day.
    public static class ConstructionLinkageScorer
    {
        public const int MaxSharedPointsPerDay = 6;
        public const int MaxBiodiversityHomesPerDay = 2;
        public const int BiodiversityPointsPerHome = 2;
        public const int MaxPointsPerDay = MaxSharedPointsPerDay +
            BiodiversityPointsPerHome * MaxBiodiversityHomesPerDay;
        public const int MaxSharedPointsPerResident = 3;
        public const int MaxPointsPerResident = MaxSharedPointsPerResident +
            BiodiversityPointsPerHome;

        public static int PointsFor(ConstructionLinkageKind kind) => kind switch
        {
            ConstructionLinkageKind.SharedSquare => 3,
            ConstructionLinkageKind.SharedSupermarket => 2,
            ConstructionLinkageKind.SharedGreen => 1,
            _ => 0
        };

        public static ConstructionLinkageDayResult Apply(
            ConstructionBoardModel board, int day,
            ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human,
            bool useLocalFoodAreaScore = false,
            bool useBiodiversityBonus = false,
            string pigeonHabitatTileId = null,
            string hedgehogHabitatTileId = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (ecology == null) throw new ArgumentNullException(nameof(ecology));
            if (human == null) throw new ArgumentNullException(nameof(human));
            if (human.day != day || ecology.day != day)
                throw new ArgumentException("Linkage inputs must describe the same day.");
            if (human.residents == null || human.residents.Any(person =>
                    person == null || person.linkageBonus != 0 ||
                    person.linkageKind != ConstructionLinkageKind.None ||
                    person.biodiversityBonus != 0 ||
                    !string.IsNullOrEmpty(person.linkageSourceTileId) ||
                    person.biodiversityHabitatTileIds?.Count > 0))
                throw new ArgumentException("Linkage requires an unscored resident day.");

            var result = new ConstructionLinkageDayResult();
            var tiles = board.BuiltTiles.Where(tile => tile.builtDay <= day)
                .ToDictionary(tile => tile.id);
            var residents = human.residents.OrderBy(person => person.homeTileId,
                StringComparer.Ordinal).ToArray();
            var sharedUrbanTiles = useLocalFoodAreaScore
                ? ecology.scoredFoodTileIds ?? new List<string>()
                : ecology.urbanMealTileIds ?? new List<string>();

            bool Grant(ConstructionResidentDayResult person,
                ConstructionLinkageKind kind, int points, string sourceTileId)
            {
                if (person == null || !person.completedCycle ||
                    person.linkageKind != ConstructionLinkageKind.None ||
                    result.SharedPoints + points > MaxSharedPointsPerDay)
                    return false;
                person.linkageKind = kind;
                person.linkageBonus = points;
                person.linkageSourceTileId = sourceTileId;
                person.satisfactionPoints += points;
                switch (kind)
                {
                    case ConstructionLinkageKind.SharedSquare:
                        result.sharedSquareMeals++;
                        break;
                    case ConstructionLinkageKind.SharedSupermarket:
                        result.sharedSupermarketMeals++;
                        break;
                    case ConstructionLinkageKind.SharedGreen:
                        result.sharedGreenMeals++;
                        break;
                }
                return true;
            }

            // In V7 the animal's nearby, available food area is enough for a
            // shared-space award. Older runs still require an observed meal.
            foreach (var id in sharedUrbanTiles
                         .Where(id => id != null && tiles.TryGetValue(id,
                             out var tile) && tile.category ==
                             ConstructionCategory.Square)
                         .Distinct().OrderBy(id => id, StringComparer.Ordinal))
            {
                var visitor = residents.FirstOrDefault(person =>
                    person.visitedSquare && person.squareTileId == id);
                Grant(visitor, ConstructionLinkageKind.SharedSquare, 3, id);
            }

            // A market produces only one wildlife portion after a resident's
            // actual meal. Prefer an uncredited diner who completed the cycle.
            foreach (var id in sharedUrbanTiles
                         .Where(id => id != null && tiles.TryGetValue(id,
                             out var tile) && tile.category ==
                             ConstructionCategory.Supermarket)
                         .Distinct().OrderBy(id => id, StringComparer.Ordinal))
            {
                var diner = residents.FirstOrDefault(person =>
                    person.mealTileId == id && person.ate &&
                    person.completedCycle &&
                    person.linkageKind == ConstructionLinkageKind.None);
                Grant(diner, ConstructionLinkageKind.SharedSupermarket, 2, id);
            }

            var greenMeals = new List<string>();
            if (useLocalFoodAreaScore)
                greenMeals.AddRange(ecology.scoredFoodTileIds ??
                    new List<string>());
            else
            {
                // Historical meal scoring preserves multiple meals from one
                // green tile for distinct neighbouring homes.
                greenMeals.AddRange(ecology.squirrelFoodTileIds ??
                    new List<string>());
                greenMeals.AddRange(ecology.pigeonMealTileIds ??
                    new List<string>());
                if (ecology.hedgehogFed &&
                    !string.IsNullOrEmpty(ecology.hedgehogFoodTileId))
                    greenMeals.Add(ecology.hedgehogFoodTileId);
            }
            foreach (var id in greenMeals.Where(id => id != null &&
                         tiles.TryGetValue(id, out var tile) &&
                         tile.category == ConstructionCategory.Green &&
                         ConstructionFoodModel.MaturityStage(tile, day) == 2)
                         .OrderBy(id => id, StringComparer.Ordinal))
            {
                var green = tiles[id];
                var neighbour = residents.FirstOrDefault(person =>
                    person.completedCycle &&
                    person.linkageKind == ConstructionLinkageKind.None &&
                    tiles.TryGetValue(person.homeTileId, out var home) &&
                    home.category == ConstructionCategory.Residence &&
                    (useLocalFoodAreaScore
                        ? ConstructionBoardModel.AreNeighbours(home.column,
                            home.row, green.column, green.row)
                        : Math.Abs(home.column - green.column) +
                          Math.Abs(home.row - green.row) == 1));
                Grant(neighbour, ConstructionLinkageKind.SharedGreen, 1, id);
            }
            if (useBiodiversityBonus)
            {
                // An occupied habitat counts only when that species actually
                // found food today. A second species, rather than a second
                // animal of the same species, creates the neighbourhood bonus.
                var fedSquirrelHomeIds = new HashSet<string>(
                    ecology.squirrelMealHomeTileIds ?? new List<string>());
                var squirrelHomes = ecology.squirrelsFed > 0
                    ? ConstructionFoodModel.SquirrelHomeTiles(board, day,
                            ecology.squirrelsPresent)
                        .Where(tile => fedSquirrelHomeIds.Contains(tile.id))
                        .ToArray()
                    : Array.Empty<ConstructionTileData>();
                var pigeonHomeId = ecology.pigeonHabitatTileId ??
                    pigeonHabitatTileId;
                var hedgehogHomeId = ecology.hedgehogHabitatTileId ??
                    hedgehogHabitatTileId;

                ConstructionTileData FedHabitat(string id,
                    GreenPlanting planting, bool fed) =>
                    fed && !string.IsNullOrEmpty(id) &&
                    tiles.TryGetValue(id, out var tile) &&
                    tile.category == ConstructionCategory.Green &&
                    tile.planting == planting ? tile : null;

                var pigeonHome = FedHabitat(pigeonHomeId,
                    GreenPlanting.Meadow, ecology.pigeonsFed > 0);
                var hedgehogHome = FedHabitat(hedgehogHomeId,
                    GreenPlanting.Shrub, ecology.hedgehogFed);
                foreach (var person in residents)
                {
                    if (result.biodiversityHomes >=
                            MaxBiodiversityHomesPerDay ||
                        !person.completedCycle ||
                        !tiles.TryGetValue(person.homeTileId, out var home) ||
                        home.category != ConstructionCategory.Residence)
                        continue;
                    bool Near(ConstructionTileData habitat) =>
                        habitat != null &&
                        ConstructionBoardModel.AreNeighbours(home.column,
                            home.row, habitat.column, habitat.row);
                    var habitats = new List<string>(2);
                    var squirrelHome = squirrelHomes.FirstOrDefault(Near);
                    if (squirrelHome != null) habitats.Add(squirrelHome.id);
                    if (Near(pigeonHome)) habitats.Add(pigeonHome.id);
                    if (habitats.Count < 2 && Near(hedgehogHome))
                        habitats.Add(hedgehogHome.id);
                    if (habitats.Count < 2) continue;
                    person.biodiversityBonus = BiodiversityPointsPerHome;
                    person.biodiversityHabitatTileIds = habitats;
                    person.satisfactionPoints += BiodiversityPointsPerHome;
                    result.biodiversityHomes++;
                }
            }
            return result;
        }
    }
}
