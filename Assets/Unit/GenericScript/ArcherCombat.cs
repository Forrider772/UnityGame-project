using UnityEngine;

/// <summary>
/// 射手专属远程战斗模块
/// 完全不修改原有UnitCombat代码
/// 只接管攻击逻辑，其他全部复用原有系统
/// </summary>
public class ArcherCombat : MonoBehaviour
{
    [Header("远程攻击配置")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 8f;

    private UnitAttr attr;
    private UnitCombat originalCombat;
    private Transform currentTarget;
    private float attackTimer;

    void Awake()
    {
        // 获取原有组件引用
        attr = GetComponent<UnitAttr>();
        originalCombat = GetComponent<UnitCombat>();

        // 关键：冻结原有UnitCombat的攻击，让它永远不会出手
        originalCombat.enabled = false;
    }

    void Update()
    {
        // 死亡则停止所有逻辑
        if (attr.currentHp <= 0) return;

        // 1. 复用原有系统的索敌结果（完全和战士一样的索敌逻辑）
        currentTarget = GetCurrentTarget();

        // 2. 有目标就执行远程攻击
        if (currentTarget != null)
        {
            AttackTarget();
        }
    }

    // 反射获取UnitCombat里的私有currentTarget（完全不修改原代码）
    private Transform GetCurrentTarget()
    {
        var field = typeof(UnitCombat).GetField("currentTarget", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Transform)field.GetValue(originalCombat);
    }

    // 射手专属远程攻击
    private void AttackTarget()
    {
        // 目标超出攻击范围则停止攻击
        if (Vector3.Distance(transform.position, currentTarget.position) > attr.atkRange)
        {
            return;
        }

        // 自己管理攻击冷却
        attackTimer += Time.deltaTime;
        if (attackTimer < attr.atkCD) return;
        attackTimer = 0;

        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        // 传递伤害参数
        bulletScript.damage = attr.atk;
        bulletScript.target = currentTarget;
        bulletScript.speed = bulletSpeed;
        bulletScript.attackType = attr.attackType;
    }
}