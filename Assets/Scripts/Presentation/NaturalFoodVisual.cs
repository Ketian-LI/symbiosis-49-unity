using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class NaturalFoodVisual : MonoBehaviour
    {
        private readonly List<GameObject> portions = new();

        public void Initialize(
            NaturalFoodKind kind,
            int count,
            Material material,
            HideFlags hideFlags)
        {
            var color = kind switch
            {
                NaturalFoodKind.Seed => new Color(0.82f, 0.67f, 0.34f),
                NaturalFoodKind.Nut => new Color(0.56f, 0.31f, 0.13f),
                NaturalFoodKind.Insect => new Color(0.20f, 0.16f, 0.12f),
                _ => new Color(0.78f, 0.43f, 0.24f)
            };
            for (var index = 0; index < count; index++)
            {
                var angle = index * 2.17f;
                portions.Add(UrbanVisualFactory.CreatePrimitive(
                    kind == NaturalFoodKind.Insect ? PrimitiveType.Capsule : PrimitiveType.Sphere,
                    $"{kind} Portion {index + 1}",
                    transform,
                    new Vector3(Mathf.Cos(angle) * 0.10f, 0.22f, Mathf.Sin(angle) * 0.08f),
                    kind == NaturalFoodKind.Insect
                        ? new Vector3(0.055f, 0.035f, 0.085f)
                        : new Vector3(0.075f, 0.045f, 0.065f),
                    color,
                    material,
                    true,
                    hideFlags));
            }
        }
    }
}
