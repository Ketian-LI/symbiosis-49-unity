using System;
using UnityEngine;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct ResourceSettlement
    {
        public ResourceSettlement(
            int dayNumber,
            float openingBalance,
            float production,
            int foodServiceCost,
            float closingBalance,
            float unstoredSurplus)
        {
            DayNumber = dayNumber;
            OpeningBalance = openingBalance;
            Production = production;
            FoodServiceCost = foodServiceCost;
            ClosingBalance = closingBalance;
            UnstoredSurplus = unstoredSurplus;
        }

        public int DayNumber { get; }
        public float OpeningBalance { get; }
        public float Production { get; }
        public int FoodServiceCost { get; }
        public float ClosingBalance { get; }
        public float UnstoredSurplus { get; }
        public bool Failed => ClosingBalance < 0f;
    }

    [Serializable]
    public sealed class ResourceEconomyModel
    {
        public const float StartingBalance = 8f;
        public const float MaximumBalance = 20f;
        public const int FeedActionCost = 1;
        public const int EmergencyCollectionCost = 2;
        public const int PlantTreeCost = 6;
        public const int MoveOneCellRoomCost = 2;
        public const int MoveTwoCellRoomCost = 3;

        public float Balance { get; private set; } = StartingBalance;
        public float CumulativeIncome { get; private set; }
        public float CumulativeSpending { get; private set; }
        public float UnstoredSurplus { get; private set; }
        public float PeakBalance { get; private set; } = StartingBalance;

        public bool CanAfford(int cost)
        {
            return cost >= 0 && Balance + 0.0001f >= cost;
        }

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            Balance -= cost;
            CumulativeSpending += cost;
            return true;
        }

        public ResourceSettlement SettleDay(
            int dayNumber,
            float completedWorkProduction,
            int foodServiceCost)
        {
            var opening = Balance;
            var production = Mathf.Max(0f, completedWorkProduction);
            var spending = Mathf.Max(0, foodServiceCost);
            var uncapped = Balance + production - spending;
            var surplus = Mathf.Max(0f, uncapped - MaximumBalance);

            Balance = Mathf.Min(MaximumBalance, uncapped);
            CumulativeIncome += production;
            CumulativeSpending += spending;
            UnstoredSurplus += surplus;
            PeakBalance = Mathf.Max(PeakBalance, Balance);

            return new ResourceSettlement(
                Math.Max(1, dayNumber),
                opening,
                production,
                spending,
                Balance,
                surplus);
        }

        public void Restore(
            float balance,
            float cumulativeIncome,
            float cumulativeSpending,
            float unstoredSurplus,
            float peakBalance)
        {
            Balance = Mathf.Min(MaximumBalance, balance);
            CumulativeIncome = Mathf.Max(0f, cumulativeIncome);
            CumulativeSpending = Mathf.Max(0f, cumulativeSpending);
            UnstoredSurplus = Mathf.Max(0f, unstoredSurplus);
            PeakBalance = Mathf.Max(Balance, peakBalance);
        }

        public void Reset()
        {
            Balance = StartingBalance;
            CumulativeIncome = 0f;
            CumulativeSpending = 0f;
            UnstoredSurplus = 0f;
            PeakBalance = StartingBalance;
        }

        public static int FoodServiceCost(int residentCount, int operatingFoodShopCount)
        {
            var residents = Mathf.Max(0, residentCount);
            var shops = Mathf.Max(0, operatingFoodShopCount);
            if (residents == 0 || shops == 0)
            {
                return 0;
            }

            var servedResidents = Mathf.Min(residents, shops * 4);
            var baseOccupancy = servedResidents / shops;
            var extra = servedResidents % shops;
            var cost = 0;
            for (var index = 0; index < shops; index++)
            {
                var occupancy = baseOccupancy + (index < extra ? 1 : 0);
                cost += occupancy switch
                {
                    <= 0 => 0,
                    <= 2 => 1,
                    _ => 2
                };
            }

            return cost;
        }

        public static int RoomMovementCost(int cellCount)
        {
            return cellCount <= 0
                ? 0
                : cellCount == 1
                    ? MoveOneCellRoomCost
                    : MoveTwoCellRoomCost;
        }
    }
}
