using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class ConstructionDayResult
    {
        public int day;
        public int animalMeals;
        public int completedWorkCycles;
    }

    [Serializable]
    public sealed class ConstructionScoreSaveData
    {
        public List<ConstructionDayResult> days = new();
    }

    // Counts observed outcomes, never expected route availability. This keeps
    // the new end score separate from a preview or a green-adjacency bonus.
    public sealed class ConstructionScoreLedger
    {
        private readonly List<ConstructionDayResult> days = new();

        public int AnimalScore => days.Sum(result => result.animalMeals);
        public int HumanScore => days.Sum(result => result.completedWorkCycles);
        public IReadOnlyList<ConstructionDayResult> Days => days.Select(Clone).ToArray();

        public bool TryRecordDay(int day, int animalMeals, int completedWorkCycles)
        {
            if (day < 1 || animalMeals < 0 || completedWorkCycles < 0 ||
                days.Any(result => result.day == day) ||
                (days.Count > 0 && day != days[days.Count - 1].day + 1) ||
                (days.Count == 0 && day != 1)) return false;

            days.Add(new ConstructionDayResult
            {
                day = day,
                animalMeals = animalMeals,
                completedWorkCycles = completedWorkCycles
            });
            return true;
        }

        public ConstructionScoreSaveData Export() => new()
        {
            days = days.Select(Clone).ToList()
        };

        public static bool TryRestore(ConstructionScoreSaveData saved,
            out ConstructionScoreLedger ledger)
        {
            ledger = null;
            if (saved?.days == null) return false;
            var candidate = new ConstructionScoreLedger();
            foreach (var result in saved.days)
            {
                if (result == null || !candidate.TryRecordDay(result.day,
                        result.animalMeals, result.completedWorkCycles)) return false;
            }
            ledger = candidate;
            return true;
        }

        private static ConstructionDayResult Clone(ConstructionDayResult source) => new()
        {
            day = source.day,
            animalMeals = source.animalMeals,
            completedWorkCycles = source.completedWorkCycles
        };
    }
}
