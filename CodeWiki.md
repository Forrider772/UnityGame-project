# 卡牌塔防游戏 - Code Wiki

## 1. 项目概述

这是一个基于 Unity 2D 开发的卡牌塔防策略游戏（Unity 2022.3.62f3c1）。核心玩法融合卡牌召唤、单位沿路径移动、双塔攻防，并包含剧情（Fungus）、波次、资源点/驻扎点争夺、Buff、Boss 关、路径传送等机制。

**主要特性：**
- 卡牌系统：卡牌选择、部署、冷却机制、ToggleGroup 互斥选中
- 路径系统：多阵营路径管理、可视化、曲线（Catmull-Rom）、循环、**传送段**
- 波次系统：配置化敌人生成、多触发策略（AfterPrevious/Concurrent/Manual/AllUnitsDead）
- 单位系统：v2 策略组件模式（UnitBrain + IMoveStrategy + ICombatStrategy），5 组件组合
- 战斗系统：单位/塔属性、攻防结算、普通 & **Boss 胜利**判定、**牵制范围**机制
- Buff 系统：属性修饰层（攻击/生命/移速/攻速/防御倍率）
- 资源点/驻扎点：驻扎、占领、争夺机制
- 存档系统：多栏位 JSON 存档，关卡进度持久化
- 剧情系统：Fungus 对话 + 剧本导入工具
- 菜单系统：新游戏/继续/加载/设置完整流程

---

## 2. 项目结构

```text
Assets/
├── Game/                          # 核心游戏代码（按模块分）
│   ├── Level/                     # 关卡模块
│   │   ├── CommonLevel/           # 通用战斗模块（被各关复用）
│   │   │   ├── Battle/            # 战斗：BattleManager / TowerBase / BossUnit / TowerLeashZone / DamageCalculator
│   │   │   ├── Buff/              # Buff：BuffData（SO）/ BuffManager
│   │   │   ├── Camp/              # CampType.cs
│   │   │   ├── Card/              # 卡牌：CardData / CardManager / CardInteraction / CardDeploy / PlayerDeck
│   │   │   ├── GarrisonPoint/     # 驻扎点：Garrison（核心）/ GarrisonPoint / Manager / Placer
│   │   │   ├── ResourcePoint/     # 资源点：ResourcePoint / ResourcePointManager
│   │   │   ├── Path/              # 路径：PathManager(+Editor) / LevelPathManager / PathVisualManager / ConnectionType
│   │   │   ├── Wave/              # 波次：WaveGenerator / WaveData / WaveList / WaveTriggerType
│   │   │   ├── LevelSetup/        # 关卡统一配置入口（LevelSetup / InitialUnitPlacer / TowerOverrideConfig）
│   │   │   ├── UI/                # HPBar / GamePauseManager / GameSettingsManager
│   │   │   ├── Helper/            # Math2DHelper / PathMath / CatmullRomMath / RangeCircleDisplay / LeashCircleDisplay
│   │   │   └── DefaultConfig/     # 默认配置资产（Buff / 卡牌 / WaveList）
│   │   ├── Level_1~6/             # 各关卡：场景 + WaveList 资产
│   │   └── LevelTest/             # 测试关卡
│   ├── Menu/                      # 主菜单（MenuManager / SaveSlotPanel / MenuSettingsManager）
│   ├── Save/                      # 存档（SaveManager / GameSaveData）
│   ├── Story/                     # 剧情（StoryScene_Ch1/Ch2 + 剧本 txt）
│   └── Unit/                      # 单位系统
│       ├── Scripts/               # 单位脚本（按职责分）
│       │   ├── Combat/            # ICombatStrategy / Base/Melee/Ranged/Healer / Bullet
│       │   ├── Movement/          # IMoveStrategy / Base/Ground/Flight / MoveType
│       │   ├── Core/              # UnitAttr / UnitBrain / UnitState / UnitHelper / UnitVisual
│       │   └── UI/                # UnitUI / FloatNumber
│       ├── CardAssets/            # 8 张卡牌资产
│       ├── Editor/                # CardDataGenerator（一键生成卡牌数据）
│       ├── Effects/               # Bullet / Fireball / HealEffect 预制体
│       ├── GeneralUnit/           # 8 个单位预制体
│       └── SpecialUnit/           # Boss.prefab
├── ArtResources/                  # 美术资源（按类型分）
│   ├── Fonts/                     # 中文字体 + SDF
│   ├── GameMap/                   # 地图 Tilemap 资产
│   ├── Map/                       # 关卡背景图
│   ├── Portraits/                 # 对话立绘
│   ├── UI/                        # （空）
│   └── Unit Sprite/               # 单位贴图
├── Editor/                        # 自建编辑器工具（ScriptToFungusTool 剧本导入）
├── Deprecated/                    # 废弃代码（旧 Dialogue 对话系统）
├── Scenes/                        # 附加场景（GameStart / Test）
├── Fungus/                        # 第三方插件（剧情/对话）
└── TextMesh Pro/                  # 第三方插件
```

---

## 3. 核心架构

### 3.1 全局单例模式

项目使用全局单例模式管理核心系统：

