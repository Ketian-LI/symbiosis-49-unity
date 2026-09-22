using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class OakTreeLifecycleController : MonoBehaviour
    {
        private readonly Dictionary<string, OakTreeStageVisual> visuals = new();
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layoutEditor;
        private ResourceEconomyController resourceEconomy;

        public event Action StateChanged;
        public event Action<string> TreeFelled;
        public event Action<string> TreePlanted;
        public event Action<string> TreeMatured;

        public OakTreeGrowthModel Model { get; private set; }

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController editorController,
            ResourceEconomyController economyController,
            IReadOnlyDictionary<string, OakTreeStageVisual> treeVisuals)
        {
            runtime = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
            layoutEditor = editorController ?? throw new ArgumentNullException(nameof(editorController));
            resourceEconomy = economyController ?? throw new ArgumentNullException(nameof(economyController));
            visuals.Clear();
            if (treeVisuals != null)
            {
                foreach (var pair in treeVisuals)
                {
                    if (pair.Value != null)
                    {
                        visuals[pair.Key] = pair.Value;
                    }
                }
            }

            Model = new OakTreeGrowthModel(RoomLayoutData.All);
            layoutEditor.RoomsMoved += HandleRoomsMoved;
            resourceEconomy.DaySettled += HandleDaySettled;
            runtime.RestartRequested += HandleRestartRequested;
            SyncVisuals();
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.RoomsMoved -= HandleRoomsMoved;
            }
            if (resourceEconomy != null)
            {
                resourceEconomy.DaySettled -= HandleDaySettled;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
        }

        public bool TryPlant(string roomId)
        {
            if (Model == null || Model.StageOf(roomId) != OakTreeStage.Felled)
            {
                return false;
            }

            if (!resourceEconomy.TrySpend(ResourceEconomyModel.PlantTreeCost))
            {
                return false;
            }

            if (!Model.Plant(roomId))
            {
                return false;
            }

            SyncVisual(roomId);
            TreePlanted?.Invoke(roomId);
            StateChanged?.Invoke();
            return true;
        }

        public void RestoreSession(
            IEnumerable<OakTreeSaveData> savedTrees,
            int felled,
            int planted,
            int matured)
        {
            Model?.Restore(savedTrees, felled, planted, matured);
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleRoomsMoved(IReadOnlyList<string> roomIds)
        {
            foreach (var roomId in roomIds)
            {
                if (!Model.Fell(roomId))
                {
                    continue;
                }

                SyncVisual(roomId);
                TreeFelled?.Invoke(roomId);
            }
            StateChanged?.Invoke();
        }

        private void HandleDaySettled(ResourceSettlement settlement)
        {
            foreach (var roomId in Model.AdvanceOneCompleteDay())
            {
                TreeMatured?.Invoke(roomId);
            }
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void HandleRestartRequested()
        {
            Model?.Reset();
            SyncVisuals();
            StateChanged?.Invoke();
        }

        private void SyncVisuals()
        {
            if (Model == null)
            {
                return;
            }

            foreach (var roomId in Model.Trees.Keys)
            {
                SyncVisual(roomId);
            }
        }

        private void SyncVisual(string roomId)
        {
            if (visuals.TryGetValue(roomId, out var visual))
            {
                visual.SetStage(Model.StageOf(roomId));
            }
        }
    }
}
