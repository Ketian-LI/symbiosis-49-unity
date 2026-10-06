using System;
using System.Collections.Generic;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Data
{
    public enum RunEndReason
    {
        AnimalDeathLimit,
        NegativeResourceBalance,
        ResearchTimeExpired,
        InsufficientWorkers,
        InsufficientResidents,
        WildlifePopulationCollapse,
        CommunityCollapse
    }

    [Serializable]
    public sealed class RunResultsData
    {
        public RunEndReason endReason;
        public int daysSurvived = 1;
        public int bestSurvivalDays;
        public bool isNewRecord;

        public int cumulativeResourceIncome;
        public int cumulativeResourceSpending;
        public int finalResourceBalance;
        public int peakResourceBalance;

        public int finalResidents;
        public int requiredResidents;
        public int lastWorkingResidents;
        public int requiredWorkingResidents;
        public int peakResidents;
        public float averageCommuteEfficiency;
        public int arrivals;
        public int relocations;
        public int departures;

        public int roomMovements;
        public int treesFelled;
        public int treesPlanted;
        public int treesMatured;

        public int pigeonDeaths;
        public int squirrelDeaths;
        public int hedgehogDeaths;
        public int foxDeaths;
        public int starvationDeaths;
        public int trafficDeaths;
        public List<AnimalDeathBreakdownData> animalDeathBreakdown = new();
        public float averageHabitatProvision;
        public int configuredDeathLimit = 3;
        public int finalWildlifeCount;
        public int requiredWildlifeCount;
        public int finalCommunity;
        public string sandboxDifficulty = "Standard";
        public string researchParticipantCode;
        public float researchDurationSeconds;
        public float researchElapsedSeconds;
        public float researchOperationSeconds;

        public int TotalAnimalDeaths =>
            Math.Max(0, pigeonDeaths) +
            Math.Max(0, squirrelDeaths) +
            Math.Max(0, hedgehogDeaths) +
            Math.Max(0, foxDeaths);

        public AnimalDeathBreakdownData BreakdownOf(WildlifeSpecies species)
        {
            var speciesTotal = species switch
            {
                WildlifeSpecies.Pigeon => Math.Max(0, pigeonDeaths),
                WildlifeSpecies.Squirrel => Math.Max(0, squirrelDeaths),
                WildlifeSpecies.Hedgehog => Math.Max(0, hedgehogDeaths),
                WildlifeSpecies.Fox => Math.Max(0, foxDeaths),
                _ => 0
            };
            if (animalDeathBreakdown != null)
            {
                foreach (var saved in animalDeathBreakdown)
                {
                    if (saved == null || saved.species != species)
                    {
                        continue;
                    }
                    var row = saved.Clone();
                    if (row.Total <= speciesTotal)
                    {
                        row.unrecorded += speciesTotal - row.Total;
                        return row;
                    }
                    break;
                }
            }
            return new AnimalDeathBreakdownData { species = species, unrecorded = speciesTotal };
        }

        public string LocalizedEndReason(bool chinese)
        {
            return endReason switch
            {
                RunEndReason.AnimalDeathLimit => chinese
                    ? $"动物死亡达到 {Math.Max(1, configuredDeathLimit)}"
                    : $"{Math.Max(1, configuredDeathLimit)} animal deaths",
                RunEndReason.ResearchTimeExpired => chinese ? "研究时长结束" : "Research duration complete",
                RunEndReason.InsufficientWorkers => chinese
                    ? $"上班人数不足：{lastWorkingResidents}/{requiredWorkingResidents}"
                    : $"Too few workers: {lastWorkingResidents}/{requiredWorkingResidents}",
                RunEndReason.InsufficientResidents => chinese
                    ? $"居民人数不足：{finalResidents}/{Math.Max(1, requiredResidents)}"
                    : $"Too few residents: {finalResidents}/{Math.Max(1, requiredResidents)}",
                RunEndReason.WildlifePopulationCollapse => chinese
                    ? $"野生动物种群持续过低：{finalWildlifeCount}/{Math.Max(1, requiredWildlifeCount)}"
                    : $"Wildlife population remained too low: {finalWildlifeCount}/{Math.Max(1, requiredWildlifeCount)}",
                RunEndReason.CommunityCollapse => chinese
                    ? "社区活力耗尽" : "Community health depleted",
                _ => chinese ? "资源点结算为负数" : "Negative resource balance"
            };
        }
    }
}