| 单例类 | 职责 | 所在文件 |
|--------|------|----------|
| [BattleManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/BattleManager.cs) | 战斗核心管理、费用控制、胜负判定、通关剧情映射 | Game/Level/CommonLevel/Battle/BattleManager.cs |
| [BuffManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Buff/BuffManager.cs) | 战斗 Buff 注册与应用 | Game/Level/CommonLevel/Buff/BuffManager.cs |
| [CardManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Card/CardManager.cs) | 卡牌 UI 生成、选中管理 | Game/Level/CommonLevel/Card/CardManager.cs |
| [LevelPathManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Path/LevelPathManager.cs) | 场景路径收集与查询 | Game/Level/CommonLevel/Path/LevelPathManager.cs |
| [ResourcePointManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs) | 资源点注册与费用增益计算 | Game/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs |
| [GarrisonPointManager](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs) | 驻扎点创建与查询 | Game/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs |
| [TowerLeashZone](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/TowerLeashZone.cs) | Boss 关牵制范围检测（属性式单例） | Game/Level/CommonLevel/Battle/TowerLeashZone.cs |

> 旧的 `DialogueManager`（手写对话单例）已废弃，位于 `Assets/Deprecated/Dialogue/`，请勿使用；剧情一律用 Fungus。

### 3.2 静态管理器 / 工具

| 静态类 | 职责 | 所在文件 |
|--------|------|----------|
| SaveManager | 存档文件读写、多栏位管理 | Game/Save/SaveManager.cs |
| DamageCalculator | 伤害结算：`max(1, 伤害 - 对应防御)` | Game/Level/CommonLevel/Battle/DamageCalculator.cs |
| UnitHelper | 单位生成时注入 camp/layer | Game/Unit/Scripts/Core/UnitHelper.cs |
| MoveTypeHelper | 移动类型兼容性判断 | Game/Unit/Scripts/Movement/MoveType.cs |
| Math2DHelper / PathMath / CatmullRomMath | 几何 / 直线路径 / 曲线数学 | Game/Level/CommonLevel/Helper/ |

### 3.3 系统依赖关系图

```
BattleManager (核心控制)
    ├── WaveGenerator (敌人生成)
    │   └── LevelPathManager (路径查询)
    │       └── PathManager (单条路径，含传送段)
    ├── CardManager (卡牌管理)
    │   └── CardDeploy (卡牌部署 → 记录 deployCost → 注册 TowerLeashZone)
    ├── TowerBase (双塔攻防) ── BossUnit (Boss 死亡判胜)
    ├── TowerLeashZone (牵制范围，单例)
    ├── BuffManager (Buff 应用 → UnitAttr.Modified*)
    ├── ResourcePointManager (资源点管理) ── Garrison (驻扎核心)
    ├── GarrisonPointManager (驻扎点管理) ── Garrison
    ├── LevelSetup (关卡统一配置分发)
    └── SaveManager (胜利时自动存档 → LevelToStoryMap 剧情衔接)

MenuManager (菜单控制)
    ├── SaveSlotPanel (栏位选择UI) ── SaveManager
    ├── MenuSettingsManager (设置面板)
    └── 新游戏 → StoryScene_Ch1 (Fungus 剧情) → Level_1

单位系统 (v2 策略组件模式):
    UnitAttr (属性数据 + Buff 修饰层)
    UnitUI (血条)
    UnitBrain (状态机调度器 + 事件系统)
    ├── IMoveStrategy (移动策略)
    │   ├── GroundMoveStrategy / FlightMoveStrategy
    │   └── BaseMoveStrategy (两阶段移动 + 传送状态机)
    └── ICombatStrategy (战斗策略)
        ├── MeleeCombatStrategy / RangedCombatStrategy / HealerCombatStrategy
        └── BaseCombatStrategy (攻击节奏 + 统一伤害结算)
```

---

## 4. 核心模块详解

### 4.1 战斗系统 (Battle)

#### 4.1.1 BattleManager

**文件：** `Assets/Game/Level/CommonLevel/Battle/BattleManager.cs`

**职责：**
- 管理关卡流程与游戏状态
- 控制费用自动增长与使用（`UseCost` 扣费 / `AddCost` 加费，均不超过上限）
- 双塔存活状态管理
- 胜负判定与游戏结束面板
- 胜利时自动存档，并通过 `LevelToStoryMap` 判断是否先播剧情
- **Boss 胜利模式**：`useBossVictory=true` 时敌方塔摧毁不判胜，仅 BossUnit 死亡判胜

**核心方法：**

| 方法 | 功能 |
|------|------|
| `UseCost(int cost)` | 消耗费用，返回是否成功 |
| `AddCost(float amount)` | 增加费用（牵制死亡返还用），不超过上限 |
| `CheckWin()` | 胜负判定，由塔死亡时触发 |
| `OnBossDefeated()` | Boss 单位死亡时调用（由 `BossUnit` 触发），直接判胜 |
| `GameWin()` | 暂停时间 → MarkLevelCompleted 存档 → 判断剧情衔接 → 显示胜利面板 |
| `GameLose()` | 暂停时间 → 显示失败面板 |
| `OnWinNextLevel()` / `OnWinMainMenu()` | 胜利面板 — 下一关 / 主菜单 |
| `OnLoseRetry()` / `OnLoseMainMenu()` | 失败面板 — 重试 / 主菜单 |

**关键字段：**

```csharp
public float nowCost;              // 当前费用
public float maxCost = 10f;        // 费用上限
public float costAddSpeed = 1f;    // 费用增长速度
public bool playerTowerAlive;      // 玩家塔存活状态
public bool enemyTowerAlive;       // 敌方塔存活状态
[HideInInspector] public bool useBossVictory;  // Boss 胜利模式开关（LevelSetup 设置）
private static readonly Dictionary<string,string> LevelToStoryMap; // 通关剧情映射
// 当前映射：{ "Level_1" → "StoryScene_Ch2" }
public GameObject winPanel / losePanel;
```

#### 4.1.2 TowerBase

**文件：** `Assets/Game/Level/CommonLevel/Battle/TowerBase.cs`

