using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum PauseMenuVisual
    {
        Panel,
        ButtonBase,
        ContinueIcon,
        RestartIcon,
        SettingsIcon,
        LanguageIcon,
        ReturnDesktopIcon,
        SettingsPanel,
        VolumeSliderTrack,
        VolumeSliderFill,
        VolumeSliderHandle,
        MuteIcon,
        RestoreSoundIcon,
        BackIcon,
        CameraRecalibrateIcon
    }

    public static class PauseMenuVisualCatalog
    {
        private const string ResourceRoot = "UI/PauseMenu/";

        private static readonly IReadOnlyDictionary<PauseMenuVisual, string> ResourceNames =
            new Dictionary<PauseMenuVisual, string>
            {
                { PauseMenuVisual.Panel, "pause-menu-panel-v01" },
                { PauseMenuVisual.ButtonBase, "pause-menu-button-base-v01" },
                { PauseMenuVisual.ContinueIcon, "pause-menu-continue-icon-v01" },
                { PauseMenuVisual.RestartIcon, "pause-menu-restart-icon-v01" },
                { PauseMenuVisual.SettingsIcon, "pause-menu-settings-icon-v01" },
                { PauseMenuVisual.LanguageIcon, "pause-menu-language-icon-v01" },
                { PauseMenuVisual.ReturnDesktopIcon, "pause-menu-return-desktop-icon-v01" },
                { PauseMenuVisual.SettingsPanel, "pause-menu-settings-panel-v01" },
                { PauseMenuVisual.VolumeSliderTrack, "pause-menu-volume-slider-track-v01" },
                { PauseMenuVisual.VolumeSliderFill, "pause-menu-volume-slider-fill-v01" },
                { PauseMenuVisual.VolumeSliderHandle, "pause-menu-volume-slider-handle-v01" },
                { PauseMenuVisual.MuteIcon, "pause-menu-mute-icon-v01" },
                { PauseMenuVisual.RestoreSoundIcon, "pause-menu-restore-sound-icon-v01" },
                { PauseMenuVisual.BackIcon, "pause-menu-back-icon-v01" },
                { PauseMenuVisual.CameraRecalibrateIcon, "pause-menu-camera-recalibrate-icon-v01" }
            };

        private static readonly Dictionary<PauseMenuVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(PauseMenuVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(PauseMenuVisual visual)
        {
            if (SpriteCache.TryGetValue(visual, out var cached) && cached != null)
            {
                return cached;
            }

            var path = ResourcePath(visual);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = $"Pause Menu {visual}";
                }
            }

            if (sprite != null)
            {
                SpriteCache[visual] = sprite;
            }

            return sprite;
        }
    }
}
