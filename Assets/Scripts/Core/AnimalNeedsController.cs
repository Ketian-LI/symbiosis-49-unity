using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct AnimalMealDaySummary
    {
        public AnimalMealDaySummary(int day, int fed, int living, int pigeonsFed,
            int pigeonsLiving, int seedPortionsLeft)
        {
            Day = day;
            Fed = fed;
            Living = living;
            PigeonsFed = pigeonsFed;
            PigeonsLiving = pigeonsLiving;
            SeedPortionsLeft = seedPortionsLeft;
        }

        public int Day { get; }
        public int Fed { get; }
        public int Living { get; }
        public int PigeonsFed { get; }
        public int PigeonsLiving { get; }
        public int SeedPortionsLeft { get; }
    }

    public sealed class AnimalNeedsController : MonoBehaviour
    {
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly List<FoxDemoAgent> foxes = new();
        private readonly Dictionary<IWildlifeLayoutAgent, string> idsByAgent = new();
        private readonly Dictionary<WildlifeVitality, string> idsByVitality = new();
        private readonly Dictionary<string, IWildlifeLayoutAgent> agentsById = new();
        private readonly Dictionary<IWildlifeLayoutAgent, string> homeRooms = new();

        private GameRuntimeController runtime;
        private ResourceEconomyController resourceEconomy;
        private NaturalFoodController naturalFood;
        private PlayerFeedingController playerFeeding;
        private AnimalMortalityController mortality;
        private AnimalNavigationCoordinator navigation;
        private readonly Dictionary<string, bool> foodAccessById = new();
        private float accessRefreshTimer;
        private bool refreshingFoodAccess;

        public event Action StateChanged;
        public AnimalNeedsModel Model { get; private set; }
        public AnimalMealDaySummary LastCompletedDayMeals { get; private set; }
        public int LivingUnfedCount => Model == null
            ? 0
            : agentsById.Values.Count(agent => IsAlive(agent) && NeedsMeal(agent));
        public int AnimalsWithoutFoodAccess { get; private set; }
        public string FirstFoodRiskRoomId { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            ResourceEconomyController economyController,
            NaturalFoodController naturalFoodController,
            PlayerFeedingController feedingController,
            AnimalMortalityController mortalityController,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            IEnumerable<FoxDemoAgent> foxAgents,
            IReadOnlyDictionary<IWildlifeLayoutAgent, string> wildlifeHomeRooms = null,
            AnimalNavigationCoordinator navigationCoordinator = null)
        {
            runtime = runtimeController;
            resourceEconomy = economyController;
            naturalFood = naturalFoodController;
            playerFeeding = feedingController;
            mortality = mortalityController;
            navigation = navigationCoordinator;
            pigeons.AddRange(pigeonAgents ?? Array.Empty<PigeonDemoAgent>());
            squirrels.AddRange(squirrelAgents ?? Array.Empty<SquirrelDemoAgent>());
            hedgehogs.AddRange(hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>());
            foxes.AddRange(foxAgents ?? Array.Empty<FoxDemoAgent>());
            if (wildlifeHomeRooms != null)
            {
                foreach (var pair in wildlifeHomeRooms)
                {
                    homeRooms[pair.Key] = pair.Value;
                }
            }
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
            playerFeeding.StateChanged += HandleFoodChanged;
            naturalFood.StateChanged += HandleFoodChanged;
            if (navigation != null)
                navigation.NavigationChanged += HandleFoodChanged;
            if (mortality != null)
                mortality.AnimalDied += HandleAnimalDied;
            runtime.RestartRequested += HandleRestartRequested;
            RefreshWarnings();
        }

        private void Update()
        {
            if (!Application.isPlaying || Model == null) return;
            accessRefreshTimer += Time.unscaledDeltaTime;
            if (accessRefreshTimer < 0.8f) return;
            accessRefreshTimer = 0f;
            if (RefreshFoodAccess()) StateChanged?.Invoke();
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
                playerFeeding.StateChanged -= HandleFoodChanged;
            }
            if (naturalFood != null) naturalFood.StateChanged -= HandleFoodChanged;
            if (navigation != null) navigation.NavigationChanged -= HandleFoodChanged;
            if (mortality != null) mortality.AnimalDied -= HandleAnimalDied;
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

        public bool CanReceiveWorkerMeal(IWildlifeLayoutAgent agent)
        {
            return agent != null && IsAlive(agent) && NeedsMeal(agent);
        }

        public void RegisterPredationMeal(IWildlifeLayoutAgent predator)
        {
            HandleAnimalAte(predator);
        }

        // Natural pigeon food is awarded only after the visible trip reaches
        // the source. The portion and the daily meal change together.
        public bool TryFeedFromNaturalArrival(
            IWildlifeLayoutAgent animal, string roomId, NaturalFoodKind kind)
        {
            if (animal == null || !IsAlive(animal) || !NeedsMeal(animal) ||
                !naturalFood.TryConsume(roomId, kind))
            {
                return false;
            }

            Model.MarkMeal(idsByAgent[animal]);
            RefreshWarnings();
            StateChanged?.Invoke();
            return true;
        }

        // A passing worker gives one meal directly to one animal. This does not
        // create a five-portion player food source or spend a resource point.
        public bool TryFeedFromWorker(IWildlifeLayoutAgent animal)
        {
            if (!CanReceiveWorkerMeal(animal))
            {
                return false;
            }

            Model.MarkMeal(idsByAgent[animal]);
            RefreshWarnings();
            StateChanged?.Invoke();
            return true;
        }

        private void HandleSettlementPreparing(int completedDay)
        {
            AllocateAvailableFood();
            var livingIds = agentsById.Where(pair => IsAlive(pair.Value))
                .Select(pair => pair.Key).ToArray();
            var fed = livingIds.Count(id => Model.Animals[id].ateToday);
            var livingPigeons = livingIds.Count(id =>
                Model.Animals[id].species == WildlifeSpecies.Pigeon);
            var fedPigeons = livingIds.Count(id =>
                Model.Animals[id].species == WildlifeSpecies.Pigeon &&
                Model.Animals[id].ateToday);
            var seedPortionsLeft = naturalFood.Model.Sources.Values
                .Where(source => source.kind == NaturalFoodKind.Seed)
                .Sum(source => source.portions);
            // Capture meals before CompleteDay clears ateToday and before
            // tomorrow's dawn replenishment changes the food stock.
            LastCompletedDayMeals = new AnimalMealDaySummary(completedDay, fed,
                livingIds.Length, fedPigeons, livingPigeons, seedPortionsLeft);
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
            // EditMode balance tests advance the clock without MonoBehaviour
            // updates, so use the reachability model as their trip surrogate.
            // During play, natural food must be claimed by a visible foraging
            // trip (or, for squirrels, from a cache filled by such a trip).
            if (!Application.isPlaying)
            {
                foreach (var pigeon in pigeons.Where(item => item.IsAlive))
                {
                    TryFeedFromNatural(pigeon, NaturalFoodKind.Seed,
                        NaturalFoodKind.DiscardedFood);
                }
            }
            foreach (var squirrel in squirrels.Where(item => item.IsAlive))
            {
                if (NeedsMeal(squirrel) && squirrel.TryConsumeCachedPortion())
                {
                    Model.MarkMeal(idsByAgent[squirrel]);
                }
                else if (!Application.isPlaying)
                {
                    TryFeedFromNatural(squirrel, NaturalFoodKind.Nut, NaturalFoodKind.DiscardedFood);
                }
            }
            if (!Application.isPlaying)
            {
                foreach (var hedgehog in hedgehogs.Where(item => item.IsAlive))
                    TryFeedFromNatural(hedgehog, NaturalFoodKind.Insect);
                foreach (var fox in foxes.Where(item => item.IsAlive))
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
                // A day represents repeated trips from an animal's habitat,
                // not a single sample of its animated position at midnight.
                var ate = homeRooms.TryGetValue(agent, out var homeRoom)
                    ? naturalFood.TryConsumeReachable(homeRoom, agent.Species, kind,
                        HungerDaysOf(agent))
                    : naturalFood.TryConsumeReachable(agent.AgentTransform.position, agent.Species, kind,
                        HungerDaysOf(agent));
                if (!ate)
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
            LastCompletedDayMeals = default;
            RefreshWarnings();
            StateChanged?.Invoke();
        }

        private void HandleFoodChanged()
        {
            if (RefreshFoodAccess()) StateChanged?.Invoke();
        }

        private void HandleAnimalDied(WildlifeSpecies species, AnimalDeathCause cause,
            int totalDeaths) => HandleFoodChanged();

        // A capacity-aware snapshot of food that exists now. This is a warning,
        // not a promise that a moving animal will arrive before a portion is
        // eaten or expires. Passing workers and future production are omitted.
        private bool RefreshFoodAccess()
        {
            if (refreshingFoodAccess || Model == null || naturalFood?.Model == null || navigation == null)
                return false;

            refreshingFoodAccess = true;
            try
            {
                return EvaluateFoodAccess();
            }
            finally
            {
                refreshingFoodAccess = false;
            }
        }

        private bool EvaluateFoodAccess()
        {
            var requests = agentsById
                .Where(pair => IsAlive(pair.Value) &&
                               Model.Animals.TryGetValue(pair.Key, out var state) &&
                               !state.ateToday &&
                               !(pair.Value is SquirrelDemoAgent squirrel && squirrel.CachePortions > 0))
                .OrderByDescending(pair => Model.Animals[pair.Key].hungerDays)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .ToArray();
            var next = new Dictionary<string, bool>(StringComparer.Ordinal);
            var requestIds = new HashSet<string>(requests.Select(pair => pair.Key),
                StringComparer.Ordinal);
            foreach (var pair in agentsById)
                if (IsAlive(pair.Value) && !requestIds.Contains(pair.Key))
                    next[pair.Key] = true;

            var slots = new List<FoodAccessSlot>();
            foreach (var source in naturalFood.Model.Sources.Values
                         .Where(item => item != null && item.portions > 0)
                         .OrderBy(item => item.roomId, StringComparer.Ordinal)
                         .ThenBy(item => item.kind))
                for (var portion = 0; portion < Mathf.Min(source.portions, requests.Length); portion++)
                    slots.Add(new FoodAccessSlot(source.roomId, source.kind, false));
            if (playerFeeding?.Model != null)
                foreach (var source in playerFeeding.Model.Sources.Values
                             .Where(item => item != null && item.portions > 0 &&
                                            item.remainingLifetime > 0f)
                             .OrderBy(item => item.id, StringComparer.Ordinal))
                    for (var portion = 0; portion < Mathf.Min(source.portions, requests.Length); portion++)
                        slots.Add(new FoodAccessSlot(source.roomId, default, true));

            var candidates = new bool[requests.Length, slots.Count];
            for (var animalIndex = 0; animalIndex < requests.Length; animalIndex++)
            {
                var agent = requests[animalIndex].Value;
                if (!homeRooms.TryGetValue(agent, out var homeRoom)) continue;
                var routeMap = navigation.NavigationMapFor(agent);
                for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                {
                    var slot = slots[slotIndex];
                    if (!CanEat(agent.Species, slot)) continue;
                    candidates[animalIndex, slotIndex] =
                        HabitatFoodNetworkModel.CanReachSource(routeMap, homeRoom,
                            agent.Species, slot.RoomId, out _);
                }
            }

            var owner = Enumerable.Repeat(-1, slots.Count).ToArray();
            bool Match(int animalIndex, bool[] visited)
            {
                for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                {
                    if (!candidates[animalIndex, slotIndex] || visited[slotIndex]) continue;
                    visited[slotIndex] = true;
                    if (owner[slotIndex] >= 0 && !Match(owner[slotIndex], visited)) continue;
                    owner[slotIndex] = animalIndex;
                    return true;
                }
                return false;
            }
            for (var index = 0; index < requests.Length; index++)
                next[requests[index].Key] = Match(index, new bool[slots.Count]);

            var nextRiskCount = 0;
            string nextFirstRoom = null;
            foreach (var pair in requests)
            {
                if (next[pair.Key]) continue;
                nextRiskCount++;
                if (nextFirstRoom == null)
                    homeRooms.TryGetValue(pair.Value, out nextFirstRoom);
            }
            var changed = nextRiskCount != AnimalsWithoutFoodAccess ||
                          nextFirstRoom != FirstFoodRiskRoomId ||
                          next.Count != foodAccessById.Count ||
                          next.Any(pair => !foodAccessById.TryGetValue(pair.Key, out var previous) ||
                                           previous != pair.Value);
            foodAccessById.Clear();
            foreach (var pair in next) foodAccessById[pair.Key] = pair.Value;
            AnimalsWithoutFoodAccess = nextRiskCount;
            FirstFoodRiskRoomId = nextFirstRoom;
            foreach (var pair in agentsById)
                if (Model.Animals.TryGetValue(pair.Key, out var state))
                {
                    var access = next.TryGetValue(pair.Key, out var canEat) && canEat;
                    (pair.Value as Component)?.GetComponent<AnimalNeedIndicator>()?
                        .SetFoodAccess(state.hungerDays, access);
                    var hungerWarning = state.hungerDays > 0 || IsAlive(pair.Value) && !access;
                    if (pair.Value is PigeonDemoAgent pigeon)
                        pigeon.SetNeedWarnings(hungerWarning, false, false);
                    else
                        (pair.Value as Component)?.GetComponent<AnimalFollowSelection>()?
                            .SetNeedWarnings(hungerWarning, false, false, IsAlive(pair.Value));
                }
            return changed;
        }

        private static bool CanEat(WildlifeSpecies species, FoodAccessSlot slot)
        {
            if (slot.PlayerPlaced)
                return species is WildlifeSpecies.Pigeon or WildlifeSpecies.Squirrel;
            return species switch
            {
                WildlifeSpecies.Pigeon => slot.Kind is NaturalFoodKind.Seed or NaturalFoodKind.DiscardedFood,
                // In Play Mode squirrels actually collect nuts; the EditMode
                // settlement surrogate's scrap fallback is not a visible trip.
                WildlifeSpecies.Squirrel => slot.Kind == NaturalFoodKind.Nut,
                WildlifeSpecies.Hedgehog => slot.Kind == NaturalFoodKind.Insect,
                WildlifeSpecies.Fox => slot.Kind == NaturalFoodKind.DiscardedFood,
                _ => false
            };
        }

        private readonly struct FoodAccessSlot
        {
            public FoodAccessSlot(string roomId, NaturalFoodKind kind, bool playerPlaced)
            {
                RoomId = roomId;
                Kind = kind;
                PlayerPlaced = playerPlaced;
            }
            public string RoomId { get; }
            public NaturalFoodKind Kind { get; }
            public bool PlayerPlaced { get; }
        }

        private void RefreshWarnings()
        {
            RefreshFoodAccess();
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
