using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class BoardCameraController : MonoBehaviour
    {
        private const float MenuTiltFromTopDownDegrees = 16f;
        private const float MenuCameraHeight = 55f;
        private const float MenuOverviewScale = 1.18f;

        public event System.Action OverviewRestored;
        public event System.Action FollowingStopped;

        private Camera controlledCamera;
        private Vector3 overviewPosition;
        private Quaternion overviewRotation;
        private float overviewSize;
        private Vector3 menuPosition;
        private Quaternion menuRotation;
        private float menuSize;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float targetSize;
        private Transform followTarget;
        private bool focused;
        private bool menuView;
        private bool viewTransitionActive;
        private float viewTransitionElapsed;
        private float viewTransitionDuration;
        private Vector3 transitionStartPosition;
        private Quaternion transitionStartRotation;
        private float transitionStartSize;
        private Vector3 transitionEndPosition;
        private Quaternion transitionEndRotation;
        private float transitionEndSize;
        private bool transitionEndsAtMenu;
        private Vector3 lastMousePosition;

        public bool IsFocused => focused;
        public bool IsMenuView => menuView;
        public bool IsFollowing => followTarget != null;
        public Vector3 TargetPosition => targetPosition;
        public float TargetSize => targetSize;

        public void Initialize(Camera camera)
        {
            controlledCamera = camera;
            overviewPosition = camera.transform.position;
            overviewRotation = camera.transform.rotation;
            overviewSize = camera.orthographicSize;
            ConfigureMenuView();
            targetPosition = overviewPosition;
            targetRotation = overviewRotation;
            targetSize = overviewSize;
        }

        private void ConfigureMenuView()
        {
            var overviewForward = overviewRotation * Vector3.forward;
            var focusPoint = overviewPosition;
            if (overviewForward.y < -0.001f)
            {
                var distanceToGround = overviewPosition.y / -overviewForward.y;
                focusPoint += overviewForward * distanceToGround;
            }

            var overviewEuler = overviewRotation.eulerAngles;
            menuRotation = Quaternion.Euler(
                90f - MenuTiltFromTopDownDegrees,
                overviewEuler.y,
                overviewEuler.z);
            var menuForward = menuRotation * Vector3.forward;
            var menuDistance = MenuCameraHeight / Mathf.Max(0.001f, -menuForward.y);
            menuPosition = focusPoint - menuForward * menuDistance;
            menuSize = overviewSize * MenuOverviewScale;
        }

        private static float PerspectiveFieldOfView(Camera camera, float halfHeightAtGround)
        {
            var forward = camera.transform.forward;
            var distanceToGround = camera.transform.position.y /
                Mathf.Max(0.001f, -forward.y);
            return 2f * Mathf.Atan(halfHeightAtGround / distanceToGround) * Mathf.Rad2Deg;
        }

        private void Update()
        {
            if (!Application.isPlaying || controlledCamera == null)
            {
                return;
            }

            if (viewTransitionActive)
            {
                UpdateViewTransition();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                ReturnToOverviewIfNeeded();
            }

            if (!menuView)
            {
                UpdateGameplayInput();
            }
            if (followTarget != null)
            {
                var focus = followTarget.position;
                targetPosition = focus + Vector3.up * Mathf.Max(10f, targetSize * 1.55f);
            }

            var interpolation = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f);
            controlledCamera.transform.position = Vector3.Lerp(controlledCamera.transform.position, targetPosition, interpolation);
            controlledCamera.transform.rotation = Quaternion.Slerp(controlledCamera.transform.rotation, targetRotation, interpolation);
            controlledCamera.orthographicSize = Mathf.Lerp(controlledCamera.orthographicSize, targetSize, interpolation);
            if (!controlledCamera.orthographic)
            {
                controlledCamera.fieldOfView = PerspectiveFieldOfView(controlledCamera, controlledCamera.orthographicSize);
            }
        }

        public void SetMenuViewImmediate()
        {
            if (controlledCamera == null)
            {
                return;
            }

            viewTransitionActive = false;
            focused = false;
            followTarget = null;
            menuView = true;
            targetPosition = menuPosition;
            targetRotation = menuRotation;
            targetSize = menuSize;
            controlledCamera.transform.position = menuPosition;
            controlledCamera.transform.rotation = menuRotation;
            controlledCamera.orthographicSize = menuSize;
            controlledCamera.fieldOfView = PerspectiveFieldOfView(controlledCamera, menuSize);
            controlledCamera.orthographic = false;
        }

        public void SetGameplayViewImmediate()
        {
            if (controlledCamera == null)
            {
                return;
            }

            viewTransitionActive = false;
            focused = false;
            followTarget = null;
            menuView = false;
            targetPosition = overviewPosition;
            targetRotation = overviewRotation;
            targetSize = overviewSize;
            controlledCamera.transform.position = overviewPosition;
            controlledCamera.transform.rotation = overviewRotation;
            controlledCamera.orthographicSize = overviewSize;
            controlledCamera.orthographic = true;
        }

        public void BeginGameplayViewTransition(float durationSeconds)
        {
            BeginViewTransition(
                overviewPosition,
                overviewRotation,
                overviewSize,
                false,
                durationSeconds);
        }

        public void BeginMenuViewTransition(float durationSeconds)
        {
            BeginViewTransition(
                menuPosition,
                menuRotation,
                menuSize,
                true,
                durationSeconds);
        }

        private void BeginViewTransition(
            Vector3 endPosition,
            Quaternion endRotation,
            float endSize,
            bool endsAtMenu,
            float durationSeconds)
        {
            if (controlledCamera == null)
            {
                return;
            }

            focused = false;
            followTarget = null;
            viewTransitionActive = true;
            viewTransitionElapsed = 0f;
            viewTransitionDuration = Mathf.Max(0.01f, durationSeconds);
            transitionStartPosition = controlledCamera.transform.position;
            transitionStartRotation = controlledCamera.transform.rotation;
            transitionStartSize = controlledCamera.orthographicSize;
            transitionEndPosition = endPosition;
            transitionEndRotation = endRotation;
            transitionEndSize = endSize;
            transitionEndsAtMenu = endsAtMenu;
            if (controlledCamera.orthographic)
            {
                controlledCamera.fieldOfView = PerspectiveFieldOfView(controlledCamera, transitionStartSize);
                controlledCamera.orthographic = false;
            }
        }

        private void UpdateViewTransition()
        {
            viewTransitionElapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(viewTransitionElapsed / viewTransitionDuration);
            var eased = progress * progress * (3f - 2f * progress);
            controlledCamera.transform.position = Vector3.LerpUnclamped(
                transitionStartPosition,
                transitionEndPosition,
                eased);
            controlledCamera.transform.rotation = Quaternion.SlerpUnclamped(
                transitionStartRotation,
                transitionEndRotation,
                eased);
            controlledCamera.orthographicSize = Mathf.LerpUnclamped(
                transitionStartSize,
                transitionEndSize,
                eased);
            controlledCamera.fieldOfView = PerspectiveFieldOfView(
                controlledCamera,
                controlledCamera.orthographicSize);

            if (progress < 1f)
            {
                return;
            }

            viewTransitionActive = false;
            menuView = transitionEndsAtMenu;
            if (!menuView)
            {
                controlledCamera.orthographic = true;
            }
            targetPosition = transitionEndPosition;
            targetRotation = transitionEndRotation;
            targetSize = transitionEndSize;
        }

        public void FocusRoom(Transform roomTransform, float footprintSize)
        {
            if (roomTransform == null)
            {
                return;
            }

            // A risk alert can be clicked while the menu-to-board transition is
            // still running. Let the explicit room focus take precedence.
            viewTransitionActive = false;
            menuView = false;
            if (controlledCamera != null)
            {
                controlledCamera.orthographic = true;
            }
            followTarget = null;
            focused = true;
            targetSize = Mathf.Clamp(footprintSize * 2.1f, 4.4f, 7.2f);
            targetRotation = overviewRotation;
            targetPosition = roomTransform.position + Vector3.up * Mathf.Max(13f, targetSize * 1.7f);
        }

        public void Follow(Transform target, float viewSize)
        {
            if (target == null)
            {
                return;
            }

            followTarget = target;
            focused = true;
            targetSize = Mathf.Clamp(viewSize, 1.2f, 5f);
            targetRotation = overviewRotation;
        }

        public bool ReturnToOverviewIfNeeded()
        {
            if (!focused)
            {
                return false;
            }

            focused = false;
            followTarget = null;
            targetPosition = overviewPosition;
            targetRotation = overviewRotation;
            targetSize = overviewSize;
            OverviewRestored?.Invoke();
            return true;
        }

        private void UpdateGameplayInput()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                var step = focused ? targetSize * 0.12f : 1.1f;
                targetSize = Mathf.Clamp(targetSize - scroll * step,
                    focused ? 1.2f : 9.5f, overviewSize);
            }

            if (Input.GetMouseButtonDown(2))
            {
                lastMousePosition = Input.mousePosition;
            }

            if (!Input.GetMouseButton(2))
            {
                return;
            }

            var delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            PanByPixels(new Vector2(delta.x, delta.y), controlledCamera.pixelHeight);
        }

        public bool PanByPixels(Vector2 delta, float viewportHeight)
        {
            if (controlledCamera == null || menuView || viewTransitionActive ||
                delta.sqrMagnitude < 0.01f)
            {
                return false;
            }

            if (followTarget != null)
            {
                // Keep the image steady when a drag switches from tracking an
                // animal to free camera movement at the same zoom level.
                followTarget = null;
                targetPosition = controlledCamera.transform.position;
                targetRotation = controlledCamera.transform.rotation;
                targetSize = controlledCamera.orthographicSize;
                FollowingStopped?.Invoke();
            }

            var right = targetRotation * Vector3.right;
            var up = Vector3.ProjectOnPlane(targetRotation * Vector3.up, Vector3.up).normalized;
            var scale = 2f * targetSize / Mathf.Max(1f, viewportHeight);
            var move = (-right * delta.x - up * delta.y) * scale;
            targetPosition += move;

            if (focused)
            {
                var halfBoard = RoomLayoutData.GridSize * WorldScaleStandards.CellSizeMeters * 0.5f;
                targetPosition.x = Mathf.Clamp(targetPosition.x,
                    overviewPosition.x - halfBoard, overviewPosition.x + halfBoard);
                targetPosition.z = Mathf.Clamp(targetPosition.z,
                    overviewPosition.z - halfBoard, overviewPosition.z + halfBoard);
            }
            else
            {
                var centerOffset = targetPosition - overviewPosition;
                centerOffset.y = 0f;
                centerOffset = Vector3.ClampMagnitude(centerOffset, 5.5f);
                targetPosition = new Vector3(
                    overviewPosition.x + centerOffset.x,
                    targetPosition.y,
                    overviewPosition.z + centerOffset.z);
            }
            return true;
        }

    }
}
