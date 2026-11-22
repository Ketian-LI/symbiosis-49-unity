using System;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionUtilizationKind
    {
        None,
        GreenFood,
        Resident,
        HumanService,
        SquareVisit
    }

    public enum ConstructionUtilizationLevel
    {
        None,
        Waiting,
        Full,
        Idle,
        Blocked
    }

    public enum ConstructionUtilizationReason
    {
        None,
        NoFoodAvailable,
        FoodRemaining,
        YieldLostAtCapacity,
        ResidentDeparted,
        HomeBlockedByWaste,
        ResidentMissedMeal,
        ResidentMissedWork,
        SpareServiceCapacity,
        ServiceBlockedByWaste,
        UnreachableServiceDemand,
        SquareNoVisitor,
        SquareUnreachable
    }

    // This is a forecast for the current day, not a second resource stock.
    // Used/Available means animal portions eaten/available on green plots,
    // residents completing a full cycle/1 at home, filled/total service slots,
    // or completed-cycle square visits/1.
    public readonly struct ConstructionUtilizationStatus
    {
        public ConstructionUtilizationKind Kind { get; }
        public ConstructionUtilizationLevel Level { get; }
        public ConstructionUtilizationReason Reason { get; }
        public int Used { get; }
        public int Available { get; }
        public int Idle => Math.Max(0, Available - Used);
        public bool ShowOnTile => Level is ConstructionUtilizationLevel.Full or
            ConstructionUtilizationLevel.Idle or ConstructionUtilizationLevel.Blocked;

        public ConstructionUtilizationStatus(ConstructionUtilizationKind kind,
            ConstructionUtilizationLevel level,
            ConstructionUtilizationReason reason, int used, int available)
        {
            Kind = kind;
            Level = level;
            Reason = reason;
            Available = Math.Max(0, available);
            Used = Math.Min(Available, Math.Max(0, used));
        }
    }

    public static class ConstructionTileUtilizationModel
    {
        public static ConstructionUtilizationStatus Evaluate(ConstructionTileData tile,
            ConstructionRunModel run, ConstructionHumanDayResult human,
            ConstructionEcologyDayResult ecology,
            ConstructionWasteAvailability waste)
        {
            if (tile == null || run == null) return default;
            if (tile.category == ConstructionCategory.Green)
            {
                var change = ecology?.foodChanges?.FirstOrDefault(item =>
                    item.tileId == tile.id);
                return change == null
                    ? new ConstructionUtilizationStatus(
                        ConstructionUtilizationKind.GreenFood,
                        ConstructionUtilizationLevel.Waiting,
                        ConstructionUtilizationReason.NoFoodAvailable, 0, 0)
                    : Food(change.eaten, change.remaining,
                        run.GreenDailyYield(tile.id), change.produced,
                        run.FoodCapacity(tile.id), run.FoodStock(tile.id));
            }
            if (human?.residents == null) return default;
            if (tile.category == ConstructionCategory.Residence)
            {
                var person = human.residents.FirstOrDefault(item =>
                    item.homeTileId == tile.id);
                return Residence(run.HasResident(tile.id),
                    person?.worked == true, person?.ate == true,
                    waste?.BlockedHomes?.Contains(tile.id) == true);
            }
            if (tile.category == ConstructionCategory.Square)
            {
                var squareUsed = human.residents.Count(person =>
                    person.visitedSquare && person.squareTileId == tile.id);
                if (squareUsed > 0)
                    return new ConstructionUtilizationStatus(
                        ConstructionUtilizationKind.SquareVisit,
                        ConstructionUtilizationLevel.Full,
                        ConstructionUtilizationReason.None, squareUsed, 1);
                var ready = human.residents.Where(person =>
                    person.completedCycle).ToArray();
                var squareReachable = ready.Any(person =>
                    run.ResidenceRouteTo(person.homeTileId, tile.id).Count > 0);
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.SquareVisit,
                    ready.Length > 0 && !squareReachable
                        ? ConstructionUtilizationLevel.Blocked
                        : ConstructionUtilizationLevel.Idle,
                    ready.Length > 0 && !squareReachable
                        ? ConstructionUtilizationReason.SquareUnreachable
                        : ConstructionUtilizationReason.SquareNoVisitor,
                    0, 1);
            }
            if (tile.category is not (ConstructionCategory.Workshop or
                ConstructionCategory.Restaurant or ConstructionCategory.Supermarket))
                return default;

            var capacity = tile.category == ConstructionCategory.Workshop
                ? ConstructionHumanModel.DailyJobCapacity(tile.category)
                : ConstructionHumanModel.DailyMealCapacity(tile, run.BuiltTiles,
                    run.UsesNeighborUpgrades);
            var used = human.residents.Count(person =>
                tile.category == ConstructionCategory.Workshop
                    ? person.workTileId == tile.id
                    : person.mealTileId == tile.id);
            var unmetNeed = tile.category == ConstructionCategory.Workshop
                ? human.WorkedCount < human.ResidentCount
                : human.MealsEaten < human.ResidentCount;
            var reachable = human.residents.Any(person =>
                run.ResidenceRouteTo(person.homeTileId, tile.id).Count > 0);
            return Service(used, capacity,
                waste?.BlockedFoodServices?.Contains(tile.id) == true,
                unmetNeed, reachable);
        }

        public static ConstructionUtilizationStatus Food(int eaten, int remaining,
            int potentialYield, int actualYield, int capacity, int openingStock)
        {
            eaten = Math.Max(0, eaten);
            remaining = Math.Max(0, remaining);
            var available = eaten + remaining;
            // This corner badge answers whether today's available food is used.
            // If every portion was eaten, it stays green even when the dawn
            // production step could not add another portion to a full store.
            if (available > 0 && remaining == 0)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.GreenFood,
                    ConstructionUtilizationLevel.Full,
                    ConstructionUtilizationReason.None, eaten, available);
            if (capacity > 0 && openingStock >= capacity &&
                potentialYield > actualYield)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.GreenFood,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.YieldLostAtCapacity,
                    eaten, available);
            if (available == 0)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.GreenFood,
                    ConstructionUtilizationLevel.Waiting,
                    ConstructionUtilizationReason.NoFoodAvailable, 0, 0);
            return new ConstructionUtilizationStatus(
                ConstructionUtilizationKind.GreenFood,
                ConstructionUtilizationLevel.Idle,
                ConstructionUtilizationReason.FoodRemaining,
                eaten, available);
        }

        public static ConstructionUtilizationStatus Residence(bool hasResident,
            bool worked, bool ate, bool blockedByWaste)
        {
            if (!hasResident)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.Resident,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.ResidentDeparted, 0, 1);
            if (blockedByWaste)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.Resident,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.HomeBlockedByWaste, 0, 1);
            if (!ate)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.Resident,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.ResidentMissedMeal, 0, 1);
            if (!worked)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.Resident,
                    ConstructionUtilizationLevel.Idle,
                    ConstructionUtilizationReason.ResidentMissedWork, 0, 1);
            return new ConstructionUtilizationStatus(
                ConstructionUtilizationKind.Resident,
                ConstructionUtilizationLevel.Full,
                ConstructionUtilizationReason.None, 1, 1);
        }

        public static ConstructionUtilizationStatus Service(int used, int capacity,
            bool blockedByWaste, bool unmetNeed, bool reachable)
        {
            if (capacity <= 0) return default;
            if (blockedByWaste)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.HumanService,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.ServiceBlockedByWaste,
                    used, capacity);
            if (used == 0 && unmetNeed && !reachable)
                return new ConstructionUtilizationStatus(
                    ConstructionUtilizationKind.HumanService,
                    ConstructionUtilizationLevel.Blocked,
                    ConstructionUtilizationReason.UnreachableServiceDemand,
                    0, capacity);
            return new ConstructionUtilizationStatus(
                ConstructionUtilizationKind.HumanService,
                used >= capacity ? ConstructionUtilizationLevel.Full :
                    ConstructionUtilizationLevel.Idle,
                used >= capacity ? ConstructionUtilizationReason.None :
                    ConstructionUtilizationReason.SpareServiceCapacity,
                used, capacity);
        }
    }
}
