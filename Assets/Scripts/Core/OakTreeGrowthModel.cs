using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum OakTreeStage
    {
        Felled,
        Sapling,
        Young,
        Mature
    }

    [Serializable]
    public sealed class OakTreeState
    {
        public string roomId;
        public OakTreeStage stage;
        public int completeDaysSincePlanting;
    }

    public sealed class OakTreeGrowthModel
    {
        private readonly Dictionary<string, bool> startsRecovering;
        private readonly Dictionary<string, OakTreeState> trees = new();

        public OakTreeGrowthModel(IEnumerable<RoomSpec> roomSpecs)
        {
            startsRecovering = roomSpecs
                .Where(room => room.Type == RoomType.OakHabitat)
                .ToDictionary(room => room.Id, room => room.StartsRecovering);
            Reset();
        }

        public IReadOnlyDictionary<string, OakTreeState> Trees => trees;
        public int TreesFelled { get; private set; }
        public int TreesPlanted { get; private set; }
        public int TreesMatured { get; private set; }

        public OakTreeStage StageOf(string roomId)
        {
            return trees.TryGetValue(roomId, out var tree)
                ? tree.stage
                : OakTreeStage.Felled;
        }

        public bool Fell(string roomId)
        {
            if (!trees.TryGetValue(roomId, out var tree) || tree.stage == OakTreeStage.Felled)
            {
                return false;
            }

            tree.stage = OakTreeStage.Felled;
            tree.completeDaysSincePlanting = 0;
            TreesFelled++;
            return true;
        }

        public bool Plant(string roomId)
        {
            if (!trees.TryGetValue(roomId, out var tree) || tree.stage != OakTreeStage.Felled)
            {
                return false;
            }

            tree.stage = OakTreeStage.Sapling;
            tree.completeDaysSincePlanting = 0;
            TreesPlanted++;
            return true;
        }

        public IReadOnlyList<string> AdvanceOneCompleteDay()
        {
            var matured = new List<string>();
            foreach (var tree in trees.Values)
            {
                if (tree.stage != OakTreeStage.Sapling && tree.stage != OakTreeStage.Young)
                {
                    continue;
                }

                tree.completeDaysSincePlanting++;
                if (tree.completeDaysSincePlanting >= 2)
                {
                    tree.stage = OakTreeStage.Mature;
                    TreesMatured++;
                    matured.Add(tree.roomId);
                }
                else
                {
                    tree.stage = OakTreeStage.Young;
                }
            }

            return matured;
        }

        public List<OakTreeSaveData> ExportTrees()
        {
            return trees.Values
                .OrderBy(tree => tree.roomId, StringComparer.Ordinal)
                .Select(tree => new OakTreeSaveData
                {
                    roomId = tree.roomId,
                    stage = tree.stage.ToString(),
                    completeDaysSincePlanting = tree.completeDaysSincePlanting
                })
                .ToList();
        }

        public void Restore(
            IEnumerable<OakTreeSaveData> savedTrees,
            int felled,
            int planted,
            int matured)
        {
            var saved = (savedTrees ?? Array.Empty<OakTreeSaveData>())
                .Where(item => item != null && trees.ContainsKey(item.roomId))
                .ToDictionary(item => item.roomId);
            if (saved.Count == 0)
            {
                Reset();
                return;
            }

            foreach (var pair in trees)
            {
                if (!saved.TryGetValue(pair.Key, out var source) ||
                    !Enum.TryParse<OakTreeStage>(source.stage, out var stage))
                {
                    continue;
                }

                pair.Value.stage = stage;
                pair.Value.completeDaysSincePlanting = Math.Max(0, source.completeDaysSincePlanting);
            }
            TreesFelled = Math.Max(0, felled);
            TreesPlanted = Math.Max(0, planted);
            TreesMatured = Math.Max(0, matured);
        }

        public void Reset()
        {
            trees.Clear();
            foreach (var pair in startsRecovering)
            {
                trees[pair.Key] = new OakTreeState
                {
                    roomId = pair.Key,
                    stage = pair.Value ? OakTreeStage.Young : OakTreeStage.Mature,
                    completeDaysSincePlanting = pair.Value ? 1 : 2
                };
            }
            TreesFelled = 0;
            TreesPlanted = 0;
            TreesMatured = 0;
        }
    }
}
