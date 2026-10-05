using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Data
{
    [Serializable]
    public sealed class SessionSaveData
    {
        public int schemaVersion = 2;
        public string savedAtUtc;
        public string mode = "Sandbox";
        public double elapsedSimulationSeconds;
        public int speedMultiplier = 1;
        public List<RoomPlacementData> rooms = new();
        public int residentCount = 4;
        public int operatingFoodShopCount = 2;
        public bool supermarketOperating = true;
        public float resourceBalance = 8f;
        public float cumulativeResourceIncome;
        public float cumulativeResourceSpending;
        public float unstoredResourceSurplus;
        public float peakResourceBalance = 8f;
        public List<ResidentSaveData> residents = new();
        public string prospectiveResidenceId;
        public int nextResidentNumber = 5;
        public int cumulativeResidentArrivals;
        public int cumulativeResidentRelocations;
        public int cumulativeResidentDepartures;
        public int peakResidentCount = 4;
        public List<OakTreeSaveData> oakTrees = new();
        public int treesFelled;
        public int treesPlanted;
        public int treesMatured;
        public int roomMovements;
        public int lastRoomMovementDay;
        public int lastLayoutPlanningDay;
        public List<ShrubShelterSaveData> shrubShelters = new();
        public DailyOutcomeSaveData dailyOutcome;
        public List<PlayerFoodSourceSaveData> playerFoodSources = new();
        public int lastManualFeedingDay;
        public int squirrelDemoCachePortions;
        public List<int> squirrelCachePortions = new();
        public int pigeonDeaths;
        public int squirrelDeaths;
        public int hedgehogDeaths;
        public int foxDeaths;
        public int starvationDeaths;
        public int trafficDeaths;
        public List<AnimalDeathBreakdownData> animalDeathBreakdown = new();
        public List<NaturalFoodSaveData> naturalFoodSources = new();
        public List<AnimalNeedSaveData> animalNeeds = new();
        public int workerFeedDayNumber;
        public List<string> workerFedResidentIds = new();
        public List<WasteRoomSaveData> wasteRooms = new();
        public List<BlockedWasteSaveData> blockedWaste = new();
    }

    [Serializable]
    public sealed class WasteRoomSaveData
    {
        public string id;
        public int units;
    }

    [Serializable]
    public sealed class BlockedWasteSaveData
    {
        public string producerRoomId;
        public int units;
    }

    [Serializable]
    public sealed class ResidentSaveData
    {
        public string id;
        public string residenceId;
        public string assignedOfficeId;
        public string assignedFoodShopId;
        public int dissatisfiedDays;
        public int leaveCountdownDays;
        public float lastEfficiency = 1f;
    }

    [Serializable]
    public sealed class OakTreeSaveData
    {
        public string roomId;
        public string stage = "Mature";
        public int completeDaysSincePlanting = 2;
    }

    [Serializable]
    public sealed class ShrubShelterSaveData
    {
        public string roomId;
        public int readyDay;
    }

    [Serializable]
    public sealed class DailyOutcomeSaveData
    {
        public int pendingDay;
        public int pendingMovedRooms;
        public int previousTotalDeaths;
        public int previousStarvationDeaths;
        public int previousTrafficDeaths;
        public int previousPredationDeaths;
        public bool predationHistoryKnown;
        public int lastDay;
        public int lastMovedRooms;
        public int lastWorkingResidents;
        public bool lastWorkingResidentsKnown;
        public float lastProduction;
        public int lastSpending;
        public float lastClosingBalance;
        public int lastDeaths;
        public int lastStarvationDeaths;
        public int lastTrafficDeaths;
        public int lastPredationDeaths;
        public int lastWasteIssues;
        public bool lastMealsKnown;
        public int lastFedAnimals;
        public int lastLivingAnimals;
        public int lastFedPigeons;
        public int lastLivingPigeons;
        public int lastSeedPortionsLeft;
    }

    [Serializable]
    public sealed class PlayerFoodSourceSaveData
    {
        public string id;
        public string roomId;
        public float x;
        public float y;
        public float z;
        public int portions = 5;
        public float remainingLifetime = 30f;
        public bool playerPlaced = true;
    }

    [Serializable]
    public sealed class NaturalFoodSaveData
    {
        public string roomId;
        public string kind;
        public int portions = 1;
    }

    [Serializable]
    public sealed class AnimalNeedSaveData
    {
        public string id;
        public string species;
        public int hungerDays;
        public bool ateToday;
    }
}
