using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Keeps the photographed tabletop's baked perspective from being tilted
    /// a second time by the main-menu camera. The playable board is not moved.
    /// </summary>
    public sealed class TabletopBackdropAlignment : MonoBehaviour
    {
        // Farther than the closed case at the longer menu-camera distance.
        private const float PerspectiveBackdropDistance = 220f;

        private Camera controlledCamera;
        private Vector3 gameplayPosition;
        private Quaternion gameplayRotation;
        private Vector3 gameplayScale;
        private bool hasGameplayOverride;
        private Vector3 gameplayOverridePosition;
        private Vector3 gameplayOverrideScale;

        public void SetGameplayOverride(Vector3 position, Vector3 scale)
        {
            hasGameplayOverride = true;
            gameplayOverridePosition = position;
            gameplayOverrideScale = scale;
            AlignNow();
        }

        public void ClearGameplayOverride()
        {
            hasGameplayOverride = false;
            AlignNow();
        }

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
                transform.SetPositionAndRotation(hasGameplayOverride
                    ? gameplayOverridePosition : gameplayPosition,
                    gameplayRotation);
                transform.localScale = hasGameplayOverride
                    ? gameplayOverrideScale : gameplayScale;
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
            // The gameplay plane was authored smaller than the menu viewport.
            // Fit its photographed image as a cover, not a centred postcard:
            // the live case and UI can then remain on the same continuous desk.
            var planeWidth = 10f * gameplayScale.x * scale;
            var planeHeight = 10f * gameplayScale.z * scale;
            var cover = Mathf.Max(
                2f * halfHeightAtDistance * controlledCamera.aspect /
                    Mathf.Max(0.001f, planeWidth),
                2f * halfHeightAtDistance /
                    Mathf.Max(0.001f, planeHeight)) * 1.025f;
            transform.SetPositionAndRotation(
                controlledCamera.transform.position + forward * distance,
                Quaternion.FromToRotation(Vector3.up, -forward) * gameplayRotation);
            transform.localScale = gameplayScale * scale * cover;
        }
    }
}
