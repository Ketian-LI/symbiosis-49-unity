using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionDayResult
    {
        public int day;
        public int animalMeals;
        public int animalPoints;
        // V5: a traffic collision costs three animal points in addition to
        // any meal the lost animal could not eat. Older recorded days remain
        // unchanged when their run is upgraded.
        public int trafficPenaltyPoints;
        public bool trafficPenaltyRulesApplied;
        public int completedWorkCycles;
        // Version 2: points awarded for completed resident days, including
        // satisfaction. Zero in earlier saves, which retain their old score.
        public int humanPoints;
        // Version 2: both sides must score on the same day.
        // Version 3: coexistence is the sum of animal and resident points.
        public int balancePoints;
        // V4 awards are already included in humanPoints and balancePoints.
        // False on historical V3 days migrated into an active V4 run.
        public bool linkageRulesApplied;
        public int sharedSquareMeals;
        public int sharedSupermarketMeals;
        public int sharedGreenMeals;
        // V8: completed-cycle homes next to two different fed wildlife
        // habitats. Included in linkagePoints and humanPoints.
        public int biodiversityHomes;
        public int linkagePoints;
    }

    [Serializable]
    public sealed class ConstructionScoreSaveData
    {
        public List<ConstructionDayResult> days = new();
        // Missing in older saves: their animal total included fox hunts.
        public int scoringVersion;
    }

    // Totals are the displayed scores. Rates remain available as diagnostics
    // for historical tests and runs of different lengths.
    public sealed class ConstructionFinalResult
    {
        public int DaysPlayed { get; internal set; }
        public int AnimalMealsTotal { get; internal set; }
        public int AnimalPointsTotal { get; internal set; }
        public int HumanWorkdaysTotal { get; internal set; }
        public int HumanPointsTotal { get; internal set; }
        public int BalancePointsTotal { get; internal set; }
        public int LinkagePointsTotal { get; internal set; }
        public int SharedSquareMealsTotal { get; internal set; }
        public int SharedSupermarketMealsTotal { get; internal set; }
        public int SharedGreenMealsTotal { get; internal set; }
        public int BiodiversityHomesTotal { get; internal set; }
        public int LinkageTrackedDays { get; internal set; }
        // A diagnostic alongside the additive score: both groups actually
        // earned points on these days. It does not alter historical totals.
        public int BothSidesScoredDays { get; internal set; }
        public float AnimalMealsPerDay => DaysPlayed == 0 ? 0f :
            (float)AnimalMealsTotal / DaysPlayed;
        public float HumanWorkdaysPerDay => DaysPlayed == 0 ? 0f :
            (float)HumanWorkdaysTotal / DaysPlayed;
    }

    // Counts observed outcomes, never expected route availability. This keeps
    // the new end score separate from a preview or a green-adjacency bonus.
    public sealed class ConstructionScoreLedger
    {
        // V8 rewards two different fed species near a completed-cycle home.
        // V9 separates stronger placement awards from older leaderboard scores.
        // V10 rewards observed wildlife feeding and the sources actually used.
        // V11 gives a completed-cycle green visit and a green feeding source
        // one extra point each. Saved earlier days retain their original points.
        // V12 gives the 49-placement hand game its own balanced score bands.
        // V13 weights functioning neighbourhoods and harmful edges more
        // strongly. Historical runs keep the version saved with their days.
        public const int CurrentScoringVersion = 13;
        public static int CurrentRankingVersion(ConstructionGameMode mode) =>
            mode == ConstructionGameMode.BoardGame ? 9 : CurrentScoringVersion;
        public const int TrafficCollisionPenalty = 3;
        public const int BalancedTrafficCollisionPenalty = 25;
        public const int LayoutTrafficCollisionPenalty = 60;
        public int CollisionPenalty => scoringVersion >= 13
            ? LayoutTrafficCollisionPenalty : scoringVersion >= 12
            ? BalancedTrafficCollisionPenalty : TrafficCollisionPenalty;
        // A resident can earn one additional point after a completed cycle
        // by visiting a reachable square. Existing recorded totals stay intact.
        public const int MaxHumanPointsPerCycle = 9;
        private readonly List<ConstructionDayResult> days = new();
        private readonly int scoringVersion;

        public ConstructionScoreLedger(int scoringVersion = CurrentScoringVersion)
        {
            this.scoringVersion = scoringVersion;
        }

        public bool UsesLegacyAnimalScoring => scoringVersion == 0;
        public int ScoringVersion => scoringVersion;
        public bool HasBalanceScore => scoringVersion >= 2;
        public bool HasAdditiveCoexistenceScore => scoringVersion >= 3;
        public bool HasLinkageScore => scoringVersion >= 4;
        public bool HasTrafficPenaltyScore => scoringVersion >= 5;
        public bool HasVariableGreenYield => scoringVersion >= 6;
        public bool HasLocalFoodAreaScore => scoringVersion >= 7;
        public bool HasBiodiversityScore => scoringVersion >= 8;
        public bool HasFedAnimalScore => scoringVersion >= 10;

        public int CoexistencePoints(int animalPoints, int humanPoints,
            int trafficPenaltyPoints = 0) =>
            !HasBalanceScore ? 0 : HasAdditiveCoexistenceScore
                ? animalPoints - trafficPenaltyPoints + humanPoints
                : Math.Min(animalPoints, humanPoints);

        public int AnimalScore => days.Sum(result => UsesLegacyAnimalScoring
            ? result.animalMeals : result.animalPoints -
                result.trafficPenaltyPoints);
        public int HumanScore => days.Sum(result => HasBalanceScore
            ? result.humanPoints : result.completedWorkCycles);
        public int BalanceScore => HasBalanceScore
            ? days.Sum(result => result.balancePoints) : 0;
        public IReadOnlyList<ConstructionDayResult> Days => days.Select(Clone).ToArray();
        internal (int current, int best) CoexistenceStreak()
        {
            var current = 0;
            var best = 0;
            foreach (var day in days)
            {
                current = day.animalMeals > 0 && day.completedWorkCycles > 0
                    ? current + 1 : 0;
                best = Math.Max(best, current);
            }
            return (current, best);
        }

        public ConstructionDayResult LastDay => days.Count == 0
            ? null : Clone(days[days.Count - 1]);

        public ConstructionFinalResult FinalResult() => new()
        {
            DaysPlayed = days.Count,
            AnimalMealsTotal = days.Sum(result => result.animalMeals),
            AnimalPointsTotal = AnimalScore,
            HumanWorkdaysTotal = days.Sum(result => result.completedWorkCycles),
            HumanPointsTotal = HumanScore,
            BalancePointsTotal = BalanceScore,
            LinkagePointsTotal = days.Sum(result => result.linkagePoints),
            SharedSquareMealsTotal = days.Sum(result => result.sharedSquareMeals),
            SharedSupermarketMealsTotal = days.Sum(result =>
                result.sharedSupermarketMeals),
            SharedGreenMealsTotal = days.Sum(result => result.sharedGreenMeals),
            BiodiversityHomesTotal = days.Sum(result =>
                result.biodiversityHomes),
            LinkageTrackedDays = days.Count(result =>
                result.linkageRulesApplied),
            BothSidesScoredDays = days.Count(result =>
                (UsesLegacyAnimalScoring ? result.animalMeals :
                    result.animalPoints - result.trafficPenaltyPoints) > 0 &&
                (HasBalanceScore ? result.humanPoints :
                    result.completedWorkCycles) > 0)
        };

        public bool TryRecordDay(int day, int animalMeals, int completedWorkCycles,
            int animalPoints = -1, int humanPoints = -1,
            int sharedSquareMeals = 0, int sharedSupermarketMeals = 0,
            int sharedGreenMeals = 0, bool linkageRulesApplied = true,
            int trafficPenaltyPoints = 0,
            bool trafficPenaltyRulesApplied = true,
            int biodiversityHomes = 0)
        {
            // The optional values keep the historical simulation test entry
            // point usable. Production settlement supplies observed points.
            var actualAnimalPoints = animalPoints < 0 ? animalMeals : animalPoints;
            var actualHumanPoints = humanPoints < 0
                ? completedWorkCycles : humanPoints;
            var sharedPoints = 3L * sharedSquareMeals +
                2L * sharedSupermarketMeals + sharedGreenMeals;
            var expectedLinkagePoints = sharedPoints +
                2L * biodiversityHomes;
            var usesLinkage = HasLinkageScore && linkageRulesApplied;
            var usesTrafficPenalty = HasTrafficPenaltyScore &&
                                     trafficPenaltyRulesApplied;
            var netAnimalPoints = UsesLegacyAnimalScoring
                ? animalMeals : actualAnimalPoints - trafficPenaltyPoints;
            var nextHumanPoints = HasBalanceScore
                ? actualHumanPoints : completedWorkCycles;
            var nextBalancePoints = HasBalanceScore
                ? (long)netAnimalPoints + actualHumanPoints : 0L;
            var animalTotal = days.Sum(result => (long)(UsesLegacyAnimalScoring
                ? result.animalMeals : result.animalPoints -
                    result.trafficPenaltyPoints));
            var humanTotal = days.Sum(result => (long)(HasBalanceScore
                ? result.humanPoints : result.completedWorkCycles));
            var balanceTotal = days.Sum(result => (long)result.balancePoints);
            if (day < 1 || animalMeals < 0 || completedWorkCycles < 0 ||
                animalPoints < -1 || humanPoints < -1 ||
                trafficPenaltyPoints != 0 &&
                    trafficPenaltyPoints != CollisionPenalty ||
                !usesTrafficPenalty && trafficPenaltyPoints != 0 ||
                sharedSquareMeals < 0 || sharedSupermarketMeals < 0 ||
                sharedGreenMeals < 0 || biodiversityHomes < 0 ||
                sharedPoints > ConstructionLinkageScorer.MaxSharedPointsPerDay ||
                biodiversityHomes >
                    (HasBiodiversityScore
                        ? Math.Min(ConstructionLinkageScorer
                            .MaxBiodiversityHomesPerDay, completedWorkCycles)
                        : 0) ||
                expectedLinkagePoints > (HasBiodiversityScore
                    ? ConstructionLinkageScorer.MaxPointsPerDay
                    : ConstructionLinkageScorer.MaxSharedPointsPerDay) ||
                (long)sharedSquareMeals + sharedSupermarketMeals +
                    sharedGreenMeals > (HasLocalFoodAreaScore
                        ? completedWorkCycles
                        : Math.Min(animalMeals, completedWorkCycles)) ||
                !usesLinkage && expectedLinkagePoints != 0 ||
                !UsesLegacyAnimalScoring && !HasLocalFoodAreaScore &&
                    (long)actualAnimalPoints > 3L * animalMeals ||
                HasAdditiveCoexistenceScore &&
                    (nextBalancePoints > int.MaxValue ||
                     nextBalancePoints < int.MinValue) ||
                animalTotal + netAnimalPoints > int.MaxValue ||
                animalTotal + netAnimalPoints < int.MinValue ||
                humanTotal + nextHumanPoints > int.MaxValue ||
                humanTotal + nextHumanPoints < int.MinValue ||
                balanceTotal + nextBalancePoints > int.MaxValue ||
                balanceTotal + nextBalancePoints < int.MinValue ||
                HasBalanceScore &&
                    ((long)actualHumanPoints <
                         completedWorkCycles + expectedLinkagePoints ||
                     (long)actualHumanPoints >
                          (long)(scoringVersion >= 11 ?
                              MaxHumanPointsPerCycle : 8) * completedWorkCycles +
                         expectedLinkagePoints) ||
                days.Any(result => result.day == day) ||
                (days.Count > 0 && day != days[days.Count - 1].day + 1) ||
                (days.Count == 0 && day != 1)) return false;

            days.Add(new ConstructionDayResult
            {
                day = day,
                animalMeals = animalMeals,
                animalPoints = UsesLegacyAnimalScoring ? 0 :
                    actualAnimalPoints,
                trafficPenaltyPoints = trafficPenaltyPoints,
                trafficPenaltyRulesApplied = usesTrafficPenalty,
                completedWorkCycles = completedWorkCycles,
                humanPoints = HasBalanceScore ? actualHumanPoints : 0,
                balancePoints = HasBalanceScore
                    ? CoexistencePoints(actualAnimalPoints,
                        actualHumanPoints, trafficPenaltyPoints) : 0,
                linkageRulesApplied = usesLinkage,
                sharedSquareMeals = sharedSquareMeals,
                sharedSupermarketMeals = sharedSupermarketMeals,
                sharedGreenMeals = sharedGreenMeals,
                biodiversityHomes = biodiversityHomes,
                linkagePoints = (int)expectedLinkagePoints
            });
            return true;
        }

        public ConstructionScoreSaveData Export() => new()
        {
            days = days.Select(Clone).ToList(),
            scoringVersion = scoringVersion
        };

        // Existing V3 day records retain their points; only future settlement
        // uses linkage scoring. V0-V2 keep their distinct historical formulae.
        public ConstructionScoreLedger UpgradeVersionThreeToCurrent()
        {
            if (scoringVersion != 3)
                throw new InvalidOperationException(
                    "Only additive V3 scores can be upgraded in place.");
            return UpgradeAdditiveToCurrent();
        }

        // A running V3 save adopts the V4 linkage rules it would have used
        // before traffic penalties were introduced. Keeping the old version
        // prevents historical collision days from entering V5 rankings.
        public ConstructionScoreLedger UpgradeVersionThreeToFour()
        {
            if (scoringVersion != 3)
                throw new InvalidOperationException(
                    "Only additive V3 scores can be upgraded to V4.");
            var upgraded = new ConstructionScoreLedger(4);
            upgraded.days.AddRange(days.Select(Clone));
            return upgraded;
        }

        public ConstructionScoreLedger UpgradeAdditiveToCurrent()
        {
            if (scoringVersion is not 3 and not 4)
                throw new InvalidOperationException(
                    "Only additive V3/V4 scores can be upgraded in place.");
            var upgraded = new ConstructionScoreLedger();
            upgraded.days.AddRange(days.Select(Clone));
            return upgraded;
        }

        public static bool TryRestore(ConstructionScoreSaveData saved,
            out ConstructionScoreLedger ledger)
        {
            ledger = null;
            if (saved?.days == null) return false;
            if (saved.scoringVersion is < 0 or > CurrentScoringVersion)
                return false;
            // An untouched legacy V0 run has no points to preserve. Explicit
            // V1+ versions still identify their rules even before settlement.
            var version = saved.days.Count == 0 && saved.scoringVersion == 0
                ? CurrentScoringVersion : saved.scoringVersion;
            var candidate = new ConstructionScoreLedger(version);
            foreach (var result in saved.days)
            {
                if (result == null || saved.scoringVersion > 0 &&
                    result.animalPoints < 0 ||
                    (saved.scoringVersion < 5 &&
                     (result.trafficPenaltyPoints != 0 ||
                      result.trafficPenaltyRulesApplied)) ||
                    saved.scoringVersion >= 2 &&
                    result.balancePoints != candidate.CoexistencePoints(
                        result.animalPoints, result.humanPoints,
                        result.trafficPenaltyPoints) ||
                    saved.scoringVersion < 4 &&
                        (result.linkageRulesApplied ||
                         result.linkagePoints != 0) ||
                    saved.scoringVersion < 8 &&
                        result.biodiversityHomes != 0 ||
                    saved.scoringVersion >= 4 &&
                        result.linkagePoints != 3L * result.sharedSquareMeals +
                            2L * result.sharedSupermarketMeals +
                            result.sharedGreenMeals +
                            2L * result.biodiversityHomes ||
                    !candidate.TryRecordDay(result.day,
                        result.animalMeals, result.completedWorkCycles,
                        result.animalPoints, saved.scoringVersion >= 2
                            ? result.humanPoints : -1,
                        result.sharedSquareMeals,
                        result.sharedSupermarketMeals,
                        result.sharedGreenMeals,
                        result.linkageRulesApplied,
                        result.trafficPenaltyPoints,
                        result.trafficPenaltyRulesApplied,
                        result.biodiversityHomes)) return false;
            }
            ledger = candidate;
            return true;
        }

        private static ConstructionDayResult Clone(ConstructionDayResult source) => new()
        {
            day = source.day,
            animalMeals = source.animalMeals,
            animalPoints = source.animalPoints,
            trafficPenaltyPoints = source.trafficPenaltyPoints,
            trafficPenaltyRulesApplied = source.trafficPenaltyRulesApplied,
            completedWorkCycles = source.completedWorkCycles,
            humanPoints = source.humanPoints,
            balancePoints = source.balancePoints,
            linkageRulesApplied = source.linkageRulesApplied,
            sharedSquareMeals = source.sharedSquareMeals,
            sharedSupermarketMeals = source.sharedSupermarketMeals,
            sharedGreenMeals = source.sharedGreenMeals,
            biodiversityHomes = source.biodiversityHomes,
            linkagePoints = source.linkagePoints
        };
    }
}
