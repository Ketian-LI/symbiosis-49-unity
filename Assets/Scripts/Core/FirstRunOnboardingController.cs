using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        public FirstRunOnboardingModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            PlayerFeedingController feedingController,
            ResidentPopulationController residentController,
            WasteManagementController wasteController,
            CitizenPopulationPresenter citizenPresenter,
            IEnumerable<RoomView> roomViews,
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
            wasteTarget = rooms.FirstOrDefault(item => item.Spec.Type == RoomType.Trash);
            practiceTarget = rooms.FirstOrDefault(item => item.Spec.Movable && item.Spec.CellCount == 1);
            foodTarget = rooms.FirstOrDefault(item => item.Spec.Type == RoomType.CentralPark);
            Model = new FirstRunOnboardingModel();

            overlay = hud.gameObject.AddComponent<FirstRunOnboardingOverlay>();
            overlay.Build(font, worldCamera, hideFlags);
            overlay.SkipRequested += Skip;
            overlay.HighlightedResidentRequested += HandleHighlightedResidentRequested;
            overlay.ContinueRequested += HandleContinueRequested;
            overlay.PreviousRequested += HandlePreviousRequested;
            runtime.RestartRequested += HandleRestartRequested;
            if (citizenPopulation != null)
            {
                citizenPopulation.ResidentClicked += HandleCitizenClicked;
            }
            foreach (var room in rooms)
            {
                room.Clicked += HandleRoomClicked;
            }
            layout.TrayStateChanged += HandleTrayStateChanged;
            layout.LayoutConfirmed += HandleLayoutConfirmed;
            feeding.FoodPlaced += HandleFoodPlaced;
            feeding.FeedingModeChanged += HandleFeedingModeChanged;
            feeding.InvalidPlacementAttempted += HandleInvalidPlacement;
        }

        private void OnDestroy()
        {
            if (overlay != null)
            {
                overlay.SkipRequested -= Skip;
                overlay.HighlightedResidentRequested -= HandleHighlightedResidentRequested;
                overlay.ContinueRequested -= HandleContinueRequested;
                overlay.PreviousRequested -= HandlePreviousRequested;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
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

        private void HandleRestartRequested()
        {
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 0)
            {
                Begin(false);
            }
            else
            {
                Model.Hide();
                guidePageIndex = -1;
                runtime.SetOnboardingOpen(false);
                overlay.Show(OnboardingStep.Hidden, null, IsChinese());
            }
        }

        private void Begin(bool isReplay)
        {
            if (layout.IsEditing)
            {
                layout.SetGuidedTargetRoom(null);
                layout.CancelEditing();
            }
            feeding.CancelFeedingMode();
            replaying = isReplay;
            Model.Begin();
            guidePageIndex = 0;
            sawTrayOccupied = false;
            wasteInspected = false;
            layoutRetryOffered = false;
            DestroyRouteLine();
            layout.SetGuidedTargetRoom(null);
            runtime.SetOnboardingOpen(true);
            ShowCurrentStep();
        }

        private void HandleCitizenClicked(string residentId, CitizenDemoAgent selected)
        {
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

        private void HandleContinueRequested()
        {
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
    }
}
