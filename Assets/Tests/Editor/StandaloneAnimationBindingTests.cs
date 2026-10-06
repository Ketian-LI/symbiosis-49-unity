using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class StandaloneAnimationBindingTests
    {
        [Test]
        public void CitizenKeepsAnAnimatorForStandaloneSampling() =>
            AssertDriver("Citizen Blender Model v01",
                root => root.AddComponent<CitizenDemoVisual>().Initialize(null, HideFlags.None));

        [Test]
        public void PigeonKeepsAnAnimatorForStandaloneSampling() =>
            AssertDriver("Pigeon Blender Model v02",
                root => root.AddComponent<PigeonDemoVisual>().Initialize(null, HideFlags.None));

        [Test]
        public void SquirrelKeepsAnAnimatorForStandaloneSampling() =>
            AssertDriver("Squirrel Blender Model v02",
                root => root.AddComponent<SquirrelDemoVisual>().Initialize(null, HideFlags.None));

        [Test]
        public void HedgehogKeepsAnAnimatorForStandaloneSampling() =>
            AssertDriver("Hedgehog Blender Model v04",
                root => root.AddComponent<HedgehogDemoVisual>().Initialize(null, HideFlags.None));

        [Test]
        public void FoxKeepsAnAnimatorForStandaloneSampling() =>
            AssertDriver("Quaternius Fox CC0",
                root => root.AddComponent<FoxDemoVisual>().Initialize(null, HideFlags.None));

        private static void AssertDriver(string modelName, Action<GameObject> initialize)
        {
            var root = new GameObject("Standalone Animation Binding Test");
            try
            {
                initialize(root);
                var imported = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(part => part.name == modelName);
                Assert.That(imported, Is.Not.Null, modelName + " should be imported.");
                var animator = imported.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null, modelName + " needs its own Animator in a player.");
                Assert.That(animator.enabled, Is.True);
                Assert.That(animator.runtimeAnimatorController, Is.Null,
                    "A controller would override the manually sampled pose.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