**职责：**
- 防御塔血量、攻击、索敌
- 受到伤害与死亡处理
- 触发胜负判定

**核心方法：**

| 方法 | 功能 |
|------|------|
| `TakeDamage(float dmg, AttackType type)` | 承受伤害，区分物理/法术防御 |
| `FindEnemy()` | 搜索范围内最近敌人 |
| `AttackLogic()` | 攻击冷却与伤害结算 |
| `Die()` | 死亡逻辑，标记胜负 |

#### 4.1.3 BossUnit（Boss 标记）

**文件：** `Assets/Game/Level/CommonLevel/Battle/BossUnit.cs`

挂任意单位标记为 Boss，订阅 `UnitBrain.OnDeath`，死亡时调用 `BattleManager.Instance.OnBossDefeated()`。与 `BattleManager.useBossVictory` 配合实现 **Boss 死亡判胜**。

> ⚠️ 当前状态：`useBossVictory` 已在 Level_6 开启，但 `SpecialUnit/Boss.prefab` **尚未被任何波次/场景引用**，Boss 实际出场未接线（开发中）。

#### 4.1.4 TowerLeashZone（牵制范围）

**文件：** `Assets/Game/Level/CommonLevel/Battle/TowerLeashZone.cs`（单例）

挂在己方塔上。`CardDeploy` 部署单位后 `RegisterUnit(brain)` 注册；每 `checkInterval` 检测单位与塔距离，超出 `leashRange` → 返还 `deployCost × costRefundRatio` 费用（`BattleManager.AddCost`）→ `UnitBrain.Die()` 走正常死亡。传送中的单位跳过检测。

#### 4.1.5 DamageCalculator（伤害结算）

**文件：** `Assets/Game/Level/CommonLevel/Battle/DamageCalculator.cs`（静态）

```csharp
最终伤害 = Mathf.Max(1, 伤害 - 对应防御)
// 物理伤害减 physicalDefense，法术伤害减 magicDefense
```

---

### 4.2 卡牌系统 (Card)

**文件路径：** `Assets/Game/Level/CommonLevel/Card/`，卡牌资产在 `Assets/Game/Unit/CardAssets/`

#### 4.2.1 CardData（ScriptableObject）

| 字段 | 类型 | 说明 |
|------|------|------|
| `camp` | CampType | 卡牌所属阵营 |
| `cardID` | string | 卡牌唯一ID |
| `cardName` | string | 卡牌名称 |
| `cost` | int | 消耗费用 |
| `cooldown` | float | 冷却时间 |
| `icon` | Sprite | 卡牌图标 |
| `unitPrefab` | GameObject | 召唤的单位预制体 |

#### 4.2.2 CardManager（单例）

**职责：** 生成卡牌 UI、ToggleGroup 互斥选中、卡牌列表维护。

#### 4.2.3 CardInteraction

**职责：** 卡牌 UI 显示与交互、冷却计时与视觉反馈、选中/取消选中、费用不足提示。

#### 4.2.4 CardDeploy

**职责：** 卡牌部署流程控制、路线显示与高亮、单位生成与路径分配、记录 `deployCost` 并注册 `TowerLeashZone`。

**部署流程：**
1. 玩家选中卡牌 → 进入部署模式，显示同阵营路线
2. 鼠标悬停路线 → 通过 `PathManager.GetClosestPoint()` 检测（支持曲线/直线）
3. 左键点击 → 在路线起点生成单位，扣费，触发冷却
4. 右键点击 → 退出部署模式

---

### 4.3 路径系统 (Path)

**文件路径：** `Assets/Game/Level/CommonLevel/Path/`

#### 4.3.1 PathManager

**职责：**
- 单条路径数据管理（partial class，编辑器扩展在 `PathManager.Editor.cs`）
- 路径可视化控制（运行时 + 编辑器）
- 提供路径点查询接口
- **Centripetal Catmull-Rom 曲线支持**
- **传送段支持**（`ConnectionType.Walk/Teleport`）

**Inspector 配置字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `camp` | CampType | 所属阵营 |
| `pathId` | PathID | 路线唯一标识（Path_01~05）|
| `pathPoints` | List\<Transform\> | 路径点列表（子物体）|
| `useCurve` | bool | 启用曲线（默认 false = 直线）|
| `curveAlpha` | float | 曲线参数化指数（0.5=centripetal）|
| `curveSamples` | int | LineRenderer 采样精度 |
| `isLooping` | bool | 循环路径 |
| `connectionTypes` | List\<ConnectionType\> | 每段连接类型（Walk/Teleport）|
| `teleportTime` | float | 传送段等待时长 |

**公共 API：**

| 方法 | 功能 |
|------|------|
| `SetVisible(bool)` / `SetHighlight(bool)` | 路径可见/高亮 |
| `GetStartPoint()` | 路径起点坐标 |
| `GetWaypoints2D()` | 路径点（曲线模式返回密集采样）|
| `GetCurvePoint(float t)` | 归一化距离 → 曲线世界坐标 |
| `GetCurveTangent(float t)` | 归一化距离 → 切线方向 |
| `GetTotalArcLength()` | 路径总弧长 |
| `GetClosestPoint(Vector2)` | 点到路径最近点（Newton 迭代）|
| `GetSegmentAtProgress(float t)` | 当前所在路径段（修复传送死循环）|
| `GetConnectionType(int segment)` | 某段的连接类型 |

**曲线系统设计：**
- Centripetal Catmull-Rom（alpha=0.5），路径点间距不均时无尖角/回环
- 弧长用 Gauss-Legendre 5 点求积预计算；最近点用粗扫 + Newton 迭代
- 段内弧长→s 映射表（LUT）保证匀速运动

