/// <summary>
/// 伤害结算器（静态工具类）
/// 统一所有伤害计算公式，消除 BaseCombatStrategy 和 TowerBase 中的重复代码
///
/// 公式：最终伤害 = max(1, 原始伤害 - 对应防御)
///   - 物理伤害 → 物理防御减免
///   - 法术伤害 → 法术防御减免
///   - 最低伤害保底：1
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 根据伤害类型和防御值，计算最终扣血量
    /// </summary>
    /// <param name="rawDamage">原始伤害值</param>
    /// <param name="type">伤害类型（Physical / Magic）</param>
    /// <param name="physicalDefense">目标物理防御</param>
    /// <param name="magicDefense">目标法术防御</param>
    /// <returns>减免后的最终伤害（最低为 1）</returns>
    public static float Calculate(float rawDamage, AttackType type,
        float physicalDefense, float magicDefense)
    {
        float reduction = (type == AttackType.Physical) ? physicalDefense : magicDefense;
        return UnityEngine.Mathf.Max(1f, rawDamage - reduction);
    }
}
