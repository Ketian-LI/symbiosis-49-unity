using System;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionDeckInventoryTests
    {
        [Test] public void CountsConserveSupplyAcrossTutorialSwapAndRestore()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill, useFillHand: true);
            Assert.That(run.TryConfirmSpecialTasks(), Is.True);
            var opening = new[] {
                (ConstructionCategory.Green,3,3), (ConstructionCategory.Residence,4,3),
                (ConstructionCategory.Restaurant,5,3), (ConstructionCategory.Workshop,4,4),
                (ConstructionCategory.Street,4,2), (ConstructionCategory.Waste,6,3),
                (ConstructionCategory.Supermarket,3,2), (ConstructionCategory.Square,4,1) };
            Assert.That(run.FillDeckHeldCards,Is.Zero,"The tutorial queue is not counted twice as held stock.");
            foreach(var tile in opening)
            {
                Check(run);
                Assert.That(run.TryBuild(tile.Item2,tile.Item3,tile.Item1,
                    tile.Item1==ConstructionCategory.Green?GreenPlanting.Oak:GreenPlanting.None,
                    out _,out _,out _),Is.True);
                Assert.That(run.TrySimulateFillDay(out _),Is.True);
            }
            Check(run);
            var old = run.FillHandCards.First(run.CanRerollFillHandCard);
            var before = run.FillDeckRemainingFor(old);
            Assert.That(run.TryRerollFillHandCard(old,out var replacement),Is.True);
            Assert.That(replacement,Is.Not.EqualTo(old));
            Assert.That(run.FillDeckRemainingFor(old),Is.EqualTo(before+1));
            Check(run);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),out var restored),Is.True);
            foreach(ConstructionCategory category in Enum.GetValues(typeof(ConstructionCategory)))
                Assert.That(restored.FillDeckRemainingFor(category),Is.EqualTo(run.FillDeckRemainingFor(category)));
            Check(restored);
        }
        private static void Check(ConstructionRunModel run)
        {
            var sum=Enum.GetValues(typeof(ConstructionCategory)).Cast<ConstructionCategory>().Sum(run.FillDeckRemainingFor);
            Assert.That(sum,Is.EqualTo(run.FillDeckCardsRemaining));
            Assert.That(sum+run.FillDeckHeldCards+run.BuiltCount,Is.EqualTo(100));
        }
        [Test] public void RandomLegacyDeckDoesNotInventFiniteCounts()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Fill,useFillHand:true);
            var save=run.Export();save.fillHandDeckVersion=0;
            save.greenPatchRulesVersion=0;save.fillHandWitnessedLinkage=false;
            save.fillHandExpanded=false;save.fillHandFedSpeciesMask=0;save.fillHandRerollsSpent=0;
            save.score.scoringVersion=9;save.specialTasks=null;save.placementScore=null;
            Assert.That(ConstructionRunModel.TryRestore(save,out run),Is.True);
            Assert.That(run.UsesFiniteFillDeck,Is.False);
            foreach(ConstructionCategory category in Enum.GetValues(typeof(ConstructionCategory)))
                Assert.That(run.FillDeckRemainingFor(category),Is.EqualTo(-1));
        }
        [Test] public void GuidanceEmphasizesActionsWithoutChangingPlainCopyOrNestingBold()
        {
            const string copy="点击高亮空格预览，再点「建造」确认。住宅旁有至少 2 格连片绿地。";
            var formatted=InstructionEmphasis.Format(copy);
            Assert.That(formatted,Does.Contain("<b>"));
            Assert.That(formatted.Replace("<b>","").Replace("</b>",""),Is.EqualTo(copy));
            Assert.That(InstructionEmphasis.Format("<b>建造</b>"),Is.EqualTo("<b>建造</b>"));
        }
    }
}
