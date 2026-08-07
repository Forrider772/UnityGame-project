# My Project — 技术文档

2D 塔防策略游戏（Unity 2022.3.62f3c1），融合卡牌召唤、单位沿路径移动、双塔攻防、波次、资源点/驻扎点争夺、Buff、剧情（Fungus）、Boss 关与传送等机制。

> 代码按模块组织在 `Assets/Game/` 下；单位脚本按职责分 `Combat / Movement / Core / UI`；美术资源在 `Assets/ArtResources/`；废弃代码在 `Assets/Deprecated/`。更详细的架构与 API 见 `CodeWiki.md`。

---

## 1. 关卡统一配置（LevelSetup）

**文件:** `Assets/Game/Level/CommonLevel/LevelSetup/LevelSetup.cs`

每个关卡场景中挂载一个 `LevelSetup` 组件（`[DefaultExecutionOrder(-100)]`，最早执行），作为该关卡所有配置的统一入口，在 `Awake` 中自动分发到各子系统。

### Inspector 中的配置分组：

| 分组 | 功能 | 关键参数 |
|------|------|----------|
| 关卡基本信息 | 名称、索引、描述 | `levelName`, `levelIndex`, `levelDescription` |
| 战斗/费用 | 覆盖 `BattleManager` 的默认值 | `overrideBattleConfig`, `maxCost`, `costAddSpeed`, `startingCost` |
| 波次 | 绑定本关的波次配置 | `overrideWaveConfig`, `waveList` |
| 防御塔 | 拖入场景中的双塔对象 | `overrideTowerConfig`, `playerTower`, `enemyTower` |
| 塔属性覆盖 | 逐项覆盖塔的属性 | `overridePlayerTowerProps` / `overrideEnemyTowerProps` + `TowerOverrideConfig` |
| 卡组 | 替换玩家可用卡牌 | `overrideDeckConfig`, `availableCards`（CardData 数组）|
| 资源点 | 覆盖场景中所有 ResourcePoint 的默认值 | `overrideResourcePointConfig`, `resourcePointMaxGarrison`, `resourcePointRange`, `resourcePointBonus` |
| 驻扎点 | 写入 GarrisonPointManager 的默认值 | `overrideGarrisonConfig`, `garrisonPointMaxGarrison`, `garrisonPointRange` |
| 关卡 Buff | 游戏开始时自动注册到 BuffManager | 拖入 `BuffData` 资产（对匹配阵营的单位生效）|
| Boss 牵制范围 | 覆盖己方塔的 `TowerLeashZone` | `overrideLeashConfig`, `enableLeashZone`, `leashRange`, `leashCostRefundRatio` |
| Boss 单位胜利 | 替代敌方塔摧毁判定 | `useBossVictory`（开启后仅挂 `BossUnit` 的敌方单位死亡判胜，玩家塔被毁仍判负）|

> 每个分组都有独立的 `overrideXxx` 开关。关闭后该子系统使用自身的 Inspector 默认值，不受 LevelSetup 影响。

---

## 2. 路径系统

**核心文件:**
- `Assets/Game/Level/CommonLevel/Path/PathManager.cs` — 单条路径的数据与运行时逻辑（含传送段）
- `Assets/Game/Level/CommonLevel/Path/PathManager.Editor.cs` — 编辑器 Gizmo 绘制与右键工具
- `Assets/Game/Level/CommonLevel/Path/LevelPathManager.cs` — 场景级路径注册中心（单例）
- `Assets/Game/Level/CommonLevel/Path/PathVisualManager.cs` — LineRenderer 视觉效果
- `Assets/Game/Level/CommonLevel/Path/ConnectionType.cs` — 路径段连接类型枚举（`Walk` / `Teleport`）
- `Assets/Game/Level/CommonLevel/Helper/CatmullRomMath.cs` — Catmull-Rom 曲线数学库
- `Assets/Game/Level/CommonLevel/Helper/PathMath.cs` — 直线路径数学库
- `Assets/Game/Level/CommonLevel/Helper/Math2DHelper.cs` — 2D 几何工具

### 创建路径

1. 在场景中创建空 GameObject，挂载 `PathManager` 组件（会自动添加 `PathVisualManager`）
2. 设置 **阵营** (`CampType`) 和 **路线 ID** (`PathID`)
3. 在 `pathPoints` 列表中拖入路径节点（Transform 空物体），按经过顺序排列
4. 根据需要开启曲线模式、循环模式，或配置传送段

