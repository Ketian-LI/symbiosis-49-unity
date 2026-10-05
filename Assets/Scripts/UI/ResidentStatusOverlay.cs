using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    public sealed class ResidentStatusOverlay : MonoBehaviour
    {
        private static readonly Color ArrivalCyan = new(0.27f, 0.78f, 0.74f, 0.96f);
        private static readonly Color MoveAmber = new(0.93f, 0.62f, 0.20f, 0.96f);
        private static readonly Color LeaveRed = new(0.65f, 0.20f, 0.16f, 0.96f);
        private static readonly Color WarmPaper = new(0.96f, 0.92f, 0.82f, 1f);
        private const float ArrivalNoticeDuration = 6.5f;

        private readonly List<Badge> badges = new();
        private ResidentPopulationController residents;
        private GameRuntimeController runtime;
        private RectTransform canvasRoot;
        private Camera worldCamera;
        private Font font;
        private HideFlags generatedHideFlags;
        private Func<string, Transform> roomTransformResolver;
        private RectTransform arrivalNotice;
        private CanvasGroup arrivalNoticeGroup;
        private Text arrivalTitle;
        private Text arrivalDetail;
        private string arrivalResidenceId;
        private int arrivalResidentCount;
        private float arrivalNoticeAge;
        private bool arrivalNoticeVisible;
        private InterfaceLanguage arrivalNoticeLanguage;

        public void Build(
            RectTransform parent,
            Font uiFont,
            HideFlags generatedHideFlags,
            Camera camera,
            GameRuntimeController runtimeController,
            ResidentPopulationController residentController,
            Func<string, Transform> transformResolver)
        {
            if (residents != null)
            {
                residents.StateChanged -= RefreshBadges;
                residents.DayCompleted -= HandleResidentDayCompleted;
            }

            canvasRoot = parent;
            font = uiFont;
            this.generatedHideFlags = generatedHideFlags;
            worldCamera = camera;
            runtime = runtimeController;
            residents = residentController;
            roomTransformResolver = transformResolver;
            residents.StateChanged += RefreshBadges;
            residents.DayCompleted += HandleResidentDayCompleted;
            if (arrivalNotice == null)
            {
                BuildArrivalNotice();
            }
            RefreshBadges();
        }

        private void OnDestroy()
        {
            if (residents != null)
            {
                residents.StateChanged -= RefreshBadges;
                residents.DayCompleted -= HandleResidentDayCompleted;
            }
        }

        private void LateUpdate()
        {
            UpdateArrivalNotice();
            if (runtime == null || runtime.AtDesktop || runtime.ResultsOpen)
            {
                foreach (var badge in badges)
                {
                    badge.Root.gameObject.SetActive(false);
                }
                return;
            }

            foreach (var badge in badges)
            {
                UpdateBadgePosition(badge);
            }
        }

        private void BuildArrivalNotice()
        {
            arrivalNotice = NewRect("New Resident Arrival Notice", canvasRoot);
            arrivalNotice.anchorMin = arrivalNotice.anchorMax = new Vector2(0.5f, 1f);
            arrivalNotice.pivot = new Vector2(0.5f, 1f);
            arrivalNotice.sizeDelta = new Vector2(478f, 112f);
            arrivalNotice.anchoredPosition = new Vector2(0f, -248f);

            var panel = arrivalNotice.gameObject.AddComponent<RoundedPanelGraphic>();
            panel.color = new Color(0.08f, 0.13f, 0.16f, 0.88f);
            panel.CornerRadius = 23f;
            panel.BorderWidth = 2f;
            panel.BorderColor = new Color(ArrivalCyan.r, ArrivalCyan.g, ArrivalCyan.b, 0.78f);
            panel.raycastTarget = false;
            arrivalNoticeGroup = arrivalNotice.gameObject.AddComponent<CanvasGroup>();
            arrivalNoticeGroup.alpha = 0f;
            arrivalNoticeGroup.interactable = false;
            arrivalNoticeGroup.blocksRaycasts = false;

            var iconRoot = NewRect("Arrival Count Icon", arrivalNotice);
            iconRoot.anchorMin = iconRoot.anchorMax = new Vector2(0f, 0.5f);
            iconRoot.pivot = new Vector2(0f, 0.5f);
            iconRoot.anchoredPosition = new Vector2(20f, 0f);
            iconRoot.sizeDelta = new Vector2(64f, 64f);
            var iconPanel = iconRoot.gameObject.AddComponent<RoundedPanelGraphic>();
            iconPanel.color = new Color(ArrivalCyan.r, ArrivalCyan.g, ArrivalCyan.b, 0.2f);
            iconPanel.CornerRadius = 32f;
            iconPanel.BorderWidth = 1.5f;
            iconPanel.BorderColor = ArrivalCyan;
            iconPanel.raycastTarget = false;
            CreateNoticeText("Plus One", iconRoot, "+1", 27, FontStyle.Bold,
                TextAnchor.MiddleCenter, WarmPaper, Vector2.zero, Vector2.zero);

            arrivalTitle = CreateNoticeText("Arrival Title", arrivalNotice, string.Empty, 25,
                FontStyle.Bold, TextAnchor.MiddleLeft, WarmPaper,
                new Vector2(102f, 60f), new Vector2(-18f, -14f));
            arrivalDetail = CreateNoticeText("Arrival Detail", arrivalNotice, string.Empty, 18,
                FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.82f, 0.94f, 0.9f, 1f),
                new Vector2(102f, 18f), new Vector2(-18f, -60f));
            arrivalNotice.gameObject.SetActive(false);
        }

        private Text CreateNoticeText(string name, RectTransform parent, string value,
            int size, FontStyle style, TextAnchor alignment, Color color,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = UrbanFontResolver.GetFont(style);
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style == FontStyle.Bold && label.font != font ? FontStyle.Normal : style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private void HandleResidentDayCompleted(ResidentDayReport report)
        {
            if (!report.ResidentArrived || residents?.Model == null || arrivalNotice == null)
            {
                return;
            }

            arrivalResidenceId = report.ArrivalResidenceId;
            arrivalResidentCount = residents.Model.Residents.Count;
            arrivalNoticeAge = 0f;
            arrivalNoticeVisible = true;
            RefreshArrivalNoticeText();
            arrivalNotice.anchoredPosition = new Vector2(0f, -248f);
            arrivalNoticeGroup.alpha = 0f;
            arrivalNotice.gameObject.SetActive(true);
            arrivalNotice.SetAsLastSibling();
        }

        private void RefreshArrivalNoticeText()
        {
            arrivalNoticeLanguage = runtime != null ? runtime.Language : InterfaceLanguage.Chinese;
            var residenceLetter = arrivalResidenceId != null &&
                                  arrivalResidenceId.StartsWith("residence-", StringComparison.Ordinal)
                ? arrivalResidenceId.Substring("residence-".Length).ToUpperInvariant()
                : arrivalResidenceId;
            if (arrivalNoticeLanguage == InterfaceLanguage.Chinese)
            {
                var roomName = arrivalResidenceId;
                foreach (var room in RoomLayoutData.All)
                {
                    if (room.Id == arrivalResidenceId)
                    {
                        roomName = room.DisplayName;
                        break;
                    }
                }
                arrivalTitle.text = "新居民入住";
                arrivalDetail.text = $"{roomName} · 居民 {arrivalResidentCount}/{ResidentPopulationModel.MaximumResidents}";
            }
            else
            {
                arrivalTitle.text = "New resident arrived";
                arrivalDetail.text = $"Residence {residenceLetter} · Residents {arrivalResidentCount}/{ResidentPopulationModel.MaximumResidents}";
            }
        }

        private void UpdateArrivalNotice()
        {
            if (!arrivalNoticeVisible || arrivalNotice == null)
            {
                return;
            }

            var suppressed = runtime == null || runtime.AtDesktop || runtime.ResultsOpen ||
                             runtime.PauseMenuOpen || runtime.SettingsOpen ||
                             runtime.CameraCalibrationOpen;
            if (arrivalNotice.gameObject.activeSelf == suppressed)
            {
                arrivalNotice.gameObject.SetActive(!suppressed);
            }
            if (suppressed)
            {
                return;
            }

            if (runtime.Language != arrivalNoticeLanguage)
            {
                RefreshArrivalNoticeText();
            }
            arrivalNoticeAge += Time.unscaledDeltaTime;
            if (arrivalNoticeAge >= ArrivalNoticeDuration)
            {
                arrivalNoticeVisible = false;
                arrivalNotice.gameObject.SetActive(false);
                return;
            }

            var enter = Mathf.Clamp01(arrivalNoticeAge / 0.28f);
            var exit = Mathf.Clamp01((ArrivalNoticeDuration - arrivalNoticeAge) / 0.48f);
            arrivalNoticeGroup.alpha = Mathf.Min(enter, exit);
            var eased = 1f - (1f - enter) * (1f - enter);
            arrivalNotice.anchoredPosition = new Vector2(0f, Mathf.Lerp(-248f, -230f, eased));
        }

        private void RefreshBadges()
        {
            foreach (var badge in badges)
            {
                Destroy(badge.Root.gameObject);
            }
            badges.Clear();

            if (residents?.Model == null)
            {
                return;
            }

            foreach (var resident in residents.Model.Residents)
            {
                switch (resident.WarningState)
                {
                    case ResidentWarningState.RouteAtRisk:
                        badges.Add(CreateBadge(
                            resident.residenceId,
                            "!",
                            MoveAmber,
                            "Resident route warning"));
                        break;
                    case ResidentWarningState.LeavingTomorrow:
                        badges.Add(CreateBadge(
                            resident.residenceId,
                            "↗",
                            LeaveRed,
                            "Resident leaving after next failed day"));
                        break;
                }
            }

            if (!string.IsNullOrEmpty(residents.Model.ProspectiveResidenceId))
            {
                badges.Add(CreateBadge(
                    residents.Model.ProspectiveResidenceId,
                    "▣",
                    ArrivalCyan,
                    "Prospective resident luggage"));
            }

            if (arrivalNoticeVisible)
            {
                arrivalNotice.SetAsLastSibling();
            }
        }

        private Badge CreateBadge(string roomId, string glyph, Color color, string name)
        {
            var root = NewRect(name, canvasRoot);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(46f, 46f);
            var image = root.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var labelRect = NewRect("Pictogram", root);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.gameObject.AddComponent<CanvasRenderer>();
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = UrbanFontResolver.GetFont(FontStyle.Bold);
            label.text = glyph;
            label.fontSize = 28;
            label.fontStyle = label.font != font ? FontStyle.Normal : FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = WarmPaper;
            label.raycastTarget = false;

            var badge = new Badge(root, roomId);
            UpdateBadgePosition(badge);
            return badge;
        }

        private void UpdateBadgePosition(Badge badge)
        {
            var target = roomTransformResolver?.Invoke(badge.RoomId);
            if (target == null || worldCamera == null)
            {
                badge.Root.gameObject.SetActive(false);
                return;
            }

            var screenPoint = worldCamera.WorldToScreenPoint(target.position + Vector3.up * 2.2f);
            if (screenPoint.z <= 0f)
            {
                badge.Root.gameObject.SetActive(false);
                return;
            }

            badge.Root.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot,
                screenPoint,
                worldCamera,
                out var localPoint);
            badge.Root.anchoredPosition = localPoint;
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            instance.hideFlags = generatedHideFlags;
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }

        private readonly struct Badge
        {
            public Badge(RectTransform root, string roomId)
            {
                Root = root;
                RoomId = roomId;
            }

            public RectTransform Root { get; }
            public string RoomId { get; }
        }
    }
}
