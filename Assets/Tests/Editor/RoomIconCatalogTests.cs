using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomIconCatalogTests
    {
        [Test]
        public void EveryRoomTypeHasOneUniqueIconResource()
        {
            var paths = new HashSet<string>();

            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                var path = RoomIconCatalog.ResourcePath(type);
                Assert.That(path, Is.Not.Empty, $"Missing room-icon mapping for {type}.");
                Assert.That(paths.Add(path), Is.True, $"Room icon path is reused: {path}.");
            }
        }

        [Test]
        public void EveryMappedRoomIconLoadsAsATexture()
        {
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                var texture = Resources.Load<Texture2D>(RoomIconCatalog.ResourcePath(type));
                Assert.That(texture, Is.Not.Null, $"Room icon texture failed to load for {type}.");
                Assert.That(texture.width, Is.GreaterThan(0));
                Assert.That(texture.height, Is.GreaterThan(0));

                var sprite = RoomIconCatalog.GetSprite(type);
                Assert.That(sprite, Is.Not.Null, $"Room icon sprite failed to build for {type}.");
                Assert.That(sprite.texture, Is.SameAs(texture));
            }
        }
    }
}
