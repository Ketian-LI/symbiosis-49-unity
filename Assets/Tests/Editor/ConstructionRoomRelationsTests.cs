using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionRoomRelationsTests
    {
        [Test] public void CoverageUsesEightNearbyPlotsAndOnlySharedEdgeRoadChains()
        {
            ConstructionTileData Tile(string id,ConstructionCategory category,int x,int y)=>
                new(){id=id,category=category,column=x,row=y};
            var waste=Tile("w",ConstructionCategory.Waste,2,2);
            var built=new[]{waste,Tile("s1",ConstructionCategory.Street,3,2),Tile("s2",ConstructionCategory.Street,4,2),
                Tile("diagonal",ConstructionCategory.Street,5,3),Tile("h",ConstructionCategory.Residence,4,3)};
            var range=ConstructionWasteModel.CoveragePlots(waste,built);
            Assert.That(range,Does.Contain((1,1)),"Nearby diagonal plots are served.");
            Assert.That(range,Does.Contain((4,3)),"A shared-edge road chain serves its adjacent source.");
            Assert.That(range,Has.No.Member((6,3)),"A diagonal road must not extend the collection route.");
        }

        [Test] public void CoveredDiagonalHomeAndPenalisedEdgeHomeRemainDifferentReadOnlyRelations()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Endless);
            run.ConfigureEndlessLossRules(20,20);Assert.That(run.TrySimulateDay(out _),Is.True);
            foreach(var item in new[]{(ConstructionCategory.Residence,4,3),(ConstructionCategory.Waste,4,4),(ConstructionCategory.Green,5,4),(ConstructionCategory.Residence,5,5)})
            {
                Assert.That(run.TryBuild(item.Item2,item.Item3,item.Item1,item.Item1==ConstructionCategory.Green?GreenPlanting.Meadow:GreenPlanting.None,out _,out _,out _),Is.True);
                Assert.That(run.TrySimulateConstructionDay(out _),Is.True);
            }
            var before=JsonUtility.ToJson(run.Export());
            var relation=ConstructionRoomRelations.For(run,run.At(4,4).id);
            Assert.That(relation.Coverage,Does.Contain((4,3)));
            Assert.That(relation.Coverage,Does.Contain((5,5)));
            Assert.That(relation.Penalties.Keys,Is.EquivalentTo(new[]{run.At(4,3).id}));
            Assert.That(relation.Penalties.Values.Single(),Is.EqualTo(-80));
            Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
            Assert.That(ConstructionRoomRelations.For(run,"missing").Selected,Is.Null);
        }
    }
}
