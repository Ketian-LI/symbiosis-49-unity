using System;
using System.Collections.Generic;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum AnimalDeathCause
    {
        Other,
        Starvation,
        Traffic,
        Predation,
        Unrecorded
    }

    [Serializable]
    public sealed class AnimalMortalityModel
    {
        public const int DefaultDeathLimit = 3;

        private readonly Dictionary<WildlifeSpecies, AnimalDeathBreakdownData> breakdown = new();

        public int PigeonDeaths { get; private set; }
        public int SquirrelDeaths { get; private set; }
        public int HedgehogDeaths { get; private set; }
        public int FoxDeaths { get; private set; }
        public int StarvationDeaths { get; private set; }
        public int TrafficDeaths { get; private set; }
        public int PredationDeaths
        {
            get
            {
                var total = 0;
                foreach (var row in breakdown.Values)
                    total += Math.Max(0, row.predation);
                return total;
            }
        }
        public int TotalDeaths => PigeonDeaths + SquirrelDeaths + HedgehogDeaths + FoxDeaths;
        public int DeathLimit { get; private set; } = DefaultDeathLimit;
        public bool LimitReached => TotalDeaths >= DeathLimit;

        public bool RecordDeath(WildlifeSpecies species, AnimalDeathCause cause)
        {
            switch (species)
            {
                case WildlifeSpecies.Pigeon:
                    PigeonDeaths++;
                    break;
                case WildlifeSpecies.Squirrel:
                    SquirrelDeaths++;
                    break;
                case WildlifeSpecies.Hedgehog:
                    HedgehogDeaths++;
                    break;
                case WildlifeSpecies.Fox:
                    FoxDeaths++;
                    break;
                default:
                    return false;
            }

            if (cause == AnimalDeathCause.Starvation)
            {
                StarvationDeaths++;
            }
            else if (cause == AnimalDeathCause.Traffic)
            {
                TrafficDeaths++;
            }
            var row = GetOrCreateBreakdown(species);
            switch (cause)
            {
                case AnimalDeathCause.Starvation: row.starvation++; break;
                case AnimalDeathCause.Traffic: row.traffic++; break;
                case AnimalDeathCause.Predation: row.predation++; break;
                case AnimalDeathCause.Unrecorded: row.unrecorded++; break;
                default: row.other++; break;
            }
            return LimitReached;
        }

        public AnimalDeathBreakdownData BreakdownOf(WildlifeSpecies species)
        {
            return GetOrCreateBreakdown(species).Clone();
        }

        public List<AnimalDeathBreakdownData> ExportBreakdown()
        {
            return new List<AnimalDeathBreakdownData>
            {
                BreakdownOf(WildlifeSpecies.Pigeon),
                BreakdownOf(WildlifeSpecies.Squirrel),
                BreakdownOf(WildlifeSpecies.Hedgehog),
                BreakdownOf(WildlifeSpecies.Fox)
            };
        }

        public void SetDeathLimit(int deathLimit)
        {
            DeathLimit = Math.Max(1, deathLimit);
        }

        public void Restore(
            int pigeonDeaths,
            int squirrelDeaths,
            int hedgehogDeaths,
            int foxDeaths,
            int starvationDeaths,
            int trafficDeaths,
            int deathLimit = DefaultDeathLimit,
            IEnumerable<AnimalDeathBreakdownData> savedBreakdown = null)
        {
            PigeonDeaths = Math.Max(0, pigeonDeaths);
            SquirrelDeaths = Math.Max(0, squirrelDeaths);
            HedgehogDeaths = Math.Max(0, hedgehogDeaths);
            FoxDeaths = Math.Max(0, foxDeaths);
            StarvationDeaths = Math.Max(0, starvationDeaths);
            TrafficDeaths = Math.Max(0, trafficDeaths);
            DeathLimit = Math.Max(1, deathLimit);
            breakdown.Clear();
            if (savedBreakdown != null)
            {
                foreach (var saved in savedBreakdown)
                {
                    if (saved == null || !IsKnownSpecies(saved.species) || breakdown.ContainsKey(saved.species))
                    {
                        continue;
                    }
                    var row = saved.Clone();
                    var speciesTotal = DeathsOf(saved.species);
                    if (row.Total <= speciesTotal)
                    {
                        row.unrecorded += speciesTotal - row.Total;
                        breakdown[saved.species] = row;
                    }
                }
            }
            foreach (var species in new[]
                     {
                         WildlifeSpecies.Pigeon, WildlifeSpecies.Squirrel,
                         WildlifeSpecies.Hedgehog, WildlifeSpecies.Fox
                     })
            {
                if (!breakdown.ContainsKey(species))
                {
                    breakdown[species] = new AnimalDeathBreakdownData
                    {
                        species = species,
                        unrecorded = DeathsOf(species)
                    };
                }
            }
        }

        public void Reset()
        {
            PigeonDeaths = 0;
            SquirrelDeaths = 0;
            HedgehogDeaths = 0;
            FoxDeaths = 0;
            StarvationDeaths = 0;
            TrafficDeaths = 0;
            DeathLimit = DefaultDeathLimit;
            breakdown.Clear();
        }

        private int DeathsOf(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => PigeonDeaths,
                WildlifeSpecies.Squirrel => SquirrelDeaths,
                WildlifeSpecies.Hedgehog => HedgehogDeaths,
                WildlifeSpecies.Fox => FoxDeaths,
                _ => 0
            };
        }

        private AnimalDeathBreakdownData GetOrCreateBreakdown(WildlifeSpecies species)
        {
            if (!breakdown.TryGetValue(species, out var row))
            {
                row = new AnimalDeathBreakdownData { species = species };
                breakdown[species] = row;
            }
            return row;
        }

        private static bool IsKnownSpecies(WildlifeSpecies species)
        {
            return species == WildlifeSpecies.Pigeon || species == WildlifeSpecies.Squirrel ||
                   species == WildlifeSpecies.Hedgehog || species == WildlifeSpecies.Fox;
        }
    }
}
