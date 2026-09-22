# 城市共栖房间 Unity 原型

这是根据《城市共栖房间 游戏设计文档 V0.1》建立的 Unity 6 URP 灰盒工程。

## 仓库范围

本仓库保存可在 Unity 中重新打开和构建的源工程：`Assets`、`Packages`、`ProjectSettings`、建模源文件、工具脚本与测试代码。桌面副本同时保留本地 Windows 构建、测试日志和大尺寸预览资料；这些可再生成或体积较大的目录由 `.gitignore` 排除，不上传到 GitHub。

摄像头版本目前完成摄像头采集、校准界面、35 模块／49 格布局验证和稳定确认接口，但尚未包含从原始画面识别实体模块编号的成品检测器。不能把当前版本描述为已经完成真实实体识别。

当前版本完成：

- 精确的 7×7 单层地图、49 个单位格与 35 个逻辑房间
- 固定7×7底板、49个可识别格位、独立可移动房间外壳与内容层，以及自动封闭外围出口的边界覆盖层
- 1×1房间固定4个开放门框；1×2与2×1房间固定6个开放门框并随整个模块旋转
- 按房间功能区分的颜色、中文名称和尺寸标注
- 中央公园、住宅、办公室、食物设施、车库、垃圾房及动物栖息房的基础几何表现
- 无标题、无上下边框的极简游戏 HUD；旧版左右信息大面板已移除
- 六分钟昼夜循环：黎明 30 秒、白天 150 秒、黄昏 30 秒、夜晚 150 秒
- 沙盒暂停、1×、2×、4×速度控制，以及四项全局生态指标占位组件
- 左下角鸽子、松鼠、刺猬和狐狸的存活动物数量组件
- Esc 暂停菜单、游戏音量、静音和中英文即时切换，并通过 PlayerPrefs 保存
- 返回桌面后保留当前运行，可继续此前的沙盒状态
- 全程正俯视：正方形棋盘总览、滚轮缩放与中键平移；双击房间或跟随动物只进行平移和缩放，右键/Esc返回总览
- 点击房间后使用紧凑的名称与状态图标，不再打开永久信息面板
- 无摄像头版自由重新拼板框架：整房拖拽、1×2 模块旋转、合法/非法落点反馈
- 编辑时自动暂停模拟；中央公园、鸽群房和狐狸洞显示固定标记
- 单模块临时托盘、完整 49 格校验、确认继续运行和取消恢复编辑前布局
- 房间及其家具作为完整模块移动，动物示例保持原地图坐标不随房间移动
- 确认布局后自动重建房间邻接图；落在新墙线上的动物平滑移至最近合法地面且不计死亡
- 自动保存当前布局、模拟时间、模式与倍速；返回桌面或下次启动时继续同一运行
- 中央公园鸽子动画样板：12 帧定格感待机、行走、啄食与扑翅
- 中央公园松鼠动画样板：按真实比例呈现待机、跳跑、觅食与警觉
- 中央公园刺猬动画样板：按真实比例呈现待机、短步行走、嗅探与蜷缩警戒
- 点击鸽子进入正俯视跟随视角，行走时仅显示 6 秒足迹
- `K` 模拟死亡，留下 5 秒物种印记，20 秒后原位重生
- Unity 编辑器菜单中的场景重建、布局校验和预览图导出工具

## 打开方式

使用 Unity `6000.3.4f1` 打开本目录，然后打开 `Assets/Scenes/Main.unity`。按 Play 即可查看灰盒布局并点击不同房间。

如需重新生成场景，使用菜单：

`Urban Wildlife > Create Or Refresh Main Scene`

如需输出 1920×1080 预览图，使用菜单：

`Urban Wildlife > Capture Layout Preview`

如需输出拼板编辑状态预览图，使用菜单：

`Urban Wildlife > Capture Layout Editing Preview`

如需隐藏UI并检查固定底板、门框和边界覆盖层，使用菜单：

`Urban Wildlife > Capture Board Foundation Preview`

如需输出鸽子动画预览帧，使用菜单：

`Urban Wildlife > Capture Pigeon Animation Preview Frames`

## 三维模型制作流程

- 后续所有人物、动物、房间道具和环境模型必须先制作可建模概念图。
- 概念图至少包含45度游戏视角、正侧面比例和需要验证的关键动作姿态。
- 概念造型经确认后才能开始Blender建模，完成后再进入Unity做真实比例与地图视角验证。
- 被否定或被替代的版本继续保留，使用递增版本号，不直接覆盖。
- 已确认松鼠概念图：`SourceAssets/References/squirrel-feasible-concept-v01.png`
- 已确认刺猬概念图：`SourceAssets/References/hedgehog-feasible-concept-v02.png`
- 当前待确认狐狸概念图：`SourceAssets/References/fox-feasible-concept-v01.png`

## Blender 鸽子动画原型

- Blender 精细化源文件：`SourceAssets/Blender/SYMBIOSIS_49_Pigeon_v02.blend`
- Unity 精细化 FBX 资产：`Assets/Resources/Animals/Pigeon/Pigeon_Animated_v02.fbx`
- 动作参考：`SourceAssets/References/pigeon-animation-keyposes-v01.png`
- 生成脚本：`Tools/Blender/create_pigeon_animation.py`
- 动画段落：待机、行走摆头、啄食、短暂扑翅与收势，定格感采样为 12 帧/秒
- v02 增加分层主羽、翼尖骨骼、五片尾羽、头颈面片、眼圈、上下喙与细化脚爪，并在 Unity 外层统一模型前向轴。

