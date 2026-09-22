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
            var commute = residentStates != null && residentStates.Count > 0
                ? residentStates.Average(item => item.lastEfficiency)
                : 1f;
            var naturalPortions = naturalFood.Model?.TotalPortions ?? 0;
            var playerPortions = playerFood.Model?.Sources.Values.Sum(item => item.portions) ?? 0;
            var living = population.LivingCount(WildlifeSpecies.Pigeon) +
                         population.LivingCount(WildlifeSpecies.Squirrel) +
                         population.LivingCount(WildlifeSpecies.Hedgehog) +
                         population.LivingCount(WildlifeSpecies.Fox);
            var matureTrees = oakTrees.Model?.Trees.Values.Count(item => item.stage == OakTreeStage.Mature) ?? 0;
            var hungry = needs.Model?.Animals.Values.Count(item => item.hungerDays >= 1) ?? 0;
            Snapshot = EcologicalMetricsModel.Calculate(
                waste.HumanFunctionPenalty,
                commute,
                naturalPortions + playerPortions,
                living,
                matureTrees,
                hungry,
                mortality.Model?.TotalDeaths ?? 0,
                mortality.Model?.TrafficDeaths ?? 0);
            StateChanged?.Invoke();
        }
    }
}
