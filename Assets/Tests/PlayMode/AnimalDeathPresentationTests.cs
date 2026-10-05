using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class AnimalDeathPresentationTests
    {
        [UnityTest]
        public IEnumerator SpeciesTooltipsAndResultsShowTheActualCauses()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Object.Destroy(persistence); // Do not touch the player's save.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);
            var hud = generated.GetComponentInChildren<UrbanWildlifeHud>(true);
            var results = generated.GetComponentInChildren<EndRunResultsOverlay>(true);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(mortality, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(results, Is.Not.Null);

            runtime.StartNewRun(GameMode.Sandbox);
            mortality.ConfigureDeathLimit(8);
            mortality.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Starvation);
            mortality.RecordDeath(WildlifeSpecies.Squirrel, AnimalDeathCause.Traffic);
            mortality.RecordDeath(WildlifeSpecies.Hedgehog, AnimalDeathCause.Predation);

            var speciesTooltips = hud.GetComponentsInChildren<Text>(true)
                .Where(item => item.name == "Species Death Causes")
                .Select(item => item.text)
                .ToArray();
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            Assert.That(speciesTooltips.Length, Is.EqualTo(4));
            Assert.That(speciesTooltips.Any(item => item.Contains(
                $"{AnimalDeathBreakdownData.CauseName(AnimalDeathCause.Starvation, chinese)} 1")), Is.True);
            Assert.That(speciesTooltips.Any(item => item.Contains(
                $"{AnimalDeathBreakdownData.CauseName(AnimalDeathCause.Traffic, chinese)} 1")), Is.True);
            Assert.That(speciesTooltips.Any(item => item.Contains(
                $"{AnimalDeathBreakdownData.CauseName(AnimalDeathCause.Predation, chinese)} 1")), Is.True);
            var pigeonBadge = hud.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "Population Pigeon");
            Assert.That(pigeonBadge.Find("Hover Hit Area").GetComponent<Image>().raycastTarget, Is.True);
            var deathRing = hud.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "Animal Death Limit");
            Assert.That(deathRing.Find("Hover Hit Area").GetComponent<Image>().raycastTarget, Is.True);

            var report = new RunResultsData { endReason = RunEndReason.AnimalDeathLimit };
            mortality.PopulateResults(report);
            runtime.EndRun(report);
            yield return new WaitForSecondsRealtime(1.5f);

            var ecologyCard = results.GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "Ecology Summary");
            var cardRect = ecologyCard.GetComponent<RectTransform>();
            var screenPosition = RectTransformUtility.WorldToScreenPoint(bootstrap.LayoutCamera,
                cardRect.TransformPoint(cardRect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new List<RaycastResult>();
            hud.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.SameAs(ecologyCard.gameObject),
                "The death-details card must be reachable by a real UI click.");
            ecologyCard.onClick.Invoke();
            var detailPanel = results.GetComponentsInChildren<RectTransform>(true)
                .Single(item => item.name == "Animal Death Details Scrim");
            Assert.That(detailPanel.gameObject.activeSelf, Is.True);
            var detailText = detailPanel.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Death Details By Species").text;
            foreach (var species in new[] { WildlifeSpecies.Pigeon, WildlifeSpecies.Squirrel,
                         WildlifeSpecies.Hedgehog, WildlifeSpecies.Fox })
                Assert.That(detailText, Does.Contain(report.BreakdownOf(species).LocalizedLine(chinese)));
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
