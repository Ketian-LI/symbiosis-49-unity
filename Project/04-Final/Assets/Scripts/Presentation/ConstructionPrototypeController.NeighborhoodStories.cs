using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform neighborhoodStoryRoot;
        private Text neighborhoodStoryDate;
        private Text neighborhoodStoryTitle;
        private Text neighborhoodStoryBody;
        private Text neighborhoodStoryCause;
        private Text neighborhoodStoryPrevious;
        private Image neighborhoodStoryIcon;
        private Image neighborhoodStoryAccent;
        private string neighborhoodStoryStamp;
        private IReadOnlyList<NeighborhoodStory> neighborhoodStories;

        private void BuildNeighborhoodStories(Transform parent)
        {
            var paper = Panel("Neighbourhood story paper", parent,
                new Vector2(-CaseLeafCenter - 10f, 233f), new Vector2(288f, 208f), Color.clear);
            paper.raycastTarget = false;
            neighborhoodStoryRoot = paper.rectTransform;
            neighborhoodStoryAccent = Panel("Story accent", paper.transform,
                new Vector2(0f, 68f), new Vector2(272f, 1f), HandQuietBrass);
            neighborhoodStoryAccent.raycastTarget = false;
            neighborhoodStoryDate = TextAt("Story date", paper.transform,
                new Vector2(89f, 83f), new Vector2(86f, 28f), "", 16,
                false, HeadingInk, TextAnchor.MiddleRight);
            neighborhoodStoryIcon = Panel("Story event icon", paper.transform,
                new Vector2(-116f, 101f), new Vector2(32f, 32f), Color.white);
            neighborhoodStoryIcon.preserveAspect = true;
            neighborhoodStoryIcon.raycastTarget = false;
            neighborhoodStoryIcon.gameObject.SetActive(false);
            neighborhoodStoryTitle = TextAt("Story title", paper.transform,
                new Vector2(0f, 29f), new Vector2(264f, 56f), "", 24,
                true, Ink, TextAnchor.MiddleLeft);
            neighborhoodStoryBody = TextAt("Story paragraph", paper.transform,
                new Vector2(0f, -38f), new Vector2(264f, 76f), "", 18,
                false, Ink, TextAnchor.UpperLeft);
            neighborhoodStoryCause = TextAt("Story trigger", paper.transform,
                new Vector2(0f, -103f), new Vector2(264f, 48f), "", 16,
                false, HeadingInk, TextAnchor.UpperLeft);
            neighborhoodStoryPrevious = TextAt("Previous story", paper.transform,
                new Vector2(-8f, -109f), new Vector2(248f, 28f), "", 16,
                false, HeadingInk, TextAnchor.MiddleLeft);
            neighborhoodStoryPrevious.gameObject.SetActive(false);
            foreach (var label in new[] { neighborhoodStoryDate, neighborhoodStoryTitle,
                neighborhoodStoryBody, neighborhoodStoryCause, neighborhoodStoryPrevious })
                label.raycastTarget = false;
            neighborhoodStoryRoot.gameObject.SetActive(false);
        }

        private void RefreshNeighborhoodStories(bool visible)
        {
            if (neighborhoodStoryRoot == null) return;
            neighborhoodStoryRoot.gameObject.SetActive(visible);
            SetRect(todayHeading.rectTransform, visible ? new Vector2(-76f, 101f) : new Vector2(-18f, 99f),
                visible ? new Vector2(132f, 28f) : new Vector2(150f, 28f));
            todayHeading.fontSize = visible ? 16 : 19;
            textWeights[todayHeading] = !visible;
            todayHeading.font = Chinese ? chineseFont : visible ? regularFont : boldFont;
            todayHeading.fontStyle = Chinese && !visible ? FontStyle.Bold : FontStyle.Normal;
            todayHeading.alignment = TextAnchor.MiddleLeft;
            if (!visible || run == null) return;
            todayHeading.text = L("Street journal", "街坊手记");
            var stamp = $"{run.RunId}:{run.CurrentDay}:{run.BuiltCount}:{run.LastScoreDay?.day}:{run.IsFinished}";
            if (stamp != neighborhoodStoryStamp)
            {
                neighborhoodStoryStamp = stamp;
                neighborhoodStories = ConstructionNeighborhoodStories.For(run);
            }
            var story = neighborhoodStories[0];
            neighborhoodStoryDate.text = story.Day == 0
                ? L("Opening", "开篇")
                : L($"Day {story.Day}", $"第 {story.Day} 天");
            neighborhoodStoryTitle.text = L(story.EnglishTitle, story.ChineseTitle);
            neighborhoodStoryBody.fontSize = 18;
            neighborhoodStoryCause.fontSize = 16;
            neighborhoodStoryBody.text = L(story.EnglishBody, story.ChineseBody);
            neighborhoodStoryCause.text = L(story.EnglishCause, story.ChineseCause);
            neighborhoodStoryCause.color = story.Concern ? ReadingWarning : HeadingInk;
            neighborhoodStoryPrevious.text = neighborhoodStories.Count > 1
                ? L("Earlier: " + neighborhoodStories[1].EnglishTitle,
                    "前一则：" + neighborhoodStories[1].ChineseTitle) : "";
            neighborhoodStoryIcon.sprite = ConstructionIconCatalog.GetBuild(story.Icon);
            neighborhoodStoryAccent.color = story.Concern
                ? ReadingWarning : HandQuietBrass;
        }
    }
}
