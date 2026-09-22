using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class RoomLayoutEditorController : MonoBehaviour
    {
        private readonly Dictionary<string, RoomView> views = new();
        private readonly List<GameObject> fixedMarkers = new();

        private RoomLayoutModel model;
        private Dictionary<string, RoomPlacement> snapshot;
        private GameRuntimeController runtime;
        private BoardCameraController boardCamera;
        private Camera worldCamera;
        private Transform mapRoot;
        private GameObject trayRoot;
        private float cellSize;
        private RoomView selectedRoom;
        private bool dragging;
        private bool pendingTray;
        private bool pendingLegal;
        private int pendingColumn;
        private int pendingRow;
        private int dragOriginColumn;
        private int dragOriginRow;
        private Vector3 dragGrabOffset;
        private string pendingSwapRoomId;
        private string guidedTargetRoomId;
        private Func<int, bool> trySpendResourcePoints;
        private Func<float> currentResourceBalance;

        public event Action StateChanged;
        public event Action LayoutConfirmed;
        public event Action<IReadOnlyList<string>> RoomsMoved;
        public event Action<bool> TrayStateChanged;

        public bool IsEditing { get; private set; }
        public bool CanConfirm => IsEditing && model != null && model.IsCompleteAndLegal() &&
                                  HasEnoughResourcesForPendingLayout;
        public int PendingMovementCost => CalculatePendingMovementCost();
        public int TotalRoomsMoved { get; private set; }
        public bool HasEnoughResourcesForPendingLayout =>
            currentResourceBalance == null || currentResourceBalance() + 0.0001f >= PendingMovementCost;
        public bool CanRotateSelected => IsEditing && selectedRoom != null &&
                                         model.Get(selectedRoom.Spec.Id).CanRotate &&
                                         selectedRoom.Spec.Movable;
        public bool TrayOccupied => model != null && model.TrayOccupied;
        public RoomSpec TrayRoomSpec
        {
            get
            {
                if (model == null || string.IsNullOrEmpty(model.TrayRoomId))
                {
                    return null;
                }

                return views.TryGetValue(model.TrayRoomId, out var view) ? view.Spec : null;
            }
        }

        public void SetGuidedTargetRoom(string roomId)
        {
            guidedTargetRoomId = string.IsNullOrWhiteSpace(roomId) ? null : roomId;
            foreach (var view in views.Values)
            {
                view.SetSelected(IsEditing && view.Spec.Id == guidedTargetRoomId);
            }
            NotifyStateChanged();
        }
        public string StatusText => !IsEditing
            ? string.Empty
            : TrayOccupied
                ? "临时托盘已占用 · 请放回地图后确认"
                : model != null && model.IsCompleteAndLegal() && !HasEnoughResourcesForPendingLayout
                    ? $"资源点不足 · 还差 {Mathf.CeilToInt(PendingMovementCost - currentResourceBalance())}"
                    : CanConfirm
                        ? PendingMovementCost > 0
                            ? $"可以确认 · 消耗 {PendingMovementCost}"
                            : "49 格布局完整，可以确认"
                        : "布局尚未完整";

        public IReadOnlyList<RoomPlacementData> ExportLayout()
        {
            return model.ExportData();
        }

        public bool RestoreLayout(IEnumerable<RoomPlacementData> savedPlacements)
        {
            if (model == null || !model.TryRestore(savedPlacements))
            {
                return false;
            }

            ApplyAllPlacements();
            snapshot = model.CaptureSnapshot();
            NotifyStateChanged();
            return true;
        }

        public void BindResourceEconomy(
            Func<int, bool> spendResourcePoints,
            Func<float> balanceProvider)
        {
            trySpendResourcePoints = spendResourcePoints;
            currentResourceBalance = balanceProvider;
            NotifyStateChanged();
        }

        public void ResetToInitialLayout()
        {
            IsEditing = false;
            dragging = false;
            selectedRoom = null;
            trayRoot.SetActive(false);
            foreach (var view in views.Values)
            {
                view.SetSelected(false);
                view.SetLayoutEditing(false);
            }

            foreach (var marker in fixedMarkers)
            {
                marker.SetActive(false);
            }

            model = new RoomLayoutModel(RoomLayoutData.All);
            TotalRoomsMoved = 0;
            snapshot = model.CaptureSnapshot();
            ApplyAllPlacements();
            LayoutConfirmed?.Invoke();
            NotifyStateChanged();
        }

        public void Initialize(
            IEnumerable<RoomView> roomViews,
            GameRuntimeController runtimeController,
            BoardCameraController cameraController,
            Camera camera,
            Material surfaceMaterial,
            HideFlags hideFlags,
            float gridCellSize)
        {
            runtime = runtimeController;
            boardCamera = cameraController;
            worldCamera = camera;
            cellSize = gridCellSize;
            model = new RoomLayoutModel(RoomLayoutData.All);

            foreach (var view in roomViews)
            {
                views[view.Spec.Id] = view;
                view.DragStarted += HandleDragStarted;
                view.Dragged += HandleDragging;
                view.DragEnded += HandleDragEnded;
                mapRoot ??= view.VisualRoot.parent;
            }

            BuildTray(surfaceMaterial, hideFlags);
            BuildFixedMarkers(hideFlags);
            ApplyAllPlacements();
        }

        private void Update()
        {
            if (IsEditing && Input.GetKeyDown(KeyCode.R))
            {
                RotateSelected();
            }
        }

        private void OnDestroy()
        {
            foreach (var view in views.Values)
            {
                if (view == null)
                {
                    continue;
                }

                view.DragStarted -= HandleDragStarted;
                view.Dragged -= HandleDragging;
                view.DragEnded -= HandleDragEnded;
            }
        }

        public void EnterEditing()
        {
            if (IsEditing)
            {
                return;
            }

            boardCamera?.ReturnToOverviewIfNeeded();
            snapshot = model.CaptureSnapshot();
            IsEditing = true;
            trayRoot.SetActive(true);
            runtime.SetLayoutEditing(true);
            foreach (var view in views.Values)
            {
                view.SetLayoutEditing(true);
                view.SetSelected(view.Spec.Id == guidedTargetRoomId);
            }
            foreach (var marker in fixedMarkers)
            {
                marker.SetActive(true);
            }

            NotifyStateChanged();
        }

        public void ConfirmEditing()
        {
            if (!CanConfirm)
            {
                return;
            }

            var movementCost = PendingMovementCost;
            if (movementCost > 0 && trySpendResourcePoints != null &&
                !trySpendResourcePoints(movementCost))
            {
                NotifyStateChanged();
                return;
            }

            var movedRoomIds = GetChangedRoomIds();
            snapshot = model.CaptureSnapshot();
            ExitEditing();
            if (movedRoomIds.Count > 0)
            {
                TotalRoomsMoved += movedRoomIds.Count;
                RoomsMoved?.Invoke(movedRoomIds);
            }
            LayoutConfirmed?.Invoke();
        }

        public void RestoreMovementCount(int totalRoomsMoved)
        {
            TotalRoomsMoved = Mathf.Max(0, totalRoomsMoved);
        }

        private int CalculatePendingMovementCost()
        {
            if (model == null || snapshot == null)
            {
                return 0;
            }

            var cost = 0;
            foreach (var pair in snapshot)
            {
                var current = model.Get(pair.Key);
                var previous = pair.Value;
                if (current.InTray || current.Column == previous.Column && current.Row == previous.Row)
                {
                    continue;
                }

                if (views.TryGetValue(pair.Key, out var view) && view.Spec.Movable)
                {
                    cost += ResourceEconomyModel.RoomMovementCost(view.Spec.CellCount);
                }
            }

            return cost;
        }

        private List<string> GetChangedRoomIds()
        {
            var changed = new List<string>();
            if (model == null || snapshot == null)
            {
                return changed;
            }

            foreach (var pair in snapshot)
            {
                var current = model.Get(pair.Key);
                var previous = pair.Value;
                if (current.Column != previous.Column ||
                    current.Row != previous.Row ||
                    current.QuarterTurns != previous.QuarterTurns)
                {
                    changed.Add(pair.Key);
                }
            }

            return changed;
        }

        public void CancelEditing()
        {
            if (!IsEditing)
            {
                return;
            }

            if (snapshot != null)
            {
                model.Restore(snapshot);
                ApplyAllPlacements();
            }

            ExitEditing();
        }

        public void RotateSelected()
        {
            if (!CanRotateSelected)
            {
                return;
            }

            if (model.TryRotate(selectedRoom.Spec.Id))
            {
                ApplyPlacement(selectedRoom);
            }
            else
            {
                selectedRoom.SetDragPreview(false);
                selectedRoom.RestoreRestingPlacement();
            }

            NotifyStateChanged();
        }

        private void ExitEditing()
        {
            ClearSwapPreview();
            IsEditing = false;
            dragging = false;
            selectedRoom = null;
            guidedTargetRoomId = null;
            trayRoot.SetActive(false);
            foreach (var view in views.Values)
            {
                view.SetSelected(false);
                view.SetLayoutEditing(false);
            }
            foreach (var marker in fixedMarkers)
            {
                marker.SetActive(false);
            }

            runtime.SetLayoutEditing(false);
            NotifyStateChanged();
        }

        private void HandleDragStarted(RoomView view, Vector2 screenPosition)
        {
            if (!IsEditing || !view.Spec.Movable ||
                guidedTargetRoomId != null && view.Spec.Id != guidedTargetRoomId)
            {
                return;
            }

            foreach (var room in views.Values)
            {
                room.SetSelected(room == view);
            }

            selectedRoom = view;
            dragging = true;
            var placement = model.Get(view.Spec.Id);
            dragOriginColumn = placement.Column;
            dragOriginRow = placement.Row;
            dragGrabOffset = Vector3.zero;
            if (TryScreenToBoard(screenPosition, out var localPoint))
            {
                dragGrabOffset = view.VisualRoot.localPosition - localPoint;
                dragGrabOffset.y = 0f;
            }
            UpdateDragPreview(view, screenPosition);
            NotifyStateChanged();
        }

        private void HandleDragging(RoomView view, Vector2 screenPosition)
        {
            if (!IsEditing || !dragging || view != selectedRoom)
            {
                return;
            }

            UpdateDragPreview(view, screenPosition);
        }

        private void HandleDragEnded(RoomView view, Vector2 screenPosition)
        {
            if (!IsEditing || !dragging || view != selectedRoom)
            {
                return;
            }

            UpdateDragPreview(view, screenPosition);
            dragging = false;
            var placement = model.Get(view.Spec.Id);
            var swapRoomId = pendingSwapRoomId;
            var applied = pendingTray
                ? pendingLegal && model.TryMoveToTray(view.Spec.Id)
                : pendingLegal && !string.IsNullOrEmpty(swapRoomId)
                    ? model.TrySwap(view.Spec.Id, swapRoomId)
                    : pendingLegal && model.TryPlace(
                        view.Spec.Id,
                        pendingColumn,
                        pendingRow,
                        placement.QuarterTurns);

            ClearSwapPreview();

            if (!applied)
            {
                view.RestoreRestingPlacement();
            }
            else
            {
                ApplyPlacement(view);
                if (!string.IsNullOrEmpty(swapRoomId) && views.TryGetValue(swapRoomId, out var swappedView))
                {
                    ApplyPlacement(swappedView);
                }
                TrayStateChanged?.Invoke(model.TrayOccupied);
            }

            NotifyStateChanged();
        }

        private void UpdateDragPreview(RoomView view, Vector2 screenPosition)
        {
            ClearSwapPreview();
            if (!TryScreenToBoard(screenPosition, out var localPoint))
            {
                pendingLegal = false;
                view.SetDragPreview(false);
                return;
            }

            var placement = model.Get(view.Spec.Id);
            var desiredCenter = localPoint + dragGrabOffset;
            pendingTray = IsPointInsideTray(desiredCenter);
            if (pendingTray)
            {
                pendingLegal = !model.TrayOccupied || model.TrayRoomId == view.Spec.Id;
                view.SetDraggedLocalPosition(TrayRoomPosition(placement));
                view.SetDragPreview(pendingLegal);
                return;
            }

            WorldToGridCandidate(desiredCenter, placement, out pendingColumn, out pendingRow);
            pendingLegal = model.CanPlace(
                view.Spec.Id,
                pendingColumn,
                pendingRow,
                placement.QuarterTurns);
            if (!pendingLegal)
            {
                pendingSwapRoomId = FindSwapTarget(
                    view.Spec.Id,
                    pendingColumn,
                    pendingRow,
                    placement.Width,
                    placement.Height);
                pendingLegal = !string.IsNullOrEmpty(pendingSwapRoomId) &&
                               model.CanSwap(view.Spec.Id, pendingSwapRoomId);
                if (pendingLegal && views.TryGetValue(pendingSwapRoomId, out var swapView))
                {
                    var swapPlacement = model.Get(pendingSwapRoomId);
                    swapView.SetSwapPreviewLocalPosition(GridToLocal(
                        dragOriginColumn,
                        dragOriginRow,
                        swapPlacement.Width,
                        swapPlacement.Height));
                }
            }
            view.SetDraggedLocalPosition(GridToLocal(pendingColumn, pendingRow, placement.Width, placement.Height));
            view.SetDragPreview(pendingLegal);
        }

        private string FindSwapTarget(string draggedRoomId, int column, int row, int width, int height)
        {
            foreach (var candidate in model.All)
            {
                if (candidate.Id == draggedRoomId || candidate.InTray ||
                    candidate.Column != column || candidate.Row != row ||
                    candidate.Width != width || candidate.Height != height ||
                    !model.CanMove(candidate.Id))
                {
                    continue;
                }

                return candidate.Id;
            }

            return null;
        }

        private void ClearSwapPreview()
        {
            if (!string.IsNullOrEmpty(pendingSwapRoomId) &&
                views.TryGetValue(pendingSwapRoomId, out var swapView))
            {
                swapView.ClearSwapPreview();
            }

            pendingSwapRoomId = null;
        }

        private bool TryScreenToBoard(Vector2 screenPosition, out Vector3 localPoint)
        {
            var ray = worldCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, mapRoot.position);
            if (!plane.Raycast(ray, out var distance))
            {
                localPoint = default;
                return false;
            }

            localPoint = mapRoot.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }

        private void WorldToGridCandidate(
            Vector3 localPoint,
            RoomPlacement placement,
            out int column,
            out int row)
        {
            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            column = Mathf.RoundToInt((localPoint.x + halfBoard) / cellSize - placement.Width * 0.5f);
            row = Mathf.RoundToInt((halfBoard - localPoint.z) / cellSize - placement.Height * 0.5f);
        }

        private Vector3 GridToLocal(int column, int row, int width, int height)
        {
            var x = (column + width * 0.5f - RoomLayoutData.GridSize * 0.5f) * cellSize;
            var z = (RoomLayoutData.GridSize * 0.5f - row - height * 0.5f) * cellSize;
            return new Vector3(x, 0f, z);
        }

        private bool IsPointInsideTray(Vector3 localPoint)
        {
            var offset = localPoint - trayRoot.transform.localPosition;
            return Mathf.Abs(offset.x) <= cellSize * 1.02f && Mathf.Abs(offset.z) <= cellSize * 1.02f;
        }

        private Vector3 TrayRoomPosition(RoomPlacement placement)
        {
            return trayRoot.transform.localPosition + new Vector3(0f, 0.28f, 0f);
        }

        private void ApplyAllPlacements()
        {
            foreach (var view in views.Values)
            {
                ApplyPlacement(view);
            }
        }

        private void ApplyPlacement(RoomView view)
        {
            var placement = model.Get(view.Spec.Id);
            var position = placement.InTray
                ? TrayRoomPosition(placement)
                : GridToLocal(placement.Column, placement.Row, placement.Width, placement.Height);
            view.ApplyPlacement(position, placement.QuarterTurns);
        }

        private void BuildTray(Material surfaceMaterial, HideFlags hideFlags)
        {
            trayRoot = new GameObject("Temporary Holding Tray")
            {
                hideFlags = hideFlags
            };
            trayRoot.transform.SetParent(mapRoot, false);
            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            trayRoot.transform.localPosition = new Vector3(-(halfBoard + cellSize * 0.86f), 0f, 0f);

            var traySize = cellSize * 1.58f;
            var trayColor = Color.Lerp(UrbanPalette.Board, UrbanPalette.Legal, 0.24f);
            var rimColor = Color.Lerp(UrbanPalette.Boundary, UrbanPalette.Legal, 0.46f);

            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube,
                "Tray Surface",
                trayRoot.transform,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(traySize, 0.10f, traySize),
                trayColor,
                surfaceMaterial,
                true,
                hideFlags);

            const float rimThickness = 0.10f;
            var rimHeight = 0.16f;
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube, "Tray Rim North", trayRoot.transform,
                new Vector3(0f, 0.13f, traySize * 0.5f),
                new Vector3(traySize + rimThickness, rimHeight, rimThickness),
                rimColor, surfaceMaterial, true, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube, "Tray Rim South", trayRoot.transform,
                new Vector3(0f, 0.13f, -traySize * 0.5f),
                new Vector3(traySize + rimThickness, rimHeight, rimThickness),
                rimColor, surfaceMaterial, true, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube, "Tray Rim West", trayRoot.transform,
                new Vector3(-traySize * 0.5f, 0.13f, 0f),
                new Vector3(rimThickness, rimHeight, traySize + rimThickness),
                rimColor, surfaceMaterial, true, hideFlags);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube, "Tray Rim East", trayRoot.transform,
                new Vector3(traySize * 0.5f, 0.13f, 0f),
                new Vector3(rimThickness, rimHeight, traySize + rimThickness),
                rimColor, surfaceMaterial, true, hideFlags);

            var labelObject = new GameObject("Tray Symbol", typeof(TextMesh))
            {
                hideFlags = hideFlags
            };
            labelObject.transform.SetParent(trayRoot.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var label = labelObject.GetComponent<TextMesh>();
            label.font = UrbanFontResolver.GetFont();
            label.fontSize = 72;
            label.characterSize = 0.027f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = UrbanPalette.LightText;
            label.text = "托盘\n1×1";
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            trayRoot.SetActive(false);
        }

        private void BuildFixedMarkers(HideFlags hideFlags)
        {
            foreach (var view in views.Values)
            {
                if (view.Spec.Movable)
                {
                    continue;
                }

                var markerObject = new GameObject("Fixed Module Marker", typeof(TextMesh))
                {
                    hideFlags = hideFlags
                };
                markerObject.transform.SetParent(view.VisualRoot, false);
                markerObject.transform.localPosition = new Vector3(0f, 1.58f, 0f);
                markerObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var marker = markerObject.GetComponent<TextMesh>();
                marker.font = UrbanFontResolver.GetFont();
                marker.fontSize = 72;
                marker.characterSize = 0.026f;
                marker.anchor = TextAnchor.MiddleCenter;
                marker.alignment = TextAlignment.Center;
                marker.color = new Color(0.95f, 0.90f, 0.76f);
                marker.text = "▣";
                markerObject.GetComponent<MeshRenderer>().sharedMaterial = marker.font.material;
                markerObject.SetActive(false);
                fixedMarkers.Add(markerObject);
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
