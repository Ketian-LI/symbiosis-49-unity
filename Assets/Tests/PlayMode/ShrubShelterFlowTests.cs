using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ShrubShelterFlowTests
    {
        [UnityTest]
        public IEnumerator ConfirmedShrubSwapShowsRecoveryAndHonestForecast()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var foraging = generated.GetComponentInChildren<HedgehogForagingController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);

            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            var shrub = rooms.Single(room => room.Spec.Id == "shrub-a");
            var plaza = rooms.Single(room => room.Spec.Id == "pigeon-d");
            var from = bootstrap.LayoutCamera.WorldToScreenPoint(shrub.VisualRoot.position);
            var to = bootstrap.LayoutCamera.WorldToScreenPoint(plaza.VisualRoot.position);
            var dragStart = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragStarted", BindingFlags.Instance | BindingFlags.NonPublic);
            var dragMove = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragging", BindingFlags.Instance | BindingFlags.NonPublic);
            var dragEnd = typeof(RoomLayoutEditorController).GetMethod(
                "HandleDragEnded", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dragStart, Is.Not.Null);
            Assert.That(dragMove, Is.Not.Null);
            Assert.That(dragEnd, Is.Not.Null);

            editor.EnterEditing();
            yield return null;
            var impactPanel = hud.transform.Find("Minimal Gameplay HUD/Layout Impact Preview");
            Assert.That(impactPanel, Is.Not.Null);
            var noChange = impactPanel.Find("No Metric Change")?.GetComponent<Text>();
            Assert.That(noChange, Is.Not.Null);
            Assert.That(noChange.gameObject.activeSelf, Is.True,
                "An unchanged layout should show one clear no-change message.");
            Assert.That(noChange.text, Does.Contain(runtime.Language == InterfaceLanguage.Chinese
                ? "无变化" : "No core outcome changed"));
            dragStart.Invoke(editor, new object[] { shrub, new Vector2(from.x, from.y) });
            dragMove.Invoke(editor, new object[] { shrub, new Vector2(to.x, to.y) });
            Assert.That(editor.TryGetImpactPreview(residents.Model, out var impact), Is.True);
            Assert.That(impact.AfterShelterPairs, Is.Zero);
            Assert.That(impact.RecoveredShelterPairs, Is.EqualTo(2));
            Assert.That(impact.MovedShrubs, Is.EqualTo(1));
            Assert.That(impactPanel.Find("Next Impact Page"), Is.Null);
            var foundRecovery = false;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            var expectedChanged = LayoutImpactFeedback.DecisionMetrics(impact,
                LayoutImpactFeedback.BuildVisualMetrics(impact, chinese))
                .Count(metric => metric.Changed);
            var activeCards = 0;
            for (var index = 1; index <= 16; index++)
            {
                var card = impactPanel.Find($"Impact Metric {index}");
                Assert.That(card, Is.Not.Null);
                if (!card.gameObject.activeSelf) continue;
                activeCards++;
                var label = card.Find("Metric Label")?.GetComponent<Text>();
                if (label == null || !label.text.Contains(chinese ? "第 3 天恢复" : "Recovers day 3")) continue;
                foundRecovery = true;
                Assert.That(card.Find("Metric Icon")?.GetComponent<LayoutImpactPictogramGraphic>()?.Kind,
                    Is.EqualTo(LayoutImpactMetricKind.ShrubRecovery));
                var delta = card.Find("Delta Value")?.GetComponent<Text>();
                Assert.That(delta?.text,
                    Is.EqualTo(chinese ? "恢复后 +2组" : "After recovery +2 pairs"));
                var explanation = card.Find("Metric Explanation/Explanation Text")?.GetComponent<Text>();
                Assert.That(explanation?.text,
                    Does.Contain(chinese ? "恢复后预计 2 组" : "2 pairs projected after recovery"));
                Canvas.ForceUpdateCanvases();
                Assert.That(delta.preferredWidth,
                    Is.LessThanOrEqualTo(delta.rectTransform.rect.width + 2f),
                    "The plain-language change must fit its row.");
                Assert.That(explanation.preferredHeight,
                    Is.LessThanOrEqualTo(explanation.rectTransform.rect.height + 2f));
            }
            Assert.That(activeCards, Is.EqualTo(expectedChanged));
            Assert.That(foundRecovery, Is.True, "Delayed shrub recovery must be visible without paging.");

            dragEnd.Invoke(editor, new object[] { shrub, new Vector2(to.x, to.y) });
            Assert.That(editor.CanConfirm, Is.True, editor.StatusText);
            editor.ConfirmEditing();
            yield return null;

            var receipt = generated.GetComponentInChildren<DailyOutcomeController>(true)
                .Model.Export();
            Assert.That(receipt.pendingCommuteForecastDay,
                Is.EqualTo(runtime.Clock.DayNumber));
            Assert.That(receipt.pendingPigeonForecastDay,
                Is.EqualTo(runtime.Clock.DayNumber + 1),
                "Animal food forecast belongs to the next day, not tonight's outcome.");

            Assert.That(foraging.Shelter.DaysUntilReady("shrub-a", runtime.Clock.DayNumber),
                Is.EqualTo(2));
            Assert.That(foraging.CurrentShelterPairs, Is.Zero);
            var statusLabel = hud.transform.Find(
                "Minimal Gameplay HUD/Hedgehog Night Report/Hedgehog Night Summary")?.GetComponent<Text>();
            Assert.That(statusLabel, Is.Not.Null);
            Assert.That(statusLabel.text,
                Does.Contain(chinese ? "恢复中 1 株" : "1 recovering"));
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
