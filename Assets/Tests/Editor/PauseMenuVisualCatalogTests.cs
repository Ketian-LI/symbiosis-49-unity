using System;
using NUnit.Framework;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class PauseMenuVisualCatalogTests
    {
        [Test]
        public void EveryApprovedPauseMenuVisualLoadsFromResources()
        {
            foreach (PauseMenuVisual visual in Enum.GetValues(typeof(PauseMenuVisual)))
            {
                var path = PauseMenuVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty, $"Missing resource path for {visual}.");
                Assert.That(PauseMenuVisualCatalog.GetSprite(visual), Is.Not.Null,
                    $"Missing sprite for {visual} at {path}.");
            }
        }
    }
}
