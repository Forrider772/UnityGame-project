using UnityEngine;

/// <summary>
/// 近战战斗策略
/// 继承 BaseCombatStrategy，索敌按距离排序，跳过飞行单位。
/// </summary>
public class MeleeCombatStrategy : BaseCombatStrategy
{
    // 近战无额外 Inspector 配置项，所有参数由 BaseCombatStrategy 和 UnitAttr 提供
    // 索敌时自动跳过飞行单位（attackRangeType == Melee && moveType == Flying）

    protected override Transform PerformDetection(UnitAttr attr, Vector2 position)
    {
        LayerMask targetMask = attr.camp == CampType.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, attr.detectRange, targetMask);

        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            UnitAttr ta = hit.GetComponent<UnitAttr>();
            if (ta == null || ta.currentHp <= 0) continue;

            // 近战不能攻击飞行单位
            if (attr.attackRangeType == AttackRangeType.Melee && ta.moveType == MoveType.Flying)
                continue;

            float dist = Vector2.Distance(position, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hit.transform;
            }
        }

        return best;
    }

    protected override void ExecuteAttack(Transform target)
    {
        UnitAttr attr = GetComponent<UnitAttr>();
        DealDamage(target, attr.atk, attr.attackType);
    }
}
