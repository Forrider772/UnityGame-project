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
/// 单位属性配置
/// 职责：纯数据存储，不处理任何逻辑、UI、移动、战斗
/// 所有模块统一读取该脚本数值
/// </summary>
public class UnitAttr : MonoBehaviour
{
    [Header("基础战斗属性")]
    public CampType camp;                // 所属阵营
    public float maxHp = 120f;      // 最大生命值
    public float atk = 8f;          // 基础攻击力
    public float moveSpeed = 1.8f;  // 移动速度
    public float atkRange = 1.2f;   // 普攻攻击范围
    public float atkCD = 1f;        // 攻击冷却时间

    [Header("索敌配置")]
    public float detectRange = 3f; // 大范围搜寻敌人半径

    [Header("防御属性")]
    public AttackType attackType;     // 自身攻击类型
    public float physicalDefense = 0f;// 物理防御减伤
    public float magicDefense = 0f;    // 法术防御减伤

    [Header("UI资源配置")]
    public GameObject hpBarPrefab;    // 血条预制体

    [HideInInspector]
    public float currentHp; // 当前运行时血量
}