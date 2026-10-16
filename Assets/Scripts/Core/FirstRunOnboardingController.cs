using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Core
{
    public sealed class FirstRunOnboardingController : MonoBehaviour
    {
        private const string CompletedKey = "symbiosis49.onboardingCompleted";

        private readonly List<RoomView> rooms = new();
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layout;
        private PlayerFeedingController feeding;
        private ResidentPopulationController residents;
        private WasteManagementController waste;
        private CitizenPopulationPresenter citizenPopulation;
        private UrbanWildlifeHud hud;
        private FirstRunOnboardingOverlay overlay;
        private RoomView wasteTarget;
        private RoomView practiceTarget;
        private RoomView foodTarget;
        private bool sawTrayOccupied;
        private bool wasteInspected;
        private bool replaying;
        private bool layoutRetryOffered;
        private int guidePageIndex = -1;
        private LineRenderer routeLine;
        private Material routeMaterial;
        private OakStoryOpeningWorld oakOpeningWorld;
        private Transform openingWorldRoot;
        private RoomView oakOpeningRoom;
        private SquirrelDemoAgent oakOpeningSquirrel;
        private RoomView openingHomeRoom;
        private RoomView openingWorkRoom;
        private RoomView openingMealRoom;
        private CitizenDemoAgent openingResident;
        private OakTreeLifecycleController oakTrees;
        private BoardCameraController boardCamera;
        private bool squirrelMet;
        private bool oakRoomInspected;
        private bool oakFunctionsRead;
        private bool oakDateDialRead;
        private bool oakNextDayRead;
        // -1: oak/date lesson; 0-7: home, resident, work and meal,
        // alternating between an interactive prompt and its explanation.
        private int humanLessonStage = -1;
        private bool oakOpeningPending;

        public FirstRunOnboardingModel Model { get; private set; }
        public bool IsOakOpeningActive => oakOpeningWorld?.IsActive == true;
        public bool OakOpeningPending => oakOpeningPending;

        public void RestoreOakOpeningPending(bool pending)
        {
            oakOpeningPending = pending;
        }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            PlayerFeedingController feedingController,
            ResidentPopulationController residentController,
            WasteManagementController wasteController,
            CitizenPopulationPresenter citizenPresenter,
            IEnumerable<RoomView> roomViews,
            Transform generatedWorld,
            SquirrelDemoAgent openingSquirrel,
            OakTreeLifecycleController oakTreeController,
            BoardCameraController cameraController,
            UrbanWildlifeHud gameHud,
            Camera worldCamera,
            Font font,
            HideFlags hideFlags)
        {
            runtime = runtimeController;
            layout = layoutController;
            feeding = feedingController;
            residents = residentController;
            waste = wasteController;
            citizenPopulation = citizenPresenter;
            hud = gameHud;
            rooms.AddRange(roomViews ?? Array.Empty<RoomView>());
            oakOpeningRoom = rooms.FirstOrDefault(item => item.Spec.Id == "oak-a");
            oakOpeningSquirrel = openingSquirrel;
            openingWorldRoot = generatedWorld;
            oakTrees = oakTreeController;
            boardCamera = cameraController;
            wasteTarget = rooms.FirstOrDefault(item => item.Spec.Type == RoomType.Trash);
            practiceTarget = rooms.FirstOrDefault(item => item.Spec.Movable && item.Spec.CellCount == 1);
            foodTarget = rooms.FirstOrDefault(item => item.Spec.Type == RoomType.CentralPark);
            Model = new FirstRunOnboardingModel();

            overlay = hud.gameObject.AddComponent<FirstRunOnboardingOverlay>();
            overlay.Build(font, worldCamera, hideFlags);
            overlay.SkipRequested += Skip;
            overlay.HighlightedResidentRequested += HandleHighlightedResidentRequested;
            overlay.HighlightedSquirrelRequested += HandleHighlightedSquirrelRequested;
            overlay.HighlightedRoomRequested += HandleHighlightedRoomRequested;
            overlay.ContinueRequested += HandleContinueRequested;
            overlay.PreviousRequested += HandlePreviousRequested;
            runtime.RestartRequested += HandleRestartRequested;
            if (citizenPopulation != null)
            {
                citizenPopulation.ResidentClicked += HandleCitizenClicked;
            }
            if (oakOpeningSquirrel != null)
                oakOpeningSquirrel.Clicked += HandleSquirrelClicked;
            foreach (var room in rooms)
            {
                room.Clicked += HandleRoomClicked;
            }
            layout.TrayStateChanged += HandleTrayStateChanged;
            layout.LayoutConfirmed += HandleLayoutConfirmed;
            feeding.FoodPlaced += HandleFoodPlaced;
            feeding.FeedingModeChanged += HandleFeedingModeChanged;
            feeding.InvalidPlacementAttempted += HandleInvalidPlacement;
            runtime.StateChanged += HandleRuntimeStateChanged;
        }

        private void OnDestroy()
        {
            if (overlay != null)
            {
                overlay.SkipRequested -= Skip;
                overlay.HighlightedResidentRequested -= HandleHighlightedResidentRequested;
                overlay.HighlightedSquirrelRequested -= HandleHighlightedSquirrelRequested;
                overlay.HighlightedRoomRequested -= HandleHighlightedRoomRequested;
                overlay.ContinueRequested -= HandleContinueRequested;
                overlay.PreviousRequested -= HandlePreviousRequested;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
                runtime.StateChanged -= HandleRuntimeStateChanged;
            }
            if (oakOpeningSquirrel != null)
                oakOpeningSquirrel.Clicked -= HandleSquirrelClicked;
            if (citizenPopulation != null)
            {
                citizenPopulation.ResidentClicked -= HandleCitizenClicked;
            }
            foreach (var room in rooms)
            {
                if (room != null)
                {
                    room.Clicked -= HandleRoomClicked;
                }
            }
            if (layout != null)
            {
                layout.TrayStateChanged -= HandleTrayStateChanged;
                layout.LayoutConfirmed -= HandleLayoutConfirmed;
            }
            if (feeding != null)
            {
                feeding.FoodPlaced -= HandleFoodPlaced;
                feeding.FeedingModeChanged -= HandleFeedingModeChanged;
                feeding.InvalidPlacementAttempted -= HandleInvalidPlacement;
            }
            DestroyRouteLine();
            oakOpeningWorld?.Abandon();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && Model?.Step == OnboardingStep.PracticeLayout &&
                !layout.IsEditing && !layoutRetryOffered && !runtime.AtDesktop)
            {
                layoutRetryOffered = true;
                ShowCurrentStep();
            }
        }

        public void Replay()
        {
            Begin(true);
        }

        public void SetOverlaySuppressed(bool suppressed)
        {
            overlay?.SetSuppressed(suppressed);
        }