### 关键参数

| 参数 | 说明 |
|------|------|
| `camp` | 所属阵营，决定部署时哪些卡牌可以使用此路线 |
| `pathId` | 路线唯一标识（Path_01 ~ Path_05），波次配置中通过此 ID 绑定 |
| `useCurve` | 开启后使用 Centripetal Catmull-Rom 曲线插值（默认关闭，走直线折线）|
| `curveAlpha` | 曲线参数化指数，默认 0.5（centripetal）|
| `isLooping` | 开启后路径首尾相连形成闭环，单位到达终点后循环前进 |
| `connectionTypes` | 每段路径的连接类型列表（`Walk` 行走 / `Teleport` 传送）|
| `teleportTime` | 传送段中单位在段起点等待的时长，之后瞬移到段终点 |

### 路径节点间传送（ConnectionType.Teleport）

- `BaseMoveStrategy` 内置传送状态机：`BeginTeleport()`（单位隐藏 + 关闭碰撞 + 隐藏血条）→ 在段起点等待 `teleportTime` → 瞬移到段终点。
- **传送期间单位完全无敌**：`UnitBrain.TakeDamage()` 忽略一切伤害，索敌/驻扎检测全部跳过，血条隐藏。
- `PathManager.GetSegmentAtProgress()` 负责判断当前位于哪一段（修复了直线/曲线边界归属不一致导致的重复传送死循环）。
- 场景可视化：编辑器里传送段显示为**紫色虚线 + 段起点菱形 + "传送"标签**。
- **已配置关卡**：`Level_5`（12 处）、`Level_6`（18 处）；Level_1~4 未配置传送段。

### 编辑器工具

- 选中 PathManager 物体后，Scene 视图中绘制路径 Gizmo（曲线/直线/传送段各有样式）
- 右键 PathManager 组件标题 → "反转路径"（反转节点顺序）或 "交换阵营"（Player ↔ Enemy）
- `Path/Editor/PathVisualMigration.cs` — 把 PathVisualManager + LineRenderer 迁移到子物体的编辑器工具

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
- `Assets/Game/Level/CommonLevel/Card/CardData.cs` — 卡牌数据（ScriptableObject）
- `Assets/Game/Level/CommonLevel/Card/CardManager.cs` — 卡牌管理器（单例），生成 UI、ToggleGroup 互斥选中
- `Assets/Game/Level/CommonLevel/Card/CardInteraction.cs` — 单张卡牌的 UI 交互组件
- `Assets/Game/Level/CommonLevel/Card/CardDeploy.cs` — 部署系统，处理悬停、高亮、点击部署（记录 `deployCost` 供牵制返还）
- `Assets/Game/Level/CommonLevel/Card/PlayerDeck.cs` — 玩家卡组数据容器
- `Assets/Game/Unit/CardAssets/` — 8 张卡牌资产（Card_Archer / Card_Enemy / Card_FlyingUnit / Card_Healer / Card_Mage / Card_Mechs / Card_hound / Card_soldier）

### 创建卡牌

1. 在 Project 窗口中右键 → 创建 `CardData` ScriptableObject 资产
2. 配置卡牌属性：

| 参数 | 说明 |
|------|------|
| `camp` | 卡牌阵营，决定可在哪个阵营的路线上部署 |
| `cost` | 部署费用 |
| `cooldown` | 冷却时间（秒）|
| `cardIcon` | 卡牌 UI 图标 |
| `unitPrefab` | 部署时生成的单位预制体 |

> 可使用 `Assets/Game/Unit/Editor/CardDataGenerator.cs`（菜单 `Tools/生成所有兵种卡牌数据`）一键生成所有兵种卡牌。

### 玩家操作流程

1. **选中卡牌** — 点击卡牌 UI，进入部署模式
2. **悬停路线** — 鼠标移动到同阵营路线上方，路线高亮
3. **确认部署** — 左键点击高亮路线，在路径起点生成单位、扣费、卡牌进入冷却
4. **取消部署** — 右键退出部署模式

---

## 4. 单位系统

**核心文件**（`Assets/Game/Unit/Scripts/`，按职责分目录）：

