namespace UrbanWildlifeRooms.Core
{
    public enum ApplicationBuildVariant
    {
        NoCamera,
        CameraRecognition
    }

    public static class BuildVariantSettings
    {
#if UNITY_EDITOR
        private static ApplicationBuildVariant? editorVariantOverride;

        public static void SetEditorVariantOverride(ApplicationBuildVariant? variant)
        {
            editorVariantOverride = variant;
        }
#endif

        public static ApplicationBuildVariant ActiveVariant
        {
            get
            {
#if UNITY_EDITOR
                if (editorVariantOverride.HasValue)
                {
                    return editorVariantOverride.Value;
                }
#endif
#if SYMBIOSIS_CAMERA_BUILD
                return ApplicationBuildVariant.CameraRecognition;
#else
                return ApplicationBuildVariant.NoCamera;
#endif
            }
        }

        public static bool UsesCameraRecognition => ActiveVariant == ApplicationBuildVariant.CameraRecognition;
        public static bool SupportsResearch => !UsesCameraRecognition;
    }
}
