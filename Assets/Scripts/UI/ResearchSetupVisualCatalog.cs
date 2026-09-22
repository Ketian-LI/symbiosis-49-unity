using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlifeRooms.UI
{
    public enum ResearchSetupVisual
    {
        Panel,
        ParticipantField,
        StepperCard,
        StartButton,
        ParticipantIcon,
        EditIcon,
        ClockIcon,
        PawIcon
    }

    public static class ResearchSetupVisualCatalog
    {
        private const string ResourceRoot = "UI/ResearchSetup/";

        private static readonly IReadOnlyDictionary<ResearchSetupVisual, string> ResourceNames =
            new Dictionary<ResearchSetupVisual, string>
            {
                { ResearchSetupVisual.Panel, "research-panel-v01" },
                { ResearchSetupVisual.ParticipantField, "research-participant-field-v01" },
                { ResearchSetupVisual.StepperCard, "research-stepper-card-v01" },
                { ResearchSetupVisual.StartButton, "research-start-button-v01" },
                { ResearchSetupVisual.ParticipantIcon, "research-participant-icon-v01" },
                { ResearchSetupVisual.EditIcon, "research-edit-icon-v01" },
                { ResearchSetupVisual.ClockIcon, "research-clock-icon-v01" },
                { ResearchSetupVisual.PawIcon, "research-paw-icon-v01" }
            };

        private static readonly Dictionary<ResearchSetupVisual, Sprite> SpriteCache = new();

        public static string ResourcePath(ResearchSetupVisual visual)
        {
            return ResourceNames.TryGetValue(visual, out var name)
                ? ResourceRoot + name
                : string.Empty;
        }

        public static Sprite GetSprite(ResearchSetupVisual visual)
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
                    sprite.name = $"Research Setup {visual}";
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
