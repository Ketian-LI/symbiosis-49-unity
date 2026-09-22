using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlifeRooms.Data
{
    /// <summary>
    /// Layout-derived room adjacency and safe floor interiors.
    /// Ecological preferences can later filter this shared physical graph per species.
    /// </summary>
    public sealed class RoomNavigationMap
    {
        private readonly List<Node> nodes;
        private readonly Dictionary<string, Node> nodesById = new();
        private readonly Dictionary<string, List<string>> neighbours = new();
        private readonly float cellSize;

        public RoomNavigationMap(
            IEnumerable<RoomPlacementData> placementData,
            IEnumerable<RoomSpec> roomSpecs,
            float gridCellSize)
        {
            cellSize = gridCellSize;
            var specs = roomSpecs.ToDictionary(item => item.Id);
            nodes = new List<Node>();
            foreach (var placement in placementData)
            {
                if (!specs.TryGetValue(placement.id, out var spec))
                {
                    continue;
                }

                var rotated = Mathf.Abs(placement.quarterTurns) % 2 == 1;
                nodes.Add(new Node(
                    placement.id,
                    placement.column,
                    placement.row,
                    rotated ? spec.Height : spec.Width,
                    rotated ? spec.Width : spec.Height));
                nodesById[placement.id] = nodes[nodes.Count - 1];
                neighbours[placement.id] = new List<string>();
            }

            for (var first = 0; first < nodes.Count; first++)
            {
                for (var second = first + 1; second < nodes.Count; second++)
                {
                    if (!AreAdjacent(nodes[first], nodes[second]))
                    {
                        continue;
                    }

                    neighbours[nodes[first].Id].Add(nodes[second].Id);
                    neighbours[nodes[second].Id].Add(nodes[first].Id);
                }
            }
        }

        public int RoomCount => nodes.Count;

        public IReadOnlyList<string> NeighboursOf(string roomId)
        {
            return neighbours.TryGetValue(roomId, out var result)
                ? result
                : System.Array.Empty<string>();
        }

        public bool TryFindNearestReachable(
            string startRoomId,
            IEnumerable<string> candidateRoomIds,
            out string nearestRoomId,
            out int distance)
        {
            nearestRoomId = null;
            distance = -1;
            if (string.IsNullOrWhiteSpace(startRoomId) ||
                !neighbours.ContainsKey(startRoomId))
            {
                return false;
            }

            var candidates = new HashSet<string>(
                candidateRoomIds ?? System.Array.Empty<string>());
            candidates.RemoveWhere(item => !neighbours.ContainsKey(item));
            if (candidates.Count == 0)
            {
                return false;
            }

            var distances = new Dictionary<string, int>
            {
                [startRoomId] = 0
            };
            var queue = new Queue<string>();
            queue.Enqueue(startRoomId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbour in neighbours[current])
                {
                    if (distances.ContainsKey(neighbour))
                    {
                        continue;
                    }

                    distances[neighbour] = distances[current] + 1;
                    queue.Enqueue(neighbour);
                }
            }

            var match = candidates
                .Where(distances.ContainsKey)
                .Select(item => new { Id = item, Distance = distances[item] })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Id, System.StringComparer.Ordinal)
                .FirstOrDefault();
            if (match == null)
            {
                return false;
            }

            nearestRoomId = match.Id;
            distance = match.Distance;
            return true;
        }

        public bool TryFindRoomContaining(Vector2 localBoardPoint, out string roomId)
        {
            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            var column = (localBoardPoint.x + halfBoard) / cellSize;
            var row = (halfBoard - localBoardPoint.y) / cellSize;
            foreach (var node in nodes)
            {
                if (column >= node.Column && column <= node.Column + node.Width &&
                    row >= node.Row && row <= node.Row + node.Height)
                {
                    roomId = node.Id;
                    return true;
                }
            }

            roomId = string.Empty;
            return false;
        }

        public bool TryFindRoute(
            string startRoomId,
            string destinationRoomId,
            out IReadOnlyList<string> route)
        {
            return TryFindRoute(startRoomId, destinationRoomId, null, out route);
        }

        public bool TryFindRoute(
            string startRoomId,
            string destinationRoomId,
            ISet<string> excludedRoomIds,
            out IReadOnlyList<string> route)
        {
            route = System.Array.Empty<string>();
            if (!neighbours.ContainsKey(startRoomId) || !neighbours.ContainsKey(destinationRoomId))
            {
                return false;
            }

            var previous = new Dictionary<string, string>
            {
                [startRoomId] = null
            };
            var queue = new Queue<string>();
            queue.Enqueue(startRoomId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == destinationRoomId)
                {
                    break;
                }

                foreach (var neighbour in neighbours[current].OrderBy(id => id, System.StringComparer.Ordinal))
                {
                    if (previous.ContainsKey(neighbour) ||
                        excludedRoomIds != null && excludedRoomIds.Contains(neighbour) &&
                        neighbour != destinationRoomId)
                    {
                        continue;
                    }

                    previous[neighbour] = current;
                    queue.Enqueue(neighbour);
                }
            }

            if (!previous.ContainsKey(destinationRoomId))
            {
                return false;
            }

            var result = new List<string>();
            for (var current = destinationRoomId; current != null; current = previous[current])
            {
                result.Add(current);
            }
            result.Reverse();
            route = result;
            return true;
        }

        public bool TryGetConnectionPoint(
            string firstRoomId,
            string secondRoomId,
            out Vector2 localBoardPoint)
        {
            localBoardPoint = default;
            if (!nodesById.TryGetValue(firstRoomId, out var first) ||
                !nodesById.TryGetValue(secondRoomId, out var second) ||
                !AreAdjacent(first, second))
            {
                return false;
            }

            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            if (first.Column + first.Width == second.Column ||
                second.Column + second.Width == first.Column)
            {
                var boundaryColumn = first.Column + first.Width == second.Column
                    ? second.Column
                    : first.Column;
                var overlapStart = Mathf.Max(first.Row, second.Row);
                var overlapEnd = Mathf.Min(first.Row + first.Height, second.Row + second.Height);
                localBoardPoint = new Vector2(
                    -halfBoard + boundaryColumn * cellSize,
                    halfBoard - (overlapStart + overlapEnd) * 0.5f * cellSize);
                return true;
            }

            var boundaryRow = first.Row + first.Height == second.Row
                ? second.Row
                : first.Row;
            var horizontalStart = Mathf.Max(first.Column, second.Column);
            var horizontalEnd = Mathf.Min(first.Column + first.Width, second.Column + second.Width);
            localBoardPoint = new Vector2(
                -halfBoard + (horizontalStart + horizontalEnd) * 0.5f * cellSize,
                halfBoard - boundaryRow * cellSize);
            return true;
        }

        public Vector2 FindNearestLegalFloorPoint(Vector2 point, float wallClearance)
        {
            var bestPoint = point;
            var bestDistance = float.PositiveInfinity;
            var halfBoard = RoomLayoutData.GridSize * cellSize * 0.5f;
            foreach (var node in nodes)
            {
                var left = -halfBoard + node.Column * cellSize + wallClearance;
                var right = -halfBoard + (node.Column + node.Width) * cellSize - wallClearance;
                var top = halfBoard - node.Row * cellSize - wallClearance;
                var bottom = halfBoard - (node.Row + node.Height) * cellSize + wallClearance;
                var candidate = new Vector2(
                    Mathf.Clamp(point.x, left, right),
                    Mathf.Clamp(point.y, bottom, top));
                var distance = (candidate - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = candidate;
                }
            }

            return bestPoint;
        }

        private static bool AreAdjacent(Node first, Node second)
        {
            var horizontalTouch = first.Column + first.Width == second.Column ||
                                  second.Column + second.Width == first.Column;
            var verticalOverlap = first.Row < second.Row + second.Height &&
                                  first.Row + first.Height > second.Row;
            var verticalTouch = first.Row + first.Height == second.Row ||
                                second.Row + second.Height == first.Row;
            var horizontalOverlap = first.Column < second.Column + second.Width &&
                                    first.Column + first.Width > second.Column;
            return horizontalTouch && verticalOverlap || verticalTouch && horizontalOverlap;
        }

        private readonly struct Node
        {
            public Node(string id, int column, int row, int width, int height)
            {
                Id = id;
                Column = column;
                Row = row;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public int Column { get; }
            public int Row { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
