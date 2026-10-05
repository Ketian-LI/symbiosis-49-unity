# SYMBIOSIS: 49 — 会话存档结构

当前运行存档版本：`schemaVersion = 2`。旧版 35 房存档 `session-v1.json` 保留原位，不会被新版 49 房存档覆盖。

运行时存档位置：

`Application.persistentDataPath/SYMBIOSIS_49/session-v2.json`

永久纪录位置：

`Application.persistentDataPath/SYMBIOSIS_49/profile-v1.json`

## 保存时机

- 每 15 秒自动保存一次。
- 玩家确认新布局后立即保存。
- 玩家选择“回到桌面”时立即保存。
- 应用进入后台或退出时保存。
- 布局编辑尚未确认时不写入存档，防止保存半完成布局。

## 当前字段

```json
{
  "schemaVersion": 2,
  "savedAtUtc": "2026-09-18T23:00:00.0000000Z",
  "mode": "Sandbox",
  "elapsedSimulationSeconds": 120.0,
  "speedMultiplier": 2,
  "roomMovements": 2,
  "lastRoomMovementDay": 3,
  "shrubShelters": [
    { "roomId": "shrub-a", "readyDay": 5 }
  ],
  "dailyOutcome": {
    "pendingDay": 4,
    "pendingMovedRooms": 2,
    "previousTotalDeaths": 1,
    "previousPredationDeaths": 0,
    "predationHistoryKnown": true,
    "lastDay": 3,
    "lastWorkingResidents": 4,
    "lastWorkingResidentsKnown": true,
    "lastDeaths": 0,
    "lastPredationDeaths": 0,
    "lastMealsKnown": true,
    "lastFedAnimals": 14,
    "lastLivingAnimals": 19,
    "lastFedPigeons": 8,
    "lastLivingPigeons": 12,
    "lastSeedPortionsLeft": 2
  },
  "rooms": [
    {
      "id": "garage-a",
      "column": 0,
      "row": 0,
      "quarterTurns": 0
    }
  ]
}
```

`rooms` 必须包含全部 49 间独立单格房。载入时重新检查边界、重叠、托盘状态和完整 49 格；无效或版本不兼容的存档会被忽略，不会破坏默认布局。旧版 35 房进度不会自动迁移到新版布局，永久纪录仍保存在 `profile-v1.json`。

`lastRoomMovementDay` 记录最近一次已确认的免费调整日，用于阻止同一天重复确认；旧存档缺少此字段时按 `0` 处理。取消编辑、教程中放回原位都不占用调整次数。

`shrubShelters` 只记录曾被搬动的灌木及其恢复日；搬动当日和次日不提供庇护，从 `readyDay` 起恢复。旧存档没有此可选字段时，所有灌木视为已恢复。它不保存未完成的觅食行动；重新载入后次日回顾从新观察开始。

`dailyOutcome` 保存当日已移动房间数、上班人数、死亡计数基线和最近一次日结回顾；死亡原因单列饥饿、交通与狐狸捕食。旧存档没有该字段时，以载入时的死亡总数作基线；旧存档没有捕食基线时，以载入时已记录的捕食数作基线，避免把历史死亡重复记为新的一天。回顾将同期观察结果与操作并列展示，不把上班人数、死亡或垃圾变化全部归因于移动。旧版资源余额字段仍可读取以兼容已有存档，但当前玩法不再产生、消费或因资源点不足而结束。

`lastFedAnimals` 和 `lastFedPigeons` 是结算前实际完成进食的存活动物数量；`lastSeedPortionsLeft` 是次日补给前尚未消耗的种子份数，不等于可达份数。旧存档缺少 `lastMealsKnown` 时，回顾不伪造进食数字。

## 永久纪录字段

```json
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-09-19T12:00:00.0000000Z",
  "bestSurvivalDays": 12
}
```

`bestSurvivalDays` 只在一局结束且存活天数高于旧纪录时更新。失败后重新开始会清除当前局存档，但不会清除该纪录。

## 不在当前版本保存的内容

- 最终房间美术资源引用。
- 尚未确认的物种需求参数。
- 研究记录与死亡事件表；研究导出将使用独立的会话文件夹。
- 摄像头识别的原始图像。

兼容性的可选字段可在版本 1 中追加并为旧存档提供默认值；改变既有字段含义或做不兼容调整时必须增加 `schemaVersion` 并提供迁移或明确的拒绝行为。
