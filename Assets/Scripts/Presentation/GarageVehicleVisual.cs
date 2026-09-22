using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class GarageVehicleVisual : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 end;
        private float duration;
        private float elapsed;

        public void Initialize(Vector3 localStart, Vector3 localEnd, float travelDuration)
        {
            start = localStart;
            end = localEnd;
            duration = Mathf.Max(0.2f, travelDuration);
            transform.localPosition = start;
            var direction = end - start;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
