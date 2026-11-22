using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UnityEngine;
namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionRecurringPenaltyTests
    {
        private static void Build(ref ConstructionRunModel run,ConstructionCategory category,int x,int y)
        {
            if(!run.FillHandCards.Contains(category))
            {var save=run.Export();save.fillHandCards[0]=category;Assert.That(ConstructionRunModel.TryRestore(save,out run),Is.True);}
            Assert.That(run.TryBuild(x,y,category,category==ConstructionCategory.Green?run.BuiltCount==0?GreenPlanting.Oak:GreenPlanting.Meadow:GreenPlanting.None,out _,out _,out _),Is.True);
            Assert.That(run.TrySimulateFillDay(out _),Is.True);
        }
        [Test] public void SharedWasteEdgeRepeatsOncePerConfirmedBuildWithoutFirstDayDoubleCharge()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Fill,useFillHand:true);Assert.That(run.TryConfirmSpecialTasks(),Is.True);Assert.That(run.SkipFillTutorial(),Is.True);
            Build(ref run,ConstructionCategory.Green,3,3);Build(ref run,ConstructionCategory.Residence,4,3);Build(ref run,ConstructionCategory.Waste,5,3);
            Assert.That(run.LastRecurringPenaltyPoints,Is.Zero,"The introducing placement already paid its -80 pairing.");
            Assert.That(run.LastPlacementPoints,Is.EqualTo(-80));
            Build(ref run,ConstructionCategory.Green,4,4);Assert.That(run.LastRecurringPenaltyPoints,Is.EqualTo(-80));
            Build(ref run,ConstructionCategory.Square,5,4);Assert.That(run.LastRecurringPenaltyPoints,Is.EqualTo(-80));Assert.That(run.RecurringPenaltyTotal,Is.EqualTo(-160));
            var before=JsonUtility.ToJson(run.Export());run.PreviewHumanDay();run.PreviewEcologyDay();var ignored=run.NextRecurringPenaltyPoints;
            Assert.That(run.TrySimulateFillDay(out _),Is.False);Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),out var loaded),Is.True);Assert.That(loaded.PlacementScore,Is.EqualTo(run.PlacementScore));
            var changed=loaded.Export();changed.recurringPenalties[0].points=-1;Assert.That(ConstructionRunModel.TryRestore(changed,out _),Is.False);
            Build(ref run,ConstructionCategory.Residence,6,3);Build(ref run,ConstructionCategory.Green,6,4);
            Assert.That(run.LastRecurringPenaltyPoints,Is.EqualTo(-160),"Two distinct shared-edge homes pay once each.");
        }
    }
}
