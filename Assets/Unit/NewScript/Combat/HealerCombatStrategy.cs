using UnityEngine;

/// <summary>
/// 治疗战斗策略
/// 继承 BaseCombatStrategy，寻找血量百分比最低的友方单位进行治疗。
/// 治疗师不攻击防御塔。
/// </summary>
public class HealerCombatStrategy : BaseCombatStrategy
{
    [Header("=== 治疗属性 ===")]
    [Tooltip("每次治疗量")]
    public float healAmount = 25f;

    [Tooltip("治疗范围：搜索受伤友方的距离")]
    public float healRange = 10f;

    [Header("=== 治疗特效 ===")]
    [Tooltip("治疗特效预制体，在目标位置生成")]
    public GameObject healEffectPrefab;

    [Tooltip("特效生成高度偏移")]
    public float effectHeightOffset = 2f;

    [Header("=== 治疗策略 ===")]
    [Tooltip("是否可以治疗自己")]
    public bool canHealSelf = true;

    protected override Transform PerformDetection(UnitAttr attr, Vector2 position)
    {
        LayerMask allyMask = attr.camp == CampType.Player
            ? LayerMask.GetMask("PlayerUnit")
            : LayerMask.GetMask("EnemyUnit");

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, healRange, allyMask);

        Transform best = null;
        float lowestHpPercent = 1f;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!canHealSelf && hit.transform == transform) continue;

            UnitAttr allyAttr = hit.GetComponent<UnitAttr>();
            if (allyAttr == null || allyAttr.currentHp <= 0) continue;
            if (allyAttr.camp != attr.camp) continue;

            float hpPercent = allyAttr.currentHp / allyAttr.maxHp;
            if (hpPercent < lowestHpPercent && hpPercent < 0.99f)
            {
                lowestHpPercent = hpPercent;
                best = hit.transform;
            }
        }

        return best;
    }

    /// <summary>
    /// 重写 TryExecute：用 healRange 代替 atkRange 做距离判定
    /// </summary>
    public override bool TryExecute(Transform target, float deltaTime, UnitAttr attr,
                                    Vector2 position, IMoveStrategy movement)
    {
        if (target == null) return false;
        if (phase != AttackPhase.Idle) return false;

        float distance = Vector2.Distance(position, target.position);

        if (distance > healRange)
        {
            movement.MoveToward(target.position, attr.moveSpeed);
            return true;
        }

        phase = AttackPhase.Windup;
        phaseTimer = WindupDuration;
        return true;
    }

    protected override void ExecuteAttack(Transform target)
    {
        HealTarget(target);
    }

    private void HealTarget(Transform target)
    {
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr == null) return;

        targetAttr.currentHp = Mathf.Min(targetAttr.currentHp + healAmount, targetAttr.maxHp);

        UnitUI targetUI = target.GetComponent<UnitUI>();
        targetUI?.RefreshHp(targetAttr.currentHp);

        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab,
                target.position + Vector3.up * effectHeightOffset,
                Quaternion.identity);
        }
    }

    /// <summary>治疗师不攻击塔</summary>
    public override void SetTowerTarget(UnitAttr attr)
    {
        CurrentTarget = null;
        CancelAttack();
    }
}
