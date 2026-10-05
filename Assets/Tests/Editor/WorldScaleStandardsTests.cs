using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WorldScaleStandardsTests
    {
        [Test]
        public void Citizen_IsApproximatelyRealWorldHeight()
        {
            Assert.That(WorldScaleStandards.CitizenVisualHeight, Is.InRange(1.65f, 1.80f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.CitizenVisualHeight), Is.InRange(0.53f, 0.58f));
        }

        [Test]
        public void Pigeon_IsReadableButSmallerThanOneMapCell()
        {
            Assert.That(WorldScaleStandards.PigeonVisualLength, Is.EqualTo(0.66f).Within(0.001f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.PigeonVisualLength), Is.InRange(0.20f, 0.22f));
        }

        [Test]
        public void OtherAnimals_AreReadableWithoutFillingTheirRooms()
        {
            Assert.That(WorldScaleStandards.SquirrelVisualLength, Is.EqualTo(0.60f).Within(0.001f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.SquirrelVisualLength), Is.InRange(0.18f, 0.20f));
            Assert.That(WorldScaleStandards.HedgehogVisualLength, Is.EqualTo(0.48f).Within(0.001f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.HedgehogVisualLength), Is.InRange(0.15f, 0.16f));
            Assert.That(WorldScaleStandards.FoxVisualLength, Is.EqualTo(1.20f).Within(0.001f));
            Assert.That(WorldScaleStandards.FractionOfCell(WorldScaleStandards.FoxVisualLength), Is.LessThan(0.40f));
        }

        [Test]
        public void MainSceneKeepsAgentsAtUnitScale_WithReadableIndependentClickColliders()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);

                AssertVisualOnlyScale(generated, "Pigeon 01",
                    "Replaceable Blender Pigeon Visual/Blender To Unity Axis",
                    WorldScaleStandards.PigeonModelScale, new Vector3(0.52f, 0.42f, 0.55f));
                AssertVisualOnlyScale(generated, "Squirrel 01",
                    "Replaceable Squirrel Visual/Blender To Unity Axis",
                    WorldScaleStandards.SquirrelModelScale, new Vector3(0.56f, 0.48f, 0.58f));
                AssertVisualOnlyScale(generated, "Hedgehog 01",
                    "Replaceable Hedgehog Visual/Blender To Unity Axis",
                    WorldScaleStandards.HedgehogModelScale, new Vector3(0.48f, 0.33f, 0.50f));
                AssertFoxVisualScale(generated);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertVisualOnlyScale(Transform generated, string agentName,
            string visualPath, float expectedScale, Vector3 expectedColliderSize)
        {
            var agent = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == agentName);
            var visualAxis = agent.Find(visualPath);
            Assert.That(visualAxis, Is.Not.Null, agentName);
            Assert.That(visualAxis.localScale, Is.EqualTo(Vector3.one * expectedScale), agentName);
            Assert.That(agent.localScale, Is.EqualTo(Vector3.one), agentName);
            Assert.That(agent.GetComponent<BoxCollider>().size,
                Is.EqualTo(expectedColliderSize), agentName);
        }

        private static void AssertFoxVisualScale(Transform generated)
        {
            var agent = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Fox 01");
            var visualAxis = agent.Find("Replaceable Fox Visual/Quaternius Fox Axis");
            Assert.That(visualAxis, Is.Not.Null);
            Assert.That(visualAxis.localScale.x, Is.EqualTo(visualAxis.localScale.y).Within(0.0001f));
            Assert.That(visualAxis.localScale.y, Is.EqualTo(visualAxis.localScale.z).Within(0.0001f));
            var renderer = visualAxis.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z),
                Is.EqualTo(WorldScaleStandards.FoxTargetLength).Within(0.05f));
            Assert.That(agent.localScale, Is.EqualTo(Vector3.one));
            Assert.That(agent.GetComponent<BoxCollider>().size,
                Is.EqualTo(new Vector3(0.92f, 0.62f, 0.42f)));
        }
    }
}
