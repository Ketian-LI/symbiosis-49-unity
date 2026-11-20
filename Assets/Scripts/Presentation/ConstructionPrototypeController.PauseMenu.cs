using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private const string MasterVolumeKey = "symbiosis49.masterVolume";
        private const string MuteKey = "symbiosis49.muted";
        private readonly Vector2Int[] windowPresets =
        {
            new(1280, 720), new(1600, 900), new(1920, 1080)
        };
        private RectTransform pauseRoot;
        private GameObject pauseHomePage;
        private GameObject pauseSettingsPage;
        private Text pauseTitle;
        private Text pauseContinueLabel;
        private Text pauseSettingsLabel;
        private Text pauseMenuLabel;
        private Text pauseResetLabel;
        private Text pauseNotice;
        private Text settingsTitle;
        private Text settingsAudioHeading;
        private Text settingsDisplayHeading;
        private Text settingsMuteLabel;
        private Text settingsFullscreenLabel;
        private Text settingsWindowLabel;
        private Text settingsSizeLabel;
        private Text settingsSizeValue;
        private Text settingsWindowHint;
        private Text settingsBackLabel;
        private Button settingsSmallerButton;
        private Button settingsLargerButton;
        private readonly Text[] volumeCaptions = new Text[3];
        private readonly Text[] volumeValues = new Text[3];
        private readonly Slider[] volumeSliders = new Slider[3];

        private bool PauseOpen => pauseRoot != null && pauseRoot.gameObject.activeSelf;
        private bool PauseSettingsOpen => pauseSettingsPage != null &&
            pauseSettingsPage.activeSelf;

        private void BuildPauseMenu(Transform canvas)
        {
            pauseRoot = NewRect("Esc pause and settings", canvas,
                Vector2.zero, new Vector2(1600f, 900f));
            Stretch(pauseRoot);
            var shade = Panel("Pause dimmer", pauseRoot, Vector2.zero,
                new Vector2(1600f, 900f),
                new Color(0.04f, 0.08f, 0.10f, 0.82f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = true;

            pauseHomePage = NewRect("Pause actions", pauseRoot,
                Vector2.zero, new Vector2(560f, 720f)).gameObject;
            var homePaper = Panel("Pause paper", pauseHomePage.transform,
                Vector2.zero, new Vector2(560f, 720f), Paper);
            ApplySkin(homePaper, "UI/PauseMenu/pause-menu-panel-v01");
            homePaper.raycastTarget = false;
            pauseTitle = TextAt("Pause heading", pauseHomePage.transform,
                new Vector2(0f, 220f), new Vector2(390f, 54f), "", 32,
                true, Ink, TextAnchor.MiddleCenter);
            pauseTitle.raycastTarget = false;
            pauseContinueLabel = PauseAction("Continue", 135f, () => ClosePause());
            pauseSettingsLabel = PauseAction("Settings", 45f, OpenPauseSettings);
            pauseMenuLabel = PauseAction("Main menu", -45f, () =>
            {
                if (!ReturnToMainMenu()) ClosePause();
            });
            pauseResetLabel = PauseAction("New run", -135f, RestartFromPause);
            pauseNotice = TextAt("Pause notice", pauseHomePage.transform,
                new Vector2(0f, -240f), new Vector2(420f, 55f), "", 16,
                false, Ink, TextAnchor.MiddleCenter);
            pauseNotice.raycastTarget = false;

            pauseSettingsPage = NewRect("Sound and display settings", pauseRoot,
                Vector2.zero, new Vector2(1000f, 710f)).gameObject;
            var settingsPaper = Panel("Settings paper", pauseSettingsPage.transform,
                Vector2.zero, new Vector2(1000f, 710f), Paper);
            ApplySkin(settingsPaper, "UI/PauseMenu/pause-menu-settings-panel-v01");
            settingsPaper.raycastTarget = false;
            settingsTitle = TextAt("Settings heading", pauseSettingsPage.transform,
                new Vector2(0f, 192f), new Vector2(560f, 52f), "", 29,
                true, Ink, TextAnchor.MiddleCenter);
            settingsAudioHeading = TextAt("Sound heading", pauseSettingsPage.transform,
                new Vector2(-247f, 132f), new Vector2(350f, 42f), "", 23,
                true, Ink, TextAnchor.MiddleLeft);
            settingsDisplayHeading = TextAt("Display heading", pauseSettingsPage.transform,
                new Vector2(215f, 132f), new Vector2(350f, 42f), "", 23,
                true, Ink, TextAnchor.MiddleLeft);
            var divider = Panel("Settings divider", pauseSettingsPage.transform,
                new Vector2(0f, -5f), new Vector2(2f, 350f), Gold);
            divider.raycastTarget = false;
            BuildVolumeRow(0, 65f);
            BuildVolumeRow(1, -25f);
            BuildVolumeRow(2, -115f);
            var mute = ButtonAt("Mute sound", pauseSettingsPage.transform,
                new Vector2(-265f, -191f), new Vector2(260f, 52f),
                " ", 18, Ink, Paper, TogglePauseMute);
            settingsMuteLabel = mute.GetComponentInChildren<Text>();

            var fullscreen = ButtonAt("Borderless fullscreen", pauseSettingsPage.transform,
                new Vector2(120f, 65f), new Vector2(165f, 54f),
                " ", 16, Ink, Paper, () => SetPauseFullscreen(true));
            settingsFullscreenLabel = fullscreen.GetComponentInChildren<Text>();
            var windowed = ButtonAt("Windowed", pauseSettingsPage.transform,
                new Vector2(305f, 65f), new Vector2(165f, 54f),
                " ", 16, Ink, Paper, () => SetPauseFullscreen(false));
            settingsWindowLabel = windowed.GetComponentInChildren<Text>();
            settingsSizeLabel = TextAt("Window size caption", pauseSettingsPage.transform,
                new Vector2(215f, -7f), new Vector2(402f, 33f), "", 17,
                true, Ink, TextAnchor.MiddleLeft);
            settingsSmallerButton = ButtonAt("Smaller window", pauseSettingsPage.transform,
                new Vector2(55f, -65f), new Vector2(55f, 50f), "‹", 25,
                Ink, Paper, () => CycleWindowSize(-1));
            settingsSizeValue = TextAt("Selected window size",
                pauseSettingsPage.transform, new Vector2(215f, -65f),
                new Vector2(265f, 50f), "", 20, true, Ink,
                TextAnchor.MiddleCenter);
            settingsLargerButton = ButtonAt("Larger window", pauseSettingsPage.transform,
                new Vector2(375f, -65f), new Vector2(55f, 50f), "›", 25,
                Ink, Paper, () => CycleWindowSize(1));
            settingsWindowHint = TextAt("Window resizing hint", pauseSettingsPage.transform,
                new Vector2(215f, -145f), new Vector2(390f, 55f),
                L("You can also drag the window edge. F11 switches modes.",
                    "也可以拖动窗口边缘；F11 可切换模式。"), 14, false,
                Ink, TextAnchor.MiddleCenter);
            settingsWindowHint.raycastTarget = false;
            var back = ButtonAt("Back to pause", pauseSettingsPage.transform,
                new Vector2(0f, -245f), new Vector2(280f, 54f), " ", 19,
                Ink, Paper, ClosePauseSettings);
            settingsBackLabel = back.GetComponentInChildren<Text>();
            pauseSettingsPage.SetActive(false);
            pauseRoot.gameObject.SetActive(false);
            RefreshPauseLabels();
        }

        private Text PauseAction(string name, float y, System.Action action)
        {
            var button = ButtonAt(name, pauseHomePage.transform,
                new Vector2(0f, y), new Vector2(400f, 72f), " ", 23,
                Ink, Paper, action);
            return button.GetComponentInChildren<Text>();
        }

        private void BuildVolumeRow(int index, float y)
        {
            volumeCaptions[index] = TextAt("Volume caption " + index,
                pauseSettingsPage.transform, new Vector2(-255f, y + 25f),
                new Vector2(260f, 30f), "", 17, true, Ink,
                TextAnchor.MiddleLeft);
            volumeValues[index] = TextAt("Volume percent " + index,
                pauseSettingsPage.transform, new Vector2(-94f, y + 25f),
                new Vector2(80f, 30f), "", 17, true, Ink,
                TextAnchor.MiddleRight);
            var track = Panel("Volume track " + index,
                pauseSettingsPage.transform, new Vector2(-244f, y - 12f),
                new Vector2(300f, 10f), new Color(0.72f, 0.63f, 0.45f));
            var fill = Panel("Volume fill", track.transform, Vector2.zero,
                new Vector2(300f, 10f), new Color(0.22f, 0.38f, 0.32f));
            // Slider drives the fill anchors. Zero offsets keep the fill
            // inside the 300 px track instead of adding another 300 px.
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var handle = Panel("Volume handle", track.transform, Vector2.zero,
                new Vector2(20f, 25f), Ink);
            track.raycastTarget = true;
            fill.raycastTarget = false;
            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            var captured = index;
            slider.onValueChanged.AddListener(value => OnPauseVolume(captured, value));
            volumeSliders[index] = slider;
        }

        private void OnPauseVolume(int index, float value)
        {
            value = Mathf.Clamp01(value);
            switch (index)
            {
                case 0:
                    PlayerPrefs.SetFloat(MasterVolumeKey, value);
                    PlayerPrefs.Save();
                    ApplyPauseMasterVolume();
                    break;
                case 1: SymbiosisAudioDirector.SetMusicVolume(value); break;
                case 2: SymbiosisAudioDirector.SetEffectsVolume(value); break;
            }
            volumeValues[index].text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private static void ApplyPauseMasterVolume()
        {
            AudioListener.volume = PlayerPrefs.GetInt(MuteKey, 0) != 0
                ? 0f : Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f));
        }

        private void TogglePauseMute()
        {
            PlayerPrefs.SetInt(MuteKey, PlayerPrefs.GetInt(MuteKey, 0) == 0 ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPauseMasterVolume();
            RefreshPauseSettings();
        }

        private void SetPauseFullscreen(bool fullscreen)
        {
            DisplayModeController.SetFullscreen(fullscreen);
            RefreshPauseSettings();
        }

        private void CycleWindowSize(int step)
        {
            if (DisplayModeController.IsFullscreen) return;
            var current = new Vector2Int(DisplayModeController.WindowWidth,
                DisplayModeController.WindowHeight);
            var available = new System.Collections.Generic.List<Vector2Int>();
            foreach (var preset in windowPresets)
            {
                var fitted = DisplayModeController.FittedWindowSize(preset.x, preset.y);
                if (!available.Contains(fitted)) available.Add(fitted);
            }
            var index = available.IndexOf(current);
            if (index < 0) index = 0;
            index = (index + step + available.Count) % available.Count;
            DisplayModeController.SetWindowSize(available[index].x,
                available[index].y);
            RefreshPauseSettings();
        }

        private void RefreshPauseSettings()
        {
            if (pauseSettingsPage == null) return;
            var values = new[]
            {
                Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f)),
                SymbiosisAudioDirector.MusicVolume,
                SymbiosisAudioDirector.EffectsVolume
            };
            for (var i = 0; i < volumeSliders.Length; i++)
            {
                volumeSliders[i].SetValueWithoutNotify(values[i]);
                volumeValues[i].text = Mathf.RoundToInt(values[i] * 100f) + "%";
            }
            settingsMuteLabel.text = PlayerPrefs.GetInt(MuteKey, 0) != 0
                ? L("Unmute", "取消静音") : L("Mute", "静音");
            settingsFullscreenLabel.text = L("Fullscreen", "全屏") +
                (DisplayModeController.IsFullscreen ? "  ✓" : "");
            settingsWindowLabel.text = L("Window", "窗口") +
                (!DisplayModeController.IsFullscreen ? "  ✓" : "");
            settingsSizeValue.text = DisplayModeController.WindowWidth + " × " +
                DisplayModeController.WindowHeight;
            settingsSizeValue.color = DisplayModeController.IsFullscreen
                ? new Color(0.43f, 0.43f, 0.40f) : Ink;
            settingsSmallerButton.interactable = !DisplayModeController.IsFullscreen;
            settingsLargerButton.interactable = !DisplayModeController.IsFullscreen;
        }

        private void RefreshPauseLabels()
        {
            if (pauseRoot == null) return;
            pauseTitle.text = L("PAUSED", "暂停");
            pauseContinueLabel.text = L("Continue", "继续游戏");
            pauseSettingsLabel.text = L("Settings", "设置");
            pauseMenuLabel.text = L("Main menu", "主菜单");
            pauseResetLabel.text = confirmReset
                ? L("Confirm restart", "确认重新开始")
                : L("New run", "重新开始");
            settingsTitle.text = L("SETTINGS", "设置");
            settingsAudioHeading.text = L("Sound", "声音");
            settingsDisplayHeading.text = L("Display", "画面");
            volumeCaptions[0].text = L("Overall", "总音量");
            volumeCaptions[1].text = L("Music", "背景音乐");
            volumeCaptions[2].text = L("Effects", "操作音效");
            settingsSizeLabel.text = L("Window size", "窗口大小");
            settingsWindowHint.text = L(
                "You can also drag the window edge. F11 switches modes.",
                "也可以拖动窗口边缘；F11 可切换模式。");
            settingsBackLabel.text = L("Back", "返回");
            RefreshPauseSettings();
        }

        private void TogglePauseMenu()
        {
            if (pauseRoot == null || !IsBoardCaseOpen ||
                specialTaskSelectionRoot != null &&
                    specialTaskSelectionRoot.gameObject.activeSelf ||
                endlessRulesOverlay != null && endlessRulesOverlay.activeSelf)
                return;
            if (PauseSettingsOpen) { ClosePauseSettings(); return; }
            if (PauseOpen) { ClosePause(); return; }
            if (QuickGuideOpen)
            {
                ToggleQuickGuide();
                return;
            }
            pauseNotice.text = L("Esc closes this menu.", "再按 Esc 返回游戏。");
            RefreshPauseLabels();
            pauseHomePage.SetActive(true);
            pauseSettingsPage.SetActive(false);
            pauseRoot.gameObject.SetActive(true);
            pauseRoot.SetAsLastSibling();
            HideTooltip();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        private void ClosePause()
        {
            ClearResetConfirmation();
            pauseRoot.gameObject.SetActive(false);
            pauseSettingsPage.SetActive(false);
            pauseHomePage.SetActive(true);
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
        }

        private void OpenPauseSettings()
        {
            pauseHomePage.SetActive(false);
            pauseSettingsPage.SetActive(true);
            RefreshPauseLabels();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
        }

        private void ClosePauseSettings()
        {
            pauseSettingsPage.SetActive(false);
            pauseHomePage.SetActive(true);
            SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
        }

        private void RestartFromPause()
        {
            var oldRun = run?.RunId;
            if (IsEndless)
            {
                pauseRoot.gameObject.SetActive(false);
                OnResetPressed();
                return;
            }
            OnResetPressed();
            if (run?.RunId != oldRun)
            {
                pauseRoot.gameObject.SetActive(false);
                TryShowOpeningQuickGuide();
                return;
            }
            pauseResetLabel.text = confirmReset
                ? L("Confirm restart", "确认重新开始")
                : L("New run", "重新开始");
            pauseNotice.text = confirmReset
                ? L("Press again to erase this save.", "再按一次将清除当前存档。")
                : feedbackText != null ? feedbackText.text : string.Empty;
        }
    }
}
