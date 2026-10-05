using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
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
    public sealed class FoodBalanceFlowTests
    {
        [UnityTest]
        public IEnumerator RestartConfirmationReceivesPointerClickAndResetsDayAndLayout()
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
            var moved = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(moved.TrySwap("pigeon-c", "shared-f"), Is.True);
            Assert.That(editor.RestoreLayout(moved.ExportData()), Is.True);
            runtime.RestoreSession(SimulationClockModel.CycleSeconds + 1d, 1, GameMode.Sandbox);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(2));
            runtime.TogglePauseMenu();

            var buttons = generated.GetComponentsInChildren<Button>(true);
            var restart = buttons.Single(item => item.name == "Restart Endless Mode");
            Assert.That(restart.gameObject.activeInHierarchy, Is.True);
            restart.onClick.Invoke();
            var confirm = buttons.Single(item => item.name == "Confirm Restart");
            Assert.That(confirm.gameObject.activeInHierarchy, Is.True);
            yield return null; // Let the newly visible modal register its graphics for raycasting.
            Canvas.ForceUpdateCanvases();
            var rect = confirm.GetComponent<RectTransform>();
            var screen = RectTransformUtility.WorldToScreenPoint(
                bootstrap.LayoutCamera, rect.TransformPoint(rect.rect.center));
            var click = new PointerEventData(EventSystem.current)
            {
                position = screen,
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(click, hits);
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits[0].gameObject, Is.EqualTo(confirm.gameObject),
                    "The visible confirm control must be the topmost pointer target.");
            }
            // Execute the pointer-click path even in graphics-free CI, where
            // GraphicRaycaster cannot supply screen hits.
            ExecuteEvents.Execute(confirm.gameObject, click, ExecuteEvents.pointerClickHandler);

            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(1));
            Assert.That(runtime.PauseMenuOpen, Is.False);
            Assert.That(runtime.HasActiveRun, Is.True);
            var restored = editor.ExportLayout().ToDictionary(item => item.id);
            var original = RoomLayoutData.All.ToDictionary(item => item.Id);
            Assert.That(restored["pigeon-c"].column, Is.EqualTo(original["pigeon-c"].Column));
            Assert.That(restored["pigeon-c"].row, Is.EqualTo(original["pigeon-c"].Row));
        }

        [UnityTest]
        public IEnumerator FewerThanThreeWorkersEndsRunAtDayBoundaryWithoutResourceSettlement()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            mortality.ConfigureDeathLimit(100);
            var initial = residents.Model.ExportResidents();
            var oldBalance = economy.Balance;
            Assert.That(economy.TrySpend(1), Is.False,
                "No old resource-point payment should remain active.");
            Assert.That(economy.Balance, Is.EqualTo(oldBalance));
            Assert.That(residents.Model.PreviewCommute(residents.Model.NavigationMap).WorkingResidents,
                Is.GreaterThanOrEqualTo(ResidentPopulationController.MinimumRequiredResidents));
            residents.RestoreSession(initial.Take(2), string.Empty, 3, 0, 0, 0, 2);
            Assert.That(residents.Model.PreviewCommute(residents.Model.NavigationMap).WorkingResidents,
                Is.EqualTo(2));
            var workforce = generated.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Workforce Target");
            Assert.That(workforce.text, Does.Contain("2/2"));

            // This test isolates the population threshold, not the planning gate.
            generated.GetComponentInChildren<RoomLayoutEditorController>(true)
                .RestoreLastMovementDay(runtime.Clock.DayNumber);
            runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
            economy.ProcessUntil(runtime.Clock.TotalSeconds);
            Assert.That(runtime.HasActiveRun, Is.False);
            Assert.That(runtime.CurrentResults.endReason, Is.EqualTo(RunEndReason.InsufficientResidents));
            Assert.That(runtime.CurrentResults.finalResidents, Is.EqualTo(2));
            Assert.That(runtime.CurrentResults.requiredResidents, Is.EqualTo(3));
            Assert.That(economy.Model.CumulativeIncome, Is.Zero);
            Assert.That(economy.Model.CumulativeSpending, Is.Zero);
            yield return null;
            var resultWorkforce = generated.GetComponentsInChildren<Text>(true)
                .Single(item => item.transform.parent.name == "Workforce Summary");
            Assert.That(resultWorkforce.text, Does.Contain("2"));
        }

        [UnityTest]
        public IEnumerator FirstDaySettlementAppearsAsObservedDailyOutcome()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var outcome = generated.GetComponentInChildren<DailyOutcomeController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
            Assert.That(outcome, Is.Not.Null);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            economy.RestoreSession(-5f, 0f, 0f, 0f, 0f);
            generated.GetComponentInChildren<RoomLayoutEditorController>(true)
                .RestoreLastMovementDay(runtime.Clock.DayNumber);
            Assert.That(runtime.TrySkipToNextDay(), Is.True);
            var deadline = Time.realtimeSinceStartup + 8f;
            while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(runtime.IsDaySkipping, Is.False);
            Assert.That(runtime.HasActiveRun, Is.True);
            Assert.That(economy.Balance, Is.EqualTo(-5f),
                "The former balance is inert, not a hidden failure condition.");
            Assert.That(outcome.Model.LastReport.Day, Is.EqualTo(1));
            Assert.That(outcome.Model.LastReport.WorkingResidents,
                Is.GreaterThanOrEqualTo(ResidentPopulationController.MinimumRequiredResidents));
            Assert.That(outcome.Model.LastReport.MovedRooms, Is.Zero);
            Assert.That(outcome.Model.LastReport.MealsKnown, Is.True);
            Assert.That(outcome.Model.LastReport.FedAnimals,
                Is.InRange(0, outcome.Model.LastReport.LivingAnimals));
            Assert.That(outcome.Model.LastReport.FedAnimals,
                Is.EqualTo(needs.LastCompletedDayMeals.Fed));
            Assert.That(outcome.Model.LastReport.SeedPortionsLeft,
                Is.EqualTo(needs.LastCompletedDayMeals.SeedPortionsLeft),
                "The review must use pre-dawn remaining seeds, not tomorrow's replenished stock.");
            var summary = generated.GetComponentsInChildren<Text>(true)
                .Single(item => item.gameObject.name == "Daily Outcome Summary");
            Assert.That(summary.gameObject.activeInHierarchy, Is.True);
            Assert.That(summary.text, Does.Contain("第 1 天").Or.Contain("Day 1"));
            Assert.That(summary.text, Does.Contain("实际进食").Or.Contain("ate "));
            Canvas.ForceUpdateCanvases();
            Assert.That(summary.preferredHeight,
                Is.LessThanOrEqualTo(summary.rectTransform.rect.height + 2f),
                "The concise outcome must fit its card in the current language.");
            runtime.ToggleLanguage();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var otherLanguageHeight = summary.preferredHeight;
            var otherLanguageText = summary.text;
            runtime.ToggleLanguage();
            Assert.That(otherLanguageText, Does.Contain("实际进食").Or.Contain("ate "));
            Assert.That(otherLanguageHeight,
                Is.LessThanOrEqualTo(summary.rectTransform.rect.height + 2f),
                "The outcome must also fit when the player switches language.");
        }

        [UnityTest]
        public IEnumerator UneatenPlayerFoodBecomesLocalWasteButEatenFoodDoesNot()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var waste = generated.GetComponentInChildren<WasteManagementController>(true);
            var park = generated.GetComponentsInChildren<RoomView>(true)
                .Single(item => item.Spec.Id == "central-park");
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var world = park.VisualRoot.TransformPoint(new Vector3(0.55f, 0.16f, 0.55f));
            var initialWaste = waste.Model.TotalWasteUnits;
            var discardedCount = 0;
            feeding.LeftoverFoodDiscarded += (_, portions, _) =>
            {
                Assert.That(portions, Is.EqualTo(PlayerFoodSourceModel.PortionsPerSource));
                discardedCount++;
            };

            Assert.That(feeding.TryPlaceFood("central-park", world), Is.True);
            runtime.Clock.Advance(PlayerFoodSourceModel.LifetimeSeconds + 1d, 1f);
            yield return null;
            Assert.That(discardedCount, Is.EqualTo(1));
            Assert.That(waste.Model.TotalWasteUnits, Is.EqualTo(initialWaste + 1));
            Assert.That(feeding.Model.Sources, Is.Empty);
            var feedingHint = generated.GetComponentsInChildren<Text>(true)
                .Single(item => item.gameObject.name == "Feeding Instruction");
            Assert.That(feedingHint.text, Does.Contain("垃圾 +1").Or.Contain("waste +1"));

            runtime.RestartRun();
            runtime.SetOnboardingOpen(false);
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.Zero);
            var wasteAfterRestart = waste.Model.TotalWasteUnits;
            Assert.That(feeding.TryPlaceFood("central-park", world), Is.True);
            var eaten = feeding.Model.Sources.Values.Single();
            for (var index = 0; index < PlayerFoodSourceModel.PortionsPerSource; index++)
            {
                Assert.That(feeding.Model.TryClaimPortion(eaten.id), Is.True);
            }
            runtime.Clock.Advance(PlayerFoodSourceModel.LifetimeSeconds + 1d, 1f);
            yield return null;
            Assert.That(discardedCount, Is.EqualTo(1));
            Assert.That(waste.Model.TotalWasteUnits, Is.EqualTo(wasteAfterRestart));
        }

        [UnityTest]
        public IEnumerator PlayerFoodOnlyCallsSquirrelsFromTheirHomeOrOneConnectedRoom()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var navigation = generated.GetComponentInChildren<AnimalNavigationCoordinator>(true);
            var squirrels = generated.GetComponentsInChildren<SquirrelDemoAgent>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var board = generated.Find("7x7 Modular Floor");
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);

            string CacheRoom(SquirrelDemoAgent squirrel)
            {
                var local = board.InverseTransformPoint(squirrel.CachePosition);
                return navigation.NavigationMap.TryFindRoomContaining(
                    new Vector2(local.x, local.z), out var roomId) ? roomId : string.Empty;
            }

            const string farRoom = "garage-a";
            Assert.That(squirrels, Is.Not.Empty);
            Assert.That(squirrels.All(squirrel =>
                !HabitatFoodNetworkModel.IsWithinSquirrelHomeRange(
                    navigation.NavigationMap, CacheRoom(squirrel), farRoom)), Is.True);
            var farView = rooms.Single(room => room.Spec.Id == farRoom);
            var farPosition = farView.VisualRoot.TransformPoint(new Vector3(0f, 0.16f, 0f));
            Assert.That(feeding.TryPlaceFood(farRoom, farPosition), Is.True);
            Assert.That(squirrels.Any(squirrel => squirrel.IsRespondingToFood), Is.False,
                "A distant manual source must not pull a squirrel beyond its home range.");

            runtime.RestartRun();
            runtime.SetOnboardingOpen(false);
            const string homeRoom = "oak-a";
            var homeSquirrel = squirrels.First(squirrel => CacheRoom(squirrel) == homeRoom);
            var adjacentRoom = navigation.NavigationMap.NeighboursOf(homeRoom).First();
            var adjacentView = rooms.Single(room => room.Spec.Id == adjacentRoom);
            var adjacentPosition = adjacentView.VisualRoot.TransformPoint(new Vector3(0f, 0.16f, 0f));
            Assert.That(feeding.TryPlaceFood(adjacentRoom, adjacentPosition), Is.True);
            Assert.That(homeSquirrel.IsRespondingToFood, Is.True,
                "Food in a directly connected neighbouring room should still attract it.");
        }

        [UnityTest]
        public IEnumerator ManualFeedHasThreeDayCooldownAndInvalidPlacementDoesNotConsumeIt()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
            var park = generated.GetComponentsInChildren<RoomView>(true)
                .Single(item => item.Spec.Id == "central-park");
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            var world = park.VisualRoot.TransformPoint(new Vector3(0.55f, 0.16f, 0.55f));
            economy.RestoreSession(0f, 0f, 0f, 0f, 0f);
            var startingBalance = economy.Balance;
            Assert.That(feeding.CanActivateFeedingMode, Is.True);

            Assert.That(feeding.TryPlaceFood(string.Empty, world), Is.False);
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.Zero);
            Assert.That(economy.Balance, Is.EqualTo(startingBalance));
            Assert.That(feeding.TryPlaceFood("central-park", world), Is.True,
                "A zero legacy balance must not gate feeding.");
            Assert.That(feeding.LastManualFeedingDay, Is.EqualTo(1));
            Assert.That(feeding.NextManualFeedingDay, Is.EqualTo(4));
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.EqualTo(3));
            var savedSources = feeding.Model.Export();
            Assert.That(savedSources, Has.Count.EqualTo(1));
            yield return null;
            var feedingButton = generated.GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "Activate Feeding Mode");
            Assert.That(feedingButton.interactable, Is.False);
            Assert.That(feedingButton.GetComponentInChildren<Text>().text,
                Does.Contain("冷却").Or.Contain("Wait"));
            Assert.That(feeding.TryPlaceFood("central-park", world), Is.False);
            Assert.That(economy.Balance, Is.EqualTo(startingBalance),
                "Manual feeding is limited by days, not resource points.");

            // Inspect the day-indexed rule without advancing economy or animal needs.
            runtime.Clock.Restore(SimulationClockModel.CycleSeconds);
            feeding.RestoreSession(savedSources, 0, null, 0);
            Assert.That(feeding.LastManualFeedingDay, Is.EqualTo(2),
                "An older save with active manual food must not grant an immediate extra feed.");
            feeding.RestoreSession(savedSources, 0, null, 1);
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.EqualTo(2),
                "Reloading must not grant an extra manual feed.");
            runtime.Clock.Restore(2d * SimulationClockModel.CycleSeconds);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(3));
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.EqualTo(1));
            runtime.Clock.Restore(3d * SimulationClockModel.CycleSeconds);
            Assert.That(runtime.Clock.DayNumber, Is.EqualTo(4));
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.Zero);
            Assert.That(feeding.TryPlaceFood("central-park", world), Is.True);
            Assert.That(feeding.NextManualFeedingDay, Is.EqualTo(7));

            runtime.RestartRun();
            Assert.That(feeding.LastManualFeedingDay, Is.Zero);
            Assert.That(feeding.ManualFeedingDaysRemaining, Is.Zero);
        }

        [UnityTest]
        public IEnumerator StaticLayoutWithPlanningGateBypassedEventuallyLosesAnimals()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);

            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Object.Destroy(persistence); // Never write to the player's save/profile.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var workerMeals = generated.GetComponentInChildren<WorkerPasserbyFeedingController>(true);
            Assert.That(workerMeals, Is.Not.Null);

            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            for (var day = 1; day <= 8 && runtime.HasActiveRun; day++)
            {
                // Isolate long-term food balance; no-action is no longer a legal play strategy.
                generated.GetComponentInChildren<RoomLayoutEditorController>(true)
                    .RestoreLastMovementDay(runtime.Clock.DayNumber);
                Assert.That(runtime.TrySkipToNextDay(), Is.True);
                var deadline = Time.realtimeSinceStartup + 8f;
                while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(runtime.IsDaySkipping, Is.False, $"Day {day} did not finish skipping.");
                var hungry = needs.Model.Animals.Values.Count(item => item.hungerDays > 0);
                TestContext.WriteLine(
                    $"Completed day {day}: map food {food.Model.TotalPortions}, " +
                    $"worker meals {workerMeals.Quota.FedResidents.Count}, " +
                    $"hungry {hungry}, deaths {mortality.Model.TotalDeaths} " +
                    $"(starvation {mortality.Model.StarvationDeaths}, traffic {mortality.Model.TrafficDeaths}).");
            }

            Assert.That(runtime.HasActiveRun, Is.False,
                "A static food network must not be stable when the planning gate is bypassed for this diagnostic.");
            Assert.That(runtime.CurrentResults.daysSurvived, Is.InRange(2, 8));
            Assert.That(mortality.Model.StarvationDeaths, Is.GreaterThan(0),
                "Food scarcity, not only traffic or predation, must contribute to the unattended loss.");
        }

        [UnityTest]
        public IEnumerator PeriodicFeedingAtTheParkCannotReplaceAHabitatNetwork()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var park = generated.GetComponentsInChildren<RoomView>(true)
                .Single(item => item.Spec.Id == "central-park");
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);

            for (var day = 1; day <= 6 && runtime.HasActiveRun; day++)
            {
                if (feeding.CanActivateFeedingMode)
                {
                    var world = park.VisualRoot.TransformPoint(new Vector3(0.55f, 0.16f, 0.55f));
                    Assert.That(feeding.TryPlaceFood("central-park", world), Is.True);
                }
                // Compare food placement with a static layout, without testing the daily gate here.
                generated.GetComponentInChildren<RoomLayoutEditorController>(true)
                    .RestoreLastMovementDay(runtime.Clock.DayNumber);
                Assert.That(runtime.TrySkipToNextDay(), Is.True);
                var deadline = Time.realtimeSinceStartup + 8f;
                while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(runtime.IsDaySkipping, Is.False, $"Day {day} skip timed out.");
            }

            Assert.That(runtime.HasActiveRun, Is.False,
                "Repeated feeding at one site should not solve a dispersed habitat shortage.");
            Assert.That(runtime.CurrentResults.daysSurvived, Is.LessThanOrEqualTo(6));
            Assert.That(mortality.Model.StarvationDeaths, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator AnnouncedMarketRoutesWasteWithoutCurrencyIncome()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var waste = generated.GetComponentInChildren<WasteManagementController>(true);
            var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            // This test isolates scheduled waste from unrelated
            // traffic/predation losses during the seven-day fast-forward.
            mortality.ConfigureDeathLimit(100);
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("pigeon-d", "residence-d"), Is.True);
            Assert.That(layout.TrySwap("residence-e", "trash-d"), Is.True);
            Assert.That(editor.RestoreLayout(layout.ExportData()), Is.True);

            editor.EnterEditing();
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var home = rooms.Single(item => item.Spec.Id == "residence-g");
            var bin = rooms.Single(item => item.Spec.Id == "trash-a");
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(home.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(bin.VisualRoot.position);
            var dragStart = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragStarted", BindingFlags.Instance | BindingFlags.NonPublic);
            var dragMove = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragging", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dragStart, Is.Not.Null);
            Assert.That(dragMove, Is.Not.Null);
            dragStart.Invoke(editor, new object[] { home, new Vector2(from.x, from.y) });
            dragMove.Invoke(editor, new object[] { home, new Vector2(to.x, to.y) });
            Assert.That(editor.TryGetImpactPreview(residents.Model, out var forecast), Is.True);
            Assert.That(forecast.MarketDay, Is.EqualTo(4));
            Assert.That(forecast.MarketRoomId, Is.EqualTo("canteen-a"));
            var impactPanel = generated.GetComponentInChildren<UrbanWildlifeRooms.UI.UrbanWildlifeHud>(true)
                .transform.Find("Minimal Gameplay HUD/Layout Impact Preview");
            Assert.That(impactPanel, Is.Not.Null);
            var nextPage = impactPanel.Find("Next Impact Page")?.GetComponent<Button>();
            Assert.That(nextPage, Is.Not.Null);
            var foundMarket = false;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            for (var page = 0; page < 4 && !foundMarket; page++)
            {
                foreach (var cardName in new[] { "Impact Metric 1", "Impact Metric 2" })
                {
                    var card = impactPanel.Find(cardName);
                    if (card == null || !card.gameObject.activeSelf) continue;
                    var label = card.Find("Metric Label")?.GetComponent<Text>();
                    if (label == null || !label.text.Contains(chinese ? "轮值垃圾" : "Rotation waste")) continue;
                    foundMarket = true;
                    Assert.That(card.Find("Metric Icon")?.GetComponent<LayoutImpactPictogramGraphic>()?.Kind,
                        Is.EqualTo(LayoutImpactMetricKind.MarketWaste));
                    Assert.That(card.Find("Baseline Value")?.GetComponent<Text>()?.text,
                        Does.StartWith(chinese ? "原 " : "Was "));
                    var explanation = card.Find("Metric Explanation/Explanation Text")?.GetComponent<Text>();
                    Assert.That(explanation?.text,
                        Does.Contain(chinese ? "第 4 天轮值" : "day 4 rotation"));
                    Canvas.ForceUpdateCanvases();
                    Assert.That(explanation.preferredHeight,
                        Is.LessThanOrEqualTo(explanation.rectTransform.rect.height + 2f));
                }
                if (!foundMarket && nextPage.interactable) nextPage.onClick.Invoke();
            }
            Assert.That(foundMarket, Is.True, "Rotation waste must stay available via pages.");
            var dragEnd = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragEnded", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dragEnd, Is.Not.Null);
            dragEnd.Invoke(editor, new object[] { home, new Vector2(to.x, to.y) });
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();

            var marketWaste = -1;
            var marketResidents = -1;
            ResourceSettlement marketSettlement = default;
            waste.DailyWasteProduced += day =>
            {
                if (day != 7) return;
                marketWaste = waste.Model.TotalWasteUnits;
                marketResidents = waste.ResidentCount;
            };
            economy.DaySettled += settlement =>
            {
                if (settlement.DayNumber == 7) marketSettlement = settlement;
            };

            for (var day = 1; day <= 7; day++)
            {
                Assert.That(runtime.HasActiveRun, Is.True, $"Run ended before market day {day}.");
                // Hold layout constant so the test measures the announced market schedule.
                editor.RestoreLastMovementDay(runtime.Clock.DayNumber);
                Assert.That(runtime.TrySkipToNextDay(), Is.True);
                var deadline = Time.realtimeSinceStartup + 8f;
                while (runtime.IsDaySkipping && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(runtime.IsDaySkipping, Is.False, $"Day {day} skip timed out.");
                if (day == 6)
                {
                    var banner = generated.GetComponentInChildren<UrbanWildlifeRooms.UI.UrbanWildlifeHud>(true)
                        .transform.Find("Minimal Gameplay HUD/Neighborhood Market Forecast");
                    Assert.That(banner, Is.Not.Null);
                    Assert.That(banner.gameObject.activeSelf, Is.True);
                    var label = banner.GetComponentInChildren<Text>();
                    Assert.That(label.text, Does.Contain(chinese ? "今日" : "Today"));
                    Assert.That(label.text, Does.Contain(chinese ? "社区集市" : "Community market"));
                    Canvas.ForceUpdateCanvases();
                    Assert.That(label.preferredHeight,
                        Is.LessThanOrEqualTo(label.rectTransform.rect.height + 2f),
                        "The next-market hint must fit without covering another control.");
                    var originalText = label.text;
                    label.text = "D8 · open passages · plan today\n" +
                                 "Green 4/4 · seeds 9\nCommunity market · Day 7 · Food shop A · waste +5";
                    Canvas.ForceUpdateCanvases();
                    Assert.That(label.preferredHeight,
                        Is.LessThanOrEqualTo(label.rectTransform.rect.height + 2f),
                        "The English market hint must also fit its card.");
                    label.text = originalText;
                }
            }

            Assert.That(marketResidents, Is.GreaterThan(0));
            Assert.That(marketWaste, Is.EqualTo(marketResidents + 2 * 2 + 2 + NeighborhoodMarketSchedule.ExtraWaste),
                "Odd-day market waste should be routed in addition to normal waste after day-six collection.");
            Assert.That(marketSettlement.DayNumber, Is.EqualTo(7));
            Assert.That(waste.Model.AffectedWasteRoomCount, Is.Zero);
            Assert.That(marketSettlement.Production, Is.Zero,
                "The day boundary must not generate hidden resource points.");
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
