using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Editor
{
    public static class HedgehogDemoCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const int FrameRate = 8;
        private const int FrameCount = 48;
        private const int Width = 960;
        private const int Height = 540;

        [MenuItem("Urban Wildlife/Capture Hedgehog Animation Preview Frames")]
        public static void CapturePreviewFrames()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var hedgehogs = Resources.FindObjectsOfTypeAll<HedgehogDemoAgent>();
            var hedgehog = hedgehogs.Length > 0 ? hedgehogs[0] : null;
            var camera = bootstrap.LayoutCamera;
            if (hedgehog == null || camera == null)
            {
                throw new System.InvalidOperationException("Hedgehog demo or layout camera was not generated.");
            }

            foreach (var pigeon in Resources.FindObjectsOfTypeAll<PigeonDemoAgent>())
            {
                pigeon.gameObject.SetActive(false);
            }

            foreach (var squirrel in Resources.FindObjectsOfTypeAll<SquirrelDemoAgent>())
            {
                squirrel.gameObject.SetActive(false);
            }

            foreach (var citizen in Resources.FindObjectsOfTypeAll<CitizenDemoAgent>())
            {
                citizen.gameObject.SetActive(false);
            }

            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                canvas.enabled = false;
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                label.gameObject.SetActive(false);
            }

            var basePosition = hedgehog.SpawnPosition;
            var cameraFocus = basePosition + new Vector3(-0.08f, 0.16f, 0.16f);
            camera.transform.position = cameraFocus + new Vector3(-1.95f, 1.45f, -2.15f);
            camera.transform.rotation = Quaternion.LookRotation(cameraFocus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 1.10f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var previewDirectory = Path.Combine(projectRoot, "Previews", "HedgehogAnimation");
            Directory.CreateDirectory(previewDirectory);
            foreach (var oldFrame in Directory.GetFiles(previewDirectory, "frame_*.png"))
            {
                File.Delete(oldFrame);
            }

            for (var frame = 0; frame < FrameCount; frame++)
            {
                var sequenceTime = frame / (float)FrameRate;
                ResolvePreviewPose(sequenceTime, basePosition, out var state, out var stateTime, out var position, out var rotation);
                hedgehog.ApplyPreviewPose(state, stateTime, position, rotation);
                RenderCamera(camera, Path.Combine(previewDirectory, $"frame_{frame:000}.png"));
            }

            File.Copy(
                Path.Combine(previewDirectory, "frame_034.png"),
                Path.Combine(projectRoot, "Previews", "HedgehogAnimationDemo.png"),
                true);
            Debug.Log($"[Hedgehog Demo] {FrameCount} preview frames saved to {previewDirectory}");
        }

        private static void ResolvePreviewPose(
            float sequenceTime,
            Vector3 basePosition,
            out HedgehogDemoState state,
            out float stateTime,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = basePosition + new Vector3(0.32f, 0f, 0.22f);
            var travelVector = new Vector3(-0.72f, 0f, 0.12f);
            rotation = Quaternion.LookRotation(travelVector.normalized, Vector3.up);
            if (sequenceTime < 0.75f)
            {
                state = HedgehogDemoState.Idle;
                stateTime = sequenceTime;
                return;
            }

            if (sequenceTime < 2.75f)
            {
                state = HedgehogDemoState.Waddle;
                stateTime = sequenceTime - 0.75f;
                position += travelVector * Mathf.InverseLerp(0.75f, 2.75f, sequenceTime);
                return;
            }

            position += travelVector;
            if (sequenceTime < 4.1f)
            {
                state = HedgehogDemoState.Sniff;
                stateTime = sequenceTime - 2.75f;
                return;
            }

            if (sequenceTime < 5.25f)
            {
                state = HedgehogDemoState.Curl;
                stateTime = sequenceTime - 4.1f;
                return;
            }

            state = HedgehogDemoState.Idle;
            stateTime = sequenceTime - 5.25f;
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            renderTexture.Create();
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.aspect = Width / (float)Height;
            camera.Render();
            RenderTexture.active = renderTexture;
            var screenshot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(screenshot);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }
}
