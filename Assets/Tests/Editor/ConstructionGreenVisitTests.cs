using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionGreenVisitTests
    {
        [Test]
        public void NewResidentGreenAwardAddsOnePointWithoutChangingOldRules()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 2, ConstructionCategory.Residence);
            Build(board, 3, 1, ConstructionCategory.Restaurant);
            Build(board, 2, 2, ConstructionCategory.Workshop);

            var oldResult = ConstructionHumanModel.SettleDay(1, board)
                .residents.Single();
            var newResult = ConstructionHumanModel.SettleDay(1, board,
                enhancedGreenVisitScore: true).residents.Single();
            Assert.That(newResult.greenVisitTileId,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(oldResult.greenNeighbourBonus, Is.EqualTo(1));
            Assert.That(newResult.greenNeighbourBonus, Is.EqualTo(2));
            Assert.That(newResult.satisfactionPoints,
                Is.EqualTo(oldResult.satisfactionPoints + 1));
        }

        [Test]
        public void NewResidentCapAllowsNineButVersionTenKeepsEight()
        {
            var oldLedger = new ConstructionScoreLedger(10);
            var newLedger = new ConstructionScoreLedger(11);
            Assert.That(oldLedger.TryRecordDay(1, 1, 1, 4, 9), Is.False);
            Assert.That(newLedger.TryRecordDay(1, 1, 1, 4, 9), Is.True);
            Assert.That(ConstructionScoreLedger.TryRestore(newLedger.Export(),
                out var restored), Is.True);
            Assert.That(restored.HumanScore, Is.EqualTo(9));
        }

        [Test]
        public void OnlyGreenActuallyUsedForFoodGetsTheExtraSourcePoint()
        {
            var board = new ConstructionBoardModel();
            var ecology = new ConstructionFoodModel().PreviewDay(1, board,
                null, useLocalFoodAreaScore: true,
                useFedAnimalScore: true);
            var oldResult = ConstructionFoodAreaScorer.ScoreFed(board,
                ecology);
            var newResult = ConstructionFoodAreaScorer.ScoreFed(board,
                ecology, enhancedGreenSourceScore: true);
            var balancedResult = ConstructionFoodAreaScorer.ScoreFed(board,
                ecology, enhancedGreenSourceScore: true,
                balancedScore: true);
            Assert.That(oldResult.points, Is.EqualTo(6));
            Assert.That(newResult.points, Is.EqualTo(7));
            Assert.That(balancedResult.points, Is.EqualTo(5));

            ecology.squirrelFoodTileIds.Clear();
            ecology.squirrelsFed = 0;
            ecology.squirrelAte = false;
            var withoutMeal = ConstructionFoodAreaScorer.ScoreFed(board,
                ecology, enhancedGreenSourceScore: true);
            Assert.That(withoutMeal.points, Is.Zero);
            Assert.That(withoutMeal.sourceTileIds, Is.Empty);
        }

        [Test]
        public void WildlifeCanUseOutdoorRoomsButCannotCutThroughRestaurant()
        {
            var board = new ConstructionBoardModel();
            var green = Build(board, 4, 3, ConstructionCategory.Green);
            var square = Build(board, 5, 3, ConstructionCategory.Square);
            Build(board, 4, 2, ConstructionCategory.Residence);
            Build(board, 5, 2, ConstructionCategory.Street);
            var waste = Build(board, 5, 1, ConstructionCategory.Waste);
            Build(board, 6, 2, ConstructionCategory.Street);
            var restaurant = Build(board, 6, 1,
                ConstructionCategory.Restaurant);
            var market = Build(board, 6, 3,
                ConstructionCategory.Supermarket);
            Assert.That(board.AnimalRouteBetween(green.id, square.id),
                Is.Not.Empty);
            Assert.That(board.AnimalRouteBetween(green.id, waste.id),
                Is.Not.Empty);
            Assert.That(board.AnimalRouteBetween(green.id, market.id),
                Is.Not.Empty);
            Assert.That(board.AnimalRouteBetween(green.id, restaurant.id),
                Is.Empty);
        }

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(board.TryBuild(column, row, category,
                category == ConstructionCategory.Green
                    ? GreenPlanting.Meadow : GreenPlanting.None,
                out var tile, out var failure, 1), Is.True,
                $"{category} at {column},{row}: {failure}");
            return tile;
        }
    }
}
