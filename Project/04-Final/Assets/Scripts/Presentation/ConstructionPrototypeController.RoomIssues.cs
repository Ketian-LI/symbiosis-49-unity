using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private const int IssueSlotsPerRoom = 3;
        private readonly RectTransform[,] roomIssueRoots = new RectTransform[7, 7];
        private readonly Image[,,] roomIssueImages =
            new Image[7, 7, IssueSlotsPerRoom];
        private readonly ConstructionRoomIssue[,,] roomIssueKinds =
            new ConstructionRoomIssue[7, 7, IssueSlotsPerRoom];
        private readonly string[,] roomIssueTileIds = new string[7, 7];
        private bool holdResolvedRoomIssues;
        private Coroutine roomIssueReleaseAnimation;

        private void HoldRoomIssuesThroughActivity()
        {
            if (roomIssueReleaseAnimation != null)
            {
                StopCoroutine(roomIssueReleaseAnimation);
                roomIssueReleaseAnimation = null;
            }
            holdResolvedRoomIssues = true;
        }

        private void ReleaseRoomIssuesAfterActivity()
        {
            roomIssueReleaseAnimation = StartCoroutine(
                ReleaseRoomIssuesWhenMotionFinishes(run));
        }

        private IEnumerator ReleaseRoomIssuesWhenMotionFinishes(
            ConstructionRunModel settledRun)
        {
            yield return null;
            while (run == settledRun &&
                   (scoreMotionAnimation != null || residentMealAnimation != null ||
                    worldView != null && worldView.HasActiveDayMotion))
                yield return null;
            holdResolvedRoomIssues = false;
            roomIssueReleaseAnimation = null;
            if (run == settledRun)
                RefreshRoomIssues(run.IsFinished ? run.LastHumanDay :
                        run.PreviewHumanDay(),
                    run.IsFinished ? null : run.PreviewEcologyDay(),
                    run.PreviewWasteAvailability());
        }

        private static bool RequiresSettledResolution(
            ConstructionRoomIssue issue) => issue is
            ConstructionRoomIssue.Waste or
            ConstructionRoomIssue.MissingWork or
            ConstructionRoomIssue.MissingMeal or
            ConstructionRoomIssue.WildlifeHungry;

        private void BuildRoomIssueIcons()
        {
            for (var column = 0; column < 7; column++)
            for (var row = 0; row < 7; row++)
            {
                var viewColumn = column;
                var viewRow = row;
                var parent = cellBackgrounds[column, row].transform;
                var root = NewRect("Room issue icons", parent,
                    new Vector2(0f, 38f), new Vector2(84f, 25f));
                roomIssueRoots[column, row] = root;
                for (var slot = 0; slot < IssueSlotsPerRoom; slot++)
                {
                    var issueSlot = slot;
                    var icon = Panel($"Room issue slot {slot + 1}", root,
                        Vector2.zero, new Vector2(25f, 25f), Color.white);
                    icon.preserveAspect = true;
                    var button = icon.gameObject.AddComponent<Button>();
                    button.targetGraphic = icon;
                    button.onClick.AddListener(() => ShowRoomIssue(
                        viewColumn, viewRow, issueSlot));
                    roomIssueImages[column, row, slot] = icon;
                    icon.gameObject.SetActive(false);
                }
                root.gameObject.SetActive(false);
            }
        }

        private void RefreshRoomIssues(ConstructionHumanDayResult human,
            ConstructionEcologyDayResult ecology,
            ConstructionWasteAvailability waste)
        {
            if (roomIssueRoots[0, 0] == null || run == null) return;
            for (var column = 0; column < 7; column++)
            for (var row = 0; row < 7; row++)
            {
                var tile = run.At(viewMinColumn + column,
                    viewMinRow + row);
                var issues = run.UsesFillHand && !run.IsFinished
                    ? ConstructionRoomIssueModel.ForTile(run, tile, human,
                        ecology, waste)
                    : System.Array.Empty<ConstructionRoomIssue>();
                if (holdResolvedRoomIssues && tile != null &&
                    roomIssueTileIds[column, row] == tile.id)
                {
                    var held = new List<ConstructionRoomIssue>(issues);
                    for (var slot = 0; slot < IssueSlotsPerRoom; slot++)
                    {
                        var image = roomIssueImages[column, row, slot];
                        var old = roomIssueKinds[column, row, slot];
                        if (image.gameObject.activeSelf &&
                            RequiresSettledResolution(old) &&
                            !held.Contains(old))
                            held.Add(old);
                    }
                    issues = held;
                }
                var shown = Mathf.Min(IssueSlotsPerRoom, issues.Count);
                roomIssueTileIds[column, row] = tile?.id;
                roomIssueRoots[column, row].gameObject.SetActive(shown > 0);
                for (var slot = 0; slot < IssueSlotsPerRoom; slot++)
                {
                    var image = roomIssueImages[column, row, slot];
                    image.gameObject.SetActive(slot < shown);
                    if (slot >= shown) continue;
                    var issue = issues[slot];
                    roomIssueKinds[column, row, slot] = issue;
                    image.gameObject.name = $"Room issue {issue}";
                    image.sprite = IssueSprite(issue);
                    image.color = Color.white;
                    image.rectTransform.anchoredPosition = new Vector2(
                        (slot - (shown - 1) * 0.5f) * 26f, 0f);
                }
            }
        }

        private static Sprite IssueSprite(ConstructionRoomIssue issue) =>
            issue switch
            {
                ConstructionRoomIssue.Waste =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Waste),
                ConstructionRoomIssue.EmptyHome =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Residence),
                ConstructionRoomIssue.MissingWork =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Workshop),
                ConstructionRoomIssue.MissingMeal =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Restaurant),
                ConstructionRoomIssue.WildlifeHungry =>
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.AnimalFood),
                ConstructionRoomIssue.Route =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Street),
                _ => ConstructionIconCatalog.GetStatus(
                    ConstructionStatusIcon.RiskAlert)
            };

        private void ShowRoomIssue(int column, int row, int slot)
        {
            if (feedbackText == null || run == null) return;
            var tile = run.At(viewMinColumn + column, viewMinRow + row);
            if (tile == null) return;
            feedbackText.text = roomIssueKinds[column, row, slot] switch
            {
                ConstructionRoomIssue.Waste => L(
                    "Waste remains here. A nearby waste room clears it at the next settlement.",
                    "这里还有未清运垃圾。附近的垃圾间会在结算时清走它。"),
                ConstructionRoomIssue.EmptyHome => L(
                    "This home is empty. A resident left after several incomplete daily cycles.",
                    "这间住宅已空置；居民因多次未完成日常循环而搬走。"),
                ConstructionRoomIssue.MissingWork => L(
                    "This resident has not worked yet. A reachable job is confirmed at settlement.",
                    "这位居民还没上班。接通岗位后，要等结算确认。"),
                ConstructionRoomIssue.MissingMeal => L(
                    "This resident has not eaten yet. A reachable seat is confirmed at settlement.",
                    "这位居民还没吃到饭。餐位要能到达，并在结算时实际用餐。"),
                ConstructionRoomIssue.WildlifeHungry => L(
                    "An animal here has not fed. Add reachable food and check the next settlement.",
                    "这里有动物缺食。补上可达食源，再看下次结算。"),
                ConstructionRoomIssue.Route => L(
                    "Residents need this place but cannot reach it. Connect a route.",
                    "居民需要这里，却无法到达。请接通路线。"),
                _ => L(
                    "Wildlife beside this street still faces traffic risk. A square lowers but does not remove it.",
                    "街道旁的动物仍有车祸风险。广场能降低概率，但不能消除风险。")
            };
        }
    }
}
