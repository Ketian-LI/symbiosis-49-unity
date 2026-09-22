using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed class PlayerFoodSourceVisual : MonoBehaviour
    {
        private readonly List<GameObject> portions = new();

        public void Initialize(Material material, HideFlags hideFlags)
        {
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                "Feeding Dish",
                transform,
                new Vector3(0f, 0.035f, 0f),
                new Vector3(0.38f, 0.025f, 0.38f),
                new Color(0.90f, 0.83f, 0.67f),
                material,
                false,
                hideFlags);

            var offsets = new[]
            {
                new Vector3(-0.12f, 0.085f, -0.04f),
                new Vector3(0.00f, 0.085f, 0.10f),
                new Vector3(0.13f, 0.085f, -0.02f),
                new Vector3(-0.05f, 0.085f, -0.13f),
                new Vector3(0.06f, 0.085f, -0.08f)
            };
            foreach (var offset in offsets)
            {
                portions.Add(UrbanVisualFactory.CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Food Portion",
                    transform,
                    offset,
                    new Vector3(0.09f, 0.045f, 0.07f),
                    new Color(0.86f, 0.59f, 0.20f),
                    material,
                    false,
                    hideFlags));
            }
        }

        public void SetPortions(int count)
        {
            for (var index = 0; index < portions.Count; index++)
            {
                portions[index].SetActive(index < count);
            }
        }
    }
}
