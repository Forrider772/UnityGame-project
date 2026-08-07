========================================
  策划配置表使用说明（单位表 + 卡牌表）
========================================

【这套配置表是干什么的】
  以前单位数值写在 Unity 预制体里、卡牌费用写在资产里，策划改数值必须打开 Unity 编辑器。
  现在把这些数值抽到两个 CSV 表格里，策划用 Excel 改表 → 回 Unity 点一个菜单 → 数值自动写入游戏。

【工作流（三步）】
  1. 用 Excel 打开本目录下的 UnitTable.csv / CardTable.csv 修改数值
  2. 保存（务必选「CSV UTF-8」格式，见下方编码说明）
  3. 回 Unity 菜单：Tools → 策划表 → 一键导入全部
     导入完成会弹窗提示成功/失败条数

【两张表】

  1. UnitTable.csv 单位表（一行 = 一个单位，主键 unitID = 预制体文件名）
     列说明：
       unitID            单位ID，必须等于 Assets/Game/Unit/GeneralUnit/ 下的预制体文件名（不含 .prefab）
       camp              阵营：Player（玩家）/ Enemy（敌人）
       moveType          移动：Ground（地面）/ Flying（飞行）
       attackRangeType   攻击距离：Melee（近战）/ Ranged（远程）
       attackType        伤害类型：Physical（物理）/ Magic（法术）
       maxHp             最大生命值
       physicalDefense   物理防御
       magicDefense      法术防御
       atk               基础攻击力
       atkRange          攻击范围
       atkCD             攻击间隔（秒）
       moveSpeed         移动速度
       detectRange       索敌范围

  2. CardTable.csv 卡牌表（一行 = 一张卡牌）
     列说明：
       cardID   卡牌ID，决定资产名 Card_{cardID}.asset（用英文）
       unitID   召唤的单位，必须与单位表的 unitID 对应
       camp     卡牌阵营：Player / Enemy
       cardName 卡牌显示名（中文）
       cost     部署费用
       cooldown 冷却时间（秒）

【注意】
  - 表头（第一行）绝对不要改，列顺序可以随便拖
  - unitID / cardID 是主键，改成别的会找不到对应资源，建议不要乱改
  - 图标 / 血条等美术资源引用不进表，仍是在 Unity 里配置
  - 想加新单位：先在 Unity 里建好预制体，再在表里加一行（unitID = 预制体名）
  - 导入 / 导出前请先关闭正打开着的 CSV 文件（Excel 会锁定文件，否则会报「文件被占用」）
  - 导入前会自动备份被改动的文件到 _backup 目录，误操作可以恢复
  - 如果导入后发现问题：Ctrl+Z 撤销，或从 _backup 恢复文件

【编码说明（重要）】
  Excel 保存 CSV 时有坑，中文会乱码：
  - 推荐：文件 → 另存为 → 选择「CSV UTF-8（逗号分隔）」
  - 不要选「CSV（逗号分隔）」——那是 GBK 编码，本工具也能识别，但换电脑可能乱码
  - 表头列名请保持英文，避免编码差异导致匹配失败
