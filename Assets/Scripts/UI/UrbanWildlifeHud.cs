using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    public sealed class UrbanWildlifeHud : MonoBehaviour
    {
        private static readonly Color Graphite = new(0.10f, 0.12f, 0.14f, 0.94f);
        private static readonly Color WarmPaper = new(0.94f, 0.90f, 0.80f, 0.98f);
        private static readonly Color Cyan = new(0.27f, 0.78f, 0.74f, 1f);
        private static readonly Color MutedTrack = new(0.23f, 0.25f, 0.27f, 0.62f);
        private const float DesktopTransitionDuration = 1.8f;
        private const float DesktopEdgeFadeDuration = 0.5f;
        private const float GameplayHudFadeDuration = 0.3f;

        private readonly Dictionary<int, RoundedPanelGraphic> speedButtons = new();
        private readonly Dictionary<int, Text> speedButtonLabels = new();
        private Button skipDayButton;
        private Text skipDayLabel;
        private GameObject marketForecastPanel;
        private Text marketForecastLabel;
        private GameObject needRiskPanel;
        private Text needRiskSignal;
        private Button residentRiskButton;
        private Button animalRiskButton;
        private Text residentRiskLabel;
        private Text animalRiskLabel;
        private string residentRiskRoomId;
        private string animalRiskRoomId;
        private System.Action<string> riskRoomNavigation;
        private int riskRowLayout = -1;
        private AnimalNeedsController animalNeeds;
        private readonly Dictionary<WildlifeSpecies, Text> populationCounts = new();
        private readonly Dictionary<WildlifeSpecies, AnimalPopulationBadgeGraphic> populationBadges = new();
        private readonly Dictionary<WildlifeSpecies, Image> populationPortraits = new();
        private readonly Dictionary<WildlifeSpecies, Text> populationDeathTooltips = new();
        private readonly Dictionary<EcologicalMetricKind, CircularMeterGraphic> ecologicalMeters = new();
        private readonly Dictionary<EcologicalMetricKind, Text> ecologicalTooltips = new();
        private readonly Dictionary<EcologicalMetricKind, Text> ecologicalValueBadges = new();
        private readonly Dictionary<EcologicalMetricKind, Color> ecologicalColors = new();
        private const int FoodLocationsPerPage = 7;
        private GameObject foodLocationsPanel;
        private Text foodLocationsTitle;
        private Text foodLocationsHint;
        private Text foodLocationsEmpty;
        private Text foodLocationsPageLabel;
        private Button foodLocationsPrevious;
        private Button foodLocationsNext;
        private readonly Button[] foodLocationButtons = new Button[FoodLocationsPerPage];
        private readonly Text[] foodLocationNames = new Text[FoodLocationsPerPage];
        private readonly Text[] foodLocationDetails = new Text[FoodLocationsPerPage];
        private readonly Text[] foodLocationCounts = new Text[FoodLocationsPerPage];
        private readonly string[] foodLocationRoomIds = new string[FoodLocationsPerPage];
        private readonly List<FoodLocationEntry> foodLocationEntries = new();
        private readonly List<FoodBadgeTarget> foodBadgeTargets = new();
        private NaturalFoodController naturalFood;
        private bool foodLocationsOpen;
        private int foodLocationsPage;
        private GameObject foodBadgeTooltip;
        private RectTransform foodBadgeTooltipRect;
        private Text foodBadgeTooltipText;

        private Font font;
        private HideFlags generatedHideFlags;
        private Camera worldCamera;
        private GameRuntimeController runtime;
        private BoardCameraController boardCamera;
        private RoomLayoutEditorController layoutEditor;
        private RectTransform gameplayRoot;
        private CanvasGroup gameplayCanvasGroup;
        private DayNightDialGraphic clockDial;
        private Text dayNumber;
        private GameObject pauseOverlay;
        private GameObject pausePanel;
        private RectTransform pausePanelRect;
        private GameObject settingsPanel;
        private GameObject restartPauseAction;
        private RectTransform desktopPauseActionRect;
        private GameObject restartConfirmationPanel;
        private bool restartConfirmationOpen;
        private GameObject cameraCalibrationOverlay;
        private RawImage cameraCalibrationPreview;
        private Button cameraCalibrationStartButton;
        private Image cameraCalibrationStartBacking;
        private readonly Image[] cameraCalibrationCells = new Image[49];
        private GameObject cameraRecognitionFeedbackRoot;
        private RectTransform cameraRecognitionFeedbackRect;
        private Image cameraRecognitionScanFrame;
        private Image cameraRecognitionConfirmationFrame;
        private RectTransform cameraInvalidPlacementRoot;
        private readonly List<Image> cameraInvalidPlacementFrames = new();
        private CameraRecognitionFeedbackState previousCameraRecognitionState;
        private float cameraRecognitionConfirmationUntil;
        private GameObject desktopOverlay;
        private CanvasGroup desktopCanvasGroup;
        private GameObject roomContext;
        private RectTransform roomContextRect;
        private Image roomContextIcon;
        private GameObject roomTypeChip;
        private GameObject roomFunctionChip;
        private GameObject roomUseChip;
        private Image roomFunctionIcon;
        private Image roomUseIcon;
        private Text roomTypeTooltipText;
        private Text roomFunctionTooltipText;
        private Text roomUseTooltipText;
        private readonly List<RectTransform> roomContextTooltipRects = new();
        private Transform selectedRoomTransform;
        private RoomSpec selectedRoomSpec;
        private GameObject roomHoverLabel;
        private RectTransform roomHoverRect;
        private CanvasGroup roomHoverCanvasGroup;
        private Text roomHoverText;
        private RoomSpec hoveredRoom;
        private Transform hoveredRoomTransform;
        private bool hoverRequested;
        private float hoverElapsed;
        private float hoverAlpha;
        private Slider volumeSlider;
        private Text pauseTitle;
        private Text continueLabel;
        private Text settingsLabel;
        private Text languageLabel;
        private Text restartLabel;
        private Text desktopLabel;
        private Text helpLabel;
        private Text restartConfirmationTitle;
        private Text restartConfirmationMessage;
        private Text restartConfirmLabel;
        private Text restartCancelLabel;
        private Font restartChineseFont;
        private Text settingsTitle;
        private Text volumeLabel;
        private Image muteIcon;
        private Text muteLabel;
        private Text cameraCalibrationLabel;
        private Text backLabel;
        private GameObject desktopSandboxCard;
        private GameObject desktopResearchCard;
        private RectTransform desktopSandboxCardRect;
        private RectTransform desktopResearchCardRect;
        private RectTransform desktopBestRecordRect;
        private Text desktopSandboxLabel;
        private Text desktopResearchLabel;
        private Text desktopBestRecordLabel;
        private Text desktopSettingsLabel;
        private Text desktopLanguageLabel;
        private Text desktopExitLabel;
        private Text settingsLanguageLabel;
        private GameObject sandboxDifficultyOverlay;
        private Text sandboxDifficultyTitle;
        private Text sandboxDifficultyHint;
        private Text sandboxDifficultyBackLabel;
        private Text sandboxDifficultyContinueLabel;
        private Text sandboxDifficultyStartLabel;
        private Text sandboxDifficultySummary;
        private GameObject sandboxDifficultyContinueButton;
        private RectTransform sandboxDifficultyBackRect;
        private RectTransform sandboxDifficultyStartRect;
        private bool sandboxDifficultyReplacePending;
        private readonly Text[] sandboxDifficultyOptions = new Text[3];
        private readonly RoundedPanelGraphic[] sandboxDifficultyOptionBackings = new RoundedPanelGraphic[3];
        private readonly Text[] sandboxDifficultyRowLabels = new Text[4];
        private readonly Text[] sandboxDifficultyValues = new Text[4];
        private EndlessDifficultySettings sandboxDifficultyDraft =
            EndlessDifficultySettings.Preset(EndlessDifficulty.Standard);
        private Coroutine desktopTransitionCoroutine;
        private bool desktopStateKnown;
        private bool previousDesktopState;
        private GameObject enterEditButtonObject;
        private RoundedPanelGraphic enterEditBacking;
        private Text enterEditLabel;
        private GameObject editToolbar;
        private GameObject layoutImpactPanel;
        private Text layoutImpactText;
        private Text layoutImpactScopeText;
        private Text layoutImpactPageLabel;
        private Button layoutImpactPreviousButton;
        private Button layoutImpactNextButton;
        private readonly LayoutImpactCard[] layoutImpactCards = new LayoutImpactCard[2];
        private int layoutImpactPage;
        private bool layoutImpactWasEditing;
        private GameObject hedgehogNightPanel;
        private Text hedgehogNightText;
        private HedgehogForagingController hedgehogForaging;
        private GameObject dailyOutcomePanel;
        private Text dailyOutcomeText;
        private DailyOutcomeController dailyOutcome;
        private Image trayRoomIcon;
        private Text editStatus;
        private Button rotateEditButton;
        private Button confirmEditButton;
        private WasteCollectionNotification wasteCollectionNotification;
        private RectTransform workforceCounterRect;
        private Text workforceCounterText;
        private ResidentStatusOverlay residentStatusOverlay;
        private ResidentPopulationController residentPopulation;
        private Text residentCountText;
        private OakTreeLifecycleController oakTreeLifecycle;
        private AnimalPopulationController animalPopulation;
        private AnimalMortalityController animalMortality;
        private EndlessBalanceController endlessBalance;
        private EcologicalMetricsController ecologicalMetrics;
        private bool? lastEcologicalLanguageChinese;
        private CircularMeterGraphic mortalityMeter;
        private Text mortalityTooltip;
        private Text mortalityCountText;
        private ResearchSessionController researchSession;
        private FirstRunOnboardingController onboardingController;
        private CircularMeterGraphic researchDurationRing;
        private Text researchDurationTooltip;
        private GameObject researchSetupOverlay;
        private InputField researchCodeInput;
        private Text researchDurationValue;
        private Text researchDeathLimitValue;
        private Text researchSetupTitle;
        private Text researchParticipantLabel;
        private Text researchStartLabel;
        private Text researchBackLabel;
        private Button oakPlantButton;
        private Text oakPlantLabel;
        private PlayerFeedingController playerFeeding;
        private GameObject feedingModeButtonObject;
        private Button feedingModeButton;
        private RoundedPanelGraphic feedingModeButtonBacking;
        private FeedingModeIconGraphic feedingModeIcon;
        private Text feedingModeLabel;
        private GameObject feedingModeHint;
        private Text feedingModeHintLabel;
        private float feedingWasteNoticeUntil;
        private string feedingWasteRoomId;
        private int feedingWastePortions;
        private bool feedingWasteRouted;
        private int spatialForecastDay = -1;
        private int spatialForecastMovementCount = -1;
        private int spatialForecastGreenCells;
        private int spatialForecastSeeds;
        private int spatialForecastSeedMealCeiling = -1;
        private int spatialForecastLivingPigeons = -1;

        public RectTransform LayoutConfirmButtonRect =>
            confirmEditButton != null ? confirmEditButton.GetComponent<RectTransform>() : null;
        public RectTransform ClockDialRect =>
            clockDial != null ? clockDial.transform.parent as RectTransform : null;
        public RectTransform WorkforceCounterRect => workforceCounterRect;
        public RectTransform LayoutEditButtonRect =>
            enterEditButtonObject != null ? enterEditButtonObject.GetComponent<RectTransform>() : null;
        public RectTransform FeedingModeButtonRect =>
            feedingModeButton != null ? feedingModeButton.GetComponent<RectTransform>() : null;
        public RectTransform GetEcologicalIndicatorRect(EcologicalMetricKind kind) =>
            ecologicalMeters.TryGetValue(kind, out var meter) && meter != null
                ? meter.transform.parent as RectTransform
                : null;
        public RectTransform AnimalDeathIndicatorRect =>
            mortalityMeter != null ? mortalityMeter.transform.parent as RectTransform : null;

        public void Build(
            Camera camera,
            Font uiFont,
            HideFlags hideFlags,
            GameRuntimeController runtimeController,
            BoardCameraController cameraController)
        {
            worldCamera = camera;
            font = uiFont;
            generatedHideFlags = hideFlags;
            runtime = runtimeController;
            boardCamera = cameraController;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 20;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            gameplayRoot = CreateEmpty("Minimal Gameplay HUD", transform);
            Stretch(gameplayRoot);
            gameplayCanvasGroup = gameplayRoot.gameObject.AddComponent<CanvasGroup>();
            BuildClock();
            BuildIndicators();
            BuildFoodLocationsPanel();
            BuildNeedRiskPanel();
            BuildMarketForecast();
            BuildPopulation();
            BuildHedgehogNightReport();
            BuildDailyOutcomeReport();
            BuildRoomHoverLabel();
            BuildFoodBadgeTooltip();
            BuildRoomContext();
            BuildLayoutEditingControls();
            BuildPauseMenu();
            if (BuildVariantSettings.UsesCameraRecognition)
            {
                BuildCameraCalibrationOverlay();
                BuildCameraRecognitionFeedback();
            }
            BuildDesktopOverlay();
            BuildEventSystem();

            runtime.StateChanged += Refresh;
            runtime.RestartRequested += InvalidateSpatialForecast;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.StateChanged -= Refresh;
                runtime.RestartRequested -= InvalidateSpatialForecast;
            }
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged -= RefreshLayoutEditor;
                layoutEditor.LayoutConfirmed -= InvalidateSpatialForecast;
                layoutEditor.LayoutRestored -= InvalidateSpatialForecast;
            }
            if (animalNeeds != null)
                animalNeeds.StateChanged -= RefreshNeedRisk;
            if (oakTreeLifecycle != null)
            {
                oakTreeLifecycle.StateChanged -= RefreshSelectedRoomContext;
            }
            if (animalPopulation != null)
            {
                animalPopulation.StateChanged -= RefreshAnimalPopulation;
            }
            if (animalMortality != null)
            {
                animalMortality.StateChanged -= RefreshMortalityIndicator;
            }
            if (endlessBalance != null)
            {
                endlessBalance.StateChanged -= RefreshEndlessBalance;
            }
            if (ecologicalMetrics != null)
            {
                ecologicalMetrics.StateChanged -= RefreshEcologicalMetrics;
            }
            if (residentPopulation != null)
            {
                residentPopulation.StateChanged -= RefreshResidentPopulation;
            }
            if (researchSession != null)
            {
                researchSession.StateChanged -= RefreshResearchSession;
            }
            if (playerFeeding != null)
            {
                playerFeeding.FeedingModeChanged -= RefreshFeedingControls;
                playerFeeding.LeftoverFoodDiscarded -= HandleLeftoverFoodDiscarded;
                playerFeeding.StateChanged -= RefreshFoodBadgeTargets;
            }
            if (naturalFood != null)
                naturalFood.StateChanged -= RefreshFoodBadgeTargets;
            if (hedgehogForaging != null)
            {
                hedgehogForaging.NightReportChanged -= RefreshHedgehogNightReport;
            }
            if (dailyOutcome != null)
            {
                dailyOutcome.StateChanged -= RefreshDailyOutcome;
            }
        }

        public void BindLayoutEditor(RoomLayoutEditorController editor)
        {
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged -= RefreshLayoutEditor;
                layoutEditor.LayoutConfirmed -= InvalidateSpatialForecast;
                layoutEditor.LayoutRestored -= InvalidateSpatialForecast;
            }

            layoutEditor = editor;
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged += RefreshLayoutEditor;
                layoutEditor.LayoutConfirmed += InvalidateSpatialForecast;
                layoutEditor.LayoutRestored += InvalidateSpatialForecast;
            }

            InvalidateSpatialForecast();
            RefreshLayoutEditor();
        }

        public void BindHedgehogForaging(HedgehogForagingController controller)
        {
            if (hedgehogForaging != null)
            {
                hedgehogForaging.NightReportChanged -= RefreshHedgehogNightReport;
            }
            hedgehogForaging = controller;
            if (hedgehogForaging != null)
            {
                hedgehogForaging.NightReportChanged += RefreshHedgehogNightReport;
            }
            RefreshHedgehogNightReport();
        }

        public void BindDailyOutcome(DailyOutcomeController controller)
        {
            if (dailyOutcome != null)
            {
                dailyOutcome.StateChanged -= RefreshDailyOutcome;
            }
            dailyOutcome = controller;
            if (dailyOutcome != null)
            {
                dailyOutcome.StateChanged += RefreshDailyOutcome;
            }
            RefreshDailyOutcome();
        }

        private void BuildDailyOutcomeReport()
        {
            var panel = CreatePanel("Daily Outcome Report", gameplayRoot,
                new Color(0.10f, 0.12f, 0.14f, 0.82f));
            panel.Image.raycastTarget = false;
            RoundSolidPanel(panel, 14f);
            dailyOutcomePanel = panel.GameObject;
            panel.RectTransform.anchorMin = panel.RectTransform.anchorMax = Vector2.zero;
            panel.RectTransform.pivot = Vector2.zero;
            panel.RectTransform.anchoredPosition = new Vector2(20f, 224f);
            panel.RectTransform.sizeDelta = new Vector2(470f, 104f);
            dailyOutcomeText = CreateText(panel.Transform, "Daily Outcome Summary",
                string.Empty, 14, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            dailyOutcomeText.raycastTarget = false;
            dailyOutcomeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(dailyOutcomeText.rectTransform, 14f, 8f);
            dailyOutcomePanel.SetActive(false);
        }

        private void RefreshDailyOutcome()
        {
            if (dailyOutcomePanel == null || runtime == null)
            {
                return;
            }
            var report = dailyOutcome?.Model.LastReport ?? default;
            var visible = runtime.HasActiveRun && !runtime.AtDesktop && !runtime.LayoutEditing &&
                          report.Day > 0 && runtime.Clock.DayNumber == report.Day + 1;
            dailyOutcomePanel.SetActive(visible);
            if (!visible)
            {
                return;
            }
            if (runtime.Language == InterfaceLanguage.Chinese)
            {
                var workers = report.WorkingResidentsKnown
                    ? $"{report.WorkingResidents}"
                    : "未记录";
                var meals = report.MealsKnown
                    ? $" · 实际进食 {report.FedAnimals}/{report.LivingAnimals}"
                    : string.Empty;
                var pigeons = report.MealsKnown
                    ? $"鸽子 {report.FedPigeons}/{report.LivingPigeons} · 剩余种子 {report.SeedPortionsLeft} · "
                    : string.Empty;
                var causes = new List<string>();
                if (report.StarvationDeaths > 0) causes.Add($"饿 {report.StarvationDeaths}");
                if (report.TrafficDeaths > 0) causes.Add($"车 {report.TrafficDeaths}");
                if (report.PredationDeaths > 0) causes.Add($"捕 {report.PredationDeaths}");
                if (report.OtherDeaths > 0) causes.Add($"其他 {report.OtherDeaths}");
                var deathDetails = causes.Count > 0 ? $"（{string.Join(" · ", causes)}）" : string.Empty;
                var waste = report.WasteIssues > 0 ? $" · 垃圾 {report.WasteIssues}" : string.Empty;
                dailyOutcomeText.text = $"第 {report.Day} 天 · 挪房 {report.MovedRooms} · 通勤 {workers}{meals}" +
                    $"\n{pigeons}死亡 {report.Deaths}{deathDetails}{waste}" +
                    $"\n{DailyOutcomeHint(report, true)}";
            }
            else
            {
                var workers = report.WorkingResidentsKnown
                    ? $"{report.WorkingResidents}"
                    : "not recorded";
                var meals = report.MealsKnown
                    ? $" · ate {report.FedAnimals}/{report.LivingAnimals}"
                    : string.Empty;
                var pigeons = report.MealsKnown
                    ? $"Pigeons {report.FedPigeons}/{report.LivingPigeons} · seeds left {report.SeedPortionsLeft} · "
                    : string.Empty;
                var causes = new List<string>();
                if (report.StarvationDeaths > 0) causes.Add($"starve {report.StarvationDeaths}");
                if (report.TrafficDeaths > 0) causes.Add($"car {report.TrafficDeaths}");
                if (report.PredationDeaths > 0) causes.Add($"fox {report.PredationDeaths}");
                if (report.OtherDeaths > 0) causes.Add($"other {report.OtherDeaths}");
                var deathDetails = causes.Count > 0 ? $" ({string.Join(" · ", causes)})" : string.Empty;
                var waste = report.WasteIssues > 0 ? $" · waste {report.WasteIssues}" : string.Empty;
                dailyOutcomeText.text = $"Day {report.Day} · moved {report.MovedRooms} · commute {workers}{meals}" +
                    $"\n{pigeons}deaths {report.Deaths}{deathDetails}{waste}" +
                    $"\n{DailyOutcomeHint(report, false)}";
            }
        }

        private static string DailyOutcomeHint(DailyOutcomeReport report, bool chinese)
        {
            if (report.MealsKnown && report.FedPigeons < report.LivingPigeons)
                return report.SeedPortionsLeft > 0
                    ? chinese ? "仍有种子却未吃到：检查通道或抵达时间。"
                        : "Seeds remain: check passage and arrival time."
                    : chinese ? "种子已用尽：检查补给与鸽群数量。"
                        : "Seeds depleted: check supply versus flock size.";
            if (report.StarvationDeaths > 0)
                return chinese ? "食路不等于进食：查存量、门与实际抵达。"
                    : "A food route is not a meal: check stock, doors and arrival.";
            if (report.PredationDeaths > 0)
                return chinese ? "狐狸捕食：检查狐狸与猎物的通道连通。"
                    : "Fox predation: check fox-to-prey passage links.";
            if (report.TrafficDeaths > 0)
                return chinese ? "车祸：检查动物是否穿越车库。"
                    : "Traffic death: inspect animal garage crossings.";
            if (report.WasteIssues > 0)
                return chinese ? "垃圾异常：检查垃圾房及人行道路。"
                    : "Waste issue: check bins and pedestrian roads.";
            return chinese ? "以上为同期观察，不能仅归因于挪房。"
                : "Observed together; room moves are not the sole cause.";
        }

        private void BuildHedgehogNightReport()
        {
            var panel = CreatePanel("Hedgehog Night Report", gameplayRoot,
                new Color(0.10f, 0.12f, 0.14f, 0.82f));
            panel.Image.raycastTarget = false;
            RoundSolidPanel(panel, 14f);
            hedgehogNightPanel = panel.GameObject;
            panel.RectTransform.anchorMin = panel.RectTransform.anchorMax = Vector2.zero;
            panel.RectTransform.pivot = Vector2.zero;
            panel.RectTransform.anchoredPosition = new Vector2(20f, 136f);
            panel.RectTransform.sizeDelta = new Vector2(470f, 76f);
            hedgehogNightText = CreateText(panel.Transform, "Hedgehog Night Summary",
                string.Empty, 15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            hedgehogNightText.raycastTarget = false;
            Stretch(hedgehogNightText.rectTransform, 14f, 8f);
            hedgehogNightPanel.SetActive(false);
        }

        private void RefreshHedgehogNightReport()
        {
            if (hedgehogNightPanel == null || runtime == null)
            {
                return;
            }
            var report = hedgehogForaging?.LastNightReport ?? default;
            var visible = hedgehogForaging != null && runtime.HasActiveRun && !runtime.LayoutEditing;
            hedgehogNightPanel.SetActive(visible);
            if (!visible)
            {
                return;
            }
            var lastNightVisible = report.Day > 0 && runtime.Clock.DayNumber == report.Day + 1;
            if (runtime.Language == InterfaceLanguage.Chinese)
            {
                hedgehogNightText.text = $"刺猬庇护｜相邻灌木 {hedgehogForaging.CurrentShelterPairs} 组 · 恢复中 {hedgehogForaging.RecoveringShrubs} 株" +
                    (lastNightVisible
                        ? $"\n第 {report.Day} 夜｜庇护路线 {report.CoveredTrips} 次 · 其他路线 {report.OtherTrips} 次 · 觅食成功 {report.Meals} 次"
                        : string.Empty);
            }
            else
            {
                hedgehogNightText.text = $"Hedgehog cover | {hedgehogForaging.CurrentShelterPairs} pairs, {hedgehogForaging.RecoveringShrubs} recovering" +
                    (lastNightVisible
                        ? $"\nNight {report.Day} | covered trips {report.CoveredTrips}, other {report.OtherTrips}, meals {report.Meals}"
                        : string.Empty);
            }
        }

        public void BindWasteManagement(WasteManagementController controller)
        {
            if (controller == null || gameplayRoot == null)
            {
                return;
            }

            if (wasteCollectionNotification == null)
            {
                wasteCollectionNotification = gameObject.AddComponent<WasteCollectionNotification>();
            }

            wasteCollectionNotification.Build(
                gameplayRoot,
                runtime,
                controller,
                worldCamera,
                generatedHideFlags,
                roomId => selectedRoomSpec != null && selectedRoomSpec.Id == roomId
                    ? selectedRoomTransform
                    : null);
        }

        public void BindResourceEconomy(ResourceEconomyController controller)
        {
            if (gameplayRoot == null || workforceCounterRect != null)
            {
                return;
            }
            var panel = CreatePanel("Workforce Counter", gameplayRoot,
                new Color(0.10f, 0.12f, 0.14f, 0.88f));
            RoundSolidPanel(panel, 14f);
            panel.Image.raycastTarget = false;
            workforceCounterRect = panel.RectTransform;
            SetTopLeft(workforceCounterRect, 22f, 20f, 170f, 68f);
            workforceCounterText = CreateText(panel.Transform, "Workforce Target",
                string.Empty, 18, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            workforceCounterText.raycastTarget = false;
            Stretch(workforceCounterText.rectTransform, 8f);
            RefreshResidentPopulation();
        }

        public void BindPlayerFeeding(PlayerFeedingController controller)
        {
            if (playerFeeding != null)
            {
                playerFeeding.FeedingModeChanged -= RefreshFeedingControls;
                playerFeeding.LeftoverFoodDiscarded -= HandleLeftoverFoodDiscarded;
                playerFeeding.StateChanged -= RefreshFoodBadgeTargets;
            }

            playerFeeding = controller;
            if (playerFeeding == null || gameplayRoot == null)
            {
                return;
            }

            if (feedingModeButton == null)
            {
                BuildFeedingControls();
            }
            playerFeeding.FeedingModeChanged += RefreshFeedingControls;
            playerFeeding.LeftoverFoodDiscarded += HandleLeftoverFoodDiscarded;
            playerFeeding.StateChanged += RefreshFoodBadgeTargets;
            RefreshFoodBadgeTargets();
            RefreshFeedingControls();
        }

        private void HandleLeftoverFoodDiscarded(string roomId, int portions, bool routed)
        {
            feedingWasteRoomId = roomId;
            feedingWastePortions = portions;
            feedingWasteRouted = routed;
            feedingWasteNoticeUntil = Time.unscaledTime + 5f;
            RefreshFeedingControls();
        }

        public void BindResidentPopulation(
            ResidentPopulationController controller,
            System.Func<string, Transform> roomTransformResolver)
        {
            if (residentPopulation != null)
            {
                residentPopulation.StateChanged -= RefreshResidentPopulation;
            }
            residentPopulation = controller;
            if (controller == null || gameplayRoot == null)
            {
                return;
            }

            if (residentStatusOverlay == null)
            {
                residentStatusOverlay = gameObject.AddComponent<ResidentStatusOverlay>();
            }

            residentStatusOverlay.Build(
                gameplayRoot,
                font,
                generatedHideFlags,
                worldCamera,
                runtime,
                controller,
                roomTransformResolver);
            residentPopulation.StateChanged += RefreshResidentPopulation;
            RefreshResidentPopulation();
            RefreshNeedRisk();
        }

        public void BindAnimalNeeds(AnimalNeedsController controller)
        {
            if (animalNeeds != null)
                animalNeeds.StateChanged -= RefreshNeedRisk;
            animalNeeds = controller;
            if (animalNeeds != null)
                animalNeeds.StateChanged += RefreshNeedRisk;
            RefreshNeedRisk();
        }

        public void BindRiskRoomNavigation(System.Action<string> navigateToRoom)
        {
            riskRoomNavigation = navigateToRoom;
            RefreshNeedRisk();
            RefreshFoodLocations();
        }

        public void BindNaturalFood(NaturalFoodController controller)
        {
            if (naturalFood != null)
                naturalFood.StateChanged -= RefreshFoodBadgeTargets;
            naturalFood = controller;
            if (naturalFood != null)
                naturalFood.StateChanged += RefreshFoodBadgeTargets;
            RefreshFoodBadgeTargets();
            RefreshFoodLocations();
        }

        public void BindOakTreeLifecycle(OakTreeLifecycleController controller)
        {
            if (oakTreeLifecycle != null)
            {
                oakTreeLifecycle.StateChanged -= RefreshSelectedRoomContext;
            }

            oakTreeLifecycle = controller;
            if (oakTreeLifecycle != null)
            {
                oakTreeLifecycle.StateChanged += RefreshSelectedRoomContext;
            }
            RefreshSelectedRoomContext();
        }

        public void BindAnimalPopulation(AnimalPopulationController controller)
        {
            if (animalPopulation != null)
            {
                animalPopulation.StateChanged -= RefreshAnimalPopulation;
            }
            animalPopulation = controller;
            if (animalPopulation != null)
            {
                animalPopulation.StateChanged += RefreshAnimalPopulation;
            }
            RefreshAnimalPopulation();
        }

        public void BindAnimalMortality(AnimalMortalityController controller)
        {
            if (animalMortality != null)
            {
                animalMortality.StateChanged -= RefreshMortalityIndicator;
            }
            animalMortality = controller;
            if (animalMortality != null)
            {
                animalMortality.StateChanged += RefreshMortalityIndicator;
            }
            RefreshMortalityIndicator();
        }

        public void BindEndlessBalance(EndlessBalanceController controller)
        {
            if (endlessBalance != null)
                endlessBalance.StateChanged -= RefreshEndlessBalance;
            endlessBalance = controller;
            if (endlessBalance != null)
                endlessBalance.StateChanged += RefreshEndlessBalance;
            RefreshEndlessBalance();
        }

        private void RefreshEndlessBalance()
        {
            RefreshResidentPopulation();
            RefreshMortalityIndicator();
            RefreshAnimalPopulation();
        }

        public void BindEcologicalMetrics(EcologicalMetricsController controller)
        {
            if (ecologicalMetrics != null)
            {
                ecologicalMetrics.StateChanged -= RefreshEcologicalMetrics;
            }
            ecologicalMetrics = controller;
            if (ecologicalMetrics != null)
            {
                ecologicalMetrics.StateChanged += RefreshEcologicalMetrics;
            }
            RefreshEcologicalMetrics();
        }

        public void BindResearchSession(ResearchSessionController controller)
        {
            if (researchSession != null)
            {
                researchSession.StateChanged -= RefreshResearchSession;
            }
            researchSession = controller;
            if (researchSession != null)
            {
                researchSession.StateChanged += RefreshResearchSession;
                BuildResearchSetupOverlay();
            }
            RefreshResearchSession();
        }

        public void ShowResearchSetupPreview()
        {
            if (researchSetupOverlay == null)
            {
                return;
            }
            researchSetupOverlay.SetActive(true);
            researchSetupOverlay.transform.SetAsLastSibling();
            SetDesktopMenuControlsVisible(false);
            RefreshResearchSession();
            RefreshLanguage();
        }

#if UNITY_EDITOR
        public void ShowRestartConfirmationPreview()
        {
            if (runtime.AtDesktop)
            {
                runtime.StartNewRun(GameMode.Sandbox);
            }
            onboardingController?.Replay();
            if (!runtime.PauseMenuOpen)
            {
                runtime.TogglePauseMenu();
            }
            restartConfirmationOpen = true;
            Refresh();
        }
#endif

        public void BindOnboarding(FirstRunOnboardingController controller)
        {
            onboardingController = controller;
        }

        private void ReplayOnboarding()
        {
            runtime.ContinueGame();
            onboardingController?.Replay();
        }

        private void BeginSandboxRestartConfirmation()
        {
            if (runtime == null || runtime.Mode != GameMode.Sandbox || !runtime.HasActiveRun)
            {
                return;
            }

            restartConfirmationOpen = true;
            Refresh();
        }

        private void CancelSandboxRestart()
        {
            restartConfirmationOpen = false;
            Refresh();
        }

        private void ConfirmSandboxRestart()
        {
            if (runtime == null || runtime.Mode != GameMode.Sandbox)
            {
                CancelSandboxRestart();
                return;
            }

            restartConfirmationOpen = false;
            runtime.RestartRun();
        }

        public void SetCameraCalibrationPreview(Texture previewTexture)
        {
            if (cameraCalibrationPreview != null)
            {
                cameraCalibrationPreview.texture = previewTexture;
            }
        }

        public void SetCameraCalibrationCellState(
            int cellIndex,
            CameraCalibrationCellState state)
        {
            if (cellIndex < 0 || cellIndex >= cameraCalibrationCells.Length)
            {
                return;
            }

            var cell = cameraCalibrationCells[cellIndex];
            if (cell == null)
            {
                return;
            }

            var outline = cell.GetComponent<Outline>();
            switch (state)
            {
                case CameraCalibrationCellState.Recognised:
                    cell.color = new Color(0.32f, 0.78f, 0.46f, 0.05f);
                    outline.effectColor = new Color(0.32f, 0.88f, 0.50f, 0.92f);
                    break;
                case CameraCalibrationCellState.Unresolved:
                    cell.color = new Color(0.95f, 0.42f, 0.18f, 0.13f);
                    outline.effectColor = new Color(0.98f, 0.43f, 0.18f, 0.96f);
                    break;
                default:
                    cell.color = Color.clear;
                    outline.effectColor = Color.clear;
                    break;
            }
        }

        public void SetCameraRecognitionFeedbackRect(
            Vector2 anchoredPosition,
            Vector2 size)
        {
            if (cameraRecognitionFeedbackRect == null)
            {
                return;
            }

            cameraRecognitionFeedbackRect.anchoredPosition = anchoredPosition;
            cameraRecognitionFeedbackRect.sizeDelta = size;
        }

        public void SetCameraInvalidPlacementRects(IReadOnlyList<Rect> affectedRects)
        {
            if (cameraInvalidPlacementRoot == null)
            {
                return;
            }

            var count = affectedRects?.Count ?? 0;
            while (cameraInvalidPlacementFrames.Count < count)
            {
                var frame = CreateImage(
                    cameraInvalidPlacementRoot,
                    $"Invalid Placement {cameraInvalidPlacementFrames.Count + 1}",
                    CameraRecognitionVisualCatalog.GetSprite(
                        CameraRecognitionVisual.AmberInvalidPlacement));
                frame.preserveAspect = false;
                frame.raycastTarget = false;
                cameraInvalidPlacementFrames.Add(frame);
            }

            for (var index = 0; index < cameraInvalidPlacementFrames.Count; index++)
            {
                var frame = cameraInvalidPlacementFrames[index];
                var active = index < count;
                frame.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var affectedRect = affectedRects[index];
                frame.rectTransform.anchoredPosition = affectedRect.center;
                frame.rectTransform.sizeDelta = affectedRect.size;
                frame.rectTransform.localScale = Vector3.one;
            }
        }

        private void LateUpdate()
        {
            UpdateRoomContextPosition();
            UpdateRoomHoverLabel();
            UpdateFoodBadgeHover();
            UpdateCameraRecognitionFeedbackAnimation();
        }

        private void RefreshFoodBadgeTargets()
        {
            foodBadgeTargets.Clear();
            if (transform.parent == null) return;
            foreach (var visual in transform.parent.GetComponentsInChildren<NaturalFoodVisual>(true))
                if (visual != null && visual.gameObject.activeSelf && visual.MapBadgeText != null)
                    foodBadgeTargets.Add(new FoodBadgeTarget(
                        visual.MapBadgeText, visual.Kind, visual.AddedToday, false));
            foreach (var visual in transform.parent.GetComponentsInChildren<PlayerFoodSourceVisual>(true))
                if (visual != null && visual.gameObject.activeSelf && visual.MapBadgeText != null)
                    foodBadgeTargets.Add(new FoodBadgeTarget(
                        visual.MapBadgeText, null, null, visual.PlayerPlaced));
        }

        private void UpdateFoodBadgeHover()
        {
            if (foodBadgeTooltip == null || worldCamera == null || runtime == null ||
                !gameplayRoot.gameObject.activeInHierarchy || !runtime.HasActiveRun ||
                runtime.AtDesktop || runtime.PauseMenuOpen || runtime.ResultsOpen ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (foodBadgeTooltip != null) foodBadgeTooltip.SetActive(false);
                return;
            }

            var pointer = (Vector2)Input.mousePosition;
            FoodBadgeTarget? hovered = null;
            var bestDistance = float.PositiveInfinity;
            foreach (var target in foodBadgeTargets)
            {
                if (target.Label == null || !int.TryParse(target.Label.text, out var portions) ||
                    portions <= 0 ||
                    !RoomMapBadgeVisual.TryGetFoodScreenRect(target.Label, worldCamera, out var rect))
                    continue;
                var hit = Rect.MinMaxRect(rect.xMin - 3f, rect.yMin - 3f,
                    rect.xMax + 3f, rect.yMax + 3f);
                if (!hit.Contains(pointer)) continue;
                var distance = (pointer - rect.center).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                hovered = target;
            }

            if (!hovered.HasValue)
            {
                foodBadgeTooltip.SetActive(false);
                return;
            }

            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            int.TryParse(hovered.Value.Label.text, out var count);
            foodBadgeTooltipText.text = RoomMapBadgeVisual.DescribeFood(
                hovered.Value.Kind, count, chinese,
                hovered.Value.AddedToday, hovered.Value.PlayerPlaced);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameplayRoot, pointer, worldCamera, out var localPoint);
            var x = Mathf.Clamp(localPoint.x + 17f,
                gameplayRoot.rect.xMin + 8f, gameplayRoot.rect.xMax - 308f);
            var y = Mathf.Clamp(localPoint.y - 16f,
                gameplayRoot.rect.yMin + 76f, gameplayRoot.rect.yMax - 8f);
            foodBadgeTooltipRect.anchoredPosition = new Vector2(x, y);
            if (!foodBadgeTooltip.activeSelf)
                foodBadgeTooltip.transform.SetAsLastSibling();
            foodBadgeTooltip.SetActive(true);
            if (roomHoverCanvasGroup != null) roomHoverCanvasGroup.alpha = 0f;
        }

        private void UpdateCameraRecognitionFeedbackAnimation()
        {
            if (cameraRecognitionFeedbackRoot == null || runtime == null)
            {
                return;
            }

            if (runtime.CameraRecognitionState == CameraRecognitionFeedbackState.Scanning &&
                cameraRecognitionScanFrame != null)
            {
                cameraRecognitionScanFrame.fillAmount =
                    0.08f + Mathf.PingPong(Time.unscaledTime * 0.12f, 0.08f);
            }

            if (runtime.CameraRecognitionState == CameraRecognitionFeedbackState.InvalidPlacement)
            {
                var invalidAlpha = 0.72f + Mathf.PingPong(Time.unscaledTime * 0.35f, 0.20f);
                foreach (var frame in cameraInvalidPlacementFrames)
                {
                    if (frame.gameObject.activeSelf)
                    {
                        frame.color = new Color(1f, 1f, 1f, invalidAlpha);
                    }
                }
            }

            if (runtime.CameraRecognitionState != CameraRecognitionFeedbackState.Confirmed ||
                cameraRecognitionConfirmationFrame == null)
            {
                return;
            }

            var remaining = cameraRecognitionConfirmationUntil - Time.unscaledTime;
            if (remaining <= 0f)
            {
                runtime.ClearCameraRecognitionFeedback();
                return;
            }

            var alpha = Mathf.Clamp01(remaining / 0.55f);
            cameraRecognitionConfirmationFrame.color = new Color(1f, 1f, 1f, alpha);
        }

        private void UpdateRoomContextPosition()
        {
            if (selectedRoomTransform == null || roomContext == null || !roomContext.activeSelf)
            {
                return;
            }

            var screenPoint = worldCamera.WorldToScreenPoint(selectedRoomTransform.position + Vector3.up * 2.25f);
            if (screenPoint.z <= 0f)
            {
                roomContext.SetActive(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameplayRoot,
                screenPoint,
                worldCamera,
                out var localPoint);
            roomContextRect.anchoredPosition = localPoint;
            const float tooltipHalfWidth = 125f;
            var tooltipX = Mathf.Clamp(0f,
                gameplayRoot.rect.xMin + tooltipHalfWidth + 8f - localPoint.x,
                gameplayRoot.rect.xMax - tooltipHalfWidth - 8f - localPoint.x);
            var tooltipY = localPoint.y - 66f < gameplayRoot.rect.yMin + 8f
                ? roomContextRect.rect.height + 66f
                : -8f;
            foreach (var tooltipRect in roomContextTooltipRects)
            {
                tooltipRect.anchoredPosition = new Vector2(tooltipX, tooltipY);
            }
        }

        private void UpdateRoomHoverLabel()
        {
            if (roomHoverLabel == null || roomHoverCanvasGroup == null)
            {
                return;
            }

            if (hoverRequested && hoveredRoomTransform != null)
            {
                hoverElapsed += Time.unscaledDeltaTime;
            }

            var targetAlpha = hoverRequested && hoverElapsed >= 0.4f ? 1f : 0f;
            var fadeSpeed = targetAlpha > hoverAlpha ? 7f : 11f;
            hoverAlpha = Mathf.MoveTowards(hoverAlpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            roomHoverCanvasGroup.alpha = hoverAlpha;

            if (hoveredRoomTransform != null && roomHoverLabel.activeSelf)
            {
                var screenPoint = worldCamera.WorldToScreenPoint(hoveredRoomTransform.position + Vector3.up * 1.62f);
                if (screenPoint.z > 0f)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        gameplayRoot,
                        screenPoint,
                        worldCamera,
                        out var localPoint);
                    roomHoverRect.anchoredPosition = localPoint;
                }
            }

            if (!hoverRequested && hoverAlpha <= 0.001f)
            {
                roomHoverLabel.SetActive(false);
                hoveredRoom = null;
                hoveredRoomTransform = null;
            }
        }

        public void ShowRoomHover(RoomSpec room, Transform roomTransform)
        {
            if (room == null || roomTransform == null || room == selectedRoomSpec)
            {
                return;
            }

            hoveredRoom = room;
            hoveredRoomTransform = roomTransform;
            hoverRequested = true;
            hoverElapsed = 0f;
            hoverAlpha = 0f;
            roomHoverText.text = UrbanPalette.LocalizedRoomName(
                room,
                runtime.Language == InterfaceLanguage.Chinese);
            roomHoverCanvasGroup.alpha = 0f;
            roomHoverLabel.SetActive(true);
        }

        public void HideRoomHover(RoomSpec room, bool immediate = false)
        {
            if (room != null && hoveredRoom != null && room != hoveredRoom)
            {
                return;
            }

            hoverRequested = false;
            if (!immediate)
            {
                return;
            }

            hoverAlpha = 0f;
            roomHoverCanvasGroup.alpha = 0f;
            roomHoverLabel.SetActive(false);
            hoveredRoom = null;
            hoveredRoomTransform = null;
        }

        public void ShowRoom(RoomSpec room, Transform roomTransform)
        {
            HideRoomHover(room, true);
            selectedRoomSpec = room;
            selectedRoomTransform = roomTransform;
            roomContext.SetActive(true);
            roomContextIcon.sprite = RoomIconCatalog.GetSprite(room.Type);
            roomContextIcon.enabled = roomContextIcon.sprite != null;

            RefreshSelectedRoomContext();
        }

        private void RefreshSelectedRoomContext()
        {
            if (selectedRoomSpec == null || roomContext == null)
            {
                return;
            }

            // The current prototype has an explicit operating/occupied visual
            // state only for Food Shop. Other room simulations add their second
            // and third chips when their live state models are connected.
            var showFunction = selectedRoomSpec.Type == RoomType.Canteen;
            var showUse = selectedRoomSpec.Type == RoomType.Canteen;
            var functionVisual = RoomContextVisual.FoodAvailable;
            if (selectedRoomSpec.Type == RoomType.OakHabitat && oakTreeLifecycle?.Model != null)
            {
                showFunction = true;
                showUse = false;
                functionVisual = oakTreeLifecycle.Model.StageOf(selectedRoomSpec.Id) switch
                {
                    OakTreeStage.Felled => RoomContextVisual.OakFelled,
                    OakTreeStage.Sapling => RoomContextVisual.OakSapling,
                    OakTreeStage.Young => RoomContextVisual.OakYoung,
                    _ => RoomContextVisual.OakMature
                };
            }

            roomFunctionIcon.sprite = RoomContextVisualCatalog.GetSprite(functionVisual);
            roomUseIcon.sprite = RoomContextVisualCatalog.GetSprite(RoomContextVisual.HumanUse);
            roomFunctionChip.SetActive(showFunction);
            roomUseChip.SetActive(showUse);
            var canPlant = selectedRoomSpec.Type == RoomType.OakHabitat &&
                           oakTreeLifecycle?.Model?.StageOf(selectedRoomSpec.Id) == OakTreeStage.Felled;
            if (oakPlantButton != null)
            {
                oakPlantButton.interactable = canPlant;
            }
            if (oakPlantLabel != null)
            {
                oakPlantLabel.gameObject.SetActive(canPlant);
                oakPlantLabel.text = runtime.Language == InterfaceLanguage.Chinese
                    ? "栽种"
                    : "Plant";
            }
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            roomTypeTooltipText.text = chinese
                ? $"房间类型 · {UrbanPalette.LocalizedRoomName(selectedRoomSpec, true)}"
                : $"Room type · {UrbanPalette.LocalizedRoomName(selectedRoomSpec, false)}";
            if (selectedRoomSpec.Type == RoomType.OakHabitat && oakTreeLifecycle?.Model != null)
            {
                var stage = oakTreeLifecycle.Model.StageOf(selectedRoomSpec.Id);
                roomFunctionTooltipText.text = stage switch
                {
                    OakTreeStage.Felled => chinese ? "橡树状态 · 已砍伐\n点击栽种" : "Oak status · Felled\nClick to plant",
                    OakTreeStage.Sapling => chinese ? "橡树状态 · 树苗\n等待生长" : "Oak status · Sapling\nGrowing over time",
                    OakTreeStage.Young => chinese ? "橡树状态 · 幼树\n尚未成熟" : "Oak status · Young\nNot mature yet",
                    _ => chinese ? "橡树状态 · 成熟\n提供栖息地与自然食物" : "Oak status · Mature\nProvides shelter and natural food"
                };
            }
            else
            {
                roomFunctionTooltipText.text = chinese
                    ? "当前功能 · 提供食物来源"
                    : "Current function · Food source";
            }
            roomUseTooltipText.text = chinese
                ? "当前用途 · 居民用餐区域"
                : "Current use · Resident dining";
            LayoutRoomContextChips(showFunction, showUse);
        }

        private void BuildClock()
        {
            var root = CreateEmpty("Day Night Clock", gameplayRoot);
            SetTopCenter(root, 0f, 8f, 104f, 104f);

            var researchRingObject = NewUiObject(
                "Research Duration Ring",
                root,
                typeof(CanvasRenderer),
                typeof(CircularMeterGraphic),
                typeof(EventTrigger));
            var researchRingRect = researchRingObject.GetComponent<RectTransform>();
            researchRingRect.anchorMin = researchRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            researchRingRect.pivot = new Vector2(0.5f, 0.5f);
            researchRingRect.anchoredPosition = Vector2.zero;
            researchRingRect.sizeDelta = new Vector2(116f, 116f);
            researchDurationRing = researchRingObject.GetComponent<CircularMeterGraphic>();
            researchDurationRing.raycastTarget = true;
            researchDurationRing.SetValue(1f, new Color(0.48f, 0.39f, 0.62f, 0.95f));

            researchDurationTooltip = CreateText(
                root,
                "Research Remaining Tooltip",
                string.Empty,
                15,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            SetTopCenter(researchDurationTooltip.rectTransform, 0f, 110f, 190f, 34f);
            researchDurationTooltip.gameObject.SetActive(false);
            var trigger = researchRingObject.GetComponent<EventTrigger>();
            trigger.triggers = new List<EventTrigger.Entry>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => researchDurationTooltip.gameObject.SetActive(true));
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => researchDurationTooltip.gameObject.SetActive(false));
            trigger.triggers.Add(exit);

            var dialArtwork = CreateImage(root, "Day Night Artwork", GameplayHudVisualCatalog.GetDayNightDialSprite());
            dialArtwork.preserveAspect = true;
            dialArtwork.raycastTarget = false;
            Stretch(dialArtwork.rectTransform);

            var dialObject = NewUiObject("Four Phase Dial", root, typeof(CanvasRenderer), typeof(DayNightDialGraphic));
            var dialRect = dialObject.GetComponent<RectTransform>();
            Stretch(dialRect);
            clockDial = dialObject.GetComponent<DayNightDialGraphic>();
            clockDial.raycastTarget = false;
            clockDial.UseArtworkBackground();

            dayNumber = CreateText(root, "Day Number", "1", 26, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            dayNumber.rectTransform.anchorMin = dayNumber.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dayNumber.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dayNumber.rectTransform.anchoredPosition = Vector2.zero;
            dayNumber.rectTransform.sizeDelta = new Vector2(54f, 42f);
            var dayOutline = dayNumber.gameObject.AddComponent<Outline>();
            dayOutline.effectColor = new Color(0.08f, 0.12f, 0.19f, 0.9f);
            dayOutline.effectDistance = new Vector2(1.4f, -1.4f);

            // Keep the four speed controls centred on the time dial. Parenting
            // them to the dial also preserves alignment at different aspect ratios.
            var speeds = CreateEmpty("Simulation Speed", root);
            SetTopCenter(speeds, 0f, 112f, 228f, 42f);
            var speedRail = CreatePanel("Speed Selection Rail", speeds,
                new Color(0.09f, 0.13f, 0.16f, 0.78f));
            SetCenter(speedRail.RectTransform, 228f, 42f);
            speedRail.Image.raycastTarget = false;
            RoundSolidPanel(speedRail, 14f);
            BuildSpeedButton(speeds, 0, "Ⅱ", -81f);
            BuildSpeedButton(speeds, 1, "1×", -27f);
            BuildSpeedButton(speeds, 2, "2×", 27f);
            BuildSpeedButton(speeds, 4, "4×", 81f);

            skipDayButton = CreateButton(root, "Skip To Next Day", "跳至次日", 15,
                () => runtime.TrySkipToNextDay());
            var skipBacking = skipDayButton.GetComponent<Image>();
            skipBacking.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            skipBacking.preserveAspect = false;
            SetTopCenter(skipDayButton.GetComponent<RectTransform>(), 185f, 112f, 122f, 36f);
            skipDayLabel = skipDayButton.GetComponentInChildren<Text>();
        }

        private void BuildSpeedButton(Transform parent, int speed, string label, float x)
        {
            var button = CreateButton(parent, $"Speed {speed}", label, 19, () => runtime.SetSpeed(speed));
            button.transition = Selectable.Transition.None;
            var hitArea = button.GetComponent<Image>();
            hitArea.color = Color.clear;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(50f, 34f);
            var segment = NewUiObject("Rounded Speed Segment", button.transform,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            segment.color = new Color(0.14f, 0.18f, 0.21f, 0.92f);
            segment.CornerRadius = 10f;
            segment.BorderWidth = 1f;
            segment.BorderColor = new Color(0.94f, 0.90f, 0.80f, 0.27f);
            segment.raycastTarget = false;
            Stretch(segment.rectTransform);
            segment.transform.SetAsFirstSibling();
            speedButtons[speed] = segment;
            speedButtonLabels[speed] = button.GetComponentInChildren<Text>();
        }

        private void BuildIndicators()
        {
            var root = CreateEmpty("Global Ecological Indicators", gameplayRoot);
            root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-36f, -56f);
            root.sizeDelta = new Vector2(100f, 552f);

            var indicators = new[]
            {
                new Indicator(EcologicalMetricKind.HumanFunction, "Resident Count", 1f, Cyan),
                new Indicator(EcologicalMetricKind.FoodAccessibility, "Animals Fed Today", 1f, new Color(0.86f, 0.61f, 0.24f)),
                new Indicator(EcologicalMetricKind.HabitatProvision, "Shelter Index", 1f, new Color(0.35f, 0.66f, 0.47f)),
                new Indicator(EcologicalMetricKind.AnimalSafety, "Living Animals", 1f, Cyan)
            };

            for (var index = 0; index < indicators.Length; index++)
            {
                BuildIndicator(root, indicators[index], index);
            }
            BuildMortalityIndicator(root);
        }

        private void BuildFoodLocationsPanel()
        {
            var panel = CreatePanel("Food Locations", gameplayRoot,
                new Color(0.10f, 0.13f, 0.16f, 0.97f));
            RoundSolidPanel(panel, 16f);
            SetTopRight(panel.RectTransform, 154f, 169f, 390f, 395f);
            foodLocationsPanel = panel.GameObject;

            foodLocationsTitle = CreateText(panel.Transform, "Food Locations Title",
                string.Empty, 20, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            SetTopLeft(foodLocationsTitle.rectTransform, 17f, 8f, 300f, 32f);
            foodLocationsHint = CreateText(panel.Transform, "Food Locations Hint",
                string.Empty, 12, TextAnchor.MiddleLeft, new Color(0.73f, 0.85f, 0.81f),
                FontStyle.Normal);
            SetTopLeft(foodLocationsHint.rectTransform, 17f, 38f, 352f, 20f);
            foodLocationsEmpty = CreateText(panel.Transform, "No Food Sources", string.Empty,
                16, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Normal);
            SetTopLeft(foodLocationsEmpty.rectTransform, 20f, 177f, 350f, 48f);
            var close = CreateButton(panel.Transform, "Close Food Locations", "×", 22,
                () => SetFoodLocationsOpen(false));
            SetTopRight(close.GetComponent<RectTransform>(), 10f, 8f, 32f, 32f);

            for (var index = 0; index < FoodLocationsPerPage; index++)
            {
                var slot = index;
                var row = CreateButton(panel.Transform, $"Food Location {index + 1}",
                    string.Empty, 1, () => NavigateToFoodLocation(slot));
                SetTopLeft(row.GetComponent<RectTransform>(), 14f, 65f + index * 39f, 362f, 36f);
                row.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.25f, 0.97f);
                row.GetComponentInChildren<Text>().gameObject.SetActive(false);
                foodLocationButtons[index] = row;
                foodLocationNames[index] = CreateText(row.transform, "Room Name", string.Empty,
                    15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
                SetTopLeft(foodLocationNames[index].rectTransform, 10f, 0f, 262f, 20f);
                foodLocationDetails[index] = CreateText(row.transform, "Food Kinds", string.Empty,
                    11, TextAnchor.MiddleLeft, new Color(0.76f, 0.86f, 0.79f),
                    FontStyle.Normal);
                SetTopLeft(foodLocationDetails[index].rectTransform, 10f, 18f, 280f, 16f);
                foodLocationCounts[index] = CreateText(row.transform, "Food Portions", string.Empty,
                    20, TextAnchor.MiddleRight, WarmPaper, FontStyle.Bold);
                SetTopRight(foodLocationCounts[index].rectTransform, 12f, 2f, 65f, 31f);
            }

            foodLocationsPrevious = CreateButton(panel.Transform, "Previous Food Page", "‹", 21,
                () => ChangeFoodLocationsPage(-1));
            SetTopLeft(foodLocationsPrevious.GetComponent<RectTransform>(), 94f, 347f, 42f, 34f);
            foodLocationsPageLabel = CreateText(panel.Transform, "Food Locations Page", string.Empty,
                14, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            SetTopLeft(foodLocationsPageLabel.rectTransform, 142f, 347f, 106f, 34f);
            foodLocationsNext = CreateButton(panel.Transform, "Next Food Page", "›", 21,
                () => ChangeFoodLocationsPage(1));
            SetTopLeft(foodLocationsNext.GetComponent<RectTransform>(), 254f, 347f, 42f, 34f);
            foodLocationsPanel.SetActive(false);
        }

        private void ToggleFoodLocations()
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.AtDesktop ||
                runtime.PauseMenuOpen || runtime.ResultsOpen || runtime.LayoutEditing)
                return;
            SetFoodLocationsOpen(!foodLocationsOpen);
        }

        private void SetFoodLocationsOpen(bool open)
        {
            foodLocationsOpen = open;
            if (open) foodLocationsPanel?.transform.SetAsLastSibling();
            RefreshFoodLocations();
        }

        private void ChangeFoodLocationsPage(int delta)
        {
            foodLocationsPage += delta;
            RefreshFoodLocations();
        }

        private void NavigateToFoodLocation(int slot)
        {
            if (slot < 0 || slot >= foodLocationRoomIds.Length ||
                string.IsNullOrEmpty(foodLocationRoomIds[slot])) return;
            var roomId = foodLocationRoomIds[slot];
            RefreshFoodLocations();
            if (!foodLocationEntries.Exists(entry => entry.RoomId == roomId)) return;
            SetFoodLocationsOpen(false);
            riskRoomNavigation?.Invoke(roomId);
        }

        private void RefreshFoodLocations()
        {
            if (foodLocationsPanel == null || runtime == null) return;
            var available = runtime.HasActiveRun && !runtime.AtDesktop &&
                            !runtime.PauseMenuOpen && !runtime.ResultsOpen &&
                            !runtime.LayoutEditing;
            if (!available) foodLocationsOpen = false;
            foodLocationsPanel.SetActive(available && foodLocationsOpen);
            if (!foodLocationsPanel.activeSelf) return;

            foodLocationEntries.Clear();
            var byRoom = new Dictionary<string, FoodLocationEntry>();
            if (naturalFood?.Model != null)
                foreach (var source in naturalFood.Model.Sources.Values)
                {
                    if (source.portions <= 0 || string.IsNullOrEmpty(source.roomId)) continue;
                    if (!byRoom.TryGetValue(source.roomId, out var entry))
                    {
                        entry = new FoodLocationEntry(source.roomId);
                        byRoom.Add(source.roomId, entry);
                    }
                    entry.Add(source.kind, source.portions);
                }
            if (playerFeeding?.Model != null)
                foreach (var source in playerFeeding.Model.Sources.Values)
                {
                    if (source.portions <= 0) continue;
                    var roomId = playerFeeding.CurrentRoomIdAt(source.worldPosition);
                    if (string.IsNullOrEmpty(roomId)) roomId = source.roomId;
                    if (string.IsNullOrEmpty(roomId)) continue;
                    if (!byRoom.TryGetValue(roomId, out var entry))
                    {
                        entry = new FoodLocationEntry(roomId);
                        byRoom.Add(roomId, entry);
                    }
                    entry.Placed += source.portions;
                }
            foodLocationEntries.AddRange(byRoom.Values);
            foodLocationEntries.Sort((a, b) =>
            {
                var count = b.Total.CompareTo(a.Total);
                return count != 0 ? count : string.CompareOrdinal(a.RoomId, b.RoomId);
            });

            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var total = 0;
            foreach (var entry in foodLocationEntries) total += entry.Total;
            foodLocationsTitle.text = chinese ? $"食物位置 · {total} 份" : $"Food locations · {total}";
            foodLocationsHint.text = chinese
                ? "按库存排序 · 点击定位；有食物未必能到达"
                : "Sorted by stock · click room; routes may be blocked";
            foodLocationsEmpty.text = chinese ? "地图上暂无食物" : "No food on the map";
            foodLocationsEmpty.gameObject.SetActive(foodLocationEntries.Count == 0);
            var pageCount = Mathf.Max(1,
                (foodLocationEntries.Count + FoodLocationsPerPage - 1) / FoodLocationsPerPage);
            foodLocationsPage = Mathf.Clamp(foodLocationsPage, 0, pageCount - 1);
            for (var index = 0; index < FoodLocationsPerPage; index++)
            {
                var entryIndex = foodLocationsPage * FoodLocationsPerPage + index;
                var shown = entryIndex < foodLocationEntries.Count;
                var row = foodLocationButtons[index];
                row.gameObject.SetActive(shown);
                foodLocationRoomIds[index] = shown ? foodLocationEntries[entryIndex].RoomId : null;
                if (!shown) continue;
                var entry = foodLocationEntries[entryIndex];
                foodLocationNames[index].text = RiskRoomName(entry.RoomId, chinese);
                foodLocationDetails[index].text = entry.Detail(chinese);
                foodLocationCounts[index].text = chinese ? $"{entry.Total}份" : $"{entry.Total}";
                row.interactable = riskRoomNavigation != null;
            }
            foodLocationsPageLabel.text = $"{foodLocationsPage + 1}/{pageCount}";
            foodLocationsPrevious.interactable = foodLocationsPage > 0;
            foodLocationsNext.interactable = foodLocationsPage < pageCount - 1;
        }

        private void BuildMarketForecast()
        {
            var panel = CreatePanel("Neighborhood Market Forecast", gameplayRoot,
                new Color(0.10f, 0.12f, 0.14f, 0.86f));
            RoundSolidPanel(panel, 14f);
            panel.Image.raycastTarget = false;
            SetTopLeft(panel.RectTransform, 22f, 330f, 400f, 120f);
            marketForecastPanel = panel.GameObject;
            marketForecastLabel = CreateText(panel.Transform, "Market Forecast Text",
                string.Empty, 14, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            marketForecastLabel.raycastTarget = false;
            marketForecastLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(marketForecastLabel.rectTransform, 13f, 7f);
            marketForecastPanel.SetActive(false);
        }

        private void BuildNeedRiskPanel()
        {
            var panel = CreatePanel("Need Risk Attention", gameplayRoot,
                new Color(0.12f, 0.13f, 0.15f, 0.90f));
            RoundSolidPanel(panel, 14f);
            panel.Image.raycastTarget = false;
            SetTopLeft(panel.RectTransform, 22f, 242f, 400f, 76f);
            needRiskPanel = panel.GameObject;

            needRiskSignal = CreateText(panel.Transform, "Risk Signal", "!", 31,
                TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0.35f), FontStyle.Bold);
            needRiskSignal.raycastTarget = false;
            SetTopLeft(needRiskSignal.rectTransform, 8f, 14f, 34f, 47f);
            residentRiskButton = BuildRiskNavigationButton(panel.Transform, "Resident Risk Target",
                () => NavigateToRiskRoom(true));
            animalRiskButton = BuildRiskNavigationButton(panel.Transform, "Animal Risk Target",
                () => NavigateToRiskRoom(false));
            residentRiskLabel = CreateText(residentRiskButton.transform, "Resident Risk", string.Empty,
                16, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            residentRiskLabel.raycastTarget = false;
            SetTopLeft(residentRiskLabel.rectTransform, 4f, 0f, 308f, 29f);
            animalRiskLabel = CreateText(animalRiskButton.transform, "Animal Risk", string.Empty,
                16, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            animalRiskLabel.raycastTarget = false;
            SetTopLeft(animalRiskLabel.rectTransform, 4f, 0f, 308f, 29f);
            needRiskPanel.SetActive(false);
        }

        private Button BuildRiskNavigationButton(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var row = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var image = row.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;
            var button = row.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var arrow = CreateText(row.transform, "Go To Risk Room", "›", 25,
                TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            arrow.raycastTarget = false;
            SetTopLeft(arrow.rectTransform, 318f, 0f, 24f, 29f);
            return button;
        }

        private void NavigateToRiskRoom(bool resident)
        {
            // A source may have changed since the last UI refresh. Never jump
            // to a resolved warning or a room that is no longer in the alert.
            RefreshNeedRisk();
            var button = resident ? residentRiskButton : animalRiskButton;
            var roomId = resident ? residentRiskRoomId : animalRiskRoomId;
            if (button != null && button.gameObject.activeInHierarchy &&
                !string.IsNullOrEmpty(roomId))
                riskRoomNavigation?.Invoke(roomId);
        }

        private void RefreshNeedRisk()
        {
            if (needRiskPanel == null || runtime == null) return;
            var visible = runtime.Mode == GameMode.Sandbox && runtime.HasActiveRun &&
                          !runtime.AtDesktop && !runtime.LayoutEditing &&
                          !runtime.PauseMenuOpen && !runtime.ResultsOpen;
            var model = residentPopulation?.Model;
            var residentRiskCount = 0;
            string firstResidentHome = null;
            if (model != null)
                foreach (var resident in model.Residents)
                {
                    if (model.PreviewRouteLegs(resident.id).Complete) continue;
                    residentRiskCount++;
                    firstResidentHome ??= resident.residenceId;
                }
            var animalRiskCount = animalNeeds?.AnimalsWithoutFoodAccess ?? 0;
            var hasResidentRisk = residentRiskCount > 0;
            var hasAnimalRisk = animalRiskCount > 0;
            residentRiskRoomId = hasResidentRisk ? firstResidentHome : null;
            animalRiskRoomId = hasAnimalRisk ? animalNeeds?.FirstFoodRiskRoomId : null;
            var nextRowLayout = (hasResidentRisk ? 1 : 0) | (hasAnimalRisk ? 2 : 0);
            if (riskRowLayout != nextRowLayout)
            {
                riskRowLayout = nextRowLayout;
                residentRiskButton.gameObject.SetActive(hasResidentRisk);
                animalRiskButton.gameObject.SetActive(hasAnimalRisk);
                SetTopLeft(residentRiskButton.GetComponent<RectTransform>(), 42f, 7f, 348f, 29f);
                SetTopLeft(animalRiskButton.GetComponent<RectTransform>(), 42f,
                    hasResidentRisk ? 39f : 7f, 348f, 29f);
                SetTopLeft(needRiskPanel.GetComponent<RectTransform>(), 22f, 242f,
                    400f, hasResidentRisk && hasAnimalRisk ? 76f : 44f);
                SetTopLeft(needRiskSignal.rectTransform, 8f,
                    hasResidentRisk && hasAnimalRisk ? 14f : 2f, 34f,
                    hasResidentRisk && hasAnimalRisk ? 47f : 40f);
            }
            needRiskPanel.SetActive(visible && (hasResidentRisk || hasAnimalRisk));
            if (!needRiskPanel.activeSelf) return;

            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            if (hasResidentRisk)
                residentRiskLabel.text = chinese
                    ? $"通勤预警 {residentRiskCount} 人 · {RiskRoomName(residentRiskRoomId, true)}"
                    : $"Commute risk {residentRiskCount} · {RiskRoomName(residentRiskRoomId, false)}";
            if (hasAnimalRisk)
                animalRiskLabel.text = chinese
                    ? $"觅食预警 {animalRiskCount} 只 · {RiskRoomName(animalRiskRoomId, true)}"
                    : $"Food access risk {animalRiskCount} · {RiskRoomName(animalRiskRoomId, false)}";
            residentRiskLabel.color = animalRiskLabel.color = new Color(1f, 0.61f, 0.52f);
            residentRiskButton.interactable = !string.IsNullOrEmpty(residentRiskRoomId) &&
                                              riskRoomNavigation != null;
            animalRiskButton.interactable = !string.IsNullOrEmpty(animalRiskRoomId) &&
                                            riskRoomNavigation != null;
        }

        private static string RiskRoomName(string roomId, bool chinese)
        {
            if (string.IsNullOrEmpty(roomId)) return chinese ? "查看红色动物" : "see red animals";
            foreach (var room in RoomLayoutData.All)
                if (room.Id == roomId)
                {
                    if (chinese) return room.DisplayName;
                    var suffix = roomId.Length > 0 ? roomId[roomId.Length - 1].ToString().ToUpperInvariant() : "";
                    return room.Type switch
                    {
                        RoomType.PigeonHabitat => $"Pigeon plaza {suffix}",
                        RoomType.Residence => $"Home {suffix}",
                        RoomType.OakHabitat => $"Oak {suffix}",
                        RoomType.ShrubHabitat => $"Shrub {suffix}",
                        RoomType.CentralPark => "Central park",
                        RoomType.Canteen => $"Food shop {suffix}",
                        RoomType.Trash => $"Waste {suffix}",
                        RoomType.SharedSpace => room.GreenRole switch
                        {
                            ParkGreenRole.SquirrelGrove => "Squirrel grove",
                            ParkGreenRole.HedgehogGarden => "Hedgehog garden",
                            ParkGreenRole.FoxEdge => "Fox edge",
                            _ => "Green room"
                        },
                        _ => roomId
                    };
                }
            return roomId;
        }

        private void BuildIndicator(Transform parent, Indicator indicator, int index)
        {
            var holder = CreateEmpty(indicator.Name, parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -index * 112f);
            holder.sizeDelta = new Vector2(100f, 100f);

            var hitArea = AddHoverHitArea(holder);
            if (indicator.Kind == EcologicalMetricKind.FoodAccessibility)
            {
                var button = hitArea.AddComponent<Button>();
                button.targetGraphic = hitArea.GetComponent<Image>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(ToggleFoodLocations);
            }
            BuildIndicatorDisc(holder);
            var meterObject = NewUiObject("Value Ring", holder, typeof(CanvasRenderer), typeof(CircularMeterGraphic));
            Stretch(meterObject.GetComponent<RectTransform>());
            var meter = meterObject.GetComponent<CircularMeterGraphic>();
            meter.raycastTarget = false;
            meter.SetValue(indicator.Value, indicator.Color);
            ecologicalMeters[indicator.Kind] = meter;
            ecologicalColors[indicator.Kind] = indicator.Color;
            var pictogram = CreateImage(
                holder,
                "Metric Pictogram",
                GameplayHudVisualCatalog.GetMetricSprite(indicator.Kind));
            pictogram.preserveAspect = true;
            pictogram.color = Color.white;
            pictogram.rectTransform.anchorMin = pictogram.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pictogram.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pictogram.rectTransform.anchoredPosition = new Vector2(0f, 7f);
            pictogram.rectTransform.sizeDelta = indicator.Kind == EcologicalMetricKind.HumanFunction
                ? new Vector2(74f, 74f)
                : new Vector2(68f, 68f);

            var valueBadge = BuildIndicatorValueBadge(holder,
                indicator.Kind == EcologicalMetricKind.HumanFunction
                    ? "Resident Count Badge"
                    : "Value Badge",
                "Current Value",
                indicator.Kind == EcologicalMetricKind.HumanFunction ? 42f : 62f);
            ecologicalValueBadges[indicator.Kind] = valueBadge;
            if (indicator.Kind == EcologicalMetricKind.HumanFunction)
            {
                residentCountText = valueBadge;
            }

            var tooltip = CreatePanel("Hover Detail", holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            tooltip.RectTransform.anchorMin = tooltip.RectTransform.anchorMax = new Vector2(0f, 0.5f);
            tooltip.RectTransform.pivot = new Vector2(1f, 0.5f);
            tooltip.RectTransform.anchoredPosition = new Vector2(-12f, 0f);
            tooltip.RectTransform.sizeDelta = indicator.Kind == EcologicalMetricKind.FoodAccessibility
                ? new Vector2(420f, 120f)
                : indicator.Kind == EcologicalMetricKind.HumanFunction
                    ? new Vector2(310f, 84f)
                : new Vector2(310f, 76f);
            tooltip.Image.raycastTarget = false;
            RoundSolidPanel(tooltip, 14f);
            var tooltipText = CreateText(tooltip.Transform, "Exact Value And Reason", string.Empty, 14, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(tooltipText.rectTransform, 12f);
            ecologicalTooltips[indicator.Kind] = tooltipText;
            holder.gameObject.AddComponent<EcologicalIndicatorHover>().Initialize(tooltip.GameObject);
        }

        private Text BuildIndicatorValueBadge(Transform holder, string name,
            string label, float width)
        {
            var badge = CreatePanel(name, holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            badge.RectTransform.anchorMin = badge.RectTransform.anchorMax = new Vector2(0.5f, 0f);
            badge.RectTransform.pivot = new Vector2(0.5f, 0f);
            badge.RectTransform.anchoredPosition = new Vector2(0f, -1f);
            badge.RectTransform.sizeDelta = new Vector2(width, 21f);
            badge.Image.raycastTarget = false;
            RoundSolidPanel(badge, 9f);
            var value = CreateText(badge.Transform, label, "–", 13,
                TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            Stretch(value.rectTransform, 2f);
            return value;
        }

        private void BuildMortalityIndicator(Transform parent)
        {
            var holder = CreateEmpty("Animal Death Limit", parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -448f);
            holder.sizeDelta = new Vector2(100f, 100f);

            AddHoverHitArea(holder);

            BuildIndicatorDisc(holder);
            var meterObject = NewUiObject("Death Limit Ring", holder,
                typeof(CanvasRenderer), typeof(CircularMeterGraphic));
            Stretch(meterObject.GetComponent<RectTransform>());
            mortalityMeter = meterObject.GetComponent<CircularMeterGraphic>();
            mortalityMeter.raycastTarget = false;
            mortalityMeter.SetValue(0f, new Color(0.94f, 0.28f, 0.23f));

            var pulseObject = NewUiObject("Heartbeat Pictogram", holder,
                typeof(CanvasRenderer), typeof(PulseIconGraphic));
            var pulseRect = pulseObject.GetComponent<RectTransform>();
            pulseRect.anchorMin = pulseRect.anchorMax = new Vector2(0.5f, 0.5f);
            pulseRect.pivot = new Vector2(0.5f, 0.5f);
            pulseRect.anchoredPosition = Vector2.zero;
            pulseRect.sizeDelta = new Vector2(54f, 54f);
            pulseObject.GetComponent<PulseIconGraphic>().raycastTarget = false;
            mortalityCountText = BuildIndicatorValueBadge(holder,
                "Death Count Badge", "Cumulative Deaths", 62f);

            var tooltip = CreatePanel("Hover Detail", holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            tooltip.RectTransform.anchorMin = tooltip.RectTransform.anchorMax = new Vector2(0f, 0.5f);
            tooltip.RectTransform.pivot = new Vector2(1f, 0.5f);
            tooltip.RectTransform.anchoredPosition = new Vector2(-12f, 0f);
            tooltip.RectTransform.sizeDelta = new Vector2(410f, 184f);
            tooltip.Image.raycastTarget = false;
            RoundSolidPanel(tooltip, 14f);
            mortalityTooltip = CreateText(tooltip.Transform, "Death Count And Limit", string.Empty,
                15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            mortalityTooltip.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(mortalityTooltip.rectTransform, 12f);
            holder.gameObject.AddComponent<EcologicalIndicatorHover>().Initialize(tooltip.GameObject);
            RefreshMortalityIndicator();
        }

        private void BuildIndicatorDisc(Transform holder)
        {
            var discObject = NewUiObject("Graphite Disc", holder,
                typeof(CanvasRenderer), typeof(DiscGraphic));
            Stretch(discObject.GetComponent<RectTransform>(), 7f);
            var disc = discObject.GetComponent<DiscGraphic>();
            disc.color = Graphite;
            disc.raycastTarget = false;
        }

        private void RefreshMortalityIndicator()
        {
            if (mortalityMeter == null)
            {
                return;
            }
            if (runtime != null && runtime.Mode == GameMode.Sandbox && endlessBalance?.Model != null)
            {
                var model = endlessBalance.Model;
                var living = animalPopulation == null ? model.LastWildlifeCount :
                    animalPopulation.LivingCount(WildlifeSpecies.Pigeon) +
                    animalPopulation.LivingCount(WildlifeSpecies.Squirrel) +
                    animalPopulation.LivingCount(WildlifeSpecies.Hedgehog) +
                    animalPopulation.LivingCount(WildlifeSpecies.Fox);
                var floor = model.CurrentWildlifeFloor;
                var risk = Mathf.Clamp01((AnimalPopulationDefaults.Total - living) /
                    (float)Mathf.Max(1, AnimalPopulationDefaults.Total - floor));
                if (mortalityCountText != null) mortalityCountText.text = $"{living}/{floor}";
                var riskColor = living < floor
                    ? new Color(0.94f, 0.28f, 0.23f)
                    : new Color(0.96f, 0.72f, 0.31f);
                if (Application.isPlaying && mortalityMeter.isActiveAndEnabled)
                    mortalityMeter.AnimateTo(risk, riskColor);
                else
                    mortalityMeter.SetValue(risk, riskColor);
                if (mortalityTooltip != null)
                {
                    var chineseEndless = runtime.Language == InterfaceLanguage.Chinese;
                    mortalityTooltip.text = chineseEndless
                        ? $"现存动物 {living} 只 · 低于 {floor} 连续 {model.WildlifeGraceDays} 天结束\n" +
                          $"当前危急 {model.CriticalWildlifeDays}/{model.WildlifeGraceDays} 天\n" +
                          "死亡不会自动复活；食物和通道稳定后可有新个体到来"
                        : $"Wildlife {living} · below {floor} for {model.WildlifeGraceDays} days ends the run\n" +
                          $"Critical days {model.CriticalWildlifeDays}/{model.WildlifeGraceDays}\n" +
                          "Deaths persist; stable food and routes may bring new arrivals";
                }
                RefreshPopulationDeathTooltips();
                return;
            }
            var deaths = animalMortality?.Model?.TotalDeaths ?? 0;
            var limit = animalMortality?.Model?.DeathLimit ?? AnimalMortalityModel.DefaultDeathLimit;
            var fraction = Mathf.Clamp01(deaths / (float)Mathf.Max(1, limit));
            if (mortalityCountText != null)
            {
                mortalityCountText.text = $"{deaths}/{limit}";
            }
            var red = new Color(0.94f, 0.28f, 0.23f);
            if (Application.isPlaying && mortalityMeter.isActiveAndEnabled)
            {
                mortalityMeter.AnimateTo(fraction, red);
            }
            else
            {
                mortalityMeter.SetValue(fraction, red);
            }
            if (mortalityTooltip != null)
            {
                var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
                var model = animalMortality?.Model;
                mortalityTooltip.text = (chinese
                        ? $"累计死亡 {deaths}/{limit} · 达上限结束"
                        : $"Cumulative deaths {deaths}/{limit} · run ends at limit") + "\n" +
                    (model?.BreakdownOf(WildlifeSpecies.Pigeon).LocalizedLine(chinese) ??
                     new AnimalDeathBreakdownData { species = WildlifeSpecies.Pigeon }.LocalizedLine(chinese)) + "\n" +
                    (model?.BreakdownOf(WildlifeSpecies.Squirrel).LocalizedLine(chinese) ??
                     new AnimalDeathBreakdownData { species = WildlifeSpecies.Squirrel }.LocalizedLine(chinese)) + "\n" +
                    (model?.BreakdownOf(WildlifeSpecies.Hedgehog).LocalizedLine(chinese) ??
                     new AnimalDeathBreakdownData { species = WildlifeSpecies.Hedgehog }.LocalizedLine(chinese)) + "\n" +
                    (model?.BreakdownOf(WildlifeSpecies.Fox).LocalizedLine(chinese) ??
                     new AnimalDeathBreakdownData { species = WildlifeSpecies.Fox }.LocalizedLine(chinese));
            }
            RefreshPopulationDeathTooltips();
        }

        private void BuildPopulation()
        {
            var root = CreateEmpty("Animal Population", gameplayRoot);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = new Vector2(20f, 18f);
            root.sizeDelta = new Vector2(370f, 106f);

            BuildPopulationChip(root, 0, WildlifeSpecies.Pigeon, AnimalPopulationDefaults.Pigeons, new Color(0.40f, 0.67f, 0.99f));
            BuildPopulationChip(root, 1, WildlifeSpecies.Squirrel, AnimalPopulationDefaults.Squirrels, new Color(1f, 0.67f, 0.25f));
            BuildPopulationChip(root, 2, WildlifeSpecies.Hedgehog, AnimalPopulationDefaults.Hedgehogs, new Color(1f, 0.82f, 0.35f));
            BuildPopulationChip(root, 3, WildlifeSpecies.Fox, AnimalPopulationDefaults.Foxes, new Color(0.98f, 0.34f, 0.30f));
        }

        private void BuildPopulationChip(Transform parent, int index, WildlifeSpecies species, int count, Color accent)
        {
            var holder = CreateEmpty($"Population {species}", parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 0.5f);
            holder.pivot = new Vector2(0f, 0.5f);
            holder.anchoredPosition = new Vector2(index * 92f, 0f);
            holder.sizeDelta = new Vector2(84f, 106f);

            AddHoverHitArea(holder);

            var frameObject = NewUiObject(
                "Circular Population Frame", holder, typeof(CanvasRenderer), typeof(AnimalPopulationBadgeGraphic));
            var frameRect = frameObject.GetComponent<RectTransform>();
            Stretch(frameRect);
            var frame = frameObject.GetComponent<AnimalPopulationBadgeGraphic>();
            frame.raycastTarget = false;
            frame.SetPopulation(accent, count, count);
            populationBadges[species] = frame;

            var portrait = CreateImage(holder, "Animal Portrait", GameplayHudVisualCatalog.GetPopulationSprite(species));
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            portrait.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            portrait.rectTransform.anchoredPosition = new Vector2(0f, 11f);
            portrait.rectTransform.sizeDelta = new Vector2(93f, 82f);
            populationPortraits[species] = portrait;

            var amount = CreateText(holder, "Living Count", count.ToString(), 20, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            amount.raycastTarget = false;
            amount.rectTransform.anchorMin = amount.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            amount.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            amount.rectTransform.anchoredPosition = new Vector2(0f, -33f);
            amount.rectTransform.sizeDelta = new Vector2(48f, 25f);
            populationCounts[species] = amount;

            var tooltip = CreatePanel("Death Causes", holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            tooltip.RectTransform.anchorMin = tooltip.RectTransform.anchorMax = new Vector2(0f, 1f);
            tooltip.RectTransform.pivot = new Vector2(0f, 0f);
            // Keep every species detail in one left-column slot above the
            // persistent hedgehog report, not over the board or each other.
            tooltip.RectTransform.anchoredPosition = new Vector2(-index * 92f, 100f);
            tooltip.RectTransform.sizeDelta = new Vector2(310f, 112f);
            tooltip.Image.raycastTarget = false;
            RoundSolidPanel(tooltip, 14f);
            var tooltipText = CreateText(tooltip.Transform, "Species Death Causes", string.Empty,
                15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(tooltipText.rectTransform, 12f);
            populationDeathTooltips[species] = tooltipText;
            holder.gameObject.AddComponent<EcologicalIndicatorHover>().Initialize(tooltip.GameObject);
        }

        private void RefreshPopulationDeathTooltips()
        {
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            foreach (var pair in populationDeathTooltips)
            {
                var row = animalMortality?.Model?.BreakdownOf(pair.Key) ??
                          new AnimalDeathBreakdownData { species = pair.Key };
                var line = row.LocalizedLine(chinese);
                var deaths = chinese
                    ? line.Replace("：", "\n")
                    : line.Replace(": ", "\n");
                if (runtime != null && runtime.Mode == GameMode.Sandbox &&
                    endlessBalance != null && animalPopulation != null)
                {
                    var capacity = endlessBalance.HabitatCapacityOf(pair.Key);
                    var living = animalPopulation.LivingCount(pair.Key);
                    pair.Value.text = chinese
                        ? $"{deaths}\n现存 {living} · 预计栖地上限 {capacity}"
                        : $"{deaths}\nLiving {living} · habitat ceiling {capacity}";
                }
                else pair.Value.text = deaths;
            }
        }

        private GameObject AddHoverHitArea(RectTransform holder)
        {
            var hitArea = NewUiObject("Hover Hit Area", holder, typeof(CanvasRenderer), typeof(Image));
            Stretch(hitArea.GetComponent<RectTransform>());
            var image = hitArea.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            hitArea.transform.SetAsFirstSibling();
            return hitArea;
        }

        private void RefreshResidentPopulation()
        {
            var count = residentPopulation?.Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents;
            if (residentCountText != null)
            {
                residentCountText.text = $"{count}/{ResidentPopulationModel.MaximumResidents}";
            }
            if (workforceCounterText == null || residentPopulation?.Model == null)
            {
                return;
            }
            var workers = residentPopulation.Model.PreviewCommute(
                residentPopulation.Model.NavigationMap).WorkingResidents;
            var residents = residentPopulation.Model.ResidentCount;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            workforceCounterText.text = chinese
                ? $"通勤 {workers}/{residents}" +
                  (runtime.Mode == GameMode.Sandbox && endlessBalance?.Model != null
                      ? $"\n社区 {endlessBalance.Model.Community} {endlessBalance.Model.LastCommunityChange:+#;-#;0}"
                      : "")
                : $"Commute {workers}/{residents}" +
                  (runtime.Mode == GameMode.Sandbox && endlessBalance?.Model != null
                      ? $"\nCommunity {endlessBalance.Model.Community} {endlessBalance.Model.LastCommunityChange:+#;-#;0}"
                      : "");
            var communityLow = runtime.Mode == GameMode.Sandbox &&
                               endlessBalance?.Model != null &&
                               endlessBalance.Model.Community <= 16;
            workforceCounterText.color = workers >= residents && !communityLow
                ? WarmPaper : new Color(1f, 0.63f, 0.46f);
            RefreshNeedRisk();
        }

        private void RefreshAnimalPopulation()
        {
            if (animalPopulation == null)
            {
                return;
            }
            foreach (var pair in populationCounts)
            {
                var living = animalPopulation.LivingCount(pair.Key);
                var total = animalPopulation.TotalCount(pair.Key);
                var endless = runtime != null && runtime.Mode == GameMode.Sandbox;
                var remaining = !endless && living == 0 && total > 0
                    ? animalPopulation.SoonestRespawnRemaining(pair.Key)
                    : 0f;
                var dead = living == 0 && total > 0 && (endless ||
                    remaining > WildlifeVitality.RespawnDelaySeconds - 0.85f);
                var respawning = !endless && living == 0 && total > 0 && !dead;
                // This badge is a living-count display even while a species
                // is waiting to respawn; seconds here looked like population.
                pair.Value.text = living.ToString();
                pair.Value.fontSize = 20;
                if (populationBadges.TryGetValue(pair.Key, out var frame))
                {
                    var accent = pair.Key switch
                    {
                        WildlifeSpecies.Pigeon => new Color(0.40f, 0.67f, 0.99f),
                        WildlifeSpecies.Squirrel => new Color(1f, 0.67f, 0.25f),
                        WildlifeSpecies.Hedgehog => new Color(1f, 0.82f, 0.35f),
                        _ => new Color(0.98f, 0.34f, 0.30f)
                    };
                    frame.SetPopulation(
                        dead ? new Color(0.98f, 0.34f, 0.30f) :
                        respawning ? new Color(0.27f, 0.85f, 0.86f) : accent,
                        living,
                        total,
                        dead ? 0.18f :
                        respawning ? remaining / WildlifeVitality.RespawnDelaySeconds : -1f);
                }
                if (populationPortraits.TryGetValue(pair.Key, out var portrait))
                {
                    portrait.color = living > 0 ? Color.white : new Color(0.53f, 0.55f, 0.57f, 0.72f);
                }
            }
        }

        private void RefreshEcologicalMetrics()
        {
            if (ecologicalMetrics == null)
            {
                return;
            }
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            lastEcologicalLanguageChinese = chinese;
            foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
            {
                var value = kind switch
                {
                    EcologicalMetricKind.HumanFunction =>
                        ecologicalMetrics.ResidentCount / (float)ResidentPopulationModel.MaximumResidents,
                    EcologicalMetricKind.FoodAccessibility => new AnimalMealProgress(
                        ecologicalMetrics.LivingAnimalCount,
                        ecologicalMetrics.LivingUnfedCount).Fraction,
                    EcologicalMetricKind.AnimalSafety => ecologicalMetrics.TotalAnimalSlots == 0
                        ? 0f
                        : ecologicalMetrics.LivingAnimalCount / (float)ecologicalMetrics.TotalAnimalSlots,
                    _ => ecologicalMetrics.Snapshot.ValueOf(kind)
                };
                var color = kind != EcologicalMetricKind.HumanFunction && value < 0.30f
                    ? new Color(0.62f, 0.16f, 0.12f)
                    : kind != EcologicalMetricKind.HumanFunction && value < 0.60f
                        ? new Color(0.86f, 0.46f, 0.16f)
                        : ecologicalColors[kind];
                if (ecologicalMeters.TryGetValue(kind, out var meter))
                {
                    if (Application.isPlaying && meter.isActiveAndEnabled)
                    {
                        meter.AnimateTo(value, color);
                    }
                    else
                    {
                        meter.SetValue(value, color);
                    }
                }
                if (ecologicalValueBadges.TryGetValue(kind, out var badge))
                {
                    badge.text = IndicatorBadge(kind, ecologicalMetrics, chinese);
                }
                if (ecologicalTooltips.TryGetValue(kind, out var tooltip))
                {
                    tooltip.text = IndicatorTooltip(kind, ecologicalMetrics, chinese);
                }
            }
            RefreshFoodLocations();
        }

        private void RefreshResearchSession()
        {
            if (researchSession == null)
            {
                if (researchDurationRing != null)
                {
                    researchDurationRing.gameObject.SetActive(false);
                }
                return;
            }

            var model = researchSession.Model;
            if (researchDurationRing != null)
            {
                var visible = runtime.Mode == GameMode.Research && model.IsRunning &&
                              !runtime.AtDesktop && !runtime.ResultsOpen;
                researchDurationRing.gameObject.SetActive(visible);
                if (visible)
                {
                    var remainingSeconds = Mathf.Max(0f, model.DurationMinutes * 60f - model.ActiveSeconds);
                    var color = remainingSeconds <= 120f
                        ? new Color(0.91f, 0.58f, 0.18f, 1f)
                        : new Color(0.48f, 0.39f, 0.62f, 0.95f);
                    researchDurationRing.SetValue(model.RemainingFraction, color);
                    if (researchDurationTooltip != null)
                    {
                        var totalSeconds = Mathf.CeilToInt(remainingSeconds);
                        researchDurationTooltip.text = runtime.Language == InterfaceLanguage.Chinese
                            ? $"剩余 {totalSeconds / 60:00}:{totalSeconds % 60:00}"
                            : $"Remaining {totalSeconds / 60:00}:{totalSeconds % 60:00}";
                    }
                }
            }

            if (researchDurationValue != null)
            {
                researchDurationValue.text = runtime.Language == InterfaceLanguage.Chinese
                    ? $"{model.DurationMinutes} 分钟"
                    : $"{model.DurationMinutes} min";
            }
            if (researchDeathLimitValue != null)
            {
                researchDeathLimitValue.text = model.DeathLimit.ToString();
            }
        }

        private static string IndicatorBadge(EcologicalMetricKind kind,
            EcologicalMetricsController metrics, bool chinese)
        {
            return kind switch
            {
                EcologicalMetricKind.HumanFunction =>
                    $"{metrics.ResidentCount}/{ResidentPopulationModel.MaximumResidents}",
                EcologicalMetricKind.FoodAccessibility =>
                    new AnimalMealProgress(metrics.LivingAnimalCount,
                        metrics.LivingUnfedCount).Badge,
                EcologicalMetricKind.HabitatProvision =>
                    $"{Mathf.RoundToInt(metrics.Snapshot.HabitatProvision * 100f)}%",
                _ => $"{metrics.LivingAnimalCount}/{metrics.TotalAnimalSlots}"
            };
        }

        private string IndicatorTooltip(EcologicalMetricKind kind,
            EcologicalMetricsController metrics, bool chinese)
        {
            var food = metrics.NaturalFoodPortions + metrics.PlayerFoodPortions;
            var meals = new AnimalMealProgress(metrics.LivingAnimalCount,
                metrics.LivingUnfedCount);
            return kind switch
            {
                EcologicalMetricKind.HumanFunction =>
                    runtime?.Mode == GameMode.Sandbox && endlessBalance?.Model != null
                        ? chinese
                            ? $"居民 {metrics.ResidentCount}/{ResidentPopulationModel.MaximumResidents} 人\n社区活力 {endlessBalance.Model.Community}（{endlessBalance.Model.LastCommunityChange:+#;-#;0}）· 降至 0 结束\n人形牌=规划人数/容量"
                            : $"Residents {metrics.ResidentCount}/{ResidentPopulationModel.MaximumResidents}\nCommunity {endlessBalance.Model.Community} ({endlessBalance.Model.LastCommunityChange:+#;-#;0}) · 0 ends the run\nPerson badge = planned / capacity"
                        : chinese
                            ? $"居民 {metrics.ResidentCount}/{ResidentPopulationModel.MaximumResidents} 人\n人形牌=规划人数/容量\n通勤与垃圾参考 {Mathf.RoundToInt(metrics.Snapshot.HumanFunction * 100f)}%"
                            : $"Residents {metrics.ResidentCount}/{ResidentPopulationModel.MaximumResidents}\nPerson badge = planned / capacity\nCommute/waste index {Mathf.RoundToInt(metrics.Snapshot.HumanFunction * 100f)}%",
                EcologicalMetricKind.FoodAccessibility => chinese
                    ? $"今日已进食 {meals.Badge} · 尚需 {meals.UnfedAnimals}\n地图库存 {food} 份 · 点击查看所在房间\n有食物不代表动物可到达；居民路过投喂另计。"
                    : $"Fed today {meals.Badge} · still need {meals.UnfedAnimals}\nMap stock {food} · click for room locations\nStock may be unreachable; worker feeding is separate.",
                EcologicalMetricKind.HabitatProvision => chinese
                    ? $"庇护指数 {Mathf.RoundToInt(metrics.Snapshot.HabitatProvision * 100f)}%\n固定庇护 75% · 成熟橡树 {metrics.MatureOakCount}/4"
                    : $"Shelter index {Mathf.RoundToInt(metrics.Snapshot.HabitatProvision * 100f)}%\nFixed cover 75% · mature oaks {metrics.MatureOakCount}/4",
                _ => chinese
                    ? $"存活动物 {metrics.LivingAnimalCount}/{metrics.TotalAnimalSlots} 只\n圆环=存活比例；左下查看各物种"
                    : $"Living animals {metrics.LivingAnimalCount}/{metrics.TotalAnimalSlots}\nRing = alive share; species at bottom left"
            };
        }

        private void BuildRoomHoverLabel()
        {
            roomHoverRect = CreateEmpty("Room Hover Name", gameplayRoot);
            roomHoverRect.anchorMin = roomHoverRect.anchorMax = new Vector2(0.5f, 0.5f);
            roomHoverRect.pivot = new Vector2(0.5f, 0f);
            roomHoverRect.sizeDelta = new Vector2(350f, 135f);

            roomHoverCanvasGroup = roomHoverRect.gameObject.AddComponent<CanvasGroup>();
            roomHoverCanvasGroup.alpha = 0f;
            roomHoverCanvasGroup.interactable = false;
            roomHoverCanvasGroup.blocksRaycasts = false;

            var background = CreateImage(
                roomHoverRect,
                "Blank Nameplate",
                RoomHoverLabelCatalog.GetSprite());
            Stretch(background.rectTransform);

            roomHoverText = CreateText(
                roomHoverRect,
                "Dynamic Room Name",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Bold);
            SetRect(roomHoverText.rectTransform, 70f, 42f, 210f, 52f);
            roomHoverLabel = roomHoverRect.gameObject;
            roomHoverLabel.SetActive(false);
        }

        private void BuildFoodBadgeTooltip()
        {
            var panel = CreatePanel("Food Badge Tooltip", gameplayRoot,
                new Color(0.10f, 0.13f, 0.16f, 0.96f));
            RoundSolidPanel(panel, 12f);
            panel.Image.raycastTarget = false;
            foodBadgeTooltipRect = panel.RectTransform;
            foodBadgeTooltipRect.anchorMin = foodBadgeTooltipRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            foodBadgeTooltipRect.pivot = new Vector2(0f, 1f);
            foodBadgeTooltipRect.sizeDelta = new Vector2(300f, 68f);
            foodBadgeTooltipText = CreateText(panel.Transform, "Food Badge Description",
                string.Empty, 14, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            Stretch(foodBadgeTooltipText.rectTransform, 12f, 7f);
            foodBadgeTooltip = panel.GameObject;
            foodBadgeTooltip.SetActive(false);
        }

        private void BuildRoomContext()
        {
            roomContextRect = CreateEmpty("Selected Room Context", gameplayRoot);
            roomContext = roomContextRect.gameObject;
            roomContextRect.anchorMin = roomContextRect.anchorMax = new Vector2(0.5f, 0.5f);
            roomContextRect.pivot = new Vector2(0.5f, 0f);
            roomContextRect.sizeDelta = new Vector2(230f, 126f);

            var connector = CreateImage(
                roomContextRect,
                "Context Connector",
                RoomContextVisualCatalog.GetSprite(RoomContextVisual.Connector));
            connector.preserveAspect = true;
            SetRect(connector.rectTransform, 98f, 0f, 34f, 58f);

            roomTypeChip = BuildRoomContextChip(roomContextRect, "Room Type", out roomContextIcon);
            roomFunctionChip = BuildRoomContextChip(roomContextRect, "Current Function", out roomFunctionIcon);
            roomUseChip = BuildRoomContextChip(roomContextRect, "Current Use", out roomUseIcon);
            var plantHitArea = roomFunctionChip.GetComponent<Image>();
            oakPlantButton = roomFunctionChip.AddComponent<Button>();
            oakPlantButton.targetGraphic = plantHitArea;
            oakPlantButton.onClick.AddListener(() =>
            {
                if (selectedRoomSpec != null && oakTreeLifecycle != null)
                {
                    oakTreeLifecycle.TryPlant(selectedRoomSpec.Id);
                }
            });
            oakPlantLabel = CreateText(
                roomFunctionChip.transform,
                "Plant Tree Cost",
                "Plant Tree Action",
                14,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            oakPlantLabel.rectTransform.anchorMin = oakPlantLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            oakPlantLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            oakPlantLabel.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            oakPlantLabel.rectTransform.sizeDelta = new Vector2(96f, 24f);
            oakPlantLabel.gameObject.SetActive(false);
            roomTypeChip.AddComponent<EcologicalIndicatorHover>().Initialize(
                BuildRoomContextTooltip("Room Type Detail", out roomTypeTooltipText));
            roomFunctionChip.AddComponent<EcologicalIndicatorHover>().Initialize(
                BuildRoomContextTooltip("Current Function Detail", out roomFunctionTooltipText));
            roomUseChip.AddComponent<EcologicalIndicatorHover>().Initialize(
                BuildRoomContextTooltip("Current Use Detail", out roomUseTooltipText));
            LayoutRoomContextChips(true, true);
            roomContext.SetActive(false);
        }

        private GameObject BuildRoomContextChip(Transform parent, string name, out Image pictogram)
        {
            var holder = CreateEmpty($"{name} Chip", parent);
            holder.anchorMin = holder.anchorMax = Vector2.zero;
            holder.pivot = Vector2.zero;
            holder.sizeDelta = new Vector2(72f, 72f);
            var hitArea = holder.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            var background = CreateImage(
                holder,
                "Blank Circular Chip",
                RoomContextVisualCatalog.GetSprite(RoomContextVisual.Chip));
            Stretch(background.rectTransform);
            background.preserveAspect = true;

            pictogram = CreateImage(holder, $"{name} Pictogram", null);
            Stretch(pictogram.rectTransform, 16f);
            pictogram.preserveAspect = true;
            return holder.gameObject;
        }

        private GameObject BuildRoomContextTooltip(string name, out Text tooltipText)
        {
            var panel = CreatePanel(name, roomContextRect,
                new Color(0.10f, 0.12f, 0.14f, 0.94f));
            panel.RectTransform.anchorMin = panel.RectTransform.anchorMax = new Vector2(0.5f, 0f);
            panel.RectTransform.pivot = new Vector2(0.5f, 1f);
            panel.RectTransform.anchoredPosition = new Vector2(0f, -8f);
            panel.RectTransform.sizeDelta = new Vector2(250f, 58f);
            panel.Image.raycastTarget = false;
            RoundSolidPanel(panel, 13f);
            tooltipText = CreateText(panel.Transform, "Explanation", string.Empty,
                15, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Normal);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(tooltipText.rectTransform, 10f);
            roomContextTooltipRects.Add(panel.RectTransform);
            return panel.GameObject;
        }

        private void LayoutRoomContextChips(bool showFunction, bool showUse)
        {
            if (!showFunction && !showUse)
            {
                roomTypeChip.GetComponent<RectTransform>().anchoredPosition = new Vector2(79f, 44f);
                return;
            }

            roomTypeChip.GetComponent<RectTransform>().anchoredPosition = new Vector2(10f, 38f);
            roomFunctionChip.GetComponent<RectTransform>().anchoredPosition = new Vector2(79f, 50f);
            roomUseChip.GetComponent<RectTransform>().anchoredPosition = new Vector2(148f, 38f);
        }

        private void BuildLayoutEditingControls()
        {
            var enterButton = CreateButton(gameplayRoot, "Enter Layout Editing", "Move", 18,
                () => layoutEditor?.EnterEditing());
            enterEditButtonObject = enterButton.gameObject;
            var enterBacking = enterButton.GetComponent<Image>();
            enterBacking.sprite = null;
            enterBacking.color = Color.clear;
            SetTopLeft(enterButton.GetComponent<RectTransform>(), 22f, 103f, 146f, 60f);
            var moveBacking = NewUiObject("Rounded Move Button Backing", enterButton.transform,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            enterEditBacking = moveBacking;
            moveBacking.color = Graphite;
            moveBacking.CornerRadius = 17f;
            moveBacking.BorderWidth = 2f;
            moveBacking.BorderColor = new Color(0.94f, 0.90f, 0.80f, 0.76f);
            moveBacking.raycastTarget = false;
            Stretch(moveBacking.rectTransform);
            moveBacking.transform.SetAsFirstSibling();
            enterButton.targetGraphic = moveBacking;
            var icon = NewUiObject("Shovel And Room Pictogram", enterButton.transform,
                typeof(CanvasRenderer), typeof(RoomMoveIconGraphic))
                .GetComponent<RoomMoveIconGraphic>();
            icon.color = WarmPaper;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, 6f, 2f, 56f, 56f);
            enterEditLabel = enterButton.GetComponentInChildren<Text>();
            enterEditLabel.alignment = TextAnchor.MiddleCenter;
            SetRect(enterEditLabel.rectTransform, 65f, 3f, 75f, 54f);

            var toolbar = CreatePanel("Layout Editing Toolbar", gameplayRoot, Color.white);
            toolbar.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            toolbar.Image.preserveAspect = false;
            editToolbar = toolbar.GameObject;
            toolbar.RectTransform.anchorMin = toolbar.RectTransform.anchorMax = new Vector2(0.5f, 0f);
            toolbar.RectTransform.pivot = new Vector2(0.5f, 0f);
            toolbar.RectTransform.anchoredPosition = new Vector2(0f, 22f);
            toolbar.RectTransform.sizeDelta = new Vector2(650f, 74f);

            var traySlot = CreatePanel("Temporary Tray Icon Slot", toolbar.Transform, new Color(0.16f, 0.19f, 0.20f, 0.92f));
            SetRect(traySlot.RectTransform, 12f, 10f, 54f, 54f);
            RoundSolidPanel(traySlot, 12f);
            trayRoomIcon = CreateImage(traySlot.Transform, "Stored Room Pictogram", null);
            trayRoomIcon.preserveAspect = true;
            Stretch(trayRoomIcon.rectTransform, 3f);
            trayRoomIcon.gameObject.SetActive(false);
            editStatus = CreateText(toolbar.Transform, "Layout Status", "布局编辑", 16, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            SetRect(editStatus.rectTransform, 80f, 12f, 276f, 50f);
            rotateEditButton = CreateButton(toolbar.Transform, "Rotate Selected", "↻", 25, () => layoutEditor?.RotateSelected());
            StyleCompactPaperButton(rotateEditButton);
            SetRect(rotateEditButton.GetComponent<RectTransform>(), 378f, 12f, 62f, 50f);
            var cancelButton = CreateButton(toolbar.Transform, "Cancel Layout", "×", 28, () => layoutEditor?.CancelEditing());
            StyleCompactPaperButton(cancelButton);
            SetRect(cancelButton.GetComponent<RectTransform>(), 450f, 12f, 62f, 50f);
            confirmEditButton = CreateButton(toolbar.Transform, "Confirm Layout", "✓", 25, () => layoutEditor?.ConfirmEditing());
            StyleCompactPaperButton(confirmEditButton);
            SetRect(confirmEditButton.GetComponent<RectTransform>(), 522f, 12f, 62f, 50f);

            var impact = CreatePanel("Layout Impact Preview", gameplayRoot,
                new Color(0.10f, 0.12f, 0.14f, 0.86f));
            impact.Image.raycastTarget = false;
            RoundSolidPanel(impact, 16f);
            layoutImpactPanel = impact.GameObject;
            // Keep the forecast beside the board: the former bottom-centred
            // strip covered the last row of rooms during planning.
            impact.RectTransform.anchorMin = impact.RectTransform.anchorMax = Vector2.zero;
            impact.RectTransform.pivot = Vector2.zero;
            impact.RectTransform.anchoredPosition = new Vector2(22f, 180f);
            impact.RectTransform.sizeDelta = new Vector2(420f, 255f);
            layoutImpactText = CreateText(impact.Transform, "Projected Layout Consequences",
                string.Empty, 15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            layoutImpactText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(layoutImpactText.rectTransform, 17f, 199f, 353f, 49f);
            var scope = CreatePanel("Forecast Scope", impact.Transform,
                new Color(0.25f, 0.29f, 0.29f, 0.94f));
            SetRect(scope.RectTransform, 382f, 215f, 24f, 24f);
            RoundSolidPanel(scope, 12f);
            var question = CreateText(scope.Transform, "Scope Symbol", "?", 15,
                TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            Stretch(question.rectTransform);
            var scopeTip = CreatePanel("Scope Explanation", scope.Transform,
                new Color(0.10f, 0.12f, 0.14f, 0.97f));
            scopeTip.Image.raycastTarget = false;
            RoundSolidPanel(scopeTip, 12f);
            scopeTip.RectTransform.anchorMin = scopeTip.RectTransform.anchorMax = new Vector2(1f, 1f);
            scopeTip.RectTransform.pivot = new Vector2(1f, 0f);
            scopeTip.RectTransform.anchoredPosition = new Vector2(0f, 8f);
            scopeTip.RectTransform.sizeDelta = new Vector2(390f, 72f);
            layoutImpactScopeText = CreateText(scopeTip.Transform, "Scope Text",
                "仅预测布局指标；不预测动物实际路线、存量或车库风险。",
                13, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            layoutImpactScopeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(layoutImpactScopeText.rectTransform, 10f, 6f);
            scope.GameObject.AddComponent<EcologicalIndicatorHover>().Initialize(scopeTip.GameObject);
            for (var index = 0; index < layoutImpactCards.Length; index++)
            {
                layoutImpactCards[index] = BuildLayoutImpactCard(impact.Transform, index);
            }
            layoutImpactPreviousButton = CreateButton(impact.Transform,
                "Previous Impact Page", "‹", 23, () => ChangeLayoutImpactPage(-1));
            StyleCompactPaperButton(layoutImpactPreviousButton);
            SetRect(layoutImpactPreviousButton.GetComponent<RectTransform>(), 145f, 5f, 36f, 27f);
            layoutImpactNextButton = CreateButton(impact.Transform,
                "Next Impact Page", "›", 23, () => ChangeLayoutImpactPage(1));
            StyleCompactPaperButton(layoutImpactNextButton);
            SetRect(layoutImpactNextButton.GetComponent<RectTransform>(), 239f, 5f, 36f, 27f);
            layoutImpactPageLabel = CreateText(impact.Transform, "Impact Page Count",
                string.Empty, 12, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Normal);
            SetRect(layoutImpactPageLabel.rectTransform, 185f, 5f, 50f, 27f);
            layoutImpactPanel.SetActive(false);
            editToolbar.SetActive(false);
        }

        private LayoutImpactCard BuildLayoutImpactCard(Transform parent, int index)
        {
            var panel = CreatePanel($"Impact Metric {index + 1}", parent,
                new Color(0.17f, 0.20f, 0.21f, 0.92f));
            SetRect(panel.RectTransform, 14f, 119f - index * 83f, 392f, 75f);
            RoundSolidPanel(panel, 12f);
            panel.Image.raycastTarget = true;
            var signal = CreateImage(panel.Transform, "Impact Signal", null);
            SetRect(signal.rectTransform, 4f, 11f, 4f, 53f);
            var icon = NewUiObject("Metric Icon", panel.Transform,
                typeof(CanvasRenderer), typeof(LayoutImpactPictogramGraphic))
                .GetComponent<LayoutImpactPictogramGraphic>();
            icon.color = WarmPaper;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, 10f, 14f, 46f, 46f);
            var label = CreateText(panel.Transform, "Metric Label", string.Empty,
                15, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            SetRect(label.rectTransform, 65f, 43f, 310f, 25f);
            var before = CreateText(panel.Transform, "Baseline Value", string.Empty,
                16, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            SetRect(before.rectTransform, 65f, 9f, 87f, 31f);
            var delta = CreateText(panel.Transform, "Delta Value", string.Empty,
                19, TextAnchor.MiddleLeft, Cyan, FontStyle.Bold);
            SetRect(delta.rectTransform, 171f, 9f, 204f, 31f);
            var tooltip = CreatePanel("Metric Explanation", panel.Transform,
                new Color(0.10f, 0.12f, 0.14f, 0.97f));
            tooltip.Image.raycastTarget = false;
            RoundSolidPanel(tooltip, 12f);
            tooltip.RectTransform.anchorMin = tooltip.RectTransform.anchorMax = new Vector2(0f, 1f);
            tooltip.RectTransform.pivot = Vector2.zero;
            tooltip.RectTransform.anchoredPosition = new Vector2(0f, 8f);
            tooltip.RectTransform.sizeDelta = new Vector2(392f, 68f);
            var explanation = CreateText(tooltip.Transform, "Explanation Text", string.Empty,
                13, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            explanation.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(explanation.rectTransform, 10f, 6f);
            panel.GameObject.AddComponent<EcologicalIndicatorHover>().Initialize(tooltip.GameObject);
            return new LayoutImpactCard(panel.GameObject, signal, icon, label, before, delta, explanation);
        }

        private void ChangeLayoutImpactPage(int direction)
        {
            layoutImpactPage += direction;
            RefreshLayoutEditor();
        }

        private void RenderLayoutImpactCard(LayoutImpactCard card, LayoutImpactMetric? maybeMetric,
            bool chinese)
        {
            card.Root.SetActive(maybeMetric.HasValue);
            if (!maybeMetric.HasValue)
            {
                return;
            }

            var metric = maybeMetric.Value;
            card.Icon.Kind = metric.Kind;
            card.Label.text = metric.Label;
            card.Baseline.text = chinese ? $"原 {metric.BeforeText}" : $"Was {metric.BeforeText}";
            var direction = metric.IsRisk ? "! " : metric.Delayed || metric.Neutral ? "· " : metric.Changed ? "✓ " : "= ";
            card.Delta.text = $"{direction}{(metric.Changed ? metric.DeltaText : "±0")} → {metric.After:0.#}";
            card.Delta.color = metric.Neutral ? WarmPaper : metric.Delayed
                ? new Color(0.98f, 0.76f, 0.45f, 1f)
                : metric.IsRisk ? new Color(0.95f, 0.62f, 0.50f, 1f)
                : !metric.Changed ? WarmPaper : Cyan;
            card.Signal.color = card.Delta.color;
            card.Explanation.text = chinese
                ? $"原 {metric.BeforeText} → 预计 {metric.After:0.#}。{metric.Detail}"
                : $"Was {metric.BeforeText} → projected {metric.After:0.#}. {metric.Detail}";
        }

        private void BuildFeedingControls()
        {
            feedingModeButton = CreateButton(
                gameplayRoot,
                "Activate Feeding Mode",
                string.Empty,
                18,
                () => playerFeeding?.ToggleFeedingMode());
            feedingModeButtonObject = feedingModeButton.gameObject;
            var feedingHitArea = feedingModeButton.GetComponent<Image>();
            feedingHitArea.sprite = null;
            feedingHitArea.color = Color.clear;
            SetTopLeft(feedingModeButton.GetComponent<RectTransform>(), 178f, 103f, 146f, 60f);
            feedingModeButtonBacking = NewUiObject("Rounded Feeding Button Backing", feedingModeButton.transform,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            feedingModeButtonBacking.color = Graphite;
            feedingModeButtonBacking.CornerRadius = 17f;
            feedingModeButtonBacking.BorderWidth = 2f;
            feedingModeButtonBacking.BorderColor = new Color(0.94f, 0.90f, 0.80f, 0.76f);
            feedingModeButtonBacking.raycastTarget = false;
            Stretch(feedingModeButtonBacking.rectTransform);
            feedingModeButtonBacking.transform.SetAsFirstSibling();
            feedingModeButton.targetGraphic = feedingModeButtonBacking;
            var feedingColors = feedingModeButton.colors;
            feedingColors.disabledColor = Color.white;
            feedingModeButton.colors = feedingColors;

            feedingModeLabel = feedingModeButton.GetComponentInChildren<Text>();
            feedingModeLabel.alignment = TextAnchor.MiddleCenter;
            SetRect(feedingModeLabel.rectTransform, 65f, 3f, 75f, 54f);

            feedingModeIcon = NewUiObject("Food Dish Pictogram", feedingModeButton.transform,
                typeof(CanvasRenderer), typeof(FeedingModeIconGraphic))
                .GetComponent<FeedingModeIconGraphic>();
            feedingModeIcon.color = WarmPaper;
            feedingModeIcon.raycastTarget = false;
            SetRect(feedingModeIcon.rectTransform, 6f, 2f, 56f, 56f);

            var hint = CreatePanel(
                "Feeding Mode Instruction",
                gameplayRoot,
                Color.white);
            hint.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            hint.Image.preserveAspect = false;
            feedingModeHint = hint.GameObject;
            SetTopLeft(hint.RectTransform, 342f, 107f, 410f, 52f);
            feedingModeHintLabel = CreateText(
                hint.Transform,
                "Feeding Instruction",
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            Stretch(feedingModeHintLabel.rectTransform, 8f);
            feedingModeHint.SetActive(false);
        }

        private void RefreshFeedingControls()
        {
            if (feedingModeButton == null || playerFeeding == null || runtime == null)
            {
                return;
            }

            var visible = !runtime.AtDesktop && !runtime.ResultsOpen && !runtime.LayoutEditing;
            var active = playerFeeding.FeedingModeActive;
            var available = playerFeeding.CanActivateFeedingMode;
            var cooldownDays = playerFeeding.ManualFeedingDaysRemaining;
            var actionAlreadyUsed = runtime.TodayAction is DailyActionKind.Rearrange or
                DailyActionKind.Transform;
            feedingModeButtonObject.SetActive(visible);
            feedingModeButton.interactable = active || available;
            feedingModeButtonBacking.color = active
                ? new Color(0.18f, 0.45f, 0.43f, 0.98f)
                : available ? Graphite : MutedTrack;
            feedingModeButtonBacking.BorderColor = active
                ? Cyan
                : available
                    ? new Color(0.96f, 0.69f, 0.38f, 0.94f)
                    : new Color(0.78f, 0.75f, 0.67f, 0.46f);
            feedingModeIcon.color = available || active
                ? WarmPaper
                : new Color(0.85f, 0.82f, 0.76f, 0.72f);
            feedingModeLabel.color = available || active
                ? WarmPaper
                : new Color(0.91f, 0.87f, 0.77f, 0.72f);

            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            feedingModeLabel.text = active
                ? (chinese ? "取消" : "Cancel")
                : actionAlreadyUsed
                    ? chinese ? "今日已行动" : "Action used"
                : cooldownDays > 0
                    ? chinese ? $"冷却 {cooldownDays}天" : $"Wait {cooldownDays}d"
                    : (chinese ? "投喂" : "Feed");
            feedingModeLabel.fontSize = (cooldownDays > 0 || actionAlreadyUsed) && !active ? 16 : 18;
            var showWasteNotice = Time.unscaledTime < feedingWasteNoticeUntil && !active;
            if (showWasteNotice)
            {
                var roomName = feedingWasteRoomId;
                foreach (var room in RoomLayoutData.All)
                {
                    if (room.Id == feedingWasteRoomId)
                    {
                        roomName = UrbanPalette.LocalizedRoomName(room, chinese);
                        break;
                    }
                }
                feedingModeHintLabel.text = chinese
                    ? $"{roomName}：剩食 {feedingWastePortions} 份 → 垃圾 +1{(feedingWasteRouted ? "" : "（清运受阻）")}"
                    : $"{roomName}: {feedingWastePortions} leftovers → waste +1{(feedingWasteRouted ? "" : " (route blocked)")}";
            }
            else if (actionAlreadyUsed)
            {
                feedingModeHintLabel.text = chinese
                    ? "今日已调整房间 · 明天才能投喂"
                    : "Rooms rearranged today · feed tomorrow";
            }
            else if (cooldownDays > 0)
            {
                feedingModeHintLabel.text = chinese
                    ? $"每 3 天可投喂 1 次 · 第 {playerFeeding.NextManualFeedingDay} 天可用"
                    : $"One feed every 3 days · available on day {playerFeeding.NextManualFeedingDay}";
            }
            else
            {
                feedingModeHintLabel.text = chinese
                    ? "每 3 天 1 次 · 点击空地投喂 · 剩食变垃圾"
                    : "Once every 3 days · click ground · leftovers become waste";
            }
            feedingModeHint.SetActive(visible && (active || showWasteNotice || actionAlreadyUsed || cooldownDays > 0));
        }

        private void BuildPauseMenu()
        {
            var overlay = CreatePanel("Esc Overlay", transform, new Color(0.04f, 0.055f, 0.065f, 0.74f));
            pauseOverlay = overlay.GameObject;
            Stretch(overlay.RectTransform);

            var pause = CreatePanel("Pause Paper Panel", overlay.Transform, Color.white);
            pause.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.Panel);
            pause.Image.preserveAspect = false;
            pausePanel = pause.GameObject;
            pausePanelRect = pause.RectTransform;
            SetCenter(pausePanelRect, 460f, 750f);
            pauseTitle = CreateText(pause.Transform, "Pause Title", "暂停", 34, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            SetTopCenter(pauseTitle.rectTransform, 0f, 54f, 340f, 52f);

            continueLabel = BuildPauseAction(
                pause.Transform,
                "Continue",
                PauseMenuVisual.ContinueIcon,
                132f,
                runtime.ContinueGame);
            settingsLabel = BuildPauseAction(
                pause.Transform,
                "Settings",
                PauseMenuVisual.SettingsIcon,
                222f,
                runtime.OpenSettings);
            languageLabel = BuildPauseAction(
                pause.Transform,
                "Language",
                PauseMenuVisual.LanguageIcon,
                312f,
                runtime.ToggleLanguage);
            helpLabel = BuildPauseAction(
                pause.Transform,
                "Replay Onboarding",
                PauseMenuVisual.ContinueIcon,
                402f,
                ReplayOnboarding);
            restartLabel = BuildPauseAction(
                pause.Transform,
                "Restart Endless Mode",
                PauseMenuVisual.RestartIcon,
                492f,
                BeginSandboxRestartConfirmation);
            restartPauseAction = restartLabel.transform.parent.gameObject;
            desktopLabel = BuildPauseAction(
                pause.Transform,
                "Return Desktop",
                PauseMenuVisual.ReturnDesktopIcon,
                582f,
                runtime.ReturnToDesktop);
            desktopPauseActionRect = desktopLabel.transform.parent.GetComponent<RectTransform>();

            var restartConfirmation = CreatePanel(
                "Restart Confirmation Panel",
                overlay.Transform,
                Color.white);
            restartConfirmation.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.SettingsPanel);
            restartConfirmation.Image.preserveAspect = false;
            restartConfirmationPanel = restartConfirmation.GameObject;
            SetCenter(restartConfirmation.RectTransform, 560f, 360f);
            restartConfirmationTitle = CreateText(
                restartConfirmation.Transform,
                "Restart Confirmation Title",
                "重新开始？",
                30,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Bold);
            SetTopCenter(restartConfirmationTitle.rectTransform, 0f, 76f, 430f, 46f);
            restartConfirmationMessage = CreateText(
                restartConfirmation.Transform,
                "Restart Confirmation Message",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Normal);
            SetTopCenter(restartConfirmationMessage.rectTransform, 0f, 137f, 440f, 68f);
            var cancelRestartButton = CreateRoundedRestartButton(
                restartConfirmation.Transform,
                "Cancel Restart",
                Graphite,
                CancelSandboxRestart);
            SetTopCenter(cancelRestartButton.GetComponent<RectTransform>(), -108f, 238f, 190f, 58f);
            restartCancelLabel = cancelRestartButton.GetComponentInChildren<Text>();
            var confirmRestartButton = CreateRoundedRestartButton(
                restartConfirmation.Transform,
                "Confirm Restart",
                new Color(0.72f, 0.30f, 0.22f, 1f),
                ConfirmSandboxRestart);
            SetTopCenter(confirmRestartButton.GetComponent<RectTransform>(), 108f, 238f, 190f, 58f);
            restartConfirmLabel = confirmRestartButton.GetComponentInChildren<Text>();
            restartConfirmationPanel.SetActive(false);

            var settings = CreatePanel("Settings Paper Panel", overlay.Transform, Color.white);
            settings.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.SettingsPanel);
            settings.Image.preserveAspect = false;
            settingsPanel = settings.GameObject;
            var cameraSettings = BuildVariantSettings.UsesCameraRecognition;
            var settingsWidth = cameraSettings ? 700f : 640f;
            SetCenter(settings.RectTransform, settingsWidth, cameraSettings ? 580f : 500f);
            settingsTitle = CreateText(settings.Transform, "Settings Title", "设置", 26, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            SetTopCenter(settingsTitle.rectTransform, 0f, 36f, 240f, 42f);
            volumeLabel = CreateText(settings.Transform, "Volume Label", "游戏音量", 21, TextAnchor.MiddleLeft, Graphite, FontStyle.Bold);
            var settingsContentLeft = (settingsWidth - 472f) * 0.5f;
            SetTopLeft(volumeLabel.rectTransform, settingsContentLeft, 110f, 220f, 35f);
            volumeSlider = CreateSlider(settings.Transform, "Master Volume");
            SetTopLeft(volumeSlider.GetComponent<RectTransform>(), settingsContentLeft, 154f, 472f, 34f);
            volumeSlider.onValueChanged.AddListener(runtime.SetMasterVolume);
            muteLabel = BuildSettingsAction(
                settings.Transform,
                "Mute",
                PauseMenuVisual.MuteIcon,
                202f,
                runtime.ToggleMute,
                out muteIcon);
            settingsLanguageLabel = BuildSettingsAction(
                settings.Transform,
                "Settings Language",
                PauseMenuVisual.LanguageIcon,
                278f,
                runtime.ToggleLanguage,
                out _);
            if (cameraSettings)
            {
                cameraCalibrationLabel = BuildSettingsAction(
                    settings.Transform,
                    "Recalibrate Camera",
                    PauseMenuVisual.CameraRecalibrateIcon,
                    354f,
                    runtime.RequestCameraRecalibration,
                    out _);
            }
            backLabel = BuildSettingsAction(
                settings.Transform,
                "Back",
                PauseMenuVisual.BackIcon,
                cameraSettings ? 430f : 354f,
                runtime.CloseSettings,
                out _);
        }

        private void BuildCameraCalibrationOverlay()
        {
            var overlay = CreatePanel(
                "Camera Calibration Overlay",
                transform,
                new Color(0.035f, 0.045f, 0.055f, 0.86f));
            cameraCalibrationOverlay = overlay.GameObject;
            Stretch(overlay.RectTransform);

            var frame = CreatePanel(
                "Live Camera Paper Frame",
                overlay.Transform,
                new Color(0.88f, 0.80f, 0.66f, 1f));
            SetCenter(frame.RectTransform, 720f, 720f);
            frame.RectTransform.anchoredPosition = new Vector2(0f, 40f);

            var previewObject = NewUiObject(
                "Live Top Down Camera Preview",
                frame.Transform,
                typeof(CanvasRenderer),
                typeof(RawImage));
            cameraCalibrationPreview = previewObject.GetComponent<RawImage>();
            cameraCalibrationPreview.color = new Color(0.08f, 0.10f, 0.11f, 1f);
            cameraCalibrationPreview.raycastTarget = false;
            SetRect(cameraCalibrationPreview.rectTransform, 28f, 28f, 664f, 664f);

            var cellLayer = CreateEmpty("Recognition Cell States", frame.Transform);
            SetRect(cellLayer, 28f, 28f, 664f, 664f);
            var cellSize = 664f / 7f;
            for (var row = 0; row < 7; row++)
            {
                for (var column = 0; column < 7; column++)
                {
                    var index = row * 7 + column;
                    var cell = CreatePanel($"Recognition Cell {index + 1}", cellLayer, Color.clear);
                    cell.Image.raycastTarget = false;
                    SetRect(
                        cell.RectTransform,
                        column * cellSize + 2f,
                        (6 - row) * cellSize + 2f,
                        cellSize - 4f,
                        cellSize - 4f);
                    var outline = cell.GameObject.AddComponent<Outline>();
                    outline.effectColor = Color.clear;
                    outline.effectDistance = new Vector2(2f, -2f);
                    outline.useGraphicAlpha = false;
                    cameraCalibrationCells[index] = cell.Image;
                }
            }

            var gridLayer = CreateEmpty("Seven By Seven Grid", frame.Transform);
            SetRect(gridLayer, 28f, 28f, 664f, 664f);
            for (var line = 1; line < 7; line++)
            {
                var vertical = CreatePanel(
                    $"Vertical Grid {line}",
                    gridLayer,
                    new Color(0.20f, 0.84f, 0.86f, 0.58f));
                SetRect(vertical.RectTransform, line * cellSize - 1f, 0f, 2f, 664f);
                vertical.Image.raycastTarget = false;

                var horizontal = CreatePanel(
                    $"Horizontal Grid {line}",
                    gridLayer,
                    new Color(0.20f, 0.84f, 0.86f, 0.58f));
                SetRect(horizontal.RectTransform, 0f, line * cellSize - 1f, 664f, 2f);
                horizontal.Image.raycastTarget = false;
            }

            BuildCalibrationCorner(frame.Transform, "North West", -16f, 650f, 0f);
            BuildCalibrationCorner(frame.Transform, "North East", 650f, 650f, -90f);
            BuildCalibrationCorner(frame.Transform, "South East", 650f, -16f, 180f);
            BuildCalibrationCorner(frame.Transform, "South West", -16f, -16f, 90f);

            var cameraIcon = CreateImage(
                overlay.Transform,
                "Calibration Camera Pictogram",
                PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.CameraRecalibrateIcon));
            cameraIcon.preserveAspect = true;
            SetCenter(cameraIcon.rectTransform, 96f, 96f);
            cameraIcon.rectTransform.anchoredPosition = new Vector2(0f, 476f);

            var startPanel = CreatePanel("Calibration Start", overlay.Transform, Color.white);
            startPanel.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            startPanel.Image.preserveAspect = false;
            cameraCalibrationStartBacking = startPanel.Image;
            SetCenter(startPanel.RectTransform, 420f, 88f);
            startPanel.RectTransform.anchoredPosition = new Vector2(0f, -412f);
            cameraCalibrationStartButton = startPanel.GameObject.AddComponent<Button>();
            cameraCalibrationStartButton.targetGraphic = startPanel.Image;
            cameraCalibrationStartButton.onClick.AddListener(runtime.CompleteCameraCalibration);

            var playIcon = CreateImage(
                startPanel.Transform,
                "Start Pictogram",
                PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ContinueIcon));
            playIcon.preserveAspect = true;
            SetCenter(playIcon.rectTransform, 54f, 54f);
        }

        private void BuildCalibrationCorner(
            Transform parent,
            string name,
            float left,
            float bottom,
            float rotation)
        {
            var corner = CreateImage(parent, name, RoomSelectionVisualCatalog.GetCornerSprite());
            corner.color = new Color(0.58f, 0.45f, 0.60f, 1f);
            corner.preserveAspect = true;
            SetRect(corner.rectTransform, left, bottom, 86f, 86f);
            corner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void BuildCameraRecognitionFeedback()
        {
            cameraRecognitionFeedbackRect = CreateEmpty(
                "Physical Board Recognition Feedback",
                gameplayRoot);
            cameraRecognitionFeedbackRoot = cameraRecognitionFeedbackRect.gameObject;
            SetCenter(cameraRecognitionFeedbackRect, 900f, 900f);

            cameraRecognitionScanFrame = CreateImage(
                cameraRecognitionFeedbackRect,
                "Cyan Perimeter Progress",
                CameraRecognitionVisualCatalog.GetSprite(
                    CameraRecognitionVisual.CyanPerimeter));
            Stretch(cameraRecognitionScanFrame.rectTransform);
            cameraRecognitionScanFrame.preserveAspect = false;
            cameraRecognitionScanFrame.type = Image.Type.Filled;
            cameraRecognitionScanFrame.fillMethod = Image.FillMethod.Radial360;
            cameraRecognitionScanFrame.fillOrigin = (int)Image.Origin360.Top;
            cameraRecognitionScanFrame.fillClockwise = true;
            cameraRecognitionScanFrame.fillAmount = 0f;
            cameraRecognitionScanFrame.raycastTarget = false;

            cameraRecognitionConfirmationFrame = CreateImage(
                cameraRecognitionFeedbackRect,
                "Green Confirmation Flash",
                CameraRecognitionVisualCatalog.GetSprite(
                    CameraRecognitionVisual.GreenPerimeter));
            Stretch(cameraRecognitionConfirmationFrame.rectTransform);
            cameraRecognitionConfirmationFrame.preserveAspect = false;
            cameraRecognitionConfirmationFrame.raycastTarget = false;
            cameraRecognitionConfirmationFrame.gameObject.SetActive(false);

            cameraInvalidPlacementRoot = CreateEmpty(
                "Invalid Physical Placements",
                gameplayRoot);
            Stretch(cameraInvalidPlacementRoot);
            cameraInvalidPlacementRoot.gameObject.SetActive(false);

            cameraRecognitionFeedbackRoot.SetActive(false);
        }

        private void BuildDesktopOverlay()
        {
            var overlay = CreatePanel("Main Menu", transform, Color.clear);
            desktopOverlay = overlay.GameObject;
            desktopCanvasGroup = overlay.GameObject.AddComponent<CanvasGroup>();
            Stretch(overlay.RectTransform);

            // The same live board stays under both the menu and gameplay HUD.
            // A full-screen menu illustration would hide the camera's transition
            // and make entering a mode look like a cut to a different scene.

            BuildDesktopTitle(overlay.Transform);

            var sandboxButton = BuildModeCard(
                overlay.Transform,
                "Sandbox Mode Entrance",
                MainMenuVisual.SandboxIcon,
                HandleSandboxEntry,
                out desktopSandboxLabel);
            desktopSandboxCard = sandboxButton.gameObject;
            desktopSandboxCardRect = sandboxButton.GetComponent<RectTransform>();

            var researchButton = BuildModeCard(
                overlay.Transform,
                "Research Mode Entrance",
                MainMenuVisual.ResearchIcon,
                HandleResearchEntry,
                out desktopResearchLabel);
            desktopResearchCard = researchButton.gameObject;
            desktopResearchCardRect = researchButton.GetComponent<RectTransform>();

            var recordPanel = CreatePanel("Best Survival Record", overlay.Transform, Color.clear);
            recordPanel.Image.raycastTarget = false;
            desktopBestRecordRect = recordPanel.RectTransform;
            ConfigureResponsiveMenuElement(desktopBestRecordRect, new Vector2(0.5f, 0.205f), 390f, 80f);
            var recordArtwork = NewUiObject("Best Record Rounded Backing", recordPanel.Transform,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            recordArtwork.CornerRadius = 40f;
            recordArtwork.BorderWidth = 2.5f;
            recordArtwork.BorderColor = new Color(0.16f, 0.25f, 0.32f, 0.92f);
            recordArtwork.color = new Color(0.96f, 0.92f, 0.82f, 0.9f);
            recordArtwork.raycastTarget = false;
            Stretch(recordArtwork.rectTransform);
            var recordIcon = CreateImage(
                recordPanel.Transform,
                "Survival Days Pictogram",
                ResultsVisualCatalog.GetSprite(ResultsVisual.DaysSurvived));
            recordIcon.preserveAspect = true;
            SetRect(recordIcon.rectTransform, 18f, 13f, 54f, 54f);
            desktopBestRecordLabel = CreateText(
                recordPanel.Transform,
                "Best Survival Value",
                "尚无记录",
                19,
                TextAnchor.MiddleLeft,
                Graphite,
                FontStyle.Bold);
            SetRect(desktopBestRecordLabel.rectTransform, 88f, 10f, 280f, 60f);

            desktopSettingsLabel = BuildDesktopControl(
                overlay.Transform,
                "Desktop Settings",
                MainMenuVisual.SettingsIcon,
                -170f,
                runtime.OpenSettings);
            desktopLanguageLabel = BuildDesktopControl(
                overlay.Transform,
                "Desktop Language",
                MainMenuVisual.LanguageIcon,
                0f,
                runtime.ToggleLanguage);
            desktopExitLabel = BuildDesktopControl(
                overlay.Transform,
                "Desktop Exit",
                MainMenuVisual.ExitIcon,
                170f,
                runtime.ExitApplication);

            BuildSandboxDifficultyOverlay();
        }

        private void BuildSandboxDifficultyOverlay()
        {
            var shade = CreatePanel("Endless Difficulty Overlay", desktopOverlay.transform,
                new Color(0.04f, 0.06f, 0.08f, 0.82f));
            sandboxDifficultyOverlay = shade.GameObject;
            Stretch(shade.RectTransform);

            var card = CreatePanel("Endless Difficulty Card", shade.Transform,
                new Color(0.94f, 0.90f, 0.80f, 0.98f));
            RoundSolidPanel(card, 28f);
            SetCenter(card.RectTransform, 840f, 720f);

            sandboxDifficultyTitle = CreateText(card.Transform, "Difficulty Title",
                string.Empty, 32, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            SetTopCenter(sandboxDifficultyTitle.rectTransform, 0f, 25f, 740f, 48f);
            sandboxDifficultyHint = CreateText(card.Transform, "Difficulty Hint",
                string.Empty, 18, TextAnchor.MiddleCenter, Graphite, FontStyle.Normal);
            SetTopCenter(sandboxDifficultyHint.rectTransform, 0f, 76f, 740f, 48f);

            var choices = new[]
            {
                EndlessDifficulty.Gentle,
                EndlessDifficulty.Standard,
                EndlessDifficulty.Demanding
            };
            for (var index = 0; index < choices.Length; index++)
            {
                var choice = choices[index];
                var button = CreateRoundedRestartButton(card.Transform,
                    $"{choice} Difficulty", Graphite,
                    () => SelectSandboxTemplate(choice));
                SetTopCenter(button.GetComponent<RectTransform>(),
                    (index - 1) * 251f, 145f, 235f, 64f);
                var label = button.GetComponentInChildren<Text>();
                label.fontSize = 20;
                label.alignment = TextAnchor.MiddleCenter;
                sandboxDifficultyOptions[index] = label;
                sandboxDifficultyOptionBackings[index] =
                    button.GetComponent<RoundedPanelGraphic>();
            }

            for (var index = 0; index < sandboxDifficultyValues.Length; index++)
            {
                var row = index;
                var top = 237f + index * 78f;
                sandboxDifficultyRowLabels[index] = CreateText(card.Transform,
                    $"Difficulty Setting {index} Label", string.Empty, 21,
                    TextAnchor.MiddleLeft, Graphite, FontStyle.Bold);
                SetTopCenter(sandboxDifficultyRowLabels[index].rectTransform,
                    -175f, top, 370f, 56f);
                var minus = CreateRoundedRestartButton(card.Transform,
                    $"Difficulty Setting {index} Decrease", Graphite,
                    () => AdjustSandboxSetting(row, -1));
                SetTopCenter(minus.GetComponent<RectTransform>(), 105f, top + 3f, 50f, 50f);
                minus.GetComponentInChildren<Text>().text = "−";
                sandboxDifficultyValues[index] = CreateText(card.Transform,
                    $"Difficulty Setting {index} Value", string.Empty, 22,
                    TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
                SetTopCenter(sandboxDifficultyValues[index].rectTransform,
                    190f, top, 110f, 56f);
                var plus = CreateRoundedRestartButton(card.Transform,
                    $"Difficulty Setting {index} Increase", Graphite,
                    () => AdjustSandboxSetting(row, 1));
                SetTopCenter(plus.GetComponent<RectTransform>(), 275f, top + 3f, 50f, 50f);
                plus.GetComponentInChildren<Text>().text = "+";
            }

            sandboxDifficultySummary = CreateText(card.Transform, "Difficulty Rule Summary",
                string.Empty, 19, TextAnchor.MiddleCenter, Graphite, FontStyle.Normal);
            SetTopCenter(sandboxDifficultySummary.rectTransform, 0f, 552f, 740f, 66f);

            var back = CreateButton(card.Transform, "Back From Difficulty",
                string.Empty, 20, CloseSandboxDifficulty);
            sandboxDifficultyBackRect = back.GetComponent<RectTransform>();
            SetTopCenter(sandboxDifficultyBackRect, -145f, 633f, 240f, 56f);
            sandboxDifficultyBackLabel = back.GetComponentInChildren<Text>();
            var continueRun = CreateButton(card.Transform, "Continue Existing Endless",
                string.Empty, 20, ContinueExistingSandbox);
            sandboxDifficultyContinueButton = continueRun.gameObject;
            SetTopCenter(continueRun.GetComponent<RectTransform>(), 0f, 633f, 190f, 56f);
            sandboxDifficultyContinueLabel = continueRun.GetComponentInChildren<Text>();
            sandboxDifficultyContinueButton.SetActive(false);
            var start = CreateRoundedRestartButton(card.Transform,
                "Start Configured Endless", new Color(0.12f, 0.38f, 0.36f, 1f),
                StartConfiguredSandbox);
            sandboxDifficultyStartRect = start.GetComponent<RectTransform>();
            SetTopCenter(sandboxDifficultyStartRect, 145f, 633f, 240f, 56f);
            sandboxDifficultyStartLabel = start.GetComponentInChildren<Text>();
            sandboxDifficultyOverlay.SetActive(false);
        }

        private void SelectSandboxTemplate(EndlessDifficulty difficulty)
        {
            sandboxDifficultyReplacePending = false;
            sandboxDifficultyDraft = EndlessDifficultySettings.Preset(difficulty);
            UpdateSandboxDifficultyDisplay();
        }

        private void AdjustSandboxSetting(int row, int direction)
        {
            sandboxDifficultyReplacePending = false;
            switch (row)
            {
                case 0: sandboxDifficultyDraft.startingCommunity += direction * 5; break;
                case 1: sandboxDifficultyDraft.commuteTargetPercent += direction * 5; break;
                case 2: sandboxDifficultyDraft.wildlifeFloor += direction; break;
                case 3: sandboxDifficultyDraft.wildlifeGraceDays += direction; break;
            }
            sandboxDifficultyDraft = sandboxDifficultyDraft.Normalized();
            UpdateSandboxDifficultyDisplay();
        }

        private void StartConfiguredSandbox()
        {
            if (runtime.HasResumableRun && !sandboxDifficultyReplacePending)
            {
                sandboxDifficultyReplacePending = true;
                UpdateSandboxDifficultyDisplay();
                return;
            }
            sandboxDifficultyReplacePending = false;
            sandboxDifficultyOverlay.SetActive(false);
            SetDesktopMenuControlsVisible(true);
            runtime.StartNewRun(GameMode.Sandbox, sandboxDifficultyDraft);
        }

        private void ContinueExistingSandbox()
        {
            if (!runtime.HasResumableRun || runtime.Mode != GameMode.Sandbox) return;
            sandboxDifficultyReplacePending = false;
            sandboxDifficultyOverlay.SetActive(false);
            SetDesktopMenuControlsVisible(true);
            runtime.ResumeFromDesktop();
        }

        private void UpdateSandboxDifficultyDisplay()
        {
            if (sandboxDifficultyTitle == null) return;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var values = sandboxDifficultyDraft.Normalized();
            var resumable = runtime.HasResumableRun && runtime.Mode == GameMode.Sandbox;
            var hasPreviousRun = runtime.HasResumableRun;
            var gain = values.commuteTargetPercent;
            var stable = gain - 15;
            var minorLoss = gain - 40;
            sandboxDifficultyOptions[0].text = chinese ? "舒缓" : "Gentle";
            sandboxDifficultyOptions[1].text = chinese ? "标准" : "Standard";
            sandboxDifficultyOptions[2].text = chinese ? "挑战" : "Demanding";
            for (var index = 0; index < sandboxDifficultyOptionBackings.Length; index++)
                sandboxDifficultyOptionBackings[index].color = index == (int)values.template
                    ? new Color(0.12f, 0.38f, 0.36f, 1f) : Graphite;
            var labels = chinese
                ? new[] { "初始社区活力", "每日通勤达标", "动物数量警戒线", "低于警戒线宽限" }
                : new[] { "Starting community", "Daily commute target", "Wildlife warning floor", "Days below floor" };
            for (var index = 0; index < labels.Length; index++)
                sandboxDifficultyRowLabels[index].text = labels[index];
            sandboxDifficultyValues[0].text = values.startingCommunity.ToString();
            sandboxDifficultyValues[1].text = $"{values.commuteTargetPercent}%";
            sandboxDifficultyValues[2].text = values.wildlifeFloor.ToString();
            sandboxDifficultyValues[3].text = values.wildlifeGraceDays.ToString();
            sandboxDifficultyContinueButton.SetActive(resumable);
            SetTopCenter(sandboxDifficultyBackRect, resumable ? -245f : -145f,
                633f, resumable ? 190f : 240f, 56f);
            SetTopCenter(sandboxDifficultyStartRect, resumable ? 245f : 145f,
                633f, resumable ? 190f : 240f, 56f);
            sandboxDifficultyContinueLabel.text = chinese ? "继续旧进度" : "Continue";
            sandboxDifficultyStartLabel.text = sandboxDifficultyReplacePending
                ? chinese ? "确认新开局" : "Confirm New Run"
                : hasPreviousRun ? chinese ? "重新开局" : "New Run"
                : chinese ? "开始" : "Start";
            sandboxDifficultyHint.text = sandboxDifficultyReplacePending
                ? chinese ? "重新开局会覆盖当前存档；再点一次确认。"
                    : "A new run replaces the current save. Confirm once more."
                : hasPreviousRun && !resumable
                    ? chinese ? "当前有研究模式进度；新开无尽模式会替换该存档。"
                        : "An existing Research save will be replaced by a new Endless run."
                    : chinese ? "通勤指完成上班→吃饭→回家；连续 3 天失败的居民会离开。"
                        : "Commute means work → eat → home; residents leave after 3 failed days.";
            sandboxDifficultySummary.text = chinese
                ? $"{(values.IsCustom ? "自定义 · " : string.Empty)}通勤≥{gain}% 活力+4；≥{stable}% 0；≥{minorLoss}% −4；否则−8。\n动物总数<{values.wildlifeFloor} 持续{values.wildlifeGraceDays}天，或活力为0：结束。"
                : $"{(values.IsCustom ? "Custom · " : string.Empty)}Commute ≥{gain}%: +4 | ≥{stable}%: 0 | ≥{minorLoss}%: −4 | else: −8.\nWildlife <{values.wildlifeFloor} for {values.wildlifeGraceDays} days, or community = 0: run ends.";
        }

        private void CloseSandboxDifficulty()
        {
            sandboxDifficultyReplacePending = false;
            sandboxDifficultyOverlay.SetActive(false);
            SetDesktopMenuControlsVisible(true);
        }

        private void BuildResearchSetupOverlay()
        {
            if (researchSetupOverlay != null || desktopOverlay == null || !BuildVariantSettings.SupportsResearch)
            {
                return;
            }

            var shade = CreatePanel("Research Setup Overlay", desktopOverlay.transform, new Color(0.05f, 0.045f, 0.055f, 0.50f));
            researchSetupOverlay = shade.GameObject;
            Stretch(shade.RectTransform);

            var card = CreatePanel("Handcrafted Research Setup Card", shade.Transform, Color.white);
            card.Image.sprite = ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.Panel);
            card.Image.preserveAspect = false;
            SetCenter(card.RectTransform, 760f, 650f);
            card.RectTransform.anchoredPosition = new Vector2(0f, -65f);

            var back = CreateButton(card.Transform, "Back From Research Setup", "‹", 48, CloseResearchSetup);
            back.GetComponent<Image>().color = Color.clear;
            SetTopLeft(back.GetComponent<RectTransform>(), 34f, 54f, 62f, 62f);
            researchBackLabel = back.GetComponentInChildren<Text>();

            researchSetupTitle = CreateText(card.Transform, "Research Setup Title", "限时模式", 42, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            SetTopCenter(researchSetupTitle.rectTransform, 0f, 58f, 560f, 58f);
            var titleFlourish = CreateText(card.Transform, "Research Title Flourish", "—  ♧  —", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.80f, 0.65f, 0.94f), FontStyle.Normal);
            SetTopCenter(titleFlourish.rectTransform, 0f, 110f, 260f, 30f);

            var participantField = CreatePanel("Participant Field Artwork", card.Transform, Color.white);
            participantField.Image.sprite = ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.ParticipantField);
            participantField.Image.preserveAspect = false;
            SetTopCenter(participantField.RectTransform, 0f, 154f, 610f, 112f);

            var participantIcon = CreateImage(
                participantField.Transform,
                "Participant Identity Icon",
                ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.ParticipantIcon));
            participantIcon.preserveAspect = true;
            SetRect(participantIcon.rectTransform, 19f, 22f, 68f, 68f);

            researchParticipantLabel = CreateText(
                participantField.Transform,
                "Participant Code Label",
                "参与者",
                22,
                TextAnchor.MiddleLeft,
                WarmPaper,
                FontStyle.Bold);
            SetRect(researchParticipantLabel.rectTransform, 108f, 22f, 166f, 68f);

            var inputPanel = CreatePanel("Participant Code Input", participantField.Transform, Color.clear);
            SetRect(inputPanel.RectTransform, 298f, 25f, 250f, 62f);
            researchCodeInput = inputPanel.GameObject.AddComponent<InputField>();
            researchCodeInput.targetGraphic = inputPanel.Image;
            researchCodeInput.characterLimit = 16;
            var inputText = CreateText(inputPanel.Transform, "Participant Code", "P001", 24, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Bold);
            Stretch(inputText.rectTransform, 12f, 5f);
            researchCodeInput.textComponent = inputText;
            researchCodeInput.text = researchSession?.Model?.ParticipantCode ?? "P001";

            var editIcon = CreateImage(
                participantField.Transform,
                "Participant Edit Icon",
                ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.EditIcon));
            editIcon.preserveAspect = true;
            SetRect(editIcon.rectTransform, 558f, 32f, 44f, 44f);

            BuildResearchStepper(
                card.Transform,
                "Duration",
                ResearchSetupVisual.ClockIcon,
                -160f,
                294f,
                () => researchSession?.AdjustDuration(-6),
                () => researchSession?.AdjustDuration(6),
                out researchDurationValue);
            BuildResearchStepper(
                card.Transform,
                "Death Limit",
                ResearchSetupVisual.PawIcon,
                160f,
                294f,
                () => researchSession?.AdjustDeathLimit(-1),
                () => researchSession?.AdjustDeathLimit(1),
                out researchDeathLimitValue);

            var startArtwork = CreatePanel("Start Research Artwork", card.Transform, Color.white);
            startArtwork.Image.sprite = ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.StartButton);
            startArtwork.Image.preserveAspect = false;
            var start = startArtwork.GameObject.AddComponent<Button>();
            start.targetGraphic = startArtwork.Image;
            var startColors = start.colors;
            startColors.normalColor = Color.white;
            startColors.highlightedColor = new Color(1f, 0.97f, 0.87f, 1f);
            startColors.pressedColor = new Color(0.86f, 0.78f, 0.66f, 1f);
            start.colors = startColors;
            start.onClick.AddListener(StartConfiguredResearch);
            SetTopCenter(startArtwork.RectTransform, 0f, 484f, 470f, 112f);
            researchStartLabel = CreateText(
                startArtwork.Transform,
                "Label",
                "开始",
                34,
                TextAnchor.MiddleCenter,
                new Color(0.24f, 0.19f, 0.30f, 1f),
                FontStyle.Bold);
            Stretch(researchStartLabel.rectTransform, 94f, 12f);

            researchSetupOverlay.SetActive(false);
        }

        private void BuildResearchStepper(
            Transform parent,
            string name,
            ResearchSetupVisual iconVisual,
            float x,
            float top,
            UnityEngine.Events.UnityAction decrement,
            UnityEngine.Events.UnityAction increment,
            out Text value)
        {
            var stepper = CreatePanel($"{name} Stepper Artwork", parent, Color.white);
            stepper.Image.sprite = ResearchSetupVisualCatalog.GetSprite(ResearchSetupVisual.StepperCard);
            stepper.Image.preserveAspect = false;
            SetTopCenter(stepper.RectTransform, x, top, 300f, 128f);

            var icon = CreateImage(
                stepper.Transform,
                $"{name} Icon",
                ResearchSetupVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetTopCenter(icon.rectTransform, 0f, 17f, 52f, 52f);

            value = CreateText(stepper.Transform, $"{name} Value", string.Empty, 24, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            SetTopCenter(value.rectTransform, 0f, 72f, 184f, 38f);

            var previous = CreateButton(stepper.Transform, $"{name} Previous", string.Empty, 1, decrement);
            previous.GetComponent<Image>().color = Color.clear;
            SetRect(previous.GetComponent<RectTransform>(), 0f, 0f, 74f, 128f);
            var next = CreateButton(stepper.Transform, $"{name} Next", string.Empty, 1, increment);
            next.GetComponent<Image>().color = Color.clear;
            SetRect(next.GetComponent<RectTransform>(), 226f, 0f, 74f, 128f);
        }

        private void StartConfiguredResearch()
        {
            researchSession?.Configure(
                researchCodeInput != null ? researchCodeInput.text : "P001",
                researchSession.Model.DurationMinutes,
                researchSession.Model.DeathLimit);
            researchSetupOverlay?.SetActive(false);
            SetDesktopMenuControlsVisible(true);
            runtime.StartNewRun(GameMode.Research);
        }

        private void CloseResearchSetup()
        {
            researchSetupOverlay?.SetActive(false);
            SetDesktopMenuControlsVisible(true);
        }

        private void SetDesktopMenuControlsVisible(bool visible)
        {
            if (desktopSandboxCard != null)
            {
                desktopSandboxCard.SetActive(visible);
            }
            if (desktopResearchCard != null)
            {
                desktopResearchCard.SetActive(false);
            }
            if (desktopBestRecordRect != null)
            {
                desktopBestRecordRect.gameObject.SetActive(visible && runtime.BestSurvivalDays > 0);
            }

            SetDesktopControlVisible(desktopSettingsLabel, visible);
            SetDesktopControlVisible(desktopLanguageLabel, visible);
            SetDesktopControlVisible(desktopExitLabel, visible);
            if (visible)
            {
                RefreshDesktopEntries();
            }
        }

        private static void SetDesktopControlVisible(Text label, bool visible)
        {
            if (label != null && label.transform.parent != null)
            {
                label.transform.parent.gameObject.SetActive(visible);
            }
        }

        private void BuildDesktopTitle(Transform parent)
        {
            var title = CreateEmpty("Desktop Title", parent);
            ConfigureResponsiveMenuElement(title, new Vector2(0.5f, 0.81f), 1060f, 200f);

            var wordmark = CreateImage(
                title,
                "SYMBIOSIS 49 Wordmark",
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.TitleWordmark));
            wordmark.preserveAspect = true;
            Stretch(wordmark.rectTransform);
            // Preserve the source wordmark's width-to-height ratio. Stretch sets
            // the available area; Image.preserveAspect fits the cropped sprite.
            wordmark.rectTransform.localScale = Vector3.one;
            wordmark.raycastTarget = false;
        }

        private Text BuildDesktopControl(
            Transform parent,
            string name,
            MainMenuVisual iconVisual,
            float x,
            UnityEngine.Events.UnityAction action)
        {
            var root = CreateEmpty(name, parent);
            ConfigureResponsiveMenuElement(
                root,
                new Vector2(0.5f + (x / 1920f), 0.085f),
                140f,
                132f);

            var backing = CreatePanel("Paper Button Backing", root, Color.white);
            backing.Image.sprite = MainMenuVisualCatalog.GetSprite(MainMenuVisual.BottomButtonBase);
            backing.Image.preserveAspect = true;
            SetTopCenter(backing.RectTransform, 0f, 0f, 92f, 92f);

            var button = backing.GameObject.AddComponent<Button>();
            button.targetGraphic = backing.Image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.88f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.76f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);

            var icon = CreateImage(
                backing.Transform,
                "Control Pictogram",
                MainMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 18f, 18f, 56f, 56f);

            var label = CreateText(
                root,
                "Live Control Label",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Bold);
            SetTopCenter(label.rectTransform, 0f, 80f, 140f, 34f);
            return label;
        }

        private static void ConfigureResponsiveMenuElement(
            RectTransform rectTransform,
            Vector2 normalizedCenter,
            float width,
            float height)
        {
            rectTransform.anchorMin = normalizedCenter;
            rectTransform.anchorMax = normalizedCenter;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private Button BuildModeCard(
            Transform parent,
            string name,
            MainMenuVisual iconVisual,
            UnityEngine.Events.UnityAction action,
            out Text label)
        {
            var panel = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(RoundedPanelGraphic));
            var rounded = panel.GetComponent<RoundedPanelGraphic>();
            rounded.CornerRadius = 86f;
            rounded.BorderWidth = 3f;
            var button = panel.AddComponent<Button>();
            button.targetGraphic = rounded;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);

            var feedback = panel.AddComponent<MainMenuModeCardFeedback>();
            feedback.Initialize(rounded);

            var icon = CreateImage(panel.transform, "Mode Pictogram", MainMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 48f, 31f, 148f, 148f);

            label = CreateText(
                panel.transform,
                "Live Mode Label",
                string.Empty,
                64,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            label.resizeTextForBestFit = false;
            SetRect(label.rectTransform, 226f, 30f, 395f, 150f);

            var arrow = CreateText(panel.transform, "Enter Arrow", "›", 72,
                TextAnchor.MiddleCenter, WarmPaper, FontStyle.Normal);
            SetRect(arrow.rectTransform, 665f, 70f, 56f, 70f);
            return button;
        }

        private static void SetModeCardLabel(Text label, string caption, int preferredFontSize)
        {
            if (label == null || (label.text == caption && label.fontSize <= preferredFontSize))
            {
                return;
            }

            label.text = caption;
            label.resizeTextForBestFit = false;
            label.fontSize = preferredFontSize;
            var availableWidth = label.rectTransform.rect.width - 16f;
            while (label.fontSize > 36 && label.preferredWidth > availableWidth)
            {
                label.fontSize -= 2;
            }
        }

        private void HandleSandboxEntry()
        {
            sandboxDifficultyReplacePending = false;
            sandboxDifficultyDraft = runtime.HasResumableRun && runtime.Mode == GameMode.Sandbox
                ? runtime.SandboxSettings.Normalized()
                : EndlessDifficultySettings.Preset(EndlessDifficulty.Standard);
            sandboxDifficultyOverlay.SetActive(true);
            sandboxDifficultyOverlay.transform.SetAsLastSibling();
            SetDesktopMenuControlsVisible(false);
            RefreshLanguage();
        }

        private void HandleResearchEntry()
        {
            if (!BuildVariantSettings.SupportsResearch)
            {
                return;
            }

            if (runtime.HasResumableRun && runtime.Mode == GameMode.Research)
            {
                runtime.ResumeFromDesktop();
                return;
            }

            if (researchSetupOverlay != null)
            {
                researchCodeInput.text = researchSession.Model.ParticipantCode;
                researchSetupOverlay.SetActive(true);
                researchSetupOverlay.transform.SetAsLastSibling();
                SetDesktopMenuControlsVisible(false);
                RefreshResearchSession();
                return;
            }

            runtime.StartNewRun(GameMode.Research);
        }

        private void Refresh()
        {
            if (runtime == null || clockDial == null)
            {
                return;
            }

            clockDial.SetTime(runtime.Clock.CycleProgress, runtime.Clock.Phase);
            DetectDesktopStateChange();
            dayNumber.text = runtime.Clock.DayNumber.ToString();
            RefreshHedgehogNightReport();
            RefreshDailyOutcome();
            RefreshMarketForecast();
            RefreshNeedRisk();
            RefreshFoodLocations();
            RefreshAnimalPopulation();
            foreach (var pair in speedButtons)
            {
                var active = !runtime.IsDaySkipping && pair.Key == runtime.SpeedMultiplier && !runtime.PauseMenuOpen &&
                             !runtime.AtDesktop && !runtime.LayoutEditing;
                pair.Value.color = active
                    ? new Color(0.25f, 0.62f, 0.59f, 0.98f)
                    : new Color(0.14f, 0.18f, 0.21f, 0.92f);
                pair.Value.BorderColor = active
                    ? new Color(0.70f, 0.96f, 0.89f, 0.92f)
                    : new Color(0.94f, 0.90f, 0.80f, 0.27f);
                speedButtonLabels[pair.Key].color = active ? Color.white : WarmPaper;
            }
            if (skipDayButton != null)
            {
                skipDayButton.gameObject.SetActive(runtime.Mode == GameMode.Sandbox &&
                                                 runtime.HasActiveRun && !runtime.AtDesktop);
                skipDayButton.interactable = !runtime.IsPaused && !runtime.IsDaySkipping &&
                                             runtime.HasDailyAction;
                skipDayLabel.text = runtime.Language == InterfaceLanguage.Chinese
                    ? runtime.IsDaySkipping ? "快进中…"
                        : runtime.HasDailyAction ? "跳至次日" : "先选行动"
                    : runtime.IsDaySkipping ? "Skipping…"
                        : runtime.HasDailyAction ? "Next day" : "Choose action";
            }

            var desktopSettingsOpen = runtime.AtDesktop && runtime.SettingsOpen;
            var pauseVisible = runtime.PauseMenuOpen && !runtime.SettingsOpen &&
                               !runtime.AtDesktop && !runtime.ResultsOpen;
            var sandboxRestartVisible = runtime.Mode == GameMode.Sandbox && runtime.HasActiveRun;
            if (!pauseVisible || !sandboxRestartVisible)
            {
                restartConfirmationOpen = false;
            }
            pauseOverlay.SetActive(
                (runtime.PauseMenuOpen && !runtime.AtDesktop && !runtime.ResultsOpen) ||
                desktopSettingsOpen);
            pausePanel.SetActive(pauseVisible && !restartConfirmationOpen);
            restartConfirmationPanel.SetActive(pauseVisible && restartConfirmationOpen);
            restartPauseAction.SetActive(sandboxRestartVisible);
            SetCenter(pausePanelRect, 460f, sandboxRestartVisible ? 750f : 660f);
            SetTopCenter(
                desktopPauseActionRect,
                0f,
                sandboxRestartVisible ? 582f : 492f,
                350f,
                82f);
            settingsPanel.SetActive(runtime.SettingsOpen);
            if (runtime.AtDesktop)
            {
                SetDesktopMenuControlsVisible(
                    !desktopSettingsOpen &&
                    (researchSetupOverlay == null || !researchSetupOverlay.activeSelf));
            }
            var calibrationOpen = cameraCalibrationOverlay != null && runtime.CameraCalibrationOpen;
            onboardingController?.SetOverlaySuppressed(
                runtime.PauseMenuOpen || runtime.AtDesktop || runtime.ResultsOpen || calibrationOpen);
            if (cameraCalibrationOverlay != null)
            {
                cameraCalibrationOverlay.SetActive(calibrationOpen);
            }
            if (cameraCalibrationStartButton != null)
            {
                cameraCalibrationStartButton.interactable = runtime.CameraCalibrationReady;
                cameraCalibrationStartBacking.color = runtime.CameraCalibrationReady
                    ? Color.white
                    : new Color(0.54f, 0.57f, 0.60f, 0.74f);
            }
            var desktopTransitionActive = desktopTransitionCoroutine != null;
            desktopOverlay.SetActive(runtime.AtDesktop || desktopTransitionActive);
            if (calibrationOpen)
            {
                cameraCalibrationOverlay.transform.SetAsLastSibling();
            }
            else if (pauseOverlay.activeSelf)
            {
                pauseOverlay.transform.SetAsLastSibling();
            }
            else if (runtime.AtDesktop)
            {
                desktopOverlay.transform.SetAsLastSibling();
            }
            gameplayRoot.gameObject.SetActive(
                (!runtime.AtDesktop && !runtime.ResultsOpen) ||
                desktopTransitionActive);
            if (!desktopTransitionActive)
            {
                desktopCanvasGroup.alpha = runtime.AtDesktop ? 1f : 0f;
                desktopCanvasGroup.interactable = runtime.AtDesktop;
                desktopCanvasGroup.blocksRaycasts = runtime.AtDesktop;
                gameplayCanvasGroup.alpha = runtime.AtDesktop ? 0f : 1f;
            }
            volumeSlider.SetValueWithoutNotify(runtime.MasterVolume);
            if (runtime.PauseMenuOpen || runtime.AtDesktop || runtime.LayoutEditing)
            {
                HideRoomHover(hoveredRoom, true);
            }
            if (!desktopTransitionActive)
            {
                RefreshDesktopEntries();
            }
            RefreshCameraRecognitionFeedback();
            RefreshResearchSession();
            RefreshLanguage();
            RefreshMortalityIndicator();
            RefreshFeedingControls();
        }

        private void DetectDesktopStateChange()
        {
            if (!desktopStateKnown)
            {
                desktopStateKnown = true;
                previousDesktopState = runtime.AtDesktop;
                return;
            }

            if (previousDesktopState == runtime.AtDesktop)
            {
                return;
            }

            previousDesktopState = runtime.AtDesktop;
            if (!Application.isPlaying)
            {
                return;
            }

            if (desktopTransitionCoroutine != null)
            {
                StopCoroutine(desktopTransitionCoroutine);
                desktopTransitionCoroutine = null;
            }

            if (runtime.AtDesktop)
            {
                RefreshDesktopEntries();
            }
            desktopTransitionCoroutine = StartCoroutine(AnimateDesktopTransition(runtime.AtDesktop));
        }

        private IEnumerator AnimateDesktopTransition(bool toDesktop)
        {
            var sandboxRest = desktopSandboxCardRect.anchoredPosition;
            var researchRest = desktopResearchCardRect.anchoredPosition;
            var sandboxEdge = sandboxRest + new Vector2(0f, 155f);
            var researchEdge = researchRest + new Vector2(0f, -155f);

            desktopOverlay.SetActive(true);
            gameplayRoot.gameObject.SetActive(true);
            desktopCanvasGroup.interactable = false;
            desktopCanvasGroup.blocksRaycasts = false;

            if (toDesktop)
            {
                desktopCanvasGroup.alpha = 0f;
                gameplayCanvasGroup.alpha = 1f;
                desktopSandboxCardRect.anchoredPosition = sandboxEdge;
                desktopResearchCardRect.anchoredPosition = researchEdge;
                boardCamera?.BeginMenuViewTransition(DesktopTransitionDuration);
            }
            else
            {
                desktopCanvasGroup.alpha = 1f;
                gameplayCanvasGroup.alpha = 0f;
                desktopSandboxCardRect.anchoredPosition = sandboxRest;
                desktopResearchCardRect.anchoredPosition = researchRest;
                boardCamera?.BeginGameplayViewTransition(DesktopTransitionDuration);
            }

            var elapsed = 0f;
            while (elapsed < DesktopTransitionDuration)
            {
                var edgeProgress = toDesktop
                    ? Mathf.Clamp01(
                        (elapsed - (DesktopTransitionDuration - DesktopEdgeFadeDuration)) /
                        DesktopEdgeFadeDuration)
                    : Mathf.Clamp01(elapsed / DesktopEdgeFadeDuration);
                edgeProgress = SmoothStep(edgeProgress);

                if (toDesktop)
                {
                    desktopCanvasGroup.alpha = edgeProgress;
                    desktopSandboxCardRect.anchoredPosition = Vector2.LerpUnclamped(
                        sandboxEdge,
                        sandboxRest,
                        edgeProgress);
                    desktopResearchCardRect.anchoredPosition = Vector2.LerpUnclamped(
                        researchEdge,
                        researchRest,
                        edgeProgress);
                    var hudProgress = SmoothStep(Mathf.Clamp01(elapsed / GameplayHudFadeDuration));
                    gameplayCanvasGroup.alpha = 1f - hudProgress;
                }
                else
                {
                    desktopCanvasGroup.alpha = 1f - edgeProgress;
                    desktopSandboxCardRect.anchoredPosition = Vector2.LerpUnclamped(
                        sandboxRest,
                        sandboxEdge,
                        edgeProgress);
                    desktopResearchCardRect.anchoredPosition = Vector2.LerpUnclamped(
                        researchRest,
                        researchEdge,
                        edgeProgress);
                    var hudProgress = SmoothStep(Mathf.Clamp01(
                        (elapsed - (DesktopTransitionDuration - GameplayHudFadeDuration)) /
                        GameplayHudFadeDuration));
                    gameplayCanvasGroup.alpha = hudProgress;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            desktopSandboxCardRect.anchoredPosition = sandboxRest;
            desktopResearchCardRect.anchoredPosition = researchRest;
            desktopCanvasGroup.alpha = toDesktop ? 1f : 0f;
            gameplayCanvasGroup.alpha = toDesktop ? 0f : 1f;
            desktopCanvasGroup.interactable = toDesktop;
            desktopCanvasGroup.blocksRaycasts = toDesktop;
            desktopTransitionCoroutine = null;
            Refresh();
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void RefreshDesktopEntries()
        {
            if (desktopSandboxCard == null || desktopResearchCard == null)
            {
                return;
            }

            var menuEntriesVisible = !runtime.SettingsOpen &&
                                     (researchSetupOverlay == null || !researchSetupOverlay.activeSelf);
            var showSandbox = menuEntriesVisible;
            // The playable menu has one mode. The research recorder and setup
            // remain available to the study harness, not as a second game mode.
            desktopSandboxCard.SetActive(showSandbox);
            desktopResearchCard.SetActive(false);
            desktopBestRecordRect.gameObject.SetActive(menuEntriesVisible && runtime.BestSurvivalDays > 0);

            if (showSandbox)
                ConfigureResponsiveMenuElement(desktopSandboxCardRect, new Vector2(0.5f, 0.48f), 750f, 210f);
            ConfigureResponsiveMenuElement(desktopBestRecordRect, new Vector2(0.5f, 0.255f), 390f, 80f);
        }

        private void RefreshLayoutEditor()
        {
            if (editToolbar == null)
            {
                return;
            }

            var editing = layoutEditor != null && layoutEditor.IsEditing;
            enterEditButtonObject.SetActive(!editing);
            editToolbar.SetActive(editing);
            layoutImpactPanel.SetActive(editing);
            if (editing && !layoutImpactWasEditing)
            {
                layoutImpactPage = 0;
            }
            layoutImpactWasEditing = editing;
            if (!editing)
            {
                trayRoomIcon.gameObject.SetActive(false);
                return;
            }

            editStatus.text = layoutEditor.StatusText;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var routeDay = runtime.Clock.DayNumber + (runtime.Mode == GameMode.Sandbox ? 1 : 0);
            var routeEvent = AnimalRouteEventSchedule.ForDay(routeDay);
            layoutImpactScopeText.text = routeEvent is { } forecastRoute
                ? chinese
                    ? $"第 {routeDay} 天 · 米色人行、青色动物专用；{AnimalRouteEventSchedule.ShortLabel(forecastRoute, true)}橙色封闭。食物存量未预测。"
                    : $"Day {routeDay} · beige pedestrian, teal wildlife-only; {AnimalRouteEventSchedule.ShortLabel(forecastRoute, false)} blocked in amber. Food stock not forecast."
                : chinese
                    ? $"第 {routeDay} 天 · 米色人行、青色动物专用；两侧同色对齐才可通行。食物存量未预测。"
                    : $"Day {routeDay} · beige pedestrian, teal wildlife-only; matching exits must face. Food stock not forecast.";
            if (layoutEditor.TryGetImpactPreview(residentPopulation?.Model, out var impact, true))
            {
                var free = impact.ChangedRoomIds.Count == 0 ||
                           layoutEditor.FreeRearrangementAvailable &&
                           impact.ChangedRoomIds.Count <= RoomLayoutEditorController.MaximumRoomsPerFreeRearrangement;
                var metrics = LayoutImpactFeedback.BuildVisualMetrics(impact, chinese);
                string firstRisk = null;
                foreach (var metric in metrics)
                {
                    if (!metric.IsRisk) continue;
                    firstRisk = metric.Label;
                    break;
                }
                var hasMeasuredChange = DailySpatialDecision.HasMaterialImpact(impact);
                var blockingRisk = DailySpatialDecision.BlockingRisk(
                    impact, residentPopulation.Model.ResidentCount);
                var priority = firstRisk == null && impact.ChangedRoomIds.Count > 0 &&
                               !hasMeasuredChange
                    ? chinese ? "预测指标无变化 · 不计作今日行动"
                        : "No forecast effect · does not count today"
                    : impact.ChangedRoomIds.Count == 0 && blockingRisk != DailySpatialDecision.Risk.None
                    ? chinese ? $"维持布局不可用 · 先处理{SpatialRiskName(blockingRisk, true)}"
                        : $"Cannot keep layout · address {SpatialRiskName(blockingRisk, false)}"
                    : impact.ChangedRoomIds.Count == 0
                    ? chinese ? "明日关键指标稳定 · 可选择维持布局"
                        : "Tomorrow's core forecast is stable · keep layout"
                    : firstRisk == null
                    ? chinese ? "暂无已知预警 · 仍需观察" : "No known warning · observe outcomes"
                    : chinese ? $"风险：{firstRisk}" : $"Risk: {firstRisk}";
                layoutImpactText.text = chinese
                    ? $"{(layoutEditor.IsDragging ? "若放下" : "待确认")} · {(free ? "今日免费" : "不可确认")} · {priority}"
                    : $"{(layoutEditor.IsDragging ? "If dropped" : "Pending")} · {(free ? "Free today" : "Cannot confirm")} · {priority}";
                layoutImpactText.color = firstRisk == null
                    ? WarmPaper : new Color(0.95f, 0.70f, 0.52f, 1f);
                var pageCount = (metrics.Count + layoutImpactCards.Length - 1) / layoutImpactCards.Length;
                layoutImpactPage = Mathf.Clamp(layoutImpactPage, 0, Mathf.Max(0, pageCount - 1));
                for (var index = 0; index < layoutImpactCards.Length; index++)
                {
                    var metricIndex = layoutImpactPage * layoutImpactCards.Length + index;
                    RenderLayoutImpactCard(layoutImpactCards[index],
                        metricIndex < metrics.Count ? metrics[metricIndex] : (LayoutImpactMetric?)null,
                        chinese);
                }
                layoutImpactPageLabel.text = $"{layoutImpactPage + 1}/{pageCount}";
                layoutImpactPreviousButton.interactable = layoutImpactPage > 0;
                layoutImpactNextButton.interactable = layoutImpactPage < pageCount - 1;
            }
            else
            {
                layoutImpactText.text = layoutEditor.IsGuidedPractice
                    ? chinese ? "引导练习 · 放回原位后确认" : "Practice · return the room before confirming"
                    : chinese ? "拖动预览变化 · 今日只能选择调整或投喂" : "Drag to preview · choose rearrange or feed today";
                layoutImpactText.color = WarmPaper;
                foreach (var card in layoutImpactCards)
                {
                    card.Root.SetActive(false);
                }
                layoutImpactPageLabel.text = string.Empty;
                layoutImpactPreviousButton.interactable = false;
                layoutImpactNextButton.interactable = false;
            }
            var traySpec = layoutEditor.TrayRoomSpec;
            trayRoomIcon.sprite = traySpec == null ? null : RoomIconCatalog.GetSprite(traySpec.Type);
            trayRoomIcon.gameObject.SetActive(trayRoomIcon.sprite != null);
            rotateEditButton.interactable = layoutEditor.CanRotateSelected;
            confirmEditButton.interactable = layoutEditor.CanConfirm;
            confirmEditButton.GetComponentInChildren<Text>().text =
                layoutEditor.IsGuidedPractice || !layoutEditor.CanHoldLayout
                    ? "✓"
                    : layoutEditor.HasMaterialImpact
                        ? "✓"
                        : chinese ? "维持" : "Keep";
            confirmEditButton.GetComponent<Image>().color = layoutEditor.CanConfirm
                ? Cyan
                : new Color(0.25f, 0.27f, 0.28f, 0.72f);
        }

        private static string SpatialRiskName(DailySpatialDecision.Risk risk, bool chinese) => risk switch
        {
            DailySpatialDecision.Risk.Commute => chinese ? "通勤" : "commute",
            DailySpatialDecision.Risk.Waste => chinese ? "垃圾" : "waste",
            DailySpatialDecision.Risk.GreenNetwork => chinese ? "绿地连通" : "green links",
            DailySpatialDecision.Risk.PigeonMeals => chinese ? "鸽群食物" : "pigeon food",
            _ => chinese ? "风险" : "risk"
        };

        private void RefreshMarketForecast()
        {
            if (marketForecastPanel == null || runtime == null)
            {
                return;
            }
            var visible = runtime.Mode == GameMode.Sandbox && runtime.HasActiveRun &&
                          !runtime.AtDesktop && !runtime.LayoutEditing;
            marketForecastPanel.SetActive(visible);
            if (!visible)
            {
                return;
            }
            var market = NeighborhoodMarketSchedule.NextUnprocessed(runtime.Clock.TotalSeconds);
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var when = market.DayNumber == runtime.Clock.DayNumber
                ? chinese ? "今日" : "Today"
                : chinese ? $"第 {market.DayNumber} 天" : $"Day {market.DayNumber}";
            var eventName = market.IsMajorMarket
                ? chinese ? "社区集市" : "Community market"
                : chinese ? "每日轮值" : "Daily shop rotation";
            var nextDawn = runtime.Clock.DayNumber + 1;
            RefreshSpatialForecast(nextDawn);
            var tomorrowRoute = AnimalRouteEventSchedule.ForDay(nextDawn);
            var route = tomorrowRoute is { } forecastRoute
                ? AnimalRouteEventSchedule.ShortLabel(forecastRoute, chinese)
                : chinese ? "通道正常" : "open passages";
            var movedToday = layoutEditor != null &&
                             layoutEditor.LastConfirmedMovementDay == runtime.Clock.DayNumber;
            var status = movedToday
                ? chinese ? "已调整" : "adjusted"
                : chinese ? "待调整" : "adjust today";
            var rest = ParkEdgeRestSchedule.TryGetRestingCell(nextDawn,
                out var restColumn, out var restRow)
                ? chinese ? $" · 轮休 C{restColumn + 1}R{restRow + 1}"
                    : $" · rest C{restColumn + 1}R{restRow + 1}"
                : string.Empty;
            var seedMeals = spatialForecastSeedMealCeiling >= 0 &&
                            spatialForecastLivingPigeons >= 0
                ? chinese
                    ? $"种子餐上限 {spatialForecastSeedMealCeiling}/{spatialForecastLivingPigeons}"
                    : $"new-seed cap {spatialForecastSeedMealCeiling}/{spatialForecastLivingPigeons}"
                : chinese ? $"种子 {spatialForecastSeeds}" : $"seeds {spatialForecastSeeds}";
            marketForecastLabel.text = chinese
                ? $"明日 D{nextDawn} · {route} · {status}\n绿地 {spatialForecastGreenCells}/4 · {seedMeals}{rest}\n{eventName} {when} · {MarketRoomName(market.RoomId, true)} · 垃圾 +{market.ExtraWaste}"
                : $"D{nextDawn} · {route} · {status}\nGreen {spatialForecastGreenCells}/4 · {seedMeals}{rest}\n{eventName} {when} · {MarketRoomName(market.RoomId, false)} · waste +{market.ExtraWaste}";
        }

        private void RefreshSpatialForecast(int forecastDay)
        {
            if (layoutEditor == null ||
                spatialForecastDay == forecastDay &&
                spatialForecastMovementCount == layoutEditor.TotalRoomsMoved &&
                spatialForecastLivingPigeons ==
                    (animalPopulation?.LivingCount(WildlifeSpecies.Pigeon) ?? -1))
            {
                return;
            }
            var forecast = new RoomNavigationMap(layoutEditor.ExportLayout(),
                RoomLayoutData.All, 1f, true, forecastDay);
            spatialForecastGreenCells = GreenNetworkModel.ConnectedCount(forecast);
            spatialForecastSeeds = HabitatFoodNetworkModel.TotalSeedCapacity(forecast, forecastDay);
            spatialForecastSeedMealCeiling = layoutEditor.ForecastCurrentPigeonSeedMealCeiling(
                forecast, forecastDay);
            spatialForecastLivingPigeons = animalPopulation?.LivingCount(WildlifeSpecies.Pigeon) ?? -1;
            spatialForecastDay = forecastDay;
            spatialForecastMovementCount = layoutEditor.TotalRoomsMoved;
        }

        private void InvalidateSpatialForecast()
        {
            spatialForecastDay = -1;
            spatialForecastMovementCount = -1;
            spatialForecastSeedMealCeiling = -1;
            spatialForecastLivingPigeons = -1;
            RefreshPopulationDeathTooltips();
        }

        private static string MarketRoomName(string roomId, bool chinese)
        {
            return roomId switch
            {
                "canteen-a" => chinese ? "餐饮商铺 A" : "Food shop A",
                "canteen-b" => chinese ? "餐饮商铺 B" : "Food shop B",
                "supermarket" => chinese ? "小型超市" : "Supermarket",
                _ => roomId
            };
        }

        private void RefreshLanguage()
        {
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            if (enterEditLabel != null)
            {
                var movedToday = layoutEditor != null && runtime.HasActiveRun &&
                                 layoutEditor.LastConfirmedMovementDay == runtime.Clock.DayNumber;
                var fedToday = runtime.HasActiveRun &&
                               runtime.TodayAction == DailyActionKind.Feed;
                var heldToday = runtime.HasActiveRun &&
                                runtime.TodayAction == DailyActionKind.Hold;
                var reviewedToday = layoutEditor != null && runtime.HasActiveRun &&
                                    layoutEditor.LastConfirmedPlanningDay == runtime.Clock.DayNumber;
                enterEditLabel.text = movedToday
                    ? chinese ? "已调整" : "Adjusted"
                    : fedToday
                        ? chinese ? "已投喂" : "Fed today"
                    : heldToday
                        ? chinese ? "已维持" : "Kept today"
                    : reviewedToday
                        ? chinese ? "继续调整" : "Adjust"
                        : chinese ? "规划" : "Plan";
                if (enterEditBacking != null)
                    enterEditBacking.BorderColor = movedToday
                        ? Cyan : new Color(0.96f, 0.69f, 0.38f, 0.94f);
            }
            if (lastEcologicalLanguageChinese != chinese)
            {
                RefreshEcologicalMetrics();
            }
            pauseTitle.text = chinese ? "暂停" : "Paused";
            continueLabel.text = chinese ? "继续" : "Continue";
            settingsLabel.text = chinese ? "设置" : "Settings";
            languageLabel.text = chinese ? "语言 · 中文" : "Language · English";
            helpLabel.text = chinese ? "玩法与区域导览" : "Play & Area Guide";
            restartLabel.text = chinese ? "重新开始" : "Restart";
            desktopLabel.text = chinese ? "回到桌面" : "Return to Desktop";
            restartConfirmationTitle.text = chinese ? "重新开始？" : "Restart?";
            restartConfirmationMessage.text = chinese
                ? "当前无尽模式进度将被清除。\n从第 1 天和初始布局重新开始。"
                : "Your current Endless Mode progress will be cleared.\nRestart from day one with the initial layout.";
            restartConfirmLabel.text = chinese ? "确认重新开始" : "Restart";
            restartCancelLabel.text = chinese ? "取消" : "Cancel";
            ApplyRestartConfirmationTypography(chinese);
            settingsTitle.text = chinese ? "设置" : "Settings";
            volumeLabel.text = chinese ? "游戏音量" : "Master Volume";
            muteLabel.text = runtime.Muted
                ? (chinese ? "取消静音" : "Unmute")
                : (chinese ? "静音" : "Mute");
            muteIcon.sprite = PauseMenuVisualCatalog.GetSprite(
                runtime.Muted ? PauseMenuVisual.RestoreSoundIcon : PauseMenuVisual.MuteIcon);
            settingsLanguageLabel.text = chinese ? "语言 · 中文" : "Language · English";
            if (cameraCalibrationLabel != null)
            {
                cameraCalibrationLabel.text = chinese ? "重新校准摄像头" : "Recalibrate Camera";
            }
            backLabel.text = chinese ? "返回" : "Back";
            var continuing = runtime.HasResumableRun;
            var sandboxCaption = continuing && runtime.Mode == GameMode.Sandbox
                ? (chinese ? "继续无尽模式" : "Continue Endless")
                : (chinese ? "无尽模式" : "Endless Mode");
            var researchCaption = continuing && runtime.Mode == GameMode.Research
                ? (chinese ? "继续限时模式" : "Continue Timed")
                : (chinese ? "限时模式" : "Timed Mode");
            SetModeCardLabel(desktopSandboxLabel, sandboxCaption,
                continuing && runtime.Mode == GameMode.Sandbox ? (chinese ? 56 : 46) : (chinese ? 64 : 54));
            SetModeCardLabel(desktopResearchLabel, researchCaption,
                continuing && runtime.Mode == GameMode.Research ? (chinese ? 56 : 46) : (chinese ? 64 : 54));
            desktopSettingsLabel.text = chinese ? "设置" : "Settings";
            desktopLanguageLabel.text = chinese ? "语言" : "Language";
            desktopExitLabel.text = chinese ? "退出" : "Exit";
            desktopBestRecordLabel.text = runtime.BestSurvivalDays > 0
                ? chinese
                    ? $"最长共栖（所有设置）：{runtime.BestSurvivalDays} 天"
                    : $"Longest run (all settings): {runtime.BestSurvivalDays} days"
                : chinese ? "尚无记录" : "No completed record";
            if (sandboxDifficultyTitle != null)
            {
                sandboxDifficultyTitle.text = chinese ? "设置无尽模式规则" : "Set Endless Rules";
                sandboxDifficultyBackLabel.text = chinese ? "返回" : "Back";
                UpdateSandboxDifficultyDisplay();
            }
            if (researchSetupTitle != null)
            {
                researchSetupTitle.text = chinese ? "限时模式" : "Timed Mode";
                researchParticipantLabel.text = chinese ? "参与者" : "Participant";
                researchStartLabel.text = chinese ? "开始" : "Start";
                researchBackLabel.text = "‹";
            }
            if (hoveredRoom != null)
            {
                roomHoverText.text = UrbanPalette.LocalizedRoomName(hoveredRoom, chinese);
            }
            if (selectedRoomSpec != null)
            {
                RefreshSelectedRoomContext();
            }
        }

        private void RefreshCameraRecognitionFeedback()
        {
            if (cameraRecognitionFeedbackRoot == null)
            {
                return;
            }

            var state = runtime.CameraRecognitionState;
            var visible = state != CameraRecognitionFeedbackState.Hidden &&
                          !runtime.CameraCalibrationOpen &&
                          !runtime.AtDesktop &&
                          !runtime.ResultsOpen;
            cameraRecognitionFeedbackRoot.SetActive(visible);

            var cyanVisible = visible &&
                              (state == CameraRecognitionFeedbackState.Scanning ||
                               state == CameraRecognitionFeedbackState.Stabilising);
            cameraRecognitionScanFrame.gameObject.SetActive(cyanVisible);
            cameraRecognitionConfirmationFrame.gameObject.SetActive(
                visible && state == CameraRecognitionFeedbackState.Confirmed);
            cameraInvalidPlacementRoot.gameObject.SetActive(
                visible && state == CameraRecognitionFeedbackState.InvalidPlacement);

            if (state == CameraRecognitionFeedbackState.Stabilising)
            {
                cameraRecognitionScanFrame.fillAmount = runtime.CameraRecognitionProgress;
            }
            else if (state == CameraRecognitionFeedbackState.Scanning)
            {
                cameraRecognitionScanFrame.fillAmount = 0.08f;
            }

            if (state == CameraRecognitionFeedbackState.Confirmed &&
                previousCameraRecognitionState != CameraRecognitionFeedbackState.Confirmed)
            {
                cameraRecognitionConfirmationUntil = Time.unscaledTime + 0.55f;
                cameraRecognitionConfirmationFrame.color = Color.white;
            }

            previousCameraRecognitionState = state;
        }

        private Text BuildMenuButton(
            Transform parent,
            string name,
            string label,
            float top,
            UnityEngine.Events.UnityAction action,
            float width = 322f)
        {
            var button = CreateButton(parent, name, label, 21, action);
            SetTopCenter(button.GetComponent<RectTransform>(), 0f, top, width, 50f);
            return button.GetComponentInChildren<Text>();
        }

        private Text BuildPauseAction(
            Transform parent,
            string name,
            PauseMenuVisual iconVisual,
            float top,
            UnityEngine.Events.UnityAction action)
        {
            var panel = CreatePanel(name, parent, Color.white);
            panel.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            panel.Image.preserveAspect = false;

            var button = panel.GameObject.AddComponent<Button>();
            button.targetGraphic = panel.Image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.88f, 1f);
            colors.pressedColor = new Color(0.78f, 0.83f, 0.75f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
            SetTopCenter(panel.RectTransform, 0f, top, 350f, 82f);

            var icon = CreateImage(
                panel.Transform,
                "Action Pictogram",
                PauseMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 20f, 15f, 52f, 52f);

            var label = CreateText(
                panel.Transform,
                "Live Action Label",
                string.Empty,
                22,
                TextAnchor.MiddleLeft,
                WarmPaper,
                FontStyle.Bold);
            SetRect(label.rectTransform, 90f, 11f, 235f, 60f);
            return label;
        }

        private Text BuildSettingsAction(
            Transform parent,
            string name,
            PauseMenuVisual iconVisual,
            float top,
            UnityEngine.Events.UnityAction action,
            out Image icon)
        {
            var panel = CreatePanel(name, parent, Color.white);
            panel.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            panel.Image.preserveAspect = false;

            var button = panel.GameObject.AddComponent<Button>();
            button.targetGraphic = panel.Image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.88f, 1f);
            colors.pressedColor = new Color(0.78f, 0.83f, 0.75f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
            SetTopCenter(panel.RectTransform, 0f, top, 472f, 76f);

            icon = CreateImage(
                panel.Transform,
                "Action Pictogram",
                PauseMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 24f, 16f, 44f, 44f);

            var label = CreateText(
                panel.Transform,
                "Live Action Label",
                string.Empty,
                23,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            SetRect(label.rectTransform, 80f, 11f, 312f, 54f);
            return label;
        }

        private Button CreateButton(
            Transform parent,
            string name,
            string label,
            int fontSize,
            UnityEngine.Events.UnityAction action)
        {
            var panel = CreatePanel(name, parent, Graphite);
            var button = panel.GameObject.AddComponent<Button>();
            button.targetGraphic = panel.Image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 1f, 0.98f);
            colors.pressedColor = new Color(0.65f, 0.88f, 0.84f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var text = CreateText(panel.Transform, "Label", label, fontSize, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            Stretch(text.rectTransform, 5f);
            return button;
        }

        private Button CreateRoundedRestartButton(
            Transform parent,
            string name,
            Color fill,
            UnityEngine.Events.UnityAction action)
        {
            var instance = NewUiObject(name, parent,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic));
            var graphic = instance.GetComponent<RoundedPanelGraphic>();
            graphic.color = fill;
            graphic.CornerRadius = 17f;
            graphic.BorderWidth = 1.5f;
            graphic.BorderColor = new Color(0.96f, 0.90f, 0.77f, 0.76f);
            var button = instance.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.onClick.AddListener(action);
            var label = CreateText(instance.transform, "Label", string.Empty,
                19, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            Stretch(label.rectTransform, 8f);
            return button;
        }

        private void ApplyRestartConfirmationTypography(bool chinese)
        {
            restartChineseFont ??= Resources.Load<Font>("Fonts/NotoSansSC-Regular") ?? font;
            var boldFont = chinese ? restartChineseFont : UrbanFontResolver.GetFont(FontStyle.Bold);
            var bodyFont = chinese ? restartChineseFont : UrbanFontResolver.GetFont();
            restartConfirmationTitle.font = boldFont;
            restartConfirmationTitle.fontStyle = chinese ? FontStyle.Bold : FontStyle.Normal;
            restartCancelLabel.font = boldFont;
            restartConfirmLabel.font = boldFont;
            restartCancelLabel.fontStyle = FontStyle.Normal;
            restartConfirmLabel.fontStyle = FontStyle.Normal;
            restartConfirmationMessage.font = bodyFont;
            restartConfirmationMessage.fontStyle = FontStyle.Normal;
        }

        private static void StyleCompactPaperButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private Slider CreateSlider(Transform parent, string name)
        {
            var sliderObject = NewUiObject(name, parent, typeof(Slider));
            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            var background = CreatePanel("Track", sliderObject.transform, Color.white);
            background.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.VolumeSliderTrack);
            background.Image.preserveAspect = false;
            Stretch(background.RectTransform);
            var fillArea = CreateEmpty("Fill Area", sliderObject.transform);
            Stretch(fillArea, 8f, 0f);
            var fill = CreatePanel("Fill", fillArea, Color.white);
            fill.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.VolumeSliderFill);
            fill.Image.preserveAspect = false;
            Stretch(fill.RectTransform);
            var handleArea = CreateEmpty("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea, 8f, 0f);
            var handle = CreatePanel("Handle", handleArea, Color.white);
            handle.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.VolumeSliderHandle);
            handle.Image.preserveAspect = true;
            handle.RectTransform.anchorMin = handle.RectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.RectTransform.sizeDelta = new Vector2(28f, 38f);

            slider.fillRect = fill.RectTransform;
            slider.handleRect = handle.RectTransform;
            slider.targetGraphic = handle.Image;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private void BuildEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.hideFlags = generatedHideFlags;
            eventSystem.transform.SetParent(transform.parent, false);
        }

        private PanelElements CreatePanel(string name, Transform parent, Color color)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Image));
            var image = instance.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return new PanelElements(instance, instance.GetComponent<RectTransform>(), image);
        }

        // Keep the existing RectTransform (and all child positions) unchanged;
        // only replace a plain rectangular fill with a rounded one.
        private void RoundSolidPanel(PanelElements panel, float radius)
        {
            var fill = panel.Image.color;
            panel.Image.color = Color.clear;
            var backing = NewUiObject("Rounded Backing", panel.Transform,
                typeof(CanvasRenderer), typeof(RoundedPanelGraphic))
                .GetComponent<RoundedPanelGraphic>();
            backing.color = fill;
            backing.CornerRadius = radius;
            backing.BorderWidth = 0f;
            backing.raycastTarget = false;
            Stretch(backing.rectTransform);
            backing.transform.SetAsFirstSibling();
        }

        private RectTransform CreateEmpty(string name, Transform parent)
        {
            return NewUiObject(name, parent).GetComponent<RectTransform>();
        }

        private GameObject NewUiObject(string name, Transform parent, params System.Type[] components)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            instance.hideFlags = generatedHideFlags;
            instance.transform.SetParent(parent, false);
            foreach (var component in components)
            {
                instance.AddComponent(component);
            }

            return instance;
        }

        private Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle style)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Text));
            var text = instance.GetComponent<Text>();
            text.font = UrbanFontResolver.GetFont(style);
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style == FontStyle.Bold && text.font != font ? FontStyle.Normal : style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Image));
            var image = instance.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float inset = 0f, float verticalInset = -1f)
        {
            if (verticalInset < 0f)
            {
                verticalInset = inset;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, verticalInset);
            rect.offsetMax = new Vector2(-inset, -verticalInset);
        }

        private static void SetCenter(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopCenter(RectTransform rect, float x, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRight(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRect(RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private readonly struct FoodBadgeTarget
        {
            public readonly TextMesh Label;
            public readonly NaturalFoodKind? Kind;
            public readonly int? AddedToday;
            public readonly bool PlayerPlaced;

            public FoodBadgeTarget(TextMesh label, NaturalFoodKind? kind,
                int? addedToday, bool playerPlaced)
            {
                Label = label;
                Kind = kind;
                AddedToday = addedToday;
                PlayerPlaced = playerPlaced;
            }
        }

        private sealed class FoodLocationEntry
        {
            public readonly string RoomId;
            public int Seeds;
            public int Nuts;
            public int Insects;
            public int Scraps;
            public int Placed;
            public int Total => Seeds + Nuts + Insects + Scraps + Placed;

            public FoodLocationEntry(string roomId) => RoomId = roomId;

            public void Add(NaturalFoodKind kind, int portions)
            {
                switch (kind)
                {
                    case NaturalFoodKind.Seed: Seeds += portions; break;
                    case NaturalFoodKind.Nut: Nuts += portions; break;
                    case NaturalFoodKind.Insect: Insects += portions; break;
                    case NaturalFoodKind.DiscardedFood: Scraps += portions; break;
                }
            }

            public string Detail(bool chinese)
            {
                var parts = new List<string>(5);
                if (Seeds > 0) parts.Add(chinese ? $"种子 {Seeds}" : $"Seed {Seeds}");
                if (Nuts > 0) parts.Add(chinese ? $"坚果 {Nuts}" : $"Nut {Nuts}");
                if (Insects > 0) parts.Add(chinese ? $"昆虫 {Insects}" : $"Insect {Insects}");
                if (Scraps > 0) parts.Add(chinese ? $"残食 {Scraps}" : $"Scrap {Scraps}");
                if (Placed > 0) parts.Add(chinese ? $"投喂 {Placed}" : $"Placed {Placed}");
                return string.Join(" · ", parts);
            }
        }

        private sealed class LayoutImpactCard
        {
            public LayoutImpactCard(GameObject root, Image signal, LayoutImpactPictogramGraphic icon, Text label,
                Text baseline, Text delta, Text explanation)
            {
                Root = root;
                Signal = signal;
                Icon = icon;
                Label = label;
                Baseline = baseline;
                Delta = delta;
                Explanation = explanation;
            }

            public GameObject Root { get; }
            public Image Signal { get; }
            public LayoutImpactPictogramGraphic Icon { get; }
            public Text Label { get; }
            public Text Baseline { get; }
            public Text Delta { get; }
            public Text Explanation { get; }
        }

        private readonly struct Indicator
        {
            public Indicator(EcologicalMetricKind kind, string name, float value, Color color)
            {
                Kind = kind;
                Name = name;
                Value = value;
                Color = color;
            }

            public EcologicalMetricKind Kind { get; }
            public string Name { get; }
            public float Value { get; }
            public Color Color { get; }
        }

        private readonly struct PanelElements
        {
            public PanelElements(GameObject gameObject, RectTransform rectTransform, Image image)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                Image = image;
            }

            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }
            public Image Image { get; }
            public Transform Transform => GameObject.transform;
        }
    }
}
