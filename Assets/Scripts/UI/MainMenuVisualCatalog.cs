using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum MainMenuVisual
    {
        BestRecordPanel,
        SandboxCard,
        SandboxIcon,
        ResearchCard,
        ResearchIcon,
        EnterArrow,
        BottomButtonBase,
        SettingsIcon,
        LanguageIcon,
        ExitIcon,
        TitleFootprints
    }

    public static class MainMenuVisualCatalog
    {
        private const string ResourceRoot = "UI/MainMenu/";

        private static readonly IReadOnlyDictionary<MainMenuVisual, string> ResourceNames =
            new Dictionary<MainMenuVisual, string>
            {
                { MainMenuVisual.BestRecordPanel, "main-menu-best-record-panel-v01" },
                { MainMenuVisual.SandboxCard, "main-menu-sandbox-card-v01" },
                { MainMenuVisual.SandboxIcon, "main-menu-sandbox-icon-v01" },
                { MainMenuVisual.ResearchCard, "main-menu-research-card-v01" },
                { MainMenuVisual.ResearchIcon, "main-menu-research-icon-v01" },
                { MainMenuVisual.EnterArrow, "main-menu-enter-arrow-v01" },
                { MainMenuVisual.BottomButtonBase, "main-menu-round-button-base-v01" },
                { MainMenuVisual.SettingsIcon, "main-menu-settings-icon-v01" },
                { MainMenuVisual.LanguageIcon, "main-menu-language-icon-v01" },
                { MainMenuVisual.ExitIcon, "main-menu-exit-icon-v01" },
                { MainMenuVisual.TitleFootprints, "main-menu-title-footprints-v01" }
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
