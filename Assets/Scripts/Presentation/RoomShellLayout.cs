using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public enum RoomEdge
    {
        North,
        East,
        South,
        West
    }

    public readonly struct RoomDoorwaySlot
    {
        public RoomDoorwaySlot(RoomEdge edge, int segmentIndex, Vector3 localCenter)
        {
            Edge = edge;
            SegmentIndex = segmentIndex;
            LocalCenter = localCenter;
        }

        public RoomEdge Edge { get; }
        public int SegmentIndex { get; }
        public Vector3 LocalCenter { get; }
    }

    public readonly struct DoorClearanceZone
    {
        public DoorClearanceZone(RoomEdge edge, int segmentIndex, Vector2 localCenter, Vector2 size)
        {
            Edge = edge;
            SegmentIndex = segmentIndex;
            LocalCenter = localCenter;
            Size = size;
        }

        public RoomEdge Edge { get; }
        public int SegmentIndex { get; }
        public Vector2 LocalCenter { get; }
        public Vector2 Size { get; }

        public bool Overlaps(Vector2 boundsCenter, Vector2 boundsSize)
        {
            if (boundsSize.x < 0f || boundsSize.y < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(boundsSize));
            }

            const float contactTolerance = 0.001f;
            var combinedHalfWidth = (Size.x + boundsSize.x) * 0.5f;
            var combinedHalfDepth = (Size.y + boundsSize.y) * 0.5f;
            return Mathf.Abs(LocalCenter.x - boundsCenter.x) < combinedHalfWidth - contactTolerance &&
                   Mathf.Abs(LocalCenter.y - boundsCenter.y) < combinedHalfDepth - contactTolerance;
        }
    }

    public readonly struct RoomObstacle2D
    {
        public RoomObstacle2D(Vector2 localCenter, Vector2 size)
        {
            if (size.x < 0f || size.y < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            LocalCenter = localCenter;
            Size = size;
        }

        public Vector2 LocalCenter { get; }
        public Vector2 Size { get; }

        public bool Contains(Vector2 point, float padding)
        {
            return Mathf.Abs(point.x - LocalCenter.x) <= Size.x * 0.5f + padding &&
                   Mathf.Abs(point.y - LocalCenter.y) <= Size.y * 0.5f + padding;
        }
    }

    /// <summary>
    /// Describes the fixed doorway topology authored into a reusable room shell.
    /// Each occupied grid-cell edge contributes one centred doorway. Runtime
    /// navigation may disable an outward connection without changing this shell.
    /// </summary>
    public static class RoomShellLayout
    {
        public static IReadOnlyList<RoomDoorwaySlot> CreateDoorways(
            int widthCells,
            int heightCells,
            float cellSize,
            float roomGap)
        {
            if (widthCells <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(widthCells));
            }

            if (heightCells <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heightCells));
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            if (roomGap < 0f || roomGap >= cellSize)
            {
                throw new ArgumentOutOfRangeException(nameof(roomGap));
            }

            var width = widthCells * cellSize - roomGap;
            var depth = heightCells * cellSize - roomGap;
            var result = new List<RoomDoorwaySlot>((widthCells + heightCells) * 2);

            for (var column = 0; column < widthCells; column++)
            {
                var x = (column + 0.5f - widthCells * 0.5f) * cellSize;
                result.Add(new RoomDoorwaySlot(RoomEdge.North, column, new Vector3(x, 0f, depth * 0.5f)));
                result.Add(new RoomDoorwaySlot(RoomEdge.South, column, new Vector3(x, 0f, -depth * 0.5f)));
            }

            for (var row = 0; row < heightCells; row++)
            {
                var z = (heightCells * 0.5f - row - 0.5f) * cellSize;
                result.Add(new RoomDoorwaySlot(RoomEdge.West, row, new Vector3(-width * 0.5f, 0f, z)));
                result.Add(new RoomDoorwaySlot(RoomEdge.East, row, new Vector3(width * 0.5f, 0f, z)));
            }

            return result;
        }

        /// <summary>
        /// Creates the reserved walkable landing immediately inside every door.
        /// Static decoration and runtime-spawned objects must never overlap these
        /// zones. Navigation validation separately confirms that every landing can
        /// reach the room's internal circulation area.
        /// </summary>
        public static IReadOnlyList<DoorClearanceZone> CreateDoorClearances(
            int widthCells,
            int heightCells,
            float cellSize,
            float roomGap,
            float doorwayWidth,
            float clearanceDepth,
            float sideMargin)
        {
            if (doorwayWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(doorwayWidth));
            }

            if (clearanceDepth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(clearanceDepth));
            }

            if (sideMargin < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sideMargin));
            }

            var doorways = CreateDoorways(widthCells, heightCells, cellSize, roomGap);
            var clearanceWidth = doorwayWidth + sideMargin * 2f;
            var result = new List<DoorClearanceZone>(doorways.Count);

            foreach (var doorway in doorways)
            {
                Vector2 center;
                Vector2 size;

                switch (doorway.Edge)
                {
                    case RoomEdge.North:
                        center = new Vector2(doorway.LocalCenter.x, doorway.LocalCenter.z - clearanceDepth * 0.5f);
                        size = new Vector2(clearanceWidth, clearanceDepth);
                        break;
                    case RoomEdge.South:
                        center = new Vector2(doorway.LocalCenter.x, doorway.LocalCenter.z + clearanceDepth * 0.5f);
                        size = new Vector2(clearanceWidth, clearanceDepth);
                        break;
                    case RoomEdge.West:
                        center = new Vector2(doorway.LocalCenter.x + clearanceDepth * 0.5f, doorway.LocalCenter.z);
                        size = new Vector2(clearanceDepth, clearanceWidth);
                        break;
                    case RoomEdge.East:
                        center = new Vector2(doorway.LocalCenter.x - clearanceDepth * 0.5f, doorway.LocalCenter.z);
                        size = new Vector2(clearanceDepth, clearanceWidth);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                result.Add(new DoorClearanceZone(doorway.Edge, doorway.SegmentIndex, center, size));
            }

            return result;
        }

        /// <summary>
        /// Conservatively checks that a ground agent can travel between every
        /// doorway landing after furniture footprints are expanded by its radius.
        /// This supplements, rather than replaces, the final NavMesh agent test.
        /// </summary>
        public static bool AreDoorClearancesConnected(
            float roomWidth,
            float roomDepth,
            IReadOnlyList<DoorClearanceZone> clearances,
            IReadOnlyList<RoomObstacle2D> obstacles,
            float agentRadius,
            float sampleSpacing,
            out DoorClearanceZone unreachable)
        {
            if (roomWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(roomWidth));
            }

            if (roomDepth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(roomDepth));
            }

            if (clearances == null)
            {
                throw new ArgumentNullException(nameof(clearances));
            }

            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            if (agentRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(agentRadius));
            }

            if (sampleSpacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSpacing));
            }

            unreachable = default;
            if (clearances.Count <= 1)
            {
                return true;
            }

            const float wallInset = 0.07f;
            var minimum = new Vector2(
                -roomWidth * 0.5f + agentRadius + wallInset,
                -roomDepth * 0.5f + agentRadius + wallInset);
            var maximum = new Vector2(
                roomWidth * 0.5f - agentRadius - wallInset,
                roomDepth * 0.5f - agentRadius - wallInset);

            if (minimum.x >= maximum.x || minimum.y >= maximum.y)
            {
                unreachable = clearances[0];
                return false;
            }

            var columns = Mathf.Max(2, Mathf.CeilToInt((maximum.x - minimum.x) / sampleSpacing) + 1);
            var rows = Mathf.Max(2, Mathf.CeilToInt((maximum.y - minimum.y) / sampleSpacing) + 1);
            var blocked = new bool[columns, rows];
            var visited = new bool[columns, rows];

            Vector2 SamplePoint(int column, int row)
            {
                return new Vector2(
                    Mathf.Lerp(minimum.x, maximum.x, column / (float)(columns - 1)),
                    Mathf.Lerp(minimum.y, maximum.y, row / (float)(rows - 1)));
            }

            Vector2Int NearestCell(Vector2 point)
            {
                var x = Mathf.RoundToInt(Mathf.InverseLerp(minimum.x, maximum.x, point.x) * (columns - 1));
                var y = Mathf.RoundToInt(Mathf.InverseLerp(minimum.y, maximum.y, point.y) * (rows - 1));
                return new Vector2Int(Mathf.Clamp(x, 0, columns - 1), Mathf.Clamp(y, 0, rows - 1));
            }

            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    var point = SamplePoint(column, row);
                    for (var obstacleIndex = 0; obstacleIndex < obstacles.Count; obstacleIndex++)
                    {
                        if (!obstacles[obstacleIndex].Contains(point, agentRadius))
                        {
                            continue;
                        }

                        blocked[column, row] = true;
                        break;
                    }
                }
            }

            var start = NearestCell(clearances[0].LocalCenter);
            if (blocked[start.x, start.y])
            {
                unreachable = clearances[0];
                return false;
            }

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            var directions = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = current + direction;
                    if (next.x < 0 || next.x >= columns || next.y < 0 || next.y >= rows ||
                        blocked[next.x, next.y] || visited[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            for (var index = 1; index < clearances.Count; index++)
            {
                var target = NearestCell(clearances[index].LocalCenter);
                if (!blocked[target.x, target.y] && visited[target.x, target.y])
                {
                    continue;
                }

                unreachable = clearances[index];
                return false;
            }

            return true;
        }
    }
}
