using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public enum ConstructionLocalLeaderboardStatus
    {
        Missing, Loaded, Invalid, Unavailable
    }

    [Serializable]
    public sealed class ConstructionLocalLeaderboardRecord
    {
        public ConstructionLeaderboardPayload score;
        public string recordedUtc;
    }

    [Serializable]
    internal sealed class ConstructionLocalLeaderboardData
    {
        public int version = 1;
        public List<ConstructionLocalLeaderboardRecord> records = new();
    }

    // A separate history file survives a new run in any of the three modes.
    // No score or nickname leaves this device.
    public sealed class ConstructionLocalLeaderboardStore
    {
        private readonly string filePath;

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath, "construction-local-leaderboard-v1.json");

        public ConstructionLocalLeaderboardStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A leaderboard path is required.",
                    nameof(path));
            filePath = Path.GetFullPath(path);
        }

        public ConstructionLocalLeaderboardStatus TryLoad(
            out List<ConstructionLocalLeaderboardRecord> records)
        {
            records = new List<ConstructionLocalLeaderboardRecord>();
            string json;
            try
            {
                json = File.ReadAllText(filePath, new UTF8Encoding(false, true));
            }
            catch (FileNotFoundException) { return ConstructionLocalLeaderboardStatus.Missing; }
            catch (DirectoryNotFoundException) { return ConstructionLocalLeaderboardStatus.Missing; }
            catch (DecoderFallbackException) { return ConstructionLocalLeaderboardStatus.Invalid; }
            catch (UnauthorizedAccessException) { return ConstructionLocalLeaderboardStatus.Unavailable; }
            catch (IOException) { return ConstructionLocalLeaderboardStatus.Unavailable; }
            catch (System.Security.SecurityException) { return ConstructionLocalLeaderboardStatus.Unavailable; }

            try
            {
                var data = JsonUtility.FromJson<ConstructionLocalLeaderboardData>(json);
                if (data == null || data.version != 1 || data.records == null ||
                    data.records.Any(record => !Valid(record)) ||
                    data.records.Select(record => record.score.runId)
                        .Distinct(StringComparer.Ordinal).Count() != data.records.Count)
                    return ConstructionLocalLeaderboardStatus.Invalid;
                records = data.records;
                return ConstructionLocalLeaderboardStatus.Loaded;
            }
            catch (ArgumentException) { return ConstructionLocalLeaderboardStatus.Invalid; }
            catch (InvalidOperationException) { return ConstructionLocalLeaderboardStatus.Invalid; }
            catch (OverflowException) { return ConstructionLocalLeaderboardStatus.Invalid; }
            catch (NullReferenceException) { return ConstructionLocalLeaderboardStatus.Invalid; }
        }

        public ConstructionLocalLeaderboardStatus RankingsFor(
            ConstructionGameMode mode, out List<ConstructionLocalLeaderboardRecord> rankings)
        {
            var status = TryLoad(out var records);
            rankings = status == ConstructionLocalLeaderboardStatus.Loaded
                ? Sorted(records.Where(record =>
                    record.score.mode == mode.ToString() &&
                    record.score.scoringVersion ==
                        ConstructionScoreLedger.CurrentRankingVersion(mode)))
                : new List<ConstructionLocalLeaderboardRecord>();
            return status;
        }

        // Keep the earlier mode-only query for existing callers. The game UI
        // uses this ruleset-aware query so linear and random tabletop results
        // never appear in the same ranking.
        public ConstructionLocalLeaderboardStatus RankingsFor(
            ConstructionGameMode mode, int boardGameRulesVersion,
            out List<ConstructionLocalLeaderboardRecord> rankings)
            => RankingsFor(mode, boardGameRulesVersion,
                ConstructionScoreLedger.CurrentRankingVersion(mode), out rankings);

        // Keep earlier score rules in the same local file without comparing
        // their totals against runs using the traffic-penalty rule.
        public ConstructionLocalLeaderboardStatus RankingsFor(
            ConstructionGameMode mode, int boardGameRulesVersion,
            int scoringVersion,
            out List<ConstructionLocalLeaderboardRecord> rankings)
            => RankingsFor(mode, boardGameRulesVersion, scoringVersion,
                -1, out rankings);

        // The final Fill result now ranks the same all-points total shown in
        // game. Historical shared-only scores remain readable, but separate.
        public ConstructionLocalLeaderboardStatus RankingsFor(
            ConstructionGameMode mode, int boardGameRulesVersion,
            int scoringVersion, int scoreFormatVersion,
            out List<ConstructionLocalLeaderboardRecord> rankings)
        {
            var status = TryLoad(out var records);
            rankings = status == ConstructionLocalLeaderboardStatus.Loaded
                ? Sorted(records.Where(record =>
                    record.score.mode == mode.ToString() &&
                    record.score.scoringVersion == scoringVersion &&
                    (scoreFormatVersion < 0 ||
                     record.score.scoreFormatVersion == scoreFormatVersion) &&
                    EffectiveBoardGameRulesVersion(record.score) ==
                    boardGameRulesVersion))
                : new List<ConstructionLocalLeaderboardRecord>();
            return status;
        }

        // The first result for a run ID is immutable. A later click may change
        // only its display name, never the score or scoring version.
        public bool TryRecord(ConstructionLeaderboardPayload score,
            bool updateNickname, out int rank)
        {
            rank = 0;
            if (!ValidScore(score)) return false;
            var status = TryLoad(out var records);
            if (status == ConstructionLocalLeaderboardStatus.Invalid ||
                status == ConstructionLocalLeaderboardStatus.Unavailable)
                return false;
            var existing = records.FirstOrDefault(record =>
                record.score.runId == score.runId);
            if (existing != null)
            {
                if (!SameResult(existing.score, score)) return false;
                if (updateNickname && existing.score.nickname != score.nickname)
                {
                    existing.score.nickname = score.nickname;
                    if (!TryWrite(records)) return false;
                }
            }
            else
            {
                records.Add(new ConstructionLocalLeaderboardRecord
                {
                    score = score,
                    recordedUtc = DateTime.UtcNow.ToString("o")
                });
                if (!TryWrite(records)) return false;
            }
            rank = Sorted(records.Where(record =>
                record.score.mode == score.mode &&
                record.score.scoringVersion == score.scoringVersion &&
                record.score.scoreFormatVersion == score.scoreFormatVersion &&
                EffectiveBoardGameRulesVersion(record.score) ==
                EffectiveBoardGameRulesVersion(score))).FindIndex(record =>
                record.score.runId == score.runId) + 1;
            return rank > 0;
        }

        private bool TryWrite(List<ConstructionLocalLeaderboardRecord> records)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory)) return false;
                Directory.CreateDirectory(directory);
                var temporary = filePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(
                    new ConstructionLocalLeaderboardData { records = records }),
                    new UTF8Encoding(false));
                if (File.Exists(filePath)) File.Replace(temporary, filePath, null);
                else File.Move(temporary, filePath);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }

        private static List<ConstructionLocalLeaderboardRecord> Sorted(
            IEnumerable<ConstructionLocalLeaderboardRecord> records)
        {
            var entries = records.ToList();
            // The random tabletop list rewards both sides thriving. Keep the
            // displayed shared score additive; ranking uses the weaker side
            // first, then observed animal/resident linkage, then shared score.
            // Other modes and older tabletop runs retain their old ordering.
            if (entries.Count > 0 && entries.All(record =>
                    EffectiveBoardGameRulesVersion(record.score) == 2))
                return entries.OrderByDescending(record =>
                        Math.Min(record.score.animalPoints,
                            record.score.residentPoints))
                    .ThenByDescending(record => record.score.linkagePoints)
                    .ThenByDescending(record => record.score.totalPoints)
                    .ThenByDescending(record => record.score.bothSidesScoredDays)
                    .ThenBy(record => record.recordedUtc, StringComparer.Ordinal)
                    .ThenBy(record => record.score.runId, StringComparer.Ordinal)
                    .ToList();

            return entries.OrderByDescending(record => record.score.totalPoints)
                .ThenByDescending(record => Math.Min(record.score.animalPoints,
                    record.score.residentPoints))
                .ThenByDescending(record => record.score.bothSidesScoredDays)
                .ThenBy(record => record.recordedUtc, StringComparer.Ordinal)
                .ThenBy(record => record.score.runId, StringComparer.Ordinal)
                .ToList();
        }

        private static bool Valid(ConstructionLocalLeaderboardRecord record) =>
            record != null && ValidScore(record.score) &&
            DateTime.TryParse(record.recordedUtc,
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);

        private static bool ValidScore(ConstructionLeaderboardPayload score) =>
            score != null && Guid.TryParse(score.runId, out _) &&
            !string.IsNullOrWhiteSpace(score.nickname) &&
            Regex.IsMatch(score.nickname,
                @"\A[A-Za-z0-9_. \u4e00-\u9fff-]{1,16}\z") &&
            !score.nickname.Contains("  ") &&
            (score.mode == ConstructionGameMode.Fill.ToString() ||
             score.mode == ConstructionGameMode.Endless.ToString() ||
             score.mode == ConstructionGameMode.BoardGame.ToString()) &&
            score.scoringVersion is >= 4 and <=
                ConstructionScoreLedger.CurrentScoringVersion &&
            (score.mode == ConstructionGameMode.BoardGame.ToString()
                ? score.boardGameRulesVersion is >= 0 and <= 2
                : score.boardGameRulesVersion == 0) &&
            score.daysPlayed > 0 &&
            (score.mode == ConstructionGameMode.Endless.ToString()
                ? score.endReason == ConstructionEndReason.AnimalDeathsLimit.ToString() ||
                  score.endReason == ConstructionEndReason.ResidentsUnemployed.ToString() ||
                  score.endReason == ConstructionEndReason.WildlifeGone.ToString() ||
                  score.endReason == ConstructionEndReason.ResidentsGone.ToString()
                : score.daysPlayed == 49 &&
                  score.endReason == ConstructionEndReason.BoardComplete.ToString()) &&
            (score.scoringVersion == 4
                ? score.animalPoints >= 0
                : (long)score.animalPoints >= -(long)(score.scoringVersion >= 13
                    ? ConstructionScoreLedger.LayoutTrafficCollisionPenalty
                    : score.scoringVersion >= 12 ? ConstructionScoreLedger.BalancedTrafficCollisionPenalty
                    : ConstructionScoreLedger.TrafficCollisionPenalty) * score.daysPlayed) &&
            score.residentPoints >= 0 &&
            (score.scoreFormatVersion == 0 &&
                score.totalPoints == (long)score.animalPoints +
                    score.residentPoints ||
             score.scoreFormatVersion == 1 &&
                (score.mode == ConstructionGameMode.Fill.ToString() ||
                 score.mode == ConstructionGameMode.Endless.ToString() && score.scoringVersion >= 13) &&
                score.totalPoints == (long)score.animalPoints +
                    score.residentPoints + score.buildPoints +
                    score.goalPoints) &&
            score.linkageTrackedDays == score.daysPlayed &&
            score.animalMeals >= 0 && score.humanWorkdays >= 0 &&
            score.linkagePoints >= 0 && score.sharedSquareMeals >= 0 &&
            score.sharedSupermarketMeals >= 0 && score.sharedGreenMeals >= 0 &&
            score.bothSidesScoredDays >= 0 &&
            score.bothSidesScoredDays <= score.daysPlayed;

        private static bool SameResult(ConstructionLeaderboardPayload a,
            ConstructionLeaderboardPayload b) =>
            a.mode == b.mode && a.scoringVersion == b.scoringVersion &&
            EffectiveBoardGameRulesVersion(a) ==
                EffectiveBoardGameRulesVersion(b) &&
            a.daysPlayed == b.daysPlayed && a.endReason == b.endReason &&
            a.animalPoints == b.animalPoints &&
            a.residentPoints == b.residentPoints &&
            a.totalPoints == b.totalPoints &&
            a.scoreFormatVersion == b.scoreFormatVersion &&
            a.buildPoints == b.buildPoints &&
            a.goalPoints == b.goalPoints &&
            a.animalMeals == b.animalMeals &&
            a.humanWorkdays == b.humanWorkdays &&
            a.linkagePoints == b.linkagePoints &&
            a.sharedSquareMeals == b.sharedSquareMeals &&
            a.sharedSupermarketMeals == b.sharedSupermarketMeals &&
            a.sharedGreenMeals == b.sharedGreenMeals &&
            a.linkageTrackedDays == b.linkageTrackedDays &&
            a.bothSidesScoredDays == b.bothSidesScoredDays;

        private static int EffectiveBoardGameRulesVersion(
            ConstructionLeaderboardPayload score) =>
            score.mode == ConstructionGameMode.BoardGame.ToString()
                ? Math.Max(1, score.boardGameRulesVersion) : 0;
    }
}
