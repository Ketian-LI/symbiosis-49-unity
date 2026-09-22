using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Core
{
    /// <summary>
    /// Owns camera capture and the stable-layout transaction. A marker detector
    /// submits complete room observations through SubmitObservation; this class
    /// validates, pauses, stabilises and atomically applies them.
    /// </summary>
    public sealed class PhysicalBoardCameraController : MonoBehaviour
    {
        private readonly PhysicalBoardStabilityTracker stability = new();
        private readonly List<RoomPlacementData> pendingObservation = new();
        private GameRuntimeController runtime;
        private RoomLayoutEditorController layout;
        private UrbanWildlifeHud hud;
        private WebCamTexture cameraTexture;
        private string appliedSignature = string.Empty;
        private string pendingSignature = string.Empty;
        private PhysicalBoardValidationResult pendingValidation;
        private bool hasObservation;

        public bool CameraAvailable => cameraTexture != null;
        public string ActiveCameraName => cameraTexture?.deviceName ?? string.Empty;

        public void Initialize(
            GameRuntimeController runtimeController,
            RoomLayoutEditorController layoutController,
            UrbanWildlifeHud hudController)
        {
            runtime = runtimeController;
            layout = layoutController;
            hud = hudController;
            appliedSignature = PhysicalBoardRecognitionModel.Signature(layout.ExportLayout());
            runtime.CameraRecalibrationRequested += HandleCalibrationRequested;
            layout.LayoutConfirmed += HandleLayoutConfirmed;
            if (Application.isPlaying && BuildVariantSettings.UsesCameraRecognition)
            {
                StartCamera();
            }
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.CameraRecalibrationRequested -= HandleCalibrationRequested;
            }
            if (layout != null)
            {
                layout.LayoutConfirmed -= HandleLayoutConfirmed;
            }
            if (cameraTexture != null && cameraTexture.isPlaying)
            {
                cameraTexture.Stop();
            }
        }

        private void Update()
        {
            if (!BuildVariantSettings.UsesCameraRecognition || runtime == null)
            {
                return;
            }
            if (cameraTexture != null && cameraTexture.didUpdateThisFrame)
            {
                hud.SetCameraCalibrationPreview(cameraTexture);
            }
            if (!hasObservation || runtime.CameraCalibrationOpen || !runtime.HasActiveRun)
            {
                return;
            }
            if (pendingSignature == appliedSignature)
            {
                stability.Reset();
                if (runtime.CameraRecognitionState != CameraRecognitionFeedbackState.Hidden)
                {
                    runtime.ClearCameraRecognitionFeedback();
                }
                return;
            }
            if (!pendingValidation.IsCompleteAndLegal)
            {
                stability.Reset();
                if (runtime.CameraRecognitionState == CameraRecognitionFeedbackState.Hidden)
                {
                    runtime.BeginCameraRecognition();
                }
                runtime.ReportCameraRecognitionInvalidPlacement();
                hud.SetCameraInvalidPlacementRects(BuildAffectedRects(pendingValidation.AffectedCells));
                return;
            }

            if (runtime.CameraRecognitionState == CameraRecognitionFeedbackState.Hidden)
            {
                runtime.BeginCameraRecognition();
            }
            stability.Observe(pendingSignature, Time.unscaledDeltaTime);
            runtime.ReportCameraRecognitionStability(stability.Progress);
            if (stability.Progress < 1f)
            {
                return;
            }
            if (layout.RestoreLayout(ClonePlacements(pendingObservation)))
            {
                appliedSignature = pendingSignature;
                runtime.ConfirmCameraRecognisedLayout();
            }
            else
            {
                runtime.ReportCameraRecognitionInvalidPlacement();
            }
            stability.Reset();
        }

        public void SubmitObservation(IEnumerable<RoomPlacementData> observation)
        {
            pendingObservation.Clear();
            pendingObservation.AddRange(ClonePlacements(observation));
            pendingValidation = PhysicalBoardRecognitionModel.Validate(pendingObservation);
            pendingSignature = pendingValidation.Signature;
            hasObservation = true;

            if (!runtime.CameraCalibrationOpen)
            {
                return;
            }
            runtime.ReportRecognisedPhysicalModules(pendingValidation.RecognisedModuleCount);
            var affected = new HashSet<int>(pendingValidation.AffectedCells);
            for (var cell = 0; cell < RoomLayoutData.GridSize * RoomLayoutData.GridSize; cell++)
            {
                hud.SetCameraCalibrationCellState(
                    cell,
                    affected.Contains(cell)
                        ? CameraCalibrationCellState.Unresolved
                        : CameraCalibrationCellState.Recognised);
            }
        }

        private void HandleCalibrationRequested()
        {
            stability.Reset();
            hasObservation = false;
            StartCamera();
            for (var cell = 0; cell < RoomLayoutData.GridSize * RoomLayoutData.GridSize; cell++)
            {
                hud.SetCameraCalibrationCellState(cell, CameraCalibrationCellState.Unknown);
            }
        }

        private void HandleLayoutConfirmed()
        {
            appliedSignature = PhysicalBoardRecognitionModel.Signature(layout.ExportLayout());
        }

        private void StartCamera()
        {
            if (cameraTexture != null)
            {
                if (!cameraTexture.isPlaying)
                {
                    cameraTexture.Play();
                }
                hud.SetCameraCalibrationPreview(cameraTexture);
                return;
            }
            var device = WebCamTexture.devices.FirstOrDefault();
            if (string.IsNullOrEmpty(device.name))
            {
                return;
            }
            cameraTexture = new WebCamTexture(device.name, 1280, 720, 30);
            cameraTexture.Play();
            hud.SetCameraCalibrationPreview(cameraTexture);
        }

        private static IReadOnlyList<Rect> BuildAffectedRects(IEnumerable<int> cells)
        {
            const float boardSize = 900f;
            var cellSize = boardSize / RoomLayoutData.GridSize;
            return (cells ?? Array.Empty<int>())
                .Select(cell =>
                {
                    var column = cell % RoomLayoutData.GridSize;
                    var row = cell / RoomLayoutData.GridSize;
                    var center = new Vector2(
                        -boardSize * 0.5f + (column + 0.5f) * cellSize,
                        boardSize * 0.5f - (row + 0.5f) * cellSize);
                    return new Rect(center - Vector2.one * cellSize * 0.5f, Vector2.one * cellSize);
                })
                .ToArray();
        }

        private static IEnumerable<RoomPlacementData> ClonePlacements(IEnumerable<RoomPlacementData> placements)
        {
            return (placements ?? Array.Empty<RoomPlacementData>())
                .Where(item => item != null)
                .Select(item => new RoomPlacementData
                {
                    id = item.id,
                    column = item.column,
                    row = item.row,
                    quarterTurns = item.quarterTurns
                });
        }
    }
}
