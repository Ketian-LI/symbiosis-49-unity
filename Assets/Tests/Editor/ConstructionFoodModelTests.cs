using System.Collections.Generic;
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
        public void FedAnimalRulesGiveOpeningOakSixPointsWithoutChangingOldRules()
        {
            var board = new ConstructionBoardModel();
            var food = new ConstructionFoodModel();
            var preview = food.PreviewDay(1, board, null,
                useLocalFoodAreaScore: true, useFedAnimalScore: true);
            Assert.That(preview.AnimalMeals, Is.EqualTo(1));
            Assert.That(preview.animalPoints, Is.EqualTo(6));
            Assert.That(preview.fedScoringSourceTileIds,
                Is.EqualTo(new[] { ConstructionBoardModel.StarterOakId }));
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1), "Preview must not consume the opening nut.");

            var settled = food.SettleDay(1, board, null,
                useLocalFoodAreaScore: true, useFedAnimalScore: true);
            Assert.That(settled.animalPoints, Is.EqualTo(preview.animalPoints));
            Assert.That(settled.scoredFoodTileIds,
                Is.EqualTo(new[] { ConstructionBoardModel.StarterOakId }),
                "The available-source record remains for resident linkage.");
            var oldRules = new ConstructionFoodModel().PreviewDay(1, board,
                null, useLocalFoodAreaScore: true);
            Assert.That(oldRules.animalPoints, Is.EqualTo(1));
        }

        [Test]
        public void FedAnimalRulesCountUsedFoodPlotsOnceAndGiveWasteNoSourceBonus()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, 1), Is.True);
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 2, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out _, 1), Is.True);
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Waste,
                GreenPlanting.None, out var waste, out _, 1), Is.True);
            var sources = new[]
            {
                new ConstructionUrbanFoodSource { tileId = market.id,
                    category = ConstructionCategory.Supermarket, portions = 1 },
                new ConstructionUrbanFoodSource { tileId = waste.id,
                    category = ConstructionCategory.Waste, portions = 1 }
            };
            var fed = new ConstructionFoodModel().SettleDay(1, board,
                meadow.id, 1, 2, null, sources,
                useLocalFoodAreaScore: true, useFedAnimalScore: true);
            Assert.That(fed.AnimalMeals, Is.EqualTo(3));
            Assert.That(fed.urbanMealTileIds,
                Is.EqualTo(new[] { market.id, waste.id }));
            Assert.That(fed.animalPoints, Is.EqualTo(15),
                "Three fed animals + the used oak and supermarket; waste gives no source bonus.");
            Assert.That(fed.fedScoringSourceTileIds,
                Is.EquivalentTo(new[] { ConstructionBoardModel.StarterOakId,
                    market.id }));

            var unused = new ConstructionFoodModel().SettleDay(1, board,
                meadow.id, 1, 2, null, null,
                useLocalFoodAreaScore: true, useFedAnimalScore: true);
            Assert.That(unused.AnimalMeals, Is.EqualTo(1));
            Assert.That(unused.animalPoints, Is.EqualTo(6),
                "Available plots must not score when no animal ate there.");
        }

        [Test]
        public void LocalFoodAreaScoreCountsDistinctNearbySourcesOncePerDay()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var eastBridge, out _), Is.True);
            Assert.That(board.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var westBridge, out _), Is.True);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out var cornerOak, out var failure),
                Is.True, failure.ToString());
            Assert.That(board.TryBuild(2, 2, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out failure),
                Is.True, failure.ToString());
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out failure,
                allowIntroPlacement: true), Is.True, failure.ToString());
            Assert.That(board.TryBuild(2, 4, ConstructionCategory.Waste,
                GreenPlanting.None, out var waste, out failure,
                allowIntroPlacement: true), Is.True, failure.ToString());
            Assert.That(board.TryBuild(6, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var distantSquare, out failure,
                allowIntroPlacement: true),
                Is.True, failure.ToString());
            var sources = new List<ConstructionUrbanFoodSource>
            {
                new() { tileId = square.id, category = ConstructionCategory.Square,
                    portions = 1 },
                new() { tileId = market.id,
                    category = ConstructionCategory.Supermarket, portions = 1 },
                new() { tileId = waste.id, category = ConstructionCategory.Waste,
                    portions = 1 },
                new() { tileId = distantSquare.id,
                    category = ConstructionCategory.Square, portions = 1 }
            };
            ConstructionFoodAreaScore Score(int animals) =>
                ConstructionFoodAreaScorer.Score(board, 1,
                    id => id == eastBridge.id || id == westBridge.id ? 0 : 1,
                    ConstructionBoardModel.StarterOakId, animals,
                    null, 0, null, sources);

            var one = Score(1);
            Assert.That(one.points, Is.EqualTo(10),
                "Oak 1 + corner Oak 1 + Square 4 + Market 3 + Waste 1.");
            Assert.That(one.sourceTileIds, Is.EquivalentTo(new[]
            {
                ConstructionBoardModel.StarterOakId, cornerOak.id,
                square.id, market.id, waste.id
            }));
            Assert.That(one.sourceTileIds, Does.Not.Contain(distantSquare.id),
                "A source two cells from every animal origin is outside the local area.");
            Assert.That(Score(3).points, Is.EqualTo(one.points),
                "More animals sharing a source do not multiply its area value.");
            Assert.That(Score(0).points, Is.Zero,
                "Food without a living animal origin cannot score.");
        }

        [Test]
        public void LocalFoodAreaScoreRequiresAvailableSpeciesFoodAndIsNotMealPoints()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _), Is.True);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out var failure),
                Is.True, failure.ToString());
            var squirrelOnly = ConstructionFoodAreaScorer.Score(board, 1,
                id => id == meadow.id ? 1 : 0,
                ConstructionBoardModel.StarterOakId, 1,
                null, 0, null, null);
            Assert.That(squirrelOnly.points, Is.Zero,
                "An empty Oak and Meadow without pigeons are unavailable food areas.");
            var pigeon = ConstructionFoodAreaScorer.Score(board, 1,
                id => id == meadow.id ? 1 : 0,
                ConstructionBoardModel.StarterOakId, 1,
                meadow.id, 2, null, null);
            Assert.That(pigeon.points, Is.EqualTo(1));
            Assert.That(pigeon.sourceTileIds, Is.EqualTo(new[] { meadow.id }));

            var food = new ConstructionFoodModel();
            var opening = food.PreviewDay(1, new ConstructionBoardModel(), null,
                useLocalFoodAreaScore: true);
            Assert.That(opening.AnimalMeals, Is.EqualTo(1));
            Assert.That(opening.animalPoints, Is.EqualTo(1));
            Assert.That(opening.scoredFoodTileIds,
                Is.EqualTo(new[] { ConstructionBoardModel.StarterOakId }));
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1), "Preview does not consume the opening nut.");
        }

        [Test]
        public void FoxHuntFollowsOldPreyOrderButRespectsConnectedCover()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var hedge, out _, 1), Is.True);
            Assert.That(board.TryBuild(3, 1, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var den, out _, 1), Is.True);
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, 1), Is.True);
            var first = ConstructionFoodModel.PlanFoxHunt(board, 4, den.id,
                meadow.id, 3, 1, hedge.id, true);
            Assert.That(first.species, Is.EqualTo(ConstructionPreySpecies.Pigeon));
            Assert.That(first.routeTileIds.Count, Is.EqualTo(4),
                "The fox must reach the meadow through the starter oak.");
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 1), Is.True);
            Assert.That(ConstructionFoodModel.PlanFoxHunt(board, 4, den.id,
                meadow.id, 3, 1, hedge.id, true).species,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(board.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 1), Is.True);
            Assert.That(ConstructionFoodModel.PlanFoxHunt(board, 4, den.id,
                meadow.id, 3, 1, hedge.id, true).species,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));
            Assert.That(board.TryBuild(2, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out _, out _, 1), Is.True);
            Assert.That(ConstructionFoodModel.PlanFoxHunt(board, 4, den.id,
                meadow.id, 3, 1, hedge.id, true).WillHunt, Is.False);
        }

        [Test]
        public void FoxKillMatchesPreviewAndPersistsWithoutInstantRespawn()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 6, row: 3, planting: GreenPlanting.Shrub)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.CurrentDay, Is.EqualTo(9));
            Assert.That(run.PigeonPopulation, Is.EqualTo(3));
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel),
                "The flock's current forage tile is out of fox range; the fox should fall back to the squirrel.");
            Assert.That(run.SquirrelPopulation, Is.EqualTo(1),
                "Inspecting a forecast must not consume a prey animal.");
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.predatedSpecies, Is.EqualTo(preview.predatedSpecies));
            Assert.That(actual.foxHuntRouteTileIds,
                Is.EqualTo(preview.foxHuntRouteTileIds));
            Assert.That(actual.AnimalMeals, Is.EqualTo(preview.AnimalMeals));
            Assert.That(run.SquirrelPopulation, Is.Zero);
            Assert.That(run.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.None),
                "The fox needs another hungry day before hunting again.");
            var json = JsonUtility.ToJson(run.Export());
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(json),
                out var restored), Is.True, json);
            Assert.That(restored.SquirrelPopulation, Is.Zero);
            Assert.That(restored.LastEcologyDay.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            var calmSave = restored.Export();
            calmSave.survival.foxReturnDay = 100;
            calmSave.survival.foxEstablished = false;
            Assert.That(ConstructionRunModel.TryRestore(calmSave, out var calm),
                Is.True, "Keep the predator away to isolate immigration timing.");
            var pending = calm.ReturnStatus(ConstructionWildlifeSpecies.Squirrel);
            Assert.That(pending.Pending, Is.True);
            Assert.That(pending.EarliestDay, Is.EqualTo(actual.day + 3));
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.SquirrelPopulation, Is.Zero,
                "The replacement cannot appear on the next day.");
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.CurrentDay, Is.EqualTo(actual.day + 3));
            Assert.That(calm.SquirrelPopulation, Is.EqualTo(1),
                "The existing oak can attract a new squirrel on day d+3.");
            Assert.That(calm.ReturnStatus(
                ConstructionWildlifeSpecies.Squirrel).Pending, Is.False);
        }

        [Test]
        public void ReachablePigeonFlockLosesOneBirdToFox()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow),
                (column: 2, row: 3, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True,
                "The third meadow must mature and bring the fox first.");
            Assert.That(run.FoxHabitat, Is.Not.Null);
            Assert.That(run.TrySimulateDay(out _), Is.True,
                "The fox's first habitat day should establish hunger, not hunt.");
            Assert.That(run.PigeonPopulation, Is.EqualTo(3));
            Assert.That(run.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Pigeon));
            Assert.That(run.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Pigeon));
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));
            Assert.That(ecology.pigeonsPresent, Is.EqualTo(2));
        }

        [Test]
        public void MaturingOaksDoNotEraseADeathBeforeReturnDay()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 2, row: 3, planting: GreenPlanting.Oak)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.CurrentDay, Is.EqualTo(9));
            Assert.That(run.TryBuild(3, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(run.TrySimulateDay(out var death), Is.True);
            Assert.That(run.SquirrelPopulation, Is.Zero);
            var saved = run.Export();
            saved.survival.foxReturnDay = 100;
            saved.survival.foxEstablished = false;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var calm),
                Is.True);
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.CurrentDay, Is.EqualTo(death.day + 2));
            Assert.That(calm.MatureNearbyOakCount, Is.EqualTo(3));
            Assert.That(calm.SquirrelPopulation, Is.Zero,
                "New habitat cannot cancel a still-active death cooldown.");
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.CurrentDay, Is.EqualTo(death.day + 3));
            Assert.That(calm.SquirrelPopulation, Is.GreaterThan(0));
        }

        [Test]
        public void ShelteredFlockAndSquirrelLeaveHedgehogAsPrey()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 3, row: 4, planting: GreenPlanting.Oak),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 4, row: 2, planting: GreenPlanting.Meadow)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var save = run.Export();
            save.pigeonTileId = run.At(4, 3).id;
            Assert.That(ConstructionRunModel.TryRestore(save, out var sheltered),
                Is.True, "A flock resting in a mature sheltered meadow is a valid state.");
            Assert.That(sheltered.HedgehogHabitat, Is.Not.Null);
            Assert.That(sheltered.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));
            Assert.That(sheltered.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Hedgehog));
            Assert.That(sheltered.HedgehogHabitat, Is.Null,
                "A caught hedgehog must not instantly reappear on the same shrubs.");
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(
                    JsonUtility.ToJson(sheltered.Export())), out var restored), Is.True);
            Assert.That(restored.HedgehogHabitat, Is.Null);
            var wait = restored.ReturnStatus(
                ConstructionWildlifeSpecies.Hedgehog);
            Assert.That(wait.Pending, Is.True);
            Assert.That(wait.EarliestDay, Is.EqualTo(ecology.day + 3));
            var calmSave = restored.Export();
            calmSave.survival.foxReturnDay = 100;
            calmSave.survival.foxEstablished = false;
            Assert.That(ConstructionRunModel.TryRestore(calmSave, out var calm),
                Is.True);
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.HedgehogHabitat, Is.Null);
            Assert.That(calm.TrySimulateDay(out _), Is.True);
            Assert.That(calm.CurrentDay, Is.EqualTo(ecology.day + 3));
            Assert.That(calm.HedgehogHabitat, Is.Not.Null,
                "After the wait, a new visitor can use the existing mature shrub.");
        }

        [Test]
        public void LastSquirrelKilledByFoxStartsEndlessRescueWindow()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var build in new[]
            {
                (column: 3, row: 2, planting: GreenPlanting.Shrub),
                (column: 3, row: 1, planting: GreenPlanting.Shrub),
                (column: 4, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 3, planting: GreenPlanting.Meadow),
                (column: 5, row: 2, planting: GreenPlanting.Meadow),
                (column: 6, row: 2, planting: GreenPlanting.Oak),
                (column: 6, row: 3, planting: GreenPlanting.Shrub)
            })
            {
                Assert.That(run.TryBuild(build.column, build.row,
                    ConstructionCategory.Green, build.planting,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var saved = run.Export();
            saved.mode = ConstructionGameMode.Endless;
            saved.survival.squirrels = 1;
            saved.survival.pigeons = 3;
            saved.survival.foxEstablished = true;
            saved.survival.foxHungryDays = 1;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var endless),
                Is.True);
            Assert.That(endless.PreviewEcologyDay().predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(endless.TrySimulateDay(out var ecology), Is.True);
            Assert.That(ecology.predatedSpecies,
                Is.EqualTo(ConstructionPreySpecies.Squirrel));
            Assert.That(endless.SquirrelPopulation, Is.Zero);
            Assert.That(endless.IsFinished, Is.False);
            Assert.That(endless.EndReason, Is.EqualTo(ConstructionEndReason.None));
            var wait = endless.ReturnStatus(ConstructionWildlifeSpecies.Squirrel);
            Assert.That(wait.Pending, Is.True);
            Assert.That(wait.EarliestDay, Is.EqualTo(ecology.day + 3));
            Assert.That(wait.RescueDeadlineDay, Is.EqualTo(ecology.day + 5));
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(
                    JsonUtility.ToJson(endless.Export())), out var restored), Is.True);
            Assert.That(restored.EndReason,
                Is.EqualTo(ConstructionEndReason.None));
            Assert.That(restored.ReturnStatus(
                    ConstructionWildlifeSpecies.Squirrel).EarliestDay,
                Is.EqualTo(ecology.day + 3));
        }

        [Test]
        public void LostEndlessFlockNeedsHabitatAndHasFiveDayRescueWindow()
        {
            var fill = LegacyFreeBuildRun();
            Assert.That(fill.TrySimulateDay(out _), Is.True);
            Assert.That(fill.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(fill.TrySimulateDay(out _), Is.True);
            Assert.That(fill.TryBuild(3, 4, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(fill.TrySimulateDay(out _), Is.True);
            var converted = fill.Export();
            converted.mode = ConstructionGameMode.Endless;
            converted.survival.squirrels = 1;
            converted.survival.pigeons = 2;
            Assert.That(ConstructionRunModel.TryRestore(converted, out var run),
                Is.True);
            Assert.That(run.TryBuild(4, 4, ConstructionCategory.Street,
                GreenPlanting.None, out _, out _, out _), Is.True);

            // Skip only quiet days; the fatal event itself uses the real
            // deterministic traffic planner and normal ecology settlement.
            ConstructionEcologyDayResult death = null;
            for (var attempt = 0; attempt < 40; attempt++)
            {
                if (run.PreviewEcologyDay().trafficSpecies ==
                    ConstructionTrafficSpecies.Pigeon)
                {
                    Assert.That(run.TrySimulateDay(out death), Is.True);
                    break;
                }
                Assert.That(run.TryEndDay(0, 0), Is.True);
            }
            Assert.That(death, Is.Not.Null,
                "An exposed two-bird flock should have a deterministic collision within this search range.");
            Assert.That(death.trafficSpecies,
                Is.EqualTo(ConstructionTrafficSpecies.Pigeon));
            Assert.That(run.PigeonPopulation, Is.Zero,
                "A lone survivor leaves the site in endless mode.");
            Assert.That(run.IsFinished, Is.False,
                "The flock must receive a rescue window rather than end the run immediately.");
            var pending = run.ReturnStatus(ConstructionWildlifeSpecies.Pigeon);
            Assert.That(pending.Pending, Is.True);
            Assert.That(pending.EarliestDay, Is.EqualTo(death.day + 3));
            Assert.That(pending.RescueDeadlineDay, Is.EqualTo(death.day + 5));
            Assert.That(pending.HabitatReady, Is.False,
                "One meadow cannot sustain a returning two-bird flock.");

            var save = JsonUtility.FromJson<ConstructionRunSaveData>(
                JsonUtility.ToJson(run.Export()));
            Assert.That(ConstructionRunModel.TryRestore(save, out var blocked),
                Is.True);
            Assert.That(blocked.ReturnStatus(
                    ConstructionWildlifeSpecies.Pigeon).EarliestDay,
                Is.EqualTo(death.day + 3));
            while (blocked.CurrentDay <= death.day + 5 && !blocked.IsFinished)
            {
                var settlingDay = blocked.CurrentDay;
                Assert.That(blocked.TrySimulateDay(out _), Is.True);
                if (settlingDay < death.day + 5)
                    Assert.That(blocked.IsFinished, Is.False,
                        "The full five-day grace period should be playable.");
            }
            Assert.That(blocked.IsFinished, Is.True);
            Assert.That(blocked.EndReason,
                Is.EqualTo(ConstructionEndReason.WildlifeGone));

            Assert.That(ConstructionRunModel.TryRestore(save, out var rescued),
                Is.True);
            Assert.That(rescued.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            while (rescued.CurrentDay < death.day + 3)
            {
                Assert.That(rescued.PigeonPopulation, Is.Zero);
                Assert.That(rescued.TrySimulateDay(out _), Is.True);
                Assert.That(rescued.IsFinished, Is.False);
            }
            Assert.That(rescued.CurrentDay, Is.EqualTo(death.day + 3));
            Assert.That(rescued.PigeonPopulation, Is.EqualTo(2),
                "A connected second meadow should allow a new flock on d+3.");
            Assert.That(rescued.ReturnStatus(
                ConstructionWildlifeSpecies.Pigeon).Pending, Is.False);
        }

        [Test]
        public void LaterWildlifeNeedsMatureConnectedShrubsAndEstablishedPigeons()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var firstShrub, out _, 2), Is.True);
            Assert.That(board.TryBuild(3, 1, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var secondShrub, out _, 3), Is.True);
            Assert.That(ConstructionFoodModel.HedgehogHabitat(board, 3), Is.Null,
                "A newly placed shrub must not attract an animal immediately.");
            Assert.That(ConstructionFoodModel.HedgehogHabitat(board, 4)?.id,
                Is.EqualTo(firstShrub.id));
            Assert.That(ConstructionFoodModel.FoxHabitat(board, 5, 2), Is.Null,
                "A fox needs established prey as well as shelter.");
            Assert.That(ConstructionFoodModel.FoxHabitat(board, 5, 3)?.id,
                Is.EqualTo(secondShrub.id));
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var isolated, out _), Is.True,
                "A non-green room can physically connect a shrub without joining the oak habitat.");
            Assert.That(board.ConnectedGreenCount(isolated.id), Is.EqualTo(1));
            Assert.That(ConstructionFoodModel.HedgehogHabitat(board, 6)?.id,
                Is.EqualTo(firstShrub.id),
                "A green room reached only through a residence is not part of this habitat.");
        }

        [Test]
        public void StarterOakFeedsExactlyOneSquirrelAndCannotSettleTwice()
        {
            var board = new ConstructionBoardModel();
            var food = new ConstructionFoodModel();
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId), Is.EqualTo(1));

            var first = food.SettleDay(1, board, null);
            Assert.That(first.AnimalMeals, Is.EqualTo(1));
            Assert.That(first.animalPoints, Is.EqualTo(3));
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
        public void V6HarvestChanceStartsOnDayTenForMatureGreenOnly()
        {
            var oak = new ConstructionBoardModel().At(
                ConstructionBoardModel.StarterColumn,
                ConstructionBoardModel.StarterRow);
            var meadow = new ConstructionTileData
            {
                id = "built-002", category = ConstructionCategory.Green,
                planting = GreenPlanting.Meadow, builtDay = 1
            };
            var shrub = new ConstructionTileData
            {
                id = "built-003", category = ConstructionCategory.Green,
                planting = GreenPlanting.Shrub, builtDay = 1
            };
            var youngMeadow = new ConstructionTileData
            {
                id = "built-004", category = ConstructionCategory.Green,
                planting = GreenPlanting.Meadow, builtDay = 9
            };

            Assert.That(ConstructionFoodModel.DailyYield(oak, 9, true,
                "v6-test-run"), Is.EqualTo(1));
            Assert.That(ConstructionFoodModel.DailyYield(youngMeadow, 10,
                true, "v6-test-run"), Is.EqualTo(2),
                "A first-stage meadow keeps its tutorial yield.");
            Assert.That(ConstructionFoodModel.DailyYield(oak, 30),
                Is.EqualTo(1), "Old direct callers retain the V5 rule.");
            Assert.That(ConstructionFoodModel.DailyYield(oak, 30, true,
                "v6-test-run"), Is.Zero);
            Assert.That(ConstructionFoodModel.DailyYield(oak, 30, true,
                "another-run"), Is.EqualTo(1),
                "A new run must not replay the same harvest sequence.");
            Assert.That(ConstructionFoodModel.DailyYield(meadow, 16),
                Is.EqualTo(2));
            Assert.That(ConstructionFoodModel.DailyYield(meadow, 16, true,
                "v6-test-run"), Is.Zero,
                "A failed harvest loses the whole normal meadow yield.");
            Assert.That(ConstructionFoodModel.DailyYield(shrub, 16, true,
                "v6-test-run"), Is.Zero);
            Assert.That(Enumerable.Range(10, 200).Count(day =>
                ConstructionFoodModel.DailyYield(oak, day, true,
                    "v6-test-run") == 1), Is.EqualTo(164),
                "The fixed draw sequence should stay reproducible and near 80%.");
        }

        [Test]
        public void V6DryDayPreviewMatchesSettlementAndLeavesUrbanFoodAvailable()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out _, 1), Is.True);
            var food = new ConstructionFoodModel();
            for (var day = 1; day < 30; day++)
                food.SettleDay(day, board, null,
                    useV6YieldRule: true, runSeed: "v6-test-run");
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId),
                Is.Zero);
            var scraps = new[]
            {
                new ConstructionUrbanFoodSource
                {
                    tileId = square.id, category = ConstructionCategory.Square,
                    portions = 1
                }
            };

            var preview = food.PreviewDay(30, board, null,
                urbanSources: scraps, useV6YieldRule: true,
                runSeed: "v6-test-run");
            Assert.That(preview.foodChanges.Single(change =>
                change.tileId == ConstructionBoardModel.StarterOakId).produced,
                Is.Zero);
            Assert.That(preview.squirrelsFed, Is.EqualTo(1));
            Assert.That(preview.urbanMealTileIds,
                Is.EqualTo(new[] { square.id }));
            Assert.That(preview.animalPoints, Is.EqualTo(2));
            Assert.That(food.LastSettledDay, Is.EqualTo(29));
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId),
                Is.Zero, "A preview must never spend or produce real food.");

            var actual = food.SettleDay(30, board, null,
                urbanSources: scraps, useV6YieldRule: true,
                runSeed: "v6-test-run");
            Assert.That(actual.foodChanges.Single(change =>
                change.tileId == ConstructionBoardModel.StarterOakId).produced,
                Is.EqualTo(preview.foodChanges.Single(change =>
                    change.tileId == ConstructionBoardModel.StarterOakId).produced));
            Assert.That(actual.urbanMealTileIds,
                Is.EqualTo(preview.urbanMealTileIds));
            Assert.That(actual.animalPoints, Is.EqualTo(preview.animalPoints));
        }

        [Test]
        public void TwoNearbyMatureOaksAttractSecondSquirrelOnlyWhenFoodIsEaten()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 2), Is.True);
            var food = new ConstructionFoodModel();
            for (var day = 1; day < 4; day++)
                food.SettleDay(day, board, null);
            var preview = food.PreviewDay(4, board, null);
            Assert.That(preview.squirrelsPresent, Is.EqualTo(2));
            Assert.That(ConstructionFoodModel.MatureNearbyOakCount(board, 4),
                Is.EqualTo(3));
            Assert.That(preview.squirrelsFed, Is.EqualTo(2));
            Assert.That(preview.squirrelFoodTileIds.Count, Is.EqualTo(2));
            var actual = food.SettleDay(4, board, null);
            Assert.That(actual.AnimalMeals, Is.EqualTo(2));
            Assert.That(actual.squirrelFoodTileIds,
                Is.EqualTo(preview.squirrelFoodTileIds));
        }

        [Test]
        public void EightSurroundingCellsCanStillAttractMoreSquirrelsAfterFirstMeadow()
        {
            var board = new ConstructionBoardModel();
            Assert.That(ConstructionFoodModel.MaximumNearbyOakCount(board),
                Is.EqualTo(9));
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 2), Is.True);
            Assert.That(ConstructionFoodModel.MaximumNearbyOakCount(board),
                Is.EqualTo(8));
            foreach (var (column, row) in new[]
                     { (3, 2), (2, 3), (3, 4) })
                Assert.That(board.TryBuild(column, row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, 2), Is.True);

            Assert.That(ConstructionFoodModel.MatureNearbyOakCount(board, 5),
                Is.EqualTo(4));
            Assert.That(ConstructionFoodModel.SquirrelPopulation(board, 5),
                Is.EqualTo(2));
            Assert.That(ConstructionFoodModel.OaksForNextSquirrel(2),
                Is.LessThanOrEqualTo(ConstructionFoodModel.MaximumNearbyOakCount(board)),
                "The UI can offer a third squirrel while more nearby oak sites remain.");
        }

        [Test]
        public void MatureCornerOaksContributeToSquirrelPopulationWhenConnected()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _), Is.True);
            Assert.That(board.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _), Is.True);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out var firstFailure, 1),
                Is.True, firstFailure.ToString());
            Assert.That(board.TryBuild(2, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out var secondFailure, 1),
                Is.True, secondFailure.ToString());
            Assert.That(ConstructionFoodModel.MatureNearbyOakCount(board, 3),
                Is.EqualTo(3));
            Assert.That(ConstructionFoodModel.SquirrelPopulation(board, 3),
                Is.EqualTo(2));
        }

        [Test]
        public void OccupiedSecondaryOakCanReachAndScoreFoodBeyondStarterOak()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out var occupiedOak, out _, 1), Is.True);
            Assert.That(board.TryBuild(4, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out var outerFoodOak, out _, 1), Is.True);
            foreach (var (column, row) in new[]
                     { (3, 2), (2, 3), (3, 4) })
                Assert.That(board.TryBuild(column, row,
                    ConstructionCategory.Green, GreenPlanting.Oak,
                    out _, out _, 1), Is.True);
            Assert.That(ConstructionBoardModel.GridDistance(3, 3, 5, 4),
                Is.EqualTo(2));
            Assert.That(ConstructionBoardModel.GridDistance(
                occupiedOak.column, occupiedOak.row,
                outerFoodOak.column, outerFoodOak.row), Is.EqualTo(1));
            Assert.That(ConstructionFoodModel.SquirrelPopulation(board, 3),
                Is.EqualTo(3));
            Assert.That(ConstructionFoodModel.SquirrelHomeTiles(board, 3, 3)
                .Select(tile => tile.id), Does.Contain(occupiedOak.id));

            var food = new ConstructionFoodModel();
            food.SettleDay(1, board, null, useLocalFoodAreaScore: true);
            food.SettleDay(2, board, null, useLocalFoodAreaScore: true);
            var day3 = food.SettleDay(3, board, null,
                useLocalFoodAreaScore: true);
            Assert.That(board.GreenRouteBetween(occupiedOak.id,
                    outerFoodOak.id).Count, Is.EqualTo(3),
                "The occupied secondary oak reaches food by an edge-connected route.");
            Assert.That(day3.scoredFoodTileIds, Does.Contain(outerFoodOak.id),
                "The same available outer food area counts once for animal score.");
            Assert.That(day3.scoredFoodTileIds.Distinct().Count(),
                Is.EqualTo(day3.scoredFoodTileIds.Count));
        }

        [Test]
        public void V7SquirrelsCannotShareFoodOnlyNearOneOccupiedOak()
        {
            var board = LocalSquirrelBoard();
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out var firstOuter, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, out var secondOuter, out _, 1), Is.True);
            var dry = board.BuiltTiles.Where(tile =>
                tile.category == ConstructionCategory.Green &&
                ConstructionBoardModel.GridDistance(tile.column, tile.row,
                    3, 3) <= 1).ToArray();
            var wet = new[] { firstOuter, secondOuter };
            var seed = DayTenSeed(dry, wet);
            var current = FoodWithEmptyDayTenStock(board);

            var v7 = current.PreviewDay(10, board, null,
                useV6YieldRule: true, runSeed: seed,
                useLocalFoodAreaScore: true);
            Assert.That(v7.squirrelsPresent, Is.EqualTo(2));
            Assert.That(v7.squirrelsFed, Is.EqualTo(1),
                "Only the squirrel in the neighbouring occupied oak can eat either outer source.");
            Assert.That(v7.squirrelFoodTileIds.Count, Is.EqualTo(1));
            Assert.That(v7.squirrelMealHomeTileIds,
                Is.EqualTo(new[] { board.At(4, 3).id }),
                "The compact meal list must retain its actual squirrel home for route animation.");
            Assert.That(v7.scoredFoodTileIds, Does.Contain(firstOuter.id));
            Assert.That(v7.scoredFoodTileIds, Does.Contain(secondOuter.id),
                "Both available local food areas score once even if only one is eaten.");
            var legacy = current.PreviewDay(10, board, null,
                useV6YieldRule: true, runSeed: seed);
            Assert.That(legacy.squirrelsFed, Is.EqualTo(2),
                "Historical saves retain their collective feeding order.");
        }

        [Test]
        public void V7UrbanFoodFeedsOnlyItsNearbySquirrel()
        {
            var board = LocalSquirrelBoard();
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out _), Is.True);
            var seed = DayTenSeed(board.BuiltTiles.Where(tile =>
                tile.category == ConstructionCategory.Green).ToArray(),
                System.Array.Empty<ConstructionTileData>());
            var current = FoodWithEmptyDayTenStock(board);
            var sources = new[]
            {
                new ConstructionUrbanFoodSource
                {
                    tileId = square.id, category = ConstructionCategory.Square,
                    portions = 2
                }
            };

            var v7 = current.PreviewDay(10, board, null,
                urbanSources: sources, useV6YieldRule: true, runSeed: seed,
                useLocalFoodAreaScore: true);
            Assert.That(v7.squirrelsPresent, Is.EqualTo(2));
            Assert.That(v7.squirrelsFed, Is.EqualTo(1));
            Assert.That(v7.urbanMealTileIds, Is.EqualTo(new[] { square.id }));
            Assert.That(v7.squirrelMealHomeTileIds,
                Is.EqualTo(new[] { board.At(4, 3).id }));
            Assert.That(v7.animalPoints, Is.EqualTo(4),
                "The available square scores once, independently of its two portions.");
            var legacy = current.PreviewDay(10, board, null,
                urbanSources: sources, useV6YieldRule: true, runSeed: seed);
            Assert.That(legacy.squirrelsFed, Is.EqualTo(2));
            Assert.That(legacy.urbanMealTileIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void ThreeMatureMeadowsGrowOneFlockAndUseMoreThanOneFoodTile()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var first, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 2), Is.True);
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 3), Is.True);
            var food = new ConstructionFoodModel();
            for (var day = 1; day < 5; day++)
                food.SettleDay(day, board, day >= 2 ? first.id : null);
            var preview = food.PreviewDay(5, board, first.id);
            Assert.That(preview.pigeonsPresent, Is.EqualTo(3));
            Assert.That(ConstructionFoodModel.MatureConnectedMeadowCount(
                board, 5, first.id), Is.EqualTo(3));
            Assert.That(preview.pigeonsFed, Is.EqualTo(3));
            Assert.That(preview.pigeonFoodTileIds.Count, Is.GreaterThan(1));
            Assert.That(preview.pigeonMealTileIds.Count,
                Is.EqualTo(preview.pigeonsFed),
                "Each fed pigeon needs its own settled food destination.");
            var actual = food.SettleDay(5, board, first.id);
            Assert.That(actual.pigeonFoodTileIds,
                Is.EqualTo(preview.pigeonFoodTileIds));
            Assert.That(actual.pigeonMealTileIds,
                Is.EqualTo(preview.pigeonMealTileIds));
            Assert.That(actual.foodChanges.Sum(change => change.eaten),
                Is.EqualTo(actual.AnimalMeals));
        }

        [Test]
        public void GrowingAnimalPopulationSurvivesRunSaveAndForecast()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.SquirrelPopulation, Is.EqualTo(2));
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.squirrelsFed, Is.EqualTo(2));
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.AnimalMeals, Is.EqualTo(preview.AnimalMeals));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.LastEcologyDay.squirrelsPresent, Is.EqualTo(2));
            Assert.That(restored.LastForecast.animalMeals, Is.EqualTo(2));
        }

        [Test]
        public void ExpandedPigeonFlockSurvivesRunSaveAndForecast()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var position in new[] { (4, 3), (5, 3), (4, 2) })
            {
                Assert.That(run.TryBuild(position.Item1, position.Item2,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(3));
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.pigeonsFed, Is.EqualTo(3));
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.pigeonsPresent, Is.EqualTo(3));
            Assert.That(actual.pigeonsFed, Is.EqualTo(3));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.LastEcologyDay.pigeonsFed, Is.EqualTo(3));
            Assert.That(restored.LastForecast.animalMeals,
                Is.EqualTo(actual.AnimalMeals));
        }

        [Test]
        public void LegacyPigeonLossMigratesWithoutChangingVisiblePopulation()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            foreach (var position in new[] { (4, 3), (5, 3), (4, 2) })
            {
                Assert.That(run.TryBuild(position.Item1, position.Item2,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.PigeonPopulation, Is.EqualTo(3));
            var expectedExisting = run.PigeonPopulation - 2;
            var oldSave = run.Export();
            // This migration case predates variable green harvests. Keep its
            // historical food rule while checking the return schedule.
            oldSave.score.scoringVersion = 5;
            oldSave.wildlifeReturnRulesVersion = 0;
            oldSave.survival.predatedPigeons = 2;
            var dueDay = oldSave.currentDay + 3;
            Assert.That(ConstructionRunModel.TryRestore(oldSave, out var migrated),
                Is.True);
            Assert.That(migrated.PigeonPopulation, Is.EqualTo(expectedExisting),
                "Loading an old save must not instantly create replacement pigeons.");
            Assert.That(migrated.ReturnStatus(
                    ConstructionWildlifeSpecies.Pigeon).EarliestDay,
                Is.EqualTo(dueDay));
            Assert.That(migrated.TrySimulateDay(out _), Is.True);
            var waitingJson = JsonUtility.ToJson(migrated.Export());
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(waitingJson),
                out var resumed), Is.True);
            Assert.That(resumed.PigeonPopulation, Is.EqualTo(expectedExisting));
            while (resumed.CurrentDay < dueDay)
            {
                Assert.That(resumed.TrySimulateDay(out _), Is.True);
                if (resumed.CurrentDay < dueDay)
                    Assert.That(resumed.PigeonPopulation,
                        Is.EqualTo(expectedExisting));
            }
            Assert.That(resumed.PigeonPopulation, Is.EqualTo(expectedExisting + 1),
                "At most one newcomer should appear on the first eligible day.");
            Assert.That(resumed.TrySimulateDay(out _), Is.True);
            Assert.That(resumed.PigeonPopulation, Is.EqualTo(3),
                "The second historical loss may be refilled on a later day.");
        }

        [Test]
        public void AnimalPreviewPredictsSettlementWithoutSpendingFood()
        {
            var board = new ConstructionBoardModel();
            var food = new ConstructionFoodModel();
            var preview = food.PreviewDay(1, board, null);
            Assert.That(preview.squirrelAte, Is.True);
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId),
                Is.EqualTo(1));
            Assert.That(food.LastSettledDay, Is.Zero);
            var actual = food.SettleDay(1, board, null);
            Assert.That(actual.squirrelFoodTileId,
                Is.EqualTo(preview.squirrelFoodTileId));
            Assert.That(actual.AnimalMeals, Is.EqualTo(preview.AnimalMeals));
        }

        [Test]
        public void DailyForecastAndObservedOutcomeSurviveReloadSeparately()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(run.LastForecast.day, Is.EqualTo(1));
            Assert.That(run.LastForecast.animalMeals,
                Is.EqualTo(actual.AnimalMeals));
            Assert.That(run.LastForecast.completedWorkCycles,
                Is.EqualTo(run.LastHumanDay.CompletedWorkCycles));
            Assert.That(run.LastForecast.wasteCleared,
                Is.EqualTo(run.LastWasteDay.Cleared));
            var json = JsonUtility.ToJson(run.Export());
            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(json);
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(restored.LastForecast.animalMeals, Is.EqualTo(1));
            Assert.That(restored.LastEcologyDay.AnimalMeals, Is.EqualTo(1));

            saved.lastForecast = null;
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var olderSave), Is.True);
            Assert.That(olderSave.LastForecast, Is.Null);

            var tampered = run.Export();
            tampered.lastForecast.animalMeals = -1;
            Assert.That(ConstructionRunModel.TryRestore(tampered, out _), Is.False);
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
            Assert.That(third.scoredFoodTileIds, Does.Contain(meadow.id));
            Assert.That(third.animalPoints, Is.EqualTo(2),
                "Both the available oak and meadow count once as nearby food areas.");
            Assert.That(run.AnimalScore, Is.EqualTo(4));
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.LastEcologyDay.pigeonsFed, Is.Zero,
                "The meadow is recovering; no food means no pigeon meals.");
            Assert.That(run.LastEcologyDay.scoredFoodTileIds,
                Does.Not.Contain(meadow.id));
            Assert.That(run.AnimalScore, Is.EqualTo(5));
        }

        [Test]
        public void UrbanFoodAwardsTwoOrOneOnlyWhenSourceIsPresentAndReachable()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, 1), Is.True);
            Assert.That(board.TryBuild(4, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(5, 2, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out _, 1), Is.True);
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Waste,
                GreenPlanting.None, out var waste, out _, 1), Is.True);
            var sources = new[]
            {
                new ConstructionUrbanFoodSource { tileId = market.id,
                    category = ConstructionCategory.Supermarket, portions = 1 },
                new ConstructionUrbanFoodSource { tileId = waste.id,
                    category = ConstructionCategory.Waste, portions = 1 }
            };
            var food = new ConstructionFoodModel();
            var preview = food.PreviewDay(1, board, meadow.id, 1, 2, null, sources);
            Assert.That(preview.AnimalMeals, Is.EqualTo(3));
            Assert.That(preview.animalPoints, Is.EqualTo(6),
                "Oak 3 + supermarket 2 + collected waste 1.");
            Assert.That(preview.urbanMealTileIds,
                Is.EqualTo(new[] { market.id, waste.id }));
            Assert.That(food.Stock(ConstructionBoardModel.StarterOakId), Is.EqualTo(1),
                "Preview must not spend natural food.");
            var actual = food.SettleDay(1, board, meadow.id, 1, 2, null, sources);
            Assert.That(actual.animalPoints, Is.EqualTo(preview.animalPoints));
            Assert.That(actual.pigeonMealTileIds, Is.EqualTo(preview.pigeonMealTileIds));

            var withoutActivity = new ConstructionFoodModel().SettleDay(1, board,
                meadow.id, 1, 2);
            Assert.That(withoutActivity.pigeonsFed, Is.Zero);
            Assert.That(withoutActivity.animalPoints, Is.EqualTo(3));
            var unconnected = new ConstructionFoodModel().SettleDay(1, board,
                meadow.id, 1, 2, null, new[]
                {
                    new ConstructionUrbanFoodSource { tileId = market.id,
                        category = ConstructionCategory.Waste, portions = 1 }
                });
            Assert.That(unconnected.pigeonsFed, Is.Zero,
                "A source with the wrong room type is not edible.");
        }

        [Test]
        public void SquareFoodNeedsActualPortionsAndAnAnimalRoute()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Supermarket,
                GreenPlanting.None, out _, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(5, 3, ConstructionCategory.Street,
                GreenPlanting.None, out _, out _, 1,
                allowIntroPlacement: true), Is.True);
            Assert.That(board.TryBuild(6, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var square, out _, 1,
                allowIntroPlacement: true), Is.True);
            var source = new ConstructionUrbanFoodSource
            {
                tileId = square.id, category = ConstructionCategory.Square,
                portions = 2
            };

            var emptySquare = new ConstructionFoodModel().SettleDay(1, board,
                null, 3, 0);
            Assert.That(emptySquare.urbanMealTileIds, Is.Empty);
            Assert.That(emptySquare.animalPoints, Is.EqualTo(3),
                "A plaza is not an automatic source of food.");

            var fed = new ConstructionFoodModel().SettleDay(1, board,
                null, 3, 0, null, new[] { source });
            Assert.That(fed.squirrelsFed, Is.EqualTo(3));
            Assert.That(fed.urbanMealTileIds,
                Is.EqualTo(new[] { square.id, square.id }));
            Assert.That(fed.animalPoints, Is.EqualTo(7),
                "An eaten plaza portion scores 2, after the oak's 3 points.");

            var blocked = new ConstructionBoardModel();
            Assert.That(blocked.TryBuild(4, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, 1), Is.True);
            Assert.That(blocked.TryBuild(5, 3, ConstructionCategory.Square,
                GreenPlanting.None, out var unreachable, out _, 1), Is.True);
            var inaccessible = new ConstructionFoodModel().SettleDay(1,
                blocked, null, 3, 0, null, new[]
                {
                    new ConstructionUrbanFoodSource
                    {
                        tileId = unreachable.id,
                        category = ConstructionCategory.Square, portions = 2
                    }
                });
            Assert.That(inaccessible.urbanMealTileIds, Is.Empty);
            Assert.That(inaccessible.animalPoints, Is.EqualTo(3),
                "Plaza food cannot be collected through a residence.");
        }

        [Test]
        public void MatureShrubFeedsHedgehogButFoxHuntDoesNotAwardAnimalPoints()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(3, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub, out var shrub, out _, 1), Is.True);
            var food = new ConstructionFoodModel();
            var first = food.SettleDay(1, board, null, 1, 0, shrub.id);
            Assert.That(first.hedgehogFed, Is.False);
            Assert.That(food.SettleDay(2, board, null, 1, 0, shrub.id)
                .hedgehogFed, Is.False);
            var third = food.SettleDay(3, board, null, 1, 0, shrub.id);
            Assert.That(third.hedgehogFed, Is.True);
            Assert.That(third.hedgehogFoodTileId, Is.EqualTo(shrub.id));
            Assert.That(third.animalPoints, Is.EqualTo(6));
            third.predatedSpecies = ConstructionPreySpecies.Pigeon;
            Assert.That(third.AnimalMeals, Is.EqualTo(2));
            Assert.That(third.LegacyAnimalMeals, Is.EqualTo(3));
            Assert.That(third.animalPoints, Is.EqualTo(6));
        }

        [Test]
        public void SupermarketScrapsNeedARealServedMealAndSurviveRunSave()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var meadow, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 2, ConstructionCategory.Supermarket,
                GreenPlanting.None, out var market, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(2, 2, ConstructionCategory.Waste,
                GreenPlanting.None, out _, out _, out _), Is.True,
                "Cleanup must unblock the home before a real market meal exists.");
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.pigeonMealTileIds, Does.Contain(market.id));
            Assert.That(preview.urbanMealTileIds, Does.Contain(market.id));
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.urbanMealTileIds,
                Is.EqualTo(preview.urbanMealTileIds));
            Assert.That(run.LastWasteDay.changes.Single(change =>
                change.tileId == market.id).foraged, Is.EqualTo(1));
            Assert.That(run.AnimalScore,
                Is.EqualTo(run.Export().score.days.Sum(day => day.animalPoints)));
            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(
                JsonUtility.ToJson(run.Export()));
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True, JsonUtility.ToJson(saved));
            Assert.That(restored.LastEcologyDay.animalPoints,
                Is.EqualTo(actual.animalPoints));
        }

        [Test]
        public void VersionZeroSaveKeepsOldMealScoreAndAcceptsMissingUrbanHistory()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var saved = run.Export();
            // Reconstruct the fields an actual V0 save contains. Changing
            // only the version number would leave V7 linkage/traffic flags
            // and area points in a record that V0 never produced.
            saved.score.scoringVersion = 0;
            saved.score.days[0].animalPoints = 0;
            saved.score.days[0].humanPoints = 0;
            saved.score.days[0].balancePoints = 0;
            saved.score.days[0].linkageRulesApplied = false;
            saved.score.days[0].trafficPenaltyRulesApplied = false;
            saved.lastEcologyDay.urbanMealTileIds = null;
            saved.lastEcologyDay.scoredFoodTileIds = null;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored), Is.True);
            Assert.That(restored.UsesLegacyAnimalScoring, Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(1));
            Assert.That(restored.TrySimulateDay(out _), Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(2));
        }

        [Test]
        public void CollectedWasteCanFeedAdjacentFlockForOnePoint()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TryBuild(3, 2, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(2, 2, ConstructionCategory.Waste,
                GreenPlanting.None, out var waste, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.urbanMealTileIds, Does.Contain(waste.id));
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.urbanMealTileIds, Is.EqualTo(preview.urbanMealTileIds));
            Assert.That(actual.scoredFoodTileIds, Does.Contain(waste.id));
            Assert.That(actual.animalPoints, Is.EqualTo(2),
                "Available Oak area 1 + collected Waste area 1.");
            Assert.That(run.LastWasteDay.Cleared, Is.GreaterThan(0));
            Assert.That(run.PigeonTileId, Is.Not.EqualTo(waste.id));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out _), Is.True);
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
            var run = LegacyFreeBuildRun();
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
        public void FlockRoutePreviewMatchesActualTargetWithoutChangingStock()
        {
            var run = LegacyFreeBuildRun();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var first, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Assert.That(run.TryBuild(5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out var second, out _, out _), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var stockBefore = run.FoodStock(second.id);
            var preview = run.PreviewEcologyDay();
            Assert.That(preview.pigeonFoodTileId, Is.EqualTo(second.id));
            Assert.That(run.PreviewPigeonFoodRoute().Select(tile => tile.id),
                Is.EqualTo(new[] { first.id, second.id }));
            Assert.That(run.FoodStock(second.id), Is.EqualTo(stockBefore));
            Assert.That(run.TrySimulateDay(out var actual), Is.True);
            Assert.That(actual.pigeonFoodTileId,
                Is.EqualTo(preview.pigeonFoodTileId));
            Assert.That(actual.pigeonsFed, Is.EqualTo(preview.pigeonsFed));
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
            var run = LegacyFreeBuildRun();
            run.RecordSquirrelMeal();
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.TryEndDay(1, 0), Is.True);
            var json = JsonUtility.ToJson(run.Export());
            var saved = JsonUtility.FromJson<ConstructionRunSaveData>(json);
            Assert.That(ConstructionRunModel.TryRestore(saved, out _), Is.True, json);
        }

        private static ConstructionBoardModel LocalSquirrelBoard()
        {
            var board = new ConstructionBoardModel();
            Assert.That(board.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 1), Is.True);
            Assert.That(board.TryBuild(2, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, 1), Is.True);
            Assert.That(ConstructionFoodModel.SquirrelPopulation(board, 10),
                Is.EqualTo(2));
            return board;
        }

        private static string DayTenSeed(
            IReadOnlyList<ConstructionTileData> dry,
            IReadOnlyList<ConstructionTileData> wet)
        {
            for (var index = 0; index < 100000; index++)
            {
                var seed = "local-squirrel-" + index;
                if (dry.All(tile => ConstructionFoodModel.DailyYield(tile,
                        10, true, seed) == 0) &&
                    wet.All(tile => ConstructionFoodModel.DailyYield(tile,
                        10, true, seed) > 0)) return seed;
            }
            Assert.Fail("Could not find a deterministic day-10 dry/wet seed.");
            return string.Empty;
        }

        private static ConstructionFoodModel FoodWithEmptyDayTenStock(
            ConstructionBoardModel board)
        {
            var saved = new ConstructionFoodSaveData
            {
                lastSettledDay = 9,
                tiles = board.BuiltTiles.Where(tile =>
                    tile.category == ConstructionCategory.Green)
                    .Select(tile => new ConstructionFoodTileSaveData
                    {
                        tileId = tile.id, stock = 0
                    }).ToList()
            };
            Assert.That(ConstructionFoodModel.TryRestore(saved, board, 10,
                false, out var food), Is.True);
            return food;
        }

        // Ecology scenarios with repeated early green construction predate the
        // current eight-room introduction. Loading a version-zero run keeps
        // their original free-build sequence while exercising today's ecology.
        private static ConstructionRunModel LegacyFreeBuildRun()
        {
            var saved = new ConstructionRunModel().Export();
            saved.introRulesVersion = 0;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run), Is.True);
            return run;
        }
    }
}
