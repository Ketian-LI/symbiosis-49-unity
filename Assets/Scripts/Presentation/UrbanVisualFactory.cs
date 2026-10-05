using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public static class UrbanVisualFactory
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int InkStrengthId = Shader.PropertyToID("_InkStrength");

        public static Material CreateSurfaceMaterial()
        {
            // Keeping the source material in Resources makes the line weight and
            // paper grain adjustable in the Inspector and prevents build stripping.
            var template = Resources.Load<Material>("Materials/SketchSurface");
            var sketchShader = Shader.Find("Symbiosis/Sketch Surface");
            var fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ??
                                 Shader.Find("Standard");
            var material = template != null && template.shader != null && template.shader.isSupported
                ? new Material(template)
                : new Material(sketchShader != null && sketchShader.isSupported
                    ? sketchShader
                    : fallbackShader);
            material.name = "Symbiosis Runtime Sketch Surface";
            material.hideFlags = HideFlags.HideAndDontSave;

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
            // The ink follows the perimeter UVs on box faces. Curved and imported
            // meshes keep their clean silhouettes instead of acquiring UV seams.
            if (material.HasProperty(InkStrengthId))
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat(InkStrengthId, primitiveType == PrimitiveType.Cube ? 1f : 0f);
                renderer.SetPropertyBlock(block);
            }

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
