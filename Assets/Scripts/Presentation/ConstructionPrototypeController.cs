using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    // A separate vertical slice while the legacy full-board simulation is
    // migrated. Green food and squirrel/pigeon meals now settle each day;
    // daily home/work/food capacity now settles in the independent prototype.
    public sealed class ConstructionPrototypeController : MonoBehaviour
    {
        private enum RouteSubject { None, Resident, Squirrel, Pigeons }

        private static readonly Color Ink = new(0.13f, 0.19f, 0.24f);
        private static readonly Color Paper = new(0.93f, 0.89f, 0.79f);
        private static readonly Color Green = new(0.39f, 0.57f, 0.34f);
        private static readonly Color Teal = new(0.26f, 0.63f, 0.60f);
        private static readonly Color Locked = new(0.49f, 0.49f, 0.46f);

        private readonly Dictionary<ConstructionCategory, Button> categoryButtons = new();
        private readonly Dictionary<GreenPlanting, Button> plantingButtons = new();
        private readonly Image[,] cellBackgrounds = new Image[7, 7];
        private readonly Image[,] cellIcons = new Image[7, 7];
        private readonly RectTransform[,] cellIconRects = new RectTransform[7, 7];
        private readonly Image[,] squirrelIcons = new Image[7, 7];
        private readonly Image[,] pigeonIcons = new Image[7, 7];
        private readonly Text[,] foodBadges = new Text[7, 7];
        private readonly Text[,] capacityBadges = new Text[7, 7];
        private readonly Text[,] wasteBadges = new Text[7, 7];
        private readonly GameObject[,] residentMarkers = new GameObject[7, 7];
        private readonly GameObject[,] foodNeedMarkers = new GameObject[7, 7];
        private readonly GameObject[,] workNeedMarkers = new GameObject[7, 7];

        private ConstructionRunModel run;
        private ConstructionRunStore store;
        private ConstructionCategory selectedCategory = ConstructionCategory.Green;
        private GreenPlanting selectedPlanting = GreenPlanting.Oak;
        private Font regularFont;
        private Font boldFont;
        private Text dayText;
        private Text scoreText;
        private Text dayReviewText;
        private Text storyText;
        private Text feedbackText;
        private Text progressText;
        private Text resetLabel;
        private Text routeText;
        private RectTransform routeOverlay;
        private RouteSubject routeSubject;
        private string selectedHomeId;
        private GameObject plantingPanel;
        private GameObject visitorMarker;
        private GameObject movingResidentMarker;
        private bool confirmReset;
        private Coroutine growthAnimation;
        private Coroutine residentMealAnimation;

        public int CurrentDay => run?.CurrentDay ?? 0;
        public int BuiltCount => run?.BuiltCount ?? 0;
        public int ObservedAnimalMeals => run?.AnimalScore ?? 0;
        public int ObservedHumanWorkCycles => run?.HumanScore ?? 0;
        public ConstructionStoryStage Stage => run?.StoryStage ??
            ConstructionStoryStage.WatchSquirrelEat;

        // Allows PlayMode tests to exercise the screen without touching the
        // user's persistent save file.
        public void UseRunForTesting(ConstructionRunModel testRun) => run = testRun;

        private void Start()
        {
            if (run == null)
            {
                store = new ConstructionRunStore(ConstructionRunStore.DefaultPath);
                if (!store.TryLoad(out run)) run = new ConstructionRunModel();
            }
            regularFont = Resources.Load<Font>("Fonts/Nunito-Regular");
            boldFont = Resources.Load<Font>("Fonts/Nunito-Bold") ?? regularFont;
            EnsureCameraAndInput();
            BuildUi();
            Refresh();
        }

        public bool TrySelect(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            ClearResetConfirmation();
            if (run == null || !run.IsUnlocked(category)) return false;
            selectedCategory = category;
            if (category == ConstructionCategory.Green && planting != GreenPlanting.None)
                selectedPlanting = planting;
            Refresh();
            return true;
        }

        public bool TryPlace(int column, int row)
        {
            ClearResetConfirmation();
            if (run == null) return false;
            var existing = run.At(column, row);
            if (existing != null)
            {
                routeSubject = existing.category == ConstructionCategory.Residence
                    ? RouteSubject.Resident
                    : existing.id == run.PigeonTileId ? RouteSubject.Pigeons
                    : existing.id == ConstructionBoardModel.StarterOakId
                        ? RouteSubject.Squirrel : RouteSubject.None;
                selectedHomeId = routeSubject == RouteSubject.Resident
                    ? existing.id : null;
                feedbackText.text = existing.category == ConstructionCategory.Green
                    ? $"{existing.planting} · growth {run.GreenGrowthStage(existing.id)}/2 · " +
                      $"food {run.FoodStock(existing.id)}/{run.FoodCapacity(existing.planting)} " +
                      $"· potential +{run.GreenDailyYield(existing.id)}/day"
                    : $"{existing.category} · built day {existing.builtDay}";
                if (existing.category == ConstructionCategory.Green &&
                    run.LastEcologyDay?.foodChanges != null)
                    foreach (var change in run.LastEcologyDay.foodChanges)
                        if (change.tileId == existing.id)
                        {
                            feedbackText.text +=
                                $" · last day +{change.produced}, eaten {change.eaten}";
                            break;
                        }
                if (existing.id == run.PigeonTileId)
                    feedbackText.text += " · a pigeon flock arrived here";
                if (existing.category == ConstructionCategory.Residence)
                {
                    var person = run.LastHumanDay?.residents.Find(item =>
                        item.homeTileId == existing.id);
                    feedbackText.text += person == null
                        ? " · 1 resident · daily route not settled yet"
                        : $" · last day work {(person.worked ? 1 : 0)}/1, " +
                          $"meal {(person.ate ? 1 : 0)}/1, " +
                          $"complete {(person.completedCycle ? 1 : 0)}/1";
                }
                if (existing.category is ConstructionCategory.Residence or
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket)
                    feedbackText.text += $" · waste {run.WasteBacklog(existing.id)}/" +
                        $"{ConstructionWasteModel.DisruptionThreshold} (cleanup before next route)";
                var capacity = ConstructionHumanModel.DailyCapacity(existing.category);
                if (capacity > 0 && existing.category != ConstructionCategory.Residence)
                {
                    var expected = run.PreviewHumanDay().residents.Count(person =>
                        person.workTileId == existing.id ||
                        person.mealTileId == existing.id);
                    feedbackText.text +=
                        $" · next day ~{expected}/{capacity} capacity used";
                }
                Refresh();
                return false;
            }
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (!run.TryBuild(column, row, selectedCategory, planting,
                    out var built, out var failure, out var placement))
            {
                feedbackText.text = failure == ConstructionBuildFailure.LockedByStory
                    ? "Follow the story to unlock this building."
                    : PlacementMessage(placement);
                Refresh();
                return false;
            }
            feedbackText.text = built.id == run.FirstResidentHomeTileId
                ? "One resident moved in. A reachable restaurant is their next need."
                : built.category == ConstructionCategory.Restaurant
                    ? run.FirstResidentHasRestaurantAccess
                        ? "Restaurant connected. Advance one day to see the resident eat."
                        : "Restaurant built, but the first resident cannot reach it."
                    : $"Built {TileLabel(built)} at {column + 1},{row + 1}.";
            Save();
            Refresh();
            return true;
        }

        public bool AdvanceDay()
        {
            ClearResetConfirmation();
            if (run == null || dayText == null || run.IsFinished) return false;
            if (growthAnimation != null) StopCoroutine(growthAnimation);
            if (residentMealAnimation != null)
            {
                StopCoroutine(residentMealAnimation);
                residentMealAnimation = null;
                movingResidentMarker.SetActive(false);
            }
            var sizesBefore = new Vector2[7, 7];
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
                sizesBefore[column, row] = cellIconRects[column, row].sizeDelta;
            var openingMeal = run.StoryStage == ConstructionStoryStage.WatchSquirrelEat;
            var pigeonsBefore = run.PigeonTileId;
            var residentAteBefore = run.FirstResidentAte;
            var residentMealRoute = run.StoryStage ==
                ConstructionStoryStage.WatchResidentEat
                ? run.FirstResidentRestaurantRoute
                : Array.Empty<ConstructionTileData>();
            if (!run.TrySimulateDay(out var ecology)) return false;
            var human = run.LastHumanDay;
            feedbackText.text = openingMeal && ecology.squirrelAte
                ? "The squirrel ate one oak nut. Green space is now available."
                : pigeonsBefore == null && run.PigeonTileId != null
                    ? "A flock arrived. It will forage from the next day."
                : !residentAteBefore && run.FirstResidentAte
                    ? "The resident reached the restaurant. Workshop unlocked."
                : human?.restaurantRejectedForCapacity > 0 &&
                  human.MealsEaten < human.ResidentCount
                    ? $"Restaurant full: {human.ResidentCount - human.MealsEaten} " +
                      $"resident(s) missed food. Workdays " +
                      $"{human.CompletedWorkCycles}/{human.ResidentCount}."
                : human?.restaurantRejectedForWaste > 0
                    ? $"Waste blocked {human.restaurantRejectedForWaste} restaurant " +
                      $"meal(s). Cleanup {run.LastWasteDay?.Cleared ?? 0}; " +
                      $"backlog {run.LastWasteDay?.Remaining ?? 0}."
                : $"Animal meals {ecology.AnimalMeals}; residents " +
                  $"worked {human?.WorkedCount ?? 0}, ate {human?.MealsEaten ?? 0}, " +
                  $"finished {human?.CompletedWorkCycles ?? 0}.";
            Save();
            Refresh();
            var newPigeonFlock = pigeonsBefore == null && run.PigeonTileId != null;
            if (newPigeonFlock)
                foreach (var tile in run.BuiltTiles)
                    if (tile.id == run.PigeonTileId)
                    {
                        pigeonIcons[tile.column, tile.row].rectTransform.localScale =
                            Vector3.one * 0.2f;
                        break;
                    }
            growthAnimation = StartCoroutine(AnimateDailyGrowth(sizesBefore,
                newPigeonFlock));
            if (!residentAteBefore && run.FirstResidentAte)
                residentMealAnimation = StartCoroutine(
                    AnimateResidentMeal(residentMealRoute));
            return true;
        }

        private IEnumerator AnimateResidentMeal(
            IReadOnlyList<ConstructionTileData> route)
        {
            if (route == null || route.Count < 2)
            {
                residentMealAnimation = null;
                yield break;
            }
            var home = route[0];
            var stationary = residentMarkers[home.column, home.row];
            var marker = movingResidentMarker.GetComponent<RectTransform>();
            var points = new Vector2[route.Count];
            for (var index = 0; index < route.Count; index++)
                points[index] = new Vector2(23 + (route[index].column - 3) * 79,
                    -22 + (3 - route[index].row) * 79);
            marker.anchoredPosition = points[0];
            stationary.SetActive(false);
            movingResidentMarker.SetActive(true);
            yield return null;
            for (var index = 1; index < points.Length; index++)
                yield return MoveResident(marker, points[index - 1], points[index]);
            yield return new WaitForSecondsRealtime(0.12f);
            for (var index = points.Length - 1; index > 0; index--)
                yield return MoveResident(marker, points[index], points[index - 1]);
            movingResidentMarker.SetActive(false);
            stationary.SetActive(true);
            residentMealAnimation = null;
        }

        private static IEnumerator MoveResident(RectTransform marker, Vector2 from,
            Vector2 to)
        {
            const float duration = 0.32f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                marker.anchoredPosition = Vector2.Lerp(from, to,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            marker.anchoredPosition = to;
        }

        private IEnumerator AnimateDailyGrowth(Vector2[,] sizesBefore, bool newPigeonFlock)
        {
            var targets = new Vector2[7, 7];
            Image arrivingPigeon = null;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                targets[column, row] = cellIconRects[column, row].sizeDelta;
                var tile = run.At(column, row);
                if (tile?.category == ConstructionCategory.Green)
                    cellIconRects[column, row].sizeDelta = sizesBefore[column, row];
                if (newPigeonFlock && tile?.id == run.PigeonTileId)
                {
                    arrivingPigeon = pigeonIcons[column, row];
                    arrivingPigeon.rectTransform.localScale = Vector3.one * 0.2f;
                }
            }
            // Keep the first frame visibly small, even when the first batchmode
            // frame reports a very large unscaled delta time.
            yield return null;
            const float duration = 0.45f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var eased = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                for (var row = 0; row < 7; row++)
                for (var column = 0; column < 7; column++)
                    if (run.At(column, row)?.category == ConstructionCategory.Green)
                        cellIconRects[column, row].sizeDelta = Vector2.Lerp(
                            sizesBefore[column, row], targets[column, row], eased);
                if (arrivingPigeon != null)
                    arrivingPigeon.rectTransform.localScale = Vector3.one * eased;
                yield return null;
            }
            if (arrivingPigeon != null)
                arrivingPigeon.rectTransform.localScale = Vector3.one;
            growthAnimation = null;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Construction UI", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = Panel("Paper background", canvas.transform,
                Vector2.zero, Vector2.zero, Paper);
            Stretch(background.rectTransform);

            TextAt("Title", canvas.transform, new Vector2(-480, 410),
                new Vector2(590, 48), "SYMBIOSIS · BUILD", 32, true, Ink,
                TextAnchor.MiddleLeft);
            dayText = TextAt("Day", canvas.transform, new Vector2(205, 413),
                new Vector2(240, 44), "", 25, true, Ink, TextAnchor.MiddleRight);
            var reset = ButtonAt("Start a new construction run", canvas.transform,
                new Vector2(635, 412), new Vector2(215, 45), "New run", 19,
                Ink, Paper, () => OnResetPressed());
            resetLabel = reset.GetComponentInChildren<Text>();

            var boardPanel = Panel("7 by 7 construction board", canvas.transform,
                new Vector2(-285, -22), new Vector2(595, 595), Ink);
            Panel("Board inset", boardPanel.transform, Vector2.zero,
                new Vector2(574, 574), new Color(0.79f, 0.75f, 0.66f));
            BuildCells(boardPanel.transform);
            routeOverlay = NewRect("Selected route overlay", boardPanel.transform,
                Vector2.zero, new Vector2(595, 595));
            movingResidentMarker = CreateHumanMarker(boardPanel.transform,
                Vector2.zero, 0.8f, "Resident walking");
            movingResidentMarker.SetActive(false);
            var routePanel = Panel("Route inspection", canvas.transform,
                new Vector2(-285, -357), new Vector2(595, 68), Ink);
            routeText = TextAt("Route status", routePanel.transform,
                Vector2.zero, new Vector2(565, 62), "", 16, false,
                Color.white, TextAnchor.MiddleLeft);

            var side = Panel("Building choices", canvas.transform,
                new Vector2(455, -23), new Vector2(555, 650),
                new Color(0.97f, 0.94f, 0.85f));
            visitorMarker = CreateHumanMarker(side.transform,
                new Vector2(-228, 268), 1.3f, "Waiting visitor");
            storyText = TextAt("Story instruction", side.transform,
                new Vector2(25, 268), new Vector2(455, 84), "", 21, true,
                Ink, TextAnchor.MiddleLeft);
            scoreText = TextAt("Observed outcomes", side.transform,
                new Vector2(0, 208), new Vector2(505, 46), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            BuildCategoryButtons(side.transform);
            var review = Panel("Last day review", side.transform,
                new Vector2(0, -127), new Vector2(505, 35), Ink);
            dayReviewText = TextAt("Day review text", review.transform,
                Vector2.zero, new Vector2(487, 33), "", 14, false,
                Color.white, TextAnchor.MiddleLeft);
            BuildPlantingButtons(side.transform);
            feedbackText = TextAt("Feedback", side.transform,
                new Vector2(0, -205), new Vector2(505, 70), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            progressText = TextAt("Board progress", side.transform,
                new Vector2(-95, -269), new Vector2(310, 42), "", 20, true,
                Ink, TextAnchor.MiddleLeft);
            ButtonAt("Advance to next day", side.transform,
                new Vector2(150, -269), new Vector2(194, 50), "Next day  →", 20,
                Teal, Color.white, () => AdvanceDay());

            TextAt("Prototype notice", canvas.transform,
                new Vector2(-180, -415), new Vector2(1130, 30),
                "Construction vertical slice · existing full-board game remains separate",
                15, false, Locked, TextAnchor.MiddleCenter);
        }

        private void BuildCells(Transform parent)
        {
            const float spacing = 79f;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var x = column;
                var y = row;
                var button = ButtonAt($"Cell {column + 1},{row + 1}", parent,
                    new Vector2((column - 3) * spacing, (3 - row) * spacing),
                    new Vector2(72, 72), "", 1, Paper, Ink,
                    () => TryPlace(x, y));
                cellBackgrounds[column, row] = button.GetComponent<Image>();
                var icon = Panel("Room icon", button.transform,
                    new Vector2(0, 4), new Vector2(47, 47), Color.white);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                cellIcons[column, row] = icon;
                cellIconRects[column, row] = icon.rectTransform;
                var squirrel = Panel("Resident squirrel", button.transform,
                    new Vector2(23, -22), new Vector2(31, 31), Color.white);
                squirrel.sprite = LoadSprite("UI/GameplayHud/population-squirrel-v02");
                squirrel.raycastTarget = false;
                squirrel.preserveAspect = true;
                squirrelIcons[column, row] = squirrel;
                var pigeon = Panel("Pigeon flock", button.transform,
                    new Vector2(-23, -22), new Vector2(31, 31), Color.white);
                pigeon.sprite = LoadSprite("UI/GameplayHud/population-pigeon-v02");
                pigeon.raycastTarget = false;
                pigeon.preserveAspect = true;
                pigeonIcons[column, row] = pigeon;
                var foodBadge = Panel("Food stock", button.transform,
                    new Vector2(23, 24), new Vector2(35, 19), Ink);
                foodBadge.raycastTarget = false;
                var foodLabel = TextAt("Food count", foodBadge.transform,
                    Vector2.zero, new Vector2(34, 18), "0", 12, true,
                    Color.white, TextAnchor.MiddleCenter);
                foodLabel.raycastTarget = false;
                foodBadges[column, row] = foodLabel;
                var capacityBadge = Panel("Daily capacity", button.transform,
                    new Vector2(23, 24), new Vector2(35, 19), Ink);
                capacityBadge.raycastTarget = false;
                var capacityLabel = TextAt("Capacity count", capacityBadge.transform,
                    Vector2.zero, new Vector2(34, 18), "0/1", 10, true,
                    Color.white, TextAnchor.MiddleCenter);
                capacityLabel.raycastTarget = false;
                capacityBadges[column, row] = capacityLabel;
                var wasteBadge = Panel("Waste backlog", button.transform,
                    new Vector2(-23, -25), new Vector2(31, 19),
                    new Color(0.68f, 0.35f, 0.25f));
                wasteBadge.raycastTarget = false;
                var wasteLabel = TextAt("Waste count", wasteBadge.transform,
                    Vector2.zero, new Vector2(30, 18), "0", 11, true,
                    Color.white, TextAnchor.MiddleCenter);
                wasteLabel.raycastTarget = false;
                wasteBadges[column, row] = wasteLabel;
                residentMarkers[column, row] = CreateHumanMarker(button.transform,
                    new Vector2(23, -22), 0.8f, "First resident");
                var foodNeed = Panel("Food access needed", button.transform,
                    new Vector2(25, 24), new Vector2(20, 20),
                    new Color(0.77f, 0.37f, 0.23f));
                foodNeed.raycastTarget = false;
                TextAt("Need marker", foodNeed.transform, Vector2.zero,
                    new Vector2(20, 20), "!", 17, true, Color.white,
                    TextAnchor.MiddleCenter).raycastTarget = false;
                foodNeedMarkers[column, row] = foodNeed.gameObject;
                var workNeed = Panel("Work access needed", button.transform,
                    new Vector2(-25, 24), new Vector2(20, 20),
                    new Color(0.65f, 0.40f, 0.20f));
                workNeed.raycastTarget = false;
                TextAt("Work marker", workNeed.transform, Vector2.zero,
                    new Vector2(20, 20), "W", 13, true, Color.white,
                    TextAnchor.MiddleCenter).raycastTarget = false;
                workNeedMarkers[column, row] = workNeed.gameObject;
            }
        }

        private void BuildCategoryButtons(Transform parent)
        {
            var categories = new[]
            {
                ConstructionCategory.Green, ConstructionCategory.Residence,
                ConstructionCategory.Restaurant, ConstructionCategory.Workshop,
                ConstructionCategory.Waste, ConstructionCategory.Street,
                ConstructionCategory.Supermarket, ConstructionCategory.Square
            };
            for (var index = 0; index < categories.Length; index++)
            {
                var category = categories[index];
                var column = index % 2;
                var row = index / 2;
                var button = ButtonAt($"Build {category}", parent,
                    new Vector2(column == 0 ? -128 : 128, 131 - row * 69),
                    new Vector2(239, 59), category.ToString(), 18,
                    Ink, Color.white, () => TrySelect(category));
                var icon = Panel("Building icon", button.transform,
                    new Vector2(-93, 0), new Vector2(39, 39), Color.white);
                icon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(category,
                    GreenPlanting.Oak));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition = new Vector2(17, 0);
                label.rectTransform.sizeDelta = new Vector2(175, 48);
                categoryButtons.Add(category, button);
            }
        }

        private void BuildPlantingButtons(Transform parent)
        {
            plantingPanel = new GameObject("Green planting", typeof(RectTransform));
            plantingPanel.transform.SetParent(parent, false);
            SetRect(plantingPanel.GetComponent<RectTransform>(),
                new Vector2(0, -168), new Vector2(505, 44));
            var plantings = new[]
                { GreenPlanting.Oak, GreenPlanting.Meadow, GreenPlanting.Shrub };
            for (var index = 0; index < plantings.Length; index++)
            {
                var planting = plantings[index];
                var button = ButtonAt($"Plant {planting}", plantingPanel.transform,
                    new Vector2((index - 1) * 170, 0), new Vector2(159, 39),
                    planting.ToString(), 16, Ink, Color.white,
                    () => TrySelect(ConstructionCategory.Green, planting));
                plantingButtons.Add(planting, button);
            }
        }

        private void Refresh()
        {
            if (run == null || dayText == null) return;
            var humanPreview = run.PreviewHumanDay();
            dayText.text = $"DAY {run.CurrentDay}";
            scoreText.text = $"Animal meals {run.AnimalScore} · Workdays {run.HumanScore}\n" +
                $"Residents {run.BuiltTiles.Count(tile => tile.category == ConstructionCategory.Residence)}" +
                " · ~ means next-day forecast";
            RefreshDayReview();
            storyText.text = StoryInstruction(run);
            visitorMarker.SetActive(run.StoryStage ==
                ConstructionStoryStage.WelcomeResident);
            progressText.text = $"{run.BuiltCount} / 49 cells";
            plantingPanel.SetActive(selectedCategory == ConstructionCategory.Green &&
                run.IsUnlocked(ConstructionCategory.Green));

            foreach (var pair in categoryButtons)
            {
                var unlocked = run.IsUnlocked(pair.Key) && !run.IsFinished;
                pair.Value.interactable = unlocked;
                pair.Value.GetComponent<Image>().color = !unlocked ? Locked :
                    pair.Key == selectedCategory ? Teal : Ink;
            }
            foreach (var pair in plantingButtons)
                pair.Value.GetComponent<Image>().color = pair.Key == selectedPlanting
                    ? Teal : Ink;

            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var tile = run.At(column, row);
                var icon = cellIcons[column, row];
                icon.gameObject.SetActive(tile != null);
                foodBadges[column, row].transform.parent.gameObject.SetActive(
                    tile?.category == ConstructionCategory.Green);
                if (tile?.category == ConstructionCategory.Green)
                    foodBadges[column, row].text =
                        run.FoodStock(tile.id).ToString();
                var capacity = tile == null ? 0 :
                    ConstructionHumanModel.DailyCapacity(tile.category);
                var isService = capacity > 0 &&
                    tile.category != ConstructionCategory.Residence;
                capacityBadges[column, row].transform.parent.gameObject.SetActive(isService);
                if (isService)
                {
                    var used = humanPreview.residents.Count(person =>
                        person.workTileId == tile.id || person.mealTileId == tile.id);
                    capacityBadges[column, row].text = $"~{used}/{capacity}";
                }
                var wasteSource = tile?.category is ConstructionCategory.Residence or
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket;
                var backlog = wasteSource ? run.WasteBacklog(tile.id) : 0;
                wasteBadges[column, row].transform.parent.gameObject.SetActive(
                    backlog > 0);
                if (backlog > 0) wasteBadges[column, row].text = backlog.ToString();
                squirrelIcons[column, row].gameObject.SetActive(tile?.id ==
                    ConstructionBoardModel.StarterOakId);
                pigeonIcons[column, row].gameObject.SetActive(tile != null &&
                    tile.id == run.PigeonTileId);
                var isHome = tile?.category == ConstructionCategory.Residence;
                residentMarkers[column, row].SetActive(isHome &&
                    (movingResidentMarker == null || !movingResidentMarker.activeSelf));
                var forecastResident = isHome ? humanPreview.residents.Find(
                    person => person.homeTileId == tile.id) : null;
                foodNeedMarkers[column, row].SetActive(isHome &&
                    (forecastResident == null || !forecastResident.ate ||
                     tile.id == run.FirstResidentHomeTileId &&
                     run.StoryStage == ConstructionStoryStage.WatchResidentEat));
                workNeedMarkers[column, row].SetActive(isHome &&
                    run.IsUnlocked(ConstructionCategory.Workshop) &&
                    (forecastResident == null || !forecastResident.worked));
                pigeonIcons[column, row].rectTransform.localScale = Vector3.one;
                cellBackgrounds[column, row].color = tile == null
                    ? new Color(0.92f, 0.90f, 0.83f) : TileColor(tile.category);
                if (tile == null) continue;
                icon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(tile.category,
                    tile.planting));
                icon.color = Color.white;
                var size = tile.category == ConstructionCategory.Green &&
                           tile.id != ConstructionBoardModel.StarterOakId
                    ? 29f + run.GreenGrowthStage(tile.id) * 9f : 47f;
                cellIconRects[column, row].sizeDelta = new Vector2(size, size);
            }
            RefreshRouteInspection(humanPreview);
        }

        private void RefreshDayReview()
        {
            var ecology = run.LastEcologyDay;
            var human = run.LastHumanDay;
            var waste = run.LastWasteDay;
            var forecast = run.LastForecast;
            if (ecology == null || human == null || waste == null)
            {
                dayReviewText.text = "LAST DAY · No simulated result yet.";
                dayReviewText.color = Color.white;
                return;
            }
            if (forecast == null)
            {
                dayReviewText.text = $"DAY {ecology.day} · Animal {ecology.AnimalMeals} " +
                    $"· Work {human.CompletedWorkCycles}\nForecast not saved in this older run.";
                dayReviewText.color = Color.white;
                return;
            }
            var matches = ecology.AnimalMeals == forecast.animalMeals &&
                human.CompletedWorkCycles == forecast.completedWorkCycles &&
                waste.Cleared == forecast.wasteCleared;
            var issue = DayIssue(ecology, human, forecast);
            dayReviewText.text = $"D{ecology.day}  Animal {ecology.AnimalMeals}/" +
                $"{forecast.animalMeals}  Work {human.CompletedWorkCycles}/" +
                $"{forecast.completedWorkCycles}  Clean {waste.Cleared}/" +
                $"{forecast.wasteCleared}\nActual/forecast · " +
                (matches ? issue : "Forecast differed from actual; inspect result.");
            dayReviewText.color = matches && issue == "No unmet need observed."
                ? Color.white : new Color(1f, 0.79f, 0.57f);
        }

        private string DayIssue(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human, ConstructionDayForecast forecast)
        {
            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            if (forecast.blockedHomeTileIds.Count > 0)
            {
                var location = TileCoordinate(built, forecast.blockedHomeTileIds[0]);
                return $"Waste blocked home {location}.";
            }
            if (forecast.blockedFoodTileIds.Count > 0 &&
                human.MealsEaten < human.ResidentCount)
            {
                var location = TileCoordinate(built, forecast.blockedFoodTileIds[0]);
                return $"Waste closed food at {location}.";
            }
            if (human.restaurantRejectedForCapacity > 0)
                return $"Restaurant full for {human.restaurantRejectedForCapacity}.";
            if (human.CompletedWorkCycles < human.ResidentCount)
                return $"{human.ResidentCount - human.CompletedWorkCycles} " +
                    "resident(s) missed work or food.";
            if (!ecology.squirrelAte)
                return "Squirrel found no oak food.";
            if (run.PigeonArrivalDay > 0 && run.PigeonArrivalDay <= ecology.day &&
                ecology.pigeonsFed < ConstructionFoodModel.PigeonFlockSize)
                return $"Pigeons ate {ecology.pigeonsFed}/" +
                    $"{ConstructionFoodModel.PigeonFlockSize}; meadow food short.";
            return "No unmet need observed.";
        }

        private void RefreshRouteInspection(ConstructionHumanDayResult humanPreview)
        {
            foreach (Transform child in routeOverlay)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            if (routeSubject == RouteSubject.None)
            {
                routeText.text = "Click a resident, squirrel or pigeon flock to inspect its next-day route.";
                return;
            }
            if (run.IsFinished)
            {
                routeText.text = "The board is complete. There is no next-day route to preview.";
                return;
            }
            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            IReadOnlyList<ConstructionTileData> route;
            ConstructionTileData origin;
            var colour = routeSubject == RouteSubject.Resident ? Teal :
                new Color(0.94f, 0.68f, 0.23f);
            if (routeSubject == RouteSubject.Resident)
            {
                var resident = humanPreview.residents.FirstOrDefault(person =>
                    person.homeTileId == selectedHomeId);
                if (resident == null || !built.TryGetValue(selectedHomeId,
                        out origin))
                {
                    routeSubject = RouteSubject.None;
                    routeText.text = "Select a resident to inspect its next-day route.";
                    return;
                }
                route = resident.routeTileIds.Where(built.ContainsKey)
                    .Select(id => built[id]).ToArray();
                var availability = run.PreviewWasteAvailability();
                if (availability.BlockedHomes.Contains(selectedHomeId))
                    routeText.text = "NEXT DAY · RESIDENT\nWaste at home blocks the outing. Build cleanup nearby.";
                else
                {
                    var work = resident.worked
                        ? $"work {TileCoordinate(built, resident.workTileId)}" :
                        run.ResidenceCanReachWork(selectedHomeId)
                            ? "work full" : "no work route";
                    var meal = resident.ate
                        ? $"meal {TileCoordinate(built, resident.mealTileId)}" :
                        !run.ResidenceCanReachFood(selectedHomeId)
                            ? "no meal route"
                            : ReachableFoodClosedByWaste(selectedHomeId,
                                availability.BlockedFoodServices)
                                ? "meal closed by waste" : "meal full";
                    routeText.text = $"NEXT DAY · RESIDENT\n{work} · {meal} · returns home";
                }
            }
            else
            {
                var ecology = run.PreviewEcologyDay();
                var squirrel = routeSubject == RouteSubject.Squirrel;
                var originId = squirrel ? ConstructionBoardModel.StarterOakId :
                    run.PigeonTileId;
                if (originId == null || !built.TryGetValue(originId, out origin))
                {
                    routeSubject = RouteSubject.None;
                    routeText.text = "No animal is currently here.";
                    return;
                }
                route = squirrel ? run.PreviewSquirrelFoodRoute() :
                    run.PreviewPigeonFoodRoute();
                var target = route.Count > 0 ? route[route.Count - 1] : null;
                routeText.text = squirrel
                    ? target == null
                        ? "NEXT DAY · SQUIRREL\nNo oak food within one cell after growth."
                        : $"NEXT DAY · SQUIRREL\n1 portion expected at oak {target.column + 1},{target.row + 1}."
                    : target == null
                        ? "NEXT DAY · PIGEON FLOCK\nNo reachable seeded meadow after growth."
                        : $"NEXT DAY · PIGEON FLOCK\n{ecology.pigeonsFed}/" +
                          $"{ConstructionFoodModel.PigeonFlockSize} portions expected " +
                          $"at meadow {target.column + 1},{target.row + 1}.";
            }
            DrawRoute(route, origin, colour);
        }

        private bool ReachableFoodClosedByWaste(string homeId,
            IReadOnlyCollection<string> blockedServices)
        {
            var reachable = run.BuiltTiles.Where(tile => tile.category is
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket &&
                run.ResidenceRouteTo(homeId, tile.id).Count > 0).ToArray();
            return reachable.Length > 0 && reachable.All(tile =>
                blockedServices.Contains(tile.id));
        }

        private void DrawRoute(IReadOnlyList<ConstructionTileData> route,
            ConstructionTileData origin, Color colour)
        {
            var seen = new HashSet<string>();
            for (var index = 1; index < route.Count; index++)
            {
                var first = route[index - 1];
                var second = route[index];
                if (Math.Abs(first.column - second.column) +
                    Math.Abs(first.row - second.row) != 1) continue;
                var edge = string.CompareOrdinal(first.id, second.id) < 0
                    ? first.id + ":" + second.id : second.id + ":" + first.id;
                if (!seen.Add(edge)) continue;
                var from = BoardPoint(first);
                var to = BoardPoint(second);
                var segment = Panel("Route segment", routeOverlay,
                    (from + to) / 2f,
                    new Vector2(Vector2.Distance(from, to), 5f), colour);
                segment.raycastTarget = false;
                segment.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            }
            var marker = Panel("Route origin", routeOverlay,
                BoardPoint(origin), new Vector2(14, 14), colour);
            marker.raycastTarget = false;
            if (route.Count > 0 && route[route.Count - 1].id != origin.id)
            {
                var target = Panel("Food destination", routeOverlay,
                    BoardPoint(route[route.Count - 1]), new Vector2(20, 20), colour);
                target.raycastTarget = false;
            }
        }

        private static Vector2 BoardPoint(ConstructionTileData tile) =>
            new((tile.column - 3) * 79f, (3 - tile.row) * 79f);

        private static string TileCoordinate(
            IReadOnlyDictionary<string, ConstructionTileData> built, string tileId) =>
            tileId != null && built.TryGetValue(tileId, out var tile)
                ? $"{tile.column + 1},{tile.row + 1}" : "?";

        private void OnResetPressed()
        {
            if (!confirmReset)
            {
                confirmReset = true;
                resetLabel.text = "Confirm new run";
                feedbackText.text = "Press again to erase this construction save.";
                return;
            }
            confirmReset = false;
            resetLabel.text = "New run";
            if (store != null && !store.TryDelete())
            {
                feedbackText.text = "Could not clear the save; run was not reset.";
                return;
            }
            if (growthAnimation != null)
            {
                StopCoroutine(growthAnimation);
                growthAnimation = null;
            }
            if (residentMealAnimation != null)
            {
                StopCoroutine(residentMealAnimation);
                residentMealAnimation = null;
            }
            movingResidentMarker.SetActive(false);
            run = new ConstructionRunModel();
            routeSubject = RouteSubject.None;
            selectedHomeId = null;
            selectedCategory = ConstructionCategory.Green;
            selectedPlanting = GreenPlanting.Oak;
            feedbackText.text = "New run: one oak and one squirrel.";
            Refresh();
        }

        private void ClearResetConfirmation()
        {
            if (!confirmReset) return;
            confirmReset = false;
            if (resetLabel != null) resetLabel.text = "New run";
        }

        private void Save()
        {
            if (store != null && !store.TrySave(run))
                feedbackText.text = "Save failed. Current changes may be lost on exit.";
        }

        private static string StoryInstruction(ConstructionRunModel run) => run.StoryStage switch
        {
            ConstructionStoryStage.WatchSquirrelEat =>
                "A squirrel lives in the centre oak. Advance one day to see where its food comes from.",
            ConstructionStoryStage.GrowGreen =>
                run.HasConnectedMeadow
                    ? "A connected meadow is ready. Advance one day to welcome a pigeon flock."
                    : "Plant a meadow beside the oak or connected green. This can attract pigeons.",
            ConstructionStoryStage.WelcomeResident =>
                "Pigeons have arrived. One visitor needs a home. Build a residence.",
            ConstructionStoryStage.BuildRestaurant =>
                "One resident has moved in. Put a restaurant within reach of their home.",
            ConstructionStoryStage.WatchResidentEat =>
                "The restaurant is reachable. Advance one day to watch the resident eat.",
            ConstructionStoryStage.BuildWorkshop =>
                "The resident ate once. Food needs work to sustain it; build a workshop.",
            ConstructionStoryStage.BuildWasteRoom =>
                "Homes and meals make waste. Build a waste room to clear two nearby portions each day.",
            ConstructionStoryStage.AwaitAccessProblem =>
                "Add another home beyond direct work or food access. A street can reconnect it.",
            ConstructionStoryStage.BuildStreet => "A street extends the human connection from a home.",
            ConstructionStoryStage.AwaitRestaurantCrowding =>
                "Add another residence, then advance a day. Can the restaurant feed everyone?",
            ConstructionStoryStage.BuildSupermarket =>
                "Build a supermarket for people and an additional animal food source.",
            ConstructionStoryStage.FreeBuild => "Choose how to use the remaining land.",
            _ => "All 49 cells are built. The final day has been counted."
        };

        private static string PlacementMessage(ConstructionPlacementFailure failure) => failure switch
        {
            ConstructionPlacementFailure.Disconnected => "Build beside an existing cell, not diagonally.",
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet =>
                "This building needs a home or a street connected to one.",
            ConstructionPlacementFailure.NeedsHomeOrRestaurant =>
                "A waste room must touch a home or restaurant.",
            ConstructionPlacementFailure.StreetNeedsHomeConnection =>
                "A street must extend from a home or connected street.",
            ConstructionPlacementFailure.Occupied => "That cell is already built.",
            _ => "This cell cannot be built here."
        };

        private static string TileLabel(ConstructionTileData tile) =>
            tile.category == ConstructionCategory.Green
                ? $"{tile.planting} green" : tile.category.ToString();

        private static Color TileColor(ConstructionCategory category) => category switch
        {
            ConstructionCategory.Green => Green,
            ConstructionCategory.Street => new Color(0.48f, 0.55f, 0.56f),
            ConstructionCategory.Square => new Color(0.64f, 0.68f, 0.57f),
            ConstructionCategory.Residence => new Color(0.77f, 0.60f, 0.45f),
            ConstructionCategory.Workshop => new Color(0.48f, 0.48f, 0.62f),
            ConstructionCategory.Restaurant => new Color(0.74f, 0.48f, 0.37f),
            ConstructionCategory.Supermarket => new Color(0.48f, 0.63f, 0.66f),
            _ => new Color(0.57f, 0.56f, 0.51f)
        };

        private GameObject CreateHumanMarker(Transform parent, Vector2 position,
            float scale, string name)
        {
            var marker = NewRect(name, parent, position, new Vector2(30, 34));
            marker.localScale = Vector3.one * scale;
            Panel("Head", marker, new Vector2(0, 10), new Vector2(11, 11), Ink)
                .raycastTarget = false;
            Panel("Body", marker, new Vector2(0, -2), new Vector2(16, 14), Ink)
                .raycastTarget = false;
            Panel("Left leg", marker, new Vector2(-5, -13), new Vector2(5, 9), Ink)
                .raycastTarget = false;
            Panel("Right leg", marker, new Vector2(5, -13), new Vector2(5, 9), Ink)
                .raycastTarget = false;
            return marker.gameObject;
        }

        // Reuse the existing pictograms only. All three plantings remain one
        // Green category in the construction and ecological rules.
        private static RoomType RoomTypeFor(ConstructionCategory category,
            GreenPlanting planting) => category switch
        {
            ConstructionCategory.Green => planting switch
            {
                GreenPlanting.Oak => RoomType.OakHabitat,
                GreenPlanting.Shrub => RoomType.ShrubHabitat,
                _ => RoomType.CentralPark
            },
            ConstructionCategory.Residence => RoomType.Residence,
            ConstructionCategory.Workshop => RoomType.Office,
            ConstructionCategory.Restaurant => RoomType.Canteen,
            ConstructionCategory.Supermarket => RoomType.Supermarket,
            ConstructionCategory.Waste => RoomType.Trash,
            ConstructionCategory.Street => RoomType.Garage,
            _ => RoomType.CommunitySquare
        };

        private void EnsureCameraAndInput()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Construction Camera");
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Paper;
            }
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var input = new GameObject("Construction Input", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                input.transform.SetParent(transform, false);
            }
        }

        private Image Panel(string name, Transform parent, Vector2 position,
            Vector2 size, Color color)
        {
            var rect = NewRect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text TextAt(string name, Transform parent, Vector2 position,
            Vector2 size, string value, int fontSize, bool bold, Color color,
            TextAnchor alignment)
        {
            var rect = NewRect(name, parent, position, size);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = bold ? boldFont : regularFont;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = value;
            return label;
        }

        private Button ButtonAt(string name, Transform parent, Vector2 position,
            Vector2 size, string label, int fontSize, Color background,
            Color foreground, Action onClick)
        {
            var image = Panel(name, parent, position, size, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(label))
                TextAt("Label", button.transform, Vector2.zero, size, label, fontSize,
                    true, foreground, TextAnchor.MiddleCenter).raycastTarget = false;
            return button;
        }

        private static RectTransform NewRect(string name, Transform parent,
            Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(path);
            return texture == null ? null : Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
