using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class FoxVisualBindingTests
    {
        [Test]
        public void FoxUsesTheCc0RigAndSamplesLocomotion()
        {
            var root = new GameObject("Fox Binding Test");
            try
            {
                var visual = root.AddComponent<FoxDemoVisual>();
                visual.Initialize(null, HideFlags.None);
                var imported = root.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(part => part.name == "Quaternius Fox CC0");
                Assert.IsNotNull(imported, "The CC0 fox should be imported instead of the original fallback.");

                var renderer = imported.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsNotNull(renderer);
                Assert.That(Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z),
                    Is.EqualTo(WorldScaleStandards.FoxTargetLength).Within(0.05f));
                var leg = renderer.bones.FirstOrDefault(bone => bone.name.Contains("FrontLowerLeg"));
                Assert.IsNotNull(leg);

                visual.ApplyPose(FoxDemoState.Trot, 0f);
                var initial = leg.localRotation;
                visual.ApplyPose(FoxDemoState.Trot, 0.5f);
                Assert.That(Quaternion.Angle(initial, leg.localRotation), Is.GreaterThan(1f));
                visual.ApplyPose(FoxDemoState.Rest, 0f);
                visual.ApplyPose(FoxDemoState.Sniff, 0.5f);
                visual.ApplyPose(FoxDemoState.Alert, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
