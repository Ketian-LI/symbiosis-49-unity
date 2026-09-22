using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class DeathFeedbackVisualTests
    {
        [Test]
        public void EveryDeathFeedbackLayerLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (DeathFeedbackVisual visual in Enum.GetValues(typeof(DeathFeedbackVisual)))
            {
                var path = DeathFeedbackVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(DeathFeedbackVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }

        [Test]
        public void DeathMarkerFadesWhileItsSingleRippleExpands()
        {
            Assert.That(PigeonDeathMarkerEffect.FootprintAlphaAt(0f), Is.EqualTo(1f));
            Assert.That(PigeonDeathMarkerEffect.FootprintAlphaAt(0.5f), Is.LessThan(1f));
            Assert.That(PigeonDeathMarkerEffect.FootprintAlphaAt(1f), Is.EqualTo(0f));
            Assert.That(PigeonDeathMarkerEffect.RippleAlphaAt(1f), Is.EqualTo(0f));
            Assert.That(PigeonDeathMarkerEffect.RippleScaleAt(1f),
                Is.GreaterThan(PigeonDeathMarkerEffect.RippleScaleAt(0f)));
        }
    }
}
