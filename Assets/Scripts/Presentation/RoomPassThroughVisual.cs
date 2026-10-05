using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Visual-only furniture that must not close a doorway or block a ground-agent route.
    /// The furnished meshes themselves have no colliders; this also keeps the
    /// room-clearance audit consistent with the actual movement model.
    /// </summary>
    public sealed class RoomPassThroughVisual : MonoBehaviour
    {
    }
}
