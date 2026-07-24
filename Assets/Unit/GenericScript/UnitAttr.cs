using UnityEngine;

/// <summary>
/// 攻击类型枚举
/// </summary>
public enum AttackType
{
    Physical, // 物理攻击
    Magic     // 法术攻击
}

/// <summary>
/// 攻击距离类型枚举
/// </summary>
public enum AttackRangeType
{
    Melee,  // 近战单位
    Ranged  // 远程单位
}

/// <summary>
/// 单位属性配置
/// 职责：纯数据存储，不处理任何逻辑、UI、移动、战斗
/// 所有模块统一读取该脚本数值
/// </summary>
public class UnitAttr : MonoBehaviour
{
    [Header("=== 阵营与类型 ===")]
    [Tooltip("所属阵营：Player（玩家）/ Enemy（敌人）")]
    public CampType camp;

    [Tooltip("移动类型：地面沿路径行走 / 飞行沿路径飞行")]
    public MoveType moveType = MoveType.Ground;

    [Tooltip("攻击类型：近战（不能打飞行单位）/ 远程（可打所有类型）")]
    public AttackRangeType attackRangeType = AttackRangeType.Melee;

    [Header("=== 生存属性 ===")]
    [Tooltip("最大生命值")]
    public float maxHp = 120f;

    [Tooltip("物理防御：减免物理伤害，最终伤害 = max(1, 伤害 - 防御)")]
    public float physicalDefense = 0f;

    [Tooltip("法术防御：减免法术伤害，最终伤害 = max(1, 伤害 - 防御)")]
    public float magicDefense = 0f;

    [Header("=== 攻击属性 ===")]
    [Tooltip("基础攻击力")]
    public float atk = 8f;

    [Tooltip("攻击类型（自身造成的伤害类型）：物理 / 法术")]
    public AttackType attackType;

    [Tooltip("攻击范围：触发攻击的最大距离")]
    public float atkRange = 1.2f;

    [Tooltip("攻击间隔：两次攻击之间的基础时间（秒），受攻速 buff 影响")]
    public float atkCD = 1f;

    [Header("=== 移动属性 ===")]
    [Tooltip("移动速度：沿路径行走的速度")]
    public float moveSpeed = 1.8f;

    [Header("=== 索敌属性 ===")]
    [Tooltip("索敌范围：搜索敌人的侦测半径，大于攻击范围")]
    public float detectRange = 3f;

    [Header("=== UI 配置 ===")]
    [Tooltip("血条预制体，挂载 HPBar 组件")]
    public GameObject hpBarPrefab;

    // ==================== 运行时状态（Inspector 隐藏） ====================
    [HideInInspector] public float currentHp;
    [HideInInspector] public bool isGarrisoned;
    [HideInInspector] public ResourcePoint garrisonedPoint;
    [HideInInspector] public GarrisonPoint garrisonedGarrisonPoint;
}
