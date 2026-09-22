using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class AnimalNeedState
    {
        public string id;
        public WildlifeSpecies species;
        public int hungerDays;
        public bool ateToday;
    }

    public sealed class AnimalNeedsModel
    {
        public const int StarvationDays = 3;
        private readonly Dictionary<string, AnimalNeedState> animals = new();

        public IReadOnlyDictionary<string, AnimalNeedState> Animals => animals;

        public void Register(string id, WildlifeSpecies species)
        {
            if (string.IsNullOrWhiteSpace(id) || animals.ContainsKey(id))
            {
                return;
            }
            animals[id] = new AnimalNeedState { id = id, species = species };
        }

        public bool MarkMeal(string id)
        {
            if (!animals.TryGetValue(id, out var animal))
            {
                return false;
            }
            animal.ateToday = true;
            animal.hungerDays = 0;
            return true;
        }

        public IReadOnlyList<string> CompleteDay(IEnumerable<string> livingAnimalIds)
        {
            var living = new HashSet<string>(livingAnimalIds ?? Array.Empty<string>());
            var starved = new List<string>();
            foreach (var animal in animals.Values)
            {
                if (!living.Contains(animal.id))
                {
                    animal.ateToday = false;
                    continue;
                }
                if (animal.ateToday)
                {
                    animal.hungerDays = 0;
                }
                else
                {
                    animal.hungerDays++;
                    if (animal.hungerDays >= StarvationDays)
                    {
                        starved.Add(animal.id);
                    }
                }
                animal.ateToday = false;
            }
            return starved;
        }

        public void ResetIndividual(string id)
        {
            if (animals.TryGetValue(id, out var animal))
            {
                animal.hungerDays = 0;
                animal.ateToday = false;
            }
        }

        public List<AnimalNeedSaveData> Export()
        {
            return animals.Values
                .OrderBy(item => item.id, StringComparer.Ordinal)
                .Select(item => new AnimalNeedSaveData
                {
                    id = item.id,
                    species = item.species.ToString(),
                    hungerDays = item.hungerDays,
                    ateToday = item.ateToday
                })
                .ToList();
        }

        public void Restore(IEnumerable<AnimalNeedSaveData> savedStates)
        {
            foreach (var item in savedStates ?? Array.Empty<AnimalNeedSaveData>())
            {
                if (item == null || !animals.TryGetValue(item.id, out var state))
                {
                    continue;
                }
                state.hungerDays = Math.Max(0, item.hungerDays);
                state.ateToday = item.ateToday;
            }
        }

        public void Reset()
        {
            foreach (var animal in animals.Values)
            {
                animal.hungerDays = 0;
                animal.ateToday = false;
            }
        }
    }
}
