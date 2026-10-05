using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public static class TutorialPanelVisualCatalog
    {
        public const string ResourcePath = "UI/TutorialPanelV2";

        private static Sprite cachedSprite;

        public static Sprite GetSprite()
        {
            if (cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                return null;
            }

            // Crop the transparent outer margin. Unity sprite coordinates start
            // at the bottom left; the source artwork is 1983 x 793 pixels.
            var source = new Rect(70f, 119f, 1843f, 555f);
            if (texture.width < source.xMax || texture.height < source.yMax)
            {
                source = new Rect(0f, 0f, texture.width, texture.height);
            }
            cachedSprite = Sprite.Create(
                texture, source, new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect,
                new Vector4(34f, 28f, 34f, 28f));
            cachedSprite.name = "Tutorial Panel V2";
            return cachedSprite;
        }
    }
}
