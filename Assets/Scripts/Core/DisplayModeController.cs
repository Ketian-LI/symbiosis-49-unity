using UnityEngine;

namespace UrbanWildlifeRooms.Core
{
    /// <summary>Keeps the display choice separate from game progress and available in both scenes.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class DisplayModeController : MonoBehaviour
    {
        private const string FullscreenKey = "symbiosis49.display.fullscreen";
        private const string WindowWidthKey = "symbiosis49.display.windowWidth";
        private const string WindowHeightKey = "symbiosis49.display.windowHeight";
        private static DisplayModeController instance;

        private bool fullscreen;
        private float modeChangeDeadline;

        public static bool IsAvailable => !Application.isEditor;
        public static bool IsFullscreen => instance != null && instance.fullscreen;
        public static int WindowWidth => PlayerPrefs.GetInt(WindowWidthKey, 1600);
        public static int WindowHeight => PlayerPrefs.GetInt(WindowHeightKey, 900);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeSceneLoad()
        {
            if (Object.FindFirstObjectByType<DisplayModeController>() != null) return;
            var root = new GameObject("Display Mode Controller");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<DisplayModeController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (!IsAvailable) return; // Editor Play Mode must not change the desktop display.

            // Fullscreen is the first-launch default. A previous explicit choice wins.
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            ApplyMode();
        }

        private void Update()
        {
            if (!IsAvailable) return;
            if (Input.GetKeyDown(KeyCode.F11)) Toggle();

            // Unity also supports its own fullscreen shortcut. Keep our setting in sync
            // after an external display-mode change has finished applying.
            if (Time.unscaledTime < modeChangeDeadline) return;
            // MaximizedWindow still has a title bar and is not our borderless
            // fullscreen choice. Treat it as windowed for the setting and F11.
            var actualFullscreen = IsFullscreenMode(Screen.fullScreenMode);
            if (actualFullscreen == fullscreen) return;
            fullscreen = actualFullscreen;
            if (!fullscreen) RememberWindowSize();
            SavePreference();
        }

        public static void Toggle()
        {
            if (!IsAvailable || instance == null) return;
            if (!instance.fullscreen) RememberWindowSize();
            instance.fullscreen = !instance.fullscreen;
            instance.ApplyMode();
            instance.SavePreference();
        }

        public static void SetFullscreen(bool value)
        {
            if (!IsAvailable || instance == null || instance.fullscreen == value) return;
            if (!instance.fullscreen) RememberWindowSize();
            instance.fullscreen = value;
            instance.ApplyMode();
            instance.SavePreference();
        }

        public static void SetWindowSize(int width, int height)
        {
            FitWindowToDisplay(ref width, ref height);
            PlayerPrefs.SetInt(WindowWidthKey, width);
            PlayerPrefs.SetInt(WindowHeightKey, height);
            PlayerPrefs.Save();
            if (!IsAvailable || instance == null || instance.fullscreen) return;
            instance.modeChangeDeadline = Time.unscaledTime + 1f;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        public static Vector2Int FittedWindowSize(int width, int height)
        {
            FitWindowToDisplay(ref width, ref height);
            return new Vector2Int(width, height);
        }

        private static bool IsFullscreenMode(FullScreenMode mode)
        {
            return mode == FullScreenMode.FullScreenWindow ||
                mode == FullScreenMode.ExclusiveFullScreen;
        }

        private void ApplyMode()
        {
            modeChangeDeadline = Time.unscaledTime + 1f;
            if (fullscreen)
            {
                // Use the display's native aspect. Reusing the last 16:9
                // window resolution can letterbox a 16:10 fullscreen display.
                var display = Display.main;
                var resolution = Screen.currentResolution;
                var nativeWidth = display == null ? resolution.width : display.systemWidth;
                var nativeHeight = display == null ? resolution.height : display.systemHeight;
                Screen.SetResolution(nativeWidth, nativeHeight,
                    FullScreenMode.FullScreenWindow);
                return;
            }

            var width = PlayerPrefs.GetInt(WindowWidthKey, 1600);
            var height = PlayerPrefs.GetInt(WindowHeightKey, 900);
            FitWindowToDisplay(ref width, ref height);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        private static void FitWindowToDisplay(ref int width, ref int height)
        {
            width = Mathf.Clamp(width, 640, 8192);
            height = Mathf.Clamp(height, 360, 4320);
            var display = Display.main;
            if (display == null) return;
            var maxWidth = Mathf.Max(640, display.systemWidth - 120);
            var maxHeight = Mathf.Max(360, display.systemHeight - 120);
            var scale = Mathf.Min(1f,
                Mathf.Min(maxWidth / (float)width, maxHeight / (float)height));
            width = Mathf.Clamp(Mathf.RoundToInt(width * scale), 640, maxWidth);
            height = Mathf.Clamp(Mathf.RoundToInt(height * scale), 360, maxHeight);
        }

        private static void RememberWindowSize()
        {
            if (Screen.fullScreenMode != FullScreenMode.Windowed ||
                Screen.width < 640 || Screen.height < 360) return;
            PlayerPrefs.SetInt(WindowWidthKey, Screen.width);
            PlayerPrefs.SetInt(WindowHeightKey, Screen.height);
        }

        private void SavePreference()
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnApplicationQuit()
        {
            if (!IsAvailable || fullscreen) return;
            RememberWindowSize();
            PlayerPrefs.Save();
        }
    }
}
