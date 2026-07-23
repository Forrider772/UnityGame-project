using UnityEngine;

/// <summary>
/// 通用子弹/投射物
/// 可被所有远程单位复用（射手、法师等）
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("=== 运行时数据（由远程策略设置） ===")]
    [HideInInspector] public float damage;
    [HideInInspector] public Transform target;
    [HideInInspector] public float speed;
    [HideInInspector] public AttackType attackType;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 目标已死亡 → 销毁子弹
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr != null && targetAttr.currentHp <= 0)
        {
            Destroy(gameObject);
            return;
        }
        TowerBase targetTower = target.GetComponent<TowerBase>();
        if (targetTower != null && targetTower.hp <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 飞向目标
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 命中判定
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target.TryGetComponent<UnitBrain>(out var unitBrain))
        {
            unitBrain.TakeDamage(damage, attackType);
        }
        else if (target.TryGetComponent<TowerBase>(out var tower))
        {
            tower.TakeDamage(damage, attackType);
        }

        Destroy(gameObject);
    }
}
