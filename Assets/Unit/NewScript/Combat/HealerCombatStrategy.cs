using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 治疗战斗策略 —— 寻找血量最低的友方单位并进行治疗
/// 实现 ICombatStrategy，无目标时返回 null 让 UnitBrain 切换到 Moving 状态
/// </summary>
public class HealerCombatStrategy : MonoBehaviour, ICombatStrategy
{
    [Header("治疗配置")]
    public float healAmount = 25f;
    public float healRange = 10f;
    public float healCooldown = 2f;
    public GameObject healEffectPrefab;
    public float effectHeightOffset = 2f;
    public bool canHealSelf = true;

    private float healTimer;

    public Transform CurrentTarget { get; private set; }

    void Awake()
    {
        healTimer = healCooldown; // 初始就可以治疗
    }

    // ==================== ICombatStrategy 实现 ====================

    /// <summary>
    /// 寻找血量百分比最低的友方单位
    /// 无治疗目标时返回 null（让 UnitBrain 走 Moving 状态）
    /// </summary>
    public Transform DetectTarget(UnitAttr attr, Vector2 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, healRange);

        Transform lowestHpTarget = null;
        float lowestHpPercent = 1f;

        foreach (var collider in colliders)
        {
            // 可选：不治疗自己
            if (!canHealSelf && collider.transform == transform) continue;

            UnitAttr allyAttr = collider.GetComponent<UnitAttr>();
            if (allyAttr != null && allyAttr.currentHp > 0 && allyAttr.camp == attr.camp)
            {
                float hpPercent = allyAttr.currentHp / allyAttr.maxHp;
                // 只治疗血量低于 99% 的（避免满血浪费）
                if (hpPercent < lowestHpPercent && hpPercent < 0.99f)
                {
                    lowestHpPercent = hpPercent;
                    lowestHpTarget = collider.transform;
                }
            }
        }

        CurrentTarget = lowestHpTarget;
        return CurrentTarget;
    }

    /// <summary>
    /// 执行治疗（面向目标旋转 + 冷却计时 + 治疗）
    /// 返回 true 表示执行了治疗行动
    /// </summary>
    public bool TryExecute(Transform target, float deltaTime, UnitAttr attr,
                           Vector2 position, IMoveStrategy movement)
    {
        if (target == null) return false;

        // 面向治疗目标旋转
        Vector3 dir = (target.position - (Vector3)position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(Vector3.forward, new Vector3(dir.x, dir.y, 0)),
                10f * deltaTime);

        // 冷却计时
        healTimer -= deltaTime;
        if (healTimer <= 0)
        {
            HealTarget(target, attr);
            healTimer = healCooldown;
            return true;
        }

        return false; // 在冷却中
    }

    public void SetTowerTarget(UnitAttr attr)
    {
        // 治疗师不攻击塔，清空目标
        CurrentTarget = null;
    }

    public void TakeDamage(float damage, AttackType type, UnitAttr attr, Action onDie)
    {
        float finalDmg = damage;

        if (type == AttackType.Physical)
            finalDmg = Mathf.Max(1f, damage - attr.physicalDefense);
        else if (type == AttackType.Magic)
            finalDmg = Mathf.Max(1f, damage - attr.magicDefense);

        attr.currentHp -= finalDmg;

        if (attr.currentHp <= 0)
            onDie?.Invoke();
    }

    // ==================== 内部方法 ====================

    void HealTarget(Transform target, UnitAttr selfAttr)
    {
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr == null) return;

        targetAttr.currentHp = Mathf.Min(targetAttr.currentHp + healAmount, targetAttr.maxHp);

        // 刷新目标的血条
        UnitUI targetUI = target.GetComponent<UnitUI>();
        targetUI?.RefreshHp(targetAttr.currentHp);

        // 治疗特效
        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab,
                target.position + Vector3.up * effectHeightOffset,
                Quaternion.identity);
        }
    }
}
