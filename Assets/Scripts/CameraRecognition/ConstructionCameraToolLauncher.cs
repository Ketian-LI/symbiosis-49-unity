using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace UrbanWildlifeRooms.CameraRecognition
{
    // The camera detector is an optional local process. The player chooses
    // when to open its visible preview; entering a mode never launches it.
    public static class ConstructionCameraToolLauncher
    {
        private static Process runningTool;

        public static bool TryLaunchCameraTool(out string error)
        {
            error = string.Empty;
            if (Application.platform != RuntimePlatform.WindowsEditor &&
                Application.platform != RuntimePlatform.WindowsPlayer)
            {
                error = "The camera tool currently runs on Windows only.";
                return false;
            }

            if (runningTool != null)
            {
                try
                {
                    if (!runningTool.HasExited)
                    {
                        error = "The camera tool is already running.";
                        return false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // A process from a previous launch is no longer available.
                }
                runningTool.Dispose();
                runningTool = null;
            }

            var projectOrExecutableFolder = Path.GetDirectoryName(
                Application.dataPath);
            if (string.IsNullOrEmpty(projectOrExecutableFolder))
            {
                error = "Cannot locate the camera tool folder.";
                return false;
            }
            var script = Application.isEditor
                ? Path.Combine(projectOrExecutableFolder, "Tools", "CameraRecognition", "StartCamera.cmd")
                : Path.Combine(projectOrExecutableFolder, "CameraRecognition", "StartCamera.cmd");
            if (!File.Exists(script))
            {
                error = $"Camera tool was not found: {script}";
                return false;
            }

            try
            {
                runningTool = Process.Start(new ProcessStartInfo
                {
                    FileName = script,
                    WorkingDirectory = Path.GetDirectoryName(script),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false
                });
                if (runningTool != null) return true;
                error = "Windows did not start the camera tool.";
                return false;
            }
            catch (Exception exception)
            {
                error = $"Cannot start camera tool: {exception.Message}";
                return false;
            }
        }

        // The only process we can stop is the command shell started above.
        // /T limits the shutdown to that shell's descendants (the detector).
        public static void StopCameraTool()
        {
            var launched = runningTool;
            if (launched == null) return;
            try
            {
                if (!launched.HasExited)
                {
                    var taskkillPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.System),
                        "taskkill.exe");
                    using (var taskkill = Process.Start(new ProcessStartInfo
                    {
                        FileName = taskkillPath,
                        Arguments = $"/PID {launched.Id} /T /F",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }))
                    {
                        if (taskkill == null || !taskkill.WaitForExit(2000) ||
                            taskkill.ExitCode != 0)
                            UnityEngine.Debug.LogWarning(
                                "Camera tool did not stop cleanly. Close its preview window.");
                    }
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning(
                    "Cannot stop camera tool: " + exception.Message);
            }
            finally
            {
                try
                {
                    if (launched.HasExited)
                    {
                        launched.Dispose();
                        if (runningTool == launched) runningTool = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    launched.Dispose();
                    if (runningTool == launched) runningTool = null;
                }
            }
        }
    }
}
