using System;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class EcologicalMetricsController : MonoBehaviour
    {
        private WasteManagementController waste;
        private ResidentPopulationController residents;
        private NaturalFoodController naturalFood;
        private PlayerFeedingController playerFood;
        private AnimalPopulationController population;
        private OakTreeLifecycleController oakTrees;
        private AnimalNeedsController needs;
        private AnimalMortalityController mortality;

        public event Action StateChanged;
        public EcologicalMetricsSnapshot Snapshot { get; private set; }
        public int ResidentCount { get; private set; }
        public int NaturalFoodPortions { get; private set; }
        public int SeedFoodPortions { get; private set; }
        public int NutFoodPortions { get; private set; }
        public int InsectFoodPortions { get; private set; }
        public int DiscardedFoodPortions { get; private set; }
        public int PlayerFoodPortions { get; private set; }
        public int LivingAnimalCount { get; private set; }
        public int LivingUnfedCount { get; private set; }
        public int TotalAnimalSlots { get; private set; }
        public int MatureOakCount { get; private set; }

        public void Initialize(
            WasteManagementController wasteController,
            ResidentPopulationController residentController,
            NaturalFoodController naturalFoodController,
            PlayerFeedingController playerFoodController,
            AnimalPopulationController populationController,
            OakTreeLifecycleController oakTreeController,
            AnimalNeedsController needsController,
            AnimalMortalityController mortalityController)
        {
            waste = wasteController;
            residents = residentController;
            naturalFood = naturalFoodController;
            playerFood = playerFoodController;
            population = populationController;
            oakTrees = oakTreeController;
            needs = needsController;
            mortality = mortalityController;
            waste.StateChanged += Refresh;
            residents.StateChanged += Refresh;
            naturalFood.StateChanged += Refresh;
            playerFood.StateChanged += Refresh;
            population.StateChanged += Refresh;
            oakTrees.StateChanged += Refresh;
            needs.StateChanged += Refresh;
            mortality.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (waste != null) waste.StateChanged -= Refresh;
            if (residents != null) residents.StateChanged -= Refresh;
            if (naturalFood != null) naturalFood.StateChanged -= Refresh;
            if (playerFood != null) playerFood.StateChanged -= Refresh;
            if (population != null) population.StateChanged -= Refresh;
            if (oakTrees != null) oakTrees.StateChanged -= Refresh;
            if (needs != null) needs.StateChanged -= Refresh;
            if (mortality != null) mortality.StateChanged -= Refresh;
        }

        private void Refresh()
        {
            var residentStates = residents.Model?.Residents;
            ResidentCount = residentStates?.Count ?? 0;
            var commute = residentStates != null && residentStates.Count > 0
                ? residentStates.Average(item => item.lastEfficiency)
                : 1f;
            NaturalFoodPortions = naturalFood.Model?.TotalPortions ?? 0;
            var naturalSources = naturalFood.Model?.Sources.Values;
            SeedFoodPortions = naturalSources?.Where(item => item.kind == NaturalFoodKind.Seed)
                .Sum(item => item.portions) ?? 0;
            NutFoodPortions = naturalSources?.Where(item => item.kind == NaturalFoodKind.Nut)
                .Sum(item => item.portions) ?? 0;
            InsectFoodPortions = naturalSources?.Where(item => item.kind == NaturalFoodKind.Insect)
                .Sum(item => item.portions) ?? 0;
            DiscardedFoodPortions = naturalSources?.Where(item => item.kind == NaturalFoodKind.DiscardedFood)
                .Sum(item => item.portions) ?? 0;
            PlayerFoodPortions = playerFood.Model?.Sources.Values.Sum(item => item.portions) ?? 0;
            LivingAnimalCount = population.LivingCount(WildlifeSpecies.Pigeon) +
                         population.LivingCount(WildlifeSpecies.Squirrel) +
                         population.LivingCount(WildlifeSpecies.Hedgehog) +
                         population.LivingCount(WildlifeSpecies.Fox);
            TotalAnimalSlots = population.TotalCount(WildlifeSpecies.Pigeon) +
                               population.TotalCount(WildlifeSpecies.Squirrel) +
                               population.TotalCount(WildlifeSpecies.Hedgehog) +
                               population.TotalCount(WildlifeSpecies.Fox);
            LivingUnfedCount = needs.LivingUnfedCount;
            MatureOakCount = oakTrees.Model?.Trees.Values.Count(item => item.stage == OakTreeStage.Mature) ?? 0;
            var hungry = needs.Model?.Animals.Values.Count(item => item.hungerDays >= 1) ?? 0;
            Snapshot = EcologicalMetricsModel.Calculate(
                waste.HumanFunctionPenalty,
                commute,
                NaturalFoodPortions + PlayerFoodPortions,
                LivingAnimalCount,
                MatureOakCount,
                hungry,
                mortality.Model?.TotalDeaths ?? 0,
                mortality.Model?.TrafficDeaths ?? 0);
            StateChanged?.Invoke();
        }
    }
}
