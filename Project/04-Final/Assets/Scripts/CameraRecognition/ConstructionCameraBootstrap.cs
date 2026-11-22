using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.CameraRecognition
{
    public sealed class ConstructionCameraBootstrap : MonoBehaviour
    {
        private static ConstructionCameraBootstrap instance;
        private float nextSearchAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // The bridge is an input choice, not a compile-time build variant.
            // The bootstrap remains inert in Fill and Endless.
            if (instance != null) return;
            var host = new GameObject("Construction Camera Recognition Bootstrap");
            instance = host.AddComponent<ConstructionCameraBootstrap>();
            DontDestroyOnLoad(host);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryAttach();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextSearchAt) return;
            nextSearchAt = Time.realtimeSinceStartup + 1f;
            TryAttach();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryAttach();
        }

        private static void TryAttach()
        {
            var controller = FindFirstObjectByType<ConstructionPrototypeController>(
                FindObjectsInactive.Include);
            if (controller == null || !controller.isActiveAndEnabled ||
                controller.Mode != ConstructionGameMode.BoardGame ||
                controller.GetComponent<ConstructionCameraUdpBridge>() != null) return;
            controller.gameObject.AddComponent<ConstructionCameraUdpBridge>();
        }
    }
}
