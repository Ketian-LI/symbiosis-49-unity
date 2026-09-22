using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    public sealed class AnimalNavigationCoordinator : MonoBehaviour
    {
        private readonly List<IWildlifeLayoutAgent> agents = new();

        private RoomLayoutEditorController layoutEditor;
        private Transform mapRoot;
        private float cellSize;

        public RoomNavigationMap NavigationMap { get; private set; }
        public int NavigationRevision { get; private set; }

        public void Initialize(
            IEnumerable<IWildlifeLayoutAgent> wildlifeAgents,
            RoomLayoutEditorController editor,
            Transform boardRoot,
            float gridCellSize)
        {
            agents.AddRange(wildlifeAgents);
            layoutEditor = editor;
            mapRoot = boardRoot;
            cellSize = gridCellSize;
            layoutEditor.LayoutConfirmed += HandleLayoutConfirmed;
            RebuildNavigation(false);
        }

        private void OnDestroy()
        {
            if (layoutEditor != null)
            {
                layoutEditor.LayoutConfirmed -= HandleLayoutConfirmed;
            }
        }

        private void HandleLayoutConfirmed()
        {
            RebuildNavigation(true);
        }

        private void RebuildNavigation(bool correctAnimalPositions)
        {
            NavigationMap = new RoomNavigationMap(
                layoutEditor.ExportLayout(),
                RoomLayoutData.All,
                cellSize);
            NavigationRevision++;

            if (!correctAnimalPositions)
            {
                return;
            }

            foreach (var agent in agents)
            {
                if (agent?.AgentTransform == null)
                {
                    continue;
                }

                var transform = agent.AgentTransform;
                var local = mapRoot.InverseTransformPoint(transform.position);
                var legal = NavigationMap.FindNearestLegalFloorPoint(
                    new Vector2(local.x, local.z),
                    ClearanceFor(agent.Species));
                var correctedLocal = new Vector3(legal.x, local.y, legal.y);
                if ((correctedLocal - local).sqrMagnitude < 0.0025f)
                {
                    continue;
                }

                agent.RelocateTo(mapRoot.TransformPoint(correctedLocal), 0.45f);
            }
        }

        private static float ClearanceFor(WildlifeSpecies species)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => 0.20f,
                WildlifeSpecies.Squirrel => 0.24f,
                WildlifeSpecies.Hedgehog => 0.18f,
                WildlifeSpecies.Fox => 0.38f,
                _ => 0.24f
            };
        }
    }
}
