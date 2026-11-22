using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.CameraRecognition;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    // A separate vertical slice while the legacy full-board simulation is
    // migrated. Green food and squirrel/pigeon meals now settle each day;
    // daily home/work/food capacity now settles in the independent prototype.
    public sealed partial class ConstructionPrototypeController : MonoBehaviour
    {
        private const string LanguagePreferenceKey = "symbiosis49.language";
        private const string LeaderboardNicknamePreferenceKey =
            "symbiosis49.leaderboard.nickname";
        private const string LeftPanelIconRoot =
            "UI/ConstructionCase/LeftPanelIcons/";
        private const float BoardCellSpacing = 101f;
        private const int OpeningTourSteps = 7;
        private const float TooltipWidth = 360f;
        private const float TooltipTextWidth = 294f;
        private enum RouteSubject
        {
            None, Resident, Squirrel, Pigeons, Hedgehog, Fox
        }
        private enum TooltipLocation { FollowPointer, BoardInfo, LeftLeaf }
        private enum ScoreSide { None, Animal, Resident, Shared, Build }
        private enum RoomBadgeKind
        {
            Growth, HomeBonus, WorkCycle, TrafficDeaths, RoadsideWildlife,
            Utilization, Waste, Food, Squirrel, Pigeon, Hedgehog, Fox,
            Coexistence
        }

        private readonly struct ScoreMotionStep
        {
            public readonly ScoreSide Side;
            public readonly int Delta;
            public readonly string SourceTileId;
            public readonly string EnglishReason;
            public readonly string ChineseReason;
            public readonly bool MoveToTotal;

            public ScoreMotionStep(ScoreSide side, int delta, string sourceTileId,
                string englishReason, string chineseReason, bool moveToTotal = false)
            {
                Side = side;
                Delta = delta;
                SourceTileId = sourceTileId;
                EnglishReason = englishReason;
                ChineseReason = chineseReason;
                MoveToTotal = moveToTotal;
            }
        }

        private static readonly Color Ink = new(0.13f, 0.19f, 0.24f);
        private static readonly Color HeadingInk = Ink;
        private static readonly Color Paper = new(0.93f, 0.89f, 0.79f);
        // Keep the lower instructions legible over the printed city without
        // covering the illustration at the leaf edges and foot.
        private static readonly Color ReadingPaper =
            new(0.96f, 0.92f, 0.84f, 0.95f);
        private static readonly Color Green = new(0.39f, 0.57f, 0.34f);
        private static readonly Color Teal = new(0.26f, 0.63f, 0.60f);
        private static readonly Color Gold = new(0.84f, 0.69f, 0.42f);
        private static readonly Color UsedGreen = new(0.23f, 0.43f, 0.32f);
        private static readonly Color IdleAmber = new(0.62f, 0.39f, 0.17f);
        private static readonly Color BlockedRed = new(0.60f, 0.27f, 0.22f);
        private static readonly Color ScorePlaqueBrass = new(0.72f, 0.58f, 0.36f);
        private static readonly Color ScorePlaqueNumber = new(0.93f, 0.85f, 0.66f);
        private static readonly Color ScorePlaqueMuted = new(0.77f, 0.80f, 0.75f);

        private readonly Dictionary<ConstructionCategory, Button> categoryButtons = new();
        private readonly Dictionary<ConstructionCategory, Image> categoryFaces = new();
        private readonly Dictionary<ConstructionCategory, Text> categoryStatusLabels = new();
        private readonly Dictionary<ConstructionCategory, Text> categoryCountLabels = new();
        private readonly Dictionary<ConstructionCategory, Text> categoryProvisionLabels = new();
        private readonly Dictionary<Text, bool> textWeights = new();
        private readonly Dictionary<Text, Func<string>> metricCaptions = new();
        private readonly Dictionary<string, Sprite> skinnedSprites = new();
        private readonly Image[,] cellBackgrounds = new Image[7, 7];
        private readonly Text[,] buildableHints = new Text[7, 7];
        private readonly Image[,] placementPreviewFrames = new Image[7, 7];
        private readonly Image[,] placementPreviewMats = new Image[7, 7];
        private readonly Image[,] placementPreviewIcons = new Image[7, 7];
        private readonly GameObject[,] upgradePreviewBorders = new GameObject[7, 7];
        private readonly Image[,] cellIcons = new Image[7, 7];
        private readonly RectTransform[,] cellIconRects = new RectTransform[7, 7];
        private readonly Image[,] squirrelIcons = new Image[7, 7];
        private readonly Image[,] pigeonIcons = new Image[7, 7];
        private readonly Image[,] hedgehogIcons = new Image[7, 7];
        private readonly Image[,] foxIcons = new Image[7, 7];
        private readonly Text[,] squirrelCounts = new Text[7, 7];
        private readonly Text[,] pigeonCounts = new Text[7, 7];
        private readonly Text[,] hedgehogCounts = new Text[7, 7];
        private readonly Text[,] foxCounts = new Text[7, 7];
        private readonly Text[,] foodBadges = new Text[7, 7];
        private readonly Text[,] capacityBadges = new Text[7, 7];
        private readonly Text[,] utilizationSymbols = new Text[7, 7];
        private readonly Text[,] wasteBadges = new Text[7, 7];
        private readonly Text[,] growthBadges = new Text[7, 7];
        private readonly Text[,] residenceBonusBadges = new Text[7, 7];
        private readonly Text[,] workCycleBadges = new Text[7, 7];
        private readonly Text[,] streetDeathBadges = new Text[7, 7];
        private readonly Text[,] streetExposureBadges = new Text[7, 7];
        private readonly GameObject[,] coexistenceBadges = new GameObject[7, 7];
        private readonly GameObject[,] coexistenceHighlights = new GameObject[7, 7];
        private readonly CanvasGroup[,] cellMetricGroups = new CanvasGroup[7, 7];
        private readonly bool[,] cellMetricHovering = new bool[7, 7];
        private readonly GameObject[,] leftMetricRails = new GameObject[7, 7];
        private readonly GameObject[,] rightMetricRails = new GameObject[7, 7];
        private readonly GameObject[,] residentMarkers = new GameObject[7, 7];
        private readonly GameObject[,] foxRiskMarkers = new GameObject[7, 7];

        private ConstructionRunModel run;
        private float CaseLeafCenter =>
            (openingTourBoardTarget == null
                ? (run != null && run.UsesFillHand ? 342f : 380f)
                : openingTourBoardTarget.rect.width *
                  openingTourBoardTarget.localScale.x * 0.5f) + 191f;
        private float CaseLeafHeight => openingTourBoardTarget == null
            ? (run != null && run.UsesFillHand ? 684f : 760f)
            : openingTourBoardTarget.rect.height *
              openingTourBoardTarget.localScale.y;
        private ConstructionRunStore store;
        private ConstructionLocalLeaderboardStore localLeaderboardStore;
        private bool runSavedAtLeastOnce;
        private bool runNeedsSave;
        private bool openingFromPreparedRestart;
        private ConstructionCategory selectedCategory = ConstructionCategory.Green;
        private GreenPlanting selectedPlanting = GreenPlanting.None;
        private ResidenceKind pendingResidenceKind = ResidenceKind.Standard;
        private readonly Dictionary<ResidenceKind, Image> residenceConditionCards = new();
        private Text residenceConditionHeading;
        private Text balanceScoreText;
        private GameObject endlessScoreStrip;
        private readonly Text[] endlessScoreCaptions = new Text[3];
        private readonly Text[] endlessScoreValues = new Text[3];
        private Button endlessRulesButton;
        private Text endlessRulesButtonLabel;
        private GameObject endlessLossStrip;
        private Text endlessDeathCaption;
        private Text endlessDeathValue;
        private Text endlessUnemployedCaption;
        private Text endlessUnemployedValue;
        private GameObject endlessRulesOverlay;
        private Text endlessRulesTitle;
        private Text endlessRulesIntro;
        private Text endlessRulesDeathLabel;
        private Text endlessRulesDeathValue;
        private Text endlessRulesDeathRecommendation;
        private Text endlessRulesUnemployedLabel;
        private Text endlessRulesUnemployedValue;
        private Text endlessRulesUnemployedRecommendation;
        private Text endlessRulesSummary;
        private Button endlessRulesCancelButton;
        private Text endlessRulesCancelLabel;
        private Text endlessRulesRecommendedLabel;
        private Text endlessRulesConfirmLabel;
        private readonly List<Button> endlessRulesAdjustmentButtons = new();
        private Button endlessRulesRecommendedButton;
        private Button endlessRulesConfirmButton;
        private int draftAnimalDeathLimit;
        private int draftUnemployedResidentLimit;
        private bool endlessRulesNewRunPending;
        private Image progressIcon;
        private bool tutorialGreenChoiceMade;
        private bool introChoiceMade;
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
        private GameObject boardGameCameraPanel;
        private Text boardGameCameraHeading;
        private Text boardGameCameraStatusText;
        private Text boardGameCameraButtonLabel;
        private Button boardGameCameraButton;
        private string boardGameCameraToolError;
        private Text paletteNotice;
        private Image paletteBackdrop;
        private bool buildPaletteOpen;
        private bool cameraCommitInProgress;
        private bool cameraStageInProgress;
        private Text resultTitleLabel;
        private Text dayText;
        private Text scoreText;
        private string predationRiskText;
        private string predationRiskTileId;
        private string focusedPredationTileId;
        private Text dayReviewText;
        private Image dayReviewPanel;
        private Image observationLedger;
        private RectTransform primerRulesRoot;
        private readonly Text[] primerRuleLabels = new Text[3];
        private readonly Text[] primerRuleDetails = new Text[3];
        private Sprite storyOakIcon;
        private Text storyText;
        private Text storyPreviewText;
        private Text roomFunctionText;
        private Button skipTutorialButton;
        private Text skipTutorialLabel;
        private Image storyCueIcon;
        private Image storyDivider;
        private Text todayHeading;
        private Text reviewHeading;
        private Text routeHeading;
        private Text progressHeading;
        private Text nextStepHeading;
        private Text feedbackText;
        private Image feedbackPanel;
        private Image feedbackIcon;
        private Text feedbackHeading;
        private Image feedbackRiskIcon;
        private bool fixedRiskHovered;
        private string selectedInfoTileId;
        private string hoveredInfoTileId;
        private int? hoveredEmptyColumn;
        private int? hoveredEmptyRow;
        private string hoveredBadgeTileId;
        private RoomBadgeKind? hoveredBadgeKind;
        private RectTransform hoveredBadgeAnchor;
        private RectTransform hoveredRiskAnchor;
        private bool hoveredLocalRisk;
        private RectTransform roomBadgeTooltipRect;
        private Text roomBadgeTooltipHeading;
        private Text roomBadgeTooltipDetails;
        private GameObject foxRiskLocator;
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
        private Image tooltipAccent;
        private RectTransform scorePlaque;
        private Texture2D scorePlaqueTexture;
        private Sprite scorePlaqueSprite;
        private Text scorePlaqueEyebrow;
        private Text scorePlaqueTitle;
        private Text scorePlaquePoints;
        private Text scorePlaqueUnit;
        private Text scorePlaqueComparison;
        private Text scorePlaqueExplanation;
        private Text scorePlaqueForecastHeading;
        private readonly Text[] scorePlaqueForecast = new Text[4];
        private Image guidePanel;
        private Image progressPanel;
        private GameObject placementConfirmation;
        private Image placementConfirmationIcon;
        private Text placementConfirmationLocation;
        private Text placementConfirmationNotice;
        private Text placementCancelLabel;
        private Text placementConfirmLabel;
        private Button placementConfirmButton;
        private RectTransform tutorialFocus;
        private RectTransform openingTourRoot;
        private RectTransform openingTourFrame;
        private RectTransform openingTourCard;
        private RectTransform openingTourTodayTarget;
        private RectTransform openingTourReviewTarget;
        private RectTransform openingTourScoreTarget;
        private RectTransform openingTourBoardTarget;
        private RectTransform openingTourProgressTarget;
        private RectTransform openingTourGuideTarget;
        private Text openingTourTitle;
        private Text openingTourBody;
        private Text openingTourCount;
        private Text openingTourFocusIndex;
        private Text openingTourNextLabel;
        private Text openingTourSkipLabel;
        private int openingTourStep;
        private bool hasPendingPlacement;
        private int pendingColumn;
        private int pendingRow;
        private int pendingDay;
        private ConstructionCategory pendingCategory;
        private GreenPlanting pendingPlanting;
        private Text resetLabel;
        private Text resultResetLabel;
        private Text resultMenuLabel;
        private Text resultNoticeText;
        private InputField resultNicknameInput;
        private Text resultNicknamePlaceholder;
        private Text resultLeaderboardStatus;
        private Button resultSubmitButton;
        private Text resultSubmitLabel;
        private Text resultViewRankingLabel;
        private string resultLeaderboardMessage;
        private string resultLeaderboardMessageRunId;
        private string resultLeaderboardRecordedRunId;
        private GameObject localRankingsOverlay;
        private Text localRankingsTitle;
        private Text localRankingsSubtitle;
        private Text localRankingsEmpty;
        private Text localRankingsCloseLabel;
        private readonly Text[] localRankingsHeaders = new Text[6];
        private readonly Text[,] localRankingsCells = new Text[10, 6];
        private readonly Image[] localRankingsRowFaces = new Image[10];
        private readonly Text[] localRankingsModeLabels = new Text[3];
        private readonly Image[] localRankingsModeFaces = new Image[3];
        private ConstructionGameMode localRankingsMode;
        private int resultLeaderboardRank;
        private Text finalResultText;
        private Text resultModeLabel;
        private Text resultTotalCaption;
        private Text resultAnimalCaption;
        private Text resultResidentCaption;
        private Text resultAnimalValue;
        private Text resultResidentValue;
        private Image resultLinkageRibbon;
        private Image resultLinkageDivider;
        private Text resultLinkageValue;
        private Text resultLinkageDetail;
        private readonly Image[] resultEventIcons = new Image[3];
        private readonly Text[] resultEventCaptions = new Text[3];
        private readonly Text[] resultEventValues = new Text[3];
        private Text resultEquationText;
        private Text resultObservedText;
        private GameObject finalResultPanel;
        private Text routeText;
        private Image routePanel;
        private Image routeSuccessIcon;
        private RectTransform routeOverlay;
        private RouteSubject routeSubject;
        private string selectedHomeId;
        private string selectedWildlifeTileId;
        private Button openDispatchButton;
        private Image residentDispatchPanel;
        private Text dispatchHeading;
        private Text dispatchHome;
        private Text dispatchRules;
        private Text dispatchWorkHeading;
        private Text dispatchMealHeading;
        private Text dispatchWorkChoice;
        private Text dispatchMealChoice;
        private Text dispatchPreviewHeading;
        private Text dispatchPreview;
        private Text dispatchBreakdown;
        private string dispatchBreakdownDetails = string.Empty;
        private string dayReviewDetails = string.Empty;
        private Text dispatchScoreHeading;
        private Text dispatchAnimalScore;
        private Text dispatchResidentScore;
        private Text dispatchSharedScore;
        private Text dispatchNote;
        private Text dispatchAutoLabel;
        private Text dispatchCloseLabel;
        private Button dispatchWorkPrevious;
        private Button dispatchWorkNext;
        private Button dispatchMealPrevious;
        private Button dispatchMealNext;
        private bool residentDispatchOpen;
        private GameObject visitorMarker;
        private GameObject movingResidentMarker;
        private bool confirmReset;
        private bool openingOakInspected;
        private bool freeBuildChoiceMade;
        private bool endlessChoiceMade;
        private Coroutine growthAnimation;
        private Coroutine residentMealAnimation;
        private Coroutine scoreMotionAnimation;
        private RectTransform scoreMotionLayer;
        private RectTransform scoreMotionTag;
        private CanvasGroup scoreMotionGroup;
        private Image scoreMotionAccent;
        private Text scoreMotionValue;
        private Text scoreMotionReason;
        private readonly List<GameObject> fillScoreTags = new();
        private string pendingPlacementScoreTileId;
        private int pendingPlacementScoreDelta;
        private ConstructionWorldView worldView;
        private int viewMinColumn;
        private int viewMinRow;
        private readonly List<GameObject> panControls = new();
        private Text latestRoomLabel;
        private Text viewportCoordinateText;
        private readonly List<CanvasGroup> caseHeaderGroups = new();
        private RectTransform leftCaseLeaf;
        private RectTransform rightCaseLeaf;
        private CanvasGroup leftCaseLeafInk;
        private CanvasGroup rightCaseLeafInk;
        private CanvasGroup caseBoardOverlay;
        private RectTransform leftCaseCover;
        private RectTransform rightCaseCover;
        private CanvasGroup caseInformation;
        private CanvasGroup caseRouteInformation;
        private GameObject caseOpeningBlocker;
        private Coroutine caseOpening;

        public int CurrentDay => run?.CurrentDay ?? 0;
        public ConstructionGameMode Mode => run?.Mode ?? ConstructionGameMode.Fill;
        public int BuiltCount => run?.BuiltCount ?? 0;
        public IReadOnlyList<ConstructionTileData> BuiltTiles =>
            run?.BuiltTiles ?? Array.Empty<ConstructionTileData>();
        public int ObservedAnimalScore => run?.AnimalScore ?? 0;
        // Retained for older presentation tests and consumers.
        public int ObservedAnimalMeals => ObservedAnimalScore;
        public int ObservedHumanWorkCycles => run?.CompletedWorkCyclesTotal ?? 0;
        public int ObservedBalanceScore => run?.BalanceScore ?? 0;
        public ConstructionStoryStage Stage => run?.StoryStage ??
            ConstructionStoryStage.WatchSquirrelEat;
        public InterfaceLanguage Language => language;
        public bool IsBoardCaseOpen { get; private set; } = true;
        public bool HasPendingPlacement => hasPendingPlacement;
        private bool IsEndless => run?.Mode == ConstructionGameMode.Endless;
        private bool IsBoardGame => run?.Mode == ConstructionGameMode.BoardGame;
        private bool Chinese => language == InterfaceLanguage.Chinese;
        private string L(string english, string chinese) => Chinese ? chinese : english;

        public bool ReturnToMainMenu()
        {
            ClearPendingPlacement();
            ClearResetConfirmation();
            if (run == null || !Application.CanStreamedLevelBeLoaded("Main"))
            {
                ShowReturnFailure("Main menu is unavailable.", "主菜单场景不可用，尚未返回。");
                return false;
            }
            if (store != null && runSavedAtLeastOnce && !runNeedsSave)
            {
                var status = store.TryLoadDetailed(out var persisted);
                if (status == ConstructionSaveLoadStatus.Missing)
                    runNeedsSave = true;
                else if (status != ConstructionSaveLoadStatus.Loaded ||
                         persisted.Mode != run.Mode ||
                         JsonUtility.ToJson(persisted.Export()) !=
                         JsonUtility.ToJson(run.Export()))
                {
                    ShowReturnFailure(
                        "Save changed outside the game. Still here to protect it.",
                        "存档已在游戏外发生变化。为保护存档，尚未返回菜单。");
                    return false;
                }
            }
            if (store != null && (runNeedsSave || !runSavedAtLeastOnce) &&
                !store.TrySave(run))
            {
                ShowReturnFailure("Save failed. Still here; try Menu again.",
                    "存档失败，尚未返回；请重试。");
                return false;
            }
            runSavedAtLeastOnce = true;
            runNeedsSave = false;
            StopScoreMotion();
            if (ConstructionSceneTransition.ReturnToMenu()) return true;
            ShowReturnFailure("Please wait for the case animation to finish.",
                "请等待盒子动画结束后重试。");
            return false;
        }

        private void ShowReturnFailure(string english, string chinese)
        {
            var message = L(english, chinese);
            if (feedbackText != null) feedbackText.text = message;
            if (feedbackPreviewText != null) feedbackPreviewText.text = message;
            if (paletteNotice != null && buildPaletteOpen)
                paletteNotice.text = message;
        }

        // Allows PlayMode tests to exercise the screen without touching the
        // user's persistent save file.
        public void UseRunForTesting(ConstructionRunModel testRun,
            ConstructionRunStore testStore = null,
            ConstructionLocalLeaderboardStore testLeaderboardStore = null)
        {
            StopScoreMotion();
            ClearPendingPlacement();
            run = testRun;
            ClearFixedInfoSelection();
            store = testStore;
            localLeaderboardStore = testLeaderboardStore;
            networkDisabledForTesting = true;
            routeSubject = RouteSubject.None;
            selectedHomeId = null;
            selectedWildlifeTileId = null;
            residentDispatchOpen = false;
            freeBuildChoiceMade = false;
            endlessChoiceMade = false;
            introChoiceMade = false;
            openingTourStep = 0;
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

        public void ToggleTutorialTips()
        {
            if (run == null || IsEndless ||
                run.IsFinished || run.CurrentDay > 12) return;
            ClearResetConfirmation();
            if (run.UsesFillHand)
            {
                if (!run.SkipFillTutorial()) return;
                SyncFillHandSelection();
            }
            else
                run.SetTutorialTipsSkipped(!run.TutorialTipsSkipped);
            HideTooltip();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
            Save();
        }

        public int OpeningTourStep => openingTourStep;

        public void AdvanceOpeningTour()
        {
            if (!ShouldShowOpeningTour()) return;
            if (openingTourStep < OpeningTourSteps - 1)
                openingTourStep++;
            else
            {
                run.SetOpeningTourCompleted(true);
                Save();
            }
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            RefreshOpeningTour();
            RefreshTutorialFocus();
        }

        public void DismissOpeningTour()
        {
            if (!ShouldShowOpeningTour()) return;
            run.SetOpeningTourCompleted(true);
            if (run.UsesFillHand && run.IsIntroPhase && !run.BuiltToday)
            {
                run.SkipFillTutorial();
                SyncFillHandSelection();
            }
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Save();
            Refresh();
        }

        private void SetLanguage(InterfaceLanguage choice)
        {
            if (language == choice) return;
            ClearResetConfirmation();
            language = choice;
            resultLeaderboardMessage = null;
            if (resultNicknamePlaceholder != null)
                resultNicknamePlaceholder.text = L("Local name (1–16)",
                    "本地昵称（1–16字）");
            if (!languageOverriddenForTesting)
            {
                PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
                PlayerPrefs.Save();
            }
            ApplyLanguageFonts();
            if (feedbackText != null)
                feedbackText.text = L("Language changed to English.", "已切换为中文。");
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
            RefreshPauseLabels();
            if (localRankingsOverlay != null && localRankingsOverlay.activeSelf)
                RefreshLocalRankings();
        }

        private void Start()
        {
            if (!languageOverriddenForTesting)
                language = (InterfaceLanguage)Mathf.Clamp(PlayerPrefs.GetInt(
                    LanguagePreferenceKey, (int)InterfaceLanguage.English), 0, 1);
            if (run == null)
            {
                var mode = ConstructionLaunchContext.Take();
                localLeaderboardStore = new ConstructionLocalLeaderboardStore(
                    ConstructionLocalLeaderboardStore.DefaultPath);
                openingFromPreparedRestart =
                    ConstructionLaunchContext.TakePreparedRestart(out run,
                        out store);
                if (openingFromPreparedRestart)
                    runSavedAtLeastOnce = store != null;
                else
                {
                    store = new ConstructionRunStore(
                        ConstructionRunStore.PathFor(mode));
                    var load = store.TryLoadDetailed(out run);
                    runSavedAtLeastOnce =
                        load == ConstructionSaveLoadStatus.Loaded;
                    if (load == ConstructionSaveLoadStatus.Missing)
                        run = new ConstructionRunModel(mode,
                            useFillHand: mode == ConstructionGameMode.Fill);
                    else if (load != ConstructionSaveLoadStatus.Loaded ||
                             run.Mode != mode)
                    {
                        Debug.LogError(
                            $"Construction save blocked: {load}; existing file preserved.",
                            this);
                        enabled = false;
                        if (Application.CanStreamedLevelBeLoaded("Main"))
                            SceneManager.LoadScene("Main");
                        return;
                    }
                }
            }
            // A save can land between a confirmed hand placement and its
            // automatic settlement. Finish that pending turn before building
            // the no-day UI so the restored run never appears unable to build.
            if (run.UsesFillHand && run.BuiltToday && !run.IsFinished)
            {
                if (run.TrySimulateFillDay(out _))
                    store?.TrySave(run);
                else
                    Debug.LogError("Could not settle the restored Fill hand placement.", this);
            }
            // Also cover direct scene launches and restored runs: the menu
            // transition is not the only way this controller can be created.
            FindFirstObjectByType<PhysicalBoardCameraController>()?
                .SetRecognitionSuspended(run.Mode != ConstructionGameMode.Fill ||
                    run.UsesFillHand);
            if (run.Mode == ConstructionGameMode.Endless && run.BuiltCount > 1)
            {
                var latest = run.BuiltTiles[run.BuiltCount - 1];
                viewMinColumn = latest.column - 3;
                viewMinRow = latest.row - 3;
            }
            regularFont = UrbanFontResolver.GetFont();
            // Loading an already finished save is not a new game completion.
            // An explicit tap on the result's online status can retry it.
            if (run.IsFinished) networkAttemptRunId = run.RunId;
            boldFont = Resources.Load<Font>("Fonts/Nunito-Bold") ?? regularFont;
            chineseFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular") ?? regularFont;
            EnsureCameraAndInput();
            SymbiosisAudioDirector.Ensure();
            ApplyPauseMasterVolume();
            BuildUi();
            Refresh();
            worldView?.RevealRestoredRooms();
            if (!IsBoardCaseOpen)
                caseOpening = StartCoroutine(OpenBoardCase());
            else if (IsEndless && !run.IsFinished &&
                !run.EndlessLossRulesConfirmed)
                OpenEndlessRules(false);
        }

        private void OnDestroy()
        {
            StopScoreMotion(false);
            if (idleCueSprite != null) Destroy(idleCueSprite);
            if (idleCueTexture != null) Destroy(idleCueTexture);
            ReleaseSpecialTaskBackdrop();
            if (scorePlaqueSprite != null) Destroy(scorePlaqueSprite);
            if (scorePlaqueTexture != null) Destroy(scorePlaqueTexture);
        }

        private void Update()
        {
            if(DeckInventoryOpen && Input.GetKeyDown(KeyCode.Escape)){CloseDeckInventory();return;}
            UpdateFillHandMotion();
            if (Input.GetKeyDown(KeyCode.Escape) && !HandleScoreIndexKey(KeyCode.Escape) &&
                !TryCancelRoomHighlights()) TogglePauseMenu();
            UpdateScoreTrace();
            DrawSelectedRoomHighlights();
            if (IsEndless && IsBoardCaseOpen && !PauseOpen && !QuickGuideOpen &&
                !run.IsFinished &&
                (EventSystem.current?.currentSelectedGameObject == null ||
                 EventSystem.current.currentSelectedGameObject
                     .GetComponent<InputField>() == null))
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Pan(-1, 0);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) Pan(1, 0);
                else if (Input.GetKeyDown(KeyCode.UpArrow)) Pan(0, -1);
                else if (Input.GetKeyDown(KeyCode.DownArrow)) Pan(0, 1);
            }
            if (Input.GetKeyDown(KeyCode.O) &&
                (EventSystem.current?.currentSelectedGameObject == null ||
                 EventSystem.current.currentSelectedGameObject
                     .GetComponent<InputField>() == null))
            {
                CloseDeckInventory();
                ToggleQuickGuide();
            }
            // A click can rebuild the board while the pointer is still over
            // a cell, so Unity does not always dispatch PointerExit. The hand
            // game uses nearby popovers; clear them when the pointer leaves
            // the board even after a layout rebuild.
            if (run?.UsesFillHand == true && openingTourBoardTarget != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(
                    openingTourBoardTarget, Input.mousePosition, null) &&
                (selectedInfoTileId != null || hoveredInfoTileId != null ||
                 hoveredEmptyColumn.HasValue || hoveredBadgeTileId != null ||
                 hoveredLocalRisk))
            {
                for (var row = 0; row < 7; row++)
                for (var column = 0; column < 7; column++)
                    cellMetricHovering[column, row] = false;
                ClearFixedInfoSelection();
            }
            // Keep the central room art readable while its compact metrics
            // fade in together when the pointer enters any built tile, so
            // the player can compare neighbouring rooms at a glance.
            var revealMetrics = hoveredBadgeTileId != null;
            for (var row = 0; row < 7 && !revealMetrics; row++)
            for (var column = 0; column < 7; column++)
            {
                if (!cellMetricHovering[column, row] ||
                    run?.At(viewMinColumn + column, viewMinRow + row) == null)
                    continue;
                revealMetrics = true;
                break;
            }
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var group = cellMetricGroups[column, row];
                if (group == null) continue;
                group.alpha = Mathf.MoveTowards(group.alpha,
                    revealMetrics ? 1f : 0.30f,
                    Time.unscaledDeltaTime * 5f);
            }
            if (IsBoardGame && boardGameCameraPanel != null)
                RefreshBoardGameCameraStatus();
        }

        private void RefreshBoardGameCameraStatus()
        {
            if (boardGameCameraPanel == null) return;
            boardGameCameraPanel.SetActive(IsBoardGame && !run.IsFinished);
            if (!IsBoardGame || run.IsFinished) return;
            var bridge = GetComponent<ConstructionCameraUdpBridge>();
            if (bridge?.HasReceivedPacket == true)
                boardGameCameraToolError = string.Empty;
            boardGameCameraStatusText.text = !string.IsNullOrEmpty(boardGameCameraToolError)
                ? boardGameCameraToolError
                : bridge == null || string.IsNullOrEmpty(bridge.CurrentStatus)
                    ? L("Start the camera, then show the board's four printed corner motifs.",
                        "启动摄像头，让棋盘原有的四角图案入镜。")
                    : Chinese ? bridge.CurrentStatusChinese : bridge.CurrentStatus;
            boardGameCameraStatusText.color = bridge?.StatusIsWarning == true ||
                !string.IsNullOrEmpty(boardGameCameraToolError)
                    ? new Color(0.43f, 0.16f, 0.14f) : Ink;
            boardGameCameraButtonLabel.text = L("Start camera", "启动摄像头");
            boardGameCameraButton.interactable = bridge != null;
        }

        private void StartBoardGameCameraTool()
        {
            if (!IsBoardGame) return;
            boardGameCameraToolError =
                ConstructionCameraUdpBridge.TryLaunchCameraTool(out var error)
                    ? string.Empty : error;
            RefreshBoardGameCameraStatus();
        }

        public bool TrySelect(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            ClearResetConfirmation();
            if (run == null) return false;
            if (run.IsFinished)
            {
                if (feedbackText != null)
                    feedbackText.text = L("This run has ended. Review the final result.",
                        "本局已结束，请查看最终结果。");
                return false;
            }
            if (run.IsCategoryAtLimit(category))
            {
                if (feedbackText != null)
                    feedbackText.text = L(
                        $"{CategoryLabel(category)} limit reached " +
                        $"({run.CategoryBuiltCount(category)}/{run.CategoryLimit(category)}). Choose another type.",
                        $"{CategoryLabel(category)}已达上限" +
                        $"（{run.CategoryBuiltCount(category)}/{run.CategoryLimit(category)}），请选择其他建筑。");
                Refresh();
                return false;
            }
            if (run.IsIntroPhase && run.IsIntroTypeUsed(category))
            {
                if (feedbackText != null)
                    feedbackText.text = L(
                        $"{CategoryLabel(category)} is complete. Build {CategoryLabel(run.IntroRequiredCategory.Value)} next; all types reopen after eight.",
                        $"{CategoryLabel(category)}已完成。下一步建{CategoryLabel(run.IntroRequiredCategory.Value)}；八类齐后可自由建造。");
                Refresh();
                return false;
            }
            if (run.IsIntroPhase && run.IntroRequiredCategory != category)
            {
                if (feedbackText != null)
                    feedbackText.text = L(
                        $"Build {CategoryLabel(run.IntroRequiredCategory.Value)} next.",
                        $"下一步先建{CategoryLabel(run.IntroRequiredCategory.Value)}。");
                Refresh();
                return false;
            }
            if (!CanSelectCategory(category))
            {
                if (feedbackText != null)
                    feedbackText.text = L("Finish observing the oak before building.",
                        "先完成橡树观察，再开始建造。");
                return false;
            }
            ClearPendingPlacement();
            selectedCategory = category;
            if (run.IsIntroPhase) introChoiceMade = true;
            if (IsEndless) endlessChoiceMade = true;
            if (category == ConstructionCategory.Green)
                selectedPlanting = planting;
            if (!IsEndless && run.TutorialChapter == ConstructionTutorialChapter.Green)
                tutorialGreenChoiceMade = category == ConstructionCategory.Green;
            if (!IsEndless && !run.IsIntroPhase)
                freeBuildChoiceMade = true;
            var available = AvailablePlotCount(category,
                category == ConstructionCategory.Green ? selectedPlanting : GreenPlanting.None);
            feedbackText.text = run.BuiltToday
                ? L($"{SelectedBuildLabel()} has {available} plot(s) tomorrow. " +
                    "Choose a building again after Next day.",
                    $"{SelectedBuildLabel()}明日可放 {available} 处。" +
                    "点击“下一天”后请重新选择建筑。")
                : L($"Selected: {SelectedBuildLabel()} · {available} valid plot(s). " +
                    "Choose a + plot to preview.",
                    $"已选择：{SelectedBuildLabel()} · 可放 {available} 处。" +
                    "点击＋地块预览。");
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
            return true;
        }

        public bool TrySelectResidenceKind(ResidenceKind kind)
        {
            // Physical board markers still call this API. All new homes are
            // generic, while old saved subtype data remains readable.
            if (kind != ResidenceKind.Standard) return false;
            return selectedCategory == ConstructionCategory.Residence &&
                   run != null && !run.IsFinished
                ? true : TrySelect(ConstructionCategory.Residence);
        }

        public bool RequestCameraPlacement(int column, int row)
        {
            if (!IsBoardGame || run == null) return false;
            cameraStageInProgress = true;
            try { return RequestPlacement(column, row); }
            finally { cameraStageInProgress = false; }
        }

        public bool CameraUsesRandomDraw => IsBoardGame &&
            run?.IsRandomBoardGame == true;
        public bool CameraCanBuildToday => IsBoardGame && run != null &&
            !run.IsFinished && run.CurrentDay > 1 && !run.BuiltToday;
        public bool CameraDrawnTypeHasLegalPlot(ConstructionCategory category,
            GreenPlanting planting) => CameraCanBuildToday &&
            CanSelectCategory(category) && AvailablePlotCount(category,
                planting) > 0;

        public bool CameraPendingMatches(int column, int row,
            ConstructionCategory category, GreenPlanting planting,
            ResidenceKind residenceKind) =>
            IsBoardGame && hasPendingPlacement && run != null &&
            pendingDay == run.CurrentDay && pendingColumn == column &&
            pendingRow == row && pendingCategory == category &&
            pendingPlanting == planting &&
            pendingResidenceKind == residenceKind;

        // A board click only stages a placement. The model, daily allowance,
        // score and save are changed solely by ConfirmPendingPlacement.
        public bool RequestPlacement(int column, int row)
        {
            if (run == null) return false;
            if (run.At(column, row) == null)
            {
                selectedInfoTileId = null;
                hoveredBadgeTileId = null;
                hoveredBadgeKind = null;
            }
            if (IsBoardGame && !cameraStageInProgress &&
                run.At(column, row) == null)
            {
                if (feedbackText != null)
                    feedbackText.text = L(
                        "Place a printed tile on the physical board first.",
                        "请先在实体棋盘放置对应的房间方块。");
                return false;
            }
            if (AwaitingFreeBuildChoice && !run.BuiltToday &&
                run.At(column, row) == null)
            {
                feedbackText.text = run.IsIntroPhase
                    ? L($"Select {CategoryLabel(run.IntroRequiredCategory.Value)} in Build before choosing a plot.",
                        $"先从建造栏选择{CategoryLabel(run.IntroRequiredCategory.Value)}，再选择位置。")
                    : L("Choose a room from Build before placing it.",
                        "先从建造栏自行选择房间，再选择位置。");
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                Refresh();
                return false;
            }
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (run.At(column, row) != null || run.BuiltToday || run.IsFinished ||
                !CanSelectCategory(selectedCategory) ||
                run.CheckPlacement(column, row, selectedCategory, planting,
                    ResidenceKind.Standard) !=
                    ConstructionPlacementFailure.None)
                return TryPlace(column, row);
            ClearResetConfirmation();
            pendingColumn = column;
            pendingRow = row;
            pendingDay = run.CurrentDay;
            pendingCategory = selectedCategory;
            pendingPlanting = planting;
            pendingResidenceKind = ResidenceKind.Standard;
            hasPendingPlacement = true;
            buildPaletteOpen = false;
            residentDispatchOpen = false;
            SymbiosisAudioDirector.Play(SymbiosisSound.BuildPreview);
            Refresh();
            return true;
        }

        public bool ConfirmPendingPlacement()
        {
            if (!hasPendingPlacement || run == null) return false;
            if (IsBoardGame &&
                GetComponent<ConstructionCameraUdpBridge>()?.CanConfirmPendingPlacement != true)
            {
                CancelCameraPendingPlacement();
                if (feedbackText != null)
                    feedbackText.text = L(
                        "Camera view changed. Hold the physical board still and try again.",
                        "摄像头画面已变化。请固定实体棋盘后重试。");
                return false;
            }
            var column = pendingColumn;
            var row = pendingRow;
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            if (run.CurrentDay != pendingDay || run.BuiltToday || run.IsFinished ||
                selectedCategory != pendingCategory || planting != pendingPlanting ||
                run.CheckPlacement(column, row, pendingCategory, pendingPlanting,
                    pendingResidenceKind) !=
                    ConstructionPlacementFailure.None)
            {
                ClearPendingPlacement();
                feedbackText.text = L("Placement changed. Choose a plot again.",
                    "建造条件已变化，请重新选择地块。");
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                Refresh();
                return false;
            }
            ClearPendingPlacement();
            cameraCommitInProgress = IsBoardGame;
            try { return TryPlace(column, row); }
            finally { cameraCommitInProgress = false; }
        }

        public void CancelPendingPlacement()
        {
            if (!hasPendingPlacement) return;
            ClearPendingPlacement();
            feedbackText.text = L("Cancelled. Today's build is still available.",
                "已取消，今天仍可建造一格。");
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
            Refresh();
        }

        public void CancelCameraPendingPlacement()
        {
            if (!IsBoardGame || !hasPendingPlacement) return;
            ClearPendingPlacement();
            Refresh();
        }

        private void ClearPendingPlacement()
        {
            hasPendingPlacement = false;
            if (placementConfirmation != null)
                placementConfirmation.SetActive(false);
        }

        private RouteSubject RouteSubjectAt(ConstructionTileData tile)
        {
            if (tile.category == ConstructionCategory.Residence &&
                run.HasResident(tile.id))
                return RouteSubject.Resident;
            if (tile.category != ConstructionCategory.Green)
                return RouteSubject.None;
            if (run.VisibleSquirrelHomeTiles(
                    run.UsesFillHand ? int.MaxValue :
                        ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .Any(home => home.id == tile.id))
                return RouteSubject.Squirrel;
            if (run.VisiblePigeonHomeTiles(run.UsesFillHand ? int.MaxValue :
                    ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .Any(home => home.id == tile.id))
                return RouteSubject.Pigeons;
            if (run.HedgehogHabitats.Any(home => home.id == tile.id))
                return RouteSubject.Hedgehog;
            if (tile.id == run.FoxHabitat?.id)
                return RouteSubject.Fox;
            return RouteSubject.None;
        }

        public bool TryPlace(int column, int row)
        {
            ClearPendingPlacement();
            ClearResetConfirmation();
            if (run == null) return false;
            var existing = run.At(column, row);
            if (existing != null)
            {
                selectedInfoTileId = existing.id;
                SelectRoomForReview(column,row);
                hoveredInfoTileId = null;
                hoveredEmptyColumn = null;
                hoveredEmptyRow = null;
                hoveredBadgeTileId = null;
                hoveredBadgeKind = null;
                if (!IsEndless && run.CurrentDay == 1 &&
                    existing.id == ConstructionBoardModel.StarterOakId)
                    openingOakInspected = true;
                var inspectedPigeons = !run.IsFinished && run.InspectPigeonMeadow(existing.id);
                var nextSubject = RouteSubjectAt(existing);
                var alreadySelected = nextSubject != RouteSubject.None &&
                    routeSubject == nextSubject &&
                    (nextSubject == RouteSubject.Resident
                        ? selectedHomeId == existing.id
                        : selectedWildlifeTileId == existing.id);
                routeSubject = alreadySelected ? RouteSubject.None : nextSubject;
                selectedHomeId = routeSubject == RouteSubject.Resident
                    ? existing.id : null;
                selectedWildlifeTileId = routeSubject is
                    RouteSubject.Squirrel or RouteSubject.Pigeons or
                    RouteSubject.Hedgehog or RouteSubject.Fox
                    ? existing.id : null;
                if (routeSubject != RouteSubject.Resident)
                    residentDispatchOpen = false;
                feedbackText.text = L(
                        $"{TileLabel(existing)} · {RoomProvisionBrief(existing.category, existing.planting)} · built day {existing.builtDay}",
                        $"{TileLabel(existing)} · {RoomProvisionBrief(existing.category, existing.planting)} · 第 {existing.builtDay} 天建造") +
                    "\n" + BuiltRoomProvision(existing);
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
                if (run.VisiblePigeonHomeTiles(run.UsesFillHand ? int.MaxValue :
                        ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                    .Any(home => home.id == existing.id))
                    feedbackText.text = !IsEndless && run.TutorialChapter ==
                        ConstructionTutorialChapter.Residence
                        ? L($"Meadow: {run.PigeonPopulation} pigeons, grass seeds " +
                            $"{run.FoodStock(existing.id)}/{run.FoodCapacity(existing.id)}.\n" +
                            "First meal tomorrow. Next: a residence.",
                            $"草地：{run.PigeonPopulation} 只鸽子，草籽 " +
                            $"{run.FoodStock(existing.id)}/{run.FoodCapacity(existing.id)}。\n" +
                            "明日首次觅食；接着建住宅。")
                        : feedbackText.text + L(" · a pigeon flock lives here",
                            " · 鸽群在此栖息");
                if (run.HedgehogHabitats.Any(home => home.id == existing.id))
                    feedbackText.text += L(" · hedgehog habitat visitor",
                        " · 刺猬已到来（栖息访客）");
                if (existing.id == run.FoxHabitat?.id)
                    feedbackText.text += L(" · fox den; may hunt reachable wildlife tomorrow",
                        " · 狐狸栖息地；明日可能捕食可达动物");
                if (existing.category == ConstructionCategory.Residence)
                {
                    if (!run.HasResident(existing.id))
                        feedbackText.text += L(" · vacant: resident left after missed work-and-meal cycles",
                            " · 空置：居民连续未完成上班和用餐后离开");
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
                    feedbackText.text += run.UsesUncappedWaste
                        ? L($" · waste {run.WasteBacklog(existing.id)}",
                            $" · 未清运垃圾 {run.WasteBacklog(existing.id)} 份")
                        : L(
                            $" · waste {run.WasteBacklog(existing.id)}/" +
                            $"{ConstructionWasteModel.DisruptionThreshold} (cleanup before next route)",
                            $" · 垃圾 {run.WasteBacklog(existing.id)}/" +
                            $"{ConstructionWasteModel.DisruptionThreshold}（次日外出前清运）");
                var jobCapacity = ConstructionHumanModel.DailyJobCapacity(
                    existing.category);
                var mealCapacity = ConstructionHumanModel.DailyMealCapacity(
                    existing, run.BuiltTiles, run.UsesNeighborUpgrades);
                if (jobCapacity > 0 || mealCapacity > 0)
                {
                    var preview = run.PreviewHumanDay();
                    var jobs = preview.residents.Count(person =>
                        person.workTileId == existing.id);
                    var meals = preview.residents.Count(person =>
                        person.mealTileId == existing.id);
                    feedbackText.text += L(
                        $" · tomorrow jobs {jobs}/{jobCapacity}" +
                        (mealCapacity > 0 ? $", meals {meals}/{mealCapacity}" : ""),
                        $" · 明日岗位 {jobs}/{jobCapacity}" +
                        (mealCapacity > 0 ? $"、餐位 {meals}/{mealCapacity}" : ""));
                }
                if (inspectedPigeons) Save();
                SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
                Refresh();
                return false;
            }
            ClearRoomHighlightSelection();
            if (IsBoardGame && !cameraCommitInProgress)
            {
                if (feedbackText != null)
                    feedbackText.text = L(
                        "Place a printed tile on the physical board and wait for camera recognition.",
                        "请在实体棋盘放置房间方块，等待摄像头识别。");
                Refresh();
                return false;
            }
            if (run.BuiltToday)
            {
                feedbackText.text = L("Built 1/1 today. Click Next day, then choose a building again.",
                    "今日已建 1/1。点击“下一天”后请重新选择建筑。");
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                Refresh();
                return false;
            }
            if (AwaitingFreeBuildChoice)
            {
                feedbackText.text = run.IsIntroPhase
                    ? L($"Select {CategoryLabel(run.IntroRequiredCategory.Value)} in Build, then choose a plot.",
                        $"先在建造栏选择{CategoryLabel(run.IntroRequiredCategory.Value)}，再选位置。")
                    : L("Choose a room from Build before placing it.",
                        "先从建造栏自行选择房间，再选择位置。");
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                Refresh();
                return false;
            }
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var wasIntroBuild = run.IsIntroPhase;
            if (!run.TryBuild(column, row, selectedCategory, planting,
                    out var built, out var failure, out var placement,
                    ResidenceKind.Standard))
            {
                feedbackText.text = failure == ConstructionBuildFailure.LockedByStory
                    ? placement == ConstructionPlacementFailure.IntroTypeNotNext
                        ? PlacementMessage(placement)
                        : L("Finish observing the oak before building.",
                            "先完成橡树观察，再开始建造。")
                    : failure == ConstructionBuildFailure.IntroTypeAlreadyBuilt
                        ? PlacementMessage(ConstructionPlacementFailure.IntroTypeAlreadyBuilt)
                    : failure == ConstructionBuildFailure.TypeLimitReached
                        ? PlacementMessage(ConstructionPlacementFailure.TypeLimitReached)
                    : failure == ConstructionBuildFailure.DailyLimitReached
                        ? L("One new cell per day. Check tomorrow's needs, then advance.",
                            "每天只能建造一格。查看次日需求后推进日期。")
                    : failure == ConstructionBuildFailure.RunFinished
                        ? L("This run has ended. Review the final result.",
                            "本局已结束，请查看最终结果。")
                    : PlacementMessage(placement);
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                Refresh();
                return false;
            }
            selectedInfoTileId = run.UsesFillHand ? null : built.id;
            if (run.UsesFillHand)
            {
                hoveredInfoTileId = null;
                hoveredEmptyColumn = null;
                hoveredEmptyRow = null;
                HideRoomBadgeTooltip();
            }
            feedbackText.text = IsEndless
                ? L($"Built {TileLabel(built)} at {column + 1},{row + 1}.",
                    $"已在 {column + 1},{row + 1} 建造{TileLabel(built)}。")
                : wasIntroBuild
                    ? L($"Explored {TileLabel(built)} ({run.IntroTypesBuilt}/8). " +
                        (run.IntroRequiredCategory.HasValue
                            ? $"Next day: build {CategoryLabel(run.IntroRequiredCategory.Value)}."
                            : "All eight types complete; free building is open."),
                        $"已建{TileLabel(built)}（{run.IntroTypesBuilt}/8）。" +
                        (run.IntroRequiredCategory.HasValue
                            ? $"下一天建{CategoryLabel(run.IntroRequiredCategory.Value)}。"
                            : "八类已齐，可以自由建造。"))
                : run.UsesGreenPatches &&
                  built.category == ConstructionCategory.Green &&
                  built.planting == GreenPlanting.Meadow
                ? L("The meadow gives two visiting pigeons a place to rest. They do not count or score yet; two mature meadows in one patch can house two resident pigeons.",
                    "草地有了新访客：两只鸽子会来歇脚。它们暂不计数量或得分；同一板块有2块成熟草地后，才能住下2只常驻鸽子。")
                : built.category == ConstructionCategory.Green &&
                built.planting == GreenPlanting.Meadow && !run.HasConnectedMeadow
                ? IsBoardGame && run.IsRandomBoardGame
                    ? L("The meadow is ready. Pigeons need a green route to the central oak; " +
                        "future draws may connect it.",
                        "草地铺好了，鸽群还在等一条通向中央橡树的绿地路；后续抽牌也许能接通。")
                    : L("The meadow is ready. Pigeons need a green route to the central oak; " +
                        "you can connect it with later green rooms.",
                        "草地铺好了，鸽群还在等一条通向中央橡树的绿地路；之后可以继续铺绿地接通。")
                : built.id == run.FirstResidentHomeTileId &&
                  IsBoardGame && run.IsRandomBoardGame
                ? L("Someone has moved in. They need one meal and a reachable job " +
                    "each day; later draws can supply them.",
                    "新邻居搬来了！每天还需要一份餐食和一个能到达的岗位；后续抽牌可以慢慢补齐。")
                : built.id == run.FirstResidentHomeTileId
                ? L("Someone has moved in. They need 1 meal/day.\n" +
                    "Build a restaurant beside home (2 seats/day).",
                    "新邻居搬来了！每天需要 1 份餐食。\n" +
                    "在住宅旁开间餐厅吧（每日提供 2 个餐位）。")
                : built.category == ConstructionCategory.Restaurant
                    ? run.FirstResidentHasRestaurantAccess
                        ? L("The restaurant is within reach. Advance a day to see whether the resident gets a meal.",
                            "餐厅和住宅接通了。推进一天，看看居民有没有吃上饭。")
                        : L("Restaurant built, but the first resident cannot reach it.",
                            "餐厅开张了，可第一位居民还走不到；检查住宅和街道的连接。")
                    : L($"Built {TileLabel(built)} at {column + 1},{row + 1}.",
                        $"已在 {column + 1},{row + 1} 建造{TileLabel(built)}。");
            if (run.UsesPlacementScore)
            {
                var formedNeighborhood = run.ScoringVersion < 12 &&
                    run.UsesCompleteNeighborhoods &&
                    ConstructionPlacementScore.NewlyCompletedNeighborhoods(
                        built, run.BuiltTiles) > 0;
                var upgradedGreen = run.UsesGreenUpgrades &&
                    built.category == ConstructionCategory.Green &&
                    run.GreenUpgradeLevel(built.id) > 1;
                var placementReasonEnglish = formedNeighborhood
                    ? "Complete neighbourhood" : upgradedGreen
                        ? "Green upgrade" : "Nearby placement";
                var placementReasonChinese = formedNeighborhood
                    ? "街坊成形" : upgradedGreen
                        ? "绿地升级" : run.ScoringVersion >= 12
                            ? "共边联动" : "八邻格联动";
                feedbackText.text += L(
                    $"\n{placementReasonEnglish} " +
                    $"{run.LastPlacementPoints:+#;-#;0} · " +
                    $"build total {run.PlacementScore}.",
                    $"\n{placementReasonChinese} " +
                    $"{run.LastPlacementPoints:+#;-#;0} · " +
                    $"建造累计 {run.PlacementScore}。");
            }
            buildPaletteOpen = false;
            Save();
            SymbiosisAudioDirector.Play(SymbiosisSound.BuildConfirm);
            if (run.Mode == ConstructionGameMode.Endless)
            {
                // Reveal the next adjacent frontier without a fixed map edge.
                if (column - viewMinColumn <= 1 || column - viewMinColumn >= 5)
                    viewMinColumn = column - 3;
                if (row - viewMinRow <= 1 || row - viewMinRow >= 5)
                    viewMinRow = row - 3;
            }
            if (built.category == ConstructionCategory.Residence)
                worldView?.PlayResidentArrival(built.id);
            if (run.UsesPlacementScore)
            {
                pendingPlacementScoreTileId = built.id;
                pendingPlacementScoreDelta = run.LastPlacementPoints;
            }
            if (run.UsesFillHand)
            {
                pendingPlacementScoreTileId = built.id;
                pendingPlacementScoreDelta = run.LastPlacementPoints;
                // In the digital hand game every confirmed tile is one turn.
                // Settle it now, then reveal only the observed score change.
                if (!AdvanceDay())
                {
                    feedbackText.text = L(
                        "The room was built, but settlement did not finish. Try again.",
                        "房间已建成，但本次结算未完成，请重试。");
                    Refresh();
                }
                return true;
            }
            Refresh();
            // The starter oak occupies day one. The forty-eighth player
            // placement fills the last vacancy on day 49; settle that day
            // immediately so "board full" and "game over" are the same event.
            if (!IsEndless && run.IsFull &&
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
            if (IsEndless && !run.EndlessLossRulesConfirmed)
            {
                OpenEndlessRules(false);
                return false;
            }
            if (!run.CanAdvanceConstructionDay)
            {
                feedbackText.text = IsEndless
                    ? L("Build one plot before advancing the day.",
                        "本轮先建造一格，再进入下一天。")
                    : L("Build one room before ending this day.",
                        "今天还没建造房间。先选一格安放，再进入下一天。");
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                return false;
            }
            StopScoreMotion();
            var animalScoreBefore = run.AnimalScore;
            var residentScoreBefore = run.HumanScore;
            var sharedScoreBefore = run.BalanceScore;
            var taskScoreBefore = run.SpecialTaskScore;
            var handSizeBefore = run.UsesFillHand ? run.FillHandSize : 0;
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
            var openingMeal = !IsEndless &&
                run.StoryStage == ConstructionStoryStage.WatchSquirrelEat;
            var pigeonsBefore = run.PigeonTileId;
            var squirrelsBefore = run.SquirrelPopulation;
            var pigeonCountBefore = run.PigeonPopulation;
            var residentAteBefore = run.FirstResidentAte;
            var residentMealRoute = run.StoryStage ==
                ConstructionStoryStage.WatchResidentEat
                ? run.FirstResidentRestaurantRoute
                : Array.Empty<ConstructionTileData>();
            ConstructionEcologyDayResult ecology;
            if (!(run.UsesFillHand
                    ? run.TrySimulateFillDay(out ecology)
                    : run.TrySimulateConstructionDay(out ecology))) return false;
            HoldRoomIssuesThroughActivity();
            routeSubject = RouteSubject.None;
            selectedHomeId = null;
            selectedWildlifeTileId = null;
            residentDispatchOpen = false;
            introChoiceMade = false;
            freeBuildChoiceMade = false;
            endlessChoiceMade = false;
            if (!run.UsesFillHand)
                SymbiosisAudioDirector.Play(SymbiosisSound.NextDay);
            var human = run.LastHumanDay;
            feedbackText.text = openingMeal && ecology.squirrelAte &&
                                run.HasFedAnimalScore && run.UsesFillHand
                ? run.ScoringVersion >= 12
                    ? L("The squirrel ate an acorn: 3 animal points, plus 2 for the oak that fed it. Next, place a home beside the oak.",
                        "松鼠吃到了橡果：进食 +3，橡树供食 +2，共得 5 分动物分。接下来在橡树共边格建住宅。")
                    : L("The squirrel ate an acorn: 4 animal points, plus 3 for the oak that fed it. Next, place a home beside the oak.",
                        "松鼠吃到了橡果：进食 +4，橡树供食 +3，共得 7 分动物分。接下来在橡树共边格建住宅。")
                : openingMeal && ecology.squirrelAte &&
                                IsBoardGame && run.IsRandomBoardGame
                ? run.ScoringVersion >= 7
                    ? L("Day 1 settled: the nearby oak food area earned 1 animal point; the squirrel ate to survive. Now draw one face-down room tile.",
                        "第 1 天，松鼠吃到了橡果。附近橡树食源带来 1 分动物分；现在随机抽一块背面朝上的方块。")
                    : L("Day 1 settled: the squirrel ate one oak nut for 3 animal points. " +
                        "Now draw one face-down room tile.",
                        "第 1 天，松鼠吃到了橡果，获得 3 分动物分。现在随机抽一块背面朝上的房间方块。")
                : openingMeal && ecology.squirrelAte
                ? run.ScoringVersion >= 7
                    ? L("Day 1 settled: the nearby oak food area earned 1 animal point; the squirrel ate to survive. Next, build Green.",
                        "第 1 天，松鼠吃到了橡果。附近橡树食源带来 1 分动物分；下一步给动物添块绿地。")
                    : L("Day 1 settled: the squirrel ate one oak nut for 3 animal points. Next, build Green.",
                        "第 1 天，松鼠吃到了橡果，获得 3 分动物分。下一步给动物添块绿地。")
                : ecology.predatedSpecies != ConstructionPreySpecies.None
                    ? L($"Fox hunted one {PreyName(ecology.predatedSpecies, false)} " +
                        $"at {TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.predationTileId)}. " +
                        "The prey population changed; inspect tomorrow's forecast.",
                        $"狐狸在{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.predationTileId)}" +
                        $"捕食了 1 只{PreyName(ecology.predatedSpecies, true)}。" +
                        "动物数量已变化，请查看明日预测。")
                : pigeonsBefore == null && run.PigeonTileId != null
                    ? L("A pigeon flock has settled in. It will forage from tomorrow.",
                        "鸽群安顿下来了！明天开始，它们会外出觅食。")
                : run.SquirrelPopulation > squirrelsBefore
                    ? L("A mature oak drew in another squirrel. Check that tomorrow has enough food.",
                        "成熟橡树又迎来一只松鼠。明天的食物够不够，也替它看看。")
                : run.PigeonPopulation > pigeonCountBefore
                    ? L("Connected mature meadows made room for more pigeons. Check tomorrow's food.",
                        "相连的成熟草地让更多鸽子住下。别忘了看看明天的食物够不够。")
                : !IsEndless && !residentAteBefore && run.FirstResidentAte
                    ? L("The resident found a meal and work at the restaurant. Next, add a workshop for more jobs.",
                        "居民在餐厅吃上饭，也找到了工作。下一步建工作坊，再添两个岗位。")
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
                    $"今天动物吃到 {ecology.AnimalMeals} 份食物；居民有 {human?.WorkedCount ?? 0} 人上班、" +
                    $"{human?.MealsEaten ?? 0} 人用餐、{human?.CompletedWorkCycles ?? 0} 人完成往返循环。");
            if (run.EndReason == ConstructionEndReason.WildlifeGone)
                feedbackText.text = L("Squirrels or an arrived pigeon flock stayed absent for five days. Endless run ended.",
                    "松鼠或已到来的鸽群连续 5 天消失，无尽模式结束。");
            else if (run.EndReason == ConstructionEndReason.ResidentsGone)
                feedbackText.text = L("All residents left after missing full work-and-meal cycles. Endless run ended.",
                    "居民连续未完成上班和用餐，全部离开；无尽模式结束。");
            else if (run.EndReason == ConstructionEndReason.AnimalDeathsLimit)
                feedbackText.text = L(
                    $"{run.RecentAnimalDeaths} animal deaths in the last " +
                    $"{ConstructionRunModel.AnimalDeathWindowDays} days reached the " +
                    $"limit of {run.AnimalDeathLimit}. Endless run ended.",
                    $"最近 {ConstructionRunModel.AnimalDeathWindowDays} 天死亡 " +
                    $"{run.RecentAnimalDeaths} 只动物，达到上限 {run.AnimalDeathLimit} 只；无尽模式结束。");
            else if (run.EndReason == ConstructionEndReason.ResidentsUnemployed)
                feedbackText.text = L(
                    $"At least {run.UnemployedResidentLimit} residents did not work " +
                    $"for {run.UnemployedDaysInRow} consecutive days. Endless run ended.",
                    $"至少 {run.UnemployedResidentLimit} 名居民连续 " +
                    $"{run.UnemployedDaysInRow} 天未上班；无尽模式结束。");
            if (ecology.trafficSpecies != ConstructionTrafficSpecies.None &&
                run.EndReason == ConstructionEndReason.None)
                feedbackText.text = L(
                    $"Traffic collision at {TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.trafficStreetTileId)}: " +
                    $"one {TrafficSpeciesName(ecology.trafficSpecies, false)} died" +
                    (run.LastScoreDay?.trafficPenaltyPoints > 0
                        ? $"; animal score −{run.LastScoreDay.trafficPenaltyPoints}.\n"
                        : ".\n"),
                    $"{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), ecology.trafficStreetTileId)} 街道发生车祸：" +
                    $"1 只{TrafficSpeciesName(ecology.trafficSpecies, true)}死亡" +
                    (run.LastScoreDay?.trafficPenaltyPoints > 0
                        ? $"；动物得分 −{run.LastScoreDay.trafficPenaltyPoints}。\n"
                        : "。\n")) +
                    feedbackText.text;
            if (run.UsesFillHand)
            {
                var animalDelta = run.AnimalScore - animalScoreBefore;
                var residentDelta = run.HumanScore - residentScoreBefore;
                var sharedDelta = run.BalanceScore - sharedScoreBefore;
                var taskDelta = run.SpecialTaskScore - taskScoreBefore;
                var settlementLine = L(
                    $"Placement settled · animal {animalDelta:+#;-#;0} · " +
                    $"resident {residentDelta:+#;-#;0} · " +
                    $"shared {sharedDelta:+#;-#;0} · " +
                    $"tasks {taskDelta:+#;-#;0}.",
                    $"本次放置已结算 · 动物 {animalDelta:+#;-#;0} · " +
                    $"居民 {residentDelta:+#;-#;0} · " +
                    $"共栖 {sharedDelta:+#;-#;0} · " +
                    $"任务 {taskDelta:+#;-#;0}。");
                feedbackText.text = ecology.trafficSpecies !=
                    ConstructionTrafficSpecies.None
                    ? L($"Traffic collision: one {TrafficSpeciesName(ecology.trafficSpecies, false)} died.\n",
                        $"发生车祸：1 只{TrafficSpeciesName(ecology.trafficSpecies, true)}死亡。\n") +
                      settlementLine
                    : ecology.predatedSpecies != ConstructionPreySpecies.None
                        ? L($"Fox hunted one {PreyName(ecology.predatedSpecies, false)}.\n",
                            $"狐狸捕食了 1 只{PreyName(ecology.predatedSpecies, true)}。\n") +
                          settlementLine
                        : settlementLine;
                if (run.FillHandSize > handSizeBefore)
                    feedbackText.text += L(
                        $"\nPlazas increased your hand limit to {run.FillHandSize}.",
                        $"\n广场使手牌上限增加到 {run.FillHandSize} 张。");
                if (run.CategoryBuiltCount(ConstructionCategory.Residence) == 1 &&
                    run.BuiltTiles.LastOrDefault()?.category ==
                        ConstructionCategory.Residence)
                    feedbackText.text += L(
                        "\nClick the new home, then use Set work & meals on the left.",
                        "\n点击新住宅，再点左侧“安排工作与用餐”。");
            }
            Save();
            worldView?.Sync(run, true);
            if (ecology.trafficSpecies != ConstructionTrafficSpecies.None)
                worldView?.PlayCollision(ecology.trafficStreetTileId,
                    ecology.trafficSpecies, ecology.trafficWildlifeTileId);
            var newPigeonFlock = pigeonsBefore == null && run.PigeonTileId != null;
            if (newPigeonFlock) worldView?.PlayPigeonArrival();
            Refresh();
            worldView?.PlayForaging(
                ecology.squirrelFoodTileIds.Select((target, index) =>
                    run.GreenFoodRouteTo(
                        ecology.squirrelMealHomeTileIds != null &&
                        index < ecology.squirrelMealHomeTileIds.Count
                            ? ecology.squirrelMealHomeTileIds[index]
                            : ConstructionBoardModel.StarterOakId,
                        target)).ToArray(),
                ecology.pigeonMealTileIds.Select((target, index) =>
                    run.GreenFoodRouteTo(run.UsesGreenPatches &&
                        index < ecology.pigeonMealHomeTileIds.Count
                            ? ecology.pigeonMealHomeTileIds[index] :
                            run.PigeonTileId, target)).ToArray(),
                run,
                ecology.hedgehogFed && run.HedgehogHabitat != null
                    ? run.GreenFoodRouteTo(run.HedgehogHabitat.id,
                        ecology.hedgehogFoodTileId)
                    : null,
                run.UsesGreenPatches
                    ? ecology.hedgehogMealTileIds.Select((target, index) =>
                        run.GreenFoodRouteTo(
                            ecology.hedgehogMealHomeTileIds[index], target))
                        .ToArray()
                    : null);
            if (ecology.predatedSpecies != ConstructionPreySpecies.None)
                worldView?.PlayFoxHunt(ecology.foxHuntRouteTileIds, run.BuiltTiles,
                    ecology.predatedSpecies);
            if (human?.residents != null)
                worldView?.PlayResidentCommutes(human.residents, run.BuiltTiles);
            worldView?.PlayHabitatVisits(run);
            StartIdleCues(ecology, human);
            if (newPigeonFlock)
                foreach (var tile in run.BuiltTiles)
                    if (tile.id == run.PigeonTileId)
                    {
                        var column = tile.column - viewMinColumn;
                        var row = tile.row - viewMinRow;
                        if (column >= 0 && column < 7 && row >= 0 && row < 7)
                            pigeonIcons[column, row].rectTransform.localScale =
                                Vector3.one * 0.2f;
                        break;
                    }
            growthAnimation = StartCoroutine(AnimateDailyGrowth(sizesBefore,
                newPigeonFlock));
            if (!residentAteBefore && run.FirstResidentAte &&
                (!run.UsesFillHand || worldView?.IsReady != true))
                residentMealAnimation = StartCoroutine(
                    AnimateResidentMeal(residentMealRoute));
            if (run.UsesPlacementScore)
                StartFillHandScoreMotion(ecology, animalScoreBefore,
                    residentScoreBefore, sharedScoreBefore);
            else
                StartScoreMotion(ecology, animalScoreBefore,
                    residentScoreBefore, sharedScoreBefore);
            ReleaseRoomIssuesAfterActivity();
            return true;
        }

        private IEnumerator AnimateResidentMeal(
            IReadOnlyList<ConstructionTileData> route)
        {
            if (route == null || route.Count < 2 || route.Any(tile =>
                    tile == null || (long)tile.column - viewMinColumn < 0 ||
                    (long)tile.column - viewMinColumn >= 7 ||
                    (long)tile.row - viewMinRow < 0 ||
                    (long)tile.row - viewMinRow >= 7))
            {
                residentMealAnimation = null;
                yield break;
            }
            var home = route[0];
            var homeColumn = home.column - viewMinColumn;
            var homeRow = home.row - viewMinRow;
            var stationary = residentMarkers[homeColumn, homeRow];
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
            var viewportColumn = viewMinColumn;
            var viewportRow = viewMinRow;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                targets[column, row] = cellIconRects[column, row].sizeDelta;
                var tile = run.At(viewportColumn + column, viewportRow + row);
                if (tile?.category == ConstructionCategory.Green)
                    cellIconRects[column, row].sizeDelta = sizesBefore[column, row];
                if (newPigeonFlock && tile != null &&
                    tile.id == run.PigeonTileId)
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
                if (viewportColumn != viewMinColumn || viewportRow != viewMinRow)
                {
                    growthAnimation = null;
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                var eased = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                for (var row = 0; row < 7; row++)
                for (var column = 0; column < 7; column++)
                    if (run.At(viewportColumn + column, viewportRow + row)?.category ==
                        ConstructionCategory.Green)
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
            // Fit the whole case and its header on 16:10 and narrower screens.
            // A blended scale can put the left title outside the viewport.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

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
            titleText.gameObject.SetActive(false);
            var languageControl = NewRect("Construction language", canvas.transform,
                new Vector2(632, 409), new Vector2(138, 42));
            var languageIcon = Panel("Language pictogram", languageControl,
                new Vector2(-55, 0), new Vector2(23, 23), Color.white);
            languageIcon.sprite = ConstructionIconCatalog.GetControl(
                ConstructionControlIcon.Language);
            languageIcon.preserveAspect = true;
            languageIcon.raycastTarget = false;
            languageIcon.gameObject.SetActive(false);
            englishLanguageButton = ButtonAt("English", languageControl,
                new Vector2(-18, 0), new Vector2(50, 40), "EN", 15,
                Ink, Paper, () => SetLanguage(InterfaceLanguage.English));
            chineseLanguageButton = ButtonAt("Chinese", languageControl,
                new Vector2(36, 0), new Vector2(50, 40), "中文", 15,
                Ink, Paper, () => SetLanguage(InterfaceLanguage.Chinese));
            englishLanguageLabel = englishLanguageButton.GetComponentInChildren<Text>();
            chineseLanguageLabel = chineseLanguageButton.GetComponentInChildren<Text>();
            // A slim label sits on the board-case rim. Only the day number is
            // needed here; the separate Research HUD retains its time-of-day dial.
            var handMode = run.UsesFillHand;
            var plaqueWidth = handMode ? 190f : 136f;
            var dayPlaque = Panel("Construction day plaque", canvas.transform,
                new Vector2(0, 393), new Vector2(plaqueWidth, 42), Gold);
            dayPlaque.raycastTarget = false;
            var dayPlaqueFace = Panel("Day plaque face", dayPlaque.transform,
                Vector2.zero, new Vector2(plaqueWidth - 4f, 38), Ink);
            dayPlaqueFace.raycastTarget = false;
            var dayPlaqueRule = Panel("Day plaque highlight", dayPlaque.transform,
                new Vector2(0, 17), new Vector2(plaqueWidth - 10f, 1),
                new Color(0.95f, 0.82f, 0.60f, 0.55f));
            dayPlaqueRule.raycastTarget = false;
            dayText = TextAt("Day", dayPlaque.transform, Vector2.zero,
                new Vector2(plaqueWidth - 10f, 34), "", handMode ? 18 : 21,
                true, Paper,
                TextAnchor.MiddleCenter);
            dayText.resizeTextForBestFit = true;
            dayText.resizeTextMinSize = 15;
            dayText.resizeTextMaxSize = handMode ? 18 : 21;
            dayText.raycastTarget = false;
            var menuButton = ButtonAt("Return to main menu", canvas.transform,
                new Vector2(320, 409), new Vector2(130, 42), "←  Menu", 15,
                Ink, Paper, () => { ReturnToMainMenu(); });
            AddButtonIcon(menuButton, ConstructionControlIcon.Back);
            menuButton.gameObject.SetActive(false);
            menuLabel = menuButton.GetComponentInChildren<Text>();
            var reset = ButtonAt("Start a new construction run", canvas.transform,
                new Vector2(470, 409), new Vector2(150, 42), "New run", 15,
                Ink, Paper, () => OnResetPressed());
            AddButtonIcon(reset, ConstructionControlIcon.Restart);
            reset.gameObject.SetActive(false);
            resetLabel = reset.GetComponentInChildren<Text>();
            endlessRulesButton = ButtonAt("Endless ending rules", canvas.transform,
                new Vector2(-435, 409), new Vector2(130, 42), "Rules", 15,
                Ink, Paper, () => OpenEndlessRules(false));
            AddButtonIcon(endlessRulesButton, ConstructionControlIcon.Settings);
            endlessRulesButtonLabel = endlessRulesButton.GetComponentInChildren<Text>();
            foreach (var header in new GameObject[] { titleText.gameObject,
                         languageControl.gameObject, dayPlaque.gameObject,
                         menuButton.gameObject, reset.gameObject,
                         endlessRulesButton.gameObject })
                caseHeaderGroups.Add(header.AddComponent<CanvasGroup>());

            var boardScale = handMode ? 0.84f : 1f;
            var boardPanel = Panel("7 by 7 construction board", canvas.transform,
                new Vector2(0, handMode ? 40f : -3f), new Vector2(760, 760),
                direct3D ? Color.clear : Ink);
            boardPanel.rectTransform.localScale = Vector3.one * boardScale;
            openingTourBoardTarget = boardPanel.rectTransform;
            boardPanel.raycastTarget = false;
            caseBoardOverlay = boardPanel.gameObject.AddComponent<CanvasGroup>();
            var goldEdge = Panel("Board gold edge", boardPanel.transform,
                Vector2.zero, new Vector2(750, 750),
                direct3D ? Color.clear : Gold);
            goldEdge.raycastTarget = false;
            var inset = Panel("Board inset", boardPanel.transform,
                Vector2.zero, new Vector2(740, 740),
                direct3D ? Color.clear : UrbanPalette.ConstructionGrid);
            inset.raycastTarget = false;
            var boardArea = NewRect("3D board alignment", boardPanel.transform,
                Vector2.zero, new Vector2(740, 740));
            worldView = gameObject.AddComponent<ConstructionWorldView>();
            worldView.Initialize(Camera.main, boardArea,
                BoardCellSpacing * boardScale, run.Mode);
            BuildCells(boardPanel.transform);
            panControls.Add(ButtonAt("Pan board north", canvas.transform,
                new Vector2(-310, 385), new Vector2(42, 35), "↑", 22,
                Color.white, Paper, () => Pan(0, -1)).gameObject);
            panControls.Add(ButtonAt("Pan board south", canvas.transform,
                new Vector2(-310, -391), new Vector2(42, 35), "↓", 22,
                Color.white, Paper, () => Pan(0, 1)).gameObject);
            panControls.Add(ButtonAt("Pan board west", canvas.transform,
                new Vector2(-390, -3), new Vector2(42, 35), "←", 22,
                Color.white, Paper, () => Pan(-1, 0)).gameObject);
            panControls.Add(ButtonAt("Pan board east", canvas.transform,
                new Vector2(390, -3), new Vector2(42, 35), "→", 22,
                Color.white, Paper, () => Pan(1, 0)).gameObject);
            var latestRoomButton = ButtonAt("Center on latest room", canvas.transform,
                new Vector2(-231, -391), new Vector2(104, 35), "Latest room", 12,
                Ink, Paper, () => FocusLatestBuiltRoom());
            latestRoomLabel = latestRoomButton.GetComponentInChildren<Text>();
            panControls.Add(latestRoomButton.gameObject);
            var coordinateBacking = Panel("Viewport coordinates", canvas.transform,
                new Vector2(58, -391), new Vector2(282, 32), Ink);
            viewportCoordinateText = TextAt("Viewport position", coordinateBacking.transform,
                Vector2.zero, new Vector2(274, 28), "", 12, true,
                Paper, TextAnchor.MiddleCenter);
            viewportCoordinateText.raycastTarget = false;
            panControls.Add(coordinateBacking.gameObject);
            var riskLocator = ButtonAt("Locate fox risk", canvas.transform,
                new Vector2(291, -391), new Vector2(154, 35),
                L("Fox alert →", "狐狸风险 →"), 12, BlockedRed, Paper,
                () => FocusPredationRisk());
            foxRiskLocator = riskLocator.gameObject;
            foxRiskLocator.SetActive(false);
            routeOverlay = NewRect("Selected route overlay", boardPanel.transform,
                Vector2.zero, new Vector2(760, 760));
            routeOverlay.gameObject.AddComponent<RectMask2D>();
            movingResidentMarker = CreateHumanMarker(boardPanel.transform,
                Vector2.zero, 0.8f, "Resident walking");
            movingResidentMarker.SetActive(false);
            // Both leaves are the inside of one box: equal ivory paper, one
            // navy/copper frame. Printed motifs stay at the edges so all live
            // story, metric and action controls remain independent UI.
            var leftFrame = BuildCaseLeaf(canvas.transform, "Observation frame",
                new Vector2(-CaseLeafCenter,
                    openingTourBoardTarget.anchoredPosition.y),
                ConstructionCasePaper.LeftInteriorPrint);
            var rightFrame = BuildCaseLeaf(canvas.transform, "Action frame",
                new Vector2(CaseLeafCenter,
                    openingTourBoardTarget.anchoredPosition.y),
                ConstructionCasePaper.InteriorPrint);
            leftCaseLeaf = leftFrame.rectTransform;
            rightCaseLeaf = rightFrame.rectTransform;
            leftCaseLeafInk = leftFrame.gameObject.AddComponent<CanvasGroup>();
            rightCaseLeafInk = rightFrame.gameObject.AddComponent<CanvasGroup>();
            storyOakIcon = LoadSprite(LeftPanelIconRoot + "oak_print");
            observationLedger = Panel("Observation ledger", canvas.transform,
                new Vector2(-CaseLeafCenter, -29 - BoardCellSpacing),
                new Vector2(326, 292), Ink);
            observationLedger.raycastTarget = false;
            Panel("Ledger brass trim", observationLedger.transform,
                Vector2.zero, new Vector2(322, 288), Gold).raycastTarget = false;
            Panel("Ledger paper", observationLedger.transform,
                Vector2.zero, new Vector2(318, 284), ReadingPaper)
                .raycastTarget = false;
            openingTourReviewTarget = NewRect("Review tour target",
                observationLedger.transform, new Vector2(-17, 0),
                new Vector2(278, 286));
            routePanel = Panel("Route inspection", canvas.transform,
                new Vector2(-CaseLeafCenter, -126 - BoardCellSpacing),
                new Vector2(310, 90),
                ReadingPaper);
            routePanel.raycastTarget = false;
            caseRouteInformation = routePanel.gameObject.AddComponent<CanvasGroup>();
            routeHeading = TextAt("Route heading", routePanel.transform,
                new Vector2(-25, 25), new Vector2(170, 28), "ROUTE", 19, true,
                HeadingInk, TextAnchor.MiddleLeft);
            routeHeading.raycastTarget = false;
            routeText = TextAt("Route status", routePanel.transform,
                new Vector2(0, -17), new Vector2(270, 50), "", 14, false,
                Ink, TextAnchor.MiddleLeft);
            routeText.raycastTarget = false;
            Panel("Route divider", routePanel.transform,
                new Vector2(0, 43), new Vector2(276, 2), Gold)
                .raycastTarget = false;
            routeSuccessIcon = Panel("Route success icon", routePanel.transform,
                new Vector2(-130, 25), new Vector2(27, 27), Color.white);
            routeSuccessIcon.sprite = LoadSprite(LeftPanelIconRoot + "route_success");
            routeSuccessIcon.preserveAspect = true;
            routeSuccessIcon.raycastTarget = false;
            routeSuccessIcon.gameObject.SetActive(false);
            openDispatchButton = ButtonAt("Open resident dispatch",
                routePanel.transform, new Vector2(88, 25), new Vector2(78, 32),
                "Dispatch →", 12, UsedGreen, Paper,
                () => OpenResidentDispatch(), themed: false);
            var dispatchOutline = openDispatchButton.gameObject.AddComponent<Outline>();
            dispatchOutline.effectColor = Gold;
            dispatchOutline.effectDistance = new Vector2(1f, -1f);
            AttachHover(openDispatchButton.gameObject,
                () => ConstructionIconCatalog.GetBuild(ConstructionCategory.Residence),
                () => L("Resident dispatch", "居民调度"),
                () => L("Choose work and meal places for this home. Change them freely without using a build.",
                    "为这户选择上班与用餐地点；可随时调换，不占用建造次数。"),
                TooltipLocation.LeftLeaf);

            // Keep the original painted sprite available for tests and later
            // popovers, but do not leave a tall permanent sidebar on screen.
            var side = Panel("Building choices", canvas.transform,
                Vector2.zero, new Vector2(1600, 900), Color.clear);
            ApplySkin(side, "UI/PauseMenu/pause-menu-panel-v01");
            side.color = Color.clear;
            side.raycastTarget = false;
            caseInformation = side.gameObject.AddComponent<CanvasGroup>();
            var storyBacking = Panel("Story cue backing", side.transform,
                new Vector2(-CaseLeafCenter, 215),
                new Vector2(350, 266), Color.clear);
            storyBacking.raycastTarget = false;
            if (handMode)
            {
                // The hand game has no permanent settlement/route ledger.
                // Keep manual resident choices reachable beside the scores.
                openDispatchButton.transform.SetParent(storyBacking.transform,
                    false);
                SetRect(openDispatchButton.GetComponent<RectTransform>(),
                    new Vector2(0f, -161f), new Vector2(186f, 34f));
                var dispatchLabel = openDispatchButton.GetComponentInChildren<Text>();
                dispatchLabel.rectTransform.sizeDelta = new Vector2(180f, 32f);
                dispatchLabel.fontSize = 16;
            }
            // Focus the task copy below the heading row. The Skip tips button
            // shares that row and must remain outside the yellow tour frame.
            openingTourTodayTarget = NewRect("Today tour target",
                storyBacking.transform, new Vector2(-12, 49),
                new Vector2(278, 58));
            openingTourScoreTarget = NewRect("Score tour target",
                storyBacking.transform, new Vector2(0, -105),
                new Vector2(316, 56));
            visitorMarker = CreateHumanMarker(side.transform,
                new Vector2(-CaseLeafCenter - 148f, 250), 1.0f,
                "Waiting visitor");
            todayHeading = TextAt("Today heading", storyBacking.transform,
                new Vector2(-18, 99), new Vector2(150, 28),
                "TODAY", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            todayHeading.raycastTarget = false;
            skipTutorialButton = ButtonAt("Skip tutorial tips",
                storyBacking.transform, new Vector2(88, 99),
                new Vector2(54, 27), "Skip", 12, Ink, Paper,
                ToggleTutorialTips, themed: false);
            skipTutorialLabel = skipTutorialButton.GetComponentInChildren<Text>();
            AttachHover(skipTutorialButton.gameObject, () => null,
                () => run?.TutorialTipsSkipped == true
                    ? L("Show tips", "显示说明") : run?.UsesFillHand == true
                        ? L("Skip tutorial", "跳过教程")
                        : L("Skip tips", "跳过说明"),
                () => run?.UsesFillHand == true
                    ? L("Enter free building now. Fixed lesson cards and their order end.",
                        "立即进入自由建造，解除教程牌和顺序限制。")
                    : L("Only hides lesson text and focus. Build the required room each day to continue.",
                        "只收起讲解和聚焦框；每天仍需建好指定房间才能继续。"));
            storyText = TextAt("Story instruction", storyBacking.transform,
                new Vector2(0, 35), new Vector2(250, 100), "", 17, true,
                Ink, TextAnchor.MiddleLeft);
            storyText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            storyPreviewText = TextAt("Story preview", storyBacking.transform,
                new Vector2(-12, 47), new Vector2(278, 66), "", 15, true,
                Ink, TextAnchor.MiddleLeft);
            storyPreviewText.raycastTarget = false;
            roomFunctionText = TextAt("Room function", storyBacking.transform,
                new Vector2(-12, -25), new Vector2(278, 72), "", 14, false,
                Ink, TextAnchor.MiddleLeft);
            roomFunctionText.raycastTarget = false;
            storyCueIcon = Panel("Story pictogram", storyBacking.transform,
                new Vector2(-110, 99), new Vector2(34, 34), Color.white);
            storyCueIcon.preserveAspect = true;
            storyCueIcon.raycastTarget = true;
            AttachHover(storyCueIcon.gameObject, () => storyCueIcon.sprite,
                () => todayHeading.text, () => storyText.text,
                TooltipLocation.LeftLeaf);
            storyDivider = Panel("Story divider", storyBacking.transform,
                new Vector2(-12, -64), new Vector2(278, 2),
                new Color(0.45f, 0.51f, 0.36f, 0.8f));
            storyDivider.raycastTarget = false;
            scoreText = TextAt("Observed outcomes", storyBacking.transform,
                new Vector2(0, -63), new Vector2(250, 72), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            scoreText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            animalMetricText = MetricCard(storyBacking.transform,
                "Animal score metric", new Vector2(-80, -105),
                LoadSprite(LeftPanelIconRoot + "animal_score") ??
                    ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.AnimalFood),
                () => L("Animal score", "动物得分"), AnimalScoreTooltip,
                width: 148f, caption: () => L("ANIMALS", "动物"),
                scoreSide: ScoreSide.Animal);
            humanMetricText = MetricCard(storyBacking.transform,
                "Work cycles metric", new Vector2(80, -105),
                LoadSprite(LeftPanelIconRoot + "resident_score") ??
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.CompletedCommute),
                () => L("Resident score", "居民得分"), ResidentScoreTooltip,
                width: 148f, caption: () => L("PEOPLE", "居民"),
                scoreSide: ScoreSide.Resident);
            paletteBackdrop = Panel("Build palette backing", side.transform,
                new Vector2(CaseLeafCenter - 3f, 15),
                new Vector2(370, 680), Paper);
            ApplySkin(paletteBackdrop, "UI/PauseMenu/pause-menu-settings-panel-v01");
            paletteBackdrop.raycastTarget = true;
            paletteNotice = TextAt("Palette notice", paletteBackdrop.transform,
                new Vector2(0, 200), new Vector2(320, 30), "", 17, true,
                Ink, TextAnchor.MiddleCenter);
            paletteNotice.raycastTarget = false;
            BuildCategoryButtons(side.transform);
            BuildResidenceConditionCards(side.transform);
            BuildFillHand(canvas.transform);
            BuildSpecialTasks(canvas.transform);
            if (handMode) BuildGoalOverview(side.transform);
            if (handMode) BuildFillIntroGuide(storyBacking.transform);
            dayReviewPanel = Panel("Last day review", side.transform,
                new Vector2(-CaseLeafCenter, 20 - BoardCellSpacing),
                new Vector2(310, 185),
                ReadingPaper);
            dayReviewPanel.raycastTarget = false;
            reviewHeading = TextAt("Review heading", dayReviewPanel.transform,
                new Vector2(-5, 68), new Vector2(270, 28),
                "LAST DAY", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            reviewHeading.raycastTarget = false;
            Panel("Review divider", dayReviewPanel.transform,
                new Vector2(0, 48), new Vector2(276, 2), Gold)
                .raycastTarget = false;
            dayReviewText = TextAt("Day review text", dayReviewPanel.transform,
                new Vector2(0, -52), new Vector2(270, 76), "", 14, false,
                Ink, TextAnchor.UpperLeft);
            dayReviewText.raycastTarget = true;
            dayReviewText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            AttachHover(dayReviewText.gameObject, () => null,
                () => reviewHeading.text, () => dayReviewDetails,
                TooltipLocation.LeftLeaf);
            BuildPrimerRules(dayReviewPanel.transform);
            dayAnimalMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday animal points", new Vector2(-102, 18),
                ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.AnimalFood),
                () => L("Yesterday: animal score", "昨日动物得分"),
                DailyAnimalScoreTooltip, 94f);
            dayHumanMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday work cycles", new Vector2(0, 18),
                ConstructionIconCatalog.GetStatus(
                    ConstructionStatusIcon.CompletedCommute),
                () => L("Yesterday: work cycles", "昨日完整通勤"),
                YesterdayWorkTooltip, 94f);
            dayWasteMetricText = MetricCard(dayReviewPanel.transform,
                "Yesterday waste cleared", new Vector2(102, 18),
                ConstructionIconCatalog.GetBuild(ConstructionCategory.Waste),
                () => L("Yesterday: waste cleared", "昨日垃圾清运"),
                YesterdayWasteTooltip, 94f);
            feedbackPanel = Panel("Feedback backing", side.transform,
                new Vector2(CaseLeafCenter, -123),
                new Vector2(342, 124), Ink);
            feedbackPanel.color = new Color(Ink.r, Ink.g, Ink.b, 0.97f);
            var feedbackBorder = feedbackPanel.gameObject.AddComponent<Outline>();
            feedbackBorder.effectColor = Gold;
            feedbackBorder.effectDistance = new Vector2(1f, -1f);
            Panel("Feedback divider", feedbackPanel.transform,
                new Vector2(0, 59), new Vector2(332, 3), Gold)
                .raycastTarget = false;
            feedbackText = TextAt("Feedback", feedbackPanel.transform,
                new Vector2(0, -6), new Vector2(250, 94), "", 17, false,
                Ink, TextAnchor.MiddleLeft);
            feedbackText.raycastTarget = false;
            feedbackText.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            feedbackIcon = Panel("Feedback icon", feedbackPanel.transform,
                new Vector2(-137, 31), new Vector2(30, 30), Color.white);
            feedbackIcon.sprite = ConstructionIconCatalog.GetStatus(
                ConstructionStatusIcon.AnimalFood);
            feedbackIcon.preserveAspect = true;
            feedbackIcon.raycastTarget = false;
            feedbackHeading = TextAt("Feedback heading", feedbackPanel.transform,
                new Vector2(0, 31), new Vector2(230, 26), "", 17, true,
                Paper, TextAnchor.MiddleLeft);
            feedbackHeading.raycastTarget = false;
            feedbackRiskIcon = Panel("Feedback fox risk", feedbackPanel.transform,
                new Vector2(141, 31), new Vector2(27, 27), Color.white);
            feedbackRiskIcon.sprite = ConstructionIconCatalog.GetStatus(
                ConstructionStatusIcon.RiskAlert);
            feedbackRiskIcon.preserveAspect = true;
            feedbackRiskIcon.raycastTarget = true;
            var feedbackRiskButton = feedbackRiskIcon.gameObject
                .AddComponent<Button>();
            feedbackRiskButton.targetGraphic = feedbackRiskIcon;
            feedbackRiskButton.onClick.AddListener(() => FocusPredationRisk());
            feedbackRiskIcon.gameObject.AddComponent<ConstructionRoomBadgeHover>()
                .Initialize(() =>
                {
                    fixedRiskHovered = true;
                    RefreshFixedInfoCard();
                }, () =>
                {
                    fixedRiskHovered = false;
                    RefreshFixedInfoCard();
                });
            feedbackPreviewText = TextAt("Feedback preview", feedbackPanel.transform,
                new Vector2(0, -25), new Vector2(300, 70), "", 14, false,
                Paper, TextAnchor.UpperLeft);
            feedbackPreviewText.raycastTarget = false;
            feedbackPanel.raycastTarget = true;
            var feedbackClick = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerClick };
            feedbackClick.callback.AddListener(_ => FocusPredationRisk());
            var feedbackTrigger = feedbackPanel.gameObject.AddComponent<EventTrigger>();
            feedbackTrigger.triggers.Add(feedbackClick);
            feedbackPanel.gameObject.SetActive(false);
            progressPanel = Panel("Board progress backing", side.transform,
                new Vector2(CaseLeafCenter, 204),
                new Vector2(342, 132), Color.clear);
            openingTourProgressTarget = NewRect("Progress tour target",
                progressPanel.transform, new Vector2(20, 0),
                new Vector2(292, 132));
            progressPanel.raycastTarget = false;
            progressHeading = TextAt("Progress heading", progressPanel.transform,
                new Vector2(0, 36), new Vector2(250, 28),
                "BOARD", 19, true, HeadingInk, TextAnchor.MiddleLeft);
            progressHeading.raycastTarget = false;
            progressText = TextAt("Board progress", progressPanel.transform,
                new Vector2(21, -10), new Vector2(208, 40), "", 22, true,
                Ink, TextAnchor.MiddleLeft);
            balanceScoreText = TextAt("Balance score", progressPanel.transform,
                new Vector2(0, -49), new Vector2(292, 38), "", 14, true,
                HeadingInk, TextAnchor.MiddleLeft);
            balanceScoreText.raycastTarget = true;
            AttachHover(balanceScoreText.gameObject,
                () => ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.CompletedCommute),
                () => L("Shared score", "共栖得分"), BalanceScoreTooltip,
                TooltipLocation.BoardInfo);
            var scoreStripImage = Panel("Endless score strip", progressPanel.transform,
                new Vector2(0, -47), new Vector2(300, 52),
                new Color(Paper.r, Paper.g, Paper.b, 0.96f));
            endlessScoreStrip = scoreStripImage.gameObject;
            foreach (var ruleY in new[] { -26f, 26f })
                Panel("Score strip rule", endlessScoreStrip.transform,
                    new Vector2(0, ruleY), new Vector2(300, 1), Gold)
                    .raycastTarget = false;
            foreach (var separatorX in new[] { -49f, 49f })
                Panel("Score separator", endlessScoreStrip.transform,
                    new Vector2(separatorX, 0), new Vector2(1, 30), Gold)
                    .raycastTarget = false;
            for (var index = 0; index < 3; index++)
            {
                var centreX = (index - 1) * 99f;
                endlessScoreCaptions[index] = TextAt($"Score caption {index}",
                    endlessScoreStrip.transform, new Vector2(centreX, 12),
                    new Vector2(94, 24), "", 13, true, HeadingInk,
                    TextAnchor.MiddleCenter);
                endlessScoreValues[index] = TextAt($"Score value {index}",
                    endlessScoreStrip.transform, new Vector2(centreX, -11),
                    new Vector2(94, 30), "", 17, true, Ink,
                    TextAnchor.MiddleCenter);
                endlessScoreCaptions[index].raycastTarget = false;
                endlessScoreValues[index].raycastTarget = false;
            }
            AttachHover(endlessScoreStrip,
                () => ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.CompletedCommute),
                () => L("Shared score", "共栖得分"), BalanceScoreTooltip,
                TooltipLocation.BoardInfo);
            progressIcon = Panel("Board progress icon", progressPanel.transform,
                new Vector2(-107, -17), new Vector2(39, 39), Color.white);
            progressIcon.sprite = ConstructionIconCatalog.GetControl(
                !IsEndless
                    ? ConstructionControlIcon.FillMode
                    : ConstructionControlIcon.EndlessMode);
            progressIcon.preserveAspect = true;
            progressIcon.raycastTarget = false;
            progressPanel.raycastTarget = true;
            AttachHover(progressPanel.gameObject, () => progressIcon.sprite,
                () => run?.UsesFillHand == true
                    ? L("Run score", "本局得分")
                    : !IsEndless
                    ? L("Total plots", "总建造格")
                    : L("Rooms built", "已建房间"),
                () => run == null ? string.Empty : run.UsesFillHand
                    ? BalanceScoreTooltip() : L(
                    (!IsEndless
                        ? $"Built {run.BuiltCount}/{run.BoardCapacity} plots, including the starting oak. "
                        : $"Built {run.BuiltCount} rooms; no room limit. ") +
                    $"Squirrels {run.SquirrelPopulation}, pigeons {run.PigeonPopulation}; " +
                    $"hedgehog {run.HedgehogHabitats.Count}, fox {(run.FoxHabitat != null ? 1 : 0)}; " +
                    $"residents {run.ResidentPopulation}.",
                    (!IsEndless
                        ? $"已建 {run.BuiltCount}/{run.BoardCapacity} 格，含开局橡树；"
                        : $"已建 {run.BuiltCount} 格；房间总数无上限。") +
                    $"松鼠 {run.SquirrelPopulation}、鸽子 {run.PigeonPopulation}，" +
                    $"刺猬 {run.HedgehogHabitats.Count}、狐狸 {(run.FoxHabitat != null ? 1 : 0)}，" +
                    $"居民 {run.ResidentPopulation}。"));
            if (handMode)
                BuildFillScoreSummary();
            BuildEndlessLossStrip(side.transform);
            nextDayButton = ButtonAt("Advance to next day", side.transform,
                new Vector2(CaseLeafCenter + 87f, -230),
                new Vector2(118, 48), "Next day  →", 15,
                new Color(0.17f, 0.40f, 0.36f), Paper, () => AdvanceDay());
            AddButtonIcon(nextDayButton, ConstructionControlIcon.NextDay);
            nextDayLabel = nextDayButton.GetComponentInChildren<Text>();
            buildPaletteButton = ButtonAt("Open build palette", side.transform,
                new Vector2(CaseLeafCenter - 65f, -230),
                new Vector2(118, 48), "Build", 15,
                Ink, Paper, ToggleBuildPalette);
            AddButtonIcon(buildPaletteButton, ConstructionControlIcon.Build);
            buildPaletteLabel = buildPaletteButton.GetComponentInChildren<Text>();

            var cameraCard = Panel("Board game camera status", side.transform,
                new Vector2(CaseLeafCenter, -314),
                new Vector2(310, 110), ReadingPaper);
            boardGameCameraPanel = cameraCard.gameObject;
            cameraCard.raycastTarget = true;
            Panel("Camera card top rule", cameraCard.transform,
                new Vector2(0, 53), new Vector2(294, 2), Gold)
                .raycastTarget = false;
            boardGameCameraHeading = TextAt("Camera status heading",
                cameraCard.transform, new Vector2(-70, 36),
                new Vector2(150, 30), "", 16, true, HeadingInk,
                TextAnchor.MiddleLeft);
            boardGameCameraHeading.text = L("CAMERA", "摄像头");
            boardGameCameraHeading.raycastTarget = false;
            boardGameCameraStatusText = TextAt("Camera connection status",
                cameraCard.transform, new Vector2(0, 0),
                new Vector2(276, 44), "", 14, true, Ink,
                TextAnchor.MiddleLeft);
            boardGameCameraStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            boardGameCameraStatusText.verticalOverflow = VerticalWrapMode.Truncate;
            boardGameCameraStatusText.raycastTarget = false;
            AttachHover(cameraCard.gameObject, () => null,
                () => L("Camera status", "摄像头状态"),
                () => boardGameCameraStatusText.text, TooltipLocation.BoardInfo);
            boardGameCameraButton = ButtonAt("Start physical camera tool",
                cameraCard.transform, new Vector2(0, -39),
                new Vector2(178, 29), L("Start camera", "启动摄像头"),
                14, Ink, Paper,
                StartBoardGameCameraTool);
            boardGameCameraButtonLabel =
                boardGameCameraButton.GetComponentInChildren<Text>();
            boardGameCameraPanel.SetActive(IsBoardGame);

            guidePanel = Panel("Next step backing", side.transform,
                new Vector2(CaseLeafCenter, 17),
                new Vector2(342, 130), Color.clear);
            openingTourGuideTarget = NewRect("Guide tour target",
                guidePanel.transform, new Vector2(20, 0),
                new Vector2(292, 130));
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
                () => !IsEndless && run?.TutorialChapter == ConstructionTutorialChapter.Complete &&
                      run.BuiltToday
                    ? L("Built today", "今日已建")
                    : run?.IsIntroPhase == true && !introChoiceMade
                        ? L($"Next: {CategoryLabel(run.IntroRequiredCategory.Value)}",
                            $"下一步：{CategoryLabel(run.IntroRequiredCategory.Value)}")
                    : AwaitingFreeBuildChoice
                        ? L("Your choice", "自行选择") : SelectedBuildLabel(),
                () => placementGuideText.text);
            if (handMode) BuildFillRightHandPreview();
            if (!IsEndless && !handMode)
                Panel("Action divider", side.transform,
                    new Vector2(CaseLeafCenter, 116),
                    new Vector2(250, 2), new Color(0.45f, 0.51f, 0.36f, 0.8f))
                    .raycastTarget = false;
            BuildPlacementConfirmation(side.transform);
            // The case hinges are added to the root Canvas during the opening
            // sequence. Keep this popover at that same level so it can sit
            // above the hardware when opened.
            BuildResidentDispatch(canvas.transform);
            BuildTutorialFocus(canvas.transform);
            BuildScoreMotion(canvas.transform);
            BuildScoreTrace(canvas.transform);
            BuildRoomHighlights(canvas.transform);
            if (handMode)
            {
                BuildRoomIssueIcons();
                BuildIdleCueLayer(canvas.transform);
            }

            finalResultPanel = Panel("Construction final result", canvas.transform,
                Vector2.zero, new Vector2(1600, 900),
                new Color(0.05f, 0.09f, 0.11f, 0.78f)).gameObject;
            Stretch(finalResultPanel.GetComponent<RectTransform>());
            var resultCard = Panel("Result card", finalResultPanel.transform,
                Vector2.zero, new Vector2(800, 800), Paper);
            ApplySkin(resultCard, "UI/PauseMenu/pause-menu-settings-panel-v01");
            resultTitleLabel = TextAt("Result title", resultCard.transform,
                new Vector2(0, 264), new Vector2(696, 52),
                "49-DAY COEXISTENCE", 34, true, Ink, TextAnchor.MiddleCenter);
            resultModeLabel = TextAt("Result mode", resultCard.transform,
                new Vector2(0, 217), new Vector2(680, 25), "", 16, false,
                Ink, TextAnchor.MiddleCenter);
            Panel("Result top divider", resultCard.transform,
                new Vector2(0, 194), new Vector2(646, 2), Gold)
                .raycastTarget = false;
            Panel("Shared score caption border", resultCard.transform,
                new Vector2(-219, 140), new Vector2(177, 49), Gold)
                .raycastTarget = false;
            Panel("Shared score caption paper", resultCard.transform,
                new Vector2(-219, 140), new Vector2(173, 45), Paper)
                .raycastTarget = false;
            resultTotalCaption = TextAt("Shared score caption", resultCard.transform,
                new Vector2(-219, 140), new Vector2(165, 43), "", 22, true,
                Ink, TextAnchor.MiddleCenter);
            finalResultText = TextAt("Result numbers", resultCard.transform,
                new Vector2(89, 140), new Vector2(338, 88), "", 67, true,
                Ink, TextAnchor.MiddleCenter);
            finalResultText.resizeTextForBestFit = true;
            finalResultText.resizeTextMinSize = 36;
            finalResultText.resizeTextMaxSize = 67;
            Panel("Result score divider", resultCard.transform,
                new Vector2(0, 97), new Vector2(650, 2), Gold)
                .raycastTarget = false;
            BuildResultScoreSide(resultCard.transform, -175, "Animal",
                LoadSprite(LeftPanelIconRoot + "animal_score") ??
                    ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.AnimalFood),
                out resultAnimalCaption, out resultAnimalValue);
            BuildResultScoreSide(resultCard.transform, 175, "Resident",
                LoadSprite(LeftPanelIconRoot + "resident_score") ??
                    ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.CompletedCommute),
                out resultResidentCaption, out resultResidentValue);
            Panel("Result score split", resultCard.transform,
                new Vector2(0, 33), new Vector2(2, 86), Gold)
                .raycastTarget = false;
            Panel("Linkage ribbon border", resultCard.transform,
                new Vector2(0, -54), new Vector2(660, 48), Gold)
                .raycastTarget = false;
            resultLinkageRibbon = Panel("Linkage ribbon", resultCard.transform,
                new Vector2(0, -54), new Vector2(656, 44),
                new Color(0.83f, 0.87f, 0.72f, 0.98f));
            resultLinkageRibbon.raycastTarget = false;
            resultLinkageValue = TextAt("Linkage value", resultCard.transform,
                new Vector2(-116, -54), new Vector2(368, 37), "", 22, true,
                Ink, TextAnchor.MiddleCenter);
            resultLinkageDivider = Panel("Linkage divider", resultCard.transform,
                new Vector2(96, -54), new Vector2(2, 28), Gold);
            resultLinkageDivider.raycastTarget = false;
            resultLinkageDetail = TextAt("Linkage detail", resultCard.transform,
                new Vector2(216, -54), new Vector2(225, 32), "", 15, true,
                Ink, TextAnchor.MiddleCenter);
            Panel("Result events divider", resultCard.transform,
                new Vector2(0, -91), new Vector2(650, 2), Gold)
                .raycastTarget = false;
            BuildResultEvent(resultCard.transform, 0, -222);
            BuildResultEvent(resultCard.transform, 1, 0);
            BuildResultEvent(resultCard.transform, 2, 222);
            resultEquationText = TextAt("Result equation", resultCard.transform,
                new Vector2(0, -181), new Vector2(680, 32), "", 18, false,
                Ink, TextAnchor.MiddleCenter);
            resultObservedText = TextAt("Result observed", resultCard.transform,
                new Vector2(0, -211), new Vector2(690, 28), "", 14, false,
                Ink, TextAnchor.MiddleCenter);
            resultNoticeText = TextAt("Result notice", resultCard.transform,
                new Vector2(0, -244), new Vector2(690, 31), "", 14, false,
                Ink, TextAnchor.MiddleCenter);
            var resultReset = ButtonAt("New run after result", resultCard.transform,
                new Vector2(151, -269), new Vector2(245, 48), "New run", 20,
                new Color(0.70f, 0.97f, 0.89f), Paper, OnResetPressed);
            AddButtonIcon(resultReset, ConstructionControlIcon.Restart);
            resultResetLabel = resultReset.GetComponentInChildren<Text>();
            var resultMenu = ButtonAt("Return to menu after result",
                resultCard.transform, new Vector2(-151, -269),
                new Vector2(245, 48), "Menu", 20, Ink, Paper,
                () => { ReturnToMainMenu(); });
            AddButtonIcon(resultMenu, ConstructionControlIcon.Back);
            resultMenuLabel = resultMenu.GetComponentInChildren<Text>();
            // Leave a compact row for the device's own score history.
            for (var childIndex = 0; childIndex < resultCard.transform.childCount;
                 childIndex++)
            {
                if (resultCard.transform.GetChild(childIndex) is RectTransform child)
                    child.anchoredPosition += new Vector2(0, 35);
            }
            BuildResultLeaderboardActions(resultCard.transform);
            BuildAwardEvaluation(resultCard.transform);
            BuildResultReview(canvas.transform);
            finalResultPanel.SetActive(false);
            BuildTooltip(canvas.transform);
            BuildRoomBadgeTooltip(canvas.transform);
            BuildOpeningTour(canvas.transform);
            BuildQuickGuide(canvas.transform);
            BuildDeckInventory(canvas.transform);
            BuildLocalRankingsOverlay(canvas.transform);
            ApplyLanguageFonts();
            RefreshPaletteVisibility();
            PrepareBoardCaseOpening(canvas.transform,
                !openingFromPreparedRestart &&
                SystemInfo.graphicsDeviceType !=
                    UnityEngine.Rendering.GraphicsDeviceType.Null);
            BuildEndlessRulesOverlay(canvas.transform);
            BuildPauseMenu(canvas.transform);
        }

        private void BuildResultScoreSide(Transform parent, float x, string name,
            Sprite sprite, out Text caption, out Text value)
        {
            var area = Panel("Result " + name + " score", parent,
                new Vector2(x, 33), new Vector2(310, 126),
                new Color(1f, 1f, 1f, 0f));
            area.raycastTarget = false;
            var icon = Panel(name + " score icon", area.transform,
                new Vector2(-94, 0), new Vector2(76, 76), Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            caption = TextAt(name + " score label", area.transform,
                new Vector2(35, 32), new Vector2(151, 34), "", 23, true,
                Ink, TextAnchor.MiddleCenter);
            value = TextAt(name + " score value", area.transform,
                new Vector2(35, -20), new Vector2(160, 72), "0", 43, true,
                Ink, TextAnchor.MiddleCenter);
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 24;
            value.resizeTextMaxSize = 43;
        }

        private void BuildResultEvent(Transform parent, int index, float x)
        {
            var border = Panel("Result event " + index, parent,
                new Vector2(x, -130), new Vector2(206, 65), Gold);
            border.raycastTarget = false;
            var paper = Panel("Event paper", border.transform,
                Vector2.zero, new Vector2(202, 61), Paper);
            paper.raycastTarget = false;
            var icon = Panel("Event icon", paper.transform,
                new Vector2(-75, 0), new Vector2(35, 35), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            resultEventIcons[index] = icon;
            resultEventCaptions[index] = TextAt("Event caption", paper.transform,
                new Vector2(15, 15), new Vector2(148, 25), "", 15, true,
                Ink, TextAnchor.MiddleCenter);
            resultEventValues[index] = TextAt("Event value", paper.transform,
                new Vector2(15, -13), new Vector2(148, 31), "", 25, true,
                Ink, TextAnchor.MiddleCenter);
            resultEventValues[index].resizeTextForBestFit = true;
            resultEventValues[index].resizeTextMinSize = 16;
            resultEventValues[index].resizeTextMaxSize = 25;
        }

        private void BuildResultLeaderboardActions(Transform parent)
        {
            Panel("Leaderboard divider", parent, new Vector2(0, -263),
                new Vector2(650, 2), Gold).raycastTarget = false;
            var inputBackground = Panel("Leaderboard name", parent,
                new Vector2(-222, -293), new Vector2(250, 40), Ink);
            var inputBorder = Panel("Nickname paper", inputBackground.transform,
                Vector2.zero, new Vector2(246, 36), ReadingPaper);
            inputBorder.raycastTarget = false;
            resultNicknameInput = inputBackground.gameObject.AddComponent<InputField>();
            resultNicknameInput.targetGraphic = inputBackground;
            resultNicknameInput.characterLimit = 16;
            resultNicknameInput.lineType = InputField.LineType.SingleLine;
            var nicknameText = TextAt("Nickname value", inputBackground.transform,
                Vector2.zero, new Vector2(219, 30), "", 16, false,
                Ink, TextAnchor.MiddleLeft);
            nicknameText.raycastTarget = false;
            var placeholder = TextAt("Nickname placeholder",
                inputBackground.transform, Vector2.zero, new Vector2(219, 30),
                L("Leaderboard name (1–16)", "排行名字（1–16字）"), 15, false,
                new Color(Ink.r, Ink.g, Ink.b, 0.76f), TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            resultNicknameInput.textComponent = nicknameText;
            resultNicknameInput.placeholder = placeholder;
            resultNicknamePlaceholder = placeholder;
            resultNicknameInput.text = PlayerPrefs.GetString(
                LeaderboardNicknamePreferenceKey, string.Empty);

            resultSubmitButton = ButtonAt("Save leaderboard name", parent,
                new Vector2(47, -293), new Vector2(143, 40),
                L("Save name", "保存名字"), 16, Ink, Paper,
                SaveLocalLeaderboardName);
            resultSubmitLabel = resultSubmitButton.GetComponentInChildren<Text>();
            var viewButton = ButtonAt("Open local rankings", parent,
                new Vector2(241, -293), new Vector2(161, 40),
                L("Local ranks", "本地排行"), 16, Ink, Paper,
                OpenLocalRankings);
            resultViewRankingLabel = viewButton.GetComponentInChildren<Text>();
            resultLeaderboardStatus = TextAt("Leaderboard status", parent,
                new Vector2(0, -327), new Vector2(660, 20), "", 13, false,
                Ink, TextAnchor.MiddleCenter);
            resultLeaderboardStatus.raycastTarget = false;
            BuildNetworkLeaderboardStatus(parent);
        }

        private void BuildLocalRankingsOverlay(Transform parent)
        {
            localRankingsOverlay = Panel("Local rankings overlay", parent,
                Vector2.zero, new Vector2(1600, 900),
                new Color(0.05f, 0.09f, 0.11f, 0.90f)).gameObject;
            Stretch(localRankingsOverlay.GetComponent<RectTransform>());
            var card = Panel("Local rankings card", localRankingsOverlay.transform,
                Vector2.zero, new Vector2(800, 520), Paper);
            ApplySkin(card, "UI/PauseMenu/pause-menu-settings-panel-v01");
            localRankingsCard = card.rectTransform;
            localRankingsTitle = TextAt("Local rankings title", card.transform,
                Vector2.zero, new Vector2(660, 44), "", 29, true,
                Ink, TextAnchor.MiddleCenter);
            AnchorRankingFromTop(localRankingsTitle.rectTransform, 0, 98);
            localRankingsSubtitle = TextAt("Local rankings subtitle",
                card.transform, Vector2.zero, new Vector2(660, 26),
                "", 14, false, Ink, TextAnchor.MiddleCenter);
            AnchorRankingFromTop(localRankingsSubtitle.rectTransform, 0, 135);

            var modes = new[] { ConstructionGameMode.Fill,
                ConstructionGameMode.Endless, ConstructionGameMode.BoardGame };
            for (var i = 0; i < modes.Length; i++)
            {
                var index = i;
                var mode = modes[i];
                var button = ButtonAt($"Local ranks {mode}", card.transform,
                    Vector2.zero, new Vector2(176, 38),
                    "", 17, Ink, Paper, () => SelectLocalRankingsMode(mode));
                AnchorRankingFromTop(button.GetComponent<RectTransform>(),
                    (i - 1) * 190, 188);
                localRankingsModeFaces[index] = button.GetComponent<Image>();
                localRankingsModeLabels[index] = TextAt("Mode label",
                    button.transform, Vector2.zero, new Vector2(176, 38),
                    "", 17, true, Paper, TextAnchor.MiddleCenter);
                localRankingsModeLabels[index].raycastTarget = false;
            }
            var headingRule = Panel("Rankings heading divider", card.transform,
                Vector2.zero, new Vector2(655, 2), Gold);
            AnchorRankingFromTop(headingRule.rectTransform, 0, 226);
            headingRule.raycastTarget = false;
            var headings = new[] { "Rank", "Name", "Shared", "Animal",
                "Resident", "Days" };
            var x = new[] { -292f, -170f, -32f, 77f, 177f, 273f };
            var widths = new[] { 60f, 170f, 92f, 82f, 88f, 90f };
            for (var column = 0; column < headings.Length; column++)
            {
                localRankingsHeaders[column] = TextAt(
                    "Rankings header " + headings[column], card.transform,
                    Vector2.zero, new Vector2(widths[column], 25),
                    "", 14, true, Ink,
                    column == 1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                AnchorRankingFromTop(localRankingsHeaders[column].rectTransform,
                    x[column], 252);
                localRankingsHeaders[column].raycastTarget = false;
            }
            for (var row = 0; row < 10; row++)
            {
                var top = 285 + row * 31;
                localRankingsRowFaces[row] = Panel(
                    "Local rankings row " + (row + 1), card.transform,
                    Vector2.zero, new Vector2(655, 29),
                    row % 2 == 0
                        ? new Color(1f, 0.99f, 0.93f, 0.54f)
                        : new Color(0.83f, 0.78f, 0.65f, 0.28f));
                AnchorRankingFromTop(localRankingsRowFaces[row].rectTransform,
                    0, top);
                localRankingsRowFaces[row].raycastTarget = false;
                for (var column = 0; column < 6; column++)
                {
                    localRankingsCells[row, column] = TextAt(
                        $"Rank {row + 1} column {column}", card.transform,
                        Vector2.zero,
                        new Vector2(widths[column], 28), "", 16,
                        column == 2, Ink,
                        column == 1 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                    AnchorRankingFromTop(
                        localRankingsCells[row, column].rectTransform,
                        x[column], top);
                }
                localRankingsCells[row, 1].resizeTextForBestFit = true;
                localRankingsCells[row, 1].resizeTextMinSize = 12;
                localRankingsCells[row, 1].resizeTextMaxSize = 16;
                localRankingsAwardIcons[row] = Panel(
                    $"Rank {row + 1} award icon", card.transform,
                    Vector2.zero, new Vector2(21, 21), Color.white);
                AnchorRankingFromTop(localRankingsAwardIcons[row].rectTransform,
                    241, top);
                localRankingsAwardIcons[row].preserveAspect = true;
                localRankingsAwardIcons[row].raycastTarget = false;
                localRankingsAwardIcons[row].gameObject.SetActive(false);
            }
            localRankingsEmpty = TextAt("Local rankings empty", card.transform,
                Vector2.zero, new Vector2(620, 35), "", 17, false,
                Ink, TextAnchor.MiddleCenter);
            AnchorRankingFromTop(localRankingsEmpty.rectTransform, 0, 285);
            localRankingsEmpty.raycastTarget = false;
            var footRule = Panel("Rankings foot divider", card.transform,
                Vector2.zero, new Vector2(655, 2), Gold);
            localRankingsFooterRule = footRule.rectTransform;
            footRule.raycastTarget = false;

            localRankingsNameEditor = NewRect("Edit my ranking name",
                card.transform, Vector2.zero, new Vector2(655, 42));
            localRankingsNameCaption = TextAt("My ranking name caption",
                localRankingsNameEditor,
                new Vector2(-271, 0), new Vector2(110, 36),
                L("MY NAME", "我的名字"), 15, true, Ink,
                TextAnchor.MiddleLeft);
            localRankingsNameCaption.raycastTarget = false;
            var nameBackground = Panel("My ranking name input",
                localRankingsNameEditor, new Vector2(-69, 0),
                new Vector2(290, 38), Ink);
            Panel("My ranking name paper", nameBackground.transform,
                Vector2.zero, new Vector2(286, 34), ReadingPaper)
                .raycastTarget = false;
            localRankingsNicknameInput = nameBackground.gameObject
                .AddComponent<InputField>();
            localRankingsNicknameInput.targetGraphic = nameBackground;
            localRankingsNicknameInput.characterLimit = 16;
            localRankingsNicknameInput.lineType = InputField.LineType.SingleLine;
            var nameValue = TextAt("My ranking name value",
                nameBackground.transform, Vector2.zero,
                new Vector2(262, 30), "", 16, false,
                Ink, TextAnchor.MiddleLeft);
            nameValue.raycastTarget = false;
            var namePlaceholder = TextAt("My ranking name placeholder",
                nameBackground.transform, Vector2.zero,
                new Vector2(262, 30), L("1–16 characters", "1–16 个字"),
                14, false, new Color(Ink.r, Ink.g, Ink.b, 0.76f),
                TextAnchor.MiddleLeft);
            namePlaceholder.raycastTarget = false;
            localRankingsNamePlaceholder = namePlaceholder;
            localRankingsNicknameInput.textComponent = nameValue;
            localRankingsNicknameInput.placeholder = namePlaceholder;
            var saveName = ButtonAt("Save my ranking name",
                localRankingsNameEditor, new Vector2(239, 0),
                new Vector2(145, 38), L("Save name", "保存名字"), 16,
                Ink, Paper, SaveNameFromLocalRankings);
            localRankingsSaveLabel = saveName.GetComponentInChildren<Text>();
            localRankingsEditStatus = TextAt("Ranking name status",
                card.transform, Vector2.zero, new Vector2(650, 23),
                "", 13, false, Ink, TextAnchor.MiddleCenter);
            localRankingsEditStatus.raycastTarget = false;
            var close = ButtonAt("Close local rankings", card.transform,
                Vector2.zero, new Vector2(175, 38),
                L("Close", "关闭"), 18, Ink, Paper,
                () => localRankingsOverlay.SetActive(false));
            localRankingsCloseRect = close.GetComponent<RectTransform>();
            localRankingsCloseLabel = close.GetComponentInChildren<Text>();
            var review = ButtonAt("Review layout from local rankings", card.transform,
                Vector2.zero, new Vector2(175, 38), L("Review layout", "收起复盘"),
                18, Ink, Paper, () => SetResultReview(true));
            localRankingsReviewRect = review.GetComponent<RectTransform>();
            localRankingsReviewLabel = review.GetComponentInChildren<Text>();
            localRankingsOverlay.SetActive(false);
        }

        private void BuildEndlessLossStrip(Transform parent)
        {
            var backing = Panel("Endless ending progress", parent,
                new Vector2(CaseLeafCenter, 108), new Vector2(300, 60),
                new Color(Paper.r, Paper.g, Paper.b, 0.96f));
            endlessLossStrip = backing.gameObject;
            backing.raycastTarget = false;
            foreach (var y in new[] { -29f, 29f })
                Panel("Ending strip rule", backing.transform,
                    new Vector2(0, y), new Vector2(300, 1), Gold)
                    .raycastTarget = false;
            Panel("Ending strip separator", backing.transform,
                Vector2.zero, new Vector2(1, 48), Gold).raycastTarget = false;

            var deaths = Panel("Recent animal deaths", backing.transform,
                new Vector2(-75, 0), new Vector2(148, 58), Color.clear);
            endlessDeathCaption = TextAt("Animal deaths heading", deaths.transform,
                new Vector2(0, 14), new Vector2(142, 24), "", 13, true,
                HeadingInk, TextAnchor.MiddleCenter);
            endlessDeathValue = TextAt("Animal deaths count", deaths.transform,
                new Vector2(0, -13), new Vector2(142, 30), "", 18, true,
                UsedGreen, TextAnchor.MiddleCenter);
            endlessDeathCaption.raycastTarget = false;
            endlessDeathValue.raycastTarget = false;
            AttachHover(deaths.gameObject,
                () => ConstructionIconCatalog.GetStatus(
                    ConstructionStatusIcon.RiskAlert),
                () => L("Animal deaths", "动物死亡"),
                () => L(
                    $"Deaths in the last {ConstructionRunModel.AnimalDeathWindowDays} settled days: " +
                    $"{run.RecentAnimalDeaths}/{run.AnimalDeathLimit}. " +
                    "A returning animal does not erase a recorded death.",
                    $"最近 {ConstructionRunModel.AnimalDeathWindowDays} 个结算日死亡：" +
                    $"{run.RecentAnimalDeaths}/{run.AnimalDeathLimit} 只。" +
                    "动物回归不会抹去已记录的死亡。"),
                TooltipLocation.BoardInfo);

            var unemployed = Panel("Residents without work", backing.transform,
                new Vector2(75, 0), new Vector2(148, 58), Color.clear);
            endlessUnemployedCaption = TextAt("Unemployment heading",
                unemployed.transform, new Vector2(0, 14),
                new Vector2(142, 24), "", 13, true,
                HeadingInk, TextAnchor.MiddleCenter);
            endlessUnemployedValue = TextAt("Unemployment count",
                unemployed.transform, new Vector2(0, -13),
                new Vector2(142, 30), "", 18, true,
                UsedGreen, TextAnchor.MiddleCenter);
            endlessUnemployedCaption.raycastTarget = false;
            endlessUnemployedValue.raycastTarget = false;
            AttachHover(unemployed.gameObject,
                () => ConstructionIconCatalog.GetStatus(
                    ConstructionStatusIcon.CompletedCommute),
                () => L("Residents who did not work", "未上班居民"),
                () => L(
                    $"Last day: {run.UnemployedResidentsLastDay}/" +
                    $"{run.UnemployedResidentLimit} residents did not work. " +
                    $"At the limit for {run.UnemployedDaysInRow}/" +
                    $"{ConstructionRunModel.UnemployedConsecutiveDays} days in a row. " +
                    "Missing jobs or blocked routes count; working without a meal does not.",
                    $"上一天未上班居民 {run.UnemployedResidentsLastDay}/" +
                    $"{run.UnemployedResidentLimit} 人；连续达到阈值 " +
                    $"{run.UnemployedDaysInRow}/" +
                    $"{ConstructionRunModel.UnemployedConsecutiveDays} 天。" +
                    "岗位不足或路线受阻都计入；已上班但没吃饭不计入。"),
                TooltipLocation.BoardInfo);
            endlessLossStrip.SetActive(false);
        }

        private void BuildEndlessRulesOverlay(Transform parent)
        {
            var shade = Panel("Construction Endless rules overlay", parent,
                Vector2.zero, new Vector2(1600, 900),
                new Color(0.04f, 0.07f, 0.09f, 0.82f));
            Stretch(shade.rectTransform);
            endlessRulesOverlay = shade.gameObject;
            var border = Panel("Rules card border", shade.transform,
                Vector2.zero, new Vector2(724, 636), Gold);
            var card = Panel("Rules card", border.transform,
                Vector2.zero, new Vector2(718, 630), Paper);
            Panel("Rules header", card.transform,
                new Vector2(0, 267), new Vector2(718, 76), Ink)
                .raycastTarget = false;
            endlessRulesTitle = TextAt("Rules title", card.transform,
                new Vector2(0, 266), new Vector2(650, 46), "", 29,
                true, Paper, TextAnchor.MiddleCenter);
            endlessRulesIntro = TextAt("Rules introduction", card.transform,
                new Vector2(0, 188), new Vector2(630, 42), "", 17,
                false, Ink, TextAnchor.MiddleCenter);

            var deathRow = Panel("Death limit row", card.transform,
                new Vector2(0, 103), new Vector2(646, 78), ReadingPaper);
            Panel("Death row marker", deathRow.transform,
                new Vector2(-318, 0), new Vector2(6, 78), IdleAmber)
                .raycastTarget = false;
            endlessRulesDeathLabel = TextAt("Death limit label", deathRow.transform,
                new Vector2(-153, 15), new Vector2(310, 40), "", 21,
                true, Ink, TextAnchor.MiddleLeft);
            endlessRulesDeathRecommendation = TextAt("Death recommendation",
                deathRow.transform, new Vector2(-153, -21),
                new Vector2(310, 27), "", 15, false,
                UsedGreen, TextAnchor.MiddleLeft);
            endlessRulesAdjustmentButtons.Add(ButtonAt("Decrease death limit", deathRow.transform,
                new Vector2(92, 0), new Vector2(42, 42), "−", 22,
                Ink, Paper, () => AdjustEndlessRule(0, -1)));
            endlessRulesDeathValue = TextAt("Death limit value", deathRow.transform,
                new Vector2(166, 0), new Vector2(70, 42), "", 23,
                true, Ink, TextAnchor.MiddleCenter);
            endlessRulesAdjustmentButtons.Add(ButtonAt("Increase death limit", deathRow.transform,
                new Vector2(242, 0), new Vector2(42, 42), "+", 22,
                Ink, Paper, () => AdjustEndlessRule(0, 1)));

            var workRow = Panel("Unemployment limit row", card.transform,
                new Vector2(0, 6), new Vector2(646, 78), ReadingPaper);
            Panel("Unemployment row marker", workRow.transform,
                new Vector2(-318, 0), new Vector2(6, 78), IdleAmber)
                .raycastTarget = false;
            endlessRulesUnemployedLabel = TextAt("Unemployment limit label",
                workRow.transform, new Vector2(-153, 15),
                new Vector2(310, 40), "", 21,
                true, Ink, TextAnchor.MiddleLeft);
            endlessRulesUnemployedRecommendation = TextAt(
                "Unemployment recommendation", workRow.transform,
                new Vector2(-153, -21), new Vector2(310, 27), "", 15,
                false, UsedGreen, TextAnchor.MiddleLeft);
            endlessRulesAdjustmentButtons.Add(ButtonAt("Decrease unemployment limit", workRow.transform,
                new Vector2(92, 0), new Vector2(42, 42), "−", 22,
                Ink, Paper, () => AdjustEndlessRule(1, -1)));
            endlessRulesUnemployedValue = TextAt("Unemployment limit value",
                workRow.transform, new Vector2(166, 0),
                new Vector2(70, 42), "", 23,
                true, Ink, TextAnchor.MiddleCenter);
            endlessRulesAdjustmentButtons.Add(ButtonAt("Increase unemployment limit", workRow.transform,
                new Vector2(242, 0), new Vector2(42, 42), "+", 22,
                Ink, Paper, () => AdjustEndlessRule(1, 1)));

            endlessRulesSummary = TextAt("Rules summary", card.transform,
                new Vector2(0, -120), new Vector2(630, 150), "", 18,
                false, Ink, TextAnchor.MiddleLeft);
            Panel("Rules footer divider", card.transform,
                new Vector2(0, -214), new Vector2(630, 2), Gold)
                .raycastTarget = false;
            var cancel = ButtonAt("Cancel rules", card.transform,
                new Vector2(-220, -255), new Vector2(172, 52), "Cancel", 17,
                Ink, Paper, CancelEndlessRules);
            endlessRulesCancelButton = cancel;
            endlessRulesCancelLabel = cancel.GetComponentInChildren<Text>();
            var recommend = ButtonAt("Use recommended rules", card.transform,
                new Vector2(0, -255), new Vector2(198, 52), "Recommended", 16,
                Ink, Paper, UseRecommendedEndlessRules);
            endlessRulesRecommendedButton = recommend;
            endlessRulesRecommendedLabel = recommend.GetComponentInChildren<Text>();
            var confirm = ButtonAt("Confirm ending rules", card.transform,
                new Vector2(220, -255), new Vector2(172, 52), "Confirm", 17,
                UsedGreen, Paper, ConfirmEndlessRules);
            endlessRulesConfirmButton = confirm;
            endlessRulesConfirmLabel = confirm.GetComponentInChildren<Text>();
            endlessRulesOverlay.SetActive(false);
        }

        private void OpenEndlessRules(bool newRun)
        {
            if (!IsEndless || run == null || endlessRulesOverlay == null) return;
            if (run.IsFinished && !newRun) return;
            ClearResetConfirmation();
            ClearPendingPlacement();
            buildPaletteOpen = false;
            RefreshPaletteVisibility();
            HideTooltip();
            endlessRulesNewRunPending = newRun;
            draftAnimalDeathLimit = newRun || !run.EndlessLossRulesConfirmed
                ? ConstructionRunModel.RecommendedAnimalDeathLimit
                : run.AnimalDeathLimit;
            draftUnemployedResidentLimit = newRun || !run.EndlessLossRulesConfirmed
                ? ConstructionRunModel.RecommendedUnemployedResidentLimit
                : run.UnemployedResidentLimit;
            RefreshEndlessRulesUi();
            endlessRulesOverlay.SetActive(true);
            endlessRulesOverlay.transform.SetAsLastSibling();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        private void AdjustEndlessRule(int rule, int change)
        {
            if (endlessRulesOverlay == null || !endlessRulesOverlay.activeSelf ||
                (run.EndlessLossRulesConfirmed && !endlessRulesNewRunPending)) return;
            if (rule == 0)
                draftAnimalDeathLimit = Mathf.Clamp(draftAnimalDeathLimit + change,
                    ConstructionRunModel.MinimumEndlessLossLimit,
                    ConstructionRunModel.MaximumEndlessLossLimit);
            else
                draftUnemployedResidentLimit = Mathf.Clamp(
                    draftUnemployedResidentLimit + change,
                    ConstructionRunModel.MinimumEndlessLossLimit,
                    ConstructionRunModel.MaximumEndlessLossLimit);
            RefreshEndlessRulesUi();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        private void UseRecommendedEndlessRules()
        {
            if (run.EndlessLossRulesConfirmed && !endlessRulesNewRunPending) return;
            draftAnimalDeathLimit = ConstructionRunModel.RecommendedAnimalDeathLimit;
            draftUnemployedResidentLimit =
                ConstructionRunModel.RecommendedUnemployedResidentLimit;
            RefreshEndlessRulesUi();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        private void RefreshEndlessRulesUi()
        {
            if (endlessRulesTitle == null || run == null) return;
            var required = !run.EndlessLossRulesConfirmed;
            endlessRulesTitle.text = endlessRulesNewRunPending
                ? L("NEW ENDLESS RUN", "新无尽模式")
                : L("ENDLESS ENDING RULES", "无尽模式结束条件");
            endlessRulesIntro.text = endlessRulesNewRunPending
                ? L("Confirm these limits to replace the current run.",
                    "确认结束条件后，才会覆盖当前存档。")
                : required
                    ? L("Choose your limits before the first day ends.",
                        "先设定结束条件，再开始推进天数。")
                    : L("These limits are fixed for this run. Start a new run to choose again.",
                        "本局条件已固定；重新开始可再次设定。");
            endlessRulesIntro.color = Ink;
            endlessRulesDeathLabel.text = L("Animal deaths in 7 days",
                "7 天内动物死亡");
            endlessRulesDeathRecommendation.text = L(
                $"Recommended: {ConstructionRunModel.RecommendedAnimalDeathLimit}",
                $"推荐：{ConstructionRunModel.RecommendedAnimalDeathLimit} 只");
            endlessRulesDeathValue.text = draftAnimalDeathLimit.ToString();
            endlessRulesUnemployedLabel.text = L("Residents who did not work",
                "未上班居民");
            endlessRulesUnemployedRecommendation.text = L(
                $"Recommended: {ConstructionRunModel.RecommendedUnemployedResidentLimit} " +
                $"for {ConstructionRunModel.UnemployedConsecutiveDays} days",
                $"推荐：{ConstructionRunModel.RecommendedUnemployedResidentLimit} 人，" +
                $"连续 {ConstructionRunModel.UnemployedConsecutiveDays} 天");
            endlessRulesUnemployedValue.text = draftUnemployedResidentLimit.ToString();
            endlessRulesSummary.text = L(
                $"{draftAnimalDeathLimit} deaths / last {ConstructionRunModel.AnimalDeathWindowDays} days" +
                $"  ·  {draftUnemployedResidentLimit}+ without work / " +
                $"{ConstructionRunModel.UnemployedConsecutiveDays} days\n" +
                "Fixed: no squirrels or arrived pigeons for 5 days; all residents leave.\n" +
                "Residents leave after 3 missed work-and-meal days once services exist.\n" +
                "Limits lock on Confirm.",
                $"最近 {ConstructionRunModel.AnimalDeathWindowDays} 天死亡达 " +
                $"{draftAnimalDeathLimit} 只 · 至少 {draftUnemployedResidentLimit} 人" +
                $"连续 {ConstructionRunModel.UnemployedConsecutiveDays} 天未上班\n" +
                "固定：松鼠或已到来的鸽群消失 5 天；居民全部离开。\n" +
                "有岗位和餐食后，居民连续 3 天未完成上班用餐循环会离开。\n" +
                "确认后本局条件锁定。");
            var editable = endlessRulesNewRunPending || required;
            foreach (var button in endlessRulesAdjustmentButtons)
                button.gameObject.SetActive(editable);
            endlessRulesRecommendedButton.gameObject.SetActive(editable);
            endlessRulesConfirmButton.gameObject.SetActive(editable);
            endlessRulesCancelButton.GetComponent<RectTransform>().anchoredPosition =
                editable ? new Vector2(-220, -255) : new Vector2(0, -255);
            endlessRulesCancelLabel.text = required
                ? L("Main menu", "主菜单") : L("Close", "关闭");
            endlessRulesRecommendedLabel.text = L("Recommended", "使用推荐值");
            endlessRulesConfirmLabel.text = endlessRulesNewRunPending
                ? L("Start new run", "开始新局")
                : L("Confirm", "确认");
        }

        private void CancelEndlessRules()
        {
            if (run == null || endlessRulesOverlay == null) return;
            if (!run.EndlessLossRulesConfirmed)
            {
                if (!ReturnToMainMenu())
                {
                    endlessRulesIntro.text = L(
                        "Could not open the menu. Try again.",
                        "无法返回主菜单，请重试。");
                    endlessRulesIntro.color = BlockedRed;
                }
                return;
            }
            endlessRulesNewRunPending = false;
            endlessRulesOverlay.SetActive(false);
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
            Refresh();
        }

        private void ConfirmEndlessRules()
        {
            if (!IsEndless || endlessRulesOverlay == null ||
                !endlessRulesOverlay.activeSelf) return;
            if (run.EndlessLossRulesConfirmed && !endlessRulesNewRunPending) return;
            if (endlessRulesNewRunPending)
            {
                var replacement = new ConstructionRunModel(
                    ConstructionGameMode.Endless);
                if (!replacement.ConfigureEndlessLossRules(draftAnimalDeathLimit,
                        draftUnemployedResidentLimit)) return;
                if (store != null &&
                    !store.TrySaveConfirmedNewRun(replacement, out _))
                {
                    ShowEndlessRulesSaveFailure();
                    return;
                }
                endlessRulesNewRunPending = false;
                endlessRulesOverlay.SetActive(false);
                StartPreparedRun(replacement);
                return;
            }
            var snapshot = run.Export();
            if (!run.ConfigureEndlessLossRules(draftAnimalDeathLimit,
                    draftUnemployedResidentLimit)) return;
            if (store != null && !store.TrySave(run))
            {
                if (ConstructionRunModel.TryRestore(snapshot, out var restored))
                    run = restored;
                ShowEndlessRulesSaveFailure();
                return;
            }
            runSavedAtLeastOnce = true;
            runNeedsSave = false;
            endlessRulesOverlay.SetActive(false);
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
        }

        private void ShowEndlessRulesSaveFailure()
        {
            endlessRulesIntro.text = L("Save failed; current run is unchanged.",
                "保存失败，当前存档未更改。");
            endlessRulesIntro.color = BlockedRed;
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
        }

        private void PrepareBoardCaseOpening(Transform canvas, bool animate)
        {
            IsBoardCaseOpen = !animate;
            if (!animate) return;

            var sharedOpening = worldView != null && worldView.UsesOpeningBoard;
            // A standalone construction scene unfolds the pages. The shared
            // menu board has already opened its physical lids; their screen
            // rectangles become the starting pose of the UI pages below.
            var boardHalfWidth = openingTourBoardTarget.rect.width *
                openingTourBoardTarget.localScale.x * 0.5f;
            var boardY = openingTourBoardTarget.anchoredPosition.y;
            leftCaseLeaf.pivot = new Vector2(1f, 0.5f);
            leftCaseLeaf.anchoredPosition = new Vector2(-boardHalfWidth, boardY);
            leftCaseLeaf.localScale = new Vector3(sharedOpening ? 1f : 0f, 1f, 1f);
            rightCaseLeaf.pivot = new Vector2(0f, 0.5f);
            rightCaseLeaf.anchoredPosition = new Vector2(boardHalfWidth, boardY);
            rightCaseLeaf.localScale = new Vector3(sharedOpening ? 1f : 0f, 1f, 1f);
            leftCaseLeafInk.alpha = rightCaseLeafInk.alpha = sharedOpening ? 0f : 1f;
            caseBoardOverlay.alpha = 0f;
            caseBoardOverlay.interactable = caseBoardOverlay.blocksRaycasts = false;

            caseInformation.alpha = caseRouteInformation.alpha = 0f;
            caseInformation.interactable = caseInformation.blocksRaycasts = false;
            caseRouteInformation.interactable =
                caseRouteInformation.blocksRaycasts = false;
            foreach (var group in caseHeaderGroups)
            {
                group.alpha = 0f;
                group.interactable = group.blocksRaycasts = false;
            }

            // The two paper frames meet the board rim directly. A narrow
            // bookbinding spine and three small straps mark the joint rather
            // than turning the former empty gap into a wide metal bridge.
            var boardHeight = CaseLeafHeight;
            foreach (var side in new[] { -1f, 1f })
            {
                var spine = Panel(side < 0f ? "West case binding" :
                        "East case binding", canvas,
                    new Vector2(side * boardHalfWidth, boardY),
                    new Vector2(18f, boardHeight + 6f), Ink);
                spine.raycastTarget = false;
                var bindingInk = spine.gameObject.AddComponent<CanvasGroup>();
                bindingInk.alpha = 0f;
                bindingInk.blocksRaycasts = false;
                caseHeaderGroups.Add(bindingInk);
                Panel("Brass spine hairline", spine.transform,
                    new Vector2(-side * 3f, 0f),
                    new Vector2(2f, boardHeight - 10f), Gold)
                    .raycastTarget = false;
                foreach (var pinY in new[]
                         { -boardHeight * 0.32f, 0f, boardHeight * 0.32f })
                {
                    var strap = Panel("Bookbinding copper clasp",
                        spine.transform, new Vector2(0f, pinY),
                        new Vector2(54f, 20f), Ink);
                    strap.raycastTarget = false;
                    Panel("Copper clasp edge", strap.transform,
                        new Vector2(0f, 6f), new Vector2(48f, 2f), Gold)
                        .raycastTarget = false;
                    Panel("Copper clasp centre", strap.transform,
                        Vector2.zero, new Vector2(44f, 4f), Gold)
                        .raycastTarget = false;
                    foreach (var rivetX in new[] { -17f, 17f })
                        Panel("Copper rivet", strap.transform,
                            new Vector2(rivetX, 0f), new Vector2(5f, 5f),
                            UrbanPalette.BoardFrameBrass)
                            .raycastTarget = false;
                }
            }

            // On the shared menu board the player is already looking inside
            // this very case. Only unfold its leaves; introducing opaque
            // covers would make the board appear to close before it opens.
            if (worldView == null || !worldView.UsesOpeningBoard)
            {
                leftCaseCover = CreateCaseCover(canvas, "Left board-case cover",
                    -boardHalfWidth, 1f, boardHalfWidth);
                rightCaseCover = CreateCaseCover(canvas, "Right board-case cover",
                    boardHalfWidth, 0f, boardHalfWidth);
            }
            var blocker = Panel("Board-case opening input guard", canvas,
                Vector2.zero, Vector2.zero, Color.clear);
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true;
            caseOpeningBlocker = blocker.gameObject;
        }

        private Image BuildCaseLeaf(Transform parent, string name,
            Vector2 position, string printPath)
        {
            var frame = Panel(name, parent, position,
                new Vector2(382f, CaseLeafHeight),
                UrbanPalette.Boundary);
            frame.raycastTarget = false;
            Panel("Copper leaf rule", frame.transform, Vector2.zero,
                new Vector2(367f, CaseLeafHeight - 15f),
                UrbanPalette.BoardFrameBrass)
                .raycastTarget = false;
            var paper = Panel("Printed leaf paper", frame.transform,
                Vector2.zero, new Vector2(359f, CaseLeafHeight - 23f), Paper);
            ApplySkin(paper, printPath);
            paper.raycastTarget = false;
            return frame;
        }

        private RectTransform CreateCaseCover(Transform canvas, string name,
            float hingeX, float pivotX, float coverWidth)
        {
            var cover = Panel(name, canvas,
                new Vector2(hingeX, openingTourBoardTarget.anchoredPosition.y),
                new Vector2(coverWidth, CaseLeafHeight), Ink);
            cover.rectTransform.pivot = new Vector2(pivotX, 0.5f);
            cover.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            cover.raycastTarget = false;
            Panel("Brass cover border", cover.transform, Vector2.zero,
                new Vector2(coverWidth - 14f, CaseLeafHeight - 16f), Gold)
                .raycastTarget = false;
            Panel("Navy cover centre", cover.transform, Vector2.zero,
                new Vector2(coverWidth - 24f, CaseLeafHeight - 26f), Ink)
                .raycastTarget = false;
            var print = Resources.Load<Texture2D>(
                "UI/ConstructionCase/lid-city-oak-v01");
            if (print != null)
            {
                var leftHalf = pivotX > 0.5f;
                var halfWidth = print.width * 0.5f;
                var art = Panel(leftHalf ? "City cover print" : "Oak cover print",
                    cover.transform, Vector2.zero,
                    new Vector2(coverWidth - 30f, CaseLeafHeight - 32f),
                    Color.white);
                art.sprite = Sprite.Create(print,
                    new Rect(leftHalf ? 0f : halfWidth, 0f,
                        halfWidth, print.height),
                    new Vector2(0.5f, 0.5f), 100f);
                // The cover root folds from a negative X scale. Counter-flip
                // the print so its city and oak motifs retain their direction.
                art.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                art.raycastTarget = false;
            }
            return cover.rectTransform;
        }

        private IEnumerator OpenBoardCase()
        {
            // Wait until the menu UI is gone, then unfold the two information
            // leaves while the same board stays visible and the camera settles.
            while (ConstructionSceneTransition.IsActive) yield return null;
            var elapsed = 0f;
            var sharedOpening = worldView != null && worldView.UsesOpeningBoard;
            if (sharedOpening)
            {
                // The camera has reached its final framing. Take over each
                // printed 3D lid in one frame, then ease the UI page into its
                // final position. Cross-fading two differently projected
                // prints exposed a second paper strip beside both seams.
                var leftTargetPosition = leftCaseLeaf.anchoredPosition;
                var rightTargetPosition = rightCaseLeaf.anchoredPosition;
                var leftStartPosition = leftTargetPosition;
                var rightStartPosition = rightTargetPosition;
                var leftStartScale = Vector3.one;
                var rightStartScale = Vector3.one;
                if (TryGetOpeningLeafPose(leftCaseLeaf, true,
                        out var westPosition, out var westScale))
                {
                    leftStartPosition = westPosition;
                    leftStartScale = westScale;
                }
                if (TryGetOpeningLeafPose(rightCaseLeaf, false,
                        out var eastPosition, out var eastScale))
                {
                    rightStartPosition = eastPosition;
                    rightStartScale = eastScale;
                }
                leftCaseLeaf.anchoredPosition = leftStartPosition;
                rightCaseLeaf.anchoredPosition = rightStartPosition;
                leftCaseLeaf.localScale = leftStartScale;
                rightCaseLeaf.localScale = rightStartScale;
                FindFirstObjectByType<ConstructionOpeningBoardPreview>()?
                    .SetInteriorPrintsVisible(false);
                leftCaseLeafInk.alpha = rightCaseLeafInk.alpha = 1f;
                const float blendDuration = 1.35f;
                while (elapsed < blendDuration)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                    var progress = Mathf.Clamp01(elapsed / blendDuration);
                    var move = Mathf.SmoothStep(0f, 1f, progress);
                    leftCaseLeaf.anchoredPosition = Vector2.Lerp(
                        leftStartPosition, leftTargetPosition, move);
                    rightCaseLeaf.anchoredPosition = Vector2.Lerp(
                        rightStartPosition, rightTargetPosition, move);
                    leftCaseLeaf.localScale = Vector3.Lerp(
                        leftStartScale, Vector3.one, move);
                    rightCaseLeaf.localScale = Vector3.Lerp(
                        rightStartScale, Vector3.one, move);
                    yield return null;
                }
                leftCaseLeaf.anchoredPosition = leftTargetPosition;
                rightCaseLeaf.anchoredPosition = rightTargetPosition;
                leftCaseLeaf.localScale = rightCaseLeaf.localScale = Vector3.one;
                // Text belongs to the settled desk layout, not to the moving
                // printed pages. Reveal it once both page frames are in place.
                const float inkDuration = 0.45f;
                elapsed = 0f;
                while (elapsed < inkDuration)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                    var inkReveal = Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / inkDuration));
                    caseBoardOverlay.alpha = inkReveal;
                    caseInformation.alpha = caseRouteInformation.alpha = inkReveal;
                    foreach (var group in caseHeaderGroups)
                        group.alpha = inkReveal;
                    yield return null;
                }
            }
            else
            {
                if (leftCaseCover != null && rightCaseCover != null)
                {
                    yield return null;
                    const float coverDuration = 0.70f;
                    while (elapsed < coverDuration)
                    {
                        elapsed += Mathf.Min(Time.unscaledDeltaTime,
                            1f / 30f);
                        var fold = 1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.Clamp01(elapsed / coverDuration));
                        leftCaseCover.localScale = rightCaseCover.localScale =
                            new Vector3(-fold, 1f, 1f);
                        yield return null;
                    }
                    leftCaseCover.gameObject.SetActive(false);
                    rightCaseCover.gameObject.SetActive(false);
                }

                const float leafDuration = 1.00f;
                elapsed = 0f;
                while (elapsed < leafDuration)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                    var open = Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / leafDuration));
                    leftCaseLeaf.localScale = rightCaseLeaf.localScale =
                        new Vector3(open, 1f, 1f);
                    var inkReveal = Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01((open - 0.55f) / 0.45f));
                    caseBoardOverlay.alpha = inkReveal;
                    caseInformation.alpha = caseRouteInformation.alpha = inkReveal;
                    foreach (var group in caseHeaderGroups) group.alpha = inkReveal;
                    yield return null;
                }
            }
            leftCaseLeaf.localScale = rightCaseLeaf.localScale = Vector3.one;
            leftCaseLeafInk.alpha = rightCaseLeafInk.alpha = 1f;
            caseBoardOverlay.alpha = 1f;
            caseBoardOverlay.interactable = caseBoardOverlay.blocksRaycasts = true;
            caseInformation.alpha = caseRouteInformation.alpha = 1f;
            caseInformation.interactable = caseInformation.blocksRaycasts = true;
            caseRouteInformation.interactable =
                caseRouteInformation.blocksRaycasts = true;
            foreach (var group in caseHeaderGroups)
            {
                group.alpha = 1f;
                group.interactable = group.blocksRaycasts = true;
            }
            caseOpeningBlocker.SetActive(false);
            IsBoardCaseOpen = true;
            RefreshOpeningTour();
            RefreshTutorialFocus();
            if (IsEndless && !run.IsFinished &&
                !run.EndlessLossRulesConfirmed)
                OpenEndlessRules(false);
            RefreshQuickGuide();
            TryShowOpeningQuickGuide();
            caseOpening = null;
        }

        private bool TryGetOpeningLeafPose(RectTransform leaf, bool west,
            out Vector2 anchoredPosition, out Vector3 scale)
        {
            anchoredPosition = leaf.anchoredPosition;
            scale = Vector3.one;
            var opening = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
            var camera = worldView == null ? null : worldView.BoardCamera;
            var canvasRect = leaf.parent as RectTransform;
            var lid = opening?.CaseShell?.Find(west
                ? "West lid hinge/Navy lid half"
                : "East lid hinge/Navy lid half");
            var renderer = lid == null ? null : lid.GetComponent<Renderer>();
            if (camera == null || canvasRect == null || renderer == null)
                return false;

            var bounds = renderer.bounds;
            var screenMin = new Vector2(float.PositiveInfinity,
                float.PositiveInfinity);
            var screenMax = new Vector2(float.NegativeInfinity,
                float.NegativeInfinity);
            for (var x = 0; x < 2; x++)
                for (var y = 0; y < 2; y++)
                    for (var z = 0; z < 2; z++)
                    {
                        var corner = new Vector3(
                            x == 0 ? bounds.min.x : bounds.max.x,
                            y == 0 ? bounds.min.y : bounds.max.y,
                            z == 0 ? bounds.min.z : bounds.max.z);
                        var screen = camera.WorldToScreenPoint(corner);
                        if (screen.z <= 0f) return false;
                        screenMin = Vector2.Min(screenMin, screen);
                        screenMax = Vector2.Max(screenMax, screen);
                    }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenMin, null, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenMax, null, out var localMax);
            if (localMax.x - localMin.x < 20f ||
                localMax.y - localMin.y < 20f)
                return false;
            anchoredPosition = new Vector2(west ? localMax.x : localMin.x,
                (localMin.y + localMax.y) * 0.5f);
            scale = new Vector3(
                (localMax.x - localMin.x) / leaf.rect.width,
                (localMax.y - localMin.y) / leaf.rect.height, 1f);
            return true;
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
                AttachFixedCellHover(button.gameObject, x, y);
                cellBackgrounds[column, row] = button.GetComponent<Image>();
                var buildable = TextAt("Buildable hint", button.transform,
                    Vector2.zero, new Vector2(90, 90), "+", 25, true,
                    new Color(0.20f, 0.52f, 0.47f), TextAnchor.MiddleCenter);
                buildable.raycastTarget = false;
                buildableHints[column, row] = buildable;
                buildable.gameObject.SetActive(false);
                var previewFrame = Panel("Placement preview", button.transform,
                    Vector2.zero, new Vector2(90f, 90f), Ink);
                previewFrame.raycastTarget = false;
                var previewGroup = previewFrame.gameObject
                    .AddComponent<CanvasGroup>();
                previewGroup.alpha = 0.86f;
                previewGroup.blocksRaycasts = false;
                var previewOutline = previewFrame.gameObject.AddComponent<Outline>();
                previewOutline.effectColor = Gold;
                previewOutline.effectDistance = new Vector2(1f, -1f);
                var previewMat = Panel("Preview illustration field",
                    previewFrame.transform, Vector2.zero,
                    new Vector2(84f, 84f), ReadingPaper);
                previewMat.raycastTarget = false;
                var previewIcon = Panel("Preview room icon", previewFrame.transform,
                    Vector2.zero, new Vector2(84f, 84f), Color.white);
                previewIcon.preserveAspect = true;
                previewIcon.raycastTarget = false;
                placementPreviewFrames[column, row] = previewFrame;
                placementPreviewMats[column, row] = previewMat;
                placementPreviewIcons[column, row] = previewIcon;
                previewFrame.gameObject.SetActive(false);
                if (run.UsesFillHand)
                {
                    var upgradeBorder = NewRect("Neighbour upgrade preview",
                        button.transform, Vector2.zero, new Vector2(94f, 94f));
                    var upgradeGold = new Color(1f, 0.89f, 0.53f);
                    foreach (var side in new[] { -1f, 1f })
                    {
                        Panel("Horizontal brass edge", upgradeBorder,
                            new Vector2(0f, side * 44f), new Vector2(90f, 2.4f),
                            upgradeGold).raycastTarget = false;
                        Panel("Vertical brass edge", upgradeBorder,
                            new Vector2(side * 44f, 0f), new Vector2(2.4f, 90f),
                            upgradeGold).raycastTarget = false;
                    }
                    upgradePreviewBorders[column, row] = upgradeBorder.gameObject;
                    Panel("Upgrade cue mount", upgradeBorder,
                        new Vector2(0f, 44f), new Vector2(30f, 24f), Ink).raycastTarget = false;
                    TextAt("Upgrade cue", upgradeBorder, new Vector2(0f, 44f),
                        new Vector2(30f, 26f), "↑", 18, true, upgradeGold,
                        TextAnchor.MiddleCenter).raycastTarget = false;
                    upgradeBorder.gameObject.SetActive(false);
                }
                var icon = Panel("Room icon", button.transform,
                    new Vector2(0, 4), new Vector2(47, 47), Color.white);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                cellIcons[column, row] = icon;
                cellIconRects[column, row] = icon.rectTransform;
                icon.gameObject.SetActive(false);
                var metricFrame = NewRect("Metric side rails", button.transform,
                    Vector2.zero, new Vector2(95, 95));
                var metricGroup = metricFrame.gameObject.AddComponent<CanvasGroup>();
                metricGroup.alpha = 0.30f;
                cellMetricGroups[column, row] = metricGroup;
                var leftRail = CreateMetricRail("Left metric rail", metricFrame,
                    -34f);
                var rightRail = CreateMetricRail("Right metric rail", metricFrame,
                    34f);
                leftMetricRails[column, row] = leftRail.gameObject;
                rightMetricRails[column, row] = rightRail.gameObject;
                // A quiet two-node mark sits on the frame, leaving the four
                // corner metrics and room artwork unobstructed. The existing
                // metric group fades it with the other badges.
                var linkBorder = Panel("Coexistence link", metricFrame,
                    new Vector2(0, 47.5f), new Vector2(24, 10), Gold);
                linkBorder.raycastTarget = true;
                Panel("Link inset", linkBorder.transform, Vector2.zero,
                    new Vector2(22, 8), Ink).raycastTarget = false;
                Panel("Link bridge", linkBorder.transform, Vector2.zero,
                    new Vector2(7, 1.5f), Paper).raycastTarget = false;
                Panel("Wildlife node", linkBorder.transform,
                    new Vector2(-6, 0), new Vector2(4, 4),
                    new Color(0.60f, 0.80f, 0.54f)).raycastTarget = false;
                Panel("Resident node", linkBorder.transform,
                    new Vector2(6, 0), new Vector2(4, 4),
                    ScorePlaqueNumber).raycastTarget = false;
                coexistenceBadges[column, row] = linkBorder.gameObject;
                AttachRoomBadgeHover(linkBorder.gameObject, x, y,
                    RoomBadgeKind.Coexistence);
                linkBorder.gameObject.SetActive(false);
                var squirrel = Panel("Resident squirrel", button.transform,
                    new Vector2(0, -20), new Vector2(22, 22), Color.white);
                squirrel.sprite = ConstructionIconCatalog.GetCharacter(
                    ConstructionCharacterIcon.Squirrel);
                squirrel.raycastTarget = false;
                squirrel.preserveAspect = true;
                squirrelIcons[column, row] = squirrel;
                squirrelCounts[column, row] = AnimalCountBadge(leftRail.transform,
                    squirrel.sprite);
                AttachRoomBadgeHover(squirrelCounts[column, row].transform.parent.gameObject, x, y,
                    RoomBadgeKind.Squirrel);
                squirrel.gameObject.SetActive(false);
                var pigeon = Panel("Pigeon flock", button.transform,
                    new Vector2(0, -20), new Vector2(22, 22), Color.white);
                pigeon.sprite = ConstructionIconCatalog.GetCharacter(
                    ConstructionCharacterIcon.Pigeon);
                pigeon.raycastTarget = false;
                pigeon.preserveAspect = true;
                pigeonIcons[column, row] = pigeon;
                pigeonCounts[column, row] = AnimalCountBadge(leftRail.transform,
                    pigeon.sprite);
                AttachRoomBadgeHover(pigeonCounts[column, row].transform.parent.gameObject, x, y,
                    RoomBadgeKind.Pigeon);
                pigeon.gameObject.SetActive(false);
                var hedgehog = Panel("Resident hedgehog", button.transform,
                    new Vector2(0, -20), new Vector2(22, 22), Color.white);
                hedgehog.sprite = ConstructionIconCatalog.GetCharacter(
                    ConstructionCharacterIcon.Hedgehog);
                hedgehog.raycastTarget = false;
                hedgehog.preserveAspect = true;
                hedgehogIcons[column, row] = hedgehog;
                hedgehogCounts[column, row] = AnimalCountBadge(leftRail.transform,
                    hedgehog.sprite);
                AttachRoomBadgeHover(hedgehogCounts[column, row].transform.parent.gameObject, x, y,
                    RoomBadgeKind.Hedgehog);
                hedgehog.gameObject.SetActive(false);
                var fox = Panel("Resident fox", button.transform,
                    new Vector2(0, -20), new Vector2(22, 22), Color.white);
                fox.sprite = ConstructionIconCatalog.GetCharacter(
                    ConstructionCharacterIcon.Fox);
                fox.raycastTarget = false;
                fox.preserveAspect = true;
                foxIcons[column, row] = fox;
                foxCounts[column, row] = AnimalCountBadge(leftRail.transform,
                    fox.sprite);
                AttachRoomBadgeHover(foxCounts[column, row].transform.parent.gameObject, x, y,
                    RoomBadgeKind.Fox);
                fox.gameObject.SetActive(false);
                var foodBadge = Panel("Food stock", rightRail.transform,
                    new Vector2(0, -20.5f), new Vector2(28, 39), Ink);
                foodBadge.raycastTarget = true;
                AddMetricPictogram(foodBadge.transform,
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.AnimalFood));
                AttachRoomBadgeHover(foodBadge.gameObject, x, y,
                    RoomBadgeKind.Food);
                var foodLabel = TextAt("Food count", foodBadge.transform,
                    new Vector2(0, -9), new Vector2(28, 19), "0", 10, true,
                    Color.white, TextAnchor.MiddleCenter);
                foodLabel.raycastTarget = false;
                foodBadges[column, row] = foodLabel;
                foodBadge.gameObject.SetActive(false);
                growthBadges[column, row] = CellStatusBadge("Plant growth",
                    leftRail.transform, new Vector2(0, 20.5f),
                    new Vector2(28, 39), "0/2", 10,
                    new Color(0.25f, 0.42f, 0.32f),
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Green,
                        GreenPlanting.Meadow));
                AttachRoomBadgeHover(
                    growthBadges[column, row].transform.parent.gameObject,
                    x, y, RoomBadgeKind.Growth);
                residenceBonusBadges[column, row] = CellStatusBadge(
                    "Home bonus", leftRail.transform, new Vector2(0, 20.5f),
                    new Vector2(28, 39), "+0", 10, IdleAmber,
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.CompletedCommute));
                AttachRoomBadgeHover(
                    residenceBonusBadges[column, row].transform.parent.gameObject,
                    x, y, RoomBadgeKind.HomeBonus);
                workCycleBadges[column, row] = CellStatusBadge("Work cycle",
                    leftRail.transform, new Vector2(0, 20.5f),
                    new Vector2(28, 39), "·", 15,
                    new Color(0.60f, 0.35f, 0.26f),
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.CompletedCommute));
                AttachRoomBadgeHover(
                    workCycleBadges[column, row].transform.parent.gameObject,
                    x, y, RoomBadgeKind.WorkCycle);
                streetDeathBadges[column, row] = CellStatusBadge(
                    "Traffic deaths", leftRail.transform, new Vector2(0, -20.5f),
                    new Vector2(28, 39), "0", 11,
                    new Color(0.36f, 0.41f, 0.42f),
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.RiskAlert));
                AttachRoomBadgeHover(
                    streetDeathBadges[column, row].transform.parent.gameObject,
                    x, y, RoomBadgeKind.TrafficDeaths);
                streetExposureBadges[column, row] = CellStatusBadge(
                    "Roadside wildlife", rightRail.transform, new Vector2(0, -20.5f),
                    new Vector2(28, 39), "0", 11,
                    new Color(0.53f, 0.39f, 0.25f),
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.AnimalFood));
                AttachRoomBadgeHover(
                    streetExposureBadges[column, row].transform.parent.gameObject,
                    x, y, RoomBadgeKind.RoadsideWildlife);
                growthBadges[column, row].transform.parent.gameObject.SetActive(false);
                residenceBonusBadges[column, row].transform.parent.gameObject.SetActive(false);
                workCycleBadges[column, row].transform.parent.gameObject.SetActive(false);
                streetDeathBadges[column, row].transform.parent.gameObject.SetActive(false);
                streetExposureBadges[column, row].transform.parent.gameObject.SetActive(false);
                // One upper-right status per room. The text remains a real
                // forecast ratio; colour and symbol explain its severity.
                var capacityBadge = Panel("Utilization status", rightRail.transform,
                    new Vector2(0, 20.5f), new Vector2(28, 39), Ink);
                capacityBadge.raycastTarget = true;
                AttachRoomBadgeHover(capacityBadge.gameObject, x, y,
                    RoomBadgeKind.Utilization);
                var utilizationSymbol = TextAt("Status symbol",
                    capacityBadge.transform, new Vector2(0, 8),
                    new Vector2(27, 17), "·", 15, true,
                    Color.white, TextAnchor.MiddleCenter);
                utilizationSymbol.raycastTarget = false;
                utilizationSymbols[column, row] = utilizationSymbol;
                var capacityLabel = TextAt("Use count", capacityBadge.transform,
                    new Vector2(0, -9), new Vector2(28, 18), "0/1", 10, true,
                    Color.white, TextAnchor.MiddleCenter);
                capacityLabel.raycastTarget = false;
                capacityBadges[column, row] = capacityLabel;
                capacityBadge.gameObject.SetActive(false);
                var wasteBadge = Panel("Waste backlog", leftRail.transform,
                    new Vector2(0, -20.5f), new Vector2(28, 39),
                    new Color(0.68f, 0.35f, 0.25f));
                wasteBadge.raycastTarget = true;
                AddMetricPictogram(wasteBadge.transform,
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Waste));
                AttachRoomBadgeHover(wasteBadge.gameObject, x, y,
                    RoomBadgeKind.Waste);
                var wasteLabel = TextAt("Waste count", wasteBadge.transform,
                    new Vector2(0, -9), new Vector2(28, 18), "0", 11, true,
                    Color.white, TextAnchor.MiddleCenter);
                wasteLabel.raycastTarget = false;
                wasteBadges[column, row] = wasteLabel;
                wasteBadge.gameObject.SetActive(false);
                residentMarkers[column, row] = CreateHumanMarker(button.transform,
                    new Vector2(0, -20), 0.7f, "First resident");
                residentMarkers[column, row].SetActive(false);
                var foxRisk = NewRect("Fox risk", button.transform,
                    Vector2.zero, new Vector2(88, 88));
                var warningRed = new Color(0.72f, 0.29f, 0.22f, 0.92f);
                foreach (var edge in new[]
                {
                    (name: "Top", position: new Vector2(0, 44),
                        size: new Vector2(88, 3)),
                    (name: "Bottom", position: new Vector2(0, -44),
                        size: new Vector2(88, 3)),
                    (name: "Left", position: new Vector2(-44, 0),
                        size: new Vector2(3, 88)),
                    (name: "Right", position: new Vector2(44, 0),
                        size: new Vector2(3, 88))
                })
                    Panel($"Fox risk {edge.name}", foxRisk,
                        edge.position, edge.size, warningRed).raycastTarget = false;
                var foxBadge = Panel("Fox risk badge", foxRisk,
                    new Vector2(0, 29), new Vector2(24, 24), warningRed);
                foxBadge.raycastTarget = true;
                var foxRiskIcon = Panel("Fox risk icon", foxBadge.transform,
                    Vector2.zero, new Vector2(20, 20), Color.white);
                foxRiskIcon.sprite = ConstructionIconCatalog.GetCharacter(
                    ConstructionCharacterIcon.Fox);
                foxRiskIcon.preserveAspect = true;
                foxRiskIcon.raycastTarget = false;
                foxBadge.gameObject.AddComponent<Button>().onClick.AddListener(
                    () => FocusPredationRisk());
                foxBadge.gameObject.AddComponent<ConstructionRoomBadgeHover>()
                    .Initialize(() =>
                    {
                        hoveredLocalRisk = true;
                        hoveredRiskAnchor = foxBadge.rectTransform;
                        RefreshFixedInfoCard();
                    }, () =>
                    {
                        hoveredLocalRisk = false;
                        hoveredRiskAnchor = null;
                        RefreshFixedInfoCard();
                    });
                foxRiskMarkers[column, row] = foxRisk.gameObject;
                foxRisk.gameObject.SetActive(false);
                var linkFocus = NewRect("Coexistence neighbours", button.transform,
                    Vector2.zero, new Vector2(93, 93));
                foreach (var edge in new[]
                {
                    (name: "Top", position: new Vector2(0, 45.5f),
                        size: new Vector2(93, 2)),
                    (name: "Bottom", position: new Vector2(0, -45.5f),
                        size: new Vector2(93, 2)),
                    (name: "Left", position: new Vector2(-45.5f, 0),
                        size: new Vector2(2, 93)),
                    (name: "Right", position: new Vector2(45.5f, 0),
                        size: new Vector2(2, 93))
                })
                    Panel($"Link focus {edge.name}", linkFocus,
                        edge.position, edge.size, Gold).raycastTarget = false;
                coexistenceHighlights[column, row] = linkFocus.gameObject;
                linkFocus.gameObject.SetActive(false);
            }
        }

        private Text CellStatusBadge(string name, Transform parent,
            Vector2 position, Vector2 size, string value, int fontSize,
            Color background, Sprite pictogram = null)
        {
            var panel = Panel(name, parent, position, size, background);
            panel.raycastTarget = true;
            if (pictogram != null) AddMetricPictogram(panel.transform, pictogram);
            var label = TextAt(name + " value", panel.transform,
                new Vector2(0, pictogram == null ? 0 : -9),
                new Vector2(size.x, pictogram == null ? size.y - 1f : 18f),
                value, fontSize, true,
                Color.white, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            return label;
        }

        private Image CreateMetricRail(string name, Transform parent, float x)
        {
            var rim = Panel(name, parent, new Vector2(x, 0),
                new Vector2(26, 84), Gold);
            rim.raycastTarget = false;
            Panel("Rail face", rim.transform, Vector2.zero,
                new Vector2(23, 81), Ink).raycastTarget = false;
            Panel("Rail divider", rim.transform, Vector2.zero,
                new Vector2(23, 1), Gold).raycastTarget = false;
            return rim;
        }

        private void AddMetricPictogram(Transform parent, Sprite sprite)
        {
            var image = Panel("Metric pictogram", parent,
                new Vector2(0, 8), new Vector2(15, 15), Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Vector2 BadgePosition(bool right, bool top, Vector2 size)
        {
            const float edge = 41f;
            return new Vector2((right ? 1f : -1f) * (edge - size.x * 0.5f),
                (top ? 1f : -1f) * (edge - size.y * 0.5f));
        }

        private void AttachFixedCellHover(GameObject cell, int viewColumn,
            int viewRow)
        {
            var trigger = cell.GetComponent<EventTrigger>() ??
                cell.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                cellMetricHovering[viewColumn, viewRow] = true;
                var tile = run?.At(viewMinColumn + viewColumn,
                    viewMinRow + viewRow);
                hoveredInfoTileId = tile?.id;
                hoveredEmptyColumn = tile == null
                    ? viewMinColumn + viewColumn : null;
                hoveredEmptyRow = tile == null
                    ? viewMinRow + viewRow : null;
                RefreshFixedInfoCard();
            });
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                cellMetricHovering[viewColumn, viewRow] = false;
                hoveredInfoTileId = null;
                selectedInfoTileId = null;
                hoveredEmptyColumn = null;
                hoveredEmptyRow = null;
                hoveredBadgeTileId = null;
                hoveredBadgeKind = null;
                hoveredBadgeAnchor = null;
                hoveredRiskAnchor = null;
                hoveredLocalRisk = false;
                RefreshCoexistenceHighlights();
                RefreshFixedInfoCard();
            });
            trigger.triggers.Add(exit);
        }

        private void AttachRoomBadgeHover(GameObject badge, int viewColumn,
            int viewRow, RoomBadgeKind kind)
        {
            badge.AddComponent<ConstructionRoomBadgeHover>().Initialize(
                () =>
                {
                    var tile = run?.At(viewMinColumn + viewColumn,
                        viewMinRow + viewRow);
                    hoveredBadgeTileId = tile?.id;
                    hoveredBadgeKind = tile == null ? null : kind;
                    hoveredBadgeAnchor = badge.GetComponent<RectTransform>();
                    RefreshCoexistenceHighlights();
                    RefreshFixedInfoCard();
                },
                () =>
                {
                    hoveredBadgeTileId = null;
                    hoveredBadgeKind = null;
                    hoveredBadgeAnchor = null;
                    RefreshCoexistenceHighlights();
                    RefreshFixedInfoCard();
                });
        }

        private void RefreshCoexistenceHighlights()
        {
            var linked = new HashSet<string>();
            if (run?.ScoringVersion >= 8 &&
                hoveredBadgeKind == RoomBadgeKind.Coexistence &&
                !string.IsNullOrEmpty(hoveredBadgeTileId))
            {
                var tile = run.BuiltTiles.FirstOrDefault(item =>
                    item.id == hoveredBadgeTileId);
                var human = run.IsFinished && run.LastHumanDay != null
                    ? run.LastHumanDay : run.PreviewHumanDay();
                if (tile != null)
                {
                    linked.Add(tile.id);
                    if (tile.category == ConstructionCategory.Residence)
                    {
                        var person = human.residents.FirstOrDefault(item =>
                            item.homeTileId == tile.id);
                        if (!string.IsNullOrEmpty(person?.linkageSourceTileId))
                            linked.Add(person.linkageSourceTileId);
                        if (person?.biodiversityHabitatTileIds != null)
                            foreach (var id in person.biodiversityHabitatTileIds)
                                linked.Add(id);
                    }
                    else if (tile.category == ConstructionCategory.Street)
                    {
                        foreach (var square in run.BuiltTiles.Where(item =>
                                     item.category == ConstructionCategory.Square &&
                                      ConstructionBoardModel.AreNeighbours(
                                         item.column, item.row,
                                         tile.column, tile.row) &&
                                     human.residents.Any(person =>
                                         person.visitedSquare &&
                                         person.squareTileId == item.id)))
                            linked.Add(square.id);
                    }
                    else
                    {
                        foreach (var person in human.residents.Where(item =>
                                     item.linkageSourceTileId == tile.id))
                            linked.Add(person.homeTileId);
                    }
                }
            }
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var frame = coexistenceHighlights[column, row];
                if (frame == null) continue;
                var visible = run?.At(viewMinColumn + column,
                    viewMinRow + row);
                frame.SetActive(visible != null && linked.Contains(visible.id));
            }
        }

        private void ClearFixedInfoSelection()
        {
            selectedInfoTileId = null;
            hoveredInfoTileId = null;
            hoveredEmptyColumn = null;
            hoveredEmptyRow = null;
            hoveredBadgeTileId = null;
            hoveredBadgeKind = null;
            hoveredBadgeAnchor = null;
            hoveredRiskAnchor = null;
            hoveredLocalRisk = false;
            fixedRiskHovered = false;
            RefreshCoexistenceHighlights();
            HideRoomBadgeTooltip();
        }

        private void BuildPlacementConfirmation(Transform parent)
        {
            var card = Panel("Placement confirmation", parent,
                new Vector2(CaseLeafCenter, -40),
                new Vector2(344, 288), Color.white);
            ApplySkin(card, "UI/PauseMenu/pause-menu-settings-panel-v01");
            placementConfirmation = card.gameObject;
            placementConfirmationIcon = Panel("Confirmation room icon", card.transform,
                new Vector2(-99, 42), new Vector2(56, 56), Color.white);
            placementConfirmationIcon.preserveAspect = true;
            placementConfirmationIcon.raycastTarget = false;
            placementConfirmationLocation = TextAt("Confirmation location",
                card.transform, new Vector2(36, 42), new Vector2(198, 64),
                "", 17, true, Ink, TextAnchor.MiddleLeft);
            placementConfirmationLocation.raycastTarget = false;
            var divider = Panel("Confirmation divider", card.transform,
                new Vector2(0, 4), new Vector2(280, 2), Gold);
            divider.raycastTarget = false;
            placementConfirmationNotice = TextAt("Confirmation notice",
                card.transform, new Vector2(0, -34), new Vector2(280, 64),
                "", 16, false, Ink, TextAnchor.UpperLeft);
            placementConfirmationNotice.raycastTarget = false;
            var cancel = ButtonAt("Cancel placement", card.transform,
                new Vector2(-75, -103), new Vector2(132, 43), "", 16,
                Ink, Paper, CancelPendingPlacement);
            placementCancelLabel = cancel.GetComponentInChildren<Text>();
            if (placementCancelLabel == null)
                placementCancelLabel = TextAt("Label", cancel.transform,
                    Vector2.zero, new Vector2(132, 43), "", 16, true,
                    Paper, TextAnchor.MiddleCenter);
            placementCancelLabel.raycastTarget = false;
            var confirm = ButtonAt("Confirm placement", card.transform,
                new Vector2(75, -103), new Vector2(132, 43), "", 16,
                new Color(0.17f, 0.40f, 0.36f), Paper,
                () => ConfirmPendingPlacement());
            placementConfirmButton = confirm;
            placementConfirmLabel = confirm.GetComponentInChildren<Text>();
            if (placementConfirmLabel == null)
                placementConfirmLabel = TextAt("Label", confirm.transform,
                    Vector2.zero, new Vector2(132, 43), "", 16, true,
                    Paper, TextAnchor.MiddleCenter);
            placementConfirmLabel.raycastTarget = false;
            AddButtonIcon(confirm, ConstructionControlIcon.Confirm);
            placementConfirmation.SetActive(false);
        }

        private void BuildResidentDispatch(Transform parent)
        {
            residentDispatchPanel = Panel("Resident dispatch", parent,
                new Vector2(CaseLeafCenter, 90), new Vector2(350, 600), Ink);
            residentDispatchPanel.raycastTarget = true;
            var face = Panel("Dispatch paper", residentDispatchPanel.transform,
                Vector2.zero, new Vector2(336, 586), Paper);
            face.raycastTarget = true;
            Panel("Dispatch header", face.transform, new Vector2(0, 251),
                new Vector2(330, 48), Ink).raycastTarget = false;
            dispatchHeading = TextAt("Dispatch heading", face.transform,
                new Vector2(0, 251), new Vector2(288, 32), "", 21, true,
                Paper, TextAnchor.MiddleCenter);
            var homeIcon = Panel("Dispatch residence icon", face.transform,
                new Vector2(-131, 201), new Vector2(22, 22), Color.clear);
            homeIcon.sprite = ConstructionIconCatalog.GetBuild(
                ConstructionCategory.Residence);
            homeIcon.color = homeIcon.sprite == null ? Color.clear : Color.white;
            homeIcon.preserveAspect = true;
            homeIcon.raycastTarget = false;
            dispatchHome = TextAt("Dispatch home", face.transform,
                new Vector2(17, 201), new Vector2(254, 26), "", 16, true,
                Ink, TextAnchor.MiddleLeft);
            dispatchRules = TextAt("Dispatch rules", face.transform,
                new Vector2(0, 174), new Vector2(288, 22), "", 14, false,
                Ink, TextAnchor.MiddleLeft);
            var workIcon = Panel("Dispatch work icon", face.transform,
                new Vector2(-131, 139), new Vector2(21, 21), Color.clear);
            workIcon.sprite = LoadSprite(
                "UI/ConstructionCase/ResidentDispatch/workplace");
            workIcon.color = workIcon.sprite == null ? Color.clear : Color.white;
            workIcon.preserveAspect = true;
            workIcon.raycastTarget = false;
            dispatchWorkHeading = TextAt("Work destination label", face.transform,
                new Vector2(17, 139), new Vector2(254, 28),
                L("WORK", "上班地点"), 16, true, HeadingInk,
                TextAnchor.MiddleLeft);
            dispatchWorkHeading.verticalOverflow = VerticalWrapMode.Overflow;
            Panel("Work destination border", face.transform,
                new Vector2(0, 101), new Vector2(288, 44), Gold)
                .raycastTarget = false;
            Panel("Work destination backing", face.transform,
                new Vector2(0, 101), new Vector2(284, 40), Paper)
                .raycastTarget = false;
            dispatchWorkPrevious = ButtonAt("Previous work destination", face.transform,
                new Vector2(-123, 101), new Vector2(36, 38), "‹", 25,
                Ink, Paper, () => CycleResidentDispatch(true, -1), false);
            dispatchWorkChoice = TextAt("Work destination", face.transform,
                new Vector2(0, 101), new Vector2(198, 40), "", 15, true,
                Ink, TextAnchor.MiddleCenter);
            dispatchWorkNext = ButtonAt("Next work destination", face.transform,
                new Vector2(123, 101), new Vector2(36, 38), "›", 25,
                Ink, Paper, () => CycleResidentDispatch(true, 1), false);
            var mealIcon = Panel("Dispatch meal icon", face.transform,
                new Vector2(-131, 62), new Vector2(21, 21), Color.clear);
            mealIcon.sprite = LoadSprite(
                "UI/ConstructionCase/ResidentDispatch/meal_place");
            mealIcon.color = mealIcon.sprite == null ? Color.clear : Color.white;
            mealIcon.preserveAspect = true;
            mealIcon.raycastTarget = false;
            dispatchMealHeading = TextAt("Meal destination label", face.transform,
                new Vector2(17, 62), new Vector2(254, 28),
                L("MEAL", "用餐地点"), 16, true, HeadingInk,
                TextAnchor.MiddleLeft);
            dispatchMealHeading.verticalOverflow = VerticalWrapMode.Overflow;
            Panel("Meal destination border", face.transform,
                new Vector2(0, 24), new Vector2(288, 44), Gold)
                .raycastTarget = false;
            Panel("Meal destination backing", face.transform,
                new Vector2(0, 24), new Vector2(284, 40), Paper)
                .raycastTarget = false;
            dispatchMealPrevious = ButtonAt("Previous meal destination", face.transform,
                new Vector2(-123, 24), new Vector2(36, 38), "‹", 25,
                Ink, Paper, () => CycleResidentDispatch(false, -1), false);
            dispatchMealChoice = TextAt("Meal destination", face.transform,
                new Vector2(0, 24), new Vector2(198, 40), "", 15, true,
                Ink, TextAnchor.MiddleCenter);
            dispatchMealNext = ButtonAt("Next meal destination", face.transform,
                new Vector2(123, 24), new Vector2(36, 38), "›", 25,
                Ink, Paper, () => CycleResidentDispatch(false, 1), false);
            Panel("Dispatch divider", face.transform, new Vector2(0, -11),
                new Vector2(288, 2), Gold).raycastTarget = false;
            dispatchPreviewHeading = TextAt("Dispatch preview heading", face.transform,
                new Vector2(0, -32), new Vector2(288, 28), "", 16, true,
                Ink, TextAnchor.MiddleLeft);
            dispatchPreviewHeading.verticalOverflow = VerticalWrapMode.Overflow;
            dispatchPreview = TextAt("Dispatch preview", face.transform,
                new Vector2(0, -69), new Vector2(288, 40), "", 14, false,
                Ink, TextAnchor.MiddleLeft);
            dispatchPreview.lineSpacing = 1.1f;
            dispatchBreakdown = TextAt("Dispatch home score breakdown", face.transform,
                new Vector2(0, -118), new Vector2(288, 54), "", 14, false,
                Ink, TextAnchor.MiddleLeft);
            dispatchBreakdown.lineSpacing = 1.0f;
            AttachHover(dispatchBreakdown.gameObject, () => null,
                () => L("This home's conditions", "本户路线条件"),
                () => dispatchBreakdownDetails, TooltipLocation.LeftLeaf);
            dispatchScoreHeading = TextAt("Dispatch score heading", face.transform,
                new Vector2(0, -158), new Vector2(288, 20), "", 14, true,
                Ink, TextAnchor.MiddleLeft);
            Panel("Dispatch score backing", face.transform,
                new Vector2(0, -187), new Vector2(288, 30), Ink)
                .raycastTarget = false;
            dispatchAnimalScore = TextAt("Dispatch animal score", face.transform,
                new Vector2(-96, -187), new Vector2(92, 27), "", 13, true,
                Paper, TextAnchor.MiddleCenter);
            dispatchResidentScore = TextAt("Dispatch resident score", face.transform,
                new Vector2(0, -187), new Vector2(92, 27), "", 13, true,
                Paper, TextAnchor.MiddleCenter);
            dispatchSharedScore = TextAt("Dispatch shared score", face.transform,
                new Vector2(96, -187), new Vector2(92, 27), "", 13, true,
                Paper, TextAnchor.MiddleCenter);
            dispatchNote = TextAt("Dispatch note", face.transform,
                new Vector2(0, -218), new Vector2(288, 22), "", 14, true,
                IdleAmber, TextAnchor.MiddleLeft);
            var auto = ButtonAt("Reset resident dispatch to auto", face.transform,
                new Vector2(-74, -255), new Vector2(140, 42), "Auto", 15,
                Ink, Paper, () => ClearResidentDispatch());
            dispatchAutoLabel = auto.GetComponentInChildren<Text>();
            var close = ButtonAt("Close resident dispatch", face.transform,
                new Vector2(74, -255), new Vector2(140, 42), "Close", 15,
                Ink, Paper, CloseResidentDispatch);
            dispatchCloseLabel = close.GetComponentInChildren<Text>();
            residentDispatchPanel.gameObject.SetActive(false);
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

        private void BuildOpeningTour(Transform canvas)
        {
            openingTourRoot = NewRect("Opening UI tour", canvas,
                Vector2.zero, Vector2.zero);
            Stretch(openingTourRoot);
            openingTourFrame = NewRect("Focus frame", openingTourRoot,
                Vector2.zero, Vector2.zero);
            var yellow = new Color(0.98f, 0.78f, 0.37f);
            foreach (var edgeName in new[] { "Top", "Bottom", "Left", "Right" })
            {
                var edge = Panel(edgeName, openingTourFrame,
                    Vector2.zero, Vector2.zero, yellow);
                edge.raycastTarget = false;
                var rect = edge.rectTransform;
                if (edgeName == "Top" || edgeName == "Bottom")
                {
                    var y = edgeName == "Top" ? 1f : 0f;
                    rect.anchorMin = new Vector2(0f, y);
                    rect.anchorMax = new Vector2(1f, y);
                    rect.sizeDelta = new Vector2(0f, 5f);
                    rect.anchoredPosition = new Vector2(0f, y == 1f ? -2.5f : 2.5f);
                }
                else
                {
                    var x = edgeName == "Right" ? 1f : 0f;
                    rect.anchorMin = new Vector2(x, 0f);
                    rect.anchorMax = new Vector2(x, 1f);
                    rect.sizeDelta = new Vector2(5f, 0f);
                    rect.anchoredPosition = new Vector2(x == 1f ? -2.5f : 2.5f, 0f);
                }
                var contrast = edge.gameObject.AddComponent<Outline>();
                contrast.effectColor = Ink;
                contrast.effectDistance = new Vector2(1f, -1f);
            }

            // Repeat the step number on the focus frame so it stays identifiable
            // as the explanation card follows each target.
            var focusIndexFace = Panel("Focus step badge", openingTourFrame,
                Vector2.zero, new Vector2(27f, 23f), Ink);
            focusIndexFace.raycastTarget = false;
            focusIndexFace.rectTransform.anchorMin = new Vector2(0f, 1f);
            focusIndexFace.rectTransform.anchorMax = new Vector2(0f, 1f);
            focusIndexFace.rectTransform.anchoredPosition = new Vector2(13f, 9f);
            openingTourFocusIndex = TextAt("Focus step number",
                focusIndexFace.transform, Vector2.zero, new Vector2(24f, 20f),
                "1", 13, true, yellow, TextAnchor.MiddleCenter);
            openingTourFocusIndex.raycastTarget = false;

            // Dock the tour beside the relevant paper leaf, in a lower board
            // row that does not cover the starter oak or the case hinges.
            var card = Panel("Tour card", openingTourRoot,
                new Vector2(-180, -135), new Vector2(360, 170), yellow);
            openingTourCard = card.rectTransform;
            card.raycastTarget = false;
            Panel("Tour face", card.transform, Vector2.zero,
                new Vector2(354, 164), Ink).raycastTarget = false;
            openingTourTitle = TextAt("Tour title", card.transform,
                new Vector2(-47, 53), new Vector2(220, 27), "", 18,
                true, Paper, TextAnchor.MiddleLeft);
            openingTourTitle.raycastTarget = false;
            openingTourCount = TextAt("Tour count", card.transform,
                new Vector2(143, 53), new Vector2(45, 27), "", 14,
                true, yellow, TextAnchor.MiddleRight);
            openingTourCount.raycastTarget = false;
            openingTourBody = TextAt("Tour body", card.transform,
                new Vector2(0, 1), new Vector2(312, 66), "", 14,
                false, Paper, TextAnchor.MiddleLeft);
            openingTourBody.raycastTarget = false;
            Panel("Skip tour border", card.transform,
                new Vector2(-82, -57), new Vector2(138, 35), Gold)
                .raycastTarget = false;
            var skip = ButtonAt("Skip tour", card.transform,
                new Vector2(-82, -57), new Vector2(134, 31), "Skip tour", 13,
                new Color(0.24f, 0.31f, 0.35f), Paper,
                DismissOpeningTour, themed: false);
            openingTourSkipLabel = skip.GetComponentInChildren<Text>();
            var next = ButtonAt("Next tour step", card.transform,
                new Vector2(82, -57), new Vector2(134, 31), "Next", 14,
                yellow, Ink, AdvanceOpeningTour, themed: false);
            openingTourNextLabel = next.GetComponentInChildren<Text>();
            openingTourRoot.gameObject.SetActive(false);
        }

        private bool ShouldShowOpeningTour() =>
            openingTourRoot != null && run != null &&
            run.Mode == ConstructionGameMode.Fill && run.CurrentDay == 1 &&
            !run.IsFinished && !run.OpeningTourCompleted &&
            !run.TutorialTipsSkipped && IsBoardCaseOpen &&
            !QuickGuideOpen &&
            (specialTaskSelectionRoot == null ||
                !specialTaskSelectionRoot.gameObject.activeSelf);

        private RectTransform OpeningTourTarget()
        {
            if (run?.UsesFillHand == true)
                return openingTourStep switch
                {
                    0 => openingTourTodayTarget,
                    1 => openingTourScoreTarget,
                    2 => buildableHints[3, 2].transform.parent as RectTransform,
                    3 => buildableHints[3, 3].transform.parent as RectTransform,
                    4 => dayText.rectTransform,
                    5 => openingTourGuideTarget,
                    _ => fillHandCards[0]?.transform as RectTransform
                };
            return openingTourStep switch
            {
                0 => openingTourTodayTarget,
                1 => openingTourScoreTarget,
                2 => openingTourReviewTarget,
                3 => VisibleTile(ConstructionBoardModel.StarterOakId) ??
                    openingTourBoardTarget,
                4 => openingTourProgressTarget,
                5 => openingTourGuideTarget,
                _ => nextDayButton.transform as RectTransform
            };
        }

        private void RefreshOpeningTour()
        {
            if (openingTourRoot == null) return;
            var show = ShouldShowOpeningTour();
            openingTourRoot.gameObject.SetActive(show);
            if (!show) return;
            var target = OpeningTourTarget();
            if (target == null)
            {
                openingTourRoot.gameObject.SetActive(false);
                return;
            }
            // Both the focus and its explanation must follow the current
            // target. Calculate them in the overlay's own coordinate space so
            // canvas scaling and the opening case animation cannot offset one
            // from the other.
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                openingTourRoot, target);
            openingTourFrame.anchoredPosition = new Vector2(bounds.center.x,
                bounds.center.y);
            openingTourFrame.sizeDelta = new Vector2(bounds.size.x + 10f,
                bounds.size.y + 10f);
            PositionOpeningTourCard(bounds);
            if (run.UsesFillHand)
            {
                openingTourTitle.text = openingTourStep switch
                {
                    0 => L("ONE CITY · TWO LIVES", "一座城，两种生活"),
                    1 => L("RUN SCORE", "本局得分"),
                    2 => L("SHARED EDGES", "共边才能接通"),
                    3 => L("START WITH THE OAK", "先放中央橡树"),
                    4 => L("49 PLOTS", "建满 49 格"),
                    5 => L("PREVIEW AND BUILD", "预览与建造"),
                    _ => L("YOUR HAND", "下方手牌")
                };
                openingTourBody.text = openingTourStep switch
                {
                    0 => L("Wildlife lived here first. Begin with its habitat, then make room for people without losing the animals' home.",
                        "动物先在这里生活。先留下它们的栖息地，再迎接居民搬来，让双方都能安稳生活。"),
                    1 => L("Total score combines shared, build and goal points. Shared points come from animals and residents.",
                        "总分由共栖、建造和目标牌分组成。共栖分来自动物和居民。"),
                    2 => L("Build and travel across shared sides. Empty plots and diagonal contact break routes.",
                        "建造和通行沿上下左右共边接通。空格和斜角都会断开路线。"),
                    3 => L("The first card is the central oak. Keep a home and food source for the squirrel before people arrive.",
                        "第一张先在正中放橡树，为松鼠留下家和食物，再迎接居民。"),
                    4 => L("This is the placement count. Fill all 49 plots to finish the run.",
                        "这里记录已放置格数。建满 49 格后结算本局。"),
                    5 when run.UsesNeighborUpgrades =>
                        L("Click a lit plot to preview. Gold outlines mark upgrades. Confirm to settle.",
                            "点击亮格预览。金边标出会升级的地块。确认后结算本回合。"),
                    5 => L("Click a lit plot to preview, then confirm to build and settle the turn.",
                        "点击亮格预览，确认后建造并结算本回合。"),
                    _ => L("Follow eight room lessons, or skip them to build freely. Press O for the handbook.",
                        "可按顺序认识八种地块，也可跳过教程直接自由建造。按 O 打开手册。")
                };
                openingTourCount.text = $"{openingTourStep + 1}/{OpeningTourSteps}";
                openingTourFocusIndex.text = (openingTourStep + 1).ToString();
                openingTourSkipLabel.text = L("Skip tour", "跳过引导");
                openingTourNextLabel.text = openingTourStep == OpeningTourSteps - 1
                    ? L("Got it", "知道了") : L("Next", "下一项");
                openingTourRoot.SetAsLastSibling();
                return;
            }
            openingTourTitle.text = openingTourStep switch
            {
                0 => L("ONE CITY · TWO LIVES", "一座城，两种生活"),
                1 => L("GUIDE · TWO SCORES", "新手引导 · 动物与居民"),
                2 => L("GUIDE · DAILY REPORT", "新手引导 · 结算与路线"),
                3 => L("GUIDE · THE BOARD", "新手引导 · 中央棋盘"),
                4 => L("GUIDE · PROGRESS", "新手引导 · 棋盘进度"),
                5 => L("GUIDE · NEXT STEP", "新手引导 · 下一步"),
                _ => L("GUIDE · START", "新手引导 · 开始观察")
            };
            openingTourBody.text = openingTourStep switch
            {
                0 => L("Wildlife lived here before people arrived. Keep its food and routes as you welcome residents who need homes, work and meals. Each tile can affect its eight neighbours.",
                    "动物先在这里生活。迎接需要住宅、工作和餐食的居民时，也要留住动物的食物与路线；每一格都可能影响周围八格。"),
                1 when run.ScoringVersion >= 8 => L(
                    "Animal + resident points = shared score. A visited square feeds wildlife and makes nearby roads safer; fed wildlife near homes lifts resident satisfaction.",
                    "动物分＋居民分＝共栖分。居民到访广场能给动物食物、保护附近道路；住宅旁觅食成功的动物也能提高居民满意度。"),
                1 when run.HasLinkageScore => L(
                    "Animal + resident points = shared score. Food sources near wildlife and residents can benefit both.",
                    "动物分＋居民分＝共栖分。动物与居民附近的可用食源能让双方受益。"),
                1 when run.HasAdditiveCoexistenceScore => L(
                    "Animal + resident points = shared score.",
                    "动物分＋居民分＝共栖分。"),
                1 => L("Hover each score to see this save's rules.",
                    "悬停各项分数，查看此存档的计分规则。"),
                2 => L("Rules today; results and routes from tomorrow.",
                    "首日看规则；之后看结算与路线。"),
                3 => L("Badges: green used, amber spare, red issue. Hover; click oak for food.",
                    "角标绿=用足、橙=余量、红=问题；悬停看原因。可点橡树查食物。"),
                4 => L("Built plots and shared score appear here.",
                    "这里看已建格数和共栖分。"),
                5 => L("This panel explains the current goal and build rules.",
                    "这里说明当前目标与建造条件。"),
                _ => L("You can inspect the oak. End the day; building starts tomorrow.",
                    "可点橡树查看食物。点“下一天”；明天开始建造。")
            };
            openingTourCount.text = $"{openingTourStep + 1}/{OpeningTourSteps}";
            openingTourFocusIndex.text = (openingTourStep + 1).ToString();
            openingTourSkipLabel.text = L("Skip tour", "跳过引导");
            openingTourNextLabel.text = openingTourStep == OpeningTourSteps - 1
                ? L("Got it", "知道了") : L("Next", "下一项");
            openingTourRoot.SetAsLastSibling();
        }

        private void PositionOpeningTourCard(Bounds targetBounds)
        {
            const float gap = 36f;
            const float margin = 12f;
            var half = openingTourCard.rect.size * 0.5f;
            var focusLeft = targetBounds.center.x -
                (targetBounds.size.x + 10f) * 0.5f;
            var focusRight = targetBounds.center.x +
                (targetBounds.size.x + 10f) * 0.5f;
            var focusBottom = targetBounds.center.y -
                (targetBounds.size.y + 10f) * 0.5f;
            Vector2 position;
            if (openingTourStep <= 2)
                position = new Vector2(focusRight + gap + half.x,
                    targetBounds.center.y);
            else if (openingTourStep == 3)
                position = new Vector2(targetBounds.center.x,
                    focusBottom - gap - half.y);
            else
                position = new Vector2(focusLeft - gap - half.x,
                    targetBounds.center.y);

            var area = openingTourRoot.rect;
            position.x = Mathf.Clamp(position.x, area.xMin + half.x + margin,
                area.xMax - half.x - margin);
            position.y = Mathf.Clamp(position.y, area.yMin + half.y + margin,
                area.yMax - half.y - margin);
            openingTourCard.anchoredPosition = position;
        }

        private void RefreshPendingPlacement(string upgradeSummary,
            ConstructionTileData pendingPlot,
            IReadOnlyList<ConstructionTileData> projectedBoard)
        {
            if (placementConfirmation == null) return;
            placementConfirmation.SetActive(hasPendingPlacement);
            if (!hasPendingPlacement) return;
            placementConfirmation.transform.SetAsLastSibling();
            placementConfirmationIcon.sprite = ConstructionIconCatalog.GetBuild(
                pendingCategory, pendingCategory == ConstructionCategory.Green &&
                    pendingPlanting == GreenPlanting.None
                    ? run.ResolveGreenPlanting(pendingColumn, pendingRow)
                    : pendingPlanting);
            var resolvedPlanting = pendingCategory == ConstructionCategory.Green &&
                pendingPlanting == GreenPlanting.None
                ? run.ResolveGreenPlanting(pendingColumn, pendingRow)
                : pendingPlanting;
            var pendingLabel = pendingCategory == ConstructionCategory.Green &&
                pendingPlanting == GreenPlanting.None
                ? L($"Green · {PlantingLabel(resolvedPlanting)}",
                    $"绿地 · {PlantingLabel(resolvedPlanting)}")
                : SelectedBuildLabel();
            if (run.UsesFillHand && run.BuiltCount == 0)
                pendingLabel = L("Central oak", "中央橡树");
            var terrace = pendingPlot != null &&
                ConstructionPlacementScore.IsTerraceRestaurant(pendingPlot, projectedBoard);
            if (terrace)
                pendingLabel = L("Terrace restaurant", "露台餐厅");
            var provision = terrace ? TerraceProvisionBrief() :
                RoomProvisionBrief(pendingCategory, resolvedPlanting);
            if (pendingCategory == ConstructionCategory.Green)
                provision = run.UsesFillHand && run.BuiltCount == 0
                    ? L("Already mature", "已成熟")
                    : L($"Matures on day {run.CurrentDay + 2}",
                        $"第 {run.CurrentDay + 2} 天成熟");
            placementConfirmationLocation.text = pendingLabel + "\n" + provision;
            placementConfirmationNotice.text = string.IsNullOrEmpty(upgradeSummary)
                ? PlacementDecisionHint() : upgradeSummary;
            placementCancelLabel.text = L("Cancel", "取消");
            placementConfirmLabel.text = L("Confirm", "确认建造");
        }

        private string PlacementDecisionHint()
        {
            if (run.UsesFillHand && run.BuiltCount == 0)
                return L("Place the oak in the centre. Confirm to settle this turn and see if the squirrel eats.",
                    "把橡树放在正中。确认后结算这回合，看看松鼠吃到食物没有。");
            switch (pendingCategory)
            {
                case ConstructionCategory.Green:
                    return pendingPlanting == GreenPlanting.None
                        ? GreenPlantingReason(pendingColumn, pendingRow)
                        : L("Uses the chosen plant type.", "按选定的植物类型放置。");
                case ConstructionCategory.Residence:
                    if (run.TryPreviewResidenceRoutes(pendingColumn, pendingRow,
                            out var work, out var meal))
                    {
                        if (work && meal)
                            return L("Jobs and meals are connected. A daily cycle still needs free places.",
                                "岗位和餐位已接通；有空位时可完成每日循环。");
                        if (work)
                            return L("A job is reachable. Connect a restaurant or supermarket for meals.",
                                "岗位已接通；还需连通餐厅或超市供餐。");
                        if (meal)
                            return L("A meal is reachable. Connect a workshop, restaurant or supermarket for work.",
                                "餐位已接通；还需连通可上班的建筑。");
                    }
                    return L("No job or meal route yet. Connect nearby services directly or by street.",
                        "目前无可达岗位或餐位；后续需直邻或经街道接通。");
                case ConstructionCategory.Restaurant:
                    return run.UsesNeighborUpgrades
                        ? L("Share an edge with a plaza to open a third meal seat.",
                            "餐厅与广场共边可升级为露台餐厅，增加 1 个餐位。")
                        : L("Residents must reach it directly or by street to use the job and meal.",
                            "居民直邻或经街道到达，才能在这里上班和用餐。");
                case ConstructionCategory.Workshop:
                    return L("Residents need a route here and a meal elsewhere to finish a daily cycle.",
                        "居民需能到达这里，并吃到饭，才算完成每日循环。");
                case ConstructionCategory.Waste:
                    return run.UsesUncappedWaste
                        ? L("Clears all waste nearby or in rooms linked by continuous streets, including new waste.",
                            "清运周围八格或经连续街道接通的房间中的全部垃圾，含新产生的垃圾。")
                        : L("Clears old waste nearby or in rooms linked by continuous streets, restoring blocked services.",
                            "清运周围八格或经连续街道接通的房间中的旧垃圾，帮助恢复受阻服务。");
                case ConstructionCategory.Street:
                    return run.UsesNeighborUpgrades
                        ? L("Green plots on opposite sides make this a slow street and halve collision risk.",
                            "道路相对两侧若各与一块绿地共边，可升级为慢行街，车祸风险减半。")
                        : L("Extends resident routes. Nearby animals face traffic risk.",
                            "延伸居民路线；附近动物也会面临车祸风险。");
                case ConstructionCategory.Supermarket:
                    return L("Residents must reach it directly or by street to use its job and meals.",
                        "居民直邻或经街道到达，才能在这里上班和用餐。");
                case ConstructionCategory.Square:
                    return run.WouldConnectSquareToHome(pendingColumn, pendingRow)
                        ? L("Residents with a completed cycle can visit and leave animal food.",
                            "完成循环的居民可到访，给动物留下食物。")
                        : L("No home route yet. Visits and animal food start after it connects.",
                            "尚未连通住宅；接通后才会有到访和动物食物。");
                default:
                    return string.Empty;
            }
        }

        private void RefreshTutorialFocus()
        {
            if (tutorialFocus == null) return;
            if (run?.TutorialTipsSkipped == true || QuickGuideOpen ||
                openingTourRoot != null && openingTourRoot.gameObject.activeSelf)
            {
                tutorialFocus.gameObject.SetActive(false);
                return;
            }
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
            if (!IsBoardCaseOpen || run == null || run.IsFinished || IsEndless)
                return null;
            if (run.TutorialChapter == ConstructionTutorialChapter.Complete)
                return null;
            if (hasPendingPlacement)
                return placementConfirmButton.transform as RectTransform;
            if (run.UsesFillHand)
                return run.BuiltCount == 0
                    ? cellBackgrounds[3, 3].rectTransform
                    : run.IsIntroPhase && run.FillHandActiveIndex >= 0 &&
                      run.FillHandActiveIndex < fillHandCards.Length
                        ? fillHandCards[run.FillHandActiveIndex].transform as RectTransform
                        : null;
            if (run.TutorialChapter == ConstructionTutorialChapter.InspectOak)
                return openingOakInspected
                    ? nextDayButton.transform as RectTransform
                    : VisibleTile(ConstructionBoardModel.StarterOakId);
            if (run.BuiltToday)
                return nextDayButton.transform as RectTransform;
            if (run.IsIntroPhase)
            {
                if (!introChoiceMade)
                    return buildPaletteOpen
                        ? categoryFaces[run.IntroRequiredCategory.Value].rectTransform
                        : buildPaletteButton.transform as RectTransform;
                return null;
            }
            if (run.TutorialChapter == ConstructionTutorialChapter.Green &&
                !tutorialGreenChoiceMade)
                return buildPaletteOpen
                    ? categoryFaces[ConstructionCategory.Green].rectTransform
                    : buildPaletteButton.transform as RectTransform;
            var suggested = SuggestedTutorialCategory();
            if (!buildPaletteOpen && selectedCategory != suggested)
                return buildPaletteButton.transform as RectTransform;
            if (buildPaletteOpen && selectedCategory != suggested)
                return categoryFaces[suggested].rectTransform;
            // A single framed cell reads like a mandatory tutorial slot.
            // The ordinary + hints show every valid choice instead.
            return null;
        }

        private ConstructionCategory SuggestedTutorialCategory() => run.TutorialChapter switch
        {
            ConstructionTutorialChapter.Green => ConstructionCategory.Green,
            ConstructionTutorialChapter.Residence => ConstructionCategory.Residence,
            ConstructionTutorialChapter.Restaurant => ConstructionCategory.Restaurant,
            ConstructionTutorialChapter.Workshop => ConstructionCategory.Workshop,
            ConstructionTutorialChapter.Waste => ConstructionCategory.Waste,
            ConstructionTutorialChapter.Street => ConstructionCategory.Street,
            ConstructionTutorialChapter.Supermarket => ConstructionCategory.Supermarket,
            ConstructionTutorialChapter.Square => ConstructionCategory.Square,
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

        private Text AnimalCountBadge(Transform leftRail, Sprite speciesSprite)
        {
            var badge = Panel("Animal group count", leftRail,
                new Vector2(0, -20.5f), new Vector2(28, 39), Ink);
            badge.raycastTarget = true;
            AddMetricPictogram(badge.transform, speciesSprite);
            var label = TextAt("Count", badge.transform, new Vector2(0, -9),
                new Vector2(28, 18), "1", 11, true, Color.white,
                TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            badge.gameObject.SetActive(false);
            return label;
        }

        // Living residents keep a white count. An amber icon marked 待/…
        // means a different animal may immigrate; it is never counted alive.
        private void ShowAnimalBadge(Image icon, Text countLabel, int count,
            ConstructionWildlifeReturnStatus arrival, string tileId)
        {
            var present = count > 0;
            var waiting = !present && arrival.Pending &&
                !string.IsNullOrEmpty(tileId) && tileId == arrival.HabitatTileId;
            // The board already renders a moving 3D animal. Keep this small
            // static portrait only as a fallback when the world view is absent.
            icon.gameObject.SetActive((present || waiting) &&
                (worldView == null || !worldView.IsReady));
            countLabel.transform.parent.gameObject.SetActive(present || waiting);
            if (!present && !waiting) return;
            icon.color = present ? Color.white :
                new Color(1f, 0.78f, 0.48f, 0.9f);
            countLabel.text = present ? count.ToString() : L("...", "待");
            countLabel.transform.parent.GetComponent<Image>().color =
                present ? Ink : IdleAmber;
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
                    // The centre hinge reaches x=432. Reserve a safe gutter
                    // before the left card instead of covering its icon.
                    new Vector2(579f + (column - 0.5f) * 152f,
                        95f - row * 83f),
                    new Vector2(132, 68), "", 13,
                    Gold, Paper, () =>
                    {
                        if (TrySelect(category))
                        {
                            buildPaletteOpen = category == ConstructionCategory.Residence;
                            RefreshPaletteVisibility();
                        }
                    }, themed: false);
                // The former painted button has a transparent lower margin.
                // Use a solid face inside one shared brass trim so the title,
                // daily provision and tutorial frame all have the same bounds.
                var face = Panel("Card face", button.transform,
                    Vector2.zero, new Vector2(128, 64), Ink);
                face.raycastTarget = false;
                categoryFaces.Add(category, face);
                var colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                button.gameObject.AddComponent<CanvasGroup>();
                var icon = Panel("Building icon", button.transform,
                    new Vector2(-49, 13), new Vector2(24, 24), Color.white);
                icon.sprite = IsEndless
                    ? ConstructionHandArtCatalog.GetRoom(category) ??
                      ConstructionIconCatalog.GetBuild(category)
                    : ConstructionIconCatalog.GetBuild(category);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                AttachHover(button.gameObject, () => icon.sprite,
                    () => CategoryLabel(category),
                    () => CategoryHoverDetails(category),
                    TooltipLocation.LeftLeaf);
                var label = TextAt("Label", button.transform,
                    new Vector2(-3, 13), new Vector2(66, 26),
                    category.ToString(), Chinese ? 14 : 13, true, Paper,
                    TextAnchor.MiddleLeft);
                label.raycastTarget = false;
                var provision = TextAt("Daily provision", button.transform,
                    new Vector2(0, -18), new Vector2(122, 22), "",
                    13,
                    true, new Color(0.98f, 0.96f, 0.89f),
                    TextAnchor.MiddleLeft);
                provision.raycastTarget = false;
                categoryProvisionLabels.Add(category, provision);
                var status = TextAt("Unlock status", button.transform,
                    new Vector2(0, -18), new Vector2(122, 20), "", 11,
                    false, Paper, TextAnchor.MiddleLeft);
                status.raycastTarget = false;
                status.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
                var countBacking = Panel("Build limit badge", button.transform,
                    new Vector2(47, 13), new Vector2(34, 20),
                    new Color(0.08f, 0.13f, 0.17f));
                countBacking.raycastTarget = false;
                var count = TextAt("Built and limit", countBacking.transform,
                    Vector2.zero, new Vector2(34, 20), "–", 12, true,
                    Paper, TextAnchor.MiddleCenter);
                count.raycastTarget = false;
                categoryCountLabels.Add(category, count);
                categoryStatusLabels.Add(category, status);
                categoryButtons.Add(category, button);
            }
        }

        private void BuildResidenceConditionCards(Transform parent)
        {
            residenceConditionHeading = TextAt("Residence condition heading", parent,
                new Vector2(CaseLeafCenter - 3f, 228f),
                new Vector2(310f, 26f), "", 15,
                true, Ink, TextAnchor.MiddleCenter);
            residenceConditionHeading.raycastTarget = false;
            foreach (var kind in new[] { ResidenceKind.Standard,
                ResidenceKind.Garden, ResidenceKind.Urban })
            {
                var offset = kind == ResidenceKind.Standard ? -110f :
                    kind == ResidenceKind.Garden ? 0f : 110f;
                var card = Panel($"Residence condition {kind}", parent,
                    new Vector2(CaseLeafCenter - 3f + offset, 173f),
                    new Vector2(104f, 52f),
                    new Color(0.89f, 0.85f, 0.73f, 0.94f));
                var accent = Panel("Condition accent", card.transform,
                    new Vector2(0f, 24f), new Vector2(100f, 2f), Gold);
                accent.raycastTarget = false;
                var label = TextAt("Label", card.transform, Vector2.zero,
                    new Vector2(100f, 48f), string.Empty, 12, true,
                    Ink, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                AttachHover(card.gameObject,
                    () => ConstructionIconCatalog.GetBuild(ConstructionCategory.Residence),
                    () => ResidenceConditionLabel(kind),
                    () => ResidenceConditionRule(kind), TooltipLocation.LeftLeaf);
                residenceConditionCards.Add(kind, card);
            }
        }

        private void BuildPrimerRules(Transform parent)
        {
            primerRulesRoot = NewRect("Primer rules", parent,
                new Vector2(0f, -27f), new Vector2(310, 185));
            var positions = new[] { 0f, -19f, -61f };
            for (var index = 0; index < positions.Length; index++)
            {
                var chip = Panel($"Primer label chip {index}", primerRulesRoot,
                    new Vector2(-98, positions[index]),
                    new Vector2(62, 26), new Color(0.89f, 0.81f, 0.65f));
                chip.raycastTarget = false;
                primerRuleLabels[index] = TextAt($"Primer label {index}",
                    chip.transform, Vector2.zero, new Vector2(60, 24),
                    string.Empty, 13, true, Ink, TextAnchor.MiddleCenter);
                primerRuleLabels[index].raycastTarget = false;
                primerRuleDetails[index] = TextAt($"Primer detail {index}",
                    primerRulesRoot, new Vector2(38, positions[index]),
                    new Vector2(200, index == 2 ? 30 : 40), string.Empty,
                    13, false, Ink, TextAnchor.MiddleLeft);
                primerRuleDetails[index].raycastTarget = false;
                if (index == 0) continue;
                chip.gameObject.SetActive(false);
                primerRuleDetails[index].gameObject.SetActive(false);
            }
            primerRulesRoot.gameObject.SetActive(false);
        }

        private Text MetricCard(Transform parent, string name, Vector2 position,
            Sprite sprite, Func<string> title, Func<string> details,
            float width = 116f, Func<string> caption = null,
            ScoreSide scoreSide = ScoreSide.None)
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
                new Vector2(19f, caption == null ? 0f : 10f),
                new Vector2(width - 57f, caption == null ? 44f : 36f),
                "–", 22, true, Paper, TextAnchor.MiddleCenter);
            value.raycastTarget = false;
            if (caption != null)
            {
                var label = TextAt("Metric caption", card.transform,
                    new Vector2(19f, -17f), new Vector2(width - 57f, 22f),
                    caption(), 13, true, Gold, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                metricCaptions.Add(label, caption);
            }
            if (scoreSide != ScoreSide.None)
                AttachScorePlaqueHover(card.gameObject, scoreSide);
            else
                AttachHover(card.gameObject, () => sprite, title, details,
                    TooltipLocation.LeftLeaf);
            return value;
        }

        private void BuildScoreMotion(Transform canvas)
        {
            scoreMotionLayer = NewRect("Score motion overlay", canvas,
                Vector2.zero, Vector2.zero);
            Stretch(scoreMotionLayer);
            scoreMotionGroup = scoreMotionLayer.gameObject.AddComponent<CanvasGroup>();
            scoreMotionGroup.alpha = 0f;
            scoreMotionGroup.interactable = false;
            scoreMotionGroup.blocksRaycasts = false;
            var border = Panel("Score motion border", scoreMotionLayer,
                Vector2.zero, new Vector2(150, 57), Gold);
            border.raycastTarget = false;
            scoreMotionTag = border.rectTransform;
            var face = Panel("Score motion face", border.transform,
                Vector2.zero, new Vector2(146, 53), Ink);
            face.raycastTarget = false;
            scoreMotionAccent = Panel("Score motion accent", face.transform,
                new Vector2(-68, 0), new Vector2(5, 45), UsedGreen);
            scoreMotionAccent.raycastTarget = false;
            scoreMotionValue = TextAt("Score change", face.transform,
                new Vector2(-29, 0), new Vector2(60, 44), "", 22, true,
                ScorePlaqueNumber, TextAnchor.MiddleCenter);
            scoreMotionValue.raycastTarget = false;
            scoreMotionReason = TextAt("Score reason", face.transform,
                new Vector2(36, 0), new Vector2(72, 42), "", 12, true,
                Paper, TextAnchor.MiddleLeft);
            scoreMotionReason.raycastTarget = false;
        }

        private void StartScoreMotion(ConstructionEcologyDayResult ecology,
            int oldAnimal, int oldResident, int oldShared)
        {
            // The final-result card takes over the screen on the last day.
            if (scoreMotionLayer == null || run == null || run.IsFinished ||
                ecology == null) return;
            var penalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
            var animalGain = (long)run.AnimalScore - oldAnimal + penalty;
            var residentGain = (long)run.HumanScore - oldResident;
            var steps = new List<ScoreMotionStep>(8);
            if (animalGain != 0)
            {
                // Several food areas may score at once. In that case start
                // from the board, never imply the whole gain came from the
                // first room in the list.
                var foodTileId = run.HasFedAnimalScore
                    ? ecology.squirrelFoodTileIds?.FirstOrDefault() ??
                      ecology.pigeonMealTileIds?.FirstOrDefault() ??
                      ecology.hedgehogMealTileIds?.FirstOrDefault()
                    : run.ScoringVersion >= 8 &&
                    ecology.scoredFoodTileIds?.Distinct().Count() > 1
                        ? null : run.ScoringVersion >= 7
                    ? ecology.scoredFoodTileIds?.FirstOrDefault()
                    : ecology.squirrelFoodTileIds?.FirstOrDefault() ??
                        ecology.pigeonMealTileIds?.FirstOrDefault() ??
                        ecology.hedgehogFoodTileId ??
                        ecology.urbanMealTileIds?.FirstOrDefault();
                steps.Add(new ScoreMotionStep(ScoreSide.Animal,
                    (int)animalGain, foodTileId,
                    run.HasFedAnimalScore ? "Wildlife fed" :
                        run.ScoringVersion >= 7 ? "Nearby food areas" : "Food eaten",
                    run.HasFedAnimalScore ? "动物进食" :
                        run.ScoringVersion >= 7 ? "附近食源" : "动物觅食"));
            }
            if (penalty > 0)
                steps.Add(new ScoreMotionStep(ScoreSide.Animal, -penalty,
                    ecology.trafficStreetTileId,
                    "Traffic collision", "街道车祸"));
            if (residentGain != 0 && run.ScoringVersion >= 8 &&
                run.LastHumanDay?.residents != null)
            {
                var residents = run.LastHumanDay.residents;
                var sourceLinks = residents.Sum(person => person.linkageBonus);
                var wildlifeLinks = residents.Sum(person => person.biodiversityBonus);
                var ordinaryGain = residentGain - sourceLinks - wildlifeLinks;
                if (ordinaryGain != 0)
                    steps.Add(new ScoreMotionStep(ScoreSide.Resident,
                        (int)ordinaryGain,
                        residents.FirstOrDefault()?.homeTileId,
                        "Resident day", "居民循环"));
                foreach (var person in residents.Where(person =>
                             person.linkageBonus > 0))
                    steps.Add(new ScoreMotionStep(ScoreSide.Resident,
                        person.linkageBonus,
                        person.linkageSourceTileId ?? person.homeTileId,
                        "Shared food", "共享食源"));
                foreach (var person in residents.Where(person =>
                             person.biodiversityBonus > 0))
                    steps.Add(new ScoreMotionStep(ScoreSide.Resident,
                        person.biodiversityBonus,
                        person.biodiversityHabitatTileIds?.FirstOrDefault() ??
                            person.homeTileId,
                        "Wildlife neighbours", "动物邻居"));
            }
            else if (residentGain != 0)
                steps.Add(new ScoreMotionStep(ScoreSide.Resident,
                    (int)residentGain,
                    run.BuiltTiles.FirstOrDefault(tile =>
                        tile.category == ConstructionCategory.Residence)?.id,
                    "Resident day", "居民循环"));
            if (steps.Count == 0) return;
            scoreMotionAnimation = StartCoroutine(AnimateScoreMotion(steps,
                oldAnimal, oldResident, oldShared,
                run.AnimalScore, run.HumanScore, run.BalanceScore));
        }

        private IEnumerator AnimateScoreMotion(IReadOnlyList<ScoreMotionStep> steps,
            int oldAnimal, int oldResident, int oldShared,
            int settledAnimal, int settledResident, int settledShared)
        {
            var animal = oldAnimal;
            var resident = oldResident;
            var shared = oldShared;
            WriteScoreMotionTotals(animal, resident, shared);
            var activeSide=ScoreSide.None;
            foreach (var step in steps.OrderBy(step=>step.Side==ScoreSide.Resident?0:1))
            {
                if(activeSide!=step.Side)
                {
                    EndScoreTracePhase();
                    yield return new WaitForSecondsRealtime(0.14f);
                    ShowScoreTracePhase(step.Side);activeSide=step.Side;
                }
                var card = step.Side == ScoreSide.Animal
                    ? animalMetricText.transform.parent.GetComponent<Image>()
                    : humanMetricText.transform.parent.GetComponent<Image>();
                var cardColor = card.color;
                var changeColor = step.Delta >= 0 ? UsedGreen : BlockedRed;
                var source = ScoreMotionSource(step.SourceTileId);
                var target = scoreMotionLayer.InverseTransformPoint(
                    card.rectTransform.position);
                scoreMotionValue.text = step.Delta > 0
                    ? $"+{step.Delta}" : step.Delta.ToString(
                        CultureInfo.InvariantCulture);
                scoreMotionValue.color = FillScoreColor(step.Side,step.Delta);
                scoreMotionReason.text = L(step.EnglishReason, step.ChineseReason);
                scoreMotionAccent.color = changeColor;
                var nextAnimal = step.Side == ScoreSide.Animal
                    ? (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
                        (long)animal + step.Delta)) : animal;
                var nextResident = step.Side == ScoreSide.Resident
                    ? (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
                        (long)resident + step.Delta)) : resident;
                var nextShared = shared;
                var elapsed = 0f;
                var duration = steps.Count > 5 ? 0.43f : 0.7f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var fraction = Mathf.Clamp01(elapsed / duration);
                    var eased = Mathf.SmoothStep(0f, 1f, fraction);
                    var position = Vector2.Lerp(source, target, eased);
                    position.y += Mathf.Sin(fraction * Mathf.PI) * 28f;
                    scoreMotionTag.anchoredPosition = position;
                    scoreMotionGroup.alpha = Mathf.Clamp01(fraction * 8f) *
                        Mathf.Clamp01((1f - fraction) * 6f);
                    card.color = Color.Lerp(cardColor, changeColor,
                        Mathf.Sin(fraction * Mathf.PI) * 0.65f);
                    WriteScoreMotionTotals(
                        InterpolateScore(animal, nextAnimal, eased),
                        InterpolateScore(resident, nextResident, eased),
                        InterpolateScore(shared, nextShared, eased));
                    yield return null;
                }
                card.color = cardColor;
                animal = nextAnimal;
                resident = nextResident;
                shared = nextShared;
                WriteScoreMotionTotals(animal, resident, shared);
                scoreMotionGroup.alpha = 0f;
                yield return new WaitForSecondsRealtime(
                    steps.Count > 5 ? 0.04f : 0.12f);
            }
            EndScoreTracePhase();
            yield return new WaitForSecondsRealtime(0.14f);
            ShowScoreTracePhase(ScoreSide.Shared);
            scoreMotionReason.text=L("Shared score · third batch","共同得分 · 第三批");
            scoreMotionValue.text=(settledShared-oldShared).ToString("+0;-0;0");
            scoreMotionValue.color=ScoreSharedBlue;
            scoreMotionTag.anchoredPosition=ScoreMotionSource(null);
            scoreMotionGroup.alpha=1f;
            var sharedTime=0f;
            while(sharedTime<0.9f)
            {
                sharedTime+=Time.unscaledDeltaTime;
                WriteScoreMotionTotals(settledAnimal,settledResident,
                    InterpolateScore(oldShared,settledShared,Mathf.Clamp01(sharedTime/0.9f)));
                yield return null;
            }
            WriteScoreMotionTotals(settledAnimal, settledResident, settledShared);
            scoreMotionGroup.alpha = 0f;
            scoreMotionAnimation = null;
            EndScoreTracePhase();
        }

        private Vector2 ScoreMotionSource(string tileId)
        {
            if (!string.IsNullOrEmpty(tileId))
            {
                var tile = run.BuiltTiles.FirstOrDefault(built => built.id == tileId);
                if (tile != null)
                {
                    var column = tile.column - viewMinColumn;
                    var row = tile.row - viewMinRow;
                    if (column >= 0 && column < 7 && row >= 0 && row < 7 &&
                        cellBackgrounds[column, row] != null)
                        return scoreMotionLayer.InverseTransformPoint(
                            cellBackgrounds[column, row].rectTransform.position);
                }
            }
            return caseBoardOverlay != null
                ? scoreMotionLayer.InverseTransformPoint(
                    caseBoardOverlay.transform.position)
                : Vector2.zero;
        }

        private static int InterpolateScore(int from, int to, float fraction) =>
            (int)Math.Round(from + ((double)to - from) * fraction);

        private void WriteScoreMotionTotals(int animal, int resident, int shared,
            int? animatedBuild = null)
        {
            animalMetricText.text = IsEndless
                ? CompactScore(animal, Chinese) : animal.ToString();
            humanMetricText.text = IsEndless
                ? CompactScore(resident, Chinese) : resident.ToString();
            if (IsEndless)
            {
                endlessScoreValues[0].text = run.HasBalanceScore
                    ? CompactScore(run.UsesOverallScore
                        ? shared + (animatedBuild ?? run.PlacementScore) : shared, Chinese) : "–";
                endlessScoreValues[1].text = CompactScore(animal, Chinese);
                endlessScoreValues[2].text = CompactScore(resident, Chinese);
            }
            else if (run.UsesSpecialTasks)
            {
                var build = animatedBuild ?? run.PlacementScore;
                var total = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue,
                    (long)shared + build + run.SpecialTaskScore));
                balanceScoreText.text = L(
                    $"TOTAL {total} = SHARED {shared}\n" +
                    $"+ BUILD {build} + TASKS {run.SpecialTaskScore}",
                    $"总分 {total} = 共栖 {shared}\n" +
                    $"+ 建造 {build} + 任务 {run.SpecialTaskScore}");
                UpdateFillScoreSummary(animal, resident, shared, build);
            }
            else if (run.HasAdditiveCoexistenceScore)
                balanceScoreText.text = L(
                    $"SHARED {shared}\n= ANIMAL {animal} + RESIDENT {resident}",
                    $"共栖 {shared} = 动物 {animal} + 居民 {resident}");
            else if (run.HasBalanceScore)
                balanceScoreText.text = L(
                    $"OLD SHARED {shared}\nANIMAL {animal} · RESIDENT {resident}",
                    $"旧共栖 {shared} · 动物 {animal} · 居民 {resident}");
        }

        private void StopScoreMotion(bool restoreTotals = true)
        {
            EndScoreTracePhase();
            ClearIdleCues();
            if (scoreMotionAnimation != null)
            {
                StopCoroutine(scoreMotionAnimation);
                scoreMotionAnimation = null;
                if (restoreTotals && run != null && animalMetricText != null &&
                    humanMetricText != null && balanceScoreText != null)
                    WriteScoreMotionTotals(run.AnimalScore, run.HumanScore,
                        run.BalanceScore);
            }
            if (scoreMotionGroup != null) scoreMotionGroup.alpha = 0f;
            ClearFillScoreTags();
            if (animalMetricText != null && animalMetricText.transform.parent != null)
                animalMetricText.transform.parent.GetComponent<Image>().color = Ink;
            if (humanMetricText != null && humanMetricText.transform.parent != null)
                humanMetricText.transform.parent.GetComponent<Image>().color = Ink;
        }

        private void BuildTooltip(Transform canvas)
        {
            tooltipCanvas = (RectTransform)canvas;
            var panel = Panel("Context tooltip", canvas, Vector2.zero,
                new Vector2(TooltipWidth, 120f), Ink);
            tooltipRect = panel.rectTransform;
            panel.raycastTarget = false;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.06f, 0.10f, 0.12f, 0.34f);
            shadow.effectDistance = new Vector2(5f, -6f);
            tooltipAccent = Panel("Tooltip accent", panel.transform,
                new Vector2(0f, 58f), new Vector2(TooltipWidth - 4f, 3f), Gold);
            tooltipAccent.raycastTarget = false;
            tooltipIcon = Panel("Tooltip icon", panel.transform,
                new Vector2(-148f, 33f), new Vector2(30f, 30f), Color.white);
            tooltipIcon.preserveAspect = true;
            tooltipIcon.raycastTarget = false;
            tooltipTitle = TextAt("Tooltip title", panel.transform,
                new Vector2(14f, 33f), new Vector2(TooltipTextWidth, 34f), "", 18,
                true, Paper, TextAnchor.MiddleLeft);
            tooltipTitle.raycastTarget = false;
            tooltipBody = TextAt("Tooltip details", panel.transform,
                new Vector2(14f, -16f), new Vector2(TooltipTextWidth, 52f), "", 15,
                false, Paper, TextAnchor.UpperLeft);
            tooltipBody.raycastTarget = false;
            tooltipBody.lineSpacing = 1.08f;
            panel.gameObject.SetActive(false);
            BuildScorePlaque(canvas);
        }

        private void BuildRoomBadgeTooltip(Transform canvas)
        {
            var rim = Panel("Room badge tooltip", canvas, Vector2.zero,
                new Vector2(252, 110), Gold);
            roomBadgeTooltipRect = rim.rectTransform;
            rim.raycastTarget = false;
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.08f, 0.11f, 0.42f);
            shadow.effectDistance = new Vector2(4f, -5f);
            var face = Panel("Tooltip face", rim.transform, Vector2.zero,
                new Vector2(248, 106), Ink);
            face.raycastTarget = false;
            roomBadgeTooltipHeading = TextAt("Tooltip heading", rim.transform,
                new Vector2(0, 34), new Vector2(222, 24), "", 14,
                true, Paper, TextAnchor.MiddleLeft);
            roomBadgeTooltipHeading.raycastTarget = false;
            Panel("Tooltip divider", rim.transform,
                new Vector2(0, 19), new Vector2(222, 1), Gold)
                .raycastTarget = false;
            roomBadgeTooltipDetails = TextAt("Tooltip details", rim.transform,
                new Vector2(0, -22), new Vector2(222, 65), "", 12,
                false, Paper, TextAnchor.UpperLeft);
            roomBadgeTooltipDetails.raycastTarget = false;
            roomBadgeTooltipDetails.lineSpacing = 1.08f;
            rim.gameObject.SetActive(false);
        }

        private void ShowRoomBadgeTooltip(string heading, string details,
            RectTransform anchor)
        {
            if (roomBadgeTooltipRect == null || tooltipCanvas == null ||
                openingTourBoardTarget == null || anchor == null)
            {
                HideRoomBadgeTooltip();
                return;
            }
            roomBadgeTooltipHeading.text = FillHandTiming(heading);
            roomBadgeTooltipDetails.text = FillHandTiming(details);
            var bodyHeight = Mathf.Clamp(
                roomBadgeTooltipDetails.preferredHeight + 8f, 53f, 158f);
            // Reserve room for the heading, divider, and a visible gap before
            // the body. With 45 px the body starts 6 px above the divider.
            var height = bodyHeight + 60f;
            roomBadgeTooltipRect.sizeDelta = new Vector2(252f, height);
            roomBadgeTooltipRect.Find("Tooltip face")
                .GetComponent<RectTransform>().sizeDelta =
                new Vector2(248f, height - 4f);
            roomBadgeTooltipHeading.rectTransform.anchoredPosition =
                new Vector2(0, height * 0.5f - 23f);
            roomBadgeTooltipRect.Find("Tooltip divider")
                .GetComponent<RectTransform>().anchoredPosition =
                new Vector2(0, height * 0.5f - 43f);
            roomBadgeTooltipDetails.rectTransform.sizeDelta =
                new Vector2(222f, bodyHeight);
            roomBadgeTooltipDetails.rectTransform.anchoredPosition =
                new Vector2(0, -height * 0.5f + bodyHeight * 0.5f + 8f);

            var boardCorners = new Vector3[4];
            var anchorCorners = new Vector3[4];
            openingTourBoardTarget.GetWorldCorners(boardCorners);
            anchor.GetWorldCorners(anchorCorners);
            var boardLeft = tooltipCanvas.InverseTransformPoint(boardCorners[0]).x;
            var boardRight = tooltipCanvas.InverseTransformPoint(boardCorners[2]).x;
            var boardBottom = tooltipCanvas.InverseTransformPoint(boardCorners[0]).y;
            var boardTop = tooltipCanvas.InverseTransformPoint(boardCorners[2]).y;
            var anchorLeft = tooltipCanvas.InverseTransformPoint(anchorCorners[0]).x;
            var anchorRight = tooltipCanvas.InverseTransformPoint(anchorCorners[2]).x;
            var anchorY = tooltipCanvas.InverseTransformPoint(anchor.position).y;
            const float halfWidth = 126f;
            var x = anchorRight + 8f + halfWidth;
            if (x + halfWidth > boardRight - 5f)
                x = anchorLeft - 8f - halfWidth;
            x = Mathf.Clamp(x, boardLeft + halfWidth + 5f,
                boardRight - halfWidth - 5f);
            var y = Mathf.Clamp(anchorY,
                boardBottom + height * 0.5f + 5f,
                boardTop - height * 0.5f - 5f);
            roomBadgeTooltipRect.anchoredPosition = new Vector2(x, y);
            roomBadgeTooltipRect.gameObject.SetActive(true);
            roomBadgeTooltipRect.SetAsLastSibling();
        }

        private void HideRoomBadgeTooltip()
        {
            if (roomBadgeTooltipRect != null)
                roomBadgeTooltipRect.gameObject.SetActive(false);
        }

        private void BuildScorePlaque(Transform canvas)
        {
            // Both score metrics share this plaque; room details keep the
            // general tooltip. The 346 px card fits inside the 382 px leaf.
            var border = Panel("Score plaque", canvas,
                new Vector2(-CaseLeafCenter, -262f),
                new Vector2(346f, 226f),
                Color.white);
            scorePlaque = border.rectTransform;
            scorePlaqueTexture = MakeScorePlaqueTexture();
            scorePlaqueSprite = Sprite.Create(scorePlaqueTexture,
                new Rect(0f, 0f, scorePlaqueTexture.width,
                    scorePlaqueTexture.height), new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect);
            border.sprite = scorePlaqueSprite;
            border.raycastTarget = false;
            var shadow = border.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.07f, 0.11f, 0.14f, 0.28f);
            shadow.effectDistance = new Vector2(4f, -5f);
            var face = NewRect("Plaque face", border.transform, Vector2.zero,
                new Vector2(342f, 222f));
            Panel("Score divider", face.transform, new Vector2(50f, 43f),
                new Vector2(1f, 53f), new Color(ScorePlaqueBrass.r,
                    ScorePlaqueBrass.g, ScorePlaqueBrass.b, 0.55f))
                .raycastTarget = false;
            Panel("Content divider", face.transform, new Vector2(0f, 6f),
                new Vector2(312f, 1f), ScorePlaqueBrass).raycastTarget = false;
            Panel("Forecast divider", face.transform, new Vector2(0f, -59f),
                new Vector2(312f, 1f),
                new Color(ScorePlaqueBrass.r, ScorePlaqueBrass.g,
                    ScorePlaqueBrass.b, 0.45f)).raycastTarget = false;
            scorePlaqueEyebrow = TextAt("Score eyebrow", face.transform,
                new Vector2(-110f, 58f), new Vector2(105f, 19f), "", 13,
                true, ScorePlaqueMuted, TextAnchor.MiddleLeft);
            scorePlaqueTitle = TextAt("Score title", face.transform,
                new Vector2(-110f, 33f), new Vector2(105f, 30f), "", 20,
                true, Paper, TextAnchor.MiddleLeft);
            scorePlaquePoints = TextAt("Score points", face.transform,
                new Vector2(-11f, 40f), new Vector2(82f, 59f), "", 42,
                false, ScorePlaqueNumber, TextAnchor.MiddleRight);
            scorePlaquePoints.resizeTextForBestFit = true;
            scorePlaquePoints.resizeTextMinSize = 13;
            scorePlaquePoints.resizeTextMaxSize = 42;
            scorePlaqueUnit = TextAt("Points unit", face.transform,
                new Vector2(41f, 22f), new Vector2(16f, 20f), "", 12,
                true, Paper, TextAnchor.MiddleLeft);
            scorePlaqueComparison = TextAt("Comparison summary", face.transform,
                new Vector2(110f, 41f), new Vector2(99f, 49f), "", 13,
                true, Paper, TextAnchor.MiddleLeft);
            scorePlaqueExplanation = TextAt("Scoring rule", face.transform,
                new Vector2(0f, -24f), new Vector2(312f, 55f), "", 13,
                false, Paper, TextAnchor.MiddleLeft);
            scorePlaqueForecastHeading = TextAt("Forecast heading",
                face.transform, new Vector2(-132f, -85f),
                new Vector2(67f, 36f), "", 13, true, ScorePlaqueMuted,
                TextAnchor.MiddleLeft);
            for (var index = 0; index < scorePlaqueForecast.Length; index++)
                scorePlaqueForecast[index] = TextAt($"Forecast slot {index + 1}",
                    face.transform, new Vector2(-59f + 64f * index, -85f),
                    new Vector2(62f, 42f), "", 12, true, Paper,
                    TextAnchor.MiddleCenter);
            foreach (var label in new[] { scorePlaqueEyebrow,
                scorePlaqueTitle, scorePlaquePoints, scorePlaqueUnit,
                scorePlaqueComparison, scorePlaqueExplanation,
                scorePlaqueForecastHeading }.Concat(scorePlaqueForecast))
                label.raycastTarget = false;
            border.gameObject.SetActive(false);
        }

        private static Texture2D MakeScorePlaqueTexture()
        {
            // Two texture pixels per UI pixel: a 10 px angled cut on the
            // upper-right and lower-left corners, with a 2 px brass outline.
            const int width = 692;
            const int height = 452;
            const int bevel = 20;
            const int border = 4;
            const int diagonalBorder = 6;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32,
                false)
            {
                name = "Score plaque blue brass",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            var brass = new Color32(0xb7, 0x95, 0x5b, 0xff);
            var light = new Color32(0x29, 0x3e, 0x4e, 0xff);
            var dark = new Color32(0x18, 0x2d, 0x3b, 0xff);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var lowerLeft = x + y;
                    var upperRight = (width - 1 - x) + (height - 1 - y);
                    var index = y * width + x;
                    if (lowerLeft < bevel || upperRight < bevel)
                        continue;
                    if (x < border || x >= width - border ||
                        y < border || y >= height - border ||
                        lowerLeft < bevel + diagonalBorder ||
                        upperRight < bevel + diagonalBorder)
                    {
                        pixels[index] = brass;
                        continue;
                    }
                    var gradient = Mathf.Clamp01(
                        0.48f * x / (width - 1f) +
                        0.52f * (height - 1f - y) / (height - 1f));
                    pixels[index] = (Color32)Color.Lerp(light, dark, gradient);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void AttachScorePlaqueHover(GameObject target, ScoreSide side)
        {
            var trigger = target.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowScorePlaque(side));
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry
                { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exit);
        }

        private void ShowScorePlaque(ScoreSide side)
        {
            if (scorePlaque == null || run == null || !IsBoardCaseOpen ||
                finalResultPanel.activeSelf || hasPendingPlacement) return;
            HideTooltip();
            RefreshScorePlaque(side);
            scorePlaque.gameObject.SetActive(true);
            scorePlaque.SetAsLastSibling();
        }

        private void RefreshScorePlaque(ScoreSide side)
        {
            if (scorePlaque == null || run == null) return;
            var animal = side == ScoreSide.Animal;
            scorePlaqueEyebrow.text = L("TOTAL SCORE", "累计得分");
            scorePlaqueTitle.text = animal
                ? L("ANIMALS", "动物") : L("RESIDENTS", "居民");
            scorePlaqueTitle.fontSize = Chinese ? 20 : animal ? 18 : 17;
            scorePlaquePoints.text = (animal ? run.AnimalScore :
                run.HumanScore).ToString();
            var scoreDigits = scorePlaquePoints.text.Length;
            scorePlaquePoints.fontSize = scoreDigits <= 2 ? 42 :
                scoreDigits == 3 ? 34 : scoreDigits == 4 ? 28 :
                scoreDigits == 5 ? 24 : scoreDigits == 6 ? 20 :
                Mathf.Max(13, Mathf.FloorToInt(136f / scoreDigits));
            scorePlaqueUnit.text = L("pt", "分");
            var otherScore = animal ? run.HumanScore : run.AnimalScore;
            scorePlaqueComparison.text = L(
                run.HasBalanceScore
                    ? $"{(animal ? "RESIDENTS" : "ANIMALS")} {otherScore}\nSHARED {run.BalanceScore}"
                    : $"{(animal ? "RESIDENTS" : "ANIMALS")}\n{otherScore} pts",
                run.HasBalanceScore
                    ? $"{(animal ? "居民" : "动物")} {otherScore}\n共栖 {run.BalanceScore}"
                    : $"{(animal ? "居民" : "动物")}得分\n{otherScore} 分");
            scorePlaqueExplanation.text = animal
                ? run.UsesLegacyAnimalScoring
                    ? L("Older save: fox hunts also scored.\nNew wildlife: 3 days after loss if habitat feeds.",
                        "旧存档：狐狸捕食也曾计分。\n损失后第3天起可迁入，须有栖地和食物。")
                    : run.HasFedAnimalScore
                         ? L($"Each fed animal +{(run.ScoringVersion >= 12 ? 3 : 4)} · green source used +{(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)}\nsquare/shop source used +1 · waste source +0" +
                                (run.HasTrafficPenaltyScore ? $" · crash −{run.CollisionPenalty}" : ""),
                            $"每只吃到食物的动物 +{(run.ScoringVersion >= 12 ? 3 : 4)} · 实际供食绿地 +{(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)}\n广场/超市供食 +1 · 垃圾食源 +0" +
                                (run.HasTrafficPenaltyScore ? $" · 车祸−{run.CollisionPenalty}" : ""))
                    : run.ScoringVersion >= 7
                        ? L("Nearby food: green +1 · square +4\nshop +3 · waste +1" +
                                (run.HasTrafficPenaltyScore ? " · crash −3" : ""),
                            "附近食源：绿地+1 · 广场+4\n超市+3 · 垃圾+1" +
                                (run.HasTrafficPenaltyScore ? " · 车祸−3" : ""))
                        : L("Green +3 · urban +2 · waste +1" +
                                (run.HasTrafficPenaltyScore ? " · crash −3" : "") +
                                "\nWildlife may return from day 3 with food.",
                            "绿地+3 · 城市食物+2 · 残食+1" +
                                (run.HasTrafficPenaltyScore ? " · 车祸−3" : "") +
                                "\n损失后第3天起，有栖地和食物可迁入。")
                : run.HasBalanceScore
                    ? L($"Cycle +1 · job +0–2 · diner +1\nHome: near +1 · green +{(run.ScoringVersion >= 11 ? 2 : 1)}\nstreet +2 · plaza +1",
                        $"循环+1 · 岗位+0~2 · 餐厅+1\n住宅：就近+1 · 绿邻+{(run.ScoringVersion >= 11 ? 2 : 1)}\n街区+2 · 广场+1")
                    : L("Work, eat and return home: +1 point.",
                        "上班、用餐并返家：+1 分。");
            if (run.IsFinished)
            {
                scorePlaqueForecastHeading.text = L("FINISHED", "本局结束");
                scorePlaqueForecast[0].text = L(
                    "Final tally shown above.", "最终得分见上方。");
                scorePlaqueForecast[0].rectTransform.anchoredPosition =
                    new Vector2(31f, -85f);
                scorePlaqueForecast[0].rectTransform.sizeDelta =
                    new Vector2(250f, 42f);
                for (var index = 1; index < scorePlaqueForecast.Length; index++)
                    scorePlaqueForecast[index].gameObject.SetActive(false);
                return;
            }
            scorePlaqueForecastHeading.text = L("STATUS", "当前状态");
            for (var index = 0; index < scorePlaqueForecast.Length; index++)
            {
                scorePlaqueForecast[index].gameObject.SetActive(true);
                scorePlaqueForecast[index].rectTransform.anchoredPosition =
                    new Vector2(-59f + 64f * index, -85f);
                scorePlaqueForecast[index].rectTransform.sizeDelta =
                    new Vector2(62f, 42f);
            }
            if (animal)
            {
                var ecology = run.PreviewEcologyDay();
                scorePlaqueForecast[0].text = L(
                    $"Squirrel\n{ecology.squirrelsFed}/{ecology.squirrelsPresent}",
                    $"松鼠\n{ecology.squirrelsFed}/{ecology.squirrelsPresent}");
                scorePlaqueForecast[1].text = L(
                    $"Pigeons\n{ecology.pigeonsFed}/{ecology.pigeonsPresent}",
                    $"鸽群\n{ecology.pigeonsFed}/{ecology.pigeonsPresent}");
                scorePlaqueForecast[2].text = L(
                    ecology.hedgehogPresent
                        ? $"Hedgehog\n{(ecology.hedgehogFed ? 1 : 0)}/1"
                        : "Hedgehog\n—",
                    ecology.hedgehogPresent
                        ? $"刺猬\n{(ecology.hedgehogFed ? 1 : 0)}/1"
                        : "刺猬\n未出现");
                scorePlaqueForecast[3].text = L(
                    $"Meals\n{ecology.AnimalMeals}",
                    $"进食\n{ecology.AnimalMeals}");
            }
            else
            {
                var human = run.PreviewHumanDay();
                scorePlaqueForecast[0].text = L(
                    $"Work\n{human.WorkedCount}/{human.ResidentCount}",
                    $"上班\n{human.WorkedCount}/{human.ResidentCount}");
                scorePlaqueForecast[1].text = L(
                    $"Meals\n{human.MealsEaten}/{human.ResidentCount}",
                    $"用餐\n{human.MealsEaten}/{human.ResidentCount}");
                scorePlaqueForecast[2].text = L(
                    $"Cycles\n{human.CompletedWorkCycles}/{human.ResidentCount}",
                    $"循环\n{human.CompletedWorkCycles}/{human.ResidentCount}");
                scorePlaqueForecast[3].text = L(
                    $"Plaza\n{human.SquareVisits}",
                    $"广场\n{human.SquareVisits}");
            }
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

        // The ecology still advances one internal tick per placed card. The
        // hand mode explains that tick as a settlement, not a calendar day.
        private string FillHandTiming(string copy)
        {
            if (run?.UsesFillHand != true || string.IsNullOrEmpty(copy))
                return copy;
            var normalized = copy
                .Replace("NEXT DAY", "NEXT SETTLEMENT")
                .Replace("TOMORROW", "NEXT SETTLEMENT")
                .Replace("Next day", "Next settlement")
                .Replace("next-day", "next-settlement")
                .Replace("Day ", "Settlement ")
                .Replace("every other day", "every other settlement")
                .Replace("each day", "each settlement")
                .Replace("per day", "per settlement")
                .Replace("/day", "/settlement")
                .Replace("daily", "per settlement")
                .Replace("tomorrow", "after the next placement")
                .Replace("today", "this settlement")
                .Replace("yesterday", "the previous settlement")
                .Replace("第10天", "第10次结算")
                .Replace("每天", "每次结算")
                .Replace("每日", "每次结算")
                .Replace("隔天", "隔次结算")
                .Replace("今日", "本次结算")
                .Replace("明日", "下次结算")
                .Replace("昨日", "上次结算")
                .Replace("次日", "下次结算")
                .Replace("/日", "/次结算");
            normalized = Regex.Replace(normalized, @"第\s*(\d+)\s*天",
                "第 $1 次结算");
            return Regex.Replace(normalized, @"\bday\s*(\d+)\b",
                "settlement $1", RegexOptions.IgnoreCase);
        }

        private void ShowTooltip(Sprite icon, string title, string details,
            Vector2 pointer, TooltipLocation location)
        {
            if (tooltipRect == null || !IsBoardCaseOpen ||
                finalResultPanel.activeSelf || hasPendingPlacement ||
                buildPaletteOpen && location == TooltipLocation.BoardInfo) return;
            HideTooltip();
            tooltipIcon.sprite = icon;
            tooltipIcon.gameObject.SetActive(icon != null);
            tooltipTitle.text = FillHandTiming(title);
            tooltipBody.text = string.IsNullOrWhiteSpace(details)
                ? string.Empty : NormalizeTooltipBody(
                    FillHandTiming(details.Trim()), Chinese);
            var bounds = tooltipCanvas.rect;
            var maxBodyHeight = Mathf.Min(620f,
                Mathf.Max(140f, bounds.height - 132f));
            tooltipBody.fontSize = 18;
            tooltipBody.rectTransform.sizeDelta =
                new Vector2(TooltipTextWidth, maxBodyHeight);
            // Leave space for SDF line metrics without shrinking the body.
            var bodyHeight = Mathf.Clamp(
                Mathf.Ceil(tooltipBody.preferredHeight) + 28f,
                20f, maxBodyHeight);
            var height = 62f + bodyHeight;
            tooltipRect.sizeDelta = new Vector2(TooltipWidth, height);
            tooltipAccent.rectTransform.anchoredPosition =
                new Vector2(0f, height * 0.5f - 2f);
            tooltipIcon.rectTransform.anchoredPosition =
                new Vector2(-148f, height * 0.5f - 28f);
            tooltipTitle.rectTransform.anchoredPosition =
                new Vector2(14f, height * 0.5f - 28f);
            tooltipBody.rectTransform.sizeDelta =
                new Vector2(TooltipTextWidth, bodyHeight);
            tooltipBody.rectTransform.anchoredPosition =
                new Vector2(14f, height * 0.5f - 52f - bodyHeight * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipCanvas, pointer, null, out var local);
            var halfHeight = height * 0.5f;
            var desiredPosition = location switch
            {
                TooltipLocation.BoardInfo => new Vector2(CaseLeafCenter,
                    Mathf.Max(-125f, -196f + halfHeight)),
                TooltipLocation.LeftLeaf when
                    goalOverviewBackdrop != null && goalOverviewBackdrop.gameObject.activeSelf =>
                    new Vector2(0f, Mathf.Max(-225f, -350f + halfHeight)),
                TooltipLocation.LeftLeaf => new Vector2(-CaseLeafCenter,
                    Mathf.Max(-225f, -350f + halfHeight)),
                _ => new Vector2(
                    Mathf.Clamp(local.x + 190f, bounds.xMin + 177f,
                        bounds.xMax - 177f),
                    Mathf.Clamp(local.y - 32f, bounds.yMin + halfHeight + 8f,
                        bounds.yMax - halfHeight - 8f))
            };
            tooltipRect.anchoredPosition = new Vector2(desiredPosition.x,
                Mathf.Clamp(desiredPosition.y,
                    bounds.yMin + halfHeight + 8f,
                    bounds.yMax - halfHeight - 8f));
            tooltipRect.gameObject.SetActive(true);
            tooltipRect.SetAsLastSibling();
        }

        private static string NormalizeTooltipBody(string value, bool chinese)
        {
            if (!chinese || string.IsNullOrEmpty(value)) return value;
            var normalized = new char[value.Length];
            var length = 0;
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] == ' ' &&
                    (index > 0 && IsHan(value[index - 1]) ||
                     index + 1 < value.Length && IsHan(value[index + 1])))
                    continue;
                normalized[length++] = value[index];
            }
            return new string(normalized, 0, length);
        }

        private static bool IsHan(char value) =>
            value >= '\u3400' && value <= '\u9fff';

        private void HideTooltip()
        {
            if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
            if (scorePlaque != null)
                scorePlaque.gameObject.SetActive(false);
        }

        private string RoomProvisionBrief(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None)
        {
            if (run?.UsesFillHand == true)
                return category switch
                {
                    ConstructionCategory.Green => planting switch
                    {
                        GreenPlanting.Oak => L("Mature oak · up to 1 acorn per settlement",
                            "成熟橡树 · 每次结算最多补 1 份橡果"),
                        GreenPlanting.Meadow => L("Meadow · up to 2 seeds every other settlement",
                            "草地 · 隔次结算最多补 2 份草籽"),
                        GreenPlanting.Shrub => L("Mature shrub · up to 1 food per settlement",
                            "成熟灌木 · 每次结算最多补 1 份食物"),
                        _ => L("Habitat and food depend on the plot",
                            "落点决定植被与食源")
                    },
                    ConstructionCategory.Residence => L("Home for 1 resident",
                        "容纳 1 名居民"),
                    ConstructionCategory.Restaurant => L("2 meal seats · 1 job",
                        "餐位 2 个 · 岗位 1 个"),
                    ConstructionCategory.Workshop => L("2 jobs",
                        "岗位 2 个"),
                    ConstructionCategory.Waste => run.UsesUncappedWaste
                        ? L("Clears nearby and road-linked waste", "清运周围及街道接通的垃圾")
                        : L("Clear up to 2 waste per settlement",
                            "每次结算最多清运 2 份垃圾"),
                    ConstructionCategory.Street => L("Connects travel routes",
                        "接通出行路线"),
                    ConstructionCategory.Supermarket => L("2 meal seats · 1 job",
                        "餐位 2 个 · 岗位 1 个"),
                    ConstructionCategory.Square => L("Visits can offer animal food",
                        "居民到访可形成动物食源"),
                    _ => string.Empty
                };
            return LegacyRoomProvisionBrief(category, planting);
        }

        private string LegacyRoomProvisionBrief(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None) => category switch
        {
            ConstructionCategory.Green => planting switch
            {
                GreenPlanting.Oak => run?.HasVariableGreenYield == true
                    ? L("1/day · 80% after day 9", "日限1份 · 第10天起80%")
                    : L("Mature: +1 acorn/day", "成熟后橡果 +1/日"),
                GreenPlanting.Meadow => run?.HasVariableGreenYield == true
                    ? L("2/2 days · 80% after day 9", "隔日限2份 · 第10天起80%")
                    : L("Grown: +2 seeds/2 days", "长成后草籽 +2/隔日"),
                GreenPlanting.Shrub => run?.HasVariableGreenYield == true
                    ? L("1/day · 80% after day 9", "日限1份 · 第10天起80%")
                    : L("Mature: +1 food/day", "成熟后食物 +1/日"),
                _ => L("Food by plot", "食源随地块变化")
            },
            ConstructionCategory.Residence => L("1 resident", "居民 1 人"),
            ConstructionCategory.Restaurant => L("Meals 2 · job 1", "餐食2·岗位1"),
            ConstructionCategory.Workshop => L("2 jobs/day", "岗位 2 个/日"),
            ConstructionCategory.Waste => run?.UsesUncappedWaste == true
                ? L("Clears nearby and road-linked waste", "清运周围及街道接通的垃圾")
                : L("Clear max 2/day", "清运至多2/日"),
            ConstructionCategory.Street => L("Connects routes", "连通出行路线"),
            ConstructionCategory.Supermarket => L("Meals 2 · job 1", "餐食2·岗位1"),
            ConstructionCategory.Square => run?.ScoringVersion >= 7
                ? L("Food on visit", "到访激活食源")
                : L("Visit: food +1", "到访后食物+1"),
            _ => L("No daily yield", "无直接日产")
        };

        private string RoomProvisionDetails(ConstructionCategory category,
            GreenPlanting planting = GreenPlanting.None) => category switch
        {
            ConstructionCategory.Green => (planting switch
            {
                GreenPlanting.Oak => L(
                    "Acorns: up to +1/day once mature; stock limit 3.",
                    "橡果：成熟后每天最多补充 1 份；储量上限 3。"),
                GreenPlanting.Meadow => L(
                    "Grass seeds: up to +2 every other day after growth; stock limit 2.",
                    "草籽：生长后隔天最多补充 2 份；储量上限 2。"),
                GreenPlanting.Shrub => L(
                    "Shrub food: up to +1/day once mature; stock limit 2.",
                    "灌木食物：成熟后每天最多补充 1 份；储量上限 2。"),
                _ => GreenPlacementAndWildlifeGuide()
            }) + (planting != GreenPlanting.None &&
                run?.HasVariableGreenYield == true ? L(
                " From day 10, each mature green plot produces on 80% of its eligible days. Stored food remains available.",
                " 第10天起，成熟绿地每个应产日有80%概率产出；存粮仍可供动物取食。") : "") +
                (planting != GreenPlanting.None &&
                 run?.HasLinkageScore == true ? run.ScoringVersion >= 7
                    ? L(" A completed-cycle home near mature green gains resident linkage +1 when this food area is available to wildlife.",
                        " 完成循环的住宅附近有成熟绿地，且该食源可供动物利用时，居民联动 +1。")
                    : L(" A completed-cycle home beside mature green gains resident linkage +1 if an animal eats there.",
                        " 成熟绿地紧邻完成循环的住宅，动物在此进食时，居民联动 +1。") : ""),
            ConstructionCategory.Residence => L(
                $"Houses 1 resident. After a full work-and-meal cycle, each settlement grants direct trips +1, a visit to neighbouring mature green +{(run?.ScoringVersion >= 11 ? 2 : 1)}, or short street trips +2 as applicable.",
                $"容纳 1 人。完成上班、用餐循环后，每次结算按条件加分：就近+1、到访周围成熟绿地+{(run?.ScoringVersion >= 11 ? 2 : 1)}、短街道+2。") +
                (run?.ScoringVersion >= 8 ? L(
                    " Two nearby wildlife species that fed today add +2 for this completed home; at most two homes earn this each day.",
                    " 周围八格内有两种今日已觅食的动物栖息地时，本户另得+2；每天最多2户。") : ""),
            ConstructionCategory.Restaurant => L(
                "1 job and 2 meal seats/day. Staff can use an open seat. After a full cycle: restaurant job +1 and restaurant meal +1 (supermarket meal +0).",
                    "每日 1 岗位、2 餐位；本店员工也可使用空餐位。\n完成循环后，餐厅上班额外 +1、餐厅用餐额外 +1 分（超市用餐 +0）。"),
            ConstructionCategory.Workshop => L(
                "2 reachable jobs/day. After a full work-and-meal cycle, a workshop job adds +2 points.",
                "每天最多提供 2 个岗位；居民须能到达。\n完成上班、用餐循环后，工作坊上班额外 +2 分。"),
            ConstructionCategory.Waste => run?.UsesUncappedWaste == true
                ? L("Clears all existing and newly produced waste in the eight surrounding plots or in rooms connected by continuous shared-edge streets. Cleanup may leave animal food. Animals can cross this room.",
                    "每天清运周围八格，或经连续共边街道接通的住宅和餐饮建筑中的全部垃圾，含当天新产生的垃圾。清运后可能留下动物食物；动物可通行。")
                : L("Clears up to 2 old waste portions/day in the eight surrounding plots or in rooms connected by continuous shared-edge streets. Cleanup may leave 1 animal-food scrap. Animals can cross this room.",
                    "每天最多清运周围八格，或经连续共边街道接通的住宅和餐饮建筑中的 2 份旧垃圾。清运后可能留下 1 份动物食物；动物可通行。"),
            ConstructionCategory.Street => L(
                "Connects homes to meals, jobs and waste collection over any distance when the street chain is unbroken. Animals can cross, but roadside wildlife faces traffic risk; the street provides no food or jobs.",
                "连续共边街道可跨距离接通住宅、餐食、岗位和垃圾清运。\n动物可通行，但临路有车祸风险。\n自身不直接产食或提供岗位。") +
                (run?.ScoringVersion >= 8 ? L(
                    " A resident-visited square within eight plots halves today's collision chance on this street.",
                    " 周围八格内有居民到访的广场，今天这条街道的车祸概率减半。") : ""),
            ConstructionCategory.Supermarket => L(
                "1 job and 2 meal seats/day. The job counts toward a full cycle. After a full cycle: supermarket job +0 and supermarket meal +0 (restaurant meal +1). Animals can cross. ",
                    "每日 1 岗位、2 餐位；超市上班计入循环。\n完成循环后，超市上班额外 +0、超市用餐额外 +0 分（餐厅用餐 +1）。动物可通行。\n") +
                MarketFoodRule() +
                (run?.HasLinkageScore == true ? run.HasFedAnimalScore
                    ? L($" If wildlife eats the food left here, each fed animal earns {(run.ScoringVersion >= 12 ? 3 : 4)} animal points; this supermarket adds 1 source point once. Nearby completed-cycle residents may also gain 2 linkage points.",
                        $" 动物实际吃到超市留下的食物时，每只得 {(run.ScoringVersion >= 12 ? 3 : 4)} 分动物分；超市本次供食再得 1 分。附近完成循环的居民还可能得 2 分联动分。")
                    : run.ScoringVersion >= 7
                    ? L(" Once a resident dines here, a nearby occupied wildlife habitat can use this food area for animal +3; a nearby resident who completed a cycle may gain linkage +2. Wildlife need not eat the portion to score.",
                        " 有居民在此用餐后，附近有动物的栖息格可从此食源获得动物 +3；附近完成循环的居民可得联动 +2。动物不必实际吃到才得分。")
                    : L(" If wildlife eats that diner's portion, resident linkage +2.",
                        " 用餐者留下的食物被动物吃到，居民联动 +2。") : ""),
            ConstructionCategory.Square => run?.HasFedAnimalScore == true
                ? L($"One resident who worked and ate may visit each settlement: resident +1 and food for nearby wildlife. Each animal that eats it gains {(run.ScoringVersion >= 12 ? 3 : 4)} animal points; this square adds 1 source point once. A completed visitor may gain 3 linkage points. Nearby streets have half their collision risk.",
                    $"每次结算最多 1 名已上班、用餐的居民到访：居民 +1，并为附近动物留下食物。动物实际吃到时，每只得 {(run.ScoringVersion >= 12 ? 3 : 4)} 分；广场供食再得 1 分。访客还可能得 3 分联动分，周围街道车祸风险减半。") +
                  (run.UsesFiniteFillDeck ? L(
                      "\nThe third square unlocks a fourth hand card; the sixth unlocks a fifth.",
                      "\n建成第 3 座广场解锁第 4 个手牌位；第 6 座解锁第 5 个。") : "")
                : run?.ScoringVersion >= 8
                ? L("One resident who worked and ate may visit each day: resident +1 and one animal-food area. An occupied animal habitat within eight plots can score +4 from it; a completed visitor may gain linkage +3. Each street within eight plots has half its normal collision chance that day. Animals can cross the square.",
                    "完成上班和用餐的居民每天可到访1人：居民+1，并形成动物食源。周围八格有动物栖息地时，动物+4，完成循环的访客可得联动+3。广场周围八格的街道当天车祸概率减半。动物可通行。")
                : run?.ScoringVersion >= 7
                ? L("One resident who worked and ate may visit each day: resident +1 and one animal-food area. An occupied animal habitat within eight plots can score +4 from it; nearby completed-cycle residents may gain linkage +3. Animals cross green, square, street, supermarket and waste.",
                    "完成上班和用餐的居民每天可到访1人：居民+1，并形成动物食源。周围八格有动物栖息地时，此食源使动物+4；附近完成循环的居民可得联动+3。动物可走绿地、广场、街道、超市和垃圾间。")
                : L("One resident who worked and ate can visit each day: +1 satisfaction and one temporary animal-food portion. Animals score +2 only if they eat it. Animal routes cross green, square, street, supermarket and waste; homes, workshops and restaurants block them.",
                    "完成上班和用餐的居民可到访，每座广场每天最多1人。到访：满意度+1，留下1份动物食物。动物实际吃到才得+2分。动物可走绿地、广场、街道、超市和垃圾间；住宅、工作坊、餐厅不可通行。") +
                    (run?.HasLinkageScore == true ? L(
                        " If wildlife eats the visitor's portion, resident linkage +3.",
                        " 到访者留下的食物被动物吃到，居民联动 +3。") : ""),
            _ => string.Empty
        };

        private string GreenPlacementAndWildlifeGuide() => run?.UsesGreenPatches == true
            ? L("PLACE: Share an edge with a built plot. Green plots sharing an edge form one habitat patch.\nWILDLIFE: The first meadow can show two visiting pigeons; they do not count or score. A patch with 2 mature meadows can house 2 resident pigeons; 3–4 meadows house 3, 5–6 house 4, and 7 or more house 5. Each patch with a mature shrub can house one hedgehog; 2 mature shrubs and reachable prey may support a fox. A mature oak patch houses 1–3 squirrels. Food determines survival.",
                "放置：贴已建格的上下左右一格；共边绿地组成一个栖息板块。\n动物：首块草地可出现2只访客鸽子，但不计数量和分数。每个板块有2块成熟草地可容纳2只常驻鸽子，3至4块容纳3只，5至6块容纳4只，7块以上容纳5只。每个有成熟灌木的板块可容纳1只刺猬；至少2块成熟灌木且有可达猎物时，可能容纳狐狸。成熟橡树板块可容纳1至3只松鼠。能否存活还取决于食物。")
            : L(
            "PLACE: Share an edge with a built plot: above, below, left or right. Corner contact does not connect. The first green plot must touch the central oak.\n" +
            "TERRAIN: First, meadow. Later, check the four edge neighbours in this order: non-green → meadow; central oak → oak; 2 green plots → shrub; meadow → oak; otherwise meadow. Hover an empty plot to preview.\n" +
            "WILDLIFE: Connected meadow brings 2 pigeons next day; each 2 more mature connected meadows can add 1. Each 2 mature oaks around the central oak can add 1 squirrel. Mature connected shrubs bring hedgehogs; 2 shrubs plus 3 pigeons may bring a fox.",
            "放置：贴已建格的上下左右一格，斜角不相连；教程首块贴中央橡树。\n" +
            "地形：首块是草地。之后按上下左右四格判断：贴非绿地→草地；贴中央橡树→橡树；贴2块绿地→灌木；贴草地→橡树；否则草地。悬停空格可预览。\n" +
            "动物：连通草地次日来2只鸽子；每多2块成熟连通草地，可再多1只。中央橡树旁每2株成熟橡树，可多1只松鼠。成熟连通灌木引来刺猬；2块灌木加3只鸽子，可能引来狐狸。");

        private static Color UtilizationColour(ConstructionUtilizationLevel level) =>
            level switch
            {
                ConstructionUtilizationLevel.Full => UsedGreen,
                ConstructionUtilizationLevel.Blocked => BlockedRed,
                _ => IdleAmber
            };

        private ConstructionUtilizationStatus TileUtilization(
            ConstructionTileData tile)
        {
            if (run == null || run.IsFinished) return default;
            return ConstructionTileUtilizationModel.Evaluate(tile, run,
                run.PreviewHumanDay(), run.PreviewEcologyDay(),
                run.PreviewWasteAvailability());
        }

        private string UtilizationDetails(ConstructionUtilizationStatus status,
            ConstructionCategory category)
        {
            if (status.Kind == ConstructionUtilizationKind.None) return string.Empty;
            var amount = status.Kind switch
            {
                ConstructionUtilizationKind.GreenFood => L(
                    $"Animal meals {status.Used}/{status.Available}",
                    $"动物取食 {status.Used}/{status.Available}"),
                ConstructionUtilizationKind.Resident => L(
                    $"Daily cycle {status.Used}/{status.Available}",
                    $"日常循环 {status.Used}/{status.Available}"),
                ConstructionUtilizationKind.SquareVisit => L(
                    $"Square visits {status.Used}/{status.Available}",
                    $"广场到访 {status.Used}/{status.Available}"),
                _ => category == ConstructionCategory.Workshop
                    ? L($"Jobs filled {status.Used}/{status.Available}",
                        $"岗位使用 {status.Used}/{status.Available}")
                    : L($"Meals served {status.Used}/{status.Available}",
                        $"供餐 {status.Used}/{status.Available}")
            };
            var reason = status.Reason switch
            {
                ConstructionUtilizationReason.FoodRemaining => L(
                    $"{status.Idle} portion(s) remain for a later day. Check animal routes and demand.",
                    $"余 {status.Idle} 份可留待后续。可查看动物路线与食物需求。"),
                ConstructionUtilizationReason.YieldLostAtCapacity => L(
                    "Stock is full; today's new food cannot be stored. Check demand and routes.",
                    "库存已满，今日新增食物无法储存。查看需求与路线。"),
                ConstructionUtilizationReason.NoFoodAvailable => L(
                    "No food available today. Check plant growth and tomorrow's yield.",
                    "今日暂无食物。查看植被生长与明日产量。"),
                ConstructionUtilizationReason.ResidentDeparted => L(
                    "This home is empty. Restore food and work access.",
                    "住宅已空置。检查餐食与工作路线。"),
                ConstructionUtilizationReason.HomeBlockedByWaste => L(
                    "Waste blocks this home. Add a waste room within range.",
                    "垃圾阻断住宅。可在清运范围内增设垃圾间。"),
                ConstructionUtilizationReason.ResidentMissedMeal => L(
                    "The resident has no meal. Check a reachable restaurant or supermarket.",
                    "居民缺少餐食。检查可达的餐厅或超市。"),
                ConstructionUtilizationReason.ResidentMissedWork => L(
                    "The resident has not worked. Check a reachable job.",
                    "居民未上班。检查可达的岗位。"),
                ConstructionUtilizationReason.SpareServiceCapacity => L(
                    $"{status.Idle} place(s) spare. Compare resident demand and routes.",
                    $"空闲 {status.Idle} 个名额。可查看居民需求与路线。"),
                ConstructionUtilizationReason.ServiceBlockedByWaste => L(
                    "Waste blocks this service. Clear the backlog nearby.",
                    "垃圾阻断服务。请清运附近积压。"),
                ConstructionUtilizationReason.UnreachableServiceDemand => L(
                    "Residents need this service but cannot reach it. Connect a street.",
                    "居民需要此服务，但路线不通。可用街道连通。"),
                ConstructionUtilizationReason.SquareNoVisitor => L(
                    "No resident has completed work and a meal to visit today.",
                    "今日尚无完成上班与用餐的居民到访。"),
                ConstructionUtilizationReason.SquareUnreachable => L(
                    "A resident is ready, but cannot reach this square. Check the street route.",
                    "已有居民完成循环，但到广场的路线不通；请检查街道。"),
                _ => string.Empty
            };
            var label = status.Level switch
            {
                ConstructionUtilizationLevel.Full => L("✓ Fully used", "✓ 充分利用"),
                ConstructionUtilizationLevel.Idle => L("· Capacity spare", "· 尚有余量"),
                ConstructionUtilizationLevel.Blocked => L("! Needs attention", "! 需要处理"),
                _ => L("Waiting", "等待产出")
            };
            return string.IsNullOrEmpty(reason)
                ? label + " · " + amount
                : label + " · " + amount + "\n" + reason;
        }

        private string BuiltRoomProvision(ConstructionTileData tile)
        {
            var description = RoomProvisionDetails(tile.category, tile.planting);
            var mealCapacity = ConstructionHumanModel.DailyMealCapacity(
                tile, run.BuiltTiles, run.UsesNeighborUpgrades);
            if (tile.category == ConstructionCategory.Restaurant && mealCapacity == 3)
                description = L(
                    "Terrace restaurant. Provides 1 job and 3 meal seats.",
                    "露台餐厅提供 1 个岗位和 3 个餐位。");
            if (run.UsesNeighborUpgrades &&
                ConstructionPlacementScore.IsGardenResidence(tile, run.BuiltTiles))
                description += "\n" + (run.UsesPlacementScore
                    ? L("Garden home · first upgrade +4 build points.",
                        "花园住宅 · 首次升级 +4 建造分。")
                    : L("Garden home · borders connected green space.",
                        "花园住宅 · 与连片绿地共边。"));
            if (tile.category is ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket)
            {
                var availability = run.PreviewWasteAvailability();
                if (availability.BlockedFoodServices.Contains(tile.id))
                {
                    var afterCleanup = availability.BacklogAfterCleanup
                        .TryGetValue(tile.id, out var remaining) ? remaining : 0;
                    return L(
                        $"CLOSED today: waste {run.WasteBacklog(tile.id)} → " +
                        $"{afterCleanup}/{ConstructionWasteModel.DisruptionThreshold} after cleanup. " +
                        $"Job 0/1 and meals 0/{mealCapacity}. " +
                        "A waste room can clear this nearby or through connected streets.",
                        $"今日停业：垃圾 {run.WasteBacklog(tile.id)}→" +
                        $"{afterCleanup}/{ConstructionWasteModel.DisruptionThreshold} 份（清运后）。" +
                        $"岗位 0/1、餐位 0/{mealCapacity}；" +
                        "垃圾间可清运周围八格，或通过连续街道接通的积压。") + "\n" + description;
                }
            }
            if (tile.category == ConstructionCategory.Green)
            {
                var stock = run.FoodStock(tile.id);
                var capacity = run.FoodCapacity(tile.id);
                var produced = tile.id == ConstructionBoardModel.StarterOakId &&
                    run.CurrentDay == 1 ? 0 : Mathf.Min(run.GreenDailyYield(tile.id),
                    Mathf.Max(0, capacity - stock));
                return GreenGrowthSummary(tile) + "\n" + L(
                    $"Food {stock}/{capacity}. Today: +{produced} food forecast" +
                    (run.HasVariableGreenYield && run.CurrentDay >= 10 ? " (today's yield decided)." : "."),
                    $"食物 {stock}/{capacity}；今日预计新增 +{produced} 份" +
                    (run.HasVariableGreenYield && run.CurrentDay >= 10 ? "（今日产出已确定）。" : "。")) +
                    "\n" + GreenHarvestRule(tile.planting) +
                    (run.UsesGreenUpgrades
                        ? "\n" + L(
                            $"Habitat level {run.GreenUpgradeLevel(tile.id)}/3 · {run.ConnectedGreenCount(tile.id)} edge-connected greens. Each higher level adds 1 mature harvest and storage.",
                            $"绿地等级 {run.GreenUpgradeLevel(tile.id)}/3 · 共边连成 {run.ConnectedGreenCount(tile.id)} 格。成熟后每升一级多产、存 1 份食物。")
                        : string.Empty) +
                    (run.GreenPatchFor(tile.id) is { } patch
                        ? "\n" + L(
                            $"Habitat patch {patch.Tiles.Count} plots · capacity: {patch.SquirrelCapacity} squirrels, {patch.PigeonCapacity} pigeons, {patch.HedgehogCapacity} hedgehog, {patch.FoxCapacity} fox. Food still determines survival.",
                            $"绿地板块 {patch.Tiles.Count} 格 · 容量：松鼠 {patch.SquirrelCapacity}、鸽子 {patch.PigeonCapacity}、刺猬 {patch.HedgehogCapacity}、狐狸 {patch.FoxCapacity}。能否存活仍取决于食物。")
                        : string.Empty) +
                    "\n" + GreenAnimalStatus(tile) +
                    "\n" + UtilizationDetails(TileUtilization(tile), tile.category);
            }
            if (tile.category == ConstructionCategory.Residence)
            {
                var resident = run.PreviewHumanDay().residents.FirstOrDefault(person =>
                    person.homeTileId == tile.id);
                var dailyStatus = run.HasResident(tile.id)
                    ? L($"Today: work {(resident?.worked == true ? 1 : 0)}/1; " +
                         $"meal {(resident?.ate == true ? 1 : 0)}/1.",
                        $"今日：上班 {(resident?.worked == true ? 1 : 0)}/1；" +
                         $"用餐 {(resident?.ate == true ? 1 : 0)}/1。")
                    : L("This home is empty.", "当前住宅空置。");
                var route = resident == null ? string.Empty : "\n" + L(
                    $"Work: {ServiceLabel(resident.workTileId)} ({resident.workStreetTiles} street plots used)\nMeal: {ServiceLabel(resident.mealTileId)} ({resident.mealStreetTiles} street plots used)",
                    $"上班：{ServiceLabel(resident.workTileId)}（经过街道{resident.workStreetTiles}格）\n用餐：{ServiceLabel(resident.mealTileId)}（经过街道{resident.mealStreetTiles}格）") +
                    "\n" + L(
                         $"Conditions: direct {(resident.routeBonus > 0 ? "✓" : "·")} · green {(resident.greenNeighbourBonus > 0 ? "✓" : "·")} · short street {(resident.shortStreetBonus > 0 ? "✓" : "·")}.\n" +
                         $"Plaza visit {(resident.visitedSquare ? "✓" : "·")}.",
                         $"条件：就近{(resident.routeBonus > 0 ? "✓" : "·")} · 绿邻{(resident.greenNeighbourBonus > 0 ? "✓" : "·")} · 短街道{(resident.shortStreetBonus > 0 ? "✓" : "·")}。\n" +
                         $"广场到访{(resident.visitedSquare ? "✓" : "·")}。");
                return RoomProvisionDetails(ConstructionCategory.Residence) + "\n" +
                    dailyStatus + route +
                    "\n" + UtilizationDetails(TileUtilization(tile), tile.category);
            }
            if (tile.category == ConstructionCategory.Square)
            {
                var visitors = run.PreviewHumanDay().residents.Count(person =>
                    person.visitedSquare && person.squareTileId == tile.id);
                var eaten = run.PreviewEcologyDay().urbanMealTileIds.Count(id =>
                    id == tile.id);
                return description + "\n" + L(
                    $"Visits {visitors}/1; temporary food +{visitors}.\nAnimals ate {eaten} portion(s).",
                    $"到访{visitors}/1；临时食物+{visitors}。\n临时食物已被动物吃{eaten}份。") +
                    "\n" + UtilizationDetails(TileUtilization(tile), tile.category);
            }
            if (tile.category is ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket or ConstructionCategory.Workshop)
            {
                if (tile.category == ConstructionCategory.Workshop)
                {
                    var completed = run.PreviewHumanDay().residents.Any(person =>
                        person.workTileId == tile.id && person.completedCycle);
                    var assigned = run.PreviewHumanDay().residents.Any(person =>
                        person.workTileId == tile.id);
                    return description + "\n" + UtilizationDetails(TileUtilization(tile), tile.category) +
                        "\n" + L(
                            completed
                                ? "✓ A resident completes work, meal and return."
                                : assigned
                                    ? "× Assigned resident cannot complete the full cycle."
                                    : "· No resident assigned to this job yet.",
                            completed
                                ? "✓ 有居民完成上班、用餐及返家。"
                                : assigned
                                    ? "× 已有居民上班，但尚未完成完整循环。"
                                    : "· 这个岗位尚无居民上班。");
                }
                var human = run.PreviewHumanDay();
                var jobs = human.residents.Count(person => person.workTileId == tile.id);
                var meals = human.residents.Count(person => person.mealTileId == tile.id);
                var workerCompleted = human.residents.Any(person =>
                    person.workTileId == tile.id && person.completedCycle);
                return description +
                    (tile.category == ConstructionCategory.Supermarket
                        ? "\n" + MarketFoodSummary(tile) : string.Empty) + "\n" + L(
                    $"Today: jobs {jobs}/1; meals {meals}/{mealCapacity}.\nThe meal badge tracks seats only.",
                    $"今日岗位{jobs}/1；餐位{meals}/{mealCapacity}。\n餐位角标只统计餐位使用。") +
                    "\n" + L(
                        $"{(workerCompleted ? "✓" : "×")} " +
                        (workerCompleted ? "worker completed the full cycle."
                            : "no worker completed the full cycle."),
                        $"{(workerCompleted ? "✓" : "×")} " +
                        (workerCompleted ? "有员工完成完整循环。" : "暂无员工完成完整循环。")) +
                    "\n" + UtilizationDetails(TileUtilization(tile), tile.category);
            }
            if (tile.category == ConstructionCategory.Waste)
            {
                var collected = run.PreviewWasteAvailability().CollectedByRoom
                    .TryGetValue(tile.id, out var amount) ? amount : 0;
                return description + "\n" + (run.UsesUncappedWaste
                    ? L($"Existing waste cleared before today's activity: {collected}. New nearby waste is cleared during settlement.",
                        $"今日活动前预计清运旧垃圾 {collected} 份；附近新产生的垃圾在结算时清运。")
                    : L($"Today: forecast {collected}/{ConstructionWasteModel.CleanupCapacity} old waste portions cleared.",
                        $"今日预计清运旧垃圾 {collected}/{ConstructionWasteModel.CleanupCapacity} 份。"));
            }
            if (tile.category == ConstructionCategory.Street)
            {
                var slow = run.UsesNeighborUpgrades &&
                    ConstructionPlacementScore.IsSlowStreet(tile, run.BuiltTiles);
                var odds = slow ? ConstructionTrafficModel.ProtectedChanceDenominator :
                    ConstructionTrafficModel.ChanceDenominator;
                return description + "\n" + L(
                    $"Cumulative traffic deaths {run.StreetTrafficDeathCount(tile.id)}.\n" +
                    $"Roadside wildlife {run.StreetAdjacentWildlifeCount(tile.id)}: animals on adjacent green plots.\n" +
                    $"Each exposed animal has a 1-in-{odds} collision chance per turn here. " +
                    (slow ? "A nearby visited plaza provides the same protection. " :
                        "A nearby visited plaza can halve this risk. ") +
                    "At most one traffic death occurs per turn across the board.",
                    $"累计车祸死亡 {run.StreetTrafficDeathCount(tile.id)} 只；临路动物 {run.StreetAdjacentWildlifeCount(tile.id)} 只，来自周围八格的绿地。\n" +
                    $"每只临路动物在此街道本回合的遇险概率为 1/{odds}。" +
                    (slow ? "到访广场提供同等保护，两个效果取其一。" :
                        "附近有居民到访的广场可让风险减半。") +
                    "全棋盘每回合最多发生 1 起致死车祸。");
            }
            return description;
        }

        private string CategoryHoverDetails(ConstructionCategory category)
        {
            if (run == null) return string.Empty;
            var planting = category == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var plots = AvailablePlotCount(category, planting);
            var availability = run.IsIntroPhase && run.IsIntroTypeUsed(category)
                ? L("Complete. Finish the remaining steps to reopen it.",
                    "已完成。八类齐后可以再次建造。")
                : run.IsIntroPhase && run.IntroRequiredCategory != category
                ? L($"Locked for now. Build {CategoryLabel(run.IntroRequiredCategory.Value)} next.",
                    $"暂未轮到。下一步建{CategoryLabel(run.IntroRequiredCategory.Value)}。")
                : run.IsCategoryAtLimit(category)
                ? L("Type limit reached", "此类建筑已达上限")
                : run.IsUnlocked(category)
                ? plots > 0 ? run.BuiltToday
                    ? L($"Tomorrow: {plots} plot(s). Click Next day.",
                        $"明日可放：{plots} 处。请点“下一天”。")
                    : L($"Valid plots: {plots}", $"可放置：{plots} 处")
                    : PlacementPrerequisite(category)
                : L("Available after the oak lesson", "完成橡树观察后可选");
            var quota = run.IsIntroPhase
                ? L($"Room tour {run.IntroTypesBuilt}/8 · this type " +
                    (run.IsIntroTypeUsed(category) ? "1/1" : "0/1"),
                    $"房间体验 {run.IntroTypesBuilt}/8 · 此类" +
                    (run.IsIntroTypeUsed(category) ? "1/1" : "0/1"))
                : IsBoardGame
                ? L($"Built {run.CategoryBuiltCount(category)}/{run.CategoryLimit(category)}",
                    $"已建 {run.CategoryBuiltCount(category)}/{run.CategoryLimit(category)}")
                : L($"Built {run.CategoryBuiltCount(category)} · no type limit",
                    $"已建 {run.CategoryBuiltCount(category)} · 无类型上限");
            // During the eight-room introduction, the hovered build card
            // repeats today's plain-language lesson. Detailed production
            // numbers remain available on the placed room afterward.
            var details = category == ConstructionCategory.Green
                ? GreenPlacementAndWildlifeGuide()
                : run.IsIntroPhase && !run.BuiltToday &&
                  run.IntroRequiredCategory == category
                    ? StoryInstruction(run)
                    : RoomProvisionDetails(category, planting);
            return $"{details}\n{quota}\n{availability}";
        }

        private string ServiceLabel(string tileId)
        {
            var tile = run.BuiltTiles.FirstOrDefault(item => item.id == tileId);
            return tile == null ? L("none", "无") : CategoryLabel(tile.category);
        }

        private void RefreshFixedInfoCard()
        {
            // The former right-side backing still stores the hidden feedback
            // text used elsewhere, but room explanations now belong to the
            // badge or cell being inspected on the board itself.
            if (feedbackPanel != null) feedbackPanel.gameObject.SetActive(false);
            if (run == null || roomBadgeTooltipRect == null ||
                hasPendingPlacement || residentDispatchOpen || run.IsFinished && !resultReviewOpen)
            {
                HideRoomBadgeTooltip();
                return;
            }
            if (hoveredLocalRisk && hoveredRiskAnchor != null &&
                !string.IsNullOrEmpty(predationRiskText))
            {
                ShowRoomBadgeTooltip(L("FOX RISK", "狐狸捕食风险"),
                    predationRiskText + " " + L(
                        "Click the red marker to locate it.",
                        "点击红色标记定位。"), hoveredRiskAnchor);
                return;
            }
            var badgeTile = hoveredBadgeKind.HasValue
                ? run.BuiltTiles.FirstOrDefault(tile =>
                    tile.id == hoveredBadgeTileId) : null;
            if (badgeTile != null)
            {
                var badge = RoomBadgeInfo(badgeTile, hoveredBadgeKind.Value);
                ShowRoomBadgeTooltip(badge.title, badge.body,
                    hoveredBadgeAnchor);
                return;
            }
            var hovered = run.BuiltTiles.FirstOrDefault(tile =>
                tile.id == hoveredInfoTileId);
            var clicked = run.BuiltTiles.FirstOrDefault(tile =>
                tile.id == selectedInfoTileId);
            if (hoveredEmptyColumn.HasValue && hoveredEmptyRow.HasValue)
            {
                var column = hoveredEmptyColumn.Value - viewMinColumn;
                var row = hoveredEmptyRow.Value - viewMinRow;
                if (column >= 0 && column < 7 && row >= 0 && row < 7)
                    ShowRoomBadgeTooltip(L("EMPTY PLOT", "空地"),
                        FixedEmptySummary(hoveredEmptyColumn.Value,
                            hoveredEmptyRow.Value),
                        cellBackgrounds[column, row].rectTransform);
                else HideRoomBadgeTooltip();
                return;
            }
            var room = hovered ?? clicked;
            if (room != null)
            {
                ShowRoomBadgeTooltip(TileLabel(room),
                    FixedRoomSummary(room), VisibleTile(room.id));
                return;
            }
            HideRoomBadgeTooltip();
        }

        private string TerraceProvisionBrief() =>
            L("3 meal seats · 1 job", "餐位 3 个 · 岗位 1 个");

        private string FixedRoomSummary(ConstructionTileData tile)
        {
            if (tile.category == ConstructionCategory.Green)
            {
                var stock = run.FoodStock(tile.id);
                var capacity = run.FoodCapacity(tile.id);
                var produced = tile.id == ConstructionBoardModel.StarterOakId &&
                    run.CurrentDay == 1 ? 0 :
                    Mathf.Min(run.GreenDailyYield(tile.id), Mathf.Max(0, capacity - stock));
                return GreenGrowthSummary(tile) + "\n" +
                    L($"Food {stock}/{capacity} · forecast +{produced}\n",
                        $"食物 {stock}/{capacity} · 今日预计新增 +{produced}\n") +
                    GreenHarvestRule(tile.planting);
            }
            if (tile.category == ConstructionCategory.Supermarket)
                return MarketFoodSummary(tile) + "\n" +
                    L("A resident meal leaves 1 portion today. Hungry wildlife with access can eat it and leave less waste.",
                        "当天有人用餐即留1份；未吃饱且能到达的动物可取食，吃掉后少留垃圾。");
            var terrace = run.UsesNeighborUpgrades &&
                ConstructionPlacementScore.IsTerraceRestaurant(tile, run.BuiltTiles);
            var provision = terrace ? TerraceProvisionBrief() :
                RoomProvisionBrief(tile.category, tile.planting);
            return L($"{GreenFoodName(tile)}{provision}.",
                $"{GreenFoodName(tile)}{provision}。");
        }

        private string GreenFoodName(ConstructionTileData tile) =>
            tile.category != ConstructionCategory.Green ? string.Empty :
            tile.planting switch
            {
                GreenPlanting.Oak => L("Acorns · ", "橡果 · "),
                GreenPlanting.Meadow => L("Grass seeds · ", "草籽 · "),
                _ => L("Shrub food · ", "灌木食物 · ")
            };

        private string FixedEmptySummary(int column, int row)
        {
            if (run.IsFinished)
                return L("This run has ended.", "本局已结束。");
            if (run.UsesFillHand)
            {
                if (hasPendingPlacement)
                    return L("Confirm or cancel the staged room first.",
                        "请先确认或取消待建房间。");
                if (run.BuiltToday)
                    return L("Settling this placement…", "正在结算这次放置……");
                if (run.BuiltCount == 0)
                    return column == 3 && row == 3
                        ? L("Place the central oak here. Click to preview.",
                            "中央橡树放在这里。点击预览。")
                        : L("The first oak belongs in the centre plot.",
                            "第一张橡树只能放在正中格。");
                if (!run.IsFillHandCardPlayable(selectedCategory))
                    return L("Select a playable card below.",
                        "从下方选择一张可用手牌。");
            }
            if (!run.UsesFillHand && !IsEndless && run.CurrentDay == 1)
                return L("Inspect the central oak, then press Next day. " +
                    "Building starts on Day 2.",
                    "先查看中央橡树，再点“下一天”；第2天开始建造。");
            if (run.BuiltToday)
                return L("One room has been built today. Press Next day " +
                    "before building another.",
                    "今天已建造一格；点“下一天”后才能继续。");
            if (hasPendingPlacement)
                return L("Confirm or cancel the staged room first.",
                    "请先确认或取消待建房间。");
            if (AwaitingFreeBuildChoice)
                return run.UsesFillHand
                    ? L("Choose a card or a basic resource below, then preview a plot.",
                        "先点击下方手牌或基础资源，再预览地块。")
                    : run.IsIntroPhase &&
                    run.IntroRequiredCategory == ConstructionCategory.Green
                    ? L("Choose Green in Build, then a plot directly beside " +
                        "the central oak.",
                        "先选绿地，再选中央橡树上下左右四格中的一格。")
                    : L("Choose a room in Build before selecting a plot.",
                        "先在建造栏选择房间，再选择位置。");
            var planting = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var failure = run.CheckPlacement(column, row, selectedCategory,
                planting, ResidenceKind.Standard);
            return failure == ConstructionPlacementFailure.None
                ? selectedCategory == ConstructionCategory.Green
                    ? GreenPlotPreviewSummary(column, row, planting)
                    : L($"{SelectedBuildLabel()} can be built here. " +
                        "Click to preview, then confirm.",
                        $"这里可建{SelectedBuildLabel()}；点击预览，再确认。")
                : PlacementMessage(failure);
        }

        private string GreenPlotPreviewSummary(int column, int row,
            GreenPlanting planting)
        {
            var terrain = planting == GreenPlanting.None
                ? run.ResolveGreenPlanting(column, row) : planting;
            var starter = run.UsesFillHand && run.BuiltCount == 0;
            return L($"Forms {PlantingLabel(terrain)} here · ",
                    $"此处形成：{PlantingLabel(terrain)} · ") +
                (starter ? L("already mature", "已成熟") :
                    L($"matures on day {run.CurrentDay + 2}",
                        $"第 {run.CurrentDay + 2} 天成熟")) +
                "\n" + (planting == GreenPlanting.None
                    ? GreenPlantingReason(column, row) :
                    L("Uses the chosen plant type.", "按选定的植物类型放置。")) +
                "\n" + GreenHarvestRule(terrain);
        }

        private string GreenPlantingReason(int column, int row)
        {
            if (run.UsesFillHand && run.BuiltCount == 0)
                return L("The opening plot is the central oak.", "开局第一格为中央橡树。");
            if (!run.HasConnectedMeadow)
                return L("First establish a meadow reachable from the central oak.",
                    "先形成能与中央橡树接通的草地。");
            var adjacent = run.BuiltTiles.Where(tile =>
                ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row,
                    column, row)).ToArray();
            if (adjacent.Any(tile => tile.category != ConstructionCategory.Green))
                return L("An edge touches a building or street: meadow.",
                    "上下左右贴建筑或街道，优先形成草地。");
            if (ConstructionBoardModel.AreCardinalNeighbours(column, row,
                ConstructionBoardModel.StarterColumn, ConstructionBoardModel.StarterRow))
                return L("An edge touches the central oak: oak.",
                    "上下左右贴中央橡树，形成橡树。");
            if (adjacent.Length >= 2)
                return L("At least two green edges, no buildings: shrub.",
                    "上下左右贴至少两格绿地，且不贴建筑，形成灌木。");
            return adjacent.Any(tile => tile.planting == GreenPlanting.Meadow)
                ? L("An edge touches meadow, no buildings: oak.",
                    "上下左右贴草地，且不贴建筑，形成橡树。")
                : L("Other green-edge locations form meadow.",
                    "其余贴绿地的位置形成草地。");
        }

        private string GreenGrowthSummary(ConstructionTileData tile)
        {
            if (run.GreenGrowthStage(tile.id) == 2)
                return L("Mature · habitat ready", "已成熟 · 可计入栖息容量");
            var matureDay = Math.Max(1, tile.builtDay) + 2;
            var state = run.GreenGrowthStage(tile.id) == 0
                ? L("Newly planted", "新植") : L("Growing", "生长中");
            return L($"{state} · matures on day {matureDay}",
                $"{state} · 第 {matureDay} 天成熟");
        }

        private string GreenHarvestRule(GreenPlanting planting) =>
            planting == GreenPlanting.Meadow
                ? L("Meadow seeds from the next day, then every other day; habitat counts when mature.",
                    "草地次日开始隔天产籽；成熟后才计入栖息容量。")
                : L("Produces food once mature. Habitat level does not change the maturity date.",
                    "成熟后开始产食；连片等级不改变成熟日期。");

        private string MarketFoodRule() => L(
            "One or more resident meals leave 1 temporary wildlife portion today. Hungry animals with access may eat it after natural food; eating it removes 1 waste portion. No overnight stock.",
            "当天有居民用餐，留下1份临时动物食物。动物先吃自然食物，仍未吃饱且能到达时可取食；吃掉后少留1份垃圾，不作隔夜存粮。");

        private string MarketFoodSummary(ConstructionTileData tile)
        {
            var human = run.IsFinished ? run.LastHumanDay : run.PreviewHumanDay();
            var available = human?.residents.Any(person =>
                person.ate && person.mealTileId == tile.id) == true ? 1 : 0;
            var ecology = run.IsFinished ? run.LastEcologyDay : run.PreviewEcologyDay();
            var eaten = ecology?.urbanMealTileIds?.Count(id => id == tile.id) ?? 0;
            return run.IsFinished
                ? L($"Last turn's wildlife food {available} · eaten {eaten}/{available} · waste reduced by {eaten}",
                    $"末次结算动物食物 {available} 份 · 已取食 {eaten}/{available} · 少留垃圾 {eaten} 份")
                : L($"Today's wildlife food {available} · forecast eaten {eaten}/{available} · waste reduced by {eaten}",
                    $"今日动物食物 {available} 份 · 预计取食 {eaten}/{available} · 少留垃圾 {eaten} 份");
        }

        private string FixedRoomDetails(ConstructionTileData tile) =>
            tile.category switch
            {
                ConstructionCategory.Green => L(
                    $"{RoomProvisionBrief(tile.category, tile.planting)}. " +
                    $"Food {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)} · " +
                    GreenGrowthSummary(tile) + ".\n" +
                    FixedGreenReturnSummary(tile.planting),
                    $"{RoomProvisionBrief(tile.category, tile.planting)}。" +
                    $"食物 {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)} · " +
                    GreenGrowthSummary(tile) + "。\n" +
                    FixedGreenReturnSummary(tile.planting)),
                ConstructionCategory.Residence => L(
                    "Houses 1 resident. Work and food are directly reachable across a shared edge, or through connected streets; diagonal contact alone does not connect.",
                    "容纳1人。上下左右共边的岗位和餐食可直达，远处须经连续街道；斜角不连通。"),
                ConstructionCategory.Restaurant => L(
                    $"1 job and {ConstructionHumanModel.DailyMealCapacity(tile, run.BuiltTiles, run.UsesNeighborUpgrades)} meal seats per day. A completed resident cycle " +
                    "can gain restaurant work and meal bonuses.",
                    $"每日 1 个岗位、{ConstructionHumanModel.DailyMealCapacity(tile, run.BuiltTiles, run.UsesNeighborUpgrades)} 个餐位。居民完成循环后，可获得餐厅上班与用餐加分。"),
                ConstructionCategory.Workshop => L(
                    "2 jobs per day. A resident must also find food to " +
                    "complete the work-and-meal cycle.",
                    "每日2岗位。居民还须吃到饭，才算完成上班用餐循环。"),
                ConstructionCategory.Waste => run.UsesUncappedWaste
                    ? L("Clears all waste nearby or reached by continuous streets, including waste produced today. Cleanup may leave animal food.",
                        "每天清运周围八格及连续共边街道接通的全部垃圾，包括当天新产生的垃圾；清运后可能留下动物食物。")
                    : L("Clears up to 2 old waste portions per day nearby or through continuous streets. " +
                        "Cleanup may leave animal food.",
                        "每日清运周围八格及连续共边街道接通的旧垃圾，最多2份；清运后可能留下动物食物。"),
                ConstructionCategory.Street => L(
                    "Connects homes to jobs and meals, and waste rooms to waste sources. Wildlife can cross, " +
                    "but nearby animals face traffic risk.",
                    "连通住宅、岗位和餐食，也能让垃圾间清运远处的垃圾。动物可通行，但临路有车祸风险。"),
                ConstructionCategory.Supermarket =>
                    L("1 job and 2 meal seats per day. ", "每日1岗位、2餐位。") +
                    MarketFoodRule(),
                ConstructionCategory.Square => L(
                    "A resident who completed a cycle may visit. A visit " +
                    "leaves one temporary animal meal.",
                    "完成循环的居民可到访；到访后留下1份临时动物食物。"),
                _ => RoomProvisionBrief(tile.category, tile.planting)
            };

        private string FixedGreenReturnSummary(GreenPlanting planting) =>
            planting switch
            {
                GreenPlanting.Oak => L(
                    "Return from day 3 after loss: mature nearby oak and acorns.",
                    "回归：损失第3天起，需邻近成熟橡树和橡果。"),
                GreenPlanting.Meadow => L(
                    "Pigeon return from day 3: connected mature meadow and 2 days' seeds.",
                    "鸽群回归：损失第3天起，需连通成熟草地和连续2天草籽。"),
                _ => L(
                    "Return from day 3: mature connected shrubs and food.",
                    "动物回归：损失第3天起，需成熟连通灌木和食物。")
            };

        private (string title, string body) RoomBadgeInfo(
            ConstructionTileData tile, RoomBadgeKind kind)
        {
            switch (kind)
            {
                case RoomBadgeKind.Growth:
                    return (L("PLANT GROWTH", "绿地生长"),
                        GreenGrowthSummary(tile) + "\n" + GreenHarvestRule(tile.planting) +
                        "\n" + L("Habitat level increases storage and mature yield; it does not speed up growth.",
                            "连片等级增加储量和成熟后的产量，不会加快成熟。"));
                case RoomBadgeKind.HomeBonus:
                {
                    var person = run.PreviewHumanDay().residents.FirstOrDefault(
                        item => item.homeTileId == tile.id);
                    return (L("HOME BONUS", "住宅加分"), person == null
                        ? L("No resident here. Bonuses require " +
                            "a complete work-and-meal cycle.",
                            "目前没有居民；完成上班和用餐循环后才有住宅加分。")
                        : L($"Conditions: direct {(person.routeBonus > 0 ? "✓" : "·")} · " +
                            $"mature green {(person.greenNeighbourBonus > 0 ? "✓" : "·")} · " +
                            $"short street {(person.shortStreetBonus > 0 ? "✓" : "·")}. " +
                            "A full work-and-meal cycle is required.",
                            $"条件：就近{(person.routeBonus > 0 ? "✓" : "·")} · " +
                            $"成熟绿地{(person.greenNeighbourBonus > 0 ? "✓" : "·")} · " +
                            $"短街道{(person.shortStreetBonus > 0 ? "✓" : "·")}。" +
                            "完成上班用餐循环后生效。"));
                }
                case RoomBadgeKind.WorkCycle:
                    return (L("WORK CYCLE", "工作循环"), L(
                        "✓ Worker completed work and a meal; " +
                        "× assigned but incomplete; · no worker assigned.",
                        "✓员工上班并用餐；×已上班但循环未完成；·暂无员工。"));
                case RoomBadgeKind.TrafficDeaths:
                    return (L("TRAFFIC DEATHS", "车祸死亡"), L(
                        $"{run.StreetTrafficDeathCount(tile.id)} " +
                        "wildlife deaths recorded on this street so far.",
                        $"这条街道累计造成 {run.StreetTrafficDeathCount(tile.id)} 只动物死亡。"));
                case RoomBadgeKind.RoadsideWildlife:
                    return (L("WILDLIFE AT RISK", "临路动物"), L(
                        $"{run.StreetAdjacentWildlifeCount(tile.id)} " +
                        "animals on neighbouring green plots face traffic risk.",
                        $"相邻绿地有 {run.StreetAdjacentWildlifeCount(tile.id)} 只动物面临车祸风险。"));
                case RoomBadgeKind.Coexistence:
                    return CoexistenceBadgeInfo(tile);
                case RoomBadgeKind.Utilization:
                {
                    var use = TileUtilization(tile);
                    var label = tile.category switch
                    {
                        ConstructionCategory.Green => L("forecast meals", "预计取食"),
                        ConstructionCategory.Residence => L("complete cycles", "完整循环"),
                        ConstructionCategory.Square => L("resident visits", "居民到访"),
                        ConstructionCategory.Restaurant => L("seats used", "餐位使用"),
                        ConstructionCategory.Supermarket => L("seats used", "餐位使用"),
                        _ => L("jobs filled", "岗位使用")
                    };
                    return (L("TODAY'S USE", "今日使用"), L(
                        $"{label} {use.Used}/{use.Available}. " +
                        "Green = full; amber = underused; red = blocked.",
                        $"{label} {use.Used}/{use.Available}。" +
                        "绿=用满，橙=未充分使用，红=受阻。") +
                        UtilizationBlocker(use.Reason));
                }
                case RoomBadgeKind.Waste:
                    return (L("WASTE BACKLOG", "积压垃圾"), L(
                        $"{run.WasteBacklog(tile.id)} old waste portions. " +
                        "A waste room clears backlog nearby or through connected streets.",
                        $"积压 {run.WasteBacklog(tile.id)} 份旧垃圾；" +
                        "垃圾间可清运周围八格，或经连续街道接通的积压。"));
                case RoomBadgeKind.Food:
                    return tile.category switch
                    {
                        ConstructionCategory.Green => (L("FOOD STOCK", "食物存量"),
                            run.HasFedAnimalScore
                                ? L($"{run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)} portions stored. An animal must eat for animal points; a green plot actually used as food adds 2 once.",
                                    $"现有 {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)} 份食物。动物实际吃到才得动物分；绿地本次供食再加 2 分，只计一次。")
                                : run.ScoringVersion >= 7
                                ? L($"{run.FoodStock(tile.id)}/" +
                                    $"{run.FoodCapacity(tile.id)} portions stored. Available food near an occupied animal habitat scores; eating supports survival.",
                                    $"现有 {run.FoodStock(tile.id)}/" +
                                    $"{run.FoodCapacity(tile.id)} 份食物。附近有动物栖息地即可计分；进食关系到存活。")
                                : L($"{run.FoodStock(tile.id)}/" +
                                    $"{run.FoodCapacity(tile.id)} portions " +
                                    "stored. Animals score only when they eat.",
                                    $"现有 {run.FoodStock(tile.id)}/" +
                                    $"{run.FoodCapacity(tile.id)} 份食物；" +
                                    "动物吃到才得分。")),
                        ConstructionCategory.Waste => (L("FOOD AFTER CLEANUP", "清运后食物"),
                            L("Cleanup may leave one temporary " +
                                "portion for wildlife; ~ means possible.",
                                "清运后可能留下1份临时动物食物；~表示可能。")),
                        ConstructionCategory.Supermarket =>
                            (L("MARKET FOOD", "超市食源"),
                                MarketFoodSummary(tile) + "\n" + MarketFoodRule()),
                        _ => (L("VISIT FOOD", "到访食物"),
                            L("A completed-cycle resident's visit " +
                                "can leave one temporary animal meal.",
                                "完成循环的居民到访后，可留下1份临时动物食物。"))
                    };
                default:
                    return AnimalBadgeInfo(tile, kind);
            }
        }

        private (string title, string body) CoexistenceBadgeInfo(
            ConstructionTileData tile)
        {
            var human = run.IsFinished && run.LastHumanDay != null
                ? run.LastHumanDay : run.PreviewHumanDay();
            if (tile.category == ConstructionCategory.Street)
                return (L("SHARED SAFETY", "共栖安全"), L(
                    "A resident visited a square within the eight neighbouring plots. " +
                    "At today's settlement, each exposed animal's collision " +
                    "chance on this street falls from 1/16 to 1/32. Risk remains.",
                    "居民预计今天到访周围八格内的广场。结算时，这条街道旁" +
                    "每只动物的车祸概率从 1/16 降至 1/32，仍有风险。"));

            if (tile.category == ConstructionCategory.Residence)
            {
                var resident = human.residents.FirstOrDefault(person =>
                    person.homeTileId == tile.id);
                if (resident == null)
                    return (L("SHARED LIFE", "人与动物共栖"), string.Empty);
                var english = new List<string>();
                var chinese = new List<string>();
                if (resident.linkageBonus > 0)
                {
                    var source = run.BuiltTiles.FirstOrDefault(item =>
                        item.id == resident.linkageSourceTileId);
                    var name = source?.category switch
                    {
                        ConstructionCategory.Square => L("square", "广场"),
                        ConstructionCategory.Supermarket => L("market", "超市"),
                        _ => L("green plot", "绿地")
                    };
                    english.Add($"Shared {name} food links this home and wildlife.");
                    chinese.Add($"共享{name}食源连接本户与动物。");
                }
                if (resident.biodiversityBonus > 0)
                {
                    english.Add("Two nearby wildlife species have food available.");
                    chinese.Add("周围两种动物有可用食物。");
                }
                english.Add("Points appear after the full cycle settles.");
                chinese.Add("完成循环并结算后显示得分。");
                return (L("SHARED LIFE", "人与动物共栖"),
                    Chinese ? string.Join("\n", chinese) :
                        string.Join("\n", english));
            }

            var linked = human.residents.FirstOrDefault(person =>
                person.linkageSourceTileId == tile.id);
            return (L("SHARED FOOD AREA", "共享食源"), linked == null
                ? L("No resident and wildlife link is active on this route.",
                    "当前路线尚未形成居民与动物的联动。")
                : L("Residents and nearby wildlife can both use this food area. " +
                    "The result appears after settlement.",
                    "居民与附近动物都能利用这处食源；得分在结算后显示。"));
        }

        private string UtilizationBlocker(ConstructionUtilizationReason reason) =>
            reason switch
            {
                ConstructionUtilizationReason.NoFoodAvailable =>
                    L(" No food available.", " 缺少可取食物。"),
                ConstructionUtilizationReason.YieldLostAtCapacity =>
                    L(" Stock is full; no new food can grow.",
                        " 食物库存已满，无法再产食。"),
                ConstructionUtilizationReason.ResidentDeparted =>
                    L(" Resident left.", " 居民已离开。"),
                ConstructionUtilizationReason.HomeBlockedByWaste or
                    ConstructionUtilizationReason.ServiceBlockedByWaste =>
                    L(" Waste blocks service; clear it.", " 垃圾阻断服务，需清运。"),
                ConstructionUtilizationReason.ResidentMissedMeal =>
                    L(" Resident lacks a reachable meal.", " 居民缺少可达餐食。"),
                ConstructionUtilizationReason.ResidentMissedWork =>
                    L(" Resident lacks a reachable job.", " 居民缺少可达岗位。"),
                ConstructionUtilizationReason.UnreachableServiceDemand =>
                    L(" Demand cannot reach this room.", " 需求无法到达这里。"),
                ConstructionUtilizationReason.SquareUnreachable =>
                    L(" Residents cannot reach this square.", " 居民无法到达广场。"),
                ConstructionUtilizationReason.SquareNoVisitor =>
                    L(" No completed-cycle visitor.", " 暂无完成循环的访客。"),
                _ => string.Empty
            };

        private (string title, string body) AnimalBadgeInfo(
            ConstructionTileData tile, RoomBadgeKind kind)
        {
            var species = kind switch
            {
                RoomBadgeKind.Pigeon => ConstructionWildlifeSpecies.Pigeon,
                RoomBadgeKind.Hedgehog => ConstructionWildlifeSpecies.Hedgehog,
                RoomBadgeKind.Fox => ConstructionWildlifeSpecies.Fox,
                _ => ConstructionWildlifeSpecies.Squirrel
            };
            var name = kind switch
            {
                RoomBadgeKind.Pigeon => L("PIGEONS", "鸽子"),
                RoomBadgeKind.Hedgehog => L("HEDGEHOG", "刺猬"),
                RoomBadgeKind.Fox => L("FOX", "狐狸"),
                _ => L("SQUIRRELS", "松鼠")
            };
            var arrival = run.ReturnStatus(species);
            if (arrival.Pending && arrival.HabitatTileId == tile.id)
            {
                var blocker = !arrival.HabitatReady
                    ? species switch
                    {
                        ConstructionWildlifeSpecies.Squirrel => L(
                            " Needs a mature nearby oak.", " 需要成熟邻近橡树。"),
                        ConstructionWildlifeSpecies.Pigeon => L(
                            " Needs connected mature meadows.",
                            " 需要成熟连通草地。"),
                        ConstructionWildlifeSpecies.Hedgehog => L(
                            " Needs a mature connected shrub.",
                            " 需要成熟连通灌木。"),
                        _ => L(" Needs two mature connected shrubs.",
                            " 需要两块成熟连通灌木。")
                    }
                    : !arrival.FoodReady
                        ? species switch
                        {
                            ConstructionWildlifeSpecies.Pigeon => L(
                                " Needs seeds for two days.",
                                " 需要连续两天供足草籽。"),
                            ConstructionWildlifeSpecies.Fox => L(
                                " Needs reachable prey.",
                                " 需要可捕猎的动物。"),
                            _ => L(" Needs enough food.", " 需要足量食物。")
                        }
                        : string.Empty;
                return (name, L(
                    $"Amber = waiting to return, not yet counted. Earliest day " +
                    $"{arrival.EarliestDay}.{blocker}",
                    $"琥珀色“待”表示待迁入，尚未计数；最早第{arrival.EarliestDay}天迁入。" +
                    blocker) + (arrival.RescueDeadlineDay > 0
                    ? L($" Rescue by day {arrival.RescueDeadlineDay}.",
                        $" 救援截至第{arrival.RescueDeadlineDay}天结算前。")
                    : string.Empty));
            }
            var count = kind switch
            {
                RoomBadgeKind.Pigeon => run.PigeonPopulation,
                RoomBadgeKind.Hedgehog => 1,
                RoomBadgeKind.Fox => 1,
                _ => run.VisibleSquirrelHomeTiles(
                    run.UsesFillHand ? int.MaxValue :
                        ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                    .Count(home => home.id == tile.id)
            };
            return (name, L($"{count} animals based on this plot. " +
                "Follow the highlighted route to inspect food access.",
                $"本格栖居 {count} 只；选中绿地可查看取食路线。"));
        }

        private Sprite TileHoverIcon(int column, int row)
        {
            var tile = run?.At(column, row);
            return tile == null ? null : ConstructionIconCatalog.GetBuild(
                tile.category, tile.planting);
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
                if (run.UsesFillHand)
                    return $"{location}\n" + FixedEmptySummary(column, row);
                if (!IsEndless &&
                    run.StoryStage == ConstructionStoryStage.WatchSquirrelEat)
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
                if (AwaitingFreeBuildChoice)
                    return $"{location}\n" + (run.IsIntroPhase
                        ? run.IntroRequiredCategory == ConstructionCategory.Green
                            ? L("Select Green in Build, then choose one of the four edge-adjacent plots around the centre oak.",
                                "先在建造栏选择绿地，再选中央橡树上下左右四格中的一格。")
                            : L($"Select {CategoryLabel(run.IntroRequiredCategory.Value)} in Build first. You choose the plot.",
                                $"先在建造栏选择{CategoryLabel(run.IntroRequiredCategory.Value)}；地块由你决定。")
                        : L("Choose a room in Build first; you decide what belongs here.",
                            "先在建造栏选择房间；这里建什么由你决定。"));
                var planting = selectedCategory == ConstructionCategory.Green
                    ? selectedPlanting : GreenPlanting.None;
                var failure = run.CheckPlacement(column, row,
                    selectedCategory, planting, ResidenceKind.Standard);
                return $"{location}\n" + (failure == ConstructionPlacementFailure.None
                    ? L($"Click to preview {SelectedBuildLabel()}, then confirm.",
                        $"点击预览{SelectedBuildLabel()}，然后确认建造。") +
                        "\n" + RoomProvisionDetails(selectedCategory,
                            selectedCategory == ConstructionCategory.Green &&
                            planting == GreenPlanting.None
                                ? run.ResolveGreenPlanting(column, row) : planting)
                    : PlacementMessage(failure));
            }
            if (!IsEndless && run.CurrentDay == 1 &&
                tile.id == ConstructionBoardModel.StarterOakId)
            {
                var use = TileUtilization(tile);
                var foodUse = use.ShowOnTile
                    ? L($"Forecast eaten {use.Used}/{use.Available}",
                        $"今日预计取食 {use.Used}/{use.Available}")
                    : L("No food-use forecast yet",
                        "暂无食物使用预测");
                return L(
                    $"Growth {run.GreenGrowthStage(tile.id)}/2 · food {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)} (stock/limit)\n",
                    $"生长 {run.GreenGrowthStage(tile.id)}/2 · 食物 {run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)}（现有/上限）\n") +
                    foodUse + "\n" + GreenAnimalStatus(tile) + "\n" + L(
                        "Green = fully used · amber = spare · red = needs attention",
                        "绿=充分利用 · 橙=有余量 · 红=需处理");
            }
            var detail = BuiltRoomProvision(tile);
            var waste = run.WasteBacklog(tile.id);
            var cornerLegend = TileCornerLegend(tile);
            return (tile.id == predationRiskTileId ? predationRiskText + "\n" : "") +
                (string.IsNullOrEmpty(cornerLegend) ? "" : cornerLegend + "\n") +
                detail + (waste > 0
                    ? L($"\nWaste backlog {waste}",
                        $"\n积压垃圾 {waste}")
                    : string.Empty) + (tile.category == ConstructionCategory.Green
                    ? "\n" + GreenHabitatHint(tile) +
                      "\n" + GreenRecoveryHint(tile) : string.Empty) +
                "\n" + location;
        }

        private string TileCornerLegend(ConstructionTileData tile) =>
            tile.category switch
            {
                ConstructionCategory.Residence => L(
                    "Badges: home conditions · daily cycle · waste if present.",
                    "角标：住宅条件 · 日常循环 · 积压垃圾（如有）。"),
                ConstructionCategory.Restaurant or ConstructionCategory.Supermarket => L(
                    "Badges: worker's full cycle · meals used · waste if present.",
                    "角标：员工循环✓/× · 餐位使用 · 积压垃圾（如有）。"),
                ConstructionCategory.Workshop => L(
                    "Badges: worker's full cycle · jobs used.",
                    "角标：员工循环✓/× · 岗位使用。"),
                ConstructionCategory.Waste => L(
                    "~1: one possible temporary animal meal after cleanup.",
                    "~1：清运后可能留下1份动物食物。"),
                ConstructionCategory.Street => L(
                    "Badges: cumulative traffic deaths · wildlife at risk.",
                    "角标：累计车祸死亡 · 临路动物数量。"),
                ConstructionCategory.Square => L(
                    "Badges: resident visits · temporary animal food.",
                    "角标：居民到访 · 临时动物食物。"),
                _ => string.Empty
            };

        private string GreenAnimalStatus(ConstructionTileData tile)
        {
            var namesEnglish = new List<string>();
            var namesChinese = new List<string>();
            var localSquirrels = run.VisibleSquirrelHomeTiles(
                    run.UsesFillHand ? int.MaxValue :
                        ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .Count(home => home.id == tile.id);
            if (localSquirrels > 0)
            {
                namesEnglish.Add($"squirrels {localSquirrels} ({run.SquirrelPopulation} total)");
                namesChinese.Add($"松鼠 {localSquirrels} 只（全图 {run.SquirrelPopulation} 只）");
            }
            var localPigeons = run.VisiblePigeonHomeTiles(run.UsesFillHand
                    ? int.MaxValue : ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .Count(home => home.id == tile.id);
            if (localPigeons > 0)
            {
                var visualLimit = !run.UsesFillHand && run.PigeonPopulation >
                    ConstructionWorldView.MaxVisibleAnimalsPerSpecies;
                namesEnglish.Add(run.UsesFillHand
                    ? $"pigeons {localPigeons} ({run.PigeonPopulation} total)"
                    : $"pigeons {run.PigeonPopulation}" +
                      (visualLimit ? " (up to 8 animated)" : string.Empty));
                namesChinese.Add(run.UsesFillHand
                    ? $"鸽子 {localPigeons} 只（全图 {run.PigeonPopulation} 只）"
                    : $"鸽子 {run.PigeonPopulation} 只" +
                      (visualLimit ? "（画面最多展示8只）" : string.Empty));
            }
            if (tile.id == run.VisitorPigeonTile?.id)
            {
                namesEnglish.Add("2 visiting pigeons (not residents; no score)");
                namesChinese.Add("访客鸽子 2 只（未入住、不计分）");
            }
            if (run.HedgehogHabitats.Any(home => home.id == tile.id))
            {
                namesEnglish.Add("hedgehog 1");
                namesChinese.Add("刺猬 1 只");
            }
            if (tile.id == run.FoxHabitat?.id)
            {
                namesEnglish.Add("fox 1");
                namesChinese.Add("狐狸 1 只");
            }
            var awaitingHere = new[]
            {
                ConstructionWildlifeSpecies.Squirrel,
                ConstructionWildlifeSpecies.Pigeon,
                ConstructionWildlifeSpecies.Hedgehog,
                ConstructionWildlifeSpecies.Fox
            }.Any(species =>
            {
                var status = run.ReturnStatus(species);
                return status.Pending && status.HabitatTileId == tile.id;
            });
            return namesEnglish.Count == 0
                ? awaitingHere
                    ? L("No resident animal yet; amber icon = newcomer waiting.",
                        "本格无栖居动物；琥珀角标=待迁入。")
                    : L("No animal lives on this tile.", "本格无栖居动物。")
                : L("Animals based here: " + string.Join(" · ", namesEnglish),
                    "本格栖居" + string.Join(" · ", namesChinese));
        }

        private string GreenRecoveryHint(ConstructionTileData tile)
        {
            var kinds = tile.planting switch
            {
                GreenPlanting.Oak => new[] { ConstructionWildlifeSpecies.Squirrel },
                GreenPlanting.Meadow => new[] { ConstructionWildlifeSpecies.Pigeon },
                _ => new[] { ConstructionWildlifeSpecies.Hedgehog,
                    ConstructionWildlifeSpecies.Fox }
            };
            var pending = kinds.Select(kind => ReturnStatusHint(kind, tile.id))
                .Where(hint => !string.IsNullOrEmpty(hint)).ToArray();
            if (pending.Length > 0) return string.Join("\n", pending);
            return tile.planting switch
            {
                GreenPlanting.Oak => L(
                    "New squirrels may arrive 3 days after loss if nearby oak and acorns suffice.",
                    "松鼠损失后第3天起可迁入；需成熟邻近橡树与足量橡果。"),
                GreenPlanting.Meadow => L(
                    "New pigeons may arrive 3 days after loss if connected meadows feed them for 2 days.",
                    "鸽群损失后第3天起可迁入；需连通草地连续两天供足草籽。"),
                _ => L(
                    "New hedgehogs or foxes may arrive 3 days after loss if mature shrubs and food suffice.",
                    "刺猬或狐狸损失后第3天起可迁入；需成熟灌木与食物。")
            };
        }

        private string ReturnStatusHint(ConstructionWildlifeSpecies species,
            string tileId)
        {
            var status = run.ReturnStatus(species);
            if (!status.Pending || status.HabitatTileId != tileId)
                return string.Empty;
            var name = species switch
            {
                ConstructionWildlifeSpecies.Squirrel => L("Squirrel", "松鼠"),
                ConstructionWildlifeSpecies.Pigeon => L("Pigeon flock", "鸽群"),
                ConstructionWildlifeSpecies.Hedgehog => L("Hedgehog", "刺猬"),
                _ => L("Fox", "狐狸")
            };
            var daysLeft = Mathf.Max(0, status.EarliestDay - run.CurrentDay);
            var timing = daysLeft > 0
                ? L($"day {status.EarliestDay} earliest ({daysLeft} left)",
                    $"最早第{status.EarliestDay}天（还需{daysLeft}天）")
                : L($"from day {status.EarliestDay}",
                    $"第{status.EarliestDay}天起");
            var blocker = !status.HabitatReady
                ? species switch
                {
                    ConstructionWildlifeSpecies.Squirrel => L(
                        " · needs a mature nearby oak", "；需成熟邻近橡树"),
                    ConstructionWildlifeSpecies.Pigeon => L(
                        " · needs more mature connected meadows", "；需更多成熟连通草地"),
                    ConstructionWildlifeSpecies.Hedgehog => L(
                        " · needs a mature connected shrub", "；需成熟连通灌木"),
                    _ => L(" · needs 2 mature connected shrubs", "；需2块成熟连通灌木")
                }
                : !status.FoodReady
                ? species switch
                {
                    ConstructionWildlifeSpecies.Squirrel => L(
                        " · needs enough acorns", "；缺足量橡果"),
                    ConstructionWildlifeSpecies.Pigeon => L(
                        " · needs enough grass seed for 2 days", "；缺连续两天足量草籽"),
                    ConstructionWildlifeSpecies.Hedgehog => L(
                        " · needs shrub food", "；缺灌木食物"),
                    _ => L(" · needs reachable prey", "；缺可捕猎动物")
                }
                : string.Empty;
            var rescue = status.RescueDeadlineDay > 0
                ? L($" · rescue by end of day {status.RescueDeadlineDay}",
                    $"；救援至第{status.RescueDeadlineDay}天结算前")
                : string.Empty;
            return L($"New {name}: {timing}{blocker}{rescue}.",
                $"新{name}待迁入：{timing}{blocker}{rescue}。");
        }

        private string GreenHabitatHint(ConstructionTileData tile)
        {
            if (run.GreenFoodRouteTo(ConstructionBoardModel.StarterOakId,
                    tile.id).Count == 0)
                return L("Isolated green · wildlife cannot reach it",
                    "孤立绿地 · 动物无法到达");
            if (tile.planting == GreenPlanting.Meadow)
                return run.GreenGrowthStage(tile.id) == 0
                    ? L("Meadow growing · pigeons wait for seeds",
                        "草地生长中 · 鸽群等待草籽")
                    : L("Grass seeds can feed pigeons; animals based here are listed above.",
                        "草籽可供鸽群取食；本格栖居动物见上方。");
            if (run.GreenGrowthStage(tile.id) < 2)
                return L("Plants growing · food and shelter mature later",
                    "植物生长中 · 食物与庇护尚未成熟");
            return tile.planting switch
            {
                GreenPlanting.Oak => run.VisibleSquirrelHomeTiles(
                    run.SquirrelPopulation).Any(home =>
                    ConstructionBoardModel.GridDistance(tile.column,
                        tile.row, home.column, home.row) <= 1 &&
                    run.GreenFoodRouteTo(home.id, tile.id).Count > 0)
                    ? L("Oak food can feed squirrels; animals based here are listed above.",
                        "橡果可供松鼠取食；本格栖居动物见上方。")
                    : L("Oak food · outside squirrel range",
                        "橡果食源 · 不在松鼠活动范围内"),
                _ => L($"Shrub shelter · fox needs mature linked shrubs " +
                        $"{run.MatureConnectedShrubCount}/2 and pigeons {run.PigeonPopulation}/3.",
                    $"灌木可提供庇护；狐狸需要成熟相连灌木 " +
                        $"{run.MatureConnectedShrubCount}/2、鸽子 {run.PigeonPopulation}/3。")
            };
        }

        private static string ShortPreview(string value, int maximum)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var oneLine = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return oneLine.Length <= maximum ? oneLine :
                oneLine.Substring(0, maximum - 1).TrimEnd() + "…";
        }

        private string FeedbackSummary(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var oneLine = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            if (oneLine.Length <= 50) return oneLine;
            var sentenceEnd = oneLine.IndexOfAny(new[] { '.', '。', '!', '！', ';', '；' });
            var summary = sentenceEnd >= 12 && sentenceEnd < 58
                ? oneLine.Substring(0, sentenceEnd + 1)
                : ShortPreview(oneLine, 46);
            return ShortPreview(summary, 30) + "\n" +
                L("Hover for details", "悬停查看详情");
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

        // These cards reserve their right edge for the count badge. Keep the
        // full room name in the hover title while using a legible short label.
        private string BuildCardLabel(ConstructionCategory category) => category switch
        {
            ConstructionCategory.Residence => L("Home", "住宅"),
            ConstructionCategory.Restaurant => L("Diner", "餐厅"),
            ConstructionCategory.Waste => L("Waste", "垃圾间"),
            ConstructionCategory.Supermarket => L("Market", "超市"),
            _ => CategoryLabel(category)
        };

        private string ResidenceConditionLabel(ResidenceKind kind) => kind switch
        {
            ResidenceKind.Garden => L(
                $"Green visit +{(run?.ScoringVersion >= 11 ? 2 : 1)}",
                $"绿地到访 +{(run?.ScoringVersion >= 11 ? 2 : 1)}"),
            ResidenceKind.Urban => L("Short street +2", "街区 +2"),
            _ => L("Direct trips +1", "就近 +1")
        };

        private string ResidenceConditionRule(ResidenceKind kind) => kind switch
        {
            ResidenceKind.Garden => L(
                $"After a complete work-and-meal cycle, a resident visits mature green in one of the home's eight neighbouring plots and gains +{(run?.ScoringVersion >= 11 ? 2 : 1)}. This can combine with direct or short-street bonus.",
                $"完成上班、用餐循环后，居民到访住宅周围八格内的成熟绿地，得 +{(run?.ScoringVersion >= 11 ? 2 : 1)}；可与就近或短街道加分同时获得。"),
            ResidenceKind.Urban => L(
                "Automatic +2 each complete work-and-meal day when at least one trip uses a street and each outbound trip uses at most 2 street plots.",
                "每日完成上班、用餐，至少一程经过街道，且上班、用餐每程最多经过2格街道，自动+2。"),
            _ => L(
                "Automatic +1 each complete work-and-meal day when neither trip uses a street. Can combine with a mature green neighbour.",
                "每日完成上班、用餐，且两程都不经过街道，自动+1。可与绿邻加分同时获得。")
        };

        private bool CanSelectCategory(ConstructionCategory category) =>
            run != null && run.IsIntroCategoryAvailable(category) &&
            (!run.UsesFillHand || run.IsFillHandCardPlayable(category));

        private bool AwaitingFreeBuildChoice => run != null &&
            !run.IsFinished &&
            !run.BuiltToday &&
            (run.UsesFillHand ? !run.IsIntroPhase && !freeBuildChoiceMade :
            (IsEndless && !endlessChoiceMade ||
             !IsEndless && (run.IsIntroPhase && !introChoiceMade ||
             !run.IsIntroPhase && !freeBuildChoiceMade)));

        private string SelectedBuildLabel() => run?.UsesFillHand == true &&
            run.BuiltCount == 0
            ? L("central oak", "中央橡树")
            : selectedCategory == ConstructionCategory.Green
            ? selectedPlanting == GreenPlanting.None ? CategoryLabel(selectedCategory)
                : L($"{PlantingLabel(selectedPlanting)} green",
                    $"{PlantingLabel(selectedPlanting)}绿地")
            : CategoryLabel(selectedCategory);

        private int AvailablePlotCount(ConstructionCategory category,
            GreenPlanting planting)
        {
            if (run == null || run.IsFinished) return 0;
            if (run.Mode == ConstructionGameMode.Endless)
            {
                // Endless boards may span many rings. Only the shared-edge
                // frontier can be built, so scanning every empty cell would
                // make each UI refresh grow with the square of the map size.
                var frontier = new HashSet<(int column, int row)>();
                var directions = new[]
                {
                    (column: -1, row: 0), (column: 1, row: 0),
                    (column: 0, row: -1), (column: 0, row: 1)
                };
                foreach (var tile in run.BuiltTiles)
                foreach (var direction in directions)
                {
                    var column = (long)tile.column + direction.column;
                    var row = (long)tile.row + direction.row;
                    if (column < int.MinValue || column > int.MaxValue ||
                        row < int.MinValue || row > int.MaxValue) continue;
                    if (run.Contains((int)column, (int)row))
                        frontier.Add(((int)column, (int)row));
                }
                return frontier.Count(plot => run.CheckPlacement(plot.column,
                    plot.row, category, planting, ResidenceKind.Standard) ==
                    ConstructionPlacementFailure.None);
            }
            var count = 0;
            for (var row = run.MinCoordinate; row <= run.MaxCoordinate; row++)
            for (var column = run.MinCoordinate; column <= run.MaxCoordinate; column++)
                if (run.CheckPlacement(column, row, category, planting,
                    ResidenceKind.Standard) ==
                    ConstructionPlacementFailure.None)
                    count++;
            return count;
        }

        private string PlacementPrerequisite(ConstructionCategory category) => category switch
        {
            ConstructionCategory.Restaurant or ConstructionCategory.Workshop or
                ConstructionCategory.Supermarket =>
                L("Needs home/linked road", "需住宅/连通街道"),
            ConstructionCategory.Street =>
                L("Needs home/linked road", "需住宅/连通街道"),
            ConstructionCategory.Waste => L("Needs home/cafe/road", "需住宅/餐厅/连通路"),
            ConstructionCategory.Square when run?.IsIntroPhase == true =>
                L("Needs home/linked road", "需住宅/连通街道"),
            _ => L("Needs adjacent empty plot", "需相邻空地")
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
            residentDispatchOpen = false;
            buildPaletteOpen = !buildPaletteOpen;
            if (buildPaletteOpen) HideTooltip();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
        }

        public void OpenResidentDispatch()
        {
            if (run == null || run.IsFinished || hasPendingPlacement ||
                routeSubject != RouteSubject.Resident ||
                string.IsNullOrEmpty(selectedHomeId) ||
                !run.HasResident(selectedHomeId)) return;
            buildPaletteOpen = false;
            residentDispatchOpen = true;
            HideTooltip();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
        }

        public void CloseResidentDispatch()
        {
            if (!residentDispatchOpen) return;
            residentDispatchOpen = false;
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
            Refresh();
        }

        // This is a separate, unlimited operation. Each click applies to the
        // current day's preview immediately; building remains available.
        public bool CycleResidentDispatch(bool work, int direction)
        {
            if (!residentDispatchOpen || run == null || run.IsFinished ||
                direction == 0 || string.IsNullOrEmpty(selectedHomeId) ||
                !run.HasResident(selectedHomeId)) return false;
            var preference = run.GetResidentDispatch(selectedHomeId);
            var currentId = work ? preference?.workTileId : preference?.mealTileId;
            var options = (work ? run.DispatchWorkOptions(selectedHomeId) :
                    run.DispatchMealOptions(selectedHomeId))
                .OrderBy(tile => tile.row).ThenBy(tile => tile.column)
                .ThenBy(tile => tile.id, StringComparer.Ordinal).ToArray();
            var choices = new List<string> { null };
            choices.AddRange(options.Select(tile => tile.id));
            if (choices.Count == 1) return false;
            var index = choices.FindIndex(id => id == currentId);
            if (index < 0) index = 0;
            var next = (index + Math.Sign(direction) + choices.Count) % choices.Count;
            var workId = work ? choices[next] : preference?.workTileId;
            var mealId = work ? preference?.mealTileId : choices[next];
            if (!run.TrySetResidentDispatch(selectedHomeId, workId, mealId))
                return false;
            Save();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
            return true;
        }

        public bool ClearResidentDispatch()
        {
            if (!residentDispatchOpen || run == null || run.IsFinished ||
                string.IsNullOrEmpty(selectedHomeId) ||
                !run.HasResident(selectedHomeId)) return false;
            if (!run.TrySetResidentDispatch(selectedHomeId, null, null))
                return false;
            Save();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Refresh();
            return true;
        }

        private void RefreshPaletteVisibility()
        {
            if (run?.UsesFillHand == true) buildPaletteOpen = false;
            if (guidePanel != null)
                guidePanel.gameObject.SetActive(!buildPaletteOpen &&
                    !hasPendingPlacement);
            if (progressPanel != null)
                progressPanel.gameObject.SetActive(!buildPaletteOpen);
            if (endlessLossStrip != null)
                endlessLossStrip.SetActive(IsEndless && !buildPaletteOpen &&
                    run.EndlessLossRulesConfirmed && !run.IsFinished);
            if (paletteBackdrop != null)
            {
                paletteBackdrop.gameObject.SetActive(buildPaletteOpen &&
                    run?.UsesFillHand != true);
                if (buildPaletteOpen) paletteBackdrop.transform.SetAsLastSibling();
            }
            foreach (var button in categoryButtons.Values)
            {
                if (buildPaletteOpen) button.transform.SetAsLastSibling();
                var group = button.GetComponent<CanvasGroup>();
                if (group == null) continue;
                group.alpha = buildPaletteOpen && run?.UsesFillHand != true ? 1f : 0f;
                group.interactable = buildPaletteOpen && run?.UsesFillHand != true;
                group.blocksRaycasts = buildPaletteOpen && run?.UsesFillHand != true;
            }
            var showConditions = buildPaletteOpen &&
                selectedCategory == ConstructionCategory.Residence;
            if (paletteNotice != null)
                paletteNotice.gameObject.SetActive(buildPaletteOpen && !showConditions);
            if (residenceConditionHeading != null)
            {
                residenceConditionHeading.gameObject.SetActive(showConditions);
                if (showConditions) residenceConditionHeading.transform.SetAsLastSibling();
            }
            foreach (var card in residenceConditionCards.Values)
            {
                card.gameObject.SetActive(showConditions);
                if (!showConditions) continue;
                card.transform.SetAsLastSibling();
            }
            // The backdrop blocks clicks through the open palette. Keep the
            // persistent actions above it so Close is a real clickable control.
            if (buildPaletteOpen)
            {
                if (nextDayButton != null) nextDayButton.transform.SetAsLastSibling();
                if (buildPaletteButton != null)
                    buildPaletteButton.transform.SetAsLastSibling();
            }
            if (buildPaletteLabel != null)
                buildPaletteLabel.text = buildPaletteOpen
                    ? L("Close", "关闭") : L("Build", "建造");
            if (buildPaletteOpen && paletteNotice != null &&
                string.IsNullOrEmpty(paletteNotice.text))
                paletteNotice.text = L("Choose a building and a valid plot",
                    "选择建筑与符合条件的地块");
        }

        private string EndlessProgressLabel()
        {
            var (current, best, target) = run.EndlessCoexistenceProgress;
            return L($"Coexistence streak  {current}/{target}\nBest streak  {best} days",
                $"连续共栖  {current}/{target} 天\n最长纪录  {best} 天");
        }

        private void RefreshEndlessBuildingAdvice(
            HashSet<ConstructionCategory> playable, ConstructionHumanDayResult human,
            ConstructionEcologyDayResult wildlife, ConstructionWasteAvailability waste)
        {
            if (playable.Count == 0) return;
            var animalNeedsFood = wildlife.squirrelsFed < wildlife.squirrelsPresent ||
                wildlife.pigeonsFed < wildlife.pigeonsPresent ||
                wildlife.hedgehogPresent && !wildlife.hedgehogFed;
            var missingWork = human.WorkedCount < human.ResidentCount;
            var missingMeals = human.residents.Any(person => person.worked && !person.ate);
            var habitatRecovery = run.SquirrelPopulation == 0 ||
                run.PigeonArrivalDay > 0 && run.PigeonPopulation == 0;
            var category = ConstructionCategory.Green;
            var reason = L("connect habitat and food", "连通栖息地和食源");
            if (habitatRecovery && playable.Contains(ConstructionCategory.Green))
            {
                reason = L("restore food and habitat for wildlife", "恢复动物的食源和栖息地");
            }
            else if ((waste.BlockedHomes.Count > 0 || waste.BlockedFoodServices.Count > 0) &&
                playable.Contains(ConstructionCategory.Waste))
            {
                category = ConstructionCategory.Waste;
                reason = L("clear waste near blocked rooms", "清运受阻房间附近的垃圾");
            }
            else if (missingWork && !missingMeals && human.WorkedCount > 0 &&
                playable.Contains(ConstructionCategory.Workshop))
            {
                category = ConstructionCategory.Workshop;
                reason = L("add accessible jobs", "补充可达岗位");
            }
            else if ((missingWork || missingMeals) &&
                (playable.Contains(ConstructionCategory.Restaurant) ||
                 playable.Contains(ConstructionCategory.Supermarket)))
            {
                category = playable.Contains(ConstructionCategory.Restaurant)
                    ? ConstructionCategory.Restaurant : ConstructionCategory.Supermarket;
                reason = L("add jobs and meals", "补充岗位和餐位");
            }
            else if (!animalNeedsFood && human.ResidentCount == 0 &&
                playable.Contains(ConstructionCategory.Residence))
            {
                category = ConstructionCategory.Residence;
                reason = L("welcome a resident", "让居民入住");
            }
            else if (!animalNeedsFood &&
                human.residents.Any(person => person.completedCycle && !person.visitedSquare) &&
                playable.Contains(ConstructionCategory.Square))
            {
                category = ConstructionCategory.Square;
                reason = L("give residents a place to visit", "给居民留一个到访点");
            }
            if (!playable.Contains(category)) return;

            var label = CategoryLabel(category);
            paletteNotice.text = L($"Suggested: {label}", $"建议建造 · {label}");
            if (AwaitingFreeBuildChoice)
            {
                placementGuideText.text = L(
                    $"Try {label}: {reason}. Place one room to continue.",
                    $"建议：{label}，{reason}。放置一格后结算。");
                selectedBuildIcon.sprite = ConstructionHandArtCatalog.GetRoom(category) ??
                    ConstructionIconCatalog.GetBuild(category);
            }
        }

        private void RefreshEndlessLossStrip(ConstructionHumanDayResult preview)
        {
            if (endlessLossStrip == null || run == null) return;
            endlessLossStrip.SetActive(IsEndless && !buildPaletteOpen &&
                run.EndlessLossRulesConfirmed && !run.IsFinished);
            if (!endlessLossStrip.activeSelf) return;
            endlessDeathCaption.text = L("DEATHS · 7D", "死亡 · 7 天");
            endlessDeathValue.text = $"{run.RecentAnimalDeaths}/{run.AnimalDeathLimit}";
            endlessDeathValue.color = run.RecentAnimalDeaths >= run.AnimalDeathLimit
                ? BlockedRed
                : run.RecentAnimalDeaths + 1 >= run.AnimalDeathLimit
                    ? IdleAmber : UsedGreen;
            endlessUnemployedCaption.text = L("MISSED WORK", "未上班居民");
            var residents = $"{run.UnemployedResidentsLastDay}/{run.UnemployedResidentLimit}";
            var streak = $"{run.UnemployedDaysInRow}/" +
                ConstructionRunModel.UnemployedConsecutiveDays;
            endlessUnemployedValue.text = L($"{residents} · {streak} d",
                $"{residents} 人 · {streak} 天");
            endlessUnemployedValue.fontSize = 15;
            var atRiskToday = preview != null &&
                preview.ResidentCount - preview.WorkedCount >= run.UnemployedResidentLimit;
            endlessUnemployedValue.color = run.UnemployedDaysInRow >=
                ConstructionRunModel.UnemployedConsecutiveDays
                    ? BlockedRed
                    : run.UnemployedDaysInRow > 0 || atRiskToday
                        ? IdleAmber : UsedGreen;
        }

        private void RefreshStaticLabels()
        {
            if (boardGameCameraHeading != null)
                boardGameCameraHeading.text = L("CAMERA", "摄像头");
            titleText.text = run.UsesFillHand
                ? L("SYMBIOSIS:49 · THREE CARDS", "共栖：49 · 三张手牌")
                : IsBoardGame
                ? run.IsRandomBoardGame
                    ? L("TABLETOP DRAW · 49", "桌游抽牌 · 49 格")
                    : L("BOARD GAME · 49 DAYS", "桌游 · 49 天")
                : IsEndless
                    ? L("ENDLESS BUILD", "无尽建造")
                    : L("FILL · 49 DAYS", "填空 · 49 天");
            titleText.fontSize = run.UsesFillHand ? 18 : 23;
            menuLabel.text = L("Menu", "主菜单");
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
            if (endlessRulesButton != null)
                endlessRulesButton.gameObject.SetActive(IsEndless &&
                    !run.IsFinished);
            if (endlessRulesButtonLabel != null)
                endlessRulesButtonLabel.text = L("Rules", "规则");
            resultResetLabel.text = resetLabel.text;
            resultMenuLabel.text = L("Menu", "主菜单");
            nextDayLabel.text = L("Next day", "下一天");
            latestRoomLabel.text = L("Latest room", "回到最新房间");
            endlessScoreCaptions[0].text = run.UsesOverallScore
                ? L("TOTAL", "总分")
                : !run.HasBalanceScore || run.HasAdditiveCoexistenceScore
                    ? L("SHARED", "共栖") : L("OLD SHARED", "旧共栖");
            endlessScoreCaptions[1].text = L("ANIMAL", "动物");
            endlessScoreCaptions[2].text = L("RESIDENT", "居民");
            skipTutorialLabel.text = run.UsesFillHand
                ? L("Skip", "跳过")
                : run.TutorialTipsSkipped
                    ? L("Tips", "提示") : L("Skip", "跳过");
            skipTutorialButton.gameObject.SetActive(
                !IsEndless && !run.IsFinished &&
                (run.UsesFillHand ? run.IsIntroPhase && !run.BuiltToday
                    : run.CurrentDay <= 12));
            if (residenceConditionHeading != null)
                residenceConditionHeading.text = L(
                    "Automatic home bonuses · no choice needed",
                    "住宅自动加分 · 无需选择类型");
            foreach (var pair in residenceConditionCards)
                pair.Value.GetComponentInChildren<Text>().text =
                    pair.Key switch
                    {
                        ResidenceKind.Garden =>
                            L($"GREEN +{(run.ScoringVersion >= 11 ? 2 : 1)}\nMature visit",
                                $"绿地 +{(run.ScoringVersion >= 11 ? 2 : 1)}\n到访成熟绿地"),
                        ResidenceKind.Urban =>
                            L("STREET +2\nShort commute", "街区 +2\n短街道通勤"),
                        _ => L("DIRECT +1\nNo streets", "就近 +1\n两程无街道")
                    };
            var finishingIntroToday = !run.UsesFillHand && !IsEndless &&
                run.CurrentDay == 9 &&
                run.BuiltToday && run.IntroTypesBuilt == ConstructionTutorialGuide.RoomCount;
            todayHeading.text = run.IsFinished
                    ? run.EndReason == ConstructionEndReason.BoardComplete
                        ? L("COMPLETE", "建造完成")
                        : L("RUN ENDED", "本局结束")
                : run.UsesFillHand
                    ? run.IsIntroPhase
                        ? L($"FIXED HAND {run.IntroTypesBuilt + 1}/8",
                            $"固定手牌 {run.IntroTypesBuilt + 1}/8")
                        : L("GOAL PROGRESS", "目标牌进度")
                : IsEndless
                    ? L("ECOLOGY", "生态概览")
                : run.IsIntroPhase || finishingIntroToday
                    ? L($"TUTORIAL {run.IntroTypesBuilt}/8",
                        $"教程 {run.IntroTypesBuilt}/8")
                : run.TutorialChapter == ConstructionTutorialChapter.Complete
                    ? L("YOUR WORLD", "自由规划")
                    : run.TutorialChapter == ConstructionTutorialChapter.InspectOak
                        ? L("INTRO", "序章")
                        : L($"ROOM {(int)run.TutorialChapter:00}/{ConstructionTutorialGuide.RoomCount:00}",
                            $"房间 {(int)run.TutorialChapter:00}/{ConstructionTutorialGuide.RoomCount:00}");
            reviewHeading.text = run.UsesFillHand
                ? L("LAST PLACEMENT", "上次结算")
                : L("LAST DAY", "昨日结算");
            routeHeading.text = L("ROUTE", "路线检查");
            progressHeading.text = !IsEndless
                ? run.UsesFillHand
                    ? L("RUN SCORE", "本局得分")
                    : L("TOTAL PLOTS", "总建造格")
                : L("ROOMS BUILT", "已建房间");
            nextStepHeading.text = run.UsesFillHand
                ? L("Selected card", "当前手牌")
                : run.IsFinished
                ? L("RESULT", "结算结果")
                : IsEndless
                ? L("BUILD", "建造")
                : run.IsIntroPhase || finishingIntroToday
                ? run.BuiltToday
                    ? L($"BUILT {run.IntroTypesBuilt}/8",
                        $"已完成 {run.IntroTypesBuilt}/8")
                    : L($"BUILD {run.IntroTypesBuilt + 1}/8",
                        $"待建 {run.IntroTypesBuilt + 1}/8")
                : run.TutorialChapter == ConstructionTutorialChapter.Complete
                ? L("TODAY", "今日可做")
                : !run.BuiltToday &&
                run.TutorialChapter != ConstructionTutorialChapter.InspectOak &&
                !run.IsFinished
                ? L("CURRENT BUILD", "当前建造") : L("NEXT STEP", "下一步");
            RefreshPaletteVisibility();
            resultTitleLabel.text = run.UsesFillHand
                ? L("49-PLOT COEXISTENCE", "49 格共栖结算")
                : IsEndless
                ? L("ENDLESS COEXISTENCE", "无尽共栖结算")
                : IsBoardGame
                    ? L("BOARD GAME RESULTS", "桌游共栖结算")
                    : L("49-DAY COEXISTENCE", "49 天共栖结算");
            resultModeLabel.text = run.EndReason switch
            {
                ConstructionEndReason.AnimalDeathsLimit => L(
                    $"Day {run.CurrentDay} · animal death limit reached",
                    $"第 {run.CurrentDay} 天 · 动物死亡达到上限"),
                ConstructionEndReason.ResidentsUnemployed => L(
                    $"Day {run.CurrentDay} · residents missed work",
                    $"第 {run.CurrentDay} 天 · 居民连续未上班"),
                ConstructionEndReason.ResidentsGone => L(
                    $"Day {run.CurrentDay} · all residents left",
                    $"第 {run.CurrentDay} 天 · 居民全部离开"),
                ConstructionEndReason.WildlifeGone => L(
                    $"Day {run.CurrentDay} · wildlife disappeared",
                    $"第 {run.CurrentDay} 天 · 动物种群消失"),
                _ => L("All 49 plots have a place · see how their neighbours fared",
                    "49 格都有了归属 · 看看这里的人与动物过得怎样")
            };
            foreach (var pair in categoryButtons)
            {
                var label = pair.Value.GetComponentInChildren<Text>();
                label.text = BuildCardLabel(pair.Key);
                label.fontSize = Chinese ? 14 : 13;
                categoryProvisionLabels[pair.Key].fontSize = 13;
            }
            foreach (var pair in metricCaptions)
                pair.Key.text = pair.Value();
        }

        private void Refresh()
        {
            StopScoreMotion();
            if (run == null || dayText == null) return;
            SyncFillHandSelection();
            RefreshStaticLabels();
            var pendingPlot = PendingUpgradePlot();
            var upgradeTiles = ConstructionPlacementScore.NewlyUpgradedTiles(
                pendingPlot, run.BuiltTiles);
            var upgradesById = upgradeTiles.Select(tile => tile.id).ToHashSet();
            var projectedBoard = pendingPlot == null ? run.BuiltTiles :
                run.BuiltTiles.Append(pendingPlot).ToArray();
            var humanPreview = run.IsFinished && run.LastHumanDay != null
                ? run.LastHumanDay : run.PreviewHumanDay();
            var foxForecast = run.IsFinished ? null : run.PreviewEcologyDay();
            var wastePreview = run.PreviewWasteAvailability();
            predationRiskTileId = foxForecast?.predatedSpecies ==
                ConstructionPreySpecies.None ? null : foxForecast?.predationTileId;
            predationRiskText = string.IsNullOrEmpty(predationRiskTileId)
                ? null : L($"Fox risk: tomorrow a fox may hunt one " +
                    $"{PreyName(foxForecast.predatedSpecies, false)} at " +
                    $"{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), predationRiskTileId)}.",
                    $"狐狸风险：明日狐狸可能在" +
                    $"{TileCoordinate(run.BuiltTiles.ToDictionary(tile => tile.id), predationRiskTileId)}" +
                    $"捕食 1 只{PreyName(foxForecast.predatedSpecies, true)}。");
            if (focusedPredationTileId != predationRiskTileId)
                focusedPredationTileId = null;
            dayText.text = run.UsesFillHand
                ? L($"PLACED {run.BuiltCount} / {run.BoardCapacity}",
                    $"已放置 {run.BuiltCount} / {run.BoardCapacity}")
                : L($"DAY {run.CurrentDay}", $"第 {run.CurrentDay} 天");
            if (run.IsFinished)
                scoreText.text = L(
                    $"Animal points {run.AnimalScore} · Resident points {run.HumanScore}\n" +
                    "Run ended · final scores below",
                    $"动物得分 {run.AnimalScore} · 居民得分 {run.HumanScore}\n" +
                    "本局已结束 · 请查看最终得分");
            else
            {
                var ecologyPreview = foxForecast;
                var pigeonForecast = ecologyPreview.pigeonsPresent == 0
                    ? string.Empty
                    : L($"\nPigeons {ecologyPreview.pigeonsFed}/" +
                        $"{ecologyPreview.pigeonsPresent}",
                        $"\n鸽群 {ecologyPreview.pigeonsFed}/{ecologyPreview.pigeonsPresent}");
                scoreText.text = L(
                    $"Animal points {run.AnimalScore} · Resident points {run.HumanScore}\n" +
                    $"Next: Squirrel {ecologyPreview.squirrelsFed}/" +
                    $"{ecologyPreview.squirrelsPresent} · Work " +
                    $"{humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}" +
                    pigeonForecast +
                    (ecologyPreview.predatedSpecies == ConstructionPreySpecies.None
                        ? "" : $"\nFox may hunt 1 {PreyName(ecologyPreview.predatedSpecies, false)}"),
                    $"动物得分{run.AnimalScore} · 居民得分{run.HumanScore}\n" +
                    $"明日 松鼠{ecologyPreview.squirrelsFed}/{ecologyPreview.squirrelsPresent} · " +
                    $"通勤{humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}" +
                    pigeonForecast +
                    (ecologyPreview.predatedSpecies == ConstructionPreySpecies.None
                        ? "" : $"\n狐狸可能捕食 1 只{PreyName(ecologyPreview.predatedSpecies, true)}"));
            }
            animalMetricText.text = IsEndless
                ? CompactScore(run.AnimalScore, Chinese) : run.AnimalScore.ToString();
            humanMetricText.text = IsEndless
                ? CompactScore(run.HumanScore, Chinese) : run.HumanScore.ToString();
            animalMetricText.fontSize = humanMetricText.fontSize = IsEndless ? 16 : 22;
            UpdateFillScoreSummary(run.AnimalScore, run.HumanScore,
                run.BalanceScore, run.PlacementScore);
            RefreshDayReview();
            storyText.text = run.IsFinished
                ? L("This run has ended. Review the observed outcomes.",
                    "本局已结束，请查看实际结算结果。")
                : run.UsesFillHand
                    ? run.BuiltCount == 0
                        ? L("Click the centre plot to preview the oak, then confirm.",
                            "点击正中格预览中央橡树，再确认建造。")
                        : run.IsIntroPhase
                            ? L($"Next: {SelectedBuildLabel()}. Preview a lit plot, then confirm.",
                                $"下一张是{SelectedBuildLabel()}。点击亮格预览，再确认建造。")
                            : L("Choose any playable card. Preview a lit plot, then confirm.",
                                "选一张可用手牌，点击亮格预览，再确认建造。")
                : IsEndless
                    ? L("Build one plot before advancing the day. Keep food, jobs and meals connected.",
                        "每轮先建一格，再进入下一天。让食物、工作和餐食保持接通。")
                : IsBoardGame ? BoardGameInstruction() : StoryInstruction(run);
            storyPreviewText.text = run.IsFinished
                ? L("Run ended · review results", "本局结束 · 查看结果")
                : run.UsesFillHand
                    ? run.IsIntroPhase
                        ? run.TutorialTipsSkipped
                            ? L($"Fixed hand {run.IntroTypesBuilt + 1}/8",
                                $"固定手牌 {run.IntroTypesBuilt + 1}/8")
                            : TutorialRoomStory(TutorialLessonChapter(run),
                                run.BuiltCount == 0)
                        : L($"{SelectedBuildLabel()} · choose a lit plot",
                             $"{SelectedBuildLabel()} · 选高亮格")
                : IsEndless
                     ? EndlessProgressLabel()
                : IsBoardGame ? BoardGameShortInstruction()
                    : run.IsIntroPhase && !run.TutorialTipsSkipped
                        ? TutorialRoomStory(TutorialLessonChapter(run), false)
                        : StoryShortInstruction(run);
            roomFunctionText.text = run.IsFinished
                ? L("Actual outcomes take precedence over planned benefits.",
                    "以实际结算为准，不按建造计划推断效果。")
                : run.UsesFillHand
                    ? AwaitingFreeBuildChoice
                        ? L("Check the goals on the left, then select a card below.",
                            "先看左侧目标进度，再选择下方手牌。")
                        : FillHandRoomFunction()
                : IsEndless
                     ? L("A day counts when wildlife eats and at least one resident completes their daily loop.",
                         "动物实际进食，且至少一名居民完成上班、用餐、回家，才计入共栖连日。")
                : run.TutorialTipsSkipped ? string.Empty : StoryRoomFunction(run);
            storyCueIcon.sprite = run.IsFinished
                ? ConstructionIconCatalog.GetControl(IsEndless
                    ? ConstructionControlIcon.EndlessMode
                    : ConstructionControlIcon.FillMode)
                : run.UsesFillHand
                    ? ConstructionIconCatalog.GetBuild(selectedCategory,
                        selectedPlanting)
                : IsEndless || run.TutorialChapter ==
                    ConstructionTutorialChapter.InspectOak
                    ? storyOakIcon ?? ConstructionIconCatalog.GetBuild(
                        ConstructionCategory.Green)
                : StoryCueSprite(run);
            visitorMarker.SetActive(!run.IsFinished && !IsEndless && run.TutorialChapter ==
                ConstructionTutorialChapter.Residence && run.PigeonMeadowInspected);
            progressText.text = run.UsesFillHand
                ? run.OverallScore.ToString()
                : !IsEndless
                ? $"{run.BuiltCount} / {run.BoardCapacity}"
                : run.BuiltCount.ToString();
            if (!run.UsesFillHand)
            {
                progressHeading.rectTransform.anchoredPosition = new Vector2(0,
                    IsEndless ? 42 : 36);
                progressText.rectTransform.anchoredPosition = new Vector2(21,
                    IsEndless ? 0 : -10);
                progressIcon.rectTransform.anchoredPosition = new Vector2(-107,
                    IsEndless ? 0 : -17);
            }
            balanceScoreText.gameObject.SetActive(!IsEndless);
            endlessScoreStrip.SetActive(IsEndless);
            if (IsEndless)
            {
                endlessScoreValues[0].text = run.HasBalanceScore
                    ? CompactScore(run.UsesOverallScore ? run.OverallScore : run.BalanceScore, Chinese) : "–";
                endlessScoreValues[1].text = CompactScore(run.AnimalScore, Chinese);
                endlessScoreValues[2].text = CompactScore(run.HumanScore, Chinese);
                var latest = run.BuiltTiles[run.BuiltCount - 1];
                viewportCoordinateText.text = L(
                    $"VIEW {viewMinColumn + 4},{viewMinRow + 4}  ·  LATEST {latest.column + 1},{latest.row + 1}",
                    $"视野 {viewMinColumn + 4},{viewMinRow + 4}  ·  最新 {latest.column + 1},{latest.row + 1}");
            }
            RefreshEndlessLossStrip(humanPreview);
            balanceScoreText.text = run.HasBalanceScore
                ? run.UsesOverallScore
                    ? run.UsesFillHand
                        ? L($"SHARED {run.BalanceScore} · BUILD {run.PlacementScore} · GOALS {run.SpecialTaskScore}",
                            $"共栖 {run.BalanceScore} · 建造 {run.PlacementScore} · 目标 {run.SpecialTaskScore}")
                        : L($"TOTAL {run.OverallScore} = SHARED {run.BalanceScore}\n+ BUILD {run.PlacementScore} + TASKS {run.SpecialTaskScore}",
                            $"总分 {run.OverallScore} = 共栖 {run.BalanceScore}\n+ 建造 {run.PlacementScore} + 任务 {run.SpecialTaskScore}")
                : run.HasAdditiveCoexistenceScore
                    ? L($"SHARED {run.BalanceScore}\n= ANIMAL {run.AnimalScore} + RESIDENT {run.HumanScore}",
                        $"共栖 {run.BalanceScore} = 动物 {run.AnimalScore} + 居民 {run.HumanScore}")
                    : L($"OLD SHARED {run.BalanceScore}\nANIMAL {run.AnimalScore} · RESIDENT {run.HumanScore}",
                        $"旧共栖 {run.BalanceScore} · 动物 {run.AnimalScore} · 居民 {run.HumanScore}")
                : L("OLDER SCORE RULES", "旧版计分规则");
            nextDayButton.gameObject.SetActive(!run.UsesFillHand);
            nextDayButton.interactable = run.CanAdvanceConstructionDay &&
                (!IsEndless || run.EndlessLossRulesConfirmed);
            if (buildPaletteButton != null)
                buildPaletteButton.gameObject.SetActive(!run.UsesFillHand &&
                    !run.IsFinished &&
                    !IsBoardGame &&
                    (IsEndless || run.TutorialChapter !=
                        ConstructionTutorialChapter.InspectOak));
            var selectedPlantingToPlace = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var selectedPlotCount = AvailablePlotCount(selectedCategory,
                selectedPlantingToPlace);
            placementGuideText.text = run.IsFinished
                ? L("Run ended · review the observed results.",
                    "本局已结束 · 查看实际结算结果。")
                : run.UsesFillHand
                    ? run.BuiltCount == 0
                        ? L("Place the central oak in the middle plot.",
                            "先把中央橡树放在棋盘正中格。")
                        : AwaitingFreeBuildChoice
                            ? L("Choose a card or basic resource below.",
                                "点击下方手牌或基础资源，自行选择建筑。")
                        : selectedPlotCount == 0
                            ? L($"{SelectedBuildLabel()}: no valid plot. Choose another card when available.",
                                $"{SelectedBuildLabel()}暂无可建位置；可选其他手牌。")
                            : L($"{SelectedBuildLabel()} · choose one of {selectedPlotCount} valid plots.",
                                $"{SelectedBuildLabel()} · 从 {selectedPlotCount} 个合法位置中选择。")
                : IsBoardGame ? BoardGameShortInstruction()
                : IsEndless
                    ? run.BuiltToday
                        ? L("Built 1 room today · click Next day to continue.",
                            "今日已建 1 格 · 点击“下一天”继续。")
                        : AwaitingFreeBuildChoice
                            ? L("Choose and place one room before the next day.",
                                "先选择并放置一格，再进入下一天。")
                        : selectedPlotCount == 0
                            ? L($"{SelectedBuildLabel()} · {PlacementPrerequisite(selectedCategory)}",
                                $"{SelectedBuildLabel()} · {PlacementPrerequisite(selectedCategory)}")
                            : L($"{SelectedBuildLabel()} · {selectedPlotCount} valid plots",
                                $"{SelectedBuildLabel()} · 可放 {selectedPlotCount} 处")
                : run.TutorialChapter == ConstructionTutorialChapter.InspectOak
                    ? openingOakInspected
                        ? L("Oak inspected. Click Next day to settle the squirrel's meal.",
                            "已查看橡树。点“下一天”结算松鼠是否进食。")
                        : L("Click the central oak to keep its room guide on the right; " +
                            "hover a corner badge to inspect that number.",
                            "点击中央橡树，右侧固定显示用途；悬停单个角标查看该数值。")
                : run.IsIntroPhase
                    ? run.BuiltToday
                        ? L($"Room {run.IntroTypesBuilt}/8 built. Next day: {CategoryLabel(run.IntroRequiredCategory.Value)}.",
                            $"已建 {run.IntroTypesBuilt}/8 类。下一天建{CategoryLabel(run.IntroRequiredCategory.Value)}。")
                        : !introChoiceMade
                            ? run.IntroRequiredCategory == ConstructionCategory.Green
                                ? L("Next: Green. Open Build, then place it beside the centre oak.",
                                    "下一步建绿地。打开建造栏，选中央橡树旁一格。")
                                : L($"Next: {CategoryLabel(run.IntroRequiredCategory.Value)}. Open Build to select it.",
                                    $"下一步建{CategoryLabel(run.IntroRequiredCategory.Value)}。打开建造栏选择。")
                            : run.IntroRequiredCategory == ConstructionCategory.Green
                                ? L("Green selected. Choose a + plot directly above, below, left or right of the oak.",
                                    "已选绿地。点橡树上下左右四格中的＋格预览并确认。")
                                : L($"{SelectedBuildLabel()} selected. Click a + plot to preview, then confirm.",
                                    $"已选{SelectedBuildLabel()}。点＋空格预览，再确认建造。")
                : run.TutorialChapter == ConstructionTutorialChapter.Complete && run.BuiltToday
                    ? run.CurrentDay == 9 &&
                      run.IntroTypesBuilt == ConstructionTutorialGuide.RoomCount
                        ? L("All eight rooms built. Next day begins free planning.",
                            "八类房间已建完。下一天开始自由规划。")
                        : L("Built 1/1 today. Click Next day to build again.",
                            "今日已建 1/1。点击“下一天”继续建造。")
                : AwaitingFreeBuildChoice
                    ? L("Inspect food, routes and risks. Build one room of your choice today.",
                        "可查看食物、路线与风险；今天还能自行选建一格。")
                : run.IsCategoryAtLimit(selectedCategory)
                    ? L($"{CategoryLabel(selectedCategory)} limit " +
                            $"{run.CategoryBuiltCount(selectedCategory)}/{run.CategoryLimit(selectedCategory)}. Choose another type.",
                        $"{CategoryLabel(selectedCategory)}已达上限 " +
                            $"{run.CategoryBuiltCount(selectedCategory)}/{run.CategoryLimit(selectedCategory)}，请选择其他建筑。")
                : run.BuiltToday
                    ? L($"Selected: {SelectedBuildLabel()}. Built 1/1 today; click Next day.",
                        $"已选{SelectedBuildLabel()}。今日已建 1/1，请点“下一天”。")
                : run.TutorialChapter == ConstructionTutorialChapter.Green &&
                      selectedCategory != ConstructionCategory.Green
                        ? L($"Selected: {SelectedBuildLabel()}. A connected green space can grow meadow seeds for pigeons.",
                            $"已选{SelectedBuildLabel()}。建造相连绿地，草籽可能引来鸽子。")
                    : selectedPlotCount == 0
                        ? L($"Selected: {SelectedBuildLabel()}. {PlacementPrerequisite(selectedCategory)}.",
                            $"已选{SelectedBuildLabel()}。{PlacementPrerequisite(selectedCategory)}。")
                        : L($"Selected: {SelectedBuildLabel()}. Preview on a + plot ({selectedPlotCount} valid).",
                            $"已选{SelectedBuildLabel()}。点击＋地块预览（可放{selectedPlotCount}处）。");
            var showFreeBuildPrompt = !run.IsFinished && !IsBoardGame &&
                (!run.IsIntroPhase && AwaitingFreeBuildChoice ||
                 !IsEndless && run.TutorialChapter ==
                     ConstructionTutorialChapter.Complete && run.BuiltToday);
            selectedBuildIcon.sprite = showFreeBuildPrompt
                ? ConstructionIconCatalog.GetControl(run.BuiltToday
                    ? ConstructionControlIcon.NextDay : ConstructionControlIcon.Build)
                : run.UsesFillHand
                    ? ConstructionIconCatalog.GetBuild(selectedCategory,
                        selectedPlanting)
                : IsEndless
                    ? ConstructionHandArtCatalog.GetRoom(selectedCategory, selectedPlanting) ??
                      ConstructionIconCatalog.GetBuild(selectedCategory, selectedPlanting)
                : run.IsIntroPhase && !introChoiceMade
                    ? ConstructionIconCatalog.GetBuild(run.IntroRequiredCategory.Value)
                : ConstructionIconCatalog.GetBuild(selectedCategory, selectedPlanting);

            var endlessChoices = IsEndless && !run.IsFinished && !run.BuiltToday
                ? new HashSet<ConstructionCategory>() : null;
            foreach (var pair in categoryButtons)
            {
                var atLimit = run.IsCategoryAtLimit(pair.Key);
                var explored = run.IsIntroPhase && run.IsIntroTypeUsed(pair.Key);
                var nextIntroRoom = run.IsIntroPhase &&
                    run.IntroRequiredCategory == pair.Key;
                var unlocked = CanSelectCategory(pair.Key) && !run.IsFinished;
                var categoryPlotCount = unlocked ? AvailablePlotCount(pair.Key,
                    pair.Key == ConstructionCategory.Green ? selectedPlanting :
                        GreenPlanting.None) : 0;
                if (categoryPlotCount > 0) endlessChoices?.Add(pair.Key);
                // Keep all eight visible so the player can see the sequence;
                // only the next room is actionable until the tour is complete.
                pair.Value.interactable = !run.IsFinished &&
                    (!run.IsIntroPhase || nextIntroRoom && !run.BuiltToday);
                pair.Value.GetComponent<CanvasGroup>().alpha = buildPaletteOpen
                    ? explored ? 0.6f : 1f : 0f;
                var highlighted = nextIntroRoom ||
                    !run.IsIntroPhase && !atLimit &&
                    pair.Key == selectedCategory && !AwaitingFreeBuildChoice;
                pair.Value.GetComponent<Image>().color = highlighted
                    ? new Color(0.98f, 0.78f, 0.37f) : Gold;
                categoryFaces[pair.Key].color = highlighted ? UsedGreen : Ink;
                var status = categoryStatusLabels[pair.Key];
                status.text = run.IsFinished
                    ? L("Run ended", "本局结束")
                    : explored
                        ? L("Complete", "已完成")
                    : run.IsIntroPhase && !nextIntroRoom
                        ? L("Later step", "后续步骤")
                    : atLimit
                        ? L("Limit reached", "已达上限")
                    : !unlocked
                        ? L("Day 1", "第 1 天")
                    : categoryPlotCount == 0
                        ? PlacementPrerequisite(pair.Key)
                    : run.BuiltToday
                        ? L($"Tomorrow: {categoryPlotCount}", $"明日可放{categoryPlotCount}处")
                        : L($"{categoryPlotCount} plots", $"可放{categoryPlotCount}处");
                status.color = categoryPlotCount == 0 ? Gold : Paper;
                categoryCountLabels[pair.Key].text = run.IsIntroPhase
                    ? explored ? "✓1/1" : "0/1"
                    : IsBoardGame
                    ? $"{run.CategoryBuiltCount(pair.Key)}/{run.CategoryLimit(pair.Key)}"
                    : run.CategoryBuiltCount(pair.Key).ToString();
                categoryProvisionLabels[pair.Key].text = RoomProvisionBrief(pair.Key);
            }
            if (paletteNotice != null)
                paletteNotice.text = run.IsIntroPhase
                    ? run.BuiltToday
                        ? L($"Built {run.IntroTypesBuilt}/8 · click Next day",
                            $"已建 {run.IntroTypesBuilt}/8 · 点击“下一天”")
                        : L($"Next: {CategoryLabel(run.IntroRequiredCategory.Value)} · {run.IntroTypesBuilt}/8",
                            $"下一步：{CategoryLabel(run.IntroRequiredCategory.Value)} · {run.IntroTypesBuilt}/8")
                    : run.BuiltToday
                    ? L("Built 1 room today · click Next day to continue",
                        "今日已建 1 格 · 点击“下一天”继续")
                    : AwaitingFreeBuildChoice
                    ? IsEndless
                        ? L("Build one room or pass the day", "可建造一格或直接进入下一天")
                        : L("Choose freely · one room today", "自行选择 · 今日可建一格")
                    : IsBoardGame
                    ? run.IsCategoryAtLimit(selectedCategory)
                        ? L("Type full · choose another", "该类已满，请改选")
                        : L("Badges: built / deck stock", "数字：已建 / 牌组库存")
                    : L("Badges: rooms built · no type limit",
                         "数字：已建数量 · 无类型上限");
            if (endlessChoices != null)
                RefreshEndlessBuildingAdvice(endlessChoices, humanPreview,
                    foxForecast, wastePreview);

            var mayBuild = !run.IsFinished && !run.BuiltToday &&
                CanSelectCategory(selectedCategory) && !AwaitingFreeBuildChoice;
            var plantingToPlace = selectedCategory == ConstructionCategory.Green
                ? selectedPlanting : GreenPlanting.None;
            var squirrelHomes = run.VisibleSquirrelHomeTiles(
                    run.UsesFillHand ? int.MaxValue :
                        ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .GroupBy(home => home.id)
                .ToDictionary(group => group.Key, group => group.Count());
            var pigeonHomes = run.VisiblePigeonHomeTiles(run.UsesFillHand
                    ? int.MaxValue : ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                .GroupBy(home => home.id)
                .ToDictionary(group => group.Key, group => group.Count());
            var hedgehogHomeIds = run.HedgehogHabitats.Select(home => home.id)
                .ToHashSet();
            var squirrelArrival = run.ReturnStatus(
                ConstructionWildlifeSpecies.Squirrel);
            var pigeonArrival = run.ReturnStatus(
                ConstructionWildlifeSpecies.Pigeon);
            var hedgehogArrival = run.ReturnStatus(
                ConstructionWildlifeSpecies.Hedgehog);
            var foxArrival = run.ReturnStatus(
                ConstructionWildlifeSpecies.Fox);
            var foxHomeId = run.FoxHabitat?.id;
            foreach (var control in panControls)
            {
                control.SetActive(run.Mode == ConstructionGameMode.Endless);
                if (control.activeSelf) control.transform.SetAsLastSibling();
            }
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var worldColumn = viewMinColumn + column;
                var worldRow = viewMinRow + row;
                var tile = run.At(worldColumn, worldRow);
                // Digital hand rooms show their inhabitants in the 3D scene.
                // The four metric corners duplicate that visual information.
                cellMetricGroups[column, row].gameObject.SetActive(
                    !run.UsesFillHand);
                foxRiskMarkers[column, row].SetActive(tile != null &&
                    tile.id == predationRiskTileId);
                foxRiskMarkers[column, row].transform.localScale =
                    tile != null && tile.id == focusedPredationTileId
                        ? Vector3.one * 1.08f : Vector3.one;
                var canPlaceHere = tile == null && mayBuild &&
                    run.CheckPlacement(worldColumn, worldRow, selectedCategory,
                        plantingToPlace, ResidenceKind.Standard) ==
                    ConstructionPlacementFailure.None;
                var isPending = hasPendingPlacement &&
                    worldColumn == pendingColumn && worldRow == pendingRow;
                buildableHints[column, row].gameObject.SetActive(
                    canPlaceHere && !isPending);
                upgradePreviewBorders[column, row]?.SetActive(
                    tile != null && upgradesById.Contains(tile.id));
                placementPreviewFrames[column, row].gameObject.SetActive(isPending);
                if (isPending)
                {
                    placementPreviewMats[column, row].color = ReadingPaper;
                    var previewPlanting = pendingCategory ==
                        ConstructionCategory.Green &&
                        pendingPlanting == GreenPlanting.None
                            ? run.ResolveGreenPlanting(worldColumn, worldRow)
                            : pendingPlanting;
                    var upgradedPicture = pendingPlot != null &&
                        upgradesById.Contains(pendingPlot.id)
                        ? ConstructionIllustratedVisualStyle.UpgradePicture(
                            pendingPlot, ConstructionPlacementScore.UpgradeLevel(
                                pendingPlot, projectedBoard))
                        : null;
                    placementPreviewIcons[column, row].sprite = upgradedPicture ??
                        ConstructionHandArtCatalog.GetRoom(pendingCategory,
                            previewPlanting) ??
                        ConstructionIconCatalog.GetBuild(pendingCategory,
                            previewPlanting);
                    var turnCrossing = upgradedPicture != null &&
                        ConstructionPlacementScore.SlowStreetAxis(pendingPlot,
                            projectedBoard) == ConstructionSlowStreetAxis.NorthSouth;
                    placementPreviewIcons[column, row].rectTransform.localRotation =
                        Quaternion.Euler(0f, 0f, turnCrossing ? 90f : 0f);
                }
                var icon = cellIcons[column, row];
                icon.gameObject.SetActive(tile != null &&
                    (worldView == null || !worldView.IsReady));
                var isFoodSource = tile?.category is ConstructionCategory.Green or
                    ConstructionCategory.Waste or ConstructionCategory.Square or
                    ConstructionCategory.Supermarket;
                foodBadges[column, row].transform.parent.gameObject.SetActive(
                    isFoodSource);
                if (tile?.category == ConstructionCategory.Green)
                    foodBadges[column, row].text =
                        $"{run.FoodStock(tile.id)}/{run.FoodCapacity(tile.id)}";
                else if (tile?.category == ConstructionCategory.Waste)
                {
                    foodBadges[column, row].text =
                        wastePreview.CollectedByRoom.TryGetValue(tile.id, out var cleared) &&
                        cleared > 0 ? "~1" : "~0";
                }
                else if (tile?.category == ConstructionCategory.Square)
                {
                    foodBadges[column, row].text = humanPreview.residents.Any(person =>
                        person.visitedSquare && person.squareTileId == tile.id)
                        ? "+1" : "+0";
                }
                else if (tile?.category == ConstructionCategory.Supermarket)
                {
                    foodBadges[column, row].text = humanPreview.residents.Any(person =>
                        person.ate && person.mealTileId == tile.id)
                        ? "+1" : "+0";
                }
                growthBadges[column, row].transform.parent.gameObject.SetActive(
                    tile?.category == ConstructionCategory.Green);
                if (tile?.category == ConstructionCategory.Green)
                    growthBadges[column, row].text =
                        $"{run.GreenGrowthStage(tile.id)}/2";
                residenceBonusBadges[column, row].transform.parent.gameObject.SetActive(
                    tile?.category == ConstructionCategory.Residence);
                if (tile?.category == ConstructionCategory.Residence)
                {
                    var resident = humanPreview.residents.FirstOrDefault(person =>
                        person.homeTileId == tile.id);
                    var bonus = (resident?.routeBonus ?? 0) +
                        (resident?.homeBonus ?? 0);
                    residenceBonusBadges[column, row].text = bonus > 0 ? "✓" :
                        resident?.completedCycle == true ? "·" : "!";
                    residenceBonusBadges[column, row].transform.parent
                        .GetComponent<Image>().color = bonus > 0
                            ? UsedGreen : resident?.completedCycle == true
                                ? IdleAmber : BlockedRed;
                }
                var hasJob = tile != null &&
                    ConstructionHumanModel.DailyJobCapacity(tile.category) > 0;
                workCycleBadges[column, row].transform.parent.gameObject.SetActive(
                    hasJob);
                if (hasJob)
                {
                    var assigned = humanPreview.residents.Any(person =>
                        person.workTileId == tile.id);
                    var completed = humanPreview.residents.Any(person =>
                        person.workTileId == tile.id && person.completedCycle);
                    workCycleBadges[column, row].text = completed ? "✓" :
                        assigned ? "×" : "·";
                    workCycleBadges[column, row].transform.parent
                        .GetComponent<Image>().color = completed
                            ? UsedGreen : assigned ? BlockedRed : IdleAmber;
                }
                streetDeathBadges[column, row].transform.parent.gameObject.SetActive(
                    tile?.category == ConstructionCategory.Street);
                streetExposureBadges[column, row].transform.parent.gameObject.SetActive(
                    tile?.category == ConstructionCategory.Street);
                if (tile?.category == ConstructionCategory.Street)
                {
                    var deaths = run.StreetTrafficDeathCount(tile.id);
                    streetDeathBadges[column, row].text = deaths.ToString();
                    streetDeathBadges[column, row].transform.parent
                        .GetComponent<Image>().color = deaths > 0
                            ? BlockedRed : UsedGreen;
                    streetExposureBadges[column, row].text =
                        run.StreetAdjacentWildlifeCount(tile.id).ToString();
                    streetExposureBadges[column, row].transform.parent
                        .GetComponent<Image>().color =
                            run.StreetAdjacentWildlifeCount(tile.id) > 0
                                ? IdleAmber : UsedGreen;
                }
                var utilization = run.IsFinished ? default :
                    ConstructionTileUtilizationModel.Evaluate(tile,
                        run, humanPreview, foxForecast, wastePreview);
                var usePanel = capacityBadges[column, row].transform.parent;
                // The red fox marker sits above the centre, so the right
                // metric remains readable even when this room is at risk.
                var showUse = utilization.ShowOnTile;
                usePanel.gameObject.SetActive(showUse);
                if (showUse)
                {
                    capacityBadges[column, row].text =
                        $"{utilization.Used}/{utilization.Available}";
                    utilizationSymbols[column, row].text = utilization.Level switch
                    {
                        ConstructionUtilizationLevel.Full => "✓",
                        ConstructionUtilizationLevel.Blocked => "!",
                        _ => "·"
                    };
                    usePanel.GetComponent<Image>().color =
                        UtilizationColour(utilization.Level);
                }
                var wasteSource = tile?.category is ConstructionCategory.Residence or
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket;
                var backlog = wasteSource ? run.WasteBacklog(tile.id) : 0;
                wasteBadges[column, row].transform.parent.gameObject.SetActive(
                    backlog > 0);
                if (backlog > 0) wasteBadges[column, row].text = backlog.ToString();
                ShowAnimalBadge(squirrelIcons[column, row],
                    squirrelCounts[column, row], tile != null &&
                    squirrelHomes.TryGetValue(tile.id, out var squirrelsHere)
                        ? squirrelsHere : 0, squirrelArrival, tile?.id);
                ShowAnimalBadge(pigeonIcons[column, row],
                    pigeonCounts[column, row], tile != null &&
                    pigeonHomes.TryGetValue(tile.id, out var pigeonsHere)
                        ? pigeonsHere : 0, pigeonArrival, tile?.id);
                ShowAnimalBadge(hedgehogIcons[column, row],
                    hedgehogCounts[column, row], tile != null &&
                    hedgehogHomeIds.Contains(tile.id)
                        ? 1 : 0, hedgehogArrival, tile?.id);
                ShowAnimalBadge(foxIcons[column, row],
                    foxCounts[column, row], tile != null &&
                    tile.id == foxHomeId
                        ? 1 : 0, foxArrival, tile?.id);
                var hasCoexistenceLink = run.ScoringVersion >= 8 &&
                    tile != null &&
                    (tile.category == ConstructionCategory.Residence &&
                        humanPreview.residents.Any(person =>
                            person.homeTileId == tile.id &&
                            (person.linkageBonus > 0 ||
                             person.biodiversityBonus > 0)) ||
                     (tile.category is ConstructionCategory.Green or
                         ConstructionCategory.Square or
                         ConstructionCategory.Supermarket) &&
                        humanPreview.residents.Any(person =>
                            person.linkageSourceTileId == tile.id) ||
                     tile.category == ConstructionCategory.Street &&
                         run.StreetAdjacentWildlifeCount(tile.id) > 0 &&
                         run.IsStreetProtectedTomorrow(tile.id));
                coexistenceBadges[column, row].SetActive(hasCoexistenceLink);
                leftMetricRails[column, row].SetActive(tile != null &&
                    (growthBadges[column, row].transform.parent.gameObject.activeSelf ||
                     residenceBonusBadges[column, row].transform.parent.gameObject.activeSelf ||
                     workCycleBadges[column, row].transform.parent.gameObject.activeSelf ||
                     streetDeathBadges[column, row].transform.parent.gameObject.activeSelf ||
                     wasteBadges[column, row].transform.parent.gameObject.activeSelf ||
                     squirrelCounts[column, row].transform.parent.gameObject.activeSelf ||
                     pigeonCounts[column, row].transform.parent.gameObject.activeSelf ||
                     hedgehogCounts[column, row].transform.parent.gameObject.activeSelf ||
                     foxCounts[column, row].transform.parent.gameObject.activeSelf));
                rightMetricRails[column, row].SetActive(tile != null &&
                    (usePanel.gameObject.activeSelf ||
                     foodBadges[column, row].transform.parent.gameObject.activeSelf ||
                     streetExposureBadges[column, row].transform.parent.gameObject.activeSelf));
                var isHome = tile?.category == ConstructionCategory.Residence &&
                    run.HasResident(tile.id);
                residentMarkers[column, row].SetActive(isHome &&
                    (!run.UsesFillHand || worldView?.IsReady != true) &&
                    (movingResidentMarker == null || !movingResidentMarker.activeSelf));
                pigeonIcons[column, row].rectTransform.localScale = Vector3.one;
                cellBackgrounds[column, row].color = worldView != null &&
                    worldView.IsReady
                    ? tile != null ? Color.clear : canPlaceHere
                        ? new Color(0.61f, 0.87f, 0.72f, 0.16f)
                        : Color.clear
                    : tile == null
                        ? canPlaceHere ? new Color(0.53f, 0.65f, 0.54f)
                            : UrbanPalette.ConstructionGround
                        : TileColor(tile.category);
                if (tile == null) continue;
                icon.sprite = ConstructionIconCatalog.GetBuild(tile.category,
                    tile.planting);
                icon.color = Color.white;
                var size = tile.category == ConstructionCategory.Green &&
                           tile.id != ConstructionBoardModel.StarterOakId
                    ? 29f + run.GreenGrowthStage(tile.id) * 9f : 47f;
                cellIconRects[column, row].sizeDelta = new Vector2(size, size);
            }
            worldView?.SetViewport(viewMinColumn, viewMinRow);
            worldView?.Sync(run, false);
            RefreshRoomIssues(humanPreview, foxForecast, wastePreview);
            if (foxRiskLocator != null)
                foxRiskLocator.SetActive(!run.IsFinished &&
                    !string.IsNullOrEmpty(predationRiskTileId) &&
                    VisibleTile(predationRiskTileId) == null);
            RefreshRouteInspection(humanPreview);
            RefreshResidentDispatch(humanPreview, foxForecast);
            RefreshCoexistenceHighlights();
            RefreshFixedInfoCard();
            HideTooltip();
            RefreshPendingPlacement(UpgradePreviewSummary(upgradeTiles, projectedBoard),
                pendingPlot, projectedBoard);
            RefreshFillHand();
            RefreshSpecialTasks();
            var result = run.FinalResult;
            finalResultPanel.SetActive(result != null && !resultReviewOpen);
            if (result != null)
                RefreshFinalResult(result);
            if (endlessRulesOverlay != null && endlessRulesOverlay.activeSelf)
                RefreshEndlessRulesUi();
            RefreshOpeningTour();
            RefreshTutorialFocus();
            // Room-detail popovers can reorder root Canvas children during a
            // refresh. Keep the open dispatch card above those popovers and
            // persistent page actions so its controls stay clickable.
            if (residentDispatchPanel != null &&
                residentDispatchPanel.gameObject.activeSelf)
                residentDispatchPanel.transform.SetAsLastSibling();
            RefreshQuickGuide();
            TryShowOpeningQuickGuide();
            if (result != null)
            {
                finalResultPanel.transform.SetAsLastSibling();
                if (localRankingsOverlay != null &&
                    localRankingsOverlay.activeSelf)
                    localRankingsOverlay.transform.SetAsLastSibling();
                if (networkRankingsOverlay != null &&
                    networkRankingsOverlay.activeSelf)
                    networkRankingsOverlay.transform.SetAsLastSibling();
                if (awardStandardsOverlay != null && awardStandardsOverlay.activeSelf)
                    awardStandardsOverlay.transform.SetAsLastSibling();
                if (endlessRulesOverlay != null && endlessRulesOverlay.activeSelf)
                    endlessRulesOverlay.transform.SetAsLastSibling();
            }
            RefreshResultReview();
            RefreshScoreTrace();
        }

        private void RefreshFinalResult(ConstructionFinalResult result)
        {
            RefreshResultLeaderboard(result);
            var hasLinkage = run.HasLinkageScore;
            resultTotalCaption.text = !run.HasBalanceScore
                ? L("NO TOTAL", "无共栖分")
                : run.UsesOverallScore
                    ? L("TOTAL", "本局总分")
                : run.HasAdditiveCoexistenceScore
                    ? L("SHARED", "共栖总分")
                    : L("OLD SCORE", "旧共栖分");
            finalResultText.text = run.HasBalanceScore
                ? (run.UsesOverallScore ? run.OverallScore :
                    result.BalancePointsTotal).ToString(CultureInfo.InvariantCulture)
                : "—";
            resultAnimalCaption.text = L("ANIMALS", "动物");
            resultResidentCaption.text = L("RESIDENTS", "居民");
            resultAnimalValue.text = result.AnimalPointsTotal.ToString(
                CultureInfo.InvariantCulture);
            resultResidentValue.text = result.HumanPointsTotal.ToString(
                CultureInfo.InvariantCulture);

            resultLinkageRibbon.color = hasLinkage
                ? new Color(0.83f, 0.87f, 0.72f, 0.98f)
                : new Color(0.89f, 0.85f, 0.76f, 0.98f);
            resultLinkageValue.text = hasLinkage
                ? L($"LINKAGE +{result.LinkagePointsTotal}",
                    $"联动 +{result.LinkagePointsTotal}")
                : L("NO LINKAGE DATA", "无联动明细");
            resultLinkageValue.rectTransform.anchoredPosition = new Vector2(
                hasLinkage ? -116 : 0, -19);
            resultLinkageValue.rectTransform.sizeDelta = new Vector2(
                hasLinkage ? 368 : 625, 37);
            resultLinkageDivider.gameObject.SetActive(hasLinkage);
            resultLinkageDetail.gameObject.SetActive(hasLinkage);
            if (hasLinkage)
            {
                resultLinkageDetail.fontSize = run.ScoringVersion >= 8 ? 13 : 15;
                var untracked = result.DaysPlayed - result.LinkageTrackedDays;
                resultLinkageDetail.text = untracked > 0
                    ? L($"{untracked} OLD DAYS UNTRACKED",
                        $"旧存档：前 {untracked} 天未统计联动")
                    : run.ScoringVersion >= 8
                        ? L($"FOOD +{result.LinkagePointsTotal - 2 * result.BiodiversityHomesTotal} · HABITAT +{2 * result.BiodiversityHomesTotal}",
                            $"食源+{result.LinkagePointsTotal - 2 * result.BiodiversityHomesTotal} · 动物邻居+{2 * result.BiodiversityHomesTotal}")
                    : L("IN RESIDENT SCORE", "已计入居民分");
                SetResultEvent(0, run.ScoringVersion >= 7
                        ? L("SQUARE LINKS", "广场联动")
                        : L("PLAZA SHARES", "广场共享"),
                    result.SharedSquareMealsTotal,
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Square));
                SetResultEvent(1, run.ScoringVersion >= 7
                        ? L("MARKET LINKS", "超市联动")
                        : L("MARKET SHARES", "超市共享"),
                    result.SharedSupermarketMealsTotal,
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Supermarket));
                SetResultEvent(2, run.ScoringVersion >= 7
                        ? L("GREEN LINKS", "绿地联动")
                        : L("GREEN SHARES", "绿地共生"),
                    result.SharedGreenMealsTotal,
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Green,
                        GreenPlanting.Oak));
            }
            else
            {
                SetResultEvent(0, L("ANIMAL EVENTS", "动物进食"),
                    result.AnimalMealsTotal,
                    ConstructionIconCatalog.GetStatus(ConstructionStatusIcon.AnimalFood));
                SetResultEvent(1, L("RESIDENT CYCLES", "居民循环"),
                    result.HumanWorkdaysTotal,
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.CompletedCommute));
                resultEventCaptions[2].text = run.UsesFillHand
                    ? L("BOTH SCORED", "双方得分次数")
                    : L("BOTH SCORED", "双方得分天数");
                resultEventValues[2].text =
                    $"{result.BothSidesScoredDays}/{result.DaysPlayed}";
                resultEventIcons[2].sprite =
                    ConstructionIconCatalog.GetControl(ConstructionControlIcon.FillMode);
            }

            resultEquationText.text = !run.HasBalanceScore
                ? L("Older save: no shared total was recorded.",
                    "旧存档未记录共栖总分。")
                : run.UsesOverallScore
                    ? L($"Total = shared {result.BalancePointsTotal} + build {run.PlacementScore} + goals {run.SpecialTaskScore}.",
                        $"本局总分 = 共栖 {result.BalancePointsTotal} + 建造 {run.PlacementScore} + 目标牌 {run.SpecialTaskScore}。")
                : run.HasAdditiveCoexistenceScore
                    ? L("Shared score = animal score + resident score",
                        "共栖总分 = 动物分 + 居民分")
                    : L("Older rule: sum of each day's lower animal/resident score",
                        "旧规则：逐日取动物与居民分中较低值，再求和");
            var bestStreak = IsEndless ? run.EndlessCoexistenceProgress.best : 0;
            resultObservedText.text = IsEndless
                ? L($"{result.DaysPlayed} days settled · longest coexistence streak {bestStreak} days",
                    $"已结算 {result.DaysPlayed} 天 · 最长连续共栖 {bestStreak} 天")
                : run.UsesFillHand
                ? L($"Observed {result.AnimalMealsTotal} meals · " +
                    $"{result.HumanWorkdaysTotal} resident cycles · " +
                    $"both scored in {result.BothSidesScoredDays}/{result.DaysPlayed} settlements",
                    $"实际进食 {result.AnimalMealsTotal} 份 · " +
                    $"居民循环 {result.HumanWorkdaysTotal} 次 · " +
                    $"双方得分 {result.BothSidesScoredDays}/{result.DaysPlayed} 次结算")
                : run.UsesLegacyAnimalScoring
                ? L($"{result.AnimalMealsTotal} legacy animal events (including fox hunts) · " +
                    $"{result.HumanWorkdaysTotal} resident cycles",
                    $"旧版动物事件 {result.AnimalMealsTotal} 次（含狐狸捕食）· " +
                    $"居民循环 {result.HumanWorkdaysTotal} 次")
                : L($"Observed {result.AnimalMealsTotal} meals · " +
                    $"{result.HumanWorkdaysTotal} resident cycles · " +
                    $"both scored {result.BothSidesScoredDays}/{result.DaysPlayed} days",
                    $"实际进食 {result.AnimalMealsTotal} 份 · " +
                    $"居民循环 {result.HumanWorkdaysTotal} 次 · " +
                    $"双方得分 {result.BothSidesScoredDays}/{result.DaysPlayed} 天");
            RefreshAwardEvaluation(result);
            if (confirmReset) return;
            resultNoticeText.text = run.EndReason switch
            {
                ConstructionEndReason.BoardComplete when run.IsRandomBoardGame => L(
                    $"Balance {Math.Min(result.AnimalPointsTotal, result.HumanPointsTotal)} · " +
                    "rank by balance, then linkage, then shared score.",
                    $"均衡 {Math.Min(result.AnimalPointsTotal, result.HumanPointsTotal)} · " +
                    "排行先看均衡，再看联动、共栖分。"),
                ConstructionEndReason.AnimalDeathsLimit => L(
                    $"Last {ConstructionRunModel.AnimalDeathWindowDays} days: " +
                    $"{run.RecentAnimalDeaths} deaths; limit {run.AnimalDeathLimit}.",
                    $"最近 {ConstructionRunModel.AnimalDeathWindowDays} 天死亡 " +
                    $"{run.RecentAnimalDeaths} 只；上限 {run.AnimalDeathLimit} 只。"),
                ConstructionEndReason.ResidentsUnemployed => L(
                    $"{run.UnemployedResidentsLastDay} without work for " +
                    $"{run.UnemployedDaysInRow}/" +
                    $"{ConstructionRunModel.UnemployedConsecutiveDays} days; " +
                    $"limit {run.UnemployedResidentLimit}.",
                    $"未上班居民 {run.UnemployedResidentsLastDay} 人，连续 " +
                    $"{run.UnemployedDaysInRow}/" +
                    $"{ConstructionRunModel.UnemployedConsecutiveDays} 天；" +
                    $"上限 {run.UnemployedResidentLimit} 人。"),
                ConstructionEndReason.WildlifeGone => L(
                    "Squirrels or arrived pigeons remained absent for 5 days.",
                    "松鼠或已到来的鸽群连续 5 天消失。"),
                ConstructionEndReason.ResidentsGone => L(
                    "All residents left after 3 missed work-and-meal cycles.",
                    "居民连续 3 天未完成上班用餐循环，全部离开。"),
                _ => string.Empty
            };
        }

        private void SetResultEvent(int index, string caption, int count,
            Sprite sprite)
        {
            resultEventCaptions[index].text = caption;
            resultEventValues[index].text = count.ToString(
                CultureInfo.InvariantCulture);
            resultEventIcons[index].sprite = sprite;
        }

        private void RefreshDayReview()
        {
            var ecology = run.LastEcologyDay;
            var human = run.LastHumanDay;
            var waste = run.LastWasteDay;
            var forecast = run.LastForecast;
            if (run.UsesFillHand)
            {
                dayReviewPanel.gameObject.SetActive(false);
                observationLedger.gameObject.SetActive(false);
                primerRulesRoot.gameObject.SetActive(false);
                return;
            }
            if (ecology == null || human == null || waste == null)
            {
                var showPrimer = !IsEndless &&
                    run.CurrentDay == 1 && !run.IsFinished &&
                    !run.TutorialTipsSkipped;
                dayReviewPanel.gameObject.SetActive(showPrimer);
                observationLedger.gameObject.SetActive(showPrimer);
                var showRules = showPrimer && run.HasAdditiveCoexistenceScore;
                primerRulesRoot.gameObject.SetActive(showRules);
                dayAnimalMetricText.transform.parent.gameObject.SetActive(false);
                dayHumanMetricText.transform.parent.gameObject.SetActive(false);
                dayWasteMetricText.transform.parent.gameObject.SetActive(false);
                dayAnimalMetricText.text = dayHumanMetricText.text =
                    dayWasteMetricText.text = "–";
                reviewHeading.text = showPrimer
                    ? L("TODAY'S TIP", "今日提示")
                    : L("LAST DAY", "昨日结算");
                primerRuleLabels[0].text = L("ANIMAL", "动物");
                primerRuleLabels[1].text = string.Empty;
                primerRuleLabels[2].text = string.Empty;
                primerRuleDetails[0].text = run.HasFedAnimalScore
                    ? L($"Each animal that eats +{(run.ScoringVersion >= 12 ? 3 : 4)}; each green plot used as food +{(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)} once.",
                        $"每只吃到食物的动物 +{(run.ScoringVersion >= 12 ? 3 : 4)}；实际供食的绿地每块再 +{(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)}。")
                    : run.ScoringVersion >= 7
                    ? L("Available food in own tile + eight neighbours: green +1, square +4.",
                        "自身及周围八格的可用食源：绿地+1，广场+4。")
                    : L("Green food gives +3 only when an animal eats it.",
                        "绿地食物被动物实际吃到才得 +3 分。");
                primerRuleDetails[1].text = string.Empty;
                primerRuleDetails[2].text = string.Empty;
                dayReviewText.GetComponent<CanvasGroup>().alpha =
                    showPrimer && !showRules ? 1f : 0f;
                dayReviewText.raycastTarget = showPrimer && !showRules;
                dayReviewText.fontSize = 14;
                dayReviewText.text = !showPrimer || showRules ? string.Empty : L(
                        "Older save: shared points use each day's lower animal/resident score. Both totals stay separate.",
                        "旧存档：共栖分逐日取动物与居民分的较低值；两项得分分别累计。");
                dayReviewDetails = dayReviewText.text;
                dayReviewText.color = Ink;
                return;
            }
            dayReviewPanel.gameObject.SetActive(true);
            observationLedger.gameObject.SetActive(true);
            primerRulesRoot.gameObject.SetActive(false);
            dayAnimalMetricText.transform.parent.gameObject.SetActive(true);
            dayHumanMetricText.transform.parent.gameObject.SetActive(true);
            dayWasteMetricText.transform.parent.gameObject.SetActive(true);
            // The former scoring-primer area now carries the settled day's
            // concise actual/forecast report instead of staying blank.
            dayReviewText.GetComponent<CanvasGroup>().alpha = 1f;
            dayReviewText.raycastTarget = true;
            dayAnimalMetricText.text = (run.UsesLegacyAnimalScoring
                ? ecology.LegacyAnimalMeals
                : (run.LastScoreDay?.animalPoints ?? ecology.animalPoints) -
                  (run.LastScoreDay?.trafficPenaltyPoints ?? 0)).ToString();
            dayHumanMetricText.text = human.CompletedWorkCycles.ToString();
            dayWasteMetricText.text = waste.Cleared.ToString();
            if (forecast == null)
            {
                dayReviewText.text = L(
                    $"DAY {ecology.day} · Animal {ecology.AnimalMeals} " +
                    $"· Work {human.CompletedWorkCycles}\nForecast not saved in this older run.",
                    $"第 {ecology.day} 天 · 进食 {ecology.AnimalMeals} · " +
                    $"通勤 {human.CompletedWorkCycles}\n旧存档未记录预计值。");
                dayReviewDetails = dayReviewText.text;
                dayReviewText.color = Ink;
                return;
            }
            var forecastMealCount = run.UsesLegacyAnimalScoring
                ? ecology.LegacyAnimalMeals : ecology.AnimalMeals;
            var matches = forecastMealCount == forecast.animalMeals &&
                human.CompletedWorkCycles == forecast.completedWorkCycles &&
                waste.Cleared == forecast.wasteCleared;
            var issues = DayIssues(ecology, human, forecast);
            var issueLines = issues.Count == 0
                ? L("No unmet need observed.", "未发现未满足的需求。")
                : issues[0] +
                  (issues.Count > 1 ? L($" (+{issues.Count - 1} more)",
                      $"（另有 {issues.Count - 1} 项）") : "");
            // Keep actual and forecast values visible even when they differ.
            // A mismatch must never replace the concrete cause found below.
            var actualForecast = L(
                $"Actual/plan: Meals {ecology.AnimalMeals}/{forecast.animalMeals} · " +
                $"Work {human.CompletedWorkCycles}/{forecast.completedWorkCycles} · " +
                $"Cleanup {waste.Cleared}/{forecast.wasteCleared}" +
                (matches ? "" : " !"),
                $"实际/预计：进食{ecology.AnimalMeals}/{forecast.animalMeals} · " +
                $"通勤{human.CompletedWorkCycles}/{forecast.completedWorkCycles} · " +
                $"清运{waste.Cleared}/{forecast.wasteCleared}" +
                (matches ? "" : " !"));
            dayReviewText.text = actualForecast + "\n" + issueLines;
            dayReviewDetails = actualForecast + "\n" +
                (issues.Count == 0 ? issueLines : string.Join("\n", issues));
            dayReviewText.color = matches && issues.Count == 0
                ? Ink : new Color(0.55f, 0.27f, 0.16f);
        }

        private string AnimalScoreTooltip()
        {
            if (run == null) return string.Empty;
            if (run.UsesLegacyAnimalScoring)
                return L(
                    $"Total {run.AnimalScore} points under the saved older rules. Fox hunts were included.",
                    $"累计 {run.AnimalScore} 分，使用存档的旧规则；狐狸捕食曾计分。");
            if (run.HasFedAnimalScore)
                return L(
                    $"Total {run.AnimalScore} animal points. After each placement, each squirrel, resident pigeon or hedgehog that eats gains {(run.ScoringVersion >= 12 ? 3 : 4)}. Each green plot that fed wildlife adds {(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)} once; each square or supermarket adds 1 once. Waste adds no source bonus. Fox hunts and visits without feeding do not count as meals. " +
                    (run.HasTrafficPenaltyScore ? $"A traffic collision costs {run.CollisionPenalty} points. " : "") +
                    (run.IsFinished ? "This run has ended." : "The next score appears after settlement."),
                    $"累计 {run.AnimalScore} 分。每次放置后，实际吃到食物的松鼠、常驻鸽和刺猬每只 +{(run.ScoringVersion >= 12 ? 3 : 4)}；实际供食的绿地每块再 +{(run.ScoringVersion >= 12 ? 2 : run.ScoringVersion >= 11 ? 3 : 2)}，广场或超市每块再 +1，垃圾食源不加分。同一食源每次只加一次；单纯到访和狐狸捕食不算进食。" +
                    (run.HasTrafficPenaltyScore ? $"每次车祸扣 {run.CollisionPenalty} 分。" : "") +
                    (run.IsFinished ? "本局已结束。" : "下次放置后再次结算。"));
            if (run.ScoringVersion >= 7)
                return L(
                    $"Total {run.AnimalScore} points. Each distinct available food plot in an occupied animal habitat's own space or eight neighbours scores once per day: green +1 · square +4 · supermarket +3 · waste +1. Meals keep animals alive; they do not add points. " +
                    (run.HasTrafficPenaltyScore ? "A traffic death costs 3. " : "") +
                    (run.ScoringVersion >= 8
                        ? "A resident-visited square halves collision risk on streets in its eight neighbouring plots. "
                        : "") +
                    (run.IsFinished ? "This run has ended." :
                        "The next animal score appears after settlement."),
                    $"累计 {run.AnimalScore} 分。每个动物栖息格自身及周围八格的不同可用食源，每天各计一次：绿地+1 · 广场+4 · 超市+3 · 垃圾间+1。进食关系到存活，不直接加分。" +
                    (run.HasTrafficPenaltyScore ? "车祸死亡扣3分。" : "") +
                    (run.ScoringVersion >= 8
                        ? "居民到访广场后，其周围八格内街道的车祸风险减半。"
                        : "") +
                    (run.IsFinished ? "本局已结束。" :
                        "本次动物得分将在结算后显示。"));
            return L(
                $"Total {run.AnimalScore} points. Meals add points" +
                (run.HasTrafficPenaltyScore ? "; traffic collisions subtract 3.\n" : ".\n") +
                "Green food +3 · supermarket/square food +2 · waste scrap +1.\n" +
                "Each animal eats once per day: suitable green first, then reachable urban food.\n" +
                (run.HasVariableGreenYield ? "From day 10, mature green yields succeed on 80% of eligible days; stored food can cover a miss.\n" : "") +
                (run.IsFinished ? "This run has ended."
                    : "The next animal score appears after settlement."),
                $"累计 {run.AnimalScore} 分。觅食加分" +
                (run.HasTrafficPenaltyScore ? "，车祸扣 3 分。\n" : "。\n") +
                "绿地 +3 · 超市/广场食物 +2 · 垃圾区残食 +1。\n" +
                "每只动物每天最多吃 1 份：先吃适合的绿地食物，再找可达的城市食物。\n" +
                (run.HasVariableGreenYield ? "第10天起成熟绿地应产时有80%概率产出；停产日可用存粮补足。\n" : "") +
                (run.IsFinished ? "本局已结束。"
                    : "本次动物得分将在结算后显示。"));
        }

        private string ResidentScoreTooltip()
        {
            if (run == null) return string.Empty;
            if (!run.HasBalanceScore)
                return L(
                    $"Older save: {run.HumanScore} points. Each complete work, meal and return cycle gives 1 point.",
                    $"旧存档累计 {run.HumanScore} 分。每次完成上班、用餐、返家循环得 1 分。");
            return L(
                $"Resident satisfaction {run.HumanScore} points. Complete work, meal and return: +1.\n" +
                "Jobs: workshop +2 · restaurant +1 · supermarket +0. Meals: supermarket +0 · restaurant +1.\n" +
                $"Direct +1 · mature green visit +{(run.ScoringVersion >= 11 ? 2 : 1)} · short street +2 · square visit +1.\n" +
                (run.HasLinkageScore
                    ? (run.ScoringVersion >= 8
                        ? "Shared food: square +3 · market +2 · mature green +1 (max +6/day). Two fed wildlife species near a completed home: +2 (max two homes/day). Both are included here.\n"
                        : run.ScoringVersion >= 7
                        ? "Nearby food areas available to wildlife: square +3 · supermarket +2 · mature green +1. Best one per resident, max +6/day; included here.\n"
                        : "Shared food: plaza +3 · supermarket +2 · mature green +1. Best one per resident, max +6/day; included here.\n")
                    : "") +
                (run.IsFinished ? "This run has ended."
                    : "The next resident score appears after settlement."),
                $"居民满意度累计 {run.HumanScore} 分。上班、用餐、返家 +1。\n" +
                "岗位：工作坊+2、餐厅+1、超市+0。用餐：超市+0、餐厅+1。\n" +
                $"就近+1 · 到访周围成熟绿地+{(run.ScoringVersion >= 11 ? 2 : 1)} · 短街道+2 · 到访广场+1。\n" +
                (run.HasLinkageScore
                    ? (run.ScoringVersion >= 8
                        ? "共享食源：广场+3、超市+2、成熟绿地+1（每日最多+6）。完成循环的住宅旁有两种已觅食动物：+2（每天最多2户）。都已计入居民分。\n"
                        : run.ScoringVersion >= 7
                        ? "动物可利用的附近食源：广场+3、超市+2、成熟绿地+1；每人每天只取最高一项，全局每日最多+6，已计入居民分。\n"
                        : "共享食物：广场+3、超市+2、成熟绿地+1；每人每天只取最高一项，全局每日最多+6，已计入居民分。\n")
                    : "") +
                (run.IsFinished ? "本局已结束。"
                    : "本次居民得分将在结算后显示。"));
        }

        private string BalanceScoreTooltip()
        {
            if (run == null) return string.Empty;
            if (!run.HasBalanceScore)
                return L("This older save keeps its original scoring rules; shared score starts with a new run.",
                    "此旧存档沿用原计分；新开一局才记录共栖分。");
            if (run.UsesOverallScore)
            {
                if (run.ScoringVersion >= 13)
                    return L(
                        $"Total {run.OverallScore} = shared {run.BalanceScore} + build {run.PlacementScore} + goals {run.SpecialTaskScore}. Each shared edge scores once: home with workshop/restaurant +24; home with green/market/square +16; green with green/market/square +12. Street–green −40; waste–home −80; each collision −60. A fully equipped neighbourhood earns +240 once, after its resident actually completes the daily cycle. Scores can be negative. Old runs keep their saved rules.",
                        $"总分 {run.OverallScore} = 共栖 {run.BalanceScore} + 建造 {run.PlacementScore} + 目标 {run.SpecialTaskScore}。每条共边结算一次：住宅配工作坊/餐厅 +24，配绿地/超市/广场 +16；绿地配绿地/超市/广场 +12。街道贴绿地 −40，垃圾间贴住宅 −80，每次车祸 −60。配套齐全且居民实际完成循环的街坊，一次奖励 240 分。允许负分；旧对局沿用原规则。");
                if (run.ScoringVersion >= 12)
                    return L(
                        $"Total {run.OverallScore} = shared {run.BalanceScore} + build {run.PlacementScore} + goals {run.SpecialTaskScore}. Build pairs score only across a shared edge. Connected green milestones give +20 and +40 once per patch. A complete neighbourhood gives +120 only after a resident finishes a work, meal and home cycle. The local ranking uses this total.",
                        $"总分 {run.OverallScore} = 共栖 {run.BalanceScore} + 建造 {run.PlacementScore} + 目标牌 {run.SpecialTaskScore}。建造组合只计算共边格；连续绿地首次达到 2 格加 20 分，达到至少 3 格且有两种植物加 40 分。配套齐全的住宅须有居民完成上班、用餐、回家循环，才获得一次 120 分。本地排行使用总分。");
                return L(
                    $"Total {run.OverallScore} = shared {run.BalanceScore} + build {run.PlacementScore} + tasks {run.SpecialTaskScore}. Build points are awarded once for existing neighbours in the eight surrounding plots." +
                    (run.UsesCompleteNeighborhoods
                        ? " A home beside a workshop, restaurant, supermarket, square, and two different greens earns +24 once."
                        : "") +
                    " Daily animal and resident activity remains in shared score. Local ranking uses total score.",
                    $"总分 {run.OverallScore} = 共栖 {run.BalanceScore} + 建造 {run.PlacementScore} + 任务 {run.SpecialTaskScore}。新牌与八邻格的建造联动只结算一次。" +
                    (run.UsesCompleteNeighborhoods
                        ? "住宅旁凑齐工作坊、餐厅、超市、广场和两种不同绿地，额外得 24 分。"
                        : "") +
                    "动物和居民每日活动仍计入共栖分；本地排行采用总分。");
            }
            if (!run.HasAdditiveCoexistenceScore)
                return L(
                    $"Older shared score {run.BalanceScore}: each day added the lower of animal and resident points. Animal {run.AnimalScore}, resident {run.HumanScore} are shown separately; this older total is not their sum.",
                    $"旧共栖分 {run.BalanceScore}：每天取动物分与居民分中较低的一个并累计。动物 {run.AnimalScore}、居民 {run.HumanScore} 分别显示；旧共栖分并非两者之和。");
            return L(
                $"Shared score {run.BalanceScore} = animal {run.AnimalScore} + resident {run.HumanScore}.\n" +
                (run.IsFinished ? "This run has ended."
                    : "The next shared score appears after settlement.") +
                (run.HasLinkageScore
                    ? (run.ScoringVersion >= 8
                        ? "\nResidents can feed wildlife and protect nearby roads through square visits; two fed species near a home give that resident +2. Shared food adds up to +6/day and wildlife neighbours up to +4/day, already included in resident points."
                        : run.ScoringVersion >= 7
                        ? "\nNearby available food areas link wildlife and residents: square +3, supermarket +2, green +1; one best per resident, max +6/day. Already in resident points."
                        : "\nShared-food linkage is already in resident points: plaza +3, supermarket +2, green +1; one best per resident, max +6/day.")
                    : "\nCheck both totals: their sum alone does not show whether both sides thrive."),
                $"共栖得分 {run.BalanceScore} = 动物 {run.AnimalScore} + 居民 {run.HumanScore}。\n" +
                (run.IsFinished ? "本局已结束。"
                    : "本次共栖得分将在结算后显示。") +
                (run.HasLinkageScore
                    ? (run.ScoringVersion >= 8
                        ? "\n居民到访广场能给动物食物并降低附近道路风险；住宅旁有两种已觅食动物，居民+2。共享食源每天最多+6，动物邻居每天最多+4，均已计入居民分。"
                        : run.ScoringVersion >= 7
                        ? "\n附近可用食源使动物与居民联动：广场+3、超市+2、绿地+1；每人只取最高一项，每日最多+6，已计入居民分。"
                        : "\n共享食物联动已计入居民分：广场+3、超市+2、绿地+1；每人只取最高一项，每日最多+6。")
                    : "\n请同时关注两项分数：总和本身不代表双方都受益。"));
        }

        private static string CompactScore(int value, bool chinese)
        {
            var magnitude = Math.Abs((long)value);
            if (magnitude < 100000)
                return value.ToString(CultureInfo.InvariantCulture);
            var divisor = chinese
                ? magnitude >= 100000000 ? 100000000d : 10000d
                : magnitude >= 1000000000 ? 1000000000d
                    : magnitude >= 1000000 ? 1000000d : 1000d;
            var suffix = chinese
                ? magnitude >= 100000000 ? "亿" : "万"
                : magnitude >= 1000000000 ? "B"
                    : magnitude >= 1000000 ? "M" : "K";
            var truncated = Math.Truncate(value / divisor * 10d) / 10d;
            return truncated.ToString("0.#", CultureInfo.InvariantCulture) + suffix;
        }

        private string YesterdayWorkTooltip()
        {
            var report = run?.LastHumanDay;
            if (report == null) return L("No day has settled yet.", "还没有昨日结算。");
            var biodiversity = run.ScoringVersion >= 8
                ? report.residents.Sum(person => person.biodiversityBonus) : 0;
            return L(
                $"Day {report.day}: {report.CompletedWorkCycles}/{report.ResidentCount} residents completed work, a meal and the return home.\n" +
                $"Worked {report.WorkedCount} · ate {report.MealsEaten} · square visits {report.SquareVisits}." +
                (run.HasLinkageScore
                    ? run.ScoringVersion >= 8
                        ? $"\nCoexistence +{report.LinkagePoints} (food +{report.LinkagePoints - biodiversity}, wildlife neighbours +{biodiversity}) is included in resident points."
                        : $"\n{(run.ScoringVersion >= 7 ? "Nearby food-area" : "Shared-food")} linkage +{report.LinkagePoints} is included in resident points."
                    : ""),
                $"第 {report.day} 天：{report.CompletedWorkCycles}/{report.ResidentCount} 人完成工作、用餐及返家。\n" +
                $"上班 {report.WorkedCount} 人 · 用餐 {report.MealsEaten} 人 · 到访广场 {report.SquareVisits} 人。" +
                (run.HasLinkageScore
                    ? run.ScoringVersion >= 8
                        ? $"\n共栖联动 +{report.LinkagePoints}（食源 +{report.LinkagePoints - biodiversity}、动物邻居 +{biodiversity}），已计入居民分。"
                        : $"\n{(run.ScoringVersion >= 7 ? "附近食源" : "共享食物")}联动 +{report.LinkagePoints}，已计入居民分。"
                    : ""));
        }

        private string YesterdayWasteTooltip()
        {
            var report = run?.LastWasteDay;
            if (report == null) return L("No day has settled yet.", "还没有昨日结算。");
            if (run.UsesUncappedWaste)
                return L(
                    $"Day {report.day}: {report.Cleared} waste portions cleared; {report.Remaining} remain.\n" +
                    "Each waste room clears all waste in its eight neighbouring plots or reached by continuous streets, including new waste from this day.",
                    $"第 {report.day} 天：清运垃圾 {report.Cleared} 份，剩余 {report.Remaining} 份。\n" +
                    "每间垃圾间清运周围八格，或经连续共边街道接通的全部垃圾，包含当天新产生的垃圾。");
            return L(
                $"Day {report.day}: {report.Cleared} old waste portions cleared; {report.Remaining} remain.\n" +
                "A waste room clears up to 2 portions each day from neighbouring plots or through continuous streets.",
                $"第 {report.day} 天：清运旧垃圾 {report.Cleared} 份，剩余 {report.Remaining} 份。\n" +
                "每间垃圾间每天可清运周围八格，或经连续共边街道接通的旧垃圾，最多 2 份。");
        }

        private string DailyAnimalScoreTooltip()
        {
            var ecology = run?.LastEcologyDay;
            if (ecology == null) return string.Empty;
            if (run.UsesLegacyAnimalScoring)
                return L($"Day {ecology.day}: {ecology.LegacyAnimalMeals} under old rules.\n" +
                    "Fox hunts were included in this saved score.",
                    $"第 {ecology.day} 天：旧规则 {ecology.LegacyAnimalMeals} 分。\n" +
                    "此存档曾将狐狸捕食计入得分。");
            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            if (run.HasFedAnimalScore)
            {
                var used = ecology.fedScoringSourceTileIds ?? new List<string>();
                var fedGreen = used.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category == ConstructionCategory.Green);
                var fedUrban = used.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category is
                        ConstructionCategory.Square or
                        ConstructionCategory.Supermarket);
                var fedPenalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
                var fedGross = run.LastScoreDay?.animalPoints ?? ecology.animalPoints;
                return L(
                    $"Settlement {ecology.day}: animal {fedGross - fedPenalty:+#;-#;0}.\n" +
                    $"Fed animals {ecology.AnimalMeals}×4 · green sources {fedGreen}×2 · square/shop sources {fedUrban}×1" +
                    (fedPenalty > 0 ? $" · collision −{fedPenalty}" : "") + ".",
                    $"第 {ecology.day} 次结算：动物 {fedGross - fedPenalty:+#;-#;0} 分。\n" +
                    $"实际进食 {ecology.AnimalMeals} 只×4 · 供食绿地 {fedGreen} 块×2 · 广场/超市 {fedUrban} 块×1" +
                    (fedPenalty > 0 ? $" · 车祸 −{fedPenalty}" : "") + "。");
            }
            if (run.ScoringVersion >= 7)
            {
                var scored = ecology.scoredFoodTileIds;
                var greenSources = scored?.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category == ConstructionCategory.Green) ?? 0;
                var squareSources = scored?.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category == ConstructionCategory.Square) ?? 0;
                var marketSources = scored?.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category == ConstructionCategory.Supermarket) ?? 0;
                var wasteSources = scored?.Count(id => built.TryGetValue(id,
                    out var tile) && tile.category == ConstructionCategory.Waste) ?? 0;
                var trafficPenalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
                var grossPoints = run.LastScoreDay?.animalPoints ?? ecology.animalPoints;
                return L(
                    $"Day {ecology.day}: animal {grossPoints - trafficPenalty:+#;-#;0} points" +
                    (trafficPenalty > 0 ? $" (food areas +{grossPoints}, collision −{trafficPenalty})" : "") + ".\n" +
                    $"Distinct nearby sources: green {greenSources}×1 · square {squareSources}×4 · supermarket {marketSources}×3 · waste {wasteSources}×1.\n" +
                    $"Meals {ecology.AnimalMeals} keep animals alive; hunts do not score.",
                    $"第 {ecology.day} 天：动物 {grossPoints - trafficPenalty:+#;-#;0} 分" +
                    (trafficPenalty > 0 ? $"（附近食源 +{grossPoints}，车祸 −{trafficPenalty}）" : "") + "。\n" +
                    $"周围不同食源：绿地 {greenSources}×1 · 广场 {squareSources}×4 · 超市 {marketSources}×3 · 垃圾间 {wasteSources}×1。\n" +
                    $"实际进食 {ecology.AnimalMeals} 次关系到存活；狐狸捕食不加分。");
            }
            var urban = ecology.urbanMealTileIds ?? new List<string>();
            var market = urban.Count(id => built.TryGetValue(id, out var tile) &&
                tile.category == ConstructionCategory.Supermarket);
            var square = urban.Count(id => built.TryGetValue(id, out var tile) &&
                tile.category == ConstructionCategory.Square);
            var waste = urban.Count - market - square;
            var green = Math.Max(0, ecology.AnimalMeals - urban.Count);
            var penalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
            var gross = run.LastScoreDay?.animalPoints ?? ecology.animalPoints;
            var net = gross - penalty;
            return L($"Day {ecology.day}: {net:+#;-#;0} animal points" +
                (penalty > 0 ? $" (food +{gross}, collision −{penalty})" : "") + ".\n" +
                $"Green {green}×3 · market {market}×2 · square {square}×2 · waste {waste}×1\n" +
                "Meals score when eaten; hunts do not score.",
                $"第 {ecology.day} 天：动物 {net:+#;-#;0} 分" +
                (penalty > 0 ? $"（觅食 +{gross}，车祸 −{penalty}）" : "") + "。\n" +
                $"绿地 {green}×3 · 超市 {market}×2 · 广场 {square}×2 · 垃圾间 {waste}×1\n" +
                "实际吃到食物才加分；狐狸捕食不加分。");
        }

        private List<string> DayIssues(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human, ConstructionDayForecast forecast)
        {
            var issues = new List<string>();
            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            if (ecology.trafficSpecies != ConstructionTrafficSpecies.None)
            {
                var location = TileCoordinate(built, ecology.trafficStreetTileId);
                var penalty = run.LastScoreDay?.trafficPenaltyPoints ?? 0;
                issues.Add(L($"Traffic: 1 {TrafficSpeciesName(ecology.trafficSpecies, false)} died at {location}" +
                        (penalty > 0 ? $"; animal −{penalty}." : "."),
                    $"车祸：{TrafficSpeciesName(ecology.trafficSpecies, true)} 1 只（{location}）" +
                        (penalty > 0 ? $"；动物 −{penalty}。" : "。")));
            }
            if (ecology.predatedSpecies != ConstructionPreySpecies.None)
            {
                var location = TileCoordinate(built, ecology.predationTileId);
                issues.Add(L($"Fox: 1 {PreyName(ecology.predatedSpecies, false)} hunted at {location}.",
                    $"狐狸捕食：{PreyName(ecology.predatedSpecies, true)} 1 只（{location}）。"));
            }
            if (forecast.blockedHomeTileIds.Count > 0)
            {
                var location = TileCoordinate(built, forecast.blockedHomeTileIds[0]);
                issues.Add(L($"Waste blocked {forecast.blockedHomeTileIds.Count} home(s); first at {location}.",
                    $"垃圾阻断住宅 {forecast.blockedHomeTileIds.Count} 间，首处 {location}。"));
            }
            if (forecast.blockedFoodTileIds.Count > 0 &&
                human.MealsEaten < human.ResidentCount)
            {
                var location = TileCoordinate(built, forecast.blockedFoodTileIds[0]);
                issues.Add(L($"Waste closed food at {location}.",
                    $"餐饮点 {location} 因垃圾停用。"));
            }
            if (human.restaurantRejectedForCapacity > 0)
                issues.Add(L($"Food seats full: {human.restaurantRejectedForCapacity} missed meals.",
                    $"餐位不足：{human.restaurantRejectedForCapacity} 人未用餐。"));
            if (human.CompletedWorkCycles < human.ResidentCount)
                issues.Add(L($"Incomplete resident cycles: {human.ResidentCount - human.CompletedWorkCycles}.",
                    $"居民未完成循环 {human.ResidentCount - human.CompletedWorkCycles} 人。"));
            if (ecology.squirrelsFed < ecology.squirrelsPresent)
                issues.Add(L($"Squirrel food short: {ecology.squirrelsFed}/{ecology.squirrelsPresent} fed.",
                    $"松鼠缺食：进食 {ecology.squirrelsFed}/{ecology.squirrelsPresent}。"));
            if (run.PigeonArrivalDay > 0 && run.PigeonArrivalDay <= ecology.day &&
                ecology.pigeonsFed < ecology.pigeonsPresent)
                issues.Add(L($"Pigeon food short: {ecology.pigeonsFed}/{ecology.pigeonsPresent} fed.",
                    $"鸽群缺食：进食 {ecology.pigeonsFed}/{ecology.pigeonsPresent}。"));
            return issues;
        }

        private static string PreyName(ConstructionPreySpecies species, bool chinese) =>
            species switch
            {
                ConstructionPreySpecies.Pigeon => chinese ? "鸽子" : "pigeon",
                ConstructionPreySpecies.Squirrel => chinese ? "松鼠" : "squirrel",
                ConstructionPreySpecies.Hedgehog => chinese ? "刺猬" : "hedgehog",
                _ => chinese ? "动物" : "animal"
            };

        private static string TrafficSpeciesName(ConstructionTrafficSpecies species,
            bool chinese) => species switch
        {
            ConstructionTrafficSpecies.Squirrel => chinese ? "松鼠" : "squirrel",
            ConstructionTrafficSpecies.Pigeon => chinese ? "鸽子" : "pigeon",
            ConstructionTrafficSpecies.Hedgehog => chinese ? "刺猬" : "hedgehog",
            ConstructionTrafficSpecies.Fox => chinese ? "狐狸" : "fox",
            _ => chinese ? "动物" : "animal"
        };

        private void RefreshResidentDispatch(ConstructionHumanDayResult humanPreview,
            ConstructionEcologyDayResult ecologyPreview)
        {
            if (residentDispatchPanel == null) return;
            var validHome = !run.IsFinished &&
                routeSubject == RouteSubject.Resident &&
                !string.IsNullOrEmpty(selectedHomeId) &&
                run.HasResident(selectedHomeId);
            openDispatchButton.gameObject.SetActive(validHome);
            openDispatchButton.GetComponentInChildren<Text>().text =
                run.UsesFillHand
                    ? L("Set work & meals", "安排工作与用餐")
                    : L("Dispatch →", "调度 →");
            if (!validHome) residentDispatchOpen = false;
            residentDispatchPanel.gameObject.SetActive(residentDispatchOpen &&
                validHome && !hasPendingPlacement && !buildPaletteOpen);
            if (!residentDispatchPanel.gameObject.activeSelf) return;

            var built = run.BuiltTiles.ToDictionary(tile => tile.id);
            if (!built.TryGetValue(selectedHomeId, out var home))
            {
                residentDispatchOpen = false;
                residentDispatchPanel.gameObject.SetActive(false);
                return;
            }
            var preference = run.GetResidentDispatch(selectedHomeId);
            var workOptions = run.DispatchWorkOptions(selectedHomeId);
            var mealOptions = run.DispatchMealOptions(selectedHomeId);
            dispatchHeading.text = L("RESIDENT DISPATCH", "居民调度");
            dispatchHome.text = L($"Home {home.column + 1},{home.row + 1}",
                $"住宅 · 第 {home.column + 1} 列、第 {home.row + 1} 行");
            dispatchRules.text = L("Separate from building · change freely",
                "独立于建造 · 随时可以调换");
            dispatchWorkHeading.text = L("WORK", "上班地点");
            dispatchMealHeading.text = L("MEAL", "用餐地点");
            dispatchAutoLabel.text = L("Both auto", "两项自动");
            dispatchCloseLabel.text = L("Close", "关闭");
            dispatchWorkChoice.text = DispatchChoiceLabel(preference?.workTileId,
                built, workOptions.Count);
            dispatchMealChoice.text = DispatchChoiceLabel(preference?.mealTileId,
                built, mealOptions.Count);
            dispatchWorkPrevious.interactable = dispatchWorkNext.interactable =
                workOptions.Count > 0;
            dispatchMealPrevious.interactable = dispatchMealNext.interactable =
                mealOptions.Count > 0;

            var resident = humanPreview.residents.FirstOrDefault(person =>
                person.homeTileId == selectedHomeId);
            var actualWork = resident?.worked == true
                ? DispatchChoiceLabel(resident.workTileId, built, 0)
                : L("No job", "无岗位");
            var actualMeal = resident?.ate == true
                ? DispatchChoiceLabel(resident.mealTileId, built, 0)
                : L("No meal", "无餐位");
            dispatchPreviewHeading.text = run.UsesFillHand
                ? L("NEXT SETTLEMENT · THIS HOME", "下次结算 · 本户动线")
                : L("TOMORROW · THIS HOME", "明日实际动线 · 本户");
            dispatchPreview.text = L(
                $"Work: {actualWork}\nMeal: {actualMeal} · return home",
                $"上班：{actualWork}\n用餐：{actualMeal} · 返回住宅");
            dispatchBreakdown.text = resident == null
                ? L("No resident in this home yet.", "本户尚无居民。")
                : !resident.completedCycle
                    ? L("Cycle incomplete: a job or meal is missing.\nChoose a reachable destination.",
                        "循环未完成：缺少岗位或餐位。\n请改选可达地点。")
                    : L("Work and meal route connected.\n" +
                        (resident.visitedSquare ? "Square visit available." :
                            "No square visit on this route."),
                        "上班和用餐路线已接通。\n" +
                        (resident.visitedSquare ? "可到访广场。" :
                            "当前路线未到访广场。"));
            dispatchBreakdownDetails = resident == null
                ? dispatchBreakdown.text
                : L($"Direct {(resident.routeBonus > 0 ? "✓" : "·")} · mature green {(resident.greenNeighbourBonus > 0 ? "✓" : "·")} · short street {(resident.shortStreetBonus > 0 ? "✓" : "·")}\n" +
                    $"Square {(resident.visitedSquare ? "✓" : "·")} · wildlife link {(resident.linkageBonus + resident.biodiversityBonus > 0 ? "✓" : "·")}. Points appear after settlement.",
                    $"就近{(resident.routeBonus > 0 ? "✓" : "·")} · 成熟绿地{(resident.greenNeighbourBonus > 0 ? "✓" : "·")} · 短街道{(resident.shortStreetBonus > 0 ? "✓" : "·")}\n" +
                    $"广场{(resident.visitedSquare ? "✓" : "·")} · 动物联动{(resident.linkageBonus + resident.biodiversityBonus > 0 ? "✓" : "·")}；得分结算后显示。");
            dispatchScoreHeading.text = L("WHOLE TOWN · ROUTE STATUS",
                "全城动线状态");
            dispatchAnimalScore.text = L($"Fed {ecologyPreview?.AnimalMeals ?? 0}",
                $"动物进食 {ecologyPreview?.AnimalMeals ?? 0}");
            dispatchResidentScore.text = L(
                $"Work {humanPreview.WorkedCount}/{humanPreview.ResidentCount}",
                $"上班 {humanPreview.WorkedCount}/{humanPreview.ResidentCount}");
            dispatchSharedScore.text = L(
                $"Cycle {humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}",
                $"循环 {humanPreview.CompletedWorkCycles}/{humanPreview.ResidentCount}");
            dispatchSharedScore.gameObject.SetActive(true);
            dispatchAnimalScore.rectTransform.anchoredPosition =
                new Vector2(-96, -187);
            dispatchResidentScore.rectTransform.anchoredPosition =
                new Vector2(0, -187);
            dispatchAnimalScore.rectTransform.sizeDelta =
            dispatchResidentScore.rectTransform.sizeDelta =
                new Vector2(92, 27);
            var workFallback = !string.IsNullOrEmpty(preference?.workTileId) &&
                preference.workTileId != resident?.workTileId;
            var mealFallback = !string.IsNullOrEmpty(preference?.mealTileId) &&
                preference.mealTileId != resident?.mealTileId;
            dispatchNote.color = workFallback || mealFallback
                ? BlockedRed : workOptions.Count == 0 || mealOptions.Count == 0
                    ? IdleAmber : Ink;
            dispatchNote.text = workFallback || mealFallback
                ? L("Choice unavailable; route updated.",
                    "首选不可用，已显示实际动线。")
                : workOptions.Count == 0 || mealOptions.Count == 0
                    ? L("Build a reachable destination.",
                        "建造可达的服务设施。")
                    : L("Changes saved automatically.",
                        "调整已自动保存。");
        }

        private string DispatchChoiceLabel(string tileId,
            IReadOnlyDictionary<string, ConstructionTileData> built,
            int optionCount)
        {
            if (string.IsNullOrEmpty(tileId))
                return optionCount == 0 ? L("Auto · no choice yet", "自动 · 暂无可选") :
                    L("Auto", "自动安排");
            return built.TryGetValue(tileId, out var tile)
                ? L($"{TileLabel(tile)} {tile.column + 1},{tile.row + 1}",
                    $"{TileLabel(tile)} {tile.column + 1},{tile.row + 1}")
                : L("Unavailable destination", "地点已不可用");
        }

        private void SetRouteSuccess(bool success)
        {
            routeSuccessIcon.gameObject.SetActive(success);
            // Keep the two section titles on one left edge when there is no
            // seal. Reserve its width only when a verified route shows it.
            routeHeading.rectTransform.anchoredPosition =
                new Vector2(success ? -30 : -50, 25);
            routeHeading.rectTransform.sizeDelta =
                new Vector2(success ? 150 : 180, 28);
        }

        private void RefreshRouteInspection(ConstructionHumanDayResult humanPreview)
        {
            SetRouteSuccess(false);
            foreach (Transform child in routeOverlay)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            if (routeSubject == RouteSubject.None)
            {
                var hasHomes = run.ResidentPopulation > 0;
                var opening = !IsEndless &&
                    run.CurrentDay == 1 && !run.IsFinished;
                routeText.text = opening
                    ? L("Click the central oak to inspect tomorrow's squirrel food route.",
                        "点击中央橡树，查看松鼠明日的食物路线。")
                    : hasHomes
                    ? L("Click a home to inspect routes and dispatch its resident to work and meals.",
                        "点击住宅查看路线，并安排居民的上班、用餐地点。")
                    : L("Click a green tile with wildlife to preview its food or hunting route.",
                        "点击有动物的绿地，查看明日觅食或捕猎路线。");
                routePanel.gameObject.SetActive(!run.UsesFillHand &&
                    (hasHomes || observationLedger.gameObject.activeSelf));
                NormalizeFillRouteText();
                return;
            }
            routePanel.gameObject.SetActive(!run.UsesFillHand);
            if (run.IsFinished)
            {
                routeText.text = L(
                    "This run has ended. There is no next-day route to preview.",
                    "本局已结束，不再预览次日路线。");
                NormalizeFillRouteText();
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
                    NormalizeFillRouteText();
                    return;
                }
                route = resident.routeTileIds.Where(built.ContainsKey)
                    .Select(id => built[id]).ToArray();
                var availability = run.PreviewWasteAvailability();
                if (availability.BlockedHomes.Contains(selectedHomeId))
                    routeText.text = L(
                        "NEXT DAY · RESIDENT\nWaste at home blocks the outing. Build cleanup nearby.",
                        "明日 · 居民\n住宅垃圾阻断外出，请在附近建垃圾间。");
                else if (!IsEndless &&
                         run.TutorialChapter == ConstructionTutorialChapter.Restaurant &&
                         !resident.ate)
                    routeText.text = L(
                        "NEXT DAY · RESIDENT\nNo meal route. Add a reachable restaurant.",
                        "明日 · 居民\n无用餐路线；请建可到达的餐厅。");
                else
                {
                    var work = resident.worked
                        ? L($"work {TileCoordinate(built, resident.workTileId)}",
                            $"上班 {TileCoordinate(built, resident.workTileId)}") :
                        !run.ResidenceCanReachWork(selectedHomeId)
                            ? L("no work route", "无法到可达岗位") :
                        ReachableWorkClosedByWaste(selectedHomeId,
                            availability.BlockedFoodServices)
                            ? L("job closed by waste", "岗位因垃圾停用")
                            : L("work full", "岗位已满");
                    var meal = resident.ate
                        ? L($"meal {TileCoordinate(built, resident.mealTileId)}",
                            $"用餐 {TileCoordinate(built, resident.mealTileId)}") :
                        !run.ResidenceCanReachFood(selectedHomeId)
                            ? L("no meal route", "无法到餐饮点")
                            : ReachableFoodClosedByWaste(selectedHomeId,
                                availability.BlockedFoodServices)
                                ? L("meal closed by waste", "餐饮点因垃圾停用")
                                : L("meal full", "餐位已满");
                    var square = resident.visitedSquare
                        ? L($"square {TileCoordinate(built, resident.squareTileId)}",
                            $"广场 {TileCoordinate(built, resident.squareTileId)}")
                        : string.Empty;
                    routeText.text = string.IsNullOrEmpty(square)
                        ? L($"NEXT DAY · RESIDENT\n{work} · {meal} · returns home",
                            $"明日 · 居民\n{work} · {meal} · 回家")
                        : L($"NEXT DAY · RESIDENT\n{work} · {meal}\n{square} · returns home",
                            $"明日 · 居民\n{work} · {meal}\n{square} · 回家");
                }
                SetRouteSuccess(resident.worked &&
                    resident.ate && !availability.BlockedHomes.Contains(
                        selectedHomeId));
            }
            else if (routeSubject is RouteSubject.Hedgehog or RouteSubject.Fox)
            {
                var ecology = run.PreviewEcologyDay();
                if (selectedWildlifeTileId == null ||
                    !built.TryGetValue(selectedWildlifeTileId, out origin))
                {
                    routeSubject = RouteSubject.None;
                    routeText.text = L("No animal is currently here.",
                        "这里目前没有动物。");
                    NormalizeFillRouteText();
                    return;
                }
                if (routeSubject == RouteSubject.Hedgehog)
                {
                    route = ecology.hedgehogFed
                        ? run.GreenFoodRouteTo(origin.id,
                            ecology.hedgehogFoodTileId)
                        : Array.Empty<ConstructionTileData>();
                    routeText.text = ecology.hedgehogFed && route.Count > 0
                        ? L($"NEXT DAY · HEDGEHOG · food at " +
                            $"{TileCoordinate(built, ecology.hedgehogFoodTileId)}",
                            $"明日 · 刺猬 · 觅食至" +
                            $"{TileCoordinate(built, ecology.hedgehogFoodTileId)}")
                        : L("NEXT DAY · HEDGEHOG · no reachable food",
                            "明日 · 刺猬 · 无可达食物");
                    SetRouteSuccess(ecology.hedgehogFed && route.Count > 0);
                    DrawRoute(route, origin, colour);
                }
                else
                {
                    route = ecology.foxHuntRouteTileIds
                        .Where(built.ContainsKey).Select(id => built[id])
                        .ToArray();
                    var hunt = ecology.predatedSpecies !=
                        ConstructionPreySpecies.None && route.Count > 0;
                    routeText.text = hunt
                        ? L($"NEXT DAY · FOX · hunting route to " +
                            $"{TileCoordinate(built, ecology.predationTileId)}",
                            $"明日 · 狐狸 · 预计捕猎至" +
                            $"{TileCoordinate(built, ecology.predationTileId)}")
                        : L("NEXT DAY · FOX · no hunt planned",
                            "明日 · 狐狸 · 暂无预计捕猎路线");
                    SetRouteSuccess(hunt);
                    DrawRoute(route, origin,
                        new Color(0.93f, 0.43f, 0.29f));
                }
                NormalizeFillRouteText();
                return;
            }
            else
            {
                var ecology = run.PreviewEcologyDay();
                var squirrel = routeSubject == RouteSubject.Squirrel;
                var originId = selectedWildlifeTileId;
                if (originId == null || !built.TryGetValue(originId, out origin))
                {
                    routeSubject = RouteSubject.None;
                    routeText.text = L("No animal is currently here.",
                        "这里目前没有动物。");
                    NormalizeFillRouteText();
                    return;
                }
                var squirrelHomes = squirrel
                    ? run.VisibleSquirrelHomeTiles(
                        run.UsesFillHand ? int.MaxValue :
                            ConstructionWorldView.MaxVisibleAnimalsPerSpecies)
                    : Array.Empty<ConstructionTileData>();
                var squirrelRoutes = Enumerable.Range(0,
                        ecology.squirrelFoodTileIds.Count)
                    .Where(index => ecology.squirrelMealHomeTileIds != null &&
                            index < ecology.squirrelMealHomeTileIds.Count
                            ? ecology.squirrelMealHomeTileIds[index] == originId
                            : index < squirrelHomes.Count &&
                              squirrelHomes[index].id == originId)
                    .Select(index => run.GreenFoodRouteTo(originId,
                        ecology.squirrelFoodTileIds[index]))
                    .Where(candidate => candidate.Count > 0).ToArray();
                route = squirrel
                    ? squirrelRoutes.FirstOrDefault() ??
                      Array.Empty<ConstructionTileData>()
                    : run.PreviewPigeonFoodRoute();
                var target = route.Count > 0 ? route[route.Count - 1] : null;
                var oakCount = run.MatureNearbyOakCount;
                var nextSquirrelOaks = ConstructionFoodModel.OaksForNextSquirrel(
                    ecology.squirrelsPresent);
                var oakProgress = nextSquirrelOaks > run.MaximumNearbyOakCount
                    ? L($"habitat limit: {oakCount}/{run.MaximumNearbyOakCount} nearby oaks",
                        $"邻近橡树上限 {oakCount}/{run.MaximumNearbyOakCount}，无法再增松鼠")
                    : L($"next: oaks {oakCount}/{nextSquirrelOaks}",
                        $"下只需橡树 {oakCount}/{nextSquirrelOaks}");
                var meadowCount = run.MatureConnectedMeadowCount;
                var meadowProgress = L($"Next pigeon: {meadowCount}/" +
                    $"{ConstructionFoodModel.MeadowsForNextPigeon(ecology.pigeonsPresent)} " +
                    "connected mature meadows",
                    $"吸引下一只鸽子：相连成熟草地 {meadowCount}/" +
                    $"{ConstructionFoodModel.MeadowsForNextPigeon(ecology.pigeonsPresent)}");
                var squirrelsHere = squirrelHomes.Count(home =>
                    home.id == originId);
                var squirrelsFedHere = squirrelRoutes.Length;
                routeText.text = squirrel
                    ? target == null
                        ? L($"NEXT DAY · SQUIRRELS {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent} fed\n" +
                            $"Here {squirrelsFedHere}/{squirrelsHere} · no food",
                            $"明日松鼠进食 {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent}\n" +
                            $"此处 {squirrelsFedHere}/{squirrelsHere} · 无可达食物")
                        : L($"NEXT DAY · SQUIRRELS {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent} fed\n" +
                            $"Here {squirrelsFedHere}/{squirrelsHere} · {oakProgress}",
                            $"明日松鼠进食 {ecology.squirrelsFed}/" +
                            $"{ecology.squirrelsPresent}\n" +
                            $"此处 {squirrelsFedHere}/{squirrelsHere} · {oakProgress}")
                    : target == null
                        ? L($"NEXT DAY · PIGEONS · can eat {ecology.pigeonsFed}/" +
                            $"{ecology.pigeonsPresent} · no reachable food\n{meadowProgress}",
                            $"明日 · 鸽群 {ecology.pigeonsFed}/{ecology.pigeonsPresent} " +
                            $"可进食 · 无可达食物\n{meadowProgress}")
                        : L($"NEXT DAY · PIGEONS · can eat {ecology.pigeonsFed}/" +
                            $"{ecology.pigeonsPresent} from " +
                            $"{ecology.pigeonFoodTileIds.Count} source(s)\n{meadowProgress}",
                            $"明日 · 鸽群 {ecology.pigeonsFed}/{ecology.pigeonsPresent} " +
                            $"可进食 · {ecology.pigeonFoodTileIds.Count} 处食源\n{meadowProgress}");
                SetRouteSuccess(squirrel
                    ? squirrelsHere > 0 && squirrelsFedHere == squirrelsHere
                    : ecology.pigeonsPresent > 0 &&
                      ecology.pigeonsFed == ecology.pigeonsPresent);
                var routes = squirrel ? squirrelRoutes :
                    run.PreviewPigeonFoodRoutes();
                if (routes.Count > 0)
                {
                    foreach (var foodRoute in routes)
                        DrawRoute(foodRoute, origin, colour);
                    NormalizeFillRouteText();
                    return;
                }
            }
            DrawRoute(route, origin, colour);
            NormalizeFillRouteText();
        }

        private void NormalizeFillRouteText()
        {
            if (run?.UsesFillHand != true || routeText == null) return;
            routeText.text = FillHandTiming(routeText.text);
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

        private bool ReachableWorkClosedByWaste(string homeId,
            IReadOnlyCollection<string> blockedServices)
        {
            var reachable = run.BuiltTiles.Where(tile =>
                ConstructionHumanModel.DailyJobCapacity(tile.category) > 0 &&
                run.ResidenceRouteTo(homeId, tile.id).Count > 0).ToArray();
            return reachable.Length > 0 && reachable.All(tile =>
                blockedServices.Contains(tile.id));
        }

        private void DrawRoute(IReadOnlyList<ConstructionTileData> route,
            ConstructionTileData origin, Color colour)
        {
            // A meal in the animal's own tile has no travel path. The old
            // overlapping destination and origin squares looked like a stuck
            // selection marker, and a missing route needs no origin marker.
            if (route == null || route.Count <= 1) return;
            var seen = new HashSet<string>();
            for (var index = 1; index < route.Count; index++)
            {
                var first = route[index - 1];
                var second = route[index];
                if (!ConstructionBoardModel.AreCardinalNeighbours(first.column,
                        first.row, second.column, second.row)) continue;
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
            if (route.Count > 0)
            {
                var target = Panel("Food destination", routeOverlay,
                    BoardPoint(route[route.Count - 1]), new Vector2(22, 22),
                    colour);
                target.raycastTarget = false;
            }
            var marker = Panel("Route origin", routeOverlay,
                BoardPoint(origin), new Vector2(12, 12), Ink);
            marker.raycastTarget = false;
        }

        private Vector2 BoardPoint(ConstructionTileData tile) =>
            new((tile.column - viewMinColumn - 3) * BoardCellSpacing,
                (3 - tile.row + viewMinRow) * BoardCellSpacing);

        public bool FocusPredationRisk()
        {
            if (run == null || hasPendingPlacement ||
                string.IsNullOrEmpty(predationRiskTileId)) return false;
            var tile = run.BuiltTiles.FirstOrDefault(item =>
                item.id == predationRiskTileId);
            if (tile == null) return false;
            focusedPredationTileId = predationRiskTileId;
            if (run.Mode == ConstructionGameMode.Endless)
            {
                StopViewportUiAnimations();
                viewMinColumn = tile.column - 3;
                viewMinRow = tile.row - 3;
            }
            Refresh();
            return true;
        }

        public bool FocusLatestBuiltRoom()
        {
            if (run == null || run.Mode != ConstructionGameMode.Endless ||
                run.BuiltCount == 0) return false;
            var latest = run.BuiltTiles[run.BuiltCount - 1];
            StopViewportUiAnimations();
            ClearPendingPlacement();
            viewMinColumn = latest.column - 3;
            viewMinRow = latest.row - 3;
            Refresh();
            return true;
        }

        private void StopViewportUiAnimations()
        {
            if (growthAnimation != null)
            {
                StopCoroutine(growthAnimation);
                growthAnimation = null;
            }
            if (residentMealAnimation == null) return;
            StopCoroutine(residentMealAnimation);
            residentMealAnimation = null;
            if (movingResidentMarker != null)
                movingResidentMarker.SetActive(false);
        }

        public bool Pan(int columns, int rows)
        {
            if (run == null || run.Mode != ConstructionGameMode.Endless) return false;
            var nextColumn = (long)viewMinColumn + columns;
            var nextRow = (long)viewMinRow + rows;
            if (nextColumn < int.MinValue + 6 || nextColumn > int.MaxValue - 6 ||
                nextRow < int.MinValue + 6 || nextRow > int.MaxValue - 6)
                return false;
            if (nextColumn == viewMinColumn && nextRow == viewMinRow) return false;
            StopViewportUiAnimations();
            ClearPendingPlacement();
            viewMinColumn = (int)nextColumn;
            viewMinRow = (int)nextRow;
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
            if (IsEndless)
            {
                // The replacement is written only after the player confirms
                // the new ending limits in the rules card.
                OpenEndlessRules(true);
                return;
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
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                return;
            }
            confirmReset = false;
            resetLabel.text = L("New run", "重新开始");
            if (resultResetLabel != null) resultResetLabel.text = resetLabel.text;
            if (resultNoticeText != null) resultNoticeText.text = "";
            var newRun = new ConstructionRunModel(run.Mode,
                useFillHand: run.Mode == ConstructionGameMode.Fill);
            if (store != null &&
                !store.TrySaveConfirmedNewRun(newRun, out _))
            {
                feedbackText.text = L(
                    "Could not save the new run; current run was not reset.",
                    "新局存档失败；原进度未被重置。");
                if (resultNoticeText != null)
                    resultNoticeText.text = L("New run could not be saved. Run not reset.",
                        "存档失败，未重新开始。");
                return;
            }
            if (newRun.UsesFillHand != run.UsesFillHand)
            {
                // Rebuild the shorter hand layout while keeping the Main scene,
                // its physical board and the camera alive across the reload.
                if (!ConstructionSceneTransition.ReloadPreparedRun(newRun, store))
                    feedbackText.text = L("Could not restart this run.",
                        "无法重新开始本局。");
                return;
            }
            StartPreparedRun(newRun);
        }

        private void StartPreparedRun(ConstructionRunModel newRun)
        {
            resultReviewOpen = false;
            if (localRankingsOverlay != null) localRankingsOverlay.SetActive(false);
            if (networkRankingsOverlay != null) networkRankingsOverlay.SetActive(false);
            if (awardStandardsOverlay != null) awardStandardsOverlay.SetActive(false);
            StopScoreMotion();
            SymbiosisAudioDirector.Play(SymbiosisSound.RestartConfirm);
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
            ClearFixedInfoSelection();
            runSavedAtLeastOnce = true;
            runNeedsSave = false;
            viewMinColumn = 0;
            viewMinRow = 0;
            routeSubject = RouteSubject.None;
            selectedHomeId = null;
            selectedWildlifeTileId = null;
            residentDispatchOpen = false;
            selectedCategory = ConstructionCategory.Green;
            selectedPlanting = GreenPlanting.None;
            tutorialGreenChoiceMade = false;
            freeBuildChoiceMade = false;
            endlessChoiceMade = false;
            introChoiceMade = false;
            openingTourStep = 0;
            openingOakInspected = false;
            feedbackText.text = run.UsesFillHand
                ? L("New run: place the central oak to begin.",
                    "新局开始：先放置中央橡树。")
                : L("New run: one oak and one squirrel.",
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
            if (store == null) return;
            runNeedsSave = true;
            if (!store.TrySave(run))
                feedbackText.text = L("Save failed. Current changes may be lost on exit.",
                    "存档失败。退出后可能丢失本次改动。");
            else
            {
                runSavedAtLeastOnce = true;
                runNeedsSave = false;
            }
        }

        // Building a room advances TutorialChapter immediately. Keep the
        // current day's lesson on that room until the player advances time.
        private static ConstructionTutorialChapter TutorialLessonChapter(
            ConstructionRunModel current)
        {
            if (current.Mode == ConstructionGameMode.Endless ||
                current.CurrentDay < 2 ||
                current.CurrentDay > ConstructionTutorialGuide.RoomCount + 1 ||
                !current.BuiltToday)
                return current.TutorialChapter;
            var builtToday = current.BuiltTiles.FirstOrDefault(tile =>
                tile.id != ConstructionBoardModel.StarterOakId &&
                tile.builtDay == current.CurrentDay);
            return builtToday?.category switch
            {
                ConstructionCategory.Green => ConstructionTutorialChapter.Green,
                ConstructionCategory.Residence => ConstructionTutorialChapter.Residence,
                ConstructionCategory.Restaurant => ConstructionTutorialChapter.Restaurant,
                ConstructionCategory.Workshop => ConstructionTutorialChapter.Workshop,
                ConstructionCategory.Street => ConstructionTutorialChapter.Street,
                ConstructionCategory.Waste => ConstructionTutorialChapter.Waste,
                ConstructionCategory.Supermarket => ConstructionTutorialChapter.Supermarket,
                ConstructionCategory.Square => ConstructionTutorialChapter.Square,
                _ => current.TutorialChapter
            };
        }

        private string TutorialRoomStory(ConstructionTutorialChapter chapter,
            bool firstOak) => firstOak
                ? L("Wildlife lived here first. Keep the central oak as a home and food source for the squirrel.",
                    "这里原本是动物的家。先在中央留下橡树，让松鼠有家，也有食物。")
                : chapter switch
                {
                    ConstructionTutorialChapter.InspectOak =>
                        L("The oak and squirrel were here before the town. Watch how the squirrel finds food.",
                            "橡树和松鼠比城市更早在这里。先看看松鼠怎样找到食物。"),
                    ConstructionTutorialChapter.Green =>
                        L("Wildlife needs more than one tree. Link green plots to leave room for shelter and food.",
                            "动物需要的不只是一棵树。让绿地相连，留下栖身和觅食的空间。"),
                    ConstructionTutorialChapter.Residence =>
                        L("A new resident would like to move in. Give them a home, then help them find work and a meal.",
                            "一位居民想搬来这里。先给他一处家，接下来还要帮他找到工作和餐食。"),
                    ConstructionTutorialChapter.Restaurant =>
                        L("The new resident needs somewhere to eat. A restaurant offers two meal seats and one job.",
                            "新居民每天都要吃饭。餐厅提供两个餐位，也提供一份工作。"),
                    ConstructionTutorialChapter.Workshop =>
                        L("More residents will need work. A workshop adds two jobs to the growing community.",
                            "社区慢慢热闹起来。工作坊增加两个岗位，让居民有更多工作机会。"),
                    ConstructionTutorialChapter.Street =>
                        L("Work and meals may be farther away. Connect a street, while watching for animals crossing it.",
                            "工作和餐食可能在更远处。接通街道，也要留意过路动物的安全。"),
                    ConstructionTutorialChapter.Waste =>
                        L("People's daily lives leave waste behind. A waste room clears nearby or street-connected waste.",
                            "人们的生活开始留下垃圾。垃圾间能清运周围八格，或经连续街道接通的垃圾。"),
                    ConstructionTutorialChapter.Supermarket =>
                        L("The restaurant may become busy. A supermarket adds seats; when someone eats there, wildlife may benefit too.",
                            "餐厅可能忙不过来。超市补充餐位；有人用餐时，附近动物也可能受益。"),
                    ConstructionTutorialChapter.Square =>
                        L("After work and meals, residents need a place to gather. A visited square can feed nearby wildlife.",
                            "工作与吃饭之外，居民也想有个相聚的地方。有人到访的广场能给附近动物留下食物。"),
                    _ => string.Empty
                };

        private string FreeBuildTip(ConstructionRunModel current) =>
            current.CurrentDay switch
            {
                10 when current.HasLinkageScore =>
                    current.ScoringVersion >= 8
                        ? L("Shared = animal + resident points. A visited square feeds wildlife and protects nearby streets; fed wildlife can lift residents. Hover the small link mark.",
                            "共栖分＝动物分＋居民分。有人到访的广场供动物觅食并保护附近街道；觅食成功的动物也能让附近居民加分。悬停小联动标记查看。")
                        : current.ScoringVersion >= 7
                        ? L("Shared = animal + resident points. Nearby food areas can benefit both; hover scores for rules.",
                            "共栖分＝动物分＋居民分。附近食源可让双方受益；悬停分数看规则。")
                        : L("Shared = animal + resident points. Shared food adds resident linkage points; hover for rules.",
                            "共栖分＝动物分＋居民分；共享食物可加居民联动分。悬停看规则。"),
                10 when current.HasAdditiveCoexistenceScore =>
                    L("Shared = animal + resident points. Hover scores for sources.",
                        "共栖分＝动物分＋居民分。悬停分数可看来源。"),
                10 => L("Older score rules apply. Hover the shared score for details.",
                    "旧存档沿用原计分；悬停共栖分查看规则。"),
                11 => L("Badge: green used, amber spare, red issue. Hover for cause.",
                    "角标：绿是充分，橙是余量，红需处理；悬停看原因。"),
                12 when current.UsesLegacyAnimalScoring =>
                    L("Older animal score rules apply. Hover its score for details.",
                    "旧存档沿用原动物计分；悬停动物分查看。"),
                12 when current.HasFedAnimalScore =>
                    L($"Each fed animal +{(current.ScoringVersion >= 12 ? 3 : 4)}; each green source used +{(current.ScoringVersion >= 12 ? 2 : current.ScoringVersion >= 11 ? 3 : 2)}, each square/shop source used +1. Waste adds no source bonus.",
                        $"每只实际吃到食物的动物 +{(current.ScoringVersion >= 12 ? 3 : 4)}；供食绿地 +{(current.ScoringVersion >= 12 ? 2 : current.ScoringVersion >= 11 ? 3 : 2)}，广场/超市 +1；垃圾食源不加分。"),
                12 when current.ScoringVersion >= 7 =>
                    L("Food areas near wildlife: green +1 · square +4 · shop +3 · waste +1. Meals keep animals alive.",
                        "动物附近的食源：绿地+1、广场+4、超市+3、垃圾+1。进食关系到存活。"),
                12 => L("Eat: green 3 · shop/plaza 2 · waste 1.",
                    "吃到：绿地3、超市/广场2、垃圾1。"),
                _ => L("Inspect actual meals, routes and waste before expanding.",
                    "继续发展前，先查看实际进食、路线与垃圾。")
            };

        private string FreeBuildActionHint(ConstructionRunModel current)
        {
            var waste = current.PreviewWasteAvailability();
            if (current.UsesUncappedWaste &&
                waste.BacklogAfterCleanup.Values.Any(amount => amount > 0))
                return L("Rooms with a waste icon still have uncollected garbage. Place a waste room nearby or connect one by street.",
                    "房间亮起垃圾图标，说明还有垃圾没运走。可在周围合法位置建垃圾间，或用连续街道接通。");
            if (waste.BlockedHomes.Count > 0 ||
                waste.BlockedFoodServices.Count > 0)
                return L("Waste blocks a home or closes a food service. Build a waste room nearby or connect one by street.",
                    "垃圾挡住了居民出门，或让餐饮点暂时停业。可在周围建垃圾间，或用连续街道接通。");
            var human = current.PreviewHumanDay();
            if (human.WorkedCount < human.ResidentCount)
                return L("Someone cannot work. Check home routes and jobs.",
                    "有人还没找到能到达的工作。看看住宅到岗位的路通不通。");
            if (human.MealsEaten < human.ResidentCount)
                return L("Someone lacks a meal. Add reachable food seats.",
                    "有人今天没吃上饭。补一个从住宅走得到的餐位吧。");
            var animals = current.PreviewEcologyDay();
            if (animals.squirrelsFed < animals.squirrelsPresent ||
                animals.pigeonsFed < animals.pigeonsPresent)
                return L("Wildlife lacks food. Check routes and add green.",
                    "有动物还没找到食物。看看觅食路线，也可以补些绿地。");
            if (current.HumanScore < current.AnimalScore)
                return L("To lift resident points, pair a home with jobs and meals.",
                    "想让居民过得更好？让住宅接上工作地点和餐位。");
            return L("To lift animal points, add reachable green food.",
                "想让动物过得更好？添些它们能到达的绿地食源。");
        }

        private string FirstWorkshopScoreLesson(ConstructionRunModel current)
        {
            var resident = current.LastHumanDay?.residents?.FirstOrDefault();
            if (current.CurrentDay != 5 || resident == null ||
                !current.HasBalanceScore)
                return L("Select a home, then Dispatch. Change work and meals any time.",
                    "点住宅，再点“居民调度”；上班和用餐地点可随时换。");
            if (!resident.completedCycle)
                return L("No full cycle yesterday. Select a home and use Dispatch to inspect work and meal routes.",
                    "昨日未完成循环。点住宅用“居民调度”检查上班和用餐路线。");

            var en = new List<string>();
            var zh = new List<string>();
            void Add(int points, string english, string chinese)
            {
                if (points <= 0) return;
                en.Add($"{english} {points}");
                zh.Add($"{chinese}{points}");
            }
            Add(resident.basePoints, "cycle", "循环");
            Add(resident.workplaceBonus, "job", "岗位");
            Add(resident.mealBonus, "meal", "餐食");
            Add(resident.routeBonus, "route", "直达");
            Add(resident.homeBonus, "home", "住宅");
            Add(resident.squareBonus, "square", "广场");
            return L($"Yesterday +{resident.satisfactionPoints}: " +
                    $"{string.Join(" + ", en)}. Home → Dispatch.",
                $"昨日居民+{resident.satisfactionPoints}：" +
                    $"{string.Join("＋", zh)}。点住宅“居民调度”。");
        }

        private string BoardGameInstruction()
        {
            if (run.CurrentDay == 1)
                return L(
                    "Put printed green tile 34 in the centre. Keep the board's four corner motifs visible, inspect the oak, then end Day 1.",
                    "将原图 34 号绿地方块放在中央，保持棋盘四角图案入镜；查看橡树后结束第 1 天。");
            if (run.BuiltToday)
                return L("Physical tile confirmed. Click Next day to continue.",
                    "实体棋子已确认。点击“下一天”继续。");
            if (run.IsRandomBoardGame)
                return L(
                    "Draw one face-down room tile. Place it on a legal plot and confirm. Redraw only if that type has no legal plot.",
                    "背面朝上随机抽一块房间；放在合法格并确认。只有该类完全无合法格时才重抽。");
            if (run.IsIntroPhase)
                return L(
                    $"Next: {CategoryLabel(run.IntroRequiredCategory.Value)}. Place its printed tile beside an eligible room, sharing an edge, hold still for 1.5 seconds, then confirm the preview.",
                    $"下一步：{CategoryLabel(run.IntroRequiredCategory.Value)}。把对应实体方块放在符合条件的已建格上下左右一格，静置 1.5 秒，再确认预览。");
            return L(
                "Add one printed room tile to the physical board. Hold still until the preview appears, then confirm it in the game.",
                "在实体棋盘增加一块房间方块。放稳后等待预览，再在游戏中确认。");
        }

        private string BoardGameShortInstruction()
        {
            if (run.CurrentDay == 1)
                return L("Start camera · show four corner motifs and centre tile 34.",
                    "启动摄像头 · 四角图案与中央 34 号绿地入镜。");
            if (run.BuiltToday)
                return L("Tile confirmed · click Next day.", "棋子已确认 · 点击“下一天”。");
            if (run.IsRandomBoardGame)
                return L("Draw one tile · legal plot · confirm.",
                    "抽一块 · 放合法格 · 确认。");
            return run.IsIntroPhase
                ? L($"Place {CategoryLabel(run.IntroRequiredCategory.Value)} on the physical board.",
                    $"在实体棋盘放置{CategoryLabel(run.IntroRequiredCategory.Value)}。")
                : L("Add one printed tile · hold still · confirm.",
                    "增加一块实体方块 · 放稳 · 确认。");
        }

        private string StoryShortInstruction(ConstructionRunModel current) =>
            current.BuiltToday
                ? L("Built 1/1 today. Click Next day to build again.",
                    "今日已建 1/1。点击“下一天”继续建造。")
                : current.TutorialChapter switch
            {
                ConstructionTutorialChapter.InspectOak => openingOakInspected
                    ? L("Click Next day to check the oak's food supply.",
                        "点“下一天”，查看橡树的食物结算。")
                    : L("You can click the oak for food; then end the day.",
                        "可点橡树查看食物，再点“下一天”。"),
                ConstructionTutorialChapter.Green =>
                    current.IsIntroPhase
                        ? L("Build the first green plot directly above, below, left or right of the central oak.",
                            "第一块绿地建在中央橡树上下左右四格之一。")
                        : L("No green room has been built yet. Add one.",
                            "还没有新建绿地，可先补上一块。"),
                ConstructionTutorialChapter.Residence =>
                    L("Build a home beside a built plot, sharing an edge. Corner contact does not connect.",
                        "住宅贴已建格的上下左右一侧；斜角不相连。"),
                ConstructionTutorialChapter.Restaurant =>
                    L("Place a restaurant beside a home or linked street, sharing an edge.",
                        "餐厅贴住宅或连通街道的上下左右一侧。"),
                ConstructionTutorialChapter.Workshop =>
                    L("Build the workshop beside a home or connected street, sharing an edge, to add a workplace.",
                        "工作坊贴住宅或已连通街道的上下左右一侧，增加上班地点。"),
                ConstructionTutorialChapter.Street =>
                    L("Extend a street across a shared edge from a home or connected street. Each new plot extends the route by one step.",
                        "街道从住宅或连通街道的上下左右一侧接出；每铺一格，路线延长一格。"),
                ConstructionTutorialChapter.Waste =>
                    L("Build a waste room beside a home, restaurant or home-linked street, sharing an edge.",
                        "垃圾间贴住宅、餐厅或住宅连通街道的上下左右一侧。"),
                ConstructionTutorialChapter.Supermarket =>
                    L("Build the supermarket beside a home or connected street, sharing an edge, to add meals.",
                        "超市贴住宅或已连通街道的上下左右一侧，增加就餐选择。"),
                ConstructionTutorialChapter.Square =>
                    L("Build a square beside a home or linked street, sharing an edge. Visitors leave wildlife food.",
                        "广场贴住宅或连通街道的上下左右一侧；居民到访会留下动物食物。"),
                _ => FreeBuildActionHint(current)
            };

        private string StoryRoomFunction(ConstructionRunModel current) =>
            TutorialLessonChapter(current) switch
            {
                ConstructionTutorialChapter.InspectOak =>
                    L("Hover oak badges. Click the oak to inspect food.",
                        "悬停橡树角标看含义；可点击查看食物。"),
                ConstructionTutorialChapter.Green =>
                    current.UsesGreenPatches
                        ? L("Edge-connected green plots form a habitat patch. Two mature meadows can support two pigeons; oaks support squirrels and shrubs support hedgehogs or foxes.",
                            "共边绿地组成栖息板块。板块内2块成熟草地可容纳2只鸽子；橡树养松鼠，灌木可引来刺猬或狐狸。")
                        : L("Meadow: pigeons and seeds tomorrow; seeds may recur. Oak: squirrels. Shrub: hedgehogs/foxes.",
                            "首块草地次日引来鸽子并产食，之后隔日有产食机会。橡树养松鼠，灌木可引来刺猬或狐狸。"),
                ConstructionTutorialChapter.Residence =>
                    L("One person moves in. Streets reach distant services. Work, eat, return: +1 resident point.",
                        "住进1人；远处工作和餐食要靠连续街道。上班、用餐、返家完成完整循环，居民得分＋1；住宅条件自动加分。"),
                ConstructionTutorialChapter.Restaurant =>
                    L("1 job and 2 meal seats/day. After a full cycle: restaurant work +1, meal +1.",
                        "每天1岗位、2餐位，可上班也可吃饭。完成循环后，餐厅上班＋1分、用餐再＋1分。"),
                ConstructionTutorialChapter.Workshop =>
                    L("2 jobs/day. Work, eat, return: workshop work +2 points. Assign it at home.",
                        "每天2岗位。居民在这里上班、吃饭并回家后，岗位额外＋2分；可在住宅的“居民调度”中选择。"),
                ConstructionTutorialChapter.Waste =>
                    current.UsesUncappedWaste
                        ? L("Clears all nearby or street-connected waste, including new waste today. Remaining piles show a waste issue icon but do not shut rooms down.",
                            "清运周围八格及连续街道接通的全部垃圾，包括当天新产生的。未清运的房间显示垃圾问题图标，但不会停摆。")
                        : L("Clears up to 2 old waste/day nearby or through continuous streets; clearing may leave 1 animal food. Piles block services.",
                            "每天清运周围八格及连续街道接通的旧垃圾，最多2份；清运可能留下1份动物食物，积压则阻碍居民或餐饮。"),
                ConstructionTutorialChapter.Street =>
                    L("Continuous streets reach distant jobs and meals and let waste rooms collect from distant sources. Gaps break routes; animals risk traffic.",
                        "连续街道让远处的岗位、餐食可达，也让垃圾间清运远处垃圾。街道不能跨空格；临路动物有车祸风险。"),
                ConstructionTutorialChapter.Supermarket =>
                    current.HasFedAnimalScore
                        ? L($"Two meal seats and one job. If wildlife eats food left by diners, each fed animal gains {(current.ScoringVersion >= 12 ? 3 : 4)}; the supermarket adds 1 source point once.",
                            $"提供 2 个餐位、1 个岗位。动物吃到用餐留下的食物时，每只得 {(current.ScoringVersion >= 12 ? 3 : 4)} 分；超市供食再得 1 分。")
                        : current.ScoringVersion >= 7
                        ? L("2 meal seats, 1 job/day. Resident dining activates nearby animal food (+3).",
                            "每天2 餐位、1 岗位。居民用餐激活附近动物食源+3；动物进食关系到存活。")
                        : L("2 meal seats and 1 job/day. Eases full restaurants; dining leaves 1 animal food.",
                            "超市每天提供2 餐位、1岗位；当天有人用餐就留1份动物食物。"),
                ConstructionTutorialChapter.Square =>
                    current.ScoringVersion >= 7
                        ? L("One fed worker may visit/day: resident +1 and a food area that nearby wildlife can use for score and linkage.",
                            "广场每天最多接待1名已上班用餐的居民：居民+1，并形成附近动物可利用的食源与联动分。")
                        : L("One fed worker may visit/day: +1 resident point and 1 animal food. Animal eating earns linkage.",
                            "广场每天最多接待1名完成上班和用餐的居民：居民分＋1，留下1份食物；动物实际吃到才加联动分。"),
                ConstructionTutorialChapter.Complete =>
                    FreeBuildTip(current),
                _ => string.Empty
            };

        private Sprite StoryCueSprite(ConstructionRunModel current)
        {
            var chapter = TutorialLessonChapter(current);
            var category = chapter switch
            {
                ConstructionTutorialChapter.InspectOak or
                    ConstructionTutorialChapter.Green => ConstructionCategory.Green,
                ConstructionTutorialChapter.Residence => ConstructionCategory.Residence,
                ConstructionTutorialChapter.Restaurant => ConstructionCategory.Restaurant,
                ConstructionTutorialChapter.Workshop => ConstructionCategory.Workshop,
                ConstructionTutorialChapter.Waste => ConstructionCategory.Waste,
                ConstructionTutorialChapter.Street => ConstructionCategory.Street,
                ConstructionTutorialChapter.Supermarket =>
                    ConstructionCategory.Supermarket,
                _ => ConstructionCategory.Square
            };
            if (chapter == ConstructionTutorialChapter.Complete)
                return ConstructionIconCatalog.GetControl(current.Mode ==
                    ConstructionGameMode.Endless
                        ? ConstructionControlIcon.EndlessMode
                        : ConstructionControlIcon.FillMode);
            var planting = chapter == ConstructionTutorialChapter.Green
                ? GreenPlanting.Meadow : GreenPlanting.Oak;
            return ConstructionIconCatalog.GetBuild(category, planting);
        }

        private string StoryInstruction(ConstructionRunModel run) =>
            TutorialLessonChapter(run) switch
        {
            ConstructionTutorialChapter.InspectOak =>
                openingOakInspected
                    ? L("End Day 1 to observe the squirrel's meal and the oak's remaining food.",
                        "结束第 1 天，核对松鼠是否进食、橡树还剩多少食物。")
                    : L("The starter oak and one squirrel are already here. Click the oak to inspect food; Day 1 ends with Next day.",
                        "初始橡树和一只松鼠已经在场。点击橡树查看食物；第 1 天点“下一天”结束。"),
            ConstructionTutorialChapter.Green =>
                run.IsIntroPhase
                    ? GreenPlacementAndWildlifeGuide()
                    : run.ResidentPopulation > 0
                    ? L("The squirrel was here first; the pigeons still need suitable green space. The starter oak is not a new build. Add green on a legal plot, then check for wildlife later.",
                        "松鼠已经在这里，鸽群还在等合适的绿地。初始橡树不算新建房间；选一处合法空格补上绿地，再观察动物变化。")
                    : L("The starter oak is not a new room. Build one green room on any legal plot. Connected meadow may support pigeons; check the next day.",
                        "初始橡树不算新建房间。任选合法空格新增绿地；相连草地可能引来鸽子，需在次日核对。"),
            ConstructionTutorialChapter.Residence =>
                L("PLACE: Build a residence beside an existing plot, sharing an edge. A resident can directly reach jobs, meals and squares only above, below, left or right; connected streets reach farther services. Corners alone do not connect.\nPURPOSE: One person moves in. Work, eat and return to complete a cycle for +1 resident point. Home bonuses then apply automatically when their conditions are met.",
                    "放置：住宅贴已建格的上下左右一侧。居民可直达共边的餐厅、工作坊、超市或广场；远处服务靠连续街道，斜角不连通。\n解决：住进1人。上班、吃饭、返家形成完整循环，居民得分＋1；住宅额外分按实际条件自动结算。"),
            ConstructionTutorialChapter.Restaurant =>
                L("PLACE: Beside a home or connected street, sharing an edge. Corner contact does not connect.\nPURPOSE: One job and two meal seats each day, so residents can work or eat here. The same worker may use a free seat. After a full cycle, the restaurant job and meal each add +1.",
                    "放置：餐厅贴住宅或已连通街道的上下左右一侧；斜角不连通。\n解决：每天1岗位、2餐位，居民可以在此上班或吃饭。同一个人也能在此上班和用餐。完成完整循环后，餐厅工作和用餐各额外＋1分。"),
            ConstructionTutorialChapter.Workshop =>
                L("PLACE: Beside a home or connected street, sharing an edge. Corner contact does not connect.\nPURPOSE: Two more jobs each day. When an assigned resident works, eats and returns home, a workshop job adds +2. Choose the job from the home in Resident dispatch.",
                    "放置：工作坊贴住宅或已连通街道的上下左右一侧；斜角不连通。\n解决：每天增加2个岗位。居民完成上班、吃饭、回家后，工作坊岗位额外＋2分；可点住宅打开“居民调度”安排。") +
                "\n" + FirstWorkshopScoreLesson(run),
            ConstructionTutorialChapter.Street =>
                L("PLACE: Extend each street across a shared edge from a home or connected street. Diagonal contact and empty gaps do not connect.\nPURPOSE: Continuous streets bring distant jobs and meals within reach and let waste rooms collect from distant sources. Streets never skip a plot. Animals can cross, but roadside animals face traffic risk.",
                    "放置：街道从住宅或已连通街道的上下左右一侧继续铺设；斜角和空格都不连通。\n作用：连续街道让远处岗位与餐位可达，也让垃圾间清运远处垃圾；街道不会跳格。动物能过路，但临路有车祸风险。"),
            ConstructionTutorialChapter.Waste =>
                run.UsesUncappedWaste
                    ? L("PLACE: Beside a home, restaurant or home-linked street, sharing an edge; corners alone do not connect.\nPURPOSE: Clears every waste pile in the eight surrounding plots or in homes and food services reached by continuous shared-edge streets, including new waste that day. Uncollected piles show an issue icon; they do not close rooms. Animals can cross the waste room.",
                        "放置：垃圾间贴住宅、餐厅或住宅连通街道的上下左右一侧；斜角不连通。\n作用：清运周围八格，以及经连续共边街道接通的住宅和餐饮建筑中的全部垃圾，包含当天新产生的。未清运的房间会显示问题图标，但不会停摆；动物可经过。")
                    : L("PLACE: Beside a home, restaurant or home-linked street, sharing an edge; corners alone do not connect.\nPURPOSE: Clears up to 2 old waste portions per day in the eight surrounding plots or in homes and food services reached by continuous shared-edge streets. Clearing waste may leave 1 temporary animal-food portion. Piles may stop residents leaving home or close food service. Animals can cross the waste room.",
                        "放置：垃圾间贴住宅、餐厅或住宅连通街道的上下左右一侧；斜角不连通。\n作用：每天清运周围八格，以及经连续共边街道接通的住宅和餐饮建筑中的旧垃圾，最多2份；可能留下1份临时动物食物。垃圾积压会阻碍居民出门或让餐饮停业；动物可经过。"),
            ConstructionTutorialChapter.Supermarket =>
                run.HasFedAnimalScore
                    ? L($"PLACE: Share an edge with a home or its connected street.\nPURPOSE: Two meal seats and one job per settlement help when the restaurant is full. Dining can leave food for nearby wildlife. Each animal that actually eats gains {(run.ScoringVersion >= 12 ? 3 : 4)} animal points; this supermarket adds 1 source point once. Nearby completed-cycle residents may gain 2 linkage points.",
                        $"放置：与住宅或其连通街道共边。\n作用：每次结算提供 2 个餐位、1 个岗位，缓解餐厅满座。有人用餐可能给附近动物留下食物；动物实际吃到时每只得 {(run.ScoringVersion >= 12 ? 3 : 4)} 分，超市本次供食再得 1 分。附近完成循环的居民还可能得 2 分联动分。")
                    : run.ScoringVersion >= 7
                    ? L("PLACE: Beside a home or its connected street, sharing an edge; corners alone do not connect.\nPURPOSE: 2 meal seats and 1 job each day help when the restaurant is full. A supermarket meal completes a cycle but adds +0 meal bonus, versus restaurant +1. Dining activates one animal-food area: a nearby occupied habitat gains animal +3, and a nearby completed-cycle resident may gain linkage +2.",
                        "放置：超市贴住宅或已连通街道的上下左右一侧；斜角不连通。\n解决：每天2餐位、1岗位，缓解餐厅满座；超市用餐可完成循环，但额外用餐分为0（餐厅为+1）。有人用餐时形成食源：周围有动物栖息地可得动物+3，附近完成循环的居民可得联动+2。")
                    : L("PLACE: Beside a home or its connected street.\nPURPOSE: 2 meal seats and 1 job each day help when the restaurant is full. A supermarket meal completes a cycle but adds +0 meal bonus, versus restaurant +1. If anyone dines here today, 1 temporary animal-food portion is left; linkage needs an animal to eat it.",
                        "放置：超市贴住宅或住宅连通街道。\n解决：每天2餐位、1岗位，缓解餐厅满座；超市用餐能完成循环，但额外用餐分为0（餐厅为＋1）。当天只要有人用餐，就留下1份临时动物食物；动物实际吃到才有联动分。"),
            ConstructionTutorialChapter.Square =>
                run.HasFedAnimalScore
                    ? L($"PLACE: In this lesson, share an edge with a home or its connected street.\nPURPOSE: One resident who worked and ate may visit each settlement for 1 resident point and leave food. Each nearby animal that actually eats gains {(run.ScoringVersion >= 12 ? 3 : 4)} animal points; the square adds 1 source point once. The visitor may gain 3 linkage points. Wildlife can cross the square.",
                        $"放置：这一步与住宅或其连通街道共边。\n作用：每次结算最多 1 名已上班、用餐的居民到访，得 1 分居民分并留下食物。附近动物实际吃到时每只得 {(run.ScoringVersion >= 12 ? 3 : 4)} 分，广场本次供食再得 1 分；访客还可能得 3 分联动分。动物可经过广场。")
                    : run.ScoringVersion >= 7
                    ? L("PLACE: In this lesson, beside a home or its connected street, sharing an edge; corners alone do not connect.\nPURPOSE: One resident who worked and ate can visit each day for resident +1. The visit activates a food area: nearby occupied wildlife habitat gains animal +4, and a nearby completed-cycle resident may gain linkage +3. Wildlife can cross the square.",
                        "放置：这一步广场贴住宅或已连通街道的上下左右一侧；斜角不连通。\n解决：每天最多1名完成上班和用餐的居民到访，居民+1并形成食源。附近有动物栖息地可得动物+4，完成循环的居民可得联动+3。动物可经过广场。")
                    : L("PLACE: In this lesson, beside a home or its connected street.\nPURPOSE: One resident who worked and ate can visit each day for +1 resident point and one temporary animal-food portion. Wildlife can cross the square; it earns animal points only if it eats that portion, which also adds resident linkage points.",
                        "放置：这一步广场须贴住宅或住宅连通街道。\n解决：完成上班、用餐的居民每天最多1人到访，居民分＋1并留下1份临时动物食物。动物可经过广场；实际吃到才得动物分，并增加居民联动分。"),
            ConstructionTutorialChapter.Complete =>
                run.BuiltToday
                    ? L("Today's room is built. Check actual meals, commutes and waste before advancing.",
                        "今天已建造。推进前查看实际进食、通勤和垃圾状况。")
                    : L("All eight types are open. Follow today's need, then choose a legal room and plot.",
                        "八类房间已开放。先看今日需求，再选合法房间与位置。"),
            _ => L("All 49 plots have a place in this story. See how the people and wildlife fared.",
                "四十九格都有了归属。看看生活在这里的人与动物过得如何。")
        };

        private string PlacementMessage(ConstructionPlacementFailure failure) => failure switch
        {
            ConstructionPlacementFailure.StarterOakMustBeCenter =>
                L("Place the first oak in the centre cell of the board.",
                    "第一张中央橡树只能放在棋盘正中格。"),
            ConstructionPlacementFailure.IntroTypeNotNext =>
                L($"Build {CategoryLabel(run.IntroRequiredCategory.Value)} next; this room unlocks later.",
                    $"下一步先建{CategoryLabel(run.IntroRequiredCategory.Value)}；这类房间稍后解锁。"),
            ConstructionPlacementFailure.IntroTypeAlreadyBuilt =>
                L("This room type has been explored once. Build the other types first; all eight reopen afterward.",
                    "这类房间已建过。按顺序完成八类后可再次建造。"),
            ConstructionPlacementFailure.Disconnected =>
                run.IsIntroPhase && run.IntroRequiredCategory == ConstructionCategory.Green &&
                !run.UsesFillHand
                    ? L("The first green must share an edge with the centre oak: above, below, left or right.",
                        "第一块绿地须建在中央橡树上下左右四格之一；斜角不连通。")
                    : L("Build beside an existing plot, sharing an edge. Diagonal contact does not connect.",
                        "须贴已建格的上下左右一侧；斜角不连通。"),
            ConstructionPlacementFailure.NeedsHomeOrConnectedStreet =>
                run.IsIntroPhase && run.IntroRequiredCategory ==
                    ConstructionCategory.Square
                    ? L("For this lesson, share an edge with a home or connected street. Residents can visit after a complete cycle.",
                        "这一步把广场与住宅或已连通街道共边放置。居民完成往返循环后才可到访。")
                    : L("This building must share an edge with a home or connected street.",
                        "这类建筑须贴住宅或已连通街道的上下左右一侧。"),
            ConstructionPlacementFailure.NeedsHomeOrRestaurant =>
                L("A waste room must share an edge with a home, restaurant or home-linked street.",
                    "垃圾间须贴住宅、餐厅或与住宅连通街道的上下左右一侧。"),
            ConstructionPlacementFailure.StreetNeedsHomeConnection =>
                L("A street must share an edge with a home or connected street.",
                    "街道须贴住宅或已接通街道的上下左右一侧。"),
            ConstructionPlacementFailure.Occupied =>
                L("That cell is already built.", "这格已有建筑。"),
            ConstructionPlacementFailure.TypeLimitReached =>
                L("This building type has reached its 49-cell limit. Choose another type.",
                    "此类建筑已达 49 格模式的数量上限，请选择其他建筑。"),
            ConstructionPlacementFailure.InvalidResidenceKind =>
                L("This residence marker is not supported. Build a standard residence.",
                    "该住宅标记不受支持，请建普通住宅。"),
            _ => L("This cell cannot be built here.", "这格不能建造所选建筑。")
        };

        private string TileLabel(ConstructionTileData tile)
        {
            if (run.UsesFillHand && tile.id == ConstructionBoardModel.StarterOakId)
                return L("central oak", "中央橡树");
            if (tile.category == ConstructionCategory.Green)
                return L($"{PlantingLabel(tile.planting)} green",
                    $"{PlantingLabel(tile.planting)}绿地");
            if (!run.UsesNeighborUpgrades ||
                ConstructionPlacementScore.UpgradeLevel(tile, run.BuiltTiles) < 2)
                return CategoryLabel(tile.category);
            return tile.category switch
            {
                ConstructionCategory.Residence => L("Garden home", "花园住宅"),
                ConstructionCategory.Restaurant => L("Terrace restaurant", "露台餐厅"),
                ConstructionCategory.Street => L("Slow street", "慢行街"),
                _ => CategoryLabel(tile.category)
            };
        }

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
            var icon = Panel("Resident icon", marker, Vector2.zero,
                new Vector2(32, 32), Color.white);
            icon.sprite = ConstructionIconCatalog.GetCharacter(
                ConstructionCharacterIcon.Resident);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return marker.gameObject;
        }

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
            // A direct ConstructionPrototype launch has no Main-scene listener.
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();
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
            if (InstructionEmphasis.AppliesTo(name))
            {
                bold = false;
                rect.gameObject.AddComponent<InstructionEmphasis>();
            }
            textWeights.Add(label, bold);
            label.font = Chinese ? chineseFont : bold ? boldFont : regularFont;
            label.fontStyle = Chinese && bold ? FontStyle.Bold : FontStyle.Normal;
            // Reading roles use a consistent floor; tiny map markers keep their own size.
            var readingMinimum = name switch
            {
                "Story preview" or "Room function" or "Route status" or
                "Day review text" or "Placement guide" or "Selected hand tagline" => 18,
                "Hand instruction" or "Goals completed" or "Goal state" or
                "Manual subtitle" or "Leaderboard status" or "Network leaderboard status" or
                "Local rankings subtitle" or "Local rankings hint" => 16,
                "Nickname value" or "Local nickname value" => 18,
                "Draw pile title" => 22,
                "Remaining cards on draw pile" => 20,
                _ => 0
            };
            label.fontSize = Mathf.Max(fontSize, readingMinimum);
            label.lineSpacing = 1.12f;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = value;
            LegacyTextTmpBridge.Attach(label);
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

        private void AddButtonIcon(Button button, ConstructionControlIcon kind)
        {
            var width = (button.transform as RectTransform).sizeDelta.x;
            var image = Panel("Action icon", button.transform,
                new Vector2(-width * 0.5f + 24f, 0f),
                new Vector2(27f, 27f), Color.white);
            image.sprite = ConstructionIconCatalog.GetControl(kind);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.rectTransform.anchoredPosition = new Vector2(13f, 0f);
            label.rectTransform.sizeDelta = new Vector2(width - 36f,
                label.rectTransform.sizeDelta.y);
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
