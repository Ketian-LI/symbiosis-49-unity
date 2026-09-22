using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Editor
{
    public static class PigeonDemoCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const int FrameRate = 8;
        private const int FrameCount = 48;
        private const int Width = 960;
        private const int Height = 540;

        [MenuItem("Urban Wildlife/Capture Pigeon Selection Reference")]
        public static void CaptureSelectionReference()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var pigeons = Resources.FindObjectsOfTypeAll<PigeonDemoAgent>();
            var pigeon = pigeons.Length > 0 ? pigeons[0] : null;
            var camera = bootstrap.LayoutCamera;
            if (pigeon == null || camera == null)
            {
                throw new System.InvalidOperationException("Pigeon demo or layout camera was not generated.");
            }

            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                canvas.enabled = false;
            }

            foreach (var citizen in Resources.FindObjectsOfTypeAll<CitizenDemoAgent>())
            {
                citizen.gameObject.SetActive(false);
            }

            foreach (var squirrel in Resources.FindObjectsOfTypeAll<SquirrelDemoAgent>())
            {
                squirrel.gameObject.SetActive(false);
            }

            foreach (var hedgehog in Resources.FindObjectsOfTypeAll<HedgehogDemoAgent>())
            {
                hedgehog.gameObject.SetActive(false);
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                label.gameObject.SetActive(false);
            }

            var position = pigeon.SpawnPosition;
            var rotation = Quaternion.LookRotation(new Vector3(0.8f, 0f, 0.35f), Vector3.up);
            pigeon.ApplyPreviewPose(PigeonDemoState.Idle, 0.28f, position, rotation);
            pigeon.SetSelected(true);

            var focus = position + new Vector3(0f, 0.16f, 0f);
            camera.transform.position = focus + new Vector3(-1.6f, 1.45f, -1.85f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 0.88f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputPath = Path.Combine(projectRoot, "Previews", "PigeonSelectionRuntime.png");
            RenderCamera(camera, outputPath);
            Debug.Log($"[Pigeon Demo] Selection reference saved to {outputPath}");
        }

        [MenuItem("Urban Wildlife/Capture Pigeon Animation Preview Frames")]
        public static void CapturePreviewFrames()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var pigeons = Resources.FindObjectsOfTypeAll<PigeonDemoAgent>();
            var pigeon = pigeons.Length > 0 ? pigeons[0] : null;
            var camera = bootstrap.LayoutCamera;
            if (pigeon == null || camera == null)
            {
                throw new System.InvalidOperationException("Pigeon demo or layout camera was not generated.");
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

            var basePosition = pigeon.SpawnPosition;
            var cameraFocus = basePosition + new Vector3(0.06f, 0.20f, 0.04f);
            camera.transform.position = cameraFocus + new Vector3(-1.45f, 1.32f, -1.75f);
            camera.transform.rotation = Quaternion.LookRotation(cameraFocus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 0.94f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var previewDirectory = Path.Combine(projectRoot, "Previews", "PigeonAnimation");
            Directory.CreateDirectory(previewDirectory);
            foreach (var oldFrame in Directory.GetFiles(previewDirectory, "frame_*.png"))
            {
                File.Delete(oldFrame);
            }

            for (var frame = 0; frame < FrameCount; frame++)
            {
                var sequenceTime = frame / (float)FrameRate;
                ResolvePreviewPose(sequenceTime, basePosition, out var state, out var stateTime, out var position, out var rotation);
                pigeon.ApplyPreviewPose(state, stateTime, position, rotation);
                var framePath = Path.Combine(previewDirectory, $"frame_{frame:000}.png");
                RenderCamera(camera, framePath);
            }

            var heroFrame = Path.Combine(previewDirectory, "frame_025.png");
            var heroPath = Path.Combine(projectRoot, "Previews", "PigeonAnimationDemo.png");
            File.Copy(heroFrame, heroPath, true);
            Debug.Log($"[Pigeon Demo] {FrameCount} preview frames saved to {previewDirectory}");
        }

        private static void ResolvePreviewPose(
            float sequenceTime,
            Vector3 basePosition,
            out PigeonDemoState state,
            out float stateTime,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = basePosition + new Vector3(-0.55f, 0f, 0.08f);
            var travelDirection = new Vector3(1.10f, 0f, 0.16f).normalized;
            rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

            if (sequenceTime < 1f)
            {
                state = PigeonDemoState.Idle;
                stateTime = sequenceTime;
                return;
            }

            if (sequenceTime < 3f)
            {
                state = PigeonDemoState.Walk;
                stateTime = sequenceTime - 1f;
                var travel = Mathf.InverseLerp(1f, 3f, sequenceTime);
                position += new Vector3(travel * 1.10f, 0f, travel * 0.16f);
                return;
            }

            position += new Vector3(1.10f, 0f, 0.16f);
            if (sequenceTime < 4.35f)
            {
                state = PigeonDemoState.Peck;
                stateTime = sequenceTime - 3f;
                return;
            }

            if (sequenceTime < 5.2f)
            {
                state = PigeonDemoState.Flutter;
                stateTime = sequenceTime - 4.35f;
                return;
            }

            state = PigeonDemoState.Idle;
            stateTime = sequenceTime - 5.2f;
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
