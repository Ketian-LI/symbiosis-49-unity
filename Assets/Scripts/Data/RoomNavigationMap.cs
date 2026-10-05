using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlifeRooms.Data
{
    /// <summary>
    /// Layout-derived room adjacency and safe floor interiors. The default graph
    /// represents physical doorways for legacy systems. Pedestrian road mode
    /// requires facing visible routes; animal mode uses separate hidden ports.
    /// </summary>
    public sealed class RoomNavigationMap
    {
        private readonly List<Node> nodes;
        private readonly Dictionary<string, Node> nodesById = new();
        private readonly Dictionary<string, List<string>> neighbours = new();
        private readonly Dictionary<(string, string), Vector2> restrictedConnections = new();
        private readonly Dictionary<(string, string), AnimalPassagePort> animalConnectionPorts = new();
        private readonly float cellSize;
        private readonly bool animalPassagesOnly;
        private readonly bool humanRoadsOnly;

        public RoomNavigationMap(
            IEnumerable<RoomPlacementData> placementData,
            IEnumerable<RoomSpec> roomSpecs,
            float gridCellSize, bool animalPassagesOnly = false,
            int animalDayNumber = 1, bool humanRoadsOnly = false,
            bool hungryWildlifeMayUsePedestrianDoors = false)
        {
            cellSize = gridCellSize;
            this.animalPassagesOnly = animalPassagesOnly;
            this.humanRoadsOnly = humanRoadsOnly;
            if (animalPassagesOnly && humanRoadsOnly)
                throw new System.ArgumentException("Choose animal passages or human roads, not both.");
            ActiveAnimalRouteEvent = animalPassagesOnly
                ? AnimalRouteEventSchedule.ForDay(animalDayNumber)
                : null;
            var specs = roomSpecs.ToDictionary(item => item.Id);
            var ports = new Dictionary<string, IReadOnlyList<AnimalPassagePort>>();
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
                if (animalPassagesOnly)
                {
                    var animalPorts = AnimalPassageLayout.Ports(spec, placement.quarterTurns);
                    var usablePorts = hungryWildlifeMayUsePedestrianDoors
                        ? animalPorts.Concat(HumanRoadLayout.Ports(spec, placement.quarterTurns))
                        : animalPorts.AsEnumerable();
                    ports[placement.id] = ActiveAnimalRouteEvent is { } routeEvent
                        ? usablePorts.Where(port => !routeEvent.Blocks(placement.id, port)).ToArray()
                        : usablePorts.ToArray();
                }
                else if (humanRoadsOnly)
                {
                    ports[placement.id] = HumanRoadLayout.Ports(spec, placement.quarterTurns);
                }
            }

            for (var first = 0; first < nodes.Count; first++)
            {
                for (var second = first + 1; second < nodes.Count; second++)
                {
                    if (!AreAdjacent(nodes[first], nodes[second]))
                    {
                        continue;
                    }

                    if (animalPassagesOnly || humanRoadsOnly)
                    {
                        if (!TryMatchPort(nodes[first], ports[nodes[first].Id],
                                nodes[second], ports[nodes[second].Id], out var point,
                                out var firstPort, out var secondPort))
                            continue;
                        restrictedConnections[(nodes[first].Id, nodes[second].Id)] = point;
                        restrictedConnections[(nodes[second].Id, nodes[first].Id)] = point;
                        if (animalPassagesOnly)
                        {
                            animalConnectionPorts[(nodes[first].Id, nodes[second].Id)] = firstPort;
                            animalConnectionPorts[(nodes[second].Id, nodes[first].Id)] = secondPort;
                        }
                    }

                    neighbours[nodes[first].Id].Add(nodes[second].Id);
                    neighbours[nodes[second].Id].Add(nodes[first].Id);
                }
            }
        }

        public int RoomCount => nodes.Count;
        public int ConnectionCount => neighbours.Values.Sum(items => items.Count) / 2;
        public AnimalRouteEvent? ActiveAnimalRouteEvent { get; }

        // The same hunger threshold must select the same doorway graph for
        // visible movement and for the food settlement at the end of a day.
        public static RoomNavigationMap ForWildlifeHunger(
            RoomNavigationMap ordinary, RoomNavigationMap desperate, int hungerDays) =>
            hungerDays >= 2 ? desperate : ordinary;

        public bool TryGetOrigin(string roomId, out int column, out int row)
        {
            if (nodesById.TryGetValue(roomId, out var node))
            {
                column = node.Column;
                row = node.Row;
                return true;
            }

            column = row = -1;
            return false;
        }

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
                !AreAdjacent(first, second) ||
                (animalPassagesOnly || humanRoadsOnly) &&
                !restrictedConnections.ContainsKey((firstRoomId, secondRoomId)))
            {
                return false;
            }

            if (animalPassagesOnly || humanRoadsOnly)
            {
                localBoardPoint = restrictedConnections[(firstRoomId, secondRoomId)];
                return true;
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

        // The planning overlay reads the exact port chosen by animal navigation,
        // so its visible lines cannot claim a connection that animals cannot use.
        public bool TryGetAnimalConnectionPort(string roomId, string neighbourId,
            out AnimalPassagePort port) =>
            animalConnectionPorts.TryGetValue((roomId, neighbourId), out port);

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

        private bool TryMatchPort(Node first, IReadOnlyList<AnimalPassagePort> firstPorts,
            Node second, IReadOnlyList<AnimalPassagePort> secondPorts, out Vector2 point,
            out AnimalPassagePort firstPort, out AnimalPassagePort secondPort)
        {
            foreach (var a in firstPorts)
            foreach (var b in secondPorts)
            {
                var boundary = 0;
                var segment = 0;
                var vertical = false;
                if (a.Edge == AnimalPassageEdge.East && b.Edge == AnimalPassageEdge.West &&
                    first.Column + first.Width == second.Column &&
                    first.Row + a.Segment == second.Row + b.Segment)
                {
                    boundary = second.Column;
                    segment = first.Row + a.Segment;
                    vertical = true;
                }
                else if (a.Edge == AnimalPassageEdge.West && b.Edge == AnimalPassageEdge.East &&
                         first.Column == second.Column + second.Width &&
                         first.Row + a.Segment == second.Row + b.Segment)
                {
                    boundary = first.Column;
                    segment = first.Row + a.Segment;
                    vertical = true;
                }
                else if (a.Edge == AnimalPassageEdge.South && b.Edge == AnimalPassageEdge.North &&
                         first.Row + first.Height == second.Row &&
                         first.Column + a.Segment == second.Column + b.Segment)
                {
                    boundary = second.Row;
                    segment = first.Column + a.Segment;
                }
                else if (a.Edge == AnimalPassageEdge.North && b.Edge == AnimalPassageEdge.South &&
                         first.Row == second.Row + second.Height &&
                         first.Column + a.Segment == second.Column + b.Segment)
                {
                    boundary = first.Row;
                    segment = first.Column + a.Segment;
                }
                else continue;

                var half = RoomLayoutData.GridSize * cellSize * 0.5f;
                point = vertical
                    ? new Vector2(-half + boundary * cellSize, half - (segment + 0.5f) * cellSize)
                    : new Vector2(-half + (segment + 0.5f) * cellSize, half - boundary * cellSize);
                firstPort = a;
                secondPort = b;
                return true;
            }

            point = default;
            firstPort = default;
            secondPort = default;
            return false;
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
