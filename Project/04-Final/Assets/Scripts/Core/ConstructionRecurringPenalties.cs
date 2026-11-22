using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;
namespace UrbanWildlifeRooms.Core
{
    [Serializable] public sealed class ConstructionRecurringPenalty
    {
        public int day, points;
        public string firstTileId, secondTileId;
    }
    public sealed partial class ConstructionRunModel
    {
        private readonly List<ConstructionRecurringPenalty> recurringPenalties=new();
        public bool UsesRecurringPenalties=>UsesFillHand && UsesPlacementScore;
        public IReadOnlyList<ConstructionRecurringPenalty> RecurringPenalties=>recurringPenalties.Select(CopyPenalty).ToArray();
        public int RecurringPenaltyTotal=>recurringPenalties.Sum(p=>p.points);
        public int LastRecurringPenaltyPoints=>recurringPenalties.Where(p=>p.day==LastScoreDay?.day).Sum(p=>p.points);
        public int NextRecurringPenaltyPoints=>ActiveRecurringPenalties(CurrentDay).Sum(p=>p.points);
        private static ConstructionRecurringPenalty CopyPenalty(ConstructionRecurringPenalty p)=>new(){day=p.day,points=p.points,firstTileId=p.firstTileId,secondTileId=p.secondTileId};
        private IEnumerable<ConstructionRecurringPenalty> ActiveRecurringPenalties(int day)
        {
            if(!UsesRecurringPenalties)yield break;
            // Introduction-day pairings already paid once in placement score.
            // Charge them again only on subsequent confirmed settlements.
            var tiles=BuiltTiles.Where(t=>Math.Max(1,t.builtDay)<day).OrderBy(t=>t.buildIndex).ToArray();
            for(var i=0;i<tiles.Length;i++)for(var j=i+1;j<tiles.Length;j++)
            {
                var a=tiles[i];var b=tiles[j];
                if(!ConstructionBoardModel.AreCardinalNeighbours(a.column,a.row,b.column,b.row))continue;
                var points=ConstructionPlacementScore.PairPoints(a.category,b.category,PlacementRulesVersion);
                if(points<0)yield return new(){day=day,points=points,firstTileId=a.id,secondTileId=b.id};
            }
        }
        private void RecordRecurringPenalties()
        {
            if(recurringPenalties.Any(p=>p.day==CurrentDay))return;
            recurringPenalties.AddRange(ActiveRecurringPenalties(CurrentDay));
        }
        private bool RestoreRecurringPenalties(List<ConstructionRecurringPenalty> saved)
        {
            recurringPenalties.Clear();if(saved==null)return true;var keys=new HashSet<string>();
            foreach(var p in saved)
            {
                if(p==null || p.day<1 || p.day>(LastScoreDay?.day??0) ||
                    !keys.Add(p.day+":"+p.firstTileId+":"+p.secondTileId))return false;
                if(!ActiveRecurringPenalties(p.day).Any(x=>x.firstTileId==p.firstTileId && x.secondTileId==p.secondTileId && x.points==p.points))return false;
                recurringPenalties.Add(CopyPenalty(p));
            }
            foreach(var group in saved.GroupBy(p=>p.day))
                if(group.Count()!=ActiveRecurringPenalties(group.Key).Count())return false;
            return true;
        }
    }
}
