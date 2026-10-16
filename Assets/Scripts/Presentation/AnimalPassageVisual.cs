using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Planning-only doorway marks for the two exclusive route networks.</summary>
    public static class AnimalPassageVisual
    {
        public static void Build(Transform parent, RoomSpec spec, float cellSize, float roomGap,
            Material material, HideFlags hideFlags)
        {
            var animalPorts = AnimalPassageLayout.Ports(spec, 0);
            var pedestrianPorts = HumanRoadLayout.Ports(spec, 0);
            var overlayObject = new GameObject("Animal Passage Overlay") { hideFlags = hideFlags };
            overlayObject.transform.SetParent(parent, false);
            var overlay = overlayObject.AddComponent<AnimalPassageOverlay>();
            var animalRenderers = new List<Renderer[]>();
            var pedestrianRenderers = new List<Renderer[]>();
            var doorways = RoomShellLayout.CreateDoorways(spec.Width, spec.Height, cellSize, roomGap);
            foreach (var port in animalPorts)
            {
                var slot = doorways.First(item => (int)port.Edge == (int)item.Edge &&
                                                   port.Segment == item.SegmentIndex);
                animalRenderers.Add(BuildMark(overlayObject.transform, slot, roomGap,
                    "Animal Passage", AnimalPassageOverlay.AnimalOpen, material, hideFlags));
            }
            foreach (var port in pedestrianPorts)
            {
                var slot = doorways.First(item => (int)port.Edge == (int)item.Edge &&
                                                   port.Segment == item.SegmentIndex);
                pedestrianRenderers.Add(BuildMark(overlayObject.transform, slot, roomGap,
                    "Pedestrian Passage", AnimalPassageOverlay.PedestrianOpen, material, hideFlags));
            }
            overlay.Initialize(spec, animalRenderers, pedestrianRenderers);
            overlayObject.SetActive(false);
        }

        private static Renderer[] BuildMark(Transform parent, RoomDoorwaySlot slot,
            float roomGap, string name, Color tint, Material material, HideFlags hideFlags)
        {
            var inward = slot.Edge switch
            {
                RoomEdge.North => Vector3.back,
                RoomEdge.East => Vector3.left,
                RoomEdge.South => Vector3.forward,
                _ => Vector3.right
            };
            // A short doorway stroke, not a room-wide line: two facing marks
            // visually meet only when the navigation graph has a real link.
            var start = slot.LocalCenter - inward * (roomGap * 0.5f + 0.015f);
            var end = start + inward * 0.26f;
            start.y = end.y = 0.274f;
            var direction = end - start;
            var stroke = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                $"{name} {slot.Edge} {slot.SegmentIndex}", parent,
                (start + end) * 0.5f, new Vector3(0.045f, 0.008f, direction.magnitude),
                tint, material, true, hideFlags);
            stroke.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            var marker = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cylinder,
                $"{name} Port {slot.Edge} {slot.SegmentIndex}", parent,
                start + Vector3.up * 0.002f, new Vector3(0.15f, 0.006f, 0.15f),
                tint, material, true, hideFlags);
            return new[] { stroke.GetComponent<Renderer>(), marker.GetComponent<Renderer>() };
        }
    }

    public sealed class AnimalPassageOverlay : MonoBehaviour
    {
        public static readonly Color AnimalOpen = new(0.55f, 0.88f, 0.80f);
        public static readonly Color PedestrianOpen = new(0.97f, 0.83f, 0.60f);
        private static readonly Color Disturbed = new(1f, 0.66f, 0.32f);
        private static readonly Color AnimalUnconnected = new(0.35f, 0.62f, 0.59f);
        private static readonly Color PedestrianUnconnected = new(0.66f, 0.55f, 0.39f);
        private RoomSpec spec;
        private IReadOnlyList<Renderer[]> animalPortRenderers;
        private IReadOnlyList<Renderer[]> pedestrianPortRenderers;
        private HashSet<AnimalPassagePort> connectedAnimalPorts = new();
        private HashSet<AnimalPassagePort> connectedPedestrianPorts = new();
        private bool showUnconnectedPorts;
        private int previewTurns;
        private int previewDay;

        public void Initialize(RoomSpec roomSpec, IReadOnlyList<Renderer[]> animalRenderers,
            IReadOnlyList<Renderer[]> pedestrianRenderers)
        {
            spec = roomSpec;
            animalPortRenderers = animalRenderers;
            pedestrianPortRenderers = pedestrianRenderers;
        }

        public void SetPreview(bool visible, int quarterTurns, int dayNumber)
        {
            if (spec == null || animalPortRenderers == null || pedestrianPortRenderers == null) return;
            previewTurns = quarterTurns;
            previewDay = dayNumber;
            gameObject.SetActive(visible);
            if (!visible) return;
            RefreshLines();
        }

        public void SetConnectedPorts(IReadOnlyCollection<AnimalPassagePort> ports)
        {
            connectedAnimalPorts = ports != null
                ? new HashSet<AnimalPassagePort>(ports)
                : new HashSet<AnimalPassagePort>();
            if (gameObject.activeSelf) RefreshLines();
        }

        public void SetConnectedPedestrianPorts(IReadOnlyCollection<AnimalPassagePort> ports)
        {
            connectedPedestrianPorts = ports != null
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
            var rotatedAnimalPorts = AnimalPassageLayout.Ports(spec, previewTurns);
            var routeEvent = AnimalRouteEventSchedule.ForDay(previewDay);
            for (var index = 0; index < animalPortRenderers.Count; index++)
            {
                var blocked = routeEvent is { } active && active.Blocks(spec.Id, rotatedAnimalPorts[index]);
                var connected = connectedAnimalPorts.Contains(rotatedAnimalPorts[index]);
                var stroke = animalPortRenderers[index][0];
                var marker = animalPortRenderers[index][1];
                stroke.enabled = !blocked && connected;
                // Only the selected room shows unmatched exits. This reveals
                // the piece's shape without repeating marks across all 49 rooms.
                marker.enabled = blocked || showUnconnectedPorts && !connected;
                UrbanVisualFactory.ApplyColor(stroke, AnimalOpen);
                UrbanVisualFactory.ApplyColor(marker, blocked ? Disturbed : AnimalUnconnected);
            }
            var rotatedPedestrianPorts = HumanRoadLayout.Ports(spec, previewTurns);
            for (var index = 0; index < pedestrianPortRenderers.Count; index++)
            {
                var connected = connectedPedestrianPorts.Contains(rotatedPedestrianPorts[index]);
                var stroke = pedestrianPortRenderers[index][0];
                var marker = pedestrianPortRenderers[index][1];
                stroke.enabled = connected;
                marker.enabled = showUnconnectedPorts && !connected;
                UrbanVisualFactory.ApplyColor(stroke, PedestrianOpen);
                UrbanVisualFactory.ApplyColor(marker, PedestrianUnconnected);
            }
        }
    }
}
