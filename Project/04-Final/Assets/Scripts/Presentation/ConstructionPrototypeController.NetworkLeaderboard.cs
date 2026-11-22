using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private const string NetworkLeaderboardBaseUrl =
            "https://symbiosis49-own-leaderboard.symbiosis49-independent-leaderboard.workers.dev";

        [Serializable]
        private sealed class NumericScoreSubmission
        {
            public string mode;
            public int scoringVersion;
            public int boardRulesVersion;
            public int score;
            public string displayName;
        }

        [Serializable]
        private sealed class NumericScoreSubmitResponse
        {
            public int score;
            public int rank;
        }

        [Serializable]
        private sealed class NumericScoreListResponse
        {
            public int[] scores;
            public NamedScoreEntry[] entries;
            public int count;
        }

        [Serializable]
        private sealed class NamedScoreEntry
        {
            public string name;
            public int score;
        }

        private Text resultNetworkStatus;
        private string networkAttemptRunId;
        private string networkStatusRunId;
        private string networkStatusMessage;
        private bool networkRequestBusy;
        private bool networkRequestFailed;
        private bool networkDisabledForTesting;
        private bool networkNameResubmitPending;
        private GameObject networkRankingsOverlay;
        private Text networkRankingsSubtitle;
        private InputField networkNicknameInput;
        private Text networkNameSaveLabel;
        private Text networkReviewLabel;
        private readonly Text[] networkRankingRows = new Text[20];
        private NamedScoreEntry[] networkCachedEntries;
        private int networkCachedCount;

        private void BuildNetworkLeaderboardStatus(Transform parent)
        {
            resultNetworkStatus = TextAt("Network leaderboard status", parent,
                new Vector2(0, -346), new Vector2(660, 20), "", 12, false,
                Ink, TextAnchor.MiddleCenter);
            resultNetworkStatus.raycastTarget = true;
            var retry = resultNetworkStatus.gameObject.AddComponent<Button>();
            retry.targetGraphic = resultNetworkStatus;
            retry.onClick.AddListener(OpenOrRetryNetworkLeaderboard);

            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            networkRankingsOverlay = Panel("Online rankings overlay",
                canvas.transform, Vector2.zero, new Vector2(1600, 900),
                new Color(0.05f, 0.09f, 0.11f, 0.82f)).gameObject;
            Stretch(networkRankingsOverlay.GetComponent<RectTransform>());
            var card = Panel("Online rankings card",
                networkRankingsOverlay.transform, Vector2.zero,
                new Vector2(760, 700), Paper);
            ApplySkin(card, "UI/PauseMenu/pause-menu-settings-panel-v01");
            TextAt("Online rankings title", card.transform,
                new Vector2(0, 292), new Vector2(650, 42),
                L("Online leaderboard", "联网排行榜"), 30, true,
                Ink, TextAnchor.MiddleCenter).raycastTarget = false;
            networkRankingsSubtitle = TextAt("Online rankings subtitle",
                card.transform, new Vector2(0, 254),
                new Vector2(650, 44), "", 13, false,
                Ink, TextAnchor.MiddleCenter);
            networkRankingsSubtitle.raycastTarget = false;
            Panel("Online rankings divider", card.transform,
                new Vector2(0, 229), new Vector2(650, 2), Gold)
                .raycastTarget = false;
            for (var index = 0; index < networkRankingRows.Length; index++)
            {
                var column = index / 10;
                var row = index % 10;
                var label = TextAt("Online rank " + (index + 1),
                    card.transform,
                    new Vector2(column == 0 ? -169 : 169, 200 - row * 43),
                    new Vector2(310, 35), "", 16, false,
                    Ink, TextAnchor.MiddleLeft);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 16;
                label.raycastTarget = false;
                label.supportRichText = false;
                networkRankingRows[index] = label;
            }
            var nameEditor = Instantiate(resultNicknameInput.gameObject, card.transform);
            nameEditor.name = "Online nickname";
            var nameRect = nameEditor.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(-135, -247);
            networkNicknameInput = nameEditor.GetComponent<InputField>();
            var saveName = ButtonAt("Save online nickname", card.transform,
                new Vector2(155, -247), new Vector2(225, 40), L("Save & sync name", "保存并同步名字"), 16,
                Ink, Paper, () =>
                {
                    resultNicknameInput.text = networkNicknameInput.text;
                    SaveLocalLeaderboardName();
                    RefreshNetworkRankingsOverlay();
                });
            networkNameSaveLabel = saveName.GetComponentInChildren<Text>();
            ButtonAt("Close online rankings", card.transform,
                new Vector2(-145, -300), new Vector2(230, 42),
                L("Back to result", "返回结算"), 17, Ink, Paper,
                () => networkRankingsOverlay.SetActive(false));
            var review = ButtonAt("Review layout from online rankings", card.transform,
                new Vector2(145, -300), new Vector2(230, 42), L("Review layout", "收起复盘"), 17,
                Ink, Paper, () => SetResultReview(true));
            networkReviewLabel = review.GetComponentInChildren<Text>();
            networkRankingsOverlay.SetActive(false);
        }

        private void OpenOrRetryNetworkLeaderboard()
        {
            if (networkRankingsOverlay == null) return;
            networkNicknameInput.text = resultNicknameInput.text;
            networkRankingsOverlay.SetActive(true);
            networkRankingsOverlay.transform.SetAsLastSibling();
            RefreshNetworkRankingsOverlay();
            if (!networkRequestBusy &&
                (networkRequestFailed || networkCachedEntries == null))
                RetryNetworkLeaderboard();
        }

        private void RefreshNetworkRankingsOverlay()
        {
            if (networkRankingsOverlay == null) return;
            networkNameSaveLabel.text = L("Save & sync name", "保存并同步名字");
            networkReviewLabel.text = L("Review layout", "收起复盘");
            networkRankingsSubtitle.text = networkCachedEntries == null || networkRequestBusy || networkRequestFailed
                ? networkStatusMessage ?? L("Fetching real server records…", "正在读取服务器成绩…")
                : L($"Server records: {networkCachedCount} · same mode and score rules\nOne best score per public IP; players on the same network share a record.",
                    $"服务器共 {networkCachedCount} 条成绩 · 同模式、同计分规则\n当前按公网 IP 保留最高分；同一网络共用一条记录。");
            for (var index = 0; index < networkRankingRows.Length; index++)
            {
                var entry = networkCachedEntries != null && index < networkCachedEntries.Length
                    ? networkCachedEntries[index] : null;
                networkRankingRows[index].text = entry == null
                    ? index == 0 && networkCachedEntries?.Length == 0
                        ? L("No server records under these rules yet.", "当前规则下还没有服务器成绩。")
                        : "" :
                    $"#{index + 1}  {entry.name}    {entry.score}";
            }
        }

        private void BeginNetworkLeaderboardIfNeeded(bool eligible)
        {
            if (resultNetworkStatus == null || run == null) return;
            if (networkDisabledForTesting)
            {
                resultNetworkStatus.text = L("Online board disabled in test run.",
                    "测试对局不连接联网榜。");
                return;
            }
            if (string.IsNullOrEmpty(NetworkLeaderboardBaseUrl))
            {
                resultNetworkStatus.text = L("Online board is being moved. Offline play is ready.",
                    "联网榜迁移中，离线游玩和计分照常可用。");
                return;
            }
            if (networkStatusRunId != run.RunId)
            {
                networkStatusRunId = run.RunId;
                networkStatusMessage = null;
                networkRequestBusy = false;
                networkRequestFailed = false;
                networkNameResubmitPending = false;
                networkCachedEntries = null;
                networkCachedCount = 0;
            }
            if (!eligible)
                networkStatusMessage = L("This save is not on the online board.",
                    "这份旧存档不参加联网排行。");
            else if (networkAttemptRunId != run.RunId && !networkRequestBusy)
                StartNetworkLeaderboardRequest();
            resultNetworkStatus.text = networkStatusMessage ??
                L("Online board: waiting for the final score.",
                    "联网榜：等待最终成绩。");
        }

        private void RetryNetworkLeaderboard()
        {
            if (string.IsNullOrEmpty(NetworkLeaderboardBaseUrl) ||
                networkDisabledForTesting || networkRequestBusy ||
                run?.FinalResult == null) return;
            if (!ConstructionLeaderboardPayload.TryCreate(run, "Player",
                    out _)) return;
            StartNetworkLeaderboardRequest();
        }

        private void StartNetworkLeaderboardRequest()
        {
            if (run == null || networkRequestBusy) return;
            var submission = new NumericScoreSubmission
            {
                mode = run.Mode.ToString(),
                scoringVersion = run.ScoringVersion,
                boardRulesVersion = run.Mode == ConstructionGameMode.BoardGame
                    ? run.BoardGameRulesVersion : 0,
                score = run.OverallScore,
                displayName = (resultNicknameInput?.text ??
                    PlayerPrefs.GetString(LeaderboardNicknamePreferenceKey, "Player")).Trim()
            };
            if (!ValidLocalNickname(submission.displayName))
                submission.displayName = "Player";
            var runId = run.RunId;
            networkAttemptRunId = runId;
            networkRequestBusy = true;
            networkRequestFailed = false;
            networkStatusMessage = L("Syncing final score…",
                "正在同步最终成绩…");
            StartCoroutine(SendAndReadNetworkLeaderboard(runId, submission));
        }

        private IEnumerator SendAndReadNetworkLeaderboard(string runId,
            NumericScoreSubmission submission)
        {
            var endpoint = NetworkLeaderboardBaseUrl + "/api/v2/scores";
            var rank = 0;
            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(JsonUtility.ToJson(submission)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (run?.RunId != runId) yield break;
                if (request.result != UnityWebRequest.Result.Success ||
                    request.responseCode != 200)
                {
                    networkRequestFailed = true;
                    SetNetworkLeaderboardStatus(runId,
                        L("Offline or server unavailable · click to retry",
                            "离线或网站暂不可用 · 点击重试"));
                    FinishNetworkLeaderboardRequest(runId);
                    yield break;
                }
                try
                {
                    rank = JsonUtility.FromJson<NumericScoreSubmitResponse>(
                        request.downloadHandler.text)?.rank ?? 0;
                }
                catch (ArgumentException) { rank = 0; }
            }

            var mode = submission.mode.ToLowerInvariant();
            var query = $"?mode={mode}&version={submission.scoringVersion}" +
                $"&rules={submission.boardRulesVersion}";
            using (var request = UnityWebRequest.Get(endpoint + query))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (run?.RunId != runId) yield break;
                if (request.result != UnityWebRequest.Result.Success ||
                    request.responseCode != 200)
                {
                    networkRequestFailed = true;
                    SetNetworkLeaderboardStatus(runId,
                        rank > 0
                            ? L($"Online rank #{rank} · board unavailable · click to retry",
                                $"联网第 {rank} 名 · 榜单暂不可读 · 点击重试")
                            : L("Score sent · board unavailable · click to retry",
                                "成绩已提交 · 榜单暂不可读 · 点击重试"));
                    FinishNetworkLeaderboardRequest(runId);
                    yield break;
                }
                NumericScoreListResponse list;
                try
                {
                    list = JsonUtility.FromJson<NumericScoreListResponse>(
                        request.downloadHandler.text);
                }
                catch (ArgumentException) { list = null; }
                if (list?.scores == null)
                {
                    networkRequestFailed = true;
                    SetNetworkLeaderboardStatus(runId,
                        L("Score sent · board response invalid · click to retry",
                            "成绩已提交 · 榜单返回异常 · 点击重试"));
                }
                else
                {
                    networkCachedEntries = list.entries;
                    if (networkCachedEntries == null)
                    {
                        networkCachedEntries = new NamedScoreEntry[
                            list.scores.Length];
                        for (var i = 0; i < list.scores.Length; i++)
                            networkCachedEntries[i] = new NamedScoreEntry
                            { name = "Player", score = list.scores[i] };
                    }
                    networkCachedCount = list.count;
                    RefreshNetworkRankingsOverlay();
                    var prefix = rank > 0
                        ? L($"Online #{rank}", $"联网第 {rank} 名")
                        : L("Online board", "联网榜");
                    SetNetworkLeaderboardStatus(runId, prefix +
                        L($" · {networkCachedCount} score(s) · View rankings",
                            $" · {networkCachedCount} 条成绩 · 查看排行"));
                }
            }
            FinishNetworkLeaderboardRequest(runId);
        }

        private void RequestNetworkLeaderboardNameRefresh()
        {
            if (networkDisabledForTesting || run?.FinalResult == null) return;
            if (networkRequestBusy)
            {
                networkNameResubmitPending = true;
                return;
            }
            StartNetworkLeaderboardRequest();
        }

        private void FinishNetworkLeaderboardRequest(string runId)
        {
            if (run?.RunId != runId) return;
            networkRequestBusy = false;
            if (networkRankingsOverlay != null && networkRankingsOverlay.activeSelf)
                RefreshNetworkRankingsOverlay();
            if (!networkNameResubmitPending || run?.RunId != runId) return;
            networkNameResubmitPending = false;
            StartNetworkLeaderboardRequest();
        }

        private void SetNetworkLeaderboardStatus(string runId, string message)
        {
            if (run?.RunId != runId || resultNetworkStatus == null) return;
            networkStatusMessage = message;
            resultNetworkStatus.text = message;
            if (networkRankingsOverlay != null && networkRankingsOverlay.activeSelf)
                RefreshNetworkRankingsOverlay();
        }
    }
}
