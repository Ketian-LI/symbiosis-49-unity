using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class LayoutImpactFeedbackTests
    {
        [Test]
        public void ActionComparisonUsesStockUpperBoundAndDoesNotPromiseFeedingOutcome()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(3, 0f), new ResidentCommutePreview(4, 0f),
                2, 2, 7, 7, 0, 0, 0, null, 0, 0,
                new[] { "pigeon-b", "shared-h" }, livingPigeons: 12,
                beforePigeonSeedMealCeiling: 5, afterPigeonSeedMealCeiling: 5,
                beforeSeedStockCeiling: 7, afterSeedStockCeiling: 9,
                currentSeedStock: 4);

            var chinese = LayoutImpactFeedback.ActionComparison(impact, 4, true, 1, true);
            Assert.That(chinese, Does.Contain("维持  种子 ≤7/12 · 人 3/4"));
            Assert.That(chinese, Does.Contain("挪房  种子 ≤9/12 · 人 4/4"));
            Assert.That(chinese, Does.Contain("可放 5 份 / 30 秒 · 效果取决于地点"));
            Assert.That(chinese, Does.Not.Contain("必定"));
            var metric = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.PigeonStockCeiling);
            Assert.That(metric.Detail, Does.Contain("只补到容量"));
            Assert.That(LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Any(item => item.Kind == LayoutImpactMetricKind.PigeonSeedMealCeiling), Is.False);
            Assert.That(LayoutImpactFeedback.ActionComparison(
                impact, 4, false, 4, true, actionAlreadyUsed: true),
                Does.Contain("今日已选择行动"));
        }

        [Test]
        public void MovingBufferShowsRiskTradeoffWithoutPromisingSafety()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "shared-g" },
                beforeBufferedGarages: 1, afterBufferedGarages: 0);

            var metric = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.BufferedGarages);
            Assert.That(metric.DeltaText, Is.EqualTo("-1"));
            Assert.That(metric.IsRisk, Is.True);
            Assert.That(metric.Detail, Does.Contain("仍有风险"));
        }

        [Test]
        public void UnchangedCommuteDoesNotClaimAnImmediateGameOver()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(2, 0f), new ResidentCommutePreview(2, 0f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "shared-k" });

            var workers = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.Workers);
            Assert.That(workers.IsRisk, Is.False);
            Assert.That(workers.Detail, Does.Contain("连续 3 天"));
        }

        [Test]
        public void VisualPreviewShowsBaselineAndDeltaAndPrioritisesRisks()
        {
            var impact = Preview(2, 3, 5, 14, 4, 3, 4f, 3f, 1, 2);

            var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, true);

            Assert.That(metrics[0].Kind, Is.EqualTo(LayoutImpactMetricKind.Workers));
            Assert.That(metrics[0].BeforeText, Is.EqualTo("4"));
            Assert.That(metrics[0].DeltaText, Is.EqualTo("-1"));
            var waste = metrics.Single(item => item.Kind == LayoutImpactMetricKind.Waste);
            Assert.That(waste.DeltaText, Is.EqualTo("+1"));
            Assert.That(waste.IsRisk, Is.True, "More waste failures are harmful despite a positive delta.");
            Assert.That(metrics.TakeWhile(item => item.IsRisk).Count(), Is.EqualTo(2));
            Assert.That(metrics.Any(item => item.Kind == LayoutImpactMetricKind.Seeds &&
                item.BeforeText == "5" && item.DeltaText == "+9"), Is.True);
        }

        [Test]
        public void SeedGainIsLabelledAsSupplyPotentialAgainstLivingPigeons()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                1, 2, 5, 9, 0, 0, 0, null, 0, 0,
                new[] { "pigeon-d" }, livingPigeons: 12);

            var seeds = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.Seeds);
            Assert.That(seeds.Label, Does.Contain("鸽 12"));
            Assert.That(seeds.Label, Does.Contain("仍偏少"));
            Assert.That(seeds.IsRisk, Is.True,
                "A positive seed delta must not hide a remaining supply warning.");
            Assert.That(seeds.Detail, Does.Contain("不是实际新增量或保证餐数"));
            Assert.That(seeds.Detail, Does.Contain("捕食"));
            Assert.That(seeds.After, Is.EqualTo(9));
        }

        [Test]
        public void ReachableSeedMealCeilingRemainsARiskEvenWhenTotalSeedsRise()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 11, 14, 0, 0, 0, null, 0, 0,
                new[] { "pigeon-b", "shared-h" }, livingPigeons: 12,
                beforePigeonSeedMealCeiling: 7, afterPigeonSeedMealCeiling: 8);

            var metric = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.PigeonSeedMealCeiling);
            Assert.That(metric.DeltaText, Is.EqualTo("+1"));
            Assert.That(metric.IsRisk, Is.True,
                "An improvement must not hide the remaining flock-level deficit.");
            Assert.That(metric.Label, Does.Contain("鸽 12"));
            Assert.That(metric.Detail, Does.Contain("理论上限"));
        }

        [Test]
        public void UnreachableNewSeedsLeadThePreviewEvenWhenProductionRises()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 7, 9, 0, 0, 0, null, 0, 0,
                new[] { "pigeon-b", "shared-h" }, livingPigeons: 12,
                beforePigeonSeedMealCeiling: 4, afterPigeonSeedMealCeiling: 3);

            var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, true);
            Assert.That(metrics[0].Kind, Is.EqualTo(LayoutImpactMetricKind.PigeonSeedMealCeiling));
            Assert.That(metrics[0].DeltaText, Is.EqualTo("-1"));
            Assert.That(metrics.Single(item => item.Kind == LayoutImpactMetricKind.Seeds).DeltaText,
                Is.EqualTo("+2"));
            Assert.That(LayoutImpactFeedback.RiskHeadline(metrics[0], impact, true),
                Does.Contain("可达 ≤3/12"));
            Assert.That(LayoutImpactFeedback.RiskHeadline(metrics[0], impact, false),
                Does.Contain("reach ≤3/12 birds"));
        }

        [Test]
        public void UnchangedPigeonRouteDeficitRemainsVisibleAsPersistentRisk()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                1, 1, 12, 12, 0, 0, 0, null, 0, 0,
                new[] { "office-a", "shared-g" }, livingPigeons: 12,
                beforeFoodAccess: new AnimalFoodAccessSnapshot(2, 2, 1, 0),
                afterFoodAccess: new AnimalFoodAccessSnapshot(2, 2, 1, 0));

            var routes = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.PigeonFoodAccess);
            Assert.That(routes.DeltaText, Is.EqualTo("0"));
            Assert.That(routes.IsRisk, Is.True);
            Assert.That(routes.Label, Does.Contain("仍不足"));
            Assert.That(routes.Detail, Does.Contain("不保证每只鸽子实际进食"));
        }

        [Test]
        public void HedgehogCoverAndGreenStabilityRemainVisibleWithoutAMetricDelta()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                1, 1, 12, 12, 0, 0, 0, null, 0, 0,
                new[] { "office-a", "shared-g" },
                beforeShelterPairs: 0, afterShelterPairs: 0,
                beforeGreenCells: 3, afterGreenCells: 3,
                livingHedgehogs: 2);

            var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, true);
            var cover = metrics.Single(item => item.Kind == LayoutImpactMetricKind.Shelter);
            var greenery = metrics.Single(item => item.Kind == LayoutImpactMetricKind.GreenCells);
            Assert.That(cover.IsRisk, Is.True);
            Assert.That(cover.Label, Does.Contain("无"));
            Assert.That(greenery.IsRisk, Is.True);
            Assert.That(greenery.Detail, Does.Contain("连通不等于庇护或进食"));
            Assert.That(metrics.TakeWhile(item => item.IsRisk), Does.Contain(cover));
            Assert.That(metrics.TakeWhile(item => item.IsRisk), Does.Contain(greenery));
        }

        [Test]
        public void NoLivingHedgehogsDoesNotRaiseAnEmptyShrubCoverWarning()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                1, 1, 12, 12, 0, 0, 0, null, 0, 0,
                new[] { "office-a", "shared-g" },
                beforeShelterPairs: 0, afterShelterPairs: 0,
                livingHedgehogs: 0);

            var cover = LayoutImpactFeedback.BuildVisualMetrics(impact, false)
                .Single(item => item.Kind == LayoutImpactMetricKind.Shelter);
            Assert.That(cover.IsRisk, Is.False);
        }

        [Test]
        public void UnchangedFoxReachIsStillShownAsPotentialPredationRisk()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 12, 12, 0, 0, 0, null, 0, 0,
                new[] { "shared-g" },
                beforeFoodAccess: new AnimalFoodAccessSnapshot(4, 2, 2, 1),
                afterFoodAccess: new AnimalFoodAccessSnapshot(4, 2, 2, 1));

            var fox = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.FoxPreyAccess);
            Assert.That(fox.Changed, Is.False);
            Assert.That(fox.IsRisk, Is.True);
            Assert.That(fox.Detail, Does.Contain("不代表一定发生"));
        }

        [Test]
        public void VisualShrubRecoveryHasItsOwnDelayedCard()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "shrub-a", "pigeon-d" }, 2,
                0, 0, 2, 1, 3);

            var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, true);

            var recovery = metrics.Single(item => item.Kind == LayoutImpactMetricKind.ShrubRecovery);
            Assert.That(metrics[0].Kind, Is.EqualTo(LayoutImpactMetricKind.Waste),
                "An unresolved waste failure remains ahead of delayed benefits.");
            Assert.That(recovery.Label, Does.Contain("第 3 天恢复"));
            Assert.That(recovery.DeltaText, Is.EqualTo("+2"));
            Assert.That(recovery.Delayed, Is.True);
        }

        [Test]
        public void AnimalPassageChangeIsVisibleWithoutCallingEveryNewLinkSafe()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "shrub-a" },
                beforeAnimalConnections: 24, afterAnimalConnections: 25);

            var metric = LayoutImpactFeedback.BuildVisualMetrics(impact, true)
                .Single(item => item.Kind == LayoutImpactMetricKind.AnimalPassages);

            Assert.That(metric.DeltaText, Is.EqualTo("+1"));
            Assert.That(metric.Neutral, Is.True);
            Assert.That(metric.IsRisk, Is.False);
            Assert.That(metric.Detail, Does.Contain("狐狸"));
            Assert.That(metric.Detail, Does.Contain("人走独立道路"));
        }

        [Test]
        public void SpeciesPreviewShowsFoodRoutesAndTreatsFoxReachAsRisk()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "pigeon-d" },
                beforeAnimalConnections: 24, afterAnimalConnections: 25,
                beforeFoodAccess: new AnimalFoodAccessSnapshot(2, 2, 1, 0),
                afterFoodAccess: new AnimalFoodAccessSnapshot(3, 2, 0, 1));

            var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, true);

            Assert.That(metrics[0].Kind, Is.EqualTo(LayoutImpactMetricKind.Waste));
            Assert.That(metrics[0].IsRisk, Is.True);
            Assert.That(metrics.ToList().FindIndex(item => item.Kind == LayoutImpactMetricKind.HedgehogFoodAccess),
                Is.LessThan(metrics.ToList().FindIndex(item => item.Kind == LayoutImpactMetricKind.PigeonFoodAccess)));
            Assert.That(metrics.Single(item => item.Kind == LayoutImpactMetricKind.PigeonFoodAccess)
                .DeltaText, Is.EqualTo("+1"));
            Assert.That(metrics.Single(item => item.Kind == LayoutImpactMetricKind.FoxPreyAccess)
                .IsRisk, Is.True);
            Assert.That(metrics.Any(item => item.Kind == LayoutImpactMetricKind.SquirrelFoodAccess), Is.False);
            Assert.That(metrics.Any(item => item.Kind == LayoutImpactMetricKind.AnimalPassages), Is.False,
                "A graph-wide count should not add another card when species impacts are available.");
        }

        [Test]
        public void ParkThresholdExplainsFoodGainAndNamesMovedRooms()
        {
            var impact = Preview(2, 3, 5, 14, 4, 4, 4f, 4f, 0, 0);

            var text = LayoutImpactFeedback.Build(impact, true, true, true);

            Assert.That(text, Does.Contain("若现在松手"));
            Assert.That(text, Does.Contain("鸽群 D"));
            Assert.That(text, Does.Contain("住宅 D"));
            Assert.That(text, Does.Contain("今日调整免费"));
            Assert.That(text, Does.Contain("次日种子 5→14"));
            Assert.That(text, Does.Contain("与进入编辑时比较"));
        }

        [Test]
        public void SeedLossIsShownWithoutAnObsoleteFourEdgeCrowdingRule()
        {
            var impact = Preview(3, 4, 16, 5, 4, 4, 4f, 4f, 0, 0);

            var text = LayoutImpactFeedback.Build(impact, true, true, true);

            Assert.That(text, Does.Not.Contain("四边拥挤"));
            Assert.That(text, Does.Contain("16→5"));
        }

        [Test]
        public void TradeOffCallsOutWorseCommuteAndBetterWaste()
        {
            var impact = Preview(2, 2, 5, 5, 4, 3, 3.5f, 2.5f, 2, 1);

            var text = LayoutImpactFeedback.Build(impact, true, false, true);

            Assert.That(text, Does.Contain("有得有失"));
            Assert.That(text, Does.Contain("可完成循环 4→3"));
            Assert.That(text, Does.Contain("变差"));
            Assert.That(text, Does.Contain("超载/送达失败点 2→1"));
            Assert.That(text, Does.Contain("改善"));
        }

        [Test]
        public void UnchangedMetricsDoNotClaimNoGameplayImpact()
        {
            var impact = Preview(2, 2, 5, 5, 4, 4, 3f, 3f, 1, 1);

            var text = LayoutImpactFeedback.Build(impact, true, false, false);

            Assert.That(text, Does.Contain("已测指标未变"));
            Assert.That(text, Does.Contain("动物的实际路线"));
            Assert.That(text, Does.Contain("超出今日免费调整范围"));
        }

        [Test]
        public void EnglishForecastStaysConcreteAndMarksMarketChange()
        {
            var impact = new RoomLayoutImpactPreview(4,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1,
                7, "canteen-a", 1, 0,
                new[] { "residence-g", "trash-a" });

            var text = LayoutImpactFeedback.Build(impact, false, true, true);

            Assert.That(text, Does.Contain("If released now"));
            Assert.That(text, Does.Contain("Residence G"));
            Assert.That(text, Does.Contain("Market | day 7, Food Shop A, 8 residents: overflow/blocked sites 1→0"));
            Assert.That(text, Does.Contain("animal routes"));
        }

        [Test]
        public void DailyRotationStaysVisibleEvenWhenAProposedSwapDoesNotChangeWasteRisk()
        {
            var impact = new RoomLayoutImpactPreview(4,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1,
                4, "canteen-a", 1, 1,
                new[] { "residence-g", "trash-a" });

            var text = LayoutImpactFeedback.Build(impact, true, true, true);

            Assert.That(text, Does.Contain("每日轮值｜第 4 天·餐饮商铺 A"));
            Assert.That(text, Does.Contain("超载/送达失败点 1→1"));
        }

        [Test]
        public void NextDawnRestCellAndFreeAllowanceAreVisibleInPreview()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 3, 5, 14, 1, 1, 4, "canteen-a", 1, 1,
                new[] { "pigeon-b", "office-a" }, 4);

            var text = LayoutImpactFeedback.Build(impact, true, false, true);

            Assert.That(text, Does.Contain("今日调整免费"));
            Assert.That(text, Does.Contain("第 4 天 C2R3"));
            Assert.That(text, Does.Contain("次日种子 5→14"));
        }

        [Test]
        public void ShrubPreviewSeparatesTonightFromRecovery()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 3f), new ResidentCommutePreview(4, 3f),
                2, 2, 5, 5, 1, 1, 0, null, 0, 0,
                new[] { "shrub-a", "pigeon-d" }, 2,
                0, 0, 2, 1, 3);

            var text = LayoutImpactFeedback.Build(impact, true, false, true);

            Assert.That(text, Does.Contain("今夜相邻灌木 0→0 组"));
            Assert.That(text, Does.Contain("第 3 天恢复后预计 2 组"));
            Assert.That(text, Does.Contain("不保证觅食成功"));
        }

        [Test]
        public void FocusPreviewHidesSafeUnchangedValuesButKeepsPersistentRisks()
        {
            var all = new[]
            {
                new LayoutImpactMetric(LayoutImpactMetricKind.GreenCells, "连通绿地", "", 5, 5, true),
                new LayoutImpactMetric(LayoutImpactMetricKind.Waste, "垃圾异常", "", 0, 0, false),
                new LayoutImpactMetric(LayoutImpactMetricKind.Seeds, "新增种子", "", 4, 6, true),
                new LayoutImpactMetric(LayoutImpactMetricKind.Shelter, "今夜灌木庇护", "", 0, 0,
                    true, persistentRisk: true),
                new LayoutImpactMetric(LayoutImpactMetricKind.ShrubRecovery, "稍后恢复", "", 0, 2,
                    true, delayed: true)
            };

            var focused = LayoutImpactFeedback.FocusMetrics(all);

            Assert.That(focused.Select(metric => metric.Kind), Is.EqualTo(new[]
            {
                LayoutImpactMetricKind.Seeds,
                LayoutImpactMetricKind.Shelter,
                LayoutImpactMetricKind.ShrubRecovery
            }));
            Assert.That(all.Length - focused.Count, Is.EqualTo(2));
        }

        [Test]
        public void DailyDecisionCardsExcludeSecondaryWasteAndSurplusChanges()
        {
            var impact = new RoomLayoutImpactPreview(0,
                new ResidentCommutePreview(4, 0), new ResidentCommutePreview(3, 0),
                2, 3, 12, 13, 0, 1, 0, null, 0, 1, null,
                beforeGreenCells: 4, afterGreenCells: 5, livingPigeons: 12,
                beforePigeonSeedMealCeiling: 12, afterPigeonSeedMealCeiling: 12);
            var cards = LayoutImpactFeedback.DecisionMetrics(impact,
                LayoutImpactFeedback.BuildVisualMetrics(impact, true))
                .Where(metric => metric.Changed).ToArray();

            Assert.That(cards.Select(metric => metric.Kind),
                Is.EqualTo(new[] { LayoutImpactMetricKind.Workers }));
            Assert.That(DailySpatialDecision.HasMaterialImpact(impact), Is.True);
        }

        [Test]
        public void PreviewCardUsesOneComparisonAndPlainLanguageChange()
        {
            var greenery = new LayoutImpactMetric(LayoutImpactMetricKind.GreenCells,
                "连通绿地", "", 3, 5, true);
            var waste = new LayoutImpactMetric(LayoutImpactMetricKind.Waste,
                "垃圾异常", "", 2, 1, false);
            var fox = new LayoutImpactMetric(LayoutImpactMetricKind.FoxPreyAccess,
                "狐狸可达猎物", "", 0, 1, false);
            var unresolved = new LayoutImpactMetric(LayoutImpactMetricKind.Shelter,
                "今夜灌木庇护", "", 0, 0, true, persistentRisk: true);

            Assert.That(LayoutImpactFeedback.BaselineLabel(greenery, true), Is.EqualTo("3 → 5"));
            Assert.That(LayoutImpactFeedback.ChangeLabel(greenery, true), Is.EqualTo("改善 +2格"));
            Assert.That(LayoutImpactFeedback.ChangeLabel(waste, true), Is.EqualTo("改善 -1处"));
            Assert.That(LayoutImpactFeedback.BaselineLabel(fox, true), Is.EqualTo("否 → 是"));
            Assert.That(LayoutImpactFeedback.ChangeLabel(fox, false), Is.EqualTo("Predation risk up"));
            Assert.That(LayoutImpactFeedback.BaselineLabel(unresolved, true), Is.EqualTo("现 0"));
            Assert.That(LayoutImpactFeedback.ChangeLabel(unresolved, true), Is.EqualTo("仍需处理"));
        }

        private static RoomLayoutImpactPreview Preview(int beforePark, int afterPark,
            int beforeSeeds, int afterSeeds, int beforeWorkers, int afterWorkers,
            float beforeProduction, float afterProduction, int beforeWaste, int afterWaste)
        {
            return new RoomLayoutImpactPreview(4,
                new ResidentCommutePreview(beforeWorkers, beforeProduction),
                new ResidentCommutePreview(afterWorkers, afterProduction),
                beforePark, afterPark, beforeSeeds, afterSeeds,
                beforeWaste, afterWaste, 0, null, 0, 0,
                new[] { "pigeon-d", "residence-d" });
        }
    }
}
