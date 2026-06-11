# My Project — 技术文档

2D 塔防策略游戏（Unity 2022.3.62f3c1），融合卡牌部署与路径移动机制。

---

## 1. 关卡统一配置（LevelSetup）

**文件:** `Assets/Level/CommonLevel/LevelSetup/LevelSetup.cs`

每个关卡场景中挂载一个 `LevelSetup` 组件，作为该关卡所有配置的统一入口。它在 `Awake`/`Start` 中自动将设置分发到各子系统。

### Inspector 中的配置分组：

| 分组 | 功能 | 关键参数 |
|------|------|----------|
| 关卡基本信息 | 名称、索引、描述 | `levelName`, `levelIndex`, `levelDescription` |
| 战斗/费用 | 覆盖 `BattleManager` 的默认值 | `maxCost`（费用上限）、`costAddSpeed`（回复速度）、`startingCost`（初始费用） |
| 波次 | 绑定本关的波次配置 | `waveList`（拖入 WaveList 资产） |
| 防御塔 | 拖入场景中的双塔对象 | `playerTower`, `enemyTower`，可选择性覆盖塔属性 |
| 卡组 | 替换玩家可用卡牌 | `availableCards`（CardData 数组） |
| 资源点 | 覆盖场景中所有 ResourcePoint 的默认值 | `maxGarrison`, `garrisonRange`, `resourceBonus` |
| 驻扎点 | 写入 GarrisonPointManager 的默认值 | `maxGarrison`, `garrisonRange` |

> 每个分组都有独立的 `overrideXxx` 开关。关闭后该子系统使用自身的 Inspector 默认值，不受 LevelSetup 影响。

---

## 2. 路径系统

**核心文件:**
- `Assets/Level/CommonLevel/Path/PathManager.cs` — 单条路径的数据与运行时逻辑
- `Assets/Level/CommonLevel/Path/PathManager.Editor.cs` — 编辑器 Gizmo 绘制与右键工具
- `Assets/Level/CommonLevel/Path/LevelPathManager.cs` — 场景级路径注册中心（单例）
- `Assets/Level/CommonLevel/Path/PathVisualManager.cs` — LineRenderer 视觉效果
- `Assets/Level/CommonLevel/Helper/CatmullRomMath.cs` — Catmull-Rom 曲线数学库
- `Assets/Level/CommonLevel/Helper/PathMath.cs` — 直线路径数学库
- `Assets/Level/CommonLevel/Helper/Math2DHelper.cs` — 2D 几何工具

### 创建路径

1. 在场景中创建空 GameObject，挂载 `PathManager` 组件（会自动添加 `PathVisualManager`）
2. 设置 **阵营** (`CampType`) 和 **路线 ID** (`PathID`)
3. 在 `pathPoints` 列表中拖入路径节点（Transform 空物体），按经过顺序排列
4. 根据需要开启曲线模式或循环模式

### 关键参数

| 参数 | 说明 |
|------|------|
| `camp` | 所属阵营，决定部署时哪些卡牌可以使用此路线 |
| `pathId` | 路线唯一标识（Path_01 / Path_02 / Path_03），波次配置中通过此 ID 绑定 |
| `useCurve` | 开启后使用 Centripetal Catmull-Rom 曲线插值（默认关闭，走直线折线） |
| `curveAlpha` | 曲线参数化指数，默认 0.5（centripetal），值越小越趋近均匀，越大越趋近弦长 |
| `isLooping` | 开启后路径首尾相连形成闭环，单位到达终点后自动回到起点循环前进 |

### 编辑器工具

- 选中 PathManager 物体后，Scene 视图中会绘制路径 Gizmo（曲线模式显示平滑曲线，直线模式显示折线）
- 右键 PathManager 组件标题 → 可执行 "反转路径"（反转节点顺序）或 "交换阵营"（Player ↔ Enemy）

### 运行时查询

```csharp
// 按阵营+ID 获取单条路径
PathManager path = LevelPathManager.Instance.GetPath(CampType.Player, PathID.Path_01);

// 获取某阵营所有路径
List<PathManager> paths = LevelPathManager.Instance.GetAllPathsByCamp(CampType.Enemy);

// 获取路径上某一点的最近位置（曲线/直线统一 API）
var result = path.GetClosestPoint(worldPosition);
// result.point → 最近点坐标
// result.distance → 距离
// result.curveT → 归一化位置 [0,1]
```

---

## 3. 卡牌系统

**核心文件:**
- `Assets/Level/CommonLevel/Card/CardData.cs` — 卡牌数据（ScriptableObject）
- `Assets/Level/CommonLevel/Card/CardManager.cs` — 卡牌管理器（单例），生成 UI、管理选中
- `Assets/Level/CommonLevel/Card/CardInteraction.cs` — 单张卡牌的 UI 交互组件
- `Assets/Level/CommonLevel/Card/CardDeploy.cs` — 部署系统，处理悬停、高亮、点击部署
- `Assets/Level/CommonLevel/Card/PlayerDeck.cs` — 玩家卡组数据容器

