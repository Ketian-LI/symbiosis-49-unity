using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UnityEngine;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionTrafficModelTests
    {
        [Test]
        public void NoStreetNeverProducesATrafficDeath()
        {
            var board = new ConstructionBoardModel();
            for (var day = 1; day <= 64; day++)
                Assert.That(Plan(board, day).Happened, Is.False, $"day {day}");
        }

        [Test]
        public void BuiltStreetWithoutAnimalsNeverProducesADeath()
        {
            var board = RoadBesideStarterOak(out _);
            for (var day = 1; day <= 64; day++)
                Assert.That(ConstructionTrafficModel.PlanDay(board, day,
                    0, 0, null, null, null, ConstructionPreySpecies.None)
                    .Happened, Is.False, $"day {day}");
        }

        [Test]
        public void AStreetExposesDiagonallyAdjacentWildlifeButNotTwoCellsAway()
        {
            var diagonal = new ConstructionBoardModel();
            Build(diagonal, 4, 3, ConstructionCategory.Residence);
            var diagonalStreet = Build(diagonal, 4, 2, ConstructionCategory.Street);
            var diagonalCollision = Plan(diagonal, FirstCollisionDay(diagonal));
            Assert.That(diagonalCollision.Happened, Is.True);
            Assert.That(diagonalCollision.streetTileId, Is.EqualTo(diagonalStreet.id));
            Assert.That(diagonalCollision.wildlifeTileId,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));

            var distant = new ConstructionBoardModel();
            Build(distant, 4, 3, ConstructionCategory.Residence);
            Build(distant, 5, 3, ConstructionCategory.Street);
            for (var day = 1; day <= 64; day++)
                Assert.That(Plan(distant, day).Happened, Is.False,
                    $"The oak is two columns from the street on day {day}.");

            var adjacent = RoadBesideStarterOak(out var street);
            var collision = Plan(adjacent, FirstCollisionDay(adjacent));
            Assert.That(collision.Happened, Is.True);
            Assert.That(collision.streetTileId, Is.EqualTo(street.id));
            Assert.That(collision.wildlifeTileId,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(collision.species, Is.EqualTo(ConstructionTrafficSpecies.Squirrel));
        }

        [Test]
        public void SameBoardAndDayGiveTheSameCollisionAfterSerialization()
        {
            var board = RoadBesideStarterOak(out _);
            var day = FirstCollisionDay(board);
            var before = Plan(board, day);
            Assert.That(Plan(board, day).streetTileId, Is.EqualTo(before.streetTileId));
            Assert.That(Plan(board, day).species, Is.EqualTo(before.species));

            var json = JsonUtility.ToJson(board.Export());
            Assert.That(ConstructionBoardModel.TryRestore(
                JsonUtility.FromJson<ConstructionBoardSaveData>(json),
                out var restored), Is.True);
            var after = Plan(restored, day);
            Assert.That(after.streetTileId, Is.EqualTo(before.streetTileId));
            Assert.That(after.wildlifeTileId, Is.EqualTo(before.wildlifeTileId));
            Assert.That(after.species, Is.EqualTo(before.species));
        }

        [Test]
        public void PlazaProtectedStreetHalvesRiskWithoutAddingCollisions()
        {
            var board = RoadBesideStarterOak(out var street);
            var protectedStreets = new[] { street.id };
            var ordinaryDeaths = 0;
            var protectedDeaths = 0;
            for (var day = 1; day <= 2048; day++)
            {
                var ordinary = Plan(board, day);
                var guarded = Plan(board, day, protectedStreets);
                if (ordinary.Happened) ordinaryDeaths++;
                if (!guarded.Happened) continue;
                protectedDeaths++;
                Assert.That(ordinary.Happened, Is.True,
                    $"Protection added a death on day {day}.");
                Assert.That(guarded.streetTileId, Is.EqualTo(street.id));
                Assert.That(guarded.wildlifeTileId,
                    Is.EqualTo(ordinary.wildlifeTileId));
            }

            Assert.That(protectedDeaths, Is.GreaterThan(0));
            Assert.That(protectedDeaths, Is.LessThan(ordinaryDeaths - 10),
                "An active square should have a visible protective effect over time.");
        }

        [Test]
        public void SlowStreetMatchesPlazaProtectionWithoutStackingAndOnlyAfterGreenIsBuilt()
        {
            var board = RoadBesideStarterOak(out var street);
            const int upgradeDay = 129;
            Assert.That(board.TryBuild(3, 1, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out var failure, upgradeDay),
                Is.True, failure.ToString());
            var protectedStreets = new[] { street.id };
            var avoidedCollision = false;
            var keptCollision = false;
            for (var day = 1; day <= 512; day++)
            {
                var ordinary = Plan(board, day);
                var plaza = Plan(board, day, protectedStreets);
                var slow = Plan(board, day, useNeighborUpgrades: true);
                var both = Plan(board, day, protectedStreets, useNeighborUpgrades: true);
                Assert.That(slow.Happened,
                    Is.EqualTo(day < upgradeDay ? ordinary.Happened : plaza.Happened),
                    $"Upgrade protection differs on day {day}.");
                Assert.That(both.Happened, Is.EqualTo(plaza.Happened),
                    $"Protection stacked on day {day}.");
                if (day < upgradeDay) continue;
                avoidedCollision |= ordinary.Happened && !slow.Happened;
                keptCollision |= slow.Happened;
            }

            Assert.That(avoidedCollision, Is.True);
            Assert.That(keptCollision, Is.True, "A slow street still carries risk.");
        }

        [Test]
        public void ProtectionOfAnotherTileDoesNotAffectExposedStreet()
        {
            var board = RoadBesideStarterOak(out _);
            for (var day = 1; day <= 128; day++)
            {
                var ordinary = Plan(board, day);
                var guarded = Plan(board, day,
                    new[] { ConstructionBoardModel.StarterOakId });
                Assert.That(guarded.species, Is.EqualTo(ordinary.species),
                    $"day {day}");
                Assert.That(guarded.streetTileId,
                    Is.EqualTo(ordinary.streetTileId), $"day {day}");
            }
        }

        [Test]
        public void ProtectedStreetForecastIsStableAfterSerialization()
        {
            var board = RoadBesideStarterOak(out var street);
            var protectedStreets = new[] { street.id };
            var json = JsonUtility.ToJson(board.Export());
            Assert.That(ConstructionBoardModel.TryRestore(
                JsonUtility.FromJson<ConstructionBoardSaveData>(json),
                out var restored), Is.True);
            for (var day = 1; day <= 128; day++)
            {
                var before = Plan(board, day, protectedStreets);
                var after = Plan(restored, day, protectedStreets);
                Assert.That(after.species, Is.EqualTo(before.species),
                    $"day {day}");
                Assert.That(after.streetTileId, Is.EqualTo(before.streetTileId),
                    $"day {day}");
                Assert.That(after.wildlifeTileId,
                    Is.EqualTo(before.wildlifeTileId), $"day {day}");
            }
        }

        [Test]
        public void TwoAdjacentStreetsSelectOnlyOneCollisionForOneAnimal()
        {
            var board = new ConstructionBoardModel();
            // Extend the oak first, then connect both street branches to a
            // home. Their only exposed wildlife is still the central squirrel.
            Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Build(board, 4, 2, ConstructionCategory.Residence);
            var north = Build(board, 3, 2, ConstructionCategory.Street);
            Build(board, 2, 2, ConstructionCategory.Residence);
            var west = Build(board, 2, 3, ConstructionCategory.Street);
            var incident = Plan(board, FirstCollisionDay(board));

            Assert.That(incident.species, Is.EqualTo(ConstructionTrafficSpecies.Squirrel));
            Assert.That(incident.wildlifeTileId,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
            Assert.That(new[] { north.id, west.id }, Does.Contain(incident.streetTileId));
        }

        [Test]
        public void EightExposedAnimalsDoNotForceACollisionEveryDay()
        {
            var board = RoadBesideStarterOak(out _);
            var quietDays = 0;
            for (var day = 1; day <= 64; day++)
                if (!ConstructionTrafficModel.PlanDay(board, day,
                        8, 0, null, null, null, ConstructionPreySpecies.None)
                    .Happened)
                    quietDays++;
            Assert.That(quietDays, Is.GreaterThan(0),
                "Sequential animal IDs must not create a guaranteed death every day.");
        }

        [Test]
        public void FoxPreyCannotAlsoDieInTrafficOnTheSameDay()
        {
            var board = RoadBesideStarterOak(out _);
            for (var day = 1; day <= 64; day++)
                Assert.That(ConstructionTrafficModel.PlanDay(board, day,
                    1, 0, null, null, null, ConstructionPreySpecies.Squirrel)
                    .Happened, Is.False, $"day {day}");
        }

        [TestCase(ConstructionTrafficSpecies.Pigeon)]
        [TestCase(ConstructionTrafficSpecies.Hedgehog)]
        [TestCase(ConstructionTrafficSpecies.Fox)]
        public void EveryOtherWildlifeSpeciesCanBeARealVictim(
            ConstructionTrafficSpecies species)
        {
            var board = RoadBesideStarterOak(out var street);
            var meadow = Build(board, 3, 1, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            var shrub = Build(board, 2, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            var habitat = species == ConstructionTrafficSpecies.Pigeon
                ? meadow.id : shrub.id;
            for (var day = 1; day <= 128; day++)
            {
                var collision = ConstructionTrafficModel.PlanDay(board, day,
                    0, species == ConstructionTrafficSpecies.Pigeon ? 2 : 0,
                    species == ConstructionTrafficSpecies.Pigeon ? meadow.id : null,
                    species == ConstructionTrafficSpecies.Hedgehog ? shrub.id : null,
                    species == ConstructionTrafficSpecies.Fox ? shrub.id : null,
                    ConstructionPreySpecies.None);
                if (!collision.Happened) continue;
                Assert.That(collision.species, Is.EqualTo(species));
                Assert.That(collision.streetTileId, Is.EqualTo(street.id));
                Assert.That(collision.wildlifeTileId, Is.EqualTo(habitat));
                return;
            }
            Assert.Fail($"No deterministic collision found for {species}.");
        }

        [Test]
        public void EndlessDayPreviewSettlementAndSavedStreetTotalAgree()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var street = Build(run, 3, 2, ConstructionCategory.Street);

            for (var extraDay = 0; extraDay < 64; extraDay++)
            {
                var preview = run.PreviewEcologyDay();
                var scoreBefore = run.AnimalScore;
                Assert.That(run.TrySimulateConstructionDay(out var settled), Is.True);
                Assert.That(settled.trafficSpecies, Is.EqualTo(preview.trafficSpecies));
                Assert.That(settled.trafficStreetTileId,
                    Is.EqualTo(preview.trafficStreetTileId));
                Assert.That(settled.trafficWildlifeTileId,
                    Is.EqualTo(preview.trafficWildlifeTileId));
                Assert.That(settled.AnimalMeals, Is.EqualTo(preview.AnimalMeals));
                Assert.That(settled.animalPoints, Is.EqualTo(preview.animalPoints));
                var expectedPenalty = settled.trafficSpecies ==
                    ConstructionTrafficSpecies.None ? 0 : 3;
                Assert.That(run.AnimalScore - scoreBefore,
                    Is.EqualTo(settled.animalPoints - expectedPenalty));
                Assert.That(run.LastScoreDay.trafficPenaltyPoints,
                    Is.EqualTo(expectedPenalty));

                if (settled.trafficSpecies == ConstructionTrafficSpecies.Squirrel)
                {
                    Assert.That(settled.squirrelsPresent, Is.Zero,
                        "An animal killed before foraging must not eat or score that day.");
                    Assert.That(run.SquirrelPopulation, Is.Zero);
                    Assert.That(run.StreetTrafficDeathCount(street.id), Is.EqualTo(1));
                    Assert.That(run.IsFinished, Is.False,
                        "The five-day rescue period must keep the next day playable.");
                    Assert.That(run.CurrentDay, Is.EqualTo(settled.day + 1));
                    var saved = run.Export();
                    Assert.That(saved.food.lastSettledDay,
                        Is.EqualTo(settled.day),
                        "The fatal collision must settle only its own day.");
                    Assert.That(saved.score.days[saved.score.days.Count - 1].day,
                        Is.EqualTo(settled.day));
                    var json = JsonUtility.ToJson(saved);
                    Assert.That(ConstructionRunModel.TryRestore(
                        JsonUtility.FromJson<ConstructionRunSaveData>(json),
                        out var restored), Is.True, json);
                    Assert.That(restored.StreetTrafficDeathCount(street.id), Is.EqualTo(1));
                    Assert.That(restored.SquirrelPopulation, Is.Zero);
                    Assert.That(restored.LastEcologyDay.trafficSpecies,
                        Is.EqualTo(ConstructionTrafficSpecies.Squirrel));
                    Assert.That(restored.AnimalScore, Is.EqualTo(run.AnimalScore));
                    Assert.That(restored.LastScoreDay.trafficPenaltyPoints,
                        Is.EqualTo(3));
                    var lastScore = saved.score.days[
                        saved.score.days.Count - 1];
                    lastScore.trafficPenaltyRulesApplied = false;
                    lastScore.trafficPenaltyPoints = 0;
                    lastScore.balancePoints += 3;
                    Assert.That(ConstructionRunModel.TryRestore(saved,
                        out _), Is.False,
                        "A saved collision cannot be relabeled as an unpenalized day.");
                    return;
                }

                Assert.That(run.StreetTrafficDeathCount(street.id), Is.Zero);
                Assert.That(run.IsFinished, Is.False,
                    "The oak should sustain its squirrel until the collision.");
                Build(run, 4 + extraDay, 1, ConstructionCategory.Square);
            }

            Assert.Fail("The adjacent street produced no collision in 64 settled days.");
        }

        private static ConstructionTrafficCollision Plan(ConstructionBoardModel board,
            int day, string[] protectedStreets = null, bool useNeighborUpgrades = false) =>
            ConstructionTrafficModel.PlanDay(board, day,
                1, 0, null, null, null, ConstructionPreySpecies.None,
                protectedStreets, useNeighborUpgrades: useNeighborUpgrades);

        private static int FirstCollisionDay(ConstructionBoardModel board)
        {
            for (var day = 1; day <= 128; day++)
                if (Plan(board, day).Happened) return day;
            Assert.Fail("A roadside squirrel had no reproducible collision in 128 days.");
            return -1;
        }

        private static ConstructionBoardModel RoadBesideStarterOak(
            out ConstructionTileData street)
        {
            var board = new ConstructionBoardModel();
            // The green bridge keeps this test's street cardinally adjacent
            // to the central oak, so diagonal exposure is tested separately.
            Build(board, 4, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Build(board, 4, 2, ConstructionCategory.Residence);
            street = Build(board, 3, 2, ConstructionCategory.Street);
            return board;
        }

        private static ConstructionTileData Build(ConstructionBoardModel board,
            int column, int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(board.TryBuild(column, row, category,
                planting, out var tile, out var failure), Is.True,
                failure.ToString());
            return tile;
        }

        private static ConstructionTileData Build(ConstructionRunModel run,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category,
                GreenPlanting.None, out var tile, out var failure,
                out var placement), Is.True, $"{failure}: {placement}");
            return tile;
        }
    }
}
