using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class RoomView : MonoBehaviour
    {
        private Transform visualRoot;
        private Renderer floorRenderer;
        private Color baseColor;
        private bool hovered;
        private bool hoverVisible;
        private Vector3 restingPosition;
        private Quaternion restingRotation;
        private float lastClickTime = -10f;
        private bool layoutEditing;
        private bool dragging;
        private bool swapPreviewing;
        private bool? dragPreviewLegal;
        private RoomSelectionMarker selectionMarker;
        private AnimalPassageOverlay animalPassages;
        private TextMesh facilityCapacityText;
        private int editingDay = 1;
        private int placedQuarterTurns;

        public event Action<RoomView> Clicked;
        public event Action<RoomView> DoubleClicked;
        public event Action<RoomView, Vector2> DragStarted;
        public event Action<RoomView, Vector2> Dragged;
        public event Action<RoomView, Vector2> DragEnded;
        public event Action<RoomView, bool> HoverChanged;

        public RoomSpec Spec { get; private set; }
        public Transform VisualRoot => visualRoot;

        public void Initialize(
            RoomSpec spec,
            Transform roomRoot,
            Renderer renderer,
            Color roomColor,
            float roomWidth,
            float roomDepth,
            HideFlags generatedHideFlags)
        {
            Spec = spec;
            visualRoot = roomRoot;
            floorRenderer = renderer;
            baseColor = roomColor;
            restingPosition = roomRoot.localPosition;
            restingRotation = roomRoot.localRotation;
            selectionMarker = roomRoot.gameObject.GetComponent<RoomSelectionMarker>() ??
                              roomRoot.gameObject.AddComponent<RoomSelectionMarker>();
            selectionMarker.Initialize(roomWidth, roomDepth, generatedHideFlags);
            animalPassages = roomRoot.GetComponentInChildren<AnimalPassageOverlay>(true);
            if (spec.Type is RoomType.Office or RoomType.Canteen)
            {
                var office = spec.Type == RoomType.Office;
                var capacity = office
                    ? ResidentPopulationModel.OfficeCapacity
                    : ResidentPopulationModel.FoodShopCapacity;
                facilityCapacityText = RoomMapBadgeVisual.BuildCapacity(
                    visualRoot, office, new Vector3(-0.88f, 1.67f, 0f),
                    0, capacity, floorRenderer.sharedMaterial, generatedHideFlags);
            }
            ApplyState();
        }

        public void SetFacilityCapacity(int used)
        {
            if (facilityCapacityText == null) return;
            var capacity = Spec.Type == RoomType.Office
                ? ResidentPopulationModel.OfficeCapacity
                : ResidentPopulationModel.FoodShopCapacity;
            facilityCapacityText.text = $"{Mathf.Clamp(used, 0, capacity)}/{capacity}";
        }

        public void SetSelected(bool value)
        {
            selectionMarker?.SetVisible(value);
            animalPassages?.SetShowUnconnectedPorts(value);
            ApplyState();
        }

        public void SetLayoutEditing(bool value, int dayNumber = 1)
        {
            layoutEditing = value;
            editingDay = dayNumber;
            animalPassages?.SetPreview(value, placedQuarterTurns, editingDay);
            RefreshHoverState();
            if (!value)
            {
                dragging = false;
                swapPreviewing = false;
                dragPreviewLegal = null;
                visualRoot.localPosition = restingPosition;
            }

            ApplyState();
        }

        public void SetAnimalPassageConnections(IReadOnlyCollection<AnimalPassagePort> ports)
        {
            animalPassages?.SetConnectedPorts(ports);
        }

        public void SetDragPreview(bool legal)
        {
            dragPreviewLegal = legal;
            ApplyState();
        }

        public void SetDraggedLocalPosition(Vector3 localPosition)
        {
            visualRoot.localPosition = localPosition;
            Physics.SyncTransforms();
        }

        public void SetDraggedPlacement(Vector3 localPosition, int quarterTurns)
        {
            visualRoot.localPosition = localPosition;
            visualRoot.localRotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
            if (layoutEditing)
                animalPassages?.SetPreview(true, quarterTurns, editingDay);
            Physics.SyncTransforms();
        }

        public void SetSwapPreviewLocalPosition(Vector3 localPosition)
        {
            swapPreviewing = true;
            dragPreviewLegal = true;
            visualRoot.localPosition = localPosition;
            Physics.SyncTransforms();
            ApplyState();
        }

        public void ClearSwapPreview()
        {
            swapPreviewing = false;
            dragPreviewLegal = null;
            visualRoot.localPosition = restingPosition;
            visualRoot.localRotation = restingRotation;
            if (layoutEditing)
                animalPassages?.SetPreview(true, placedQuarterTurns, editingDay);
            Physics.SyncTransforms();
            ApplyState();
        }

        public void ApplyPlacement(Vector3 localPosition, int quarterTurns)
        {
            placedQuarterTurns = quarterTurns;
            restingPosition = localPosition;
            restingRotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
            visualRoot.localPosition = restingPosition;
            visualRoot.localRotation = restingRotation;
            if (layoutEditing)
                animalPassages?.SetPreview(true, placedQuarterTurns, editingDay);
            swapPreviewing = false;
            dragPreviewLegal = null;
            ApplyState();
            Physics.SyncTransforms();
        }

        public void RestoreRestingPlacement()
        {
            visualRoot.localPosition = restingPosition;
            visualRoot.localRotation = restingRotation;
            dragPreviewLegal = null;
            ApplyState();
            Physics.SyncTransforms();
        }

        private void OnMouseEnter()
        {
            hovered = true;
            RefreshHoverState();
        }

        private void OnMouseExit()
        {
            hovered = false;
            RefreshHoverState();
        }

        private void Update()
        {
            // A room can stay under the pointer while a HUD button moves over it.
            // OnMouseExit is not raised in that case, so refresh the UI hit test.
            if (hovered || hoverVisible)
            {
                RefreshHoverState();
            }

            if (!layoutEditing || !dragging)
            {
                return;
            }

            // The room collider moves with the preview and timeScale is zero while
            // editing. Keep tracking the original press even after leaving it.
            if (Input.GetMouseButton(0))
            {
                Dragged?.Invoke(this, Input.mousePosition);
            }
            else
            {
                EndDrag(Input.mousePosition);
            }
        }

        private void LateUpdate()
        {
            if (facilityCapacityText != null)
                facilityCapacityText.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void OnMouseDown()
        {
            if (IsPointerBlockedByUi())
            {
                return;
            }

            if (layoutEditing)
            {
                if (Spec == null || !Spec.Movable)
                {
                    return;
                }

                dragging = true;
                DragStarted?.Invoke(this, Input.mousePosition);
                return;
            }

            var now = Time.unscaledTime;
            if (now - lastClickTime <= 0.34f)
            {
                DoubleClicked?.Invoke(this);
                lastClickTime = -10f;
            }
            else
            {
                Clicked?.Invoke(this);
                lastClickTime = now;
            }
        }

        private void OnMouseUp()
        {
            if (layoutEditing)
            {
                EndDrag(Input.mousePosition);
            }
        }

        private void EndDrag(Vector2 screenPosition)
        {
            if (!dragging)
            {
                return;
            }
            dragging = false;
            DragEnded?.Invoke(this, screenPosition);
        }

        private void RefreshHoverState()
        {
            var shouldShow = hovered && !layoutEditing && !IsPointerBlockedByUi();
            if (hoverVisible == shouldShow)
            {
                return;
            }

            hoverVisible = shouldShow;
            HoverChanged?.Invoke(this, hoverVisible);
            ApplyState();
        }

        private static bool IsPointerBlockedByUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void ApplyState()
        {
            if (floorRenderer == null || visualRoot == null)
            {
                return;
            }

            var displayColor = dragPreviewLegal.HasValue
                ? Color.Lerp(baseColor, dragPreviewLegal.Value ? UrbanPalette.Legal : UrbanPalette.Risk, 0.68f)
                : hoverVisible
                    ? Color.Lerp(baseColor, UrbanPalette.Legal, 0.22f)
                    : baseColor;

            UrbanVisualFactory.ApplyColor(floorRenderer, displayColor);
            if (!dragging && !swapPreviewing)
            {
                visualRoot.localPosition = restingPosition;
                visualRoot.localRotation = restingRotation;
            }
        }
    }
}
