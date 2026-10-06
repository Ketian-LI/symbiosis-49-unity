using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class AnimalPopulationController : MonoBehaviour
    {
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly List<FoxDemoAgent> foxes = new();

        public event Action StateChanged;

        public void Initialize(
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            IEnumerable<FoxDemoAgent> foxAgents)
        {
            pigeons.AddRange((pigeonAgents ?? Array.Empty<PigeonDemoAgent>()).Where(item => item != null));
            squirrels.AddRange((squirrelAgents ?? Array.Empty<SquirrelDemoAgent>()).Where(item => item != null));
            hedgehogs.AddRange((hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>()).Where(item => item != null));
            foxes.AddRange((foxAgents ?? Array.Empty<FoxDemoAgent>()).Where(item => item != null));
            foreach (var pigeon in pigeons)
            {
                pigeon.StateChanged += HandlePigeonStateChanged;
            }
            foreach (var vitality in squirrels.Select(item => item.Vitality)
                         .Concat(hedgehogs.Select(item => item.Vitality))
                         .Concat(foxes.Select(item => item.Vitality))
                         .Where(item => item != null))
            {
                vitality.StateChanged += HandleVitalityStateChanged;
            }
        }

        private void OnDestroy()
        {
            foreach (var pigeon in pigeons)
            {
                if (pigeon != null)
                {
                    pigeon.StateChanged -= HandlePigeonStateChanged;
                }
            }
            foreach (var vitality in squirrels.Select(item => item.Vitality)
                         .Concat(hedgehogs.Select(item => item.Vitality))
                         .Concat(foxes.Select(item => item.Vitality))
                         .Where(item => item != null))
            {
                vitality.StateChanged -= HandleVitalityStateChanged;
            }
        }

        public int LivingCount(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => pigeons.Count(item => item.IsAlive),
                WildlifeSpecies.Squirrel => squirrels.Count(item => item.IsAlive),
                WildlifeSpecies.Hedgehog => hedgehogs.Count(item => item.IsAlive),
                WildlifeSpecies.Fox => foxes.Count(item => item.IsAlive),
                _ => 0
            };
        }

        public int TotalCount(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => pigeons.Count(item => item != null),
                WildlifeSpecies.Squirrel => squirrels.Count(item => item != null),
                WildlifeSpecies.Hedgehog => hedgehogs.Count(item => item != null),
                WildlifeSpecies.Fox => foxes.Count(item => item != null),
                _ => 0
            };
        }

        public IWildlifeLayoutAgent FirstDormantAgent(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => pigeons.FirstOrDefault(item => item != null && !item.IsAlive),
                WildlifeSpecies.Squirrel => squirrels.FirstOrDefault(item => item != null && !item.IsAlive),
                WildlifeSpecies.Hedgehog => hedgehogs.FirstOrDefault(item => item != null && !item.IsAlive),
                WildlifeSpecies.Fox => foxes.FirstOrDefault(item => item != null && !item.IsAlive),
                _ => null
            };
        }

        public IWildlifeLayoutAgent TryReintroduce(WildlifeSpecies species)
        {
            var dormant = FirstDormantAgent(species);
            switch (dormant)
            {
                case PigeonDemoAgent pigeon:
                    pigeon.ResetForNewRun(pigeon.SpawnPosition);
                    break;
                case SquirrelDemoAgent squirrel:
                    squirrel.ResetForNewRun(squirrel.SpawnPosition, squirrel.CachePosition);
                    break;
                case HedgehogDemoAgent hedgehog:
                    hedgehog.ResetForNewRun(hedgehog.SpawnPosition);
                    break;
                case FoxDemoAgent fox:
                    fox.ResetForNewRun(fox.SpawnPosition);
                    break;
            }
            if (dormant != null) StateChanged?.Invoke();
            return dormant;
        }

        public List<string> ExportDeadIds()
        {
            var result = new List<string>();
            for (var index = 0; index < pigeons.Count; index++)
                if (pigeons[index] != null && !pigeons[index].IsAlive)
                    result.Add($"pigeon-{index + 1:00}");
            for (var index = 0; index < squirrels.Count; index++)
                if (squirrels[index] != null && !squirrels[index].IsAlive)
                    result.Add($"squirrel-{index + 1:00}");
            for (var index = 0; index < hedgehogs.Count; index++)
                if (hedgehogs[index] != null && !hedgehogs[index].IsAlive)
                    result.Add($"hedgehog-{index + 1:00}");
            for (var index = 0; index < foxes.Count; index++)
                if (foxes[index] != null && !foxes[index].IsAlive)
                    result.Add($"fox-{index + 1:00}");
            return result;
        }

        public void RestoreDeadIds(IEnumerable<string> ids)
        {
            var dead = new HashSet<string>(ids ?? Array.Empty<string>(), StringComparer.Ordinal);
            for (var index = 0; index < pigeons.Count; index++)
                if (dead.Contains($"pigeon-{index + 1:00}")) pigeons[index]?.RestoreDeadForSession();
            for (var index = 0; index < squirrels.Count; index++)
                if (dead.Contains($"squirrel-{index + 1:00}")) squirrels[index]?.Vitality?.RestoreDeadForSession();
            for (var index = 0; index < hedgehogs.Count; index++)
                if (dead.Contains($"hedgehog-{index + 1:00}")) hedgehogs[index]?.Vitality?.RestoreDeadForSession();
            for (var index = 0; index < foxes.Count; index++)
                if (dead.Contains($"fox-{index + 1:00}")) foxes[index]?.Vitality?.RestoreDeadForSession();
            StateChanged?.Invoke();
        }

        public float SoonestRespawnRemaining(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => pigeons
                    .Where(item => item != null && !item.IsAlive)
                    .Select(item => item.RespawnRemaining)
                    .DefaultIfEmpty(0f).Min(),
                WildlifeSpecies.Squirrel => squirrels
                    .Where(item => item != null && !item.IsAlive && item.Vitality != null)
                    .Select(item => item.Vitality.RespawnRemainingSeconds)
                    .DefaultIfEmpty(0f).Min(),
                WildlifeSpecies.Hedgehog => hedgehogs
                    .Where(item => item != null && !item.IsAlive && item.Vitality != null)
                    .Select(item => item.Vitality.RespawnRemainingSeconds)
                    .DefaultIfEmpty(0f).Min(),
                WildlifeSpecies.Fox => foxes
                    .Where(item => item != null && !item.IsAlive && item.Vitality != null)
                    .Select(item => item.Vitality.RespawnRemainingSeconds)
                    .DefaultIfEmpty(0f).Min(),
                _ => 0f
            };
        }

        private void HandlePigeonStateChanged(PigeonDemoAgent agent, PigeonDemoState state)
        {
            StateChanged?.Invoke();
        }

        private void HandleVitalityStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
