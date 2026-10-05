using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public static class UrbanFontResolver
    {
        private static Font cachedFont;
        private static Font cachedBoldFont;

        public static Font GetFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            // A packaged font keeps English typography identical in editor and builds.
            // The importer chains Noto Sans SC for glyphs Nunito does not contain.
            cachedFont = Resources.Load<Font>("Fonts/Nunito-Regular");
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }

        public static Font GetFont(FontStyle style)
        {
            if (style != FontStyle.Bold)
            {
                return GetFont();
            }

            if (cachedBoldFont == null)
            {
                cachedBoldFont = Resources.Load<Font>("Fonts/Nunito-Bold");
            }

            return cachedBoldFont != null ? cachedBoldFont : GetFont();
        }
    }
}
