using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionAwardCalibrationTests
    {
        [Serializable] public sealed class Sample
        {
            public int seed, total, animals, residents, build, goals, placed;
            public string policy;
            public bool completed;
        }
        [Serializable] public sealed class Report { public List<Sample> samples = new(); }

        // Diagnostic samples, not participant data or an optimal-play solver.
        [Test, Timeout(240000)]
        public void CompareCompleteFillLayoutsUnderThreePlacementPolicies()
        {
            var report = new Report();
            foreach (var policy in new[] { "unfavourable-edges", "random-legal", "connected-services" })
            for (var seed = 1; seed <= 12; seed++)
            {
                var saved = new ConstructionRunModel(ConstructionGameMode.Fill, true).Export();
                saved.runId = "00000000-0000-0000-0000-" + seed.ToString("D12");
                saved.fillHandRandomState = seed * 73856093;
                saved.specialTasks = null;
                Assert.That(ConstructionRunModel.TryRestore(saved, out var run), Is.True);
                Assert.That(run.TryConfirmSpecialTasks(), Is.True);
                var opening = new (ConstructionCategory category, int x, int y)[]
                {
                    (ConstructionCategory.Green,3,3),(ConstructionCategory.Residence,4,3),
                    (ConstructionCategory.Restaurant,5,3),(ConstructionCategory.Workshop,4,4),
                    (ConstructionCategory.Street,4,2),(ConstructionCategory.Waste,6,3),
                    (ConstructionCategory.Supermarket,3,2),(ConstructionCategory.Square,4,1)
                };
                foreach (var c in opening) Place(run,c.category,c.x,c.y);
                var random = new System.Random(seed);
                while (!run.IsFinished && run.BuiltCount < 49)
                {
                    var options = new List<(ConstructionCategory type,int x,int y,float value)>();
                    foreach (var type in run.FillHandCards)
                    for (var y=0;y<7;y++) for(var x=0;x<7;x++)
                    {
                        if (run.CheckPlacement(x,y,type,GreenPlanting.None)!=ConstructionPlacementFailure.None) continue;
                        var tile=new ConstructionTileData { id="probe",category=type,column=x,row=y,buildIndex=run.BuiltCount+1 };
                        var value=(float)ConstructionPlacementScore.PointsFor(tile,run.BuiltTiles,run.PlacementRulesVersion);
                        if (policy=="connected-services")
                        {
                            var near=run.BuiltTiles.Where(t=>Math.Abs(t.column-x)<=1 && Math.Abs(t.row-y)<=1).ToArray();
                            var homes=near.Count(t=>t.category==ConstructionCategory.Residence);
                            if (type is ConstructionCategory.Restaurant or ConstructionCategory.Workshop or ConstructionCategory.Supermarket)
                                value+=homes*24;
                            if (type==ConstructionCategory.Residence)
                                value+=near.Count(t=>t.category is ConstructionCategory.Restaurant or ConstructionCategory.Workshop or ConstructionCategory.Supermarket)*18;
                            if (type==ConstructionCategory.Waste) value-=10;
                        }
                        if(policy=="unfavourable-edges")value=-value;
                        if(policy=="random-legal")value=0;
                        options.Add((type,x,y,value+(float)random.NextDouble()*.1f));
                    }
                    Assert.That(options,Is.Not.Empty,"No legal placement at seed "+seed+" / "+policy);
                    var best=options.OrderByDescending(c=>c.value).First();
                    Place(run,best.type,best.x,best.y);
                }
                var sample=new Sample {seed=seed,policy=policy,total=run.OverallScore,animals=run.AnimalScore,
                    residents=run.HumanScore,build=run.PlacementScore,goals=run.SpecialTaskScore,
                    placed=run.BuiltCount,completed=run.EndReason==ConstructionEndReason.BoardComplete};
                report.samples.Add(sample);
                Assert.That(sample.completed && sample.placed==49,Is.True);
                TestContext.WriteLine(policy+" seed "+seed+": "+sample.total);
            }
            var path=Environment.GetEnvironmentVariable("SYMBIOSIS_AWARD_PROBE_PATH");
            if(!string.IsNullOrEmpty(path))File.WriteAllText(path,JsonUtility.ToJson(report,true));
        }

        private static void Place(ConstructionRunModel run,ConstructionCategory type,int x,int y)
        {
            Assert.That(run.TryBuild(x,y,type,run.BuiltCount==0?GreenPlanting.Oak:GreenPlanting.None,
                out _,out var failure,out var placement),Is.True,failure+" / "+placement);
            Assert.That(run.TrySimulateFillDay(out _),Is.True);
        }
    }
}
