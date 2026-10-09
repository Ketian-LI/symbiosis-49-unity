using System;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionStoryStage
    {
        WatchSquirrelEat,
        GrowGreen,
        WelcomeResident,
        BuildRestaurant,
        WatchResidentEat,
        BuildWorkshop,
        BuildWasteRoom,
        AwaitAccessProblem,
        BuildStreet,
        AwaitRestaurantCrowding,
        BuildSupermarket,
        FreeBuild,
        BoardComplete
    }

    [Serializable]
    public sealed class ConstructionStorySaveData
    {
        public bool squirrelAte;
        public bool newWildlifeArrived;
        public bool residentAte;
        public bool directAccessBlocked;
        public bool restaurantCrowded;
    }

    // Events must be raised by observed simulation outcomes, not just by a
    // timer. This makes each story unlock explain a real unmet need.
    public sealed class ConstructionStoryModel
    {
        private bool squirrelAte;
        private bool newWildlifeArrived;
        private bool residentAte;
        private bool directAccessBlocked;
        private bool restaurantCrowded;

        public ConstructionStoryStage Stage { get; private set; } =
            ConstructionStoryStage.WatchSquirrelEat;
        public bool NewWildlifeArrived => newWildlifeArrived;
        public bool ResidentAte => residentAte;

        public void RecordSquirrelMeal() => squirrelAte = true;
        public void RecordNewWildlifeArrival() => newWildlifeArrived = true;
        public void RecordResidentMeal() => residentAte = true;
        public void RecordDirectAccessBlocked() => directAccessBlocked = true;
        public void RecordRestaurantCrowded() => restaurantCrowded = true;

        public ConstructionStoryStage Observe(ConstructionBoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var placed = board.BuiltTiles;
            bool Has(ConstructionCategory category) =>
                placed.Any(tile => tile.category == category);
            if (!directAccessBlocked && Has(ConstructionCategory.Waste) &&
                placed.Count(tile => tile.category == ConstructionCategory.Residence) > 1 &&
                placed.Where(tile => tile.category == ConstructionCategory.Residence)
                    .Any(home => board.ResidenceServiceRoute(home.id,
                        ConstructionCategory.Restaurant).Count == 0 ||
                        board.ResidenceServiceRoute(home.id,
                            ConstructionCategory.Workshop).Count == 0))
                directAccessBlocked = true;

            Stage = board.IsFull ? ConstructionStoryStage.BoardComplete :
                !squirrelAte ? ConstructionStoryStage.WatchSquirrelEat :
                !newWildlifeArrived
                    ? ConstructionStoryStage.GrowGreen :
                !Has(ConstructionCategory.Residence)
                    ? ConstructionStoryStage.WelcomeResident :
                !board.FirstResidenceCanReachService(ConstructionCategory.Restaurant)
                    ? ConstructionStoryStage.BuildRestaurant :
                !residentAte ? ConstructionStoryStage.WatchResidentEat :
                !board.FirstResidenceCanReachService(ConstructionCategory.Workshop)
                    ? ConstructionStoryStage.BuildWorkshop :
                !Has(ConstructionCategory.Waste)
                    ? ConstructionStoryStage.BuildWasteRoom :
                !directAccessBlocked
                    ? ConstructionStoryStage.AwaitAccessProblem :
                !Has(ConstructionCategory.Street)
                    ? ConstructionStoryStage.BuildStreet :
                !restaurantCrowded
                    ? ConstructionStoryStage.AwaitRestaurantCrowding :
                !Has(ConstructionCategory.Supermarket)
                    ? ConstructionStoryStage.BuildSupermarket :
                ConstructionStoryStage.FreeBuild;
            return Stage;
        }

        public bool IsUnlocked(ConstructionCategory category) => Stage switch
        {
            ConstructionStoryStage.WatchSquirrelEat or ConstructionStoryStage.BoardComplete
                => false,
            _ => category switch
            {
                ConstructionCategory.Green => true,
                ConstructionCategory.Residence =>
                    Stage >= ConstructionStoryStage.WelcomeResident,
                ConstructionCategory.Restaurant =>
                    Stage >= ConstructionStoryStage.BuildRestaurant,
                ConstructionCategory.Workshop =>
                    Stage >= ConstructionStoryStage.BuildWorkshop,
                ConstructionCategory.Waste =>
                    Stage >= ConstructionStoryStage.BuildWasteRoom,
                ConstructionCategory.Street =>
                    Stage >= ConstructionStoryStage.BuildStreet,
                ConstructionCategory.Supermarket =>
                    Stage >= ConstructionStoryStage.BuildSupermarket,
                ConstructionCategory.Square =>
                    Stage >= ConstructionStoryStage.FreeBuild,
                _ => false
            }
        };

        public ConstructionStorySaveData Export() => new()
        {
            squirrelAte = squirrelAte,
            newWildlifeArrived = newWildlifeArrived,
            residentAte = residentAte,
            directAccessBlocked = directAccessBlocked,
            restaurantCrowded = restaurantCrowded
        };

        public static bool TryRestore(ConstructionStorySaveData saved,
            ConstructionBoardModel board, out ConstructionStoryModel story)
        {
            story = null;
            if (saved == null || board == null) return false;
            var placed = board.BuiltTiles;
            if (saved.newWildlifeArrived &&
                (!saved.squirrelAte || board.ConnectedGreenCount(
                    ConstructionBoardModel.StarterOakId) < 2 ||
                    !board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                        .Any(tile => tile.planting == GreenPlanting.Meadow)) ||
                saved.residentAte && (!saved.newWildlifeArrived ||
                    !board.FirstResidenceCanReachService(
                        ConstructionCategory.Restaurant)) ||
                saved.directAccessBlocked && !placed.Any(tile =>
                    tile.category == ConstructionCategory.Waste) ||
                saved.restaurantCrowded && (!placed.Any(tile =>
                    tile.category == ConstructionCategory.Restaurant) ||
                    !placed.Any(tile => tile.category == ConstructionCategory.Street)))
                return false;
            var candidate = new ConstructionStoryModel
            {
                squirrelAte = saved.squirrelAte,
                newWildlifeArrived = saved.newWildlifeArrived,
                residentAte = saved.residentAte,
                directAccessBlocked = saved.directAccessBlocked,
                restaurantCrowded = saved.restaurantCrowded
            };
            candidate.Observe(board);
            story = candidate;
            return true;
        }
    }
}
