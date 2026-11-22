using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionLinkageScoreTests
    {
        [Test]
        public void TwoFedWildlifeSpeciesNearACompletedHomeAwardTwoResidentPoints()
        {
            var board = new ConstructionBoardModel();
            var oak = board.At(ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            var meadow = Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var home = Build(board, 4, 2, ConstructionCategory.Residence);
            var ecology = new ConstructionEcologyDayResult
            {
                day = 3,
                squirrelsPresent = 1,
                squirrelsFed = 1,
                squirrelMealHomeTileIds = new List<string> { oak.id },
                pigeonsPresent = 2,
                pigeonsFed = 1,
                pigeonHabitatTileId = meadow.id
            };
            var human = new ConstructionHumanDayResult
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                    { Resident(home.id) }
            };

            var result = ConstructionLinkageScorer.Apply(board, 3, ecology,
                human, useLocalFoodAreaScore: true,
                useBiodiversityBonus: true);

            Assert.That(result.biodiversityHomes, Is.EqualTo(1));
            Assert.That(result.LinkagePoints, Is.EqualTo(2));
            Assert.That(human.residents[0].biodiversityBonus, Is.EqualTo(2));
            Assert.That(human.residents[0].biodiversityHabitatTileIds,
                Is.EqualTo(new[] { oak.id, meadow.id }));
            Assert.That(human.SatisfactionPoints, Is.EqualTo(3));
            Assert.That(human.LinkagePoints, Is.EqualTo(2));

            var oldRules = new ConstructionHumanDayResult
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                    { Resident(home.id) }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 3, ecology,
                oldRules, useLocalFoodAreaScore: true).LinkagePoints, Is.Zero);
        }

        [Test]
        public void BiodiversityRequiresTwoFedSpeciesACompletedCycleAndNearbyHabitats()
        {
            var board = new ConstructionBoardModel();
            var oak = board.At(ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            var meadow = Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var nearby = Build(board, 4, 2, ConstructionCategory.Residence);
            var farAway = Build(board, 6, 1, ConstructionCategory.Residence);
            ConstructionEcologyDayResult Ecology(int pigeonFed) => new()
            {
                day = 3,
                squirrelsPresent = 1,
                squirrelsFed = 1,
                squirrelMealHomeTileIds = new List<string> { oak.id },
                pigeonsPresent = 2,
                pigeonsFed = pigeonFed,
                pigeonHabitatTileId = meadow.id
            };
            ConstructionHumanDayResult Human(string homeId,
                bool completed = true) => new()
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                {
                    new() { homeTileId = homeId, completedCycle = completed,
                        satisfactionPoints = completed ? 1 : 0 }
                }
            };

            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                Ecology(0), Human(nearby.id), useBiodiversityBonus: true)
                .biodiversityHomes, Is.Zero);
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                Ecology(1), Human(farAway.id), useBiodiversityBonus: true)
                .biodiversityHomes, Is.Zero);
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                Ecology(1), Human(nearby.id, false),
                useBiodiversityBonus: true).biodiversityHomes, Is.Zero);
        }

        [Test]
        public void BiodiversityRewardsAtMostTwoHomesPerDayWithoutStealingSharedFood()
        {
            var board = new ConstructionBoardModel();
            var oak = board.At(ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            var meadow = Build(board, 3, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var homes = new[]
            {
                Build(board, 2, 3, ConstructionCategory.Residence),
                Build(board, 4, 3, ConstructionCategory.Residence),
                Build(board, 2, 4, ConstructionCategory.Residence)
            };
            var human = new ConstructionHumanDayResult
            {
                day = 3,
                residents = homes.Select(home => Resident(home.id)).ToList()
            };
            var ecology = new ConstructionEcologyDayResult
            {
                day = 3,
                squirrelsPresent = 1,
                squirrelsFed = 1,
                squirrelMealHomeTileIds = new List<string> { oak.id },
                pigeonsPresent = 2,
                pigeonsFed = 1,
                pigeonHabitatTileId = meadow.id,
                scoredFoodTileIds = new List<string> { oak.id }
            };

            var result = ConstructionLinkageScorer.Apply(board, 3, ecology,
                human, useLocalFoodAreaScore: true,
                useBiodiversityBonus: true);
            Assert.That(result.biodiversityHomes, Is.EqualTo(2));
            Assert.That(result.sharedGreenMeals, Is.EqualTo(1));
            Assert.That(result.LinkagePoints, Is.EqualTo(5));
            Assert.That(human.residents.Count(person =>
                person.biodiversityBonus == 2), Is.EqualTo(2));
            Assert.That(human.residents.Single(person =>
                person.linkageBonus == 1).linkageSourceTileId,
                Is.EqualTo(oak.id));
        }

        [Test]
        public void BiodiversityLedgerPersistsV8AndRejectsV7AwardOrTampering()
        {
            var current = new ConstructionScoreLedger();
            Assert.That(current.HasBiodiversityScore, Is.True);
            Assert.That(current.TryRecordDay(1, 2, 1, 3, 3,
                biodiversityHomes: 1), Is.True);
            Assert.That(current.LastDay.biodiversityHomes, Is.EqualTo(1));
            Assert.That(current.FinalResult().BiodiversityHomesTotal,
                Is.EqualTo(1));
            Assert.That(current.FinalResult().LinkagePointsTotal,
                Is.EqualTo(2));
            Assert.That(current.BalanceScore,
                Is.EqualTo(current.AnimalScore + current.HumanScore));
            Assert.That(ConstructionScoreLedger.TryRestore(current.Export(),
                out var restored), Is.True);
            Assert.That(restored.HumanScore, Is.EqualTo(3));
            var tampered = current.Export();
            tampered.days[0].biodiversityHomes = 2;
            Assert.That(ConstructionScoreLedger.TryRestore(tampered, out _),
                Is.False);

            var old = new ConstructionScoreLedger(7);
            Assert.That(old.TryRecordDay(1, 2, 1, 3, 1), Is.True);
            var oldSave = old.Export();
            Assert.That(ConstructionScoreLedger.TryRestore(oldSave, out _),
                Is.True);
            oldSave.days[0].biodiversityHomes = 1;
            oldSave.days[0].linkagePoints = 2;
            Assert.That(ConstructionScoreLedger.TryRestore(oldSave, out _),
                Is.False);
        }

        [Test]
        public void LocalFoodAreaLinksDiagonalHomeOnceWithoutAnAnimalMeal()
        {
            var board = new ConstructionBoardModel();
            var green = Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak);
            var diagonalHome = Build(board, 5, 2,
                ConstructionCategory.Residence);
            var otherHome = Build(board, 4, 2,
                ConstructionCategory.Residence);
            var ecology = new ConstructionEcologyDayResult
            {
                day = 3,
                scoredFoodTileIds = new List<string> { green.id }
            };
            ConstructionHumanDayResult People() => new()
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                {
                    Resident(diagonalHome.id), Resident(otherHome.id)
                }
            };

            var historical = ConstructionLinkageScorer.Apply(board, 3,
                ecology, People());
            Assert.That(historical.LinkagePoints, Is.Zero,
                "Earlier scoring versions still require an actual meal.");
            var current = People();
            var awarded = ConstructionLinkageScorer.Apply(board, 3,
                ecology, current, useLocalFoodAreaScore: true);
            Assert.That(awarded.sharedGreenMeals, Is.EqualTo(1));
            Assert.That(awarded.LinkagePoints, Is.EqualTo(1));
            Assert.That(current.residents.Sum(person => person.linkageBonus),
                Is.EqualTo(1), "One scored green source cannot pay two homes.");
        }

        [Test]
        public void ThreeObservedInteractionsAreIncludedOnceInResidentScore()
        {
            var board = new ConstructionBoardModel();
            var squareVisitor = Build(board, 4, 2, ConstructionCategory.Residence);
            var marketDiner = Build(board, 2, 3, ConstructionCategory.Residence);
            var greenNeighbour = Build(board, 3, 1, ConstructionCategory.Residence);
            var square = Build(board, 4, 3, ConstructionCategory.Square);
            var market = Build(board, 5, 2, ConstructionCategory.Supermarket);
            var green = Build(board, 3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak);
            var waste = Build(board, 2, 2, ConstructionCategory.Waste);
            var human = new ConstructionHumanDayResult
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                {
                    Resident(squareVisitor.id, mealId: market.id,
                        squareId: square.id),
                    Resident(marketDiner.id, mealId: market.id),
                    Resident(greenNeighbour.id)
                }
            };
            var ecology = new ConstructionEcologyDayResult
            {
                day = 3,
                urbanMealTileIds = new List<string>
                    { square.id, market.id, waste.id },
                squirrelFoodTileIds = new List<string> { green.id }
            };

            var awarded = ConstructionLinkageScorer.Apply(board, 3,
                ecology, human);

            Assert.That(awarded.sharedSquareMeals, Is.EqualTo(1));
            Assert.That(awarded.sharedSupermarketMeals, Is.EqualTo(1));
            Assert.That(awarded.sharedGreenMeals, Is.EqualTo(1));
            Assert.That(awarded.LinkagePoints, Is.EqualTo(6));
            Assert.That(human.SatisfactionPoints, Is.EqualTo(9),
                "Three base cycle points plus six included linkage points.");
            Assert.That(human.residents.Select(person => person.linkageBonus),
                Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(human.residents[0].linkageKind,
                Is.EqualTo(ConstructionLinkageKind.SharedSquare),
                "The same diner cannot receive the market award too.");
            Assert.That(human.residents[0].linkageSourceTileId,
                Is.EqualTo(square.id));
        }

        [Test]
        public void NoActualMealOrIncompleteCycleMeansNoLinkage()
        {
            var board = new ConstructionBoardModel();
            var home = Build(board, 4, 2, ConstructionCategory.Residence);
            var square = Build(board, 4, 3, ConstructionCategory.Square);
            var human = new ConstructionHumanDayResult
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                    { Resident(home.id, squareId: square.id) }
            };
            var noMeal = ConstructionLinkageScorer.Apply(board, 3,
                new ConstructionEcologyDayResult { day = 3 }, human);
            Assert.That(noMeal.LinkagePoints, Is.Zero);
            Assert.That(human.SatisfactionPoints, Is.EqualTo(1));

            var incomplete = new ConstructionHumanDayResult
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                {
                    new() { homeTileId = home.id, visitedSquare = true,
                        squareTileId = square.id }
                }
            };
            var ate = new ConstructionEcologyDayResult
            {
                day = 3,
                urbanMealTileIds = new List<string> { square.id }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                ate, incomplete).LinkagePoints, Is.Zero);
        }

        [Test]
        public void WasteMealsNeverAwardLinkageAndOneMarketPortionCreditsOneDiner()
        {
            var board = new ConstructionBoardModel();
            var first = Build(board, 4, 2, ConstructionCategory.Residence);
            var second = Build(board, 2, 3, ConstructionCategory.Residence);
            var market = Build(board, 5, 2, ConstructionCategory.Supermarket);
            var waste = Build(board, 2, 2, ConstructionCategory.Waste);
            ConstructionHumanDayResult Diners() => new()
            {
                day = 3,
                residents = new List<ConstructionResidentDayResult>
                {
                    Resident(first.id, mealId: market.id),
                    Resident(second.id, mealId: market.id)
                }
            };
            var wasteOnly = new ConstructionEcologyDayResult
            {
                day = 3,
                urbanMealTileIds = new List<string> { waste.id }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                wasteOnly, Diners()).LinkagePoints, Is.Zero);

            var oneMarketPortion = new ConstructionEcologyDayResult
            {
                day = 3,
                urbanMealTileIds = new List<string> { market.id }
            };
            var human = Diners();
            var awarded = ConstructionLinkageScorer.Apply(board, 3,
                oneMarketPortion, human);
            Assert.That(awarded.sharedSupermarketMeals, Is.EqualTo(1));
            Assert.That(awarded.LinkagePoints, Is.EqualTo(2));
            Assert.That(human.residents.Count(person =>
                person.linkageBonus == 2), Is.EqualTo(1));
        }

        [Test]
        public void GreenMealPortionsAreMatchedOnceAndMustBeMature()
        {
            var board = new ConstructionBoardModel();
            var green = Build(board, 3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak);
            var first = Build(board, 4, 2, ConstructionCategory.Residence);
            var second = Build(board, 3, 1, ConstructionCategory.Residence);
            ConstructionHumanDayResult TwoHomes(int day) => new()
            {
                day = day,
                residents = new List<ConstructionResidentDayResult>
                    { Resident(first.id), Resident(second.id) }
            };
            var oneMeal = new ConstructionEcologyDayResult
            {
                day = 3,
                squirrelFoodTileIds = new List<string> { green.id }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                oneMeal, TwoHomes(3)).sharedGreenMeals, Is.EqualTo(1));

            var twoMeals = new ConstructionEcologyDayResult
            {
                day = 3,
                squirrelFoodTileIds = new List<string> { green.id },
                pigeonMealTileIds = new List<string> { green.id }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 3,
                twoMeals, TwoHomes(3)).sharedGreenMeals, Is.EqualTo(2));

            var immature = new ConstructionEcologyDayResult
            {
                day = 1,
                squirrelFoodTileIds = new List<string> { green.id }
            };
            Assert.That(ConstructionLinkageScorer.Apply(board, 1,
                immature, TwoHomes(1)).LinkagePoints, Is.Zero);
        }

        [Test]
        public void DailyCapStopsAdditionalSquareAwards()
        {
            var board = new ConstructionBoardModel();
            var homes = new[]
            {
                Build(board, 2, 3, ConstructionCategory.Residence),
                Build(board, 3, 2, ConstructionCategory.Residence),
                Build(board, 4, 3, ConstructionCategory.Residence)
            };
            var squares = new[]
            {
                Build(board, 1, 3, ConstructionCategory.Square),
                Build(board, 3, 1, ConstructionCategory.Square),
                Build(board, 5, 3, ConstructionCategory.Square)
            };
            var human = new ConstructionHumanDayResult
            {
                day = 3,
                residents = homes.Select((home, index) =>
                    Resident(home.id, squareId: squares[index].id)).ToList()
            };
            var ecology = new ConstructionEcologyDayResult
            {
                day = 3,
                urbanMealTileIds = squares.Select(tile => tile.id).ToList()
            };
            var awarded = ConstructionLinkageScorer.Apply(board, 3,
                ecology, human);
            Assert.That(awarded.sharedSquareMeals, Is.EqualTo(2));
            Assert.That(awarded.LinkagePoints, Is.EqualTo(6));
            Assert.That(human.residents.Count(person =>
                person.linkageBonus > 0), Is.EqualTo(2));
        }

        [Test]
        public void MarketPreviewMatchesSettlementAndSurvivesRunRestore()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 2, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(2, 2, ConstructionCategory.Waste,
                GreenPlanting.None, out _, out _, out _), Is.True);
            var preview = run.PreviewHumanDay();
            Assert.That(run.PreviewEcologyDay().urbanMealTileIds,
                Does.Contain(market.id));
            Assert.That(preview.residents.Single().linkageBonus,
                Is.EqualTo(2));
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastHumanDay.SatisfactionPoints,
                Is.EqualTo(preview.SatisfactionPoints));
            Assert.That(run.LastForecast.humanPoints,
                Is.EqualTo(preview.SatisfactionPoints));
            Assert.That(run.Export().score.days.Last().sharedSupermarketMeals,
                Is.EqualTo(1));
            Assert.That(run.BalanceScore,
                Is.EqualTo(run.AnimalScore + run.HumanScore));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.LastHumanDay.residents.Single().linkageBonus,
                Is.EqualTo(2));
        }

        [Test]
        public void ActiveVersionThreeRunUpgradesOnlyFutureDays()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var saved = run.Export();
            saved.score.scoringVersion = 3;
            saved.score.days[0].linkageRulesApplied = false;

            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(saved.score.scoringVersion, Is.EqualTo(3),
                "Restore must not mutate the source save.");
            Assert.That(restored.HasLinkageScore, Is.True);
            Assert.That(restored.Export().score.days[0].balancePoints,
                Is.EqualTo(1));
            Assert.That(restored.Export().score.days[0].linkageRulesApplied,
                Is.False);
            Assert.That(restored.TryEndDay(1, 0), Is.True);
            Assert.That(restored.Export().score.days[1].linkageRulesApplied,
                Is.False, "Synthetic compatibility days are not observed linkage days.");
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var roundTrip), Is.True);
            Assert.That(roundTrip.AnimalScore, Is.EqualTo(2));
            Assert.That(roundTrip.BalanceScore,
                Is.EqualTo(roundTrip.AnimalScore + roundTrip.HumanScore));
        }

        [Test]
        public void EmptyVersionThreeSaveAdoptsVersionFourWithoutSecondUpgrade()
        {
            var saved = new ConstructionRunModel(
                ConstructionGameMode.Endless).Export();
            saved.score.scoringVersion = 3;
            Assert.That(saved.score.days, Is.Empty);
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(restored.HasLinkageScore, Is.True);
            Assert.That(restored.Export().score.scoringVersion,
                Is.EqualTo(ConstructionScoreLedger.CurrentScoringVersion));
        }

        [Test]
        public void FinishedRunReturnsSettledHumanDayWithoutPreviewingFoodAgain()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 1), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            for (var day = 0; day < 3; day++)
                Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.EndReason,
                Is.EqualTo(ConstructionEndReason.ResidentsUnemployed));
            Assert.That(run.PreviewHumanDay(), Is.SameAs(run.LastHumanDay));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.PreviewHumanDay(),
                Is.SameAs(restored.LastHumanDay));
        }

        [Test]
        public void SyntheticDayIsNotCountedAsLinkageTracking()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var saved = run.Export();
            Assert.That(saved.score.days[0].linkageRulesApplied, Is.False);
            Assert.That(ConstructionScoreLedger.TryRestore(saved.score,
                out var score), Is.True);
            Assert.That(score.FinalResult().LinkageTrackedDays, Is.Zero);
        }

        private static ConstructionResidentDayResult Resident(string homeId,
            string mealId = null, string squareId = null) => new()
        {
            homeTileId = homeId,
            mealTileId = mealId,
            squareTileId = squareId,
            completedCycle = true,
            ate = mealId != null,
            visitedSquare = squareId != null,
            basePoints = 1,
            satisfactionPoints = 1
        };

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(board.TryBuild(column, row, category, planting,
                out var tile, out _, 1, allowIntroPlacement: true), Is.True);
            return tile;
        }
    }
}
