using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    public sealed partial class ConstructionPrototypeController
    {
        private static readonly Vector2[] SpecialTaskCardPositions =
        {
            new(-370f, 0f), new(0f, 0f), new(370f, 0f)
        };

        private RectTransform specialTaskSelectionRoot;
        private CanvasGroup specialTaskSelectionCanvas;
        private RawImage specialTaskBlurImage;
        private Text specialTaskSelectionHeading;
        private Text specialTaskSelectionSubtitle;
        private Text specialTaskSelectionNote;
        private readonly RectTransform[] specialTaskCards = new RectTransform[3];
        private readonly Image[] specialTaskCardBackings = new Image[3];
        private readonly Image[] specialTaskArtImages = new Image[3];
        private readonly Text[] specialTaskDifficultyLabels = new Text[3];
        private readonly Text[] specialTaskTitles = new Text[3];
        private readonly Text[] specialTaskRules = new Text[3];
        private readonly Text[] specialTaskEffects = new Text[3];
        private readonly Text[] specialTaskProgress = new Text[3];
        private readonly Image[] specialTaskProgressFills = new Image[3];
        private readonly Image[] specialTaskProgressTracks = new Image[3];
        private readonly Text[] specialTaskPoints = new Text[3];
        private readonly Text[] specialTaskPointUnitLabels = new Text[3];
        private readonly Button[] specialTaskReplaceButtons = new Button[3];
        private readonly Text[] specialTaskReplaceLabels = new Text[3];
        private Button specialTaskConfirmButton;
        private Text specialTaskConfirmLabel;
        private Button specialTaskCloseButton;
        private Text specialTaskCloseLabel;
        private Button specialTaskTrayButton;
        private Text specialTaskTrayLabel;
        private TextMeshProUGUI specialTaskTrayCount;
        private RenderTexture specialTaskBlurTexture;
        private Coroutine specialTaskAnimation;
        private bool specialTaskSelectionStarted;
        private string specialTaskPresentationRunId;
        private bool specialTaskReviewOpen;
        private bool specialTaskAnimating;
        private Image goalOverviewBackdrop;
        private RectTransform goalOverviewRoot;
        private Text goalOverviewCount;
        private Text goalOverviewHeading;
        private readonly Image[] goalOverviewIcons = new Image[3];
        private readonly Image[] goalOverviewCompanionIcons = new Image[3];
        private readonly Text[] goalOverviewTitles = new Text[3];
        private readonly Text[] goalOverviewStates = new Text[3];
        private readonly Text[] goalOverviewProgress = new Text[3];
        private readonly Image[] goalOverviewFills = new Image[3];
        private Image goalOverviewCompletionBadge;
        private readonly GameObject[] goalOverviewSeals = new GameObject[3];

        private void BuildGoalOverview(Transform parent)
        {
            if (run?.UsesSpecialTasks != true) return;
            BuildNeighborhoodStories(parent);
            goalOverviewBackdrop = Panel("Goal progress paper", parent,
                new Vector2(-CaseLeafCenter - 10f, 0f),
                new Vector2(288f, 208f), Color.clear);
            goalOverviewBackdrop.raycastTarget = false;
            parent = goalOverviewBackdrop.transform;
            Panel("Goal heading rule", parent, new Vector2(0f, 103f),
                new Vector2(272f, 1f), HandQuietBrass).raycastTarget = false;
            var heading = TextAt("Goal progress heading", parent,
                new Vector2(-52f, 83f), new Vector2(160f, 34f),
                L("Goals", "本局目标"), 21, true, HeadingInk,
                TextAnchor.MiddleLeft);
            heading.raycastTarget = false;
            goalOverviewHeading = heading;
            goalOverviewRoot = NewRect("Goal progress overview", parent,
                Vector2.zero, new Vector2(264f, 172f));
            goalOverviewCompletionBadge = Panel("Goal completion badge", parent,
                new Vector2(82f, 83f), new Vector2(108f, 28f), Color.clear);
            goalOverviewCompletionBadge.raycastTarget = false;
            goalOverviewCount = TextAt("Goals completed", parent,
                new Vector2(82f, 83f), new Vector2(108f, 28f), "", 16,
                false, Ink, TextAnchor.MiddleRight);
            goalOverviewCount.raycastTarget = false;
            for (var slot = 0; slot < 3; slot++)
            {
                var row = NewRect($"Goal progress row {slot + 1}",
                    goalOverviewRoot, new Vector2(0f, 40f - 54f * slot),
                    new Vector2(264f, 52f));
                var mat = Panel("Goal illustration mount", row,
                    new Vector2(-114f, 3f), new Vector2(38f, 38f), HandPaper);
                mat.raycastTarget = false;
                var outline = mat.gameObject.AddComponent<Outline>();
                outline.effectColor = HandInk;
                outline.effectDistance = new Vector2(1f, -1f);
                goalOverviewIcons[slot] = Panel("Goal icon", row,
                    new Vector2(-114f, 3f), new Vector2(36f, 36f),
                    Color.white);
                goalOverviewIcons[slot].preserveAspect = true;
                goalOverviewIcons[slot].raycastTarget = false;
                goalOverviewCompanionIcons[slot] = Panel("Goal companion card", row,
                    new Vector2(-102f, 3f), new Vector2(20f, 30f), Color.white);
                goalOverviewCompanionIcons[slot].preserveAspect = true;
                goalOverviewCompanionIcons[slot].raycastTarget = false;
                goalOverviewTitles[slot] = TextAt("Goal title", row,
                    new Vector2(9f, 14f), new Vector2(186f, 28f), "", 18,
                    true, Ink, TextAnchor.MiddleLeft);
                goalOverviewTitles[slot].raycastTarget = false;
                goalOverviewStates[slot] = TextAt("Goal state", row,
                    new Vector2(-22f, -9f), new Vector2(124f, 24f), "", 16,
                    false, HeadingInk, TextAnchor.MiddleLeft);
                goalOverviewStates[slot].raycastTarget = false;
                goalOverviewProgress[slot] = TextAt("Goal count", row,
                    new Vector2(76f, -9f), new Vector2(60f, 34f), "", 20,
                    true, Ink, TextAnchor.MiddleRight);
                goalOverviewProgress[slot].raycastTarget = false;
                goalOverviewProgress[slot].resizeTextForBestFit = true;
                goalOverviewProgress[slot].resizeTextMinSize = 18;
                goalOverviewProgress[slot].resizeTextMaxSize = 21;
                var seal = Panel("Goal achieved seal", row,
                    new Vector2(-101f, -8f), new Vector2(18f, 18f), ReadingGreen);
                seal.raycastTarget = false;
                var tickShort = Panel("Tick short", seal.transform,
                    new Vector2(-3f, -1f), new Vector2(3f, 7f), Paper);
                tickShort.raycastTarget = false;
                tickShort.transform.localEulerAngles = new Vector3(0f, 0f, 45f);
                var tickLong = Panel("Tick long", seal.transform,
                    new Vector2(2f, 1f), new Vector2(3f, 11f), Paper);
                tickLong.raycastTarget = false;
                tickLong.transform.localEulerAngles = new Vector3(0f, 0f, -35f);
                goalOverviewSeals[slot] = seal.gameObject;
                seal.gameObject.SetActive(false);
                var track = Panel("Goal progress track", row,
                    new Vector2(18f, -25f), new Vector2(216f, 2f),
                    new Color(0.61f, 0.62f, 0.50f, 0.55f));
                track.raycastTarget = false;
                goalOverviewFills[slot] = Panel("Goal progress fill", row,
                    new Vector2(-90f, -25f), new Vector2(0f, 3f), UsedGreen);
                goalOverviewFills[slot].rectTransform.pivot =
                    new Vector2(0f, 0.5f);
                goalOverviewFills[slot].raycastTarget = false;
            }
            goalOverviewBackdrop.gameObject.SetActive(false);
        }

        private void RefreshGoalOverview()
        {
            if (goalOverviewRoot == null || run == null) return;
            goalOverviewHeading.text = L("Goals", "本局目标");
            var visible = run.IsSpecialTaskSelectionConfirmed &&
                !run.IsIntroPhase;
            goalOverviewBackdrop.gameObject.SetActive(visible);
            RefreshNeighborhoodStories(visible);
            var showStoryCopy = !visible && !(run.UsesFillHand &&
                run.IsIntroPhase && !run.TutorialTipsSkipped);
            storyPreviewText.gameObject.SetActive(showStoryCopy);
            roomFunctionText.gameObject.SetActive(showStoryCopy);
            storyCueIcon.gameObject.SetActive(showStoryCopy);
            storyDivider.gameObject.SetActive(showStoryCopy);
            skipTutorialButton.gameObject.SetActive(run.IsIntroPhase &&
                !run.BuiltToday && !run.IsFinished);
            if (!visible) return;
            var tasks = run.SpecialTasks;
            var completed = tasks.Count(task => task.Completed);
            goalOverviewCount.text = L($"{completed}/3 done", $"已达成 {completed}/3");
            goalOverviewCount.color = completed == 3 ? ReadingGreen : Ink;
            for (var slot = 0; slot < goalOverviewIcons.Length; slot++)
            {
                var task = tasks[slot];
                goalOverviewIcons[slot].sprite = GoalOverviewCardArt(task.Kind);
                var commute = task.Kind == ConstructionSpecialTaskKind.CommuteCycles;
                SetRect(goalOverviewIcons[slot].rectTransform,
                    new Vector2(commute ? -124f : -114f, 3f),
                    commute ? new Vector2(20f, 30f) : new Vector2(36f, 36f));
                goalOverviewCompanionIcons[slot].gameObject.SetActive(commute);
                goalOverviewCompanionIcons[slot].sprite = ConstructionHandArtCatalog.GetRoom(ConstructionCategory.Workshop);
                goalOverviewTitles[slot].text = SpecialTaskDisplayTitle(task.Kind);
                goalOverviewProgress[slot].text =
                    $"{task.Progress}/{task.Target}";
                var endScore = task.Kind is
                    ConstructionSpecialTaskKind.CommuteCycles or
                    ConstructionSpecialTaskKind.CompleteCommunities;
                goalOverviewStates[slot].text = task.Completed
                    ? endScore
                        ? run.IsFinished
                            ? L("Awarded", "已结算")
                            : L("At finish", "终局结算")
                        : L("Active", "已生效")
                    : GoalOverviewCounterLabel(task.Kind);
                goalOverviewSeals[slot].SetActive(task.Completed);
                goalOverviewStates[slot].color = task.Completed ? ReadingGreen : HeadingInk;
                goalOverviewProgress[slot].color = task.Completed ? ReadingGreen : Ink;
                goalOverviewFills[slot].rectTransform.sizeDelta =
                    new Vector2(216f * Mathf.Clamp01(
                        task.Progress / (float)Mathf.Max(1, task.Target)), 3f);
                goalOverviewFills[slot].color = UsedGreen;
            }
        }
        private static Sprite GoalOverviewCardArt(ConstructionSpecialTaskKind kind) =>
            ConstructionHandArtCatalog.GetRoom(kind switch
            {
                ConstructionSpecialTaskKind.BuildSquares => ConstructionCategory.Square,
                ConstructionSpecialTaskKind.AvoidStreets => ConstructionCategory.Street,
                ConstructionSpecialTaskKind.ConnectGreens or ConstructionSpecialTaskKind.RaiseAnimals => ConstructionCategory.Green,
                _ => ConstructionCategory.Residence
            }, kind == ConstructionSpecialTaskKind.RaiseAnimals ? GreenPlanting.Oak : GreenPlanting.Meadow);
        private string GoalOverviewCounterLabel(ConstructionSpecialTaskKind kind) => kind switch
        {
            ConstructionSpecialTaskKind.CommuteCycles => L("Commutes", "完成通勤"),
            ConstructionSpecialTaskKind.RaiseAnimals => run.UsesSpeciesTaskRules
                ? L("Species", "动物种类") : L("Animals", "动物数量"),
            ConstructionSpecialTaskKind.BuildSquares => L("Plazas built", "已建广场"),
            ConstructionSpecialTaskKind.AvoidStreets => L("No streets", "连续未铺路"),
            ConstructionSpecialTaskKind.ConnectGreens => L("Linked greens", "连片绿地"),
            ConstructionSpecialTaskKind.CompleteCommunities => L("Communities", "完整街坊"),
            _ => string.Empty
        };
        private void BuildSpecialTasks(Transform canvas)
        {
            if (run?.UsesSpecialTasks != true) return;

            specialTaskTrayButton = ButtonAt("Special task tray", canvas,
                new Vector2(CaseLeafCenter + 132f, 282f),
                new Vector2(78f, 120f),
                "", 14, Ink, Paper,
                OpenSpecialTaskReview, themed: false);
            var trayImage = specialTaskTrayButton.GetComponent<Image>();
            trayImage.sprite = LoadSprite(
                "UI/ConstructionSpecialTasks/bookmark-tab-v3");
            trayImage.color = trayImage.sprite == null ? Ink : Color.white;
            specialTaskTrayLabel = TextAt("Bookmark title",
                specialTaskTrayButton.transform, new Vector2(0f, 38f),
                new Vector2(60f, 28f), "", 13, true, Ink,
                TextAnchor.MiddleCenter);
            specialTaskTrayLabel.raycastTarget = false;
            var countRect = NewRect("Bookmark count",
                specialTaskTrayButton.transform, new Vector2(0f, -14f),
                new Vector2(60f, 54f));
            specialTaskTrayCount = countRect.gameObject
                .AddComponent<TextMeshProUGUI>();
            specialTaskTrayCount.font = Resources.Load<TMP_FontAsset>(
                "Fonts/TMP/Nunito-Bold SDF");
            specialTaskTrayCount.fontSize = 38f;
            specialTaskTrayCount.alignment = TextAlignmentOptions.Center;
            specialTaskTrayCount.color = Paper;
            specialTaskTrayCount.text = "3";
            specialTaskTrayCount.raycastTarget = false;
            var rootImage = Panel("Special task selection", canvas,
                Vector2.zero, new Vector2(1600f, 900f), Color.clear);
            specialTaskSelectionRoot = rootImage.rectTransform;
            Stretch(specialTaskSelectionRoot);
            rootImage.raycastTarget = true;
            specialTaskSelectionCanvas = rootImage.gameObject
                .AddComponent<CanvasGroup>();
            specialTaskSelectionCanvas.alpha = 0f;
            specialTaskSelectionCanvas.interactable = true;
            specialTaskSelectionCanvas.blocksRaycasts = true;

            var blurObject = new GameObject("Blurred gameplay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            blurObject.transform.SetParent(rootImage.transform, false);
            specialTaskBlurImage = blurObject.GetComponent<RawImage>();
            Stretch(specialTaskBlurImage.rectTransform);
            specialTaskBlurImage.color = new Color(0.78f, 0.82f, 0.82f, 1f);
            specialTaskBlurImage.raycastTarget = false;

            var dimmer = Panel("Dark soft-focus veil", rootImage.transform,
                Vector2.zero, new Vector2(1600f, 900f),
                new Color(0.015f, 0.025f, 0.04f, 0.76f));
            Stretch(dimmer.rectTransform);
            dimmer.raycastTarget = true;

            Panel("Heading paper", rootImage.transform,
                new Vector2(0f, 340f), new Vector2(520f, 68f),
                new Color(ReadingPaper.r, ReadingPaper.g, ReadingPaper.b, 0.98f))
                .raycastTarget = false;
            Panel("Heading left rule", rootImage.transform,
                new Vector2(-430f, 340f), new Vector2(310f, 3f), Gold)
                .raycastTarget = false;
            Panel("Heading right rule", rootImage.transform,
                new Vector2(430f, 340f), new Vector2(310f, 3f), Gold)
                .raycastTarget = false;
            specialTaskSelectionHeading = TextAt("Special task selection heading",
                rootImage.transform, new Vector2(0f, 340f),
                new Vector2(520f, 64f), "", 28, true, Ink,
                TextAnchor.MiddleCenter);
            specialTaskSelectionHeading.raycastTarget = false;
            specialTaskSelectionSubtitle = TextAt(
                "Special task selection subtitle", rootImage.transform,
                new Vector2(0f, 288f), new Vector2(750f, 34f), "", 17,
                false, Paper, TextAnchor.MiddleCenter);
            specialTaskSelectionSubtitle.raycastTarget = false;

            for (var slot = 0; slot < specialTaskCards.Length; slot++)
                BuildSpecialTaskCard(rootImage.transform, slot);

            specialTaskConfirmButton = ButtonAt("Confirm special tasks",
                rootImage.transform, new Vector2(0f, -316f),
                new Vector2(510f, 68f), "CONFIRM THESE 3 TASKS", 20,
                Ink, Paper, ConfirmSpecialTasks, themed: false);
            specialTaskConfirmLabel = specialTaskConfirmButton
                .GetComponentInChildren<Text>();
            var confirmOutline = specialTaskConfirmButton.gameObject
                .AddComponent<Outline>();
            confirmOutline.effectColor = Gold;
            confirmOutline.effectDistance = new Vector2(2f, -2f);

            specialTaskCloseButton = ButtonAt("Close special task review",
                rootImage.transform, new Vector2(0f, -316f),
                new Vector2(330f, 62f), "BACK TO GAME", 18,
                Ink, Paper, CloseSpecialTaskReview, themed: false);
            specialTaskCloseLabel = specialTaskCloseButton
                .GetComponentInChildren<Text>();
            var closeOutline = specialTaskCloseButton.gameObject
                .AddComponent<Outline>();
            closeOutline.effectColor = Gold;
            closeOutline.effectDistance = new Vector2(2f, -2f);

            specialTaskSelectionNote = TextAt("Special task selection note",
                rootImage.transform, new Vector2(0f, -374f),
                new Vector2(820f, 30f), "", 14, false, Paper,
                TextAnchor.MiddleCenter);
            specialTaskSelectionNote.raycastTarget = false;
            specialTaskSelectionRoot.gameObject.SetActive(false);
            specialTaskTrayButton.gameObject.SetActive(false);
        }

        private void BuildSpecialTaskCard(Transform parent, int slot)
        {
            var captured = slot;
            var card = Panel($"Special task card {slot + 1}", parent,
                SpecialTaskCardPositions[slot], new Vector2(340f, 490f),
                new Color(ReadingPaper.r, ReadingPaper.g, ReadingPaper.b, 0.99f));
            specialTaskCards[slot] = card.rectTransform;
            specialTaskCardBackings[slot] = card;
            card.raycastTarget = true;
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.07f, 0.08f, 0.75f);
            shadow.effectDistance = new Vector2(8f, -8f);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            Panel("Illustration frame", card.transform, new Vector2(0f, 79f),
                new Vector2(316f, 316f), Gold).raycastTarget = false;
            var art = Panel("Task illustration", card.transform,
                new Vector2(0f, 79f), new Vector2(312f, 312f), Color.white);
            art.preserveAspect = true;
            art.raycastTarget = false;
            specialTaskArtImages[slot] = art;
            Panel("Difficulty badge", card.transform,
                new Vector2(-107f, 207f), new Vector2(74f, 30f), Ink)
                .raycastTarget = false;
            specialTaskDifficultyLabels[slot] = TextAt("Task difficulty",
                card.transform, new Vector2(-107f, 207f),
                new Vector2(74f, 30f), "", 16, true, Paper,
                TextAnchor.MiddleCenter);
            specialTaskDifficultyLabels[slot].raycastTarget = false;
            var rewardSeal = Panel("Reward seal", card.transform,
                new Vector2(115f, 201f), new Vector2(82f, 82f), Color.white);
            rewardSeal.sprite = LoadSprite(
                "UI/ConstructionSpecialTasks/reward-seal-v1");
            rewardSeal.preserveAspect = true;
            rewardSeal.raycastTarget = false;
            specialTaskPoints[slot] = TextAt("Task points", card.transform,
                new Vector2(115f, 207f), new Vector2(76f, 38f), "", 25,
                true, Ink, TextAnchor.MiddleCenter);
            specialTaskPoints[slot].raycastTarget = false;
            specialTaskPointUnitLabels[slot] = TextAt("Points unit", card.transform,
                new Vector2(115f, 179f), new Vector2(52f, 20f),
                L("PTS", "分"), 12, false, Ink,
                TextAnchor.MiddleCenter);
            specialTaskPointUnitLabels[slot].raycastTarget = false;

            specialTaskTitles[slot] = TextAt("Task title", card.transform,
                new Vector2(0f, -106f), new Vector2(304f, 38f), "", 24,
                true, HeadingInk, TextAnchor.MiddleCenter);
            specialTaskTitles[slot].raycastTarget = false;
            Panel("Title rule", card.transform,
                new Vector2(0f, -127f), new Vector2(274f, 2f), Gold)
                .raycastTarget = false;
            specialTaskRules[slot] = TextAt("Task rule", card.transform,
                new Vector2(0f, -153f), new Vector2(298f, 48f), "", 13,
                false, Ink, TextAnchor.MiddleCenter);
            specialTaskRules[slot].raycastTarget = false;
            specialTaskEffects[slot] = TextAt("Unlocked task effect",
                card.transform, new Vector2(0f, -191f),
                new Vector2(300f, 30f), "", 12, true, UsedGreen,
                TextAnchor.MiddleCenter);
            specialTaskEffects[slot].raycastTarget = false;
            specialTaskProgress[slot] = TextAt("Task progress", card.transform,
                new Vector2(0f, -219f), new Vector2(290f, 24f), "", 15,
                true, Ink, TextAnchor.MiddleLeft);
            specialTaskProgress[slot].raycastTarget = false;
            specialTaskProgressTracks[slot] = Panel("Progress track",
                card.transform, new Vector2(0f, -237f),
                new Vector2(290f, 7f), new Color(0.78f, 0.76f, 0.66f));
            specialTaskProgressTracks[slot].raycastTarget = false;
            specialTaskProgressFills[slot] = Panel("Progress fill",
                card.transform, new Vector2(-145f, -237f),
                new Vector2(0f, 7f), Gold);
            specialTaskProgressFills[slot].rectTransform.pivot =
                new Vector2(0f, 0.5f);
            specialTaskProgressFills[slot].rectTransform.anchoredPosition =
                new Vector2(-145f, -237f);
            specialTaskProgressFills[slot].raycastTarget = false;

            var replace = ButtonAt("Replace special task", card.transform,
                new Vector2(0f, -224f), new Vector2(230f, 30f), "SWAP 1",
                13, Ink, Paper, () => ReplaceSpecialTask(captured),
                themed: false);
            specialTaskReplaceButtons[slot] = replace;
            specialTaskReplaceLabels[slot] = replace.GetComponentInChildren<Text>();
            var replaceOutline = replace.gameObject.AddComponent<Outline>();
            replaceOutline.effectColor = Gold;
            replaceOutline.effectDistance = new Vector2(1f, -1f);
        }

        private void ReplaceSpecialTask(int slot)
        {
            if (specialTaskAnimating || run?.TryReplaceSpecialTask(slot) != true)
            {
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                return;
            }
            ClearResetConfirmation();
            feedbackText.text = L(
                "A new task has arrived. This card's one opening swap is now used.",
                "换来了一张新任务！这张牌的开局更换机会已经用完。");
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            Save();
            RefreshSpecialTasks();
        }

        private void ConfirmSpecialTasks()
        {
            if (specialTaskAnimating || run?.TryConfirmSpecialTasks() != true)
            {
                SymbiosisAudioDirector.Play(SymbiosisSound.UiCancel);
                return;
            }
            Save();
            specialTaskAnimating = true;
            RefreshSpecialTaskCards();
            SymbiosisAudioDirector.Play(SymbiosisSound.UiSelect);
            if (specialTaskAnimation != null) StopCoroutine(specialTaskAnimation);
            specialTaskAnimation = StartCoroutine(AnimateSpecialTasksToTray());
        }

        private void OpenSpecialTaskReview()
        {
            if (specialTaskAnimating || run?.IsSpecialTaskSelectionConfirmed != true)
                return;
            specialTaskReviewOpen = true;
            if (specialTaskAnimation != null) StopCoroutine(specialTaskAnimation);
            specialTaskAnimation = StartCoroutine(OpenSpecialTaskReviewRoutine());
        }

        private void CloseSpecialTaskReview()
        {
            if (specialTaskAnimating || !specialTaskReviewOpen) return;
            if (specialTaskAnimation != null) StopCoroutine(specialTaskAnimation);
            specialTaskAnimation = StartCoroutine(CloseSpecialTaskReviewRoutine());
        }

        private void RefreshSpecialTasks()
        {
            if (specialTaskSelectionRoot == null || run == null) return;
            if (specialTaskPresentationRunId != run.RunId)
            {
                // Restart reuses this UI. Presentation flags and in-flight
                // animations belong to one run, just like its three cards.
                specialTaskPresentationRunId = run.RunId;
                if (specialTaskAnimation != null)
                    StopCoroutine(specialTaskAnimation);
                specialTaskAnimation = null;
                specialTaskSelectionStarted = false;
                specialTaskReviewOpen = false;
                specialTaskAnimating = false;
                specialTaskSelectionRoot.gameObject.SetActive(false);
                specialTaskSelectionCanvas.alpha = 1f;
                specialTaskTrayButton.gameObject.SetActive(false);
                if (goalOverviewBackdrop != null)
                    goalOverviewBackdrop.gameObject.SetActive(false);
                ReleaseSpecialTaskBackdrop();
                PrepareSpecialTaskCardsAtRest();
            }
            if (!run.UsesSpecialTasks)
            {
                RefreshNeighborhoodStories(false);
                specialTaskSelectionRoot.gameObject.SetActive(false);
                specialTaskTrayButton.gameObject.SetActive(false);
                return;
            }

            specialTaskTrayLabel.text = L("GOALS", "目标牌");
            specialTaskTrayCount.text = run.SpecialTasks.Count.ToString();
            RefreshSpecialTaskCards();
            RefreshGoalOverview();

            if (!run.IsSpecialTaskSelectionConfirmed &&
                !specialTaskSelectionStarted)
            {
                specialTaskSelectionStarted = true;
                specialTaskAnimation = StartCoroutine(
                    AnimateSpecialTaskSelectionIn());
            }

            var trayVisible = run.IsSpecialTaskSelectionConfirmed &&
                !specialTaskReviewOpen && !specialTaskAnimating &&
                !specialTaskSelectionRoot.gameObject.activeSelf;
            specialTaskTrayButton.gameObject.SetActive(trayVisible);
            if (specialTaskSelectionRoot.gameObject.activeSelf)
                specialTaskSelectionRoot.SetAsLastSibling();
        }

        private void RefreshSpecialTaskCards()
        {
            var confirmed = run.IsSpecialTaskSelectionConfirmed;
            var tasks = run.SpecialTasks;
            specialTaskSelectionHeading.text = confirmed
                ? L("THESE CARDS TRAVEL WITH YOUR CITY",
                    "这三张牌，正跟着这座城")
                : L("CHOOSE THREE GOAL CARDS", "给这座城选三张目标牌");
            specialTaskSelectionSubtitle.text = confirmed
                ? run.UsesPostTutorialTaskRules && run.IsIntroPhase
                    ? L("Goal progress starts after the eight lessons.",
                        "完成前 8 张教程牌后，目标才开始计进度。")
                    : L("Chosen at the opening · completed cards stay in their slots",
                        "开局定下的三张牌 · 达成后仍留在原位")
                : L("Swap a card now if you wish; after confirmation, it stays with the city.",
                    "想换的牌现在换；确定以后，它们就跟着这座城了。");
            specialTaskSelectionNote.text = confirmed
                ? L($"Goals reached {tasks.Count(task => task.Completed)}/3 · Goal card score: {run.SpecialTaskScore}",
                    $"已达成 {tasks.Count(task => task.Completed)}/3 · 目标牌累计得分 {run.SpecialTaskScore}")
                : run.UsesPostTutorialTaskRules
                    ? L("One swap per card before confirmation. Goals start after the eight lessons.",
                        "确认前每张可换一次。完成前 8 张教程牌后，目标才开始计进度。")
                    : L("Each card may be replaced once before confirmation.",
                        "确认前，每张目标牌最多可以换一次。");
            specialTaskConfirmLabel.text = L("CONFIRM THESE 3 TASKS",
                "确定这三张牌");
            specialTaskCloseLabel.text = L("BACK TO GAME", "返回游戏");
            specialTaskConfirmButton.gameObject.SetActive(!confirmed);
            specialTaskCloseButton.gameObject.SetActive(confirmed);
            specialTaskConfirmButton.interactable = !specialTaskAnimating;
            specialTaskCloseButton.interactable = !specialTaskAnimating;

            for (var slot = 0; slot < specialTaskCards.Length; slot++)
            {
                var visible = slot < tasks.Count;
                specialTaskCards[slot].gameObject.SetActive(visible);
                if (!visible) continue;
                var task = tasks[slot];
                specialTaskArtImages[slot].sprite =
                    SpecialTaskArt(task.Kind) ?? SpecialTaskIcon(task.Kind);
                specialTaskDifficultyLabels[slot].text =
                    SpecialTaskDifficultyLabel(task.Difficulty);
                specialTaskTitles[slot].text =
                    SpecialTaskDisplayTitle(task.Kind);
                specialTaskRules[slot].text = SpecialTaskCardRule(task);
                specialTaskRules[slot].fontSize =
                    task.Kind == ConstructionSpecialTaskKind.CompleteCommunities
                        ? 11 : task.Kind ==
                            ConstructionSpecialTaskKind.RaiseAnimals ? 12 : 13;
                specialTaskEffects[slot].text = SpecialTaskEffectRule(task);
                specialTaskProgress[slot].text = task.Completed
                    ? task.Kind is ConstructionSpecialTaskKind.CommuteCycles or
                        ConstructionSpecialTaskKind.CompleteCommunities
                        ? run.IsFinished
                            ? L("END SCORE AWARDED", "终局奖励已结算")
                            : L("READY · SCORES AT END", "已达成 · 终局结算")
                        : task.EffectLimit > 0 &&
                          task.EffectPoints >= task.EffectLimit
                            ? L("EFFECT LIMIT REACHED", "后续加分已达上限")
                            : L("EFFECT ACTIVE", "已生效")
                    : $"{SpecialTaskProgressLabel(task.Kind)}  " +
                      $"{task.Progress}/{task.Target}";
                specialTaskProgress[slot].gameObject.SetActive(confirmed);
                specialTaskProgressTracks[slot].gameObject.SetActive(confirmed);
                specialTaskProgressFills[slot].gameObject.SetActive(confirmed);
                specialTaskProgressFills[slot].rectTransform.sizeDelta =
                    new Vector2(290f * Mathf.Clamp01(
                        task.Progress / (float)Mathf.Max(1, task.Target)), 7f);
                specialTaskPoints[slot].text = $"+{task.Points}";
                specialTaskPointUnitLabels[slot].text =
                    L("REWARD", "达成奖励");
                specialTaskReplaceButtons[slot].gameObject.SetActive(!confirmed);
                specialTaskReplaceButtons[slot].interactable =
                    task.CanReplace && !specialTaskAnimating;
                specialTaskReplaceLabels[slot].text = task.ReplacementUsed
                    ? L("SWAP USED", "已更换")
                    : task.CanReplace
                        ? L("SWAP THIS CARD · 1", "更换这张 · 1 次")
                        : L("LOCKED", "已锁定");
                specialTaskCardBackings[slot].color =
                    new Color(ReadingPaper.r, ReadingPaper.g,
                        ReadingPaper.b, 0.99f);
            }
        }

        private IEnumerator AnimateSpecialTaskSelectionIn()
        {
            specialTaskAnimating = true;
            while (!IsBoardCaseOpen && isActiveAndEnabled) yield return null;
            yield return CaptureSpecialTaskBackdrop();
            PrepareSpecialTaskOverlay(review: false);
            specialTaskSelectionRoot.gameObject.SetActive(true);
            specialTaskSelectionRoot.SetAsLastSibling();
            specialTaskSelectionCanvas.alpha = 0f;
            for (var slot = 0; slot < specialTaskCards.Length; slot++)
            {
                specialTaskCards[slot].anchoredPosition =
                    new Vector2(-1040f - slot * 110f, 12f);
                specialTaskCards[slot].localScale = Vector3.one * 0.24f;
            }

            const float duration = 0.92f;
            const float stagger = 0.13f;
            var elapsed = 0f;
            while (elapsed < duration + stagger * 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                specialTaskSelectionCanvas.alpha = Mathf.Clamp01(elapsed / 0.22f);
                for (var slot = 0; slot < specialTaskCards.Length; slot++)
                {
                    var progress = Mathf.Clamp01((elapsed - stagger * slot) /
                        duration);
                    var eased = BackOut(progress);
                    specialTaskCards[slot].anchoredPosition = Vector2.LerpUnclamped(
                        new Vector2(-1040f - slot * 110f, 12f),
                        SpecialTaskCardPositions[slot], eased);
                    specialTaskCards[slot].localScale = Vector3.one *
                        Mathf.LerpUnclamped(0.24f, 1f, eased);
                }
                yield return null;
            }
            PrepareSpecialTaskCardsAtRest();
            specialTaskSelectionCanvas.alpha = 1f;
            specialTaskAnimating = false;
            RefreshSpecialTasks();
        }

        private IEnumerator AnimateSpecialTasksToTray()
        {
            var startPositions = specialTaskCards
                .Select(card => card.position).ToArray();
            var startScales = specialTaskCards
                .Select(card => card.localScale).ToArray();
            var target = specialTaskTrayButton.transform.position;
            const float duration = 0.62f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var eased = progress * progress * (3f - 2f * progress);
                for (var slot = 0; slot < specialTaskCards.Length; slot++)
                {
                    specialTaskCards[slot].position = Vector3.Lerp(
                        startPositions[slot], target, eased);
                    specialTaskCards[slot].localScale = Vector3.Lerp(
                        startScales[slot], Vector3.one * 0.12f, eased);
                }
                specialTaskSelectionCanvas.alpha = 1f -
                    Mathf.Clamp01((progress - 0.58f) / 0.42f);
                yield return null;
            }
            specialTaskSelectionRoot.gameObject.SetActive(false);
            specialTaskReviewOpen = false;
            specialTaskAnimating = false;
            ReleaseSpecialTaskBackdrop();
            PrepareSpecialTaskCardsAtRest();
            RefreshSpecialTasks();
            RefreshQuickGuide();
            TryShowOpeningQuickGuide();
        }

        private IEnumerator OpenSpecialTaskReviewRoutine()
        {
            specialTaskAnimating = true;
            yield return CaptureSpecialTaskBackdrop();
            PrepareSpecialTaskOverlay(review: true);
            PrepareSpecialTaskCardsAtRest();
            for (var slot = 0; slot < specialTaskCards.Length; slot++)
                specialTaskCards[slot].localScale = Vector3.one * 0.92f;
            specialTaskSelectionCanvas.alpha = 0f;
            specialTaskSelectionRoot.gameObject.SetActive(true);
            specialTaskSelectionRoot.SetAsLastSibling();
            specialTaskTrayButton.gameObject.SetActive(false);
            var elapsed = 0f;
            const float duration = 0.25f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                specialTaskSelectionCanvas.alpha = progress;
                for (var slot = 0; slot < specialTaskCards.Length; slot++)
                    specialTaskCards[slot].localScale = Vector3.one *
                        Mathf.Lerp(0.92f, 1f, progress);
                yield return null;
            }
            PrepareSpecialTaskCardsAtRest();
            specialTaskSelectionCanvas.alpha = 1f;
            specialTaskAnimating = false;
            RefreshSpecialTasks();
        }

        private IEnumerator CloseSpecialTaskReviewRoutine()
        {
            specialTaskAnimating = true;
            var elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                specialTaskSelectionCanvas.alpha = 1f -
                    Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            specialTaskSelectionRoot.gameObject.SetActive(false);
            specialTaskReviewOpen = false;
            specialTaskAnimating = false;
            ReleaseSpecialTaskBackdrop();
            RefreshSpecialTasks();
        }

        private IEnumerator CaptureSpecialTaskBackdrop()
        {
            ReleaseSpecialTaskBackdrop();
            // Headless Test Runner has no rendered frame to capture; waiting
            // for EndOfFrame there can stall the opening animation forever.
            // The dark veil is still present and visual runs keep the blur.
            if (Application.isBatchMode) yield break;
            yield return new WaitForEndOfFrame();
            Texture2D captured = null;
            try
            {
                var captureType = Type.GetType(
                    "UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule");
                var method = captureType?.GetMethod("CaptureScreenshotAsTexture",
                    Type.EmptyTypes);
                captured = method?.Invoke(null, null) as Texture2D;
                if (captured == null) yield break;
                var width = Mathf.Max(80, captured.width / 14);
                var height = Mathf.Max(45, captured.height / 14);
                specialTaskBlurTexture = new RenderTexture(width, height, 0,
                    RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Special task soft-focus backdrop"
                };
                specialTaskBlurTexture.Create();
                Graphics.Blit(captured, specialTaskBlurTexture);
                specialTaskBlurImage.texture = specialTaskBlurTexture;
                specialTaskBlurImage.gameObject.SetActive(true);
            }
            finally
            {
                if (captured != null) Destroy(captured);
            }
        }

        private void PrepareSpecialTaskOverlay(bool review)
        {
            specialTaskReviewOpen = review;
            specialTaskConfirmButton.gameObject.SetActive(!review);
            specialTaskCloseButton.gameObject.SetActive(review);
            RefreshSpecialTaskCards();
        }

        private void PrepareSpecialTaskCardsAtRest()
        {
            for (var slot = 0; slot < specialTaskCards.Length; slot++)
            {
                specialTaskCards[slot].anchoredPosition =
                    SpecialTaskCardPositions[slot];
                specialTaskCards[slot].localScale = Vector3.one;
            }
        }

        private void ReleaseSpecialTaskBackdrop()
        {
            if (specialTaskBlurImage != null)
            {
                specialTaskBlurImage.texture = null;
                specialTaskBlurImage.gameObject.SetActive(false);
            }
            if (specialTaskBlurTexture == null) return;
            specialTaskBlurTexture.Release();
            Destroy(specialTaskBlurTexture);
            specialTaskBlurTexture = null;
        }

        private static float BackOut(float progress)
        {
            const float overshoot = 1.12f;
            var shifted = progress - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                   overshoot * shifted * shifted;
        }

        private string SpecialTaskCardRule(ConstructionSpecialTaskStatus task) =>
            task.Kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles => L(
                    $"Complete {task.Target} home-work-meal-home cycles.",
                    $"完成 {task.Target} 次上班、用餐、回家循环"),
                ConstructionSpecialTaskKind.RaiseAnimals => L(
                    run.UsesSpeciesTaskRules
                        ? $"Have {task.Target} wildlife species present at once: squirrel, pigeon, hedgehog and fox."
                        : $"Support {task.Target} wild animals at the same time.",
                    run.UsesSpeciesTaskRules
                        ? $"让 {task.Target} 种动物同时出现：松鼠、鸽子、刺猬、狐狸"
                        : $"同时养育 {task.Target} 只野生动物"),
                ConstructionSpecialTaskKind.BuildSquares => L(
                    $"Build {task.Target} plazas.",
                    $"建造 {task.Target} 个广场"),
                ConstructionSpecialTaskKind.AvoidStreets => L(
                    $"Place {task.Target} non-street plots consecutively. A street resets progress.",
                    $"连续放置 {task.Target} 格非街道地块；放街道会清零"),
                ConstructionSpecialTaskKind.ConnectGreens => L(
                    $"Connect {task.Target} green plots by shared edges.",
                    $"让 {task.Target} 块绿地沿边相连"),
                ConstructionSpecialTaskKind.CompleteCommunities => L(
                    $"In {task.Target} homes' surrounding 3×3 areas,\n" +
                    "non-green building types do not repeat.\n" +
                    "Each resident's work, meal and return stay inside.",
                    $"让 {task.Target} 户住宅达成：周围九格内\n" +
                    "非绿地建筑类型不重复；居民的\n" +
                    "上班、用餐和返家路线都在九格内"),
                _ => string.Empty
            };

        private string SpecialTaskEffectRule(ConstructionSpecialTaskStatus task)
        {
            var strength = (int)task.Difficulty + 1;
            if (task.EffectLimit > 0)
            {
                var shortRule = task.Kind switch
                {
                    ConstructionSpecialTaskKind.CommuteCycles =>
                        L($"At finish: +{1 + strength} per cycle (up to 30 cycles)",
                            $"终局：完整循环每次 +{1 + strength}，最多计 30 次"),
                    ConstructionSpecialTaskKind.RaiseAnimals =>
                        L($"Later turns: first 2 animals fed +{strength} each",
                            $"达成后，每回合前 2 只进食动物各 +{strength}"),
                    ConstructionSpecialTaskKind.BuildSquares =>
                        L($"Later turns: first 2 shared plaza meals +{strength} each",
                            $"达成后，每回合前 2 次广场共享进食各 +{strength}"),
                    ConstructionSpecialTaskKind.AvoidStreets =>
                        L($"+{1 + strength} per later non-street plot",
                            $"达成后：每格非街道 +{1 + strength}"),
                    ConstructionSpecialTaskKind.ConnectGreens =>
                        L($"+{1 + strength} per later linked green",
                            $"达成后：每块新连绿地 +{1 + strength}"),
                    ConstructionSpecialTaskKind.CompleteCommunities =>
                        L($"At finish: +{task.EffectLimit}",
                            $"终局：固定加 {task.EffectLimit} 分"),
                    _ => string.Empty
                };
                return shortRule + "\n" +
                    L($"Effect {task.EffectPoints}/{task.EffectLimit}",
                        $"后续加分 {task.EffectPoints}/{task.EffectLimit}");
            }
            return task.Kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles => L(
                    $"At finish: +{1 + strength} per completed cycle in this run (up to 30)",
                    $"终局：本局前 30 次完整生活循环各 +{1 + strength}"),
                ConstructionSpecialTaskKind.RaiseAnimals => L(
                    $"After completion: first 2 fed animals per placement +{strength} each",
                    $"达成后：每次放置结算，前 2 只进食动物各 +{strength}"),
                ConstructionSpecialTaskKind.BuildSquares => L(
                    $"After completion: first 2 shared plaza meals per placement +{strength} each",
                    $"达成后：每次放置结算，前 2 次广场共享食物各 +{strength}"),
                ConstructionSpecialTaskKind.AvoidStreets => L(
                    $"After: each later non-street build +{1 + strength}",
                    $"达成后：以后每建非街道 +{1 + strength}"),
                ConstructionSpecialTaskKind.ConnectGreens => L(
                    $"After: each later linked Green +{1 + strength}",
                    $"达成后：以后每块新连绿地 +{1 + strength}"),
                ConstructionSpecialTaskKind.CompleteCommunities => L(
                    $"At finish: a fixed +{40 * strength} for this completed goal",
                    $"终局：这张目标牌固定加 {40 * strength} 分"),
                _ => string.Empty
            };
        }

        private string SpecialTaskDisplayTitle(ConstructionSpecialTaskKind kind) =>
            kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles =>
                    L("Commuting", "通勤"),
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    run.UsesSpeciesTaskRules
                        ? L("Wildlife variety", "多种野邻居")
                        : L("More wild neighbours", "再来几位野邻居"),
                ConstructionSpecialTaskKind.BuildSquares =>
                    L("Meet at the plaza", "广场见"),
                ConstructionSpecialTaskKind.AvoidStreets =>
                    L("Hold off on streets", "先别铺路"),
                ConstructionSpecialTaskKind.ConnectGreens =>
                    L("Let the green meet", "绿地连成片"),
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    L("Life in nine plots", "九格里的小日子"),
                _ => string.Empty
            };

        private static Sprite SpecialTaskArt(ConstructionSpecialTaskKind kind) =>
            kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles =>
                    LoadSprite("UI/ConstructionSpecialTasks/commute-loop-v1"),
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    LoadSprite("UI/ConstructionSpecialTasks/raise-animals-v1"),
                ConstructionSpecialTaskKind.BuildSquares =>
                    LoadSprite("UI/ConstructionSpecialTasks/build-squares-v1"),
                ConstructionSpecialTaskKind.AvoidStreets =>
                    LoadSprite("UI/ConstructionSpecialTasks/no-road-v1"),
                ConstructionSpecialTaskKind.ConnectGreens =>
                    LoadSprite("UI/ConstructionSpecialTasks/connected-green-v1"),
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    LoadSprite("UI/ConstructionSpecialTasks/complete-communities-v1"),
                _ => null
            };

        private string SpecialTaskDifficultyLabel(
            ConstructionSpecialTaskDifficulty difficulty) => difficulty switch
        {
            ConstructionSpecialTaskDifficulty.Easy => L("EASY", "简单"),
            ConstructionSpecialTaskDifficulty.Medium => L("MEDIUM", "进阶"),
            ConstructionSpecialTaskDifficulty.Hard => L("HARD", "困难"),
            _ => string.Empty
        };

        private string SpecialTaskProgressLabel(ConstructionSpecialTaskKind kind) =>
            kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles => L("cycles", "循环"),
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    run.UsesSpeciesTaskRules
                        ? L("species", "种类") : L("animals", "动物"),
                ConstructionSpecialTaskKind.BuildSquares => L("built", "已建"),
                ConstructionSpecialTaskKind.AvoidStreets => L("in a row", "连续"),
                ConstructionSpecialTaskKind.ConnectGreens => L("connected", "相连"),
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    L("complete", "完成"),
                _ => string.Empty
            };

        private static Sprite SpecialTaskIcon(ConstructionSpecialTaskKind kind) =>
            kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles =>
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.CompletedCommute),
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    ConstructionIconCatalog.GetStatus(
                        ConstructionStatusIcon.AnimalFood),
                ConstructionSpecialTaskKind.BuildSquares =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Square),
                ConstructionSpecialTaskKind.AvoidStreets =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Street),
                ConstructionSpecialTaskKind.ConnectGreens =>
                    ConstructionIconCatalog.GetBuild(ConstructionCategory.Green),
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    ConstructionIconCatalog.GetBuild(
                        ConstructionCategory.Residence),
                _ => null
            };
    }
}
