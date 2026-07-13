# 卡牌塔防游戏 - Code Wiki

## 1. 项目概述

这是一个基于Unity 2D开发的卡牌塔防策略游戏。游戏核心玩法结合了卡牌召唤、单位沿路径移动、以及双塔攻防机制。

**主要特性：**
- 卡牌系统：支持卡牌选择、部署、冷却机制
- 路径系统：多阵营路径管理与可视化
- 波次系统：配置化敌人生成与路径分配
- 战斗系统：单位属性、攻防机制、胜负判定
- 存档系统：多栏位 JSON 存档，关卡进度持久化
- 菜单系统：新游戏/继续/加载/设置完整流程
- 对话系统：剧情对话展示

---

## 2. 项目结构

```json
Assets/
├── ArtResources/              # 美术资源
│   ├── Fonts/                # 字体文件
│   └── Deprecated/           # 弃用资源
├── Dialogue/                 # 对话模块
│   ├── DialogueAsset.cs      # 对话数据资产
│   ├── DialogueManager.cs    # 对话管理器
│   ├── DialogueStarter.cs    # 对话触发
│   └── NPCInteract.cs        # NPC交互
├── Level/                    # 关卡核心模块
│   └── CommonLevel/
│       ├── Battle/           # 战斗系统
│       │   ├── BattleManager.cs
│       │   └── TowerBase.cs
│       ├── Camp/             # 阵营定义
│       │   └── CampType.cs
│       ├── Card/             # 卡牌系统
│       │   ├── CardData.cs
│       │   ├── CardDeploy.cs
│       │   ├── CardInteraction.cs
│       │   ├── CardManager.cs
│       │   └── PlayerDeck.cs
│       ├── Helper/           # 工具类
│       │   ├── CatmullRomMath.cs
│       │   ├── Math2DHelper.cs
│       │   └── PathMath.cs
│       ├── Path/             # 路径系统
│       │   ├── LevelPathManager.cs
│       │   ├── PathID.cs
│       │   ├── PathManager.cs
│       │   ├── PathMoveType.cs     # 路径移动类型 + 兼容性校验
│       │   └── PathVisualManager.cs
│       ├── ResourcePoint/    # 资源点系统
│       │   ├── ResourcePoint.cs
│       │   └── ResourcePointManager.cs
│       ├── GarrisonPoint/    # 驻扎点系统
│       │   ├── GarrisonPoint.cs
│       │   ├── GarrisonPointManager.cs
│       │   └── GarrisonPointPlacer.cs
│       ├── UI/               # 战斗UI
│       │   ├── GamePauseManager.cs
│       │   ├── GameSettingsManager.cs
│       │   └── HPBar.cs
│       ├── LevelSetup/       # 关卡配置入口
│       │   └── LevelSetup.cs
│       └── Wave/             # 波次系统
│           ├── WaveData.cs
│           ├── WaveGenerator.cs
│           ├── WaveList.cs
│           └── WaveTriggerType.cs
├── Menu/                     # 主菜单模块
│   ├── MenuManager.cs        # 菜单核心逻辑
│   ├── MenuSettingsManager.cs # 菜单设置管理
│   └── SaveSlotPanel.cs      # 存档栏位选择面板
├── Save/                     # 存档模块
│   ├── GameSaveData.cs       # 存档数据结构
│   ├── LevelRecord.cs        # 关卡记录
│   └── SaveManager.cs        # 存档管理器（静态类）
├── Scenes/                   # 场景
│   ├── MenuScene.unity       # 主菜单场景
│   ├── Level_1.unity         # 关卡1场景
│   ├── TestLevelScene.unity  # 测试关卡场景
│   └── GameStart/
│       └── UiStart.cs
├── TextMesh Pro/             # TextMeshPro插件
├── Unit/                     # 单位系统
│   ├── GenericScript/        # 保留不变的组件
│   │   ├── UnitAttr.cs       # 单位属性数据
│   │   ├── UnitUI.cs         # 血条UI
│   │   └── FloatNumber.cs    # 浮动数字
│   ├── NewScript/            # v2 新架构（策略组件模式）
│   │   ├── UnitBrain.cs      # 状态机调度器
│   │   ├── UnitState.cs      # 状态枚举
│   │   ├── IMoveStrategy.cs  # 移动策略接口
│   │   ├── ICombatStrategy.cs # 战斗策略接口
│   │   ├── Bullet.cs         # 投射物（更新版）
│   │   ├── Movement/
│   │   │   ├── GroundMoveStrategy.cs   # 地面路径移动
│   │   │   └── FlightMoveStrategy.cs   # 飞行路径移动
│   │   └── Combat/
│   │       ├── MeleeCombatStrategy.cs  # 近战策略
│   │       ├── RangedCombatStrategy.cs # 远程射击策略
│   │       └── HealerCombatStrategy.cs # 治疗策略
│   └── Editor/
│       └── UnitPrefabMigrator.cs  # 预制体一键迁移工具
└── Deprecated/               # 弃用代码
    ├── UnitAI.cs             # 旧AI决策组件
    ├── UnitCombat.cs         # 旧战斗组件
    ├── UnitMovement.cs       # 旧移动组件
    ├── ArcherCombat.cs       # 旧射手特殊战斗
    ├── MageCombat.cs         # 旧法师特殊战斗
    ├── HealerCombat.cs       # 旧治疗特殊战斗
    ├── CardDeployManager.cs
    ├── CardItemUI.cs
    └── UnitBase.cs
```

