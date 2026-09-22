using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class AnimalActivityController : MonoBehaviour
    {
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly List<FoxDemoAgent> foxes = new();
        private GameRuntimeController runtime;
        private DayPhase? appliedPhase;

        public void Initialize(
            GameRuntimeController runtimeController,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            IEnumerable<FoxDemoAgent> foxAgents)
        {
            runtime = runtimeController;
            pigeons.AddRange(pigeonAgents ?? Array.Empty<PigeonDemoAgent>());
            squirrels.AddRange(squirrelAgents ?? Array.Empty<SquirrelDemoAgent>());
            hedgehogs.AddRange(hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>());
            foxes.AddRange(foxAgents ?? Array.Empty<FoxDemoAgent>());
            Apply(runtime.Clock.Phase);
        }

        private void Update()
        {
            if (runtime == null || appliedPhase == runtime.Clock.Phase)
            {
                return;
            }
            Apply(runtime.Clock.Phase);
        }

        private void Apply(DayPhase phase)
        {
            appliedPhase = phase;
            foreach (var pigeon in pigeons)
            {
                pigeon.SetActivityEnabled(AnimalActivitySchedule.IsNormallyActive(WildlifeSpecies.Pigeon, phase));
            }
            foreach (var squirrel in squirrels)
            {
                squirrel.SetActivityEnabled(AnimalActivitySchedule.IsNormallyActive(WildlifeSpecies.Squirrel, phase));
            }
            foreach (var hedgehog in hedgehogs)
            {
                hedgehog.SetActivityEnabled(AnimalActivitySchedule.IsNormallyActive(WildlifeSpecies.Hedgehog, phase));
            }
            foreach (var fox in foxes)
            {
                fox.SetActivityEnabled(AnimalActivitySchedule.IsNormallyActive(WildlifeSpecies.Fox, phase));
            }
        }
    }
}
