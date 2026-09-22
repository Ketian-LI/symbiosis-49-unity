using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomSelectionVisualTests
    {
        [Test]
        public void ApprovedCornerLoadsFromResources()
        {
            var texture = Resources.Load<Texture2D>(RoomSelectionVisualCatalog.CornerResourcePath);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.GreaterThan(0));
            Assert.That(texture.height, Is.GreaterThan(0));
            Assert.That(RoomSelectionVisualCatalog.GetCornerSprite(), Is.Not.Null);
        }

        [Test]
        public void MarkerBuildsFourCornersAndTogglesAsOneSelectionState()
        {
            var room = new GameObject("Room Selection Test");

            try
            {
                var marker = room.AddComponent<RoomSelectionMarker>();
                marker.Initialize(2.4f, 4.8f, HideFlags.None);

                Assert.That(marker.CornerCount, Is.EqualTo(4));
                Assert.That(marker.IsVisible, Is.False);

                marker.SetVisible(true);
                Assert.That(marker.IsVisible, Is.True);

                marker.SetVisible(false);
                Assert.That(marker.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(room);
            }
        }
    }
}
