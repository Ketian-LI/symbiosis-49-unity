using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Animals
{
    /// <summary>
    /// Connects the replaceable pigeon demo agent to the shared board camera.
    /// Camera input and Esc handling live in the board/runtime controllers.
    /// </summary>
    public sealed class PigeonDemoDirector : MonoBehaviour
    {
        private BoardCameraController boardCamera;
        private readonly List<PigeonDemoAgent> pigeons = new();
        private PigeonDemoAgent selectedPigeon;

        public void Initialize(
            BoardCameraController cameraController,
            IEnumerable<PigeonDemoAgent> agents)
        {
            boardCamera = cameraController;
            pigeons.AddRange((agents ?? Array.Empty<PigeonDemoAgent>()).Where(item => item != null));
            if (boardCamera != null)
            {
                boardCamera.OverviewRestored += StopFollowing;
            }

            foreach (var pigeon in pigeons)
            {
                pigeon.Clicked += StartFollowing;
            }
        }

        private void Update()
        {
            if (Application.isPlaying && selectedPigeon != null && Input.GetKeyDown(KeyCode.K))
            {
                selectedPigeon.Kill();
            }
        }

        private void OnDestroy()
        {
            foreach (var pigeon in pigeons)
            {
                pigeon.Clicked -= StartFollowing;
            }

            if (boardCamera != null)
            {
                boardCamera.OverviewRestored -= StopFollowing;
            }
        }

        private void StartFollowing(PigeonDemoAgent selectedPigeon)
        {
            if (selectedPigeon == null || boardCamera == null)
            {
                return;
            }

            if (this.selectedPigeon != null && this.selectedPigeon != selectedPigeon)
            {
                this.selectedPigeon.SetSelected(false);
            }
            this.selectedPigeon = selectedPigeon;
            this.selectedPigeon.SetSelected(true);
            boardCamera.Follow(this.selectedPigeon.transform, 1.45f);
        }

        private void StopFollowing()
        {
            if (selectedPigeon != null)
            {
                selectedPigeon.SetSelected(false);
                selectedPigeon = null;
            }
        }
    }
}
