using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UnityEngine;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionFoodModelTests
    {
        [Test]
        public void StarterOakFeedsExactlyOneSquirrelAndCannotSettleTwice()
        {
            var board = new ConstructionBoardModel();
            var food = new ConstructionFoodModel();
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId), Is.EqualTo(1));

            var first = food.SettleDay(1, board, null);
            Assert.That(first.AnimalMeals, Is.EqualTo(1));
            Assert.That(first.squirrelFoodTileId,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId), Is.Zero);
            Assert.Throws<System.InvalidOperationException>(() =>
                food.SettleDay(1, board, null));

            var second = food.SettleDay(2, board, null);
            Assert.That(second.AnimalMeals, Is.EqualTo(1));
            Assert.That(second.foodChanges.Single().produced, Is.EqualTo(1));
        }

        [Test]
        public void ConnectedMeadowGrowsThenFlockActuallyEatsTwoPortions()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out var first), Is.True);
            Assert.That(first.AnimalMeals, Is.EqualTo(1));
            Assert.That(run.StoryStage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, out _), Is.True);
            Assert.That(run.GreenGrowthStage(meadow.id), Is.Zero);
            Assert.That(run.FoodStock(meadow.id), Is.Zero);

            Assert.That(run.TrySimulateDay(out var second), Is.True);
            Assert.That(second.pigeonsFed, Is.Zero,
                "The flock has not arrived until the following day.");
            Assert.That(run.PigeonTileId, Is.EqualTo(meadow.id));
            Assert.That(run.GreenGrowthStage(meadow.id), Is.EqualTo(1));

            Assert.That(run.TrySimulateDay(out var third), Is.True);
            Assert.That(third.pigeonsFed, Is.EqualTo(2));
            Assert.That(third.pigeonFoodTileId, Is.EqualTo(meadow.id));
            Assert.That(third.foodChanges.Single(change =>
                change.tileId == meadow.id).eaten, Is.EqualTo(2));
            Assert.That(run.FoodStock(meadow.id), Is.Zero);
            Assert.That(run.AnimalScore, Is.EqualTo(5));
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastEcologyDay.pigeonsFed, Is.Zero,
                "The meadow is recovering; no food means no pigeon points.");
            Assert.That(run.AnimalScore, Is.EqualTo(6));
        }

        [Test]
        public void SecondMeadowCanFeedFlockDuringFirstMeadowsRecovery()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var firstMeadow, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var secondMeadow, out _, 2), Is.True);
            var food = new ConstructionFoodModel();
            food.SettleDay(1, board, null);
            food.SettleDay(2, board, firstMeadow.id);
            var third = food.SettleDay(3, board, firstMeadow.id);

            Assert.That(third.pigeonsFed, Is.EqualTo(2));
            Assert.That(third.pigeonFoodTileId, Is.EqualTo(secondMeadow.id));
            Assert.That(third.foodChanges.Single(change =>
                change.tileId == secondMeadow.id).remaining, Is.Zero);
        }

        [Test]
        public void FlockMovesTogetherToFoodAndMovedLocationSurvivesSave()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var firstMeadow, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonTileId, Is.EqualTo(firstMeadow.id));
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var secondMeadow, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out var third), Is.True);
            Assert.That(third.pigeonFoodTileId, Is.EqualTo(firstMeadow.id));
            Assert.That(run.TrySimulateDay(out var fourth), Is.True);
            Assert.That(fourth.pigeonsFed, Is.EqualTo(2));
            Assert.That(run.PigeonTileId, Is.EqualTo(secondMeadow.id));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PigeonTileId, Is.EqualTo(secondMeadow.id));
        }

        [Test]
        public void StaggeredSecondMeadowImprovesSixDayObservedFeeding()
        {
            static int FedThroughDayEight(bool addSecondMeadow)
            {
                var board = new ConstructionBoardModel();
                Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                    GreenPlanting.Meadow, out var first, out _, 2), Is.True);
                if (addSecondMeadow)
                    Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                        GreenPlanting.Meadow, out _, out _, 3), Is.True);
                var food = new ConstructionFoodModel();
                var flockTileId = first.id;
                var fed = 0;
                for (var day = 1; day <= 8; day++)
                {
                    var result = food.SettleDay(day, board,
                        day >= 3 ? flockTileId : null);
                    if (result.pigeonFoodTileId != null)
                        flockTileId = result.pigeonFoodTileId;
                    fed += result.pigeonsFed;
                }
                return fed;
            }

            Assert.That(FedThroughDayEight(false), Is.EqualTo(6));
            Assert.That(FedThroughDayEight(true), Is.EqualTo(12));
        }

        [Test]
        public void FoodStockAndObservedDaySurviveSaveWithInvalidStockRejected()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.CurrentDay, Is.EqualTo(3));
            Assert.That(restored.PigeonTileId, Is.EqualTo(meadow.id));
            Assert.That(restored.FoodStock(meadow.id), Is.Zero);
            Assert.That(restored.LastEcologyDay.day, Is.EqualTo(2));

            var tampered = run.Export();
            tampered.food.tiles.Single(tile => tile.tileId == meadow.id).stock = 99;
            Assert.That(ConstructionRunModel.TryRestore(tampered, out _), Is.False);
        }

        [Test]
        public void LegacyScoredRunSurvivesUnityJsonRoundTrip()
        {
            var run = new ConstructionRunModel();
            run.RecordSquirrelMeal();
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var json = JsonUtility.ToJson(run.Export());
            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(json);
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.True, json);
        }
    }
}
