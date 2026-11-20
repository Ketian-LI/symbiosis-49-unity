using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.CameraRecognition;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;
using Object = UnityEngine.Object;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionRandomBoardGameFlowTests
    {
        private GameObject root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            root = null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        [UnityTest]
        public IEnumerator RandomDrawResidenceCanBeRecognisedAndBuiltOnDayTwo()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            root = new GameObject("Random board game camera test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            var bridge = root.AddComponent<ConstructionCameraUdpBridge>();
            yield return null;

            const string starter = "{\"schemaVersion\":1,\"sequence\":1," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";
            const string drawnHome = "{\"schemaVersion\":1,\"sequence\":2," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}," +
                "{\"markerId\":13,\"column\":4,\"row\":3," +
                "\"category\":\"Residence\",\"planting\":\"None\"}]}";

            using (var sender = new UdpClient())
            {
                for (var frame = 0; frame < 55; frame++)
                {
                    var bytes = Encoding.UTF8.GetBytes(starter);
                    sender.Send(bytes, bytes.Length,
                        new IPEndPoint(IPAddress.Loopback,
                            ConstructionCameraUdpBridge.ListenPort));
                    yield return new WaitForSecondsRealtime(0.04f);
                }
                Assert.That(bridge.CurrentStatus, Does.Contain("Board synced"));

                for (var frame = 0; frame < 55; frame++)
                {
                    var bytes = Encoding.UTF8.GetBytes(drawnHome);
                    sender.Send(bytes, bytes.Length,
                        new IPEndPoint(IPAddress.Loopback,
                            ConstructionCameraUdpBridge.ListenPort));
                    yield return new WaitForSecondsRealtime(0.04f);
                }
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);
                Assert.That(controller.ConfirmPendingPlacement(), Is.True);
                Assert.That(run.At(4, 3).category,
                    Is.EqualTo(ConstructionCategory.Residence));
            }
        }

        [UnityTest]
        public IEnumerator IllegalLocationKeepsOriginalDraw_ButNoLegalTypeCanRedraw()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            root = new GameObject("Random draw lock camera test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            var bridge = root.AddComponent<ConstructionCameraUdpBridge>();
            yield return null;

            const string starter = "{\"schemaVersion\":1,\"sequence\":1," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";
            using (var sender = new UdpClient())
            {
                yield return SendFrames(sender, starter);
                Assert.That(bridge.CurrentStatus, Does.Contain("Board synced"));

                // A workshop has no legal destination beside only the oak.
                yield return SendUntilStatus(sender,
                    WithTile(16, 4, 3, "Workshop"), bridge, "No legal plot");
                yield return SendUntilStatus(sender, starter, bridge,
                    "Board restored");

                // A home has a legal destination, but this corner is wrong.
                yield return SendUntilStatus(sender,
                    WithTile(13, 0, 0, "Residence"), bridge,
                    "Legal plot exists");
                yield return SendUntilStatus(sender, starter, bridge,
                    "Board restored");

                // Swapping in a different card cannot bypass the draw.
                yield return SendUntilStatus(sender,
                    WithTile(11, 4, 3, "Green"), bridge,
                    "previous draw still has a legal plot");
                Assert.That(controller.HasPendingPlacement, Is.False);
                yield return SendUntilStatus(sender, starter, bridge,
                    "Board restored");

                // Moving the original home card is accepted.
                yield return SendFrames(sender, WithTile(13, 4, 3, "Residence"));
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator SyncedBoardStatusUpdatesWhenDayAdvances()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            root = new GameObject("Camera day status test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            var bridge = root.AddComponent<ConstructionCameraUdpBridge>();
            yield return null;

            const string starter = "{\"schemaVersion\":1,\"sequence\":1," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";
            using (var sender = new UdpClient())
            {
                yield return SendFrames(sender, starter);
                Assert.That(bridge.CurrentStatus,
                    Does.Contain("Finish Day 1 before drawing"));

                Assert.That(run.TrySimulateDay(out _), Is.True);
                Assert.That(controller.CurrentDay, Is.EqualTo(2));
                yield return SendFrames(sender, starter);

                Assert.That(bridge.CurrentStatus,
                    Does.Contain("Board synced. Draw one face-down tile"));
                Assert.That(bridge.StatusIsWarning, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator InvalidObservationRecoversAndBriefOcclusionKeepsPendingRoom()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.BoardGame);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            root = new GameObject("Camera observation recovery test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run);
            var bridge = root.AddComponent<ConstructionCameraUdpBridge>();
            yield return null;

            const string starter = "{\"schemaVersion\":1,\"sequence\":1," +
                "\"calibrated\":true,\"cornerCount\":4,\"error\":\"\"," +
                "\"tiles\":[{\"markerId\":10,\"column\":3,\"row\":3," +
                "\"category\":\"Green\",\"planting\":\"Oak\"}]}";
            const string invalid = "{\"schemaVersion\":1,\"sequence\":2," +
                "\"calibrated\":false,\"cornerCount\":0,\"error\":\"\"," +
                "\"tiles\":[]}";
            using (var sender = new UdpClient())
            {
                yield return SendFrames(sender, starter);
                Assert.That(bridge.StatusIsWarning, Is.False);

                yield return SendFrames(sender, invalid);
                Assert.That(bridge.StatusIsWarning, Is.True);

                // The exact same board must clear a stale calibration warning.
                yield return SendFrames(sender, starter);
                Assert.That(bridge.CurrentStatus, Does.Contain("Board synced"));
                Assert.That(bridge.StatusIsWarning, Is.False);

                yield return SendFrames(sender,
                    WithTile(13, 4, 3, "Residence"));
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);

                var bytes = Encoding.UTF8.GetBytes(invalid);
                sender.Send(bytes, bytes.Length,
                    new IPEndPoint(IPAddress.Loopback,
                        ConstructionCameraUdpBridge.ListenPort));
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.False);

                yield return SendFrames(sender,
                    WithTile(13, 4, 3, "Residence"));
                Assert.That(controller.HasPendingPlacement, Is.True);
                Assert.That(bridge.CanConfirmPendingPlacement, Is.True);
            }
        }

        private static IEnumerator SendFrames(UdpClient sender, string packet)
        {
            var bytes = Encoding.UTF8.GetBytes(packet);
            for (var frame = 0; frame < 55; frame++)
            {
                sender.Send(bytes, bytes.Length,
                    new IPEndPoint(IPAddress.Loopback,
                        ConstructionCameraUdpBridge.ListenPort));
                yield return new WaitForSecondsRealtime(0.04f);
            }
        }

        private static IEnumerator SendUntilStatus(UdpClient sender,
            string packet, ConstructionCameraUdpBridge bridge, string expected)
        {
            var bytes = Encoding.UTF8.GetBytes(packet);
            for (var frame = 0; frame < 65; frame++)
            {
                sender.Send(bytes, bytes.Length,
                    new IPEndPoint(IPAddress.Loopback,
                        ConstructionCameraUdpBridge.ListenPort));
                yield return new WaitForSecondsRealtime(0.04f);
                if (bridge.CurrentStatus.Contains(expected)) yield break;
            }
            Assert.That(bridge.CurrentStatus, Does.Contain(expected));
        }

        private static string WithTile(int markerId, int column, int row,
            string category) =>
            "{\"schemaVersion\":1,\"sequence\":2,\"calibrated\":true," +
            "\"cornerCount\":4,\"error\":\"\",\"tiles\":[" +
            "{\"markerId\":10,\"column\":3,\"row\":3," +
            "\"category\":\"Green\",\"planting\":\"Oak\"}," +
            "{\"markerId\":" + markerId + ",\"column\":" + column +
            ",\"row\":" + row + ",\"category\":\"" + category +
            "\",\"planting\":\"None\"}]}";
    }
}
