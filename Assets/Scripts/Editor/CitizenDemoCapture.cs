using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Editor
{
    public static class CitizenDemoCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const int FrameRate = 8;
        private const int FrameCount = 48;
        private const int Width = 960;
        private const int Height = 540;

        [MenuItem("Urban Wildlife/Capture Citizen Animation Preview Frames")]
        public static void CapturePreviewFrames()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var citizens = Resources.FindObjectsOfTypeAll<CitizenDemoAgent>();
            var citizen = citizens.Length > 0 ? citizens[0] : null;
            var camera = bootstrap.LayoutCamera;
            if (citizen == null || camera == null)
            {
                throw new System.InvalidOperationException("Citizen demo or layout camera was not generated.");
            }

            foreach (var pigeon in Resources.FindObjectsOfTypeAll<PigeonDemoAgent>())
            {
                pigeon.gameObject.SetActive(false);
            }

            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                canvas.enabled = false;
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                label.gameObject.SetActive(false);
            }

            var basePosition = citizen.SpawnPosition;
            var cameraFocus = basePosition + new Vector3(0.20f, 0.92f, 0.10f);
            camera.transform.position = cameraFocus + new Vector3(-4.6f, 3.8f, -5.2f);
            camera.transform.rotation = Quaternion.LookRotation(cameraFocus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 2.9f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var previewDirectory = Path.Combine(projectRoot, "Previews", "CitizenAnimation");
            Directory.CreateDirectory(previewDirectory);
            foreach (var oldFrame in Directory.GetFiles(previewDirectory, "frame_*.png"))
            {
                File.Delete(oldFrame);
            }

            for (var frame = 0; frame < FrameCount; frame++)
            {
                var sequenceTime = frame / (float)FrameRate;
                ResolvePreviewPose(sequenceTime, basePosition, out var state, out var stateTime, out var position, out var rotation);
                citizen.ApplyPreviewPose(state, stateTime, position, rotation);
                RenderCamera(camera, Path.Combine(previewDirectory, $"frame_{frame:000}.png"));
            }

            File.Copy(
                Path.Combine(previewDirectory, "frame_018.png"),
                Path.Combine(projectRoot, "Previews", "CitizenAnimationDemo.png"),
                true);
            Debug.Log($"[Citizen Demo] {FrameCount} preview frames saved to {previewDirectory}");
        }

        private static void ResolvePreviewPose(
            float sequenceTime,
            Vector3 basePosition,
            out CitizenDemoState state,
            out float stateTime,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = basePosition + new Vector3(-0.72f, 0f, -0.12f);
            var travelVector = new Vector3(1.42f, 0f, 0.24f);
            rotation = Quaternion.LookRotation(travelVector.normalized, Vector3.up);
            if (sequenceTime < 1f)
            {
                state = CitizenDemoState.Idle;
                stateTime = sequenceTime;
                return;
            }

            if (sequenceTime < 3f)
            {
                state = CitizenDemoState.Walk;
                stateTime = sequenceTime - 1f;
                var travel = Mathf.InverseLerp(1f, 3f, sequenceTime);
                position += travelVector * travel;
                return;
            }

            position += travelVector;
            if (sequenceTime < 4.5f)
            {
                state = CitizenDemoState.Observe;
                stateTime = sequenceTime - 3f;
                return;
            }

            state = CitizenDemoState.Idle;
            stateTime = sequenceTime - 4.5f;
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
