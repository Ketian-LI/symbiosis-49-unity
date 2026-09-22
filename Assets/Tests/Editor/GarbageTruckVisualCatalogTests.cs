using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class GarbageTruckVisualCatalogTests
    {
        [Test]
        public void EveryGarbageTruckLayerLoadsAsAnIndependentResource()
        {
            var paths = new HashSet<string>();
            foreach (GarbageTruckVisual visual in Enum.GetValues(typeof(GarbageTruckVisual)))
            {
                var path = GarbageTruckVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(GarbageTruckVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }
    }
}
