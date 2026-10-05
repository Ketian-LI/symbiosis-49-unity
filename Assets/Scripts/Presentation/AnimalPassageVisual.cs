using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Wildlife links are shown only while planning; pedestrian routes use another graph.</summary>
    public static class AnimalPassageVisual
    {
        public static void Build(Transform parent, RoomSpec spec, float cellSize, float roomGap,
            Material material, HideFlags hideFlags)
        {
            var ports = AnimalPassageLayout.Ports(spec, 0);
            var overlayObject = new GameObject("Animal Passage Overlay") { hideFlags = hideFlags };
            overlayObject.transform.SetParent(parent, false);
            var overlay = overlayObject.AddComponent<AnimalPassageOverlay>();
            var renderers = new List<Renderer[]>();
            var tint = new Color(0.55f, 0.88f, 0.80f);
            var doorways = RoomShellLayout.CreateDoorways(spec.Width, spec.Height, cellSize, roomGap);
            foreach (var port in ports)
            {
                var slot = doorways.First(item => (int)port.Edge == (int)item.Edge &&
                                                   port.Segment == item.SegmentIndex);

                var inward = slot.Edge switch
                {
                    RoomEdge.North => Vector3.back,
                    RoomEdge.East => Vector3.left,
                    RoomEdge.South => Vector3.forward,
                    _ => Vector3.right
                };
                // Mark the animal-only doorway locally. A long line to the
                // centre falsely suggests that animals use the human road.
                var start = slot.LocalCenter - inward * (roomGap * 0.5f + 0.015f);
                var end = start + inward * 0.26f;
                start.y = end.y = 0.274f;
                var direction = end - start;
                var stroke = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                    $"Animal Passage {slot.Edge} {slot.SegmentIndex}", overlayObject.transform,
                    (start + end) * 0.5f, new Vector3(0.045f, 0.008f, direction.magnitude),
                    tint, material, true, hideFlags);
                stroke.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
                var marker = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cylinder,
                    $"Animal Passage Port {slot.Edge} {slot.SegmentIndex}", overlayObject.transform,
                    start + Vector3.up * 0.002f, new Vector3(0.15f, 0.006f, 0.15f),
                    tint, material, true, hideFlags);
                renderers.Add(new[] { stroke.GetComponent<Renderer>(), marker.GetComponent<Renderer>() });
            }
            overlay.Initialize(spec, renderers);
            overlayObject.SetActive(false);
        }
    }

    public sealed class AnimalPassageOverlay : MonoBehaviour
    {
        private static readonly Color Open = new(0.55f, 0.88f, 0.80f);
        private static readonly Color Disturbed = new(1f, 0.66f, 0.32f);
        private static readonly Color Unconnected = new(0.87f, 0.84f, 0.72f);
        private RoomSpec spec;
        private IReadOnlyList<Renderer[]> portRenderers;
        private HashSet<AnimalPassagePort> connectedPorts = new();
        private bool showUnconnectedPorts;
        private int previewTurns;
        private int previewDay;

        public void Initialize(RoomSpec roomSpec, IReadOnlyList<Renderer[]> renderers)
        {
            spec = roomSpec;
            portRenderers = renderers;
        }

        public void SetPreview(bool visible, int quarterTurns, int dayNumber)
        {
            if (spec == null || portRenderers == null) return;
            previewTurns = quarterTurns;
            previewDay = dayNumber;
            gameObject.SetActive(visible);
            if (!visible) return;
            RefreshLines();
        }

        public void SetConnectedPorts(IReadOnlyCollection<AnimalPassagePort> ports)
        {
            connectedPorts = ports != null
                ? new HashSet<AnimalPassagePort>(ports)
                : new HashSet<AnimalPassagePort>();
            if (gameObject.activeSelf) RefreshLines();
        }

        public void SetShowUnconnectedPorts(bool visible)
        {
            showUnconnectedPorts = visible;
            if (gameObject.activeSelf) RefreshLines();
        }

        private void RefreshLines()
        {
            var rotatedPorts = AnimalPassageLayout.Ports(spec, previewTurns);
            var routeEvent = AnimalRouteEventSchedule.ForDay(previewDay);
            for (var index = 0; index < portRenderers.Count; index++)
            {
                var blocked = routeEvent is { } active && active.Blocks(spec.Id, rotatedPorts[index]);
                var connected = connectedPorts.Contains(rotatedPorts[index]);
                var stroke = portRenderers[index][0];
                var marker = portRenderers[index][1];
                stroke.enabled = !blocked && connected;
                // Only the selected room shows unmatched exits. This reveals
                // the piece's shape without repeating marks across all 49 rooms.
                marker.enabled = blocked || showUnconnectedPorts && !connected;
                UrbanVisualFactory.ApplyColor(stroke, Open);
                UrbanVisualFactory.ApplyColor(marker, blocked ? Disturbed : Unconnected);
            }
        }
    }
}
