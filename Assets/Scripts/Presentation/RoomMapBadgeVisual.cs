using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // Small floor-plan badges, parented to the movable room rather than the
    // board. They have no colliders, so they never steal a room drag/click.
    public static class RoomMapBadgeVisual
    {
        private static readonly Color Rim = new(0.93f, 0.84f, 0.65f);
        private static readonly Color Backing = new(0.12f, 0.17f, 0.21f);
        private static readonly Color TextColor = new(0.98f, 0.95f, 0.84f);

        public static TextMesh BuildFood(Transform parent, NaturalFoodKind kind,
            Vector3 localPosition, int portions, Material material, HideFlags hideFlags)
        {
            var accent = kind switch
            {
                NaturalFoodKind.Seed => new Color(0.93f, 0.76f, 0.39f),
                NaturalFoodKind.Nut => new Color(0.75f, 0.48f, 0.28f),
                NaturalFoodKind.Insect => new Color(0.59f, 0.79f, 0.51f),
                _ => new Color(0.96f, 0.57f, 0.37f)
            };
            var root = BuildCard(parent, $"Food Map Badge {kind}", localPosition,
                0.96f, accent, material, hideFlags);
            DrawFoodSymbol(root, kind, accent, material, hideFlags);
            return BuildNumber(root, "Food Portions", portions.ToString(),
                0.13f, hideFlags);
        }

        public static TextMesh BuildCapacity(Transform parent, bool office,
            Vector3 localPosition, int used, int capacity, Material material,
            HideFlags hideFlags)
        {
            var accent = office
                ? new Color(0.43f, 0.80f, 0.77f)
                : new Color(0.95f, 0.68f, 0.40f);
            var root = BuildCard(parent, office ? "Office Capacity Badge" :
                "Food Shop Capacity Badge", localPosition, 1.12f,
                accent, material, hideFlags);
            DrawPerson(root, accent, material, hideFlags);
            return BuildNumber(root, "Planned People / Capacity",
                $"{Mathf.Clamp(used, 0, capacity)}/{capacity}", 0.18f, hideFlags);
        }

        public static TextMesh BuildPlacedFood(Transform parent,
            Vector3 localPosition, int portions, Material material, HideFlags hideFlags)
        {
            var accent = new Color(0.38f, 0.80f, 0.77f);
            var root = BuildCard(parent, "Placed Food Map Badge", localPosition,
                0.96f, accent, material, hideFlags);
            DrawFoodSymbol(root, NaturalFoodKind.DiscardedFood, accent,
                material, hideFlags);
            return BuildNumber(root, "Placed Food Portions", portions.ToString(),
                0.13f, hideFlags);
        }

        private static Transform BuildCard(Transform parent, string name,
            Vector3 position, float width, Color accent, Material material,
            HideFlags hideFlags)
        {
            var root = new GameObject(name) { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            Part(root.transform, "Badge Rim", new Vector3(0f, 0f, 0f),
                new Vector3(width, 0.035f, 0.45f), Rim, material, hideFlags);
            Part(root.transform, "Badge Face", new Vector3(0f, 0.027f, 0f),
                new Vector3(width - 0.035f, 0.025f, 0.415f), Backing, material, hideFlags);
            Part(root.transform, "Badge Accent", new Vector3(-width * 0.5f + 0.057f,
                    0.047f, 0f), new Vector3(0.045f, 0.014f, 0.34f),
                accent, material, hideFlags);
            return root.transform;
        }

        private static void DrawFoodSymbol(Transform root, NaturalFoodKind kind,
            Color accent, Material material, HideFlags hideFlags)
        {
            switch (kind)
            {
                case NaturalFoodKind.Seed:
                    Part(root, "Seed Symbol A", new Vector3(-0.27f, 0.07f, -0.06f),
                        new Vector3(0.09f, 0.035f, 0.13f), accent, material, hideFlags,
                        PrimitiveType.Sphere);
                    Part(root, "Seed Symbol B", new Vector3(-0.19f, 0.07f, 0.06f),
                        new Vector3(0.09f, 0.035f, 0.13f), accent, material, hideFlags,
                        PrimitiveType.Sphere);
                    break;
                case NaturalFoodKind.Nut:
                    Part(root, "Nut Symbol", new Vector3(-0.23f, 0.07f, -0.015f),
                        new Vector3(0.19f, 0.045f, 0.17f), accent, material, hideFlags,
                        PrimitiveType.Sphere);
                    Part(root, "Nut Cap", new Vector3(-0.23f, 0.095f, 0.065f),
                        new Vector3(0.18f, 0.022f, 0.055f), Rim, material, hideFlags);
                    break;
                case NaturalFoodKind.Insect:
                    Part(root, "Insect Body", new Vector3(-0.23f, 0.07f, 0f),
                        new Vector3(0.105f, 0.04f, 0.19f), accent, material, hideFlags,
                        PrimitiveType.Sphere);
                    Part(root, "Insect Wings", new Vector3(-0.23f, 0.078f, 0f),
                        new Vector3(0.23f, 0.017f, 0.095f), Rim, material, hideFlags);
                    break;
                default:
                    Part(root, "Scrap Dish", new Vector3(-0.23f, 0.068f, 0f),
                        new Vector3(0.22f, 0.025f, 0.17f), accent, material, hideFlags,
                        PrimitiveType.Cylinder);
                    Part(root, "Scrap Morsel", new Vector3(-0.23f, 0.094f, 0f),
                        new Vector3(0.095f, 0.035f, 0.075f), Rim, material, hideFlags,
                        PrimitiveType.Sphere);
                    break;
            }
        }

        private static void DrawPerson(Transform root, Color accent,
            Material material, HideFlags hideFlags)
        {
            Part(root, "Person Head", new Vector3(-0.34f, 0.073f, 0.07f),
                new Vector3(0.105f, 0.045f, 0.105f), accent, material, hideFlags,
                PrimitiveType.Sphere);
            Part(root, "Person Body", new Vector3(-0.34f, 0.068f, -0.075f),
                new Vector3(0.16f, 0.022f, 0.17f), accent, material, hideFlags);
        }

        private static TextMesh BuildNumber(Transform root, string name,
            string value, float x, HideFlags hideFlags)
        {
            var objectWithText = new GameObject(name, typeof(TextMesh))
            {
                hideFlags = hideFlags
            };
            objectWithText.transform.SetParent(root, false);
            objectWithText.transform.localPosition = new Vector3(x, 0.073f, 0f);
            objectWithText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var label = objectWithText.GetComponent<TextMesh>();
            label.font = UrbanFontResolver.GetFont(FontStyle.Bold);
            label.fontSize = 72;
            label.characterSize = 0.034f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = TextColor;
            label.text = value;
            var renderer = objectWithText.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = label.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 12;
            return label;
        }

        private static void Part(Transform parent, string name, Vector3 position,
            Vector3 scale, Color color, Material material, HideFlags hideFlags,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            var part = UrbanVisualFactory.CreatePrimitive(primitive, name, parent,
                position, scale, color, material, true, hideFlags);
            // CreatePrimitive destroys the collider at frame end in play mode;
            // disable it immediately so a freshly spawned badge is never clickable.
            var collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = part.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
