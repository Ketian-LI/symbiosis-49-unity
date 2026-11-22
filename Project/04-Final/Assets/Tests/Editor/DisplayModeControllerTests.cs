using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class DisplayModeControllerTests
    {
        [Test]
        public void MaximizedWindowIsNotReportedAsBorderlessFullscreen()
        {
            var classify = typeof(DisplayModeController).GetMethod(
                "IsFullscreenMode", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(classify, Is.Not.Null);

            Assert.That(classify.Invoke(null, new object[] { FullScreenMode.Windowed }),
                Is.EqualTo(false));
            Assert.That(classify.Invoke(null, new object[] { FullScreenMode.MaximizedWindow }),
                Is.EqualTo(false));
            Assert.That(classify.Invoke(null, new object[] { FullScreenMode.FullScreenWindow }),
                Is.EqualTo(true));
            Assert.That(classify.Invoke(null, new object[] { FullScreenMode.ExclusiveFullScreen }),
                Is.EqualTo(true));
        }
    }
}
