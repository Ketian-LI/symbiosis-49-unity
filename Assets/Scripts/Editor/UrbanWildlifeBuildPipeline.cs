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
            try
            {
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
                var output = Path.Combine(projectRoot, "Builds", $"Windows-{variant}", $"SYMBIOSIS49-{variant}.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? projectRoot);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene },
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
                PlayerSettings.SetScriptingDefineSymbols(target, originalDefines);
                AssetDatabase.Refresh();
            }
        }
    }
}
