using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Editor
{
    /// <summary>Close-up, UI-free render for reviewing the runtime sketch material.</summary>
    public static class SketchSurfaceCapture
    {
        [MenuItem("Urban Wildlife/Capture Sketch Surface Preview")]
        public static void Capture()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null || bootstrap.LayoutCamera == null)
            {
                Debug.LogError("[Sketch Surface] Open the Main scene before capturing.");
                return;
            }

            var room = Resources.FindObjectsOfTypeAll<RoomView>()
                .FirstOrDefault(view => view.Spec != null && view.Spec.Id == "residence-a");
            if (room == null)
            {
                Debug.LogError("[Sketch Surface] Residence A was not found.");
                return;
            }

            var camera = bootstrap.LayoutCamera;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var oldSize = camera.orthographicSize;
            var oldTarget = camera.targetTexture;
            var oldAspect = camera.aspect;
            var oldActive = RenderTexture.active;
            // Generated preview objects use DontSaveInEditor and are omitted by
            // FindObjectsByType, so collect them via Resources instead.
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>()
                .Where(canvas => canvas.gameObject.scene == bootstrap.gameObject.scene)
                .ToArray();
            var canvasStates = canvases.Select(canvas => canvas.gameObject.activeSelf).ToArray();
            RenderTexture target = null;
            Texture2D image = null;

            try
            {
                foreach (var canvas in canvases) canvas.gameObject.SetActive(false);
                Canvas.ForceUpdateCanvases();
                camera.transform.position = room.VisualRoot.position + Vector3.up * 12f;
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camera.orthographicSize = 2.15f;
                camera.aspect = 16f / 9f;

                target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4
                };
                target.Create();
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();

                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                var previewDirectory = Path.Combine(projectRoot, "Previews");
                Directory.CreateDirectory(previewDirectory);
                var outputPath = Path.Combine(previewDirectory, "SketchSurface_Residence_v04.png");
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"[Sketch Surface] Preview saved to {outputPath}");
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.transform.position = oldPosition;
                camera.transform.rotation = oldRotation;
                camera.orthographicSize = oldSize;
                camera.aspect = oldAspect;
                RenderTexture.active = oldActive;
                for (var index = 0; index < canvases.Length; index++)
                {
                    if (canvases[index] != null) canvases[index].gameObject.SetActive(canvasStates[index]);
                }
                Canvas.ForceUpdateCanvases();
                if (image != null) Object.DestroyImmediate(image);
                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }
        }
    }
}
