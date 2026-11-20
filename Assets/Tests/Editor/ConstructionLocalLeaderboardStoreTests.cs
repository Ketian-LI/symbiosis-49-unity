using System;
using System.IO;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionLocalLeaderboardStoreTests
    {
        private string directory;
        private string path;
        private ConstructionLocalLeaderboardStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(),
                "symbiosis49-local-ranks-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "leaderboard.json");
            store = new ConstructionLocalLeaderboardStore(path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void CompletedRunsPersistAcrossInstancesAndStaySeparateByMode()
        {
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Fill, 35, 40),
                false, out var firstRank), Is.True);
            Assert.That(firstRank, Is.EqualTo(1));
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Endless, 90, 70),
                false, out var endlessRank), Is.True);
            Assert.That(endlessRank, Is.EqualTo(1));
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Fill, 60, 50),
                false, out var secondRank), Is.True);
            Assert.That(secondRank, Is.EqualTo(1));

            var reopened = new ConstructionLocalLeaderboardStore(path);
            Assert.That(reopened.RankingsFor(ConstructionGameMode.Fill,
                out var fill), Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(fill.Count, Is.EqualTo(2));
            Assert.That(fill[0].score.totalPoints, Is.EqualTo(110));
            Assert.That(fill[1].score.totalPoints, Is.EqualTo(75));
            Assert.That(reopened.RankingsFor(ConstructionGameMode.Endless,
                out var endless), Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(endless.Count, Is.EqualTo(1));
            Assert.That(endless[0].score.totalPoints, Is.EqualTo(160));
            Assert.That(reopened.RankingsFor(ConstructionGameMode.BoardGame,
                out var boardGame), Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(boardGame, Is.Empty);
        }

        [Test]
        public void SameRunCannotDuplicateOrChangeScoreButMayRename()
        {
            var score = Score(ConstructionGameMode.Fill, 60, 50);
            Assert.That(store.TryRecord(score, false, out _), Is.True);
            var otherName = Score(ConstructionGameMode.Fill, 60, 50);
            otherName.runId = score.runId;
            otherName.nickname = "New name";
            Assert.That(store.TryRecord(otherName, false, out _), Is.True);
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, out var first),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(first[0].score.nickname, Is.EqualTo("Player"));
            Assert.That(store.TryRecord(otherName, true, out var rank), Is.True);
            Assert.That(rank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, out var renamed),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(renamed[0].score.nickname, Is.EqualTo("New name"));

            otherName.totalPoints++;
            Assert.That(store.TryRecord(otherName, true, out _), Is.False);
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, out var unchanged),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(unchanged.Count, Is.EqualTo(1));
            Assert.That(unchanged[0].score.totalPoints, Is.EqualTo(110));
        }

        [Test]
        public void CorruptFileIsNotOverwrittenByAResult()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "{broken");
            var before = File.ReadAllBytes(path);
            Assert.That(store.TryLoad(out _),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Invalid));
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Fill, 10, 10),
                false, out _), Is.False);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        }

        [Test]
        public void SharedScoreTiePrefersTheMoreBalancedResult()
        {
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Fill, 95, 5),
                false, out _), Is.True);
            Assert.That(store.TryRecord(Score(ConstructionGameMode.Fill, 55, 45),
                false, out var rank), Is.True);
            Assert.That(rank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill,
                out var rankings), Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(rankings[0].score.animalPoints, Is.EqualTo(55));
        }

        [Test]
        public void RandomBoardGameRanksDoNotMixWithEarlierTabletopRuns()
        {
            var earlier = Score(ConstructionGameMode.BoardGame, 100, 90);
            var random = Score(ConstructionGameMode.BoardGame, 70, 60);
            random.boardGameRulesVersion = 2;
            Assert.That(store.TryRecord(earlier, false, out var earlierRank),
                Is.True);
            Assert.That(store.TryRecord(random, false, out var randomRank),
                Is.True);
            Assert.That(earlierRank, Is.EqualTo(1));
            Assert.That(randomRank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.BoardGame, 1,
                out var earlierScores), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(store.RankingsFor(ConstructionGameMode.BoardGame, 2,
                out var randomScores), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(earlierScores.Count, Is.EqualTo(1));
            Assert.That(randomScores.Count, Is.EqualTo(1));
            Assert.That(randomScores[0].score.runId, Is.EqualTo(random.runId));
        }

        [Test]
        public void RandomBoardGameRanksByWeakerSideThenLinkageThenSharedTotal()
        {
            var forestHeavy = Score(ConstructionGameMode.BoardGame, 500, 30);
            forestHeavy.boardGameRulesVersion = 2;
            var balanced = Score(ConstructionGameMode.BoardGame, 240, 180);
            balanced.boardGameRulesVersion = 2;
            var moreLinked = Score(ConstructionGameMode.BoardGame, 180, 240);
            moreLinked.boardGameRulesVersion = 2;
            moreLinked.linkagePoints = 12;
            var moreShared = Score(ConstructionGameMode.BoardGame, 250, 180);
            moreShared.boardGameRulesVersion = 2;
            moreShared.linkagePoints = 12;

            Assert.That(store.TryRecord(forestHeavy, false, out _), Is.True);
            Assert.That(store.TryRecord(balanced, false, out _), Is.True);
            Assert.That(store.TryRecord(moreLinked, false, out _), Is.True);
            Assert.That(store.TryRecord(moreShared, false, out var rank), Is.True);
            Assert.That(rank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.BoardGame, 2,
                out var rankings), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(rankings[0].score.runId, Is.EqualTo(moreShared.runId));
            Assert.That(rankings[1].score.runId, Is.EqualTo(moreLinked.runId));
            Assert.That(rankings[2].score.runId, Is.EqualTo(balanced.runId));
            Assert.That(rankings[3].score.runId, Is.EqualTo(forestHeavy.runId));
            Assert.That(rankings[0].score.totalPoints,
                Is.EqualTo(rankings[0].score.animalPoints +
                    rankings[0].score.residentPoints));
        }

        [Test]
        public void OlderScoreRecordsRemainReadableAndSeparateFromTrafficPenaltyScores()
        {
            var older = Score(ConstructionGameMode.Fill, 60, 40);
            older.scoringVersion = 4;
            var current = Score(ConstructionGameMode.Fill, 45, 35);
            Assert.That(store.TryRecord(older, false, out var olderRank), Is.True);
            Assert.That(store.TryRecord(current, false, out var currentRank), Is.True);
            Assert.That(olderRank, Is.EqualTo(1));
            Assert.That(currentRank, Is.EqualTo(1));
            Assert.That(store.TryLoad(out var all),
                Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, 0, 4,
                out var oldRanks), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(oldRanks.Count, Is.EqualTo(1));
            Assert.That(oldRanks[0].score.runId, Is.EqualTo(older.runId));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill,
                out var newRanks), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(newRanks.Count, Is.EqualTo(1));
            Assert.That(newRanks[0].score.runId, Is.EqualTo(current.runId));
        }

        [Test]
        public void TrafficPenaltyCanLeaveAValidNegativeAnimalScore()
        {
            var penalized = Score(ConstructionGameMode.Endless, -3, 0);
            penalized.bothSidesScoredDays = 0;
            Assert.That(store.TryRecord(penalized, false, out var rank), Is.True);
            Assert.That(rank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.Endless,
                out var rankings), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(rankings[0].score.animalPoints, Is.EqualTo(-3));

            var impossibleOlderScore = Score(ConstructionGameMode.Endless, -3, 0);
            impossibleOlderScore.scoringVersion = 4;
            Assert.That(store.TryRecord(impossibleOlderScore, false, out _),
                Is.False);
        }

        [Test]
        public void AllPointsFillRankIsSeparateFromOlderSharedOnlyRank()
        {
            var older = Score(ConstructionGameMode.Fill, 120, 180);
            var current = Score(ConstructionGameMode.Fill, 120, 180);
            current.scoreFormatVersion = 1;
            current.buildPoints = 45;
            current.goalPoints = 12;
            current.totalPoints = 357;
            Assert.That(store.TryRecord(older, false, out var oldRank), Is.True);
            Assert.That(store.TryRecord(current, false, out var newRank), Is.True);
            Assert.That(oldRank, Is.EqualTo(1));
            Assert.That(newRank, Is.EqualTo(1));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, 0,
                ConstructionScoreLedger.CurrentScoringVersion, 1,
                out var currentRanks), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(currentRanks.Count, Is.EqualTo(1));
            Assert.That(currentRanks[0].score.totalPoints, Is.EqualTo(357));
            Assert.That(store.RankingsFor(ConstructionGameMode.Fill, 0,
                ConstructionScoreLedger.CurrentScoringVersion, 0,
                out var oldRanks), Is.EqualTo(
                    ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(oldRanks.Count, Is.EqualTo(1));
            Assert.That(oldRanks[0].score.totalPoints, Is.EqualTo(300));

            current.totalPoints = 356;
            Assert.That(store.TryRecord(current, false, out _), Is.False,
                "The displayed total must match every score component.");
        }

        private static ConstructionLeaderboardPayload Score(
            ConstructionGameMode mode, int animal, int resident) =>
            new ConstructionLeaderboardPayload
            {
                runId = Guid.NewGuid().ToString("D"),
                nickname = "Player",
                mode = mode.ToString(),
                scoringVersion = ConstructionScoreLedger.CurrentRankingVersion(mode),
                daysPlayed = mode == ConstructionGameMode.Endless ? 12 : 49,
                endReason = mode == ConstructionGameMode.Endless
                    ? ConstructionEndReason.AnimalDeathsLimit.ToString()
                    : ConstructionEndReason.BoardComplete.ToString(),
                animalPoints = animal,
                residentPoints = resident,
                totalPoints = animal + resident,
                linkageTrackedDays = mode == ConstructionGameMode.Endless ? 12 : 49,
                bothSidesScoredDays = 8
            };
    }
}
