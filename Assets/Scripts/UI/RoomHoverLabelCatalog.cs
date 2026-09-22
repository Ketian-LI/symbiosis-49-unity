using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    /// <summary>
    /// Loads the blank hover nameplate. Room names are rendered dynamically so
    /// the same asset supports both interface languages.
    /// </summary>
    public static class RoomHoverLabelCatalog
    {
        public const string ResourcePath = "UI/Hover/room-hover-nameplate-v01";

        private static Sprite cachedSprite;

        public static Sprite GetSprite()
        {
            if (cachedSprite != null)
            {
                return cachedSprite;
            }

            cachedSprite = Resources.Load<Sprite>(ResourcePath);
            if (cachedSprite != null)
            {
                return cachedSprite;
            }

            var texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                return null;
            }

            cachedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            cachedSprite.name = "Room Hover Nameplate";
            return cachedSprite;
        }
    }
}
