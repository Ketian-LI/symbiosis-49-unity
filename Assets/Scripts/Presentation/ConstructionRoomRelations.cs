using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class ConstructionRoomRelations
    {
        public ConstructionTileData Selected { get; private set; }
        public HashSet<(int column, int row)> Coverage { get; } = new();
        public Dictionary<string, int> Penalties { get; } = new();
        public Dictionary<string, int> Benefits { get; } = new();
        public HashSet<string> SettledRooms { get; } = new();
        public List<ScoreTraceActor> SettledActors { get; } = new();
        public int SettledDay { get; private set; }

        public static ConstructionRoomRelations For(ConstructionRunModel run, string id)
        {
            var result = new ConstructionRoomRelations();
            if (run == null || string.IsNullOrEmpty(id)) return result;
            var room = run.BuiltTiles.FirstOrDefault(tile => tile.id == id);
            if (room == null) return result;
            result.Selected = room;
            result.Coverage.UnionWith(ConstructionWasteModel.CoveragePlots(room, run.BuiltTiles)
                .Where(plot => run.Contains(plot.column, plot.row)));
            if (run.UsesPlacementScore)
                foreach (var neighbour in run.BuiltTiles.Where(tile => tile.id != id &&
                    (run.PlacementRulesVersion >= ConstructionPlacementScore.BalancedRulesVersion
                        ? ConstructionBoardModel.AreCardinalNeighbours(room.column, room.row, tile.column, tile.row)
                        : Math.Abs(tile.column-room.column)<=1 && Math.Abs(tile.row-room.row)<=1)))
                {
                    var points = ConstructionPlacementScore.PairPoints(room.category, neighbour.category,
                        run.PlacementRulesVersion);
                    if (points < 0) result.Penalties[neighbour.id] = points;
                    if (points > 0) result.Benefits[neighbour.id] = points;
                }
            // Shared is an aggregate of all contributors. Following it from a
            // single room would incorrectly highlight the entire settled town.
            var entries = ConstructionScoreTrace.For(run).Where(entry =>
                (entry.Kind is ScoreTraceKind.Animal or ScoreTraceKind.Resident) && entry.Tiles.Contains(id)).ToArray();
            foreach (var entry in entries)
            {
                result.SettledRooms.UnionWith(entry.Tiles);
                result.SettledActors.AddRange(entry.Actors);
            }
            result.SettledDay = run.LastScoreDay?.day ?? 0;
            return result;
        }
    }
}