**Combat/**（战斗策略）
- `Combat/ICombatStrategy.cs` — 战斗策略接口
- `Combat/BaseCombatStrategy.cs` — 战斗抽象基类：攻击节奏（Windup→Recovery→IdleWait）、索敌缓存、统一伤害结算
- `Combat/MeleeCombatStrategy.cs` — 近战（按距离索敌 + 跳过飞行单位）
- `Combat/RangedCombatStrategy.cs` — 远程投射物（生成 Bullet）
- `Combat/HealerCombatStrategy.cs` — 治疗（奶血量百分比最低友方）
- `Combat/Bullet.cs` — 通用投射物

**Movement/**（移动策略）
- `Movement/IMoveStrategy.cs` — 移动策略接口（含 `IsTeleporting`）
- `Movement/BaseMoveStrategy.cs` — 移动抽象基类：两阶段移动、追击回归、**传送状态机**
- `Movement/GroundMoveStrategy.cs` / `FlightMoveStrategy.cs` — 地面 / 飞行沿路径移动
- `Movement/MoveType.cs` — `MoveType` 枚举 + `MoveTypeHelper`（兼容性判断）

**Core/**（单位核心）
- `Core/UnitAttr.cs` — 属性数据 + Buff 修饰层 + `AttackType`/`AttackRangeType` 枚举
- `Core/UnitBrain.cs` — 状态机调度器（含事件系统）
- `Core/UnitState.cs` — 状态枚举
- `Core/UnitHelper.cs` — 生成时注入 camp/layer
- `Core/UnitVisual.cs` — 受击闪红视觉

**UI/**（界面表现）
- `UI/UnitUI.cs` — 血条生成/刷新/销毁/显隐
- `UI/FloatNumber.cs` — 漂浮数字（当前为未接入的死代码）

**预制体 / 数据：**
- `GeneralUnit/` — 8 个单位预制体（Archer / Enemy / FlyingUnit / Healer / Mage / Mechs / hound / soldier）
- `SpecialUnit/` — `Boss.prefab`（含 `BossUnit` 组件）
- `Effects/` — `Bullet.prefab`、`Fireball.prefab`、`HealEffect.prefab`

### 单位架构（策略组件模式）

每个单位预制体由 **5 个 MonoBehaviour** 组合而成：

`UnitAttr → UnitUI → UnitBrain → [MoveStrategy] → [CombatStrategy]`

| 组件 | 职责 |
|------|------|
| `UnitAttr` | 纯数据存储：HP、攻击、速度、射程、防御、阵营、移动类型、攻击类型；提供 Buff 修饰后的 `Modified*` 属性 |
| `UnitUI` | 血条生成、刷新、销毁、显隐 |
| `UnitBrain` | 纯状态机调度：按状态分支，委托 MoveStrategy/CombatStrategy；内置事件（OnStateChanged / OnDeath / OnDamageTaken 等）|
| `IMoveStrategy` | 移动策略：地面 / 飞行沿路径，含传送状态机 |
| `ICombatStrategy` | 战斗策略：近战 / 远程投射 / 治疗 |

### 各兵种组合

| 兵种 | MoveStrategy | CombatStrategy |
|------|-------------|----------------|
| soldier / hound / Mechs / Enemy | GroundMoveStrategy | MeleeCombatStrategy |
| FlyingUnit | **FlightMoveStrategy** | MeleeCombatStrategy |
| Archer / Mage | GroundMoveStrategy | RangedCombatStrategy |
| Healer | GroundMoveStrategy | HealerCombatStrategy |

### 状态机行为

`UnitState`：`Moving / Advancing / Fighting / Garrisoned / AttackingTower / Dead`

```
Moving ──┬── DetectTarget() 找到敌人 ──► Fighting
         ├── 进入驻扎点/资源点范围 ────► Garrisoned
         ├── 路径走完 ────────────────► Advancing
         └── hp ≤ 0 ──────────────────► Dead

Advancing ──┬── DetectTarget() 找到敌人 ─► Fighting
            ├── 塔在攻击范围 ────────────► AttackingTower
            ├── 塔太远 ─────────────────► 向塔推进（MoveToward）
            └── hp ≤ 0 ─────────────────► Dead

Fighting ──┬── DetectTarget() 无敌人 ─►（路径未完 → Moving，已完 → Advancing）
           └── hp ≤ 0 ────────────────► Dead

Garrisoned ──┬── DetectTarget() 找到敌人 → Fighting（离开驻扎）
             └── hp ≤ 0 ───────────────────► Dead

AttackingTower ──┬── 目标超出范围/丢失 ─► Advancing
                 └── hp ≤ 0 ────────────► Dead
```

> **传送中**：跳过索敌/驻扎检测，且 `TakeDamage` 完全无敌。

### 创建单位预制体

1. 创建 GameObject，挂载组件：`UnitAttr` + `UnitUI` + `UnitBrain` + 一个 MoveStrategy + 一个 CombatStrategy
2. 配置 `UnitAttr` 的关键属性：

| 参数 | 说明 |
|------|------|
| `camp` | 所属阵营 |
| `maxHp` / `atk` / `moveSpeed` / `atkRange` / `atkCD` | 基础战斗属性 |
| `detectRange` | 搜寻敌人半径 |
| `physicalDefense` / `magicDefense` | 对应攻击类型的减伤值 |
| `moveType` | `Ground`（沿路径走）或 `Flying`（沿 Flight 路径飞行）|
| `attackRangeType` | `Melee`（近战，不能攻击飞行单位）或 `Ranged`（远程）|
| `attackType` | `Physical` 或 `Magic`（决定受哪种防御减免）|

3. 远程兵种需在 `RangedCombatStrategy` 中配置 `bulletPrefab`、`bulletSpeed`
4. 治疗兵种需在 `HealerCombatStrategy` 中配置 `healRange`、`healAmount`、`healCooldown` 等
5. 关联到卡牌 `CardData.unitPrefab` 或波次 `WaveData.unitPrefab`；初始布阵可挂 `InitialUnitPlacer`（`LevelSetup/InitialUnitPlacer.cs`）

---

## 5. 波次系统

**核心文件:**
- `Assets/Game/Level/CommonLevel/Wave/WaveData.cs` — 单个波次配置
- `Assets/Game/Level/CommonLevel/Wave/WaveList.cs` — 波次列表（ScriptableObject）
- `Assets/Game/Level/CommonLevel/Wave/WaveTriggerType.cs` — 触发类型枚举
- `Assets/Game/Level/CommonLevel/Wave/WaveGenerator.cs` — 协程驱动的生成器

### 创建波次配置

1. 在 Project 窗口中创建 `WaveList` ScriptableObject 资产
2. 在 `waves` 数组中逐波配置：

| 参数 | 说明 |
|------|------|
| `unitPrefab` | 要生成的单位预制体 |
| `spawnPoint` | 出生点 Transform（为空则使用 WaveGenerator 的默认生成点）|
| `pathID` | 单位行走的路线 ID |
| `spawnCount` | 本波总生成数量 |
| `spawnInterval` | 每个单位之间的生成间隔（秒）|
| `triggerType` | 触发方式 |
| `delayBeforeStart` | 满足触发条件后的额外延迟秒数 |

### 触发类型

| 类型 | 行为 |
|------|------|
| `AfterPrevious` | 等上一波生成完毕后，经 `delayBeforeStart` 秒开始 |
| `Concurrent` | 遍历到本波时经 `delayBeforeStart` 秒后开始，与前一波并发出怪 |
| `Manual` | 等待外部调用 `WaveGenerator.Continue()` 后开始 |
| `AllUnitsDead` | 等场上所有已生成单位全部死亡后开始 |

```csharp
waveGenerator.StartWave(defaultSpawnPoint, waveList);
waveGenerator.StopWave();   // 中断所有未开始的波次
waveGenerator.Continue();   // 继续下一个 Manual 波次
```

---

## 6. 战斗系统

**核心文件**（`Assets/Game/Level/CommonLevel/Battle/`）:
- `BattleManager.cs` — 战斗核心（单例）：费用、胜负、面板、通关剧情映射
- `TowerBase.cs` — 防御塔组件
- `BossUnit.cs` — Boss 标记：死亡时调用 `BattleManager.OnBossDefeated()` 判胜
- `TowerLeashZone.cs` — Boss 关牵制范围（单例）
- `DamageCalculator.cs` — 伤害结算静态工具

### BattleManager

| 参数 | 说明 |
|------|------|
| `waveGenerator` / `waveList` | 波次生成器与配置 |
| `playerTower` / `enemyTower` | 双塔对象引用 |
| `maxCost` / `costAddSpeed` / `nowCost` | 费用体系 |
| `useBossVictory` | Boss 胜利模式开关（由 LevelSetup 写入）|

`BattleManager` 统一管理扣费（`UseCost()`）、加费（`AddCost()`，不超过上限，牵制返还用）、胜负判定、胜利/失败面板显示。

**胜负判定：**
- 普通模式：一方防御塔被摧毁即触发胜负。
- **Boss 胜利模式**（`useBossVictory=true`）：敌方塔被摧毁**不判胜**，仅挂有 `BossUnit` 的敌方单位死亡触发 `OnBossDefeated()` 判胜；玩家塔被摧毁仍判负。
- 胜利时若该关在 `LevelToStoryMap` 中有对应剧情场景，先播剧情再进下一关（当前映射：`Level_1 → StoryScene_Ch2`）。

### TowerBase

防御塔组件挂载在塔 GameObject 上：`camp` / `hp` / `atk` / `atkRange` / `atkCD` / 双防御 / `hpBarPrefab`。每帧搜索范围内敌方单位并攻击，死亡时通知 `BattleManager` 判定胜负。

### Boss 关牵制范围（TowerLeashZone）

挂载在己方塔上（单例）。`CardDeploy` 部署单位后调用 `RegisterUnit(brain)` 注册；每 `checkInterval` 检测单位与塔的距离，超出 `leashRange` 时：
1. 返还 `deployCost × costRefundRatio` 费用（`BattleManager.AddCost()`）
2. 调用 `UnitBrain.Die()` 走正常死亡流程
3. 传送中的单位跳过检测

范围圈可视化：`LeashCircleDisplay`（`Helper/`，空心圆，世界坐标精确）。

### 伤害结算（DamageCalculator）

静态工具类：`最终伤害 = max(1, 伤害 - 对应防御)`（物理伤害减 `physicalDefense`，法术减 `magicDefense`）。

---

## 7. 驻扎与资源点

**驻扎核心：** `Assets/Game/Level/CommonLevel/GarrisonPoint/Garrison.cs` — 可复用的驻扎/占领/争夺逻辑（`ResourcePoint` 与 `GarrisonPoint` 都委托它）。

### 资源点（ResourcePoint）

**文件:** `Assets/Game/Level/CommonLevel/ResourcePoint/ResourcePoint.cs`
**管理器:** `Assets/Game/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs`（场景单例）

场景中的固定位置，单位可在范围内驻扎以获得费用增益。

**驻扎/争夺机制：**
- 无人占领 → 任何阵营的单位可驻扎
- 同阵营占领且未满员 → 可加入驻扎
- 敌方占领 → 触发抢占战斗（争夺状态），守方单位被唤醒投入战斗
- 争夺中一方全灭 → 胜方自动驻扎

### 驻扎点（GarrisonPoint）

**文件:** `Assets/Game/Level/CommonLevel/GarrisonPoint/GarrisonPoint.cs`
**管理器:** `Assets/Game/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs`
**放置器:** `Assets/Game/Level/CommonLevel/GarrisonPoint/GarrisonPointPlacer.cs`

动态创建的驻扎点，吸附在路径上，可指定对哪些阵营生效。`GarrisonPointPlacer.StartDeployPlayer()/StartDeployEnemy()/StartDeployBoth()` 进入放置模式，鼠标悬停路径 → 左键确认放置，右键取消。

**脚本 API：**
```csharp
GarrisonPoint gp = GarrisonPointManager.Instance.CreateGarrisonPoint(position, effectiveCamps);
GarrisonPoint gp2 = GarrisonPointManager.Instance.CreateGarrisonPointOnPath(path, curveT, effectiveCamps);
GarrisonPointManager.Instance.RemoveGarrisonPoint(gp);
GarrisonPoint gp3 = GarrisonPointManager.Instance.GetGarrisonPointInRange(unitPos, unitCamp);
```

---

## 8. 存档系统

**文件:**
- `Assets/Game/Save/SaveManager.cs` — 静态存档管理器
- `Assets/Game/Save/GameSaveData.cs` — 存档数据模型

### 存档机制

- 3 个存档栏位（0~2），保存在 `Application.persistentDataPath` 下，JSON 格式，文件名 `gamesave_{slotIndex}.json`
- 自动记录保存时间和栏位索引

### 核心 API

```csharp
bool exists = SaveManager.HasSave(slotIndex);
GameSaveData data = SaveManager.LoadSave(slotIndex);
SaveManager.SaveGame(data, slotIndex);
SaveManager.DeleteSave(slotIndex);
GameSaveData newData = SaveManager.CreateNewGame();
int latest = SaveManager.GetLatestSaveSlot();
SaveManager.MarkLevelCompleted("Level_1", slotIndex);
```

### 关卡推进

`SaveManager.levelSceneOrder` 数组决定可推进的关卡顺序，**当前硬编码仅 `{ "Level_1", "Level_2" }`**——正常流程最多推进到第二关，`Level_3~6` 尚未加入推进链。

---

## 9. 剧情与对话（Fungus）

剧情系统基于 **Fungus 4.3.4** 插件实现。旧的 `Assets/Dialogue/` 手写对话系统已废弃，移入 `Assets/Deprecated/Dialogue/`，**请勿再使用**。

**相关文件:**
- `Assets/Game/Story/StoryScene_Ch1.unity` / `StoryScene_Ch2.unity` — 第一、二章剧情场景（内含 Fungus Flowchart，结尾用 LoadScene 进入下一关）
- `Assets/Game/Story/第一章_出征_正式剧本.txt` / `第二章_魔兵_正式剧本.txt` — 规范化剧本（`## 幕标题`、`@说话人`、`【】`注释 格式）
- `Assets/Game/Story/JuBen/` — 原始剧本
- `Assets/Editor/ScriptToFungusTool.cs` — 剧本导入工具（菜单 `Tools → Fungus → 导入剧本文本…`）：按规范格式自动生成 Block / Say / Character / Stage / Portrait，自动匹配 `Assets/ArtResources/Portraits/Portrait_{角色名}.png` 立绘
- `Assets/ArtResources/Portraits/` — 角色立绘（Portrait_A / 卡尔 / 村民甲 / 铁匠）

### 剧情接入点

| 时机 | 场景 |
|------|------|
| 新游戏 | `MenuManager.StartNewGame()` → 加载 `StoryScene_Ch1` → Level_1 |
| Level_1 通关 | `BattleManager.LevelToStoryMap`（`Level_1 → StoryScene_Ch2`）→ Level_2 |

> 新增剧情：编写规范格式剧本 → 用 `ScriptToFungusTool` 导入 → 在 `LevelToStoryMap` 登记对应关卡。

---

## 10. 主菜单与场景流程

**文件:**
- `Assets/Game/Menu/MenuManager.cs` — 主菜单逻辑（新游戏先播第一章剧情）
- `Assets/Game/Menu/SaveSlotPanel.cs` — 存档栏位选择面板（NewGame/Load 两种模式 + 覆盖确认弹窗）
- `Assets/Game/Menu/MenuSettingsManager.cs` — 菜单设置（音量/全屏，PlayerPrefs 持久化）
- `Assets/Game/Menu/MenuScene.unity` — 主菜单场景

### 菜单流程

```
MenuScene (主菜单)
    ├── 新游戏 → 选择栏位 → 建新档 → StoryScene_Ch1（第一章剧情）→ Level_1
    ├── 继续   → 自动找最新存档 → 进入对应关卡
    └── 加载   → 选择栏位 → 加载存档 → 进入对应关卡
```

### 场景构建序（Build Settings）

`MenuScene → StoryScene_Ch1 → Level_1 → StoryScene_Ch2 → Level_2`

> **注意**：`Level_3~6`、`Test`、`GameStart` 目前未加入 Build Settings。`Scenes/GameStart/UiStart.cs` 加载的是 `"Test"` 场景。

---

## 全局单例一览

| 单例 | 文件 | 职责 |
|------|------|------|
| `BattleManager` | `Battle/BattleManager.cs` | 费用、胜负判定、通关剧情映射 |
| `BuffManager` | `Buff/BuffManager.cs` | 战斗 Buff 注册与应用 |
| `CardManager` | `Card/CardManager.cs` | 卡牌 UI 生成与选中 |
| `LevelPathManager` | `Path/LevelPathManager.cs` | 场景内路径注册与查询 |
| `ResourcePointManager` | `ResourcePoint/ResourcePointManager.cs` | 资源点注册与增益计算 |
| `GarrisonPointManager` | `GarrisonPoint/GarrisonPointManager.cs` | 驻扎点创建与查询 |
| `TowerLeashZone` | `Battle/TowerLeashZone.cs` | Boss 关牵制范围检测 |

> 静态类（无 Instance）：`SaveManager`、`DamageCalculator`、`UnitHelper`、`MoveTypeHelper`、`Math2DHelper`、`PathMath`、`CatmullRomMath`。废弃的 `DialogueManager` 在 `Deprecated/Dialogue/`，请勿使用。
