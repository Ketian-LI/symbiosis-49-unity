using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum MainMenuVisual
    {
        TabletopBackground,
        BestRecordPanel,
        SandboxCard,
        SandboxIcon,
        ResearchCard,
        ResearchIcon,
        ModeHoverCard,
        EnterArrow,
        BottomButtonBase,
        SettingsIcon,
        LanguageIcon,
        ExitIcon,
        TitleWordmark
    }

    public static class MainMenuVisualCatalog
    {
        private const string ResourceRoot = "UI/MainMenu/";

        private static readonly IReadOnlyDictionary<MainMenuVisual, string> ResourceNames =
            new Dictionary<MainMenuVisual, string>
            {
                { MainMenuVisual.TabletopBackground, "main-menu-tabletop-background-v01" },
                { MainMenuVisual.BestRecordPanel, "main-menu-best-record-panel-v01" },
                { MainMenuVisual.SandboxCard, "main-menu-endless-card-v02" },
                { MainMenuVisual.SandboxIcon, "main-menu-endless-icon-v02" },
                { MainMenuVisual.ResearchCard, "main-menu-timed-card-v02" },
                { MainMenuVisual.ResearchIcon, "main-menu-timed-icon-v02" },
                { MainMenuVisual.ModeHoverCard, "main-menu-mode-hover-card-v02" },
                { MainMenuVisual.EnterArrow, "main-menu-enter-arrow-v01" },
                { MainMenuVisual.BottomButtonBase, "main-menu-round-button-base-v01" },
                { MainMenuVisual.SettingsIcon, "main-menu-settings-icon-v01" },
                { MainMenuVisual.LanguageIcon, "main-menu-language-icon-v01" },
                { MainMenuVisual.ExitIcon, "main-menu-exit-icon-v01" },
                { MainMenuVisual.TitleWordmark, "main-menu-title-v04-rounded" }
            };

        private static readonly Dictionary<MainMenuVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(MainMenuVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(MainMenuVisual visual)
        {
            if (SpriteCache.TryGetValue(visual, out var cached) && cached != null)
            {
                return cached;
            }

            var path = ResourcePath(visual);
            Sprite sprite = null;
            if (visual == MainMenuVisual.TitleWordmark)
            {
                var wordmarkTexture = Resources.Load<Texture2D>(path);
                if (wordmarkTexture != null)
                {
                    // The edited transparent PNG includes empty canvas above and
                    // below the mark. Use normalized crop coordinates because
                    // Unity may downscale the imported texture per platform.
                    var width = wordmarkTexture.width;
                    var height = wordmarkTexture.height;
                    sprite = Sprite.Create(
                        wordmarkTexture,
                        new Rect(
                            width * (20f / 2169f),
                            height * (190f / 725f),
                            width * (2111f / 2169f),
                            height * (360f / 725f)),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = "Main Menu Title Wordmark v04 Rounded";
                }
            }
            sprite ??= Resources.Load<Sprite>(path);
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
                    sprite.name = $"Main Menu {visual}";
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
