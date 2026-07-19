using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 近战战斗策略
/// 实现 ICombatStrategy，处理：索敌(按距离排序) + 近战追击 + 近战挥砍 + 受伤减伤 + 死亡
/// </summary>
public class MeleeCombatStrategy : MonoBehaviour, ICombatStrategy
{
    private float attackTimer;

    void Update()
    {
        // 攻击冷却始终运行，与旧 UnitCombat 行为一致
        // 确保首击不会被 "进入范围后才开始计时" 延迟
        attackTimer += Time.deltaTime;
    }

    public Transform CurrentTarget { get; private set; }

    // ==================== ICombatStrategy 实现 ====================

    public Transform DetectTarget(UnitAttr attr, Vector2 position)
    {
        LayerMask targetMask = attr.camp == CampType.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, attr.detectRange, targetMask);

        // 按距离排序，找到最近的有效目标
        var validTargets = hits
            .Select(h => new { collider = h, dist = Vector2.Distance(position, h.transform.position) })
            .Where(x =>
            {
                UnitAttr ta = x.collider.GetComponent<UnitAttr>();
                if (ta == null || ta.currentHp <= 0) return false;

                // 核心规则：近战不能攻击飞行单位
                if (attr.attackRangeType == AttackRangeType.Melee
                    && ta.moveType == MoveType.Flying)
                    return false;

                return true;
            })
            .OrderBy(x => x.dist);

        var best = validTargets.FirstOrDefault();
        CurrentTarget = best?.collider.transform;
        return CurrentTarget;
    }

    public bool TryExecute(Transform target, float deltaTime, UnitAttr attr,
                           Vector2 position, IMoveStrategy movement)
    {
        if (target == null) return false;

        float distance = Vector2.Distance(position, target.position);

        // 超出攻击范围 → 追击
        if (distance > attr.atkRange)
        {
            movement.MoveToward(target.position, attr.moveSpeed);
            return true; // 执行了追击
        }

        // 攻击冷却（由 Update 统一计时，此处只做判定和重置）
        if (attackTimer >= attr.atkCD)
        {
            attackTimer = 0f;
            DealDamage(target, attr.atk, attr.attackType);
            return true; // 执行了攻击
        }

        return false;
    }

    public void SetTowerTarget(UnitAttr attr)
    {
        if (attr.camp == CampType.Player)
        {
            if (BattleManager.Instance.enemyTower != null)
                CurrentTarget = BattleManager.Instance.enemyTower.transform;
            else
                CurrentTarget = null;
        }
        else
        {
            if (BattleManager.Instance.playerTower != null)
                CurrentTarget = BattleManager.Instance.playerTower.transform;
            else
                CurrentTarget = null;
        }
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

    void DealDamage(Transform target, float damage, AttackType attackType)
    {
        // 对敌方单位造成伤害
        if (target.TryGetComponent<UnitBrain>(out var unitBrain))
        {
            unitBrain.TakeDamage(damage, attackType);
            return;
        }
        // 对防御塔造成伤害
        if (target.TryGetComponent<TowerBase>(out var tower))
        {
            tower.TakeDamage(damage, attackType);
            return;
        }
        // 兜底：目标既没有 UnitBrain 也没有 TowerBase —— 预制体可能未迁移
        Debug.LogWarning(
            $"MeleeCombatStrategy: {gameObject.name} 攻击 {target.name}，" +
            $"目标缺少 UnitBrain 和 TowerBase 组件，伤害无法结算！请确认目标预制体已迁移到新体系。",
            target);
    }
}
