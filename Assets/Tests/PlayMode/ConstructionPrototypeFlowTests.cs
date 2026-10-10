using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionPrototypeFlowTests
    {
        [UnityTest]
        public IEnumerator FullBoardShowsResultAndCanStartNewRun()
        {
            var run = new ConstructionRunModel();
            Assert.That(run.TrySimulateDay(out _), Is.True);
            var positions = Enumerable.Range(0, 7)
                .SelectMany(column => Enumerable.Range(0, 7)
                    .Select(row => (column, row)))
                .Where(position => position.column != 3 || position.row != 3)
                .OrderBy(position => Mathf.Abs(position.column - 3) +
                    Mathf.Abs(position.row - 3));
            foreach (var position in positions)
            {
                Assert.That(run.TryBuild(position.column, position.row,
                    ConstructionCategory.Green, GreenPlanting.Meadow,
                    out _, out var failure, out _), Is.True, failure.ToString());
                Assert.That(run.TrySimulateDay(out _), Is.True);
            }
            var root = new GameObject("Construction result test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            yield return null;

            var result = root.transform.Find(
                "Construction UI/Construction final result");
            Assert.That(result.gameObject.activeSelf, Is.True);
            var numbers = result.Find("Result card/Result numbers")
                .GetComponent<Text>().text;
            Assert.That(numbers, Does.Contain("Animal score"));
            Assert.That(numbers, Does.Contain("Human score"));
            Assert.That(numbers, Does.Contain("Completed in 49 days"));
            var reset = result.GetComponentInChildren<Button>();
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(49));
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(result.gameObject.activeSelf, Is.False);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PigeonArrivalIntroducesOneResidentWithVisibleFoodNeed()
        {
            var root = new GameObject("Construction resident test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            var visitor = root.transform.Find(
                "Construction UI/Building choices/Waiting visitor");
            Assert.That(visitor, Is.Not.Null);
            Assert.That(visitor.gameObject.activeSelf, Is.True);

            Assert.That(controller.TrySelect(ConstructionCategory.Residence), Is.True);
            Assert.That(controller.TryPlace(3, 2), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildRestaurant));
            Assert.That(visitor.gameObject.activeSelf, Is.False);
            var home = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,3");
            Assert.That(home.transform.Find("First resident").gameObject.activeSelf,
                Is.True);
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.False);

            Assert.That(controller.AdvanceDay(), Is.True,
                "A residence uses today's one construction slot.");
            Assert.That(controller.TrySelect(ConstructionCategory.Restaurant), Is.True);
            Assert.That(controller.TryPlace(3, 1), Is.True);
            var restaurant = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,2");
            var seats = restaurant.transform.Find("Daily capacity/Capacity count")
                .GetComponent<Text>();
            Assert.That(seats.text, Is.EqualTo("~1/1"));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchResidentEat));
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.True);
            Assert.That(controller.TrySelect(ConstructionCategory.Workshop), Is.False);
            Assert.That(controller.AdvanceDay(), Is.True);
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.BuildWorkshop));
            Assert.That(home.transform.Find("Waste backlog").gameObject.activeSelf,
                Is.True);
            Assert.That(home.transform.Find("Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("2"),
                "The residence produced waste on its own construction day too.");
            Assert.That(restaurant.transform.Find("Waste backlog/Waste count")
                .GetComponent<Text>().text, Is.EqualTo("1"));
            var walking = root.transform.Find(
                "Construction UI/7 by 7 construction board/Resident walking");
            Assert.That(walking, Is.Not.Null);
            Assert.That(walking.gameObject.activeSelf, Is.True);
            Assert.That(home.transform.Find("Food access needed").gameObject.activeSelf,
                Is.False);
            Assert.That(home.transform.Find("Work access needed").gameObject.activeSelf,
                Is.True);
            Assert.That(seats.text, Is.EqualTo("~1/1"));
            Assert.That(controller.ObservedHumanWorkCycles, Is.Zero);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.That(walking.gameObject.activeSelf, Is.False);
            var resident = home.transform.Find("First resident")
                .GetComponent<RectTransform>();
            Assert.That(resident.gameObject.activeSelf, Is.True);
            Assert.That(resident.anchoredPosition.x, Is.EqualTo(23f).Within(0.1f));
            Assert.That(resident.anchoredPosition.y, Is.EqualTo(-22f).Within(0.1f));
            Assert.That(controller.TryPlace(3, 2), Is.False,
                "Selecting an occupied residence inspects rather than rebuilds it.");
            var routeStatus = root.transform.Find(
                "Construction UI/Route inspection/Route status").GetComponent<Text>();
            Assert.That(routeStatus.text, Does.Contain("no work route"));
            var overlay = root.transform.Find(
                "Construction UI/7 by 7 construction board/Selected route overlay");
            Assert.That(overlay.GetComponentsInChildren<Image>()
                .Count(image => image.name == "Route segment"), Is.GreaterThan(0));
            Assert.That(controller.TryPlace(3, 3), Is.False);
            Assert.That(routeStatus.text, Does.Contain("SQUIRREL"));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CentralOakDayAndGreenPlacementWorkInActualUi()
        {
            var root = new GameObject("Construction UI test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WatchSquirrelEat));
            Assert.That(root.GetComponentsInChildren<Button>(true)
                .Count(button => button.name.StartsWith("Cell ")), Is.EqualTo(49));
            Assert.That(root.GetComponentsInChildren<Text>(true)
                .All(label => label.font != null), Is.True);
            var starter = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 4,4");
            var oakIcon = starter.transform.Find("Room icon").GetComponent<Image>();
            Assert.That(oakIcon.sprite, Is.Not.Null);
            var initialOakSize = oakIcon.rectTransform.sizeDelta.x;

            Assert.That(controller.AdvanceDay(), Is.True);
            var review = root.transform.Find(
                "Construction UI/Building choices/Last day review/Day review text")
                .GetComponent<Text>();
            Assert.That(review.text, Does.Contain("D1  Animal 1/1"));
            Assert.That(oakIcon.rectTransform.sizeDelta.x, Is.EqualTo(initialOakSize),
                "The established starter oak should already be mature.");
            Assert.That(controller.CurrentDay, Is.EqualTo(2));
            Assert.That(controller.Stage, Is.EqualTo(ConstructionStoryStage.GrowGreen));
            Assert.That(controller.TrySelect(ConstructionCategory.Green,
                GreenPlanting.Meadow), Is.True);
            Assert.That(controller.TryPlace(4, 3), Is.True);
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TryPlace(2, 3), Is.False,
                "A second valid cell cannot be built on the same day.");
            Assert.That(root.transform.Find(
                "Construction UI/Building choices/Feedback").GetComponent<Text>().text,
                Does.Contain("One new cell per day"));
            Assert.That(controller.TryPlace(6, 6), Is.False);

            var newGreen = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Cell 5,4");
            var newGreenIcon = newGreen.transform.Find("Room icon")
                .GetComponent<Image>();
            var initialGreenSize = newGreenIcon.rectTransform.sizeDelta.x;
            Assert.That(newGreen.transform.Find("Food stock/Food count")
                .GetComponent<Text>().text, Is.EqualTo("0"));
            Assert.That(controller.AdvanceDay(), Is.True);
            var pigeonFlock = newGreen.transform.Find("Pigeon flock");
            Assert.That(pigeonFlock.gameObject.activeSelf, Is.True);
            Assert.That(pigeonFlock.localScale.x, Is.LessThan(1f),
                "The arriving flock should pop into view instead of appearing abruptly.");
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(newGreenIcon.rectTransform.sizeDelta.x,
                Is.GreaterThan(initialGreenSize),
                "A new green tile should visibly grow after the day changes.");
            Assert.That(controller.CurrentDay, Is.EqualTo(3));
            Assert.That(controller.Stage, Is.EqualTo(
                ConstructionStoryStage.WelcomeResident));
            Assert.That(pigeonFlock.localScale.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(pigeonFlock.GetComponent<Image>().sprite,
                Is.Not.Null);
            Assert.That(controller.ObservedAnimalMeals, Is.EqualTo(2),
                "Each of the first two days includes one actual oak meal; the " +
                "new flock has not eaten until its first full day.");

            var reset = root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Start a new construction run");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2));
            Assert.That(controller.TrySelect(ConstructionCategory.Green), Is.True);
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(2),
                "Any intervening action should cancel reset confirmation.");
            reset.onClick.Invoke();
            Assert.That(controller.BuiltCount, Is.EqualTo(1));
            Assert.That(controller.CurrentDay, Is.EqualTo(1));
            Assert.That(review.text, Does.Contain("No simulated result yet"));
            Object.Destroy(root);
            yield return null;
        }
    }
}
