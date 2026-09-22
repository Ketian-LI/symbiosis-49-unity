using System;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public sealed class WasteCollectionNotification : MonoBehaviour
    {
        public const float TruckTravelDuration = 1.05f;
        public const float FirstBinClearTime = 0.38f;
        public const float BinClearInterval = 0.16f;
        public const float CompleteDuration = 0.9f;
        public const float EmergencyDuration = 1.15f;

        private static readonly Color WarningYellow = new(0.91f, 0.70f, 0.27f, 1f);
        private static readonly Color RingTrack = new(0.16f, 0.18f, 0.19f, 0.32f);

        private GameRuntimeController runtime;
        private WasteManagementController wasteController;
        private Camera worldCamera;
        private RectTransform canvasRoot;
        private Func<string, Transform> roomTransformResolver;
        private RectTransform notificationRoot;
        private RectTransform truckRect;
        private CanvasGroup notificationGroup;
        private CircularMeterGraphic countdownMeter;
        private Image countdownArtwork;
        private Image truckImage;
        private readonly Image[] binPips = new Image[4];
        private Image completeImage;
        private RectTransform emergencyRect;
        private CanvasGroup emergencyGroup;
        private Transform emergencyWorldTarget;
        private float animationElapsed;
        private float emergencyElapsed;
        private DisplayState state;

        private enum DisplayState
        {
            Hidden,
            Warning,
            Collecting,
            Complete
        }

        public void Build(
            RectTransform parent,
            GameRuntimeController runtimeController,
            WasteManagementController controller,
            Camera camera,
            HideFlags hideFlags,
            Func<string, Transform> transformResolver)
        {
            canvasRoot = parent;
            runtime = runtimeController;
            wasteController = controller;
            worldCamera = camera;
            roomTransformResolver = transformResolver;

            notificationRoot = NewRect("Waste Collection Notice", parent, hideFlags);
            notificationRoot.anchorMin = notificationRoot.anchorMax = new Vector2(0.5f, 1f);
            notificationRoot.pivot = new Vector2(0.5f, 1f);
            notificationRoot.anchoredPosition = new Vector2(132f, -23f);
            notificationRoot.sizeDelta = new Vector2(390f, 112f);
            notificationGroup = notificationRoot.gameObject.AddComponent<CanvasGroup>();
            notificationGroup.blocksRaycasts = false;
            notificationGroup.interactable = false;

            countdownArtwork = NewImage(
                "Countdown Artwork",
                notificationRoot,
                GarbageTruckVisualCatalog.GetSprite(GarbageTruckVisual.CountdownRing),
                hideFlags);
            SetRect(countdownArtwork.rectTransform, 147f, 8f, 96f, 96f);
            countdownArtwork.preserveAspect = true;
            countdownArtwork.color = new Color(1f, 1f, 1f, 0.42f);

            var meterObject = NewRect("Live Countdown", notificationRoot, hideFlags);
            SetRect(meterObject, 153f, 14f, 84f, 84f);
            countdownMeter = meterObject.gameObject.AddComponent<CircularMeterGraphic>();
            countdownMeter.raycastTarget = false;
            countdownMeter.color = Color.white;
            countdownMeter.SetValue(1f, WarningYellow);

            truckImage = NewImage(
                "Garbage Truck",
                notificationRoot,
                GarbageTruckVisualCatalog.GetSprite(GarbageTruckVisual.Truck),
                hideFlags);
            truckRect = truckImage.rectTransform;
            SetRect(truckRect, 157f, 20f, 76f, 70f);
            truckImage.preserveAspect = true;

            for (var index = 0; index < binPips.Length; index++)
            {
                var pip = NewImage(
                    $"Waste Bin {index + 1}",
                    notificationRoot,
                    GarbageTruckVisualCatalog.GetSprite(GarbageTruckVisual.BinPip),
                    hideFlags);
                SetRect(pip.rectTransform, 151f + index * 23f, 0f, 20f, 20f);
                pip.preserveAspect = true;
                binPips[index] = pip;
            }

            completeImage = NewImage(
                "Collection Complete",
                notificationRoot,
                GarbageTruckVisualCatalog.GetSprite(GarbageTruckVisual.CollectionComplete),
                hideFlags);
            SetRect(completeImage.rectTransform, 155f, 12f, 80f, 80f);
            completeImage.preserveAspect = true;
            completeImage.gameObject.SetActive(false);

            emergencyRect = NewRect("Emergency Collection Cost", parent, hideFlags);
            emergencyRect.anchorMin = emergencyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emergencyRect.pivot = new Vector2(0.5f, 0f);
            emergencyRect.sizeDelta = new Vector2(94f, 94f);
            var emergencyImage = emergencyRect.gameObject.AddComponent<Image>();
            emergencyImage.sprite = GarbageTruckVisualCatalog.GetSprite(GarbageTruckVisual.EmergencyCost);
            emergencyImage.preserveAspect = true;
            emergencyImage.raycastTarget = false;
            emergencyGroup = emergencyRect.gameObject.AddComponent<CanvasGroup>();
            emergencyGroup.blocksRaycasts = false;
            emergencyGroup.interactable = false;
            emergencyRect.gameObject.SetActive(false);

            wasteController.CollectionWarningRaised += HandleCollectionWarning;
            wasteController.MunicipalCollectionCompleted += HandleMunicipalCollection;
            wasteController.EmergencyCollectionCompleted += HandleEmergencyCollection;
            HideNotice();
        }

        private void OnDestroy()
        {
            if (wasteController == null)
            {
                return;
            }

            wasteController.CollectionWarningRaised -= HandleCollectionWarning;
            wasteController.MunicipalCollectionCompleted -= HandleMunicipalCollection;
            wasteController.EmergencyCollectionCompleted -= HandleEmergencyCollection;
        }

        private void Update()
        {
            if (runtime == null || notificationRoot == null)
            {
                return;
            }

            if (state == DisplayState.Hidden && IsCollectionWarningWindow(runtime.Clock.TotalSeconds))
            {
                EnterWarning();
            }

            switch (state)
            {
                case DisplayState.Warning:
                    UpdateWarning();
                    break;
                case DisplayState.Collecting:
                    if (!runtime.IsPaused)
                    {
                        animationElapsed += Time.unscaledDeltaTime;
                    }
                    UpdateCollectionAnimation();
                    break;
                case DisplayState.Complete:
                    if (!runtime.IsPaused)
                    {
                        animationElapsed += Time.unscaledDeltaTime;
                    }
                    UpdateCompleteAnimation();
                    break;
            }

            UpdateEmergencyFeedback();
        }

        public static bool IsCollectionWarningWindow(double totalSeconds)
        {
            var safeSeconds = Math.Max(0d, totalSeconds);
            var dayIndex = (int)(safeSeconds / SimulationClockModel.CycleSeconds);
            var dayNumber = dayIndex + 1;
            var secondsIntoDay = safeSeconds % SimulationClockModel.CycleSeconds;
            return dayNumber % 2 == 0 &&
                   secondsIntoDay >= WasteCollectionSchedule.CollectionWarningSeconds &&
                   secondsIntoDay < WasteCollectionSchedule.MunicipalCollectionSeconds;
        }

        public static float WarningCountdown01(double totalSeconds)
        {
            if (!IsCollectionWarningWindow(totalSeconds))
            {
                return 0f;
            }

            var secondsIntoDay = totalSeconds % SimulationClockModel.CycleSeconds;
            var duration = WasteCollectionSchedule.MunicipalCollectionSeconds -
                           WasteCollectionSchedule.CollectionWarningSeconds;
            return Mathf.Clamp01((float)(
                (WasteCollectionSchedule.MunicipalCollectionSeconds - secondsIntoDay) /
                duration));
        }

        public static float TruckTravel01(float elapsed)
        {
            return Mathf.Clamp01(elapsed / TruckTravelDuration);
        }

        public static int VisibleBinCount(float elapsed)
        {
            var cleared = Mathf.Clamp(
                Mathf.FloorToInt((elapsed - FirstBinClearTime) / BinClearInterval) + 1,
                0,
                4);
            return 4 - cleared;
        }

        private void HandleCollectionWarning(int dayNumber)
        {
            EnterWarning();
        }

        private void HandleMunicipalCollection(int dayNumber)
        {
            state = DisplayState.Collecting;
            animationElapsed = 0f;
            notificationRoot.gameObject.SetActive(true);
            notificationGroup.alpha = 1f;
            countdownArtwork.gameObject.SetActive(false);
            countdownMeter.gameObject.SetActive(false);
            truckImage.gameObject.SetActive(true);
            completeImage.gameObject.SetActive(false);
            SetPipVisibility(4);
        }

        private void HandleEmergencyCollection(string roomId, int resourceCost)
        {
            emergencyWorldTarget = roomTransformResolver?.Invoke(roomId);
            emergencyElapsed = 0f;
            emergencyRect.gameObject.SetActive(true);
            emergencyGroup.alpha = 1f;
            UpdateEmergencyPosition();
        }

        private void EnterWarning()
        {
            state = DisplayState.Warning;
            notificationRoot.gameObject.SetActive(true);
            notificationGroup.alpha = 1f;
            countdownArtwork.gameObject.SetActive(true);
            countdownMeter.gameObject.SetActive(true);
            truckImage.gameObject.SetActive(true);
            completeImage.gameObject.SetActive(false);
            SetRect(truckRect, 157f, 20f, 76f, 70f);
            SetPipVisibility(4);
            UpdateWarning();
        }

        private void UpdateWarning()
        {
            if (!IsCollectionWarningWindow(runtime.Clock.TotalSeconds))
            {
                HideNotice();
                return;
            }

            countdownMeter.SetValue(
                WarningCountdown01(runtime.Clock.TotalSeconds),
                WarningYellow);
        }

        private void UpdateCollectionAnimation()
        {
            var travel = TruckTravel01(animationElapsed);
            var easedTravel = travel * travel * (3f - 2f * travel);
            var x = Mathf.Lerp(-18f, 332f, easedTravel);
            SetRect(truckRect, x, 20f, 76f, 70f);
            SetPipVisibility(VisibleBinCount(animationElapsed));

            if (travel < 1f)
            {
                return;
            }

            state = DisplayState.Complete;
            animationElapsed = 0f;
            truckImage.gameObject.SetActive(false);
            completeImage.gameObject.SetActive(true);
        }

        private void UpdateCompleteAnimation()
        {
            var progress = Mathf.Clamp01(animationElapsed / CompleteDuration);
            notificationGroup.alpha = 1f - Mathf.Clamp01((progress - 0.55f) / 0.45f);
            if (progress >= 1f)
            {
                HideNotice();
            }
        }

        private void UpdateEmergencyFeedback()
        {
            if (emergencyRect == null || !emergencyRect.gameObject.activeSelf)
            {
                return;
            }

            if (!runtime.IsPaused)
            {
                emergencyElapsed += Time.unscaledDeltaTime;
            }

            UpdateEmergencyPosition();
            var progress = Mathf.Clamp01(emergencyElapsed / EmergencyDuration);
            emergencyGroup.alpha = 1f - Mathf.Clamp01((progress - 0.58f) / 0.42f);
            emergencyRect.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, Mathf.Min(1f, progress * 4f));
            if (progress >= 1f)
            {
                emergencyRect.gameObject.SetActive(false);
                emergencyWorldTarget = null;
            }
        }

        private void UpdateEmergencyPosition()
        {
            if (emergencyWorldTarget == null || worldCamera == null || canvasRoot == null)
            {
                emergencyRect.anchoredPosition = new Vector2(0f, 90f);
                return;
            }

            var screenPoint = worldCamera.WorldToScreenPoint(
                emergencyWorldTarget.position + Vector3.up * 2.35f);
            if (screenPoint.z <= 0f)
            {
                emergencyRect.gameObject.SetActive(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot,
                screenPoint,
                worldCamera,
                out var localPoint);
            emergencyRect.anchoredPosition = localPoint;
        }

        private void HideNotice()
        {
            state = DisplayState.Hidden;
            animationElapsed = 0f;
            if (notificationRoot != null)
            {
                notificationRoot.gameObject.SetActive(false);
            }
        }

        private void SetPipVisibility(int visibleCount)
        {
            for (var index = 0; index < binPips.Length; index++)
            {
                binPips[index].color = index < visibleCount
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0f);
            }
        }

        private static RectTransform NewRect(string name, Transform parent, HideFlags hideFlags)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            instance.hideFlags = hideFlags;
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }

        private static Image NewImage(
            string name,
            Transform parent,
            Sprite sprite,
            HideFlags hideFlags)
        {
            var rect = NewRect(name, parent, hideFlags);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect(
            RectTransform rect,
            float left,
            float bottom,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