#if UNITY_EDITOR
        public void ShowVisualPreview(OnboardingStep previewStep)
        {
            LeaveOakOpening();
            if (runtime.AtDesktop)
            {
                runtime.StartNewRun(GameMode.Sandbox);
            }
            if (layout.IsEditing)
            {
                layout.CancelEditing();
            }
            layout.SetGuidedTargetRoom(null);
            runtime.SetOnboardingOpen(true);
            if (previewStep == OnboardingStep.PracticeLayout)
            {
                layout.SetGuidedTargetRoom(practiceTarget?.Spec.Id);
                layout.EnterEditing();
            }

            var previewTarget = previewStep switch
            {
                OnboardingStep.SelectResident => citizenPopulation?.PrimaryAgent?.transform,
                OnboardingStep.InspectWaste => wasteTarget?.VisualRoot,
                OnboardingStep.PracticeLayout => practiceTarget?.VisualRoot,
                _ => null
            };
            overlay.Show(previewStep, previewTarget, IsChinese());
        }
#endif

        public void Skip()
        {
            if (!Model.IsActive)
            {
                return;
            }
            if (layout.IsEditing)
            {
                layout.SetGuidedTargetRoom(null);
                layout.CancelEditing();
            }
            feeding.CancelFeedingMode();
            Model.Skip();
            CompleteAndDismiss();
        }

        private void HandleRuntimeStateChanged()
        {
            if (!IsOakOpeningActive)
            {
                if (oakOpeningPending && runtime.HasActiveRun && !runtime.AtDesktop &&
                    !runtime.ResultsOpen && !runtime.OnboardingOpen)
                    Begin(false);
                return;
            }
            if (!runtime.AtDesktop && !runtime.ResultsOpen && runtime.OnboardingOpen) return;
            if (!runtime.AtDesktop && !runtime.OnboardingOpen)
                oakOpeningPending = false;
            LeaveOakOpening();
            Model.Hide();
            overlay.Show(OnboardingStep.Hidden, null, IsChinese());
        }

        private void HandleRestartRequested()
        {
            // A new run always starts at the new story opening. The old
            // completion preference must not silently skip this new design.
            Begin(false);
        }

        private void Begin(bool isReplay)
        {
            LeaveOakOpening();
            if (layout.IsEditing)
            {
                layout.SetGuidedTargetRoom(null);
                layout.CancelEditing();
            }
            feeding.CancelFeedingMode();
            replaying = isReplay;
            Model.BeginOakOpening();
            guidePageIndex = -1;
            squirrelMet = false;
            oakRoomInspected = false;
            oakFunctionsRead = false;
            oakDateDialRead = false;
            oakNextDayRead = false;
            humanLessonStage = -1;
            oakOpeningPending = true;
            sawTrayOccupied = false;
            wasteInspected = false;
            layoutRetryOffered = false;
            DestroyRouteLine();
            layout.SetGuidedTargetRoom(null);
            SelectHumanStoryRooms();
            oakOpeningWorld = new OakStoryOpeningWorld(openingWorldRoot, oakOpeningRoom,
                oakOpeningSquirrel, openingHomeRoom, openingWorkRoom, openingMealRoom,
                openingResident);
            if (!oakOpeningWorld.Enter())
            {
                Debug.LogError("[Story opening] Oak, squirrel, resident or human rooms unavailable. Opening was not shown.", this);
                Model.Hide();
                oakOpeningPending = false;
                return;
            }
            hud.SetStoryOpeningActive(true);
            runtime.SetOnboardingOpen(true);
            // Enter the story with the only visible room already centred;
            // do not expose a moving/off-centre camera during the first prompt.
            boardCamera?.FocusRoomImmediate(oakOpeningRoom.VisualRoot, 4.3f);
            ShowCurrentStep();
        }

        private void LeaveOakOpening()
        {
            if (!IsOakOpeningActive) return;
            oakOpeningWorld.Exit();
            oakOpeningRoom?.SetSelected(false);
            openingHomeRoom?.SetSelected(false);
            openingWorkRoom?.SetSelected(false);
            openingMealRoom?.SetSelected(false);
            hud?.ClearRoomSelection();
            hud?.SetStoryOpeningActive(false);
            boardCamera?.ReturnToOverviewIfNeeded();
        }

        private void HandleSquirrelClicked(SquirrelDemoAgent selected)
        {
            if (Model.Step != OnboardingStep.OakOpening || selected != oakOpeningSquirrel || squirrelMet)
                return;
            squirrelMet = true;
            ShowCurrentStep();
        }

        private void HandleHighlightedSquirrelRequested()
        {
            HandleSquirrelClicked(oakOpeningSquirrel);
        }

        private void HandleCitizenClicked(string residentId, CitizenDemoAgent selected)
        {
            if (Model.Step == OnboardingStep.OakOpening && humanLessonStage == 2 &&
                selected == openingResident)
            {
                humanLessonStage = 3;
                ShowCurrentStep();
                return;
            }
            if (guidePageIndex >= 0 || Model.Step != OnboardingStep.SelectResident || selected == null)
            {
                return;
            }
            DrawResidentRoute(residentId);
            Model.CompleteStep(OnboardingStep.SelectResident);
            ShowCurrentStep();
        }

        private void HandleHighlightedResidentRequested()
        {
            if (Model.Step == OnboardingStep.OakOpening && humanLessonStage == 2)
            {
                if (openingResident != null && citizenPopulation.TrySelectAgent(openingResident))
                    return;
                // The stage is paused; use the same narrative selection if the
                // full-map route selector is temporarily unavailable.
                HandleCitizenClicked(string.Empty, openingResident);
                return;
            }
            if (guidePageIndex >= 0 || Model.Step != OnboardingStep.SelectResident)
            {
                return;
            }
            var highlighted = citizenPopulation?.PrimaryAgent;
            if (highlighted == null || !citizenPopulation.TrySelectAgent(highlighted))
            {
                overlay.ShowFeedback(IsChinese()
                    ? "居民暂时不可选，请稍候再试。"
                    : "The resident is unavailable. Please try again shortly.");
            }
        }

        private void HandleRoomClicked(RoomView room)
        {
            if (Model.Step == OnboardingStep.OakOpening)
            {
                if (!squirrelMet || !oakRoomInspected)
                {
                    if (!squirrelMet || room != oakOpeningRoom) return;
                    oakRoomInspected = true;
                }
                else if (humanLessonStage == 0 && room == openingHomeRoom) humanLessonStage = 1;
                else if (humanLessonStage == 4 && room == openingWorkRoom) humanLessonStage = 5;
                else if (humanLessonStage == 6 && room == openingMealRoom) humanLessonStage = 7;
                else return;
                ShowCurrentStep();
                return;
            }
            if (guidePageIndex >= 0 || Model.Step != OnboardingStep.InspectWaste)
            {
                return;
            }
            if (room != wasteTarget)
            {
                overlay.ShowFeedback(IsChinese() ? "这是另一间房。请点击高亮的垃圾房。" :
                    "That is another room. Select the highlighted waste room.");
                return;
            }
            wasteInspected = true;
            ShowCurrentStep();
        }

        private void HandleHighlightedRoomRequested()
        {
            HandleRoomClicked(humanLessonStage switch
            {
                0 => openingHomeRoom,
                4 => openingWorkRoom,
                6 => openingMealRoom,
                _ => oakOpeningRoom
            });
        }

        private void HandleContinueRequested()
        {
            if (Model.Step == OnboardingStep.OakOpening)
            {
                if (!oakRoomInspected) return;
                if (!oakFunctionsRead)
                {
                    oakFunctionsRead = true;
                    ShowCurrentStep();
                    return;
                }
                if (!oakDateDialRead)
                {
                    oakDateDialRead = true;
                    ShowCurrentStep();
                    return;
                }
                if (!oakNextDayRead)
                {
                    oakNextDayRead = true;
                    ShowCurrentStep();
                    return;
                }
                if (humanLessonStage < 0)
                {
                    humanLessonStage = 0;
                    ShowCurrentStep();
                    return;
                }
                if (humanLessonStage is 1 or 3 or 5)
                {
                    humanLessonStage++;
                    ShowCurrentStep();
                    return;
                }
                if (humanLessonStage != 7) return;
                Model.CompleteStep(OnboardingStep.OakOpening);
                CompleteAndDismiss();
                return;
            }
            if (guidePageIndex >= 0)
            {
                guidePageIndex++;
                if (guidePageIndex >= GameplayGuideCatalog.PageCount)
                {
                    guidePageIndex = -1;
                }
                ShowCurrentStep();
                return;
            }
            if (Model.Step == OnboardingStep.InspectWaste && wasteInspected)
            {
                Model.CompleteStep(OnboardingStep.InspectWaste);
                OpenGuidedLayout();
                return;
            }
            if (Model.Step == OnboardingStep.PlaceFood && replaying)
            {
                Model.CompleteStep(OnboardingStep.PlaceFood);
                CompleteAndDismiss();
            }
            else if (Model.Step == OnboardingStep.PracticeLayout &&
                     layoutRetryOffered && !layout.IsEditing)
            {
                sawTrayOccupied = false;
                OpenGuidedLayout();
            }
        }

        private void HandlePreviousRequested()
        {
            if (guidePageIndex > 0)
            {
                guidePageIndex--;
                ShowCurrentStep();
            }
        }

        private void OpenGuidedLayout()
        {
            layoutRetryOffered = false;
            DestroyRouteLine();
            layout.SetGuidedTargetRoom(practiceTarget?.Spec.Id);
            layout.EnterEditing();
            ShowCurrentStep();
        }

        private void HandleTrayStateChanged(bool occupied)
        {
            if (Model.Step != OnboardingStep.PracticeLayout)
            {
                return;
            }
            if (occupied && layout.TrayRoomSpec?.Id == practiceTarget?.Spec.Id)
            {
                sawTrayOccupied = true;
            }
            ShowCurrentStep();
        }

        private void HandleLayoutConfirmed()
        {
            if (Model.Step != OnboardingStep.PracticeLayout || !sawTrayOccupied || layout.TrayOccupied)
            {
                return;
            }
            Model.CompleteStep(OnboardingStep.PracticeLayout);
            ShowCurrentStep();
        }

        private void HandleFeedingModeChanged()
        {
            if (Model.Step != OnboardingStep.PlaceFood)
            {
                return;
            }
            if (replaying && feeding.FeedingModeActive)
            {
                feeding.CancelFeedingMode();
                overlay.ShowFeedback(IsChinese() ? "这是教程回顾。点“完成”即可返回游戏。" :
                    "This is a tutorial replay. Press Finish to return to the game.");
                return;
            }
            if (!replaying)
            {
                ShowCurrentStep();
            }
        }

        private void HandleInvalidPlacement(string roomId)
        {
            if (Model.Step == OnboardingStep.PlaceFood)
            {
                overlay.ShowFeedback(IsChinese()
                    ? "此处无法投喂。请点房间里没有陈设的空地，或右键取消。"
                    : "You cannot feed there. Choose open ground in a room, or right-click to cancel.");
            }
        }

        private void HandleFoodPlaced(PlayerFoodSourceState source)
        {
            if (Model.Step != OnboardingStep.PlaceFood || source == null)
            {
                return;
            }
            Model.CompleteStep(OnboardingStep.PlaceFood);
            CompleteAndDismiss();
        }

        private void CompleteAndDismiss()
        {
            oakOpeningPending = false;
            LeaveOakOpening();
            guidePageIndex = -1;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            layout.SetGuidedTargetRoom(null);
            runtime.SetOnboardingOpen(false);
            overlay.Show(OnboardingStep.Complete, null, IsChinese());
            DestroyRouteLine();
        }

        private void ShowCurrentStep()
        {
            if (Model.Step == OnboardingStep.OakOpening)
            {
                var phase = humanLessonStage >= 0 ? humanLessonStage switch
                    {
                        0 => OnboardingPromptPhase.HumanHome,
                        1 => OnboardingPromptPhase.HumanHomeIdentified,
                        2 => OnboardingPromptPhase.HumanResident,
                        3 => OnboardingPromptPhase.HumanResidentMet,
                        4 => OnboardingPromptPhase.HumanWork,
                        5 => OnboardingPromptPhase.HumanWorkIdentified,
                        6 => OnboardingPromptPhase.HumanMeal,
                        _ => OnboardingPromptPhase.HumanMealIdentified
                    }
                    : !squirrelMet ? OnboardingPromptPhase.Default
                    : !oakRoomInspected ? OnboardingPromptPhase.OakMet
                    : !oakFunctionsRead ? OnboardingPromptPhase.OakRoomIdentified
                    : !oakDateDialRead ? OnboardingPromptPhase.OakFunctions
                    : !oakNextDayRead ? OnboardingPromptPhase.OakDate
                    : OnboardingPromptPhase.OakNextDay;
                var dateLesson = phase is OnboardingPromptPhase.OakDate or OnboardingPromptPhase.OakNextDay;
                hud.SetStoryOpeningClockVisible(dateLesson);
                if (humanLessonStage >= 0)
                {
                    oakOpeningWorld.RevealHome();
                    if (humanLessonStage >= 2) oakOpeningWorld.RevealResident();
                    if (humanLessonStage >= 4) oakOpeningWorld.RevealWork();
                    if (humanLessonStage >= 6) oakOpeningWorld.RevealMeal();
                    boardCamera?.FocusRoomImmediate(oakOpeningWorld.StageCenter, 4.3f);
                }
                var oakTarget = phase == OnboardingPromptPhase.Default
                    ? oakOpeningSquirrel?.transform
                    : dateLesson ? null
                    : phase is OnboardingPromptPhase.HumanHome or OnboardingPromptPhase.HumanHomeIdentified
                        ? openingHomeRoom?.VisualRoot
                    : phase is OnboardingPromptPhase.HumanResident or OnboardingPromptPhase.HumanResidentMet
                        ? openingResident?.transform
                    : phase is OnboardingPromptPhase.HumanWork or OnboardingPromptPhase.HumanWorkIdentified
                        ? openingWorkRoom?.VisualRoot
                    : phase is OnboardingPromptPhase.HumanMeal or OnboardingPromptPhase.HumanMealIdentified
                        ? openingMealRoom?.VisualRoot
                    : oakOpeningRoom?.VisualRoot;
                var chinese = IsChinese();
                var dateMessage = phase switch
                {
                    OnboardingPromptPhase.OakDate => chinese
                        ? $"圆盘中央是第 {runtime.Clock.DayNumber} 天；外圈依次经过晨、昼、昏、夜。教程中时间暂停。"
                        : $"The dial shows day {runtime.Clock.DayNumber}. Its ring moves through dawn, day, dusk and night. Time is paused here.",
                    OnboardingPromptPhase.OakNextDay when runtime.Mode == GameMode.Research => chinese
                        ? "跨天时橡树生长与自然食物照常结算。研究模式按固定时长进行，不要求每日选择。"
                        : "Oak growth and natural food still settle across days. Research Mode has a fixed timer, not a daily-action gate.",
                    _ => null
                };
                overlay.Show(Model.Step, oakTarget, chinese, phase,
                    dateLesson ? hud.ClockDialRect : null, dateMessage);
                if (phase is OnboardingPromptPhase.HumanHomeIdentified or
                    OnboardingPromptPhase.HumanWorkIdentified or
                    OnboardingPromptPhase.HumanMealIdentified)
                {
                    var storyRoom = phase == OnboardingPromptPhase.HumanHomeIdentified
                        ? openingHomeRoom : phase == OnboardingPromptPhase.HumanWorkIdentified
                        ? openingWorkRoom : openingMealRoom;
                    overlay.ShowHumanRoomFact(storyRoom?.Spec, phase, chinese);
                }
                if (!dateLesson && oakRoomInspected && oakOpeningRoom?.Spec != null)
                {
                    var stage = oakTrees?.Model?.StageOf(oakOpeningRoom.Spec.Id) ?? OakTreeStage.Felled;
                    overlay.ShowOakRoomFact(oakOpeningRoom.Spec, stage, chinese, phase);
                }
                return;
            }
            if (guidePageIndex >= 0)
            {
                var page = GameplayGuideCatalog.GetPage(guidePageIndex, IsChinese(), runtime.Mode);
                var area = page.RoomType.HasValue
                    ? rooms.FirstOrDefault(room => room.Spec.Type == page.RoomType.Value)
                    : null;
                var indicator = page.MetricKind.HasValue
                    ? hud.GetEcologicalIndicatorRect(page.MetricKind.Value)
                    : page.DeathLimit ? hud.AnimalDeathIndicatorRect
                    : guidePageIndex == 1 ? hud.ClockDialRect
                    : guidePageIndex == 2 ? hud.WorkforceCounterRect
                    : guidePageIndex == 8 ? hud.LayoutEditButtonRect
                    : null;
                overlay.ShowGuide(guidePageIndex, GameplayGuideCatalog.PageCount,
                    page, area?.VisualRoot, IsChinese(), indicator);
                return;
            }
            if (Model.Step == OnboardingStep.InspectWaste && wasteInspected)
            {
                var units = 0;
                if (waste?.Model?.WasteRooms != null && wasteTarget != null &&
                    waste.Model.WasteRooms.TryGetValue(wasteTarget.Spec.Id, out var load))
                {
                    units = load.Units;
                }
                var nextDay = WasteCollectionSchedule.NextMunicipalCollectionDayNumber(runtime.Clock.TotalSeconds);
                var message = IsChinese()
                    ? $"垃圾房：{units}/{WasteRoomLoadModel.Capacity}；下次清运在第 {nextDay} 天 04:00。看完点“继续”。"
                    : $"Waste room: {units}/{WasteRoomLoadModel.Capacity}. Next collection: day {nextDay}, 04:00. Press Next.";
                overlay.Show(Model.Step, wasteTarget?.VisualRoot, IsChinese(),
                    OnboardingPromptPhase.WasteReviewed, null, message);
                return;
            }
            if (Model.Step == OnboardingStep.PracticeLayout)
            {
                if (layoutRetryOffered && !layout.IsEditing)
                {
                    overlay.Show(Model.Step, practiceTarget?.VisualRoot, IsChinese(),
                        OnboardingPromptPhase.LayoutRetry);
                    return;
                }
                var phase = layout.TrayOccupied
                    ? OnboardingPromptPhase.RoomInTray
                    : layout.GuidedPracticeReadyToConfirm
                        ? OnboardingPromptPhase.ConfirmLayout
                        : sawTrayOccupied
                            ? OnboardingPromptPhase.RestoreRoom
                            : OnboardingPromptPhase.Default;
                var worldTarget = phase == OnboardingPromptPhase.RoomInTray
                    ? layout.TrayTransform
                    : phase == OnboardingPromptPhase.ConfirmLayout
                        ? null
                        : practiceTarget?.VisualRoot;
                var buttonTarget = phase == OnboardingPromptPhase.ConfirmLayout
                    ? hud.LayoutConfirmButtonRect
                    : null;
                overlay.Show(Model.Step, worldTarget, IsChinese(), phase, buttonTarget);
                return;
            }
            if (Model.Step == OnboardingStep.PlaceFood)
            {
                if (replaying)
                {
                    overlay.Show(Model.Step, null, IsChinese(), OnboardingPromptPhase.ReplayFinish,
                        hud.FeedingModeButtonRect);
                }
                else if (feeding.FeedingModeActive)
                {
                    overlay.Show(Model.Step, foodTarget?.VisualRoot, IsChinese(),
                        OnboardingPromptPhase.FeedingActive);
                }
                else
                {
                    overlay.Show(Model.Step, null, IsChinese(), OnboardingPromptPhase.Default,
                        hud.FeedingModeButtonRect);
                }
                return;
            }
            var target = Model.Step switch
            {
                OnboardingStep.SelectResident => citizenPopulation?.PrimaryAgent?.transform,
                OnboardingStep.InspectWaste => wasteTarget?.VisualRoot,
                OnboardingStep.PracticeLayout => practiceTarget?.VisualRoot,
                _ => null
            };
            overlay.Show(Model.Step, target, IsChinese());
        }

        private void DrawResidentRoute(string residentId)
        {
            if (citizenPopulation == null ||
                !citizenPopulation.TryBuildActivityRoute(residentId, out var routePoints))
            {
                return;
            }
            var routeObject = new GameObject("Onboarding Resident Route");
            routeObject.transform.SetParent(transform, false);
            routeLine = routeObject.AddComponent<LineRenderer>();
            routeMaterial = new Material(Shader.Find("Sprites/Default"));
            routeLine.sharedMaterial = routeMaterial;
            routeLine.startColor = routeLine.endColor = new Color(0.27f, 0.90f, 0.84f, 0.96f);
            routeLine.startWidth = routeLine.endWidth = 0.08f;
            routeLine.positionCount = routePoints.Count;
            routeLine.useWorldSpace = true;
            for (var index = 0; index < routePoints.Count; index++)
            {
                routeLine.SetPosition(index, routePoints[index] + Vector3.up * 0.06f);
            }
        }

        private void DestroyRouteLine()
        {
            if (routeLine != null)
            {
                Destroy(routeLine.gameObject);
                routeLine = null;
            }
            if (routeMaterial != null)
            {
                Destroy(routeMaterial);
                routeMaterial = null;
            }
        }

        private bool IsChinese()
        {
            return runtime.Language == InterfaceLanguage.Chinese;
        }

        private void SelectHumanStoryRooms()
        {
            var first = residents?.Model?.Residents?.FirstOrDefault();
            openingResident = citizenPopulation?.PrimaryAgent;
            openingHomeRoom = rooms.FirstOrDefault(item => item.Spec.Id == first?.residenceId);
            string workId = null, mealId = null;
            if (first != null)
                residents.Model.TryGetRoute(first.id, out _, out workId, out mealId);
            openingWorkRoom = rooms.FirstOrDefault(item => item.Spec.Id == workId) ??
                              rooms.FirstOrDefault(item => item.Spec.Type == RoomType.Office);
            openingMealRoom = rooms.FirstOrDefault(item => item.Spec.Id == mealId) ??
                              rooms.FirstOrDefault(item => item.Spec.Type == RoomType.Canteen);
        }
    }
}