#### 4.3.2 传送段（ConnectionType.Teleport）

- 单位进入传送段：`BaseMoveStrategy.BeginTeleport()`（隐藏 + 关碰撞 + 隐藏血条）→ 段起点等待 `teleportTime` → 瞬移到段终点。
- **传送期间完全无敌**（`UnitBrain.TakeDamage` 忽略一切伤害），跳过索敌/驻扎检测。
- 编辑器可视化：传送段紫色虚线 + 段起点菱形 + "传送"标签。
- 已配置：`Level_5`（12 处）、`Level_6`（18 处）。

#### 4.3.3 LevelPathManager（单例）

**核心数据结构：**

```csharp
Dictionary<CampType, Dictionary<PathID, PathManager>> campPathDict
// 第一层：阵营 → 该阵营所有路线
// 第二层：路线ID → 具体路径对象
```

**查询接口：** `GetPath(camp, pathId)`、`GetAllPathsByCamp(camp)`

#### 4.3.4 驻扎 / 资源点

**驻扎核心 `Garrison`**（`GarrisonPoint/Garrison.cs`）：可复用的驻扎/占领/争夺逻辑，`ResourcePoint` 与 `GarrisonPoint` 都委托它。

- **资源点**（`ResourcePoint.cs`）：场景固定位置，单位驻扎获得费用增益；驻扎/争夺规则见 README 第 7 章。
- **驻扎点**（`GarrisonPoint.cs` + `GarrisonPointManager.cs` 单例 + `GarrisonPointPlacer.cs`）：动态创建、吸附路径、按阵营生效。

---

### 4.4 波次系统 (Wave)

**文件路径：** `Assets/Game/Level/CommonLevel/Wave/`

#### 4.4.1 WaveGenerator（单例）

按配置生成波次、分配单位路径、控制生成间隔。触发策略：`AfterPrevious / Concurrent / Manual / AllUnitsDead`，均支持 `delayBeforeStart`。

#### 4.4.2 WaveData

| 字段 | 说明 |
|------|------|
| `camp` | 单位所属阵营 |
| `pathID` | 行走路径ID |
| `unitPrefab` | 单位预制体 |
| `spawnCount` | 生成数量 |
| `spawnInterval` | 生成间隔 |
| `triggerType` | 触发类型 |
| `delayBeforeStart` | 触发后额外延迟 |

---

### 4.5 单位系统 (Unit) — v2 策略组件模式

> 单位系统 v1 的 `UnitAI`/`UnitCombat`/`UnitMovement`/`ArcherCombat`/`MageCombat`/`HealerCombat` 已废弃，移入 `Assets/Deprecated/`。
> 新架构采用 **策略组件模式**：`UnitBrain`（状态机） + `IMoveStrategy`（移动策略） + `ICombatStrategy`（战斗策略）。

**脚本目录：** `Assets/Game/Unit/Scripts/{Combat,Movement,Core,UI}/`
**预制体目录：** `Assets/Game/Unit/GeneralUnit/`（8 个）、`SpecialUnit/`（Boss）、`Effects/`（特效）

#### 4.5.1 架构概览

每个单位预制体由 **5 个 MonoBehaviour** 组合：

`UnitAttr → UnitUI → UnitBrain → [IMoveStrategy] → [ICombatStrategy]`

```
                    ┌─────────────┐
                    │  UnitBrain  │  纯状态机调度器 + 事件系统
                    │  (状态机)    │  不包含任何具体逻辑
                    └──┬───────┬──┘
                       │       │
              ┌────────▼─┐  ┌──▼──────────┐
              │IMoveStrategy│ │ICombatStrategy│
              │ SetPath()  │  │ DetectTarget()│
              │ Move()     │  │ TryExecute()  │
              │ MoveToward()│ │ TakeDamage()  │
              │ IsTeleporting│ │ SetTowerTarget│
              └──┬─────────┘ └──┬───────────┘
                 │              │
    ┌────────────▼──┐  ┌───────┴──────────┐
    │BaseMoveStrategy│  │BaseCombatStrategy│
    │ ├Ground/Flight │  │ ├Melee           │
    │ └传送状态机     │  │ ├Ranged          │
    └───────────────┘  │ └Healer          │
                       └──────────────────┘
```

#### 4.5.2 状态机

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

#### 4.5.3 UnitBrain（状态机调度器）

**文件：** `Assets/Game/Unit/Scripts/Core/UnitBrain.cs`

**职责：** 纯行为调度，不包含移动/战斗/驻扎逻辑。在 `Awake()` 中通过 `GetComponent<IMoveStrategy>()` 和 `GetComponent<ICombatStrategy>()` 获取策略组件。

**事件系统（供音效/特效/UI 挂载）：**

| 事件 | 触发时机 |
|------|----------|
| `OnStateChanged(UnitState, UnitState)` | 状态切换 (旧, 新) |
| `OnStateEnter` / `OnStateExit` | 进入/离开状态 |
| `OnDeath(GameObject)` | 死亡前 |
| `OnDamageTaken(float, AttackType)` | 受到伤害 |
| `OnAttackHit` / `OnMoveStart` / `OnPathComplete` | 攻击命中 / 开始移动 / 路径走完 |

**核心方法：**

| 方法 | 功能 |
|------|------|
| `TakeDamage(float dmg, AttackType type)` | 伤害入口；**传送中完全无敌**，委托 CombatStrategy 结算后刷新血条 |
| `SetPath(PathManager path)` | 委托给 MoveStrategy |
| `SetPathFromPosition(path, pos)` | 定位到最近点（初始布阵用）|
| `StopMovement()` / `ResumeMovement()` | 驻扎用 |
| `Die()` | 死亡序列（公开，牵制等外部系统可触发）：切 Dead → OnDeath → 淡出 0.3s → 销毁 |
| `IsTeleporting()` | 是否传送中（Boss 关牵制检测应跳过）|

