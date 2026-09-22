using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class RoomLayoutDataTests
    {
        [Test]
        public void LayoutHasNoValidationErrors()
        {
            Assert.That(RoomLayoutData.Validate(), Is.Empty);
        }

        [Test]
        public void LayoutContainsThirtyFiveRoomsAndFortyNineCells()
        {
            Assert.That(RoomLayoutData.All.Count, Is.EqualTo(35));
            Assert.That(RoomLayoutData.All.Sum(room => room.CellCount), Is.EqualTo(49));
        }

        [Test]
        public void LayoutUsesExpectedFootprints()
        {
            Assert.That(RoomLayoutData.All.Count(room => room.Width == 1 && room.Height == 1), Is.EqualTo(23));
            Assert.That(RoomLayoutData.All.Count(room => room.Width * room.Height == 2), Is.EqualTo(11));
            Assert.That(RoomLayoutData.All.Count(room => room.Width == 2 && room.Height == 2), Is.EqualTo(1));
        }

        [TestCase(RoomType.Residence, 8)]
        [TestCase(RoomType.Office, 4)]
        [TestCase(RoomType.Canteen, 2)]
        [TestCase(RoomType.Garage, 3)]
        [TestCase(RoomType.Trash, 4)]
        [TestCase(RoomType.PigeonHabitat, 4)]
        [TestCase(RoomType.OakHabitat, 4)]
        [TestCase(RoomType.ShrubHabitat, 3)]
        public void LayoutContainsExpectedRoomTypeCounts(RoomType roomType, int expectedCount)
        {
            Assert.That(RoomLayoutData.All.Count(room => room.Type == roomType), Is.EqualTo(expectedCount));
        }

        [Test]
        public void CentralParkIsFixedAtTheExpectedLocation()
        {
            var park = RoomLayoutData.All.Single(room => room.Type == RoomType.CentralPark);
            Assert.That(park.Column, Is.EqualTo(2));
            Assert.That(park.Row, Is.EqualTo(2));
            Assert.That(park.Width, Is.EqualTo(2));
            Assert.That(park.Height, Is.EqualTo(2));
            Assert.That(park.Movable, Is.False);
        }

        [Test]
        public void OfficeDReplacesTheMaintenanceRoom()
        {
            var officeD = RoomLayoutData.All.Single(room => room.Id == "office-d");
            Assert.That(officeD.Type, Is.EqualTo(RoomType.Office));
            Assert.That(officeD.Column, Is.EqualTo(3));
            Assert.That(officeD.Row, Is.EqualTo(6));
            Assert.That(officeD.Width, Is.EqualTo(1));
            Assert.That(officeD.Height, Is.EqualTo(1));
        }
    }
}
