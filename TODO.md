# 项目待办事项

> 路径已随代码结构更新（`Assets/Level|Unit|Dialogue` → `Assets/Game/...`、`Assets/Deprecated/...`）。旧 `Dialogue/` 手写对话系统已废弃，相关待办不再适用。

# 补充完整战斗系统

- [ ] **飞行单位追击行为**：飞行单位会往回走追击敌人。可考虑改为：遇到敌人时攻击，敌人离开后继续沿路径移动（不再回头追）。
- [ ] **左右反转朝向**：单位/子弹缺朝向翻转逻辑。
- [ ] **优化塔和驻扎点的检测交互逻辑**：利用 2D 碰撞检测替代目前的距离计算。

---

## 🟡 中优先级 — 体验完善

### 5. 音效系统完全缺失

**现状：** 项目中有音量设置（`MenuSettingsManager`、`GameSettingsManager`），但没有音效管理器，也没有任何 AudioSource/AudioClip 的使用。

**需要：**
- [ ] 创建 `AudioManager` 单例，统一管理 BGM 和 SFX
- [ ] 添加关键音效：卡牌点击、单位部署、攻击命中、单位死亡、胜利/失败
- [ ] 添加背景音乐（菜单、战斗）
- [ ] 从 `PlayerPrefs` 读取音量设置并应用到 AudioMixer

### 6. 视觉反馈缺失（部分已实现）

**现状：** 受击闪红已实现（`UnitVisual.cs`，`Assets/Game/Unit/Scripts/Core/UnitVisual.cs`）；但单位死亡直接 `Destroy(gameObject)`，攻击无视觉特效反馈，治疗特效仅为可选预制体。

**需要：**
- [ ] 单位死亡动画/特效（爆炸、消散等）
- [ ] 攻击挥砍/射击特效
- [ ] Bullet/Fireball 飞行拖尾特效
- [ ] 伤害数字弹出（`FloatNumber.cs` 已存在但从未被 Instantiate，属于死代码）

### 7. 场景加载过渡

**现状：** 所有场景切换都是直接 `SceneManager.LoadScene()`，无过渡效果。

**需要：**
- [ ] 创建 Loading 场景或 Loading UI 面板
- [ ] 使用 `SceneManager.LoadSceneAsync()` 异步加载 + 进度条
- [ ] 淡入淡出过渡动画

### 8. 单位动画系统缺失

**现状：** 所有移动/战斗策略只更新 Transform 位置，没有 Animator 或 sprite 动画控制。角色朝向仅 HealerCombatStrategy 中有旋转逻辑。

**需要：**
- [ ] 为每个兵种添加 Animator 组件 + 动画控制器
- [ ] 动画状态：Idle、Walk、Attack、Die
- [ ] 策略组件中触发动画切换（通过 UnitBrain 事件或 Animator 参数）

### 9. 单位死亡后波次追踪残留

**现状：** `WaveGenerator` 追踪生成的单位用于 `AllUnitsDead` 触发条件判定，单位死亡后仅在 `AnyTrackedUnitStillAlive()` 中清理 null 引用。

**需要：**
- [ ] 利用 `UnitBrain.OnDeath` 事件通知 WaveGenerator 单位已死亡（`UnitBrain.Die()` 已触发 OnDeath，直接订阅即可）

---

## 🟢 低优先级 — 锦上添花

### 12. 关卡星级/评分系统

**需要：**
- [ ] 设计评分维度（通关时间、损失单位数、剩余费用等）
- [ ] 1~3 星评价标准
- [ ] 存档中记录每关最高星级
- [ ] 关卡选择界面展示星级

### 13. 卡牌/单位升级系统

**需要：**
- [ ] 设计升级货币（金币/经验）
- [ ] 卡牌升级提升单位属性（HP、ATK 等）
- [ ] 升级数据持久化到存档

### 14. 新手教程关卡

**需要：**
- [ ] 设计引导式教程关卡
- [ ] 逐步介绍：部署 → 费用 → 路径 → 驻扎 → 资源点
- [ ] 高亮提示和操作引导箭头

### 15. 图鉴/兵种百科

**需要：**
- [ ] 展示所有已解锁兵种的属性和描述
- [ ] 解锁条件（通过指定关卡后解锁）

### 16. 成就系统

**需要：**
- [ ] 设计成就列表（首次通关、无伤通关、全兵种收集等）
- [ ] `AchievementManager` + 成就数据持久化
- [ ] 成就解锁弹窗

### 17. 资源点/驻扎点运行时可视化（部分已实现）

**现状：** 已实现通用范围圈显示（`RangeCircleDisplay` 实心圈、`LeashCircleDisplay` 空心圈，`Assets/Game/Level/CommonLevel/Helper/`）。资源点/驻扎点的**占领状态**显示（颜色按阵营变化、驻扎数量、争夺闪烁）仍待补。

**需要：**
- [ ] 运行时显示资源点/驻扎点占领范围（复用 RangeCircleDisplay）
- [ ] 显示当前驻扎单位数量
- [ ] 争夺中闪烁/黄色提示

### 18. 暂停菜单完善

**现状：** `GamePauseManager`（`Assets/Game/Level/CommonLevel/UI/GamePauseManager.cs`）有暂停/继续/重试功能，但缺少"返回主菜单"按钮（设置面板中有一个）。

**需要：**
- [ ] 暂停面板中增加返回主菜单按钮
- [ ] 暂停时禁用卡牌点击和部署操作

---

## 🔧 技术债务 / 代码改进

### 20. 事件系统解耦

**现状：** 大量直接单例引用（`BattleManager.Instance.xxx`、`CardManager.Instance.xxx`），模块耦合紧密。单位侧已引入事件系统（`UnitBrain.OnStateChanged/OnDeath/OnDamageTaken` 等），但系统级事件仍缺失。

