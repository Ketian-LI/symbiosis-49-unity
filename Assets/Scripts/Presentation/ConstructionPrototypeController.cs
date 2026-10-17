using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        private const string LanguagePreferenceKey = "symbiosis49.language";
        private const float BoardCellSpacing = 101f;
        private enum RouteSubject { None, Resident, Squirrel, Pigeons }
        private enum TooltipLocation { FollowPointer, BoardInfo, LeftLeaf }

        private static readonly Color Ink = new(0.13f, 0.19f, 0.24f);
        private static readonly Color HeadingInk = new(0.16f, 0.32f, 0.30f);
        private static readonly Color Paper = new(0.93f, 0.89f, 0.79f);
        private static readonly Color Green = new(0.39f, 0.57f, 0.34f);
        private static readonly Color Teal = new(0.26f, 0.63f, 0.60f);
        private static readonly Color Gold = new(0.84f, 0.69f, 0.42f);

        private readonly Dictionary<ConstructionCategory, Button> categoryButtons = new();
        private readonly Dictionary<ConstructionCategory, Text> categoryStatusLabels = new();
        private readonly Dictionary<ConstructionCategory, Text> categoryCountLabels = new();
        private readonly Dictionary<GreenPlanting, Button> plantingButtons = new();
        private readonly Dictionary<Text, bool> textWeights = new();
        private readonly Dictionary<Text, Func<string>> metricCaptions = new();
        private readonly Dictionary<string, Sprite> skinnedSprites = new();
        private readonly Image[,] cellBackgrounds = new Image[7, 7];
        private readonly Text[,] buildableHints = new Text[7, 7];
        private readonly Image[,] placementPreviewFrames = new Image[7, 7];
        private readonly Image[,] placementPreviewIcons = new Image[7, 7];
        private readonly Image[,] cellIcons = new Image[7, 7];
        private readonly RectTransform[,] cellIconRects = new RectTransform[7, 7];
        private readonly Image[,] squirrelIcons = new Image[7, 7];
        private readonly Image[,] pigeonIcons = new Image[7, 7];
        private readonly Text[,] squirrelCounts = new Text[7, 7];
        private readonly Text[,] pigeonCounts = new Text[7, 7];
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
        private Font chineseFont;
        private InterfaceLanguage language = InterfaceLanguage.English;
        private bool languageOverriddenForTesting;
        private Text titleText;
        private Text menuLabel;
        private Button englishLanguageButton;
        private Button chineseLanguageButton;
        private Text englishLanguageLabel;
        private Text chineseLanguageLabel;
        private Text nextDayLabel;
        private Button nextDayButton;
        private Button buildPaletteButton;
        private Text buildPaletteLabel;
        private Text paletteNotice;
        private Image paletteBackdrop;
        private bool buildPaletteOpen;
        private Text resultTitleLabel;
        private Text dayText;
        private Text scoreText;
        private string predationRiskText;
        private Text dayReviewText;
        private Image dayReviewPanel;
        private Text storyText;
        private Text storyPreviewText;
        private Image storyCueIcon;
        private Text todayHeading;
        private Text reviewHeading;
        private Text routeHeading;
        private Text progressHeading;
        private Text nextStepHeading;
        private Text feedbackText;
        private Image feedbackPanel;
        private Text progressText;
        private Text placementGuideText;
        private Text animalMetricText;
        private Text humanMetricText;
        private Text dayAnimalMetricText;
        private Text dayHumanMetricText;
        private Text dayWasteMetricText;
        private Text feedbackPreviewText;
        private Image selectedBuildIcon;
        private RectTransform tooltipRect;
        private RectTransform tooltipCanvas;
        private Image tooltipIcon;
        private Text tooltipTitle;
        private Text tooltipBody;
        private Image guidePanel;
        private Image progressPanel;
        private GameObject placementConfirmation;
        private Image placementConfirmationIcon;
        private Text placementConfirmationTitle;
        private Text placementConfirmationLocation;
        private Text placementConfirmationNotice;
        private Text placementCancelLabel;
        private Text placementConfirmLabel;
        private Button placementConfirmButton;
        private RectTransform tutorialFocus;
        private bool hasPendingPlacement;
        private int pendingColumn;
        private int pendingRow;
        private int pendingDay;
        private ConstructionCategory pendingCategory;
        private GreenPlanting pendingPlanting;
        private Text resetLabel;
        private Text resultResetLabel;
        private Text resultNoticeText;
        private Text finalResultText;
        private GameObject finalResultPanel;
        private Text routeText;
        private Image routePanel;
        private RectTransform routeOverlay;
        private RouteSubject routeSubject;
        private string selectedHomeId;
        private GameObject plantingPanel;
        private GameObject visitorMarker;
        private GameObject movingResidentMarker;
        private bool confirmReset;
        private bool openingOakInspected;
        private Coroutine growthAnimation;
        private Coroutine residentMealAnimation;
        private ConstructionWorldView worldView;
        private int viewMinColumn;
        private int viewMinRow;
        private readonly List<GameObject> panControls = new();
        private readonly List<CanvasGroup> caseHeaderGroups = new();
        private RectTransform leftCaseLeaf;
        private RectTransform rightCaseLeaf;
        private RectTransform leftCaseCover;
        private RectTransform rightCaseCover;
        private CanvasGroup caseInformation;
        private CanvasGroup caseRouteInformation;
        private GameObject caseOpeningBlocker;
        private Coroutine caseOpening;

        public int CurrentDay => run?.CurrentDay ?? 0;
        public ConstructionGameMode Mode => run?.Mode ?? ConstructionGameMode.Fill;
        public int BuiltCount => run?.BuiltCount ?? 0;
        public int ObservedAnimalMeals => run?.AnimalScore ?? 0;
        public int ObservedHumanWorkCycles => run?.HumanScore ?? 0;
        public ConstructionStoryStage Stage => run?.StoryStage ??
            ConstructionStoryStage.WatchSquirrelEat;
        public InterfaceLanguage Language => language;
        public bool IsBoardCaseOpen { get; private set; } = true;
        public bool HasPendingPlacement => hasPendingPlacement;
        private bool Chinese => language == InterfaceLanguage.Chinese;
        private string L(string english, string chinese) => Chinese ? chinese : english;

        public bool ReturnToMainMenu()
        {
            ClearPendingPlacement();
            ClearResetConfirmation();
            if (run == null || !Application.CanStreamedLevelBeLoaded("Main"))
                return false;
            if (store != null && !store.TrySave(run))
            {
                if (feedbackText != null)
                    feedbackText.text = L("Save failed. Stay here and try again.",
                        "存档失败。请留在这里重试。");
                return false;
            }
            return ConstructionSceneTransition.ReturnToMenu();
        }

        // Allows PlayMode tests to exercise the screen without touching the
        // user's persistent save file.
        public void UseRunForTesting(ConstructionRunModel testRun,
            ConstructionRunStore testStore = null)
        {
            ClearPendingPlacement();
            run = testRun;
            store = testStore;
            // Tests must not depend on, or write to, the player's language setting.
            languageOverriddenForTesting = true;
            language = InterfaceLanguage.English;
        }

        public void UseLanguageForTesting(InterfaceLanguage testLanguage)
        {
            languageOverriddenForTesting = true;
            language = testLanguage;
        }

        public void ToggleLanguage()
        {
            SetLanguage(Chinese ? InterfaceLanguage.English : InterfaceLanguage.Chinese);
        }

        private void SetLanguage(InterfaceLanguage choice)
        {
            if (language == choice) return;
            ClearResetConfirmation();
            language = choice;
            if (!languageOverriddenForTesting)
            {
                PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
                PlayerPrefs.Save();
            }
            ApplyLanguageFonts();
            if (feedbackText != null)
                feedbackText.text = L("Language changed to English.", "已切换为中文。");
            Refresh();
        }

        private void Start()
        {
            if (!languageOverriddenForTesting)
                language = (InterfaceLanguage)Mathf.Clamp(PlayerPrefs.GetInt(
                    LanguagePreferenceKey, (int)InterfaceLanguage.English), 0, 1);
            if (run == null)
            {
                var mode = ConstructionLaunchContext.Take();
                store = new ConstructionRunStore(ConstructionRunStore.PathFor(mode));
                if (!store.TryLoad(out run) || run.Mode != mode)
                    run = new ConstructionRunModel(mode);
            }
            regularFont = UrbanFontResolver.GetFont();
            boldFont = Resources.Load<Font>("Fonts/Nunito-Bold") ?? regularFont;
            chineseFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular") ?? regularFont;
            EnsureCameraAndInput();
            BuildUi();
            Refresh();
            worldView?.RevealRestoredRooms();
            if (!IsBoardCaseOpen)
                caseOpening = StartCoroutine(OpenBoardCase());
        }

        public bool TrySelect(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            ClearResetConfirmation();
            if (run == null) return false;
            if (!CanSelectCategory(category))
            {
                if (feedbackText != null)
                    feedbackText.text = L("Finish observing the oak before building.",
                        "先完成橡树观察，再开始建造。");
                return false;
            }
            ClearPendingPlacement();
            selectedCategory = category;
            if (category == ConstructionCategory.Green && planting != GreenPlanting.None)
                selectedPlanting = planting;
            Refresh();
            var available = AvailablePlotCount(category,
                category == ConstructionCategory.Green ? selectedPlanting : GreenPlanting.None);
            feedbackText.text = L(
                $"Selected: {SelectedBuildLabel()} · {available} valid plot(s)." +
                (run.BuiltToday ? " Build tomorrow." : " Choose a + plot to preview."),
                $"已选择：{SelectedBuildLabel()} · 可放 {available} 处。" +
                (run.BuiltToday ? "明天可建。" : "点击＋地块预览。"));
            return true;
        }

        // A board click only stages a placement. The model, daily allowance,
        // score and save are changed solely by ConfirmPendingPlacement.
        public bool RequestPlacement(int column, int row)
        {
            if (run == null) return false;
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (run.At(column, row) != null || run.BuiltToday || run.IsFinished ||
                !CanSelectCategory(selectedCategory) ||
                run.CheckPlacement(column, row, selectedCategory, planting) !=
                    ConstructionPlacementFailure.None)
                return TryPlace(column, row);
            ClearResetConfirmation();
            pendingColumn = column;
            pendingRow = row;
            pendingDay = run.CurrentDay;
            pendingCategory = selectedCategory;
            pendingPlanting = planting;
            hasPendingPlacement = true;
            buildPaletteOpen = false;
            Refresh();
            return true;
        }

        public bool ConfirmPendingPlacement()
        {
            if (!hasPendingPlacement || run == null) return false;
            var column = pendingColumn;
            var row = pendingRow;
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (run.CurrentDay != pendingDay || run.BuiltToday || run.IsFinished ||
                selectedCategory != pendingCategory || planting != pendingPlanting ||
                run.CheckPlacement(column, row, pendingCategory, pendingPlanting) !=
                    ConstructionPlacementFailure.None)
            {
                ClearPendingPlacement();
                feedbackText.text = L("Placement changed. Choose a plot again.",
                    "建造条件已变化，请重新选择地块。");
                Refresh();
                return false;
            }
            ClearPendingPlacement();
            return TryPlace(column, row);
        }

        public void CancelPendingPlacement()
        {
            if (!hasPendingPlacement) return;
            ClearPendingPlacement();
            feedbackText.text = L("Cancelled. Today's build is still available.",
                "已取消，今天仍可建造一格。");
            Refresh();
        }

        private void ClearPendingPlacement()
        {
            hasPendingPlacement = false;
            if (placementConfirmation != null)
                placementConfirmation.SetActive(false);
        }

        public bool TryPlace(int column, int row)
        {
            ClearPendingPlacement();
            ClearResetConfirmation();
            if (run == null) return false;
            var existing = run.At(column, row);
            if (existing != null)
            {
                if (run.CurrentDay == 1 &&
                    existing.id == ConstructionBoardModel.StarterOakId)
                    openingOakInspected = true;
                var inspectedPigeons = run.InspectPigeonMeadow(existing.id);
                routeSubject = existing.category == ConstructionCategory.Residence &&
                               run.HasResident(existing.id)
                    ? RouteSubject.Resident
                    : existing.id == run.PigeonTileId && run.PigeonPopulation > 0
                        ? RouteSubject.Pigeons
                    : existing.id == ConstructionBoardModel.StarterOakId &&
                      run.SquirrelPopulation > 0
                        ? RouteSubject.Squirrel : RouteSubject.None;
                selectedHomeId = routeSubject == RouteSubject.Resident
                    ? existing.id : null;
                feedbackText.text = existing.category == ConstructionCategory.Green
                    ? L($"{PlantingLabel(existing.planting)} · growth {run.GreenGrowthStage(existing.id)}/2 · " +
                        $"food {run.FoodStock(existing.id)}/{run.FoodCapacity(existing.planting)}\n" +
                        $"Yield +{run.GreenDailyYield(existing.id)}/day",
                        $"{PlantingLabel(existing.planting)} · 成长 {run.GreenGrowthStage(existing.id)}/2 · " +
                        $"食物 {run.FoodStock(existing.id)}/{run.FoodCapacity(existing.planting)}\n" +
                        $"日产 +{run.GreenDailyYield(existing.id)}")
                    : L($"{CategoryLabel(existing.category)} · built day {existing.builtDay}",
                        $"{CategoryLabel(existing.category)} · 第 {existing.builtDay} 天建造");
                if (existing.category == ConstructionCategory.Green &&
                    run.LastEcologyDay?.foodChanges != null)
                    foreach (var change in run.LastEcologyDay.foodChanges)
                        if (change.tileId == existing.id)
                        {
                            feedbackText.text += L(
                                $" · yesterday +{change.produced}, eaten {change.eaten}",
                                $" · 昨日 +{change.produced} / 被吃 {change.eaten}");
                            break;
                        }
                if (run.CurrentDay == 1 &&
                    existing.id == ConstructionBoardModel.StarterOakId)
                    feedbackText.text = L(
                        "Oak green · mature tree · 1 nut in stock.\n" +
                        "It feeds the squirrel. Next day consumes food and advances the date.",
                        "橡树绿地 · 成熟橡树 · 现有 1 份橡果。\n" +
                        "松鼠在这里觅食；点“下一天”结算食物并推进日期。");
                if (existing.id == run.PigeonTileId && run.PigeonPopulation > 0)
                    feedbackText.text = run.StoryStage ==
                        ConstructionStoryStage.WelcomeResident
                        ? L($"Meadow: {run.PigeonPopulation} pigeons, grass seeds " +
                            $"{run.FoodStock(existing.id)}/{run.FoodCapacity(existing.planting)}.\n" +
                            "First meal tomorrow. Next: a residence.",
                            $"草地：{run.PigeonPopulation} 只鸽子，草籽 " +
                            $"{run.FoodStock(existing.id)}/{run.FoodCapacity(existing.planting)}。\n" +
                            "明日首次觅食；接着建住宅。")
                        : feedbackText.text + L(" · a pigeon flock lives here",
                            " · 鸽群在此栖息");
                if (existing.id == run.HedgehogHabitat?.id)
                    feedbackText.text += L(" · hedgehog habitat visitor",
                        " · 刺猬已到来（栖息访客）");
                if (existing.id == run.FoxHabitat?.id)
                    feedbackText.text += L(" · fox den; may hunt reachable wildlife tomorrow",
                        " · 狐狸栖息地；明日可能捕食可达动物");
                if (existing.category == ConstructionCategory.Residence)
                {
                    if (!run.HasResident(existing.id))
                        feedbackText.text += L(" · vacant: resident left after missed commutes",
                            " · 空置：居民因连续缺勤离开");
                    else
                    {
                        var person = run.LastHumanDay?.residents.Find(item =>
                            item.homeTileId == existing.id);
                        feedbackText.text += person == null
                            ? L(" · 1 resident · daily route not settled yet",
                                " · 1 名居民 · 尚未结算路线")
                            : L($" · last day work {(person.worked ? 1 : 0)}/1, " +
                                $"meal {(person.ate ? 1 : 0)}/1, " +
                                $"complete {(person.completedCycle ? 1 : 0)}/1",
                                $" · 昨日上班 {(person.worked ? 1 : 0)}/1、" +
                                $"用餐 {(person.ate ? 1 : 0)}/1、" +
                                $"完成循环 {(person.completedCycle ? 1 : 0)}/1");
                    }
                }
                if (existing.category is ConstructionCategory.Residence or
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket)
                    feedbackText.text += L(
                        $" · waste {run.WasteBacklog(existing.id)}/" +
                        $"{ConstructionWasteModel.DisruptionThreshold} (cleanup before next route)",
                        $" · 垃圾 {run.WasteBacklog(existing.id)}/" +
                        $"{ConstructionWasteModel.DisruptionThreshold}（次日外出前清运）");
                var capacity = ConstructionHumanModel.DailyCapacity(existing.category);
                if (capacity > 0 && existing.category != ConstructionCategory.Residence)
                {
                    var expected = run.PreviewHumanDay().residents.Count(person =>
                        person.workTileId == existing.id ||
                        person.mealTileId == existing.id);
                    feedbackText.text += L(
                        $" · next day ~{expected}/{capacity} capacity used",
                        $" · 次日预计占用 {expected}/{capacity}");
                }
                if (inspectedPigeons) Save();
                Refresh();
                return false;
            }
            if (run.BuiltToday)
            {
                feedbackText.text = L("One room per day. Your selection is kept for tomorrow.",
                    "每天只能建造一格；所选建筑会保留到明天。");
                Refresh();
                return false;
            }
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (!run.TryBuild(column, row, selectedCategory, planting,
                    out var built, out var failure, out var placement))
            {
                feedbackText.text = failure == ConstructionBuildFailure.LockedByStory
                    ? L("Finish observing the oak before building.",
                        "先完成橡树观察，再开始建造。")
                    : failure == ConstructionBuildFailure.DailyLimitReached
                        ? L("One new cell per day. Check tomorrow's needs, then advance.",
                            "每天只能建造一格。查看次日需求后推进日期。")
                    : failure == ConstructionBuildFailure.RunFinished
                        ? L("This run has ended. Review the final result.",
                            "本局已结束，请查看最终结果。")
                    : PlacementMessage(placement);
                Refresh();
                return false;
            }
            feedbackText.text = built.id == run.FirstResidentHomeTileId
                ? L("A resident moved in. They need 1 meal/day.\n" +
                    "Build a restaurant beside home (1 seat/day).",
                    "一位居民入住，每天需要 1 份餐食。\n" +
                    "请在住宅旁建餐厅（每日接待 1 人）。")
                : built.category == ConstructionCategory.Restaurant
                    ? run.FirstResidentHasRestaurantAccess
                        ? L("Restaurant connected. Advance one day to see the resident eat.",
                            "餐厅已接通。推进一天，观察居民用餐。")
                        : L("Restaurant built, but the first resident cannot reach it.",
                            "餐厅已建好，但第一位居民还无法到达。")
                    : L($"Built {TileLabel(built)} at {column + 1},{row + 1}.",
                        $"已在 {column + 1},{row + 1} 建造{TileLabel(built)}。");
            buildPaletteOpen = false;
            Save();
            Refresh();
            if (built.category == ConstructionCategory.Residence)
                worldView?.PlayResidentArrival(built.id);
            // The starter oak occupies day one. The forty-eighth player
            // placement fills the last vacancy on day 49; settle that day
            // immediately so "board full" and "game over" are the same event.
            if (run.Mode == ConstructionGameMode.Fill && run.IsFull &&
                !run.IsFinished && !AdvanceDay())
                feedbackText.text = L("Final day could not be settled. Try Next day.",
                    "最后一天未能结算，请尝试点击“下一天”。");
            return true;
        }

        public bool AdvanceDay()
        {
            if (hasPendingPlacement) return false;
            ClearResetConfirmation();
            if (run == null || dayText == null || run.IsFinished) return false;
            if (!run.CanAdvanceConstructionDay)
            {
                feedbackText.text = L("Build one room before ending this day.",
                    "本日还需建造一格，才能进入下一天。");
                return false;
            }
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
            var squirrelsBefore = run.SquirrelPopulation;
            var pigeonCountBefore = run.PigeonPopulation;
            var residentAteBefore = run.FirstResidentAte;
            var residentMealRoute = run.StoryStage ==
                ConstructionStoryStage.WatchResidentEat
                ? run.FirstResidentRestaurantRoute
                : Array.Empty<ConstructionTileData>();
            var squirrelRoute = run.PreviewSquirrelFoodRoute();
            var pigeonRoute = run.PreviewPigeonFoodRoute();
            if (!run.TrySimulateConstructionDay(out var ecology)) return false;
            var human = run.LastHumanDay;
            feedbackText.text = openingMeal && ecology.squirrelAte
                ? L("Day 1 settled: the squirrel ate one oak nut. Choose any building; a connected meadow may attract pigeons.",
                    "第 1 天已结算：松鼠吃到 1 份橡果。现在可自由选建筑；相连草地可能吸引鸽群。")
                : ecology.predatedSpecies != ConstructionPreySpecies.None
                    ? L($"Fox hunted one {PreyName(ecology.predatedSpecies, false)} " +
                        $"at {TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.predationTileId)}. " +
                        "The prey population changed; inspect tomorrow's forecast.",
                        $"狐狸在{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.predationTileId)}" +
                        $"捕食了 1 只{PreyName(ecology.predatedSpecies, true)}。" +
                        "动物数量已变化，请查看明日预测。")
                : pigeonsBefore == null && run.PigeonTileId != null
                    ? L("A flock arrived. It will forage from the next day.",
                        "鸽群到来了，明天开始觅食。")
                : run.SquirrelPopulation > squirrelsBefore
                    ? L("Mature nearby oaks attracted another squirrel. Check tomorrow's food.",
                        "附近成熟橡树吸引来一只松鼠，留意明天的食物。")
                : run.PigeonPopulation > pigeonCountBefore
                    ? L("Connected mature meadows enlarged the flock. Check tomorrow's food.",
                        "相连成熟草地让鸽群壮大，留意明天的食物。")
                : !residentAteBefore && run.FirstResidentAte
                    ? L("The resident reached the restaurant. A workshop could complete their routine.",
                        "居民到餐厅吃了饭；建工作坊可补全日常路线。")
                : human?.restaurantRejectedForCapacity > 0 &&
                  human.MealsEaten < human.ResidentCount
                    ? L($"Restaurant full: {human.ResidentCount - human.MealsEaten} " +
                        $"resident(s) missed food. Workdays " +
                        $"{human.CompletedWorkCycles}/{human.ResidentCount}.",
                        $"餐厅已满：{human.ResidentCount - human.MealsEaten} 人未吃到饭；" +
                        $"完整通勤 {human.CompletedWorkCycles}/{human.ResidentCount}。")
                : human?.restaurantRejectedForWaste > 0
                    ? L($"Waste blocked {human.restaurantRejectedForWaste} restaurant " +
                        $"meal(s). Cleanup {run.LastWasteDay?.Cleared ?? 0}; " +
                        $"backlog {run.LastWasteDay?.Remaining ?? 0}.",
                        $"垃圾阻断 {human.restaurantRejectedForWaste} 份餐食；" +
                        $"已清运 {run.LastWasteDay?.Cleared ?? 0}，积压 {run.LastWasteDay?.Remaining ?? 0}。")
                : L($"Animal meals {ecology.AnimalMeals}; residents " +
                    $"worked {human?.WorkedCount ?? 0}, ate {human?.MealsEaten ?? 0}, " +
                    $"finished {human?.CompletedWorkCycles ?? 0}.",
                    $"动物进食 {ecology.AnimalMeals} 份；居民上班 {human?.WorkedCount ?? 0}、" +
                    $"用餐 {human?.MealsEaten ?? 0}、完成循环 {human?.CompletedWorkCycles ?? 0}。");
            if (run.EndReason == ConstructionEndReason.WildlifeGone)
                feedbackText.text = L("A wildlife population disappeared. Endless run ended.",
                    "动物种群消失，无尽模式结束。");
            else if (run.EndReason == ConstructionEndReason.ResidentsGone)
                feedbackText.text = L("All residents left after missed commutes. Endless run ended.",
                    "居民连续缺勤后全部离开，无尽模式结束。");
            Save();
            worldView?.Sync(run, true);
            var newPigeonFlock = pigeonsBefore == null && run.PigeonTileId != null;
            if (newPigeonFlock) worldView?.PlayPigeonArrival();
            Refresh();
            worldView?.PlayForaging(
                ecology.predatedSpecies == ConstructionPreySpecies.Squirrel
                    ? null : squirrelRoute,
                ecology.predatedSpecies == ConstructionPreySpecies.Pigeon
                    ? null : pigeonRoute);
            if (ecology.predatedSpecies != ConstructionPreySpecies.None)
                worldView?.PlayFoxHunt(ecology.foxHuntRouteTileIds, run.BuiltTiles);
            if (human?.residents != null)
            {
                var firstJourney = human.residents.FirstOrDefault(person =>
                    (person.worked || person.ate) && person.routeTileIds.Count > 1);
                if (firstJourney != null)
                    worldView?.PlayResidentCommute(firstJourney.routeTileIds,
                        run.BuiltTiles);
            }
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
                points[index] = new Vector2(23 +
                    (route[index].column - viewMinColumn - 3) * BoardCellSpacing,
                    -22 + (3 - route[index].row + viewMinRow) * BoardCellSpacing);
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
            background.sprite = LoadSprite("UI/GameplayHud/gameplay-tabletop-backdrop-v01");
            // In a graphical player the photographed tabletop is a world-space
            // plane behind the actual 3D board, not a screen-space board image.
            var direct3D = SystemInfo.graphicsDeviceType !=
                UnityEngine.Rendering.GraphicsDeviceType.Null;
            background.color = direct3D ? Color.clear : Color.white;
            background.raycastTarget = false;

            // The original tabletop and live 3D board are the scene. Avoid a
            // full-width HUD banner that would turn it into a flat menu.
            titleText = TextAt("Title", canvas.transform, new Vector2(-590, 409),
                new Vector2(350, 42), "SYMBIOSIS · BUILD", 23, true, Ink,
                TextAnchor.MiddleLeft);
            var languageControl = NewRect("Construction language", canvas.transform,
                new Vector2(414, 409), new Vector2(122, 42));
            englishLanguageButton = ButtonAt("English", languageControl,
                new Vector2(-31, 0), new Vector2(58, 40), "EN", 15,
                Ink, Paper, () => SetLanguage(InterfaceLanguage.English));
            chineseLanguageButton = ButtonAt("Chinese", languageControl,
                new Vector2(31, 0), new Vector2(58, 40), "中文", 15,
                Ink, Paper, () => SetLanguage(InterfaceLanguage.Chinese));
            englishLanguageLabel = englishLanguageButton.GetComponentInChildren<Text>();
            chineseLanguageLabel = chineseLanguageButton.GetComponentInChildren<Text>();
            var dayDial = Panel("Construction day dial", canvas.transform,
                new Vector2(0, 393), new Vector2(74, 74), Color.white);
            dayDial.sprite = LoadSprite("UI/GameplayHud/day-night-dial-v02");
            dayDial.preserveAspect = true;
            dayDial.raycastTarget = false;
            dayText = TextAt("Day", dayDial.transform, Vector2.zero,
                new Vector2(56, 36), "", 25, true, Paper,
                TextAnchor.MiddleCenter);
            dayText.raycastTarget = false;
            var dayOutline = dayText.gameObject.AddComponent<Outline>();
            dayOutline.effectColor = Ink;
            dayOutline.effectDistance = new Vector2(1.2f, -1.2f);
            var menuButton = ButtonAt("Return to main menu", canvas.transform,
                new Vector2(548, 409), new Vector2(130, 42), "←  Menu", 15,
                Ink, Paper, () => { ReturnToMainMenu(); });
            menuLabel = menuButton.GetComponentInChildren<Text>();
            var reset = ButtonAt("Start a new construction run", canvas.transform,
                new Vector2(683, 409), new Vector2(130, 42), "New run", 15,
                Ink, Paper, () => OnResetPressed());
            resetLabel = reset.GetComponentInChildren<Text>();
            foreach (var header in new GameObject[] { titleText.gameObject,
                         languageControl.gameObject, dayDial.gameObject,
                         menuButton.gameObject, reset.gameObject })
                caseHeaderGroups.Add(header.AddComponent<CanvasGroup>());

            var boardPanel = Panel("7 by 7 construction board", canvas.transform,
                new Vector2(0, -3), new Vector2(760, 760),
                direct3D ? Color.clear : Ink);
            boardPanel.raycastTarget = false;
            var goldEdge = Panel("Board gold edge", boardPanel.transform,
                Vector2.zero, new Vector2(750, 750),
                direct3D ? Color.clear : Gold);
            goldEdge.raycastTarget = false;
            var inset = Panel("Board inset", boardPanel.transform,
                Vector2.zero, new Vector2(740, 740),
                direct3D ? Color.clear : new Color(0.79f, 0.75f, 0.66f));
            inset.raycastTarget = false;
            var boardArea = NewRect("3D board alignment", boardPanel.transform,
                Vector2.zero, new Vector2(740, 740));
            worldView = gameObject.AddComponent<ConstructionWorldView>();
            worldView.Initialize(Camera.main, boardArea, BoardCellSpacing);
            BuildCells(boardPanel.transform);
            panControls.Add(ButtonAt("Pan board north", canvas.transform,
                new Vector2(0, 385), new Vector2(42, 35), "↑", 22,
                Color.white, Paper, () => Pan(0, -1)).gameObject);
            panControls.Add(ButtonAt("Pan board south", canvas.transform,
                new Vector2(0, -391), new Vector2(42, 35), "↓", 22,
                Color.white, Paper, () => Pan(0, 1)).gameObject);
            panControls.Add(ButtonAt("Pan board west", canvas.transform,
                new Vector2(-390, -3), new Vector2(42, 35), "←", 22,
                Color.white, Paper, () => Pan(-1, 0)).gameObject);
            panControls.Add(ButtonAt("Pan board east", canvas.transform,
                new Vector2(390, -3), new Vector2(42, 35), "→", 22,
                Color.white, Paper, () => Pan(1, 0)).gameObject);
            routeOverlay = NewRect("Selected route overlay", boardPanel.transform,
                Vector2.zero, new Vector2(760, 760));
            routeOverlay.gameObject.AddComponent<RectMask2D>();
            movingResidentMarker = CreateHumanMarker(boardPanel.transform,
                Vector2.zero, 0.8f, "Resident walking");
            movingResidentMarker.SetActive(false);
            // The original parchment note artwork makes the side information
            // part of the same tabletop scene as the old settings UI.
            var leftFrame = Panel("Observation frame", canvas.transform,
                new Vector2(-568, -2), new Vector2(382, 690), Color.white);
            ApplySkin(leftFrame, "UI/PauseMenu/pause-menu-panel-v01");
            leftFrame.raycastTarget = false;
            var rightFrame = Panel("Action frame", canvas.transform,
                new Vector2(568, -2), new Vector2(382, 690), Color.white);
            ApplySkin(rightFrame, "UI/PauseMenu/pause-menu-panel-v01");
            rightFrame.raycastTarget = false;
            leftCaseLeaf = leftFrame.rectTransform;
            rightCaseLeaf = rightFrame.rectTransform;
            routePanel = Panel("Route inspection", canvas.transform,
                new Vector2(-568, -145), new Vector2(350, 130), Color.clear);
            routePanel.raycastTarget = false;
            caseRouteInformation = routePanel.gameObject.AddComponent<CanvasGroup>();
            routeHeading = TextAt("Route heading", routePanel.transform,
                new Vector2(0, 43), new Vector2(250, 28), "ROUTE", 19, true,
                HeadingInk, TextAnchor.MiddleLeft);
            routeHeading.raycastTarget = false;
            routeText = TextAt("Route status", routePanel.transform,
                new Vector2(0, -15), new Vector2(250, 82), "", 17, false,
                Ink, TextAnchor.MiddleLeft);

            // Keep the original painted sprite available for tests and later
            // popovers, but do not leave a tall permanent sidebar on screen.
            var side = Panel("Building choices", canvas.transform,
                Vector2.zero, new Vector2(1600, 900), Color.clear);
            ApplySkin(side, "UI/PauseMenu/pause-menu-panel-v01");
            side.color = Color.clear;
            side.raycastTarget = false;
            caseInformation = side.gameObject.AddComponent<CanvasGroup>();
            var storyBacking = Panel("Story cue backing", side.transform,
                new Vector2(-568, 135), new Vector2(350, 230), Color.clear);
            storyBacking.raycastTarget = false;
            visitorMarker = CreateHumanMarker(side.transform,
                new Vector2(-716, 250), 1.0f, "Waiting visitor");
            todayHeading = TextAt("Today heading", storyBacking.transform,
                new Vector2(26, 99), new Vector2(196, 28),
                "TODAY", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            todayHeading.raycastTarget = false;
            storyText = TextAt("Story instruction", storyBacking.transform,
                new Vector2(0, 35), new Vector2(250, 100), "", 17, true,
                Ink, TextAnchor.MiddleLeft);
            storyText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            storyPreviewText = TextAt("Story preview", storyBacking.transform,
                new Vector2(0, 35), new Vector2(250, 100), "", 19, true,
                Ink, TextAnchor.MiddleLeft);
            storyPreviewText.raycastTarget = false;
            storyCueIcon = Panel("Story pictogram", storyBacking.transform,
                new Vector2(-110, 99), new Vector2(34, 34), Color.white);
            storyCueIcon.preserveAspect = true;
            storyCueIcon.raycastTarget = true;
            AttachHover(storyCueIcon.gameObject, () => storyCueIcon.sprite,
                () => todayHeading.text, () => storyText.text,
                TooltipLocation.LeftLeaf);
            Panel("Story divider", storyBacking.transform,
                new Vector2(0, -20), new Vector2(250, 2),
                new Color(0.45f, 0.51f, 0.36f, 0.8f))
                .raycastTarget = false;
            scoreText = TextAt("Observed outcomes", storyBacking.transform,
                new Vector2(0, -63), new Vector2(250, 72), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            scoreText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            animalMetricText = MetricCard(storyBacking.transform,
                "Animal meals metric", new Vector2(-65, -67),
                LoadSprite("UI/GameplayHud/population-squirrel-v02"),
                () => L("Animal meals", "动物进食"), () => scoreText.text,
                caption: () => L("MEALS", "进食"));
            humanMetricText = MetricCard(storyBacking.transform,
                "Work cycles metric", new Vector2(65, -67),
                LoadSprite("UI/GameplayHud/metric-human-function-v01"),
                () => L("Work cycles", "完整通勤"), () => scoreText.text,
                caption: () => L("WORK", "通勤"));
            paletteBackdrop = Panel("Build palette backing", side.transform,
                new Vector2(565, 15), new Vector2(370, 570), Paper);
            ApplySkin(paletteBackdrop, "UI/PauseMenu/pause-menu-settings-panel-v01");
            paletteBackdrop.raycastTarget = true;
            paletteNotice = TextAt("Palette notice", paletteBackdrop.transform,
                new Vector2(0, 170), new Vector2(320, 48), "", 15, true,
                Ink, TextAnchor.MiddleCenter);
            paletteNotice.raycastTarget = false;
            BuildCategoryButtons(side.transform);
            dayReviewPanel = Panel("Last day review", side.transform,
                new Vector2(-568, -18), new Vector2(350, 110), Color.clear);
            dayReviewPanel.raycastTarget = false;
            reviewHeading = TextAt("Review heading", dayReviewPanel.transform,
                new Vector2(0, 33), new Vector2(250, 28),
                "LAST DAY", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            reviewHeading.raycastTarget = false;
            dayReviewText = TextAt("Day review text", dayReviewPanel.transform,
                new Vector2(0, -18), new Vector2(250, 72), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            dayReviewText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            dayAnimalMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday animal meals", new Vector2(-87, -22),
                LoadSprite("UI/GameplayHud/population-squirrel-v02"),
                () => L("Yesterday: animal meals", "昨日动物进食"),
                () => dayReviewText.text, 78f);
            dayHumanMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday work cycles", new Vector2(0, -22),
                LoadSprite("UI/GameplayHud/metric-human-function-v01"),
                () => L("Yesterday: work cycles", "昨日完整通勤"),
                () => dayReviewText.text, 78f);
            dayWasteMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday waste cleared", new Vector2(87, -22),
                RoomIconCatalog.GetSprite(RoomType.Trash),
                () => L("Yesterday: waste cleared", "昨日垃圾清运"),
                () => dayReviewText.text, 78f);
            BuildPlantingButtons(side.transform);
            feedbackPanel = Panel("Feedback backing", side.transform,
                new Vector2(568, -123), new Vector2(342, 108), Color.clear);
            feedbackPanel.raycastTarget = false;
            Panel("Feedback divider", feedbackPanel.transform,
                new Vector2(0, 49), new Vector2(250, 2),
                new Color(0.45f, 0.51f, 0.36f, 0.8f)).raycastTarget = false;
            feedbackText = TextAt("Feedback", feedbackPanel.transform,
                new Vector2(0, -6), new Vector2(250, 94), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            feedbackText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            var feedbackIcon = Panel("Feedback icon", feedbackPanel.transform,
                new Vector2(-107, -8), new Vector2(35, 35), Color.white);
            feedbackIcon.sprite = LoadSprite("UI/GameplayHud/metric-food-access-v01");
            feedbackIcon.preserveAspect = true;
            feedbackIcon.raycastTarget = false;
            feedbackPreviewText = TextAt("Feedback preview", feedbackPanel.transform,
                new Vector2(19, -8), new Vector2(205, 62), "", 15, true,
                Ink, TextAnchor.MiddleLeft);
            feedbackPreviewText.raycastTarget = false;
            feedbackPanel.raycastTarget = true;
            AttachHover(feedbackPanel.gameObject,
                () => feedbackIcon.sprite, () => L("Status", "当前提示"),
                () => string.IsNullOrEmpty(predationRiskText)
                    ? feedbackText.text : predationRiskText + "\n" + feedbackText.text,
                TooltipLocation.BoardInfo);
            progressPanel = Panel("Board progress backing", side.transform,
                new Vector2(568, 204), new Vector2(342, 132), Color.clear);
            progressPanel.raycastTarget = false;
            progressHeading = TextAt("Progress heading", progressPanel.transform,
                new Vector2(0, 36), new Vector2(250, 28),
                "BOARD", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            progressHeading.raycastTarget = false;
            progressText = TextAt("Board progress", progressPanel.transform,
                new Vector2(21, -17), new Vector2(208, 58), "", 22, true,
                Ink, TextAnchor.MiddleLeft);
            var progressIcon = Panel("Board progress icon", progressPanel.transform,
                new Vector2(-107, -17), new Vector2(39, 39), Color.white);
            progressIcon.sprite = RoomIconCatalog.GetSprite(RoomType.CommunitySquare);
            progressIcon.preserveAspect = true;
            progressIcon.raycastTarget = false;
            progressPanel.raycastTarget = true;
            AttachHover(progressPanel.gameObject, () => progressIcon.sprite,
                () => L("Board progress", "棋盘进度"),
                () => run == null ? string.Empty : L(
                    $"Built {run.BuiltCount}/{run.BoardCapacity}. " +
                    $"Squirrels {run.SquirrelPopulation}, pigeons {run.PigeonPopulation}; " +
                    $"hedgehog {(run.HedgehogHabitat != null ? 1 : 0)}, fox {(run.FoxHabitat != null ? 1 : 0)}; " +
                    $"residents {run.ResidentPopulation}.",
                    $"已建 {run.BuiltCount}/{run.BoardCapacity} 格；" +
                    $"松鼠 {run.SquirrelPopulation}、鸽子 {run.PigeonPopulation}，" +
                    $"刺猬 {(run.HedgehogHabitat != null ? 1 : 0)}、狐狸 {(run.FoxHabitat != null ? 1 : 0)}，" +
                    $"居民 {run.ResidentPopulation}。"));
            nextDayButton = ButtonAt("Advance to next day", side.transform,
                new Vector2(631, -230), new Vector2(118, 48), "Next day  →", 15,
                new Color(0.17f, 0.40f, 0.36f), Paper, () => AdvanceDay());
            nextDayLabel = nextDayButton.GetComponentInChildren<Text>();
            buildPaletteButton = ButtonAt("Open build palette", side.transform,
                new Vector2(505, -230), new Vector2(118, 48), "Build", 15,
                Ink, Paper, ToggleBuildPalette);
            buildPaletteLabel = buildPaletteButton.GetComponentInChildren<Text>();

            guidePanel = Panel("Next step backing", side.transform,
                new Vector2(568, 17), new Vector2(342, 130), Color.clear);
            guidePanel.raycastTarget = false;
            nextStepHeading = TextAt("Next step heading", guidePanel.transform,
                new Vector2(0, 34), new Vector2(250, 28),
                "NEXT STEP", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            nextStepHeading.raycastTarget = false;
            placementGuideText = TextAt("Placement guide", guidePanel.transform,
                new Vector2(22, -18), new Vector2(205, 82),
                "", 16, false, Ink, TextAnchor.MiddleLeft);
            selectedBuildIcon = Panel("Selected building icon", guidePanel.transform,
                new Vector2(-107, -18), new Vector2(40, 40), Color.white);
            selectedBuildIcon.preserveAspect = true;
            selectedBuildIcon.raycastTarget = false;
            guidePanel.raycastTarget = true;
            AttachHover(guidePanel.gameObject, () => selectedBuildIcon.sprite,
                () => SelectedBuildLabel(), () => placementGuideText.text);
            Panel("Action divider", side.transform, new Vector2(568, 116),
                new Vector2(250, 2), new Color(0.45f, 0.51f, 0.36f, 0.8f))
                .raycastTarget = false;
            BuildPlacementConfirmation(side.transform);
            BuildTutorialFocus(canvas.transform);

            finalResultPanel = Panel("Construction final result", canvas.transform,
                Vector2.zero, new Vector2(1600, 900),
                new Color(0.05f, 0.09f, 0.11f, 0.78f)).gameObject;
            var resultCard = Panel("Result card", finalResultPanel.transform,
                Vector2.zero, new Vector2(640, 470), Paper);
            ApplySkin(resultCard, "UI/PauseMenu/pause-menu-settings-panel-v01");
            resultTitleLabel = TextAt("Result title", resultCard.transform,
                new Vector2(0, 175), new Vector2(580, 55),
                "49 CELLS COMPLETE", 31, true, Ink, TextAnchor.MiddleCenter);
            finalResultText = TextAt("Result numbers", resultCard.transform,
                new Vector2(0, 12), new Vector2(560, 245), "", 22, false,
                Ink, TextAnchor.MiddleCenter);
            resultNoticeText = TextAt("Result notice", resultCard.transform,
                new Vector2(0, -125), new Vector2(560, 36), "", 16, false,
                Ink, TextAnchor.MiddleCenter);
            var resultReset = ButtonAt("New run after result", resultCard.transform,
                new Vector2(0, -177), new Vector2(270, 54), "New run", 22,
                new Color(0.70f, 0.97f, 0.89f), Paper, OnResetPressed);
            resultResetLabel = resultReset.GetComponentInChildren<Text>();
            finalResultPanel.SetActive(false);
            BuildTooltip(canvas.transform);
            ApplyLanguageFonts();
            RefreshPaletteVisibility();
            PrepareBoardCaseOpening(canvas.transform,
                SystemInfo.graphicsDeviceType !=
                    UnityEngine.Rendering.GraphicsDeviceType.Null);
        }

        private void PrepareBoardCaseOpening(Transform canvas, bool animate)
        {
            IsBoardCaseOpen = !animate;
            if (!animate) return;

            // Each page is hinged at the outside edge of the 7x7 board. Its
            // closed cover lies over one half of the board; the paper face
            // then unfolds into the information area beside that same board.
            leftCaseLeaf.pivot = new Vector2(1f, 0.5f);
            leftCaseLeaf.anchoredPosition = new Vector2(-380f, -2f);
            leftCaseLeaf.localScale = new Vector3(0f, 1f, 1f);
            rightCaseLeaf.pivot = new Vector2(0f, 0.5f);
            rightCaseLeaf.anchoredPosition = new Vector2(380f, -2f);
            rightCaseLeaf.localScale = new Vector3(0f, 1f, 1f);

            caseInformation.alpha = caseRouteInformation.alpha = 0f;
            caseInformation.interactable = caseInformation.blocksRaycasts = false;
            caseRouteInformation.interactable =
                caseRouteInformation.blocksRaycasts = false;
            foreach (var group in caseHeaderGroups)
            {
                group.alpha = 0f;
                group.interactable = group.blocksRaycasts = false;
            }

            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var pinY in new[] { -245f, 0f, 245f })
                {
                    var hinge = Panel(side < 0f ? "West case hinge" : "East case hinge",
                        canvas, new Vector2(side * 400f, pinY - 3f),
                        new Vector2(64f, 28f), Ink);
                    hinge.raycastTarget = false;
                    Panel("Brass hinge band", hinge.transform, Vector2.zero,
                        new Vector2(58f, 8f), Gold).raycastTarget = false;
                    Panel("Brass hinge pin", hinge.transform,
                        new Vector2(-side * 20f, 0f),
                        new Vector2(9f, 22f), Gold).raycastTarget = false;
                }
            }

            // On the shared menu board the player is already looking inside
            // this very case. Only unfold its leaves; introducing opaque
            // covers would make the board appear to close before it opens.
            if (worldView == null || !worldView.UsesOpeningBoard)
            {
                leftCaseCover = CreateCaseCover(canvas, "Left board-case cover",
                    -380f, 1f);
                rightCaseCover = CreateCaseCover(canvas, "Right board-case cover",
                    380f, 0f);
            }
            var blocker = Panel("Board-case opening input guard", canvas,
                Vector2.zero, Vector2.zero, Color.clear);
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;
            caseOpeningBlocker = blocker.gameObject;
        }

        private RectTransform CreateCaseCover(Transform canvas, string name,
            float hingeX, float pivotX)
        {
            var cover = Panel(name, canvas, new Vector2(hingeX, -3f),
                new Vector2(380f, 760f), Ink);
            cover.rectTransform.pivot = new Vector2(pivotX, 0.5f);
            cover.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            cover.raycastTarget = false;
            Panel("Brass cover border", cover.transform, Vector2.zero,
                new Vector2(366f, 744f), Gold).raycastTarget = false;
            Panel("Navy cover centre", cover.transform, Vector2.zero,
                new Vector2(356f, 734f), Ink).raycastTarget = false;
            return cover.rectTransform;
        }

        private IEnumerator OpenBoardCase()
        {
            // Wait until the menu UI is gone, then unfold the two information
            // leaves while the same board stays visible and the camera settles.
            while (ConstructionSceneTransition.IsActive) yield return null;
            var elapsed = 0f;
            if (leftCaseCover != null && rightCaseCover != null)
            {
                yield return null;
                const float coverDuration = 0.42f;
                while (elapsed < coverDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var fold = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / coverDuration));
                    leftCaseCover.localScale = rightCaseCover.localScale =
                        new Vector3(-fold, 1f, 1f);
                    yield return null;
                }
                leftCaseCover.gameObject.SetActive(false);
                rightCaseCover.gameObject.SetActive(false);
            }

            const float leafDuration = 0.68f;
            elapsed = 0f;
            while (elapsed < leafDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var open = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / leafDuration));
                leftCaseLeaf.localScale = rightCaseLeaf.localScale =
                    new Vector3(open, 1f, 1f);
                var inkReveal = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((open - 0.55f) / 0.45f));
                caseInformation.alpha = caseRouteInformation.alpha = inkReveal;
                foreach (var group in caseHeaderGroups) group.alpha = inkReveal;
                yield return null;
            }
            leftCaseLeaf.localScale = rightCaseLeaf.localScale = Vector3.one;
            caseInformation.alpha = caseRouteInformation.alpha = 1f;
            caseInformation.interactable = caseInformation.blocksRaycasts = true;
            caseRouteInformation.interactable = true;
            foreach (var group in caseHeaderGroups)
            {
                group.alpha = 1f;
                group.interactable = group.blocksRaycasts = true;
            }
            caseOpeningBlocker.SetActive(false);
            IsBoardCaseOpen = true;
            RefreshTutorialFocus();
            caseOpening = null;
        }

        private void BuildCells(Transform parent)
        {
            const float spacing = BoardCellSpacing;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var x = column;
                var y = row;
                var button = ButtonAt($"Cell {column + 1},{row + 1}", parent,
                    new Vector2((column - 3) * spacing, (3 - row) * spacing),
                    new Vector2(95, 95), "", 1, Paper, Ink,
                    () => RequestPlacement(viewMinColumn + x, viewMinRow + y), false);
                AttachHover(button.gameObject,
                    () => TileHoverIcon(viewMinColumn + x, viewMinRow + y),
                    () => TileHoverTitle(viewMinColumn + x, viewMinRow + y),
                    () => TileHoverDetails(viewMinColumn + x, viewMinRow + y),
                    TooltipLocation.BoardInfo);
                cellBackgrounds[column, row] = button.GetComponent<Image>();
                var buildable = TextAt("Buildable hint", button.transform,
                    Vector2.zero, new Vector2(90, 90), "+", 25, true,
                    new Color(0.20f, 0.52f, 0.47f), TextAnchor.MiddleCenter);
                buildable.raycastTarget = false;
                buildableHints[column, row] = buildable;
                var previewFrame = Panel("Placement preview", button.transform,
                    Vector2.zero, new Vector2(88, 88),
                    new Color(0.81f, 0.68f, 0.40f, 0.45f));
                previewFrame.raycastTarget = false;
                var previewOutline = previewFrame.gameObject.AddComponent<Outline>();
                previewOutline.effectColor = Gold;
                previewOutline.effectDistance = new Vector2(2f, -2f);
                var previewIcon = Panel("Preview room icon", previewFrame.transform,
                    Vector2.zero, new Vector2(49, 49), Color.white);
                previewIcon.preserveAspect = true;
                previewIcon.raycastTarget = false;
                placementPreviewFrames[column, row] = previewFrame;
                placementPreviewIcons[column, row] = previewIcon;
                previewFrame.gameObject.SetActive(false);
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
                squirrelCounts[column, row] = AnimalCountBadge(squirrel.transform);
                var pigeon = Panel("Pigeon flock", button.transform,
                    new Vector2(-23, -22), new Vector2(31, 31), Color.white);
                pigeon.sprite = LoadSprite("UI/GameplayHud/population-pigeon-v02");
                pigeon.raycastTarget = false;
                pigeon.preserveAspect = true;
                pigeonIcons[column, row] = pigeon;
                pigeonCounts[column, row] = AnimalCountBadge(pigeon.transform);
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

        private void BuildPlacementConfirmation(Transform parent)
        {
            var card = Panel("Placement confirmation", parent,
                new Vector2(568, -45), new Vector2(344, 278), Color.white);
            ApplySkin(card, "UI/PauseMenu/pause-menu-settings-panel-v01");
            placementConfirmation = card.gameObject;
            placementConfirmationTitle = TextAt("Confirmation title", card.transform,
                new Vector2(0, 82), new Vector2(290, 36), "", 20, true,
                HeadingInk, TextAnchor.MiddleCenter);
            placementConfirmationTitle.raycastTarget = false;
            placementConfirmationIcon = Panel("Confirmation room icon", card.transform,
                new Vector2(-99, 32), new Vector2(56, 56), Color.white);
            placementConfirmationIcon.preserveAspect = true;
            placementConfirmationIcon.raycastTarget = false;
            placementConfirmationLocation = TextAt("Confirmation location",
                card.transform, new Vector2(36, 29), new Vector2(187, 76),
                "", 17, true, Ink, TextAnchor.MiddleLeft);
            placementConfirmationLocation.raycastTarget = false;
            placementConfirmationNotice = TextAt("Confirmation notice",
                card.transform, new Vector2(0, -38), new Vector2(272, 58),
                "", 14, false, Ink, TextAnchor.MiddleCenter);
            placementConfirmationNotice.raycastTarget = false;
            var cancel = ButtonAt("Cancel placement", card.transform,
                new Vector2(-75, -87), new Vector2(132, 43), "", 16,
                Ink, Paper, CancelPendingPlacement);
            placementCancelLabel = cancel.GetComponentInChildren<Text>();
            if (placementCancelLabel == null)
                placementCancelLabel = TextAt("Label", cancel.transform,
                    Vector2.zero, new Vector2(132, 43), "", 16, true,
                    Paper, TextAnchor.MiddleCenter);
            placementCancelLabel.raycastTarget = false;
            var confirm = ButtonAt("Confirm placement", card.transform,
                new Vector2(75, -87), new Vector2(132, 43), "", 16,
                new Color(0.17f, 0.40f, 0.36f), Paper,
                () => ConfirmPendingPlacement());
            placementConfirmButton = confirm;
            placementConfirmLabel = confirm.GetComponentInChildren<Text>();
            if (placementConfirmLabel == null)
                placementConfirmLabel = TextAt("Label", confirm.transform,
                    Vector2.zero, new Vector2(132, 43), "", 16, true,
                    Paper, TextAnchor.MiddleCenter);
            placementConfirmLabel.raycastTarget = false;
            placementConfirmation.SetActive(false);
        }

        private void BuildTutorialFocus(Transform parent)
        {
            // One non-interactive frame follows the next suggested control.
            // It is reparented, never duplicated, so it cannot cover the board
            // or prevent the player from choosing a different action.
            tutorialFocus = NewRect("Tutorial focus", parent,
                Vector2.zero, Vector2.zero);
            var color = new Color(0.98f, 0.78f, 0.37f);
            foreach (var edgeName in new[] { "Top", "Bottom", "Left", "Right" })
            {
                var edge = Panel(edgeName, tutorialFocus, Vector2.zero,
                    Vector2.zero, color);
                edge.raycastTarget = false;
                var rect = edge.rectTransform;
                if (edgeName == "Top" || edgeName == "Bottom")
                {
                    var y = edgeName == "Top" ? 1f : 0f;
                    rect.anchorMin = new Vector2(0f, y);
                    rect.anchorMax = new Vector2(1f, y);
                    rect.sizeDelta = new Vector2(0f, 3f);
                    rect.anchoredPosition = new Vector2(0f, y == 1f ? -1.5f : 1.5f);
                }
                else
                {
                    var x = edgeName == "Right" ? 1f : 0f;
                    rect.anchorMin = new Vector2(x, 0f);
                    rect.anchorMax = new Vector2(x, 1f);
                    rect.sizeDelta = new Vector2(3f, 0f);
                    rect.anchoredPosition = new Vector2(x == 1f ? -1.5f : 1.5f, 0f);
                }
                var contrast = edge.gameObject.AddComponent<Outline>();
                contrast.effectColor = Ink;
                contrast.effectDistance = new Vector2(1f, -1f);
            }
            tutorialFocus.gameObject.SetActive(false);
        }

        private void RefreshPendingPlacement()
        {
            if (placementConfirmation == null) return;
            placementConfirmation.SetActive(hasPendingPlacement);
            if (!hasPendingPlacement) return;
            placementConfirmation.transform.SetAsLastSibling();
            placementConfirmationTitle.text = L("CONFIRM BUILD", "确认建造");
            placementConfirmationIcon.sprite = RoomIconCatalog.GetSprite(
                RoomTypeFor(pendingCategory, pendingPlanting));
            placementConfirmationLocation.text = L(
                $"{SelectedBuildLabel()}\nPlot {pendingColumn + 1},{pendingRow + 1} · Day {pendingDay}",
                $"{SelectedBuildLabel()}\n坐标 {pendingColumn + 1},{pendingRow + 1} · 第 {pendingDay} 天");
            placementConfirmationNotice.text = run.TryPreviewBuildImpact(
                    pendingColumn, pendingRow, pendingCategory, pendingPlanting,
                    out var impact)
                ? L($"Day-end meals {impact.animalMealsBefore}→{impact.animalMealsAfter}" +
                    $" · work {impact.workCyclesBefore}→{impact.workCyclesAfter}\n" +
                    "Uses today's build.",
                    $"日终进食 {impact.animalMealsBefore}→{impact.animalMealsAfter}" +
                    $" · 通勤 {impact.workCyclesBefore}→{impact.workCyclesAfter}\n" +
                    "确认后使用今日建造机会。")
                : L("Impact preview unavailable. Uses today's build.",
                    "暂无法预览影响；确认后使用今日建造机会。");
            placementCancelLabel.text = L("Cancel", "取消");
            placementConfirmLabel.text = L("Confirm", "确认建造");
        }

        private void RefreshTutorialFocus()
        {
            if (tutorialFocus == null) return;
            var target = TutorialTarget();
            if (target == null)
            {
                tutorialFocus.gameObject.SetActive(false);
                return;
            }
            tutorialFocus.SetParent(target, false);
            tutorialFocus.anchorMin = Vector2.zero;
            tutorialFocus.anchorMax = Vector2.one;
            tutorialFocus.offsetMin = new Vector2(-3f, -3f);
            tutorialFocus.offsetMax = new Vector2(3f, 3f);
            tutorialFocus.SetAsLastSibling();
            tutorialFocus.gameObject.SetActive(true);
        }

        private RectTransform TutorialTarget()
        {
            if (!IsBoardCaseOpen || run == null || run.IsFinished) return null;
            if (hasPendingPlacement)
                return placementConfirmButton.transform as RectTransform;
            if (run.StoryStage == ConstructionStoryStage.WatchSquirrelEat)
                return openingOakInspected
                    ? nextDayButton.transform as RectTransform
                    : VisibleTile(ConstructionBoardModel.StarterOakId);
            if (run.StoryStage == ConstructionStoryStage.WelcomeResident &&
                !run.PigeonMeadowInspected)
                return VisibleTile(run.PigeonTileId);
            if (run.BuiltToday ||
                run.StoryStage == ConstructionStoryStage.GrowGreen &&
                run.HasConnectedMeadow ||
                run.StoryStage == ConstructionStoryStage.WatchResidentEat ||
                run.StoryStage == ConstructionStoryStage.AwaitRestaurantCrowding &&
                run.PreviewWasteAvailability().BlockedHomes.Count == 0 &&
                run.PreviewWasteAvailability().BlockedFoodServices.Count == 0)
                return nextDayButton.transform as RectTransform;
            if (run.StoryStage is ConstructionStoryStage.FreeBuild or
                ConstructionStoryStage.BoardComplete)
                return null;

            var suggested = SuggestedTutorialCategory();
            var planting = suggested == ConstructionCategory.Green
                ? GreenPlanting.Meadow : GreenPlanting.None;
            if (!buildPaletteOpen && (selectedCategory != suggested ||
                suggested == ConstructionCategory.Green &&
                selectedPlanting != GreenPlanting.Meadow))
                return buildPaletteButton.transform as RectTransform;
            if (buildPaletteOpen && selectedCategory != suggested)
                return categoryButtons[suggested].transform as RectTransform;
            if (buildPaletteOpen && suggested == ConstructionCategory.Green &&
                selectedPlanting != GreenPlanting.Meadow)
                return plantingButtons[GreenPlanting.Meadow].transform as RectTransform;
            return VisibleValidPlot(suggested, planting) ??
                (buildPaletteOpen ? null : buildPaletteButton.transform as RectTransform);
        }

        private ConstructionCategory SuggestedTutorialCategory() => run.StoryStage switch
        {
            ConstructionStoryStage.GrowGreen => ConstructionCategory.Green,
            ConstructionStoryStage.WelcomeResident or
                ConstructionStoryStage.AwaitAccessProblem =>
                ConstructionCategory.Residence,
            ConstructionStoryStage.BuildRestaurant => ConstructionCategory.Restaurant,
            ConstructionStoryStage.BuildWorkshop => ConstructionCategory.Workshop,
            ConstructionStoryStage.BuildWasteRoom => ConstructionCategory.Waste,
            ConstructionStoryStage.BuildStreet => ConstructionCategory.Street,
            ConstructionStoryStage.BuildSupermarket => ConstructionCategory.Supermarket,
            ConstructionStoryStage.AwaitRestaurantCrowding =>
                ConstructionCategory.Waste,
            _ => ConstructionCategory.Green
        };

        private RectTransform VisibleTile(string tileId)
        {
            if (string.IsNullOrEmpty(tileId)) return null;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
                if (run.At(viewMinColumn + column, viewMinRow + row)?.id == tileId)
                    return cellBackgrounds[column, row].rectTransform;
            return null;
        }

        private RectTransform VisibleValidPlot(ConstructionCategory category,
            GreenPlanting planting)
        {
            RectTransform best = null;
            var bestDistance = int.MaxValue;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var worldColumn = viewMinColumn + column;
                var worldRow = viewMinRow + row;
                if (run.CheckPlacement(worldColumn, worldRow, category, planting) !=
                    ConstructionPlacementFailure.None)
                    continue;
                var distance = Math.Abs(worldColumn - 3) + Math.Abs(worldRow - 3);
                if (distance >= bestDistance) continue;
                best = cellBackgrounds[column, row].rectTransform;
                bestDistance = distance;
            }
            return best;
        }

        private Text AnimalCountBadge(Transform animalIcon)
        {
            var badge = Panel("Animal group count", animalIcon,
                new Vector2(12, -12), new Vector2(22, 19), Ink);
            badge.raycastTarget = false;
            var label = TextAt("Count", badge.transform, Vector2.zero,
                new Vector2(21, 18), "1", 12, true, Color.white,
                TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            return label;
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
                    new Vector2(565f + (column - 0.5f) * 164f,
                        118f - row * 75f),
                    new Vector2(156, 58), category.ToString(), 13,
                    Ink, Paper, () =>
                    {
                        if (TrySelect(category) &&
                            category != ConstructionCategory.Green)
                        {
                            buildPaletteOpen = false;
                            RefreshPaletteVisibility();
                        }
                    });
                button.gameObject.AddComponent<CanvasGroup>();
                var icon = Panel("Building icon", button.transform,
                    new Vector2(-52, 0), new Vector2(46, 46), Color.white);
                icon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(category,
                    GreenPlanting.Oak));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                AttachHover(button.gameObject, () => icon.sprite,
                    () => CategoryLabel(category),
                    () => CategoryHoverDetails(category),
                    TooltipLocation.LeftLeaf);
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition = new Vector2(13, 0);
                label.rectTransform.sizeDelta = new Vector2(82, 38);
                var status = TextAt("Unlock status", button.transform,
                    new Vector2(19, -13), new Vector2(90, 17), "", 11,
                    false, Paper, TextAnchor.MiddleLeft);
                status.raycastTarget = false;
                status.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
                var countBacking = Panel("Valid plot badge", button.transform,
                    new Vector2(62, -18), new Vector2(21, 18), Ink);
                countBacking.raycastTarget = false;
                var count = TextAt("Valid plot count", countBacking.transform,
                    Vector2.zero, new Vector2(21, 18), "–", 11, true,
                    Paper, TextAnchor.MiddleCenter);
                count.raycastTarget = false;
                categoryCountLabels.Add(category, count);
                categoryStatusLabels.Add(category, status);
                categoryButtons.Add(category, button);
            }
        }

        private void BuildPlantingButtons(Transform parent)
        {
            plantingPanel = new GameObject("Green planting", typeof(RectTransform));
            plantingPanel.transform.SetParent(parent, false);
            SetRect(plantingPanel.GetComponent<RectTransform>(),
                new Vector2(580, -178), new Vector2(350, 42));
            plantingPanel.AddComponent<CanvasGroup>();
            var plantings = new[]
                { GreenPlanting.Oak, GreenPlanting.Meadow, GreenPlanting.Shrub };
            for (var index = 0; index < plantings.Length; index++)
            {
                var planting = plantings[index];
                var button = ButtonAt($"Plant {planting}", plantingPanel.transform,
                    new Vector2((index - 1) * 116, 0), new Vector2(110, 38),
                    planting.ToString(), 13, Ink, Paper,
                    () =>
                    {
                        TrySelect(ConstructionCategory.Green, planting);
                        buildPaletteOpen = false;
                        RefreshPaletteVisibility();
                    });
                var icon = Panel("Plant icon", button.transform,
                    new Vector2(-38, 0), new Vector2(26, 26), Color.white);
                icon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(
                    ConstructionCategory.Green, planting));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.anchoredPosition = new Vector2(12, 0);
                label.rectTransform.sizeDelta = new Vector2(78, 36);
                AttachHover(button.gameObject, () => icon.sprite,
                    () => PlantingLabel(planting),
                    () => PlantingHoverDetails(planting),
                    TooltipLocation.LeftLeaf);
                plantingButtons.Add(planting, button);
            }
        }

        private Text MetricCard(Transform parent, string name, Vector2 position,
            Sprite sprite, Func<string> title, Func<string> details,
            float width = 116f, Func<string> caption = null)
        {
            var card = Panel(name, parent, position, new Vector2(width, 56f), Ink);
            var accent = Panel("Accent", card.transform,
                new Vector2(-width * 0.5f + 2f, 0),
                new Vector2(3f, 48f), Gold);
            accent.raycastTarget = false;
            var icon = Panel("Icon", card.transform,
                new Vector2(-width * 0.5f + 27f, 0),
                new Vector2(36f, 36f), Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var value = TextAt("Value", card.transform,
                new Vector2(19f, caption == null ? 0f : 6f),
                new Vector2(width - 57f, caption == null ? 44f : 40f),
                "–", 22, true, Paper, TextAnchor.MiddleCenter);
            value.raycastTarget = false;
            if (caption != null)
            {
                var label = TextAt("Metric caption", card.transform,
                    new Vector2(19f, -20f), new Vector2(width - 57f, 14f),
                    caption(), 10, true, Gold, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                metricCaptions.Add(label, caption);
            }
            AttachHover(card.gameObject, () => sprite, title, details,
                TooltipLocation.LeftLeaf);
            return value;
        }

        private void BuildTooltip(Transform canvas)
        {
            tooltipCanvas = (RectTransform)canvas;
            var panel = Panel("Context tooltip", canvas, Vector2.zero,
                new Vector2(344f, 148f), Ink);
            tooltipRect = panel.rectTransform;
            panel.raycastTarget = false;
            var accent = Panel("Tooltip accent", panel.transform,
                new Vector2(-168f, 0), new Vector2(4f, 132f), Gold);
            accent.raycastTarget = false;
            tooltipIcon = Panel("Tooltip icon", panel.transform,
                new Vector2(-137f, 40f), new Vector2(44f, 44f), Color.white);
            tooltipIcon.preserveAspect = true;
            tooltipIcon.raycastTarget = false;
            tooltipTitle = TextAt("Tooltip title", panel.transform,
                new Vector2(20f, 43f), new Vector2(255f, 38f), "", 19,
                true, Paper, TextAnchor.MiddleLeft);
            tooltipTitle.raycastTarget = false;
            tooltipBody = TextAt("Tooltip details", panel.transform,
                new Vector2(0, -28f), new Vector2(300f, 75f), "", 15,
                false, Paper, TextAnchor.UpperLeft);
            tooltipBody.raycastTarget = false;
            panel.gameObject.SetActive(false);
        }

        private void AttachHover(GameObject target, Func<Sprite> icon,
            Func<string> title, Func<string> details,
            TooltipLocation location = TooltipLocation.FollowPointer)
        {
            var trigger = target.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(data =>
            {
                if (data is PointerEventData pointer)
                    ShowTooltip(icon(), title(), details(), pointer.position,
                        location);
            });
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exit);
        }

        private void ShowTooltip(Sprite icon, string title, string details,
            Vector2 pointer, TooltipLocation location)
        {
            if (tooltipRect == null || !IsBoardCaseOpen ||
                finalResultPanel.activeSelf || hasPendingPlacement) return;
            tooltipIcon.sprite = icon;
            tooltipIcon.gameObject.SetActive(icon != null);
            tooltipTitle.text = title;
            tooltipBody.text = details;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipCanvas, pointer, null, out var local);
            var bounds = tooltipCanvas.rect;
            tooltipRect.anchoredPosition = location switch
            {
                TooltipLocation.BoardInfo => new Vector2(568f, -125f),
                TooltipLocation.LeftLeaf => new Vector2(-568f, -225f),
                _ => new Vector2(
                    Mathf.Clamp(local.x + 190f, bounds.xMin + 180f,
                        bounds.xMax - 180f),
                    Mathf.Clamp(local.y - 88f, bounds.yMin + 80f,
                        bounds.yMax - 80f))
            };
            tooltipRect.gameObject.SetActive(true);
            tooltipRect.SetAsLastSibling();
        }

        private void HideTooltip()
        {
            if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
        }

        private string CategoryHoverDetails(ConstructionCategory category)
        {
            if (run == null) return string.Empty;
            var planting = category == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var plots = AvailablePlotCount(category, planting);
            var availability = run.IsUnlocked(category)
                ? plots > 0 ? L($"Valid plots: {plots}", $"可放置：{plots} 处")
                    : PlacementPrerequisite(category)
                : L("Available after the oak lesson", "完成橡树观察后可选");
            var purpose = category switch
            {
                ConstructionCategory.Green => L(
                    "Choose oak, meadow or shrub. Plants grow over time.",
                    "可选橡树、草地或灌木；植物会随日期生长。"),
                ConstructionCategory.Residence => L(
                    "A home for residents; food and work routes matter.",
                    "居民的家；需考虑餐食与工作路线。"),
                ConstructionCategory.Restaurant => L(
                    "Provides daily meals, limited by seats.",
                    "提供每日餐食，受餐位限制。"),
                ConstructionCategory.Workshop => L(
                    "Provides work places for residents.",
                    "提供居民工作岗位。"),
                ConstructionCategory.Waste => L(
                    "Clears waste near homes and restaurants.",
                    "清运住宅与餐厅附近的垃圾。"),
                ConstructionCategory.Street => L(
                    "Extends residents' reachable service network.",
                    "延伸居民可达的服务网络。"),
                ConstructionCategory.Supermarket => L(
                    "Adds another source of daily meals.",
                    "增加一处每日餐食来源。"),
                _ => L("A shared space within the neighbourhood.",
                    "社区中的公共空间。")
            };
            return $"{purpose}\n{availability}";
        }

        private string PlantingHoverDetails(GreenPlanting planting) =>
            planting switch
            {
                GreenPlanting.Oak => L(
                    "Oak food supports squirrels. Growth takes days.",
                    "橡果供松鼠取食；生长需要时间。"),
                GreenPlanting.Meadow => L(
                    "Connected mature meadows can attract pigeons.",
                    "相连且成熟的草地可吸引鸽群。"),
                _ => L("Shrubs grow over days; inspect their stock on the board.",
                    "灌木随日期生长；可在棋盘查看存量。")
            };

        private Sprite TileHoverIcon(int column, int row)
        {
            var tile = run?.At(column, row);
            return tile == null ? null : RoomIconCatalog.GetSprite(
                RoomTypeFor(tile.category, tile.planting));
        }

        private string TileHoverTitle(int column, int row)
        {
            var tile = run?.At(column, row);
            return tile == null ? L("Empty plot", "空地") : TileLabel(tile);
        }

        private string TileHoverDetails(int column, int row)
        {
            if (run == null) return string.Empty;
            var tile = run.At(column, row);
            var location = L($"Column {column + 1}, row {row + 1}",
                $"第 {column + 1} 列 · 第 {row + 1} 行");
            if (tile == null)
            {
                if (run.StoryStage == ConstructionStoryStage.WatchSquirrelEat)
                    return $"{location}\n" + L(
                        "Building starts tomorrow. Inspect the central oak, then end Day 1.",
                        "明天开始建造。先查看中央橡树，再结束第 1 天。");
                if (run.IsFinished)
                    return $"{location}\n" + L("This run has ended.", "本局已结束。");
                if (run.BuiltToday)
                    return $"{location}\n" + L(
                        "Today's room is built. Advance a day to build again.",
                        "今天的房间已建好；推进一天后可再次建造。");
                if (hasPendingPlacement)
                    return $"{location}\n" + L(
                        "Confirm or cancel the staged room first.",
                        "请先确认或取消待建房间。");
                var planting = selectedCategory == ConstructionCategory.Green
                    ? selectedPlanting : GreenPlanting.None;
                var failure = run.CheckPlacement(column, row,
                    selectedCategory, planting);
                return $"{location}\n" + (failure == ConstructionPlacementFailure.None
                    ? L($"Click to preview {SelectedBuildLabel()}, then confirm.",
                        $"点击预览{SelectedBuildLabel()}，然后确认建造。")
                    : PlacementMessage(failure));
            }
            if (tile.category == ConstructionCategory.Green)
                return L($"{location}\nGrowth {run.GreenGrowthStage(tile.id)}/2" +
                    $" · food {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.planting)}",
                    $"{location}\n生长 {run.GreenGrowthStage(tile.id)}/2" +
                    $" · 食物 {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.planting)}");
            var capacity = ConstructionHumanModel.DailyCapacity(tile.category);
            var detail = capacity > 0
                ? L($"Daily capacity: {capacity}", $"每日容量：{capacity}")
                : CategoryHoverDetails(tile.category).Split('\n')[0];
            var waste = run.WasteBacklog(tile.id);
            return $"{location}\n{detail}" + (waste > 0
                ? L($" · waste {waste}", $" · 积压垃圾 {waste}")
                : string.Empty);
        }

        private static string ShortPreview(string value, int maximum)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var oneLine = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return oneLine.Length <= maximum ? oneLine :
                oneLine.Substring(0, maximum - 1).TrimEnd() + "…";
        }

        private string CategoryLabel(ConstructionCategory category) => category switch
        {
            ConstructionCategory.Green => L("Green", "绿地"),
            ConstructionCategory.Residence => L("Residence", "住宅"),
            ConstructionCategory.Restaurant => L("Restaurant", "餐厅"),
            ConstructionCategory.Workshop => L("Workshop", "工作坊"),
            ConstructionCategory.Waste => L("Waste room", "垃圾间"),
            ConstructionCategory.Street => L("Street", "街道"),
            ConstructionCategory.Supermarket => L("Supermarket", "超市"),
            _ => L("Square", "广场")
        };

        private bool CanSelectCategory(ConstructionCategory category) =>
            run != null && run.IsUnlocked(category);

        private string SelectedBuildLabel() => selectedCategory == ConstructionCategory.Green
            ? L($"{PlantingLabel(selectedPlanting)} green",
                $"{PlantingLabel(selectedPlanting)}绿地")
            : CategoryLabel(selectedCategory);

        private int AvailablePlotCount(ConstructionCategory category,
            GreenPlanting planting)
        {
            if (run == null || run.IsFinished) return 0;
            var candidates = new HashSet<(int column, int row)>();
            foreach (var tile in run.BuiltTiles)
            {
                candidates.Add((tile.column + 1, tile.row));
                candidates.Add((tile.column - 1, tile.row));
                candidates.Add((tile.column, tile.row + 1));
                candidates.Add((tile.column, tile.row - 1));
            }
            return candidates.Count(position =>
                run.CheckPlacement(position.column, position.row,
                    category, planting) == ConstructionPlacementFailure.None);
        }

        private string PlacementPrerequisite(ConstructionCategory category) => category switch
        {
            ConstructionCategory.Restaurant or ConstructionCategory.Workshop or
                ConstructionCategory.Supermarket =>
                L("Needs home/road", "需住宅/街道"),
            ConstructionCategory.Street => L("Needs home/road", "需住宅/街道"),
            ConstructionCategory.Waste => L("Needs home/cafe", "需住宅/餐厅"),
            _ => L("No adjacent plot", "暂无相邻空地")
        };

        private string PlantingLabel(GreenPlanting planting) => planting switch
        {
            GreenPlanting.Oak => L("Oak", "橡树"),
            GreenPlanting.Meadow => L("Meadow", "草地"),
            _ => L("Shrub", "灌木")
        };

        public void ToggleBuildPalette()
        {
            if (run == null || run.IsFinished) return;
            ClearPendingPlacement();
            buildPaletteOpen = !buildPaletteOpen;
            Refresh();
        }

        private void RefreshPaletteVisibility()
        {
            if (guidePanel != null)
                guidePanel.gameObject.SetActive(!buildPaletteOpen &&
                    !hasPendingPlacement);
            if (progressPanel != null)
                progressPanel.gameObject.SetActive(!buildPaletteOpen);
            if (paletteBackdrop != null)
            {
                paletteBackdrop.gameObject.SetActive(buildPaletteOpen);
                if (buildPaletteOpen) paletteBackdrop.transform.SetAsLastSibling();
            }
            foreach (var button in categoryButtons.Values)
            {
                if (buildPaletteOpen) button.transform.SetAsLastSibling();
                var group = button.GetComponent<CanvasGroup>();
                if (group == null) continue;
                group.alpha = buildPaletteOpen ? 1f : 0f;
                group.interactable = buildPaletteOpen;
                group.blocksRaycasts = buildPaletteOpen;
            }
            if (plantingPanel != null)
            {
                if (buildPaletteOpen) plantingPanel.transform.SetAsLastSibling();
                var group = plantingPanel.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = buildPaletteOpen ? 1f : 0f;
                    group.interactable = buildPaletteOpen;
                    group.blocksRaycasts = buildPaletteOpen;
                }
            }
            if (buildPaletteLabel != null)
                buildPaletteLabel.text = buildPaletteOpen
                    ? L("Close", "关闭") : L("Build", "建造");
            if (buildPaletteOpen && paletteNotice != null &&
                string.IsNullOrEmpty(paletteNotice.text))
                paletteNotice.text = L("Choose a building and a valid plot",
                    "选择建筑与符合条件的地块");
        }

        private void RefreshStaticLabels()
        {
            titleText.text = run.Mode == ConstructionGameMode.Fill
                ? L("FILL · 49 DAYS", "填空 · 49 天")
                : L("ENDLESS BUILD", "无尽建造");
            menuLabel.text = L("←  Menu", "←  主菜单");
            englishLanguageLabel.text = "EN";
            chineseLanguageLabel.text = "中文";
            englishLanguageLabel.font = boldFont;
            chineseLanguageLabel.font = chineseFont;
            englishLanguageButton.targetGraphic.color =
                new Color(1f, 1f, 1f, Chinese ? 0.55f : 1f);
            chineseLanguageButton.targetGraphic.color =
                new Color(1f, 1f, 1f, Chinese ? 1f : 0.55f);
            resetLabel.text = confirmReset
                ? L("Confirm new run", "确认重新开始")
                : L("New run", "重新开始");
            resultResetLabel.text = resetLabel.text;
            nextDayLabel.text = L("Next day  →", "下一天  →");
            todayHeading.text = run.StoryStage == ConstructionStoryStage.FreeBuild
                ? L("YOUR WORLD", "自由规划")
                : run.StoryStage == ConstructionStoryStage.BoardComplete
                    ? L("COMPLETE", "建造完成")
                    : L($"STORY {(int)run.StoryStage + 1:00}/11",
                        $"故事 {(int)run.StoryStage + 1:00}/11");
            reviewHeading.text = L("LAST DAY", "昨日结算");
            routeHeading.text = L("ROUTE", "路线检查");
            progressHeading.text = L("BOARD", "棋盘进度");
            nextStepHeading.text = !run.BuiltToday &&
                run.StoryStage != ConstructionStoryStage.WatchSquirrelEat &&
                !run.IsFinished
                ? L("CURRENT BUILD", "当前建造") : L("NEXT STEP", "下一步");
            RefreshPaletteVisibility();
            resultTitleLabel.text = run.EndReason switch
            {
                ConstructionEndReason.ResidentsGone =>
                    L("ALL RESIDENTS LEFT", "居民全部离开"),
                ConstructionEndReason.WildlifeGone =>
                    L("WILDLIFE LOST", "动物种群消失"),
                _ => L("49 CELLS COMPLETE", "49 格建造完成")
            };
            foreach (var pair in categoryButtons)
                pair.Value.GetComponentInChildren<Text>().text = CategoryLabel(pair.Key);
            foreach (var pair in plantingButtons)
                pair.Value.GetComponentInChildren<Text>().text = PlantingLabel(pair.Key);
            foreach (var pair in metricCaptions)
                pair.Key.text = pair.Value();
        }

        private void Refresh()
        {
            if (run == null || dayText == null) return;
            RefreshStaticLabels();
            var humanPreview = run.PreviewHumanDay();
            dayText.text = run.CurrentDay.ToString();
            if (run.IsFinished)
                scoreText.text = L(
                    $"Animal meals {run.AnimalScore} · Workdays {run.HumanScore}\n" +
                    "Run ended · final scores below",
                    $"动物进食 {run.AnimalScore} 份 · 完整通勤 {run.HumanScore} 次\n" +
                    "本局已结束 · 请查看最终得分");
            else
            {
                var ecologyPreview = run.PreviewEcologyDay();
                var pigeonForecast = ecologyPreview.pigeonsPresent == 0
                    ? string.Empty
                    : L($"\nPigeons {ecologyPreview.pigeonsFed}/" +
                        $"{ecologyPreview.pigeonsPresent}",
                        $"\n鸽群 {ecologyPreview.pigeonsFed}/{ecologyPreview.pigeonsPresent}");
                scoreText.text = L(
                    $"Total meals {run.AnimalScore} · Work {run.HumanScore}\n" +
                    $"Next: Squirrel {ecologyPreview.squirrelsFed}/" +
                    $"{ecologyPreview.squirrelsPresent} · Work " +
                    $"{humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}" +
                    pigeonForecast +
                    (ecologyPreview.predatedSpecies == ConstructionPreySpecies.None
                        ? "" : $"\nFox may hunt 1 {PreyName(ecologyPreview.predatedSpecies, false)}"),
                    $"累计进食{run.AnimalScore} · 通勤{run.HumanScore}\n" +
                    $"明日 松鼠{ecologyPreview.squirrelsFed}/{ecologyPreview.squirrelsPresent} · " +
                    $"通勤{humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}" +
                    pigeonForecast +
                    (ecologyPreview.predatedSpecies == ConstructionPreySpecies.None
                        ? "" : $"\n狐狸可能捕食 1 只{PreyName(ecologyPreview.predatedSpecies, true)}"));
            }
            animalMetricText.text = run.AnimalScore.ToString();
            humanMetricText.text = run.HumanScore.ToString();
            RefreshDayReview();
            storyText.text = StoryInstruction(run);
            storyPreviewText.text = StoryShortInstruction(run);
            storyCueIcon.sprite = StoryCueSprite(run);
            visitorMarker.SetActive(run.StoryStage ==
                ConstructionStoryStage.WelcomeResident && run.PigeonMeadowInspected);
            progressText.text = $"{run.BuiltCount} / {run.BoardCapacity}";
            nextDayButton.interactable = run.CanAdvanceConstructionDay;
            if (buildPaletteButton != null)
                buildPaletteButton.gameObject.SetActive(!run.IsFinished &&
                    run.StoryStage != ConstructionStoryStage.WatchSquirrelEat);
            var selectedPlantingToPlace = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var selectedPlotCount = AvailablePlotCount(selectedCategory,
                selectedPlantingToPlace);
            placementGuideText.text = run.IsFinished
                ? L("Run ended · review the observed results.",
                    "本局已结束 · 查看实际结算结果。")
                : run.StoryStage == ConstructionStoryStage.WatchSquirrelEat
                    ? openingOakInspected
                        ? L("End Day 1 to see the squirrel eat and the date change.",
                            "结束第 1 天，查看松鼠进食和日期变化。")
                        : L("Click the centre oak.", "点击中央橡树。")
                : run.BuiltToday
                    ? L($"Selected: {SelectedBuildLabel()}. Built today; advance to place it tomorrow.",
                        $"已选{SelectedBuildLabel()}。今日已建造，明天可放置。")
                    : run.StoryStage == ConstructionStoryStage.GrowGreen &&
                      (selectedCategory != ConstructionCategory.Green ||
                       selectedPlanting != GreenPlanting.Meadow)
                        ? L($"Selected: {SelectedBuildLabel()}. Story tip: Meadow green.",
                            $"已选{SelectedBuildLabel()}。故事建议：草地绿地。")
                    : selectedPlotCount == 0
                        ? L($"Selected: {SelectedBuildLabel()}. {PlacementPrerequisite(selectedCategory)}.",
                            $"已选{SelectedBuildLabel()}。{PlacementPrerequisite(selectedCategory)}。")
                        : L($"Selected: {SelectedBuildLabel()}. Preview on a + plot ({selectedPlotCount} valid).",
                            $"已选{SelectedBuildLabel()}。点击＋地块预览（可放{selectedPlotCount}处）。");
            selectedBuildIcon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(
                selectedCategory, selectedPlanting));
            plantingPanel.SetActive(selectedCategory == ConstructionCategory.Green &&
                run.IsUnlocked(ConstructionCategory.Green));

            foreach (var pair in categoryButtons)
            {
                var unlocked = CanSelectCategory(pair.Key) && !run.IsFinished;
                var categoryPlotCount = unlocked ? AvailablePlotCount(pair.Key,
                    pair.Key == ConstructionCategory.Green ? selectedPlanting :
                        GreenPlanting.None) : 0;
                // Players can select any building after the oak lesson. A
                // zero-site choice explains the spatial rule, not a story lock.
                pair.Value.interactable = !run.IsFinished;
                pair.Value.GetComponent<Image>().color = pair.Key == selectedCategory
                        ? new Color(0.73f, 0.96f, 0.88f) : Color.white;
                var status = categoryStatusLabels[pair.Key];
                status.text = run.IsFinished
                    ? L("Run ended", "本局结束")
                    : !unlocked
                        ? L("Day 1", "第 1 天")
                    : categoryPlotCount == 0
                        ? PlacementPrerequisite(pair.Key)
                        : L($"{categoryPlotCount} plots", $"可放{categoryPlotCount}处");
                status.color = categoryPlotCount == 0 ? Gold : Paper;
                categoryCountLabels[pair.Key].text = unlocked && categoryPlotCount > 0
                    ? categoryPlotCount.ToString() : "–";
            }
            if (paletteNotice != null)
                paletteNotice.text = L($"Selected: {SelectedBuildLabel()}",
                    $"已选：{SelectedBuildLabel()}");
            foreach (var pair in plantingButtons)
                pair.Value.GetComponent<Image>().color = pair.Key == selectedPlanting
                    ? new Color(0.73f, 0.96f, 0.88f) : Color.white;

            var mayBuild = !run.IsFinished && !run.BuiltToday &&
                CanSelectCategory(selectedCategory);
            var plantingToPlace = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            foreach (var control in panControls)
                control.SetActive(run.Mode == ConstructionGameMode.Endless &&
                    run.ExpansionRings > 0);
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var worldColumn = viewMinColumn + column;
                var worldRow = viewMinRow + row;
                var tile = run.At(worldColumn, worldRow);
                var canPlaceHere = tile == null && mayBuild &&
                    run.CheckPlacement(worldColumn, worldRow, selectedCategory,
                        plantingToPlace) == ConstructionPlacementFailure.None;
                var isPending = hasPendingPlacement &&
                    worldColumn == pendingColumn && worldRow == pendingRow;
                buildableHints[column, row].gameObject.SetActive(
                    canPlaceHere && !isPending);
                placementPreviewFrames[column, row].gameObject.SetActive(isPending);
                if (isPending)
                    placementPreviewIcons[column, row].sprite =
                        RoomIconCatalog.GetSprite(RoomTypeFor(pendingCategory,
                            pendingPlanting));
                var icon = cellIcons[column, row];
                icon.gameObject.SetActive(tile != null &&
                    (worldView == null || !worldView.IsReady));
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
                // Keep the count badge, but let the moving 3D squirrel be the
                // animal the player actually sees on a rendered board.
                squirrelIcons[column, row].color = new Color(1f, 1f, 1f,
                    worldView != null && worldView.IsReady ? 0f : 1f);
                squirrelCounts[column, row].text =
                    run.SquirrelPopulation.ToString();
                pigeonIcons[column, row].gameObject.SetActive(tile != null &&
                    tile.id == run.PigeonTileId);
                pigeonIcons[column, row].color = new Color(1f, 1f, 1f,
                    worldView != null && worldView.IsReady ? 0f : 1f);
                pigeonCounts[column, row].text =
                    run.PigeonPopulation.ToString();
                var isHome = tile?.category == ConstructionCategory.Residence &&
                    run.HasResident(tile.id);
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
                cellBackgrounds[column, row].color = worldView != null &&
                    worldView.IsReady
                    ? tile != null ? Color.clear : canPlaceHere
                        ? new Color(0.61f, 0.87f, 0.72f, 0.16f)
                        : Color.clear
                    : tile == null
                        ? canPlaceHere ? new Color(0.83f, 0.92f, 0.84f)
                            : new Color(0.92f, 0.90f, 0.83f)
                        : TileColor(tile.category);
                if (tile == null) continue;
                icon.sprite = RoomIconCatalog.GetSprite(RoomTypeFor(tile.category,
                    tile.planting));
                icon.color = Color.white;
                var size = tile.category == ConstructionCategory.Green &&
                           tile.id != ConstructionBoardModel.StarterOakId
                    ? 29f + run.GreenGrowthStage(tile.id) * 9f : 47f;
                cellIconRects[column, row].sizeDelta = new Vector2(size, size);
            }
            worldView?.SetViewport(viewMinColumn, viewMinRow);
            worldView?.Sync(run, false);
            RefreshRouteInspection(humanPreview);
            var foxForecast = run.IsFinished ? null : run.PreviewEcologyDay();
            predationRiskText = foxForecast == null ||
                foxForecast.predatedSpecies == ConstructionPreySpecies.None
                ? null : L($"Tomorrow: fox may hunt one " +
                    $"{PreyName(foxForecast.predatedSpecies, false)} at " +
                    $"{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), foxForecast.predationTileId)}.",
                    $"明日风险：狐狸可能在" +
                    $"{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), foxForecast.predationTileId)}" +
                    $"捕食 1 只{PreyName(foxForecast.predatedSpecies, true)}。");
            feedbackPanel.gameObject.SetActive(!hasPendingPlacement &&
                (!string.IsNullOrWhiteSpace(feedbackText.text) ||
                 !string.IsNullOrEmpty(predationRiskText)));
            feedbackPreviewText.text = string.IsNullOrEmpty(predationRiskText)
                ? ShortPreview(feedbackText.text, 43)
                : L($"Fox risk · 1 {PreyName(foxForecast.predatedSpecies, false)}",
                    $"狐狸捕食风险 · 1 只{PreyName(foxForecast.predatedSpecies, true)}");
            HideTooltip();
            RefreshPendingPlacement();
            var result = run.FinalResult;
            finalResultPanel.SetActive(result != null);
            if (result != null)
                finalResultText.text = L(
                    $"Animal score  {result.AnimalMealsPerDay:F2} meals/day\n" +
                    $"Human score  {result.HumanWorkdaysPerDay:F2} workdays/day\n\n" +
                    $"Observed totals: {result.AnimalMealsTotal} meals · " +
                    $"{result.HumanWorkdaysTotal} workdays\n" +
                    $"Completed in {result.DaysPlayed} days.\n\n" +
                    "Scores are per-day averages; waiting cannot increase them without limit.",
                    $"动物得分  {result.AnimalMealsPerDay:F2} 份／天\n" +
                    $"人类得分  {result.HumanWorkdaysPerDay:F2} 次／天\n\n" +
                    $"累计：动物进食 {result.AnimalMealsTotal} 份 · " +
                    $"完整通勤 {result.HumanWorkdaysTotal} 次\n" +
                    $"用时 {result.DaysPlayed} 天。\n\n" +
                    "得分按日均计算，单纯等待不会无限提高得分。");
            RefreshTutorialFocus();
        }

        private void RefreshDayReview()
        {
            var ecology = run.LastEcologyDay;
            var human = run.LastHumanDay;
            var waste = run.LastWasteDay;
            var forecast = run.LastForecast;
            if (ecology == null || human == null || waste == null)
            {
                dayReviewPanel.gameObject.SetActive(false);
                dayAnimalMetricText.text = dayHumanMetricText.text =
                    dayWasteMetricText.text = "–";
                routePanel.rectTransform.anchoredPosition =
                    new Vector2(-568, -53);
                dayReviewText.text = string.Empty;
                dayReviewText.color = Ink;
                return;
            }
            dayReviewPanel.gameObject.SetActive(true);
            dayAnimalMetricText.text = ecology.AnimalMeals.ToString();
            dayHumanMetricText.text = human.CompletedWorkCycles.ToString();
            dayWasteMetricText.text = waste.Cleared.ToString();
            routePanel.rectTransform.anchoredPosition = new Vector2(-568, -145);
            if (forecast == null)
            {
                dayReviewText.text = L(
                    $"DAY {ecology.day} · Animal {ecology.AnimalMeals} " +
                    $"· Work {human.CompletedWorkCycles}\nForecast not saved in this older run.",
                    $"第 {ecology.day} 天 · 进食 {ecology.AnimalMeals} · " +
                    $"通勤 {human.CompletedWorkCycles}\n旧存档未记录预计值。");
                dayReviewText.color = Ink;
                return;
            }
            var matches = ecology.AnimalMeals == forecast.animalMeals &&
                human.CompletedWorkCycles == forecast.completedWorkCycles &&
                waste.Cleared == forecast.wasteCleared;
            var issue = DayIssue(ecology, human, forecast);
            dayReviewText.text = L(
                $"D{ecology.day}  Meals {ecology.AnimalMeals}/" +
                $"{forecast.animalMeals} · Work {human.CompletedWorkCycles}/" +
                $"{forecast.completedWorkCycles}\nClean {waste.Cleared}/" +
                $"{forecast.wasteCleared} · actual/plan\n" +
                (matches ? issue : "Forecast differed from actual; inspect result."),
                $"第{ecology.day}天  进食 {ecology.AnimalMeals}/{forecast.animalMeals} · " +
                $"通勤 {human.CompletedWorkCycles}/{forecast.completedWorkCycles}\n" +
                $"清运 {waste.Cleared}/{forecast.wasteCleared} · 实际/预计\n" +
                (matches ? issue : "预计与实际不符，请检查结果。"));
            dayReviewText.color = matches && issue == L("No unmet need observed.",
                    "未发现未满足的需求。")
                ? Ink : new Color(0.55f, 0.27f, 0.16f);
        }

        private string DayIssue(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human, ConstructionDayForecast forecast)
        {
            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            if (ecology.predatedSpecies != ConstructionPreySpecies.None)
            {
                var location = TileCoordinate(built, ecology.predationTileId);
                return L($"Fox hunted one {PreyName(ecology.predatedSpecies, false)} at {location}.",
                    $"狐狸在{location}捕食 1 只{PreyName(ecology.predatedSpecies, true)}。");
            }
            if (forecast.blockedHomeTileIds.Count > 0)
            {
                var location = TileCoordinate(built, forecast.blockedHomeTileIds[0]);
                return L($"Waste blocked home {location}.",
                    $"{location} 住宅被垃圾阻断。");
            }
            if (forecast.blockedFoodTileIds.Count > 0 &&
                human.MealsEaten < human.ResidentCount)
            {
                var location = TileCoordinate(built, forecast.blockedFoodTileIds[0]);
                return L($"Waste closed food at {location}.",
                    $"{location} 餐饮点因垃圾停用。");
            }
            if (human.restaurantRejectedForCapacity > 0)
                return L($"Restaurant full for {human.restaurantRejectedForCapacity}.",
                    $"餐厅满员，{human.restaurantRejectedForCapacity} 人未获餐食。");
            if (human.CompletedWorkCycles < human.ResidentCount)
                return L($"{human.ResidentCount - human.CompletedWorkCycles} " +
                    "resident(s) missed work or food.",
                    $"{human.ResidentCount - human.CompletedWorkCycles} 人未完成上班或用餐。");
            if (ecology.squirrelsFed < ecology.squirrelsPresent)
                return L($"Squirrels ate {ecology.squirrelsFed}/" +
                    $"{ecology.squirrelsPresent}; oak food short.",
                    $"松鼠进食 {ecology.squirrelsFed}/{ecology.squirrelsPresent}；橡果不足。");
            if (!ecology.squirrelAte)
                return L("Squirrel found no oak food.", "松鼠没有找到橡果。");
            if (run.PigeonArrivalDay > 0 && run.PigeonArrivalDay <= ecology.day &&
                ecology.pigeonsFed < ecology.pigeonsPresent)
                return L($"Pigeons ate {ecology.pigeonsFed}/" +
                    $"{ecology.pigeonsPresent}; meadow food short.",
                    $"鸽群进食 {ecology.pigeonsFed}/{ecology.pigeonsPresent}；草籽不足。");
            return L("No unmet need observed.", "未发现未满足的需求。");
        }

        private static string PreyName(ConstructionPreySpecies species, bool chinese) =>
            species switch
            {
                ConstructionPreySpecies.Pigeon => chinese ? "鸽子" : "pigeon",
                ConstructionPreySpecies.Squirrel => chinese ? "松鼠" : "squirrel",
                ConstructionPreySpecies.Hedgehog => chinese ? "刺猬" : "hedgehog",
                _ => chinese ? "动物" : "animal"
            };

        private void RefreshRouteInspection(ConstructionHumanDayResult humanPreview)
        {
            foreach (Transform child in routeOverlay)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            if (routeSubject == RouteSubject.None)
            {
                routeText.text = L(
                    "Click a resident, squirrel or pigeon flock to inspect its next-day route.",
                    "点击居民、松鼠或鸽群，查看其明日预计路线。");
                routePanel.gameObject.SetActive(false);
                return;
            }
            routePanel.gameObject.SetActive(true);
            if (run.IsFinished)
            {
                routeText.text = L(
                    "This run has ended. There is no next-day route to preview.",
                    "本局已结束，不再预览次日路线。");
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
                    routeText.text = L("Select a resident to inspect its next-day route.",
                        "请选择居民查看明日预计路线。");
                    return;
                }
                route = resident.routeTileIds.Where(built.ContainsKey)
                    .Select(id => built[id]).ToArray();
                var availability = run.PreviewWasteAvailability();
                if (availability.BlockedHomes.Contains(selectedHomeId))
                    routeText.text = L(
                        "NEXT DAY · RESIDENT\nWaste at home blocks the outing. Build cleanup nearby.",
                        "明日 · 居民\n住宅垃圾阻断外出，请在附近建垃圾间。");
                else if (run.StoryStage == ConstructionStoryStage.BuildRestaurant &&
                         !resident.ate)
                    routeText.text = L(
                        "NEXT DAY · RESIDENT\nNo meal route. Add restaurant.\n" +
                        "Work explained after eating.",
                        "明日 · 居民\n无用餐路线，请建餐厅。\n" +
                        "首次用餐后介绍工作。");
                else
                {
                    var work = resident.worked
                        ? L($"work {TileCoordinate(built, resident.workTileId)}",
                            $"上班 {TileCoordinate(built, resident.workTileId)}") :
                        run.ResidenceCanReachWork(selectedHomeId)
                            ? L("work full", "岗位已满") : L("no work route", "无法到工作坊");
                    var meal = resident.ate
                        ? L($"meal {TileCoordinate(built, resident.mealTileId)}",
                            $"用餐 {TileCoordinate(built, resident.mealTileId)}") :
                        !run.ResidenceCanReachFood(selectedHomeId)
                            ? L("no meal route", "无法到餐饮点")
                            : ReachableFoodClosedByWaste(selectedHomeId,
                                availability.BlockedFoodServices)
                                ? L("meal closed by waste", "餐饮点因垃圾停用")
                                : L("meal full", "餐位已满");
                    routeText.text = L($"NEXT DAY · RESIDENT\n{work} · {meal} · returns home",
                        $"明日 · 居民\n{work} · {meal} · 回家");
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
                    routeText.text = L("No animal is currently here.",
                        "这里目前没有动物。");
                    return;
                }
                route = squirrel ? run.PreviewSquirrelFoodRoute() :
                    run.PreviewPigeonFoodRoute();
                var target = route.Count > 0 ? route[route.Count - 1] : null;
                var oakCount = run.MatureNearbyOakCount;
                var oakProgress = oakCount >= 5
                    ? L("Local oak area full", "附近橡树栖地已满")
                    : L($"Next squirrel: {oakCount}/" +
                        $"{ConstructionFoodModel.OaksForNextSquirrel(ecology.squirrelsPresent)} " +
                        "mature nearby oaks",
                        $"吸引下一只松鼠：成熟邻近橡树 {oakCount}/" +
                        $"{ConstructionFoodModel.OaksForNextSquirrel(ecology.squirrelsPresent)}");
                var meadowCount = run.MatureConnectedMeadowCount;
                var meadowProgress = L($"Next pigeon: {meadowCount}/" +
                    $"{ConstructionFoodModel.MeadowsForNextPigeon(ecology.pigeonsPresent)} " +
                    "connected mature meadows",
                    $"吸引下一只鸽子：相连成熟草地 {meadowCount}/" +
                    $"{ConstructionFoodModel.MeadowsForNextPigeon(ecology.pigeonsPresent)}");
                routeText.text = squirrel
                    ? target == null
                        ? L($"NEXT DAY · SQUIRRELS · can eat {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent} · no oak food\n{oakProgress}",
                            $"明日 · 松鼠 {ecology.squirrelsFed}/{ecology.squirrelsPresent} " +
                            $"可进食 · 无橡果\n{oakProgress}")
                        : L($"NEXT DAY · SQUIRRELS · can eat {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent} from " +
                            $"{ecology.squirrelFoodTileIds.Count} oak(s)\n{oakProgress}",
                            $"明日 · 松鼠 {ecology.squirrelsFed}/{ecology.squirrelsPresent} " +
                            $"可进食 · {ecology.squirrelFoodTileIds.Count} 处橡树\n{oakProgress}")
                    : target == null
                        ? L($"NEXT DAY · PIGEONS · can eat {ecology.pigeonsFed}/" +
                            $"{ecology.pigeonsPresent} · no meadow food\n{meadowProgress}",
                            $"明日 · 鸽群 {ecology.pigeonsFed}/{ecology.pigeonsPresent} " +
                            $"可进食 · 无草籽\n{meadowProgress}")
                        : L($"NEXT DAY · PIGEONS · can eat {ecology.pigeonsFed}/" +
                            $"{ecology.pigeonsPresent} from " +
                            $"{ecology.pigeonFoodTileIds.Count} meadow(s)\n{meadowProgress}",
                            $"明日 · 鸽群 {ecology.pigeonsFed}/{ecology.pigeonsPresent} " +
                            $"可进食 · {ecology.pigeonFoodTileIds.Count} 处草地\n{meadowProgress}");
                var routes = squirrel ? run.PreviewSquirrelFoodRoutes() :
                    run.PreviewPigeonFoodRoutes();
                if (routes.Count > 0)
                {
                    foreach (var foodRoute in routes)
                        DrawRoute(foodRoute, origin, colour);
                    return;
                }
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

        private Vector2 BoardPoint(ConstructionTileData tile) =>
            new((tile.column - viewMinColumn - 3) * BoardCellSpacing,
                (3 - tile.row + viewMinRow) * BoardCellSpacing);

        public bool Pan(int columns, int rows)
        {
            if (run == null || run.Mode != ConstructionGameMode.Endless ||
                run.ExpansionRings == 0) return false;
            var nextColumn = Mathf.Clamp(viewMinColumn + columns,
                run.MinCoordinate, run.MaxCoordinate - 6);
            var nextRow = Mathf.Clamp(viewMinRow + rows,
                run.MinCoordinate, run.MaxCoordinate - 6);
            if (nextColumn == viewMinColumn && nextRow == viewMinRow) return false;
            ClearPendingPlacement();
            viewMinColumn = nextColumn;
            viewMinRow = nextRow;
            Refresh();
            return true;
        }

        private static string TileCoordinate(
            IReadOnlyDictionary<string, ConstructionTileData> built, string tileId) =>
            tileId != null && built.TryGetValue(tileId, out var tile)
                ? $"{tile.column + 1},{tile.row + 1}" : "?";

        private void OnResetPressed()
        {
            if (hasPendingPlacement)
            {
                ClearPendingPlacement();
                Refresh();
            }
            if (!confirmReset)
            {
                confirmReset = true;
                resetLabel.text = L("Confirm new run", "确认重新开始");
                if (resultResetLabel != null)
                    resultResetLabel.text = resetLabel.text;
                feedbackText.text = L("Press again to erase this construction save.",
                    "再次点击将清除当前建造存档。");
                if (resultNoticeText != null)
                    resultNoticeText.text = feedbackText.text;
                return;
            }
            confirmReset = false;
            resetLabel.text = L("New run", "重新开始");
            if (resultResetLabel != null) resultResetLabel.text = resetLabel.text;
            if (resultNoticeText != null) resultNoticeText.text = "";
            var newRun = new ConstructionRunModel(run.Mode);
            if (store != null && !store.TrySave(newRun))
            {
                feedbackText.text = L(
                    "Could not save the new run; current run was not reset.",
                    "新局存档失败；原进度未被重置。");
                if (resultNoticeText != null)
                    resultNoticeText.text = L("New run could not be saved. Run not reset.",
                        "存档失败，未重新开始。");
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
            worldView?.ClearBuiltWorld();
            run = newRun;
            viewMinColumn = 0;
            viewMinRow = 0;
            routeSubject = RouteSubject.None;
            selectedHomeId = null;
            selectedCategory = ConstructionCategory.Green;
            selectedPlanting = GreenPlanting.Oak;
            openingOakInspected = false;
            feedbackText.text = L("New run: one oak and one squirrel.",
                "新局开始：一棵橡树，一只松鼠。");
            Refresh();
        }

        private void ClearResetConfirmation()
        {
            if (!confirmReset) return;
            confirmReset = false;
            if (resetLabel != null) resetLabel.text = L("New run", "重新开始");
            if (resultResetLabel != null) resultResetLabel.text = L("New run", "重新开始");
            if (resultNoticeText != null) resultNoticeText.text = "";
        }

        private void Save()
        {
            if (store != null && !store.TrySave(run))
                feedbackText.text = L("Save failed. Current changes may be lost on exit.",
                    "存档失败。退出后可能丢失本次改动。");
        }

        private string StoryShortInstruction(ConstructionRunModel current) =>
            current.StoryStage switch
            {
                ConstructionStoryStage.WatchSquirrelEat => openingOakInspected
                    ? L("Press Next day: watch the squirrel find its first meal.",
                        "点击「下一天」，看松鼠吃到第一份食物。")
                    : L("Meet the squirrel. Click its oak in the centre.",
                        "先认识松鼠：点击中央的橡树房间。"),
                ConstructionStoryStage.GrowGreen => current.HasConnectedMeadow
                    ? L("The meadow is ready. Advance a day to see who arrives.",
                        "草地已建好。推进一天，看看谁会到来。")
                    : selectedCategory == ConstructionCategory.Green &&
                      selectedPlanting == GreenPlanting.Meadow
                        ? L("Place a meadow beside the oak, then confirm.",
                            "在橡树旁放置草地，再确认建造。")
                        : L("Open Build and choose Meadow for the pigeons.",
                            "打开建造栏，选择草地吸引鸽群。"),
                ConstructionStoryStage.WelcomeResident =>
                    current.PigeonMeadowInspected
                        ? L("A visitor has arrived. Build a home beside the town.",
                            "一位访客来了。为他建一间相邻的住宅。")
                        : L("Pigeons arrived! Click their meadow to inspect it.",
                            "鸽群到了！点击草地看看它们吃什么。"),
                ConstructionStoryStage.BuildRestaurant =>
                    L("The resident is hungry. Add a restaurant by home.",
                        "居民饿了：在住宅旁建餐厅。"),
                ConstructionStoryStage.WatchResidentEat =>
                    L("Advance a day to watch the resident reach dinner.",
                        "推进一天，观察居民能否到餐厅吃饭。"),
                ConstructionStoryStage.BuildWorkshop =>
                    L("Meals alone are not enough. Build a workshop.",
                        "只有餐食还不够：建一间工作坊。"),
                ConstructionStoryStage.BuildWasteRoom =>
                    L("Homes and meals leave waste. Add a waste room.",
                        "居住和餐饮产生垃圾：建一间垃圾间。"),
                ConstructionStoryStage.AwaitAccessProblem =>
                    L("Try another residence.", "尝试建第二间住宅。"),
                ConstructionStoryStage.BuildStreet =>
                    L("A home is cut off. Extend a street to reconnect it.",
                        "有住宅无法到达服务点：用街道接通。"),
                ConstructionStoryStage.AwaitRestaurantCrowding =>
                    current.PreviewWasteAvailability().BlockedHomes.Count > 0 ||
                    current.PreviewWasteAvailability().BlockedFoodServices.Count > 0
                        ? L("Waste blocks a service. Add a nearby waste room.",
                            "垃圾阻断服务：在附近增加垃圾间。")
                        : L("Advance a day and check whether one seat is enough.",
                            "推进一天，看看一个餐位是否够用。"),
                ConstructionStoryStage.BuildSupermarket =>
                    L("The restaurant is full. Add a supermarket.",
                        "餐厅已满：建超市补充餐食。"),
                ConstructionStoryStage.FreeBuild =>
                    current.Mode == ConstructionGameMode.Fill
                        ? L("Shape all 49 plots: balance wildlife meals and workdays.",
                            "规划满 49 格：兼顾动物进食与居民通勤。")
                        : L("Keep building outward while people and wildlife thrive.",
                            "继续向外建造，让居民和动物都能生存。"),
                _ => L("Review the finished board.", "查看建造结果。")
            };

        private Sprite StoryCueSprite(ConstructionRunModel current)
        {
            var category = current.StoryStage switch
            {
                ConstructionStoryStage.WatchSquirrelEat or
                    ConstructionStoryStage.GrowGreen => ConstructionCategory.Green,
                ConstructionStoryStage.WelcomeResident =>
                    current.PigeonMeadowInspected
                        ? ConstructionCategory.Residence : ConstructionCategory.Green,
                ConstructionStoryStage.AwaitAccessProblem =>
                    ConstructionCategory.Residence,
                ConstructionStoryStage.BuildRestaurant or
                    ConstructionStoryStage.WatchResidentEat =>
                    ConstructionCategory.Restaurant,
                ConstructionStoryStage.AwaitRestaurantCrowding =>
                    current.PreviewWasteAvailability().BlockedHomes.Count > 0 ||
                    current.PreviewWasteAvailability().BlockedFoodServices.Count > 0
                        ? ConstructionCategory.Waste : ConstructionCategory.Restaurant,
                ConstructionStoryStage.BuildWorkshop => ConstructionCategory.Workshop,
                ConstructionStoryStage.BuildWasteRoom => ConstructionCategory.Waste,
                ConstructionStoryStage.BuildStreet => ConstructionCategory.Street,
                ConstructionStoryStage.BuildSupermarket =>
                    ConstructionCategory.Supermarket,
                ConstructionStoryStage.FreeBuild => selectedCategory,
                _ => ConstructionCategory.Square
            };
            var planting = current.StoryStage == ConstructionStoryStage.GrowGreen ||
                current.StoryStage == ConstructionStoryStage.WelcomeResident &&
                !current.PigeonMeadowInspected
                ? GreenPlanting.Meadow : current.StoryStage ==
                    ConstructionStoryStage.FreeBuild ? selectedPlanting :
                    GreenPlanting.Oak;
            return RoomIconCatalog.GetSprite(RoomTypeFor(category, planting));
        }

        private string StoryInstruction(ConstructionRunModel run) => run.StoryStage switch
        {
            ConstructionStoryStage.WatchSquirrelEat =>
                openingOakInspected
                    ? L("The oak feeds the squirrel. End Day 1 to settle its meal, plant growth and the date.",
                        "橡树为松鼠提供食物。结束第 1 天，结算进食、植物生长和日期。")
                    : L("One squirrel lives in the central oak. Click that room to inspect its plant, food stock and the animal it can feed.",
                        "中央橡树里住着一只松鼠。点击房间查看植物、食物存量以及能被喂养的动物。"),
            ConstructionStoryStage.GrowGreen =>
                run.HasConnectedMeadow
                    ? L("A connected meadow is ready. Advance one day to welcome a pigeon flock.",
                        "相连草地已建好。推进一天，等待鸽群到来。")
                    : run.ResidentPopulation > 0
                        ? L("A resident arrived early. Check their food and work routes; a connected meadow can still attract pigeons.",
                            "居民已先搬来。留意餐饮与工作路线；相连草地仍可吸引鸽群。")
                    : L("Open Build, select Meadow, then click a + plot touching the oak or connected green. Confirm the placement. Plants grow as days pass; pigeons may arrive after settlement.",
                        "打开建造栏选「草地」，点击与橡树或相连绿地上下左右相邻的＋地块，再确认建造。植物随日期生长；结算后鸽群可能到来。"),
            ConstructionStoryStage.WelcomeResident =>
                run.PigeonMeadowInspected
                    ? L("The pigeons will eat meadow seeds from tomorrow. A visitor is here: build a residence.",
                        "鸽群从明天开始吃草籽。一位访客到来，请建一间住宅。")
                    : L("Pigeons arrived on the meadow. Click their room to learn what feeds them.",
                        "鸽群来到草地。先点击它们的房间，了解它们吃什么。"),
            ConstructionStoryStage.BuildRestaurant =>
                L("A resident needs one meal each day. Build a one-seat restaurant beside their home.",
                    "居民每天需要一份餐食。请在住宅旁建一间可接待 1 人的餐厅。"),
            ConstructionStoryStage.WatchResidentEat =>
                L("The restaurant is reachable. Advance one day to watch the resident eat.",
                    "餐厅已可到达。推进一天，观察居民用餐。"),
            ConstructionStoryStage.BuildWorkshop =>
                L("The resident ate once. Food needs work to sustain it; build a workshop.",
                    "居民吃到了饭，也需要工作。请建工作坊。"),
            ConstructionStoryStage.BuildWasteRoom =>
                L("Homes and meals make waste. Build a waste room to clear two nearby portions each day.",
                    "住宅和餐饮会产生垃圾。建垃圾间，每天可清运附近两份垃圾。"),
            ConstructionStoryStage.AwaitAccessProblem =>
                L("Add another home beyond direct work or food access. A street can reconnect it.",
                    "建第二间住宅。若无法直达工作或餐饮点，可用街道连接。"),
            ConstructionStoryStage.BuildStreet =>
                L("Build a continuous street the new resident uses to reach food or work. Both homes need restaurant access.",
                    "建连续街道，让新居民通过它到餐厅或工作坊；两户都需能到餐厅。"),
            ConstructionStoryStage.AwaitRestaurantCrowding =>
                run.PreviewWasteAvailability().BlockedHomes.Count > 0
                    ? L("Waste blocks a home. Build a waste room nearby, then test the restaurant.",
                        "住宅被垃圾阻断。先在附近建垃圾间，再测试餐厅容量。")
                    : run.PreviewWasteAvailability().BlockedFoodServices.Count > 0
                        ? L("Waste closed the restaurant. Build a waste room nearby before testing seats.",
                            "餐厅因垃圾停用。先建垃圾间，再测试座位。")
                        : L("Both homes reach the restaurant. Advance a day to test its one seat.",
                            "两户都能到餐厅。推进一天，看看一个餐位是否足够。"),
            ConstructionStoryStage.BuildSupermarket =>
                L("Build a supermarket to add two daily meals for residents.",
                    "建超市，每天可额外供应两份居民餐食。"),
            ConstructionStoryStage.FreeBuild =>
                run.Mode == ConstructionGameMode.Fill
                    ? L("Fill 49 plots, one per day. Score animal meals and complete resident work–meal–home routes.",
                        "每天建一格，填满 49 格。动物实际进食与居民完整通勤分别计分。")
                    : L("Build outward while both species survive. Score animal meals and full resident routines.",
                        "继续向外建造，维持人与动物生存；实际进食与完整通勤分别计分。"),
            _ => L("All 49 cells are built. The final day has been counted.",
                "49 格已建满，最后一天已完成结算。")
        };

        private string PlacementMessage(ConstructionPlacementFailure failure) => failure switch
        {
            ConstructionPlacementFailure.Disconnected =>
                L("Build beside an existing cell, not diagonally.", "须紧邻已有格，斜角不算相邻。"),
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet =>
                L("This building needs a home or a street connected to one.",
                    "这类建筑须挨着住宅或与住宅相连的街道。"),
            ConstructionPlacementFailure.NeedsHomeOrRestaurant =>
                L("A waste room must touch a home or restaurant.",
                    "垃圾间须紧邻住宅或餐厅。"),
            ConstructionPlacementFailure.StreetNeedsHomeConnection =>
                L("A street must extend from a home or connected street.",
                    "街道须从住宅或已接通街道延伸。"),
            ConstructionPlacementFailure.Occupied =>
                L("That cell is already built.", "这格已有建筑。"),
            _ => L("This cell cannot be built here.", "这格不能建造所选建筑。")
        };

        private string TileLabel(ConstructionTileData tile) =>
            tile.category == ConstructionCategory.Green
                ? L($"{PlantingLabel(tile.planting)} green",
                    $"{PlantingLabel(tile.planting)}绿地")
                : CategoryLabel(tile.category);

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
            textWeights.Add(label, bold);
            label.font = Chinese ? chineseFont : bold ? boldFont : regularFont;
            label.fontStyle = Chinese && bold ? FontStyle.Bold : FontStyle.Normal;
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
            Color foreground, Action onClick, bool themed = true)
        {
            var image = Panel(name, parent, position, size, background);
            if (themed)
                ApplySkin(image, "UI/PauseMenu/pause-menu-button-base-v01");
            image.color = image.sprite == null ? background : Color.white;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(label))
                TextAt("Label", button.transform, Vector2.zero, size, label, fontSize,
                    true, foreground, TextAnchor.MiddleCenter).raycastTarget = false;
            return button;
        }

        private void ApplySkin(Image image, string path)
        {
            if (!skinnedSprites.TryGetValue(path, out var sprite))
            {
                // These painted panels have transparent decorative margins.
                // Nine-slicing a runtime sprite erased their opaque centre in
                // the construction canvas; the original proportions are close
                // to the target cards, so render the whole artwork instead.
                sprite = LoadSprite(path);
                if (sprite == null) return;
                skinnedSprites.Add(path, sprite);
            }
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        private void ApplyLanguageFonts()
        {
            foreach (var pair in textWeights)
            {
                if (pair.Key == null) continue;
                pair.Key.font = Chinese ? chineseFont : pair.Value ? boldFont : regularFont;
                pair.Key.fontStyle = Chinese && pair.Value
                    ? FontStyle.Bold : FontStyle.Normal;
            }
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
