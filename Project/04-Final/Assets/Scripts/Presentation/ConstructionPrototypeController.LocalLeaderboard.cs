using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private RectTransform localRankingsCard;
        private RectTransform localRankingsFooterRule;
        private RectTransform localRankingsNameEditor;
        private RectTransform localRankingsCloseRect;
        private RectTransform localRankingsReviewRect;
        private Text localRankingsReviewLabel;
        private InputField localRankingsNicknameInput;
        private Text localRankingsNameCaption;
        private Text localRankingsNamePlaceholder;
        private Text localRankingsSaveLabel;
        private Text localRankingsEditStatus;
        private string localRankingsNameMessage;

        private static void AnchorRankingFromTop(RectTransform rect, float x,
            float top)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
        }

        private void LayoutLocalRankings(int rowCount)
        {
            var extra = 31 * (Mathf.Clamp(rowCount, 1, 10) - 1);
            localRankingsCard.sizeDelta = new Vector2(800, 520 + extra);
            AnchorRankingFromTop(localRankingsFooterRule, 0, 320 + extra);
            AnchorRankingFromTop(localRankingsNameEditor, 0, 356 + extra);
            AnchorRankingFromTop(localRankingsEditStatus.rectTransform,
                0, 392 + extra);
            AnchorRankingFromTop(localRankingsCloseRect, -110, 431 + extra);
            AnchorRankingFromTop(localRankingsReviewRect, 110, 431 + extra);
        }

        private void RefreshResultLeaderboard(ConstructionFinalResult result)
        {
            if (resultLeaderboardStatus == null || run == null) return;
            if (resultLeaderboardMessageRunId != run.RunId)
            {
                resultLeaderboardMessageRunId = run.RunId;
                resultLeaderboardRecordedRunId = null;
                resultLeaderboardMessage = null;
                resultLeaderboardRank = 0;
            }

            var name = PlayerPrefs.GetString(LeaderboardNicknamePreferenceKey,
                "Player").Trim();
            if (!ValidLocalNickname(name)) name = "Player";
            var eligible = ConstructionLeaderboardPayload.TryCreate(run,
                name, out var payload);
            if (eligible && localLeaderboardStore != null &&
                resultLeaderboardRecordedRunId != run.RunId)
            {
                if (!EnsureCurrentRunSavedForRanking())
                    resultLeaderboardMessage = L(
                        "Game save failed. Local ranking was not updated.",
                        "对局存档失败，本地排行尚未更新。");
                else if (!localLeaderboardStore.TryRecord(payload, false,
                    out resultLeaderboardRank))
                {
                    var status = localLeaderboardStore.TryLoad(out _);
                    resultLeaderboardMessage = status ==
                        ConstructionLocalLeaderboardStatus.Invalid
                        ? L("Local ranking file cannot be read; it was left unchanged.",
                            "本地排行文件无法读取，文件未被更改。")
                        : L("Local ranking could not be updated. Try Save name.",
                            "本地排行未能更新，请点保存名字重试。");
                }
                else
                {
                    resultLeaderboardRecordedRunId = run.RunId;
                    resultLeaderboardMessage = null;
                    if (string.IsNullOrWhiteSpace(resultNicknameInput.text))
                        resultNicknameInput.text = name;
                }
            }

            resultSubmitButton.interactable = eligible;
            resultNicknameInput.interactable = eligible;
            resultSubmitLabel.text = L("Save name", "保存名字");
            resultViewRankingLabel.text = L("Local ranks", "本地排行");
            resultLeaderboardStatus.text = !string.IsNullOrEmpty(resultLeaderboardMessage)
                ? resultLeaderboardMessage
                : !eligible
                    ? L("This older save uses different scoring rules.",
                        "此旧存档使用不同计分规则，不参与当前排行。")
                    : resultLeaderboardRecordedRunId == run.RunId
                        ? L($"Saved on this device · #{resultLeaderboardRank} in this mode",
                            $"已保存到本机 · 本模式第 {resultLeaderboardRank} 名")
                        : L("Local ranking is unavailable.",
                            "本地排行暂时不可用。");
            BeginNetworkLeaderboardIfNeeded(eligible);
        }

        private bool EnsureCurrentRunSavedForRanking()
        {
            if (store == null) return false;
            if (runSavedAtLeastOnce && !runNeedsSave) return true;
            if (!store.TrySave(run)) return false;
            runSavedAtLeastOnce = true;
            runNeedsSave = false;
            return true;
        }

        private void SaveLocalLeaderboardName()
        {
            if (run?.FinalResult == null) return;
            var nickname = resultNicknameInput.text.Trim();
            if (!ValidLocalNickname(nickname))
            {
                resultLeaderboardMessage = L(
                    "Use 1–16 letters, numbers or Chinese characters.",
                    "昵称限 1–16 个汉字、字母或数字，可用空格、点和连字符。");
                RefreshResultLeaderboard(run.FinalResult);
                return;
            }
            if (!ConstructionLeaderboardPayload.TryCreate(run, nickname,
                    out var payload))
            {
                resultLeaderboardMessage = L(
                    "This run uses older scoring rules and cannot enter the current ranking.",
                    "本局使用旧计分规则，不能加入当前排行。");
                RefreshResultLeaderboard(run.FinalResult);
                return;
            }
            var localSaved = localLeaderboardStore != null &&
                EnsureCurrentRunSavedForRanking() &&
                localLeaderboardStore.TryRecord(payload, true,
                    out resultLeaderboardRank);
            if (!localSaved)
            {
                resultLeaderboardMessage = L(
                    "Name saved on this device, but local ranking could not be updated.",
                    "名字已记在本机，但本地排行未能更新。");
            }
            else resultLeaderboardRecordedRunId = run.RunId;
            if (!languageOverriddenForTesting)
            {
                PlayerPrefs.SetString(LeaderboardNicknamePreferenceKey, nickname);
                PlayerPrefs.Save();
            }
            RequestNetworkLeaderboardNameRefresh();
            if (localSaved)
                resultLeaderboardMessage = L(
                    $"Saved on this device · #{resultLeaderboardRank} in this mode",
                    $"已保存到本机 · 本模式第 {resultLeaderboardRank} 名");
            RefreshResultLeaderboard(run.FinalResult);
            if (localRankingsOverlay != null && localRankingsOverlay.activeSelf)
                RefreshLocalRankings();
        }

        private static bool ValidLocalNickname(string name) =>
            !string.IsNullOrWhiteSpace(name) &&
            Regex.IsMatch(name,
                @"\A[A-Za-z0-9_. \u4e00-\u9fff-]{1,16}\z") &&
            !name.Contains("  ");

        private void OpenLocalRankings()
        {
            if (localRankingsOverlay == null) return;
            localRankingsMode = run?.Mode ?? ConstructionGameMode.Fill;
            localRankingsNicknameInput.text = resultNicknameInput.text;
            localRankingsNameMessage = null;
            localRankingsOverlay.SetActive(true);
            localRankingsOverlay.transform.SetAsLastSibling();
            RefreshLocalRankings();
            StartCoroutine(FocusLocalRankingsName());
        }

        private IEnumerator FocusLocalRankingsName()
        {
            // The click that opens this overlay still owns selection in the
            // current frame. Select the InputField after the card is laid out.
            yield return null;
            if (localRankingsOverlay == null ||
                !localRankingsOverlay.activeInHierarchy ||
                localRankingsNicknameInput == null ||
                !localRankingsNicknameInput.isActiveAndEnabled ||
                !localRankingsNicknameInput.interactable ||
                EventSystem.current == null)
                yield break;
            localRankingsNicknameInput.caretWidth = 2;
            localRankingsNicknameInput.customCaretColor = true;
            localRankingsNicknameInput.caretColor = Ink;
            localRankingsNicknameInput.selectionColor =
                new Color(0.75f, 0.65f, 0.38f, 0.55f);
            EventSystem.current.SetSelectedGameObject(
                localRankingsNicknameInput.gameObject);
            localRankingsNicknameInput.ActivateInputField();
        }

        private void SaveNameFromLocalRankings()
        {
            if (localRankingsNicknameInput == null ||
                resultNicknameInput == null) return;
            resultNicknameInput.text = localRankingsNicknameInput.text;
            SaveLocalLeaderboardName();
            localRankingsNameMessage = resultLeaderboardMessage;
            RefreshLocalRankings();
        }

        private void SelectLocalRankingsMode(ConstructionGameMode mode)
        {
            localRankingsMode = mode;
            RefreshLocalRankings();
        }

        private void RefreshLocalRankings()
        {
            if (localRankingsOverlay == null) return;
            var chinese = Chinese;
            localRankingsTitle.text = L("LOCAL RANKINGS", "本地排行榜");
            localRankingsNameCaption.text = L("MY NAME", "我的名字");
            localRankingsNamePlaceholder.text = L("1–16 characters",
                "1–16 个字");
            localRankingsSaveLabel.text = L("Save name", "保存名字");
            var canEditName = run?.FinalResult != null &&
                ConstructionLeaderboardPayload.TryCreate(run, "Player", out _);
            localRankingsNameEditor.gameObject.SetActive(canEditName);
            localRankingsEditStatus.text = !canEditName
                ? L("Only a completed current-rules game can change its name.",
                    "只有按当前规则完成的对局才能改名。")
                : localRankingsNameMessage ?? L(
                    "Type a new name, then save. Your score stays the same.",
                    "直接输入新名字，再点保存；分数不会变。");
            var boardRulesVersion = localRankingsMode == ConstructionGameMode.BoardGame
                ? run != null && run.Mode == ConstructionGameMode.BoardGame
                    ? run.BoardGameRulesVersion : 2
                : 0;
            var rankingScoringVersion = run != null && run.Mode == localRankingsMode && run.IsFinished &&
                run.HasLinkageScore ? run.ScoringVersion :
                ConstructionScoreLedger.CurrentRankingVersion(localRankingsMode);
            localRankingsSubtitle.text = localRankingsMode ==
                ConstructionGameMode.BoardGame
                ? boardRulesVersion == 2
                    ? L($"Random draw · score rules {rankingScoringVersion} · balance → linkage → shared",
                        $"随机抽牌 · 计分规则 {rankingScoringVersion} · 均衡→联动→共栖")
                    : L($"Earlier tabletop rules · score rules {rankingScoringVersion} · this device",
                        $"旧版桌游 · 计分规则 {rankingScoringVersion} · 仅此设备")
                : rankingScoringVersion >= 13 || localRankingsMode == ConstructionGameMode.Fill &&
                    run != null && run.UsesOverallScore
                    ? L($"This device · score rules {rankingScoringVersion} · total score first",
                        $"仅此设备 · 计分规则 {rankingScoringVersion} · 本局总分优先")
                    : L($"This device · score rules {rankingScoringVersion} · shared score first",
                        $"仅此设备 · 计分规则 {rankingScoringVersion} · 共栖分优先");
            localRankingsCloseLabel.text = L("Close", "关闭");
            localRankingsReviewLabel.text = L("Review layout", "收起复盘");
            localRankingsReviewRect.gameObject.SetActive(run?.FinalResult != null);
            var modeNames = chinese
                ? new[] { "填空", "无尽", "桌游" }
                : new[] { "Fill", "Endless", "Board game" };
            for (var i = 0; i < modeNames.Length; i++)
            {
                localRankingsModeLabels[i].text = modeNames[i];
                localRankingsModeFaces[i].color = (int)localRankingsMode == i
                    ? UsedGreen : Ink;
            }
            var randomBoardRanks = localRankingsMode ==
                ConstructionGameMode.BoardGame && boardRulesVersion == 2;
            var awardColumn = localRankingsMode == ConstructionGameMode.Fill &&
                ConstructionAwardModel.SupportsScoringVersion(
                    rankingScoringVersion);
            var headings = chinese
                ? new[] { "名次", "昵称", rankingScoringVersion >= 13 || localRankingsMode ==
                    ConstructionGameMode.Fill && run != null &&
                    run.UsesSpecialTasks ? "总分" : "共栖", "动物", "居民",
                    randomBoardRanks ? "均衡" : awardColumn ? "奖项" : "天数" }
                : new[] { "Rank", "Name", rankingScoringVersion >= 13 || localRankingsMode ==
                    ConstructionGameMode.Fill && run != null &&
                    run.UsesSpecialTasks ? "Total" : "Shared", "Animal", "Resident",
                    randomBoardRanks ? "Balance" : awardColumn ? "Award" : "Days" };
            for (var column = 0; column < headings.Length; column++)
                localRankingsHeaders[column].text = headings[column];

            List<ConstructionLocalLeaderboardRecord> rankings;
            ConstructionLocalLeaderboardStatus status;
            if (localLeaderboardStore == null)
            {
                status = ConstructionLocalLeaderboardStatus.Unavailable;
                rankings = new List<ConstructionLocalLeaderboardRecord>();
            }
            else
                status = localLeaderboardStore.RankingsFor(localRankingsMode,
                    boardRulesVersion, rankingScoringVersion,
                    rankingScoringVersion >= 13 || localRankingsMode == ConstructionGameMode.Fill &&
                    (run?.Mode != ConstructionGameMode.Fill || run.UsesSpecialTasks) ? 1 : 0,
                    out rankings);
            localRankingsEmpty.text = status switch
            {
                ConstructionLocalLeaderboardStatus.Invalid => L(
                    "The local ranking file cannot be read. It has not been changed.",
                    "本地排行文件无法读取，文件没有更改。"),
                ConstructionLocalLeaderboardStatus.Unavailable => L(
                    "The local ranking file is temporarily unavailable.",
                    "本地排行文件暂时无法读取。"),
                _ => L("No completed games recorded in this mode yet.",
                    "此模式还没有已记录的完整对局。")
            };
            localRankingsEmpty.gameObject.SetActive(rankings.Count == 0);
            for (var row = 0; row < 10; row++)
            {
                var visible = row < rankings.Count;
                localRankingsRowFaces[row].gameObject.SetActive(visible);
                localRankingsAwardIcons[row].gameObject.SetActive(false);
                for (var column = 0; column < 6; column++)
                    localRankingsCells[row, column].gameObject.SetActive(visible);
                if (!visible) continue;
                var score = rankings[row].score;
                localRankingsCells[row, 0].text = "#" + (row + 1);
                localRankingsCells[row, 1].text = score.nickname;
                localRankingsCells[row, 2].text = score.totalPoints.ToString();
                localRankingsCells[row, 3].text = score.animalPoints.ToString();
                localRankingsCells[row, 4].text = score.residentPoints.ToString();
                ConstructionAwardTier awardTier = default;
                var awardEligible = awardColumn &&
                    ConstructionAwardPayloadEvaluator.TryEvaluate(score,
                        out awardTier);
                var finalCell = localRankingsCells[row, 5];
                finalCell.rectTransform.anchoredPosition =
                    new UnityEngine.Vector2(awardEligible ? 285 : 273,
                        finalCell.rectTransform.anchoredPosition.y);
                finalCell.rectTransform.sizeDelta = new UnityEngine.Vector2(
                    awardEligible ? 66 : 90, 28);
                finalCell.fontSize = awardEligible ? 14 : 16;
                finalCell.text = awardEligible
                    ? AwardRankShortName(awardTier)
                    : randomBoardRanks
                        ? Math.Min(score.animalPoints,
                            score.residentPoints).ToString()
                        : score.daysPlayed.ToString();
                if (awardEligible)
                {
                    localRankingsAwardIcons[row].sprite = AwardSprite(awardTier);
                    localRankingsAwardIcons[row].gameObject.SetActive(true);
                }
            }
            LayoutLocalRankings(rankings.Count);
        }

        private string AwardRankShortName(ConstructionAwardTier tier) =>
            tier switch
            {
                ConstructionAwardTier.Plastic => L("Plastic", "塑料"),
                ConstructionAwardTier.Iron => L("Iron", "铁牌"),
                ConstructionAwardTier.Bronze => L("Bronze", "铜杯"),
                ConstructionAwardTier.Silver => L("Silver", "银杯"),
                _ => L("Gold", "金杯")
            };
    }
}
