using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>Shared faceted tree language for the park and every oak growth stage.</summary>
    public static class LowPolyTreeVisualBuilder
    {
        private static readonly Color Bark = new(0.39f, 0.27f, 0.18f);
        private static readonly Color BarkLight = new(0.48f, 0.34f, 0.23f);
        private static readonly Color[] Leaves =
        {
            new(0.46f, 0.53f, 0.29f),
            new(0.52f, 0.60f, 0.34f),
            new(0.38f, 0.49f, 0.29f),
            new(0.57f, 0.62f, 0.36f),
            new(0.43f, 0.56f, 0.33f)
        };

        // Each facet has its own vertices and normal; Unity's smooth sphere
        // would erase the deliberate low-poly planes of the approved reference.
        private static readonly int[] CrownFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        public static Transform Build(
            Transform parent,
            Vector3 roomPosition,
            float scale,
            bool seedling,
            Material material,
            HideFlags hideFlags)
        {
            var tree = new GameObject(seedling ? "Low-poly Growing Tree" : "Low-poly Mature Tree")
            {
                hideFlags = hideFlags
            };
            tree.transform.SetParent(parent, false);
            // Both park and oak callers use different anchor y values. The tree
            // itself always grows out of the same top surface of the room floor.
            tree.transform.localPosition = new Vector3(roomPosition.x, 0.24f, roomPosition.z);
            var root = tree.transform;

            var trunkHeight = (seedling ? 0.67f : 0.80f) * scale;
            var trunkRadius = (seedling ? 0.12f : 0.18f) * scale;
            CreateFrustum("Faceted Tapered Trunk", root,
                new Vector3(0f, trunkHeight * 0.5f, 0f),
                new Vector3(trunkRadius, trunkHeight, trunkRadius),
                Quaternion.identity, Bark, material, hideFlags);

            if (!seedling)
            {
                CreateFrustum("Left Branch", root,
                    new Vector3(-0.12f, 0.56f, 0.03f) * scale,
                    new Vector3(0.075f, 0.35f, 0.075f) * scale,
                    Quaternion.Euler(0f, 0f, -43f), Bark, material, hideFlags);
                CreateFrustum("Right Branch", root,
                    new Vector3(0.13f, 0.55f, 0.02f) * scale,
                    new Vector3(0.07f, 0.34f, 0.07f) * scale,
                    Quaternion.Euler(18f, 0f, 40f), BarkLight, material, hideFlags);
                for (var index = 0; index < 3; index++)
                {
                    var angle = index * 120f + 20f;
                    CreateFrustum($"Visible Root {index + 1}", root,
                        Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.10f, 0.14f * scale),
                        new Vector3(0.10f, 0.28f, 0.10f) * scale,
                        Quaternion.Euler(65f, angle, 0f), BarkLight, material, hideFlags);
                }
            }

            if (seedling)
            {
                CreateCrown("Young Crown A", root, new Vector3(-0.06f, 0.75f, 0f) * scale,
                    new Vector3(0.29f, 0.32f, 0.28f) * scale, 17f, Leaves[1], material, hideFlags);
                CreateCrown("Young Crown B", root, new Vector3(0.15f, 0.68f, 0.06f) * scale,
                    new Vector3(0.22f, 0.25f, 0.21f) * scale, -24f, Leaves[0], material, hideFlags);
            }
            else
            {
                CreateCrown("Canopy Centre", root, new Vector3(0f, 0.91f, 0f) * scale,
                    new Vector3(0.38f, 0.34f, 0.35f) * scale, 8f, Leaves[0], material, hideFlags);
                CreateCrown("Canopy Left", root, new Vector3(-0.22f, 0.82f, 0.10f) * scale,
                    new Vector3(0.25f, 0.27f, 0.24f) * scale, -17f, Leaves[2], material, hideFlags);
                CreateCrown("Canopy Right", root, new Vector3(0.22f, 0.85f, 0.09f) * scale,
                    new Vector3(0.25f, 0.28f, 0.24f) * scale, 29f, Leaves[1], material, hideFlags);
                CreateCrown("Canopy Front", root, new Vector3(0.02f, 0.82f, -0.18f) * scale,
                    new Vector3(0.28f, 0.25f, 0.25f) * scale, -31f, Leaves[4], material, hideFlags);
                CreateCrown("Canopy High", root, new Vector3(-0.05f, 1.08f, 0.04f) * scale,
                    new Vector3(0.24f, 0.24f, 0.22f) * scale, 45f, Leaves[3], material, hideFlags);
            }

            return root;
        }

        /// <summary>Use the same flat-shaded foliage language for low shrubs and den weeds.</summary>
        public static void BuildFacetedFoliage(
            string name, Transform parent, Vector3 position, Vector3 diameter,
            float yaw, Color color, Material material, HideFlags hideFlags)
        {
            CreateCrown(name, parent, position, diameter * 0.5f, yaw, color, material, hideFlags);
        }

        private static void CreateCrown(
            string name, Transform parent, Vector3 position, Vector3 size, float yaw,
            Color color, Material material, HideFlags hideFlags)
        {
            CreateMeshObject(name, parent, position, size,
                Quaternion.Euler(0f, yaw, 0f), CreateIcosahedron(), color, material, hideFlags);
        }

        private static void CreateFrustum(
            string name, Transform parent, Vector3 position, Vector3 size,
            Quaternion rotation, Color color, Material material, HideFlags hideFlags)
        {
            CreateMeshObject(name, parent, position, size,
                rotation, CreateTaperedHexagon(), color, material, hideFlags);
        }

        private static void CreateMeshObject(
            string name, Transform parent, Vector3 position, Vector3 scale,
            Quaternion rotation, Mesh mesh, Color color, Material material, HideFlags hideFlags)
        {
            var instance = new GameObject(name) { hideFlags = hideFlags };
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            mesh.hideFlags = hideFlags;
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = instance.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            UrbanVisualFactory.ApplyColor(renderer, color);
            instance.AddComponent<GeneratedMeshCleanup>().Initialize(mesh);
        }

        private static Mesh CreateIcosahedron()
        {
            var phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var corners = new[]
            {
                new Vector3(-1f, phi, 0f), new Vector3(1f, phi, 0f),
                new Vector3(-1f, -phi, 0f), new Vector3(1f, -phi, 0f),
                new Vector3(0f, -1f, phi), new Vector3(0f, 1f, phi),
                new Vector3(0f, -1f, -phi), new Vector3(0f, 1f, -phi),
                new Vector3(phi, 0f, -1f), new Vector3(phi, 0f, 1f),
                new Vector3(-phi, 0f, -1f), new Vector3(-phi, 0f, 1f)
            };
            var vertices = new Vector3[CrownFaces.Length];
            var triangles = new int[CrownFaces.Length];
            for (var index = 0; index < CrownFaces.Length; index++)
            {
                vertices[index] = corners[CrownFaces[index]].normalized;
                triangles[index] = index;
            }
            var mesh = new Mesh { name = "Flat-shaded Crown Icosahedron" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateTaperedHexagon()
        {
            const int sides = 6;
            var vertices = new Vector3[sides * 6];
            var triangles = new int[vertices.Length];
            for (var side = 0; side < sides; side++)
            {
                var a = side * Mathf.PI * 2f / sides;
                var b = (side + 1) * Mathf.PI * 2f / sides;
                var bottomA = new Vector3(Mathf.Sin(a), -0.5f, Mathf.Cos(a));
                var bottomB = new Vector3(Mathf.Sin(b), -0.5f, Mathf.Cos(b));
                var topA = new Vector3(Mathf.Sin(a) * 0.56f, 0.5f, Mathf.Cos(a) * 0.56f);
                var topB = new Vector3(Mathf.Sin(b) * 0.56f, 0.5f, Mathf.Cos(b) * 0.56f);
                var offset = side * 6;
                vertices[offset] = bottomA;
                vertices[offset + 1] = bottomB;
                vertices[offset + 2] = topB;
                vertices[offset + 3] = bottomA;
                vertices[offset + 4] = topB;
                vertices[offset + 5] = topA;
                for (var index = 0; index < 6; index++)
                {
                    triangles[offset + index] = offset + index;
                }
            }
            var mesh = new Mesh { name = "Flat-shaded Tapered Hexagon" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
