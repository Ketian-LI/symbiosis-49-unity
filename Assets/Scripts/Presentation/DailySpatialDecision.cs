using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // An action must change a forecasted outcome, not only the coordinates of
    // two visually similar rooms. A well-functioning layout may be kept, but
    // not while these observable, preventable pressures remain unresolved.
    public static class DailySpatialDecision
    {
        public enum Risk
        {
            None,
            Commute,
            Waste,
            GreenNetwork,
            PigeonMeals
        }

        public static bool HasMaterialImpact(RoomLayoutImpactPreview impact) =>
            impact.Before.WorkingResidents != impact.After.WorkingResidents ||
            impact.BeforeParkEdges != impact.AfterParkEdges ||
            impact.BeforeSeedCapacity != impact.AfterSeedCapacity ||
            impact.BeforePigeonSeedMealCeiling != impact.AfterPigeonSeedMealCeiling ||
            impact.BeforeFullPopulationWasteOverflow != impact.AfterFullPopulationWasteOverflow ||
            impact.BeforeMarketOverflow != impact.AfterMarketOverflow ||
            impact.BeforeShelterPairs != impact.AfterShelterPairs ||
            // Moving a shrub suspends shelter even when today's pair count is unchanged.
            impact.MovedShrubs > 0 ||
            impact.BeforeAnimalConnections != impact.AfterAnimalConnections ||
            impact.BeforeBufferedGarages != impact.AfterBufferedGarages ||
            impact.BeforeGreenCells != impact.AfterGreenCells ||
            impact.BeforeFoodAccess?.Pigeon != impact.AfterFoodAccess?.Pigeon ||
            impact.BeforeFoodAccess?.Squirrel != impact.AfterFoodAccess?.Squirrel ||
            impact.BeforeFoodAccess?.Hedgehog != impact.AfterFoodAccess?.Hedgehog ||
            impact.BeforeFoodAccess?.FoxPrey != impact.AfterFoodAccess?.FoxPrey;

        public static Risk BlockingRisk(RoomLayoutImpactPreview impact, int residentCount)
        {
            if (impact.Before.WorkingResidents < residentCount) return Risk.Commute;
            if (impact.BeforeFullPopulationWasteOverflow > 0) return Risk.Waste;
            if (impact.BeforeGreenCells < GreenNetworkModel.StableCellCount) return Risk.GreenNetwork;
            if (impact.LivingPigeons > 0 &&
                impact.BeforePigeonSeedMealCeiling < impact.LivingPigeons)
                return Risk.PigeonMeals;
            return Risk.None;
        }

        public static bool CanKeepLayout(RoomLayoutImpactPreview impact, int residentCount) =>
            BlockingRisk(impact, residentCount) == Risk.None;
    }
}
