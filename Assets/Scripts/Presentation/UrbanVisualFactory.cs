using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public static class UrbanVisualFactory
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Material CreateSurfaceMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "Urban Wildlife Runtime Surface",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.08f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
        }

        public static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Material material,
            bool removeCollider,
            HideFlags hideFlags)
        {
            var instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            instance.hideFlags = hideFlags;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;

            var renderer = instance.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            ApplyColor(renderer, color);

            if (removeCollider)
            {
                var collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(collider);
                    }
                    else
                    {
                        Object.DestroyImmediate(collider);
                    }
                }
            }

            return instance;
        }

        public static void ApplyColor(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }
    }
}

