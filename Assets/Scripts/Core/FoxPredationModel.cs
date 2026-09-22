using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public readonly struct FoxPreyCandidate
    {
        public FoxPreyCandidate(string id, WildlifeSpecies species, float distance, bool alive, bool grounded)
        {
            Id = id;
            Species = species;
            Distance = distance;
            Alive = alive;
            Grounded = grounded;
        }

        public string Id { get; }
        public WildlifeSpecies Species { get; }
        public float Distance { get; }
        public bool Alive { get; }
        public bool Grounded { get; }
    }

    public static class FoxPredationModel
    {
        public static bool CanStartHunt(bool foxAlive, bool foxActive, int hungerDays)
        {
            return foxAlive && foxActive && hungerDays >= 1;
        }

        public static string SelectPrey(IEnumerable<FoxPreyCandidate> candidates)
        {
            return (candidates ?? Array.Empty<FoxPreyCandidate>())
                .Where(item => item.Alive && item.Grounded && item.Species != WildlifeSpecies.Fox)
                .OrderBy(item => Priority(item.Species))
                .ThenBy(item => item.Distance)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .Select(item => item.Id)
                .FirstOrDefault();
        }

        private static int Priority(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => 0,
                WildlifeSpecies.Squirrel => 1,
                WildlifeSpecies.Hedgehog => 2,
                _ => int.MaxValue
            };
        }
    }
}
