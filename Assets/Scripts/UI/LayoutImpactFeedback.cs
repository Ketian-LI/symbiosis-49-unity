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
        BufferedGarages,
        PigeonStockCeiling
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

        public static string ActionComparison(RoomLayoutImpactPreview impact,
            int residentCount, bool feedAvailable, int nextFeedDay, bool chinese,
            bool actionAlreadyUsed = false)
        {
            var living = Math.Max(0, impact.LivingPigeons);
            var before = impact.BeforeSeedStockCeiling >= 0
                ? impact.BeforeSeedStockCeiling : impact.BeforePigeonSeedMealCeiling;
            var after = impact.AfterSeedStockCeiling >= 0
                ? impact.AfterSeedStockCeiling : impact.AfterPigeonSeedMealCeiling;
            var keep = chinese ? "维持" : "Keep";
            var move = chinese ? "挪房" : "Move";
            var feed = chinese ? "投喂" : "Feed";
            var birdBefore = before < 0 ? "?" : $"≤{before}/{living}";
            var birdAfter = after < 0 ? "?" : $"≤{after}/{living}";
            var peopleBefore = $"{impact.Before.WorkingResidents}/{residentCount}";
            var peopleAfter = $"{impact.After.WorkingResidents}/{residentCount}";
            var unchangedMove = impact.ChangedRoomIds == null || impact.ChangedRoomIds.Count == 0;
            var moveResult = unchangedMove
                ? chinese ? "拖动房间查看" : "Drag a room to preview"
                : chinese ? $"种子 {birdAfter} · 人 {peopleAfter}" : $"Seeds {birdAfter} · People {peopleAfter}";
            var feedResult = actionAlreadyUsed
                ? chinese ? "今日已选择行动" : "Today's action already chosen"
                : feedAvailable
                    ? chinese
                        ? $"可放 {PlayerFoodSourceModel.PortionsPerSource} 份 / {PlayerFoodSourceModel.LifetimeSeconds:0} 秒 · 效果取决于地点"
                        : $"Ready · {PlayerFoodSourceModel.PortionsPerSource} portions / {PlayerFoodSourceModel.LifetimeSeconds:0}s · site matters"
                    : chinese ? $"冷却中 · 第 {nextFeedDay} 天可用" : $"Cooldown · available day {nextFeedDay}";
            return chinese
                ? $"{keep}  种子 {birdBefore} · 人 {peopleBefore}\n{move}  {moveResult}\n{feed}  {feedResult}"
                : $"{keep}  Seeds {birdBefore} · People {peopleBefore}\n{move}  {moveResult}\n{feed}  {feedResult}";
        }

        // Keep the full forecast for reasoning and tests. The HUD shows only
        // changed values in its comparison list; unchanged risks remain in its heading.
        public static IReadOnlyList<LayoutImpactMetric> BuildVisualMetrics(
            RoomLayoutImpactPreview impact, bool chinese)
        {
            var seedsBelowBirds = impact.AfterSeedStockCeiling < 0 &&
                                  impact.LivingPigeons > 0 &&
                                  impact.AfterSeedCapacity < impact.LivingPigeons;
            var metrics = new List<LayoutImpactMetric>
            {
                new(LayoutImpactMetricKind.Seeds,
                    impact.LivingPigeons >= 0
                        ? chinese ? $"次日种子容量 / 鸽 {impact.LivingPigeons}" +
                                    (seedsBelowBirds ? " · 仍偏少" : string.Empty)
                            : $"Dawn seed target / {impact.LivingPigeons} birds" +
                              (seedsBelowBirds ? " · LOW" : string.Empty)
                        : chinese ? "次日种子基准" : "Dawn seed target",
                    chinese
                        ? "这是明晨补给的目标容量，不是实际新增量或保证餐数；现有存量、路线、到达时间和捕食仍会影响结果。"
                        : "This is dawn target capacity, not newly added portions or guaranteed meals; stock, travel and predation still matter.",
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
                    impact.BeforeFullPopulationWasteOverflow, impact.AfterFullPopulationWasteOverflow, false,
                    persistentRisk: impact.AfterFullPopulationWasteOverflow > 0),
                new(LayoutImpactMetricKind.Shelter,
                    impact.LivingHedgehogs > 0 && impact.AfterShelterPairs == 0
                        ? chinese ? "今夜灌木庇护 · 无" : "Night shrub cover · NONE"
                        : chinese ? "今夜灌木庇护" : "Night shrub cover",
                    chinese ? "今晚可用的相邻灌木组；不保证动物觅食成功。" : "Connected shrub pairs tonight; not a guaranteed meal.",
                    impact.BeforeShelterPairs, impact.AfterShelterPairs, true,
                    persistentRisk: impact.LivingHedgehogs > 0 && impact.AfterShelterPairs == 0)
            };
            if (impact.LivingPigeons >= 0 &&
                impact.BeforeSeedStockCeiling >= 0 && impact.AfterSeedStockCeiling >= 0)
            {
                var ceilingLow = impact.AfterSeedStockCeiling < impact.LivingPigeons;
                if (ceilingLow || impact.BeforeSeedStockCeiling != impact.AfterSeedStockCeiling)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.PigeonStockCeiling,
                        chinese ? $"常规路线种子上限 / 鸽 {impact.LivingPigeons}" +
                                  (ceilingLow ? " · 不足" : string.Empty)
                            : $"Normal-route seed cap / {impact.LivingPigeons} birds" +
                              (ceilingLow ? " · LOW" : string.Empty),
                        chinese ? "常规动物门下的种子匹配上限。旧存量结转，明晨只补到容量；极饿动物可借人行口，今日余下消耗及实际行走也未计入。"
                            : "Seed matching cap on normal animal doors. Stock carries over and dawn tops up; starving birds may use pedestrian doors. Later consumption and travel are excluded.",
                        impact.BeforeSeedStockCeiling, impact.AfterSeedStockCeiling, true,
                        persistentRisk: ceilingLow));
            }
            else if (impact.LivingPigeons >= 0 &&
                impact.BeforePigeonSeedMealCeiling >= 0 &&
                impact.AfterPigeonSeedMealCeiling >= 0)
            {
                var ceilingLow = impact.AfterPigeonSeedMealCeiling < impact.LivingPigeons;
                if (ceilingLow || impact.BeforePigeonSeedMealCeiling != impact.AfterPigeonSeedMealCeiling)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.PigeonSeedMealCeiling,
                        chinese ? $"目标容量可达上限 / 鸽 {impact.LivingPigeons}" +
                                  (ceilingLow ? " · 不足" : string.Empty)
                            : $"Dawn-target reach ceiling / {impact.LivingPigeons} birds" +
                              (ceilingLow ? " · LOW" : string.Empty),
                        chinese ? "按各鸽群位置、动物通道和明晨种子目标容量计算的理论上限；不含旧存量、其他食物和实际行走。"
                            : "Theoretical ceiling from flock homes, wildlife routes and dawn target capacity; excludes stored seed, other food and travel.",
                        impact.BeforePigeonSeedMealCeiling,
                        impact.AfterPigeonSeedMealCeiling, true,
                        persistentRisk: ceilingLow));
            }
            if (impact.BeforeGreenCells >= 0 && impact.AfterGreenCells >= 0)
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.GreenCells,
                    impact.AfterGreenCells < GreenNetworkModel.StableCellCount
                        ? chinese ? "连通绿地 · 不足" : "Linked greenery · LOW"
                        : chinese ? "连通绿地" : "Linked greenery",
                    chinese ? "公园沿动物门可达的绿地；4 格才稳定。连通不等于庇护或进食。"
                        : "Green cells reached from the park; 4 make a stable network. A link is not cover or a meal.",
                    impact.BeforeGreenCells, impact.AfterGreenCells, true,
                    persistentRisk: impact.AfterGreenCells < GreenNetworkModel.StableCellCount));
            if (impact.MarketDay > 0)
            {
                metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.MarketWaste,
                    chinese ? "轮值垃圾" : "Rotation waste",
                    chinese ? $"第 {impact.MarketDay} 天轮值时的垃圾异常点；越少越好。"
                        : $"Waste failures during the day {impact.MarketDay} rotation; lower is better.",
                    impact.BeforeMarketOverflow, impact.AfterMarketOverflow, false,
                    persistentRisk: impact.AfterMarketOverflow > 0));
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
                    chinese ? "可通往当前有种子或明晨能补种子的地点的鸽群房数；路线可达仍不保证每只鸽子实际进食。"
                        : "Pigeon rooms reaching stocked or dawn-replenished seed; a route still does not guarantee each bird eats.",
                    beforeAccess.Pigeon, afterAccess.Pigeon, true,
                    persistentRisk: pigeonRoutesLow));
                if (beforeAccess.Squirrel != afterAccess.Squirrel)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.SquirrelFoodAccess,
                    chinese ? "松鼠食路" : "Squirrel food routes",
                    chinese ? "可通往存量或明晨坚果来源的栖地数；包括松鼠林地，搬树会使树倒下。"
                        : "Habitats reaching stored or dawn nuts, including the squirrel grove; moved trees are felled.",
                    beforeAccess.Squirrel, afterAccess.Squirrel, true));
                if (beforeAccess.Hedgehog != afterAccess.Hedgehog)
                    metrics.Add(new LayoutImpactMetric(LayoutImpactMetricKind.HedgehogFoodAccess,
                    chinese ? "刺猬食路" : "Hedgehog food routes",
                    chinese ? "可通往存量、公园或连通刺猬花园昆虫点的栖地数；不计明日随机昆虫。"
                        : "Habitats reaching stored insects, the park or linked garden; tomorrow's random insects excluded.",
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
                    impact.BeforeRecoveredShelterPairs >= 0
                        ? impact.BeforeRecoveredShelterPairs : impact.BeforeShelterPairs,
                    impact.RecoveredShelterPairs, true, true));
            }
            // A larger seed harvest can still leave most birds unable to reach
            // it. Put the flock-level deficit ahead of the production total.
            return metrics.OrderBy(metric => metric.IsRisk ? 0 : metric.Delayed ? 1 : metric.Changed ? 2 : 3)
                .ThenBy(metric => metric.IsRisk ? metric.Kind switch
                {
                    LayoutImpactMetricKind.Workers => -2,
                    LayoutImpactMetricKind.PigeonSeedMealCeiling or
                    LayoutImpactMetricKind.PigeonStockCeiling => -1,
                    _ => (int)metric.Kind
                } : (int)metric.Kind).ToArray();
        }

        public static IReadOnlyList<LayoutImpactMetric> FocusMetrics(
            IReadOnlyList<LayoutImpactMetric> allMetrics)
        {
            return allMetrics.Where(metric => metric.Changed || metric.IsRisk || metric.Delayed)
                .ToArray();
        }

        // The main comparison shows only changes that can alter a rule the
        // player is trying to satisfy. Secondary forecasts remain available
        // to systems/tests, but cannot masquerade as today's required action.
        public static IReadOnlyList<LayoutImpactMetric> DecisionMetrics(
            RoomLayoutImpactPreview impact, IReadOnlyList<LayoutImpactMetric> allMetrics)
        {
            return allMetrics.Where(metric => metric.Kind switch
            {
                LayoutImpactMetricKind.Workers or
                LayoutImpactMetricKind.PigeonSeedMealCeiling or
                LayoutImpactMetricKind.PigeonStockCeiling or
                LayoutImpactMetricKind.PigeonFoodAccess or
                LayoutImpactMetricKind.SquirrelFoodAccess or
                LayoutImpactMetricKind.HedgehogFoodAccess or
                LayoutImpactMetricKind.FoxPreyAccess => true,
                LayoutImpactMetricKind.Seeds => DailySpatialDecision.SeedShortfallChanged(impact),
                LayoutImpactMetricKind.GreenCells =>
                    DailySpatialDecision.GreenDeficitChanged(impact) || metric.IsRisk,
                LayoutImpactMetricKind.Shelter => impact.LivingHedgehogs > 0,
                LayoutImpactMetricKind.ShrubRecovery =>
                    DailySpatialDecision.FutureShelterChanged(impact),
                _ => false
            }).Where(metric => !metric.Changed || metric.Kind switch
            {
                LayoutImpactMetricKind.GreenCells =>
                    DailySpatialDecision.GreenDeficitChanged(impact),
                LayoutImpactMetricKind.Seeds =>
                    DailySpatialDecision.SeedShortfallChanged(impact),
                _ => true
            }).ToArray();
        }

        public static string BaselineLabel(LayoutImpactMetric metric, bool chinese)
        {
            if (metric.Kind == LayoutImpactMetricKind.FoxPreyAccess)
            {
                var before = metric.Before > 0f ? chinese ? "是" : "Yes" : chinese ? "否" : "No";
                var after = metric.After > 0f ? chinese ? "是" : "Yes" : chinese ? "否" : "No";
                return metric.Changed ? $"{before} → {after}" :
                    chinese ? $"现 {after}" : $"Now {after}";
            }
            return metric.Changed ? $"{metric.BeforeText} → {metric.After:0.#}" :
                chinese ? $"现 {metric.After:0.#}" : $"Now {metric.After:0.#}";
        }

        public static string ChangeLabel(LayoutImpactMetric metric, bool chinese)
        {
            if (!metric.Changed)
                return metric.IsRisk
                    ? chinese ? "仍需处理" : "Still at risk"
                    : chinese ? "保持" : "Stable";
            if (metric.Kind == LayoutImpactMetricKind.FoxPreyAccess)
                return metric.After > metric.Before
                    ? chinese ? "捕食风险增加" : "Predation risk up"
                    : chinese ? "猎物通道阻断" : "Prey route blocked";

            var unit = MetricUnit(metric.Kind, chinese);
            var amount = chinese ? $"{metric.DeltaText}{unit}" :
                string.IsNullOrEmpty(unit) ? metric.DeltaText : $"{metric.DeltaText} {unit}";
            if (metric.Delayed)
                return chinese ? $"恢复后 {amount}" : $"After recovery {amount}";
            if (metric.Neutral)
                return chinese ? $"路线变化 {amount}" : $"Route change {amount}";
            var improves = (metric.Delta > 0f) == metric.HigherIsBetter;
            return chinese
                ? $"{(improves ? "改善" : "恶化")} {amount}"
                : $"{(improves ? "Better" : "Worse")} {amount}";
        }

        private static string MetricUnit(LayoutImpactMetricKind kind, bool chinese)
        {
            return kind switch
            {
                LayoutImpactMetricKind.Seeds => chinese ? "份" : "portions",
                LayoutImpactMetricKind.PigeonSeedMealCeiling or
                LayoutImpactMetricKind.PigeonStockCeiling => chinese ? "只" : "birds",
                LayoutImpactMetricKind.ParkLinks or LayoutImpactMetricKind.PigeonFoodAccess =>
                    chinese ? "间" : "rooms",
                LayoutImpactMetricKind.GreenCells => chinese ? "格" : "cells",
                LayoutImpactMetricKind.Workers => chinese ? "人" : "people",
                LayoutImpactMetricKind.Waste or LayoutImpactMetricKind.MarketWaste =>
                    chinese ? "处" : "sites",
                LayoutImpactMetricKind.Shelter or LayoutImpactMetricKind.ShrubRecovery =>
                    chinese ? "组" : "pairs",
                LayoutImpactMetricKind.AnimalPassages => chinese ? "处" : "links",
                LayoutImpactMetricKind.SquirrelFoodAccess or LayoutImpactMetricKind.HedgehogFoodAccess =>
                    chinese ? "处" : "habitats",
                LayoutImpactMetricKind.BufferedGarages => chinese ? "间" : "garages",
                _ => string.Empty
            };
        }

        public static string RiskHeadline(LayoutImpactMetric metric,
            RoomLayoutImpactPreview impact, bool chinese)
        {
            if (metric.Kind == LayoutImpactMetricKind.PigeonSeedMealCeiling &&
                impact.LivingPigeons > 0 && impact.AfterPigeonSeedMealCeiling >= 0)
                return chinese
                    ? $"鸽群目标容量可达 ≤{impact.AfterPigeonSeedMealCeiling}/{impact.LivingPigeons}"
                    : $"Dawn-target reach ≤{impact.AfterPigeonSeedMealCeiling}/{impact.LivingPigeons} birds";
            if (metric.Kind == LayoutImpactMetricKind.PigeonStockCeiling &&
                impact.LivingPigeons > 0 && impact.AfterSeedStockCeiling >= 0)
                return chinese
                    ? $"常规种子可达 ≤{impact.AfterSeedStockCeiling}/{impact.LivingPigeons} 鸽"
                    : $"Normal-route seeds ≤{impact.AfterSeedStockCeiling}/{impact.LivingPigeons} birds";
            return metric.Label;
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
            lines.Add(impact.CurrentSeedStock >= 0
                ? chinese
                    ? "与进入编辑时比较；已计当前种子存量，但不保证觅食成功；今日余下消耗、实际行走及车库风险未预测。"
                    : "Compared with edit start; current seed stock is included, not guaranteed meals. Later consumption, travel and traffic remain unpredicted."
                : chinese
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
