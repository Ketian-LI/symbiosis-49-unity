using System;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public enum AnimalDeathCause
    {
        Other,
        Starvation,
        Traffic
    }

    [Serializable]
    public sealed class AnimalMortalityModel
    {
        public const int DefaultDeathLimit = 5;

        public int PigeonDeaths { get; private set; }
        public int SquirrelDeaths { get; private set; }
        public int HedgehogDeaths { get; private set; }
        public int FoxDeaths { get; private set; }
        public int StarvationDeaths { get; private set; }
        public int TrafficDeaths { get; private set; }
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
            return LimitReached;
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
            int deathLimit = DefaultDeathLimit)
        {
            PigeonDeaths = Math.Max(0, pigeonDeaths);
            SquirrelDeaths = Math.Max(0, squirrelDeaths);
            HedgehogDeaths = Math.Max(0, hedgehogDeaths);
            FoxDeaths = Math.Max(0, foxDeaths);
            StarvationDeaths = Math.Max(0, starvationDeaths);
            TrafficDeaths = Math.Max(0, trafficDeaths);
            DeathLimit = Math.Max(1, deathLimit);
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
        }
    }
}
