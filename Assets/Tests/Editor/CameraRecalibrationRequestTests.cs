using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class CameraRecalibrationRequestTests
    {
        [Test]
        public void RecalibrationRequestIsIgnoredWithoutCameraAndDispatchedWithCamera()
        {
            var runtimeObject = new GameObject("Camera Recalibration Request Test");
            try
            {
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                var requestCount = 0;
                runtime.CameraRecalibrationRequested += () => requestCount++;

                BuildVariantSettings.SetEditorVariantOverride(ApplicationBuildVariant.NoCamera);
                runtime.RequestCameraRecalibration();
                Assert.That(requestCount, Is.Zero);
                Assert.That(runtime.CameraCalibrationOpen, Is.False);

                BuildVariantSettings.SetEditorVariantOverride(ApplicationBuildVariant.CameraRecognition);
                runtime.RequestCameraRecalibration();
                Assert.That(requestCount, Is.EqualTo(1));
                Assert.That(runtime.CameraCalibrationOpen, Is.True);

                runtime.ReportRecognisedPhysicalModules(
                    GameRuntimeController.RequiredPhysicalModuleCount);
                Assert.That(runtime.CameraCalibrationReady, Is.True);

                runtime.CompleteCameraCalibration();
                Assert.That(runtime.CameraCalibrationOpen, Is.False);
            }
            finally
            {
                BuildVariantSettings.SetEditorVariantOverride(null);
                Object.DestroyImmediate(runtimeObject);
            }
        }

        [Test]
        public void RecognitionFeedbackPausesUntilStableLayoutIsConfirmed()
        {
            var runtimeObject = new GameObject("Camera Recognition Feedback Test");
            try
            {
                BuildVariantSettings.SetEditorVariantOverride(ApplicationBuildVariant.CameraRecognition);
                var runtime = runtimeObject.AddComponent<GameRuntimeController>();
                runtime.Initialize(null);
                runtime.StartNewRun(GameMode.Sandbox);

                runtime.ReportRecognisedPhysicalModules(
                    GameRuntimeController.RequiredPhysicalModuleCount);
                runtime.CompleteCameraCalibration();

                runtime.BeginCameraRecognition();
                Assert.That(runtime.CameraRecognitionState, Is.EqualTo(CameraRecognitionFeedbackState.Scanning));
                Assert.That(runtime.IsPaused, Is.True);

                runtime.ReportCameraRecognitionInvalidPlacement();
                Assert.That(runtime.CameraRecognitionState, Is.EqualTo(CameraRecognitionFeedbackState.InvalidPlacement));
                Assert.That(runtime.IsPaused, Is.True);

                runtime.ConfirmCameraRecognisedLayout();
                Assert.That(runtime.CameraRecognitionState, Is.EqualTo(CameraRecognitionFeedbackState.InvalidPlacement));

                runtime.ReportCameraRecognitionStability(1f);
                runtime.ConfirmCameraRecognisedLayout();

                Assert.That(runtime.CameraRecognitionState, Is.EqualTo(CameraRecognitionFeedbackState.Confirmed));
                Assert.That(runtime.IsPaused, Is.False);
            }
            finally
            {
                BuildVariantSettings.SetEditorVariantOverride(null);
                Time.timeScale = 1f;
                Object.DestroyImmediate(runtimeObject);
            }
        }
    }
}
