using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.UI
{
    public sealed class EndRunResultsOverlay : MonoBehaviour
    {
        private const int SynthSampleRate = 22050;

        private static readonly Color Graphite = new(0.08f, 0.11f, 0.16f, 0.98f);
        private static readonly Color WarmPaper = new(0.96f, 0.92f, 0.83f, 0.99f);
        private static readonly Color Ink = new(0.11f, 0.15f, 0.20f, 1f);
        private static readonly Color Amber = new(0.93f, 0.61f, 0.18f, 1f);
        private static readonly Color BrickRed = new(0.52f, 0.13f, 0.11f, 1f);
        private static readonly Color Cyan = new(0.27f, 0.78f, 0.74f, 1f);

        private Font font;
        private HideFlags generatedHideFlags;
        private GameRuntimeController runtime;
        private Camera worldCamera;
        private GameObject overlay;
        private Image backgroundDimmer;
        private RectTransform resultsCard;
        private CanvasGroup resultsCardGroup;
        private Coroutine revealRoutine;
        private bool wasVisible;
        private AudioSource resultAudioSource;
        private AudioClip animalEndClip;
        private AudioClip resourceEndClip;
        private AudioClip newRecordClip;
        private AudioClip buttonClickClip;
        private Image transitionCurtain;
        private Coroutine transitionRoutine;
        private readonly List<CanvasGroup> reasonReveal = new();
        private readonly List<CanvasGroup> daysReveal = new();
        private readonly List<CanvasGroup> summaryReveal = new();
        private readonly List<CanvasGroup> layoutReveal = new();
        private readonly List<CanvasGroup> actionReveal = new();
        private Image endReasonIcon;
        private Text title;
        private Text reason;
        private Text days;
        private Image daysIcon;
        private Text record;
        private GameObject recordBadge;
        private Text resourceValue;
        private Text residentValue;
        private Text ecologyValue;
        private Text thumbnailLabel;
        private RawImage layoutThumbnail;
        private Text restartLabel;
        private Text menuLabel;
        private Text exportLabel;
        private Button restartButton;
        private Button menuButton;
        private Button exportButton;
        private ResearchSessionController researchSession;
        private Texture2D capturedLayoutThumbnail;
        private bool capturingLayoutThumbnail;

        public bool IsVisible => overlay != null && overlay.activeSelf;

        public void Build(Font uiFont, HideFlags hideFlags, GameRuntimeController runtimeController, Camera layoutCamera)
        {
            font = uiFont;
            generatedHideFlags = hideFlags;
            runtime = runtimeController;
            worldCamera = layoutCamera;
            resultAudioSource = gameObject.AddComponent<AudioSource>();
            resultAudioSource.playOnAwake = false;
            resultAudioSource.loop = false;
            resultAudioSource.spatialBlend = 0f;
            resultAudioSource.ignoreListenerPause = true;
            resultAudioSource.volume = 0.72f;
            animalEndClip = CreateAnimalEndClip();
            resourceEndClip = CreateResourceEndClip();
            newRecordClip = CreateNewRecordClip();
            buttonClickClip = CreateButtonClickClip();

            var dimmer = CreatePanel("Results Dimmed Board", transform, new Color(0.025f, 0.04f, 0.055f, 0.58f));
            backgroundDimmer = dimmer;
            overlay = dimmer.gameObject;
            Stretch(dimmer.rectTransform);

            var card = CreatePanel("Warm Results Card", dimmer.transform, Color.clear);
            SetCenter(card.rectTransform, 700f, 930f);
            resultsCard = card.rectTransform;
            resultsCardGroup = card.gameObject.AddComponent<CanvasGroup>();
            CreateDecorativeArt(
                card.transform,
                "Results Card Artwork",
                ResultsVisualCatalog.GetSprite(ResultsVisual.MainCard),
                720f,
                906f);

            var pigeon = CreateDecorativeArt(
                card.transform,
                "Header Pigeon",
                ResultsVisualCatalog.GetSprite(ResultsVisual.HeaderPigeon),
                88f,
                82f);
            SetTopCenter(pigeon.rectTransform, 198f, -28f, 88f, 82f);

            var emblemObject = NewUiObject("End Reason Emblem", card.transform, typeof(CanvasRenderer), typeof(Image));
            endReasonIcon = emblemObject.GetComponent<Image>();
            endReasonIcon.preserveAspect = true;
            endReasonIcon.raycastTarget = false;
            SetTopCenter(endReasonIcon.rectTransform, 0f, -42f, 205f, 100f);
            title = CreateText(card.transform, "Results Title", "运行结束", 40, TextAnchor.MiddleCenter, BrickRed, FontStyle.Bold);
            SetTopCenter(title.rectTransform, 0f, 62f, 540f, 54f);
            reason = CreateText(card.transform, "End Reason", string.Empty, 22, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            SetTopCenter(reason.rectTransform, 0f, 112f, 540f, 38f);
            reasonReveal.Add(AddRevealGroup(endReasonIcon.gameObject));
            reasonReveal.Add(AddRevealGroup(title.gameObject));
            reasonReveal.Add(AddRevealGroup(reason.gameObject));

            var daysIconObject = NewUiObject("Days Survived Emblem", card.transform, typeof(CanvasRenderer), typeof(Image));
            daysIcon = daysIconObject.GetComponent<Image>();
            daysIcon.sprite = ResultsVisualCatalog.GetSprite(ResultsVisual.DaysSurvived);
            daysIcon.preserveAspect = true;
            daysIcon.raycastTarget = false;
            SetTopCenter(daysIcon.rectTransform, -128f, 156f, 108f, 108f);

            days = CreateText(card.transform, "Days Survived", string.Empty, 66, TextAnchor.MiddleCenter, Graphite, FontStyle.Bold);
            SetTopCenter(days.rectTransform, 74f, 160f, 270f, 82f);
            var badge = CreatePanel("New Record Badge", card.transform, Color.white);
            recordBadge = badge.gameObject;
            badge.sprite = ResultsVisualCatalog.GetSprite(ResultsVisual.NewRecordRibbon);
            badge.preserveAspect = false;
            SetTopCenter(badge.rectTransform, 58f, 236f, 220f, 64f);
            record = CreateText(badge.transform, "New Record Label", "新纪录", 18, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            Stretch(record.rectTransform, 16f);
            daysReveal.Add(AddRevealGroup(daysIcon.gameObject));
            daysReveal.Add(AddRevealGroup(days.gameObject));
            daysReveal.Add(AddRevealGroup(recordBadge));

            resourceValue = BuildSummaryCard(card.transform, "Resource Summary", ResultsVisual.SummaryResources, -210f);
            residentValue = BuildSummaryCard(card.transform, "Resident Summary", ResultsVisual.SummaryResidents, 0f);
            ecologyValue = BuildSummaryCard(card.transform, "Ecology Summary", ResultsVisual.SummaryEcology, 210f);

            var thumbnailFrame = CreatePanel("Final Layout Thumbnail Frame", card.transform, Color.white);
            thumbnailFrame.sprite = ResultsVisualCatalog.GetSprite(ResultsVisual.LayoutThumbnailFrame);
            thumbnailFrame.type = Image.Type.Simple;
            thumbnailFrame.preserveAspect = false;
            SetTopCenter(thumbnailFrame.rectTransform, 0f, 430f, 520f, 300f);
            var thumbnailPaper = CreatePanel("Final Layout Thumbnail Paper", thumbnailFrame.transform, new Color(0.86f, 0.83f, 0.74f, 1f));
            Stretch(thumbnailPaper.rectTransform, 28f);
            var imageObject = NewUiObject("Final Layout Thumbnail", thumbnailPaper.transform, typeof(CanvasRenderer), typeof(RawImage));
            layoutThumbnail = imageObject.GetComponent<RawImage>();
            layoutThumbnail.color = Color.white;
            layoutThumbnail.raycastTarget = false;
            Stretch(layoutThumbnail.rectTransform, 6f);
            thumbnailLabel = CreateText(thumbnailPaper.transform, "Thumbnail Placeholder", "最终布局", 24, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            Stretch(thumbnailLabel.rectTransform, 10f);
            layoutReveal.Add(AddRevealGroup(thumbnailFrame.gameObject));

            var restart = CreateButton(
                card.transform,
                "Restart",
                "重新开始",
                25,
                BeginRestartTransition,
                Amber,
                Ink,
                ResultsVisualCatalog.GetSprite(ResultsVisual.RestartButton),
                72f);
            SetTopCenter(restart.GetComponent<RectTransform>(), 0f, 746f, 390f, 72f);
            restartLabel = restart.GetComponentInChildren<Text>();
            restartButton = restart;
            actionReveal.Add(AddRevealGroup(restart.gameObject));

            var menu = CreateButton(
                card.transform,
                "Return Main Menu",
                "返回主菜单",
                20,
                BeginMenuTransition,
                Graphite,
                WarmPaper,
                ResultsVisualCatalog.GetSprite(ResultsVisual.MainMenuButton),
                54f);
            SetTopCenter(menu.GetComponent<RectTransform>(), 0f, 830f, 280f, 54f);
            menuLabel = menu.GetComponentInChildren<Text>();
            menuButton = menu;
            actionReveal.Add(AddRevealGroup(menu.gameObject));

            var export = CreateButton(
                card.transform,
                "Export Research Record",
                "导出记录",
                20,
                ExportResearchRecord,
                Cyan,
                Graphite,
                ResultsVisualCatalog.GetSprite(ResultsVisual.ExportButton),
                54f);
            SetTopCenter(export.GetComponent<RectTransform>(), 0f, 830f, 280f, 54f);
            exportLabel = export.GetComponentInChildren<Text>();
            exportButton = export;
            export.gameObject.SetActive(false);
            actionReveal.Add(AddRevealGroup(export.gameObject));

            transitionCurtain = CreatePanel("Results Transition Curtain", transform, new Color(0.025f, 0.04f, 0.055f, 0f));
            Stretch(transitionCurtain.rectTransform);
            transitionCurtain.gameObject.SetActive(false);

            runtime.StateChanged += Refresh;
            Refresh();
        }

        public void BindResearchSession(ResearchSessionController controller)
        {
            researchSession = controller;
        }

        public void SetFinalLayoutThumbnail(Texture texture)
        {
            layoutThumbnail.texture = texture;
            layoutThumbnail.gameObject.SetActive(texture != null);
            thumbnailLabel.gameObject.SetActive(texture == null);
        }

        private void OnDestroy()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (runtime != null)
            {
                runtime.StateChanged -= Refresh;
            }

            ReleaseCapturedLayoutThumbnail();
        }

        private void BeginRestartTransition()
        {
            BeginExitTransition(true);
        }

        private void BeginMenuTransition()
        {
            BeginExitTransition(false);
        }

        private void ExportResearchRecord()
        {
            var folder = researchSession?.ExportResearchRecord();
            if (!string.IsNullOrEmpty(folder) && exportLabel != null)
            {
                exportLabel.text = runtime.Language == InterfaceLanguage.Chinese ? "已导出" : "Exported";
            }
        }

        private void BeginExitTransition(bool restart)
        {
            if (transitionRoutine != null)
            {
                return;
            }

            transitionRoutine = StartCoroutine(PlayExitTransition(restart));
        }

        private IEnumerator PlayExitTransition(bool restart)
        {
            resultsCardGroup.interactable = false;
            resultsCardGroup.blocksRaycasts = false;
            transitionCurtain.transform.SetAsLastSibling();
            transitionCurtain.gameObject.SetActive(true);
            yield return FadeCurtain(0f, 1f, 0.25f);

            if (restart)
            {
                runtime.RestartRun();
            }
            else
            {
                runtime.ReturnToMainMenuFromResults();
            }

            yield return null;
            yield return FadeCurtain(1f, 0f, 0.25f);
            transitionCurtain.gameObject.SetActive(false);
            transitionRoutine = null;
        }

        private IEnumerator FadeCurtain(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                var color = transitionCurtain.color;
                color.a = Mathf.LerpUnclamped(from, to, t);
                transitionCurtain.color = color;
                yield return null;
            }

            var finalColor = transitionCurtain.color;
            finalColor.a = to;
            transitionCurtain.color = finalColor;
        }

        private Text BuildSummaryCard(Transform parent, string name, ResultsVisual visual, float x)
        {
            var panel = CreatePanel(name, parent, Color.white);
            panel.sprite = ResultsVisualCatalog.GetSprite(visual);
            panel.type = Image.Type.Simple;
            panel.preserveAspect = false;
            SetTopCenter(panel.rectTransform, x, 306f, 194f, 108f);
            summaryReveal.Add(AddRevealGroup(panel.gameObject));
            var value = CreateText(panel.transform, "Live Value", "—", 15, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            SetTopCenter(value.rectTransform, 0f, 64f, 174f, 36f);
            return value;
        }

        private void Refresh()
        {
            if (runtime == null || overlay == null)
            {
                return;
            }

            var shouldShow = runtime.ResultsOpen && runtime.CurrentResults != null;
            if (!shouldShow)
            {
                if (revealRoutine != null)
                {
                    StopCoroutine(revealRoutine);
                    revealRoutine = null;
                }

                overlay.SetActive(false);
                wasVisible = false;
                return;
            }

            var isEntering = !wasVisible;
            if (isEntering)
            {
                CaptureCurrentLayoutThumbnail();
            }

            overlay.SetActive(true);

            var data = runtime.CurrentResults;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var research = runtime.Mode == GameMode.Research;
            title.text = research
                ? (chinese ? "研究记录" : "Research Record")
                : (chinese ? "运行结束" : "Run Ended");
            reason.text = data.LocalizedEndReason(chinese);
            endReasonIcon.sprite = ResultsVisualCatalog.GetSprite(
                data.endReason == RunEndReason.AnimalDeathLimit
                    ? ResultsVisual.EndAnimalDeaths
                    : ResultsVisual.EndNegativeResources);
            endReasonIcon.enabled = endReasonIcon.sprite != null;
            days.text = research
                ? $"{Mathf.FloorToInt(data.researchElapsedSeconds / 60f):00}:{Mathf.FloorToInt(data.researchElapsedSeconds % 60f):00}"
                : chinese ? $"{Mathf.Max(1, data.daysSurvived)} 天" : $"{Mathf.Max(1, data.daysSurvived)} days";
            record.text = chinese ? "新纪录" : "New record";
            recordBadge.SetActive(data.isNewRecord);

            resourceValue.text = chinese
                ? $"资源 {data.finalResourceBalance}\n收 {data.cumulativeResourceIncome} · 支 {data.cumulativeResourceSpending}"
                : $"Balance {data.finalResourceBalance}\nIn {data.cumulativeResourceIncome} · Out {data.cumulativeResourceSpending}";
            residentValue.text = chinese
                ? $"居民 {data.finalResidents}\n到达 {data.arrivals} · 离开 {data.departures}"
                : $"Residents {data.finalResidents}\nArrived {data.arrivals} · Left {data.departures}";
            ecologyValue.text = chinese
                ? $"死亡 {data.TotalAnimalDeaths}\n植树 {data.treesPlanted} · 砍伐 {data.treesFelled}"
                : $"Deaths {data.TotalAnimalDeaths}\nPlanted {data.treesPlanted} · Felled {data.treesFelled}";
            thumbnailLabel.text = chinese ? "最终布局" : "Final layout";
            restartLabel.text = chinese ? "重新开始" : "Restart";
            menuLabel.text = chinese ? "返回主菜单" : "Main Menu";
            if (exportButton != null)
            {
                exportButton.gameObject.SetActive(research);
                exportLabel.text = chinese ? "导出研究记录" : "Export Research Record";
                SetTopCenter(restartButton.GetComponent<RectTransform>(), research ? 150f : 0f, 746f, research ? 270f : 390f, 72f);
                SetTopCenter(menuButton.GetComponent<RectTransform>(), research ? -150f : 0f, research ? 746f : 830f, research ? 270f : 280f, research ? 72f : 54f);
                SetTopCenter(exportButton.GetComponent<RectTransform>(), 0f, 830f, 280f, 54f);
            }

            if (isEntering)
            {
                if (Application.isPlaying)
                {
                    revealRoutine = StartCoroutine(PlayReveal());
                }
                else
                {
                    ShowImmediatelyForEditorPreview();
                }
            }

            wasVisible = true;
        }

        private void ShowImmediatelyForEditorPreview()
        {
            resultsCardGroup.alpha = 1f;
            resultsCardGroup.interactable = true;
            resultsCardGroup.blocksRaycasts = true;
            resultsCard.localScale = Vector3.one;
            SetRevealAlpha(reasonReveal, 1f);
            SetRevealAlpha(daysReveal, 1f);
            SetRevealAlpha(summaryReveal, 1f);
            SetRevealAlpha(layoutReveal, 1f);
            SetRevealAlpha(actionReveal, 1f);
        }

        private void CaptureCurrentLayoutThumbnail()
        {
            if (capturingLayoutThumbnail || worldCamera == null || layoutThumbnail == null)
            {
                return;
            }

            const int width = 720;
            const int height = 460;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "Results Layout Snapshot"
            };
            var canvas = GetComponent<Canvas>();
            var canvasWasEnabled = canvas != null && canvas.enabled;
            var previousTarget = worldCamera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = worldCamera.aspect;

            try
            {
                capturingLayoutThumbnail = true;
                target.Create();
                if (canvasWasEnabled)
                {
                    canvas.enabled = false;
                }

                worldCamera.targetTexture = target;
                worldCamera.aspect = width / (float)height;
                worldCamera.Render();
                RenderTexture.active = target;

                var snapshot = new Texture2D(width, height, TextureFormat.RGB24, false)
                {
                    name = "Final Layout Snapshot"
                };
                snapshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                snapshot.Apply(false, false);

                ReleaseCapturedLayoutThumbnail();
                capturedLayoutThumbnail = snapshot;
                SetFinalLayoutThumbnail(capturedLayoutThumbnail);
            }
            finally
            {
                worldCamera.targetTexture = previousTarget;
                worldCamera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                if (canvas != null)
                {
                    canvas.enabled = canvasWasEnabled;
                }

                capturingLayoutThumbnail = false;
                target.Release();
                DestroyRuntimeObject(target);
            }
        }

        private void ReleaseCapturedLayoutThumbnail()
        {
            if (capturedLayoutThumbnail == null)
            {
                return;
            }

            if (layoutThumbnail != null && layoutThumbnail.texture == capturedLayoutThumbnail)
            {
                layoutThumbnail.texture = null;
            }

            DestroyRuntimeObject(capturedLayoutThumbnail);
            capturedLayoutThumbnail = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private IEnumerator PlayReveal()
        {
            var targetDimmerColor = backgroundDimmer.color;
            backgroundDimmer.color = new Color(
                targetDimmerColor.r,
                targetDimmerColor.g,
                targetDimmerColor.b,
                0f);
            resultsCardGroup.alpha = 0f;
            resultsCardGroup.interactable = false;
            resultsCardGroup.blocksRaycasts = false;
            resultsCard.localScale = Vector3.one * 0.92f;
            SetRevealAlpha(reasonReveal, 0f);
            SetRevealAlpha(daysReveal, 0f);
            SetRevealAlpha(summaryReveal, 0f);
            SetRevealAlpha(layoutReveal, 0f);
            SetRevealAlpha(actionReveal, 0f);

            const float openingDuration = 0.35f;
            var elapsed = 0f;
            while (elapsed < openingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / openingDuration);
                var eased = EaseOutCubic(t);
                backgroundDimmer.color = new Color(
                    targetDimmerColor.r,
                    targetDimmerColor.g,
                    targetDimmerColor.b,
                    targetDimmerColor.a * eased);
                resultsCardGroup.alpha = eased;
                resultsCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, eased);
                yield return null;
            }

            backgroundDimmer.color = targetDimmerColor;
            resultsCardGroup.alpha = 1f;
            resultsCard.localScale = Vector3.one;
            PlayEndingSound();
            yield return FadeReveal(reasonReveal, 0.10f);
            yield return FadeReveal(daysReveal, 0.10f);
            yield return FadeReveal(summaryReveal, 0.15f);
            if (runtime.CurrentResults != null && runtime.CurrentResults.isNewRecord)
            {
                resultAudioSource.PlayOneShot(newRecordClip, 0.62f);
            }

            yield return FadeReveal(layoutReveal, 0.12f);
            yield return FadeReveal(actionReveal, 0.12f);
            resultsCardGroup.interactable = true;
            resultsCardGroup.blocksRaycasts = true;
            revealRoutine = null;
        }

        private void PlayEndingSound()
        {
            if (runtime.CurrentResults == null || resultAudioSource == null)
            {
                return;
            }

            var clip = runtime.CurrentResults.endReason == RunEndReason.AnimalDeathLimit
                ? animalEndClip
                : resourceEndClip;
            resultAudioSource.PlayOneShot(clip);
        }

        private static AudioClip CreateAnimalEndClip()
        {
            const float duration = 0.58f;
            var samples = Mathf.CeilToInt(duration * SynthSampleRate);
            var data = new float[samples];
            var noise = new System.Random(4901);
            for (var index = 0; index < samples; index++)
            {
                var time = index / (float)SynthSampleRate;
                var thudEnvelope = Mathf.Exp(-9f * time);
                var thud = Mathf.Sin(2f * Mathf.PI * (105f - 48f * time) * time) * thudEnvelope * 0.34f;
                var flutterTime = Mathf.Max(0f, time - 0.16f);
                var flutterEnvelope = flutterTime > 0f ? Mathf.Exp(-5.5f * flutterTime) : 0f;
                var flutterPulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 13f * flutterTime);
                var rustle = ((float)noise.NextDouble() * 2f - 1f) * flutterEnvelope * flutterPulse * 0.08f;
                data[index] = Mathf.Clamp(thud + rustle, -0.45f, 0.45f);
            }

            return CreateSynthClip("Results Animal Ending", data);
        }

        private static AudioClip CreateResourceEndClip()
        {
            const float duration = 0.52f;
            var samples = Mathf.CeilToInt(duration * SynthSampleRate);
            var data = new float[samples];
            var noise = new System.Random(1949);
            for (var index = 0; index < samples; index++)
            {
                var time = index / (float)SynthSampleRate;
                var first = CreateTokenTap(time, 0.04f, 620f);
                var second = CreateTokenTap(time, 0.23f, 430f);
                var texture = ((float)noise.NextDouble() * 2f - 1f) * (Mathf.Abs(first) + Mathf.Abs(second)) * 0.07f;
                data[index] = Mathf.Clamp(first + second + texture, -0.48f, 0.48f);
            }

            return CreateSynthClip("Results Resource Ending", data);
        }

        private static AudioClip CreateNewRecordClip()
        {
            const float duration = 0.64f;
            var samples = Mathf.CeilToInt(duration * SynthSampleRate);
            var data = new float[samples];
            for (var index = 0; index < samples; index++)
            {
                var time = index / (float)SynthSampleRate;
                var envelope = Mathf.Min(1f, time * 45f) * Mathf.Exp(-4.8f * time);
                var first = Mathf.Sin(2f * Mathf.PI * 523.25f * time);
                var secondTime = Mathf.Max(0f, time - 0.11f);
                var secondEnvelope = time >= 0.11f ? Mathf.Exp(-5.2f * secondTime) : 0f;
                var second = Mathf.Sin(2f * Mathf.PI * 659.25f * secondTime) * secondEnvelope;
                data[index] = Mathf.Clamp((first * envelope + second) * 0.17f, -0.42f, 0.42f);
            }

            return CreateSynthClip("Results New Record", data);
        }

        private static AudioClip CreateButtonClickClip()
        {
            const float duration = 0.11f;
            var samples = Mathf.CeilToInt(duration * SynthSampleRate);
            var data = new float[samples];
            var noise = new System.Random(491);
            for (var index = 0; index < samples; index++)
            {
                var time = index / (float)SynthSampleRate;
                var envelope = Mathf.Exp(-37f * time);
                var body = Mathf.Sin(2f * Mathf.PI * 310f * time) * envelope * 0.27f;
                var wood = ((float)noise.NextDouble() * 2f - 1f) * envelope * 0.11f;
                data[index] = Mathf.Clamp(body + wood, -0.38f, 0.38f);
            }

            return CreateSynthClip("Results Button Click", data);
        }

        private static float CreateTokenTap(float time, float start, float frequency)
        {
            var localTime = time - start;
            if (localTime < 0f || localTime > 0.17f)
            {
                return 0f;
            }

            var envelope = Mathf.Exp(-29f * localTime);
            return Mathf.Sin(2f * Mathf.PI * frequency * localTime) * envelope * 0.39f;
        }

        private static AudioClip CreateSynthClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SynthSampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static IEnumerator FadeReveal(IReadOnlyList<CanvasGroup> groups, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetRevealAlpha(groups, EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetRevealAlpha(groups, 1f);
        }

        private static void SetRevealAlpha(IReadOnlyList<CanvasGroup> groups, float alpha)
        {
            for (var index = 0; index < groups.Count; index++)
            {
                groups[index].alpha = alpha;
            }
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private Button CreateButton(
            Transform parent,
            string name,
            string label,
            int fontSize,
            UnityEngine.Events.UnityAction action,
            Color background,
            Color foreground,
            Sprite backgroundSprite = null,
            float backgroundArtHeight = 0f)
        {
            var panel = CreatePanel(name, parent, backgroundSprite == null ? background : Color.clear);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.onClick.AddListener(action);

            var visualObject = NewUiObject("Visuals", panel.transform);
            var visualRect = visualObject.GetComponent<RectTransform>();
            Stretch(visualRect);
            Image art = null;

            if (backgroundSprite != null)
            {
                var artObject = NewUiObject("Background Art", visualObject.transform, typeof(CanvasRenderer), typeof(Image));
                art = artObject.GetComponent<Image>();
                art.sprite = backgroundSprite;
                art.color = Color.white;
                art.preserveAspect = false;
                art.raycastTarget = false;
                var artRect = art.rectTransform;
                artRect.anchorMin = new Vector2(0f, 0.5f);
                artRect.anchorMax = new Vector2(1f, 0.5f);
                artRect.pivot = new Vector2(0.5f, 0.5f);
                artRect.anchoredPosition = Vector2.zero;
                artRect.sizeDelta = new Vector2(0f, backgroundArtHeight);
            }

            var text = CreateText(visualObject.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, foreground, FontStyle.Bold);
            Stretch(text.rectTransform, 5f);

            if (art != null)
            {
                var feedback = panel.gameObject.AddComponent<ResultsButtonFeedback>();
                feedback.Initialize(visualRect, art, resultAudioSource, buttonClickClip);
                button.onClick.AddListener(feedback.PlayClick);
            }

            return button;
        }

        private Image CreatePanel(string name, Transform parent, Color color)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Image));
            var image = instance.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private Image CreateDecorativeArt(Transform parent, string name, Sprite sprite, float width, float height)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Image));
            var image = instance.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            return image;
        }

        private static CanvasGroup AddRevealGroup(GameObject target)
        {
            return target.AddComponent<CanvasGroup>();
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color, FontStyle style)
        {
            var instance = NewUiObject(name, parent, typeof(CanvasRenderer), typeof(Text));
            var text = instance.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private GameObject NewUiObject(string name, Transform parent, params System.Type[] components)
        {
            var instance = new GameObject(name, typeof(RectTransform))
            {
                hideFlags = generatedHideFlags
            };
            instance.transform.SetParent(parent, false);
            foreach (var component in components)
            {
                instance.AddComponent(component);
            }
            return instance;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static void SetCenter(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopCenter(RectTransform rect, float x, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
