using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.UI
{
    public static class GameplayHudVisualCatalog
    {
        private const string PopulationRoot = "UI/GameplayHud/";

        private static readonly IReadOnlyDictionary<WildlifeSpecies, string> PopulationResources =
            new Dictionary<WildlifeSpecies, string>
            {
                { WildlifeSpecies.Pigeon, PopulationRoot + "population-pigeon-v01" },
                { WildlifeSpecies.Squirrel, PopulationRoot + "population-squirrel-v01" },
                { WildlifeSpecies.Hedgehog, PopulationRoot + "population-hedgehog-v01" },
                { WildlifeSpecies.Fox, PopulationRoot + "population-fox-v01" }
            };

        private static readonly IReadOnlyDictionary<EcologicalMetricKind, string> MetricResources =
            new Dictionary<EcologicalMetricKind, string>
            {
                { EcologicalMetricKind.HumanFunction, PopulationRoot + "metric-human-function-v01" },
                { EcologicalMetricKind.FoodAccessibility, PopulationRoot + "metric-food-access-v01" },
                { EcologicalMetricKind.HabitatProvision, PopulationRoot + "metric-habitat-v01" },
                { EcologicalMetricKind.AnimalSafety, PopulationRoot + "metric-animal-safety-v01" }
            };

        private static readonly Dictionary<string, Sprite> RuntimeSprites = new();

        public static string PopulationResourcePath(WildlifeSpecies species)
        {
            return PopulationResources.TryGetValue(species, out var path) ? path : string.Empty;
        }

        public static string MetricResourcePath(EcologicalMetricKind kind)
        {
            return MetricResources.TryGetValue(kind, out var path) ? path : string.Empty;
        }

        public static Sprite GetPopulationSprite(WildlifeSpecies species)
        {
            return LoadSprite(PopulationResourcePath(species));
        }

        public static Sprite GetMetricSprite(EcologicalMetricKind kind)
        {
            return LoadSprite(MetricResourcePath(kind));
        }

        private static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            if (RuntimeSprites.TryGetValue(path, out sprite) && sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name + " Runtime Sprite";
            RuntimeSprites[path] = sprite;
            return sprite;
        }
    }
}
