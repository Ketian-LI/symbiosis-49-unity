using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PigeonDemoTests
    {
        [Test]
        public void AnimationTime_IsQuantizedToTwelveFramesPerSecond()
        {
            Assert.That(PigeonDemoVisual.QuantizeTime(0.20f), Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(PigeonDemoVisual.QuantizeTime(0.249f), Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(PigeonDemoVisual.QuantizeTime(0.25f), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void AnimalLifecycle_UsesConfirmedDurations()
        {
            Assert.That(PigeonDemoAgent.RespawnDelaySeconds, Is.EqualTo(20f));
            Assert.That(PigeonDemoAgent.RespawnFeedbackDurationSeconds, Is.EqualTo(1f));
            Assert.That(PigeonDemoAgent.DeathMarkerLifetimeSeconds, Is.EqualTo(5f));
            Assert.That(PigeonDemoAgent.FootprintLifetimeSeconds, Is.EqualTo(6f));
        }

        [Test]
        public void RespawnFeedback_IsVisibleOnlyDuringRespawnPose()
        {
            var pigeon = new GameObject("Respawn Feedback Test");
            var material = UrbanVisualFactory.CreateSurfaceMaterial();
            try
            {
                var visual = pigeon.AddComponent<PigeonDemoVisual>();
                visual.Initialize(material, HideFlags.None);
                Assert.That(visual.IsRespawnFeedbackVisible, Is.False);

                visual.ApplyPose(PigeonDemoState.Respawning, 0.35f);
                Assert.That(visual.IsRespawnFeedbackVisible, Is.True);

                visual.ApplyPose(PigeonDemoState.Idle, 0f);
                Assert.That(visual.IsRespawnFeedbackVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(pigeon);
                Object.DestroyImmediate(material);
            }
        }
    }
}
