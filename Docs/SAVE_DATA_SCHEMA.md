# SYMBIOSIS: 49 — 会话存档结构

当前存档版本：`schemaVersion = 1`

运行时存档位置：

`Application.persistentDataPath/SYMBIOSIS_49/session-v1.json`

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
  "schemaVersion": 1,
  "savedAtUtc": "2026-09-18T23:00:00.0000000Z",
  "mode": "Sandbox",
  "elapsedSimulationSeconds": 120.0,
  "speedMultiplier": 2,
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

`rooms` 必须包含全部 35 个逻辑房间。载入时重新检查边界、重叠、托盘状态和完整 49 格；无效或版本不兼容的存档会被忽略，不会破坏默认布局。

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

后续扩展存档结构时增加 `schemaVersion`，不要在不改变版本号的情况下改变既有字段含义。
