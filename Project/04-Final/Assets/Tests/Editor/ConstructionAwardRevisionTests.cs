using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionAwardRevisionTests
    {
        [TestCase(-200,ConstructionAwardTier.Plastic)]
        [TestCase(1499,ConstructionAwardTier.Plastic)]
        [TestCase(1500,ConstructionAwardTier.Iron)]
        [TestCase(2499,ConstructionAwardTier.Iron)]
        [TestCase(2500,ConstructionAwardTier.Bronze)]
        [TestCase(3499,ConstructionAwardTier.Bronze)]
        [TestCase(3500,ConstructionAwardTier.Silver)]
        [TestCase(4499,ConstructionAwardTier.Silver)]
        [TestCase(4500,ConstructionAwardTier.Gold)]
        public void NewBandsIncludeTheExactRoundNumber(int points,ConstructionAwardTier expected)
        {
            Assert.That(ConstructionAwardModel.TryEvaluate(ConstructionGameMode.Fill,13,49,
                ConstructionEndReason.BoardComplete,points,2,out var tier),Is.True);
            Assert.That(tier,Is.EqualTo(expected));
        }

        [Test]
        public void SaveRoundTripKeepsTheAwardRulesAndMissingMetadataKeepsLegacy()
        {
            var fresh=new ConstructionRunModel(ConstructionGameMode.Fill,true);
            var saved=fresh.Export();
            Assert.That(fresh.AwardRulesVersion,Is.EqualTo(2));
            Assert.That(ConstructionRunModel.TryRestore(saved,out var restored),Is.True);
            Assert.That(restored.AwardRulesVersion,Is.EqualTo(2));
            Assert.That(restored.OverallScore,Is.EqualTo(fresh.OverallScore));
            saved.awardRulesVersion=0;
            Assert.That(ConstructionRunModel.TryRestore(saved,out restored),Is.True);
            Assert.That(restored.AwardRulesVersion,Is.EqualTo(1));
            Assert.That(ConstructionAwardModel.TryEvaluate(ConstructionGameMode.Fill,13,49,
                ConstructionEndReason.BoardComplete,3200,restored.AwardRulesVersion,out var old),Is.True);
            Assert.That(old,Is.EqualTo(ConstructionAwardTier.Gold));
            Assert.That(ConstructionAwardModel.TryEvaluate(ConstructionGameMode.Fill,13,49,
                ConstructionEndReason.BoardComplete,3200,2,out var current),Is.True);
            Assert.That(current,Is.EqualTo(ConstructionAwardTier.Bronze));
            saved.awardRulesVersion=99;
            Assert.That(ConstructionRunModel.TryRestore(saved,out _),Is.False);
        }

        [Test]
        public void LocalRecordPersistsItsOwnTrophyBands()
        {
            var path=Path.Combine(Path.GetTempPath(),"award-rules-"+Guid.NewGuid()+".json");
            try
            {
                var score=new ConstructionLeaderboardPayload { runId=Guid.NewGuid().ToString(),nickname="Tester",
                    mode="Fill",scoringVersion=13,awardRulesVersion=2,daysPlayed=49,endReason="BoardComplete",
                    animalPoints=1000,residentPoints=2200,totalPoints=3200,linkageTrackedDays=49,bothSidesScoredDays=30 };
                var store=new ConstructionLocalLeaderboardStore(path);
                Assert.That(store.TryRecord(score,false,out _),Is.True);
                Assert.That(store.TryLoad(out var rows),Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
                Assert.That(rows[0].score.awardRulesVersion,Is.EqualTo(2));
                Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(rows[0].score,out var tier),Is.True);
                Assert.That(tier,Is.EqualTo(ConstructionAwardTier.Bronze));
                var legacy=JsonUtility.FromJson<ConstructionLeaderboardPayload>(score.ToJson().Replace("\"awardRulesVersion\":2,",""));
                Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(legacy,out tier),Is.True);
                Assert.That(tier,Is.EqualTo(ConstructionAwardTier.Gold));
            }
            finally { if(File.Exists(path)) File.Delete(path); }
        }
    }
}
