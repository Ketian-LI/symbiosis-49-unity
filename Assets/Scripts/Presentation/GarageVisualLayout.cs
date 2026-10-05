using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Vehicle lane and portal positions shared by the static room and passing cars.</summary>
    public static class GarageVisualLayout
    {
        public static float LaneCenterX(int widthCells)
        {
            // Every production garage is now one cell wide. Keep the lane on
            // its centreline so the traffic opening fits inside the room.
            return 0f;
        }

        public static void BuildShellDetails(
            Transform parent,
            RoomSpec spec,
            float width,
            float depth,
            Material material,
            HideFlags hideFlags)
        {
            var laneX = LaneCenterX(spec.Width);
            var road = new Color(0.38f, 0.39f, 0.39f);
            var portal = new Color(0.065f, 0.075f, 0.08f);
            var stone = new Color(0.56f, 0.56f, 0.53f);

            // The garage concepts show brick-paved pedestrian bays on either
            // side of an uninterrupted vehicle lane. Garage A uses the warm
            // paving study; the other two use the cooler grey-brick study.
            // These are surface-only pieces so the floor remains the sole
            // clickable collider and the authored doorway routes do not move.
            var warmPaving = spec.Id == "garage-a";
            var grout = warmPaving
                ? new Color(0.63f, 0.46f, 0.38f)
                : new Color(0.51f, 0.52f, 0.53f);
            var brickA = warmPaving
                ? new Color(0.72f, 0.49f, 0.41f)
                : new Color(0.67f, 0.68f, 0.69f);
            var brickB = warmPaving
                ? new Color(0.67f, 0.45f, 0.37f)
                : new Color(0.62f, 0.63f, 0.65f);
            BuildPavedBay(-width * 0.5f + 0.12f, laneX - 0.51f, "West", grout, brickA, brickB);
            BuildPavedBay(laneX + 0.51f, width * 0.5f - 0.12f, "East", grout, brickA, brickB);

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

            void BuildPavedBay(float minX, float maxX, string side, Color joint, Color first, Color second)
            {
                var bayWidth = maxX - minX;
                if (bayWidth < 0.28f)
                {
                    return;
                }

                var centreX = (minX + maxX) * 0.5f;
                var bayDepth = depth - 0.24f;
                var edge = new Color(0.84f, 0.78f, 0.66f);
                Create($"{side} Paving Grout", new Vector3(centreX, 0.247f, 0f),
                    new Vector3(bayWidth, 0.012f, bayDepth), joint);
                Create($"{side} Paving Border Inboard", new Vector3(side == "West" ? maxX - 0.045f : minX + 0.045f, 0.256f, 0f),
                    new Vector3(0.09f, 0.006f, bayDepth), edge);
                for (var end = -1; end <= 1; end += 2)
                {
                    Create($"{side} Paving Border End {end}",
                        new Vector3(centreX, 0.256f, end * (bayDepth * 0.5f - 0.045f)),
                        new Vector3(bayWidth, 0.006f, 0.09f), edge);
                }

                var innerMinX = minX + (side == "East" ? 0.12f : 0.04f);
                var innerMaxX = maxX - (side == "West" ? 0.12f : 0.04f);
                var innerWidth = innerMaxX - innerMinX;
                var rows = Mathf.Max(1, Mathf.FloorToInt((bayDepth - 0.22f) / 0.23f));
                var rowLength = (bayDepth - 0.22f) / rows;
                for (var row = 0; row < rows; row++)
                {
                    var columns = Mathf.Max(1, Mathf.FloorToInt(innerWidth / 0.38f));
                    var columnWidth = innerWidth / columns;
                    var stagger = row % 2 == 0 ? 0f : columnWidth * 0.5f;
                    var z = -bayDepth * 0.5f + 0.11f + (row + 0.5f) * rowLength;
                    for (var column = 0; column <= columns; column++)
                    {
                        var left = innerMinX + column * columnWidth - stagger;
                        var right = Mathf.Min(innerMaxX, left + columnWidth);
                        left = Mathf.Max(innerMinX, left);
                        if (right - left < 0.06f)
                        {
                            continue;
                        }
                        Create($"{side} Paving Brick {row + 1}-{column + 1}",
                            new Vector3((left + right) * 0.5f, 0.257f, z),
                            new Vector3(right - left - 0.014f, 0.009f, rowLength - 0.014f),
                            (row + column) % 3 == 0 ? second : first);
                    }
                }
            }

            void Create(string name, Vector3 position, Vector3 scale, Color color)
            {
                UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube, name, parent,
                    position, scale, color, material, true, hideFlags);
            }
        }
    }
}
