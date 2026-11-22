using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;
namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionAutomaticRoutesAndTermsTests
    {
        [Test] public void SpareJobsAreMatchedAcrossHomesInsteadOfGreedyNearestChoice()
        {
            var board=new ConstructionBoardModel();
            ConstructionTileData Build(int x,int y,ConstructionCategory type)
            {Assert.That(board.TryBuild(x,y,type,GreenPlanting.None,out var tile,out _,allowIntroPlacement:true),Is.True);return tile;}
            var shared=Build(3,2,ConstructionCategory.Workshop);
            var first=Build(2,2,ConstructionCategory.Residence);
            var second=Build(3,1,ConstructionCategory.Residence);
            var constrained=Build(4,2,ConstructionCategory.Residence);
            var alternative=Build(2,1,ConstructionCategory.Workshop);
            var result=ConstructionHumanModel.SettleDay(3,board);
            Assert.That(result.WorkedCount,Is.EqualTo(3));
            Assert.That(result.CompletedWorkCycles,Is.Zero,"Jobs alone are not a completed commute.");
            Assert.That(result.residents.Single(p=>p.homeTileId==constrained.id).workTileId,Is.EqualTo(shared.id));
            foreach(var group in result.residents.Where(p=>p.worked).GroupBy(p=>p.workTileId))
                Assert.That(group.Count(),Is.LessThanOrEqualTo(2));
            var closed=ConstructionHumanModel.SettleDay(3,board,blockedFoodServices:new[]{alternative.id});
            Assert.That(closed.WorkedCount,Is.EqualTo(2));
        }
        [Test] public void ItineraryConnectsWorkToMealWithoutAnUnnecessaryReturnHome()
        {
            var board=new ConstructionBoardModel();
            ConstructionTileData Build(int x,int y,ConstructionCategory type)
            {Assert.That(board.TryBuild(x,y,type,GreenPlanting.None,out var tile,out _),Is.True);return tile;}
            var home=Build(3,2,ConstructionCategory.Residence);var street=Build(3,1,ConstructionCategory.Street);
            var work=Build(2,2,ConstructionCategory.Workshop);var meal=Build(2,1,ConstructionCategory.Restaurant);
            var resident=ConstructionHumanModel.SettleDay(1,board).residents.Single();
            Assert.That(resident.completedCycle,Is.True);
            Assert.That(resident.routeTileIds,Is.EqualTo(new[]{home.id,work.id,meal.id,street.id,home.id}));
        }
        [Test] public void GlossaryKeepsCopyAndExistingMarkupWithoutNestedLinks()
        {
            const string copy="住宅、餐厅、工作坊、街道、垃圾间、超市、广场与中央橡树。";
            var linked=ConstructionGlossary.Linkify(copy);
            Assert.That(Regex.Replace(linked,"<[^>]+>",""),Is.EqualTo(copy));
            foreach(var id in new[]{"home","restaurant","workshop","street","waste","supermarket","square","oak"})
                Assert.That(linked,Does.Contain("term:"+id));
            Assert.That(ConstructionGlossary.Linkify(linked),Is.EqualTo(linked));
            Assert.That(ConstructionGlossary.Linkify("<b>住宅</b>"),Does.StartWith("<b><link="));
            Assert.That(ConstructionGlossary.Linkify("Restaurant home",true),Does.Contain("#F1D18A"));
            Assert.That(ConstructionGlossary.Linkify("homeless workshops"),Does.Not.Contain("term:home"));
        }
    }
}