### 创建卡牌

1. 在 Project 窗口中右键 → 创建 `CardData` ScriptableObject 资产
2. 配置卡牌属性：

| 参数 | 说明 |
|------|------|
| `camp` | 卡牌阵营，决定可在哪个阵营的路线上部署 |
| `cost` | 部署费用 |
| `cooldown` | 冷却时间（秒），部署后需等待才能再次使用 |
| `cardIcon` | 卡牌 UI 图标 |
| `unitPrefab` | 部署时生成的单位预制体 |

### 设置卡组

通过 `LevelSetup.availableCards` 或直接操作 `CardManager.playerDeck`：

```csharp
// 在 CardManager 的 Inspector 中直接拖入 PlayerDeck
// 或者通过 LevelSetup 统一配置
```

### 玩家操作流程

1. **选中卡牌** — 点击卡牌 UI，进入部署模式
2. **悬停路线** — 鼠标移动到同阵营路线上方，路线高亮
3. **确认部署** — 左键点击高亮路线，在路径起点生成单位、扣费、卡牌进入冷却
4. **取消部署** — 右键退出部署模式

### 扩展方向

- 给 `CardData` 添加类型字段，在 `CardDeploy` 中根据类型切换部署方式（如选路径变为选范围）
- 敌人可向玩家牌堆塞负面卡牌，根据阵营判断并强制打出
- 援助卡牌仅可在特定路径上部署

---

## 4. 单位系统

**核心文件:**
- `Assets/Unit/GenericScript/UnitAttr.cs` — 属性数据组件
- `Assets/Unit/GenericScript/UnitAI.cs` — 行为决策组件
- `Assets/Unit/GenericScript/UnitMovement.cs` — 移动执行组件
- `Assets/Unit/GenericScript/UnitCombat.cs` — 战斗执行组件
- `Assets/Unit/GenericScript/UnitUI.cs` — 血条 UI 组件
- `Assets/Unit/GenericScript/ArcherCombat.cs` / `MageCombat.cs` / `HealerCombat.cs` — 特殊兵种战斗变体
- `Assets/Unit/GenericScript/Bullet.cs` — 投射物
- `Assets/Unit/GenericScript/FloatNumber.cs` — 浮动伤害数字

### 单位架构

单位采用模块化组件设计，每个组件只负责一个关注点：

| 组件 | 职责 |
|------|------|
| `UnitAttr` | 纯数据存储：HP、攻击、速度、射程、防御、阵营、单位类型、攻击类型 |
| `UnitAI` | 每帧决策：检测敌人 → 战斗 / 驻扎点交互 / 资源点交互 / 沿路径移动 / 攻击防御塔 |
| `UnitMovement` | 执行移动：沿路径匀速前进（支持曲线/直线/循环）、追击、飞行 |
| `UnitCombat` | 执行战斗：索敌（OverlapCircle）、攻击冷却、伤害计算、死亡处理 |
| `UnitUI` | 血条生成、刷新、销毁 |

### 创建单位预制体

1. 创建 GameObject，按需挂载组件：**必须的** → `UnitAttr` + `UnitAI` + `UnitMovement` + `UnitCombat` + `UnitUI`
2. 配置 `UnitAttr` 的关键属性：

| 参数 | 说明 |
|------|------|
| `camp` | 所属阵营 |
| `maxHp` / `atk` / `moveSpeed` / `atkRange` / `atkCD` | 基础战斗属性 |
| `detectRange` | 大范围搜寻敌人半径 |
| `physicalDefense` / `magicDefense` | 对应攻击类型的减伤值 |
| `unitType` | `Ground`（沿路径走）或 `Flying`（直线飞向目标） |
| `attackRangeType` | `Melee`（近战，不能攻击飞行单位）或 `Ranged`（远程） |
| `attackType` | `Physical` 或 `Magic`（决定受哪种防御减免） |

3. 配置 `UnitAI` 的 `enemyBasePosition`（敌方防御塔坐标）
4. 特殊兵种可替换 `UnitCombat` 为 `ArcherCombat` / `MageCombat` / `HealerCombat`

### AI 行为优先级

1. 范围内有敌方单位 → 脱离驻扎 → 追击并攻击
2. 驻扎点范围内 → 驻扎/抢占判定
3. 资源点范围内 → 驻扎/抢占判定
4. 无敌人且路径未走完 → 沿路径前进
5. 路径已走完 → 锁定并攻击敌方防御塔

---

## 5. 波次系统