#### 4.5.4 移动策略 (IMoveStrategy / BaseMoveStrategy)

**接口：** `Assets/Game/Unit/Scripts/Movement/IMoveStrategy.cs`

| 方法 | 功能 |
|------|------|
| `SetPath(PathManager)` | 绑定路径，缓存总弧长 |
| `Move(float dt, float speed)` | 沿路径匀速推进 |
| `MoveToward(Vector2 target, float speed)` | 战斗追击 / 向塔推进 |
| `Stop()` / `Resume()` | 暂停/恢复（驻扎用）|
| `IsPathCompleted()` | 路径是否走完 |
| `IsTeleporting { get; }` | 是否处于传送中 |

**抽象基类：** `BaseMoveStrategy` — 两阶段移动（寻路接近 + 路径跟随）、追击回归、**传送状态机**（`BeginTeleport` → 等待 → 瞬移）。

**实现：**

| 实现 | 文件 | 说明 |
|------|------|------|
| `GroundMoveStrategy` | `Movement/GroundMoveStrategy.cs` | 沿 PathManager 路径弧长推进（支持曲线/直线/循环/传送）|
| `FlightMoveStrategy` | `Movement/FlightMoveStrategy.cs` | 沿 Flight 类型路径推进，不旋转朝向 |

**MoveType**（`Movement/MoveType.cs`）：`Ground` / `Flying`（预留 `Water`）。`MoveTypeHelper.IsPathCompatible(pathType, unitType)`：地面单位只能走 Ground 路径，飞行单位可走 Ground+Flying。

#### 4.5.5 战斗策略 (ICombatStrategy / BaseCombatStrategy)

**接口：** `Assets/Game/Unit/Scripts/Combat/ICombatStrategy.cs`

**抽象基类：** `BaseCombatStrategy` — 攻击节奏（`Idle → Windup → Recovery → IdleWait`）、索敌缓存、统一伤害结算、`AttackPhase` 私有枚举。

**实现：**

| 实现 | 文件 | 说明 |
|------|------|------|
| `MeleeCombatStrategy` | `Combat/MeleeCombatStrategy.cs` | 索敌(按距离排序) + 追击 + 近战挥砍 + 防御结算 |
| `RangedCombatStrategy` | `Combat/RangedCombatStrategy.cs` | 索敌 + 追击 + 生成 Bullet 投射物，伤害类型从 `UnitAttr.attackType` 读取 |
| `HealerCombatStrategy` | `Combat/HealerCombatStrategy.cs` | 找血量%最低的友方 + 治疗。无目标时让状态机回 Moving/Advancing |

#### 4.5.6 各兵种组合

| 兵种 | MoveStrategy | CombatStrategy |
|------|-------------|----------------|
| soldier / hound / Mechs / Enemy | GroundMoveStrategy | MeleeCombatStrategy |
| FlyingUnit | **FlightMoveStrategy** | MeleeCombatStrategy |
| Archer / Mage | GroundMoveStrategy | RangedCombatStrategy |
| Healer | GroundMoveStrategy | HealerCombatStrategy |

#### 4.5.7 UnitAttr（属性数据）

**文件：** `Assets/Game/Unit/Scripts/Core/UnitAttr.cs`

**核心属性：**

```csharp
public CampType camp;                   // 所属阵营
public float maxHp = 120f;             // 最大生命
public float atk = 8f;                 // 攻击力
public float moveSpeed = 1.8f;         // 移动速度
public float atkRange = 1.2f;          // 攻击范围
public float atkCD = 1f;               // 攻击冷却
public float detectRange = 3f;         // 索敌范围
public AttackType attackType;          // 攻击类型 (Physical/Magic)
public float physicalDefense;          // 物理防御减伤
public float magicDefense;             // 法术防御减伤
public MoveType moveType;              // 移动类型 (Ground/Flying)
public AttackRangeType attackRangeType;// 攻击范围类型 (Melee/Ranged)
public int deployCost;                 // 部署费用（牵制死亡返还用，CardDeploy 设置）
```

**Buff 修饰层：** `atkMultiplier / maxHpMultiplier / moveSpeedMultiplier / atkSpeedMultiplier / physicalDefenseMultiplier / magicDefenseMultiplier`，对外提供 `ModifiedAtk / ModifiedMaxHp / ModifiedMoveSpeed / ModifiedAtkCD`（倍率累乘）。`BuffManager.ApplyBuffs()` 调用后触发 `SyncDebugDisplay()` 同步到 Inspector 调试区。

#### 4.5.8 UnitVisual / UnitUI

- **UnitVisual**（`Core/UnitVisual.cs`）：受击闪红视觉。
- **UnitUI**（`UI/UnitUI.cs`）：血条生成、刷新、销毁、`SetHpBarVisible()`（传送期间隐藏血条）。血条预制体由 `UnitAttr.hpBarPrefab` 指定，绑定 `BattleCanvas`。

#### 4.5.9 Bullet（投射物）

**文件：** `Assets/Game/Unit/Scripts/Combat/Bullet.cs`

- 检查目标存活状态（`currentHp <= 0`），不攻击尸体
- 伤害通过 `UnitBrain.TakeDamage()` 或 `TowerBase.TakeDamage()` 结算
- 伤害类型从发射方配置传递（`RangedCombatStrategy` 读取 `UnitAttr.attackType`）

