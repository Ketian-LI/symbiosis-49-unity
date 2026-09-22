using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResultsVisualCatalogTests
    {
        [Test]
        public void EveryApprovedResultsVisualLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (ResultsVisual visual in Enum.GetValues(typeof(ResultsVisual)))
            {
                var path = ResultsVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(ResultsVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }
    }
}