Unity 会优先载入 Blender FBX；只有在资产缺失时才回退到程序化占位鸽子。

## Blender 松鼠动画原型

- Blender 源文件：`SourceAssets/Blender/SYMBIOSIS_49_Squirrel_v02.blend`
- Unity FBX 资产：`Assets/Resources/Animals/Squirrel/Squirrel_Animated_v02.fbx`
- 生成脚本：`Tools/Blender/create_squirrel_animation.py`
- 动画段落：呼吸待机、连续跳跑、低头觅食、直立警觉与收势，定格感采样为 12 帧/秒。
- v02 按确认概念图重做头身比例、胸腹分色、四肢和连续蓬松尾巴，并保留可替换材质与骨骼动画。
- Unity 外层按源模型 2.315599 个建模单位等比缩放至 0.38 米；模型朝向与实际移动方向一致。
- 地图动画预览：`Docs/Images/squirrel-animation-mapscale-v01-apng.png`

## Blender 城市居民动画原型

- Blender 源文件：`SourceAssets/Blender/SYMBIOSIS_49_Citizen_v01.blend`
- Unity FBX 资产：`Assets/Resources/People/Citizen/Citizen_Animated_v01.fbx`
- 生成脚本：`Tools/Blender/create_citizen_animation.py`
- 视觉基准：芥末黄针织衫、蓝色长裤、帆布袋和白色运动鞋的普通城市居民。
- 动画段落：呼吸待机、行走、抬手观察与重心停留，定格感采样为 12 帧/秒。
- Unity 中人物与动物使用同一套模型轴向规范，头部始终朝实际移动方向。

## Blender 刺猬动画原型

- Blender 源文件：`SourceAssets/Blender/SYMBIOSIS_49_Hedgehog_v04.blend`
- Unity FBX 资产：`Assets/Resources/Animals/Hedgehog/Hedgehog_Animated_v04.fbx`
- 生成脚本：`Tools/Blender/create_hedgehog_animation.py`
- 动画段落：呼吸待机、低幅短步、低头嗅探、蜷缩警戒与收势，定格感采样为 12 帧/秒。
- Unity 外层按源模型 1.487344 个建模单位等比缩放至 0.25 米。
- v04 按确认的概念图 v02 改为更圆润的一体式刺壳、隐藏式短头、仅露脚掌的短腿和明确的浅色腹部，蜷缩时头脚收入刺壳轮廓。
- 地图动画预览：`Docs/Images/hedgehog-animation-mapscale-v03-apng.png`

## Blender 狐狸动画原型

- Blender 源文件：`SourceAssets/Blender/SYMBIOSIS_49_Fox_v01.blend`
- Unity FBX 资产：`Assets/Resources/Animals/Fox/Fox_Animated_v01.fbx`
- 结构参考：用户提供的低多边形狐狸模型截图与 `SourceAssets/References/fox-feasible-concept-v01.png`
- 生成脚本：`Tools/Blender/create_fox_animation.py`
- 动画段落：呼吸待机、低幅小跑、低头嗅探、抬头警觉与收势，定格感采样为 12 帧/秒。
- 每条腿由上下两段骨骼驱动，尾巴由三段骨骼驱动；材质与低多边形体块可继续手动修改。
- Unity 外层按源模型 3.476015 个建模单位等比缩放至 0.95 米。

## 地图与角色比例

- 1 Unity单位约等于1米；一个地图格边长固定为3.1米。
- 城市居民显示高度约1.72米；鸽子显示长度约0.43米；松鼠建模基准长度0.38米。
- 松鼠、刺猬和成年狐狸的导入外层分别按源包围跨度校准至0.38米、0.25米和0.95米。
- 特写只调整摄像机，不放大模型；点击范围和选择环可独立于模型尺寸设置。
- 所有FBX缩放放在动画节点外层，避免播放动画时被重置。

## 代码结构

- `Assets/Scripts/Data`：房间类型、精确排布和数据校验
- `Assets/Scripts/Presentation`：地图、房间、基础模型和颜色系统
- `Assets/Scripts/UI`：极简 HUD、昼夜钟表与环形指标组件
- `Assets/Scripts/Core/SimulationClockModel.cs`：独立可测试的六分钟昼夜时钟
- `Assets/Scripts/Core/GameRuntimeController.cs`：暂停、倍速、语言、音量与运行状态
- `Assets/Scripts/Presentation/BoardCameraController.cs`：全景、缩放、平移、房间聚焦与动物跟随
- `Assets/Scripts/Data/RoomLayoutModel.cs`：房间位置、旋转、托盘与 49 格合法性规则
- `Assets/Scripts/Presentation/RoomLayoutEditorController.cs`：鼠标拖拽、预览反馈与编辑事务
- `Assets/Scripts/Presentation/RoomShellLayout.cs`：通用房间外壳的4门／6门拓扑与旋转位置
- `Assets/Scripts/Data/RoomNavigationMap.cs`：布局变化后的房间邻接图与最近合法地面查询
- `Assets/Scripts/Core/SessionPersistenceController.cs`：版本化 JSON 会话存档与恢复
- `Assets/Scripts/Editor`：场景生成和预览导出
- `Assets/Tests/Editor`：排布完整性测试