---

### 4.6 存档系统 (Save)

**文件路径：** `Assets/Game/Save/`

#### 4.6.1 SaveManager（静态）

- 支持 3 个存档栏位（0~2），JSON 格式，路径 `Application.persistentDataPath/gamesave_{slotIndex}.json`
- `levelSceneOrder = { "Level_1", "Level_2" }`（当前仅两关可推进）

**核心方法：** `HasSave / LoadSave / SaveGame / DeleteSave / CreateNewGame / MarkLevelCompleted / HasAnySave / GetLatestSaveSlot`

#### 4.6.2 GameSaveData

```csharp
[System.Serializable]
public class GameSaveData
{
    public int saveVersion;            // 存档版本号
    public int slotIndex;              // 所属栏位
    public string saveTime;            // 存档时间
    public int currentLevelIndex;      // 当前关卡索引
    public string currentLevelScene;   // 当前关卡场景名
    public List<LevelRecord> levelRecords; // 关卡完成记录
}
```

---

### 4.7 菜单系统 (Menu)

**文件路径：** `Assets/Game/Menu/`

- **MenuManager**：主菜单核心逻辑。新游戏流程为：选择栏位 → 删旧档 → `CreateNewGame` → `SaveGame` → **加载 `StoryScene_Ch1`（第一章剧情）** → 剧情播完进 `Level_1`。继续游戏 / 加载存档直接读档进对应关卡。
- **SaveSlotPanel**：存档栏位选择 UI（NewGame / Load 两种模式、覆盖确认弹窗）。
- **MenuSettingsManager**：菜单设置（音量/全屏，PlayerPrefs 持久化）。
- **GameSettingsManager / GamePauseManager**：关卡内设置与暂停（打开设置时暂停游戏）。

---

### 4.8 剧情与对话 (Fungus)

> 旧的 `Assets/Dialogue/` 手写对话系统已废弃，移入 `Assets/Deprecated/Dialogue/`。剧情一律使用 **Fungus 4.3.4**。

**场景：**
- `Assets/Game/Story/StoryScene_Ch1.unity` — 第一章剧情（3 个 Block，结尾 LoadScene → `Level_1`）
- `Assets/Game/Story/StoryScene_Ch2.unity` — 第二章剧情（5 个 Block，结尾 LoadScene → `Level_2`）

**剧本导入工具：** `Assets/Editor/ScriptToFungusTool.cs`（菜单 `Tools → Fungus → 导入剧本文本…`）
- 规范格式：`## 幕标题`→Block、`@说话人`→Character、`【】`→Comment
- 自动匹配 `Assets/ArtResources/Portraits/Portrait_{角色名}.png` 立绘
- 幕间自动 Call 串联

**剧情接入：**
- 新游戏：`MenuManager.StartNewGame()` → `StoryScene_Ch1`
- 通关：`BattleManager.LevelToStoryMap`（`Level_1 → StoryScene_Ch2`）

---

### 4.9 Buff 系统 (Buff)

**文件路径：** `Assets/Game/Level/CommonLevel/Buff/`

- **BuffData**（ScriptableObject）：Buff 定义，含 `BuffStatType`（Attack/MaxHp/MoveSpeed/AttackSpeed/PhysicalDefense/MagicDefense）与 `BuffTargetCamp`（PlayerOnly/EnemyOnly/All）。
- **BuffManager**（战斗级单例，挂 BattleManager 同物体）：注册/应用 Buff，写入 `UnitAttr` 的 multiplier 修饰层。
- 关卡配置：`LevelSetup` 拖入 BuffData 资产，游戏开始时自动注册。

---

## 5. 枚举定义

| 枚举 | 文件路径 | 成员 |
|------|----------|------|
| `CampType` | Game/Level/CommonLevel/Camp/CampType.cs | Player, Enemy |
| `ConnectionType` | Game/Level/CommonLevel/Path/ConnectionType.cs | Walk, Teleport |
| `PathID` | Game/Level/CommonLevel/Path/PathID.cs | Path_01 ~ Path_05 |
| `WaveTriggerType` | Game/Level/CommonLevel/Wave/WaveTriggerType.cs | AfterPrevious, Concurrent, Manual, AllUnitsDead |
| `BuffStatType` | Game/Level/CommonLevel/Buff/BuffData.cs | Attack, MaxHp, MoveSpeed, AttackSpeed, PhysicalDefense, MagicDefense |
| `BuffTargetCamp` | Game/Level/CommonLevel/Buff/BuffData.cs | PlayerOnly, EnemyOnly, All |
| `MoveType` | Game/Unit/Scripts/Movement/MoveType.cs | Ground, Flying（预留 Water）|
| `AttackType` | Game/Unit/Scripts/Core/UnitAttr.cs | Physical, Magic |
| `AttackRangeType` | Game/Unit/Scripts/Core/UnitAttr.cs | Melee, Ranged |
| `UnitState` | Game/Unit/Scripts/Core/UnitState.cs | Moving, Advancing, Fighting, Garrisoned, AttackingTower, Dead |
| `PathVisualManager.EffectType` | Game/Level/CommonLevel/Path/PathVisualManager.cs | None, ColorChange, WidthPulse, ColorAndPulse |
| `BaseCombatStrategy.AttackPhase` | Game/Unit/Scripts/Combat/BaseCombatStrategy.cs | Idle, Windup, Recovery, IdleWait（嵌套私有）|
| `SaveSlotPanel.Mode` | Game/Menu/SaveSlotPanel.cs | NewGame, Load（嵌套）|

---

## 6. 工具类

