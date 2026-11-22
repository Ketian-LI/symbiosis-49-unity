using System;
using System.IO;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionAwardModelTests
    {
        [TestCase(-30, ConstructionAwardTier.Plastic)]
        [TestCase(0, ConstructionAwardTier.Plastic)]
        [TestCase(299, ConstructionAwardTier.Plastic)]
        [TestCase(300, ConstructionAwardTier.Iron)]
        [TestCase(549, ConstructionAwardTier.Iron)]
        [TestCase(550, ConstructionAwardTier.Bronze)]
        [TestCase(799, ConstructionAwardTier.Bronze)]
        [TestCase(800, ConstructionAwardTier.Silver)]
        [TestCase(1199, ConstructionAwardTier.Silver)]
        [TestCase(1200, ConstructionAwardTier.Silver)]
        [TestCase(1800, ConstructionAwardTier.Silver)]
        [TestCase(1999, ConstructionAwardTier.Silver)]
        [TestCase(2000, ConstructionAwardTier.Silver)]
        [TestCase(3000, ConstructionAwardTier.Silver)]
        [TestCase(3001, ConstructionAwardTier.Gold)]
        [TestCase(3050, ConstructionAwardTier.Gold)]
        public void CompletedFillScoreUsesExactBandBoundaries(int score,
            ConstructionAwardTier expected)
        {
            Assert.That(ConstructionAwardModel.TryEvaluate(
                ConstructionGameMode.Fill,
                ConstructionAwardModel.AwardScoringVersion,
                ConstructionAwardModel.RequiredDays,
                ConstructionEndReason.BoardComplete,
                score, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(expected));
        }

        [TestCase(1199, ConstructionAwardTier.Plastic)]
        [TestCase(1200, ConstructionAwardTier.Iron)]
        [TestCase(1799, ConstructionAwardTier.Iron)]
        [TestCase(1800, ConstructionAwardTier.Bronze)]
        [TestCase(2399, ConstructionAwardTier.Bronze)]
        [TestCase(2400, ConstructionAwardTier.Silver)]
        [TestCase(3000, ConstructionAwardTier.Silver)]
        [TestCase(3001, ConstructionAwardTier.Gold)]
        public void BalancedFillRunUsesNewAwardBands(int score,
            ConstructionAwardTier expected)
        {
            Assert.That(ConstructionAwardModel.TryEvaluate(
                ConstructionGameMode.Fill, 12, 49,
                ConstructionEndReason.BoardComplete, score,
                out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(expected));
        }

        [TestCase(9)]
        [TestCase(10)]
        public void NewerCompletedFillRunRequiresMoreThanThreeThousandForGold(
            int scoringVersion)
        {
            Assert.That(ConstructionAwardModel.TryEvaluate(
                ConstructionGameMode.Fill, scoringVersion, 49,
                ConstructionEndReason.BoardComplete, 3000,
                out var silver), Is.True);
            Assert.That(silver, Is.EqualTo(ConstructionAwardTier.Silver));
            Assert.That(ConstructionAwardModel.TryEvaluate(
                ConstructionGameMode.Fill, scoringVersion, 49,
                ConstructionEndReason.BoardComplete, 3001,
                out var gold), Is.True);
            Assert.That(gold, Is.EqualTo(ConstructionAwardTier.Gold));
        }

        [TestCase(ConstructionGameMode.Endless, 8, 49,
            ConstructionEndReason.AnimalDeathsLimit)]
        [TestCase(ConstructionGameMode.BoardGame, 8, 49,
            ConstructionEndReason.BoardComplete)]
        [TestCase(ConstructionGameMode.Fill, 7, 49,
            ConstructionEndReason.BoardComplete)]
        [TestCase(ConstructionGameMode.Fill, ConstructionScoreLedger.CurrentScoringVersion + 1, 49,
            ConstructionEndReason.BoardComplete)]
        [TestCase(ConstructionGameMode.Fill, 8, 48,
            ConstructionEndReason.None)]
        [TestCase(ConstructionGameMode.Fill, 8, 49,
            ConstructionEndReason.None)]
        public void OtherRulesetsDoNotReceiveFillAward(
            ConstructionGameMode mode, int version, int days,
            ConstructionEndReason endReason)
        {
            Assert.That(ConstructionAwardModel.TryEvaluate(mode, version,
                days, endReason, 1200, out _), Is.False);
        }

        [Test]
        public void AwardUsesCurrentLocalRecordWithoutRewritingOldRecords()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "symbiosis49-award-test-" + Guid.NewGuid().ToString("N") +
                ".json");
            try
            {
                var store = new ConstructionLocalLeaderboardStore(path);
                var old = Score(7, 400, 500);
                var current = Score(8, 250, 300);
                Assert.That(store.TryRecord(old, false, out _), Is.True);
                Assert.That(store.TryRecord(current, false, out _), Is.True);

                Assert.That(store.TryLoad(out var all),
                    Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
                Assert.That(all.Count, Is.EqualTo(2));
                Assert.That(store.RankingsFor(ConstructionGameMode.Fill, 0, 7,
                    out var oldRanks), Is.EqualTo(
                        ConstructionLocalLeaderboardStatus.Loaded));
                Assert.That(oldRanks.Count, Is.EqualTo(1));
                Assert.That(oldRanks[0].score.totalPoints, Is.EqualTo(900));
                Assert.That(store.RankingsFor(ConstructionGameMode.Fill, 0, 8,
                    out var currentRanks), Is.EqualTo(
                        ConstructionLocalLeaderboardStatus.Loaded));
                Assert.That(currentRanks.Count, Is.EqualTo(1));
                Assert.That(currentRanks[0].score.totalPoints, Is.EqualTo(550));

                Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(
                    currentRanks[0].score,
                    out var tier), Is.True);
                Assert.That(tier, Is.EqualTo(ConstructionAwardTier.Bronze));
                Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(
                    oldRanks[0].score,
                    out _), Is.False);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void MalformedOrMissingLeaderboardPayloadHasNoAward()
        {
            Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(null,
                out _), Is.False);
            var malformed = Score(8, 250, 300);
            malformed.mode = "NotAMode";
            Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(
                malformed, out _), Is.False);
            malformed.mode = ConstructionGameMode.Fill.ToString();
            malformed.endReason = "NotAnEndReason";
            Assert.That(ConstructionAwardPayloadEvaluator.TryEvaluate(
                malformed, out _), Is.False);
        }

        private static ConstructionLeaderboardPayload Score(int scoringVersion,
            int animal, int resident) => new()
        {
            runId = Guid.NewGuid().ToString("D"),
            nickname = "Tester",
            mode = ConstructionGameMode.Fill.ToString(),
            scoringVersion = scoringVersion,
            daysPlayed = 49,
            endReason = ConstructionEndReason.BoardComplete.ToString(),
            animalPoints = animal,
            residentPoints = resident,
            totalPoints = animal + resident,
            linkageTrackedDays = 49,
            bothSidesScoredDays = 30
        };
    }
}
