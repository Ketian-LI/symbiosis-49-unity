using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Releases a procedural mesh when an editor preview is rebuilt.</summary>
    [ExecuteAlways]
    public sealed class GeneratedMeshCleanup : MonoBehaviour
    {
        private Mesh mesh;

        public void Initialize(Mesh ownedMesh)
        {
            mesh = ownedMesh;
        }

        private void OnDestroy()
        {
            if (mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }

            mesh = null;
        }
    }
}
