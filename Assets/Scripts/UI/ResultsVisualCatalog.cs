using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum ResultsVisual
    {
        EndAnimalDeaths,
        EndNegativeResources,
        DaysSurvived,
        NewRecordRibbon,
        SummaryResources,
        SummaryResidents,
        SummaryEcology,
        LayoutThumbnailFrame,
        RestartButton,
        MainMenuButton,
        MainCard,
        SummaryCard
    }

    public static class ResultsVisualCatalog
    {
        private const string ResourceRoot = "UI/Results/";

        private static readonly IReadOnlyDictionary<ResultsVisual, string> ResourceNames =
            new Dictionary<ResultsVisual, string>
            {
                { ResultsVisual.EndAnimalDeaths, "results-end-animal-deaths-v01" },
                { ResultsVisual.EndNegativeResources, "results-end-negative-resources-v01" },
                { ResultsVisual.DaysSurvived, "results-days-survived-v01" },
                { ResultsVisual.NewRecordRibbon, "results-new-record-ribbon-v01" },
                { ResultsVisual.SummaryResources, "results-summary-resources-v01" },
                { ResultsVisual.SummaryResidents, "results-summary-residents-v01" },
                { ResultsVisual.SummaryEcology, "results-summary-ecology-v01" },
                { ResultsVisual.LayoutThumbnailFrame, "results-layout-thumbnail-frame-v01" },
                { ResultsVisual.RestartButton, "results-restart-button-v01" },
                { ResultsVisual.MainMenuButton, "results-main-menu-button-v01" },
                { ResultsVisual.MainCard, "results-main-card-v01" },
                { ResultsVisual.SummaryCard, "results-summary-card-v01" }
            };

        private static readonly Dictionary<ResultsVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(ResultsVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(ResultsVisual visual)
        {
            if (SpriteCache.TryGetValue(visual, out var cached) && cached != null)
            {
                return cached;
            }

            var path = ResourcePath(visual);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = $"Results {visual}";
                }
            }

            if (sprite != null)
            {
                SpriteCache[visual] = sprite;
            }

            return sprite;
        }
    }
}
