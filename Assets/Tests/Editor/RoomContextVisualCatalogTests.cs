using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomContextVisualCatalogTests
    {
        [Test]
        public void EveryContextLayerHasAUniqueResource()
        {
            var paths = new HashSet<string>();
            foreach (RoomContextVisual visual in Enum.GetValues(typeof(RoomContextVisual)))
            {
                var path = RoomContextVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty, $"Missing path for {visual}.");
                Assert.That(paths.Add(path), Is.True, $"Duplicate path {path}.");
            }
        }

        [Test]
        public void EveryContextLayerLoadsAsTextureAndSprite()
        {
            foreach (RoomContextVisual visual in Enum.GetValues(typeof(RoomContextVisual)))
            {
                var path = RoomContextVisualCatalog.ResourcePath(visual);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null, $"Missing texture for {visual}.");
                Assert.That(RoomContextVisualCatalog.GetSprite(visual), Is.Not.Null, $"Missing sprite for {visual}.");
            }
        }
    }
}
