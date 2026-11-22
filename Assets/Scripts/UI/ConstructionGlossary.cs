using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UrbanWildlifeRooms.UI
{
    public static class ConstructionGlossary
    {
        public sealed class Term
        {
            public readonly string Id, English, Chinese, EnglishMeaning, ChineseMeaning;
            public readonly string[] Aliases;
            public Term(string id,string en,string zh,string enMeaning,string zhMeaning,params string[] aliases)
            {Id=id;English=en;Chinese=zh;EnglishMeaning=enMeaning;ChineseMeaning=zhMeaning;Aliases=aliases.Concat(new[]{en,zh}).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
        }
        public static readonly Term[] Terms={
            new("oak","Central oak","中央橡树","The opening habitat for squirrels. Place the first oak in the centre.","松鼠最初的栖息地。第一棵橡树放在棋盘正中。","橡树"),
            new("home","Home","住宅","Houses one resident. Work and meal destinations are planned automatically through connected routes.","容纳 1 名居民。系统沿连通路线自动分配岗位和餐位。","homes","residence","residences"),
            new("restaurant","Restaurant","餐厅","Provides one job and two meal seats; a terrace upgrade adds a seat.","提供 1 个岗位、2 个餐位；露台升级后增加 1 个餐位。","restaurants"),
            new("workshop","Workshop","工作坊","Provides two jobs. Residents still need a reachable meal service.","提供 2 个岗位。居民还需要能到达的用餐地点。","workshops"),
            new("street","Street","街道","Shared-edge streets extend resident and cleanup routes. Diagonal contact does not connect them.","沿上下左右共边接通居民和清运路线；斜角接触不算连通。","streets","道路"),
            new("waste","Waste room","垃圾间","Collects waste around it and along connected streets. A shared-edge home loses pairing points at each settlement.","清运周围和连通街道沿线的垃圾。与住宅共边会在每次建造结算时持续扣分。","waste rooms","垃圾站"),
            new("supermarket","Supermarket","超市","Provides one job and two meal seats. Actual dining can leave temporary food for wildlife.","提供 1 个岗位、2 个餐位；当天实际有人用餐后，可留下临时动物食物。","supermarkets","market","markets"),
            new("square","Square","广场","A resident can visit after work and a meal, leaving temporary wildlife food. Surrounding traffic risk is reduced.","居民完成上班、用餐后可到访并留下临时动物食物，也可降低周围街道的车祸风险。","squares","plaza","plazas"),
            new("green","Green habitat","绿地","Provides wildlife habitat and natural food after maturing. Planting type depends on the landing plot.","为动物提供栖息地；成熟后产生自然食物。植物类型取决于落点。","green plots","green patch","green patches","greenery"),
            new("commute","Commuting","通勤","A complete resident routine: home, work, meal, then home. Routes and capacity are allocated automatically.","居民从住宅出发，上班、用餐后回家的一次完整循环。系统自动分配可达路线与名额。","commute","commutes","完整循环","上班、用餐、回家"),
        };
        private static readonly Dictionary<string,Term> ById=Terms.ToDictionary(t=>t.Id);
        private static readonly Dictionary<string,Term> ByWord=Terms.SelectMany(t=>t.Aliases.Select(a=>(a,t))).GroupBy(x=>x.a,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.First().t,StringComparer.OrdinalIgnoreCase);
        private static readonly Regex Words=new(string.Join("|",ByWord.Keys.OrderByDescending(s=>s.Length).Select(s=>
            Regex.IsMatch(s,"^[a-zA-Z ]+$") ? @"(?<![A-Za-z])"+Regex.Escape(s)+@"(?![A-Za-z])" : Regex.Escape(s))),RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        public static bool TryGet(string id,out Term term)=>ById.TryGetValue(id??"",out term);
        public static string Anchor(string heading)=>heading switch {
            "Home"=>"home","Central oak"=>"oak","Green habitats"=>"green","Workshop"=>"workshop",
            "Restaurant"=>"restaurant","Supermarket"=>"supermarket","Street"=>"street","Square"=>"square","Waste room"=>"waste","Commuting"=>"commute",
            _=>Regex.Replace(heading.ToLowerInvariant(),"[^a-z0-9]+","-").Trim('-')};
        public static bool AppliesTo(string name)=>name is "Rules summary" or "Tour body" or "Story instruction" or "Story paragraph" or "Story trigger" or
            "Placement guide" or "Room function" or "Lesson room summary" or "Hand instruction" or "Selected hand tagline" or "Selected hand instruction" or
            "Goal title" or "Selected hand name" or "Selected room relationship explanation" or "Selected relationship room" or "Special task rule" or "Special task selection note";
        public static string Linkify(string copy, bool onDark = false)
        {
            if(string.IsNullOrEmpty(copy))return copy;
            var result=new StringBuilder(copy.Length+100);var linkDepth=0;
            foreach(var part in Regex.Split(copy,"(<[^>]+>)"))
            {
                if(part.StartsWith("<",StringComparison.Ordinal))
                {if(part.StartsWith("<link",StringComparison.OrdinalIgnoreCase))linkDepth++;if(part.StartsWith("</link",StringComparison.OrdinalIgnoreCase))linkDepth=Math.Max(0,linkDepth-1);result.Append(part);}
                else result.Append(linkDepth>0?part:Words.Replace(part,m=>"<link=\"term:"+ByWord[m.Value].Id+"\"><color="+(onDark?"#F1D18A":"#315C50")+"><u>"+m.Value+"</u></color></link>"));
            }
            return result.ToString();
        }
    }
}
