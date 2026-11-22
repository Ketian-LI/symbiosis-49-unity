using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UrbanWildlifeRooms.Editor
{
    public static class UrbanWildlifeBuildPipeline
    {
        private const string CameraDefine = "SYMBIOSIS_CAMERA_BUILD";
        private const string MainScene = "Assets/Scenes/Main.unity";
        private const string ConstructionScene =
            "Assets/Scenes/ConstructionPrototype.unity";
        private const string WindowsIconAsset =
            "Assets/Branding/Symbiosis49-Woodcut-WindowsIcon-v1.png";

        [MenuItem("Urban Wildlife/Set Windows App Icon")]
        public static void SetWindowsAppIcon()
        {
            AssetDatabase.Refresh();
            ConfigureWindowsIcon(NamedBuildTarget.Standalone);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Urban Wildlife/Build Windows No-Camera")]
        public static void BuildWindowsNoCamera()
        {
            BuildWindows(false);
        }

        [MenuItem("Urban Wildlife/Build Windows Camera")]
        public static void BuildWindowsCamera()
        {
            BuildWindows(true);
        }

        [MenuItem("Urban Wildlife/Build macOS No-Camera")]
        public static void BuildMacOSNoCamera()
        {
            BuildStandalone(false, BuildTarget.StandaloneOSX, "macOS", ".app", false);
        }

        private static void BuildWindows(bool cameraBuild)
        {
            BuildStandalone(cameraBuild, BuildTarget.StandaloneWindows64,
                "Windows", ".exe", true);
        }

        private static void BuildStandalone(bool cameraBuild, BuildTarget buildTarget,
            string platform, string extension, bool copyCameraTool)
        {
            var target = NamedBuildTarget.Standalone;
            var originalDefines = PlayerSettings.GetScriptingDefineSymbols(target);
            var originalCompany = PlayerSettings.companyName;
            var originalProduct = PlayerSettings.productName;
            try
            {
                // A smoke-test build must not read or update a player's save.
                if (Environment.GetEnvironmentVariable("SYMBIOSIS49_BUILD_ISOLATED_QA") == "1")
                {
                    PlayerSettings.companyName = "Codex QA Local";
                    PlayerSettings.productName = "SYMBIOSIS49 Construction Smoke";
                }
                var defines = originalDefines.Split(';')
                    .Where(item => !string.IsNullOrWhiteSpace(item) && item != CameraDefine)
                    .ToList();
                if (cameraBuild)
                {
                    defines.Add(CameraDefine);
                }
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
                AssetDatabase.Refresh();
                if (buildTarget == BuildTarget.StandaloneWindows64)
                    ConfigureWindowsIcon(target);

                var variant = cameraBuild ? "Camera" : "NoCamera";
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                var requestedRoot = Environment.GetEnvironmentVariable(
                    "SYMBIOSIS49_BUILD_OUTPUT_ROOT");
                var buildRoot = string.IsNullOrWhiteSpace(requestedRoot)
                    ? Path.Combine(projectRoot, "Builds")
                    : Path.GetFullPath(requestedRoot);
                var output = Path.Combine(buildRoot, $"{platform}-{variant}",
                    $"SYMBIOSIS49-{variant}{extension}");
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene, ConstructionScene },
                    locationPathName = output,
                    target = buildTarget,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"{variant} build failed: {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings.");
                }
                if (copyCameraTool)
                {
                    // The Windows Board Game card starts the local detector
                    // only when the player presses Start camera.
                    var cameraToolSource = Path.Combine(projectRoot, "Tools",
                        "CameraRecognition");
                    if (!Directory.Exists(cameraToolSource))
                        throw new DirectoryNotFoundException(
                            $"Board Game camera tool missing: {cameraToolSource}");
                    var cameraToolOutput = Path.Combine(
                        Path.GetDirectoryName(output) ?? projectRoot,
                        "CameraRecognition");
                    CopyCameraTool(cameraToolSource, cameraToolOutput);
                }
                Debug.Log($"[Urban Wildlife Build] {platform} {variant} build saved to {output}");
            }
            finally
            {
                PlayerSettings.companyName = originalCompany;
                PlayerSettings.productName = originalProduct;
                PlayerSettings.SetScriptingDefineSymbols(target, originalDefines);
                AssetDatabase.Refresh();
            }
        }

        private static void ConfigureWindowsIcon(NamedBuildTarget target)
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(WindowsIconAsset);
            if (icon == null)
                throw new FileNotFoundException("Windows app icon is missing", WindowsIconAsset);

            var sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            if (sizes == null || sizes.Length == 0)
                throw new InvalidOperationException("Unity has no Windows app icon slots.");
            var icons = Enumerable.Repeat(icon, sizes.Length).ToArray();
            PlayerSettings.SetIcons(target, icons, IconKind.Application);
            var assigned = PlayerSettings.GetIcons(target, IconKind.Application);
            if (assigned.Length != icons.Length || assigned.Any(item => item != icon))
                throw new InvalidOperationException("Unity did not assign the Windows app icon.");
            Debug.Log("[Urban Wildlife Build] Windows icon assigned to " +
                sizes.Length + " slots: " + string.Join(", ", sizes));
        }

        private static void CopyCameraTool(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination,
                    Path.GetFileName(file)), true);
            foreach (var directory in Directory.GetDirectories(source))
            {
                var name = Path.GetFileName(directory);
                if (name is ".venv" or "__pycache__") continue;
                CopyCameraTool(directory, Path.Combine(destination, name));
            }
        }
    }
}
