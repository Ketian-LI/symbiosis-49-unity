using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Editor
{
    public static class UrbanWildlifeProjectSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Urban Wildlife/Create Or Refresh Main Scene")]
        public static void CreateOrRefreshProject()
        {
            var validationErrors = RoomLayoutData.Validate();
            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                {
                    Debug.LogError($"[Urban Wildlife Setup] {error}");
                }

                throw new System.InvalidOperationException("Room layout validation failed.");
            }

            PlayerSettings.productName = "城市共栖房间";
            PlayerSettings.companyName = "Urban Wildlife Rooms";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.vSyncCount = 1;

            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            var root = new GameObject("Urban Wildlife Rooms");
            var bootstrap = root.AddComponent<UrbanWildlifeBootstrap>();
            bootstrap.RebuildPreview();

            EditorSceneManager.SaveScene(scene, MainScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            Selection.activeGameObject = root;
            CaptureCurrentLayoutPreview(bootstrap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Urban Wildlife Setup] Main scene created, build settings updated, and layout preview captured.");
        }

        [MenuItem("Urban Wildlife/Validate Layout Data")]
        public static void ValidateLayoutData()
        {
            var validationErrors = RoomLayoutData.Validate();
            if (validationErrors.Count == 0)
            {
                Debug.Log("[Urban Wildlife Layout] Validation passed: 35 rooms, 49 occupied cells, fixed 2×2 central park.");
                return;
            }

            foreach (var error in validationErrors)
            {
                Debug.LogError($"[Urban Wildlife Layout] {error}");
            }
        }

        [MenuItem("Urban Wildlife/Capture Layout Preview")]
        public static void CaptureLayoutPreview()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null)
            {
                if (File.Exists(Path.GetFullPath(MainScenePath)))
                {
                    EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                    bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
                }
            }

            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            CaptureCurrentLayoutPreview(bootstrap);
        }

        [MenuItem("Urban Wildlife/Capture Layout Editing Preview")]
        public static void CaptureLayoutEditingPreview()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null && File.Exists(Path.GetFullPath(MainScenePath)))
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            }

            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var layoutEditor = bootstrap.LayoutEditor;
            if (layoutEditor == null)
            {
                throw new System.InvalidOperationException("Generated preview does not contain RoomLayoutEditorController.");
            }

            layoutEditor.EnterEditing();
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_LayoutEditing.png");
        }

        [MenuItem("Urban Wildlife/Capture Board Foundation Preview")]
        public static void CaptureBoardFoundationPreview()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null && File.Exists(Path.GetFullPath(MainScenePath)))
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            }

            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var generatedRoot = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var hudRoot = generatedRoot == null ? null : generatedRoot.Find("HUD Canvas");
            if (hudRoot != null)
            {
                Object.DestroyImmediate(hudRoot.gameObject);
            }

            var camera = bootstrap.LayoutCamera;
            var previousOrthographicSize = camera.orthographicSize;
            camera.orthographicSize = 12.2f;
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_BoardFoundation.png");
            camera.orthographicSize = previousOrthographicSize;
            bootstrap.RebuildPreview();
        }

        [MenuItem("Urban Wildlife/Capture Research Setup Preview")]
        public static void CaptureResearchSetupPreview()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null && File.Exists(Path.GetFullPath(MainScenePath)))
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            }
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }
            bootstrap.RebuildPreview();
            var generatedRoot = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var hudRoot = generatedRoot == null ? null : generatedRoot.Find("HUD Canvas");
            var hud = hudRoot == null ? null : hudRoot.GetComponent<UrbanWildlifeHud>();
            if (hud == null)
            {
                throw new System.InvalidOperationException("Generated preview does not contain UrbanWildlifeHud.");
            }
            hud.ShowResearchSetupPreview();
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_ResearchSetup.png");
            bootstrap.RebuildPreview();
        }

        [MenuItem("Urban Wildlife/Capture Onboarding Preview")]
        public static void CaptureOnboardingPreview()
        {
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            if (bootstrap == null && File.Exists(Path.GetFullPath(MainScenePath)))
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            }
            if (bootstrap == null)
            {
                throw new System.InvalidOperationException("Main scene does not contain UrbanWildlifeBootstrap.");
            }

            bootstrap.RebuildPreview();
            var generatedRoot = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var onboardingRoot = generatedRoot == null ? null : generatedRoot.Find("First Run Onboarding");
            var onboarding = onboardingRoot == null ? null : onboardingRoot.GetComponent<FirstRunOnboardingController>();
            if (onboarding == null)
            {
                throw new System.InvalidOperationException("Generated preview does not contain FirstRunOnboardingController.");
            }
            onboarding.ShowVisualPreview(OnboardingStep.SelectResident);
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_OnboardingResident.png");

            bootstrap.RebuildPreview();
            generatedRoot = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            onboardingRoot = generatedRoot == null ? null : generatedRoot.Find("First Run Onboarding");
            onboarding = onboardingRoot == null ? null : onboardingRoot.GetComponent<FirstRunOnboardingController>();
            if (onboarding == null)
            {
                throw new System.InvalidOperationException("Rebuilt preview does not contain FirstRunOnboardingController.");
            }
            onboarding.ShowVisualPreview(OnboardingStep.PracticeLayout);
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_OnboardingLayout.png");
            bootstrap.RebuildPreview();
        }

        private static void CaptureCurrentLayoutPreview(UrbanWildlifeBootstrap bootstrap)
        {
            CaptureCurrentLayoutPreview(bootstrap, "UrbanWildlifeRooms_Layout.png");
        }

        private static void CaptureCurrentLayoutPreview(UrbanWildlifeBootstrap bootstrap, string fileName)
        {
            Canvas.ForceUpdateCanvases();
            var camera = bootstrap.LayoutCamera;
            if (camera == null)
            {
                throw new System.InvalidOperationException("Layout camera was not generated.");
            }

            const int width = 1920;
            const int height = 1080;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
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

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var previewDirectory = Path.Combine(projectRoot, "Previews");
            Directory.CreateDirectory(previewDirectory);
            var previewPath = Path.Combine(previewDirectory, fileName);
            File.WriteAllBytes(previewPath, screenshot.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(screenshot);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            Debug.Log($"[Urban Wildlife Setup] Preview saved to {previewPath}");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
