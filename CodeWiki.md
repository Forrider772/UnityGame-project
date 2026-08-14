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
- **CSV 策划配置表**：Excel 改表 → 一键导入 → 自动写入单位 / 卡牌 / 波次 / Buff / 塔数值（LevelConfig 资产）
- **全局事件总线**：`GameEvents` 系统级事件，供音效等订阅方集中监听
- **音效系统**：`AudioManager` 双通道（BGM/SFX）独立音量 + 全事件音效

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
│   │   │   ├── LevelSetup/        # 关卡统一配置入口（LevelSetup / LevelConfig / BossLevelSetup / InitialUnitPlacer / TowerOverrideConfig）
│   │   │   ├── UI/                # HPBar / GamePauseManager / GameSettingsManager
│   │   │   ├── Helper/            # Math2DHelper / PathMath / CatmullRomMath / RangeCircleDisplay / LeashCircleDisplay / GameEvents
│   │   │   └── DefaultConfig/     # 默认配置资产（Buff / 卡牌 / WaveList）
│   │   ├── Level_1~6/             # 各关卡：场景 + WaveList 资产
│   │   └── LevelTest/             # 测试关卡
│   ├── Audio/                     # 音效系统（AudioManager / AudioConfig / AudioManager.prefab / AudioConfig.asset）
│   ├── CSV/                       # 策划配置表（Core/ 通用解析 + Editor/ 导入导出工具 + Tables/ 六张表）
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
├── MusicResources/                # 占位测试音效（3 个 mp3）
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
| [AudioManager](file:///d:/project/My%20project/Assets/Game/Audio/AudioManager.cs) | 音效管理器：BGM/SFX 双通道、跨场景保活、事件驱动 | Game/Audio/AudioManager.cs |

> 旧的 `DialogueManager`（手写对话单例）已废弃，位于 `Assets/Deprecated/Dialogue/`，请勿使用；剧情一律用 Fungus。

### 3.2 静态管理器 / 工具

| 静态类 | 职责 | 所在文件 |
|--------|------|----------|
| SaveManager | 存档文件读写、多栏位管理 | Game/Save/SaveManager.cs |
| DamageCalculator | 伤害结算：`max(1, 伤害 - 对应防御)` | Game/Level/CommonLevel/Battle/DamageCalculator.cs |
| UnitHelper | 单位生成时注入 camp/layer | Game/Unit/Scripts/Core/UnitHelper.cs |
| MoveTypeHelper | 移动类型兼容性判断 | Game/Unit/Scripts/Movement/MoveType.cs |
| Math2DHelper / PathMath / CatmullRomMath | 几何 / 直线路径 / 曲线数学 | Game/Level/CommonLevel/Helper/ |
| GameEvents | 全局事件总线（卡牌/波次/塔/胜负/子弹命中） | Game/Level/CommonLevel/Helper/GameEvents.cs |
| CsvReader / CsvTable | CSV 解析与类型化取值（Runtime 通用层） | Game/CSV/Core/ |

### 3.3 系统依赖关系图

```
BattleManager (核心控制)
    ├── WaveGenerator (敌人生成)
    │   └── LevelPathManager (路径查询)
    │       └── PathManager (单条路径，含传送段)
    ├── CardManager (卡牌管理)
    │   └── CardDeploy (卡牌部署 → 记录 deployCost → 注册 TowerLeashZone)
    ├── TowerBase (双塔攻防) ── BossUnit (Boss 死亡判胜)
    ├── TowerLeashZone (牵制范围，单例，由 BossLevelSetup 配置)
    ├── BuffManager (Buff 应用 → UnitAttr.Modified*)
    ├── ResourcePointManager (资源点管理) ── Garrison (驻扎核心)
    ├── GarrisonPointManager (驻扎点管理) ── Garrison
    ├── LevelSetup (关卡统一配置分发 ── 读取 LevelConfig 资产)
    └── SaveManager (胜利时自动存档 → LevelToStoryMap 剧情衔接)

GameEvents (全局事件总线)
    └── AudioManager (订阅系统事件 + UnitBrain 单位事件，驱动全游戏音效)

MenuManager (菜单控制)
    ├── SaveSlotPanel (栏位选择UI) ── SaveManager
    ├── MenuSettingsManager (设置面板：BGM/SFX 双通道音量)
    └── 新游戏 → StoryScene_Ch1 (Fungus 剧情) → Level_1

单位系统 (v2 策略组件模式):
    UnitAttr (属性数据 + Buff 修饰层)
    UnitUI (血条)
    UnitBrain (状态机调度器 + 事件系统，Awake/OnDestroy 自动注册/解绑 AudioManager)
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
| `OnBossDefeated()` | Boss 单位死亡时调用（由 `BossUnit` 触发），发 `OnBossDefeated` 事件后直接判胜 |
| `GameWin()` | 发 `OnGameWin` 事件 → 暂停时间 → MarkLevelCompleted 存档 → 判断剧情衔接 → 显示胜利面板 |
| `GameLose()` | 发 `OnGameLose` 事件 → 暂停时间 → 显示失败面板 |
| `OnWinNextLevel()` / `OnWinMainMenu()` | 胜利面板 — 下一关 / 主菜单 |
| `OnLoseRetry()` / `OnLoseMainMenu()` | 失败面板 — 重试 / 主菜单 |

**关键字段：**

```csharp
public float nowCost;              // 当前费用
public float maxCost = 10f;        // 费用上限
public float costAddSpeed = 1f;    // 费用增长速度
public bool playerTowerAlive;      // 玩家塔存活状态
public bool enemyTowerAlive;       // 敌方塔存活状态
[HideInInspector] public bool useBossVictory;  // Boss 胜利模式开关（BossLevelSetup 写入）
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
| `TakeDamage(float dmg, AttackType type)` | 承受伤害，区分物理/法术防御，发 `OnTowerDamaged` 事件 |
| `FindEnemy()` | 搜索范围内最近敌人 |
| `AttackLogic()` | 攻击冷却与伤害结算，发 `OnTowerAttack` 事件 |
| `Die()` | 死亡逻辑，发 `OnTowerDestroyed` 事件，标记胜负 |

#### 4.1.3 BossUnit（Boss 标记）

**文件：** `Assets/Game/Level/CommonLevel/Battle/BossUnit.cs`

挂任意单位标记为 Boss，订阅 `UnitBrain.OnDeath`，死亡时调用 `BattleManager.Instance.OnBossDefeated()`（先发 `GameEvents.OnBossDefeated`，供音效播 Boss 击破音）。与 `BattleManager.useBossVictory` 配合实现 **Boss 死亡判胜**。

> ⚠️ 当前状态：`useBossVictory` 开关已从 `LevelSetup` 拆出为 `BossLevelSetup` 组件，但 **`BossLevelSetup` 尚未挂载到任何场景**（Level_6 场景中的旧 `useBossVictory` 序列化残留已不生效）；`SpecialUnit/Boss.prefab` 含 `BossUnit` 组件但**未被任何波次/场景引用**。Boss 关机制整体未激活（开发中）。

#### 4.1.4 TowerLeashZone（牵制范围）

**文件：** `Assets/Game/Level/CommonLevel/Battle/TowerLeashZone.cs`（单例）

挂在己方塔上，参数由场景中的 `BossLevelSetup` 组件写入。`CardDeploy` 部署单位后 `RegisterUnit(brain)` 注册；每 `checkInterval` 检测单位与塔距离，超出 `leashRange` → 返还 `deployCost × costRefundRatio` 费用（`BattleManager.AddCost`）→ `UnitBrain.Die()` 走正常死亡。传送中的单位跳过检测。

> ⚠️ 当前状态：`TowerLeashZone` 已无任何场景 / prefab 实例，需挂到己方塔上并配 `BossLevelSetup` 才会生效。

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
| `OnAttackHit` | 近战攻击命中（`DealDamage`）|
| `OnRangedFire` / `OnHeal` | 远程开火（`NotifyRangedFire`）/ 治疗生效（`NotifyHeal`）|
| `OnMoveStart` / `OnPathComplete` | 开始移动 / 路径走完 |

> **音频注册**：`UnitBrain.Awake()` 自动 `AudioManager.Instance?.RegisterUnit(this)` 订阅攻击 / 受击 / 死亡音，`OnDestroy()` 解绑（无 AudioManager 时安全跳过）。

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
| `MeleeCombatStrategy` | `Combat/MeleeCombatStrategy.cs` | 索敌(按距离排序) + 追击 + 近战挥砍 + 防御结算；**仅地面近战跳过飞行单位，飞行近战可攻击任意目标含飞行**（修复飞行单位互攻 bug）|
| `RangedCombatStrategy` | `Combat/RangedCombatStrategy.cs` | 索敌 + 追击 + 生成 Bullet 投射物，伤害类型从 `UnitAttr.attackType` 读取；发射时 `NotifyRangedFire()` 发 `OnRangedFire` |
| `HealerCombatStrategy` | `Combat/HealerCombatStrategy.cs` | 找血量%最低的友方 + 治疗；生效时 `NotifyHeal()` 发 `OnHeal`。无目标时让状态机回 Moving/Advancing |

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

- **MenuManager**：主菜单核心逻辑。`Start()` 播放主菜单 BGM（`bgmMenu`）；新游戏流程为：选择栏位 → 删旧档 → `CreateNewGame` → `SaveGame` → **加载 `StoryScene_Ch1`（第一章剧情）** → 剧情播完进 `Level_1`。继续游戏 / 加载存档直接读档进对应关卡。
- **SaveSlotPanel**：存档栏位选择 UI（NewGame / Load 两种模式、覆盖确认弹窗），按钮绑定 `AudioManager.BindClick`。
- **MenuSettingsManager**：菜单设置（**BGM / SFX 双通道音量** + 全屏，音量持久化由 `AudioManager` 统一处理）。
- **GameSettingsManager / GamePauseManager**：关卡内设置与暂停（打开设置时暂停游戏），音量同为双通道滑块；暂停面板按钮绑定点击音。

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
- 关卡配置：`LevelSetup` 拖入 BuffData 资产，游戏开始时自动注册；或由 `BuffTable.csv` 经导入器写入 `LevelConfig.levelBuffs`。

---

### 4.10 CSV 策划配置表（CSV Config Table）

**文件路径：** `Assets/Game/CSV/`（Core / Editor / Tables 三层）

将策划数值从预制体 / 资产抽离到 CSV，策划用 Excel 改表 → Unity 一键导入 → 数值自动写入游戏。

#### 4.10.1 运行时解析层（Core/）

| 文件 | 职责 |
|------|------|
| `CsvReader.cs` | 静态 CSV 解析器：字节流 → 二维表格（含表头）。编码兼容 UTF-8 BOM / 严格 UTF-8 / **GBK 回退**（Excel 简体中文默认）；`#` 注释行与全空行过滤；引号 / 逗号 / 换行转义；文件被占用时抛异常提示关闭 Excel |
| `CsvTable.cs` | 表头映射 + 类型化取值（`GetInt / GetFloat / GetString`，InvariantCulture 避免 zh-CN 的 "1,5" 问题）+ `CsvWriterHelper`（字段转义、文件占用检测、UTF-8 BOM 写出）|

#### 4.10.2 编辑器工具（Editor/）

| 文件 | 职责 |
|------|------|
| `ConfigTableConst.cs` | 常量：CSV 路径 / 预制体目录 / 各表列映射（`UnitColumns` 等）；`LoadOrCreateLevelConfig()` 加载或创建 LevelConfig 资产；`FindAssetByFileName()` 按文件名解析资产引用；`BackupFiles()` 导入前备份到 `_backup/{时间戳}/` |
| `UnitTableImporter.cs` | UnitTable.csv → 改写 `GeneralUnit/*.prefab` 的 `UnitAttr`（`SerializedObject + FindProperty` 落盘 + `Undo` 支持 Ctrl+Z；整行解析，坏行不落盘）|
| `CardTableImporter.cs` | CardTable.csv → 更新 `CardAssets/*.asset` |
| `DeckTableImporter.cs` | DeckTable.csv → `LevelConfig.availableCards` |
| `BuffTableImporter.cs` | BuffTable.csv → `LevelConfig.levelBuffs` |
| `TowerTableImporter.cs` | TowerTable.csv → `LevelConfig.playerTowerOverride / enemyTowerOverride` |
| `WaveTableImporter.cs` | `WaveTable_Level_N.csv` → 重建 `WaveList_Level_N.asset` + 写入 `LevelConfig.waveList`；顺带迁移旧字段 `nextWaveInterval → delayBeforeStart`；`spawnPoint` 不进表（Unity 里手动拖）|
| `ConfigTableExporter.cs` | 反向导出 CSV 初版：从现有预制体 / 卡牌资产 / LevelConfig 生成六张表 |
| `ConfigTableMenu.cs` | 菜单 `Tools/策划表/`：一键导入全部、打开 CSV 文件夹 |

#### 4.10.3 LevelConfig 资产（LevelSetup/LevelConfig.cs）

每关一个 `Assets/Resources/Config/LevelConfig_Level_N.asset`（ScriptableObject），字段：

| 字段 | 来源 CSV | 运行时行为 |
|------|----------|------------|
| `levelIndex` / `levelName` | — | 关卡标识 |
| `availableCards` | DeckTable | 非空 → LevelSetup 启用 `overrideDeckConfig` |
| `levelBuffs` | BuffTable | 非空 → 注册到 BuffManager |
| `waveList` | WaveTable | 非空 → 启用 `overrideWaveConfig` |
| `playerTowerOverride` / `enemyTowerOverride` | TowerTable | 对应开关开启时应用塔覆盖 |

`LevelSetup.Awake()` 用 `Resources.Load<LevelConfig>("Config/LevelConfig_{场景名}")` 读取并填充自身字段；资产缺失则回退场景默认配置。

> **现状**：DeckTable / BuffTable 目前只有注释示例无数据行；`LevelConfig_Level_N.asset` 需在 Unity 执行导入后才生成（仓库中尚未生成）。

#### 4.10.4 工作流

1. Excel 打开 CSV 修改数值（**另存为「CSV UTF-8」**）
2. 保存并关闭文件
3. Unity 菜单 `Tools → 策划表 → 一键导入全部`（或单表导入），导入前自动备份，支持 Ctrl+Z 撤销

---

### 4.11 全局事件系统（GameEvents）

**文件：** `Assets/Game/Level/CommonLevel/Helper/GameEvents.cs`（静态类）

系统级事件总线，游戏逻辑只在关键位置发事件，不直接耦合订阅方（音效 / 特效 / 成就）。用 public 静态委托字段（非 `event` 关键字）允许外部直接 `Invoke`。

| 事件 | 触发点 |
|------|--------|
| `OnCardSelected` / `OnCardDeployed` | `CardInteraction` 选中卡牌 / 进入冷却 |
| `OnUnitSummoned` | `CardDeploy` 生成单位 |
| `OnWaveStart` / `OnEnemySpawned` | `WaveGenerator` 每波开始 / 生成首个敌人 |
| `OnTowerAttack` / `OnTowerDamaged` / `OnTowerDestroyed` | `TowerBase` 攻击 / 受击 / 摧毁 |
| `OnBulletHit` | `Bullet.HitTarget()` |
| `OnGameWin` / `OnGameLose` | `BattleManager.GameWin() / GameLose()` |
| `OnBossDefeated` | `BattleManager.OnBossDefeated()` |

---

### 4.12 音效系统（AudioManager）

**文件路径：** `Assets/Game/Audio/`

| 文件 | 职责 |
|------|------|
| `AudioManager.cs` | 单例，`DontDestroyOnLoad` 跨场景保活；BGM 循环源 + SFX 一次性源双通道，音量 `PlayerPrefs` 持久化（`AudioBGMVolume` / `AudioSFXVolume`，旧 `Volume` 键自动迁移）；集中订阅 `GameEvents` + `UnitBrain` 事件驱动音效；`BindClick()` 绑定 UI 点击音 |
| `AudioConfig.cs` | `AudioSlot` 枚举（18 个 SFX 槽）+ `AudioConfig` SO（`bgmMenu` / `bgmBattle` + 18 个 SFX 字段），`GetClip(slot)` 按槽取音频 |
| `AudioManager.prefab` / `AudioConfig.asset` | 预制体（拖入 `config` 引用）与配置资产 |

**关键机制：**

- **事件驱动**：`GameEvents.OnCardSelected / OnWaveStart / OnTowerAttack / OnGameWin / ...` 与单位事件 `OnAttackHit / OnRangedFire / OnHeal / OnDamageTaken / OnDeath` → 对应 `AudioSlot` 播放
- **单位注册**：`UnitBrain.Awake` 调 `AudioManager.Instance.RegisterUnit(brain)`（`HashSet` 去重防重复订阅），`OnDestroy` 解绑；受击音 0.15s 节流防叠音
- **Boss 死亡音去重**：Boss 单位死亡音由 `OnBossDefeated` 统一播放，单位死亡回调跳过 `BossUnit` 避免双响
- **挂载**：`AudioManager.prefab` 放入 `MenuScene` 与 `CommonLevel.prefab` 各一实例；`MenuManager.Start` 播 `bgmMenu`，`BattleManager.Start` 播 `bgmBattle`
- **双通道音量**：`MenuSettingsManager` / `GameSettingsManager` 均改为 BGM + SFX 双滑块，调 `SetBGMVolume / SetSFXVolume`（PlayerPrefs 持久化）

> **当前配置**：`bgmMenu`、`sfxUIClick`（Fungus Click）、`sfxCardSelect`（Fungus Click2）、`sfxAttackMelee`、`sfxHurt` 已配占位音频（`Assets/MusicResources/` 3 个 mp3 + Fungus 自带 wav）；`bgmBattle` 及其余 SFX 槽为空，对应事件静默 no-op。

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
| `AudioSlot` | Game/Audio/AudioConfig.cs | UIClick, CardSelect, CardDeploy, Summon, AttackMelee, AttackRanged, BulletHit, Heal, Hurt, Die, TowerAttack, TowerHurt, TowerDestroy, WaveStart, EnemySpawn, Win, Lose, BossDefeat |

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
- `Game/CSV/Editor/*` — **策划配置表工具**（`Tools/策划表/` 菜单）：导入 / 导出六张 CSV、一键导入全部、打开 CSV 文件夹（详见 4.10）

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
   - `LevelSetup`（最早执行）读取 `LevelConfig` 资产（若有）并统一下发各配置 → `BattleManager` / `BuffManager` 初始化
   - `LevelPathManager` 收集场景路径
   - `CardManager` 生成玩家卡组卡牌 UI
2. **战斗开始**：`BattleManager` 启动 `WaveGenerator`、播放战斗 BGM（`bgmBattle`），费用自动增长
3. **波次循环**：按配置生成敌人，沿指定路径（含传送段）向目标塔移动；`GameEvents.OnWaveStart / OnEnemySpawned` 触发音效
4. **玩家操作**：选卡 → 部署 → 单位沿路径前进并战斗、驻扎、推进；卡牌 / 召唤 / 攻击 / 受击等事件驱动音效
5. **胜负判定**：双塔互攻；普通模式塔亡判胜，Boss 模式 BossUnit 死亡判胜（需挂 `BossLevelSetup`）；牵制机制下超出范围的己方单位死亡并返还费用

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

实际构建序：`MenuScene → StoryScene_Ch1 → Level_1 → StoryScene_Ch2 → Level_2`（路径已更新为 `Assets/Game/...` 新位置）

> ⚠️ `Level_3~6`、`Test`、`GameStart` 仍未加入 Build Settings。

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

- **Buff**：创建 `BuffData` 资产 → 拖入 `LevelSetup` 的 Buff 配置分组；或写入 `BuffTable.csv` → 导入 → 进 `LevelConfig.levelBuffs`。
- **牵制**：把 `TowerLeashZone` 挂到己方塔上 → 场景中挂 `BossLevelSetup` → 开启 `overrideLeashConfig` → 设 `enableLeashZone / leashRange / leashCostRefundRatio`。
- **Boss 胜利**：场景中挂 `BossLevelSetup` 并开 `useBossVictory=true`，将含 `BossUnit` 组件的单位预制体加入波次配置。

### 9.8 CSV 策划配置表

1. 首次使用：菜单 `Tools → 策划表 → 导出 CSV 初版`，从现有资产生成六张 CSV 初版
2. 策划用 Excel 修改数值，**另存为「CSV UTF-8」**，保存后关闭文件
3. 菜单 `Tools → 策划表 → 一键导入全部`（或单表导入），弹窗提示成功 / 失败条数
4. 波次 / 牌组 / Buff / 塔四张表导入会生成 / 更新每关 `LevelConfig_Level_N.asset`（`Assets/Resources/Config/`），`LevelSetup` 运行时自动读取
5. 新增单位：先在 Unity 建好预制体，再在 UnitTable.csv 加一行（`unitID` = 预制体名）
6. 导入前自动备份到 `_backup/{时间戳}/`；出错可 Ctrl+Z 或从备份恢复

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
| [LevelConfig.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/LevelSetup/LevelConfig.cs) | 关卡配置资产（CSV 导入生成）|
| [BossLevelSetup.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/LevelSetup/BossLevelSetup.cs) | Boss 关独立配置（判胜 + 牵制）|
| [GameEvents.cs](file:///d:/project/My%20project/Assets/Game/Level/CommonLevel/Helper/GameEvents.cs) | 全局事件总线 |
| [AudioManager.cs](file:///d:/project/My%20project/Assets/Game/Audio/AudioManager.cs) | 音效管理器（单例）|
| [AudioConfig.cs](file:///d:/project/My%20project/Assets/Game/Audio/AudioConfig.cs) | 音效槽位配置（SO + AudioSlot 枚举）|
| [CsvReader.cs](file:///d:/project/My%20project/Assets/Game/CSV/Core/CsvReader.cs) | CSV 解析器（运行时）|
| [ConfigTableMenu.cs](file:///d:/project/My%20project/Assets/Game/CSV/Editor/ConfigTableMenu.cs) | 策划表工具菜单 |

---

## 11. 扩展建议

1. **卡牌系统扩展**：特殊效果卡牌（AOE、治疗、Buff）、卡牌升级与组合、抽卡机制。
2. **单位系统扩展**：更多策略组件（AoECombatStrategy、BuffCombatStrategy）、技能系统、单位升级进化。
3. **路径系统扩展**：路径事件（陷阱、buff 点）、路径分支与选择。~~曲线路径~~（✅ 已实现 CR 曲线）、传送段（✅ 已实现）。
4. **存档系统扩展**：存档导入/导出、自动存档、云存档；扩展 `levelSceneOrder` 以支持更多关卡。
5. **UI/UX 优化**：关卡选择地图、战斗统计面板、部署手感优化。
6. **Boss 关补全**：挂 `BossLevelSetup`（判胜 + 牵制）→ 把 `TowerLeashZone` 挂到己方塔 → 将含 `BossUnit` 的 `SpecialUnit/Boss.prefab` 接入波次配置（当前脚本已就绪、场景未接线）。
7. **FloatNumber 接入**：在攻击/治疗结算处 Instantiate 伤害飘字（当前为死代码）。
8. **音效资源补全**：✅ 音效系统已实现（AudioManager 双通道）；当前仅 5 个槽位配了占位音频，需补齐 `bgmBattle` 及其余 SFX 槽正式资源。
9. **配置表完善**：✅ CSV 策划表已实现；当前 DeckTable / BuffTable 无数据行，`LevelConfig` 资产尚未在 Unity 中生成，可继续扩展更多表（如关卡费用、资源点）。

---

*文档更新时间：2026-08-14（新增 CSV 策划配置表、GameEvents 事件系统、AudioManager 音效系统；更新 Boss 机制现状与飞行近战修复）*
