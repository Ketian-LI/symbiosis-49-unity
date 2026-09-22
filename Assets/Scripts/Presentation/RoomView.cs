using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class RoomView : MonoBehaviour
    {
        private Transform visualRoot;
        private Renderer floorRenderer;
        private Color baseColor;
        private bool hovered;
        private Vector3 restingPosition;
        private Quaternion restingRotation;
        private float lastClickTime = -10f;
        private bool layoutEditing;
        private bool dragging;
        private bool? dragPreviewLegal;
        private RoomSelectionMarker selectionMarker;

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
            ApplyState();
        }

        public void SetSelected(bool value)
        {
            selectionMarker?.SetVisible(value);
            ApplyState();
        }

        public void SetLayoutEditing(bool value)
        {
            if (hovered)
            {
                HoverChanged?.Invoke(this, !value);
            }

            layoutEditing = value;
            if (!value)
            {
                dragging = false;
                dragPreviewLegal = null;
                visualRoot.localPosition = restingPosition;
            }

            ApplyState();
        }

        public void SetDragPreview(bool legal)
        {
            dragPreviewLegal = legal;
            ApplyState();
        }

        public void SetDraggedLocalPosition(Vector3 localPosition)
        {
            visualRoot.localPosition = localPosition;
        }

        public void ApplyPlacement(Vector3 localPosition, int quarterTurns)
        {
            restingPosition = localPosition;
            restingRotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
            visualRoot.localPosition = restingPosition;
            visualRoot.localRotation = restingRotation;
            dragPreviewLegal = null;
            ApplyState();
        }

        public void RestoreRestingPlacement()
        {
            visualRoot.localPosition = restingPosition;
            visualRoot.localRotation = restingRotation;
            dragPreviewLegal = null;
            ApplyState();
        }

        private void OnMouseEnter()
        {
            hovered = true;
            if (!layoutEditing)
            {
                HoverChanged?.Invoke(this, true);
            }
            ApplyState();
        }

        private void OnMouseExit()
        {
            hovered = false;
            HoverChanged?.Invoke(this, false);
            ApplyState();
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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

        private void OnMouseDrag()
        {
            if (layoutEditing && dragging)
            {
                Dragged?.Invoke(this, Input.mousePosition);
            }
        }

        private void OnMouseUp()
        {
            if (!layoutEditing || !dragging)
            {
                return;
            }

            dragging = false;
            DragEnded?.Invoke(this, Input.mousePosition);
        }

        private void ApplyState()
        {
            if (floorRenderer == null || visualRoot == null)
            {
                return;
            }

            var displayColor = dragPreviewLegal.HasValue
                ? Color.Lerp(baseColor, dragPreviewLegal.Value ? UrbanPalette.Legal : UrbanPalette.Risk, 0.68f)
                : hovered
                    ? Color.Lerp(baseColor, UrbanPalette.Legal, 0.22f)
                    : baseColor;

            UrbanVisualFactory.ApplyColor(floorRenderer, displayColor);
            if (!dragging)
            {
                visualRoot.localPosition = restingPosition;
                visualRoot.localRotation = restingRotation;
            }
        }
    }
}
