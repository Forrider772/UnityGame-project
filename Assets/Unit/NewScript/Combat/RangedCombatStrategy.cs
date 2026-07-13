using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 远程战斗策略 —— 生成子弹/投射物攻击
/// 实现 ICombatStrategy，处理：索敌(按距离排序) + 追击 + 生成投射物
/// 伤害类型从 UnitAttr.attackType 读取，不再硬编码
/// </summary>
public class RangedCombatStrategy : MonoBehaviour, ICombatStrategy
{
    [Header("投射物配置")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 8f;
    [Tooltip("投射物生成点高度偏移")]
    public float spawnHeightOffset = 0f;

    private float attackTimer;

    void Update()
    {
        // 攻击冷却始终运行，与旧 ArcherCombat/MageCombat 行为一致
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
        // 远程单位可以攻击所有单位类型（地面+飞行）
        var validTargets = hits
            .Select(h => new { collider = h, dist = Vector2.Distance(position, h.transform.position) })
            .Where(x =>
            {
                UnitAttr ta = x.collider.GetComponent<UnitAttr>();
                return ta != null && ta.currentHp > 0;
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

        // 超出攻击范围 → 追击（新增！修复 Bug 2）
        if (distance > attr.atkRange)
        {
            movement.MoveToward(target.position, attr.moveSpeed);
            return true;
        }

        // 攻击冷却（由 Update 统一计时，此处只做判定和重置）
        if (attackTimer >= attr.atkCD)
        {
            attackTimer = 0f;
            SpawnBullet(target, attr);
            return true;
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

    void SpawnBullet(Transform target, UnitAttr attr)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"RangedCombatStrategy: {gameObject.name} 未赋值 bulletPrefab！");
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            // 使用配置的伤害类型（修复 Bug 4：不再硬编码 Magic）
            bulletScript.damage = attr.atk;
            bulletScript.target = target;
            bulletScript.speed = bulletSpeed;
            bulletScript.attackType = attr.attackType;
        }
    }
}
