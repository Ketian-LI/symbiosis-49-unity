using System.Collections;
using System.Collections.Generic;
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

        private readonly Dictionary<int, Image> speedButtons = new();
        private readonly Dictionary<WildlifeSpecies, Text> populationCounts = new();
        private readonly Dictionary<EcologicalMetricKind, CircularMeterGraphic> ecologicalMeters = new();
        private readonly Dictionary<EcologicalMetricKind, Text> ecologicalTooltips = new();
        private readonly Dictionary<EcologicalMetricKind, Color> ecologicalColors = new();

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
        private Coroutine desktopTransitionCoroutine;
        private bool desktopStateKnown;
        private bool previousDesktopState;
        private GameObject enterEditButtonObject;
        private GameObject editToolbar;
        private Image trayRoomIcon;
        private Text editStatus;
        private Button rotateEditButton;
        private Button confirmEditButton;
        private WasteCollectionNotification wasteCollectionNotification;
        private ResourcePointCounter resourcePointCounter;
        private ResidentStatusOverlay residentStatusOverlay;
        private ResidentPopulationController residentPopulation;
        private Text residentCountText;
        private OakTreeLifecycleController oakTreeLifecycle;
        private AnimalPopulationController animalPopulation;
        private EcologicalMetricsController ecologicalMetrics;
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
        private Image feedingModeButtonBackground;
        private Image feedingModeIcon;
        private Text feedingModeLabel;
        private GameObject feedingModeHint;
        private Text feedingModeHintLabel;

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            gameplayRoot = CreateEmpty("Minimal Gameplay HUD", transform);
            Stretch(gameplayRoot);
            gameplayCanvasGroup = gameplayRoot.gameObject.AddComponent<CanvasGroup>();
            BuildClock();
            BuildIndicators();
            BuildPopulation();
            BuildRoomHoverLabel();
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
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.StateChanged -= Refresh;
            }
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged -= RefreshLayoutEditor;
            }
            if (oakTreeLifecycle != null)
            {
                oakTreeLifecycle.StateChanged -= RefreshSelectedRoomContext;
            }
            if (animalPopulation != null)
            {
                animalPopulation.StateChanged -= RefreshAnimalPopulation;
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
            }
        }

        public void BindLayoutEditor(RoomLayoutEditorController editor)
        {
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged -= RefreshLayoutEditor;
            }

            layoutEditor = editor;
            if (layoutEditor != null)
            {
                layoutEditor.StateChanged += RefreshLayoutEditor;
            }

            RefreshLayoutEditor();
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
            if (controller == null || gameplayRoot == null)
            {
                return;
            }

            if (resourcePointCounter == null)
            {
                resourcePointCounter = gameObject.AddComponent<ResourcePointCounter>();
            }

            resourcePointCounter.Build(
                gameplayRoot,
                font,
                generatedHideFlags,
                runtime,
                controller);
        }

        public void BindPlayerFeeding(PlayerFeedingController controller)
        {
            if (playerFeeding != null)
            {
                playerFeeding.FeedingModeChanged -= RefreshFeedingControls;
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
            UpdateCameraRecognitionFeedbackAnimation();
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
                    ? "栽种 −6"
                    : "Plant −6";
            }
            LayoutRoomContextChips(showFunction, showUse);
        }

        private void BuildClock()
        {
            var root = CreateEmpty("Day Night Clock", gameplayRoot);
            SetTopCenter(root, 0f, 18f, 118f, 118f);

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
            researchRingRect.sizeDelta = new Vector2(134f, 134f);
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
            SetTopCenter(researchDurationTooltip.rectTransform, 0f, 118f, 190f, 34f);
            researchDurationTooltip.gameObject.SetActive(false);
            var trigger = researchRingObject.GetComponent<EventTrigger>();
            trigger.triggers = new List<EventTrigger.Entry>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => researchDurationTooltip.gameObject.SetActive(true));
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => researchDurationTooltip.gameObject.SetActive(false));
            trigger.triggers.Add(exit);

            var dialObject = NewUiObject("Four Phase Dial", root, typeof(CanvasRenderer), typeof(DayNightDialGraphic));
            var dialRect = dialObject.GetComponent<RectTransform>();
            Stretch(dialRect);
            clockDial = dialObject.GetComponent<DayNightDialGraphic>();
            clockDial.raycastTarget = false;

            dayNumber = CreateText(root, "Day Number", "1", 29, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            Stretch(dayNumber.rectTransform);

            var speeds = CreateEmpty("Simulation Speed", gameplayRoot);
            SetTopCenter(speeds, 0f, 142f, 222f, 38f);
            BuildSpeedButton(speeds, 0, "Ⅱ", -84f);
            BuildSpeedButton(speeds, 1, "▶", -28f);
            BuildSpeedButton(speeds, 2, "2×", 28f);
            BuildSpeedButton(speeds, 4, "4×", 84f);
        }

        private void BuildSpeedButton(Transform parent, int speed, string label, float x)
        {
            var button = CreateButton(parent, $"Speed {speed}", label, 17, () => runtime.SetSpeed(speed));
            var backing = button.GetComponent<Image>();
            backing.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            backing.preserveAspect = false;
            backing.color = Color.white;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(48f, 34f);
            speedButtons[speed] = button.GetComponent<Image>();
        }

        private void BuildIndicators()
        {
            var root = CreateEmpty("Global Ecological Indicators", gameplayRoot);
            root.anchorMin = root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-24f, 8f);
            root.sizeDelta = new Vector2(84f, 360f);

            var indicators = new[]
            {
                new Indicator(EcologicalMetricKind.HumanFunction, "Human Function", 1f, new Color(0.52f, 0.45f, 0.71f)),
                new Indicator(EcologicalMetricKind.FoodAccessibility, "Food Accessibility", 1f, new Color(0.86f, 0.61f, 0.24f)),
                new Indicator(EcologicalMetricKind.HabitatProvision, "Habitat Provision", 1f, new Color(0.35f, 0.66f, 0.47f)),
                new Indicator(EcologicalMetricKind.AnimalSafety, "Animal Safety", 1f, Cyan)
            };

            for (var index = 0; index < indicators.Length; index++)
            {
                BuildIndicator(root, indicators[index], index);
            }
        }

        private void BuildIndicator(Transform parent, Indicator indicator, int index)
        {
            var holder = CreateEmpty(indicator.Name, parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -index * 86f);
            holder.sizeDelta = new Vector2(76f, 76f);

            var basePanel = CreatePanel("Graphite Base", holder, Graphite);
            Stretch(basePanel.RectTransform, 7f);
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
            pictogram.rectTransform.anchoredPosition = indicator.Kind == EcologicalMetricKind.HumanFunction
                ? new Vector2(0f, 5f)
                : Vector2.zero;
            pictogram.rectTransform.sizeDelta = indicator.Kind == EcologicalMetricKind.HumanFunction
                ? new Vector2(36f, 36f)
                : new Vector2(42f, 42f);

            if (indicator.Kind == EcologicalMetricKind.HumanFunction)
            {
                var badge = CreatePanel("Resident Count Badge", holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
                badge.RectTransform.anchorMin = badge.RectTransform.anchorMax = new Vector2(0.5f, 0f);
                badge.RectTransform.pivot = new Vector2(0.5f, 0f);
                badge.RectTransform.anchoredPosition = new Vector2(0f, -1f);
                badge.RectTransform.sizeDelta = new Vector2(42f, 21f);
                badge.Image.raycastTarget = false;
                residentCountText = CreateText(
                    badge.Transform,
                    "Resident Count",
                    "4/8",
                    13,
                    TextAnchor.MiddleCenter,
                    WarmPaper,
                    FontStyle.Bold);
                Stretch(residentCountText.rectTransform, 2f);
            }

            var tooltip = CreatePanel("Hover Detail", holder, new Color(0.10f, 0.12f, 0.14f, 0.96f));
            tooltip.RectTransform.anchorMin = tooltip.RectTransform.anchorMax = new Vector2(0f, 0.5f);
            tooltip.RectTransform.pivot = new Vector2(1f, 0.5f);
            tooltip.RectTransform.anchoredPosition = new Vector2(-12f, 0f);
            tooltip.RectTransform.sizeDelta = new Vector2(270f, 62f);
            var tooltipText = CreateText(tooltip.Transform, "Exact Value And Reason", string.Empty, 16, TextAnchor.MiddleLeft, WarmPaper, FontStyle.Normal);
            Stretch(tooltipText.rectTransform, 12f);
            ecologicalTooltips[indicator.Kind] = tooltipText;
            holder.gameObject.AddComponent<EcologicalIndicatorHover>().Initialize(tooltip.GameObject);
        }

        private void BuildPopulation()
        {
            var root = CreateEmpty("Animal Population", gameplayRoot);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = new Vector2(22f, 20f);
            root.sizeDelta = new Vector2(334f, 68f);

            BuildPopulationChip(root, 0, WildlifeSpecies.Pigeon, 12, new Color(0.43f, 0.51f, 0.62f));
            BuildPopulationChip(root, 1, WildlifeSpecies.Squirrel, 4, new Color(0.35f, 0.56f, 0.31f));
            BuildPopulationChip(root, 2, WildlifeSpecies.Hedgehog, 2, new Color(0.78f, 0.55f, 0.25f));
            BuildPopulationChip(root, 3, WildlifeSpecies.Fox, 2, new Color(0.76f, 0.34f, 0.18f));
        }

        private void BuildPopulationChip(Transform parent, int index, WildlifeSpecies species, int count, Color accent)
        {
            var holder = CreateEmpty($"Population {species}", parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 0.5f);
            holder.pivot = new Vector2(0f, 0.5f);
            holder.anchoredPosition = new Vector2(index * 82f, 0f);
            holder.sizeDelta = new Vector2(74f, 62f);
            var shadow = CreatePanel("Portrait Shadow", holder, new Color(0.08f, 0.09f, 0.10f, 0.72f));
            shadow.RectTransform.anchorMin = shadow.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            shadow.RectTransform.anchoredPosition = new Vector2(0f, 1f);
            shadow.RectTransform.sizeDelta = new Vector2(62f, 62f);
            shadow.Image.raycastTarget = false;

            var portrait = CreateImage(holder, "Animal Portrait", GameplayHudVisualCatalog.GetPopulationSprite(species));
            portrait.preserveAspect = true;
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            portrait.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            portrait.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            portrait.rectTransform.sizeDelta = new Vector2(62f, 62f);

            var badge = CreatePanel("Living Count Badge", holder, accent);
            badge.RectTransform.anchorMin = badge.RectTransform.anchorMax = new Vector2(0.5f, 0f);
            badge.RectTransform.pivot = new Vector2(0.5f, 0f);
            badge.RectTransform.anchoredPosition = new Vector2(0f, -1f);
            badge.RectTransform.sizeDelta = new Vector2(40f, 22f);
            badge.Image.raycastTarget = false;
            var amount = CreateText(badge.Transform, "Living Count", count.ToString(), 15, TextAnchor.MiddleCenter, WarmPaper, FontStyle.Bold);
            Stretch(amount.rectTransform, 2f);
            populationCounts[species] = amount;
        }

        private void RefreshResidentPopulation()
        {
            if (residentCountText == null)
            {
                return;
            }

            var count = residentPopulation?.Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents;
            residentCountText.text = $"{count}/{ResidentPopulationModel.MaximumResidents}";
        }

        private void RefreshAnimalPopulation()
        {
            if (animalPopulation == null)
            {
                return;
            }
            foreach (var pair in populationCounts)
            {
                pair.Value.text = animalPopulation.LivingCount(pair.Key).ToString();
            }
        }

        private void RefreshEcologicalMetrics()
        {
            if (ecologicalMetrics == null)
            {
                return;
            }
            var chinese = runtime == null || runtime.Language == InterfaceLanguage.Chinese;
            foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
            {
                var value = ecologicalMetrics.Snapshot.ValueOf(kind);
                var color = value < 0.30f
                    ? new Color(0.62f, 0.16f, 0.12f)
                    : value < 0.60f
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
                if (ecologicalTooltips.TryGetValue(kind, out var tooltip))
                {
                    var population = kind == EcologicalMetricKind.HumanFunction
                        ? $" · {(chinese ? "居民" : "Residents")} " +
                          $"{residentPopulation?.Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents}/" +
                          ResidentPopulationModel.MaximumResidents
                        : string.Empty;
                    tooltip.text = $"{MetricName(kind, chinese)}  {Mathf.RoundToInt(value * 100f)}%{population}\n{MetricReason(kind, chinese)}";
                }
            }
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

        private static string MetricName(EcologicalMetricKind kind, bool chinese)
        {
            return kind switch
            {
                EcologicalMetricKind.HumanFunction => chinese ? "人类功能" : "Human function",
                EcologicalMetricKind.FoodAccessibility => chinese ? "食物可达" : "Food accessibility",
                EcologicalMetricKind.HabitatProvision => chinese ? "栖息供给" : "Habitat provision",
                _ => chinese ? "动物安全" : "Animal safety"
            };
        }

        private static string MetricReason(EcologicalMetricKind kind, bool chinese)
        {
            return kind switch
            {
                EcologicalMetricKind.HumanFunction => chinese ? "通勤与垃圾负担" : "Commute and waste load",
                EcologicalMetricKind.FoodAccessibility => chinese ? "现有食物 / 存活动物" : "Available food / living animals",
                EcologicalMetricKind.HabitatProvision => chinese ? "固定栖息地与成熟橡树" : "Fixed habitats and mature oaks",
                _ => chinese ? "饥饿、死亡与交通风险" : "Hunger, deaths and traffic risk"
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
            var plantHitArea = roomFunctionChip.AddComponent<Image>();
            plantHitArea.color = Color.clear;
            plantHitArea.raycastTarget = true;
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
                "栽种 −6",
                14,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            oakPlantLabel.rectTransform.anchorMin = oakPlantLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            oakPlantLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            oakPlantLabel.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            oakPlantLabel.rectTransform.sizeDelta = new Vector2(96f, 24f);
            oakPlantLabel.gameObject.SetActive(false);
            LayoutRoomContextChips(true, true);
            roomContext.SetActive(false);
        }

        private GameObject BuildRoomContextChip(Transform parent, string name, out Image pictogram)
        {
            var holder = CreateEmpty($"{name} Chip", parent);
            holder.anchorMin = holder.anchorMax = Vector2.zero;
            holder.pivot = Vector2.zero;
            holder.sizeDelta = new Vector2(72f, 72f);

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
            var enterButton = CreateButton(gameplayRoot, "Enter Layout Editing", string.Empty, 1, () => layoutEditor?.EnterEditing());
            enterEditButtonObject = enterButton.gameObject;
            var enterBacking = enterButton.GetComponent<Image>();
            enterBacking.sprite = MainMenuVisualCatalog.GetSprite(MainMenuVisual.BottomButtonBase);
            enterBacking.preserveAspect = true;
            enterBacking.color = Color.white;
            var enterIcon = CreateImage(
                enterButton.transform,
                "Layout Editing Pictogram",
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.SandboxIcon));
            enterIcon.preserveAspect = true;
            Stretch(enterIcon.rectTransform, 11f);
            SetTopLeft(enterButton.GetComponent<RectTransform>(), 22f, 102f, 58f, 58f);

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
            editToolbar.SetActive(false);
        }

        private void BuildFeedingControls()
        {
            feedingModeButton = CreateButton(
                gameplayRoot,
                "Activate Feeding Mode",
                string.Empty,
                17,
                () => playerFeeding?.ToggleFeedingMode());
            feedingModeButtonObject = feedingModeButton.gameObject;
            feedingModeButtonBackground = feedingModeButton.GetComponent<Image>();
            feedingModeButtonBackground.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            feedingModeButtonBackground.preserveAspect = false;
            SetTopLeft(feedingModeButton.GetComponent<RectTransform>(), 84f, 102f, 154f, 52f);

            feedingModeLabel = feedingModeButton.GetComponentInChildren<Text>();
            feedingModeLabel.alignment = TextAnchor.MiddleCenter;
            SetRect(feedingModeLabel.rectTransform, 48f, 2f, 100f, 48f);

            feedingModeIcon = CreateImage(
                feedingModeButton.transform,
                "Food Dish Pictogram",
                RoomContextVisualCatalog.GetSprite(RoomContextVisual.FoodAvailable));
            feedingModeIcon.preserveAspect = true;
            feedingModeIcon.raycastTarget = false;
            SetRect(feedingModeIcon.rectTransform, 8f, 8f, 36f, 36f);

            var hint = CreatePanel(
                "Feeding Mode Instruction",
                gameplayRoot,
                Color.white);
            hint.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.ButtonBase);
            hint.Image.preserveAspect = false;
            feedingModeHint = hint.GameObject;
            SetTopLeft(hint.RectTransform, 248f, 102f, 410f, 52f);
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
            feedingModeButtonObject.SetActive(visible);
            feedingModeButton.interactable = active || available;
            feedingModeButtonBackground.color = active
                ? Cyan
                : available
                    ? Color.white
                    : MutedTrack;
            feedingModeIcon.color = active ? Graphite : Color.white;
            feedingModeLabel.color = active ? Graphite : WarmPaper;

            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            feedingModeLabel.text = active
                ? (chinese ? "取消" : "Cancel")
                : (chinese ? "投喂 −1" : "Feed −1");
            feedingModeHintLabel.text = chinese
                ? "投喂模式：点击房间空地投放 · 右键取消"
                : "Feeding mode: click open ground · right-click to cancel";
            feedingModeHint.SetActive(visible && active);
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
                32,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Bold);
            SetTopCenter(restartConfirmationTitle.rectTransform, 0f, 42f, 430f, 50f);
            restartConfirmationMessage = CreateText(
                restartConfirmation.Transform,
                "Restart Confirmation Message",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                Graphite,
                FontStyle.Normal);
            SetTopCenter(restartConfirmationMessage.rectTransform, 0f, 108f, 440f, 78f);
            var cancelRestartButton = CreateButton(
                restartConfirmation.Transform,
                "Cancel Restart",
                string.Empty,
                19,
                CancelSandboxRestart);
            SetTopCenter(cancelRestartButton.GetComponent<RectTransform>(), -108f, 230f, 190f, 62f);
            restartCancelLabel = cancelRestartButton.GetComponentInChildren<Text>();
            var confirmRestartButton = CreateButton(
                restartConfirmation.Transform,
                "Confirm Restart",
                string.Empty,
                19,
                ConfirmSandboxRestart);
            SetTopCenter(confirmRestartButton.GetComponent<RectTransform>(), 108f, 230f, 190f, 62f);
            confirmRestartButton.GetComponent<Image>().color = new Color(0.72f, 0.30f, 0.22f, 1f);
            restartConfirmLabel = confirmRestartButton.GetComponentInChildren<Text>();
            restartConfirmationPanel.SetActive(false);

            var settings = CreatePanel("Settings Paper Panel", overlay.Transform, Color.white);
            settings.Image.sprite = PauseMenuVisualCatalog.GetSprite(PauseMenuVisual.SettingsPanel);
            settings.Image.preserveAspect = false;
            settingsPanel = settings.GameObject;
            var cameraSettings = BuildVariantSettings.UsesCameraRecognition;
            SetCenter(settings.RectTransform, 620f, cameraSettings ? 480f : 420f);
            settingsTitle = CreateText(settings.Transform, "Settings Title", "设置", 32, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            SetTopCenter(settingsTitle.rectTransform, 0f, 30f, 400f, 48f);
            volumeLabel = CreateText(settings.Transform, "Volume Label", "游戏音量", 20, TextAnchor.MiddleLeft, Graphite, FontStyle.Bold);
            SetTopLeft(volumeLabel.rectTransform, 74f, 110f, 220f, 35f);
            volumeSlider = CreateSlider(settings.Transform, "Master Volume");
            SetTopLeft(volumeSlider.GetComponent<RectTransform>(), 74f, 154f, 472f, 34f);
            volumeSlider.onValueChanged.AddListener(runtime.SetMasterVolume);
            muteLabel = BuildSettingsAction(
                settings.Transform,
                "Mute",
                PauseMenuVisual.MuteIcon,
                218f,
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
                    338f,
                    runtime.RequestCameraRecalibration,
                    out _);
            }
            backLabel = BuildSettingsAction(
                settings.Transform,
                "Back",
                PauseMenuVisual.BackIcon,
                cameraSettings ? 398f : 338f,
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
            var overlay = CreatePanel("Main Menu", transform, new Color(0.08f, 0.09f, 0.09f, 0.56f));
            desktopOverlay = overlay.GameObject;
            desktopCanvasGroup = overlay.GameObject.AddComponent<CanvasGroup>();
            Stretch(overlay.RectTransform);
            BuildDesktopTitle(overlay.Transform);

            var sandboxButton = BuildModeCard(
                overlay.Transform,
                "Sandbox Mode Entrance",
                MainMenuVisual.SandboxCard,
                MainMenuVisual.SandboxIcon,
                HandleSandboxEntry,
                out desktopSandboxLabel);
            desktopSandboxCard = sandboxButton.gameObject;
            desktopSandboxCardRect = sandboxButton.GetComponent<RectTransform>();

            var researchButton = BuildModeCard(
                overlay.Transform,
                "Research Mode Entrance",
                MainMenuVisual.ResearchCard,
                MainMenuVisual.ResearchIcon,
                HandleResearchEntry,
                out desktopResearchLabel);
            desktopResearchCard = researchButton.gameObject;
            desktopResearchCardRect = researchButton.GetComponent<RectTransform>();

            var recordPanel = CreatePanel("Best Survival Record", overlay.Transform, Color.clear);
            recordPanel.Image.raycastTarget = false;
            desktopBestRecordRect = recordPanel.RectTransform;
            SetTopCenter(desktopBestRecordRect, 0f, 770f, 390f, 80f);
            var recordArtwork = CreateImage(
                recordPanel.Transform,
                "Best Record Panel Artwork",
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.BestRecordPanel));
            recordArtwork.preserveAspect = false;
            SetCenter(recordArtwork.rectTransform, 420f, 420f);
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
                desktopResearchCard.SetActive(visible && !BuildVariantSettings.UsesCameraRecognition);
            }
            if (desktopBestRecordRect != null)
            {
                desktopBestRecordRect.gameObject.SetActive(visible);
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
            SetTopCenter(title, 0f, 55f, 1000f, 188f);

            var wordmark = CreateImage(
                title,
                "SYMBIOSIS 49 Wordmark",
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.TitleWordmark));
            wordmark.preserveAspect = true;
            Stretch(wordmark.rectTransform);
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
            SetTopCenter(root, x, 850f, 140f, 132f);

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
                18,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            SetTopCenter(label.rectTransform, 0f, 98f, 140f, 30f);
            return label;
        }

        private Button BuildModeCard(
            Transform parent,
            string name,
            MainMenuVisual cardVisual,
            MainMenuVisual iconVisual,
            UnityEngine.Events.UnityAction action,
            out Text label)
        {
            var panel = CreatePanel(name, parent, Color.white);
            var normalSprite = MainMenuVisualCatalog.GetSprite(cardVisual);
            panel.Image.sprite = normalSprite;
            panel.Image.preserveAspect = false;
            var button = panel.GameObject.AddComponent<Button>();
            button.targetGraphic = panel.Image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);

            var feedback = panel.GameObject.AddComponent<MainMenuModeCardFeedback>();
            feedback.Initialize(
                panel.Image,
                normalSprite,
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.ModeHoverCard));

            var icon = CreateImage(panel.Transform, "Mode Pictogram", MainMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 36f, 20f, 130f, 130f);

            label = CreateText(
                panel.Transform,
                "Live Mode Label",
                string.Empty,
                36,
                TextAnchor.MiddleCenter,
                WarmPaper,
                FontStyle.Bold);
            SetRect(label.rectTransform, 175f, 35f, 430f, 100f);

            var arrow = CreateImage(
                panel.Transform,
                "Enter Arrow",
                MainMenuVisualCatalog.GetSprite(MainMenuVisual.EnterArrow));
            arrow.preserveAspect = true;
            SetRect(arrow.rectTransform, 638f, 52f, 66f, 66f);
            return button;
        }

        private void HandleSandboxEntry()
        {
            if (runtime.HasResumableRun && runtime.Mode == GameMode.Sandbox)
            {
                runtime.ResumeFromDesktop();
                return;
            }

            runtime.StartNewRun(GameMode.Sandbox);
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
            foreach (var pair in speedButtons)
            {
                var active = pair.Key == runtime.SpeedMultiplier && !runtime.PauseMenuOpen &&
                             !runtime.AtDesktop && !runtime.LayoutEditing;
                pair.Value.color = active ? new Color(0.62f, 0.96f, 0.94f, 1f) : Color.white;
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

            var cameraBuild = BuildVariantSettings.UsesCameraRecognition;
            var showSandbox = true;
            var showResearch = !cameraBuild;
            desktopSandboxCard.SetActive(showSandbox);
            desktopResearchCard.SetActive(showResearch);

            if (showSandbox && showResearch)
            {
                SetTopCenter(desktopSandboxCardRect, 0f, 320f, 720f, 190f);
                SetTopCenter(desktopResearchCardRect, 0f, 525f, 720f, 190f);
                SetTopCenter(desktopBestRecordRect, 0f, 745f, 390f, 80f);
            }
            else
            {
                if (showSandbox)
                {
                    SetTopCenter(desktopSandboxCardRect, 0f, 410f, 720f, 190f);
                }
                if (showResearch)
                {
                    SetTopCenter(desktopResearchCardRect, 0f, 410f, 720f, 190f);
                }
                SetTopCenter(desktopBestRecordRect, 0f, 635f, 390f, 80f);
            }
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
            if (!editing)
            {
                trayRoomIcon.gameObject.SetActive(false);
                return;
            }

            editStatus.text = layoutEditor.StatusText;
            var traySpec = layoutEditor.TrayRoomSpec;
            trayRoomIcon.sprite = traySpec == null ? null : RoomIconCatalog.GetSprite(traySpec.Type);
            trayRoomIcon.gameObject.SetActive(trayRoomIcon.sprite != null);
            rotateEditButton.interactable = layoutEditor.CanRotateSelected;
            confirmEditButton.interactable = layoutEditor.CanConfirm;
            confirmEditButton.GetComponent<Image>().color = layoutEditor.CanConfirm
                ? Cyan
                : new Color(0.25f, 0.27f, 0.28f, 0.72f);
        }

        private void RefreshLanguage()
        {
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            pauseTitle.text = chinese ? "暂停" : "Paused";
            continueLabel.text = chinese ? "继续" : "Continue";
            settingsLabel.text = chinese ? "设置" : "Settings";
            languageLabel.text = chinese ? "语言 · 中文" : "Language · English";
            helpLabel.text = chinese ? "重新查看引导" : "Replay Tutorial";
            restartLabel.text = chinese ? "重新开始" : "Restart";
            desktopLabel.text = chinese ? "回到桌面" : "Return to Desktop";
            restartConfirmationTitle.text = chinese ? "重新开始？" : "Restart?";
            restartConfirmationMessage.text = chinese
                ? "当前无尽模式进度将被清除，\n并从第1天、初始布局与初始资源重新开始。"
                : "Your current Endless Mode progress will be cleared.\nRestart from day one with the initial layout and resources.";
            restartConfirmLabel.text = chinese ? "确认重新开始" : "Restart";
            restartCancelLabel.text = chinese ? "取消" : "Cancel";
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
            desktopSandboxLabel.text = continuing && runtime.Mode == GameMode.Sandbox
                ? (chinese ? "继续无尽模式" : "Continue Endless")
                : (chinese ? "无尽模式" : "Endless Mode");
            desktopResearchLabel.text = continuing && runtime.Mode == GameMode.Research
                ? (chinese ? "继续限时模式" : "Continue Timed")
                : (chinese ? "限时模式" : "Timed Mode");
            desktopSettingsLabel.text = chinese ? "设置" : "Settings";
            desktopLanguageLabel.text = chinese ? "语言" : "Language";
            desktopExitLabel.text = chinese ? "退出" : "Exit";
            desktopBestRecordLabel.text = runtime.BestSurvivalDays > 0
                ? chinese
                    ? $"最长共栖：{runtime.BestSurvivalDays} 天"
                    : $"Longest coexistence: {runtime.BestSurvivalDays} days"
                : chinese ? "尚无记录" : "No completed record";
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
            SetTopCenter(panel.RectTransform, 0f, top, 472f, 50f);

            icon = CreateImage(
                panel.Transform,
                "Action Pictogram",
                PauseMenuVisualCatalog.GetSprite(iconVisual));
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, 24f, 4f, 42f, 42f);

            var label = CreateText(
                panel.Transform,
                "Live Action Label",
                string.Empty,
                20,
                TextAnchor.MiddleLeft,
                WarmPaper,
                FontStyle.Bold);
            SetRect(label.rectTransform, 82f, 2f, 360f, 46f);
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
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
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

        private static void SetRect(RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
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
