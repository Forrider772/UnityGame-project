using UnityEngine;

/// <summary>
/// 远程战斗策略
/// 继承 BaseCombatStrategy，索敌按距离排序（可攻击飞行单位）。
/// 攻击执行时生成 Bullet 投射物。
/// </summary>
public class RangedCombatStrategy : BaseCombatStrategy
{
    [Header("=== 投射物 ===")]
    [Tooltip("子弹预制体，需挂载 Bullet 组件")]
    public GameObject bulletPrefab;

    [Tooltip("子弹飞行速度")]
    public float bulletSpeed = 8f;

    [Tooltip("子弹生成点相对于单位位置的 Y 轴偏移")]
    public float spawnHeightOffset = 0f;

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
        SpawnBullet(target, attr);
    }

    private void SpawnBullet(Transform target, UnitAttr attr)
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
            bulletScript.damage = attr.atk;
            bulletScript.target = target;
            bulletScript.speed = bulletSpeed;
            bulletScript.attackType = attr.attackType;
        }
    }
}
