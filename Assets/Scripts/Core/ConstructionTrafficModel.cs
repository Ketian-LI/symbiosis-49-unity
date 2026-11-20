using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionTrafficSpecies { None, Squirrel, Pigeon, Hedgehog, Fox }

    public sealed class ConstructionTrafficCollision
    {
        public string streetTileId;
        public string wildlifeTileId;
        public ConstructionTrafficSpecies species;
        public bool Happened => species != ConstructionTrafficSpecies.None;
    }

    // A daily, reproducible abstraction of wildlife occasionally crossing a
    // road beside its habitat. The animated car's frame timing is decorative;
    // settlement depends only on the saved board and day. Each exposed animal
    // has one independent 1-in-16 chance per adjacent street, with a maximum
    // of one fatal collision on the whole board in a day. A slow street or a
    // resident-visited plaza nearby halves that risk (1 in 32).
    public static class ConstructionTrafficModel
    {
        public const int ChanceDenominator = 16;
        public const int ProtectedChanceDenominator = 32;

        public static ConstructionTrafficCollision PlanDay(
            ConstructionBoardModel board, int day, int squirrels, int pigeons,
            string pigeonTileId, string hedgehogTileId, string foxTileId,
            ConstructionPreySpecies predatedSpecies,
            IReadOnlyCollection<string> protectedStreetTileIds = null,
            bool usePatchWildlife = false,
            IReadOnlyList<string> patchHedgehogTileIds = null,
            string predatedTileId = null, bool useNeighborUpgrades = false)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var none = new ConstructionTrafficCollision();
            if (day < 1) return none;

            var habitats = new List<(ConstructionTrafficSpecies species,
                string tileId, int count)>();
            var livingSquirrels = Math.Max(0, squirrels -
                (predatedSpecies == ConstructionPreySpecies.Squirrel ? 1 : 0));
            foreach (var home in ConstructionFoodModel.SquirrelHomeTiles(
                         board, day, usePatchWildlife ? squirrels :
                             livingSquirrels, usePatchWildlife))
            {
                if (usePatchWildlife && predatedSpecies ==
                    ConstructionPreySpecies.Squirrel &&
                    home.id == predatedTileId) continue;
                habitats.Add((ConstructionTrafficSpecies.Squirrel,
                    home.id, 1));
            }
            if (usePatchWildlife)
                foreach (var group in ConstructionGreenPatchModel.PigeonGroups(
                             board, day, pigeons))
                    habitats.Add((ConstructionTrafficSpecies.Pigeon,
                        group.Origin.id, group.Count -
                            (predatedSpecies == ConstructionPreySpecies.Pigeon &&
                             group.Origin.id == predatedTileId ? 1 : 0)));
            if (usePatchWildlife && patchHedgehogTileIds != null)
                foreach (var homeId in patchHedgehogTileIds)
                    habitats.Add((ConstructionTrafficSpecies.Hedgehog,
                        homeId, predatedSpecies ==
                            ConstructionPreySpecies.Hedgehog &&
                            homeId == predatedTileId ? 0 : 1));
            habitats.AddRange(new[]
            {
                (species: ConstructionTrafficSpecies.Pigeon,
                    tileId: pigeonTileId,
                    count: usePatchWildlife ? 0 : Math.Max(0, pigeons -
                        (predatedSpecies == ConstructionPreySpecies.Pigeon ? 1 : 0))),
                (species: ConstructionTrafficSpecies.Hedgehog,
                    tileId: hedgehogTileId,
                    count: usePatchWildlife || predatedSpecies ==
                        ConstructionPreySpecies.Hedgehog ? 0 : 1),
                (species: ConstructionTrafficSpecies.Fox,
                    tileId: foxTileId, count: 1)
            });
            var built = board.BuiltTiles.Where(tile => tile.builtDay <= day).ToArray();
            var streets = built.Where(tile => tile.category == ConstructionCategory.Street)
                .OrderBy(tile => tile.buildIndex);
            uint bestRoll = uint.MaxValue;
            foreach (var street in streets)
            {
                var protectedStreet = protectedStreetTileIds?.Contains(street.id) == true ||
                    useNeighborUpgrades && ConstructionPlacementScore.IsSlowStreet(street, built);
                var denominator = protectedStreet ? ProtectedChanceDenominator : ChanceDenominator;
                foreach (var habitat in habitats)
                {
                    if (habitat.count == 0 || string.IsNullOrEmpty(habitat.tileId))
                        continue;
                    var wildlife = built.FirstOrDefault(tile => tile.id == habitat.tileId &&
                        tile.category == ConstructionCategory.Green);
                    if (wildlife == null ||
                        !ConstructionBoardModel.AreNeighbours(street.column, street.row,
                            wildlife.column, wildlife.row))
                        continue;
                    for (var animal = 0; animal < habitat.count; animal++)
                    {
                        var roll = StableRoll(day, street.id, habitat.tileId,
                            habitat.species, animal);
                        if (roll % denominator != 0 || roll >= bestRoll) continue;
                        bestRoll = roll;
                        none.streetTileId = street.id;
                        none.wildlifeTileId = habitat.tileId;
                        none.species = habitat.species;
                    }
                }
            }
            return none;
        }

        // FNV-1a over explicit UTF-16 code units avoids process-dependent
        // string.GetHashCode and keeps save/reload forecasts identical.
        private static uint StableRoll(int day, string streetId,
            string wildlifeId, ConstructionTrafficSpecies species, int animal)
        {
            unchecked
            {
                uint hash = 2166136261;
                void Mix(uint value) { hash = (hash ^ value) * 16777619; }
                Mix((uint)day);
                foreach (var letter in streetId) Mix(letter);
                Mix(0xff);
                foreach (var letter in wildlifeId) Mix(letter);
                Mix((uint)species);
                Mix((uint)animal);
                // FNV's low bits have short cycles for sequential day/animal
                // values. Avalanche before sampling to avoid guaranteed
                // periodic deaths near a fixed road.
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return hash;
            }
        }
    }
}
