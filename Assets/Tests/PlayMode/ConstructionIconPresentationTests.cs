using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionIconPresentationTests
    {
        [UnityTest]
        public IEnumerator ConstructionScreenUsesNewIconSetWithoutReplacingWorldModels()
        {
            var root = new GameObject("Construction icon presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(new ConstructionRunModel());
            yield return null;

            Image At(string path) => root.transform.Find("Construction UI/" + path)
                ?.GetComponent<Image>();
            foreach (ConstructionCategory category in System.Enum.GetValues(
                typeof(ConstructionCategory)))
                Assert.That(At($"Building choices/Build {category}/Building icon")
                    ?.sprite, Is.SameAs(ConstructionIconCatalog.GetBuild(category)),
                    $"The {category} choice must show its own icon.");
            Assert.That(At("Building choices/Green planting/Plant Oak/Plant icon")?.sprite,
                Is.SameAs(ConstructionIconCatalog.GetBuild(
                    ConstructionCategory.Green, GreenPlanting.Oak)));
            Assert.That(At("Building choices/Story cue backing/Animal meals metric/Icon")?.sprite,
                Is.SameAs(ConstructionIconCatalog.GetStatus(
                    ConstructionStatusIcon.AnimalFood)));
            Assert.That(At("Building choices/Open build palette/Action icon")?.sprite,
                Is.SameAs(ConstructionIconCatalog.GetControl(
                    ConstructionControlIcon.Build)));
            Assert.That(At("Building choices/Advance to next day/Action icon")?.sprite,
                Is.SameAs(ConstructionIconCatalog.GetControl(
                    ConstructionControlIcon.NextDay)));
            Assert.That(At("Building choices/Advance to next day/Action icon")
                .raycastTarget, Is.False,
                "The pictogram must not intercept the button click.");
            Assert.That(root.GetComponent<ConstructionWorldView>(), Is.Not.Null);

            Object.Destroy(root);
            yield return null;
        }
    }
}
