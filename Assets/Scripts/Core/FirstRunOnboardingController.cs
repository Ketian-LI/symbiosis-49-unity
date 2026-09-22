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
        private CitizenDemoAgent citizen;
        private FirstRunOnboardingOverlay overlay;
        private RoomView wasteTarget;
        private RoomView practiceTarget;
        private bool sawTrayOccupied;
        private LineRenderer routeLine;
        private Material routeMaterial;

        public FirstRunOnboardingModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            PlayerFeedingController feedingController,
            ResidentPopulationController residentController,
            CitizenDemoAgent citizenAgent,
            IEnumerable<RoomView> roomViews,
            UrbanWildlifeHud hud,
            Camera worldCamera,
            Font font,
            HideFlags hideFlags)
        {
            runtime = runtimeController;
            layout = layoutController;
            feeding = feedingController;
            residents = residentController;
            citizen = citizenAgent;
            rooms.AddRange(roomViews ?? Array.Empty<RoomView>());
            wasteTarget = rooms.FirstOrDefault(item => item.Spec.Type == RoomType.Trash);
            practiceTarget = rooms.FirstOrDefault(item => item.Spec.Movable && item.Spec.CellCount == 1);
            Model = new FirstRunOnboardingModel();

            overlay = hud.gameObject.AddComponent<FirstRunOnboardingOverlay>();
            overlay.Build(font, worldCamera, hideFlags);
            overlay.SkipRequested += Skip;
            runtime.RestartRequested += HandleRestartRequested;
            if (citizen != null)
            {
                citizen.Clicked += HandleCitizenClicked;
            }
            foreach (var room in rooms)
            {
                room.Clicked += HandleRoomClicked;
            }
            layout.TrayStateChanged += HandleTrayStateChanged;
            layout.LayoutConfirmed += HandleLayoutConfirmed;
            feeding.FoodPlaced += HandleFoodPlaced;
        }

        private void OnDestroy()
        {
            if (overlay != null)
            {
                overlay.SkipRequested -= Skip;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            if (citizen != null)
            {
                citizen.Clicked -= HandleCitizenClicked;
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
            }
            DestroyRouteLine();
        }

        public void Replay()
        {
            Begin();
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
                OnboardingStep.SelectResident => citizen?.transform,
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
            Model.Skip();
            CompleteAndDismiss();
        }

        private void HandleRestartRequested()
        {
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 0)
            {
                Begin();
            }
            else
            {
                Model.Hide();
                runtime.SetOnboardingOpen(false);
                overlay.Show(OnboardingStep.Hidden, null, IsChinese());
            }
        }

        private void Begin()
        {
            Model.Begin();
            sawTrayOccupied = false;
            DestroyRouteLine();
            layout.SetGuidedTargetRoom(null);
            runtime.SetOnboardingOpen(true);
            ShowCurrentStep();
        }

        private void HandleCitizenClicked(CitizenDemoAgent selected)
        {
            if (Model.Step != OnboardingStep.SelectResident || selected != citizen)
            {
                return;
            }
            DrawResidentRoute();
            Model.CompleteStep(OnboardingStep.SelectResident);
            ShowCurrentStep();
        }

        private void HandleRoomClicked(RoomView room)
        {
            if (Model.Step != OnboardingStep.InspectWaste || room != wasteTarget)
            {
                return;
            }
            Model.CompleteStep(OnboardingStep.InspectWaste);
            DestroyRouteLine();
            layout.SetGuidedTargetRoom(practiceTarget?.Spec.Id);
            layout.EnterEditing();
            ShowCurrentStep();
        }

        private void HandleTrayStateChanged(bool occupied)
        {
            if (Model.Step == OnboardingStep.PracticeLayout && occupied)
            {
                sawTrayOccupied = true;
            }
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
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            layout.SetGuidedTargetRoom(null);
            runtime.SetOnboardingOpen(false);
            overlay.Show(OnboardingStep.Complete, null, IsChinese());
            DestroyRouteLine();
        }

        private void ShowCurrentStep()
        {
            var target = Model.Step switch
            {
                OnboardingStep.SelectResident => citizen?.transform,
                OnboardingStep.InspectWaste => wasteTarget?.VisualRoot,
                OnboardingStep.PracticeLayout => practiceTarget?.VisualRoot,
                _ => null
            };
            overlay.Show(Model.Step, target, IsChinese());
        }

        private void DrawResidentRoute()
        {
            var resident = residents.Model?.Residents?.FirstOrDefault();
            if (resident == null)
            {
                return;
            }
            var routeRooms = new[] { resident.residenceId, resident.assignedOfficeId, resident.assignedFoodShopId }
                .Select(id => rooms.FirstOrDefault(room => room.Spec.Id == id))
                .Where(room => room != null)
                .ToArray();
            if (routeRooms.Length < 2)
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
            routeLine.positionCount = routeRooms.Length;
            routeLine.useWorldSpace = true;
            for (var index = 0; index < routeRooms.Length; index++)
            {
                routeLine.SetPosition(index, routeRooms[index].VisualRoot.position + Vector3.up * 0.18f);
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
