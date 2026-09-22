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
