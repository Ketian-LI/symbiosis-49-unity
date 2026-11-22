using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class ConstructionGreenPatch
    {
        public string Id { get; }
        public IReadOnlyList<ConstructionTileData> Tiles { get; }
        public IReadOnlyList<ConstructionTileData> Oaks { get; }
        public IReadOnlyList<ConstructionTileData> Meadows { get; }
        public IReadOnlyList<ConstructionTileData> Shrubs { get; }
        public int SquirrelCapacity => Oaks.Count == 0 ? 0 :
            Math.Min(3, 1 + (Oaks.Count - 1) / 2);
        public int PigeonCapacity => Meadows.Count < 2 ? 0 :
            Math.Min(5, 1 + (Meadows.Count + 1) / 2);
        public int HedgehogCapacity => Shrubs.Count > 0 ? 1 : 0;
        public int FoxCapacity => Shrubs.Count >= 2 ? 1 : 0;

        internal ConstructionGreenPatch(IReadOnlyList<ConstructionTileData> tiles,
            int day)
        {
            Tiles = tiles.OrderBy(tile => tile.buildIndex).ToArray();
            Id = Tiles[0].id;
            Oaks = Mature(GreenPlanting.Oak);
            Meadows = Mature(GreenPlanting.Meadow);
            Shrubs = Mature(GreenPlanting.Shrub);

            IReadOnlyList<ConstructionTileData> Mature(GreenPlanting planting) =>
                Tiles.Where(tile => tile.planting == planting &&
                    tile.builtDay <= day &&
                    ConstructionFoodModel.MaturityStage(tile, day) == 2)
                    .ToArray();
        }
    }

    public static class ConstructionGreenPatchModel
    {
        public static int SquirrelCapacity(ConstructionBoardModel board, int day) =>
            Find(board, day).Sum(patch => patch.SquirrelCapacity);

        public static int PigeonCapacity(ConstructionBoardModel board, int day) =>
            Find(board, day).Sum(patch => patch.PigeonCapacity);

        public static IReadOnlyList<ConstructionTileData> SquirrelHomes(
            ConstructionBoardModel board, int day, int population) =>
            Find(board, day).SelectMany(patch =>
                    patch.Oaks.Take(patch.SquirrelCapacity))
                .Take(Math.Max(0, population)).ToArray();

        public static IReadOnlyList<ConstructionPigeonGroup> PigeonGroups(
            ConstructionBoardModel board, int day, int population)
        {
            var remaining = Math.Max(0, population);
            var groups = new List<ConstructionPigeonGroup>();
            foreach (var patch in Find(board, day))
            {
                var count = Math.Min(remaining, patch.PigeonCapacity);
                if (count == 0) continue;
                groups.Add(new ConstructionPigeonGroup(patch, count));
                remaining -= count;
                if (remaining == 0) break;
            }
            return groups;
        }

        public static IReadOnlyList<ConstructionTileData> PigeonHomes(
            ConstructionBoardModel board, int day, int population) =>
            PigeonGroups(board, day, population).SelectMany(group =>
                Enumerable.Range(0, group.Count).Select(index =>
                    group.Patch.Meadows[index % group.Patch.Meadows.Count]))
                .ToArray();

        // Habitat connectivity is shared-edge only. A road or square can be
        // traversed while foraging, but it never joins two habitat patches.
        public static IReadOnlyList<ConstructionGreenPatch> Find(
            ConstructionBoardModel board, int day)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var greens = board.BuiltTiles.Where(tile =>
                    tile.category == ConstructionCategory.Green &&
                    tile.builtDay <= day)
                .OrderBy(tile => tile.buildIndex).ToArray();
            var seen = new HashSet<string>();
            var patches = new List<ConstructionGreenPatch>();
            foreach (var green in greens)
            {
                if (!seen.Add(green.id)) continue;
                var members = new List<ConstructionTileData>();
                var pending = new Queue<ConstructionTileData>();
                pending.Enqueue(green);
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    members.Add(current);
                    foreach (var neighbour in greens.Where(tile =>
                                 !seen.Contains(tile.id) &&
                                 ConstructionBoardModel.AreCardinalNeighbours(
                                     current.column, current.row,
                                     tile.column, tile.row)))
                    {
                        seen.Add(neighbour.id);
                        pending.Enqueue(neighbour);
                    }
                }
                patches.Add(new ConstructionGreenPatch(members, day));
            }
            return patches;
        }
    }

    public sealed class ConstructionPigeonGroup
    {
        public ConstructionGreenPatch Patch { get; }
        public int Count { get; }
        public ConstructionTileData Origin => Patch.Meadows[0];

        internal ConstructionPigeonGroup(ConstructionGreenPatch patch,
            int count)
        {
            Patch = patch;
            Count = count;
        }
    }
}
