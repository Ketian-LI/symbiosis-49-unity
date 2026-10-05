using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class SpatialFoodStrategyFlowTests
    {
        [UnityTest]
        public IEnumerator TargetedDailyMovesCanSustainTheOpeningTwelveDays()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);

            for (var day = 1; day <= 12; day++)
            {
                Assert.That(runtime.HasActiveRun, Is.True, $"The run ended before day {day}.");
                if (day == 1) Swap(bootstrap, editor, rooms, "residence-d", "pigeon-d");
                else if (day == 2) Swap(bootstrap, editor, rooms, "pigeon-a", "shared-j");
                else Swap(bootstrap, editor, rooms, "pigeon-b", "shared-h");
                yield return SkipDay(runtime, day);
                TestContext.WriteLine($"Targeted layout day {day}: pigeon meals " +
                    $"{needs.LastCompletedDayMeals.PigeonsFed}/{needs.LastCompletedDayMeals.PigeonsLiving}, " +
                    $"seeds left {needs.LastCompletedDayMeals.SeedPortionsLeft}, " +
                    $"workers {residents.LastReport.WorkingResidents}, " +
                    $"deaths {mortality.Model.TotalDeaths}, active {runtime.HasActiveRun}.");
                Assert.That(residents.LastReport.WorkingResidents, Is.GreaterThanOrEqualTo(3));
                Assert.That(mortality.Model.TotalDeaths, Is.Zero);
            }
            Assert.That(runtime.HasActiveRun, Is.True);
        }

        [UnityTest]
        public IEnumerator StrongOpeningAndAlternatingCorridorReportsObservedDays()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);

            var completedDays = 0;
            for (var day = 1; day <= 10 && runtime.HasActiveRun; day++)
            {
                if (day == 1)
                    Swap(bootstrap, editor, rooms, "residence-d", "pigeon-d");
                else
                    Swap(bootstrap, editor, rooms, "pigeon-b", "shared-h");
                Assert.That(editor.LastConfirmedMovementDay, Is.EqualTo(day));
                yield return SkipDay(runtime, day);
                completedDays++;
                TestContext.WriteLine($"Strong opening day {day}: " +
                    $"pigeon meals {needs.LastCompletedDayMeals.PigeonsFed}/" +
                    $"{needs.LastCompletedDayMeals.PigeonsLiving}, " +
                    $"seeds left {needs.LastCompletedDayMeals.SeedPortionsLeft}, " +
                    $"workers {residents.LastReport.WorkingResidents}, " +
                    $"deaths {mortality.Model.TotalDeaths}, active {runtime.HasActiveRun}.");
            }
            TestContext.WriteLine(runtime.HasActiveRun
                ? $"Strong opening survived at least {completedDays} days."
                : $"Strong opening ended after day {runtime.CurrentResults.daysSurvived} " +
                  $"({runtime.CurrentResults.endReason}).");
            Assert.That(completedDays, Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator AlternatingOneCorridorUsesRealDailyMovesAndRealDeathLimit()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);

            var completedDays = 0;
            for (var day = 1; day <= 8 && runtime.HasActiveRun; day++)
            {
                Swap(bootstrap, editor, rooms, "pigeon-c", "shared-f");
                Assert.That(editor.LastConfirmedMovementDay, Is.EqualTo(day));
                yield return SkipDay(runtime, day);
                completedDays++;
                TestContext.WriteLine($"Alternating corridor day {day}: " +
                    $"residents {residents.Model.ResidentCount}, " +
                    $"meals {needs.LastCompletedDayMeals.Fed}/{needs.LastCompletedDayMeals.Living}, " +
                    $"pigeon meals {needs.LastCompletedDayMeals.PigeonsFed}/{needs.LastCompletedDayMeals.PigeonsLiving}, " +
                    $"seeds left {needs.LastCompletedDayMeals.SeedPortionsLeft}, " +
                    $"animal deaths {mortality.Model.TotalDeaths}, " +
                    $"starvation {mortality.Model.StarvationDeaths}, " +
                    $"active {runtime.HasActiveRun}.");
            }

            Assert.That(completedDays, Is.GreaterThanOrEqualTo(2));
            TestContext.WriteLine(runtime.HasActiveRun
                ? $"Alternating corridor survived at least {completedDays} days."
                : $"Alternating corridor ended after day {runtime.CurrentResults.daysSurvived} " +
                  $"({runtime.CurrentResults.endReason}).");
        }

        [UnityTest]
        public IEnumerator LinkedGreeneryWithDailyMovesReportsActualMortality()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);

            var completedDays = 0;
            for (var day = 1; day <= 8 && runtime.HasActiveRun; day++)
            {
                if (day == 1)
                    Swap(bootstrap, editor, rooms, "pigeon-b", "shared-h", 2);
                else
                    Swap(bootstrap, editor, rooms, "residence-e", "residence-f");
                Assert.That(editor.LastConfirmedMovementDay, Is.EqualTo(day));
                var network = new RoomNavigationMap(editor.ExportLayout(),
                    RoomLayoutData.All, 3.1f, true, day);
                var greenCells = GreenNetworkModel.ConnectedCount(network);
                var seeds = HabitatFoodNetworkModel.TotalSeedCapacity(network, day);
                var linkedPlazas = HabitatFoodNetworkModel.ParkLinkedPigeonHabitats(network, day);
                var homes = (IReadOnlyDictionary<IWildlifeLayoutAgent, string>)
                    typeof(AnimalNeedsController).GetField("homeRooms",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(needs);
                var livingPigeonHomes = homes
                    .Where(pair => pair.Key is PigeonDemoAgent pigeon && pigeon.IsAlive)
                    .GroupBy(pair => pair.Value)
                    .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()));
                var seedMealCeiling = HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                    network, day, livingPigeonHomes);
                yield return SkipDay(runtime, day);
                completedDays++;
                TestContext.WriteLine($"Green network day {day}: " +
                    $"connected {greenCells}, seeds {seeds}, seed-meal ceiling {seedMealCeiling}, plazas {linkedPlazas}, " +
                    $"meals {needs.LastCompletedDayMeals.Fed}/{needs.LastCompletedDayMeals.Living}, " +
                    $"pigeon meals {needs.LastCompletedDayMeals.PigeonsFed}/{needs.LastCompletedDayMeals.PigeonsLiving}, " +
                    $"seeds left {needs.LastCompletedDayMeals.SeedPortionsLeft}, " +
                    $"residents {residents.Model.ResidentCount}, " +
                    $"animal deaths {mortality.Model.TotalDeaths}, " +
                    $"starvation {mortality.Model.StarvationDeaths} " +
                    $"(pigeon {mortality.Model.PigeonDeaths}, " +
                    $"squirrel {mortality.Model.SquirrelDeaths}, " +
                    $"hedgehog {mortality.Model.HedgehogDeaths}, " +
                    $"fox {mortality.Model.FoxDeaths}), " +
                    $"active {runtime.HasActiveRun}.");
            }

            Assert.That(completedDays, Is.GreaterThanOrEqualTo(2));
            TestContext.WriteLine(runtime.HasActiveRun
                ? $"Green network survived at least {completedDays} days."
                : $"Green network ended after day {runtime.CurrentResults.daysSurvived} " +
                  $"({runtime.CurrentResults.endReason}).");
        }

        [UnityTest]
        public IEnumerator OneOpeningSwapAloneDoesNotSolveTenDaysOfChangingRoutes()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            mortality.ConfigureDeathLimit(100);
            Swap(bootstrap, editor, generated.GetComponentsInChildren<RoomView>(true),
                "pigeon-c", "shared-f");
            Assert.That(residents.Model.PreviewCommute(residents.Model.NavigationMap).WorkingResidents,
                Is.GreaterThanOrEqualTo(ResidentPopulationController.MinimumRequiredResidents),
                "The opening food-network move should not already break the workforce target.");

            var completedDays = 0;
            for (var day = 1; day <= 10 && runtime.HasActiveRun; day++)
            {
                // Compare a fixed one-swap layout across days; bypass only the
                // planning gate, which has its own PlayMode regression test.
                editor.RestoreLastMovementDay(runtime.Clock.DayNumber);
                yield return SkipDay(runtime, day);
                completedDays++;
                TestContext.WriteLine($"Day {day}: workers {residents.LastReport.WorkingResidents}, " +
                    $"starvation deaths {mortality.Model.StarvationDeaths}, " +
                    $"active {runtime.HasActiveRun}.");
            }

            Assert.That(completedDays, Is.EqualTo(10),
                "This comparison must observe ten days, not end early for an unrelated workforce failure.");
            Assert.That(mortality.Model.StarvationDeaths, Is.GreaterThan(0),
                "A single permanent swap should not solve the changing food network.");
        }

        [UnityTest]
        public IEnumerator SwappingAnimalDoorsPreviewsAndCarriesThroughDayTwo()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            mortality.ConfigureDeathLimit(100);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var forecastLabel = hud.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Market Forecast Text");
            var planLabel = hud.GetComponentsInChildren<Text>(true)
                .Single(item => item.transform.parent.name == "Enter Layout Editing");
            Assert.That(forecastLabel.text, Does.Contain("5/4"));
            Assert.That(forecastLabel.text,
                Does.Contain("种子餐上限").Or.Contain("new-seed cap"));
            Canvas.ForceUpdateCanvases();
            Assert.That(forecastLabel.preferredHeight,
                Is.LessThanOrEqualTo(forecastLabel.rectTransform.rect.height + 2f));
            runtime.ToggleLanguage();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var oppositeLanguageHeight = forecastLabel.preferredHeight;
            runtime.ToggleLanguage();
            yield return null;
            Assert.That(oppositeLanguageHeight,
                Is.LessThanOrEqualTo(forecastLabel.rectTransform.rect.height + 2f),
                "The next-day forecast should fit in both languages.");
            Assert.That(planLabel.text, Is.EqualTo(runtime.Language == InterfaceLanguage.Chinese
                ? "规划" : "Plan"));
            editor.SetGuidedTargetRoom(null);
            editor.EnterEditing();
            Assert.That(editor.CanConfirm, Is.True);
            editor.ConfirmEditing();
            yield return null;
            Assert.That(editor.LastConfirmedPlanningDay, Is.EqualTo(1));
            Assert.That(editor.LastConfirmedMovementDay, Is.Zero);
            Assert.That(editor.FreeRearrangementAvailable, Is.True,
                "Reviewing an unchanged layout must not consume the free rearrangement.");
            Assert.That(planLabel.text, Is.EqualTo(runtime.Language == InterfaceLanguage.Chinese
                ? "继续调整" : "Adjust"));
            editor.EnterEditing();

            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var plaza = rooms.Single(item => item.Spec.Id == "pigeon-a");
            var garden = rooms.Single(item => item.Spec.Id == "shared-j");
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(plaza.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(garden.VisualRoot.position);
            var start = new Vector2(from.x, from.y);
            var target = new Vector2(to.x, to.y);
            InvokeDrag(editor, "HandleDragStarted", plaza, start);
            InvokeDrag(editor, "HandleDragging", plaza, target);
            Assert.That(editor.TryGetImpactPreview(residents.Model, out var preview), Is.True);
            Assert.That(preview.SeedForecastDay, Is.EqualTo(2));
            Assert.That(preview.BeforeGreenCells, Is.EqualTo(5));
            Assert.That(preview.AfterGreenCells, Is.EqualTo(4));
            Assert.That(preview.BeforeSeedCapacity, Is.GreaterThan(0));
            Assert.That(preview.AfterSeedCapacity, Is.GreaterThan(preview.BeforeSeedCapacity));
            Assert.That(preview.AfterPigeonSeedMealCeiling,
                Is.InRange(0, preview.AfterSeedCapacity));
            InvokeDrag(editor, "HandleDragEnded", plaza, target);
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            Assert.That(editor.PendingMovementCost, Is.Zero);
            editor.ConfirmEditing();
            yield return null;
            Assert.That(forecastLabel.text, Does.Contain("4/4"));
            Assert.That(planLabel.text, Is.EqualTo(runtime.Language == InterfaceLanguage.Chinese
                ? "已调整" : "Adjusted"));
            Assert.That(residents.Model.ResidentCount,
                Is.GreaterThanOrEqualTo(ResidentPopulationController.MinimumRequiredResidents));

            yield return SkipDay(runtime, 1);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            Assert.That(food.Model.TotalPortions, Is.GreaterThan(0));
            var dayTwo = new RoomNavigationMap(editor.ExportLayout(), RoomLayoutData.All, 3.1f, true, 2);
            Assert.That(GreenNetworkModel.ConnectedCount(dayTwo), Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator EveryBirdInASwappedFlockRoomMovesWithThatRoom()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            editor.SetGuidedTargetRoom(null);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var pigeonRoom = rooms.Single(item => item.Spec.Id == "pigeon-a");
            var firstCenter = pigeonRoom.VisualRoot.position;
            var flock = generated.GetComponentsInChildren<PigeonDemoAgent>(true)
                .Where(item => item.name is "Pigeon 01" or "Pigeon 02" or "Pigeon 10")
                .OrderBy(item => item.name).ToArray();
            Assert.That(flock.Length, Is.EqualTo(3));
            var before = flock.Select(item => item.SpawnPosition).ToArray();

            Swap(bootstrap, editor, rooms, "pigeon-a", "shared-e");
            var delta = pigeonRoom.VisualRoot.position - firstCenter;
            Assert.That(delta.sqrMagnitude, Is.GreaterThan(1f));
            for (var index = 0; index < flock.Length; index++)
                Assert.That(Vector3.Distance(flock[index].SpawnPosition - before[index], delta),
                    Is.LessThan(0.001f), flock[index].name);
        }

        private static void Swap(UrbanWildlifeBootstrap bootstrap, RoomLayoutEditorController editor,
            RoomView[] rooms, string movingId, string targetId, int quarterTurns = 0)
        {
            editor.EnterEditing();
            var moving = rooms.Single(item => item.Spec.Id == movingId);
            var target = rooms.Single(item => item.Spec.Id == targetId);
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(moving.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(target.VisualRoot.position);
            InvokeDrag(editor, "HandleDragStarted", moving, new Vector2(from.x, from.y));
            for (var turn = 0; turn < quarterTurns; turn++)
                editor.RotateSelected();
            InvokeDrag(editor, "HandleDragging", moving, new Vector2(to.x, to.y));
            InvokeDrag(editor, "HandleDragEnded", moving, new Vector2(to.x, to.y));
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
        }

        private static IEnumerator SkipDay(GameRuntimeController runtime, int day)
        {
            Assert.That(runtime.TrySkipToNextDay(), Is.True, $"Could not skip day {day}.");
            var deadline = Time.realtimeSinceStartup + 8f;
            while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(runtime.IsDaySkipping, Is.False, $"Day {day} skip timed out.");
        }

        private static void InvokeDrag(
            RoomLayoutEditorController editor, string methodName, RoomView room, Vector2 position)
        {
            var method = typeof(RoomLayoutEditorController).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(editor, new object[] { room, position });
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
