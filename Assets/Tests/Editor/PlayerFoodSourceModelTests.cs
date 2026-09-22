using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PlayerFoodSourceModelTests
    {
        [Test]
        public void SourceStartsWithFivePortionsAndExpiresAfterThirtySimulationSeconds()
        {
            var model = new PlayerFoodSourceModel();

            Assert.That(model.TryCreate("central-park", Vector3.one, out var source), Is.True);
            Assert.That(source.portions, Is.EqualTo(5));
            Assert.That(model.Advance(29.9f), Is.Empty);
            Assert.That(model.Advance(0.2f), Is.EquivalentTo(new[] { source.id }));
            Assert.That(model.Sources, Is.Empty);
        }

        [Test]
        public void FifthClaimRemovesFoodSource()
        {
            var model = new PlayerFoodSourceModel();
            model.TryCreate("central-park", Vector3.zero, out var source);

            for (var index = 0; index < 4; index++)
            {
                Assert.That(model.TryClaimPortion(source.id), Is.True);
            }

            Assert.That(model.Sources[source.id].portions, Is.EqualTo(1));
            Assert.That(model.TryClaimPortion(source.id), Is.True);
            Assert.That(model.Sources.ContainsKey(source.id), Is.False);
        }

        [Test]
        public void NoMoreThanFiveFoodSourcesCanExist()
        {
            var model = new PlayerFoodSourceModel();
            for (var index = 0; index < 5; index++)
            {
                Assert.That(model.TryCreate("central-park", Vector3.right * index, out _), Is.True);
            }

            Assert.That(model.TryCreate("central-park", Vector3.right * 6f, out _), Is.False);
            Assert.That(model.Sources, Has.Count.EqualTo(5));
        }

        [Test]
        public void SaveRoundTripPreservesPositionPortionsAndLifetime()
        {
            var original = new PlayerFoodSourceModel();
            original.TryCreate("oak-a", new Vector3(1f, 2f, 3f), out var source);
            original.TryClaimPortion(source.id);
            original.Advance(7f);

            var restored = new PlayerFoodSourceModel();
            restored.Restore(original.Export());

            Assert.That(restored.Sources, Has.Count.EqualTo(1));
            var item = restored.Sources[source.id];
            Assert.That(item.roomId, Is.EqualTo("oak-a"));
            Assert.That(item.worldPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(item.portions, Is.EqualTo(4));
            Assert.That(item.remainingLifetime, Is.EqualTo(23f).Within(0.001f));
        }

        [Test]
        public void ExposedCacheFoodDoesNotConsumePlayerFiveSourceAllowance()
        {
            var model = new PlayerFoodSourceModel();
            for (var index = 0; index < 5; index++)
            {
                model.TryCreate("central-park", Vector3.right * index, out _);
            }

            Assert.That(model.TryCreateExposed("oak-a", Vector3.up, 3, out var exposed), Is.True);
            Assert.That(exposed.playerPlaced, Is.False);
            Assert.That(exposed.portions, Is.EqualTo(3));
            Assert.That(model.PlayerPlacedCount, Is.EqualTo(5));
            Assert.That(model.Sources, Has.Count.EqualTo(6));
        }
    }
}
