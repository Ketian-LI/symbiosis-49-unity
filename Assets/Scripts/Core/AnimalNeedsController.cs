using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class AnimalNeedsController : MonoBehaviour
    {
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly List<FoxDemoAgent> foxes = new();
        private readonly Dictionary<IWildlifeLayoutAgent, string> idsByAgent = new();
        private readonly Dictionary<WildlifeVitality, string> idsByVitality = new();
        private readonly Dictionary<string, IWildlifeLayoutAgent> agentsById = new();

        private GameRuntimeController runtime;
        private ResourceEconomyController resourceEconomy;
        private NaturalFoodController naturalFood;
        private PlayerFeedingController playerFeeding;
        private AnimalMortalityController mortality;

        public event Action StateChanged;
        public AnimalNeedsModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            NaturalFoodController naturalFoodController,
            PlayerFeedingController feedingController,
            AnimalMortalityController mortalityController,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            IEnumerable<FoxDemoAgent> foxAgents)
        {
            runtime = runtimeController;
            resourceEconomy = economyController;
            naturalFood = naturalFoodController;
            playerFeeding = feedingController;
            mortality = mortalityController;
            pigeons.AddRange(pigeonAgents ?? Array.Empty<PigeonDemoAgent>());
            squirrels.AddRange(squirrelAgents ?? Array.Empty<SquirrelDemoAgent>());
            hedgehogs.AddRange(hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>());
            foxes.AddRange(foxAgents ?? Array.Empty<FoxDemoAgent>());
            Model = new AnimalNeedsModel();

            RegisterAgents(pigeons, "pigeon", WildlifeSpecies.Pigeon);
            RegisterAgents(squirrels, "squirrel", WildlifeSpecies.Squirrel);
            RegisterAgents(hedgehogs, "hedgehog", WildlifeSpecies.Hedgehog);
            RegisterAgents(foxes, "fox", WildlifeSpecies.Fox);

            foreach (var agent in squirrels.Cast<IWildlifeLayoutAgent>()
                         .Concat(hedgehogs)
                         .Concat(foxes))
            {
                var vitality = VitalityOf(agent);
                if (vitality == null)
                {
                    continue;
                }
                idsByVitality[vitality] = idsByAgent[agent];
                vitality.Respawned += HandleRespawned;
            }
            resourceEconomy.SettlementPreparing += HandleSettlementPreparing;
            playerFeeding.AnimalAte += HandleAnimalAte;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (resourceEconomy != null)
            {
                resourceEconomy.SettlementPreparing -= HandleSettlementPreparing;
            }
            if (playerFeeding != null)
            {
                playerFeeding.AnimalAte -= HandleAnimalAte;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            foreach (var vitality in idsByVitality.Keys)
            {
                if (vitality != null)
                {
                    vitality.Respawned -= HandleRespawned;
                }
            }
        }

        public void RestoreSession(IEnumerable<AnimalNeedSaveData> states)
        {
            Model?.Restore(states);
            RefreshWarnings();
            StateChanged?.Invoke();
        }

        public int HungerDaysOf(IWildlifeLayoutAgent agent)
        {
            if (agent == null || !idsByAgent.TryGetValue(agent, out var id) ||
                !Model.Animals.TryGetValue(id, out var state))
            {
                return 0;
            }
            return state.hungerDays;
        }

        public bool NeedsMealOf(IWildlifeLayoutAgent agent)
        {
            return NeedsMeal(agent);
        }

        public void RegisterPredationMeal(IWildlifeLayoutAgent predator)
        {
            HandleAnimalAte(predator);
        }

        private void HandleSettlementPreparing(int completedDay)
        {
            AllocateAvailableFood();
            var livingIds = agentsById
                .Where(pair => IsAlive(pair.Value))
                .Select(pair => pair.Key)
                .ToArray();
            foreach (var id in Model.CompleteDay(livingIds))
            {
                if (!runtime.HasActiveRun || !agentsById.TryGetValue(id, out var agent))
                {
                    break;
                }
                if (agent is PigeonDemoAgent pigeon)
                {
                    mortality.PreparePigeonDeath(pigeon, AnimalDeathCause.Starvation);
                    pigeon.Kill();
                }
                else
                {
                    VitalityOf(agent)?.Kill(AnimalDeathCause.Starvation);
                }
            }
            RefreshWarnings();
            StateChanged?.Invoke();
        }

        private void AllocateAvailableFood()
        {
            foreach (var pigeon in pigeons.Where(item => item.IsAlive))
            {
                TryFeedFromNatural(pigeon, NaturalFoodKind.Seed, NaturalFoodKind.DiscardedFood);
            }
            foreach (var squirrel in squirrels.Where(item => item.IsAlive))
            {
                if (NeedsMeal(squirrel) && squirrel.TryConsumeCachedPortion())
                {
                    Model.MarkMeal(idsByAgent[squirrel]);
                }
                else
                {
                    TryFeedFromNatural(squirrel, NaturalFoodKind.Nut, NaturalFoodKind.DiscardedFood);
                }
            }
            foreach (var hedgehog in hedgehogs.Where(item => item.IsAlive))
            {
                TryFeedFromNatural(hedgehog, NaturalFoodKind.Insect);
            }
            foreach (var fox in foxes.Where(item => item.IsAlive))
            {
                TryFeedFromNatural(fox, NaturalFoodKind.DiscardedFood);
            }
        }

        private void TryFeedFromNatural(IWildlifeLayoutAgent agent, params NaturalFoodKind[] kinds)
        {
            if (!NeedsMeal(agent))
            {
                return;
            }
            foreach (var kind in kinds)
            {
                if (!naturalFood.TryConsumeAny(kind))
                {
                    continue;
                }
                Model.MarkMeal(idsByAgent[agent]);
                return;
            }
        }

        private bool NeedsMeal(IWildlifeLayoutAgent agent)
        {
            return idsByAgent.TryGetValue(agent, out var id) &&
                   Model.Animals.TryGetValue(id, out var state) &&
                   !state.ateToday;
        }

        private void HandleAnimalAte(IWildlifeLayoutAgent agent)
        {
            if (agent != null && idsByAgent.TryGetValue(agent, out var id))
            {
                Model.MarkMeal(id);
                RefreshWarnings();
                StateChanged?.Invoke();
            }
        }

        private void HandleRespawned(WildlifeVitality vitality)
        {
            if (!idsByVitality.TryGetValue(vitality, out var id))
            {
                return;
            }
            Model.ResetIndividual(id);
            if (vitality.Species == WildlifeSpecies.Squirrel &&
                agentsById[id] is SquirrelDemoAgent squirrel)
            {
                squirrel.RestoreCache(0);
            }
            RefreshWarnings();
            StateChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            Model?.Reset();
            RefreshWarnings();
            StateChanged?.Invoke();
        }

        private void RefreshWarnings()
        {
            foreach (var pigeon in pigeons)
            {
                var id = idsByAgent[pigeon];
                var hungry = Model.Animals[id].hungerDays >= 1;
                pigeon.SetNeedWarnings(hungry, false, false);
            }
        }

        private void RegisterAgents<T>(IReadOnlyList<T> agents, string prefix, WildlifeSpecies species)
            where T : MonoBehaviour, IWildlifeLayoutAgent
        {
            for (var index = 0; index < agents.Count; index++)
            {
                var id = $"{prefix}-{index + 1:00}";
                Model.Register(id, species);
                idsByAgent[agents[index]] = id;
                agentsById[id] = agents[index];
            }
        }

        private static WildlifeVitality VitalityOf(IWildlifeLayoutAgent agent)
        {
            return (agent as Component)?.GetComponent<WildlifeVitality>();
        }

        private static bool IsAlive(IWildlifeLayoutAgent agent)
        {
            return agent switch
            {
                PigeonDemoAgent pigeon => pigeon.IsAlive,
                SquirrelDemoAgent squirrel => squirrel.IsAlive,
                HedgehogDemoAgent hedgehog => hedgehog.IsAlive,
                FoxDemoAgent fox => fox.IsAlive,
                _ => false
            };
        }
    }
}
