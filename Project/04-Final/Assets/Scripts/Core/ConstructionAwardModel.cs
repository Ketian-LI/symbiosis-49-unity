namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionAwardTier
    {
        Plastic,
        Iron,
        Bronze,
        Silver,
        Gold
    }

    // Fill awards use the completed board's final score. Current Fill runs
    // include shared, construction and goal points; historical records retain
    // their original shared-only totals. Endless and BoardGame have separate
    // completion rules.
    public static class ConstructionAwardModel
    {
        public const int AwardScoringVersion = 8;
        public const int CurrentRulesVersion = 2;
        public const int CurrentIronMinimum = 1500;
        public const int CurrentBronzeMinimum = 2500;
        public const int CurrentSilverMinimum = 3500;
        public const int CurrentGoldMinimum = 4500;

        public static int MinimumFor(ConstructionAwardTier tier,
            int scoringVersion, int awardRulesVersion = 1)
        {
            var current = awardRulesVersion >= CurrentRulesVersion;
            var balanced = scoringVersion >= 12;
            return tier switch
            {
                ConstructionAwardTier.Gold => current ? CurrentGoldMinimum : GoldMinimum,
                ConstructionAwardTier.Silver => current ? CurrentSilverMinimum : balanced ? BalancedSilverMinimum : SilverMinimum,
                ConstructionAwardTier.Bronze => current ? CurrentBronzeMinimum : balanced ? BalancedBronzeMinimum : BronzeMinimum,
                ConstructionAwardTier.Iron => current ? CurrentIronMinimum : balanced ? BalancedIronMinimum : IronMinimum,
                _ => int.MinValue
            };
        }
        public const int RequiredDays = 49;
        public const int IronMinimum = 300;
        public const int BronzeMinimum = 550;
        public const int SilverMinimum = 800;
        // More than 3000 points earns gold; 3000 itself remains silver.
        public const int GoldMinimum = 3001;
        public const int BalancedIronMinimum = 1200;
        public const int BalancedBronzeMinimum = 1800;
        public const int BalancedSilverMinimum = 2400;

        public static bool SupportsScoringVersion(int scoringVersion) =>
            scoringVersion >= AwardScoringVersion &&
            scoringVersion <= ConstructionScoreLedger.CurrentScoringVersion;

        public static bool TryEvaluate(ConstructionGameMode mode,
            int scoringVersion, int daysPlayed,
            ConstructionEndReason endReason, int totalPoints,
            out ConstructionAwardTier tier)
            => TryEvaluate(mode, scoringVersion, daysPlayed, endReason,
                totalPoints, 1, out tier);

        public static bool TryEvaluate(ConstructionGameMode mode,
            int scoringVersion, int daysPlayed, ConstructionEndReason endReason,
            int totalPoints, int awardRulesVersion, out ConstructionAwardTier tier)
        {
            tier = default;
            if (awardRulesVersion < 0 || awardRulesVersion > CurrentRulesVersion || mode != ConstructionGameMode.Fill ||
                !SupportsScoringVersion(scoringVersion) ||
                daysPlayed != RequiredDays ||
                endReason != ConstructionEndReason.BoardComplete)
                return false;

            tier = totalPoints >= MinimumFor(ConstructionAwardTier.Gold, scoringVersion, awardRulesVersion) ? ConstructionAwardTier.Gold
                : totalPoints >= MinimumFor(ConstructionAwardTier.Silver, scoringVersion, awardRulesVersion) ? ConstructionAwardTier.Silver
                : totalPoints >= MinimumFor(ConstructionAwardTier.Bronze, scoringVersion, awardRulesVersion) ? ConstructionAwardTier.Bronze
                : totalPoints >= MinimumFor(ConstructionAwardTier.Iron, scoringVersion, awardRulesVersion) ? ConstructionAwardTier.Iron
                : ConstructionAwardTier.Plastic;
            return true;
        }
    }
}
