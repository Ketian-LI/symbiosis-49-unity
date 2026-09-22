using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Editor
{
    public static class WorldScaleCapture
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Urban Wildlife/Capture Human Animal Scale Reference")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var citizens = Resources.FindObjectsOfTypeAll<CitizenDemoAgent>();
            var pigeons = Resources.FindObjectsOfTypeAll<PigeonDemoAgent>();
            var squirrels = Resources.FindObjectsOfTypeAll<SquirrelDemoAgent>();
            var hedgehogs = Resources.FindObjectsOfTypeAll<HedgehogDemoAgent>();
            var citizen = citizens.Length > 0 ? citizens[0] : null;
            var pigeon = pigeons.Length > 0 ? pigeons[0] : null;
            var squirrel = squirrels.Length > 0 ? squirrels[0] : null;
            var hedgehog = hedgehogs.Length > 0 ? hedgehogs[0] : null;
            var camera = bootstrap.LayoutCamera;
            if (citizen == null || pigeon == null || squirrel == null || hedgehog == null || camera == null)
            {
                throw new System.InvalidOperationException("Scale reference requires the citizen, pigeon, squirrel, hedgehog and layout camera.");
            }

            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                canvas.enabled = false;
            }

            foreach (var label in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                label.gameObject.SetActive(false);
            }

            var humanPosition = citizen.SpawnPosition + new Vector3(0.12f, 0f, 0.18f);
            var pigeonPosition = humanPosition + new Vector3(0.92f, 0f, -0.62f);
            var squirrelPosition = humanPosition + new Vector3(1.56f, 0f, -0.28f);
            var hedgehogPosition = humanPosition + new Vector3(1.56f, 0f, -0.88f);
            var facing = Quaternion.LookRotation(new Vector3(0.92f, 0f, 0.18f).normalized, Vector3.up);
            citizen.ApplyPreviewPose(CitizenDemoState.Idle, 0.32f, humanPosition, facing);
            pigeon.ApplyPreviewPose(PigeonDemoState.Idle, 0.32f, pigeonPosition, facing);
            squirrel.ApplyPreviewPose(SquirrelDemoState.Idle, 0.32f, squirrelPosition, facing);
            hedgehog.ApplyPreviewPose(HedgehogDemoState.Idle, 0.32f, hedgehogPosition, facing);

            var humanBounds = CalculateBounds(citizen.gameObject);
            var pigeonBounds = CalculateBounds(pigeon.gameObject);
            var squirrelBounds = CalculateBounds(squirrel.gameObject);
            var hedgehogBounds = CalculateBounds(hedgehog.gameObject);
            var pigeonHorizontalLength = Mathf.Sqrt(
                pigeonBounds.size.x * pigeonBounds.size.x + pigeonBounds.size.z * pigeonBounds.size.z);
            var squirrelHorizontalLength = Mathf.Sqrt(
                squirrelBounds.size.x * squirrelBounds.size.x + squirrelBounds.size.z * squirrelBounds.size.z);
            var hedgehogHorizontalLength = Mathf.Sqrt(
                hedgehogBounds.size.x * hedgehogBounds.size.x + hedgehogBounds.size.z * hedgehogBounds.size.z);
            Debug.Log(
                $"[World Scale] Measured citizen height: {humanBounds.size.y:F2} m; " +
                $"pigeon horizontal span: {pigeonHorizontalLength:F2} m; " +
                $"squirrel horizontal span: {squirrelHorizontalLength:F2} m; " +
                $"hedgehog horizontal span: {hedgehogHorizontalLength:F2} m.");

            var focus = (humanPosition + squirrelPosition) * 0.5f + Vector3.up * 0.78f;
            camera.transform.position = focus + new Vector3(-4.2f, 3.45f, -4.75f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
            camera.orthographicSize = 2.55f;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputPath = Path.Combine(projectRoot, "Previews", "HumanAnimalScaleReference.png");
            RenderCamera(camera, outputPath);
            Debug.Log($"[World Scale] Human/animal scale reference saved to {outputPath}");
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var allRenderers = root.GetComponentsInChildren<Renderer>(true);
            var renderers = System.Array.FindAll(
                allRenderers,
                renderer => renderer.gameObject.name != "Follow Selection Ring");
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            renderTexture.Create();
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.aspect = 16f / 9f;
            camera.Render();
            RenderTexture.active = renderTexture;
            var screenshot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
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