### 6.1 数学工具（Game/Level/CommonLevel/Helper/）

| 类 | 职责 |
|------|------|
| `Math2DHelper` | 2D 几何：点-线段/点-折线最近距离 |
| `PathMath` | 直线路径数学（直线/闭环采样与切线）|
| `CatmullRomMath` | Centripetal Catmull-Rom 曲线数学（弧长表 / Gauss 求积）|
| `RangeCircleDisplay` | 运行时实心填充范围圈 |
| `LeashCircleDisplay` | 运行时空心圆范围圈（牵制范围可视化）|

### 6.2 DamageCalculator（Game/Level/CommonLevel/Battle/）

静态伤害结算：`max(1, 伤害 - 对应防御)`。

### 6.3 编辑器工具（Assets/Editor/ 与模块内 Editor/）

- `ScriptToFungusTool.cs` — 剧本 txt → Fungus Flowchart 导入工具
- `Game/Unit/Editor/CardDataGenerator.cs` — 一键生成所有兵种卡牌数据
- `Game/Level/CommonLevel/Path/PathManager.Editor.cs` — 路径 Gizmo + 右键复制/传送可视化
- `Game/Level/CommonLevel/Path/Editor/PathVisualMigration.cs` — 路径视觉子物体迁移工具

---

## 7. 游戏流程

### 7.1 菜单到关卡流程

1. **主菜单**
   - 新游戏 → 选择存档栏位 → **播第一章剧情（StoryScene_Ch1）** → 进入 Level_1
   - 继续游戏 → 自动加载最新存档 → 进入对应关卡
   - 加载存档 → 选择已有存档栏位 → 进入对应关卡

2. **关卡进行中**
   - 暂停面板：暂停/继续/重开/返回主菜单
   - 设置面板：音量/全屏/返回主菜单（打开时自动暂停）

3. **胜利**
   - 自动存档 + 标记关卡完成 → 若该关在 `LevelToStoryMap` 中（当前 `Level_1`）先播第二章剧情 → 显示胜利面板 → 下一关/主菜单

4. **失败**
   - 显示失败面板 → 重试/返回主菜单

### 7.2 完整战斗流程

1. **初始化阶段**
   - `LevelSetup`（最早执行）统一下发各配置 → `BattleManager` / `BuffManager` 初始化
   - `LevelPathManager` 收集场景路径
   - `CardManager` 生成玩家卡组卡牌 UI
2. **战斗开始**：`BattleManager` 启动 `WaveGenerator`，费用自动增长
3. **波次循环**：按配置生成敌人，沿指定路径（含传送段）向目标塔移动
4. **玩家操作**：选卡 → 部署 → 单位沿路径前进并战斗、驻扎、推进
5. **胜负判定**：双塔互攻；普通模式塔亡判胜，Boss 模式 BossUnit 死亡判胜；牵制机制下超出范围的己方单位死亡并返还费用

---

## 8. 项目配置

### 8.1 Unity 包依赖

