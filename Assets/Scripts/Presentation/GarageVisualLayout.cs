using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Vehicle lane and portal positions shared by the static room and passing cars.</summary>
    public static class GarageVisualLayout
    {
        public static float LaneCenterX(int widthCells)
        {
            return widthCells == 1 ? -0.90f : 0f;
        }

        public static void BuildShellDetails(
            Transform parent,
            RoomSpec spec,
            float depth,
            Material material,
            HideFlags hideFlags)
        {
            var laneX = LaneCenterX(spec.Width);
            var road = new Color(0.38f, 0.39f, 0.39f);
            var portal = new Color(0.065f, 0.075f, 0.08f);
            var stone = new Color(0.56f, 0.56f, 0.53f);

            // These are flush, collider-free shell surfaces, not furniture obstacles.
            Create("Single Vehicle Lane", new Vector3(laneX, 0.247f, 0f),
                new Vector3(0.88f, 0.012f, depth - 0.22f), road);

            for (var end = -1; end <= 1; end += 2)
            {
                var insideZ = end * (depth * 0.5f - 0.12f);
                Create($"Vehicle Tunnel Floor {end}", new Vector3(laneX, 0.257f, insideZ),
                    new Vector3(0.80f, 0.012f, 0.24f), portal);
                Create($"Vehicle Tunnel Opening {end}", new Vector3(laneX, 0.40f, end * (depth * 0.5f - 0.082f)),
                    new Vector3(0.80f, 0.27f, 0.024f), portal);
                Create($"Vehicle Tunnel Lintel {end}", new Vector3(laneX, 0.55f, end * (depth * 0.5f - 0.091f)),
                    new Vector3(0.86f, 0.035f, 0.045f), stone);
            }

            void Create(string name, Vector3 position, Vector3 scale, Color color)
            {
                UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube, name, parent,
                    position, scale, color, material, true, hideFlags);
            }
        }
    }
}
