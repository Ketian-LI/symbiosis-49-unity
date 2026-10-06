using System;
using System.Collections.Generic;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Endless-mode pressure is deliberately separate from the Research death limit.
    // All values here are first-pass tuning parameters, not ecological predictions.
    public sealed class EndlessBalanceModel
    {
        public const int StartingCommunity = 60;
        public const int MaximumCommunity = 100;
        public const int WildlifeEmergencyFloor = 10;
        public const int WildlifeEmergencyGraceDays = 2;
        public const int RecoveryGoodDays = 3;

        private readonly int[] recoveryDays = new int[4];
        private readonly EndlessDifficultyRules rules;

        public EndlessBalanceModel(EndlessDifficulty difficulty = EndlessDifficulty.Standard)
            : this(EndlessDifficultySettings.Preset(difficulty))
        {
        }

        public EndlessBalanceModel(EndlessDifficultySettings settings)
        {
            rules = EndlessDifficultyRules.For(settings);
            Community = rules.StartingCommunity;
        }

        public EndlessDifficulty Difficulty => rules.Difficulty;
        public int WildlifeGraceDays => rules.WildlifeGraceDays;
        public int LastSettledDay { get; private set; }
        public int Community { get; private set; } = StartingCommunity;
        public int LastCommunityChange { get; private set; }
        public int CriticalWildlifeDays { get; private set; }
        public int LastWildlifeCount { get; private set; } = AnimalPopulationDefaults.Total;
        public int CurrentWildlifeFloor => rules.WildlifeFloor;
        public bool CommunityCollapse => Community <= 0;
        public bool WildlifeCollapse => CriticalWildlifeDays >= rules.WildlifeGraceDays;

        // The agreed emergency floor is fixed. Habitat capacity and warning
        // targets may evolve, but surviving longer must not silently raise the
        // game-over threshold.
        public static int WildlifeFloorForDay(int day) => WildlifeEmergencyFloor;

        public static int CommunityChangeFor(int completedRoutes, int residents)
        {
            return EndlessDifficultyRules.For(EndlessDifficulty.Standard)
                .CommunityChangeFor(completedRoutes, residents);
        }

        // This is an arrival ceiling derived from usable habitat rooms, not
        // a guarantee that food will actually be eaten. Never create actors
        // beyond the existing model/animation slots.
        public static int HabitatCapacityFor(WildlifeSpecies species,
            AnimalFoodAccessSnapshot access, int actorSlots)
        {
            var reachable = species switch
            {
                WildlifeSpecies.Pigeon => access.Pigeon * 3,
                WildlifeSpecies.Squirrel => access.Squirrel,
                WildlifeSpecies.Hedgehog => access.Hedgehog,
                WildlifeSpecies.Fox => access.FoxPrey,
                _ => 0
            };
            return Math.Clamp(reachable, 0, Math.Max(0, actorSlots));
        }

        public void SettleDay(int day, int completedRoutes, int residents, int livingWildlife)
        {
            if (day <= LastSettledDay) return;
            LastSettledDay = day;
            LastCommunityChange = rules.CommunityChangeFor(completedRoutes, residents);
            Community = Math.Clamp(Community + LastCommunityChange, 0, MaximumCommunity);
            LastWildlifeCount = Math.Max(0, livingWildlife);
            CriticalWildlifeDays = LastWildlifeCount < CurrentWildlifeFloor
                ? CriticalWildlifeDays + 1
                : 0;
        }

        // Only one dormant visual actor may return per day. A restored actor
        // represents recruitment/arrival, never a 20-second resurrection.
        public WildlifeSpecies? SelectArrival(
            IReadOnlyDictionary<WildlifeSpecies, bool> eligible,
            IReadOnlyDictionary<WildlifeSpecies, int> living,
            IReadOnlyDictionary<WildlifeSpecies, int> availableSlots)
        {
            WildlifeSpecies? selected = null;
            foreach (var species in new[]
                     {
                         WildlifeSpecies.Fox, WildlifeSpecies.Hedgehog,
                         WildlifeSpecies.Squirrel, WildlifeSpecies.Pigeon
                     })
            {
                var index = (int)species;
                var canArrive = eligible != null && eligible.TryGetValue(species, out var safe) && safe &&
                                living != null && living.TryGetValue(species, out var count) &&
                                availableSlots != null && availableSlots.TryGetValue(species, out var slots) &&
                                slots > 0 && count >= 0;
                recoveryDays[index] = canArrive
                    ? Math.Min(RecoveryGoodDays, recoveryDays[index] + 1)
                    : 0;
                if (selected == null && recoveryDays[index] >= RecoveryGoodDays)
                    selected = species;
            }
            if (selected.HasValue) recoveryDays[(int)selected.Value] = 0;
            return selected;
        }

        public int RecoveryProgress(WildlifeSpecies species) => recoveryDays[(int)species];

        public EndlessBalanceSaveData Export() => new()
        {
            lastSettledDay = LastSettledDay,
            community = Community,
            lastCommunityChange = LastCommunityChange,
            criticalWildlifeDays = CriticalWildlifeDays,
            lastWildlifeCount = LastWildlifeCount,
            pigeonRecoveryDays = recoveryDays[(int)WildlifeSpecies.Pigeon],
            squirrelRecoveryDays = recoveryDays[(int)WildlifeSpecies.Squirrel],
            hedgehogRecoveryDays = recoveryDays[(int)WildlifeSpecies.Hedgehog],
            foxRecoveryDays = recoveryDays[(int)WildlifeSpecies.Fox]
        };

        public void Restore(EndlessBalanceSaveData saved)
        {
            Reset();
            if (saved == null) return;
            LastSettledDay = Math.Max(0, saved.lastSettledDay);
            Community = Math.Clamp(saved.community, 0, MaximumCommunity);
            LastCommunityChange = Math.Clamp(saved.lastCommunityChange, -8, 4);
            CriticalWildlifeDays = Math.Max(0, saved.criticalWildlifeDays);
            LastWildlifeCount = Math.Max(0, saved.lastWildlifeCount);
            recoveryDays[(int)WildlifeSpecies.Pigeon] = ClampRecovery(saved.pigeonRecoveryDays);
            recoveryDays[(int)WildlifeSpecies.Squirrel] = ClampRecovery(saved.squirrelRecoveryDays);
            recoveryDays[(int)WildlifeSpecies.Hedgehog] = ClampRecovery(saved.hedgehogRecoveryDays);
            recoveryDays[(int)WildlifeSpecies.Fox] = ClampRecovery(saved.foxRecoveryDays);
        }

        public void Reset()
        {
            LastSettledDay = 0;
            Community = rules.StartingCommunity;
            LastCommunityChange = 0;
            CriticalWildlifeDays = 0;
            LastWildlifeCount = AnimalPopulationDefaults.Total;
            Array.Clear(recoveryDays, 0, recoveryDays.Length);
        }

        private static int ClampRecovery(int days) => Math.Clamp(days, 0, RecoveryGoodDays);
    }
}
