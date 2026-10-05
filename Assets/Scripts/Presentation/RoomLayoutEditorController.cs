using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public readonly struct RoomLayoutImpactPreview
    {
        public RoomLayoutImpactPreview(int movementCost,
            ResidentCommutePreview before, ResidentCommutePreview after,
            int beforeParkEdges, int afterParkEdges,
            int beforeSeedCapacity, int afterSeedCapacity,
            int beforeFullPopulationWasteOverflow, int afterFullPopulationWasteOverflow,
            int marketDay, string marketRoomId,
            int beforeMarketOverflow, int afterMarketOverflow,
            IReadOnlyList<string> changedRoomIds,
            int seedForecastDay = 1,
            int beforeShelterPairs = 0, int afterShelterPairs = 0,
            int recoveredShelterPairs = 0, int movedShrubs = 0,
            int shelterRecoveryDay = 0,
            int beforeAnimalConnections = -1, int afterAnimalConnections = -1,
            AnimalFoodAccessSnapshot? beforeFoodAccess = null,
            AnimalFoodAccessSnapshot? afterFoodAccess = null,
            int beforeBufferedGarages = 0, int afterBufferedGarages = 0,
            int beforeGreenCells = -1, int afterGreenCells = -1,
            int livingPigeons = -1,
            int beforePigeonSeedMealCeiling = -1, int afterPigeonSeedMealCeiling = -1)
        {
            MovementCost = movementCost;
            Before = before;
            After = after;
            BeforeParkEdges = beforeParkEdges;
            AfterParkEdges = afterParkEdges;
            BeforeSeedCapacity = beforeSeedCapacity;
            AfterSeedCapacity = afterSeedCapacity;
            BeforeFullPopulationWasteOverflow = beforeFullPopulationWasteOverflow;
            AfterFullPopulationWasteOverflow = afterFullPopulationWasteOverflow;
            MarketDay = marketDay;
            MarketRoomId = marketRoomId;
            BeforeMarketOverflow = beforeMarketOverflow;
            AfterMarketOverflow = afterMarketOverflow;
            ChangedRoomIds = changedRoomIds ?? Array.Empty<string>();
            SeedForecastDay = seedForecastDay;
            BeforeShelterPairs = beforeShelterPairs;
            AfterShelterPairs = afterShelterPairs;
            RecoveredShelterPairs = recoveredShelterPairs;
            MovedShrubs = movedShrubs;
            ShelterRecoveryDay = shelterRecoveryDay;
            BeforeAnimalConnections = beforeAnimalConnections;
            AfterAnimalConnections = afterAnimalConnections;
            BeforeFoodAccess = beforeFoodAccess;
            AfterFoodAccess = afterFoodAccess;
            BeforeBufferedGarages = beforeBufferedGarages;
            AfterBufferedGarages = afterBufferedGarages;
            BeforeGreenCells = beforeGreenCells;
            AfterGreenCells = afterGreenCells;
            LivingPigeons = livingPigeons;
            BeforePigeonSeedMealCeiling = beforePigeonSeedMealCeiling;
            AfterPigeonSeedMealCeiling = afterPigeonSeedMealCeiling;
        }

        public int MovementCost { get; }
        public ResidentCommutePreview Before { get; }
        public ResidentCommutePreview After { get; }
        public int BeforeParkEdges { get; }
        public int AfterParkEdges { get; }
        public int BeforeSeedCapacity { get; }
        public int AfterSeedCapacity { get; }
        public int BeforeFullPopulationWasteOverflow { get; }
        public int AfterFullPopulationWasteOverflow { get; }
        public int MarketDay { get; }
        public string MarketRoomId { get; }
        public int BeforeMarketOverflow { get; }
        public int AfterMarketOverflow { get; }
        public IReadOnlyList<string> ChangedRoomIds { get; }
        public int SeedForecastDay { get; }
        public int BeforeShelterPairs { get; }
        public int AfterShelterPairs { get; }
        public int RecoveredShelterPairs { get; }
        public int MovedShrubs { get; }
        public int ShelterRecoveryDay { get; }
        public int BeforeAnimalConnections { get; }
        public int AfterAnimalConnections { get; }
        public AnimalFoodAccessSnapshot? BeforeFoodAccess { get; }
        public AnimalFoodAccessSnapshot? AfterFoodAccess { get; }
        public int BeforeBufferedGarages { get; }
        public int AfterBufferedGarages { get; }
        public int BeforeGreenCells { get; }
        public int AfterGreenCells { get; }
        public int LivingPigeons { get; }
        public int BeforePigeonSeedMealCeiling { get; }
        public int AfterPigeonSeedMealCeiling { get; }
    }

    public sealed class RoomLayoutEditorController : MonoBehaviour
    {
        public const int MaximumRoomsPerFreeRearrangement = 3;

        private readonly Dictionary<string, RoomView> views = new();
        private readonly List<GameObject> fixedMarkers = new();

        private RoomLayoutModel model;
        private Dictionary<string, RoomPlacement> snapshot;
        private GameRuntimeController runtime;
        private BoardCameraController boardCamera;
        private Camera worldCamera;
        private Transform mapRoot;
        private GameObject trayRoot;
        private TextMesh trayLabel;
        private float cellSize;
        private RoomView selectedRoom;
        private bool dragging;
        private bool pendingTray;
        private bool pendingLegal;
        private int pendingColumn;
        private int pendingRow;
        private int pendingQuarterTurns;
        private int dragOriginColumn;
        private int dragOriginRow;
        private Vector2 lastDragScreenPosition;
        private Vector3 dragGrabOffset;
        private string pendingSwapRoomId;
        private readonly List<RoomPlacementData> pendingSingleSwapPlacements = new();
        private string guidedTargetRoomId;
        private bool guidedTrayVisited;
        private int lastConfirmedMovementDay;
        private int lastConfirmedPlanningDay;
        private ShrubShelterModel shrubShelter;
        private OakTreeGrowthModel oakTrees;
        private AnimalPopulationController animalPopulation;
        private IReadOnlyDictionary<PigeonDemoAgent, string> pigeonHomes;

        public event Action StateChanged;
        public event Action LayoutConfirmed;
        public event Action LayoutRestored;
        public event Action<IReadOnlyList<string>> RoomsMoved;
        public event Action<bool> TrayStateChanged;

        public bool IsEditing { get; private set; }
        public bool CanConfirm => IsEditing && model != null && model.IsCompleteAndLegal() &&
                                  CanConfirmFreeRearrangement &&
                                  (guidedTargetRoomId == null || GuidedPracticeReadyToConfirm);
        public bool GuidedPracticeReadyToConfirm =>
            guidedTargetRoomId != null && guidedTrayVisited && !TrayOccupied &&
            model != null && model.IsCompleteAndLegal() && GetChangedRoomIds().Count == 0;
        public Transform TrayTransform => trayRoot != null ? trayRoot.transform : null;
        public int PendingMovementCost => 0;
        public int TotalRoomsMoved { get; private set; }
        public int LastConfirmedMovementDay => lastConfirmedMovementDay;
        public int LastConfirmedPlanningDay => lastConfirmedPlanningDay;
        public bool FreeRearrangementAvailable => runtime == null ||
                                                  lastConfirmedMovementDay != runtime.Clock.DayNumber &&
                                                  runtime.CanTakeDailyAction;
        public bool CanConfirmFreeRearrangement =>
            GetChangedRoomIds().Count <= MaximumRoomsPerFreeRearrangement &&
            (GetChangedRoomIds().Count == 0 || FreeRearrangementAvailable);
        public bool CanRotateSelected => IsEditing && selectedRoom != null &&
                                         model.Get(selectedRoom.Spec.Id).CanRotate &&
                                         selectedRoom.Spec.Movable;
        public bool TrayOccupied => model != null && model.TrayOccupied;
        public bool IsDragging => dragging;
        public bool IsGuidedPractice => guidedTargetRoomId != null;
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
            var nextId = string.IsNullOrWhiteSpace(roomId) ? null : roomId;
            if (guidedTargetRoomId != nextId)
            {
                guidedTrayVisited = false;
            }
            guidedTargetRoomId = nextId;
            foreach (var view in views.Values)
            {
                view.SetSelected(IsEditing && view.Spec.Id == guidedTargetRoomId);
            }
            NotifyStateChanged();
        }
        public string StatusText => runtime != null && runtime.Language == InterfaceLanguage.English
            ? EnglishStatusText
            : ChineseStatusText;

        private string EnglishStatusText => !IsEditing
            ? string.Empty
            : dragging && pendingSingleSwapPlacements.Count > 0
                ? $"Release to swap {pendingSingleSwapPlacements.Count + 1} rooms"
                : dragging && pendingLegal && pendingTray
                    ? "Release into tray"
                : dragging && pendingLegal
                    ? !string.IsNullOrEmpty(pendingSwapRoomId)
                        ? "Release to swap rooms"
                        : "Release to place room"
                : dragging && !pendingLegal
                    ? "Invalid target"
                : TrayOccupied
                    ? "Return tray room to board"
                : guidedTargetRoomId != null && !guidedTrayVisited
                    ? "Practice: use left tray"
                : guidedTargetRoomId != null && GetChangedRoomIds().Count > 0
                    ? "Practice: return the room"
                : guidedTargetRoomId != null && GuidedPracticeReadyToConfirm
                    ? "Practice: ready to confirm"
                : GetChangedRoomIds().Count > MaximumRoomsPerFreeRearrangement
                    ? $"Max {MaximumRoomsPerFreeRearrangement} rooms per edit"
                : GetChangedRoomIds().Count > 0 && !FreeRearrangementAvailable
                    ? runtime != null && runtime.TodayAction == DailyActionKind.Feed
                        ? "Fed today · rearrange tomorrow"
                        : "Already adjusted today"
                : CanConfirm
                    ? GetChangedRoomIds().Count > 0
                        ? "Ready · free today"
                        : runtime != null && runtime.HasDailyAction
                            ? "Today's action complete"
                            : "Move a room or feed to advance"
                    : "Layout incomplete";

        private string ChineseStatusText => !IsEditing
            ? string.Empty
            : dragging && pendingSingleSwapPlacements.Count > 0
                ? $"松手交换 {pendingSingleSwapPlacements.Count + 1} 间房 · " +
                  (IsGuidedPractice ? "练习不占次数" : "今日调整免费")
                : dragging && pendingLegal && pendingTray
                    ? "松手放入托盘 · 放回地图后可确认"
                : dragging && pendingLegal
                    ? !string.IsNullOrEmpty(pendingSwapRoomId)
                        ? "松手交换房间 · 查看布局变化预览"
                        : "松手放置房间 · 查看布局变化预览"
                : dragging && !pendingLegal
                    ? "目标无效 · 请避开固定房间与重叠区域"
            : TrayOccupied
                ? "临时托盘已占用 · 请放回地图后确认"
                : guidedTargetRoomId != null && !guidedTrayVisited
                    ? "引导练习 · 先放入左侧托盘"
                    : guidedTargetRoomId != null && GetChangedRoomIds().Count > 0
                        ? "引导练习 · 请放回原位"
                        : guidedTargetRoomId != null && GuidedPracticeReadyToConfirm
                            ? "可以确认 · 练习不占次数"
                : GetChangedRoomIds().Count > MaximumRoomsPerFreeRearrangement
                    ? $"一次最多调整 {MaximumRoomsPerFreeRearrangement} 间房 · 请分天规划"
                : GetChangedRoomIds().Count > 0 && !FreeRearrangementAvailable
                    ? runtime != null && runtime.TodayAction == DailyActionKind.Feed
                        ? "今日已投喂 · 明天才能交换房间"
                        : "今日已完成调整 · 明天可再次挪动"
                    : CanConfirm
                        ? GetChangedRoomIds().Count > 0
                            ? "可以确认 · 今日调整免费"
                            : runtime != null && runtime.HasDailyAction
                                ? "今日行动已完成 · 可确认检查"
                                : "可确认检查 · 今日仍需交换或投喂"
                        : "布局尚未完整";

        public IReadOnlyList<RoomPlacementData> ExportLayout()
        {
            return model.ExportData();
        }

        public void BindShrubShelter(ShrubShelterModel shelter)
        {
            shrubShelter = shelter;
        }

        public void BindOakTreeGrowth(OakTreeGrowthModel trees)
        {
            oakTrees = trees;
        }

        public void BindAnimalPopulation(AnimalPopulationController population)
        {
            animalPopulation = population;
        }

        public void BindPigeonHomes(IReadOnlyDictionary<PigeonDemoAgent, string> homes)
        {
            pigeonHomes = homes;
        }

        public int ForecastCurrentPigeonSeedMealCeiling(RoomNavigationMap navigation,
            int dayNumber)
        {
            if (pigeonHomes == null) return -1;
            var livingByHome = pigeonHomes.Where(pair => pair.Key != null && pair.Key.IsAlive)
                .GroupBy(pair => pair.Value, StringComparer.Ordinal)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()));
            return HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                navigation, dayNumber, livingByHome);
        }

        // Project the actual drop operation on a copy. No live room, resident,
        // resource balance, or saved layout is changed by this preview.
        public bool TryGetImpactPreview(ResidentPopulationModel population,
            out RoomLayoutImpactPreview impact)
        {
            impact = default;
            if (!IsEditing || IsGuidedPractice || model == null || snapshot == null || population == null ||
                dragging && (!pendingLegal || pendingTray))
            {
                return false;
            }

            var projected = new RoomLayoutModel(RoomLayoutData.All);
            projected.Restore(model.CaptureSnapshot());
            if (dragging)
            {
                var roomId = selectedRoom?.Spec?.Id;
                if (string.IsNullOrEmpty(roomId))
                {
                    return false;
                }

                var applied = !string.IsNullOrEmpty(pendingSwapRoomId)
                    ? projected.TrySwap(roomId, pendingSwapRoomId, pendingQuarterTurns)
                    : pendingSingleSwapPlacements.Count > 0
                        ? projected.TrySwapWithSingles(roomId, pendingColumn, pendingRow, pendingQuarterTurns)
                        : projected.TryPlace(roomId, pendingColumn, pendingRow, pendingQuarterTurns);
                if (!applied)
                {
                    return false;
                }
            }

            if (!projected.IsCompleteAndLegal())
            {
                return false;
            }

            var changedRoomIds = new List<string>();
            foreach (var pair in snapshot)
            {
                var candidate = projected.Get(pair.Key);
                if (candidate.Column != pair.Value.Column ||
                    candidate.Row != pair.Value.Row ||
                    candidate.QuarterTurns != pair.Value.QuarterTurns)
                {
                    changedRoomIds.Add(pair.Key);
                }
            }
            if (changedRoomIds.Count == 0)
            {
                return false;
            }

            var original = new RoomLayoutModel(RoomLayoutData.All);
            original.Restore(snapshot);
            var originalNavigation = new RoomNavigationMap(original.ExportData(), RoomLayoutData.All, cellSize);
            var projectedNavigation = new RoomNavigationMap(projected.ExportData(), RoomLayoutData.All, cellSize);
            var originalPedestrianNavigation = new RoomNavigationMap(
                original.ExportData(), RoomLayoutData.All, cellSize, humanRoadsOnly: true);
            var projectedPedestrianNavigation = new RoomNavigationMap(
                projected.ExportData(), RoomLayoutData.All, cellSize, humanRoadsOnly: true);
            var market = runtime.Mode == GameMode.Sandbox
                ? NeighborhoodMarketSchedule.NextUnprocessed(runtime.Clock.TotalSeconds)
                : default;
            var seedForecastDay = runtime.Mode == GameMode.Sandbox
                ? runtime.Clock.DayNumber + 1
                : 1;
            var originalAnimalNavigation = new RoomNavigationMap(
                original.ExportData(), RoomLayoutData.All, cellSize,
                true, seedForecastDay);
            var projectedAnimalNavigation = new RoomNavigationMap(
                projected.ExportData(), RoomLayoutData.All, cellSize,
                true, seedForecastDay);
            var originalTonightNavigation = new RoomNavigationMap(
                original.ExportData(), RoomLayoutData.All, cellSize,
                true, runtime.Clock.DayNumber);
            var projectedTonightNavigation = new RoomNavigationMap(
                projected.ExportData(), RoomLayoutData.All, cellSize,
                true, runtime.Clock.DayNumber);
            var shelterRecoveryDay = runtime.Clock.DayNumber + ShrubShelterModel.RecoveryDays;
            var projectedRecoveryNavigation = new RoomNavigationMap(
                projected.ExportData(), RoomLayoutData.All, cellSize,
                true, shelterRecoveryDay);
            var movedShrubs = new HashSet<string>(changedRoomIds.FindAll(id =>
                RoomLayoutData.All.Any(room => room.Id == id && room.Type == RoomType.ShrubHabitat)),
                StringComparer.Ordinal);
            var shelterDay = runtime.Clock.DayNumber;
            var beforeFoodAccess = HabitatFoodNetworkModel.ForecastHabitatAccess(
                originalAnimalNavigation, originalNavigation, seedForecastDay, oakTrees);
            var afterFoodAccess = HabitatFoodNetworkModel.ForecastHabitatAccess(
                projectedAnimalNavigation, projectedNavigation, seedForecastDay,
                oakTrees, new HashSet<string>(changedRoomIds, StringComparer.Ordinal));
            var livingPigeonsByHome = pigeonHomes?.Where(pair => pair.Key != null && pair.Key.IsAlive)
                .GroupBy(pair => pair.Value, StringComparer.Ordinal)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .ToArray();
            impact = new RoomLayoutImpactPreview(
                0,
                population.PreviewCommute(originalPedestrianNavigation),
                population.PreviewCommute(projectedPedestrianNavigation),
                HabitatFoodNetworkModel.ParkLinkedPigeonHabitats(originalAnimalNavigation, seedForecastDay),
                HabitatFoodNetworkModel.ParkLinkedPigeonHabitats(projectedAnimalNavigation, seedForecastDay),
                HabitatFoodNetworkModel.TotalSeedCapacity(originalAnimalNavigation, seedForecastDay),
                HabitatFoodNetworkModel.TotalSeedCapacity(projectedAnimalNavigation, seedForecastDay),
                ForecastFullPopulationWasteOverflow(originalNavigation),
                ForecastFullPopulationWasteOverflow(projectedNavigation),
                market.DayNumber,
                market.RoomId,
                market.DayNumber > 0
                    ? ForecastFullPopulationWasteOverflow(originalNavigation, market.RoomId, market.ExtraWaste)
                    : 0,
                market.DayNumber > 0
                    ? ForecastFullPopulationWasteOverflow(projectedNavigation, market.RoomId, market.ExtraWaste)
                    : 0,
                changedRoomIds.ToArray(),
                seedForecastDay,
                shrubShelter?.ConnectedPairCount(originalTonightNavigation, shelterDay) ?? 0,
                shrubShelter?.ConnectedPairCount(projectedTonightNavigation, shelterDay, movedShrubs) ?? 0,
                shrubShelter?.ConnectedPairCount(projectedRecoveryNavigation,
                    shelterRecoveryDay) ?? 0,
                movedShrubs.Count,
                shelterRecoveryDay,
                originalAnimalNavigation.ConnectionCount,
                projectedAnimalNavigation.ConnectionCount,
                beforeFoodAccess, afterFoodAccess,
                CountBufferedGarages(originalNavigation),
                CountBufferedGarages(projectedNavigation),
                GreenNetworkModel.ConnectedCount(originalAnimalNavigation),
                GreenNetworkModel.ConnectedCount(projectedAnimalNavigation),
                animalPopulation?.LivingCount(WildlifeSpecies.Pigeon) ?? -1,
                livingPigeonsByHome == null ? -1 :
                    HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                        originalAnimalNavigation, seedForecastDay, livingPigeonsByHome),
                livingPigeonsByHome == null ? -1 :
                    HabitatFoodNetworkModel.ForecastPigeonSeedMealCeiling(
                        projectedAnimalNavigation, seedForecastDay, livingPigeonsByHome));
            return true;
        }

        private static int CountBufferedGarages(RoomNavigationMap navigation)
        {
            var buffers = new HashSet<string>(RoomLayoutData.All
                .Where(room => room.Type == RoomType.EcologicalBuffer)
                .Select(room => room.Id), StringComparer.Ordinal);
            return RoomLayoutData.All.Count(room => room.Type == RoomType.Garage &&
                navigation.NeighboursOf(room.Id).Any(buffers.Contains));
        }

        private static int ForecastFullPopulationWasteOverflow(RoomNavigationMap navigation,
            string extraProducerRoomId = null, int extraWaste = 0)
        {
            var forecast = new WasteManagementModel(navigation, RoomLayoutData.All);
            forecast.ProduceDailyWaste(
                ResidentPopulationModel.MaximumResidents,
                WasteManagementController.FoodShopCount,
                true);
            if (!string.IsNullOrEmpty(extraProducerRoomId) && extraWaste > 0)
            {
                forecast.RouteWaste(extraProducerRoomId, extraWaste);
            }
            return forecast.AffectedWasteRoomCount;
        }

        public bool RestoreLayout(IEnumerable<RoomPlacementData> savedPlacements)
        {
            if (model == null || !model.TryRestore(savedPlacements))
            {
                return false;
            }

            ApplyAllPlacements();
            snapshot = model.CaptureSnapshot();
            LayoutRestored?.Invoke();
            NotifyStateChanged();
            return true;
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
            guidedTargetRoomId = null;
            guidedTrayVisited = false;
            TotalRoomsMoved = 0;
            lastConfirmedMovementDay = 0;
            lastConfirmedPlanningDay = 0;
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
            runtime.BindLayoutEditor(this);
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
            if (IsEditing) RefreshTrayLabel();
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
            RefreshTrayLabel();
            runtime.SetLayoutEditing(true);
            foreach (var view in views.Values)
            {
                view.SetLayoutEditing(true, runtime.Clock.DayNumber +
                    (runtime.Mode == GameMode.Sandbox ? 1 : 0));
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

            var movedRoomIds = GetChangedRoomIds();
            var wasGuidedPractice = guidedTargetRoomId != null;
            snapshot = model.CaptureSnapshot();
            ExitEditing();
            if (!wasGuidedPractice)
                lastConfirmedPlanningDay = runtime.Clock.DayNumber;
            if (movedRoomIds.Count > 0)
            {
                lastConfirmedMovementDay = runtime.Clock.DayNumber;
                TotalRoomsMoved += movedRoomIds.Count;
                RoomsMoved?.Invoke(movedRoomIds);
            }
            LayoutConfirmed?.Invoke();
        }

        public void RestoreMovementCount(int totalRoomsMoved)
        {
            TotalRoomsMoved = Mathf.Max(0, totalRoomsMoved);
        }

        public void RestoreLastMovementDay(int dayNumber)
        {
            lastConfirmedMovementDay = Mathf.Max(0, dayNumber);
            NotifyStateChanged();
        }

        public void RestoreLastPlanningDay(int dayNumber)
        {
            lastConfirmedPlanningDay = Mathf.Max(0, dayNumber);
            NotifyStateChanged();
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

            if (dragging)
            {
                pendingQuarterTurns = (pendingQuarterTurns + 1) % 4;
                UpdateDragPreview(selectedRoom, lastDragScreenPosition);
            }
            else if (model.TryRotate(selectedRoom.Spec.Id))
            {
                ApplyPlacement(selectedRoom);
            }
            else
            {
                var placement = model.Get(selectedRoom.Spec.Id);
                var nextTurns = (placement.QuarterTurns + 1) % 4;
                if (model.TryPlanSwapWithSingles(
                        selectedRoom.Spec.Id, placement.Column, placement.Row, nextTurns, out var plan) &&
                    model.TrySwapWithSingles(
                        selectedRoom.Spec.Id, placement.Column, placement.Row, nextTurns))
                {
                    ApplyPlacement(selectedRoom);
                    for (var index = 1; index < plan.Count; index++)
                    {
                        if (views.TryGetValue(plan[index].id, out var displacedView))
                        {
                            ApplyPlacement(displacedView);
                        }
                    }
                }
                else
                {
                    selectedRoom.SetDragPreview(false);
                    selectedRoom.RestoreRestingPlacement();
                }
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
            guidedTrayVisited = false;
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
            pendingQuarterTurns = placement.QuarterTurns;
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

            var previousLegal = pendingLegal;
            var previousTray = pendingTray;
            var previousColumn = pendingColumn;
            var previousRow = pendingRow;
            var previousTurns = pendingQuarterTurns;
            var previousSwapId = pendingSwapRoomId;
            var previousSwapCount = pendingSingleSwapPlacements.Count;
            UpdateDragPreview(view, screenPosition);
            if (previousLegal != pendingLegal || previousTray != pendingTray ||
                previousColumn != pendingColumn || previousRow != pendingRow ||
                previousTurns != pendingQuarterTurns || previousSwapId != pendingSwapRoomId ||
                previousSwapCount != pendingSingleSwapPlacements.Count)
            {
                NotifyStateChanged();
            }
        }

        private void HandleDragEnded(RoomView view, Vector2 screenPosition)
        {
            if (!IsEditing || !dragging || view != selectedRoom)
            {
                return;
            }

            UpdateDragPreview(view, screenPosition);
            dragging = false;
            var swapRoomId = pendingSwapRoomId;
            var displacedSingleIds = new List<string>();
            foreach (var item in pendingSingleSwapPlacements)
            {
                displacedSingleIds.Add(item.id);
            }
            var applied = false;
            if (pendingLegal)
            {
                if (pendingTray)
                {
                    applied = model.TryMoveToTray(view.Spec.Id, pendingQuarterTurns);
                }
                else if (!string.IsNullOrEmpty(swapRoomId))
                {
                    applied = model.TrySwap(view.Spec.Id, swapRoomId, pendingQuarterTurns);
                }
                else if (displacedSingleIds.Count > 0)
                {
                    applied = model.TrySwapWithSingles(view.Spec.Id, pendingColumn, pendingRow, pendingQuarterTurns);
                }
                else
                {
                    applied = model.TryPlace(view.Spec.Id, pendingColumn, pendingRow, pendingQuarterTurns);
                }
            }

            ClearSwapPreview();

            if (!applied)
            {
                view.RestoreRestingPlacement();
            }
            else
            {
                ApplyPlacement(view);
                if (guidedTargetRoomId == view.Spec.Id && model.TrayOccupied &&
                    model.TrayRoomId == guidedTargetRoomId)
                {
                    guidedTrayVisited = true;
                }
                if (!string.IsNullOrEmpty(swapRoomId) && views.TryGetValue(swapRoomId, out var swappedView))
                {
                    ApplyPlacement(swappedView);
                }
                foreach (var displacedId in displacedSingleIds)
                {
                    if (views.TryGetValue(displacedId, out var displacedView))
                    {
                        ApplyPlacement(displacedView);
                    }
                }
                TrayStateChanged?.Invoke(model.TrayOccupied);
            }

            NotifyStateChanged();
        }

        private void UpdateDragPreview(RoomView view, Vector2 screenPosition)
        {
            lastDragScreenPosition = screenPosition;
            ClearSwapPreview();
            if (!TryScreenToBoard(screenPosition, out var localPoint))
            {
                pendingLegal = false;
                view.SetDragPreview(false);
                return;
            }

            var placement = model.Get(view.Spec.Id);
            var width = pendingQuarterTurns % 2 == 0 ? placement.BaseWidth : placement.BaseHeight;
            var height = pendingQuarterTurns % 2 == 0 ? placement.BaseHeight : placement.BaseWidth;
            var desiredCenter = localPoint + dragGrabOffset;
            pendingTray = IsPointInsideTray(desiredCenter);
            if (pendingTray)
            {
                pendingLegal = !model.TrayOccupied || model.TrayRoomId == view.Spec.Id;
                view.SetDragPreview(pendingLegal);
                view.SetDraggedPlacement(TrayRoomPosition(placement), pendingQuarterTurns);
                return;
            }

            WorldToGridCandidate(desiredCenter, width, height, out pendingColumn, out pendingRow);
            pendingLegal = model.CanPlace(
                view.Spec.Id,
                pendingColumn,
                pendingRow,
                pendingQuarterTurns);
            if (!pendingLegal)
            {
                pendingSwapRoomId = FindSwapTarget(
                    view.Spec.Id,
                    pendingColumn,
                    pendingRow,
                    width,
                    height);
                pendingLegal = !string.IsNullOrEmpty(pendingSwapRoomId) &&
                               model.CanSwap(view.Spec.Id, pendingSwapRoomId, pendingQuarterTurns);
                if (pendingLegal && views.TryGetValue(pendingSwapRoomId, out var swapView))
                {
                    var swapPlacement = model.Get(pendingSwapRoomId);
                    swapView.SetSwapPreviewLocalPosition(GridToLocal(
                        dragOriginColumn,
                        dragOriginRow,
                        swapPlacement.Width,
                        swapPlacement.Height));
                }
                if (!pendingLegal && model.TryPlanSwapWithSingles(
                        view.Spec.Id, pendingColumn, pendingRow, pendingQuarterTurns, out var exchangePlan))
                {
                    pendingLegal = true;
                    for (var index = 1; index < exchangePlan.Count; index++)
                    {
                        var displaced = exchangePlan[index];
                        pendingSingleSwapPlacements.Add(displaced);
                        if (views.TryGetValue(displaced.id, out var displacedView))
                        {
                            displacedView.SetSwapPreviewLocalPosition(GridToLocal(
                                displaced.column, displaced.row, 1, 1));
                        }
                    }
                }
            }
            view.SetDragPreview(pendingLegal);
            view.SetDraggedPlacement(GridToLocal(pendingColumn, pendingRow, width, height), pendingQuarterTurns);
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

            foreach (var item in pendingSingleSwapPlacements)
            {
                if (views.TryGetValue(item.id, out var displacedView))
                {
                    displacedView.ClearSwapPreview();
                }
            }

            pendingSwapRoomId = null;
            pendingSingleSwapPlacements.Clear();
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
            int width,
            int height,
            out int column,
            out int row)
        {
            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            column = Mathf.RoundToInt((localPoint.x + halfBoard) / cellSize - width * 0.5f);
            row = Mathf.RoundToInt((halfBoard - localPoint.z) / cellSize - height * 0.5f);
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
            label.characterSize = 0.075f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = UrbanPalette.LightText;
            trayLabel = label;
            RefreshTrayLabel();
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            trayRoot.SetActive(false);
        }

        private void RefreshTrayLabel()
        {
            if (trayLabel == null) return;
            var text = runtime != null && runtime.Language == InterfaceLanguage.Chinese
                ? "托盘\n1×1" : "TRAY\n1×1";
            if (trayLabel.text != text) trayLabel.text = text;
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
            if (IsEditing)
            {
                RefreshAnimalPassageConnections();
            }
            StateChanged?.Invoke();
        }

        private void RefreshAnimalPassageConnections()
        {
            if (model == null) return;

            var routeLayout = model;
            if (dragging)
            {
                if (!pendingLegal || selectedRoom?.Spec == null)
                {
                    ClearAnimalPassageConnections();
                    return;
                }

                routeLayout = new RoomLayoutModel(RoomLayoutData.All);
                routeLayout.Restore(model.CaptureSnapshot());
                var roomId = selectedRoom.Spec.Id;
                var applied = pendingTray
                    ? routeLayout.TryMoveToTray(roomId, pendingQuarterTurns)
                    : !string.IsNullOrEmpty(pendingSwapRoomId)
                        ? routeLayout.TrySwap(roomId, pendingSwapRoomId, pendingQuarterTurns)
                        : pendingSingleSwapPlacements.Count > 0
                            ? routeLayout.TrySwapWithSingles(roomId, pendingColumn, pendingRow, pendingQuarterTurns)
                            : routeLayout.TryPlace(roomId, pendingColumn, pendingRow, pendingQuarterTurns);
                if (!applied)
                {
                    ClearAnimalPassageConnections();
                    return;
                }
            }

            var placements = routeLayout.ExportData()
                .Where(item => !routeLayout.Get(item.id).InTray).ToArray();
            var routeDay = runtime.Clock.DayNumber +
                (runtime.Mode == GameMode.Sandbox ? 1 : 0);
            var navigation = new RoomNavigationMap(
                placements, RoomLayoutData.All, cellSize, true, routeDay);
            foreach (var view in views.Values)
            {
                var connected = new HashSet<AnimalPassagePort>();
                foreach (var neighbour in navigation.NeighboursOf(view.Spec.Id))
                {
                    if (navigation.TryGetAnimalConnectionPort(view.Spec.Id, neighbour, out var port))
                    {
                        connected.Add(port);
                    }
                }
                view.SetAnimalPassageConnections(connected);
            }
        }

        private void ClearAnimalPassageConnections()
        {
            foreach (var view in views.Values)
            {
                view.SetAnimalPassageConnections(Array.Empty<AnimalPassagePort>());
            }
        }
    }
}
