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
                        "The first oak", "橡树种下了",
                        "The first shade belongs to the animals already living here.",
                        "人还没搬来，松鼠先有了一片树荫。",
                        "The central oak has been placed.", "中央橡树已放置，动物有了最初的栖地。");
                if (tile.category == ConstructionCategory.Residence &&
                    !before.Any(other => other.category == ConstructionCategory.Residence))
                    Add(NeighborhoodStoryKind.FirstHome, day, tile.category,
                        "A new neighbour", "有人搬来了",
                        "A neighbour has moved in. They still need work and somewhere to eat.",
                        "新邻居搬进了住宅，还得给他接通上班和吃饭的路。",
                        "First home built. Connect it to work and a meal place.",
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
                            level >= 3 ? "A small green refuge" : "The shade joins up",
                            level >= 3 ? "绿地连成了小公园" : "绿地连起来了",
                            "The green patches have joined up. New plants still need time to grow.",
                            "两片绿地接到了一起。刚种下的植物还得长几天，才能供食。",
                            $"Connected green: level {level}. New plants still need to mature.",
                            $"绿地连片升至 {level} 级；新植物仍需成熟。");
                    }
                    if (upgraded.Any(other => other.category == ConstructionCategory.Residence))
                        Add(NeighborhoodStoryKind.GardenHome, day, ConstructionCategory.Residence,
                            "A garden at home", "窗外有了花园",
                            "A connected green patch now grows beside this home.",
                            "门外的绿地连成了片，这户人家有了花园。",
                            "The home touches a connected patch of at least 2 green plots.",
                            "住宅贴着一片至少 2 格的连片绿地。");
                    if (upgraded.Any(other => other.category == ConstructionCategory.Restaurant))
                        Add(NeighborhoodStoryKind.Terrace, day, ConstructionCategory.Restaurant,
                            "One more seat", "餐厅多了一个座位",
                            "The restaurant has room for one more diner. Keep the route open.",
                            "餐厅朝广场多摆了一把椅子。路接通，邻居才能坐下来。",
                            "Restaurant beside a plaza: meal seats increase from 2 to 3.",
                            "餐厅与广场共边，餐位从 2 个增至 3 个。");
                    if (upgraded.Any(other => other.category == ConstructionCategory.Street))
                        Add(NeighborhoodStoryKind.SlowStreet, day, ConstructionCategory.Street,
                            "A gentler crossing", "这条路慢下来了",
                            "Green plots face each other across the road. Crossing is safer, but still risky.",
                            "街道两侧都有了绿地。小动物过街时风险减半，仍要小心。",
                            "Green on opposite sides of the road: crossing risk halved.",
                            "街道相对两侧都有绿地，过街风险减半。");
                }

                if (tile.category == ConstructionCategory.Green &&
                    tile.id != ConstructionBoardModel.StarterOakId && day + 2 <= run.CurrentDay)
                    Add(NeighborhoodStoryKind.MatureGreen, day + 2, ConstructionCategory.Green,
                        "New plants are ready", "新绿地成熟了",
                        "The new planting is mature. Its harvest depends on the plant type.",
                        "新种的植物长成了，开始提供食物。不同植物的收成不一样。",
                        "Grown for 2 days. Yield depends on the plant and its harvest.",
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
                            day, tile.category, street ? "Traffic by the green" : "Too close to home",
                            street ? "车流靠近了绿地" : "垃圾间离住宅太近",
                            street ? "Traffic now runs beside the green, where animals search for food."
                                : "The waste room is right beside a home. Its neighbours pay for the disturbance.",
                            street ? "动物觅食的地方紧挨着车流。这次共边搭配会扣分。"
                                : "垃圾间贴着住宅，打扰了旁边的住户。这次搭配会扣分。",
                            $"{edges} poor pairing{(edges == 1 ? "" : "s")}: build score {points * edges}.",
                            $"新增 {edges} 组{(street ? "街道—绿地" : "垃圾间—住宅")}{(run.PlacementRulesVersion >= 4 ? "共边" : "相邻")}，建造 {points * edges} 分。", true);
                    }
            }

            var days = run.Export().score.days;
            var firstMeal = run.FirstResidentMealDay;
            if (firstMeal > 0)
                Add(NeighborhoodStoryKind.FirstMeal, firstMeal, ConstructionCategory.Restaurant,
                    "The first meal", "邻居吃上了第一顿饭",
                    "A neighbour reached a meal place and ate there for the first time.",
                    "这位邻居走到了用餐地点，吃上了搬来后的第一顿饭。",
                    "A resident reached a meal place and ate there.",
                    "居民首次实际用餐；可到达的餐位被使用了。");
            var firstCycle = days.FirstOrDefault(day => day.completedWorkCycles > 0);
            if (firstCycle != null)
                Add(NeighborhoodStoryKind.FirstCycle, firstCycle.day, ConstructionCategory.Residence,
                    "Home after work", "忙完一天，回家了",
                    "For the first time, a neighbour worked, ate and made it home.",
                    "上班、吃饭，再回家。终于有邻居顺利走完了这一天。",
                    $"{firstCycle.completedWorkCycles} resident{(firstCycle.completedWorkCycles == 1 ? "" : "s")} worked, ate and returned home.",
                    $"当天 {firstCycle.completedWorkCycles} 位居民实际完成上班、用餐、回家循环。");
            var firstWildlifeMeal = days.FirstOrDefault(day => day.animalMeals > 0);
            if (firstWildlifeMeal != null)
                Add(NeighborhoodStoryKind.FirstWildlifeMeal, firstWildlifeMeal.day, ConstructionCategory.Green,
                    "Wildlife finds food", "动物找到了食物",
                    "The animals have found food and eaten it.",
                    "小动物找到了食源，这一顿没有饿肚子。",
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
                        "Market leftovers", "动物吃了超市余粮",
                        "Animals found food left by the market and ate it.",
                        "小动物找到了超市留下的食物，也吃上了一份。",
                        $"Wildlife ate {marketMeals} portion{(marketMeals == 1 ? "" : "s")} left at markets today.",
                        $"本次动物实际吃到 {marketMeals} 份超市食物。");
                if (ecology.pigeonsPresent > 0 || ecology.hedgehogPresent)
                    Add(NeighborhoodStoryKind.WildlifeNeighbours, ecology.day, ConstructionCategory.Green,
                        "New neighbours", "又有新邻居",
                        "Pigeons or a hedgehog were seen in the neighbourhood today.",
                        "今天见到了鸽子或刺猬，绿地里又多了几道身影。",
                        ecology.pigeonsPresent == 0 ? "Observed: a hedgehog." :
                            $"Observed: {ecology.pigeonsPresent} pigeon{(ecology.pigeonsPresent == 1 ? "" : "s")}" +
                            (ecology.hedgehogPresent ? " and a hedgehog." : "."),
                        $"本次观察到 {ecology.pigeonsPresent} 只常驻鸽子" + (ecology.hedgehogPresent ? "，并有刺猬出现。" : "。"));
                if (ecology.trafficSpecies != ConstructionTrafficSpecies.None)
                    Add(NeighborhoodStoryKind.Traffic, ecology.day, ConstructionCategory.Street,
                        "A road casualty", "一只动物没能过街",
                        "An animal was killed while crossing the road.",
                        "一只动物在过街时被撞死了。留意绿地旁的车流。",
                        $"One traffic death was recorded; animal score -{run.LastScoreDay?.trafficPenaltyPoints ?? 0}.",
                        $"本次实际发生 1 起车祸，动物扣 {run.LastScoreDay?.trafficPenaltyPoints ?? 0} 分。", true);
            }
            if (run.LastWasteDay?.Cleared > 0)
                Add(NeighborhoodStoryKind.WasteCollected, run.LastWasteDay.day, ConstructionCategory.Waste,
                    "Rubbish collected", "垃圾运走了",
                    "The waste room collected rubbish from the neighbourhood today.",
                    "今天的清运接上了，堆着的垃圾被运走了一些。",
                    $"Collected {run.LastWasteDay.Cleared} portion{(run.LastWasteDay.Cleared == 1 ? "" : "s")} of waste today.",
                    $"本次实际清运 {run.LastWasteDay.Cleared} 份垃圾。");

            if (stories.Count == 0)
                Add(NeighborhoodStoryKind.Beginning, 0, ConstructionCategory.Green,
                    "Start with an oak", "从橡树开始",
                    "Plant the central oak first. The squirrel needs a home.",
                    "先种下中央橡树，给松鼠留一个家。",
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
