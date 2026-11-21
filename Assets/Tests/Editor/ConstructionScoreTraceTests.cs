using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionScoreTraceTests
    {
        [Test] public void EmptyRunHasNoInventedContributors()
        {
            Assert.That(ConstructionScoreTrace.For(null),Is.Empty);
            Assert.That(ConstructionScoreTrace.For(new ConstructionRunModel(ConstructionGameMode.Fill,true)),Is.Empty);
        }
        [Test] public void TracesActualMealsRoutinesAndRoomsWithoutChangingSave()
        {
            var run=Start();
            Build(run,4,3,ConstructionCategory.Residence);
            Build(run,5,3,ConstructionCategory.Restaurant);
            var before=JsonUtility.ToJson(run.Export());
            var trace=ConstructionScoreTrace.For(run);
            var humans=trace.Where(entry=>entry.Kind==ScoreTraceKind.Resident).ToArray();
            Assert.That(humans.Sum(entry=>entry.Points??0),Is.EqualTo(run.LastHumanDay.SatisfactionPoints));
            foreach(var entry in humans)
            {
                Assert.That(entry.Actors.Count,Is.EqualTo(1));
                var person=run.LastHumanDay.residents.Single(person=>person.homeTileId==entry.Actors[0].HomeId);
                Assert.That(person.completedCycle,Is.True);
                Assert.That(entry.Tiles,Does.Contain(person.homeTileId));
                Assert.That(entry.Tiles,Does.Contain(person.workTileId));
                Assert.That(entry.Tiles,Does.Contain(person.mealTileId));
            }
            var ecology=run.LastEcologyDay;
            var meals=trace.Where(entry=>entry.English.EndsWith("meal")).ToArray();
            Assert.That(meals.Length,Is.EqualTo(ecology.squirrelFoodTileIds.Count+ecology.pigeonMealTileIds.Count+ecology.hedgehogMealTileIds.Count));
            Assert.That(trace.Where(entry=>entry.Kind==ScoreTraceKind.Animal).Sum(entry=>entry.Points??0),
                Is.EqualTo(run.LastScoreDay.animalPoints-run.LastScoreDay.trafficPenaltyPoints));
            Assert.That(trace.Any(entry=>entry.Kind==ScoreTraceKind.Shared),Is.True);
            Assert.That(JsonUtility.ToJson(run.Export()),Is.EqualTo(before));
        }
        [Test] public void DeductionHighlightsBothSidesAndExcludesTheUnsettledNextRoom()
        {
            var run=Start();Build(run,4,3,ConstructionCategory.Residence);Build(run,4,4,ConstructionCategory.Waste);
            var trace=ConstructionScoreTrace.For(run);
            var negative=trace.Single(entry=>entry.Kind==ScoreTraceKind.Build && entry.Points==-80);
            Assert.That(negative.Tiles.Count,Is.EqualTo(2));
            var home=run.At(4,3);var waste=run.At(4,4);
            Assert.That(negative.Tiles,Is.EquivalentTo(new[]{home.id,waste.id}));
            Assert.That(negative.Actors,Is.Empty);
            Assert.That(run.TryBuild(5,3,ConstructionCategory.Restaurant,GreenPlanting.None,out var queued,out _,out _),Is.True);
            Assert.That(ConstructionScoreTrace.For(run).SelectMany(entry=>entry.Tiles),Does.Not.Contain(queued.id));
        }
        [Test] public void RestoringASettledSaveReconstructsTheSameIndex()
        {
            var run=Start();Build(run,4,3,ConstructionCategory.Residence);Build(run,5,3,ConstructionCategory.Restaurant);
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),out var restored),Is.True);
            Assert.That(ConstructionScoreTrace.For(restored).Select(entry=>(entry.Kind,entry.English,entry.Points,string.Join(",",entry.Tiles))),
                Is.EqualTo(ConstructionScoreTrace.For(run).Select(entry=>(entry.Kind,entry.English,entry.Points,string.Join(",",entry.Tiles)))));
        }
        [Test] public void AHomeWithoutACompletedRoutineDoesNotInventAResidentContributor()
        {
            var run=Start();Build(run,4,3,ConstructionCategory.Residence);
            Assert.That(run.LastHumanDay.CompletedWorkCycles,Is.Zero);
            Assert.That(ConstructionScoreTrace.For(run).SelectMany(entry=>entry.Actors)
                .Any(actor=>actor.Kind==ScoreTraceActorKind.Resident),Is.False);
        }
        private static ConstructionRunModel Start()
        {
            var run=new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20,20),Is.True);
            Assert.That(run.TrySimulateDay(out _),Is.True);return run;
        }
        private static void Build(ConstructionRunModel run,int x,int y,ConstructionCategory category)
        {
            Assert.That(run.TryBuild(x,y,category,GreenPlanting.None,out _,out _,out _),Is.True);
            Assert.That(run.TrySimulateConstructionDay(out _),Is.True);
        }
    }
}
