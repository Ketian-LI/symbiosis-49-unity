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

        private static void BuildWindows(bool cameraBuild)
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

                var variant = cameraBuild ? "Camera" : "NoCamera";
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                var requestedRoot = Environment.GetEnvironmentVariable(
                    "SYMBIOSIS49_BUILD_OUTPUT_ROOT");
                var buildRoot = string.IsNullOrWhiteSpace(requestedRoot)
                    ? Path.Combine(projectRoot, "Builds")
                    : Path.GetFullPath(requestedRoot);
                var output = Path.Combine(buildRoot, $"Windows-{variant}",
                    $"SYMBIOSIS49-{variant}.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene, ConstructionScene },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"{variant} build failed: {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings.");
                }
                Debug.Log($"[Urban Wildlife Build] {variant} build saved to {output}");
            }
            finally
            {
                PlayerSettings.companyName = originalCompany;
                PlayerSettings.productName = originalProduct;
                PlayerSettings.SetScriptingDefineSymbols(target, originalDefines);
                AssetDatabase.Refresh();
            }
        }
    }
}
