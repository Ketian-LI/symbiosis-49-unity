using System;
using System.Collections.Generic;

namespace UrbanWildlifeRooms.Data
{
    [Serializable]
    public sealed class SessionSaveData
    {
        public int schemaVersion = 1;
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
        public List<PlayerFoodSourceSaveData> playerFoodSources = new();
        public int squirrelDemoCachePortions;
        public List<int> squirrelCachePortions = new();
        public int pigeonDeaths;
        public int squirrelDeaths;
        public int hedgehogDeaths;
        public int foxDeaths;
        public int starvationDeaths;
        public int trafficDeaths;
        public List<NaturalFoodSaveData> naturalFoodSources = new();
        public List<AnimalNeedSaveData> animalNeeds = new();
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
