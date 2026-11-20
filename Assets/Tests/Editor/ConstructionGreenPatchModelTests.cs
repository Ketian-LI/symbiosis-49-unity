using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionGreenPatchModelTests
    {
        [Test]
        public void SeparatePatchesHaveIndependentSpeciesCapacitiesAndMergeByEdge()
        {
            var board = new ConstructionBoardModel();
            Build(board, 3, 4, ConstructionCategory.Green,
                GreenPlanting.Oak);
            Build(board, 2, 4, ConstructionCategory.Green,
                GreenPlanting.Oak);
            Build(board, 4, 3, ConstructionCategory.Residence);
            Build(board, 5, 3, ConstructionCategory.Street);
            Build(board, 5, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Build(board, 6, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            Build(board, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            Build(board, 6, 2, ConstructionCategory.Green,
                GreenPlanting.Shrub);

            var patches = ConstructionGreenPatchModel.Find(board, 4);
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches[0].SquirrelCapacity, Is.EqualTo(2));
            Assert.That(patches[0].PigeonCapacity, Is.Zero);
            Assert.That(patches[1].SquirrelCapacity, Is.Zero);
            Assert.That(patches[1].PigeonCapacity, Is.EqualTo(2));
            Assert.That(patches[1].HedgehogCapacity, Is.EqualTo(1));
            Assert.That(patches[1].FoxCapacity, Is.EqualTo(1));
            Assert.That(ConstructionGreenPatchModel.SquirrelHomes(board, 4, 2)
                .Count, Is.EqualTo(2));
            Assert.That(ConstructionGreenPatchModel.PigeonGroups(board, 4, 2)
                .Single().Count, Is.EqualTo(2));

            Build(board, 4, 4, ConstructionCategory.Green,
                GreenPlanting.Oak, day: 5);
            Assert.That(ConstructionGreenPatchModel.Find(board, 4).Count,
                Is.EqualTo(2), "A future connector does not join past-day patches.");
            Assert.That(ConstructionGreenPatchModel.Find(board, 7).Count,
                Is.EqualTo(1));
        }

        [Test]
        public void DigitalHandSaveCarriesPatchRuleVersion()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Assert.That(run.UsesGreenPatches, Is.True);
            var saved = run.Export();
            Assert.That(saved.greenPatchRulesVersion, Is.EqualTo(1));
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var restored), Is.True);
            Assert.That(restored.UsesGreenPatches, Is.True);
            saved.greenPatchRulesVersion = 0;
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var legacy), Is.True);
            Assert.That(legacy.UsesGreenPatches, Is.False);
        }

        [Test]
        public void IndependentMeadowAndShrubPatchesFeedTheirOwnAnimals()
        {
            var board = new ConstructionBoardModel();
            var firstMeadow = BuildTile(board, 3, 4,
                ConstructionCategory.Green, GreenPlanting.Meadow);
            BuildTile(board, 2, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            BuildTile(board, 2, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            BuildTile(board, 4, 3, ConstructionCategory.Residence);
            BuildTile(board, 5, 3, ConstructionCategory.Square);
            var secondMeadow = BuildTile(board, 5, 4,
                ConstructionCategory.Green, GreenPlanting.Meadow);
            BuildTile(board, 6, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            BuildTile(board, 6, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);

            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ConstructionRunModel).GetField("board", flags)
                .SetValue(run, board);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 4);
            typeof(ConstructionRunModel).GetProperty("PigeonTileId")
                .SetValue(run, firstMeadow.id);
            typeof(ConstructionRunModel).GetProperty("PigeonArrivalDay")
                .SetValue(run, 2);

            Assert.That(run.PigeonPopulation, Is.EqualTo(4));
            Assert.That(run.HedgehogHabitats.Count, Is.EqualTo(2));
            Assert.That(run.PigeonGroups(4).Select(group => group.Origin.id),
                Is.EquivalentTo(new[] { firstMeadow.id, secondMeadow.id }));
            var ecology = run.PreviewEcologyDay();
            Assert.That(ecology.pigeonsPresent, Is.EqualTo(4));
            Assert.That(ecology.pigeonsFed, Is.EqualTo(4));
            Assert.That(ecology.pigeonMealHomeTileIds.Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(ecology.hedgehogHomeTileIds.Count, Is.EqualTo(2));
            Assert.That(ecology.hedgehogMealHomeTileIds.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void FoxDoesNotArriveInIsolatedShrubPatchJustFromBoardwidePigeons()
        {
            var board = new ConstructionBoardModel();
            var firstMeadow = BuildTile(board, 4, 3,
                ConstructionCategory.Green, GreenPlanting.Meadow);
            BuildTile(board, 5, 3, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            BuildTile(board, 3, 4, ConstructionCategory.Square);
            BuildTile(board, 3, 5, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            BuildTile(board, 4, 5, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            BuildTile(board, 2, 3, ConstructionCategory.Residence);
            BuildTile(board, 1, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            BuildTile(board, 1, 4, ConstructionCategory.Green,
                GreenPlanting.Shrub);
            BuildTile(board, 0, 3, ConstructionCategory.Green,
                GreenPlanting.Shrub);

            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ConstructionRunModel).GetField("board", flags)
                .SetValue(run, board);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 4);
            typeof(ConstructionRunModel).GetProperty("PigeonTileId")
                .SetValue(run, firstMeadow.id);
            Assert.That(run.PigeonPopulation, Is.EqualTo(4));
            Assert.That(ConstructionGreenPatchModel.Find(board, 4)
                .Any(patch => patch.FoxCapacity == 1), Is.True);
            Assert.That(run.FoxHabitat, Is.Null,
                "The residence blocks distant prey and local cover protects the hedgehog.");
        }

        [Test]
        public void FirstMeadowHostsVisitorsUntilSecondMeadowMatures()
        {
            var board = new ConstructionBoardModel();
            var first = BuildTile(board, 3, 4,
                ConstructionCategory.Green, GreenPlanting.Meadow);
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ConstructionRunModel).GetField("board", flags)
                .SetValue(run, board);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 2);
            Assert.That(run.VisitorPigeonTile?.id, Is.EqualTo(first.id));
            Assert.That(run.PigeonPopulation, Is.Zero);
            Assert.That(run.PreviewEcologyDay().pigeonsPresent, Is.Zero);

            BuildTile(board, 2, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow);
            typeof(ConstructionRunModel).GetProperty("CurrentDay")
                .SetValue(run, 4);
            typeof(ConstructionRunModel).GetProperty("PigeonTileId")
                .SetValue(run, first.id);
            Assert.That(run.VisitorPigeonTile, Is.Null);
            Assert.That(run.PigeonPopulation, Is.EqualTo(2));
        }

        private static void Build(ConstructionBoardModel board, int column,
            int row, ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None, int day = 1)
        {
            Assert.That(board.TryBuild(column, row, category, planting,
                out _, out var failure, day), Is.True, failure.ToString());
        }

        private static ConstructionTileData BuildTile(
            ConstructionBoardModel board, int column, int row,
            ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            Assert.That(board.TryBuild(column, row, category, planting,
                out var tile, out var failure, 1), Is.True,
                failure.ToString());
            return tile;
        }
    }
}
