using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class OakTreeGrowthModelTests
    {
        [Test]
        public void MatureTreeBecomesFelledWhenItsRoomMoves()
        {
            var model = new OakTreeGrowthModel(RoomLayoutData.All);

            Assert.That(model.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Mature));
            Assert.That(model.Fell("oak-a"), Is.True);
            Assert.That(model.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Felled));
            Assert.That(model.TreesFelled, Is.EqualTo(1));
        }

        [Test]
        public void PlantedTreeNeedsTwoCompleteDaysToMature()
        {
            var model = new OakTreeGrowthModel(RoomLayoutData.All);
            model.Fell("oak-a");
            Assert.That(model.Plant("oak-a"), Is.True);
            Assert.That(model.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Sapling));

            model.AdvanceOneCompleteDay();
            Assert.That(model.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Young));

            var matured = model.AdvanceOneCompleteDay();
            Assert.That(model.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Mature));
            Assert.That(matured, Does.Contain("oak-a"));
            Assert.That(model.TreesMatured, Is.EqualTo(2));
        }

        [Test]
        public void StartingRecoveryTreeBeginsYoung()
        {
            var model = new OakTreeGrowthModel(RoomLayoutData.All);

            Assert.That(model.StageOf("oak-c"), Is.EqualTo(OakTreeStage.Young));
            Assert.That(model.Trees.Values.Count(item => item.stage == OakTreeStage.Mature), Is.EqualTo(3));
        }

        [Test]
        public void TreeStateAndStatisticsRoundTrip()
        {
            var model = new OakTreeGrowthModel(RoomLayoutData.All);
            model.Fell("oak-a");
            model.Plant("oak-a");
            model.AdvanceOneCompleteDay();

            var restored = new OakTreeGrowthModel(RoomLayoutData.All);
            restored.Restore(model.ExportTrees(), 1, 1, 0);

            Assert.That(restored.StageOf("oak-a"), Is.EqualTo(OakTreeStage.Young));
            Assert.That(restored.TreesFelled, Is.EqualTo(1));
            Assert.That(restored.TreesPlanted, Is.EqualTo(1));
        }
    }
}
