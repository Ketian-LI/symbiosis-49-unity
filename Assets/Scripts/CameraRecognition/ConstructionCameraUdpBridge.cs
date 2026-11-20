using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.CameraRecognition
{
    // Receives artwork or ArUco observations from the local Python detector.
    // Only BoardGame attaches this bridge. A stable physical addition stages
    // the normal Unity confirmation; camera input never commits a build.
    public sealed class ConstructionCameraUdpBridge : MonoBehaviour
    {
        public const int ListenPort = 49549;
        private const int MaximumPacketBytes = 32768;
        private const int MaximumQueuedPackets = 8;
        private const float MaximumObservationAgeSeconds = 0.5f;

        private readonly ConcurrentQueue<string> incomingPackets = new();
        private readonly PhysicalBoardStabilityTracker stability = new();
        private ConstructionPrototypeController controller;
        private PhysicalBoardCameraController legacyCamera;
        private ConstructionCameraSnapshot baseline;
        private ConstructionCameraSnapshot stagedSnapshot;
        private ConstructionCameraTile stagedAddition;
        private UdpClient listener;
        private Thread receiveThread;
        private volatile bool receiving;
        private bool awaitingDecision;
        private bool requiresBoardReset;
        // A drawn card with a legal destination must stay in play even after
        // the player restores the board to move it to a different cell.
        private int? requiredDrawMarkerId;
        private string observedSignature = string.Empty;
        private string currentStatus = string.Empty;
        private string currentStatusChinese = string.Empty;
        private float previousObservationAt = -1f;
        private float lastPacketAt = -1f;
        private float lastMatchingStagedAt = -1f;
        private float firstInvalidPacketAt = -1f;
        private int lastSyncedStatusDay = -1;
        private bool hasReceivedPacket;
        private bool statusIsWarning;

        public string CurrentStatus => currentStatus;
        public string CurrentStatusChinese => currentStatusChinese;
        public bool StatusIsWarning => statusIsWarning;
        public bool HasReceivedPacket => hasReceivedPacket;
        public float StabilityProgress => stability.Progress;
        public bool CanConfirmPendingPlacement => awaitingDecision &&
            stagedSnapshot != null && stagedAddition != null &&
            controller != null && controller.CameraPendingMatches(
                stagedAddition.Column, stagedAddition.Row,
                stagedAddition.Category, stagedAddition.Planting,
                stagedAddition.ResidenceKind) && lastMatchingStagedAt >= 0f &&
            Time.realtimeSinceStartup - lastMatchingStagedAt <=
            MaximumObservationAgeSeconds;
        public event Action<string, string, bool> StatusChanged;
        public static bool TryLaunchCameraTool(out string error) =>
            ConstructionCameraToolLauncher.TryLaunchCameraTool(out error);

        private IEnumerator Start()
        {
            controller = GetComponent<ConstructionPrototypeController>();
            if (controller == null || controller.Mode != ConstructionGameMode.BoardGame)
            {
                enabled = false;
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + 5f;
            while (controller.BuiltCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (controller.BuiltCount == 0)
            {
                SetStatus("Camera bridge could not load the tabletop run.",
                    "桌游存档未能载入，请返回主菜单重试。", true);
                enabled = false;
                yield break;
            }

            // A camera build may also contain the legacy full-board preview.
            // The local detector must have exclusive access to the device.
            legacyCamera = FindFirstObjectByType<PhysicalBoardCameraController>(
                FindObjectsInactive.Include);
            legacyCamera?.SetRecognitionSuspended(true);

            try
            {
                listener = new UdpClient(new IPEndPoint(IPAddress.Loopback,
                    ListenPort));
                listener.Client.ReceiveTimeout = 250;
                receiving = true;
                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "SYMBIOSIS49 camera UDP receiver"
                };
                receiveThread.Start();
                lastPacketAt = Time.realtimeSinceStartup;
                SetStatus($"Waiting for the board detector on 127.0.0.1:{ListenPort}.",
                    "等待相机识别工具连接。请点击“启动相机”。");
            }
            catch (Exception exception)
            {
                SetStatus($"Camera bridge cannot open port {ListenPort}: " +
                          exception.Message,
                    "相机连接端口不可用，请关闭重复运行的识别工具。", true);
                legacyCamera?.SetRecognitionSuspended(false);
                enabled = false;
            }
        }

        private void Update()
        {
            if (controller == null || controller.Mode != ConstructionGameMode.BoardGame)
                return;
            MonitorStagedPlacement();

            var now = Time.realtimeSinceStartup;
            if (lastPacketAt >= 0f && now - lastPacketAt >
                MaximumObservationAgeSeconds)
            {
                if (awaitingDecision)
                    CancelStaged("Camera image stopped; the pending room was cancelled.",
                        "相机画面中断，已取消待确认的房间。", true);
                else
                {
                    ResetStability();
                    SetStatus("Waiting for live camera observations.",
                        "等待相机画面；检查连接与棋盘四角图案。", true);
                }
            }

            string newest = null;
            while (incomingPackets.TryDequeue(out var packet)) newest = packet;
            if (newest == null) return;
            lastPacketAt = now;
            hasReceivedPacket = true;

            if (!ConstructionCameraSnapshotModel.TryParseAndValidate(newest,
                    out var snapshot, out var error))
            {
                if (firstInvalidPacketAt < 0f) firstInvalidPacketAt = now;
                ResetStability();
                if (awaitingDecision)
                {
                    // Never confirm a placement without a current matching image.
                    // Brief occlusion should not destroy the pending decision.
                    lastMatchingStagedAt = -1f;
                    if (now - firstInvalidPacketAt >=
                        MaximumObservationAgeSeconds)
                        CancelStaged("Camera lost the pending board: " + error,
                            "棋盘识别中断，已取消待确认的房间。", true);
                }
                else if (now - firstInvalidPacketAt >=
                         MaximumObservationAgeSeconds)
                {
                    SetStatus(error,
                        "识别结果无效，请确认棋盘四角图案和房间方块完整入镜。", true);
                }
                return;
            }
            firstInvalidPacketAt = -1f;
            ProcessSnapshot(snapshot);
        }

        private void ProcessSnapshot(ConstructionCameraSnapshot snapshot)
        {
            if (awaitingDecision)
            {
                if (snapshot.Signature == stagedSnapshot?.Signature)
                {
                    lastMatchingStagedAt = Time.realtimeSinceStartup;
                    return;
                }
                CancelStaged("Physical room changed before confirmation; " +
                             "the preview was cancelled.",
                    "实体方块在确认前发生变化，已取消预览。",
                    snapshot.Signature != baseline?.Signature);
                return;
            }

            if (baseline == null || controller.BuiltCount != baseline.Tiles.Count)
            {
                baseline = null;
                if (!ConstructionCameraSnapshotModel.MatchesBuiltTiles(snapshot,
                        controller.BuiltTiles, out var mismatch))
                {
                    ResetStability();
                    SetStatus("Match the physical board to the saved run. " +
                              mismatch,
                        "实体棋盘与存档不一致，请核对方块数量、种类和位置。", true);
                    return;
                }
                if (!ObserveUntilStable(snapshot.Signature))
                {
                    SetStatus("Hold the matching physical board still to resume.",
                        "棋盘已匹配，请保持静止完成同步。");
                    return;
                }
                baseline = snapshot;
                requiresBoardReset = false;
                ResetStability();
                SetBoardSyncedStatus();
                return;
            }

            if (requiresBoardReset)
            {
                if (snapshot.Signature != baseline.Signature)
                {
                    ResetStability();
                    SetStatus("Remove the unconfirmed room and restore " +
                              "the accepted physical board.",
                        "请移走未确认的方块，恢复上次已确认的棋盘。", true);
                    return;
                }
                if (!ObserveUntilStable(snapshot.Signature)) return;
                requiresBoardReset = false;
                ResetStability();
                SetStatus(controller.CameraUsesRandomDraw
                        ? "Board restored. Place one tile."
                        : "Board reset accepted. Add one printed room tile.",
                    controller.CameraUsesRandomDraw
                        ? "棋盘已恢复。可以放置一块方块。"
                        : "棋盘已恢复，可以再放一个方块。");
                return;
            }

            if (snapshot.Signature == baseline.Signature)
            {
                ResetStability();
                if (statusIsWarning || lastSyncedStatusDay != controller.CurrentDay)
                    SetBoardSyncedStatus();
                return;
            }
            if (!ConstructionCameraSnapshotModel.TryGetSingleAddition(baseline,
                    snapshot, out var addition, out var additionError))
            {
                ResetStability();
                SetStatus(additionError,
                    "一次只能新增一个方块；已确认的方块不能移动或更换。", true);
                return;
            }
            if (controller.CameraUsesRandomDraw && requiredDrawMarkerId.HasValue &&
                addition.MarkerId != requiredDrawMarkerId.Value)
            {
                requiresBoardReset = true;
                ResetStability();
                SetStatus("The previous draw still has a legal plot. " +
                          "Restore the board and place that same tile.",
                    "上一块仍有合法格。恢复棋盘后放置同一块方块。", true);
                return;
            }
            if (controller.HasPendingPlacement)
            {
                ResetStability();
                SetStatus("Finish the pending room preview in Unity first.",
                    "请先在游戏内确认或取消当前房间。", true);
                return;
            }
            if (!ObserveUntilStable(snapshot.Signature))
            {
                SetStatus("Hold the new room still for recognition.",
                    "请保持新方块静止，等待识别完成。");
                return;
            }

            ResetStability();
            var selected = controller.TrySelect(addition.Category,
                addition.Planting);
            if (selected && addition.Category == ConstructionCategory.Residence)
                selected = controller.TrySelectResidenceKind(addition.ResidenceKind);
            if (!selected || !controller.RequestCameraPlacement(addition.Column,
                    addition.Row) || !controller.HasPendingPlacement)
            {
                requiresBoardReset = true;
                if (controller.CameraUsesRandomDraw)
                {
                    if (!controller.CameraCanBuildToday)
                        SetStatus("Remove tile; finish this day first.",
                            "移走方块；先结算今天。", true);
                    else if (!controller.CameraDrawnTypeHasLegalPlot(
                                 addition.Category, addition.Planting))
                    {
                        requiredDrawMarkerId = null;
                        SetStatus("No legal plot. Set aside; redraw after board reset.",
                            "全盘无合法格。暂存此块；恢复棋盘后重抽。", true);
                    }
                    else
                    {
                        requiredDrawMarkerId = addition.MarkerId;
                        SetStatus("Legal plot exists. Restore board; move this tile.",
                            "仍有合法格。恢复棋盘后移动同一方块。", true);
                    }
                }
                else
                    SetStatus("This room is not legal today. Remove it to continue.",
                        "此位置或建筑今天不能建造，请移走方块。", true);
                return;
            }

            stagedSnapshot = snapshot;
            stagedAddition = addition;
            if (controller.CameraUsesRandomDraw)
                requiredDrawMarkerId = addition.MarkerId;
            awaitingDecision = true;
            lastMatchingStagedAt = Time.realtimeSinceStartup;
            SetStatus("New room recognised. Confirm or cancel it in Unity.",
                "已识别新房间。请在游戏内确认或取消。");
        }

        private void MonitorStagedPlacement()
        {
            if (!awaitingDecision || stagedSnapshot == null) return;
            if (controller.BuiltCount == stagedSnapshot.Tiles.Count)
            {
                if (ConstructionCameraSnapshotModel.MatchesBuiltTiles(stagedSnapshot,
                        controller.BuiltTiles, out _))
                {
                    baseline = stagedSnapshot;
                    stagedSnapshot = null;
                    stagedAddition = null;
                    awaitingDecision = false;
                    requiredDrawMarkerId = null;
                    lastMatchingStagedAt = -1f;
                    ResetStability();
                    SetStatus("Room confirmed. Advance the day before adding " +
                              "another room.",
                        "房间已确认。请进入下一天后再添加方块。");
                    return;
                }
                // A different manual build was committed; require a full
                // physical/save match before accepting another observation.
                baseline = null;
                stagedSnapshot = null;
                stagedAddition = null;
                awaitingDecision = false;
                lastMatchingStagedAt = -1f;
                ResetStability();
                SetStatus("The saved layout changed. Match the physical " +
                          "board to the game before continuing.",
                    "游戏布局发生变化，请先让实体棋盘与游戏一致。", true);
                return;
            }
            if (controller.HasPendingPlacement) return;
            CancelStaged("Room was not confirmed. Remove the added tile.",
                "房间未确认，请移走刚加入的方块。", true);
        }

        private void SetBoardSyncedStatus()
        {
            lastSyncedStatusDay = controller.CurrentDay;
            SetStatus(controller.CurrentDay == 1
                    ? "Physical board synced. Finish Day 1 before drawing."
                    : controller.CameraUsesRandomDraw
                        ? "Board synced. Draw one face-down tile."
                        : "Physical board synced. Add one printed room tile.",
                controller.CurrentDay == 1
                    ? "实体棋盘已同步。先结束第 1 天，再开始抽牌。"
                    : controller.CameraUsesRandomDraw
                        ? "棋盘已同步。随机抽一块背面朝上的方块。"
                        : "实体棋盘已同步。请加入一块房间方块。");
        }

        private void CancelStaged(string message, string chinese,
            bool needsReset)
        {
            if (controller != null && controller.HasPendingPlacement)
                controller.CancelCameraPendingPlacement();
            stagedSnapshot = null;
            stagedAddition = null;
            awaitingDecision = false;
            lastMatchingStagedAt = -1f;
            requiresBoardReset = needsReset;
            ResetStability();
            SetStatus(message, chinese, true);
        }

        private bool ObserveUntilStable(string signature)
        {
            var now = Time.realtimeSinceStartup;
            var delta = 0f;
            if (signature == observedSignature && previousObservationAt >= 0f)
            {
                var interval = now - previousObservationAt;
                if (interval <= MaximumObservationAgeSeconds)
                    delta = Mathf.Max(0f, interval);
                else
                {
                    stability.Reset();
                    stability.Observe(signature, 0f);
                    previousObservationAt = now;
                    return false;
                }
            }
            else
            {
                stability.Reset();
                observedSignature = signature;
            }
            previousObservationAt = now;
            stability.Observe(signature, delta);
            return stability.Progress >= 1f;
        }

        private void ResetStability()
        {
            stability.Reset();
            observedSignature = string.Empty;
            previousObservationAt = -1f;
        }

        private void ReceiveLoop()
        {
            var sender = new IPEndPoint(IPAddress.Loopback, 0);
            while (receiving)
            {
                try
                {
                    var bytes = listener.Receive(ref sender);
                    if (sender.Address.Equals(IPAddress.Loopback) &&
                        bytes.Length <= MaximumPacketBytes)
                        EnqueuePacket(Encoding.UTF8.GetString(bytes));
                }
                catch (SocketException exception) when (
                    exception.SocketErrorCode == SocketError.TimedOut ||
                    exception.SocketErrorCode == SocketError.Interrupted)
                {
                    // Timeout keeps shutdown responsive without Unity API calls.
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    EnqueuePacket("{\"schemaVersion\":0,\"error\":\"UDP receive " +
                                  "failed: " + EscapeJson(exception.Message) + "\"}");
                    return;
                }
            }
        }

        private void EnqueuePacket(string packet)
        {
            while (incomingPackets.Count >= MaximumQueuedPackets)
                incomingPackets.TryDequeue(out _);
            incomingPackets.Enqueue(packet);
        }

        private void OnDestroy()
        {
            receiving = false;
            listener?.Close();
            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Join(300);
            listener?.Dispose();
            ConstructionCameraToolLauncher.StopCameraTool();
            if (legacyCamera != null)
                legacyCamera.SetRecognitionSuspended(false);
        }

        private void SetStatus(string message, string chinese,
            bool warning = false)
        {
            if (message == currentStatus && chinese == currentStatusChinese &&
                warning == statusIsWarning) return;
            currentStatus = message;
            currentStatusChinese = chinese;
            statusIsWarning = warning;
            StatusChanged?.Invoke(message, chinese, warning);
            if (warning) Debug.LogWarning("[Camera Recognition] " + message, this);
            else Debug.Log("[Camera Recognition] " + message, this);
        }

        private static string EscapeJson(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
