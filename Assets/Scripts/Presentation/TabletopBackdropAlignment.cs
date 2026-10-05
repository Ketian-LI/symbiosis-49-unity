using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Keeps the photographed tabletop's baked perspective from being tilted
    /// a second time by the main-menu camera. The playable board is not moved.
    /// </summary>
    public sealed class TabletopBackdropAlignment : MonoBehaviour
    {
        private const float PerspectiveBackdropDistance = 90f;

        private Camera controlledCamera;
        private Vector3 gameplayPosition;
        private Quaternion gameplayRotation;
        private Vector3 gameplayScale;

        public void Initialize(Camera camera)
        {
            controlledCamera = camera;
            gameplayPosition = transform.position;
            gameplayRotation = transform.rotation;
            gameplayScale = transform.localScale;
            AlignNow();
        }

        private void LateUpdate()
        {
            AlignNow();
        }

        public void AlignNow()
        {
            if (controlledCamera == null)
            {
                return;
            }

            if (controlledCamera.orthographic)
            {
                transform.SetPositionAndRotation(gameplayPosition, gameplayRotation);
                transform.localScale = gameplayScale;
                return;
            }

            // The photograph already contains its own perspective. Keep it flat
            // to the screen and behind every room while the live board receives
            // the perspective projection. Scale it to preserve the framing of
            // the orthographic gameplay backdrop during the camera transition.
            var forward = controlledCamera.transform.forward;
            var distance = PerspectiveBackdropDistance;
            var halfHeightAtDistance = distance * Mathf.Tan(
                controlledCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var scale = halfHeightAtDistance / Mathf.Max(0.001f, controlledCamera.orthographicSize);
            transform.SetPositionAndRotation(
                controlledCamera.transform.position + forward * distance,
                Quaternion.FromToRotation(Vector3.up, -forward) * gameplayRotation);
            transform.localScale = gameplayScale * scale;
        }
    }
}