**核心文件:**
- `Assets/Level/CommonLevel/Wave/WaveData.cs` — 单个波次配置
- `Assets/Level/CommonLevel/Wave/WaveList.cs` — 波次列表（ScriptableObject）
- `Assets/Level/CommonLevel/Wave/WaveTriggerType.cs` — 触发类型枚举
- `Assets/Level/CommonLevel/Wave/WaveGenerator.cs` — 协程驱动的生成器

### 创建波次配置

1. 在 Project 窗口中创建 `WaveList` ScriptableObject 资产
2. 在 `waves` 数组中逐波配置：

| 参数 | 说明 |
|------|------|
| `unitPrefab` | 要生成的单位预制体 |
| `spawnPoint` | 出生点 Transform（为空则使用 WaveGenerator 的默认生成点） |
| `pathID` | 单位行走的路线 ID |
| `spawnCount` | 本波总生成数量 |
| `spawnInterval` | 每个单位之间的生成间隔（秒） |
| `triggerType` | 触发方式（见下方） |
| `delayBeforeStart` | 仅 `AfterPrevious` 模式生效，延迟秒数 |

### 触发类型说明

| 类型 | 行为 |
|------|------|
| `AfterPrevious` | 等上一波生成完毕 + 本波 `delayBeforeStart` 秒后开始 |
| `Concurrent` | 遍历到本波时立即开始，与前一波并发出怪 |
| `Manual` | 等待外部调用 `WaveGenerator.Continue()` 后开始 |
| `AllUnitsDead` | 等场上所有已生成单位全部死亡后开始 |

### 使用方式

`BattleManager` 在游戏开始时调用 `WaveGenerator.StartWave()`，传入默认生成点和波次列表。也可独立调用：

```csharp
waveGenerator.StartWave(defaultSpawnPoint, waveList);
waveGenerator.StopWave();   // 中断所有未开始的波次
waveGenerator.Continue();   // 继续下一个 Manual 波次
```

---

## 6. 战斗系统

**核心文件:**
- `Assets/Level/CommonLevel/Battle/BattleManager.cs` — 战斗核心（单例）
- `Assets/Level/CommonLevel/Battle/TowerBase.cs` — 防御塔组件

### BattleManager

| 参数 | 说明 |
|------|------|
| `waveGenerator` | 波次生成器引用 |
| `waveList` | 本关波次配置 |
| `playerTower` / `enemyTower` | 双塔对象引用 |
| `maxCost` | 费用上限 |
| `costAddSpeed` | 每秒费用回复量 |
| `nowCost` | 当前可用费用 |

`BattleManager` 统一管理扣费（`UseCost()`）、胜负判定、胜利/失败面板显示。当一方防御塔被摧毁时触发游戏结束。

### TowerBase

防御塔组件挂载在塔 GameObject 上：

| 参数 | 说明 |
|------|------|
| `camp` | 塔的阵营 |
| `hp` / `atk` / `atkRange` / `atkCD` | 基础属性 |
| `physicalDefense` / `magicDefense` | 防御减伤 |
| `hpBarPrefab` | 血条预制体 |

防御塔每帧搜索范围内敌方单位并攻击。死亡时通知 `BattleManager` 判定胜负。

---

## 7. 驻扎与资源点

### 资源点（ResourcePoint）

**文件:** `Assets/Level/CommonLevel/ResourcePoint/ResourcePoint.cs`
**管理器:** `Assets/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs`

场景中的固定位置，单位可在范围内驻扎以获得费用增益。

**设置方式：**
1. 在场景中创建 GameObject，挂载 `ResourcePoint` 组件
2. 配置 `garrisonRange`（驻扎范围）、`resourceBonus`（提供的费用增益）、`maxGarrison`（最大驻扎人数）
3. 启动时自动注册到 `ResourcePointManager`

**驻扎/争夺机制：**
- 无人占领 → 任何阵营的单位可驻扎
- 同阵营占领且未满员 → 可加入驻扎
- 敌方占领 → 触发抢占战斗（争夺状态），守方单位被唤醒投入战斗
- 争夺中一方全灭 → 胜方自动驻扎，无人占领则双方同归于尽

### 驻扎点（GarrisonPoint）

**文件:** `Assets/Level/CommonLevel/GarrisonPoint/GarrisonPoint.cs`
**管理器:** `Assets/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs`
**放置器:** `Assets/Level/CommonLevel/GarrisonPoint/GarrisonPointPlacer.cs`

动态创建的驻扎点，必须吸附在路径上，可指定对哪些阵营生效。

**设置方式：**
1. 场景中需要 `GarrisonPointManager` 单例（挂载 `garrisonPointPrefab`）
2. 场景中需要 `GarrisonPointPlacer` 组件用于玩家交互
3. 调用 `GarrisonPointPlacer.StartDeployPlayer()` / `StartDeployEnemy()` / `StartDeployBoth()` 进入放置模式
4. 鼠标悬停路径 → 左键确认放置，右键取消

