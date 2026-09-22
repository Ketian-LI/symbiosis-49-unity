using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public sealed class ResidentStatusOverlay : MonoBehaviour
    {
        private static readonly Color ArrivalCyan = new(0.27f, 0.78f, 0.74f, 0.96f);
        private static readonly Color MoveAmber = new(0.93f, 0.62f, 0.20f, 0.96f);
        private static readonly Color LeaveRed = new(0.65f, 0.20f, 0.16f, 0.96f);
        private static readonly Color WarmPaper = new(0.96f, 0.92f, 0.82f, 1f);

        private readonly List<Badge> badges = new();
        private ResidentPopulationController residents;
        private GameRuntimeController runtime;
        private RectTransform canvasRoot;
        private Camera worldCamera;
        private Font font;
        private HideFlags generatedHideFlags;
        private Func<string, Transform> roomTransformResolver;

        public void Build(
            RectTransform parent,
            Font uiFont,
            HideFlags generatedHideFlags,
            Camera camera,
            GameRuntimeController runtimeController,
            ResidentPopulationController residentController,
            Func<string, Transform> transformResolver)
        {
            canvasRoot = parent;
            font = uiFont;
            this.generatedHideFlags = generatedHideFlags;
            worldCamera = camera;
            runtime = runtimeController;
            residents = residentController;
            roomTransformResolver = transformResolver;
            residents.StateChanged += RefreshBadges;
            RefreshBadges();
        }

        private void OnDestroy()
        {
            if (residents != null)
            {
                residents.StateChanged -= RefreshBadges;
            }
        }

        private void LateUpdate()
        {
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
                    case ResidentWarningState.Move:
                        badges.Add(CreateBadge(
                            resident.residenceId,
                            "↔",
                            MoveAmber,
                            "Resident move warning"));
                        break;
                    case ResidentWarningState.LeaveCity:
                        badges.Add(CreateBadge(
                            resident.residenceId,
                            "↗",
                            LeaveRed,
                            "Resident leave-city warning"));
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
            label.font = font;
            label.text = glyph;
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
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
