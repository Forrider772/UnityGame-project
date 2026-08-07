========================================
  策划配置表使用说明（六张表）
========================================

【这套配置表是干什么的】
  以前单位数值写在 Unity 预制体里、卡牌费用写在资产里、波次/牌组/Buff 散在场景里，
  策划改数值必须打开 Unity 编辑器。现在把这些数值抽到 CSV 表格里，
  策划用 Excel 改表 → 回 Unity 点一个菜单 → 数值自动写入游戏。

【工作流（三步）】
  1. 用 Excel 打开本目录下的 CSV 修改数值
  2. 保存（务必选「CSV UTF-8」格式，见下方编码说明）
  3. 回 Unity 菜单：Tools → 策划表 → 一键导入全部
     导入完成会弹窗提示成功/失败条数

【六张表总览】

  1. UnitTable.csv 单位表（一行 = 一个单位，主键 unitID = 预制体文件名）
     列：unitID, camp, moveType, attackRangeType, attackType,
         maxHp, physicalDefense, magicDefense, atk, atkRange, atkCD, moveSpeed, detectRange

  2. CardTable.csv 卡牌表（一行 = 一张卡牌，主键 cardID）
     列：cardID, unitID, camp, cardName, cost, cooldown

  3. DeckTable.csv 初始牌组表（每行 = 某关卡组里的一张卡）
     列：levelIndex, cardID
     - cardID 引用 Card_{cardID}.asset（不生成新卡），填现有卡牌 ID
     - 同一 levelIndex 多行 = 该关可用的多张卡

  4. BuffTable.csv 关卡 Buff 表（每行 = 某关应用的一个 Buff）
     列：levelIndex, buffID
     - buffID 引用 Buff_{buffID}.asset（需先在 Unity 建好 Buff 资产）
     - 现有 Buff：PlayerAtkUp20（攻击力+20%）

  5. TowerTable.csv 塔属性表（主键 levelIndex + towerCamp，6 关 × 2 阵营）
     列：levelIndex, towerCamp, hp, atk, atkRange, atkCD, physicalDefense, magicDefense
     - towerCamp：Player / Enemy
     - 导入会启用塔覆盖并写入 LevelConfig，运行时需在场景 LevelSetup 接好
       playerTower / enemyTower 引用才会生效

  6. WaveTable_Level_N.csv 波次表（每关一个文件，N = 1..6）
     列：waveIndex, camp, unitID, pathID, spawnCount, spawnInterval, triggerType, delayBeforeStart
     - unitID 引用 Assets/Game/Unit/GeneralUnit/ 下的预制体文件名
     - spawnPoint（出生点）不进表，需要时在 Unity 的 WaveList 资产里手动拖

【导入后发生了什么】
  四张新表（牌组/Buff/塔/波次）导入时会生成/更新每关的 LevelConfig 资产：
    Assets/Resources/Config/LevelConfig_Level_N.asset
  LevelSetup 在游戏运行时自动读取该资产填充配置，无需修改场景物体。

【注意】
  - 表头（第一行）绝对不要改，列顺序可以随便拖；# 开头的行是注释
  - unitID / cardID / buffID 是主键，改成别的会找不到对应资源，建议不要乱改
  - 图标 / 血条等美术资源引用不进表，仍是在 Unity 里配置
  - 想加新单位：先在 Unity 里建好预制体，再在表里加一行（unitID = 预制体名）
  - 导入 / 导出前请先关闭正打开着的 CSV 文件（Excel 会锁定文件，否则会报「文件被占用」）
  - 导入前会自动备份被改动的文件到 _backup 目录，误操作可以恢复
  - 如果导入后发现问题：Ctrl+Z 撤销，或从 _backup 恢复文件
  - Level_2~6 波次表的 delayBeforeStart：旧资产里的 nextWaveInterval 字段已迁移为新字段，
    首次导入后会按新字段重写（旧延迟值可能已丢失，需重新填）

【编码说明（重要）】
  Excel 保存 CSV 时有坑，中文会乱码：
  - 推荐：文件 → 另存为 → 选择「CSV UTF-8（逗号分隔）」
  - 不要选「CSV（逗号分隔）」——那是 GBK 编码，本工具也能识别，但换电脑可能乱码
  - 表头列名请保持英文，避免编码差异导致匹配失败
