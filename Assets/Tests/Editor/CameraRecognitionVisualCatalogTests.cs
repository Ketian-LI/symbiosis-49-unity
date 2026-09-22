using System;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class CameraRecognitionVisualCatalogTests
    {
        [Test]
        public void EveryRecognitionVisualLoadsFromItsDedicatedResource()
        {
            foreach (CameraRecognitionVisual visual in Enum.GetValues(typeof(CameraRecognitionVisual)))
            {
                var path = CameraRecognitionVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty, $"Missing path for {visual}.");
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null,
                    $"Missing texture for {visual} at {path}.");
                Assert.That(CameraRecognitionVisualCatalog.GetSprite(visual), Is.Not.Null,
                    $"Missing sprite for {visual} at {path}.");
            }
        }
    }
}
