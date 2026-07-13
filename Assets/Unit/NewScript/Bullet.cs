using UnityEngine;

/// <summary>
/// 通用子弹/投射物脚本
/// 可被所有远程单位复用（射手、法师等）
///
/// 修复内容：
///   - 检查目标是否存活（Bug 5 修复：不攻击尸体）
///   - 伤害目标改为 UnitBrain（新架构）
///   - 伤害类型从配置传递（Bug 4 修复：不再硬编码）
/// </summary>
public class Bullet : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public Transform target;
    [HideInInspector] public float speed;
    [HideInInspector] public AttackType attackType;

    void Update()
    {
        // 目标被销毁 → 销毁子弹
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 目标已死亡（被其他单位击杀但尚未 Destroy）→ 销毁子弹（Bug 5 修复）
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr != null && targetAttr.currentHp <= 0)
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
        // 新系统：UnitBrain
        if (target.TryGetComponent<UnitBrain>(out var unitBrain))
        {
            unitBrain.TakeDamage(damage, attackType);
        }
        // 对防御塔造成伤害
        else if (target.TryGetComponent<TowerBase>(out var tower))
        {
            tower.TakeDamage(damage, attackType);
        }

        Destroy(gameObject);
    }
}
