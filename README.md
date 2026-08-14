# My Project — 技术文档

2D 塔防策略游戏（Unity 2022.3.62f3c1），融合卡牌召唤、单位沿路径移动、双塔攻防、波次、资源点/驻扎点争夺、Buff、剧情（Fungus）、Boss 关与传送等机制。

> 代码按模块组织在 `Assets/Game/` 下；单位脚本按职责分 `Combat / Movement / Core / UI`；美术资源在 `Assets/ArtResources/`；废弃代码在 `Assets/Deprecated/`。更详细的架构与 API 见 `CodeWiki.md`。

---

## 1. 关卡统一配置（LevelSetup）

**文件:** `Assets/Game/Level/CommonLevel/LevelSetup/LevelSetup.cs`

每个关卡场景中挂载一个 `LevelSetup` 组件（`[DefaultExecutionOrder(-100)]`，最早执行），作为该关卡所有配置的统一入口，在 `Awake` 中自动分发到各子系统。

> **LevelConfig 资产**：`LevelSetup.Awake()` 会先 `Resources.Load<LevelConfig>("Config/LevelConfig_{场景名}")` 读取本关配置资产（由 CSV 策划表工具生成，见第 11 章）。资产存在时**优先用资产填充**卡组 / Buff / 波次 / 塔覆盖并自动打开对应 `overrideXxx`；资产缺失则回退到场景 Inspector 默认配置（不影响 Test / BeiFen 等无配置场景）。

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

> 每个分组都有独立的 `overrideXxx` 开关。关闭后该子系统使用自身的 Inspector 默认值，不受 LevelSetup 影响。

**Boss 相关配置已从 LevelSetup 拆出**，独立为 `BossLevelSetup` 组件（`Assets/Game/Level/CommonLevel/LevelSetup/BossLevelSetup.cs`）：需要 Boss 机制（`useBossVictory` 判胜 + `TowerLeashZone` 牵制范围）的关卡，在场景中挂一个并配置即可，与 LevelSetup 解耦。**当前 BossLevelSetup 尚未挂载到任何场景**（Boss 关整体未激活，详见第 6 章 TowerLeashZone 状态说明）。

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
- `Combat/MeleeCombatStrategy.cs` — 近战（按距离索敌；**地面近战跳过飞行单位，飞行近战可攻击任意目标含飞行**）
- `Combat/RangedCombatStrategy.cs` — 远程投射物（生成 Bullet，发射时触发 `OnRangedFire`）
- `Combat/HealerCombatStrategy.cs` — 治疗（奶血量百分比最低友方，生效时触发 `OnHeal`）
- `Combat/Bullet.cs` — 通用投射物（命中触发 `GameEvents.OnBulletHit`）

