using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionMenuEntry
    {
        private static readonly Color RankingInk = new(0.11f, 0.18f, 0.23f);
        private static readonly Color RankingPaper = new(0.96f, 0.91f, 0.79f);
        private static readonly Color RankingGold = new(0.78f, 0.61f, 0.34f);
        private static readonly Color RankingGreen = new(0.24f, 0.43f, 0.34f);

        private Button localRankingsEntrance;
        private Text localRankingsEntranceLabel;
        private GameObject menuRankingsOverlay;
        private Text menuRankingsTitle;
        private Text menuRankingsSubtitle;
        private Text menuRankingsEmpty;
        private Text menuRankingsCloseLabel;
        private readonly Text[] menuRankingsModeLabels = new Text[3];
        private readonly Image[] menuRankingsModeFaces = new Image[3];
        private readonly Text[] menuRankingsHeaders = new Text[6];
        private readonly Image[] menuRankingsRowFaces = new Image[10];
        private readonly Image[] menuRankingsAwardIcons = new Image[10];
        private readonly Text[,] menuRankingsCells = new Text[10, 6];
        private ConstructionGameMode menuRankingsMode = ConstructionGameMode.Fill;
        private ConstructionLocalLeaderboardStore menuLeaderboardStore;

        private void CreateLocalRankingsEntrance()
        {
            var root = new GameObject("Desktop Leaderboard",
                typeof(RectTransform));
            root.transform.SetParent(menu, false);
            ConstructionMainMenuLayout.Place(root.GetComponent<RectTransform>(),
                ConstructionMainMenuLayout.Leaderboard);

            var backing = RankingImage("Paper Button Backing", root.transform,
                Color.white, new Vector2(0f, 13f), new Vector2(74f, 74f));
            backing.sprite = ConstructionMainMenuLayout.RoundControlBase;
            backing.preserveAspect = true;
            localRankingsEntrance = backing.gameObject.AddComponent<Button>();
            localRankingsEntrance.targetGraphic = backing;
            var colors = localRankingsEntrance.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.88f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.76f);
            localRankingsEntrance.colors = colors;
            localRankingsEntrance.onClick.AddListener(OpenMenuLocalRankings);

            var icon = RankingImage("Control Pictogram", backing.transform,
                Color.white, Vector2.zero, new Vector2(46f, 46f));
            icon.sprite = ConstructionMainMenuLayout.LeaderboardIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (icon.sprite == null)
                icon.sprite = ConstructionMainMenuLayout.FooterIcon(
                    MainMenuVisual.SettingsIcon);

            localRankingsEntranceLabel = RankingText("Live Control Label",
                root.transform, new Vector2(0f, -40f),
                new Vector2(112f, 30f), 20, RankingInk,
                TextAnchor.MiddleCenter);
            CreateMenuLocalRankingsOverlay();
        }

        private void CreateMenuLocalRankingsOverlay()
        {
            menuRankingsOverlay = new GameObject("Main Menu Local Rankings",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuRankingsOverlay.transform.SetParent(menu, false);
            var shade = menuRankingsOverlay.GetComponent<Image>();
            shade.color = new Color(0.03f, 0.06f, 0.08f, 0.80f);
            shade.raycastTarget = true;
            var shadeRect = shade.rectTransform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;

            var border = RankingImage("Gold edge",
                menuRankingsOverlay.transform, RankingGold,
                Vector2.zero, new Vector2(824f, 744f));
            var paper = RankingImage("Ranking paper", border.transform,
                RankingPaper, Vector2.zero, new Vector2(814f, 734f));
            RankingImage("Navy heading", paper.transform, RankingInk,
                new Vector2(0f, 335f), new Vector2(814f, 64f));
            menuRankingsTitle = RankingText("Ranking title", paper.transform,
                new Vector2(0f, 335f), new Vector2(680f, 52f), 28,
                RankingPaper, TextAnchor.MiddleCenter);
            menuRankingsSubtitle = RankingText("Ranking subtitle",
                paper.transform, new Vector2(0f, 277f),
                new Vector2(680f, 31f), 16, RankingInk,
                TextAnchor.MiddleCenter);

            var modes = new[] { ConstructionGameMode.Fill,
                ConstructionGameMode.Endless, ConstructionGameMode.BoardGame };
            for (var index = 0; index < modes.Length; index++)
            {
                var selectedMode = modes[index];
                var button = RankingButton("Rankings " + selectedMode,
                    paper.transform, new Vector2((index - 1) * 188f, 226f),
                    new Vector2(175f, 43f), RankingInk,
                    () => SelectMenuRankingsMode(selectedMode));
                menuRankingsModeFaces[index] = button.GetComponent<Image>();
                menuRankingsModeLabels[index] = RankingText("Mode label",
                    button.transform, Vector2.zero, new Vector2(169f, 40f),
                    17, RankingPaper, TextAnchor.MiddleCenter);
            }

            RankingImage("Heading rule", paper.transform, RankingGold,
                new Vector2(0f, 184f), new Vector2(710f, 2f))
                .raycastTarget = false;
            var headings = new[] { "Rank", "Name", "Total", "Animal",
                "Resident", "Days" };
            var x = new[] { -326f, -227f, -42f, 57f, 172f, 292f };
            var widths = new[] { 54f, 162f, 80f, 100f, 100f, 100f };
            for (var column = 0; column < headings.Length; column++)
                menuRankingsHeaders[column] = RankingText(
                    "Rankings header " + headings[column], paper.transform,
                    new Vector2(x[column], 162f),
                    new Vector2(widths[column], 26f), 14, RankingInk,
                    column == 1 ? TextAnchor.MiddleLeft :
                        TextAnchor.MiddleCenter);

            for (var row = 0; row < 10; row++)
            {
                var y = 135f - row * 40f;
                menuRankingsRowFaces[row] = RankingImage(
                    "Local rankings row " + (row + 1), paper.transform,
                    row % 2 == 0
                        ? new Color(1f, 0.99f, 0.93f, 0.54f)
                        : new Color(0.83f, 0.78f, 0.65f, 0.28f),
                    new Vector2(0f, y), new Vector2(710f, 36f));
                menuRankingsRowFaces[row].raycastTarget = false;
                menuRankingsAwardIcons[row] = RankingImage(
                    "Rank award " + (row + 1), paper.transform, Color.white,
                    new Vector2(252f, y), new Vector2(24f, 24f));
                menuRankingsAwardIcons[row].preserveAspect = true;
                menuRankingsAwardIcons[row].raycastTarget = false;
                menuRankingsAwardIcons[row].gameObject.SetActive(false);
                for (var column = 0; column < 6; column++)
                    menuRankingsCells[row, column] = RankingText(
                        $"Rank {row + 1} column {column}", paper.transform,
                        new Vector2(x[column], y),
                        new Vector2(widths[column], 34f), 16, RankingInk,
                        column == 1 ? TextAnchor.MiddleLeft :
                            TextAnchor.MiddleCenter);
            }
            menuRankingsEmpty = RankingText("Rankings empty",
                paper.transform, new Vector2(0f, -55f),
                new Vector2(630f, 90f), 18, RankingInk,
                TextAnchor.MiddleCenter);
            RankingImage("Footer rule", paper.transform, RankingGold,
                new Vector2(0f, -286f), new Vector2(710f, 2f))
                .raycastTarget = false;
            var close = RankingButton("Close local rankings", paper.transform,
                new Vector2(0f, -324f), new Vector2(175f, 43f),
                RankingInk, () => menuRankingsOverlay.SetActive(false));
            menuRankingsCloseLabel = RankingText("Close label", close.transform,
                Vector2.zero, new Vector2(170f, 40f), 18,
                RankingPaper, TextAnchor.MiddleCenter);
            menuRankingsOverlay.SetActive(false);
        }

        private void OpenMenuLocalRankings()
        {
            if (!EnsureDesktopMenu() || menuRankingsOverlay == null) return;
            menuRankingsMode = ConstructionGameMode.Fill;
            menuLeaderboardStore ??= new ConstructionLocalLeaderboardStore(
                ConstructionLocalLeaderboardStore.DefaultPath);
            menuRankingsOverlay.transform.SetAsLastSibling();
            menuRankingsOverlay.SetActive(true);
            RefreshMenuLocalRankings();
        }

        private void SelectMenuRankingsMode(ConstructionGameMode mode)
        {
            menuRankingsMode = mode;
            RefreshMenuLocalRankings();
        }

        private void UpdateLocalRankingsLanguage()
        {
            var chinese = shownLanguage == InterfaceLanguage.Chinese;
            if (localRankingsEntranceLabel != null)
            {
                ConstructionMainMenuLayout.ApplyModeFont(
                    localRankingsEntranceLabel, chinese, 20);
                localRankingsEntranceLabel.text = chinese ? "排行榜" : "Rankings";
            }
            if (menuRankingsOverlay != null && menuRankingsOverlay.activeSelf)
                RefreshMenuLocalRankings();
        }

        private void RefreshMenuLocalRankings()
        {
            if (menuRankingsOverlay == null) return;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var boardRulesVersion = menuRankingsMode ==
                ConstructionGameMode.BoardGame ? 2 : 0;
            var randomBoard = boardRulesVersion == 2;
            var fillRanks = menuRankingsMode == ConstructionGameMode.Fill;
            var overallRanks = menuRankingsMode != ConstructionGameMode.BoardGame;
            SetRankingCopy(menuRankingsTitle, chinese,
                "LOCAL RANKINGS", "本地排行榜", 28);
            SetRankingCopy(menuRankingsSubtitle, chinese,
                randomBoard
                    ? "Random draw · balance, then linkage, then shared score"
                    : overallRanks
                        ? "This device · current score rules · total score first"
                        : "This device · current score rules · shared score first",
                randomBoard
                    ? "随机抽牌 · 依次比较均衡、联动、共栖分"
                    : overallRanks
                        ? "仅此设备 · 当前计分规则 · 本局总分优先"
                        : "仅此设备 · 当前计分规则 · 共栖分优先", 16);
            SetRankingCopy(menuRankingsCloseLabel, chinese,
                "Close", "关闭", 18);
            var englishModes = new[] { "Fill", "Endless", "Board game" };
            var chineseModes = new[] { "填空", "无尽", "桌游" };
            for (var index = 0; index < englishModes.Length; index++)
            {
                SetRankingCopy(menuRankingsModeLabels[index], chinese,
                    englishModes[index], chineseModes[index], 17);
                menuRankingsModeFaces[index].color =
                    (int)menuRankingsMode == index ? RankingGreen : RankingInk;
            }
            var enHeadings = new[] { "Rank", "Name",
                overallRanks ? "Total" : "Shared", "Animal",
                "Resident", menuRankingsMode == ConstructionGameMode.Fill
                    ? "Award" : randomBoard ? "Balance" : "Days" };
            var zhHeadings = new[] { "名次", "昵称",
                overallRanks ? "总分" : "共栖", "动物",
                "居民", menuRankingsMode == ConstructionGameMode.Fill
                    ? "奖项" : randomBoard ? "均衡" : "天数" };
            for (var column = 0; column < 6; column++)
                SetRankingCopy(menuRankingsHeaders[column], chinese,
                    enHeadings[column], zhHeadings[column], 14);

            List<ConstructionLocalLeaderboardRecord> rankings;
            ConstructionLocalLeaderboardStatus status;
            if (menuLeaderboardStore == null)
            {
                status = ConstructionLocalLeaderboardStatus.Unavailable;
                rankings = new List<ConstructionLocalLeaderboardRecord>();
            }
            else
                status = menuLeaderboardStore.RankingsFor(menuRankingsMode,
                    boardRulesVersion,
                    ConstructionScoreLedger.CurrentRankingVersion(menuRankingsMode),
                    overallRanks ? 1 : 0,
                    out rankings);
            var emptyEnglish = status switch
            {
                ConstructionLocalLeaderboardStatus.Invalid =>
                    "The local rankings file cannot be read. It was not changed.",
                ConstructionLocalLeaderboardStatus.Unavailable =>
                    "The local rankings file is temporarily unavailable.",
                _ => "No completed games under the current score rules yet."
            };
            var emptyChinese = status switch
            {
                ConstructionLocalLeaderboardStatus.Invalid =>
                    "本地排行文件无法读取，原文件没有更改。",
                ConstructionLocalLeaderboardStatus.Unavailable =>
                    "本地排行文件暂时无法读取。",
                _ => "当前计分规则下还没有已完成的对局。"
            };
            SetRankingCopy(menuRankingsEmpty, chinese,
                emptyEnglish, emptyChinese, 18);
            menuRankingsEmpty.gameObject.SetActive(rankings.Count == 0);
            for (var row = 0; row < menuRankingsRowFaces.Length; row++)
            {
                var visible = row < rankings.Count;
                menuRankingsRowFaces[row].gameObject.SetActive(visible);
                menuRankingsAwardIcons[row].gameObject.SetActive(false);
                for (var column = 0; column < 6; column++)
                    menuRankingsCells[row, column].gameObject.SetActive(visible);
                if (!visible) continue;
                var score = rankings[row].score;
                var hasAward = ConstructionAwardPayloadEvaluator.TryEvaluate(
                    score, out var awardTier);
                if (hasAward)
                {
                    menuRankingsAwardIcons[row].sprite = Resources.Load<Sprite>(
                        "UI/Awards/award-" +
                        awardTier.ToString().ToLowerInvariant() + "-v01");
                    menuRankingsAwardIcons[row].gameObject.SetActive(
                        menuRankingsAwardIcons[row].sprite != null);
                }
                var values = new[]
                {
                    "#" + (row + 1), score.nickname,
                    score.totalPoints.ToString(), score.animalPoints.ToString(),
                    score.residentPoints.ToString(), randomBoard
                        ? Math.Min(score.animalPoints, score.residentPoints).ToString()
                        : score.daysPlayed.ToString()
                };
                for (var column = 0; column < values.Length; column++)
                    SetRankingCopy(menuRankingsCells[row, column], chinese,
                        values[column], values[column], 16);
                var lastCell = menuRankingsCells[row, 5];
                if (menuRankingsMode == ConstructionGameMode.Fill)
                {
                    lastCell.rectTransform.anchoredPosition =
                        new Vector2(310f, 135f - row * 40f);
                    lastCell.rectTransform.sizeDelta = new Vector2(66f, 34f);
                    SetRankingCopy(lastCell, chinese,
                        hasAward ? awardTier.ToString() : "—",
                        hasAward ? AwardChineseName(awardTier) : "—", 14);
                }
                else
                {
                    lastCell.rectTransform.anchoredPosition =
                        new Vector2(292f, 135f - row * 40f);
                    lastCell.rectTransform.sizeDelta = new Vector2(100f, 34f);
                }
            }
        }

        private static string AwardChineseName(ConstructionAwardTier tier) =>
            tier switch
            {
                ConstructionAwardTier.Plastic => "塑料",
                ConstructionAwardTier.Iron => "铁",
                ConstructionAwardTier.Bronze => "铜",
                ConstructionAwardTier.Silver => "银",
                _ => "金"
            };

        private static void SetRankingCopy(Text label, bool chinese,
            string english, string chineseCopy, int size)
        {
            ConstructionMainMenuLayout.ApplyModeFont(label, chinese, size);
            label.text = chinese ? chineseCopy : english;
        }

        private static Image RankingImage(string name, Transform parent,
            Color color, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.color = color;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        private static Text RankingText(string name, Transform parent,
            Vector2 position, Vector2 size, int fontSize, Color color,
            TextAnchor alignment)
        {
            var item = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var label = item.GetComponent<Text>();
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            ConstructionMainMenuLayout.ApplyModeFont(label, true, fontSize);
            LegacyTextTmpBridge.Attach(label);
            return label;
        }

        private static Button RankingButton(string name, Transform parent,
            Vector2 position, Vector2 size, Color color,
            UnityEngine.Events.UnityAction click)
        {
            var image = RankingImage(name, parent, color, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            return button;
        }
    }
}
