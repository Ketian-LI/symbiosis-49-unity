using System;
using NUnit.Framework;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResearchSetupVisualCatalogTests
    {
        [Test]
        public void EveryResearchSetupVisualHasALoadableResource()
        {
            foreach (ResearchSetupVisual visual in Enum.GetValues(typeof(ResearchSetupVisual)))
            {
                var path = ResearchSetupVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty, $"Missing resource path for {visual}.");
                Assert.That(
                    ResearchSetupVisualCatalog.GetSprite(visual),
                    Is.Not.Null,
                    $"Missing research setup sprite at Resources/{path}.");
            }
        }
    }
}