---

## 3. 核心架构

### 3.1 全局单例模式

项目使用全局单例模式管理核心系统：

| 单例类 | 职责 | 所在文件 |
|--------|------|----------|
| [BattleManager](file:///d:/project/My%20project/Assets/Level/CommonLevel/Battle/BattleManager.cs) | 战斗核心管理、费用控制、胜负判定 | Battle/BattleManager.cs |
| [CardManager](file:///d:/project/My%20project/Assets/Level/CommonLevel/Card/CardManager.cs) | 卡牌UI生成、选中管理 | Card/CardManager.cs |
| [LevelPathManager](file:///d:/project/My%20project/Assets/Level/CommonLevel/Path/LevelPathManager.cs) | 场景路径收集与查询 | Path/LevelPathManager.cs |
| [WaveGenerator](file:///d:/project/My%20project/Assets/Level/CommonLevel/Wave/WaveGenerator.cs) | 波次生成与单位生成 | Wave/WaveGenerator.cs |
| [ResourcePointManager](file:///d:/project/My%20project/Assets/Level/CommonLevel/ResourcePoint/ResourcePointManager.cs) | 资源点注册与费用增益计算 | ResourcePoint/ResourcePointManager.cs |
| [GarrisonPointManager](file:///d:/project/My%20project/Assets/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs) | 驻扎点创建与查询 | GarrisonPoint/GarrisonPointManager.cs |
| [DialogueManager](file:///d:/project/My%20project/Assets/Dialogue/DialogueManager.cs) | 对话播放（跨场景持久化） | Dialogue/DialogueManager.cs |

### 3.2 静态管理器

| 静态类 | 职责 | 所在文件 |
|--------|------|----------|
| [SaveManager](file:///d:/project/My%20project/Assets/Save/SaveManager.cs) | 存档文件读写、多栏位管理 | Save/SaveManager.cs |

### 3.3 系统依赖关系图

```
BattleManager (核心控制)
    ├── WaveGenerator (敌人生成)
    │   └── LevelPathManager (路径查询)
    │       └── PathManager (单条路径)
    ├── CardManager (卡牌管理)
    │   └── CardDeploy (卡牌部署)
    │       └── LevelPathManager
    ├── TowerBase (双塔攻防)
    ├── ResourcePointManager (资源点管理)
    │   └── ResourcePoint (单个资源点)
    ├── GarrisonPointManager (驻扎点管理)
    │   └── GarrisonPointPlacer (驻扎点放置)
    └── SaveManager (胜利时自动存档)

MenuManager (菜单控制)
    ├── SaveSlotPanel (栏位选择UI)
    │   └── SaveManager (存档读写)
    ├── MenuSettingsManager (设置面板)
    └── GameSettingsManager (关卡内设置)

单位系统 (v2 策略组件模式):
    UnitAttr (属性数据)
    UnitBrain (状态机调度器)
    ├── IMoveStrategy (移动策略)
    │   ├── GroundMoveStrategy (地面沿路径)
    │   └── FlightMoveStrategy (飞行沿路径)
    └── ICombatStrategy (战斗策略)
        ├── MeleeCombatStrategy (近战)
        ├── RangedCombatStrategy (远程投射)
        └── HealerCombatStrategy (治疗)
```

---

## 4. 核心模块详解

### 4.1 战斗系统 (Battle)

#### 4.1.1 BattleManager

**职责：**
- 管理关卡流程与游戏状态
- 控制费用自动增长与使用
- 双塔存活状态管理
- 胜负判定与游戏结束面板
- 胜利时自动存档并推进关卡

**核心方法：**

| 方法 | 功能 |
|------|------|
| `UseCost(int cost)` | 消耗费用，返回是否成功 |
| `CheckWin()` | 胜负判定，由塔死亡时触发 |
| `GameWin()` | 暂停时间 → MarkLevelCompleted 存档 → 显示胜利面板 |
| `GameLose()` | 暂停时间 → 显示失败面板 |
| `OnWinNextLevel()` | 胜利面板 — 加载下一关 |
| `OnWinMainMenu()` | 胜利面板 — 返回主菜单 |
| `OnLoseRetry()` | 失败面板 — 重新加载当前关卡 |
| `OnLoseMainMenu()` | 失败面板 — 返回主菜单 |

**关键字段：**

```csharp
public float nowCost;              // 当前费用
public float maxCost = 10f;        // 费用上限
public float costAddSpeed = 1f;    // 费用增长速度
public bool playerTowerAlive;      // 玩家塔存活状态
public bool enemyTowerAlive;       // 敌方塔存活状态
// 胜利/失败面板
public GameObject winPanel / losePanel;
```

#### 4.1.2 TowerBase

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

---

### 4.2 卡牌系统 (Card)

#### 4.2.1 CardData

**职责：** 卡牌数据配置容器（ScriptableObject）

**字段说明：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `camp` | CampType | 卡牌所属阵营 |
| `cardID` | string | 卡牌唯一ID |
| `cardName` | string | 卡牌名称 |
| `cost` | int | 消耗费用 |
| `cooldown` | float | 冷却时间 |
| `icon` | Sprite | 卡牌图标 |
| `unitPrefab` | GameObject | 召唤的单位预制体 |

#### 4.2.2 CardManager

**职责：**
- 生成卡牌UI
- 管理卡牌选中状态
- 卡牌列表维护

**核心方法：**

| 方法 | 功能 |
|------|------|
| `RefreshAllCards()` | 刷新所有卡牌UI |
| `OnCardClicked(CardInteraction)` | 处理卡牌点击 |
| `DeselectCurrentCard()` | 取消选中卡牌 |

#### 4.2.3 CardInteraction

**职责：**
- 卡牌UI显示与交互
- 冷却计时与视觉反馈
- 选中/取消选中状态管理

**核心方法：**

| 方法 | 功能 |
|------|------|
| `Init(CardData data)` | 初始化卡牌数据 |
| `Select()` / `Deselect()` | 设置选中/取消选中 |
| `StartCooldown()` | 开始冷却计时 |

#### 4.2.4 CardDeploy

**职责：**
- 卡牌部署流程控制
- 路线显示与高亮
- 单位生成与路径分配

**部署流程：**
1. 玩家选中卡牌 → 进入部署模式，显示同阵营路线
2. 鼠标悬停路线 → 通过 `PathManager.GetClosestPoint()` 检测（支持曲线/直线）
3. 左键点击 → 在路线起点生成单位，扣费，触发冷却
4. 右键点击 → 退出部署模式

---

### 4.3 路径系统 (Path)

#### 4.3.1 PathManager

**职责：**
- 单条路径数据管理
- 路径可视化控制（运行时 + 编辑器）
- 提供路径点查询接口
- **Centripetal Catmull-Rom 曲线支持**

**Inspector 配置字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `camp` | CampType | 所属阵营 |
| `pathId` | PathID | 路线唯一标识 |
| `pathPoints` | List\<Transform\> | 路径点列表（子物体） |
| `useCurve` | bool | 启用曲线（默认 false = 直线） |
| `curveAlpha` | float | 曲线参数化指数（0.5=centripetal） |
| `curveSamples` | int | LineRenderer 采样精度（默认15） |

**公共 API：**

| 方法 | 功能 |
|------|------|
| `SetVisible(bool)` | 设置路线是否可见 |
| `SetHighlight(bool)` | 设置路线高亮状态 |
| `GetStartPoint()` | 获取路线起点坐标 |
| `GetWaypoints2D()` | 获取路径点（曲线模式返回密集采样） |
| `GetCurvePoint(float t)` | 归一化距离 [0,1] → 曲线上的世界坐标 |
| `GetCurveTangent(float t)` | 归一化距离 → 切线方向 |
| `GetTotalArcLength()` | 路径总弧长 |
| `GetClosestPoint(Vector2)` | 点到路径最近点（Newton 迭代，不采样） |

**编辑器功能：**
- `[SelectionBase]`：点击子路径点自动选中父级 PathManager
- **选中高亮**：金黄粗线 + 实心方向箭头 + 路径标签
- **未选中**：半透明蓝细线 + 空心方向箭头
- **右键菜单**：复制路径（反向/切换阵营/两者组合）

**曲线系统设计：**
- 采用 Centripetal Catmull-Rom（alpha=0.5），路径点间距不均时无尖角/回环
- 弧长用 Gauss-Legendre 5 点求积预计算
- 最近点用粗扫 + Newton 迭代（每段 7 次求值，远少于密集采样）
- 段内弧长→s 映射表（LUT）保证匀速运动

#### 4.3.2 LevelPathManager

**职责：**
- 场景内所有路径收集
- 按阵营+路线ID查询路径

**核心数据结构：**

```csharp
Dictionary<CampType, Dictionary<PathID, PathManager>> campPathDict
// 第一层：阵营 → 该阵营所有路线
// 第二层：路线ID → 具体路径对象
```

**查询接口：**

| 方法 | 功能 |
|------|------|
| `GetPath(CampType camp, PathID pathId)` | 获取指定路径 |
| `GetAllPathsByCamp(CampType camp)` | 获取阵营所有路径 |

#### 4.3.3 PathVisualManager

**职责：**
- 运行时路径 LineRenderer 视觉效果
- 支持颜色切换、脉冲宽度动画

**配置字段：**

| 字段 | 说明 |
|------|------|
| `effect` | 效果类型（ColorChange/WidthPulse/ColorAndPulse） |
| `normalColor` / `highlightColor` | 普通/高亮颜色 |
| `normalWidth` / `highlightWidth` | 普通/高亮线宽 |
| `pulseSpeed` | 脉冲动画速度 |

#### 4.3.4 驻扎点 (GarrisonPoint)

**GarrisonPoint** — 动态创建的驻扎点组件，可吸附到路径上。

**配置字段：**

| 字段 | 说明 |
|------|------|
| `effectiveCamps` | 对哪些阵营生效（可多选） |
| `garrisonRange` | 驻扎范围半径 |
| `maxGarrison` | 最大驻扎人数 |
| `boundPath` | 绑定的路径（吸附目标） |
| `boundCurveT` | 归一化曲线距离 [0,1]（支持曲线定位） |

**GarrisonPointManager** — 场景级单例，管理驻扎点创建/查询/删除。

**核心方法：**

| 方法 | 功能 |
|------|------|
| `CreateGarrisonPoint(pos, camps)` | 在坐标创建驻扎点（自动吸附最近路径） |
| `CreateGarrisonPointOnPath(path, curveT, camps)` | 在路径指定位置创建驻扎点 |
| `GetNearestGarrisonPoint(pos, camp)` | 获取对阵营生效的最近驻扎点 |
| `RemoveGarrisonPoint(gp)` | 移除驻扎点 |

**GarrisonPointPlacer** — 驻扎点放置交互（鼠标悬停→点击创建）。

---

### 4.4 波次系统 (Wave)

#### 4.4.1 WaveGenerator

**职责：**
- 按配置生成波次
- 分配单位路径
- 控制生成间隔

**生成流程：**
1. 读取波次列表配置
2. 按阵营和路径ID获取真实路径
3. 生成单位并设置路径
4. 波次间固定间隔2秒

#### 4.4.2 WaveData

**配置字段：**

| 字段 | 说明 |
|------|------|
| `camp` | 单位所属阵营 |
| `pathID` | 行走路径ID |
| `unitPrefab` | 单位预制体 |
| `spawnCount` | 生成数量 |
| `spawnInterval` | 生成间隔 |

---

### 4.5 单位系统 (Unit) — v2 策略组件模式

> 单位系统 v1 的 `UnitAI`/`UnitCombat`/`UnitMovement`/`ArcherCombat`/`MageCombat`/`HealerCombat` 已废弃，移入 `Assets/Deprecated/`。
> 新架构采用 **策略组件模式**：`UnitBrain`（状态机） + `IMoveStrategy`（移动策略） + `ICombatStrategy`（战斗策略）。

#### 4.5.1 架构概览

每个单位预制体由 **5 个 MonoBehaviour** 组合：

`UnitAttr → UnitUI → UnitBrain → [IMoveStrategy] → [ICombatStrategy]`

```
                    ┌─────────────┐
                    │  UnitBrain  │  纯状态机调度器
                    │  (状态机)    │  不包含任何具体逻辑
                    └──┬───────┬──┘
                       │       │
              ┌────────▼─┐  ┌──▼──────────┐
              │IMoveStrategy│ │ICombatStrategy│
              │ SetPath()  │  │ DetectTarget()│
              │ Move()     │  │ TryExecute()  │
              │ Stop/Resume│  │ SetTowerTarget│
              │ MoveToward()│ │ TakeDamage() │
              └──┬─────────┘ └──┬───────────┘
                 │              │
    ┌────────────▼──┐  ┌───────┴──────────┐
    │GroundMoveStrat │  │MeleeCombatStrat  │
    │FlightMoveStrat │  │RangedCombatStrat │
    └───────────────┘  │HealerCombatStrat │
                       └──────────────────┘
```

#### 4.5.2 状态机

```
Moving ──┬── DetectTarget() 找到敌人 ──► Fighting
         ├── 进入驻扎点/资源点范围 ────► Garrisoned
         ├── path 走完 ───────────────► AttackingTower
         └── hp ≤ 0 ──────────────────► Dead

Fighting ──┬── DetectTarget() 无敌人 ─► Moving
           └── hp ≤ 0 ─────────────────► Dead

Garrisoned ──┬── DetectTarget() 找到敌人 → Fighting（离开驻扎）
             └── hp ≤ 0 ───────────────────► Dead

AttackingTower ──┬── tower 被毁 ───► Moving
                 └── hp ≤ 0 ────────► Dead
```

#### 4.5.3 UnitBrain（状态机调度器）

**文件：** `Assets/Unit/NewScript/UnitBrain.cs`

**职责：** 纯行为调度，不包含移动/战斗/驻扎逻辑。在 `Awake()` 中通过 `GetComponent<IMoveStrategy>()` 和 `GetComponent<ICombatStrategy>()` 获取策略组件。

**核心方法：**

| 方法 | 功能 |
|------|------|
| `TakeDamage(float dmg, AttackType type)` | 外部调用入口，委托给 CombatStrategy 结算减伤后刷新血条 |
| `SetPath(PathManager path)` | 委托给 MoveStrategy |
| `StopMovement()` / `ResumeMovement()` | 委托给 MoveStrategy（驻扎用） |

#### 4.5.4 移动策略 (IMoveStrategy)

**接口定义：** `Assets/Unit/NewScript/IMoveStrategy.cs`

| 方法 | 功能 |
|------|------|
| `SetPath(PathManager)` | 绑定路径，缓存总弧长 |
| `Move(float dt, float speed)` | 沿路径匀速推进（弧长参数化） |
| `Stop()` / `Resume()` | 暂停/恢复（驻扎用） |
| `IsPathCompleted()` | 路径是否走完（循环路径永远 false） |
| `MoveToward(Vector2 target, float speed)` | 战斗追击移动 |

**实现：**

| 实现 | 文件 | 说明 |
|------|------|------|
| `GroundMoveStrategy` | `Movement/GroundMoveStrategy.cs` | 沿 PathManager 路径弧长推进（支持曲线/直线/循环） |
| `FlightMoveStrategy` | `Movement/FlightMoveStrategy.cs` | 沿 Flight 类型路径推进，不旋转朝向 |

#### 4.5.5 战斗策略 (ICombatStrategy)

**接口定义：** `Assets/Unit/NewScript/ICombatStrategy.cs`

| 方法 | 功能 |
|------|------|
| `DetectTarget(UnitAttr, Vector2)` | 在 detectRange 内搜寻最近敌人，近战跳过飞行单位 |
| `TryExecute(Transform, dt, ...)` | 执行攻击/治疗行动 |
| `SetTowerTarget(UnitAttr)` | 路径走完后锁定敌方防御塔 |
| `TakeDamage(damage, type, attr, onDie)` | 结算防御减伤、扣血、死亡回调 |

**实现：**

| 实现 | 文件 | 说明 |
|------|------|------|
| `MeleeCombatStrategy` | `Combat/MeleeCombatStrategy.cs` | 索敌(按距离排序) + 追击 + 近战挥砍 + 防御结算 |
| `RangedCombatStrategy` | `Combat/RangedCombatStrategy.cs` | 索敌(按距离排序) + 追击 + 生成投射物。伤害类型从 `UnitAttr.attackType` 读取 |
| `HealerCombatStrategy` | `Combat/HealerCombatStrategy.cs` | 找血量%最低的友方 + 治疗。无目标时让状态机回 Moving |

**RangedCombatStrategy Inspector 配置：**

| 字段 | 说明 |
|------|------|
| `bulletPrefab` | 投射物预制体 |
| `bulletSpeed` | 投射物飞行速度 |
| `spawnHeightOffset` | 发射点高度偏移 |

**HealerCombatStrategy Inspector 配置：**

| 字段 | 说明 |
|------|------|
| `healAmount` | 每次治疗量 |
| `healRange` | 治疗范围半径 |
| `healCooldown` | 治疗冷却时间 |
| `healEffectPrefab` | 治疗特效预制体 |
| `effectHeightOffset` | 特效高度偏移 |
| `canHealSelf` | 是否可治疗自己 |

#### 4.5.6 各兵种组合

| 兵种 | MoveStrategy | CombatStrategy |
|------|-------------|----------------|
| soldier / hound / Mechs / Enemy2 | GroundMoveStrategy | MeleeCombatStrategy |
| FlyingUnit | **FlightMoveStrategy** | MeleeCombatStrategy |
| Archer / Mage | GroundMoveStrategy | RangedCombatStrategy |
| Healer | GroundMoveStrategy | HealerCombatStrategy |

#### 4.5.7 UnitAttr（属性数据）

**文件：** `Assets/Unit/GenericScript/UnitAttr.cs`（保留不变）

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
public UnitType unitType;              // Ground / Flying
public AttackRangeType attackRangeType; // Melee / Ranged
```

#### 4.5.8 Bullet（投射物）

**文件：** `Assets/Unit/NewScript/Bullet.cs`

v2 改进：
- 检查目标存活状态（`targetAttr.currentHp <= 0`），不攻击尸体
- 伤害通过 `GetComponent<UnitBrain>().TakeDamage()` 或 `GetComponent<TowerBase>().TakeDamage()` 结算
- 伤害类型从发射方配置传递（`RangedCombatStrategy` 读取 `UnitAttr.attackType`）

#### 4.5.9 v2 vs v1 差异总结

| 方面 | v1（旧） | v2（新） |
|------|---------|---------|
| 组件数量 | 5-7 个（含特殊变体接管模式） | 统一 5 个 |
| 索敌排序 | 不排序，取第一个 | 按距离排序，取最近 |
| 攻击冷却 | UnitCombat.Update 始终运行；特殊变体只在范围内 | 所有 CombatStrategy.Update 始终运行 |
| Mage 伤害类型 | 硬编码 Magic | 读取 `UnitAttr.attackType` |
| Bullet 存活检查 | 只检查 null | 检查 `currentHp <= 0` |
| 飞行单位 | free-flight 到硬编码坐标 | 沿 Flight 类型 PathManager 路径 |
| 远程追击 | 无 | `RangedCombatStrategy.TryExecute` 含追击 |
| Healer 接管方式 | 禁用 UnitAI + UnitCombat | HealerCombatStrategy 作为一等策略 |

---

### 4.6 存档系统 (Save)

#### 4.6.1 SaveManager

**职责：**
- 静态存档管理器，全局访问
- 支持 3 个存档栏位（0~2），JSON 格式持久化
- 文件路径：`Application.persistentDataPath/gamesave_{slotIndex}.json`

**核心方法：**

| 方法 | 功能 |
|------|------|
| `HasSave(int slotIndex)` | 检查指定栏位是否有存档 |
| `LoadSave(int slotIndex)` | 读取指定栏位存档，失败返回 null |
| `SaveGame(GameSaveData, int)` | 保存存档到指定栏位，自动记录时间 |
| `DeleteSave(int slotIndex)` | 删除指定栏位存档 |
| `CreateNewGame()` | 创建全新存档数据（所有关卡未完成，当前=第一关） |
| `MarkLevelCompleted(string, int)` | 标记关卡完成，自动推进到下一关 |
| `HasAnySave()` | 检查是否存在任何存档 |
| `GetLatestSaveSlot()` | 获取最新存档栏位索引，无存档返回 -1 |

**关键字段：**

```csharp
public const int SlotCount = 3;            // 存档栏位数
public static int CurrentSlotIndex;        // 当前游玩的栏位（MenuManager 设置）
```

#### 4.6.2 GameSaveData

**数据结构：**

```csharp
[System.Serializable]
public class GameSaveData
{
    public int saveVersion = 1;            // 存档版本号
    public int slotIndex;                  // 所属栏位
    public string saveTime;                // 存档时间
    public int currentLevelIndex;          // 当前关卡索引
    public string currentLevelScene;       // 当前关卡场景名
    public List<LevelRecord> levelRecords; // 关卡完成记录列表
}

[System.Serializable]
public class LevelRecord
{
    public string levelScene;              // 关卡场景名
    public bool isCompleted;               // 是否已完成
}
```

---

### 4.7 菜单系统 (Menu)

#### 4.7.1 MenuManager

**职责：**
- 主菜单核心逻辑
- 新游戏/继续游戏/加载存档/退出游戏
- 委托 SaveSlotPanel 处理栏位选择 UI

**核心方法：**

| 方法 | 功能 |
|------|------|
| `OnNewGameClicked()` | 打开栏位面板（NewGame 模式） |
| `OnContinueClicked()` | 自动加载最新存档进入游戏 |
| `OnLoadClicked()` | 打开栏位面板（Load 模式） |
| `OnSlotConfirmed(int)` | 栏位确认回调，开始新游戏或加载存档 |

**数据流：**
```
新游戏: 选择栏位 → 删旧档 → CreateNewGame → SaveGame → LoadScene("Level_1")
继续游戏: GetLatestSaveSlot → LoadSave → LoadScene(save.currentLevelScene)
加载存档: 选择栏位 → LoadSave → LoadScene
```

#### 4.7.2 SaveSlotPanel

**职责：**
- 独立的存档栏位选择 UI 组件
- 支持 NewGame（选空栏位/覆盖已有）和 Load（选已有存档）两种模式
- 从按钮子节点自动获取 Text 显示存档信息
- 内置覆盖确认弹窗

**核心方法：**

| 方法 | 功能 |
|------|------|
| `Show(Mode)` | 显示面板并刷新栏位信息 |
| `Hide()` | 隐藏面板及子面板 |
| `OnSlotConfirmed(int)` | 事件，用户确认选择后触发 |
| `OnCancelled` | 事件，用户取消时触发 |

#### 4.7.3 MenuSettingsManager

**职责：**
- 菜单场景设置面板管理
- 音量/全屏调整，PlayerPrefs 持久化

#### 4.7.4 GameSettingsManager

**职责：**
- 关卡内设置面板管理
- 打开设置时自动暂停游戏（保存/恢复 timeScale）
- 返回主菜单功能

#### 4.7.5 GamePauseManager

**职责：**
- 关卡内暂停/继续/重开/返回主菜单

---

### 4.8 对话系统 (Dialogue)

#### 4.8.1 DialogueManager

**职责：**
- 对话流程控制
- 文字打字机效果
- 对话UI显示

**核心方法：**

| 方法 | 功能 |
|------|------|
| `StartDialogue(DialogueAsset dialogue)` | 开始对话 |
| `OnContinueClick()` | 继续/跳过打字 |
| `ShowNextLine()` | 显示下一句 |
| `TypeLine(string text)` | 打字机协程 |

---

## 5. 枚举定义

### 5.1 CampType

```csharp
public enum CampType
{
    Player,  // 玩家阵营
    Enemy    // 敌方阵营
}
```

### 5.2 PathID

```csharp
public enum PathID
{
    Path_01,
    Path_02,
    Path_03
    // 可扩展
}
```

### 5.3 AttackType

```csharp
public enum AttackType
{
    Physical,  // 物理攻击（结算 physicalDefense）
    Magic      // 法术攻击（结算 magicDefense）
}
```

### 5.4 UnitType

```csharp
public enum UnitType
{
    Ground, // 地面单位，沿 Ground 路径移动
    Flying  // 飞行单位，沿 Flight 路径移动，近战无法攻击
}
```

### 5.5 AttackRangeType

```csharp
public enum AttackRangeType
{
    Melee,  // 近战，不能攻击飞行单位
    Ranged  // 远程，可攻击所有单位
}
```

### 5.6 PathMoveType

```csharp
public enum PathMoveType
{
    Ground,  // 地面路径（默认），仅地面单位可部署
    Flight   // 飞行路径，仅飞行单位可部署
}
```

### 5.7 UnitState

```csharp
public enum UnitState
{
    Moving,         // 沿路径移动
    Fighting,       // 与敌方单位交战
    Garrisoned,     // 驻扎在资源点/驻扎点
    AttackingTower, // 攻击敌方防御塔
    Dead            // 已死亡
}
```

### 5.8 WaveTriggerType

```csharp
public enum WaveTriggerType
{
    AfterPrevious,  // 上波结束后延迟触发
    Concurrent,     // 与上波并发
    Manual,         // 等待外部调用 Continue()
    AllUnitsDead    // 场上无存活敌人后触发
}
```

---

## 6. 工具类

### 6.1 Math2DHelper

**职责：** 2D数学计算辅助

**核心方法：**

| 方法 | 功能 |
|------|------|
| `SqDistPointToSegment()` | 点到线段最短距离平方 |
| `MinDistancePointToPolyline()` | 点到折线最短距离 |
| `ClosestPointOnPolyline()` | 点到折线最近点坐标 + 段索引 + 插值 t |

---

## 7. 游戏流程

### 7.1 菜单到关卡流程

1. **主菜单**
   - 新游戏 → 选择存档栏位 → 进入 Level_1
   - 继续游戏 → 自动加载最新存档 → 进入对应关卡
   - 加载存档 → 选择已有存档栏位 → 进入对应关卡

2. **关卡进行中**
   - 暂停面板：暂停/继续/重开/返回主菜单
   - 设置面板：音量/全屏/返回主菜单（打开时自动暂停）

3. **胜利**
   - 自动存档 + 标记关卡完成 → 显示胜利面板 → 下一关/返回主菜单

4. **失败**
   - 显示失败面板 → 重试/返回主菜单

### 7.2 完整战斗流程

1. **初始化阶段**
   - BattleManager 初始化单例
   - LevelPathManager 收集场景路径
   - CardManager 生成玩家卡组卡牌UI

2. **战斗开始**
   - BattleManager 启动 WaveGenerator
   - 费用开始自动增长

3. **波次循环**
   - WaveGenerator 按配置生成敌人
   - 敌人沿指定路径向目标塔移动

4. **玩家操作**
   - 玩家选择卡牌
   - 进入部署模式，显示同阵营路线
   - 选择路线并部署单位
   - 单位沿路径前进并战斗

5. **胜负判定**
   - 双塔互相攻击
   - 一方塔死亡触发胜负判定
   - 暂停游戏，显示结果面板

---

## 8. 项目配置

### 8.1 Unity包依赖

根据 [Packages/manifest.json](file:///d:/project/My%20project/Packages/manifest.json)，主要依赖：

- `com.unity.feature.2d` - 2D功能包
- `com.unity.textmeshpro` - 文本渲染
- `com.unity.ugui` - UI系统
- `com.unity.modules.physics2d` - 2D物理

### 8.2 标签与层

项目使用以下标签/层：

- `BattleCanvas` - 战斗UI画布标签
- `EnemyUnit` - 敌方单位层
- `PlayerUnit` - 玩家单位层

### 8.3 构建场景列表

- `MenuScene.unity` — 主菜单
- `Level_1.unity` — 关卡 1
- `TestLevelScene.unity` — 测试关卡

---

## 9. 开发指南

### 9.1 新增卡牌

1. 创建 CardData 资产（右键 → Battle → Card Data）
2. 配置卡牌属性（费用、冷却、图标、单位预制体）
3. 将卡牌添加到 PlayerDeck 配置

### 9.2 新增路径

1. 在场景创建空物体，添加 PathManager 组件
2. 配置阵营和路径ID
3. 添加路径点子物体
4. LevelPathManager 会自动收集

### 9.3 新增波次

1. 创建 WaveList 配置资产
2. 添加 WaveData 条目
3. 配置每个波次的阵营、路径、单位、数量、间隔
4. 将 WaveList 绑定到 BattleManager

### 9.4 新增单位

1. 创建单位预制体
2. 挂载组件：`UnitAttr` + `UnitUI` + `UnitBrain` + 一个 MoveStrategy + 一个 CombatStrategy
3. 配置 `UnitAttr` 的各项数值（HP、攻击、速度、范围、阵营、类型等）
4. 远程单位在 `RangedCombatStrategy` 中配置 `bulletPrefab`、`bulletSpeed`
5. 治疗单位在 `HealerCombatStrategy` 中配置 `healRange`、`healAmount`、`healCooldown`
6. 关联到卡牌 `CardData.unitPrefab` 或波次 `WaveData.unitPrefab`

> 旧预制体可通过 `Tools → 迁移兵种预制体到新体系` 一键升级。

### 9.5 新增关卡

1. 将关卡场景添加到 Build Settings
2. 在 `SaveManager.levelSceneOrder` 数组中按顺序追加关卡场景名

### 9.6 存档栏位数调整

修改 `SaveManager.SlotCount` 常量，SaveSlotPanel 会自动适配。

---

## 10. 关键文件索引

| 文件 | 说明 |
|------|------|
| [BattleManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Battle/BattleManager.cs) | 战斗核心管理器 |
| [TowerBase.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Battle/TowerBase.cs) | 防御塔基础脚本 |
| [CardManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Card/CardManager.cs) | 卡牌管理器 |
| [CardData.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Card/CardData.cs) | 卡牌数据 |
| [CardDeploy.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Card/CardDeploy.cs) | 卡牌部署系统 |
| [PathManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Path/PathManager.cs) | 单条路径管理（含曲线系统） |
| [LevelPathManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Path/LevelPathManager.cs) | 场景路径中心 |
| [PathVisualManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Path/PathVisualManager.cs) | 路径视觉效果管理 |
| [GarrisonPoint.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/GarrisonPoint/GarrisonPoint.cs) | 驻扎点组件 |
| [GarrisonPointManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/GarrisonPoint/GarrisonPointManager.cs) | 驻扎点管理器（场景级单例） |
| [GarrisonPointPlacer.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/GarrisonPoint/GarrisonPointPlacer.cs) | 驻扎点放置交互 |
| [WaveGenerator.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Wave/WaveGenerator.cs) | 波次生成器 |
| [UnitBrain.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/UnitBrain.cs) | 单位状态机调度器 |
| [UnitAttr.cs](file:///d:/project/My%20project/Assets/Unit/GenericScript/UnitAttr.cs) | 单位属性数据 |
| [GroundMoveStrategy.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Movement/GroundMoveStrategy.cs) | 地面沿路径移动 |
| [FlightMoveStrategy.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Movement/FlightMoveStrategy.cs) | 飞行沿路径移动 |
| [MeleeCombatStrategy.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Combat/MeleeCombatStrategy.cs) | 近战战斗策略 |
| [RangedCombatStrategy.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Combat/RangedCombatStrategy.cs) | 远程射击策略 |
| [HealerCombatStrategy.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Combat/HealerCombatStrategy.cs) | 治疗策略 |
| [Bullet.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/Bullet.cs) | 投射物 |
| [PathMoveType.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Path/PathMoveType.cs) | 路径移动类型与兼容性校验 |
| [UnitState.cs](file:///d:/project/My%20project/Assets/Unit/NewScript/UnitState.cs) | 单位状态枚举 |
| [Math2DHelper.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/Helper/Math2DHelper.cs) | 2D数学工具 |
| [DialogueManager.cs](file:///d:/project/My%20project/Assets/Dialogue/DialogueManager.cs) | 对话管理器 |
| [SaveManager.cs](file:///d:/project/My%20project/Assets/Save/SaveManager.cs) | 存档管理器（静态） |
| [GameSaveData.cs](file:///d:/project/My%20project/Assets/Save/GameSaveData.cs) | 存档数据结构 |
| [MenuManager.cs](file:///d:/project/My%20project/Assets/Menu/MenuManager.cs) | 主菜单管理器 |
| [MenuSettingsManager.cs](file:///d:/project/My%20project/Assets/Menu/MenuSettingsManager.cs) | 菜单设置管理器 |
| [SaveSlotPanel.cs](file:///d:/project/My%20project/Assets/Menu/SaveSlotPanel.cs) | 存档栏位选择面板 |
| [GameSettingsManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/UI/GameSettingsManager.cs) | 关卡内设置管理器 |
| [GamePauseManager.cs](file:///d:/project/My%20project/Assets/Level/CommonLevel/UI/GamePauseManager.cs) | 暂停/重开/返回菜单 |

---

## 11. 扩展建议

1. **卡牌系统扩展**
   - 添加特殊效果卡牌（AOE、治疗、Buff等）
   - 支持卡牌升级与组合
   - 实现卡牌抽卡机制

2. **单位系统扩展**
   - 添加更多策略组件（如 AoECombatStrategy、BuffCombatStrategy）
   - 实现单位技能系统
   - 添加单位升级与进化

3. **路径系统扩展**
   - 添加路径事件（陷阱、buff点）
   - 支持路径分支与选择
   - ~~曲线路径~~（✅ 已实现 Centripetal CR 曲线）

4. **存档系统扩展**
   - 支持存档导入/导出
   - 添加自动存档（波次中途）
   - 支持云存档

5. **UI/UX优化**
   - 关卡选择地图界面
   - 战斗统计面板
   - 优化卡牌拖动与部署手感

---

*文档更新时间：2026-07-13*