**脚本 API：**
```csharp
// 在指定坐标创建驻扎点（自动吸附到最近路径）
GarrisonPoint gp = GarrisonPointManager.Instance.CreateGarrisonPoint(position, effectiveCamps);

// 在指定路径的指定位置创建
GarrisonPoint gp = GarrisonPointManager.Instance.CreateGarrisonPointOnPath(path, curveT, effectiveCamps);

// 移除驻扎点
GarrisonPointManager.Instance.RemoveGarrisonPoint(gp);

// 查询单位是否进入驻扎点范围
GarrisonPoint gp = GarrisonPointManager.Instance.GetGarrisonPointInRange(unitPos, unitCamp);
```

---

## 8. 存档系统

**文件:**
- `Assets/Save/SaveManager.cs` — 静态存档管理器
- `Assets/Save/GameSaveData.cs` — 存档数据模型
- `Assets/Save/LevelRecord.cs` — 单关卡记录

### 存档机制

- 3 个存档栏位（0~2），保存在 `Application.persistentDataPath` 下
- JSON 格式（`JsonUtility`），文件名 `gamesave_{slotIndex}.json`
- 自动记录保存时间和栏位索引

### 核心 API

```csharp
// 检查栏位是否有存档
bool exists = SaveManager.HasSave(slotIndex);

// 读取存档
GameSaveData data = SaveManager.LoadSave(slotIndex);

// 保存存档
SaveManager.SaveGame(data, slotIndex);

// 删除存档
SaveManager.DeleteSave(slotIndex);

// 创建新游戏数据（自动初始化所有关卡为未完成）
GameSaveData newData = SaveManager.CreateNewGame();

// 获取最新存档栏位
int latest = SaveManager.GetLatestSaveSlot();

// 标记关卡完成并自动推进
SaveManager.MarkLevelCompleted("Level_1", slotIndex);
```

### 新增关卡

在 `SaveManager.levelSceneOrder` 数组中追加关卡场景名即可：

```csharp
private static readonly string[] levelSceneOrder = { "Level_1", "Level_2", "Level_3" };
```

---

## 9. 对话系统

**文件:**
- `Assets/Dialogue/DialogueManager.cs` — 对话管理器（单例，`DontDestroyOnLoad` 跨场景持久化）
- `Assets/Dialogue/DialogueAsset.cs` — 对话行数据（ScriptableObject）
- `Assets/Dialogue/DialogueStarter.cs` — 触发对话的组件
- `Assets/Dialogue/NPCInteract.cs` — NPC 交互触发器

### 创建对话

1. 创建 `DialogueAsset` ScriptableObject，配置对话行（角色名、内容、头像）
2. 通过 `DialogueStarter` 在场景加载时自动触发，或通过 `NPCInteract` 让玩家点击触发
3. `DialogueManager` 以打字机效果逐字显示，支持 `textSpeed` 调节速度

### 触发对话

```csharp
DialogueManager.Instance.StartDialogue(dialogueAsset);
```

---

## 10. 主菜单与场景流程

**文件:**
- `Assets/Menu/MenuManager.cs` — 主菜单逻辑
- `Assets/Menu/SaveSlotPanel.cs` — 存档栏位选择面板
- `Assets/Scenes/GameStart/` — 启动场景（加载 MenuScene）
- `Assets/Scenes/MenuScene.unity` — 主菜单场景

### 菜单流程

```
GameStart (启动) → MenuScene (主菜单)
                    ├── 新游戏 → 选择栏位 → 创建新档 → Level_1
                    ├── 继续   → 自动找最新存档 → 进入对应关卡
                    └── 加载   → 选择栏位 → 加载存档 → 进入对应关卡
```

### 关卡推进

通关后 `BattleManager` 调用 `SaveManager.MarkLevelCompleted()`，自动解锁下一关。全部通关后返回主菜单。

---

## 全局单例一览

| 单例 | 文件 | 职责 |
|------|------|------|
| `BattleManager` | `Battle/BattleManager.cs` | 费用、波次启动、胜负判定 |
| `CardManager` | `Card/CardManager.cs` | 卡牌 UI 生成与选中管理 |
| `LevelPathManager` | `Path/LevelPathManager.cs` | 场景内路径注册与查询 |
| `ResourcePointManager` | `ResourcePoint/ResourcePointManager.cs` | 资源点注册与增益计算 |
| `GarrisonPointManager` | `GarrisonPoint/GarrisonPointManager.cs` | 驻扎点创建与查询 |
| `DialogueManager` | `Dialogue/DialogueManager.cs` | 对话播放（跨场景持久化） |
