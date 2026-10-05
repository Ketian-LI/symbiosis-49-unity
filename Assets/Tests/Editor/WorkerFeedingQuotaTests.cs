using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WorkerFeedingQuotaTests
    {
        [Test]
        public void EachWorkerCanFeedOnlyOncePerDay()
        {
            var quota = new WorkerFeedingQuota();

            Assert.That(quota.TryRecord(1, "R001"), Is.True);
            Assert.That(quota.TryRecord(1, "R001"), Is.False);
            Assert.That(quota.TryRecord(1, "R002"), Is.True);
            Assert.That(quota.TryRecord(2, "R001"), Is.True);
            Assert.That(quota.CanFeed(2, "R002"), Is.True);
        }

        [Test]
        public void RestoredQuotaKeepsAlreadyFedResidents()
        {
            var quota = new WorkerFeedingQuota();
            quota.Restore(3, new[] { "R001" });

            Assert.That(quota.CanFeed(3, "R001"), Is.False);
            Assert.That(quota.CanFeed(3, "R002"), Is.True);
            Assert.That(quota.CanFeed(4, "R001"), Is.True);
        }

        [Test]
        public void PassByDistanceChecksEntireMovementSegment()
        {
            var distance = WorkerFeedingQuota.DistanceSquaredToSegmentXZ(
                new Vector3(1f, 0f, 0.5f),
                Vector3.zero,
                new Vector3(2f, 0f, 0f),
                out var nearest);

            Assert.That(distance, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(nearest.x, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
