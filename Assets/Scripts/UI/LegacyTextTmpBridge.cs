using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanWildlifeRooms.UI
{
    /// <summary>
    /// Renders existing dynamic UI labels through SDF fonts while their Text
    /// components remain the data source for the current gameplay screens.
    /// InputField text is excluded because its caret depends on UI.Text.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class LegacyTextTmpBridge : MonoBehaviour
    {
        private static TMP_FontAsset chineseFont;
        private static TMP_FontAsset regularFont;
        private static TMP_FontAsset boldFont;
        private static bool lookedForFonts;

        private Text source;
        private TextMeshProUGUI rendered;
        private string previousText;
        private Font previousFont;
        private int previousSize = -1;
        private FontStyle previousStyle;
        private TextAnchor previousAlignment;
        private HorizontalWrapMode previousHorizontal;
        private VerticalWrapMode previousVertical;
        private float previousLineSpacing = float.NaN;
        private Color previousColor;
        private bool previousRichText;
        private bool previousBestFit;
        private int previousMinSize = -1;
        private int previousMaxSize = -1;
        private bool previousRaycast;
        private Outline trackedOutline;
        private bool previousOutlineEnabled;
        private Color previousOutlineColor;
        private float nextOutlineProbe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartMigration()
        {
            var root = new GameObject("SDF UI text migration");
            DontDestroyOnLoad(root);
            root.AddComponent<LegacyTextTmpCoordinator>();
        }

        internal static void Attach(Text label)
        {
            if (label == null || label.GetComponent<LegacyTextTmpBridge>() != null ||
                label.GetComponentInParent<TmpTextMigrationExclude>(true) != null ||
                label.GetComponentInParent<InputField>(true) != null ||
                !LoadFonts()) return;
            label.gameObject.AddComponent<LegacyTextTmpBridge>();
        }

        private static bool LoadFonts()
        {
            if (!lookedForFonts)
            {
                lookedForFonts = true;
                chineseFont = Resources.Load<TMP_FontAsset>(
                    "Fonts/TMP/NotoSansSC-Regular SDF");
                regularFont = Resources.Load<TMP_FontAsset>(
                    "Fonts/TMP/Nunito-Regular SDF");
                boldFont = Resources.Load<TMP_FontAsset>(
                    "Fonts/TMP/Nunito-Bold SDF");
                if (chineseFont == null || regularFont == null || boldFont == null)
                    Debug.LogError("SYMBIOSIS TMP fonts missing; keeping legacy text.");
            }
            return chineseFont != null && regularFont != null && boldFont != null;
        }

        private void Awake()
        {
            source = GetComponent<Text>();
            if (source == null || !LoadFonts()) return;

            var overlay = new GameObject("SDF text", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);
            overlay.transform.SetAsFirstSibling();
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rendered = overlay.AddComponent<TextMeshProUGUI>();
            rendered.margin = Vector4.zero;
            rendered.enableAutoSizing = false;
            Refresh(force: true);
            source.enabled = false;
        }

        private void LateUpdate()
        {
            if (source == null || rendered == null) return;
            Refresh(force: false);
            if (source.enabled) source.enabled = false;
        }

        private void Refresh(bool force)
        {
            if (force || previousFont != source.font ||
                previousStyle != source.fontStyle)
            {
                var name = source.font == null ? string.Empty : source.font.name;
                var chinese = name.IndexOf("NotoSansSC", StringComparison.OrdinalIgnoreCase) >= 0;
                var bold = name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    source.fontStyle == FontStyle.Bold ||
                    source.fontStyle == FontStyle.BoldAndItalic;
                rendered.font = chinese ? chineseFont : bold ? boldFont : regularFont;
                rendered.fontStyle = source.fontStyle switch
                {
                    FontStyle.Bold => chinese ? FontStyles.Bold : FontStyles.Normal,
                    FontStyle.Italic => FontStyles.Italic,
                    FontStyle.BoldAndItalic => chinese
                        ? FontStyles.Bold | FontStyles.Italic
                        : FontStyles.Italic,
                    _ => FontStyles.Normal
                };
                previousFont = source.font;
                previousStyle = source.fontStyle;
            }
            if (force || previousText != source.text)
            {
                rendered.text = source.text;
                previousText = source.text;
            }
            if (force || previousSize != source.fontSize ||
                previousBestFit != source.resizeTextForBestFit ||
                previousMinSize != source.resizeTextMinSize ||
                previousMaxSize != source.resizeTextMaxSize)
            {
                // Best Fit is reserved for changing numbers. Never let it
                // silently reduce a label to the old 10-11 pt range.
                rendered.enableAutoSizing = source.resizeTextForBestFit;
                rendered.fontSize = source.fontSize;
                if (source.resizeTextForBestFit)
                {
                    rendered.fontSizeMin = Mathf.Min(source.fontSize,
                        Mathf.Max(16, source.resizeTextMinSize));
                    rendered.fontSizeMax = Mathf.Max(source.fontSize,
                        source.resizeTextMaxSize);
                }
                previousSize = source.fontSize;
                previousBestFit = source.resizeTextForBestFit;
                previousMinSize = source.resizeTextMinSize;
                previousMaxSize = source.resizeTextMaxSize;
            }
            if (force || previousAlignment != source.alignment)
            {
                rendered.alignment = source.alignment switch
                {
                    TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                    TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                    TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                    TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                    TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                    TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                    TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                    TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                    TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                    _ => TextAlignmentOptions.Center
                };
                previousAlignment = source.alignment;
            }
            if (force || previousHorizontal != source.horizontalOverflow)
            {
                rendered.textWrappingMode =
                    source.horizontalOverflow == HorizontalWrapMode.Wrap
                        ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                previousHorizontal = source.horizontalOverflow;
            }
            if (force || previousVertical != source.verticalOverflow)
            {
                rendered.overflowMode =
                    source.verticalOverflow == VerticalWrapMode.Overflow
                        ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
                previousVertical = source.verticalOverflow;
            }
            if (force || !Mathf.Approximately(previousLineSpacing, source.lineSpacing))
            {
                rendered.lineSpacing = (source.lineSpacing - 1f) * source.fontSize;
                previousLineSpacing = source.lineSpacing;
            }
            if (force || previousColor != source.color)
            {
                rendered.color = source.color;
                previousColor = source.color;
            }
            if (force || previousRichText != source.supportRichText)
            {
                rendered.richText = source.supportRichText;
                previousRichText = source.supportRichText;
            }
            if (force || previousRaycast != source.raycastTarget)
            {
                // A few labels are themselves hover targets, so keep their
                // pointer hit area when the legacy Graphic stops rendering.
                rendered.raycastTarget = source.raycastTarget;
                previousRaycast = source.raycastTarget;
            }
            RefreshOutline(force);
        }

        private void RefreshOutline(bool force)
        {
            // Text factories can add an Outline after Attach has run. Probe
            // again on the first LateUpdate, then less often for labels without
            // one; labels with an Outline track its state every frame.
            if (trackedOutline == null &&
                (force || Time.unscaledTime >= nextOutlineProbe))
            {
                trackedOutline = GetComponent<Outline>();
                nextOutlineProbe = Time.unscaledTime +
                    (force ? 0f : 0.5f);
            }

            var enabled = trackedOutline != null && trackedOutline.enabled;
            if (!force && enabled == previousOutlineEnabled &&
                (!enabled || trackedOutline.effectColor == previousOutlineColor))
                return;

            rendered.outlineWidth = enabled ? 0.13f : 0f;
            if (enabled)
                rendered.outlineColor = trackedOutline.effectColor;
            previousOutlineEnabled = enabled;
            if (enabled)
                previousOutlineColor = trackedOutline.effectColor;
        }
    }

    /// <summary>Allows a legacy text field to remain for special QA comparisons.</summary>
    public sealed class TmpTextMigrationExclude : MonoBehaviour { }

    [DefaultExecutionOrder(9999)]
    internal sealed class LegacyTextTmpCoordinator : MonoBehaviour
    {
        private float nextScan;

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.5f;
            var canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null || !canvas.gameObject.scene.isLoaded) continue;
                foreach (var label in canvas.GetComponentsInChildren<Text>(true))
                    LegacyTextTmpBridge.Attach(label);
            }
        }
    }
}