**Movement/**（移动策略）
- `Movement/IMoveStrategy.cs` — 移动策略接口（含 `IsTeleporting`）
- `Movement/BaseMoveStrategy.cs` — 移动抽象基类：两阶段移动、追击回归、**传送状态机**
- `Movement/GroundMoveStrategy.cs` / `FlightMoveStrategy.cs` — 地面 / 飞行沿路径移动
- `Movement/MoveType.cs` — `MoveType` 枚举 + `MoveTypeHelper`（兼容性判断）

**Core/**（单位核心）
- `Core/UnitAttr.cs` — 属性数据 + Buff 修饰层 + `AttackType`/`AttackRangeType` 枚举
- `Core/UnitBrain.cs` — 状态机调度器（事件系统：状态 / 死亡 / 受击 / 攻击命中 / 远程开火 / 治疗等；并自动注册到 `AudioManager`）
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
| `attackRangeType` | `Melee`（近战；**地面近战不能攻击飞行单位，飞行近战可攻击任意**）或 `Ranged`（远程）|
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

> 波次事件：每波开始触发 `GameEvents.OnWaveStart`，生成第一个敌人时触发 `GameEvents.OnEnemySpawned`（供音效等订阅方监听）。

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
| `useBossVictory` | Boss 胜利模式开关（由 `BossLevelSetup` 组件写入）|

`BattleManager` 统一管理扣费（`UseCost()`）、加费（`AddCost()`，不超过上限，牵制返还用）、胜负判定、胜利/失败面板显示。

**胜负判定：**
- 普通模式：一方防御塔被摧毁即触发胜负。
- **Boss 胜利模式**（`useBossVictory=true`）：敌方塔被摧毁**不判胜**，仅挂有 `BossUnit` 的敌方单位死亡触发 `OnBossDefeated()` 判胜；玩家塔被摧毁仍判负。
- 胜利时若该关在 `LevelToStoryMap` 中有对应剧情场景，先播剧情再进下一关（当前映射：`Level_1 → StoryScene_Ch2`）。

> 战斗事件：胜负 / Boss 击破触发 `GameEvents.OnGameWin / OnGameLose / OnBossDefeated`；塔攻击 / 受击 / 摧毁触发 `GameEvents.OnTowerAttack / OnTowerDamaged / OnTowerDestroyed`。

### TowerBase

防御塔组件挂载在塔 GameObject 上：`camp` / `hp` / `atk` / `atkRange` / `atkCD` / 双防御 / `hpBarPrefab`。每帧搜索范围内敌方单位并攻击，死亡时通知 `BattleManager` 判定胜负。

### Boss 关牵制范围（TowerLeashZone）

挂载在己方塔上（单例），参数由场景中的 `BossLevelSetup` 组件写入。`CardDeploy` 部署单位后调用 `RegisterUnit(brain)` 注册；每 `checkInterval` 检测单位与塔的距离，超出 `leashRange` 时：
1. 返还 `deployCost × costRefundRatio` 费用（`BattleManager.AddCost()`）
2. 调用 `UnitBrain.Die()` 走正常死亡流程
3. 传送中的单位跳过检测

范围圈可视化：`LeashCircleDisplay`（`Helper/`，空心圆，世界坐标精确）。

> **当前状态**：`TowerLeashZone` 组件已无任何场景 / prefab 实例，需挂到己方塔上并配 `BossLevelSetup` 才会生效。

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
- `Assets/Game/Menu/MenuSettingsManager.cs` — 菜单设置（BGM / SFX 双通道音量 + 全屏，音量持久化由 `AudioManager` 统一处理）
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

## 11. CSV 策划配置表

将策划数值从 Unity 预制体 / 资产中抽离到 CSV，策划用 Excel 改表 → 回 Unity 点菜单 → 数值自动写入游戏。

**目录结构**（`Assets/Game/CSV/`）：

| 目录 | 内容 |
|------|------|
| `Core/` | 运行时通用解析层：`CsvReader.cs`（轻量 CSV 解析，UTF-8 BOM / 严格 UTF-8 / GBK 回退编码兼容，支持 `#` 注释行）、`CsvTable.cs`（表头映射 + 类型化取值 + `CsvWriterHelper` 写出 UTF-8 BOM）|
| `Editor/` | 编辑器工具：`ConfigTableConst`（路径 / 列映射 / 导入前备份）、`ConfigTableExporter`、`ConfigTableMenu`、各表导入器（Unit / Card / Deck / Buff / Tower / Wave）|
| `Tables/` | 数据表 CSV：`UnitTable.csv`、`CardTable.csv`、`DeckTable.csv`、`BuffTable.csv`、`TowerTable.csv`、`WaveTable_Level_1~6.csv` + `README.txt` + `_backup/`（导入前自动备份）|

**菜单**（`Tools/策划表/`）：导入 单位表 / 卡牌表 / 初始牌组表 / Buff 表 / 塔属性表 / 波次表、**一键导入全部**、导出 CSV 初版、打开 CSV 文件夹。

**六张表：**

| 表 | 主键 | 内容 |
|----|------|------|
| UnitTable.csv | `unitID` = 预制体文件名 | 单位 `UnitAttr` 全量数值（`GeneralUnit/` 下 8 个单位）|
| CardTable.csv | `cardID` | 卡牌费用 / 冷却 / 阵营 / 名称（`CardAssets/` 下 8 张卡）|
| DeckTable.csv | `levelIndex` + `cardID` | 每关初始牌组（引用 `Card_{cardID}.asset`）|
| BuffTable.csv | `levelIndex` + `buffID` | 每关应用的 Buff（引用 `Buff_{buffID}.asset`）|
| TowerTable.csv | `levelIndex` + `towerCamp` | 双塔属性覆盖（6 关 × 2 阵营）|
| WaveTable_Level_N.csv | `waveIndex` | 每关波次（`spawnPoint` 不进表，需在 Unity 的 WaveList 资产里拖）|

**工作流（三步）：**
1. 用 Excel 打开 CSV 修改数值（**另存为「CSV UTF-8」格式**，避免中文乱码）
2. 保存并关闭文件（Excel 会锁定文件）
3. Unity 菜单 `Tools → 策划表 → 一键导入全部`，弹窗提示成功 / 失败条数

**导入后发生了什么：**
- 单位表 / 卡牌表：直接改写 `GeneralUnit/*.prefab` 的 `UnitAttr` 与 `CardAssets/*.asset`（`SerializedObject` 落盘 + `Undo` 支持 Ctrl+Z）
- 牌组 / Buff / 塔 / 波次四张表：生成 / 更新每关的 **`LevelConfig` 资产**（`Assets/Resources/Config/LevelConfig_Level_N.asset`），`LevelSetup` 运行时 `Resources.Load` 自动读取（见第 1 章）
- 导入前自动备份被改动文件到 `_backup/{时间戳}/`，误操作可恢复
- 波次表导入会顺带把旧字段 `nextWaveInterval` 迁移为新字段 `delayBeforeStart`

> **注意**：DeckTable / BuffTable 当前只有注释示例无数据行；`LevelConfig_Level_N.asset` 需在 Unity 中执行过导入后才会生成（仓库中尚未生成）。表头（第一行）勿改，列顺序可任意调整；`unitID / cardID / buffID` 为主键勿乱改；图标 / 血条等美术引用不进表。

---

## 12. 音效系统（AudioManager）

**文件**（`Assets/Game/Audio/`）：
- `AudioManager.cs` — 音效管理器（单例，`DontDestroyOnLoad` 跨场景保活）
- `AudioConfig.cs` — `AudioSlot` 枚举（18 个 SFX 槽）+ `AudioConfig` 资产（2 个 BGM + 18 个 SFX）
- `AudioManager.prefab` / `AudioConfig.asset` — 预制体与配置资产

**核心机制：**
- **双通道**：BGM 循环源 + SFX 一次性源，音量独立调节，`PlayerPrefs` 持久化（`AudioBGMVolume` / `AudioSFXVolume`；旧单一 `Volume` 键首次启动自动迁移）
- **事件驱动**：集中订阅 `GameEvents` 系统事件（第 5 章波次事件、第 6 章战斗事件等）+ `UnitBrain` 单位事件（`OnAttackHit / OnRangedFire / OnHeal / OnDamageTaken / OnDeath`），驱动全游戏音效；单位 `Awake / OnDestroy` 时由 `UnitBrain` 自动注册 / 解绑
- **Boss 死亡音去重**：Boss 死亡音由 `GameEvents.OnBossDefeated` 统一播放，跳过单位死亡音避免双响
- **UI 点击音**：`AudioManager.BindClick(button)` 一键绑定（主菜单 / 档位 / 设置 / 暂停 / 胜负面板按钮均已绑定）

**挂载与使用：**
- `AudioManager.prefab` 放入 `MenuScene` 与 `CommonLevel.prefab` 各一个实例，`Awake` 单例去重 + `DontDestroyOnLoad` 保活 BGM；后加载场景的实例自动销毁
- BGM：`MenuManager.Start` 播 `bgmMenu`，`BattleManager.Start` 播 `bgmBattle`
- 音量：设置面板（`MenuSettingsManager` / `GameSettingsManager`）改为 BGM + SFX 双滑块，调 `AudioManager.SetBGMVolume / SetSFXVolume`
- 新增音效：把音频拖入 `AudioConfig.asset` 对应槽位（`Create → Audio/AudioConfig`），再拖到 `AudioManager.config` 字段

> **当前配置**：`bgmMenu`、`sfxUIClick`（Fungus Click）、`sfxCardSelect`（Fungus Click2）、`sfxAttackMelee`、`sfxHurt` 已配占位音频（`Assets/MusicResources/` 下 3 个 mp3 + Fungus 自带 wav）；`bgmBattle` 及其余 SFX 槽为空，对应事件静默 no-op。

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
| `TowerLeashZone` | `Battle/TowerLeashZone.cs` | Boss 关牵制范围检测（需挂到塔上，当前无场景实例）|
| `AudioManager` | `Audio/AudioManager.cs` | 音效管理器（BGM/SFX 双通道，跨场景保活）|

> 静态类（无 Instance）：`SaveManager`、`DamageCalculator`、`UnitHelper`、`MoveTypeHelper`、`Math2DHelper`、`PathMath`、`CatmullRomMath`、**`GameEvents`**（全局事件总线）、`CsvReader` / `CsvTable`（CSV 解析）。废弃的 `DialogueManager` 在 `Deprecated/Dialogue/`，请勿使用。
