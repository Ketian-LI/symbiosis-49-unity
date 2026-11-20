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
        {
            tier = default;
            if (mode != ConstructionGameMode.Fill ||
                !SupportsScoringVersion(scoringVersion) ||
                daysPlayed != RequiredDays ||
                endReason != ConstructionEndReason.BoardComplete)
                return false;

            var balanced = scoringVersion >= 12;
            tier = totalPoints >= GoldMinimum ? ConstructionAwardTier.Gold
                : totalPoints >= (balanced ? BalancedSilverMinimum :
                    SilverMinimum) ? ConstructionAwardTier.Silver
                : totalPoints >= (balanced ? BalancedBronzeMinimum :
                    BronzeMinimum) ? ConstructionAwardTier.Bronze
                : totalPoints >= (balanced ? BalancedIronMinimum :
                    IronMinimum) ? ConstructionAwardTier.Iron
                : ConstructionAwardTier.Plastic;
            return true;
        }
    }
}
