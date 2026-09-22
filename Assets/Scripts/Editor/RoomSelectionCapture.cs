using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Editor
{
    public static class RoomSelectionCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Urban Wildlife/Capture Room Selection Reference")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var room = Resources.FindObjectsOfTypeAll<RoomView>()
                .FirstOrDefault(view => view.Spec.Type == RoomType.Canteen);
            var camera = bootstrap.LayoutCamera ?? Resources.FindObjectsOfTypeAll<Camera>()
                .FirstOrDefault(candidate => candidate.gameObject.name == "Main Camera");
            if (room == null || camera == null)
            {
                throw new System.InvalidOperationException("Selection capture requires a food-shop room and layout camera.");
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                label.gameObject.SetActive(false);
            }

            room.SetSelected(true);
            var focus = room.VisualRoot.position + Vector3.up * 0.35f;
            camera.transform.position = focus + new Vector3(7.6f, 7.7f, -8.8f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 4.4f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var previewDirectory = Path.Combine(projectRoot, "Previews");
            Directory.CreateDirectory(previewDirectory);
            var outputPath = Path.Combine(previewDirectory, "RoomSelectionRuntime.png");
            RenderCamera(camera, outputPath);
            Debug.Log($"[Room Selection] Runtime reference saved to {outputPath}");
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            const int width = 1280;
            const int height = 720;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            renderTexture.Create();

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.aspect = width / (float)height;
            camera.Render();

            RenderTexture.active = renderTexture;
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
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
