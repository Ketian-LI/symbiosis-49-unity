using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    // Visible pedestrian trails; animal routes remain in the planning overlay.
    public static class GroundTrailVisualBuilder
    {
        private static readonly int InkStrengthId = Shader.PropertyToID("_InkStrength");
        private static readonly float[] Stations = { 0f, 0.16f, 0.34f, 0.52f, 0.70f, 0.86f, 1f };
        private static readonly float[] Widths = { 1.15f, 1.08f, 1.01f, 0.96f, 1.02f, 1.05f, 1f };
        private static readonly float[] Bends = { 0f, 0.28f, 0.58f, 0.76f, 0.68f, 0.42f, 0f };

        public static void Build(Transform parent, RoomSpec spec, float cellSize,
            float roomGap, Color surfaceColor, float width, Material material, HideFlags hideFlags)
        {
            var slots = RoomShellLayout.CreateDoorways(spec.Width, spec.Height, cellSize, roomGap);
            var ports = HumanRoadLayout.Ports(spec, 0).ToArray();
            if (ports.Length == 0) return;
            var edgeColor = new Color(surfaceColor.r * 0.84f,
                surfaceColor.g * 0.83f, surfaceColor.b * 0.81f, 1f);
            foreach (var port in ports)
            {
                var slot = slots.First(item => (int)item.Edge == (int)port.Edge &&
                                               item.SegmentIndex == port.Segment);
                var endpoint = slot.LocalCenter;
                endpoint.y = 0f;
                var length = endpoint.magnitude;
                if (length < 0.01f) continue;

                var direction = endpoint / length;
                var sideways = new Vector3(direction.z, 0f, -direction.x);
                var bend = StableBend(spec.Id, (int)port.Edge, port.Segment);
                var route = NewTrailObject($"Authored Human Route {port.Edge} {port.Segment}",
                    parent, hideFlags);
                AddSurface(route.transform, "Trail Edge", edgeColor,
                    MakeRibbon(direction, sideways, length, width + 0.13f, bend, 0.255f),
                    material, hideFlags);
                AddSurface(route.transform, "Trail Surface", surfaceColor,
                    MakeRibbon(direction, sideways, length, width, bend, 0.259f),
                    material, hideFlags);
            }

            // The irregular central patch blends arms without drawing a rigid
            // rectangular plus sign; it also gives one-port trails a soft end.
            AddSurface(parent, "Trail Junction Edge", edgeColor,
                MakeJunction(width * 0.88f, 0.256f), material, hideFlags);
            AddSurface(parent, "Trail Junction Surface", surfaceColor,
                MakeJunction(width * 0.68f, 0.260f), material, hideFlags);
        }

        private static float StableBend(string roomId, int edge, int segment)
        {
            var hash = edge * 37 + segment * 17;
            foreach (var character in roomId)
                hash = unchecked(hash * 31 + character);
            return (hash & 1) == 0 ? 0.085f : -0.085f;
        }

        private static Mesh MakeRibbon(Vector3 direction, Vector3 sideways,
            float length, float width, float bend, float height)
        {
            var vertices = new Vector3[Stations.Length * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(Stations.Length - 1) * 6];
            for (var index = 0; index < Stations.Length; index++)
            {
                var center = direction * (Stations[index] * length) +
                             sideways * (Bends[index] * bend) + Vector3.up * height;
                var halfWidth = width * Widths[index] * 0.5f;
                vertices[index * 2] = center - sideways * halfWidth;
                vertices[index * 2 + 1] = center + sideways * halfWidth;
                normals[index * 2] = normals[index * 2 + 1] = Vector3.up;
                uvs[index * 2] = new Vector2(0f, Stations[index]);
                uvs[index * 2 + 1] = new Vector2(1f, Stations[index]);
                if (index == Stations.Length - 1) continue;
                var baseVertex = index * 2;
                var baseTriangle = index * 6;
                triangles[baseTriangle] = baseVertex;
                triangles[baseTriangle + 1] = baseVertex + 2;
                triangles[baseTriangle + 2] = baseVertex + 1;
                triangles[baseTriangle + 3] = baseVertex + 1;
                triangles[baseTriangle + 4] = baseVertex + 2;
                triangles[baseTriangle + 5] = baseVertex + 3;
            }

            return NewMesh("Worn Trail Ribbon", vertices, normals, uvs, triangles);
        }

        private static Mesh MakeJunction(float radius, float height)
        {
            const int sides = 10;
            var vertices = new Vector3[sides + 1];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[sides * 3];
            vertices[0] = new Vector3(0f, height, 0f);
            for (var index = 0; index <= sides; index++)
            {
                normals[index] = Vector3.up;
                if (index == sides) continue;
                var angle = index * Mathf.PI * 2f / sides;
                var roughness = index % 3 == 0 ? 1.04f : index % 3 == 1 ? 0.97f : 1.01f;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius * roughness,
                    height, Mathf.Sin(angle) * radius * roughness);
                uvs[index + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f,
                    Mathf.Sin(angle) * 0.5f + 0.5f);
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = (index + 1) % sides + 1;
                triangles[index * 3 + 2] = index + 1;
            }

            return NewMesh("Worn Trail Junction", vertices, normals, uvs, triangles);
        }

        private static Mesh NewMesh(string name, Vector3[] vertices,
            Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject NewTrailObject(string name, Transform parent, HideFlags hideFlags)
        {
            var instance = new GameObject(name) { hideFlags = hideFlags };
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void AddSurface(Transform parent, string name, Color color,
            Mesh mesh, Material material, HideFlags hideFlags)
        {
            var instance = NewTrailObject(name, parent, hideFlags);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<GeneratedMeshCleanup>().Initialize(mesh);
            var renderer = instance.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            UrbanVisualFactory.ApplyColor(renderer, color);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetFloat(InkStrengthId, 0f);
            renderer.SetPropertyBlock(block);
        }
    }
}