**建议：**
- [ ] 引入 `UnityEvent` 或自定义事件总线
- [ ] 关键事件：费用变化、波次结束、游戏结束、塔血量变化

### 21. 配置表驱动

**现状：** 单位属性硬编码在 prefab 的 `UnitAttr` 中，卡牌数据依赖 ScriptableObject 手动创建。

**建议：**
- [ ] 考虑用 CSV/JSON 配置表管理单位数值
- [ ] 编辑器工具一键从配置表生成/更新 prefab 和 ScriptableObject

### 22. 对象池

**现状：** 单位、子弹、特效全部使用 `Instantiate`/`Destroy`。

**建议：**
- [ ] 高频生成销毁的对象使用对象池（单位、Bullet、FloatNumber）
- [ ] 减少 GC 压力

### 23. 单元测试

**现状：** 项目无任何单元测试。

**建议：**
- [ ] 核心逻辑单元测试（伤害计算、索敌排序、路径弧长参数化、传送段判定等）
- [ ] 使用 Unity Test Framework

### 24. FloatNumber 死代码

**现状：** `FloatNumber.cs`（`Assets/Game/Unit/Scripts/UI/FloatNumber.cs`）已实现但项目中没有任何地方 Instantiate 它，是未接入的死代码。

**建议：**
- [ ] 在 `BaseCombatStrategy` 伤害结算、`Bullet` 命中、`HealerCombatStrategy.HealTarget()` 中接入 FloatNumber 弹出

### 25. 清理 BeiFen.unity 备份场景

**现状：** `Assets/Game/Level/CommonLevel/Battle/BeiFen.unity`（"备份"的拼音）是 CommonLevel 场景的复制品，不在 Build Settings 中。

**建议：**
- [ ] 确认无引用后删除，避免混淆

### 26. UiStart.cs 乱码修复 + GameStart 场景未接入

**现状：** `Assets/Scenes/GameStart/UiStart.cs` 中的 Debug.Log 字符串显示为乱码（文件编码问题），且其加载的 `"Test"` 场景与 `GameStart` 都**不在 Build Settings** 中。

**建议：**
- [ ] 修复文件编码为 UTF-8，重新写入正确的中文字符串
- [ ] 确认 GameStart/Test 场景在构建流程中的定位（目前游戏从 MenuScene 开始）

### 27. 废弃对话系统清理

**现状：** 旧 `Assets/Dialogue/` 手写对话系统已整体移入 `Assets/Deprecated/Dialogue/`（含 `DialogueScene.unity`），已被 Fungus 取代。

**建议：**
- [ ] 确认无引用后，从 `Deprecated/` 删除（或保留作参考）

---

## 📋 已完成

- [x] 单位系统 v2 — 策略组件模式重构（UnitBrain + IMoveStrategy + ICombatStrategy）
- [x] 路径系统 — 直线/曲线/循环 + Gauss 求积弧长参数化
- [x] 波次系统 — 多触发策略（AfterPrevious/Concurrent/Manual/AllUnitsDead）
- [x] 驻扎/资源点 — 动态驻扎 + 争夺判定（Garrison 核心组件）
- [x] 存档系统 — 3 栏位 JSON 存档
- [x] 关卡配置中枢 — LevelSetup 统一分发
- [x] 主菜单 — 新游戏/继续/加载/设置 + 栏位覆盖确认
- [x] 8 个兵种预制体 — soldier/hound/Mechs/Enemy/Archer/Mage/FlyingUnit/Healer
- [x] 3 个完整关卡 — Level_1、Level_2、Level_3（含场景 + WaveList）→ 已扩展到 Level_1~6
- [x] 编辑器工具 — 卡牌数据一键生成、PathManager 路线编辑器、路径视觉迁移
- [x] 投射物预制体 — Bullet + Fireball；治疗特效 — HealEffect
- [x] 设置持久化 — 音量/全屏通过 PlayerPrefs 保存
- [x] 路径节点间传送 — ConnectionType.Walk/Teleport + BaseMoveStrategy 传送状态机（Level_5/6 配置）
- [x] Boss 关胜利判定 — BossUnit 死亡判胜（useBossVictory，Level_6 已开启，**Boss 出场未接线**）
- [x] 牵制范围机制 — TowerLeashZone 超半径死亡并返还费用（Level_6 已挂载）
- [x] Buff 系统 — BuffManager + UnitAttr 修饰层（攻击/生命/移速/攻速/防御倍率）
- [x] 剧情系统（Fungus）— StoryScene_Ch1/Ch2 + 剧本导入工具 ScriptToFungusTool
- [x] 受击闪红视觉 — UnitVisual.cs
- [x] 项目文件结构整理 — 代码迁移到 `Assets/Game/`，单位脚本按职责分 Combat/Movement/Core/UI

---

## 🚧 开发中 / 已知问题（文档同步时发现）

- [ ] **Boss 关未真正可玩**：`Level_6` 已开启 `useBossVictory` 与牵制范围，但 `SpecialUnit/Boss.prefab` **未被任何波次/场景引用**，Boss 实际出场未接线 → 敌方塔不判胜、场上又无 Boss 可杀，关卡目前无法通关。
- [ ] **关卡推进链仅到第二关**：`SaveManager.levelSceneOrder` 硬编码 `{ "Level_1", "Level_2" }`，Level_3~6 未加入推进链。
- [ ] **Build Settings 不完整**：`Level_3~6`、`Test`、`GameStart` 未加入；`EditorBuildSettings.asset` 内路径仍为旧路径（GUID 与新位置一致，Unity 打开会自动修正，属低风险）。
- [ ] **对话内容占位**：剧情立绘 `Portrait_A.png` 等为占位图，正式美术待替换。
