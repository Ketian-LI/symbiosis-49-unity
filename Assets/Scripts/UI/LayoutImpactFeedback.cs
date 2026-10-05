using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    public enum LayoutImpactMetricKind
    {
        Seeds,
        PigeonSeedMealCeiling,
        ParkLinks,
        GreenCells,
        Workers,
        Production,
        Waste,
        Shelter,
        MarketWaste,
        ShrubRecovery,
        AnimalPassages,
        PigeonFoodAccess,
        SquirrelFoodAccess,
        HedgehogFoodAccess,
        FoxPreyAccess,
        BufferedGarages
    }

    public readonly struct LayoutImpactMetric
    {
        public LayoutImpactMetric(LayoutImpactMetricKind kind, string label, string detail,
            float before, float after, bool higherIsBetter, bool delayed = false,
            bool neutral = false, bool persistentRisk = false)
        {
            Kind = kind;
            Label = label;
            Detail = detail;
            Before = before;
            After = after;
            HigherIsBetter = higherIsBetter;
            Delayed = delayed;
            Neutral = neutral;
            PersistentRisk = persistentRisk;
        }

        public LayoutImpactMetricKind Kind { get; }
        public string Label { get; }
        public string Detail { get; }
        public float Before { get; }
        public float After { get; }
        public float Delta => After - Before;
        public bool HigherIsBetter { get; }
        public bool Delayed { get; }
        public bool Neutral { get; }
        public bool PersistentRisk { get; }
        public bool Changed => Math.Abs(Delta) >= 0.05f;
        public bool IsRisk => !Delayed && !Neutral &&
                              (PersistentRisk || Changed && (Delta > 0f) != HigherIsBetter);
        public string BeforeText => Before.ToString("0.#");
        public string DeltaText => Delta > 0.05f ? $"+{Delta:0.#}" : Delta < -0.05f ? $"{Delta:0.#}" : "0";
    }

    // Keep the drop preview decision-oriented. Every displayed number is a
    // comparison with the layout captured when the player entered edit mode.
    public static class LayoutImpactFeedback
    {
        private const string Good = "#82DCC7";
        private const string Bad = "#F3A984";
        private const string Neutral = "#E9DFC9";

        // Two metrics are shown per page. Risks come first, then gains, then unchanged
        // values; every metric remains reachable without opening a paragraph panel.
        public static IReadOnlyList<LayoutImpactMetric> BuildVisualMetrics(
            RoomLayoutImpactPreview impact, bool chinese)
        {
            var seedsBelowBirds = impact.LivingPigeons > 0 &&
                                  impact.AfterSeedCapacity < impact.LivingPigeons;
            var metrics = new List<LayoutImpactMetric>
            {
                new(LayoutImpactMetricKind.Seeds,
                    impact.LivingPigeons >= 0
                        ? chinese ? $"次日种子 / 鸽 {impact.LivingPigeons}" +
                                    (seedsBelowBirds ? " · 仍偏少" : string.Empty)
                            : $"Dawn seeds / {impact.LivingPigeons} birds" +
                              (seedsBelowBirds ? " · LOW" : string.Empty)
                        : chinese ? "次日种子基准" : "Dawn seed target",
                    chinese
                        ? "这是新增补给基准，不是保证餐数；现有存量、路线、到达时间和捕食仍会影响结果。"
                        : "New replenishment, not guaranteed meals; current stock, routes, arrival time and predation still matter.",
                    impact.BeforeSeedCapacity, impact.AfterSeedCapacity, true,
                    persistentRisk: seedsBelowBirds),
                new(LayoutImpactMetricKind.ParkLinks, chinese ? "增产鸽群房" : "Productive plazas",
                    chinese ? "动物通道连接到稳定绿地、且未轮休的鸽群房数量。"
                        : "Pigeon plazas joined to stable greenery by animal passages and not resting.",
                    impact.BeforeParkEdges, impact.AfterParkEdges, true),
                new(LayoutImpactMetricKind.Workers, chinese ? "通勤循环" : "Commute loop",
                    chinese ? "能完成住宅→工作→餐饮→住宅循环的人数；连续 3 天失败的居民会离开，总人数低于 3 人才结束。"
                        : "Residents with a home-work-meal-home loop. Three failed days cause departure; the run ends below three residents.",
                    impact.Before.WorkingResidents, impact.After.WorkingResidents, true),
                new(LayoutImpactMetricKind.Waste, chinese ? "垃圾异常" : "Waste failures",
                    chinese ? $"按满员 {ResidentPopulationModel.MaximumResidents} 人测试的超载或送达失败点，越少越好。"
                        : $"Overflow or blocked sites at {ResidentPopulationModel.MaximumResidents} residents; lower is better.",
                    impact.BeforeFullPopulationWasteOverflow, impact.AfterFullPopulationWasteOverflow, false),
                new(LayoutImpactMetricKind.Shelter, chinese ? "今夜庇护" : "Cover tonight",
                    chinese ? "今晚可用的相邻灌木组；不保证动物觅食成功。" : "Connected shrub pairs tonight; not a guaranteed meal.",
                    impact.BeforeShelterPairs, impact.AfterShelterPairs, true)
            };
            if (impact.LivingPigeons >= 0 &&
                impact.BeforePigeonSeedMealCeiling >= 0 &&
                impact.AfterPigeonSeedMealCeiling >= 0)
            {
                var ceilingLow = impact.AfterPigeonSeedMealCeiling < impact.LivingPigeons;
                if (ceilingLow || impact.BeforePigeonSeedMealCeiling != impact.AfterPigeonSeedMealCeiling)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.PigeonSeedMealCeiling,
                        chinese ? $"种子最多覆盖 / 鸽 {impact.LivingPigeons}" +
                                  (ceilingLow ? " · 不足" : string.Empty)
                            : $"Seed reach / {impact.LivingPigeons} birds" +
                              (ceilingLow ? " · LOW" : string.Empty),
                        chinese ? "按各鸽群的位置、动物通道和次日新增种子份数计算的理论上限；不含旧存量、其他食物和实际行走。"
                            : "Theoretical ceiling from flock homes, wildlife routes and new dawn seeds; excludes old stock, other food and travel time.",
                        impact.BeforePigeonSeedMealCeiling,
                        impact.AfterPigeonSeedMealCeiling, true,
                        persistentRisk: ceilingLow));
            }
            if (impact.BeforeGreenCells >= 0 && impact.AfterGreenCells >= 0)
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.GreenCells,
                    chinese ? "连通绿地" : "Linked greenery",
                    chinese ? "从中央公园沿匹配的动物通道可到达的绿地格数；至少 4 格才稳定。"
                        : "Green cells reachable from the park through matching animal passages; 4 make a stable network.",
                    impact.BeforeGreenCells, impact.AfterGreenCells, true));
            if (impact.MarketDay > 0)
            {
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.MarketWaste,
                    chinese ? "轮值垃圾" : "Rotation waste",
                    chinese ? $"第 {impact.MarketDay} 天轮值时的垃圾异常点；越少越好。"
                        : $"Waste failures during the day {impact.MarketDay} rotation; lower is better.",
                    impact.BeforeMarketOverflow, impact.AfterMarketOverflow, false));
            }
            if (impact.BeforeBufferedGarages != impact.AfterBufferedGarages)
            {
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.BufferedGarages,
                    chinese ? "车库缓冲" : "Buffered garages",
                    chinese ? "邻接生态缓冲庭的车库数；穿越该车库仍有风险。"
                        : "Garages beside an ecological buffer; crossings remain risky.",
                    impact.BeforeBufferedGarages, impact.AfterBufferedGarages, true));
            }
            if (impact.BeforeFoodAccess is { } beforeAccess &&
                impact.AfterFoodAccess is { } afterAccess)
            {
                var pigeonPlazas = RoomLayoutData.All.Count(room =>
                    room.Type == RoomType.PigeonHabitat);
                var pigeonRoutesLow = impact.LivingPigeons > 0 &&
                                      afterAccess.Pigeon < pigeonPlazas;
                if (beforeAccess.Pigeon != afterAccess.Pigeon || pigeonRoutesLow)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.PigeonFoodAccess,
                    chinese ? $"鸽群食路 / {pigeonPlazas}" +
                              (pigeonRoutesLow ? " · 仍不足" : string.Empty)
                        : $"Pigeon food routes / {pigeonPlazas}" +
                          (pigeonRoutesLow ? " · LOW" : string.Empty),
                    chinese ? "明日可通往种子补给地点的鸽群房数；路线可达仍不保证每只鸽子实际进食。"
                        : "Pigeon rooms with a route to next-day seed replenishment; a route still does not guarantee each bird eats.",
                    beforeAccess.Pigeon, afterAccess.Pigeon, true,
                    persistentRisk: pigeonRoutesLow));
                if (beforeAccess.Squirrel != afterAccess.Squirrel)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.SquirrelFoodAccess,
                    chinese ? "松鼠食路" : "Squirrel food routes",
                    chinese ? "明日可通往坚果来源的栖地数；包括连通的松鼠林地，搬树会使树倒下。"
                        : "Habitats with a route to next-day nuts, including the linked squirrel grove; moved trees are felled.",
                    beforeAccess.Squirrel, afterAccess.Squirrel, true));
                if (beforeAccess.Hedgehog != afterAccess.Hedgehog)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.HedgehogFoodAccess,
                    chinese ? "刺猬食路" : "Hedgehog food routes",
                    chinese ? "明日可通往公园或连通刺猬花园昆虫点的栖地数；不计随机昆虫。"
                        : "Habitats with a route to park or linked hedgehog-garden insects; random insects excluded.",
                    beforeAccess.Hedgehog, afterAccess.Hedgehog, true));
                if (beforeAccess.FoxPrey != afterAccess.FoxPrey || afterAccess.FoxPrey > 0)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.FoxPreyAccess,
                    chinese ? "狐狸可达猎物" : "Fox reaches prey",
                    chinese ? "明日狐狸能否从边缘栖地通往任一猎物栖息地；这是捕食风险，不代表一定发生。"
                        : "Whether the fox can reach a prey habitat from its edge refuge tomorrow; a predation risk, not a certain kill.",
                    beforeAccess.FoxPrey, afterAccess.FoxPrey, false,
                    persistentRisk: afterAccess.FoxPrey > 0));
            }
            else if (impact.BeforeAnimalConnections >= 0 && impact.AfterAnimalConnections >= 0)
            {
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.AnimalPassages,
                    chinese ? "次日动物通道" : "Next-day passages",
                    chinese ? "次日对齐且可通过的动物门。增加可能帮助觅食，也可能扩大狐狸活动范围。人走独立道路。"
                        : "Matched next-day animal doors. More may help foraging but can extend fox reach. People use separate roads.",
                    impact.BeforeAnimalConnections, impact.AfterAnimalConnections, true,
                    neutral: true));
            }
            if (impact.MovedShrubs > 0)
            {
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.ShrubRecovery,
                    chinese ? $"第 {impact.ShelterRecoveryDay} 天恢复" : $"Recovers day {impact.ShelterRecoveryDay}",
                    chinese ? $"搬动 {impact.MovedShrubs} 株灌木；恢复后预计 {impact.RecoveredShelterPairs} 组。今夜庇护另见单独指标。"
                        : $"{impact.MovedShrubs} shrubs moved; {impact.RecoveredShelterPairs} pairs projected after recovery. See tonight's cover separately.",
                    impact.BeforeShelterPairs, impact.RecoveredShelterPairs, true, true));
            }
            return metrics.OrderBy(metric => metric.IsRisk ? 0 : metric.Delayed ? 1 : metric.Changed ? 2 : 3)
                .ThenBy(metric => metric.Kind).ToArray();
        }

        public static string Build(RoomLayoutImpactPreview impact, bool chinese,
            bool dragging, bool withinFreeAllowance)
        {
            var rooms = ChangedRoomNames(impact.ChangedRoomIds, chinese);
            var roomLabel = rooms.Count == 0
                ? chinese ? "布局" : "layout"
                : string.Join(chinese ? "、" : ", ", rooms.Take(3)) +
                  (rooms.Count > 3
                      ? chinese ? $"等 {rooms.Count} 间" : $" and {rooms.Count - 3} more"
                      : string.Empty);
            var cost = withinFreeAllowance
                ? chinese ? "今日调整免费" : "Free rearrangement today"
                : chinese ? "超出今日免费调整范围" : "Outside today's free rearrangement allowance";
            var heading = chinese
                ? $"<b>{(dragging ? "若现在松手" : "待确认")}：{roomLabel}</b>  ·  {cost}"
                : $"<b>{(dragging ? "If released now" : "Pending")}: {roomLabel}</b>  ·  {cost}";

            var seedDelta = impact.AfterSeedCapacity - impact.BeforeSeedCapacity;
            var parkDelta = impact.AfterParkEdges - impact.BeforeParkEdges;
            var greenDelta = impact.BeforeGreenCells >= 0 && impact.AfterGreenCells >= 0
                ? impact.AfterGreenCells - impact.BeforeGreenCells
                : parkDelta;
            var workerDelta = impact.After.WorkingResidents - impact.Before.WorkingResidents;
            var wasteDelta = impact.AfterFullPopulationWasteOverflow -
                             impact.BeforeFullPopulationWasteOverflow;
            var marketDelta = impact.AfterMarketOverflow - impact.BeforeMarketOverflow;
            var summary = Summary(chinese, seedDelta, greenDelta, workerDelta,
                wasteDelta,
                impact.MarketDay > 0 ? marketDelta : 0);

            var greenState = impact.AfterGreenCells >= GreenNetworkModel.StableCellCount
                ? chinese ? "绿地稳定" : "stable greenery"
                : chinese ? "绿地不足 4 格" : "fewer than 4 linked cells";
            var greenDetail = impact.BeforeGreenCells < 0 || impact.AfterGreenCells < 0
                ? string.Empty
                : chinese
                    ? $"；连通绿地 {impact.BeforeGreenCells}→{impact.AfterGreenCells} 格（{greenState}）"
                    : $"; linked greenery {impact.BeforeGreenCells}→{impact.AfterGreenCells} ({greenState})";
            var food = chinese
                ? $"食物｜次日种子 {impact.BeforeSeedCapacity}→{impact.AfterSeedCapacity} {Tag(seedDelta, true, true)}{greenDetail}"
                : $"Food | dawn seeds {impact.BeforeSeedCapacity}→{impact.AfterSeedCapacity} {Tag(seedDelta, true, false)}{greenDetail}";
            var commute = chinese
                ? $"居民｜可完成循环 {impact.Before.WorkingResidents}→{impact.After.WorkingResidents} {Tag(workerDelta, true, true)}；连续 3 天失败会离开"
                : $"People | complete loop {impact.Before.WorkingResidents}→{impact.After.WorkingResidents} {Tag(workerDelta, true, false)}; three failed days cause departure";
            var waste = chinese
                ? $"垃圾｜满员 {ResidentPopulationModel.MaximumResidents} 人压力测试：超载/送达失败点 {impact.BeforeFullPopulationWasteOverflow}→{impact.AfterFullPopulationWasteOverflow} {Tag(wasteDelta, false, true)}"
                : $"Waste | {ResidentPopulationModel.MaximumResidents}-resident test: overflow/blocked sites {impact.BeforeFullPopulationWasteOverflow}→{impact.AfterFullPopulationWasteOverflow} {Tag(wasteDelta, false, false)}";
            var lines = new List<string> { heading, summary, food, commute, waste };
            var shelterDelta = impact.AfterShelterPairs - impact.BeforeShelterPairs;
            var recovery = impact.MovedShrubs > 0
                ? chinese
                    ? $"；搬动 {impact.MovedShrubs} 株，第 {impact.ShelterRecoveryDay} 天恢复后预计 {impact.RecoveredShelterPairs} 组"
                    : $"; {impact.MovedShrubs} moved, projected {impact.RecoveredShelterPairs} after recovery on day {impact.ShelterRecoveryDay}"
                : string.Empty;
            lines.Add(chinese
                ? $"刺猬庇护｜今夜相邻灌木 {impact.BeforeShelterPairs}→{impact.AfterShelterPairs} 组 {Tag(shelterDelta, true, true)}{recovery}"
                : $"Hedgehog cover | connected shrub pairs tonight {impact.BeforeShelterPairs}→{impact.AfterShelterPairs} {Tag(shelterDelta, true, false)}{recovery}");
            if (ParkEdgeRestSchedule.TryGetRestingCell(impact.SeedForecastDay,
                    out var restingColumn, out var restingRow))
            {
                lines.Add(chinese
                    ? $"绿地轮休｜第 {impact.SeedForecastDay} 天 C{restingColumn + 1}R{restingRow + 1} 上的鸽群房暂停增产。"
                    : $"Green-space renewal | a pigeon plaza at C{restingColumn + 1}R{restingRow + 1} rests on day {impact.SeedForecastDay}.");
            }
            if (impact.MarketDay > 0)
            {
                var marketRoom = RoomLayoutData.All.FirstOrDefault(item => item.Id == impact.MarketRoomId);
                var marketName = marketRoom != null
                    ? UrbanPalette.LocalizedRoomName(marketRoom, chinese)
                    : impact.MarketRoomId;
                var isMajorMarket = NeighborhoodMarketSchedule.ForDay(impact.MarketDay)?.IsMajorMarket ?? false;
                var eventName = isMajorMarket
                    ? chinese ? "集市" : "Market"
                    : chinese ? "每日轮值" : "Daily rotation";
                lines.Add(chinese
                    ? $"{eventName}｜第 {impact.MarketDay} 天·{marketName}（按满员）：超载/送达失败点 {impact.BeforeMarketOverflow}→{impact.AfterMarketOverflow} {Tag(marketDelta, false, true)}"
                    : $"{eventName} | day {impact.MarketDay}, {marketName}, {ResidentPopulationModel.MaximumResidents} residents: overflow/blocked sites {impact.BeforeMarketOverflow}→{impact.AfterMarketOverflow} {Tag(marketDelta, false, false)}");
            }
            lines.Add(chinese
                ? "与进入编辑时比较；庇护是布局预测，不保证觅食成功；不预测存量、动物的实际路线及车库风险。"
                : "Compared with edit start; cover is a layout forecast, not a guaranteed meal. Stock, actual animal routes and traffic are not predicted.");
            return string.Join("\n", lines);
        }

        private static List<string> ChangedRoomNames(IReadOnlyList<string> ids, bool chinese)
        {
            var names = new List<string>();
            if (ids == null)
            {
                return names;
            }
            foreach (var id in ids)
            {
                var room = RoomLayoutData.All.FirstOrDefault(item => item.Id == id);
                names.Add(room != null ? UrbanPalette.LocalizedRoomName(room, chinese) : id);
            }
            return names;
        }

        private static string Summary(bool chinese, int seed, int greenery, int workers,
            int waste, int market)
        {
            var gains = new List<string>();
            var losses = new List<string>();
            Add(seed, true, chinese ? "种子补给基准" : "seed target", gains, losses);
            if (seed == 0)
            {
                Add(greenery, true, chinese ? "连通绿地" : "linked greenery", gains, losses);
            }
            Add(workers, true, chinese ? "可完成循环的居民" : "residents completing the loop", gains, losses);
            Add(waste, false, chinese ? "垃圾异常" : "waste problems", gains, losses);
            Add(market, false, chinese ? "活动垃圾异常" : "event waste problems", gains, losses);

            if (gains.Count == 0 && losses.Count == 0)
            {
                return Colorize(chinese
                    ? "已测指标未变；这不代表动物的实际路线没有变化。"
                    : "Measured metrics unchanged; animal routes may still differ.", Neutral);
            }
            var gainText = string.Join(chinese ? "、" : ", ", gains.Take(2));
            var lossText = string.Join(chinese ? "、" : ", ", losses.Take(2));
            if (gains.Count > 0 && losses.Count > 0)
            {
                return Colorize(chinese ? $"有得有失：改善 {gainText}；变差 {lossText}"
                    : $"Trade-off: better {gainText}; worse {lossText}", Bad);
            }
            return gains.Count > 0
                ? Colorize(chinese ? $"主要改善：{gainText}" : $"Main gain: {gainText}", Good)
                : Colorize(chinese ? $"主要风险：{lossText}" : $"Main risk: {lossText}", Bad);
        }

        private static void Add(int delta, bool higherIsBetter, string label,
            List<string> gains, List<string> losses)
        {
            if (delta == 0) return;
            ((delta > 0) == higherIsBetter ? gains : losses).Add($"{label} {Signed(delta)}");
        }

        private static void Add(float delta, bool higherIsBetter, string label,
            List<string> gains, List<string> losses)
        {
            if (Math.Abs(delta) < 0.05f) return;
            ((delta > 0f) == higherIsBetter ? gains : losses).Add($"{label} {Signed(delta)}");
        }

        private static string Tag(int delta, bool higherIsBetter, bool chinese)
        {
            if (delta == 0) return chinese ? "(不变)" : "(unchanged)";
            var better = (delta > 0) == higherIsBetter;
            var label = chinese ? better ? "改善" : "变差" : better ? "better" : "worse";
            return Colorize($"({Signed(delta)}, {label})", better ? Good : Bad);
        }

        private static string Colorize(string text, string color) =>
            $"<color={color}>{text}</color>";

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
        private static string Signed(float value) => value > 0f ? $"+{value:0.#}" : $"{value:0.#}";
    }
}
