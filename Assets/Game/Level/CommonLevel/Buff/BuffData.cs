using UnityEngine;
/// <summary>
/// Buff 修改的属性类型
/// 后续可扩展：HealAmount / Defense / CritRate 等
/// </summary>
public enum BuffStatType
{
    Attack,         // 攻击力
    MaxHp,          // 最大生命值
    MoveSpeed,      // 移动速度
    AttackSpeed,    // 攻击速度
    PhysicalDefense,// 物理防御
    MagicDefense    // 法术防御
}

/// <summary>
/// Buff 生效的阵营目标
/// </summary>
public enum BuffTargetCamp
{
    PlayerOnly,     // 仅玩家单位
    EnemyOnly,      // 仅敌方单位
    All             // 所有单位
}

/// <summary>
/// Buff 数据定义（ScriptableObject）
/// 配置一种 Buff 的效果：改什么属性、多少数值、对谁生效
///
/// 使用方式：
///   1. 右键 → Create → Battle → Buff Data 创建资产
///   2. 填入 buffName / statType / modifierValue / targetCamp
///   3. 拖入 LevelSetup.levelBuffs 数组中
///   4. 游戏开始时 BuffManager 自动读取并应用
///
/// modifierValue 说明：
///   +0.2  → 提升 20%（如 100 → 120）
///   -0.15 → 降低 15%（如 100 → 85）
///   多个同类型 buff 的 multiplier 会累乘：
///     +20%（×1.2）+ +10%（×1.1）= ×1.32
/// </summary>
[CreateAssetMenu(fileName = "New Buff", menuName = "Battle/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("=== 基本信息 ===")]
    [UnityEngine.Tooltip("Buff 显示名称，方便在 Inspector 中识别")]
    public string buffName = "新 Buff";

    [Header("=== 效果配置 ===")]
    [UnityEngine.Tooltip("要修改的属性类型")]
    public BuffStatType statType = BuffStatType.Attack;

    [UnityEngine.Tooltip("倍率偏移值：+0.2 表示提升 20%，-0.15 表示降低 15%")]
    public float modifierValue = 0.2f;

    [Header("=== 目标配置 ===")]
    [UnityEngine.Tooltip("对哪个阵营的单位生效")]
    public BuffTargetCamp targetCamp = BuffTargetCamp.PlayerOnly;
}
