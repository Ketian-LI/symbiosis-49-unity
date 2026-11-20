using System;

namespace UrbanWildlifeRooms.CameraRecognition
{
    [Serializable]
    public sealed class ConstructionCameraPacket
    {
        public int schemaVersion;
        public long sequence;
        public bool calibrated;
        public int cornerCount;
        public string error;
        public ConstructionCameraTilePacket[] tiles;
    }

    [Serializable]
    public sealed class ConstructionCameraTilePacket
    {
        public int markerId;
        public int column;
        public int row;
        public string category;
        public string planting;
        // Omitted by older detector packets; Standard is the compatible default.
        public string residenceKind;
    }
}
