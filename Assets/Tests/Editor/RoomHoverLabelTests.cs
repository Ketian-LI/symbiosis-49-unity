using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomHoverLabelTests
    {
        [Test]
        public void ApprovedBlankNameplateLoadsFromResources()
        {
            Assert.That(Resources.Load<Texture2D>(RoomHoverLabelCatalog.ResourcePath), Is.Not.Null);
            Assert.That(RoomHoverLabelCatalog.GetSprite(), Is.Not.Null);
        }

        [Test]
        public void RoomNamesSupportChineseAndEnglishWithoutBakedText()
        {
            var foodShop = new RoomSpec(
                "canteen-b",
                "餐饮商铺 B",
                RoomType.Canteen,
                0,
                0,
                2,
                1,
                true,
                string.Empty);
            var park = new RoomSpec(
                "central-park",
                "中央公园",
                RoomType.CentralPark,
                0,
                0,
                2,
                2,
                false,
                string.Empty);

            Assert.That(UrbanPalette.LocalizedRoomName(foodShop, true), Is.EqualTo("餐饮商铺 B"));
            Assert.That(UrbanPalette.LocalizedRoomName(foodShop, false), Is.EqualTo("Food Shop B"));
            Assert.That(UrbanPalette.LocalizedRoomName(park, false), Is.EqualTo("Central Park"));
        }
    }
}
