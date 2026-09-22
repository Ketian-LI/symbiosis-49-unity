using UnityEngine;

namespace UrbanWildlifeRooms.Core
{
    public enum EcologicalMetricKind
    {
        HumanFunction,
        FoodAccessibility,
        HabitatProvision,
        AnimalSafety
    }

    public readonly struct EcologicalMetricsSnapshot
    {
        public EcologicalMetricsSnapshot(float human, float food, float habitat, float safety)
        {
            HumanFunction = Mathf.Clamp01(human);
            FoodAccessibility = Mathf.Clamp01(food);
            HabitatProvision = Mathf.Clamp01(habitat);
            AnimalSafety = Mathf.Clamp01(safety);
        }

        public float HumanFunction { get; }
        public float FoodAccessibility { get; }
        public float HabitatProvision { get; }
        public float AnimalSafety { get; }

        public float ValueOf(EcologicalMetricKind kind)
        {
            return kind switch
            {
                EcologicalMetricKind.HumanFunction => HumanFunction,
                EcologicalMetricKind.FoodAccessibility => FoodAccessibility,
                EcologicalMetricKind.HabitatProvision => HabitatProvision,
                _ => AnimalSafety
            };
        }
    }

    public static class EcologicalMetricsModel
    {
        public static EcologicalMetricsSnapshot Calculate(
            int wastePenaltyPercent,
            float averageCommuteEfficiency,
            int availableFoodPortions,
            int livingAnimals,
            int matureOakTrees,
            int hungryAnimals,
            int totalDeaths,
            int trafficDeaths)
        {
            var wasteFunction = 1f - Mathf.Clamp(wastePenaltyPercent, 0, 100) / 100f;
            var human = Mathf.Min(wasteFunction, Mathf.Clamp01(averageCommuteEfficiency));
            var food = livingAnimals <= 0
                ? 1f
                : Mathf.Clamp01(availableFoodPortions / (float)livingAnimals);
            // Pigeon houses, shrub/park cover and fox den are fixed. The final
            // quarter reflects the four oak habitats' mature-tree provision.
            var habitat = 0.75f + 0.25f * Mathf.Clamp01(matureOakTrees / 4f);
            var hungerRisk = livingAnimals <= 0
                ? 0f
                : Mathf.Clamp01(hungryAnimals / (float)livingAnimals);
            var deathRisk = Mathf.Clamp01(totalDeaths / 5f);
            var trafficRisk = Mathf.Clamp01(trafficDeaths / 5f);
            var safety = 1f - Mathf.Clamp01(hungerRisk * 0.45f + deathRisk * 0.35f + trafficRisk * 0.20f);
            return new EcologicalMetricsSnapshot(human, food, habitat, safety);
        }
    }
}
