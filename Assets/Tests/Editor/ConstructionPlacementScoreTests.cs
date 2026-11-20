using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionPlacementScoreTests
    {
        [Test]
        public void EightNeighboursScoreEachExistingCombinationOnce()
        {
            var placed = Tile(4, 2, 4, ConstructionCategory.Residence);
            var earlier = new[]
            {
                Tile(1, 3, 3, ConstructionCategory.Green),
                Tile(2, 4, 3, ConstructionCategory.Workshop),
                Tile(3, 5, 2, ConstructionCategory.Waste),
                Tile(5, 3, 2, ConstructionCategory.Restaurant),
                Tile(0, 6, 2, ConstructionCategory.Green)
            };
            // Diagonal green +1, workshop +1, waste -1. The later restaurant
            // and the distant green do not affect this placement.
            Assert.That(ConstructionPlacementScore.PointsFor(placed, earlier),
                Is.EqualTo(1));
            Assert.That(ConstructionPlacementScore.PairPoints(
                    ConstructionCategory.Green, ConstructionCategory.Street),
                Is.EqualTo(-1));
            Assert.That(ConstructionPlacementScore.PairPoints(
                    ConstructionCategory.Green, ConstructionCategory.Green),
                Is.EqualTo(1));
            Assert.That(ConstructionPlacementScore.PairPoints(
                    ConstructionCategory.Waste, ConstructionCategory.Square),
                Is.Zero);
        }

        [Test]
        public void EdgeConnectedGreenUpgradesAndAddsBuildPoints()
        {
            var oak = Tile(1, 3, 3, ConstructionCategory.Green);
            var edge = Tile(2, 4, 3, ConstructionCategory.Green);
            var chain = Tile(3, 5, 3, ConstructionCategory.Green);
            var diagonal = Tile(2, 4, 4, ConstructionCategory.Green);
            Assert.That(ConstructionPlacementScore.GreenUpgradeLevel(oak,
                new[] { oak }), Is.EqualTo(1));
            Assert.That(ConstructionPlacementScore.GreenUpgradeLevel(edge,
                new[] { oak, edge }), Is.EqualTo(2));
            Assert.That(ConstructionPlacementScore.GreenUpgradeLevel(chain,
                new[] { oak, edge, chain }), Is.EqualTo(3));
            Assert.That(ConstructionPlacementScore.GreenUpgradeLevel(diagonal,
                new[] { oak, diagonal }), Is.EqualTo(1),
                "A corner scores adjacency but cannot upgrade habitat.");
            Assert.That(ConstructionPlacementScore.PointsFor(edge,
                new[] { oak, edge }, ConstructionPlacementScore.RulesVersion),
                Is.EqualTo(3), "Stronger adjacency plus one upgrade point.");
            Assert.That(ConstructionPlacementScore.PointsFor(chain,
                new[] { oak, edge, chain }, ConstructionPlacementScore.RulesVersion),
                Is.EqualTo(4), "Stronger adjacency plus two upgrade points.");
            Assert.That(ConstructionPlacementScore.PointsFor(diagonal,
                new[] { oak, diagonal }, ConstructionPlacementScore.RulesVersion),
                Is.EqualTo(2));
            Assert.That(ConstructionPlacementScore.PointsFor(edge,
                new[] { oak, edge },
                ConstructionPlacementScore.GreenUpgradeRulesVersion),
                Is.EqualTo(2), "Version-two saves keep their build awards.");
            Assert.That(ConstructionPlacementScore.PointsFor(edge,
                new[] { oak, edge },
                ConstructionPlacementScore.OriginalRulesVersion),
                Is.EqualTo(1), "Existing saves retain their recorded score.");
        }

        [Test]
        public void MixedNineRoomBlockEarnsAVisibleOneTimeAward()
        {
            // Matches the pictured 3x3 arrangement: square / street / work,
            // restaurant / home / supermarket, waste / shrub / meadow.
            var tiles = new[]
            {
                Tile(1, 2, 2, ConstructionCategory.Square),
                Tile(2, 3, 2, ConstructionCategory.Street),
                Tile(3, 4, 2, ConstructionCategory.Workshop),
                Tile(4, 2, 3, ConstructionCategory.Restaurant),
                Tile(5, 3, 3, ConstructionCategory.Residence),
                Tile(6, 4, 3, ConstructionCategory.Supermarket),
                Tile(7, 2, 4, ConstructionCategory.Waste),
                Tile(8, 3, 4, ConstructionCategory.Green,
                    GreenPlanting.Shrub),
                Tile(9, 4, 4, ConstructionCategory.Green,
                    GreenPlanting.Meadow)
            };
            var oldTotal = tiles.Sum(tile =>
                ConstructionPlacementScore.PointsFor(tile, tiles,
                    ConstructionPlacementScore.GreenUpgradeRulesVersion));
            var newTotal = tiles.Sum(tile =>
                ConstructionPlacementScore.PointsFor(tile, tiles,
                    ConstructionPlacementScore.RulesVersion));
            Assert.That(oldTotal, Is.EqualTo(8));
            Assert.That(newTotal, Is.EqualTo(42));
            Assert.That(ConstructionPlacementScore.PointsFor(
                    tiles[8], tiles, ConstructionPlacementScore.RulesVersion),
                Is.EqualTo(31), "The final meadow completes the block once.");
            var homeLast = tiles.Where(tile => tile.category !=
                    ConstructionCategory.Residence)
                .Select((tile, index) => Tile(index + 1, tile.column,
                    tile.row, tile.category, tile.planting))
                .Append(Tile(9, 3, 3, ConstructionCategory.Residence))
                .ToArray();
            Assert.That(homeLast.Sum(tile =>
                    ConstructionPlacementScore.PointsFor(tile, homeLast,
                        ConstructionPlacementScore.RulesVersion)),
                Is.EqualTo(42), "The award also applies when the home is last.");
            var duplicateSpecies = tiles.ToArray();
            duplicateSpecies[8] = Tile(9, 4, 4,
                ConstructionCategory.Green, GreenPlanting.Shrub);
            Assert.That(duplicateSpecies.Sum(tile =>
                    ConstructionPlacementScore.PointsFor(tile,
                        duplicateSpecies,
                        ConstructionPlacementScore.RulesVersion)),
                Is.EqualTo(18),
                "Two identical green types do not earn the mixed-habitat award.");
            var outside = Tile(10, 6, 6, ConstructionCategory.Square);
            Assert.That(ConstructionPlacementScore.PointsFor(outside,
                    tiles.Append(outside),
                    ConstructionPlacementScore.RulesVersion),
                Is.Zero, "An already completed home is never paid again.");
        }

        [Test]
        public void UpgradedMatureGreenStoresAndProducesMoreFood()
        {
            var board = new ConstructionBoardModel();
            var oak = board.At(3, 3);
            Assert.That(ConstructionFoodModel.Capacity(board, oak, true),
                Is.EqualTo(3));
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, builtDay: 1),
                Is.True);
            Assert.That(ConstructionFoodModel.Capacity(board, oak, true),
                Is.EqualTo(4));
            Assert.That(ConstructionFoodModel.Capacity(board, meadow, true),
                Is.EqualTo(3));
            Assert.That(ConstructionFoodModel.DailyYield(oak, 3,
                greenUpgradeLevel: 2), Is.EqualTo(2));
            Assert.That(ConstructionFoodModel.DailyYield(meadow, 4,
                greenUpgradeLevel: 2), Is.EqualTo(3));
            Assert.That(ConstructionFoodModel.DailyYield(meadow, 3,
                greenUpgradeLevel: 2), Is.Zero,
                "Meadows still have their alternate-day rest.");
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, builtDay: 1), Is.True);
            Assert.That(ConstructionFoodModel.Capacity(board, oak, true),
                Is.EqualTo(5));
            Assert.That(ConstructionFoodModel.DailyYield(oak, 3,
                greenUpgradeLevel: 3), Is.EqualTo(3));
            var food = new ConstructionFoodModel();
            var preview = food.PreviewDay(3, board, null,
                squirrelsPresent: 0, pigeonsPresent: 0,
                useGreenUpgrade: true);
            var settled = food.SettleDay(3, board, null,
                squirrelsPresent: 0, pigeonsPresent: 0,
                useGreenUpgrade: true);
            Assert.That(preview.foodChanges.Single(change =>
                change.tileId == oak.id).produced, Is.EqualTo(3));
            Assert.That(settled.foodChanges.Single(change =>
                change.tileId == oak.id).produced, Is.EqualTo(3));
            Assert.That(food.Stock(oak.id), Is.EqualTo(4));

            var saved = new ConstructionFoodSaveData
            {
                lastSettledDay = 2,
                tiles = new System.Collections.Generic.List<ConstructionFoodTileSaveData>
                {
                    new() { tileId = oak.id, stock = 5 },
                    new() { tileId = meadow.id, stock = 4 }
                }
            };
            Assert.That(ConstructionFoodModel.TryRestore(saved, board, 3,
                false, out _, useGreenUpgrade: true), Is.True);
            Assert.That(ConstructionFoodModel.TryRestore(saved, board, 3,
                false, out _, useGreenUpgrade: false), Is.False);
        }

        [Test]
        public void HandRunRecordsPlacementSeparatelyAndRejectsTamperedSave()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            // Keep this compatibility case on the saved V12 placement rules.
            var legacy = run.Export();
            legacy.score.scoringVersion = 12;
            legacy.placementScore.version = ConstructionPlacementScore.NeighborUpgradeRulesVersion;
            Assert.That(ConstructionRunModel.TryRestore(legacy, out run), Is.True);
            Assert.That(run.TryBuild(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.UsesGreenUpgrades, Is.True);
            Assert.That(run.GreenUpgradeLevel(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1));
            Assert.That(run.LastPlacementPoints, Is.Zero);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.LastPlacementPoints, Is.EqualTo(8));
            Assert.That(run.PlacementScore, Is.EqualTo(8));
            Assert.That(run.BalanceScore, Is.GreaterThanOrEqualTo(0));
            Assert.That(run.OverallScore, Is.EqualTo(run.BalanceScore +
                run.PlacementScore + run.SpecialTaskScore));

            var saved = run.Export();
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(restored.PlacementScore, Is.EqualTo(8));
            var earlierRules = run.Export();
            earlierRules.placementScore.version =
                ConstructionPlacementScore.OriginalRulesVersion;
            Assert.That(ConstructionRunModel.TryRestore(earlierRules,
                out _), Is.False, "A V12 save cannot silently switch placement rules.");
            saved.placementScore.entries.Last().points = 3;
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.False);
        }

        [Test]
        public void BalancedPairsOnlyCountSharedEdgesAndGreenMilestonesOnce()
        {
            var oak = Tile(1, 3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak);
            var diagonal = Tile(2, 4, 4, ConstructionCategory.Residence);
            Assert.That(ConstructionPlacementScore.PointsFor(diagonal,
                new[] { oak, diagonal },
                ConstructionPlacementScore.BalancedRulesVersion), Is.Zero);

            var meadow = Tile(2, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var shrub = Tile(3, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            var more = Tile(4, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Assert.That(ConstructionPlacementScore.PointsFor(meadow,
                new[] { oak, meadow },
                ConstructionPlacementScore.BalancedRulesVersion),
                Is.EqualTo(26));
            Assert.That(ConstructionPlacementScore.PointsFor(shrub,
                new[] { oak, meadow, shrub },
                ConstructionPlacementScore.BalancedRulesVersion),
                Is.EqualTo(46));
            Assert.That(ConstructionPlacementScore.PointsFor(more,
                new[] { oak, meadow, shrub, more },
                ConstructionPlacementScore.BalancedRulesVersion),
                Is.EqualTo(6));
        }

        [Test]
        public void BalancedCommunityRequiresAllFourFacilitiesAndDistinctGreenTypes()
        {
            var home = Tile(1, 3, 3, ConstructionCategory.Residence);
            var neighbours = new[]
            {
                home,
                Tile(2, 2, 2, ConstructionCategory.Workshop),
                Tile(3, 3, 2, ConstructionCategory.Restaurant),
                Tile(4, 4, 2, ConstructionCategory.Supermarket),
                Tile(5, 2, 3, ConstructionCategory.Square),
                Tile(6, 4, 3, ConstructionCategory.Green,
                    GreenPlanting.Oak),
                Tile(7, 2, 4, ConstructionCategory.Green,
                    GreenPlanting.Meadow)
            };
            Assert.That(ConstructionPlacementScore.HasCompleteNeighborhood(
                home, neighbours), Is.True);
            Assert.That(ConstructionPlacementScore.HasCompleteNeighborhood(
                home, neighbours.Take(6)), Is.False);
            neighbours[6].planting = GreenPlanting.Oak;
            Assert.That(ConstructionPlacementScore.HasCompleteNeighborhood(
                home, neighbours), Is.False);
        }

        [Test]
        public void NewGreenUpgradesExistingOakAndHomeAcrossTwoEdges()
        {
            var oak = Tile(1, 3, 3, ConstructionCategory.Green, GreenPlanting.Oak);
            var home = Tile(2, 2, 3, ConstructionCategory.Residence);
            var diagonalHome = Tile(3, 2, 2, ConstructionCategory.Residence);
            var meadow = Tile(4, 4, 3, ConstructionCategory.Green, GreenPlanting.Meadow);
            var before = new[] { oak, home, diagonalHome };

            Assert.That(ConstructionPlacementScore.NewlyUpgradedTiles(meadow, before),
                Is.EquivalentTo(new[] { oak, home, meadow }));
            Assert.That(ConstructionPlacementScore.AwardedGardenResidences(meadow, before),
                Is.EqualTo(new[] { home }));
            Assert.That(ConstructionPlacementScore.PointsFor(meadow, before,
                ConstructionPlacementScore.NeighborUpgradeRulesVersion), Is.EqualTo(30));
            Assert.That(ConstructionPlacementScore.PointsFor(meadow, before,
                ConstructionPlacementScore.BalancedRulesVersion), Is.EqualTo(26));

            var laterGreen = Tile(5, 5, 3, ConstructionCategory.Green, GreenPlanting.Shrub);
            Assert.That(ConstructionPlacementScore.AwardedGardenResidences(laterGreen,
                before.Append(meadow)), Is.Empty);
            Assert.That(ConstructionPlacementScore.AwardedGardenResidences(home,
                new[] { oak, meadow }), Is.EqualTo(new[] { home }));
        }

        [Test]
        public void RestaurantAndStreetUpgradeOnlyFromTheIntendedEdges()
        {
            var restaurant = Tile(1, 1, 1, ConstructionCategory.Restaurant);
            var cornerSquare = Tile(2, 2, 2, ConstructionCategory.Square);
            var edgeSquare = Tile(3, 1, 2, ConstructionCategory.Square);
            Assert.That(ConstructionPlacementScore.IsTerraceRestaurant(restaurant,
                new[] { cornerSquare }), Is.False);
            Assert.That(ConstructionPlacementScore.NewlyUpgradedTiles(edgeSquare,
                new[] { restaurant, cornerSquare }), Is.EqualTo(new[] { restaurant }));

            var street = Tile(4, 4, 4, ConstructionCategory.Street);
            var north = Tile(5, 4, 3, ConstructionCategory.Green);
            var west = Tile(6, 3, 4, ConstructionCategory.Green);
            var east = Tile(7, 5, 4, ConstructionCategory.Green);
            var south = Tile(8, 4, 5, ConstructionCategory.Green);
            Assert.That(ConstructionPlacementScore.IsSlowStreet(street,
                new[] { north, west }), Is.False);
            Assert.That(ConstructionPlacementScore.SlowStreetAxis(street,
                new[] { north, west, east }), Is.EqualTo(ConstructionSlowStreetAxis.EastWest));
            Assert.That(ConstructionPlacementScore.SlowStreetAxis(street,
                new[] { north, west, east, south }),
                Is.EqualTo(ConstructionSlowStreetAxis.NorthSouth));
        }

        private static ConstructionTileData Tile(int index, int column,
            int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None) => new()
        {
            id = index.ToString(),
            buildIndex = index,
            column = column,
            row = row,
            category = category,
            planting = planting
        };
    }
}
