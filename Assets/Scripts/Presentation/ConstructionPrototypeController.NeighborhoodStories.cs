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
                new Vector2(-CaseLeafCenter, 168f), new Vector2(310f, 244f), ReadingPaper);
            paper.raycastTarget = false;
            neighborhoodStoryRoot = paper.rectTransform;
            neighborhoodStoryAccent = Panel("Story accent", paper.transform,
                new Vector2(-152f, 0f), new Vector2(3f, 230f), Gold);
            neighborhoodStoryAccent.raycastTarget = false;
            neighborhoodStoryDate = TextAt("Story date", paper.transform,
                new Vector2(0f, 101f), new Vector2(282f, 21f), "", 11,
                false, HeadingInk, TextAnchor.MiddleRight);
            neighborhoodStoryIcon = Panel("Story event icon", paper.transform,
                new Vector2(-124f, 63f), new Vector2(32f, 32f), Color.white);
            neighborhoodStoryIcon.preserveAspect = true;
            neighborhoodStoryIcon.raycastTarget = false;
            neighborhoodStoryTitle = TextAt("Story title", paper.transform,
                new Vector2(21f, 63f), new Vector2(230f, 48f), "", 19,
                true, Ink, TextAnchor.MiddleLeft);
            neighborhoodStoryBody = TextAt("Story paragraph", paper.transform,
                new Vector2(0f, 6f), new Vector2(282f, 78f), "", 14,
                false, Ink, TextAnchor.UpperLeft);
            Panel("Story cause divider", paper.transform, new Vector2(0f, -39f),
                new Vector2(282f, 1f), Gold).raycastTarget = false;
            neighborhoodStoryCause = TextAt("Story trigger", paper.transform,
                new Vector2(0f, -70f), new Vector2(282f, 56f), "", 12,
                false, HeadingInk, TextAnchor.UpperLeft);
            neighborhoodStoryPrevious = TextAt("Previous story", paper.transform,
                new Vector2(0f, -109f), new Vector2(282f, 18f), "", 10,
                false, HeadingInk, TextAnchor.MiddleLeft);
            foreach (var label in new[] { neighborhoodStoryDate, neighborhoodStoryTitle,
                neighborhoodStoryBody, neighborhoodStoryCause, neighborhoodStoryPrevious })
                label.raycastTarget = false;
            neighborhoodStoryRoot.gameObject.SetActive(false);
        }

        private void RefreshNeighborhoodStories(bool visible)
        {
            if (neighborhoodStoryRoot == null) return;
            neighborhoodStoryRoot.gameObject.SetActive(visible);
            if (!visible || run == null) return;
            todayHeading.text = L("STREET JOURNAL", "街坊手记");
            var stamp = $"{run.RunId}:{run.CurrentDay}:{run.BuiltCount}:{run.LastScoreDay?.day}:{run.IsFinished}";
            if (stamp != neighborhoodStoryStamp)
            {
                neighborhoodStoryStamp = stamp;
                neighborhoodStories = ConstructionNeighborhoodStories.For(run);
            }
            var story = neighborhoodStories[0];
            neighborhoodStoryDate.text = story.Day == 0
                ? L("A beginning", "街坊的开篇")
                : L($"Day {story.Day} · {(story.Concern ? "A concern" : "A little change")}",
                    $"第 {story.Day} 天 · {(story.Concern ? "需要留意" : "街坊的新变化")}");
            neighborhoodStoryTitle.text = L(story.EnglishTitle, story.ChineseTitle);
            neighborhoodStoryBody.fontSize = Chinese ? 14 : 13;
            neighborhoodStoryCause.fontSize = Chinese ? 12 : 11;
            neighborhoodStoryBody.text = L(story.EnglishBody, story.ChineseBody);
            neighborhoodStoryCause.text = L("WHY IT HAPPENED\n" + story.EnglishCause,
                "触发原因\n" + story.ChineseCause);
            neighborhoodStoryPrevious.text = neighborhoodStories.Count > 1
                ? L("Earlier: " + neighborhoodStories[1].EnglishTitle,
                    "上次的手记：" + neighborhoodStories[1].ChineseTitle) : "";
            neighborhoodStoryIcon.sprite = ConstructionIconCatalog.GetBuild(story.Icon);
            neighborhoodStoryAccent.color = story.Concern
                ? new Color(0.70f, 0.36f, 0.24f) : UsedGreen;
        }
    }
}
