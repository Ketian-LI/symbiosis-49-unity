using System;
using System.Linq;
using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public static class UrbanFontResolver
    {
        private static Font cachedFont;

        public static Font GetFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            var installedFonts = Font.GetOSInstalledFontNames();
            var preferredFonts = new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "DengXian",
                "SimHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            };

            var selected = preferredFonts.FirstOrDefault(preferred =>
                installedFonts.Any(installed => string.Equals(installed, preferred, StringComparison.OrdinalIgnoreCase)));

            cachedFont = !string.IsNullOrEmpty(selected)
                ? Font.CreateDynamicFontFromOSFont(selected, 64)
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (cachedFont != null)
            {
                cachedFont.hideFlags = HideFlags.HideAndDontSave;
            }

            return cachedFont;
        }
    }
}

