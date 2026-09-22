using System;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class ResidentPopulationController : MonoBehaviour
    {
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layoutEditor;
        private WasteManagementController wasteManagement;
        private ResourceEconomyController resourceEconomy;
        private float cellSize;

        public event Action StateChanged;
        public event Action<ResidentDayReport> DayCompleted;

        public ResidentPopulationModel Model { get; private set; }
        public ResidentDayReport LastReport { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController editorController,
            WasteManagementController wasteController,
            ResourceEconomyController resourceController,
            float gridCellSize)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            wasteManagement = wasteController ?? throw new ArgumentNullException(nameof(wasteController));
            resourceEconomy = resourceController ?? throw new ArgumentNullException(nameof(resourceController));
            cellSize = gridCellSize;

            Model = new ResidentPopulationModel(BuildNavigation(), RoomLayoutData.All);
            wasteManagement.SetResidentCount(Model.ResidentCount);
            layoutEditor.LayoutConfirmed += HandleLayoutConfirmed;
            runtime.RestartRequested += HandleRestartRequested;
            resourceEconomy.SettlementPreparing += HandleSettlementPreparing;
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= HandleLayoutConfirmed;
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
            StateChanged?.Invoke();
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
            StateChanged?.Invoke();
        }

        private void HandleLayoutConfirmed()
        {
            Model?.UpdateNavigation(BuildNavigation());
            StateChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            Model?.Reset();
            wasteManagement.SetResidentCount(Model?.ResidentCount ?? ResidentPopulationModel.StartingResidents);
            StateChanged?.Invoke();
        }

        private RoomNavigationMap BuildNavigation()
        {
            return new RoomNavigationMap(
                layoutEditor.ExportLayout(),
                RoomLayoutData.All,
                cellSize);
        }
    }
}
