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
/// 单位类型枚举（新增）
/// </summary>
public enum UnitType
{
    Ground, // 地面单位（默认值，原有兵种自动继承）
    Flying  // 飞行单位
}

/// <summary>
/// 攻击距离类型枚举（新增）
/// </summary>
public enum AttackRangeType
{
    Melee,  // 近战单位（默认值，原有兵种自动继承）
    Ranged  // 远程单位/防御塔
}

/// <summary>
/// 单位属性配置
/// 职责：纯数据存储，不处理任何逻辑、UI、移动、战斗
/// 所有模块统一读取该脚本数值
/// </summary>
public class UnitAttr : MonoBehaviour
{
    [Header("基础战斗属性")]
    public CampType camp;          // 所属阵营
    public float maxHp = 120f;     // 最大生命值
    public float atk = 8f;         // 基础攻击力
    public float moveSpeed = 1.8f; // 移动速度
    public float atkRange = 1.2f;  // 普攻攻击范围
    public float atkCD = 1f;       // 攻击冷却时间

    [Header("索敌配置")]
    public float detectRange = 3f; // 大范围搜寻敌人半径

    [Header("防御属性")]
    public AttackType attackType;  // 自身攻击类型
    public float physicalDefense = 0f; // 物理防御减伤
    public float magicDefense = 0f;    // 法术防御减伤

    [Header("UI资源配置")]
    public GameObject hpBarPrefab; // 血条预制体

    [Header("单位类型配置（新增）")]
    [Tooltip("单位类型：地面/飞行")]
    public UnitType unitType = UnitType.Ground;
    
    [Tooltip("攻击类型：近战/远程")]
    public AttackRangeType attackRangeType = AttackRangeType.Melee;

    [HideInInspector]
    public float currentHp; // 当前运行时血量

    [HideInInspector]
    public bool isGarrisoned;     // 是否处于驻扎状态
    [HideInInspector]
    public ResourcePoint garrisonedPoint; // 驻扎的资源点引用
    [HideInInspector]
    public GarrisonPoint garrisonedGarrisonPoint; // 驻扎的驻扎点引用（与 garrisonedPoint 互斥）
}