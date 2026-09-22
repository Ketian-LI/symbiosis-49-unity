using System;

namespace UrbanWildlifeRooms.Data
{
    public enum RunEndReason
    {
        AnimalDeathLimit,
        NegativeResourceBalance,
        ResearchTimeExpired
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
        public float averageHabitatProvision;
        public int configuredDeathLimit = 5;
        public string researchParticipantCode;
        public float researchDurationSeconds;
        public float researchElapsedSeconds;
        public float researchOperationSeconds;

        public int TotalAnimalDeaths =>
            Math.Max(0, pigeonDeaths) +
            Math.Max(0, squirrelDeaths) +
            Math.Max(0, hedgehogDeaths) +
            Math.Max(0, foxDeaths);

        public string LocalizedEndReason(bool chinese)
        {
            return endReason switch
            {
                RunEndReason.AnimalDeathLimit => chinese
                    ? $"动物死亡达到 {Math.Max(1, configuredDeathLimit)}"
                    : Math.Max(1, configuredDeathLimit) == 5
                        ? "Five animal deaths"
                        : $"{Math.Max(1, configuredDeathLimit)} animal deaths",
                RunEndReason.ResearchTimeExpired => chinese ? "研究时长结束" : "Research duration complete",
                _ => chinese ? "资源点结算为负数" : "Negative resource balance"
            };
        }
    }
}
