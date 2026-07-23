# 项目待办事项



# 补充完整战斗系统
**现状：**
飞行单位会往回走追击敌人，可选择改为不在追击敌人（遇到敌人攻击，敌人离开后继续移动）。
上海结算脚本要独立出来
资源点和驻扎点的显示

# 左右反转朝向

利用2d碰撞检测优化塔和驻扎点的检测交互逻辑

## 🟡 中优先级 — 体验完善

### 5. 音效系统完全缺失

**现状：** 项目中有音量设置（`MenuSettingsManager`、`GameSettingsManager`），但没有音效管理器，也没有任何 AudioSource/AudioClip 的使用。

**需要：**
- [ ] 创建 `AudioManager` 单例，统一管理 BGM 和 SFX
- [ ] 添加关键音效：卡牌点击、单位部署、攻击命中、单位死亡、胜利/失败
- [ ] 添加背景音乐（菜单、战斗）
- [ ] 从 `PlayerPrefs` 读取音量设置并应用到 AudioMixer

### 6. 视觉反馈缺失

**现状：** 单位死亡直接 `Destroy(gameObject)`，攻击无视觉特效反馈，治疗有特效预制体但仅为可选。

**需要：**
- [ ] 单位死亡动画/特效（爆炸、消散等）
- [ ] 攻击挥砍/射击特效
- [ ] 受击闪白/震动反馈
- [ ] Bullet/Fireball 飞行拖尾特效
- [ ] 伤害数字弹出（`FloatNumber.cs` 已存在但从未被 Instantiate，属于死代码）

### 7. 场景加载过渡

**现状：** 所有场景切换（菜单→关卡、关卡→菜单、重试）都是直接 `SceneManager.LoadScene()`，无过渡效果。

**需要：**
- [ ] 创建 Loading 场景或 Loading UI 面板
- [ ] 使用 `SceneManager.LoadSceneAsync()` 异步加载 + 进度条
- [ ] 淡入淡出过渡动画

### 8. 单位动画系统缺失

**现状：** 所有移动/战斗策略只更新 Transform 位置，没有 Animator 或 sprite 动画控制。角色朝向仅 HealerCombatStrategy 中有旋转逻辑。

**需要：**
- [ ] 为每个兵种添加 Animator 组件 + 动画控制器
- [ ] 动画状态：Idle、Walk、Attack、Die
- [ ] 策略组件中触发动画切换（通过 UnitBrain 或 Animator 参数）

### 9. 对话系统未集成到关卡流程

**现状：** [DialogueManager](Assets/Dialogue/DialogueManager.cs) 框架完整，但只有测试资产 `New Dialogue.asset`，没有在关卡中实际触发。

**需要：**
- [ ] 为每个关卡编写关卡剧情对话
- [ ] 关卡开始时自动触发开场对话
- [ ] 关键事件触发对话（波次开始、Boss 出现、胜利等）
- [ ] 对话结束后恢复游戏控制

### 10. 单位死亡后波次追踪残留

**现状：** `WaveGenerator` 追踪生成的单位用于 `AllUnitsDead` 触发条件判定，但单位死亡后只是在 `AnyTrackedUnitStillAlive()` 中清理 null 引用，没有主动通知 WaveGenerator。

**需要：**
- [ ] UnitBrain.Die() 中通知 WaveGenerator 单位已死亡
- [ ] 或用事件系统解耦：单位死亡 → 广播事件 → WaveGenerator 接收

### 11. NPCInteract 交互逻辑 Bug

**现状：** [NPCInteract.cs](Assets/Dialogue/NPCInteract.cs) 中 `OnTriggerEnter` 回调内检查 `Input.GetKeyDown(KeyCode.R)`——只有当玩家在进入触发器的**同一帧**按下 R 键才会触发对话。

**影响：** NPC 对话几乎不可能正常触发。

**需要：**
- [ ] 将逻辑拆分为：触发器进入时显示"按 R 对话"提示，`Update` 中检测按键
- [ ] 或改为 `OnTriggerStay` + 按键检测

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

### 17. 资源点/驻扎点运行时可视化

