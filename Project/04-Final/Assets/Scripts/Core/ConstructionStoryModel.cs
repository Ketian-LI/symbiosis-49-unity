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

    // The eight-room introduction advances in a fixed order based on what the
    // player actually built, not on optional arrivals or meals. Legacy modes
    // supply the oak; digital hand Fill deals it as the first Green card.
    public enum ConstructionTutorialChapter
    {
        InspectOak,
        Green,
        Residence,
        Restaurant,
        Workshop,
        Street,
        Waste,
        Supermarket,
        Square,
        Complete,
        ExploreRooms
    }

    public static class ConstructionTutorialGuide
    {
        public const int RoomCount = 8;

        private static readonly ConstructionCategory[] FixedRoomOrder =
        {
            ConstructionCategory.Green,
            ConstructionCategory.Residence,
            ConstructionCategory.Restaurant,
            ConstructionCategory.Workshop,
            ConstructionCategory.Street,
            ConstructionCategory.Waste,
            ConstructionCategory.Supermarket,
            ConstructionCategory.Square
        };

        public static ConstructionCategory CategoryAt(int index)
        {
            if (index < 0 || index >= FixedRoomOrder.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return FixedRoomOrder[index];
        }

        public static ConstructionTutorialChapter ChapterAt(int index) =>
            index switch
            {
                0 => ConstructionTutorialChapter.Green,
                1 => ConstructionTutorialChapter.Residence,
                2 => ConstructionTutorialChapter.Restaurant,
                3 => ConstructionTutorialChapter.Workshop,
                4 => ConstructionTutorialChapter.Street,
                5 => ConstructionTutorialChapter.Waste,
                6 => ConstructionTutorialChapter.Supermarket,
                7 => ConstructionTutorialChapter.Square,
                _ => ConstructionTutorialChapter.Complete
            };

        public static ConstructionCategory? RequiredCategory(
            ConstructionTutorialChapter chapter) => chapter switch
        {
            ConstructionTutorialChapter.Green => ConstructionCategory.Green,
            ConstructionTutorialChapter.Residence => ConstructionCategory.Residence,
            ConstructionTutorialChapter.Restaurant => ConstructionCategory.Restaurant,
            ConstructionTutorialChapter.Workshop => ConstructionCategory.Workshop,
            ConstructionTutorialChapter.Street => ConstructionCategory.Street,
            ConstructionTutorialChapter.Waste => ConstructionCategory.Waste,
            ConstructionTutorialChapter.Supermarket => ConstructionCategory.Supermarket,
            ConstructionTutorialChapter.Square => ConstructionCategory.Square,
            _ => null
        };

        public static ConstructionTutorialChapter FromBoard(
            ConstructionBoardModel board, bool oakLessonComplete,
            bool freeChoiceIntro = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!oakLessonComplete) return ConstructionTutorialChapter.InspectOak;
            var placed = board.BuiltTiles;
            if (freeChoiceIntro)
            {
                var typesBuilt = Enum.GetValues(typeof(ConstructionCategory))
                    .Cast<ConstructionCategory>().Count(category => placed.Any(tile =>
                        tile.category == category &&
                        tile.id != ConstructionBoardModel.StarterOakId));
                return typesBuilt < RoomCount
                    ? ConstructionTutorialChapter.ExploreRooms
                    : ConstructionTutorialChapter.Complete;
            }
            if (!placed.Any(tile => tile.category == ConstructionCategory.Green &&
                    tile.id != ConstructionBoardModel.StarterOakId))
                return ConstructionTutorialChapter.Green;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Residence))
                return ConstructionTutorialChapter.Residence;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Restaurant))
                return ConstructionTutorialChapter.Restaurant;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Workshop))
                return ConstructionTutorialChapter.Workshop;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Street))
                return ConstructionTutorialChapter.Street;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Waste))
                return ConstructionTutorialChapter.Waste;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Supermarket))
                return ConstructionTutorialChapter.Supermarket;
            if (!placed.Any(tile => tile.category == ConstructionCategory.Square))
                return ConstructionTutorialChapter.Square;
            return ConstructionTutorialChapter.Complete;
        }
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

    // Story events reflect observed outcomes, not a timer. New runs introduce
    // the eight room types in a fixed order independent of these events.
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
        // Derived from the current board, so an arbitrary street placement
        // cannot silently count as connecting the second household.
        public int HomesWithRestaurantRoute { get; private set; }
        public bool LaterHomeUsesStreet { get; private set; }

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
            HomesWithRestaurantRoute = placed
                .Where(tile => tile.category == ConstructionCategory.Residence)
                .Count(home => board.ResidenceServiceRoute(home.id,
                    ConstructionCategory.Restaurant).Count > 0);
            // A street only completes this beat if it is actually used by a
            // later resident to reach a service. Placing a decorative street
            // beside the first home must not count as solving the access need.
            LaterHomeUsesStreet = placed
                .Where(tile => tile.category == ConstructionCategory.Residence)
                .Skip(1)
                .Any(home => board.ResidenceServiceRoute(home.id,
                        ConstructionCategory.Restaurant)
                        .Concat(board.ResidenceServiceRoute(home.id,
                            ConstructionCategory.Workshop))
                        .Any(routeTile => routeTile.category == ConstructionCategory.Street));
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
                HomesWithRestaurantRoute < 2 || !LaterHomeUsesStreet
                    ? ConstructionStoryStage.BuildStreet :
                !restaurantCrowded
                    ? ConstructionStoryStage.AwaitRestaurantCrowding :
                !Has(ConstructionCategory.Supermarket)
                    ? ConstructionStoryStage.BuildSupermarket :
                ConstructionStoryStage.FreeBuild;
            return Stage;
        }

        // The legacy story suggests what to try next without locking choices.
        // Its day-one oak observation does not apply to digital hand Fill.
        public bool IsUnlocked(ConstructionCategory category) =>
            Stage != ConstructionStoryStage.WatchSquirrelEat &&
            Stage != ConstructionStoryStage.BoardComplete &&
            Enum.IsDefined(typeof(ConstructionCategory), category);

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
            var reachableGreen = board.AnimalReachableGreenTiles(
                ConstructionBoardModel.StarterOakId,
                useLegacyEightNeighbours: true);
            if (saved.newWildlifeArrived &&
                (!saved.squirrelAte || reachableGreen.Count < 2 ||
                    !reachableGreen.Any(tile => tile.planting == GreenPlanting.Meadow)) ||
                saved.residentAte &&
                    !board.FirstResidenceCanReachService(
                        ConstructionCategory.Restaurant,
                        useLegacyEightNeighbours: true) &&
                    !board.FirstResidenceCanReachService(
                        ConstructionCategory.Supermarket,
                        useLegacyEightNeighbours: true) ||
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
