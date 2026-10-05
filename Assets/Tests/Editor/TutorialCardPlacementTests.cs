using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class TutorialCardPlacementTests
    {
        private static readonly Rect Viewport = new Rect(-960f, -540f, 1920f, 1080f);
        private static readonly Vector2 Card = new Vector2(720f, 260f);

        [TestCase(820f, 220f, false)]
        [TestCase(-760f, -220f, true)]
        public void CardMovesToTheOpenSideOfAHorizontalTarget(float x, float y, bool right)
        {
            var target = new Vector2(x, y);
            var center = TutorialCardPlacement.Choose(Viewport, Card, target, 56f);
            Assert.That(center.x > x, Is.EqualTo(right));
            AssertInsideViewport(center, Card);
            Assert.That(DistanceToCard(target, center, Card), Is.GreaterThanOrEqualTo(80f));
        }

        [TestCase(460f, false)]
        [TestCase(-460f, true)]
        public void CardMovesAwayFromTheTimeDialOrBottomTarget(float y, bool above)
        {
            var target = new Vector2(0f, y);
            var center = TutorialCardPlacement.Choose(Viewport, Card, target, 56f);
            Assert.That(center.y > y, Is.EqualTo(above));
            AssertInsideViewport(center, Card);
            Assert.That(DistanceToCard(target, center, Card), Is.GreaterThanOrEqualTo(80f));
        }

        [Test]
        public void IntroCardIsCenteredAndVisibleWithoutAHighlight()
        {
            var center = TutorialCardPlacement.Choose(Viewport, Card, null, 0f);
            Assert.That(center.x, Is.EqualTo(0f));
            AssertInsideViewport(center, Card);
        }

        [Test]
        public void NarrowViewportFallsBackToAVisibleCorner()
        {
            var narrow = new Rect(-450f, -300f, 900f, 600f);
            var center = TutorialCardPlacement.Choose(narrow, Card, Vector2.zero, 70f);
            Assert.That(center.x, Is.Not.EqualTo(0f));
            Assert.That(center.y, Is.Not.EqualTo(0f));
            Assert.That(center.x - Card.x * 0.5f, Is.GreaterThanOrEqualTo(narrow.xMin));
            Assert.That(center.x + Card.x * 0.5f, Is.LessThanOrEqualTo(narrow.xMax));
            Assert.That(center.y - Card.y * 0.5f, Is.GreaterThanOrEqualTo(narrow.yMin));
            Assert.That(center.y + Card.y * 0.5f, Is.LessThanOrEqualTo(narrow.yMax));
        }

        private static void AssertInsideViewport(Vector2 center, Vector2 size)
        {
            Assert.That(center.x - size.x * 0.5f, Is.GreaterThanOrEqualTo(Viewport.xMin + 20f));
            Assert.That(center.x + size.x * 0.5f, Is.LessThanOrEqualTo(Viewport.xMax - 20f));
            Assert.That(center.y - size.y * 0.5f, Is.GreaterThanOrEqualTo(Viewport.yMin + 20f));
            Assert.That(center.y + size.y * 0.5f, Is.LessThanOrEqualTo(Viewport.yMax - 20f));
        }

        private static float DistanceToCard(Vector2 point, Vector2 center, Vector2 size)
        {
            var x = Mathf.Clamp(point.x, center.x - size.x * 0.5f, center.x + size.x * 0.5f);
            var y = Mathf.Clamp(point.y, center.y - size.y * 0.5f, center.y + size.y * 0.5f);
            return Vector2.Distance(point, new Vector2(x, y));
        }
    }
}
