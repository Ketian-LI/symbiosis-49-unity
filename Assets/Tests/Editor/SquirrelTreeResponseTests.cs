using NUnit.Framework;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class SquirrelTreeResponseTests
    {
        [Test]
        public void ConfirmedTreeFellingPanicLastsFiveSimulationSeconds()
        {
            Assert.That(SquirrelTreeResponseController.PanicDurationSeconds, Is.EqualTo(5f));
        }
    }
}