**现状：** ResourcePoint 和 GarrisonPoint 只有 `OnDrawGizmosSelected` 编辑器可视化，运行时不显示占领状态。

**需要：**
- [ ] 运行时显示占领范围圆圈（颜色按阵营变化）
- [ ] 显示当前驻扎单位数量
- [ ] 争夺中闪烁/黄色提示

### 18. 暂停菜单完善

**现状：** [GamePauseManager](Assets/Level/CommonLevel/UI/GamePauseManager.cs) 有暂停/继续/重试功能，但缺少"返回主菜单"按钮（设置面板中有一个）。

**需要：**
- [ ] 暂停面板中增加返回主菜单按钮
- [ ] 暂停时禁用卡牌点击和部署操作



---

## 🔧 技术债务 / 代码改进

### 20. 事件系统解耦

**现状：** 大量直接单例引用（`BattleManager.Instance.xxx`、`CardManager.Instance.xxx`），模块耦合紧密。

**建议：**
- [ ] 引入 `UnityEvent` 或自定义事件总线
- [ ] 关键事件：费用变化、单位死亡、波次结束、游戏结束、塔血量变化

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
- [ ] 核心逻辑单元测试（伤害计算、索敌排序、路径弧长参数化等）
- [ ] 使用 Unity Test Framework

### 24. FloatNumber 死代码

**现状：** [FloatNumber.cs](Assets/Unit/GenericScript/FloatNumber.cs) 已实现但项目中没有任何地方 Instantiate 它，是未接入的死代码。

**建议：**
- [ ] 在 `MeleeCombatStrategy.DealDamage()`、`RangedCombatStrategy`（通过 Bullet）、`HealerCombatStrategy.HealTarget()` 中接入 FloatNumber 弹出

### 25. 清理 BeiFen.unity 备份场景

**现状：** `Assets/Level/CommonLevel/BeiFen.unity`（"备份"的拼音）是 CommonLevel 场景的复制品，不在 Build Settings 中。

**建议：**
- [ ] 确认无引用后删除，避免混淆

### 26. UiStart.cs 乱码修复

**现状：** [UiStart.cs](Assets/Scenes/GameStart/UiStart.cs) 中的 Debug.Log 字符串显示为乱码（`"��Ϸ����ת"`），是文件编码问题导致的中文损坏。

**建议：**
- [ ] 修复文件编码为 UTF-8，重新写入正确的中文字符串

### 27. DialogueScene 清理或正式化

**现状：** `DialogueScene` 在 Build Settings 中但是一个测试场景，不应该是可发布内容。

**建议：**
- [ ] 从 Build Settings 中移除，或将其改造为正式关卡

### 28. ArtResources/Map 空目录

**现状：** `Assets/ArtResources/Map/` 目录存在但为空，说明地图纹理资源曾被计划但未添加。

**建议：**
- [ ] 添加地图背景纹理资源，或删除空目录

---

## 📋 已完成

- [x] 单位系统 v2 — 策略组件模式重构（UnitBrain + IMoveStrategy + ICombatStrategy）
- [x] 路径系统 — 直线/曲线/循环 + Gauss 求积弧长参数化
- [x] 波次系统 — 多触发策略（AfterPrevious/Concurrent/Manual/AllUnitsDead）
- [x] 驻扎/资源点 — 动态驻扎 + 争夺判定
- [x] 存档系统 — 3 栏位 JSON 存档
- [x] 关卡配置中枢 — LevelSetup 统一分发
- [x] 主菜单 — 新游戏/继续/加载/设置 + 栏位覆盖确认
- [x] 对话系统框架 — 打字机效果 + 跨场景持久化 + NPC 交互触发
- [x] 8 个兵种预制体 — soldier/hound/Mechs/Enemy2/Archer/Mage/FlyingUnit/Healer
- [x] 3 个完整关卡 — Level_1、Level_2、Level_3（含场景 + WaveList）
- [x] 编辑器工具 — UnitPrefabMigrator 一键迁移、PathManager 路线编辑器
- [x] 2 个投射物预制体 — Bullet + Fireball
- [x] 治疗特效预制体 — HealEffect
- [x] 设置持久化 — 音量/全屏通过 PlayerPrefs 保存
