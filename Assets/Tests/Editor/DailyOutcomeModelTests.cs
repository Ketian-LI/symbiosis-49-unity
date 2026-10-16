using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class DailyOutcomeModelTests
    {
        [Test]
        public void ReportKeepsMovementAndObservedOutcomesSeparateByDay()
        {
            var model = new DailyOutcomeModel();
            model.RecordMovement(1, 2);
            model.CompleteDay(new ResourceSettlement(1, 8f, 4f, 2, 1, 9f, 0f),
                3, 1, 1, 0, 2,
                meals: new AnimalMealDaySummary(1, 14, 19, 8, 12, 2));
            var first = model.LastReport;

            Assert.That(first.Day, Is.EqualTo(1));
            Assert.That(first.MovedRooms, Is.EqualTo(2));
            Assert.That(first.WorkingResidents, Is.EqualTo(3));
            Assert.That(first.WorkingResidentsKnown, Is.True);
            Assert.That(first.Production, Is.EqualTo(4f));
            Assert.That(first.Spending, Is.EqualTo(3));
            Assert.That(first.Deaths, Is.EqualTo(1));
            Assert.That(first.StarvationDeaths, Is.EqualTo(1));
            Assert.That(first.WasteIssues, Is.EqualTo(2));
            Assert.That(first.MealsKnown, Is.True);
            Assert.That(first.FedAnimals, Is.EqualTo(14));
            Assert.That(first.LivingAnimals, Is.EqualTo(19));
            Assert.That(first.FedPigeons, Is.EqualTo(8));
            Assert.That(first.LivingPigeons, Is.EqualTo(12));
            Assert.That(first.SeedPortionsLeft, Is.EqualTo(2));

            var restored = new DailyOutcomeModel();
            restored.Restore(model.Export(), 1, 1, 0);
            Assert.That(restored.LastReport.FedPigeons, Is.EqualTo(8),
                "A saved report must keep observed meals, not replace them with tomorrow's stock.");

            model.CompleteDay(new ResourceSettlement(2, 9f, 3f, 2, 1, 9f, 0f),
                4, 2, 1, 1, 0);
            var second = model.LastReport;
            Assert.That(second.MovedRooms, Is.Zero);
            Assert.That(second.Deaths, Is.EqualTo(1));
            Assert.That(second.StarvationDeaths, Is.Zero);
            Assert.That(second.TrafficDeaths, Is.EqualTo(1));

            model.CompleteDay(new ResourceSettlement(3, 9f, 3f, 2, 1, 9f, 0f),
                4, 3, 1, 1, 0, predationDeaths: 1);
            var third = model.LastReport;
            Assert.That(third.PredationDeaths, Is.EqualTo(1));
            Assert.That(third.OtherDeaths, Is.Zero,
                "Fox predation should not be hidden in the other-deaths bucket.");
        }

        [Test]
        public void SaveRoundTripAndLegacyRestoreDoNotDoubleCountPriorDeaths()
        {
            var before = new DailyOutcomeModel();
            before.Reset(4, 2, 1);
            before.RecordMovement(3, 1);
            var saved = before.Export();
            var after = new DailyOutcomeModel();
            after.Restore(saved, 4, 2, 1);
            after.CompleteDay(new ResourceSettlement(3, 8f, 2f, 1, 1, 8f, 0f),
                3, 5, 2, 2, 1);
            Assert.That(after.LastReport.MovedRooms, Is.EqualTo(1));
            Assert.That(after.LastReport.Deaths, Is.EqualTo(1));

            var legacy = new DailyOutcomeModel();
            legacy.Restore(null, 4, 2, 1);
            legacy.CompleteDay(new ResourceSettlement(3, 8f, 2f, 1, 1, 8f, 0f),
                3, 5, 2, 2, 1);
            Assert.That(legacy.LastReport.Deaths, Is.EqualTo(1));

            var oldSave = new UrbanWildlifeRooms.Data.DailyOutcomeSaveData
            {
                lastDay = 2,
                lastProduction = 2f
            };
            legacy.Restore(oldSave, 4, 2, 1);
            Assert.That(legacy.LastReport.WorkingResidentsKnown, Is.False,
                "An older save's production cannot be presented as an exact worker count.");
            Assert.That(legacy.LastReport.MealsKnown, Is.False,
                "An older save must not invent a meal count.");
            legacy.Restore(oldSave, 4, 2, 1, predationDeaths: 1);
            legacy.CompleteDay(new ResourceSettlement(3, 8f, 2f, 1, 1, 8f, 0f),
                3, 4, 2, 1, 0, predationDeaths: 1);
            Assert.That(legacy.LastReport.PredationDeaths, Is.Zero,
                "Restoring an older save must not attribute an earlier fox kill to the new day.");
        }

        [Test]
        public void MovementForecastUsesCorrectDayAndSurvivesSaveRestore()
        {
            var model = new DailyOutcomeModel();
            model.RecordMovement(1, 2, plannedWorkers: 3,
                pigeonSeedCeiling: 8, pigeonForecastBirds: 12);
            var restored = new DailyOutcomeModel();
            restored.Restore(model.Export(), 0, 0, 0);
            restored.CompleteDay(new ResourceSettlement(1, 0f, 0f, 0, 0, 0f, 0f),
                3, 0, 0, 0, 0);
            Assert.That(restored.LastReport.PlannedWorkersKnown, Is.True);
            Assert.That(restored.LastReport.PlannedWorkers, Is.EqualTo(3));
            Assert.That(restored.LastReport.PigeonSeedForecastKnown, Is.False,
                "Tomorrow's seed-route forecast must not be compared with today's meals.");

            restored.CompleteDay(new ResourceSettlement(2, 0f, 0f, 0, 0, 0f, 0f),
                4, 0, 0, 0, 0,
                meals: new AnimalMealDaySummary(2, 14, 19, 9, 12, 1));
            Assert.That(restored.LastReport.PlannedWorkersKnown, Is.False);
            Assert.That(restored.LastReport.PigeonSeedForecastKnown, Is.True);
            Assert.That(restored.LastReport.PigeonSeedCeiling, Is.EqualTo(8));
            Assert.That(UrbanWildlifeHud.DailyOutcomeSummary(restored.LastReport, true),
                Does.Contain("实际进食 14/19"));
            Assert.That(UrbanWildlifeHud.DailyOutcomeSummary(restored.LastReport, true),
                Does.Contain("补给目标容量可达 ≤8/12"));
        }

        [Test]
        public void NewRearrangementSupersedesEarlierNextDaySeedForecast()
        {
            var model = new DailyOutcomeModel();
            model.RecordMovement(1, 2, 3, 8, 12);
            model.CompleteDay(new ResourceSettlement(1, 0f, 0f, 0, 0, 0f, 0f),
                3, 0, 0, 0, 0);
            model.RecordMovement(2, 2, 4, 10, 12);
            model.CompleteDay(new ResourceSettlement(2, 0f, 0f, 0, 0, 0f, 0f),
                4, 0, 0, 0, 0);
            Assert.That(model.LastReport.PigeonSeedForecastKnown, Is.False);
            model.CompleteDay(new ResourceSettlement(3, 0f, 0f, 0, 0, 0f, 0f),
                4, 0, 0, 0, 0);
            Assert.That(model.LastReport.PigeonSeedForecastKnown, Is.True);
            Assert.That(model.LastReport.PigeonSeedCeiling, Is.EqualTo(10));
        }

        [Test]
        public void RecordedDeathCauseTakesPriorityOverAnUnfedFlockHypothesis()
        {
            var report = new DailyOutcomeReport(new DailyOutcomeSaveData
            {
                lastDay = 2,
                lastDeaths = 1,
                lastPredationDeaths = 1,
                lastMealsKnown = true,
                lastFedPigeons = 8,
                lastLivingPigeons = 12,
                lastSeedPortionsLeft = 3
            });

            Assert.That(UrbanWildlifeHud.DailyOutcomeHint(report, true), Does.Contain("狐狸捕食"));
            Assert.That(UrbanWildlifeHud.DailyOutcomeHint(report, false), Does.Contain("Fox predation"));
            Assert.That(UrbanWildlifeHud.DailyOutcomeHint(report, true), Does.Not.Contain("仍有种子"));

            var noDeath = new DailyOutcomeReport(new DailyOutcomeSaveData
            {
                lastMealsKnown = true,
                lastFedPigeons = 8,
                lastLivingPigeons = 12,
                lastSeedPortionsLeft = 3
            });
            Assert.That(UrbanWildlifeHud.DailyOutcomeHint(noDeath, true), Does.Contain("仍有种子"));
        }
    }
}
