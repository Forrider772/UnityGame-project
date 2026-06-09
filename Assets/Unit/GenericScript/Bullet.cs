using UnityEngine;

/// <summary>
/// 通用子弹脚本
/// 可被所有远程单位复用（法师、炮手等）
/// </summary>
public class Bullet : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public Transform target;
    [HideInInspector] public float speed;
    [HideInInspector] public AttackType attackType;

    void Update()
    {
        // 目标消失则销毁子弹
        if (target == null)
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
        // ✅ 先打单位（士兵/怪物）
        if (target.TryGetComponent(out UnitCombat unitCombat))
        {
            // 调用原有系统的TakeDamage，自动触发：伤害计算、血条刷新、死亡判定
            unitCombat.TakeDamage(damage, attackType);
        }
        // ✅ 再打防御塔（兼容你的防御塔系统）
        else if (target.TryGetComponent(out TowerBase tower))
        {
            tower.TakeDamage(damage, attackType);
        }

        // 销毁子弹
        Destroy(gameObject);
    }
}