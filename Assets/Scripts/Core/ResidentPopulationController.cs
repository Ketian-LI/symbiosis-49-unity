using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class ResidentPopulationController : MonoBehaviour
    {
        public const int MinimumRequiredResidents = 3;
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layoutEditor;
        private WasteManagementController wasteManagement;
        private ResourceEconomyController resourceEconomy;
        private float cellSize;
        private readonly List<RoomView> facilityRooms = new();

        public event Action StateChanged;
        public event Action<ResidentDayReport> DayCompleted;

        public ResidentPopulationModel Model { get; private set; }
        public ResidentDayReport LastReport { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController editorController,
            WasteManagementController wasteController,
            ResourceEconomyController resourceController,
            float gridCellSize,
            IEnumerable<RoomView> rooms)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            resourceEconomy = resourceController ?? throw new ArgumentNullException(nameof(resourceController));
            cellSize = gridCellSize;

            Model = new ResidentPopulationModel(BuildNavigation(), RoomLayoutData.All);
            facilityRooms.Clear();
            foreach (var room in rooms ?? Array.Empty<RoomView>())
                if (room?.Spec?.Type is RoomType.Office or RoomType.Canteen)
                    facilityRooms.Add(room);
            RefreshFacilityBadges();
            wasteManagement.SetResidentCount(Model.ResidentCount);
            layoutEditor.LayoutConfirmed += HandleLayoutConfirmed;
            layoutEditor.LayoutRestored += HandleLayoutConfirmed;
            runtime.RestartRequested += HandleRestartRequested;
            resourceEconomy.SettlementPreparing += HandleSettlementPreparing;
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= HandleLayoutConfirmed;
                layoutEditor.LayoutRestored -= HandleLayoutConfirmed;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
            if (resourceEconomy != null)
            {
                resourceEconomy.SettlementPreparing -= HandleSettlementPreparing;
            }
        }

        public void RestoreSession(
            System.Collections.Generic.IEnumerable<ResidentSaveData> residents,
            string prospectiveResidenceId,
            int nextResidentNumber,
            int arrivals,
            int relocations,
            int departures,
            int peakResidentCount)
        {
            Model?.Restore(
                residents,
                prospectiveResidenceId,
                nextResidentNumber,
                arrivals,
                relocations,
                departures,
                peakResidentCount);
            wasteManagement.SetResidentCount(Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents);
            NotifyStateChanged();
        }

        private void HandleSettlementPreparing(int completedDay)
        {
            if (Model == null)
            {
                return;
            }

            var humanFunction = Mathf.Clamp(
                100 - wasteManagement.HumanFunctionPenalty,
                0,
                100);
            LastReport = Model.CompleteDay(
                completedDay,
                humanFunction,
                wasteManagement.OperatingFoodShopCount >= WasteManagementController.FoodShopCount);
            resourceEconomy.SetDailyEconomyEstimate(
                LastReport.Production,
                LastReport.FoodServiceCost);
            wasteManagement.SetResidentCount(Model.ResidentCount);
            DayCompleted?.Invoke(LastReport);
            NotifyStateChanged();
            if (Model.ResidentCount < MinimumRequiredResidents && runtime.HasActiveRun)
            {
                resourceEconomy.EndRunForInsufficientResidents(
                    completedDay, Model.ResidentCount, MinimumRequiredResidents);
            }
        }

        private void HandleLayoutConfirmed()
        {
            Model?.UpdateNavigation(BuildNavigation());
            NotifyStateChanged();
        }

        private void HandleRestartRequested()
        {
            Model?.Reset();
            wasteManagement.SetResidentCount(Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents);
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            RefreshFacilityBadges();
            StateChanged?.Invoke();
        }

        private void RefreshFacilityBadges()
        {
            if (Model == null) return;
            var use = Model.PreviewFacilityUse();
            foreach (var room in facilityRooms)
            {
                if (room == null) continue;
                var counts = room.Spec.Type == RoomType.Office
                    ? use.Offices : use.FoodShops;
                room.SetFacilityCapacity(counts.TryGetValue(room.Spec.Id, out var used)
                    ? used : 0);
            }
        }

        private RoomNavigationMap BuildNavigation()
        {
            return new RoomNavigationMap(
                layoutEditor.ExportLayout(),
                RoomLayoutData.All,
                cellSize, humanRoadsOnly: true);
        }
    }
}
