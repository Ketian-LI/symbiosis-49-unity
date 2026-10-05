using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // A seed bed rests at one fixed edge of the park; adjacent plaza rooms move.
    public sealed class ParkEdgeRestVisual : MonoBehaviour
    {
        private GameRuntimeController runtime;
        private GameObject west;
        private GameObject east;
        private GameObject south;
        private GameObject north;
        private int lastDay = int.MinValue;

        public void Initialize(GameRuntimeController controller, GameObject westMarker,
            GameObject eastMarker, GameObject southMarker, GameObject northMarker)
        {
            runtime = controller;
            west = westMarker;
            east = eastMarker;
            south = southMarker;
            north = northMarker;
            RefreshForDay(runtime?.Clock.DayNumber ?? 1);
        }

        public void RefreshForDay(int dayNumber)
        {
            lastDay = dayNumber;
            var hasRest = ParkEdgeRestSchedule.TryGetRestingCell(dayNumber,
                out var column, out var row);
            Set(west, hasRest && column == 1 && row == 2);
            Set(east, hasRest && column == 3 && row == 2);
            Set(south, hasRest && column == 2 && row == 3);
            Set(north, hasRest && column == 2 && row == 1);
        }

        private void Update()
        {
            if (!Application.isPlaying || runtime == null) return;
            var day = runtime.Clock.DayNumber;
            if (day != lastDay) RefreshForDay(day);
        }

        private static void Set(GameObject marker, bool visible)
        {
            if (marker != null && marker.activeSelf != visible)
                marker.SetActive(visible);
        }
    }
}
