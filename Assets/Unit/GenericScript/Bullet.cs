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
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr != null)
        {
            // 完全沿用你原有系统的伤害计算公式
            float finalDamage;
            if (attackType == AttackType.Physical)
            {
                finalDamage = damage * (100f / (100f + targetAttr.physicalDefense));
            }
            else
            {
                finalDamage = damage * (100f / (100f + targetAttr.magicDefense));
            }

            targetAttr.currentHp -= finalDamage;
        }

        // 销毁子弹
        Destroy(gameObject);
    }
}