using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class MainMenuVisualCatalogTests
    {
        [Test]
        public void EveryApprovedMainMenuVisualLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (MainMenuVisual visual in Enum.GetValues(typeof(MainMenuVisual)))
            {
                var path = MainMenuVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(MainMenuVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }
    }
}
