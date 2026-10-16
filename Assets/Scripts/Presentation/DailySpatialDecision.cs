using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // An action must change a forecasted outcome, not only the coordinates of
    // two visually similar rooms. A forecast risk is information, not a forced
    // rearrangement: the resident economy already prices missed commutes, and
    // a swap can make the overall situation worse. The player may deliberately
    // keep a complete layout and accept the observed outcome.
    public static class DailySpatialDecision
    {
        public enum Risk
        {
            None,
            Commute
        }

        public static bool HasMaterialImpact(RoomLayoutImpactPreview impact) =>
            impact.Before.WorkingResidents != impact.After.WorkingResidents ||
            // Count an ecological change only when it changes a reachable
            // meal, a food shortfall, cover, or the stable-green threshold.
            // Raw links and room coordinates are not outcomes by themselves.
            (impact.BeforeSeedStockCeiling >= 0 && impact.AfterSeedStockCeiling >= 0
                ? impact.BeforeSeedStockCeiling != impact.AfterSeedStockCeiling
                : impact.BeforePigeonSeedMealCeiling != impact.AfterPigeonSeedMealCeiling) ||
            SeedShortfallChanged(impact) ||
            impact.LivingHedgehogs > 0 &&
                impact.BeforeShelterPairs != impact.AfterShelterPairs ||
            FutureShelterChanged(impact) ||
            GreenDeficitChanged(impact) ||
            impact.BeforeFoodAccess?.Pigeon != impact.AfterFoodAccess?.Pigeon ||
            impact.BeforeFoodAccess?.Squirrel != impact.AfterFoodAccess?.Squirrel ||
            impact.BeforeFoodAccess?.Hedgehog != impact.AfterFoodAccess?.Hedgehog ||
            impact.BeforeFoodAccess?.FoxPrey != impact.AfterFoodAccess?.FoxPrey;

        public static bool SeedShortfallChanged(RoomLayoutImpactPreview impact) =>
            impact.LivingPigeons > 0 &&
            System.Math.Max(0, impact.LivingPigeons - impact.BeforeSeedCapacity) !=
            System.Math.Max(0, impact.LivingPigeons - impact.AfterSeedCapacity);

        public static bool GreenDeficitChanged(RoomLayoutImpactPreview impact) =>
            impact.BeforeGreenCells >= 0 && impact.AfterGreenCells >= 0 &&
            System.Math.Max(0, GreenNetworkModel.StableCellCount - impact.BeforeGreenCells) !=
            System.Math.Max(0, GreenNetworkModel.StableCellCount - impact.AfterGreenCells);

        public static bool FutureShelterChanged(RoomLayoutImpactPreview impact) =>
            impact.LivingHedgehogs > 0 && impact.MovedShrubs > 0 &&
            (impact.BeforeRecoveredShelterPairs >= 0
                ? impact.BeforeRecoveredShelterPairs : impact.BeforeShelterPairs) !=
            impact.RecoveredShelterPairs;

        public static Risk BlockingRisk(RoomLayoutImpactPreview impact, int residentCount)
        {
            if (impact.Before.WorkingResidents < residentCount) return Risk.Commute;
            return Risk.None;
        }

        public static bool CanKeepLayout(RoomLayoutImpactPreview impact, int residentCount) => true;
    }
}
