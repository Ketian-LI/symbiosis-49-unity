using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResourcePointVisualCatalogTests
    {
        [Test]
        public void EveryResourcePointStateLoadsAsAnIndependentResource()
        {
            var paths = new HashSet<string>();
            foreach (ResourcePointVisual visual in Enum.GetValues(typeof(ResourcePointVisual)))
            {
                var path = ResourcePointVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(ResourcePointVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }
    }
}
