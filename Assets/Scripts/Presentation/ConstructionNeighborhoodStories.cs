using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public enum NeighborhoodStoryKind
    {
        Beginning, Oak, FirstHome, GreenConnected, GreenSanctuary,
        MatureGreen, GardenHome, Terrace, SlowStreet, FirstMeal,
        FirstCycle, FirstWildlifeMeal, MarketFood, WildlifeNeighbours,
        WasteCollected, StreetBesideGreen, WasteBesideHome, Traffic
    }

    public sealed class NeighborhoodStory
    {
        public NeighborhoodStoryKind Kind { get; }
        public int Day { get; }
        public ConstructionCategory Icon { get; }
        public bool Concern { get; }
        public string EnglishTitle { get; }
        public string ChineseTitle { get; }
        public string EnglishBody { get; }
        public string ChineseBody { get; }
        public string EnglishCause { get; }
        public string ChineseCause { get; }

        public NeighborhoodStory(NeighborhoodStoryKind kind, int day,
            ConstructionCategory icon, string englishTitle, string chineseTitle,
            string englishBody, string chineseBody, string englishCause,
            string chineseCause, bool concern = false)
        {
            Kind = kind; Day = day; Icon = icon; Concern = concern;
            EnglishTitle = englishTitle; ChineseTitle = chineseTitle;
            EnglishBody = englishBody; ChineseBody = chineseBody;
            EnglishCause = englishCause; ChineseCause = chineseCause;
        }
    }

    // Read-only stories from saved placements and observed results. Rebuilding
    // this list after loading cannot create food, points or fictional activity.
    public static class ConstructionNeighborhoodStories
    {
        public static IReadOnlyList<NeighborhoodStory> For(ConstructionRunModel run)
        {
            if (run == null) return Array.Empty<NeighborhoodStory>();
            var stories = new List<NeighborhoodStory>();
            var tiles = run.BuiltTiles.OrderBy(tile => tile.buildIndex).ToArray();
            void Add(NeighborhoodStoryKind kind, int day, ConstructionCategory icon,
                string enTitle, string zhTitle, string enBody, string zhBody,
                string enCause, string zhCause, bool concern = false) =>
                stories.Add(new NeighborhoodStory(kind, day, icon, enTitle, zhTitle,
                    enBody, zhBody, enCause, zhCause, concern));

            foreach (var tile in tiles)
            {
                var day = Math.Max(1, tile.builtDay);
                var before = tiles.Where(other => other.buildIndex < tile.buildIndex).ToArray();
                var after = before.Append(tile).ToArray();
                if (tile.id == ConstructionBoardModel.StarterOakId)
                    Add(NeighborhoodStoryKind.Oak, day, ConstructionCategory.Green,
                        "Shade for the earliest neighbour", "先给老邻居留一片树荫",
                        "Before the streets fill with people, a little shade is kept for the animals who were already here.",
                        "街坊还没热闹起来，树荫先留给了原本住在这里的小动物。这座城的故事，从照顾一位老邻居开始。",
                        "The central oak has been placed.", "中央橡树已放置，动物有了最初的栖地。");
                if (tile.category == ConstructionCategory.Residence &&
                    !before.Any(other => other.category == ConstructionCategory.Residence))
                    Add(NeighborhoodStoryKind.FirstHome, day, tile.category,
                        "A light in the first window", "第一扇窗里亮起了灯",
                        "A new neighbour has a home. Now the neighbourhood needs places they can reach for work and a meal.",
                        "一扇门打开了，新邻居有了住处。接下来，街坊还要让上班和吃饭都有着落，日子才能慢慢安稳下来。",
                        "The first home was built; reachable work and meal places are still needed.",
                        "第一间住宅建成；居民仍需要可到达的岗位和餐位。");

                if (run.UsesNeighborUpgrades)
                {
                    var upgraded = ConstructionPlacementScore.NewlyUpgradedTiles(tile, after);
                    var greens = upgraded.Where(other => other.category == ConstructionCategory.Green).ToArray();
                    if (greens.Length > 0)
                    {
                        var level = greens.Max(green => ConstructionPlacementScore.GreenUpgradeLevel(green, after));
                        Add(level >= 3 ? NeighborhoodStoryKind.GreenSanctuary : NeighborhoodStoryKind.GreenConnected,
                            day, ConstructionCategory.Green,
                            level >= 3 ? "A patch becomes a little refuge" : "The shade joins up",
                            level >= 3 ? "几片绿意，连成一处小天地" : "树荫终于连起来了",
                            "What was a separate patch is now part of a larger green space. New planting still needs time to mature.",
                            "原本零散的绿意接到了一起，街坊多了一片完整的绿色角落。新种下的植物还要慢慢长成，食物不会凭空出现。",
                            $"Shared-edge green connection reached level {level}; maturity is checked separately.",
                            $"绿地沿边连片，升至 {level} 级；新植地块仍需等待成熟。");
                    }
                    if (upgraded.Any(other => other.category == ConstructionCategory.Residence))
                        Add(NeighborhoodStoryKind.GardenHome, day, ConstructionCategory.Residence,
                            "A garden outside the window", "窗外，多了一片小花园",
                            "The home now faces a connected green patch. The view beyond its door belongs to a greener neighbourhood.",
                            "推开窗，门外不再只是一小块零散的绿地。连起来的树荫和草木，让这户人家有了自己的花园日常。",
                            "A home borders a green patch containing at least two edge-connected plots.",
                            "住宅邻接绿地；这片绿地已沿边连成至少 2 格。");
                    if (upgraded.Any(other => other.category == ConstructionCategory.Restaurant))
                        Add(NeighborhoodStoryKind.Terrace, day, ConstructionCategory.Restaurant,
                            "One more chair at the corner", "街角，又添了一把椅子",
                            "The restaurant opens towards the plaza. There is room for one more diner, if someone can reach the table.",
                            "餐厅朝着广场多摆出了一把椅子。街坊的饭桌宽裕了一点，至于谁能坐下来，还要看路是否接通。",
                            "A restaurant and plaza share an edge: meal capacity increased from 2 to 3.",
                            "餐厅与广场共边，餐位从 2 个增至 3 个。");
                    if (upgraded.Any(other => other.category == ConstructionCategory.Street))
                        Add(NeighborhoodStoryKind.SlowStreet, day, ConstructionCategory.Street,
                            "A gentler crossing", "这条路，走得慢一点",
                            "Green plots face one another across the street. The crossing is safer now, though passing animals still face some risk.",
                            "路的两侧有了相望的绿意。小动物穿过去时，多了一分从容；街道仍有车流，风险没有完全消失。",
                            "Opposite sides of a street have green plots: collision risk is halved.",
                            "街道相对两侧都有绿地，过街风险减半。");
                }

                if (tile.category == ConstructionCategory.Green &&
                    tile.id != ConstructionBoardModel.StarterOakId && day + 2 <= run.CurrentDay)
                    Add(NeighborhoodStoryKind.MatureGreen, day + 2, ConstructionCategory.Green,
                        "The new green has settled in", "新种下的绿意，长成了",
                        "The young planting has had time to settle. It can now take part in the neighbourhood's food cycle; each harvest has its own rhythm.",
                        "起初稀疏的绿意终于站稳了。它开始参与街坊的食物循环，草籽、浆果和橡果各有自己的收成节奏。",
                        "Two growing days have passed; actual food yield depends on plant type and harvest.",
                        "新绿地已生长 2 天；实际产量仍取决于植物类型和收成。");

                if (run.UsesPlacementScore)
                    foreach (ConstructionCategory neighbourKind in Enum.GetValues(typeof(ConstructionCategory)))
                    {
                        var points = ConstructionPlacementScore.PairPoints(tile.category, neighbourKind, run.PlacementRulesVersion);
                        if (points >= 0) continue;
                        var edges = before.Count(other => other.category == neighbourKind &&
                            (run.PlacementRulesVersion >= ConstructionPlacementScore.BalancedRulesVersion
                                ? ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row, other.column, other.row)
                                : Math.Abs(tile.column - other.column) <= 1 && Math.Abs(tile.row - other.row) <= 1));
                        if (edges == 0) continue;
                        var street = tile.category == ConstructionCategory.Street || neighbourKind == ConstructionCategory.Street;
                        Add(street ? NeighborhoodStoryKind.StreetBesideGreen : NeighborhoodStoryKind.WasteBesideHome,
                            day, tile.category, street ? "Traffic beside the shade" : "Too close to the front door",
                            street ? "树荫边，多了车流" : "清运的地方，离家门近了些",
                            street ? "A road now borders the green. The quiet corner has become a place where wildlife needs to cross carefully."
                                : "The waste room helps keep the neighbourhood clean, but sharing an edge with a home brings a disturbance too.",
                            street ? "车流紧贴着绿地，安静的角落多了一次需要小心的穿越。便利的道路，也会给野生邻居带来代价。"
                                : "清运设施帮街坊处理垃圾，却也让旁边这户人家多了一份近邻干扰。便利和舒适，未必总在同一个位置。",
                            $"{edges} new unfavourable shared edge(s): construction {points * edges}.",
                            $"新增 {edges} 组{(street ? "街道—绿地" : "垃圾间—住宅")}{(run.PlacementRulesVersion >= 4 ? "共边" : "相邻")}，建造 {points * edges} 分。", true);
                    }
            }

            var days = run.Export().score.days;
            var firstMeal = run.FirstResidentMealDay;
            if (firstMeal > 0)
                Add(NeighborhoodStoryKind.FirstMeal, firstMeal, ConstructionCategory.Restaurant,
                    "The first meal has a place", "第一顿饭，有了着落",
                    "The new neighbour has found a place to eat. A reachable table has become part of an ordinary day.",
                    "新邻居终于不用只在门口张望。今天，这位邻居找到了能走到、也有余量的用餐地点，日常生活向前迈了一小步。",
                    "A resident actually ate at an available, reachable meal place.",
                    "居民首次实际用餐；可到达的餐位被使用了。");
            var firstCycle = days.FirstOrDefault(day => day.completedWorkCycles > 0);
            if (firstCycle != null)
                Add(NeighborhoodStoryKind.FirstCycle, firstCycle.day, ConstructionCategory.Residence,
                    "Someone comes home at dusk", "傍晚，有人顺利回家了",
                    "Work, a meal, then home: the neighbourhood has supported its first complete daily routine.",
                    "上班、吃饭，再沿着熟悉的路回家。看似平常的一天，终于在这座小街坊里完整地发生了。",
                    $"{firstCycle.completedWorkCycles} resident(s) completed work, a meal and the return home that day.",
                    $"当天 {firstCycle.completedWorkCycles} 位居民实际完成上班、用餐、回家循环。");
            var firstWildlifeMeal = days.FirstOrDefault(day => day.animalMeals > 0);
            if (firstWildlifeMeal != null)
                Add(NeighborhoodStoryKind.FirstWildlifeMeal, firstWildlifeMeal.day, ConstructionCategory.Green,
                    "A first meal for the wild neighbours", "野生邻居，也吃上饭了",
                    "The little neighbours have found something to eat. The green spaces are becoming part of their everyday lives.",
                    "小动物找到了可以吃的东西。这片街坊不只是在地图上留出绿色，也开始接住它们每天真实的生活。",
                    "Wildlife actually ate food in a settled day.", "动物已在实际结算中进食。");

            var ecology = run.LastEcologyDay;
            if (ecology != null)
            {
                var sources = (ecology.squirrelFoodTileIds ?? new List<string>())
                    .Concat(ecology.pigeonMealTileIds ?? new List<string>())
                    .Concat(ecology.hedgehogMealTileIds ?? new List<string>());
                var marketMeals = sources.Count(id => tiles.Any(tile =>
                    tile.id == id && tile.category == ConstructionCategory.Supermarket));
                if (marketMeals > 0)
                    Add(NeighborhoodStoryKind.MarketFood, ecology.day, ConstructionCategory.Supermarket,
                        "A little food left for a neighbour", "餐桌之外，还留了一小份",
                        "Food from the market has reached the wild neighbours too. A human meal and an animal's search for food have met at the same corner.",
                        "超市留下的食物没有白白消失。街角的小动物找到了它，人的餐桌和动物的觅食，在这里有了一次相遇。",
                        $"Wildlife actually ate {marketMeals} market food portion(s) in this settlement.",
                        $"本次动物实际吃到 {marketMeals} 份超市食物。");
                if (ecology.pigeonsPresent > 0 || ecology.hedgehogPresent)
                    Add(NeighborhoodStoryKind.WildlifeNeighbours, ecology.day, ConstructionCategory.Green,
                        "More kinds of neighbour", "街坊里，有了不同的邻居",
                        "There are more ways to live here than one. Wildlife observed today is making use of the neighbourhood's habitats.",
                        "树下的日子不只属于一种动物。今天，街坊里出现了不同的身影，这些栖地开始被真正使用。",
                        $"Observed: {ecology.pigeonsPresent} pigeon(s)" + (ecology.hedgehogPresent ? " and a hedgehog." : "."),
                        $"本次观察到 {ecology.pigeonsPresent} 只常驻鸽子" + (ecology.hedgehogPresent ? "，并有刺猬出现。" : "。"));
                if (ecology.trafficSpecies != ConstructionTrafficSpecies.None)
                    Add(NeighborhoodStoryKind.Traffic, ecology.day, ConstructionCategory.Street,
                        "A difficult day at the crossing", "今天的过街，没有平安抵达",
                        "One wild neighbour was lost on the road. The convenience of a street has carried a cost for another life.",
                        "一位野生邻居没能平安穿过街道。道路让城市更方便，也提醒街坊：另一种生活，同样需要被照顾。",
                        $"One traffic death was recorded; animal score -{run.LastScoreDay?.trafficPenaltyPoints ?? 0}.",
                        $"本次实际发生 1 起车祸，动物扣 {run.LastScoreDay?.trafficPenaltyPoints ?? 0} 分。", true);
            }
            if (run.LastWasteDay?.Cleared > 0)
                Add(NeighborhoodStoryKind.WasteCollected, run.LastWasteDay.day, ConstructionCategory.Waste,
                    "The street corner is cared for", "街角，也有人照料",
                    "The waste collection has done its work. The neighbourhood's daily life includes the quiet work of keeping it clean.",
                    "街坊的热闹也会留下垃圾。今天，清运认真地运转起来，干净的角落背后，又多了一份不显眼的照料。",
                    $"{run.LastWasteDay.Cleared} waste portion(s) were actually collected today.",
                    $"本次实际清运 {run.LastWasteDay.Cleared} 份垃圾。");

            if (stories.Count == 0)
                Add(NeighborhoodStoryKind.Beginning, 0, ConstructionCategory.Green,
                    "A city waiting for its first shade", "一座城，等着第一片树荫",
                    "It is quiet here for now. The city can begin by making a little room for those who lived here first.",
                    "这里暂时还很安静。城可以慢慢长大，先给原本住在这里的动物留一点地方，也给未来的街坊留一个开始。",
                    "No plots have been placed yet; start with the central oak.",
                    "尚未放置地块；先安放中央橡树。");
            return stories.OrderByDescending(story => story.Day)
                .ThenByDescending(story => Priority(story.Kind)).ToArray();
        }

        private static int Priority(NeighborhoodStoryKind kind) => kind switch
        {
            NeighborhoodStoryKind.Traffic => 100,
            NeighborhoodStoryKind.StreetBesideGreen or NeighborhoodStoryKind.WasteBesideHome => 90,
            NeighborhoodStoryKind.MarketFood => 80,
            NeighborhoodStoryKind.GardenHome or NeighborhoodStoryKind.Terrace or NeighborhoodStoryKind.SlowStreet => 75,
            NeighborhoodStoryKind.FirstCycle => 70,
            NeighborhoodStoryKind.GreenConnected or NeighborhoodStoryKind.GreenSanctuary => 65,
            NeighborhoodStoryKind.FirstMeal => 60,
            NeighborhoodStoryKind.MatureGreen => 55,
            NeighborhoodStoryKind.WildlifeNeighbours => 40,
            NeighborhoodStoryKind.WasteCollected => 20,
            _ => 30
        };
    }
}
