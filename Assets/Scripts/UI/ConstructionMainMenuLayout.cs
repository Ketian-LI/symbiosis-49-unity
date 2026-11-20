using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.UI
{
    // Reference-pixel positions on a 1920x1080 menu. The CanvasScaler may
    // expand on wider screens; centre anchors keep the complete composition
    // together while the additional width remains tabletop only.
    public static class ConstructionMainMenuLayout
    {
        private const string ArtworkRoot = "UI/ConstructionMainMenu/";
        private static readonly Dictionary<string, Sprite> CroppedSprites = new();
        private static Sprite plaqueShadowSprite;
        private static Font chineseModeFont;
        private static Font englishModeFont;

        public static readonly Rect Wordmark = new(680f, 45f, 560f, 115f);
        // Three equal plaques fit inside the illustrated case at 16:9 and
        // retain a single baseline above the round footer controls.
        public static readonly Rect FillMode = new(586f, 835f, 236f, 78f);
        public static readonly Rect EndlessMode = new(842f, 835f, 236f, 78f);
        public static readonly Rect BoardGameMode = new(1098f, 835f, 236f, 78f);
        // The macOS edition has two playable modes; center their plaques.
        public static Rect FillModeForPlatform =>
            Application.platform == RuntimePlatform.OSXPlayer
                ? new Rect(714f, 835f, 236f, 78f) : FillMode;
        public static Rect EndlessModeForPlatform =>
            Application.platform == RuntimePlatform.OSXPlayer
                ? new Rect(970f, 835f, 236f, 78f) : EndlessMode;
        // Four footer controls use one baseline and the same 164 px rhythm.
        public static readonly Rect Settings = new(658f, 935f, 112f, 100f);
        public static readonly Rect Language = new(822f, 935f, 112f, 100f);
        public static readonly Rect Leaderboard = new(986f, 935f, 112f, 100f);
        public static readonly Rect Exit = new(1150f, 935f, 112f, 100f);

        public static string ActionLabel(ConstructionSavedRunStatus status,
            bool chinese) => status switch
        {
            ConstructionSavedRunStatus.InProgress =>
                chinese ? "继续游戏" : "Continue Game",
            ConstructionSavedRunStatus.Finished =>
                chinese ? "查看结果" : "View Results",
            ConstructionSavedRunStatus.Corrupt =>
                chinese ? "存档异常" : "Save Issue",
            ConstructionSavedRunStatus.Unavailable =>
                chinese ? "无法读取存档" : "Save Unavailable",
            _ => chinese ? "开始游戏" : "Start Game"
        };

        public static string ModeLabel(ConstructionGameMode mode, bool chinese) =>
            mode switch
            {
                ConstructionGameMode.Endless =>
                    chinese ? "无尽模式" : "Endless Mode",
                ConstructionGameMode.BoardGame =>
                    chinese ? "桌游模式" : "Board Game",
                _ => chinese ? "填空模式" : "Fill Mode"
            };

        public static void ApplyModeFont(Text label, bool chinese, int size)
        {
            if (label == null) return;
            if (chinese)
                chineseModeFont ??= Resources.Load<Font>(
                    "Fonts/NotoSansSC-Regular") ??
                    UrbanFontResolver.GetFont(FontStyle.Bold);
            else
                englishModeFont ??= Resources.Load<Font>(
                    "Fonts/Nunito-Bold") ??
                    UrbanFontResolver.GetFont(FontStyle.Bold);
            label.font = chinese ? chineseModeFont : englishModeFont;
            label.fontStyle = FontStyle.Normal;
            label.fontSize = size;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public static void Place(RectTransform target, Rect referenceRect,
            bool resetScale = true)
        {
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = referenceRect.size;
            target.anchoredPosition = new Vector2(
                referenceRect.center.x - 960f,
                540f - referenceRect.center.y);
            if (resetScale) target.localScale = Vector3.one;
        }

        public static Image AddPlaqueArtwork(Transform parent)
        {
            var objectWithArtwork = new GameObject("Mode plaque artwork",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            objectWithArtwork.transform.SetParent(parent, false);
            var artwork = objectWithArtwork.GetComponent<Image>();
            artwork.sprite = LoadCroppedSprite(
                ArtworkRoot + "menu-mode-plaque-v02",
                new Rect(44f / 2172f, 134f / 724f,
                    2086f / 2172f, 470f / 724f));
            artwork.raycastTarget = false;
            // The plaque must fill the taller hit area; preserving the source
            // aspect ratio would leave the text outside its illustrated rim.
            artwork.preserveAspect = false;
            var rect = artwork.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return artwork;
        }

        public static Image AddPlaqueShadow(Transform parent)
        {
            var shadowObject = new GameObject("Soft plaque shadow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadowObject.transform.SetParent(parent, false);
            var shadow = shadowObject.GetComponent<Image>();
            shadow.sprite = PlaqueShadowSprite();
            shadow.raycastTarget = false;
            var rect = shadow.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-5f, -11f);
            rect.offsetMax = new Vector2(5f, 1f);
            shadowObject.transform.SetAsFirstSibling();
            return shadow;
        }

        private static Sprite PlaqueShadowSprite()
        {
            if (plaqueShadowSprite != null) return plaqueShadowSprite;
            const int width = 256;
            const int height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Soft mode plaque shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = new Vector2(x / (width - 1f) - 0.5f,
                        y / (height - 1f) - 0.5f);
                    const float radius = 0.08f;
                    var corner = new Vector2(Mathf.Abs(point.x) - 0.43f + radius,
                        Mathf.Abs(point.y) - 0.36f + radius);
                    var outside = new Vector2(Mathf.Max(0f, corner.x),
                        Mathf.Max(0f, corner.y));
                    var distance = outside.magnitude +
                        Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - radius;
                    var alpha = 0.34f * (1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(Mathf.Max(0f, distance) / 0.055f)));
                    texture.SetPixel(x, y,
                        new Color(0.075f, 0.055f, 0.045f, alpha));
                }
            }
            texture.Apply(false, false);
            plaqueShadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 100f);
            plaqueShadowSprite.name = "Soft plaque shadow - not a border";
            return plaqueShadowSprite;
        }

        public static Sprite TitleArtwork => LoadCroppedSprite(
            ArtworkRoot + "menu-title-lockup-v01",
            new Rect(62f / 2141f, 144f / 734f,
                2020f / 2141f, 400f / 734f));

        public static Sprite RoundControlBase => LoadCroppedSprite(
            "UI/MainMenu/main-menu-round-button-base-v01",
            new Rect(220f / 1278f, 200f / 1230f,
                840f / 1278f, 850f / 1230f));

        public static Sprite LeaderboardIcon
        {
            get
            {
                const string path = "UI/Awards/award-menu-trophy-v01";
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null) return sprite;
                var texture = Resources.Load<Texture2D>(path);
                return texture == null ? null : Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
        }

        public static Sprite FooterIcon(MainMenuVisual visual) => visual switch
        {
            MainMenuVisual.SettingsIcon => LoadCroppedSprite(
                "UI/ConstructionIcons/controls/settings",
                new Rect(280f / 1254f, 254f / 1254f,
                    690f / 1254f, 710f / 1254f)),
            MainMenuVisual.LanguageIcon => LoadCroppedSprite(
                "UI/ConstructionIcons/controls/language",
                new Rect(332f / 1254f, 332f / 1254f,
                    588f / 1254f, 590f / 1254f)),
            MainMenuVisual.ExitIcon => LoadCroppedSprite(
                "UI/MainMenu/main-menu-exit-icon-v01",
                new Rect(220f / 1254f, 154f / 1254f,
                    890f / 1254f, 955f / 1254f)),
            _ => null
        };

        private static Sprite LoadCroppedSprite(string path, Rect crop)
        {
            if (CroppedSprites.TryGetValue(path, out var cached) && cached != null)
                return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture,
                new Rect(texture.width * crop.x, texture.height * crop.y,
                    texture.width * crop.width, texture.height * crop.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = path.Substring(path.LastIndexOf('/') + 1);
            CroppedSprites[path] = sprite;
            return sprite;
        }
    }
}
