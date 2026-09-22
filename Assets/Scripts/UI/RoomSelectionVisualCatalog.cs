using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    /// <summary>
    /// Loads the approved detached room-selection corner. A single sprite is
    /// rotated four times so selection remains separate from room artwork.
    /// </summary>
    public static class RoomSelectionVisualCatalog
    {
        public const string CornerResourcePath = "UI/Selection/room-selection-corner-v01";

        private static Sprite cachedCorner;

        public static Sprite GetCornerSprite()
        {
            if (cachedCorner != null)
            {
                return cachedCorner;
            }

            cachedCorner = Resources.Load<Sprite>(CornerResourcePath);
            if (cachedCorner != null)
            {
                return cachedCorner;
            }

            var texture = Resources.Load<Texture2D>(CornerResourcePath);
            if (texture == null)
            {
                return null;
            }

            cachedCorner = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1000f,
                0,
                SpriteMeshType.FullRect);
            cachedCorner.name = "Room Selection Corner";
            return cachedCorner;
        }
    }
}