根据 [Packages/manifest.json](file:///d:/project/My%20project/Packages/manifest.json)：
- `com.unity.feature.2d` - 2D 功能包
- `com.unity.textmeshpro` - 文本渲染
- `com.unity.ugui` - UI 系统
- `com.unity.modules.physics2d` - 2D 物理

### 8.2 标签与层

- `BattleCanvas` - 战斗 UI 画布标签
- `EnemyUnit` / `PlayerUnit` - 单位层

### 8.3 构建场景列表（Build Settings）

实际构建序：`MenuScene → StoryScene_Ch1 → Level_1 → StoryScene_Ch2 → Level_2`

> ⚠️ `Level_3~6`、`Test`、`GameStart` 未加入 Build Settings。`EditorBuildSettings.asset` 中的路径仍为旧路径（GUID 与新位置一致，Unity 打开会自动修正）。

---

## 9. 开发指南

### 9.1 新增卡牌

1. 创建 CardData 资产（右键 → Battle → Card Data），或用 `CardDataGenerator` 一键生成
2. 配置卡牌属性（费用、冷却、图标、单位预制体）
3. 将卡牌添加到 `LevelSetup.availableCards` 或 PlayerDeck 配置

### 9.2 新增路径

1. 在场景创建空物体，添加 PathManager 组件
2. 配置阵营、路径ID、路径点、曲线/循环
3. 如需传送：配置 `connectionTypes`（对应段设为 Teleport）+ `teleportTime`
4. `LevelPathManager` 自动收集

### 9.3 新增波次

1. 创建 WaveList 配置资产
2. 添加 WaveData 条目（阵营、路径、单位、数量、间隔、触发类型）
3. 将 WaveList 绑定到 `LevelSetup.waveList` 或 BattleManager

### 9.4 新增单位

1. 创建单位预制体，挂载：`UnitAttr` + `UnitUI` + `UnitBrain` + 一个 MoveStrategy + 一个 CombatStrategy
2. 配置 `UnitAttr` 数值（HP、攻击、速度、范围、阵营、`moveType`、`attackType` 等）
3. 远程单位配置 `RangedCombatStrategy.bulletPrefab/bulletSpeed`；治疗单位配置 `HealerCombatStrategy` 各参数
4. 关联到卡牌 `CardData.unitPrefab` 或波次 `WaveData.unitPrefab`；初始布阵挂 `InitialUnitPlacer`

### 9.5 新增关卡

1. 创建关卡场景，挂 `LevelSetup` 统一下发配置（费用/波次/双塔/卡组/资源点/驻扎点/Buff/牵制/Boss）
2. 添加到 Build Settings（场景序中）
3. 在 `SaveManager.levelSceneOrder` 数组追加场景名
4. 若通关需要插剧情：在 `BattleManager.LevelToStoryMap` 登记 `{关卡场景 → 剧情场景}`

### 9.6 新增剧情

1. 编写规范格式剧本（`## 幕标题` / `@说话人` / `【】`注释）
2. 用 `Tools → Fungus → 导入剧本文本…`（ScriptToFungusTool）生成 Flowchart 场景
3. 确保结尾 Fungus `LoadScene` 指向目标关卡
4. 在 `LevelToStoryMap` 登记接入点

### 9.7 Buff / 牵制 / Boss 配置

- **Buff**：创建 `BuffData` 资产 → 拖入 `LevelSetup` 的 Buff 配置分组。
- **牵制**：`LevelSetup` 开启 `overrideLeashConfig` → 设 `enableLeashZone / leashRange / leashCostRefundRatio`。
- **Boss 胜利**：`LevelSetup.useBossVictory=true`，并将含 `BossUnit` 组件的单位预制体加入波次配置。

---

## 10. 关键文件索引

| 文件 | 说明 |
|------|------|
| [BattleManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/BattleManager.cs) | 战斗核心管理器（费用/胜负/剧情映射）|
| [TowerBase.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/TowerBase.cs) | 防御塔基础脚本 |
| [BossUnit.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/BossUnit.cs) | Boss 标记（死亡判胜）|
| [TowerLeashZone.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/TowerLeashZone.cs) | Boss 关牵制范围（单例）|
| [DamageCalculator.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Battle/DamageCalculator.cs) | 伤害结算静态工具 |
| [BuffManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Buff/BuffManager.cs) | Buff 管理器（战斗级单例）|
| [BuffData.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Buff/BuffData.cs) | Buff 数据（SO）+ 枚举 |
| [LevelSetup.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/LevelSetup/LevelSetup.cs) | 关卡统一配置入口 |
| [CardManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Card/CardManager.cs) | 卡牌管理器 |
| [CardData.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Card/CardData.cs) | 卡牌数据 |
| [CardDeploy.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Card/CardDeploy.cs) | 卡牌部署系统 |
| [PathManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Path/PathManager.cs) | 单条路径管理（曲线/传送段）|
| [PathManager.Editor.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Path/PathManager.Editor.cs) | 路径编辑器扩展 |
| [LevelPathManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Path/LevelPathManager.cs) | 场景路径中心（单例）|
| [ConnectionType.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Path/ConnectionType.cs) | 路径段连接类型枚举 |
| [WaveGenerator.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Wave/WaveGenerator.cs) | 波次生成器 |
| [Garrison.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/GarrisonPoint/Garrison.cs) | 驻扎核心组件 |
| [GarrisonPointManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs) | 驻扎点管理器（单例）|
| [ResourcePointManager.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs) | 资源点管理器（单例）|
| [UnitBrain.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Core/UnitBrain.cs) | 单位状态机调度器 |
| [UnitAttr.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Core/UnitAttr.cs) | 单位属性数据 + Buff 修饰层 |
| [UnitState.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Core/UnitState.cs) | 单位状态枚举 |
| [BaseMoveStrategy.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Movement/BaseMoveStrategy.cs) | 移动抽象基类（传送状态机）|
| [MoveType.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Movement/MoveType.cs) | MoveType 枚举 + 兼容性判断 |
| [BaseCombatStrategy.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Combat/BaseCombatStrategy.cs) | 战斗抽象基类 |
| [Bullet.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/Combat/Bullet.cs) | 投射物 |
| [UnitUI.cs](file:///d:/project/My%20project/Assets/Game/Unit/Scripts/UI/UnitUI.cs) | 血条 UI |
| [ScriptToFungusTool.cs](file:///d:/project/My%20project/Assets/Editor/ScriptToFungusTool.cs) | 剧本 → Fungus 导入工具 |
| [CardDataGenerator.cs](file:///d:/project/My%20project/Assets/Game/Unit/Editor/CardDataGenerator.cs) | 一键生成卡牌数据 |
| [SaveManager.cs](file:///d:/project/My%20project/Assets/Game/Save/SaveManager.cs) | 存档管理器（静态）|
| [MenuManager.cs](file:///d:/project/My%20project/Assets/Game/Menu/MenuManager.cs) | 主菜单管理器 |
| [SaveSlotPanel.cs](file:///d:/project/My%20project/Assets/Game/Menu/SaveSlotPanel.cs) | 存档栏位选择面板 |

---

## 11. 扩展建议

1. **卡牌系统扩展**：特殊效果卡牌（AOE、治疗、Buff）、卡牌升级与组合、抽卡机制。
2. **单位系统扩展**：更多策略组件（AoECombatStrategy、BuffCombatStrategy）、技能系统、单位升级进化。
3. **路径系统扩展**：路径事件（陷阱、buff 点）、路径分支与选择。~~曲线路径~~（✅ 已实现 CR 曲线）、传送段（✅ 已实现）。
4. **存档系统扩展**：存档导入/导出、自动存档、云存档；扩展 `levelSceneOrder` 以支持更多关卡。
5. **UI/UX 优化**：关卡选择地图、战斗统计面板、部署手感优化。
6. **Boss 关补全**：将 `SpecialUnit/Boss.prefab` 接入波次配置，完成 Boss 出场接线（当前仅开关已配、出场未接）。
7. **FloatNumber 接入**：在攻击/治疗结算处 Instantiate 伤害飘字（当前为死代码）。
8. **音效系统**：AudioManager 统一管理 BGM/SFX（当前缺失）。

---

*文档更新时间：2026-08-07（同步到最新目录结构与新增功能）*
